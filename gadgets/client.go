package gadgets

import (
	context "context"
	api "github.com/zamn/acme-cli-dist-test"
	core "github.com/zamn/acme-cli-dist-test/core"
	internal "github.com/zamn/acme-cli-dist-test/internal"
	option "github.com/zamn/acme-cli-dist-test/option"
)

type Client struct {
	WithRawResponse *RawClient

	options *core.RequestOptions
	baseURL string
	caller  *internal.Caller
}

func NewClient(options *core.RequestOptions) *Client {
	return &Client{
		WithRawResponse: NewRawClient(options),
		options:         options,
		baseURL:         options.BaseURL,
		caller: internal.NewCaller(
			&internal.CallerParams{
				Client:         options.HTTPClient,
				MaxAttempts:    options.MaxAttempts,
				DisableRetries: options.DisableRetries,
			},
		),
	}
}

func (c *Client) ListGadgets(ctx context.Context, opts ...option.RequestOption) (*api.ListGadgetsResponse, error) {
	response, err := c.WithRawResponse.ListGadgets(ctx, opts...)
	if err != nil {
		return nil, err
	}
	return response.Body, nil
}
