pub use crate::prelude::*;
#[allow(unused_imports)]
use super::*;

#[derive(Debug, Clone, Serialize, Deserialize, Default, PartialEq, Eq, Hash)]
pub struct ListGadgetsResponse {
    #[serde(skip_serializing_if = "Option::is_none")]
    pub name: Option<String>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub description: Option<String>,
}

impl ListGadgetsResponse {
    pub fn builder() -> ListGadgetsResponseBuilder {
        <ListGadgetsResponseBuilder as Default>::default()
    }
}

#[derive(Clone, PartialEq, Default, Debug)]
#[non_exhaustive]
pub struct ListGadgetsResponseBuilder {
    name: Option<String>,
    description: Option<String>,
}

impl ListGadgetsResponseBuilder {
    pub fn name(mut self, value: impl Into<String>) -> Self {
        self.name = Some(value.into());
        self
    }

    pub fn description(mut self, value: impl Into<String>) -> Self {
        self.description = Some(value.into());
        self
    }

    /// Consumes the builder and constructs a [`ListGadgetsResponse`].
    pub fn build(self) -> Result<ListGadgetsResponse, BuildError> {
        Ok(ListGadgetsResponse {
            name: self.name,
            description: self.description,
        })
    }
}
