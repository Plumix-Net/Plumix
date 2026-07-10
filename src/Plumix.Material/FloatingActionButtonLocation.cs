using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/floating_action_button_location.dart

public sealed record ScaffoldPrelayoutGeometry(
    Size ScaffoldSize,
    double ContentTop,
    double ContentBottom,
    Size FloatingActionButtonSize,
    Size BottomSheetSize,
    Size SnackBarSize,
    Thickness MinInsets,
    Thickness MinViewPadding,
    TextDirection TextDirection);

public abstract class FloatingActionButtonLocation
{
    public const double Margin = 16.0;
    public const double MiniButtonOffsetAdjustment = 8.0;

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
    public static FloatingActionButtonAnimator Scaling { get; } = new ScalingFloatingActionButtonAnimator();
    public static FloatingActionButtonAnimator NoAnimation { get; } = new NoAnimationFloatingActionButtonAnimator();

    public abstract Point GetOffset(Point begin, Point end, double progress);
    public abstract double GetScale(double progress);
    public abstract double GetRotation(double progress);
    public virtual double GetAnimationRestart(double previousValue) => 0.0;

    private sealed class ScalingFloatingActionButtonAnimator : FloatingActionButtonAnimator
    {
        public override Point GetOffset(Point begin, Point end, double progress) => progress < 0.5 ? begin : end;
        public override double GetScale(double progress) => Math.Abs(1.0 - 2.0 * Math.Clamp(progress, 0.0, 1.0));
        public override double GetRotation(double progress) => progress < 0.5 ? 1.0 - 0.25 * progress * 2.0 : 1.0;
        public override double GetAnimationRestart(double previousValue) =>
            Math.Min(1.0 - previousValue, previousValue);
    }

    private sealed class NoAnimationFloatingActionButtonAnimator : FloatingActionButtonAnimator
    {
        public override Point GetOffset(Point begin, Point end, double progress) => end;
        public override double GetScale(double progress) => 1.0;
        public override double GetRotation(double progress) => 1.0;
    }
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

    internal override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderFloatingActionButtonPosition(Geometry, Location);

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
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
