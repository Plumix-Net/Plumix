using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/date_picker.dart

public sealed class DateRangePickerDialog : StatefulWidget
{
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
        string? restorationId = null,
        Widget? switchToInputEntryModeIcon = null,
        Widget? switchToCalendarEntryModeIcon = null,
        SelectableDayForRangePredicate? selectableDayPredicate = null,
        CalendarDelegate<DateTime>? calendarDelegate = null,
        Key? key = null) : base(key)
    {
        CalendarDelegate = calendarDelegate ?? GregorianCalendarDelegate.Instance;
        FirstDate = CalendarDelegate.DateOnly(firstDate);
        LastDate = CalendarDelegate.DateOnly(lastDate);
        InitialDateRange = initialDateRange is null ? null : CalendarDelegate.DatesOnly(initialDateRange);
        CurrentDate = CalendarDelegate.DateOnly(currentDate ?? CalendarDelegate.Now());
        ValidateDates(InitialDateRange, FirstDate, LastDate, selectableDayPredicate);
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
        RestorationId = restorationId;
        SwitchToInputEntryModeIcon = switchToInputEntryModeIcon;
        SwitchToCalendarEntryModeIcon = switchToCalendarEntryModeIcon;
        SelectableDayPredicate = selectableDayPredicate;
    }

    public DateTimeRange<DateTime>? InitialDateRange { get; }
    public DateTime FirstDate { get; }
    public DateTime LastDate { get; }
    public DateTime CurrentDate { get; }
    public DatePickerEntryMode InitialEntryMode { get; }
    public string? HelpText { get; }
    public string? CancelText { get; }
    public string? ConfirmText { get; }
    public string? SaveText { get; }
    public string? ErrorInvalidRangeText { get; }
    public string? ErrorFormatText { get; }
    public string? ErrorInvalidText { get; }
    public string? FieldStartHintText { get; }
    public string? FieldEndHintText { get; }
    public string? FieldStartLabelText { get; }
    public string? FieldEndLabelText { get; }
    public string? RestorationId { get; }
    public Widget? SwitchToInputEntryModeIcon { get; }
    public Widget? SwitchToCalendarEntryModeIcon { get; }
    public SelectableDayForRangePredicate? SelectableDayPredicate { get; }
    public CalendarDelegate<DateTime> CalendarDelegate { get; }

    public override State CreateState() => new DateRangePickerDialogState();

    internal static void ValidateDates(
        DateTimeRange<DateTime>? range,
        DateTime firstDate,
        DateTime lastDate,
        SelectableDayForRangePredicate? predicate)
    {
        if (lastDate < firstDate) throw new ArgumentException("lastDate must be on or after firstDate.", nameof(lastDate));
        if (range is null) return;
        if (range.Start < firstDate || range.Start > lastDate) throw new ArgumentOutOfRangeException(nameof(range));
        if (range.End < firstDate || range.End > lastDate) throw new ArgumentOutOfRangeException(nameof(range));
        if (predicate is not null && (!predicate(range.Start, range.Start, range.End)
                                      || !predicate(range.End, range.Start, range.End)))
        {
            throw new ArgumentException("The initial range must satisfy selectableDayPredicate.", nameof(range));
        }
    }
}

internal sealed class DateRangePickerDialogState : State
{
    private static readonly Size InputPortraitSizeM2 = new(330, 270);
    private static readonly Size InputPortraitSizeM3 = new(328, 270);
    private static readonly Size InputLandscapeSize = new(496, 164);
    private readonly LabeledGlobalKey<FormState> _formKey = new("date-range-input-form");
    private readonly TextEditingController _startController = new();
    private readonly TextEditingController _endController = new();
    private DateTime? _selectedStart;
    private DateTime? _selectedEnd;
    private DatePickerEntryMode _entryMode;
    private AutovalidateMode _autovalidateMode = AutovalidateMode.Disabled;
    private bool _inputsInitialized;

    private DateRangePickerDialog Current => (DateRangePickerDialog)StateWidget;

    public override void InitState()
    {
        _selectedStart = Current.InitialDateRange?.Start;
        _selectedEnd = Current.InitialDateRange?.End;
        _entryMode = Current.InitialEntryMode;
    }

    public override void DidChangeDependencies()
    {
        if (_inputsInitialized) return;
        _inputsInitialized = true;
        SyncInputControllers();
    }

    public override void Dispose()
    {
        _startController.Dispose();
        _endController.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return _entryMode is DatePickerEntryMode.Calendar or DatePickerEntryMode.CalendarOnly
            ? BuildCalendarDialog(context)
            : BuildInputDialog(context);
    }

    private Widget BuildCalendarDialog(BuildContext context)
    {
        var theme = Theme.Of(context);
        var local = DatePickerTheme.Of(context);
        var defaults = DatePickerTheme.Defaults(context);
        var localizations = MaterialLocalizations.Of(context);
        var media = MediaQuery.MaybeOf(context) ?? new MediaQueryData(Size: new Size(360, 640));
        var foreground = local.RangePickerHeaderForegroundColor ?? defaults.RangePickerHeaderForegroundColor;
        var background = local.RangePickerHeaderBackgroundColor ?? defaults.RangePickerHeaderBackgroundColor;
        var helpStyle = (local.RangePickerHeaderHelpStyle ?? defaults.RangePickerHeaderHelpStyle ?? theme.TextTheme.LabelLarge)
            .CopyWith(color: foreground);
        var headlineStyle = (local.RangePickerHeaderHeadlineStyle ?? defaults.RangePickerHeaderHeadlineStyle ?? theme.TextTheme.TitleLarge)
            .CopyWith(color: foreground);
        string startText = FormatRangeStart(localizations);
        string endText = FormatRangeEnd(localizations);
        var switchButton = BuildModeButton(context, toInput: true, foreground);

        Widget header = new ColoredBox(
            background ?? Colors.Transparent,
            child: new SafeArea(
                bottom: false,
                child: new SizedBox(
                    height: 120,
                    child: new Column(
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        children:
                        [
                            new Row(
                                mainAxisAlignment: MainAxisAlignment.SpaceBetween,
                                children:
                                [
                                    new CloseButton(color: foreground, onPressed: () => Navigator.Pop(context)),
                                    new Row(
                                        mainAxisSize: MainAxisSize.Min,
                                        children:
                                        [
                                            switchButton,
                                            new TextButton(
                                                onPressed: HasCompleteRange ? HandleOk : null,
                                                style: TextButton.StyleFrom(foregroundColor: foreground),
                                                child: new Text(Current.SaveText ?? (theme.UseMaterial3
                                                    ? localizations.SaveButtonLabel
                                                    : localizations.SaveButtonLabel.ToUpperInvariant()))),
                                            new SizedBox(width: 8),
                                        ])
                                ]),
                            new Padding(
                                new Thickness(media.Size.Width < 360 ? 42 : 72, 0, 16, 12),
                                new Semantics(
                                    label: $"{Current.HelpText ?? localizations.DateRangePickerHelpText} {startText} to {endText}",
                                    child: new Column(
                                        crossAxisAlignment: CrossAxisAlignment.Start,
                                        spacing: 6,
                                        children:
                                        [
                                            new DefaultTextStyle(helpStyle, new Text(Current.HelpText ?? localizations.DateRangePickerHelpText)),
                                            new DefaultTextStyle(headlineStyle, new Text($"{startText} – {endText}", maxLines: 1, overflow: TextOverflow.Ellipsis)),
                                        ])))
                        ]))));

        var calendar = new CalendarDateRangePicker(
            initialStartDate: _selectedStart,
            initialEndDate: _selectedEnd,
            firstDate: Current.FirstDate,
            lastDate: Current.LastDate,
            currentDate: Current.CurrentDate,
            selectableDayPredicate: Current.SelectableDayPredicate,
            calendarDelegate: Current.CalendarDelegate,
            onStartDateChanged: value => SetState(() =>
            {
                _selectedStart = value;
                _selectedEnd = null;
                SyncInputControllers();
            }),
            onEndDateChanged: value => SetState(() =>
            {
                _selectedEnd = value;
                SyncInputControllers();
            }));

        return Dialog.Fullscreen(
            backgroundColor: local.RangePickerBackgroundColor ?? defaults.RangePickerBackgroundColor,
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children:
                [
                    header,
                    new Expanded(calendar),
                ]));
    }

    private Widget BuildInputDialog(BuildContext context)
    {
        var theme = Theme.Of(context);
        var localizations = MaterialLocalizations.Of(context);
        var local = DatePickerTheme.Of(context);
        var defaults = DatePickerTheme.Defaults(context);
        var media = MediaQuery.MaybeOf(context) ?? new MediaQueryData(Size: new Size(360, 640));
        bool landscape = media.Orientation == Orientation.Landscape;
        var size = landscape ? InputLandscapeSize : theme.UseMaterial3 ? InputPortraitSizeM3 : InputPortraitSizeM2;
        var foreground = local.HeaderForegroundColor ?? defaults.HeaderForegroundColor;
        string headline = FormatRange(localizations);

        var header = new Container(
            width: landscape ? 152 : null,
            height: landscape ? null : 120,
            color: local.HeaderBackgroundColor ?? defaults.HeaderBackgroundColor,
            padding: new Thickness(24, landscape ? 20 : 16),
            child: new Row(
                crossAxisAlignment: CrossAxisAlignment.Start,
                children:
                [
                    new Expanded(new Column(
                        mainAxisSize: MainAxisSize.Min,
                        crossAxisAlignment: CrossAxisAlignment.Start,
                        spacing: 12,
                        children:
                        [
                            new DefaultTextStyle(
                                (local.HeaderHelpStyle ?? defaults.HeaderHelpStyle ?? theme.TextTheme.LabelLarge).CopyWith(color: foreground),
                                new Text(Current.HelpText ?? localizations.DateRangePickerHelpText)),
                            new DefaultTextStyle(
                                (landscape ? theme.TextTheme.HeadlineSmall : local.HeaderHeadlineStyle ?? defaults.HeaderHeadlineStyle ?? theme.TextTheme.HeadlineMedium)
                                    .CopyWith(color: foreground),
                                new Text(headline, maxLines: 2, overflow: TextOverflow.Ellipsis)),
                        ])),
                    BuildModeButton(context, toInput: false, foreground),
                ]));

        var picker = new Form(
            key: _formKey,
            autovalidateMode: _autovalidateMode,
            child: new Padding(
                new Thickness(24, 12),
                new Row(
                    spacing: 12,
                    children:
                    [
                        new Expanded(BuildInputField(_startController, start: true)),
                        new Expanded(BuildInputField(_endController, start: false)),
                    ])));

        var actions = new Padding(
            new Thickness(8, 4),
            new Row(
                mainAxisAlignment: MainAxisAlignment.End,
                spacing: 8,
                children:
                [
                    new TextButton(
                        style: local.CancelButtonStyle ?? defaults.CancelButtonStyle,
                        onPressed: () => Navigator.Pop(context),
                        child: new Text(Current.CancelText ?? (theme.UseMaterial3
                            ? localizations.CancelButtonLabel
                            : localizations.CancelButtonLabel.ToUpperInvariant()))),
                    new TextButton(
                        style: local.ConfirmButtonStyle ?? defaults.ConfirmButtonStyle,
                        onPressed: HandleOk,
                        child: new Text(Current.ConfirmText ?? localizations.OkButtonLabel)),
                ]));

        Widget content = landscape
            ? new Row(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children:
                [
                    header,
                    new Expanded(new Column(children: [new Expanded(picker), actions])),
                ])
            : new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children:
                [
                    header,
                    new Expanded(picker),
                    actions,
                ]);

        return new Dialog(
            backgroundColor: local.BackgroundColor ?? defaults.BackgroundColor,
            elevation: local.Elevation
                       ?? (theme.UseMaterial3
                           ? defaults.Elevation
                           : DialogTheme.Of(context).Elevation ?? defaults.Elevation),
            shadowColor: local.ShadowColor ?? defaults.ShadowColor,
            surfaceTintColor: local.SurfaceTintColor ?? defaults.SurfaceTintColor,
            shape: local.Shape
                   ?? (theme.UseMaterial3
                       ? defaults.Shape
                       : DialogTheme.Of(context).Shape ?? defaults.Shape),
            insetPadding: new Thickness(16, 24),
            child: new SizedBox(width: size.Width, height: size.Height, child: content));
    }

    private Widget BuildInputField(TextEditingController controller, bool start)
    {
        var localizations = MaterialLocalizations.Of(Context);
        var dateTheme = DatePickerTheme.Of(Context);
        var inputTheme = dateTheme.InputDecorationTheme ?? Theme.Of(Context).InputDecorationTheme;
        return new TextFormField(
            controller: controller,
            autofocus: start,
            decoration: new InputDecoration(
                    hintText: start
                        ? Current.FieldStartHintText ?? Current.CalendarDelegate.DateHelpText(localizations)
                        : Current.FieldEndHintText ?? Current.CalendarDelegate.DateHelpText(localizations),
                    labelText: start
                        ? Current.FieldStartLabelText ?? localizations.DateRangeStartLabel
                        : Current.FieldEndLabelText ?? localizations.DateRangeEndLabel)
                .ApplyDefaults(inputTheme),
            validator: value => ValidateInputDate(value, start),
            onSaved: value => SaveInputDate(value, start));
    }

    private Widget BuildModeButton(BuildContext context, bool toInput, Color? color)
    {
        bool visible = _entryMode is DatePickerEntryMode.Calendar or DatePickerEntryMode.Input;
        if (!visible) return new SizedBox();
        var localizations = MaterialLocalizations.Of(context);
        return new Tooltip(
            message: toInput ? localizations.InputDateModeButtonLabel : localizations.CalendarModeButtonLabel,
            child: new IconButton(
                icon: toInput
                    ? Current.SwitchToInputEntryModeIcon ?? new Icon(Theme.Of(context).UseMaterial3 ? Icons.EditOutlined : Icons.Edit)
                    : Current.SwitchToCalendarEntryModeIcon ?? new Icon(Icons.CalendarToday),
                color: color,
                onPressed: ToggleEntryMode));
    }

    private void ToggleEntryMode()
    {
        SetState(() =>
        {
            if (_entryMode == DatePickerEntryMode.Calendar)
            {
                _autovalidateMode = AutovalidateMode.Disabled;
                _entryMode = DatePickerEntryMode.Input;
            }
            else if (_entryMode == DatePickerEntryMode.Input)
            {
                _formKey.CurrentState?.Save();
                if (_selectedStart.HasValue && _selectedEnd.HasValue && _selectedStart > _selectedEnd)
                    _selectedEnd = null;
                if (_selectedStart.HasValue && !IsSelectable(_selectedStart.Value))
                {
                    _selectedStart = null;
                    _selectedEnd = null;
                }
                else if (_selectedEnd.HasValue && !IsSelectable(_selectedEnd.Value))
                {
                    _selectedEnd = null;
                }
                _entryMode = DatePickerEntryMode.Calendar;
            }
            else
            {
                throw new InvalidOperationException($"Cannot change entry mode from {_entryMode}.");
            }
            SyncInputControllers();
        });
    }

    private void HandleOk()
    {
        if (_entryMode is DatePickerEntryMode.Input or DatePickerEntryMode.InputOnly)
        {
            var form = _formKey.CurrentState;
            if (form is null || !form.Validate())
            {
                SetState(() => _autovalidateMode = AutovalidateMode.Always);
                return;
            }
            form.Save();
        }
        Navigator.Pop(Context, HasCompleteRange ? new DateTimeRange<DateTime>(_selectedStart!.Value, _selectedEnd!.Value) : null);
    }

    private string? ValidateInputDate(string? text, bool start)
    {
        var localizations = MaterialLocalizations.Of(Context);
        var date = Current.CalendarDelegate.ParseCompactDate(text, localizations);
        if (!date.HasValue) return Current.ErrorFormatText ?? localizations.InvalidDateFormatLabel;
        if (!IsSelectable(date.Value)) return Current.ErrorInvalidText ?? localizations.DateOutOfRangeLabel;
        string otherText = start ? _endController.Text : _startController.Text;
        var other = Current.CalendarDelegate.ParseCompactDate(otherText, localizations);
        if (other.HasValue && (start ? date > other : other > date))
            return Current.ErrorInvalidRangeText ?? localizations.InvalidDateRangeLabel;
        return null;
    }

    private void SaveInputDate(string? text, bool start)
    {
        var date = Current.CalendarDelegate.ParseCompactDate(text, MaterialLocalizations.Of(Context));
        if (!date.HasValue || !IsSelectable(date.Value)) return;
        if (start) _selectedStart = date;
        else _selectedEnd = date;
    }

    private bool IsSelectable(DateTime date) => date >= Current.FirstDate
        && date <= Current.LastDate
        && (Current.SelectableDayPredicate?.Invoke(date, _selectedStart, _selectedEnd) ?? true);

    private bool HasCompleteRange => _selectedStart.HasValue && _selectedEnd.HasValue;

    private void SyncInputControllers()
    {
        if (!_inputsInitialized) return;
        var localizations = MaterialLocalizations.Of(Context);
        _startController.Text = _selectedStart.HasValue
            ? Current.CalendarDelegate.FormatCompactDate(_selectedStart.Value, localizations)
            : string.Empty;
        _endController.Text = _selectedEnd.HasValue
            ? Current.CalendarDelegate.FormatCompactDate(_selectedEnd.Value, localizations)
            : string.Empty;
    }

    private string FormatRangeStart(MaterialLocalizations localizations) => _selectedStart.HasValue
        ? Current.CalendarDelegate.FormatShortMonthDay(_selectedStart.Value, localizations)
        : localizations.DateRangeStartLabel;

    private string FormatRangeEnd(MaterialLocalizations localizations) => _selectedEnd.HasValue
        ? Current.CalendarDelegate.FormatShortDate(_selectedEnd.Value, localizations)
        : localizations.DateRangeEndLabel;

    private string FormatRange(MaterialLocalizations localizations) => HasCompleteRange
        ? $"{FormatRangeStart(localizations)} – {FormatRangeEnd(localizations)}"
        : localizations.UnspecifiedDateRange;

}

internal sealed class CalendarDateRangePicker : StatefulWidget
{
    public CalendarDateRangePicker(
        DateTime? initialStartDate,
        DateTime? initialEndDate,
        DateTime firstDate,
        DateTime lastDate,
        DateTime currentDate,
        Action<DateTime> onStartDateChanged,
        Action<DateTime?> onEndDateChanged,
        SelectableDayForRangePredicate? selectableDayPredicate,
        CalendarDelegate<DateTime> calendarDelegate)
    {
        InitialStartDate = initialStartDate;
        InitialEndDate = initialEndDate;
        FirstDate = firstDate;
        LastDate = lastDate;
        CurrentDate = currentDate;
        OnStartDateChanged = onStartDateChanged;
        OnEndDateChanged = onEndDateChanged;
        SelectableDayPredicate = selectableDayPredicate;
        CalendarDelegate = calendarDelegate;
    }

    public DateTime? InitialStartDate { get; }
    public DateTime? InitialEndDate { get; }
    public DateTime FirstDate { get; }
    public DateTime LastDate { get; }
    public DateTime CurrentDate { get; }
    public Action<DateTime> OnStartDateChanged { get; }
    public Action<DateTime?> OnEndDateChanged { get; }
    public SelectableDayForRangePredicate? SelectableDayPredicate { get; }
    public CalendarDelegate<DateTime> CalendarDelegate { get; }
    public override State CreateState() => new CalendarDateRangePickerState();
}

internal sealed class CalendarDateRangePickerState : State
{
    private const double MonthHeaderHeight = 58;
    private const double MonthFooterHeight = 12;
    private const double DayRowHeight = 42;
    private const double DayRowSpacing = 8;
    private DateTime? _start;
    private DateTime? _end;
    private ScrollController? _controller;
    private CalendarDateRangePicker Current => (CalendarDateRangePicker)StateWidget;

    public override void InitState()
    {
        _start = Current.InitialStartDate;
        _end = Current.InitialEndDate;
        var initial = _start ?? Current.CurrentDate;
        int initialMonth = initial >= Current.FirstDate && initial <= Current.LastDate
            ? Current.CalendarDelegate.MonthDelta(Current.FirstDate, initial)
            : 0;
        double initialOffset = 0.0;
        for (int index = 0; index < initialMonth; index++) initialOffset += MonthExtent(index);
        _controller = new ScrollController(initialScrollOffset: Math.Max(0, initialOffset));
    }

    public override void Dispose()
    {
        _controller?.Dispose();
        _controller = null;
    }

    public override Widget Build(BuildContext context)
    {
        var localizations = MaterialLocalizations.Of(context);
        var weekdayStyle = DatePickerTheme.Of(context).WeekdayStyle
                           ?? DatePickerTheme.Defaults(context).WeekdayStyle!;
        var headers = new List<Widget>();
        for (int index = localizations.FirstDayOfWeekIndex; headers.Count < 7; index = (index + 1) % 7)
        {
            headers.Add(new Center(new DefaultTextStyle(weekdayStyle, new Text(localizations.NarrowWeekdays[index]))));
        }
        int monthCount = Current.CalendarDelegate.MonthDelta(Current.FirstDate, Current.LastDate) + 1;

        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            children:
            [
                new SizedBox(
                    height: 42,
                    child: new GridView(
                        gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(7, mainAxisExtent: 42),
                        children: headers,
                        addAutomaticKeepAlives: false)),
                new Divider(height: 0),
                new Expanded(ListView.Builder(
                    itemCount: monthCount,
                    controller: _controller,
                    addAutomaticKeepAlives: false,
                    itemBuilder: (_, index) => BuildMonth(index))),
            ]);
    }

    private Widget BuildMonth(int monthIndex)
    {
        var month = Current.CalendarDelegate.AddMonthsToMonthDate(Current.FirstDate, monthIndex);
        var localizations = MaterialLocalizations.Of(Context);
        var theme = Theme.Of(Context);
        var dateTheme = DatePickerTheme.Of(Context);
        var defaults = DatePickerTheme.Defaults(Context);
        var items = new List<Widget>(42);
        int offset = Current.CalendarDelegate.FirstDayOffset(month.Year, month.Month, localizations);
        int count = Current.CalendarDelegate.GetDaysInMonth(month.Year, month.Month);
        int weeks = (int)Math.Ceiling((offset + count) / 7.0);
        double gridHeight = weeks * DayRowHeight + (weeks - 1) * DayRowSpacing;
        for (int blank = 0; blank < offset; blank++) items.Add(new SizedBox());
        for (int day = 1; day <= count; day++)
        {
            var date = Current.CalendarDelegate.GetDay(month.Year, month.Month, day);
            bool disabled = date < Current.FirstDate || date > Current.LastDate
                                                     || !(Current.SelectableDayPredicate?.Invoke(date, _start, _end) ?? true);
            bool endpoint = Current.CalendarDelegate.IsSameDay(date, _start)
                            || Current.CalendarDelegate.IsSameDay(date, _end);
            bool inRange = _start.HasValue && _end.HasValue && date >= _start && date <= _end;
            Widget cell = new CalendarDay(
                day: date,
                isDisabled: disabled,
                isSelected: endpoint,
                isToday: Current.CalendarDelegate.IsSameDay(date, Current.CurrentDate),
                isFocused: false,
                onChanged: UpdateSelection,
                calendarDelegate: Current.CalendarDelegate,
                overlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                    dateTheme.RangeSelectionOverlayColor?.Resolve(states)
                    ?? defaults.RangeSelectionOverlayColor?.Resolve(states)));
            if (inRange)
            {
                cell = new CustomPaint(
                    painter: new DateRangeHighlightPainter(
                        color: dateTheme.RangeSelectionBackgroundColor
                               ?? defaults.RangeSelectionBackgroundColor
                               ?? theme.ColorScheme.SecondaryContainer,
                        isStart: Current.CalendarDelegate.IsSameDay(date, _start),
                        isEnd: Current.CalendarDelegate.IsSameDay(date, _end),
                        textDirection: Directionality.Of(Context)),
                    child: cell);
            }
            items.Add(cell);
        }
        while (items.Count < weeks * 7) items.Add(new SizedBox());

        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            children:
            [
                new SizedBox(
                    height: MonthHeaderHeight,
                    child: new Padding(
                        new Thickness(24, 0),
                        new Align(
                            alignment: Alignment.CenterLeft,
                            child: new DefaultTextStyle(
                                theme.TextTheme.TitleSmall,
                                new Text(Current.CalendarDelegate.FormatMonthYear(month, localizations)))))),
                new SizedBox(
                    height: gridHeight,
                    child: GridView.Count(
                        crossAxisCount: 7,
                        mainAxisExtent: DayRowHeight,
                        mainAxisSpacing: DayRowSpacing,
                        padding: new Thickness(8, 0),
                        addAutomaticKeepAlives: false,
                        children: items)),
                new SizedBox(height: MonthFooterHeight),
            ]);
    }

    private double MonthExtent(int monthIndex)
    {
        var month = Current.CalendarDelegate.AddMonthsToMonthDate(Current.FirstDate, monthIndex);
        int offset = Current.CalendarDelegate.FirstDayOffset(
            month.Year,
            month.Month,
            MaterialLocalizations.Of(Context));
        int days = Current.CalendarDelegate.GetDaysInMonth(month.Year, month.Month);
        int weeks = (int)Math.Ceiling((offset + days) / 7.0);
        return MonthHeaderHeight + MonthFooterHeight
               + weeks * DayRowHeight
               + (weeks - 1) * DayRowSpacing;
    }

    private void UpdateSelection(DateTime date)
    {
        SetState(() =>
        {
            if (_start.HasValue && !_end.HasValue && date >= _start.Value)
            {
                _end = date;
                Current.OnEndDateChanged(date);
            }
            else
            {
                _start = date;
                Current.OnStartDateChanged(date);
                if (_end.HasValue)
                {
                    _end = null;
                    Current.OnEndDateChanged(null);
                }
            }
        });
    }
}

internal sealed class DateRangeHighlightPainter : CustomPainter
{
    public DateRangeHighlightPainter(Color color, bool isStart, bool isEnd, TextDirection textDirection)
    {
        Color = color;
        IsStart = isStart;
        IsEnd = isEnd;
        TextDirection = textDirection;
    }

    public Color Color { get; }
    public bool IsStart { get; }
    public bool IsEnd { get; }
    public TextDirection TextDirection { get; }

    public override void Paint(PaintingContext context, Size size)
    {
        if (IsStart && IsEnd) return;
        double left = 0.0;
        double width = size.Width;
        if (IsStart)
        {
            left = TextDirection == TextDirection.Ltr ? size.Width / 2 : 0;
            width = size.Width / 2;
        }
        else if (IsEnd)
        {
            left = TextDirection == TextDirection.Ltr ? 0 : size.Width / 2;
            width = size.Width / 2;
        }
        context.Canvas.DrawRectangle(new SolidColorBrush(Color), null, new Rect(left, 0, width, size.Height));
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate) => oldDelegate is not DateRangeHighlightPainter old
        || old.Color != Color
        || old.IsStart != IsStart
        || old.IsEnd != IsEnd
        || old.TextDirection != TextDirection;
}

public static partial class MaterialDatePickers
{
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
        bool barrierDismissible = true,
        Color? barrierColor = null,
        string? barrierLabel = null,
        bool useRootNavigator = true,
        RouteSettings? routeSettings = null,
        Locale? locale = null,
        TextDirection? textDirection = null,
        DatePickerTransitionBuilder? builder = null,
        Widget? switchToInputEntryModeIcon = null,
        Widget? switchToCalendarEntryModeIcon = null,
        SelectableDayForRangePredicate? selectableDayPredicate = null,
        CalendarDelegate<DateTime>? calendarDelegate = null)
    {
        Widget dialog = new DateRangePickerDialog(
            initialDateRange: initialDateRange,
            firstDate: firstDate,
            lastDate: lastDate,
            currentDate: currentDate,
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
            switchToInputEntryModeIcon: switchToInputEntryModeIcon,
            switchToCalendarEntryModeIcon: switchToCalendarEntryModeIcon,
            selectableDayPredicate: selectableDayPredicate,
            calendarDelegate: calendarDelegate);
        if (textDirection.HasValue) dialog = new Directionality(textDirection.Value, dialog);
        Locale? effectiveLocale = locale ?? DatePickerTheme.Of(context).Locale;
        if (effectiveLocale is not null)
        {
            dialog = Localizations.Override(context, dialog, locale: effectiveLocale);
        }
        var captured = dialog;
        return MaterialDialogs.ShowDialog<DateTimeRange<DateTime>?>(
            context,
            routeContext => builder?.Invoke(routeContext, captured) ?? captured,
            barrierDismissible: barrierDismissible,
            barrierColor: barrierColor,
            barrierLabel: barrierLabel,
            useSafeArea: false,
            useRootNavigator: useRootNavigator,
            routeSettings: routeSettings);
    }
}
