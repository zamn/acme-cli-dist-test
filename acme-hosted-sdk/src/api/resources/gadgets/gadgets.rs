use crate::api::*;
use crate::{ApiError, ClientConfig, HttpClient, RequestOptions};
use reqwest::Method;

pub struct GadgetsClient {
    pub http_client: HttpClient,
}

impl GadgetsClient {
    pub fn new(config: ClientConfig) -> Result<Self, ApiError> {
        Ok(Self {
            http_client: HttpClient::new(config.clone())?,
        })
    }

    /// # Examples
    ///
    /// ```no_run
    /// use acme_hosted_sdk::prelude::*;
    ///
    /// #[tokio::main]
    /// async fn main() {
    ///     let config = ClientConfig {
    ///         ..Default::default()
    ///     };
    ///     let client = AcmeHostedClient::new(config).expect("Failed to build client");
    ///     client.gadgets.list_gadgets(None).await;
    /// }
    /// ```
    pub async fn list_gadgets(
        &self,
        options: Option<RequestOptions>,
    ) -> Result<ListGadgetsResponse, ApiError> {
        self.http_client
            .execute_request(Method::GET, "gadgets", None, None, options)
            .await
    }
}
