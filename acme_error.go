package api

import (
	"github.com/zamn/acme-cli-dist-test/internal/clients/rest/httptransport"
	"net/http"
)

// AcmeError wraps API errors with detailed metadata including status code, headers, and raw response.
// It implements the error interface and provides structured access to error information.
type AcmeError[T any] struct {
	Err      error
	Data     *T
	Body     []byte
	Raw      *http.Response
	Metadata AcmeErrorMetadata
}

// AcmeErrorMetadata contains HTTP metadata associated with an error response.
type AcmeErrorMetadata struct {
	Headers    map[string]string
	StatusCode int
}

// NewAcmeError creates a new AcmeError from an internal transport error.
// It extracts error details, body, status code, and headers into a user-facing error structure.
func NewAcmeError[T any](transportError *httptransport.ErrorResponse[T]) *AcmeError[T] {
	return &AcmeError[T]{
		Err:  transportError.GetError(),
		Data: transportError.Data,
		Body: transportError.GetBody(),
		Raw:  transportError.Raw,
		Metadata: AcmeErrorMetadata{
			StatusCode: transportError.GetStatusCode(),
			Headers:    transportError.GetHeaders(),
		},
	}
}

// Error implements the error interface, returning the error message string.
func (e *AcmeError[T]) Error() string {
	if e == nil || e.Err == nil {
		return ""
	}
	return e.Err.Error()
}

// Unwrap returns the underlying error, enabling errors.Is and errors.As to traverse the chain.
func (e *AcmeError[T]) Unwrap() error {
	return e.Err
}

// GetData returns the deserialized error response data.
// Returns nil if unmarshaling failed or the response body was empty.
func (e *AcmeError[T]) GetData() *T {
	return e.Data
}

// GetBody returns the raw response body bytes from the error response.
// Returns nil if no response body was received.
func (e *AcmeError[T]) GetBody() []byte {
	return e.Body
}
