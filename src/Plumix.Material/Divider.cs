using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/divider.dart (approximate)

public sealed class Divider : StatelessWidget
{
    public Divider(
        double? height = null,
        double? thickness = null,
        double? indent = null,
        double? endIndent = null,
        Color? color = null,
        BorderRadius? radius = null,
        Key? key = null) : base(key)
    {
        ValidateNonNegativeFinite(height, nameof(height));
        ValidateNonNegativeFinite(thickness, nameof(thickness));
        ValidateNonNegativeFinite(indent, nameof(indent));
        ValidateNonNegativeFinite(endIndent, nameof(endIndent));

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

    public Color? Color { get; }

    public BorderRadius? Radius { get; }

    public static BorderSide CreateBorderSide(BuildContext? context, Color? color = null, double? width = null)
    {
        DividerThemeData? dividerTheme = null;
        DividerThemeData? defaults = null;
        if (context is { } resolvedContext)
        {
            dividerTheme = DividerTheme.Of(resolvedContext);
            defaults = DividerDefaults.Resolve(Theme.Of(resolvedContext));
        }

        var effectiveWidth = width
                             ?? dividerTheme?.Thickness
                             ?? defaults?.Thickness
                             ?? 0.0;
        ValidateNonNegativeFinite(effectiveWidth, nameof(width));

        var effectiveColor = color
                             ?? dividerTheme?.Color
                             ?? defaults?.Color
                             ?? Colors.Black;
        return new BorderSide(effectiveColor, effectiveWidth);
    }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var dividerTheme = DividerTheme.Of(context);
        var defaults = DividerDefaults.Resolve(theme);
        var effectiveHeight = Height ?? dividerTheme.Space ?? defaults.Space ?? 16.0;
        var effectiveThickness = Thickness ?? dividerTheme.Thickness ?? defaults.Thickness ?? 0.0;
        var effectiveIndent = Indent ?? dividerTheme.Indent ?? defaults.Indent ?? 0.0;
        var effectiveEndIndent = EndIndent ?? dividerTheme.EndIndent ?? defaults.EndIndent ?? 0.0;
        var effectiveColor = Color ?? dividerTheme.Color ?? defaults.Color ?? theme.DividerColor;
        var effectiveRadius = Radius ?? dividerTheme.Radius ?? defaults.Radius;

        return new SizedBox(
            height: effectiveHeight,
            child: new Center(
                child: new DividerLine(
                    axis: Axis.Horizontal,
                    thickness: effectiveThickness,
                    indent: effectiveIndent,
                    endIndent: effectiveEndIndent,
                    color: effectiveColor,
                    radius: effectiveRadius)));
    }

    internal static void ValidateNonNegativeFinite(double? value, string paramName)
    {
        if (value.HasValue && (double.IsNaN(value.Value) || double.IsInfinity(value.Value) || value.Value < 0))
        {
            throw new ArgumentOutOfRangeException(paramName, "Divider values must be non-negative and finite.");
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
        BorderRadius? radius = null,
        Key? key = null) : base(key)
    {
        Divider.ValidateNonNegativeFinite(width, nameof(width));
        Divider.ValidateNonNegativeFinite(thickness, nameof(thickness));
        Divider.ValidateNonNegativeFinite(indent, nameof(indent));
        Divider.ValidateNonNegativeFinite(endIndent, nameof(endIndent));

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

    public Color? Color { get; }

    public BorderRadius? Radius { get; }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var dividerTheme = DividerTheme.Of(context);
        var defaults = DividerDefaults.Resolve(theme);
        var effectiveWidth = Width ?? dividerTheme.Space ?? defaults.Space ?? 16.0;
        var effectiveThickness = Thickness ?? dividerTheme.Thickness ?? defaults.Thickness ?? 0.0;
        var effectiveIndent = Indent ?? dividerTheme.Indent ?? defaults.Indent ?? 0.0;
        var effectiveEndIndent = EndIndent ?? dividerTheme.EndIndent ?? defaults.EndIndent ?? 0.0;
        var effectiveColor = Color ?? dividerTheme.Color ?? defaults.Color ?? theme.DividerColor;
        var effectiveRadius = Radius ?? dividerTheme.Radius ?? defaults.Radius;

        return new SizedBox(
            width: effectiveWidth,
            child: new Center(
                child: new DividerLine(
                    axis: Axis.Vertical,
                    thickness: effectiveThickness,
                    indent: effectiveIndent,
                    endIndent: effectiveEndIndent,
                    color: effectiveColor,
                    radius: effectiveRadius)));
    }
}

internal static class DividerDefaults
{
    public static DividerThemeData Resolve(ThemeData theme)
    {
        return theme.UseMaterial3
            ? new DividerThemeData(
                Color: theme.OutlineVariantColor,
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

internal sealed class DividerLine : LeafRenderObjectWidget
{
    public DividerLine(
        Axis axis,
        double thickness,
        double indent,
        double endIndent,
        Color color,
        BorderRadius? radius,
        Key? key = null) : base(key)
    {
        Axis = axis;
        Thickness = thickness;
        Indent = indent;
        EndIndent = endIndent;
        Color = color;
        Radius = radius;
    }

    public Axis Axis { get; }

    public double Thickness { get; }

    public double Indent { get; }

    public double EndIndent { get; }

    public Color Color { get; }

    public BorderRadius? Radius { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderDividerLine(
            axis: Axis,
            thickness: Thickness,
            indent: Indent,
            endIndent: EndIndent,
            color: Color,
            radius: Radius);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var line = (RenderDividerLine)renderObject;
        line.Axis = Axis;
        line.Thickness = Thickness;
        line.Indent = Indent;
        line.EndIndent = EndIndent;
        line.Color = Color;
        line.Radius = Radius;
    }
}

internal sealed class RenderDividerLine : RenderBox
{
    private Axis _axis;
    private double _thickness;
    private double _indent;
    private double _endIndent;
    private Color _color;
    private BorderRadius? _radius;

    public RenderDividerLine(
        Axis axis,
        double thickness,
        double indent,
        double endIndent,
        Color color,
        BorderRadius? radius)
    {
        _axis = axis;
        _thickness = thickness;
        _indent = indent;
        _endIndent = endIndent;
        _color = color;
        _radius = radius;
    }

    public Axis Axis
    {
        get => _axis;
        set
        {
            if (_axis == value)
            {
                return;
            }

            _axis = value;
            MarkNeedsLayout();
        }
    }

    public double Thickness
    {
        get => _thickness;
        set
        {
            if (Math.Abs(_thickness - value) <= 0.0001)
            {
                return;
            }

            _thickness = value;
            MarkNeedsLayout();
            MarkNeedsPaint();
        }
    }

    public double Indent
    {
        get => _indent;
        set
        {
            if (Math.Abs(_indent - value) <= 0.0001)
            {
                return;
            }

            _indent = value;
            MarkNeedsPaint();
        }
    }

    public double EndIndent
    {
        get => _endIndent;
        set
        {
            if (Math.Abs(_endIndent - value) <= 0.0001)
            {
                return;
            }

            _endIndent = value;
            MarkNeedsPaint();
        }
    }

    public Color Color
    {
        get => _color;
        set
        {
            if (_color == value)
            {
                return;
            }

            _color = value;
            MarkNeedsPaint();
        }
    }

    public BorderRadius? Radius
    {
        get => _radius;
        set
        {
            if (_radius == value)
            {
                return;
            }

            _radius = value;
            MarkNeedsPaint();
        }
    }

    protected override void PerformLayout()
    {
        var logicalThickness = Math.Max(0, Thickness);
        if (Axis == Axis.Horizontal)
        {
            var width = Constraints.HasBoundedWidth ? Constraints.MaxWidth : Constraints.ConstrainWidth(0);
            Size = Constraints.Constrain(new Size(width, logicalThickness));
        }
        else
        {
            var height = Constraints.HasBoundedHeight ? Constraints.MaxHeight : Constraints.ConstrainHeight(0);
            Size = Constraints.Constrain(new Size(logicalThickness, height));
        }
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        var brush = new SolidColorBrush(Color);
        var drawThickness = Thickness <= 0 ? 1.0 : Thickness;
        var radius = Radius?.Radius ?? 0.0;

        if (Axis == Axis.Horizontal)
        {
            var availableWidth = Math.Max(0, Size.Width - Indent - EndIndent);
            if (availableWidth <= 0)
            {
                return;
            }

            var y = offset.Y + ((Size.Height - drawThickness) / 2.0);
            var x = offset.X + Indent;
            ctx.DrawRectangle(brush, null, new Rect(x, y, availableWidth, drawThickness), radius, radius);
            return;
        }

        var availableHeight = Math.Max(0, Size.Height - Indent - EndIndent);
        if (availableHeight <= 0)
        {
            return;
        }

        var xVertical = offset.X + ((Size.Width - drawThickness) / 2.0);
        var yVertical = offset.Y + Indent;
        ctx.DrawRectangle(brush, null, new Rect(xVertical, yVertical, drawThickness, availableHeight), radius, radius);
    }
}
