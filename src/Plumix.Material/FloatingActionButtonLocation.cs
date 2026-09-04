using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/floating_action_button_location.dart

public static class FloatingActionButtonConstants
{
    /// <summary>Flutter's <c>kFloatingActionButtonSegue</c>: how long a FAB takes to appear or disappear.</summary>
    public static readonly TimeSpan Segue = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// Flutter's <c>kFloatingActionButtonTurnInterval</c>: the fraction of a circle the FAB rotates through
    /// when it is appearing or disappearing.
    /// </summary>
    public const double TurnInterval = 0.125;
}

public sealed record ScaffoldPrelayoutGeometry(
    Size ScaffoldSize,
    double ContentTop,
    double ContentBottom,
    Size FloatingActionButtonSize,
    Size BottomSheetSize,
    Size SnackBarSize,
    Thickness MinInsets,
    Thickness MinViewPadding,
    TextDirection TextDirection,
    Size MaterialBannerSize = default);

public abstract class FloatingActionButtonLocation
{
    public const double Margin = 16.0;
    public const double MiniButtonOffsetAdjustment = 4.0;

    public static FloatingActionButtonLocation StartTop { get; } =
        new StandardFabLocation(FabHorizontal.Start, FabVertical.Top);
    public static FloatingActionButtonLocation MiniStartTop { get; } =
        new StandardFabLocation(FabHorizontal.Start, FabVertical.Top, true);
    public static FloatingActionButtonLocation CenterTop { get; } =
        new StandardFabLocation(FabHorizontal.Center, FabVertical.Top);
    public static FloatingActionButtonLocation MiniCenterTop { get; } =
        new StandardFabLocation(FabHorizontal.Center, FabVertical.Top, true);
    public static FloatingActionButtonLocation EndTop { get; } =
        new StandardFabLocation(FabHorizontal.End, FabVertical.Top);
    public static FloatingActionButtonLocation MiniEndTop { get; } =
        new StandardFabLocation(FabHorizontal.End, FabVertical.Top, true);
    public static FloatingActionButtonLocation StartFloat { get; } =
        new StandardFabLocation(FabHorizontal.Start, FabVertical.Float);
    public static FloatingActionButtonLocation MiniStartFloat { get; } =
        new StandardFabLocation(FabHorizontal.Start, FabVertical.Float, true);
    public static FloatingActionButtonLocation CenterFloat { get; } =
        new StandardFabLocation(FabHorizontal.Center, FabVertical.Float);
    public static FloatingActionButtonLocation MiniCenterFloat { get; } =
        new StandardFabLocation(FabHorizontal.Center, FabVertical.Float, true);
    public static FloatingActionButtonLocation EndFloat { get; } =
        new StandardFabLocation(FabHorizontal.End, FabVertical.Float);
    public static FloatingActionButtonLocation MiniEndFloat { get; } =
        new StandardFabLocation(FabHorizontal.End, FabVertical.Float, true);
    public static FloatingActionButtonLocation StartDocked { get; } =
        new StandardFabLocation(FabHorizontal.Start, FabVertical.Docked);
    public static FloatingActionButtonLocation MiniStartDocked { get; } =
        new StandardFabLocation(FabHorizontal.Start, FabVertical.Docked, true);
    public static FloatingActionButtonLocation CenterDocked { get; } =
        new StandardFabLocation(FabHorizontal.Center, FabVertical.Docked);
    public static FloatingActionButtonLocation MiniCenterDocked { get; } =
        new StandardFabLocation(FabHorizontal.Center, FabVertical.Docked, true);
    public static FloatingActionButtonLocation EndDocked { get; } =
        new StandardFabLocation(FabHorizontal.End, FabVertical.Docked);
    public static FloatingActionButtonLocation MiniEndDocked { get; } =
        new StandardFabLocation(FabHorizontal.End, FabVertical.Docked, true);
    public static FloatingActionButtonLocation EndContained { get; } =
        new StandardFabLocation(FabHorizontal.End, FabVertical.Contained);

    public abstract Point GetOffset(ScaffoldPrelayoutGeometry scaffoldGeometry);

    private enum FabHorizontal { Start, Center, End }
    private enum FabVertical { Top, Float, Docked, Contained }

    private sealed class StandardFabLocation(FabHorizontal horizontal, FabVertical vertical, bool mini = false)
        : FloatingActionButtonLocation
    {
        public override Point GetOffset(ScaffoldPrelayoutGeometry geometry)
        {
            double adjustment = mini ? MiniButtonOffsetAdjustment : 0.0;
            double x = horizontal switch
            {
                FabHorizontal.Center => (geometry.ScaffoldSize.Width - geometry.FloatingActionButtonSize.Width) / 2.0,
                FabHorizontal.Start when geometry.TextDirection == TextDirection.Rtl => Right(geometry, adjustment),
                FabHorizontal.End when geometry.TextDirection == TextDirection.Rtl => Left(geometry, adjustment),
                FabHorizontal.Start => Left(geometry, adjustment),
                _ => Right(geometry, adjustment),
            };
            double y = vertical switch
            {
                FabVertical.Top => Top(geometry),
                FabVertical.Float => Float(geometry, adjustment),
                FabVertical.Docked => Docked(geometry),
                _ => Contained(geometry),
            };
            return new Point(x, y);
        }

        private static double Left(ScaffoldPrelayoutGeometry geometry, double adjustment) =>
            Margin + geometry.MinInsets.Left - adjustment;

        private static double Right(ScaffoldPrelayoutGeometry geometry, double adjustment) =>
            geometry.ScaffoldSize.Width
            - Margin
            - geometry.MinInsets.Right
            - geometry.FloatingActionButtonSize.Width
            + adjustment;

        private static double Top(ScaffoldPrelayoutGeometry geometry)
        {
            return geometry.ContentTop > geometry.MinViewPadding.Top
                ? geometry.ContentTop - geometry.FloatingActionButtonSize.Height / 2.0
                : geometry.MinViewPadding.Top;
        }

        private static double Float(ScaffoldPrelayoutGeometry geometry, double adjustment)
        {
            double bottomContentHeight = geometry.ScaffoldSize.Height - geometry.ContentBottom;
            double safeMargin = Math.Max(Margin, geometry.MinViewPadding.Bottom - bottomContentHeight + Margin);
            double y = geometry.ContentBottom - geometry.FloatingActionButtonSize.Height - safeMargin;
            if (geometry.SnackBarSize.Height > 0)
            {
                y = Math.Min(
                    y,
                    geometry.ContentBottom
                    - geometry.SnackBarSize.Height
                    - geometry.FloatingActionButtonSize.Height
                    - Margin);
            }
            if (geometry.BottomSheetSize.Height > 0)
            {
                y = Math.Min(
                    y,
                    geometry.ContentBottom
                    - geometry.BottomSheetSize.Height
                    - geometry.FloatingActionButtonSize.Height / 2.0);
            }
            return y + adjustment;
        }

        private static double Docked(ScaffoldPrelayoutGeometry geometry)
        {
            double contentMargin = geometry.ScaffoldSize.Height - geometry.ContentBottom;
            double halfHeight = geometry.FloatingActionButtonSize.Height / 2.0;
            double safeMargin = contentMargin > geometry.MinInsets.Bottom + halfHeight
                ? 0.0
                : geometry.MinInsets.Bottom == 0.0
                    ? geometry.MinViewPadding.Bottom
                    : halfHeight + Margin;
            double y = geometry.ContentBottom - halfHeight - safeMargin;
            if (geometry.SnackBarSize.Height > 0)
            {
                y = Math.Min(
                    y,
                    geometry.ContentBottom
                    - geometry.SnackBarSize.Height
                    - geometry.FloatingActionButtonSize.Height
                    - Margin);
            }
            if (geometry.BottomSheetSize.Height > 0)
            {
                y = Math.Min(y, geometry.ContentBottom - geometry.BottomSheetSize.Height - halfHeight);
            }
            double maximum = geometry.ScaffoldSize.Height - geometry.FloatingActionButtonSize.Height - safeMargin;
            return Math.Min(maximum, y);
        }

        private static double Contained(ScaffoldPrelayoutGeometry geometry)
        {
            double contentMargin = geometry.ScaffoldSize.Height - geometry.ContentBottom;
            double height = geometry.FloatingActionButtonSize.Height;
            double safeMargin = contentMargin > geometry.MinViewPadding.Bottom + height
                ? 0.0
                : geometry.MinViewPadding.Bottom;
            double y = geometry.ContentBottom + (contentMargin - geometry.MinViewPadding.Bottom - height) / 2.0;
            double maximum = geometry.ScaffoldSize.Height - height - safeMargin;
            return Math.Min(maximum, y);
        }
    }
}

public abstract class FloatingActionButtonAnimator
{
    public static FloatingActionButtonAnimator Scaling { get; } = new ScalingFabMotionAnimator();
    public static FloatingActionButtonAnimator NoAnimation { get; } = new NoAnimationFabMotionAnimator();

    /// <summary>Gets the <see cref="FloatingActionButton"/>'s position relative to the origin of the
    /// <see cref="Scaffold"/> based on <paramref name="progress"/>.</summary>
    public abstract Point GetOffset(Point begin, Point end, double progress);

    /// <summary>Animates the scale of the <see cref="FloatingActionButton"/>.</summary>
    public abstract Animation<double> GetScaleAnimation(Animation<double> parent);

    /// <summary>Animates the rotation of the <see cref="FloatingActionButton"/>, in turns.</summary>
    public abstract Animation<double> GetRotationAnimation(Animation<double> parent);

    /// <summary>
    /// Gets the progress value to restart a motion animation from when the animation is interrupted.
    /// </summary>
    public virtual double GetAnimationRestart(double previousValue) => 0.0;

    private sealed class ScalingFabMotionAnimator : FloatingActionButtonAnimator
    {
        private static readonly Curve ScaleCurve = Curves.Interval(0.5, 1.0, Curves.Ease);

        // Animate the scale down from 1 to 0 in the first half of the animation, then scale back up from
        // 0 to 1 in the second half. The `flipped` curve is used so that the animation is symmetric.
        private static readonly Animatable<double> RotationTween = new DoubleTween(
            begin: 1.0 - (FloatingActionButtonConstants.TurnInterval * 2.0),
            end: 1.0);

        private static readonly Animatable<double> ThresholdCenterTween = new CurveTween(Curves.Threshold(0.5));

        public override Point GetOffset(Point begin, Point end, double progress) => progress < 0.5 ? begin : end;

        public override Animation<double> GetScaleAnimation(Animation<double> parent)
        {
            ArgumentNullException.ThrowIfNull(parent);
            return new AnimationSwap<double>(
                new ReverseAnimation(parent.Drive(new CurveTween(Curves.Flipped(ScaleCurve)))),
                parent.Drive(new CurveTween(ScaleCurve)),
                parent,
                0.5);
        }

        public override Animation<double> GetRotationAnimation(Animation<double> parent)
        {
            ArgumentNullException.ThrowIfNull(parent);
            // This rotation will turn on the way in, but not on the way out.
            return new AnimationSwap<double>(
                parent.Drive(RotationTween),
                new ReverseAnimation(parent.Drive(ThresholdCenterTween)),
                parent,
                0.5);
        }

        // If the animation was just starting, we'll continue from where we left off. If the animation was
        // finishing, we'll treat it as if we were starting at that point in reverse. This avoids a size jump
        // during the animation.
        public override double GetAnimationRestart(double previousValue) =>
            Math.Min(1.0 - previousValue, previousValue);
    }

    private sealed class NoAnimationFabMotionAnimator : FloatingActionButtonAnimator
    {
        public override Point GetOffset(Point begin, Point end, double progress) => end;

        public override Animation<double> GetScaleAnimation(Animation<double> parent) =>
            new ConstantAnimation<double>(1.0);

        public override Animation<double> GetRotationAnimation(Animation<double> parent) =>
            new ConstantAnimation<double>(1.0);
    }
}

/// <summary>
/// Ports Flutter's private <c>_AnimationSwap</c>: an animation that swaps from one animation to the next
/// when the parent animation reaches the swap threshold.
/// </summary>
internal sealed class AnimationSwap<T> : CompoundAnimation<T>
{
    private readonly Animation<double> _parent;
    private readonly double _swapThreshold;

    public AnimationSwap(Animation<T> first, Animation<T> next, Animation<double> parent, double swapThreshold)
        : base(first, next)
    {
        _parent = parent ?? throw new ArgumentNullException(nameof(parent));
        _swapThreshold = swapThreshold;
    }

    public override T Value => _parent.Value < _swapThreshold ? First.Value : Next.Value;
}

/// <summary>
/// Ports Flutter's private <c>_TransitionSnapshotFabLocation</c>: freezes an in-flight FAB motion so a new
/// motion can start from the position the button currently occupies.
/// </summary>
internal sealed class TransitionSnapshotFabLocation : FloatingActionButtonLocation
{
    private readonly FloatingActionButtonLocation _begin;
    private readonly FloatingActionButtonLocation _end;
    private readonly FloatingActionButtonAnimator _animator;
    private readonly double _progress;

    public TransitionSnapshotFabLocation(
        FloatingActionButtonLocation begin,
        FloatingActionButtonLocation end,
        FloatingActionButtonAnimator animator,
        double progress)
    {
        _begin = begin;
        _end = end;
        _animator = animator;
        _progress = progress;
    }

    public override Point GetOffset(ScaffoldPrelayoutGeometry scaffoldGeometry)
    {
        return _animator.GetOffset(
            begin: _begin.GetOffset(scaffoldGeometry),
            end: _end.GetOffset(scaffoldGeometry),
            progress: _progress);
    }

    public override string ToString() =>
        $"TransitionSnapshotFabLocation(begin: {_begin}, end: {_end}, progress: {_progress})";
}

internal sealed class FloatingActionButtonPosition : SingleChildRenderObjectWidget
{
    public FloatingActionButtonPosition(
        ScaffoldPrelayoutGeometry geometry,
        FloatingActionButtonLocation location,
        Widget child)
        : base(child)
    {
        Geometry = geometry;
        Location = location;
    }

    public ScaffoldPrelayoutGeometry Geometry { get; }
    public FloatingActionButtonLocation Location { get; }

    public override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderFloatingActionButtonPosition(Geometry, Location);

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var position = (RenderFloatingActionButtonPosition)renderObject;
        position.Geometry = Geometry;
        position.Location = Location;
    }
}

internal sealed class RenderFloatingActionButtonPosition : RenderProxyBox
{
    private ScaffoldPrelayoutGeometry _geometry;
    private FloatingActionButtonLocation _location;

    public RenderFloatingActionButtonPosition(ScaffoldPrelayoutGeometry geometry, FloatingActionButtonLocation location)
    {
        _geometry = geometry;
        _location = location;
    }

    public ScaffoldPrelayoutGeometry Geometry { get => _geometry; set { _geometry = value; MarkNeedsLayout(); } }
    public FloatingActionButtonLocation Location { get => _location; set { _location = value; MarkNeedsLayout(); } }

    protected override void PerformLayout()
    {
        Size = Constraints.Biggest;
        if (Child is null) return;
        Child.Layout(new BoxConstraints(MaxWidth: Size.Width, MaxHeight: Size.Height), parentUsesSize: true);
        var geometry = _geometry with { ScaffoldSize = Size, FloatingActionButtonSize = Child.Size };
        ((BoxParentData)Child.parentData!).offset = _location.GetOffset(geometry);
    }
}
