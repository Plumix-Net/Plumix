using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/scaffold.dart

/// <summary>Ports Flutter's private <c>_ScaffoldSlot</c>.</summary>
internal enum ScaffoldSlot
{
    Body,
    AppBar,
    BodyScrim,
    BottomSheet,
    SnackBar,
    MaterialBanner,
    BottomNavigationBar,
    FloatingActionButton,
}

/// <summary>
/// Ports Flutter's private <c>_ScaffoldLayout</c>: measures every scaffold feature, positions the floating
/// action button through the active <see cref="FloatingActionButtonAnimator"/>, and publishes the resulting
/// <see cref="ScaffoldGeometry"/>.
/// </summary>
internal sealed class ScaffoldLayout : MultiChildLayoutDelegate
{
    public ScaffoldLayout(
        Thickness minInsets,
        Thickness minViewPadding,
        ScaffoldGeometryNotifier geometryNotifier,
        FloatingActionButtonLocation previousFloatingActionButtonLocation,
        FloatingActionButtonLocation currentFloatingActionButtonLocation,
        Animation<double> floatingActionButtonMoveAnimation,
        FloatingActionButtonAnimator floatingActionButtonMotionAnimator,
        bool isSnackBarFloating,
        double? snackBarWidth,
        bool extendBody,
        bool extendBodyBehindAppBar,
        bool extendBodyBehindMaterialBanner,
        TextDirection textDirection) : base(relayout: floatingActionButtonMoveAnimation)
    {
        ExtendBody = extendBody;
        ExtendBodyBehindAppBar = extendBodyBehindAppBar;
        MinInsets = minInsets;
        MinViewPadding = minViewPadding;
        GeometryNotifier = geometryNotifier;
        PreviousFloatingActionButtonLocation = previousFloatingActionButtonLocation
                                               ?? throw new ArgumentNullException(
                                                   nameof(previousFloatingActionButtonLocation));
        CurrentFloatingActionButtonLocation = currentFloatingActionButtonLocation
                                              ?? throw new ArgumentNullException(
                                                  nameof(currentFloatingActionButtonLocation));
        FloatingActionButtonMoveAnimation = floatingActionButtonMoveAnimation;
        FloatingActionButtonMotionAnimator = floatingActionButtonMotionAnimator;
        IsSnackBarFloating = isSnackBarFloating;
        SnackBarWidth = snackBarWidth;
        ExtendBodyBehindMaterialBanner = extendBodyBehindMaterialBanner;
        TextDirection = textDirection;
    }

    public bool ExtendBody { get; }
    public bool ExtendBodyBehindAppBar { get; }
    public Thickness MinInsets { get; }
    public Thickness MinViewPadding { get; }
    public ScaffoldGeometryNotifier GeometryNotifier { get; }
    public FloatingActionButtonLocation PreviousFloatingActionButtonLocation { get; }
    public FloatingActionButtonLocation CurrentFloatingActionButtonLocation { get; }
    public Animation<double> FloatingActionButtonMoveAnimation { get; }
    public FloatingActionButtonAnimator FloatingActionButtonMotionAnimator { get; }
    public bool IsSnackBarFloating { get; }
    public double? SnackBarWidth { get; }
    public bool ExtendBodyBehindMaterialBanner { get; }
    public TextDirection TextDirection { get; }

    public override void PerformLayout(Size size)
    {
        var looseConstraints = new BoxConstraints(MaxWidth: size.Width, MaxHeight: size.Height);

        // This part of the layout has the same effect as putting the app bar and the body in a column and
        // making the body flexible. What's different is that in this case the app bar appears _after_ the
        // body in the stacking order, so the app bar's shadow is drawn on top of the body.
        BoxConstraints fullWidthConstraints = looseConstraints.Tighten(width: size.Width);
        double bottom = size.Height;
        double contentTop = 0.0;
        double bottomWidgetsHeight = 0.0;
        double appBarHeight = 0.0;

        if (HasChild(ScaffoldSlot.AppBar))
        {
            appBarHeight = LayoutChild(ScaffoldSlot.AppBar, fullWidthConstraints).Height;
            contentTop = ExtendBodyBehindAppBar ? 0.0 : appBarHeight;
            PositionChild(ScaffoldSlot.AppBar, default);
        }

        double? bottomNavigationBarTop = null;
        if (HasChild(ScaffoldSlot.BottomNavigationBar))
        {
            double bottomNavigationBarHeight =
                LayoutChild(ScaffoldSlot.BottomNavigationBar, fullWidthConstraints).Height;
            bottomWidgetsHeight += bottomNavigationBarHeight;
            bottomNavigationBarTop = Math.Max(0.0, bottom - bottomWidgetsHeight);
            PositionChild(ScaffoldSlot.BottomNavigationBar, new Point(0.0, bottomNavigationBarTop.Value));
        }

        var materialBannerSize = default(Size);
        if (HasChild(ScaffoldSlot.MaterialBanner))
        {
            materialBannerSize = LayoutChild(ScaffoldSlot.MaterialBanner, fullWidthConstraints);
            PositionChild(ScaffoldSlot.MaterialBanner, new Point(0.0, appBarHeight));

            // Push content down only if elevation is 0.
            if (!ExtendBodyBehindMaterialBanner)
            {
                contentTop += materialBannerSize.Height;
            }
        }

        // Set the content bottom to account for the greater of the height of any bottom-anchored material
        // widgets or of the keyboard or other bottom-anchored system UI.
        double contentBottom = Math.Max(0.0, bottom - Math.Max(MinInsets.Bottom, bottomWidgetsHeight));

        if (HasChild(ScaffoldSlot.Body))
        {
            double bodyMaxHeight = Math.Max(0.0, contentBottom - contentTop);

            if (ExtendBody && MinInsets.Bottom <= bottomWidgetsHeight)
            {
                bodyMaxHeight += bottomWidgetsHeight;
                bodyMaxHeight = Math.Clamp(bodyMaxHeight, 0.0, looseConstraints.MaxHeight - contentTop);
            }

            LayoutChild(
                ScaffoldSlot.Body,
                new BoxConstraints(MaxWidth: fullWidthConstraints.MaxWidth, MaxHeight: bodyMaxHeight));
            PositionChild(ScaffoldSlot.Body, new Point(0.0, contentTop));
        }

        // The BottomSheet and the SnackBar are anchored to the bottom of the parent, they are
        // built based on the bottom padding and the current content bottom.
        var bottomSheetSize = default(Size);
        var snackBarSize = default(Size);

        if (HasChild(ScaffoldSlot.BodyScrim))
        {
            var bottomSheetScrimConstraints = new BoxConstraints(
                MaxWidth: fullWidthConstraints.MaxWidth,
                MaxHeight: contentBottom);
            LayoutChild(ScaffoldSlot.BodyScrim, bottomSheetScrimConstraints);
            PositionChild(ScaffoldSlot.BodyScrim, default);
        }

        // Set the size of the SnackBar early if the behavior is fixed so that the FAB is positioned
        // correctly. Otherwise the FAB is positioned above the SnackBar and it is measured afterwards.
        if (!IsSnackBarFloating && HasChild(ScaffoldSlot.SnackBar))
        {
            snackBarSize = LayoutChild(ScaffoldSlot.SnackBar, fullWidthConstraints);
        }

        if (HasChild(ScaffoldSlot.BottomSheet))
        {
            var bottomSheetConstraints = new BoxConstraints(
                MaxWidth: fullWidthConstraints.MaxWidth,
                MaxHeight: Math.Max(0.0, contentBottom - contentTop));
            bottomSheetSize = LayoutChild(ScaffoldSlot.BottomSheet, bottomSheetConstraints);
            PositionChild(
                ScaffoldSlot.BottomSheet,
                new Point(
                    (size.Width - bottomSheetSize.Width) / 2.0,
                    contentBottom - bottomSheetSize.Height));
        }

        Rect floatingActionButtonRect;
        if (HasChild(ScaffoldSlot.FloatingActionButton))
        {
            Size fabSize = LayoutChild(ScaffoldSlot.FloatingActionButton, looseConstraints);

            // To account for the FAB position being changed, we'll animate between the old and new positions.
            var currentGeometry = new ScaffoldPrelayoutGeometry(
                BottomSheetSize: bottomSheetSize,
                ContentBottom: contentBottom,

                // `appBarHeight` should be used instead of `contentTop` because
                // ScaffoldPrelayoutGeometry.contentTop must not be affected by `extendBodyBehindAppBar`.
                ContentTop: appBarHeight,
                FloatingActionButtonSize: fabSize,
                ScaffoldSize: size,
                SnackBarSize: snackBarSize,
                MaterialBannerSize: materialBannerSize,
                TextDirection: TextDirection,
                MinInsets: MinInsets,
                MinViewPadding: MinViewPadding);
            Point currentFabOffset = CurrentFloatingActionButtonLocation.GetOffset(currentGeometry);
            Point previousFabOffset = PreviousFloatingActionButtonLocation.GetOffset(currentGeometry);
            Point fabOffset = FloatingActionButtonMotionAnimator.GetOffset(
                begin: previousFabOffset,
                end: currentFabOffset,
                progress: FloatingActionButtonMoveAnimation.Value);
            PositionChild(ScaffoldSlot.FloatingActionButton, fabOffset);
            floatingActionButtonRect = new Rect(fabOffset, fabSize);
        }
        else
        {
            floatingActionButtonRect = default;
        }

        if (HasChild(ScaffoldSlot.SnackBar))
        {
            bool hasCustomWidth = SnackBarWidth.HasValue && SnackBarWidth.Value < size.Width;
            if (snackBarSize == default)
            {
                snackBarSize = LayoutChild(
                    ScaffoldSlot.SnackBar,
                    hasCustomWidth ? looseConstraints : fullWidthConstraints);
            }

            double snackBarYOffsetBase;
            if (floatingActionButtonRect.Size != default && IsSnackBarFloating
                && ShowSnackBarAboveFab(CurrentFloatingActionButtonLocation))
            {
                snackBarYOffsetBase = bottomNavigationBarTop.HasValue
                    ? Math.Min(bottomNavigationBarTop.Value, floatingActionButtonRect.Top)
                    : floatingActionButtonRect.Top;
            }
            else
            {
                // SnackBarBehavior.fixed applies a SafeArea automatically. SnackBarBehavior.floating does
                // not since the positioning is affected if there is a floating action button between the
                // SnackBar and the bottom of the screen.
                double safeYOffsetBase = size.Height - MinViewPadding.Bottom;
                snackBarYOffsetBase = IsSnackBarFloating
                    ? Math.Min(contentBottom, safeYOffsetBase)
                    : contentBottom;
            }

            double xOffset = hasCustomWidth ? (size.Width - SnackBarWidth!.Value) / 2 : 0.0;
            PositionChild(ScaffoldSlot.SnackBar, new Point(xOffset, snackBarYOffsetBase - snackBarSize.Height));
        }

        GeometryNotifier.UpdateWith(
            bottomNavigationBarTop: bottomNavigationBarTop,
            floatingActionButtonArea: floatingActionButtonRect);
    }

    public override bool ShouldRelayout(MultiChildLayoutDelegate oldDelegate)
    {
        var previous = (ScaffoldLayout)oldDelegate;
        return previous.MinInsets != MinInsets
               || previous.MinViewPadding != MinViewPadding
               || previous.TextDirection != TextDirection
               || !ReferenceEquals(
                   previous.PreviousFloatingActionButtonLocation,
                   PreviousFloatingActionButtonLocation)
               || !ReferenceEquals(
                   previous.CurrentFloatingActionButtonLocation,
                   CurrentFloatingActionButtonLocation)
               || previous.ExtendBody != ExtendBody
               || previous.ExtendBodyBehindAppBar != ExtendBodyBehindAppBar;
    }

    /// <summary>
    /// Ports the <c>showAboveFab</c> switch: a floating snack bar sits above every floating action button
    /// location except the six top ones.
    /// </summary>
    private static bool ShowSnackBarAboveFab(FloatingActionButtonLocation location)
    {
        return !ReferenceEquals(location, FloatingActionButtonLocation.StartTop)
               && !ReferenceEquals(location, FloatingActionButtonLocation.CenterTop)
               && !ReferenceEquals(location, FloatingActionButtonLocation.EndTop)
               && !ReferenceEquals(location, FloatingActionButtonLocation.MiniStartTop)
               && !ReferenceEquals(location, FloatingActionButtonLocation.MiniCenterTop)
               && !ReferenceEquals(location, FloatingActionButtonLocation.MiniEndTop);
    }
}

/// <summary>
/// Ports Flutter's private <c>_FloatingActionButtonTransition</c>: cross-fades, scales and rotates the
/// entering and exiting floating action buttons, and keeps the scaffold geometry's FAB scale in sync.
/// </summary>
internal sealed class FloatingActionButtonTransition : StatefulWidget
{
    public FloatingActionButtonTransition(
        Widget? child,
        Animation<double> fabMoveAnimation,
        FloatingActionButtonAnimator fabMotionAnimator,
        ScaffoldGeometryNotifier geometryNotifier,
        AnimationController currentController,
        Key? key = null) : base(key)
    {
        Child = child;
        FabMoveAnimation = fabMoveAnimation ?? throw new ArgumentNullException(nameof(fabMoveAnimation));
        FabMotionAnimator = fabMotionAnimator ?? throw new ArgumentNullException(nameof(fabMotionAnimator));
        GeometryNotifier = geometryNotifier ?? throw new ArgumentNullException(nameof(geometryNotifier));
        CurrentController = currentController ?? throw new ArgumentNullException(nameof(currentController));
    }

    public Widget? Child { get; }

    public Animation<double> FabMoveAnimation { get; }

    public FloatingActionButtonAnimator FabMotionAnimator { get; }

    public ScaffoldGeometryNotifier GeometryNotifier { get; }

    /// <summary>Controls the current child's entrance and exit; owned by <see cref="ScaffoldState"/>.</summary>
    public AnimationController CurrentController { get; }

    public override State CreateState() => new FloatingActionButtonTransitionState();
}

internal sealed class FloatingActionButtonTransitionState : State
{
    private static readonly Animatable<double> EntranceTurnTween =
        new DoubleTween(begin: 1.0 - FloatingActionButtonConstants.TurnInterval, end: 1.0)
            .Chain(new CurveTween(Curves.EaseIn));

    private AnimationController _previousController = null!;
    private CurvedAnimation? _previousExitScaleAnimation;
    private CurvedAnimation? _previousExitRotationCurvedAnimation;
    private CurvedAnimation? _currentEntranceScaleAnimation;
    private Animation<double> _previousScaleAnimation = null!;
    private TrainHoppingAnimation _previousRotationAnimation = null!;
    private Animation<double> _currentScaleAnimation = null!;
    private Animation<double> _extendedCurrentScaleAnimation = null!;
    private TrainHoppingAnimation _currentRotationAnimation = null!;
    private Widget? _previousChild;

    private FloatingActionButtonTransition CurrentWidget => (FloatingActionButtonTransition)StateWidget;

    public override void InitState()
    {
        base.InitState();
        _previousController = new AnimationController(FloatingActionButtonConstants.Segue, this);
        _previousController.AddStatusListener(HandlePreviousAnimationStatusChanged);
        UpdateAnimations();

        if (CurrentWidget.Child is not null)
        {
            // If we start out with a child, have the child appear fully visible instead of animating in.
            // With FloatingActionButtonAnimator.NoAnimation the geometry's scale stays unset otherwise.
            CurrentWidget.CurrentController.SetValue(1.0);
            UpdateGeometryScale(1.0);
        }
        else
        {
            // If we start without a child we update the geometry object with a scale of 0.0.
            UpdateGeometryScale(0.0);
        }
    }

    public override void Dispose()
    {
        _previousController.RemoveStatusListener(HandlePreviousAnimationStatusChanged);
        _previousController.Dispose();
        _previousExitScaleAnimation?.Dispose();
        _previousExitRotationCurvedAnimation?.Dispose();
        _currentEntranceScaleAnimation?.Dispose();
        DisposeAnimations();
        base.Dispose();
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var previous = (FloatingActionButtonTransition)oldWidget;
        bool oldChildIsNull = previous.Child is null;
        bool newChildIsNull = CurrentWidget.Child is null;
        if (!ReferenceEquals(previous.FabMotionAnimator, CurrentWidget.FabMotionAnimator)
            || !ReferenceEquals(previous.FabMoveAnimation, CurrentWidget.FabMoveAnimation))
        {
            // Get the right scale and rotation animations to use for this widget.
            DisposeAnimations();
            UpdateAnimations();
        }

        if (oldChildIsNull == newChildIsNull && Equals(previous.Child?.Key, CurrentWidget.Child?.Key))
        {
            return;
        }

        if (_previousController.Status == AnimationStatus.Dismissed)
        {
            double currentValue = CurrentWidget.CurrentController.Value;
            if (currentValue == 0.0 || oldChildIsNull)
            {
                // The current child hasn't started its entrance animation yet. We can just skip directly to
                // scaling in the new child.
                _previousChild = null;
                if (CurrentWidget.Child is not null)
                {
                    CurrentWidget.CurrentController.Forward();
                }
            }
            else
            {
                // Otherwise, we need to copy the state from the current controller to the previous
                // controller and run an exit animation for the previous widget before docking the new one.
                _previousChild = previous.Child;
                _previousController.SetValue(currentValue);
                _previousController.Reverse();
                CurrentWidget.CurrentController.SetValue(0.0);
            }
        }
    }

    private static bool IsExtendedFloatingActionButton(Widget? widget) =>
        widget is FloatingActionButton { IsExtended: true };

    private void UpdateAnimations()
    {
        // Get the animations for exit and entrance.
        _previousExitScaleAnimation?.Dispose();
        _previousExitScaleAnimation = new CurvedAnimation(_previousController, Curves.EaseIn);
        _previousExitRotationCurvedAnimation?.Dispose();
        _previousExitRotationCurvedAnimation = new CurvedAnimation(_previousController, Curves.EaseIn);

        Animation<double> previousExitRotationAnimation =
            new DoubleTween(begin: 1.0, end: 1.0).Animate(_previousExitRotationCurvedAnimation);

        _currentEntranceScaleAnimation?.Dispose();
        _currentEntranceScaleAnimation = new CurvedAnimation(CurrentWidget.CurrentController, Curves.EaseIn);
        Animation<double> currentEntranceRotationAnimation =
            CurrentWidget.CurrentController.Drive(EntranceTurnTween);

        // Get the animations for when the FAB is moving.
        Animation<double> moveScaleAnimation =
            CurrentWidget.FabMotionAnimator.GetScaleAnimation(CurrentWidget.FabMoveAnimation);
        Animation<double> moveRotationAnimation =
            CurrentWidget.FabMotionAnimator.GetRotationAnimation(CurrentWidget.FabMoveAnimation);

        // Aggregate the animations.
        if (ReferenceEquals(CurrentWidget.FabMotionAnimator, FloatingActionButtonAnimator.NoAnimation))
        {
            _previousScaleAnimation = moveScaleAnimation;
            _currentScaleAnimation = moveScaleAnimation;
            _previousRotationAnimation = new TrainHoppingAnimation(moveRotationAnimation, null);
            _currentRotationAnimation = new TrainHoppingAnimation(moveRotationAnimation, null);
        }
        else
        {
            _previousScaleAnimation = new AnimationMin<double>(moveScaleAnimation, _previousExitScaleAnimation);
            _currentScaleAnimation = new AnimationMin<double>(moveScaleAnimation, _currentEntranceScaleAnimation);
            _previousRotationAnimation =
                new TrainHoppingAnimation(previousExitRotationAnimation, moveRotationAnimation);
            _currentRotationAnimation =
                new TrainHoppingAnimation(currentEntranceRotationAnimation, moveRotationAnimation);
        }

        _extendedCurrentScaleAnimation = _currentScaleAnimation.Drive(new CurveTween(Curves.Interval(0.0, 0.1)));
        _currentScaleAnimation.AddListener(HandleProgressChanged);
        _previousScaleAnimation.AddListener(HandleProgressChanged);
    }

    private void DisposeAnimations()
    {
        _previousRotationAnimation.Dispose();
        _currentRotationAnimation.Dispose();
    }

    private void HandlePreviousAnimationStatusChanged(AnimationStatus status)
    {
        SetState(() =>
        {
            if (CurrentWidget.Child is not null && status == AnimationStatus.Dismissed)
            {
                CurrentWidget.CurrentController.Forward();
            }
        });
    }

    public override Widget Build(BuildContext context)
    {
        var children = new List<Widget>();
        if (_previousController.Status != AnimationStatus.Dismissed)
        {
            if (IsExtendedFloatingActionButton(_previousChild))
            {
                children.Add(new FadeTransition(opacity: _previousScaleAnimation, child: _previousChild));
            }
            else
            {
                children.Add(new ScaleTransition(
                    scale: _previousScaleAnimation,
                    child: new RotationTransition(
                        turns: _previousRotationAnimation,
                        child: _previousChild)));
            }
        }

        if (IsExtendedFloatingActionButton(CurrentWidget.Child))
        {
            children.Add(new ScaleTransition(
                scale: _extendedCurrentScaleAnimation,
                child: new FadeTransition(
                    opacity: _currentScaleAnimation,
                    child: CurrentWidget.Child)));
        }
        else
        {
            children.Add(new ScaleTransition(
                scale: _currentScaleAnimation,
                child: new RotationTransition(
                    turns: _currentRotationAnimation,
                    child: CurrentWidget.Child)));
        }

        return new Stack(alignment: Alignment.CenterRight, children: children);
    }

    private void HandleProgressChanged()
    {
        UpdateGeometryScale(Math.Max(_previousScaleAnimation.Value, _currentScaleAnimation.Value));
    }

    private void UpdateGeometryScale(double scale)
    {
        CurrentWidget.GeometryNotifier.UpdateWith(floatingActionButtonScale: scale);
    }
}
