using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/snack_bar_theme.dart

/// Defines where a [SnackBar] should appear within a `Scaffold` and how its location should be
/// adjusted when the scaffold also includes a `FloatingActionButton` or a `BottomNavigationBar`.
public enum SnackBarBehavior
{
    /// Fixes the [SnackBar] at the bottom of the `Scaffold`.
    Fixed,

    /// The [SnackBar] floats above the `Scaffold`'s contents.
    Floating,
}

/// Customizes default property values for [SnackBar] widgets.
///
/// Flutter declares this as an ordinary class so `_SnackbarDefaultsM2`/`_SnackbarDefaultsM3` can
/// extend it and override individual getters; Plumix keeps that shape, which is why the members are
/// `virtual` rather than record properties.
public partial class SnackBarThemeData
{
    public SnackBarThemeData(
        Color? backgroundColor = null,
        WidgetStateColor? actionTextColor = null,
        Color? disabledActionTextColor = null,
        TextStyle? contentTextStyle = null,
        double? elevation = null,
        ShapeBorder? shape = null,
        SnackBarBehavior? behavior = null,
        double? width = null,
        Thickness? insetPadding = null,
        bool? showCloseIcon = null,
        Color? closeIconColor = null,
        double? actionOverflowThreshold = null,
        WidgetStateColor? actionBackgroundColor = null,
        Color? disabledActionBackgroundColor = null,
        DismissDirection? dismissDirection = null)
    {
        if (elevation is < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(elevation));
        }

        if (width is not null && behavior != SnackBarBehavior.Floating)
        {
            throw new ArgumentException(
                "Width can only be set if behaviour is SnackBarBehavior.floating",
                nameof(width));
        }

        if (actionOverflowThreshold is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actionOverflowThreshold),
                "Action overflow threshold must be between 0 and 1 inclusive");
        }

        if (actionBackgroundColor is { IsConstantColor: false } && disabledActionBackgroundColor is not null)
        {
            throw new ArgumentException(
                "disabledBackgroundColor must not be provided when background color is a WidgetStateColor",
                nameof(disabledActionBackgroundColor));
        }

        BackgroundColor = backgroundColor;
        ActionTextColor = actionTextColor;
        DisabledActionTextColor = disabledActionTextColor;
        ContentTextStyle = contentTextStyle;
        Elevation = elevation;
        Shape = shape;
        Behavior = behavior;
        Width = width;
        InsetPadding = insetPadding;
        ShowCloseIcon = showCloseIcon;
        CloseIconColor = closeIconColor;
        ActionOverflowThreshold = actionOverflowThreshold;
        ActionBackgroundColor = actionBackgroundColor;
        DisabledActionBackgroundColor = disabledActionBackgroundColor;
        DismissDirection = dismissDirection;
    }

    public virtual Color? BackgroundColor { get; }

    public virtual WidgetStateColor? ActionTextColor { get; }

    public virtual Color? DisabledActionTextColor { get; }

    public virtual TextStyle? ContentTextStyle { get; }

    public virtual double? Elevation { get; }

    public virtual ShapeBorder? Shape { get; }

    public virtual SnackBarBehavior? Behavior { get; }

    public virtual double? Width { get; }

    public virtual Thickness? InsetPadding { get; }

    public virtual bool? ShowCloseIcon { get; }

    public virtual Color? CloseIconColor { get; }

    public virtual double? ActionOverflowThreshold { get; }

    public virtual WidgetStateColor? ActionBackgroundColor { get; }

    public virtual Color? DisabledActionBackgroundColor { get; }

    public virtual DismissDirection? DismissDirection { get; }

    public SnackBarThemeData CopyWith(
        Color? backgroundColor = null,
        WidgetStateColor? actionTextColor = null,
        Color? disabledActionTextColor = null,
        TextStyle? contentTextStyle = null,
        double? elevation = null,
        ShapeBorder? shape = null,
        SnackBarBehavior? behavior = null,
        double? width = null,
        Thickness? insetPadding = null,
        bool? showCloseIcon = null,
        Color? closeIconColor = null,
        double? actionOverflowThreshold = null,
        WidgetStateColor? actionBackgroundColor = null,
        Color? disabledActionBackgroundColor = null,
        DismissDirection? dismissDirection = null)
    {
        return new SnackBarThemeData(
            backgroundColor: backgroundColor ?? BackgroundColor,
            actionTextColor: actionTextColor ?? ActionTextColor,
            disabledActionTextColor: disabledActionTextColor ?? DisabledActionTextColor,
            contentTextStyle: contentTextStyle ?? ContentTextStyle,
            elevation: elevation ?? Elevation,
            shape: shape ?? Shape,
            behavior: behavior ?? Behavior,
            width: width ?? Width,
            insetPadding: insetPadding ?? InsetPadding,
            showCloseIcon: showCloseIcon ?? ShowCloseIcon,
            closeIconColor: closeIconColor ?? CloseIconColor,
            actionOverflowThreshold: actionOverflowThreshold ?? ActionOverflowThreshold,
            actionBackgroundColor: actionBackgroundColor ?? ActionBackgroundColor,
            disabledActionBackgroundColor: disabledActionBackgroundColor ?? DisabledActionBackgroundColor,
            dismissDirection: dismissDirection ?? DismissDirection);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return obj is SnackBarThemeData other
               && other.BackgroundColor == BackgroundColor
               && Equals(other.ActionTextColor, ActionTextColor)
               && other.DisabledActionTextColor == DisabledActionTextColor
               && Equals(other.ContentTextStyle, ContentTextStyle)
               && other.Elevation == Elevation
               && Equals(other.Shape, Shape)
               && other.Behavior == Behavior
               && other.Width == Width
               && other.InsetPadding == InsetPadding
               && other.ShowCloseIcon == ShowCloseIcon
               && other.CloseIconColor == CloseIconColor
               && other.ActionOverflowThreshold == ActionOverflowThreshold
               && Equals(other.ActionBackgroundColor, ActionBackgroundColor)
               && other.DisabledActionBackgroundColor == DisabledActionBackgroundColor
               && other.DismissDirection == DismissDirection;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(BackgroundColor);
        hash.Add(ActionTextColor);
        hash.Add(DisabledActionTextColor);
        hash.Add(ContentTextStyle);
        hash.Add(Elevation);
        hash.Add(Shape);
        hash.Add(Behavior);
        hash.Add(Width);
        hash.Add(InsetPadding);
        hash.Add(ShowCloseIcon);
        hash.Add(CloseIconColor);
        hash.Add(ActionOverflowThreshold);
        hash.Add(ActionBackgroundColor);
        hash.Add(DisabledActionBackgroundColor);
        hash.Add(DismissDirection);
        return hash.ToHashCode();
    }
}

/// Applies a snack bar theme to descendant [SnackBar] widgets.
public sealed class SnackBarTheme : InheritedTheme
{
    public SnackBarTheme(SnackBarThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public SnackBarThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    public override Widget Wrap(BuildContext context, Widget child) => new SnackBarTheme(Data, child);

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(((SnackBarTheme)oldWidget).Data, Data);

    public static SnackBarThemeData Of(BuildContext context) =>
        context.DependOnInherited<SnackBarTheme>()?.Data ?? Theme.Of(context).SnackBarTheme;
}
