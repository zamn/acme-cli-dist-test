//! Service clients and API endpoints
//!
//! This module contains client implementations for:
//!
//! - **Widgets**
//! - **Gadgets**

use crate::{ApiError, ClientConfig};

pub mod gadgets;
pub mod widgets;
pub struct ApiClient {
    pub config: ClientConfig,
    pub widgets: WidgetsClient,
    pub gadgets: GadgetsClient,
}

impl ApiClient {
    pub fn new(config: ClientConfig) -> Result<Self, ApiError> {
        Ok(Self {
            config: config.clone(),
            widgets: WidgetsClient::new(config.clone())?,
            gadgets: GadgetsClient::new(config.clone())?,
        })
    }
}

pub use gadgets::GadgetsClient;
pub use widgets::WidgetsClient;
