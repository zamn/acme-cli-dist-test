//! Request and response types for the Acme UAT API
//!
//! This module contains all data structures used for API communication,
//! including request bodies, response types, and shared models.
//!
//! ## Type Categories
//!
//! - **Request/Response Types**: 2 types for API operations

pub mod widgets_list_widgets_response;
pub mod gadgets_list_gadgets_response;

pub use widgets_list_widgets_response::ListWidgetsResponse;
pub use gadgets_list_gadgets_response::ListGadgetsResponse;

