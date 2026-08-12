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
    Matrix ChildPaintTransform,
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

    private OverlayPortal CurrentWidget => (OverlayPortal)StateWidget;

    internal long? ZOrderIndex => _zOrderIndex;

    public override void InitState()
    {
        base.InitState();
        SetupController(CurrentWidget.Controller);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var oldPortal = (OverlayPortal)oldWidget;
        if (!ReferenceEquals(oldPortal.Controller, CurrentWidget.Controller))
        {
            oldPortal.Controller.Detach(this);
            SetupController(CurrentWidget.Controller);
        }
    }

    public override void Dispose()
    {
        CurrentWidget.Controller.Detach(this);
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        if (!_zOrderIndex.HasValue)
        {
            return new OverlayPortalRenderWidget(
                child: CurrentWidget.Child,
                overlayChild: null,
                location: null);
        }

        bool rootOverlay = CurrentWidget.OverlayLocation == OverlayChildLocation.RootOverlay;
        OverlayState target = Overlay.Of(context, rootOverlay);
        var location = new OverlayPortalLocation(
            target.RequireTheater(),
            _zOrderIndex.Value);
        Widget overlayChild = CurrentWidget.LayoutBuilder is { } layoutBuilder
            ? new OverlayPortalLayoutBuilderWidget(
                builder: layoutBuilder,
                infoBuilder: constraints => ResolveLayoutInfo(
                    location.Theater,
                    constraints))
            : new Builder(CurrentWidget.OverlayChildBuilder);
        overlayChild = WrapWithOverlayMediaQuery(
            context,
            target.Context,
            overlayChild);
        return new OverlayPortalRenderWidget(
            child: CurrentWidget.Child,
            overlayChild: overlayChild,
            location: location);
    }

    internal void Show(long zOrderIndex)
    {
        SetState(() => _zOrderIndex = zOrderIndex);
    }

    internal void Hide()
    {
        SetState(() => _zOrderIndex = null);
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

    private OverlayChildLayoutInfo ResolveLayoutInfo(
        RenderOverlayTheater theater,
        BoxConstraints constraints)
    {
        if (Context.FindRenderObject() is not RenderBox portalRenderBox
            || !portalRenderBox.HasSize)
        {
            throw new InvalidOperationException(
                "OverlayPortal layout information is only available after its regular child is laid out.");
        }

        Matrix transform = ResolveTransformToAncestor(portalRenderBox, theater);
        Size overlaySize = theater.HasSize
            ? theater.Size
            : constraints.Biggest;
        return new OverlayChildLayoutInfo(
            portalRenderBox.Size,
            transform,
            overlaySize);
    }

    private static Matrix ResolveTransformToAncestor(
        RenderObject source,
        RenderObject ancestor)
    {
        Matrix transform = Matrix.Identity;
        RenderObject? child = source;
        while (child?.Parent is not null && !ReferenceEquals(child, ancestor))
        {
            RenderObject parent = child.Parent;
            Point childOffset = child.parentData is BoxParentData boxParentData
                ? boxParentData.offset
                : default;
            Matrix childToParent = Matrix.CreateTranslation(
                childOffset.X,
                childOffset.Y);
            if (parent is RenderTransform renderTransform)
            {
                childToParent *= renderTransform.EffectiveTransform;
            }

            transform = childToParent * transform;
            child = parent;
        }

        if (!ReferenceEquals(child, ancestor))
        {
            throw new InvalidOperationException(
                "OverlayPortal layout information requires an ancestor target Overlay.");
        }

        return transform;
    }
}

public sealed class OverlayEntry : IListenable, IDisposable
{
    private OverlayState? _overlay;
    private bool _disposed;
    private bool _opaque;
    private bool _maintainState;
    private bool _widgetMounted;
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

    public bool Mounted => _widgetMounted;

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

    internal void SetWidgetMounted(bool mounted)
    {
        if (_widgetMounted == mounted)
        {
            return;
        }

        _widgetMounted = mounted;
        foreach (Action listener in _listeners.ToArray())
        {
            listener();
        }

        if (!mounted && _disposed)
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
    private RenderOverlayTheater? _theater;

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
        if (!_entries.Contains(entry))
        {
            return;
        }

        SetState(() => _entries.Remove(entry));
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
                children.Add(
                    BuildEntry(entry, tickerEnabled: true, isOnstage: true));

                if (entry.Opaque)
                {
                    onstage = false;
                }
            }
            else if (entry.MaintainState)
            {
                children.Add(
                    BuildEntry(entry, tickerEnabled: false, isOnstage: false));
            }
        }

        children.Reverse();
        return new OverlayTheater(
            owner: this,
            offstageCount: children.Count - onstageCount,
            clipBehavior: CurrentWidget.ClipBehavior,
            alwaysSizeToContent: CurrentWidget.AlwaysSizeToContent,
            children: children);
    }

    internal void SetTheater(RenderOverlayTheater theater)
    {
        _theater = theater;
    }

    internal RenderOverlayTheater RequireTheater()
    {
        return _theater
               ?? throw new InvalidOperationException("The target Overlay has not created its render theater.");
    }

    private static Widget BuildEntry(
        OverlayEntry entry,
        bool tickerEnabled,
        bool isOnstage)
    {
        // Both levels carry the entry's identity: the theater's children change position (and count) as
        // entries go offstage, so an unkeyed parent would re-target its child element onto another entry.
        return new OverlayTheaterEntry(
            canSizeOverlay: entry.CanSizeOverlay,
            isOnstage: isOnstage,
            key: new ObjectKey(entry),
            child: new OverlayEntryWidget(
                entry,
                tickerEnabled,
                key: new ObjectKey(entry)));
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
        OverlayState owner,
        IReadOnlyList<Widget> children,
        int offstageCount,
        Clip clipBehavior,
        bool alwaysSizeToContent) : base(children)
    {
        if (offstageCount < 0 || offstageCount > children.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(offstageCount));
        }

        Owner = owner;
        OffstageCount = offstageCount;
        ClipBehavior = clipBehavior;
        AlwaysSizeToContent = alwaysSizeToContent;
    }

    public OverlayState Owner { get; }

    public int OffstageCount { get; }

    public Clip ClipBehavior { get; }

    public bool AlwaysSizeToContent { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        Alignment alignment = Directionality.Of(context) == TextDirection.Rtl
            ? Alignment.TopRight
            : Alignment.TopLeft;
        var theater = new RenderOverlayTheater(
            alignment,
            ClipBehavior,
            AlwaysSizeToContent);
        Owner.SetTheater(theater);
        return theater;
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var theater = (RenderOverlayTheater)renderObject;
        theater.Alignment = Directionality.Of(context) == TextDirection.Rtl
            ? Alignment.TopRight
            : Alignment.TopLeft;
        theater.ClipBehavior = ClipBehavior;
        theater.AlwaysSizeToContent = AlwaysSizeToContent;
        Owner.SetTheater(theater);
    }
}

internal sealed record OverlayPortalLocation(
    RenderOverlayTheater Theater,
    long ZOrder);

internal sealed class OverlayPortalLayoutBuilderWidget : RenderObjectWidget
{
    public OverlayPortalLayoutBuilderWidget(
        OverlayChildLayoutBuilder builder,
        Func<BoxConstraints, OverlayChildLayoutInfo> infoBuilder)
    {
        Builder = builder;
        InfoBuilder = infoBuilder;
    }

    public OverlayChildLayoutBuilder Builder { get; }

    public Func<BoxConstraints, OverlayChildLayoutInfo> InfoBuilder { get; }

    internal override Element CreateElement() => new OverlayPortalLayoutBuilderElement(this);

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderOverlayPortalLayoutBuilder();
    }
}

internal sealed class OverlayPortalLayoutBuilderElement : RenderObjectElement
{
    private Element? _child;

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
        LayoutRenderObject.ScheduleLayoutCallback();
    }

    internal override void MarkNeedsBuild()
    {
        if (!IsActive)
        {
            return;
        }

        Dirty = false;
        LayoutRenderObject.ScheduleLayoutCallback();
    }

    internal override void Rebuild()
    {
        Dirty = false;
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
        LayoutRenderObject.UpdateCallback(null);
        if (_child is not null)
        {
            UnmountChild(_child);
            _child = null;
        }

        base.Unmount();
    }

    private void RebuildDuringLayout(BoxConstraints constraints)
    {
        OverlayChildLayoutInfo info = LayoutWidget.InfoBuilder(constraints);
        Widget built = LayoutWidget.Builder(new BuildContext(this), info);
        _child = UpdateChild(
            _child,
            new Stack(
                fit: StackFit.Expand,
                children: [built]),
            null);
    }
}

internal sealed class OverlayPortalRenderWidget : RenderObjectWidget
{
    public OverlayPortalRenderWidget(
        Widget? child,
        Widget? overlayChild,
        OverlayPortalLocation? location)
    {
        if (overlayChild is not null && location is null)
        {
            throw new ArgumentException("A visible overlay child requires an OverlayPortal location.");
        }

        Child = child;
        OverlayChild = overlayChild;
        Location = location;
    }

    public Widget? Child { get; }

    public Widget? OverlayChild { get; }

    public OverlayPortalLocation? Location { get; }

    internal override Element CreateElement() => new OverlayPortalElement(this);

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderOverlayPortalSurrogate();
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

        if (slot is not OverlayPortalLocation location)
        {
            throw new InvalidOperationException("OverlayPortal received an invalid overlay child slot.");
        }

        RenderBox anchor = FindAnchor(location.Theater);
        location.Theater.InsertPortal((RenderBox)child, anchor, location.ZOrder);
        PortalRenderObject.PortalChild = (RenderBox)child;
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

        if (oldSlot is not OverlayPortalLocation oldLocation
            || newSlot is not OverlayPortalLocation newLocation)
        {
            throw new InvalidOperationException("OverlayPortal cannot move a child between regular and overlay slots.");
        }

        RenderBox anchor = FindAnchor(newLocation.Theater);
        newLocation.Theater.MovePortal(
            (RenderBox)child,
            oldLocation.Theater,
            anchor,
            newLocation.ZOrder);
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

        if (slot is OverlayPortalLocation location)
        {
            location.Theater.RemovePortal((RenderBox)child);
            if (ReferenceEquals(PortalRenderObject.PortalChild, child))
            {
                PortalRenderObject.PortalChild = null;
            }
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

    private RenderBox FindAnchor(RenderOverlayTheater theater)
    {
        RenderObject? candidate = PortalRenderObject;
        while (candidate?.Parent is not null && !ReferenceEquals(candidate.Parent, theater))
        {
            candidate = candidate.Parent;
        }

        if (candidate is not RenderBox anchor || !ReferenceEquals(anchor.Parent, theater))
        {
            throw new InvalidOperationException(
                "An OverlayPortal can only target an ancestor Overlay.");
        }

        return anchor;
    }
}

internal sealed class RenderOverlayPortalSurrogate : RenderProxyBox
{
    private RenderBox? _portalChild;

    internal RenderBox? PortalChild
    {
        get => _portalChild;
        set
        {
            if (ReferenceEquals(_portalChild, value))
            {
                return;
            }

            _portalChild = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    internal override void VisitChildrenForSemantics(
        Action<RenderObject, Point, Matrix> visitor)
    {
        base.VisitChildrenForSemantics(visitor);
        if (_portalChild is null)
        {
            return;
        }

        Matrix portalChildToRoot = _portalChild.ComputePaintTransformToRoot();
        Matrix surrogateToRoot = ComputePaintTransformToRoot();
        if (!surrogateToRoot.TryInvert(out Matrix rootToSurrogate))
        {
            return;
        }

        visitor(
            _portalChild,
            new Point(),
            portalChildToRoot * rootToSurrogate);
    }
}

internal sealed class OverlayTheaterEntry : ParentDataWidget<OverlayTheaterParentData>
{
    public OverlayTheaterEntry(
        Widget child,
        bool canSizeOverlay,
        bool isOnstage,
        Key? key = null) : base(child, key)
    {
        CanSizeOverlay = canSizeOverlay;
        IsOnstage = isOnstage;
    }

    public bool CanSizeOverlay { get; }

    public bool IsOnstage { get; }

    public override Type DebugTypicalAncestorWidgetType => typeof(OverlayTheater);

    protected override void ApplyParentData(RenderObject renderObject)
    {
        var parentData = (OverlayTheaterParentData)renderObject.parentData!;
        bool needsLayout = false;
        if (parentData.CanSizeOverlay != CanSizeOverlay)
        {
            parentData.CanSizeOverlay = CanSizeOverlay;
            needsLayout = true;
        }

        if (parentData.IsOnstage != IsOnstage)
        {
            parentData.IsOnstage = IsOnstage;
            needsLayout = true;
        }

        if (needsLayout)
        {
            renderObject.Parent?.MarkNeedsLayout();
        }
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
    private OverlayEntryWidget CurrentWidget => (OverlayEntryWidget)StateWidget;

    public override void InitState()
    {
        base.InitState();
        CurrentWidget.Entry.Changed += HandleEntryChanged;
        CurrentWidget.Entry.SetWidgetMounted(true);
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
        oldEntryWidget.Entry.SetWidgetMounted(false);
        CurrentWidget.Entry.Changed += HandleEntryChanged;
        CurrentWidget.Entry.SetWidgetMounted(true);
    }

    public override void Dispose()
    {
        CurrentWidget.Entry.Changed -= HandleEntryChanged;
        CurrentWidget.Entry.SetWidgetMounted(false);
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return new TickerMode(
            enabled: CurrentWidget.TickerEnabled,
            child: CurrentWidget.Entry.Builder(context));
    }

    private void HandleEntryChanged()
    {
        if (Mounted)
        {
            SetState(static () => { });
        }
    }
}
