using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/debug.dart

namespace Plumix.Rendering;

/// <summary>Signature for <see cref="RenderingDebug.OnProfilePaint"/> implementations.</summary>
/// <remarks>Flutter's <c>ProfilePaintCallback</c>.</remarks>
public delegate void ProfilePaintCallback(RenderObject renderObject);

/// <summary>
/// Debug flags for the rendering subsystem. Ports Dart's `rendering/debug.dart`.
/// </summary>
/// <remarks>
/// Dart's library-level variables live on this static class because C# has no top-level fields, the
/// way <c>Plumix.Gestures.GestureDebug</c> hosts `gestures/debug.dart`. Names drop the `debug`
/// prefix that the namespace already supplies. Any change here must be reflected in
/// <see cref="AssertAllRenderVarsUnset"/>.
/// </remarks>
public static class RenderingDebug
{
    /// <remarks>Flutter's <c>_kDebugDefaultRepaintColor</c>.</remarks>
    private static readonly HSVColor KDebugDefaultRepaintColor = new(0.4, 60.0, 1.0, 1.0);

    /// <summary>
    /// Causes each <see cref="RenderBox"/> to paint a box around its bounds, and some extra boxes,
    /// such as <see cref="RenderPadding"/>, to draw construction lines.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>debugPaintSizeEnabled</c>. The edges of the boxes are painted as a one-pixel
    /// thick <c>0xFF00FFFF</c> outline; spacing is painted as a solid <c>0x90909090</c> area, and
    /// padding is filled in solid <c>0x900090FF</c> with the inner edge outlined in
    /// <c>0xFF0090FF</c>, using <see cref="PaintPadding"/>.
    /// </remarks>
    public static bool PaintSizeEnabled { get; set; }

    /// <summary>Causes each <see cref="RenderBox"/> to paint a line at each of its baselines.</summary>
    /// <remarks>Flutter's <c>debugPaintBaselinesEnabled</c>.</remarks>
    public static bool PaintBaselinesEnabled { get; set; }

    /// <summary>Causes each <c>RenderParagraph</c> to paint the layout boxes of its text.</summary>
    /// <remarks>Flutter's <c>debugPaintTextLayoutBoxes</c>.</remarks>
    public static bool PaintTextLayoutBoxes { get; set; }

    /// <summary>Causes each <see cref="Layer"/> to paint a box around its bounds.</summary>
    /// <remarks>Flutter's <c>debugPaintLayerBordersEnabled</c>.</remarks>
    public static bool PaintLayerBordersEnabled { get; set; }

    /// <summary>
    /// Causes objects like <see cref="RenderPointerListener"/> to flash while they are being tapped.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>debugPaintPointersEnabled</c>. For details on how to support this in a
    /// <see cref="RenderBox"/> subclass, see <see cref="RenderBox.DebugHandleEvent"/>.
    /// </remarks>
    public static bool PaintPointersEnabled { get; set; }

    /// <summary>Overlays a rotating set of colors when repainting layers in debug mode.</summary>
    /// <remarks>Flutter's <c>debugRepaintRainbowEnabled</c>.</remarks>
    public static bool RepaintRainbowEnabled { get; set; }

    /// <summary>Overlays a rotating set of colors when repainting text in debug mode.</summary>
    /// <remarks>Flutter's <c>debugRepaintTextRainbowEnabled</c>.</remarks>
    public static bool RepaintTextRainbowEnabled { get; set; }

    /// <summary>The current color to overlay when repainting a layer.</summary>
    /// <remarks>
    /// Flutter's <c>debugCurrentRepaintColor</c>. The value is incremented by
    /// <c>PipelineOwner.CompositeFrame</c> when either <see cref="RepaintRainbowEnabled"/> or
    /// <see cref="RepaintTextRainbowEnabled"/> is set.
    /// </remarks>
    public static HSVColor CurrentRepaintColor { get; set; } = KDebugDefaultRepaintColor;

    /// <summary>Logs the call stacks that mark render objects as needing layout.</summary>
    /// <remarks>
    /// Flutter's <c>debugPrintMarkNeedsLayoutStacks</c>. Only the cases where an object is added to
    /// the list of nodes needing layout are logged, so a single
    /// <see cref="RenderObject.MarkNeedsLayout"/> call walking up the tree prints one stack.
    /// </remarks>
    public static bool PrintMarkNeedsLayoutStacks { get; set; }

    /// <summary>Logs the call stacks that mark render objects as needing paint.</summary>
    /// <remarks>Flutter's <c>debugPrintMarkNeedsPaintStacks</c>.</remarks>
    public static bool PrintMarkNeedsPaintStacks { get; set; }

    /// <summary>Logs the dirty render objects that are laid out each frame.</summary>
    /// <remarks>Flutter's <c>debugPrintLayouts</c>.</remarks>
    public static bool PrintLayouts { get; set; }

    /// <summary>Checks the intrinsic sizes of each <see cref="RenderBox"/> during layout.</summary>
    /// <remarks>
    /// Flutter's <c>debugCheckIntrinsicSizes</c>. Off by default because the checks are expensive;
    /// turn it on in unit tests that exercise custom intrinsics.
    /// </remarks>
    public static bool CheckIntrinsicSizes { get; set; }

    /// <summary>Adds timeline events for every <see cref="RenderObject"/> layout.</summary>
    /// <remarks>
    /// Flutter's <c>debugProfileLayoutsEnabled</c>. Plumix has no <c>dart:developer</c>
    /// <c>Timeline</c>, so the flag is carried but no events are emitted yet; see
    /// <c>docs/ai/DIVERGENCES.md</c>.
    /// </remarks>
    public static bool ProfileLayoutsEnabled { get; set; }

    /// <summary>Adds timeline events for every <see cref="RenderObject"/> painted.</summary>
    /// <remarks>
    /// Flutter's <c>debugProfilePaintsEnabled</c>. See <see cref="ProfileLayoutsEnabled"/> for why
    /// no events are emitted yet; <see cref="OnProfilePaint"/> is the ported callback equivalent.
    /// </remarks>
    public static bool ProfilePaintsEnabled { get; set; }

    /// <summary>Adds debugging information to timeline events related to layouts.</summary>
    /// <remarks>Flutter's <c>debugEnhanceLayoutTimelineArguments</c>.</remarks>
    public static bool EnhanceLayoutTimelineArguments { get; set; }

    /// <summary>Adds debugging information to timeline events related to paints.</summary>
    /// <remarks>Flutter's <c>debugEnhancePaintTimelineArguments</c>.</remarks>
    public static bool EnhancePaintTimelineArguments { get; set; }

    /// <summary>Callback invoked for every <see cref="RenderObject"/> painted each frame.</summary>
    /// <remarks>Flutter's <c>debugOnProfilePaint</c>; only invoked in debug builds.</remarks>
    public static ProfilePaintCallback? OnProfilePaint { get; set; }

    /// <summary>Causes all clipping effects from the layer tree to be ignored.</summary>
    /// <remarks>
    /// Flutter's <c>debugDisableClipLayers</c>. This does not reduce the number of
    /// <see cref="Layer"/> objects created; it merely causes the clipping layers to be skipped when
    /// building the scene.
    /// </remarks>
    public static bool DisableClipLayers { get; set; }

    /// <summary>Causes all physical modeling effects from the layer tree to be ignored.</summary>
    /// <remarks>Flutter's <c>debugDisablePhysicalShapeLayers</c>.</remarks>
    public static bool DisablePhysicalShapeLayers { get; set; }

    /// <summary>Causes all opacity effects from the layer tree to be ignored.</summary>
    /// <remarks>
    /// Flutter's <c>debugDisableOpacityLayers</c>. The optimization that skips painting the child
    /// entirely when the opacity is 0 still remains.
    /// </remarks>
    public static bool DisableOpacityLayers { get; set; }

    /// <summary>Paints a diagram showing the given area as padding.</summary>
    /// <remarks>
    /// Flutter's <c>debugPaintPadding</c>, used by <see cref="RenderPadding.DebugPaintSize"/> when
    /// <see cref="PaintSizeEnabled"/> is set. When <paramref name="innerRect"/> is null the whole
    /// <paramref name="outerRect"/> is drawn in a grayish color representing spacing; otherwise the
    /// padding region around it is drawn in a tealish color with a solid outline around the inner
    /// region.
    /// </remarks>
    public static void PaintPadding(
        PaintingContext context,
        Rect outerRect,
        Rect? innerRect,
        double outlineWidth = 2.0)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!Constants.KDebugMode)
        {
            return;
        }

        if (innerRect is { } inner && !IsEmptyRect(inner))
        {
            DrawDoubleRect(context, outerRect, inner, Color.FromUInt32(0x900090FF));
            Rect outline = Intersect(Inflate(inner, outlineWidth), outerRect);
            DrawDoubleRect(context, outline, inner, Color.FromUInt32(0xFF0090FF));
        }
        else
        {
            context.Canvas.DrawRectangle(new SolidColorBrush(Color.FromUInt32(0x90909090)), null, outerRect);
        }
    }

    /// <summary>
    /// Returns true if none of the rendering library debug variables have been changed.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>debugAssertAllRenderVarsUnset</c>. The
    /// <paramref name="checkIntrinsicSizesOverride"/> argument overrides the expected value of
    /// <see cref="CheckIntrinsicSizes"/>, because test harnesses sometimes set it themselves.
    /// </remarks>
    public static bool AssertAllRenderVarsUnset(string reason, bool checkIntrinsicSizesOverride = false)
    {
        if (PaintSizeEnabled
            || PaintBaselinesEnabled
            || PaintLayerBordersEnabled
            || PaintTextLayoutBoxes
            || PaintPointersEnabled
            || RepaintRainbowEnabled
            || RepaintTextRainbowEnabled
            || CurrentRepaintColor != KDebugDefaultRepaintColor
            || PrintMarkNeedsLayoutStacks
            || PrintMarkNeedsPaintStacks
            || PrintLayouts
            || CheckIntrinsicSizes != checkIntrinsicSizesOverride
            || ProfileLayoutsEnabled
            || ProfilePaintsEnabled
            || OnProfilePaint is not null
            || DisableClipLayers
            || DisablePhysicalShapeLayers
            || DisableOpacityLayers)
        {
            throw new FlutterError(reason);
        }

        return true;
    }

    /// <summary>
    /// Returns true if the given <see cref="Axis"/> is bounded within the given
    /// <see cref="BoxConstraints"/> in both the main and cross axis, throwing otherwise.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>debugCheckHasBoundedAxis</c>, used by viewports during layout because bounded
    /// constraints are required in order to lay their children out.
    /// </remarks>
    public static bool CheckHasBoundedAxis(Axis axis, BoxConstraints constraints)
    {
        if (constraints.HasBoundedHeight && constraints.HasBoundedWidth)
        {
            return true;
        }

        switch (axis)
        {
            case Axis.Vertical:
                if (!constraints.HasBoundedHeight)
                {
                    throw new FlutterError([
                        new ErrorSummary("Vertical viewport was given unbounded height."),
                        new ErrorDescription(
                            "Viewports expand in the scrolling direction to fill their container. "
                            + "In this case, a vertical viewport was given an unlimited amount of "
                            + "vertical space in which to expand. This situation typically happens "
                            + "when a scrollable widget is nested inside another scrollable widget."),
                        new ErrorHint(
                            "If this widget is always nested in a scrollable widget there "
                            + "is no need to use a viewport because there will always be enough "
                            + "vertical space for the children. In this case, consider using a "
                            + "Column or Wrap instead. Otherwise, consider using a "
                            + "CustomScrollView to concatenate arbitrary slivers into a "
                            + "single scrollable."),
                    ]);
                }

                if (!constraints.HasBoundedWidth)
                {
                    throw new FlutterError(
                        "Vertical viewport was given unbounded width.\n"
                        + "Viewports expand in the cross axis to fill their container and "
                        + "constrain their children to match their extent in the cross axis. "
                        + "In this case, a vertical viewport was given an unlimited amount of "
                        + "horizontal space in which to expand.");
                }

                break;
            case Axis.Horizontal:
                if (!constraints.HasBoundedWidth)
                {
                    throw new FlutterError([
                        new ErrorSummary("Horizontal viewport was given unbounded width."),
                        new ErrorDescription(
                            "Viewports expand in the scrolling direction to fill their container. "
                            + "In this case, a horizontal viewport was given an unlimited amount of "
                            + "horizontal space in which to expand. This situation typically happens "
                            + "when a scrollable widget is nested inside another scrollable widget."),
                        new ErrorHint(
                            "If this widget is always nested in a scrollable widget there "
                            + "is no need to use a viewport because there will always be enough "
                            + "horizontal space for the children. In this case, consider using a "
                            + "Row or Wrap instead. Otherwise, consider using a "
                            + "CustomScrollView to concatenate arbitrary slivers into a "
                            + "single scrollable."),
                    ]);
                }

                if (!constraints.HasBoundedHeight)
                {
                    throw new FlutterError(
                        "Horizontal viewport was given unbounded height.\n"
                        + "Viewports expand in the cross axis to fill their container and "
                        + "constrain their children to match their extent in the cross axis. "
                        + "In this case, a horizontal viewport was given an unlimited amount of "
                        + "vertical space in which to expand.");
                }

                break;
        }

        return true;
    }

    /// <summary>Resets every variable to its default. Test-only.</summary>
    /// <remarks>
    /// C#-only: Dart's test harness assigns the library-level variables back one by one, which is
    /// not expressible over properties on a static class without repeating the list at every call
    /// site.
    /// </remarks>
    internal static void ResetForTesting()
    {
        PaintSizeEnabled = false;
        PaintBaselinesEnabled = false;
        PaintTextLayoutBoxes = false;
        PaintLayerBordersEnabled = false;
        PaintPointersEnabled = false;
        RepaintRainbowEnabled = false;
        RepaintTextRainbowEnabled = false;
        CurrentRepaintColor = KDebugDefaultRepaintColor;
        PrintMarkNeedsLayoutStacks = false;
        PrintMarkNeedsPaintStacks = false;
        PrintLayouts = false;
        CheckIntrinsicSizes = false;
        ProfileLayoutsEnabled = false;
        ProfilePaintsEnabled = false;
        EnhanceLayoutTimelineArguments = false;
        EnhancePaintTimelineArguments = false;
        OnProfilePaint = null;
        DisableClipLayers = false;
        DisablePhysicalShapeLayers = false;
        DisableOpacityLayers = false;
    }

    /// <remarks>Flutter's <c>_debugDrawDoubleRect</c>.</remarks>
    private static void DrawDoubleRect(PaintingContext context, Rect outerRect, Rect innerRect, Color color)
    {
        context.Canvas.DrawPath(BuildDoubleRectPath(outerRect, innerRect), new SolidColorBrush(color), pen: null);
    }

    /// <summary>The even-odd ring between two nested rectangles.</summary>
    /// <remarks>
    /// The path half of Flutter's <c>_debugDrawDoubleRect</c>, split out from the drawing so the
    /// geometry can be asserted without a drawing backend.
    /// </remarks>
    internal static UI.Path BuildDoubleRectPath(Rect outerRect, Rect innerRect)
    {
        var path = new UI.Path { FillType = PathFillType.EvenOdd };
        path.AddRect(outerRect);
        path.AddRect(innerRect);
        return path;
    }

    /// <summary>Rotates <see cref="CurrentRepaintColor"/> by the per-frame two degrees.</summary>
    /// <remarks>
    /// The assert block at the end of Flutter's <c>RenderView.compositeFrame</c>. Plumix's
    /// <c>PipelineOwner</c> owns the view and composites the frame, so it calls this from
    /// <c>PipelineOwner.CompositeFrame</c>.
    /// </remarks>
    internal static void AdvanceRepaintColorForFrame()
    {
        if (!Constants.KDebugMode || (!RepaintRainbowEnabled && !RepaintTextRainbowEnabled))
        {
            return;
        }

        CurrentRepaintColor = CurrentRepaintColor.WithHue((CurrentRepaintColor.Hue + 2.0) % 360.0);
    }

    private static bool IsEmptyRect(Rect rect) => rect.Width <= 0.0 || rect.Height <= 0.0;

    /// <remarks>Dart's <c>Rect.inflate</c>.</remarks>
    private static Rect Inflate(Rect rect, double delta)
    {
        return new Rect(
            rect.X - delta,
            rect.Y - delta,
            rect.Width + (delta * 2.0),
            rect.Height + (delta * 2.0));
    }

    /// <remarks>Dart's <c>Rect.intersect</c>, which does not normalize an empty result.</remarks>
    private static Rect Intersect(Rect a, Rect b)
    {
        double left = Math.Max(a.X, b.X);
        double top = Math.Max(a.Y, b.Y);
        double right = Math.Min(a.X + a.Width, b.X + b.Width);
        double bottom = Math.Min(a.Y + a.Height, b.Y + b.Height);
        return new Rect(left, top, Math.Max(0.0, right - left), Math.Max(0.0, bottom - top));
    }
}
