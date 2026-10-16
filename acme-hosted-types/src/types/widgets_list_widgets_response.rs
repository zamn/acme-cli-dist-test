pub use crate::prelude::*;
#[allow(unused_imports)]
use super::*;

#[derive(Debug, Clone, Serialize, Deserialize, Default, PartialEq, Eq, Hash)]
pub struct ListWidgetsResponse {
    #[serde(skip_serializing_if = "Option::is_none")]
    pub id: Option<String>,
}

impl ListWidgetsResponse {
    pub fn builder() -> ListWidgetsResponseBuilder {
        <ListWidgetsResponseBuilder as Default>::default()
    }
}

#[derive(Clone, PartialEq, Default, Debug)]
#[non_exhaustive]
pub struct ListWidgetsResponseBuilder {
    id: Option<String>,
}

impl ListWidgetsResponseBuilder {
    pub fn id(mut self, value: impl Into<String>) -> Self {
        self.id = Some(value.into());
        self
    }

    /// Consumes the builder and constructs a [`ListWidgetsResponse`].
    pub fn build(self) -> Result<ListWidgetsResponse, BuildError> {
        Ok(ListWidgetsResponse {
            id: self.id,
        })
    }
}
