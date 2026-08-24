using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: cupertino_ui/lib/src/date_picker.dart

namespace Plumix.Cupertino;

public delegate Widget? SelectionOverlayBuilder(
    BuildContext context,
    int columnCount,
    int selectedIndex);

public delegate bool SelectableDayPredicate(DateTime day);

public enum CupertinoDatePickerMode
{
    Time,
    Date,
    DateAndTime,
    MonthYear,
}

public sealed class CupertinoDatePicker : StatefulWidget
{
    internal const double DefaultItemExtent = 32.0;
    internal const double PickerWidth = 320.0;
    internal const double PickerHeight = 216.0;
    internal const double DatePickerPadSize = 12.0;
    internal const double Squeeze = 1.25;
    internal const double Magnification = 2.35 / 2.1;
    internal static readonly TimeSpan ScrollToDateDuration = TimeSpan.FromMilliseconds(200.0);

    public CupertinoDatePicker(
        Action<DateTime> onDateTimeChanged,
        CupertinoDatePickerMode mode = CupertinoDatePickerMode.DateAndTime,
        DateTime? initialDateTime = null,
        DateTime? minimumDate = null,
        DateTime? maximumDate = null,
        int minimumYear = 1,
        int? maximumYear = null,
        int minuteInterval = 1,
        bool use24hFormat = false,
        DatePickerDateOrder? dateOrder = null,
        CupertinoDynamicColor? backgroundColor = null,
        bool showDayOfWeek = false,
        bool showTimeSeparator = false,
        double itemExtent = DefaultItemExtent,
        SelectionOverlayBuilder? selectionOverlayBuilder = null,
        SelectableDayPredicate? selectableDayPredicate = null,
        ChangeReportingBehavior changeReportingBehavior = ChangeReportingBehavior.OnScrollUpdate,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(onDateTimeChanged);
        DateTime effectiveInitialDateTime = initialDateTime ?? DateTime.Now;
        if (!(itemExtent > 0.0))
        {
            throw new ArgumentOutOfRangeException(nameof(itemExtent), "item extent should be greater than 0");
        }

        ValidateInterval(minuteInterval, nameof(minuteInterval));
        if (effectiveInitialDateTime.Minute % minuteInterval != 0)
        {
            throw new ArgumentException("initial minute is not divisible by minute interval", nameof(initialDateTime));
        }

        if (mode is CupertinoDatePickerMode.DateAndTime or CupertinoDatePickerMode.Time)
        {
            if (minimumDate is DateTime minimum && effectiveInitialDateTime < minimum)
            {
                throw new ArgumentException("initial date is before minimum date", nameof(initialDateTime));
            }

            if (maximumDate is DateTime maximum && effectiveInitialDateTime > maximum)
            {
                throw new ArgumentException("initial date is after maximum date", nameof(initialDateTime));
            }
        }

        if (mode is CupertinoDatePickerMode.Date or CupertinoDatePickerMode.MonthYear)
        {
            if (minimumYear < 1 || effectiveInitialDateTime.Year < minimumYear)
            {
                throw new ArgumentException(
                    "initial year is not greater than minimum year, or minimum year is not positive",
                    nameof(minimumYear));
            }

            if (maximumYear is int lastYear && effectiveInitialDateTime.Year > lastYear)
            {
                throw new ArgumentException("initial year is not smaller than maximum year", nameof(maximumYear));
            }

            if (minimumDate is DateTime minimum && DateOnly.FromDateTime(minimum) > DateOnly.FromDateTime(effectiveInitialDateTime))
            {
                throw new ArgumentException(
                    $"initial date {effectiveInitialDateTime} is not greater than or equal to minimumDate {minimum}",
                    nameof(initialDateTime));
            }

            if (maximumDate is DateTime maximum && DateOnly.FromDateTime(maximum) < DateOnly.FromDateTime(effectiveInitialDateTime))
            {
                throw new ArgumentException(
                    $"initial date {effectiveInitialDateTime} is not less than or equal to maximumDate {maximum}",
                    nameof(initialDateTime));
            }
        }

        if (showDayOfWeek && mode != CupertinoDatePickerMode.Date)
        {
            throw new ArgumentException("showDayOfWeek is only supported in date mode", nameof(showDayOfWeek));
        }

        if (showTimeSeparator && mode is not CupertinoDatePickerMode.Time and not CupertinoDatePickerMode.DateAndTime)
        {
            throw new ArgumentException(
                "showTimeSeparator is only supported in time or dateAndTime modes",
                nameof(showTimeSeparator));
        }

        if (selectableDayPredicate is not null
            && initialDateTime is DateTime explicitInitialDate
            && !selectableDayPredicate(explicitInitialDate))
        {
            throw new ArgumentException(
                $"{explicitInitialDate} must satisfy provided selectableDayPredicate.",
                nameof(initialDateTime));
        }

        Mode = mode;
        OnDateTimeChanged = onDateTimeChanged;
        InitialDateTime = effectiveInitialDateTime;
        MinimumDate = minimumDate;
        MaximumDate = maximumDate;
        MinimumYear = minimumYear;
        MaximumYear = maximumYear;
        MinuteInterval = minuteInterval;
        Use24hFormat = use24hFormat;
        DateOrder = dateOrder;
        BackgroundColor = backgroundColor;
        ShowDayOfWeek = showDayOfWeek;
        ShowTimeSeparator = showTimeSeparator;
        ItemExtent = itemExtent;
        SelectionOverlayBuilder = selectionOverlayBuilder;
        SelectableDayPredicate = selectableDayPredicate;
        ChangeReportingBehavior = changeReportingBehavior;
    }

    public CupertinoDatePickerMode Mode { get; }

    public Action<DateTime> OnDateTimeChanged { get; }

    public DateTime InitialDateTime { get; }

    public DateTime? MinimumDate { get; }

    public DateTime? MaximumDate { get; }

    public int MinimumYear { get; }

    public int? MaximumYear { get; }

    public int MinuteInterval { get; }

    public bool Use24hFormat { get; }

    public DatePickerDateOrder? DateOrder { get; }

    public CupertinoDynamicColor? BackgroundColor { get; }

    public bool ShowDayOfWeek { get; }

    public bool ShowTimeSeparator { get; }

    public double ItemExtent { get; }

    public SelectionOverlayBuilder? SelectionOverlayBuilder { get; }

    public SelectableDayPredicate? SelectableDayPredicate { get; }

    public ChangeReportingBehavior ChangeReportingBehavior { get; }

    public override State CreateState()
    {
        return Mode switch
        {
            CupertinoDatePickerMode.Date => new CupertinoDatePickerDateState(),
            CupertinoDatePickerMode.MonthYear => new CupertinoDatePickerMonthYearState(),
            _ => new CupertinoDatePickerDateTimeState(),
        };
    }

    public static double GetColumnWidth(
        IReadOnlyList<string> texts,
        BuildContext context,
        TextStyle? textStyle = null)
    {
        ArgumentNullException.ThrowIfNull(texts);
        TextStyle effectiveStyle = textStyle ?? ThemeTextStyle(context);
        double maximumWidth = 0.0;
        using var painter = new TextPainter(textDirection: Directionality.Of(context));
        foreach (string text in texts)
        {
            painter.Text = new TextSpan(text: text, style: effectiveStyle);
            painter.Layout();
            maximumWidth = Math.Max(maximumWidth, painter.Width);
        }

        return maximumWidth;
    }

    internal static TextStyle ThemeTextStyle(BuildContext context) =>
        CupertinoTheme.Of(context).TextTheme.DateTimePickerTextStyle;

    internal static void ValidateInterval(int interval, string parameterName)
    {
        if (interval <= 0 || 60 % interval != 0)
        {
            throw new ArgumentException("minute interval is not a positive integer factor of 60", parameterName);
        }
    }
}

public enum CupertinoTimerPickerMode
{
    Hm,
    Ms,
    Hms,
}

public sealed class CupertinoTimerPicker : StatefulWidget
{
    public CupertinoTimerPicker(
        Action<TimeSpan> onTimerDurationChanged,
        CupertinoTimerPickerMode mode = CupertinoTimerPickerMode.Hms,
        TimeSpan? initialTimerDuration = null,
        int minuteInterval = 1,
        int secondInterval = 1,
        AlignmentGeometry alignment = default,
        CupertinoDynamicColor? backgroundColor = null,
        double itemExtent = CupertinoDatePicker.DefaultItemExtent,
        ChangeReportingBehavior changeReportingBehavior = ChangeReportingBehavior.OnScrollUpdate,
        SelectionOverlayBuilder? selectionOverlayBuilder = null,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(onTimerDurationChanged);
        TimeSpan effectiveDuration = initialTimerDuration ?? TimeSpan.Zero;
        if (effectiveDuration < TimeSpan.Zero || effectiveDuration >= TimeSpan.FromDays(1.0))
        {
            throw new ArgumentOutOfRangeException(nameof(initialTimerDuration));
        }

        CupertinoDatePicker.ValidateInterval(minuteInterval, nameof(minuteInterval));
        if (secondInterval <= 0 || 60 % secondInterval != 0)
        {
            throw new ArgumentException("second interval is not a positive integer factor of 60", nameof(secondInterval));
        }

        if ((int)effectiveDuration.TotalMinutes % minuteInterval != 0)
        {
            throw new ArgumentException(nameof(initialTimerDuration));
        }

        if ((int)effectiveDuration.TotalSeconds % secondInterval != 0)
        {
            throw new ArgumentException(nameof(initialTimerDuration));
        }

        if (!(itemExtent > 0.0))
        {
            throw new ArgumentOutOfRangeException(nameof(itemExtent), "item extent should be greater than 0");
        }

        Mode = mode;
        InitialTimerDuration = effectiveDuration;
        MinuteInterval = minuteInterval;
        SecondInterval = secondInterval;
        Alignment = alignment == default ? Plumix.Rendering.Alignment.Center : alignment;
        BackgroundColor = backgroundColor;
        ItemExtent = itemExtent;
        OnTimerDurationChanged = onTimerDurationChanged;
        ChangeReportingBehavior = changeReportingBehavior;
        SelectionOverlayBuilder = selectionOverlayBuilder;
    }

    public CupertinoTimerPickerMode Mode { get; }

    public TimeSpan InitialTimerDuration { get; }

    public int MinuteInterval { get; }

    public int SecondInterval { get; }

    public AlignmentGeometry Alignment { get; }

    public CupertinoDynamicColor? BackgroundColor { get; }

    public double ItemExtent { get; }

    public Action<TimeSpan> OnTimerDurationChanged { get; }

    public ChangeReportingBehavior ChangeReportingBehavior { get; }

    public SelectionOverlayBuilder? SelectionOverlayBuilder { get; }

    public override State CreateState() => new CupertinoTimerPickerState();
}

internal static class CupertinoDatePickerHelpers
{
    public static int Modulo(int value, int divisor) => ((value % divisor) + divisor) % divisor;

    public static Widget WrapPicker(
        BuildContext context,
        Widget child,
        bool valid,
        string? semanticsLabel = null,
        AlignmentGeometry alignment = default,
        EdgeInsetsGeometry padding = default,
        double? maxWidth = null)
    {
        TextStyle baseStyle = CupertinoDatePicker.ThemeTextStyle(context);
        TextStyle style = valid
            ? baseStyle
            : baseStyle.CopyWith(color: CupertinoDynamicColor.Resolve(CupertinoColors.InactiveGray, context));
        Widget result = child is Text text
            ? new Text(
                text.Data ?? string.Empty,
                style: style,
                textAlign: text.TextAlign,
                semanticsLabel: semanticsLabel)
            : child;
        if (!valid)
        {
            result = new ExcludeSemantics(child: result);
        }

        if (maxWidth is double width)
        {
            result = new ConstrainedBox(new BoxConstraints(MaxWidth: width), result);
        }

        result = new Align(
            alignment: alignment == default ? Plumix.Rendering.Alignment.Center : alignment,
            child: result);
        return padding == default ? result : new Padding(padding, result);
    }

    public static Widget? Overlay(
        BuildContext context,
        SelectionOverlayBuilder? builder,
        int columnCount,
        int selectedIndex)
    {
        if (builder is not null)
        {
            return builder(context, columnCount, selectedIndex);
        }

        return new CupertinoPickerDefaultSelectionOverlay(
            capStartEdge: selectedIndex == 0,
            capEndEdge: selectedIndex == columnCount - 1);
    }

    public static DateTime DateWithDayClamped(int year, int month, int day)
    {
        int lastDay = DateTime.DaysInMonth(year, month);
        return new DateTime(year, month, Math.Min(day, lastDay));
    }

    public static bool SameDate(DateTime left, DateTime right) =>
        left.Year == right.Year && left.Month == right.Month && left.Day == right.Day;
}

internal sealed class DatePickerLayoutDelegate : MultiChildLayoutDelegate
{
    private readonly IReadOnlyList<double> _columnWidths;
    private readonly double _textDirectionFactor;
    private readonly double _maxWidth;

    public DatePickerLayoutDelegate(
        IReadOnlyList<double> columnWidths,
        double textDirectionFactor,
        double maxWidth)
    {
        _columnWidths = columnWidths;
        _textDirectionFactor = textDirectionFactor;
        _maxWidth = maxWidth;
    }

    public override void PerformLayout(Size size)
    {
        double remainingWidth = Math.Min(_maxWidth, size.Width);
        double horizontalOffset = (size.Width - remainingWidth) / 2.0;
        foreach (double width in _columnWidths)
        {
            remainingWidth -= width + (CupertinoDatePicker.DatePickerPadSize * 2.0);
        }

        int count = _columnWidths.Count;
        for (int visualIndex = 0; visualIndex < count; visualIndex++)
        {
            int logicalIndex = _textDirectionFactor < 0.0 ? count - visualIndex - 1 : visualIndex;
            double childWidth = _columnWidths[logicalIndex] + (CupertinoDatePicker.DatePickerPadSize * 2.0);
            if (logicalIndex == 0 || logicalIndex == count - 1)
            {
                childWidth += remainingWidth / 2.0;
            }

            if (childWidth < 0.0)
            {
                throw new InvalidOperationException(
                    "Insufficient horizontal space to render the CupertinoDatePicker.");
            }

            LayoutChild(
                logicalIndex,
                BoxConstraints.Tight(new Size(Math.Max(0.0, childWidth), size.Height)));
            PositionChild(logicalIndex, new Point(horizontalOffset, 0.0));
            horizontalOffset += childWidth;
        }
    }

    public override bool ShouldRelayout(MultiChildLayoutDelegate oldDelegate)
    {
        var old = (DatePickerLayoutDelegate)oldDelegate;
        return !_columnWidths.SequenceEqual(old._columnWidths)
               || _textDirectionFactor != old._textDirectionFactor;
    }
}

internal sealed class CupertinoDatePickerDateState : State
{
    private FixedExtentScrollController _dayController = null!;
    private FixedExtentScrollController _monthController = null!;
    private FixedExtentScrollController _yearController = null!;
    private int _selectedDay;
    private int _selectedMonth;
    private int _selectedYear;
    private bool _correctionScheduled;

    private CupertinoDatePicker Current => (CupertinoDatePicker)StateWidget;

    public override void InitState()
    {
        base.InitState();
        _selectedDay = Current.InitialDateTime.Day;
        _selectedMonth = Current.InitialDateTime.Month;
        _selectedYear = Current.InitialDateTime.Year;
        _dayController = new FixedExtentScrollController(initialItem: _selectedDay - 1);
        _monthController = new FixedExtentScrollController(initialItem: _selectedMonth - 1);
        _yearController = new FixedExtentScrollController(initialItem: _selectedYear - Current.MinimumYear);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var oldPicker = (CupertinoDatePicker)oldWidget;
        if (oldPicker.Mode != Current.Mode)
        {
            throw new InvalidOperationException("The CupertinoDatePicker's mode cannot change once it's built.");
        }

        base.DidUpdateWidget(oldWidget);
    }

    public override Widget Build(BuildContext context)
    {
        CupertinoLocalizations localizations = CupertinoLocalizations.Of(context);
        TextDirection direction = Directionality.Of(context);
        double directionFactor = direction == TextDirection.Ltr ? 1.0 : -1.0;
        DatePickerDateOrder order = Current.DateOrder ?? localizations.DatePickerDateOrder;

        double dayWidth = CupertinoDatePicker.GetColumnWidth(
            Enumerable.Range(1, 31)
                .Select(day => Current.ShowDayOfWeek
                    ? localizations.DatePickerDayOfMonth(day, ((day + 5) % 7) + 1)
                    : localizations.DatePickerDayOfMonth(day))
                .ToArray(),
            context);
        double monthWidth = CupertinoDatePicker.GetColumnWidth(
            Enumerable.Range(1, 12).Select(localizations.DatePickerMonth).ToArray(),
            context);
        double yearWidth = CupertinoDatePicker.GetColumnWidth(
            [localizations.DatePickerYear(2018)],
            context);

        var columns = new List<DatePickerColumn>(3);
        DatePickerColumn day = new(
            dayWidth,
            _dayController,
            null,
            true,
            BuildDay,
            SelectDay);
        DatePickerColumn month = new(
            monthWidth,
            _monthController,
            null,
            true,
            BuildMonth,
            SelectMonth);
        int? yearCount = Current.MaximumYear is int maximumYear
            ? maximumYear - Current.MinimumYear + 1
            : null;
        DatePickerColumn year = new(
            yearWidth,
            _yearController,
            yearCount,
            false,
            BuildYear,
            SelectYear);
        switch (order)
        {
            case DatePickerDateOrder.Mdy:
                columns.AddRange([month, day, year]);
                break;
            case DatePickerDateOrder.Dmy:
                columns.AddRange([day, month, year]);
                break;
            case DatePickerDateOrder.Ymd:
                columns.AddRange([year, month, day]);
                break;
            case DatePickerDateOrder.Ydm:
                columns.AddRange([year, day, month]);
                break;
        }

        return BuildPickerLayout(context, columns, directionFactor, localizations);
    }

    public override void Dispose()
    {
        _dayController.Dispose();
        _monthController.Dispose();
        _yearController.Dispose();
        base.Dispose();
    }

    private Widget BuildPickerLayout(
        BuildContext context,
        IReadOnlyList<DatePickerColumn> columns,
        double directionFactor,
        CupertinoLocalizations localizations)
    {
        double[] widths = columns.Select(column => column.Width).ToArray();
        double totalWidth = 4.0 * CupertinoDatePicker.DatePickerPadSize;
        foreach (double width in widths)
        {
            totalWidth += width + (2.0 * CupertinoDatePicker.DatePickerPadSize);
        }

        double maxWidth = Math.Max(totalWidth, CupertinoDatePicker.PickerWidth);
        var children = new List<Widget>(columns.Count);
        for (int index = 0; index < columns.Count; index++)
        {
            int physicalIndex = index;
            DatePickerColumn column = columns[index];
            double offAxisFraction = (index - 1) * 0.3 * directionFactor;
            Widget picker = CupertinoPicker.Builder(
                itemExtent: Current.ItemExtent,
                onSelectedItemChanged: column.OnSelected,
                itemBuilder: (itemContext, itemIndex) => column.Builder(
                    itemContext,
                    itemIndex,
                    localizations,
                    column.Width + CupertinoDatePicker.DatePickerPadSize),
                selectionOverlay: CupertinoDatePickerHelpers.Overlay(
                    context,
                    Current.SelectionOverlayBuilder,
                    columns.Count,
                    physicalIndex),
                childCount: column.ChildCount,
                backgroundColor: Current.BackgroundColor,
                offAxisFraction: offAxisFraction,
                useMagnifier: true,
                magnification: CupertinoDatePicker.Magnification,
                scrollController: column.Controller,
                squeeze: CupertinoDatePicker.Squeeze,
                changeReportingBehavior: Current.ChangeReportingBehavior);
            children.Add(new LayoutId(
                physicalIndex,
                new NotificationListener<ScrollEndNotification>(
                    picker,
                    onNotification: HandleScrollEnd)));
        }

        Widget layout = new CustomMultiChildLayout(
            new DatePickerLayoutDelegate(widths, directionFactor, maxWidth),
            children);
        return MediaQuery.WithNoTextScaling(
            context,
            DefaultTextStyle.Merge(
                child: layout,
                style: new TextStyle(LetterSpacing: -0.83)));
    }

    private Widget? BuildDay(
        BuildContext context,
        int index,
        CupertinoLocalizations localizations,
        double maxWidth)
    {
        int day = CupertinoDatePickerHelpers.Modulo(index, 31) + 1;
        DateTime candidate = CreateCandidate(day: day);
        bool valid = IsValid(candidate) && day <= DateTime.DaysInMonth(_selectedYear, _selectedMonth);
        int weekday = ((int)candidate.DayOfWeek + 6) % 7 + 1;
        string text = localizations.DatePickerDayOfMonth(day, Current.ShowDayOfWeek ? weekday : null);
        return CupertinoDatePickerHelpers.WrapPicker(
            context,
            new Text(text),
            valid,
            maxWidth: maxWidth);
    }

    private Widget? BuildMonth(
        BuildContext context,
        int index,
        CupertinoLocalizations localizations,
        double maxWidth)
    {
        int month = CupertinoDatePickerHelpers.Modulo(index, 12) + 1;
        DateTime candidate = CreateCandidate(month: month);
        bool valid = _selectedDay <= DateTime.DaysInMonth(_selectedYear, month) && IsValid(candidate);
        return CupertinoDatePickerHelpers.WrapPicker(
            context,
            new Text(localizations.DatePickerMonth(month)),
            valid,
            maxWidth: maxWidth);
    }

    private Widget? BuildYear(
        BuildContext context,
        int index,
        CupertinoLocalizations localizations,
        double maxWidth)
    {
        int year = Current.MinimumYear + index;
        if (year < 1 || year > DateTime.MaxValue.Year)
        {
            return null;
        }

        DateTime candidate = CreateCandidate(year: year);
        bool valid = _selectedDay <= DateTime.DaysInMonth(year, _selectedMonth) && IsValid(candidate);
        return CupertinoDatePickerHelpers.WrapPicker(
            context,
            new Text(localizations.DatePickerYear(year)),
            valid,
            maxWidth: maxWidth);
    }

    private void SelectDay(int index)
    {
        int value = CupertinoDatePickerHelpers.Modulo(index, 31) + 1;
        SetState(() => _selectedDay = value);
        ReportIfValid();
    }

    private void SelectMonth(int index)
    {
        int value = CupertinoDatePickerHelpers.Modulo(index, 12) + 1;
        SetState(() => _selectedMonth = value);
        ReportIfValid();
    }

    private void SelectYear(int index)
    {
        int value = Current.MinimumYear + index;
        SetState(() => _selectedYear = value);
        ReportIfValid();
    }

    private bool HandleScrollEnd(ScrollEndNotification notification)
    {
        ScheduleCorrection();
        return false;
    }

    private void ScheduleCorrection()
    {
        if (_correctionScheduled)
        {
            return;
        }

        _correctionScheduled = true;
        global::Plumix.Scheduler.AddPostFrameCallback(_ =>
        {
            _correctionScheduled = false;
            CorrectSelection();
        });
    }

    private async void CorrectSelection()
    {
        DateTime candidate = CreateCandidate();
        DateTime corrected = candidate;
        if (_selectedDay > DateTime.DaysInMonth(_selectedYear, _selectedMonth))
        {
            corrected = new DateTime(
                _selectedYear,
                _selectedMonth,
                DateTime.DaysInMonth(_selectedYear, _selectedMonth));
        }

        if (Current.MinimumDate is DateTime minimum
            && DateOnly.FromDateTime(corrected) < DateOnly.FromDateTime(minimum))
        {
            corrected = minimum.Date;
        }

        if (Current.MaximumDate is DateTime maximum
            && DateOnly.FromDateTime(corrected) > DateOnly.FromDateTime(maximum))
        {
            corrected = maximum.Date;
        }

        if (Current.SelectableDayPredicate is not null && !Current.SelectableDayPredicate(corrected))
        {
            DateTime next = corrected.AddDays(1.0);
            if (IsValid(next))
            {
                corrected = next;
            }
            else
            {
                return;
            }
        }

        if (CupertinoDatePickerHelpers.SameDate(candidate, corrected))
        {
            return;
        }

        SetState(() =>
        {
            _selectedYear = corrected.Year;
            _selectedMonth = corrected.Month;
            _selectedDay = corrected.Day;
        });
        await Task.WhenAll(
            _dayController.AnimateToItem(
                corrected.Day - 1,
                CupertinoDatePicker.ScrollToDateDuration,
                Curves.EaseInOut),
            _monthController.AnimateToItem(
                corrected.Month - 1,
                CupertinoDatePicker.ScrollToDateDuration,
                Curves.EaseInOut),
            _yearController.AnimateToItem(
                corrected.Year - Current.MinimumYear,
                CupertinoDatePicker.ScrollToDateDuration,
                Curves.EaseInOut));
    }

    private void ReportIfValid()
    {
        DateTime candidate = CreateCandidate();
        if (_selectedDay <= DateTime.DaysInMonth(_selectedYear, _selectedMonth) && IsValid(candidate))
        {
            Current.OnDateTimeChanged(candidate);
        }
    }

    private DateTime CreateCandidate(int? year = null, int? month = null, int? day = null)
    {
        int candidateYear = year ?? _selectedYear;
        int candidateMonth = month ?? _selectedMonth;
        int candidateDay = Math.Min(day ?? _selectedDay, DateTime.DaysInMonth(candidateYear, candidateMonth));
        return new DateTime(candidateYear, candidateMonth, candidateDay);
    }

    private bool IsValid(DateTime date)
    {
        DateOnly value = DateOnly.FromDateTime(date);
        if (Current.MinimumDate is DateTime minimum && value < DateOnly.FromDateTime(minimum))
        {
            return false;
        }

        if (Current.MaximumDate is DateTime maximum && value > DateOnly.FromDateTime(maximum))
        {
            return false;
        }

        return Current.SelectableDayPredicate?.Invoke(date) ?? true;
    }

    private sealed record DatePickerColumn(
        double Width,
        FixedExtentScrollController Controller,
        int? ChildCount,
        bool Looping,
        Func<BuildContext, int, CupertinoLocalizations, double, Widget?> Builder,
        Action<int> OnSelected);
}

internal sealed class CupertinoDatePickerMonthYearState : State
{
    private FixedExtentScrollController _monthController = null!;
    private FixedExtentScrollController _yearController = null!;
    private int _selectedMonth;
    private int _selectedYear;
    private bool _correctionScheduled;

    private CupertinoDatePicker Current => (CupertinoDatePicker)StateWidget;

    public override void InitState()
    {
        base.InitState();
        _selectedMonth = Current.InitialDateTime.Month;
        _selectedYear = Current.InitialDateTime.Year;
        _monthController = new FixedExtentScrollController(initialItem: _selectedMonth - 1);
        _yearController = new FixedExtentScrollController(initialItem: _selectedYear - Current.MinimumYear);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        if (((CupertinoDatePicker)oldWidget).Mode != Current.Mode)
        {
            throw new InvalidOperationException("The CupertinoDatePicker's mode cannot change once it's built.");
        }

        base.DidUpdateWidget(oldWidget);
    }

    public override Widget Build(BuildContext context)
    {
        CupertinoLocalizations localizations = CupertinoLocalizations.Of(context);
        TextDirection direction = Directionality.Of(context);
        double directionFactor = direction == TextDirection.Ltr ? 1.0 : -1.0;
        DatePickerDateOrder order = Current.DateOrder ?? localizations.DatePickerDateOrder;
        double monthWidth = CupertinoDatePicker.GetColumnWidth(
            Enumerable.Range(1, 12).Select(localizations.DatePickerStandaloneMonth).ToArray(),
            context);
        double yearWidth = CupertinoDatePicker.GetColumnWidth(
            [localizations.DatePickerYear(2018)],
            context);
        bool yearFirst = order is DatePickerDateOrder.Ymd or DatePickerDateOrder.Ydm;
        IReadOnlyList<double> widths = yearFirst ? [yearWidth, monthWidth] : [monthWidth, yearWidth];
        int? yearCount = Current.MaximumYear is int maximumYear
            ? maximumYear - Current.MinimumYear + 1
            : null;
        int monthSelectedIndex = yearFirst ? 1 : 0;
        int yearSelectedIndex = yearFirst ? 0 : 1;

        Widget monthPicker = CupertinoPicker.Builder(
            itemExtent: CupertinoDatePicker.DefaultItemExtent,
            onSelectedItemChanged: SelectMonth,
            itemBuilder: (itemContext, index) => BuildMonth(itemContext, index, localizations, monthWidth),
            selectionOverlay: CupertinoDatePickerHelpers.Overlay(
                context,
                Current.SelectionOverlayBuilder,
                2,
                monthSelectedIndex),
            backgroundColor: Current.BackgroundColor,
            offAxisFraction: (yearFirst ? 0.5 : -0.3) * directionFactor,
            useMagnifier: true,
            magnification: CupertinoDatePicker.Magnification,
            scrollController: _monthController,
            squeeze: CupertinoDatePicker.Squeeze,
            changeReportingBehavior: Current.ChangeReportingBehavior);
        Widget yearPicker = CupertinoPicker.Builder(
            itemExtent: CupertinoDatePicker.DefaultItemExtent,
            onSelectedItemChanged: SelectYear,
            itemBuilder: (itemContext, index) => BuildYear(itemContext, index, localizations, yearWidth),
            selectionOverlay: CupertinoDatePickerHelpers.Overlay(
                context,
                Current.SelectionOverlayBuilder,
                2,
                yearSelectedIndex),
            childCount: yearCount,
            backgroundColor: Current.BackgroundColor,
            offAxisFraction: (yearFirst ? -0.3 : 0.5) * directionFactor,
            useMagnifier: true,
            magnification: CupertinoDatePicker.Magnification,
            scrollController: _yearController,
            squeeze: CupertinoDatePicker.Squeeze,
            changeReportingBehavior: Current.ChangeReportingBehavior);

        IReadOnlyList<Widget> orderedPickers = yearFirst ? [yearPicker, monthPicker] : [monthPicker, yearPicker];
        var children = new List<Widget>(2);
        for (int index = 0; index < orderedPickers.Count; index++)
        {
            children.Add(new LayoutId(
                index,
                new NotificationListener<ScrollEndNotification>(orderedPickers[index], HandleScrollEnd)));
        }

        double totalWidth = (3.0 * CupertinoDatePicker.DatePickerPadSize)
                            + widths.Sum(width => width + (2.0 * CupertinoDatePicker.DatePickerPadSize));
        Widget layout = new CustomMultiChildLayout(
            new DatePickerLayoutDelegate(
                widths,
                directionFactor,
                Math.Max(totalWidth, CupertinoDatePicker.PickerWidth)),
            children);
        return MediaQuery.WithNoTextScaling(
            context,
            DefaultTextStyle.Merge(layout, new TextStyle(LetterSpacing: -0.83)));
    }

    public override void Dispose()
    {
        _monthController.Dispose();
        _yearController.Dispose();
        base.Dispose();
    }

    private Widget? BuildMonth(
        BuildContext context,
        int index,
        CupertinoLocalizations localizations,
        double width)
    {
        int month = CupertinoDatePickerHelpers.Modulo(index, 12) + 1;
        DateTime candidate = new(_selectedYear, month, 1);
        return CupertinoDatePickerHelpers.WrapPicker(
            context,
            new Text(localizations.DatePickerStandaloneMonth(month)),
            IsValid(candidate),
            maxWidth: width + CupertinoDatePicker.DatePickerPadSize);
    }

    private Widget? BuildYear(
        BuildContext context,
        int index,
        CupertinoLocalizations localizations,
        double width)
    {
        int year = Current.MinimumYear + index;
        if (year < 1 || year > DateTime.MaxValue.Year)
        {
            return null;
        }

        DateTime candidate = new(year, _selectedMonth, 1);
        return CupertinoDatePickerHelpers.WrapPicker(
            context,
            new Text(localizations.DatePickerYear(year)),
            IsValid(candidate),
            maxWidth: width + CupertinoDatePicker.DatePickerPadSize);
    }

    private void SelectMonth(int index)
    {
        int value = CupertinoDatePickerHelpers.Modulo(index, 12) + 1;
        SetState(() => _selectedMonth = value);
        ReportIfValid();
    }

    private void SelectYear(int index)
    {
        int value = Current.MinimumYear + index;
        SetState(() => _selectedYear = value);
        ReportIfValid();
    }

    private bool HandleScrollEnd(ScrollEndNotification notification)
    {
        if (_correctionScheduled)
        {
            return false;
        }

        _correctionScheduled = true;
        global::Plumix.Scheduler.AddPostFrameCallback(_ =>
        {
            _correctionScheduled = false;
            CorrectSelection();
        });
        return false;
    }

    private async void CorrectSelection()
    {
        DateTime candidate = new(_selectedYear, _selectedMonth, 1);
        DateTime corrected = candidate;
        if (Current.MinimumDate is DateTime minimum && candidate < FirstOfMonth(minimum))
        {
            corrected = FirstOfMonth(minimum);
        }

        if (Current.MaximumDate is DateTime maximum && candidate > FirstOfMonth(maximum))
        {
            corrected = FirstOfMonth(maximum);
        }

        if (corrected == candidate)
        {
            return;
        }

        SetState(() =>
        {
            _selectedYear = corrected.Year;
            _selectedMonth = corrected.Month;
        });
        await Task.WhenAll(
            _monthController.AnimateToItem(
                corrected.Month - 1,
                CupertinoDatePicker.ScrollToDateDuration,
                Curves.EaseInOut),
            _yearController.AnimateToItem(
                corrected.Year - Current.MinimumYear,
                CupertinoDatePicker.ScrollToDateDuration,
                Curves.EaseInOut));
    }

    private void ReportIfValid()
    {
        DateTime value = new(_selectedYear, _selectedMonth, 1);
        if (IsValid(value))
        {
            Current.OnDateTimeChanged(value);
        }
    }

    private bool IsValid(DateTime value)
    {
        if (Current.MinimumDate is DateTime minimum && value < FirstOfMonth(minimum))
        {
            return false;
        }

        if (Current.MaximumDate is DateTime maximum && value > FirstOfMonth(maximum))
        {
            return false;
        }

        return true;
    }

    private static DateTime FirstOfMonth(DateTime value) => new(value.Year, value.Month, 1);
}

internal sealed class CupertinoDatePickerDateTimeState : State
{
    private FixedExtentScrollController _dateController = null!;
    private FixedExtentScrollController _hourController = null!;
    private FixedExtentScrollController _minuteController = null!;
    private FixedExtentScrollController? _meridiemController;
    private int _selectedDayFromInitial;
    private int _selectedHour;
    private int _selectedMinute;
    private int _selectedAmPm;
    private bool _correctionScheduled;

    private CupertinoDatePicker Current => (CupertinoDatePicker)StateWidget;

    public override void InitState()
    {
        base.InitState();
        _selectedHour = Current.InitialDateTime.Hour;
        _selectedMinute = Current.InitialDateTime.Minute;
        _selectedAmPm = _selectedHour / 12;
        _dateController = new FixedExtentScrollController();
        _hourController = new FixedExtentScrollController(initialItem: _selectedHour);
        _minuteController = new FixedExtentScrollController(
            initialItem: _selectedMinute / Current.MinuteInterval);
        if (!Current.Use24hFormat)
        {
            _meridiemController = new FixedExtentScrollController(initialItem: _selectedAmPm);
        }
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var oldPicker = (CupertinoDatePicker)oldWidget;
        if (oldPicker.Mode != Current.Mode)
        {
            throw new InvalidOperationException("The CupertinoDatePicker's mode cannot change once it's built.");
        }

        if (oldPicker.Use24hFormat && !Current.Use24hFormat)
        {
            _meridiemController = new FixedExtentScrollController(initialItem: _selectedAmPm);
        }
        else if (!oldPicker.Use24hFormat && Current.Use24hFormat)
        {
            _meridiemController?.Dispose();
            _meridiemController = null;
        }

        base.DidUpdateWidget(oldWidget);
    }

    public override Widget Build(BuildContext context)
    {
        CupertinoLocalizations localizations = CupertinoLocalizations.Of(context);
        TextDirection direction = Directionality.Of(context);
        double directionFactor = direction == TextDirection.Ltr ? 1.0 : -1.0;
        double hourWidth = CupertinoDatePicker.GetColumnWidth(
            Enumerable.Range(0, 24).Select(localizations.DatePickerHour).ToArray(),
            context);
        double minuteWidth = CupertinoDatePicker.GetColumnWidth(
            Enumerable.Range(0, 60).Select(localizations.DatePickerMinute).ToArray(),
            context);
        double meridiemWidth = CupertinoDatePicker.GetColumnWidth(
            [localizations.AnteMeridiemAbbreviation, localizations.PostMeridiemAbbreviation],
            context);
        double dateWidth = CupertinoDatePicker.GetColumnWidth(
            Enumerable.Range(1, 12)
                .Select(month => localizations.DatePickerMediumDate(new DateTime(2018, month, 25)))
                .ToArray(),
            context);
        double separatorWidth = CupertinoDatePicker.GetColumnWidth([":"], context);

        var hourColumn = new TimePickerColumn(hourWidth, _hourController, BuildHourPicker);
        var minuteColumn = new TimePickerColumn(minuteWidth, _minuteController, BuildMinutePicker);
        var columns = direction == TextDirection.Ltr
            ? new List<TimePickerColumn>([hourColumn, minuteColumn])
            : new List<TimePickerColumn>([minuteColumn, hourColumn]);
        if (Current.ShowTimeSeparator)
        {
            columns.Insert(1, new TimePickerColumn(separatorWidth, null, BuildSeparatorPicker));
        }

        if (!Current.Use24hFormat)
        {
            var meridiem = new TimePickerColumn(meridiemWidth, _meridiemController, BuildMeridiemPicker);
            DatePickerDateTimeOrder order = localizations.DatePickerDateTimeOrder;
            if (order is DatePickerDateTimeOrder.DateDayPeriodTime or DatePickerDateTimeOrder.DayPeriodTimeDate)
            {
                columns.Insert(0, meridiem);
            }
            else
            {
                columns.Add(meridiem);
            }
        }

        if (Current.Mode == CupertinoDatePickerMode.DateAndTime)
        {
            var date = new TimePickerColumn(dateWidth, _dateController, BuildDatePicker);
            DatePickerDateTimeOrder order = localizations.DatePickerDateTimeOrder;
            if (order is DatePickerDateTimeOrder.DateTimeDayPeriod or DatePickerDateTimeOrder.DateDayPeriodTime)
            {
                columns.Insert(0, date);
            }
            else
            {
                columns.Add(date);
            }
        }

        IReadOnlyList<double> widths = columns.Select(column => column.Width).ToArray();
        double totalWidth = 4.0 * CupertinoDatePicker.DatePickerPadSize;
        foreach (double width in widths)
        {
            totalWidth += width + (2.0 * CupertinoDatePicker.DatePickerPadSize);
        }

        double maxWidth = Math.Max(totalWidth, CupertinoDatePicker.PickerWidth);
        var children = new List<Widget>(columns.Count);
        for (int index = 0; index < columns.Count; index++)
        {
            int physicalIndex = index;
            double offAxisFraction = index == 0
                ? -0.45 * directionFactor
                : index >= 2 || columns.Count == 2
                    ? 0.45 * directionFactor
                    : 0.0;
            Widget child = columns[index].Builder(
                context,
                localizations,
                columns.Count,
                physicalIndex,
                offAxisFraction,
                columns[index].Width);
            children.Add(new LayoutId(
                physicalIndex,
                new NotificationListener<ScrollEndNotification>(child, HandleScrollEnd)));
        }

        Widget layout = new CustomMultiChildLayout(
            new DatePickerLayoutDelegate(widths, directionFactor, maxWidth),
            children);
        return MediaQuery.WithNoTextScaling(
            context,
            DefaultTextStyle.Merge(layout, new TextStyle(LetterSpacing: -0.83)));
    }

    public override void Dispose()
    {
        _dateController.Dispose();
        _hourController.Dispose();
        _minuteController.Dispose();
        _meridiemController?.Dispose();
        base.Dispose();
    }

    private Widget BuildHourPicker(
        BuildContext context,
        CupertinoLocalizations localizations,
        int columnCount,
        int selectedIndex,
        double offAxisFraction,
        double width)
    {
        return CupertinoPicker.Builder(
            itemExtent: Current.ItemExtent,
            onSelectedItemChanged: SelectHour,
            itemBuilder: (itemContext, index) =>
            {
                int hour = CupertinoDatePickerHelpers.Modulo(index, 24);
                int displayHour = Current.Use24hFormat ? hour : ((hour + 11) % 12) + 1;
                DateTime candidate = CreateCandidate(hour: hour);
                string semantics = localizations.DatePickerHourSemanticsLabel(displayHour) ?? string.Empty;
                return CupertinoDatePickerHelpers.WrapPicker(
                    itemContext,
                    new Text(localizations.DatePickerHour(displayHour)),
                    IsValid(candidate),
                    semantics,
                    maxWidth: width + CupertinoDatePicker.DatePickerPadSize);
            },
            selectionOverlay: CupertinoDatePickerHelpers.Overlay(
                context,
                Current.SelectionOverlayBuilder,
                columnCount,
                selectedIndex),
            backgroundColor: Current.BackgroundColor,
            offAxisFraction: offAxisFraction,
            useMagnifier: true,
            magnification: CupertinoDatePicker.Magnification,
            scrollController: _hourController,
            squeeze: CupertinoDatePicker.Squeeze,
            changeReportingBehavior: Current.ChangeReportingBehavior);
    }

    private Widget BuildMinutePicker(
        BuildContext context,
        CupertinoLocalizations localizations,
        int columnCount,
        int selectedIndex,
        double offAxisFraction,
        double width)
    {
        int childCount = 60 / Current.MinuteInterval;
        return CupertinoPicker.Builder(
            itemExtent: Current.ItemExtent,
            onSelectedItemChanged: SelectMinute,
            itemBuilder: (itemContext, index) =>
            {
                int minute = CupertinoDatePickerHelpers.Modulo(index, childCount) * Current.MinuteInterval;
                DateTime candidate = CreateCandidate(minute: minute);
                string semantics = localizations.DatePickerMinuteSemanticsLabel(minute) ?? string.Empty;
                return CupertinoDatePickerHelpers.WrapPicker(
                    itemContext,
                    new Text(localizations.DatePickerMinute(minute)),
                    IsValid(candidate),
                    semantics,
                    maxWidth: width + CupertinoDatePicker.DatePickerPadSize);
            },
            selectionOverlay: CupertinoDatePickerHelpers.Overlay(
                context,
                Current.SelectionOverlayBuilder,
                columnCount,
                selectedIndex),
            backgroundColor: Current.BackgroundColor,
            offAxisFraction: offAxisFraction,
            useMagnifier: true,
            magnification: CupertinoDatePicker.Magnification,
            scrollController: _minuteController,
            squeeze: CupertinoDatePicker.Squeeze,
            changeReportingBehavior: Current.ChangeReportingBehavior);
    }

    private Widget BuildMeridiemPicker(
        BuildContext context,
        CupertinoLocalizations localizations,
        int columnCount,
        int selectedIndex,
        double offAxisFraction,
        double width)
    {
        return new CupertinoPicker(
            itemExtent: Current.ItemExtent,
            onSelectedItemChanged: SelectMeridiem,
            children:
            [
                CupertinoDatePickerHelpers.WrapPicker(
                    context,
                    new Text(localizations.AnteMeridiemAbbreviation),
                    IsValid(CreateCandidate(hour: _selectedHour % 12)),
                    maxWidth: width + CupertinoDatePicker.DatePickerPadSize),
                CupertinoDatePickerHelpers.WrapPicker(
                    context,
                    new Text(localizations.PostMeridiemAbbreviation),
                    IsValid(CreateCandidate(hour: (_selectedHour % 12) + 12)),
                    maxWidth: width + CupertinoDatePicker.DatePickerPadSize),
            ],
            selectionOverlay: CupertinoDatePickerHelpers.Overlay(
                context,
                Current.SelectionOverlayBuilder,
                columnCount,
                selectedIndex),
            backgroundColor: Current.BackgroundColor,
            offAxisFraction: offAxisFraction,
            useMagnifier: true,
            magnification: CupertinoDatePicker.Magnification,
            scrollController: _meridiemController,
            squeeze: CupertinoDatePicker.Squeeze,
            changeReportingBehavior: Current.ChangeReportingBehavior);
    }

    private Widget BuildDatePicker(
        BuildContext context,
        CupertinoLocalizations localizations,
        int columnCount,
        int selectedIndex,
        double offAxisFraction,
        double width)
    {
        return CupertinoPicker.Builder(
            itemExtent: Current.ItemExtent,
            onSelectedItemChanged: SelectDate,
            itemBuilder: (itemContext, index) =>
            {
                DateTime candidate = Current.InitialDateTime.Date.AddDays(index)
                    .AddHours(_selectedHour)
                    .AddMinutes(_selectedMinute);
                return CupertinoDatePickerHelpers.WrapPicker(
                    itemContext,
                    new Text(localizations.DatePickerMediumDate(candidate)),
                    IsValid(candidate),
                    maxWidth: width + CupertinoDatePicker.DatePickerPadSize);
            },
            selectionOverlay: CupertinoDatePickerHelpers.Overlay(
                context,
                Current.SelectionOverlayBuilder,
                columnCount,
                selectedIndex),
            backgroundColor: Current.BackgroundColor,
            offAxisFraction: offAxisFraction,
            useMagnifier: true,
            magnification: CupertinoDatePicker.Magnification,
            scrollController: _dateController,
            squeeze: CupertinoDatePicker.Squeeze,
            changeReportingBehavior: Current.ChangeReportingBehavior);
    }

    private Widget BuildSeparatorPicker(
        BuildContext context,
        CupertinoLocalizations localizations,
        int columnCount,
        int selectedIndex,
        double offAxisFraction,
        double width)
    {
        return new ExcludeSemantics(
            child: new CupertinoPicker(
                itemExtent: Current.ItemExtent,
                onSelectedItemChanged: null,
                children: [new Text(":")],
                selectionOverlay: CupertinoDatePickerHelpers.Overlay(
                    context,
                    Current.SelectionOverlayBuilder,
                    columnCount,
                    selectedIndex),
                backgroundColor: Current.BackgroundColor,
                offAxisFraction: offAxisFraction,
                useMagnifier: true,
                magnification: CupertinoDatePicker.Magnification,
                squeeze: CupertinoDatePicker.Squeeze));
    }

    private void SelectHour(int index)
    {
        int hour = CupertinoDatePickerHelpers.Modulo(index, 24);
        int oldAmPm = _selectedAmPm;
        SetState(() =>
        {
            _selectedHour = hour;
            _selectedAmPm = hour / 12;
        });
        if (!Current.Use24hFormat && oldAmPm != _selectedAmPm && _meridiemController is not null)
        {
            _ = _meridiemController.AnimateToItem(
                _selectedAmPm,
                TimeSpan.FromMilliseconds(300.0),
                Curves.EaseOut);
        }

        ReportIfValid();
    }

    private void SelectMinute(int index)
    {
        int childCount = 60 / Current.MinuteInterval;
        int minute = CupertinoDatePickerHelpers.Modulo(index, childCount) * Current.MinuteInterval;
        SetState(() => _selectedMinute = minute);
        ReportIfValid();
    }

    private void SelectMeridiem(int index)
    {
        int amPm = CupertinoDatePickerHelpers.Modulo(index, 2);
        SetState(() =>
        {
            _selectedAmPm = amPm;
            _selectedHour = (_selectedHour % 12) + (amPm * 12);
        });
        ReportIfValid();
    }

    private void SelectDate(int index)
    {
        SetState(() => _selectedDayFromInitial = index);
        ReportIfValid();
    }

    private bool HandleScrollEnd(ScrollEndNotification notification)
    {
        if (_correctionScheduled)
        {
            return false;
        }

        _correctionScheduled = true;
        global::Plumix.Scheduler.AddPostFrameCallback(_ =>
        {
            _correctionScheduled = false;
            CorrectSelection();
        });
        return false;
    }

    private async void CorrectSelection()
    {
        DateTime candidate = CreateCandidate();
        DateTime corrected = candidate;
        if (Current.MinimumDate is DateTime minimum && corrected < minimum)
        {
            int minute = (int)Math.Ceiling(minimum.Minute / (double)Current.MinuteInterval)
                         * Current.MinuteInterval;
            corrected = new DateTime(
                minimum.Year,
                minimum.Month,
                minimum.Day,
                minimum.Hour,
                minute == 60 ? 0 : minute,
                0).AddHours(minute == 60 ? 1.0 : 0.0);
        }

        if (Current.MaximumDate is DateTime maximum && corrected > maximum)
        {
            int minute = (maximum.Minute / Current.MinuteInterval) * Current.MinuteInterval;
            corrected = new DateTime(maximum.Year, maximum.Month, maximum.Day, maximum.Hour, minute, 0);
        }

        if (Current.SelectableDayPredicate is not null && !Current.SelectableDayPredicate(corrected))
        {
            DateTime next = corrected.AddDays(1.0);
            if (IsValid(next))
            {
                corrected = next;
            }
            else
            {
                return;
            }
        }

        if (candidate == corrected)
        {
            return;
        }

        int dayOffset = (corrected.Date - Current.InitialDateTime.Date).Days;
        int minuteItem = corrected.Minute / Current.MinuteInterval;
        SetState(() =>
        {
            _selectedDayFromInitial = dayOffset;
            _selectedHour = corrected.Hour;
            _selectedMinute = corrected.Minute;
            _selectedAmPm = corrected.Hour / 12;
        });
        var animations = new List<Task>
        {
            _hourController.AnimateToItem(
                corrected.Hour,
                CupertinoDatePicker.ScrollToDateDuration,
                Curves.EaseInOut),
            _minuteController.AnimateToItem(
                minuteItem,
                CupertinoDatePicker.ScrollToDateDuration,
                Curves.EaseInOut),
        };
        if (Current.Mode == CupertinoDatePickerMode.DateAndTime)
        {
            animations.Add(_dateController.AnimateToItem(
                dayOffset,
                CupertinoDatePicker.ScrollToDateDuration,
                Curves.EaseInOut));
        }

        if (_meridiemController is not null)
        {
            animations.Add(_meridiemController.AnimateToItem(
                _selectedAmPm,
                CupertinoDatePicker.ScrollToDateDuration,
                Curves.EaseInOut));
        }

        await Task.WhenAll(animations);
    }

    private void ReportIfValid()
    {
        DateTime value = CreateCandidate();
        if (IsValid(value))
        {
            Current.OnDateTimeChanged(value);
        }
    }

    private DateTime CreateCandidate(int? hour = null, int? minute = null)
    {
        DateTime date = Current.InitialDateTime.Date.AddDays(_selectedDayFromInitial);
        return date.AddHours(hour ?? _selectedHour).AddMinutes(minute ?? _selectedMinute);
    }

    private bool IsValid(DateTime value)
    {
        if (Current.MinimumDate is DateTime minimum && value < minimum)
        {
            return false;
        }

        if (Current.MaximumDate is DateTime maximum && value > maximum)
        {
            return false;
        }

        return Current.SelectableDayPredicate?.Invoke(value) ?? true;
    }

    private sealed record TimePickerColumn(
        double Width,
        FixedExtentScrollController? Controller,
        Func<BuildContext, CupertinoLocalizations, int, int, double, double, Widget> Builder);
}

internal sealed class CupertinoTimerPickerState : State
{
    private const double TimerPickerMagnification = 34.0 / 32.0;
    private const double TimerPickerMinHorizontalPadding = 30.0;
    private const double TimerPickerHalfColumnPadding = 4.0;
    private const double TimerPickerLabelPadSize = 6.0;
    private const double TimerPickerLabelFontSize = 17.0;
    private const double TimerPickerColumnIntrinsicWidth = 106.0;

    private FixedExtentScrollController? _hourController;
    private FixedExtentScrollController? _minuteController;
    private FixedExtentScrollController? _secondController;
    private int _selectedHour;
    private int _selectedMinute;
    private int _selectedSecond;
    private int _lastSelectedHour;
    private int _lastSelectedMinute;
    private int _lastSelectedSecond;

    private CupertinoTimerPicker Current => (CupertinoTimerPicker)StateWidget;

    public override void InitState()
    {
        base.InitState();
        _selectedHour = (int)Current.InitialTimerDuration.TotalHours;
        _selectedMinute = Current.InitialTimerDuration.Minutes;
        _selectedSecond = Current.InitialTimerDuration.Seconds;
        _lastSelectedHour = _selectedHour;
        _lastSelectedMinute = _selectedMinute;
        _lastSelectedSecond = _selectedSecond;
        if (Current.Mode != CupertinoTimerPickerMode.Ms)
        {
            _hourController = new FixedExtentScrollController(initialItem: _selectedHour);
        }

        _minuteController = new FixedExtentScrollController(
            initialItem: _selectedMinute / Current.MinuteInterval);
        if (Current.Mode != CupertinoTimerPickerMode.Hm)
        {
            _secondController = new FixedExtentScrollController(
                initialItem: _selectedSecond / Current.SecondInterval);
        }
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        if (((CupertinoTimerPicker)oldWidget).Mode != Current.Mode)
        {
            throw new InvalidOperationException("The CupertinoTimerPicker's mode cannot change once it's built");
        }

        base.DidUpdateWidget(oldWidget);
    }

    public override Widget Build(BuildContext context)
    {
        return new LayoutBuilder((layoutContext, constraints) => BuildContents(layoutContext, constraints));
    }

    public override void Dispose()
    {
        _hourController?.Dispose();
        _minuteController?.Dispose();
        _secondController?.Dispose();
        base.Dispose();
    }

    private Widget BuildContents(BuildContext context, BoxConstraints constraints)
    {
        if (constraints.MaxWidth <= 0.0 || constraints.MaxHeight <= 0.0)
        {
            return new SizedBox(width: 0.0, height: 0.0);
        }

        CupertinoLocalizations localizations = CupertinoLocalizations.Of(context);
        TextDirection direction = Directionality.Of(context);
        TextStyle baseStyle = CupertinoTheme.Of(context).TextTheme.PickerTextStyle;
        TextStyle pickerStyle = baseStyle.CopyWith(
            color: baseStyle.Color,
            fontSize: (baseStyle.FontSize ?? 21.0) * TimerPickerMagnification);
        CupertinoThemeData theme = CupertinoTheme.Of(context);
        CupertinoTextThemeData textTheme = theme.TextTheme.CopyWith(pickerTextStyle: pickerStyle);
        CupertinoThemeData pickerTheme = theme.CopyWith(textTheme: textTheme);

        IReadOnlyList<TimerColumnKind> kinds = Current.Mode switch
        {
            CupertinoTimerPickerMode.Hm => [TimerColumnKind.Hour, TimerColumnKind.Minute],
            CupertinoTimerPickerMode.Ms => [TimerColumnKind.Minute, TimerColumnKind.Second],
            _ => [TimerColumnKind.Hour, TimerColumnKind.Minute, TimerColumnKind.Second],
        };
        int columnCount = kinds.Count;
        double intrinsicTotalWidth = columnCount == 3
            ? (TimerPickerColumnIntrinsicWidth + (TimerPickerHalfColumnPadding * 2.0)) * 3.0
            : CupertinoDatePicker.PickerWidth;
        double totalWidth = Math.Min(intrinsicTotalWidth, constraints.MaxWidth);
        double pickerColumnWidth = totalWidth / columnCount;
        var columns = new List<Widget>(columnCount);
        for (int index = 0; index < columnCount; index++)
        {
            columns.Add(new Expanded(BuildColumn(
                context,
                localizations,
                kinds[index],
                columnCount,
                index,
                pickerColumnWidth,
                totalWidth,
                direction)));
        }

        Widget contents = new SizedBox(
            width: totalWidth,
            height: CupertinoDatePicker.PickerHeight,
            child: new DefaultTextStyle(
                baseStyle,
                new Row(children: columns)));
        if (Current.BackgroundColor is not null)
        {
            contents = new ColoredBox(
                CupertinoDynamicColor.Resolve(Current.BackgroundColor, context),
                contents);
        }

        contents = new Align(alignment: Current.Alignment, child: contents);
        return MediaQuery.WithNoTextScaling(
            context,
            new CupertinoTheme(pickerTheme, contents));
    }

    private Widget BuildColumn(
        BuildContext context,
        CupertinoLocalizations localizations,
        TimerColumnKind kind,
        int columnCount,
        int selectedIndex,
        double columnWidth,
        double totalWidth,
        TextDirection direction)
    {
        FixedExtentScrollController controller = kind switch
        {
            TimerColumnKind.Hour => _hourController!,
            TimerColumnKind.Minute => _minuteController!,
            _ => _secondController!,
        };
        int interval = kind switch
        {
            TimerColumnKind.Minute => Current.MinuteInterval,
            TimerColumnKind.Second => Current.SecondInterval,
            _ => 1,
        };
        int? childCount = kind == TimerColumnKind.Hour ? 24 : null;
        Action<int> callback = kind switch
        {
            TimerColumnKind.Hour => SelectHour,
            TimerColumnKind.Minute => SelectMinute,
            _ => SelectSecond,
        };
        double numberWidth = CupertinoDatePicker.GetColumnWidth(
            Enumerable.Range(0, kind == TimerColumnKind.Hour ? 24 : 60)
                .Where(value => value % interval == 0)
                .Select(value => FormatNumber(localizations, kind, value))
                .ToArray(),
            context,
            CupertinoTheme.Of(context).TextTheme.PickerTextStyle);
        string label = FormatLabel(localizations, kind, LastSelected(kind));
        double labelWidth = CupertinoDatePicker.GetColumnWidth(
            AllLabels(localizations, kind),
            context,
            new TextStyle(FontSize: TimerPickerLabelFontSize, FontWeight: FontWeight.SemiBold));
        double numberHeight;
        double numberBaseline;
        using (var painter = new TextPainter(textDirection: direction))
        {
            painter.Text = new TextSpan(
                text: FormatNumber(localizations, kind, kind == TimerColumnKind.Hour ? 22 : 55),
                style: CupertinoTheme.Of(context).TextTheme.PickerTextStyle);
            painter.Layout();
            numberHeight = painter.Height;
            numberBaseline = painter.ComputeDistanceToActualBaseline(TextBaseline.Alphabetic);
        }

        double contentWidth = numberWidth + TimerPickerLabelPadSize + labelWidth;
        double startPadding = Math.Max(
            TimerPickerMinHorizontalPadding,
            (columnWidth - contentWidth) / 2.0);
        double centerPoint = startPadding + (numberWidth / 2.0);
        double pickerColumnOffAxisFraction = 0.5 - (centerPoint / columnWidth);
        double timerPickerOffAxisFraction = 0.5 - ((centerPoint + (columnWidth * selectedIndex)) / totalWidth);
        double directionFactor = direction == TextDirection.Ltr ? 1.0 : -1.0;
        double offAxisFraction = (pickerColumnOffAxisFraction - timerPickerOffAxisFraction) * directionFactor;

        Widget picker = CupertinoPicker.Builder(
            itemExtent: Current.ItemExtent,
            onSelectedItemChanged: callback,
            itemBuilder: (itemContext, index) =>
            {
                int value = kind == TimerColumnKind.Hour
                    ? index
                    : CupertinoDatePickerHelpers.Modulo(index, 60 / interval) * interval;
                string number = FormatNumber(localizations, kind, value);
                string itemLabel = FormatLabel(localizations, kind, value);
                string semanticsLabel = direction == TextDirection.Ltr
                    ? $"{number} {itemLabel}"
                    : $"{itemLabel} {number}";
                return new Semantics(
                    label: semanticsLabel,
                    child: new SizedBox(
                        width: numberWidth,
                        child: new Align(
                            alignment: AlignmentDirectional.CenterStart,
                            child: new Text(number, textAlign: TextAlign.End))));
            },
            selectionOverlay: CupertinoDatePickerHelpers.Overlay(
                context,
                Current.SelectionOverlayBuilder,
                columnCount,
                selectedIndex),
            childCount: childCount,
            backgroundColor: Current.BackgroundColor,
            offAxisFraction: offAxisFraction,
            useMagnifier: true,
            magnification: TimerPickerMagnification,
            scrollController: controller,
            squeeze: CupertinoDatePicker.Squeeze,
            changeReportingBehavior: Current.ChangeReportingBehavior);
        Widget fixedLabel = new IgnorePointer(
            child: new Padding(
                EdgeInsetsGeometry.DirectionalOnly(start: startPadding + numberWidth + TimerPickerLabelPadSize),
                new Align(
                    alignment: AlignmentDirectional.CenterStart,
                    child: new SizedBox(
                        height: numberHeight,
                        child: new Baseline(
                            baseline: numberBaseline,
                            baselineType: TextBaseline.Alphabetic,
                            child: new Text(
                                label,
                                style: new TextStyle(
                                    FontSize: TimerPickerLabelFontSize,
                                    FontWeight: FontWeight.SemiBold)))))));
        return new Stack(
            children:
            [
                new NotificationListener<ScrollEndNotification>(
                    picker,
                    notification => HandleScrollEnd(kind, notification)),
                fixedLabel,
            ]);
    }

    private void SelectHour(int index)
    {
        SetState(() => _selectedHour = index);
        ReportDuration();
    }

    private void SelectMinute(int index)
    {
        int count = 60 / Current.MinuteInterval;
        int minute = CupertinoDatePickerHelpers.Modulo(index, count) * Current.MinuteInterval;
        SetState(() => _selectedMinute = minute);
        ReportDuration();
    }

    private void SelectSecond(int index)
    {
        int count = 60 / Current.SecondInterval;
        int second = CupertinoDatePickerHelpers.Modulo(index, count) * Current.SecondInterval;
        SetState(() => _selectedSecond = second);
        ReportDuration();
    }

    private bool HandleScrollEnd(TimerColumnKind kind, ScrollEndNotification notification)
    {
        SetState(() =>
        {
            switch (kind)
            {
                case TimerColumnKind.Hour:
                    _lastSelectedHour = _selectedHour;
                    break;
                case TimerColumnKind.Minute:
                    _lastSelectedMinute = _selectedMinute;
                    break;
                case TimerColumnKind.Second:
                    _lastSelectedSecond = _selectedSecond;
                    break;
            }
        });
        return false;
    }

    private void ReportDuration()
    {
        Current.OnTimerDurationChanged(new TimeSpan(_selectedHour, _selectedMinute, _selectedSecond));
    }

    private int LastSelected(TimerColumnKind kind)
    {
        return kind switch
        {
            TimerColumnKind.Hour => _lastSelectedHour,
            TimerColumnKind.Minute => _lastSelectedMinute,
            _ => _lastSelectedSecond,
        };
    }

    private static string FormatNumber(
        CupertinoLocalizations localizations,
        TimerColumnKind kind,
        int value)
    {
        return kind switch
        {
            TimerColumnKind.Hour => localizations.TimerPickerHour(value),
            TimerColumnKind.Minute => localizations.TimerPickerMinute(value),
            _ => localizations.TimerPickerSecond(value),
        };
    }

    private static string FormatLabel(
        CupertinoLocalizations localizations,
        TimerColumnKind kind,
        int value)
    {
        return kind switch
        {
            TimerColumnKind.Hour => localizations.TimerPickerHourLabel(value) ?? string.Empty,
            TimerColumnKind.Minute => localizations.TimerPickerMinuteLabel(value) ?? string.Empty,
            _ => localizations.TimerPickerSecondLabel(value) ?? string.Empty,
        };
    }

    private static IReadOnlyList<string> AllLabels(
        CupertinoLocalizations localizations,
        TimerColumnKind kind)
    {
        return kind switch
        {
            TimerColumnKind.Hour => localizations.TimerPickerHourLabels,
            TimerColumnKind.Minute => localizations.TimerPickerMinuteLabels,
            _ => localizations.TimerPickerSecondLabels,
        };
    }

    private enum TimerColumnKind
    {
        Hour,
        Minute,
        Second,
    }
}
