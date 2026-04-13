# Feature: material-2026-04-12-fab-cursor-feedback-closeout

## Goal

- Close the remaining framework-scope `FloatingActionButton` runtime gaps for cursor behavior and feedback dispatch while keeping Flutter-like precedence (`widget -> theme -> defaults`).

## Non-Goals

- Full Flutter `Hero` runtime transition behavior for FAB.
- Platform-native haptic/audio implementation details beyond framework->host dispatch hooks.

## Context Budget Plan

- Budget: max 14 files in initial read.
- Entry files:
  - `src/Flutter.Material/FloatingActionButton.cs`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter.Material/FloatingActionButtonTheme.cs`
  - `src/Flutter/Widgets/MouseCursor.cs`
  - `src/Flutter/FlutterHost.cs`
  - `src/Flutter.Tests/MaterialFloatingActionButtonTests.cs`
- Expansion trigger:
  - Add minimal framework primitives in `src/Flutter/UI` when required to finish FAB parity in one pass.

## Delivery Scope (Required for Control Parity Work)

- Target control:
  - Material `FloatingActionButton` runtime cursor + feedback behavior.
- Completion checklist:
  - [x] API/default values
  - [x] Widget composition order
  - [x] State transitions/interaction states
  - [x] Constraint/layout behavior
  - [x] Paint/visual semantics
  - [x] Focused tests for this control

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Framework behavior remains in framework libraries (`src/Flutter*`) without host-specific widget logic.
  - Flutter parity precedence stays explicit and testable (`widget -> theme -> defaults`).

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/floating_action_button.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/widgets/feedback.dart`
  - `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/services/mouse_cursor.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
  - [x] Paint/visual semantics mapped
- Divergence log:
  - Hero animation runtime remains out of scope until framework hero primitives are added.

## Planned Changes

- Files edited:
  - `src/Flutter.Material/FloatingActionButton.cs`
  - `src/Flutter.Material/FloatingActionButtonTheme.cs`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Flutter/Widgets/MouseCursor.cs`
  - `src/Flutter/UI/Feedback.cs`
  - `src/Flutter/FlutterHost.cs`
  - `src/Flutter.Tests/MaterialFloatingActionButtonTests.cs`
- Brief intent per file:
  - `FloatingActionButton.cs`: resolve cursor/feedback by `widget -> theme -> defaults` and pass runtime values into `MaterialButtonCore`.
  - `FloatingActionButtonTheme.cs`: add FAB theme fields for cursor + feedback.
  - `Buttons.cs`: wire cursor/feedback behavior into shared button runtime pipeline.
  - `MouseCursor.cs`: add framework mouse-cursor manager primitive for hover request stacking.
  - `Feedback.cs`: add framework feedback dispatch primitive.
  - `FlutterHost.cs`: subscribe to framework cursor/feedback channels and map them to host hooks.
  - test file: add focused FAB cursor + feedback regression coverage.

## Test Plan

- Existing tests run:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --no-restore --filter FullyQualifiedName~MaterialFloatingActionButtonTests`
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --no-restore --filter FullyQualifiedName~MaterialButtonsTests`
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --no-restore --filter "FullyQualifiedName~FlutterHostInputTests|FullyQualifiedName~FlutterHostSemanticsTests"`
- New tests added:
  - `FloatingActionButton_DefaultMouseCursor_UsesClickOnHover`
  - `FloatingActionButton_ThemeMouseCursor_UsedWhenWidgetMouseCursorIsNull`
  - `FloatingActionButton_DefaultEnableFeedback_EmitsTapFeedbackOnKeyboardActivation`
  - `FloatingActionButton_EnableFeedbackFalse_SuppressesTapFeedback`
  - `FloatingActionButton_ThemeEnableFeedbackFalse_SuppressesTapFeedback`
- Parity-risk scenarios covered:
  - Hover cursor defaults and theme fallback behavior.
  - Keyboard activation feedback behavior with widget/theme suppression.

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` update not needed (no sample route/page behavior change)

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated
- [x] `docs/ai/TEST_MATRIX.md` updated

## Done Criteria

- [x] One full control (or explicitly scoped feature) is closed end-to-end
- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
- [x] Remaining parity gaps documented with blocker + next action
