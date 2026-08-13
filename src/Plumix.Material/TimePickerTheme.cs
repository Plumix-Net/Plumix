using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/time_picker_theme.dart

public sealed partial record TimePickerThemeData(
    Color? BackgroundColor = null,
    ButtonStyle? CancelButtonStyle = null,
    ButtonStyle? ConfirmButtonStyle = null,
    BorderSide? DayPeriodBorderSide = null,
    WidgetStateColor? DayPeriodColor = null,
    OutlinedBorder? DayPeriodShape = null,
    WidgetStateColor? DayPeriodTextColor = null,
    TextStyle? DayPeriodTextStyle = null,
    Color? DialBackgroundColor = null,
    Color? DialHandColor = null,
    WidgetStateColor? DialTextColor = null,
    TextStyle? DialTextStyle = null,
    double? Elevation = null,
    Color? EntryModeIconColor = null,
    TextStyle? HelpTextStyle = null,
    WidgetStateColor? HourMinuteColor = null,
    ShapeBorder? HourMinuteShape = null,
    WidgetStateColor? HourMinuteTextColor = null,
    TextStyle? HourMinuteTextStyle = null,
    InputDecorationThemeData? InputDecorationTheme = null,
    EdgeInsetsGeometry? Padding = null,
    ShapeBorder? Shape = null,
    MaterialStateProperty<Color?>? TimeSelectorSeparatorColor = null,
    MaterialStateProperty<TextStyle?>? TimeSelectorSeparatorTextStyle = null)
{
    private readonly WidgetStateColor? _dayPeriodColor = DayPeriodColor;

    /// Mirrors Dart's `dayPeriodColor` getter: a plain `Color` (one that reached this property through
    /// the implicit `Color` -> `WidgetStateColor` conversion) is wrapped so it only applies while
    /// selected and resolves to transparent otherwise. An explicitly built state color passes through.
    public WidgetStateColor? DayPeriodColor
    {
        get
        {
            if (_dayPeriodColor is null || !_dayPeriodColor.IsConstantColor)
            {
                return _dayPeriodColor;
            }

            var constant = _dayPeriodColor.DefaultValue;
            return WidgetStateColor.ResolveWith(
                constant,
                states => states.Contains(WidgetState.Selected) ? constant : Colors.Transparent);
        }
        init => _dayPeriodColor = value;
    }
}

public sealed class TimePickerTheme : InheritedTheme
{
    public TimePickerTheme(TimePickerThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public TimePickerThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    public override Widget Wrap(BuildContext context, Widget child) => new TimePickerTheme(Data, child);

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(Data, ((TimePickerTheme)oldWidget).Data);

    public static TimePickerThemeData Of(BuildContext context) =>
        context.DependOnInherited<TimePickerTheme>()?.Data ?? Theme.Of(context).TimePickerTheme;
}

/// The non-nullable default surface Flutter models as `_TimePickerDefaults`. It also carries the
/// sizing members that never made it onto `TimePickerThemeData`.
internal abstract class TimePickerDefaults
{
    public abstract Color BackgroundColor { get; }

    public abstract ButtonStyle CancelButtonStyle { get; }

    public abstract ButtonStyle ConfirmButtonStyle { get; }

    public abstract BorderSide DayPeriodBorderSide { get; }

    public abstract WidgetStateColor DayPeriodColor { get; }

    public abstract OutlinedBorder DayPeriodShape { get; }

    public abstract Size DayPeriodInputSize { get; }

    public abstract Size DayPeriodLandscapeSize { get; }

    public abstract Size DayPeriodPortraitSize { get; }

    public abstract WidgetStateColor DayPeriodTextColor { get; }

    public abstract TextStyle DayPeriodTextStyle { get; }

    public abstract Color DialBackgroundColor { get; }

    public abstract Color DialHandColor { get; }

    public abstract Size DialSize { get; }

    public abstract double HandWidth { get; }

    public abstract double DotRadius { get; }

    public abstract double CenterRadius { get; }

    public abstract WidgetStateColor DialTextColor { get; }

    public abstract TextStyle DialTextStyle { get; }

    public abstract double Elevation { get; }

    public abstract Color EntryModeIconColor { get; }

    public abstract TextStyle HelpTextStyle { get; }

    public abstract WidgetStateColor HourMinuteColor { get; }

    public abstract ShapeBorder HourMinuteShape { get; }

    public abstract Size HourMinuteSize { get; }

    public abstract Size HourMinuteSize24Hour { get; }

    public abstract Size HourMinuteInputSize { get; }

    public abstract Size HourMinuteInputSize24Hour { get; }

    public abstract WidgetStateColor HourMinuteTextColor { get; }

    public abstract TextStyle HourMinuteTextStyle { get; }

    public abstract InputDecorationThemeData InputDecorationTheme { get; }

    public abstract EdgeInsetsGeometry Padding { get; }

    public abstract ShapeBorder Shape { get; }

    /// Null for Material 2, matching Dart, where only `_TimePickerDefaultsM3` overrides it.
    public virtual MaterialStateProperty<Color?>? TimeSelectorSeparatorColor => null;

    /// Null for Material 2, matching Dart, where only `_TimePickerDefaultsM3` overrides it.
    public virtual MaterialStateProperty<TextStyle?>? TimeSelectorSeparatorTextStyle => null;

    internal static Color WithOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Round(255 * Math.Clamp(opacity, 0, 1)),
        color.R,
        color.G,
        color.B);

    internal static Color AlphaBlend(Color foreground, Color background)
    {
        int alpha = foreground.A;
        if (alpha == 0) return background;
        if (alpha == 255) return foreground;
        int invAlpha = 255 - alpha;
        int backAlpha = background.A;
        if (backAlpha == 255)
        {
            return Color.FromArgb(
                255,
                (byte)((alpha * foreground.R + invAlpha * background.R) / 255),
                (byte)((alpha * foreground.G + invAlpha * background.G) / 255),
                (byte)((alpha * foreground.B + invAlpha * background.B) / 255));
        }

        backAlpha = backAlpha * invAlpha / 255;
        int outAlpha = alpha + backAlpha;
        return Color.FromArgb(
            (byte)outAlpha,
            (byte)((foreground.R * alpha + background.R * backAlpha) / outAlpha),
            (byte)((foreground.G * alpha + background.G * backAlpha) / outAlpha),
            (byte)((foreground.B * alpha + background.B * backAlpha) / outAlpha));
    }
}

// Dart parity source: material_ui/lib/src/time_picker.dart (_TimePickerDefaultsM2)
internal sealed class TimePickerDefaultsM2 : TimePickerDefaults
{
    private static readonly OutlinedBorder DefaultShape = new RoundedRectangleBorder(
        borderRadius: Plumix.Rendering.BorderRadius.Circular(4));

    private readonly ColorScheme _colors;
    private readonly TextTheme _textTheme;

    public TimePickerDefaultsM2(BuildContext context)
    {
        var theme = Theme.Of(context);
        _colors = theme.ColorScheme;
        _textTheme = theme.TextTheme;
    }

    public override Color BackgroundColor => _colors.Surface;

    public override ButtonStyle CancelButtonStyle => TextButton.StyleFrom();

    public override ButtonStyle ConfirmButtonStyle => TextButton.StyleFrom();

    public override BorderSide DayPeriodBorderSide =>
        new(AlphaBlend(WithOpacity(_colors.OnSurface, 0.38), _colors.Surface));

    public override WidgetStateColor DayPeriodColor => WidgetStateColor.ResolveWith(
        Colors.Transparent,
        states => states.Contains(WidgetState.Selected)
            ? WithOpacity(_colors.Primary, _colors.Brightness == Brightness.Dark ? 0.24 : 0.12)
            : Colors.Transparent);

    public override OutlinedBorder DayPeriodShape => DefaultShape;

    public override Size DayPeriodPortraitSize => new(52, 80);

    public override Size DayPeriodLandscapeSize => new(0, 40);

    public override Size DayPeriodInputSize => new(52, 70);

    public override WidgetStateColor DayPeriodTextColor => WidgetStateColor.ResolveWith(
        WithOpacity(_colors.OnSurface, 0.60),
        states => states.Contains(WidgetState.Selected)
            ? _colors.Primary
            : WithOpacity(_colors.OnSurface, 0.60));

    public override TextStyle DayPeriodTextStyle => _textTheme.TitleMedium;

    public override Color DialBackgroundColor =>
        WithOpacity(_colors.OnSurface, _colors.Brightness == Brightness.Dark ? 0.12 : 0.08);

    public override Color DialHandColor => _colors.Primary;

    public override Size DialSize => new(280, 280);

    public override double HandWidth => 2;

    public override double DotRadius => 22;

    public override double CenterRadius => 4;

    public override WidgetStateColor DialTextColor => WidgetStateColor.ResolveWith(
        _colors.OnSurface,
        states => states.Contains(WidgetState.Selected) ? _colors.Surface : _colors.OnSurface);

    public override TextStyle DialTextStyle => _textTheme.BodyLarge;

    public override double Elevation => 6;

    public override Color EntryModeIconColor =>
        WithOpacity(_colors.OnSurface, _colors.Brightness == Brightness.Dark ? 1.0 : 0.6);

    public override TextStyle HelpTextStyle => _textTheme.LabelSmall;

    public override WidgetStateColor HourMinuteColor => WidgetStateColor.ResolveWith(
        WithOpacity(_colors.OnSurface, 0.12),
        states => states.Contains(WidgetState.Selected)
            ? WithOpacity(_colors.Primary, _colors.Brightness == Brightness.Dark ? 0.24 : 0.12)
            : WithOpacity(_colors.OnSurface, 0.12));

    public override ShapeBorder HourMinuteShape => DefaultShape;

    public override Size HourMinuteSize => new(96, 80);

    public override Size HourMinuteSize24Hour => new(114, 80);

    public override Size HourMinuteInputSize => new(96, 70);

    public override Size HourMinuteInputSize24Hour => new(114, 70);

    public override WidgetStateColor HourMinuteTextColor => WidgetStateColor.ResolveWith(
        _colors.OnSurface,
        states => states.Contains(WidgetState.Selected) ? _colors.Primary : _colors.OnSurface);

    public override TextStyle HourMinuteTextStyle => _textTheme.DisplayMedium;

    private WidgetStateColor HourMinuteInputColor => WidgetStateColor.ResolveWith(
        WithOpacity(_colors.OnSurface, 0.12),
        states => states.Contains(WidgetState.Selected)
            ? Colors.Transparent
            : WithOpacity(_colors.OnSurface, 0.12));

    public override InputDecorationThemeData InputDecorationTheme => new(
        ContentPadding: EdgeInsetsGeometry.Zero,
        Filled: true,
        FillColor: HourMinuteInputColor.DefaultValue,
        FocusColor: Colors.Transparent,
        EnabledBorder: new OutlineInputBorder(borderSide: new BorderSide(Colors.Transparent)),
        ErrorBorder: new OutlineInputBorder(borderSide: new BorderSide(_colors.Error, 2)),
        FocusedBorder: new OutlineInputBorder(borderSide: new BorderSide(_colors.Primary, 2)),
        FocusedErrorBorder: new OutlineInputBorder(borderSide: new BorderSide(_colors.Error, 2)),
        HintStyle: HourMinuteTextStyle.CopyWith(color: WithOpacity(_colors.OnSurface, 0.36)),
        ErrorStyle: new TextStyle(FontSize: 0, Height: 1));

    public override EdgeInsetsGeometry Padding => EdgeInsetsGeometry.FromLTRB(8, 18, 8, 8);

    public override ShapeBorder Shape => DefaultShape;
}

// Dart parity source: material_ui/lib/src/time_picker.dart (_TimePickerDefaultsM3)
internal sealed class TimePickerDefaultsM3 : TimePickerDefaults
{
    private readonly ColorScheme _colors;
    private readonly TextTheme _textTheme;
    private readonly TimePickerEntryMode _entryMode;

    public TimePickerDefaultsM3(BuildContext context, TimePickerEntryMode entryMode = TimePickerEntryMode.Dial)
    {
        var theme = Theme.Of(context);
        _colors = theme.ColorScheme;
        _textTheme = theme.TextTheme;
        _entryMode = entryMode;
    }

    public override Color BackgroundColor => _colors.SurfaceContainerHigh;

    public override ButtonStyle CancelButtonStyle => TextButton.StyleFrom();

    public override ButtonStyle ConfirmButtonStyle => TextButton.StyleFrom();

    public override BorderSide DayPeriodBorderSide => new(_colors.Outline);

    public override WidgetStateColor DayPeriodColor => WidgetStateColor.ResolveWith(
        Colors.Transparent,
        states => states.Contains(WidgetState.Selected) ? _colors.TertiaryContainer : Colors.Transparent);

    public override OutlinedBorder DayPeriodShape =>
        new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(8))
            .CopyWith(side: DayPeriodBorderSide);

    public override Size DayPeriodPortraitSize => new(52, 80);

    public override Size DayPeriodLandscapeSize => new(216, 38);

    // Input size is eight pixels smaller than the portrait size in the spec, but there's no token yet.
    public override Size DayPeriodInputSize =>
        new(DayPeriodPortraitSize.Width, DayPeriodPortraitSize.Height - 8);

    public override WidgetStateColor DayPeriodTextColor => WidgetStateColor.ResolveWith(
        _colors.OnSurfaceVariant,
        states => states.Contains(WidgetState.Selected)
            ? _colors.OnTertiaryContainer
            : _colors.OnSurfaceVariant);

    public override TextStyle DayPeriodTextStyle => _textTheme.TitleMedium;

    public override Color DialBackgroundColor => _colors.SurfaceContainerHighest;

    public override Color DialHandColor => _colors.Primary;

    public override Size DialSize => new(256, 256);

    public override double HandWidth => 2;

    public override double DotRadius => 24;

    public override double CenterRadius => 4;

    public override WidgetStateColor DialTextColor => WidgetStateColor.ResolveWith(
        _colors.OnSurface,
        states => states.Contains(WidgetState.Selected) ? _colors.OnPrimary : _colors.OnSurface);

    public override TextStyle DialTextStyle => _textTheme.BodyLarge;

    public override double Elevation => 6;

    public override Color EntryModeIconColor => _colors.OnSurface;

    public override TextStyle HelpTextStyle => _textTheme.LabelMedium.CopyWith(color: _colors.OnSurfaceVariant);

    public override WidgetStateColor HourMinuteColor => WidgetStateColor.ResolveWith(
        _colors.SurfaceContainerHighest,
        states =>
        {
            if (states.Contains(WidgetState.Selected))
            {
                var selectedOverlay = _colors.PrimaryContainer;
                if (states.Contains(WidgetState.Pressed))
                {
                    selectedOverlay = _colors.OnPrimaryContainer;
                }
                else if (states.Contains(WidgetState.Hovered))
                {
                    selectedOverlay = WithOpacity(_colors.OnPrimaryContainer, 0.08);
                }
                else if (states.Contains(WidgetState.Focused))
                {
                    selectedOverlay = WithOpacity(_colors.OnPrimaryContainer, 0.1);
                }

                return AlphaBlend(selectedOverlay, _colors.PrimaryContainer);
            }

            var overlay = _colors.SurfaceContainerHighest;
            if (states.Contains(WidgetState.Pressed))
            {
                overlay = _colors.OnSurface;
            }
            else if (states.Contains(WidgetState.Hovered))
            {
                overlay = WithOpacity(_colors.OnSurface, 0.08);
            }
            else if (states.Contains(WidgetState.Focused))
            {
                overlay = WithOpacity(_colors.OnSurface, 0.1);
            }

            return AlphaBlend(overlay, _colors.SurfaceContainerHighest);
        });

    public override ShapeBorder HourMinuteShape =>
        new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(8));

    public override Size HourMinuteSize => new(96, 80);

    public override Size HourMinuteSize24Hour => new(114, HourMinuteSize.Height);

    public override Size HourMinuteInputSize => new(HourMinuteSize.Width, HourMinuteSize.Height - 8);

    public override Size HourMinuteInputSize24Hour =>
        new(HourMinuteSize24Hour.Width, HourMinuteSize24Hour.Height - 8);

    public override WidgetStateColor HourMinuteTextColor => WidgetStateColor.ResolveWith(
        _colors.OnSurface,
        states => states.Contains(WidgetState.Selected) ? _colors.OnPrimaryContainer : _colors.OnSurface);

    public override TextStyle HourMinuteTextStyle => _entryMode switch
    {
        TimePickerEntryMode.Dial or TimePickerEntryMode.DialOnly => _textTheme.DisplayLarge,
        _ => _textTheme.DisplayMedium,
    };

    // This is NOT correct, but there's no token for 'time-input.container.shape', so this reuses the
    // radius from the hour/minute selector shape, exactly as Dart does.
    public override InputDecorationThemeData InputDecorationTheme => new(
        ContentPadding: EdgeInsetsGeometry.Zero,
        Filled: true,
        FillColor: HourMinuteColor.DefaultValue,
        FocusColor: _colors.PrimaryContainer,
        EnabledBorder: new OutlineInputBorder(
            borderRadius: Plumix.Rendering.BorderRadius.Circular(8),
            borderSide: new BorderSide(Colors.Transparent)),
        ErrorBorder: new OutlineInputBorder(
            borderRadius: Plumix.Rendering.BorderRadius.Circular(8),
            borderSide: new BorderSide(_colors.Error, 2)),
        FocusedBorder: new OutlineInputBorder(
            borderRadius: Plumix.Rendering.BorderRadius.Circular(8),
            borderSide: new BorderSide(_colors.Primary, 2)),
        FocusedErrorBorder: new OutlineInputBorder(
            borderRadius: Plumix.Rendering.BorderRadius.Circular(8),
            borderSide: new BorderSide(_colors.Error, 2)),
        HintStyle: HourMinuteTextStyle.CopyWith(color: WithOpacity(_colors.OnSurface, 0.36)),
        ErrorStyle: new TextStyle(FontSize: 0));

    public override EdgeInsetsGeometry Padding => EdgeInsetsGeometry.All(24);

    public override ShapeBorder Shape =>
        new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(28));

    // TODO(port): update when Material tokens expose these; taken from the Material 3 time picker spec.
    public override MaterialStateProperty<Color?>? TimeSelectorSeparatorColor =>
        MaterialStateProperty<Color?>.All(_colors.OnSurface);

    public override MaterialStateProperty<TextStyle?>? TimeSelectorSeparatorTextStyle =>
        MaterialStateProperty<TextStyle?>.All(_textTheme.DisplayLarge);
}
