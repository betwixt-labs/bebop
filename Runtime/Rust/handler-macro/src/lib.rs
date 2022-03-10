use proc_macro2::{Group, TokenStream, TokenTree};
use quote::{format_ident, quote, quote_spanned, ToTokens};
use syn::spanned::Spanned;
use syn::{
    parse_quote, FnArg, GenericArgument, GenericParam, Ident, ImplItem, ImplItemMethod, ItemImpl,
    Pat, Path, PathArguments, ReturnType, Stmt, Type,
};

const HANDLERS_POSTFIX: &str = "HandlersDef";

fn handlers_2(mut item: ItemImpl, generated_path: Path) -> TokenStream {
    // extract the service name from the trait that is being implemented
    let service_name = {
        let tr = if let Some(tr) = &item.trait_ {
            tr
        } else {
            panic!("{:?}, Must be applied to a handler impl", item.span())
        };
        let ident = &tr.1.segments.last().expect("Path must be defined").ident;
        let mut ident_str = ident.to_string();
        if !ident_str.ends_with(HANDLERS_POSTFIX) {
            panic!("Must be applied to a Handlers Definition implementation")
        }
        ident_str.truncate(ident_str.len() - HANDLERS_POSTFIX.len());
        format_ident!("{}", ident_str, span = ident.span())
    };

    // ensure that this is being implemented for Arc<_>
    if let Type::Path(ty) = item.self_ty.as_ref() {
        if ty.path.segments.last().unwrap().ident != "Arc" {
            panic!(
                "Handlers should be implemented for an Arc<{}> to reduce cloning",
                service_name.to_string()
            )
        }
    } else {
        panic!("Unknown self type");
    }

    for item in item.items.iter_mut() {
        if let ImplItem::Method(method) = item {
            process_method(&service_name, method, &generated_path);
        }
    }

    quote!(#item)
}

/// Allows converting a function which "returns a value" to one which actually calls a callback.
/// This allows submitting a borrowed value which is owned by the function scope and preventing
/// extra copies of the data in some cases, such as serialization where we need to write it to a
/// buffer immediately anyway.
#[proc_macro_attribute]
pub fn handlers(
    args: proc_macro::TokenStream,
    input: proc_macro::TokenStream,
) -> proc_macro::TokenStream {
    let generated_path = syn::parse_macro_input!(args as Path);
    let item = syn::parse_macro_input!(input as ItemImpl);
    proc_macro::TokenStream::from(handlers_2(item, generated_path))
}

fn process_method(service_name: &Ident, item: &mut ImplItemMethod, generated_path: &Path) {
    if !process_method_attrs(item) {
        return;
    }

    if item.sig.asyncness.is_none() {
        panic!("Function must be async")
    }
    item.sig.asyncness = None;
    let (lifetime, custom_lifetime) = {
        let maybe_g = item
            .sig
            .generics
            .params
            .iter()
            .filter_map(|g| {
                if let GenericParam::Lifetime(ldef) = g {
                    if ldef.lifetime.ident == "sup"
                        || ldef.lifetime.ident == "fut"
                        || ldef.lifetime.ident == "__fut"
                    {
                        Some(g.clone())
                    } else {
                        None
                    }
                } else {
                    None
                }
            })
            .next();
        if let Some(g) = maybe_g {
            (g, true)
        } else {
            let lt: GenericParam = parse_quote! { '__fut };
            item.sig.generics.params.insert(0, lt.clone());
            (lt, false)
        }
    };
    let lifetime_str = if let GenericParam::Lifetime(lt) = &lifetime {
        lt.lifetime.ident.to_string()
    } else {
        unreachable!()
    };

    let method_name = &item.sig.ident;
    let mut block_return_type = extract_method_return_type(item)
        .expect("Missing return type!")
        .clone();

    if custom_lifetime {
        // verify the block return does use the custom lifetime, otherwise, it probably is a bug
        assert!(
            stream_contains_ident(block_return_type.to_token_stream(), &lifetime_str),
            "Return type does not use defined lifetime '{lifetime_str}"
        );
    }

    replace_lifetime(&mut block_return_type, &lifetime_str, &format_ident!("_"));

    let ret_type = quote_spanned! {item.sig.output.span()=>
        -> ::bebop::rpc::DynFuture<#lifetime>
    };
    item.sig.output = parse_quote! { #ret_type };

    let mut args_iter = item.sig.inputs.iter_mut();
    if let Some(FnArg::Receiver(a)) = args_iter.next() {
        if a.reference.is_some() || a.mutability.is_some() {
            panic!("Expected receiver of type `self`");
        }
        let mut n = parse_quote! { &self };
        std::mem::swap(&mut n, a);
    } else {
        drop(args_iter);
        panic!("Function must have self argument")
    };

    let ret_struct_ident = format_ident!(
        "{}{}Return",
        service_name,
        pascal_case(&method_name.to_string())
    );
    let ret_struct: Type = if custom_lifetime {
        parse_quote!(#generated_path::#ret_struct_ident<#lifetime>)
    } else {
        parse_quote!(#generated_path::#ret_struct_ident)
    };

    // let ret_struct_needs_lt = item.span().unwrap().source_file()

    let arg_call_details_ident = if let Some(FnArg::Typed(a)) = args_iter.next() {
        let ident = if let Pat::Ident(ident) = a.pat.as_ref() {
            ident.clone()
        } else {
            panic!("Unexpected pattern type, expected ident")
        };
        let pat = quote_spanned! {a.pat.span()=>
            __handle
        };
        let ty = quote_spanned! { a.ty.span()=>
            ::bebop::rpc::TypedRequestHandle<#lifetime, #ret_struct>
        };
        a.pat = parse_quote! { #pat };
        a.ty = parse_quote! { #ty };
        ident
    } else {
        drop(args_iter);
        panic!("Function must receive call details")
    };
    drop(args_iter);

    let old_body_statements: Vec<TokenStream> = {
        let mut ret_struct = ret_struct;
        replace_lifetime(&mut ret_struct, &lifetime_str, &format_ident!("_"));
        process_method_statements(
            item.block.stmts.drain(0..item.block.stmts.len()).collect(),
            ret_struct,
            block_return_type,
        )
    };

    let quoted_service_name = service_name.to_string();
    let quoted_method_name = method_name.to_string();

    let block = quote_spanned! {item.block.span()=>
        {
            let __call_id = __handle.call_id().get();
            let __self = self.clone(); // only do if there is a reference to self in the block
            Box::pin(async move {
                let #arg_call_details_ident = &__handle;
                #(#old_body_statements)*

                ::bebop::handle_respond_error!(
                    __handle.send_response(__response.as_ref()),
                    #quoted_service_name,
                    #quoted_method_name,
                    __call_id
                )
            })
        }
    };
    item.block = parse_quote! { #block };
}

fn replace_lifetime(ty: &mut Type, old: &str, new: &Ident) {
    if let Type::Path(p) = ty {
        if let Some(last) = p.path.segments.last_mut() {
            if let PathArguments::AngleBracketed(generics) = &mut last.arguments {
                for a in generics.args.iter_mut() {
                    match a {
                        GenericArgument::Lifetime(lt) => {
                            if lt.ident == old {
                                lt.ident = new.clone();
                            }
                        }
                        GenericArgument::Type(ty) => {
                            replace_lifetime(ty, old, new);
                        }
                        _ => {}
                    }
                }
            }
        }
    }
}

fn replace_ident_token(stream: TokenStream, old: &str, new: &Ident) -> TokenStream {
    stream
        .into_iter()
        .map(|tok| match tok {
            TokenTree::Ident(i) => {
                if i == old {
                    TokenTree::Ident(new.clone())
                } else {
                    TokenTree::Ident(i)
                }
            }
            TokenTree::Group(g) => {
                let ts = replace_ident_token(g.stream(), old, new);
                TokenTree::Group(Group::new(g.delimiter(), ts))
            }
            tok => tok,
        })
        .collect()
}

fn stream_contains_ident(stream: TokenStream, ident: &str) -> bool {
    for tok in stream.into_iter() {
        match tok {
            TokenTree::Group(g) => {
                if stream_contains_ident(g.stream(), ident) {
                    return true;
                }
            }
            TokenTree::Ident(i) => {
                if i == ident {
                    return true;
                }
            }
            _ => {}
        }
    }
    false
}

fn process_method_statements(
    stmts: Vec<Stmt>,
    ret_struct: Type,
    block_return_type: Type,
) -> Vec<TokenStream> {
    if stmts.is_empty() {
        // if they did not specify anything, just return a `NotSupported` error by default
        return vec![parse_quote! {
            let __response: ::bebop::rpc::LocalRpcResponse<#ret_struct> = Err(::bebop::rpc::LocalRpcError::NotSupported);
        }];
    }

    // for all but the beginning of the last statement, ensure the keyword `return` never occurs.
    for stmt in stmts.iter() {
        assert!(
            !stream_contains_ident(stmt.to_token_stream(), "return"),
            "Return statements within `handler` functions are not supported"
        );
    }
    assert!(
        matches!(stmts.last(), Some(Stmt::Expr(_))),
        "Last statement must be an expression!"
    );

    let stmt_count = stmts.len();
    stmts
        .into_iter()
        .enumerate()
        .map(|(i, mut stmt)| {
            if i + 1 == stmt_count {
                // modify return statement to be mapped and assigned to __response
                stmt = parse_quote! {
                    let __response: ::bebop::rpc::LocalRpcResponse<#ret_struct> =
                        { #stmt }.map(|v: #block_return_type| v.into());
                };
            }

            // convert self to __self
            replace_ident_token(stmt.into_token_stream(), "self", &format_ident!("__self"))
        })
        .collect()
}

fn process_method_attrs(item: &mut ImplItemMethod) -> bool {
    if let Some(idx) = item
        .attrs
        .iter()
        .enumerate()
        .filter_map(|(idx, i)| {
            if i.path.segments.len() == 1 && i.path.segments.first().unwrap().ident == "handler" {
                Some(idx)
            } else {
                None
            }
        })
        .next()
    {
        // remote the attribute since we are handling it
        let attr = item.attrs.remove(idx);
        if !attr.tokens.is_empty() {
            panic!("Unexpected token in handler attribute")
        }
        true
    } else {
        false
    }
}

fn extract_method_return_type(item: &ImplItemMethod) -> Option<&Type> {
    if let ReturnType::Type(_arrow, ty) = &item.sig.output {
        if let Type::Path(p) = ty.as_ref() {
            let segment = p.path.segments.last().unwrap();
            if segment.ident != "LocalRpcResponse" {
                panic!("Output must be a LocalRpcResponse type")
            }
            if let PathArguments::AngleBracketed(generic_args) = &segment.arguments {
                if generic_args.args.len() != 1 {
                    panic!("Expected exactly one generic type argument")
                }
                if let GenericArgument::Type(inner_ty) = generic_args.args.first().unwrap() {
                    Some(inner_ty)
                } else {
                    panic!("Expected a type argument")
                }
            } else {
                panic!("Missing template type")
            }
        } else {
            panic!("Output must be a LocalRpcResponse type")
        }
    } else {
        panic!("Output must be a LocalRpcResponse type")
    }
}

fn pascal_case(s: &str) -> String {
    let mut r = String::new();

    // allow leading underscores
    s.chars().take_while(|&c| c == '_').for_each(|c| r.push(c));

    // capitalize the first char
    if let Some(c) = s.chars().next() {
        c.to_uppercase().for_each(|c| r.push(c));
    }

    let mut cap_next = false;
    for cur in s.chars().skip(1) {
        if cur == '_' || cur == '-' {
            cap_next = true;
        } else if cur.is_digit(10) {
            cap_next = true;
            r.push(cur);
        } else if cap_next {
            cap_next = false;
            cur.to_uppercase().for_each(|c| r.push(c));
        } else {
            r.push(cur);
        }
    }
    r
}

#[cfg(test)]
mod test {
    use syn::parse_quote;

    use super::handlers_2;

    #[test]
    fn basic_err_sync() {
        let item = parse_quote! {
            #[handlers(crate::generated::rpc)]
            impl KVStoreHandlersDef for Arc<MemBackedKVStore> {
                #[handler]
                async fn ping(self, _details: &dyn CallDetails) -> LocalRpcResponse<()> {
                    Err(LocalRpcError::CustomErrorStatic(4, "some error"))
                }
            }
        };
        println!("{}", handlers_2(item, parse_quote!(crate::generated::rpc)));
    }

    #[test]
    fn basic_ok_sync() {
        // ok case is harder because of borrowing requirements
        let item = parse_quote! {
            #[handlers(crate::generated::rpc)]
            impl KVStoreHandlersDef for Arc<MemBackedKVStore> {
                #[handler]
                async fn ping(self, _details: &dyn CallDetails) -> LocalRpcResponse<()> {
                    Ok(())
                }
            }
        };
        println!("{}", handlers_2(item, parse_quote!(crate::generated::rpc)));
    }

    #[test]
    fn borrowed_return_err_sync() {
        let item = parse_quote! {
            #[handlers(crate::generated::rpc)]
            impl KVStoreHandlersDef for Arc<MemBackedKVStore> {
                #[handler]
                async fn entries<'sup>(self, _details: &dyn CallDetails, page: u64, page_size: u16) -> LocalRpcResponse<Vec<KV<'sup>>> {
                    Err(LocalRpcError::NotSupported)
                }
            }
        };
        println!("{}", handlers_2(item, parse_quote!(crate::generated::rpc)));
    }

    #[test]
    fn borrowed_return_ok_async() {
        let item = parse_quote! {
            #[handlers(crate::generated::rpc)]
            impl KVStoreHandlersDef for Arc<MemBackedKVStore> {
                #[handler]
                async fn entries<'sup>(self, _details: &dyn CallDetails, page: u64, page_size: u16) -> LocalRpcResponse<Vec<KV<'sup>>> {
                    let lock = self.0.read().await;
                    Ok(lock
                        .iter()
                        .skip(page as usize * page_size as usize)
                        .take(page_size as usize)
                        .map(|(k, v)| KV {
                            key: k,
                            value: v,
                        })
                        .collect()
                    )
                }
            }
        };
        println!("{}", handlers_2(item, parse_quote!(crate::generated::rpc)));
    }

    #[test]
    #[should_panic]
    fn return_type_must_use_sup() {
        let item = parse_quote! {
            #[handlers(crate::generated::rpc)]
            impl KVStoreHandlersDef for Arc<MemBackedKVStore> {
                #[handler]
                async fn entries<'sup>(self, _details: &dyn CallDetails) -> LocalRpcResponse<()> {
                    Ok(())
                }
            }
        };
        handlers_2(item, parse_quote!(crate::generated::rpc));
    }

    #[test]
    #[should_panic]
    fn does_not_allow_return_statements() {
        let item = parse_quote! {
            #[handlers(crate::generated::rpc)]
            impl KVStoreHandlersDef for Arc<MemBackedKVStore> {
                #[handler]
                async fn entries(self, _details: &dyn CallDetails) -> LocalRpcResponse<()> {
                    if true {
                       return Err(LocalRpcError::NotSupported);
                    }
                    Ok(())
                }
            }
        };
        handlers_2(item, parse_quote!(crate::generated::rpc));
    }

    #[test]
    fn defaults_to_not_supported() {
        let item = parse_quote! {
            #[handlers(crate::generated::rpc)]
            impl KVStoreHandlersDef for Arc<MemBackedKVStore> {
                #[handler]
                async fn entries(self, _details: &dyn CallDetails) -> LocalRpcResponse<()> {}
            }
        };
        assert!(handlers_2(item, parse_quote!(crate::generated::rpc))
            .to_string()
            .contains("NotSupported"));
    }
}
