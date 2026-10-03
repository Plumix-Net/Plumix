namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/date.dart

/// <summary>
/// Signature for predicating dates for enabled date selections.
/// </summary>
/// <remarks>
/// See <c>ShowDatePicker</c> and <c>ShowCupertinoModalPopup</c>, which have a
/// <see cref="SelectableDayPredicate"/> parameter used to specify allowable days in the date picker.
/// </remarks>
public delegate bool SelectableDayPredicate(DateTime day);
