# Acme Go SDK 0.4.0

Welcome to the Acme SDK documentation. This guide will help you get started with integrating and using the Acme SDK in your project.

## Versions

- SDK version: `0.4.0`

## Table of Contents

- [Setup & Configuration](#setup--configuration)
  - [Supported Language Versions](#supported-language-versions)
- [Setting a Custom Timeout](#setting-a-custom-timeout)
- [Sample Usage](#sample-usage)
- [Services](#services)
  - [Response Wrappers](#response-wrappers)
- [Models](#models)

# Setup & Configuration

## Supported Language Versions

This SDK is compatible with the following versions: `Go >= 1.19.0`

## Setting a Custom Timeout

You can set a custom timeout for the SDK's HTTP requests as follows:

```go
import "time"

config := api.NewConfig()

sdk := api.NewClient(config)

sdk.SetTimeout(10 * time.Second)
```

# Sample Usage

Below is a comprehensive example demonstrating how to authenticate and call a simple endpoint:

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

## Services

The SDK provides various services to interact with the API.

<details>
<summary>Below is a list of all available services with links to their detailed documentation:</summary>

| Name                                         |
| :------------------------------------------- |
| [Widgets](documentation/services/widgets.md) |
| [Gadgets](documentation/services/gadgets.md) |

</details>

### Response Wrappers

All services use response wrappers to provide a consistent interface to return the responses from the API.

The response wrapper itself is a generic struct that contains the response data and metadata.

<details>
<summary>Below are the response wrappers used in the SDK:</summary>

#### `AcmeResponse[T]`

This response wrapper is used to return the response data from the API. It contains the following fields:

| Name     | Type                   | Description                                 |
| :------- | :--------------------- | :------------------------------------------ |
| Data     | `T`                    | The body of the API response                |
| Metadata | `AcmeResponseMetadata` | Status code and headers returned by the API |

#### `AcmeError[T]`

This response wrapper is used to return an error. It contains the following fields:

| Name     | Type                | Description                                                       |
| :------- | :------------------ | :---------------------------------------------------------------- |
| Err      | `error`             | The error that occurred                                           |
| Data     | `*T`                | The deserialized error response data (nil if unmarshaling failed) |
| Body     | `[]byte`            | The raw body of the API response                                  |
| Metadata | `AcmeErrorMetadata` | Status code and headers returned by the API                       |

#### `AcmeResponseMetadata`

This struct is shared by both response wrappers and contains the following fields:

| Name       | Type                | Description                                      |
| :--------- | :------------------ | :----------------------------------------------- |
| Headers    | `map[string]string` | A map containing the headers returned by the API |
| StatusCode | `int`               | The status code returned by the API              |

</details>

## Models

The SDK includes several models that represent the data structures used in API requests and responses. These models help in organizing and managing the data efficiently.

<details>
<summary>Below is a list of all available models with links to their detailed documentation:</summary>

| Name                                                                 | Description |
| :------------------------------------------------------------------- | :---------- |
| [ListWidgetsResponse](documentation/models/list_widgets_response.md) |             |
| [ListGadgetsResponse](documentation/models/list_gadgets_response.md) |             |

</details>
