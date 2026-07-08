# Feature: TimePickerDialog + DateRangePickerDialog

## Goal

- Port the paired time/range dialogs from Flutter with matching value contracts, entry modes, interaction states, M2/M3 layout/theme behavior, paint, semantics, and typed results.

## Delivery Scope

- [x] `TimeOfDay`, 12/24-hour localization, dial drag/tap, validated input, and `TimePickerTheme`
- [x] Lazy multi-month range selection, predicates, connected endpoint paint, validated input, and range theme fields
- [x] Dialog-only entry modes, mode switching, portrait/landscape sizing, semantics, and typed route helpers
- [x] Focused tests and mirrored C#/Dart demo

## Dart Reference Mapping

- `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/time.dart`
- `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/time_picker.dart`
- `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/time_picker_theme.dart`
- `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/date_picker.dart`
- `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/date_picker_theme.dart`
- Service-level restoration, IME hint, locale/foldable, haptic, and specialized keyboard-action gaps are recorded only in `docs/ai/DIVERGENCES.md`.

## Validation

- `src/Plumix.Tests/MaterialDatePickerTests.cs`
- `dotnet test src/Plumix.Tests/Plumix.Tests.csproj`
- `dotnet build src/Plumix.sln -c Debug` (all projects except the documented local iOS SDK/Xcode version mismatch)
- C# and Dart picker demos are structurally paired and tracked in `docs/ai/PARITY_MATRIX.md`.
