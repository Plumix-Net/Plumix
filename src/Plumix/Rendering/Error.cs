using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Plumix.Foundation;
using Plumix.UI;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/error.dart

/// <summary>
/// The typeface-level style <see cref="RenderErrorBox"/> paints its message with.
/// </summary>
/// <remarks>
/// Dart's static is a `dart:ui` <c>TextStyle</c> — the engine-level type, deliberately used instead
/// of `painting`'s <c>TextStyle</c> so the error box does not depend on the text subsystem. Plumix's
/// engine-level equivalent is an Avalonia <see cref="Typeface"/> plus a brush, so the fields the box
/// actually needs are carried here.
/// </remarks>
public sealed record RenderErrorBoxTextStyle(
    Color Color,
    FontFamily FontFamily,
    double FontSize,
    FontWeight FontWeight);

/// <summary>
/// The paragraph-level style <see cref="RenderErrorBox"/> lays its message out with.
/// </summary>
/// <remarks>Dart's `dart:ui` <c>ParagraphStyle</c>, reduced to the two fields it sets.</remarks>
public sealed record RenderErrorBoxParagraphStyle(
    FlowDirection FlowDirection,
    TextAlignment TextAlignment);

/// <summary>
/// A render object used as a placeholder when an error occurs.
/// </summary>
/// <remarks>
/// Dart's <c>RenderErrorBox</c>. The box paints in <see cref="BackgroundColor"/>; a message given to
/// the constructor is laid out once, there and then, and painted on top. The message cannot change
/// after construction, which keeps this class — the one that runs when everything else has already
/// failed — as simple as possible. When the parent leaves the constraints unbounded the box tries to
/// be 100000.0 pixels wide and high, approximating infinity without using it.
/// </remarks>
public class RenderErrorBox : RenderBox
{
    private const double KMaxWidth = 100000.0;
    private const double KMaxHeight = 100000.0;

    private readonly RenderErrorBoxTextStyle? _style;
    private readonly RenderErrorBoxParagraphStyle? _paragraphStyle;
    private TextLayout? _paragraph;
    private double _paragraphWidth = double.NaN;

    public RenderErrorBox(string message = "")
    {
        Message = message;
        try
        {
            if (message != string.Empty)
            {
                // This class is intentionally doing things using the low-level primitives to avoid
                // depending on any subsystems that may have ended up in an unstable state — after
                // all, this class is mainly used when things have gone wrong. Generally, the much
                // better way to draw text in a RenderObject is to use TextPainter.
                _style = TextStyle;
                _paragraphStyle = ParagraphStyle;
            }
        }
        catch (Exception)
        {
            // If an error happens here we're in a terrible state, so we really should just forget
            // about it and let the developer deal with the already-reported errors.
        }
    }

    /// <summary>The distance to place around the text.</summary>
    /// <remarks>
    /// Dart's <c>RenderErrorBox.padding</c>: it keeps the text below a system status bar when the
    /// box sits at the top left of the screen. Ignored if the box is smaller than the padding.
    /// </remarks>
    public static EdgeInsets Padding { get; set; } = new(64.0, 96.0, 64.0, 12.0);

    /// <summary>The width below which the horizontal padding is not applied.</summary>
    public static double MinimumWidth { get; set; } = 200.0;

    /// <summary>
    /// The color to use when painting the background. Red in debug mode, a light gray otherwise.
    /// </summary>
    public static Color BackgroundColor { get; set; } = InitBackgroundColor();

    /// <summary>
    /// The text style to use when painting the message. A yellow monospace font in debug mode, and a
    /// dark gray sans-serif font otherwise.
    /// </summary>
    public static RenderErrorBoxTextStyle TextStyle { get; set; } = InitTextStyle();

    /// <summary>The paragraph style to use when painting the message.</summary>
    public static RenderErrorBoxParagraphStyle ParagraphStyle { get; set; } =
        new(FlowDirection.LeftToRight, TextAlignment.Left);

    /// <summary>The message to attempt to display at paint time.</summary>
    public string Message { get; }

    protected override bool SizedByParent => true;

    protected override double ComputeMaxIntrinsicWidth(double height) => KMaxWidth;

    protected override double ComputeMaxIntrinsicHeight(double width) => KMaxHeight;

    protected override bool HitTestSelf(Point position) => true;

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        return constraints.Constrain(new Size(KMaxWidth, KMaxHeight));
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        try
        {
            ctx.Canvas.DrawRectangle(
                new SolidColorBrush(BackgroundColor),
                null,
                new Rect(offset, Size));
            if (_style is not null)
            {
                EdgeInsets padding = Padding;
                double width = Size.Width;
                double left = 0.0;
                double top = 0.0;
                if (width > padding.Left + MinimumWidth + padding.Right)
                {
                    width -= padding.Left + padding.Right;
                    left += padding.Left;
                }

                TextLayout paragraph = LayoutParagraph(width);
                if (Size.Height > padding.Top + paragraph.Height + padding.Bottom)
                {
                    top += padding.Top;
                }

                ctx.Canvas.DrawTextLayout(paragraph, offset + new Point(left, top));
            }
        }
        catch (Exception)
        {
            // If an error happens here we're in a terrible state, so we really should just forget
            // about it and let the developer deal with the already-reported errors.
        }
    }

    /// <remarks>
    /// Dart builds the <c>ui.Paragraph</c> in the constructor and calls <c>layout(width)</c> from
    /// <c>paint</c>. Avalonia bakes the wrap width into <see cref="TextLayout"/>, so the style is
    /// snapshotted in the constructor as Dart does and the layout is (re)built here, cached per width.
    /// </remarks>
    private TextLayout LayoutParagraph(double width)
    {
        if (_paragraph is not null && _paragraphWidth.Equals(width))
        {
            return _paragraph;
        }

        RenderErrorBoxTextStyle style = _style!;
        RenderErrorBoxParagraphStyle paragraphStyle = _paragraphStyle!;
        _paragraph = new TextLayout(
            Message,
            new Typeface(style.FontFamily, FontStyle.Normal, style.FontWeight, FontStretch.Normal),
            style.FontSize,
            new SolidColorBrush(style.Color),
            textAlignment: paragraphStyle.TextAlignment,
            textWrapping: TextWrapping.Wrap,
            maxWidth: width,
            flowDirection: paragraphStyle.FlowDirection);
        _paragraphWidth = width;
        return _paragraph;
    }

    private static Color InitBackgroundColor()
    {
        return Constants.KDebugMode ? Color.FromUInt32(0xF0900000) : Color.FromUInt32(0xF0C0C0C0);
    }

    private static RenderErrorBoxTextStyle InitTextStyle()
    {
        return Constants.KDebugMode
            ? new RenderErrorBoxTextStyle(
                Color.FromUInt32(0xFFFFFF66),
                FontFamily.Parse("monospace"),
                14.0,
                FontWeight.Bold)
            : new RenderErrorBoxTextStyle(
                Color.FromUInt32(0xFF303030),
                FontFamily.Parse("sans-serif"),
                18.0,
                FontWeight.Normal);
    }
}
