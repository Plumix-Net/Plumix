using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/floating_action_button.dart

internal enum FloatingActionButtonType
{
    Regular,
    Small,
    Large,
    Extended,
}

public sealed class FloatingActionButton : StatelessWidget
{
    private sealed class DefaultHeroTag
    {
        public static readonly DefaultHeroTag Instance = new();

        private DefaultHeroTag()
        {
        }

        public override string ToString() => "<default FloatingActionButton tag>";
    }

    public FloatingActionButton(
        Widget? child,
        Action? onPressed,
        string? tooltip = null,
        object? heroTag = null,
        MouseCursor? mouseCursor = null,
        Color? foregroundColor = null,
        Color? backgroundColor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        Color? splashColor = null,
        double? elevation = null,
        double? focusElevation = null,
        double? hoverElevation = null,
        double? highlightElevation = null,
        double? disabledElevation = null,
        bool mini = false,
        ShapeBorder? shape = null,
        FocusNode? focusNode = null,
        bool? enableFeedback = null,
        Clip clipBehavior = Clip.None,
        bool autofocus = false,
        MaterialTapTargetSize? materialTapTargetSize = null,
        bool isExtended = false,
        Key? key = null,
        [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(heroTag))]
        string? heroTagExpression = null) : this(
            child: child,
            tooltip: tooltip,
            extendedLabel: null,
            onPressed: onPressed,
            type: mini ? FloatingActionButtonType.Small : FloatingActionButtonType.Regular,
            isExtended: isExtended,
            heroTag: heroTagExpression is null ? DefaultHeroTag.Instance : heroTag,
            mouseCursor: mouseCursor,
            foregroundColor: foregroundColor,
            backgroundColor: backgroundColor,
            focusColor: focusColor,
            hoverColor: hoverColor,
            splashColor: splashColor,
            elevation: elevation,
            focusElevation: focusElevation,
            hoverElevation: hoverElevation,
            highlightElevation: highlightElevation,
            disabledElevation: disabledElevation,
            shape: shape,
            focusNode: focusNode,
            enableFeedback: enableFeedback,
            clipBehavior: clipBehavior,
            autofocus: autofocus,
            materialTapTargetSize: materialTapTargetSize,
            extendedIconLabelSpacing: null,
            extendedPadding: null,
            extendedTextStyle: null,
            key: key)
    {
    }

    private FloatingActionButton(
        Widget? child,
        string? tooltip,
        Widget? extendedLabel,
        Action? onPressed,
        FloatingActionButtonType type,
        bool isExtended,
        object? heroTag,
        MouseCursor? mouseCursor,
        Color? foregroundColor,
        Color? backgroundColor,
        Color? focusColor,
        Color? hoverColor,
        Color? splashColor,
        double? elevation,
        double? focusElevation,
        double? hoverElevation,
        double? highlightElevation,
        double? disabledElevation,
        ShapeBorder? shape,
        FocusNode? focusNode,
        bool? enableFeedback,
        Clip clipBehavior,
        bool autofocus,
        MaterialTapTargetSize? materialTapTargetSize,
        double? extendedIconLabelSpacing,
        Thickness? extendedPadding,
        TextStyle? extendedTextStyle,
        Key? key) : base(key)
    {
        ValidateElevation(nameof(elevation), elevation);
        ValidateElevation(nameof(focusElevation), focusElevation);
        ValidateElevation(nameof(hoverElevation), hoverElevation);
        ValidateElevation(nameof(highlightElevation), highlightElevation);
        ValidateElevation(nameof(disabledElevation), disabledElevation);
        if (extendedIconLabelSpacing.HasValue
            && (double.IsNaN(extendedIconLabelSpacing.Value)
                || double.IsInfinity(extendedIconLabelSpacing.Value)
                || extendedIconLabelSpacing.Value < 0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(extendedIconLabelSpacing),
                "Extended icon-label spacing must be finite and non-negative.");
        }

        Child = child;
        Tooltip = tooltip;
        ExtendedLabel = extendedLabel;
        OnPressed = onPressed;
        Type = type;
        Mini = type == FloatingActionButtonType.Small;
        IsExtended = isExtended;
        HeroTag = heroTag;
        MouseCursor = mouseCursor;
        ForegroundColor = foregroundColor;
        BackgroundColor = backgroundColor;
        FocusColor = focusColor;
        HoverColor = hoverColor;
        SplashColor = splashColor;
        Elevation = elevation;
        FocusElevation = focusElevation;
        HoverElevation = hoverElevation;
        HighlightElevation = highlightElevation;
        DisabledElevation = disabledElevation;
        Shape = shape;
        FocusNode = focusNode;
        EnableFeedback = enableFeedback;
        ClipBehavior = clipBehavior;
        Autofocus = autofocus;
        MaterialTapTargetSize = materialTapTargetSize;
        ExtendedIconLabelSpacing = extendedIconLabelSpacing;
        ExtendedPadding = extendedPadding;
        ExtendedTextStyle = extendedTextStyle;
    }

    public Widget? Child { get; }

    public string? Tooltip { get; }

    private Widget? ExtendedLabel { get; }

    public Action? OnPressed { get; }

    private FloatingActionButtonType Type { get; }

    public bool Mini { get; }

    public bool IsExtended { get; }

    public object? HeroTag { get; }

    public MouseCursor? MouseCursor { get; }

    public Color? ForegroundColor { get; }

    public Color? BackgroundColor { get; }

    public Color? FocusColor { get; }

    public Color? HoverColor { get; }

    public Color? SplashColor { get; }

    public double? Elevation { get; }

    public double? FocusElevation { get; }

    public double? HoverElevation { get; }

    public double? HighlightElevation { get; }

    public double? DisabledElevation { get; }

    public ShapeBorder? Shape { get; }

    public FocusNode? FocusNode { get; }

    public bool? EnableFeedback { get; }

    public Clip ClipBehavior { get; }

    public bool Autofocus { get; }

    public MaterialTapTargetSize? MaterialTapTargetSize { get; }

    public double? ExtendedIconLabelSpacing { get; }

    public Thickness? ExtendedPadding { get; }

    public TextStyle? ExtendedTextStyle { get; }

    public static FloatingActionButton Small(
        Widget? child,
        Action? onPressed,
        string? tooltip = null,
        object? heroTag = null,
        MouseCursor? mouseCursor = null,
        Color? foregroundColor = null,
        Color? backgroundColor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        Color? splashColor = null,
        double? elevation = null,
        double? focusElevation = null,
        double? hoverElevation = null,
        double? highlightElevation = null,
        double? disabledElevation = null,
        ShapeBorder? shape = null,
        FocusNode? focusNode = null,
        bool? enableFeedback = null,
        Clip clipBehavior = Clip.None,
        bool autofocus = false,
        MaterialTapTargetSize? materialTapTargetSize = null,
        Key? key = null,
        [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(heroTag))]
        string? heroTagExpression = null)
    {
        return new FloatingActionButton(
            child: child,
            tooltip: tooltip,
            extendedLabel: null,
            onPressed: onPressed,
            type: FloatingActionButtonType.Small,
            isExtended: false,
            heroTag: heroTagExpression is null ? DefaultHeroTag.Instance : heroTag,
            mouseCursor: mouseCursor,
            foregroundColor: foregroundColor,
            backgroundColor: backgroundColor,
            focusColor: focusColor,
            hoverColor: hoverColor,
            splashColor: splashColor,
            elevation: elevation,
            focusElevation: focusElevation,
            hoverElevation: hoverElevation,
            highlightElevation: highlightElevation,
            disabledElevation: disabledElevation,
            shape: shape,
            focusNode: focusNode,
            enableFeedback: enableFeedback,
            clipBehavior: clipBehavior,
            autofocus: autofocus,
            materialTapTargetSize: materialTapTargetSize,
            extendedIconLabelSpacing: null,
            extendedPadding: null,
            extendedTextStyle: null,
            key: key);
    }

    public static FloatingActionButton Large(
        Widget? child,
        Action? onPressed,
        string? tooltip = null,
        object? heroTag = null,
        MouseCursor? mouseCursor = null,
        Color? foregroundColor = null,
        Color? backgroundColor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        Color? splashColor = null,
        double? elevation = null,
        double? focusElevation = null,
        double? hoverElevation = null,
        double? highlightElevation = null,
        double? disabledElevation = null,
        ShapeBorder? shape = null,
        FocusNode? focusNode = null,
        bool? enableFeedback = null,
        Clip clipBehavior = Clip.None,
        bool autofocus = false,
        MaterialTapTargetSize? materialTapTargetSize = null,
        Key? key = null,
        [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(heroTag))]
        string? heroTagExpression = null)
    {
        return new FloatingActionButton(
            child: child,
            tooltip: tooltip,
            extendedLabel: null,
            onPressed: onPressed,
            type: FloatingActionButtonType.Large,
            isExtended: false,
            heroTag: heroTagExpression is null ? DefaultHeroTag.Instance : heroTag,
            mouseCursor: mouseCursor,
            foregroundColor: foregroundColor,
            backgroundColor: backgroundColor,
            focusColor: focusColor,
            hoverColor: hoverColor,
            splashColor: splashColor,
            elevation: elevation,
            focusElevation: focusElevation,
            hoverElevation: hoverElevation,
            highlightElevation: highlightElevation,
            disabledElevation: disabledElevation,
            shape: shape,
            focusNode: focusNode,
            enableFeedback: enableFeedback,
            clipBehavior: clipBehavior,
            autofocus: autofocus,
            materialTapTargetSize: materialTapTargetSize,
            extendedIconLabelSpacing: null,
            extendedPadding: null,
            extendedTextStyle: null,
            key: key);
    }

    public static FloatingActionButton Extended(
        Widget label,
        Action? onPressed,
        Widget? icon = null,
        bool isExtended = true,
        string? tooltip = null,
        object? heroTag = null,
        MouseCursor? mouseCursor = null,
        Color? foregroundColor = null,
        Color? backgroundColor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        Color? splashColor = null,
        double? elevation = null,
        double? focusElevation = null,
        double? hoverElevation = null,
        double? highlightElevation = null,
        double? disabledElevation = null,
        ShapeBorder? shape = null,
        FocusNode? focusNode = null,
        bool? enableFeedback = null,
        Clip clipBehavior = Clip.None,
        bool autofocus = false,
        MaterialTapTargetSize? materialTapTargetSize = null,
        double? extendedIconLabelSpacing = null,
        Thickness? extendedPadding = null,
        TextStyle? extendedTextStyle = null,
        Key? key = null,
        [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(heroTag))]
        string? heroTagExpression = null)
    {
        return new FloatingActionButton(
            child: icon,
            tooltip: tooltip,
            extendedLabel: label ?? throw new ArgumentNullException(nameof(label)),
            onPressed: onPressed,
            type: FloatingActionButtonType.Extended,
            isExtended: isExtended,
            heroTag: heroTagExpression is null ? DefaultHeroTag.Instance : heroTag,
            mouseCursor: mouseCursor,
            foregroundColor: foregroundColor,
            backgroundColor: backgroundColor,
            focusColor: focusColor,
            hoverColor: hoverColor,
            splashColor: splashColor,
            elevation: elevation,
            focusElevation: focusElevation,
            hoverElevation: hoverElevation,
            highlightElevation: highlightElevation,
            disabledElevation: disabledElevation,
            shape: shape,
            focusNode: focusNode,
            enableFeedback: enableFeedback,
            clipBehavior: clipBehavior,
            autofocus: autofocus,
            materialTapTargetSize: materialTapTargetSize,
            extendedIconLabelSpacing: extendedIconLabelSpacing,
            extendedPadding: extendedPadding,
            extendedTextStyle: extendedTextStyle,
            key: key);
    }

    public override Widget Build(BuildContext context)
    {
        ThemeData theme = Theme.Of(context);
        FloatingActionButtonThemeData floatingActionButtonTheme = FloatingActionButtonTheme.Of(context);
        FloatingActionButtonDefaults defaults = theme.UseMaterial3
            ? FloatingActionButtonDefaults.Material3(context, Type, Child is not null)
            : FloatingActionButtonDefaults.Material2(context, Type, Child is not null);

        Color foregroundColor = ForegroundColor
                                ?? floatingActionButtonTheme.ForegroundColor
                                ?? defaults.ForegroundColor;
        Color backgroundColor = BackgroundColor
                                ?? floatingActionButtonTheme.BackgroundColor
                                ?? defaults.BackgroundColor;
        Color focusColor = FocusColor
                           ?? floatingActionButtonTheme.FocusColor
                           ?? defaults.FocusColor;
        Color hoverColor = HoverColor
                           ?? floatingActionButtonTheme.HoverColor
                           ?? defaults.HoverColor;
        Color splashColor = SplashColor
                            ?? floatingActionButtonTheme.SplashColor
                            ?? defaults.SplashColor;
        double elevation = Elevation
                           ?? floatingActionButtonTheme.Elevation
                           ?? defaults.Elevation;
        double focusElevation = FocusElevation
                                ?? floatingActionButtonTheme.FocusElevation
                                ?? defaults.FocusElevation;
        double hoverElevation = HoverElevation
                                ?? floatingActionButtonTheme.HoverElevation
                                ?? defaults.HoverElevation;
        double highlightElevation = HighlightElevation
                                    ?? floatingActionButtonTheme.HighlightElevation
                                    ?? defaults.HighlightElevation;
        double disabledElevation = DisabledElevation
                                   ?? floatingActionButtonTheme.DisabledElevation
                                   ?? defaults.DisabledElevation
                                   ?? elevation;
        ShapeBorder shape = Shape
                            ?? floatingActionButtonTheme.Shape
                            ?? defaults.Shape;
        double iconSize = floatingActionButtonTheme.IconSize
                          ?? defaults.IconSize;
        TextStyle extendedTextStyle = (ExtendedTextStyle
                                       ?? floatingActionButtonTheme.ExtendedTextStyle
                                       ?? defaults.ExtendedTextStyle) with
        {
            Color = foregroundColor
        };
        MaterialTapTargetSize tapTargetSize = MaterialTapTargetSize ?? theme.MaterialTapTargetSize;
        bool enableFeedback = EnableFeedback
                              ?? floatingActionButtonTheme.EnableFeedback
                              ?? defaults.EnableFeedback;
        BoxConstraints sizeConstraints = ResolveSizeConstraints(floatingActionButtonTheme, defaults);

        var style = new ButtonStyle(
            ForegroundColor: MaterialStateProperty<Color?>.All(foregroundColor),
            BackgroundColor: MaterialStateProperty<Color?>.All(backgroundColor),
            ShadowColor: MaterialStateProperty<Color?>.All(theme.ShadowColor),
            OverlayColor: CreateOverlayResolver(focusColor, hoverColor, splashColor),
            SplashColor: MaterialButtonCore.CreateExplicitSplashResolver(splashColor),
            Elevation: CreateElevationResolver(
                elevation: elevation,
                focusElevation: focusElevation,
                hoverElevation: hoverElevation,
                highlightElevation: highlightElevation,
                disabledElevation: disabledElevation),
            IconSize: MaterialStateProperty<double?>.All(iconSize),
            Side: MaterialStateProperty<BorderSide?>.All(ShapeBorderGeometry.SideOrNull(shape)),
            Padding: MaterialStateProperty<Thickness?>.All(default),
            Shape: MaterialStateProperty<OutlinedBorder?>.All(new RoundedRectangleBorder(borderRadius:
                ShapeBorderGeometry.ResolveRadius(shape))),
            MinimumSize: MaterialStateProperty<Size?>.All(
                new Size(sizeConstraints.MinWidth, sizeConstraints.MinHeight)),
            MaximumSize: MaterialStateProperty<Size?>.All(
                new Size(sizeConstraints.MaxWidth, sizeConstraints.MaxHeight)),
            TapTargetSize: tapTargetSize,
            TextStyle: MaterialStateProperty<TextStyle?>.All(extendedTextStyle),
            MouseCursor: CreateMouseCursorResolver(MouseCursor, floatingActionButtonTheme.MouseCursor),
            Alignment: Plumix.Rendering.Alignment.Center);

        Widget result = new MaterialButtonCore(
            child: ResolveChild(context, floatingActionButtonTheme, defaults),
            onPressed: OnPressed,
            style: style,
            focusNode: FocusNode,
            clipBehavior: ClipBehavior,
            enableFeedback: enableFeedback,
            autofocus: Autofocus);

        if (Tooltip is not null)
        {
            result = new Tooltip(
                message: Tooltip,
                child: result);
        }

        if (HeroTag != null)
        {
            result = new Hero(
                tag: HeroTag,
                child: result);
        }

        return new MergeSemantics(child: result);
    }

    private Widget ResolveChild(
        BuildContext context,
        FloatingActionButtonThemeData floatingActionButtonTheme,
        FloatingActionButtonDefaults defaults)
    {
        if (Type != FloatingActionButtonType.Extended)
        {
            return Child ?? new SizedBox();
        }

        double spacing = ExtendedIconLabelSpacing
                         ?? floatingActionButtonTheme.ExtendedIconLabelSpacing
                         ?? defaults.ExtendedIconLabelSpacing;
        var children = new List<Widget>();
        if (Child is not null)
        {
            children.Add(Child);
            if (IsExtended)
            {
                children.Add(new SizedBox(width: spacing));
            }
        }

        if (IsExtended)
        {
            children.Add(ExtendedLabel!);
        }

        return new FloatingActionButtonChildOverflowBox(
            child: new Padding(
                insets: ResolveExtendedPadding(context, floatingActionButtonTheme, defaults),
                child: new Row(
                    mainAxisSize: MainAxisSize.Min,
                    spacing: 0,
                    children: children)));
    }

    private Thickness ResolveExtendedPadding(
        BuildContext context,
        FloatingActionButtonThemeData floatingActionButtonTheme,
        FloatingActionButtonDefaults defaults)
    {
        return ExtendedPadding
               ?? floatingActionButtonTheme.ExtendedPadding
               ?? defaults.ExtendedPadding;
    }

    private BoxConstraints ResolveSizeConstraints(
        FloatingActionButtonThemeData floatingActionButtonTheme,
        FloatingActionButtonDefaults defaults)
    {
        return Type switch
        {
            FloatingActionButtonType.Small =>
                floatingActionButtonTheme.SmallSizeConstraints ?? defaults.SmallSizeConstraints,
            FloatingActionButtonType.Large =>
                floatingActionButtonTheme.LargeSizeConstraints ?? defaults.LargeSizeConstraints,
            FloatingActionButtonType.Extended =>
                floatingActionButtonTheme.ExtendedSizeConstraints ?? defaults.ExtendedSizeConstraints,
            _ => floatingActionButtonTheme.SizeConstraints ?? defaults.SizeConstraints,
        };
    }

    private static MaterialStateProperty<Color?> CreateOverlayResolver(
        Color focusColor,
        Color hoverColor,
        Color splashColor)
    {
        return MaterialStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.HasFlag(MaterialState.Disabled))
            {
                return null;
            }

            if (states.HasFlag(MaterialState.Pressed))
            {
                return splashColor;
            }

            if (states.HasFlag(MaterialState.Hovered))
            {
                return hoverColor;
            }

            if (states.HasFlag(MaterialState.Focused))
            {
                return focusColor;
            }

            return null;
        });
    }

    private static MaterialStateProperty<MouseCursor?> CreateMouseCursorResolver(
        MouseCursor? widgetCursor,
        MaterialStateProperty<MouseCursor?>? themeCursor)
    {
        return MaterialStateProperty<MouseCursor?>.ResolveWith(states =>
        {
            MouseCursor? resolvedCursor = widgetCursor ?? themeCursor?.Resolve(states);
            if (resolvedCursor is not null)
            {
                return resolvedCursor;
            }

            return states.HasFlag(MaterialState.Disabled) || !OperatingSystem.IsBrowser()
                ? SystemMouseCursors.Basic
                : SystemMouseCursors.Click;
        });
    }

    private static MaterialStateProperty<double?> CreateElevationResolver(
        double elevation,
        double focusElevation,
        double hoverElevation,
        double highlightElevation,
        double disabledElevation)
    {
        return MaterialStateProperty<double?>.ResolveWith(states =>
        {
            if (states.HasFlag(MaterialState.Disabled))
            {
                return disabledElevation;
            }

            if (states.HasFlag(MaterialState.Pressed))
            {
                return highlightElevation;
            }

            if (states.HasFlag(MaterialState.Hovered))
            {
                return hoverElevation;
            }

            if (states.HasFlag(MaterialState.Focused))
            {
                return focusElevation;
            }

            return elevation;
        });
    }

    private static void ValidateElevation(string name, double? value)
    {
        if (!value.HasValue)
        {
            return;
        }

        if (double.IsNaN(value.Value) || double.IsInfinity(value.Value) || value.Value < 0)
        {
            throw new ArgumentOutOfRangeException(name, "Elevation must be finite and non-negative.");
        }
    }
}

internal sealed class FloatingActionButtonChildOverflowBox : SingleChildRenderObjectWidget
{
    public FloatingActionButtonChildOverflowBox(Widget? child = null) : base(child)
    {
    }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderFloatingActionButtonChildOverflowBox();
    }
}

internal sealed class RenderFloatingActionButtonChildOverflowBox : RenderProxyBox
{
    protected override double ComputeMinIntrinsicWidth(double height) => 0.0;

    protected override double ComputeMinIntrinsicHeight(double width) => 0.0;

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        if (Child is null)
        {
            return constraints.Biggest;
        }

        Size childSize = Child.GetDryLayout(new BoxConstraints(
            MaxWidth: double.PositiveInfinity,
            MaxHeight: double.PositiveInfinity));
        return constraints.Constrain(childSize);
    }

    protected override void PerformLayout()
    {
        if (Child is null)
        {
            Size = Constraints.Biggest;
            return;
        }

        Child.Layout(
            new BoxConstraints(
                MaxWidth: double.PositiveInfinity,
                MaxHeight: double.PositiveInfinity),
            parentUsesSize: true);
        Size childSize = Child.Size;
        Size = new Size(
            Math.Max(Constraints.MinWidth, Math.Min(Constraints.MaxWidth, childSize.Width)),
            Math.Max(Constraints.MinHeight, Math.Min(Constraints.MaxHeight, childSize.Height)));
        ((BoxParentData)Child.parentData!).offset = Plumix.Rendering.Alignment.Center.AlongOffset(Size, childSize);
    }
}

internal sealed record FloatingActionButtonDefaults(
    Color ForegroundColor,
    Color BackgroundColor,
    Color FocusColor,
    Color HoverColor,
    Color SplashColor,
    double Elevation,
    double FocusElevation,
    double HoverElevation,
    double HighlightElevation,
    double? DisabledElevation,
    ShapeBorder Shape,
    double IconSize,
    BoxConstraints SizeConstraints,
    BoxConstraints SmallSizeConstraints,
    BoxConstraints LargeSizeConstraints,
    BoxConstraints ExtendedSizeConstraints,
    double ExtendedIconLabelSpacing,
    Thickness ExtendedPadding,
    TextStyle ExtendedTextStyle,
    bool EnableFeedback)
{
    public static FloatingActionButtonDefaults Material2(
        BuildContext context,
        FloatingActionButtonType type,
        bool hasChild)
    {
        ThemeData theme = Theme.Of(context);
        return new FloatingActionButtonDefaults(
            ForegroundColor: theme.ColorScheme.OnSecondary,
            BackgroundColor: theme.ColorScheme.Secondary,
            FocusColor: theme.FocusColor,
            HoverColor: theme.HoverColor,
            SplashColor: theme.SplashColor,
            Elevation: 6,
            FocusElevation: 6,
            HoverElevation: 8,
            HighlightElevation: 12,
            DisabledElevation: null,
            Shape: ResolveM2Shape(type),
            IconSize: type == FloatingActionButtonType.Large ? 36 : 24,
            SizeConstraints: TightConstraints(width: 56, height: 56),
            SmallSizeConstraints: TightConstraints(width: 40, height: 40),
            LargeSizeConstraints: TightConstraints(width: 96, height: 96),
            ExtendedSizeConstraints: new BoxConstraints(
                MinWidth: 0,
                MaxWidth: double.PositiveInfinity,
                MinHeight: 48,
                MaxHeight: 48),
            ExtendedIconLabelSpacing: 8,
            ExtendedPadding: MaterialButtonCore.ResolveDirectionalPadding(
                context,
                start: hasChild && type == FloatingActionButtonType.Extended ? 16 : 20,
                top: 0,
                end: 20,
                bottom: 0),
            ExtendedTextStyle: theme.TextTheme.LabelLarge with { LetterSpacing = 1.2 },
            EnableFeedback: true);
    }

    public static FloatingActionButtonDefaults Material3(
        BuildContext context,
        FloatingActionButtonType type,
        bool hasChild)
    {
        ThemeData theme = Theme.Of(context);
        return new FloatingActionButtonDefaults(
            ForegroundColor: theme.ColorScheme.OnPrimaryContainer,
            BackgroundColor: theme.ColorScheme.PrimaryContainer,
            FocusColor: MaterialButtonCore.ApplyOpacity(theme.ColorScheme.OnPrimaryContainer, 0.10),
            HoverColor: MaterialButtonCore.ApplyOpacity(theme.ColorScheme.OnPrimaryContainer, 0.08),
            SplashColor: MaterialButtonCore.ApplyOpacity(theme.ColorScheme.OnPrimaryContainer, 0.10),
            Elevation: 6,
            FocusElevation: 6,
            HoverElevation: 8,
            HighlightElevation: 6,
            DisabledElevation: null,
            Shape: ResolveM3Shape(type),
            IconSize: type == FloatingActionButtonType.Large ? 36 : 24,
            SizeConstraints: TightConstraints(width: 56, height: 56),
            SmallSizeConstraints: TightConstraints(width: 40, height: 40),
            LargeSizeConstraints: TightConstraints(width: 96, height: 96),
            ExtendedSizeConstraints: new BoxConstraints(
                MinWidth: 0,
                MaxWidth: double.PositiveInfinity,
                MinHeight: 56,
                MaxHeight: 56),
            ExtendedIconLabelSpacing: 8,
            ExtendedPadding: MaterialButtonCore.ResolveDirectionalPadding(
                context,
                start: hasChild && type == FloatingActionButtonType.Extended ? 16 : 20,
                top: 0,
                end: 20,
                bottom: 0),
            ExtendedTextStyle: theme.TextTheme.LabelLarge,
            EnableFeedback: true);
    }

    private static ShapeBorder ResolveM2Shape(FloatingActionButtonType type)
    {
        return type == FloatingActionButtonType.Extended
            ? new StadiumBorder()
            : new CircleBorder();
    }

    private static ShapeBorder ResolveM3Shape(FloatingActionButtonType type)
    {
        return type switch
        {
            FloatingActionButtonType.Small => new RoundedRectangleBorder(borderRadius:
                Plumix.Rendering.BorderRadius.Circular(12)),
            FloatingActionButtonType.Large => new RoundedRectangleBorder(borderRadius:
                Plumix.Rendering.BorderRadius.Circular(28)),
            FloatingActionButtonType.Extended => new RoundedRectangleBorder(borderRadius:
                Plumix.Rendering.BorderRadius.Circular(16)),
            _ => new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(16)),
        };
    }

    private static BoxConstraints TightConstraints(double width, double height)
    {
        return new BoxConstraints(
            MinWidth: width,
            MaxWidth: width,
            MinHeight: height,
            MaxHeight: height);
    }
}
