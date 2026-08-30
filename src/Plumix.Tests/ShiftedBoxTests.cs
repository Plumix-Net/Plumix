using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/shifted_box.dart

/// <summary>
/// Covers the `RenderShiftedBox` family: the aligning boxes hold an unresolved
/// <see cref="AlignmentGeometry"/> plus a text direction the way Dart's
/// `RenderAligningShiftedBox` does, and re-resolve when either changes.
/// </summary>
public sealed class ShiftedBoxTests
{
    [Fact]
    public void RenderPadding_ResolvesDirectionalInsetsAndReResolvesOnDirectionChange()
    {
        var child = new FixedSizeRenderBox(new Size(10, 10));
        var padding = new RenderPadding(
            EdgeInsetsGeometry.DirectionalOnly(start: 4, end: 8),
            child,
            textDirection: TextDirection.Ltr);

        padding.Layout(BoxConstraints.Unbounded);
        Assert.Equal(new Size(22, 10), padding.Size);
        Assert.Equal(new Point(4, 0), ((BoxParentData)child.parentData!).offset);

        padding.TextDirection = TextDirection.Rtl;
        padding.Layout(BoxConstraints.Unbounded);
        Assert.Equal(new Size(22, 10), padding.Size);
        Assert.Equal(new Point(8, 0), ((BoxParentData)child.parentData!).offset);

        // The widget-facing property keeps the unresolved geometry, as Dart's `padding` field does.
        Assert.Equal(EdgeInsetsGeometry.DirectionalOnly(start: 4, end: 8), padding.Padding);
    }

    [DebugOnlyFact]
    public void RenderPadding_RejectsNegativeInsets()
    {
        Assert.Throws<AssertionError>(() => new RenderPadding(EdgeInsets.Only(left: -1)));
    }

    [Fact]
    public void RenderPadding_IntrinsicsAddThePaddingAndDeflateTheChildQuery()
    {
        var child = new IntrinsicProbeRenderBox(new Size(30, 20));
        var padding = new RenderPadding(EdgeInsets.All(5), child, textDirection: TextDirection.Ltr);

        Assert.Equal(40.0, padding.GetMinIntrinsicWidth(50));
        Assert.Equal(40.0, child.LastWidthQuery);
        Assert.Equal(30.0, padding.GetMinIntrinsicHeight(50));
        Assert.Equal(40.0, child.LastHeightQuery);
    }

    [Fact]
    public void RenderPositionedBox_ResolvesDirectionalAlignmentAgainstItsTextDirection()
    {
        var child = new FixedSizeRenderBox(new Size(20, 10));
        var box = new RenderPositionedBox(
            child,
            alignment: AlignmentDirectional.TopEnd,
            textDirection: TextDirection.Ltr);

        box.Layout(BoxConstraints.Tight(new Size(100, 50)));
        Assert.Equal(new Point(80, 0), ((BoxParentData)child.parentData!).offset);

        box.TextDirection = TextDirection.Rtl;
        box.Layout(BoxConstraints.Tight(new Size(100, 50)));
        Assert.Equal(new Point(0, 0), ((BoxParentData)child.parentData!).offset);
    }

    [Fact]
    public void RenderPositionedBox_ScalesIntrinsicsByItsFactors()
    {
        var child = new IntrinsicProbeRenderBox(new Size(30, 20));
        var box = new RenderPositionedBox(child, widthFactor: 2.0, heightFactor: 0.5);

        Assert.Equal(60.0, box.GetMinIntrinsicWidth(double.PositiveInfinity));
        Assert.Equal(10.0, box.GetMinIntrinsicHeight(double.PositiveInfinity));
    }

    [DebugOnlyFact]
    public void RenderPositionedBox_RejectsNegativeFactors()
    {
        Assert.Throws<AssertionError>(() => new RenderPositionedBox(widthFactor: -1.0));
        Assert.Throws<AssertionError>(() => new RenderPositionedBox(heightFactor: -1.0));
    }

    [Fact]
    public void RenderConstrainedOverflowBox_AlignsWithTheResolvedAlignment()
    {
        var child = new FixedSizeRenderBox(new Size(40, 20));
        var box = new RenderConstrainedOverflowBox(
            child,
            minWidth: 40,
            maxWidth: 40,
            minHeight: 20,
            maxHeight: 20,
            alignment: AlignmentDirectional.BottomStart,
            textDirection: TextDirection.Rtl);

        box.Layout(BoxConstraints.Tight(new Size(100, 60)));
        Assert.Equal(new Point(60, 40), ((BoxParentData)child.parentData!).offset);
    }

    [Fact]
    public void RenderConstrainedOverflowBox_DeferToChild_SizesToTheChild()
    {
        var child = new FixedSizeRenderBox(new Size(30, 15));
        var box = new RenderConstrainedOverflowBox(
            child,
            maxWidth: 30,
            maxHeight: 15,
            fit: OverflowBoxFit.DeferToChild,
            textDirection: TextDirection.Ltr);

        box.Layout(new BoxConstraints(MinWidth: 0, MaxWidth: 200, MinHeight: 0, MaxHeight: 200));
        Assert.Equal(new Size(30, 15), box.Size);
    }

    [Fact]
    public void RenderSizedOverflowBox_TakesItsRequestedSizeAndReportsChildIntrinsics()
    {
        var child = new FixedSizeRenderBox(new Size(80, 40));
        var box = new RenderSizedOverflowBox(
            new Size(20, 10),
            child,
            alignment: Alignment.BottomRight,
            textDirection: TextDirection.Ltr);

        box.Layout(new BoxConstraints(MinWidth: 0, MaxWidth: 200, MinHeight: 0, MaxHeight: 200));

        Assert.Equal(new Size(20, 10), box.Size);
        Assert.Equal(20.0, box.GetMinIntrinsicWidth(double.PositiveInfinity));
        Assert.Equal(10.0, box.GetMaxIntrinsicHeight(double.PositiveInfinity));
        Assert.Equal(new Point(-60, -30), ((BoxParentData)child.parentData!).offset);
    }

    [Fact]
    public void RenderFractionallySizedOverflowBox_DividesIntrinsicsByTheFactors()
    {
        var child = new IntrinsicProbeRenderBox(new Size(30, 20));
        var box = new RenderFractionallySizedOverflowBox(child, widthFactor: 0.5, heightFactor: 0.25);

        // Dart: child intrinsic / widthFactor, with the cross-axis query scaled by the other factor.
        Assert.Equal(60.0, box.GetMinIntrinsicWidth(80));
        Assert.Equal(20.0, child.LastWidthQuery);
        Assert.Equal(80.0, box.GetMinIntrinsicHeight(80));
        Assert.Equal(40.0, child.LastHeightQuery);
    }

    [Fact]
    public void RenderFractionallySizedOverflowBox_GivesTheChildAFractionOfTheIncomingMaximum()
    {
        var child = new FixedSizeRenderBox(new Size(1000, 1000));
        var box = new RenderFractionallySizedOverflowBox(
            child,
            widthFactor: 0.5,
            heightFactor: 0.25,
            textDirection: TextDirection.Ltr);

        box.Layout(new BoxConstraints(MinWidth: 0, MaxWidth: 200, MinHeight: 0, MaxHeight: 80));

        // The child is forced to 100x20, and the box constrains itself to the child's size.
        Assert.Equal(new Size(100, 20), child.Size);
        Assert.Equal(new Size(100, 20), box.Size);
    }

    [Fact]
    public void BoxConstraints_Unbounded_MatchesDartsDefaultConstructor()
    {
        BoxConstraints unbounded = BoxConstraints.Unbounded;

        Assert.Equal(0.0, unbounded.MinWidth);
        Assert.Equal(0.0, unbounded.MinHeight);
        Assert.Equal(double.PositiveInfinity, unbounded.MaxWidth);
        Assert.Equal(double.PositiveInfinity, unbounded.MaxHeight);
    }

    private sealed class FixedSizeRenderBox : RenderBox
    {
        private readonly Size _size;

        public FixedSizeRenderBox(Size size)
        {
            _size = size;
        }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(_size);
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }

    /// <summary>Records the extent each intrinsic query is made with, so the tests can assert the
    /// deflation/scaling the shifted boxes apply before delegating.</summary>
    private sealed class IntrinsicProbeRenderBox : RenderBox
    {
        private readonly Size _size;

        public IntrinsicProbeRenderBox(Size size)
        {
            _size = size;
        }

        public double LastWidthQuery { get; private set; }

        public double LastHeightQuery { get; private set; }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(_size);
        }

        protected override double ComputeMinIntrinsicWidth(double height)
        {
            LastWidthQuery = height;
            return _size.Width;
        }

        protected override double ComputeMaxIntrinsicWidth(double height)
        {
            LastWidthQuery = height;
            return _size.Width;
        }

        protected override double ComputeMinIntrinsicHeight(double width)
        {
            LastHeightQuery = width;
            return _size.Height;
        }

        protected override double ComputeMaxIntrinsicHeight(double width)
        {
            LastHeightQuery = width;
            return _size.Height;
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }
}
