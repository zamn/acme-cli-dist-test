package api

import "encoding/json"

type ListGadgetsResponse struct {
	Name        *string `json:"name,omitempty" xml:"name,omitempty"`
	Description *string `json:"description,omitempty" xml:"description,omitempty"`
}

func (l *ListGadgetsResponse) GetName() (value *string) {
	if l == nil {
		return
	}
	return l.Name
}

func (l *ListGadgetsResponse) GetDescription() (value *string) {
	if l == nil {
		return
	}
	return l.Description
}

func (l *ListGadgetsResponse) SetName(name *string) {
	l.Name = name
}

func (l *ListGadgetsResponse) SetDescription(description *string) {
	l.Description = description
}

func (l *ListGadgetsResponse) GetExtraProperties() map[string]interface{} {
	return nil
}

func (l ListGadgetsResponse) String() string {
	jsonData, err := json.MarshalIndent(l, "", "  ")
	if err != nil {
		return "error converting struct: ListGadgetsResponse to string"
	}
	return string(jsonData)
}

func (l *ListGadgetsResponse) UnmarshalJSON(data []byte) error {
	type unmarshaler ListGadgetsResponse
	var value unmarshaler
	if err := json.Unmarshal(data, &value); err != nil {
		return err
	}
	*l = ListGadgetsResponse(value)
	return nil
}

func (l *ListGadgetsResponse) MarshalJSON() ([]byte, error) {
	type embed ListGadgetsResponse
	var marshaler = struct {
		embed
	}{
		embed: embed(*l),
	}
	return json.Marshal(marshaler)
}
