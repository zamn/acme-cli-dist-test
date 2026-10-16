package configmanager

import (
	"github.com/zamn/acme-cli-dist-test/acmeconfig"
	"time"
)

// ConfigManager manages configuration across all services with synchronized updates.
// Provides centralized configuration management and OAuth token handling for multiple services.
type ConfigManager struct {
	widgets acmeconfig.Config
	gadgets acmeconfig.Config
}

// NewConfigManager creates a new configuration manager with the provided config and optional OAuth token service.
// Initializes service-specific configs and sets up OAuth token management if enabled.
func NewConfigManager(config acmeconfig.Config) *ConfigManager {
	return &ConfigManager{
		widgets: config,
		gadgets: config,
	}
}

// SetBaseURL updates the BaseURL configuration parameter across all services.
// Changes are applied synchronously to all registered service configurations.
func (c *ConfigManager) SetBaseURL(baseURL string) {
	c.widgets.SetBaseURL(baseURL)
	c.gadgets.SetBaseURL(baseURL)
}

// SetTimeout updates the Timeout configuration parameter across all services.
// Changes are applied synchronously to all registered service configurations.
func (c *ConfigManager) SetTimeout(timeout time.Duration) {
	c.widgets.SetTimeout(timeout)
	c.gadgets.SetTimeout(timeout)
}

// SetRetryConfig updates the retry configuration across all services.
// Changes are applied synchronously to all registered service configurations.
func (c *ConfigManager) SetRetryConfig(retry acmeconfig.RetryConfig) {
	c.widgets.SetRetryConfig(retry)
	c.gadgets.SetRetryConfig(retry)
}

// GetWidgets returns the configuration for the Widgets service.
// Returns a pointer to the service-specific config for use in API calls.
func (c *ConfigManager) GetWidgets() *acmeconfig.Config {
	return &c.widgets
}

// GetGadgets returns the configuration for the Gadgets service.
// Returns a pointer to the service-specific config for use in API calls.
func (c *ConfigManager) GetGadgets() *acmeconfig.Config {
	return &c.gadgets
}

// GetBaseURL returns the currently configured base URL.
// All services share the same base URL; this reads it from the first service's config.
func (c *ConfigManager) GetBaseURL() string {
	return c.widgets.BaseURL
}
