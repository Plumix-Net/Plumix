using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Canvas = Plumix.UI.Canvas;
using Path = Plumix.UI.Path;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/widget_inspector.dart

namespace Plumix.Widgets;

/// <summary>
/// A layer that mimics the behavior of another layer: the screenshot tree can include a layer that
/// is already attached to the regular tree without reparenting it.
/// </summary>
/// <remarks>Flutter's private <c>_ProxyLayer</c>.</remarks>
internal sealed class InspectorProxyLayer : Layer
{
    private readonly Layer _layer;

    public InspectorProxyLayer(Layer layer)
    {
        _layer = layer;
    }

    protected internal override void AddToScene(SceneBuilder builder)
    {
        _layer.AddToScene(builder);
    }

    protected internal override bool FindAnnotations<S>(
        AnnotationResult<S> result,
        Point localPosition,
        bool onlyFirst)
    {
        return _layer.FindAnnotations(result, localPosition, onlyFirst);
    }
}

/// <summary>A canvas that forwards every call to both a main canvas and a screenshot canvas.</summary>
/// <remarks>
/// Flutter's private <c>_MulticastCanvas</c>, which <c>implements Canvas</c>. As in Dart,
/// <see cref="RestoreToCount"/>, <see cref="ClipRSuperellipse"/> and <c>DrawRSuperellipse</c> are not
/// forwarded: Dart's class leaves them to <c>noSuchMethod</c>, which throws.
/// </remarks>
internal sealed class InspectorMulticastCanvas : Canvas
{
    private readonly Canvas _main;
    private readonly Canvas _screenshot;

    public InspectorMulticastCanvas(Canvas main, Canvas screenshot)
    {
        _main = main;
        _screenshot = screenshot;
    }

    public override void ClipPath(Path path, bool doAntiAlias = true)
    {
        _main.ClipPath(path, doAntiAlias);
        _screenshot.ClipPath(path, doAntiAlias);
    }

    public override void ClipRRect(RRect rrect, bool doAntiAlias = true)
    {
        _main.ClipRRect(rrect, doAntiAlias);
        _screenshot.ClipRRect(rrect, doAntiAlias);
    }

    public override void ClipRect(Rect rect, bool doAntiAlias = true)
    {
        _main.ClipRect(rect, doAntiAlias);
        _screenshot.ClipRect(rect, doAntiAlias);
    }

    public override void ClipGeometry(Geometry geometry, bool doAntiAlias = true, Point geometryOffset = default)
    {
        _main.ClipGeometry(geometry, doAntiAlias, geometryOffset);
        _screenshot.ClipGeometry(geometry, doAntiAlias, geometryOffset);
    }

    public override void ClipRSuperellipse(RSuperellipse rsuperellipse, bool doAntiAlias = true)
    {
        throw NoSuchMethod(nameof(ClipRSuperellipse));
    }

    public override void DrawArc(IPen pen, Rect rect, double startAngleRadians, double sweepAngleRadians)
    {
        _main.DrawArc(pen, rect, startAngleRadians, sweepAngleRadians);
        _screenshot.DrawArc(pen, rect, startAngleRadians, sweepAngleRadians);
    }

    public override void DrawCircle(Point center, double radius, Paint paint)
    {
        _main.DrawCircle(center, radius, paint);
        _screenshot.DrawCircle(center, radius, paint);
    }

    public override void DrawCircle(IBrush? brush, IPen? pen, Point center, double radius)
    {
        _main.DrawCircle(brush, pen, center, radius);
        _screenshot.DrawCircle(brush, pen, center, radius);
    }

    public override void DrawDRRect(RRect outer, RRect inner, Paint paint)
    {
        _main.DrawDRRect(outer, inner, paint);
        _screenshot.DrawDRRect(outer, inner, paint);
    }

    public override void DrawDRRect(RRect outer, RRect inner, IBrush brush)
    {
        _main.DrawDRRect(outer, inner, brush);
        _screenshot.DrawDRRect(outer, inner, brush);
    }

    public override void DrawImage(
        IImage image,
        Rect sourceRect,
        Rect destinationRect,
        double opacity = 1.0,
        Rect? clipRect = null,
        BorderRadius? clipRadius = null,
        bool flipHorizontally = false,
        double? horizontalFlipAxisX = null,
        Rect? ovalClipRect = null,
        FilterQuality filterQuality = FilterQuality.Medium,
        bool isAntiAlias = false,
        BitmapBlendingMode blendMode = BitmapBlendingMode.SourceOver)
    {
        _main.DrawImage(
            image,
            sourceRect,
            destinationRect,
            opacity,
            clipRect,
            clipRadius,
            flipHorizontally,
            horizontalFlipAxisX,
            ovalClipRect,
            filterQuality,
            isAntiAlias,
            blendMode);
        _screenshot.DrawImage(
            image,
            sourceRect,
            destinationRect,
            opacity,
            clipRect,
            clipRadius,
            flipHorizontally,
            horizontalFlipAxisX,
            ovalClipRect,
            filterQuality,
            isAntiAlias,
            blendMode);
    }

    public override void DrawLine(Point startPoint, Point endPoint, Paint paint)
    {
        _main.DrawLine(startPoint, endPoint, paint);
        _screenshot.DrawLine(startPoint, endPoint, paint);
    }

    public override void DrawLine(IPen pen, Point startPoint, Point endPoint)
    {
        _main.DrawLine(pen, startPoint, endPoint);
        _screenshot.DrawLine(pen, startPoint, endPoint);
    }

    public override void DrawOval(Rect oval, Paint paint)
    {
        _main.DrawOval(oval, paint);
        _screenshot.DrawOval(oval, paint);
    }

    public override void DrawOval(Rect oval, IBrush? brush, IPen? pen)
    {
        _main.DrawOval(oval, brush, pen);
        _screenshot.DrawOval(oval, brush, pen);
    }

    public override void DrawPaint(Paint paint)
    {
        _main.DrawPaint(paint);
        _screenshot.DrawPaint(paint);
    }

    public override void DrawPaint(IBrush brush)
    {
        _main.DrawPaint(brush);
        _screenshot.DrawPaint(brush);
    }

    public override void DrawParagraph(Paragraph paragraph, Point offset)
    {
        _main.DrawParagraph(paragraph, offset);
        _screenshot.DrawParagraph(paragraph, offset);
    }

    public override void DrawPath(Path path, Paint paint)
    {
        _main.DrawPath(path, paint);
        _screenshot.DrawPath(path, paint);
    }

    public override void DrawPath(Path path, IBrush? brush, IPen? pen)
    {
        _main.DrawPath(path, brush, pen);
        _screenshot.DrawPath(path, brush, pen);
    }

    public override void DrawPolygon(IBrush? brush, IPen? pen, IReadOnlyList<Point> points)
    {
        _main.DrawPolygon(brush, pen, points);
        _screenshot.DrawPolygon(brush, pen, points);
    }

    public override void DrawGeometry(IBrush? brush, IPen? pen, Geometry geometry, Point geometryOffset = default)
    {
        _main.DrawGeometry(brush, pen, geometry, geometryOffset);
        _screenshot.DrawGeometry(brush, pen, geometry, geometryOffset);
    }

    public override void DrawRRect(RRect rrect, Paint paint)
    {
        _main.DrawRRect(rrect, paint);
        _screenshot.DrawRRect(rrect, paint);
    }

    public override void DrawRRect(RRect rrect, IBrush? brush, IPen? pen)
    {
        _main.DrawRRect(rrect, brush, pen);
        _screenshot.DrawRRect(rrect, brush, pen);
    }

    public override void DrawRSuperellipse(RSuperellipse rsuperellipse, Paint paint)
    {
        throw NoSuchMethod("DrawRSuperellipse");
    }

    public override void DrawRSuperellipse(RSuperellipse rsuperellipse, IBrush? brush, IPen? pen)
    {
        throw NoSuchMethod("DrawRSuperellipse");
    }

    public override void DrawRSuperellipseShadow(RSuperellipse rsuperellipse, Plumix.Rendering.BoxShadow shadow)
    {
        throw NoSuchMethod("DrawRSuperellipse");
    }

    public override void DrawRSuperellipseBlur(RSuperellipse rsuperellipse, Color color, double blurSigma)
    {
        throw NoSuchMethod("DrawRSuperellipse");
    }

    public override void DrawRect(Rect rect, Paint paint)
    {
        _main.DrawRect(rect, paint);
        _screenshot.DrawRect(rect, paint);
    }

    public override void DrawRectangle(
        IBrush? brush,
        IPen? pen,
        Rect rect,
        double radiusX = 0,
        double radiusY = 0,
        BoxShadows boxShadows = default,
        bool isAntiAlias = true)
    {
        _main.DrawRectangle(brush, pen, rect, radiusX, radiusY, boxShadows, isAntiAlias);
        _screenshot.DrawRectangle(brush, pen, rect, radiusX, radiusY, boxShadows, isAntiAlias);
    }

    public override void DrawRectangle(
        IBrush? brush,
        IPen? pen,
        Rect rect,
        BorderRadius borderRadius,
        BoxShadows boxShadows = default)
    {
        _main.DrawRectangle(brush, pen, rect, borderRadius, boxShadows);
        _screenshot.DrawRectangle(brush, pen, rect, borderRadius, boxShadows);
    }

    public override void DrawShadow(
        Path path,
        Color color,
        double elevation,
        bool transparentOccluder,
        Point geometryOffset = default)
    {
        _main.DrawShadow(path, color, elevation, transparentOccluder, geometryOffset);
        _screenshot.DrawShadow(path, color, elevation, transparentOccluder, geometryOffset);
    }

    public override void DrawShadow(
        Geometry geometry,
        Color color,
        double elevation,
        bool transparentOccluder,
        Point geometryOffset = default)
    {
        _main.DrawShadow(geometry, color, elevation, transparentOccluder, geometryOffset);
        _screenshot.DrawShadow(geometry, color, elevation, transparentOccluder, geometryOffset);
    }

    public override void Restore()
    {
        _main.Restore();
        _screenshot.Restore();
    }

    public override void RestoreToCount(int count)
    {
        throw NoSuchMethod(nameof(RestoreToCount));
    }

    public override void Rotate(double radians)
    {
        _main.Rotate(radians);
        _screenshot.Rotate(radians);
    }

    public override void Save()
    {
        _main.Save();
        _screenshot.Save();
    }

    public override void SaveLayer(Rect bounds)
    {
        _main.SaveLayer(bounds);
        _screenshot.SaveLayer(bounds);
    }

    public override void Scale(double sx, double? sy = null)
    {
        _main.Scale(sx, sy);
        _screenshot.Scale(sx, sy);
    }

    public override void Transform(Matrix4 matrix)
    {
        _main.Transform(matrix);
        _screenshot.Transform(matrix);
    }

    public override void Translate(double dx, double dy)
    {
        _main.Translate(dx, dy);
        _screenshot.Translate(dx, dy);
    }

    internal override void PushOpacityMask(IBrush mask, Rect bounds)
    {
        _main.PushOpacityMask(mask, bounds);
        _screenshot.PushOpacityMask(mask, bounds);
    }

    internal override void AddDrawCommand(Action<DrawingContext> draw)
    {
        _main.AddDrawCommand(draw);
        _screenshot.AddDrawCommand(draw);
    }

    // The main canvas is guaranteed to be consistent with the canvas expected by the normal paint
    // pipeline so any calls to getSaveCount() should be forwarded to the main canvas.
    public override int GetSaveCount()
    {
        return _main.GetSaveCount();
    }

    private static NotSupportedException NoSuchMethod(string member) =>
        new($"NoSuchMethodError: Class '_MulticastCanvas' has no instance method '{member}'.");
}

/// <summary>A container layer used by the inspector screenshot: it adds only its children to a scene.</summary>
/// <remarks>Flutter's private <c>_ScreenshotContainerLayer</c>.</remarks>
internal sealed class InspectorScreenshotContainerLayer : OffsetLayer
{
    protected internal override void AddToScene(SceneBuilder builder)
    {
        AddChildrenToScene(builder);
    }
}

/// <summary>Data shared between nested screenshot painting contexts.</summary>
/// <remarks>Flutter's private <c>_ScreenshotData</c>.</remarks>
internal sealed class InspectorScreenshotData
{
    public InspectorScreenshotData(RenderObject target)
    {
        Target = target;
        ContainerLayer = new InspectorScreenshotContainerLayer();
        Debug.Assert(FoundationDebug.DebugMaybeDispatchCreated("widgets", "_ScreenshotData", this));
    }

    /// <summary>Target to take a screenshot of.</summary>
    public RenderObject Target { get; }

    /// <summary>Root of the layer tree containing the screenshot.</summary>
    public OffsetLayer ContainerLayer { get; }

    /// <summary>Whether the screenshot target has already been found in the render tree.</summary>
    public bool FoundTarget { get; set; }

    /// <summary>Whether paint operations should record to the screenshot.</summary>
    /// <remarks>At least one of this and <see cref="IncludeInRegularContext"/> must be true.</remarks>
    public bool IncludeInScreenshot { get; set; }

    /// <summary>Whether paint operations should record to the regular context.</summary>
    /// <remarks>
    /// This should only be set to false before paint operations that should only apply to the
    /// screenshot such as rendering debug information about the <see cref="Target"/>.
    /// </remarks>
    public bool IncludeInRegularContext { get; set; } = true;

    /// <summary>Offset of the screenshot corresponding to the offset the target was given as part of
    /// the regular paint.</summary>
    public Point ScreenshotOffset
    {
        get
        {
            Debug.Assert(FoundTarget);
            return ContainerLayer.Offset;
        }

        set => ContainerLayer.Offset = value;
    }

    /// <summary>Releases allocated resources.</summary>
    public void Dispose()
    {
        Debug.Assert(FoundationDebug.DebugMaybeDispatchDisposed(this));
        ContainerLayer.Dispose();
    }
}

/// <summary>
/// A place to paint to build screenshots of <see cref="RenderObject"/>s: everything the target paints
/// goes both to the regular layer tree and to a separate screenshot layer tree.
/// </summary>
/// <remarks>
/// Flutter's private <c>_ScreenshotPaintingContext</c>. Requires that a render object has a repaint
/// boundary above it, which is always the case as the root render object is one.
/// </remarks>
internal sealed class InspectorScreenshotPaintingContext : PaintingContext
{
    private readonly InspectorScreenshotData _data;

    // Recording state
    private PictureLayer? _screenshotCurrentLayer;
    private PictureRecorder? _screenshotRecorder;
    private Canvas? _screenshotCanvas;
    private InspectorMulticastCanvas? _multicastCanvas;

    public InspectorScreenshotPaintingContext(
        ContainerLayer containerLayer,
        Rect estimatedBounds,
        InspectorScreenshotData screenshotData)
        : base(containerLayer, estimatedBounds)
    {
        _data = screenshotData;
    }

    public override Canvas Canvas
    {
        get
        {
            if (_data.IncludeInScreenshot)
            {
                if (_screenshotCanvas is null)
                {
                    StartRecordingScreenshot();
                }

                Debug.Assert(_screenshotCanvas is not null);
                return _data.IncludeInRegularContext ? _multicastCanvas! : _screenshotCanvas!;
            }

            Debug.Assert(_data.IncludeInRegularContext);
            return base.Canvas;
        }
    }

    private bool IsScreenshotRecording
    {
        get
        {
            bool hasScreenshotCanvas = _screenshotCanvas is not null;
            Debug.Assert(hasScreenshotCanvas
                ? _screenshotCurrentLayer is not null && _screenshotRecorder is not null
                : _screenshotCurrentLayer is null && _screenshotRecorder is null);
            return hasScreenshotCanvas;
        }
    }

    private void StartRecordingScreenshot()
    {
        Debug.Assert(_data.IncludeInScreenshot);
        Debug.Assert(!IsScreenshotRecording);
        _screenshotCurrentLayer = new PictureLayer(EstimatedBounds);
        _screenshotRecorder = new PictureRecorder();
        _screenshotCanvas = new Canvas(_screenshotRecorder);
        _data.ContainerLayer.Append(_screenshotCurrentLayer);
        if (_data.IncludeInRegularContext)
        {
            _multicastCanvas = new InspectorMulticastCanvas(main: base.Canvas, screenshot: _screenshotCanvas);
        }
        else
        {
            _multicastCanvas = null;
        }
    }

    protected override void StopRecordingIfNeeded()
    {
        base.StopRecordingIfNeeded();
        StopRecordingScreenshotIfNeeded();
    }

    private void StopRecordingScreenshotIfNeeded()
    {
        if (!IsScreenshotRecording)
        {
            return;
        }

        // There is no need to ever draw repaint rainbows as part of the screenshot.
        _screenshotCurrentLayer!.Picture = _screenshotRecorder!.EndRecording();
        _screenshotCurrentLayer = null;
        _screenshotRecorder = null;
        _multicastCanvas = null;
        _screenshotCanvas = null;
    }

    protected override void AppendLayer(Layer layer)
    {
        if (_data.IncludeInRegularContext)
        {
            base.AppendLayer(layer);
            if (_data.IncludeInScreenshot)
            {
                Debug.Assert(!IsScreenshotRecording);
                // We must use a proxy layer here as the layer is already attached to
                // the regular layer tree.
                _data.ContainerLayer.Append(new InspectorProxyLayer(layer));
            }
        }
        else
        {
            // Only record to the screenshot.
            Debug.Assert(!IsScreenshotRecording);
            Debug.Assert(_data.IncludeInScreenshot);
            layer.Remove();
            _data.ContainerLayer.Append(layer);
        }
    }

    protected override PaintingContext CreateChildContext(ContainerLayer childLayer, Rect bounds)
    {
        if (_data.FoundTarget)
        {
            // We have already found the screenshotTarget in the layer tree
            // so we can optimize and use a standard PaintingContext.
            return base.CreateChildContext(childLayer, bounds);
        }

        return new InspectorScreenshotPaintingContext(childLayer, bounds, _data);
    }

    public override void PaintChild(RenderObject child, Point offset)
    {
        bool isScreenshotTarget = ReferenceEquals(child, _data.Target);
        if (isScreenshotTarget)
        {
            Debug.Assert(!_data.IncludeInScreenshot);
            Debug.Assert(!_data.FoundTarget);
            _data.FoundTarget = true;
            _data.ScreenshotOffset = offset;
            _data.IncludeInScreenshot = true;
        }

        base.PaintChild(child, offset);
        if (isScreenshotTarget)
        {
            StopRecordingScreenshotIfNeeded();
            _data.IncludeInScreenshot = false;
        }
    }

    /// <summary>Captures an image of the current state of <paramref name="renderObject"/> and its
    /// children.</summary>
    /// <remarks>
    /// Flutter's <c>_ScreenshotPaintingContext.toImage</c>. The returned image is cropped to
    /// <paramref name="renderBounds"/>, in the coordinate space of <paramref name="renderObject"/>.
    /// With <paramref name="debugPaint"/>, the debug paint of <paramref name="renderObject"/> is drawn
    /// into the screenshot only.
    /// </remarks>
    public static async Task<Bitmap> ToImage(
        RenderObject renderObject,
        Rect renderBounds,
        double pixelRatio = 1.0,
        bool debugPaint = false)
    {
        RenderObject repaintBoundary = renderObject;
        while (!repaintBoundary.IsRepaintBoundary)
        {
            repaintBoundary = repaintBoundary.Parent!;
        }

        var data = new InspectorScreenshotData(renderObject);
        var context = new InspectorScreenshotPaintingContext(
            (ContainerLayer)repaintBoundary.DebugLayer!,
            repaintBoundary.PaintBounds,
            data);

        if (ReferenceEquals(renderObject, repaintBoundary))
        {
            // Painting the existing repaint boundary to the screenshot is sufficient.
            // We don't just take a direct screenshot of the repaint boundary as we
            // want to capture debugPaint information as well.
            data.ContainerLayer.Append(new InspectorProxyLayer(repaintBoundary.DebugLayer!));
            data.FoundTarget = true;
            var offsetLayer = (OffsetLayer)repaintBoundary.DebugLayer!;
            data.ScreenshotOffset = offsetLayer.Offset;
        }
        else
        {
            // Repaint everything under the repaint boundary.
            // We call debugInstrumentRepaintCompositedChild instead of paintChild as
            // we need to force everything under the repaint boundary to repaint.
            PaintingContext.DebugInstrumentRepaintCompositedChild(repaintBoundary, customContext: context);
        }

        // The check that debugPaintSizeEnabled is false exists to ensure we only
        // call debugPaint when it wasn't already called.
        if (debugPaint && !RenderingDebug.PaintSizeEnabled)
        {
            data.IncludeInRegularContext = false;
            // Existing recording may be to a canvas that draws to both the normal and
            // screenshot canvases.
            context.StopRecordingIfNeeded();
            Debug.Assert(data.FoundTarget);
            data.IncludeInScreenshot = true;

            RenderingDebug.PaintSizeEnabled = true;
            try
            {
                renderObject.InvokeDebugPaint(context, data.ScreenshotOffset);
            }
            finally
            {
                RenderingDebug.PaintSizeEnabled = false;
                context.StopRecordingIfNeeded();
            }
        }

        // We must build the regular scene before we can build the screenshot
        // scene as building the screenshot scene assumes addToScene has already
        // been called successfully for all layers in the regular scene.
        ((ContainerLayer)repaintBoundary.DebugLayer!).BuildScene(new SceneBuilder());

        Bitmap image;
        try
        {
            image = await data.ContainerLayer.ToImage(renderBounds, pixelRatio: pixelRatio).ConfigureAwait(true);
        }
        finally
        {
            data.Dispose();
        }

        return image;
    }
}

/// <summary>A node with a path to it from a root of the diagnostics tree.</summary>
/// <remarks>Flutter's private <c>_DiagnosticsPathNode</c>.</remarks>
internal sealed class InspectorDiagnosticsPathNode
{
    public InspectorDiagnosticsPathNode(DiagnosticsNode node, List<DiagnosticsNode> children, int? childIndex = null)
    {
        Node = node;
        Children = children;
        ChildIndex = childIndex;
    }

    /// <summary>Node at the point in the path this object describes.</summary>
    public DiagnosticsNode Node { get; }

    /// <summary>Children of the node being described, cached so the identical child nodes are
    /// reused.</summary>
    public List<DiagnosticsNode> Children { get; }

    /// <summary>Index of the child the path continues on, or null when the path ends here.</summary>
    public int? ChildIndex { get; }
}

public abstract partial class WidgetInspectorService
{
    /// <remarks>Flutter's private <c>_calculateSubtreeBoundsHelper</c>.</remarks>
    private static Rect CalculateSubtreeBoundsHelper(RenderObject @object, Matrix4 transform)
    {
        Rect bounds = MatrixUtils.TransformRect(transform, @object.SemanticBoundsForSemantics);

        @object.VisitChildren(child =>
        {
            Matrix4 childTransform = transform.Clone();
            @object.ApplyPaintTransform(child, childTransform);
            Rect childBounds = CalculateSubtreeBoundsHelper(child, childTransform);
            Rect? paintClip = @object.InvokeDescribeApproximatePaintClip(child);
            if (paintClip is { } clip)
            {
                Rect transformedPaintClip = MatrixUtils.TransformRect(transform, clip);
                childBounds = Intersect(childBounds, transformedPaintClip);
            }

            if (IsFinite(childBounds) && !IsEmpty(childBounds))
            {
                bounds = IsEmpty(bounds) ? childBounds : ExpandToInclude(bounds, childBounds);
            }
        });

        return bounds;
    }

    /// <summary>Calculate bounds for a render object and all of its descendants.</summary>
    /// <remarks>Flutter's private <c>_calculateSubtreeBounds</c>.</remarks>
    private static Rect CalculateSubtreeBounds(RenderObject @object)
    {
        return CalculateSubtreeBoundsHelper(@object, Matrix4.Identity());
    }

    /// <summary>Dart's <c>Rect.intersect</c>, which keeps a negative-size result.</summary>
    private static Rect Intersect(Rect a, Rect b)
    {
        double left = Math.Max(a.Left, b.Left);
        double top = Math.Max(a.Top, b.Top);
        double right = Math.Min(a.Right, b.Right);
        double bottom = Math.Min(a.Bottom, b.Bottom);
        return right <= left || bottom <= top ? default : new Rect(left, top, right - left, bottom - top);
    }

    private static Rect ExpandToInclude(Rect a, Rect b) => new(
        new Point(Math.Min(a.Left, b.Left), Math.Min(a.Top, b.Top)),
        new Point(Math.Max(a.Right, b.Right), Math.Max(a.Bottom, b.Bottom)));

    private static bool IsEmpty(Rect rect) => rect.Width <= 0 || rect.Height <= 0;

    private static bool IsFinite(Rect rect) =>
        double.IsFinite(rect.Left) && double.IsFinite(rect.Top)
        && double.IsFinite(rect.Right) && double.IsFinite(rect.Bottom);

    /// <remarks>Flutter's private <c>_followDiagnosticableChain</c>.</remarks>
    private static List<InspectorDiagnosticsPathNode> FollowDiagnosticableChain(List<IDiagnosticable> chain)
    {
        var path = new List<InspectorDiagnosticsPathNode>();
        if (chain.Count == 0)
        {
            return path;
        }

        DiagnosticsNode diagnostic = chain[0].ToDiagnosticsNode();
        for (int i = 1; i < chain.Count; i += 1)
        {
            IDiagnosticable target = chain[i];
            bool foundMatch = false;
            List<DiagnosticsNode> children = diagnostic.GetChildren();
            for (int j = 0; j < children.Count; j += 1)
            {
                DiagnosticsNode child = children[j];
                if (Equals(child.Value, target))
                {
                    foundMatch = true;
                    path.Add(new InspectorDiagnosticsPathNode(diagnostic, children, childIndex: j));
                    diagnostic = child;
                    break;
                }
            }

            Debug.Assert(foundMatch);
        }

        path.Add(new InspectorDiagnosticsPathNode(diagnostic, diagnostic.GetChildren()));
        return path;
    }
}
