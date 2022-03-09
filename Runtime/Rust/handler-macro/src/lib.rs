use proc_macro::TokenStream;
use proc_macro2::TokenTree;

use quote::{format_ident, quote, quote_spanned, ToTokens};
use syn::spanned::Spanned;
use syn::{
    parse_quote, FnArg, Ident, ImplItem, ImplItemMethod, ItemImpl, Pat, ReturnType, Stmt, Token,
    Type,
};

const HANDLERS_POSTFIX: &str = "HandlersDef";

/// Put quotes around a string to create a string literal.
macro_rules! quoted {
    ($name:expr) => {{
        let name = $name.to_string();
        if name.starts_with('"') && name.ends_with('"') {
            name
        } else {
            format!("\"{}\"", $name)
        }
    }};
}

fn handlers_2(mut item: ItemImpl) -> proc_macro2::TokenStream {
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
            process_method(&service_name, method);
        }
    }

    quote!(#item)
}

#[cfg(test)]
mod test {
    use super::handlers_2;
    use quote::quote;
    use syn::{parse_quote, ItemImpl};

    #[test]
    fn basic_sync() {
        let item = parse_quote! {
            #[handlers]
            impl KVStoreHandlersDef for Arc<MemBackedKVStore> {
                #[handler]
                async fn ping(self, details: &dyn CallDetails) -> LocalRpcResponse<()> {
                    Err(LocalRpcError::CustomErrorStatic(4, "some error"))
                }
            }
        };
        println!("{}", handlers_2(item));
    }
}

/// Allows converting a function which "returns a value" to one which actually calls a callback.
/// This allows submitting a borrowed value which is owned by the function scope and preventing
/// extra copies of the data in some cases, such as serialization where we need to write it to a
/// buffer immediately anyway.
#[proc_macro_attribute]
pub fn handlers(_args: TokenStream, input: TokenStream) -> TokenStream {
    let item = syn::parse_macro_input!(input as ItemImpl);
    TokenStream::from(handlers_2(item))
}

fn process_method(service_name: &Ident, item: &mut ImplItemMethod) {
    if !process_method_attrs(item) {
        return;
    }

    if item.sig.asyncness.is_none() {
        panic!("Function must be async")
    }
    item.sig.asyncness = None;
    item.sig.generics.params.insert(0, parse_quote! { '__fut });

    let method_name = &item.sig.ident;
    // let request_handle_type = extract_method_return_type(item).expect("Missing return type!").clone();
    let ret_type = quote_spanned! {item.sig.output.span()=>
        -> ::bebop::rpc::DynFuture<'__fut>
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

    let arg_call_details_ident = if let Some(FnArg::Typed(a)) = args_iter.next() {
        let ident = if let Pat::Ident(ident) = a.pat.as_ref() {
            ident.clone()
        } else {
            panic!("Unexpected pattern type, expected ident")
        };
        let ret_struct = format_ident!(
            "{}{}Return",
            service_name,
            pascal_case(&method_name.to_string())
        );
        let pat = quote_spanned! {a.pat.span()=>
            __handle
        };
        let ty = quote_spanned! { a.ty.span()=>
            ::bebop::rpc::TypedRequestHandle<'__fut, #ret_struct>
        };
        a.pat = parse_quote! { #pat };
        a.ty = parse_quote! { #ty };
        ident
    } else {
        drop(args_iter);
        panic!("Function must receive call details")
    };
    drop(args_iter);

    let old_body_statements: Vec<proc_macro2::TokenStream> = item
        .block
        .stmts
        .drain(0..item.block.stmts.len())
        .map(|stmt| {
            // convert self to __self
            let t: proc_macro2::TokenStream = stmt
                .into_token_stream()
                .into_iter()
                .map(|tok| {
                    match tok {
                        TokenTree::Ident(mut i) => {
                            if i == "self" {
                                i = format_ident!("__self");
                            }
                            TokenTree::Ident(i)
                        },
                        tok => tok
                    }
                })
                .collect();
            t
        })
        .collect();

    let quoted_service_name = service_name.to_string();
    let quoted_method_name = method_name.to_string();

    let block = quote_spanned! {item.block.span()=>
        {
            let __call_id = __handle.call_id().get();
            let __self = self.clone(); // only do if there is a reference to self in the block
            Box::pin(async move {
                let __response = async {
                    let #arg_call_details_ident = &__handle;
                    #(#old_body_statements)*
                }.await;
                ::bebop::handle_respond_error!(
                    __handle.send_response(__response),
                    #quoted_service_name,
                    #quoted_method_name,
                    __call_id
                )
            })
        }
    };
    item.block = parse_quote! { #block };
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
        Some(ty)
        // if let Type::Path(p) = ty.as_ref() {
        //     let segment = p.path.segments.last().unwrap();
        //     if segment.ident != "LocalRpcResponse" {
        //         emit_error!(
        //             item.sig.output.span(),
        //             "Output must be a LocalRpcResponse type"
        //         );
        //         return None;
        //     }
        //     if let PathArguments::AngleBracketed(generic_args) = &segment.arguments {
        //         if generic_args.args.len() != 1 {
        //             emit_error!(
        //                 generic_args.args.span(),
        //                 "Expected exactly one generic type argument"
        //             );
        //             return None;
        //         }
        //         if let GenericArgument::Type(inner_ty) = generic_args.args.first().unwrap() {
        //             Some(inner_ty)
        //         } else {
        //             emit_error!(
        //                 generic_args.args.first().unwrap().span(),
        //                 "Expected a type argument"
        //             );
        //             None
        //         }
        //     } else {
        //         emit_error!(segment.arguments.span(), "Missing template type");
        //         None
        //     }
        // } else {
        //     emit_error!(
        //         item.sig.output.span(),
        //         "Output must be a LocalRpcResponse type"
        //     );
        //     None
        // }
    } else {
        panic!("Output must be a LocalRpcResponse type")
    }
}
