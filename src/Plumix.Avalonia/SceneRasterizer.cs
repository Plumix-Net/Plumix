using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;

namespace Plumix.UI;

// C#-only host infrastructure: the engine's rasterizer over Avalonia's drawing context. It paints a
// scene's retained layer tree (src/Plumix/UI/FlowLayers.cs, framework data) through FlowLayerPainter into
// a host frame or an offscreen bitmap, capturing the scene prefixes backdrop filters and magnifier lenses
// sample from. AvaloniaRenderBackend exposes it to the framework (Scene.toImage). No Dart source maps to
// this file.

/// <summary>
/// A layer the host paints itself, outside the framework's layer types; the rasterizer tests use it to
/// probe a pass.
/// </summary>
internal abstract class HostFlowLayer : FlowLayer
{
    /// <summary>Paints this layer into <paramref name="context"/>.</summary>
    internal abstract void Paint(FlowPaintContext context);
}

/// <summary>A backdrop input: an image of the scene prefix painted before a backdrop filter.</summary>
internal sealed class BackdropCapture : IDisposable
{
    private readonly bool _ownsImage;

    public BackdropCapture(IImage image, Rect bounds, bool ownsImage = false)
    {
        Image = image ?? throw new ArgumentNullException(nameof(image));
        Bounds = bounds;
        _ownsImage = ownsImage;
    }

    public IImage Image { get; }

    public Rect Bounds { get; }

    public void Dispose()
    {
        if (_ownsImage && Image is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

/// <summary>The state one rasterization pass shares across every drawing context it paints into.</summary>
internal sealed class FlowRasterState(Size targetSize, CompositorContext compositor, TextureRegistry? textures)
{
    /// <summary>The size of the surface, in the drawing context's own (target) units.</summary>
    public Size TargetSize { get; } = targetSize;

    public CompositorContext Compositor { get; } = compositor;

    public TextureRegistry? Textures { get; } = textures;

    /// <summary>The backdrop filter a prefix capture stops at; null for a full paint.</summary>
    public BackdropFilterFlowLayer? StopAt { get; init; }

    /// <summary>Whether the prefix capture reached <see cref="StopAt"/>.</summary>
    public bool Stopped { get; set; }

    /// <summary>Whether this pass captures the magnifier input, which paints no lens.</summary>
    public bool CapturingMagnifier { get; init; }

    /// <summary>The captured backdrop of every backdrop filter, by layer.</summary>
    public Dictionary<BackdropFilterFlowLayer, BackdropCapture> Backdrops { get; init; } =
        new(ReferenceEqualityComparer.Instance);

    /// <summary>The captured scene a magnifier lens draws from.</summary>
    public BackdropCapture? MagnifierBackdrop { get; set; }

    /// <summary>The raster outputs the drawing context draws from, kept alive until the next frame.</summary>
    public List<IDisposable> Resources { get; init; } = [];
}

/// <summary>
/// One drawing context of a rasterization pass: the engine's <c>PaintContext</c> (its canvas and state
/// stack). <see cref="Transform"/> maps the current local coordinates to the target's.
/// </summary>
internal sealed class FlowPaintContext(DrawingContext canvas, Matrix transform, FlowRasterState state)
{
    public DrawingContext Canvas { get; } = canvas;

    public Matrix Transform { get; set; } = transform;

    public FlowRasterState State { get; } = state;

    /// <summary>A context drawing into <paramref name="canvas"/>, an offscreen of this one.</summary>
    public FlowPaintContext WithCanvas(DrawingContext canvas, Matrix transform) => new(canvas, transform, State);

    /// <summary>Keeps a raster output alive until the next frame of the same surface.</summary>
    public void Track(IDisposable? resource)
    {
        if (resource != null)
        {
            State.Resources.Add(resource);
        }
    }
}

/// <summary>Paints a scene's layer tree: the engine's rasterizer, over Avalonia's drawing context.</summary>
internal static class SceneRasterizer
{
    private static readonly Offset Dpi = new(96.0, 96.0);

    [ThreadStatic]
    private static int t_captureDepth;

    /// <summary>Whether the current thread is painting a backdrop or magnifier capture.</summary>
    internal static bool Capturing => t_captureDepth > 0;

    /// <summary>
    /// Paints <paramref name="root"/> into <paramref name="context"/> under <paramref name="rootTransform"/>,
    /// which maps the scene's (physical) pixels to the context's units.
    /// </summary>
    /// <returns>The raster outputs the context draws from; the caller disposes them after the next frame.</returns>
    public static List<IDisposable> Draw(
        DrawingContext context,
        ContainerFlowLayer root,
        Size targetSize,
        Matrix rootTransform,
        CompositorContext compositor,
        TextureRegistry? textures,
        List<IDisposable>? resources = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(root);
        compositor.RasterTime.Start();
        var state = new FlowRasterState(targetSize, compositor, textures) { Resources = resources ?? [] };
        try
        {
            PrepareBackdropCaptures(root, state, filter => CaptureScene(root, state, rootTransform, filter, false));
            if (root.ContainsMagnifier)
            {
                state.MagnifierBackdrop = CaptureScene(root, state, rootTransform, null, true);
            }

            using (context.PushTransform(rootTransform))
            {
                FlowLayerPainter.Paint(root, new FlowPaintContext(context, rootTransform, state));
            }
        }
        finally
        {
            foreach (BackdropCapture capture in state.Backdrops.Values.Distinct())
            {
                capture.Dispose();
            }

            state.MagnifierBackdrop?.Dispose();
            compositor.RasterTime.Stop();
        }

        return state.Resources;
    }

    /// <summary>
    /// <see cref="Draw"/> for a host's frame: an exception is reported through
    /// <see cref="FlutterError.ReportError"/> instead of propagating, and the raster outputs created
    /// before it are still returned, because the context may already draw from them.
    /// </summary>
    /// <remarks>
    /// The engine logs a failed rasterization to the console. Avalonia swallows exceptions thrown from
    /// <c>Control.Render</c>, so a host that let them propagate showed a blank view with nothing logged.
    /// </remarks>
    public static List<IDisposable> DrawFrame(
        DrawingContext context,
        ContainerFlowLayer root,
        Size targetSize,
        Matrix rootTransform,
        CompositorContext compositor,
        TextureRegistry? textures)
    {
        var resources = new List<IDisposable>();
        try
        {
            Draw(context, root, targetSize, rootTransform, compositor, textures, resources);
        }
        catch (Exception exception)
        {
            FlutterError.ReportError(new FlutterErrorDetails(
                exception: exception,
                stack: exception.StackTrace,
                library: "Plumix rasterizer",
                context: new ErrorDescription("while rasterizing a frame")));
        }

        return resources;
    }

    /// <summary>
    /// Rasterizes <paramref name="root"/> into a new <paramref name="width"/> by <paramref name="height"/>
    /// bitmap, one scene pixel per image pixel: the engine's <c>Scene::toImageSync</c>.
    /// </summary>
    public static Bitmap RasterizeToImage(ContainerFlowLayer root, int width, int height)
    {
        var image = new RenderTargetBitmap(new PixelSize(width, height), Dpi.ToAvaloniaVector());
        List<IDisposable> resources = [];
        try
        {
            using (DrawingContext context = image.CreateDrawingContext())
            {
                resources = Draw(
                    context,
                    root,
                    new Size(width, height),
                    Matrix.Identity,
                    new CompositorContext(),
                    TextureRegistry.Instance);
            }

            return image;
        }
        catch
        {
            image.Dispose();
            throw;
        }
        finally
        {
            foreach (IDisposable resource in resources)
            {
                resource.Dispose();
            }
        }
    }

    /// <summary>
    /// Assigns every backdrop filter its captured input, capturing once per backdrop id.
    /// </summary>
    internal static void PrepareBackdropCaptures(
        ContainerFlowLayer root,
        FlowRasterState state,
        Func<BackdropFilterFlowLayer, BackdropCapture> capture)
    {
        var filters = new List<BackdropFilterFlowLayer>();
        root.CollectBackdropFilters(filters);
        var groupedBackdrops = new Dictionary<int, BackdropCapture>();
        foreach (BackdropFilterFlowLayer filter in filters)
        {
            if (filter.BackdropId is { } id && groupedBackdrops.TryGetValue(id, out BackdropCapture? grouped))
            {
                state.Backdrops[filter] = grouped;
                continue;
            }

            BackdropCapture backdrop = capture(filter)
                ?? throw new InvalidOperationException("Backdrop capture must return an image.");
            state.Backdrops[filter] = backdrop;
            if (filter.BackdropId is { } key)
            {
                groupedBackdrops[key] = backdrop;
            }
        }
    }

    private static BackdropCapture CaptureScene(
        ContainerFlowLayer root,
        FlowRasterState state,
        Matrix rootTransform,
        BackdropFilterFlowLayer? stopAt,
        bool capturingMagnifier)
    {
        int width = Math.Max(1, (int)Math.Ceiling(state.TargetSize.Width));
        int height = Math.Max(1, (int)Math.Ceiling(state.TargetSize.Height));
        var bounds = Rect.FromLTWH(0.0, 0.0, width, height);
        var image = new RenderTargetBitmap(new PixelSize(width, height), Dpi.ToAvaloniaVector());
        t_captureDepth++;
        try
        {
            var captureState = new FlowRasterState(state.TargetSize, state.Compositor, state.Textures)
            {
                StopAt = stopAt,
                CapturingMagnifier = capturingMagnifier,
                Backdrops = state.Backdrops,
                Resources = state.Resources,
            };
            using (DrawingContext captureContext = image.CreateDrawingContext())
            using (captureContext.PushTransform(rootTransform))
            {
                FlowLayerPainter.Paint(root, new FlowPaintContext(captureContext, rootTransform, captureState));
            }

            return new BackdropCapture(image, bounds, ownsImage: true);
        }
        catch
        {
            image.Dispose();
            throw;
        }
        finally
        {
            t_captureDepth--;
        }
    }

    /// <summary>The statistics label of the performance overlay, or null without a font backend.</summary>
    internal static FormattedText? TryMakeStatisticsText(string text)
    {
        try
        {
            var formatted = new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                Typeface.Default,
                15,
                // Historically SK_ColorGRAY (== 0xFF888888) was used here.
                new SolidColorBrush(Avalonia.Media.Color.FromUInt32(0xFF888888)));
            // Lays the text out now, so a missing font backend surfaces here rather than in the draw.
            _ = formatted.Baseline;
            return formatted;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    internal static DrawingContext.PushedState PushRoundedRectClip(DrawingContext context, RRect rrect)
    {
        return PushRoundedRectClip(context, rrect.Rect, rrect.Radii);
    }

    internal static DrawingContext.PushedState PushRoundedRectClip(
        DrawingContext context,
        Rect rect,
        double radius)
    {
        double clampedRadius = Math.Min(Math.Max(0, radius), Math.Min(rect.Width, rect.Height) / 2.0);
        if (Capturing)
        {
            // Avalonia's recording context does not implement PushClip(RoundedRect) in every backend, but
            // its geometry-clip path records the equivalent rounded rectangle correctly.
            return context.PushGeometryClip(new RectangleGeometry(rect.ToAvalonia(), clampedRadius, clampedRadius));
        }

        return context.PushClip(new RoundedRect(rect.ToAvalonia(), clampedRadius));
    }

    internal static DrawingContext.PushedState PushRoundedRectClip(
        DrawingContext context,
        Rect rect,
        BorderRadius borderRadius)
    {
        // SkRRect::setRectRadii: oversized radii shrink by one factor for every corner, keeping each corner's
        // aspect ratio — BorderRadius.circular(100) on an 8 px tall bar is a stadium, not a lens-shaped ellipse
        // that clamping X to half the width and Y to half the height would give.
        RRect scaled = new RRect(
            rect,
            ClipRadius(borderRadius.TopLeftRadius),
            ClipRadius(borderRadius.TopRightRadius),
            ClipRadius(borderRadius.BottomRightRadius),
            ClipRadius(borderRadius.BottomLeftRadius)).ScaleRadii();
        Radius topLeft = scaled.TopLeft;
        Radius topRight = scaled.TopRight;
        Radius bottomRight = scaled.BottomRight;
        Radius bottomLeft = scaled.BottomLeft;
        if (Capturing)
        {
            double fallbackX = Math.Max(
                Math.Max(topLeft.X, topRight.X),
                Math.Max(bottomRight.X, bottomLeft.X));
            double fallbackY = Math.Max(
                Math.Max(topLeft.Y, topRight.Y),
                Math.Max(bottomRight.Y, bottomLeft.Y));
            return context.PushGeometryClip(new RectangleGeometry(rect.ToAvalonia(), fallbackX, fallbackY));
        }

        return context.PushClip(new RoundedRect(
            rect.ToAvalonia(),
            new Offset(topLeft.X, topLeft.Y).ToAvaloniaVector(),
            new Offset(topRight.X, topRight.Y).ToAvaloniaVector(),
            new Offset(bottomRight.X, bottomRight.Y).ToAvaloniaVector(),
            new Offset(bottomLeft.X, bottomLeft.Y).ToAvaloniaVector()));
    }

    private static Radius ClipRadius(Radius radius) =>
        radius.X > 0.0 && radius.Y > 0.0 && double.IsFinite(radius.X) && double.IsFinite(radius.Y)
            ? radius
            : Radius.Zero;
}
