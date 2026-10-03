using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/date_picker.dart

public static partial class MaterialDatePickers
{
    /// <summary>Shows a dialog containing a Material Design date picker.</summary>
    /// <remarks>
    /// Dart's top-level <c>showDatePicker</c>. The returned <see cref="Task"/> resolves to the date selected
    /// by the user when the user confirms the dialog. If the user cancels the dialog, null is returned.
    /// Dart's function is <c>async</c>, so a failed assertion surfaces as a faulted task rather than a
    /// synchronous throw.
    /// <para>
    /// When the date picker is first displayed, if <paramref name="initialDate"/> is not null, it will show
    /// the month of <paramref name="initialDate"/>, with <paramref name="initialDate"/> selected. Otherwise it
    /// will show the <paramref name="currentDate"/>'s month. <paramref name="firstDate"/> is the earliest
    /// allowable date, <paramref name="lastDate"/> the latest; an <paramref name="initialDate"/> must fall
    /// between them (inclusive). Only the date part of each <see cref="DateTime"/> is considered.
    /// </para>
    /// <para>
    /// <paramref name="switchToInputEntryModeIcon"/> defaults to
    /// <c>Icon(useMaterial3 ? Icons.EditOutlined : Icons.Edit)</c> and
    /// <paramref name="switchToCalendarEntryModeIcon"/> to <c>Icon(Icons.CalendarToday)</c>. The
    /// <paramref name="builder"/> parameter can be used to wrap the dialog widget to add inherited widgets
    /// like <see cref="Theme"/>.
    /// </para>
    /// <para>
    /// Using this method will not enable state restoration for the date picker. In order to enable state
    /// restoration for a date picker, use <see cref="Navigator.RestorablePush"/> with
    /// <see cref="DatePickerDialog"/> (under a <c>MaterialApp(restorationScopeId: ...)</c>), for example
    /// from a <see cref="RestorableRouteFuture{T}"/> whose <c>onPresent</c> pushes a
    /// <see cref="DialogRoute{T}"/> building a <see cref="DatePickerDialog"/> with a
    /// <c>restorationId</c>.
    /// </para>
    /// </remarks>
    public static Task<DateTime?> ShowDatePicker(
        BuildContext context,
        DateTime firstDate,
        DateTime lastDate,
        DateTime? initialDate = null,
        DateTime? currentDate = null,
        DatePickerEntryMode initialEntryMode = DatePickerEntryMode.Calendar,
        SelectableDayPredicate? selectableDayPredicate = null,
        string? helpText = null,
        string? cancelText = null,
        string? confirmText = null,
        Locale? locale = null,
        bool barrierDismissible = true,
        Color? barrierColor = null,
        string? barrierLabel = null,
        bool useRootNavigator = true,
        RouteSettings? routeSettings = null,
        TextDirection? textDirection = null,
        TransitionBuilder? builder = null,
        DatePickerMode initialDatePickerMode = DatePickerMode.Day,
        string? errorFormatText = null,
        string? errorInvalidText = null,
        string? fieldHintText = null,
        string? fieldLabelText = null,
        TextInputType? keyboardType = null,
        Point? anchorPoint = null,
        Action<DatePickerEntryMode>? onDatePickerModeChange = null,
        Icon? switchToInputEntryModeIcon = null,
        Icon? switchToCalendarEntryModeIcon = null,
        CalendarDelegate<DateTime>? calendarDelegate = null)
    {
        CalendarDelegate<DateTime> effectiveCalendarDelegate = calendarDelegate ?? GregorianCalendarDelegate.Instance;
        try
        {
            initialDate = initialDate == null ? null : effectiveCalendarDelegate.DateOnly(initialDate.Value);
            firstDate = effectiveCalendarDelegate.DateOnly(firstDate);
            lastDate = effectiveCalendarDelegate.DateOnly(lastDate);
            DebugAssertions.Assert(
                !(lastDate < firstDate),
                $"lastDate {DateUtils.DartToString(lastDate)} must be on or after firstDate "
                + $"{DateUtils.DartToString(firstDate)}.");
            DebugAssertions.Assert(
                initialDate == null || !(initialDate.Value < firstDate),
                $"initialDate {DartToString(initialDate)} must be on or after firstDate "
                + $"{DateUtils.DartToString(firstDate)}.");
            DebugAssertions.Assert(
                initialDate == null || !(initialDate.Value > lastDate),
                $"initialDate {DartToString(initialDate)} must be on or before lastDate "
                + $"{DateUtils.DartToString(lastDate)}.");
            DebugAssertions.Assert(
                selectableDayPredicate == null || initialDate == null || selectableDayPredicate(initialDate.Value),
                $"Provided initialDate {DartToString(initialDate)} must satisfy provided selectableDayPredicate.");
            DebugAssertions.Assert(MaterialDebug.DebugCheckHasMaterialLocalizations(context));

            Widget dialog = new DatePickerDialog(
                initialDate: initialDate,
                firstDate: firstDate,
                lastDate: lastDate,
                currentDate: currentDate,
                initialEntryMode: initialEntryMode,
                selectableDayPredicate: selectableDayPredicate,
                helpText: helpText,
                cancelText: cancelText,
                confirmText: confirmText,
                initialCalendarMode: initialDatePickerMode,
                errorFormatText: errorFormatText,
                errorInvalidText: errorInvalidText,
                fieldHintText: fieldHintText,
                fieldLabelText: fieldLabelText,
                keyboardType: keyboardType,
                onDatePickerModeChange: onDatePickerModeChange,
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
            else
            {
                DatePickerThemeData datePickerTheme = DatePickerTheme.Of(context);
                if (datePickerTheme.Locale != null)
                {
                    dialog = Localizations.Override(context, dialog, locale: datePickerTheme.Locale);
                }
            }

            Widget capturedDialog = dialog;
            return MaterialDialogs.ShowDialog<DateTime?>(
                context,
                routeContext => builder == null ? capturedDialog : builder(routeContext, capturedDialog),
                barrierDismissible: barrierDismissible,
                barrierColor: barrierColor,
                barrierLabel: barrierLabel,
                useRootNavigator: useRootNavigator,
                routeSettings: routeSettings,
                anchorPoint: anchorPoint);
        }
        catch (AssertionError error)
        {
            return Task.FromException<DateTime?>(error);
        }
    }

    // Dart's string interpolation of a nullable DateTime (`$initialDate`).
    internal static string DartToString(DateTime? date) =>
        date == null ? "null" : DateUtils.DartToString(date.Value);
}

/// <summary>A Material-style date picker dialog.</summary>
/// <remarks>
/// It is used internally by <see cref="MaterialDatePickers.ShowDatePicker"/> or can be directly pushed
/// onto the <see cref="Navigator"/> stack to enable state restoration. See
/// <see cref="MaterialDatePickers.ShowDatePicker"/> for a state restoration app example.
/// </remarks>
public class DatePickerDialog : StatefulWidget
{
    /// <summary>A Material-style date picker dialog.</summary>
    public DatePickerDialog(
        DateTime firstDate,
        DateTime lastDate,
        DateTime? initialDate = null,
        DateTime? currentDate = null,
        DatePickerEntryMode initialEntryMode = DatePickerEntryMode.Calendar,
        SelectableDayPredicate? selectableDayPredicate = null,
        string? cancelText = null,
        string? confirmText = null,
        string? helpText = null,
        DatePickerMode initialCalendarMode = DatePickerMode.Day,
        string? errorFormatText = null,
        string? errorInvalidText = null,
        string? fieldHintText = null,
        string? fieldLabelText = null,
        TextInputType? keyboardType = null,
        string? restorationId = null,
        Action<DatePickerEntryMode>? onDatePickerModeChange = null,
        Icon? switchToInputEntryModeIcon = null,
        Icon? switchToCalendarEntryModeIcon = null,
        Thickness? insetPadding = null,
        CalendarDelegate<DateTime>? calendarDelegate = null,
        Key? key = null) : base(key)
    {
        InitialEntryMode = initialEntryMode;
        SelectableDayPredicate = selectableDayPredicate;
        CancelText = cancelText;
        ConfirmText = confirmText;
        HelpText = helpText;
        InitialCalendarMode = initialCalendarMode;
        ErrorFormatText = errorFormatText;
        ErrorInvalidText = errorInvalidText;
        FieldHintText = fieldHintText;
        FieldLabelText = fieldLabelText;
        KeyboardType = keyboardType;
        RestorationId = restorationId;
        OnDatePickerModeChange = onDatePickerModeChange;
        SwitchToInputEntryModeIcon = switchToInputEntryModeIcon;
        SwitchToCalendarEntryModeIcon = switchToCalendarEntryModeIcon;
        InsetPadding = insetPadding ?? new Thickness(16.0, 24.0);
        CalendarDelegate = calendarDelegate ?? GregorianCalendarDelegate.Instance;
        InitialDate = initialDate == null ? null : CalendarDelegate.DateOnly(initialDate.Value);
        FirstDate = CalendarDelegate.DateOnly(firstDate);
        LastDate = CalendarDelegate.DateOnly(lastDate);
        CurrentDate = CalendarDelegate.DateOnly(currentDate ?? CalendarDelegate.Now());
        DebugAssertions.Assert(
            !(LastDate < FirstDate),
            $"lastDate {DateUtils.DartToString(LastDate)} must be on or after firstDate "
            + $"{DateUtils.DartToString(FirstDate)}.");
        DebugAssertions.Assert(
            initialDate == null || !(InitialDate!.Value < FirstDate),
            $"initialDate {MaterialDatePickers.DartToString(InitialDate)} must be on or after firstDate "
            + $"{DateUtils.DartToString(FirstDate)}.");
        DebugAssertions.Assert(
            initialDate == null || !(InitialDate!.Value > LastDate),
            $"initialDate {MaterialDatePickers.DartToString(InitialDate)} must be on or before lastDate "
            + $"{DateUtils.DartToString(LastDate)}.");
        DebugAssertions.Assert(
            selectableDayPredicate == null
            || initialDate == null
            || selectableDayPredicate(InitialDate!.Value),
            $"Provided initialDate {MaterialDatePickers.DartToString(InitialDate)} must satisfy provided "
            + "selectableDayPredicate");
    }

    /// <summary>The initially selected <see cref="DateTime"/> that the picker should display.</summary>
    /// <remarks>
    /// If this is null, there is no selected date. A date must be selected to submit the dialog.
    /// </remarks>
    public DateTime? InitialDate { get; }

    /// <summary>The earliest allowable <see cref="DateTime"/> that the user can select.</summary>
    public DateTime FirstDate { get; }

    /// <summary>The latest allowable <see cref="DateTime"/> that the user can select.</summary>
    public DateTime LastDate { get; }

    /// <summary>The <see cref="DateTime"/> representing today. It will be highlighted in the day grid.</summary>
    public DateTime CurrentDate { get; }

    /// <summary>The initial mode of date entry method for the date picker dialog.</summary>
    public DatePickerEntryMode InitialEntryMode { get; }

    /// <summary>Function to provide full control over which <see cref="DateTime"/> can be selected.</summary>
    public SelectableDayPredicate? SelectableDayPredicate { get; }

    /// <summary>The text that is displayed on the cancel button.</summary>
    public string? CancelText { get; }

    /// <summary>The text that is displayed on the confirm button.</summary>
    public string? ConfirmText { get; }

    /// <summary>The text that is displayed at the top of the header.</summary>
    /// <remarks>This is used to indicate to the user what they are selecting a date for.</remarks>
    public string? HelpText { get; }

    /// <summary>The initial display of the calendar picker.</summary>
    public DatePickerMode InitialCalendarMode { get; }

    /// <summary>The error text displayed if the entered date is not in the correct format.</summary>
    public string? ErrorFormatText { get; }

    /// <summary>The error text displayed if the date is not valid.</summary>
    /// <remarks>
    /// A date is not valid if it is earlier than <see cref="FirstDate"/>, later than <see cref="LastDate"/>,
    /// or doesn't pass the <see cref="SelectableDayPredicate"/>.
    /// </remarks>
    public string? ErrorInvalidText { get; }

    /// <summary>The hint text displayed in the <see cref="TextField"/>.</summary>
    /// <remarks>
    /// If this is null, it will default to the date format string. For example, 'mm/dd/yyyy' for en_US.
    /// </remarks>
    public string? FieldHintText { get; }

    /// <summary>The label text displayed in the <see cref="TextField"/>.</summary>
    /// <remarks>
    /// If this is null, it will default to the words representing the date format string. For example,
    /// 'Month, Day, Year' for en_US.
    /// </remarks>
    public string? FieldLabelText { get; }

    /// <summary>The keyboard type of the <see cref="TextField"/>.</summary>
    /// <remarks>If this is null, it will default to <see cref="TextInputType.Datetime"/>.</remarks>
    public TextInputType? KeyboardType { get; }

    /// <summary>Restoration ID to save and restore the state of the <see cref="DatePickerDialog"/>.</summary>
    /// <remarks>
    /// If it is non-null, the date picker will persist and restore the date selected on the dialog. The
    /// state of this widget is persisted in a <see cref="RestorationBucket"/> claimed from the surrounding
    /// <see cref="RestorationScope"/> using the provided restoration ID.
    /// </remarks>
    public string? RestorationId { get; }

    /// <summary>
    /// Called when the <see cref="DatePickerDialog"/> is toggled between
    /// <see cref="DatePickerEntryMode.Calendar"/> and <see cref="DatePickerEntryMode.Input"/>.
    /// </summary>
    public Action<DatePickerEntryMode>? OnDatePickerModeChange { get; }

    /// <summary>
    /// The icon displayed in the corner of the dialog when <see cref="DatePickerEntryMode"/> is
    /// <see cref="DatePickerEntryMode.Calendar"/>; activating it switches to
    /// <see cref="DatePickerEntryMode.Input"/>.
    /// </summary>
    /// <remarks>If null, <c>Icon(useMaterial3 ? Icons.EditOutlined : Icons.Edit)</c> is used.</remarks>
    public Icon? SwitchToInputEntryModeIcon { get; }

    /// <summary>
    /// The icon displayed in the corner of the dialog when <see cref="DatePickerEntryMode"/> is
    /// <see cref="DatePickerEntryMode.Input"/>; activating it switches to
    /// <see cref="DatePickerEntryMode.Calendar"/>.
    /// </summary>
    /// <remarks>If null, <c>Icon(Icons.CalendarToday)</c> is used.</remarks>
    public Icon? SwitchToCalendarEntryModeIcon { get; }

    /// <summary>
    /// The amount of padding added to <see cref="MediaQueryData.ViewInsets"/> on the outside of the dialog.
    /// This defines the minimum space between the screen's edges and the dialog.
    /// </summary>
    /// <remarks>Defaults to <c>EdgeInsets.symmetric(horizontal: 16.0, vertical: 24.0)</c>.</remarks>
    public Thickness InsetPadding { get; }

    /// <summary>The calendar system the dialog interprets, navigates and formats dates with.</summary>
    public CalendarDelegate<DateTime> CalendarDelegate { get; }

    public override State CreateState() => new DatePickerDialogState();
}

/// <summary>Dart's private <c>_DatePickerDialogState</c>.</summary>
internal sealed class DatePickerDialogState : RestorationState<DatePickerDialog>
{
    private RestorableDateTimeN? _selectedDateValue;
    private RestorableDatePickerEntryMode? _entryModeValue;
    private readonly RestorableAutovalidateMode _autovalidateMode = new(AutovalidateMode.Disabled);

    // Dart's `late final` fields, initialized on first use (from RestoreState).
    private RestorableDateTimeN SelectedDate => _selectedDateValue ??= new(Widget.InitialDate);

    private RestorableDatePickerEntryMode EntryMode => _entryModeValue ??= new(Widget.InitialEntryMode);

    public override void Dispose()
    {
        SelectedDate.Dispose();
        EntryMode.Dispose();
        _autovalidateMode.Dispose();
        base.Dispose();
    }

    protected override string? RestorationId => Widget.RestorationId;

    protected override void RestoreState(RestorationBucket? oldBucket, bool initialRestore)
    {
        RegisterForRestoration(SelectedDate, "selected_date");
        RegisterForRestoration(_autovalidateMode, "autovalidateMode");
        RegisterForRestoration(EntryMode, "calendar_entry_mode");
    }

    private readonly GlobalKey _calendarPickerKey = new LabeledGlobalKey<State>(null);
    private readonly GlobalKey<FormState> _formKey = new LabeledGlobalKey<FormState>(null);

    private void HandleOk()
    {
        if (EntryMode.Value == DatePickerEntryMode.Input || EntryMode.Value == DatePickerEntryMode.InputOnly)
        {
            FormState form = _formKey.CurrentState!;
            if (!form.Validate())
            {
                SetState(() => _autovalidateMode.Value = AutovalidateMode.Always);
                return;
            }

            form.Save();
        }

        Navigator.Pop(Context, SelectedDate.Value);
    }

    private void HandleCancel()
    {
        Navigator.Pop(Context);
    }

    private void HandleOnDatePickerModeChange()
    {
        Widget.OnDatePickerModeChange?.Invoke(EntryMode.Value);
    }

    private void HandleEntryModeToggle()
    {
        SetState(() =>
        {
            switch (EntryMode.Value)
            {
                case DatePickerEntryMode.Calendar:
                    _autovalidateMode.Value = AutovalidateMode.Disabled;
                    EntryMode.Value = DatePickerEntryMode.Input;
                    HandleOnDatePickerModeChange();
                    break;
                case DatePickerEntryMode.Input:
                    _formKey.CurrentState!.Save();
                    EntryMode.Value = DatePickerEntryMode.Calendar;
                    HandleOnDatePickerModeChange();
                    break;
                case DatePickerEntryMode.CalendarOnly:
                case DatePickerEntryMode.InputOnly:
                    DebugAssertions.Assert(false, $"Can not change entry mode from {EntryMode.Value}");
                    break;
            }
        });
    }

    private void HandleDateChanged(DateTime date)
    {
        SetState(() => SelectedDate.Value = date);
    }

    private Size DialogSize(BuildContext context)
    {
        bool useMaterial3 = Theme.Of(context).UseMaterial3;
        bool isCalendar = EntryMode.Value switch
        {
            DatePickerEntryMode.Calendar or DatePickerEntryMode.CalendarOnly => true,
            _ => false,
        };
        Orientation orientation = MediaQuery.OrientationOf(context);

        return (isCalendar, orientation) switch
        {
            (true, Orientation.Portrait) when useMaterial3 => DatePickerConstants.CalendarPortraitDialogSizeM3,
            (false, Orientation.Portrait) when useMaterial3 => DatePickerConstants.InputPortraitDialogSizeM3,
            (true, Orientation.Portrait) => DatePickerConstants.CalendarPortraitDialogSizeM2,
            (false, Orientation.Portrait) => DatePickerConstants.InputPortraitDialogSizeM2,
            (true, _) => DatePickerConstants.CalendarLandscapeDialogSize,
            (false, _) => DatePickerConstants.InputLandscapeDialogSize,
        };
    }

    private static readonly IReadOnlyDictionary<ShortcutActivator, Intent> FormShortcutMap =
        new Dictionary<ShortcutActivator, Intent>
        {
            // Pressing enter on the field will move focus to the next field or control.
            [new SingleActivator(LogicalKeyboardKey.Enter)] = new NextFocusIntent(),
        };

    public override Widget Build(BuildContext context)
    {
        ThemeData theme = Theme.Of(context);
        bool useMaterial3 = theme.UseMaterial3;
        MaterialLocalizations localizations = MaterialLocalizations.Of(context);
        Orientation orientation = MediaQuery.OrientationOf(context);
        bool isLandscapeOrientation = orientation == Orientation.Landscape;
        DatePickerThemeData datePickerTheme = DatePickerTheme.Of(context);
        DatePickerThemeData defaults = DatePickerTheme.Defaults(context);
        TextTheme textTheme = theme.TextTheme;

        // There's no M3 spec for a landscape layout input (not calendar)
        // date picker. To ensure that the date displayed in the input
        // date picker's header fits in landscape mode, we override the M3
        // default here.
        TextStyle? headlineStyle;
        if (useMaterial3)
        {
            headlineStyle = datePickerTheme.HeaderHeadlineStyle ?? defaults.HeaderHeadlineStyle;
            switch (EntryMode.Value)
            {
                case DatePickerEntryMode.Input:
                case DatePickerEntryMode.InputOnly:
                    if (orientation == Orientation.Landscape)
                    {
                        headlineStyle = textTheme.HeadlineSmall;
                    }

                    break;
                case DatePickerEntryMode.Calendar:
                case DatePickerEntryMode.CalendarOnly:
                    // M3 default is OK.
                    break;
            }
        }
        else
        {
            headlineStyle = isLandscapeOrientation ? textTheme.HeadlineSmall : textTheme.HeadlineMedium;
        }

        Color? headerForegroundColor = datePickerTheme.HeaderForegroundColor ?? defaults.HeaderForegroundColor;
        headlineStyle = headlineStyle?.CopyWith(color: headerForegroundColor);

        Widget actions = new ConstrainedBox(
            new BoxConstraints(MinHeight: 52.0),
            MediaQuery.WithClampedTextScaling(
                maxScaleFactor: isLandscapeOrientation ? 1.6 : DatePickerConstants.KMaxTextScaleFactor,
                child: new Padding(
                    EdgeInsets.Symmetric(horizontal: 8),
                    new Align(
                        alignment: AlignmentDirectional.CenterEnd,
                        child: new OverflowBar(
                            spacing: 8,
                            children:
                            [
                                new TextButton(
                                    style: datePickerTheme.CancelButtonStyle ?? defaults.CancelButtonStyle,
                                    onPressed: HandleCancel,
                                    child: new Text(
                                        Widget.CancelText
                                        ?? (useMaterial3
                                            ? localizations.CancelButtonLabel
                                            : localizations.CancelButtonLabel.ToUpperInvariant()))),
                                new TextButton(
                                    style: datePickerTheme.ConfirmButtonStyle ?? defaults.ConfirmButtonStyle,
                                    onPressed: HandleOk,
                                    child: new Text(Widget.ConfirmText ?? localizations.OkButtonLabel)),
                            ])))));

        CalendarDatePicker CalendarDatePicker()
        {
            return new CalendarDatePicker(
                calendarDelegate: Widget.CalendarDelegate,
                key: _calendarPickerKey,
                initialDate: SelectedDate.Value,
                firstDate: Widget.FirstDate,
                lastDate: Widget.LastDate,
                currentDate: Widget.CurrentDate,
                onDateChanged: HandleDateChanged,
                selectableDayPredicate: Widget.SelectableDayPredicate,
                initialCalendarMode: Widget.InitialCalendarMode);
        }

        Form InputDatePicker()
        {
            return new Form(
                key: _formKey,
                autovalidateMode: _autovalidateMode.Value,
                child: new SizedBox(
                    height: orientation == Orientation.Portrait
                        ? DatePickerConstants.InputFormPortraitHeight
                        : DatePickerConstants.InputFormLandscapeHeight,
                    child: new Padding(
                        EdgeInsets.Symmetric(horizontal: 24),
                        new Shortcuts(
                            shortcuts: FormShortcutMap,
                            child: new Column(
                                mainAxisAlignment: MainAxisAlignment.Center,
                                children:
                                [
                                    new Flexible(
                                        child: MediaQuery.WithClampedTextScaling(
                                            maxScaleFactor: 2.0,
                                            child: new InputDatePickerFormField(
                                                calendarDelegate: Widget.CalendarDelegate,
                                                initialDate: SelectedDate.Value,
                                                firstDate: Widget.FirstDate,
                                                lastDate: Widget.LastDate,
                                                onDateSubmitted: HandleDateChanged,
                                                onDateSaved: HandleDateChanged,
                                                selectableDayPredicate: Widget.SelectableDayPredicate,
                                                errorFormatText: Widget.ErrorFormatText,
                                                errorInvalidText: Widget.ErrorInvalidText,
                                                fieldHintText: Widget.FieldHintText,
                                                fieldLabelText: Widget.FieldLabelText,
                                                keyboardType: Widget.KeyboardType,
                                                autofocus: true))),
                                ])))));
        }

        Widget picker;
        Widget? entryModeButton;
        switch (EntryMode.Value)
        {
            case DatePickerEntryMode.Calendar:
            default:
                picker = CalendarDatePicker();
                entryModeButton = new IconButton(
                    icon: Widget.SwitchToInputEntryModeIcon
                        ?? new Icon(useMaterial3 ? Icons.EditOutlined : Icons.Edit),
                    color: headerForegroundColor,
                    tooltip: localizations.InputDateModeButtonLabel,
                    onPressed: HandleEntryModeToggle);
                break;

            case DatePickerEntryMode.CalendarOnly:
                picker = CalendarDatePicker();
                entryModeButton = null;
                break;

            case DatePickerEntryMode.Input:
                picker = InputDatePicker();
                entryModeButton = new IconButton(
                    icon: Widget.SwitchToCalendarEntryModeIcon ?? new Icon(Icons.CalendarToday),
                    color: headerForegroundColor,
                    tooltip: localizations.CalendarModeButtonLabel,
                    onPressed: HandleEntryModeToggle);
                break;

            case DatePickerEntryMode.InputOnly:
                picker = InputDatePicker();
                entryModeButton = null;
                break;
        }

        Widget header = new DatePickerHeader(
            helpText: Widget.HelpText
                ?? (useMaterial3
                    ? localizations.DatePickerHelpText
                    : localizations.DatePickerHelpText.ToUpperInvariant()),
            titleText: SelectedDate.Value == null
                ? string.Empty
                : Widget.CalendarDelegate.FormatMediumDate(SelectedDate.Value.Value, localizations),
            titleStyle: headlineStyle,
            orientation: orientation,
            isShort: orientation == Orientation.Landscape,
            entryModeButton: entryModeButton);

        // Constrain the textScaleFactor to the largest supported value to prevent
        // layout issues.
        double textScaleFactor = MediaQuery.TextScalerOf(context)
                .Clamp(maxScaleFactor: DatePickerConstants.KMaxTextScaleFactor)
                .Scale(DatePickerConstants.FontSizeToScale)
            / DatePickerConstants.FontSizeToScale;
        Size dialogSize = DialogSize(context) * textScaleFactor;
        DialogThemeData dialogTheme = theme.DialogTheme;
        return new Dialog(
            backgroundColor: datePickerTheme.BackgroundColor ?? defaults.BackgroundColor,
            elevation: useMaterial3
                ? datePickerTheme.Elevation ?? defaults.Elevation!
                : datePickerTheme.Elevation ?? dialogTheme.Elevation ?? 24,
            shadowColor: datePickerTheme.ShadowColor ?? defaults.ShadowColor,
            surfaceTintColor: datePickerTheme.SurfaceTintColor ?? defaults.SurfaceTintColor,
            shape: useMaterial3
                ? datePickerTheme.Shape ?? defaults.Shape
                : datePickerTheme.Shape ?? dialogTheme.Shape ?? defaults.Shape,
            insetPadding: Widget.InsetPadding,
            clipBehavior: Clip.AntiAlias,
            child: new AnimatedContainer(
                width: dialogSize.Width,
                height: dialogSize.Height,
                duration: DatePickerConstants.DialogSizeAnimationDuration,
                curve: Curves.EaseIn,
                child: MediaQuery.WithClampedTextScaling(
                    // Constrain the textScaleFactor to the largest supported value to prevent
                    // layout issues.
                    maxScaleFactor: DatePickerConstants.KMaxTextScaleFactor,
                    child: new LayoutBuilder((BuildContext layoutContext, BoxConstraints constraints) =>
                    {
                        Size portraitDialogSize = useMaterial3
                            ? DatePickerConstants.InputPortraitDialogSizeM3
                            : DatePickerConstants.InputPortraitDialogSizeM2;
                        // Make sure the portrait dialog can fit the contents comfortably when
                        // resized from the landscape dialog.
                        bool isFullyPortrait =
                            constraints.MaxHeight >= Math.Min(dialogSize.Height, portraitDialogSize.Height);

                        switch (orientation)
                        {
                            case Orientation.Portrait:
                            default:
                            {
                                bool isInputMode = EntryMode.Value == DatePickerEntryMode.InputOnly
                                    || EntryMode.Value == DatePickerEntryMode.Input;
                                // When the portrait dialog does not fit vertically, hide the header when the
                                // entry mode is input, or hide the picker when the entry mode is not input.
                                bool showHeader = isFullyPortrait || !isInputMode;
                                bool showPicker = isFullyPortrait || isInputMode;

                                var children = new List<Widget>();
                                if (showHeader)
                                {
                                    children.Add(header);
                                }

                                if (useMaterial3)
                                {
                                    children.Add(new Divider(height: 0, color: datePickerTheme.DividerColor));
                                }

                                if (showPicker)
                                {
                                    children.Add(new Expanded(child: picker));
                                    children.Add(actions);
                                }

                                return new Column(
                                    mainAxisSize: MainAxisSize.Min,
                                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                                    children: children);
                            }

                            case Orientation.Landscape:
                            {
                                var children = new List<Widget> { header };
                                if (useMaterial3)
                                {
                                    children.Add(new VerticalDivider(width: 0, color: datePickerTheme.DividerColor));
                                }

                                children.Add(new Flexible(
                                    child: new Column(
                                        mainAxisSize: MainAxisSize.Min,
                                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                                        children:
                                        [
                                            new Expanded(child: picker),
                                            actions,
                                        ])));
                                return new Row(
                                    mainAxisSize: MainAxisSize.Min,
                                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                                    children: children);
                            }
                        }
                    }))));
    }
}

/// <summary>
/// The library-level constants that date_picker.dart shares between <see cref="DatePickerDialog"/> and
/// <see cref="DateRangePickerDialog"/> (Dart's private top-level <c>_calendarPortraitDialogSizeM2</c> ...
/// <c>_fontSizeToScale</c>).
/// </summary>
internal static class DatePickerConstants
{
    // The M3 sizes are coming from the tokens, but are hand coded,
    // as the current token DB does not contain landscape versions.
    public static readonly Size CalendarPortraitDialogSizeM2 = new(330.0, 518.0);
    public static readonly Size CalendarPortraitDialogSizeM3 = new(360.0, 568.0);
    public static readonly Size CalendarLandscapeDialogSize = new(496.0, 346.0);
    public static readonly Size InputPortraitDialogSizeM2 = new(330.0, 270.0);
    public static readonly Size InputPortraitDialogSizeM3 = new(328.0, 270.0);
    public static readonly Size InputLandscapeDialogSize = new(496, 160.0);
    public static readonly Size InputRangeLandscapeDialogSize = new(496, 164.0);
    public static readonly TimeSpan DialogSizeAnimationDuration = TimeSpan.FromMilliseconds(200);
    public const double InputFormPortraitHeight = 98.0;
    public const double InputFormLandscapeHeight = 108.0;

    // 3.0 is the maximum scale factor on mobile phones. As of 07/30/24, iOS goes up
    // to a max of 3.0 text scale factor, and Android goes up to 2.0. This is the
    // default used for non-range date pickers. This default is changed to a lower
    // value at different parts of the date pickers depending on content, and device
    // orientation.
    public const double KMaxTextScaleFactor = 3.0;

    // The max scale factor for the date range pickers.
    public const double KMaxRangeTextScaleFactor = 1.3;

    // The max text scale factor for the header. This is lower than the default as
    // the title text already starts at a large size.
    public const double KMaxHeaderTextScaleFactor = 1.6;

    // The entry button shares a line with the header text, so there is less room to
    // scale up.
    public const double KMaxHeaderWithEntryTextScaleFactor = 1.4;

    public const double KMaxHelpPortraitTextScaleFactor = 1.6;
    public const double KMaxHelpLandscapeTextScaleFactor = 1.4;

    // 14 is a common font size used to compute the effective text scale.
    public const double FontSizeToScale = 14.0;
}

/// <summary>A restorable <see cref="DatePickerEntryMode"/> value.</summary>
/// <remarks>
/// Dart's private <c>_RestorableDatePickerEntryMode</c>. This serializes each entry as a unique
/// <c>int</c> value.
/// </remarks>
internal sealed class RestorableDatePickerEntryMode : RestorableValue<DatePickerEntryMode>
{
    private readonly DatePickerEntryMode _defaultValue;

    public RestorableDatePickerEntryMode(DatePickerEntryMode defaultValue)
    {
        _defaultValue = defaultValue;
    }

    public override DatePickerEntryMode CreateDefaultValue() => _defaultValue;

    protected override void DidUpdateValue(DatePickerEntryMode oldValue)
    {
        DebugAssertions.Assert(RestorationSerialization.DebugIsSerializableForRestoration((int)Value));
        NotifyListeners();
    }

    public override DatePickerEntryMode FromPrimitives(object? data) =>
        Enum.GetValues<DatePickerEntryMode>()[Convert.ToInt32(data!)];

    public override object? ToPrimitives() => (int)Value;
}

/// <summary>A restorable <see cref="AutovalidateMode"/> value.</summary>
/// <remarks>
/// Dart's private <c>_RestorableAutovalidateMode</c>. This serializes each entry as a unique
/// <c>int</c> value.
/// </remarks>
internal sealed class RestorableAutovalidateMode : RestorableValue<AutovalidateMode>
{
    private readonly AutovalidateMode _defaultValue;

    public RestorableAutovalidateMode(AutovalidateMode defaultValue)
    {
        _defaultValue = defaultValue;
    }

    public override AutovalidateMode CreateDefaultValue() => _defaultValue;

    protected override void DidUpdateValue(AutovalidateMode oldValue)
    {
        DebugAssertions.Assert(RestorationSerialization.DebugIsSerializableForRestoration((int)Value));
        NotifyListeners();
    }

    public override AutovalidateMode FromPrimitives(object? data) =>
        Enum.GetValues<AutovalidateMode>()[Convert.ToInt32(data!)];

    public override object? ToPrimitives() => (int)Value;
}

/// <summary>Re-usable widget that displays the selected date (in large font) and the help text above it.</summary>
/// <remarks>
/// Dart's private <c>_DatePickerHeader</c>. These types include the date picker and the date range
/// picker; the latter only uses it in its input entry mode.
/// </remarks>
internal sealed class DatePickerHeader : StatelessWidget
{
    private const double DatePickerHeaderLandscapeWidth = 152.0;
    private const double DatePickerHeaderPortraitHeight = 120.0;
    private const double HeaderPaddingLandscape = 16.0;

    /// <summary>Creates a header for use in a date picker dialog.</summary>
    public DatePickerHeader(
        string helpText,
        string titleText,
        TextStyle? titleStyle,
        Orientation orientation,
        string? titleSemanticsLabel = null,
        bool isShort = false,
        Widget? entryModeButton = null,
        Key? key = null) : base(key)
    {
        HelpText = helpText;
        TitleText = titleText;
        TitleSemanticsLabel = titleSemanticsLabel;
        TitleStyle = titleStyle;
        Orientation = orientation;
        IsShort = isShort;
        EntryModeButton = entryModeButton;
    }

    /// <summary>The text that is displayed at the top of the header.</summary>
    public string HelpText { get; }

    /// <summary>The text that is displayed at the center of the header.</summary>
    public string TitleText { get; }

    /// <summary>The semantic label associated with the <see cref="TitleText"/>.</summary>
    public string? TitleSemanticsLabel { get; }

    /// <summary>The <see cref="TextStyle"/> that the title text is displayed with.</summary>
    public TextStyle? TitleStyle { get; }

    /// <summary>The orientation is used to decide how to layout its children.</summary>
    public Orientation Orientation { get; }

    /// <summary>Indicates the header is being displayed in a shorter/narrower context.</summary>
    public bool IsShort { get; }

    /// <summary>The mode-switching button that will be displayed in the lower end/bottom of the
    /// header.</summary>
    public Widget? EntryModeButton { get; }

    public override Widget Build(BuildContext context)
    {
        ThemeData theme = Theme.Of(context);
        DatePickerThemeData datePickerTheme = DatePickerTheme.Of(context);
        DatePickerThemeData defaults = DatePickerTheme.Defaults(context);
        Color? backgroundColor = datePickerTheme.HeaderBackgroundColor ?? defaults.HeaderBackgroundColor;
        Color? foregroundColor = datePickerTheme.HeaderForegroundColor ?? defaults.HeaderForegroundColor;
        TextStyle? helpStyle = (datePickerTheme.HeaderHelpStyle ?? defaults.HeaderHelpStyle)
            ?.CopyWith(color: foregroundColor);
        double currentScale = MediaQuery.TextScalerOf(context).Scale(DatePickerConstants.FontSizeToScale)
            / DatePickerConstants.FontSizeToScale;
        double maxHeaderTextScaleFactor = Math.Min(
            currentScale,
            EntryModeButton != null
                ? DatePickerConstants.KMaxHeaderWithEntryTextScaleFactor
                : DatePickerConstants.KMaxHeaderTextScaleFactor);
        double textScaleFactor = MediaQuery.TextScalerOf(context)
                .Clamp(maxScaleFactor: maxHeaderTextScaleFactor)
                .Scale(DatePickerConstants.FontSizeToScale)
            / DatePickerConstants.FontSizeToScale;
        double scaledFontSize = MediaQuery.TextScalerOf(context).Scale(TitleStyle?.FontSize ?? 32);
        double headerScaleFactor = textScaleFactor > 1 ? textScaleFactor : 1.0;

        var help = new Text(
            HelpText,
            style: helpStyle,
            maxLines: 1,
            overflow: TextOverflow.Ellipsis,
            textScaler: MediaQuery.TextScalerOf(context).Clamp(
                maxScaleFactor: Math.Min(
                    textScaleFactor,
                    Orientation == Orientation.Portrait
                        ? DatePickerConstants.KMaxHelpPortraitTextScaleFactor
                        : DatePickerConstants.KMaxHelpLandscapeTextScaleFactor)));
        var title = new Text(
            TitleText,
            semanticsLabel: TitleSemanticsLabel ?? TitleText,
            style: TitleStyle,
            maxLines: Orientation == Orientation.Portrait
                ? (scaledFontSize > 70 ? 2 : 1)
                : scaledFontSize > 40
                    ? 3
                    : 2,
            overflow: TextOverflow.Ellipsis,
            textScaler: MediaQuery.TextScalerOf(context).Clamp(maxScaleFactor: textScaleFactor));

        double fontScaleAdjustedHeaderHeight = headerScaleFactor > 1.3 ? headerScaleFactor - 0.2 : 1.0;

        switch (Orientation)
        {
            case Orientation.Portrait:
            {
                var titleRow = new List<Widget> { new Expanded(child: title) };
                if (EntryModeButton != null)
                {
                    titleRow.Add(new Semantics(container: true, child: EntryModeButton));
                }

                return new Semantics(
                    container: true,
                    child: new SizedBox(
                        height: DatePickerHeaderPortraitHeight * fontScaleAdjustedHeaderHeight,
                        child: new Material(
                            color: backgroundColor,
                            child: new Padding(
                                EdgeInsetsDirectional.Only(start: 24, end: 12, bottom: 12),
                                new Column(
                                    crossAxisAlignment: CrossAxisAlignment.Start,
                                    children:
                                    [
                                        new SizedBox(height: 16),
                                        help,
                                        new Flexible(child: new SizedBox(height: 38)),
                                        new Row(children: titleRow),
                                    ])))));
            }
            case Orientation.Landscape:
            default:
            {
                var children = new List<Widget>
                {
                    new SizedBox(height: 16),
                    new Padding(EdgeInsets.Symmetric(horizontal: HeaderPaddingLandscape), help),
                    new SizedBox(height: IsShort ? 16 : 56),
                    new Expanded(
                        child: new Padding(EdgeInsets.Symmetric(horizontal: HeaderPaddingLandscape), title)),
                };
                if (EntryModeButton != null)
                {
                    // TODO(TahaTesser): This is an eye-balled M3 entry mode button padding from
                    // https://m3.material.io/components/date-pickers/specs#c16c142b-4706-47f3-9400-3cde654b9aa8.
                    // Update this value to use tokens when available.
                    children.Add(new Padding(
                        theme.UseMaterial3
                            ? (EdgeInsetsGeometry)EdgeInsetsDirectional.Only(start: 8.0, end: 4.0, bottom: 6.0)
                            : EdgeInsets.Symmetric(horizontal: 4),
                        new Semantics(container: true, child: EntryModeButton)));
                }

                return new Semantics(
                    container: true,
                    child: new SizedBox(
                        width: DatePickerHeaderLandscapeWidth,
                        child: new Material(
                            color: backgroundColor,
                            child: new Column(
                                crossAxisAlignment: CrossAxisAlignment.Start,
                                children: children))));
            }
        }
    }
}
