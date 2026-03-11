# flaui

**Let your AI agent navigate your app, verify its own changes, and write the tests.**

AI coding agents write your code. But when that code has a UI, the agent can't see what it built, can't check if the button works, and can't write a test for it. `flaui` closes that gap.

<!-- badges -->
[![Build](https://github.com/kodroi/FlaUI.Cli/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/kodroi/FlaUI.Cli/actions/workflows/build-and-test.yml)
[![NuGet](https://img.shields.io/nuget/v/FlaUI.Tool)](https://www.nuget.org/packages/FlaUI.Tool)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A stateless CLI built on [FlaUI](https://github.com/FlaUI/FlaUI) and Windows UI Automation (UIA3). Every command returns structured JSON. Every element selector is quality-rated. The agent drives it the same way it drives `git` or `dotnet` — from the terminal.

**The workflow:**

```
1. Agent writes code that changes a WPF form
2. Agent launches the app:          flaui session new --app "MyApp.exe"
3. Agent navigates to the form:     flaui elem find --name "Customer Details"
4. Agent verifies its changes work: flaui elem find --aid "EmailField"
                                    flaui elem type --id a1b2 --text "test@co.com"
                                    flaui elem find --name "Save"
                                    flaui elem click --id c3d4
5. Agent uses menus and keyboard:   flaui elem menu --path "File > Save As"
                                    flaui elem keys --keys "ctrl+s"
6. Agent reads the result:          flaui elem get-value --id a1b2
7. Agent captures visual proof:     flaui screenshot --output form-state.png
8. Agent exports the steps as test: flaui record export --out customer-form-test.json
```

---

## Why flaui

- **Agents can verify their own UI changes** — the agent builds a feature, launches the app, and checks that buttons, fields, and workflows actually work
- **Agents generate tests from real interactions** — every click, type, and navigation is recorded and exported as structured test steps; the agent turns them into test cases
- **Selector quality keeps tests reliable** — every element is rated `Stable`, `Acceptable`, or `Fragile`; the agent knows which selectors will survive the next refactor
- **Selector policies fail fast** — set a quality floor per session; fragile selectors return exit code `2` instead of silently producing a flaky test
- **Stateless and JSON-native** — no persistent process, no shared memory; agents parse JSON natively, so there's zero glue code between the agent and the tool

---

## Installation

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download) or later. Windows only.

```bash
dotnet tool install --global FlaUI.Tool
```

---

## Quick Start

```bash
# Agent launches the app it just modified
flaui session new --app "C:\Path\To\MyApp.exe" --selector-policy stable

# Agent explores the UI to understand the layout
flaui elem tree --depth 3

# Agent finds the field it added in the last commit
flaui elem find --aid "UsernameTextBox"

# Agent types into it to verify it works
flaui elem type --id a1b2c3d4 --text "testuser@example.com"

# Agent clicks submit to test the full flow
flaui elem find --name "Submit"
flaui elem click --id e5f6g7h8

# Agent reads back the result to confirm
flaui elem get-value --id a1b2c3d4

# Done — agent closes the app
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
flaui session end [--close-app] [--force]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--close-app` | No | Close the application when ending session |
| `--force` | No | Force-kill the application process (use with `--close-app` for unresponsive apps) |

### `elem find`

Find an element by one or more properties. Returns an element ID for use in subsequent commands.

```bash
flaui elem find [--aid <id>] [--name <name>] [--type <type>] [--class <class>] [--timeout <ms>] [--window <handle>] [--policy <policy>]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--aid` | No | AutomationId to find |
| `--name` | No | Element name to find |
| `--type` | No | ControlType to find |
| `--class` | No | ClassName to find |
| `--timeout` | No | Search timeout in milliseconds (default: 10000) |
| `--window` | No | Window handle (hex) to search in instead of the main window |
| `--policy` | No | Override session selector policy for this command (`stable`, `acceptable`, `fragile`) |

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

### `elem keys`

Send keyboard input (key combinations or single keys).

```bash
flaui elem keys --keys <keys> [--id <id>] [--window <handle>]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--keys` | Yes | Key combination (e.g. `ctrl+shift+s`, `tab`, `alt+f4`) |
| `--id` | No | Element ID to focus before sending keys |
| `--window` | No | Window handle (hex) to target |

Supported keys: `ctrl`, `alt`, `shift`, `tab`, `escape`/`esc`, `enter`/`return`, `space`, `delete`/`del`, `backspace`, `up`/`down`/`left`/`right`, `home`, `end`, `pageup`, `pagedown`, `f1`-`f24`, `a`-`z`, `0`-`9`.

### `elem menu`

Navigate and click a menu item by path.

```bash
flaui elem menu --path <path> [--window <handle>]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--path` | Yes | Menu path with `>` separators (e.g. `"File > Save As"`) |
| `--window` | No | Window handle (hex) to target |

### `elem scroll-into-view`

Scroll an element into view using the UIA ScrollItem pattern.

```bash
flaui elem scroll-into-view --id <id> [--window <handle>]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--id` | Yes | Element ID |
| `--window` | No | Window handle (hex) to target |

Returns `scrolled: true` if the element supported the ScrollItem pattern and was scrolled, or `scrolled: false` if the pattern is not supported. This is also called automatically by `elem click`, `elem type`, and other interaction commands.

### `window list`

List all top-level windows for the attached application.

```bash
flaui window list
```

Returns an array of windows with handle (hex), title, isModal, className, and bounds.

### `window focus`

Bring a window to the foreground.

```bash
flaui window focus --handle <handle>
```

| Option | Required | Description |
|--------|----------|-------------|
| `--handle` | Yes | Window handle as hex string (from `window list`) |

### `window close`

Close a window by handle or title.

```bash
flaui window close [--handle <handle>] [--title <title>] [--force]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--handle` | One of two | Window handle as hex string |
| `--title` | One of two | Window title (partial, case-insensitive) |
| `--force` | No | Force-kill the window's process if graceful close fails |

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

### `screenshot`

Capture a screenshot of a window or element and save it to a file.

```bash
flaui screenshot --output <path> [--id <id>] [--window <handle>]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--output` | Yes | Output file path. Format inferred from extension (`.png`, `.bmp`, `.jpg`) |
| `--id` | No | Element ID — captures that element's bounding rectangle |
| `--window` | No | Window handle (hex) — captures that window instead of the main window |

If neither `--id` nor `--window` is given, captures the main window from the session.

**Response:**

```json
{
  "success": true,
  "message": "Screenshot saved.",
  "outputPath": "C:/absolute/path/to/screenshot.png",
  "width": 1024,
  "height": 768
}
```

### `batch`

Execute multiple commands in a single session (one process attach, shared element state).

```bash
flaui batch --file <path> [--continue-on-error]
flaui batch --steps '<json>' [--continue-on-error]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--file` | One of two | Path to a JSON file with batch steps |
| `--steps` | One of two | Inline JSON string with batch steps |
| `--continue-on-error` | No | Continue executing remaining steps after a failure |

**Input format:**

```json
{
  "steps": [
    {"cmd": "elem find", "args": {"aid": "SubmitButton"}},
    {"cmd": "elem click", "args": {"id": "$prev.elementId"}},
    {"cmd": "elem keys", "args": {"keys": "ctrl+s"}},
    {"cmd": "elem menu", "args": {"path": "File > Save As"}}
  ]
}
```

Use `$prev.field` to reference the previous step's result, or `$steps[N].field` to reference step N's result.

Supported commands: `elem find`, `elem click`, `elem type`, `elem select`, `elem set-value`, `elem get-value`, `elem get-state`, `elem keys`, `elem menu`, `elem scroll-into-view`, `window list`, `window focus`, `window close`, `screenshot`.

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

## Test Generation from Real Interactions

The agent doesn't write tests from imagination. It writes code, runs the app, interacts with it, and turns those real interactions into tests.

```bash
# Agent finished implementing a login feature — now it verifies and records
flaui session new --app "MyApp.exe"
flaui record start

# Agent navigates the UI it just built
flaui elem find --name "Username"
flaui elem type --id <id> --text "admin"
flaui elem find --name "Password"
flaui elem type --id <id> --text "secret"
flaui elem find --name "Login"
flaui elem click --id <id>

# Agent checks the result
flaui elem find --aid "WelcomeLabel"
flaui elem get-value --id <id>

# Agent exports the verified steps as a test definition
flaui record stop
flaui record export --out login-test.json

# Agent audits selector quality to ensure test stability
flaui audit --recording
```

Every recorded step includes element selectors, quality ratings, timestamps, and parameters. The agent uses this structured JSON to generate test cases in whatever framework the project uses — NUnit, xUnit, MSTest, or a custom harness.

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

## Known Limitations & Workarounds

### 1. Elements inside collapsed WPF Expanders are not found

`elem find` returns "not found" for elements inside collapsed Expanders. This is correct UIA behavior — collapsed containers don't expose their children to the automation tree.

**Workaround:** First expand the Expander (find it, then `elem click` to expand), then search for child elements.

### 2. `elem tree` returns no children for WPF ComboBox

WPF ComboBoxes use virtualized item panels. Items only appear in the UIA tree when the dropdown is open and items are scrolled into view.

**Workaround:** Use `elem select --id <id> --item "ItemName"` to select items by name — this command expands the dropdown internally. To list items, open the dropdown first with `elem click`, then run `elem tree`.

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
