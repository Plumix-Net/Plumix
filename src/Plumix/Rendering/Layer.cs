using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/layer.dart (approximate)

namespace Plumix.Rendering;

public abstract class Layer
{
    [ThreadStatic]
    private static Drawing? _magnifierBackdrop;

    [ThreadStatic]
    private static bool _capturingMagnifierBackdrop;

    public ContainerLayer? Parent { get; private set; }

    internal virtual bool ContainsMagnifier => false;

    internal static Drawing? MagnifierBackdrop => _magnifierBackdrop;

    internal static bool CapturingMagnifierBackdrop => _capturingMagnifierBackdrop;

    internal static void BeginMagnifierBackdropCapture()
    {
        _capturingMagnifierBackdrop = true;
        _magnifierBackdrop = null;
    }

    internal static void EndMagnifierBackdropCapture(Drawing drawing)
    {
        _capturingMagnifierBackdrop = false;
        _magnifierBackdrop = drawing;
    }

    internal static void ClearMagnifierBackdrop()
    {
        _capturingMagnifierBackdrop = false;
        _magnifierBackdrop = null;
    }

    internal static DrawingContext.PushedState PushRoundedRectClip(
        DrawingContext context,
        Rect rect,
        double radius)
    {
        double clampedRadius = Math.Min(Math.Max(0, radius), Math.Min(rect.Width, rect.Height) / 2.0);
        if (CapturingMagnifierBackdrop)
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
}

public class ContainerLayer : Layer
{
    private readonly List<Layer> _children = [];

    public IReadOnlyList<Layer> Children => _children;

    internal override bool ContainsMagnifier => _children.Any(static child => child.ContainsMagnifier);

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
            _children[index].AddToScene(context, offset);
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
        if (CapturingMagnifierBackdrop)
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
        Drawing? backdrop = MagnifierBackdrop;
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
        var image = new DrawingImage(backdrop)
        {
            Viewbox = backdrop.GetBounds(),
        };

        if (scale > 0)
        {
            context.DrawImage(image, sourceRect, lensRect);
            return;
        }

        using (context.PushTransform(
                   Matrix.CreateTranslation(lensRect.Center.X, lensRect.Center.Y)
                   * Matrix.CreateScale(-1, -1)
                   * Matrix.CreateTranslation(-lensRect.Center.X, -lensRect.Center.Y)))
        {
            context.DrawImage(image, sourceRect, lensRect);
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

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        var translatedRect = new Rect(ClipRect.Position + offset, ClipRect.Size);
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
