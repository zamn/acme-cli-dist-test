package api

import "encoding/json"

type ListWidgetsResponse struct {
	ID *string `json:"id,omitempty" xml:"id,omitempty"`
}

func (l *ListWidgetsResponse) GetID() (value *string) {
	if l == nil {
		return
	}
	return l.ID
}

func (l *ListWidgetsResponse) SetID(id *string) {
	l.ID = id
}

func (l *ListWidgetsResponse) GetExtraProperties() map[string]interface{} {
	return nil
}

func (l ListWidgetsResponse) String() string {
	jsonData, err := json.MarshalIndent(l, "", "  ")
	if err != nil {
		return "error converting struct: ListWidgetsResponse to string"
	}
	return string(jsonData)
}

func (l *ListWidgetsResponse) UnmarshalJSON(data []byte) error {
	type unmarshaler ListWidgetsResponse
	var value unmarshaler
	if err := json.Unmarshal(data, &value); err != nil {
		return err
	}
	*l = ListWidgetsResponse(value)
	return nil
}

func (l *ListWidgetsResponse) MarshalJSON() ([]byte, error) {
	type embed ListWidgetsResponse
	var marshaler = struct {
		embed
	}{
		embed: embed(*l),
	}
	return json.Marshal(marshaler)
}
