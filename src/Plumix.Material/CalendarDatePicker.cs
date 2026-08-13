using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/calendar_date_picker.dart

public sealed class CalendarDatePicker : StatefulWidget
{
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
        InitialDate = initialDate.HasValue ? CalendarDelegate.DateOnly(initialDate.Value) : null;
        FirstDate = CalendarDelegate.DateOnly(firstDate);
        LastDate = CalendarDelegate.DateOnly(lastDate);
        CurrentDate = CalendarDelegate.DateOnly(currentDate ?? CalendarDelegate.Now());
        OnDateChanged = onDateChanged ?? throw new ArgumentNullException(nameof(onDateChanged));
        OnDisplayedMonthChanged = onDisplayedMonthChanged;
        InitialCalendarMode = initialCalendarMode;
        SelectableDayPredicate = selectableDayPredicate;
        ValidateDates(InitialDate, FirstDate, LastDate, selectableDayPredicate);
    }

    public DateTime? InitialDate { get; }
    public DateTime FirstDate { get; }
    public DateTime LastDate { get; }
    public DateTime CurrentDate { get; }
    public Action<DateTime> OnDateChanged { get; }
    public Action<DateTime>? OnDisplayedMonthChanged { get; }
    public DatePickerMode InitialCalendarMode { get; }
    public SelectableDayPredicate? SelectableDayPredicate { get; }
    public CalendarDelegate<DateTime> CalendarDelegate { get; }

    public override State CreateState() => new CalendarDatePickerState();

    internal static void ValidateDates(
        DateTime? initialDate,
        DateTime firstDate,
        DateTime lastDate,
        SelectableDayPredicate? predicate)
    {
        if (lastDate < firstDate) throw new ArgumentException("lastDate must be on or after firstDate.", nameof(lastDate));
        if (initialDate < firstDate) throw new ArgumentOutOfRangeException(nameof(initialDate), "initialDate must be on or after firstDate.");
        if (initialDate > lastDate) throw new ArgumentOutOfRangeException(nameof(initialDate), "initialDate must be on or before lastDate.");
        if (initialDate.HasValue && predicate is not null && !predicate(initialDate.Value))
        {
            throw new ArgumentException("initialDate must satisfy selectableDayPredicate.", nameof(initialDate));
        }
    }

    private sealed class CalendarDatePickerState : State
    {
        private const double SubHeaderHeight = 52;
        private DatePickerMode _mode;
        private DateTime _displayedMonth;
        private DateTime? _selectedDate;
        private DateTime? _focusedDate;
        private bool _announcedInitialDate;
        private string _announcementText = string.Empty;
        private AnimationController? _modeController;
        private PageController? _pageController;
        private FocusNode? _gridFocus;

        private CalendarDatePicker CurrentWidget => (CalendarDatePicker)StateWidget;

        public override void InitState()
        {
            var widget = CurrentWidget;
            _mode = widget.InitialCalendarMode;
            _selectedDate = widget.InitialDate;
            _focusedDate = widget.InitialDate ?? widget.CurrentDate;
            var source = widget.InitialDate ?? widget.CurrentDate;
            _displayedMonth = widget.CalendarDelegate.GetMonth(source.Year, source.Month);
            _modeController = new AnimationController(TimeSpan.FromMilliseconds(200), this)
            {
                Curve = Curves.EaseIn,
            };
            _modeController.SetValue(_mode == DatePickerMode.Year ? 1 : 0);
            _modeController.Changed += HandleModeAnimationChanged;
            _pageController = new PageController(initialPage: 1);
            _gridFocus = new FocusNode();
            _gridFocus.AddListener(HandleGridFocusChanged);
        }

        public override void DidChangeDependencies()
        {
            if (_announcedInitialDate || CurrentWidget.InitialDate is not { } initialDate) return;
            _announcedInitialDate = true;
            var localizations = MaterialLocalizations.Of(Context);
            string suffix = CurrentWidget.CalendarDelegate.IsSameDay(CurrentWidget.CurrentDate, initialDate)
                ? $", {localizations.CurrentDateLabel}"
                : string.Empty;
            _announcementText = $"{CurrentWidget.CalendarDelegate.FormatFullDate(initialDate, localizations)}{suffix}";
        }

        public override void Dispose()
        {
            if (_modeController is not null)
            {
                _modeController.Changed -= HandleModeAnimationChanged;
                _modeController.Dispose();
                _modeController = null;
            }
            _pageController?.Dispose();
            _gridFocus?.RemoveListener(HandleGridFocusChanged);
            _gridFocus?.Dispose();
            _pageController = null;
            _gridFocus = null;
        }

        public override Widget Build(BuildContext context)
        {
            var theme = DatePickerTheme.Of(context);
            var defaults = DatePickerTheme.Defaults(context);
            var titleStyle = theme.ToggleButtonTextStyle ?? defaults.ToggleButtonTextStyle!;
            var subHeaderColor = theme.SubHeaderForegroundColor ?? defaults.SubHeaderForegroundColor;
            var localizations = MaterialLocalizations.Of(context);
            bool useMaterial3 = Theme.Of(context).UseMaterial3;
            var media = MediaQuery.MaybeOf(context) ?? new MediaQueryData(Size: new Size(360, 640));
            bool portrait = media.Size.Height >= media.Size.Width;
            double rowHeight = useMaterial3 && portrait ? 48.0 : 42.0;
            double textScale = Math.Clamp(media.TextScaleFactor, 0, 3);
            double pickerHeight = SubHeaderHeight + (rowHeight * 7) + (textScale > 1.3 ? 7 * ((textScale - 1) * 8) : 0);

            Widget picker = _mode == DatePickerMode.Day
                ? BuildMonthPicker(context, rowHeight)
                : new Padding(new Thickness(0, SubHeaderHeight, 0, 0), BuildYearPicker());

            Widget title = new Semantics(
                label: localizations.SelectYearSemanticsLabel,
                flags: SemanticsFlags.IsButton | SemanticsFlags.IsEnabled,
                onTap: ToggleMode,
                container: true,
                child: new InkWell(
                    onTap: ToggleMode,
                    child: new Padding(
                        new Thickness(8, 0),
                        new Row(
                            mainAxisSize: MainAxisSize.Min,
                            children:
                            [
                                new Flexible(
                                    child: new DefaultTextStyle(
                                        titleStyle.CopyWith(color: titleStyle.Color ?? subHeaderColor),
                                        new Text(
                                            CurrentWidget.CalendarDelegate.FormatMonthYear(_displayedMonth, localizations),
                                            softWrap: false,
                                            maxLines: 1,
                                            overflow: TextOverflow.Ellipsis))),
                                BuildModeArrow(subHeaderColor),
                            ]))));

            var headerChildren = new List<Widget> { new Expanded(child: title) };
            if (_mode == DatePickerMode.Day)
            {
                headerChildren.Add(BuildMonthNavigation(subHeaderColor));
            }

            Widget header = new SizedBox(
                height: SubHeaderHeight,
                child: new Padding(
                    new Thickness(16, 0, 4, 0),
                    new Row(children: headerChildren)));

            return new SizedBox(
                height: pickerHeight,
                child: new Stack(
                    children:
                    [
                        new Semantics(
                            label: string.IsNullOrEmpty(_announcementText) ? null : _announcementText,
                            container: true,
                            explicitChildNodes: true,
                            liveRegion: true,
                            child: picker),
                        header,
                    ]));
        }

        private Widget BuildMonthPicker(BuildContext context, double rowHeight)
        {
            var widget = CurrentWidget;
            var previous = widget.CalendarDelegate.AddMonthsToMonthDate(_displayedMonth, -1);
            var next = widget.CalendarDelegate.AddMonthsToMonthDate(_displayedMonth, 1);
            var children = new Widget[]
            {
                BuildDayPicker(previous, rowHeight),
                BuildDayPicker(_displayedMonth, rowHeight),
                BuildDayPicker(next, rowHeight),
            };

            Widget pages = new PageView(
                controller: _pageController,
                onPageChanged: HandleMonthPageChanged,
                children: children);
            return new Padding(
                new Thickness(0, SubHeaderHeight, 0, 0),
                new Focus(
                    focusNode: _gridFocus,
                    onKeyEvent: HandleGridKey,
                    child: pages));
        }

        private Widget BuildDayPicker(DateTime month, double rowHeight) => new CalendarDayPicker(
            displayedMonth: month,
            selectedDate: _selectedDate,
            currentDate: CurrentWidget.CurrentDate,
            firstDate: CurrentWidget.FirstDate,
            lastDate: CurrentWidget.LastDate,
            selectableDayPredicate: CurrentWidget.SelectableDayPredicate,
            calendarDelegate: CurrentWidget.CalendarDelegate,
            focusedDate: _gridFocus?.HasFocus == true ? _focusedDate : null,
            rowHeight: rowHeight,
            onChanged: HandleDayChanged,
            key: new ValueKey<DateTime>(month));

        private Widget BuildYearPicker() => new YearPicker(
            currentDate: CurrentWidget.CurrentDate,
            firstDate: CurrentWidget.FirstDate,
            lastDate: CurrentWidget.LastDate,
            selectedDate: _displayedMonth,
            onChanged: HandleYearChanged,
            calendarDelegate: CurrentWidget.CalendarDelegate);

        private Widget BuildMonthNavigation(Color? color)
        {
            var localizations = MaterialLocalizations.Of(Context);
            bool previousEnabled = !IsFirstMonth;
            bool nextEnabled = !IsLastMonth;
            return new SizedBox(
                width: 108,
                child: new Row(
                    mainAxisSize: MainAxisSize.Min,
                    children:
                    [
                        new Semantics(
                            label: localizations.PreviousMonthTooltip,
                            flags: SemanticsFlags.IsButton | (previousEnabled ? SemanticsFlags.IsEnabled : SemanticsFlags.None),
                            onTap: previousEnabled ? PreviousMonth : null,
                            child: new Tooltip(
                                message: previousEnabled ? localizations.PreviousMonthTooltip : string.Empty,
                                child: new IconButton(
                                    icon: new Icon(Icons.ChevronLeft),
                                    color: color,
                                    onPressed: previousEnabled ? PreviousMonth : null))),
                        new Semantics(
                            label: localizations.NextMonthTooltip,
                            flags: SemanticsFlags.IsButton | (nextEnabled ? SemanticsFlags.IsEnabled : SemanticsFlags.None),
                            onTap: nextEnabled ? NextMonth : null,
                            child: new Tooltip(
                                message: nextEnabled ? localizations.NextMonthTooltip : string.Empty,
                                child: new IconButton(
                                    icon: new Icon(Icons.ChevronRight),
                                    color: color,
                                    onPressed: nextEnabled ? NextMonth : null))),
                    ]));
        }

        private Widget BuildModeArrow(Color? color)
        {
            const double size = 24;
            double center = size / 2;
            double angle = Math.PI * (_modeController?.Value ?? 0);
            var rotation = new Matrix(Math.Cos(angle), Math.Sin(angle), -Math.Sin(angle), Math.Cos(angle), 0, 0);
            return new Plumix.Widgets.Transform(
                transform: Matrix.CreateTranslation(center, center)
                           * rotation
                           * Matrix.CreateTranslation(-center, -center),
                child: new Icon(Icons.ArrowDropDown, size: size, color: color));
        }

        private bool IsFirstMonth => !(_displayedMonth > CurrentWidget.CalendarDelegate.GetMonth(
            CurrentWidget.FirstDate.Year, CurrentWidget.FirstDate.Month));

        private bool IsLastMonth => !(_displayedMonth < CurrentWidget.CalendarDelegate.GetMonth(
            CurrentWidget.LastDate.Year, CurrentWidget.LastDate.Month));

        private void ToggleMode()
        {
            Feedback.ForTap();
            SetState(() =>
            {
                _mode = _mode == DatePickerMode.Day ? DatePickerMode.Year : DatePickerMode.Day;
                if (_selectedDate is not { } selected) return;
                var localizations = MaterialLocalizations.Of(Context);
                _announcementText = _mode == DatePickerMode.Day
                    ? CurrentWidget.CalendarDelegate.FormatMonthYear(selected, localizations)
                    : CurrentWidget.CalendarDelegate.FormatYear(selected.Year, localizations);
            });
            if (_mode == DatePickerMode.Year) _modeController?.Forward();
            else _modeController?.Reverse();
        }

        private void PreviousMonth()
        {
            if (!IsFirstMonth) _pageController?.AnimateToPage(0, TimeSpan.FromMilliseconds(200), Curves.Ease);
        }

        private void NextMonth()
        {
            if (!IsLastMonth) _pageController?.AnimateToPage(2, TimeSpan.FromMilliseconds(200), Curves.Ease);
        }

        private void HandleMonthPageChanged(int page)
        {
            if (page == 1) return;
            var candidate = CurrentWidget.CalendarDelegate.AddMonthsToMonthDate(_displayedMonth, page - 1);
            var first = CurrentWidget.CalendarDelegate.GetMonth(CurrentWidget.FirstDate.Year, CurrentWidget.FirstDate.Month);
            var last = CurrentWidget.CalendarDelegate.GetMonth(CurrentWidget.LastDate.Year, CurrentWidget.LastDate.Month);
            if (candidate < first) candidate = first;
            if (candidate > last) candidate = last;
            if (!CurrentWidget.CalendarDelegate.IsSameMonth(candidate, _displayedMonth))
            {
                SetState(() =>
                {
                    _displayedMonth = candidate;
                    if (_focusedDate.HasValue && !CurrentWidget.CalendarDelegate.IsSameMonth(_focusedDate, candidate))
                    {
                        _focusedDate = FocusableDayForMonth(candidate, _focusedDate.Value.Day);
                    }
                    _announcementText = CurrentWidget.CalendarDelegate.FormatMonthYear(
                        candidate, MaterialLocalizations.Of(Context));
                });
                CurrentWidget.OnDisplayedMonthChanged?.Invoke(candidate);
            }
            Scheduler.AddPostFrameCallback(_ => _pageController?.JumpToPage(1));
        }

        private void HandleDayChanged(DateTime date)
        {
            Feedback.ForTap();
            SetState(() =>
            {
                _selectedDate = date;
                _focusedDate = date;
                var localizations = MaterialLocalizations.Of(Context);
                string suffix = CurrentWidget.CalendarDelegate.IsSameDay(CurrentWidget.CurrentDate, date)
                    ? $", {localizations.CurrentDateLabel}"
                    : string.Empty;
                _announcementText = $"{localizations.SelectedDateLabel} {CurrentWidget.CalendarDelegate.FormatFullDate(date, localizations)}{suffix}";
            });
            CurrentWidget.OnDateChanged(date);
        }

        private void HandleYearChanged(DateTime date)
        {
            var widget = CurrentWidget;
            var previousMonth = _displayedMonth;
            int days = widget.CalendarDelegate.GetDaysInMonth(date.Year, date.Month);
            int preferredDay = Math.Min(_selectedDate?.Day ?? 1, days);
            var value = widget.CalendarDelegate.GetDay(date.Year, date.Month, preferredDay);
            if (value < widget.FirstDate) value = widget.FirstDate;
            if (value > widget.LastDate) value = widget.LastDate;

            SetState(() =>
            {
                _mode = DatePickerMode.Day;
                _displayedMonth = widget.CalendarDelegate.GetMonth(value.Year, value.Month);
                _focusedDate = value;
                if (IsSelectable(value)) _selectedDate = value;
                _announcementText = widget.CalendarDelegate.FormatMonthYear(
                    value, MaterialLocalizations.Of(Context));
            });
            _pageController?.JumpToPage(1);
            if (!widget.CalendarDelegate.IsSameMonth(previousMonth, _displayedMonth))
            {
                widget.OnDisplayedMonthChanged?.Invoke(_displayedMonth);
            }
            if (IsSelectable(value)) widget.OnDateChanged(value);
        }

        private KeyEventResult HandleGridKey(FocusNode node, KeyEvent @event)
        {
            if (!@event.IsDown) return KeyEventResult.Ignored;
            var direction = Directionality.Of(Context);
            int delta = @event.Key switch
            {
                "ArrowLeft" => direction == TextDirection.Ltr ? -1 : 1,
                "ArrowRight" => direction == TextDirection.Ltr ? 1 : -1,
                "ArrowUp" => -7,
                "ArrowDown" => 7,
                _ => 0,
            };
            if (delta != 0)
            {
                var start = _focusedDate ?? _selectedDate ?? CurrentWidget.CurrentDate;
                var next = FindSelectableDate(start, delta);
                if (next.HasValue)
                {
                    var previousMonth = _displayedMonth;
                    SetState(() =>
                    {
                        _focusedDate = next;
                        _displayedMonth = CurrentWidget.CalendarDelegate.GetMonth(next.Value.Year, next.Value.Month);
                        _announcementText = CurrentWidget.CalendarDelegate.FormatFullDate(
                            next.Value, MaterialLocalizations.Of(Context));
                    });
                    _pageController?.JumpToPage(1);
                    if (!CurrentWidget.CalendarDelegate.IsSameMonth(previousMonth, _displayedMonth))
                    {
                        CurrentWidget.OnDisplayedMonthChanged?.Invoke(_displayedMonth);
                    }
                }
                return KeyEventResult.Handled;
            }

            if (@event.Key is "Enter" or "Return" or "Space" or "Spacebar")
            {
                if (_focusedDate.HasValue && IsSelectable(_focusedDate.Value)) HandleDayChanged(_focusedDate.Value);
                return KeyEventResult.Handled;
            }
            return KeyEventResult.Ignored;
        }

        private DateTime? FindSelectableDate(DateTime start, int delta)
        {
            var date = CurrentWidget.CalendarDelegate.AddDaysToDate(start, delta);
            while (date >= CurrentWidget.FirstDate && date <= CurrentWidget.LastDate)
            {
                if (IsSelectable(date)) return date;
                date = CurrentWidget.CalendarDelegate.AddDaysToDate(date, delta);
            }
            return null;
        }

        private DateTime? FocusableDayForMonth(DateTime month, int preferredDay)
        {
            int days = CurrentWidget.CalendarDelegate.GetDaysInMonth(month.Year, month.Month);
            if (preferredDay <= days)
            {
                var preferred = CurrentWidget.CalendarDelegate.GetDay(month.Year, month.Month, preferredDay);
                if (IsSelectable(preferred)) return preferred;
            }
            for (int day = 1; day <= days; day++)
            {
                var candidate = CurrentWidget.CalendarDelegate.GetDay(month.Year, month.Month, day);
                if (IsSelectable(candidate)) return candidate;
            }
            return null;
        }

        private void HandleGridFocusChanged() => SetState(() => { });

        private void HandleModeAnimationChanged() => SetState(() => { });

        private bool IsSelectable(DateTime date) =>
            date >= CurrentWidget.FirstDate && date <= CurrentWidget.LastDate
            && (CurrentWidget.SelectableDayPredicate?.Invoke(date) ?? true);
    }
}

internal sealed class CalendarDayPicker : StatelessWidget
{
    public CalendarDayPicker(
        DateTime displayedMonth,
        DateTime currentDate,
        DateTime firstDate,
        DateTime lastDate,
        DateTime? selectedDate,
        Action<DateTime> onChanged,
        CalendarDelegate<DateTime> calendarDelegate,
        DateTime? focusedDate,
        double rowHeight,
        SelectableDayPredicate? selectableDayPredicate = null,
        Key? key = null) : base(key)
    {
        DisplayedMonth = displayedMonth;
        CurrentDate = currentDate;
        FirstDate = firstDate;
        LastDate = lastDate;
        SelectedDate = selectedDate;
        OnChanged = onChanged;
        CalendarDelegate = calendarDelegate;
        FocusedDate = focusedDate;
        RowHeight = rowHeight;
        SelectableDayPredicate = selectableDayPredicate;
    }

    public DateTime DisplayedMonth { get; }
    public DateTime CurrentDate { get; }
    public DateTime FirstDate { get; }
    public DateTime LastDate { get; }
    public DateTime? SelectedDate { get; }
    public Action<DateTime> OnChanged { get; }
    public CalendarDelegate<DateTime> CalendarDelegate { get; }
    public DateTime? FocusedDate { get; }
    public double RowHeight { get; }
    public SelectableDayPredicate? SelectableDayPredicate { get; }

    public override Widget Build(BuildContext context)
    {
        var localizations = MaterialLocalizations.Of(context);
        var theme = DatePickerTheme.Of(context);
        var defaults = DatePickerTheme.Defaults(context);
        var weekdayStyle = theme.WeekdayStyle ?? defaults.WeekdayStyle!;
        var items = new List<Widget>(49);
        for (int index = localizations.FirstDayOfWeekIndex; items.Count < 7; index = (index + 1) % 7)
        {
            items.Add(new Center(
                child: new DefaultTextStyle(weekdayStyle, new Text(localizations.NarrowWeekdays[index]))));
        }

        int year = DisplayedMonth.Year;
        int month = DisplayedMonth.Month;
        int offset = CalendarDelegate.FirstDayOffset(year, month, localizations);
        for (int blank = 0; blank < offset; blank++) items.Add(new SizedBox());
        int days = CalendarDelegate.GetDaysInMonth(year, month);
        for (int day = 1; day <= days; day++)
        {
            var date = CalendarDelegate.GetDay(year, month, day);
            bool disabled = date < FirstDate || date > LastDate || !(SelectableDayPredicate?.Invoke(date) ?? true);
            items.Add(new CalendarDay(
                day: date,
                isDisabled: disabled,
                isSelected: CalendarDelegate.IsSameDay(SelectedDate, date),
                isToday: CalendarDelegate.IsSameDay(CurrentDate, date),
                isFocused: CalendarDelegate.IsSameDay(FocusedDate, date),
                onChanged: OnChanged,
                calendarDelegate: CalendarDelegate));
        }
        while (items.Count < 49) items.Add(new SizedBox());

        return GridView.Count(
            crossAxisCount: 7,
            children: items,
            mainAxisExtent: RowHeight,
            padding: new Thickness(Theme.Of(context).UseMaterial3 ? 12 : 8, 0),
            addAutomaticKeepAlives: false);
    }
}

internal sealed class CalendarDay : StatefulWidget
{
    public CalendarDay(DateTime day, bool isDisabled, bool isSelected, bool isToday, bool isFocused,
        Action<DateTime> onChanged, CalendarDelegate<DateTime> calendarDelegate,
        MaterialStateProperty<Color?>? overlayColor = null) : base(new ValueKey<DateTime>(day))
    {
        Day = day;
        IsDisabled = isDisabled;
        IsSelected = isSelected;
        IsToday = isToday;
        IsFocused = isFocused;
        OnChanged = onChanged;
        CalendarDelegate = calendarDelegate;
        OverlayColor = overlayColor;
    }

    public DateTime Day { get; }
    public bool IsDisabled { get; }
    public bool IsSelected { get; }
    public bool IsToday { get; }
    public bool IsFocused { get; }
    public Action<DateTime> OnChanged { get; }
    public CalendarDelegate<DateTime> CalendarDelegate { get; }
    public MaterialStateProperty<Color?>? OverlayColor { get; }
    public override State CreateState() => new CalendarDayState();

    private sealed class CalendarDayState : State
    {
        private MaterialStatesController? _states;
        private FocusNode? _focusNode;
        private CalendarDay CurrentWidget => (CalendarDay)StateWidget;

        public override void InitState()
        {
            _states = new MaterialStatesController();
            _states.AddListener(HandleStatesChanged);
            _focusNode = new FocusNode { SkipTraversal = true };
            SyncStates();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget) => SyncStates();

        public override void Dispose()
        {
            if (_states is null) return;
            _states.RemoveListener(HandleStatesChanged);
            _states.Dispose();
            _states = null;
            _focusNode?.Dispose();
            _focusNode = null;
        }

        public override Widget Build(BuildContext context)
        {
            var widget = CurrentWidget;
            var local = DatePickerTheme.Of(context);
            var defaults = DatePickerTheme.Defaults(context);
            var states = _states!.Value;
            var foreground = widget.IsToday
                ? local.TodayForegroundColor?.Resolve(states)
                  ?? defaults.TodayForegroundColor?.Resolve(states)
                : local.DayForegroundColor?.Resolve(states)
                  ?? defaults.DayForegroundColor?.Resolve(states);
            var background = widget.IsToday
                ? local.TodayBackgroundColor?.Resolve(states)
                  ?? defaults.TodayBackgroundColor?.Resolve(states)
                : local.DayBackgroundColor?.Resolve(states)
                  ?? defaults.DayBackgroundColor?.Resolve(states);
            var overlay = widget.OverlayColor ?? MaterialStateProperty<Color?>.ResolveWith(
                overlayStates => local.DayOverlayColor?.Resolve(overlayStates)
                                 ?? defaults.DayOverlayColor?.Resolve(overlayStates));
            OutlinedBorder shape = local.DayShape?.Resolve(states)
                                   ?? defaults.DayShape?.Resolve(states)
                                   ?? new CircleBorder();
            BorderSide? localTodayBorder = local.TodayBorder;
            var border = widget.IsToday
                ? localTodayBorder ?? defaults.TodayBorder
                : ShapeBorderGeometry.SideOrNull(shape);
            if (widget.IsToday
                && border.HasValue
                && foreground.HasValue
                && (!localTodayBorder.HasValue || localTodayBorder.Value.Color.A == 0))
            {
                border = new BorderSide(foreground.Value, border.Value.Width);
            }
            var style = (local.DayStyle ?? defaults.DayStyle!).CopyWith(color: foreground);
            var decoration = new BoxDecoration(
                Color: background,
                Border: border is { } cellBorder ? Plumix.Rendering.Border.FromBorderSide(cellBorder) : null,
                BorderRadius: shape is CircleBorder ? null : ShapeBorderGeometry.ResolveRadius(shape),
                Shape: ShapeBorderGeometry.BoxShapeOf(shape));

            Widget result = new Semantics(
                label: $"{MaterialLocalizations.Of(context).FormatDecimal(widget.Day.Day)}, {widget.CalendarDelegate.FormatFullDate(widget.Day, MaterialLocalizations.Of(context))}{(widget.IsToday ? $", {MaterialLocalizations.Of(context).CurrentDateLabel}" : string.Empty)}",
                flags: SemanticsFlags.IsButton
                       | (widget.IsDisabled ? SemanticsFlags.None : SemanticsFlags.IsEnabled)
                       | (widget.IsSelected ? SemanticsFlags.IsSelected : SemanticsFlags.None),
                onTap: widget.IsDisabled ? null : () => widget.OnChanged(widget.Day),
                child: new Center(
                    child: new Container(
                        width: 40,
                        height: 40,
                        alignment: Alignment.Center,
                        decoration: decoration,
                        child: new DefaultTextStyle(style, new Text(MaterialLocalizations.Of(context).FormatDecimal(widget.Day.Day))))));

            if (!widget.IsDisabled)
            {
                result = new InkResponse(
                    onTap: () => widget.OnChanged(widget.Day),
                    focusNode: _focusNode,
                    statesController: _states,
                    overlayColor: overlay,
                    customBorder: shape,
                    containedInkWell: true,
                    highlightShape: ShapeBorderGeometry.BoxShapeOf(shape),
                    child: result);
            }
            return result;
        }

        private void SyncStates()
        {
            _states?.Update(MaterialState.Disabled, CurrentWidget.IsDisabled);
            _states?.Update(MaterialState.Selected, CurrentWidget.IsSelected);
            _states?.Update(MaterialState.Focused, CurrentWidget.IsFocused);
        }

        private void HandleStatesChanged() => SetState(() => { });
    }
}

public sealed class YearPicker : StatefulWidget
{
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
        CalendarDelegate = calendarDelegate ?? GregorianCalendarDelegate.Instance;
        FirstDate = CalendarDelegate.DateOnly(firstDate);
        LastDate = CalendarDelegate.DateOnly(lastDate);
        if (LastDate < FirstDate) throw new ArgumentException("lastDate must be on or after firstDate.", nameof(lastDate));
        CurrentDate = CalendarDelegate.DateOnly(currentDate ?? CalendarDelegate.Now());
        SelectedDate = selectedDate.HasValue ? CalendarDelegate.DateOnly(selectedDate.Value) : null;
        InitialDate = initialDate;
        OnChanged = onChanged ?? throw new ArgumentNullException(nameof(onChanged));
        DragStartBehavior = dragStartBehavior;
    }

    public DateTime CurrentDate { get; }
    public DateTime FirstDate { get; }
    public DateTime LastDate { get; }
    public DateTime? InitialDate { get; }
    public DateTime? SelectedDate { get; }
    public Action<DateTime> OnChanged { get; }
    public DragStartBehavior DragStartBehavior { get; }
    public CalendarDelegate<DateTime> CalendarDelegate { get; }
    public override State CreateState() => new YearPickerState();

    private sealed class YearPickerState : State
    {
        private const int MinimumYears = 18;
        private ScrollController? _scrollController;
        private YearPicker CurrentWidget => (YearPicker)StateWidget;

        public override void InitState() => _scrollController = new ScrollController(ScrollOffsetFor(CurrentWidget.SelectedDate ?? CurrentWidget.FirstDate));

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var old = (YearPicker)oldWidget;
            if (CurrentWidget.SelectedDate != old.SelectedDate && CurrentWidget.SelectedDate.HasValue)
            {
                _scrollController?.JumpTo(ScrollOffsetFor(CurrentWidget.SelectedDate.Value));
            }
        }

        public override void Dispose()
        {
            _scrollController?.Dispose();
            _scrollController = null;
        }

        public override Widget Build(BuildContext context)
        {
            int count = CurrentWidget.LastDate.Year - CurrentWidget.FirstDate.Year + 1;
            int total = Math.Max(count, MinimumYears);
            double scale = Math.Clamp(MediaQuery.MaybeTextScaleFactorOf(context) ?? 1, 0, 3);
            int columns = scale > 1.65 ? 2 : 3;
            double height = 52 + (scale > 1 ? (scale - 1) * 9 : 0);
            return new Column(
                children:
                [
                    new Divider(),
                    new Expanded(
                        child: GridView.Builder(
                            itemCount: total,
                            controller: _scrollController,
                            padding: new Thickness(16, 0),
                            gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(
                                crossAxisCount: columns,
                                crossAxisSpacing: 8,
                                mainAxisExtent: height),
                            itemBuilder: (_, index) => BuildYearItem(index, count, total, scale),
                            addAutomaticKeepAlives: false)),
                    new Divider(),
                ]);
        }

        private Widget BuildYearItem(int index, int count, int total, double textScale)
        {
            int offset = count < MinimumYears ? (MinimumYears - count) / 2 : 0;
            int year = CurrentWidget.FirstDate.Year + index - offset;
            bool disabled = year < CurrentWidget.FirstDate.Year || year > CurrentWidget.LastDate.Year;
            return new CalendarYear(
                year: year,
                isDisabled: disabled,
                isSelected: year == CurrentWidget.SelectedDate?.Year,
                isCurrent: year == CurrentWidget.CurrentDate.Year,
                textScale: textScale,
                onChanged: disabled ? null : () => CurrentWidget.OnChanged(DateForYear(year)),
                calendarDelegate: CurrentWidget.CalendarDelegate);
        }

        private DateTime DateForYear(int year)
        {
            var widget = CurrentWidget;
            int month = widget.SelectedDate?.Month ?? 1;
            var date = widget.CalendarDelegate.GetMonth(year, month);
            var firstMonth = widget.CalendarDelegate.GetMonth(widget.FirstDate.Year, widget.FirstDate.Month);
            var lastMonth = widget.CalendarDelegate.GetMonth(widget.LastDate.Year, widget.LastDate.Month);
            if (date < firstMonth) date = widget.CalendarDelegate.GetMonth(year, widget.FirstDate.Month);
            if (date > lastMonth) date = widget.CalendarDelegate.GetMonth(year, widget.LastDate.Month);
            return date;
        }

        private int ItemCount => CurrentWidget.LastDate.Year - CurrentWidget.FirstDate.Year + 1;

        private double ScrollOffsetFor(DateTime date)
        {
            int row = (date.Year - CurrentWidget.FirstDate.Year) / 3;
            return ItemCount < MinimumYears ? 0 : Math.Max(0, row - 2) * 52;
        }
    }
}

internal sealed class CalendarYear : StatefulWidget
{
    public CalendarYear(int year, bool isDisabled, bool isSelected, bool isCurrent, double textScale, Action? onChanged,
        CalendarDelegate<DateTime> calendarDelegate) : base(new ValueKey<int>(year))
    {
        Year = year;
        IsDisabled = isDisabled;
        IsSelected = isSelected;
        IsCurrent = isCurrent;
        TextScale = textScale;
        OnChanged = onChanged;
        CalendarDelegate = calendarDelegate;
    }

    public int Year { get; }
    public bool IsDisabled { get; }
    public bool IsSelected { get; }
    public bool IsCurrent { get; }
    public double TextScale { get; }
    public Action? OnChanged { get; }
    public CalendarDelegate<DateTime> CalendarDelegate { get; }
    public override State CreateState() => new CalendarYearState();

    private sealed class CalendarYearState : State
    {
        private MaterialStatesController? _states;
        private CalendarYear CurrentWidget => (CalendarYear)StateWidget;

        public override void InitState()
        {
            _states = new MaterialStatesController();
            _states.AddListener(HandleStateChanged);
            SyncStates();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget) => SyncStates();

        public override void Dispose()
        {
            if (_states is null) return;
            _states.RemoveListener(HandleStateChanged);
            _states.Dispose();
            _states = null;
        }

        public override Widget Build(BuildContext context)
        {
            var widget = CurrentWidget;
            var local = DatePickerTheme.Of(context);
            var defaults = DatePickerTheme.Defaults(context);
            var states = _states!.Value;
            var foreground = widget.IsCurrent
                ? local.TodayForegroundColor?.Resolve(states)
                  ?? defaults.TodayForegroundColor?.Resolve(states)
                : local.YearForegroundColor?.Resolve(states)
                  ?? defaults.YearForegroundColor?.Resolve(states);
            var background = widget.IsCurrent
                ? local.TodayBackgroundColor?.Resolve(states)
                  ?? defaults.TodayBackgroundColor?.Resolve(states)
                : local.YearBackgroundColor?.Resolve(states)
                  ?? defaults.YearBackgroundColor?.Resolve(states);
            MaterialStateProperty<Color?>? overlay = local.YearOverlayColor is null
                                                        && defaults.YearOverlayColor is null
                ? null
                : MaterialStateProperty<Color?>.ResolveWith(
                    overlayStates => local.YearOverlayColor?.Resolve(overlayStates)
                                     ?? defaults.YearOverlayColor?.Resolve(overlayStates));
            OutlinedBorder shape = local.YearShape?.Resolve(states)
                                   ?? defaults.YearShape?.Resolve(states)
                                   ?? new StadiumBorder();
            var border = widget.IsCurrent
                ? local.TodayBorder ?? defaults.TodayBorder
                : ShapeBorderGeometry.SideOrNull(shape);
            if (widget.IsCurrent && border is not null && foreground.HasValue)
            {
                border = new BorderSide(foreground.Value, border.Value.Width);
            }
            var style = (local.YearStyle ?? defaults.YearStyle!).CopyWith(color: foreground);
            var localizations = MaterialLocalizations.Of(context);
            Widget result = new Center(
                child: new Container(
                    width: 72 * widget.TextScale,
                    height: 36 * widget.TextScale,
                    alignment: Alignment.Center,
                    decoration: new BoxDecoration(
                        Color: background,
                        Border: border is { } cellBorder ? Plumix.Rendering.Border.FromBorderSide(cellBorder) : null,
                        BorderRadius: ShapeBorderGeometry.ResolveRadius(shape),
                        Shape: ShapeBorderGeometry.BoxShapeOf(shape)),
                    child: new Semantics(
                        label: widget.CalendarDelegate.FormatYear(widget.Year, localizations),
                        flags: SemanticsFlags.IsButton
                               | (widget.IsDisabled ? SemanticsFlags.None : SemanticsFlags.IsEnabled)
                               | (widget.IsSelected ? SemanticsFlags.IsSelected : SemanticsFlags.None),
                        onTap: widget.OnChanged,
                        child: new DefaultTextStyle(style, new Text(widget.CalendarDelegate.FormatYear(widget.Year, localizations))))));
            if (!widget.IsDisabled)
            {
                result = new InkWell(
                    onTap: widget.OnChanged,
                    statesController: _states,
                    overlayColor: overlay,
                    customBorder: shape,
                    child: result);
            }
            return result;
        }

        private void SyncStates()
        {
            _states?.Update(MaterialState.Disabled, CurrentWidget.IsDisabled);
            _states?.Update(MaterialState.Selected, CurrentWidget.IsSelected);
        }

        private void HandleStateChanged() => SetState(() => { });
    }
}
