package api

import (
	"github.com/zamn/acme-cli-dist-test/acmeconfig"
	"github.com/zamn/acme-cli-dist-test/param"
)

// The type aliases below let consumers use a single import path for the entire SDK.
// Internally the concrete types live in acmeconfig and param.

// Config holds all configuration parameters for the SDK client.
type Config = acmeconfig.Config

// RequestOption is a function that configures a single request.
type RequestOption = acmeconfig.RequestOption

// RetryConfig holds all runtime-configurable retry parameters.
type RetryConfig = acmeconfig.RetryConfig

// NewConfig creates a Config with spec-derived defaults.
var NewConfig = acmeconfig.NewConfig

// NewRetryConfig returns a RetryConfig initialized with spec-derived defaults.
var NewRetryConfig = acmeconfig.NewRetryConfig

// WithBaseURL returns a RequestOption that overrides BaseURL for a single request.
var WithBaseURL = acmeconfig.WithBaseURL

// WithTimeout returns a RequestOption that overrides Timeout for a single request.
var WithTimeout = acmeconfig.WithTimeout

// WithRetryConfig returns a RequestOption that overrides the RetryConfig for a single request.
var WithRetryConfig = acmeconfig.WithRetryConfig

// Nullable returns a *param.Nullable[T] set to v — use for nullable fields with a value.
func Nullable[T any](v T) *param.Nullable[T] { return &param.Nullable[T]{Value: v} }

// Null returns a *param.Nullable[T] with IsNull set to true, signalling an explicit JSON null.
func Null[T any]() *param.Nullable[T] { return param.Null[T]() }

// Ptr returns a pointer to v — use when no type-specific helper exists.
func Ptr[T any](v T) *T { return param.Ptr(v) }
