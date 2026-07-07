using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/tabs.dart

public enum TabBarIndicatorSize { Tab, Label }
public enum TabAlignment { Start, StartOffset, Fill, Center }
public enum TabIndicatorAnimation { Linear, Elastic }

public sealed class Tab : StatelessWidget, IPreferredSizeWidget
{
    public Tab(
        string? text = null,
        Widget? icon = null,
        Thickness? iconMargin = null,
        double? height = null,
        Widget? child = null,
        Key? key = null) : base(key)
    {
        if (text is null && child is null && icon is null)
        {
            throw new ArgumentException("Tab requires text, child, or icon.");
        }

        if (text is not null && child is not null)
        {
            throw new ArgumentException("Provide either text or child, not both.");
        }

        if (height.HasValue && (!double.IsFinite(height.Value) || height.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        Text = text;
        Icon = icon;
        IconMargin = iconMargin;
        Height = height;
        Child = child;
    }

    public string? Text { get; }
    public Widget? Child { get; }
    public Widget? Icon { get; }
    public Thickness? IconMargin { get; }
    public double? Height { get; }

    public Size PreferredSize => new(0, Height ?? (HasLabel && Icon is not null ? 72 : 46));

    private bool HasLabel => Text is not null || Child is not null;

    public override Widget Build(BuildContext context)
    {
        Widget label = Child ?? (Text is not null
            ? new Text(Text, softWrap: false, overflow: TextOverflow.Fade)
            : Icon!);
        var calculatedHeight = 46.0;
        if (Icon is not null && HasLabel)
        {
            calculatedHeight = 72;
            var defaultMargin = Theme.Of(context).UseMaterial3
                ? new Thickness(0, 0, 0, 2)
                : new Thickness(0, 0, 0, 10);
            label = new Column(
                mainAxisAlignment: MainAxisAlignment.Center,
                children:
                [
                    new Padding(IconMargin ?? defaultMargin, Icon),
                    Child ?? new Text(Text!, softWrap: false, overflow: TextOverflow.Fade),
                ]);
        }

        return new SizedBox(
            height: Height ?? calculatedHeight,
            child: new Center(widthFactor: 1, child: label));
    }
}

public sealed class TabBar : StatefulWidget, IPreferredSizeWidget
{
    public TabBar(
        IReadOnlyList<Widget> tabs,
        TabController? controller = null,
        ScrollController? scrollController = null,
        bool isScrollable = false,
        Thickness? padding = null,
        Color? indicatorColor = null,
        bool automaticIndicatorColorAdjustment = true,
        double indicatorWeight = 2,
        Thickness? indicatorPadding = null,
        BoxDecoration? indicator = null,
        TabBarIndicatorSize? indicatorSize = null,
        Color? dividerColor = null,
        double? dividerHeight = null,
        Color? labelColor = null,
        TextStyle? labelStyle = null,
        Thickness? labelPadding = null,
        Color? unselectedLabelColor = null,
        TextStyle? unselectedLabelStyle = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        MaterialStateProperty<Color?>? overlayColor = null,
        MaterialStateProperty<MouseCursor?>? mouseCursor = null,
        bool? enableFeedback = null,
        Action<int>? onTap = null,
        Action<bool, int>? onHover = null,
        Action<bool, int>? onFocusChange = null,
        ScrollPhysics? physics = null,
        BorderRadius? splashBorderRadius = null,
        TabAlignment? tabAlignment = null,
        TabIndicatorAnimation? indicatorAnimation = null,
        Key? key = null) : this(
        tabs, controller, scrollController, isScrollable, padding, indicatorColor,
        automaticIndicatorColorAdjustment, indicatorWeight, indicatorPadding, indicator,
        indicatorSize, dividerColor, dividerHeight, labelColor, labelStyle, labelPadding,
        unselectedLabelColor, unselectedLabelStyle, dragStartBehavior, overlayColor, mouseCursor,
        enableFeedback, onTap, onHover, onFocusChange, physics, splashBorderRadius, tabAlignment,
        indicatorAnimation, isPrimary: true, key)
    {
    }

    private TabBar(
        IReadOnlyList<Widget> tabs,
        TabController? controller,
        ScrollController? scrollController,
        bool isScrollable,
        Thickness? padding,
        Color? indicatorColor,
        bool automaticIndicatorColorAdjustment,
        double indicatorWeight,
        Thickness? indicatorPadding,
        BoxDecoration? indicator,
        TabBarIndicatorSize? indicatorSize,
        Color? dividerColor,
        double? dividerHeight,
        Color? labelColor,
        TextStyle? labelStyle,
        Thickness? labelPadding,
        Color? unselectedLabelColor,
        TextStyle? unselectedLabelStyle,
        DragStartBehavior dragStartBehavior,
        MaterialStateProperty<Color?>? overlayColor,
        MaterialStateProperty<MouseCursor?>? mouseCursor,
        bool? enableFeedback,
        Action<int>? onTap,
        Action<bool, int>? onHover,
        Action<bool, int>? onFocusChange,
        ScrollPhysics? physics,
        BorderRadius? splashBorderRadius,
        TabAlignment? tabAlignment,
        TabIndicatorAnimation? indicatorAnimation,
        bool isPrimary,
        Key? key) : base(key)
    {
        Tabs = tabs ?? throw new ArgumentNullException(nameof(tabs));
        if (indicator is null && (!double.IsFinite(indicatorWeight) || indicatorWeight <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(indicatorWeight));
        }
        if (dividerHeight.HasValue && !double.IsFinite(dividerHeight.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(dividerHeight));
        }

        Controller = controller;
        ScrollController = scrollController;
        IsScrollable = isScrollable;
        Padding = padding;
        IndicatorColor = indicatorColor;
        AutomaticIndicatorColorAdjustment = automaticIndicatorColorAdjustment;
        IndicatorWeight = indicatorWeight;
        IndicatorPadding = indicatorPadding ?? new Thickness();
        Indicator = indicator;
        IndicatorSize = indicatorSize;
        DividerColor = dividerColor;
        DividerHeight = dividerHeight;
        LabelColor = labelColor;
        LabelStyle = labelStyle;
        LabelPadding = labelPadding;
        UnselectedLabelColor = unselectedLabelColor;
        UnselectedLabelStyle = unselectedLabelStyle;
        DragStartBehavior = dragStartBehavior;
        OverlayColor = overlayColor;
        MouseCursor = mouseCursor;
        EnableFeedback = enableFeedback;
        OnTap = onTap;
        OnHover = onHover;
        OnFocusChange = onFocusChange;
        Physics = physics;
        SplashBorderRadius = splashBorderRadius;
        TabAlignment = tabAlignment;
        IndicatorAnimation = indicatorAnimation;
        IsPrimary = isPrimary;
    }

    public static TabBar Secondary(
        IReadOnlyList<Widget> tabs,
        TabController? controller = null,
        bool isScrollable = false,
        Color? indicatorColor = null,
        double indicatorWeight = 2,
        TabBarIndicatorSize? indicatorSize = null,
        Color? labelColor = null,
        Color? unselectedLabelColor = null,
        Action<int>? onTap = null,
        Key? key = null) => new(
        tabs, controller, null, isScrollable, null, indicatorColor, true, indicatorWeight,
        null, null, indicatorSize, null, null, labelColor, null, null,
        unselectedLabelColor, null, DragStartBehavior.Start, null, null, null, onTap,
        null, null, null, null, null, null, isPrimary: false, key);

    public IReadOnlyList<Widget> Tabs { get; }
    public TabController? Controller { get; }
    public ScrollController? ScrollController { get; }
    public bool IsScrollable { get; }
    public Thickness? Padding { get; }
    public Color? IndicatorColor { get; }
    public bool AutomaticIndicatorColorAdjustment { get; }
    public double IndicatorWeight { get; }
    public Thickness IndicatorPadding { get; }
    public BoxDecoration? Indicator { get; }
    public TabBarIndicatorSize? IndicatorSize { get; }
    public Color? DividerColor { get; }
    public double? DividerHeight { get; }
    public Color? LabelColor { get; }
    public TextStyle? LabelStyle { get; }
    public Thickness? LabelPadding { get; }
    public Color? UnselectedLabelColor { get; }
    public TextStyle? UnselectedLabelStyle { get; }
    public DragStartBehavior DragStartBehavior { get; }
    public MaterialStateProperty<Color?>? OverlayColor { get; }
    public MaterialStateProperty<MouseCursor?>? MouseCursor { get; }
    public bool? EnableFeedback { get; }
    public Action<int>? OnTap { get; }
    public Action<bool, int>? OnHover { get; }
    public Action<bool, int>? OnFocusChange { get; }
    public ScrollPhysics? Physics { get; }
    public BorderRadius? SplashBorderRadius { get; }
    public TabAlignment? TabAlignment { get; }
    public TabIndicatorAnimation? IndicatorAnimation { get; }
    internal bool IsPrimary { get; }

    public Size PreferredSize
    {
        get
        {
            var height = 46.0;
            foreach (var tab in Tabs)
            {
                if (tab is IPreferredSizeWidget preferred) height = Math.Max(height, preferred.PreferredSize.Height);
            }
            return new Size(0, height + IndicatorWeight);
        }
    }

    public override State CreateState() => new TabBarState();

    private sealed class TabBarState : State
    {
        private TabController? _controller;
        private ScrollController? _internalScrollController;
        private IReadOnlyList<Rect> _tabRects = Array.Empty<Rect>();
        private double _tabStripWidth;
        private double _alignmentInset;
        private bool _scrollUpdateScheduled;
        private TabBar CurrentWidget => (TabBar)StateWidget;

        public override void DidChangeDependencies() => UpdateController();

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            if (!ReferenceEquals(((TabBar)oldWidget).Controller, CurrentWidget.Controller)) UpdateController();
        }

        public override void Dispose()
        {
            DetachController();
            _internalScrollController?.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            UpdateController();
            var widget = CurrentWidget;
            var controller = _controller!;
            if (controller.Length != widget.Tabs.Count)
            {
                throw new InvalidOperationException($"TabController length ({controller.Length}) must match tabs count ({widget.Tabs.Count}).");
            }
            if (controller.Length == 0) return new SizedBox(width: 0, height: 46 + widget.IndicatorWeight);

            var theme = Theme.Of(context);
            var tabTheme = TabBarTheme.Of(context);
            var indicatorSize = widget.IndicatorSize ?? tabTheme.IndicatorSize
                ?? (theme.UseMaterial3 && widget.IsPrimary ? TabBarIndicatorSize.Label : TabBarIndicatorSize.Tab);
            var alignment = widget.TabAlignment ?? tabTheme.TabAlignment
                ?? (widget.IsScrollable
                    ? theme.UseMaterial3 ? global::Plumix.Material.TabAlignment.StartOffset : global::Plumix.Material.TabAlignment.Start
                    : global::Plumix.Material.TabAlignment.Fill);
            ValidateAlignment(alignment, widget.IsScrollable);

            var selectedColor = widget.LabelColor ?? tabTheme.LabelColor
                ?? (theme.UseMaterial3
                    ? widget.IsPrimary ? theme.PrimaryColor : theme.OnSurfaceColor
                    : theme.PrimaryTextTheme.BodyLarge.Color ?? theme.OnPrimaryColor);
            var unselectedColor = widget.UnselectedLabelColor ?? tabTheme.UnselectedLabelColor
                ?? (theme.UseMaterial3 ? theme.OnSurfaceVariantColor : ApplyOpacity(selectedColor, 0.70));
            var selectedStyle = widget.LabelStyle ?? tabTheme.LabelStyle
                ?? (theme.UseMaterial3 ? theme.TextTheme.TitleSmall : theme.PrimaryTextTheme.BodyLarge);
            var unselectedStyle = widget.UnselectedLabelStyle ?? tabTheme.UnselectedLabelStyle ?? selectedStyle;
            var labelPadding = widget.LabelPadding ?? tabTheme.LabelPadding ?? new Thickness(16, 0);
            var indicatorColor = widget.IndicatorColor ?? tabTheme.IndicatorColor
                ?? (theme.UseMaterial3 ? theme.PrimaryColor : theme.SecondaryColor);
            var indicatorWeight = theme.UseMaterial3 && widget.IsPrimary && indicatorSize == TabBarIndicatorSize.Label
                ? Math.Max(3, widget.IndicatorWeight)
                : widget.IndicatorWeight;
            var dividerColor = widget.DividerColor ?? tabTheme.DividerColor
                ?? (theme.UseMaterial3 ? theme.OutlineVariantColor : Colors.Transparent);
            var dividerHeight = widget.DividerHeight ?? tabTheme.DividerHeight ?? (theme.UseMaterial3 ? 1 : 0);
            var indicatorAnimation = widget.IndicatorAnimation ?? tabTheme.IndicatorAnimation
                ?? (indicatorSize == TabBarIndicatorSize.Label ? TabIndicatorAnimation.Elastic : TabIndicatorAnimation.Linear);

            var children = new List<Widget>(widget.Tabs.Count);
            var localizations = MaterialLocalizations.Of(context);
            for (var index = 0; index < widget.Tabs.Count; index++)
            {
                var tabIndex = index;
                var fraction = Math.Clamp(1 - Math.Abs(controller.AnimationValue - index), 0, 1);
                var color = LerpColor(unselectedColor, selectedColor, fraction);
                var style = (fraction >= 0.5 ? selectedStyle : unselectedStyle).CopyWith(color: color);
                var states = index == controller.Index ? MaterialState.Selected : MaterialState.None;
                var overlay = widget.OverlayColor ?? tabTheme.OverlayColor;
                var hoverColor = overlay?.Resolve(states | MaterialState.Hovered)
                    ?? ApplyOpacity(index == controller.Index ? selectedColor : theme.OnSurfaceColor, 0.08);
                var focusColor = overlay?.Resolve(states | MaterialState.Focused)
                    ?? ApplyOpacity(index == controller.Index ? selectedColor : theme.OnSurfaceColor, 0.10);
                var splashColor = overlay?.Resolve(states | MaterialState.Pressed)
                    ?? ApplyOpacity(widget.IsPrimary ? theme.PrimaryColor : theme.OnSurfaceColor, 0.10);
                var cursor = widget.MouseCursor?.Resolve(states) ?? tabTheme.MouseCursor?.Resolve(states) ?? SystemMouseCursors.Click;

                Widget child = new Padding(
                    labelPadding,
                    new DefaultTextStyle(
                        style,
                        new Plumix.Widgets.IconTheme(
                            new IconThemeData(Color: color, Size: 24),
                            widget.Tabs[index])));
                child = new Semantics(
                    child: child,
                    label: localizations.TabLabel(index, widget.Tabs.Count),
                    selected: index == controller.Index,
                    role: SemanticsRole.Tab);
                child = new InkWell(
                    child: new Padding(new Thickness(0, 0, 0, indicatorWeight), child),
                    onTap: () => HandleTap(tabIndex),
                    onHover: value => widget.OnHover?.Invoke(value, tabIndex),
                    onFocusChange: value => widget.OnFocusChange?.Invoke(value, tabIndex),
                    hoverColor: hoverColor,
                    focusColor: focusColor,
                    splashColor: splashColor,
                    borderRadius: widget.SplashBorderRadius ?? tabTheme.SplashBorderRadius,
                    mouseCursor: cursor,
                    enableFeedback: widget.EnableFeedback ?? true);
                children.Add(child);
            }

            Widget result = new TabBarLayout(
                children,
                animationValue: controller.AnimationValue,
                currentIndex: controller.Index,
                previousIndex: controller.PreviousIndex,
                indexIsChanging: controller.IndexIsChanging,
                isScrollable: widget.IsScrollable,
                alignment: alignment,
                indicatorSize: indicatorSize,
                indicatorColor: indicatorColor,
                indicatorWeight: indicatorWeight,
                indicatorPadding: widget.IndicatorPadding,
                labelPadding: labelPadding,
                indicator: widget.Indicator ?? tabTheme.Indicator,
                dividerColor: dividerColor,
                dividerHeight: dividerHeight,
                indicatorAnimation: indicatorAnimation,
                textDirection: Directionality.Of(context),
                onLayout: HandleTabLayout);
            if (widget.IsScrollable)
            {
                var padding = widget.Padding ?? new Thickness();
                if (alignment == global::Plumix.Material.TabAlignment.StartOffset)
                {
                    padding = Directionality.Of(context) == TextDirection.Rtl
                        ? new Thickness(padding.Left, padding.Top, padding.Right + 52, padding.Bottom)
                        : new Thickness(padding.Left + 52, padding.Top, padding.Right, padding.Bottom);
                }
                if (_alignmentInset > 0)
                {
                    padding = new Thickness(
                        padding.Left + _alignmentInset,
                        padding.Top,
                        padding.Right + _alignmentInset,
                        padding.Bottom);
                }
                result = new SingleChildScrollView(
                    scrollDirection: Axis.Horizontal,
                    controller: widget.ScrollController ?? (_internalScrollController ??= new ScrollController()),
                    padding: padding,
                    physics: widget.Physics,
                    child: result);
            }
            else if (widget.Padding.HasValue)
            {
                result = new Padding(widget.Padding.Value, result);
            }

            return new Semantics(child: result, role: SemanticsRole.TabBar, container: true, explicitChildNodes: true);
        }

        private void HandleTap(int index)
        {
            _controller!.AnimateTo(index);
            CurrentWidget.OnTap?.Invoke(index);
        }

        private void UpdateController()
        {
            var next = CurrentWidget.Controller ?? DefaultTabController.MaybeOf(Context)
                ?? throw new InvalidOperationException("TabBar requires a TabController or DefaultTabController ancestor.");
            if (ReferenceEquals(next, _controller)) return;
            DetachController();
            _controller = next;
            _controller.AddListener(HandleControllerChanged);
        }

        private void DetachController()
        {
            _controller?.RemoveListener(HandleControllerChanged);
            _controller = null;
        }

        private void HandleControllerChanged()
        {
            if (!Mounted) return;
            SetState(() => { });
            ScheduleScrollUpdate();
        }

        private void HandleTabLayout(IReadOnlyList<Rect> rects, double stripWidth)
        {
            _tabRects = rects.ToArray();
            _tabStripWidth = stripWidth;
            ScheduleScrollUpdate();
        }

        private void ScheduleScrollUpdate()
        {
            if (_scrollUpdateScheduled || !CurrentWidget.IsScrollable) return;
            _scrollUpdateScheduled = true;
            Scheduler.AddPostFrameCallback(_ =>
            {
                _scrollUpdateScheduled = false;
                if (!Mounted || _controller is null || _tabRects.Count == 0) return;
                var scrollController = CurrentWidget.ScrollController ?? _internalScrollController;
                if (scrollController?.PrimaryPosition is not { } position) return;
                var alignment = CurrentWidget.TabAlignment ?? TabBarTheme.Of(Context).TabAlignment
                    ?? (Theme.Of(Context).UseMaterial3
                        ? global::Plumix.Material.TabAlignment.StartOffset
                        : global::Plumix.Material.TabAlignment.Start);
                var desiredInset = alignment == global::Plumix.Material.TabAlignment.Center &&
                                   _tabStripWidth < position.ViewportDimension
                    ? (position.ViewportDimension - _tabStripWidth) / 2
                    : 0;
                if (Math.Abs(desiredInset - _alignmentInset) > 0.01)
                {
                    SetState(() => _alignmentInset = desiredInset);
                    return;
                }

                var index = Math.Clamp(_controller.Index, 0, _tabRects.Count - 1);
                var contentPadding = CurrentWidget.Padding ?? new Thickness();
                var leading = Directionality.Of(Context) == TextDirection.Rtl
                    ? contentPadding.Right
                    : contentPadding.Left;
                if (alignment == global::Plumix.Material.TabAlignment.StartOffset) leading += 52;
                leading += _alignmentInset;
                var target = Math.Clamp(
                    leading + _tabRects[index].Center.X - (position.ViewportDimension / 2),
                    position.MinScrollExtent,
                    position.MaxScrollExtent);
                position.JumpTo(target);
            });
        }

        private static void ValidateAlignment(TabAlignment alignment, bool scrollable)
        {
            if (scrollable && alignment == global::Plumix.Material.TabAlignment.Fill)
                throw new ArgumentException("TabAlignment.Fill is only valid for non-scrollable tab bars.");
            if (!scrollable && alignment is global::Plumix.Material.TabAlignment.Start or global::Plumix.Material.TabAlignment.StartOffset)
                throw new ArgumentException("Start alignments are only valid for scrollable tab bars.");
        }
    }

    internal static Color ApplyOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Clamp((int)Math.Round(color.A * Math.Clamp(opacity, 0, 1)), 0, 255),
        color.R, color.G, color.B);

    internal static Color LerpColor(Color a, Color b, double t) => new ColorTween().Evaluate(t, a, b);
}

public sealed class TabBarView : StatefulWidget
{
    public TabBarView(
        IReadOnlyList<Widget> children,
        TabController? controller = null,
        ScrollPhysics? physics = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        double viewportFraction = 1,
        Clip clipBehavior = Clip.HardEdge,
        Key? key = null) : base(key)
    {
        if (!double.IsFinite(viewportFraction) || viewportFraction <= 0)
            throw new ArgumentOutOfRangeException(nameof(viewportFraction));
        Children = children ?? throw new ArgumentNullException(nameof(children));
        Controller = controller;
        Physics = physics;
        DragStartBehavior = dragStartBehavior;
        ViewportFraction = viewportFraction;
        ClipBehavior = clipBehavior;
    }

    public IReadOnlyList<Widget> Children { get; }
    public TabController? Controller { get; }
    public ScrollPhysics? Physics { get; }
    public DragStartBehavior DragStartBehavior { get; }
    public double ViewportFraction { get; }
    public Clip ClipBehavior { get; }
    public override State CreateState() => new TabBarViewState();

    private sealed class TabBarViewState : State
    {
        private TabController? _controller;
        private PageController? _pageController;
        private bool _controllerDrivingPage;
        private bool _pageDrivingController;
        private int? _warpingToIndex;
        private IReadOnlyList<Widget> _children = Array.Empty<Widget>();
        private bool _nonAdjacentWarpUnderway;
        private TabBarView CurrentWidget => (TabBarView)StateWidget;

        public override void InitState() => UpdateChildren();

        public override void DidChangeDependencies() => UpdateController();

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var old = (TabBarView)oldWidget;
            if (!ReferenceEquals(old.Controller, CurrentWidget.Controller)) UpdateController();
            if (old.ViewportFraction != CurrentWidget.ViewportFraction)
            {
                _pageController?.RemoveListener(HandlePageChanged);
                if (_pageController is not null) _pageController.AnimationCompleted -= HandleNonAdjacentWarpCompleted;
                _pageController?.Dispose();
                _pageController = CreatePageController();
                _pageController.AddListener(HandlePageChanged);
            }
            if (!ReferenceEquals(old.Children, CurrentWidget.Children) && !_nonAdjacentWarpUnderway)
            {
                UpdateChildren();
            }
        }

        public override void Dispose()
        {
            _controller?.RemoveListener(HandleTabControllerChanged);
            _pageController?.RemoveListener(HandlePageChanged);
            if (_pageController is not null) _pageController.AnimationCompleted -= HandleNonAdjacentWarpCompleted;
            _pageController?.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            UpdateController();
            if (_controller!.Length != CurrentWidget.Children.Count)
            {
                throw new InvalidOperationException($"TabController length ({_controller.Length}) must match children count ({CurrentWidget.Children.Count}).");
            }

            return new PageView(
                children: _children,
                controller: _pageController,
                physics: new PageScrollPhysics(CurrentWidget.Physics ?? new ClampingScrollPhysics()),
                dragStartBehavior: CurrentWidget.DragStartBehavior,
                clipBehavior: CurrentWidget.ClipBehavior);
        }

        private void UpdateController()
        {
            var next = CurrentWidget.Controller ?? DefaultTabController.MaybeOf(Context)
                ?? throw new InvalidOperationException("TabBarView requires a TabController or DefaultTabController ancestor.");
            if (ReferenceEquals(next, _controller)) return;
            _controller?.RemoveListener(HandleTabControllerChanged);
            _pageController?.RemoveListener(HandlePageChanged);
            if (_pageController is not null) _pageController.AnimationCompleted -= HandleNonAdjacentWarpCompleted;
            _pageController?.Dispose();
            _controller = next;
            _controller.AddListener(HandleTabControllerChanged);
            _pageController = CreatePageController();
            _pageController.AddListener(HandlePageChanged);
        }

        private PageController CreatePageController() => new(
            initialPage: _controller?.Index ?? 0,
            viewportFraction: CurrentWidget.ViewportFraction);

        private void HandleTabControllerChanged()
        {
            if (_pageDrivingController || _controller is null || _pageController is null) return;
            if (!_controller.IndexIsChanging)
            {
                _warpingToIndex = null;
                return;
            }
            if (_controller.Index == (int)Math.Round(_pageController.EffectivePage) ||
                _warpingToIndex == _controller.Index) return;
            _controllerDrivingPage = true;
            _warpingToIndex = _controller.Index;
            if (Math.Abs(_controller.Index - _controller.PreviousIndex) > 1)
            {
                var initialPage = _controller.Index > _controller.PreviousIndex
                    ? _controller.Index - 1
                    : _controller.Index + 1;
                var warped = _children.ToArray();
                (warped[initialPage], warped[_controller.PreviousIndex]) =
                    (warped[_controller.PreviousIndex], warped[initialPage]);
                _children = warped;
                _nonAdjacentWarpUnderway = true;
                _pageController.JumpToPage(initialPage);
                _pageController.AnimationCompleted -= HandleNonAdjacentWarpCompleted;
                _pageController.AnimationCompleted += HandleNonAdjacentWarpCompleted;
                SetState(() => { });
            }
            _pageController.AnimateToPage(_controller.Index, _controller.AnimationDuration, Curves.Ease);
            _controllerDrivingPage = false;
        }

        private void HandleNonAdjacentWarpCompleted()
        {
            if (_pageController is not null) _pageController.AnimationCompleted -= HandleNonAdjacentWarpCompleted;
            if (!_nonAdjacentWarpUnderway) return;
            _nonAdjacentWarpUnderway = false;
            UpdateChildren();
            if (Mounted) SetState(() => { });
        }

        private void HandlePageChanged()
        {
            if (_controllerDrivingPage || _controller is null || _pageController?.Page is not { } page || _controller.IndexIsChanging) return;
            _pageDrivingController = true;
            var delta = Math.Clamp(page - _controller.Index, -1, 1);
            var rounded = Math.Clamp((int)Math.Round(page), 0, Math.Max(0, _controller.Length - 1));
            if (Math.Abs(page - rounded) <= 0.0001 && Math.Abs(delta) >= 0.9999)
            {
                _controller.Index = rounded;
            }
            else
            {
                _controller.Offset = delta;
            }
            _pageDrivingController = false;
        }

        private void UpdateChildren()
        {
            _children = CurrentWidget.Children
                .Select(child => (Widget)new Semantics(child: child, role: SemanticsRole.TabPanel))
                .ToArray();
        }
    }
}

internal sealed class TabBarLayout : MultiChildRenderObjectWidget
{
    public TabBarLayout(
        IReadOnlyList<Widget> children,
        double animationValue,
        int currentIndex,
        int previousIndex,
        bool indexIsChanging,
        bool isScrollable,
        TabAlignment alignment,
        TabBarIndicatorSize indicatorSize,
        Color indicatorColor,
        double indicatorWeight,
        Thickness indicatorPadding,
        Thickness labelPadding,
        BoxDecoration? indicator,
        Color dividerColor,
        double dividerHeight,
        TabIndicatorAnimation indicatorAnimation,
        TextDirection textDirection,
        Action<IReadOnlyList<Rect>, double>? onLayout) : base(children)
    {
        AnimationValue = animationValue;
        CurrentIndex = currentIndex;
        PreviousIndex = previousIndex;
        IndexIsChanging = indexIsChanging;
        IsScrollable = isScrollable;
        Alignment = alignment;
        IndicatorSize = indicatorSize;
        IndicatorColor = indicatorColor;
        IndicatorWeight = indicatorWeight;
        IndicatorPadding = indicatorPadding;
        LabelPadding = labelPadding;
        Indicator = indicator;
        DividerColor = dividerColor;
        DividerHeight = dividerHeight;
        IndicatorAnimation = indicatorAnimation;
        TextDirection = textDirection;
        OnLayout = onLayout;
    }

    public double AnimationValue { get; }
    public int CurrentIndex { get; }
    public int PreviousIndex { get; }
    public bool IndexIsChanging { get; }
    public bool IsScrollable { get; }
    public TabAlignment Alignment { get; }
    public TabBarIndicatorSize IndicatorSize { get; }
    public Color IndicatorColor { get; }
    public double IndicatorWeight { get; }
    public Thickness IndicatorPadding { get; }
    public Thickness LabelPadding { get; }
    public BoxDecoration? Indicator { get; }
    public Color DividerColor { get; }
    public double DividerHeight { get; }
    public TabIndicatorAnimation IndicatorAnimation { get; }
    public TextDirection TextDirection { get; }
    public Action<IReadOnlyList<Rect>, double>? OnLayout { get; }

    internal override RenderObject CreateRenderObject(BuildContext context) => new RenderTabBar(
        AnimationValue, CurrentIndex, PreviousIndex, IndexIsChanging, IsScrollable, Alignment,
        IndicatorSize, IndicatorColor, IndicatorWeight, IndicatorPadding, LabelPadding, Indicator, DividerColor,
        DividerHeight, IndicatorAnimation, TextDirection, OnLayout);

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject) =>
        ((RenderTabBar)renderObject).Update(
            AnimationValue, CurrentIndex, PreviousIndex, IndexIsChanging, IsScrollable, Alignment,
            IndicatorSize, IndicatorColor, IndicatorWeight, IndicatorPadding, LabelPadding, Indicator, DividerColor,
            DividerHeight, IndicatorAnimation, TextDirection, OnLayout);
}
