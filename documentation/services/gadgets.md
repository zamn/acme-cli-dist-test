# Gadgets

A list of all methods in the `Gadgets` service. Click on the method name to view detailed information about that method.

| Methods                     | Description |
| :-------------------------- | :---------- |
| [ListGadgets](#listgadgets) |             |

## ListGadgets

- HTTP Method: `GET`
- Endpoint: `/gadgets`

**Parameters**

| Name | Type    | Required | Description                 |
| :--- | :------ | :------- | :-------------------------- |
| ctx  | Context | ✅       | Default go language context |

**Return Type**

`api.ListGadgetsResponse`

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

response, err := client.Gadgets.ListGadgets(context.Background())
if err != nil {
  panic(err)
}

fmt.Println(response)
```
