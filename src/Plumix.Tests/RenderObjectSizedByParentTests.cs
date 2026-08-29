using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

// Dart parity sources:
// - flutter/packages/flutter/lib/src/rendering/object.dart (sizedByParent, performResize,
//   markNeedsLayoutForSizedByParentChange)
// - flutter/packages/flutter/lib/src/rendering/box.dart (RenderBox.performResize, size setter)
// - flutter/packages/flutter/lib/src/rendering/sliver.dart (RenderSliver.geometry setter)

namespace Plumix.Tests;

/// <summary>
/// Covers Flutter's sized-by-parent layout contract: <c>performResize</c> runs before
/// <c>performLayout</c> and is the only phase allowed to set the size, a sized-by-parent object is
/// always a relayout boundary, and a change of <c>sizedByParent</c> has to go through
/// <c>markNeedsLayoutForSizedByParentChange</c>.
/// </summary>
public sealed class RenderObjectSizedByParentTests
{
    [Fact]
    public void RenderBox_DefaultsToNotSizedByParent_AndNeverRunsPerformResize()
    {
        var box = new ProbeRenderBox(sizedByParent: false);

        Layout(box, BoxConstraints.Loose(new Size(100, 80)));

        Assert.False(box.SizedByParentForTest);
        Assert.Equal(0, box.ResizeCount);
        Assert.Equal(1, box.LayoutCount);
        Assert.Equal(new Size(20, 10), box.Size);
    }

    [Fact]
    public void RenderBox_SizedByParent_SizesFromComputeDryLayoutInPerformResize()
    {
        var box = new ProbeRenderBox(sizedByParent: true);

        Layout(box, BoxConstraints.Loose(new Size(100, 80)));

        Assert.Equal(1, box.ResizeCount);
        Assert.Equal(1, box.LayoutCount);
        // The dry layout, not the (deliberately different) size PerformLayout would have written.
        Assert.Equal(new Size(100, 80), box.Size);
        Assert.Equal(new Size(100, 80), box.GetDryLayout(BoxConstraints.Loose(new Size(100, 80))));
    }

    [Fact]
    public void RenderBox_SizedByParent_RunsPerformResizeBeforePerformLayout()
    {
        var box = new ProbeRenderBox(sizedByParent: true);

        Layout(box, BoxConstraints.Loose(new Size(100, 80)));

        Assert.Equal(["resize", "layout"], box.Phases);
    }

    [Fact]
    public void RenderBox_SizedByParent_ReportsTheResizeAndLayoutPhasesWhileTheyRun()
    {
        var box = new ProbeRenderBox(sizedByParent: true);

        Layout(box, BoxConstraints.Loose(new Size(100, 80)));

        Assert.True(box.DebugDoingThisResizeDuringResize);
        Assert.False(box.DebugDoingThisLayoutDuringResize);
        Assert.True(box.DebugDoingThisLayoutDuringLayout);
        Assert.False(box.DebugDoingThisResizeDuringLayout);
        Assert.False(box.DebugDoingThisResize);
        Assert.False(box.DebugDoingThisLayout);
    }

    [Fact]
    public void RenderBox_SizedByParent_RejectsASizeWrittenFromPerformLayout()
    {
        using var renderErrors = RenderErrorRethrowScope.Enter();
        var box = new SetsSizeInPerformLayoutRenderBox();

        AssertionError error = Assert.Throws<AssertionError>(
            () => Layout(box, BoxConstraints.Loose(new Size(100, 80))));

        Assert.Contains("RenderBox size setter called incorrectly.", error.Message, StringComparison.Ordinal);
        Assert.Contains(
            "It appears that the size setter was called from PerformLayout().",
            error.Message,
            StringComparison.Ordinal);
        Assert.Contains(
            "Because this RenderBox has SizedByParent set to true, it must set its size in PerformResize().",
            error.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void RenderBox_RejectsASizeWrittenOutsideLayout()
    {
        var box = new ProbeRenderBox(sizedByParent: false);

        AssertionError error = Assert.Throws<AssertionError>(() => box.SetSizeOutsideLayout(new Size(1, 1)));

        Assert.Contains("RenderBox size setter called incorrectly.", error.Message, StringComparison.Ordinal);
        Assert.Contains(
            "The size setter was called from outside layout",
            error.Message,
            StringComparison.Ordinal);
        Assert.Contains(
            "Because this RenderBox has SizedByParent set to false, it must set its size in PerformLayout().",
            error.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void RenderBox_WithoutPerformLayoutAndWithoutSizedByParent_ReportsTheMissingOverride()
    {
        using var renderErrors = RenderErrorRethrowScope.Enter();
        var box = new MissingPerformLayoutRenderBox();

        AssertionError error = Assert.Throws<AssertionError>(
            () => Layout(box, BoxConstraints.Loose(new Size(100, 80))));

        Assert.Contains(
            "MissingPerformLayoutRenderBox did not implement PerformLayout().",
            error.Message,
            StringComparison.Ordinal);
        Assert.Contains("set SizedByParent to true", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderBox_SizedByParent_IsARelayoutBoundaryEvenWhenTheParentUsesItsSize()
    {
        var child = new ProbeRenderBox(sizedByParent: true);
        var parent = new SingleChildTestRenderBox(child, parentUsesSize: true);
        LayoutAttached(parent, new Size(100, 80));
        Assert.False(parent.NeedsLayout);

        child.MarkNeedsLayout();

        Assert.True(child.NeedsLayout);
        Assert.False(parent.NeedsLayout);
    }

    [Fact]
    public void RenderBox_NotSizedByParent_DefersToTheParentWhenTheParentUsesItsSize()
    {
        var child = new ProbeRenderBox(sizedByParent: false);
        var parent = new SingleChildTestRenderBox(child, parentUsesSize: true);
        Layout(parent, BoxConstraints.Loose(new Size(100, 80)));
        Assert.False(parent.NeedsLayout);

        child.MarkNeedsLayout();

        Assert.True(child.NeedsLayout);
        Assert.True(parent.NeedsLayout);
    }

    [Fact]
    public void MarkNeedsLayoutForSizedByParentChange_DirtiesTheObjectAndItsParent()
    {
        var child = new ProbeRenderBox(sizedByParent: true);
        var parent = new SingleChildTestRenderBox(child, parentUsesSize: true);
        Layout(parent, BoxConstraints.Loose(new Size(100, 80)));
        Assert.False(parent.NeedsLayout);

        child.MarkNeedsLayoutForSizedByParentChange();

        Assert.True(child.NeedsLayout);
        Assert.True(parent.NeedsLayout);
    }

    [Fact]
    public void MarkNeedsLayoutForSizedByParentChange_OnAParentlessObject_OnlyDirtiesItself()
    {
        var box = new ProbeRenderBox(sizedByParent: true);
        Layout(box, BoxConstraints.Loose(new Size(100, 80)));

        box.MarkNeedsLayoutForSizedByParentChange();

        Assert.True(box.NeedsLayout);
    }

    [Fact]
    public void RenderSliver_RejectsAGeometryWrittenOutsideLayout()
    {
        var sliver = new ProbeRenderSliver();

        AssertionError error = Assert.Throws<AssertionError>(
            () => sliver.SetGeometryOutsideLayout(SliverGeometry.Zero));

        Assert.Contains(
            "RenderSliver geometry setter called incorrectly.",
            error.Message,
            StringComparison.Ordinal);
        Assert.Contains(
            "Because this RenderSliver has SizedByParent set to false, it must set its geometry in "
            + "PerformLayout().",
            error.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void RenderOffstage_TracksSizedByParentWithTheOffstageFlag()
    {
        var child = new ProbeRenderBox(sizedByParent: false);
        var offstage = new RenderOffstage(offstage: true, child: child);
        var parent = new SingleChildTestRenderBox(offstage, parentUsesSize: true);
        PipelineOwner pipeline = LayoutAttached(parent, new Size(100, 80));

        // Offstage: sized by parent, so marking it dirty stops at the offstage box itself.
        Assert.Equal(new Size(0, 0), offstage.Size);
        offstage.MarkNeedsLayout();
        Assert.False(parent.NeedsLayout);

        // Not offstage: the flag flip has to dirty the parent too, because the boundary moved.
        offstage.Offstage = false;
        Assert.True(parent.NeedsLayout);

        pipeline.FlushLayout(new Size(100, 80));
        Assert.Equal(new Size(20, 10), offstage.Size);
        offstage.MarkNeedsLayout();
        Assert.True(parent.NeedsLayout);
    }

    [Fact]
    public void RenderOffstage_ReportsZeroIntrinsicsAndNoBaselineWhileOffstage()
    {
        var child = new ProbeRenderBox(sizedByParent: false);
        var offstage = new RenderOffstage(offstage: true, child: child);
        var constraints = BoxConstraints.Loose(new Size(100, 80));

        Assert.Equal(0.0, offstage.GetMinIntrinsicWidth(80));
        Assert.Equal(0.0, offstage.GetMaxIntrinsicWidth(80));
        Assert.Equal(0.0, offstage.GetMinIntrinsicHeight(100));
        Assert.Equal(0.0, offstage.GetMaxIntrinsicHeight(100));
        Assert.Equal(new Size(0, 0), offstage.GetDryLayout(constraints));
        Assert.Null(offstage.GetDryBaseline(constraints, TextBaseline.Alphabetic));
        Assert.False(offstage.PaintsChild(child));
    }

    [Fact]
    public void RenderOffstage_LaysOutItsChildWithoutClaimingItsSize()
    {
        var child = new ProbeRenderBox(sizedByParent: false);
        var offstage = new RenderOffstage(offstage: true, child: child);

        Layout(offstage, BoxConstraints.Loose(new Size(100, 80)));

        Assert.Equal(new Size(0, 0), offstage.Size);
        Assert.Equal(new Size(20, 10), child.Size);
        Assert.True(offstage.PaintsChild(child) == false);
    }

    [Fact]
    public void RenderConstrainedOverflowBox_IsSizedByParentOnlyWithOverflowBoxFitMax()
    {
        var child = new ProbeRenderBox(sizedByParent: false);
        var box = new RenderConstrainedOverflowBox(
            child: child,
            minWidth: 0,
            maxWidth: 200,
            minHeight: 0,
            maxHeight: 200,
            fit: OverflowBoxFit.Max);
        var parent = new SingleChildTestRenderBox(box, parentUsesSize: true);
        PipelineOwner pipeline = LayoutAttached(parent, new Size(100, 80));

        Assert.Equal(new Size(100, 80), box.Size);
        box.MarkNeedsLayout();
        Assert.False(parent.NeedsLayout);

        box.Fit = OverflowBoxFit.DeferToChild;
        Assert.True(parent.NeedsLayout);

        pipeline.FlushLayout(new Size(100, 80));
        Assert.Equal(new Size(20, 10), box.Size);
        box.MarkNeedsLayout();
        Assert.True(parent.NeedsLayout);
    }

    [Fact]
    public void RenderConstrainedOverflowBox_ReportsADryLayoutForBothFits()
    {
        var child = new ProbeRenderBox(sizedByParent: false);
        var constraints = BoxConstraints.Loose(new Size(100, 80));
        var box = new RenderConstrainedOverflowBox(
            child: child,
            minWidth: 0,
            maxWidth: 200,
            minHeight: 0,
            maxHeight: 200,
            fit: OverflowBoxFit.Max);

        Assert.Equal(new Size(100, 80), box.GetDryLayout(constraints));

        box.Fit = OverflowBoxFit.DeferToChild;
        Assert.Equal(new Size(20, 10), box.GetDryLayout(constraints));
    }

    [Fact]
    public void RenderViewport_IsSizedByParentAndReportsTheBiggestConstraintAsItsDryLayout()
    {
        var viewport = new RenderViewport(offset: ViewportOffset.Zero());

        Assert.Equal(new Size(100, 80), viewport.GetDryLayout(BoxConstraints.Loose(new Size(100, 80))));

        Layout(viewport, BoxConstraints.Loose(new Size(100, 80)));
        Assert.Equal(new Size(100, 80), viewport.Size);
    }

    [Fact]
    public void RenderViewport_RejectsUnboundedSpaceOnEitherAxis()
    {
        var vertical = new RenderViewport(offset: ViewportOffset.Zero());
        AssertionError unboundedHeight = Assert.Throws<AssertionError>(
            () => vertical.GetDryLayout(new BoxConstraints(MaxWidth: 100)));
        Assert.Contains(
            "Vertical viewport was given unbounded height.",
            unboundedHeight.Message,
            StringComparison.Ordinal);

        AssertionError unboundedWidth = Assert.Throws<AssertionError>(
            () => vertical.GetDryLayout(new BoxConstraints(MaxHeight: 80)));
        Assert.Contains(
            "Vertical viewport was given unbounded width.",
            unboundedWidth.Message,
            StringComparison.Ordinal);

        var horizontal = new RenderViewport(
            axisDirection: AxisDirection.Right,
            offset: ViewportOffset.Zero());
        AssertionError horizontalError = Assert.Throws<AssertionError>(
            () => horizontal.GetDryLayout(new BoxConstraints(MaxHeight: 80)));
        Assert.Contains(
            "Horizontal viewport was given unbounded width.",
            horizontalError.Message,
            StringComparison.Ordinal);
    }

    private static void Layout(RenderBox box, BoxConstraints constraints)
    {
        box.Layout(constraints, parentUsesSize: true);
    }

    /// <summary>
    /// Lays <paramref name="box"/> out inside a real render tree.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>markNeedsLayout</c> only takes the relayout-boundary branch when the render object
    /// has a <c>PipelineOwner</c>; a detached boundary falls through to <c>markParentNeedsLayout</c>.
    /// Tests that observe the boundary therefore need an attached tree.
    /// </remarks>
    private static PipelineOwner LayoutAttached(RenderBox box, Size size)
    {
        var view = new RenderView { Child = box };
        var pipeline = new PipelineOwner(view);
        pipeline.Attach(view);
        pipeline.FlushLayout(size);
        return pipeline;
    }

    /// <summary>A box whose sized-by-parent behavior and phase transitions the tests can observe.</summary>
    private sealed class ProbeRenderBox : RenderBox
    {
        private readonly bool _sizedByParent;

        public ProbeRenderBox(bool sizedByParent)
        {
            _sizedByParent = sizedByParent;
        }

        public List<string> Phases { get; } = [];

        public int ResizeCount { get; private set; }

        public int LayoutCount { get; private set; }

        public bool DebugDoingThisResizeDuringResize { get; private set; }

        public bool DebugDoingThisLayoutDuringResize { get; private set; }

        public bool DebugDoingThisResizeDuringLayout { get; private set; }

        public bool DebugDoingThisLayoutDuringLayout { get; private set; }

        public bool SizedByParentForTest => SizedByParent;

        protected override bool SizedByParent => _sizedByParent;

        protected override Size ComputeDryLayout(BoxConstraints constraints) =>
            _sizedByParent ? constraints.Biggest : constraints.Constrain(new Size(20, 10));

        protected override void PerformResize()
        {
            Phases.Add("resize");
            ResizeCount += 1;
            DebugDoingThisResizeDuringResize = DebugDoingThisResize;
            DebugDoingThisLayoutDuringResize = DebugDoingThisLayout;
            base.PerformResize();
        }

        protected override void PerformLayout()
        {
            Phases.Add("layout");
            LayoutCount += 1;
            DebugDoingThisResizeDuringLayout = DebugDoingThisResize;
            DebugDoingThisLayoutDuringLayout = DebugDoingThisLayout;
            if (!_sizedByParent)
            {
                Size = Constraints.Constrain(new Size(20, 10));
            }
        }

        public void SetSizeOutsideLayout(Size size) => Size = size;

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }

    /// <summary>A sized-by-parent box that illegally writes its size from <c>PerformLayout</c>.</summary>
    private sealed class SetsSizeInPerformLayoutRenderBox : RenderBox
    {
        protected override bool SizedByParent => true;

        protected override Size ComputeDryLayout(BoxConstraints constraints) => constraints.Biggest;

        protected override void PerformLayout()
        {
            Size = Constraints.Smallest;
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }

    /// <summary>Flutter's <c>MissingPerformLayoutRenderBox</c> from <c>test/rendering/box_test.dart</c>.</summary>
    private sealed class MissingPerformLayoutRenderBox : RenderBox
    {
        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }

    /// <summary>A box that lays a single child out and adopts its size.</summary>
    private sealed class SingleChildTestRenderBox : RenderBox
    {
        private readonly RenderBox _child;
        private readonly bool _parentUsesSize;

        public SingleChildTestRenderBox(RenderBox child, bool parentUsesSize)
        {
            _child = child;
            _parentUsesSize = parentUsesSize;
            AdoptChild(child);
        }

        protected override void PerformLayout()
        {
            _child.Layout(Constraints, parentUsesSize: _parentUsesSize);
            Size = _parentUsesSize ? _child.Size : Constraints.Smallest;
        }

        public override void VisitChildren(Action<RenderObject> visitor) => visitor(_child);

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }

    /// <summary>A sliver that only exists so the geometry setter's phase check can be exercised.</summary>
    private sealed class ProbeRenderSliver : RenderSliver
    {
        protected override void PerformSliverLayout(SliverConstraints constraints)
        {
            Geometry = SliverGeometry.Zero;
        }

        public void SetGeometryOutsideLayout(SliverGeometry geometry) => Geometry = geometry;

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }
}
