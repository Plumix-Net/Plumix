# Feature: material-2026-03-29-button-color-typography-parity

## Goal

- Align C# Material theme/button defaults with Flutter Material 3 so sample color and typography output is as close as possible to Dart reference for `Material buttons` flows.

## Non-Goals

- No new Material controls/routes.
- No elevation/shadow rendering rewrite in this iteration.
- No host SDK/toolchain fixes for Android/iOS build environment.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Sample/Flutter.Net/MaterialButtonsDemoPage.cs`
  - `dart_sample/lib/material_buttons_demo_page.dart`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
- Expansion trigger:
  - Expand only when Flutter source defaults/tokens needed exact verification (`theme_data.dart`, `text_button.dart`, `elevated_button.dart`, `outlined_button.dart`, `filled_button.dart`, `typography.dart`).

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- [x] `docs/ai/PORTING_MODE.md` reviewed (for Dart-to-C# control/widget ports)
- List invariants that this feature touches:
  - Material behavior remains framework-owned (`src/Flutter.Material`), not moved to host controls.
  - Parity-first mapping from Flutter Dart defaults is kept for control defaults and state handling.
  - Sample parity between `src/Sample/Flutter.Net` and `dart_sample` is preserved.

## Dart Reference Mapping (Required for Ports)

- Flutter/Dart source files used as source of truth:
  - `flutter/packages/flutter/lib/src/material/theme_data.dart`
  - `flutter/packages/flutter/lib/src/material/typography.dart`
  - `flutter/packages/flutter/lib/src/material/text_button.dart`
  - `flutter/packages/flutter/lib/src/material/elevated_button.dart`
  - `flutter/packages/flutter/lib/src/material/outlined_button.dart`
  - `flutter/packages/flutter/lib/src/material/filled_button.dart`
  - `dart_sample/lib/material_buttons_demo_page.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
- Parity mapping checklist:
  - [x] API/default values mapped
  - [x] Widget composition order mapped
  - [x] State transitions/interaction states mapped
  - [x] Constraint/layout behavior mapped
- Divergence log (only if needed):
  - none

## Planned Changes

- Files to edit:
  - `src/Flutter.Material/ThemeData.cs`
  - `src/Flutter.Material/Buttons.cs`
  - `src/Sample/Flutter.Net/MaterialButtonsDemoPage.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `src/Flutter.Tests/MaterialButtonsTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `ThemeData.cs`: update M3 light tokens + typography defaults (`titleLarge`, `labelLarge`) and platform font fallback policy.
  - `Buttons.cs`: apply `labelLarge` defaults to Material buttons, align outlined focused border token path, align M3 non-text button default padding (`24,0`), enforce Flutter behavior where textStyle color does not override foreground color, and align `_InputPadding`-like tap-target layout/hit-test behavior.
  - sample files: align literal sample colors/string formatting with Dart (`black54`, `blueGrey`, `true/false`).
  - tests/docs/changelog: add regression coverage and record shipped parity step.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter "FullyQualifiedName~MaterialButtonsTests|FullyQualifiedName~MaterialScaffoldTests|FullyQualifiedName~TextWidgetTests"`
- New tests to add:
  - `ThemeData_Light_UsesFlutterMaterial3ColorDefaults`
  - `TextButton_DefaultTextStyle_UsesLabelLargeTypography`
  - `TextButton_TextStyleColor_DoesNotOverrideForegroundColor`
  - `ElevatedButton_DefaultPadding_UsesHorizontal24AndZeroVertical`
  - `OutlinedButton_DefaultPadding_UsesHorizontal24AndZeroVertical`
  - `FilledButton_DefaultPadding_UsesHorizontal24AndZeroVertical`
  - `TextButton_TapTargetPadding_RedirectsHitTestInPaddedAreaToChildCenter`
- Parity-risk scenarios covered:
  - M3 token defaults for primary/surface/secondary tones,
  - default button typography metrics (`labelLarge`),
  - foreground precedence over `textStyle.color`,
  - tap-target padded-area hit-testing (redirect to visual-child center, matching Flutter `_InputPadding` semantics).

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
