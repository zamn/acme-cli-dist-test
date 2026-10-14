//! API client and types for the Acme UAT API
//!
//! This module contains all the API definitions including request/response types
//! and client implementations for interacting with the API.
//!
//! ## Modules
//!
//! - [`resources`] - Service clients and endpoints

pub mod resources;

pub use resources::{ApiClient, WidgetsClient};

pub use acme_hosted_types::*;
