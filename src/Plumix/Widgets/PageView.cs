using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Physics;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/page_view.dart

/// <summary>
/// A controller for <see cref="PageView"/>, expressing its scroll offset in pages.
/// </summary>
public class PageController : ScrollController
{
    public PageController(
        int initialPage = 0,
        bool keepPage = true,
        double viewportFraction = 1.0,
        Action<ScrollPosition>? onAttach = null,
        Action<ScrollPosition>? onDetach = null)
        : base(keepScrollOffset: keepPage)
    {
        if (!double.IsFinite(viewportFraction) || viewportFraction <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(viewportFraction),
                "viewportFraction must be positive and finite.");
        }

        InitialPage = initialPage;
        KeepPage = keepPage;
        ViewportFraction = viewportFraction;
        OnAttach = onAttach;
        OnDetach = onDetach;
    }

    /// <summary>The page to show when first creating the <see cref="PageView"/>.</summary>
    public int InitialPage { get; }

    /// <summary>Whether the resulting page is persisted with <see cref="PageStorage"/>.</summary>
    public bool KeepPage { get; }

    /// <summary>The fraction of the viewport each page should occupy.</summary>
    public double ViewportFraction { get; }

    /// <summary>Called when a <see cref="ScrollPosition"/> is attached to this controller.</summary>
    public Action<ScrollPosition>? OnAttach { get; }

    /// <summary>Called when a <see cref="ScrollPosition"/> is detached from this controller.</summary>
    public Action<ScrollPosition>? OnDetach { get; }

    /// <summary>
    /// The current page, which may be fractional while a scroll is in flight. Null while the
    /// attached position has no offset or before its content dimensions are established.
    /// </summary>
    public double? Page
    {
        get
        {
            if (Positions.Count == 0)
            {
                throw new InvalidOperationException(
                    "PageController.page cannot be accessed before a PageView is built with it.");
            }

            if (Positions.Count > 1)
            {
                throw new InvalidOperationException(
                    "The page property cannot be read when multiple PageViews are attached to the "
                    + "same PageController.");
            }

            return ((PagePosition)Positions[0]).Page;
        }
    }

    private PagePosition AttachedPosition => (PagePosition)Positions[0];

    /// <summary>Animates the controlled page view to the given page.</summary>
    public Task AnimateToPage(int page, TimeSpan duration, Curve? curve = null)
    {
        DebugCheckPageControllerAttached();
        PagePosition position = AttachedPosition;
        if (position.CachedPage != null)
        {
            position.CachedPage = page;
            return Task.CompletedTask;
        }

        if (!position.HasViewportDimension)
        {
            position.PageToUseOnStartup = page;
            return Task.CompletedTask;
        }

        return position.AnimateTo(position.GetPixelsFromPage(page), duration, curve ?? Curves.Ease);
    }

    /// <summary>Changes which page is displayed in the controlled page view without animating.</summary>
    public void JumpToPage(int page)
    {
        DebugCheckPageControllerAttached();
        PagePosition position = AttachedPosition;
        if (position.CachedPage != null)
        {
            position.CachedPage = page;
            return;
        }

        if (!position.HasViewportDimension)
        {
            position.PageToUseOnStartup = page;
            return;
        }

        position.JumpTo(position.GetPixelsFromPage(page));
    }

    /// <summary>Animates to the next page.</summary>
    public Task NextPage(TimeSpan duration, Curve? curve = null) =>
        AnimateToPage((int)Math.Round(Page!.Value, MidpointRounding.AwayFromZero) + 1, duration, curve);

    /// <summary>Animates to the previous page.</summary>
    public Task PreviousPage(TimeSpan duration, Curve? curve = null) =>
        AnimateToPage((int)Math.Round(Page!.Value, MidpointRounding.AwayFromZero) - 1, duration, curve);

    public override ScrollPosition CreateScrollPosition(ScrollPhysics? physics = null)
    {
        return new PagePosition(
            physics: physics ?? Physics,
            initialPage: InitialPage,
            keepPage: KeepPage,
            viewportFraction: ViewportFraction);
    }

    internal override void Attach(ScrollPosition position)
    {
        base.Attach(position);
        ((PagePosition)position).ViewportFraction = ViewportFraction;
        OnAttach?.Invoke(position);
    }

    internal override void Detach(ScrollPosition position)
    {
        base.Detach(position);
        OnDetach?.Invoke(position);
    }

    /// <summary>Dart parity: <c>_debugCheckPageControllerAttached</c>.</summary>
    private void DebugCheckPageControllerAttached()
    {
        if (Positions.Count == 0)
        {
            throw new InvalidOperationException("PageController is not attached to a PageView.");
        }

        if (Positions.Count > 1)
        {
            throw new InvalidOperationException(
                "Multiple PageViews are attached to the same PageController.");
        }
    }
}

/// <summary>
/// The <see cref="ScrollPosition"/> a <see cref="PageController"/> creates, which measures its
/// offset in whole viewport-fraction-sized pages.
/// </summary>
internal sealed class PagePosition : ScrollPosition
{
    private double _viewportFraction;
    private double _pageToUseOnStartup;
    private double? _cachedPage;

    public PagePosition(
        ScrollPhysics? physics = null,
        int initialPage = 0,
        bool keepPage = true,
        double viewportFraction = 1.0)
        : base(initialPixels: null, physics: physics, keepScrollOffset: keepPage)
    {
        if (!double.IsFinite(viewportFraction) || viewportFraction <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(viewportFraction));
        }

        InitialPage = initialPage;
        _viewportFraction = viewportFraction;
        _pageToUseOnStartup = initialPage;
    }

    public int InitialPage { get; }

    /// <summary>The page held while the viewport has no extent to derive an offset from.</summary>
    public double? CachedPage
    {
        get => _cachedPage;
        set => _cachedPage = value;
    }

    /// <summary>The page to land on once the first layout supplies a viewport dimension.</summary>
    public double PageToUseOnStartup
    {
        get => _pageToUseOnStartup;
        set => _pageToUseOnStartup = value;
    }

    public double ViewportFraction
    {
        get => _viewportFraction;
        set
        {
            if (_viewportFraction == value)
            {
                return;
            }

            double? oldPage = Page;
            _viewportFraction = value;
            if (oldPage != null)
            {
                ForcePixels(GetPixelsFromPage(oldPage.Value));
            }
        }
    }

    /// <summary>
    /// The leading offset that centers the first page when a page is wider than the viewport.
    /// </summary>
    private double InitialPageOffset =>
        Math.Max(0, ViewportDimension * (ViewportFraction - 1) / 2);

    /// <summary>
    /// The current page, or null before the offset and the content dimensions are established.
    /// </summary>
    public double? Page
    {
        get
        {
            if (!HasPixels)
            {
                return null;
            }

            if (_cachedPage != null)
            {
                return _cachedPage;
            }

            if (!HasContentDimensions && !HaveDimensions)
            {
                return null;
            }

            if (!HasViewportDimension || ViewportDimension <= 0.0)
            {
                return null;
            }

            return GetPageFromPixels(
                Math.Clamp(Pixels, MinScrollExtent, Math.Max(MinScrollExtent, MaxScrollExtent)),
                ViewportDimension);
        }
    }

    public double GetPageFromPixels(double pixels, double viewportDimension)
    {
        if (viewportDimension <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(viewportDimension));
        }

        double actual = Math.Max(0.0, pixels - InitialPageOffset)
                        / (viewportDimension * ViewportFraction);
        double round = Math.Round(actual, MidpointRounding.AwayFromZero);
        return Math.Abs(actual - round) < Constants.PrecisionErrorTolerance ? round : actual;
    }

    public double GetPixelsFromPage(double page) =>
        (page * ViewportDimension * ViewportFraction) + InitialPageOffset;

    public override void SaveScrollOffset()
    {
        WriteStorageOffset(_cachedPage ?? PageForStorage());
    }

    public override void RestoreScrollOffset()
    {
        if (HasPixels)
        {
            return;
        }

        if (ReadStorageOffset() is { } page)
        {
            _pageToUseOnStartup = page;
        }
    }

    public override void RestoreOffset(double offset, bool initialRestore = false)
    {
        if (initialRestore)
        {
            _pageToUseOnStartup = offset;
            return;
        }

        JumpTo(GetPixelsFromPage(offset));
    }

    public override bool ApplyViewportDimension(double viewportDimension)
    {
        double? oldViewportDimensions = HasViewportDimension ? ViewportDimension : null;
        if (oldViewportDimensions is { } previous && previous == viewportDimension)
        {
            return true;
        }

        bool result = base.ApplyViewportDimension(viewportDimension);
        double? oldPixels = HasPixels ? Pixels : null;
        double page;
        if (oldPixels == null)
        {
            page = _pageToUseOnStartup;
        }
        else if (oldViewportDimensions == 0.0)
        {
            page = _cachedPage ?? _pageToUseOnStartup;
        }
        else
        {
            page = GetPageFromPixels(oldPixels.Value, oldViewportDimensions!.Value);
        }

        double newPixels = GetPixelsFromPage(page);
        _cachedPage = viewportDimension == 0.0 ? page : null;

        if (oldPixels == null || Math.Abs(newPixels - oldPixels.Value) > Constants.PrecisionErrorTolerance)
        {
            CorrectPixels(newPixels);
            return false;
        }

        return result;
    }

    public override bool ApplyContentDimensions(double minScrollExtent, double maxScrollExtent)
    {
        double newMinScrollExtent = minScrollExtent + InitialPageOffset;
        return base.ApplyContentDimensions(
            newMinScrollExtent,
            Math.Max(newMinScrollExtent, maxScrollExtent - InitialPageOffset));
    }

    /// <summary>
    /// Adds the viewport fraction to the shared metrics snapshot, which is where Flutter's
    /// <c>PageMetrics</c> subclass carries it.
    /// </summary>
    public override ScrollMetricsSnapshot CopyWith()
    {
        return base.CopyWith() with { ViewportFraction = ViewportFraction };
    }

    public override void Absorb(ScrollPosition other)
    {
        base.Absorb(other);
        if (other is not PagePosition page)
        {
            return;
        }

        _pageToUseOnStartup = page._pageToUseOnStartup;
        if (page._cachedPage != null)
        {
            _cachedPage = page._cachedPage;
        }
    }

    private double PageForStorage() =>
        HasPixels && HasViewportDimension && ViewportDimension > 0.0
            ? GetPageFromPixels(Pixels, ViewportDimension)
            : _pageToUseOnStartup;
}

/// <summary>
/// Scroll physics used by page views, which always land on a whole page boundary.
/// </summary>
public sealed class PageScrollPhysics : ScrollPhysics
{
    public PageScrollPhysics(ScrollPhysics? parent = null) : base(parent)
    {
    }

    public override ScrollPhysics ApplyTo(ScrollPhysics? ancestor) => new PageScrollPhysics(BuildParent(ancestor));

    public override Simulation? CreateBallisticSimulation(IScrollMetrics position, double velocity)
    {
        // If we're out of range and not headed back in range, defer to the parent ballistics, which
        // should put us back in range at a page boundary.
        if ((velocity <= 0.0 && position.Pixels <= position.MinScrollExtent)
            || (velocity >= 0.0 && position.Pixels >= position.MaxScrollExtent))
        {
            return base.CreateBallisticSimulation(position, velocity);
        }

        Tolerance tolerance = ToleranceFor(position);
        double target = GetTargetPixels(position, tolerance, velocity);
        if (Math.Abs(target - position.Pixels) > Constants.PrecisionErrorTolerance)
        {
            return new ScrollSpringSimulation(Spring, position.Pixels, target, velocity, tolerance);
        }

        return null;
    }

    public override bool AllowImplicitScrolling => false;

    private static double GetPage(IScrollMetrics position) => position is PagePosition page
        ? page.Page ?? 0.0
        : position.Pixels / Math.Max(1.0, position.ViewportDimension);

    private static double GetPixels(IScrollMetrics position, double page) => position is PagePosition pagePosition
        ? pagePosition.GetPixelsFromPage(page)
        : page * position.ViewportDimension;

    private static double GetTargetPixels(IScrollMetrics position, Tolerance tolerance, double velocity)
    {
        double page = GetPage(position);
        if (velocity < -tolerance.Velocity)
        {
            page -= 0.5;
        }
        else if (velocity > tolerance.Velocity)
        {
            page += 0.5;
        }

        return GetPixels(position, Math.Round(page, MidpointRounding.AwayFromZero));
    }
}

/// <summary>
/// Physics that force <see cref="ScrollPhysics.AllowImplicitScrolling"/> to a fixed value, so a
/// page view can opt its viewport into building the neighboring pages for accessibility.
/// </summary>
internal sealed class ForceImplicitScrollPhysics : ScrollPhysics
{
    public ForceImplicitScrollPhysics(bool allowImplicitScrolling, ScrollPhysics? parent = null)
        : base(parent)
    {
        AllowImplicitScrolling = allowImplicitScrolling;
    }

    public override bool AllowImplicitScrolling { get; }

    public override ScrollPhysics ApplyTo(ScrollPhysics? ancestor) =>
        new ForceImplicitScrollPhysics(AllowImplicitScrolling, BuildParent(ancestor));
}

/// <summary>
/// A scrollable list that works page by page.
/// </summary>
public sealed class PageView : StatefulWidget
{
    private static readonly PageScrollPhysics KPagePhysics = new();

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
        ScrollCacheExtent? scrollCacheExtent = null,
        string? restorationId = null,
        Clip clipBehavior = Clip.HardEdge,
        HitTestBehavior hitTestBehavior = HitTestBehavior.Opaque,
        ScrollBehavior? scrollBehavior = null,
        bool padEnds = true,
        Key? key = null) : this(
        childrenDelegate: new SliverChildListDelegate(children ?? []),
        controller: controller,
        scrollDirection: scrollDirection,
        reverse: reverse,
        physics: physics,
        pageSnapping: pageSnapping,
        onPageChanged: onPageChanged,
        dragStartBehavior: dragStartBehavior,
        allowImplicitScrolling: allowImplicitScrolling,
        scrollCacheExtent: scrollCacheExtent,
        restorationId: restorationId,
        clipBehavior: clipBehavior,
        hitTestBehavior: hitTestBehavior,
        scrollBehavior: scrollBehavior,
        padEnds: padEnds,
        key: key)
    {
    }

    /// <summary>Dart parity: <c>PageView.custom</c>.</summary>
    public PageView(
        SliverChildDelegate childrenDelegate,
        PageController? controller = null,
        Axis scrollDirection = Axis.Horizontal,
        bool reverse = false,
        ScrollPhysics? physics = null,
        bool pageSnapping = true,
        Action<int>? onPageChanged = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        bool allowImplicitScrolling = false,
        ScrollCacheExtent? scrollCacheExtent = null,
        string? restorationId = null,
        Clip clipBehavior = Clip.HardEdge,
        HitTestBehavior hitTestBehavior = HitTestBehavior.Opaque,
        ScrollBehavior? scrollBehavior = null,
        bool padEnds = true,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(childrenDelegate);
        if (scrollCacheExtent != null && scrollCacheExtent.Value > 0.0 != allowImplicitScrolling)
        {
            throw new ArgumentException(
                "scrollCacheExtent and allowImplicitScrolling must be consistent: scrollCacheExtent "
                + "must be greater than 0.0 when allowImplicitScrolling is true, and must be 0.0 "
                + "when allowImplicitScrolling is false.",
                nameof(scrollCacheExtent));
        }

        ChildrenDelegate = childrenDelegate;
        Controller = controller;
        ScrollDirection = scrollDirection;
        Reverse = reverse;
        Physics = physics;
        PageSnapping = pageSnapping;
        OnPageChanged = onPageChanged;
        DragStartBehavior = dragStartBehavior;
        AllowImplicitScrolling = allowImplicitScrolling;
        ScrollCacheExtent = scrollCacheExtent
                            ?? Rendering.ScrollCacheExtent.Viewport(allowImplicitScrolling ? 1.0 : 0.0);
        RestorationId = restorationId;
        ClipBehavior = clipBehavior;
        HitTestBehavior = hitTestBehavior;
        ScrollBehavior = scrollBehavior;
        PadEnds = padEnds;
    }

    /// <summary>Dart parity: <c>PageView.builder</c>.</summary>
    public static PageView Builder(
        IndexedWidgetBuilder itemBuilder,
        int? itemCount = null,
        ChildIndexGetter? findChildIndexCallback = null,
        PageController? controller = null,
        Axis scrollDirection = Axis.Horizontal,
        bool reverse = false,
        ScrollPhysics? physics = null,
        bool pageSnapping = true,
        Action<int>? onPageChanged = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        bool allowImplicitScrolling = false,
        ScrollCacheExtent? scrollCacheExtent = null,
        string? restorationId = null,
        Clip clipBehavior = Clip.HardEdge,
        HitTestBehavior hitTestBehavior = HitTestBehavior.Opaque,
        ScrollBehavior? scrollBehavior = null,
        bool padEnds = true,
        Key? key = null)
    {
        return new PageView(
            childrenDelegate: new SliverChildBuilderDelegate(
                itemBuilder,
                childCount: itemCount,
                findChildIndexCallback: findChildIndexCallback),
            controller: controller,
            scrollDirection: scrollDirection,
            reverse: reverse,
            physics: physics,
            pageSnapping: pageSnapping,
            onPageChanged: onPageChanged,
            dragStartBehavior: dragStartBehavior,
            allowImplicitScrolling: allowImplicitScrolling,
            scrollCacheExtent: scrollCacheExtent,
            restorationId: restorationId,
            clipBehavior: clipBehavior,
            hitTestBehavior: hitTestBehavior,
            scrollBehavior: scrollBehavior,
            padEnds: padEnds,
            key: key);
    }

    public bool AllowImplicitScrolling { get; }

    public ScrollCacheExtent ScrollCacheExtent { get; }

    public string? RestorationId { get; }

    public Axis ScrollDirection { get; }

    public bool Reverse { get; }

    public PageController? Controller { get; }

    public ScrollPhysics? Physics { get; }

    public bool PageSnapping { get; }

    public Action<int>? OnPageChanged { get; }

    public SliverChildDelegate ChildrenDelegate { get; }

    public DragStartBehavior DragStartBehavior { get; }

    public Clip ClipBehavior { get; }

    public HitTestBehavior HitTestBehavior { get; }

    public ScrollBehavior? ScrollBehavior { get; }

    public bool PadEnds { get; }

    public override State CreateState() => new PageViewState();

    private sealed class PageViewState : State
    {
        private int _lastReportedPage;
        private PageController _controller = null!;
        private bool _ownsController;

        private PageView CurrentWidget => (PageView)StateWidget;

        public override void InitState()
        {
            InitController();
            _lastReportedPage = _controller.InitialPage;
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var old = (PageView)oldWidget;
            if (ReferenceEquals(old.Controller, CurrentWidget.Controller))
            {
                return;
            }

            if (old.Controller is null)
            {
                _controller.Dispose();
            }

            InitController();
        }

        public override void Dispose()
        {
            if (_ownsController)
            {
                _controller.Dispose();
            }
        }

        public override Widget Build(BuildContext context)
        {
            PageView widget = CurrentWidget;
            ScrollBehavior configuration = widget.ScrollBehavior ?? ScrollConfiguration.Of(context);
            ScrollPhysics ambient = configuration.GetScrollPhysics(context);

            // Flutter leaves the tail null and lets Scrollable append the ambient physics; the
            // chain is composed here instead so the whole PageView chain reaches the position.
            ScrollPhysics tail = widget.Physics?.ApplyTo(ambient)
                                 ?? widget.ScrollBehavior?.GetScrollPhysics(context).ApplyTo(ambient)
                                 ?? ambient;
            ScrollPhysics physics = new ForceImplicitScrollPhysics(widget.AllowImplicitScrolling)
                .ApplyTo(widget.PageSnapping ? KPagePhysics.ApplyTo(tail) : tail);

            return new NotificationListener<ScrollNotification>(
                onNotification: HandleScrollNotification,
                child: new Scrollable(
                    axis: widget.ScrollDirection,
                    reverse: widget.Reverse,
                    controller: _controller,
                    physics: physics,
                    dragStartBehavior: widget.DragStartBehavior,
                    restorationId: widget.RestorationId,
                    hitTestBehavior: widget.HitTestBehavior,
                    scrollBehavior: widget.ScrollBehavior
                                    ?? ScrollConfiguration.Of(context).CopyWith(scrollbars: false),
                    cacheExtent: widget.ScrollCacheExtent.Value,
                    cacheExtentStyle: widget.ScrollCacheExtent.Style,
                    clipBehavior: widget.ClipBehavior,
                    slivers:
                    [
                        new SliverFillViewport(
                            widget.ChildrenDelegate,
                            viewportFraction: _controller.ViewportFraction,
                            padEnds: widget.PadEnds,
                            allowImplicitScrolling: widget.AllowImplicitScrolling),
                    ]));
        }

        private void InitController()
        {
            _ownsController = CurrentWidget.Controller is null;
            _controller = CurrentWidget.Controller ?? new PageController();
        }

        private bool HandleScrollNotification(ScrollNotification notification)
        {
            if (notification.Depth != 0
                || CurrentWidget.OnPageChanged is null
                || notification is not ScrollUpdateNotification)
            {
                return false;
            }

            int currentPage = (int)Math.Round(notification.Metrics.Page, MidpointRounding.AwayFromZero);
            if (currentPage != _lastReportedPage)
            {
                _lastReportedPage = currentPage;
                CurrentWidget.OnPageChanged(currentPage);
            }

            return false;
        }
    }
}
