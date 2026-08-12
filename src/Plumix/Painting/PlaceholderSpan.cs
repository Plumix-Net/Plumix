using System.Text;
using Avalonia;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/painting/placeholder_span.dart

namespace Plumix.Painting;

/// Where to vertically align the placeholder relative to the surrounding text.
public enum PlaceholderAlignment
{
    /// Match the baseline of the placeholder with the baseline.
    ///
    /// The [TextBaseline] to use must be specified and non-null when using this
    /// alignment mode.
    Baseline,

    /// Align the bottom edge of the placeholder with the baseline such that the
    /// placeholder sits on top of the baseline.
    AboveBaseline,

    /// Align the top edge of the placeholder with the baseline specified such
    /// that the placeholder hangs below the baseline.
    BelowBaseline,

    /// Align the top edge of the placeholder with the top edge of the text.
    Top,

    /// Align the bottom edge of the placeholder with the bottom edge of the text.
    Bottom,

    /// Align the middle of the placeholder with the middle of the text.
    Middle
}

/// Holds the [Size] and baseline required to represent the dimensions of
/// a placeholder in text.
///
/// Placeholders specify an empty space in the text layout, which is used
/// to later render arbitrary inline widgets into defined by a [WidgetSpan].
public sealed record PlaceholderDimensions
{
    /// Constructs a [PlaceholderDimensions] with the specified parameters.
    ///
    /// `size` and `alignment` are required as a placeholder's dimensions require
    /// at least `size` and `alignment` to be fully defined.
    public PlaceholderDimensions(
        Size size,
        PlaceholderAlignment alignment,
        TextBaseline? baseline = null,
        double? baselineOffset = null)
    {
        Size = size;
        Alignment = alignment;
        Baseline = baseline;
        BaselineOffset = baselineOffset;
    }

    /// A constant representing an empty placeholder.
    public static PlaceholderDimensions Empty { get; } =
        new(default, PlaceholderAlignment.Bottom);

    /// Width and height dimensions of the placeholder.
    public Size Size { get; }

    /// How to align the placeholder with the text.
    ///
    /// Used to determine the baseline offset.
    public PlaceholderAlignment Alignment { get; }

    /// Distance of the [Baseline] from the top of this placeholder.
    ///
    /// This is only used when [Alignment] is [PlaceholderAlignment.Baseline].
    public double? BaselineOffset { get; }

    /// The [TextBaseline] to align to.
    ///
    /// Used with [PlaceholderAlignment.Baseline],
    /// [PlaceholderAlignment.AboveBaseline], and
    /// [PlaceholderAlignment.BelowBaseline].
    public TextBaseline? Baseline { get; }

    public override string ToString()
    {
        return Size.Equals(default(Size)) && Alignment == PlaceholderAlignment.Bottom
            ? "PlaceholderDimensions.empty"
            : $"PlaceholderDimensions({Size}, {Baseline})";
    }
}

/// An immutable placeholder that is embedded inline within text.
///
/// [PlaceholderSpan] represents a placeholder that acts as a stand-in for other
/// content. A [PlaceholderSpan] by itself does not contain useful information
/// to change a [TextSpan]. [WidgetSpan] extends [PlaceholderSpan] and may be
/// used instead to specify a widget as the contents of the placeholder.
public abstract class PlaceholderSpan : InlineSpan
{
    /// Creates a [PlaceholderSpan] with the given values.
    ///
    /// A [TextStyle] may be provided with the [Style] property, but only the
    /// decoration, foreground, background, and spacing options will be used.
    protected PlaceholderSpan(
        PlaceholderAlignment alignment = PlaceholderAlignment.Bottom,
        TextBaseline? baseline = null,
        TextStyle? style = null)
        : base(style)
    {
        Alignment = alignment;
        Baseline = baseline;
    }

    /// The unicode character to represent a placeholder.
    public const int PlaceholderCodeUnit = 0xFFFC;

    /// How the placeholder aligns vertically with the text.
    public PlaceholderAlignment Alignment { get; }

    /// The [TextBaseline] to align against when using [PlaceholderAlignment.Baseline],
    /// [PlaceholderAlignment.AboveBaseline], and [PlaceholderAlignment.BelowBaseline].
    ///
    /// This is ignored when using other alignment modes.
    public TextBaseline? Baseline { get; }

    /// [PlaceholderSpan]s are flattened to a `0xFFFC` object replacement character
    /// in the plain text representation when `includePlaceholders` is true.
    protected internal override void ComputeToPlainText(
        StringBuilder buffer,
        bool includeSemanticsLabels = true,
        bool includePlaceholders = true)
    {
        if (includePlaceholders)
        {
            buffer.Append((char)PlaceholderCodeUnit);
        }
    }

    protected internal override void ComputeSemanticsInformation(List<InlineSpanSemanticsInformation> collector)
    {
        collector.Add(InlineSpanSemanticsInformation.Placeholder);
    }
}
