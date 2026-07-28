using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/layer.dart (approximate)

namespace Plumix.Rendering;

public abstract class Layer
{
    [ThreadStatic]
    private static BackdropCapture? _magnifierBackdrop;

    [ThreadStatic]
    private static bool _capturingMagnifierBackdrop;

    [ThreadStatic]
    private static BackdropFilterLayer? _backdropCaptureTarget;

    [ThreadStatic]
    private static bool _backdropCaptureStopped;

    public ContainerLayer? Parent { get; private set; }

    internal virtual bool ContainsMagnifier => false;

    internal virtual bool ContainsBackdropFilter => false;

    internal static BackdropCapture? MagnifierBackdrop => _magnifierBackdrop;

    internal static bool CapturingMagnifierBackdrop => _capturingMagnifierBackdrop;

    internal static bool CapturingBackdrop => _backdropCaptureTarget != null;

    internal static bool BackdropCaptureStopped => _backdropCaptureStopped;

    internal static void BeginMagnifierBackdropCapture()
    {
        _capturingMagnifierBackdrop = true;
        _magnifierBackdrop = null;
    }

    internal static void EndMagnifierBackdropCapture(BackdropCapture backdrop)
    {
        _capturingMagnifierBackdrop = false;
        _magnifierBackdrop = backdrop;
    }

    internal static void ClearMagnifierBackdrop()
    {
        _capturingMagnifierBackdrop = false;
        _magnifierBackdrop?.Dispose();
        _magnifierBackdrop = null;
    }

    internal static void BeginBackdropCapture(BackdropFilterLayer target)
    {
        _backdropCaptureTarget = target ?? throw new ArgumentNullException(nameof(target));
        _backdropCaptureStopped = false;
    }

    internal static bool IsBackdropCaptureTarget(BackdropFilterLayer layer)
    {
        return ReferenceEquals(_backdropCaptureTarget, layer);
    }

    internal static void StopBackdropCapture()
    {
        _backdropCaptureStopped = true;
    }

    internal static void ClearBackdropCapture()
    {
        _backdropCaptureTarget = null;
        _backdropCaptureStopped = false;
    }

    internal static DrawingContext.PushedState PushRoundedRectClip(
        DrawingContext context,
        Rect rect,
        double radius)
    {
        double clampedRadius = Math.Min(Math.Max(0, radius), Math.Min(rect.Width, rect.Height) / 2.0);
        if (CapturingMagnifierBackdrop || CapturingBackdrop)
        {
            // Avalonia's DrawingGroup recording context does not implement PushClip(RoundedRect), but its
            // geometry-clip path records the equivalent rounded rectangle correctly.
            return context.PushGeometryClip(new RectangleGeometry(rect, clampedRadius, clampedRadius));
        }

        return context.PushClip(new RoundedRect(rect, clampedRadius));
    }

    internal virtual void Attach(ContainerLayer parent)
    {
        Parent = parent;
    }

    internal virtual void Detach()
    {
        Parent = null;
    }

    internal abstract void AddToScene(DrawingContext context, Point offset);

    internal virtual void CollectBackdropFilters(ICollection<BackdropFilterLayer> filters)
    {
    }
}

public class ContainerLayer : Layer
{
    private readonly List<Layer> _children = [];

    public IReadOnlyList<Layer> Children => _children;

    internal override bool ContainsMagnifier => _children.Any(static child => child.ContainsMagnifier);

    internal override bool ContainsBackdropFilter => _children.Any(static child => child.ContainsBackdropFilter);

    public void Append(Layer child)
    {
        if (ReferenceEquals(child.Parent, this))
        {
            return;
        }

        child.Parent?.Remove(child);
        _children.Add(child);
        child.Attach(this);
    }

    public void Remove(Layer child)
    {
        if (_children.Remove(child))
        {
            child.Detach();
        }
    }

    public void RemoveAllChildren()
    {
        foreach (var child in _children)
        {
            child.Detach();
        }

        _children.Clear();
    }

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        AddChildrenToScene(context, offset);
    }

    protected void AddChildrenToScene(DrawingContext context, Point offset)
    {
        for (int index = 0; index < _children.Count; index++)
        {
            if (BackdropCaptureStopped)
            {
                return;
            }

            _children[index].AddToScene(context, offset);
        }
    }

    internal override void CollectBackdropFilters(ICollection<BackdropFilterLayer> filters)
    {
        foreach (Layer child in _children)
        {
            child.CollectBackdropFilters(filters);
        }
    }
}

/// <summary>
/// Connects one <see cref="LeaderLayer"/> with one or more <see cref="FollowerLayer"/> instances.
/// </summary>
/// <remarks>
/// Dart parity source: flutter/packages/flutter/lib/src/rendering/layer.dart (LayerLink).
/// </remarks>
public sealed class LayerLink
{
    private LeaderLayer? _leader;
    private RenderLeaderLayer? _renderLeader;

    public LeaderLayer? Leader => _leader;

    public Size? LeaderSize { get; set; }

    internal RenderLeaderLayer? RenderLeader => _renderLeader;

    internal void RegisterLeader(LeaderLayer leader)
    {
        if (_leader != null && !ReferenceEquals(_leader, leader))
        {
            throw new InvalidOperationException(
                "A LayerLink cannot be attached to more than one LeaderLayer at the same time.");
        }

        _leader = leader;
    }

    internal void UnregisterLeader(LeaderLayer leader)
    {
        if (ReferenceEquals(_leader, leader))
        {
            _leader = null;
        }
    }

    internal void RegisterRenderLeader(RenderLeaderLayer leader)
    {
        if (_renderLeader != null && !ReferenceEquals(_renderLeader, leader))
        {
            throw new InvalidOperationException(
                "A LayerLink cannot be attached to more than one RenderLeaderLayer at the same time.");
        }

        _renderLeader = leader;
    }

    internal void UnregisterRenderLeader(RenderLeaderLayer leader)
    {
        if (ReferenceEquals(_renderLeader, leader))
        {
            _renderLeader = null;
        }
    }
}

/// <summary>
/// A composited anchor layer followed by <see cref="FollowerLayer"/> instances sharing its link.
/// </summary>
public sealed class LeaderLayer : ContainerLayer
{
    private LayerLink _link;

    public LeaderLayer(LayerLink link, Point offset = default)
    {
        _link = link ?? throw new ArgumentNullException(nameof(link));
        Offset = offset;
    }

    public LayerLink Link
    {
        get => _link;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(_link, value))
            {
                return;
            }

            if (Parent != null)
            {
                _link.UnregisterLeader(this);
                value.RegisterLeader(this);
            }

            _link = value;
        }
    }

    public Point Offset { get; set; }

    internal override void Attach(ContainerLayer parent)
    {
        base.Attach(parent);
        _link.RegisterLeader(this);
    }

    internal override void Detach()
    {
        _link.UnregisterLeader(this);
        base.Detach();
    }

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        AddChildrenToScene(context, offset + Offset);
    }
}

/// <summary>
/// A composited layer that transforms its children into a linked leader's coordinate space.
/// </summary>
public sealed class FollowerLayer : ContainerLayer
{
    public FollowerLayer(
        LayerLink link,
        bool showWhenUnlinked = true,
        Point unlinkedOffset = default,
        Matrix? linkedTransform = null)
    {
        Link = link ?? throw new ArgumentNullException(nameof(link));
        ShowWhenUnlinked = showWhenUnlinked;
        UnlinkedOffset = unlinkedOffset;
        LinkedTransform = linkedTransform;
    }

    public LayerLink Link { get; set; }

    public bool ShowWhenUnlinked { get; set; }

    public Point UnlinkedOffset { get; set; }

    public Matrix? LinkedTransform { get; set; }

    public Matrix? GetLastTransform()
    {
        return Link.Leader != null ? LinkedTransform : null;
    }

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        Matrix? linkedTransform = GetLastTransform();
        if (!linkedTransform.HasValue)
        {
            if (ShowWhenUnlinked)
            {
                AddChildrenToScene(context, offset + UnlinkedOffset);
            }

            return;
        }

        Point sceneOffset = offset + UnlinkedOffset;
        using (context.PushTransform(Matrix.CreateTranslation(sceneOffset.X, sceneOffset.Y)))
        using (context.PushTransform(linkedTransform.Value))
        {
            AddChildrenToScene(context, default);
        }
    }
}

public sealed class MagnifierLayer : ContainerLayer
{
    public Rect LensRect { get; set; }

    public Point FocalPointOffset { get; set; }

    public double MagnificationScale { get; set; } = 1.0;

    public MagnifierDecoration Decoration { get; set; } = new();

    public Clip ClipBehavior { get; set; } = Clip.None;

    internal override bool ContainsMagnifier => true;

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        if (CapturingMagnifierBackdrop || CapturingBackdrop)
        {
            return;
        }

        Rect lensRect = new(LensRect.Position + offset, LensRect.Size);
        if (lensRect.Width <= 0 || lensRect.Height <= 0)
        {
            return;
        }

        double radius = Decoration.Shape.Shape == BoxShape.Circle
            ? Math.Min(lensRect.Width, lensRect.Height) / 2.0
            : Math.Min(
                Decoration.Shape.BorderRadius.Radius,
                Math.Min(lensRect.Width, lensRect.Height) / 2.0);
        using (context.PushOpacity(Math.Clamp(Decoration.Opacity, 0.0, 1.0)))
        {
            using (PushRoundedRectClip(context, lensRect, radius))
            {
                DrawMagnifiedBackdrop(context, lensRect);
                AddChildrenToScene(context, offset);
            }

            DrawDecoration(context, lensRect, radius);
        }
    }

    private void DrawMagnifiedBackdrop(DrawingContext context, Rect lensRect)
    {
        BackdropCapture? backdrop = MagnifierBackdrop;
        if (backdrop == null)
        {
            return;
        }

        double scale = MagnificationScale;
        double absoluteScale = Math.Abs(scale);
        if (absoluteScale <= double.Epsilon)
        {
            return;
        }

        Point focalPoint = lensRect.Center + FocalPointOffset;
        var sourceSize = new Size(lensRect.Width / absoluteScale, lensRect.Height / absoluteScale);
        var sourceRect = new Rect(
            focalPoint.X - (sourceSize.Width / 2.0),
            focalPoint.Y - (sourceSize.Height / 2.0),
            sourceSize.Width,
            sourceSize.Height);
        if (scale > 0)
        {
            context.DrawImage(backdrop.Image, sourceRect, lensRect);
            return;
        }

        using (context.PushTransform(
                   Matrix.CreateTranslation(lensRect.Center.X, lensRect.Center.Y)
                   * Matrix.CreateScale(-1, -1)
                   * Matrix.CreateTranslation(-lensRect.Center.X, -lensRect.Center.Y)))
        {
            context.DrawImage(backdrop.Image, sourceRect, lensRect);
        }
    }

    private void DrawDecoration(DrawingContext context, Rect lensRect, double radius)
    {
        BoxShadows shadows = Decoration.Shadows ?? default;
        BorderSide? side = Decoration.Shape.Side;
        IPen? pen = side is { Style: BorderStyle.Solid, Width: > 0 }
            ? new Pen(new SolidColorBrush(side.Value.Color), side.Value.Width)
            : null;

        if (shadows.Count == 0 && pen == null)
        {
            return;
        }

        DrawingContext.PushedState? clip = null;
        try
        {
            if (ClipBehavior != Clip.None)
            {
                var outer = lensRect.Inflate(Math.Max(lensRect.Width, lensRect.Height));
                var geometry = new CombinedGeometry(
                    GeometryCombineMode.Exclude,
                    new RectangleGeometry(outer),
                    new RectangleGeometry(
                        new Rect(
                            lensRect.X + (pen?.Thickness ?? 0),
                            lensRect.Y + (pen?.Thickness ?? 0),
                            Math.Max(0, lensRect.Width - ((pen?.Thickness ?? 0) * 2)),
                            Math.Max(0, lensRect.Height - ((pen?.Thickness ?? 0) * 2))),
                        radius,
                        radius));
                clip = context.PushGeometryClip(geometry);
            }

            context.DrawRectangle(Brushes.Transparent, pen, new RoundedRect(lensRect, radius), shadows);
        }
        finally
        {
            clip?.Dispose();
        }
    }
}

public class OffsetLayer : ContainerLayer
{
    public Point Offset { get; set; }

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        base.AddToScene(context, offset + Offset);
    }
}

public sealed class OpacityOffsetLayer : OffsetLayer
{
    private double _opacity = 1.0;

    public double Opacity
    {
        get => _opacity;
        set => _opacity = Math.Clamp(value, 0.0, 1.0);
    }

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        using (context.PushOpacity(Opacity))
        {
            base.AddToScene(context, offset);
        }
    }
}

public sealed class ColorFilterLayer : ContainerLayer
{
    private WriteableBitmap? _filteredBitmap;

    public ColorFilter? ColorFilter { get; set; }

    public Rect FilterBounds { get; set; }

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        if (ColorFilter is null)
        {
            AddChildrenToScene(context, offset);
            return;
        }

        _filteredBitmap?.Dispose();
        _filteredBitmap = FilterLayerRasterizer.DrawColorFiltered(
            context,
            drawingContext => AddChildrenToScene(drawingContext, offset),
            ColorFilter,
            new Rect(FilterBounds.Position + offset, FilterBounds.Size));
    }

    internal override void Detach()
    {
        _filteredBitmap?.Dispose();
        _filteredBitmap = null;
        base.Detach();
    }
}

public sealed class ImageFilterLayer : OffsetLayer
{
    private WriteableBitmap? _filteredBitmap;

    public ImageFilter? ImageFilter { get; set; }

    public Rect FilterBounds { get; set; }

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        if (ImageFilter is null)
        {
            base.AddToScene(context, offset);
            return;
        }

        Point sceneOffset = offset + Offset;
        _filteredBitmap?.Dispose();
        _filteredBitmap = FilterLayerRasterizer.DrawImageFiltered(
            context,
            drawingContext => AddChildrenToScene(drawingContext, default),
            ImageFilter,
            sceneOffset,
            FilterBounds);
    }

    internal override void Detach()
    {
        _filteredBitmap?.Dispose();
        _filteredBitmap = null;
        base.Detach();
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/rendering/layer.dart (BackdropFilterLayer)
public sealed class BackdropKey
{
    private static int _nextKey;

    public BackdropKey()
    {
        Id = Interlocked.Increment(ref _nextKey) - 1;
    }

    internal int Id { get; }
}

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

public sealed class BackdropFilterLayer : ContainerLayer
{
    private WriteableBitmap? _filteredBitmap;

    internal BackdropCapture? Backdrop { get; set; }

    public ImageFilter? ImageFilter { get; set; }

    public BlendMode BlendMode { get; set; } = BlendMode.SourceOver;

    public BackdropKey? BackdropKey { get; set; }

    internal override bool ContainsBackdropFilter => true;

    internal override void CollectBackdropFilters(ICollection<BackdropFilterLayer> filters)
    {
        filters.Add(this);
        base.CollectBackdropFilters(filters);
    }

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        if (IsBackdropCaptureTarget(this))
        {
            StopBackdropCapture();
            return;
        }

        if (BackdropCaptureStopped)
        {
            return;
        }

        if (ImageFilter != null && Backdrop != null)
        {
            _filteredBitmap?.Dispose();
            _filteredBitmap = FilterLayerRasterizer.DrawBackdropFiltered(
                context,
                Backdrop.Image,
                Backdrop.Bounds,
                ImageFilter,
                BlendMode);
        }

        AddChildrenToScene(context, offset);
    }

    internal override void Detach()
    {
        _filteredBitmap?.Dispose();
        _filteredBitmap = null;
        Backdrop = null;
        base.Detach();
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/rendering/layer.dart (ShaderMaskLayer)
public sealed class ShaderMaskLayer : ContainerLayer
{
    private WriteableBitmap? _maskedBitmap;

    public IBrush? Shader { get; set; }

    public Rect MaskRect { get; set; }

    public BlendMode BlendMode { get; set; } = BlendMode.Modulate;

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        if (Shader is null)
        {
            AddChildrenToScene(context, offset);
            return;
        }

        Rect sceneMaskRect = new(MaskRect.Position + offset, MaskRect.Size);
        _maskedBitmap?.Dispose();
        _maskedBitmap = FilterLayerRasterizer.DrawShaderMasked(
            context,
            drawingContext => AddChildrenToScene(drawingContext, offset),
            Shader,
            BlendMode,
            sceneMaskRect);
    }

    internal override void Detach()
    {
        _maskedBitmap?.Dispose();
        _maskedBitmap = null;
        base.Detach();
    }
}

public sealed class TransformOffsetLayer : OffsetLayer
{
    public Matrix Transform { get; set; } = Matrix.Identity;
    public FilterQuality? FilterQuality { get; set; }

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        var sceneOffset = offset + Offset;
        using IDisposable? renderOptions = FilterQuality.HasValue
            ? context.PushRenderOptions(new RenderOptions
            {
                BitmapInterpolationMode = FilterQuality.Value switch
                {
                    Rendering.FilterQuality.None => BitmapInterpolationMode.None,
                    Rendering.FilterQuality.Low => BitmapInterpolationMode.LowQuality,
                    Rendering.FilterQuality.High => BitmapInterpolationMode.HighQuality,
                    _ => BitmapInterpolationMode.MediumQuality,
                }
            })
            : null;
        using (context.PushTransform(Matrix.CreateTranslation(sceneOffset.X, sceneOffset.Y)))
        using (context.PushTransform(Transform))
        {
            AddChildrenToScene(context, new Point(0, 0));
        }
    }
}

public sealed class ClipRectOffsetLayer : OffsetLayer
{
    public Rect ClipRect { get; set; }

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        var sceneOffset = offset + Offset;
        var translatedRect = new Rect(ClipRect.Position + sceneOffset, ClipRect.Size);
        using (context.PushClip(translatedRect))
        {
            AddChildrenToScene(context, sceneOffset);
        }
    }
}

public sealed class ClipRRectOffsetLayer : OffsetLayer
{
    public Rect ClipRect { get; set; }

    public BorderRadius BorderRadius { get; set; } = BorderRadius.Zero;

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        var sceneOffset = offset + Offset;
        var translatedRect = new Rect(ClipRect.Position + sceneOffset, ClipRect.Size);
        using (PushRoundedRectClip(context, translatedRect, ClampRadius(translatedRect, BorderRadius)))
        {
            AddChildrenToScene(context, sceneOffset);
        }
    }

    private static double ClampRadius(Rect clipRect, BorderRadius borderRadius)
    {
        double maxRadius = Math.Max(0, Math.Min(clipRect.Width, clipRect.Height) / 2);
        return Math.Min(borderRadius.Radius, maxRadius);
    }
}

public sealed class ClipRectLayer : ContainerLayer
{
    public Rect ClipRect { get; set; }

    public Clip ClipBehavior { get; set; } = Clip.HardEdge;

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        var translatedRect = new Rect(ClipRect.Position + offset, ClipRect.Size);
        using IDisposable renderOptions = context.PushRenderOptions(new RenderOptions
        {
            EdgeMode = ClipBehavior == Clip.HardEdge ? EdgeMode.Aliased : EdgeMode.Antialias,
        });
        using (context.PushClip(translatedRect))
        {
            base.AddToScene(context, offset);
        }
    }
}

public sealed class ClipRRectLayer : ContainerLayer
{
    public Rect ClipRect { get; set; }

    public BorderRadius BorderRadius { get; set; } = BorderRadius.Zero;

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        var translatedRect = new Rect(ClipRect.Position + offset, ClipRect.Size);
        using (PushRoundedRectClip(context, translatedRect, ClampRadius(translatedRect, BorderRadius)))
        {
            base.AddToScene(context, offset);
        }
    }

    private static double ClampRadius(Rect clipRect, BorderRadius borderRadius)
    {
        double maxRadius = Math.Max(0, Math.Min(clipRect.Width, clipRect.Height) / 2);
        return Math.Min(borderRadius.Radius, maxRadius);
    }
}

public sealed class ClipGeometryLayer : ContainerLayer
{
    public Geometry Geometry { get; set; } = new RectangleGeometry();

    public Clip ClipBehavior { get; set; } = Clip.AntiAlias;

    public Point GeometryOffset { get; set; }

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        Point clipOffset = offset + GeometryOffset;
        using IDisposable renderOptions = context.PushRenderOptions(new RenderOptions
        {
            EdgeMode = ClipBehavior == Clip.HardEdge ? EdgeMode.Aliased : EdgeMode.Antialias,
        });
        using (context.PushTransform(Matrix.CreateTranslation(clipOffset.X, clipOffset.Y)))
        using (context.PushGeometryClip(Geometry))
        using (context.PushTransform(Matrix.CreateTranslation(-GeometryOffset.X, -GeometryOffset.Y)))
        {
            base.AddToScene(context, new Point(0, 0));
        }
    }
}

public sealed class TransformLayer : ContainerLayer
{
    public Matrix Transform { get; set; } = Matrix.Identity;

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        using (context.PushTransform(Matrix.CreateTranslation(offset.X, offset.Y)))
        using (context.PushTransform(Transform))
        {
            base.AddToScene(context, new Point(0, 0));
        }
    }
}

public sealed class OpacityLayer : ContainerLayer
{
    private double _opacity = 1.0;

    public double Opacity
    {
        get => _opacity;
        set => _opacity = Math.Clamp(value, 0.0, 1.0);
    }

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        using (context.PushOpacity(Opacity))
        {
            base.AddToScene(context, offset);
        }
    }
}

public sealed class PictureLayer : Layer
{
    private readonly List<Action<DrawingContext, Point>> _commands = [];

    public bool IsEmpty => _commands.Count == 0;

    public void AddDrawCommand(Action<DrawingContext, Point> command)
    {
        _commands.Add(command);
    }

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        for (int index = 0; index < _commands.Count; index++)
        {
            _commands[index](context, offset);
        }
    }
}
