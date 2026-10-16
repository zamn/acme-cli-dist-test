package internal

import (
	"bytes"
	"encoding/json"
	"fmt"
	"net/http"
	"reflect"
	"strings"
	"time"
)

// MarshalJSONWithExtraProperty marshals the given value to JSON, including the extra property.
func MarshalJSONWithExtraProperty(marshaler interface{}, key string, value interface{}) ([]byte, error) {
	return MarshalJSONWithExtraProperties(marshaler, map[string]interface{}{key: value})
}

// SetHeader sets a request header from a declared header parameter. A nil pointer leaves the header
// unset, and any other value is written in its default string form.
func SetHeader(headers http.Header, name string, value interface{}) {
	reflected := reflect.ValueOf(value)
	if reflected.Kind() == reflect.Ptr {
		if reflected.IsNil() {
			return
		}
		value = reflected.Elem().Interface()
	}
	headers.Set(name, fmt.Sprintf("%v", value))
}

const dateLayout = "2006-01-02"

// ParseDateFields rewrites the date-only strings held at the given JSON keys (directly, or inside
// an array or an object value) into RFC 3339 date-times, so encoding/json can decode them into a
// time.Time. Anything that is not a date-only string is left as it is.
func ParseDateFields(data []byte, keys ...string) []byte {
	return rewriteDateFields(data, keys, func(value string) (string, bool) {
		if _, err := time.Parse(dateLayout, value); err != nil {
			return "", false
		}
		return value + "T00:00:00Z", true
	})
}

// FormatDateFields returns a function that rewrites the RFC 3339 date-times held at the given JSON
// keys into date-only strings, for use on the result of json.Marshal.
func FormatDateFields(keys ...string) func([]byte, error) ([]byte, error) {
	return func(data []byte, err error) ([]byte, error) {
		if err != nil {
			return data, err
		}
		return rewriteDateFields(data, keys, func(value string) (string, bool) {
			parsed, parseErr := time.Parse(time.RFC3339Nano, value)
			if parseErr != nil {
				return "", false
			}
			return parsed.Format(dateLayout), true
		}), nil
	}
}

func rewriteDateFields(data []byte, keys []string, convert func(string) (string, bool)) []byte {
	var fields map[string]json.RawMessage
	if err := json.Unmarshal(data, &fields); err != nil {
		return data
	}
	changed := false
	for _, key := range keys {
		raw, ok := fields[key]
		if !ok {
			continue
		}
		if rewritten, ok := rewriteDateValue(raw, convert); ok {
			fields[key] = rewritten
			changed = true
		}
	}
	if !changed {
		return data
	}
	rewritten, err := json.Marshal(fields)
	if err != nil {
		return data
	}
	return rewritten
}

func rewriteDateValue(raw json.RawMessage, convert func(string) (string, bool)) (json.RawMessage, bool) {
	var single string
	if err := json.Unmarshal(raw, &single); err == nil {
		converted, ok := convert(single)
		if !ok {
			return raw, false
		}
		encoded, err := json.Marshal(converted)
		return encoded, err == nil
	}
	var list []json.RawMessage
	if err := json.Unmarshal(raw, &list); err == nil {
		changed := false
		for i, item := range list {
			if rewritten, ok := rewriteDateValue(item, convert); ok {
				list[i] = rewritten
				changed = true
			}
		}
		if !changed {
			return raw, false
		}
		encoded, err := json.Marshal(list)
		return encoded, err == nil
	}
	var object map[string]json.RawMessage
	if err := json.Unmarshal(raw, &object); err == nil {
		changed := false
		for key, item := range object {
			if rewritten, ok := rewriteDateValue(item, convert); ok {
				object[key] = rewritten
				changed = true
			}
		}
		if !changed {
			return raw, false
		}
		encoded, err := json.Marshal(object)
		return encoded, err == nil
	}
	return raw, false
}

// MarshalJSONWithDiscriminant marshals the given value to JSON and sets the union discriminant,
// replacing the value the variant struct carries for the same key. A variant that declares its own
// discriminant property would otherwise be rejected by MarshalJSONWithExtraProperty.
func MarshalJSONWithDiscriminant(marshaler interface{}, key string, value interface{}) ([]byte, error) {
	marshaled, err := json.Marshal(marshaler)
	if err != nil {
		return nil, err
	}
	fields := map[string]json.RawMessage{}
	if !isEmptyJSON(marshaled) {
		if err := json.Unmarshal(marshaled, &fields); err != nil {
			return nil, err
		}
	}
	encoded, err := json.Marshal(value)
	if err != nil {
		return nil, err
	}
	fields[key] = encoded
	return json.Marshal(fields)
}

// MarshalJSONWithExtraProperties marshals the given value to JSON, including any extra properties.
func MarshalJSONWithExtraProperties(marshaler interface{}, extraProperties map[string]interface{}) ([]byte, error) {
	bytes, err := json.Marshal(marshaler)
	if err != nil {
		return nil, err
	}
	if len(extraProperties) == 0 {
		return bytes, nil
	}
	keys, err := getKeys(marshaler)
	if err != nil {
		return nil, err
	}
	for _, key := range keys {
		if _, ok := extraProperties[key]; ok {
			return nil, fmt.Errorf("cannot add extra property %q because it is already defined on the type", key)
		}
	}
	extraBytes, err := json.Marshal(extraProperties)
	if err != nil {
		return nil, err
	}
	if isEmptyJSON(bytes) {
		if isEmptyJSON(extraBytes) {
			return bytes, nil
		}
		return extraBytes, nil
	}
	result := bytes[:len(bytes)-1]
	result = append(result, ',')
	result = append(result, extraBytes[1:len(extraBytes)-1]...)
	result = append(result, '}')
	return result, nil
}

// ExtractExtraProperties extracts any extra properties from the given value.
func ExtractExtraProperties(bytes []byte, value interface{}, exclude ...string) (map[string]interface{}, error) {
	val := reflect.ValueOf(value)
	for val.Kind() == reflect.Ptr {
		if val.IsNil() {
			return nil, fmt.Errorf("value must be non-nil to extract extra properties")
		}
		val = val.Elem()
	}
	if err := json.Unmarshal(bytes, &value); err != nil {
		return nil, err
	}
	var extraProperties map[string]interface{}
	if err := json.Unmarshal(bytes, &extraProperties); err != nil {
		return nil, err
	}
	for i := 0; i < val.Type().NumField(); i++ {
		key := jsonKey(val.Type().Field(i))
		if key == "" || key == "-" {
			continue
		}
		delete(extraProperties, key)
	}
	for _, key := range exclude {
		delete(extraProperties, key)
	}
	if len(extraProperties) == 0 {
		return nil, nil
	}
	return extraProperties, nil
}

// getKeys returns the keys associated with the given value. The value must be a
// a struct or a map with string keys.
func getKeys(value interface{}) ([]string, error) {
	val := reflect.ValueOf(value)
	if val.Kind() == reflect.Ptr {
		val = val.Elem()
	}
	if !val.IsValid() {
		return nil, nil
	}
	switch val.Kind() {
	case reflect.Struct:
		return getKeysForStructType(val.Type()), nil
	case reflect.Map:
		var keys []string
		if val.Type().Key().Kind() != reflect.String {
			return nil, fmt.Errorf("cannot extract keys from %T; only structs and maps with string keys are supported", value)
		}
		for _, key := range val.MapKeys() {
			keys = append(keys, key.String())
		}
		return keys, nil
	default:
		return nil, fmt.Errorf("cannot extract keys from %T; only structs and maps with string keys are supported", value)
	}
}

// getKeysForStructType returns all the keys associated with the given struct type,
// visiting embedded fields recursively.
func getKeysForStructType(structType reflect.Type) []string {
	if structType.Kind() == reflect.Pointer {
		structType = structType.Elem()
	}
	if structType.Kind() != reflect.Struct {
		return nil
	}
	var keys []string
	for i := 0; i < structType.NumField(); i++ {
		field := structType.Field(i)
		if field.Anonymous {
			keys = append(keys, getKeysForStructType(field.Type)...)
			continue
		}
		keys = append(keys, jsonKey(field))
	}
	return keys
}

// jsonKey returns the JSON key from the struct tag of the given field,
// excluding the omitempty flag (if any).
func jsonKey(field reflect.StructField) string {
	return strings.TrimSuffix(field.Tag.Get("json"), ",omitempty")
}

// isEmptyJSON returns true if the given data is empty, the empty JSON object, or
// an explicit null.
func isEmptyJSON(data []byte) bool {
	return len(data) <= 2 || bytes.Equal(data, []byte("null"))
}
