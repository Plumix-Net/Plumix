using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/divider.dart

public sealed class Divider : StatelessWidget
{
    public Divider(
        double? height = null,
        double? thickness = null,
        double? indent = null,
        double? endIndent = null,
        Color? color = null,
        BorderRadiusGeometry? radius = null,
        Key? key = null) : base(key)
    {
        ValidateNonNegative(height, nameof(height));
        ValidateNonNegative(thickness, nameof(thickness));
        ValidateNonNegative(indent, nameof(indent));
        ValidateNonNegative(endIndent, nameof(endIndent));

        Height = height;
        Thickness = thickness;
        Indent = indent;
        EndIndent = endIndent;
        Color = color;
        Radius = radius;
    }

    public double? Height { get; }

    public double? Thickness { get; }

    public double? Indent { get; }

    public double? EndIndent { get; }

    public BorderRadiusGeometry? Radius { get; }

    public Color? Color { get; }

    public static BorderSide CreateBorderSide(BuildContext? context, Color? color = null, double? width = null)
    {
        DividerThemeData? dividerTheme = null;
        DividerThemeData? defaults = null;
        if (context is { } resolvedContext)
        {
            dividerTheme = DividerTheme.Of(resolvedContext);
            defaults = DividerDefaults.Resolve(Theme.Of(resolvedContext));
        }

        double effectiveWidth = width ?? dividerTheme?.Thickness ?? defaults?.Thickness ?? 0.0;
        ValidateNonNegative(effectiveWidth, nameof(width));

        Color effectiveColor = color ?? dividerTheme?.Color ?? defaults?.Color ?? Colors.Black;
        return new BorderSide(effectiveColor, effectiveWidth);
    }

    public override Widget Build(BuildContext context)
    {
        ThemeData theme = Theme.Of(context);
        DividerThemeData dividerTheme = DividerTheme.Of(context);
        DividerThemeData defaults = DividerDefaults.Resolve(theme);
        double effectiveHeight = Height ?? dividerTheme.Space ?? defaults.Space ?? 16.0;
        double effectiveThickness = Thickness ?? dividerTheme.Thickness ?? defaults.Thickness ?? 0.0;
        double effectiveIndent = Indent ?? dividerTheme.Indent ?? defaults.Indent ?? 0.0;
        double effectiveEndIndent = EndIndent ?? dividerTheme.EndIndent ?? defaults.EndIndent ?? 0.0;
        TextDirection direction = Directionality.Of(context);
        BorderRadius? effectiveRadius = (Radius ?? dividerTheme.Radius ?? defaults.Radius)?.Resolve(direction);
        var margin = direction == TextDirection.Ltr
            ? new Thickness(effectiveIndent, 0.0, effectiveEndIndent, 0.0)
            : new Thickness(effectiveEndIndent, 0.0, effectiveIndent, 0.0);

        return new SizedBox(
            height: effectiveHeight,
            child: new Center(
                child: new Container(
                    height: effectiveThickness,
                    margin: margin,
                    decoration: new BoxDecoration(
                        BorderRadius: effectiveRadius,
                        BorderSides: new BoxBorder(
                            Bottom: CreateBorderSide(
                                context,
                                color: Color,
                                width: effectiveThickness))))));
    }

    internal static void ValidateNonNegative(double? value, string paramName)
    {
        if (value.HasValue && (double.IsNaN(value.Value) || value.Value < 0.0))
        {
            throw new ArgumentOutOfRangeException(paramName, "Divider values must be non-negative.");
        }
    }
}

public sealed class VerticalDivider : StatelessWidget
{
    public VerticalDivider(
        double? width = null,
        double? thickness = null,
        double? indent = null,
        double? endIndent = null,
        Color? color = null,
        BorderRadiusGeometry? radius = null,
        Key? key = null) : base(key)
    {
        Divider.ValidateNonNegative(width, nameof(width));
        Divider.ValidateNonNegative(thickness, nameof(thickness));
        Divider.ValidateNonNegative(indent, nameof(indent));
        Divider.ValidateNonNegative(endIndent, nameof(endIndent));

        Width = width;
        Thickness = thickness;
        Indent = indent;
        EndIndent = endIndent;
        Color = color;
        Radius = radius;
    }

    public double? Width { get; }

    public double? Thickness { get; }

    public double? Indent { get; }

    public double? EndIndent { get; }

    public BorderRadiusGeometry? Radius { get; }

    public Color? Color { get; }

    public override Widget Build(BuildContext context)
    {
        ThemeData theme = Theme.Of(context);
        DividerThemeData dividerTheme = DividerTheme.Of(context);
        DividerThemeData defaults = DividerDefaults.Resolve(theme);
        double effectiveWidth = Width ?? dividerTheme.Space ?? defaults.Space ?? 16.0;
        double effectiveThickness = Thickness ?? dividerTheme.Thickness ?? defaults.Thickness ?? 0.0;
        double effectiveIndent = Indent ?? dividerTheme.Indent ?? defaults.Indent ?? 0.0;
        double effectiveEndIndent = EndIndent ?? dividerTheme.EndIndent ?? defaults.EndIndent ?? 0.0;
        TextDirection direction = Directionality.Of(context);
        BorderRadius? effectiveRadius = (Radius ?? dividerTheme.Radius ?? defaults.Radius)?.Resolve(direction);

        return new SizedBox(
            width: effectiveWidth,
            child: new Center(
                child: new Container(
                    width: effectiveThickness,
                    margin: new Thickness(0.0, effectiveIndent, 0.0, effectiveEndIndent),
                    decoration: new BoxDecoration(
                        BorderRadius: effectiveRadius,
                        BorderSides: new BoxBorder(
                            Left: Divider.CreateBorderSide(
                                context,
                                color: Color,
                                width: effectiveThickness))))));
    }
}

internal static class DividerDefaults
{
    public static DividerThemeData Resolve(ThemeData theme)
    {
        return theme.UseMaterial3
            ? new DividerThemeData(
                Color: theme.ColorScheme.OutlineVariant,
                Space: 16.0,
                Thickness: 1.0,
                Indent: 0.0,
                EndIndent: 0.0)
            : new DividerThemeData(
                Color: theme.DividerColor,
                Space: 16.0,
                Thickness: 0.0,
                Indent: 0.0,
                EndIndent: 0.0);
    }
}
