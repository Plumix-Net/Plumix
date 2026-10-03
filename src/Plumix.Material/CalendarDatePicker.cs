using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/calendar_date_picker.dart

/// <summary>Dart's file-level constants of <c>calendar_date_picker.dart</c>.</summary>
file static class Consts
{
    public static readonly TimeSpan MonthScrollDuration = TimeSpan.FromMilliseconds(200);

    // Current M2 implementation is not compliant with the M2 specification.
    // Instead of a 42 pixels row height it should be 40 with a 2 pixels inner padding.
    // See: https://m2.material.io/components/date-pickers#specs.
    public const double DayPickerRowHeightM2 = 42.0;

    // For M3, row height is 48 pxiels with 4 pixels inner padding.
    // See: https://m3.material.io/components/date-pickers/specs#2d53890e-a08f-4c63-a0d9-abd9e95b4245.
    public const double DayPickerRowHeightM3 = 48.0;

    public const int MaxDayPickerRowCount = 6; // A 31 day month that starts on Saturday.

    // One extra row for the day-of-week header.
    public const double MaxDayPickerHeightM2 = DayPickerRowHeightM2 * (MaxDayPickerRowCount + 1);
    public const double MaxDayPickerHeightM3 = DayPickerRowHeightM3 * (MaxDayPickerRowCount + 1);

    public const double MonthPickerHorizontalPaddingPortraitM3 = 12.0;
    public const double MonthPickerHorizontalPaddingOther = 8.0;

    public const int YearPickerColumnCount = 3;
    public const double YearPickerPadding = 16.0;
    public const double YearPickerRowHeight = 52.0;
    public const double YearPickerRowSpacing = 8.0;

    public const double SubHeaderHeight = 52.0;
    public const double MonthNavButtonsWidth = 108.0;

    // 3.0 is the maximum scale factor on mobile phones. As of 07/30/24, iOS goes up
    // to a max of 3.0 text scale factor, and Android goes up to 2.0. This is the
    // default used for non-range date pickers. This default is changed to a lower
    // value at different parts of the date pickers depending on content, and device
    // orientation.
    public const double KMaxTextScaleFactor = 3.0;

    public const double KModeToggleButtonMaxScaleFactor = 2.0;

    // The max scale factor of the day picker grid. This affects the size of the
    // individual days in calendar view. Due to them filling a majority of the modal,
    // which covers most of the screen, there's a limit in how large they can grow.
    // There is also less room vertically in landscape orientation.
    public const double KDayPickerGridPortraitMaxScaleFactor = 2.0;
    public const double KDayPickerGridLandscapeMaxScaleFactor = 1.5;

    // 14 is a common font size used to compute the effective text scale.
    public const double FontSizeToScale = 14.0;

    // Dart's `DateTime.daysPerWeek` and `DateTime.january`.
    public const int DaysPerWeek = 7;
    public const int January = 1;

    // Dart's top-level `_reportAnnouncementError`, attached to the announcement future as `.catchError`.
    public static void ReportAnnouncementError(Task announcement) =>
        Scheduler.RunAsync(() => ReportAnnouncementErrorAsync(announcement));

    private static async Task ReportAnnouncementErrorAsync(Task announcement)
    {
        try
        {
            await announcement;
        }
        catch (Exception exception)
        {
            FlutterError.ReportError(new FlutterErrorDetails(
                exception: exception,
                stack: exception.StackTrace,
                library: "material library",
                context: new ErrorDescription("while sending semantics announcement")));
        }
    }
}

/// <summary>
/// Displays a grid of days for a given month and allows the user to select a date.
/// </summary>
/// <remarks>
/// Days are arranged in a rectangular grid with one column for each day of the week. Controls are
/// provided to change the year and month that the grid is showing.
/// <para>
/// The calendar picker widget is rarely used directly. Instead, consider using
/// <c>showDatePicker</c>, which will create a dialog that uses this as well as provides a text entry
/// option.
/// </para>
/// </remarks>
public class CalendarDatePicker : StatefulWidget
{
    /// <summary>Creates a calendar date picker.</summary>
    /// <remarks>
    /// It will display a grid of days for the <paramref name="initialDate"/>'s month, or, if that is
    /// null, the <paramref name="currentDate"/>'s month. The day indicated by
    /// <paramref name="initialDate"/> will be selected if it is not null.
    /// <para>
    /// The <paramref name="lastDate"/> must be after or equal to <paramref name="firstDate"/>. The
    /// <paramref name="initialDate"/>, if provided, must be between <paramref name="firstDate"/> and
    /// <paramref name="lastDate"/> or equal to one of them. If <paramref name="selectableDayPredicate"/>
    /// and <paramref name="initialDate"/> are both non-null, <paramref name="selectableDayPredicate"/>
    /// must return <c>true</c> for the <paramref name="initialDate"/>.
    /// </para>
    /// <para>
    /// The <paramref name="calendarDelegate"/> controls date interpretation, formatting, and navigation
    /// within the picker. Defaults to <see cref="GregorianCalendarDelegate"/>.
    /// </para>
    /// </remarks>
    public CalendarDatePicker(
        DateTime? initialDate,
        DateTime firstDate,
        DateTime lastDate,
        Action<DateTime> onDateChanged,
        DateTime? currentDate = null,
        Action<DateTime>? onDisplayedMonthChanged = null,
        DatePickerMode initialCalendarMode = DatePickerMode.Day,
        SelectableDayPredicate? selectableDayPredicate = null,
        CalendarDelegate<DateTime>? calendarDelegate = null,
        Key? key = null) : base(key)
    {
        CalendarDelegate = calendarDelegate ?? GregorianCalendarDelegate.Instance;
        InitialDate = initialDate == null ? null : CalendarDelegate.DateOnly(initialDate.Value);
        FirstDate = CalendarDelegate.DateOnly(firstDate);
        LastDate = CalendarDelegate.DateOnly(lastDate);
        CurrentDate = CalendarDelegate.DateOnly(currentDate ?? CalendarDelegate.Now());
        OnDateChanged = onDateChanged;
        OnDisplayedMonthChanged = onDisplayedMonthChanged;
        InitialCalendarMode = initialCalendarMode;
        SelectableDayPredicate = selectableDayPredicate;

        DebugAssertions.Assert(
            !(LastDate < FirstDate),
            $"lastDate {DateUtils.DartToString(LastDate)} must be on or after firstDate "
            + $"{DateUtils.DartToString(FirstDate)}.");
        DebugAssertions.Assert(
            InitialDate == null || !(InitialDate.Value < FirstDate),
            $"initialDate {Describe(InitialDate)} must be on or after firstDate "
            + $"{DateUtils.DartToString(FirstDate)}.");
        DebugAssertions.Assert(
            InitialDate == null || !(InitialDate.Value > LastDate),
            $"initialDate {Describe(InitialDate)} must be on or before lastDate "
            + $"{DateUtils.DartToString(LastDate)}.");
        DebugAssertions.Assert(
            selectableDayPredicate == null
            || InitialDate == null
            || selectableDayPredicate(InitialDate.Value),
            $"Provided initialDate {Describe(InitialDate)} must satisfy provided selectableDayPredicate.");
    }

    /// <summary>The initially selected <see cref="DateTime"/> that the picker should display.</summary>
    /// <remarks>
    /// Subsequently changing this has no effect. To change the selected date, change the
    /// <see cref="Widget.Key"/> to create a new instance of the <see cref="CalendarDatePicker"/>, and
    /// provide that widget the new <see cref="InitialDate"/>. This will reset the widget's interactive
    /// state.
    /// </remarks>
    public DateTime? InitialDate { get; }

    /// <summary>The earliest allowable <see cref="DateTime"/> that the user can select.</summary>
    public DateTime FirstDate { get; }

    /// <summary>The latest allowable <see cref="DateTime"/> that the user can select.</summary>
    public DateTime LastDate { get; }

    /// <summary>The <see cref="DateTime"/> representing today. It will be highlighted in the day grid.</summary>
    public DateTime CurrentDate { get; }

    /// <summary>Called when the user selects a date in the picker.</summary>
    public Action<DateTime> OnDateChanged { get; }

    /// <summary>Called when the user navigates to a new month/year in the picker.</summary>
    public Action<DateTime>? OnDisplayedMonthChanged { get; }

    /// <summary>The initial display of the calendar picker.</summary>
    /// <remarks>
    /// Subsequently changing this has no effect. To change the calendar mode, change the
    /// <see cref="Widget.Key"/> to create a new instance of the <see cref="CalendarDatePicker"/>, and
    /// provide that widget a new <see cref="InitialCalendarMode"/>. This will reset the widget's
    /// interactive state.
    /// </remarks>
    public DatePickerMode InitialCalendarMode { get; }

    /// <summary>Function to provide full control over which dates in the calendar can be selected.</summary>
    public SelectableDayPredicate? SelectableDayPredicate { get; }

    /// <summary>
    /// The calendar system the picker interprets, formats, and navigates dates with. Defaults to
    /// <see cref="GregorianCalendarDelegate"/>.
    /// </summary>
    public CalendarDelegate<DateTime> CalendarDelegate { get; }

    public override State CreateState() => new CalendarDatePickerState();

    // Dart interpolates the nullable `this.initialDate`, i.e. `DateTime.toString` or `null`.
    private static string Describe(DateTime? date) => date is { } value ? DateUtils.DartToString(value) : "null";
}

/// <summary>Dart's private <c>_CalendarDatePickerState</c>.</summary>
internal sealed class CalendarDatePickerState : State<CalendarDatePicker>
{
    private bool _announcedInitialDate;
    private string _announcementText = string.Empty;
    private DatePickerMode _mode;
    private DateTime _currentDisplayedMonthDate;
    private DateTime? _selectedDate;
    private readonly GlobalKey _monthPickerKey = new LabeledGlobalKey<State>(null);
    private readonly GlobalKey _yearPickerKey = new LabeledGlobalKey<State>(null);
    private MaterialLocalizations _localizations = null!;
    private TextDirection _textDirection;

    public override void InitState()
    {
        base.InitState();
        _mode = Widget.InitialCalendarMode;
        DateTime currentDisplayedDate = Widget.InitialDate ?? Widget.CurrentDate;
        _currentDisplayedMonthDate = Widget.CalendarDelegate.GetMonth(
            currentDisplayedDate.Year,
            currentDisplayedDate.Month);
        if (Widget.InitialDate != null)
        {
            _selectedDate = Widget.InitialDate;
        }
    }

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        DebugAssertions.Assert(MaterialDebug.DebugCheckHasMaterial(Context));
        DebugAssertions.Assert(MaterialDebug.DebugCheckHasMaterialLocalizations(Context));
        DebugAssertions.Assert(WidgetsDebug.DebugCheckHasDirectionality(Context));
        _localizations = MaterialLocalizations.Of(Context);
        _textDirection = Directionality.Of(Context);
        if (!_announcedInitialDate && Widget.InitialDate != null)
        {
            DebugAssertions.Assert(_selectedDate != null);
            _announcedInitialDate = true;
            bool isToday = Widget.CalendarDelegate.IsSameDay(Widget.CurrentDate, _selectedDate);
            string semanticLabelSuffix = isToday ? $", {_localizations.CurrentDateLabel}" : string.Empty;
            Announce($"{_localizations.FormatFullDate(_selectedDate!.Value)}{semanticLabelSuffix}");
        }
    }

    // Auxiliary method for handling the difference between platforms
    private void Announce(string message)
    {
        if (MediaQuery.MaybeSupportsAnnounceOf(Context) ?? false)
        {
            Consts.ReportAnnouncementError(SemanticsService.SendAnnouncement(
                View.Of(Context),
                message,
                Directionality.Of(Context)));
        }
        else
        {
            // If SemanticsService.sendAnnouncement is not supported,
            // we use live region to achieve the announcement effect instead.
            _announcementText = message;
        }
    }

    private void Vibrate()
    {
        switch (Theme.Of(Context).Platform)
        {
            case TargetPlatform.Android:
            case TargetPlatform.Fuchsia:
            case TargetPlatform.Linux:
            case TargetPlatform.Windows:
                _ = HapticFeedback.Vibrate();
                break;
            case TargetPlatform.IOS:
            case TargetPlatform.MacOS:
                break;
        }
    }

    private void HandleModeChanged(DatePickerMode mode)
    {
        Vibrate();
        SetState(() =>
        {
            _mode = mode;
            if (_selectedDate is { } selected)
            {
                string message = mode switch
                {
                    DatePickerMode.Day => Widget.CalendarDelegate.FormatMonthYear(selected, _localizations),
                    DatePickerMode.Year => Widget.CalendarDelegate.FormatYear(selected.Year, _localizations),
                    _ => throw new ArgumentOutOfRangeException(nameof(mode)),
                };
                Announce(message);
            }
        });
    }

    private void HandleMonthChanged(DateTime date)
    {
        SetState(() =>
        {
            if (_currentDisplayedMonthDate.Year != date.Year
                || _currentDisplayedMonthDate.Month != date.Month)
            {
                _currentDisplayedMonthDate = Widget.CalendarDelegate.GetMonth(date.Year, date.Month);
                Widget.OnDisplayedMonthChanged?.Invoke(_currentDisplayedMonthDate);
            }
        });
    }

    private void HandleYearChanged(DateTime value)
    {
        Vibrate();

        int daysInMonth = Widget.CalendarDelegate.GetDaysInMonth(value.Year, value.Month);
        int preferredDay = Math.Min(_selectedDate?.Day ?? 1, daysInMonth);
        value = Widget.CalendarDelegate.GetDay(value.Year, value.Month, preferredDay);

        if (value < Widget.FirstDate)
        {
            value = Widget.FirstDate;
        }
        else if (value > Widget.LastDate)
        {
            value = Widget.LastDate;
        }

        SetState(() =>
        {
            _mode = DatePickerMode.Day;
            HandleMonthChanged(value);

            if (IsSelectable(value))
            {
                _selectedDate = value;
                Widget.OnDateChanged(_selectedDate.Value);
            }
        });
    }

    private void HandleDayChanged(DateTime value)
    {
        Vibrate();
        SetState(() =>
        {
            _selectedDate = value;
            Widget.OnDateChanged(_selectedDate.Value);
            switch (Theme.Of(Context).Platform)
            {
                case TargetPlatform.Linux:
                case TargetPlatform.MacOS:
                case TargetPlatform.Windows:
                    bool isToday = Widget.CalendarDelegate.IsSameDay(Widget.CurrentDate, _selectedDate);
                    string semanticLabelSuffix = isToday ? $", {_localizations.CurrentDateLabel}" : string.Empty;
                    string fullDate = Widget.CalendarDelegate.FormatFullDate(_selectedDate.Value, _localizations);
                    Consts.ReportAnnouncementError(SemanticsService.SendAnnouncement(
                        View.Of(Context),
                        $"{_localizations.SelectedDateLabel} {fullDate}{semanticLabelSuffix}",
                        _textDirection));
                    break;
                case TargetPlatform.Android:
                case TargetPlatform.IOS:
                case TargetPlatform.Fuchsia:
                    break;
            }
        });
    }

    private bool IsSelectable(DateTime date)
    {
        return Widget.SelectableDayPredicate?.Invoke(date) ?? true;
    }

    private Widget BuildPicker()
    {
        switch (_mode)
        {
            case DatePickerMode.Day:
                return new MonthPicker(
                    key: _monthPickerKey,
                    calendarDelegate: Widget.CalendarDelegate,
                    initialMonth: _currentDisplayedMonthDate,
                    currentDate: Widget.CurrentDate,
                    firstDate: Widget.FirstDate,
                    lastDate: Widget.LastDate,
                    selectedDate: _selectedDate,
                    onChanged: HandleDayChanged,
                    onDisplayedMonthChanged: HandleMonthChanged,
                    selectableDayPredicate: Widget.SelectableDayPredicate);
            case DatePickerMode.Year:
                return new Padding(
                    EdgeInsets.Only(top: Consts.SubHeaderHeight),
                    new YearPicker(
                        key: _yearPickerKey,
                        calendarDelegate: Widget.CalendarDelegate,
                        currentDate: Widget.CurrentDate,
                        firstDate: Widget.FirstDate,
                        lastDate: Widget.LastDate,
                        selectedDate: _currentDisplayedMonthDate,
                        onChanged: HandleYearChanged));
            default:
                throw new ArgumentOutOfRangeException(nameof(_mode));
        }
    }

    public override Widget Build(BuildContext context)
    {
        DebugAssertions.Assert(MaterialDebug.DebugCheckHasMaterial(context));
        DebugAssertions.Assert(MaterialDebug.DebugCheckHasMaterialLocalizations(context));
        DebugAssertions.Assert(WidgetsDebug.DebugCheckHasDirectionality(context));
        double textScaleFactor =
            MediaQuery.TextScalerOf(context)
                .Clamp(maxScaleFactor: Consts.KMaxTextScaleFactor)
                .Scale(Consts.FontSizeToScale)
            / Consts.FontSizeToScale;

        // Conform to M3 spec in portrait mode (landscape mode is not specified).
        Orientation orientation = MediaQuery.OrientationOf(context);
        double maxDayPickerHeight = Theme.Of(context).UseMaterial3 && orientation == Orientation.Portrait
            ? Consts.MaxDayPickerHeightM3
            : Consts.MaxDayPickerHeightM2;

        // Scale the height of the picker area up with larger text. The size of the
        // picker has room for larger text, up until a scale factor of 1.3. After
        // after which, we increase the height to add room for content to continue
        // to scale the text size.
        double scaledMaxDayPickerHeight = textScaleFactor > 1.3
            ? maxDayPickerHeight + ((Consts.MaxDayPickerRowCount + 1) * ((textScaleFactor - 1) * 8))
            : maxDayPickerHeight;
        var picker = new SizedBox(
            height: Consts.SubHeaderHeight + scaledMaxDayPickerHeight,
            child: BuildPicker());
        return new Stack(
            children:
            [
                (MediaQuery.MaybeSupportsAnnounceOf(context) ?? false)
                    ? picker
                    : new Semantics(
                        container: true,
                        liveRegion: true,
                        accessibilityFocusBlockType: AccessibilityFocusBlockType.BlockNode,
                        label: _announcementText,
                        child: picker),

                // Put the mode toggle button on top so that it won't be covered up by the _MonthPicker
                MediaQuery.WithClampedTextScaling(
                    maxScaleFactor: Consts.KModeToggleButtonMaxScaleFactor,
                    child: new DatePickerModeToggleButton(
                        mode: _mode,
                        title: Widget.CalendarDelegate.FormatMonthYear(
                            _currentDisplayedMonthDate,
                            _localizations),
                        onTitlePressed: () => HandleModeChanged(_mode switch
                        {
                            DatePickerMode.Day => DatePickerMode.Year,
                            DatePickerMode.Year => DatePickerMode.Day,
                            _ => throw new ArgumentOutOfRangeException(nameof(_mode)),
                        }))),
            ]);
    }
}

/// <summary>
/// A button that used to toggle the <see cref="DatePickerMode"/> for a date picker.
/// </summary>
/// <remarks>
/// Dart's private <c>_DatePickerModeToggleButton</c>. This appears above the calendar grid and allows
/// the user to toggle the <see cref="DatePickerMode"/> to display either the calendar view or the year
/// list.
/// </remarks>
internal sealed class DatePickerModeToggleButton : StatefulWidget
{
    public DatePickerModeToggleButton(DatePickerMode mode, string title, Action onTitlePressed)
    {
        Mode = mode;
        Title = title;
        OnTitlePressed = onTitlePressed;
    }

    /// <summary>The current display of the calendar picker.</summary>
    public DatePickerMode Mode { get; }

    /// <summary>The text that displays the current month/year being viewed.</summary>
    public string Title { get; }

    /// <summary>The callback when the title is pressed.</summary>
    public Action OnTitlePressed { get; }

    public override State CreateState() => new DatePickerModeToggleButtonState();
}

/// <summary>Dart's private <c>_DatePickerModeToggleButtonState</c>.</summary>
/// <remarks>Dart mixes in <c>SingleTickerProviderStateMixin</c>; every Plumix <see cref="State"/> is
/// already a ticker provider.</remarks>
internal sealed class DatePickerModeToggleButtonState : State<DatePickerModeToggleButton>
{
    private AnimationController _controller = null!;

    public override void InitState()
    {
        base.InitState();
        _controller = new AnimationController(
            value: Widget.Mode == DatePickerMode.Year ? 0.5 : 0,
            upperBound: 0.5,
            duration: TimeSpan.FromMilliseconds(200),
            vsync: this);
    }

    public override void DidUpdateWidget(DatePickerModeToggleButton oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        if (oldWidget.Mode == Widget.Mode)
        {
            return;
        }

        if (Widget.Mode == DatePickerMode.Year)
        {
            _ = _controller.Forward();
        }
        else
        {
            _ = _controller.Reverse();
        }
    }

    public override Widget Build(BuildContext context)
    {
        DatePickerThemeData datePickerTheme = DatePickerTheme.Of(context);
        DatePickerThemeData defaults = DatePickerTheme.Defaults(context);
        TextStyle? buttonTextStyle = datePickerTheme.ToggleButtonTextStyle ?? defaults.ToggleButtonTextStyle;
        Color? subHeaderForegroundColor =
            datePickerTheme.SubHeaderForegroundColor ?? defaults.SubHeaderForegroundColor;
        Color? buttonTextColor = datePickerTheme.ToggleButtonTextStyle?.Color
            ?? datePickerTheme.SubHeaderForegroundColor
            ?? defaults.ToggleButtonTextStyle?.Color;

        return new SizedBox(
            height: Consts.SubHeaderHeight,
            child: new Padding(
                EdgeInsetsDirectional.Only(start: 16, end: 4),
                new Row(
                    children:
                    [
                        new Flexible(
                            child: new Semantics(
                                label: MaterialLocalizations.Of(context).SelectYearSemanticsLabel,
                                button: true,
                                container: true,
                                child: new SizedBox(
                                    height: Consts.SubHeaderHeight,
                                    child: new InkWell(
                                        onTap: Widget.OnTitlePressed,
                                        child: new Padding(
                                            EdgeInsets.Symmetric(horizontal: 8),
                                            new Row(
                                                children:
                                                [
                                                    new Flexible(
                                                        child: new Text(
                                                            Widget.Title,
                                                            overflow: TextOverflow.Ellipsis,
                                                            style: buttonTextStyle?.Apply(color: buttonTextColor))),
                                                    new RotationTransition(
                                                        turns: _controller,
                                                        child: new Icon(
                                                            Icons.ArrowDropDown,
                                                            color: subHeaderForegroundColor)),
                                                ])))))),
                        .. Widget.Mode == DatePickerMode.Day
                            // Give space for the prev/next month buttons that are underneath this row
                            ? new Widget[] { new SizedBox(width: Consts.MonthNavButtonsWidth) }
                            : [],
                    ])));
    }

    public override void Dispose()
    {
        _controller.Dispose();
        base.Dispose();
    }
}

/// <summary>Dart's private <c>_MonthPicker</c>.</summary>
internal sealed class MonthPicker : StatefulWidget
{
    /// <summary>Creates a month picker.</summary>
    public MonthPicker(
        DateTime initialMonth,
        DateTime currentDate,
        DateTime firstDate,
        DateTime lastDate,
        DateTime? selectedDate,
        Action<DateTime> onChanged,
        Action<DateTime> onDisplayedMonthChanged,
        CalendarDelegate<DateTime> calendarDelegate,
        SelectableDayPredicate? selectableDayPredicate = null,
        Key? key = null) : base(key)
    {
        DebugAssertions.Assert(!(firstDate > lastDate));
        DebugAssertions.Assert(selectedDate == null || !(selectedDate.Value < firstDate));
        DebugAssertions.Assert(selectedDate == null || !(selectedDate.Value > lastDate));
        InitialMonth = initialMonth;
        CurrentDate = currentDate;
        FirstDate = firstDate;
        LastDate = lastDate;
        SelectedDate = selectedDate;
        OnChanged = onChanged;
        OnDisplayedMonthChanged = onDisplayedMonthChanged;
        CalendarDelegate = calendarDelegate;
        SelectableDayPredicate = selectableDayPredicate;
    }

    /// <summary>The initial month to display.</summary>
    /// <remarks>
    /// Subsequently changing this has no effect. To change the selected month, change the
    /// <see cref="Widget.Key"/> to create a new instance of the <see cref="MonthPicker"/>, and provide
    /// that widget the new <see cref="InitialMonth"/>. This will reset the widget's interactive state.
    /// </remarks>
    public DateTime InitialMonth { get; }

    /// <summary>The current date.</summary>
    /// <remarks>This date is subtly highlighted in the picker.</remarks>
    public DateTime CurrentDate { get; }

    /// <summary>The earliest date the user is permitted to pick.</summary>
    /// <remarks>This date must be on or before the <see cref="LastDate"/>.</remarks>
    public DateTime FirstDate { get; }

    /// <summary>The latest date the user is permitted to pick.</summary>
    /// <remarks>This date must be on or after the <see cref="FirstDate"/>.</remarks>
    public DateTime LastDate { get; }

    /// <summary>The currently selected date.</summary>
    /// <remarks>This date is highlighted in the picker.</remarks>
    public DateTime? SelectedDate { get; }

    /// <summary>Called when the user picks a day.</summary>
    public Action<DateTime> OnChanged { get; }

    /// <summary>Called when the user navigates to a new month.</summary>
    public Action<DateTime> OnDisplayedMonthChanged { get; }

    /// <summary>Optional user supplied predicate function to customize selectable days.</summary>
    public SelectableDayPredicate? SelectableDayPredicate { get; }

    /// <summary>The calendar system the picker interprets, formats, and navigates dates with.</summary>
    public CalendarDelegate<DateTime> CalendarDelegate { get; }

    public override State CreateState() => new MonthPickerState();
}

/// <summary>Dart's private <c>_MonthPickerState</c>.</summary>
internal sealed class MonthPickerState : State<MonthPicker>
{
    private static readonly IReadOnlyDictionary<TraversalDirection, int> DirectionOffset =
        new Dictionary<TraversalDirection, int>
        {
            [TraversalDirection.Up] = -Consts.DaysPerWeek,
            [TraversalDirection.Right] = 1,
            [TraversalDirection.Down] = Consts.DaysPerWeek,
            [TraversalDirection.Left] = -1,
        };

    private readonly GlobalKey _pageViewKey = new LabeledGlobalKey<State>(null);
    private string _announcementText = string.Empty;
    private DateTime _currentMonth;
    private PageController _pageController = null!;
    private MaterialLocalizations _localizations = null!;
    private IReadOnlyDictionary<ShortcutActivator, Intent>? _shortcutMap;
    private IReadOnlyDictionary<Type, FlutterAction>? _actionMap;
    private FocusNode _dayGridFocus = null!;
    private DateTime? _focusedDay;

    public override void InitState()
    {
        base.InitState();
        _currentMonth = Widget.InitialMonth;
        _pageController = new PageController(
            initialPage: Widget.CalendarDelegate.MonthDelta(Widget.FirstDate, _currentMonth));
        _shortcutMap = new Dictionary<ShortcutActivator, Intent>
        {
            [new SingleActivator(LogicalKeyboardKey.ArrowLeft)] =
                new DirectionalFocusIntent(TraversalDirection.Left),
            [new SingleActivator(LogicalKeyboardKey.ArrowRight)] =
                new DirectionalFocusIntent(TraversalDirection.Right),
            [new SingleActivator(LogicalKeyboardKey.ArrowDown)] =
                new DirectionalFocusIntent(TraversalDirection.Down),
            [new SingleActivator(LogicalKeyboardKey.ArrowUp)] = new DirectionalFocusIntent(TraversalDirection.Up),
        };
        _actionMap = new Dictionary<Type, FlutterAction>
        {
            [typeof(NextFocusIntent)] = new CallbackAction<NextFocusIntent>(onInvoke: HandleGridNextFocus),
            [typeof(PreviousFocusIntent)] =
                new CallbackAction<PreviousFocusIntent>(onInvoke: HandleGridPreviousFocus),
            [typeof(DirectionalFocusIntent)] = new CallbackAction<DirectionalFocusIntent>(
                onInvoke: HandleDirectionFocus),
        };
        _dayGridFocus = new FocusNode(debugLabel: "Day Grid");
    }

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        _localizations = MaterialLocalizations.Of(Context);
    }

    public override void Dispose()
    {
        _pageController.Dispose();
        _dayGridFocus.Dispose();
        base.Dispose();
    }

    private void HandleDateSelected(DateTime selectedDate)
    {
        _focusedDay = selectedDate;
        Widget.OnChanged(selectedDate);
    }

    // Auxiliary method for handling the difference between platforms
    private void Announce(string message)
    {
        if (MediaQuery.MaybeSupportsAnnounceOf(Context) ?? false)
        {
            Consts.ReportAnnouncementError(SemanticsService.SendAnnouncement(
                View.Of(Context),
                message,
                Directionality.Of(Context)));
        }
        else
        {
            // If SemanticsService.sendAnnouncement is not supported,
            // we use live region to achieve the announcement effect instead.
            _announcementText = message;
        }
    }

    private void HandleMonthPageChanged(int monthPage)
    {
        SetState(() =>
        {
            DateTime monthDate = Widget.CalendarDelegate.AddMonthsToMonthDate(Widget.FirstDate, monthPage);
            if (!Widget.CalendarDelegate.IsSameMonth(_currentMonth, monthDate))
            {
                _currentMonth = Widget.CalendarDelegate.GetMonth(monthDate.Year, monthDate.Month);
                Widget.OnDisplayedMonthChanged(_currentMonth);
                if (_focusedDay != null && !Widget.CalendarDelegate.IsSameMonth(_focusedDay, _currentMonth))
                {
                    // We have navigated to a new month with the grid focused, but the
                    // focused day is not in this month. Choose a new one trying to keep
                    // the same day of the month.
                    _focusedDay = FocusableDayForMonth(_currentMonth, _focusedDay.Value.Day);
                }

                Announce(Widget.CalendarDelegate.FormatMonthYear(_currentMonth, _localizations));
            }
        });
    }

    /// <summary>Returns a focusable date for the given month.</summary>
    /// <remarks>
    /// If the preferredDay is available in the month it will be returned, otherwise the first
    /// selectable day in the month will be returned. If no dates are selectable in the month, then it
    /// will return null.
    /// </remarks>
    private DateTime? FocusableDayForMonth(DateTime month, int preferredDay)
    {
        int daysInMonth = Widget.CalendarDelegate.GetDaysInMonth(month.Year, month.Month);

        // Can we use the preferred day in this month?
        if (preferredDay <= daysInMonth)
        {
            DateTime newFocus = Widget.CalendarDelegate.GetDay(month.Year, month.Month, preferredDay);
            if (IsSelectable(newFocus))
            {
                return newFocus;
            }
        }

        // Start at the 1st and take the first selectable date.
        for (int day = 1; day <= daysInMonth; day++)
        {
            DateTime newFocus = Widget.CalendarDelegate.GetDay(month.Year, month.Month, day);
            if (IsSelectable(newFocus))
            {
                return newFocus;
            }
        }

        return null;
    }

    /// <summary>Navigate to the next month.</summary>
    private void HandleNextMonth()
    {
        if (!IsDisplayingLastMonth)
        {
            _ = _pageController.NextPage(duration: Consts.MonthScrollDuration, curve: Curves.Ease);
        }
    }

    /// <summary>Navigate to the previous month.</summary>
    private void HandlePreviousMonth()
    {
        if (!IsDisplayingFirstMonth)
        {
            _ = _pageController.PreviousPage(duration: Consts.MonthScrollDuration, curve: Curves.Ease);
        }
    }

    /// <summary>Navigate to the given month.</summary>
    private void ShowMonth(DateTime month, bool jump = false)
    {
        int monthPage = Widget.CalendarDelegate.MonthDelta(Widget.FirstDate, month);
        if (jump)
        {
            _pageController.JumpToPage(monthPage);
        }
        else
        {
            _ = _pageController.AnimateToPage(
                monthPage,
                duration: Consts.MonthScrollDuration,
                curve: Curves.Ease);
        }
    }

    /// <summary>True if the earliest allowable month is displayed.</summary>
    private bool IsDisplayingFirstMonth =>
        !(_currentMonth > Widget.CalendarDelegate.GetMonth(Widget.FirstDate.Year, Widget.FirstDate.Month));

    /// <summary>True if the latest allowable month is displayed.</summary>
    private bool IsDisplayingLastMonth =>
        !(_currentMonth < Widget.CalendarDelegate.GetMonth(Widget.LastDate.Year, Widget.LastDate.Month));

    /// <summary>Handler for when the overall day grid obtains or loses focus.</summary>
    private void HandleGridFocusChange(bool focused)
    {
        SetState(() =>
        {
            if (focused && _focusedDay == null)
            {
                if (Widget.CalendarDelegate.IsSameMonth(Widget.SelectedDate, _currentMonth))
                {
                    _focusedDay = Widget.SelectedDate;
                }
                else if (Widget.CalendarDelegate.IsSameMonth(Widget.CurrentDate, _currentMonth))
                {
                    _focusedDay = FocusableDayForMonth(_currentMonth, Widget.CurrentDate.Day);
                }
                else
                {
                    _focusedDay = FocusableDayForMonth(_currentMonth, 1);
                }
            }
        });
    }

    /// <summary>Move focus to the next element after the day grid.</summary>
    private object? HandleGridNextFocus(NextFocusIntent intent)
    {
        _dayGridFocus.RequestFocus();
        _dayGridFocus.NextFocus();
        return null;
    }

    /// <summary>Move focus to the previous element before the day grid.</summary>
    private object? HandleGridPreviousFocus(PreviousFocusIntent intent)
    {
        _dayGridFocus.RequestFocus();
        _dayGridFocus.PreviousFocus();
        return null;
    }

    /// <summary>Move the internal focus date in the direction of the given intent.</summary>
    /// <remarks>
    /// This will attempt to move the focused day to the next selectable day in the given direction. If
    /// the new date is not in the current month, then the page view will be scrolled to show the new
    /// date's month.
    /// <para>
    /// For horizontal directions, it will move forward or backward a day (depending on the current
    /// <see cref="TextDirection"/>). For vertical directions it will move up and down a week at a time.
    /// </para>
    /// </remarks>
    private object? HandleDirectionFocus(DirectionalFocusIntent intent)
    {
        DebugAssertions.Assert(_focusedDay != null);
        SetState(() =>
        {
            DateTime? nextDate = NextDateInDirection(_focusedDay!.Value, intent.Direction);
            if (nextDate != null)
            {
                _focusedDay = nextDate;
                if (!Widget.CalendarDelegate.IsSameMonth(_focusedDay, _currentMonth))
                {
                    ShowMonth(_focusedDay.Value);
                }
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
        DateTime nextDate = Widget.CalendarDelegate.AddDaysToDate(
            date,
            DayDirectionOffset(direction, textDirection));
        while (!(nextDate < Widget.FirstDate) && !(nextDate > Widget.LastDate))
        {
            if (IsSelectable(nextDate))
            {
                return nextDate;
            }

            nextDate = Widget.CalendarDelegate.AddDaysToDate(
                nextDate,
                DayDirectionOffset(direction, textDirection));
        }

        return null;
    }

    private bool IsSelectable(DateTime date)
    {
        return Widget.SelectableDayPredicate?.Invoke(date) ?? true;
    }

    private Widget BuildItems(BuildContext context, int index)
    {
        DateTime month = Widget.CalendarDelegate.AddMonthsToMonthDate(Widget.FirstDate, index);
        return new DayPicker(
            key: new ValueKey<DateTime>(month),
            calendarDelegate: Widget.CalendarDelegate,
            selectedDate: Widget.SelectedDate,
            currentDate: Widget.CurrentDate,
            onChanged: HandleDateSelected,
            firstDate: Widget.FirstDate,
            lastDate: Widget.LastDate,
            displayedMonth: month,
            selectableDayPredicate: Widget.SelectableDayPredicate);
    }

    public override Widget Build(BuildContext context)
    {
        Color? subHeaderForegroundColor = DatePickerTheme.Of(context).SubHeaderForegroundColor
            ?? DatePickerTheme.Defaults(context).SubHeaderForegroundColor;

        bool supportsAnnounce = MediaQuery.MaybeSupportsAnnounceOf(context) ?? false;
        return new Semantics(
            container: true,
            explicitChildNodes: true,
            liveRegion: !supportsAnnounce,
            accessibilityFocusBlockType: !supportsAnnounce
                ? AccessibilityFocusBlockType.BlockNode
                : AccessibilityFocusBlockType.None,
            label: !supportsAnnounce ? _announcementText : null,
            child: new Column(
                children:
                [
                    new SizedBox(
                        height: Consts.SubHeaderHeight,
                        child: new Padding(
                            EdgeInsetsDirectional.Only(start: 16, end: 4),
                            new Row(
                                children:
                                [
                                    new Spacer(),
                                    new IconButton(
                                        icon: new Icon(
                                            Icons.ChevronLeft,
                                            semanticLabel: IsDisplayingFirstMonth
                                                ? _localizations.PreviousMonthTooltip
                                                : null),
                                        color: subHeaderForegroundColor,
                                        tooltip: IsDisplayingFirstMonth ? null : _localizations.PreviousMonthTooltip,
                                        onPressed: IsDisplayingFirstMonth ? null : HandlePreviousMonth),
                                    new IconButton(
                                        icon: new Icon(
                                            Icons.ChevronRight,
                                            semanticLabel: IsDisplayingLastMonth
                                                ? _localizations.NextMonthTooltip
                                                : null),
                                        color: subHeaderForegroundColor,
                                        tooltip: IsDisplayingLastMonth ? null : _localizations.NextMonthTooltip,
                                        onPressed: IsDisplayingLastMonth ? null : HandleNextMonth),
                                ]))),
                    new Expanded(
                        child: new FocusableActionDetector(
                            shortcuts: _shortcutMap,
                            actions: _actionMap,
                            focusNode: _dayGridFocus,
                            onFocusChange: HandleGridFocusChange,
                            child: new FocusedDate(
                                calendarDelegate: Widget.CalendarDelegate,
                                date: _dayGridFocus.HasFocus ? _focusedDay : null,
                                // Wrap the PageView with `Material`, so when its child paints on materials
                                // the content won't go out of boundary during page transition.
                                child: new Material(
                                    type: MaterialType.Transparency,
                                    child: PageView.Builder(
                                        key: _pageViewKey,
                                        controller: _pageController,
                                        itemBuilder: BuildItems,
                                        itemCount: Widget.CalendarDelegate.MonthDelta(
                                            Widget.FirstDate,
                                            Widget.LastDate) + 1,
                                        onPageChanged: HandleMonthPageChanged))))),
                ]));
    }
}

/// <summary>
/// InheritedWidget indicating what the current focused date is for its children.
/// </summary>
/// <remarks>
/// Dart's private <c>_FocusedDate</c>. This is used by the <see cref="MonthPicker"/> to let its children
/// <see cref="DayPicker"/>s know what the currently focused date (if any) should be. File-local because
/// <c>date_picker.dart</c> declares a private <c>_FocusedDate</c> of its own.
/// </remarks>
file sealed class FocusedDate : InheritedWidget
{
    public FocusedDate(Widget child, CalendarDelegate<DateTime> calendarDelegate, DateTime? date = null)
        : base(child)
    {
        CalendarDelegate = calendarDelegate;
        Date = date;
    }

    public CalendarDelegate<DateTime> CalendarDelegate { get; }

    public DateTime? Date { get; }

    public override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !CalendarDelegate.IsSameDay(Date, ((FocusedDate)oldWidget).Date);
    }

    public static DateTime? MaybeOf(BuildContext context)
    {
        FocusedDate? focusedDate = context.DependOnInheritedWidgetOfExactType<FocusedDate>();
        return focusedDate?.Date;
    }
}

/// <summary>Displays the days of a given month and allows choosing a day.</summary>
/// <remarks>
/// Dart's private <c>_DayPicker</c>. The days are arranged in a rectangular grid with one column for
/// each day of the week.
/// </remarks>
internal sealed class DayPicker : StatefulWidget
{
    /// <summary>Creates a day picker.</summary>
    public DayPicker(
        DateTime currentDate,
        DateTime displayedMonth,
        DateTime firstDate,
        DateTime lastDate,
        DateTime? selectedDate,
        Action<DateTime> onChanged,
        CalendarDelegate<DateTime> calendarDelegate,
        SelectableDayPredicate? selectableDayPredicate = null,
        Key? key = null) : base(key)
    {
        DebugAssertions.Assert(!(firstDate > lastDate));
        DebugAssertions.Assert(selectedDate == null || !(selectedDate.Value < firstDate));
        DebugAssertions.Assert(selectedDate == null || !(selectedDate.Value > lastDate));
        CurrentDate = currentDate;
        DisplayedMonth = displayedMonth;
        FirstDate = firstDate;
        LastDate = lastDate;
        SelectedDate = selectedDate;
        OnChanged = onChanged;
        CalendarDelegate = calendarDelegate;
        SelectableDayPredicate = selectableDayPredicate;
    }

    /// <summary>The currently selected date.</summary>
    /// <remarks>This date is highlighted in the picker.</remarks>
    public DateTime? SelectedDate { get; }

    /// <summary>The current date at the time the picker is displayed.</summary>
    public DateTime CurrentDate { get; }

    /// <summary>Called when the user picks a day.</summary>
    public Action<DateTime> OnChanged { get; }

    /// <summary>The earliest date the user is permitted to pick.</summary>
    /// <remarks>This date must be on or before the <see cref="LastDate"/>.</remarks>
    public DateTime FirstDate { get; }

    /// <summary>The latest date the user is permitted to pick.</summary>
    /// <remarks>This date must be on or after the <see cref="FirstDate"/>.</remarks>
    public DateTime LastDate { get; }

    /// <summary>The month whose days are displayed by this picker.</summary>
    public DateTime DisplayedMonth { get; }

    /// <summary>Optional user supplied predicate function to customize selectable days.</summary>
    public SelectableDayPredicate? SelectableDayPredicate { get; }

    /// <summary>The calendar system the picker interprets, formats, and navigates dates with.</summary>
    public CalendarDelegate<DateTime> CalendarDelegate { get; }

    public override State CreateState() => new DayPickerState();
}

/// <summary>Dart's private <c>_DayPickerState</c>.</summary>
internal sealed class DayPickerState : State<DayPicker>
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
        DateTime? focusedDate = FocusedDate.MaybeOf(Context);
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

    /// <summary>
    /// Builds widgets showing abbreviated days of week. The first widget in the returned list
    /// corresponds to the first day of week for the current locale.
    /// </summary>
    /// <remarks>
    /// Examples:
    /// <code>
    ///     ┌ Sunday is the first day of week in the US (en_US)
    ///     |
    ///     S M T W T F S  ← the returned list contains these widgets
    ///     _ _ _ _ _ 1 2
    ///     3 4 5 6 7 8 9
    ///
    ///     ┌ But it's Monday in the UK (en_GB)
    ///     |
    ///     M T W T F S S  ← the returned list contains these widgets
    ///     _ _ _ _ 1 2 3
    ///     4 5 6 7 8 9 10
    /// </code>
    /// </remarks>
    private static List<Widget> DayHeaders(TextStyle? headerStyle, MaterialLocalizations localizations)
    {
        var result = new List<Widget>();
        for (int i = localizations.FirstDayOfWeekIndex;
             result.Count < Consts.DaysPerWeek;
             i = (i + 1) % Consts.DaysPerWeek)
        {
            string weekday = localizations.NarrowWeekdays[i];
            result.Add(new ExcludeSemantics(
                child: new Center(child: new Text(weekday, style: headerStyle))));
        }

        return result;
    }

    public override Widget Build(BuildContext context)
    {
        MaterialLocalizations localizations = MaterialLocalizations.Of(context);
        DatePickerThemeData datePickerTheme = DatePickerTheme.Of(context);
        DatePickerThemeData defaults = DatePickerTheme.Defaults(context);
        TextStyle? weekdayStyle = datePickerTheme.WeekdayStyle ?? defaults.WeekdayStyle;

        Orientation orientation = MediaQuery.OrientationOf(context);
        bool isLandscapeOrientation = orientation == Orientation.Landscape;

        int year = Widget.DisplayedMonth.Year;
        int month = Widget.DisplayedMonth.Month;

        int daysInMonth = Widget.CalendarDelegate.GetDaysInMonth(year, month);
        int dayOffset = Widget.CalendarDelegate.FirstDayOffset(year, month, localizations);

        List<Widget> dayItems = DayHeaders(weekdayStyle, localizations);
        // 1-based day of month, e.g. 1-31 for January, and 1-29 for February on
        // a leap year.
        int day = -dayOffset;
        while (day < daysInMonth)
        {
            day++;
            if (day < 1)
            {
                dayItems.Add(SizedBox.Shrink());
            }
            else
            {
                DateTime dayToBuild = Widget.CalendarDelegate.GetDay(year, month, day);
                bool isDisabled = dayToBuild > Widget.LastDate
                    || dayToBuild < Widget.FirstDate
                    || (Widget.SelectableDayPredicate != null && !Widget.SelectableDayPredicate(dayToBuild));
                bool isSelectedDay = Widget.CalendarDelegate.IsSameDay(Widget.SelectedDate, dayToBuild);
                bool isToday = Widget.CalendarDelegate.IsSameDay(Widget.CurrentDate, dayToBuild);

                dayItems.Add(new Day(
                    dayToBuild,
                    key: new ValueKey<DateTime>(dayToBuild),
                    isDisabled: isDisabled,
                    isSelectedDay: isSelectedDay,
                    isToday: isToday,
                    onChanged: Widget.OnChanged,
                    focusNode: _dayFocusNodes[day - 1],
                    calendarDelegate: Widget.CalendarDelegate));
            }
        }

        double monthPickerHorizontalPadding = Theme.Of(context).UseMaterial3 && !isLandscapeOrientation
            ? Consts.MonthPickerHorizontalPaddingPortraitM3
            : Consts.MonthPickerHorizontalPaddingOther;
        return new Padding(
            EdgeInsets.Symmetric(horizontal: monthPickerHorizontalPadding),
            MediaQuery.WithClampedTextScaling(
                maxScaleFactor: isLandscapeOrientation
                    ? Consts.KDayPickerGridLandscapeMaxScaleFactor
                    : Consts.KDayPickerGridPortraitMaxScaleFactor,
                child: GridView.Custom(
                    physics: new ClampingScrollPhysics(),
                    gridDelegate: new DayPickerGridDelegate(context),
                    childrenDelegate: new SliverChildListDelegate(dayItems, addRepaintBoundaries: false))));
    }
}

/// <summary>Dart's private <c>_Day</c>.</summary>
internal sealed class Day : StatefulWidget
{
    public Day(
        DateTime day,
        bool isDisabled,
        bool isSelectedDay,
        bool isToday,
        Action<DateTime> onChanged,
        FocusNode focusNode,
        CalendarDelegate<DateTime> calendarDelegate,
        Key? key = null) : base(key)
    {
        DayValue = day;
        IsDisabled = isDisabled;
        IsSelectedDay = isSelectedDay;
        IsToday = isToday;
        OnChanged = onChanged;
        FocusNode = focusNode;
        CalendarDelegate = calendarDelegate;
    }

    /// <summary>Dart's <c>day</c>; C# members may not repeat their declaring type's name.</summary>
    public DateTime DayValue { get; }

    public bool IsDisabled { get; }

    public bool IsSelectedDay { get; }

    public bool IsToday { get; }

    public Action<DateTime> OnChanged { get; }

    public FocusNode FocusNode { get; }

    public CalendarDelegate<DateTime> CalendarDelegate { get; }

    public override State CreateState() => new DayState();
}

/// <summary>Dart's private <c>_DayState</c>.</summary>
internal sealed class DayState : State<Day>
{
    private readonly WidgetStatesController _statesController = new();

    public override Widget Build(BuildContext context)
    {
        DatePickerThemeData defaults = DatePickerTheme.Defaults(context);
        DatePickerThemeData datePickerTheme = DatePickerTheme.Of(context);
        TextStyle? dayStyle = datePickerTheme.DayStyle ?? defaults.DayStyle;
        T? EffectiveValue<T>(Func<DatePickerThemeData?, T?> getProperty) where T : class
        {
            return getProperty(datePickerTheme) ?? getProperty(defaults);
        }

        T? Resolve<T>(
            Func<DatePickerThemeData?, WidgetStateProperty<T?>?> getProperty,
            IReadOnlySet<WidgetState> states) where T : class
        {
            return EffectiveValue(theme => getProperty(theme)?.Resolve(states));
        }

        MaterialLocalizations localizations = MaterialLocalizations.Of(context);
        string semanticLabelSuffix = Widget.IsToday ? $", {localizations.CurrentDateLabel}" : string.Empty;

        var states = new HashSet<WidgetState>();
        if (Widget.IsDisabled)
        {
            states.Add(WidgetState.Disabled);
        }

        if (Widget.IsSelectedDay)
        {
            states.Add(WidgetState.Selected);
        }

        _statesController.Value = states;

        Color? dayForegroundColor = Resolve(
            theme => Widget.IsToday ? theme?.TodayForegroundColor : theme?.DayForegroundColor,
            states);
        Color? dayBackgroundColor = Resolve(
            theme => Widget.IsToday ? theme?.TodayBackgroundColor : theme?.DayBackgroundColor,
            states);
        WidgetStateProperty<Color?> dayOverlayColor = WidgetStateProperty<Color?>.ResolveWith(
            overlayStates => EffectiveValue(theme => theme?.DayOverlayColor?.Resolve(overlayStates)));
        OutlinedBorder dayShape = Resolve(theme => theme?.DayShape, states)!;
        bool hasCustomBorderColor = datePickerTheme.TodayBorder != null
            && datePickerTheme.TodayBorder.Value.Color.Opacity != 0.0;
        BorderSide todayBorderSide = hasCustomBorderColor
            ? datePickerTheme.TodayBorder!.Value
            : (datePickerTheme.TodayBorder ?? defaults.TodayBorder!.Value).CopyWith(color: dayForegroundColor);
        ShapeDecoration decoration = Widget.IsToday
            ? new ShapeDecoration(Color: dayBackgroundColor, Shape: dayShape.CopyWith(side: todayBorderSide))
            : new ShapeDecoration(Color: dayBackgroundColor, Shape: dayShape);

        Widget dayWidget = new Ink(
            decoration: decoration,
            child: new Center(
                child: new Text(
                    localizations.FormatDecimal(Widget.DayValue.Day),
                    style: dayStyle?.Apply(color: dayForegroundColor))));

        // Adds padding as per M3 guidelines for portrait mode. Not applied in landscape
        // mode currently due to unclear specifications.
        Orientation orientation = MediaQuery.OrientationOf(context);
        if (Theme.Of(context).UseMaterial3 && orientation == Orientation.Portrait)
        {
            dayWidget = new Padding(EdgeInsets.All(4.0), dayWidget);
        }

        string fullDate = Widget.CalendarDelegate.FormatFullDate(Widget.DayValue, localizations);
        dayWidget = new Semantics(
            // We want the day of month to be spoken first irrespective of the
            // locale-specific preferences or TextDirection. This is because
            // an accessibility user is more likely to be interested in the
            // day of month before the rest of the date, as they are looking
            // for the day of month. To do that we prepend day of month to the
            // formatted full date.
            label: $"{localizations.FormatDecimal(Widget.DayValue.Day)}, {fullDate}{semanticLabelSuffix}",
            // Set button to true to make the date selectable.
            button: true,
            selected: Widget.IsSelectedDay,
            enabled: !Widget.IsDisabled,
            excludeSemantics: true,
            child: dayWidget);

        if (!Widget.IsDisabled)
        {
            dayWidget = new InkResponse(
                focusNode: Widget.FocusNode,
                onTap: () => Widget.OnChanged(Widget.DayValue),
                statesController: _statesController,
                overlayColor: dayOverlayColor,
                customBorder: dayShape,
                containedInkWell: true,
                child: dayWidget);
        }

        return dayWidget;
    }

    public override void Dispose()
    {
        _statesController.Dispose();
        base.Dispose();
    }
}

/// <summary>Dart's private <c>_DayPickerGridDelegate</c>.</summary>
internal sealed class DayPickerGridDelegate : SliverGridDelegate
{
    public DayPickerGridDelegate(BuildContext context)
    {
        Context = context;
    }

    public BuildContext Context { get; }

    public override SliverGridLayout GetLayout(SliverConstraints constraints)
    {
        double textScaleFactor =
            MediaQuery.TextScalerOf(Context).Clamp(maxScaleFactor: 3.0).Scale(Consts.FontSizeToScale)
            / Consts.FontSizeToScale;
        // Conform to M3 spec in portrait mode (landscape mode is not specified).
        Orientation orientation = MediaQuery.OrientationOf(Context);
        double dayPickerRowHeight = Theme.Of(Context).UseMaterial3 && orientation == Orientation.Portrait
            ? Consts.DayPickerRowHeightM3
            : Consts.DayPickerRowHeightM2;
        double scaledRowHeight = textScaleFactor > 1.3
            ? ((textScaleFactor - 1) * 30) + dayPickerRowHeight
            : dayPickerRowHeight;
        const int columnCount = Consts.DaysPerWeek;
        double tileWidth = constraints.CrossAxisExtent / columnCount;
        double tileHeight = Math.Min(
            scaledRowHeight,
            constraints.ViewportMainAxisExtent / (Consts.MaxDayPickerRowCount + 1));
        return new SliverGridRegularTileLayout(
            childCrossAxisExtent: tileWidth,
            childMainAxisExtent: tileHeight,
            crossAxisCount: columnCount,
            crossAxisStride: tileWidth,
            mainAxisStride: tileHeight,
            reverseCrossAxis: BasicTypes.AxisDirectionIsReversed(constraints.CrossAxisDirection));
    }

    public override bool ShouldRelayout(SliverGridDelegate oldDelegate) => false;
}

/// <summary>A scrollable grid of years to allow picking a year.</summary>
/// <remarks>
/// The year picker widget is rarely used directly. Instead, consider using
/// <see cref="CalendarDatePicker"/>, or <c>showDatePicker</c> which create full date pickers.
/// </remarks>
public class YearPicker : StatefulWidget
{
    /// <summary>Creates a year picker.</summary>
    /// <remarks>
    /// The <paramref name="lastDate"/> must be after the <paramref name="firstDate"/>.
    /// <para>
    /// <paramref name="initialDate"/> is deprecated: this parameter has no effect and can be removed.
    /// Previously it controlled the month that was used in <paramref name="onChanged"/> when a new year
    /// was selected, but now that role is filled by <paramref name="selectedDate"/> instead. This
    /// feature was deprecated after v3.13.0-0.3.pre.
    /// </para>
    /// </remarks>
    public YearPicker(
        DateTime firstDate,
        DateTime lastDate,
        DateTime? selectedDate,
        Action<DateTime> onChanged,
        DateTime? currentDate = null,
        DateTime? initialDate = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        CalendarDelegate<DateTime>? calendarDelegate = null,
        Key? key = null) : base(key)
    {
        _ = initialDate;
        DebugAssertions.Assert(!(firstDate > lastDate));
        CalendarDelegate = calendarDelegate ?? GregorianCalendarDelegate.Instance;
        CurrentDate = CalendarDelegate.DateOnly(currentDate ?? DateTime.Now);
        FirstDate = firstDate;
        LastDate = lastDate;
        SelectedDate = selectedDate;
        OnChanged = onChanged;
        DragStartBehavior = dragStartBehavior;
    }

    /// <summary>The current date.</summary>
    /// <remarks>This date is subtly highlighted in the picker.</remarks>
    public DateTime CurrentDate { get; }

    /// <summary>The earliest date the user is permitted to pick.</summary>
    public DateTime FirstDate { get; }

    /// <summary>The latest date the user is permitted to pick.</summary>
    public DateTime LastDate { get; }

    /// <summary>The currently selected date.</summary>
    /// <remarks>This date is highlighted in the picker.</remarks>
    public DateTime? SelectedDate { get; }

    /// <summary>Called when the user picks a year.</summary>
    public Action<DateTime> OnChanged { get; }

    /// <summary>Determines the way that drag start behavior is handled.</summary>
    public DragStartBehavior DragStartBehavior { get; }

    /// <summary>The calendar system the picker interprets, formats, and navigates dates with.</summary>
    public CalendarDelegate<DateTime> CalendarDelegate { get; }

    public override State CreateState() => new YearPickerState();
}

/// <summary>Dart's private <c>_YearPickerState</c>.</summary>
internal sealed class YearPickerState : State<YearPicker>
{
    private ScrollController? _scrollController;
    private readonly WidgetStatesController _statesController = new();

    // The approximate number of years necessary to fill the available space.
    private const int MinYears = 18;

    public override void InitState()
    {
        base.InitState();
        _scrollController = new ScrollController(
            initialScrollOffset: ScrollOffsetForYear(Widget.SelectedDate ?? Widget.FirstDate));
    }

    public override void Dispose()
    {
        _scrollController?.Dispose();
        _statesController.Dispose();
        base.Dispose();
    }

    public override void DidUpdateWidget(YearPicker oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        if (Widget.SelectedDate != oldWidget.SelectedDate && Widget.SelectedDate != null)
        {
            _scrollController!.JumpTo(ScrollOffsetForYear(Widget.SelectedDate.Value));
        }
    }

    private double ScrollOffsetForYear(DateTime date)
    {
        int initialYearIndex = date.Year - Widget.FirstDate.Year;
        int initialYearRow = initialYearIndex / Consts.YearPickerColumnCount;
        // Move the offset down by 2 rows to approximately center it.
        int centeredYearRow = initialYearRow - 2;
        return ItemCount < MinYears ? 0 : centeredYearRow * Consts.YearPickerRowHeight;
    }

    private Widget BuildYearItem(BuildContext context, int index)
    {
        DatePickerThemeData datePickerTheme = DatePickerTheme.Of(context);
        DatePickerThemeData defaults = DatePickerTheme.Defaults(context);

        T? EffectiveValue<T>(Func<DatePickerThemeData?, T?> getProperty) where T : class
        {
            return getProperty(datePickerTheme) ?? getProperty(defaults);
        }

        T? Resolve<T>(
            Func<DatePickerThemeData?, WidgetStateProperty<T?>?> getProperty,
            IReadOnlySet<WidgetState> states) where T : class
        {
            return EffectiveValue(theme => getProperty(theme)?.Resolve(states));
        }

        double textScaleFactor =
            MediaQuery.TextScalerOf(context).Clamp(maxScaleFactor: 3.0).Scale(Consts.FontSizeToScale)
            / Consts.FontSizeToScale;

        // Backfill the _YearPicker with disabled years if necessary.
        int offset = ItemCount < MinYears ? (MinYears - ItemCount) / 2 : 0;
        int year = Widget.FirstDate.Year + index - offset;
        bool isSelected = year == Widget.SelectedDate?.Year;
        bool isCurrentYear = year == Widget.CurrentDate.Year;
        bool isDisabled = year < Widget.FirstDate.Year || year > Widget.LastDate.Year;
        double decorationHeight = 36.0 * textScaleFactor;
        double decorationWidth = 72.0 * textScaleFactor;

        var states = new HashSet<WidgetState>();
        if (isDisabled)
        {
            states.Add(WidgetState.Disabled);
        }

        if (isSelected)
        {
            states.Add(WidgetState.Selected);
        }

        Color? textColor = Resolve(
            theme => isCurrentYear ? theme?.TodayForegroundColor : theme?.YearForegroundColor,
            states);
        Color? background = Resolve(
            theme => isCurrentYear ? theme?.TodayBackgroundColor : theme?.YearBackgroundColor,
            states);
        WidgetStateProperty<Color?> overlayColor = WidgetStateProperty<Color?>.ResolveWith(
            overlayStates => EffectiveValue(theme => theme?.YearOverlayColor?.Resolve(overlayStates)));

        OutlinedBorder yearShape = Resolve(theme => theme?.YearShape, states)!;

        BorderSide? borderSide = null;
        if (isCurrentYear)
        {
            borderSide = datePickerTheme.TodayBorder ?? defaults.TodayBorder;
            if (borderSide != null)
            {
                borderSide = borderSide.Value.CopyWith(color: textColor);
            }
        }

        var decoration = new ShapeDecoration(Color: background, Shape: yearShape.CopyWith(side: borderSide));

        TextStyle? itemStyle = (datePickerTheme.YearStyle ?? defaults.YearStyle)?.Apply(color: textColor);
        MaterialLocalizations localizations = MaterialLocalizations.Of(context);
        Widget yearItem = new Center(
            child: new Container(
                decoration: decoration,
                height: decorationHeight,
                width: decorationWidth,
                alignment: Alignment.Center,
                child: new Semantics(
                    selected: isSelected,
                    enabled: !isDisabled,
                    button: true,
                    child: new Text(Widget.CalendarDelegate.FormatYear(year, localizations), style: itemStyle))));

        if (!isDisabled)
        {
            DateTime date = Widget.CalendarDelegate.GetMonth(
                year,
                Widget.SelectedDate?.Month ?? Consts.January);
            if (date < Widget.CalendarDelegate.GetMonth(Widget.FirstDate.Year, Widget.FirstDate.Month))
            {
                // Ignore firstDate.day because we're just working in years and months here.
                DebugAssertions.Assert(date.Year == Widget.FirstDate.Year);
                date = Widget.CalendarDelegate.GetMonth(year, Widget.FirstDate.Month);
            }
            else if (date > Widget.LastDate)
            {
                // No need to ignore the day here because it can only be bigger than what we care about.
                DebugAssertions.Assert(date.Year == Widget.LastDate.Year);
                date = Widget.CalendarDelegate.GetMonth(year, Widget.LastDate.Month);
            }

            _statesController.Value = states;
            yearItem = new InkWell(
                key: new ValueKey<int>(year),
                onTap: () => Widget.OnChanged(date),
                statesController: _statesController,
                overlayColor: overlayColor,
                child: yearItem);
        }

        return yearItem;
    }

    private int ItemCount => Widget.LastDate.Year - Widget.FirstDate.Year + 1;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            children:
            [
                new Divider(),
                new Expanded(
                    child: new Material(
                        type: MaterialType.Transparency,
                        child: GridView.Builder(
                            controller: _scrollController,
                            dragStartBehavior: Widget.DragStartBehavior,
                            gridDelegate: new YearPickerGridDelegate(context),
                            itemBuilder: BuildYearItem,
                            itemCount: Math.Max(ItemCount, MinYears),
                            padding: new Thickness(Consts.YearPickerPadding, 0)))),
                new Divider(),
            ]);
    }
}

/// <summary>Dart's private <c>_YearPickerGridDelegate</c>.</summary>
internal sealed class YearPickerGridDelegate : SliverGridDelegate
{
    public YearPickerGridDelegate(BuildContext context)
    {
        Context = context;
    }

    public BuildContext Context { get; }

    public override SliverGridLayout GetLayout(SliverConstraints constraints)
    {
        double textScaleFactor =
            MediaQuery.TextScalerOf(Context).Clamp(maxScaleFactor: 3.0).Scale(Consts.FontSizeToScale)
            / Consts.FontSizeToScale;
        int scaledYearPickerColumnCount = textScaleFactor > 1.65
            ? Consts.YearPickerColumnCount - 1
            : Consts.YearPickerColumnCount;
        double tileWidth = Math.Max(
            (constraints.CrossAxisExtent - (scaledYearPickerColumnCount - 1) * Consts.YearPickerRowSpacing)
            / scaledYearPickerColumnCount,
            0.0);
        double scaledYearPickerRowHeight = textScaleFactor > 1
            ? Consts.YearPickerRowHeight + ((textScaleFactor - 1) * 9)
            : Consts.YearPickerRowHeight;
        return new SliverGridRegularTileLayout(
            childCrossAxisExtent: tileWidth,
            childMainAxisExtent: scaledYearPickerRowHeight,
            crossAxisCount: scaledYearPickerColumnCount,
            crossAxisStride: tileWidth + Consts.YearPickerRowSpacing,
            mainAxisStride: scaledYearPickerRowHeight,
            reverseCrossAxis: BasicTypes.AxisDirectionIsReversed(constraints.CrossAxisDirection));
    }

    public override bool ShouldRelayout(SliverGridDelegate oldDelegate) => false;
}
