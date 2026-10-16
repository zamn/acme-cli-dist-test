---
name: acme-attest-custom-commands
description: How to author custom commands for the acme-attest CLI using the co-generated SDK.
---

# Custom Commands for `acme-attest`

## Overview

The `acme-attest` CLI supports user-authored custom commands that are
compiled into the binary alongside the auto-generated API commands.
Custom commands get a fully-wired SDK client that inherits the CLI's
auth, retries, TLS, base URL, and global headers — zero configuration required.

## Architecture

```
cli/acme-attest/custom.rs    ← Your command handlers (protected by .fernignore)
cli/acme-attest/sdk.rs       ← Generated bridge: client() + block_on()
cli/acme-attest/main.rs      ← Generated entrypoint (calls custom::register)
acme-attest-sdk/             ← Co-generated typed SDK crate
acme-attest-types/           ← Co-generated typed model crate
```

## Adding a Custom Command

### 1. Edit `cli/acme-attest/custom.rs`

This file is protected by `.fernignore` — `fern generate` will never
overwrite it. Register commands in the `register()` function:

```rust
use acme_attest_sdk::api::*;

pub fn register(app: CliApp) -> CliApp {
    let app = app.command(
        clap::Command::new("list-widgets")
            .about("Run widgets list-widgets")
        ,
        |matches, ctx| {
            let client = super::sdk::client(ctx);
            let result = super::sdk::block_on(
                client.widgets.list_widgets(),
            )?;
            println!("{}", serde_json::to_string_pretty(&result).unwrap());
            Ok(())
        },
    );
    app
}
```

Then build and test:

```bash
cargo build
acme-attest list-widgets
```

### 2. Available SDK Clients

The `super::sdk::client(ctx)` call returns a `acme_attest_sdk::api::Client`
with the following sub-clients:

| Field            | Type                                  | Description        |
| ---------------- | ------------------------------------- | ------------------ |
| `client.widgets` | `acme_attest_sdk::api::WidgetsClient` | widgets operations |

### 3. Key Patterns

**Get the SDK client** (execution-sharing, fully authenticated):

```rust
let client = super::sdk::client(ctx);
```

**Run an async SDK call from a sync handler:**

```rust
let result = super::sdk::block_on(
    client.some_resource.some_method(args),
)?;
```

**Use typed models for request/response serialization:**

```rust
use acme_attest_sdk::api::*;
```

## Regeneration Safety

| File                        | Regenerated? | Notes                           |
| --------------------------- | ------------ | ------------------------------- |
| `cli/acme-attest/custom.rs` | **No**       | Protected by `.fernignore`      |
| `cli/acme-attest/sdk.rs`    | Yes          | Bridges AppContext → SDK client |
| `cli/acme-attest/main.rs`   | Yes          | Calls `custom::register(app)`   |
| `acme-attest-sdk/`          | Yes          | Co-generated typed SDK crate    |
| `acme-attest-types/`        | Yes          | Co-generated typed models       |

After running `fern generate`, your `custom.rs` is preserved. All
generated code (SDK, types, glue, main.rs) is updated to match the
latest API spec. If the SDK surface changes (renamed methods, new
sub-clients), update your `custom.rs` to match.

## Build & Test

```bash
# Build the CLI (includes custom commands)
cargo build

# Run your custom command
acme-attest <your-command> [args]

# Run with verbose output for debugging
RUST_LOG=debug acme-attest <your-command> [args]
```
