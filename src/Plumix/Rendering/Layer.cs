using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Plumix.Gestures;
using Plumix.UI;
using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/layer.dart

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

/// <summary>Signature of the callback added in <see cref="Layer.AddCompositionCallback"/>.</summary>
/// <remarks>Flutter's <c>CompositionCallback</c>.</remarks>
public delegate void CompositionCallback(Layer layer);

public abstract class Layer : DiagnosticableTree
{
    private readonly Dictionary<int, Action> _callbacks = [];
    private static int _nextCallbackId;
    internal int _compositionCallbackCount;
    private bool _debugMutationsLocked;
    internal readonly LayerHandle<Layer> _parentHandle = new();
    private int _refCount;
    private bool _debugDisposed;
    internal ContainerLayer? _parent;
    internal bool _needsAddToScene = true;
    private object? _owner;
    private IDisposable? _engineLayer;
    internal int _depth;
    internal Layer? _nextSibling;
    internal Layer? _previousSibling;

    [ThreadStatic]
    private static BackdropCapture? _magnifierBackdrop;

    [ThreadStatic]
    private static bool _capturingMagnifierBackdrop;

    [ThreadStatic]
    private static BackdropFilterLayer? _backdropCaptureTarget;

    [ThreadStatic]
    private static bool _backdropCaptureStopped;

    /// <summary>Whether this layer or any of its descendants have a composition callback.</summary>
    /// <remarks>Flutter's <c>Layer.subtreeHasCompositionCallbacks</c>.</remarks>
    public bool SubtreeHasCompositionCallbacks => _compositionCallbackCount > 0;

    /// <summary>This layer's parent in the layer tree.</summary>
    public ContainerLayer? Parent => _parent;

    public object? Owner => _owner;

    public bool Attached => _owner != null;

    /// <summary>
    /// The depth of this layer in the layer tree: always greater than its parent's depth.
    /// </summary>
    /// <remarks>Flutter's <c>Layer.depth</c>. It only ever grows, including when the layer is removed.</remarks>
    public int Depth => _depth;

    /// <summary>This layer's next sibling in the parent layer's child list.</summary>
    public Layer? NextSibling => _nextSibling;

    /// <summary>This layer's previous sibling in the parent layer's child list.</summary>
    public Layer? PreviousSibling => _previousSibling;

    /// <summary>
    /// Whether this layer must be added to the scene on every composite, even when nothing about it
    /// changed.
    /// </summary>
    /// <remarks>Flutter's <c>Layer.alwaysNeedsAddToScene</c>.</remarks>
    protected internal virtual bool AlwaysNeedsAddToScene => false;

    public bool DebugDisposed => _debugDisposed;

    public int DebugHandleCount => _refCount;

    /// <summary>
    /// Whether this layer or any of its descendants changed since it was last added to the scene; null
    /// outside debug builds.
    /// </summary>
    /// <remarks>Flutter's <c>Layer.debugSubtreeNeedsAddToScene</c>.</remarks>
    public bool? DebugSubtreeNeedsAddToScene => Constants.KDebugMode ? _needsAddToScene : null;

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

    /// <remarks>Flutter's <c>Layer._updateSubtreeCompositionObserverCount</c>.</remarks>
    internal void UpdateSubtreeCompositionObserverCount(int delta)
    {
        Debug.Assert(delta != 0);
        _compositionCallbackCount += delta;
        Debug.Assert(_compositionCallbackCount >= 0);
        _parent?.UpdateSubtreeCompositionObserverCount(delta);
    }

    /// <remarks>Flutter's <c>Layer._fireCompositionCallbacks</c>.</remarks>
    internal virtual void FireCompositionCallbacks(bool includeChildren)
    {
        if (_callbacks.Count == 0)
        {
            return;
        }

        foreach (Action callback in _callbacks.Values.ToList())
        {
            callback();
        }
    }

    /// <summary>
    /// Adds a callback for when the layer tree that this layer is part of gets composited, or when it is
    /// detached and will not be rendered again. Returns a callback that removes it.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>Layer.addCompositionCallback</c>. The callback must not mutate the layer tree; doing
    /// so asserts in debug builds.
    /// </remarks>
    public Action AddCompositionCallback(CompositionCallback callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        UpdateSubtreeCompositionObserverCount(1);
        int callbackId = _nextCallbackId += 1;
        _callbacks[callbackId] = () =>
        {
            if (Constants.KDebugMode)
            {
                _debugMutationsLocked = true;
            }

            callback(this);
            if (Constants.KDebugMode)
            {
                _debugMutationsLocked = false;
            }
        };
        return () =>
        {
            if (Constants.KDebugMode && !(DebugDisposed || _callbacks.ContainsKey(callbackId)))
            {
                throw new AssertionError("A composition callback was removed twice.");
            }

            _callbacks.Remove(callbackId);
            UpdateSubtreeCompositionObserverCount(-1);
        };
    }

    /// <remarks>Dart's <c>ContainerLayer.dispose</c> clears the library-private <c>_callbacks</c>.</remarks>
    internal void ClearCompositionCallbacks()
    {
        _callbacks.Clear();
    }

    /// <summary>Whether this layer, and all of its descendants, can be rasterized to an image.</summary>
    /// <remarks>Flutter's <c>Layer.supportsRasterization</c>.</remarks>
    public virtual bool SupportsRasterization() => true;

    /// <summary>Describes the clip that this layer would apply to its children, if any.</summary>
    /// <remarks>Flutter's <c>Layer.describeClipBounds</c>.</remarks>
    public virtual Rect? DescribeClipBounds() => null;

    /// <remarks>Flutter's <c>Layer._debugMutationsLocked</c> assert.</remarks>
    internal void DebugAssertMutationsUnlocked()
    {
        if (Constants.KDebugMode && _debugMutationsLocked)
        {
            throw new AssertionError(
                "A layer tree was mutated from inside a composition callback, which is not allowed.");
        }
    }

    /// <summary>Mark that this layer has changed and <see cref="AddToScene"/> needs to be called.</summary>
    /// <remarks>Flutter's <c>Layer.markNeedsAddToScene</c>.</remarks>
    protected internal void MarkNeedsAddToScene()
    {
        DebugAssertMutationsUnlocked();
        if (Constants.KDebugMode && AlwaysNeedsAddToScene)
        {
            throw new AssertionError(
                $"{GetType().Name} with alwaysNeedsAddToScene set called markNeedsAddToScene.\n"
                + "The layer's alwaysNeedsAddToScene is set to true, and therefore it should not call "
                + "markNeedsAddToScene.");
        }

        if (Constants.KDebugMode && _debugDisposed)
        {
            throw new AssertionError("A disposed layer cannot be marked as needing to be added to the scene.");
        }

        // Already marked. Short-circuit.
        if (_needsAddToScene)
        {
            return;
        }

        _needsAddToScene = true;
    }

    /// <summary>Mark that this layer is in sync with the engine.</summary>
    /// <remarks>Flutter's <c>Layer.debugMarkClean</c>; only has an effect in debug builds.</remarks>
    public void DebugMarkClean()
    {
        DebugAssertMutationsUnlocked();
        if (Constants.KDebugMode)
        {
            _needsAddToScene = false;
        }
    }

    /// <summary>
    /// Traverses the layer subtree rooted at this layer and determines whether it needs
    /// <see cref="AddToScene"/>.
    /// </summary>
    /// <remarks>Flutter's <c>Layer.updateSubtreeNeedsAddToScene</c>.</remarks>
    public virtual void UpdateSubtreeNeedsAddToScene()
    {
        DebugAssertMutationsUnlocked();
        _needsAddToScene = _needsAddToScene || AlwaysNeedsAddToScene;
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
        Debug.Assert(_parent == null || Attached == _parent.Attached);
    }

    /// <summary>
    /// Override this method to recompute the depth of this layer's children.
    /// </summary>
    /// <remarks>Flutter's <c>Layer.redepthChildren</c>; a leaf layer has no children to redepth.</remarks>
    protected internal virtual void RedepthChildren()
    {
    }

    /// <summary>Removes this layer from its parent layer's child list.</summary>
    /// <remarks>Flutter's <c>Layer.remove</c>.</remarks>
    public virtual void Remove()
    {
        DebugAssertMutationsUnlocked();
        _parent?.RemoveChild(this);
    }

    protected internal virtual void Dispose()
    {
        DebugAssertMutationsUnlocked();
        if (_debugDisposed)
        {
            throw new AssertionError(
                "Layers must only be disposed once. This is typically handled by LayerHandle and "
                + "createHandle. Subclasses should not directly call dispose.");
        }

        if (_refCount != 0)
        {
            throw new AssertionError(
                $"Do not directly call dispose on a {GetType().Name}. Instead, use createHandle and "
                + "LayerHandle.dispose.");
        }

        _debugDisposed = true;
        _engineLayer?.Dispose();
        _engineLayer = null;
    }

    /// <summary>The engine-side object this layer retained from its last <see cref="AddToScene"/>.</summary>
    /// <remarks>
    /// Flutter's <c>Layer.engineLayer</c>. Setting it disposes the previous value and, unless this layer or
    /// its parent always needs to be added to the scene, marks the parent as needing to be added.
    /// </remarks>
    protected internal IDisposable? EngineLayer
    {
        get => _engineLayer;
        set
        {
            DebugAssertMutationsUnlocked();
            if (_debugDisposed)
            {
                throw new AssertionError("A disposed layer cannot retain an engine layer.");
            }

            _engineLayer?.Dispose();
            _engineLayer = value;
            if (!AlwaysNeedsAddToScene)
            {
                if (_parent != null && !_parent.AlwaysNeedsAddToScene)
                {
                    _parent.MarkNeedsAddToScene();
                }
            }
        }
    }

    internal void Ref()
    {
        _refCount += 1;
    }

    internal void Unref()
    {
        DebugAssertMutationsUnlocked();
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

    /// <summary>
    /// Override this method to upload this layer to the scene: to <paramref name="context"/>, drawn at
    /// the accumulated <paramref name="offset"/> of its ancestors.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>Layer.addToScene(SceneBuilder)</c>. Plumix draws into an Avalonia drawing context
    /// instead of building an engine scene, and passes the ancestors' untransformed offsets down instead
    /// of pushing them. A null <paramref name="context"/> is a headless composite: every layer still runs
    /// its composition-time bookkeeping (<see cref="FollowerLayer"/>'s transform,
    /// <see cref="TransformLayer"/>'s effective transform) but draws nothing. See docs/ai/DIVERGENCES.md.
    /// </remarks>
    internal abstract void AddToScene(DrawingContext? context, Point offset);

    /// <remarks>
    /// Flutter's <c>Layer._addToSceneWithRetainedRendering</c>. Plumix redraws the whole layer tree into
    /// its drawing context every composite, so a clean layer is never added retained; the dirty flag is
    /// still cleared the way Dart clears it.
    /// </remarks>
    internal void AddToSceneWithRetainedRendering(DrawingContext? context, Point offset)
    {
        DebugAssertMutationsUnlocked();
        AddToScene(context, offset);
        _needsAddToScene = false;
    }

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
            level: _parent != null ? DiagnosticLevel.Hidden : DiagnosticLevel.Info,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DiagnosticsProperty<object>(
            "creator",
            DebugCreator,
            defaultValue: DiagnosticsDefaults.NullValue,
            level: DiagnosticLevel.Debug));
        if (_engineLayer != null)
        {
            properties.Add(new DiagnosticsProperty<string>(
                "engine layer",
                Diagnostics.DescribeIdentity(_engineLayer)));
        }

        properties.Add(new DiagnosticsProperty<int>("handles", DebugHandleCount));
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
    private Layer? _firstChild;
    private Layer? _lastChild;

    /// <summary>The children of this layer in paint order.</summary>
    /// <remarks>
    /// Plumix-only indexed view over the <see cref="FirstChild"/>/<see cref="Layer.NextSibling"/> chain.
    /// </remarks>
    public IReadOnlyList<Layer> Children => _children;

    /// <summary>The first composited layer in this layer's child list.</summary>
    public Layer? FirstChild => _firstChild;

    /// <summary>The last composited layer in this layer's child list.</summary>
    public Layer? LastChild => _lastChild;

    /// <summary>Whether this layer has any children.</summary>
    /// <remarks>Flutter's <c>ContainerLayer.hasChildren</c>.</remarks>
    public bool HasChildren => _firstChild != null;

    internal override bool ContainsMagnifier => _children.Any(static child => child.ContainsMagnifier);

    internal override bool ContainsBackdropFilter => _children.Any(static child => child.ContainsBackdropFilter);

    /// <remarks>Flutter's <c>ContainerLayer._fireCompositionCallbacks</c>.</remarks>
    internal override void FireCompositionCallbacks(bool includeChildren)
    {
        base.FireCompositionCallbacks(includeChildren);
        if (!includeChildren)
        {
            return;
        }

        Layer? child = FirstChild;
        while (child != null)
        {
            child.FireCompositionCallbacks(includeChildren);
            child = child.NextSibling;
        }
    }

    /// <inheritdoc />
    public override bool SupportsRasterization()
    {
        for (Layer? child = LastChild; child != null; child = child.PreviousSibling)
        {
            if (!child.SupportsRasterization())
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Composites this layer tree.</summary>
    /// <remarks>
    /// Flutter's <c>ContainerLayer.buildScene</c>: updates the subtree's dirty flags, adds this layer to the
    /// scene, fires the composition callbacks and marks this layer clean. Plumix has no engine scene to
    /// return; a null <paramref name="context"/> composites headlessly (see <see cref="Layer.AddToScene"/>).
    /// </remarks>
    public void BuildScene(DrawingContext? context)
    {
        UpdateSubtreeNeedsAddToScene();
        AddToScene(context, default);
        if (SubtreeHasCompositionCallbacks)
        {
            FireCompositionCallbacks(includeChildren: true);
        }

        // Clearing the flag _after_ calling addToScene, not _before_. This is because
        // subclasses may call addChildrenToScene, which may mark this layer as dirty.
        _needsAddToScene = false;
    }

    private bool DebugUltimatePreviousSiblingOf(Layer child, Layer? equals)
    {
        Debug.Assert(child.Attached == Attached);
        while (child.PreviousSibling != null)
        {
            Debug.Assert(!ReferenceEquals(child.PreviousSibling, child));
            child = child.PreviousSibling;
            Debug.Assert(child.Attached == Attached);
        }

        return ReferenceEquals(child, equals);
    }

    private bool DebugUltimateNextSiblingOf(Layer child, Layer? equals)
    {
        Debug.Assert(child.Attached == Attached);
        while (child._nextSibling != null)
        {
            Debug.Assert(!ReferenceEquals(child._nextSibling, child));
            child = child._nextSibling;
            Debug.Assert(child.Attached == Attached);
        }

        return ReferenceEquals(child, equals);
    }

    protected internal override void Dispose()
    {
        RemoveAllChildren();
        ClearCompositionCallbacks();
        base.Dispose();
    }

    /// <inheritdoc />
    public override void UpdateSubtreeNeedsAddToScene()
    {
        base.UpdateSubtreeNeedsAddToScene();
        Layer? child = FirstChild;
        while (child != null)
        {
            child.UpdateSubtreeNeedsAddToScene();
            _needsAddToScene = _needsAddToScene || child._needsAddToScene;
            child = child.NextSibling;
        }
    }

    protected internal override bool FindAnnotations<T>(
        AnnotationResult<T> result,
        Point localPosition,
        bool onlyFirst)
    {
        ArgumentNullException.ThrowIfNull(result);
        for (Layer? child = LastChild; child != null; child = child.PreviousSibling)
        {
            bool isAbsorbed = child.FindAnnotations(result, localPosition, onlyFirst);
            if (isAbsorbed)
            {
                return true;
            }

            if (onlyFirst && result.Entries.Count > 0)
            {
                return isAbsorbed;
            }
        }

        return false;
    }

    public override void Attach(object owner)
    {
        DebugAssertMutationsUnlocked();
        base.Attach(owner);
        Layer? child = FirstChild;
        while (child != null)
        {
            child.Attach(owner);
            child = child.NextSibling;
        }
    }

    public override void Detach()
    {
        DebugAssertMutationsUnlocked();
        base.Detach();
        Layer? child = FirstChild;
        while (child != null)
        {
            child.Detach();
            child = child.NextSibling;
        }

        FireCompositionCallbacks(includeChildren: false);
    }

    /// <summary>Adds the given layer to the end of this layer's child list.</summary>
    /// <remarks>Flutter's <c>ContainerLayer.append</c>.</remarks>
    public void Append(Layer child)
    {
        ArgumentNullException.ThrowIfNull(child);
        DebugAssertMutationsUnlocked();
        if (Constants.KDebugMode)
        {
            if (ReferenceEquals(child, this)
                || ReferenceEquals(child, FirstChild)
                || ReferenceEquals(child, LastChild)
                || child.Parent != null
                || child.Attached
                || child.NextSibling != null
                || child.PreviousSibling != null
                || child._parentHandle.Layer != null)
            {
                throw new AssertionError("A layer must be detached and parentless before it can be appended.");
            }

            DebugAssertNotAncestor(child);
        }

        AdoptChild(child);
        child._previousSibling = LastChild;
        if (LastChild != null)
        {
            LastChild._nextSibling = child;
        }

        _lastChild = child;
        _firstChild ??= child;
        _children.Add(child);
        child._parentHandle.Layer = child;
        Debug.Assert(child.Attached == Attached);
    }

    private void DebugAssertNotAncestor(Layer child)
    {
        Layer node = this;
        while (node.Parent != null)
        {
            node = node.Parent;
        }

        if (ReferenceEquals(node, child))
        {
            throw new AssertionError("A layer cannot be appended to one of its own descendants.");
        }
    }

    /// <remarks>Flutter's <c>ContainerLayer._adoptChild</c>.</remarks>
    private void AdoptChild(Layer child)
    {
        DebugAssertMutationsUnlocked();
        if (!AlwaysNeedsAddToScene)
        {
            MarkNeedsAddToScene();
        }

        if (child._compositionCallbackCount != 0)
        {
            UpdateSubtreeCompositionObserverCount(child._compositionCallbackCount);
        }

        Debug.Assert(child._parent == null);
        if (Constants.KDebugMode)
        {
            DebugAssertNotAncestor(child);
        }

        child._parent = this;
        if (Attached)
        {
            child.Attach(Owner!);
        }

        RedepthChild(child);
    }

    /// <inheritdoc />
    protected internal override void RedepthChildren()
    {
        Layer? child = FirstChild;
        while (child != null)
        {
            RedepthChild(child);
            child = child.NextSibling;
        }
    }

    /// <summary>Adjust the depth of the given child to be greater than this layer's own depth.</summary>
    /// <remarks>Flutter's <c>ContainerLayer.redepthChild</c>.</remarks>
    protected void RedepthChild(Layer child)
    {
        Debug.Assert(ReferenceEquals(child.Owner, Owner));
        if (child._depth <= _depth)
        {
            child._depth = _depth + 1;
            child.RedepthChildren();
        }
    }

    /// <remarks>
    /// Flutter's <c>ContainerLayer._removeChild</c>, the implementation of <see cref="Layer.Remove"/>.
    /// </remarks>
    internal void RemoveChild(Layer child)
    {
        Debug.Assert(ReferenceEquals(child.Parent, this));
        Debug.Assert(child.Attached == Attached);
        Debug.Assert(DebugUltimatePreviousSiblingOf(child, equals: FirstChild));
        Debug.Assert(DebugUltimateNextSiblingOf(child, equals: LastChild));
        Debug.Assert(child._parentHandle.Layer != null);
        if (child._previousSibling == null)
        {
            Debug.Assert(ReferenceEquals(_firstChild, child));
            _firstChild = child._nextSibling;
        }
        else
        {
            child._previousSibling._nextSibling = child.NextSibling;
        }

        if (child._nextSibling == null)
        {
            Debug.Assert(ReferenceEquals(LastChild, child));
            _lastChild = child.PreviousSibling;
        }
        else
        {
            child._nextSibling._previousSibling = child.PreviousSibling;
        }

        Debug.Assert((FirstChild == null) == (LastChild == null));
        Debug.Assert(FirstChild == null || FirstChild.Attached == Attached);
        Debug.Assert(LastChild == null || LastChild.Attached == Attached);
        Debug.Assert(FirstChild == null || DebugUltimateNextSiblingOf(FirstChild, equals: LastChild));
        Debug.Assert(LastChild == null || DebugUltimatePreviousSiblingOf(LastChild, equals: FirstChild));
        child._previousSibling = null;
        child._nextSibling = null;
        _children.Remove(child);
        DropChild(child);
        child._parentHandle.Layer = null;
        Debug.Assert(!child.Attached);
    }

    /// <remarks>Flutter's <c>ContainerLayer._dropChild</c>.</remarks>
    private void DropChild(Layer child)
    {
        DebugAssertMutationsUnlocked();
        if (!AlwaysNeedsAddToScene)
        {
            MarkNeedsAddToScene();
        }

        if (child._compositionCallbackCount != 0)
        {
            UpdateSubtreeCompositionObserverCount(-child._compositionCallbackCount);
        }

        Debug.Assert(ReferenceEquals(child._parent, this));
        Debug.Assert(child.Attached == Attached);
        child._parent = null;
        if (Attached)
        {
            child.Detach();
        }
    }

    /// <summary>Removes the given child layer, if it is a child of this layer.</summary>
    /// <remarks>Plumix-only convenience for <c>child.remove()</c> that tolerates a non-child.</remarks>
    public void Remove(Layer child)
    {
        if (ReferenceEquals(child.Parent, this))
        {
            child.Remove();
        }
    }

    /// <summary>Removes all of this layer's children from its child list.</summary>
    /// <remarks>Flutter's <c>ContainerLayer.removeAllChildren</c>.</remarks>
    public void RemoveAllChildren()
    {
        DebugAssertMutationsUnlocked();
        Layer? child = FirstChild;
        while (child != null)
        {
            Layer? next = child.NextSibling;
            child._previousSibling = null;
            child._nextSibling = null;
            Debug.Assert(child.Attached == Attached);
            DropChild(child);
            child._parentHandle.Layer = null;
            child = next;
        }

        _firstChild = null;
        _lastChild = null;
        _children.Clear();
    }

    internal override void AddToScene(DrawingContext? context, Point offset)
    {
        AddChildrenToScene(context, offset);
    }

    /// <summary>Uploads all of this layer's children to the scene.</summary>
    /// <remarks>Flutter's <c>ContainerLayer.addChildrenToScene</c>.</remarks>
    protected void AddChildrenToScene(DrawingContext? context, Point offset)
    {
        Layer? child = FirstChild;
        while (child != null)
        {
            if (BackdropCaptureStopped)
            {
                return;
            }

            child.AddToSceneWithRetainedRendering(context, offset);
            child = child.NextSibling;
        }
    }

    /// <summary>
    /// Applies the transform that would be applied when compositing the given child to the given matrix.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>ContainerLayer.applyTransform</c>. Valid only immediately after this layer was added to
    /// the scene; a container that does not move its children adds nothing.
    /// </remarks>
    public virtual void ApplyTransform(Layer? child, Matrix4 transform)
    {
        Debug.Assert(child != null);
        ArgumentNullException.ThrowIfNull(transform);
    }

    /// <summary>Returns the descendants of this layer in depth-first order.</summary>
    /// <remarks>Flutter's <c>ContainerLayer.depthFirstIterateChildren</c>.</remarks>
    public List<Layer> DepthFirstIterateChildren()
    {
        if (FirstChild == null)
        {
            return [];
        }

        var children = new List<Layer>();
        Layer? child = FirstChild;
        while (child != null)
        {
            children.Add(child);
            if (child is ContainerLayer container)
            {
                children.AddRange(container.DepthFirstIterateChildren());
            }

            child = child.NextSibling;
        }

        return children;
    }

    internal override void CollectBackdropFilters(ICollection<BackdropFilterLayer> filters)
    {
        foreach (Layer child in _children)
        {
            child.CollectBackdropFilters(filters);
        }
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren()
    {
        var children = new List<DiagnosticsNode>();
        if (FirstChild == null)
        {
            return children;
        }

        Layer? child = FirstChild;
        int count = 1;
        while (true)
        {
            children.Add(child.ToDiagnosticsNode(name: $"child {count}"));
            if (ReferenceEquals(child, LastChild))
            {
                break;
            }

            count += 1;
            child = child.NextSibling!;
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

    public LeaderLayer? Leader => _leader;

    public Size? LeaderSize { get; set; }

    // Dart's `_debugPreviousLeaders`: while a link moves between leaders inside one frame (flutter#96959),
    // the leader it left is parked here and must have detached by the end of the frame.
    private HashSet<LeaderLayer>? _debugPreviousLeaders;
    private bool _debugLeaderCheckScheduled;

    /// <summary>Dart's <c>LayerLink._registerLeader</c>.</summary>
    internal void RegisterLeader(LeaderLayer leader)
    {
        if (Constants.KDebugMode && ReferenceEquals(_leader, leader))
        {
            throw new AssertionError();
        }

        if (Constants.KDebugMode && _leader != null)
        {
            _debugPreviousLeaders ??= [];
            DebugScheduleLeadersCleanUpCheck();
            _debugPreviousLeaders.Add(_leader);
        }

        _leader = leader;
    }

    /// <summary>Dart's <c>LayerLink._unregisterLeader</c>.</summary>
    internal void UnregisterLeader(LeaderLayer leader)
    {
        if (ReferenceEquals(_leader, leader))
        {
            _leader = null;
        }
        else if (Constants.KDebugMode && _debugPreviousLeaders?.Remove(leader) != true)
        {
            throw new AssertionError("A LeaderLayer unregistered from a LayerLink it was never registered with.");
        }
    }

    /// <summary>Dart's <c>LayerLink._debugScheduleLeadersCleanUpCheck</c>.</summary>
    private void DebugScheduleLeadersCleanUpCheck()
    {
        if (_debugLeaderCheckScheduled)
        {
            return;
        }

        _debugLeaderCheckScheduled = true;
        Scheduler.AddPostFrameCallback(
            _ =>
            {
                _debugLeaderCheckScheduled = false;
                if (_debugPreviousLeaders is { Count: > 0 })
                {
                    throw new AssertionError(
                        "A LayerLink still has previous LeaderLayers attached at the end of the frame.");
                }
            },
            debugLabel: "LayerLink.leadersCleanUpCheck");
    }

    /// <summary>Dart's <c>LayerLink.toString</c>.</summary>
    public override string ToString() =>
        $"{Diagnostics.DescribeIdentity(this)}({(_leader != null ? "<linked>" : "<dangling>")})";
}

/// <summary>
/// A composited layer that can be followed by a <see cref="FollowerLayer"/>.
/// </summary>
/// <remarks>
/// Flutter's <c>LeaderLayer</c>. This layer collapses the accumulated offset into a transform and passes
/// <see cref="Point"/> zero to its child layers in <see cref="AddToScene"/>, so the follower can compute
/// its transform from the layer chain alone.
/// </remarks>
public sealed class LeaderLayer : ContainerLayer
{
    private LayerLink _link;
    private Point _offset;

    public LeaderLayer(LayerLink link, Point offset = default)
    {
        _link = link;
        _offset = offset;
    }

    /// <summary>The object with which this layer should register.</summary>
    /// <remarks>
    /// The link will be established when this layer is attached, and will be cleared when this layer is
    /// detached.
    /// </remarks>
    public LayerLink Link
    {
        get => _link;
        set
        {
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

    /// <summary>Offset from parent in the parent's coordinate system.</summary>
    public Point Offset
    {
        get => _offset;
        set
        {
            if (value == _offset)
            {
                return;
            }

            _offset = value;
            if (!AlwaysNeedsAddToScene)
            {
                MarkNeedsAddToScene();
            }
        }
    }

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

    protected internal override bool FindAnnotations<T>(
        AnnotationResult<T> result,
        Point localPosition,
        bool onlyFirst)
    {
        return base.FindAnnotations(result, localPosition - Offset, onlyFirst);
    }

    /// <remarks>
    /// Dart pushes a translation transform when <see cref="Offset"/> is non-zero; Plumix passes the offset
    /// down with the accumulated scene offset instead (see <see cref="Layer.AddToScene"/>).
    /// </remarks>
    internal override void AddToScene(DrawingContext? context, Point offset)
    {
        AddChildrenToScene(context, offset + Offset);
    }

    /// <summary>
    /// Applies the transform that would be applied when compositing the given child to the given matrix.
    /// </summary>
    /// <remarks>
    /// See <see cref="ContainerLayer.ApplyTransform"/> for details. The <paramref name="child"/> argument may
    /// be null, as the same transform is applied to all children.
    /// </remarks>
    public override void ApplyTransform(Layer? child, Matrix4 transform)
    {
        if (Offset != default)
        {
            transform.TranslateByDouble(Offset.X, Offset.Y, 0, 1);
        }
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
/// A layer that applies a transformation which causes its children to be positioned relative to the
/// <see cref="LeaderLayer"/> registered with <see cref="Link"/>.
/// </summary>
/// <remarks>
/// Flutter's <c>FollowerLayer</c>. The transform is established while compositing, from the layer chain
/// between the leader, the follower and their common ancestor, so the leader must be composited before
/// the follower.
/// </remarks>
public sealed class FollowerLayer : ContainerLayer
{
    private Point? _lastOffset;
    private Matrix4? _lastTransform;
    private Matrix4? _invertedTransform;
    private bool _inverseDirty = true;

    public FollowerLayer(
        LayerLink link,
        bool showWhenUnlinked = true,
        Point unlinkedOffset = default,
        Point linkedOffset = default)
    {
        Link = link ?? throw new ArgumentNullException(nameof(link));
        ShowWhenUnlinked = showWhenUnlinked;
        UnlinkedOffset = unlinkedOffset;
        LinkedOffset = linkedOffset;
    }

    /// <summary>The link to the <see cref="LeaderLayer"/>.</summary>
    public LayerLink Link { get; set; }

    /// <summary>
    /// Whether to show the layer's contents when the <see cref="Link"/> does not point to a
    /// <see cref="LeaderLayer"/>.
    /// </summary>
    public bool? ShowWhenUnlinked { get; set; }

    /// <summary>
    /// Offset from parent in the parent's coordinate system, used when the layer is not linked to a
    /// <see cref="LeaderLayer"/>.
    /// </summary>
    public Point? UnlinkedOffset { get; set; }

    /// <summary>
    /// Offset from the origin of the leader layer to the origin of the child layers, used when the layer is
    /// linked to a <see cref="LeaderLayer"/>.
    /// </summary>
    public Point? LinkedOffset { get; set; }

    private Point? TransformOffset(Point localPosition)
    {
        if (_inverseDirty)
        {
            _invertedTransform = Matrix4.TryInvert(GetLastTransform()!);
            _inverseDirty = false;
        }

        if (_invertedTransform == null)
        {
            return null;
        }

        var vector = new Vector4(localPosition.X, localPosition.Y, 0.0, 1.0);
        Vector4 result = _invertedTransform.Transform(vector);
        return new Point(result[0] - LinkedOffset!.Value.X, result[1] - LinkedOffset!.Value.Y);
    }

    protected internal override bool FindAnnotations<T>(
        AnnotationResult<T> result,
        Point localPosition,
        bool onlyFirst)
    {
        if (Link.Leader == null)
        {
            if (ShowWhenUnlinked!.Value)
            {
                return base.FindAnnotations(result, localPosition - UnlinkedOffset!.Value, onlyFirst);
            }

            return false;
        }

        Point? transformedOffset = TransformOffset(localPosition);
        if (transformedOffset == null)
        {
            return false;
        }

        return base.FindAnnotations(result, transformedOffset.Value, onlyFirst);
    }

    /// <summary>
    /// The transform that was used during the last composition phase, or null if the link was not
    /// established or the layer was not composited yet.
    /// </summary>
    /// <remarks>
    /// The returned transform maps from the coordinate space of this layer's children to that of the render
    /// object that pushed it, whose paint offset was <see cref="UnlinkedOffset"/>.
    /// </remarks>
    public Matrix4? GetLastTransform()
    {
        if (_lastTransform == null)
        {
            return null;
        }

        Matrix4 result = Matrix4.TranslationValues(-_lastOffset!.Value.X, -_lastOffset!.Value.Y, 0.0);
        result.Multiply(_lastTransform);
        return result;
    }

    /// <summary>
    /// Call <see cref="ContainerLayer.ApplyTransform"/> for each layer in the provided list.
    /// </summary>
    /// <remarks>
    /// The list is in reverse order (deepest first). The first layer is not used for applying the
    /// transform, but only as the child of the next layer.
    /// </remarks>
    private static Matrix4 CollectTransformForLayerChain(List<ContainerLayer?> layers)
    {
        // Initialize our result matrix.
        Matrix4 result = Matrix4.Identity();
        // Apply each layer to the matrix in turn, starting from the last layer, and providing the previous
        // layer as the child.
        for (int index = layers.Count - 1; index > 0; index -= 1)
        {
            layers[index]?.ApplyTransform(layers[index - 1], result);
        }

        return result;
    }

    /// <summary>
    /// Find the common ancestor of two layers <paramref name="a"/> and <paramref name="b"/> by searching
    /// towards the root of the tree, and append each ancestor of <paramref name="a"/> or
    /// <paramref name="b"/> visited along the path to <paramref name="ancestorsA"/> and
    /// <paramref name="ancestorsB"/> respectively.
    /// </summary>
    /// <remarks>Returns null if <paramref name="a"/> and <paramref name="b"/> do not share a common ancestor.</remarks>
    private static Layer? PathsToCommonAncestor(
        Layer? a,
        Layer? b,
        List<ContainerLayer?> ancestorsA,
        List<ContainerLayer?> ancestorsB)
    {
        // No common ancestor found.
        if (a == null || b == null)
        {
            return null;
        }

        if (ReferenceEquals(a, b))
        {
            return a;
        }

        if (a.Depth < b.Depth)
        {
            ancestorsB.Add(b.Parent);
            return PathsToCommonAncestor(a, b.Parent, ancestorsA, ancestorsB);
        }

        if (a.Depth > b.Depth)
        {
            ancestorsA.Add(a.Parent);
            return PathsToCommonAncestor(a.Parent, b, ancestorsA, ancestorsB);
        }

        ancestorsA.Add(a.Parent);
        ancestorsB.Add(b.Parent);
        return PathsToCommonAncestor(a.Parent, b.Parent, ancestorsA, ancestorsB);
    }

    private static bool DebugCheckLeaderBeforeFollower(
        List<ContainerLayer?> leaderToCommonAncestor,
        List<ContainerLayer?> followerToCommonAncestor)
    {
        if (followerToCommonAncestor.Count <= 1)
        {
            // Follower is the common ancestor, ergo the leader must come AFTER the follower.
            return false;
        }

        if (leaderToCommonAncestor.Count <= 1)
        {
            // Leader is the common ancestor, ergo the leader must come BEFORE the follower.
            return true;
        }

        // Common ancestor is neither the leader nor the follower.
        ContainerLayer leaderSubtreeBelowAncestor = leaderToCommonAncestor[^2]!;
        ContainerLayer followerSubtreeBelowAncestor = followerToCommonAncestor[^2]!;

        Layer? sibling = leaderSubtreeBelowAncestor;
        while (sibling != null)
        {
            if (ReferenceEquals(sibling, followerSubtreeBelowAncestor))
            {
                return true;
            }

            sibling = sibling.NextSibling;
        }

        // The follower subtree didn't come after the leader subtree.
        return false;
    }

    /// <summary>
    /// Populate <c>_lastTransform</c> given the current state of the tree.
    /// </summary>
    private void EstablishTransform()
    {
        _lastTransform = null;
        LeaderLayer? leader = Link.Leader;
        // Check to see if we are linked.
        if (leader == null)
        {
            return;
        }

        // If we're linked, check the link is valid.
        if (Constants.KDebugMode && !ReferenceEquals(leader.Owner, Owner))
        {
            throw new AssertionError(
                "Linked LeaderLayer anchor is not in the same layer tree as the FollowerLayer.");
        }

        // Stores [leader, ..., commonAncestor] after calling PathsToCommonAncestor.
        List<ContainerLayer?> forwardLayers = [leader];
        // Stores [this (follower), ..., commonAncestor] after calling PathsToCommonAncestor.
        List<ContainerLayer?> inverseLayers = [this];

        Layer? ancestor = PathsToCommonAncestor(leader, this, forwardLayers, inverseLayers);
        if (Constants.KDebugMode && ancestor == null)
        {
            throw new AssertionError("LeaderLayer and FollowerLayer do not have a common ancestor.");
        }

        if (Constants.KDebugMode && !DebugCheckLeaderBeforeFollower(forwardLayers, inverseLayers))
        {
            throw new AssertionError(
                "LeaderLayer anchor must come before FollowerLayer in paint order, but the reverse was true.");
        }

        Matrix4 forwardTransform = CollectTransformForLayerChain(forwardLayers);
        // Further transforms the coordinate system to a hypothetical child (null) of the leader layer, to
        // account for the leader's additional paint offset and layer offset (LeaderLayer.Offset).
        leader.ApplyTransform(null, forwardTransform);
        forwardTransform.TranslateByDouble(LinkedOffset!.Value.X, LinkedOffset!.Value.Y, 0, 1);

        Matrix4 inverseTransform = CollectTransformForLayerChain(inverseLayers);

        if (inverseTransform.Invert() == 0.0)
        {
            // We are in a degenerate transform, so there's not much we can do.
            return;
        }

        // Combine the matrices and store the result.
        inverseTransform.Multiply(forwardTransform);
        _lastTransform = inverseTransform;
        _inverseDirty = true;
    }

    /// <remarks>
    /// This layer's transform depends on where its leader is, which can change without this layer being
    /// marked dirty, so it is recomputed on every composite.
    /// </remarks>
    protected internal override bool AlwaysNeedsAddToScene => true;

    /// <remarks>
    /// Dart pushes <c>_lastTransform</c>, or a translation by <see cref="UnlinkedOffset"/> when unlinked;
    /// Plumix first pushes the ancestors' accumulated <paramref name="offset"/>, which Dart's ancestors
    /// have already pushed.
    /// </remarks>
    internal override void AddToScene(DrawingContext? context, Point offset)
    {
        Debug.Assert(ShowWhenUnlinked != null);
        if (Link.Leader == null && !ShowWhenUnlinked!.Value)
        {
            _lastTransform = null;
            _lastOffset = null;
            _inverseDirty = true;
            EngineLayer = null;
            return;
        }

        EstablishTransform();
        Matrix4 transform;
        if (_lastTransform != null)
        {
            _lastOffset = UnlinkedOffset;
            transform = _lastTransform;
        }
        else
        {
            _lastOffset = null;
            transform = Matrix4.TranslationValues(UnlinkedOffset!.Value.X, UnlinkedOffset!.Value.Y, .0);
        }

        if (context == null)
        {
            AddChildrenToScene(null, default);
        }
        else
        {
            using (context.PushTransform(Matrix.CreateTranslation(offset.X, offset.Y)))
            using (context.PushTransform(transform.ToAvaloniaMatrix()))
            {
                AddChildrenToScene(context, default);
            }
        }

        _inverseDirty = true;
    }

    /// <inheritdoc />
    public override void ApplyTransform(Layer? child, Matrix4 transform)
    {
        Debug.Assert(child != null);
        if (_lastTransform != null)
        {
            transform.Multiply(_lastTransform);
        }
        else
        {
            transform.Multiply(Matrix4.TranslationValues(UnlinkedOffset!.Value.X, UnlinkedOffset!.Value.Y, 0));
        }
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

    internal override void AddToScene(DrawingContext? context, Point offset)
    {
        if (context == null)
        {
            AddChildrenToScene(null, offset);
            return;
        }

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

/// <summary>A layer that is displayed at an offset from its parent layer.</summary>
/// <remarks>Flutter's <c>OffsetLayer</c>.</remarks>
public class OffsetLayer : ContainerLayer
{
    private Point _offset;

    public OffsetLayer(Point offset = default)
    {
        _offset = offset;
    }

    /// <summary>Offset from parent in the parent's coordinate system.</summary>
    public Point Offset
    {
        get => _offset;
        set
        {
            if (value != _offset)
            {
                MarkNeedsAddToScene();
            }

            _offset = value;
        }
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
    public override void ApplyTransform(Layer? child, Matrix4 transform)
    {
        Debug.Assert(child != null);
        transform.TranslateByDouble(Offset.X, Offset.Y, 0, 1);
    }

    internal override void AddToScene(DrawingContext? context, Point offset)
    {
        AddChildrenToScene(context, offset + Offset);
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
    private int? _alpha;

    public OpacityLayer(int? alpha = null, Point offset = default) : base(offset)
    {
        _alpha = alpha;
    }

    /// <summary>The amount to multiply into the alpha channel, from 0 (transparent) to 255 (opaque).</summary>
    public int? Alpha
    {
        get => _alpha;
        set
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value != _alpha)
            {
                if (value == 255 || _alpha == 255)
                {
                    EngineLayer = null;
                }

                _alpha = value;
                MarkNeedsAddToScene();
            }
        }
    }

    /// <summary>
    /// Plumix-only view of <see cref="Alpha"/> as a 0..1 fraction, for the render objects and debug
    /// flags that carry an opacity rather than Flutter's 8-bit alpha.
    /// </summary>
    public double Opacity
    {
        get => (Alpha ?? 255) / 255.0;
        set => Alpha = (int)Math.Round(Math.Clamp(value, 0.0, 1.0) * 255.0);
    }

    internal override void AddToScene(DrawingContext? context, Point offset)
    {
        Debug.Assert(Alpha != null);
        bool enabled = FirstChild != null;
        if (!enabled)
        {
            // Ensure the engine layer is disposed.
            EngineLayer = null;
            // Don't add this layer if there's no child.
            return;
        }

        if (Constants.KDebugMode)
        {
            enabled = enabled && !RenderingDebug.DisableOpacityLayers;
        }

        if (context == null || !enabled || Alpha!.Value >= 255)
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

    private ColorFilter? _colorFilter;

    public ColorFilter? ColorFilter
    {
        get => _colorFilter;
        set
        {
            if (EqualityComparer<ColorFilter?>.Default.Equals(value, _colorFilter))
            {
                return;
            }

            _colorFilter = value;
            MarkNeedsAddToScene();
        }
    }

    public Rect FilterBounds { get; set; }

    internal override void AddToScene(DrawingContext? context, Point offset)
    {
        if (context == null)
        {
            AddChildrenToScene(null, offset);
            return;
        }

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

    private ImageFilter? _imageFilter;

    public ImageFilter? ImageFilter
    {
        get => _imageFilter;
        set
        {
            if (EqualityComparer<ImageFilter?>.Default.Equals(value, _imageFilter))
            {
                return;
            }

            _imageFilter = value;
            MarkNeedsAddToScene();
        }
    }

    public Rect FilterBounds { get; set; }

    internal override void AddToScene(DrawingContext? context, Point offset)
    {
        if (context == null)
        {
            base.AddToScene(null, offset);
            return;
        }

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

    private ImageFilter? _imageFilter;

    public ImageFilter? ImageFilter
    {
        get => _imageFilter;
        set
        {
            if (EqualityComparer<ImageFilter?>.Default.Equals(value, _imageFilter))
            {
                return;
            }

            _imageFilter = value;
            MarkNeedsAddToScene();
        }
    }

    private BlendMode _blendMode = BlendMode.SourceOver;

    public BlendMode BlendMode
    {
        get => _blendMode;
        set
        {
            if (EqualityComparer<BlendMode>.Default.Equals(value, _blendMode))
            {
                return;
            }

            _blendMode = value;
            MarkNeedsAddToScene();
        }
    }

    public BackdropKey? BackdropKey { get; set; }

    internal override bool ContainsBackdropFilter => true;

    internal override void CollectBackdropFilters(ICollection<BackdropFilterLayer> filters)
    {
        filters.Add(this);
        base.CollectBackdropFilters(filters);
    }

    internal override void AddToScene(DrawingContext? context, Point offset)
    {
        if (context == null)
        {
            AddChildrenToScene(null, offset);
            return;
        }

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

    private IBrush? _shader;

    public IBrush? Shader
    {
        get => _shader;
        set
        {
            if (EqualityComparer<IBrush?>.Default.Equals(value, _shader))
            {
                return;
            }

            _shader = value;
            MarkNeedsAddToScene();
        }
    }

    private Rect _maskRect;

    public Rect MaskRect
    {
        get => _maskRect;
        set
        {
            if (EqualityComparer<Rect>.Default.Equals(value, _maskRect))
            {
                return;
            }

            _maskRect = value;
            MarkNeedsAddToScene();
        }
    }

    private BlendMode _blendMode = BlendMode.Modulate;

    public BlendMode BlendMode
    {
        get => _blendMode;
        set
        {
            if (EqualityComparer<BlendMode>.Default.Equals(value, _blendMode))
            {
                return;
            }

            _blendMode = value;
            MarkNeedsAddToScene();
        }
    }

    internal override void AddToScene(DrawingContext? context, Point offset)
    {
        if (context == null)
        {
            AddChildrenToScene(null, offset);
            return;
        }

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
    private Rect _clipRect;

    public Rect ClipRect
    {
        get => _clipRect;
        set
        {
            if (EqualityComparer<Rect>.Default.Equals(value, _clipRect))
            {
                return;
            }

            _clipRect = value;
            MarkNeedsAddToScene();
        }
    }

    private Clip _clipBehavior = Clip.HardEdge;

    public Clip ClipBehavior
    {
        get => _clipBehavior;
        set
        {
            if (Constants.KDebugMode && value == Clip.None)
            {
                throw new AssertionError("A ClipRectLayer cannot use Clip.none.");
            }

            if (EqualityComparer<Clip>.Default.Equals(value, _clipBehavior))
            {
                return;
            }

            _clipBehavior = value;
            MarkNeedsAddToScene();
        }
    }

    /// <inheritdoc />
    public override Rect? DescribeClipBounds() => ClipRect;

    internal override void AddToScene(DrawingContext? context, Point offset)
    {
        if (context == null)
        {
            base.AddToScene(null, offset);
            return;
        }

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
    private RRect _clipRRect;

    public RRect ClipRRect
    {
        get => _clipRRect;
        set
        {
            if (EqualityComparer<RRect>.Default.Equals(value, _clipRRect))
            {
                return;
            }

            _clipRRect = value;
            MarkNeedsAddToScene();
        }
    }

    private Clip _clipBehavior = Clip.AntiAlias;

    public Clip ClipBehavior
    {
        get => _clipBehavior;
        set
        {
            if (Constants.KDebugMode && value == Clip.None)
            {
                throw new AssertionError("A ClipRRectLayer cannot use Clip.none.");
            }

            if (EqualityComparer<Clip>.Default.Equals(value, _clipBehavior))
            {
                return;
            }

            _clipBehavior = value;
            MarkNeedsAddToScene();
        }
    }

    /// <inheritdoc />
    public override Rect? DescribeClipBounds() => ClipRRect.Rect;

    internal override void AddToScene(DrawingContext? context, Point offset)
    {
        if (context == null)
        {
            base.AddToScene(null, offset);
            return;
        }

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
    private RSuperellipse _clipRSuperellipse;

    public RSuperellipse ClipRSuperellipse
    {
        get => _clipRSuperellipse;
        set
        {
            if (EqualityComparer<RSuperellipse>.Default.Equals(value, _clipRSuperellipse))
            {
                return;
            }

            _clipRSuperellipse = value;
            MarkNeedsAddToScene();
        }
    }

    private Clip _clipBehavior = Clip.AntiAlias;

    public Clip ClipBehavior
    {
        get => _clipBehavior;
        set
        {
            if (Constants.KDebugMode && value == Clip.None)
            {
                throw new AssertionError("A ClipRSuperellipseLayer cannot use Clip.none.");
            }

            if (EqualityComparer<Clip>.Default.Equals(value, _clipBehavior))
            {
                return;
            }

            _clipBehavior = value;
            MarkNeedsAddToScene();
        }
    }

    /// <inheritdoc />
    public override Rect? DescribeClipBounds() => ClipRSuperellipse.Rect;

    internal override void AddToScene(DrawingContext? context, Point offset)
    {
        if (context == null)
        {
            base.AddToScene(null, offset);
            return;
        }

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
        // Dart hit tests the superellipse's bounding rectangle.
        return ContainsRect(ClipRSuperellipse.Rect, localPosition)
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
            if (ReferenceEquals(value, _clipPath))
            {
                return;
            }

            _clipPath = value;
            _geometry = null;
            MarkNeedsAddToScene();
        }
    }

    private Clip _clipBehavior = Clip.AntiAlias;

    public Clip ClipBehavior
    {
        get => _clipBehavior;
        set
        {
            if (Constants.KDebugMode && value == Clip.None)
            {
                throw new AssertionError("A ClipPathLayer cannot use Clip.none.");
            }

            if (EqualityComparer<Clip>.Default.Equals(value, _clipBehavior))
            {
                return;
            }

            _clipBehavior = value;
            MarkNeedsAddToScene();
        }
    }

    /// <inheritdoc />
    public override Rect? DescribeClipBounds() => _clipPath.GetBounds();

    internal override void AddToScene(DrawingContext? context, Point offset)
    {
        if (context == null)
        {
            base.AddToScene(null, offset);
            return;
        }

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

    internal override void AddToScene(DrawingContext? context, Point offset)
    {
        if (context == null)
        {
            base.AddToScene(null, offset);
            return;
        }

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
/// <summary>A composited layer that applies a given transformation matrix to its children.</summary>
/// <remarks>
/// Flutter's <c>TransformLayer</c>. This class inherits from <see cref="OffsetLayer"/> to make it one of
/// the layers that can be used at the root of a <see cref="RenderObject"/> hierarchy.
/// </remarks>
public sealed class TransformLayer : OffsetLayer
{
    private Matrix4? _transform;
    private Matrix4? _lastEffectiveTransform;
    private Matrix4? _invertedTransform;
    private bool _inverseDirty = true;

    public TransformLayer(Matrix4? transform = null, Point offset = default) : base(offset)
    {
        _transform = transform;
    }

    /// <summary>The matrix to apply.</summary>
    /// <remarks>
    /// Dart's field is nullable and starts null; Plumix starts from the identity matrix so an unset
    /// transform composites as a no-op.
    /// </remarks>
    public Matrix4 Transform
    {
        get => _transform ??= Matrix4.Identity();
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (Constants.KDebugMode && !value.Storage.All(double.IsFinite))
            {
                throw new AssertionError("A TransformLayer transform must have finite components.");
            }

            if (value == _transform)
            {
                return;
            }

            _transform = value;
            _inverseDirty = true;
            MarkNeedsAddToScene();
        }
    }

    internal override void AddToScene(DrawingContext? context, Point offset)
    {
        _lastEffectiveTransform = Transform;
        if (Offset != default)
        {
            _lastEffectiveTransform = Matrix4.TranslationValues(Offset.X, Offset.Y, 0.0);
            _lastEffectiveTransform.Multiply(Transform);
        }

        if (context == null)
        {
            AddChildrenToScene(null, default);
            return;
        }

        using (context.PushTransform(Matrix.CreateTranslation(offset.X, offset.Y)))
        using (context.PushTransform(_lastEffectiveTransform.ToAvaloniaMatrix()))
        {
            AddChildrenToScene(context, default);
        }
    }

    private Point? TransformOffset(Point localPosition)
    {
        if (_inverseDirty)
        {
            _invertedTransform = Matrix4.TryInvert(PointerEvent.RemovePerspectiveTransform(Transform));
            _inverseDirty = false;
        }

        if (_invertedTransform == null)
        {
            return null;
        }

        return MatrixUtils.TransformPoint(_invertedTransform, localPosition);
    }

    protected internal override bool FindAnnotations<T>(
        AnnotationResult<T> result,
        Point localPosition,
        bool onlyFirst)
    {
        Point? transformedOffset = TransformOffset(localPosition);
        if (transformedOffset == null)
        {
            return false;
        }

        return base.FindAnnotations(result, transformedOffset.Value, onlyFirst);
    }

    /// <inheritdoc />
    public override void ApplyTransform(Layer? child, Matrix4 transform)
    {
        Debug.Assert(child != null);
        if (_lastEffectiveTransform == null)
        {
            transform.Multiply(Transform);
        }
        else
        {
            transform.Multiply(_lastEffectiveTransform);
        }
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

    private Picture? _picture;
    private bool _isComplexHint;
    private bool _willChangeHint;

    /// <summary>The picture recorded for this layer.</summary>
    public Picture? Picture
    {
        get => _picture;
        set
        {
            if (Constants.KDebugMode && DebugDisposed)
            {
                throw new AssertionError("A disposed PictureLayer cannot take a picture.");
            }

            MarkNeedsAddToScene();
            _picture = value;
        }
    }

    /// <summary>Hint that this layer's picture is complex enough to benefit from caching.</summary>
    public bool IsComplexHint
    {
        get => _isComplexHint;
        set
        {
            if (value != _isComplexHint)
            {
                _isComplexHint = value;
                MarkNeedsAddToScene();
            }
        }
    }

    /// <summary>Hint that this layer's picture is likely to change in the next frame.</summary>
    public bool WillChangeHint
    {
        get => _willChangeHint;
        set
        {
            if (value != _willChangeHint)
            {
                _willChangeHint = value;
                MarkNeedsAddToScene();
            }
        }
    }

    public bool IsEmpty => Picture is null || Picture.IsEmpty;

    internal override void AddToScene(DrawingContext? context, Point offset)
    {
        if (context == null)
        {
            return;
        }

        Picture?.Playback(context, offset);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<Rect>("paint bounds", CanvasBounds));
        properties.Add(new DiagnosticsProperty<string>("picture", Diagnostics.DescribeIdentity(_picture)));
        string isComplex = IsComplexHint ? "true" : "false";
        string willChange = WillChangeHint ? "true" : "false";
        properties.Add(new DiagnosticsProperty<string>(
            "raster cache hints",
            $"isComplex = {isComplex}, willChange = {willChange}"));
    }
}
