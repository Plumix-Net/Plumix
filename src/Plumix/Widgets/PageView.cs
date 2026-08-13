using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/page_view.dart

public sealed class PageController : ChangeNotifier
{
    private Plumix.AnimationController? _animationController;
    private double _animationStart;
    private double _animationTarget;
    private double _page;
    private int _pageCount;

    public PageController(int initialPage = 0, bool keepPage = true, double viewportFraction = 1.0)
    {
        if (initialPage < 0) throw new ArgumentOutOfRangeException(nameof(initialPage));
        if (!double.IsFinite(viewportFraction) || viewportFraction <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(viewportFraction));
        }

        InitialPage = initialPage;
        KeepPage = keepPage;
        ViewportFraction = viewportFraction;
        _page = initialPage;
    }

    public int InitialPage { get; }

    public bool KeepPage { get; }

    public double ViewportFraction { get; }

    public double? Page => HasClients ? _page : null;

    public bool HasClients { get; private set; }

    internal event Action? AnimationCompleted;

    internal double EffectivePage => _page;

    internal double ViewportDimension { get; private set; }

    internal int PageCount => _pageCount;

    /// <summary>
    /// The current page position expressed as scroll metrics, so a page view can report Flutter's
    /// scroll notifications without owning a <see cref="ScrollPosition"/>.
    /// </summary>
    internal ScrollMetricsSnapshot Metrics(Axis axis)
    {
        double extent = ViewportDimension * ViewportFraction;
        return new ScrollMetricsSnapshot(
            Pixels: _page * extent,
            MinScrollExtent: 0.0,
            MaxScrollExtent: Math.Max(0.0, (_pageCount - 1) * extent),
            ViewportDimension: ViewportDimension,
            AxisDirection: axis == Axis.Horizontal ? AxisDirection.Right : AxisDirection.Down);
    }

    public void JumpToPage(int page)
    {
        StopAnimation();
        SetPage(page);
    }

    public void AnimateToPage(int page, TimeSpan duration, Curve? curve = null)
    {
        double target = ClampPage(page);
        if (duration <= TimeSpan.Zero || Math.Abs(target - _page) <= double.Epsilon)
        {
            JumpToPage((int)Math.Round(target));
            return;
        }

        StopAnimation();
        _animationStart = _page;
        _animationTarget = target;
        _animationController = new Plumix.AnimationController(duration)
        {
            Curve = curve ?? Curves.Ease,
        };
        _animationController.Changed += HandleAnimationChanged;
        _animationController.Completed += HandleAnimationCompleted;
        _animationController.Forward(0);
    }

    internal void UpdatePageFromDrag(double page) => SetPage(page);

    internal void AttachViewport(double viewportDimension, int pageCount)
    {
        HasClients = true;
        ViewportDimension = Math.Max(0, viewportDimension);
        _pageCount = Math.Max(0, pageCount);
        _page = ClampPage(_page);
    }

    internal void DetachViewport()
    {
        HasClients = false;
        ViewportDimension = 0;
        _pageCount = 0;
    }

    public override void Dispose()
    {
        StopAnimation();
        base.Dispose();
    }

    private void HandleAnimationChanged()
    {
        if (_animationController is null) return;
        SetPage(_animationStart + ((_animationTarget - _animationStart) * _animationController.Evaluate()));
    }

    private void HandleAnimationCompleted()
    {
        SetPage(_animationTarget);
        AnimationCompleted?.Invoke();
        StopAnimation();
    }

    private void SetPage(double page)
    {
        double value = ClampPage(page);
        if (Math.Abs(_page - value) <= 0.000001) return;
        _page = value;
        NotifyListeners();
    }

    private double ClampPage(double page) => _pageCount <= 0
        ? Math.Max(0, page)
        : Math.Clamp(page, 0, _pageCount - 1);

    private void StopAnimation()
    {
        if (_animationController is null) return;
        _animationController.Changed -= HandleAnimationChanged;
        _animationController.Completed -= HandleAnimationCompleted;
        _animationController.Dispose();
        _animationController = null;
    }
}

public sealed class PageScrollPhysics : ScrollPhysics
{
    public PageScrollPhysics(ScrollPhysics? parent = null) : base(parent)
    {
    }
}

public sealed class PageView : StatefulWidget
{
    public PageView(
        IReadOnlyList<Widget>? children = null,
        PageController? controller = null,
        Axis scrollDirection = Axis.Horizontal,
        bool reverse = false,
        ScrollPhysics? physics = null,
        bool pageSnapping = true,
        Action<int>? onPageChanged = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        bool allowImplicitScrolling = false,
        Clip clipBehavior = Clip.HardEdge,
        Key? key = null) : base(key)
    {
        Children = children ?? Array.Empty<Widget>();
        Controller = controller;
        ScrollDirection = scrollDirection;
        Reverse = reverse;
        Physics = physics;
        PageSnapping = pageSnapping;
        OnPageChanged = onPageChanged;
        DragStartBehavior = dragStartBehavior;
        AllowImplicitScrolling = allowImplicitScrolling;
        ClipBehavior = clipBehavior;
    }

    public IReadOnlyList<Widget> Children { get; }
    public PageController? Controller { get; }
    public Axis ScrollDirection { get; }
    public bool Reverse { get; }
    public ScrollPhysics? Physics { get; }
    public bool PageSnapping { get; }
    public Action<int>? OnPageChanged { get; }
    public DragStartBehavior DragStartBehavior { get; }
    public bool AllowImplicitScrolling { get; }
    public Clip ClipBehavior { get; }

    public override State CreateState() => new PageViewState();

    private sealed class PageViewState : State
    {
        private PageController? _controller;
        private bool _ownsController;
        private int _lastReportedPage;
        private bool _dragUnderway;

        private PageView CurrentWidget => (PageView)StateWidget;

        public override void InitState() => AttachController(CurrentWidget.Controller);

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldPageView = (PageView)oldWidget;
            if (!ReferenceEquals(oldPageView.Controller, CurrentWidget.Controller))
            {
                DetachController();
                AttachController(CurrentWidget.Controller);
            }
        }

        public override void Dispose()
        {
            if (_controller is not null)
            {
                _controller.AnimationCompleted -= HandleSettleCompleted;
            }

            DetachController();
        }

        public override Widget Build(BuildContext context)
        {
            var widget = CurrentWidget;
            bool reverse = widget.Reverse ^
                           (widget.ScrollDirection == Axis.Horizontal && Directionality.Of(context) == TextDirection.Rtl);
            Widget result = new PageViewport(
                children: widget.Children,
                controller: _controller!,
                axis: widget.ScrollDirection,
                reverse: reverse,
                clipBehavior: widget.ClipBehavior);

            return widget.ScrollDirection == Axis.Horizontal
                ? new GestureDetector(
                    behavior: HitTestBehavior.Opaque,
                    onHorizontalDragUpdate: HandleDragUpdate,
                    onHorizontalDragEnd: HandleDragEnd,
                    onHorizontalDragCancel: HandleDragCancel,
                    child: result)
                : new GestureDetector(
                    behavior: HitTestBehavior.Opaque,
                    onVerticalDragUpdate: HandleDragUpdate,
                    onVerticalDragEnd: HandleDragEnd,
                    onVerticalDragCancel: HandleDragCancel,
                    child: result);
        }

        private void AttachController(PageController? provided)
        {
            _controller = provided ?? new PageController();
            _ownsController = provided is null;
            _lastReportedPage = _controller.InitialPage;
            _controller.AddListener(HandleControllerChanged);
        }

        private void DetachController()
        {
            if (_controller is null) return;
            _controller.RemoveListener(HandleControllerChanged);
            _controller.DetachViewport();
            if (_ownsController) _controller.Dispose();
            _controller = null;
            _ownsController = false;
        }

        private void HandleControllerChanged()
        {
            if (!Mounted || _controller is null) return;
            int page = (int)Math.Round(_controller.EffectivePage);
            if (page != _lastReportedPage)
            {
                _lastReportedPage = page;
                CurrentWidget.OnPageChanged?.Invoke(page);
            }

            DispatchScrollUpdate();
            SetState(() => { });
        }

        private void DispatchScrollUpdate()
        {
            if (_controller is null || !_controller.HasClients) return;
            _ = new ScrollUpdateNotification(
                _controller.Metrics(CurrentWidget.ScrollDirection),
                scrollDelta: null,
                hasDragDetails: _dragUnderway).Dispatch(Context);
        }

        private void DispatchScrollEnd()
        {
            if (_controller is null || !_controller.HasClients) return;
            _ = new ScrollEndNotification(_controller.Metrics(CurrentWidget.ScrollDirection)).Dispatch(Context);
        }

        private void SettleTo(int target)
        {
            if (_controller is null) return;
            if (!CurrentWidget.PageSnapping)
            {
                DispatchScrollEnd();
                return;
            }

            _controller.AnimationCompleted -= HandleSettleCompleted;
            _controller.AnimationCompleted += HandleSettleCompleted;
            _controller.AnimateToPage(target, TimeSpan.FromMilliseconds(300), Curves.Ease);
        }

        private void HandleSettleCompleted()
        {
            if (_controller is not null)
            {
                _controller.AnimationCompleted -= HandleSettleCompleted;
            }

            _dragUnderway = false;
            DispatchScrollEnd();
        }

        private void HandleDragUpdate(DragUpdateDetails details)
        {
            if (_controller is null || CurrentWidget.Children.Count < 2) return;
            double extent = _controller.ViewportDimension * _controller.ViewportFraction;
            if (extent <= 0) return;
            bool reverse = CurrentWidget.Reverse ^
                           (CurrentWidget.ScrollDirection == Axis.Horizontal &&
                            Directionality.Of(Context) == TextDirection.Rtl);
            int direction = reverse ? 1 : -1;
            _dragUnderway = true;
            _controller.UpdatePageFromDrag(_controller.EffectivePage + (direction * details.PrimaryDelta / extent));
        }

        private void HandleDragEnd(DragEndDetails details)
        {
            if (_controller is null) return;
            double page = _controller.EffectivePage;
            int target;
            if (Math.Abs(details.PrimaryVelocity) >= 50)
            {
                bool reverse = CurrentWidget.Reverse ^
                               (CurrentWidget.ScrollDirection == Axis.Horizontal &&
                                Directionality.Of(Context) == TextDirection.Rtl);
                bool towardNext = reverse ? details.PrimaryVelocity > 0 : details.PrimaryVelocity < 0;
                target = towardNext ? (int)Math.Floor(page + 1) : (int)Math.Ceiling(page - 1);
            }
            else
            {
                target = (int)Math.Round(page, MidpointRounding.AwayFromZero);
            }

            SettleTo(target);
        }

        private void HandleDragCancel()
        {
            if (_controller is null) return;
            SettleTo((int)Math.Round(_controller.EffectivePage));
        }
    }
}

internal sealed class PageViewport : MultiChildRenderObjectWidget
{
    public PageViewport(
        IReadOnlyList<Widget> children,
        PageController controller,
        Axis axis,
        bool reverse,
        Clip clipBehavior) : base(children)
    {
        Controller = controller;
        Page = controller.EffectivePage;
        Axis = axis;
        Reverse = reverse;
        ClipBehavior = clipBehavior;
    }

    public PageController Controller { get; }
    public double Page { get; }
    public Axis Axis { get; }
    public bool Reverse { get; }
    public Clip ClipBehavior { get; }

    internal override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderPageViewport(Controller, Page, Axis, Reverse, ClipBehavior);

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var viewport = (RenderPageViewport)renderObject;
        viewport.Controller = Controller;
        viewport.Page = Page;
        viewport.Axis = Axis;
        viewport.Reverse = Reverse;
        viewport.ClipBehavior = ClipBehavior;
    }
}
