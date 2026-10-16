package client

import (
	"github.com/zamn/acme-cli-dist-test/core"
	"github.com/zamn/acme-cli-dist-test/gadgets"
	"github.com/zamn/acme-cli-dist-test/internal"
	"github.com/zamn/acme-cli-dist-test/option"
	"github.com/zamn/acme-cli-dist-test/widgets"
)

type Client struct {
	Widgets *widgets.Client
	Gadgets *gadgets.Client
	options *core.RequestOptions
	baseURL string
	caller  *internal.Caller
}

func NewClient(opts ...option.RequestOption) *Client {
	options := core.NewRequestOptions(opts...)
	return &Client{
		Widgets: widgets.NewClient(options),
		Gadgets: gadgets.NewClient(options),
		options: options,
		baseURL: options.BaseURL,
		caller: internal.NewCaller(
			&internal.CallerParams{
				Client:         options.HTTPClient,
				MaxAttempts:    options.MaxAttempts,
				DisableRetries: options.DisableRetries,
			},
		),
	}
}
