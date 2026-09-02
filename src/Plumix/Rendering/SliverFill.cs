using Avalonia;
using Avalonia.Media;
using Plumix.Widgets;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/sliver_fill.dart

public class RenderSliverFillViewport : RenderSliverFixedExtentBoxAdaptor
{
    private double _viewportFraction;
    private bool _allowImplicitScrolling;

    public RenderSliverFillViewport(
        double viewportFraction = 1.0,
        bool allowImplicitScrolling = true,
        IRenderSliverBoxChildManager? childManager = null)
        : base(childManager)
    {
        _viewportFraction = ValidateViewportFraction(viewportFraction);
        _allowImplicitScrolling = allowImplicitScrolling;
    }

    /// <inheritdoc />
    public override double? ItemExtent => ConstraintsForSliver.ViewportMainAxisExtent * _viewportFraction;

    public double ViewportFraction
    {
        get => _viewportFraction;
        set
        {
            double validated = ValidateViewportFraction(value);
            if (Math.Abs(_viewportFraction - validated) <= 0.0001)
            {
                return;
            }

            _viewportFraction = validated;
            MarkNeedsLayout();
        }
    }

    public bool AllowImplicitScrolling
    {
        get => _allowImplicitScrolling;
        set
        {
            if (_allowImplicitScrolling == value)
            {
                return;
            }

            _allowImplicitScrolling = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (_allowImplicitScrolling)
        {
            base.VisitChildrenForSemantics(visitor);
            return;
        }

        SliverConstraints constraints = ConstraintsForSliver;
        double itemExtent = ItemExtent!.Value;
        double visibleStart = constraints.ScrollOffset;
        double visibleEnd = visibleStart + constraints.ViewportMainAxisExtent;

        for (RenderBox? child = FirstChild; child != null; child = ChildAfter(child))
        {
            var childParentData = (SliverMultiBoxAdaptorParentData)child.parentData!;
            if (childParentData.LayoutOffset is not { } childStart)
            {
                continue;
            }

            if (childStart >= visibleEnd)
            {
                break;
            }

            if (childStart + itemExtent > visibleStart)
            {
                visitor(child);
            }
        }
    }

    private static double ValidateViewportFraction(double value)
    {
        if (!double.IsFinite(value) || value <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "viewportFraction must be positive and finite.");
        }

        return value;
    }
}

internal sealed class RenderSliverFractionalPadding : RenderSliverPadding
{
    private double _viewportFraction;

    public RenderSliverFractionalPadding(
        double viewportFraction = 0.0,
        RenderSliver? sliver = null)
        : base(default, sliver)
    {
        _viewportFraction = ValidateViewportFraction(viewportFraction);
    }

    public double ViewportFraction
    {
        get => _viewportFraction;
        set
        {
            double validated = ValidateViewportFraction(value);
            if (Math.Abs(_viewportFraction - validated) <= 0.0001)
            {
                return;
            }

            _viewportFraction = validated;
            MarkNeedsLayout();
        }
    }

    protected override Thickness ResolvePaddingForConstraints(SliverConstraints constraints)
    {
        double paddingValue = constraints.ViewportMainAxisExtent * _viewportFraction;
        return constraints.Axis == Axis.Horizontal
            ? new Thickness(paddingValue, 0.0, paddingValue, 0.0)
            : new Thickness(0.0, paddingValue, 0.0, paddingValue);
    }

    private static double ValidateViewportFraction(double value)
    {
        if (!double.IsFinite(value) || value < 0.0 || value > 0.5)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Fractional sliver padding must be between 0 and 0.5.");
        }

        return value;
    }
}

public sealed class RenderSliverFillRemainingWithScrollable : RenderSliverSingleBoxAdapter
{
    public RenderSliverFillRemainingWithScrollable(RenderBox? child = null)
    {
        Child = child;
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        double extent = Math.Max(
            0.0,
            constraints.RemainingPaintExtent - Math.Min(constraints.Overlap, 0.0));
        double cacheExtent = CalculateCacheOffset(
            constraints,
            from: 0.0,
            to: constraints.ViewportMainAxisExtent);

        if (Child != null)
        {
            double maxExtent = extent == 0.0 && cacheExtent > 0.0
                ? cacheExtent
                : extent;
            Child.Layout(
                constraints.AsBoxConstraints(minExtent: extent, maxExtent: maxExtent),
                parentUsesSize: true);
        }

        double paintedChildSize = CalculatePaintOffset(constraints, from: 0.0, to: extent);
        Geometry = new SliverGeometry(
            ScrollExtent: constraints.ViewportMainAxisExtent,
            PaintExtent: paintedChildSize,
            LayoutExtent: paintedChildSize,
            MaxPaintExtent: paintedChildSize,
            CacheExtent: cacheExtent,
            HasVisualOverflow: extent > constraints.RemainingPaintExtent || constraints.ScrollOffset > 0.0);

        if (Child != null)
        {
            SetChildParentData(Child, constraints, Geometry);
        }
    }
}

public sealed class RenderSliverFillRemaining : RenderSliverSingleBoxAdapter
{
    public RenderSliverFillRemaining(RenderBox? child = null)
    {
        Child = child;
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        double extent = Math.Max(0.0, constraints.ViewportMainAxisExtent - constraints.PrecedingScrollExtent);
        if (Child != null)
        {
            double childExtent = ChildIntrinsicExtent(Child, constraints);
            extent = Math.Max(extent, childExtent);
            Child.Layout(
                constraints.AsBoxConstraints(minExtent: extent, maxExtent: extent),
                parentUsesSize: true);
        }

        EnsureFiniteExtent(extent);
        double paintedChildSize = CalculatePaintOffset(constraints, from: 0.0, to: extent);
        double cacheExtent = CalculateCacheOffset(constraints, from: 0.0, to: extent);
        Geometry = new SliverGeometry(
            ScrollExtent: extent,
            PaintExtent: paintedChildSize,
            LayoutExtent: paintedChildSize,
            MaxPaintExtent: paintedChildSize,
            CacheExtent: cacheExtent,
            HasVisualOverflow: extent > constraints.RemainingPaintExtent || constraints.ScrollOffset > 0.0);

        if (Child != null)
        {
            SetChildParentData(Child, constraints, Geometry);
        }
    }

    internal static double ChildIntrinsicExtent(RenderBox child, SliverConstraints constraints)
    {
        return constraints.Axis == Axis.Vertical
            ? child.GetMaxIntrinsicHeight(constraints.CrossAxisExtent)
            : child.GetMaxIntrinsicWidth(constraints.CrossAxisExtent);
    }

    internal static void EnsureFiniteExtent(double extent)
    {
        if (!double.IsFinite(extent))
        {
            throw new InvalidOperationException(
                "The calculated extent for the child of SliverFillRemaining is not finite. "
                + "A scrollable child requires hasScrollBody to remain true.");
        }
    }
}

public sealed class RenderSliverFillRemainingAndOverscroll : RenderSliverSingleBoxAdapter
{
    public RenderSliverFillRemainingAndOverscroll(RenderBox? child = null)
    {
        Child = child;
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        double extent = Math.Max(0.0, constraints.ViewportMainAxisExtent - constraints.PrecedingScrollExtent);
        double maxExtent = Math.Max(
            0.0,
            constraints.RemainingPaintExtent - Math.Min(constraints.Overlap, 0.0));

        if (Child != null)
        {
            double childExtent = RenderSliverFillRemaining.ChildIntrinsicExtent(Child, constraints);
            extent = Math.Max(extent, childExtent);
            maxExtent = Math.Max(extent, maxExtent);
            Child.Layout(
                constraints.AsBoxConstraints(minExtent: extent, maxExtent: maxExtent),
                parentUsesSize: true);
        }

        RenderSliverFillRemaining.EnsureFiniteExtent(extent);
        double cacheExtent = CalculateCacheOffset(constraints, from: 0.0, to: extent);
        double paintExtent = Math.Min(maxExtent, constraints.RemainingPaintExtent);
        Geometry = new SliverGeometry(
            ScrollExtent: extent,
            PaintExtent: paintExtent,
            LayoutExtent: paintExtent,
            MaxPaintExtent: maxExtent,
            CacheExtent: cacheExtent,
            HasVisualOverflow: extent > constraints.RemainingPaintExtent || constraints.ScrollOffset > 0.0);

        if (Child != null)
        {
            SetChildParentData(Child, constraints, Geometry);
        }
    }
}
