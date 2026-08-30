using System.Diagnostics;
using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/overlay.dart

public delegate Widget OverlayWidgetBuilder(BuildContext context);

public delegate Widget OverlayChildLayoutBuilder(
    BuildContext context,
    OverlayChildLayoutInfo info);

public sealed record OverlayChildLayoutInfo(
    Size ChildSize,
    Matrix4 ChildPaintTransform,
    Size OverlaySize);

public enum OverlayChildLocation
{
    NearestOverlay,
    RootOverlay,
}

public sealed class OverlayPortalController
{
    private static long _wallTime = long.MinValue;
    private OverlayPortalState? _attachTarget;
    private long? _zOrderIndex;

    public OverlayPortalController(string? debugLabel = null)
    {
        DebugLabel = debugLabel;
    }

    public string? DebugLabel { get; }

    public bool IsShowing => _attachTarget?.ZOrderIndex is not null || _zOrderIndex is not null;

    public void Show()
    {
        long zOrderIndex = Interlocked.Increment(ref _wallTime);
        if (_attachTarget is not null)
        {
            _attachTarget.Show(zOrderIndex);
        }
        else
        {
            _zOrderIndex = zOrderIndex;
        }
    }

    public void Hide()
    {
        if (_attachTarget is not null)
        {
            _attachTarget.Hide();
        }
        else
        {
            _zOrderIndex = null;
        }
    }

    public void Toggle()
    {
        if (IsShowing)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    public override string ToString()
    {
        string label = DebugLabel is null ? string.Empty : $"({DebugLabel})";
        string detached = _attachTarget is null ? " DETACHED" : string.Empty;
        return $"{nameof(OverlayPortalController)}{label}{detached}";
    }

    internal long? TakeDetachedZOrder()
    {
        long? zOrderIndex = _zOrderIndex;
        _zOrderIndex = null;
        return zOrderIndex;
    }

    internal void Attach(OverlayPortalState state)
    {
        if (_attachTarget is { Mounted: true } && !ReferenceEquals(_attachTarget, state))
        {
            throw new InvalidOperationException(
                $"{this} is already attached to another active OverlayPortal.");
        }

        _attachTarget = state;
    }

    internal void Detach(OverlayPortalState state)
    {
        if (ReferenceEquals(_attachTarget, state))
        {
            _attachTarget = null;
        }
    }
}

public sealed class OverlayPortal : StatefulWidget
{
    private readonly OverlayChildLayoutBuilder? _overlayChildLayoutBuilder;

    public OverlayPortal(
        OverlayPortalController controller,
        WidgetBuilder overlayChildBuilder,
        OverlayChildLocation overlayLocation = OverlayChildLocation.NearestOverlay,
        Widget? child = null,
        Key? key = null) : base(key)
    {
        Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        OverlayChildBuilder = overlayChildBuilder ?? throw new ArgumentNullException(nameof(overlayChildBuilder));
        OverlayLocation = overlayLocation;
        Child = child;
    }

    private OverlayPortal(
        OverlayPortalController controller,
        OverlayChildLayoutBuilder overlayChildLayoutBuilder,
        OverlayChildLocation overlayLocation,
        Widget? child,
        Key? key) : base(key)
    {
        Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        _overlayChildLayoutBuilder = overlayChildLayoutBuilder
                                     ?? throw new ArgumentNullException(nameof(overlayChildLayoutBuilder));
        OverlayChildBuilder = _ => throw new InvalidOperationException(
            "The layout-builder OverlayPortal uses OverlayChildLayoutBuilder.");
        OverlayLocation = overlayLocation;
        Child = child;
    }

    public OverlayPortalController Controller { get; }

    public WidgetBuilder OverlayChildBuilder { get; }

    public OverlayChildLocation OverlayLocation { get; }

    public Widget? Child { get; }

    public static OverlayPortal WithLayoutBuilder(
        OverlayPortalController controller,
        OverlayChildLayoutBuilder overlayChildBuilder,
        OverlayChildLocation overlayLocation = OverlayChildLocation.NearestOverlay,
        Widget? child = null,
        Key? key = null)
    {
        return new OverlayPortal(
            controller,
            overlayChildBuilder,
            overlayLocation,
            child,
            key);
    }

    [Obsolete("Use the OverlayLocation parameter with OverlayChildLocation.RootOverlay.")]
    public static OverlayPortal TargetsRootOverlay(
        OverlayPortalController controller,
        WidgetBuilder overlayChildBuilder,
        Widget? child = null,
        Key? key = null)
    {
        return new OverlayPortal(
            controller,
            overlayChildBuilder,
            OverlayChildLocation.RootOverlay,
            child,
            key);
    }

    public override State CreateState() => new OverlayPortalState();

    internal OverlayChildLayoutBuilder? LayoutBuilder => _overlayChildLayoutBuilder;
}

internal sealed class OverlayPortalState : State
{
    private long? _zOrderIndex;

    /// <remarks>Flutter's <c>_OverlayPortalState._childModelMayHaveChanged</c>: set whenever an
    /// inherited dependency or the requested overlay changed, so the cached location is re-validated
    /// against a fresh <see cref="RenderTheaterMarker"/> lookup instead of being trusted.</remarks>
    private bool _childModelMayHaveChanged = true;

    private OverlayEntryLocation? _locationCache;

    private OverlayPortal CurrentWidget => (OverlayPortal)StateWidget;

    internal long? ZOrderIndex => _zOrderIndex;

    public override void InitState()
    {
        base.InitState();
        SetupController(CurrentWidget.Controller);
    }

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        _childModelMayHaveChanged = true;
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var oldPortal = (OverlayPortal)oldWidget;
        _childModelMayHaveChanged = _childModelMayHaveChanged
                                    || oldPortal.OverlayLocation != CurrentWidget.OverlayLocation;
        if (!ReferenceEquals(oldPortal.Controller, CurrentWidget.Controller))
        {
            oldPortal.Controller.Detach(this);
            SetupController(CurrentWidget.Controller);
        }
    }

    public override void Dispose()
    {
        CurrentWidget.Controller.Detach(this);
        _locationCache?.DebugMarkLocationInvalid();
        _locationCache = null;
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        // The portal's own subtree names this state as its traversal parent, and the overlay child's
        // deferred layout box names the same object as its traversal child identifier, so the semantics
        // owner grafts the overlay child under the portal wherever the theater paints it.
        var child = new Semantics(
            traversalParentIdentifier: this,
            child: CurrentWidget.Child);
        if (!_zOrderIndex.HasValue)
        {
            return new OverlayPortalRenderWidget(
                child: child,
                overlayChild: null,
                location: null);
        }

        OverlayEntryLocation location = GetLocation(_zOrderIndex.Value, CurrentWidget.OverlayLocation);
        Widget overlayChild = CurrentWidget.LayoutBuilder is { } layoutBuilder
            ? new OverlayPortalLayoutBuilderWidget(layoutBuilder)
            : new Builder(CurrentWidget.OverlayChildBuilder);
        overlayChild = WrapWithOverlayMediaQuery(
            context,
            location.ChildModel.Context,
            overlayChild);
        return new OverlayPortalRenderWidget(
            child: child,
            overlayChild: new DeferredLayout(overlayChild, childIdentifier: this),
            location: location);
    }

    internal void Show(long zOrderIndex)
    {
        SetState(() => _zOrderIndex = zOrderIndex);
        _locationCache?.DebugMarkLocationInvalid();
        _locationCache = null;
    }

    internal void Hide()
    {
        SetState(() => _zOrderIndex = null);
        _locationCache?.DebugMarkLocationInvalid();
        _locationCache = null;
    }

    /// <remarks>
    /// Flutter's <c>_OverlayPortalState._getLocation</c>. The marker lookup is deliberately deferred:
    /// when nothing can have changed the cache is returned without creating a new dependency, exactly
    /// as Dart's <c>late final marker</c> does.
    /// </remarks>
    private OverlayEntryLocation GetLocation(long zOrderIndex, OverlayChildLocation overlayLocation)
    {
        OverlayEntryLocation? cachedLocation = _locationCache;
        bool targetRootOverlay = overlayLocation == OverlayChildLocation.RootOverlay;
        RenderTheaterMarker? marker = null;
        bool isCacheValid = cachedLocation is not null;
        if (isCacheValid && _childModelMayHaveChanged)
        {
            marker = RenderTheaterMarker.Of(Context, targetRootOverlay);
            isCacheValid = IsTheSameLocation(cachedLocation!, marker);
        }

        _childModelMayHaveChanged = false;
        if (isCacheValid)
        {
            Debug.Assert(cachedLocation!.ZOrderIndex == zOrderIndex);
            Debug.Assert(cachedLocation.DebugIsLocationValid());
            return cachedLocation;
        }

        marker ??= RenderTheaterMarker.Of(Context, targetRootOverlay);
        cachedLocation?.DebugMarkLocationInvalid();
        var newLocation = new OverlayEntryLocation(zOrderIndex, marker.EntryState, marker.Theater);
        return _locationCache = newLocation;
    }

    private static bool IsTheSameLocation(OverlayEntryLocation locationCache, RenderTheaterMarker marker)
    {
        return ReferenceEquals(locationCache.ChildModel, marker.EntryState)
               && ReferenceEquals(locationCache.Theater, marker.Theater);
    }

    private void SetupController(OverlayPortalController controller)
    {
        long? controllerZOrderIndex = controller.TakeDetachedZOrder();
        if (!_zOrderIndex.HasValue
            || controllerZOrderIndex.HasValue && controllerZOrderIndex.Value > _zOrderIndex.Value)
        {
            _zOrderIndex = controllerZOrderIndex;
        }

        controller.Attach(this);
    }

    private static Widget WrapWithOverlayMediaQuery(
        BuildContext portalContext,
        BuildContext overlayContext,
        Widget child)
    {
        MediaQueryData? portalData = MediaQuery.MaybeOf(portalContext);
        MediaQueryData? overlayData = overlayContext.GetInherited<MediaQuery>()?.Data;
        if (portalData is null || overlayData is null)
        {
            return child;
        }

        return new MediaQuery(
            data: portalData.CopyWith(
                padding: overlayData.Padding,
                viewInsets: overlayData.ViewInsets,
                viewPadding: overlayData.ViewPadding),
            child: child);
    }
}

/// <summary>
/// A cursor into one <see cref="OverlayEntry"/>'s child model: it names the overlay an
/// <see cref="OverlayPortal"/>'s overlay child goes into and, through its z-order index, the child's
/// paint order among the other overlay children hosted on the same entry.
/// </summary>
/// <remarks>
/// Flutter's <c>_OverlayEntryLocation</c>, a <c>LinkedListEntry</c> in the hosting entry's sorted
/// sibling list. It is deliberately mutable and identity-compared - it is used as an element slot, so
/// one instance must never stand for two locations.
/// </remarks>
internal sealed class OverlayEntryLocation
{
    private StackTrace? _debugMarkLocationInvalidStackTrace;

    internal OverlayEntryLocation(
        long zOrderIndex,
        OverlayEntryWidgetState childModel,
        RenderOverlayTheater theater)
    {
        ZOrderIndex = zOrderIndex;
        ChildModel = childModel;
        Theater = theater;
    }

    internal long ZOrderIndex { get; }

    internal OverlayEntryWidgetState ChildModel { get; }

    internal RenderOverlayTheater Theater { get; }

    /// <summary>The box occupying this location, or <see langword="null"/> while the location is
    /// unoccupied or its portal is detached from its layout surrogate.</summary>
    internal RenderDeferredLayoutBox? OverlayChildRenderBox { get; private set; }

    internal OverlayEntryLocation? Previous { get; set; }

    internal OverlayEntryLocation? Next { get; set; }

    internal OverlayEntryLocationList? List { get; set; }

    /// <remarks>Flutter's <c>_OverlayEntryLocation._addToChildModel</c>.</remarks>
    internal void AddToChildModel(RenderDeferredLayoutBox child)
    {
        Debug.Assert(
            OverlayChildRenderBox is null,
            $"Failed to add {child}. This location ({this}) is already occupied.");
        OverlayChildRenderBox = child;
        ChildModel.Add(this);
        Theater.MarkNeedsPaint();
        Theater.MarkNeedsCompositingBitsUpdate();
        Theater.MarkNeedsSemanticsUpdate();
    }

    /// <remarks>Flutter's <c>_OverlayEntryLocation._removeFromChildModel</c>.</remarks>
    internal void RemoveFromChildModel(RenderDeferredLayoutBox child)
    {
        Debug.Assert(ReferenceEquals(child, OverlayChildRenderBox));
        OverlayChildRenderBox = null;
        ChildModel.Remove(this);
        Theater.MarkNeedsPaint();
        Theater.MarkNeedsCompositingBitsUpdate();
        Theater.MarkNeedsSemanticsUpdate();
    }

    /// <remarks>Flutter's <c>_OverlayEntryLocation._addChild</c>.</remarks>
    internal void AddChild(RenderDeferredLayoutBox child)
    {
        Debug.Assert(DebugIsLocationValid());
        AddToChildModel(child);
        Theater.AddDeferredChild(child);
        Debug.Assert(ReferenceEquals(child.Parent, Theater));
    }

    /// <remarks>Flutter's <c>_OverlayEntryLocation._removeChild</c>. Legal even after the location has
    /// been invalidated: it runs while the portal is being torn down.</remarks>
    internal void RemoveChild(RenderDeferredLayoutBox child)
    {
        RemoveFromChildModel(child);
        Theater.RemoveDeferredChild(child);
        Debug.Assert(child.Parent is null);
    }

    /// <remarks>
    /// Flutter's <c>_OverlayEntryLocation._moveChild</c>. The theater move and the child-model move are
    /// independent: staying in the same theater but changing entry or z-order only relinks the sorted
    /// sibling list, and staying in the same location does nothing at all.
    /// </remarks>
    internal void MoveChild(RenderDeferredLayoutBox child, OverlayEntryLocation fromLocation)
    {
        Debug.Assert(!ReferenceEquals(fromLocation, this));
        Debug.Assert(DebugIsLocationValid());
        RenderOverlayTheater fromTheater = fromLocation.Theater;
        OverlayEntryWidgetState fromModel = fromLocation.ChildModel;

        if (!ReferenceEquals(fromTheater, Theater))
        {
            fromTheater.RemoveDeferredChild(child);
            Theater.AddDeferredChild(child);
        }

        if (!ReferenceEquals(fromModel, ChildModel) || fromLocation.ZOrderIndex != ZOrderIndex)
        {
            fromLocation.RemoveFromChildModel(child);
            AddToChildModel(child);
        }
    }

    /// <remarks>Flutter's <c>_OverlayEntryLocation._reattachFromLayoutSurrogate</c>: the location keeps
    /// its place in the sorted sibling list the whole time, so a reactivated portal lands back in the
    /// same paint order without re-inserting anything.</remarks>
    internal void ReattachFromLayoutSurrogate(RenderDeferredLayoutBox child)
    {
        Debug.Assert(
            OverlayChildRenderBox is null,
            $"{this} failed to reattach: DetachFromLayoutSurrogate must run first.");
        Theater.AddDeferredChild(child);
        OverlayChildRenderBox = child;
    }

    /// <remarks>Flutter's <c>_OverlayEntryLocation._detachFromLayoutSurrogate</c>.</remarks>
    internal void DetachFromLayoutSurrogate(RenderDeferredLayoutBox child)
    {
        Theater.RemoveDeferredChild(child);
        OverlayChildRenderBox = null;
    }

    /// <remarks>Flutter's <c>_OverlayEntryLocation._debugIsLocationValid</c>.</remarks>
    internal bool DebugIsLocationValid()
    {
        if (_debugMarkLocationInvalidStackTrace is null)
        {
            return true;
        }

        throw new InvalidOperationException(
            $"{this} is already disposed. Stack trace: {_debugMarkLocationInvalidStackTrace}");
    }

    /// <remarks>Flutter's <c>_OverlayEntryLocation._debugMarkLocationInvalid</c>: irreversible, and
    /// called whenever the owning portal drops its cached location.</remarks>
    internal void DebugMarkLocationInvalid()
    {
        Debug.Assert(DebugIsLocationValid());
        if (Constants.KDebugMode)
        {
            _debugMarkLocationInvalidStackTrace = new StackTrace();
        }
    }

    public override string ToString()
    {
        string invalid = _debugMarkLocationInvalidStackTrace is null ? string.Empty : " (INVALID)";
        return $"{nameof(OverlayEntryLocation)}[z-order {ZOrderIndex}]{invalid}";
    }
}

/// <summary>
/// The sorted sibling list one <see cref="OverlayEntry"/> keeps of the overlay children hosted on it.
/// </summary>
/// <remarks>
/// Stands in for the <c>dart:collection</c> <c>LinkedList</c> Flutter uses: .NET's
/// <c>LinkedList&lt;T&gt;</c> wraps values in nodes, while Dart's entries carry their own links, which
/// is what lets a walk advance past an entry the consumer is about to unlink.
/// </remarks>
internal sealed class OverlayEntryLocationList
{
    internal OverlayEntryLocation? First { get; private set; }

    internal OverlayEntryLocation? Last { get; private set; }

    internal bool IsEmpty => First is null;

    internal void AddFirst(OverlayEntryLocation entry)
    {
        Debug.Assert(entry.List is null);
        entry.List = this;
        entry.Previous = null;
        entry.Next = First;
        if (First is null)
        {
            Last = entry;
        }
        else
        {
            First.Previous = entry;
        }

        First = entry;
    }

    internal void InsertAfter(OverlayEntryLocation position, OverlayEntryLocation entry)
    {
        Debug.Assert(ReferenceEquals(position.List, this));
        Debug.Assert(entry.List is null);
        entry.List = this;
        entry.Previous = position;
        entry.Next = position.Next;
        if (position.Next is null)
        {
            Last = entry;
        }
        else
        {
            position.Next.Previous = entry;
        }

        position.Next = entry;
    }

    internal bool Remove(OverlayEntryLocation entry)
    {
        if (!ReferenceEquals(entry.List, this))
        {
            return false;
        }

        if (entry.Previous is null)
        {
            First = entry.Next;
        }
        else
        {
            entry.Previous.Next = entry.Next;
        }

        if (entry.Next is null)
        {
            Last = entry.Previous;
        }
        else
        {
            entry.Next.Previous = entry.Previous;
        }

        entry.Previous = null;
        entry.Next = null;
        entry.List = null;
        return true;
    }

    internal bool Contains(OverlayEntryLocation entry) => ReferenceEquals(entry.List, this);
}

public sealed class OverlayEntry : IListenable, IDisposable
{
    private OverlayState? _overlay;
    private bool _disposed;
    private bool _opaque;
    private bool _maintainState;
    private OverlayEntryWidgetState? _widgetState;
    private readonly List<Action> _listeners = [];

    public OverlayEntry(
        OverlayWidgetBuilder builder,
        bool opaque = false,
        bool maintainState = false,
        bool canSizeOverlay = false)
    {
        Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _opaque = opaque;
        _maintainState = maintainState;
        CanSizeOverlay = canSizeOverlay;
    }

    public OverlayWidgetBuilder Builder { get; }

    public bool Opaque
    {
        get => _opaque;
        set
        {
            ThrowIfDisposed();
            if (_opaque == value)
            {
                return;
            }

            // Settable before insertion: a route stamps its opacity while installing, before the navigator
            // hands its entries to the overlay.
            _opaque = value;
            _overlay?.MarkDirty();
        }
    }

    public bool MaintainState
    {
        get => _maintainState;
        set
        {
            ThrowIfDisposed();
            if (_maintainState == value)
            {
                return;
            }

            RequireOverlay();
            _maintainState = value;
            _overlay!.MarkDirty();
        }
    }

    public bool CanSizeOverlay { get; }

    public bool Mounted => _widgetState is not null;

    /// <summary>The state of the widget built for this entry, and the owner of the sorted list of
    /// overlay children hosted on it.</summary>
    /// <remarks>Flutter's <c>OverlayEntry._overlayEntryStateNotifier</c>.</remarks>
    internal OverlayEntryWidgetState? WidgetState => _widgetState;

    /// <summary>Whether this entry currently belongs to an overlay.</summary>
    public bool IsInserted => _overlay is not null;

    internal OverlayState? Owner => _overlay;

    internal event Action? Changed;

    public void MarkNeedsBuild()
    {
        ThrowIfDisposed();
        Changed?.Invoke();
    }

    public void Remove()
    {
        ThrowIfDisposed();
        RequireOverlay();
        OverlayState overlay = _overlay!;
        _overlay = null;
        overlay.Remove(this);
    }

    public void AddListener(Action listener)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(listener);
        _listeners.Add(listener);
    }

    public void RemoveListener(Action listener)
    {
        if (_disposed)
        {
            return;
        }

        _listeners.Remove(listener);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_overlay is not null)
        {
            throw new InvalidOperationException("An OverlayEntry must be removed before it is disposed.");
        }

        _disposed = true;
        if (!Mounted)
        {
            ReleaseListeners();
        }
    }

    internal void Attach(OverlayState overlay)
    {
        ThrowIfDisposed();
        if (_overlay is not null)
        {
            throw new InvalidOperationException("The OverlayEntry is already present in an Overlay.");
        }

        _overlay = overlay;
    }

    internal void Detach(OverlayState overlay)
    {
        if (ReferenceEquals(_overlay, overlay))
        {
            _overlay = null;
        }
    }

    internal void SetWidgetState(OverlayEntryWidgetState? state)
    {
        if (ReferenceEquals(_widgetState, state))
        {
            return;
        }

        _widgetState = state;
        foreach (Action listener in _listeners.ToArray())
        {
            listener();
        }

        if (state is null && _disposed)
        {
            ReleaseListeners();
        }
    }

    private void RequireOverlay()
    {
        if (_overlay is null)
        {
            throw new InvalidOperationException("The OverlayEntry is not present in an Overlay.");
        }
    }

    private void ReleaseListeners()
    {
        Changed = null;
        _listeners.Clear();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}

public sealed class Overlay : StatefulWidget
{
    public Overlay(
        IReadOnlyList<OverlayEntry>? initialEntries = null,
        Clip clipBehavior = Clip.HardEdge,
        bool alwaysSizeToContent = false,
        Key? key = null) : base(key)
    {
        InitialEntries = initialEntries ?? [];
        if (InitialEntries.Count != InitialEntries.Distinct().Count())
        {
            throw new ArgumentException("Overlay initial entries must be unique.", nameof(initialEntries));
        }

        ClipBehavior = clipBehavior;
        AlwaysSizeToContent = alwaysSizeToContent;
    }

    public IReadOnlyList<OverlayEntry> InitialEntries { get; }

    public Clip ClipBehavior { get; }

    public bool AlwaysSizeToContent { get; }

    public static Widget Wrap(
        Widget child,
        Clip clipBehavior = Clip.HardEdge,
        bool alwaysSizeToContent = false,
        Key? key = null)
    {
        ArgumentNullException.ThrowIfNull(child);
        return new WrappingOverlay(
            child,
            clipBehavior,
            alwaysSizeToContent,
            key);
    }

    public static OverlayState Of(BuildContext context, bool rootOverlay = false)
    {
        return MaybeOf(context, rootOverlay)
               ?? throw new InvalidOperationException("No Overlay ancestor was found.");
    }

    public static OverlayState? MaybeOf(BuildContext context, bool rootOverlay = false)
    {
        return rootOverlay
            ? context.FindRootAncestorStateOfType<OverlayState>()
            : context.FindAncestorStateOfType<OverlayState>();
    }

    public override State CreateState() => new OverlayState();
}

public sealed class OverlayState : State
{
    private readonly List<OverlayEntry> _entries = [];

    private Overlay CurrentWidget => (Overlay)StateWidget;

    public IReadOnlyList<OverlayEntry> Entries => _entries;

    public override void InitState()
    {
        base.InitState();
        foreach (OverlayEntry entry in CurrentWidget.InitialEntries)
        {
            entry.Attach(this);
            _entries.Add(entry);
        }
    }

    public override void Dispose()
    {
        foreach (OverlayEntry entry in _entries)
        {
            entry.Detach(this);
        }

        _entries.Clear();
        base.Dispose();
    }

    public void Insert(OverlayEntry entry, OverlayEntry? below = null, OverlayEntry? above = null)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (below is not null && above is not null)
        {
            throw new ArgumentException("Only one of below and above may be specified.");
        }

        int index = ResolveInsertionIndex(below, above);
        entry.Attach(this);
        SetState(() => _entries.Insert(index, entry));
    }

    public void InsertAll(
        IReadOnlyList<OverlayEntry> entries,
        OverlayEntry? below = null,
        OverlayEntry? above = null)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (below is not null && above is not null)
        {
            throw new ArgumentException("Only one of below and above may be specified.");
        }

        if (entries.Count != entries.Distinct().Count())
        {
            throw new ArgumentException("Overlay entries must be unique.", nameof(entries));
        }

        foreach (OverlayEntry entry in entries)
        {
            if (entry.Owner is not null)
            {
                throw new InvalidOperationException("An entry is already present in an Overlay.");
            }
        }

        if (entries.Count == 0)
        {
            return;
        }

        int index = ResolveInsertionIndex(below, above);
        foreach (OverlayEntry entry in entries)
        {
            entry.Attach(this);
        }

        SetState(() => _entries.InsertRange(index, entries));
    }

    public void Rearrange(
        IReadOnlyList<OverlayEntry> newEntries,
        OverlayEntry? below = null,
        OverlayEntry? above = null)
    {
        ArgumentNullException.ThrowIfNull(newEntries);
        ValidatePositionArguments(below, above);
        if (newEntries.Count == 0)
        {
            return;
        }

        if (newEntries.Count != newEntries.Distinct().Count())
        {
            throw new ArgumentException("Overlay entries must be unique.", nameof(newEntries));
        }

        if (below is not null && !newEntries.Contains(below))
        {
            throw new ArgumentException("The below entry must be present in newEntries.", nameof(below));
        }

        if (above is not null && !newEntries.Contains(above))
        {
            throw new ArgumentException("The above entry must be present in newEntries.", nameof(above));
        }

        foreach (OverlayEntry entry in newEntries)
        {
            if (entry.Owner is not null && !ReferenceEquals(entry.Owner, this))
            {
                throw new InvalidOperationException("An entry is already present in another Overlay.");
            }
        }

        if (_entries.SequenceEqual(newEntries))
        {
            return;
        }

        var oldEntries = _entries.Where(entry => !newEntries.Contains(entry)).ToList();
        foreach (OverlayEntry entry in newEntries)
        {
            if (entry.Owner is null)
            {
                entry.Attach(this);
            }
        }

        SetState(() =>
        {
            _entries.Clear();
            _entries.AddRange(newEntries);
            int insertionIndex = ResolveInsertionIndex(below, above);
            _entries.InsertRange(insertionIndex, oldEntries);
        });
    }

    public bool DebugIsVisible(OverlayEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        int entryIndex = _entries.IndexOf(entry);
        if (entryIndex < 0)
        {
            throw new ArgumentException("The entry is not present in this Overlay.", nameof(entry));
        }

        for (int index = _entries.Count - 1; index > entryIndex; index--)
        {
            if (_entries[index].Opaque)
            {
                return false;
            }
        }

        return true;
    }

    internal void Remove(OverlayEntry entry)
    {
        if (!_entries.Remove(entry))
        {
            return;
        }

        if (global::Plumix.Scheduler.Phase == global::Plumix.SchedulerPhase.PersistentCallbacks)
        {
            // OverlayEntry.remove is legal during build; Dart defers only the dirty notification.
            global::Plumix.Scheduler.AddPostFrameCallback(_ => MarkDirty());
            return;
        }

        MarkDirty();
    }

    internal void MarkDirty()
    {
        if (Mounted)
        {
            SetState(static () => { });
        }
    }

    public override Widget Build(BuildContext context)
    {
        bool onstage = true;
        int onstageCount = 0;
        var children = new List<Widget>();
        for (int index = _entries.Count - 1; index >= 0; index--)
        {
            OverlayEntry entry = _entries[index];
            if (onstage)
            {
                onstageCount += 1;
                children.Add(BuildEntry(entry, tickerEnabled: true));

                if (entry.Opaque)
                {
                    onstage = false;
                }
            }
            else if (entry.MaintainState)
            {
                children.Add(BuildEntry(entry, tickerEnabled: false));
            }
        }

        children.Reverse();
        return new OverlayTheater(
            skipCount: children.Count - onstageCount,
            clipBehavior: CurrentWidget.ClipBehavior,
            alwaysSizeToContent: CurrentWidget.AlwaysSizeToContent,
            children: children);
    }

    private static Widget BuildEntry(OverlayEntry entry, bool tickerEnabled)
    {
        // The theater's children change position (and count) as entries go offstage, so the entry's own
        // identity has to key the widget or an element would be re-targeted onto another entry. Dart
        // spells this `entry._key`, a GlobalKey it owns.
        return new OverlayEntryWidget(entry, tickerEnabled, key: new ObjectKey(entry));
    }

    private int ResolveInsertionIndex(OverlayEntry? below, OverlayEntry? above)
    {
        if (below is not null)
        {
            int index = _entries.IndexOf(below);
            if (index < 0)
            {
                throw new ArgumentException("The below entry is not present in this Overlay.", nameof(below));
            }

            return index;
        }

        if (above is not null)
        {
            int index = _entries.IndexOf(above);
            if (index < 0)
            {
                throw new ArgumentException("The above entry is not present in this Overlay.", nameof(above));
            }

            return index + 1;
        }

        return _entries.Count;
    }

    private static void ValidatePositionArguments(OverlayEntry? below, OverlayEntry? above)
    {
        if (below is not null && above is not null)
        {
            throw new ArgumentException("Only one of below and above may be specified.");
        }
    }
}

internal sealed class WrappingOverlay : StatefulWidget
{
    public WrappingOverlay(
        Widget child,
        Clip clipBehavior,
        bool alwaysSizeToContent,
        Key? key = null) : base(key)
    {
        Child = child;
        ClipBehavior = clipBehavior;
        AlwaysSizeToContent = alwaysSizeToContent;
    }

    public Widget Child { get; }

    public Clip ClipBehavior { get; }

    public bool AlwaysSizeToContent { get; }

    public override State CreateState() => new WrappingOverlayState();
}

internal sealed class WrappingOverlayState : State
{
    private OverlayEntry? _entry;

    private WrappingOverlay CurrentWidget => (WrappingOverlay)StateWidget;

    public override void InitState()
    {
        base.InitState();
        _entry = new OverlayEntry(
            _ => CurrentWidget.Child,
            opaque: true,
            canSizeOverlay: true);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        _entry!.MarkNeedsBuild();
    }

    public override Widget Build(BuildContext context)
    {
        return new Overlay(
            initialEntries: [_entry!],
            clipBehavior: CurrentWidget.ClipBehavior,
            alwaysSizeToContent: CurrentWidget.AlwaysSizeToContent);
    }

    public override void Dispose()
    {
        if (_entry!.Owner is not null)
        {
            _entry.Remove();
        }

        _entry.Dispose();
        _entry = null;
        base.Dispose();
    }
}

internal sealed class OverlayTheater : MultiChildRenderObjectWidget
{
    public OverlayTheater(
        IReadOnlyList<Widget> children,
        int skipCount,
        Clip clipBehavior,
        bool alwaysSizeToContent) : base(children)
    {
        if (skipCount < 0 || skipCount > children.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(skipCount));
        }

        SkipCount = skipCount;
        ClipBehavior = clipBehavior;
        AlwaysSizeToContent = alwaysSizeToContent;
    }

    public int SkipCount { get; }

    public Clip ClipBehavior { get; }

    public bool AlwaysSizeToContent { get; }

    internal override Element CreateElement() => new OverlayTheaterElement(this);

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        Alignment alignment = Directionality.Of(context) == TextDirection.Rtl
            ? Alignment.TopRight
            : Alignment.TopLeft;
        return new RenderOverlayTheater(
            alignment,
            SkipCount,
            ClipBehavior,
            AlwaysSizeToContent);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var theater = (RenderOverlayTheater)renderObject;
        theater.Alignment = Directionality.Of(context) == TextDirection.Rtl
            ? Alignment.TopRight
            : Alignment.TopLeft;
        theater.SkipCount = SkipCount;
        theater.ClipBehavior = ClipBehavior;
        theater.AlwaysSizeToContent = AlwaysSizeToContent;
    }
}

internal sealed class OverlayPortalLayoutBuilderWidget : RenderObjectWidget
{
    public OverlayPortalLayoutBuilderWidget(OverlayChildLayoutBuilder builder)
    {
        Builder = builder;
    }

    public OverlayChildLayoutBuilder Builder { get; }

    internal override Element CreateElement() => new OverlayPortalLayoutBuilderElement(this);

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderOverlayPortalLayoutBuilder();
    }
}

/// <summary>
/// Wraps an <see cref="OverlayPortal"/>'s overlay child in the render object whose layout the target
/// overlay defers until both the overlay and the portal itself have been laid out.
/// </summary>
/// <remarks>Flutter's <c>_DeferredLayout</c>. This widget must never be given a key: reparenting
/// between the overlay child and the regular child is not supported.</remarks>
internal sealed class DeferredLayout : SingleChildRenderObjectWidget
{
    public DeferredLayout(Widget child, object? childIdentifier) : base(child)
    {
        ChildIdentifier = childIdentifier;
    }

    /// <summary>The object the overlay child's semantics are traversed under.</summary>
    /// <remarks>Flutter's <c>_DeferredLayout.childIdentifier</c>; the owning
    /// <c>OverlayPortalState</c>.</remarks>
    public object? ChildIdentifier { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        RenderOverlayPortalSurrogate parent = GetLayoutParent(context);
        var renderObject = new RenderDeferredLayoutBox(parent, ChildIdentifier);
        parent.DeferredLayoutChild = renderObject;
        return renderObject;
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var deferredLayoutBox = (RenderDeferredLayoutBox)renderObject;
        Debug.Assert(ReferenceEquals(deferredLayoutBox.LayoutSurrogate, GetLayoutParent(context)));
        Debug.Assert(ReferenceEquals(GetLayoutParent(context).DeferredLayoutChild, deferredLayoutBox));
        deferredLayoutBox.ChildIdentifier = ChildIdentifier;
    }

    private static RenderOverlayPortalSurrogate GetLayoutParent(BuildContext context)
    {
        return context.FindAncestorRenderObjectOfType<RenderOverlayPortalSurrogate>()
               ?? throw new InvalidOperationException(
                   "An OverlayPortal overlay child must be built below its OverlayPortal.");
    }
}

internal sealed class OverlayPortalLayoutBuilderElement : RenderObjectElement
{
    private Element? _child;
    private OverlayChildLayoutInfo? _previousLayoutInfo;
    private bool _needsBuild = true;

    public OverlayPortalLayoutBuilderElement(
        OverlayPortalLayoutBuilderWidget widget) : base(widget)
    {
    }

    private OverlayPortalLayoutBuilderWidget LayoutWidget =>
        (OverlayPortalLayoutBuilderWidget)Widget;

    private RenderOverlayPortalLayoutBuilder LayoutRenderObject =>
        (RenderOverlayPortalLayoutBuilder)RequireRenderObject();

    protected override void OnMount()
    {
        base.OnMount();
        LayoutRenderObject.UpdateCallback(RebuildDuringLayout);
    }

    internal override void Update(Widget newWidget)
    {
        base.Update(newWidget);
        LayoutRenderObject.UpdateCallback(RebuildDuringLayout);
        _needsBuild = true;
        LayoutRenderObject.ScheduleLayoutCallback();
    }

    internal override void MarkNeedsBuild()
    {
        if (!IsActive)
        {
            return;
        }

        Dirty = false;
        _needsBuild = true;
        LayoutRenderObject.ScheduleLayoutCallback();
    }

    internal override void Rebuild()
    {
        Dirty = false;
        _needsBuild = true;
        LayoutRenderObject.ScheduleLayoutCallback();
    }

    internal override void VisitChildren(Action<Element> visitor)
    {
        if (_child is not null)
        {
            visitor(_child);
        }
    }

    internal override void ForgetChild(Element child)
    {
        if (ReferenceEquals(child, _child))
        {
            _child = null;
        }
    }

    public override void InsertRenderObjectChild(RenderObject child, object? slot)
    {
        if (slot is not null)
        {
            throw new InvalidOperationException(
                "OverlayPortal layout builder expects a null child slot.");
        }

        LayoutRenderObject.Child = (RenderBox)child;
    }

    public override void MoveRenderObjectChild(
        RenderObject child,
        object? oldSlot,
        object? newSlot)
    {
        if (!Equals(oldSlot, newSlot))
        {
            throw new InvalidOperationException(
                "OverlayPortal layout builder cannot move its single child.");
        }
    }

    public override void RemoveRenderObjectChild(RenderObject child, object? slot)
    {
        if (ReferenceEquals(LayoutRenderObject.Child, child))
        {
            LayoutRenderObject.Child = null;
        }
    }

    internal override void Unmount()
    {
        LayoutRenderObject.ClearCallback();
        if (_child is not null)
        {
            UnmountChild(_child);
            _child = null;
        }

        base.Unmount();
    }

    private void RebuildDuringLayout(OverlayChildLayoutInfo info)
    {
        if (!_needsBuild && Equals(_previousLayoutInfo, info))
        {
            return;
        }

        Widget built = LayoutWidget.Builder(new BuildContext(this), info)
            ?? throw new InvalidOperationException("OverlayPortal.WithLayoutBuilder must return a widget.");
        _child = UpdateChild(_child, built, null);
        _needsBuild = false;
        _previousLayoutInfo = info;
    }
}

internal sealed class OverlayPortalRenderWidget : RenderObjectWidget
{
    public OverlayPortalRenderWidget(
        Widget? child,
        Widget? overlayChild,
        OverlayEntryLocation? location)
    {
        if (overlayChild is not null && location is null)
        {
            throw new ArgumentException("A visible overlay child requires an OverlayPortal location.");
        }

        Debug.Assert(location is null || location.DebugIsLocationValid());

        Child = child;
        OverlayChild = overlayChild;
        Location = location;
    }

    public Widget? Child { get; }

    public Widget? OverlayChild { get; }

    public OverlayEntryLocation? Location { get; }

    internal override Element CreateElement() => new OverlayPortalElement(this);

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderOverlayPortalSurrogate(Location);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderOverlayPortalSurrogate)renderObject).Location = Location;
    }
}

internal sealed class OverlayPortalElement : RenderObjectElement
{
    private static readonly object ChildSlot = new();
    private Element? _child;
    private Element? _overlayChild;

    public OverlayPortalElement(OverlayPortalRenderWidget widget) : base(widget)
    {
    }

    private OverlayPortalRenderWidget PortalWidget => (OverlayPortalRenderWidget)Widget;

    private RenderOverlayPortalSurrogate PortalRenderObject =>
        (RenderOverlayPortalSurrogate)RequireRenderObject();

    protected override void OnMount()
    {
        base.OnMount();
        _child = UpdateChild(_child, PortalWidget.Child, ChildSlot);
        _overlayChild = UpdateChild(_overlayChild, PortalWidget.OverlayChild, PortalWidget.Location);
    }

    internal override void Update(Widget newWidget)
    {
        base.Update(newWidget);
        _child = UpdateChild(_child, PortalWidget.Child, ChildSlot);
        _overlayChild = UpdateChild(_overlayChild, PortalWidget.OverlayChild, PortalWidget.Location);
    }

    internal override void Rebuild()
    {
        base.Rebuild();
        _child = UpdateChild(_child, PortalWidget.Child, ChildSlot);
        _overlayChild = UpdateChild(_overlayChild, PortalWidget.OverlayChild, PortalWidget.Location);
    }

    internal override void ForgetChild(Element child)
    {
        if (ReferenceEquals(child, _child))
        {
            _child = null;
        }
        else if (ReferenceEquals(child, _overlayChild))
        {
            _overlayChild = null;
        }
    }

    internal override void VisitChildren(Action<Element> visitor)
    {
        if (_child is not null)
        {
            visitor(_child);
        }

        if (_overlayChild is not null)
        {
            visitor(_overlayChild);
        }
    }

    public override void InsertRenderObjectChild(RenderObject child, object? slot)
    {
        if (ReferenceEquals(slot, ChildSlot))
        {
            PortalRenderObject.Child = (RenderBox)child;
            return;
        }

        if (slot is not OverlayEntryLocation location)
        {
            throw new InvalidOperationException("OverlayPortal received an invalid overlay child slot.");
        }

        // `DeferredLayoutChild` was assigned by `DeferredLayout.CreateRenderObject`, before the element
        // got as far as inserting it here.
        var deferredChild = (RenderDeferredLayoutBox)child;
        Debug.Assert(ReferenceEquals(PortalRenderObject.DeferredLayoutChild, deferredChild));
        location.AddChild(deferredChild);
        PortalRenderObject.MarkNeedsSemanticsUpdate();
    }

    public override void MoveRenderObjectChild(
        RenderObject child,
        object? oldSlot,
        object? newSlot)
    {
        if (ReferenceEquals(oldSlot, ChildSlot) && ReferenceEquals(newSlot, ChildSlot))
        {
            return;
        }

        if (oldSlot is not OverlayEntryLocation oldLocation
            || newSlot is not OverlayEntryLocation newLocation)
        {
            throw new InvalidOperationException("OverlayPortal cannot move a child between regular and overlay slots.");
        }

        Debug.Assert(newLocation.DebugIsLocationValid());
        newLocation.MoveChild((RenderDeferredLayoutBox)child, oldLocation);
        PortalRenderObject.MarkNeedsSemanticsUpdate();
    }

    public override void RemoveRenderObjectChild(RenderObject child, object? slot)
    {
        if (ReferenceEquals(slot, ChildSlot))
        {
            if (ReferenceEquals(PortalRenderObject.Child, child))
            {
                PortalRenderObject.Child = null;
            }

            return;
        }

        if (slot is OverlayEntryLocation location)
        {
            var deferredChild = (RenderDeferredLayoutBox)child;
            Debug.Assert(ReferenceEquals(PortalRenderObject.DeferredLayoutChild, deferredChild));
            location.RemoveChild(deferredChild);
            PortalRenderObject.DeferredLayoutChild = null;
            PortalRenderObject.MarkNeedsSemanticsUpdate();
        }
    }

    internal override void Unmount()
    {
        if (_child is not null)
        {
            UnmountChild(_child);
            _child = null;
        }

        if (_overlayChild is not null)
        {
            UnmountChild(_overlayChild);
            _overlayChild = null;
        }

        base.Unmount();
    }

}

/// <summary>
/// The <see cref="OverlayPortal"/>'s own render object: a proxy box that keeps its deferred layout
/// child deeper than itself, lays that child out once its own layout is done, and keeps the child's
/// attached state in sync with its own.
/// </summary>
/// <remarks>Flutter's <c>_RenderLayoutSurrogateProxyBox</c>.</remarks>
internal sealed class RenderOverlayPortalSurrogate : RenderProxyBox
{
    private RenderDeferredLayoutBox? _deferredLayoutChild;
    private bool _didDetachDeferredChild;

    public RenderOverlayPortalSurrogate(OverlayEntryLocation? location)
    {
        Location = location;
    }

    internal OverlayEntryLocation? Location { get; set; }

    /// <remarks>
    /// Assigned as soon as <c>DeferredLayout</c> creates the box, and set back to null only when that
    /// widget leaves the tree - the surrogate needs it in <see cref="OnAttach"/>/<see cref="OnDetach"/>,
    /// where the element slot is not available.
    /// </remarks>
    internal RenderDeferredLayoutBox? DeferredLayoutChild
    {
        get => _deferredLayoutChild;
        set
        {
            if (ReferenceEquals(_deferredLayoutChild, value))
            {
                return;
            }

            _deferredLayoutChild = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    /// <remarks>Flutter's <c>_RenderDeferredLayoutBox.redepthChildren</c> calls this when the box
    /// enters the theater before the surrogate has an owner.</remarks>
    internal void RedepthDeferredChild(RenderDeferredLayoutBox child) => RedepthChild(child);

    /// <remarks>Flutter's <c>_RenderLayoutSurrogateProxyBox.redepthChildren</c>. While the child is not
    /// attached this is done by its real parent - the theater - once it becomes attached.</remarks>
    protected override void RedepthChildren()
    {
        base.RedepthChildren();
        if (_deferredLayoutChild is { } child && ReferenceEquals(child.Owner, Owner))
        {
            RedepthChild(child);
        }
    }

    /// <remarks>
    /// Flutter's <c>_RenderLayoutSurrogateProxyBox.attach</c>. Reattaching after
    /// <see cref="OnDetach"/> detached the deferred child is always safe, because the theater must be
    /// an ancestor of both render objects.
    /// </remarks>
    protected override void OnAttach()
    {
        base.OnAttach();
        if (!_didDetachDeferredChild)
        {
            return;
        }

        _didDetachDeferredChild = false;
        OverlayEntryLocation location = Location
            ?? throw new InvalidOperationException("A visible overlay child requires a portal location.");
        RenderDeferredLayoutBox child = _deferredLayoutChild
            ?? throw new InvalidOperationException("A detached portal child cannot be reattached after removal.");
        location.ReattachFromLayoutSurrogate(child);
    }

    /// <remarks>
    /// Flutter's <c>_RenderLayoutSurrogateProxyBox.detach</c>: the deferred child is detached only when
    /// the theater is not already detached, in which case the theater detaches it.
    /// </remarks>
    protected override void OnDetach()
    {
        if (_deferredLayoutChild is { } child
            && Location is { } location
            && location.Theater.Attached)
        {
            location.DetachFromLayoutSurrogate(child);
            _didDetachDeferredChild = true;
        }

        base.OnDetach();
    }

    /// <remarks>Flutter's <c>_RenderLayoutSurrogateProxyBox.performLayout</c>.</remarks>
    protected override void PerformLayout()
    {
        base.PerformLayout();
        if (_deferredLayoutChild is not { } deferredChild)
        {
            return;
        }

        // Every ancestor's PerformLayout must have returned by the time the deferred child lays out, so
        // the child goes on the dirty list rather than being reached through the layout tree walk. It is
        // guaranteed to be a relayout boundary but may not be in the dirty list yet when it has never
        // been laid out - `DoLayoutFrom` covers that case.
        if (deferredChild.Parent is not RenderOverlayTheater theater)
        {
            return;
        }

        // While the theater is laying out its size-determining child its size is unknown. The theater
        // always lays that child out first and a deferred child can never be size-determining, so
        // nothing has to happen here: the theater updates the deferred child's constraints itself.
        if (theater.LayingOutSizeDeterminingChild)
        {
            return;
        }

        BoxConstraints theaterConstraints = theater.Constraints;
        Size boxSize = double.IsFinite(theaterConstraints.MaxWidth)
                       && double.IsFinite(theaterConstraints.MaxHeight)
            ? theaterConstraints.Biggest
            : theater.Size;
        deferredChild.DoLayoutFrom(this, BoxConstraints.Tight(boxSize));
    }

}

/// <summary>
/// Stamps each theater child with the <see cref="OverlayEntry"/> it was built for, which is how the
/// theater reaches that entry's sorted list of overlay children.
/// </summary>
/// <remarks>Flutter's <c>_TheaterElement</c>.</remarks>
internal sealed class OverlayTheaterElement : MultiChildRenderObjectElement
{
    public OverlayTheaterElement(OverlayTheater widget) : base(widget)
    {
    }

    public override void InsertRenderObjectChild(RenderObject child, object? slot)
    {
        base.InsertRenderObjectChild(child, slot);
        var indexedSlot = (IndexedSlot<Element?>)slot!;
        var parentData = (OverlayTheaterParentData)child.parentData!;
        parentData.Entry = ((OverlayEntryWidget)((OverlayTheater)Widget).Children[indexedSlot.Index]).Entry;
    }

    public override void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
    {
        base.MoveRenderObjectChild(child, oldSlot, newSlot);
        if (!Constants.KDebugMode)
        {
            return;
        }

        var indexedSlot = (IndexedSlot<Element?>)newSlot!;
        var parentData = (OverlayTheaterParentData)child.parentData!;
        OverlayEntry entryAtNewSlot =
            ((OverlayEntryWidget)((OverlayTheater)Widget).Children[indexedSlot.Index]).Entry;
        Debug.Assert(ReferenceEquals(parentData.Entry, entryAtNewSlot));
    }
}

internal sealed class OverlayEntryWidget : StatefulWidget
{
    public OverlayEntryWidget(OverlayEntry entry, bool tickerEnabled, Key? key = null) : base(key)
    {
        Entry = entry;
        TickerEnabled = tickerEnabled;
    }

    public OverlayEntry Entry { get; }

    public bool TickerEnabled { get; }

    public override State CreateState() => new OverlayEntryWidgetState();
}

internal sealed class OverlayEntryWidgetState : State
{
    private RenderOverlayTheater? _theater;

    /// <remarks>Flutter's <c>_OverlayEntryWidgetState._sortedTheaterSiblings</c>, created lazily and
    /// dropped wholesale on dispose rather than unlinked entry by entry.</remarks>
    private OverlayEntryLocationList? _sortedTheaterSiblings;

    private OverlayEntryWidget CurrentWidget => (OverlayEntryWidget)StateWidget;

    internal RenderOverlayTheater Theater => _theater
        ?? throw new InvalidOperationException("An OverlayEntry must be built inside an Overlay.");

    /// <summary>The overlay children hosted on this entry, farthest first.</summary>
    /// <remarks>Flutter's <c>_OverlayEntryWidgetState._paintOrderIterable</c>.</remarks>
    internal IEnumerable<RenderDeferredLayoutBox> PaintOrderChildren => EnumerateChildren(reversed: false);

    /// <summary>The overlay children hosted on this entry, closest first.</summary>
    /// <remarks>Flutter's <c>_OverlayEntryWidgetState._hitTestOrderIterable</c>.</remarks>
    internal IEnumerable<RenderDeferredLayoutBox> HitTestOrderChildren => EnumerateChildren(reversed: true);

    public override void InitState()
    {
        base.InitState();
        CurrentWidget.Entry.Changed += HandleEntryChanged;
        CurrentWidget.Entry.SetWidgetState(this);
        _theater = Context.FindAncestorRenderObjectOfType<RenderOverlayTheater>()
                   ?? throw new InvalidOperationException("An OverlayEntry must be built inside an Overlay.");
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var oldEntryWidget = (OverlayEntryWidget)oldWidget;
        if (ReferenceEquals(oldEntryWidget.Entry, CurrentWidget.Entry))
        {
            return;
        }

        oldEntryWidget.Entry.Changed -= HandleEntryChanged;
        oldEntryWidget.Entry.SetWidgetState(null);
        CurrentWidget.Entry.Changed += HandleEntryChanged;
        CurrentWidget.Entry.SetWidgetState(this);
    }

    public override void Dispose()
    {
        CurrentWidget.Entry.Changed -= HandleEntryChanged;
        CurrentWidget.Entry.SetWidgetState(null);
        _sortedTheaterSiblings = null;
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        // The `Builder` is what lets the entry's own builder read the marker below.
        return new TickerMode(
            enabled: CurrentWidget.TickerEnabled,
            child: new RenderTheaterMarker(
                theater: Theater,
                entryState: this,
                child: new Builder(entryContext => CurrentWidget.Entry.Builder(entryContext))));
    }

    /// <remarks>
    /// Flutter's <c>_OverlayEntryWidgetState._add</c>: a backwards scan from the tail, inserting after
    /// the first location whose z-order index is not greater, so ties keep insertion order and the list
    /// stays sorted ascending. Worst case is linear in the number of children shown in one frame.
    /// </remarks>
    internal void Add(OverlayEntryLocation child)
    {
        Debug.Assert(Mounted);
        OverlayEntryLocationList children = _sortedTheaterSiblings ??= new OverlayEntryLocationList();
        Debug.Assert(!children.Contains(child));
        OverlayEntryLocation? insertPosition = children.IsEmpty ? null : children.Last;
        while (insertPosition is not null && insertPosition.ZOrderIndex > child.ZOrderIndex)
        {
            insertPosition = insertPosition.Previous;
        }

        if (insertPosition is null)
        {
            children.AddFirst(child);
        }
        else
        {
            children.InsertAfter(insertPosition, child);
        }

        Debug.Assert(children.Contains(child));
    }

    /// <remarks>Flutter's <c>_OverlayEntryWidgetState._remove</c>.</remarks>
    internal void Remove(OverlayEntryLocation child)
    {
        bool wasInCollection = _sortedTheaterSiblings?.Remove(child) ?? false;
        Debug.Assert(wasInCollection);
    }

    /// <remarks>
    /// Flutter's <c>_OverlayEntryWidgetState._createChildIterable</c>. The cursor advances before the
    /// element is yielded, so the consumer may unlink the location it is looking at - which is exactly
    /// what a hit test or a layout pass that removes an overlay child does. Locations whose portal is
    /// currently detached from its layout surrogate hold no box and are skipped.
    /// </remarks>
    private IEnumerable<RenderDeferredLayoutBox> EnumerateChildren(bool reversed)
    {
        OverlayEntryLocationList? children = _sortedTheaterSiblings;
        if (children is null || children.IsEmpty)
        {
            yield break;
        }

        OverlayEntryLocation? candidate = reversed ? children.Last : children.First;
        while (candidate is not null)
        {
            RenderDeferredLayoutBox? renderBox = candidate.OverlayChildRenderBox;
            candidate = reversed ? candidate.Previous : candidate.Next;
            if (renderBox is not null)
            {
                yield return renderBox;
            }
        }
    }

    private void HandleEntryChanged()
    {
        if (Mounted)
        {
            SetState(static () => { });
        }
    }
}

/// <summary>
/// Carries the target <see cref="RenderOverlayTheater"/> and the child model of the entry an
/// <see cref="OverlayPortal"/> sits in down to the portal.
/// </summary>
/// <remarks>Flutter's <c>_RenderTheaterMarker</c>.</remarks>
internal sealed class RenderTheaterMarker : InheritedWidget
{
    public RenderTheaterMarker(
        RenderOverlayTheater theater,
        OverlayEntryWidgetState entryState,
        Widget child,
        Key? key = null) : base(key)
    {
        Theater = theater;
        EntryState = entryState;
        Child = child;
    }

    public RenderOverlayTheater Theater { get; }

    public OverlayEntryWidgetState EntryState { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        var old = (RenderTheaterMarker)oldWidget;
        return !ReferenceEquals(old.Theater, Theater) || !ReferenceEquals(old.EntryState, EntryState);
    }

    /// <remarks>Flutter's <c>_RenderTheaterMarker.of</c>.</remarks>
    internal static RenderTheaterMarker Of(BuildContext context, bool targetRootOverlay = false)
    {
        return MaybeOf(context, targetRootOverlay)
               ?? throw new InvalidOperationException(
                   "No Overlay widget found. An OverlayPortal requires an Overlay widget ancestor.");
    }

    /// <remarks>Flutter's <c>_RenderTheaterMarker.maybeOf</c>.</remarks>
    internal static RenderTheaterMarker? MaybeOf(
        BuildContext context,
        bool targetRootOverlay = false,
        bool createDependency = true)
    {
        if (!targetRootOverlay)
        {
            return createDependency
                ? LookupBoundary.DependOnInheritedWidgetOfExactType<RenderTheaterMarker>(context)
                : LookupBoundary.GetInheritedWidgetOfExactType<RenderTheaterMarker>(context);
        }

        InheritedElement? ancestor = RootMarkerElementOf(
            LookupBoundary.GetElementForInheritedWidgetOfExactType<RenderTheaterMarker>(context));
        if (ancestor is null)
        {
            return null;
        }

        return createDependency
            ? (RenderTheaterMarker)context.Owner.DependOnInheritedElement(ancestor, aspect: null)
            : (RenderTheaterMarker)ancestor.Widget;
    }

    /// <remarks>Flutter's <c>_RenderTheaterMarker._rootRenderTheaterMarkerOf</c>: the outermost marker
    /// reachable without crossing a <see cref="LookupBoundary"/>, which is the root Overlay of this
    /// view rather than of the whole app.</remarks>
    private static InheritedElement? RootMarkerElementOf(InheritedElement? markerElement)
    {
        if (markerElement is null)
        {
            return null;
        }

        InheritedElement? ancestor = null;
        new BuildContext(markerElement).VisitAncestorElements(element =>
        {
            // Dart's `getElementForInheritedWidgetOfExactType` reads the element's own inherited map,
            // which includes the element itself; Plumix's starts at the parent, so check it here.
            ancestor = element is InheritedElement { Widget: RenderTheaterMarker } self
                ? self
                : LookupBoundary.GetElementForInheritedWidgetOfExactType<RenderTheaterMarker>(
                    new BuildContext(element));
            return false;
        });

        return ancestor is null ? markerElement : RootMarkerElementOf(ancestor);
    }
}
