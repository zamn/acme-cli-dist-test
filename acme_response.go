package api

import (
	"encoding/json"
	"github.com/zamn/acme-cli-dist-test/internal/clients/rest/httptransport"
	"net/http"
)

// AcmeResponse is the user-facing wrapper for API responses.
// It contains the deserialized data, raw HTTP response, and metadata like headers and status code.
type AcmeResponse[T any] struct {
	Data     T
	Raw      *http.Response
	Metadata AcmeResponseMetadata
}

// AcmeResponseMetadata contains HTTP metadata from the API response.
// Includes status code and headers for inspection and debugging.
type AcmeResponseMetadata struct {
	Headers    map[string]string
	StatusCode int
}

// NewAcmeResponse creates a new response wrapper from an internal transport response.
// Extracts data and metadata into a user-facing structure.
func NewAcmeResponse[T any](resp *httptransport.Response[T]) *AcmeResponse[T] {
	return &AcmeResponse[T]{
		Data: resp.Data,
		Raw:  resp.Raw,
		Metadata: AcmeResponseMetadata{
			StatusCode: resp.StatusCode,
			Headers:    resp.Headers,
		},
	}
}

// GetData returns the deserialized response data.
func (r *AcmeResponse[T]) GetData() T {
	return r.Data
}

// String returns a JSON representation of the response for debugging.
// Returns an error message if JSON marshaling fails.
func (r AcmeResponse[T]) String() string {
	jsonData, err := json.MarshalIndent(r, "", "  ")
	if err != nil {
		return "error converting struct: AcmeResponse to string"
	}
	return string(jsonData)
}
