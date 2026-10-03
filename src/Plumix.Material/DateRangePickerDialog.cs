using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/date_picker.dart

public static partial class MaterialDatePickers
{
    /// <summary>Shows a full screen modal dialog containing a Material Design date range picker.</summary>
    /// <remarks>
    /// Dart's top-level <c>showDateRangePicker</c>. The returned <see cref="Task"/> resolves to the
    /// <see cref="DateTimeRange{T}"/> selected by the user when the user saves their selection. If the
    /// user cancels the dialog, null is returned. Dart's function is <c>async</c>, so a failed assertion
    /// surfaces as a faulted task rather than a synchronous throw.
    /// <para>
    /// Using this method will not enable state restoration for the date range picker. In order to
    /// enable state restoration for a date range picker, use <see cref="Navigator.RestorablePush"/>
    /// with <see cref="DateRangePickerDialog"/>.
    /// </para>
    /// </remarks>
    public static Task<DateTimeRange<DateTime>?> ShowDateRangePicker(
        BuildContext context,
        DateTime firstDate,
        DateTime lastDate,
        DateTimeRange<DateTime>? initialDateRange = null,
        DateTime? currentDate = null,
        DatePickerEntryMode initialEntryMode = DatePickerEntryMode.Calendar,
        string? helpText = null,
        string? cancelText = null,
        string? confirmText = null,
        string? saveText = null,
        string? errorFormatText = null,
        string? errorInvalidText = null,
        string? errorInvalidRangeText = null,
        string? fieldStartHintText = null,
        string? fieldEndHintText = null,
        string? fieldStartLabelText = null,
        string? fieldEndLabelText = null,
        Locale? locale = null,
        bool barrierDismissible = true,
        Color? barrierColor = null,
        string? barrierLabel = null,
        bool useRootNavigator = true,
        RouteSettings? routeSettings = null,
        TextDirection? textDirection = null,
        TransitionBuilder? builder = null,
        Point? anchorPoint = null,
        TextInputType? keyboardType = null,
        Icon? switchToInputEntryModeIcon = null,
        Icon? switchToCalendarEntryModeIcon = null,
        SelectableDayForRangePredicate? selectableDayPredicate = null,
        CalendarDelegate<DateTime>? calendarDelegate = null)
    {
        CalendarDelegate<DateTime> effectiveCalendarDelegate = calendarDelegate ?? GregorianCalendarDelegate.Instance;
        try
        {
            initialDateRange = initialDateRange == null
                ? null
                : effectiveCalendarDelegate.DatesOnly(initialDateRange);
            firstDate = effectiveCalendarDelegate.DateOnly(firstDate);
            lastDate = effectiveCalendarDelegate.DateOnly(lastDate);
            DebugAssertions.Assert(
                !(lastDate < firstDate),
                $"lastDate {DateUtils.DartToString(lastDate)} must be on or after firstDate "
                + $"{DateUtils.DartToString(firstDate)}.");
            DebugAssertions.Assert(
                initialDateRange == null || !(initialDateRange.Start < firstDate),
                $"initialDateRange's start date must be on or after firstDate {DateUtils.DartToString(firstDate)}.");
            DebugAssertions.Assert(
                initialDateRange == null || !(initialDateRange.End < firstDate),
                $"initialDateRange's end date must be on or after firstDate {DateUtils.DartToString(firstDate)}.");
            DebugAssertions.Assert(
                initialDateRange == null || !(initialDateRange.Start > lastDate),
                $"initialDateRange's start date must be on or before lastDate {DateUtils.DartToString(lastDate)}.");
            DebugAssertions.Assert(
                initialDateRange == null || !(initialDateRange.End > lastDate),
                $"initialDateRange's end date must be on or before lastDate {DateUtils.DartToString(lastDate)}.");
            DebugAssertions.Assert(
                initialDateRange == null
                || selectableDayPredicate == null
                || selectableDayPredicate(initialDateRange.Start, initialDateRange.Start, initialDateRange.End),
                "initialDateRange's start date must be selectable.");
            DebugAssertions.Assert(
                initialDateRange == null
                || selectableDayPredicate == null
                || selectableDayPredicate(initialDateRange.End, initialDateRange.Start, initialDateRange.End),
                "initialDateRange's end date must be selectable.");
            currentDate = effectiveCalendarDelegate.DateOnly(currentDate ?? effectiveCalendarDelegate.Now());
            DebugAssertions.Assert(MaterialDebug.DebugCheckHasMaterialLocalizations(context));
        }
        catch (AssertionError error)
        {
            return Task.FromException<DateTimeRange<DateTime>?>(error);
        }

        Widget dialog = new DateRangePickerDialog(
            initialDateRange: initialDateRange,
            firstDate: firstDate,
            lastDate: lastDate,
            currentDate: currentDate,
            selectableDayPredicate: selectableDayPredicate,
            initialEntryMode: initialEntryMode,
            helpText: helpText,
            cancelText: cancelText,
            confirmText: confirmText,
            saveText: saveText,
            errorFormatText: errorFormatText,
            errorInvalidText: errorInvalidText,
            errorInvalidRangeText: errorInvalidRangeText,
            fieldStartHintText: fieldStartHintText,
            fieldEndHintText: fieldEndHintText,
            fieldStartLabelText: fieldStartLabelText,
            fieldEndLabelText: fieldEndLabelText,
            keyboardType: keyboardType ?? TextInputType.Datetime,
            switchToInputEntryModeIcon: switchToInputEntryModeIcon,
            switchToCalendarEntryModeIcon: switchToCalendarEntryModeIcon,
            calendarDelegate: effectiveCalendarDelegate);

        if (textDirection != null)
        {
            dialog = new Directionality(textDirection.Value, dialog);
        }

        if (locale != null)
        {
            dialog = Localizations.Override(context, dialog, locale: locale);
        }

        Widget capturedDialog = dialog;
        return MaterialDialogs.ShowDialog<DateTimeRange<DateTime>>(
            context,
            routeContext => builder == null ? capturedDialog : builder(routeContext, capturedDialog),
            barrierDismissible: barrierDismissible,
            barrierColor: barrierColor,
            barrierLabel: barrierLabel,
            useRootNavigator: useRootNavigator,
            routeSettings: routeSettings,
            useSafeArea: false,
            anchorPoint: anchorPoint);
    }

    // Dart's private top-level `_formatRangeStartDate`.
    internal static string FormatRangeStartDate(
        MaterialLocalizations localizations,
        CalendarDelegate<DateTime> calendarDelegate,
        DateTime? startDate,
        DateTime? endDate)
    {
        return startDate == null
            ? localizations.DateRangeStartLabel
            : (endDate == null || startDate.Value.Year == endDate.Value.Year)
                ? calendarDelegate.FormatShortMonthDay(startDate.Value, localizations)
                : calendarDelegate.FormatShortDate(startDate.Value, localizations);
    }

    // Dart's private top-level `_formatRangeEndDate`.
    internal static string FormatRangeEndDate(
        MaterialLocalizations localizations,
        CalendarDelegate<DateTime> calendarDelegate,
        DateTime? startDate,
        DateTime? endDate,
        DateTime currentDate)
    {
        return endDate == null
            ? localizations.DateRangeEndLabel
            : (startDate != null
               && startDate.Value.Year == endDate.Value.Year
               && startDate.Value.Year == currentDate.Year)
                ? calendarDelegate.FormatShortMonthDay(endDate.Value, localizations)
                : calendarDelegate.FormatShortDate(endDate.Value, localizations);
    }
}

/// <summary>A Material-style date range picker dialog.</summary>
/// <remarks>
/// It is used internally by <see cref="MaterialDatePickers.ShowDateRangePicker"/> or can be directly
/// pushed onto the <see cref="Navigator"/> stack to enable state restoration.
/// </remarks>
public class DateRangePickerDialog : StatefulWidget
{
    private readonly DateTime? _currentDate;

    /// <summary>Creates a date range picker dialog.</summary>
    public DateRangePickerDialog(
        DateTime firstDate,
        DateTime lastDate,
        DateTimeRange<DateTime>? initialDateRange = null,
        DateTime? currentDate = null,
        DatePickerEntryMode initialEntryMode = DatePickerEntryMode.Calendar,
        string? helpText = null,
        string? cancelText = null,
        string? confirmText = null,
        string? saveText = null,
        string? errorInvalidRangeText = null,
        string? errorFormatText = null,
        string? errorInvalidText = null,
        string? fieldStartHintText = null,
        string? fieldEndHintText = null,
        string? fieldStartLabelText = null,
        string? fieldEndLabelText = null,
        TextInputType? keyboardType = null,
        string? restorationId = null,
        Icon? switchToInputEntryModeIcon = null,
        Icon? switchToCalendarEntryModeIcon = null,
        SelectableDayForRangePredicate? selectableDayPredicate = null,
        CalendarDelegate<DateTime>? calendarDelegate = null,
        Key? key = null) : base(key)
    {
        InitialDateRange = initialDateRange;
        FirstDate = firstDate;
        LastDate = lastDate;
        _currentDate = currentDate;
        InitialEntryMode = initialEntryMode;
        HelpText = helpText;
        CancelText = cancelText;
        ConfirmText = confirmText;
        SaveText = saveText;
        ErrorInvalidRangeText = errorInvalidRangeText;
        ErrorFormatText = errorFormatText;
        ErrorInvalidText = errorInvalidText;
        FieldStartHintText = fieldStartHintText;
        FieldEndHintText = fieldEndHintText;
        FieldStartLabelText = fieldStartLabelText;
        FieldEndLabelText = fieldEndLabelText;
        KeyboardType = keyboardType ?? TextInputType.Datetime;
        RestorationId = restorationId;
        SwitchToInputEntryModeIcon = switchToInputEntryModeIcon;
        SwitchToCalendarEntryModeIcon = switchToCalendarEntryModeIcon;
        SelectableDayPredicate = selectableDayPredicate;
        CalendarDelegate = calendarDelegate ?? GregorianCalendarDelegate.Instance;
    }

    /// <summary>The date range that the date range picker starts with when it opens.</summary>
    public DateTimeRange<DateTime>? InitialDateRange { get; }

    /// <summary>The earliest allowable date on the date range.</summary>
    public DateTime FirstDate { get; }

    /// <summary>The latest allowable date on the date range.</summary>
    public DateTime LastDate { get; }

    /// <summary>The <see cref="CurrentDate"/> represents the current day (i.e. today).</summary>
    /// <remarks>Defaults to the value of <c>CalendarDelegate.Now()</c>.</remarks>
    public DateTime CurrentDate => CalendarDelegate.DateOnly(_currentDate ?? CalendarDelegate.Now());

    /// <summary>The initial date range picker entry mode.</summary>
    public DatePickerEntryMode InitialEntryMode { get; }

    /// <summary>The label on the cancel button for the text input mode.</summary>
    public string? CancelText { get; }

    /// <summary>The label on the "OK" button for the text input mode.</summary>
    public string? ConfirmText { get; }

    /// <summary>The label on the save button for the fullscreen calendar mode.</summary>
    public string? SaveText { get; }

    /// <summary>The label displayed at the top of the dialog.</summary>
    public string? HelpText { get; }

    /// <summary>The message used when the date range is invalid (e.g. start date is after end date).</summary>
    public string? ErrorInvalidRangeText { get; }

    /// <summary>The message used when an input text isn't in a proper date format.</summary>
    public string? ErrorFormatText { get; }

    /// <summary>The message used when an input text isn't a selectable date.</summary>
    public string? ErrorInvalidText { get; }

    /// <summary>The text used to prompt the user when no text has been entered in the start field.</summary>
    public string? FieldStartHintText { get; }

    /// <summary>The text used to prompt the user when no text has been entered in the end field.</summary>
    public string? FieldEndHintText { get; }

    /// <summary>The label for the start date text input field.</summary>
    public string? FieldStartLabelText { get; }

    /// <summary>The label for the end date text input field.</summary>
    public string? FieldEndLabelText { get; }

    /// <summary>The keyboard type of the <see cref="TextField"/>.</summary>
    /// <remarks>Defaults to <see cref="TextInputType.Datetime"/>.</remarks>
    public TextInputType KeyboardType { get; }

    /// <summary>Restoration ID to save and restore the state of the <see cref="DateRangePickerDialog"/>.</summary>
    public string? RestorationId { get; }

    /// <inheritdoc cref="DatePickerDialog.SwitchToInputEntryModeIcon"/>
    public Icon? SwitchToInputEntryModeIcon { get; }

    /// <inheritdoc cref="DatePickerDialog.SwitchToCalendarEntryModeIcon"/>
    public Icon? SwitchToCalendarEntryModeIcon { get; }

    /// <summary>Function to provide full control over which dates in the calendar can be selected.</summary>
    public SelectableDayForRangePredicate? SelectableDayPredicate { get; }

    /// <summary>The calendar system the dialog interprets, navigates and formats dates with.</summary>
    public CalendarDelegate<DateTime> CalendarDelegate { get; }

    public override State CreateState() => new DateRangePickerDialogState();
}

/// <summary>Dart's private <c>_DateRangePickerDialogState</c>.</summary>
internal sealed class DateRangePickerDialogState : RestorationState<DateRangePickerDialog>
{
    private RestorableDatePickerEntryMode? _entryModeValue;
    private RestorableDateTimeN? _selectedStartValue;
    private RestorableDateTimeN? _selectedEndValue;
    private readonly RestorableBool _autoValidate = new(false);
    private readonly GlobalKey _calendarPickerKey = new LabeledGlobalKey<State>(null);
    private readonly GlobalKey<InputDateRangePickerState> _inputPickerKey =
        new LabeledGlobalKey<InputDateRangePickerState>(null);

    // Dart's `late final` fields, initialized on first use (from RestoreState).
    private RestorableDatePickerEntryMode EntryMode => _entryModeValue ??= new(Widget.InitialEntryMode);

    private RestorableDateTimeN SelectedStart => _selectedStartValue ??= new(Widget.InitialDateRange?.Start);

    private RestorableDateTimeN SelectedEnd => _selectedEndValue ??= new(Widget.InitialDateRange?.End);

    protected override string? RestorationId => Widget.RestorationId;

    protected override void RestoreState(RestorationBucket? oldBucket, bool initialRestore)
    {
        RegisterForRestoration(EntryMode, "entry_mode");
        RegisterForRestoration(SelectedStart, "selected_start");
        RegisterForRestoration(SelectedEnd, "selected_end");
        RegisterForRestoration(_autoValidate, "autovalidate");
    }

    public override void Dispose()
    {
        EntryMode.Dispose();
        SelectedStart.Dispose();
        SelectedEnd.Dispose();
        _autoValidate.Dispose();
        base.Dispose();
    }

    private void HandleOk()
    {
        if (EntryMode.Value == DatePickerEntryMode.Input || EntryMode.Value == DatePickerEntryMode.InputOnly)
        {
            InputDateRangePickerState picker = _inputPickerKey.CurrentState!;
            if (!picker.Validate())
            {
                SetState(() => _autoValidate.Value = true);
                return;
            }
        }

        DateTimeRange<DateTime>? selectedRange = HasSelectedDateRange
            ? new DateTimeRange<DateTime>(start: SelectedStart.Value!.Value, end: SelectedEnd.Value!.Value)
            : null;

        Navigator.Pop(Context, selectedRange);
    }

    private void HandleCancel()
    {
        Navigator.Pop(Context);
    }

    private void HandleEntryModeToggle()
    {
        SetState(() =>
        {
            switch (EntryMode.Value)
            {
                case DatePickerEntryMode.Calendar:
                    _autoValidate.Value = false;
                    EntryMode.Value = DatePickerEntryMode.Input;
                    break;

                case DatePickerEntryMode.Input:
                    // Validate the range dates
                    if (SelectedStart.Value != null
                        && SelectedEnd.Value != null
                        && SelectedStart.Value.Value > SelectedEnd.Value.Value)
                    {
                        SelectedEnd.Value = null;
                    }

                    if (SelectedStart.Value != null && !IsDaySelectable(SelectedStart.Value.Value))
                    {
                        SelectedStart.Value = null;
                        // With no valid start date, having an end date makes no sense for the UI.
                        SelectedEnd.Value = null;
                    }
                    else if (SelectedEnd.Value != null && !IsDaySelectable(SelectedEnd.Value.Value))
                    {
                        SelectedEnd.Value = null;
                    }

                    EntryMode.Value = DatePickerEntryMode.Calendar;
                    break;

                case DatePickerEntryMode.CalendarOnly:
                case DatePickerEntryMode.InputOnly:
                    DebugAssertions.Assert(false, $"Can not change entry mode from {EntryMode}");
                    break;
            }
        });
    }

    private bool IsDaySelectable(DateTime day)
    {
        if (day < Widget.FirstDate || day > Widget.LastDate)
        {
            return false;
        }

        if (Widget.SelectableDayPredicate == null)
        {
            return true;
        }

        return Widget.SelectableDayPredicate(day, SelectedStart.Value, SelectedEnd.Value);
    }

    private void HandleStartDateChanged(DateTime? date)
    {
        SetState(() => SelectedStart.Value = date);
    }

    private void HandleEndDateChanged(DateTime? date)
    {
        SetState(() => SelectedEnd.Value = date);
    }

    private bool HasSelectedDateRange => SelectedStart.Value != null && SelectedEnd.Value != null;

    public override Widget Build(BuildContext context)
    {
        ThemeData theme = Theme.Of(context);
        bool useMaterial3 = theme.UseMaterial3;
        Orientation orientation = MediaQuery.OrientationOf(context);
        MaterialLocalizations localizations = MaterialLocalizations.Of(context);
        DatePickerThemeData datePickerTheme = DatePickerTheme.Of(context);
        DatePickerThemeData defaults = DatePickerTheme.Defaults(context);

        Widget contents;
        Size size;
        double? elevation;
        Color? shadowColor;
        Color? surfaceTintColor;
        ShapeBorder? shape;
        Thickness insetPadding;
        bool showEntryModeButton = EntryMode.Value == DatePickerEntryMode.Calendar
            || EntryMode.Value == DatePickerEntryMode.Input;
        switch (EntryMode.Value)
        {
            case DatePickerEntryMode.Calendar:
            case DatePickerEntryMode.CalendarOnly:
            default:
                contents = new CalendarRangePickerDialog(
                    key: _calendarPickerKey,
                    calendarDelegate: Widget.CalendarDelegate,
                    selectedStartDate: SelectedStart.Value,
                    selectedEndDate: SelectedEnd.Value,
                    firstDate: Widget.FirstDate,
                    lastDate: Widget.LastDate,
                    selectableDayPredicate: Widget.SelectableDayPredicate,
                    currentDate: Widget.CurrentDate,
                    onStartDateChanged: date => HandleStartDateChanged(date),
                    onEndDateChanged: HandleEndDateChanged,
                    onConfirm: HasSelectedDateRange ? HandleOk : null,
                    onCancel: HandleCancel,
                    entryModeButton: showEntryModeButton
                        ? new IconButton(
                            icon: Widget.SwitchToInputEntryModeIcon
                                ?? new Icon(useMaterial3 ? Icons.EditOutlined : Icons.Edit),
                            padding: EdgeInsets.Zero,
                            tooltip: localizations.InputDateModeButtonLabel,
                            onPressed: HandleEntryModeToggle)
                        : null,
                    confirmText: Widget.SaveText
                        ?? (useMaterial3
                            ? localizations.SaveButtonLabel
                            : localizations.SaveButtonLabel.ToUpperInvariant()),
                    helpText: Widget.HelpText
                        ?? (useMaterial3
                            ? localizations.DateRangePickerHelpText
                            : localizations.DateRangePickerHelpText.ToUpperInvariant()));
                size = MediaQuery.SizeOf(context);
                insetPadding = new Thickness(0);
                elevation = datePickerTheme.RangePickerElevation ?? defaults.RangePickerElevation!;
                shadowColor = datePickerTheme.RangePickerShadowColor ?? defaults.RangePickerShadowColor!;
                surfaceTintColor = datePickerTheme.RangePickerSurfaceTintColor
                    ?? defaults.RangePickerSurfaceTintColor!;
                shape = datePickerTheme.RangePickerShape ?? defaults.RangePickerShape;
                break;

            case DatePickerEntryMode.Input:
            case DatePickerEntryMode.InputOnly:
                contents = new InputDateRangePickerDialog(
                    calendarDelegate: Widget.CalendarDelegate,
                    selectedStartDate: SelectedStart.Value,
                    selectedEndDate: SelectedEnd.Value,
                    currentDate: Widget.CurrentDate,
                    picker: new SizedBox(
                        height: orientation == Orientation.Portrait
                            ? DatePickerConstants.InputFormPortraitHeight
                            : DatePickerConstants.InputFormLandscapeHeight,
                        child: new Padding(
                            EdgeInsets.Symmetric(horizontal: 24),
                            new Column(
                                children:
                                [
                                    new Spacer(),
                                    new InputDateRangePicker(
                                        key: _inputPickerKey,
                                        calendarDelegate: Widget.CalendarDelegate,
                                        initialStartDate: SelectedStart.Value,
                                        initialEndDate: SelectedEnd.Value,
                                        firstDate: Widget.FirstDate,
                                        lastDate: Widget.LastDate,
                                        selectableDayPredicate: Widget.SelectableDayPredicate,
                                        onStartDateChanged: HandleStartDateChanged,
                                        onEndDateChanged: HandleEndDateChanged,
                                        autofocus: true,
                                        autovalidate: _autoValidate.Value,
                                        helpText: Widget.HelpText,
                                        errorInvalidRangeText: Widget.ErrorInvalidRangeText,
                                        errorFormatText: Widget.ErrorFormatText,
                                        errorInvalidText: Widget.ErrorInvalidText,
                                        fieldStartHintText: Widget.FieldStartHintText,
                                        fieldEndHintText: Widget.FieldEndHintText,
                                        fieldStartLabelText: Widget.FieldStartLabelText,
                                        fieldEndLabelText: Widget.FieldEndLabelText,
                                        keyboardType: Widget.KeyboardType),
                                    new Spacer(),
                                ]))),
                    onConfirm: HandleOk,
                    onCancel: HandleCancel,
                    entryModeButton: showEntryModeButton
                        ? new IconButton(
                            icon: Widget.SwitchToCalendarEntryModeIcon ?? new Icon(Icons.CalendarToday),
                            padding: EdgeInsets.Zero,
                            tooltip: localizations.CalendarModeButtonLabel,
                            onPressed: HandleEntryModeToggle)
                        : null,
                    confirmText: Widget.ConfirmText ?? localizations.OkButtonLabel,
                    cancelText: Widget.CancelText
                        ?? (useMaterial3
                            ? localizations.CancelButtonLabel
                            : localizations.CancelButtonLabel.ToUpperInvariant()),
                    helpText: Widget.HelpText
                        ?? (useMaterial3
                            ? localizations.DateRangePickerHelpText
                            : localizations.DateRangePickerHelpText.ToUpperInvariant()));
                DialogThemeData dialogTheme = theme.DialogTheme;
                size = orientation == Orientation.Portrait
                    ? (useMaterial3
                        ? DatePickerConstants.InputPortraitDialogSizeM3
                        : DatePickerConstants.InputPortraitDialogSizeM2)
                    : DatePickerConstants.InputRangeLandscapeDialogSize;
                elevation = useMaterial3
                    ? datePickerTheme.Elevation ?? defaults.Elevation!
                    : datePickerTheme.Elevation ?? dialogTheme.Elevation ?? 24;
                shadowColor = datePickerTheme.ShadowColor ?? defaults.ShadowColor;
                surfaceTintColor = datePickerTheme.SurfaceTintColor ?? defaults.SurfaceTintColor;
                shape = useMaterial3
                    ? datePickerTheme.Shape ?? defaults.Shape
                    : datePickerTheme.Shape ?? dialogTheme.Shape ?? defaults.Shape;

                insetPadding = new Thickness(16.0, 24.0);
                break;
        }

        return new Dialog(
            insetPadding: insetPadding,
            backgroundColor: datePickerTheme.BackgroundColor ?? defaults.BackgroundColor,
            elevation: elevation,
            shadowColor: shadowColor,
            surfaceTintColor: surfaceTintColor,
            shape: shape,
            clipBehavior: Clip.AntiAlias,
            child: new AnimatedContainer(
                width: size.Width,
                height: size.Height,
                duration: DatePickerConstants.DialogSizeAnimationDuration,
                curve: Curves.EaseIn,
                child: MediaQuery.WithClampedTextScaling(
                    maxScaleFactor: DatePickerConstants.KMaxRangeTextScaleFactor,
                    child: new Builder(_ => contents))));
    }
}

/// <summary>Dart's private <c>_CalendarRangePickerDialog</c>.</summary>
internal sealed class CalendarRangePickerDialog : StatelessWidget
{
    public CalendarRangePickerDialog(
        DateTime? selectedStartDate,
        DateTime? selectedEndDate,
        DateTime firstDate,
        DateTime lastDate,
        DateTime? currentDate,
        Action<DateTime> onStartDateChanged,
        Action<DateTime?> onEndDateChanged,
        Action? onConfirm,
        Action? onCancel,
        string confirmText,
        string helpText,
        SelectableDayForRangePredicate? selectableDayPredicate,
        CalendarDelegate<DateTime> calendarDelegate,
        Widget? entryModeButton = null,
        Key? key = null) : base(key)
    {
        SelectedStartDate = selectedStartDate;
        SelectedEndDate = selectedEndDate;
        FirstDate = firstDate;
        LastDate = lastDate;
        CurrentDate = currentDate;
        OnStartDateChanged = onStartDateChanged;
        OnEndDateChanged = onEndDateChanged;
        OnConfirm = onConfirm;
        OnCancel = onCancel;
        ConfirmText = confirmText;
        HelpText = helpText;
        SelectableDayPredicate = selectableDayPredicate;
        CalendarDelegate = calendarDelegate;
        EntryModeButton = entryModeButton;
    }

    public DateTime? SelectedStartDate { get; }
    public DateTime? SelectedEndDate { get; }
    public DateTime FirstDate { get; }
    public DateTime LastDate { get; }
    public SelectableDayForRangePredicate? SelectableDayPredicate { get; }
    public DateTime? CurrentDate { get; }
    public Action<DateTime> OnStartDateChanged { get; }
    public Action<DateTime?> OnEndDateChanged { get; }
    public Action? OnConfirm { get; }
    public Action? OnCancel { get; }
    public string ConfirmText { get; }
    public string HelpText { get; }
    public CalendarDelegate<DateTime> CalendarDelegate { get; }
    public Widget? EntryModeButton { get; }

    public override Widget Build(BuildContext context)
    {
        ThemeData theme = Theme.Of(context);
        bool useMaterial3 = theme.UseMaterial3;
        MaterialLocalizations localizations = MaterialLocalizations.Of(context);
        Orientation orientation = MediaQuery.OrientationOf(context);
        DatePickerThemeData themeData = DatePickerTheme.Of(context);
        DatePickerThemeData defaults = DatePickerTheme.Defaults(context);
        Color? dialogBackground = themeData.RangePickerBackgroundColor ?? defaults.RangePickerBackgroundColor;
        Color? headerBackground = themeData.RangePickerHeaderBackgroundColor
            ?? defaults.RangePickerHeaderBackgroundColor;
        Color? headerForeground = themeData.RangePickerHeaderForegroundColor
            ?? defaults.RangePickerHeaderForegroundColor;
        Color? headerDisabledForeground = headerForeground?.WithOpacity(0.38);
        TextStyle? headlineStyle = themeData.RangePickerHeaderHeadlineStyle
            ?? defaults.RangePickerHeaderHeadlineStyle;
        TextStyle? headlineHelpStyle = (themeData.RangePickerHeaderHelpStyle ?? defaults.RangePickerHeaderHelpStyle)
            ?.Apply(color: headerForeground);
        string startDateText = MaterialDatePickers.FormatRangeStartDate(
            localizations,
            CalendarDelegate,
            SelectedStartDate,
            SelectedEndDate);
        string endDateText = MaterialDatePickers.FormatRangeEndDate(
            localizations,
            CalendarDelegate,
            SelectedStartDate,
            SelectedEndDate,
            CalendarDelegate.Now());
        TextStyle? startDateStyle = headlineStyle?.Apply(
            color: SelectedStartDate != null ? headerForeground : headerDisabledForeground);
        TextStyle? endDateStyle = headlineStyle?.Apply(
            color: SelectedEndDate != null ? headerForeground : headerDisabledForeground);
        ButtonStyle buttonStyle = TextButton.StyleFrom(
            foregroundColor: headerForeground,
            disabledForegroundColor: headerDisabledForeground);
        var iconTheme = new IconThemeData(Color: headerForeground);

        var actions = new List<Widget>();
        if (orientation == Orientation.Landscape && EntryModeButton != null)
        {
            actions.Add(EntryModeButton);
        }

        actions.Add(new TextButton(style: buttonStyle, onPressed: OnConfirm, child: new Text(ConfirmText)));
        actions.Add(new SizedBox(width: 8));

        var bottomRow = new List<Widget>
        {
            new SizedBox(width: MediaQuery.WidthOf(context) < 360 ? 42 : 72),
            new Expanded(
                child: new Semantics(
                    label: $"{HelpText} {startDateText} to {endDateText}",
                    excludeSemantics: true,
                    child: new Column(
                        crossAxisAlignment: CrossAxisAlignment.Start,
                        children:
                        [
                            new Text(
                                HelpText,
                                style: headlineHelpStyle,
                                maxLines: 1,
                                overflow: TextOverflow.Ellipsis),
                            new SizedBox(height: 8),
                            new Row(
                                children:
                                [
                                    new Text(
                                        startDateText,
                                        style: startDateStyle,
                                        maxLines: 1,
                                        overflow: TextOverflow.Ellipsis),
                                    new Text(" – ", style: startDateStyle),
                                    new Flexible(
                                        child: new Text(
                                            endDateText,
                                            style: endDateStyle,
                                            maxLines: 1,
                                            overflow: TextOverflow.Ellipsis)),
                                ]),
                            new SizedBox(height: 16),
                        ]))),
        };
        if (orientation == Orientation.Portrait && EntryModeButton != null)
        {
            bottomRow.Add(new Padding(
                EdgeInsets.Symmetric(horizontal: 8.0),
                new IconTheme(data: iconTheme, child: EntryModeButton)));
        }

        return new SafeArea(
            top: false,
            left: false,
            right: false,
            child: new Scaffold(
                appBar: new AppBar(
                    iconTheme: iconTheme,
                    actionsIconTheme: iconTheme,
                    elevation: useMaterial3 ? 0 : null,
                    scrolledUnderElevation: useMaterial3 ? 0 : null,
                    backgroundColor: headerBackground,
                    leading: new CloseButton(onPressed: OnCancel),
                    actions: actions,
                    bottom: new PreferredSize(
                        preferredSize: new Size(double.PositiveInfinity, 64),
                        child: new Row(children: bottomRow))),
                backgroundColor: dialogBackground,
                body: new CalendarDateRangePicker(
                    initialStartDate: SelectedStartDate,
                    initialEndDate: SelectedEndDate,
                    firstDate: FirstDate,
                    lastDate: LastDate,
                    currentDate: CurrentDate,
                    onStartDateChanged: OnStartDateChanged,
                    onEndDateChanged: OnEndDateChanged,
                    selectableDayPredicate: SelectableDayPredicate,
                    calendarDelegate: CalendarDelegate)));
    }
}

/// <summary>The range-only library constants of date_picker.dart.</summary>
internal static class DateRangePickerConstants
{
    public static readonly TimeSpan MonthScrollDuration = TimeSpan.FromMilliseconds(200);

    public const double MonthItemHeaderHeight = 58.0;
    public const double MonthItemFooterHeight = 12.0;
    public const double MonthItemRowHeight = 42.0;
    public const double MonthItemSpaceBetweenRows = 8.0;
    public const double HorizontalPadding = 8.0;
    public const double MaxCalendarWidthLandscape = 384.0;
    public const double MaxCalendarWidthPortrait = 480.0;

    // Dart's `const _MonthItemGridDelegate _monthItemGridDelegate = _MonthItemGridDelegate();`.
    public static readonly MonthItemGridDelegate MonthItemGridDelegateInstance = new();

    // Dart's `DateTime.daysPerWeek`.
    public const int DaysPerWeek = 7;
}

/// <summary>
/// Displays a scrollable calendar grid that allows a user to select a range of dates.
/// </summary>
/// <remarks>Dart's private <c>_CalendarDateRangePicker</c>.</remarks>
internal sealed class CalendarDateRangePicker : StatefulWidget
{
    /// <summary>Creates a scrollable calendar grid for picking date ranges.</summary>
    public CalendarDateRangePicker(
        DateTime firstDate,
        DateTime lastDate,
        SelectableDayForRangePredicate? selectableDayPredicate,
        Action<DateTime>? onStartDateChanged,
        Action<DateTime?>? onEndDateChanged,
        CalendarDelegate<DateTime> calendarDelegate,
        DateTime? initialStartDate = null,
        DateTime? initialEndDate = null,
        DateTime? currentDate = null,
        Key? key = null) : base(key)
    {
        InitialStartDate = initialStartDate != null ? calendarDelegate.DateOnly(initialStartDate.Value) : null;
        InitialEndDate = initialEndDate != null ? calendarDelegate.DateOnly(initialEndDate.Value) : null;
        FirstDate = calendarDelegate.DateOnly(firstDate);
        LastDate = calendarDelegate.DateOnly(lastDate);
        CurrentDate = calendarDelegate.DateOnly(currentDate ?? calendarDelegate.Now());
        SelectableDayPredicate = selectableDayPredicate;
        OnStartDateChanged = onStartDateChanged;
        OnEndDateChanged = onEndDateChanged;
        CalendarDelegate = calendarDelegate;
        DebugAssertions.Assert(
            InitialStartDate == null || InitialEndDate == null || !(InitialStartDate.Value > initialEndDate!.Value),
            "initialStartDate must be on or before initialEndDate.");
        DebugAssertions.Assert(!(LastDate < FirstDate), "firstDate must be on or before lastDate.");
    }

    /// <summary>The <see cref="DateTime"/> that represents the start of the initial date range selection.</summary>
    public DateTime? InitialStartDate { get; }

    /// <summary>The <see cref="DateTime"/> that represents the end of the initial date range selection.</summary>
    public DateTime? InitialEndDate { get; }

    /// <summary>The earliest allowable <see cref="DateTime"/> that the user can select.</summary>
    public DateTime FirstDate { get; }

    /// <summary>The latest allowable <see cref="DateTime"/> that the user can select.</summary>
    public DateTime LastDate { get; }

    public SelectableDayForRangePredicate? SelectableDayPredicate { get; }

    /// <summary>The <see cref="DateTime"/> representing today. It will be highlighted in the day grid.</summary>
    public DateTime CurrentDate { get; }

    /// <summary>Called when the user changes the start date of the selected range.</summary>
    public Action<DateTime>? OnStartDateChanged { get; }

    /// <summary>Called when the user changes the end date of the selected range.</summary>
    public Action<DateTime?>? OnEndDateChanged { get; }

    public CalendarDelegate<DateTime> CalendarDelegate { get; }

    public override State CreateState() => new CalendarDateRangePickerState();
}

/// <summary>Dart's private <c>_CalendarDateRangePickerState</c>.</summary>
internal sealed class CalendarDateRangePickerState : State<CalendarDateRangePicker>
{
    private readonly GlobalKey _scrollViewKey = new LabeledGlobalKey<State>(null);
    private readonly Key _sliverAfterKey = new UniqueKey();
    private DateTime? _startDate;
    private DateTime? _endDate;
    private int _initialMonthIndex;
    private ScrollController _controller = null!;
    private bool _showWeekBottomDivider;

    public override void InitState()
    {
        base.InitState();
        _controller = new ScrollController();
        _controller.AddListener(ScrollListener);

        _startDate = Widget.InitialStartDate;
        _endDate = Widget.InitialEndDate;

        // Calculate the index for the initially displayed month. This is needed to
        // divide the list of months into two `SliverList`s.
        DateTime initialDate = Widget.InitialStartDate ?? Widget.CurrentDate;
        if (!(initialDate < Widget.FirstDate) && !(initialDate > Widget.LastDate))
        {
            _initialMonthIndex = Widget.CalendarDelegate.MonthDelta(Widget.FirstDate, initialDate);
        }

        _showWeekBottomDivider = _initialMonthIndex != 0;
    }

    public override void Dispose()
    {
        _controller.Dispose();
        base.Dispose();
    }

    private void ScrollListener()
    {
        if (_controller.Offset <= _controller.Position.MinScrollExtent)
        {
            SetState(() => _showWeekBottomDivider = false);
        }
        else if (!_showWeekBottomDivider)
        {
            SetState(() => _showWeekBottomDivider = true);
        }
    }

    private int NumberOfMonths => Widget.CalendarDelegate.MonthDelta(Widget.FirstDate, Widget.LastDate) + 1;

    private void Vibrate()
    {
        switch (Theme.Of(Context).Platform)
        {
            case TargetPlatform.Android:
            case TargetPlatform.Fuchsia:
                _ = HapticFeedback.Vibrate();
                break;
            case TargetPlatform.IOS:
            case TargetPlatform.Linux:
            case TargetPlatform.MacOS:
            case TargetPlatform.Windows:
                break;
        }
    }

    // This updates the selected date range using this logic:
    //
    // * From the unselected state, selecting one date creates the start date.
    //   * If the next selection is before the start date, reset date range and
    //     set the start date to that selection.
    //   * If the next selection is on or after the start date, set the end date
    //     to that selection.
    // * After both start and end dates are selected, any subsequent selection
    //   resets the date range and sets start date to that selection.
    private void UpdateSelection(DateTime date)
    {
        Vibrate();
        SetState(() =>
        {
            if (_startDate != null && _endDate == null && !(date < _startDate.Value))
            {
                _endDate = date;
                Widget.OnEndDateChanged?.Invoke(_endDate);
            }
            else
            {
                _startDate = date;
                Widget.OnStartDateChanged?.Invoke(_startDate.Value);
                if (_endDate != null)
                {
                    _endDate = null;
                    Widget.OnEndDateChanged?.Invoke(_endDate);
                }
            }
        });
    }

    private Widget BuildMonthItem(BuildContext context, int index, bool beforeInitialMonth)
    {
        int monthIndex = beforeInitialMonth ? _initialMonthIndex - index - 1 : _initialMonthIndex + index;
        DateTime month = Widget.CalendarDelegate.AddMonthsToMonthDate(Widget.FirstDate, monthIndex);
        return new MonthItem(
            calendarDelegate: Widget.CalendarDelegate,
            selectedDateStart: _startDate,
            selectedDateEnd: _endDate,
            currentDate: Widget.CurrentDate,
            firstDate: Widget.FirstDate,
            lastDate: Widget.LastDate,
            displayedMonth: month,
            onChanged: UpdateSelection,
            selectableDayPredicate: Widget.SelectableDayPredicate);
    }

    public override Widget Build(BuildContext context)
    {
        var children = new List<Widget> { new DayHeaders() };
        if (_showWeekBottomDivider)
        {
            children.Add(new Divider(height: 0));
        }

        children.Add(new Expanded(
            child: new CalendarKeyboardNavigator(
                calendarDelegate: Widget.CalendarDelegate,
                firstDate: Widget.FirstDate,
                lastDate: Widget.LastDate,
                initialFocusedDay: _startDate ?? Widget.InitialStartDate ?? Widget.CurrentDate,
                // In order to prevent performance issues when displaying the
                // correct initial month, 2 `SliverList`s are used to split the
                // months. The first item in the second SliverList is the initial
                // month to be displayed.
                child: new CustomScrollView(
                    key: _scrollViewKey,
                    controller: _controller,
                    center: _sliverAfterKey,
                    slivers:
                    [
                        SliverList.Builder(
                            itemCount: _initialMonthIndex,
                            itemBuilder: (itemContext, index) => BuildMonthItem(itemContext, index, true)),
                        SliverList.Builder(
                            key: _sliverAfterKey,
                            itemCount: NumberOfMonths - _initialMonthIndex,
                            itemBuilder: (itemContext, index) => BuildMonthItem(itemContext, index, false)),
                    ]))));

        return new Column(children: children);
    }
}

/// <summary>Dart's private <c>_CalendarKeyboardNavigator</c>.</summary>
internal sealed class CalendarKeyboardNavigator : StatefulWidget
{
    public CalendarKeyboardNavigator(
        Widget child,
        DateTime firstDate,
        DateTime lastDate,
        DateTime initialFocusedDay,
        CalendarDelegate<DateTime> calendarDelegate,
        Key? key = null) : base(key)
    {
        Child = child;
        FirstDate = firstDate;
        LastDate = lastDate;
        InitialFocusedDay = initialFocusedDay;
        CalendarDelegate = calendarDelegate;
    }

    public Widget Child { get; }
    public DateTime FirstDate { get; }
    public DateTime LastDate { get; }
    public DateTime InitialFocusedDay { get; }
    public CalendarDelegate<DateTime> CalendarDelegate { get; }

    public override State CreateState() => new CalendarKeyboardNavigatorState();
}

/// <summary>Dart's private <c>_CalendarKeyboardNavigatorState</c>.</summary>
internal sealed class CalendarKeyboardNavigatorState : State<CalendarKeyboardNavigator>
{
    private static readonly Dictionary<TraversalDirection, int> DirectionOffset = new()
    {
        [TraversalDirection.Up] = -DateRangePickerConstants.DaysPerWeek,
        [TraversalDirection.Right] = 1,
        [TraversalDirection.Down] = DateRangePickerConstants.DaysPerWeek,
        [TraversalDirection.Left] = -1,
    };

    private readonly Dictionary<ShortcutActivator, Intent> _shortcutMap = new()
    {
        [new SingleActivator(LogicalKeyboardKey.ArrowLeft)] = new DirectionalFocusIntent(TraversalDirection.Left),
        [new SingleActivator(LogicalKeyboardKey.ArrowRight)] =
            new DirectionalFocusIntent(TraversalDirection.Right),
        [new SingleActivator(LogicalKeyboardKey.ArrowDown)] = new DirectionalFocusIntent(TraversalDirection.Down),
        [new SingleActivator(LogicalKeyboardKey.ArrowUp)] = new DirectionalFocusIntent(TraversalDirection.Up),
    };

    private Dictionary<Type, FlutterAction> _actionMap = null!;
    private FocusNode _dayGridFocus = null!;
    private TraversalDirection? _dayTraversalDirection;
    private DateTime? _focusedDay;

    public override void InitState()
    {
        base.InitState();

        _actionMap = new Dictionary<Type, FlutterAction>
        {
            [typeof(NextFocusIntent)] = new CallbackAction<NextFocusIntent>(HandleGridNextFocus),
            [typeof(PreviousFocusIntent)] = new CallbackAction<PreviousFocusIntent>(HandleGridPreviousFocus),
            [typeof(DirectionalFocusIntent)] = new CallbackAction<DirectionalFocusIntent>(HandleDirectionFocus),
        };
        _dayGridFocus = new FocusNode(debugLabel: "Day Grid");
    }

    public override void Dispose()
    {
        _dayGridFocus.Dispose();
        base.Dispose();
    }

    private void HandleGridFocusChange(bool focused)
    {
        SetState(() =>
        {
            if (focused)
            {
                _focusedDay ??= Widget.InitialFocusedDay;
            }
        });
    }

    private object? HandleGridNextFocus(NextFocusIntent intent)
    {
        _dayGridFocus.RequestFocus();
        _dayGridFocus.NextFocus();
        return null;
    }

    private object? HandleGridPreviousFocus(PreviousFocusIntent intent)
    {
        _dayGridFocus.RequestFocus();
        _dayGridFocus.PreviousFocus();
        return null;
    }

    private object? HandleDirectionFocus(DirectionalFocusIntent intent)
    {
        DebugAssertions.Assert(_focusedDay != null);
        SetState(() =>
        {
            DateTime? nextDate = NextDateInDirection(_focusedDay!.Value, intent.Direction);
            if (nextDate != null)
            {
                _focusedDay = nextDate;
                _dayTraversalDirection = intent.Direction;
            }
        });
        return null;
    }

    private static int DayDirectionOffset(TraversalDirection traversalDirection, TextDirection textDirection)
    {
        // Swap left and right if the text direction if RTL
        if (textDirection == TextDirection.Rtl)
        {
            if (traversalDirection == TraversalDirection.Left)
            {
                traversalDirection = TraversalDirection.Right;
            }
            else if (traversalDirection == TraversalDirection.Right)
            {
                traversalDirection = TraversalDirection.Left;
            }
        }

        return DirectionOffset[traversalDirection];
    }

    private DateTime? NextDateInDirection(DateTime date, TraversalDirection direction)
    {
        TextDirection textDirection = Directionality.Of(Context);
        DateTime nextDate = Widget.CalendarDelegate.AddDaysToDate(date, DayDirectionOffset(direction, textDirection));
        if (!(nextDate < Widget.FirstDate) && !(nextDate > Widget.LastDate))
        {
            return nextDate;
        }

        return null;
    }

    public override Widget Build(BuildContext context)
    {
        return new FocusableActionDetector(
            shortcuts: _shortcutMap,
            actions: _actionMap,
            focusNode: _dayGridFocus,
            onFocusChange: HandleGridFocusChange,
            child: new DateRangeFocusedDate(
                calendarDelegate: Widget.CalendarDelegate,
                date: _dayGridFocus.HasFocus ? _focusedDay : null,
                scrollDirection: _dayGridFocus.HasFocus ? _dayTraversalDirection : null,
                child: Widget.Child));
    }
}

/// <summary>
/// InheritedWidget indicating what the current focused date is for its children.
/// </summary>
/// <remarks>
/// Dart's private <c>_FocusedDate</c> in date_picker.dart, renamed because calendar_date_picker.dart
/// has its own, unrelated library-private <c>_FocusedDate</c>.
/// </remarks>
internal sealed class DateRangeFocusedDate : InheritedWidget
{
    public DateRangeFocusedDate(
        Widget child,
        CalendarDelegate<DateTime> calendarDelegate,
        DateTime? date = null,
        TraversalDirection? scrollDirection = null,
        Key? key = null) : base(child, key)
    {
        CalendarDelegate = calendarDelegate;
        Date = date;
        ScrollDirection = scrollDirection;
    }

    public CalendarDelegate<DateTime> CalendarDelegate { get; }
    public DateTime? Date { get; }
    public TraversalDirection? ScrollDirection { get; }

    public override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        var old = (DateRangeFocusedDate)oldWidget;
        return !CalendarDelegate.IsSameDay(Date, old.Date) || ScrollDirection != old.ScrollDirection;
    }

    public static DateRangeFocusedDate? MaybeOf(BuildContext context)
    {
        return context.DependOnInheritedWidgetOfExactType<DateRangeFocusedDate>();
    }
}

/// <summary>Dart's private <c>_DayHeaders</c>.</summary>
internal sealed class DayHeaders : StatelessWidget
{
    public DayHeaders(Key? key = null) : base(key)
    {
    }

    /// <summary>Builds widgets showing abbreviated days of week.</summary>
    /// <remarks>
    /// The first widget in the returned list corresponds to the first day of week for the current
    /// locale.
    /// </remarks>
    private static List<Widget> GetDayHeaders(TextStyle headerStyle, MaterialLocalizations localizations)
    {
        var result = new List<Widget>();
        for (int i = localizations.FirstDayOfWeekIndex;
             result.Count < DateRangePickerConstants.DaysPerWeek;
             i = (i + 1) % DateRangePickerConstants.DaysPerWeek)
        {
            string weekday = localizations.NarrowWeekdays[i];
            result.Add(new ExcludeSemantics(child: new Center(child: new Text(weekday, style: headerStyle))));
        }

        return result;
    }

    public override Widget Build(BuildContext context)
    {
        ThemeData themeData = Theme.Of(context);
        ColorScheme colorScheme = themeData.ColorScheme;
        TextStyle textStyle = themeData.TextTheme.TitleSmall!.Apply(color: colorScheme.OnSurface);
        MaterialLocalizations localizations = MaterialLocalizations.Of(context);
        List<Widget> labels = GetDayHeaders(textStyle, localizations);

        // Add leading and trailing boxes for edges of the custom grid layout.
        labels.Insert(0, SizedBox.Shrink());
        labels.Add(SizedBox.Shrink());

        return new ConstrainedBox(
            new BoxConstraints(
                MaxWidth: MediaQuery.OrientationOf(context) == Orientation.Landscape
                    ? DateRangePickerConstants.MaxCalendarWidthLandscape
                    : DateRangePickerConstants.MaxCalendarWidthPortrait,
                MaxHeight: DateRangePickerConstants.MonthItemRowHeight),
            GridView.Custom(
                shrinkWrap: true,
                gridDelegate: DateRangePickerConstants.MonthItemGridDelegateInstance,
                childrenDelegate: new SliverChildListDelegate(labels, addRepaintBoundaries: false)));
    }
}

/// <summary>Dart's private <c>_MonthItemGridDelegate</c>.</summary>
internal sealed class MonthItemGridDelegate : SliverGridDelegate
{
    public override SliverGridLayout GetLayout(SliverConstraints constraints)
    {
        double tileWidth = Math.Max(
            (constraints.CrossAxisExtent - 2 * DateRangePickerConstants.HorizontalPadding)
            / DateRangePickerConstants.DaysPerWeek,
            0.0);
        return new MonthSliverGridLayout(
            crossAxisCount: DateRangePickerConstants.DaysPerWeek + 2,
            dayChildWidth: tileWidth,
            edgeChildWidth: DateRangePickerConstants.HorizontalPadding,
            reverseCrossAxis: ScrollDirectionUtils.AxisDirectionIsReversed(constraints.CrossAxisDirection));
    }

    public override bool ShouldRelayout(SliverGridDelegate oldDelegate) => false;
}

/// <summary>Dart's private <c>_MonthSliverGridLayout</c>.</summary>
internal sealed class MonthSliverGridLayout : SliverGridLayout
{
    /// <summary>Creates a layout that uses equally sized and spaced tiles for each day of the week and an
    /// additional edge tile for padding at the start and end of each row.</summary>
    /// <remarks>This is necessary to facilitate the painting of the range highlight correctly.</remarks>
    public MonthSliverGridLayout(int crossAxisCount, double dayChildWidth, double edgeChildWidth, bool reverseCrossAxis)
    {
        DebugAssertions.Assert(crossAxisCount > 0);
        DebugAssertions.Assert(dayChildWidth >= 0);
        DebugAssertions.Assert(edgeChildWidth >= 0);
        CrossAxisCount = crossAxisCount;
        DayChildWidth = dayChildWidth;
        EdgeChildWidth = edgeChildWidth;
        ReverseCrossAxis = reverseCrossAxis;
    }

    /// <summary>The number of children in the cross axis.</summary>
    public int CrossAxisCount { get; }

    /// <summary>The width in logical pixels of the day child widget.</summary>
    public double DayChildWidth { get; }

    /// <summary>The width in logical pixels of the edge child widget.</summary>
    public double EdgeChildWidth { get; }

    /// <summary>Whether the children should be placed in the opposite order of increasing coordinates in
    /// the cross axis.</summary>
    public bool ReverseCrossAxis { get; }

    /// <summary>The number of logical pixels from the leading edge of one row to the leading edge of the
    /// next row.</summary>
    private static double RowHeight =>
        DateRangePickerConstants.MonthItemRowHeight + DateRangePickerConstants.MonthItemSpaceBetweenRows;

    /// <summary>The height in logical pixels of the children widgets.</summary>
    private static double ChildHeight => DateRangePickerConstants.MonthItemRowHeight;

    public override int GetMinChildIndexForScrollOffset(double scrollOffset)
    {
        return CrossAxisCount * (int)Math.Truncate(scrollOffset / RowHeight);
    }

    public override int GetMaxChildIndexForScrollOffset(double scrollOffset)
    {
        int mainAxisCount = (int)Math.Ceiling(scrollOffset / RowHeight);
        return Math.Max(0, CrossAxisCount * mainAxisCount - 1);
    }

    private double GetCrossAxisOffset(double crossAxisStart, bool isPadding)
    {
        if (ReverseCrossAxis)
        {
            return ((CrossAxisCount - 2) * DayChildWidth + 2 * EdgeChildWidth)
                - crossAxisStart
                - (isPadding ? EdgeChildWidth : DayChildWidth);
        }

        return crossAxisStart;
    }

    public override SliverGridGeometry GetGeometryForChildIndex(int index)
    {
        int adjustedIndex = index % CrossAxisCount;
        bool isEdge = adjustedIndex == 0 || adjustedIndex == CrossAxisCount - 1;
        double crossAxisStart = Math.Max(0, (adjustedIndex - 1) * DayChildWidth + EdgeChildWidth);

        return new SliverGridGeometry(
            scrollOffset: (index / CrossAxisCount) * RowHeight,
            crossAxisOffset: GetCrossAxisOffset(crossAxisStart, isEdge),
            mainAxisExtent: ChildHeight,
            crossAxisExtent: isEdge ? EdgeChildWidth : DayChildWidth);
    }

    public override double ComputeMaxScrollOffset(int childCount)
    {
        DebugAssertions.Assert(childCount >= 0);
        int mainAxisCount = ((childCount - 1) / CrossAxisCount) + 1;
        double mainAxisSpacing = RowHeight - ChildHeight;
        return RowHeight * mainAxisCount - mainAxisSpacing;
    }
}

/// <summary>Displays the days of a given month and allows choosing a date range.</summary>
/// <remarks>
/// Dart's private <c>_MonthItem</c>. The days are arranged in a rectangular grid with one column for
/// each day of the week.
/// </remarks>
internal sealed class MonthItem : StatefulWidget
{
    /// <summary>Creates a month item.</summary>
    public MonthItem(
        DateTime? selectedDateStart,
        DateTime? selectedDateEnd,
        DateTime currentDate,
        Action<DateTime> onChanged,
        DateTime firstDate,
        DateTime lastDate,
        DateTime displayedMonth,
        SelectableDayForRangePredicate? selectableDayPredicate,
        CalendarDelegate<DateTime> calendarDelegate,
        Key? key = null) : base(key)
    {
        DebugAssertions.Assert(!(firstDate > lastDate));
        DebugAssertions.Assert(selectedDateStart == null || !(selectedDateStart.Value < firstDate));
        DebugAssertions.Assert(selectedDateEnd == null || !(selectedDateEnd.Value < firstDate));
        DebugAssertions.Assert(selectedDateStart == null || !(selectedDateStart.Value > lastDate));
        DebugAssertions.Assert(selectedDateEnd == null || !(selectedDateEnd.Value > lastDate));
        DebugAssertions.Assert(
            selectedDateStart == null || selectedDateEnd == null || !(selectedDateStart.Value > selectedDateEnd.Value));
        SelectedDateStart = selectedDateStart;
        SelectedDateEnd = selectedDateEnd;
        CurrentDate = currentDate;
        OnChanged = onChanged;
        FirstDate = firstDate;
        LastDate = lastDate;
        DisplayedMonth = displayedMonth;
        SelectableDayPredicate = selectableDayPredicate;
        CalendarDelegate = calendarDelegate;
    }

    /// <summary>The currently selected start date.</summary>
    public DateTime? SelectedDateStart { get; }

    /// <summary>The currently selected end date.</summary>
    public DateTime? SelectedDateEnd { get; }

    /// <summary>The current date at the time the picker is displayed.</summary>
    public DateTime CurrentDate { get; }

    /// <summary>Called when the user picks a day.</summary>
    public Action<DateTime> OnChanged { get; }

    /// <summary>The earliest date the user is permitted to pick.</summary>
    public DateTime FirstDate { get; }

    /// <summary>The latest date the user is permitted to pick.</summary>
    public DateTime LastDate { get; }

    /// <summary>The month whose days are displayed by this picker.</summary>
    public DateTime DisplayedMonth { get; }

    public SelectableDayForRangePredicate? SelectableDayPredicate { get; }

    public CalendarDelegate<DateTime> CalendarDelegate { get; }

    public override State CreateState() => new MonthItemState();
}

/// <summary>Dart's private <c>_MonthItemState</c>.</summary>
internal sealed class MonthItemState : State<MonthItem>
{
    /// <summary>List of <see cref="FocusNode"/>s, one for each day of the month.</summary>
    private List<FocusNode> _dayFocusNodes = null!;

    public override void InitState()
    {
        base.InitState();
        int daysInMonth = Widget.CalendarDelegate.GetDaysInMonth(
            Widget.DisplayedMonth.Year,
            Widget.DisplayedMonth.Month);
        _dayFocusNodes = Enumerable.Range(0, daysInMonth)
            .Select(index => new FocusNode(skipTraversal: true, debugLabel: $"Day {index + 1}"))
            .ToList();
    }

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        // Check to see if the focused date is in this month, if so focus it.
        DateTime? focusedDate = DateRangeFocusedDate.MaybeOf(Context)?.Date;
        if (focusedDate != null && Widget.CalendarDelegate.IsSameMonth(Widget.DisplayedMonth, focusedDate))
        {
            _dayFocusNodes[focusedDate.Value.Day - 1].RequestFocus();
        }
    }

    public override void Dispose()
    {
        foreach (FocusNode node in _dayFocusNodes)
        {
            node.Dispose();
        }

        base.Dispose();
    }

    private static Color HighlightColor(BuildContext context)
    {
        return DatePickerTheme.Of(context).RangeSelectionBackgroundColor
            ?? DatePickerTheme.Defaults(context).RangeSelectionBackgroundColor!;
    }

    private void DayFocusChanged(bool focused)
    {
        if (focused)
        {
            TraversalDirection? focusDirection = DateRangeFocusedDate.MaybeOf(Context)?.ScrollDirection;
            if (focusDirection != null)
            {
                ScrollPositionAlignmentPolicy policy = ScrollPositionAlignmentPolicy.Explicit;
                switch (focusDirection.Value)
                {
                    case TraversalDirection.Up:
                    case TraversalDirection.Left:
                        policy = ScrollPositionAlignmentPolicy.KeepVisibleAtStart;
                        break;
                    case TraversalDirection.Right:
                    case TraversalDirection.Down:
                        policy = ScrollPositionAlignmentPolicy.KeepVisibleAtEnd;
                        break;
                }

                _ = Scrollable.EnsureVisible(
                    FocusManager.Instance.PrimaryFocus!.Context!,
                    duration: DateRangePickerConstants.MonthScrollDuration,
                    alignmentPolicy: policy);
            }
        }
    }

    private Widget BuildDayItem(BuildContext context, DateTime dayToBuild, int firstDayOffset, int daysInMonth)
    {
        int day = dayToBuild.Day;

        bool isDisabled = dayToBuild > Widget.LastDate
            || dayToBuild < Widget.FirstDate
            || (Widget.SelectableDayPredicate != null
                && !Widget.SelectableDayPredicate(dayToBuild, Widget.SelectedDateStart, Widget.SelectedDateEnd));
        bool isRangeSelected = Widget.SelectedDateStart != null && Widget.SelectedDateEnd != null;
        bool isSelectedDayStart = Widget.SelectedDateStart != null && dayToBuild == Widget.SelectedDateStart.Value;
        bool isSelectedDayEnd = Widget.SelectedDateEnd != null && dayToBuild == Widget.SelectedDateEnd.Value;
        bool isInRange = isRangeSelected
            && dayToBuild > Widget.SelectedDateStart!.Value
            && dayToBuild < Widget.SelectedDateEnd!.Value;
        bool isOneDayRange = isRangeSelected && Widget.SelectedDateStart == Widget.SelectedDateEnd;
        bool isToday = Widget.CalendarDelegate.IsSameDay(Widget.CurrentDate, dayToBuild);

        return new DayItem(
            calendarDelegate: Widget.CalendarDelegate,
            day: dayToBuild,
            focusNode: _dayFocusNodes[day - 1],
            onChanged: Widget.OnChanged,
            onFocusChange: DayFocusChanged,
            highlightColor: HighlightColor(context),
            isDisabled: isDisabled,
            isRangeSelected: isRangeSelected,
            isSelectedDayStart: isSelectedDayStart,
            isSelectedDayEnd: isSelectedDayEnd,
            isInRange: isInRange,
            isOneDayRange: isOneDayRange,
            isToday: isToday);
    }

    private static Widget BuildEdgeBox(BuildContext context, bool isHighlighted)
    {
        Widget empty = new LimitedBox(maxWidth: 0.0, maxHeight: 0.0, child: SizedBox.Expand());
        return isHighlighted ? new ColoredBox(HighlightColor(context), child: empty) : empty;
    }

    public override Widget Build(BuildContext context)
    {
        ThemeData themeData = Theme.Of(context);
        TextTheme textTheme = themeData.TextTheme;
        MaterialLocalizations localizations = MaterialLocalizations.Of(context);
        int year = Widget.DisplayedMonth.Year;
        int month = Widget.DisplayedMonth.Month;
        int daysInMonth = Widget.CalendarDelegate.GetDaysInMonth(year, month);
        int dayOffset = Widget.CalendarDelegate.FirstDayOffset(year, month, localizations);
        int weeks = (int)Math.Ceiling((daysInMonth + dayOffset) / (double)DateRangePickerConstants.DaysPerWeek);
        double gridHeight = weeks * DateRangePickerConstants.MonthItemRowHeight
            + (weeks - 1) * DateRangePickerConstants.MonthItemSpaceBetweenRows;
        var dayItems = new List<Widget>();

        // 1-based day of month, e.g. 1-31 for January, and 1-29 for February on
        // a leap year.
        for (int day = 0 - dayOffset + 1; day <= daysInMonth; day += 1)
        {
            if (day < 1)
            {
                dayItems.Add(new LimitedBox(maxWidth: 0.0, maxHeight: 0.0, child: SizedBox.Expand()));
            }
            else
            {
                DateTime dayToBuild = Widget.CalendarDelegate.GetDay(year, month, day);
                Widget dayItem = BuildDayItem(context, dayToBuild, dayOffset, daysInMonth);
                dayItems.Add(dayItem);
            }
        }

        // Add the leading/trailing edge containers to each week in order to
        // correctly extend the range highlight.
        var paddedDayItems = new List<Widget>();
        for (int i = 0; i < weeks; i++)
        {
            int start = i * DateRangePickerConstants.DaysPerWeek;
            int end = Math.Min(start + DateRangePickerConstants.DaysPerWeek, dayItems.Count);
            List<Widget> weekList = dayItems.GetRange(start, end - start);

            DateTime dateAfterLeadingPadding = Widget.CalendarDelegate.GetDay(year, month, start - dayOffset + 1);
            // Only color the edge container if it is after the start date and
            // on/before the end date.
            bool isLeadingInRange = !(dayOffset > 0 && i == 0)
                && Widget.SelectedDateStart != null
                && Widget.SelectedDateEnd != null
                && dateAfterLeadingPadding > Widget.SelectedDateStart.Value
                && !(dateAfterLeadingPadding > Widget.SelectedDateEnd.Value);
            weekList.Insert(0, BuildEdgeBox(context, isLeadingInRange));

            // Only add a trailing edge container if it is for a full week and not a
            // partial week.
            if (end < dayItems.Count
                || (end == dayItems.Count && dayItems.Count % DateRangePickerConstants.DaysPerWeek == 0))
            {
                DateTime dateBeforeTrailingPadding = Widget.CalendarDelegate.GetDay(year, month, end - dayOffset);
                // Only color the edge container if it is on/after the start date and
                // before the end date.
                bool isTrailingInRange = Widget.SelectedDateStart != null
                    && Widget.SelectedDateEnd != null
                    && !(dateBeforeTrailingPadding < Widget.SelectedDateStart.Value)
                    && dateBeforeTrailingPadding < Widget.SelectedDateEnd.Value;
                weekList.Add(BuildEdgeBox(context, isTrailingInRange));
            }

            paddedDayItems.AddRange(weekList);
        }

        double maxWidth = MediaQuery.OrientationOf(context) == Orientation.Landscape
            ? DateRangePickerConstants.MaxCalendarWidthLandscape
            : DateRangePickerConstants.MaxCalendarWidthPortrait;
        return new Column(
            children:
            [
                new ConstrainedBox(
                    new BoxConstraints(MaxWidth: maxWidth)
                        .Tighten(height: DateRangePickerConstants.MonthItemHeaderHeight),
                    new Padding(
                        EdgeInsets.Symmetric(horizontal: 16),
                        new Align(
                            alignment: AlignmentDirectional.CenterStart,
                            child: new ExcludeSemantics(
                                child: new Text(
                                    Widget.CalendarDelegate.FormatMonthYear(Widget.DisplayedMonth, localizations),
                                    style: textTheme.BodyMedium!.Apply(color: themeData.ColorScheme.OnSurface)))))),
                new ConstrainedBox(
                    new BoxConstraints(MaxWidth: maxWidth, MaxHeight: gridHeight),
                    GridView.Custom(
                        physics: new NeverScrollableScrollPhysics(),
                        gridDelegate: DateRangePickerConstants.MonthItemGridDelegateInstance,
                        childrenDelegate: new SliverChildListDelegate(paddedDayItems, addRepaintBoundaries: false))),
                new SizedBox(height: DateRangePickerConstants.MonthItemFooterHeight),
            ]);
    }
}

/// <summary>Dart's private <c>_DayItem</c>.</summary>
internal sealed class DayItem : StatefulWidget
{
    public DayItem(
        DateTime day,
        FocusNode focusNode,
        Action<DateTime> onChanged,
        Action<bool> onFocusChange,
        Color highlightColor,
        bool isDisabled,
        bool isRangeSelected,
        bool isSelectedDayStart,
        bool isSelectedDayEnd,
        bool isInRange,
        bool isOneDayRange,
        bool isToday,
        CalendarDelegate<DateTime> calendarDelegate,
        Key? key = null) : base(key)
    {
        Day = day;
        FocusNode = focusNode;
        OnChanged = onChanged;
        OnFocusChange = onFocusChange;
        HighlightColor = highlightColor;
        IsDisabled = isDisabled;
        IsRangeSelected = isRangeSelected;
        IsSelectedDayStart = isSelectedDayStart;
        IsSelectedDayEnd = isSelectedDayEnd;
        IsInRange = isInRange;
        IsOneDayRange = isOneDayRange;
        IsToday = isToday;
        CalendarDelegate = calendarDelegate;
    }

    public DateTime Day { get; }
    public FocusNode FocusNode { get; }
    public Action<DateTime> OnChanged { get; }
    public Action<bool> OnFocusChange { get; }
    public Color HighlightColor { get; }
    public bool IsDisabled { get; }
    public bool IsRangeSelected { get; }
    public bool IsSelectedDayStart { get; }
    public bool IsSelectedDayEnd { get; }
    public bool IsInRange { get; }
    public bool IsOneDayRange { get; }
    public bool IsToday { get; }
    public CalendarDelegate<DateTime> CalendarDelegate { get; }

    public override State CreateState() => new DayItemState();
}

/// <summary>Dart's private <c>_DayItemState</c>.</summary>
internal sealed class DayItemState : State<DayItem>
{
    private readonly WidgetStatesController _statesController = new();

    public override void Dispose()
    {
        _statesController.Dispose();
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        ThemeData theme = Theme.Of(context);
        ColorScheme colorScheme = theme.ColorScheme;
        TextTheme textTheme = theme.TextTheme;
        MaterialLocalizations localizations = MaterialLocalizations.Of(context);
        DatePickerThemeData datePickerTheme = DatePickerTheme.Of(context);
        DatePickerThemeData defaults = DatePickerTheme.Defaults(context);
        TextDirection textDirection = Directionality.Of(context);
        Color highlightColor = Widget.HighlightColor;

        ShapeDecoration? decoration = null;
        TextStyle? itemStyle = textTheme.BodyMedium;

        T? EffectiveValue<T>(Func<DatePickerThemeData?, T?> getProperty)
        {
            return getProperty(datePickerTheme) ?? getProperty(defaults);
        }

        T? Resolve<T>(
            Func<DatePickerThemeData?, WidgetStateProperty<T>?> getProperty,
            IReadOnlySet<WidgetState> resolveStates)
        {
            return EffectiveValue(theme =>
                getProperty(theme) is { } property ? property.Resolve(resolveStates) : default);
        }

        var states = new HashSet<WidgetState>();
        if (Widget.IsDisabled)
        {
            states.Add(WidgetState.Disabled);
        }

        if (Widget.IsSelectedDayStart || Widget.IsSelectedDayEnd)
        {
            states.Add(WidgetState.Selected);
        }

        _statesController.Value = states;

        Color? dayForegroundColor = Resolve(theme => theme?.DayForegroundColor, states);
        Color? dayBackgroundColor = Resolve(theme => theme?.DayBackgroundColor, states);
        WidgetStateProperty<Color?> dayOverlayColor = WidgetStateProperty<Color?>.ResolveWith(
            overlayStates => EffectiveValue(theme => Widget.IsInRange
                ? theme?.RangeSelectionOverlayColor?.Resolve(overlayStates)
                : theme?.DayOverlayColor?.Resolve(overlayStates)));

        OutlinedBorder dayShape = Resolve(theme => theme?.DayShape, states) ?? new CircleBorder();

        HighlightPainter? highlightPainter = null;

        if (Widget.IsSelectedDayStart || Widget.IsSelectedDayEnd)
        {
            // The selected start and end dates get a custom shaped background
            // highlight, and a contrasting text color.
            itemStyle = itemStyle?.Apply(color: dayForegroundColor);
            decoration = new ShapeDecoration(Color: dayBackgroundColor, Shape: dayShape);

            if (Widget.IsRangeSelected && !Widget.IsOneDayRange)
            {
                HighlightPainterStyle style = Widget.IsSelectedDayStart
                    ? HighlightPainterStyle.HighlightTrailing
                    : HighlightPainterStyle.HighlightLeading;
                highlightPainter = new HighlightPainter(
                    color: highlightColor,
                    style: style,
                    textDirection: textDirection);
            }
        }
        else if (Widget.IsInRange)
        {
            // The days within the range get a light background highlight.
            highlightPainter = new HighlightPainter(
                color: highlightColor,
                style: HighlightPainterStyle.HighlightAll,
                textDirection: textDirection);
            if (Widget.IsDisabled)
            {
                itemStyle = itemStyle?.Apply(color: colorScheme.OnSurface.WithOpacity(0.38));
            }
        }
        else if (Widget.IsDisabled)
        {
            itemStyle = itemStyle?.Apply(color: colorScheme.OnSurface.WithOpacity(0.38));
        }
        else if (Widget.IsToday)
        {
            // The current day gets a different text color and a custom shape border.
            itemStyle = itemStyle?.Apply(color: colorScheme.Primary);
            BorderSide todaySide = (datePickerTheme.TodayBorder ?? defaults.TodayBorder!.Value).CopyWith(
                color: colorScheme.Primary);

            decoration = new ShapeDecoration(Shape: dayShape.CopyWith(side: todaySide));
        }

        string dayText = localizations.FormatDecimal(Widget.Day.Day);

        // We want the day of month to be spoken first irrespective of the
        // locale-specific preferences or TextDirection. This is because
        // an accessibility user is more likely to be interested in the
        // day of month before the rest of the date, as they are looking
        // for the day of month. To do that we prepend day of month to the
        // formatted full date.
        string semanticLabelSuffix = Widget.IsToday ? $", {localizations.CurrentDateLabel}" : string.Empty;
        string semanticLabel =
            $"{dayText}, {Widget.CalendarDelegate.FormatFullDate(Widget.Day, localizations)}{semanticLabelSuffix}";
        if (Widget.IsSelectedDayStart)
        {
            semanticLabel = localizations.DateRangeStartDateSemanticLabel(semanticLabel);
        }
        else if (Widget.IsSelectedDayEnd)
        {
            semanticLabel = localizations.DateRangeEndDateSemanticLabel(semanticLabel);
        }

        Widget dayWidget = new Container(
            decoration: decoration,
            alignment: Alignment.Center,
            child: new Semantics(
                label: semanticLabel,
                selected: Widget.IsSelectedDayStart || Widget.IsSelectedDayEnd,
                child: new ExcludeSemantics(child: new Text(dayText, style: itemStyle))));

        if (highlightPainter != null)
        {
            dayWidget = new CustomPaint(painter: highlightPainter, child: dayWidget);
        }

        if (!Widget.IsDisabled)
        {
            dayWidget = new InkResponse(
                focusNode: Widget.FocusNode,
                onTap: () => Widget.OnChanged(Widget.Day),
                customBorder: dayShape,
                containedInkWell: true,
                statesController: _statesController,
                overlayColor: dayOverlayColor,
                onFocusChange: Widget.OnFocusChange,
                child: dayWidget);
        }

        return dayWidget;
    }
}

/// <summary>Determines which style to use to paint the highlight.</summary>
/// <remarks>Dart's private <c>_HighlightPainterStyle</c>.</remarks>
internal enum HighlightPainterStyle
{
    /// <summary>Paints nothing.</summary>
    None,

    /// <summary>Paints a rectangle that occupies the leading half of the space.</summary>
    HighlightLeading,

    /// <summary>Paints a rectangle that occupies the trailing half of the space.</summary>
    HighlightTrailing,

    /// <summary>Paints a rectangle that occupies all available space.</summary>
    HighlightAll,
}

/// <summary>
/// This custom painter will add a background highlight to its child.
/// </summary>
/// <remarks>
/// Dart's private <c>_HighlightPainter</c>. This highlight will be drawn depending on the
/// <see cref="Style"/>, <see cref="Color"/>, and <see cref="TextDirection"/> supplied.
/// </remarks>
internal sealed class HighlightPainter : CustomPainter
{
    public HighlightPainter(
        Color color,
        HighlightPainterStyle style = HighlightPainterStyle.None,
        TextDirection? textDirection = null)
    {
        Color = color;
        Style = style;
        TextDirection = textDirection;
    }

    public Color Color { get; }
    public HighlightPainterStyle Style { get; }
    public TextDirection? TextDirection { get; }

    public override void Paint(PaintingContext context, Size size)
    {
        if (Style == HighlightPainterStyle.None)
        {
            return;
        }

        var paint = new Paint { Color = Color, Style = PaintingStyle.Fill };

        bool rtl = TextDirection switch
        {
            Plumix.UI.TextDirection.Ltr => false,
            _ => true,
        };

        switch (Style)
        {
            case HighlightPainterStyle.HighlightLeading when rtl:
            case HighlightPainterStyle.HighlightTrailing when !rtl:
                context.Canvas.DrawRect(new Rect(size.Width / 2, 0, size.Width / 2, size.Height), paint);
                break;
            case HighlightPainterStyle.HighlightLeading:
            case HighlightPainterStyle.HighlightTrailing:
                context.Canvas.DrawRect(new Rect(0, 0, size.Width / 2, size.Height), paint);
                break;
            case HighlightPainterStyle.HighlightAll:
                context.Canvas.DrawRect(new Rect(0, 0, size.Width, size.Height), paint);
                break;
            case HighlightPainterStyle.None:
                break;
        }
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate) => false;
}

/// <summary>Dart's private <c>_InputDateRangePickerDialog</c>.</summary>
internal sealed class InputDateRangePickerDialog : StatelessWidget
{
    public InputDateRangePickerDialog(
        DateTime? selectedStartDate,
        DateTime? selectedEndDate,
        DateTime? currentDate,
        Widget picker,
        Action onConfirm,
        Action onCancel,
        string? confirmText,
        string? cancelText,
        string? helpText,
        Widget? entryModeButton,
        CalendarDelegate<DateTime> calendarDelegate,
        Key? key = null) : base(key)
    {
        SelectedStartDate = selectedStartDate;
        SelectedEndDate = selectedEndDate;
        CurrentDate = currentDate;
        Picker = picker;
        OnConfirm = onConfirm;
        OnCancel = onCancel;
        ConfirmText = confirmText;
        CancelText = cancelText;
        HelpText = helpText;
        EntryModeButton = entryModeButton;
        CalendarDelegate = calendarDelegate;
    }

    public DateTime? SelectedStartDate { get; }
    public DateTime? SelectedEndDate { get; }
    public DateTime? CurrentDate { get; }
    public Widget Picker { get; }
    public Action OnConfirm { get; }
    public Action OnCancel { get; }
    public string? ConfirmText { get; }
    public string? CancelText { get; }
    public string? HelpText { get; }
    public Widget? EntryModeButton { get; }
    public CalendarDelegate<DateTime> CalendarDelegate { get; }

    private string FormatDateRange(BuildContext context, DateTime? start, DateTime? end, DateTime now)
    {
        MaterialLocalizations localizations = MaterialLocalizations.Of(context);
        string startText = MaterialDatePickers.FormatRangeStartDate(localizations, CalendarDelegate, start, end);
        string endText = MaterialDatePickers.FormatRangeEndDate(localizations, CalendarDelegate, start, end, now);
        if (start == null || end == null)
        {
            return localizations.UnspecifiedDateRange;
        }

        return Directionality.Of(context) switch
        {
            Plumix.UI.TextDirection.Rtl => $"{endText} – {startText}",
            _ => $"{startText} – {endText}",
        };
    }

    public override Widget Build(BuildContext context)
    {
        bool useMaterial3 = Theme.Of(context).UseMaterial3;
        MaterialLocalizations localizations = MaterialLocalizations.Of(context);
        Orientation orientation = MediaQuery.OrientationOf(context);
        DatePickerThemeData datePickerTheme = DatePickerTheme.Of(context);
        DatePickerThemeData defaults = DatePickerTheme.Defaults(context);

        // There's no M3 spec for a landscape layout input (not calendar)
        // date range picker. To ensure that the date range displayed in the
        // input date range picker's header fits in landscape mode, we override
        // the M3 default here.
        TextStyle? headlineStyle = orientation == Orientation.Portrait
            ? datePickerTheme.HeaderHeadlineStyle ?? defaults.HeaderHeadlineStyle
            : Theme.Of(context).TextTheme.HeadlineSmall;

        Color? headerForegroundColor = datePickerTheme.HeaderForegroundColor ?? defaults.HeaderForegroundColor;
        headlineStyle = headlineStyle?.CopyWith(color: headerForegroundColor);

        string dateText = FormatDateRange(context, SelectedStartDate, SelectedEndDate, CurrentDate!.Value);
        string semanticDateText = SelectedStartDate != null && SelectedEndDate != null
            ? $"{CalendarDelegate.FormatMediumDate(SelectedStartDate.Value, localizations)} – "
              + CalendarDelegate.FormatMediumDate(SelectedEndDate.Value, localizations)
            : string.Empty;

        Widget header = new DatePickerHeader(
            helpText: HelpText
                ?? (useMaterial3
                    ? localizations.DateRangePickerHelpText
                    : localizations.DateRangePickerHelpText.ToUpperInvariant()),
            titleText: dateText,
            titleSemanticsLabel: semanticDateText,
            titleStyle: headlineStyle,
            orientation: orientation,
            isShort: orientation == Orientation.Landscape,
            entryModeButton: EntryModeButton);

        Widget actions = new ConstrainedBox(
            new BoxConstraints(MinHeight: 52.0),
            new Padding(
                EdgeInsets.Symmetric(horizontal: 8),
                new Align(
                    alignment: AlignmentDirectional.CenterEnd,
                    child: new OverflowBar(
                        spacing: 8,
                        children:
                        [
                            new TextButton(
                                onPressed: OnCancel,
                                child: new Text(
                                    CancelText
                                    ?? (useMaterial3
                                        ? localizations.CancelButtonLabel
                                        : localizations.CancelButtonLabel.ToUpperInvariant()))),
                            new TextButton(
                                onPressed: OnConfirm,
                                child: new Text(ConfirmText ?? localizations.OkButtonLabel)),
                        ]))));

        double textScaleFactor = MediaQuery.TextScalerOf(context)
                .Clamp(maxScaleFactor: DatePickerConstants.KMaxRangeTextScaleFactor)
                .Scale(DatePickerConstants.FontSizeToScale)
            / DatePickerConstants.FontSizeToScale;
        Size baseDialogSize = useMaterial3
            ? DatePickerConstants.InputPortraitDialogSizeM3
            : DatePickerConstants.InputPortraitDialogSizeM2;
        var dialogSize = new Size(baseDialogSize.Width * textScaleFactor, baseDialogSize.Height * textScaleFactor);
        switch (orientation)
        {
            case Orientation.Portrait:
                return new LayoutBuilder((_, constraints) =>
                {
                    Size portraitDialogSize = useMaterial3
                        ? DatePickerConstants.InputPortraitDialogSizeM3
                        : DatePickerConstants.InputPortraitDialogSizeM2;
                    // Make sure the portrait dialog can fit the contents comfortably when
                    // resized from the landscape dialog.
                    bool isFullyPortrait =
                        constraints.MaxHeight >= Math.Min(dialogSize.Height, portraitDialogSize.Height);

                    // When the portrait dialog does not fit vertically, hide the header.
                    var children = new List<Widget>();
                    if (isFullyPortrait)
                    {
                        children.Add(header);
                    }

                    children.Add(new Expanded(child: Picker));
                    children.Add(actions);
                    return new Column(
                        mainAxisSize: MainAxisSize.Min,
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        children: children);
                });

            case Orientation.Landscape:
            default:
                return new Row(
                    mainAxisSize: MainAxisSize.Min,
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    children:
                    [
                        header,
                        new Flexible(
                            child: new Column(
                                mainAxisSize: MainAxisSize.Min,
                                crossAxisAlignment: CrossAxisAlignment.Stretch,
                                children:
                                [
                                    new Expanded(child: Picker),
                                    actions,
                                ])),
                    ]);
        }
    }
}

/// <summary>Provides a pair of text fields that allow the user to enter the start and end dates that
/// represent a range of dates.</summary>
/// <remarks>Dart's private <c>_InputDateRangePicker</c>.</remarks>
internal sealed class InputDateRangePicker : StatefulWidget
{
    /// <summary>Creates a row with two text fields configured to accept the start and end dates of a date
    /// range.</summary>
    public InputDateRangePicker(
        DateTime firstDate,
        DateTime lastDate,
        Action<DateTime?>? onStartDateChanged,
        Action<DateTime?>? onEndDateChanged,
        SelectableDayForRangePredicate? selectableDayPredicate,
        CalendarDelegate<DateTime> calendarDelegate,
        DateTime? initialStartDate = null,
        DateTime? initialEndDate = null,
        string? helpText = null,
        string? errorFormatText = null,
        string? errorInvalidText = null,
        string? errorInvalidRangeText = null,
        string? fieldStartHintText = null,
        string? fieldEndHintText = null,
        string? fieldStartLabelText = null,
        string? fieldEndLabelText = null,
        bool autofocus = false,
        bool autovalidate = false,
        TextInputType? keyboardType = null,
        Key? key = null) : base(key)
    {
        InitialStartDate = initialStartDate == null ? null : calendarDelegate.DateOnly(initialStartDate.Value);
        InitialEndDate = initialEndDate == null ? null : calendarDelegate.DateOnly(initialEndDate.Value);
        FirstDate = calendarDelegate.DateOnly(firstDate);
        LastDate = calendarDelegate.DateOnly(lastDate);
        OnStartDateChanged = onStartDateChanged;
        OnEndDateChanged = onEndDateChanged;
        SelectableDayPredicate = selectableDayPredicate;
        CalendarDelegate = calendarDelegate;
        HelpText = helpText;
        ErrorFormatText = errorFormatText;
        ErrorInvalidText = errorInvalidText;
        ErrorInvalidRangeText = errorInvalidRangeText;
        FieldStartHintText = fieldStartHintText;
        FieldEndHintText = fieldEndHintText;
        FieldStartLabelText = fieldStartLabelText;
        FieldEndLabelText = fieldEndLabelText;
        Autofocus = autofocus;
        Autovalidate = autovalidate;
        KeyboardType = keyboardType ?? TextInputType.Datetime;
    }

    /// <summary>The <see cref="DateTime"/> that represents the start of the initial date range
    /// selection.</summary>
    public DateTime? InitialStartDate { get; }

    /// <summary>The <see cref="DateTime"/> that represents the end of the initial date range
    /// selection.</summary>
    public DateTime? InitialEndDate { get; }

    /// <summary>The earliest allowable <see cref="DateTime"/> that the user can select.</summary>
    public DateTime FirstDate { get; }

    /// <summary>The latest allowable <see cref="DateTime"/> that the user can select.</summary>
    public DateTime LastDate { get; }

    /// <summary>Called when the user changes the start date of the selected range.</summary>
    public Action<DateTime?>? OnStartDateChanged { get; }

    /// <summary>Called when the user changes the end date of the selected range.</summary>
    public Action<DateTime?>? OnEndDateChanged { get; }

    /// <summary>The text that is displayed at the top of the header.</summary>
    public string? HelpText { get; }

    /// <summary>Error text used to indicate the text in a field is not a valid date.</summary>
    public string? ErrorFormatText { get; }

    /// <summary>Error text used to indicate the date in a field is not in the valid range of
    /// <see cref="FirstDate"/> - <see cref="LastDate"/>.</summary>
    public string? ErrorInvalidText { get; }

    /// <summary>Error text used to indicate the dates given don't form a valid date range (i.e. the start
    /// date is after the end date).</summary>
    public string? ErrorInvalidRangeText { get; }

    /// <summary>Hint text shown when the start date field is empty.</summary>
    public string? FieldStartHintText { get; }

    /// <summary>Hint text shown when the end date field is empty.</summary>
    public string? FieldEndHintText { get; }

    /// <summary>Label used for the start date field.</summary>
    public string? FieldStartLabelText { get; }

    /// <summary>Label used for the end date field.</summary>
    public string? FieldEndLabelText { get; }

    /// <summary>Whether the start date field should be autofocused.</summary>
    public bool Autofocus { get; }

    /// <summary>If true, the date fields will validate and update their error text immediately after every
    /// change. Otherwise, you must call <see cref="InputDateRangePickerState.Validate"/> to validate.</summary>
    public bool Autovalidate { get; }

    /// <summary>The keyboard type of the <see cref="TextField"/>.</summary>
    public TextInputType KeyboardType { get; }

    public SelectableDayForRangePredicate? SelectableDayPredicate { get; }

    public CalendarDelegate<DateTime> CalendarDelegate { get; }

    public override State CreateState() => new InputDateRangePickerState();
}

/// <summary>The current state of an <see cref="InputDateRangePicker"/>. Can be used to
/// <see cref="Validate"/> the date field entries.</summary>
/// <remarks>Dart's private <c>_InputDateRangePickerState</c>.</remarks>
internal sealed class InputDateRangePickerState : State<InputDateRangePicker>
{
    private string _startInputText = null!;
    private string _endInputText = null!;
    private DateTime? _startDate;
    private DateTime? _endDate;
    private TextEditingController _startController = null!;
    private TextEditingController _endController = null!;
    private string? _startErrorText;
    private string? _endErrorText;
    private bool _autoSelected;

    public override void InitState()
    {
        base.InitState();
        _startDate = Widget.InitialStartDate;
        _startController = new TextEditingController();
        _endDate = Widget.InitialEndDate;
        _endController = new TextEditingController();
    }

    public override void Dispose()
    {
        _startController.Dispose();
        _endController.Dispose();
        base.Dispose();
    }

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        MaterialLocalizations localizations = MaterialLocalizations.Of(Context);
        if (_startDate != null)
        {
            _startInputText = Widget.CalendarDelegate.FormatCompactDate(_startDate.Value, localizations);
            bool selectText = Widget.Autofocus && !_autoSelected;
            UpdateController(_startController, _startInputText, selectText);
            _autoSelected = selectText;
        }

        if (_endDate != null)
        {
            _endInputText = Widget.CalendarDelegate.FormatCompactDate(_endDate.Value, localizations);
            UpdateController(_endController, _endInputText, false);
        }
    }

    /// <summary>Validates that the text in the start and end fields represent a valid date range.</summary>
    /// <returns>True if the range is valid. If not, it will return false and display an appropriate error
    /// message under one of the text fields.</returns>
    public bool Validate()
    {
        string? startError = ValidateDate(_startDate);
        string? endError = ValidateDate(_endDate);
        if (startError == null && endError == null)
        {
            if (_startDate!.Value > _endDate!.Value)
            {
                startError = Widget.ErrorInvalidRangeText ?? MaterialLocalizations.Of(Context).InvalidDateRangeLabel;
            }
        }

        SetState(() =>
        {
            _startErrorText = startError;
            _endErrorText = endError;
        });
        return startError == null && endError == null;
    }

    private DateTime? ParseDate(string? text)
    {
        MaterialLocalizations localizations = MaterialLocalizations.Of(Context);
        return Widget.CalendarDelegate.ParseCompactDate(text, localizations);
    }

    private string? ValidateDate(DateTime? date)
    {
        if (date == null)
        {
            return Widget.ErrorFormatText ?? MaterialLocalizations.Of(Context).InvalidDateFormatLabel;
        }
        else if (!IsDaySelectable(date.Value))
        {
            return Widget.ErrorInvalidText ?? MaterialLocalizations.Of(Context).DateOutOfRangeLabel;
        }

        return null;
    }

    private bool IsDaySelectable(DateTime day)
    {
        if (day < Widget.FirstDate || day > Widget.LastDate)
        {
            return false;
        }

        if (Widget.SelectableDayPredicate == null)
        {
            return true;
        }

        return Widget.SelectableDayPredicate(day, _startDate, _endDate);
    }

    private static void UpdateController(TextEditingController controller, string text, bool selectText)
    {
        TextEditingValue textEditingValue = controller.Value.CopyWith(text: text);
        if (selectText)
        {
            textEditingValue = textEditingValue.CopyWith(
                selection: new TextSelection(BaseOffset: 0, ExtentOffset: text.Length));
        }

        controller.Value = textEditingValue;
    }

    private void HandleStartChanged(string text)
    {
        SetState(() =>
        {
            _startInputText = text;
            _startDate = ParseDate(text);
            Widget.OnStartDateChanged?.Invoke(_startDate);
        });
        if (Widget.Autovalidate)
        {
            Validate();
        }
    }

    private void HandleEndChanged(string text)
    {
        SetState(() =>
        {
            _endInputText = text;
            _endDate = ParseDate(text);
            Widget.OnEndDateChanged?.Invoke(_endDate);
        });
        if (Widget.Autovalidate)
        {
            Validate();
        }
    }

    public override Widget Build(BuildContext context)
    {
        ThemeData theme = Theme.Of(context);
        bool useMaterial3 = theme.UseMaterial3;
        MaterialLocalizations localizations = MaterialLocalizations.Of(context);
        InputDecorationThemeData inputTheme = InputDecorationTheme.Of(context);
        InputBorder inputBorder = inputTheme.Border
            ?? (useMaterial3 ? new OutlineInputBorder() : new UnderlineInputBorder());

        return new Row(
            crossAxisAlignment: CrossAxisAlignment.Start,
            children:
            [
                new Expanded(
                    child: new TextField(
                        controller: _startController,
                        decoration: new InputDecoration(
                            border: inputBorder,
                            filled: inputTheme.Filled,
                            hintText: Widget.FieldStartHintText ?? Widget.CalendarDelegate.DateHelpText(localizations),
                            labelText: Widget.FieldStartLabelText ?? localizations.DateRangeStartLabel,
                            errorText: _startErrorText),
                        keyboardType: Widget.KeyboardType,
                        onChanged: HandleStartChanged,
                        autofocus: Widget.Autofocus)),
                new SizedBox(width: 8),
                new Expanded(
                    child: new TextField(
                        controller: _endController,
                        decoration: new InputDecoration(
                            border: inputBorder,
                            filled: inputTheme.Filled,
                            hintText: Widget.FieldEndHintText ?? Widget.CalendarDelegate.DateHelpText(localizations),
                            labelText: Widget.FieldEndLabelText ?? localizations.DateRangeEndLabel,
                            errorText: _endErrorText),
                        keyboardType: Widget.KeyboardType,
                        onChanged: HandleEndChanged)),
            ]);
    }
}
