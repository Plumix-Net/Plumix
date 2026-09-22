using System.Diagnostics;
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
    public override double? ItemExtent => Constraints.ViewportMainAxisExtent * _viewportFraction;

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

    public override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (_allowImplicitScrolling)
        {
            base.VisitChildrenForSemantics(visitor);
            return;
        }

        SliverConstraints constraints = Constraints;
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

/// <remarks>Flutter's private <c>_RenderSliverFractionalPadding</c> (widgets/sliver_fill.dart).</remarks>
internal sealed class RenderSliverFractionalPadding : RenderSliverEdgeInsetsPadding
{
    private SliverConstraints? _lastResolvedConstraints;
    private double _viewportFraction;
    private Thickness? _resolvedPadding;

    public RenderSliverFractionalPadding(double viewportFraction = 0.0)
    {
        Debug.Assert(viewportFraction <= 0.5);
        Debug.Assert(viewportFraction >= 0);
        _viewportFraction = viewportFraction;
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

            _viewportFraction = value;
            MarkNeedsResolution();
        }
    }

    /// <inheritdoc />
    public override Thickness? ResolvedPadding => _resolvedPadding;

    private void MarkNeedsResolution()
    {
        _resolvedPadding = null;
        MarkNeedsLayout();
    }

    private void Resolve()
    {
        if (_resolvedPadding != null && _lastResolvedConstraints == Constraints)
        {
            return;
        }

        double paddingValue = Constraints.ViewportMainAxisExtent * ViewportFraction;
        _lastResolvedConstraints = Constraints;
        _resolvedPadding = Constraints.Axis switch
        {
            Axis.Horizontal => new Thickness(paddingValue, 0.0, paddingValue, 0.0),
            _ => new Thickness(0.0, paddingValue, 0.0, paddingValue),
        };
    }

    protected override void PerformLayout()
    {
        Resolve();
        base.PerformLayout();
    }
}

public sealed class RenderSliverFillRemainingWithScrollable : RenderSliverSingleBoxAdapter
{
    public RenderSliverFillRemainingWithScrollable(RenderBox? child = null)
    {
        Child = child;
    }

    protected override void PerformLayout()
    {
        SliverConstraints constraints = Constraints;
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

    protected override void PerformLayout()
    {
        SliverConstraints constraints = Constraints;
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

    protected override void PerformLayout()
    {
        SliverConstraints constraints = Constraints;
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
