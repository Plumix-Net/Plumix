using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Tests;

/// <summary>
/// A stand-alone <see cref="IScrollContext"/> for tests that drive a <see cref="ScrollPosition"/>
/// without a <see cref="Scrollable"/>. Records what the position pushes into it.
/// </summary>
internal sealed class TestScrollContext : IScrollContext, ITickerProvider
{
    private readonly List<Ticker> _tickers = [];

    public TestScrollContext(
        AxisDirection axisDirection = AxisDirection.Down,
        double devicePixelRatio = 1.0,
        BuildContext? notificationContext = null)
    {
        AxisDirection = axisDirection;
        DevicePixelRatio = devicePixelRatio;
        NotificationContext = notificationContext;
    }

    public BuildContext? NotificationContext { get; set; }

    /// <summary>
    /// The storage context is the notification context when one is set, and otherwise a mounted but
    /// empty one: <see cref="ScrollPosition"/> reads <see cref="PageStorage"/> through it from its
    /// constructor, exactly as Flutter does, so it may never be absent.
    /// </summary>
    public BuildContext StorageContext => NotificationContext ?? EmptyBuildContext.Instance;

    public ITickerProvider Vsync => this;

    public AxisDirection AxisDirection { get; set; }

    public double DevicePixelRatio { get; set; }

    public bool IgnorePointer { get; private set; }

    public List<bool> IgnorePointerLog { get; } = [];

    public bool? CanDrag { get; private set; }

    public List<bool> CanDragLog { get; } = [];

    public SemanticsActions? SemanticsActions { get; private set; }

    public List<double> SavedOffsets { get; } = [];

    public void SetIgnorePointer(bool value)
    {
        IgnorePointer = value;
        IgnorePointerLog.Add(value);
    }

    public void SetCanDrag(bool value)
    {
        CanDrag = value;
        CanDragLog.Add(value);
    }

    public void SetSemanticsActions(SemanticsActions actions)
    {
        SemanticsActions = actions;
    }

    public void SaveOffset(double offset)
    {
        SavedOffsets.Add(offset);
    }

    public Ticker CreateTicker(TickerCallback onTick)
    {
        var ticker = new Ticker(onTick);
        _tickers.Add(ticker);
        return ticker;
    }
}

/// <summary>
/// Drives a <see cref="ScrollPosition"/> through the same <see cref="IDrag"/> API a
/// <c>DragGestureRecognizer</c> uses, which is how Flutter's own tests exercise a drag without a
/// gesture arena.
/// </summary>
internal static class ScrollPositionTestDriver
{
    /// <summary>Begins a drag activity, as <c>ScrollableState.HandleDragStart</c> does.</summary>
    public static IDrag StartDrag(this ScrollPosition position, Action? dragCancelCallback = null)
    {
        return position.Drag(
            new DragStartDetails(default, SourceTimeStampUtc: DateTime.UnixEpoch),
            dragCancelCallback);
    }

    /// <summary>Reports a drag delta along the primary axis.</summary>
    public static void DragBy(this IDrag drag, double primaryDelta, TimeSpan? elapsed = null)
    {
        drag.Update(new DragUpdateDetails(
            default,
            default,
            default,
            primaryDelta,
            DateTime.UnixEpoch + (elapsed ?? TimeSpan.Zero)));
    }

    /// <summary>Lifts the pointer at the given primary-axis velocity.</summary>
    public static void EndDrag(this IDrag drag, double primaryVelocity = 0.0)
    {
        drag.End(new DragEndDetails(primaryVelocity));
    }
}

/// <summary>
/// A mounted <see cref="BuildContext"/> with no ancestors, for the scroll positions tests drive
/// outside a widget tree. Every ancestor lookup through it (notably
/// <see cref="PageStorage.MaybeOf"/>) returns null instead of asserting.
/// </summary>
internal static class EmptyBuildContext
{
    private static readonly BuildOwner Owner = new();
    private static BuildContext? _instance;

    public static BuildContext Instance
    {
        get
        {
            if (_instance != null)
            {
                return _instance;
            }

            BuildContext? captured = null;
            var root = new EmptyRootElement(new Builder(context =>
            {
                captured = context;
                return new SizedBox();
            }));
            root.Attach(Owner);
            Owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
            Owner.FlushBuild();
            _instance = captured!;
            return _instance;
        }
    }

    private sealed class EmptyRootElement(Widget widget) : Element(widget), IRenderObjectHost
    {
        private Element? _child;

        protected override void OnMount()
        {
            base.OnMount();
            Rebuild();
        }

        protected override void PerformRebuild()
        {
            base.PerformRebuild();
            _child = UpdateChild(_child, Widget, Slot);
        }

        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Owner!.BuildScope(this, () => Rebuild(force: true));
        }

        public override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
            }
        }

        public override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
        }
    }
}

/// <summary>
/// A mounted <see cref="IScrollContext"/> whose notification context sits under a listener that
/// records every <see cref="Notification"/> a <see cref="ScrollPosition"/> dispatches.
/// </summary>
internal sealed class ScrollNotificationRecorder : IScrollContext, ITickerProvider, IDisposable
{
    private readonly BuildOwner _owner = new();
    private readonly List<Ticker> _tickers = [];
    private readonly Element _root;

    public ScrollNotificationRecorder(AxisDirection axisDirection = AxisDirection.Down)
    {
        AxisDirection = axisDirection;
        BuildContext? captured = null;
        var root = new RecorderRootElement(new NotificationListener<Notification>(
            onNotification: notification =>
            {
                Notifications.Add(notification);
                return false;
            },
            child: new Builder(context =>
            {
                captured = context;
                return new SizedBox();
            })));
        root.Attach(_owner);
        _owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
        _owner.FlushBuild();
        _root = root;
        NotificationContext = captured!;
    }

    /// <summary>Every notification dispatched through <see cref="NotificationContext"/>, in order.</summary>
    public List<Notification> Notifications { get; } = [];

    public BuildContext? NotificationContext { get; }

    public BuildContext StorageContext => NotificationContext!;

    public ITickerProvider Vsync => this;

    public AxisDirection AxisDirection { get; set; }

    public double DevicePixelRatio { get; set; } = 1.0;

    public List<double> SavedOffsets { get; } = [];

    /// <summary>Creates a position driven by this context.</summary>
    public ScrollPositionWithSingleContext CreatePosition(
        ScrollPhysics physics,
        double? initialPixels = 0.0,
        ScrollPosition? oldPosition = null)
    {
        return new ScrollPositionWithSingleContext(
            physics,
            this,
            initialPixels,
            keepScrollOffset: false,
            oldPosition: oldPosition);
    }

    public void SetIgnorePointer(bool value)
    {
    }

    public void SetCanDrag(bool value)
    {
    }

    public void SetSemanticsActions(SemanticsActions actions)
    {
    }

    public void SaveOffset(double offset) => SavedOffsets.Add(offset);

    public Ticker CreateTicker(TickerCallback onTick)
    {
        var ticker = new Ticker(onTick);
        _tickers.Add(ticker);
        return ticker;
    }

    public void Dispose()
    {
        foreach (Ticker ticker in _tickers)
        {
            ticker.Dispose();
        }

        _root.Unmount();
    }

    private sealed class RecorderRootElement(Widget widget) : Element(widget), IRenderObjectHost
    {
        private Element? _child;

        protected override void OnMount()
        {
            base.OnMount();
            Rebuild();
        }

        protected override void PerformRebuild()
        {
            base.PerformRebuild();
            _child = UpdateChild(_child, Widget, Slot);
        }

        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Owner!.BuildScope(this, () => Rebuild(force: true));
        }

        public override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
            }
        }

        public override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
        }
    }
}
