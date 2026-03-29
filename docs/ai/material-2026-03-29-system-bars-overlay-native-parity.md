# Feature: material-2026-03-29-system-bars-overlay-native-parity

## Goal

- Remove gray Android system-bar visuals and add framework-level runtime API to control status/navigation bar styling from C# code.

## Non-Goals

- No full Flutter `AnnotatedRegion<SystemUiOverlayStyle>` parity in this iteration.
- No route/module changes in sample gallery.
- No iOS-specific system-bar behavior changes.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `AGENTS.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter/FlutterHost.cs`
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Sample/Flutter.Net.Android/Resources/values/styles.xml`
  - `src/Sample/Flutter.Net.Android/Resources/values-v31/styles.xml`
- Expansion trigger:
  - Expand only if host-level application of system-bar styles cannot be completed via framework + Android theme defaults.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Runtime behavior remains in framework/host layers (`src/Flutter`, `src/Flutter.Material`, sample Android host resources).
  - Material app shell behavior remains Flutter-first (`AppBar` precedence preserved).
  - Platform-specific behavior is isolated to host integration paths.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/services/system_chrome.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/app_bar.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
- Divergence log (only if needed):
  - Separate status/navigation color application is implemented as Android-only best-effort reflection path on top of Avalonia's shared `SystemBarColor` API.

## Planned Changes

- Files to edit:
  - `src/Flutter/UI/SystemChrome.cs`
  - `src/Flutter/FlutterHost.cs`
  - `src/Flutter.Material/Scaffold.cs`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Tests/MaterialScaffoldTests.cs`
  - `src/Sample/Flutter.Net.Android/Resources/values/styles.xml`
  - `src/Sample/Flutter.Net.Android/Resources/values-v31/styles.xml`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `SystemChrome.cs`: add runtime API for system overlay style state.
  - `FlutterHost.cs`: consume and apply overlay style to platform system bars, and toggle edge-to-edge adaptively based on overlay transparency.
  - `Scaffold.cs`/`ThemeData.cs`: add app-bar/theme system overlay style precedence and default behavior.
  - `MaterialScaffoldTests.cs`: add precedence regression coverage.
  - Android styles: remove gray defaults and enforce transparent/light baseline.
  - docs/changelog: record delivered behavior and coverage.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter "MaterialScaffoldTests|FlutterHostInputTests"`
  - `dotnet build src/Sample/Flutter.Net/Flutter.Net.csproj -c Debug`
  - `dotnet build src/Sample/Flutter.Net.Desktop/Flutter.Net.Desktop.csproj -c Debug`
- New tests to add:
  - `AppBar_SystemOverlayStyle_DefaultsFromThemeAppBarTheme`
  - `AppBar_SystemOverlayStyle_WidgetValue_OverridesThemeAppBarTheme`
- Parity-risk scenarios covered:
  - App-bar overlay style precedence and runtime propagation through `SystemChrome`.
  - Android host baseline no longer starts with gray status/navigation backgrounds.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` update not required (no sample route/module structure change)

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
