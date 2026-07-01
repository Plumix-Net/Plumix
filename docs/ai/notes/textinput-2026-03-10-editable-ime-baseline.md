# Feature: textinput-2026-03-10-editable-ime-baseline

## Goal

- Add a first editable text baseline in framework widgets through `EditableText` + `TextEditingController`.
- Wire host text input events into focused framework nodes (`FlutterHost -> FocusManager -> FocusNode`).

## Non-Goals

- No rich editing model yet (selection, caret geometry, composition lifecycle).
- No clipboard/shortcut text editing integration in this iteration.

## Context Budget Plan

- Budget: max 8 files in initial read.
- Entry files:
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/MODULE_INDEX.md`
  - `src/Flutter/Widgets/Focus.cs`
  - `src/Flutter/FlutterHost.cs`
  - `src/Flutter/Widgets/Text.cs`
  - `src/Flutter.Tests/FocusTests.cs`
- Expansion trigger:
  - Expand only if new editable widget requires additional render/semantics integration primitives.

## Invariants Impacted

- [x] `docs/ai/INVARIANTS.md` reviewed
- List invariants that this feature touches:
  - Input behavior remains framework-owned and host-independent in `src/Flutter`.
  - Focus remains the gate for keyboard/text input routing.

## Planned Changes

- Files to edit:
  - `src/Flutter/Widgets/Focus.cs`
  - `src/Flutter/FlutterHost.cs`
  - `src/Flutter/Widgets/TextInput.cs`
  - `src/Flutter.Tests/TextInputTests.cs`
  - `src/Sample/Flutter.Net/Demos/General/EditableTextDemoPage.cs`
  - `src/Sample/Flutter.Net/SampleGalleryScreen.cs`
  - `dart_sample/lib/demos/general/editable_text_demo_page.dart`
  - `dart_sample/lib/sample_gallery_screen.dart`
  - `dart_sample/lib/sample_routes.dart`
  - `docs/FRAMEWORK_PLAN.md`
  - `docs/ai/TEST_MATRIX.md`
  - `docs/ai/PARITY_MATRIX.md`
  - `CHANGELOG.md`
- Brief intent per file:
  - `src/Flutter/Widgets/Focus.cs`: add text input callback path on focused node/manager.
  - `src/Flutter/FlutterHost.cs`: dispatch Avalonia `OnTextInput` into framework focus manager.
  - `src/Flutter/Widgets/TextInput.cs`: introduce `TextEditingController` and baseline `EditableText` widget.
  - `src/Flutter.Tests/TextInputTests.cs`: add regression coverage for text input dispatch and editable behavior.
  - Sample files: add mirrored `EditableText` demo route/page in C# and Dart galleries.
  - Tracking docs: reflect shipped M2 progress and remaining gaps.

## Test Plan

- Existing tests to run/update:
  - `src/Flutter.Tests/Flutter.Tests.csproj`
- New tests to add:
  - `src/Flutter.Tests/TextInputTests.cs`

## Sample Parity Plan

- [x] C# sample impact checked
- [x] Dart sample parity checked
- [x] `docs/ai/PARITY_MATRIX.md` updated (if needed)
- Notes:
  - Added a mirrored `EditableText` demo route/page in both sample galleries and updated parity matrix row.

## Docs and Tracking

- [x] `CHANGELOG.md` updated
- [x] `docs/FRAMEWORK_PLAN.md` status updated (if milestone/state changed)
- [x] `docs/ai/TEST_MATRIX.md` updated (if new coverage area was added)

## Done Criteria

- [x] Behavior implemented
- [x] Tests updated and passing
- [x] No invariant violations introduced
- [x] Parity constraints satisfied
