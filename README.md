# flaui

**Automate any Windows desktop app from the command line. No code required.**

A stateless CLI tool that turns Windows UI Automation into simple, scriptable commands with structured JSON output.

---

## The Problem

Automating Windows desktop applications usually means writing C# projects, managing NuGet packages, compiling code, and dealing with flaky element selectors that break silently. For CI pipelines, test scripts, or quick automation tasks, this overhead kills productivity.

## The Solution

`flaui` is a .NET global tool that wraps [FlaUI](https://github.com/FlaUI/FlaUI) into a zero-code CLI. Every command is stateless, every response is JSON, and every element selector is quality-rated so you know before your pipeline breaks.

- **Stateless sessions** — launch an app, get a session file, pass it to subsequent commands
- **JSON everything** — pipe output to `jq`, parse it in PowerShell, feed it to your test harness
- **Selector quality ratings** — every found element is rated `Stable`, `Acceptable`, or `Fragile`
- **Built-in recording** — record interaction sequences and export them for replay

---

## Quick Start

```bash
# Install
dotnet tool install --global FlaUI.Cli

# Launch an app and create a session
flaui session new --app "C:\Windows\System32\notepad.exe"

# Find an element
flaui elem find --aid "RichEditBox"

# Type into it
flaui elem type --id <element-id> --text "Hello from the CLI"

# Click a menu item
flaui elem find --name "File"
flaui elem click --id <element-id>
```

Every command outputs structured JSON:

```json
{
  "success": true,
  "message": "Element found.",
  "elementId": "a1b2c3d4",
  "automationId": "RichEditBox",
  "controlType": "Document",
  "selectorQuality": "Stable",
  "selectorStrategy": "AutomationId"
}
```

---

## Installation

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download) or later (Windows only).

```bash
dotnet tool install --global FlaUI.Cli
```

---

## Command Reference

### Global Options

| Option | Description |
|--------|-------------|
| `--session <path>` | Path to session file (auto-detected if omitted) |

### `session` — Manage application sessions

| Command | Description |
|---------|-------------|
| `session new --app <path> [--args <args>] [--selector-policy <policy>]` | Launch app and create session |
| `session attach --pid <pid>` | Attach to a running process |
| `session status` | Check if the session's process is still running |
| `session end` | End the session and optionally close the app |

The `--selector-policy` flag sets the minimum selector quality for the session: `stable` (default), `acceptable`, or `fragile`.

### `elem` — Find and interact with elements

| Command | Description |
|---------|-------------|
| `elem find --aid <id> \| --name <n> \| --type <t> \| --class <c> [--timeout <ms>]` | Find an element |
| `elem tree [--depth <n>]` | Display the element tree |
| `elem props --id <id>` | Get all properties of an element |
| `elem click --id <id> [--double] [--right]` | Click an element |
| `elem type --id <id> --text <text>` | Type text into an element |
| `elem set-value --id <id> --value <value>` | Set an element's value |
| `elem select --id <id> --item <item>` | Select an option |
| `elem get-value --id <id>` | Get an element's current value |
| `elem get-state --id <id>` | Get an element's toggle/check state |

### `wait` — Wait for conditions

```bash
flaui wait --id <id> --condition <condition> [--timeout <ms>]
```

### `record` — Record interaction sequences

| Command | Description |
|---------|-------------|
| `record start` | Begin recording |
| `record stop` | Stop recording |
| `record drop` | Drop the last recorded step |
| `record keep` | Mark the last step as kept |
| `record list` | List all recorded steps |
| `record export [--format <format>]` | Export the recording |

### `audit` — Audit selector quality

```bash
flaui audit
```

Analyzes all elements in the current session and reports selector quality ratings.

---

## Selector Quality System

Every time `flaui` resolves an element, it evaluates the selector and assigns a quality rating:

| Rating | Meaning |
|--------|---------|
| **Stable** | Element has a unique `AutomationId` — the gold standard |
| **Acceptable** | Resolved by `Name` + `ControlType` or similar composite selector |
| **Fragile** | Resolved by position, index, or other volatile properties |
| **Unresolvable** | Element could not be found |

### Selector Policy

Set a policy per session to enforce minimum quality:

```bash
flaui session new --app "MyApp.exe" --selector-policy stable
```

If a `find` command resolves an element below the policy threshold, it returns exit code `2` (policy violation) instead of success. This lets CI pipelines catch fragile selectors before they cause flaky tests.

---

## Recording Workflow

Record a sequence of interactions, then export them:

```bash
# Start a session and begin recording
flaui session new --app "MyApp.exe"
flaui record start

# Interact with the application
flaui elem find --name "Username"
flaui elem type --id <id> --text "admin"
flaui elem find --name "Password"
flaui elem type --id <id> --text "secret"
flaui elem find --name "Login"
flaui elem click --id <id>

# Review and export
flaui record list
flaui record stop
flaui record export
```

---

## Exit Codes

| Code | Meaning |
|------|---------|
| `0` | Success |
| `1` | General error |
| `2` | Selector policy violation |
| `3` | Element unresolvable |

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
