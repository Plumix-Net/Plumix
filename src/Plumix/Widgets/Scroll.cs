using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/scrollable.dart; flutter/packages/flutter/lib/src/widgets/scroll_view.dart; flutter/packages/flutter/lib/src/widgets/sliver.dart (approximate)

namespace Plumix.Widgets;

public delegate Widget IndexedWidgetBuilder(BuildContext context, int index);

public readonly record struct ScrollMetricsSnapshot(
    double Pixels,
    double MinScrollExtent,
    double MaxScrollExtent,
    double ViewportDimension);

public abstract class ScrollNotification(ScrollMetricsSnapshot metrics) : Notification
{
    public ScrollMetricsSnapshot Metrics { get; } = metrics;
}

public sealed class ScrollStartNotification(ScrollMetricsSnapshot metrics) : ScrollNotification(metrics)
{
}

public sealed class ScrollUpdateNotification(ScrollMetricsSnapshot metrics) : ScrollNotification(metrics)
{
}

public sealed class ScrollEndNotification(ScrollMetricsSnapshot metrics) : ScrollNotification(metrics)
{
}

public sealed class KeepAliveNotification : Notification
{
    public KeepAliveNotification(KeepAliveHandle handle)
    {
        Handle = handle;
    }

    public KeepAliveHandle Handle { get; }
}

public sealed class KeepAliveHandle : ChangeNotifier
{
    private bool _released;

    public bool IsReleased => _released;

    public void Release()
    {
        if (_released)
        {
            return;
        }

        _released = true;
        NotifyListeners();
        base.Dispose();
    }

    public override void Dispose()
    {
        Release();
    }
}

public sealed class AutomaticKeepAlive : StatefulWidget
{
    public AutomaticKeepAlive(Widget child, Key? key = null) : base(key)
    {
        Child = child;
    }

    public Widget Child { get; }

    public override State CreateState()
    {
        return new AutomaticKeepAliveState();
    }

    private sealed class AutomaticKeepAliveState : State
    {
        private readonly Dictionary<KeepAliveHandle, Action> _releaseCallbacks = [];
        private bool _keepingAlive;

        private AutomaticKeepAlive CurrentWidget => (AutomaticKeepAlive)Element.Widget;

        public override Widget Build(BuildContext context)
        {
            return new NotificationListener<KeepAliveNotification>(
                onNotification: HandleKeepAliveNotification,
                child: new KeepAlive(
                    keepAlive: _keepingAlive,
                    child: CurrentWidget.Child));
        }

        public override void Dispose()
        {
            foreach (var (handle, callback) in _releaseCallbacks.ToArray())
            {
                handle.RemoveListener(callback);
            }

            _releaseCallbacks.Clear();
        }

        private bool HandleKeepAliveNotification(KeepAliveNotification notification)
        {
            var handle = notification.Handle;
            if (!_releaseCallbacks.ContainsKey(handle))
            {
                Action callback = () => HandleReleased(handle);
                _releaseCallbacks[handle] = callback;
                handle.AddListener(callback);
            }

            if (!_keepingAlive)
            {
                SetState(() => _keepingAlive = true);
            }

            return true;
        }

        private void HandleReleased(KeepAliveHandle handle)
        {
            if (!_releaseCallbacks.Remove(handle, out var callback))
            {
                return;
            }

            handle.RemoveListener(callback);
            if (_releaseCallbacks.Count == 0 && _keepingAlive)
            {
                SetState(() => _keepingAlive = false);
            }
        }
    }
}

public abstract class AutomaticKeepAliveClientMixin : State
{
    private KeepAliveHandle? _keepAliveHandle;

    protected abstract bool WantKeepAlive { get; }

    public void UpdateKeepAlive()
    {
        if (WantKeepAlive)
        {
            EnsureKeepAlive();
        }
        else
        {
            ReleaseKeepAlive();
        }
    }

    protected void EnsureKeepAlive()
    {
        if (_keepAliveHandle != null)
        {
            return;
        }

        var handle = new KeepAliveHandle();
        _keepAliveHandle = handle;
        new KeepAliveNotification(handle).Dispatch(Context);
    }

    public override void InitState()
    {
        base.InitState();
        if (WantKeepAlive)
        {
            EnsureKeepAlive();
        }
    }

    public override void Deactivate()
    {
        ReleaseKeepAlive();
        base.Deactivate();
    }

    public override void Dispose()
    {
        ReleaseKeepAlive();
        base.Dispose();
    }

    private void ReleaseKeepAlive()
    {
        var handle = _keepAliveHandle;
        if (handle == null)
        {
            return;
        }

        _keepAliveHandle = null;
        handle.Release();
    }
}

public sealed class PrimaryScrollController : InheritedNotifier<ScrollController>
{
    public PrimaryScrollController(
        ScrollController controller,
        Widget child,
        Key? key = null) : base(controller, child, key)
    {
    }

    public static ScrollController? MaybeOf(BuildContext context)
    {
        return context.DependOnInherited<PrimaryScrollController>()?.Notifier;
    }

    public static ScrollController Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException("PrimaryScrollController not found in context.");
    }
}

public sealed class ScrollController : ChangeNotifier
{
    private readonly List<ScrollPosition> _positions = [];

    public ScrollController(double initialScrollOffset = 0.0, ScrollPhysics? physics = null)
    {
        InitialScrollOffset = initialScrollOffset;
        Physics = physics ?? new ClampingScrollPhysics();
    }

    public double InitialScrollOffset { get; }

    public ScrollPhysics Physics { get; }

    public bool HasClients => _positions.Count > 0;

    public double Offset => _positions.Count == 0 ? InitialScrollOffset : _positions[0].Pixels;

    public ScrollPosition? PrimaryPosition => _positions.Count == 0 ? null : _positions[0];

    internal ScrollPosition CreateScrollPosition(ScrollPhysics? physics = null)
    {
        return new ScrollPosition(initialPixels: InitialScrollOffset, physics: physics ?? Physics);
    }

    internal void Attach(ScrollPosition position)
    {
        if (_positions.Contains(position))
        {
            return;
        }

        _positions.Add(position);
        position.AddListener(NotifyListeners);
    }

    internal void Detach(ScrollPosition position)
    {
        if (!_positions.Remove(position))
        {
            return;
        }

        position.RemoveListener(NotifyListeners);
    }

    public void JumpTo(double value)
    {
        foreach (var position in _positions.ToArray())
        {
            position.JumpTo(value);
        }
    }

    public override void Dispose()
    {
        foreach (var position in _positions.ToArray())
        {
            position.RemoveListener(NotifyListeners);
        }

        _positions.Clear();
        base.Dispose();
    }
}

public sealed class Scrollable : StatefulWidget
{
    public Scrollable(
        Widget? child = null,
        IReadOnlyList<Widget>? slivers = null,
        Axis axis = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        ScrollPhysics? physics = null,
        double cacheExtent = 250.0,
        CacheExtentStyle cacheExtentStyle = CacheExtentStyle.Pixel,
        HitTestBehavior hitTestBehavior = HitTestBehavior.Opaque,
        Key? key = null) : base(key)
    {
        Child = child;
        Slivers = slivers;
        Axis = axis;
        Reverse = reverse;
        Controller = controller;
        Physics = physics;
        CacheExtent = cacheExtent;
        CacheExtentStyle = cacheExtentStyle;
        HitTestBehavior = hitTestBehavior;
    }

    public Widget? Child { get; }

    public IReadOnlyList<Widget>? Slivers { get; }

    public Axis Axis { get; }

    public bool Reverse { get; }

    public ScrollController? Controller { get; }

    public ScrollPhysics? Physics { get; }

    public double CacheExtent { get; }

    public CacheExtentStyle CacheExtentStyle { get; }

    public HitTestBehavior HitTestBehavior { get; }

    public override State CreateState()
    {
        return new ScrollableState();
    }

    private sealed class ScrollableState : State
    {
        private ScrollController? _fallbackController;
        private ScrollController? _attachedController;
        private ScrollPosition _position = null!;

        private Scrollable CurrentWidget => (Scrollable)Element.Widget;

        public override void InitState()
        {
            _position = AttachToController(CurrentWidget.Controller, CurrentWidget.Physics);
            _position.AddListener(HandlePositionChanged);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldScrollable = (Scrollable)oldWidget;
            var current = CurrentWidget;
            var controllerChanged = !ReferenceEquals(oldScrollable.Controller, current.Controller);
            var physicsChanged = !ReferenceEquals(oldScrollable.Physics, current.Physics);

            if (!controllerChanged && !physicsChanged)
            {
                return;
            }

            _position.RemoveListener(HandlePositionChanged);
            _attachedController?.Detach(_position);

            _position = AttachToController(current.Controller, current.Physics);
            _position.AddListener(HandlePositionChanged);
            SetState(static () => { });
        }

        public override void Dispose()
        {
            _position.RemoveListener(HandlePositionChanged);
            _attachedController?.Detach(_position);
            _fallbackController?.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            var widget = CurrentWidget;
            var slivers = ResolveSlivers(widget);
            var axisDirection = ResolveAxisDirection(widget.Axis, widget.Reverse);

            return new Listener(
                behavior: widget.HitTestBehavior,
                onPointerSignal: HandlePointerSignal,
                child: new RawGestureDetector(
                    behavior: widget.HitTestBehavior,
                    onHorizontalDragStart: widget.Axis == Axis.Horizontal ? HandleDragStart : null,
                    onHorizontalDragUpdate: widget.Axis == Axis.Horizontal ? HandleHorizontalDragUpdate : null,
                    onHorizontalDragEnd: widget.Axis == Axis.Horizontal ? HandleDragEnd : null,
                    onVerticalDragStart: widget.Axis == Axis.Vertical ? HandleDragStart : null,
                    onVerticalDragUpdate: widget.Axis == Axis.Vertical ? HandleVerticalDragUpdate : null,
                    onVerticalDragEnd: widget.Axis == Axis.Vertical ? HandleDragEnd : null,
                    child: new Viewport(
                        axis: widget.Axis,
                        axisDirection: axisDirection,
                        growthDirection: GrowthDirection.Forward,
                        offsetPixels: _position.Pixels,
                        cacheExtent: widget.CacheExtent,
                        cacheExtentStyle: widget.CacheExtentStyle,
                        slivers: slivers,
                        onViewportMetricsChanged: HandleViewportMetricsChanged)));
        }

        private IReadOnlyList<Widget> ResolveSlivers(Scrollable widget)
        {
            if (widget.Slivers is { Count: > 0 } slivers)
            {
                return slivers;
            }

            return [new SliverToBoxAdapter(widget.Child ?? new SizedBox())];
        }

        private ScrollPosition AttachToController(ScrollController? providedController, ScrollPhysics? physics)
        {
            _fallbackController ??= new ScrollController();
            _attachedController = providedController ?? _fallbackController;
            var position = _attachedController.CreateScrollPosition(physics);
            _attachedController.Attach(position);
            return position;
        }

        private void HandlePositionChanged()
        {
            new ScrollUpdateNotification(CurrentMetrics()).Dispatch(Context);
            SetState(static () => { });
        }

        private void HandleDragStart(DragStartDetails _)
        {
            _position.BeginDrag();
            new ScrollStartNotification(CurrentMetrics()).Dispatch(Context);
        }

        private void HandleHorizontalDragUpdate(DragUpdateDetails details)
        {
            var delta = IsReversedAxisDirection()
                ? -details.PrimaryDelta
                : details.PrimaryDelta;
            _position.ApplyUserOffset(delta);
        }

        private void HandleVerticalDragUpdate(DragUpdateDetails details)
        {
            var delta = IsReversedAxisDirection()
                ? -details.PrimaryDelta
                : details.PrimaryDelta;
            _position.ApplyUserOffset(delta);
        }

        private void HandleDragEnd(DragEndDetails details)
        {
            var velocity = IsReversedAxisDirection()
                ? -details.PrimaryVelocity
                : details.PrimaryVelocity;
            _position.EndDrag(velocity);
            new ScrollEndNotification(CurrentMetrics()).Dispatch(Context);
        }

        private void HandlePointerSignal(PointerSignalEvent @event)
        {
            if (@event is not PointerScrollEvent scroll)
            {
                return;
            }

            var rawDelta = CurrentWidget.Axis == Axis.Vertical ? scroll.ScrollDelta.Y : scroll.ScrollDelta.X;
            var delta = IsReversedAxisDirection() ? rawDelta : -rawDelta;
            new ScrollStartNotification(CurrentMetrics()).Dispatch(Context);
            _position.ApplyPointerScrollDelta(delta * 40.0);
            new ScrollEndNotification(CurrentMetrics()).Dispatch(Context);
        }

        private void HandleViewportMetricsChanged(double viewportExtent, double minScrollExtent, double maxScrollExtent)
        {
            _position.ApplyViewportDimension(viewportExtent);
            _position.ApplyContentDimensions(minScrollExtent, maxScrollExtent);
        }

        private ScrollMetricsSnapshot CurrentMetrics()
        {
            return new ScrollMetricsSnapshot(
                Pixels: _position.Pixels,
                MinScrollExtent: _position.MinScrollExtent,
                MaxScrollExtent: _position.MaxScrollExtent,
                ViewportDimension: _position.ViewportDimension);
        }

        private bool IsReversedAxisDirection()
        {
            var axisDirection = ResolveAxisDirection(CurrentWidget.Axis, CurrentWidget.Reverse);
            return ScrollDirectionUtils.AxisDirectionIsReversed(axisDirection);
        }

        private static AxisDirection ResolveAxisDirection(Axis axis, bool reverse)
        {
            if (axis == Axis.Vertical)
            {
                return reverse ? AxisDirection.Up : AxisDirection.Down;
            }

            return reverse ? AxisDirection.Left : AxisDirection.Right;
        }
    }
}

public sealed class Viewport : MultiChildRenderObjectWidget
{
    public Viewport(
        Axis axis,
        AxisDirection axisDirection,
        GrowthDirection growthDirection,
        double offsetPixels,
        double cacheExtent,
        CacheExtentStyle cacheExtentStyle,
        IReadOnlyList<Widget> slivers,
        Action<double, double, double>? onViewportMetricsChanged = null,
        Key? key = null) : base(slivers, key)
    {
        Axis = axis;
        AxisDirection = axisDirection;
        GrowthDirection = growthDirection;
        OffsetPixels = offsetPixels;
        CacheExtent = cacheExtent;
        CacheExtentStyle = cacheExtentStyle;
        OnViewportMetricsChanged = onViewportMetricsChanged;
    }

    public Axis Axis { get; }

    public AxisDirection AxisDirection { get; }

    public GrowthDirection GrowthDirection { get; }

    public double OffsetPixels { get; }

    public double CacheExtent { get; }

    public CacheExtentStyle CacheExtentStyle { get; }

    public Action<double, double, double>? OnViewportMetricsChanged { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderViewport(
            axis: Axis,
            axisDirection: AxisDirection,
            growthDirection: GrowthDirection,
            offsetPixels: OffsetPixels,
            cacheExtent: CacheExtent,
            cacheExtentStyle: CacheExtentStyle,
            onViewportMetricsChanged: OnViewportMetricsChanged);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var viewport = (RenderViewport)renderObject;
        viewport.Axis = Axis;
        viewport.AxisDirection = AxisDirection;
        viewport.GrowthDirection = GrowthDirection;
        viewport.OffsetPixels = OffsetPixels;
        viewport.CacheExtent = CacheExtent;
        viewport.CacheExtentStyle = CacheExtentStyle;
        viewport.OnViewportMetricsChanged = OnViewportMetricsChanged;
    }
}

public abstract class SliverChildDelegate
{
    public abstract Widget? Build(BuildContext context, int index);

    public virtual int? EstimatedChildCount => null;
}

public sealed class SliverChildBuilderDelegate : SliverChildDelegate
{
    private readonly IndexedWidgetBuilder _builder;
    private readonly int? _childCount;
    private readonly bool _addAutomaticKeepAlives;

    public SliverChildBuilderDelegate(
        IndexedWidgetBuilder builder,
        int? childCount = null,
        bool addAutomaticKeepAlives = true)
    {
        _builder = builder;
        _childCount = childCount;
        _addAutomaticKeepAlives = addAutomaticKeepAlives;
    }

    public override int? EstimatedChildCount => _childCount;

    public override Widget? Build(BuildContext context, int index)
    {
        if (_childCount.HasValue && index >= _childCount.Value)
        {
            return null;
        }

        var child = _builder(context, index);
        return _addAutomaticKeepAlives
            ? new AutomaticKeepAlive(child)
            : child;
    }
}

public sealed class SliverChildListDelegate : SliverChildDelegate
{
    private readonly IReadOnlyList<Widget> _children;
    private readonly bool _addAutomaticKeepAlives;

    public SliverChildListDelegate(
        IReadOnlyList<Widget> children,
        bool addAutomaticKeepAlives = true)
    {
        _children = children;
        _addAutomaticKeepAlives = addAutomaticKeepAlives;
    }

    public override int? EstimatedChildCount => _children.Count;

    public override Widget? Build(BuildContext context, int index)
    {
        if (index < 0 || index >= _children.Count)
        {
            return null;
        }

        var child = _children[index];
        return _addAutomaticKeepAlives
            ? new AutomaticKeepAlive(child)
            : child;
    }
}

public sealed class SliverToBoxAdapter : SingleChildRenderObjectWidget
{
    public SliverToBoxAdapter(Widget child, Key? key = null) : base(child, key)
    {
    }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverToBoxAdapter();
    }
}

public sealed class SliverPadding : SingleChildRenderObjectWidget
{
    public SliverPadding(Thickness padding, Widget? sliver = null, Key? key = null) : base(sliver, key)
    {
        Padding = padding;
    }

    public Thickness Padding { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverPadding(Padding);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderSliverPadding)renderObject).Padding = Padding;
    }
}

public sealed class KeepAlive : ParentDataWidget<SliverMultiBoxAdaptorParentData>
{
    public KeepAlive(
        bool keepAlive,
        Widget child,
        Key? key = null) : base(child, key)
    {
        Value = keepAlive;
    }

    public bool Value { get; }

    public override Type DebugTypicalAncestorWidgetType => typeof(SliverMultiBoxAdaptorWidget);

    protected override void ApplyParentData(RenderObject renderObject)
    {
        var parentData = (SliverMultiBoxAdaptorParentData)renderObject.parentData!;
        if (parentData.KeepAlive == Value)
        {
            return;
        }

        parentData.KeepAlive = Value;
        if (!Value)
        {
            renderObject.Parent?.MarkNeedsLayout();
        }
    }
}

public abstract class SliverMultiBoxAdaptorWidget : RenderObjectWidget
{
    protected SliverMultiBoxAdaptorWidget(SliverChildDelegate @delegate, Key? key = null) : base(key)
    {
        Delegate = @delegate;
    }

    public SliverChildDelegate Delegate { get; }

    internal override Element CreateElement()
    {
        return new SliverMultiBoxAdaptorElement(this);
    }
}

internal sealed class SliverMultiBoxAdaptorElement : RenderObjectElement, IRenderSliverBoxChildManager
{
    private readonly SortedDictionary<int, Element> _childElements = [];
    private readonly Dictionary<Element, int> _indexByElement = [];
    private readonly Dictionary<RenderBox, Element> _elementByRenderObject = [];
    private bool _didUnderflow;

    public SliverMultiBoxAdaptorElement(SliverMultiBoxAdaptorWidget widget) : base(widget)
    {
    }

    private SliverMultiBoxAdaptorWidget TypedWidget => (SliverMultiBoxAdaptorWidget)Widget;

    private RenderSliverMultiBoxAdaptor TypedRenderObject => (RenderSliverMultiBoxAdaptor)RequireRenderObject();

    int? IRenderSliverBoxChildManager.ChildCount => TypedWidget.Delegate.EstimatedChildCount;

    protected override void OnMount()
    {
        base.OnMount();
        TypedRenderObject.ChildManager = this;
    }

    protected override void OnActivate()
    {
        base.OnActivate();
        TypedRenderObject.ChildManager = this;
    }

    protected override void OnDeactivate()
    {
        if (RenderObject is RenderSliverMultiBoxAdaptor renderObject && ReferenceEquals(renderObject.ChildManager, this))
        {
            renderObject.ChildManager = null;
        }

        base.OnDeactivate();
    }

    internal override void Rebuild()
    {
        base.Rebuild();
        SyncChildWidgets();
        TypedRenderObject.MarkNeedsLayout();
    }

    internal override void Update(Widget newWidget)
    {
        base.Update(newWidget);
        TypedRenderObject.ChildManager = this;
        SyncChildWidgets();
        TypedRenderObject.MarkNeedsLayout();
    }

    internal override void VisitChildren(Action<Element> visitor)
    {
        foreach (var child in _childElements.Values)
        {
            visitor(child);
        }
    }

    internal override void ForgetChild(Element child)
    {
        RemoveMappings(child);
    }

    internal override void Unmount()
    {
        foreach (var child in _childElements.Values.ToArray())
        {
            UnmountChild(child);
        }

        _childElements.Clear();
        _indexByElement.Clear();
        _elementByRenderObject.Clear();
        base.Unmount();
    }

    public bool CreateChild(int index, RenderBox? after)
    {
        if (_childElements.ContainsKey(index))
        {
            return true;
        }

        var widget = TypedWidget.Delegate.Build(new BuildContext(this), index);
        if (widget == null)
        {
            return false;
        }

        var previousElement = after != null && _elementByRenderObject.TryGetValue(after, out var mapped)
            ? mapped
            : PreviousElementForIndex(index);

        var slot = new IndexedSlot<Element?>(index, previousElement);
        var child = UpdateChild(null, widget, slot);
        if (child == null)
        {
            return false;
        }

        AttachMappings(index, child);
        return true;
    }

    public void RemoveChild(RenderBox child)
    {
        if (!_elementByRenderObject.TryGetValue(child, out var element))
        {
            return;
        }

        RemoveMappings(element);
        DeactivateChild(element);
    }

    public void DidAdoptChild(RenderBox child)
    {
        if (!_elementByRenderObject.TryGetValue(child, out var element))
        {
            return;
        }

        if (!_indexByElement.TryGetValue(element, out var index))
        {
            return;
        }

        if (child.parentData is SliverMultiBoxAdaptorParentData parentData)
        {
            parentData.Index = index;
        }
    }

    public void SetDidUnderflow(bool value)
    {
        _didUnderflow = value;
    }

    public override void InsertRenderObjectChild(RenderObject child, object? slot)
    {
        if (slot is not IndexedSlot<Element?> indexedSlot)
        {
            throw new InvalidOperationException("SliverMultiBoxAdaptorElement expects IndexedSlot.");
        }

        var renderBox = (RenderBox)child;
        TypedRenderObject.Insert(renderBox, (RenderBox?)indexedSlot.Value?.RenderObject);
        if (renderBox.parentData is SliverMultiBoxAdaptorParentData parentData)
        {
            parentData.Index = indexedSlot.Index;
        }
    }

    public override void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
    {
        if (newSlot is not IndexedSlot<Element?> indexedSlot)
        {
            throw new InvalidOperationException("SliverMultiBoxAdaptorElement expects IndexedSlot.");
        }

        var renderBox = (RenderBox)child;
        TypedRenderObject.Move(renderBox, (RenderBox?)indexedSlot.Value?.RenderObject);
        if (renderBox.parentData is SliverMultiBoxAdaptorParentData parentData)
        {
            parentData.Index = indexedSlot.Index;
        }
    }

    public override void RemoveRenderObjectChild(RenderObject child, object? slot)
    {
        TypedRenderObject.Remove((RenderBox)child);
    }

    private void SyncChildWidgets()
    {
        foreach (var index in _childElements.Keys.ToArray())
        {
            if (!_childElements.TryGetValue(index, out var oldChild))
            {
                continue;
            }

            var updatedWidget = TypedWidget.Delegate.Build(new BuildContext(this), index);
            if (updatedWidget == null)
            {
                RemoveMappings(oldChild);
                DeactivateChild(oldChild);
                continue;
            }

            var updatedChild = UpdateChild(oldChild, updatedWidget, new IndexedSlot<Element?>(index, PreviousElementForIndex(index)));
            if (updatedChild == null)
            {
                RemoveMappings(oldChild);
                continue;
            }

            if (!ReferenceEquals(updatedChild, oldChild))
            {
                RemoveMappings(oldChild);
                AttachMappings(index, updatedChild);
            }
            else
            {
                RefreshRenderObjectMapping(updatedChild);
            }
        }

        if (_didUnderflow)
        {
            TypedRenderObject.MarkNeedsLayout();
        }
    }

    private Element? PreviousElementForIndex(int index)
    {
        Element? previous = null;
        foreach (var pair in _childElements)
        {
            if (pair.Key >= index)
            {
                break;
            }

            if (IsActiveRenderListChild(pair.Value))
            {
                previous = pair.Value;
            }
        }

        return previous;
    }

    private static bool IsActiveRenderListChild(Element element)
    {
        if (element.RenderObject is not RenderBox renderBox)
        {
            return false;
        }

        if (renderBox.parentData is not SliverMultiBoxAdaptorParentData parentData)
        {
            return false;
        }

        return !parentData.KeptAlive;
    }

    private void AttachMappings(int index, Element child)
    {
        _childElements[index] = child;
        _indexByElement[child] = index;
        if (child.RenderObject is RenderBox renderBox)
        {
            _elementByRenderObject[renderBox] = child;
        }
    }

    private void RemoveMappings(Element child)
    {
        if (_indexByElement.TryGetValue(child, out var index))
        {
            _indexByElement.Remove(child);
            _childElements.Remove(index);
        }

        if (child.RenderObject is RenderBox renderBox)
        {
            _elementByRenderObject.Remove(renderBox);
        }
    }

    private void RefreshRenderObjectMapping(Element child)
    {
        foreach (var key in _elementByRenderObject.Where(pair => ReferenceEquals(pair.Value, child)).Select(pair => pair.Key).ToArray())
        {
            _elementByRenderObject.Remove(key);
        }

        if (child.RenderObject is RenderBox renderBox)
        {
            _elementByRenderObject[renderBox] = child;
        }
    }
}

public sealed class SliverList : SliverMultiBoxAdaptorWidget
{
    public SliverList(SliverChildDelegate @delegate, Key? key = null) : base(@delegate, key)
    {
    }

    public static SliverList FromChildren(
        IReadOnlyList<Widget> children,
        bool addAutomaticKeepAlives = true,
        Key? key = null)
    {
        return new SliverList(
            new SliverChildListDelegate(
                children,
                addAutomaticKeepAlives: addAutomaticKeepAlives),
            key);
    }

    public static SliverList Builder(
        int childCount,
        IndexedWidgetBuilder itemBuilder,
        bool addAutomaticKeepAlives = true,
        Key? key = null)
    {
        return new SliverList(
            new SliverChildBuilderDelegate(
                itemBuilder,
                childCount,
                addAutomaticKeepAlives: addAutomaticKeepAlives),
            key);
    }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverList();
    }
}

public sealed class SliverFixedExtentList : SliverMultiBoxAdaptorWidget
{
    public SliverFixedExtentList(
        SliverChildDelegate @delegate,
        double itemExtent,
        Key? key = null) : base(@delegate, key)
    {
        if (itemExtent <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemExtent), "itemExtent must be greater than 0.");
        }

        ItemExtent = itemExtent;
    }

    public double ItemExtent { get; }

    public static SliverFixedExtentList FromChildren(
        IReadOnlyList<Widget> children,
        double itemExtent,
        bool addAutomaticKeepAlives = true,
        Key? key = null)
    {
        return new SliverFixedExtentList(
            new SliverChildListDelegate(
                children,
                addAutomaticKeepAlives: addAutomaticKeepAlives),
            itemExtent,
            key);
    }

    public static SliverFixedExtentList Builder(
        int childCount,
        IndexedWidgetBuilder itemBuilder,
        double itemExtent,
        bool addAutomaticKeepAlives = true,
        Key? key = null)
    {
        return new SliverFixedExtentList(
            new SliverChildBuilderDelegate(
                itemBuilder,
                childCount,
                addAutomaticKeepAlives: addAutomaticKeepAlives),
            itemExtent,
            key);
    }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverFixedExtentList(ItemExtent);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderSliverFixedExtentList)renderObject).ItemExtent = ItemExtent;
    }
}

public sealed class SliverGrid : SliverMultiBoxAdaptorWidget
{
    public SliverGrid(
        SliverChildDelegate @delegate,
        SliverGridDelegate gridDelegate,
        Key? key = null) : base(@delegate, key)
    {
        GridDelegate = gridDelegate ?? throw new ArgumentNullException(nameof(gridDelegate));
    }

    public SliverGridDelegate GridDelegate { get; }

    public static SliverGrid FromChildren(
        IReadOnlyList<Widget> children,
        SliverGridDelegate gridDelegate,
        bool addAutomaticKeepAlives = true,
        Key? key = null)
    {
        return new SliverGrid(
            new SliverChildListDelegate(
                children,
                addAutomaticKeepAlives: addAutomaticKeepAlives),
            gridDelegate,
            key);
    }

    public static SliverGrid Builder(
        int childCount,
        IndexedWidgetBuilder itemBuilder,
        SliverGridDelegate gridDelegate,
        bool addAutomaticKeepAlives = true,
        Key? key = null)
    {
        return new SliverGrid(
            new SliverChildBuilderDelegate(
                itemBuilder,
                childCount,
                addAutomaticKeepAlives: addAutomaticKeepAlives),
            gridDelegate,
            key);
    }

    public static SliverGrid Count(
        int crossAxisCount,
        IReadOnlyList<Widget> children,
        double mainAxisSpacing = 0,
        double crossAxisSpacing = 0,
        double childAspectRatio = 1,
        bool addAutomaticKeepAlives = true,
        Key? key = null)
    {
        return FromChildren(
            children,
            new SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: crossAxisCount,
                mainAxisSpacing: mainAxisSpacing,
                crossAxisSpacing: crossAxisSpacing,
                childAspectRatio: childAspectRatio),
            addAutomaticKeepAlives: addAutomaticKeepAlives,
            key: key);
    }

    public static SliverGrid Extent(
        double maxCrossAxisExtent,
        IReadOnlyList<Widget> children,
        double mainAxisSpacing = 0,
        double crossAxisSpacing = 0,
        double childAspectRatio = 1,
        bool addAutomaticKeepAlives = true,
        Key? key = null)
    {
        return FromChildren(
            children,
            new SliverGridDelegateWithMaxCrossAxisExtent(
                maxCrossAxisExtent: maxCrossAxisExtent,
                mainAxisSpacing: mainAxisSpacing,
                crossAxisSpacing: crossAxisSpacing,
                childAspectRatio: childAspectRatio),
            addAutomaticKeepAlives: addAutomaticKeepAlives,
            key: key);
    }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverGrid(GridDelegate);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderSliverGrid)renderObject).GridDelegate = GridDelegate;
    }
}

public sealed class CustomScrollView : StatelessWidget
{
    public CustomScrollView(
        IReadOnlyList<Widget> slivers,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        bool? primary = null,
        ScrollPhysics? physics = null,
        double cacheExtent = 250.0,
        CacheExtentStyle cacheExtentStyle = CacheExtentStyle.Pixel,
        Key? key = null) : base(key)
    {
        Slivers = slivers;
        ScrollDirection = scrollDirection;
        Reverse = reverse;
        Controller = controller;
        Primary = primary;
        Physics = physics;
        CacheExtent = cacheExtent;
        CacheExtentStyle = cacheExtentStyle;
    }

    public IReadOnlyList<Widget> Slivers { get; }

    public Axis ScrollDirection { get; }

    public bool Reverse { get; }

    public ScrollController? Controller { get; }

    public bool? Primary { get; }

    public ScrollPhysics? Physics { get; }

    public double CacheExtent { get; }

    public CacheExtentStyle CacheExtentStyle { get; }

    public override Widget Build(BuildContext context)
    {
        var usePrimary = Primary ?? (ScrollDirection == Axis.Vertical && Controller == null);
        var effectiveController = Controller;
        if (effectiveController == null && usePrimary)
        {
            effectiveController = PrimaryScrollController.MaybeOf(context);
        }

        return new Scrollable(
            slivers: Slivers,
            axis: ScrollDirection,
            reverse: Reverse,
            controller: effectiveController,
            physics: Physics,
            cacheExtent: CacheExtent,
            cacheExtentStyle: CacheExtentStyle);
    }
}

public sealed class SingleChildScrollView : StatelessWidget
{
    public SingleChildScrollView(
        Widget child,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        ScrollPhysics? physics = null,
        double cacheExtent = 250.0,
        CacheExtentStyle cacheExtentStyle = CacheExtentStyle.Pixel,
        Key? key = null) : base(key)
    {
        Child = child;
        ScrollDirection = scrollDirection;
        Reverse = reverse;
        Controller = controller;
        Physics = physics;
        CacheExtent = cacheExtent;
        CacheExtentStyle = cacheExtentStyle;
    }

    public Widget Child { get; }

    public Axis ScrollDirection { get; }

    public bool Reverse { get; }

    public ScrollController? Controller { get; }

    public ScrollPhysics? Physics { get; }

    public double CacheExtent { get; }

    public CacheExtentStyle CacheExtentStyle { get; }

    public override Widget Build(BuildContext context)
    {
        return new CustomScrollView(
            slivers: [new SliverToBoxAdapter(Child)],
            scrollDirection: ScrollDirection,
            reverse: Reverse,
            controller: Controller,
            physics: Physics,
            cacheExtent: CacheExtent,
            cacheExtentStyle: CacheExtentStyle);
    }
}

public sealed class Scrollbar : StatefulWidget
{
    public Scrollbar(
        Widget child,
        ScrollController? controller = null,
        double thickness = 4.0,
        Color? thumbColor = null,
        Key? key = null) : base(key)
    {
        Child = child;
        Controller = controller;
        Thickness = thickness;
        ThumbColor = thumbColor ?? Color.Parse("#AA5A6B82");
    }

    public Widget Child { get; }

    public ScrollController? Controller { get; }

    public double Thickness { get; }

    public Color ThumbColor { get; }

    public override State CreateState()
    {
        return new ScrollbarState();
    }

    private sealed class ScrollbarState : State
    {
        private ScrollController? _controller;

        private Scrollbar CurrentWidget => (Scrollbar)Element.Widget;

        public override void InitState()
        {
            _controller = ResolveController();
            _controller?.AddListener(HandleControllerChanged);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldScrollbar = (Scrollbar)oldWidget;
            if (ReferenceEquals(oldScrollbar.Controller, CurrentWidget.Controller))
            {
                return;
            }

            _controller?.RemoveListener(HandleControllerChanged);
            _controller = ResolveController();
            _controller?.AddListener(HandleControllerChanged);
            SetState(static () => { });
        }

        public override void Dispose()
        {
            _controller?.RemoveListener(HandleControllerChanged);
        }

        public override Widget Build(BuildContext context)
        {
            var widget = CurrentWidget;
            var controller = _controller;
            var position = controller?.PrimaryPosition;

            if (position == null || position.MaxScrollExtent <= 0 || position.ViewportDimension <= 0)
            {
                return widget.Child;
            }

            const int totalFlex = 1000;
            var fraction = Math.Clamp(position.ViewportDimension / (position.MaxScrollExtent + position.ViewportDimension), 0.05, 1.0);
            var thumbFlex = Math.Clamp((int)(fraction * totalFlex), 1, totalFlex);
            var offsetFraction = position.MaxScrollExtent <= 0 ? 0 : Math.Clamp(position.Pixels / position.MaxScrollExtent, 0, 1);
            var beforeFlex = Math.Clamp((int)(offsetFraction * (totalFlex - thumbFlex)), 0, totalFlex - thumbFlex);
            var afterFlex = Math.Max(0, totalFlex - thumbFlex - beforeFlex);

            return new Row(
                children:
                [
                    new Expanded(child: widget.Child),
                    new SizedBox(
                        width: widget.Thickness,
                        child: new Column(
                            children:
                            [
                                new Expanded(flex: Math.Max(1, beforeFlex), child: new SizedBox()),
                                new Expanded(
                                    flex: Math.Max(1, thumbFlex),
                                    child: new ColoredBox(widget.ThumbColor)),
                                new Expanded(flex: Math.Max(1, afterFlex), child: new SizedBox()),
                            ]))
                ]);
        }

        private ScrollController? ResolveController()
        {
            return CurrentWidget.Controller ?? PrimaryScrollController.MaybeOf(Context);
        }

        private void HandleControllerChanged()
        {
            SetState(static () => { });
        }
    }
}

public sealed class ListView : StatelessWidget
{
    private readonly IReadOnlyList<Widget>? _children;
    private readonly IndexedWidgetBuilder? _itemBuilder;
    private readonly IndexedWidgetBuilder? _separatorBuilder;
    private readonly int _itemCount;
    private readonly double? _itemExtent;
    private readonly Thickness _padding;
    private readonly bool _addAutomaticKeepAlives;
    private readonly double _cacheExtent;
    private readonly CacheExtentStyle _cacheExtentStyle;

    public ListView(
        IReadOnlyList<Widget>? children = null,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        ScrollPhysics? physics = null,
        double? itemExtent = null,
        Thickness? padding = null,
        bool addAutomaticKeepAlives = true,
        double cacheExtent = 250.0,
        CacheExtentStyle cacheExtentStyle = CacheExtentStyle.Pixel,
        Key? key = null) : base(key)
    {
        if (itemExtent.HasValue && itemExtent.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemExtent), "itemExtent must be greater than 0.");
        }

        _children = children ?? [];
        ScrollDirection = scrollDirection;
        Reverse = reverse;
        Controller = controller;
        Physics = physics;
        _itemExtent = itemExtent;
        _padding = padding ?? default;
        _addAutomaticKeepAlives = addAutomaticKeepAlives;
        _cacheExtent = cacheExtent;
        _cacheExtentStyle = cacheExtentStyle;
    }

    private ListView(
        int itemCount,
        IndexedWidgetBuilder itemBuilder,
        IndexedWidgetBuilder? separatorBuilder,
        Axis scrollDirection,
        bool reverse,
        ScrollController? controller,
        ScrollPhysics? physics,
        double? itemExtent,
        Thickness? padding,
        bool addAutomaticKeepAlives,
        double cacheExtent,
        CacheExtentStyle cacheExtentStyle,
        Key? key) : base(key)
    {
        if (itemCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemCount), "itemCount cannot be negative.");
        }

        if (itemExtent.HasValue && itemExtent.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemExtent), "itemExtent must be greater than 0.");
        }

        _itemCount = itemCount;
        _itemBuilder = itemBuilder;
        _separatorBuilder = separatorBuilder;
        ScrollDirection = scrollDirection;
        Reverse = reverse;
        Controller = controller;
        Physics = physics;
        _itemExtent = itemExtent;
        _padding = padding ?? default;
        _addAutomaticKeepAlives = addAutomaticKeepAlives;
        _cacheExtent = cacheExtent;
        _cacheExtentStyle = cacheExtentStyle;
    }

    public Axis ScrollDirection { get; }

    public bool Reverse { get; }

    public ScrollController? Controller { get; }

    public ScrollPhysics? Physics { get; }

    public static ListView Builder(
        int itemCount,
        IndexedWidgetBuilder itemBuilder,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        ScrollPhysics? physics = null,
        double? itemExtent = null,
        Thickness? padding = null,
        bool addAutomaticKeepAlives = true,
        double cacheExtent = 250.0,
        CacheExtentStyle cacheExtentStyle = CacheExtentStyle.Pixel,
        Key? key = null)
    {
        return new ListView(
            itemCount: itemCount,
            itemBuilder: itemBuilder,
            separatorBuilder: null,
            scrollDirection: scrollDirection,
            reverse: reverse,
            controller: controller,
            physics: physics,
            itemExtent: itemExtent,
            padding: padding,
            addAutomaticKeepAlives: addAutomaticKeepAlives,
            cacheExtent: cacheExtent,
            cacheExtentStyle: cacheExtentStyle,
            key: key);
    }

    public static ListView Separated(
        int itemCount,
        IndexedWidgetBuilder itemBuilder,
        IndexedWidgetBuilder separatorBuilder,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        ScrollPhysics? physics = null,
        double? itemExtent = null,
        Thickness? padding = null,
        bool addAutomaticKeepAlives = true,
        double cacheExtent = 250.0,
        CacheExtentStyle cacheExtentStyle = CacheExtentStyle.Pixel,
        Key? key = null)
    {
        return new ListView(
            itemCount: itemCount,
            itemBuilder: itemBuilder,
            separatorBuilder: separatorBuilder,
            scrollDirection: scrollDirection,
            reverse: reverse,
            controller: controller,
            physics: physics,
            itemExtent: itemExtent,
            padding: padding,
            addAutomaticKeepAlives: addAutomaticKeepAlives,
            cacheExtent: cacheExtent,
            cacheExtentStyle: cacheExtentStyle,
            key: key);
    }

    public override Widget Build(BuildContext context)
    {
        Widget sliver;
        if (_itemBuilder != null)
        {
            var childCount = _itemCount;
            IndexedWidgetBuilder effectiveItemBuilder = _itemBuilder;

            if (_separatorBuilder != null)
            {
                var itemBuilder = _itemBuilder;
                var separatorBuilder = _separatorBuilder;
                childCount = SeparatedChildCount(_itemCount);
                effectiveItemBuilder = (buildContext, index) =>
                {
                    var itemIndex = index / 2;
                    return index % 2 == 0
                        ? itemBuilder(buildContext, itemIndex)
                        : separatorBuilder(buildContext, itemIndex);
                };
            }

            sliver = _itemExtent.HasValue
                ? SliverFixedExtentList.Builder(
                    childCount,
                    effectiveItemBuilder,
                    _itemExtent.Value,
                    addAutomaticKeepAlives: _addAutomaticKeepAlives)
                : SliverList.Builder(
                    childCount,
                    effectiveItemBuilder,
                    addAutomaticKeepAlives: _addAutomaticKeepAlives);
        }
        else
        {
            sliver = _itemExtent.HasValue
                ? SliverFixedExtentList.FromChildren(
                    _children ?? [],
                    _itemExtent.Value,
                    addAutomaticKeepAlives: _addAutomaticKeepAlives)
                : SliverList.FromChildren(
                    _children ?? [],
                    addAutomaticKeepAlives: _addAutomaticKeepAlives);
        }

        if (HasNonZeroPadding(_padding))
        {
            sliver = new SliverPadding(_padding, sliver);
        }

        return new CustomScrollView(
            slivers: [sliver],
            scrollDirection: ScrollDirection,
            reverse: Reverse,
            controller: Controller,
            physics: Physics,
            cacheExtent: _cacheExtent,
            cacheExtentStyle: _cacheExtentStyle);
    }

    private static int SeparatedChildCount(int itemCount)
    {
        if (itemCount <= 0)
        {
            return 0;
        }

        return itemCount * 2 - 1;
    }

    private static bool HasNonZeroPadding(Thickness padding)
    {
        return Math.Abs(padding.Left) > 0.0001
               || Math.Abs(padding.Top) > 0.0001
               || Math.Abs(padding.Right) > 0.0001
               || Math.Abs(padding.Bottom) > 0.0001;
    }
}

public sealed class GridView : StatelessWidget
{
    private readonly SliverGridDelegate _gridDelegate;
    private readonly IReadOnlyList<Widget>? _children;
    private readonly IndexedWidgetBuilder? _itemBuilder;
    private readonly int _itemCount;
    private readonly Thickness _padding;
    private readonly bool _addAutomaticKeepAlives;
    private readonly double _cacheExtent;
    private readonly CacheExtentStyle _cacheExtentStyle;

    public GridView(
        SliverGridDelegate gridDelegate,
        IReadOnlyList<Widget>? children = null,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        ScrollPhysics? physics = null,
        Thickness? padding = null,
        bool addAutomaticKeepAlives = true,
        double cacheExtent = 250.0,
        CacheExtentStyle cacheExtentStyle = CacheExtentStyle.Pixel,
        Key? key = null) : base(key)
    {
        _gridDelegate = gridDelegate ?? throw new ArgumentNullException(nameof(gridDelegate));
        _children = children ?? [];
        ScrollDirection = scrollDirection;
        Reverse = reverse;
        Controller = controller;
        Physics = physics;
        _padding = padding ?? default;
        _addAutomaticKeepAlives = addAutomaticKeepAlives;
        _cacheExtent = cacheExtent;
        _cacheExtentStyle = cacheExtentStyle;
    }

    private GridView(
        SliverGridDelegate gridDelegate,
        int itemCount,
        IndexedWidgetBuilder itemBuilder,
        Axis scrollDirection,
        bool reverse,
        ScrollController? controller,
        ScrollPhysics? physics,
        Thickness? padding,
        bool addAutomaticKeepAlives,
        double cacheExtent,
        CacheExtentStyle cacheExtentStyle,
        Key? key) : base(key)
    {
        if (itemCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemCount), "itemCount cannot be negative.");
        }

        _gridDelegate = gridDelegate ?? throw new ArgumentNullException(nameof(gridDelegate));
        _itemCount = itemCount;
        _itemBuilder = itemBuilder;
        ScrollDirection = scrollDirection;
        Reverse = reverse;
        Controller = controller;
        Physics = physics;
        _padding = padding ?? default;
        _addAutomaticKeepAlives = addAutomaticKeepAlives;
        _cacheExtent = cacheExtent;
        _cacheExtentStyle = cacheExtentStyle;
    }

    public Axis ScrollDirection { get; }

    public bool Reverse { get; }

    public ScrollController? Controller { get; }

    public ScrollPhysics? Physics { get; }

    public static GridView Builder(
        int itemCount,
        IndexedWidgetBuilder itemBuilder,
        SliverGridDelegate gridDelegate,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        ScrollPhysics? physics = null,
        Thickness? padding = null,
        bool addAutomaticKeepAlives = true,
        double cacheExtent = 250.0,
        CacheExtentStyle cacheExtentStyle = CacheExtentStyle.Pixel,
        Key? key = null)
    {
        return new GridView(
            gridDelegate: gridDelegate,
            itemCount: itemCount,
            itemBuilder: itemBuilder,
            scrollDirection: scrollDirection,
            reverse: reverse,
            controller: controller,
            physics: physics,
            padding: padding,
            addAutomaticKeepAlives: addAutomaticKeepAlives,
            cacheExtent: cacheExtent,
            cacheExtentStyle: cacheExtentStyle,
            key: key);
    }

    public static GridView Count(
        int crossAxisCount,
        IReadOnlyList<Widget>? children = null,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        ScrollPhysics? physics = null,
        Thickness? padding = null,
        double mainAxisSpacing = 0,
        double crossAxisSpacing = 0,
        double childAspectRatio = 1,
        double? mainAxisExtent = null,
        bool addAutomaticKeepAlives = true,
        double cacheExtent = 250.0,
        CacheExtentStyle cacheExtentStyle = CacheExtentStyle.Pixel,
        Key? key = null)
    {
        return new GridView(
            gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: crossAxisCount,
                mainAxisSpacing: mainAxisSpacing,
                crossAxisSpacing: crossAxisSpacing,
                childAspectRatio: childAspectRatio,
                mainAxisExtent: mainAxisExtent),
            children: children,
            scrollDirection: scrollDirection,
            reverse: reverse,
            controller: controller,
            physics: physics,
            padding: padding,
            addAutomaticKeepAlives: addAutomaticKeepAlives,
            cacheExtent: cacheExtent,
            cacheExtentStyle: cacheExtentStyle,
            key: key);
    }

    public static GridView Extent(
        double maxCrossAxisExtent,
        IReadOnlyList<Widget>? children = null,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        ScrollPhysics? physics = null,
        Thickness? padding = null,
        double mainAxisSpacing = 0,
        double crossAxisSpacing = 0,
        double childAspectRatio = 1,
        double? mainAxisExtent = null,
        bool addAutomaticKeepAlives = true,
        double cacheExtent = 250.0,
        CacheExtentStyle cacheExtentStyle = CacheExtentStyle.Pixel,
        Key? key = null)
    {
        return new GridView(
            gridDelegate: new SliverGridDelegateWithMaxCrossAxisExtent(
                maxCrossAxisExtent: maxCrossAxisExtent,
                mainAxisSpacing: mainAxisSpacing,
                crossAxisSpacing: crossAxisSpacing,
                childAspectRatio: childAspectRatio,
                mainAxisExtent: mainAxisExtent),
            children: children,
            scrollDirection: scrollDirection,
            reverse: reverse,
            controller: controller,
            physics: physics,
            padding: padding,
            addAutomaticKeepAlives: addAutomaticKeepAlives,
            cacheExtent: cacheExtent,
            cacheExtentStyle: cacheExtentStyle,
            key: key);
    }

    public override Widget Build(BuildContext context)
    {
        Widget sliver = _itemBuilder != null
            ? SliverGrid.Builder(
                childCount: _itemCount,
                itemBuilder: _itemBuilder,
                gridDelegate: _gridDelegate,
                addAutomaticKeepAlives: _addAutomaticKeepAlives)
            : SliverGrid.FromChildren(
                _children ?? [],
                _gridDelegate,
                addAutomaticKeepAlives: _addAutomaticKeepAlives);

        if (HasNonZeroPadding(_padding))
        {
            sliver = new SliverPadding(_padding, sliver);
        }

        return new CustomScrollView(
            slivers: [sliver],
            scrollDirection: ScrollDirection,
            reverse: Reverse,
            controller: Controller,
            physics: Physics,
            cacheExtent: _cacheExtent,
            cacheExtentStyle: _cacheExtentStyle);
    }

    private static bool HasNonZeroPadding(Thickness padding)
    {
        return Math.Abs(padding.Left) > 0.0001
               || Math.Abs(padding.Top) > 0.0001
               || Math.Abs(padding.Right) > 0.0001
               || Math.Abs(padding.Bottom) > 0.0001;
    }
}
