using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/test/rendering/box_test.dart

namespace Plumix.Tests;

/// <summary>
/// Ports <c>box_test.dart</c>: the <c>RenderBox</c> protocol errors (layout, size setter, size access,
/// intrinsics, hit testing, paint transform), <c>BoxHitTestResult</c> and <c>BaselineOffset</c>.
/// </summary>
/// <remarks>
/// Not ported: the <c>debugAdoptSize</c> half of "Set size error messages" (a C# size carries no owner,
/// see <c>docs/ai/DIVERGENCES.md</c>), and the <c>UnconstrainedBox</c>/<c>ConstraintsTransformBox</c>
/// cases, which exercise <c>shifted_box.dart</c> and live in <c>UnconstrainedLimitedBoxTests</c>.
/// </remarks>
public sealed class BoxTests
{
    [Fact]
    public void ShouldSizeToRenderView()
    {
        var root = new RenderDecoratedBox(new BoxDecoration(
            Color: Color.FromUInt32(0xFF00FF00),
            Gradient: new RadialGradient(
                center: Alignment.TopLeft,
                radius: 1.8,
                colors: [Color.FromUInt32(0xFFFFFF00), Color.FromUInt32(0xFF00FFFF)])));
        new RenderingHarness(root);
        Assert.Equal(800.0, root.Size.Width);
        Assert.Equal(600.0, root.Size.Height);
    }

    [DebugOnlyFact]
    public void PerformLayoutErrorMessage()
    {
        FlutterError result = Assert.Throws<FlutterError>(
            () => new MissingPerformLayoutRenderBox().CallPerformLayout());
        Assert.Equal(
            "FlutterError\n"
            + "   MissingPerformLayoutRenderBox did not implement performLayout().\n"
            + "   RenderBox subclasses need to either override performLayout() to\n"
            + "   set a size and lay out any children, or, set sizedByParent to\n"
            + "   true so that performResize() sizes the render object.\n",
            result.ToStringDeep());
        DiagnosticsNode hint = Assert.Single(result.Diagnostics, node => node.Level == DiagnosticLevel.Hint);
        Assert.Equal(
            "RenderBox subclasses need to either override performLayout() to set a size and lay out any children, "
            + "or, set sizedByParent to true so that performResize() sizes the render object.",
            hint.ToString());
    }

    [DebugOnlyFact]
    public void ApplyPaintTransformErrorMessage()
    {
        var paddingBox = new RenderPadding(EdgeInsets.All(10.0));
        var root = new RenderPadding(EdgeInsets.All(10.0), paddingBox);
        new RenderingHarness(root);
        // Trigger the error by overriding the parentData with data that isn't a BoxParentData.
        paddingBox.parentData = new ParentData();

        FlutterError result = Assert.Throws<FlutterError>(
            () => root.ApplyPaintTransform(paddingBox, Matrix4.Identity()));

        string deep = FrameworkDartTester.IgnoringHashCodes(result.ToStringDeep());
        Assert.StartsWith(
            "FlutterError\n"
            + "   RenderPadding does not implement applyPaintTransform.\n"
            + "   The following RenderPadding object: RenderPadding#00000",
            deep);
        Assert.Contains(
            "   ...did not use a BoxParentData class for the parentData field of the following child:\n"
            + "     RenderPadding#00000",
            deep,
            StringComparison.Ordinal);
        Assert.Contains("     constraints: BoxConstraints(w=780.0, h=580.0)\n", deep, StringComparison.Ordinal);
        Assert.Contains("     size: Size(780.0, 580.0)\n", deep, StringComparison.Ordinal);
        Assert.EndsWith(
            "   The RenderPadding class inherits from RenderBox.\n"
            + "   The default applyPaintTransform implementation provided by\n"
            + "   RenderBox assumes that the children all use BoxParentData objects\n"
            + "   for their parentData field. Since RenderPadding does not in fact\n"
            + "   use that ParentData class for its children, it must provide an\n"
            + "   implementation of applyPaintTransform that supports the specific\n"
            + "   ParentData subclass used by its children (which apparently is\n"
            + "   ParentData).\n",
            deep);
        DiagnosticsNode hint = Assert.Single(result.Diagnostics, node => node.Level == DiagnosticLevel.Hint);
        Assert.EndsWith("(which apparently is ParentData).", hint.ToString());
    }

    [DebugOnlyFact]
    public void SetSizeErrorMessages()
    {
        var root = new RenderDecoratedBox(new BoxDecoration(Color: Color.FromUInt32(0xFF00FF00)));
        new RenderingHarness(root);

        var testBox = new MissingPerformLayoutRenderBox();
        FlutterError result = Assert.Throws<FlutterError>(testBox.TriggerExceptionSettingSizeOutsideOfLayout);
        Assert.Equal(
            "FlutterError\n"
            + "   RenderBox size setter called incorrectly.\n"
            + "   The size setter was called from outside layout (neither\n"
            + "   performResize() nor performLayout() were being run for this\n"
            + "   object).\n"
            + "   Because this RenderBox has sizedByParent set to false, it must\n"
            + "   set its size in performLayout().\n",
            result.ToStringDeep());
        Assert.DoesNotContain(result.Diagnostics, node => node.Level == DiagnosticLevel.Hint);
    }

    [DebugOnlyFact]
    public void InvalidSizeAccessErrorMessage()
    {
        // flutter_test's binding turns `debugCheckIntrinsicSizes` on; its dry-layout comparison is what
        // calls computeDryLayout again once the box has a size.
        var box = new InvalidSizeAccessInDryLayoutBox();
        List<FlutterErrorDetails> errors;
        RenderingDebug.CheckIntrinsicSizes = true;
        try
        {
            errors = CollectErrors(() => box.Layout(BoxConstraints.TightFor(width: 100.0, height: 100.0)));
        }
        finally
        {
            RenderingDebug.CheckIntrinsicSizes = false;
        }

        FlutterErrorDetails details = Assert.Single(errors);
        Assert.Contains(
            "RenderBox.size accessed in InvalidSizeAccessInDryLayoutBox.computeDryLayout. The computeDryLayout "
            + "method must not access the RenderBox's own size, or the size of its child, because it's established "
            + "in performLayout or performResize using different BoxConstraints.",
            details.ToString().Replace('\n', ' '),
            StringComparison.Ordinal);
    }

    [Fact]
    public void DoesNotThrowWhenAccessingChildSizeFromComputeDistanceToActualBaseline()
    {
        // flutter#157915.
        var leaf = new RenderConstrainedBox(BoxConstraints.TightFor(width: 10.0, height: 20.0));
        var root = new BaselineSizeAccessRootRenderBox(new BaselineSizeAccessChildRenderBox(leaf));
        List<FlutterErrorDetails> errors = CollectErrors(() => new RenderingHarness(root));
        Assert.Empty(errors);
    }

    [Fact]
    public void FlexAndPadding()
    {
        var size = new RenderConstrainedBox(BoxConstraints.Unbounded.Tighten(height: 100.0));
        var inner = new RenderDecoratedBox(new BoxDecoration(Color: Color.FromUInt32(0xFF00FF00)), child: size);
        var padding = new RenderPadding(EdgeInsets.All(50.0), inner);
        var flex = new RenderFlex(
            children: [padding],
            direction: Axis.Vertical,
            crossAxisAlignment: CrossAxisAlignment.Stretch);
        var outer = new RenderDecoratedBox(new BoxDecoration(Color: Color.FromUInt32(0xFF0000FF)), child: flex);

        new RenderingHarness(outer);

        Assert.Equal(new Size(700.0, 100.0), size.Size);
        Assert.Equal(new Size(700.0, 100.0), inner.Size);
        Assert.Equal(new Size(800.0, 200.0), padding.Size);
        Assert.Equal(new Size(800.0, 600.0), flex.Size);
        Assert.Equal(new Size(800.0, 600.0), outer.Size);
    }

    [DebugOnlyFact]
    public void ShouldNotHaveAZeroSizedColoredBox()
    {
        var coloredBox = new RenderDecoratedBox(new BoxDecoration());

        string before = FrameworkDartTester.IgnoringHashCodes(
            coloredBox.ToStringDeep(minLevel: DiagnosticLevel.Info));
        Assert.StartsWith(
            "RenderDecoratedBox#00000 NEEDS-LAYOUT NEEDS-PAINT DETACHED\n"
            + "   parentData: MISSING\n"
            + "   constraints: MISSING\n"
            + "   size: MISSING\n",
            before);

        var paddingBox = new RenderPadding(EdgeInsets.All(10.0), coloredBox);
        var root = new RenderDecoratedBox(new BoxDecoration(), child: paddingBox);
        new RenderingHarness(root);

        Assert.Equal(780.0, coloredBox.Size.Width);
        Assert.Equal(580.0, coloredBox.Size.Height);
        string after = FrameworkDartTester.IgnoringHashCodes(
            coloredBox.ToStringDeep(minLevel: DiagnosticLevel.Info));
        Assert.StartsWith(
            "RenderDecoratedBox#00000 NEEDS-PAINT\n"
            + "   parentData: offset=Offset(10.0, 10.0) (can use size)\n"
            + "   constraints: BoxConstraints(w=780.0, h=580.0)\n"
            + "   size: Size(780.0, 580.0)\n",
            after);
    }

    [Fact]
    public void ReparentingShouldClearPosition()
    {
        var coloredBox = new RenderDecoratedBox(new BoxDecoration());
        var paddedBox = new RenderPadding(EdgeInsets.All(10.0), coloredBox);
        new RenderingHarness(paddedBox);
        var parentData = (BoxParentData)coloredBox.parentData!;
        Assert.NotEqual(0.0, parentData.offset.X);

        paddedBox.Child = null;
        var constrainedBox = new RenderConstrainedBox(BoxConstraints.Unbounded, coloredBox);
        new RenderingHarness(constrainedBox);
        Assert.Equal(typeof(ParentData), coloredBox.parentData!.GetType());
    }

    [DebugOnlyFact]
    public void GetMinIntrinsicWidthErrorHandling()
    {
        var testBox = new RenderDecoratedBox(new BoxDecoration());

        FlutterError minWidth = Assert.Throws<FlutterError>(() => testBox.GetMinIntrinsicWidth(-1));
        Assert.Equal(
            "FlutterError\n"
            + "   The height argument to getMinIntrinsicWidth was negative.\n"
            + "   The argument to getMinIntrinsicWidth must not be negative or\n"
            + "   null.\n"
            + "   If you perform computations on another height before passing it\n"
            + "   to getMinIntrinsicWidth, consider using math.max() or\n"
            + "   double.clamp() to force the value into the valid range.\n",
            minWidth.ToStringDeep());
        Assert.Equal(
            "If you perform computations on another height before passing it to getMinIntrinsicWidth, consider "
            + "using math.max() or double.clamp() to force the value into the valid range.",
            Assert.Single(minWidth.Diagnostics, node => node.Level == DiagnosticLevel.Hint).ToString());

        FlutterError minHeight = Assert.Throws<FlutterError>(() => testBox.GetMinIntrinsicHeight(-1));
        Assert.Equal(
            "FlutterError\n"
            + "   The width argument to getMinIntrinsicHeight was negative.\n"
            + "   The argument to getMinIntrinsicHeight must not be negative or\n"
            + "   null.\n"
            + "   If you perform computations on another width before passing it to\n"
            + "   getMinIntrinsicHeight, consider using math.max() or\n"
            + "   double.clamp() to force the value into the valid range.\n",
            minHeight.ToStringDeep());

        FlutterError maxWidth = Assert.Throws<FlutterError>(() => testBox.GetMaxIntrinsicWidth(-1));
        Assert.Equal(
            "FlutterError\n"
            + "   The height argument to getMaxIntrinsicWidth was negative.\n"
            + "   The argument to getMaxIntrinsicWidth must not be negative or\n"
            + "   null.\n"
            + "   If you perform computations on another height before passing it\n"
            + "   to getMaxIntrinsicWidth, consider using math.max() or\n"
            + "   double.clamp() to force the value into the valid range.\n",
            maxWidth.ToStringDeep());

        FlutterError maxHeight = Assert.Throws<FlutterError>(() => testBox.GetMaxIntrinsicHeight(-1));
        Assert.Equal(
            "FlutterError\n"
            + "   The width argument to getMaxIntrinsicHeight was negative.\n"
            + "   The argument to getMaxIntrinsicHeight must not be negative or\n"
            + "   null.\n"
            + "   If you perform computations on another width before passing it to\n"
            + "   getMaxIntrinsicHeight, consider using math.max() or\n"
            + "   double.clamp() to force the value into the valid range.\n",
            maxHeight.ToStringDeep());
    }

    [Fact]
    public void HitTesting_BoxHitTestResultWrappingHitTestResult()
    {
        var entry1 = new HitTestEntry(new DummyHitTestTarget());
        var entry2 = new HitTestEntry(new DummyHitTestTarget());
        var entry3 = new HitTestEntry(new DummyHitTestTarget());
        Matrix4 transform = Matrix4.TranslationValues(40.0, 150.0, 0.0);

        var wrapped = new MyHitTestResult();
        wrapped.PublicPushTransform(transform);
        wrapped.Add(entry1);
        Assert.Equal([entry1], wrapped.Path);
        Assert.Equal(transform.Storage, entry1.Transform!.Storage);

        BoxHitTestResult wrapping = BoxHitTestResult.Wrap(wrapped);
        Assert.Equal([entry1], wrapping.Path);
        Assert.Same(wrapped.Path, wrapping.Path);

        wrapping.Add(entry2);
        Assert.Equal([entry1, entry2], wrapping.Path);
        Assert.Equal([entry1, entry2], wrapped.Path);
        Assert.Equal(transform.Storage, entry2.Transform!.Storage);

        wrapped.Add(entry3);
        Assert.Equal([entry1, entry2, entry3], wrapping.Path);
        Assert.Equal([entry1, entry2, entry3], wrapped.Path);
        Assert.Equal(transform.Storage, entry3.Transform!.Storage);
    }

    [Fact]
    public void HitTesting_AddWithPaintTransform()
    {
        var result = new BoxHitTestResult();
        var positions = new List<Point>();

        bool isHit = result.AddWithPaintTransform(null, default, Record(positions, true));
        Assert.True(isHit);
        Assert.Equal([default], positions);
        positions.Clear();

        isHit = result.AddWithPaintTransform(Matrix4.TranslationValues(20, 30, 0), default, Record(positions, true));
        Assert.True(isHit);
        Assert.Equal([new Point(-20.0, -30.0)], positions);
        positions.Clear();

        isHit = result.AddWithPaintTransform(null, new Point(3, 4), Record(positions, false));
        Assert.False(isHit);
        Assert.Equal([new Point(3.0, 4.0)], positions);
        positions.Clear();

        isHit = result.AddWithPaintTransform(Matrix4.Identity(), new Point(3, 4), Record(positions, true));
        Assert.True(isHit);
        Assert.Equal([new Point(3.0, 4.0)], positions);
        positions.Clear();

        isHit = result.AddWithPaintTransform(
            Matrix4.TranslationValues(20, 30, 0),
            new Point(3, 4),
            Record(positions, true));
        Assert.True(isHit);
        Assert.Equal([new Point(3.0, 4.0) - new Point(20.0, 30.0)], positions);
        positions.Clear();

        isHit = result.AddWithPaintTransform(
            MatrixUtils.ForceToPoint(new Point(3.0, 4.0)),
            new Point(3, 4),
            Record(positions, true));
        Assert.False(isHit);
        Assert.Empty(positions);
    }

    [Fact]
    public void HitTesting_AddWithPaintOffset()
    {
        var result = new BoxHitTestResult();
        var positions = new List<Point>();

        Assert.True(result.AddWithPaintOffset(null, default, Record(positions, true)));
        Assert.Equal([default], positions);
        positions.Clear();

        Assert.True(result.AddWithPaintOffset(new Point(55, 32), default, Record(positions, true)));
        Assert.Equal([new Point(-55.0, -32.0)], positions);
        positions.Clear();

        Assert.False(result.AddWithPaintOffset(null, new Point(3, 4), Record(positions, false)));
        Assert.Equal([new Point(3.0, 4.0)], positions);
        positions.Clear();

        Assert.True(result.AddWithPaintOffset(default(Point), new Point(3, 4), Record(positions, true)));
        Assert.Equal([new Point(3.0, 4.0)], positions);
        positions.Clear();

        Assert.True(result.AddWithPaintOffset(new Point(20, 30), new Point(3, 4), Record(positions, true)));
        Assert.Equal([new Point(3.0, 4.0) - new Point(20.0, 30.0)], positions);
    }

    [Fact]
    public void HitTesting_AddWithRawTransform()
    {
        var result = new BoxHitTestResult();
        var positions = new List<Point>();

        Assert.True(result.AddWithRawTransform(null, default, Record(positions, true)));
        Assert.Equal([default], positions);
        positions.Clear();

        Assert.True(result.AddWithRawTransform(
            Matrix4.TranslationValues(20, 30, 0),
            default,
            Record(positions, true)));
        Assert.Equal([new Point(20.0, 30.0)], positions);
        positions.Clear();

        Assert.False(result.AddWithRawTransform(null, new Point(3, 4), Record(positions, false)));
        Assert.Equal([new Point(3.0, 4.0)], positions);
        positions.Clear();

        Assert.True(result.AddWithRawTransform(Matrix4.Identity(), new Point(3, 4), Record(positions, true)));
        Assert.Equal([new Point(3.0, 4.0)], positions);
        positions.Clear();

        Assert.True(result.AddWithRawTransform(
            Matrix4.TranslationValues(20, 30, 0),
            new Point(3, 4),
            Record(positions, true)));
        Assert.Equal([new Point(3.0, 4.0) + new Point(20.0, 30.0)], positions);
    }

    [Fact]
    public void HitTesting_AddWithOutOfBandPosition()
    {
        var result = new BoxHitTestResult();
        bool ran = false;

        bool isHit = result.AddWithOutOfBandPosition(
            _ =>
            {
                ran = true;
                return true;
            },
            paintOffset: new Point(20, 30));
        Assert.True(isHit);
        Assert.True(ran);
        ran = false;

        isHit = result.AddWithOutOfBandPosition(
            _ =>
            {
                ran = true;
                return true;
            },
            paintTransform: Matrix4.TranslationValues(20, 30, 0));
        Assert.True(isHit);
        Assert.True(ran);
        ran = false;

        isHit = result.AddWithOutOfBandPosition(
            _ =>
            {
                ran = true;
                return true;
            },
            rawTransform: Matrix4.TranslationValues(20, 30, 0));
        Assert.True(isHit);
        Assert.True(ran);
        ran = false;

        // A raw transform is not inverted, so a non-invertible one is fine.
        isHit = result.AddWithOutOfBandPosition(
            _ =>
            {
                ran = true;
                return true;
            },
            rawTransform: MatrixUtils.ForceToPoint(default));
        Assert.True(isHit);
        Assert.True(ran);
        ran = false;

        // The two argument checks below are Dart asserts.
        if (!Constants.KDebugMode)
        {
            return;
        }

        AssertionError notInvertible = Assert.Throws<AssertionError>(() => result.AddWithOutOfBandPosition(
            _ =>
            {
                ran = true;
                return true;
            },
            paintTransform: MatrixUtils.ForceToPoint(default)));
        Assert.Equal("paintTransform must be invertible.", notInvertible.MessageObject);
        Assert.False(ran);

        AssertionError none = Assert.Throws<AssertionError>(() => result.AddWithOutOfBandPosition(
            _ =>
            {
                ran = true;
                return true;
            }));
        Assert.Equal("Exactly one transform or offset argument must be provided.", none.MessageObject);
        Assert.False(ran);
    }

    [DebugOnlyFact]
    public void HitTesting_ErrorMessage_NeverLaidOut()
    {
        var box = new RenderConstrainedBox(BoxConstraints.Unbounded.Tighten(height: 100.0));
        FlutterError result = Assert.Throws<FlutterError>(() => box.HitTest(new BoxHitTestResult(), default));
        Assert.Equal(
            "FlutterError\n"
            + "   Cannot hit test a render box that has never been laid out.\n"
            + "   The hitTest() method was called on this RenderBox: RenderConstrainedBox#00000 NEEDS-LAYOUT "
            + "NEEDS-PAINT DETACHED:\n"
            + "     parentData: MISSING\n"
            + "     constraints: MISSING\n"
            + "     size: MISSING\n"
            + "     additionalConstraints: BoxConstraints(0.0<=w<=Infinity, h=100.0)\n"
            + "   Unfortunately, this object's geometry is not known at this time,\n"
            + "   probably because it has never been laid out. This means it cannot\n"
            + "   be accurately hit-tested.\n"
            + "   If you are trying to perform a hit test during the layout phase\n"
            + "   itself, make sure you only hit test nodes that have completed\n"
            + "   layout (e.g. the node's children, after their layout() method has\n"
            + "   been called).\n",
            FrameworkDartTester.IgnoringHashCodes(result.ToStringDeep()));
        Assert.Equal(
            "If you are trying to perform a hit test during the layout phase itself, make sure you only hit test "
            + "nodes that have completed layout (e.g. the node's children, after their layout() method has been "
            + "called).",
            Assert.Single(result.Diagnostics, node => node.Level == DiagnosticLevel.Hint).ToString());
    }

    [DebugOnlyFact]
    public void HitTesting_ErrorMessage_NoSize()
    {
        var box = new FakeMissingSizeRenderBox();
        new RenderingHarness(box);
        box.FakeMissingSize = true;
        // Dart's `RenderView` is a plain `RenderObject` that gives its child `ParentData` (`<none>`);
        // Plumix's is an approximate `RenderBox` port and gives `BoxParentData` (see PORT_MAP.md).
        FlutterError result = Assert.Throws<FlutterError>(() => box.HitTest(new BoxHitTestResult(), default));
        Assert.Equal(
            "FlutterError\n"
            + "   Cannot hit test a render box with no size.\n"
            + "   The hitTest() method was called on this RenderBox: FakeMissingSizeRenderBox#00000 NEEDS-PAINT:\n"
            + "     parentData: offset=Offset(0.0, 0.0)\n"
            + "     constraints: BoxConstraints(w=800.0, h=600.0)\n"
            + "     size: Size(800.0, 600.0)\n"
            + "   Although this node is not marked as needing layout, its size is\n"
            + "   not set.\n"
            + "   A RenderBox object must have an explicit size before it can be\n"
            + "   hit-tested. Make sure that the RenderBox in question sets its\n"
            + "   size during layout.\n",
            FrameworkDartTester.IgnoringHashCodes(result.ToStringDeep()));
        Assert.Equal(
            "A RenderBox object must have an explicit size before it can be hit-tested. Make sure that the "
            + "RenderBox in question sets its size during layout.",
            Assert.Single(result.Diagnostics, node => node.Level == DiagnosticLevel.Hint).ToString());
    }

    [Fact]
    public void HitTesting_LocalToGlobalWithAncestor()
    {
        var innerConstrained = new RenderConstrainedBox(BoxConstraints.Tight(new Size(50, 50)));
        var innerCenter = new RenderPositionedBox(child: innerConstrained, alignment: Alignment.Center);
        var outerConstrained = new RenderConstrainedBox(BoxConstraints.Tight(new Size(100, 100)), innerCenter);
        var outerCentered = new RenderPositionedBox(child: outerConstrained, alignment: Alignment.Center);

        new RenderingHarness(outerCentered);

        Assert.Equal(25.0, innerConstrained.LocalToGlobal(default, ancestor: outerConstrained).Y);
    }

    [Fact]
    public void ErrorMessageWhenSizeHasNotBeenSetInPerformLayoutShouldBeWellVersed()
    {
        List<FlutterErrorDetails> errors = CollectErrors(
            () => new MissingSetSizeRenderBox().Layout(BoxConstraints.Unbounded));

        if (!Constants.KDebugMode)
        {
            Assert.Empty(errors);
            return;
        }

        FlutterErrorDetails details = Assert.Single(errors);
        // Check the error details without the stack trace.
        string[] lines = details.ToString().Split('\n');
        Assert.Equal(
            "══╡ EXCEPTION CAUGHT BY RENDERING LIBRARY ╞══════════════════════\n"
            + "The following assertion was thrown during performLayout():\n"
            + "RenderBox did not set its size during layout.\n"
            + "Because this RenderBox has sizedByParent set to false, it must\n"
            + "set its size in performLayout().",
            string.Join('\n', lines.Take(5)));
    }

    [Fact]
    public void DebugDoingBaselineFlagIsClearedAfterException()
    {
        var badChild = new BadBaselineRenderBox();
        var badRoot = new RenderBaseline(0.0, TextBaseline.Alphabetic, badChild);
        List<FlutterErrorDetails> errors = CollectErrors(() => new RenderingHarness(badRoot));
        Assert.NotEmpty(errors);

        var goodChild = new RenderDecoratedBox(new BoxDecoration(Color: Color.FromUInt32(0xFF00FF00)));
        var goodRoot = new RenderBaseline(0.0, TextBaseline.Alphabetic, goodChild);
        errors = CollectErrors(() => new RenderingHarness(goodRoot));
        Assert.Empty(errors);
    }

    [Fact]
    public void BaselineOffset_MinOf()
    {
        Assert.Equal(BaselineOffset.NoBaseline, BaselineOffset.NoBaseline.MinOf(BaselineOffset.NoBaseline));
        Assert.Equal(new BaselineOffset(1), BaselineOffset.NoBaseline.MinOf(new BaselineOffset(1)));
        Assert.Equal(new BaselineOffset(1), new BaselineOffset(1).MinOf(BaselineOffset.NoBaseline));
        Assert.Equal(new BaselineOffset(1), new BaselineOffset(2).MinOf(new BaselineOffset(1)));
        Assert.Equal(new BaselineOffset(1), new BaselineOffset(1).MinOf(new BaselineOffset(2)));
    }

    [Fact]
    public void BaselineOffset_Plus()
    {
        Assert.Equal(BaselineOffset.NoBaseline, BaselineOffset.NoBaseline + 2);
        Assert.Equal(new BaselineOffset(3), new BaselineOffset(1) + 2);
    }

    // ---------------------------------------------------------------- beyond box_test.dart

    [DebugOnlyFact]
    public void Size_ReadByTheParentWithoutParentUsesSize_Asserts()
    {
        var child = new RenderConstrainedBox(BoxConstraints.Tight(new Size(10, 10)));
        var parent = new ReadsChildSizeRenderBox(child);
        List<FlutterErrorDetails> errors = CollectErrors(() => new RenderingHarness(parent));

        FlutterErrorDetails details = Assert.Single(errors);
        Assert.Contains(
            "pass \"parentUsesSize: true\" to that child's layout() in RenderConstrainedBox.performLayout.",
            ((Exception)details.Exception!).Message,
            StringComparison.Ordinal);
    }

    [DebugOnlyFact]
    public void DebugAssertDoesMeetConstraints_ReportsAnInfiniteSizeAndTheUnboundedAncestor()
    {
        var infinite = new InfiniteRenderBox();
        var root = new UnboundedWidthRenderBox(infinite);
        List<FlutterErrorDetails> errors = CollectErrors(() => new RenderingHarness(root));

        FlutterError error = Assert.IsType<FlutterError>(errors[0].Exception);
        string deep = FrameworkDartTester.IgnoringHashCodes(error.ToStringDeep(wrapWidth: 400));
        Assert.StartsWith(
            "FlutterError\n   InfiniteRenderBox object was given an infinite size during layout.\n",
            deep);
        Assert.Contains(
            "The nearest ancestor providing an unbounded width constraint is: UnboundedWidthRenderBox#00000",
            deep,
            StringComparison.Ordinal);
        Assert.Contains("The exact size it was given was:\n     Size(Infinity, 600.0)", deep, StringComparison.Ordinal);
        Assert.Contains("See https://flutter.dev/to/unbounded-constraints", deep, StringComparison.Ordinal);
    }

    [DebugOnlyFact]
    public void DefaultComputeDryLayout_AssertsUnlessCheckingIntrinsics()
    {
        var box = new NoDryLayoutRenderBox();
        FlutterError error = Assert.Throws<FlutterError>(() => box.GetDryLayout(BoxConstraints.Unbounded));
        Assert.StartsWith(
            "The NoDryLayoutRenderBox class does not implement \"computeDryLayout\".",
            error.Message);

        box = new NoDryLayoutRenderBox();
        FlutterError baseline = Assert.Throws<FlutterError>(
            () => box.GetDryBaseline(BoxConstraints.Unbounded, TextBaseline.Alphabetic));
        Assert.StartsWith(
            "The NoDryLayoutRenderBox class does not implement \"computeDryBaseline\".",
            baseline.Message);
    }

    [Fact]
    public void MarkNeedsLayout_WithCachedIntrinsics_MarksTheParentInstead()
    {
        // The child gets tight constraints, so it is its own relayout boundary.
        var child = new RenderConstrainedBox(BoxConstraints.Unbounded);
        var parent = new RenderConstrainedBox(BoxConstraints.Tight(new Size(10, 10)), child);
        new RenderingHarness(new RenderPositionedBox(child: parent, alignment: Alignment.Center));

        // No cached data: only the child is dirty.
        child.MarkNeedsLayout();
        Assert.False(parent.DebugNeedsLayout);

        child.GetMinIntrinsicWidth(double.PositiveInfinity);
        child.MarkNeedsLayout();
        if (Constants.KDebugMode)
        {
            Assert.True(parent.DebugNeedsLayout);
        }
    }

    private static BoxHitTest Record(List<Point> positions, bool returnValue) => (_, position) =>
    {
        positions.Add(position);
        return returnValue;
    };

    private static List<FlutterErrorDetails> CollectErrors(Action action)
    {
        var errors = new List<FlutterErrorDetails>();
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = errors.Add;
        try
        {
            action();
        }
        finally
        {
            FlutterError.OnError = previous;
        }

        return errors;
    }

    /// <summary>The <c>layout</c> of Flutter's <c>rendering_tester.dart</c>: an 800x600 render view.</summary>
    private sealed class RenderingHarness
    {
        public RenderingHarness(RenderBox box)
        {
            var view = new RenderView(new FlutterView(new Size(800, 600))) { Child = box };
            var pipeline = new PipelineOwner(view);
            pipeline.Attach(view);
            // No root size: the owner keeps the view's tight 800x600 constraints.
            pipeline.FlushLayout();
        }
    }

    private sealed class MissingPerformLayoutRenderBox : RenderBox
    {
        public void TriggerExceptionSettingSizeOutsideOfLayout() => Size = new Size(200, 200);

        public void CallPerformLayout() => PerformLayout();

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }

    private sealed class FakeMissingSizeRenderBox : RenderBox
    {
        public bool FakeMissingSize { get; set; }

        public override bool HasSize => !FakeMissingSize && base.HasSize;

        protected override void PerformLayout() => Size = Constraints.Biggest;

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }

    private sealed class MissingSetSizeRenderBox : RenderBox
    {
        protected override void PerformLayout()
        {
        }

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }

    private sealed class BadBaselineRenderBox : RenderBox
    {
        protected override void PerformLayout() => Size = Constraints.Biggest;

        protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline) =>
            throw new InvalidOperationException();

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }

    private sealed class InvalidSizeAccessInDryLayoutBox : RenderBox
    {
        protected override Size ComputeDryLayout(BoxConstraints constraints) =>
            constraints.Constrain(HasSize ? Size : new Size(double.PositiveInfinity, double.PositiveInfinity));

        protected override void PerformLayout() => Size = GetDryLayout(Constraints);

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }

    private sealed class BaselineSizeAccessRootRenderBox(RenderBox child) : RenderProxyBox(child)
    {
        protected override void PerformLayout()
        {
            base.PerformLayout();
            Child!.GetDistanceToBaseline(TextBaseline.Alphabetic);
        }
    }

    private sealed class BaselineSizeAccessChildRenderBox(RenderBox child) : RenderProxyBox(child)
    {
        protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline) => Child!.Size.Height;
    }

    private sealed class ReadsChildSizeRenderBox(RenderBox child) : RenderProxyBox(child)
    {
        protected override void PerformLayout()
        {
            Child!.Layout(Constraints);
            Size = Constraints.Constrain(Child.Size);
        }
    }

    /// <summary>Gives its child an unbounded width.</summary>
    private sealed class UnboundedWidthRenderBox(RenderBox child) : RenderProxyBox(child)
    {
        protected override void PerformLayout()
        {
            Child!.Layout(Constraints.CopyWith(minWidth: 0.0, maxWidth: double.PositiveInfinity));
            Size = Constraints.Biggest;
        }
    }

    private sealed class InfiniteRenderBox : RenderBox
    {
        protected override void PerformLayout() =>
            Size = new Size(double.PositiveInfinity, Constraints.MaxHeight);

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }

    private sealed class NoDryLayoutRenderBox : RenderBox
    {
        protected override void PerformLayout() => Size = Constraints.Smallest;

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }

    private sealed class MyHitTestResult : HitTestResult
    {
        public void PublicPushTransform(Matrix4 transform) => PushTransform(transform);
    }

    private sealed class DummyHitTestTarget : IHitTestTarget
    {
        public void HandleEvent(PointerEvent @event, HitTestEntry entry)
        {
            // Nothing to do.
        }
    }
}
