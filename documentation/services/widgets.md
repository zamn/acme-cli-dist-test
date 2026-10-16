# Widgets

A list of all methods in the `Widgets` service. Click on the method name to view detailed information about that method.

| Methods                     | Description |
| :-------------------------- | :---------- |
| [ListWidgets](#listwidgets) |             |

## ListWidgets

- HTTP Method: `GET`
- Endpoint: `/widgets`

**Parameters**

| Name | Type    | Required | Description                 |
| :--- | :------ | :------- | :-------------------------- |
| ctx  | Context | ✅       | Default go language context |

**Return Type**

`api.ListWidgetsResponse`

**Example Usage Code Snippet**

```go
import (
  "fmt"
  "encoding/json"
  "context"
  "github.com/zamn/acme-cli-dist-test"
)

config := api.NewConfig()

client := api.NewClient(config)

response, err := client.Widgets.ListWidgets(context.Background())
if err != nil {
  panic(err)
}

fmt.Println(response)
```
