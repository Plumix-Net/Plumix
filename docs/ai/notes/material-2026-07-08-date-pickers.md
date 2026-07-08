# Feature: CalendarDatePicker + YearPicker

## Goal

- Port the paired calendar controls from Flutter with matching public contracts, state, layout, paint, theme, localization, and semantics behavior.

## Delivery Scope

- [x] API/default values and calendar delegate/date utilities
- [x] Widget composition and day/year/month state transitions
- [x] M2/M3 layout, state paint, focus, keyboard, and semantics
- [x] Focused tests and mirrored C#/Dart demo

## Dart Reference Mapping

- `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/calendar_date_picker.dart`
- `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/date.dart`
- `/Users/egorozh/Documents/flutter/flutter/packages/flutter/lib/src/material/date_picker_theme.dart`
- The active lazy-page limitation is recorded only in `docs/ai/DIVERGENCES.md`.

## Validation

- `src/Plumix.Tests/MaterialDatePickerTests.cs`
- `dotnet test src/Plumix.Tests/Plumix.Tests.csproj`
- C# and Dart sample pages are kept structurally paired and tracked in `docs/ai/PARITY_MATRIX.md`.
