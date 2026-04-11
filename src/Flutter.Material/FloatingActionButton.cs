using Avalonia;
using Avalonia.Media;
using Flutter.Foundation;
using Flutter.Rendering;
using Flutter.Widgets;

namespace Flutter.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/floating_action_button.dart (approximate)

internal enum FloatingActionButtonType
{
    Regular,
    Small,
    Large,
    Extended,
}

public sealed class FloatingActionButton : StatelessWidget
{
    public FloatingActionButton(
        Widget? child,
        Action? onPressed,
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
        BorderRadius? shape = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        MaterialTapTargetSize? materialTapTargetSize = null,
        Key? key = null) : this(
            child: child,
            extendedLabel: null,
            onPressed: onPressed,
            type: mini ? FloatingActionButtonType.Small : FloatingActionButtonType.Regular,
            isExtended: false,
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
        Widget? extendedLabel,
        Action? onPressed,
        FloatingActionButtonType type,
        bool isExtended,
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
        BorderRadius? shape,
        FocusNode? focusNode,
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
        ExtendedLabel = extendedLabel;
        OnPressed = onPressed;
        Type = type;
        IsExtended = isExtended;
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
        Autofocus = autofocus;
        MaterialTapTargetSize = materialTapTargetSize;
        ExtendedIconLabelSpacing = extendedIconLabelSpacing;
        ExtendedPadding = extendedPadding;
        ExtendedTextStyle = extendedTextStyle;
    }

    public Widget? Child { get; }

    private Widget? ExtendedLabel { get; }

    public Action? OnPressed { get; }

    private FloatingActionButtonType Type { get; }

    public bool IsExtended { get; }

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

    public BorderRadius? Shape { get; }

    public FocusNode? FocusNode { get; }

    public bool Autofocus { get; }

    public MaterialTapTargetSize? MaterialTapTargetSize { get; }

    public double? ExtendedIconLabelSpacing { get; }

    public Thickness? ExtendedPadding { get; }

    public TextStyle? ExtendedTextStyle { get; }

    public static FloatingActionButton Small(
        Widget? child,
        Action? onPressed,
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
        BorderRadius? shape = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        MaterialTapTargetSize? materialTapTargetSize = null,
        Key? key = null)
    {
        return new FloatingActionButton(
            child: child,
            extendedLabel: null,
            onPressed: onPressed,
            type: FloatingActionButtonType.Small,
            isExtended: false,
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
        BorderRadius? shape = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        MaterialTapTargetSize? materialTapTargetSize = null,
        Key? key = null)
    {
        return new FloatingActionButton(
            child: child,
            extendedLabel: null,
            onPressed: onPressed,
            type: FloatingActionButtonType.Large,
            isExtended: false,
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
        BorderRadius? shape = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        MaterialTapTargetSize? materialTapTargetSize = null,
        double? extendedIconLabelSpacing = null,
        Thickness? extendedPadding = null,
        TextStyle? extendedTextStyle = null,
        Key? key = null)
    {
        return new FloatingActionButton(
            child: icon,
            extendedLabel: label ?? throw new ArgumentNullException(nameof(label)),
            onPressed: onPressed,
            type: FloatingActionButtonType.Extended,
            isExtended: isExtended,
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
            autofocus: autofocus,
            materialTapTargetSize: materialTapTargetSize,
            extendedIconLabelSpacing: extendedIconLabelSpacing,
            extendedPadding: extendedPadding,
            extendedTextStyle: extendedTextStyle,
            key: key);
    }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var floatingActionButtonTheme = FloatingActionButtonTheme.Of(context);
        var defaults = theme.UseMaterial3
            ? FloatingActionButtonDefaults.Material3(context, Type, Child is not null)
            : FloatingActionButtonDefaults.Material2(context, Type, Child is not null);

        var foregroundColor = ForegroundColor
                              ?? floatingActionButtonTheme.ForegroundColor
                              ?? defaults.ForegroundColor;
        var backgroundColor = BackgroundColor
                              ?? floatingActionButtonTheme.BackgroundColor
                              ?? defaults.BackgroundColor;
        var focusColor = FocusColor
                         ?? floatingActionButtonTheme.FocusColor
                         ?? defaults.FocusColor;
        var hoverColor = HoverColor
                         ?? floatingActionButtonTheme.HoverColor
                         ?? defaults.HoverColor;
        var splashColor = SplashColor
                          ?? floatingActionButtonTheme.SplashColor
                          ?? defaults.SplashColor;
        var elevation = Elevation
                        ?? floatingActionButtonTheme.Elevation
                        ?? defaults.Elevation;
        var focusElevation = FocusElevation
                             ?? floatingActionButtonTheme.FocusElevation
                             ?? defaults.FocusElevation;
        var hoverElevation = HoverElevation
                             ?? floatingActionButtonTheme.HoverElevation
                             ?? defaults.HoverElevation;
        var highlightElevation = HighlightElevation
                                 ?? floatingActionButtonTheme.HighlightElevation
                                 ?? defaults.HighlightElevation;
        var disabledElevation = DisabledElevation
                                ?? floatingActionButtonTheme.DisabledElevation
                                ?? defaults.DisabledElevation
                                ?? elevation;
        var shape = Shape
                    ?? floatingActionButtonTheme.Shape
                    ?? defaults.Shape;
        var iconSize = floatingActionButtonTheme.IconSize
                       ?? defaults.IconSize;
        var extendedTextStyle = (ExtendedTextStyle
                                 ?? floatingActionButtonTheme.ExtendedTextStyle
                                 ?? defaults.ExtendedTextStyle) with
        {
            Color = foregroundColor
        };
        var tapTargetSize = MaterialTapTargetSize
                            ?? floatingActionButtonTheme.MaterialTapTargetSize
                            ?? theme.MaterialTapTargetSize;
        var sizeConstraints = ResolveSizeConstraints(floatingActionButtonTheme, defaults);

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
            Side: MaterialStateProperty<BorderSide?>.All(null),
            Padding: MaterialStateProperty<Thickness?>.All(default),
            Shape: MaterialStateProperty<BorderRadius?>.All(shape),
            MinimumSize: MaterialStateProperty<Size?>.All(new Size(sizeConstraints.MinWidth, sizeConstraints.MinHeight)),
            MaximumSize: MaterialStateProperty<Size?>.All(new Size(sizeConstraints.MaxWidth, sizeConstraints.MaxHeight)),
            TapTargetSize: tapTargetSize,
            TextStyle: MaterialStateProperty<TextStyle?>.All(extendedTextStyle),
            Alignment: Flutter.Rendering.Alignment.Center);

        return new MaterialButtonCore(
            child: ResolveChild(context, floatingActionButtonTheme, defaults),
            onPressed: OnPressed,
            style: style,
            focusNode: FocusNode,
            autofocus: Autofocus);
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

        if (!IsExtended)
        {
            return Child ?? new SizedBox();
        }

        var label = ExtendedLabel ?? new SizedBox();
        var spacing = ExtendedIconLabelSpacing
                      ?? floatingActionButtonTheme.ExtendedIconLabelSpacing
                      ?? defaults.ExtendedIconLabelSpacing;
        var children = new List<Widget>();
        if (Child is not null)
        {
            children.Add(Child);
            children.Add(new SizedBox(width: spacing));
        }

        children.Add(label);

        return new Padding(
            insets: ResolveExtendedPadding(context, floatingActionButtonTheme, defaults),
            child: new Row(
                mainAxisSize: MainAxisSize.Min,
                spacing: 0,
                children: children));
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
            FloatingActionButtonType.Small => floatingActionButtonTheme.SmallSizeConstraints ?? defaults.SmallSizeConstraints,
            FloatingActionButtonType.Large => floatingActionButtonTheme.LargeSizeConstraints ?? defaults.LargeSizeConstraints,
            FloatingActionButtonType.Extended => floatingActionButtonTheme.ExtendedSizeConstraints ?? defaults.ExtendedSizeConstraints,
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
    BorderRadius Shape,
    double IconSize,
    BoxConstraints SizeConstraints,
    BoxConstraints SmallSizeConstraints,
    BoxConstraints LargeSizeConstraints,
    BoxConstraints ExtendedSizeConstraints,
    double ExtendedIconLabelSpacing,
    Thickness ExtendedPadding,
    TextStyle ExtendedTextStyle)
{
    public static FloatingActionButtonDefaults Material2(
        BuildContext context,
        FloatingActionButtonType type,
        bool hasChild)
    {
        var theme = Theme.Of(context);
        return new FloatingActionButtonDefaults(
            ForegroundColor: theme.OnSecondaryContainerColor,
            BackgroundColor: theme.SecondaryContainerColor,
            FocusColor: MaterialButtonCore.ApplyOpacity(theme.OnSecondaryContainerColor, 0.12),
            HoverColor: MaterialButtonCore.ApplyOpacity(theme.OnSecondaryContainerColor, 0.08),
            SplashColor: MaterialButtonCore.ApplyOpacity(theme.OnSecondaryContainerColor, 0.12),
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
            ExtendedTextStyle: theme.TextTheme.LabelLarge with { LetterSpacing = 1.2 });
    }

    public static FloatingActionButtonDefaults Material3(
        BuildContext context,
        FloatingActionButtonType type,
        bool hasChild)
    {
        var theme = Theme.Of(context);
        return new FloatingActionButtonDefaults(
            ForegroundColor: theme.OnPrimaryContainerColor,
            BackgroundColor: theme.PrimaryContainerColor,
            FocusColor: MaterialButtonCore.ApplyOpacity(theme.OnPrimaryContainerColor, 0.10),
            HoverColor: MaterialButtonCore.ApplyOpacity(theme.OnPrimaryContainerColor, 0.08),
            SplashColor: MaterialButtonCore.ApplyOpacity(theme.OnPrimaryContainerColor, 0.10),
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
            ExtendedTextStyle: theme.TextTheme.LabelLarge);
    }

    private static BorderRadius ResolveM2Shape(FloatingActionButtonType type)
    {
        return type switch
        {
            FloatingActionButtonType.Small => BorderRadius.Circular(20),
            FloatingActionButtonType.Large => BorderRadius.Circular(48),
            FloatingActionButtonType.Extended => BorderRadius.Circular(999),
            _ => BorderRadius.Circular(28),
        };
    }

    private static BorderRadius ResolveM3Shape(FloatingActionButtonType type)
    {
        return type switch
        {
            FloatingActionButtonType.Small => BorderRadius.Circular(12),
            FloatingActionButtonType.Large => BorderRadius.Circular(28),
            FloatingActionButtonType.Extended => BorderRadius.Circular(16),
            _ => BorderRadius.Circular(16),
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
