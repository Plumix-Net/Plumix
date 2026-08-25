using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Plumix.Gestures;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/time_picker.dart

public enum TimePickerEntryMode
{
    Dial,
    Input,
    DialOnly,
    InputOnly,
}

public delegate void EntryModeChangeCallback(TimePickerEntryMode mode);

public delegate Widget TimePickerTransitionBuilder(BuildContext context, Widget child);

internal enum HourMinuteMode
{
    Hour,
    Minute,
}

internal enum HourDialType
{
    TwentyFourHour,
    TwentyFourHourDoubleRing,
    TwelveHour,
}

internal enum TimePickerAspect
{
    Use24HourFormat,
    UseMaterial3,
    EntryMode,
    HourMinuteMode,
    OnHourMinuteModeChanged,
    OnHourDoubleTapped,
    OnMinuteDoubleTapped,
    HourDialType,
    SelectedTime,
    OnSelectedTimeChanged,
    Orientation,
    Theme,
    DefaultTheme,
}

internal static class TimePickerConstants
{
    internal static readonly TimeSpan DialogSizeAnimationDuration = TimeSpan.FromMilliseconds(200);
    internal static readonly TimeSpan DialAnimateDuration = TimeSpan.FromMilliseconds(200);
    internal static readonly TimeSpan VibrateCommitDelay = TimeSpan.FromMilliseconds(100);
    internal const double TwoPi = 2 * Math.PI;
    internal const double HeaderLandscapeWidth = 216;
    internal const double InnerDialOffset = 28;
    internal const double DialMinRadius = 50;
    internal const double DialPadding = 28;
}

internal sealed class TimePickerModel : InheritedModel<TimePickerAspect>
{
    public TimePickerModel(
        TimePickerEntryMode entryMode,
        HourMinuteMode hourMinuteMode,
        Action<HourMinuteMode> onHourMinuteModeChanged,
        Action onHourDoubleTapped,
        Action onMinuteDoubleTapped,
        TimeOfDay selectedTime,
        Action<TimeOfDay> onSelectedTimeChanged,
        bool use24HourFormat,
        bool useMaterial3,
        HourDialType hourDialType,
        Orientation orientation,
        TimePickerThemeData theme,
        TimePickerDefaults defaultTheme,
        Widget child,
        Key? key = null) : base(key)
    {
        EntryMode = entryMode;
        HourMinuteMode = hourMinuteMode;
        OnHourMinuteModeChanged = onHourMinuteModeChanged;
        OnHourDoubleTapped = onHourDoubleTapped;
        OnMinuteDoubleTapped = onMinuteDoubleTapped;
        SelectedTime = selectedTime;
        OnSelectedTimeChanged = onSelectedTimeChanged;
        Use24HourFormat = use24HourFormat;
        UseMaterial3 = useMaterial3;
        HourDialType = hourDialType;
        Orientation = orientation;
        Theme = theme;
        DefaultTheme = defaultTheme;
        Child = child;
    }

    public TimePickerEntryMode EntryMode { get; }
    public HourMinuteMode HourMinuteMode { get; }
    public Action<HourMinuteMode> OnHourMinuteModeChanged { get; }
    public Action OnHourDoubleTapped { get; }
    public Action OnMinuteDoubleTapped { get; }
    public TimeOfDay SelectedTime { get; }
    public Action<TimeOfDay> OnSelectedTimeChanged { get; }
    public bool Use24HourFormat { get; }
    public bool UseMaterial3 { get; }
    public HourDialType HourDialType { get; }
    public Orientation Orientation { get; }
    public TimePickerThemeData Theme { get; }
    public TimePickerDefaults DefaultTheme { get; }
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    private static TimePickerModel Of(BuildContext context, TimePickerAspect aspect) =>
        InheritFrom<TimePickerModel>(context, aspect)
        ?? throw new InvalidOperationException("No TimePickerModel found in context.");

    public static TimePickerEntryMode EntryModeOf(BuildContext context) =>
        Of(context, TimePickerAspect.EntryMode).EntryMode;

    public static HourMinuteMode HourMinuteModeOf(BuildContext context) =>
        Of(context, TimePickerAspect.HourMinuteMode).HourMinuteMode;

    public static TimeOfDay SelectedTimeOf(BuildContext context) =>
        Of(context, TimePickerAspect.SelectedTime).SelectedTime;

    public static bool Use24HourFormatOf(BuildContext context) =>
        Of(context, TimePickerAspect.Use24HourFormat).Use24HourFormat;

    public static bool UseMaterial3Of(BuildContext context) =>
        Of(context, TimePickerAspect.UseMaterial3).UseMaterial3;

    public static HourDialType HourDialTypeOf(BuildContext context) =>
        Of(context, TimePickerAspect.HourDialType).HourDialType;

    public static Orientation OrientationOf(BuildContext context) =>
        Of(context, TimePickerAspect.Orientation).Orientation;

    public static TimePickerThemeData ThemeOf(BuildContext context) =>
        Of(context, TimePickerAspect.Theme).Theme;

    public static TimePickerDefaults DefaultThemeOf(BuildContext context) =>
        Of(context, TimePickerAspect.DefaultTheme).DefaultTheme;

    public static Action OnHourDoubleTappedOf(BuildContext context) =>
        Of(context, TimePickerAspect.OnHourDoubleTapped).OnHourDoubleTapped;

    public static Action OnMinuteDoubleTappedOf(BuildContext context) =>
        Of(context, TimePickerAspect.OnMinuteDoubleTapped).OnMinuteDoubleTapped;

    public static void SetSelectedTime(BuildContext context, TimeOfDay value) =>
        Of(context, TimePickerAspect.OnSelectedTimeChanged).OnSelectedTimeChanged(value);

    public static void SetHourMinuteMode(BuildContext context, HourMinuteMode value) =>
        Of(context, TimePickerAspect.OnHourMinuteModeChanged).OnHourMinuteModeChanged(value);

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        var old = (TimePickerModel)oldWidget;
        return Use24HourFormat != old.Use24HourFormat
               || UseMaterial3 != old.UseMaterial3
               || EntryMode != old.EntryMode
               || HourMinuteMode != old.HourMinuteMode
               || OnHourMinuteModeChanged != old.OnHourMinuteModeChanged
               || OnHourDoubleTapped != old.OnHourDoubleTapped
               || OnMinuteDoubleTapped != old.OnMinuteDoubleTapped
               || HourDialType != old.HourDialType
               || SelectedTime != old.SelectedTime
               || OnSelectedTimeChanged != old.OnSelectedTimeChanged
               || Orientation != old.Orientation
               || !Equals(Theme, old.Theme)
               || !ReferenceEquals(DefaultTheme, old.DefaultTheme);
    }

    protected override bool UpdateShouldNotifyDependent(
        InheritedModel<TimePickerAspect> oldWidget,
        IReadOnlySet<TimePickerAspect> dependencies)
    {
        var old = (TimePickerModel)oldWidget;
        if (Use24HourFormat != old.Use24HourFormat
            && dependencies.Contains(TimePickerAspect.Use24HourFormat)) return true;
        if (UseMaterial3 != old.UseMaterial3
            && dependencies.Contains(TimePickerAspect.UseMaterial3)) return true;
        if (EntryMode != old.EntryMode && dependencies.Contains(TimePickerAspect.EntryMode)) return true;
        if (HourMinuteMode != old.HourMinuteMode
            && dependencies.Contains(TimePickerAspect.HourMinuteMode)) return true;
        if (OnHourMinuteModeChanged != old.OnHourMinuteModeChanged
            && dependencies.Contains(TimePickerAspect.OnHourMinuteModeChanged)) return true;
        if (OnHourDoubleTapped != old.OnHourDoubleTapped
            && dependencies.Contains(TimePickerAspect.OnHourDoubleTapped)) return true;
        if (OnMinuteDoubleTapped != old.OnMinuteDoubleTapped
            && dependencies.Contains(TimePickerAspect.OnMinuteDoubleTapped)) return true;
        if (HourDialType != old.HourDialType
            && dependencies.Contains(TimePickerAspect.HourDialType)) return true;
        if (SelectedTime != old.SelectedTime
            && dependencies.Contains(TimePickerAspect.SelectedTime)) return true;
        if (OnSelectedTimeChanged != old.OnSelectedTimeChanged
            && dependencies.Contains(TimePickerAspect.OnSelectedTimeChanged)) return true;
        if (Orientation != old.Orientation
            && dependencies.Contains(TimePickerAspect.Orientation)) return true;
        if (!Equals(Theme, old.Theme) && dependencies.Contains(TimePickerAspect.Theme)) return true;
        if (!ReferenceEquals(DefaultTheme, old.DefaultTheme)
            && dependencies.Contains(TimePickerAspect.DefaultTheme)) return true;
        return false;
    }
}

internal sealed class DialTimePickerHeader : StatelessWidget
{
    public DialTimePickerHeader(string helpText, Key? key = null) : base(key)
    {
        HelpText = helpText;
    }

    public string HelpText { get; }

    public override Widget Build(BuildContext context)
    {
        var localizations = MaterialLocalizations.Of(context);
        var timeOfDayFormat = localizations.TimeOfDayFormat(TimePickerModel.Use24HourFormatOf(context));
        var orientation = TimePickerModel.OrientationOf(context);
        var theme = TimePickerModel.ThemeOf(context);
        var defaultTheme = TimePickerModel.DefaultThemeOf(context);
        bool useMaterial3 = TimePickerModel.UseMaterial3Of(context);
        var hourDialType = TimePickerModel.HourDialTypeOf(context);
        double dayPeriodHeight = orientation == Orientation.Portrait
            ? defaultTheme.DayPeriodPortraitSize.Height
            : defaultTheme.DayPeriodLandscapeSize.Height;
        double minInteractiveVerticalPadding = Math.Max(0, WidgetConstants.MinInteractiveDimension - dayPeriodHeight);
        var helpStyle = theme.HelpTextStyle ?? defaultTheme.HelpTextStyle;

        Widget selector = new Row(
            crossAxisAlignment: CrossAxisAlignment.Center,
            textDirection: TextDirection.Ltr,
            children:
            [
                new Expanded(new DialHourControl()),
                new TimeSelectorSeparator(timeOfDayFormat),
                new Expanded(new DialMinuteControl()),
            ]);

        Widget label = new Text(HelpText, style: helpStyle);

        if (orientation == Orientation.Portrait)
        {
            var rowChildren = new List<Widget> { new Expanded(selector) };
            if (hourDialType == HourDialType.TwelveHour) rowChildren.Add(new DayPeriodControl());

            return new Semantics(
                label: localizations.FormatTimeOfDay(
                    TimePickerModel.SelectedTimeOf(context),
                    MediaQuery.MaybeAlwaysUse24HourFormatOf(context) ?? false),
                child: new Column(
                    mainAxisSize: MainAxisSize.Min,
                    crossAxisAlignment: CrossAxisAlignment.Start,
                    children:
                    [
                        new Padding(
                            EdgeInsetsGeometry.DirectionalOnly(
                                bottom: (useMaterial3 ? 20 : 24) - (minInteractiveVerticalPadding / 2)),
                            label),
                        new Row(
                            textDirection: timeOfDayFormat == TimeOfDayFormat.ASpaceHColonMm
                                ? TextDirection.Rtl
                                : TextDirection.Ltr,
                            spacing: 12,
                            children: rowChildren),
                    ]));
        }

        var columnChildren = new List<Widget> { selector };
        if (hourDialType == HourDialType.TwelveHour) columnChildren.Add(new DayPeriodControl());

        return new Semantics(
            label: localizations.FormatTimeOfDay(
                TimePickerModel.SelectedTimeOf(context),
                MediaQuery.MaybeAlwaysUse24HourFormatOf(context) ?? false),
            child: new SizedBox(
                width: TimePickerConstants.HeaderLandscapeWidth,
                child: new Stack(
                    alignment: AlignmentDirectional.TopStart,
                    children:
                    [
                        label,
                        new Column(
                            verticalDirection: timeOfDayFormat == TimeOfDayFormat.ASpaceHColonMm
                                ? VerticalDirection.Up
                                : VerticalDirection.Down,
                            mainAxisAlignment: MainAxisAlignment.Center,
                            crossAxisAlignment: CrossAxisAlignment.Start,
                            spacing: Math.Max(0, 16 - (minInteractiveVerticalPadding / 2)),
                            children: columnChildren),
                    ])));
    }
}

internal sealed class DialTimeSelectorControl : StatelessWidget
{
    public DialTimeSelectorControl(
        string text,
        Action onTap,
        Action onDoubleTap,
        bool isSelected,
        Key? key = null) : base(key)
    {
        Text = text;
        OnTap = onTap;
        OnDoubleTap = onDoubleTap;
        IsSelected = isSelected;
    }

    public string Text { get; }
    public Action OnTap { get; }
    public Action OnDoubleTap { get; }
    public bool IsSelected { get; }

    public override Widget Build(BuildContext context)
    {
        var theme = TimePickerModel.ThemeOf(context);
        var defaultTheme = TimePickerModel.DefaultThemeOf(context);
        var states = IsSelected ? MaterialState.Selected : MaterialState.None;
        var backgroundColor = (theme.HourMinuteColor ?? defaultTheme.HourMinuteColor).Resolve(states);
        var textColor = (theme.HourMinuteTextColor ?? defaultTheme.HourMinuteTextColor).Resolve(states);
        var effectiveStyle = (theme.HourMinuteTextStyle ?? defaultTheme.HourMinuteTextStyle)
            .CopyWith(color: textColor);

        return new SizedBox(
            height: defaultTheme.HourMinuteSize.Height,
            child: new Plumix.Material.Material(
                color: backgroundColor,
                clipBehavior: Clip.AntiAlias,
                shape: theme.HourMinuteShape ?? defaultTheme.HourMinuteShape,
                child: new InkWell(
                    onTap: OnTap,
                    onDoubleTap: IsSelected ? OnDoubleTap : null,
                    child: new Center(
                        child: new Text(Text, style: effectiveStyle, textScaler: TextScaler.NoScaling)))));
    }
}

internal sealed class DialHourControl : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        var localizations = MaterialLocalizations.Of(context);
        bool alwaysUse24HourFormat = MediaQuery.MaybeAlwaysUse24HourFormatOf(context) ?? false;
        var hourDialType = TimePickerModel.HourDialTypeOf(context);
        var selectedTime = TimePickerModel.SelectedTimeOf(context);
        string formattedHour = localizations.FormatHour(
            selectedTime,
            TimePickerModel.Use24HourFormatOf(context));

        TimeOfDay HoursFromSelected(int hoursToAdd) => hourDialType switch
        {
            HourDialType.TwentyFourHour or HourDialType.TwentyFourHourDoubleRing =>
                selectedTime.Replacing(
                    hour: (selectedTime.Hour + hoursToAdd + TimeOfDay.HoursPerDay) % TimeOfDay.HoursPerDay),
            _ => selectedTime.Replacing(
                hour: selectedTime.PeriodOffset
                      + ((selectedTime.HourOfPeriod + hoursToAdd + TimeOfDay.HoursPerPeriod)
                         % TimeOfDay.HoursPerPeriod)),
        };

        var nextHour = HoursFromSelected(1);
        var previousHour = HoursFromSelected(-1);

        return new Semantics(
            value: $"{localizations.TimePickerHourModeAnnouncement} {formattedHour}",
            increasedValue: localizations.FormatHour(nextHour, alwaysUse24HourFormat),
            decreasedValue: localizations.FormatHour(previousHour, alwaysUse24HourFormat),
            onIncrease: () => TimePickerModel.SetSelectedTime(context, nextHour),
            onDecrease: () => TimePickerModel.SetSelectedTime(context, previousHour),
            child: new ExcludeSemantics(new DialTimeSelectorControl(
                isSelected: TimePickerModel.HourMinuteModeOf(context) == HourMinuteMode.Hour,
                text: formattedHour,
                onTap: () => TimePickerModel.SetHourMinuteMode(context, HourMinuteMode.Hour),
                onDoubleTap: TimePickerModel.OnHourDoubleTappedOf(context))));
    }
}

internal sealed class DialMinuteControl : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        var localizations = MaterialLocalizations.Of(context);
        var selectedTime = TimePickerModel.SelectedTimeOf(context);
        string formattedMinute = localizations.FormatMinute(selectedTime);
        var nextMinute = selectedTime.Replacing(
            minute: (selectedTime.Minute + 1) % TimeOfDay.MinutesPerHour);
        var previousMinute = selectedTime.Replacing(
            minute: (selectedTime.Minute - 1 + TimeOfDay.MinutesPerHour) % TimeOfDay.MinutesPerHour);

        return new Semantics(
            value: $"{localizations.TimePickerMinuteModeAnnouncement} {formattedMinute}",
            increasedValue: localizations.FormatMinute(nextMinute),
            decreasedValue: localizations.FormatMinute(previousMinute),
            onIncrease: () => TimePickerModel.SetSelectedTime(context, nextMinute),
            onDecrease: () => TimePickerModel.SetSelectedTime(context, previousMinute),
            child: new ExcludeSemantics(new DialTimeSelectorControl(
                isSelected: TimePickerModel.HourMinuteModeOf(context) == HourMinuteMode.Minute,
                text: formattedMinute,
                onTap: () => TimePickerModel.SetHourMinuteMode(context, HourMinuteMode.Minute),
                onDoubleTap: TimePickerModel.OnMinuteDoubleTappedOf(context))));
    }
}

internal sealed class TimeSelectorSeparator : StatelessWidget
{
    public TimeSelectorSeparator(TimeOfDayFormat timeOfDayFormat, Key? key = null) : base(key)
    {
        Format = timeOfDayFormat;
    }

    public TimeOfDayFormat Format { get; }

    internal static string SeparatorFor(TimeOfDayFormat format) => format switch
    {
        TimeOfDayFormat.HHDotMm => ".",
        TimeOfDayFormat.FrenchCanadian => "h",
        _ => ":",
    };

    public override Widget Build(BuildContext context)
    {
        var theme = TimePickerTheme.Of(context);
        bool useMaterial3 = Theme.Of(context).UseMaterial3;
        var entryMode = TimePickerModel.EntryModeOf(context);
        TimePickerDefaults defaultTheme = useMaterial3
            ? new TimePickerDefaultsM3(context, entryMode)
            : new TimePickerDefaultsM2(context);
        var states = MaterialState.None;
        var separatorColor = (theme.TimeSelectorSeparatorColor ?? defaultTheme.TimeSelectorSeparatorColor)
            ?.Resolve(states) ?? (theme.HourMinuteTextColor ?? defaultTheme.HourMinuteTextColor).Resolve(states);
        var separatorStyle = (theme.TimeSelectorSeparatorTextStyle ?? defaultTheme.TimeSelectorSeparatorTextStyle)
            ?.Resolve(states) ?? theme.HourMinuteTextStyle ?? defaultTheme.HourMinuteTextStyle;
        double height = entryMode is TimePickerEntryMode.Dial or TimePickerEntryMode.DialOnly
            ? defaultTheme.HourMinuteSize.Height
            : defaultTheme.HourMinuteInputSize.Height;

        return new ExcludeSemantics(new SizedBox(
            width: Format == TimeOfDayFormat.FrenchCanadian ? 36 : 24,
            height: height,
            child: new Center(child: new Text(
                SeparatorFor(Format),
                style: separatorStyle!.CopyWith(color: separatorColor, height: 1.0),
                textScaler: TextScaler.NoScaling))));
    }
}

internal sealed class DayPeriodControl : StatelessWidget
{
    public DayPeriodControl(Action<TimeOfDay>? onPeriodChanged = null, Key? key = null) : base(key)
    {
        OnPeriodChanged = onPeriodChanged;
    }

    public Action<TimeOfDay>? OnPeriodChanged { get; }

    private void TogglePeriod(BuildContext context)
    {
        var selectedTime = TimePickerModel.SelectedTimeOf(context);
        int newHour = (selectedTime.Hour + TimeOfDay.HoursPerPeriod) % TimeOfDay.HoursPerDay;
        var newTime = selectedTime.Replacing(hour: newHour);
        if (OnPeriodChanged is not null) OnPeriodChanged(newTime);
        else TimePickerModel.SetSelectedTime(context, newTime);
    }

    public override Widget Build(BuildContext context)
    {
        var localizations = MaterialLocalizations.Of(context);
        var theme = TimePickerModel.ThemeOf(context);
        var defaultTheme = TimePickerModel.DefaultThemeOf(context);
        var entryMode = TimePickerModel.EntryModeOf(context);
        var selectedTime = TimePickerModel.SelectedTimeOf(context);
        bool dialMode = entryMode is TimePickerEntryMode.Dial or TimePickerEntryMode.DialOnly;
        var orientation = dialMode ? TimePickerModel.OrientationOf(context) : Orientation.Portrait;
        var dayPeriodSize = dialMode
            ? orientation == Orientation.Portrait
                ? defaultTheme.DayPeriodPortraitSize
                : defaultTheme.DayPeriodLandscapeSize
            : defaultTheme.DayPeriodInputSize;
        bool amSelected = selectedTime.Period == DayPeriod.Am;
        bool pmSelected = !amSelected;

        var resolvedShape = (theme.DayPeriodShape ?? defaultTheme.DayPeriodShape)
            .CopyWith(side: theme.DayPeriodBorderSide ?? defaultTheme.DayPeriodBorderSide);
        var amShape = resolvedShape;
        var pmShape = resolvedShape;
        if (resolvedShape is RoundedRectangleBorder rounded
            && rounded.BorderRadius.Directional == default)
        {
            var radius = rounded.BorderRadius.Physical;
            amShape = orientation == Orientation.Portrait
                ? rounded with
                {
                    BorderRadius = Plumix.Rendering.BorderRadius.Only(
                        topLeft: radius.TopLeft,
                        topRight: radius.TopRight),
                }
                : rounded with
                {
                    BorderRadius = Plumix.Rendering.BorderRadius.Only(
                        topLeft: radius.TopLeft,
                        bottomLeft: radius.BottomLeft),
                };
            pmShape = orientation == Orientation.Portrait
                ? rounded with
                {
                    BorderRadius = Plumix.Rendering.BorderRadius.Only(
                        bottomLeft: radius.BottomLeft,
                        bottomRight: radius.BottomRight),
                }
                : rounded with
                {
                    BorderRadius = Plumix.Rendering.BorderRadius.Only(
                        topRight: radius.TopRight,
                        bottomRight: radius.BottomRight),
                };
        }

        void SetAm()
        {
            if (selectedTime.Period == DayPeriod.Am) return;
            TogglePeriod(context);
        }

        void SetPm()
        {
            if (selectedTime.Period == DayPeriod.Pm) return;
            TogglePeriod(context);
        }

        if (orientation == Orientation.Portrait)
        {
            var minInteractiveSize = new Size(
                dayPeriodSize.Width,
                Math.Max(dayPeriodSize.Height, 2 * WidgetConstants.MinInteractiveDimension));
            double inset = (minInteractiveSize.Height - dayPeriodSize.Height) / 2;
            return new DayPeriodInputPadding(
                minSize: minInteractiveSize,
                orientation: Orientation.Portrait,
                child: new SizedBox(
                    width: minInteractiveSize.Width,
                    height: minInteractiveSize.Height,
                    child: new Column(children:
                    [
                        new Expanded(new AmPmButton(
                            onPressed: SetAm,
                            selected: amSelected,
                            label: localizations.AnteMeridiemAbbreviation,
                            padding: new Thickness(0, inset, 0, 0),
                            shape: amShape)),
                        new Expanded(new AmPmButton(
                            onPressed: SetPm,
                            selected: pmSelected,
                            label: localizations.PostMeridiemAbbreviation,
                            padding: new Thickness(0, 0, 0, inset),
                            shape: pmShape)),
                    ])));
        }

        var landscapeMinSize = new Size(
            dayPeriodSize.Width,
            Math.Max(dayPeriodSize.Height, WidgetConstants.MinInteractiveDimension));
        double landscapeInset = (landscapeMinSize.Height - dayPeriodSize.Height) / 2;
        return new DayPeriodInputPadding(
            minSize: landscapeMinSize,
            orientation: Orientation.Landscape,
            child: new SizedBox(
                height: landscapeMinSize.Height,
                child: new Row(children:
                [
                    new Expanded(new AmPmButton(
                        onPressed: SetAm,
                        selected: amSelected,
                        label: localizations.AnteMeridiemAbbreviation,
                        padding: new Thickness(0, landscapeInset),
                        shape: amShape)),
                    new Expanded(new AmPmButton(
                        onPressed: SetPm,
                        selected: pmSelected,
                        label: localizations.PostMeridiemAbbreviation,
                        padding: new Thickness(0, landscapeInset),
                        shape: pmShape)),
                ])));
    }
}

internal sealed class AmPmButton : StatelessWidget
{
    public AmPmButton(
        Action onPressed,
        bool selected,
        string label,
        Thickness padding,
        OutlinedBorder shape,
        Key? key = null) : base(key)
    {
        OnPressed = onPressed;
        Selected = selected;
        Label = label;
        Padding = padding;
        Shape = shape;
    }

    public Action OnPressed { get; }
    public bool Selected { get; }
    public string Label { get; }
    public Thickness Padding { get; }
    public OutlinedBorder Shape { get; }

    public override Widget Build(BuildContext context)
    {
        var theme = TimePickerModel.ThemeOf(context);
        var defaultTheme = TimePickerModel.DefaultThemeOf(context);
        var states = Selected ? MaterialState.Selected : MaterialState.None;
        var backgroundColor = (theme.DayPeriodColor ?? defaultTheme.DayPeriodColor).Resolve(states);
        var textColor = (theme.DayPeriodTextColor ?? defaultTheme.DayPeriodTextColor).Resolve(states);
        var textStyle = (theme.DayPeriodTextStyle ?? defaultTheme.DayPeriodTextStyle)
            .CopyWith(color: textColor);
        bool isIOS = Theme.Of(context).Platform == TargetPlatform.IOS;

        return new Semantics(
            selected: isIOS ? Selected : null,
            @checked: isIOS ? null : Selected,
            flags: SemanticsFlags.IsInMutuallyExclusiveGroup | SemanticsFlags.IsButton,
            child: new Padding(
                Padding,
                new Plumix.Material.Material(
                    clipBehavior: Clip.AntiAlias,
                    color: backgroundColor,
                    shape: Shape,
                    child: new InkWell(
                        onTap: OnPressed,
                        child: new Center(child: new Text(
                            Label,
                            style: textStyle,
                            textScaler: TextScaler
                                .Linear(MediaQuery.MaybeTextScaleFactorOf(context) ?? 1.0)
                                .Clamp(maxScaleFactor: 2.0)))))));
    }
}

internal sealed class DayPeriodInputPadding : SingleChildRenderObjectWidget
{
    public DayPeriodInputPadding(Size minSize, Orientation orientation, Widget child) : base(child)
    {
        MinSize = minSize;
        Orientation = orientation;
    }

    public Size MinSize { get; }
    public Orientation Orientation { get; }

    internal override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderDayPeriodInputPadding(MinSize, Orientation);

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var padding = (RenderDayPeriodInputPadding)renderObject;
        padding.MinSize = MinSize;
        padding.Orientation = Orientation;
    }
}

internal sealed class RenderDayPeriodInputPadding : RenderProxyBox
{
    private Size _minSize;
    private Orientation _orientation;

    public RenderDayPeriodInputPadding(Size minSize, Orientation orientation)
    {
        _minSize = minSize;
        _orientation = orientation;
    }

    public Size MinSize
    {
        get => _minSize;
        set
        {
            if (_minSize == value) return;
            _minSize = value;
            MarkNeedsLayout();
        }
    }

    public Orientation Orientation
    {
        get => _orientation;
        set
        {
            if (_orientation == value) return;
            _orientation = value;
            MarkNeedsLayout();
        }
    }

    protected override double ComputeMinIntrinsicWidth(double height) =>
        Child is null ? 0.0 : Math.Max(Child.GetMinIntrinsicWidth(height), MinSize.Width);

    protected override double ComputeMaxIntrinsicWidth(double height) =>
        Child is null ? 0.0 : Math.Max(Child.GetMaxIntrinsicWidth(height), MinSize.Width);

    protected override double ComputeMinIntrinsicHeight(double width) =>
        Child is null ? 0.0 : Math.Max(Child.GetMinIntrinsicHeight(width), MinSize.Height);

    protected override double ComputeMaxIntrinsicHeight(double width) =>
        Child is null ? 0.0 : Math.Max(Child.GetMaxIntrinsicHeight(width), MinSize.Height);

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        if (Child is null) return new Size();
        var childSize = Child.GetDryLayout(constraints);
        return constraints.Constrain(new Size(
            Math.Max(childSize.Width, MinSize.Width),
            Math.Max(childSize.Height, MinSize.Height)));
    }

    protected override void PerformLayout()
    {
        if (Child is null)
        {
            Size = new Size();
            return;
        }

        Child.Layout(Constraints, parentUsesSize: true);
        Size = Constraints.Constrain(new Size(
            Math.Max(Child.Size.Width, MinSize.Width),
            Math.Max(Child.Size.Height, MinSize.Height)));
        ((BoxParentData)Child.parentData!).offset = new Point(
            (Size.Width - Child.Size.Width) / 2.0,
            (Size.Height - Child.Size.Height) / 2.0);
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        if (Child is null) return null;
        double? childBaseline = Child.GetDryBaseline(constraints, baseline);
        if (!childBaseline.HasValue) return null;
        var outerSize = ComputeDryLayout(constraints);
        var childSize = Child.GetDryLayout(constraints);
        return childBaseline.Value + ((outerSize.Height - childSize.Height) / 2.0);
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        if (base.HitTest(result, position)) return true;
        if (Child is null) return false;
        if (position.X < 0
            || position.X > Math.Max(Child.Size.Width, MinSize.Width)
            || position.Y < 0
            || position.Y > Math.Max(Child.Size.Height, MinSize.Height))
        {
            return false;
        }

        var center = new Point(Child.Size.Width / 2.0, Child.Size.Height / 2.0);
        var newPosition = Orientation switch
        {
            Orientation.Portrait when position.Y > center.Y => new Point(center.X, center.Y + 1),
            Orientation.Portrait => new Point(center.X, center.Y - 1),
            _ when position.X > center.X => new Point(center.X + 1, center.Y),
            _ => new Point(center.X - 1, center.Y),
        };
        return Child.HitTest(result, newPosition);
    }
}

internal sealed class TimePickerInput : StatefulWidget
{
    public TimePickerInput(
        TimeOfDay initialSelectedTime,
        string? errorInvalidText,
        string? hourLabelText,
        string? minuteLabelText,
        string helpText,
        bool? autofocusHour,
        bool? autofocusMinute,
        bool emptyInitialTime,
        Key? key = null) : base(key)
    {
        InitialSelectedTime = initialSelectedTime;
        ErrorInvalidText = errorInvalidText;
        HourLabelText = hourLabelText;
        MinuteLabelText = minuteLabelText;
        HelpText = helpText;
        AutofocusHour = autofocusHour;
        AutofocusMinute = autofocusMinute;
        EmptyInitialTime = emptyInitialTime;
    }

    public TimeOfDay InitialSelectedTime { get; }
    public string? ErrorInvalidText { get; }
    public string? HourLabelText { get; }
    public string? MinuteLabelText { get; }
    public string HelpText { get; }
    public bool? AutofocusHour { get; }
    public bool? AutofocusMinute { get; }
    public bool EmptyInitialTime { get; }

    public override State CreateState() => new TimePickerInputState();
}

internal sealed class TimePickerInputState : State
{
    private TimeOfDay _selectedTime;
    private bool _hourHasError;
    private bool _minuteHasError;

    private TimePickerInput Current => (TimePickerInput)StateWidget;

    public override void InitState()
    {
        _selectedTime = Current.InitialSelectedTime;
    }

    private int? ParseHour(string? value)
    {
        if (value is null) return null;
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int newHour)) return null;
        if (MediaQuery.MaybeAlwaysUse24HourFormatOf(Context) ?? false)
        {
            return newHour is >= 0 and < TimeOfDay.HoursPerDay ? newHour : null;
        }

        if (newHour is <= 0 or > TimeOfDay.HoursPerPeriod) return null;
        if ((_selectedTime.Period == DayPeriod.Pm && newHour != 12)
            || (_selectedTime.Period == DayPeriod.Am && newHour == 12))
        {
            newHour = (newHour + TimeOfDay.HoursPerPeriod) % TimeOfDay.HoursPerDay;
        }

        return newHour;
    }

    private static int? ParseMinute(string? value)
    {
        if (value is null) return null;
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int newMinute)) return null;
        return newMinute is >= 0 and < TimeOfDay.MinutesPerHour ? newMinute : null;
    }

    private string? ValidateHour(string? value)
    {
        int? newHour = ParseHour(value);
        SetState(() => _hourHasError = newHour is null);
        // Return an empty error string so the field paints its error state without an inline message.
        return newHour is null ? string.Empty : null;
    }

    private string? ValidateMinute(string? value)
    {
        int? newMinute = ParseMinute(value);
        SetState(() => _minuteHasError = newMinute is null);
        return newMinute is null ? string.Empty : null;
    }

    private void HandleHourSavedSubmitted(string? value)
    {
        int? newHour = ParseHour(value);
        if (newHour is null) return;
        _selectedTime = new TimeOfDay(newHour.Value, _selectedTime.Minute);
        TimePickerModel.SetSelectedTime(Context, _selectedTime);
        FocusManager.Instance.FocusNext();
    }

    private void HandleHourChanged(string value)
    {
        int? newHour = ParseHour(value);
        if (newHour is not null && value.Length == 2) FocusManager.Instance.FocusNext();
    }

    private void HandleMinuteSavedSubmitted(string? value)
    {
        int? newMinute = ParseMinute(value);
        if (newMinute is null) return;
        _selectedTime = new TimeOfDay(_selectedTime.Hour, newMinute.Value);
        TimePickerModel.SetSelectedTime(Context, _selectedTime);
        FocusManager.Instance.PrimaryFocus?.Unfocus();
    }

    private void HandleDayPeriodChanged(TimeOfDay value)
    {
        _selectedTime = value;
        TimePickerModel.SetSelectedTime(Context, value);
    }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var localizations = MaterialLocalizations.Of(context);
        var pickerTheme = TimePickerModel.ThemeOf(context);
        var defaultTheme = TimePickerModel.DefaultThemeOf(context);
        bool useMaterial3 = theme.UseMaterial3;
        var timeOfDayFormat = localizations.TimeOfDayFormat(
            MediaQuery.MaybeAlwaysUse24HourFormatOf(context) ?? false);
        bool use24HourDials = TimeOfDay.HourFormatOf(timeOfDayFormat) != HourFormat.H12;
        double minInteractiveVerticalPadding = Math.Max(
            0,
            (2 * WidgetConstants.MinInteractiveDimension) - defaultTheme.DayPeriodInputSize.Height);
        var hourStyle = pickerTheme.HourMinuteTextStyle ?? defaultTheme.HourMinuteTextStyle;

        var rowChildren = new List<Widget>();
        if (!use24HourDials && timeOfDayFormat == TimeOfDayFormat.ASpaceHColonMm)
        {
            rowChildren.Add(new Padding(
                EdgeInsetsGeometry.DirectionalOnly(end: 12),
                new DayPeriodControl(HandleDayPeriodChanged)));
        }

        rowChildren.Add(new Expanded(new Padding(
            EdgeInsetsGeometry.DirectionalOnly(top: minInteractiveVerticalPadding / 2),
            new Row(
                crossAxisAlignment: CrossAxisAlignment.Start,
                textDirection: TextDirection.Ltr,
                children:
                [
                    new Expanded(new Column(
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        children:
                        [
                            new Padding(
                                new Thickness(0, 0, 0, 10),
                                new HourTextField(
                                    selectedTime: _selectedTime,
                                    style: hourStyle,
                                    autofocus: Current.AutofocusHour,
                                    inputAction: TextInputAction.Next,
                                    validator: ValidateHour,
                                    onSavedSubmitted: HandleHourSavedSubmitted,
                                    onChanged: HandleHourChanged,
                                    hourLabelText: Current.HourLabelText,
                                    emptyInitialTime: Current.EmptyInitialTime)),
                            .. _hourHasError || _minuteHasError
                                ? Array.Empty<Widget>()
                                : new Widget[]
                                {
                                    new ExcludeSemantics(new Text(
                                        Current.HourLabelText ?? localizations.TimePickerHourLabel,
                                        style: theme.TextTheme.BodySmall,
                                        maxLines: 1,
                                        overflow: TextOverflow.Ellipsis)),
                                },
                        ])),
                    new TimeSelectorSeparator(timeOfDayFormat),
                    new Expanded(new Column(
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        children:
                        [
                            new Padding(
                                new Thickness(0, 0, 0, 10),
                                new MinuteTextField(
                                    selectedTime: _selectedTime,
                                    style: hourStyle,
                                    autofocus: Current.AutofocusMinute,
                                    inputAction: TextInputAction.Done,
                                    validator: ValidateMinute,
                                    onSavedSubmitted: HandleMinuteSavedSubmitted,
                                    minuteLabelText: Current.MinuteLabelText,
                                    emptyInitialTime: Current.EmptyInitialTime)),
                            .. _hourHasError || _minuteHasError
                                ? Array.Empty<Widget>()
                                : new Widget[]
                                {
                                    new ExcludeSemantics(new Text(
                                        Current.MinuteLabelText ?? localizations.TimePickerMinuteLabel,
                                        style: theme.TextTheme.BodySmall,
                                        maxLines: 1,
                                        overflow: TextOverflow.Ellipsis)),
                                },
                        ])),
                ]))));

        if (!use24HourDials && timeOfDayFormat != TimeOfDayFormat.ASpaceHColonMm)
        {
            rowChildren.Add(new Padding(
                EdgeInsetsGeometry.DirectionalOnly(start: 12),
                new DayPeriodControl(HandleDayPeriodChanged)));
        }

        return new Padding(
            useMaterial3 ? EdgeInsetsGeometry.Zero : EdgeInsetsGeometry.Symmetric(horizontal: 16),
            new Column(
                mainAxisSize: MainAxisSize.Min,
                crossAxisAlignment: CrossAxisAlignment.Start,
                children:
                [
                    new Padding(
                        EdgeInsetsGeometry.DirectionalOnly(
                            bottom: (useMaterial3 ? 20 : 24) - (minInteractiveVerticalPadding / 2)),
                        new Text(
                            Current.HelpText,
                            style: pickerTheme.HelpTextStyle ?? defaultTheme.HelpTextStyle)),
                    new Row(crossAxisAlignment: CrossAxisAlignment.Start, children: rowChildren),
                    _hourHasError || _minuteHasError
                        ? new Text(
                            Current.ErrorInvalidText ?? localizations.InvalidTimeLabel,
                            style: theme.TextTheme.BodyMedium.CopyWith(color: theme.ColorScheme.Error))
                        : new SizedBox(height: 2),
                ]));
    }
}

internal sealed class HourTextField : StatelessWidget
{
    public HourTextField(
        TimeOfDay selectedTime,
        TextStyle style,
        bool? autofocus,
        TextInputAction inputAction,
        FormFieldValidator<string> validator,
        Action<string?> onSavedSubmitted,
        Action<string> onChanged,
        string? hourLabelText,
        bool emptyInitialTime,
        Key? key = null) : base(key)
    {
        SelectedTime = selectedTime;
        Style = style;
        Autofocus = autofocus;
        InputAction = inputAction;
        Validator = validator;
        OnSavedSubmitted = onSavedSubmitted;
        OnChanged = onChanged;
        HourLabelText = hourLabelText;
        EmptyInitialTime = emptyInitialTime;
    }

    public TimeOfDay SelectedTime { get; }
    public TextStyle Style { get; }
    public bool? Autofocus { get; }
    public TextInputAction InputAction { get; }
    public FormFieldValidator<string> Validator { get; }
    public Action<string?> OnSavedSubmitted { get; }
    public Action<string> OnChanged { get; }
    public string? HourLabelText { get; }
    public bool EmptyInitialTime { get; }

    public override Widget Build(BuildContext context) => new HourMinuteTextField(
        selectedTime: SelectedTime,
        isHour: true,
        autofocus: Autofocus,
        inputAction: InputAction,
        style: Style,
        semanticHintText: HourLabelText ?? MaterialLocalizations.Of(context).TimePickerHourLabel,
        validator: Validator,
        onSavedSubmitted: OnSavedSubmitted,
        onChanged: OnChanged,
        emptyInitialTime: EmptyInitialTime);
}

internal sealed class MinuteTextField : StatelessWidget
{
    public MinuteTextField(
        TimeOfDay selectedTime,
        TextStyle style,
        bool? autofocus,
        TextInputAction inputAction,
        FormFieldValidator<string> validator,
        Action<string?> onSavedSubmitted,
        string? minuteLabelText,
        bool emptyInitialTime,
        Key? key = null) : base(key)
    {
        SelectedTime = selectedTime;
        Style = style;
        Autofocus = autofocus;
        InputAction = inputAction;
        Validator = validator;
        OnSavedSubmitted = onSavedSubmitted;
        MinuteLabelText = minuteLabelText;
        EmptyInitialTime = emptyInitialTime;
    }

    public TimeOfDay SelectedTime { get; }
    public TextStyle Style { get; }
    public bool? Autofocus { get; }
    public TextInputAction InputAction { get; }
    public FormFieldValidator<string> Validator { get; }
    public Action<string?> OnSavedSubmitted { get; }
    public string? MinuteLabelText { get; }
    public bool EmptyInitialTime { get; }

    public override Widget Build(BuildContext context) => new HourMinuteTextField(
        selectedTime: SelectedTime,
        isHour: false,
        autofocus: Autofocus,
        inputAction: InputAction,
        style: Style,
        semanticHintText: MinuteLabelText ?? MaterialLocalizations.Of(context).TimePickerMinuteLabel,
        validator: Validator,
        onSavedSubmitted: OnSavedSubmitted,
        emptyInitialTime: EmptyInitialTime);
}

internal sealed class HourMinuteTextField : StatefulWidget
{
    public HourMinuteTextField(
        TimeOfDay selectedTime,
        bool isHour,
        bool? autofocus,
        TextInputAction inputAction,
        TextStyle style,
        string semanticHintText,
        FormFieldValidator<string> validator,
        Action<string?> onSavedSubmitted,
        bool emptyInitialTime,
        Action<string>? onChanged = null,
        Key? key = null) : base(key)
    {
        SelectedTime = selectedTime;
        IsHour = isHour;
        Autofocus = autofocus;
        InputAction = inputAction;
        Style = style;
        SemanticHintText = semanticHintText;
        Validator = validator;
        OnSavedSubmitted = onSavedSubmitted;
        EmptyInitialTime = emptyInitialTime;
        OnChanged = onChanged;
    }

    public TimeOfDay SelectedTime { get; }
    public bool IsHour { get; }
    public bool? Autofocus { get; }
    public TextInputAction InputAction { get; }
    public TextStyle Style { get; }
    public string SemanticHintText { get; }
    public FormFieldValidator<string> Validator { get; }
    public Action<string?> OnSavedSubmitted { get; }
    public bool EmptyInitialTime { get; }
    public Action<string>? OnChanged { get; }

    public override State CreateState() => new HourMinuteTextFieldState();
}

internal sealed class HourMinuteTextFieldState : State
{
    private readonly TextEditingController _controller = new();
    private readonly FocusNode _focusNode = new();
    private bool _controllerHasBeenSet;

    private HourMinuteTextField Current => (HourMinuteTextField)StateWidget;

    private string FormattedValue
    {
        get
        {
            bool alwaysUse24HourFormat = MediaQuery.MaybeAlwaysUse24HourFormatOf(Context) ?? false;
            var localizations = MaterialLocalizations.Of(Context);
            return Current.IsHour
                ? localizations.FormatHour(Current.SelectedTime, alwaysUse24HourFormat)
                : localizations.FormatMinute(Current.SelectedTime);
        }
    }

    public override void InitState()
    {
        _focusNode.AddListener(() => SetState(() => { }));
    }

    public override void DidChangeDependencies()
    {
        if (_controllerHasBeenSet) return;
        _controllerHasBeenSet = true;
        _controller.Text = Current.EmptyInitialTime ? string.Empty : FormattedValue;
    }

    public override void Dispose()
    {
        _controller.Dispose();
        _focusNode.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var pickerTheme = TimePickerModel.ThemeOf(context);
        var defaultTheme = TimePickerModel.DefaultThemeOf(context);
        bool alwaysUse24HourFormat = MediaQuery.MaybeAlwaysUse24HourFormatOf(context) ?? false;

        var inputDecorationTheme = pickerTheme.InputDecorationTheme ?? defaultTheme.InputDecorationTheme;
        var inputDecoration = new InputDecoration(errorStyle: defaultTheme.InputDecorationTheme.ErrorStyle)
            .ApplyDefaults(inputDecorationTheme);
        string? hintText = _focusNode.HasFocus || Current.EmptyInitialTime ? null : FormattedValue;

        // The fill color is specified in both the input decoration theme and the time picker theme;
        // an explicit input decoration theme wins, then the hour/minute color, then the default.
        var startingFillColor = pickerTheme.InputDecorationTheme?.FillColor is { } themeFill
            ? new WidgetStateColor(themeFill)
            : pickerTheme.HourMinuteColor ?? defaultTheme.HourMinuteColor;
        var focusedStates = _focusNode.HasFocus
            ? MaterialState.Focused | MaterialState.Selected
            : MaterialState.None;
        var fillColor = theme.UseMaterial3
            ? startingFillColor.Resolve(focusedStates)
            : _focusNode.HasFocus
                ? Colors.Transparent
                : startingFillColor.Resolve(MaterialState.None);

        inputDecoration = inputDecoration with { HintText = hintText, FillColor = fillColor };

        var effectiveTextColor = (pickerTheme.HourMinuteTextColor ?? defaultTheme.HourMinuteTextColor)
            .Resolve(focusedStates);
        var effectiveStyle = Current.Style.CopyWith(color: effectiveTextColor);

        return new SizedBox(
            width: alwaysUse24HourFormat
                ? defaultTheme.HourMinuteInputSize24Hour.Width
                : defaultTheme.HourMinuteInputSize.Width,
            height: alwaysUse24HourFormat
                ? defaultTheme.HourMinuteInputSize24Hour.Height
                : defaultTheme.HourMinuteInputSize.Height,
            child: MediaQuery.WithNoTextScaling(
                context,
                new Semantics(
                    label: Current.SemanticHintText,
                    child: new TextFormField(
                        controller: _controller,
                        focusNode: _focusNode,
                        autofocus: Current.Autofocus ?? false,
                        expands: true,
                        maxLines: null,
                        maxLength: 2,
                        buildCounter: (_, _, _, _) => new SizedBox(),
                        textAlign: TextAlign.Center,
                        textInputAction: Current.InputAction,
                        keyboardType: TextInputType.Number,
                        style: effectiveStyle,
                        decoration: inputDecoration,
                        validator: Current.Validator,
                        onEditingComplete: () => Current.OnSavedSubmitted(_controller.Text),
                        onSaved: value => Current.OnSavedSubmitted(value),
                        onFieldSubmitted: value => Current.OnSavedSubmitted(value),
                        onChanged: Current.OnChanged))));
    }
}

internal static class WidgetStateColorExtensions
{
    /// Bridges Material's flags enum onto the core `WidgetState` set the state color resolves against.
    internal static Color Resolve(this WidgetStateColor color, MaterialState states)
    {
        var set = new HashSet<WidgetState>();
        if (states.HasFlag(MaterialState.Hovered)) set.Add(WidgetState.Hovered);
        if (states.HasFlag(MaterialState.Focused)) set.Add(WidgetState.Focused);
        if (states.HasFlag(MaterialState.Pressed)) set.Add(WidgetState.Pressed);
        if (states.HasFlag(MaterialState.Disabled)) set.Add(WidgetState.Disabled);
        if (states.HasFlag(MaterialState.Selected)) set.Add(WidgetState.Selected);
        if (states.HasFlag(MaterialState.Dragged)) set.Add(WidgetState.Dragged);
        return color.Resolve(set);
    }
}

internal sealed class TappableLabel
{
    public TappableLabel(int value, bool inner, string text, TextStyle style)
    {
        Value = value;
        Inner = inner;
        Text = text;
        Style = style;
    }

    public int Value { get; }
    public bool Inner { get; }
    public string Text { get; }
    public TextStyle Style { get; }

    private TextLayout? _layout;
    private bool _layoutResolved;

    public TextLayout? Layout
    {
        get
        {
            if (_layoutResolved) return _layout;
            _layoutResolved = true;
            try
            {
                var typeface = new Typeface(
                    Style.FontFamily ?? FontFamily.Default,
                    Style.FontStyle ?? Avalonia.Media.FontStyle.Normal,
                    Style.FontWeight ?? FontWeight.Normal,
                    FontStretch.Normal);
                _layout = new TextLayout(
                    Text,
                    typeface,
                    Style.FontSize ?? 16.0,
                    new SolidColorBrush(Style.Color ?? Colors.Black));
            }
            catch (Exception exception) when (TextLayoutFallback.IsMissingFontManager(exception))
            {
                _layout = null;
            }

            return _layout;
        }
    }
}

internal sealed class TimeDialPainter : CustomPainter
{
    public TimeDialPainter(
        IReadOnlyList<TappableLabel> primaryLabels,
        IReadOnlyList<TappableLabel> selectedLabels,
        Color backgroundColor,
        Color handColor,
        double handWidth,
        Color dotColor,
        double dotRadius,
        double centerRadius,
        double theta,
        double radius)
    {
        PrimaryLabels = primaryLabels;
        SelectedLabels = selectedLabels;
        BackgroundColor = backgroundColor;
        HandColor = handColor;
        HandWidth = handWidth;
        DotColor = dotColor;
        DotRadius = dotRadius;
        CenterRadius = centerRadius;
        Theta = theta;
        Radius = radius;
    }

    public IReadOnlyList<TappableLabel> PrimaryLabels { get; }
    public IReadOnlyList<TappableLabel> SelectedLabels { get; }
    public Color BackgroundColor { get; }
    public Color HandColor { get; }
    public double HandWidth { get; }
    public Color DotColor { get; }
    public double DotRadius { get; }
    public double CenterRadius { get; }
    public double Theta { get; }
    public double Radius { get; }

    public override void Paint(PaintingContext context, Size size)
    {
        double dialRadius = Math.Max(
            Math.Min(size.Width, size.Height) / 2,
            TimePickerConstants.DialMinRadius + DotRadius);
        double labelRadius = Math.Max(dialRadius - TimePickerConstants.DialPadding, 50);
        double innerLabelRadius = Math.Max(labelRadius - TimePickerConstants.InnerDialOffset, 0);
        double handleRadius = Math.Max(
            labelRadius - ((Radius < 0.5 ? 1 : 0) * (labelRadius - innerLabelRadius)),
            50);
        var center = new Point(size.Width / 2, size.Height / 2);

        Point OffsetForTheta(double theta, double radius) => new(
            center.X + (radius * Math.Cos(theta)),
            center.Y - (radius * Math.Sin(theta)));

        void PaintLabels(PaintingContext target, IReadOnlyList<TappableLabel> labels, double radius)
        {
            if (labels.Count == 0) return;
            double labelThetaIncrement = -TimePickerConstants.TwoPi / labels.Count;
            double labelTheta = Math.PI / 2;
            foreach (var label in labels)
            {
                var layout = label.Layout;
                if (layout is not null)
                {
                    var position = OffsetForTheta(labelTheta, radius);
                    target.DrawTextLayout(
                        layout,
                        new Point(position.X - (layout.Width / 2), position.Y - (layout.Height / 2)));
                }

                labelTheta += labelThetaIncrement;
            }
        }

        void PaintInnerOuterLabels(PaintingContext target, IReadOnlyList<TappableLabel> labels)
        {
            PaintLabels(target, labels.Where(label => !label.Inner).ToList(), labelRadius);
            PaintLabels(target, labels.Where(label => label.Inner).ToList(), innerLabelRadius);
        }

        context.DrawCircle(new SolidColorBrush(BackgroundColor), null, center, dialRadius);
        PaintInnerOuterLabels(context, PrimaryLabels);

        var handBrush = new SolidColorBrush(HandColor);
        var focusedPoint = OffsetForTheta(Theta, handleRadius);
        context.DrawCircle(handBrush, null, center, CenterRadius);
        context.DrawCircle(handBrush, null, focusedPoint, DotRadius);
        context.DrawLine(new Pen(handBrush, HandWidth), center, focusedPoint);

        if (PrimaryLabels.Count > 0)
        {
            double labelThetaIncrement = -TimePickerConstants.TwoPi / PrimaryLabels.Count;
            double remainder = Theta - (Math.Floor(Theta / labelThetaIncrement) * labelThetaIncrement);
            if (remainder is > 0.1 and < 0.45)
            {
                context.DrawCircle(new SolidColorBrush(DotColor), null, focusedPoint, 2);
            }
        }

        context.PushClipGeometry(
            new EllipseGeometry(new Rect(
                focusedPoint.X - DotRadius,
                focusedPoint.Y - DotRadius,
                DotRadius * 2,
                DotRadius * 2)),
            clipped => PaintInnerOuterLabels(clipped, SelectedLabels));
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate) => oldDelegate is not TimeDialPainter old
        || !ReferenceEquals(old.PrimaryLabels, PrimaryLabels)
        || !ReferenceEquals(old.SelectedLabels, SelectedLabels)
        || old.BackgroundColor != BackgroundColor
        || old.HandColor != HandColor
        || Math.Abs(old.Theta - Theta) > double.Epsilon;
}

internal sealed class Dial : StatefulWidget
{
    public Dial(
        TimeOfDay selectedTime,
        HourMinuteMode hourMinuteMode,
        HourDialType hourDialType,
        Action<TimeOfDay>? onChanged,
        Action? onHourSelected,
        Key? key = null) : base(key)
    {
        SelectedTime = selectedTime;
        HourMinuteMode = hourMinuteMode;
        HourDialType = hourDialType;
        OnChanged = onChanged;
        OnHourSelected = onHourSelected;
    }

    public TimeOfDay SelectedTime { get; }
    public HourMinuteMode HourMinuteMode { get; }
    public HourDialType HourDialType { get; }
    public Action<TimeOfDay>? OnChanged { get; }
    public Action? OnHourSelected { get; }

    public override State CreateState() => new DialState();
}

internal sealed class DialState : State
{
    private static readonly int[] AmHours = [12, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11];
    private static readonly int[] TwentyFourHoursM2 = [0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22];

    private AnimationController? _controller;
    private DoubleTween? _thetaTween;
    private DoubleTween? _radiusTween;
    private Animation<double>? _theta;
    private Animation<double>? _radius;
    private bool _dragging;
    private Point _position;
    private Point _center;
    private Size _dialSize;

    private Dial Current => (Dial)StateWidget;

    public override void InitState()
    {
        _controller = new AnimationController(duration: TimePickerConstants.DialAnimateDuration, vsync: this);
        _thetaTween = new DoubleTween(begin: GetThetaForTime(Current.SelectedTime), end: 0);
        _thetaTween.End = _thetaTween.Begin;
        _radiusTween = new DoubleTween(begin: GetRadiusForTime(Current.SelectedTime), end: 0);
        _radiusTween.End = _radiusTween.Begin;
        var curve = new CurvedAnimation(_controller, Curves.FastOutSlowIn);
        _theta = _thetaTween.Animate(curve);
        _radius = _radiusTween.Animate(curve);
        _theta.AddListener(() => SetState(() => { }));
        _radius.AddListener(() => SetState(() => { }));
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var old = (Dial)oldWidget;
        if ((old.HourMinuteMode != Current.HourMinuteMode || old.SelectedTime != Current.SelectedTime)
            && !_dragging)
        {
            AnimateTo(GetThetaForTime(Current.SelectedTime), GetRadiusForTime(Current.SelectedTime));
        }
    }

    public override void Dispose()
    {
        _controller?.Dispose();
        _controller = null;
    }

    private static double Nearest(double target, double a, double b) =>
        Math.Abs(target - a) < Math.Abs(target - b) ? a : b;

    private void AnimateTo(double targetTheta, double targetRadius)
    {
        void AnimateValue(double target, Animation<double> animation, DoubleTween tween, double min, double max)
        {
            double beginValue = Nearest(target, animation.Value, max);
            beginValue = Nearest(target, beginValue, min);
            tween.Begin = beginValue;
            tween.End = target;
            _controller!.SetValue(0);
            _controller.Forward();
        }

        AnimateValue(
            targetTheta,
            _theta!,
            _thetaTween!,
            _theta!.Value - TimePickerConstants.TwoPi,
            _theta.Value + TimePickerConstants.TwoPi);
        AnimateValue(targetRadius, _radius!, _radiusTween!, 0, 1);
    }

    private double GetRadiusForTime(TimeOfDay time)
    {
        if (Current.HourMinuteMode != HourMinuteMode.Hour) return 1;
        return Current.HourDialType == HourDialType.TwentyFourHourDoubleRing
            ? time.Hour >= 12 ? 0 : 1
            : 1;
    }

    private double GetThetaForTime(TimeOfDay time)
    {
        int hoursFactor = Current.HourDialType switch
        {
            HourDialType.TwentyFourHour => TimeOfDay.HoursPerDay,
            _ => TimeOfDay.HoursPerPeriod,
        };
        double fraction = Current.HourMinuteMode == HourMinuteMode.Hour
            ? (time.Hour / (double)hoursFactor) % hoursFactor
            : (time.Minute / (double)TimeOfDay.MinutesPerHour) % TimeOfDay.MinutesPerHour;
        return Modulo((Math.PI / 2) - (fraction * TimePickerConstants.TwoPi), TimePickerConstants.TwoPi);
    }

    private TimeOfDay GetTimeForTheta(double theta, bool roundMinutes, double radius)
    {
        double fraction = Modulo(0.25 - (Modulo(theta, TimePickerConstants.TwoPi) / TimePickerConstants.TwoPi), 1);
        if (Current.HourMinuteMode == HourMinuteMode.Hour)
        {
            int newHour;
            switch (Current.HourDialType)
            {
                case HourDialType.TwentyFourHour:
                    newHour = (int)Math.Round(fraction * TimeOfDay.HoursPerDay) % TimeOfDay.HoursPerDay;
                    break;
                case HourDialType.TwentyFourHourDoubleRing:
                    newHour = (int)Math.Round(fraction * TimeOfDay.HoursPerPeriod) % TimeOfDay.HoursPerPeriod;
                    if (radius < 0.5) newHour += TimeOfDay.HoursPerPeriod;
                    break;
                default:
                    newHour = (int)Math.Round(fraction * TimeOfDay.HoursPerPeriod) % TimeOfDay.HoursPerPeriod;
                    newHour += Current.SelectedTime.PeriodOffset;
                    break;
            }

            return Current.SelectedTime.Replacing(hour: newHour);
        }

        int minute = (int)Math.Round(fraction * TimeOfDay.MinutesPerHour) % TimeOfDay.MinutesPerHour;
        if (roundMinutes) minute = ((minute + 2) / 5 * 5) % TimeOfDay.MinutesPerHour;
        return Current.SelectedTime.Replacing(minute: minute);
    }

    private static double Modulo(double value, double modulus)
    {
        double result = value % modulus;
        return result < 0 ? result + modulus : result;
    }

    private void UpdateThetaForPan(bool roundMinutes = false)
    {
        SetState(() =>
        {
            var offset = new Point(_position.X - _center.X, _position.Y - _center.Y);
            double labelRadius = (Math.Min(_dialSize.Width, _dialSize.Height) / 2) - TimePickerConstants.DialPadding;
            double innerRadius = labelRadius - TimePickerConstants.InnerDialOffset;
            double angle = Modulo(
                Math.Atan2(offset.X, offset.Y) - (Math.PI / 2),
                TimePickerConstants.TwoPi);
            double distance = Math.Sqrt((offset.X * offset.X) + (offset.Y * offset.Y));
            double radius = Math.Clamp(
                (distance - innerRadius) / TimePickerConstants.InnerDialOffset,
                0,
                1);
            if (roundMinutes)
            {
                angle = GetThetaForTime(GetTimeForTheta(angle, roundMinutes: true, radius: radius));
            }

            _thetaTween!.Begin = angle;
            _thetaTween.End = angle;
            _radiusTween!.Begin = radius;
            _radiusTween.End = radius;
        });
    }

    private TimeOfDay NotifyOnChangedIfNeeded(bool roundMinutes = false)
    {
        var current = GetTimeForTheta(_theta!.Value, roundMinutes, _radius!.Value);
        if (Current.OnChanged is null) return current;
        if (current != Current.SelectedTime) Current.OnChanged(current);
        return current;
    }

    private void HandlePanStart(DragStartDetails details)
    {
        _dragging = true;
        var box = (RenderBox)Context.FindRenderObject()!;
        _position = ToLocal(box, details.GlobalPosition);
        _dialSize = box.Size;
        _center = new Point(_dialSize.Width / 2, _dialSize.Height / 2);
        UpdateThetaForPan();
        NotifyOnChangedIfNeeded();
    }

    private void HandlePanUpdate(DragUpdateDetails details)
    {
        _position = new Point(_position.X + details.Delta.X, _position.Y + details.Delta.Y);
        UpdateThetaForPan();
        NotifyOnChangedIfNeeded();
    }

    private void HandlePanEnd()
    {
        _dragging = false;
        _position = default;
        _center = default;
        _dialSize = default;
        AnimateTo(GetThetaForTime(Current.SelectedTime), GetRadiusForTime(Current.SelectedTime));
        if (Current.HourMinuteMode == HourMinuteMode.Hour) Current.OnHourSelected?.Invoke();
    }

    private static Point ToLocal(RenderBox box, Point globalPosition)
    {
        var origin = box.GetPaintOffsetToRoot();
        return new Point(globalPosition.X - origin.X, globalPosition.Y - origin.Y);
    }

    private void HandleTapUp(Point globalPosition)
    {
        var box = (RenderBox)Context.FindRenderObject()!;
        _position = ToLocal(box, globalPosition);
        _dialSize = box.Size;
        _center = new Point(_dialSize.Width / 2, _dialSize.Height / 2);
        UpdateThetaForPan(roundMinutes: true);
        NotifyOnChangedIfNeeded(roundMinutes: true);
        if (Current.HourMinuteMode == HourMinuteMode.Hour) Current.OnHourSelected?.Invoke();
        var time = GetTimeForTheta(_theta!.Value, roundMinutes: true, radius: _radius!.Value);
        AnimateTo(GetThetaForTime(time), GetRadiusForTime(time));
        _dragging = false;
        _position = default;
        _center = default;
        _dialSize = default;
    }

    private IReadOnlyList<TappableLabel> Build24HourRing(TextStyle style, bool useMaterial3)
    {
        var localizations = MaterialLocalizations.Of(Context);
        var labels = new List<TappableLabel>();
        if (useMaterial3)
        {
            for (int hour = 0; hour < TimeOfDay.HoursPerDay; hour++)
            {
                var timeOfDay = new TimeOfDay(hour, 0);
                labels.Add(new TappableLabel(
                    value: hour,
                    inner: hour >= 12,
                    text: hour != 0
                        ? localizations.FormatDecimal(hour)
                        : localizations.FormatHour(timeOfDay, alwaysUse24HourFormat: true),
                    style: style));
            }

            return labels;
        }

        foreach (int hour in TwentyFourHoursM2)
        {
            var timeOfDay = new TimeOfDay(hour, 0);
            labels.Add(new TappableLabel(
                value: hour,
                inner: false,
                text: localizations.FormatHour(timeOfDay, alwaysUse24HourFormat: true),
                style: style));
        }

        return labels;
    }

    private IReadOnlyList<TappableLabel> Build12HourRing(TextStyle style)
    {
        var localizations = MaterialLocalizations.Of(Context);
        bool alwaysUse24HourFormat = MediaQuery.MaybeAlwaysUse24HourFormatOf(Context) ?? false;
        return AmHours
            .Select(hour => new TappableLabel(
                value: hour,
                inner: false,
                text: localizations.FormatHour(new TimeOfDay(hour % 24, 0), alwaysUse24HourFormat),
                style: style))
            .ToList();
    }

    private IReadOnlyList<TappableLabel> BuildMinutes(TextStyle style)
    {
        var localizations = MaterialLocalizations.Of(Context);
        var labels = new List<TappableLabel>();
        for (int minute = 0; minute < TimeOfDay.MinutesPerHour; minute += 5)
        {
            labels.Add(new TappableLabel(
                value: minute,
                inner: false,
                text: localizations.FormatMinute(new TimeOfDay(0, minute)),
                style: style));
        }

        return labels;
    }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var pickerTheme = TimePickerModel.ThemeOf(context);
        var defaultTheme = TimePickerModel.DefaultThemeOf(context);
        var dialTextColor = pickerTheme.DialTextColor ?? defaultTheme.DialTextColor;
        var dialTextStyle = pickerTheme.DialTextStyle ?? defaultTheme.DialTextStyle;
        var primaryStyle = dialTextStyle.CopyWith(color: dialTextColor.Resolve(MaterialState.None));
        var selectedStyle = dialTextStyle.CopyWith(color: dialTextColor.Resolve(MaterialState.Selected));

        IReadOnlyList<TappableLabel> primaryLabels;
        IReadOnlyList<TappableLabel> selectedLabels;
        double radiusValue;
        switch (Current.HourMinuteMode)
        {
            case HourMinuteMode.Hour when Current.HourDialType == HourDialType.TwelveHour:
                primaryLabels = Build12HourRing(primaryStyle);
                selectedLabels = Build12HourRing(selectedStyle);
                radiusValue = 1;
                break;
            case HourMinuteMode.Hour:
                primaryLabels = Build24HourRing(primaryStyle, theme.UseMaterial3);
                selectedLabels = Build24HourRing(selectedStyle, theme.UseMaterial3);
                radiusValue = theme.UseMaterial3 ? _radius!.Value : 1;
                break;
            default:
                primaryLabels = BuildMinutes(primaryStyle);
                selectedLabels = BuildMinutes(selectedStyle);
                radiusValue = 1;
                break;
        }

        return new RawGestureDetector(
            excludeFromSemantics: true,
            onPanStart: HandlePanStart,
            onPanUpdate: HandlePanUpdate,
            onPanEnd: _ => HandlePanEnd(),
            onTapUp: details => HandleTapUp(details.Position),
            child: new CustomPaint(
                painter: new TimeDialPainter(
                    primaryLabels: primaryLabels,
                    selectedLabels: selectedLabels,
                    backgroundColor: pickerTheme.DialBackgroundColor ?? defaultTheme.DialBackgroundColor,
                    handColor: pickerTheme.DialHandColor ?? defaultTheme.DialHandColor,
                    handWidth: defaultTheme.HandWidth,
                    dotColor: dialTextColor.Resolve(MaterialState.Selected),
                    dotRadius: defaultTheme.DotRadius,
                    centerRadius: defaultTheme.CenterRadius,
                    theta: _theta!.Value,
                    radius: radiusValue)));
    }
}

internal sealed class TimePickerBody : StatefulWidget
{
    public TimePickerBody(
        TimeOfDay time,
        Action<TimeOfDay>? onTimeChanged,
        string? helpText = null,
        string? errorInvalidText = null,
        string? hourLabelText = null,
        string? minuteLabelText = null,
        TimePickerEntryMode entryMode = TimePickerEntryMode.Dial,
        Orientation? orientation = null,
        EntryModeChangeCallback? onEntryModeChanged = null,
        bool emptyInitialInput = false,
        Key? key = null) : base(key)
    {
        Time = time;
        OnTimeChanged = onTimeChanged;
        HelpText = helpText;
        ErrorInvalidText = errorInvalidText;
        HourLabelText = hourLabelText;
        MinuteLabelText = minuteLabelText;
        EntryMode = entryMode;
        Orientation = orientation;
        OnEntryModeChanged = onEntryModeChanged;
        EmptyInitialInput = emptyInitialInput;
    }

    public TimeOfDay Time { get; }
    public Action<TimeOfDay>? OnTimeChanged { get; }
    public string? HelpText { get; }
    public string? ErrorInvalidText { get; }
    public string? HourLabelText { get; }
    public string? MinuteLabelText { get; }
    public TimePickerEntryMode EntryMode { get; }
    public Orientation? Orientation { get; }
    public EntryModeChangeCallback? OnEntryModeChanged { get; }
    public bool EmptyInitialInput { get; }

    public override State CreateState() => new TimePickerBodyState();
}

internal sealed class TimePickerBodyState : State
{
    private HourMinuteMode _hourMinuteMode = HourMinuteMode.Hour;
    private bool? _autofocusHour;
    private bool? _autofocusMinute;
    private TimeOfDay _selectedTime;
    private Orientation? _orientation;
    private System.Threading.Timer? _vibrateTimer;

    private TimePickerBody Current => (TimePickerBody)StateWidget;

    internal TimeOfDay SelectedTime => _selectedTime;

    public override void InitState()
    {
        _selectedTime = Current.Time;
        _orientation = Current.Orientation;
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var old = (TimePickerBody)oldWidget;
        if (old.Orientation != Current.Orientation) _orientation = Current.Orientation;
        if (old.Time != Current.Time) _selectedTime = Current.Time;
    }

    public override void Dispose()
    {
        _vibrateTimer?.Dispose();
        _vibrateTimer = null;
    }

    private void Vibrate()
    {
        switch (Theme.Of(Context).Platform)
        {
            case TargetPlatform.Android:
            case TargetPlatform.Fuchsia:
            case TargetPlatform.Linux:
            case TargetPlatform.Windows:
                _vibrateTimer?.Dispose();
                _vibrateTimer = new System.Threading.Timer(
                    _ =>
                    {
                        HapticFeedback.Vibrate();
                        _vibrateTimer = null;
                    },
                    null,
                    TimePickerConstants.VibrateCommitDelay,
                    System.Threading.Timeout.InfiniteTimeSpan);
                break;
            default:
                break;
        }
    }

    private void HandleHourMinuteModeChanged(HourMinuteMode mode)
    {
        Vibrate();
        SetState(() => _hourMinuteMode = mode);
    }

    private void HandleEntryModeToggle()
    {
        var newMode = Current.EntryMode;
        SetState(() =>
        {
            switch (Current.EntryMode)
            {
                case TimePickerEntryMode.Dial:
                    newMode = TimePickerEntryMode.Input;
                    break;
                case TimePickerEntryMode.Input:
                    _autofocusHour = false;
                    _autofocusMinute = false;
                    newMode = TimePickerEntryMode.Dial;
                    break;
                default:
                    break;
            }
        });
        Current.OnEntryModeChanged?.Invoke(newMode);
    }

    private void HandleTimeChanged(TimeOfDay value)
    {
        Vibrate();
        SetState(() =>
        {
            _selectedTime = value;
            Current.OnTimeChanged?.Invoke(value);
        });
    }

    private void HandleHourDoubleTapped()
    {
        _autofocusHour = true;
        HandleEntryModeToggle();
    }

    private void HandleMinuteDoubleTapped()
    {
        _autofocusMinute = true;
        HandleEntryModeToggle();
    }

    private void HandleHourSelected() => SetState(() => _hourMinuteMode = HourMinuteMode.Minute);

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var localizations = MaterialLocalizations.Of(context);
        bool alwaysUse24HourFormat = MediaQuery.MaybeAlwaysUse24HourFormatOf(context) ?? false;
        var timeOfDayFormat = localizations.TimeOfDayFormat(alwaysUse24HourFormat);
        TimePickerDefaults defaultTheme = theme.UseMaterial3
            ? new TimePickerDefaultsM3(context, Current.EntryMode)
            : new TimePickerDefaultsM2(context);
        var orientation = _orientation ?? MediaQuery.MaybeOrientationOf(context) ?? Orientation.Portrait;
        var hourMode = TimeOfDay.HourFormatOf(timeOfDayFormat) switch
        {
            HourFormat.H12 => HourDialType.TwelveHour,
            _ => theme.UseMaterial3 ? HourDialType.TwentyFourHourDoubleRing : HourDialType.TwentyFourHour,
        };

        Widget picker;
        if (Current.EntryMode is TimePickerEntryMode.Dial or TimePickerEntryMode.DialOnly)
        {
            string helpText = Current.HelpText ?? (theme.UseMaterial3
                ? localizations.TimePickerDialHelpText
                : localizations.TimePickerDialHelpText.ToUpperInvariant());
            double portraitAdjustment = Math.Max(
                0,
                (2 * WidgetConstants.MinInteractiveDimension) - defaultTheme.DayPeriodPortraitSize.Height);
            var dialPadding = orientation == Orientation.Portrait
                ? EdgeInsetsGeometry.Only(left: 12, right: 12, top: 36 - (portraitAdjustment / 2))
                : EdgeInsetsGeometry.DirectionalOnly(start: 64);
            Widget dial = new Padding(
                dialPadding,
                new ExcludeSemantics(new SizedBox(
                    width: defaultTheme.DialSize.Width,
                    height: defaultTheme.DialSize.Height,
                    child: new AspectRatio(
                        aspectRatio: 1,
                        child: new Dial(
                            selectedTime: _selectedTime,
                            hourMinuteMode: _hourMinuteMode,
                            hourDialType: hourMode,
                            onChanged: HandleTimeChanged,
                            onHourSelected: HandleHourSelected)))));

            picker = orientation == Orientation.Portrait
                ? new Column(
                    mainAxisSize: MainAxisSize.Min,
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    children:
                    [
                        new Padding(
                            EdgeInsetsGeometry.Symmetric(horizontal: theme.UseMaterial3 ? 0 : 16),
                            new DialTimePickerHeader(helpText)),
                        new Expanded(new Column(
                            mainAxisSize: MainAxisSize.Min,
                            children:
                            [
                                new Expanded(new Padding(
                                    EdgeInsetsGeometry.Symmetric(horizontal: theme.UseMaterial3 ? 0 : 16),
                                    dial)),
                            ])),
                    ])
                : new Column(children:
                [
                    new Expanded(new Padding(
                        EdgeInsetsGeometry.Symmetric(horizontal: theme.UseMaterial3 ? 0 : 16),
                        new Row(
                            crossAxisAlignment: CrossAxisAlignment.Stretch,
                            children: [new DialTimePickerHeader(helpText), new Expanded(dial)]))),
                ]);
        }
        else
        {
            string helpText = Current.HelpText ?? (theme.UseMaterial3
                ? localizations.TimePickerInputHelpText
                : localizations.TimePickerInputHelpText.ToUpperInvariant());
            picker = new Column(
                mainAxisSize: MainAxisSize.Min,
                children:
                [
                    new TimePickerInput(
                        initialSelectedTime: _selectedTime,
                        errorInvalidText: Current.ErrorInvalidText,
                        hourLabelText: Current.HourLabelText,
                        minuteLabelText: Current.MinuteLabelText,
                        helpText: helpText,
                        autofocusHour: _autofocusHour,
                        autofocusMinute: _autofocusMinute,
                        emptyInitialTime: Current.EmptyInitialInput),
                ]);
        }

        return new TimePickerModel(
            entryMode: Current.EntryMode,
            selectedTime: _selectedTime,
            hourMinuteMode: _hourMinuteMode,
            orientation: orientation,
            onHourMinuteModeChanged: HandleHourMinuteModeChanged,
            onHourDoubleTapped: HandleHourDoubleTapped,
            onMinuteDoubleTapped: HandleMinuteDoubleTapped,
            hourDialType: hourMode,
            onSelectedTimeChanged: HandleTimeChanged,
            useMaterial3: theme.UseMaterial3,
            use24HourFormat: alwaysUse24HourFormat,
            theme: TimePickerTheme.Of(context),
            defaultTheme: defaultTheme,
            child: picker);
    }
}

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
        Icon? switchToInputEntryModeIcon = null,
        Icon? switchToTimerEntryModeIcon = null,
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
    public Icon? SwitchToInputEntryModeIcon { get; }
    public Icon? SwitchToTimerEntryModeIcon { get; }
    public bool EmptyInitialInput { get; }

    public override State CreateState() => new TimePickerDialogState();
}

internal sealed class TimePickerDialogState : State
{
    internal static readonly Size PortraitSize = new(310, 468);
    internal static readonly Size LandscapeSize = new(524, 342);
    internal static readonly Size LandscapeSizeM2 = new(508, 300);
    internal static readonly Size InputSize = new(312, 252);
    internal const double InputMinimumHeight = 216;
    internal static readonly Size MinPortraitSize = new(238, 326);
    internal static readonly Size MinLandscapeSize = new(416, 248);
    internal static readonly Size MinInputSize = new(312, 196);

    private readonly LabeledGlobalKey<FormState> _formKey = new("time-picker-form");
    private TimeOfDay _selectedTime;
    private TimePickerEntryMode _entryMode;
    private AutovalidateMode _autovalidateMode = AutovalidateMode.Disabled;
    private Orientation? _orientation;

    private TimePickerDialog Current => (TimePickerDialog)StateWidget;

    public override void InitState()
    {
        _selectedTime = Current.InitialTime;
        _entryMode = Current.InitialEntryMode;
        _orientation = Current.Orientation;
    }

    private void HandleTimeChanged(TimeOfDay value)
    {
        if (value == _selectedTime) return;
        SetState(() => _selectedTime = value);
    }

    private void HandleEntryModeChanged(TimePickerEntryMode value)
    {
        if (value == _entryMode) return;
        SetState(() =>
        {
            switch (_entryMode)
            {
                case TimePickerEntryMode.Dial:
                    _autovalidateMode = AutovalidateMode.Disabled;
                    break;
                case TimePickerEntryMode.Input:
                    _formKey.CurrentState?.Save();
                    break;
                default:
                    break;
            }

            _entryMode = value;
        });
        Current.OnEntryModeChanged?.Invoke(value);
    }

    private void ToggleEntryMode() => HandleEntryModeChanged(_entryMode switch
    {
        TimePickerEntryMode.Dial => TimePickerEntryMode.Input,
        TimePickerEntryMode.Input => TimePickerEntryMode.Dial,
        _ => _entryMode,
    });

    private void HandleCancel() => Navigator.Pop(Context);

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

    private double InputWidth(BuildContext context, TimePickerDefaults defaults, bool useMaterial3, double baseWidth)
    {
        var localizations = MaterialLocalizations.Of(context);
        var format = localizations.TimeOfDayFormat(MediaQuery.MaybeAlwaysUse24HourFormatOf(context) ?? false);
        return format switch
        {
            TimeOfDayFormat.ASpaceHColonMm or TimeOfDayFormat.HColonMmSpaceA =>
                baseWidth - (useMaterial3 ? 32 : 0),
            _ => baseWidth - defaults.DayPeriodPortraitSize.Width - 12,
        };
    }

    private Size MinDialogSize(BuildContext context, TimePickerDefaults defaults, bool useMaterial3)
    {
        var orientation = _orientation ?? MediaQuery.MaybeOrientationOf(context) ?? Orientation.Portrait;
        if (_entryMode is TimePickerEntryMode.Dial or TimePickerEntryMode.DialOnly)
        {
            return orientation == Orientation.Portrait ? MinPortraitSize : MinLandscapeSize;
        }

        return new Size(
            InputWidth(context, defaults, useMaterial3, MinInputSize.Width),
            MinInputSize.Height);
    }

    private Size DialogSize(BuildContext context, TimePickerDefaults defaults, bool useMaterial3)
    {
        var orientation = _orientation ?? MediaQuery.MaybeOrientationOf(context) ?? Orientation.Portrait;
        var scaler = TextScaler
            .Linear(MediaQuery.MaybeTextScaleFactorOf(context) ?? 1.0)
            .Clamp(maxScaleFactor: 1.1);
        double textScaleFactor = scaler.Scale(TextDefaults.DefaultFontSize) / TextDefaults.DefaultFontSize;

        Size timePickerSize;
        if (_entryMode is TimePickerEntryMode.Dial or TimePickerEntryMode.DialOnly)
        {
            timePickerSize = orientation == Orientation.Portrait
                ? PortraitSize
                : new Size(
                    LandscapeSize.Width * textScaleFactor,
                    useMaterial3 ? LandscapeSize.Height : LandscapeSizeM2.Height);
        }
        else
        {
            timePickerSize = new Size(
                InputWidth(context, defaults, useMaterial3, InputSize.Width),
                InputSize.Height);
        }

        return new Size(timePickerSize.Width, timePickerSize.Height * textScaleFactor);
    }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var localizations = MaterialLocalizations.Of(context);
        var pickerTheme = TimePickerTheme.Of(context);
        TimePickerDefaults defaultTheme = theme.UseMaterial3
            ? new TimePickerDefaultsM3(context, _entryMode)
            : new TimePickerDefaultsM2(context);
        var shape = pickerTheme.Shape ?? defaultTheme.Shape;
        var entryModeIconColor = pickerTheme.EntryModeIconColor ?? defaultTheme.EntryModeIconColor;
        bool inputMode = _entryMode is TimePickerEntryMode.Input or TimePickerEntryMode.InputOnly;

        var actionChildren = new List<Widget>();
        if (_entryMode is TimePickerEntryMode.Dial or TimePickerEntryMode.Input)
        {
            bool dialMode = _entryMode == TimePickerEntryMode.Dial;
            actionChildren.Add(new IconButton(
                color: theme.UseMaterial3 ? null : entryModeIconColor,
                style: theme.UseMaterial3
                    ? IconButton.StyleFrom(foregroundColor: entryModeIconColor)
                    : null,
                onPressed: ToggleEntryMode,
                icon: dialMode
                    ? Current.SwitchToInputEntryModeIcon ?? new Icon(Icons.KeyboardOutlined)
                    : Current.SwitchToTimerEntryModeIcon ?? new Icon(Icons.AccessTime),
                tooltip: dialMode
                    ? localizations.InputTimeModeButtonLabel
                    : localizations.DialModeButtonLabel));
        }

        actionChildren.Add(new Expanded(new ConstrainedBox(
            new BoxConstraints(MinHeight: 36),
            new Align(
                alignment: AlignmentDirectional.CenterEnd,
                child: new OverflowBar(
                    spacing: 8,
                    overflowAlignment: OverflowBarAlignment.End,
                    children:
                    [
                        new TextButton(
                            style: pickerTheme.CancelButtonStyle ?? defaultTheme.CancelButtonStyle,
                            onPressed: HandleCancel,
                            child: new Text(Current.CancelText ?? (theme.UseMaterial3
                                ? localizations.CancelButtonLabel
                                : localizations.CancelButtonLabel.ToUpperInvariant()))),
                        new TextButton(
                            style: pickerTheme.ConfirmButtonStyle ?? defaultTheme.ConfirmButtonStyle,
                            onPressed: HandleOk,
                            child: new Text(Current.ConfirmText ?? localizations.OkButtonLabel)),
                    ])))));

        Widget actions = new Padding(
            EdgeInsetsGeometry.DirectionalOnly(start: theme.UseMaterial3 ? 0 : 4),
            new Row(children: actionChildren));

        double tapTargetHeightOffset = theme.MaterialTapTargetSize == MaterialTapTargetSize.ShrinkWrap ? -12 : 0;
        var dialogSize = DialogSize(context, defaultTheme, theme.UseMaterial3);
        var minDialogSize = MinDialogSize(context, defaultTheme, theme.UseMaterial3);
        dialogSize = new Size(dialogSize.Width, dialogSize.Height + tapTargetHeightOffset);
        minDialogSize = new Size(minDialogSize.Width, minDialogSize.Height + tapTargetHeightOffset);

        Widget body = new TimePickerBody(
            time: Current.InitialTime,
            onTimeChanged: HandleTimeChanged,
            helpText: Current.HelpText,
            errorInvalidText: Current.ErrorInvalidText,
            hourLabelText: Current.HourLabelText,
            minuteLabelText: Current.MinuteLabelText,
            entryMode: _entryMode,
            orientation: Current.Orientation,
            onEntryModeChanged: HandleEntryModeChanged,
            emptyInitialInput: Current.EmptyInitialInput);
        body = new Form(key: _formKey, autovalidateMode: _autovalidateMode, child: body);
        if (!inputMode) body = new Flexible(body);

        return new Dialog(
            shape: shape,
            elevation: pickerTheme.Elevation ?? defaultTheme.Elevation,
            backgroundColor: pickerTheme.BackgroundColor ?? defaultTheme.BackgroundColor,
            insetPadding: new Thickness(16, inputMode ? 0 : 24),
            child: new Padding(
                (pickerTheme.Padding ?? defaultTheme.Padding).Resolve(
                    Directionality.MaybeOf(context) ?? TextDirection.Ltr),
                new LayoutBuilder((_, constraints) =>
                {
                    var constrainedSize = constraints.Constrain(dialogSize);
                    var allowedSize = new Size(
                        Math.Max(constrainedSize.Width, minDialogSize.Width),
                        Math.Max(constrainedSize.Height, minDialogSize.Height));
                    return new SingleChildScrollView(
                        scrollDirection: Axis.Horizontal,
                        child: new SingleChildScrollView(
                            child: new AnimatedContainer(
                                duration: TimePickerConstants.DialogSizeAnimationDuration,
                                curve: Curves.EaseIn,
                                width: allowedSize.Width,
                                constraints: new BoxConstraints(
                                    MinHeight: InputMinimumHeight,
                                    MaxHeight: allowedSize.Height),
                                child: new Column(
                                    mainAxisSize: MainAxisSize.Min,
                                    mainAxisAlignment: MainAxisAlignment.SpaceBetween,
                                    crossAxisAlignment: CrossAxisAlignment.Start,
                                    children: [body, actions]))));
                })));
    }
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
        Icon? switchToInputEntryModeIcon = null,
        Icon? switchToTimerEntryModeIcon = null,
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
