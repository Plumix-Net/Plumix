using Avalonia;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/widget_span.dart

namespace Plumix.Widgets;

/// An immutable widget that is embedded inline within text.
///
/// The [Child] property is the widget that will be embedded. Children are
/// constrained by the width of the paragraph.
///
/// [WidgetSpan]s will be ignored when passed into a bare paragraph builder. To
/// properly lay out and paint the [Child] widget, [WidgetSpan] should be passed
/// into a [Text.Rich] or [RichText] widget.
public sealed class WidgetSpan : PlaceholderSpan
{
    /// Creates a [WidgetSpan] with the given values.
    ///
    /// [WidgetSpan] is a leaf node in the [InlineSpan] tree. Child widgets are
    /// constrained by the width of the paragraph they occupy. Child widget
    /// heights are unconstrained, and may cause the text to overflow and be
    /// ellipsized/truncated.
    public WidgetSpan(
        Widget child,
        PlaceholderAlignment alignment = PlaceholderAlignment.Bottom,
        TextBaseline? baseline = null,
        TextStyle? style = null)
        : base(alignment, baseline, style)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (baseline is null
            && alignment is PlaceholderAlignment.AboveBaseline
                or PlaceholderAlignment.BelowBaseline
                or PlaceholderAlignment.Baseline)
        {
            throw new ArgumentException(
                "A baseline is required for baseline-relative alignments.",
                nameof(baseline));
        }

        Child = child;
    }

    /// The widget to embed inline within text.
    public Widget Child { get; }

    /// Helper function for extracting [WidgetSpan]s in preorder, from the given
    /// [InlineSpan], as a list of widgets.
    ///
    /// The `textScaler` is the scaling strategy for scaling the content.
    public static List<Widget> ExtractFromInlineSpan(InlineSpan span, TextScaler textScaler)
    {
        ArgumentNullException.ThrowIfNull(span);
        ArgumentNullException.ThrowIfNull(textScaler);
        var widgets = new List<Widget>();
        var fontSizeStack = new List<double> { TextDefaults.DefaultFontSize };

        bool VisitSubtree(InlineSpan current)
        {
            double? fontSizeToPush = current.Style?.FontSize is double size && size != fontSizeStack[^1]
                ? size
                : null;
            if (fontSizeToPush is not null)
            {
                fontSizeStack.Add(fontSizeToPush.Value);
            }

            if (current is WidgetSpan widgetSpan)
            {
                double fontSize = fontSizeStack[^1];
                double textScaleFactor = fontSize == 0 ? 0 : textScaler.Scale(fontSize) / fontSize;
                widgets.Add(new WidgetSpanParentData(
                    widgetSpan,
                    new AutoScaleInlineWidget(widgetSpan, textScaleFactor, widgetSpan.Child)));
            }

            current.VisitDirectChildren(VisitSubtree);
            if (fontSizeToPush is not null)
            {
                fontSizeStack.RemoveAt(fontSizeStack.Count - 1);
            }

            return true;
        }

        VisitSubtree(span);
        return widgets;
    }

    /// Calls `visitor` on this [WidgetSpan]. There are no children spans to walk.
    public override bool VisitChildren(InlineSpanVisitor visitor) => visitor(this);

    public override bool VisitDirectChildren(InlineSpanVisitor visitor) => true;

    protected internal override InlineSpan? GetSpanForPositionVisitor(TextPosition position, Accumulator offset)
    {
        if (position.Offset == offset.Value)
        {
            return this;
        }

        offset.Increment(1);
        return null;
    }

    protected internal override int? CodeUnitAtVisitor(int index, Accumulator offset)
    {
        int localOffset = index - offset.Value;
        offset.Increment(1);
        return localOffset == 0 ? PlaceholderCodeUnit : null;
    }

    /// A [WidgetSpan] never contains a text position, so this always returns null.
    public override InlineSpan? GetSpanForPosition(TextPosition position) => null;

    public override RenderComparison CompareTo(InlineSpan other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (ReferenceEquals(this, other))
        {
            return RenderComparison.Identical;
        }

        if (other.GetType() != GetType())
        {
            return RenderComparison.Layout;
        }

        if ((Style is null) != (other.Style is null))
        {
            return RenderComparison.Layout;
        }

        var typedOther = (WidgetSpan)other;
        if (!Equals(Child, typedOther.Child) || Alignment != typedOther.Alignment)
        {
            return RenderComparison.Layout;
        }

        return Style is null ? RenderComparison.Identical : Style.CompareTo(other.Style!);
    }

    public override bool Equals(InlineSpan? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (!base.Equals(other))
        {
            return false;
        }

        return other is WidgetSpan widgetSpan
               && Equals(widgetSpan.Child, Child)
               && widgetSpan.Alignment == Alignment
               && widgetSpan.Baseline == Baseline;
    }

    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Child, Alignment, Baseline);

    public override string ToString() => "WidgetSpan";
}

/// A [ParentDataWidget] that sets [TextParentData.Span].
internal sealed class WidgetSpanParentData : ParentDataWidget<TextParentData>
{
    public WidgetSpanParentData(WidgetSpan span, Widget child, Key? key = null) : base(child, key)
    {
        Span = span;
    }

    public WidgetSpan Span { get; }

    public override Type DebugTypicalAncestorWidgetType => typeof(RichText);

    protected override void ApplyParentData(RenderObject renderObject)
    {
        var parentData = (TextParentData)renderObject.parentData!;
        if (ReferenceEquals(parentData.Span, Span))
        {
            return;
        }

        parentData.Span = Span;
        (renderObject.Parent as RenderObject)?.MarkNeedsLayout();
    }
}

/// A render object widget that automatically applies text scaling to an inline
/// widget.
internal sealed class AutoScaleInlineWidget : SingleChildRenderObjectWidget
{
    public AutoScaleInlineWidget(WidgetSpan span, double textScaleFactor, Widget child, Key? key = null)
        : base(child, key)
    {
        Span = span;
        TextScaleFactor = textScaleFactor;
    }

    public WidgetSpan Span { get; }

    public double TextScaleFactor { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderScaledInlineWidget(Span.Alignment, Span.Baseline, TextScaleFactor);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var scaled = (RenderScaledInlineWidget)renderObject;
        scaled.Alignment = Span.Alignment;
        scaled.Baseline = Span.Baseline;
        scaled.Scale = TextScaleFactor;
    }
}

/// Scales an inline widget by the effective text scale factor.
internal sealed class RenderScaledInlineWidget : RenderProxyBox
{
    private double _scale;
    private PlaceholderAlignment _alignment;
    private TextBaseline? _baseline;

    public RenderScaledInlineWidget(PlaceholderAlignment alignment, TextBaseline? baseline, double scale)
    {
        _alignment = alignment;
        _baseline = baseline;
        _scale = scale;
    }

    public double Scale
    {
        get => _scale;
        set
        {
            if (value == _scale)
            {
                return;
            }

            if (value <= 0 || !double.IsFinite(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), "The inline scale must be finite and positive.");
            }

            _scale = value;
            MarkNeedsLayout();
        }
    }

    public PlaceholderAlignment Alignment
    {
        get => _alignment;
        set
        {
            if (_alignment == value)
            {
                return;
            }

            _alignment = value;
            MarkNeedsLayout();
        }
    }

    public TextBaseline? Baseline
    {
        get => _baseline;
        set
        {
            if (_baseline == value)
            {
                return;
            }

            _baseline = value;
            MarkNeedsLayout();
        }
    }

    protected override double ComputeMaxIntrinsicHeight(double width)
    {
        return (Child?.GetMaxIntrinsicHeight(width / _scale) ?? 0.0) * _scale;
    }

    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        return (Child?.GetMaxIntrinsicWidth(height / _scale) ?? 0.0) * _scale;
    }

    protected override double ComputeMinIntrinsicHeight(double width)
    {
        return (Child?.GetMinIntrinsicHeight(width / _scale) ?? 0.0) * _scale;
    }

    protected override double ComputeMinIntrinsicWidth(double height)
    {
        return (Child?.GetMinIntrinsicWidth(height / _scale) ?? 0.0) * _scale;
    }

    protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline)
    {
        double? childBaseline = Child?.GetDistanceToBaseline(baseline, onlyReal: true);
        return childBaseline is null ? base.ComputeDistanceToActualBaseline(baseline) : _scale * childBaseline.Value;
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        double? distance = Child?.GetDryBaseline(
            new BoxConstraints(MaxWidth: constraints.MaxWidth / _scale),
            baseline);
        return distance is null ? null : _scale * distance.Value;
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        Size unscaled = Child?.GetDryLayout(new BoxConstraints(MaxWidth: constraints.MaxWidth / _scale))
                        ?? default;
        return constraints.Constrain(new Size(unscaled.Width * _scale, unscaled.Height * _scale));
    }

    protected override void PerformLayout()
    {
        RenderBox? child = Child;
        if (child is null)
        {
            Size = Constraints.Constrain(default);
            return;
        }

        // Only constrain the width to the maximum width of the paragraph; leave the
        // height unconstrained, which overflows if expanded past.
        child.Layout(new BoxConstraints(MaxWidth: Constraints.MaxWidth / _scale), parentUsesSize: true);
        Size = Constraints.Constrain(new Size(child.Size.Width * _scale, child.Size.Height * _scale));
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        RenderBox? child = Child;
        if (child is null)
        {
            return;
        }

        if (_scale == 1.0)
        {
            context.PaintChild(child, offset);
            return;
        }

        Matrix4 transform = Matrix4.TranslationValues(offset.X, offset.Y, 0.0);
        transform.ScaleByDouble(_scale, _scale, 1.0, 1);
        context.PushTransform(transform, inner => inner.PaintChild(child, default));
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        RenderBox? child = Child;
        if (child is null)
        {
            return false;
        }

        return child.HitTest(result, new Point(position.X / _scale, position.Y / _scale));
    }
}
