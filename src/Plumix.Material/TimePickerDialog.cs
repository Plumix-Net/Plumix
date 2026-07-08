using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/time_picker.dart

public enum TimePickerEntryMode
{
    Dial,
    Input,
    DialOnly,
    InputOnly,
}

public delegate void EntryModeChangeCallback(TimePickerEntryMode mode);
public delegate Widget TimePickerTransitionBuilder(BuildContext context, Widget child);

public sealed class TimePickerDialog : StatefulWidget
{
    public TimePickerDialog(
        TimeOfDay initialTime,
        string? cancelText = null,
        string? confirmText = null,
        string? helpText = null,
        string? errorInvalidText = null,
        string? hourLabelText = null,
        string? minuteLabelText = null,
        string? restorationId = null,
        TimePickerEntryMode initialEntryMode = TimePickerEntryMode.Dial,
        Orientation? orientation = null,
        EntryModeChangeCallback? onEntryModeChanged = null,
        Widget? switchToInputEntryModeIcon = null,
        Widget? switchToTimerEntryModeIcon = null,
        bool emptyInitialInput = false,
        Key? key = null) : base(key)
    {
        InitialTime = initialTime;
        CancelText = cancelText;
        ConfirmText = confirmText;
        HelpText = helpText;
        ErrorInvalidText = errorInvalidText;
        HourLabelText = hourLabelText;
        MinuteLabelText = minuteLabelText;
        RestorationId = restorationId;
        InitialEntryMode = initialEntryMode;
        Orientation = orientation;
        OnEntryModeChanged = onEntryModeChanged;
        SwitchToInputEntryModeIcon = switchToInputEntryModeIcon;
        SwitchToTimerEntryModeIcon = switchToTimerEntryModeIcon;
        EmptyInitialInput = emptyInitialInput;
    }

    public TimeOfDay InitialTime { get; }
    public string? CancelText { get; }
    public string? ConfirmText { get; }
    public string? HelpText { get; }
    public string? ErrorInvalidText { get; }
    public string? HourLabelText { get; }
    public string? MinuteLabelText { get; }
    public string? RestorationId { get; }
    public TimePickerEntryMode InitialEntryMode { get; }
    public Orientation? Orientation { get; }
    public EntryModeChangeCallback? OnEntryModeChanged { get; }
    public Widget? SwitchToInputEntryModeIcon { get; }
    public Widget? SwitchToTimerEntryModeIcon { get; }
    public bool EmptyInitialInput { get; }

    public override State CreateState() => new TimePickerDialogState();
}

internal sealed class TimePickerDialogState : State
{
    internal static readonly Size PortraitSize = new(310, 468);
    internal static readonly Size LandscapeSizeM3 = new(524, 342);
    internal static readonly Size LandscapeSizeM2 = new(508, 300);
    internal static readonly Size InputSize = new(312, 252);

    private readonly LabeledGlobalKey<FormState> _formKey = new("time-picker-form");
    private readonly TextEditingController _hourController = new();
    private readonly TextEditingController _minuteController = new();
    private TimePickerEntryMode _entryMode;
    private TimeOfDay _selectedTime;
    private DayPeriod _inputPeriod;
    private bool _selectingHour = true;
    private bool _initializedInputs;
    private bool _leaveInitialInputEmpty;
    private bool _use24HourFormat;
    private AutovalidateMode _autovalidateMode = AutovalidateMode.Disabled;

    private TimePickerDialog Current => (TimePickerDialog)StateWidget;

    public override void InitState()
    {
        _entryMode = Current.InitialEntryMode;
        _selectedTime = Current.InitialTime;
        _inputPeriod = _selectedTime.Period;
        _leaveInitialInputEmpty = Current.EmptyInitialInput
                                  && _entryMode is TimePickerEntryMode.Input or TimePickerEntryMode.InputOnly;
    }

    public override void DidChangeDependencies()
    {
        var use24 = MediaQuery.MaybeAlwaysUse24HourFormatOf(Context) ?? false;
        if (_initializedInputs && use24 == _use24HourFormat) return;
        _use24HourFormat = use24;
        _initializedInputs = true;
        SyncInputControllers();
    }

    public override void Dispose()
    {
        _hourController.Dispose();
        _minuteController.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var local = TimePickerTheme.Of(context);
        var defaults = TimePickerTheme.Defaults(context);
        var media = MediaQuery.MaybeOf(context) ?? new MediaQueryData(Size: new Size(360, 640));
        var orientation = Current.Orientation ?? media.Orientation;
        var dialMode = _entryMode is TimePickerEntryMode.Dial or TimePickerEntryMode.DialOnly;
        var size = dialMode
            ? orientation == Orientation.Portrait
                ? PortraitSize
                : theme.UseMaterial3 ? LandscapeSizeM3 : LandscapeSizeM2
            : InputSize;
        var textScale = Math.Clamp(media.TextScaleFactor, 0, 1.1);
        size = new Size(size.Width * (orientation == Orientation.Landscape ? textScale : 1), size.Height * textScale);

        Widget picker = dialMode
            ? BuildDialPicker(context, orientation, local, defaults)
            : BuildInputPicker(context, orientation, local, defaults);
        Widget body = new Column(
            mainAxisSize: MainAxisSize.Min,
            mainAxisAlignment: MainAxisAlignment.SpaceBetween,
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            children:
            [
                new Flexible(picker),
                BuildActions(context, local, defaults),
            ]);

        body = new SizedBox(width: size.Width, height: size.Height, child: body);
        body = new SingleChildScrollView(
            scrollDirection: Axis.Horizontal,
            child: new SingleChildScrollView(body));

        return new Dialog(
            shape: local.Shape ?? defaults.Shape,
            elevation: local.Elevation ?? defaults.Elevation,
            backgroundColor: local.BackgroundColor ?? defaults.BackgroundColor,
            insetPadding: dialMode ? new Thickness(16, 24) : new Thickness(16, 0),
            child: new Padding(local.Padding ?? defaults.Padding ?? default, body));
    }

    private Widget BuildDialPicker(
        BuildContext context,
        Orientation orientation,
        TimePickerThemeData local,
        TimePickerThemeData defaults)
    {
        var header = BuildHeader(context, local, defaults, input: false);
        var dial = new TimePickerDial(
            selectedTime: _selectedTime,
            selectingHour: _selectingHour,
            use24HourFormat: _use24HourFormat,
            onChanged: (value, finished) =>
            {
                SetState(() =>
                {
                    _selectedTime = value;
                    _inputPeriod = value.Period;
                    _leaveInitialInputEmpty = false;
                    if (finished && _selectingHour) _selectingHour = false;
                    SyncInputControllers();
                });
            },
            theme: local,
            defaults: defaults);

        if (orientation == Orientation.Landscape)
        {
            return new Padding(
                new Thickness(0, 0, 0, 8),
                new Row(
                    mainAxisAlignment: MainAxisAlignment.SpaceAround,
                    crossAxisAlignment: CrossAxisAlignment.Center,
                    children:
                    [
                        new SizedBox(width: 216, child: header),
                        new SizedBox(width: 256, height: 256, child: dial),
                    ]));
        }

        return new Column(
            mainAxisSize: MainAxisSize.Min,
            children:
            [
                header,
                new Padding(new Thickness(0, 12, 0, 0), new SizedBox(width: 256, height: 256, child: dial)),
            ]);
    }

    private Widget BuildInputPicker(
        BuildContext context,
        Orientation orientation,
        TimePickerThemeData local,
        TimePickerThemeData defaults)
    {
        var localizations = MaterialLocalizations.Of(context);
        var inputTheme = local.InputDecorationTheme ?? defaults.InputDecorationTheme ?? new InputDecorationThemeData();
        var fieldStyle = (local.HourMinuteTextStyle ?? defaults.HourMinuteTextStyle ?? Theme.Of(context).TextTheme.HeadlineMedium)
            .CopyWith(fontSize: 40);

        Widget field(TextEditingController controller, bool hour) => new SizedBox(
            width: 96,
            child: new TextFormField(
                controller: controller,
                autofocus: hour,
                textAlign: TextAlign.Center,
                style: fieldStyle,
                maxLength: 2,
                buildCounter: (_, _, _, _) => new SizedBox(),
                decoration: new InputDecoration(
                        labelText: hour
                            ? Current.HourLabelText ?? localizations.HourLabel
                            : Current.MinuteLabelText ?? localizations.MinuteLabel,
                        contentPadding: new Thickness(8, 10),
                        counterText: string.Empty)
                    .ApplyDefaults(inputTheme),
                validator: value => ValidateTimePart(value, hour),
                onSaved: value => SaveTimePart(value, hour),
                onFieldSubmitted: _ =>
                {
                    if (hour) FocusManager.Instance.FocusNext();
                    else HandleOk();
                }));

        var separatorStates = MaterialState.None;
        var separatorStyle = (local.TimeSelectorSeparatorTextStyle ?? defaults.TimeSelectorSeparatorTextStyle)
            ?.Resolve(separatorStates)
            ?? fieldStyle;
        var separatorColor = (local.TimeSelectorSeparatorColor ?? defaults.TimeSelectorSeparatorColor)
            ?.Resolve(separatorStates);

        var fieldChildren = new List<Widget>
        {
            field(_hourController, true),
            new DefaultTextStyle(separatorStyle.CopyWith(color: separatorColor), new Text(":")),
            field(_minuteController, false),
        };
        if (!_use24HourFormat) fieldChildren.Add(BuildDayPeriodControl(context, local, defaults));
        var fields = new Row(
            mainAxisAlignment: MainAxisAlignment.Center,
            crossAxisAlignment: CrossAxisAlignment.Center,
            spacing: 8,
            children: fieldChildren);

        return new Form(
            key: _formKey,
            autovalidateMode: _autovalidateMode,
            child: new Padding(
                new Thickness(0, 20),
                new Column(
                    mainAxisSize: MainAxisSize.Min,
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    spacing: 24,
                    children:
                    [
                        new Padding(
                            new Thickness(8, 0),
                            new DefaultTextStyle(
                                local.HelpTextStyle ?? defaults.HelpTextStyle ?? Theme.Of(context).TextTheme.LabelSmall,
                                new Text(Current.HelpText ?? localizations.TimePickerInputHelpText))),
                        fields,
                    ])));
    }

    private Widget BuildHeader(
        BuildContext context,
        TimePickerThemeData local,
        TimePickerThemeData defaults,
        bool input)
    {
        var localizations = MaterialLocalizations.Of(context);
        var help = Current.HelpText ?? (input
            ? localizations.TimePickerInputHelpText
            : localizations.TimePickerDialHelpText);
        var selectedStyle = local.HourMinuteTextStyle ?? defaults.HourMinuteTextStyle
            ?? Theme.Of(context).TextTheme.HeadlineMedium.CopyWith(fontSize: 60);
        var separatorStyle = (local.TimeSelectorSeparatorTextStyle ?? defaults.TimeSelectorSeparatorTextStyle)
            ?.Resolve(MaterialState.None) ?? selectedStyle;

        var selectorChildren = new List<Widget>
        {
            BuildHeaderSegment(context, localizations.FormatHour(_selectedTime, _use24HourFormat), true, selectedStyle, local, defaults),
            new DefaultTextStyle(separatorStyle, new Text(":")),
            BuildHeaderSegment(context, localizations.FormatMinute(_selectedTime), false, selectedStyle, local, defaults),
        };
        if (!_use24HourFormat) selectorChildren.Add(BuildDayPeriodControl(context, local, defaults));

        return new Padding(
            new Thickness(0, 0, 0, 4),
            new Column(
                mainAxisSize: MainAxisSize.Min,
                crossAxisAlignment: CrossAxisAlignment.Start,
                spacing: 12,
                children:
                [
                    new DefaultTextStyle(
                        local.HelpTextStyle ?? defaults.HelpTextStyle ?? Theme.Of(context).TextTheme.LabelSmall,
                        new Text(help)),
                    new Row(
                        mainAxisSize: MainAxisSize.Min,
                        crossAxisAlignment: CrossAxisAlignment.Center,
                        spacing: 6,
                        children: selectorChildren),
                ]));
    }

    private Widget BuildHeaderSegment(
        BuildContext context,
        string label,
        bool hour,
        TextStyle style,
        TimePickerThemeData local,
        TimePickerThemeData defaults)
    {
        var selected = _selectingHour == hour;
        var states = selected ? MaterialState.Selected : MaterialState.None;
        var background = (local.HourMinuteColor ?? defaults.HourMinuteColor)?.Resolve(states);
        var foreground = (local.HourMinuteTextColor ?? defaults.HourMinuteTextColor)?.Resolve(states);
        var shape = local.HourMinuteShape ?? defaults.HourMinuteShape ?? ShapeBorder.RoundedRectangle(4);

        return new Semantics(
            label: hour ? MaterialLocalizations.Of(context).HourLabel : MaterialLocalizations.Of(context).MinuteLabel,
            flags: SemanticsFlags.IsButton | SemanticsFlags.IsEnabled | (selected ? SemanticsFlags.IsSelected : SemanticsFlags.None),
            onTap: () => SetState(() => _selectingHour = hour),
            child: new InkWell(
                onTap: () => SetState(() => _selectingHour = hour),
                borderRadius: shape.BorderRadius,
                child: new Container(
                    width: 96,
                    height: 72,
                    alignment: Alignment.Center,
                    decoration: new BoxDecoration(
                        Color: background,
                        Border: shape.Side,
                        BorderRadius: shape.BorderRadius,
                        Shape: shape.Shape),
                    child: new DefaultTextStyle(style.CopyWith(color: foreground), new Text(label)))));
    }

    private Widget BuildDayPeriodControl(
        BuildContext context,
        TimePickerThemeData local,
        TimePickerThemeData defaults)
    {
        var localizations = MaterialLocalizations.Of(context);
        var shape = local.DayPeriodShape ?? defaults.DayPeriodShape ?? ShapeBorder.RoundedRectangle(4);

        Widget button(DayPeriod period, string label)
        {
            var selected = _inputPeriod == period;
            var states = selected ? MaterialState.Selected : MaterialState.None;
            var background = (local.DayPeriodColor ?? defaults.DayPeriodColor)?.Resolve(states);
            var foreground = (local.DayPeriodTextColor ?? defaults.DayPeriodTextColor)?.Resolve(states);
            return new Expanded(
                new Semantics(
                    label: label,
                    flags: SemanticsFlags.IsButton | SemanticsFlags.IsEnabled | (selected ? SemanticsFlags.IsSelected : SemanticsFlags.None),
                    onTap: () => SetPeriod(period),
                    child: new InkWell(
                        onTap: () => SetPeriod(period),
                        child: new Container(
                            alignment: Alignment.Center,
                            color: background,
                            child: new DefaultTextStyle(
                                (local.DayPeriodTextStyle ?? defaults.DayPeriodTextStyle ?? Theme.Of(context).TextTheme.TitleMedium)
                                    .CopyWith(color: foreground),
                                new Text(label))))));
        }

        return new Container(
            width: 64,
            height: 72,
            decoration: new BoxDecoration(
                Border: local.DayPeriodBorderSide ?? defaults.DayPeriodBorderSide,
                BorderRadius: shape.BorderRadius),
            child: new Column(
                mainAxisSize: MainAxisSize.Max,
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children:
                [
                    button(DayPeriod.Am, localizations.AnteMeridiemAbbreviation),
                    button(DayPeriod.Pm, localizations.PostMeridiemAbbreviation),
                ]));
    }

    private Widget BuildActions(
        BuildContext context,
        TimePickerThemeData local,
        TimePickerThemeData defaults)
    {
        var theme = Theme.Of(context);
        var localizations = MaterialLocalizations.Of(context);
        var canSwitch = _entryMode is TimePickerEntryMode.Dial or TimePickerEntryMode.Input;
        Widget switcher = new SizedBox(width: 48, height: 48);
        if (canSwitch)
        {
            var dialMode = _entryMode == TimePickerEntryMode.Dial;
            switcher = new Tooltip(
                message: dialMode ? localizations.InputTimeModeButtonLabel : localizations.DialModeButtonLabel,
                child: new IconButton(
                    icon: dialMode
                        ? Current.SwitchToInputEntryModeIcon ?? new Icon(Icons.KeyboardOutlined)
                        : Current.SwitchToTimerEntryModeIcon ?? new Icon(Icons.AccessTime),
                    color: local.EntryModeIconColor ?? defaults.EntryModeIconColor,
                    onPressed: ToggleEntryMode));
        }

        return new Padding(
            new Thickness(theme.UseMaterial3 ? 0 : 4, 0),
            new Row(
                mainAxisAlignment: MainAxisAlignment.SpaceBetween,
                crossAxisAlignment: CrossAxisAlignment.Center,
                children:
                [
                    switcher,
                    new Row(
                        mainAxisSize: MainAxisSize.Min,
                        spacing: 8,
                        children:
                        [
                            new TextButton(
                                onPressed: () => Navigator.Pop(context),
                                style: local.CancelButtonStyle ?? defaults.CancelButtonStyle,
                                child: new Text(Current.CancelText ?? (theme.UseMaterial3
                                    ? localizations.CancelButtonLabel
                                    : localizations.CancelButtonLabel.ToUpperInvariant()))),
                            new TextButton(
                                onPressed: HandleOk,
                                style: local.ConfirmButtonStyle ?? defaults.ConfirmButtonStyle,
                                child: new Text(Current.ConfirmText ?? localizations.OkButtonLabel)),
                        ]),
                ]));
    }

    private void ToggleEntryMode()
    {
        var next = _entryMode switch
        {
            TimePickerEntryMode.Dial => TimePickerEntryMode.Input,
            TimePickerEntryMode.Input => TimePickerEntryMode.Dial,
            _ => throw new InvalidOperationException($"Cannot change entry mode from {_entryMode}.")
        };
        SetState(() =>
        {
            if (_entryMode == TimePickerEntryMode.Input)
            {
                _formKey.CurrentState?.Save();
                _autovalidateMode = AutovalidateMode.Disabled;
            }
            _entryMode = next;
            _leaveInitialInputEmpty = false;
            SyncInputControllers();
        });
        Current.OnEntryModeChanged?.Invoke(next);
    }

    private void HandleOk()
    {
        if (_entryMode is TimePickerEntryMode.Input or TimePickerEntryMode.InputOnly)
        {
            var form = _formKey.CurrentState;
            if (form is null || !form.Validate())
            {
                SetState(() => _autovalidateMode = AutovalidateMode.Always);
                return;
            }
            form.Save();
        }
        Navigator.Pop(Context, _selectedTime);
    }

    private string? ValidateTimePart(string? value, bool hour)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
            return Current.ErrorInvalidText ?? MaterialLocalizations.Of(Context).InvalidTimeLabel;
        var valid = hour
            ? _use24HourFormat ? number is >= 0 and <= 23 : number is >= 1 and <= 12
            : number is >= 0 and <= 59;
        return valid ? null : Current.ErrorInvalidText ?? MaterialLocalizations.Of(Context).InvalidTimeLabel;
    }

    private void SaveTimePart(string? value, bool hour)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number)) return;
        if (hour)
        {
            var resolved = _use24HourFormat
                ? number
                : number % 12 + (_inputPeriod == DayPeriod.Pm ? 12 : 0);
            _selectedTime = _selectedTime.Replacing(hour: resolved);
        }
        else
        {
            _selectedTime = _selectedTime.Replacing(minute: number);
        }
    }

    private void SetPeriod(DayPeriod period)
    {
        if (_inputPeriod == period) return;
        SetState(() =>
        {
            _inputPeriod = period;
            var hour = _selectedTime.HourOfPeriod % 12 + (period == DayPeriod.Pm ? 12 : 0);
            _selectedTime = _selectedTime.Replacing(hour: hour);
        });
    }

    private void SyncInputControllers()
    {
        if (!_initializedInputs || _leaveInitialInputEmpty) return;
        var localizations = MaterialLocalizations.Of(Context);
        _hourController.Text = localizations.FormatHour(_selectedTime, _use24HourFormat);
        _minuteController.Text = localizations.FormatMinute(_selectedTime);
    }
}

internal sealed class TimePickerDial : StatelessWidget
{
    private const double Extent = 256;

    public TimePickerDial(
        TimeOfDay selectedTime,
        bool selectingHour,
        bool use24HourFormat,
        Action<TimeOfDay, bool> onChanged,
        TimePickerThemeData theme,
        TimePickerThemeData defaults)
    {
        SelectedTime = selectedTime;
        SelectingHour = selectingHour;
        Use24HourFormat = use24HourFormat;
        OnChanged = onChanged;
        Theme = theme;
        Defaults = defaults;
    }

    public TimeOfDay SelectedTime { get; }
    public bool SelectingHour { get; }
    public bool Use24HourFormat { get; }
    public Action<TimeOfDay, bool> OnChanged { get; }
    public TimePickerThemeData Theme { get; }
    public TimePickerThemeData Defaults { get; }

    public override Widget Build(BuildContext context)
    {
        var children = new List<Widget>
        {
            new CustomPaint(
                painter: new TimeDialPainter(
                    SelectedTime,
                    SelectingHour,
                    Use24HourFormat,
                    Theme.DialBackgroundColor ?? Defaults.DialBackgroundColor ?? Colors.Transparent,
                    Theme.DialHandColor ?? Defaults.DialHandColor ?? ThemeData.Light.PrimaryColor),
                size: new Size(Extent, Extent))
        };

        foreach (var item in BuildLabels())
        {
            var selected = SelectingHour ? item.Value == SelectedTime.Hour : item.Value == SelectedTime.Minute;
            var states = selected ? MaterialState.Selected : MaterialState.None;
            var color = (Theme.DialTextColor ?? Defaults.DialTextColor)?.Resolve(states);
            var style = (Theme.DialTextStyle ?? Defaults.DialTextStyle ?? ThemeData.Light.TextTheme.BodyLarge)
                .CopyWith(color: color);
            children.Add(new Positioned(
                left: item.Position.X - 24,
                top: item.Position.Y - 24,
                width: 48,
                height: 48,
                child: new Semantics(
                    label: item.Label,
                    flags: SemanticsFlags.IsButton | SemanticsFlags.IsEnabled | (selected ? SemanticsFlags.IsSelected : SemanticsFlags.None),
                    onTap: () => SelectValue(item.Value, finished: true),
                    child: new GestureDetector(
                        behavior: HitTestBehavior.Translucent,
                        onTap: () => SelectValue(item.Value, finished: true),
                        child: new Center(child: new DefaultTextStyle(style, new Text(item.Label)))))));
        }

        return new Listener(
            behavior: HitTestBehavior.Opaque,
            onPointerDown: e => SelectFromPosition(e.LocalPosition, finished: false),
            onPointerMove: e => { if (e.Down) SelectFromPosition(e.LocalPosition, finished: false); },
            onPointerUp: e => SelectFromPosition(e.LocalPosition, finished: true),
            child: new SizedBox(width: Extent, height: Extent, child: new Stack(children: children)));
    }

    private IEnumerable<(int Value, string Label, Point Position)> BuildLabels()
    {
        if (!SelectingHour)
        {
            for (var index = 0; index < 12; index++)
            {
                var value = index * 5;
                yield return (value, value.ToString("00", CultureInfo.InvariantCulture), Position(index, 96));
            }
            yield break;
        }

        if (!Use24HourFormat)
        {
            for (var index = 0; index < 12; index++)
            {
                var hourOfPeriod = index == 0 ? 12 : index;
                var value = hourOfPeriod % 12 + (SelectedTime.Period == DayPeriod.Pm ? 12 : 0);
                yield return (value, hourOfPeriod.ToString(CultureInfo.InvariantCulture), Position(index, 96));
            }
            yield break;
        }

        for (var index = 0; index < 12; index++)
        {
            var inner = index == 0 ? 12 : index;
            var outer = index == 0 ? 0 : index + 12;
            yield return (outer, outer.ToString(CultureInfo.InvariantCulture), Position(index, 96));
            yield return (inner, inner.ToString(CultureInfo.InvariantCulture), Position(index, 62));
        }
    }

    private void SelectFromPosition(Point point, bool finished)
    {
        var dx = point.X - Extent / 2;
        var dy = point.Y - Extent / 2;
        var angle = Math.Atan2(dx, -dy);
        if (angle < 0) angle += Math.PI * 2;
        if (SelectingHour)
        {
            var index = (int)Math.Round(angle / (Math.PI * 2) * 12) % 12;
            int hour;
            if (Use24HourFormat)
            {
                var inner = Math.Sqrt(dx * dx + dy * dy) < 79;
                hour = inner ? (index == 0 ? 12 : index) : (index == 0 ? 0 : index + 12);
            }
            else
            {
                hour = index % 12 + (SelectedTime.Period == DayPeriod.Pm ? 12 : 0);
            }
            OnChanged(SelectedTime.Replacing(hour: hour), finished);
        }
        else
        {
            var minute = (int)Math.Round(angle / (Math.PI * 2) * 60) % 60;
            OnChanged(SelectedTime.Replacing(minute: minute), finished);
        }
    }

    private void SelectValue(int value, bool finished) => OnChanged(
        SelectingHour ? SelectedTime.Replacing(hour: value) : SelectedTime.Replacing(minute: value),
        finished);

    private static Point Position(int index, double radius)
    {
        var angle = index / 12.0 * Math.PI * 2 - Math.PI / 2;
        return new Point(Extent / 2 + Math.Cos(angle) * radius, Extent / 2 + Math.Sin(angle) * radius);
    }
}

internal sealed class TimeDialPainter : CustomPainter
{
    private readonly TimeOfDay _selectedTime;
    private readonly bool _selectingHour;
    private readonly bool _use24HourFormat;
    private readonly Color _background;
    private readonly Color _hand;

    public Color BackgroundColor => _background;
    public Color HandColor => _hand;

    public TimeDialPainter(
        TimeOfDay selectedTime,
        bool selectingHour,
        bool use24HourFormat,
        Color background,
        Color hand)
    {
        _selectedTime = selectedTime;
        _selectingHour = selectingHour;
        _use24HourFormat = use24HourFormat;
        _background = background;
        _hand = hand;
    }

    public override void Paint(PaintingContext context, Size size)
    {
        var center = new Point(size.Width / 2, size.Height / 2);
        var faceRadius = Math.Min(size.Width, size.Height) / 2;
        context.DrawCircle(new SolidColorBrush(_background), null, center, faceRadius);
        var value = _selectingHour ? _selectedTime.Hour % 12 : _selectedTime.Minute / 5.0;
        var angle = value / 12.0 * Math.PI * 2 - Math.PI / 2;
        var inner = _selectingHour && _use24HourFormat && _selectedTime.Hour is >= 1 and <= 12;
        var radius = inner ? 62 : 96;
        var endpoint = new Point(center.X + Math.Cos(angle) * radius, center.Y + Math.Sin(angle) * radius);
        var brush = new SolidColorBrush(_hand);
        context.DrawLine(new Pen(brush, 2), center, endpoint);
        context.DrawCircle(brush, null, endpoint, 24);
        context.DrawCircle(brush, null, center, 4);
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate) => oldDelegate is not TimeDialPainter old
        || old._selectedTime != _selectedTime
        || old._selectingHour != _selectingHour
        || old._use24HourFormat != _use24HourFormat
        || old._background != _background
        || old._hand != _hand;
}

public static class MaterialTimePickers
{
    public static Task<TimeOfDay?> ShowTimePicker(
        BuildContext context,
        TimeOfDay initialTime,
        TimePickerTransitionBuilder? builder = null,
        bool barrierDismissible = true,
        Color? barrierColor = null,
        string? barrierLabel = null,
        bool useRootNavigator = true,
        TimePickerEntryMode initialEntryMode = TimePickerEntryMode.Dial,
        string? cancelText = null,
        string? confirmText = null,
        string? helpText = null,
        string? errorInvalidText = null,
        string? hourLabelText = null,
        string? minuteLabelText = null,
        RouteSettings? routeSettings = null,
        EntryModeChangeCallback? onEntryModeChanged = null,
        Orientation? orientation = null,
        Widget? switchToInputEntryModeIcon = null,
        Widget? switchToTimerEntryModeIcon = null,
        bool emptyInitialInput = false)
    {
        Widget dialog = new TimePickerDialog(
            initialTime: initialTime,
            initialEntryMode: initialEntryMode,
            cancelText: cancelText,
            confirmText: confirmText,
            helpText: helpText,
            errorInvalidText: errorInvalidText,
            hourLabelText: hourLabelText,
            minuteLabelText: minuteLabelText,
            orientation: orientation,
            onEntryModeChanged: onEntryModeChanged,
            switchToInputEntryModeIcon: switchToInputEntryModeIcon,
            switchToTimerEntryModeIcon: switchToTimerEntryModeIcon,
            emptyInitialInput: emptyInitialInput);
        return MaterialDialogs.ShowDialog<TimeOfDay?>(
            context,
            routeContext => builder?.Invoke(routeContext, dialog) ?? dialog,
            barrierDismissible: barrierDismissible,
            barrierColor: barrierColor,
            barrierLabel: barrierLabel,
            useRootNavigator: useRootNavigator,
            routeSettings: routeSettings);
    }
}
