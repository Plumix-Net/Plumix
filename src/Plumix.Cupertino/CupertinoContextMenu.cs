using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using BoxShadow = Plumix.Rendering.BoxShadow;
using WidgetTransform = Plumix.Widgets.Transform;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/context_menu.dart

public delegate Widget CupertinoContextMenuBuilder(
    BuildContext context,
    Animation<double> animation);

/// <summary>An iOS context menu opened by pressing and holding its preview.</summary>
public sealed class CupertinoContextMenu : StatefulWidget
{
    public const double OpenBorderRadius = 12.0;
    public const double AnimationOpensAt = 800.0 / 1135.0;

    public static CupertinoDynamicColor BackgroundColor { get; } =
        CupertinoDynamicColor.WithBrightness(
            Color.FromUInt32(0xFFF1F1F1),
            Color.FromUInt32(0xFF212122));

    public static IReadOnlyList<BoxShadow> EndBoxShadow { get; } =
    [
        new BoxShadow(
            color: Color.FromUInt32(0x40000000),
            blurRadius: 10.0,
            spreadRadius: 0.5),
    ];

    public CupertinoContextMenu(
        IReadOnlyList<Widget> actions,
        Widget child,
        bool enableHapticFeedback = false,
        Key? key = null) : this(
            actions,
            (context, animation) => child,
            child,
            enableHapticFeedback,
            key)
    {
    }

    private CupertinoContextMenu(
        IReadOnlyList<Widget> actions,
        CupertinoContextMenuBuilder builder,
        Widget? child,
        bool enableHapticFeedback,
        Key? key) : base(key)
    {
        ArgumentNullException.ThrowIfNull(actions);
        if (actions.Count == 0)
        {
            throw new ArgumentException("CupertinoContextMenu actions must not be empty.", nameof(actions));
        }

        Actions = actions;
        Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        Child = child;
        EnableHapticFeedback = enableHapticFeedback;
    }

    public CupertinoContextMenuBuilder Builder { get; }

    public Widget? Child { get; }

    public IReadOnlyList<Widget> Actions { get; }

    public bool EnableHapticFeedback { get; }

    public static CupertinoContextMenu WithBuilder(
        IReadOnlyList<Widget> actions,
        CupertinoContextMenuBuilder builder,
        bool enableHapticFeedback = false,
        Key? key = null)
    {
        return new CupertinoContextMenu(actions, builder, null, enableHapticFeedback, key);
    }

    public override State CreateState() => new CupertinoContextMenuState();

    private sealed class CupertinoContextMenuState : State
    {
        private readonly GlobalKey _childGlobalKey = new GlobalObjectKey<State>(new object());
        private AnimationController _openController = null!;
        private TapGestureRecognizer _tapGestureRecognizer = null!;
        private OverlayEntry? _lastOverlayEntry;
        private ContextMenuRoute? _route;
        private bool _childHidden;
        private bool _midpointHandled;
        private Rect _childRect;
        private Rect _decoyChildEndRect;
        private double _scaleFactor = 1.02;
        private ContextMenuLocation _contextMenuLocation;

        private CupertinoContextMenu Current => (CupertinoContextMenu)StateWidget;

        public override void InitState()
        {
            _openController = new AnimationController(
                duration: TimeSpan.FromMilliseconds(800),
                upperBound: AnimationOpensAt,
                vsync: this);
            _openController.Changed += HandleOpenControllerChanged;
            _openController.Completed += HandleOpenControllerCompleted;
            _openController.Dismissed += HandleOpenControllerDismissed;
            _tapGestureRecognizer = new TapGestureRecognizer
            {
                OnTap = HandleTap,
                OnTapUp = _ => HandleTap(),
                OnTapCancel = HandleTap,
            };
        }

        public override Widget Build(BuildContext context)
        {
            Widget preview = Current.Builder(context, _openController);
            preview = Visibility.Maintain(
                key: _childGlobalKey,
                visible: !_childHidden,
                child: preview);
            preview = new TickerMode(enabled: !_childHidden, child: preview);
            return new MouseRegion(
                cursor: PlatformDefaults.IsWeb ? SystemMouseCursors.Click : MouseCursor.Defer,
                child: new Listener(
                    onPointerDown: HandlePointerDown,
                    child: preview));
        }

        public override void Dispose()
        {
            RemoveOverlayEntry();
            _tapGestureRecognizer.Dispose();
            _openController.Changed -= HandleOpenControllerChanged;
            _openController.Completed -= HandleOpenControllerCompleted;
            _openController.Dismissed -= HandleOpenControllerDismissed;
            _openController.Dispose();
        }

        private void HandlePointerDown(PointerDownEvent @event)
        {
            if (_route is not null || _lastOverlayEntry is not null)
            {
                return;
            }

            if (_childGlobalKey.CurrentContext?.FindRenderObject() is not RenderBox { HasSize: true } renderBox)
            {
                return;
            }

            _tapGestureRecognizer.AddPointer(@event);
            _childRect = RenderObject.TransformRect(renderBox.GetTransformTo(null), renderBox.PaintBounds);
            _contextMenuLocation = GetContextMenuLocation(_childRect, MediaQuery.WidthOf(Context));
            _scaleFactor = GetScaleFactor(Context, _childRect);
            _decoyChildEndRect = ScaleRect(_childRect, _scaleFactor);
            _midpointHandled = false;
            SetState(() => _childHidden = true);

            _lastOverlayEntry = new OverlayEntry(_ => new DecoyChild(
                beginRect: _childRect,
                endRect: _decoyChildEndRect,
                controller: _openController,
                builder: Current.Builder,
                child: Current.Child));
            Overlay.Of(Context, rootOverlay: true).Insert(_lastOverlayEntry);
            _openController.Forward(0.0);
        }

        private void HandleOpenControllerChanged()
        {
            double midpoint = AnimationOpensAt / 2.0;
            if (_midpointHandled
                || _openController.Status == AnimationStatus.Reverse
                || _openController.Value < midpoint)
            {
                return;
            }

            _midpointHandled = true;
            if (Current.EnableHapticFeedback)
            {
                HapticFeedback.HeavyImpact();
            }

            _tapGestureRecognizer.Resolve(GestureDisposition.Accepted);
        }

        private void HandleTap()
        {
            if (_openController.Status.IsAnimating()
                && _openController.Value < AnimationOpensAt / 2.0)
            {
                _openController.Reverse();
            }
        }

        private void HandleOpenControllerDismissed()
        {
            if (_route is null)
            {
                SetState(() => _childHidden = false);
            }

            RemoveOverlayEntry();
        }

        private void HandleOpenControllerCompleted()
        {
            if (!Mounted || _route is not null)
            {
                return;
            }

            _route = new ContextMenuRoute(
                actions: Current.Actions,
                barrierLabel: CupertinoLocalizations.Of(Context).MenuDismissLabel,
                contextMenuLocation: _contextMenuLocation,
                previousChildRect: _decoyChildEndRect,
                originalChildRect: _childRect,
                scaleFactor: _scaleFactor,
                builder: BuildRoutePreview,
                onDismissed: HandleRouteDismissed);
            Navigator.Of(Context, rootNavigator: true).Push(_route);
            Scheduler.AddPostFrameCallback(_ =>
            {
                if (!Mounted)
                {
                    return;
                }

                RemoveOverlayEntry();
                _openController.Reset();
            });
        }

        private Widget BuildRoutePreview(BuildContext context, Animation<double> animation)
        {
            if (Current.Child is null)
            {
                var adjusted = new DoubleTween(AnimationOpensAt, 1.0).Animate(animation);
                return Current.Builder(context, adjusted);
            }

            return new FittedBox(
                fit: BoxFit.Cover,
                child: new ClipRSuperellipse(
                    borderRadius: BorderRadius.Circular(OpenBorderRadius * animation.Value),
                    child: Current.Child));
        }

        private void HandleRouteDismissed()
        {
            if (!Mounted)
            {
                return;
            }

            SetState(() =>
            {
                _route = null;
                _childHidden = false;
            });
        }

        private void RemoveOverlayEntry()
        {
            if (_lastOverlayEntry is not { } entry)
            {
                return;
            }

            if (entry.IsInserted)
            {
                entry.Remove();
            }

            entry.Dispose();
            _lastOverlayEntry = null;
        }
    }

    private sealed class DecoyChild : StatelessWidget
    {
        public DecoyChild(
            Rect beginRect,
            Rect endRect,
            AnimationController controller,
            CupertinoContextMenuBuilder builder,
            Widget? child)
        {
            BeginRect = beginRect;
            EndRect = endRect;
            Controller = controller;
            Builder = builder;
            Child = child;
        }

        private Rect BeginRect { get; }

        private Rect EndRect { get; }

        private AnimationController Controller { get; }

        private CupertinoContextMenuBuilder Builder { get; }

        private Widget? Child { get; }

        public override Widget Build(BuildContext context)
        {
            return new Stack(
                clipBehavior: Clip.None,
                children:
                [
                    new AnimatedBuilder(
                        animation: Controller,
                        builder: (builderContext, _) => BuildAnimated(builderContext)),
                ]);
        }

        private Widget BuildAnimated(BuildContext context)
        {
            double normalized = Controller.Value / AnimationOpensAt;
            double rectProgress = normalized <= 1.0 / 6.0
                ? 0.0
                : Curves.EaseOutSine((normalized - (1.0 / 6.0)) / (5.0 / 6.0));
            Rect rect = new RectTween(BeginRect, EndRect).Transform(rectProgress);
            Widget preview;
            if (Child is not null)
            {
                preview = new Container(
                    decoration: new BoxDecoration(
                        BoxShadows: BoxShadow.LerpList([], EndBoxShadow, normalized)),
                    child: Child);
            }
            else
            {
                preview = Builder(context, Controller);
            }

            return new Positioned(
                left: rect.X,
                top: rect.Y,
                width: rect.Width,
                height: rect.Height,
                child: preview);
        }
    }

    private sealed class ContextMenuRoute : PopupRoute
    {
        private readonly IReadOnlyList<Widget> _actions;
        private readonly string _barrierLabel;
        private readonly ContextMenuLocation _contextMenuLocation;
        private readonly Rect _previousChildRect;
        private readonly Rect _originalChildRect;
        private readonly double _scaleFactor;
        private readonly Func<BuildContext, Animation<double>, Widget> _builder;
        private readonly Action _onDismissed;

        public ContextMenuRoute(
            IReadOnlyList<Widget> actions,
            string barrierLabel,
            ContextMenuLocation contextMenuLocation,
            Rect previousChildRect,
            Rect originalChildRect,
            double scaleFactor,
            Func<BuildContext, Animation<double>, Widget> builder,
            Action onDismissed) : base(filter: new ImageFilter.Blur(5.0, 5.0))
        {
            _actions = actions;
            _barrierLabel = barrierLabel;
            _contextMenuLocation = contextMenuLocation;
            _previousChildRect = previousChildRect;
            _originalChildRect = originalChildRect;
            _scaleFactor = scaleFactor;
            _builder = builder;
            _onDismissed = onDismissed;
        }

        public override bool BarrierDismissible => true;

        public override bool SemanticsDismissible => false;

        public override Color? BarrierColor => Color.FromUInt32(0x6604040F);

        public override string BarrierLabel => _barrierLabel;

        public override TimeSpan TransitionDuration => TimeSpan.FromMilliseconds(335);

        public override Widget BuildPage(BuildContext context) => new SizedBox(width: 0.0, height: 0.0);

        public override Widget BuildTransitions(
            BuildContext context,
            Animation<double> animation,
            Animation<double> secondaryAnimation,
            Widget child)
        {
            _ = secondaryAnimation;
            _ = child;
            return new ContextMenuRouteBody(
                route: this,
                actions: _actions,
                animation: animation,
                contextMenuLocation: _contextMenuLocation,
                previousChildRect: _previousChildRect,
                originalChildRect: _originalChildRect,
                scaleFactor: _scaleFactor,
                builder: _builder);
        }

        public override void Dispose()
        {
            _onDismissed();
            base.Dispose();
        }
    }

    private sealed class ContextMenuRouteBody : StatefulWidget
    {
        public ContextMenuRouteBody(
            ContextMenuRoute route,
            IReadOnlyList<Widget> actions,
            Animation<double> animation,
            ContextMenuLocation contextMenuLocation,
            Rect previousChildRect,
            Rect originalChildRect,
            double scaleFactor,
            Func<BuildContext, Animation<double>, Widget> builder)
        {
            Route = route;
            Actions = actions;
            Animation = animation;
            ContextMenuLocation = contextMenuLocation;
            PreviousChildRect = previousChildRect;
            OriginalChildRect = originalChildRect;
            ScaleFactor = scaleFactor;
            Builder = builder;
        }

        public ContextMenuRoute Route { get; }

        public IReadOnlyList<Widget> Actions { get; }

        public Animation<double> Animation { get; }

        public ContextMenuLocation ContextMenuLocation { get; }

        public Rect PreviousChildRect { get; }

        public Rect OriginalChildRect { get; }

        public double ScaleFactor { get; }

        public Func<BuildContext, Animation<double>, Widget> Builder { get; }

        public override State CreateState() => new ContextMenuRouteBodyState();
    }

    private sealed class ContextMenuRouteBodyState : State
    {
        private const double MinScale = 0.8;
        private const double SheetScaleThreshold = 0.9;
        private AnimationController _sheetController = null!;
        private AnimationController _moveController = null!;
        private Point _dragOffset;
        private Point _returnBegin;
        private double _lastScale = 1.0;

        private ContextMenuRouteBody Current => (ContextMenuRouteBody)StateWidget;

        public override void InitState()
        {
            _sheetController = new AnimationController(
                duration: TimeSpan.FromMilliseconds(100),
                reverseDuration: TimeSpan.FromMilliseconds(300),
                vsync: this);
            _moveController = new AnimationController(
                value: 1.0,
                duration: TimeSpan.FromMilliseconds(600),
                vsync: this);
            _sheetController.Changed += HandleAnimationChanged;
            _moveController.Changed += HandleAnimationChanged;
        }

        public override Widget Build(BuildContext context)
        {
            return new SafeArea(
                child: new Align(
                    alignment: Alignment.TopLeft,
                    child: new GestureDetector(
                        onPanStart: HandlePanStart,
                        onPanUpdate: HandlePanUpdate,
                        onPanEnd: HandlePanEnd,
                        child: new AnimatedBuilder(
                            animation: Current.Animation,
                            builder: (builderContext, _) => BuildLayout(builderContext)))));
        }

        public override void Dispose()
        {
            _sheetController.Changed -= HandleAnimationChanged;
            _moveController.Changed -= HandleAnimationChanged;
            _sheetController.Dispose();
            _moveController.Dispose();
        }

        private Widget BuildLayout(BuildContext context)
        {
            Orientation orientation = MediaQuery.WidthOf(context) > MediaQuery.HeightOf(context)
                ? Orientation.Landscape
                : Orientation.Portrait;
            Point translatedOffset = TranslateDragOffset(_dragOffset);
            Point effectiveOffset = _moveController.Status.IsAnimating()
                ? LerpPoint(_returnBegin, default, Curves.ElasticIn(_moveController.Value))
                : translatedOffset;
            double routeValue = Math.Clamp(Current.Animation.Value, 0.0, 1.0);
            double previewScale = Lerp(Current.ScaleFactor, _lastScale, Curves.EaseOutBack(routeValue));
            double sheetVisibility = 1.0 - _sheetController.Value;
            double sheetScale = routeValue * sheetVisibility;
            double sheetOpacity = routeValue * sheetVisibility;
            AlignmentDirectional sheetAlignment = GetSheetAlignment(Current.ContextMenuLocation, orientation);

            Widget preview = WidgetTransform.Scale(
                scale: previewScale,
                child: Current.Builder(context, Current.Animation),
                alignment: (AlignmentGeometry)Alignment.Center);
            Widget sheet = WidgetTransform.Scale(
                scale: sheetScale,
                child: new FadeTransition(
                    opacity: new ConstantAnimation<double>(sheetOpacity),
                    child: new ContextMenuSheet(Current.Actions)),
                alignment: sheetAlignment);

            return WidgetTransform.Translate(
                offset: effectiveOffset,
                child: new CustomMultiChildLayout(
                    @delegate: new ContextMenuLayoutDelegate(
                        targetRect: Current.OriginalChildRect,
                        previousChildRect: Current.PreviousChildRect,
                        contextMenuLocation: Current.ContextMenuLocation,
                        orientation: orientation,
                        animationValue: routeValue),
                    children:
                    [
                        new LayoutId(ContextMenuChild.Child, preview),
                        new LayoutId(ContextMenuChild.MenuSheet, sheet),
                    ]));
        }

        private void HandlePanStart(DragStartDetails details)
        {
            _ = details;
            _moveController.SetValue(1.0);
            SetState(() =>
            {
                _dragOffset = default;
                _returnBegin = default;
            });
        }

        private void HandlePanUpdate(DragUpdateDetails details)
        {
            Point delta = details.Delta;
            SetState(() =>
            {
                _dragOffset += delta;
                _lastScale = Math.Max(
                    MinScale,
                    (MediaQuery.HeightOf(Context) - Math.Abs(_dragOffset.Y)) / MediaQuery.HeightOf(Context));
            });

            if (_lastScale <= SheetScaleThreshold)
            {
                _sheetController.Forward();
            }
            else
            {
                _sheetController.Reverse();
            }
        }

        private void HandlePanEnd(DragEndDetails details)
        {
            double verticalVelocity = details.Velocity.PixelsPerSecond.Y;
            if (Math.Abs(verticalVelocity) >= GestureConstants.MinFlingVelocity)
            {
                if (verticalVelocity > 0.0)
                {
                    _sheetController.Forward();
                    Current.Route.Navigator?.Pop();
                    return;
                }

                ReturnHome();
                return;
            }

            if (_lastScale <= MinScale)
            {
                Current.Route.Navigator?.Pop();
                return;
            }

            ReturnHome();
        }

        private void ReturnHome()
        {
            _returnBegin = TranslateDragOffset(_dragOffset);
            _dragOffset = default;
            _lastScale = 1.0;
            _sheetController.Reverse();
            _moveController.Forward(0.0);
        }

        private void HandleAnimationChanged()
        {
            if (Mounted)
            {
                SetState(static () => { });
            }
        }

        private static Point TranslateDragOffset(Point dragOffset)
        {
            double endX = 20.0 * dragOffset.X / 400.0;
            double endY = dragOffset.Y >= 0.0
                ? dragOffset.Y
                : 20.0 * dragOffset.Y / 400.0;
            return new Point(Math.Clamp(endX, -20.0, 20.0), endY);
        }
    }

    private sealed class ContextMenuSheet : StatefulWidget
    {
        public ContextMenuSheet(IReadOnlyList<Widget> actions)
        {
            Actions = actions;
        }

        public IReadOnlyList<Widget> Actions { get; }

        public override State CreateState() => new ContextMenuSheetState();
    }

    private sealed class ContextMenuSheetState : State
    {
        private static readonly CupertinoDynamicColor BorderColor =
            CupertinoDynamicColor.WithBrightness(
                Color.FromUInt32(0xFFA9A9AF),
                Color.FromUInt32(0xFF57585A));

        private ScrollController _scrollController = null!;

        private ContextMenuSheet Current => (ContextMenuSheet)StateWidget;

        public override void InitState()
        {
            _scrollController = new ScrollController();
        }

        public override Widget Build(BuildContext context)
        {
            Color borderColor = CupertinoDynamicColor.Resolve(BorderColor, context);
            var children = new List<Widget>(Current.Actions.Count) { Current.Actions[0] };
            for (int index = 1; index < Current.Actions.Count; index++)
            {
                children.Add(new DecoratedBox(
                    position: DecorationPosition.Foreground,
                    decoration: new BoxDecoration(
                        Border: new Border(
                            top: new BorderSide(borderColor, width: 0.4))),
                    child: Current.Actions[index]));
            }

            Widget scrollView = new SingleChildScrollView(
                controller: _scrollController,
                child: new Column(
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    children: children));
            scrollView = new CupertinoScrollbar(
                controller: _scrollController,
                mainAxisMargin: 13.0,
                child: scrollView);
            scrollView = new ScrollConfiguration(
                ScrollConfiguration.Of(context).CopyWith(scrollbars: false),
                scrollView);
            return new SizedBox(
                width: 250.0,
                child: new IntrinsicHeight(
                    child: new ClipRSuperellipse(
                        borderRadius: BorderRadius.Circular(13.0),
                        child: new ColoredBox(
                            CupertinoDynamicColor.Resolve(BackgroundColor, context),
                            scrollView))));
        }

        public override void Dispose()
        {
            _scrollController.Dispose();
        }
    }

    private sealed class ContextMenuLayoutDelegate : MultiChildLayoutDelegate
    {
        private const double Padding = 20.0;

        public ContextMenuLayoutDelegate(
            Rect targetRect,
            Rect previousChildRect,
            ContextMenuLocation contextMenuLocation,
            Orientation orientation,
            double animationValue)
        {
            TargetRect = targetRect;
            PreviousChildRect = previousChildRect;
            ContextMenuLocation = contextMenuLocation;
            Orientation = orientation;
            AnimationValue = animationValue;
        }

        public Rect TargetRect { get; }

        public Rect PreviousChildRect { get; }

        public ContextMenuLocation ContextMenuLocation { get; }

        public Orientation Orientation { get; }

        public double AnimationValue { get; }

        public override void PerformLayout(Size size)
        {
            double availableHeightForChild = Math.Max(0.0, size.Height - Padding);
            double availableWidth = Math.Max(0.0, size.Width - (Padding * 2.0));
            double availableWidthForChild = Orientation == Orientation.Portrait
                ? availableWidth
                : Math.Max(0.0, availableWidth - 250.0);
            Size childSize = LayoutChild(
                ContextMenuChild.Child,
                new BoxConstraints(
                    MaxWidth: availableWidthForChild,
                    MaxHeight: availableHeightForChild));
            double availableHeightForMenu = Orientation == Orientation.Portrait
                ? Math.Max(0.0, availableHeightForChild - childSize.Height - Padding)
                : availableHeightForChild;
            Size menuSize = LayoutChild(
                ContextMenuChild.MenuSheet,
                new BoxConstraints(MaxHeight: availableHeightForMenu));

            (Point finalChild, Point finalMenu) = GetFinalPositions(size, childSize, menuSize);
            Point beginChild = new(PreviousChildRect.X, PreviousChildRect.Y);
            Point beginMenu = GetInitialMenuPosition(menuSize);
            double progress = Curves.EaseOutBack(AnimationValue);
            PositionChild(ContextMenuChild.Child, LerpPoint(beginChild, finalChild, progress));
            PositionChild(ContextMenuChild.MenuSheet, LerpPoint(beginMenu, finalMenu, progress));
        }

        public override bool ShouldRelayout(MultiChildLayoutDelegate oldDelegate)
        {
            return oldDelegate is not ContextMenuLayoutDelegate old
                   || old.TargetRect != TargetRect
                   || old.PreviousChildRect != PreviousChildRect
                   || old.ContextMenuLocation != ContextMenuLocation
                   || old.Orientation != Orientation
                   || old.AnimationValue != AnimationValue;
        }

        private (Point Child, Point Menu) GetFinalPositions(Size size, Size childSize, Size menuSize)
        {
            bool menuBeforeChild = Orientation == Orientation.Landscape
                                   && ContextMenuLocation == ContextMenuLocation.Right;
            double totalWidth;
            double totalHeight;
            Point secondOffset;
            double initialLeft;
            double initialTop;
            if (Orientation == Orientation.Portrait)
            {
                totalWidth = childSize.Width + Padding;
                totalHeight = childSize.Height + menuSize.Height + Padding;
                initialLeft = TargetRect.Center.X - (childSize.Width / 2.0);
                initialTop = TargetRect.Center.Y - childSize.Height;
                double menuX = ContextMenuLocation switch
                {
                    ContextMenuLocation.Center => (childSize.Width - menuSize.Width) / 2.0,
                    ContextMenuLocation.Left => 0.0,
                    ContextMenuLocation.Right => childSize.Width - menuSize.Width,
                    _ => 0.0,
                };
                secondOffset = new Point(menuX, childSize.Height + Padding);
            }
            else
            {
                totalWidth = childSize.Width + menuSize.Width + Padding;
                totalHeight = Math.Max(childSize.Height, menuSize.Height);
                initialLeft = (size.Width - totalWidth) / 2.0;
                initialTop = (size.Height - totalHeight) / 2.0;
                double secondX = (menuBeforeChild ? menuSize.Width : childSize.Width) + Padding;
                secondOffset = new Point(secondX, 0.0);
            }

            double maxLeft = Math.Max(Padding, size.Width - totalWidth);
            double maxTop = Math.Max(Padding, size.Height - totalHeight);
            Point first = new(
                Math.Clamp(initialLeft, Padding, maxLeft),
                Math.Clamp(initialTop, Padding, maxTop));
            Point second = first + secondOffset;
            return menuBeforeChild ? (second, first) : (first, second);
        }

        private Point GetInitialMenuPosition(Size menuSize)
        {
            return (Orientation, ContextMenuLocation) switch
            {
                (Orientation.Portrait, ContextMenuLocation.Center) => new Point(
                    TargetRect.Center.X - (menuSize.Width / 2.0),
                    TargetRect.Bottom),
                (Orientation.Portrait, ContextMenuLocation.Right) => new Point(
                    TargetRect.Right - menuSize.Width,
                    TargetRect.Bottom),
                (Orientation.Portrait, ContextMenuLocation.Left) => new Point(
                    TargetRect.Left,
                    TargetRect.Bottom),
                (Orientation.Landscape, ContextMenuLocation.Center) => new Point(
                    TargetRect.Center.X - (menuSize.Width / 2.0),
                    TargetRect.Top),
                (Orientation.Landscape, ContextMenuLocation.Right) => new Point(
                    TargetRect.Right - menuSize.Width,
                    TargetRect.Top),
                _ => new Point(TargetRect.Left, TargetRect.Top),
            };
        }
    }

    private enum ContextMenuLocation
    {
        Center,
        Right,
        Left,
    }

    private enum ContextMenuChild
    {
        Child,
        MenuSheet,
    }

    private static ContextMenuLocation GetContextMenuLocation(Rect childRect, double screenWidth)
    {
        double center = screenWidth / 2.0;
        bool centerDividesChild = childRect.Left < center && childRect.Right > center;
        double distanceFromCenter = Math.Abs(center - childRect.Center.X);
        if (centerDividesChild && distanceFromCenter <= childRect.Width / 4.0)
        {
            return ContextMenuLocation.Center;
        }

        return childRect.Center.X > center ? ContextMenuLocation.Right : ContextMenuLocation.Left;
    }

    private static double GetScaleFactor(BuildContext context, Rect childRect)
    {
        Size size = MediaQuery.SizeOf(context);
        Thickness padding = MediaQuery.PaddingOf(context);
        if (childRect.Width <= 0.0 || childRect.Height <= 0.0)
        {
            return 1.02;
        }

        double left = 2.0 * (childRect.Center.X - padding.Left) / childRect.Width;
        double top = 2.0 * (childRect.Center.Y - padding.Top) / childRect.Height;
        double right = 2.0 * (size.Width - padding.Right - childRect.Center.X) / childRect.Width;
        double bottom = 2.0 * (size.Height - padding.Bottom - childRect.Center.Y) / childRect.Height;
        return Math.Clamp(Math.Min(Math.Min(left, right), Math.Min(top, bottom)), 1.02, 1.15);
    }

    private static Rect ScaleRect(Rect rect, double scale)
    {
        double width = rect.Width * scale;
        double height = rect.Height * scale;
        return new Rect(
            rect.Center.X - (width / 2.0),
            rect.Center.Y - (height / 2.0),
            width,
            height);
    }

    private static AlignmentDirectional GetSheetAlignment(
        ContextMenuLocation location,
        Orientation orientation)
    {
        return location switch
        {
            ContextMenuLocation.Center when orientation == Orientation.Portrait => AlignmentDirectional.TopCenter,
            ContextMenuLocation.Right => AlignmentDirectional.TopEnd,
            _ => AlignmentDirectional.TopStart,
        };
    }

    private static double Lerp(double begin, double end, double t) => begin + ((end - begin) * t);

    private static Point LerpPoint(Point begin, Point end, double t)
    {
        return new Point(Lerp(begin.X, end.X, t), Lerp(begin.Y, end.Y, t));
    }
}
