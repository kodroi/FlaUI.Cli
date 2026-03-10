# flaui

**Give your AI coding agent eyes and hands on any Windows desktop app.**

A stateless CLI that lets AI agents launch apps, find elements, click buttons, type text, and generate test steps — all through structured JSON that agents already know how to parse.

<!-- badges -->
[![Build](https://github.com/kodroi/FlaUI.Cli/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/kodroi/FlaUI.Cli/actions/workflows/build-and-test.yml)
[![NuGet](https://img.shields.io/nuget/v/FlaUI.Cli)](https://www.nuget.org/packages/FlaUI.Cli)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

CLI-first, machine-readable output, built on [FlaUI](https://github.com/FlaUI/FlaUI) and Windows UI Automation (UIA3). Every command is stateless. Every response is JSON. Every element selector is quality-rated.

**Agent interaction example:**

```
You: "Click the Login button in MyApp"

Agent runs:
  $ flaui session new --app "MyApp.exe"
  $ flaui elem find --name "Login" --type "Button"
  $ flaui elem click --id a1b2c3d4

  {"success":true,"message":"Clicked.","elementId":"a1b2c3d4","selectorQuality":"Stable"}
```

---

## Why flaui

- **Desktop automation without code** — your agent calls CLI commands instead of writing C# projects, managing NuGet packages, and compiling test harnesses
- **Selector quality ratings** — every found element is rated `Stable`, `Acceptable`, or `Fragile`, so the agent knows when a selector will break in CI before it actually breaks
- **Test step generation** — the agent records its own interactions and exports reproducible test sequences as structured JSON
- **Selector policy enforcement** — set a quality floor per session; commands fail fast on fragile selectors instead of producing flaky tests
- **Session-based, stateless commands** — launch an app, get a session file, pass it to subsequent commands; no persistent process, no shared memory
- **JSON everything** — pipe output to `jq`, parse it in PowerShell, or let the agent reason about it directly

---

## Installation

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download) or later. Windows only.

```bash
dotnet tool install --global FlaUI.Cli
```

---

## Quick Start

```bash
# Launch an app and create a session
flaui session new --app "C:\Path\To\MyApp.exe" --selector-policy stable

# Explore the UI tree
flaui elem tree --depth 3

# Find an element by AutomationId
flaui elem find --aid "UsernameTextBox"

# Type into it
flaui elem type --id a1b2c3d4 --text "testuser@example.com"

# Find and click the submit button
flaui elem find --name "Submit"
flaui elem click --id e5f6g7h8

# End the session
flaui session end --close-app
```

---

## Commands

All commands accept a global `--session <path>` option to specify the session file. If omitted, the most recent session file in the current directory is used.

### `session new`

Launch an application and create a new session.

```bash
flaui session new --app <path> [--args <args>] [--selector-policy <policy>]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--app` | Yes | Path to the application executable |
| `--args` | No | Arguments to pass to the application |
| `--selector-policy` | No | Minimum selector quality: `stable` (default), `acceptable`, `fragile` |

### `session attach`

Attach to an already running application.

```bash
flaui session attach [--pid <pid>] [--name <name>] [--title <title>]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--pid` | One of three | Process ID to attach to |
| `--name` | One of three | Process name to attach to |
| `--title` | One of three | Window title to attach to |

### `session status`

Check if the session's process is still running and the window is valid.

```bash
flaui session status
```

Returns process alive state, window validity, element count, and recording status.

### `session end`

End the session and optionally close the application.

```bash
flaui session end [--close-app]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--close-app` | No | Close the application when ending session |

### `elem find`

Find an element by one or more properties. Returns an element ID for use in subsequent commands.

```bash
flaui elem find [--aid <id>] [--name <name>] [--type <type>] [--class <class>] [--timeout <ms>]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--aid` | No | AutomationId to find |
| `--name` | No | Element name to find |
| `--type` | No | ControlType to find |
| `--class` | No | ClassName to find |
| `--timeout` | No | Search timeout in milliseconds (default: 10000) |

Response includes `selectorQuality` and `selectorStrategy` so the agent knows how reliable the selector is.

### `elem tree`

Dump the element tree as JSON.

```bash
flaui elem tree [--root <id>] [--depth <n>]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--root` | No | Element ID to use as tree root (default: main window) |
| `--depth` | No | Maximum tree depth (default: 3) |

### `elem props`

Get all properties of an element.

```bash
flaui elem props --id <id>
```

Returns AutomationId, Name, ControlType, ClassName, Bounds, IsEnabled, IsOffscreen, RuntimeId, HelpText, and AcceleratorKey.

### `elem click`

Click an element.

```bash
flaui elem click --id <id> [--double] [--right]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--id` | Yes | Element ID |
| `--double` | No | Double click |
| `--right` | No | Right click |

### `elem type`

Type text into an element.

```bash
flaui elem type --id <id> --text <text>
```

| Option | Required | Description |
|--------|----------|-------------|
| `--id` | Yes | Element ID |
| `--text` | Yes | Text to type |

### `elem set-value`

Set an element's value via the Value pattern.

```bash
flaui elem set-value --id <id> --value <value>
```

| Option | Required | Description |
|--------|----------|-------------|
| `--id` | Yes | Element ID |
| `--value` | Yes | Value to set |

### `elem select`

Select an item in a combo box or list.

```bash
flaui elem select --id <id> --item <item>
```

| Option | Required | Description |
|--------|----------|-------------|
| `--id` | Yes | Element ID |
| `--item` | Yes | Item to select |

### `elem get-value`

Get an element's current value.

```bash
flaui elem get-value --id <id> [--save <name>]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--id` | Yes | Element ID |
| `--save` | No | Save the value to a session variable for later use |

### `elem get-state`

Get an element's toggle/check state, enabled status, and visibility.

```bash
flaui elem get-state --id <id>
```

### `wait`

Wait for an element condition to be met.

```bash
flaui wait --aid <id> --timeout <ms> [--value <value>] [--state <state>]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--aid` | Yes | AutomationId of the element to wait for |
| `--timeout` | Yes | Timeout in milliseconds |
| `--value` | No | Wait until element has this value |
| `--state` | No | Wait for state: `hidden`, `visible`, `enabled` |

### `record start`

Begin recording interactions. All subsequent `elem` commands are recorded as steps.

```bash
flaui record start
```

### `record stop`

Stop recording.

```bash
flaui record stop
```

### `record drop`

Drop the last recorded step.

```bash
flaui record drop
```

### `record keep`

Mark the last recorded step as kept.

```bash
flaui record keep
```

### `record list`

List all recorded steps with their sequence numbers, commands, and targets.

```bash
flaui record list
```

### `record export`

Export recorded steps to a JSON file.

```bash
flaui record export --out <path>
```

| Option | Required | Description |
|--------|----------|-------------|
| `--out` | Yes | Output file path |

### `audit`

Audit selector quality across all elements or recorded steps.

```bash
flaui audit [--window <id>] [--recording] [--out <path>]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--window` | No | Element ID to scope the audit to |
| `--recording` | No | Audit recorded steps instead of live elements |
| `--out` | No | Write audit report to file |

Returns total elements, AutomationId coverage, selector quality distribution, and a list of issues (interactive controls with weak selectors).

---

## Selector Quality System

Every time `flaui` resolves an element, it evaluates the selector and assigns a quality rating:

| Rating | What It Means | Agent Action |
|--------|---------------|--------------|
| **Stable** | Unique `AutomationId` — the gold standard | Use with confidence |
| **Acceptable** | Resolved by `Name` + `ControlType` composite | Use, but note the risk |
| **Fragile** | Resolved by position, index, or volatile property | Flag for review or find a better selector |
| **Unresolvable** | Element not found | Retry, adjust approach, or report failure |

### Selector Policy

```bash
flaui session new --app "MyApp.exe" --selector-policy stable
```

With policy set to `stable`, any `elem find` that resolves below that threshold returns exit code `2` (policy violation). The agent knows immediately that the selector isn't reliable enough for production tests.

---

## Test Step Generation

Record a sequence of interactions, then export them as structured test data:

```bash
flaui session new --app "MyApp.exe"
flaui record start

flaui elem find --name "Username"
flaui elem type --id <id> --text "admin"
flaui elem find --name "Password"
flaui elem type --id <id> --text "secret"
flaui elem find --name "Login"
flaui elem click --id <id>

flaui record stop
flaui record export --out login-steps.json

# Audit the recording for selector quality
flaui audit --recording
```

Every recorded step includes element selectors, quality ratings, timestamps, and parameters. The agent exports them as structured JSON that can be transformed into test cases in any framework.

---

## Exit Codes

| Code | Meaning |
|------|---------|
| `0` | Success |
| `1` | General error |
| `2` | Selector policy violation — element found but selector quality below policy threshold |
| `3` | Element unresolvable — element not found within timeout |

Agents use exit codes for control flow. A non-zero exit tells the agent exactly what went wrong without parsing error messages.

---

## Architecture

```
┌─────────────────────────────┐
│  AI Coding Agent            │
│  (Claude Code, Cursor, etc) │
└─────────┬───────────────────┘
          │ shell commands + JSON
          ▼
┌─────────────────────────────┐
│  flaui CLI                  │
│  stateless, per-command     │
└─────────┬───────────────────┘
          │ UI Automation (UIA3)
          ▼
┌─────────────────────────────┐
│  Windows Desktop App        │
│  (WPF, WinForms, Win32)    │
└─────────────────────────────┘
```

No SDK integration. No library imports. The agent calls `flaui` the same way it calls `git` or `dotnet` — as a CLI tool.

---

## Building from Source

```bash
git clone https://github.com/kodroi/FlaUI.Cli.git
cd FlaUI.Cli
dotnet build src/FlaUI.Cli/FlaUI.Cli.csproj
```

---

## License

MIT
