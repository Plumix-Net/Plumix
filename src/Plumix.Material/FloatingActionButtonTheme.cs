using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/floating_action_button_theme.dart

public sealed record FloatingActionButtonThemeData(
    Color? ForegroundColor = null,
    Color? BackgroundColor = null,
    Color? FocusColor = null,
    Color? HoverColor = null,
    Color? SplashColor = null,
    double? Elevation = null,
    double? FocusElevation = null,
    double? HoverElevation = null,
    double? DisabledElevation = null,
    double? HighlightElevation = null,
    ShapeBorder? Shape = null,
    bool? EnableFeedback = null,
    double? IconSize = null,
    BoxConstraints? SizeConstraints = null,
    BoxConstraints? SmallSizeConstraints = null,
    BoxConstraints? LargeSizeConstraints = null,
    BoxConstraints? ExtendedSizeConstraints = null,
    double? ExtendedIconLabelSpacing = null,
    Thickness? ExtendedPadding = null,
    TextStyle? ExtendedTextStyle = null,
    MaterialStateProperty<MouseCursor?>? MouseCursor = null)
{
    public FloatingActionButtonThemeData CopyWith(
        Color? foregroundColor = null,
        Color? backgroundColor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        Color? splashColor = null,
        double? elevation = null,
        double? focusElevation = null,
        double? hoverElevation = null,
        double? disabledElevation = null,
        double? highlightElevation = null,
        ShapeBorder? shape = null,
        bool? enableFeedback = null,
        double? iconSize = null,
        BoxConstraints? sizeConstraints = null,
        BoxConstraints? smallSizeConstraints = null,
        BoxConstraints? largeSizeConstraints = null,
        BoxConstraints? extendedSizeConstraints = null,
        double? extendedIconLabelSpacing = null,
        Thickness? extendedPadding = null,
        TextStyle? extendedTextStyle = null,
        MaterialStateProperty<MouseCursor?>? mouseCursor = null)
    {
        return new FloatingActionButtonThemeData(
            ForegroundColor: foregroundColor ?? ForegroundColor,
            BackgroundColor: backgroundColor ?? BackgroundColor,
            FocusColor: focusColor ?? FocusColor,
            HoverColor: hoverColor ?? HoverColor,
            SplashColor: splashColor ?? SplashColor,
            Elevation: elevation ?? Elevation,
            FocusElevation: focusElevation ?? FocusElevation,
            HoverElevation: hoverElevation ?? HoverElevation,
            DisabledElevation: disabledElevation ?? DisabledElevation,
            HighlightElevation: highlightElevation ?? HighlightElevation,
            Shape: shape ?? Shape,
            EnableFeedback: enableFeedback ?? EnableFeedback,
            IconSize: iconSize ?? IconSize,
            SizeConstraints: sizeConstraints ?? SizeConstraints,
            SmallSizeConstraints: smallSizeConstraints ?? SmallSizeConstraints,
            LargeSizeConstraints: largeSizeConstraints ?? LargeSizeConstraints,
            ExtendedSizeConstraints: extendedSizeConstraints ?? ExtendedSizeConstraints,
            ExtendedIconLabelSpacing: extendedIconLabelSpacing ?? ExtendedIconLabelSpacing,
            ExtendedPadding: extendedPadding ?? ExtendedPadding,
            ExtendedTextStyle: extendedTextStyle ?? ExtendedTextStyle,
            MouseCursor: mouseCursor ?? MouseCursor);
    }

    public static FloatingActionButtonThemeData? Lerp(
        FloatingActionButtonThemeData? a,
        FloatingActionButtonThemeData? b,
        double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        double clampedT = Math.Clamp(t, 0.0, 1.0);
        return new FloatingActionButtonThemeData(
            ForegroundColor: MaterialThemeLerp.Color(a?.ForegroundColor, b?.ForegroundColor, clampedT),
            BackgroundColor: MaterialThemeLerp.Color(a?.BackgroundColor, b?.BackgroundColor, clampedT),
            FocusColor: MaterialThemeLerp.Color(a?.FocusColor, b?.FocusColor, clampedT),
            HoverColor: MaterialThemeLerp.Color(a?.HoverColor, b?.HoverColor, clampedT),
            SplashColor: MaterialThemeLerp.Color(a?.SplashColor, b?.SplashColor, clampedT),
            Elevation: MaterialThemeLerp.Double(a?.Elevation, b?.Elevation, clampedT),
            FocusElevation: MaterialThemeLerp.Double(a?.FocusElevation, b?.FocusElevation, clampedT),
            HoverElevation: MaterialThemeLerp.Double(a?.HoverElevation, b?.HoverElevation, clampedT),
            DisabledElevation: MaterialThemeLerp.Double(a?.DisabledElevation, b?.DisabledElevation, clampedT),
            HighlightElevation: MaterialThemeLerp.Double(a?.HighlightElevation, b?.HighlightElevation, clampedT),
            Shape: MaterialThemeLerp.Shape(a?.Shape, b?.Shape, clampedT),
            EnableFeedback: clampedT < 0.5 ? a?.EnableFeedback : b?.EnableFeedback,
            IconSize: MaterialThemeLerp.Double(a?.IconSize, b?.IconSize, clampedT),
            SizeConstraints: MaterialThemeLerp.BoxConstraints(
                a?.SizeConstraints,
                b?.SizeConstraints,
                clampedT),
            SmallSizeConstraints: MaterialThemeLerp.BoxConstraints(
                a?.SmallSizeConstraints,
                b?.SmallSizeConstraints,
                clampedT),
            LargeSizeConstraints: MaterialThemeLerp.BoxConstraints(
                a?.LargeSizeConstraints,
                b?.LargeSizeConstraints,
                clampedT),
            ExtendedSizeConstraints: MaterialThemeLerp.BoxConstraints(
                a?.ExtendedSizeConstraints,
                b?.ExtendedSizeConstraints,
                clampedT),
            ExtendedIconLabelSpacing: MaterialThemeLerp.Double(
                a?.ExtendedIconLabelSpacing,
                b?.ExtendedIconLabelSpacing,
                clampedT),
            ExtendedPadding: MaterialThemeLerp.Thickness(
                a?.ExtendedPadding,
                b?.ExtendedPadding,
                clampedT),
            ExtendedTextStyle: MaterialThemeLerp.TextStyle(
                a?.ExtendedTextStyle,
                b?.ExtendedTextStyle,
                clampedT),
            MouseCursor: clampedT < 0.5 ? a?.MouseCursor : b?.MouseCursor);
    }
}

public sealed class FloatingActionButtonTheme : InheritedTheme
{
    public FloatingActionButtonTheme(
        FloatingActionButtonThemeData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public FloatingActionButtonThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new FloatingActionButtonTheme(Data, child);
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((FloatingActionButtonTheme)oldWidget).Data, Data);
    }

    public static FloatingActionButtonThemeData Of(BuildContext context)
    {
        return context.DependOnInherited<FloatingActionButtonTheme>()?.Data
               ?? Theme.Of(context).FloatingActionButtonTheme;
    }
}
