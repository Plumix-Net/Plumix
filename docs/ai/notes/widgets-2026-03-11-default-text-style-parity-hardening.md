# Feature: widgets-2026-03-11-default-text-style-parity-hardening

## Goal

- Reduce visual text-rendering drift between C# sample and Dart sample by matching Flutter-style inherited text defaults (`DefaultTextStyle`) in framework `Text`.

## Non-Goals

- No full Material theming system (`ThemeData`, `TextTheme`, `Scaffold`, etc.) in this iteration.
- No pixel-perfect guarantee across every host/font backend.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter/Widgets/Text.cs`
  - `src/Flutter/RenderParagraph.cs`
  - `src/Sample/Flutter.Net/CounterApp.cs`
  - `dart_sample/lib/counter_app.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `src/Flutter.Tests/TextWidgetTests.cs`
- Expansion trigger:
  - Expand only if inherited-style propagation cannot be validated via `TextWidgetTests`.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Text behavior remains framework-owned (`src/Flutter`) with no Avalonia-control theming logic migration.
  - Sample parity remains C#/Dart aligned at route/module level while improving runtime visual parity.

## Planned Changes

- Files to edit:
  - `src/Flutter/Widgets/DefaultTextStyle.cs`
  - `src/Flutter/Widgets/Text.cs`
  - `src/Sample/Flutter.Net/CounterApp.cs`
  - `src/Flutter.Tests/TextWidgetTests.cs`
  - `CHANGELOG.md`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/PARITY_MATRIX.md`
  - `docs/ai/TEST_MATRIX.md`
- Brief intent per file:
  - `DefaultTextStyle.cs`: add inherited text-style primitive and fallback defaults.
  - `Text.cs`: resolve final render typography from inherited defaults + local overrides.
  - `CounterApp.cs`: provide Material-like root text defaults for C# sample.
  - `TextWidgetTests.cs`: lock inherited-style/override behavior with regression coverage.
  - docs/changelog: record state and parity rationale.

## Test Plan

- Existing tests to run/update:
  - `dotnet test src/Flutter.Tests/Flutter.Tests.csproj -c Debug --filter TextWidgetTests`
- New tests to add:
  - Extend `src/Flutter.Tests/TextWidgetTests.cs` with inherited `DefaultTextStyle` case.

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
