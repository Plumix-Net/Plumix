using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/dialog_theme.dart

/// <summary>An inherited theme supplying defaults for descendant dialogs.</summary>
public sealed class DialogTheme : InheritedTheme
{
    public DialogTheme(
        DialogThemeData? data = null,
        Widget? child = null,
        Key? key = null,
        Color? backgroundColor = null,
        double? elevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        ShapeBorder? shape = null,
        AlignmentGeometry? alignment = null,
        Color? iconColor = null,
        TextStyle? titleTextStyle = null,
        TextStyle? contentTextStyle = null,
        EdgeInsetsGeometry? actionsPadding = null,
        Color? barrierColor = null,
        Thickness? insetPadding = null,
        Clip? clipBehavior = null) : base(child ?? new SizedBox(), key)
    {
        DebugAssertions.Assert(
            data is null || (backgroundColor is null
                              && elevation is null
                              && shadowColor is null
                              && surfaceTintColor is null
                              && shape is null
                              && alignment is null
                              && iconColor is null
                              && titleTextStyle is null
                              && contentTextStyle is null
                              && actionsPadding is null
                              && barrierColor is null
                              && insetPadding is null
                              && clipBehavior is null),
            "data cannot be combined with individual DialogTheme properties.");
        _data = data;
        _backgroundColor = backgroundColor;
        _elevation = elevation;
        _shadowColor = shadowColor;
        _surfaceTintColor = surfaceTintColor;
        _shape = shape;
        _alignment = alignment;
        _iconColor = iconColor;
        _titleTextStyle = titleTextStyle;
        _contentTextStyle = contentTextStyle;
        _actionsPadding = actionsPadding;
        _barrierColor = barrierColor;
        _insetPadding = insetPadding;
        _clipBehavior = clipBehavior;
    }

    // Retain the published positional data/child constructor.
    public DialogTheme(DialogThemeData? data, Widget? child, Key? key) : this(
        key: key,
        backgroundColor: null,
        data: data,
        child: child)
    {
    }

    private readonly DialogThemeData? _data;
    private readonly Color? _backgroundColor;
    private readonly double? _elevation;
    private readonly Color? _shadowColor;
    private readonly Color? _surfaceTintColor;
    private readonly ShapeBorder? _shape;
    private readonly AlignmentGeometry? _alignment;
    private readonly Color? _iconColor;
    private readonly TextStyle? _titleTextStyle;
    private readonly TextStyle? _contentTextStyle;
    private readonly EdgeInsetsGeometry? _actionsPadding;
    private readonly Color? _barrierColor;
    private readonly Thickness? _insetPadding;
    private readonly Clip? _clipBehavior;

    public Color? BackgroundColor => _data is not null ? _data.BackgroundColor : _backgroundColor;
    public double? Elevation => _data is not null ? _data.Elevation : _elevation;
    public Color? ShadowColor => _data is not null ? _data.ShadowColor : _shadowColor;
    public Color? SurfaceTintColor => _data is not null ? _data.SurfaceTintColor : _surfaceTintColor;
    public ShapeBorder? Shape => _data is not null ? _data.Shape : _shape;
    public AlignmentGeometry? Alignment => _data is not null ? _data.Alignment : _alignment;
    public Color? IconColor => _data is not null ? _data.IconColor : _iconColor;
    public TextStyle? TitleTextStyle => _data is not null ? _data.TitleTextStyle : _titleTextStyle;
    public TextStyle? ContentTextStyle => _data is not null ? _data.ContentTextStyle : _contentTextStyle;
    public EdgeInsetsGeometry? ActionsPadding => _data is not null ? _data.ActionsPadding : _actionsPadding;
    public Color? BarrierColor => _data is not null ? _data.BarrierColor : _barrierColor;
    public Thickness? InsetPadding => _data is not null ? _data.InsetPadding : _insetPadding;
    public Clip? ClipBehavior => _data is not null ? _data.ClipBehavior : _clipBehavior;

    public DialogThemeData Data => _data ?? new DialogThemeData(
        BackgroundColor: _backgroundColor,
        Elevation: _elevation,
        ShadowColor: _shadowColor,
        SurfaceTintColor: _surfaceTintColor,
        Shape: _shape,
        Alignment: _alignment,
        IconColor: _iconColor,
        TitleTextStyle: _titleTextStyle,
        ContentTextStyle: _contentTextStyle,
        ActionsPadding: _actionsPadding,
        BarrierColor: _barrierColor,
        InsetPadding: _insetPadding,
        ClipBehavior: _clipBehavior);

    public static DialogThemeData Of(BuildContext context) =>
        context.DependOnInheritedWidgetOfExactType<DialogTheme>()?.Data ?? Theme.Of(context).DialogTheme;

    public override Widget Wrap(BuildContext context, Widget child) => new DialogTheme(Data, child);

    public override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(Data, ((DialogTheme)oldWidget).Data);

    /// <summary>Legacy copy operation. Use DialogThemeData.CopyWith for new code.</summary>
    public DialogTheme CopyWith(
        Color? backgroundColor = null,
        double? elevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        ShapeBorder? shape = null,
        AlignmentGeometry? alignment = null,
        Color? iconColor = null,
        TextStyle? titleTextStyle = null,
        TextStyle? contentTextStyle = null,
        EdgeInsetsGeometry? actionsPadding = null,
        Color? barrierColor = null,
        Thickness? insetPadding = null,
        Clip? clipBehavior = null)
    {
        return new DialogTheme(
            backgroundColor: backgroundColor ?? BackgroundColor,
            elevation: elevation ?? Elevation,
            shadowColor: shadowColor ?? ShadowColor,
            surfaceTintColor: surfaceTintColor ?? SurfaceTintColor,
            shape: shape ?? Shape,
            alignment: alignment ?? Alignment,
            iconColor: iconColor ?? IconColor,
            titleTextStyle: titleTextStyle ?? TitleTextStyle,
            contentTextStyle: contentTextStyle ?? ContentTextStyle,
            actionsPadding: actionsPadding ?? ActionsPadding,
            barrierColor: barrierColor ?? BarrierColor,
            insetPadding: insetPadding ?? InsetPadding,
            clipBehavior: clipBehavior ?? ClipBehavior);
    }

    /// <summary>Legacy interpolation. Use DialogThemeData.Lerp for new code.</summary>
    public static DialogTheme Lerp(DialogTheme? a, DialogTheme? b, double t)
    {
        if (ReferenceEquals(a, b) && a is not null)
        {
            return a;
        }

        return new DialogTheme(
            backgroundColor: Color.Lerp(a?.BackgroundColor, b?.BackgroundColor, t),
            elevation: MaterialThemeLerp.Double(a?.Elevation, b?.Elevation, t),
            shadowColor: Color.Lerp(a?.ShadowColor, b?.ShadowColor, t),
            surfaceTintColor: Color.Lerp(a?.SurfaceTintColor, b?.SurfaceTintColor, t),
            shape: ShapeBorder.Lerp(a?.Shape, b?.Shape, t),
            alignment: AlignmentGeometry.Lerp(a?.Alignment, b?.Alignment, t),
            iconColor: Color.Lerp(a?.IconColor, b?.IconColor, t),
            titleTextStyle: TextStyle.Lerp(a?.TitleTextStyle, b?.TitleTextStyle, t),
            contentTextStyle: TextStyle.Lerp(a?.ContentTextStyle, b?.ContentTextStyle, t),
            actionsPadding: EdgeInsetsGeometry.Lerp(a?.ActionsPadding, b?.ActionsPadding, t),
            barrierColor: Color.Lerp(a?.BarrierColor, b?.BarrierColor, t),
            insetPadding: MaterialThemeLerp.Thickness(a?.InsetPadding, b?.InsetPadding, t),
            clipBehavior: t < 0.5 ? a?.ClipBehavior : b?.ClipBehavior);
    }

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        object nullDefault = DiagnosticsDefaults.NullValue;
        properties.Add(new ColorProperty("backgroundColor", BackgroundColor, defaultValue: nullDefault));
        properties.Add(new DoubleProperty("elevation", Elevation, defaultValue: nullDefault));
        properties.Add(new ColorProperty("shadowColor", ShadowColor, defaultValue: nullDefault));
        properties.Add(new ColorProperty("surfaceTintColor", SurfaceTintColor, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<ShapeBorder?>("shape", Shape, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<AlignmentGeometry?>("alignment", Alignment, defaultValue: nullDefault));
        properties.Add(new ColorProperty("iconColor", IconColor, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<TextStyle?>(
            "titleTextStyle",
            TitleTextStyle,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<TextStyle?>(
            "contentTextStyle",
            ContentTextStyle,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<EdgeInsetsGeometry?>(
            "actionsPadding",
            ActionsPadding,
            defaultValue: nullDefault));
        properties.Add(new ColorProperty("barrierColor", BarrierColor, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<Thickness?>("insetPadding", InsetPadding, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<Clip?>("clipBehavior", ClipBehavior, defaultValue: nullDefault));
    }
}

public sealed partial record DialogThemeData : IDiagnosticable
{
    public DialogThemeData(
        Color? BackgroundColor = null,
        double? Elevation = null,
        Color? ShadowColor = null,
        Color? SurfaceTintColor = null,
        ShapeBorder? Shape = null,
        AlignmentGeometry? Alignment = null,
        TextStyle? TitleTextStyle = null,
        TextStyle? ContentTextStyle = null,
        EdgeInsetsGeometry? ActionsPadding = null,
        Color? IconColor = null,
        Color? BarrierColor = null,
        Thickness? InsetPadding = null,
        Clip? ClipBehavior = null,
        BoxConstraints? Constraints = null)
    {
        this.BackgroundColor = BackgroundColor;
        this.Elevation = Elevation;
        this.ShadowColor = ShadowColor;
        this.SurfaceTintColor = SurfaceTintColor;
        this.Shape = Shape;
        this.Alignment = Alignment;
        this.TitleTextStyle = TitleTextStyle;
        this.ContentTextStyle = ContentTextStyle;
        this.ActionsPadding = ActionsPadding;
        this.IconColor = IconColor;
        this.BarrierColor = BarrierColor;
        this.InsetPadding = InsetPadding;
        this.ClipBehavior = ClipBehavior;
        this.Constraints = Constraints;
    }

    public Color? BackgroundColor { get; init; }
    public double? Elevation { get; init; }
    public Color? ShadowColor { get; init; }
    public Color? SurfaceTintColor { get; init; }
    public ShapeBorder? Shape { get; init; }
    public AlignmentGeometry? Alignment { get; init; }
    public TextStyle? TitleTextStyle { get; init; }
    public TextStyle? ContentTextStyle { get; init; }
    public EdgeInsetsGeometry? ActionsPadding { get; init; }
    public Color? IconColor { get; init; }
    public Color? BarrierColor { get; init; }
    public Thickness? InsetPadding { get; init; }
    public Clip? ClipBehavior { get; init; }
    public BoxConstraints? Constraints { get; init; }

    public DialogThemeData CopyWith(
        Color? backgroundColor = null,
        double? elevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        ShapeBorder? shape = null,
        AlignmentGeometry? alignment = null,
        Color? iconColor = null,
        TextStyle? titleTextStyle = null,
        TextStyle? contentTextStyle = null,
        EdgeInsetsGeometry? actionsPadding = null,
        Color? barrierColor = null,
        Thickness? insetPadding = null,
        Clip? clipBehavior = null,
        BoxConstraints? constraints = null)
    {
        return new DialogThemeData(
            BackgroundColor: backgroundColor ?? BackgroundColor,
            Elevation: elevation ?? Elevation,
            ShadowColor: shadowColor ?? ShadowColor,
            SurfaceTintColor: surfaceTintColor ?? SurfaceTintColor,
            Shape: shape ?? Shape,
            Alignment: alignment ?? Alignment,
            IconColor: iconColor ?? IconColor,
            TitleTextStyle: titleTextStyle ?? TitleTextStyle,
            ContentTextStyle: contentTextStyle ?? ContentTextStyle,
            ActionsPadding: actionsPadding ?? ActionsPadding,
            BarrierColor: barrierColor ?? BarrierColor,
            InsetPadding: insetPadding ?? InsetPadding,
            ClipBehavior: clipBehavior ?? ClipBehavior,
            Constraints: constraints ?? Constraints);
    }

    public static DialogThemeData Lerp(DialogThemeData? a, DialogThemeData? b, double t)
    {
        if (ReferenceEquals(a, b) && a is not null)
        {
            return a;
        }

        return new DialogThemeData(
            BackgroundColor: Color.Lerp(a?.BackgroundColor, b?.BackgroundColor, t),
            Elevation: MaterialThemeLerp.Double(a?.Elevation, b?.Elevation, t),
            ShadowColor: Color.Lerp(a?.ShadowColor, b?.ShadowColor, t),
            SurfaceTintColor: Color.Lerp(a?.SurfaceTintColor, b?.SurfaceTintColor, t),
            Shape: ShapeBorder.Lerp(a?.Shape, b?.Shape, t),
            Alignment: AlignmentGeometry.Lerp(a?.Alignment, b?.Alignment, t),
            IconColor: Color.Lerp(a?.IconColor, b?.IconColor, t),
            TitleTextStyle: TextStyle.Lerp(a?.TitleTextStyle, b?.TitleTextStyle, t),
            ContentTextStyle: TextStyle.Lerp(a?.ContentTextStyle, b?.ContentTextStyle, t),
            ActionsPadding: EdgeInsetsGeometry.Lerp(a?.ActionsPadding, b?.ActionsPadding, t),
            BarrierColor: Color.Lerp(a?.BarrierColor, b?.BarrierColor, t),
            InsetPadding: MaterialThemeLerp.Thickness(a?.InsetPadding, b?.InsetPadding, t),
            ClipBehavior: t < 0.5 ? a?.ClipBehavior : b?.ClipBehavior,
            Constraints: BoxConstraints.Lerp(a?.Constraints, b?.Constraints, t));
    }

    public void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        object nullDefault = DiagnosticsDefaults.NullValue;
        properties.Add(new ColorProperty("backgroundColor", BackgroundColor, defaultValue: nullDefault));
        properties.Add(new DoubleProperty("elevation", Elevation, defaultValue: nullDefault));
        properties.Add(new ColorProperty("shadowColor", ShadowColor, defaultValue: nullDefault));
        properties.Add(new ColorProperty("surfaceTintColor", SurfaceTintColor, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<ShapeBorder?>("shape", Shape, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<AlignmentGeometry?>("alignment", Alignment, defaultValue: nullDefault));
        properties.Add(new ColorProperty("iconColor", IconColor, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<TextStyle?>(
            "titleTextStyle",
            TitleTextStyle,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<TextStyle?>(
            "contentTextStyle",
            ContentTextStyle,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<EdgeInsetsGeometry?>(
            "actionsPadding",
            ActionsPadding,
            defaultValue: nullDefault));
        properties.Add(new ColorProperty("barrierColor", BarrierColor, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<Thickness?>("insetPadding", InsetPadding, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<Clip?>("clipBehavior", ClipBehavior, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<BoxConstraints?>("constraints", Constraints, defaultValue: nullDefault));
    }

    // Dialog itself validates its insets; theme data accepts values without validation, as in Dart.
    internal static void ValidateInsets(Thickness? value, string parameterName)
    {
        if (!value.HasValue) return;
        var insets = value.Value;
        if (!double.IsFinite(insets.Left) || !double.IsFinite(insets.Top)
            || !double.IsFinite(insets.Right) || !double.IsFinite(insets.Bottom)
            || insets.Left < 0 || insets.Top < 0 || insets.Right < 0 || insets.Bottom < 0)
            throw new ArgumentOutOfRangeException(parameterName);
    }
}
