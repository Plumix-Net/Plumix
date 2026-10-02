using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Plumix.Foundation;
using Plumix.Rendering;

// Dart parity source: flutter/engine/src/flutter/lib/ui/compositing.dart

namespace Plumix.UI;

/// <summary>
/// The engine-side retained object a <see cref="SceneBuilder"/> push returns, which a later frame can
/// pass back as an <c>oldLayer</c> or re-add whole with <see cref="SceneBuilder.AddRetained"/>.
/// </summary>
/// <remarks>
/// dart:ui's <c>EngineLayer</c> (painting.dart) folded together with compositing.dart's private
/// <c>_EngineLayerWrapper</c>: the typed subclasses (<see cref="OffsetEngineLayer"/>, ...) are the
/// wrappers themselves, built by the scene builder around the native layer. The protected constructor is
/// for custom implementations such as test fakes, which a scene builder rejects as Dart's does.
/// </remarks>
public abstract class EngineLayer : IDisposable
{
    private NativeEngineLayer? _nativeLayer;
    private readonly bool _isWrapper;

    /// <summary>Creates a custom engine layer: one no <see cref="SceneBuilder"/> accepts back.</summary>
    protected EngineLayer()
    {
    }

    private protected EngineLayer(NativeEngineLayer nativeLayer)
    {
        _nativeLayer = nativeLayer;
        _isWrapper = true;
    }

    /// <summary>Whether a scene builder created this layer: Dart's <c>is _EngineLayerWrapper</c>.</summary>
    internal bool IsWrapper => _isWrapper;

    /// <summary>The native layer this wrapper holds, null once disposed (debug builds).</summary>
    /// <remarks>Dart's <c>_EngineLayerWrapper._nativeLayer</c>.</remarks>
    internal NativeEngineLayer? NativeLayer => _nativeLayer;

    /// <summary>The layers pushed while this one was the top of the stack; debug builds only.</summary>
    /// <remarks>Dart's <c>_EngineLayerWrapper._debugChildren</c>.</remarks>
    internal List<EngineLayer>? DebugChildren { get; set; }

    /// <summary>Whether this layer was passed to a push as <c>oldLayer</c>; debug builds only.</summary>
    /// <remarks>Dart's <c>_EngineLayerWrapper._debugWasUsedAsOldLayer</c>.</remarks>
    internal bool DebugWasUsedAsOldLayer { get; set; }

    /// <summary>Releases the resources used by this layer. The layer may not be used afterwards.</summary>
    /// <remarks>Dart's <c>EngineLayer.dispose</c>.</remarks>
    public virtual void Dispose()
    {
        if (!_isWrapper)
        {
            return;
        }

        if (Constants.KDebugMode && _nativeLayer == null)
        {
            throw new AssertionError("Object disposed");
        }

        _nativeLayer?.Dispose();
        if (Constants.KDebugMode)
        {
            _nativeLayer = null;
        }
    }

    /// <remarks>Dart's <c>_EngineLayerWrapper._debugCheckNotUsedAsOldLayer</c>.</remarks>
    internal bool DebugCheckNotUsedAsOldLayer()
    {
        if (DebugWasUsedAsOldLayer)
        {
            throw new AssertionError(
                $"Layer {GetType().Name}#{Diagnostics.ShortHash(this)} was previously used as oldLayer.\n"
                + "Once a layer is used as oldLayer, it may not be used again. Instead, after calling one of "
                + "the SceneBuilder.push* methods and passing an oldLayer to it, use the layer returned by "
                + "the method as oldLayer in subsequent frames.");
        }

        return true;
    }
}

/// <summary>The native half of an <see cref="EngineLayer"/>: the retained flow layer it keeps alive.</summary>
/// <remarks>
/// dart:ui's <c>_NativeEngineLayer</c> and the engine's <c>flutter::EngineLayer</c>, whose
/// <c>dispose</c> releases the layer (<c>layer_.reset()</c>).
/// </remarks>
internal sealed class NativeEngineLayer
{
    internal NativeEngineLayer(ContainerFlowLayer layer)
    {
        Layer = layer;
    }

    /// <summary>The retained layer, null once disposed: the engine's <c>EngineLayer::Layer()</c>.</summary>
    public ContainerFlowLayer? Layer { get; private set; }

    public void Dispose()
    {
        Layer = null;
    }
}

/// <summary>
/// An opaque handle to a transform engine layer, returned by <see cref="SceneBuilder.PushTransform"/>.
/// </summary>
public class TransformEngineLayer : EngineLayer
{
    /// <summary>Creates a custom implementation, such as a test fake.</summary>
    protected TransformEngineLayer()
    {
    }

    internal TransformEngineLayer(NativeEngineLayer nativeLayer) : base(nativeLayer)
    {
    }
}

/// <summary>An opaque handle to an offset engine layer, returned by <see cref="SceneBuilder.PushOffset"/>.</summary>
public class OffsetEngineLayer : EngineLayer
{
    /// <summary>Creates a custom implementation, such as a test fake.</summary>
    protected OffsetEngineLayer()
    {
    }

    internal OffsetEngineLayer(NativeEngineLayer nativeLayer) : base(nativeLayer)
    {
    }
}

/// <summary>
/// An opaque handle to a clip-rect engine layer, returned by <see cref="SceneBuilder.PushClipRect"/>.
/// </summary>
public class ClipRectEngineLayer : EngineLayer
{
    /// <summary>Creates a custom implementation, such as a test fake.</summary>
    protected ClipRectEngineLayer()
    {
    }

    internal ClipRectEngineLayer(NativeEngineLayer nativeLayer) : base(nativeLayer)
    {
    }
}

/// <summary>
/// An opaque handle to a clip-rrect engine layer, returned by <see cref="SceneBuilder.PushClipRRect"/>.
/// </summary>
public class ClipRRectEngineLayer : EngineLayer
{
    /// <summary>Creates a custom implementation, such as a test fake.</summary>
    protected ClipRRectEngineLayer()
    {
    }

    internal ClipRRectEngineLayer(NativeEngineLayer nativeLayer) : base(nativeLayer)
    {
    }
}

/// <summary>
/// An opaque handle to a clip-rsuperellipse engine layer, returned by
/// <see cref="SceneBuilder.PushClipRSuperellipse"/>.
/// </summary>
public class ClipRSuperellipseEngineLayer : EngineLayer
{
    /// <summary>Creates a custom implementation, such as a test fake.</summary>
    protected ClipRSuperellipseEngineLayer()
    {
    }

    internal ClipRSuperellipseEngineLayer(NativeEngineLayer nativeLayer) : base(nativeLayer)
    {
    }
}

/// <summary>
/// An opaque handle to a clip-path engine layer, returned by <see cref="SceneBuilder.PushClipPath"/>.
/// </summary>
public class ClipPathEngineLayer : EngineLayer
{
    /// <summary>Creates a custom implementation, such as a test fake.</summary>
    protected ClipPathEngineLayer()
    {
    }

    internal ClipPathEngineLayer(NativeEngineLayer nativeLayer) : base(nativeLayer)
    {
    }
}

/// <summary>An opaque handle to an opacity engine layer, returned by <see cref="SceneBuilder.PushOpacity"/>.</summary>
public class OpacityEngineLayer : EngineLayer
{
    /// <summary>Creates a custom implementation, such as a test fake.</summary>
    protected OpacityEngineLayer()
    {
    }

    internal OpacityEngineLayer(NativeEngineLayer nativeLayer) : base(nativeLayer)
    {
    }
}

/// <summary>
/// An opaque handle to a color-filter engine layer, returned by <see cref="SceneBuilder.PushColorFilter"/>.
/// </summary>
public class ColorFilterEngineLayer : EngineLayer
{
    /// <summary>Creates a custom implementation, such as a test fake.</summary>
    protected ColorFilterEngineLayer()
    {
    }

    internal ColorFilterEngineLayer(NativeEngineLayer nativeLayer) : base(nativeLayer)
    {
    }
}

/// <summary>
/// An opaque handle to an image-filter engine layer, returned by <see cref="SceneBuilder.PushImageFilter"/>.
/// </summary>
public class ImageFilterEngineLayer : EngineLayer
{
    /// <summary>Creates a custom implementation, such as a test fake.</summary>
    protected ImageFilterEngineLayer()
    {
    }

    internal ImageFilterEngineLayer(NativeEngineLayer nativeLayer) : base(nativeLayer)
    {
    }
}

/// <summary>
/// An opaque handle to a backdrop-filter engine layer, returned by
/// <see cref="SceneBuilder.PushBackdropFilter"/>.
/// </summary>
public class BackdropFilterEngineLayer : EngineLayer
{
    /// <summary>Creates a custom implementation, such as a test fake.</summary>
    protected BackdropFilterEngineLayer()
    {
    }

    internal BackdropFilterEngineLayer(NativeEngineLayer nativeLayer) : base(nativeLayer)
    {
    }
}

/// <summary>
/// An opaque handle to a shader-mask engine layer, returned by <see cref="SceneBuilder.PushShaderMask"/>.
/// </summary>
public class ShaderMaskEngineLayer : EngineLayer
{
    /// <summary>Creates a custom implementation, such as a test fake.</summary>
    protected ShaderMaskEngineLayer()
    {
    }

    internal ShaderMaskEngineLayer(NativeEngineLayer nativeLayer) : base(nativeLayer)
    {
    }
}

/// <summary>Plumix-only: the handle <see cref="SceneBuilder.PushMagnifier"/> returns.</summary>
internal sealed class MagnifierEngineLayer : EngineLayer
{
    internal MagnifierEngineLayer(NativeEngineLayer nativeLayer) : base(nativeLayer)
    {
    }
}

/// <summary>Plumix-only: the handle <see cref="SceneBuilder.PushClipGeometry"/> returns.</summary>
internal sealed class ClipGeometryEngineLayer : EngineLayer
{
    internal ClipGeometryEngineLayer(NativeEngineLayer nativeLayer) : base(nativeLayer)
    {
    }
}

/// <summary>Plumix-only: the handle <see cref="SceneBuilder.PushSnapshot"/> returns.</summary>
internal sealed class SnapshotEngineLayer : EngineLayer
{
    internal SnapshotEngineLayer(NativeEngineLayer nativeLayer) : base(nativeLayer)
    {
    }
}

/// <summary>
/// An exception thrown by <see cref="Scene.ToImageSync"/> when the scene cannot be rasterized.
/// </summary>
/// <remarks>dart:ui's <c>PictureRasterizationException</c> (painting.dart).</remarks>
public sealed class PictureRasterizationException : Exception
{
    internal PictureRasterizationException(string message, string? stack = null) : base(message)
    {
        RasterizationMessage = message;
        Stack = stack;
    }

    /// <summary>A string containing details about the failure: Dart's <c>message</c>.</summary>
    public string RasterizationMessage { get; }

    /// <summary>If available, the stack trace at the time the rasterization was requested.</summary>
    public string? Stack { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        var buffer = new System.Text.StringBuilder($"Failed to rasterize a picture: {RasterizationMessage}.");
        if (Stack != null)
        {
            buffer.AppendLine();
            buffer.AppendLine("The callstack when the image was created was:");
            buffer.AppendLine(Stack);
        }

        return buffer.ToString();
    }
}

/// <summary>An opaque object representing a composited scene.</summary>
/// <remarks>
/// dart:ui's <c>Scene</c> (with <c>_NativeScene</c>). Created by <see cref="SceneBuilder.Build"/> and
/// shown with <see cref="Widgets.FlutterView.Render"/>. The scene holds the engine's layer tree; its
/// images are rasterized by the Plumix scene rasterizer through Avalonia's render interface.
/// </remarks>
public sealed class Scene : IDisposable
{
    private ContainerFlowLayer? _rootLayer;

    internal Scene(ContainerFlowLayer rootLayer)
    {
        _rootLayer = rootLayer;
    }

    /// <summary>The engine's layer tree root, null once disposed or taken by a host.</summary>
    internal ContainerFlowLayer? RootLayer => _rootLayer;

    /// <summary>
    /// Synchronously creates a <paramref name="width"/> by <paramref name="height"/> image of this scene.
    /// </summary>
    /// <remarks>Dart's <c>Scene.toImageSync</c>; the scene's physical pixels map 1:1 to image pixels.</remarks>
    public Bitmap ToImageSync(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentException("Invalid image dimensions.");
        }

        ContainerFlowLayer root = _rootLayer
            ?? throw new PictureRasterizationException("Scene has been disposed.");
        return SceneRasterizer.RasterizeToImage(root, width, height);
    }

    /// <summary>Creates a <paramref name="width"/> by <paramref name="height"/> image of this scene.</summary>
    /// <remarks>
    /// Dart's <c>Scene.toImage</c>. Dart's <c>_futurize</c> throws a synchronous error synchronously; the
    /// Plumix rasterizer renders on the calling thread, so the task is already complete when it returns.
    /// </remarks>
    public Task<Bitmap> ToImage(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentException("Invalid image dimensions.");
        }

        if (_rootLayer == null)
        {
            throw new InvalidOperationException("Scene has been disposed.");
        }

        return Task.FromResult(SceneRasterizer.RasterizeToImage(_rootLayer, width, height));
    }

    /// <summary>
    /// Moves the layer tree out of this scene, for the host that renders it: the engine's
    /// <c>Scene::takeLayerTree</c>. The scene holds no layer tree afterwards.
    /// </summary>
    internal ContainerFlowLayer? TakeLayerTree()
    {
        ContainerFlowLayer? root = _rootLayer;
        _rootLayer = null;
        return root;
    }

    /// <summary>Releases the resources used by this scene. The scene may not be used afterwards.</summary>
    /// <remarks>Dart's <c>Scene.dispose</c>; it drops the reference to the layer tree.</remarks>
    public void Dispose()
    {
        _rootLayer = null;
    }

    /// <inheritdoc />
    public override string ToString() => "Scene";
}

/// <summary>Builds a <see cref="Scene"/> containing the given visuals.</summary>
/// <remarks>
/// dart:ui's <c>SceneBuilder</c> (with <c>_NativeSceneBuilder</c> and the native
/// <c>flutter::SceneBuilder</c>). Every push adds a new engine layer as a child of the top of the
/// stack; <see cref="AddRetained"/> re-adds a previous frame's engine layer and its whole subtree.
/// The members are virtual so a test can stand in for the builder, as flutter_test's fakes do.
/// </remarks>
public class SceneBuilder
{
    // The native builder's stack: the root container, then every pushed layer.
    private readonly List<ContainerFlowLayer> _nativeLayerStack = [new ContainerFlowLayer()];

    // In debug mode checks that a layer is only used once in a given scene.
    private readonly Dictionary<EngineLayer, string> _usedLayers = new(ReferenceEqualityComparer.Instance);

    // In debug mode the wrappers of the pushed layers, to record each layer's children.
    private readonly List<EngineLayer> _layerStack = [];

    /// <summary>Pushes a transform operation onto the operation stack.</summary>
    /// <remarks>
    /// Dart's <c>pushTransform(Float64List matrix4, {oldLayer})</c>: <paramref name="matrix4"/> is a
    /// column-major 4x4 matrix, as <see cref="Matrix4.Storage"/> holds it.
    /// </remarks>
    public virtual TransformEngineLayer PushTransform(double[] matrix4, TransformEngineLayer? oldLayer = null)
    {
        ArgumentNullException.ThrowIfNull(matrix4);
        DebugAssertMatrix4IsValid(matrix4);
        DebugCheckCanBeUsedAsOldLayer(oldLayer, "pushTransform");
        var layer = new TransformEngineLayer(PushNativeLayer(new TransformFlowLayer(Matrix4.FromList(matrix4))));
        DebugPushLayer(layer);
        return layer;
    }

    /// <summary>Pushes an offset operation onto the operation stack.</summary>
    /// <remarks>Dart's <c>pushOffset</c>; natively a translation transform layer.</remarks>
    public virtual OffsetEngineLayer PushOffset(double dx, double dy, OffsetEngineLayer? oldLayer = null)
    {
        DebugCheckCanBeUsedAsOldLayer(oldLayer, "pushOffset");
        var layer = new OffsetEngineLayer(
            PushNativeLayer(new TransformFlowLayer(Matrix4.TranslationValues(dx, dy, 0.0))));
        DebugPushLayer(layer);
        return layer;
    }

    /// <summary>Pushes a rectangular clip operation onto the operation stack.</summary>
    /// <remarks>Dart's <c>pushClipRect</c>.</remarks>
    public virtual ClipRectEngineLayer PushClipRect(
        Rect rect,
        Clip clipBehavior = Clip.AntiAlias,
        ClipRectEngineLayer? oldLayer = null)
    {
        DebugAssertClipBehavior(clipBehavior);
        DebugCheckCanBeUsedAsOldLayer(oldLayer, "pushClipRect");
        var layer = new ClipRectEngineLayer(PushNativeLayer(new ClipRectFlowLayer(rect, clipBehavior)));
        DebugPushLayer(layer);
        return layer;
    }

    /// <summary>Pushes a rounded-rectangular clip operation onto the operation stack.</summary>
    /// <remarks>Dart's <c>pushClipRRect</c>.</remarks>
    public virtual ClipRRectEngineLayer PushClipRRect(
        RRect rrect,
        Clip clipBehavior = Clip.AntiAlias,
        ClipRRectEngineLayer? oldLayer = null)
    {
        DebugAssertClipBehavior(clipBehavior);
        DebugCheckCanBeUsedAsOldLayer(oldLayer, "pushClipRRect");
        var layer = new ClipRRectEngineLayer(PushNativeLayer(new ClipRRectFlowLayer(rrect, clipBehavior)));
        DebugPushLayer(layer);
        return layer;
    }

    /// <summary>Pushes a rounded-superellipse clip operation onto the operation stack.</summary>
    /// <remarks>Dart's <c>pushClipRSuperellipse</c>.</remarks>
    public virtual ClipRSuperellipseEngineLayer PushClipRSuperellipse(
        RSuperellipse rsuperellipse,
        Clip clipBehavior = Clip.AntiAlias,
        ClipRSuperellipseEngineLayer? oldLayer = null)
    {
        DebugAssertClipBehavior(clipBehavior);
        DebugCheckCanBeUsedAsOldLayer(oldLayer, "pushClipRSuperellipse");
        var layer = new ClipRSuperellipseEngineLayer(
            PushNativeLayer(new ClipRSuperellipseFlowLayer(rsuperellipse, clipBehavior)));
        DebugPushLayer(layer);
        return layer;
    }

    /// <summary>Pushes a path clip operation onto the operation stack.</summary>
    /// <remarks>Dart's <c>pushClipPath</c>.</remarks>
    public virtual ClipPathEngineLayer PushClipPath(
        Path path,
        Clip clipBehavior = Clip.AntiAlias,
        ClipPathEngineLayer? oldLayer = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        DebugAssertClipBehavior(clipBehavior);
        DebugCheckCanBeUsedAsOldLayer(oldLayer, "pushClipPath");
        var flowLayer = new ClipPathFlowLayer(path, clipBehavior);
        flowLayer.AssignOldLayer(oldLayer?.NativeLayer?.Layer as ClipPathFlowLayer);
        var layer = new ClipPathEngineLayer(PushNativeLayer(flowLayer));
        DebugPushLayer(layer);
        return layer;
    }

    /// <summary>Pushes an opacity operation onto the operation stack.</summary>
    /// <remarks>
    /// Dart's <c>pushOpacity(int alpha, {Offset? offset = Offset.zero, oldLayer})</c>: 0 is fully
    /// transparent and 255 fully opaque. Dart has no range check on <paramref name="alpha"/>.
    /// </remarks>
    public virtual OpacityEngineLayer PushOpacity(
        int alpha,
        Point offset = default,
        OpacityEngineLayer? oldLayer = null)
    {
        DebugCheckCanBeUsedAsOldLayer(oldLayer, "pushOpacity");
        var layer = new OpacityEngineLayer(PushNativeLayer(new OpacityFlowLayer(alpha, offset)));
        DebugPushLayer(layer);
        return layer;
    }

    /// <summary>Pushes a color filter operation onto the operation stack.</summary>
    /// <remarks>Dart's <c>pushColorFilter</c>.</remarks>
    public virtual ColorFilterEngineLayer PushColorFilter(ColorFilter filter, ColorFilterEngineLayer? oldLayer = null)
    {
        return PushColorFilter(filter, bounds: null, oldLayer);
    }

    /// <summary>
    /// <see cref="PushColorFilter(ColorFilter, ColorFilterEngineLayer?)"/> with the Plumix-only bounds
    /// the CPU filter rasterizes its children into (docs/ai/DIVERGENCES.md).
    /// </summary>
    internal ColorFilterEngineLayer PushColorFilter(
        ColorFilter filter,
        Rect? bounds,
        ColorFilterEngineLayer? oldLayer)
    {
        ArgumentNullException.ThrowIfNull(filter);
        DebugCheckCanBeUsedAsOldLayer(oldLayer, "pushColorFilter");
        var layer = new ColorFilterEngineLayer(PushNativeLayer(new ColorFilterFlowLayer(filter, bounds)));
        DebugPushLayer(layer);
        return layer;
    }

    /// <summary>Pushes an image filter operation onto the operation stack.</summary>
    /// <remarks>Dart's <c>pushImageFilter</c>.</remarks>
    public virtual ImageFilterEngineLayer PushImageFilter(
        ImageFilter filter,
        Point offset = default,
        ImageFilterEngineLayer? oldLayer = null)
    {
        return PushImageFilter(filter, offset, bounds: null, oldLayer);
    }

    /// <summary>
    /// <see cref="PushImageFilter(ImageFilter, Point, ImageFilterEngineLayer?)"/> with the Plumix-only
    /// bounds, in the pushed offset's coordinates, that the CPU filter rasterizes its children into.
    /// </summary>
    internal ImageFilterEngineLayer PushImageFilter(
        ImageFilter filter,
        Point offset,
        Rect? bounds,
        ImageFilterEngineLayer? oldLayer)
    {
        ArgumentNullException.ThrowIfNull(filter);
        DebugCheckCanBeUsedAsOldLayer(oldLayer, "pushImageFilter");
        var layer = new ImageFilterEngineLayer(PushNativeLayer(new ImageFilterFlowLayer(filter, offset, bounds)));
        DebugPushLayer(layer);
        return layer;
    }

    /// <summary>Pushes a backdrop filter operation onto the operation stack.</summary>
    /// <remarks>
    /// Dart's <c>pushBackdropFilter</c>. Backdrop filters that share a <paramref name="backdropId"/>
    /// sample one backdrop.
    /// </remarks>
    public virtual BackdropFilterEngineLayer PushBackdropFilter(
        ImageFilter filter,
        BlendMode blendMode = BlendMode.SourceOver,
        BackdropFilterEngineLayer? oldLayer = null,
        int? backdropId = null)
    {
        ArgumentNullException.ThrowIfNull(filter);
        DebugCheckCanBeUsedAsOldLayer(oldLayer, "pushBackdropFilter");
        var layer = new BackdropFilterEngineLayer(
            PushNativeLayer(new BackdropFilterFlowLayer(filter, blendMode, backdropId)));
        DebugPushLayer(layer);
        return layer;
    }

    /// <summary>Pushes a shader mask operation onto the operation stack.</summary>
    /// <remarks>Dart's <c>pushShaderMask</c>; the shader is an Avalonia brush (docs/ai/DIVERGENCES.md).</remarks>
    public virtual ShaderMaskEngineLayer PushShaderMask(
        IBrush shader,
        Rect maskRect,
        BlendMode blendMode,
        ShaderMaskEngineLayer? oldLayer = null,
        FilterQuality filterQuality = FilterQuality.Low)
    {
        ArgumentNullException.ThrowIfNull(shader);
        DebugCheckCanBeUsedAsOldLayer(oldLayer, "pushShaderMask");
        var layer = new ShaderMaskEngineLayer(
            PushNativeLayer(new ShaderMaskFlowLayer(shader, maskRect, blendMode, filterQuality)));
        DebugPushLayer(layer);
        return layer;
    }

    /// <summary>Plumix-only: pushes the lens of a <see cref="Rendering.MagnifierLayer"/>.</summary>
    internal MagnifierEngineLayer PushMagnifier(MagnifierFlowLayer magnifier, MagnifierEngineLayer? oldLayer)
    {
        DebugCheckCanBeUsedAsOldLayer(oldLayer, "pushMagnifier");
        var layer = new MagnifierEngineLayer(PushNativeLayer(magnifier));
        DebugPushLayer(layer);
        return layer;
    }

    /// <summary>Plumix-only: pushes the backend-geometry clip of a <see cref="Rendering.ClipGeometryLayer"/>.</summary>
    internal ClipGeometryEngineLayer PushClipGeometry(
        Geometry geometry,
        Point geometryOffset,
        Clip clipBehavior,
        ClipGeometryEngineLayer? oldLayer)
    {
        DebugCheckCanBeUsedAsOldLayer(oldLayer, "pushClipGeometry");
        var layer = new ClipGeometryEngineLayer(
            PushNativeLayer(new ClipGeometryFlowLayer(geometry, geometryOffset, clipBehavior)));
        DebugPushLayer(layer);
        return layer;
    }

    /// <summary>
    /// Plumix-only: pushes the raster snapshot of a <see cref="Rendering.SnapshotOffsetLayer"/>, which
    /// draws its children once into <paramref name="cache"/> and the cached image from then on.
    /// </summary>
    internal SnapshotEngineLayer PushSnapshot(SnapshotRasterCache cache, Point offset, SnapshotEngineLayer? oldLayer)
    {
        ArgumentNullException.ThrowIfNull(cache);
        DebugCheckCanBeUsedAsOldLayer(oldLayer, "pushSnapshot");
        var layer = new SnapshotEngineLayer(PushNativeLayer(new SnapshotFlowLayer(cache, offset)));
        DebugPushLayer(layer);
        return layer;
    }

    /// <summary>Ends the effect of the most recently pushed operation.</summary>
    /// <remarks>
    /// Dart's <c>pop</c>. Popping more than was pushed does nothing: the native builder never pops its
    /// root container.
    /// </remarks>
    public virtual void Pop()
    {
        if (_layerStack.Count > 0)
        {
            _layerStack.RemoveAt(_layerStack.Count - 1);
        }

        if (_nativeLayerStack.Count > 1)
        {
            _nativeLayerStack.RemoveAt(_nativeLayerStack.Count - 1);
        }
    }

    /// <summary>
    /// Adds a retained engine layer subtree to the scene, unchanged from the frame that built it.
    /// </summary>
    /// <remarks>Dart's <c>addRetained</c>.</remarks>
    public virtual void AddRetained(EngineLayer retainedLayer)
    {
        ArgumentNullException.ThrowIfNull(retainedLayer);
        if (!retainedLayer.IsWrapper)
        {
            throw new AssertionError("retainedLayer is not an engine layer created by a SceneBuilder.");
        }

        if (Constants.KDebugMode)
        {
            if (retainedLayer.NativeLayer == null)
            {
                throw new AssertionError("retainedLayer has been disposed.");
            }

            RecursivelyCheckChildrenUsedOnce(retainedLayer);
        }

        ContainerFlowLayer? retained = retainedLayer.NativeLayer?.Layer;
        if (retained != null)
        {
            AddNativeLayer(retained);
        }
    }

    /// <summary>Adds an object to the scene that displays performance statistics.</summary>
    /// <remarks>
    /// Dart's <c>addPerformanceOverlay</c>: bit 0 displays and bit 1 visualizes the rasterizer
    /// statistics, bit 2 displays and bit 3 visualizes the engine (UI) statistics.
    /// </remarks>
    public virtual void AddPerformanceOverlay(int enabledOptions, Rect bounds)
    {
        AddNativeLayer(new PerformanceOverlayFlowLayer(enabledOptions, bounds));
    }

    /// <summary>Adds a <see cref="Picture"/> to the scene, drawn at <paramref name="offset"/>.</summary>
    /// <remarks>Dart's <c>addPicture</c>; the raster-cache hints are recorded and otherwise unused.</remarks>
    public virtual void AddPicture(
        Point offset,
        Picture picture,
        bool isComplexHint = false,
        bool willChangeHint = false)
    {
        ArgumentNullException.ThrowIfNull(picture);
        if (Constants.KDebugMode && picture.DebugDisposed)
        {
            throw new AssertionError("picture has been disposed.");
        }

        AddNativeLayer(new DisplayListFlowLayer(offset, picture, isComplexHint, willChangeHint));
    }

    /// <summary>Adds a backend texture to the scene.</summary>
    /// <remarks>
    /// Dart's <c>addTexture</c>. The texture is looked up in <see cref="TextureRegistry"/> when the scene
    /// is rasterized; an unknown id draws nothing.
    /// </remarks>
    public virtual void AddTexture(
        int textureId,
        Point offset = default,
        double width = 0.0,
        double height = 0.0,
        bool freeze = false,
        FilterQuality filterQuality = FilterQuality.Low)
    {
        AddNativeLayer(new TextureFlowLayer(
            offset,
            new Size(Math.Max(0.0, width), Math.Max(0.0, height)),
            textureId,
            freeze,
            filterQuality));
    }

    /// <summary>Adds a platform view (for example, an iOS UIView) to the scene.</summary>
    /// <remarks>Dart's <c>addPlatformView</c>; no Plumix host embeds platform views (docs/ai/DIVERGENCES.md).</remarks>
    public virtual void AddPlatformView(
        int viewId,
        Point offset = default,
        double width = 0.0,
        double height = 0.0)
    {
        AddNativeLayer(new PlatformViewFlowLayer(
            offset,
            new Size(Math.Max(0.0, width), Math.Max(0.0, height)),
            viewId));
    }

    /// <summary>Finishes building the scene. The builder may not be used afterwards.</summary>
    /// <remarks>Dart's <c>build</c>: the root container becomes the scene's layer tree.</remarks>
    public virtual Scene Build()
    {
        if (_nativeLayerStack.Count == 0)
        {
            throw new InvalidOperationException("SceneBuilder.Build may only be called once.");
        }

        var scene = new Scene(_nativeLayerStack[0]);
        _nativeLayerStack.Clear();
        return scene;
    }

    /// <inheritdoc />
    public override string ToString() => "SceneBuilder";

    private NativeEngineLayer PushNativeLayer(ContainerFlowLayer layer)
    {
        AddNativeLayer(layer);
        _nativeLayerStack.Add(layer);
        return new NativeEngineLayer(layer);
    }

    private void AddNativeLayer(FlowLayer layer)
    {
        if (_nativeLayerStack.Count == 0)
        {
            throw new InvalidOperationException("The SceneBuilder was already built.");
        }

        _nativeLayerStack[^1].Add(layer);
    }

    private static void DebugAssertMatrix4IsValid(double[] matrix4)
    {
        if (!Constants.KDebugMode)
        {
            return;
        }

        if (matrix4.Length != 16)
        {
            throw new AssertionError("Matrix4 must have 16 entries.");
        }

        if (!matrix4.All(double.IsFinite))
        {
            throw new AssertionError("Matrix4 entries must be finite.");
        }
    }

    private static void DebugAssertClipBehavior(Clip clipBehavior)
    {
        if (Constants.KDebugMode && clipBehavior == Clip.None)
        {
            throw new AssertionError("clipBehavior != Clip.none");
        }
    }

    /// <remarks>Dart's <c>_debugCheckUsedOnce</c>.</remarks>
    private void DebugCheckUsedOnce(EngineLayer layer, string usage)
    {
        if (_usedLayers.TryGetValue(layer, out string? previousUsage))
        {
            throw new AssertionError(
                $"Layer {layer.GetType().Name} already used.\n"
                + $"The layer is already being used as {previousUsage} in this scene.\n"
                + "A layer may only be used once in a given scene.");
        }

        _usedLayers[layer] = usage;
    }

    /// <remarks>Dart's <c>_debugCheckCanBeUsedAsOldLayer</c>.</remarks>
    private void DebugCheckCanBeUsedAsOldLayer(EngineLayer? layer, string methodName)
    {
        if (!Constants.KDebugMode || layer == null)
        {
            return;
        }

        if (layer.IsWrapper && layer.NativeLayer == null)
        {
            throw new AssertionError("Object disposed");
        }

        layer.DebugCheckNotUsedAsOldLayer();
        DebugCheckUsedOnce(layer, $"oldLayer in {methodName}");
        layer.DebugWasUsedAsOldLayer = true;
    }

    /// <remarks>Dart's <c>_debugPushLayer</c>.</remarks>
    private void DebugPushLayer(EngineLayer newLayer)
    {
        if (!Constants.KDebugMode)
        {
            return;
        }

        if (_layerStack.Count > 0)
        {
            EngineLayer currentLayer = _layerStack[^1];
            currentLayer.DebugChildren ??= [];
            currentLayer.DebugChildren.Add(newLayer);
        }

        _layerStack.Add(newLayer);
    }

    /// <remarks>The <c>recursivelyCheckChildrenUsedOnce</c> walk of Dart's <c>addRetained</c>.</remarks>
    private void RecursivelyCheckChildrenUsedOnce(EngineLayer parentLayer)
    {
        DebugCheckUsedOnce(parentLayer, "retained layer");
        parentLayer.DebugCheckNotUsedAsOldLayer();
        if (parentLayer.DebugChildren is not { Count: > 0 } children)
        {
            return;
        }

        foreach (EngineLayer child in children)
        {
            RecursivelyCheckChildrenUsedOnce(child);
        }
    }
}
