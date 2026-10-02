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
    private EngineLayer? _engineLayer;
    internal int _depth;
    internal Layer? _nextSibling;
    internal Layer? _previousSibling;

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
    protected internal EngineLayer? EngineLayer
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

    /// <summary>Override this method to upload this layer to the engine.</summary>
    /// <remarks>Flutter's <c>Layer.addToScene</c>.</remarks>
    protected internal abstract void AddToScene(SceneBuilder builder);

    /// <remarks>Flutter's <c>Layer._addToSceneWithRetainedRendering</c>.</remarks>
    internal void AddToSceneWithRetainedRendering(SceneBuilder builder)
    {
        DebugAssertMutationsUnlocked();
        // There can't be a loop by adding a retained layer subtree whose
        // _needsAddToScene is false.
        //
        // Proof by contradiction:
        //
        // If we introduce a loop, this retained layer must be appended to one of
        // its descendant layers, say A. That means the child structure of A has
        // changed so A's _needsAddToScene is true. This contradicts
        // _needsAddToScene being false.
        if (!_needsAddToScene && _engineLayer != null)
        {
            builder.AddRetained(_engineLayer);
            return;
        }

        AddToScene(builder);
        // Clearing the flag _after_ calling `addToScene`, not _before_. This is
        // because `addToScene` calls children's `addToScene` methods, which may
        // mark this layer as dirty.
        _needsAddToScene = false;
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

    /// <summary>Consider this layer as the root and build a scene (a tree of layers) in the engine.</summary>
    /// <remarks>
    /// Flutter's <c>ContainerLayer.buildScene</c>: updates the subtree's dirty flags, adds this layer to the
    /// scene, fires the composition callbacks, marks this layer clean and returns the built scene.
    /// </remarks>
    public Scene BuildScene(SceneBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        UpdateSubtreeNeedsAddToScene();
        AddToScene(builder);
        if (SubtreeHasCompositionCallbacks)
        {
            FireCompositionCallbacks(includeChildren: true);
        }

        // Clearing the flag _after_ calling `addToScene`, not _before_. This is
        // because `addToScene` calls children's `addToScene` methods, which may
        // mark this layer as dirty.
        _needsAddToScene = false;
        Scene scene = builder.Build();
        return scene;
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

    protected internal override void AddToScene(SceneBuilder builder)
    {
        AddChildrenToScene(builder);
    }

    /// <summary>Uploads all of this layer's children to the engine.</summary>
    /// <remarks>
    /// Flutter's <c>ContainerLayer.addChildrenToScene</c>. This method is typically used by
    /// <see cref="Layer.AddToScene"/> to insert the children into the scene. Subclasses of
    /// <see cref="ContainerLayer"/> typically override <see cref="Layer.AddToScene"/> to apply effects to
    /// the scene using the <see cref="SceneBuilder"/> API, then insert their children using
    /// <see cref="AddChildrenToScene"/>, then reverse the aforementioned effects before returning.
    /// </remarks>
    public void AddChildrenToScene(SceneBuilder builder)
    {
        Layer? child = FirstChild;
        while (child != null)
        {
            child.AddToSceneWithRetainedRendering(builder);
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

    protected internal override void AddToScene(SceneBuilder builder)
    {
        if (Offset != default)
        {
            EngineLayer = builder.PushTransform(
                Matrix4.TranslationValues(Offset.X, Offset.Y, 0.0).Storage,
                oldLayer: EngineLayer as TransformEngineLayer);
        }
        else
        {
            EngineLayer = null;
        }

        AddChildrenToScene(builder);
        if (Offset != default)
        {
            builder.Pop();
        }
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

    protected internal override void AddToScene(SceneBuilder builder)
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
        if (_lastTransform != null)
        {
            _lastOffset = UnlinkedOffset;
            EngineLayer = builder.PushTransform(
                _lastTransform.Storage,
                oldLayer: EngineLayer as TransformEngineLayer);
            AddChildrenToScene(builder);
            builder.Pop();
        }
        else
        {
            _lastOffset = null;
            Matrix4 matrix = Matrix4.TranslationValues(UnlinkedOffset!.Value.X, UnlinkedOffset!.Value.Y, .0);
            EngineLayer = builder.PushTransform(matrix.Storage, oldLayer: EngineLayer as TransformEngineLayer);
            AddChildrenToScene(builder);
            builder.Pop();
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

/// <summary>
/// Plumix-only: the lens of a magnifier, drawing a magnified copy of the scene behind it
/// (docs/ai/DIVERGENCES.md, magnifier row).
/// </summary>
public sealed class MagnifierLayer : ContainerLayer
{
    private Rect _lensRect;
    private Point _focalPointOffset;
    private double _magnificationScale = 1.0;
    private MagnifierDecoration _decoration = new();
    private Clip _clipBehavior = Clip.None;

    public Rect LensRect
    {
        get => _lensRect;
        set => SetField(ref _lensRect, value);
    }

    public Point FocalPointOffset
    {
        get => _focalPointOffset;
        set => SetField(ref _focalPointOffset, value);
    }

    public double MagnificationScale
    {
        get => _magnificationScale;
        set => SetField(ref _magnificationScale, value);
    }

    public MagnifierDecoration Decoration
    {
        get => _decoration;
        set => SetField(ref _decoration, value ?? throw new ArgumentNullException(nameof(value)));
    }

    public Clip ClipBehavior
    {
        get => _clipBehavior;
        set => SetField(ref _clipBehavior, value);
    }

    protected internal override void AddToScene(SceneBuilder builder)
    {
        EngineLayer = builder.PushMagnifier(
            new MagnifierFlowLayer(LensRect, FocalPointOffset, MagnificationScale, Decoration, ClipBehavior),
            EngineLayer as MagnifierEngineLayer);
        AddChildrenToScene(builder);
        builder.Pop();
    }

    private void SetField<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        MarkNeedsAddToScene();
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

    protected internal override void AddToScene(SceneBuilder builder)
    {
        // Skia has a fast path for concatenating scale/translation only matrices.
        // Hence pushing a translation-only transform layer should be fast. For
        // retained rendering, we don't want to push the offset down to each leaf
        // node. Otherwise, changing an offset layer on the very high level could
        // cascade the change to too many leaves.
        EngineLayer = builder.PushOffset(Offset.X, Offset.Y, oldLayer: EngineLayer as OffsetEngineLayer);
        AddChildrenToScene(builder);
        builder.Pop();
    }

    /// <remarks>Flutter's <c>OffsetLayer._createSceneForImage</c>.</remarks>
    private Scene CreateSceneForImage(Rect bounds, double pixelRatio = 1.0)
    {
        var builder = new SceneBuilder();
        Matrix4 transform = Matrix4.Diagonal3Values(pixelRatio, pixelRatio, 1);
        transform.TranslateByDouble(-(bounds.Left + Offset.X), -(bounds.Top + Offset.Y), 0, 1);
        builder.PushTransform(transform.Storage);
        return BuildScene(builder);
    }

    /// <summary>Capture an image of the current state of this layer and its children.</summary>
    /// <remarks>
    /// Flutter's <c>OffsetLayer.toImage</c>. The returned image is cropped to <paramref name="bounds"/>,
    /// in this layer's coordinate system, and holds <paramref name="pixelRatio"/> pixels per logical
    /// pixel.
    /// </remarks>
    public async Task<Avalonia.Media.Imaging.Bitmap> ToImage(Rect bounds, double pixelRatio = 1.0)
    {
        Scene scene = CreateSceneForImage(bounds, pixelRatio);
        try
        {
            // Size is rounded up to the next pixel to make sure we don't clip off
            // anything.
            return await scene.ToImage(
                (int)Math.Ceiling(pixelRatio * bounds.Width),
                (int)Math.Ceiling(pixelRatio * bounds.Height)).ConfigureAwait(true);
        }
        finally
        {
            scene.Dispose();
        }
    }

    /// <summary>Capture an image of the current state of this layer and its children, synchronously.</summary>
    /// <remarks>Flutter's <c>OffsetLayer.toImageSync</c>; see <see cref="ToImage"/>.</remarks>
    public Avalonia.Media.Imaging.Bitmap ToImageSync(Rect bounds, double pixelRatio = 1.0)
    {
        Scene scene = CreateSceneForImage(bounds, pixelRatio);
        try
        {
            // Size is rounded up to the next pixel to make sure we don't clip off
            // anything.
            return scene.ToImageSync(
                (int)Math.Ceiling(pixelRatio * bounds.Width),
                (int)Math.Ceiling(pixelRatio * bounds.Height));
        }
        finally
        {
            scene.Dispose();
        }
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

    protected internal override void AddToScene(SceneBuilder builder)
    {
        Debug.Assert(Alpha != null);

        // Don't add this layer if there's no child.
        bool enabled = FirstChild != null;
        if (!enabled)
        {
            // Ensure the engine layer is disposed.
            EngineLayer = null;
            return;
        }

        if (Constants.KDebugMode)
        {
            enabled = enabled && !RenderingDebug.DisableOpacityLayers;
        }

        int realizedAlpha = Alpha!.Value;
        // The type assertions work because the [alpha] setter nulls out the
        // engineLayer if it would have changed type (i.e. changed to or from 255).
        if (enabled && realizedAlpha < 255)
        {
            Debug.Assert(EngineLayer is null or OpacityEngineLayer);
            EngineLayer = builder.PushOpacity(
                realizedAlpha,
                offset: Offset,
                oldLayer: EngineLayer as OpacityEngineLayer);
        }
        else
        {
            Debug.Assert(EngineLayer is null or OffsetEngineLayer);
            EngineLayer = builder.PushOffset(Offset.X, Offset.Y, oldLayer: EngineLayer as OffsetEngineLayer);
        }

        AddChildrenToScene(builder);
        builder.Pop();
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
    private ColorFilter? _colorFilter;
    private Rect _filterBounds;

    public ColorFilterLayer(ColorFilter? colorFilter = null)
    {
        _colorFilter = colorFilter;
    }

    /// <summary>The color filter to apply when compositing this layer's children.</summary>
    /// <remarks>The scene must be explicitly recomposited after this property is changed.</remarks>
    public ColorFilter? ColorFilter
    {
        get => _colorFilter;
        set
        {
            if (Constants.KDebugMode && value == null)
            {
                throw new AssertionError("A ColorFilterLayer needs a color filter.");
            }

            if (EqualityComparer<ColorFilter?>.Default.Equals(value, _colorFilter))
            {
                return;
            }

            _colorFilter = value;
            MarkNeedsAddToScene();
        }
    }

    /// <summary>
    /// Plumix-only: the region, in this layer's coordinates, the CPU filter backend rasterizes the
    /// children into before filtering them (docs/ai/DIVERGENCES.md, filter-layer row).
    /// </summary>
    public Rect FilterBounds
    {
        get => _filterBounds;
        set
        {
            if (value == _filterBounds)
            {
                return;
            }

            _filterBounds = value;
            MarkNeedsAddToScene();
        }
    }

    protected internal override void AddToScene(SceneBuilder builder)
    {
        Debug.Assert(ColorFilter != null);
        EngineLayer = builder.PushColorFilter(
            ColorFilter!,
            FilterBounds,
            EngineLayer as ColorFilterEngineLayer);
        AddChildrenToScene(builder);
        builder.Pop();
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<ColorFilter>("colorFilter", ColorFilter));
    }
}

/// <summary>A composite layer that applies an <see cref="Rendering.ImageFilter"/> to its children.</summary>
/// <remarks>Flutter's <c>ImageFilterLayer</c>.</remarks>
public sealed class ImageFilterLayer : OffsetLayer
{
    private ImageFilter? _imageFilter;
    private Rect _filterBounds;

    public ImageFilterLayer(ImageFilter? imageFilter = null, Point offset = default) : base(offset)
    {
        _imageFilter = imageFilter;
    }

    /// <summary>The image filter to apply when compositing this layer's children.</summary>
    /// <remarks>The scene must be explicitly recomposited after this property is changed.</remarks>
    public ImageFilter? ImageFilter
    {
        get => _imageFilter;
        set
        {
            if (Constants.KDebugMode && value == null)
            {
                throw new AssertionError("An ImageFilterLayer needs an image filter.");
            }

            if (EqualityComparer<ImageFilter?>.Default.Equals(value, _imageFilter))
            {
                return;
            }

            _imageFilter = value;
            MarkNeedsAddToScene();
        }
    }

    /// <summary>
    /// Plumix-only: the region, in the children's coordinates, the CPU filter backend rasterizes the
    /// children into before filtering them (docs/ai/DIVERGENCES.md, filter-layer row).
    /// </summary>
    public Rect FilterBounds
    {
        get => _filterBounds;
        set
        {
            if (value == _filterBounds)
            {
                return;
            }

            _filterBounds = value;
            MarkNeedsAddToScene();
        }
    }

    protected internal override void AddToScene(SceneBuilder builder)
    {
        Debug.Assert(ImageFilter != null);
        EngineLayer = builder.PushImageFilter(
            ImageFilter!,
            Offset,
            FilterBounds,
            EngineLayer as ImageFilterEngineLayer);
        AddChildrenToScene(builder);
        builder.Pop();
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<ImageFilter>("imageFilter", ImageFilter));
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/rendering/layer.dart (BackdropFilterLayer)
/// <summary>
/// A key that identifies the backdrop filter layers that should share one backdrop input.
/// </summary>
/// <remarks>Flutter's <c>BackdropKey</c>.</remarks>
public sealed class BackdropKey
{
    private static int _nextKey;

    public BackdropKey()
    {
        Id = Interlocked.Increment(ref _nextKey) - 1;
    }

    /// <summary>Dart's <c>BackdropKey._key</c>.</summary>
    internal int Id { get; }
}

/// <summary>
/// A composited layer that applies a filter to the existing contents of the scene.
/// </summary>
/// <remarks>Flutter's <c>BackdropFilterLayer</c>.</remarks>
public sealed class BackdropFilterLayer : ContainerLayer
{
    private ImageFilter? _imageFilter;
    private BlendMode _blendMode;
    private BackdropKey? _backdropKey;

    public BackdropFilterLayer(ImageFilter? filter = null, BlendMode blendMode = BlendMode.SourceOver)
    {
        _imageFilter = filter;
        _blendMode = blendMode;
    }

    /// <summary>The filter to apply to the existing contents of the scene.</summary>
    /// <remarks>Flutter's <c>BackdropFilterLayer.filter</c>.</remarks>
    public ImageFilter? ImageFilter
    {
        get => _imageFilter;
        set
        {
            if (Constants.KDebugMode && value == null)
            {
                throw new AssertionError("A BackdropFilterLayer needs a filter.");
            }

            if (EqualityComparer<ImageFilter?>.Default.Equals(value, _imageFilter))
            {
                return;
            }

            _imageFilter = value;
            MarkNeedsAddToScene();
        }
    }

    /// <summary>The blend mode to use to apply the filtered background content onto the background.</summary>
    public BlendMode BlendMode
    {
        get => _blendMode;
        set
        {
            if (value == _blendMode)
            {
                return;
            }

            _blendMode = value;
            MarkNeedsAddToScene();
        }
    }

    /// <summary>The backdrop key that identifies the backdrop filters sharing one input.</summary>
    public BackdropKey? BackdropKey
    {
        get => _backdropKey;
        set
        {
            if (ReferenceEquals(value, _backdropKey))
            {
                return;
            }

            _backdropKey = value;
            MarkNeedsAddToScene();
        }
    }

    protected internal override void AddToScene(SceneBuilder builder)
    {
        Debug.Assert(ImageFilter != null);
        EngineLayer = builder.PushBackdropFilter(
            ImageFilter!,
            blendMode: BlendMode,
            oldLayer: EngineLayer as BackdropFilterEngineLayer,
            backdropId: _backdropKey?.Id);
        AddChildrenToScene(builder);
        builder.Pop();
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
/// <summary>
/// A composited layer that applies a shader to its children; the shader is an Avalonia brush
/// (docs/ai/DIVERGENCES.md).
/// </summary>
/// <remarks>Flutter's <c>ShaderMaskLayer</c>.</remarks>
public sealed class ShaderMaskLayer : ContainerLayer
{
    private IBrush? _shader;
    private Rect? _maskRect;
    private BlendMode? _blendMode;

    public ShaderMaskLayer(IBrush? shader = null, Rect? maskRect = null, BlendMode? blendMode = null)
    {
        _shader = shader;
        _maskRect = maskRect;
        _blendMode = blendMode;
    }

    /// <summary>The shader to apply to the children.</summary>
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

    /// <summary>The position and size of the shader, in this layer's coordinates.</summary>
    public Rect? MaskRect
    {
        get => _maskRect;
        set
        {
            if (EqualityComparer<Rect?>.Default.Equals(value, _maskRect))
            {
                return;
            }

            _maskRect = value;
            MarkNeedsAddToScene();
        }
    }

    /// <summary>The blend mode to apply when blending the shader with the children.</summary>
    public BlendMode? BlendMode
    {
        get => _blendMode;
        set
        {
            if (EqualityComparer<BlendMode?>.Default.Equals(value, _blendMode))
            {
                return;
            }

            _blendMode = value;
            MarkNeedsAddToScene();
        }
    }

    protected internal override void AddToScene(SceneBuilder builder)
    {
        Debug.Assert(Shader != null);
        Debug.Assert(MaskRect != null);
        Debug.Assert(BlendMode != null);
        EngineLayer = builder.PushShaderMask(
            Shader!,
            MaskRect!.Value,
            BlendMode!.Value,
            oldLayer: EngineLayer as ShaderMaskEngineLayer);
        AddChildrenToScene(builder);
        builder.Pop();
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<IBrush>("shader", Shader));
        properties.Add(new DiagnosticsProperty<Rect?>("maskRect", MaskRect));
        properties.Add(new DiagnosticsProperty<BlendMode?>("blendMode", BlendMode));
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

    protected internal override void AddToScene(SceneBuilder builder)
    {
        bool enabled = true;
        if (Constants.KDebugMode)
        {
            enabled = !RenderingDebug.DisableClipLayers;
        }

        if (enabled)
        {
            EngineLayer = builder.PushClipRect(
                ClipRect,
                clipBehavior: ClipBehavior,
                oldLayer: EngineLayer as ClipRectEngineLayer);
        }
        else
        {
            EngineLayer = null;
        }

        AddChildrenToScene(builder);
        if (enabled)
        {
            builder.Pop();
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

    protected internal override void AddToScene(SceneBuilder builder)
    {
        bool enabled = true;
        if (Constants.KDebugMode)
        {
            enabled = !RenderingDebug.DisableClipLayers;
        }

        if (enabled)
        {
            EngineLayer = builder.PushClipRRect(
                ClipRRect,
                clipBehavior: ClipBehavior,
                oldLayer: EngineLayer as ClipRRectEngineLayer);
        }
        else
        {
            EngineLayer = null;
        }

        AddChildrenToScene(builder);
        if (enabled)
        {
            builder.Pop();
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

    protected internal override void AddToScene(SceneBuilder builder)
    {
        bool enabled = true;
        if (Constants.KDebugMode)
        {
            enabled = !RenderingDebug.DisableClipLayers;
        }

        if (enabled)
        {
            EngineLayer = builder.PushClipRSuperellipse(
                ClipRSuperellipse,
                clipBehavior: ClipBehavior,
                oldLayer: EngineLayer as ClipRSuperellipseEngineLayer);
        }
        else
        {
            EngineLayer = null;
        }

        AddChildrenToScene(builder);
        if (enabled)
        {
            builder.Pop();
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

    protected internal override void AddToScene(SceneBuilder builder)
    {
        bool enabled = true;
        if (Constants.KDebugMode)
        {
            enabled = !RenderingDebug.DisableClipLayers;
        }

        if (enabled)
        {
            EngineLayer = builder.PushClipPath(
                ClipPath,
                clipBehavior: ClipBehavior,
                oldLayer: EngineLayer as ClipPathEngineLayer);
        }
        else
        {
            EngineLayer = null;
        }

        AddChildrenToScene(builder);
        if (enabled)
        {
            builder.Pop();
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
    private Geometry _geometry = new RectangleGeometry();
    private Clip _clipBehavior = Clip.AntiAlias;
    private Point _geometryOffset;

    public Geometry Geometry
    {
        get => _geometry;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(value, _geometry))
            {
                return;
            }

            _geometry = value;
            MarkNeedsAddToScene();
        }
    }

    public Clip ClipBehavior
    {
        get => _clipBehavior;
        set
        {
            if (value == _clipBehavior)
            {
                return;
            }

            _clipBehavior = value;
            MarkNeedsAddToScene();
        }
    }

    public Point GeometryOffset
    {
        get => _geometryOffset;
        set
        {
            if (value == _geometryOffset)
            {
                return;
            }

            _geometryOffset = value;
            MarkNeedsAddToScene();
        }
    }

    protected internal override void AddToScene(SceneBuilder builder)
    {
        bool enabled = true;
        if (Constants.KDebugMode)
        {
            enabled = !RenderingDebug.DisableClipLayers;
        }

        if (enabled)
        {
            EngineLayer = builder.PushClipGeometry(
                Geometry,
                GeometryOffset,
                ClipBehavior,
                EngineLayer as ClipGeometryEngineLayer);
        }
        else
        {
            EngineLayer = null;
        }

        AddChildrenToScene(builder);
        if (enabled)
        {
            builder.Pop();
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

    protected internal override void AddToScene(SceneBuilder builder)
    {
        _lastEffectiveTransform = Transform;
        if (Offset != default)
        {
            _lastEffectiveTransform = Matrix4.TranslationValues(Offset.X, Offset.Y, 0.0);
            _lastEffectiveTransform.Multiply(Transform);
        }

        EngineLayer = builder.PushTransform(
            _lastEffectiveTransform.Storage,
            oldLayer: EngineLayer as TransformEngineLayer);
        AddChildrenToScene(builder);
        builder.Pop();
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
            _picture?.Dispose();
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

    /// <inheritdoc />
    protected internal override void Dispose()
    {
        Picture = null;
        base.Dispose();
    }

    protected internal override void AddToScene(SceneBuilder builder)
    {
        Debug.Assert(Picture != null);
        builder.AddPicture(
            default,
            Picture!,
            isComplexHint: IsComplexHint,
            willChangeHint: WillChangeHint);
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

// Dart parity source: flutter/packages/flutter/lib/src/rendering/layer.dart
/// <summary>A composited layer that maps a backend texture to a rectangle.</summary>
/// <remarks>
/// Flutter's <c>TextureLayer</c>. Backend textures are images that can be applied (mapped) to an area
/// of the Flutter view. They are created, managed, and updated using a platform-specific texture
/// registry (<see cref="TextureRegistry"/>). A texture layer can be assigned a frozen state, in which
/// it keeps showing the frame it last painted.
/// </remarks>
public sealed class TextureLayer : Layer
{
    public TextureLayer(
        Rect rect,
        int textureId,
        bool freeze = false,
        FilterQuality filterQuality = FilterQuality.Low)
    {
        Rect = rect;
        TextureId = textureId;
        Freeze = freeze;
        FilterQuality = filterQuality;
    }

    /// <summary>Bounding rectangle of this layer.</summary>
    public Rect Rect { get; }

    /// <summary>The identity of the backend texture.</summary>
    public int TextureId { get; }

    /// <summary>When true the texture will not be updated with new frames.</summary>
    /// <remarks>
    /// This is used for resizing embedded Android views: when resizing there is a short period during
    /// which the framework cannot tell if the newest texture frame has the previous or new size; to
    /// work around this, the framework "freezes" the texture just before resizing the Android view and
    /// un-freezes it when it is certain that a frame with the new size is ready.
    /// </remarks>
    public bool Freeze { get; }

    /// <summary>The quality of sampling the texture and rendering it on screen.</summary>
    public FilterQuality FilterQuality { get; }

    protected internal override void AddToScene(SceneBuilder builder)
    {
        builder.AddTexture(
            TextureId,
            offset: Rect.TopLeft,
            width: Rect.Width,
            height: Rect.Height,
            freeze: Freeze,
            filterQuality: FilterQuality);
    }

    protected internal override bool FindAnnotations<T>(
        AnnotationResult<T> result,
        Point localPosition,
        bool onlyFirst)
    {
        return false;
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/rendering/layer.dart
/// <summary>A layer that shows an embedded UIView on iOS.</summary>
/// <remarks>Flutter's <c>PlatformViewLayer</c>; no Plumix host embeds platform views yet.</remarks>
public sealed class PlatformViewLayer : Layer
{
    public PlatformViewLayer(Rect rect, int viewId)
    {
        Rect = rect;
        ViewId = viewId;
    }

    /// <summary>Bounding rectangle of this layer in the global coordinate space.</summary>
    public Rect Rect { get; }

    /// <summary>The unique identifier of the UIView displayed on this layer.</summary>
    /// <remarks>
    /// A UIView with this identifier must have been created by <c>PlatformViewsServices.initUiKitView</c>.
    /// </remarks>
    public int ViewId { get; }

    /// <inheritdoc />
    public override bool SupportsRasterization() => false;

    protected internal override void AddToScene(SceneBuilder builder)
    {
        builder.AddPlatformView(ViewId, offset: Rect.TopLeft, width: Rect.Width, height: Rect.Height);
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/rendering/layer.dart
/// <summary>
/// A layer that indicates to the compositor that it should display certain performance statistics within it.
/// </summary>
/// <remarks>
/// Flutter's <c>PerformanceOverlayLayer</c>. Performance overlay layers are always leaves in the layer
/// tree.
/// </remarks>
public sealed class PerformanceOverlayLayer : Layer
{
    private Rect _overlayRect;

    public PerformanceOverlayLayer(Rect overlayRect, int optionsMask)
    {
        _overlayRect = overlayRect;
        OptionsMask = optionsMask;
    }

    /// <summary>The rectangle in this layer's coordinate system that the overlay should occupy.</summary>
    /// <remarks>The scene must be explicitly recomposited after this property is changed.</remarks>
    public Rect OverlayRect
    {
        get => _overlayRect;
        set
        {
            if (value != _overlayRect)
            {
                _overlayRect = value;
                MarkNeedsAddToScene();
            }
        }
    }

    /// <summary>
    /// The mask is created by shifting 1 by the index of the specific <see cref="PerformanceOverlayOption"/>
    /// to enable.
    /// </summary>
    public int OptionsMask { get; }

    protected internal override void AddToScene(SceneBuilder builder)
    {
        builder.AddPerformanceOverlay(OptionsMask, OverlayRect);
    }

    protected internal override bool FindAnnotations<T>(
        AnnotationResult<T> result,
        Point localPosition,
        bool onlyFirst)
    {
        return false;
    }
}
