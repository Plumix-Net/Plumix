using Avalonia;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/gestures/hit_test.dart (parity regression tests,
// ported from flutter/packages/flutter/test/gestures/hit_test_test.dart)

namespace Plumix.Tests;

public sealed class HitTestResultTests
{
    [Fact]
    public void WrappedResult_SharesPathAndTransformWithWrapper()
    {
        var entry1 = new HitTestEntry(new DummyHitTestTarget());
        var entry2 = new HitTestEntry(new DummyHitTestTarget());
        var entry3 = new HitTestEntry(new DummyHitTestTarget());
        Matrix4 transform = Matrix4.TranslationValues(40.0, 150.0, 0.0);

        var wrapped = new TestHitTestResult();
        wrapped.PublicPushTransform(transform);
        wrapped.Add(entry1);
        Assert.Equal([entry1], wrapped.Path);
        Assert.Equal(transform, entry1.Transform);

        HitTestResult wrapping = HitTestResult.Wrap(wrapped);
        Assert.Equal([entry1], wrapping.Path);

        wrapping.Add(entry2);
        Assert.Equal([entry1, entry2], wrapping.Path);
        Assert.Equal([entry1, entry2], wrapped.Path);
        Assert.Equal(transform, entry2.Transform);

        wrapped.Add(entry3);
        Assert.Equal([entry1, entry2, entry3], wrapping.Path);
        Assert.Equal([entry1, entry2, entry3], wrapped.Path);
        Assert.Equal(transform, entry3.Transform);
    }

    [Fact]
    public void PushAndPopTransform_ComposesLeftMultiplied()
    {
        var result = new TestHitTestResult();

        Matrix4 m1 = Matrix4.TranslationValues(10, 20, 0);
        Matrix4 m2 = Matrix4.RotationZ(1);
        Matrix4 m3 = Matrix4.Diagonal3Values(1.1, 1.2, 1.0);

        result.PublicPushTransform(m1);
        Assert.Equal(m1, CurrentTransform(result));

        result.PublicPushTransform(m2);
        Assert.Equal(m2.Multiplied(m1), CurrentTransform(result));

        // Reading the transform a second time must not re-globalize the stack.
        Assert.Equal(m2.Multiplied(m1), CurrentTransform(result));

        // The wrapper is wrapped at [m1, m2].
        var wrapped = TestHitTestResult.WrapResult(result);
        Assert.Equal(m2.Multiplied(m1), CurrentTransform(wrapped));

        result.PublicPushTransform(m3);
        Assert.Equal(m3.Multiplied(m2).Multiplied(m1), CurrentTransform(result));
        Assert.Equal(m3.Multiplied(m2).Multiplied(m1), CurrentTransform(wrapped));

        result.PublicPopTransform();
        result.PublicPopTransform();
        Assert.Equal(m1, CurrentTransform(result));

        result.PublicPopTransform();
        result.PublicPushTransform(m3);
        Assert.Equal(m3, CurrentTransform(result));

        result.PublicPushTransform(m2);
        Assert.Equal(m2.Multiplied(m3), CurrentTransform(result));
    }

    [Fact]
    public void PushAndPopOffset_ComposesAsTranslation()
    {
        var result = new TestHitTestResult();

        Matrix4 m1 = Matrix4.RotationZ(1);
        Matrix4 m2 = Matrix4.Diagonal3Values(1.1, 1.2, 1.0);
        var o3 = new Point(10, 20);
        Matrix4 m3 = Matrix4.TranslationValues(o3.X, o3.Y, 0.0);

        // An offset pushed as the first element is the translation itself.
        result.PublicPushOffset(o3);
        Assert.Equal(m3, CurrentTransform(result));
        result.PublicPopTransform();

        result.PublicPushOffset(o3);
        result.PublicPushTransform(m1);
        Assert.Equal(m1.Multiplied(m3), CurrentTransform(result));
        Assert.Equal(m1.Multiplied(m3), CurrentTransform(result));

        var wrapped = TestHitTestResult.WrapResult(result);
        Assert.Equal(m1.Multiplied(m3), CurrentTransform(wrapped));

        result.PublicPushTransform(m2);
        Assert.Equal(m2.Multiplied(m1).Multiplied(m3), CurrentTransform(result));
        Assert.Equal(m2.Multiplied(m1).Multiplied(m3), CurrentTransform(wrapped));

        result.PublicPopTransform();
        result.PublicPopTransform();
        result.PublicPopTransform();
        Assert.Equal(Matrix4.Identity(), CurrentTransform(result));

        result.PublicPushTransform(m2);
        result.PublicPushOffset(o3);
        result.PublicPushTransform(m1);
        Assert.Equal(m1.Multiplied(m3).Multiplied(m2), CurrentTransform(result));

        result.PublicPopTransform();
        Assert.Equal(m3.Multiplied(m2), CurrentTransform(result));
    }

    [Fact]
    public void PushTransform_RejectsAPerspectiveMatrix()
    {
        var result = new TestHitTestResult();
        Matrix4 perspective = Matrix4.Identity();
        perspective.Storage[11] = 0.001;

        Assert.Throws<InvalidOperationException>(() => result.PublicPushTransform(perspective));
    }

    [Fact]
    public void Add_RejectsAnEntryThatAlreadyCarriesATransform()
    {
        var result = new TestHitTestResult();
        var entry = new HitTestEntry(new DummyHitTestTarget());
        result.Add(entry);

        Assert.Throws<InvalidOperationException>(() => new TestHitTestResult().Add(entry));
    }

    [Fact]
    public void AddWithPaintOffset_PushesTheNegatedOffset()
    {
        var target = new DummyHitTestTarget();
        var result = new BoxHitTestResult();

        bool isHit = result.AddWithPaintOffset(
            new Point(10, 20),
            new Point(30, 45),
            (hitResult, transformed) =>
            {
                Assert.Equal(new Point(20, 25), transformed);
                hitResult.Add(new HitTestEntry(target));
                return true;
            });

        Assert.True(isHit);
        Assert.Equal(Matrix4.TranslationValues(-10, -20, 0), result.Path[0].Transform);
    }

    [Fact]
    public void AddWithPaintOffset_LeavesTheStackUntouchedForANullOffset()
    {
        var result = new BoxHitTestResult();

        Assert.True(result.AddWithPaintOffset(
            null,
            new Point(30, 45),
            (hitResult, transformed) =>
            {
                Assert.Equal(new Point(30, 45), transformed);
                hitResult.Add(new HitTestEntry(new DummyHitTestTarget()));
                return true;
            }));

        Assert.Equal(Matrix4.Identity(), result.Path[0].Transform);
    }

    [Fact]
    public void AddWithPaintTransform_PushesTheInvertedTransformAndPopsAfterwards()
    {
        var result = new BoxHitTestResult();
        Matrix4 paintTransform = Matrix4.Diagonal3Values(2.0, 2.0, 1.0);

        Assert.True(result.AddWithPaintTransform(
            paintTransform,
            new Point(30, 40),
            (hitResult, transformed) =>
            {
                Assert.Equal(new Point(15, 20), transformed);
                hitResult.Add(new HitTestEntry(new DummyHitTestTarget()));
                return true;
            }));

        Assert.Equal(Matrix4.Diagonal3Values(0.5, 0.5, 1.0), result.Path[0].Transform);

        // The transform was popped, so a later entry is back at the identity.
        result.Add(new HitTestEntry(new DummyHitTestTarget()));
        Assert.Equal(Matrix4.Identity(), result.Path[1].Transform);
    }

    [Fact]
    public void AddWithPaintTransform_ReturnsFalseForASingularTransform()
    {
        var result = new BoxHitTestResult();
        bool ran = false;

        Assert.False(result.AddWithPaintTransform(
            Matrix4.Diagonal3Values(0.0, 0.0, 1.0),
            new Point(30, 40),
            (_, _) =>
            {
                ran = true;
                return true;
            }));

        Assert.False(ran);
        Assert.Empty(result.Path);
    }

    [Fact]
    public void AddWithOutOfBandPosition_AcceptsExactlyOneTransformArgument()
    {
        var result = new BoxHitTestResult();

        Assert.True(result.AddWithOutOfBandPosition(
            hitResult =>
            {
                hitResult.Add(new HitTestEntry(new DummyHitTestTarget()));
                return true;
            },
            paintOffset: new Point(5, 7)));
        Assert.Equal(Matrix4.TranslationValues(-5, -7, 0), result.Path[0].Transform);

        Assert.Throws<ArgumentException>(() => result.AddWithOutOfBandPosition(
            _ => true,
            paintOffset: new Point(1, 1),
            rawTransform: Matrix4.Identity()));

        Assert.Throws<ArgumentException>(() => result.AddWithOutOfBandPosition(_ => true));
    }

    [Fact]
    public void RenderBoxHitTest_StampsEachEntryWithItsGlobalToLocalTransform()
    {
        var leaf = new FixedHitTestBox(new Size(20, 20));
        var padded = new RenderPadding(new Thickness(10, 10, 0, 0), leaf);
        var transform = new RenderTransform(Matrix4.Diagonal3Values(2.0, 2.0, 1.0), padded);
        PipelineOwner pipeline = BuildPipeline(transform);

        var result = new BoxHitTestResult();
        Assert.True(pipeline.Root.HitTest(result, new Point(24, 26)));

        HitTestEntry leafEntry = result.Path.First(entry => ReferenceEquals(entry.Target, leaf));
        Assert.NotNull(leafEntry.Transform);

        // Global (24, 26) is (12, 13) after the 2x scale and (2, 3) after the padding.
        Assert.Equal(new Point(2, 3), MatrixUtils.TransformPoint(leafEntry.Transform!, new Point(24, 26)));
    }

    [Fact]
    public void GestureBinding_DeliversLocalPositionsThroughTheEntryTransform()
    {
        GestureBinding binding = GestureBinding.Instance;
        binding.ResetForTests();

        Point? localPosition = null;
        Point? localDelta = null;
        var listener = new RenderPointerListener(
            behavior: HitTestBehavior.Opaque,
            onPointerDown: @event => localPosition = @event.LocalPosition,
            onPointerMove: @event =>
            {
                localPosition = @event.LocalPosition;
                localDelta = @event.LocalDelta;
            },
            child: new FixedHitTestBox(new Size(40, 40)));
        var padded = new RenderPadding(new Thickness(10, 20, 0, 0), listener);
        var transform = new RenderTransform(Matrix4.Diagonal3Values(2.0, 2.0, 1.0), padded);
        PipelineOwner pipeline = BuildPipeline(transform);

        binding.HandlePointerEvent(
            pipeline.Root,
            new PointerDownEvent(
                pointer: 7,
                kind: PointerDeviceKind.Touch,
                position: new Point(40, 60),
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow));

        // (40, 60) / 2 = (20, 30); minus the (10, 20) padding = (10, 10).
        Assert.Equal(new Point(10, 10), localPosition);

        binding.HandlePointerEvent(
            pipeline.Root,
            new PointerMoveEvent(
                pointer: 7,
                kind: PointerDeviceKind.Touch,
                position: new Point(60, 60),
                buttons: PointerButtons.Primary,
                down: true,
                timestampUtc: DateTime.UtcNow));

        // The 20 global pixels of travel are 10 local pixels under the 2x scale.
        Assert.Equal(new Point(20, 10), localPosition);
        Assert.Equal(new Point(10, 0), localDelta);

        binding.ResetForTests();
    }

    [Fact]
    public void GestureBinding_DragUnderAScaledAncestorAccumulatesGlobalDistance()
    {
        GestureBinding binding = GestureBinding.Instance;
        binding.ResetForTests();

        var recognizer = new HorizontalDragGestureRecognizer();
        int starts = 0;
        Point? updateGlobalPosition = null;
        Point? updateLocalPosition = null;
        recognizer.OnStart = _ => starts += 1;
        recognizer.OnUpdate = details =>
        {
            updateGlobalPosition = details.GlobalPosition;
            updateLocalPosition = details.LocalPosition;
        };

        var listener = new RenderPointerListener(
            behavior: HitTestBehavior.Opaque,
            onPointerDown: recognizer.AddPointer,
            child: new FixedHitTestBox(new Size(100, 100)));
        var transform = new RenderTransform(Matrix4.Diagonal3Values(2.0, 2.0, 1.0), listener);
        PipelineOwner pipeline = BuildPipeline(transform);

        DateTime start = DateTime.UtcNow;
        binding.HandlePointerEvent(
            pipeline.Root,
            new PointerDownEvent(
                pointer: 9,
                kind: PointerDeviceKind.Touch,
                position: new Point(20, 20),
                buttons: PointerButtons.Primary,
                timestampUtc: start));

        binding.HandlePointerEvent(
            pipeline.Root,
            new PointerMoveEvent(
                pointer: 9,
                kind: PointerDeviceKind.Touch,
                position: new Point(60, 20),
                buttons: PointerButtons.Primary,
                down: true,
                timestampUtc: start.AddMilliseconds(16)));

        // 40 global pixels beat the 18-pixel touch slop even though they are 20 local pixels.
        Assert.Equal(1, starts);
        Assert.Equal(new Point(60, 20), updateGlobalPosition);
        Assert.Equal(new Point(30, 10), updateLocalPosition);

        recognizer.Dispose();
        binding.ResetForTests();
    }

    private static Matrix4? CurrentTransform(HitTestResult result)
    {
        var entry = new HitTestEntry(new DummyHitTestTarget());
        result.Add(entry);
        return entry.Transform;
    }

    private static PipelineOwner BuildPipeline(RenderBox child)
    {
        var root = new RenderView
        {
            Child = child
        };

        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(200, 200));
        return pipeline;
    }

    private sealed class DummyHitTestTarget : IHitTestTarget
    {
        public void HandleEvent(PointerEvent @event, HitTestEntry entry)
        {
        }
    }

    private sealed class TestHitTestResult : HitTestResult
    {
        public TestHitTestResult()
        {
        }

        private TestHitTestResult(HitTestResult result) : base(result)
        {
        }

        public static TestHitTestResult WrapResult(HitTestResult result) => new(result);

        public void PublicPushTransform(Matrix4 transform) => PushTransform(transform);

        public void PublicPushOffset(Point offset) => PushOffset(offset);

        public void PublicPopTransform() => PopTransform();
    }

    private sealed class FixedHitTestBox(Size size) : RenderBox
    {
        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(size);
        }

        protected override bool HitTestSelf(Point position) => true;

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }
}
