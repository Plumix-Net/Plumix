using System.Globalization;
using System.Text;
using Avalonia.Media;
using Plumix.Painting;
using Plumix.Rendering;

namespace Plumix.UI;

// Dart parity source: dart:ui Paint, PaintingStyle, StrokeCap, StrokeJoin and MaskFilter
// (engine/src/flutter/lib/ui/painting.dart).

/// Styles to use for line endings.
public enum StrokeCap
{
    /// Begin and end contours with a flat edge and no extension.
    Butt,

    /// Begin and end contours with a semi-circle extension.
    Round,

    /// Begin and end contours with a half square extension.
    Square,
}

/// Styles to use for line segment joins.
public enum StrokeJoin
{
    /// Joins between line segments form sharp corners.
    Miter,

    /// Joins between line segments are semi-circular.
    Round,

    /// Joins between line segments connect the corners of the butt ends of the segments.
    Bevel,
}

/// Strategies for painting shapes and paths on a canvas.
public enum PaintingStyle
{
    /// Apply the paint to the inside of the shape.
    Fill,

    /// Apply the paint to the edge of the shape.
    Stroke,
}

/// A mask filter to apply to shapes as they are painted.
public sealed class MaskFilter : IEquatable<MaskFilter>
{
    private MaskFilter(BlurStyle style, double sigma)
    {
        Style = style;
        Sigma = sigma;
    }

    /// Creates a mask filter that takes the shape being drawn and blurs it.
    public static MaskFilter Blur(BlurStyle style, double sigma) => new(style, sigma);

    /// Dart's private `_style`.
    public BlurStyle Style { get; }

    /// Dart's private `_sigma`.
    public double Sigma { get; }

    public bool Equals(MaskFilter? other) => other is not null && other.Style == Style && other.Sigma == Sigma;

    public override bool Equals(object? obj) => Equals(obj as MaskFilter);

    public override int GetHashCode() => HashCode.Combine(Style, Sigma);

    public override string ToString() =>
        $"MaskFilter.blur({DartFormat.Enum(Style)}, {Sigma.ToString("F1", CultureInfo.InvariantCulture)})";
}

/// A description of the style to use when drawing on a canvas.
///
/// Dart packs the fields into a byte buffer shared with the engine; Plumix keeps them as plain
/// properties with the same defaults. Dart's `shader` is a `dart:ui` `Shader`; Plumix has no engine
/// shader and uses an Avalonia <see cref="IBrush"/> for it, the same stand-in
/// <see cref="ShaderCallback"/> returns. Like Dart, `Paint` has identity equality.
public sealed class Paint
{
    // Dart's `_kColorDefault`.
    private static readonly Color ColorDefault = Color.FromUInt32(0xFF000000);

    // Dart's `_kStrokeMiterLimitDefault`.
    private const double StrokeMiterLimitDefault = 4.0;

    /// Constructs an empty paint object.
    public Paint()
    {
    }

    /// Constructs a new paint object with the same fields as `other`.
    public Paint(Paint other)
    {
        ArgumentNullException.ThrowIfNull(other);
        IsAntiAlias = other.IsAntiAlias;
        Color = other.Color;
        BlendMode = other.BlendMode;
        Style = other.Style;
        StrokeWidth = other.StrokeWidth;
        StrokeCap = other.StrokeCap;
        StrokeJoin = other.StrokeJoin;
        StrokeMiterLimit = other.StrokeMiterLimit;
        MaskFilter = other.MaskFilter;
        FilterQuality = other.FilterQuality;
        Shader = other.Shader;
        ColorFilter = other.ColorFilter;
        ImageFilter = other.ImageFilter;
        InvertColors = other.InvertColors;
    }

    /// Dart's `Paint.from`.
    public static Paint From(Paint other) => new(other);

    /// Whether to apply anti-aliasing to lines and images drawn on the canvas. Defaults to true.
    public bool IsAntiAlias { get; set; } = true;

    /// The color to use when stroking or filling a shape. Defaults to opaque black.
    public Color Color { get; set; } = ColorDefault;

    /// A blend mode to apply when a shape is drawn or a layer is composited. Defaults to
    /// <see cref="Rendering.BlendMode.SourceOver"/>.
    public BlendMode BlendMode { get; set; } = BlendMode.SourceOver;

    /// Whether to paint inside shapes, the edges of shapes, or both. Defaults to
    /// <see cref="PaintingStyle.Fill"/>.
    public PaintingStyle Style { get; set; } = PaintingStyle.Fill;

    /// How wide to make edges drawn when <see cref="Style"/> is <see cref="PaintingStyle.Stroke"/>.
    /// Defaults to 0.0, which is a hairline.
    public double StrokeWidth { get; set; }

    /// The kind of finish to place on the end of lines. Defaults to <see cref="StrokeCap.Butt"/>.
    public StrokeCap StrokeCap { get; set; } = StrokeCap.Butt;

    /// The kind of finish to place on the joins between segments. Defaults to
    /// <see cref="StrokeJoin.Miter"/>.
    public StrokeJoin StrokeJoin { get; set; } = StrokeJoin.Miter;

    /// The limit for miters to be drawn on segments when the join is set to
    /// <see cref="StrokeJoin.Miter"/>. Defaults to 4.0.
    public double StrokeMiterLimit { get; set; } = StrokeMiterLimitDefault;

    /// A mask filter (for example, a blur) to apply to a shape after it has been drawn.
    public MaskFilter? MaskFilter { get; set; }

    /// Controls the performance vs quality trade-off to use when sampling bitmaps. Defaults to
    /// <see cref="Rendering.FilterQuality.None"/>.
    public FilterQuality FilterQuality { get; set; } = FilterQuality.None;

    /// The shader to use when stroking or filling a shape. When null, <see cref="Color"/> is used.
    public IBrush? Shader { get; set; }

    /// A color filter to apply when a shape is drawn or when a layer is composited.
    public ColorFilter? ColorFilter { get; set; }

    /// The image filter to use when drawing raster images.
    public ImageFilter? ImageFilter { get; set; }

    /// Whether the colors of the image are inverted when drawn.
    public bool InvertColors { get; set; }

    public override string ToString()
    {
        var result = new StringBuilder();
        string semicolon = string.Empty;
        result.Append("Paint(");
        if (Style == PaintingStyle.Stroke)
        {
            result.Append(DartFormat.Enum(Style));
            if (StrokeWidth != 0.0)
            {
                result.Append(' ').Append(StrokeWidth.ToString("F1", CultureInfo.InvariantCulture));
            }
            else
            {
                result.Append(" hairline");
            }

            if (StrokeCap != StrokeCap.Butt)
            {
                result.Append(' ').Append(DartFormat.Enum(StrokeCap));
            }

            if (StrokeJoin == StrokeJoin.Miter)
            {
                if (StrokeMiterLimit != StrokeMiterLimitDefault)
                {
                    result.Append(' ')
                        .Append(DartFormat.Enum(StrokeJoin))
                        .Append(" up to ")
                        .Append(StrokeMiterLimit.ToString("F1", CultureInfo.InvariantCulture));
                }
            }
            else
            {
                result.Append(' ').Append(DartFormat.Enum(StrokeJoin));
            }

            semicolon = "; ";
        }

        if (!IsAntiAlias)
        {
            result.Append(semicolon).Append("antialias off");
            semicolon = "; ";
        }

        if (Color != ColorDefault)
        {
            result.Append(semicolon).Append(Color.ToDartString());
            semicolon = "; ";
        }

        if (BlendMode != BlendMode.SourceOver)
        {
            result.Append(semicolon).Append(BlendModeName(BlendMode));
            semicolon = "; ";
        }

        if (ColorFilter is not null)
        {
            result.Append(semicolon).Append("colorFilter: ").Append(ColorFilter);
            semicolon = "; ";
        }

        if (MaskFilter is not null)
        {
            result.Append(semicolon).Append("maskFilter: ").Append(MaskFilter);
            semicolon = "; ";
        }

        if (FilterQuality != FilterQuality.None)
        {
            result.Append(semicolon).Append("filterQuality: ").Append(DartFormat.Enum(FilterQuality));
            semicolon = "; ";
        }

        if (Shader is not null)
        {
            result.Append(semicolon).Append("shader: ").Append(Shader);
            semicolon = "; ";
        }

        if (ImageFilter is not null)
        {
            result.Append(semicolon).Append("imageFilter: ").Append(ImageFilter);
            semicolon = "; ";
        }

        if (InvertColors)
        {
            result.Append(semicolon).Append("invert: true");
        }

        result.Append(')');
        return result.ToString();
    }

    /// Dart's `BlendMode.toString`, whose value names are shorter than the C# ones.
    internal static string BlendModeName(BlendMode mode)
    {
        string name = mode switch
        {
            BlendMode.Clear => "clear",
            BlendMode.Source => "src",
            BlendMode.Destination => "dst",
            BlendMode.SourceOver => "srcOver",
            BlendMode.DestinationOver => "dstOver",
            BlendMode.SourceIn => "srcIn",
            BlendMode.DestinationIn => "dstIn",
            BlendMode.SourceOut => "srcOut",
            BlendMode.DestinationOut => "dstOut",
            BlendMode.SourceAtop => "srcATop",
            BlendMode.DestinationAtop => "dstATop",
            _ => mode.ToString(),
        };
        return $"BlendMode.{char.ToLowerInvariant(name[0])}{name[1..]}";
    }
}
