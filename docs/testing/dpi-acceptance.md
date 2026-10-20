# DPI acceptance testing

Run desktop interaction tests serially on an unlocked Windows desktop. Leave display settings unchanged during an individual operation and keep the fixture visible. These tests create and close their own application windows; they do not change display scaling or the global tool installation.

## Automated evidence

Build the solution in Release, then run the integration test project with the filter `FullyQualifiedName~Dpi|FullyQualifiedName~CoordinateContext`. Tests named `MixedDpiTests` inspect the actual monitors during discovery. An unavailable configuration is skipped with a reason; injected topology tests never count as a real display pass.

The dedicated WPF fixture (`flaui-fixture-dpi`) includes four colored edge markers, a visible grid, a selection status, and a mouse-event counter. Acceptance checks sample marker colors at independently queried UIA positions and read the resulting selection and mouse count. A UIA Invoke action cannot satisfy the mouse-event check.

Keep the test results and operation JSON in an external evidence directory. For visual investigation, retain a window screenshot and the returned physical source rectangle. Do not commit generated images, packages, session files, or test reports.

## Required real-display matrix

| Configuration | Checks |
|---|---|
| 100% | All four markers, native image dimensions, real mouse selection |
| 200% | Same checks without compatibility environment settings |
| 100% + 200% | Move the same live window in both directions without recreating its session |
| Negative desktop origin | Native screenshot and mouse selection on a monitor left of or above primary |
| Spanning differently scaled monitors | Full visible fixture, actual mouse hit, both monitor descriptors |

The suite also exercises transitions across any available different scales. Those results supplement the matrix; a 150% + 200% run does not replace the required 100% + 200% row.

## Distributed launch verification

Prepare a locally packed tool in a temporary package directory and install it into a separate temporary tool directory. Do this before running the tests, never from inside a test that has loaded the executable. Set `FLAUI_DPI_TOOL_PATH` in the test process environment to that directory's `flaui.exe` and run `DpiLaunchModeTests`. Without the path, the installed-tool check is explicitly skipped.

The launch tests compare the apphost, `dotnet` hosting the built DLL, and the isolated installed shim. Each performs capture and real mouse input. Test child environments remove `__COMPAT_LAYER`; they do not change the user's environment. The target fixture is PerMonitorV2-aware, so a single-monitor target's window DPI and monitor percentage must agree. This catches legacy application-manifest compatibility scaling in the monitor metadata.

## Failure behavior

Focused tests exercise unavailable or lost executing contexts, nested restoration, capture-file preservation, and batch recovery. A DPI failure must precede capture or mouse button dispatch, while pattern-only actions remain usable. An optional monitor-scale query failure is reported as unavailable metadata and must not become a guessed value.

## Evidence limitations

Record the exact commit and launch form, physical monitor bounds, configured scales, test outcomes, and unavailable rows with each acceptance run. CI checks only its actual desktop. A green single-display CI run does not establish mixed-DPI coverage. Covered-window capture and competing foreground applications belong to separate work.
