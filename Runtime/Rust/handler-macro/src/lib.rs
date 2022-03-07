use proc_macro::TokenStream;
use proc_macro_error::{abort, abort_if_dirty, emit_error, emit_warning};
use quote::{format_ident, quote, quote_spanned};
use syn::parse::{Parse, ParseStream};
use syn::punctuated::Punctuated;
use syn::spanned::Spanned;
use syn::token::RArrow;
use syn::{
    Attribute, GenericArgument, Ident, ImplItem, ImplItemMethod, ItemFn, ItemImpl, PathArguments,
    ReturnType, Token, Type, TypePath,
};

/// Allows converting a function which "returns a value" to one which actually calls a callback.
/// This allows submitting a borrowed value which is owned by the function scope and preventing
/// extra copies of the data in some cases, such as serialization where we need to write it to a
/// buffer immediately anyway.

const HANDLERS_POSTFIX: &str = "HandlersDef";

#[proc_macro_attribute]
pub fn handlers(args: TokenStream, input: TokenStream) -> TokenStream {
    let mut item = syn::parse_macro_input!(args as ItemImpl);

    // extract the service name from the trait that is being implemented
    let service_name = {
        let tr = if let Some(tr) = &item.trait_ {
            tr
        } else {
            abort!(item.span(), "Must be applied to a handler impl")
        };
        let ident = &tr.1.segments.last().expect("Path must be defined").ident;
        let mut ident_str = ident.to_string();
        if !ident_str.ends_with(HANDLERS_POSTFIX) {
            abort!(
                ident.span(),
                "Must be applied to a Handlers Definition implementation"
            )
        }
        ident_str.truncate(ident_str.len() - HANDLERS_POSTFIX.len());
        format_ident!("{}", ident_str, span = ident.span())
    };

    // ensure that this is being implemented for Arc<_>
    if let Type::Path(ty) = item.self_ty.as_ref() {
        if ty.path.segments.last().unwrap().ident != "Arc" {
            emit_warning!(
                ty.span(),
                "Handlers should be implemented for an Arc<{}> to reduce cloning",
                service_name.to_string()
            )
        }
    } else {
        emit_error!(item.self_ty.span(), "Unknown self type");
    }

    for item in item.items.iter_mut() {
        if let ImplItem::Method(method) = item {
            process_method(&service_name, method);
        }
    }

    abort_if_dirty();
    TokenStream::from(quote!(#item))
}

fn process_method(service_name: &Ident, item: &mut ImplItemMethod) {
    if !process_method_attrs(item) {
        return;
    }

    if !item.sig.asyncness.is_some() {
        emit_error!(item.sig.asyncness.span(), "Function must be async")
    }

    let method_name = &item.sig.ident;
    let return_type = if let Some(t) = extract_method_return_type(item) {
        t
    } else {
        return
    };

    // let h = quote_spanned! {span=>
    //     fn #ident<'f>(&self, __handle: ::bebop::rpc::TypedRequestHandle<'f, #return_struct>, #args) ->
    //         ::bebop::rpc::DynFuture<'f, #ret>
    //     {
    //         let __call_id = __handle.call_id().get();
    //         let __self = self.clone(); // only do if there is a reference to self in the block
    //         Box::pin(async move {
    //             let details: &dyn ::bebop::rpc::CallDetails = &__handle;
    //             let __response = async {
    //                 #body
    //             }.await;
    //             ::bebop::handle_respone_error!(
    //                 __handle.send_response(__response),
    //                 #quoted_service,
    //                 #quoted_fn_name,
    //                 __call_id,
    //             )
    //         })
    //     }
    // };
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
            emit_error!(attr.tokens, "Unexpected token in handler attribute")
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
                emit_error!(
                    item.sig.output.span(),
                    "Output must be a LocalRpcResponse type"
                );
                return None;
            }
            if let PathArguments::AngleBracketed(generic_args) = &segment.arguments {
                if generic_args.args.len() != 1 {
                    emit_error!(
                        generic_args.args.span(),
                        "Expected exactly one generic type argument"
                    );
                    return None;
                }
                if let GenericArgument::Type(inner_ty) = generic_args.args.first().unwrap() {
                    Some(inner_ty)
                } else {
                    emit_error!(
                        generic_args.args.first().unwrap().span(),
                        "Expected a type argument"
                    );
                    None
                }
            } else {
                emit_error!(segment.arguments.span(), "Missing template type");
                None
            }
        } else {
            emit_error!(
                item.sig.output.span(),
                "Output must be a LocalRpcResponse type"
            );
            None
        }
    } else {
        emit_error!(
            item.sig.output.span(),
            "Output must be a LocalRpcResponse type"
        );
        None
    }
}
