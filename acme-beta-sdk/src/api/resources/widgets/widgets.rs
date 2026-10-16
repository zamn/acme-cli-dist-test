use crate::api::*;
use crate::{ApiError, ClientConfig, HttpClient, RequestOptions};
use reqwest::Method;

pub struct WidgetsClient {
    pub http_client: HttpClient,
}

impl WidgetsClient {
    pub fn new(config: ClientConfig) -> Result<Self, ApiError> {
        Ok(Self {
            http_client: HttpClient::new(config.clone())?,
        })
    }

    /// # Examples
    ///
    /// ```no_run
    /// use acme_beta_sdk::prelude::*;
    ///
    /// #[tokio::main]
    /// async fn main() {
    ///     let config = ClientConfig {
    ///         ..Default::default()
    ///     };
    ///     let client = AcmeBetaClient::new(config).expect("Failed to build client");
    ///     client.widgets.list_widgets(None).await;
    /// }
    /// ```
    pub async fn list_widgets(
        &self,
        options: Option<RequestOptions>,
    ) -> Result<ListWidgetsResponse, ApiError> {
        self.http_client
            .execute_request(Method::GET, "widgets", None, None, options)
            .await
    }
}
