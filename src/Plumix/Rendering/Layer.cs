using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Plumix.Gestures;
using Plumix.UI;
using Plumix.Foundation;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/layer.dart (approximate)

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/layer.dart
public sealed class LayerHandle<T> where T : Layer
{
    private T? _layer;

    public LayerHandle(T? layer = null)
    {
        _layer = layer;
        if (_layer != null)
        {
            _layer.Ref();
        }
    }

    public T? Layer
    {
        get => _layer;
        set
        {
            if (value?.DebugDisposed == true)
            {
                throw new AssertionError($"Attempted to create a handle to an already disposed layer: {value}.");
            }

            if (ReferenceEquals(_layer, value))
            {
                return;
            }

            _layer?.Unref();
            _layer = value;
            _layer?.Ref();
        }
    }

    /// <inheritdoc />
    public override string ToString() =>
        _layer is null ? "LayerHandle(DISPOSED)" : $"LayerHandle({_layer})";
}

// Dart parity source: flutter/packages/flutter/lib/src/rendering/layer.dart
public sealed record AnnotationEntry<T>(
    T Annotation,
    Point LocalPosition)
    where T : notnull;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/layer.dart
public sealed class AnnotationResult<T> where T : notnull
{
    private readonly List<AnnotationEntry<T>> _entries = [];

    public IReadOnlyList<AnnotationEntry<T>> Entries => _entries;

    public IEnumerable<T> Annotations => _entries.Select(static entry => entry.Annotation);

    public void Add(AnnotationEntry<T> entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _entries.Add(entry);
    }
}

public abstract class Layer : DiagnosticableTree
{
    internal readonly LayerHandle<Layer> _parentHandle = new();
    private int _refCount;
    private bool _debugDisposed;
    private object? _owner;
    private IDisposable? _engineLayer;

    [ThreadStatic]
    private static BackdropCapture? _magnifierBackdrop;

    [ThreadStatic]
    private static bool _capturingMagnifierBackdrop;

    [ThreadStatic]
    private static BackdropFilterLayer? _backdropCaptureTarget;

    [ThreadStatic]
    private static bool _backdropCaptureStopped;

    public ContainerLayer? Parent { get; internal set; }

    public object? Owner => _owner;

    public bool Attached => _owner != null;

    public bool DebugDisposed => _debugDisposed;

    public int DebugHandleCount => _refCount;

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
        if (CapturingMagnifierBackdrop || CapturingBackdrop)
        {
            // Avalonia's DrawingGroup recording context does not implement PushClip(RoundedRect), but its
            // geometry-clip path records the equivalent rounded rectangle correctly.
            return context.PushGeometryClip(new RectangleGeometry(rect, clampedRadius, clampedRadius));
        }

        return context.PushClip(new RoundedRect(rect, clampedRadius));
    }

    internal static DrawingContext.PushedState PushRoundedRectClip(
        DrawingContext context,
        Rect rect,
        BorderRadius borderRadius)
    {
        double maxX = Math.Max(0.0, rect.Width / 2.0);
        double maxY = Math.Max(0.0, rect.Height / 2.0);
        var topLeft = ClampRadius(borderRadius.TopLeftRadius, maxX, maxY);
        var topRight = ClampRadius(borderRadius.TopRightRadius, maxX, maxY);
        var bottomRight = ClampRadius(borderRadius.BottomRightRadius, maxX, maxY);
        var bottomLeft = ClampRadius(borderRadius.BottomLeftRadius, maxX, maxY);
        if (CapturingMagnifierBackdrop || CapturingBackdrop)
        {
            double fallbackX = Math.Max(
                Math.Max(topLeft.X, topRight.X),
                Math.Max(bottomRight.X, bottomLeft.X));
            double fallbackY = Math.Max(
                Math.Max(topLeft.Y, topRight.Y),
                Math.Max(bottomRight.Y, bottomLeft.Y));
            return context.PushGeometryClip(new RectangleGeometry(rect, fallbackX, fallbackY));
        }

        return context.PushClip(new RoundedRect(
            rect,
            new Vector(topLeft.X, topLeft.Y),
            new Vector(topRight.X, topRight.Y),
            new Vector(bottomRight.X, bottomRight.Y),
            new Vector(bottomLeft.X, bottomLeft.Y)));
    }

    internal static bool ContainsRoundedRect(
        Rect rect,
        BorderRadius borderRadius,
        Point position)
    {
        if (!ContainsRect(rect, position))
        {
            return false;
        }

        bool left = position.X < rect.Center.X;
        bool top = position.Y < rect.Center.Y;
        Radius corner = (left, top) switch
        {
            (true, true) => borderRadius.TopLeftRadius,
            (false, true) => borderRadius.TopRightRadius,
            (false, false) => borderRadius.BottomRightRadius,
            _ => borderRadius.BottomLeftRadius,
        };
        Radius radius = ClampRadius(corner, rect.Width / 2.0, rect.Height / 2.0);
        if (radius.X <= 0.0
            || radius.Y <= 0.0
            || (position.X >= rect.Left + radius.X && position.X <= rect.Right - radius.X)
            || (position.Y >= rect.Top + radius.Y && position.Y <= rect.Bottom - radius.Y))
        {
            return true;
        }

        double centerX = left ? rect.Left + radius.X : rect.Right - radius.X;
        double centerY = top ? rect.Top + radius.Y : rect.Bottom - radius.Y;
        double dx = (position.X - centerX) / radius.X;
        double dy = (position.Y - centerY) / radius.Y;
        return (dx * dx) + (dy * dy) <= 1.0;
    }

    internal static Radius ClampRadius(Radius radius, double maxX, double maxY)
    {
        if (radius.X * radius.Y == 0.0)
        {
            return Radius.Zero;
        }

        return Radius.Elliptical(
            Math.Min(radius.X, Math.Max(0.0, maxX)),
            Math.Min(radius.Y, Math.Max(0.0, maxY)));
    }

    internal static bool ContainsRect(Rect rect, Point position)
    {
        return position.X >= rect.Left
               && position.X < rect.Right
               && position.Y >= rect.Top
               && position.Y < rect.Bottom;
    }

    public virtual void Attach(object owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (_owner != null)
        {
            throw new AssertionError("A layer cannot be attached to more than one owner.");
        }

        _owner = owner;
    }

    public virtual void Detach()
    {
        if (_owner == null)
        {
            throw new AssertionError("A detached layer cannot be detached again.");
        }

        _owner = null;
    }

    public virtual void Remove()
    {
        Parent?.Remove(this);
    }

    protected internal virtual void Dispose()
    {
        if (_debugDisposed)
        {
            throw new AssertionError(
                "Layers must only be disposed once. This is typically handled by LayerHandle.");
        }

        if (_refCount != 0)
        {
            throw new AssertionError(
                $"Do not directly call Dispose on a {GetType().Name}. Instead, use LayerHandle.Layer = null.");
        }

        EngineLayer = null;
        _debugDisposed = true;
    }

    protected internal IDisposable? EngineLayer
    {
        get => _engineLayer;
        set
        {
            if (_debugDisposed)
            {
                throw new AssertionError("A disposed layer cannot retain an engine layer.");
            }

            if (ReferenceEquals(_engineLayer, value))
            {
                return;
            }

            _engineLayer?.Dispose();
            _engineLayer = value;
        }
    }

    internal void Ref()
    {
        _refCount += 1;
    }

    internal void Unref()
    {
        if (_refCount <= 0)
        {
            throw new AssertionError("A layer handle released a layer with no references.");
        }

        _refCount -= 1;
        if (_refCount == 0)
        {
            Dispose();
        }
    }

    internal abstract void AddToScene(DrawingContext context, Point offset);

    internal virtual void CollectBackdropFilters(ICollection<BackdropFilterLayer> filters)
    {
    }

    protected internal virtual bool FindAnnotations<T>(
        AnnotationResult<T> result,
        Point localPosition,
        bool onlyFirst)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        return false;
    }

    public T? Find<T>(Point localPosition)
        where T : notnull
    {
        var result = new AnnotationResult<T>();
        FindAnnotations(result, localPosition, onlyFirst: true);
        return result.Entries.Count == 0 ? default : result.Entries[0].Annotation;
    }

    public AnnotationResult<T> FindAllAnnotations<T>(Point localPosition)
        where T : notnull
    {
        var result = new AnnotationResult<T>();
        FindAnnotations(result, localPosition, onlyFirst: false);
        return result;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<object>(
            "owner",
            Owner,
            defaultValue: DiagnosticsDefaults.NullValue,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<object>(
            "creator",
            DebugCreator,
            defaultValue: DiagnosticsDefaults.NullValue,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<IDisposable>(
            "engine layer",
            EngineLayer,
            defaultValue: DiagnosticsDefaults.NullValue,
            level: DiagnosticLevel.Debug));
        properties.Add(new IntProperty("handles", DebugHandleCount, level: DiagnosticLevel.Debug));
    }

    /// <inheritdoc />
    public override string ToStringShort()
    {
        string description = base.ToStringShort();
        return Attached ? description : $"{description} DETACHED";
    }

    /// The object responsible for creating this layer.
    ///
    /// Used in debug messages.
    public object? DebugCreator { get; set; }

}

public class ContainerLayer : Layer
{
    private readonly List<Layer> _children = [];

    public IReadOnlyList<Layer> Children => _children;

    /// <summary>Whether this layer has any children.</summary>
    /// <remarks>Flutter's <c>ContainerLayer.hasChildren</c>.</remarks>
    public bool HasChildren => _children.Count > 0;

    internal override bool ContainsMagnifier => _children.Any(static child => child.ContainsMagnifier);

    internal override bool ContainsBackdropFilter => _children.Any(static child => child.ContainsBackdropFilter);

    public void Append(Layer child)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (child.Parent != null || child.Attached)
        {
            throw new AssertionError("A layer must be detached and parentless before it can be appended.");
        }

        child.Parent = this;
        _children.Add(child);
        child._parentHandle.Layer = child;
        if (Attached)
        {
            child.Attach(Owner!);
        }
    }

    public void Remove(Layer child)
    {
        if (_children.Remove(child))
        {
            child.Parent = null;
            if (child.Attached)
            {
                child.Detach();
            }

            child._parentHandle.Layer = null;
        }
    }

    public void RemoveAllChildren()
    {
        foreach (Layer child in _children)
        {
            child.Parent = null;
            if (child.Attached)
            {
                child.Detach();
            }

            child._parentHandle.Layer = null;
        }

        _children.Clear();
    }

    public override void Attach(object owner)
    {
        base.Attach(owner);
        foreach (Layer child in _children)
        {
            child.Attach(owner);
        }
    }

    public override void Detach()
    {
        base.Detach();
        foreach (Layer child in _children)
        {
            child.Detach();
        }
    }

    protected internal override void Dispose()
    {
        RemoveAllChildren();
        base.Dispose();
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

    protected internal override bool FindAnnotations<T>(
        AnnotationResult<T> result,
        Point localPosition,
        bool onlyFirst)
    {
        ArgumentNullException.ThrowIfNull(result);
        for (int index = _children.Count - 1; index >= 0; index--)
        {
            bool isAbsorbed = _children[index].FindAnnotations(result, localPosition, onlyFirst);
            if (isAbsorbed)
            {
                return true;
            }

            if (onlyFirst && result.Entries.Count > 0)
            {
                return false;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren()
    {
        var children = new List<DiagnosticsNode>();
        for (int index = 0; index < _children.Count; index++)
        {
            children.Add(_children[index].ToDiagnosticsNode(name: $"child {index + 1}"));
        }

        return children;
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

            if (Attached)
            {
                _link.UnregisterLeader(this);
                value.RegisterLeader(this);
            }

            _link = value;
        }
    }

    public Point Offset { get; set; }

    public override void Attach(object owner)
    {
        base.Attach(owner);
        _link.RegisterLeader(this);
    }

    public override void Detach()
    {
        _link.UnregisterLeader(this);
        base.Detach();
    }

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        AddChildrenToScene(context, offset + Offset);
    }

    protected internal override bool FindAnnotations<T>(
        AnnotationResult<T> result,
        Point localPosition,
        bool onlyFirst)
    {
        return base.FindAnnotations(result, localPosition - Offset, onlyFirst);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<Point>("offset", Offset));
        properties.Add(new DiagnosticsProperty<LayerLink>("link", Link));
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
        Matrix4? linkedTransform = null)
    {
        Link = link ?? throw new ArgumentNullException(nameof(link));
        ShowWhenUnlinked = showWhenUnlinked;
        UnlinkedOffset = unlinkedOffset;
        LinkedTransform = linkedTransform;
    }

    public LayerLink Link { get; set; }

    public bool ShowWhenUnlinked { get; set; }

    public Point UnlinkedOffset { get; set; }

    public Matrix4? LinkedTransform { get; set; }

    public Matrix4? GetLastTransform()
    {
        return Link.Leader != null ? LinkedTransform : null;
    }

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        Matrix4? linkedTransform = GetLastTransform();
        if (linkedTransform is null)
        {
            if (ShowWhenUnlinked)
            {
                AddChildrenToScene(context, offset + UnlinkedOffset);
            }

            return;
        }

        Point sceneOffset = offset + UnlinkedOffset;
        using (context.PushTransform(Matrix.CreateTranslation(sceneOffset.X, sceneOffset.Y)))
        using (context.PushTransform(linkedTransform.ToAvaloniaMatrix()))
        {
            AddChildrenToScene(context, default);
        }
    }

    protected internal override bool FindAnnotations<T>(
        AnnotationResult<T> result,
        Point localPosition,
        bool onlyFirst)
    {
        Matrix4? transform = GetLastTransform();
        if (transform is null)
        {
            return ShowWhenUnlinked
                && base.FindAnnotations(result, localPosition - UnlinkedOffset, onlyFirst);
        }

        Matrix4? inverse = Matrix4.TryInvert(PointerEventUtils.RemovePerspectiveTransform(transform));
        if (inverse is null)
        {
            return false;
        }

        Point transformedPosition = MatrixUtils.TransformPoint(inverse, localPosition - UnlinkedOffset);
        return base.FindAnnotations(result, transformedPosition, onlyFirst);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<LayerLink>("link", Link));
        properties.Add(new TransformProperty(
            "transform",
            GetLastTransform(),
            defaultValue: DiagnosticsDefaults.NullValue));
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/rendering/layer.dart (AnnotatedRegionLayer).
public sealed class AnnotatedRegionLayer<T> : ContainerLayer where T : notnull
{
    public AnnotatedRegionLayer(
        T value,
        Size? size = null,
        Point? offset = null,
        bool opaque = false)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
        Size = size;
        Offset = offset ?? default;
        Opaque = opaque;
    }

    public T Value { get; }

    public Size? Size { get; }

    public Point Offset { get; }

    public bool Opaque { get; }

    protected internal override bool FindAnnotations<S>(
        AnnotationResult<S> result,
        Point localPosition,
        bool onlyFirst)
    {
        bool isAbsorbed = base.FindAnnotations(result, localPosition, onlyFirst);
        if (onlyFirst && result.Entries.Count > 0)
        {
            return isAbsorbed;
        }

        if (Size.HasValue && !ContainsRect(new Rect(Offset, Size.Value), localPosition))
        {
            return isAbsorbed;
        }

        if (typeof(T) == typeof(S))
        {
            object untypedValue = Value;
            var typedValue = (S)untypedValue;
            result.Add(new AnnotationEntry<S>(
                typedValue,
                localPosition - Offset));
            isAbsorbed |= Opaque;
        }

        return isAbsorbed;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<T>("value", Value));
        properties.Add(new DiagnosticsProperty<Size?>("size", Size, defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DiagnosticsProperty<Point>("offset", Offset, defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DiagnosticsProperty<bool>("opaque", Opaque, defaultValue: false));
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

        BorderRadius borderRadius = ResolveBorderRadius(lensRect);
        using (context.PushOpacity(Math.Clamp(Decoration.Opacity, 0.0, 1.0)))
        {
            using (PushRoundedRectClip(context, lensRect, borderRadius))
            {
                DrawMagnifiedBackdrop(context, lensRect);
                AddChildrenToScene(context, offset);
            }

            DrawDecoration(context, lensRect, borderRadius);
        }
    }

    /// <summary>
    /// Resolves the decoration shape to the per-corner radii the lens is clipped and stroked with.
    /// Each corner keeps its own (possibly elliptical) radius, clamped to half the lens so that
    /// neighbouring corners cannot overlap.
    /// </summary>
    private BorderRadius ResolveBorderRadius(Rect lensRect)
    {
        double maxX = lensRect.Width / 2.0;
        double maxY = lensRect.Height / 2.0;
        switch (Decoration.Shape)
        {
            case CircleBorder or StadiumBorder:
                return BorderRadius.Circular(Math.Min(maxX, maxY));
            case RoundedRectangleBorder rounded:
                BorderRadius resolved = rounded.BorderRadius.Resolve(Plumix.UI.TextDirection.Ltr);
                return new BorderRadius(
                    ClampRadius(resolved.TopLeftRadius, maxX, maxY),
                    ClampRadius(resolved.TopRightRadius, maxX, maxY),
                    ClampRadius(resolved.BottomRightRadius, maxX, maxY),
                    ClampRadius(resolved.BottomLeftRadius, maxX, maxY));
            default:
                return BorderRadius.Zero;
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

    private void DrawDecoration(DrawingContext context, Rect lensRect, BorderRadius borderRadius)
    {
        BoxShadows shadows = Decoration.Shadows.ToAvalonia();
        BorderSide side = Decoration.Shape is OutlinedBorder outlined ? outlined.Side : BorderSide.None;
        IPen? pen = side is { Style: BorderStyle.Solid, Width: > 0 }
            ? new Pen(new SolidColorBrush(side.Color), side.Width)
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
                double inset = pen?.Thickness ?? 0.0;
                var outer = lensRect.Inflate(Math.Max(lensRect.Width, lensRect.Height));
                var geometry = new CombinedGeometry(
                    GeometryCombineMode.Exclude,
                    new RectangleGeometry(outer),
                    new RectangleGeometry(
                        new Rect(
                            lensRect.X + inset,
                            lensRect.Y + inset,
                            Math.Max(0, lensRect.Width - (inset * 2)),
                            Math.Max(0, lensRect.Height - (inset * 2))),
                        Math.Max(0, LargestRadiusX(borderRadius) - inset),
                        Math.Max(0, LargestRadiusY(borderRadius) - inset)));
                clip = context.PushGeometryClip(geometry);
            }

            context.DrawRectangle(Brushes.Transparent, pen, ToRoundedRect(lensRect, borderRadius), shadows);
        }
        finally
        {
            clip?.Dispose();
        }
    }

    private static RoundedRect ToRoundedRect(Rect rect, BorderRadius borderRadius)
    {
        return new RoundedRect(
            rect,
            new Vector(borderRadius.TopLeftRadius.X, borderRadius.TopLeftRadius.Y),
            new Vector(borderRadius.TopRightRadius.X, borderRadius.TopRightRadius.Y),
            new Vector(borderRadius.BottomRightRadius.X, borderRadius.BottomRightRadius.Y),
            new Vector(borderRadius.BottomLeftRadius.X, borderRadius.BottomLeftRadius.Y));
    }

    private static double LargestRadiusX(BorderRadius borderRadius)
    {
        return Math.Max(
            Math.Max(borderRadius.TopLeftRadius.X, borderRadius.TopRightRadius.X),
            Math.Max(borderRadius.BottomRightRadius.X, borderRadius.BottomLeftRadius.X));
    }

    private static double LargestRadiusY(BorderRadius borderRadius)
    {
        return Math.Max(
            Math.Max(borderRadius.TopLeftRadius.Y, borderRadius.TopRightRadius.Y),
            Math.Max(borderRadius.BottomRightRadius.Y, borderRadius.BottomLeftRadius.Y));
    }
}

public class OffsetLayer : ContainerLayer
{
    public Point Offset { get; set; }

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        base.AddToScene(context, offset + Offset);
    }

    protected internal override bool FindAnnotations<T>(
        AnnotationResult<T> result,
        Point localPosition,
        bool onlyFirst)
    {
        return FindAnnotationsInChildren(result, localPosition - Offset, onlyFirst);
    }

    protected bool FindAnnotationsInChildren<T>(
        AnnotationResult<T> result,
        Point localPosition,
        bool onlyFirst)
        where T : notnull
    {
        return base.FindAnnotations(result, localPosition, onlyFirst);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<Point>("offset", Offset));
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/rendering/layer.dart
public sealed class OpacityLayer : OffsetLayer
{
    /// <summary>The amount to multiply into the alpha channel, from 0 (transparent) to 255 (opaque).</summary>
    public int? Alpha { get; set; }

    /// <summary>
    /// Plumix-only view of <see cref="Alpha"/> as a 0..1 fraction, for the render objects and debug
    /// flags that carry an opacity rather than Flutter's 8-bit alpha.
    /// </summary>
    public double Opacity
    {
        get => (Alpha ?? 255) / 255.0;
        set => Alpha = (int)Math.Round(Math.Clamp(value, 0.0, 1.0) * 255.0);
    }

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        if (Constants.KDebugMode && RenderingDebug.DisableOpacityLayers)
        {
            base.AddToScene(context, offset);
            return;
        }

        using (context.PushOpacity(Opacity))
        {
            base.AddToScene(context, offset);
        }
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<int?>("alpha", Alpha));
        properties.Add(new DoubleProperty("opacity", Opacity));
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

    public override void Detach()
    {
        _filteredBitmap?.Dispose();
        _filteredBitmap = null;
        base.Detach();
    }

    protected internal override void Dispose()
    {
        _filteredBitmap?.Dispose();
        _filteredBitmap = null;
        base.Dispose();
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<ColorFilter>("colorFilter", ColorFilter));
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

    public override void Detach()
    {
        _filteredBitmap?.Dispose();
        _filteredBitmap = null;
        base.Detach();
    }

    protected internal override void Dispose()
    {
        _filteredBitmap?.Dispose();
        _filteredBitmap = null;
        base.Dispose();
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<ImageFilter>("imageFilter", ImageFilter));
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

    public override void Detach()
    {
        _filteredBitmap?.Dispose();
        _filteredBitmap = null;
        Backdrop = null;
        base.Detach();
    }

    protected internal override void Dispose()
    {
        _filteredBitmap?.Dispose();
        _filteredBitmap = null;
        Backdrop = null;
        base.Dispose();
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<ImageFilter>("filter", ImageFilter));
        properties.Add(new EnumProperty<BlendMode>("blendMode", BlendMode));
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

    public override void Detach()
    {
        _maskedBitmap?.Dispose();
        _maskedBitmap = null;
        base.Detach();
    }

    protected internal override void Dispose()
    {
        _maskedBitmap?.Dispose();
        _maskedBitmap = null;
        base.Dispose();
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<IBrush>("shader", Shader));
        properties.Add(new DiagnosticsProperty<Rect>("maskRect", MaskRect));
        properties.Add(new EnumProperty<BlendMode>("blendMode", BlendMode));
    }
}

public sealed class ClipRectLayer : ContainerLayer
{
    public Rect ClipRect { get; set; }

    public Clip ClipBehavior { get; set; } = Clip.HardEdge;

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        if (Constants.KDebugMode && RenderingDebug.DisableClipLayers)
        {
            base.AddToScene(context, offset);
            return;
        }

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

    protected internal override bool FindAnnotations<T>(
        AnnotationResult<T> result,
        Point localPosition,
        bool onlyFirst)
    {
        return ContainsRect(ClipRect, localPosition)
            && base.FindAnnotations(result, localPosition, onlyFirst);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<Rect>("clipRect", ClipRect));
        properties.Add(new DiagnosticsProperty<Clip>("clipBehavior", ClipBehavior));
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/rendering/layer.dart
public sealed class ClipRRectLayer : ContainerLayer
{
    public RRect ClipRRect { get; set; }

    public Clip ClipBehavior { get; set; } = Clip.AntiAlias;

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        if (Constants.KDebugMode && RenderingDebug.DisableClipLayers)
        {
            base.AddToScene(context, offset);
            return;
        }

        using (PushRoundedRectClip(context, ClipRRect.Shift(offset)))
        {
            base.AddToScene(context, offset);
        }
    }

    protected internal override bool FindAnnotations<T>(
        AnnotationResult<T> result,
        Point localPosition,
        bool onlyFirst)
    {
        return ContainsRoundedRect(ClipRRect.Rect, ClipRRect.Radii, localPosition)
            && base.FindAnnotations(result, localPosition, onlyFirst);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<Rect>("clipRect", ClipRRect.Rect));
        properties.Add(new DiagnosticsProperty<BorderRadius>("borderRadius", ClipRRect.Radii));
        properties.Add(new DiagnosticsProperty<Clip>("clipBehavior", ClipBehavior));
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/rendering/layer.dart
public sealed class ClipRSuperellipseLayer : ContainerLayer
{
    public RSuperellipse ClipRSuperellipse { get; set; }

    public Clip ClipBehavior { get; set; } = Clip.AntiAlias;

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        if (Constants.KDebugMode && RenderingDebug.DisableClipLayers)
        {
            base.AddToScene(context, offset);
            return;
        }

        using IDisposable renderOptions = context.PushRenderOptions(new RenderOptions
        {
            EdgeMode = ClipBehavior == Clip.HardEdge ? EdgeMode.Aliased : EdgeMode.Antialias,
        });
        using (context.PushGeometryClip(ClipRSuperellipse.Shift(offset).ToPath().ToGeometry()))
        {
            base.AddToScene(context, offset);
        }
    }

    protected internal override bool FindAnnotations<T>(
        AnnotationResult<T> result,
        Point localPosition,
        bool onlyFirst)
    {
        return ClipRSuperellipse.Contains(localPosition)
            && base.FindAnnotations(result, localPosition, onlyFirst);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<Rect>("clipRect", ClipRSuperellipse.Rect));
        properties.Add(new DiagnosticsProperty<Clip>("clipBehavior", ClipBehavior));
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/rendering/layer.dart
public sealed class ClipPathLayer : ContainerLayer
{
    private Plumix.UI.Path _clipPath = new();
    private Geometry? _geometry;

    public Plumix.UI.Path ClipPath
    {
        get => _clipPath;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _clipPath = value;
            _geometry = null;
        }
    }

    public Clip ClipBehavior { get; set; } = Clip.AntiAlias;

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        if (Constants.KDebugMode && RenderingDebug.DisableClipLayers)
        {
            base.AddToScene(context, offset);
            return;
        }

        _geometry ??= _clipPath.ToGeometry();
        using IDisposable renderOptions = context.PushRenderOptions(new RenderOptions
        {
            EdgeMode = ClipBehavior == Clip.HardEdge ? EdgeMode.Aliased : EdgeMode.Antialias,
        });
        using (context.PushTransform(Matrix.CreateTranslation(offset.X, offset.Y)))
        using (context.PushGeometryClip(_geometry))
        using (context.PushTransform(Matrix.CreateTranslation(-offset.X, -offset.Y)))
        {
            base.AddToScene(context, offset);
        }
    }

    protected internal override bool FindAnnotations<T>(
        AnnotationResult<T> result,
        Point localPosition,
        bool onlyFirst)
    {
        return _clipPath.Contains(localPosition)
            && base.FindAnnotations(result, localPosition, onlyFirst);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<Rect>("clipRect", _clipPath.GetBounds()));
        properties.Add(new DiagnosticsProperty<Clip>("clipBehavior", ClipBehavior));
    }
}

/// <summary>Plumix-only clip layer for shapes the framework models as a backend geometry.</summary>
/// <remarks>
/// Dart has no counterpart: every clip layer there takes a <c>Path</c>. See
/// <c>PaintingContext.PushClipGeometry</c>.
/// </remarks>
public sealed class ClipGeometryLayer : ContainerLayer
{
    public Geometry Geometry { get; set; } = new RectangleGeometry();

    public Clip ClipBehavior { get; set; } = Clip.AntiAlias;

    public Point GeometryOffset { get; set; }

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        if (Constants.KDebugMode && RenderingDebug.DisableClipLayers)
        {
            base.AddToScene(context, offset);
            return;
        }

        Point clipOffset = offset + GeometryOffset;
        using IDisposable renderOptions = context.PushRenderOptions(new RenderOptions
        {
            EdgeMode = ClipBehavior == Clip.HardEdge ? EdgeMode.Aliased : EdgeMode.Antialias,
        });
        using (context.PushTransform(Matrix.CreateTranslation(clipOffset.X, clipOffset.Y)))
        using (context.PushGeometryClip(Geometry))
        using (context.PushTransform(Matrix.CreateTranslation(-clipOffset.X, -clipOffset.Y)))
        {
            base.AddToScene(context, offset);
        }
    }

    protected internal override bool FindAnnotations<T>(
        AnnotationResult<T> result,
        Point localPosition,
        bool onlyFirst)
    {
        return Geometry.FillContains(localPosition - GeometryOffset)
            && base.FindAnnotations(result, localPosition, onlyFirst);
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/rendering/layer.dart
public sealed class TransformLayer : ContainerLayer
{
    public Matrix4 Transform { get; set; } = Matrix4.Identity();

    /// <summary>
    /// Plumix-only: the sampling quality Dart applies through <c>ImageFilter.matrix</c> in
    /// <c>RenderTransform</c>'s filter-quality branch, which Avalonia has no image filter for.
    /// </summary>
    public FilterQuality? FilterQuality { get; set; }

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        using IDisposable? renderOptions = FilterQuality.HasValue
            ? context.PushRenderOptions(new RenderOptions
            {
                BitmapInterpolationMode = FilterQuality.Value switch
                {
                    Rendering.FilterQuality.None => BitmapInterpolationMode.None,
                    Rendering.FilterQuality.Low => BitmapInterpolationMode.LowQuality,
                    Rendering.FilterQuality.High => BitmapInterpolationMode.HighQuality,
                    _ => BitmapInterpolationMode.MediumQuality,
                },
            })
            : null;
        using (context.PushTransform(Matrix.CreateTranslation(offset.X, offset.Y)))
        using (context.PushTransform(Transform.ToAvaloniaMatrix()))
        {
            base.AddToScene(context, new Point(0, 0));
        }
    }

    protected internal override bool FindAnnotations<T>(
        AnnotationResult<T> result,
        Point localPosition,
        bool onlyFirst)
    {
        Matrix4? inverse = Matrix4.TryInvert(PointerEventUtils.RemovePerspectiveTransform(Transform));
        if (inverse is null)
        {
            return false;
        }

        return base.FindAnnotations(result, MatrixUtils.TransformPoint(inverse, localPosition), onlyFirst);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new TransformProperty("transform", Transform));
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/rendering/layer.dart
public sealed class PictureLayer : Layer
{
    public PictureLayer(Rect canvasBounds = default)
    {
        CanvasBounds = canvasBounds;
    }

    /// <summary>The bounds that were used for the canvas that drew this layer's <see cref="Picture"/>.</summary>
    public Rect CanvasBounds { get; }

    /// <summary>The picture recorded for this layer.</summary>
    public Picture? Picture { get; set; }

    /// <summary>Hint that this layer's picture is complex enough to benefit from caching.</summary>
    public bool IsComplexHint { get; set; }

    /// <summary>Hint that this layer's picture is likely to change in the next frame.</summary>
    public bool WillChangeHint { get; set; }

    public bool IsEmpty => Picture is null || Picture.IsEmpty;

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        Picture?.Playback(context, offset);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<Rect>("paint bounds", CanvasBounds));
        properties.Add(new DiagnosticsProperty<bool>("isComplexHint", IsComplexHint, defaultValue: false));
        properties.Add(new DiagnosticsProperty<bool>("willChangeHint", WillChangeHint, defaultValue: false));
    }
}
