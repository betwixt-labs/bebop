use async_trait::async_trait;

/// A service used when one end of the channel does not offer any callable endpoints.
/// You can also use a NullService for a remote which is any other service and it will
/// mask it, making it impossible to call, but also not causing any errors.
pub struct NullService;

// #[async_trait]
// impl ServiceHandlers for NullService {
//     async fn _recv_call(&self, opcode: u16, sig: u32, call_id: u16, buf: &[u8]) {
//         todo!()
//     }
// }
//
// impl ServiceRequests for NullService {
//     const NAME: &'static str = "";
// }
