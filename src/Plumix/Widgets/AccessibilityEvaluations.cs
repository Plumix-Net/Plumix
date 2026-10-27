using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/_accessibility_evaluations.dart

namespace Plumix.Widgets;

/// <summary>A violation of a semantics node.</summary>
/// <remarks>
/// Flutter's <c>@internal</c> <c>Violation</c>. Do not use in production: Flutter will make breaking
/// changes to this API, even in patch versions.
/// </remarks>
public sealed class Violation
{
    /// <summary>Creates a violation.</summary>
    public Violation(SemanticsNode node, string reason)
    {
        Node = node;
        Reason = reason;
    }

    /// <summary>The semantics node that has the violation.</summary>
    public SemanticsNode Node { get; }

    /// <summary>The reason for the violation.</summary>
    public string Reason { get; }
}

/// <summary>The result of an accessibility evaluation.</summary>
/// <remarks>Flutter's <c>@internal</c> <c>EvaluationResult</c>. Do not use in production.</remarks>
public sealed class EvaluationResult
{
    /// <summary>Creates an evaluation result.</summary>
    public EvaluationResult(List<Violation> violations)
    {
        Violations = violations;
    }

    /// <summary>The list of violations. Empty if the evaluation passed.</summary>
    public List<Violation> Violations { get; }
}

/// <summary>An accessibility evaluation that can be run on a widget tree.</summary>
/// <remarks>
/// Flutter's <c>@internal</c> <c>AccessibilityEvaluation</c>. Dart's <c>FutureOr</c> result is a
/// <see cref="ValueTask{TResult}"/>: the synchronous evaluations complete before they return. Do not use
/// in production.
/// </remarks>
public abstract class AccessibilityEvaluation
{
    private const string KAccessibilityEvaluationsDisabledErrorMessage =
        "Accessibility evaluations APIs are not enabled.\n"
        + "\n"
        + "Accessibility evaluations APIs are currently experimental. Do not use accessibility evaluations "
        + "APIs in\n"
        + "production applications or plugins published to pub.dev.\n"
        + "\n"
        + "To try experimental accessibility evaluations APIs:\n"
        + "1. Switch to Flutter's main release channel.\n"
        + "2. Turn on the accessibility evaluations feature flag. (See flutter config --help)\n";

    /// <summary>Creates an accessibility evaluation.</summary>
    protected AccessibilityEvaluation()
    {
    }

    /// <summary>Evaluates the widget tree of <paramref name="binding"/> for accessibility violations.</summary>
    /// <exception cref="NotSupportedException">
    /// Dart's <c>UnsupportedError</c>, thrown synchronously when
    /// <see cref="Features.IsAccessibilityEvaluationsEnabled"/> is off.
    /// </exception>
    public ValueTask<EvaluationResult> Evaluate(WidgetsBinding binding)
    {
        if (!Features.IsAccessibilityEvaluationsEnabled)
        {
            throw new NotSupportedException(KAccessibilityEvaluationsDisabledErrorMessage);
        }

        return EvaluateCore(binding);
    }

    /// <remarks>Dart's library-private <c>_evaluate</c>.</remarks>
    private protected abstract ValueTask<EvaluationResult> EvaluateCore(WidgetsBinding binding);

    /// <remarks>
    /// Dart's <c>binding.renderViews</c>: Plumix's <see cref="WidgetsBinding"/> is not a
    /// <see cref="RendererBinding"/>, so the views come from the renderer binding instance.
    /// </remarks>
    private protected static IEnumerable<RenderView> RenderViewsOf(WidgetsBinding binding) =>
        RendererBinding.Instance.RenderViews;

    private protected static SemanticsNode RootSemanticsNodeOf(RenderView view) =>
        view.Owner!.SemanticsOwner!.RootNode!;

    private protected static bool HasFlag(SemanticsFlags flags, SemanticsFlags flag) =>
        (flags & flag) != SemanticsFlags.None;

    /// <remarks>dart:ui's <c>flagsCollection.isEnabled == Tristate.isFalse</c>.</remarks>
    private protected static bool IsDisabled(SemanticsFlags flags) =>
        HasFlag(flags, SemanticsFlags.HasEnabledState) && !HasFlag(flags, SemanticsFlags.IsEnabled);

    /// <remarks>Dart's <c>Rect.inflate</c>.</remarks>
    private protected static Rect Inflate(Rect rect, double delta) =>
        DartGeometry.RectFromLTRB(rect.Left - delta, rect.Top - delta, rect.Right + delta, rect.Bottom + delta);

    /// <remarks>Dart's <c>Rect.intersect</c>, which keeps a negative size when the rects do not overlap.</remarks>
    private protected static Rect Intersect(Rect rect, Rect other) =>
        DartGeometry.RectFromLTRB(
            Math.Max(rect.Left, other.Left),
            Math.Max(rect.Top, other.Top),
            Math.Min(rect.Right, other.Right),
            Math.Min(rect.Bottom, other.Bottom));
}

/// <summary>An evaluation that checks that tap targets are at least a minimum size.</summary>
/// <remarks>Flutter's <c>@internal</c> <c>MinimumTapTargetEvaluation</c>. Do not use in production.</remarks>
public class MinimumTapTargetEvaluation : AccessibilityEvaluation
{
    /// <summary>
    /// The gap between the semantics node and the boundary of its scrollable or of the view below which
    /// the node counts as touching that boundary.
    /// </summary>
    private const double KMinimumGapToBoundary = 0.001;

    /// <summary>Creates a new <see cref="MinimumTapTargetEvaluation"/>.</summary>
    public MinimumTapTargetEvaluation(Size size)
    {
        Size = size;
    }

    /// <summary>The minimum allowed size of a tappable node.</summary>
    public Size Size { get; }

    private protected override ValueTask<EvaluationResult> EvaluateCore(WidgetsBinding binding)
    {
        var violations = new List<Violation>();
        foreach (RenderView view in RenderViewsOf(binding))
        {
            violations.AddRange(Traverse(view.FlutterView, RootSemanticsNodeOf(view)));
        }

        return ValueTask.FromResult(new EvaluationResult(violations));
    }

    private List<Violation> Traverse(FlutterView view, SemanticsNode node)
    {
        var violations = new List<Violation>();
        node.VisitChildren(child =>
        {
            violations.AddRange(Traverse(view, child));
            return true;
        });
        if (node.IsMergedIntoParent)
        {
            return violations;
        }

        if (ShouldSkipNode(node))
        {
            return violations;
        }

        Rect paintBounds = node.Rect;
        SemanticsNode? current = node;
        while (current is not null)
        {
            Matrix4? transform = current.Transform;
            if (transform is not null)
            {
                paintBounds = MatrixUtils.TransformRect(transform, paintBounds);
            }

            // Skip node if it is touching the edge of a scrollable, since it might
            // be partially scrolled offscreen.
            if (HasFlag(current.Flags, SemanticsFlags.HasImplicitScrolling)
                && IsAtBoundary(paintBounds, current.Rect))
            {
                return violations;
            }

            current = current.Parent;
        }

        var viewRect = new Rect(default, view.PhysicalSize);
        if (IsAtBoundary(paintBounds, viewRect))
        {
            return violations;
        }

        // Shrink by device pixel ratio.
        var candidateSize = new Size(
            paintBounds.Width / view.DevicePixelRatio,
            paintBounds.Height / view.DevicePixelRatio);
        if (candidateSize.Width < Size.Width - Constants.PrecisionErrorTolerance
            || candidateSize.Height < Size.Height - Constants.PrecisionErrorTolerance)
        {
            violations.Add(new Violation(
                node,
                $"{node}: expected tap target size of at least {Diagnostics.DescribeValue(Size)}, "
                + $"but found {Diagnostics.DescribeValue(candidateSize)}\n"));
        }

        return violations;
    }

    private static bool IsAtBoundary(Rect child, Rect parent)
    {
        if (child.Left - parent.Left > KMinimumGapToBoundary
            && parent.Right - child.Right > KMinimumGapToBoundary
            && child.Top - parent.Top > KMinimumGapToBoundary
            && parent.Bottom - child.Bottom > KMinimumGapToBoundary)
        {
            return false;
        }

        return true;
    }

    /// <summary>Returns whether <paramref name="node"/> should be skipped.</summary>
    /// <remarks>Skips nodes that are not tappable or that are links.</remarks>
    public bool ShouldSkipNode(SemanticsNode node)
    {
        SemanticsData data = node.GetSemanticsData();
        // Skip node if it has no actions, or is marked as hidden.
        if ((!data.HasAction(SemanticsActions.LongPress) && !data.HasAction(SemanticsActions.Tap))
            || HasFlag(data.Flags, SemanticsFlags.IsHidden))
        {
            return true;
        }

        // Skip links https://www.w3.org/WAI/WCAG21/Understanding/target-size.html#excepts
        if (HasFlag(data.Flags, SemanticsFlags.IsLink))
        {
            return true;
        }

        return false;
    }
}

/// <summary>An evaluation that checks that tappable nodes have a semantic label.</summary>
/// <remarks>Flutter's <c>@internal</c> <c>LabeledTapTargetEvaluation</c>. Do not use in production.</remarks>
public class LabeledTapTargetEvaluation : AccessibilityEvaluation
{
    /// <summary>Creates a new <see cref="LabeledTapTargetEvaluation"/>.</summary>
    public LabeledTapTargetEvaluation()
    {
    }

    private protected override ValueTask<EvaluationResult> EvaluateCore(WidgetsBinding binding)
    {
        var violations = new List<Violation>();
        foreach (RenderView view in RenderViewsOf(binding))
        {
            violations.AddRange(Traverse(RootSemanticsNodeOf(view)));
        }

        return ValueTask.FromResult(new EvaluationResult(violations));
    }

    private static List<Violation> Traverse(SemanticsNode node)
    {
        var violations = new List<Violation>();
        node.VisitChildren(child =>
        {
            violations.AddRange(Traverse(child));
            return true;
        });
        if (node.IsMergedIntoParent
            || node.IsInvisible
            || HasFlag(node.Flags, SemanticsFlags.IsHidden)
            || HasFlag(node.Flags, SemanticsFlags.IsTextField))
        {
            return violations;
        }

        SemanticsData data = node.GetSemanticsData();
        // Skip node if it has no actions.
        if (!data.HasAction(SemanticsActions.LongPress) && !data.HasAction(SemanticsActions.Tap))
        {
            return violations;
        }

        if (data.Label.Length == 0 && data.Tooltip.Length == 0)
        {
            violations.Add(new Violation(
                node,
                $"{node}: expected tappable node to have semantic label, but none was found."));
        }

        return violations;
    }
}

/// <summary>The base of the evaluations that check colour contrast against a capture of each view.</summary>
/// <remarks>
/// Flutter's private <c>_ContrastEvaluation</c>. C# cannot derive a public class from a less accessible
/// one, so the base is public with an assembly-private constructor.
/// </remarks>
public abstract class ContrastEvaluation : AccessibilityEvaluation
{
    /// <summary>Tolerance for the contrast ratio comparison.</summary>
    private protected const double KContrastTolerance = -0.01;

    private protected ContrastEvaluation()
    {
    }

    private protected override async ValueTask<EvaluationResult> EvaluateCore(WidgetsBinding binding)
    {
        var violations = new List<Violation>();
        foreach (RenderView renderView in RenderViewsOf(binding))
        {
            var layer = (OffsetLayer)renderView.DebugLayer!;
            SemanticsNode root = RootSemanticsNodeOf(renderView);
            double ratio = 1 / renderView.FlutterView.DevicePixelRatio;
            Bitmap image = await layer.ToImage(renderView.PaintBounds, pixelRatio: ratio).ConfigureAwait(true);
            ByteData byteData = (await image.ToByteData().ConfigureAwait(true))!;
            violations.AddRange(await EvaluateNode(root, image, byteData, renderView).ConfigureAwait(true));
            image.Dispose();
        }

        return new EvaluationResult(violations);
    }

    private async Task<List<Violation>> EvaluateNode(
        SemanticsNode node,
        Bitmap image,
        ByteData byteData,
        RenderView renderView)
    {
        var violations = new List<Violation>();
        if (ShouldSkipNodeTraversal(node))
        {
            return violations;
        }

        SemanticsData data = node.GetSemanticsData();
        var children = new List<SemanticsNode>();
        node.VisitChildren(child =>
        {
            children.Add(child);
            return true;
        });
        foreach (SemanticsNode child in children)
        {
            violations.AddRange(await EvaluateNode(child, image, byteData, renderView).ConfigureAwait(true));
        }

        if (ShouldSkipNodeEvaluation(data))
        {
            return violations;
        }

        // Dart returns only this node's result here, dropping what its children accumulated.
        return await EvaluateNodeContent(node, data, image, byteData, renderView).ConfigureAwait(true);
    }

    private static bool ShouldSkipNodeTraversal(SemanticsNode node) =>
        node.IsInvisible
        || node.IsMergedIntoParent
        || HasFlag(node.Flags, SemanticsFlags.IsHidden)
        || IsDisabled(node.Flags);

    /// <remarks>Dart's library-private <c>_shouldSkipNodeEvaluation</c>.</remarks>
    private protected abstract bool ShouldSkipNodeEvaluation(SemanticsData data);

    /// <summary>Evaluates the content of <paramref name="node"/> against the captured view.</summary>
    /// <remarks>
    /// Flutter's <c>evaluateNodeContent</c>. <paramref name="image"/> is the view's capture at one image
    /// pixel per logical pixel and <paramref name="byteData"/> its premultiplied RGBA bytes.
    /// </remarks>
    public abstract Task<List<Violation>> EvaluateNodeContent(
        SemanticsNode node,
        SemanticsData data,
        Bitmap image,
        ByteData byteData,
        RenderView renderView);

    private protected static bool IsNodeOffScreen(Rect paintBounds, FlutterView window)
    {
        double logicalWidth = window.PhysicalSize.Width / window.DevicePixelRatio;
        double logicalHeight = window.PhysicalSize.Height / window.DevicePixelRatio;
        return paintBounds.Top < -50.0
               || paintBounds.Left < -50.0
               || paintBounds.Bottom > logicalHeight + 50.0
               || paintBounds.Right > logicalWidth + 50.0;
    }

    /// <summary>The frequency of each colour within <paramref name="paintBounds"/>, in first-seen order.</summary>
    /// <remarks>
    /// Flutter's private top-level <c>_colorsWithinRect</c>: the pixels are read big-endian (RGBA) column by
    /// column and converted to ARGB colours.
    /// </remarks>
    private protected static OrderedDictionary<Color, int> ColorsWithinRect(
        ByteData data,
        Rect paintBounds,
        int width,
        int height)
    {
        Rect truePaintBounds = Intersect(paintBounds, new Rect(0, 0, width, height));

        int leftX = (int)Math.Floor(truePaintBounds.Left);
        int rightX = (int)Math.Ceiling(truePaintBounds.Right);
        int topY = (int)Math.Floor(truePaintBounds.Top);
        int bottomY = (int)Math.Ceiling(truePaintBounds.Bottom);

        var rgbaToCount = new OrderedDictionary<uint, int>();
        for (int x = leftX; x < rightX; x++)
        {
            for (int y = topY; y < bottomY; y++)
            {
                uint pixel = data.GetUint32(((y * width) + x) * 4);
                rgbaToCount[pixel] = rgbaToCount.TryGetValue(pixel, out int count) ? count + 1 : 1;
            }
        }

        var colors = new OrderedDictionary<Color, int>();
        foreach (KeyValuePair<uint, int> entry in rgbaToCount)
        {
            uint rgba = entry.Key;
            uint argb = (rgba << 24) | (rgba >> 8);
            colors[new Color(argb)] = entry.Value;
        }

        return colors;
    }
}

/// <summary>An evaluation that checks that text has enough contrast against its background.</summary>
/// <remarks>Flutter's <c>@internal</c> <c>MinimumTextContrastEvaluation</c>. Do not use in production.</remarks>
public class MinimumTextContrastEvaluation : ContrastEvaluation
{
    /// <summary>The minimum text size considered large for contrast checking.</summary>
    /// <remarks>Defined by http://www.w3.org/TR/UNDERSTANDING-WCAG20/visual-audio-contrast-contrast.html.</remarks>
    public const int KLargeTextMinimumSize = 18;

    /// <summary>The minimum text size for bold text to be considered large for contrast checking.</summary>
    /// <remarks>Defined by http://www.w3.org/TR/UNDERSTANDING-WCAG20/visual-audio-contrast-contrast.html.</remarks>
    public const int KBoldTextMinimumSize = 14;

    /// <summary>The minimum contrast ratio for normal text.</summary>
    /// <remarks>Defined by http://www.w3.org/TR/UNDERSTANDING-WCAG20/visual-audio-contrast-contrast.html.</remarks>
    public const double KMinimumRatioNormalText = 4.5;

    /// <summary>The minimum contrast ratio for large text.</summary>
    /// <remarks>Defined by http://www.w3.org/TR/UNDERSTANDING-WCAG20/visual-audio-contrast-contrast.html.</remarks>
    public const double KMinimumRatioLargeText = 3.0;

    private const double KDefaultFontSize = 12.0;

    /// <summary>Creates a new <see cref="MinimumTextContrastEvaluation"/>.</summary>
    public MinimumTextContrastEvaluation(double minNormalTextContrastRatio, double minLargeTextContrastRatio)
    {
        MinNormalTextContrastRatio = minNormalTextContrastRatio;
        MinLargeTextContrastRatio = minLargeTextContrastRatio;
    }

    /// <summary>The minimum contrast ratio for normal text.</summary>
    public double MinNormalTextContrastRatio { get; }

    /// <summary>The minimum contrast ratio for large text.</summary>
    public double MinLargeTextContrastRatio { get; }

    private protected override bool ShouldSkipNodeEvaluation(SemanticsData data) =>
        HasFlag(data.Flags, SemanticsFlags.ScopesRoute)
        || (data.Label.Trim().Length == 0 && data.Value.Trim().Length == 0);

    /// <inheritdoc/>
    public override async Task<List<Violation>> EvaluateNodeContent(
        SemanticsNode node,
        SemanticsData data,
        Bitmap image,
        ByteData byteData,
        RenderView renderView)
    {
        string text = data.Label.Length == 0 ? data.Value : data.Label;
        IEnumerable<Element> elements = CollectElementsByText(WidgetsBinding.Instance.RootElement!, text);
        var violations = new List<Violation>();
        foreach (Element element in elements)
        {
            violations.AddRange(
                await EvaluateElement(node, element, image, byteData, renderView).ConfigureAwait(true));
        }

        return violations;
    }

    private Task<List<Violation>> EvaluateElement(
        SemanticsNode node,
        Element element,
        Bitmap image,
        ByteData byteData,
        RenderView renderView)
    {
        // Look up inherited text properties to determine text size and weight.
        bool isBold;
        double? fontSize;

        RenderObject? renderObject = element.RenderObject;
        if (renderObject is not RenderBox renderBox)
        {
            throw new InvalidOperationException(
                $"Unexpected renderObject type: {renderObject?.ToString() ?? "null"}");
        }

        Matrix4 globalTransform = renderBox.GetTransformTo(null);
        Rect paintBoundsWithOffset = MatrixUtils.TransformRect(globalTransform, Inflate(renderBox.PaintBounds, 4.0));

        // The semantics node transform will include root view transform, which is
        // not included in renderBox.getTransformTo(null). Manually multiply the
        // root transform to the global transform.
        var rootTransform = Matrix4.Identity();
        renderView.ApplyPaintTransform(renderView.Child!, rootTransform);
        rootTransform.Multiply(globalTransform);
        Rect screenBounds = MatrixUtils.TransformRect(rootTransform, renderBox.PaintBounds);
        Rect nodeBounds = node.Rect;
        SemanticsNode? current = node;
        while (current is not null)
        {
            Matrix4? transform = current.Transform;
            if (transform is not null)
            {
                nodeBounds = MatrixUtils.TransformRect(transform, nodeBounds);
            }

            current = current.Parent;
        }

        Rect intersection = Intersect(nodeBounds, screenBounds);
        if (intersection.Width <= 0 || intersection.Height <= 0)
        {
            // Skip this element since it doesn't correspond to the given semantic
            // node.
            return Task.FromResult(new List<Violation>());
        }

        Widget widget = element.Widget;
        DefaultTextStyle defaultTextStyle = DefaultTextStyle.Of(element);
        if (widget is Text textWidget)
        {
            TextStyle? style = textWidget.Style;
            TextStyle effectiveTextStyle = style is null || style.Inherit
                ? defaultTextStyle.Style.Merge(textWidget.Style)
                : style;
            isBold = effectiveTextStyle.FontWeight == FontWeight.Bold;
            fontSize = effectiveTextStyle.FontSize;
        }
        else if (widget is EditableText editableText)
        {
            isBold = editableText.Style.FontWeight == FontWeight.Bold;
            fontSize = editableText.Style.FontSize;
        }
        else
        {
            throw new InvalidOperationException($"Unexpected widget type: {widget.GetType().Name}");
        }

        if (IsNodeOffScreen(paintBoundsWithOffset, renderView.FlutterView))
        {
            return Task.FromResult(new List<Violation>());
        }

        OrderedDictionary<Color, int> colorHistogram = ColorsWithinRect(
            byteData,
            paintBoundsWithOffset,
            image.PixelSize.Width,
            image.PixelSize.Height);

        // Node was too far off screen.
        if (colorHistogram.Count == 0)
        {
            return Task.FromResult(new List<Violation>());
        }

        var report = new ContrastReport(colorHistogram);
        double contrastRatio = report.ContrastRatio();
        double targetContrastRatio = TargetContrastRatio(fontSize, bold: isBold);

        if (contrastRatio - targetContrastRatio >= KContrastTolerance)
        {
            return Task.FromResult(new List<Violation>());
        }

        string fontSizeText = fontSize is { } size ? BindingBase.DartDoubleToString(size) : "null";
        return Task.FromResult(new List<Violation>
        {
            new(
                node,
                $"{node}:\n"
                + $"Expected contrast ratio of at least {BindingBase.DartDoubleToString(targetContrastRatio)} "
                + $"but found {Diagnostics.ToStringAsFixed(contrastRatio, 2)} "
                + $"for a font size of {fontSizeText}.\n"
                + "The computed colors were:\n"
                + $"light - {report.LightColor}, dark - {report.DarkColor}\n"
                + "See also: "
                + "https://www.w3.org/TR/UNDERSTANDING-WCAG20/visual-audio-contrast-contrast.html"),
        });
    }

    /// <summary>Returns the required contrast ratio for the <paramref name="fontSize"/> and weight.</summary>
    /// <remarks>If <paramref name="fontSize"/> is null, the 12 pixel default is assumed.</remarks>
    private double TargetContrastRatio(double? fontSize, bool bold)
    {
        double fontSizeOrDefault = fontSize ?? KDefaultFontSize;
        if ((bold && fontSizeOrDefault >= KBoldTextMinimumSize) || fontSizeOrDefault >= KLargeTextMinimumSize)
        {
            return MinLargeTextContrastRatio;
        }

        return MinNormalTextContrastRatio;
    }

    /// <summary>
    /// The <see cref="Text"/> elements below <paramref name="root"/> whose data is <paramref name="text"/>,
    /// in pre-order.
    /// </summary>
    /// <remarks>Flutter's private top-level <c>_collectElementsByText</c>.</remarks>
    private static IEnumerable<Element> CollectElementsByText(Element root, string text)
    {
        var result = new List<Element>();
        root.VisitChildren(child =>
        {
            if (child.Widget is Text textWidget && textWidget.Data == text)
            {
                result.Add(child);
            }

            result.AddRange(CollectElementsByText(child, text));
        });
        return result;
    }
}

/// <summary>An evaluation that checks that non-text controls have enough contrast against their background.</summary>
/// <remarks>Flutter's <c>@internal</c> <c>MinimumNonTextContrastEvaluation</c>. Do not use in production.</remarks>
public class MinimumNonTextContrastEvaluation : ContrastEvaluation
{
    /// <summary>The minimum contrast ratio for non-text controls.</summary>
    /// <remarks>Defined by https://www.w3.org/WAI/WCAG22/Understanding/non-text-contrast.html.</remarks>
    private const double KMinimumRatioNonText = 3.0;

    /// <summary>Creates a new <see cref="MinimumNonTextContrastEvaluation"/>.</summary>
    public MinimumNonTextContrastEvaluation()
    {
    }

    private protected override bool ShouldSkipNodeEvaluation(SemanticsData data)
    {
        if (HasFlag(data.Flags, SemanticsFlags.ScopesRoute))
        {
            return true;
        }

        bool isControl = HasFlag(data.Flags, SemanticsFlags.IsButton)
                         || HasFlag(data.Flags, SemanticsFlags.IsSlider)
                         || HasFlag(data.Flags, SemanticsFlags.IsTextField)
                         || HasFlag(data.Flags, SemanticsFlags.HasCheckedState)
                         || HasFlag(data.Flags, SemanticsFlags.HasToggledState)
                         || data.HasAction(SemanticsActions.Tap)
                         || data.HasAction(SemanticsActions.LongPress);

        return !isControl;
    }

    /// <inheritdoc/>
    public override Task<List<Violation>> EvaluateNodeContent(
        SemanticsNode node,
        SemanticsData data,
        Bitmap image,
        ByteData byteData,
        RenderView renderView)
    {
        var violations = new List<Violation>();
        Rect nodeBounds = node.Rect;
        SemanticsNode? current = node;
        while (current is not null)
        {
            Matrix4? transform = current.Transform;
            if (transform is not null && current.Parent is not null)
            {
                nodeBounds = MatrixUtils.TransformRect(transform, nodeBounds);
            }

            current = current.Parent;
        }

        double devicePixelRatio = renderView.FlutterView.DevicePixelRatio;
        Rect logicalBounds = DartGeometry.RectFromLTRB(
            nodeBounds.Left / devicePixelRatio,
            nodeBounds.Top / devicePixelRatio,
            nodeBounds.Right / devicePixelRatio,
            nodeBounds.Bottom / devicePixelRatio);

        Rect inflatedBounds = Inflate(logicalBounds, 4.0);

        if (IsNodeOffScreen(inflatedBounds, renderView.FlutterView))
        {
            return Task.FromResult(violations);
        }

        OrderedDictionary<Color, int> colorHistogram = ColorsWithinRect(
            byteData,
            inflatedBounds,
            image.PixelSize.Width,
            image.PixelSize.Height);

        if (colorHistogram.Count <= 1)
        {
            return Task.FromResult(violations);
        }

        var report = new ContrastReport(colorHistogram);
        double contrastRatio = report.ContrastRatio();

        if (contrastRatio - KMinimumRatioNonText >= KContrastTolerance)
        {
            return Task.FromResult(violations);
        }

        violations.Add(new Violation(
            node,
            $"{node}:\n"
            + "Expected non-text control contrast ratio of at least "
            + $"{Diagnostics.ToStringAsFixed(KMinimumRatioNonText, 1)} "
            + $"but found {Diagnostics.ToStringAsFixed(contrastRatio, 2)}.\n"
            + "The computed colors were:\n"
            + $"light - {report.LightColor}, dark - {report.DarkColor}\n"
            + "See also: "
            + "https://www.w3.org/WAI/WCAG22/Understanding/non-text-contrast.html"));

        return Task.FromResult(violations);
    }
}

/// <summary>The light and dark colour of a histogram, split around its average lightness.</summary>
/// <remarks>Flutter's private <c>_ContrastReport</c>.</remarks>
internal sealed class ContrastReport
{
    /// <summary>
    /// Generates a contrast report given a color histogram: the most frequent colour lighter than the
    /// average lightness and the most frequent one at or below it.
    /// </summary>
    public ContrastReport(OrderedDictionary<Color, int> colorHistogram)
    {
        // To determine the lighter and darker color, partition the colors
        // by HSL lightness and then choose the mode from each group.
        double totalLightness = 0.0;
        int count = 0;
        foreach (KeyValuePair<Color, int> entry in colorHistogram)
        {
            totalLightness += HSLColor.FromColor(entry.Key).Lightness * entry.Value;
            count += entry.Value;
        }

        double averageLightness = totalLightness / count;
        System.Diagnostics.Debug.Assert(!double.IsNaN(averageLightness));

        KeyValuePair<Color, int>? lightColor = null;
        KeyValuePair<Color, int>? darkColor = null;

        // Find the most frequently occurring light and dark color.
        foreach (KeyValuePair<Color, int> entry in colorHistogram)
        {
            double lightness = HSLColor.FromColor(entry.Key).Lightness;
            int frequency = entry.Value;
            if (lightness <= averageLightness)
            {
                if (frequency > (darkColor?.Value ?? 0))
                {
                    darkColor = entry;
                }
            }
            else if (frequency > (lightColor?.Value ?? 0))
            {
                lightColor = entry;
            }
        }

        // If there is only a single color, it is reported as both dark and light.
        LightColor = lightColor?.Key ?? darkColor!.Value.Key;
        DarkColor = darkColor?.Key ?? lightColor!.Value.Key;
    }

    /// <summary>The most frequently occurring light color.</summary>
    public Color LightColor { get; }

    /// <summary>The most frequently occurring dark color.</summary>
    public Color DarkColor { get; }

    /// <summary>Computes the contrast ratio as defined by the WCAG.</summary>
    /// <remarks>Source: https://www.w3.org/TR/UNDERSTANDING-WCAG20/visual-audio-contrast-contrast.html.</remarks>
    public double ContrastRatio() => (LightColor.ComputeLuminance() + 0.05) / (DarkColor.ComputeLuminance() + 0.05);
}

/// <summary>An evaluation that checks that important leaf semantics nodes are labeled.</summary>
/// <remarks>Flutter's <c>@internal</c> <c>UnlabeledLeafNodeEvaluation</c>. Do not use in production.</remarks>
public class UnlabeledLeafNodeEvaluation : AccessibilityEvaluation
{
    /// <summary>
    /// Dart's private top-level <c>_scrollingActions</c>: the actions that do not by themselves make a node
    /// important for accessibility.
    /// </summary>
    private const SemanticsActions ScrollingActions =
        SemanticsActions.ScrollUp
        | SemanticsActions.ScrollDown
        | SemanticsActions.ScrollLeft
        | SemanticsActions.ScrollRight
        | SemanticsActions.ScrollToOffset;

    /// <summary>Creates a new <see cref="UnlabeledLeafNodeEvaluation"/>.</summary>
    public UnlabeledLeafNodeEvaluation()
    {
    }

    private protected override ValueTask<EvaluationResult> EvaluateCore(WidgetsBinding binding)
    {
        var violations = new List<Violation>();
        foreach (RenderView view in RenderViewsOf(binding))
        {
            violations.AddRange(Traverse(RootSemanticsNodeOf(view)));
        }

        return ValueTask.FromResult(new EvaluationResult(violations));
    }

    private static List<Violation> Traverse(SemanticsNode node)
    {
        var violations = new List<Violation>();
        bool hasChildren = false;
        node.VisitChildren(child =>
        {
            hasChildren = true;
            violations.AddRange(Traverse(child));
            return true;
        });

        if (node.IsInvisible || HasFlag(node.Flags, SemanticsFlags.IsHidden))
        {
            return violations;
        }

        if (hasChildren && !node.MergeAllDescendantsIntoThisNode)
        {
            return violations;
        }

        if (!IsImportantForAccessibility(node))
        {
            return violations;
        }

        SemanticsData data = node.GetSemanticsData();
        if (data.Label.Trim().Length == 0
            && data.Value.Trim().Length == 0
            && data.Hint.Trim().Length == 0
            && data.Tooltip.Trim().Length == 0)
        {
            violations.Add(new Violation(
                node,
                $"{node}: expected leaf semantics node to have a label, value, hint, or tooltip, "
                + "but none was found."));
        }

        return violations;
    }

    /// <remarks>Flutter's private top-level <c>_isImportantForAccessibility</c>.</remarks>
    private static bool IsImportantForAccessibility(SemanticsNode node)
    {
        if (node.IsMergedIntoParent)
        {
            // If this node is merged, all its information are present on an
            // ancestor node.
            return false;
        }

        SemanticsData data = node.GetSemanticsData();
        // If the node scopes a route, it doesn't matter what other flags/actions it
        // has, it is _not_ important for accessibility, so we short circuit.
        if (HasFlag(data.Flags, SemanticsFlags.ScopesRoute))
        {
            return false;
        }

        bool hasNonScrollingAction = (data.Actions & ~ScrollingActions) != SemanticsActions.None;
        if (hasNonScrollingAction)
        {
            return true;
        }

        bool hasImportantFlag = HasFlag(data.Flags, SemanticsFlags.HasCheckedState)
                                || HasFlag(data.Flags, SemanticsFlags.HasToggledState)
                                || HasFlag(data.Flags, SemanticsFlags.HasEnabledState)
                                || HasFlag(data.Flags, SemanticsFlags.IsButton)
                                || HasFlag(data.Flags, SemanticsFlags.IsTextField)
                                || HasFlag(data.Flags, SemanticsFlags.IsFocusable)
                                || HasFlag(data.Flags, SemanticsFlags.IsSlider)
                                || HasFlag(data.Flags, SemanticsFlags.IsInMutuallyExclusiveGroup);
        if (hasImportantFlag)
        {
            return true;
        }

        bool hasContent = data.Label.Length != 0
                          || data.Value.Length != 0
                          || data.Hint.Length != 0
                          || data.Tooltip.Length != 0;
        if (hasContent)
        {
            return true;
        }

        return false;
    }
}

/// <summary>An evaluation that checks that the application has at least one <see cref="Title"/> widget.</summary>
/// <remarks>Flutter's <c>@internal</c> <c>TitleEvaluation</c>. Do not use in production.</remarks>
public class TitleEvaluation : AccessibilityEvaluation
{
    /// <summary>Creates a new <see cref="TitleEvaluation"/>.</summary>
    public TitleEvaluation()
    {
    }

    private protected override ValueTask<EvaluationResult> EvaluateCore(WidgetsBinding binding)
    {
        var violations = new List<Violation>();
        if (binding.RootElement is { } rootElement && !HasTitleWidget(rootElement))
        {
            SemanticsNode rootNode = RootSemanticsNodeOf(RenderViewsOf(binding).First());
            violations.Add(new Violation(
                rootNode,
                "Expected to find at least one Title widget, but none was found."));
        }

        return ValueTask.FromResult(new EvaluationResult(violations));
    }

    private static bool HasTitleWidget(Element element)
    {
        if (element.Widget is Title)
        {
            return true;
        }

        bool found = false;
        element.VisitChildren(child =>
        {
            if (!found)
            {
                found = HasTitleWidget(child);
            }
        });
        return found;
    }
}
