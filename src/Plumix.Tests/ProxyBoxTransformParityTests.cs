using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using Transform = Plumix.Widgets.Transform;

// Dart parity source: flutter/packages/flutter/test/rendering/proxy_box_test.dart,
// flutter/packages/flutter/test/rendering/transform_test.dart,
// flutter/packages/flutter/test/widgets/transform_test.dart,
// flutter/packages/flutter/test/widgets/fitted_box_test.dart,
// flutter/packages/flutter/test/widgets/listener_test.dart,
// flutter/packages/flutter/test/widgets/mouse_region_test.dart,
// flutter/packages/flutter/test/widgets/basic_test.dart (FractionalTranslation group)

namespace Plumix.Tests;

/// <summary>
/// Test-only readers for <see cref="RenderTransform"/>, whose matrix Dart exposes through a setter only.
/// </summary>
internal static class RenderTransformTestExtensions
{
    /// <summary>The matrix last assigned through <see cref="RenderTransform.Transform"/>, read from the
    /// `transform matrix` diagnostics property.</summary>
    public static Matrix4 DebugTransformMatrix(this RenderTransform renderTransform)
    {
        var builder = new DiagnosticPropertiesBuilder();
        renderTransform.DebugFillProperties(builder);
        TransformProperty property = builder.Properties
            .OfType<TransformProperty>()
            .Single(node => node.Name == "transform matrix");
        return property.TypedValue!;
    }

    /// <summary>Dart's `_effectiveTransform`, as `applyPaintTransform` applies it to an identity matrix.</summary>
    public static Matrix4 DebugEffectiveTransform(this RenderTransform renderTransform)
    {
        Matrix4 transform = Matrix4.Identity();
        renderTransform.ApplyPaintTransform(renderTransform.Child ?? (RenderObject)renderTransform, transform);
        return transform;
    }
}

[Collection(SchedulerTestCollection.Name)]
public sealed class ProxyBoxTransformParityTests : IDisposable
{
    private static readonly Color Blue = new Color(0xFF0000FF);
    private static readonly Color Cyan = new Color(0xFF00FFFF);
    private static readonly Color Black = new Color(0xFF000000);
    private static readonly Color Red = new Color(0xFFFF0000);

    public ProxyBoxTransformParityTests()
    {
        Scheduler.ResetForTests();
        GestureBinding.Instance.ResetForTests();
    }

    public void Dispose()
    {
        GestureBinding.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    // ---------------------------------------------------------------------------------------------
    // rendering/proxy_box_test.dart
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void RenderFittedBox_HandlesApplyingPaintTransformAndHitTestingWithEmptySize()
    {
        var fittedBox = new ProbeFittedBox(child: new PaintLogBox(new Size(0, 0)));

        LayoutBox(fittedBox);
        Matrix4 transform = Matrix4.Identity();
        fittedBox.ApplyPaintTransform(fittedBox.Child!, transform);
        Assert.Equal(Matrix4.Zero(), transform);

        var hitTestResult = new BoxHitTestResult();
        Assert.False(fittedBox.InvokeHitTestChildren(hitTestResult, new Point(0, 0)));
    }

    [Fact]
    public void RenderFittedBox_DoesNotPaintWithEmptySizes()
    {
        // The RenderFittedBox paints if both its size and its child's size are nonempty.
        var child = new PaintLogBox(new Size(1, 1));
        LayoutBox(new RenderFittedBox(child: child), paint: true);
        Assert.Equal(1, child.PaintCount);

        // The RenderFittedBox should not paint if its child is empty-sized.
        var emptyChild = new PaintLogBox(new Size(0, 0));
        LayoutBox(new RenderFittedBox(child: emptyChild), paint: true);
        Assert.Equal(0, emptyChild.PaintCount);

        // The RenderFittedBox should not paint if it is empty.
        var unpaintedChild = new PaintLogBox(new Size(1, 1));
        LayoutBox(
            new RenderFittedBox(child: unpaintedChild),
            constraints: BoxConstraints.Tight(new Size(0, 0)),
            paint: true);
        Assert.Equal(0, unpaintedChild.PaintCount);
    }

    [Fact]
    public void RenderTransform_ReusesItsLayer()
    {
        TestLayerReuse<TransformLayer>(
            new RenderTransform(
                // Use a 3D transform to force compositing.
                Matrix4.RotationX(0.1),
                child: new RenderRepaintBoundary(child: SizedLeaf(new Size(1.0, 1.0)))));
    }

    [Fact]
    public void RenderFittedBox_ReusesClipRectLayer()
    {
        TestFittedBoxWithClipRectLayer();
    }

    [Fact]
    public void RenderFittedBox_ReusesTransformLayer()
    {
        TestFittedBoxWithTransformLayer();
    }

    [Fact]
    public void RenderFittedBox_SwitchesBetweenClipRectLayerAndTransformLayer_AndReusesThem()
    {
        TestFittedBoxWithClipRectLayer();

        // clip -> transform
        TestFittedBoxWithTransformLayer();
        // transform -> clip
        TestFittedBoxWithClipRectLayer();
    }

    [Fact]
    public void RenderFittedBox_RespectsClipBehavior()
    {
        // Dart records the clip behavior with a `TestClipPaintingContext` that overrides `pushClipRect`;
        // `PaintingContext.PushClipRect` is not virtual here, so a repaint boundary under the clip makes
        // the pushed `ClipRectLayer` observable instead.
        var viewport = new BoxConstraints(MaxHeight: 100.0, MaxWidth: 100.0);
        Clip?[] clips = [null, Clip.None, Clip.HardEdge, Clip.AntiAlias, Clip.AntiAliasWithSaveLayer];
        foreach (Clip? clip in clips)
        {
            RenderBox child = new RenderRepaintBoundary(child: SizedLeaf(new Size(200.0, 200.0)));
            RenderFittedBox box = clip is { } value
                ? new RenderFittedBox(child: child, fit: BoxFit.None, clipBehavior: value)
                : new RenderFittedBox(child: child, fit: BoxFit.None);
            LayoutBox(box, constraints: viewport, paint: true);

            // By default, clipBehavior should be Clip.none
            Clip expected = clip ?? Clip.None;
            Assert.Equal(expected, box.ClipBehavior);
            if (expected == Clip.None)
            {
                Assert.IsNotType<ClipRectLayer>(box.DebugLayer);
            }
            else
            {
                Assert.Equal(expected, Assert.IsType<ClipRectLayer>(box.DebugLayer).ClipBehavior);
            }
        }
    }

    [Fact]
    public void RenderMouseRegion_CanChangePropertiesWhenDetached()
    {
        var renderObject = new RenderMouseRegion();
        renderObject.Opaque = false;
        renderObject.OnEnter = _ => { };
        renderObject.OnExit = _ => { };
        renderObject.OnHover = _ => { };
        // Passes if no error is thrown
    }

    [Fact]
    public void RenderFractionalTranslation_UpdatesItsSemanticsAfterItsTranslationValueIsSet()
    {
        var box = new TestSemanticsUpdateRenderFractionalTranslation(translation: new Point(0.5, 0.5));
        LayoutBox(box, constraints: BoxConstraints.Tight(new Size(200.0, 200.0)));
        int initialCount = box.MarkNeedsSemanticsUpdateCallCount;
        Assert.Equal(1, initialCount);
        box.Translation = new Point(0.4, 0.4);
        Assert.Equal(2, box.MarkNeedsSemanticsUpdateCallCount);
        box.Translation = new Point(0.3, 0.3);
        Assert.Equal(3, box.MarkNeedsSemanticsUpdateCallCount);
    }

    // ---------------------------------------------------------------------------------------------
    // rendering/transform_test.dart
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void RenderTransform_Identity()
    {
        RenderBox inner = SizedLeaf(new Size(100.0, 100.0));
        var sizer = new RenderTransform(Matrix4.Identity(), alignment: Alignment.Center, child: inner);
        LayoutBox(sizer, BoxConstraints.Tight(new Size(100.0, 100.0)), Alignment.TopLeft);

        Assert.Equal(new Point(0, 0), inner.GlobalToLocal(new Point(0, 0)));
        Assert.Equal(new Point(100.0, 100.0), inner.GlobalToLocal(new Point(100.0, 100.0)));
        Assert.Equal(new Point(25.0, 75.0), inner.GlobalToLocal(new Point(25.0, 75.0)));
        Assert.Equal(new Point(50.0, 50.0), inner.GlobalToLocal(new Point(50.0, 50.0)));
        Assert.Equal(new Point(0, 0), inner.LocalToGlobal(new Point(0, 0)));
        Assert.Equal(new Point(100.0, 100.0), inner.LocalToGlobal(new Point(100.0, 100.0)));
        Assert.Equal(new Point(25.0, 75.0), inner.LocalToGlobal(new Point(25.0, 75.0)));
        Assert.Equal(new Point(50.0, 50.0), inner.LocalToGlobal(new Point(50.0, 50.0)));
    }

    [Fact]
    public void RenderTransform_IdentityWithInternalOffset()
    {
        RenderBox inner = SizedLeaf(new Size(80.0, 100.0));
        var sizer = new RenderTransform(
            Matrix4.Identity(),
            alignment: Alignment.Center,
            child: new RenderPadding(EdgeInsets.Only(left: 20.0), child: inner));
        LayoutBox(sizer, BoxConstraints.Tight(new Size(100.0, 100.0)), Alignment.TopLeft);

        Assert.Equal(new Point(-20.0, 0.0), inner.GlobalToLocal(new Point(0, 0)));
        Assert.Equal(new Point(80.0, 100.0), inner.GlobalToLocal(new Point(100.0, 100.0)));
        Assert.Equal(new Point(5.0, 75.0), inner.GlobalToLocal(new Point(25.0, 75.0)));
        Assert.Equal(new Point(30.0, 50.0), inner.GlobalToLocal(new Point(50.0, 50.0)));
        Assert.Equal(new Point(20.0, 0.0), inner.LocalToGlobal(new Point(0, 0)));
        Assert.Equal(new Point(120.0, 100.0), inner.LocalToGlobal(new Point(100.0, 100.0)));
        Assert.Equal(new Point(45.0, 75.0), inner.LocalToGlobal(new Point(25.0, 75.0)));
        Assert.Equal(new Point(70.0, 50.0), inner.LocalToGlobal(new Point(50.0, 50.0)));
    }

    [Fact]
    public void RenderTransform_Translation()
    {
        RenderBox inner = SizedLeaf(new Size(100.0, 100.0));
        var sizer = new RenderTransform(
            Matrix4.TranslationValues(50.0, 200.0, 0.0),
            alignment: Alignment.Center,
            child: inner);
        LayoutBox(sizer, BoxConstraints.Tight(new Size(100.0, 100.0)), Alignment.TopLeft);

        Assert.Equal(new Point(-50.0, -200.0), inner.GlobalToLocal(new Point(0, 0)));
        Assert.Equal(new Point(50.0, -100.0), inner.GlobalToLocal(new Point(100.0, 100.0)));
        Assert.Equal(new Point(-25.0, -125.0), inner.GlobalToLocal(new Point(25.0, 75.0)));
        Assert.Equal(new Point(0.0, -150.0), inner.GlobalToLocal(new Point(50.0, 50.0)));
        Assert.Equal(new Point(50.0, 200.0), inner.LocalToGlobal(new Point(0, 0)));
        Assert.Equal(new Point(150.0, 300.0), inner.LocalToGlobal(new Point(100.0, 100.0)));
        Assert.Equal(new Point(75.0, 275.0), inner.LocalToGlobal(new Point(25.0, 75.0)));
        Assert.Equal(new Point(100.0, 250.0), inner.LocalToGlobal(new Point(50.0, 50.0)));
    }

    [Fact]
    public void RenderTransform_TranslationWithInternalOffset()
    {
        RenderBox inner = SizedLeaf(new Size(80.0, 100.0));
        var sizer = new RenderTransform(
            Matrix4.TranslationValues(50.0, 200.0, 0.0),
            alignment: Alignment.Center,
            child: new RenderPadding(EdgeInsets.Only(left: 20.0), child: inner));
        LayoutBox(sizer, BoxConstraints.Tight(new Size(100.0, 100.0)), Alignment.TopLeft);

        Assert.Equal(new Point(-70.0, -200.0), inner.GlobalToLocal(new Point(0, 0)));
        Assert.Equal(new Point(30.0, -100.0), inner.GlobalToLocal(new Point(100.0, 100.0)));
        Assert.Equal(new Point(-45.0, -125.0), inner.GlobalToLocal(new Point(25.0, 75.0)));
        Assert.Equal(new Point(-20.0, -150.0), inner.GlobalToLocal(new Point(50.0, 50.0)));
        Assert.Equal(new Point(70.0, 200.0), inner.LocalToGlobal(new Point(0, 0)));
        Assert.Equal(new Point(170.0, 300.0), inner.LocalToGlobal(new Point(100.0, 100.0)));
        Assert.Equal(new Point(95.0, 275.0), inner.LocalToGlobal(new Point(25.0, 75.0)));
        Assert.Equal(new Point(120.0, 250.0), inner.LocalToGlobal(new Point(50.0, 50.0)));
    }

    [Fact]
    public void RenderTransform_Rotation()
    {
        RenderBox inner = SizedLeaf(new Size(100.0, 100.0));
        var sizer = new RenderTransform(Matrix4.RotationZ(Math.PI), alignment: Alignment.Center, child: inner);
        LayoutBox(sizer, BoxConstraints.Tight(new Size(100.0, 100.0)), Alignment.TopLeft);

        Assert.Equal(new Point(100.0, 100.0), Round(inner.GlobalToLocal(new Point(0, 0))));
        Assert.Equal(new Point(0, 0), Round(inner.GlobalToLocal(new Point(100.0, 100.0))));
        Assert.Equal(new Point(75.0, 25.0), Round(inner.GlobalToLocal(new Point(25.0, 75.0))));
        Assert.Equal(new Point(50.0, 50.0), Round(inner.GlobalToLocal(new Point(50.0, 50.0))));
        Assert.Equal(new Point(100.0, 100.0), Round(inner.LocalToGlobal(new Point(0, 0))));
        Assert.Equal(new Point(0, 0), Round(inner.LocalToGlobal(new Point(100.0, 100.0))));
        Assert.Equal(new Point(75.0, 25.0), Round(inner.LocalToGlobal(new Point(25.0, 75.0))));
        Assert.Equal(new Point(50.0, 50.0), Round(inner.LocalToGlobal(new Point(50.0, 50.0))));
    }

    [Fact]
    public void RenderTransform_RotationWithInternalOffset()
    {
        RenderBox inner = SizedLeaf(new Size(80.0, 100.0));
        var sizer = new RenderTransform(
            Matrix4.RotationZ(Math.PI),
            alignment: Alignment.Center,
            child: new RenderPadding(EdgeInsets.Only(left: 20.0), child: inner));
        LayoutBox(sizer, BoxConstraints.Tight(new Size(100.0, 100.0)), Alignment.TopLeft);

        Assert.Equal(new Point(80.0, 100.0), Round(inner.GlobalToLocal(new Point(0, 0))));
        Assert.Equal(new Point(-20.0, 0.0), Round(inner.GlobalToLocal(new Point(100.0, 100.0))));
        Assert.Equal(new Point(55.0, 25.0), Round(inner.GlobalToLocal(new Point(25.0, 75.0))));
        Assert.Equal(new Point(30.0, 50.0), Round(inner.GlobalToLocal(new Point(50.0, 50.0))));
        Assert.Equal(new Point(80.0, 100.0), Round(inner.LocalToGlobal(new Point(0, 0))));
        Assert.Equal(new Point(-20.0, 0.0), Round(inner.LocalToGlobal(new Point(100.0, 100.0))));
        Assert.Equal(new Point(55.0, 25.0), Round(inner.LocalToGlobal(new Point(25.0, 75.0))));
        Assert.Equal(new Point(30.0, 50.0), Round(inner.LocalToGlobal(new Point(50.0, 50.0))));
    }

    [Fact]
    public void RenderTransform_Perspective_GlobalToLocal()
    {
        RenderBox inner = SizedLeaf(new Size(100.0, 100.0));
        var sizer = new RenderTransform(
            RotateAroundXAxis(Math.PI * 0.25), // at pi/4, we are about 70 pixels high
            alignment: Alignment.Center,
            child: inner);
        LayoutBox(sizer, BoxConstraints.Tight(new Size(100.0, 100.0)), Alignment.TopLeft);

        Assert.Equal(new Point(25.0, 50.0), Round(inner.GlobalToLocal(new Point(25.0, 50.0))));
        Assert.True(inner.GlobalToLocal(new Point(25.0, 17.0)).Y > 0.0);
        Assert.True(inner.GlobalToLocal(new Point(25.0, 17.0)).Y < 10.0);
        Assert.True(inner.GlobalToLocal(new Point(25.0, 83.0)).Y > 90.0);
        Assert.True(inner.GlobalToLocal(new Point(25.0, 83.0)).Y < 100.0);
        Assert.Equal(
            100 - Round(inner.GlobalToLocal(new Point(25.0, 83.0))).Y,
            Round(inner.GlobalToLocal(new Point(25.0, 17.0))).Y);
    }

    [Fact]
    public void RenderTransform_GlobalToLocalWithParallelViewDirectionReturnsZero()
    {
        RenderBox inner = SizedLeaf(new Size(100.0, 100.0));
        var sizer = new RenderTransform(
            RotateAroundXAxis90Degrees(),
            alignment: Alignment.Center,
            child: inner);
        LayoutBox(sizer, BoxConstraints.Tight(new Size(100.0, 100.0)), Alignment.TopLeft);

        Assert.Equal(new Point(0, 0), inner.GlobalToLocal(new Point(25.0, 50.0)));
    }

    [Fact]
    public void RenderTransform_Perspective_LocalToGlobal()
    {
        RenderBox inner = SizedLeaf(new Size(100.0, 100.0));
        var sizer = new RenderTransform(
            RotateAroundXAxis(Math.PI * 0.4999), // at pi/2, we're seeing the box on its edge,
            alignment: Alignment.Center,
            child: inner);
        LayoutBox(sizer, BoxConstraints.Tight(new Size(100.0, 100.0)), Alignment.TopLeft);

        // the inner widget has a height of about half a pixel at this rotation, so
        // everything should end up around the middle of the outer box.
        Assert.Equal(new Point(25.0, 50.0), inner.LocalToGlobal(new Point(25.0, 50.0)));
        Assert.Equal(new Point(25.0, 50.0), Round(inner.LocalToGlobal(new Point(25.0, 75.0))));
        Assert.Equal(new Point(25.0, 50.0), Round(inner.LocalToGlobal(new Point(25.0, 100.0))));
    }

    // ---------------------------------------------------------------------------------------------
    // widgets/transform_test.dart
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Transform_AlignmentDirectionalAlignment()
    {
        bool didReceiveTap = false;

        Widget BuildFrame(TextDirection textDirection, AlignmentGeometry alignment) => new Directionality(
            textDirection,
            new Stack(
                children:
                [
                    new Positioned(
                        new Container(width: 100.0, height: 100.0, color: Blue),
                        top: 100.0,
                        left: 100.0),
                    new Positioned(
                        SizedBox.Square(
                            dimension: 100.0,
                            child: new Transform(
                                Matrix4.Diagonal3Values(0.5, 0.5, 1.0),
                                alignment: alignment,
                                child: new GestureDetector(
                                    onTap: () => didReceiveTap = true,
                                    child: new Container(color: Cyan)))),
                        top: 100.0,
                        left: 100.0),
                ]));

        using var tester = new FrameworkDartTester();

        tester.PumpWidget(BuildFrame(TextDirection.Ltr, AlignmentDirectional.CenterEnd));
        didReceiveTap = false;
        TapAt(tester, new Point(110.0, 110.0));
        Assert.False(didReceiveTap);
        TapAt(tester, new Point(190.0, 150.0));
        Assert.True(didReceiveTap);

        tester.PumpWidget(BuildFrame(TextDirection.Rtl, AlignmentDirectional.CenterStart));
        didReceiveTap = false;
        TapAt(tester, new Point(110.0, 110.0));
        Assert.False(didReceiveTap);
        TapAt(tester, new Point(190.0, 150.0));
        Assert.True(didReceiveTap);

        tester.PumpWidget(BuildFrame(TextDirection.Ltr, AlignmentDirectional.CenterStart));
        didReceiveTap = false;
        TapAt(tester, new Point(190.0, 150.0));
        Assert.False(didReceiveTap);
        TapAt(tester, new Point(110.0, 150.0));
        Assert.True(didReceiveTap);

        tester.PumpWidget(BuildFrame(TextDirection.Rtl, AlignmentDirectional.CenterEnd));
        didReceiveTap = false;
        TapAt(tester, new Point(190.0, 150.0));
        Assert.False(didReceiveTap);
        TapAt(tester, new Point(110.0, 150.0));
        Assert.True(didReceiveTap);
    }

    [Fact]
    public void Transform_CompositedTransformOffset()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(
            new Center(
                new SizedBox(
                    width: 400.0,
                    height: 300.0,
                    child: new ClipRect(
                        child: new Transform(
                            Matrix4.Diagonal3Values(0.5, 0.5, 1.0),
                            child: new RepaintBoundary(child: new Container(color: new Color(0xFF00FF00))))))));

        // Dart counts two transform layers because its render view's root layer is a TransformLayer;
        // the root layer here is an OffsetLayer.
        TransformLayer layer = Assert.Single(Layers(tester).OfType<TransformLayer>());
        Vector3 translation = layer.Transform.GetTranslation();
        Assert.Equal(100.0, translation.X);
        Assert.Equal(75.0, translation.Y);
        Assert.Equal(0.0, translation.Z);
    }

    [Fact]
    public void Transform_Rotate_PushesTheCenteredRotationLayer()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Transform.Rotate(Math.PI / 2.0, child: new RepaintBoundary(child: new Container())));

        TransformLayer layer = Assert.Single(Layers(tester).OfType<TransformLayer>());
        double[] expected = [0.0, 1.0, 0.0, 0.0, -1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 700.0, -100.0, 0.0, 1.0];
        AssertStorage(expected, layer.Transform.Storage, precision: 10);
    }

    [Fact]
    public void Transform_ApplyPaintTransformOfTransformInPadding()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(
            new Padding(
                EdgeInsets.Only(left: 30.0, top: 20.0, right: 50.0, bottom: 70.0),
                child: new Transform(Matrix4.Diagonal3Values(2.0, 2.0, 2.0), child: new Placeholder())));

        Assert.Equal(new Point(30.0, 20.0), GetTopLeft(tester.ElementOfType<Placeholder>()));
    }

    [Fact]
    public void Transform_Translate_DoesNotInsertATransformLayer()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(
            Transform.Translate(new Point(100.0, 50.0), child: new RepaintBoundary(child: new Container())));

        // This should not cause a transform layer to be inserted.
        Assert.Empty(Layers(tester).OfType<TransformLayer>());
        Assert.Equal(new Point(100.0, 50.0), GetTopLeft(tester.ElementOfType<Container>()));
    }

    [Fact]
    public void Transform_Scale_PushesTheCenteredScaleLayer()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Transform.Scale(scale: 2.0, child: new RepaintBoundary(child: new Container())));

        TransformLayer layer = Assert.Single(Layers(tester).OfType<TransformLayer>());
        double[] expected =
        [
            // These are column-major, not row-major.
            2.0, 0.0, 0.0, 0.0,
            0.0, 2.0, 0.0, 0.0,
            0.0, 0.0, 1.0, 0.0,
            -400.0, -300.0, 0.0, 1.0, // it's 1600x1200, centered in an 800x600 square
        ];
        Assert.Equal(expected, layer.Transform.Storage);
    }

    [Fact]
    public void Transform_Rotate_DoesNotRemoveLayersDueToSingularShortCircuit()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Transform.Rotate(Math.PI / 2, child: new RepaintBoundary(child: new Container())));

        Assert.Equal(3, Layers(tester).Count);
    }

    [Fact]
    public void Transform_Rotate_CreatesNiceRotationMatricesFor0_90_180_270Degrees()
    {
        using var tester = new FrameworkDartTester();

        tester.PumpWidget(Transform.Rotate(Math.PI / 2, child: new RepaintBoundary(child: new Container())));
        Assert.Equal(
            Matrix4.FromList([0.0, 1.0, 0.0, 0.0, -1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 700.0, -100.0, 0.0, 1.0]),
            Assert.IsType<TransformLayer>(Layers(tester)[1]).Transform);

        tester.PumpWidget(Transform.Rotate(Math.PI, child: new RepaintBoundary(child: new Container())));
        Assert.Equal(
            Matrix4.FromList([-1.0, 0.0, 0.0, 0.0, 0.0, -1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 800.0, 600.0, 0.0, 1.0]),
            Assert.IsType<TransformLayer>(Layers(tester)[1]).Transform);

        tester.PumpWidget(Transform.Rotate(3 * Math.PI / 2, child: new RepaintBoundary(child: new Container())));
        Assert.Equal(
            Matrix4.FromList([0.0, -1.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 100.0, 700.0, 0.0, 1.0]),
            Assert.IsType<TransformLayer>(Layers(tester)[1]).Transform);

        tester.PumpWidget(Transform.Rotate(0, child: new RepaintBoundary(child: new Container())));

        // No transform layer created
        Assert.IsType<OffsetLayer>(Layers(tester)[1]);
        Assert.Equal(2, Layers(tester).Count);
    }

    [Fact]
    public void Transform_ScaleWithZero_DoesNotPaintChildLayers()
    {
        using var tester = new FrameworkDartTester();

        tester.PumpWidget(Transform.Scale(scale: 0.0, child: new RepaintBoundary(child: new Container())));
        Assert.Single(Layers(tester)); // root layer

        tester.PumpWidget(Transform.Scale(scaleX: 0.0, child: new RepaintBoundary(child: new Container())));
        Assert.Single(Layers(tester));

        tester.PumpWidget(Transform.Scale(scaleY: 0.0, child: new RepaintBoundary(child: new Container())));
        Assert.Single(Layers(tester));

        tester.PumpWidget(
            Transform.Scale(
                scale: 0.01, // small but non-zero
                child: new RepaintBoundary(child: new Container())));
        Assert.Equal(3, Layers(tester).Count);
    }

    [Fact(Skip = "Parity gap: RenderView inherits RenderBox.HitTest's size check, but Dart's RenderView.hitTest "
        + "has none, so a tap outside the 800x600 view never reaches the translated child.")]
    public void Transform_TranslatedChildIntoTranslatedBox_HitTest()
    {
        var key1 = new UniqueKey();
        bool pointerDown = false;
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(
            Transform.Translate(
                new Point(100.0, 50.0),
                child: Transform.Translate(
                    new Point(1000.0, 1000.0),
                    child: new Listener(
                        onPointerDown: _ => pointerDown = true,
                        child: new Container(key: key1, color: Black)))));

        Assert.False(pointerDown);
        tester.Tap(tester.ElementsWithKey(key1).Single());
        Assert.True(pointerDown);
    }

    [Fact]
    public void Transform_TranslateWithFilterQuality_ProducesFilterLayer()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(
            Transform.Translate(
                new Point(25.0, 25.0),
                filterQuality: FilterQuality.Low,
                child: new SizedBox(width: 100, height: 100)));

        ImageFilterLayer layer = Assert.Single(Layers(tester).OfType<ImageFilterLayer>());
        double[] expected = [1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 25.0, 25.0, 0.0, 1.0];
        Assert.Equal(expected, ExtractMatrix(layer.ImageFilter));
    }

    [Fact]
    public void Transform_ScaleWithFilterQuality_ProducesFilterLayer()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(
            Transform.Scale(
                scale: 3.14159,
                filterQuality: FilterQuality.Low,
                child: new SizedBox(width: 100, height: 100)));

        ImageFilterLayer layer = Assert.Single(Layers(tester).OfType<ImageFilterLayer>());
        double[] expected =
            [3.14159, 0.0, 0.0, 0.0, 0.0, 3.14159, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, -856.636, -642.477, 0.0, 1.0];
        // Dart compares the values the engine's `ImageFilter.toString` prints.
        AssertStorage(expected, ExtractMatrix(layer.ImageFilter), precision: 3);
    }

    [Fact]
    public void Transform_RotateWithFilterQuality_ProducesFilterLayer()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(
            Transform.Rotate(
                Math.PI / 4,
                filterQuality: FilterQuality.Low,
                child: new SizedBox(width: 100, height: 100)));

        ImageFilterLayer layer = Assert.Single(Layers(tester).OfType<ImageFilterLayer>());
        AssertStorage(RotatedFilterMatrix, ExtractMatrix(layer.ImageFilter), precision: 10);
    }

    [Fact]
    public void OffsetTransform_RotateWithFilterQuality_ProducesFilterLayer()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(
            SizedBox.Square(
                dimension: 400,
                child: new Center(
                    Transform.Rotate(
                        Math.PI / 4,
                        filterQuality: FilterQuality.Low,
                        child: SizedBox.Square(dimension: 100)))));

        ImageFilterLayer layer = Assert.Single(Layers(tester).OfType<ImageFilterLayer>());
        AssertStorage(RotatedFilterMatrix, ExtractMatrix(layer.ImageFilter), precision: 10);
    }

    [Fact]
    public void Transform_LayersUpdateToMatchChildAndFilterQuality()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(
            Transform.Rotate(
                Math.PI / 4,
                filterQuality: FilterQuality.Low,
                child: new SizedBox(width: 100, height: 100)));
        Assert.Single(Layers(tester).OfType<ImageFilterLayer>());

        tester.PumpWidget(Transform.Rotate(Math.PI / 4, child: new SizedBox(width: 100, height: 100)));
        Assert.Empty(Layers(tester).OfType<ImageFilterLayer>());

        tester.PumpWidget(Transform.Rotate(Math.PI / 4, filterQuality: FilterQuality.Low));
        Assert.Empty(Layers(tester).OfType<ImageFilterLayer>());

        tester.PumpWidget(
            Transform.Rotate(
                Math.PI / 4,
                filterQuality: FilterQuality.Low,
                child: new SizedBox(width: 100, height: 100)));
        Assert.Single(Layers(tester).OfType<ImageFilterLayer>());
    }

    [Fact]
    public void Transform_DoesNotCrashAtZeroArea()
    {
        using var tester = new FrameworkDartTester();
        tester.View.UpdateMetrics(physicalSize: new Size(0, 0));
        RendererBinding.Instance.HandleMetricsChanged();
        tester.PumpWidget(
            new Directionality(
                TextDirection.Ltr,
                new Center(Transform.Flip(flipY: true, child: new Placeholder()))));

        Assert.Equal(new Size(0, 0), GetSize(tester.ElementOfType<Transform>()));
    }

    // ---------------------------------------------------------------------------------------------
    // widgets/fitted_box_test.dart
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void FittedBox_CanSizeAccordingToAspectRatio()
    {
        var outside = new UniqueKey();
        var inside = new UniqueKey();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(
            new Center(
                new SizedBox(
                    width: 200.0,
                    child: new FittedBox(
                        key: outside,
                        child: new SizedBox(key: inside, width: 100.0, height: 50.0)))));

        RenderBox outsideBox = FirstRenderBox(tester, outside);
        Assert.Equal(200.0, outsideBox.Size.Width);
        Assert.Equal(100.0, outsideBox.Size.Height);

        RenderBox insideBox = FirstRenderBox(tester, inside);
        Assert.Equal(100.0, insideBox.Size.Width);
        Assert.Equal(50.0, insideBox.Size.Height);

        Point insidePoint = insideBox.LocalToGlobal(new Point(100.0, 50.0));
        Point outsidePoint = outsideBox.LocalToGlobal(new Point(200.0, 100.0));

        Assert.Equal(new Point(500.0, 350.0), outsidePoint);
        Assert.Equal(outsidePoint, insidePoint);
    }

    [Fact]
    public void FittedBox_ChildCanCover()
    {
        var outside = new UniqueKey();
        var inside = new UniqueKey();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(
            new Center(
                SizedBox.Square(
                    dimension: 200.0,
                    child: new FittedBox(
                        key: outside,
                        fit: BoxFit.Cover,
                        child: new SizedBox(key: inside, width: 100.0, height: 50.0)))));

        RenderBox outsideBox = FirstRenderBox(tester, outside);
        Assert.Equal(200.0, outsideBox.Size.Width);
        Assert.Equal(200.0, outsideBox.Size.Height);

        RenderBox insideBox = FirstRenderBox(tester, inside);
        Assert.Equal(100.0, insideBox.Size.Width);
        Assert.Equal(50.0, insideBox.Size.Height);

        Point insidePoint = insideBox.LocalToGlobal(new Point(50.0, 25.0));
        Point outsidePoint = outsideBox.LocalToGlobal(new Point(100.0, 100.0));

        Assert.Equal(outsidePoint, insidePoint);
    }

    [Fact]
    public void FittedBox_WithNoChild()
    {
        var key = new UniqueKey();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Center(new FittedBox(key: key, fit: BoxFit.Cover)));

        RenderBox box = FirstRenderBox(tester, key);
        Assert.Equal(0.0, box.Size.Width);
        Assert.Equal(0.0, box.Size.Height);
    }

    [Fact]
    public void FittedBox_ChildCanBeAlignedMultipleWaysInARow()
    {
        var outside = new UniqueKey();
        var inside = new UniqueKey();
        using var tester = new FrameworkDartTester();

        void Check(
            TextDirection direction,
            BoxFit fit,
            AlignmentGeometry alignment,
            Size childSize,
            Point outsideTopLeft,
            Point outsideBottomRight)
        {
            tester.PumpWidget(
                new Directionality(
                    direction,
                    new Center(
                        SizedBox.Square(
                            dimension: 100.0,
                            child: new FittedBox(
                                key: outside,
                                fit: fit,
                                alignment: alignment,
                                child: new SizedBox(
                                    key: inside,
                                    width: childSize.Width,
                                    height: childSize.Height))))));

            RenderBox outsideBox = FirstRenderBox(tester, outside);
            Assert.Equal(100.0, outsideBox.Size.Width);
            Assert.Equal(100.0, outsideBox.Size.Height);

            RenderBox insideBox = FirstRenderBox(tester, inside);
            Assert.Equal(childSize, insideBox.Size);

            Assert.Equal(outsideBox.LocalToGlobal(outsideTopLeft), insideBox.LocalToGlobal(new Point(0, 0)));
            Assert.Equal(
                outsideBox.LocalToGlobal(outsideBottomRight),
                insideBox.LocalToGlobal(new Point(childSize.Width, childSize.Height)));
        }

        // align RTL
        Check(
            TextDirection.Rtl,
            BoxFit.ScaleDown,
            AlignmentDirectional.BottomEnd,
            new Size(10.0, 10.0),
            new Point(0.0, 90.0),
            new Point(10.0, 100.0));

        // change direction
        Check(
            TextDirection.Ltr,
            BoxFit.ScaleDown,
            AlignmentDirectional.BottomEnd,
            new Size(10.0, 10.0),
            new Point(90.0, 90.0),
            new Point(100.0, 100.0));

        // change alignment
        Check(
            TextDirection.Ltr,
            BoxFit.ScaleDown,
            AlignmentDirectional.Center,
            new Size(10.0, 10.0),
            new Point(45.0, 45.0),
            new Point(55.0, 55.0));

        // change size
        Check(
            TextDirection.Ltr,
            BoxFit.ScaleDown,
            AlignmentDirectional.Center,
            new Size(30.0, 10.0),
            new Point(35.0, 45.0),
            new Point(65.0, 55.0));

        // change fit
        Check(
            TextDirection.Ltr,
            BoxFit.Fill,
            AlignmentDirectional.Center,
            new Size(30.0, 10.0),
            new Point(0, 0),
            new Point(100.0, 100.0));
    }

    [Fact]
    public void FittedBox_Layers_Contain()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(
            new Center(
                new SizedBox(
                    width: 100.0,
                    height: 10.0,
                    child: new FittedBox(
                        child: SizedBox.Square(
                            dimension: 50.0,
                            child: new RepaintBoundary(child: new Placeholder()))))));

        // Dart's first entry is the render view's TransformLayer; the root layer here is an OffsetLayer.
        Assert.Equal([typeof(OffsetLayer), typeof(TransformLayer), typeof(OffsetLayer)], GetLayers(tester));
    }

    [Fact]
    public void FittedBox_Layers_Cover_Horizontal()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(
            new Center(
                new SizedBox(
                    width: 100.0,
                    height: 10.0,
                    child: new FittedBox(
                        fit: BoxFit.Cover,
                        clipBehavior: Clip.HardEdge,
                        child: new SizedBox(
                            width: 10.0,
                            height: 50.0,
                            child: new RepaintBoundary(child: new Placeholder()))))));

        Assert.Equal(
            [typeof(OffsetLayer), typeof(ClipRectLayer), typeof(TransformLayer), typeof(OffsetLayer)],
            GetLayers(tester));
    }

    [Fact]
    public void FittedBox_Layers_Cover_Vertical()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(
            new Center(
                new SizedBox(
                    width: 10.0,
                    height: 100.0,
                    child: new FittedBox(
                        fit: BoxFit.Cover,
                        clipBehavior: Clip.HardEdge,
                        child: new SizedBox(
                            width: 50.0,
                            height: 10.0,
                            child: new RepaintBoundary(child: new Placeholder()))))));

        Assert.Equal(
            [typeof(OffsetLayer), typeof(ClipRectLayer), typeof(TransformLayer), typeof(OffsetLayer)],
            GetLayers(tester));
    }

    [Fact]
    public void FittedBox_Layers_None_Clip()
    {
        double[] values = [10.0, 50.0, 100.0];
        using var tester = new FrameworkDartTester();
        foreach (double a in values)
        {
            foreach (double b in values)
            {
                foreach (double c in values)
                {
                    foreach (double d in values)
                    {
                        tester.PumpWidget(
                            new Center(
                                new SizedBox(
                                    width: a,
                                    height: b,
                                    child: new FittedBox(
                                        fit: BoxFit.None,
                                        clipBehavior: Clip.HardEdge,
                                        child: new SizedBox(
                                            width: c,
                                            height: d,
                                            child: new RepaintBoundary(child: new Placeholder()))))));
                        if (a < c || b < d)
                        {
                            Assert.Equal(
                                [typeof(OffsetLayer), typeof(ClipRectLayer), typeof(OffsetLayer)],
                                GetLayers(tester));
                        }
                        else
                        {
                            Assert.Equal([typeof(OffsetLayer), typeof(OffsetLayer)], GetLayers(tester));
                        }
                    }
                }
            }
        }
    }

    [Fact]
    public void FittedBox_BigChildIntoSmallFittedBox_HitTesting()
    {
        var key1 = new UniqueKey();
        bool pointerDown = false;
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(
            new Center(
                SizedBox.Square(
                    dimension: 100.0,
                    child: new FittedBox(
                        alignment: Alignment.Center,
                        child: SizedBox.Square(
                            dimension: 1000.0,
                            child: new Listener(
                                onPointerDown: _ => pointerDown = true,
                                child: new Container(key: key1, color: Black)))))));

        Assert.False(pointerDown);
        tester.Tap(tester.ElementsWithKey(key1).Single());
        Assert.True(pointerDown);
    }

    [Fact]
    public void FittedBox_CanSetAndUpdateClipBehavior()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new FittedBox(fit: BoxFit.None, child: new Container()));
        RenderFittedBox renderObject = FindRenderObjects<RenderFittedBox>(tester.RenderView).First();
        Assert.Equal(Clip.None, renderObject.ClipBehavior);

        tester.PumpWidget(new FittedBox(fit: BoxFit.None, clipBehavior: Clip.AntiAlias, child: new Container()));
        Assert.Equal(Clip.AntiAlias, renderObject.ClipBehavior);
    }

    [Fact]
    public void FittedBox_ScaleDownMatchesSizeOfChild()
    {
        var outside = new UniqueKey();
        var inside = new UniqueKey();
        using var tester = new FrameworkDartTester();

        // Does not scale up when child is smaller than constraints
        tester.PumpWidget(
            new Center(
                new SizedBox(
                    width: 200.0,
                    child: new FittedBox(
                        key: outside,
                        fit: BoxFit.ScaleDown,
                        child: new SizedBox(key: inside, width: 100.0, height: 50.0)))));

        RenderBox outsideBox = FirstRenderBox(tester, outside);
        RenderBox insideBox = FirstRenderBox(tester, inside);

        Assert.Equal(200.0, outsideBox.Size.Width);
        Assert.Equal(50.0, outsideBox.Size.Height);

        Point outsidePoint = outsideBox.LocalToGlobal(new Point(0, 0));
        Point insidePoint = insideBox.LocalToGlobal(new Point(0, 0));
        Assert.Equal(new Point(50.0, 0.0), insidePoint - (Vector)outsidePoint);

        // Scales down when child is bigger than constraints
        tester.PumpWidget(
            new Center(
                new SizedBox(
                    width: 200.0,
                    child: new FittedBox(
                        key: outside,
                        fit: BoxFit.ScaleDown,
                        child: new SizedBox(key: inside, width: 400.0, height: 200.0)))));

        Assert.Equal(200.0, outsideBox.Size.Width);
        Assert.Equal(100.0, outsideBox.Size.Height);

        outsidePoint = outsideBox.LocalToGlobal(new Point(0, 0));
        insidePoint = insideBox.LocalToGlobal(new Point(0, 0));

        Assert.Equal(new Point(0, 0), insidePoint - (Vector)outsidePoint);
    }

    [Fact]
    public void FittedBox_WithoutChildDoesNotThrow()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Center(new SizedBox(width: 200.0, height: 200.0, child: new FittedBox())));

        Element fittedBox = Assert.Single(tester.ElementsOfType<FittedBox>());

        // Tapping it also should not throw.
        tester.Tap(fittedBox);
        Assert.Null(tester.TakeException());
    }

    [Fact]
    public void FittedBox_WithZeroSizeChildDoesNotThrow()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(
            new Center(
                SizedBox.Square(
                    dimension: 200.0,
                    child: new FittedBox(fit: BoxFit.ScaleDown, child: SizedBox.Shrink()))));
        Assert.Null(tester.TakeException());

        tester.PumpWidget(
            new Center(
                new ConstrainedBox(
                    constraints: new BoxConstraints(MaxWidth: 200.0, MaxHeight: 200.0),
                    child: new FittedBox(child: SizedBox.Shrink()))));
        Assert.Null(tester.TakeException());
    }

    [Fact]
    public void FittedBox_DoesNotCrashAtZeroArea()
    {
        using var tester = new FrameworkDartTester();
        tester.View.UpdateMetrics(physicalSize: new Size(0, 0));
        RendererBinding.Instance.HandleMetricsChanged();
        tester.PumpWidget(
            new Directionality(TextDirection.Ltr, new Center(new FittedBox(child: new Placeholder()))));

        Assert.Equal(new Size(0, 0), GetSize(tester.ElementOfType<FittedBox>()));
    }

    // ---------------------------------------------------------------------------------------------
    // widgets/basic_test.dart, FractionalTranslation group
    // ---------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(0.0, 0.0)] // hit test - entirely inside the bounding box
    [InlineData(0.5, 0.5)] // hit test - partially inside the bounding box
    [InlineData(1.0, 1.0)] // hit test - completely outside the bounding box
    public void FractionalTranslation_HitTest(double dx, double dy)
    {
        var key1 = new UniqueKey();
        bool pointerDown = false;
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(
            new Center(
                new FractionalTranslation(
                    translation: new Point(dx, dy),
                    child: new Listener(
                        onPointerDown: _ => pointerDown = true,
                        child: new SizedBox(
                            key: key1,
                            width: 100.0,
                            height: 100.0,
                            child: new Container(color: Blue))))));

        Assert.False(pointerDown);
        tester.Tap(tester.ElementsWithKey(key1).Single());
        Assert.True(pointerDown);
    }

    [Fact]
    public void FractionalTranslation_DoesNotCrashAtZeroArea()
    {
        using var tester = new FrameworkDartTester();
        tester.View.UpdateMetrics(physicalSize: new Size(0, 0));
        RendererBinding.Instance.HandleMetricsChanged();
        tester.PumpWidget(
            new Directionality(
                TextDirection.Ltr,
                new Center(new FractionalTranslation(translation: new Point(0, 0), child: new Placeholder()))));

        Assert.Equal(new Size(0, 0), GetSize(tester.ElementOfType<FractionalTranslation>()));
    }

    // ---------------------------------------------------------------------------------------------
    // widgets/listener_test.dart
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Listener_EventsBubbleUpTheTree()
    {
        var log = new List<string>();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(
            new Listener(
                onPointerDown: _ => log.Add("top"),
                child: new Listener(
                    onPointerDown: _ => log.Add("middle"),
                    child: new DecoratedBox(
                        decoration: new BoxDecoration(),
                        child: new Listener(
                            onPointerDown: _ => log.Add("bottom"),
                            child: new Directionality(TextDirection.Ltr, new Text("X")))))));

        tester.Tap(tester.ElementsWithText("X").Single());

        Assert.Equal(["bottom", "middle", "top"], log);
    }

    [Fact]
    public void Listener_DetectsHoverEventsFromTouchDevices()
    {
        var log = new List<string>();
        var listenerKey = new UniqueKey();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(
            new Center(
                SizedBox.Square(
                    dimension: 300.0,
                    child: new Listener(
                        key: listenerKey,
                        onPointerHover: _ => log.Add("bottom"),
                        child: new Directionality(TextDirection.Ltr, new Text("X"))))));

        const int device = 7101;
        Point center = tester.GetCenter(tester.ElementsWithKey(listenerKey).Single());
        Send(
            tester,
            new PointerAddedEvent(device, PointerDeviceKind.Touch, new Point(0, 0), timestampUtc: DateTime.UtcNow));
        Send(
            tester,
            new PointerHoverEvent(device, PointerDeviceKind.Touch, center, PointerButtons.None, DateTime.UtcNow));

        Assert.Equal(["bottom"], log);
    }

    [Fact]
    public void Listener_TransformedEvents_SimpleOffsetForTouchAndSignal()
    {
        var key = new UniqueKey();
        var events = new List<PointerEvent>();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Center(RecordingListener(events, key)));

        Point center = tester.GetCenter(tester.ElementsWithKey(key).Single());
        Point topLeft = GetTopLeft(tester.ElementsWithKey(key).Single());
        Matrix4 expectedTransform = Matrix4.TranslationValues(-topLeft.X, -topLeft.Y, 0);

        Assert.NotEqual(new Point(50, 50), center);
        CheckTransformedGesture(
            tester,
            events,
            center,
            expectedTransform,
            localDownPosition: new Point(50, 50),
            localDelta: new Point(20, 30));
    }

    [Fact]
    public void Listener_TransformedEvents_ScaledForTouchAndSignal()
    {
        const double scaleFactor = 2;
        var key = new UniqueKey();
        var events = new List<PointerEvent>();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(
            new Align(
                alignment: Alignment.TopLeft,
                child: new Transform(
                    Matrix4.Diagonal3Values(scaleFactor, scaleFactor, scaleFactor),
                    child: RecordingListener(events, key))));

        Point center = tester.GetCenter(tester.ElementsWithKey(key).Single());
        Matrix4 expectedTransform = Matrix4.Diagonal3Values(1 / scaleFactor, 1 / scaleFactor, 1.0);

        Assert.NotEqual(new Point(50, 50), center);
        CheckTransformedGesture(
            tester,
            events,
            center,
            expectedTransform,
            localDownPosition: new Point(50, 50),
            localDelta: new Point(20 / scaleFactor, 30 / scaleFactor));
    }

    [Fact]
    public void Listener_TransformedEvents_ScaledAndOffsetForTouchAndSignal()
    {
        const double scaleFactor = 2;
        var key = new UniqueKey();
        var events = new List<PointerEvent>();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(
            new Center(
                new Transform(
                    Matrix4.Diagonal3Values(scaleFactor, scaleFactor, scaleFactor),
                    child: RecordingListener(events, key))));

        Point center = tester.GetCenter(tester.ElementsWithKey(key).Single());
        Point topLeft = GetTopLeft(tester.ElementsWithKey(key).Single());
        Matrix4 expectedTransform = Matrix4.Diagonal3Values(1 / scaleFactor, 1 / scaleFactor, 1.0);
        expectedTransform.TranslateByDouble(-topLeft.X, -topLeft.Y, 0, 1);

        Assert.NotEqual(new Point(50, 50), center);
        CheckTransformedGesture(
            tester,
            events,
            center,
            expectedTransform,
            localDownPosition: new Point(50, 50),
            localDelta: new Point(20 / scaleFactor, 30 / scaleFactor));
    }

    [Fact]
    public void Listener_TransformedEvents_RotatedForTouchAndSignal()
    {
        var key = new UniqueKey();
        var events = new List<PointerEvent>();
        using var tester = new FrameworkDartTester();
        Matrix4 rotation = Matrix4.Identity();
        rotation.RotateZ(Math.PI / 2); // 90 degrees clockwise around Container origin
        tester.PumpWidget(new Center(new Transform(rotation, child: RecordingListener(events, key))));

        Point downPosition = tester.GetCenter(tester.ElementsWithKey(key).Single()) + new Vector(10, 5);
        var offset = new Point((800 - 100) / 2.0, (600 - 100) / 2.0);
        Matrix4 expectedTransform = Matrix4.Identity();
        expectedTransform.RotateZ(-Math.PI / 2);
        expectedTransform.TranslateByDouble(-offset.X, -offset.Y, 0, 1);

        CheckTransformedGesture(
            tester,
            events,
            downPosition,
            expectedTransform,
            localDownPosition: new Point(50 + 5, 50 - 10),
            localDelta: new Point(30, -20));
    }

    [DebugOnlyFact]
    public void RenderPointerListener_DebugFillProperties_WhenDefault()
    {
        var builder = new DiagnosticPropertiesBuilder();
        var renderListener = new RenderPointerListener();

        renderListener.DebugFillProperties(builder);

        Assert.Equal(
            ["parentData: MISSING", "constraints: MISSING", "size: MISSING", "behavior: deferToChild",
                "listeners: <none>"],
            Describe(builder));
    }

    [DebugOnlyFact]
    public void RenderPointerListener_DebugFillProperties_WhenFull()
    {
        var builder = new DiagnosticPropertiesBuilder();
        var renderListener = new RenderPointerListener(
            onPointerDown: _ => { },
            onPointerUp: _ => { },
            onPointerMove: _ => { },
            onPointerHover: _ => { },
            onPointerCancel: _ => { },
            onPointerSignal: _ => { },
            behavior: HitTestBehavior.Opaque,
            child: SizedLeaf(new Size(1, 1)));

        renderListener.DebugFillProperties(builder);

        Assert.Equal(
            ["parentData: MISSING", "constraints: MISSING", "size: MISSING", "behavior: opaque",
                "listeners: down, move, up, hover, cancel, signal"],
            Describe(builder));
    }

    // ---------------------------------------------------------------------------------------------
    // widgets/mouse_region_test.dart
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void MouseRegion_DetectsHoverFromTouchDevices()
    {
        PointerEnterEvent? enter = null;
        PointerHoverEvent? move = null;
        PointerExitEvent? exit = null;
        using var harness = new MouseTrackingHarness(
            new Center(
                new MouseRegion(
                    child: new Container(color: Red, width: 100.0, height: 100.0),
                    onEnter: details => enter = details,
                    onHover: details => move = details,
                    onExit: details => exit = details)),
            new Size(800, 600));

        harness.SendPointer(MouseTrackingHarness.Added(new Point(0, 0), kind: PointerDeviceKind.Touch));
        harness.PumpFrame();
        move = null;
        enter = null;
        exit = null;
        harness.SendPointer(MouseTrackingHarness.Hover(new Point(400.0, 300.0), kind: PointerDeviceKind.Touch));
        Assert.NotNull(move);
        Assert.Equal(new Point(400.0, 300.0), move.Position);
        Assert.Equal(new Point(50.0, 50.0), move.LocalPosition);
        Assert.Null(enter);
        Assert.Null(exit);
    }

    [Fact]
    public void MouseRegion_AnEmptyOpaqueMouseRegionIsEffective()
    {
        bool bottomRegionIsHovered = false;
        using var harness = new MouseTrackingHarness(
            new Stack(
                children:
                [
                    new Align(
                        alignment: Alignment.TopLeft,
                        child: new MouseRegion(
                            onEnter: _ => bottomRegionIsHovered = true,
                            onHover: _ => bottomRegionIsHovered = true,
                            onExit: _ => bottomRegionIsHovered = true,
                            child: new SizedBox(width: 10, height: 10))),
                    new MouseRegion(),
                ]),
            new Size(800, 600));

        harness.SendPointer(MouseTrackingHarness.Added(new Point(20, 20)));

        harness.SendPointer(MouseTrackingHarness.Hover(new Point(5, 5)));
        harness.PumpFrame();
        harness.SendPointer(MouseTrackingHarness.Hover(new Point(20, 20)));
        harness.PumpFrame();
        Assert.False(bottomRegionIsHovered);
    }

    [Fact]
    public void MouseRegion_PaintsChildOnceAndOnlyOnceWhenMouseRegionIsInactive()
    {
        int paintCount = 0;
        using var harness = new MouseTrackingHarness(
            new MouseRegion(
                onEnter: _ => { },
                child: new CustomPaint(
                    painter: new DelegatedPainter(() => paintCount += 1),
                    child: new Text("123"))));

        Assert.Equal(1, paintCount);
    }

    [Fact]
    public void MouseRegion_PaintsChildOnceAndOnlyOnceWhenMouseRegionIsActive()
    {
        int paintCount = 0;
        using var harness = new MouseTrackingHarness(new SizedBox());
        harness.SendPointer(MouseTrackingHarness.Added(new Point(0, 0)));

        harness.Update(
            new MouseRegion(
                onEnter: _ => { },
                child: new CustomPaint(
                    painter: new DelegatedPainter(() => paintCount += 1),
                    child: new Text("123"))));

        Assert.Equal(1, paintCount);
    }

    [Fact]
    public void MouseRegion_NoNewFramesAreScheduledWhenMouseMovesWithoutTriggeringCallbacks()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(
            new Center(
                new MouseRegion(
                    child: new SizedBox(width: 100.0, height: 100.0),
                    onEnter: _ => { },
                    onHover: _ => { },
                    onExit: _ => { })));
        int viewId = tester.View.ViewId;
        Send(tester, MouseTrackingHarness.Added(new Point(400.0, 300.0), device: 7201, viewId: viewId));
        tester.PumpAndSettle();
        Send(tester, MouseTrackingHarness.Hover(new Point(410.0, 310.0), device: 7201, viewId: viewId));
        Assert.False(Scheduler.HasScheduledFrame);
    }

    // ---------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------

    private static readonly double[] RotatedFilterMatrix =
    [
        0.7071067811865476, 0.7071067811865475, 0.0, 0.0,
        -0.7071067811865475, 0.7071067811865476, 0.0, 0.0,
        0.0, 0.0, 1.0, 0.0,
        329.28932188134524, -194.97474683058329, 0.0, 1.0,
    ];

    private static void TestFittedBoxWithClipRectLayer()
    {
        TestLayerReuse<ClipRectLayer>(
            new RenderFittedBox(
                fit: BoxFit.Cover,
                clipBehavior: Clip.HardEdge,
                // Inject opacity under the clip to force compositing.
                child: new RenderRepaintBoundary(child: SizedLeaf(new Size(100.0, 200.0)))));
    }

    private static void TestFittedBoxWithTransformLayer()
    {
        TestLayerReuse<TransformLayer>(
            new RenderFittedBox(
                fit: BoxFit.Fill,
                // Inject opacity under the clip to force compositing.
                child: new RenderRepaintBoundary(child: SizedLeaf(new Size(1, 1)))));
    }

    /// <summary>proxy_box_test.dart's <c>_testLayerReuse</c>.</summary>
    private static void TestLayerReuse<TLayer>(RenderBox renderObject) where TLayer : Layer
    {
        Assert.NotEqual(typeof(Layer), typeof(TLayer));
        Assert.Null(renderObject.DebugLayer);
        PipelineOwner pipeline = LayoutBox(
            renderObject,
            constraints: BoxConstraints.Tight(new Size(10, 10)),
            paint: true);
        Layer? layer = renderObject.DebugLayer;
        Assert.IsType<TLayer>(layer);
        Assert.NotNull(layer);

        // Mark for repaint otherwise pumpFrame is a noop.
        renderObject.MarkNeedsPaint();
        Assert.True(renderObject.DebugNeedsPaint);
        pipeline.FlushLayout();
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();
        pipeline.CompositeFrame();
        Assert.False(renderObject.DebugNeedsPaint);
        Assert.Same(layer, renderObject.DebugLayer);
    }

    /// <summary>rendering_tester.dart's <c>layout</c>: wraps the box in a positioned, constrained parent when
    /// constraints are given, then runs the pipeline under an 800x600 render view.</summary>
    private static PipelineOwner LayoutBox(
        RenderBox box,
        BoxConstraints? constraints = null,
        Alignment? alignment = null,
        bool paint = false)
    {
        RenderBox root = box;
        if (constraints is { } additional)
        {
            root = new RenderPositionedBox(
                child: new RenderConstrainedBox(additional, child: box),
                alignment: alignment ?? Alignment.Center);
        }

        var view = new RenderView(new FlutterView(new Size(800, 600))) { Child = root };
        var pipeline = new PipelineOwner(view);
        pipeline.Attach(view);
        // No size: the render view hands its child Flutter's tight 800x600 constraints.
        pipeline.FlushLayout();
        if (paint)
        {
            pipeline.FlushCompositingBits();
            pipeline.FlushPaint();
            pipeline.CompositeFrame();
        }

        return pipeline;
    }

    private static RenderBox SizedLeaf(Size size) => new PaintLogBox(size);

    private static Point Round(Point value) => new(
        Math.Round(value.X, MidpointRounding.AwayFromZero),
        Math.Round(value.Y, MidpointRounding.AwayFromZero));

    private static Matrix4 RotateAroundXAxis(double a)
    {
        // 3D rotation transform with alpha=a
        const double x = 1.0;
        const double y = 0.0;
        const double z = 0.0;
        double sc = Math.Sin(a / 2.0) * Math.Cos(a / 2.0);
        double sq = Math.Sin(a / 2.0) * Math.Sin(a / 2.0);
        return Matrix4.FromList(
        [
            // col 1
            1.0 - 2.0 * (y * y + z * z) * sq,
            2.0 * (x * y * sq + z * sc),
            2.0 * (x * z * sq - y * sc),
            0.0,
            // col 2
            2.0 * (x * y * sq - z * sc),
            1.0 - 2.0 * (x * x + z * z) * sq,
            2.0 * (y * z * sq + x * sc),
            0.0,
            // col 3
            2.0 * (x * z * sq + y * sc),
            2.0 * (y * z * sq - x * sc),
            1.0 - 2.0 * (x * x + z * z) * sq,
            0.0,
            // col 4
            0.0, 0.0, 0.0, 1.0,
        ]);
    }

    private static Matrix4 RotateAroundXAxis90Degrees() => Matrix4.FromList(
    [
        // col 1
        1.0, 0.0, 0.0, 0.0,
        // col 2
        0.0, 0.0, 1.0, 0.0,
        // col 3
        0.0, -1.0, 0.0, 0.0,
        // col 4
        0.0, 0.0, 0.0, 1.0,
    ]);

    /// <summary>Dart's <c>tester.layers</c>: every layer below the render view's root, depth first.</summary>
    private static List<Layer> Layers(FrameworkDartTester tester)
    {
        var layers = new List<Layer>();
        void Visit(Layer layer)
        {
            layers.Add(layer);
            if (layer is ContainerLayer container)
            {
                foreach (Layer child in container.Children)
                {
                    Visit(child);
                }
            }
        }

        Visit(tester.RenderView.DebugLayer!);
        return layers;
    }

    /// <summary>fitted_box_test.dart's <c>getLayers</c>.</summary>
    private static List<Type> GetLayers(FrameworkDartTester tester)
    {
        var layers = new List<Type>();
        Layer? container = tester.RenderView.DebugLayer;
        while (container is ContainerLayer containerLayer)
        {
            layers.Add(containerLayer.GetType());
            Assert.Single(containerLayer.Children);
            container = containerLayer.Children[0];
        }

        return layers;
    }

    private static double[] ExtractMatrix(ImageFilter? filter) =>
        Assert.IsType<ImageFilter.Matrix>(filter).Values.ToArray();

    private static void AssertStorage(IReadOnlyList<double> expected, IReadOnlyList<double> actual, int precision)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (int index = 0; index < expected.Count; index++)
        {
            Assert.Equal(expected[index], actual[index], precision);
        }
    }

    private static RenderBox FirstRenderBox(FrameworkDartTester tester, Key key) =>
        (RenderBox)tester.ElementsWithKey(key).Single().FindRenderObject()!;

    private static Point GetTopLeft(Element element) =>
        ((RenderBox)element.FindRenderObject()!).LocalToGlobal(new Point(0, 0));

    private static Size GetSize(Element element) => ((RenderBox)element.FindRenderObject()!).Size;

    private static List<T> FindRenderObjects<T>(RenderObject root) where T : RenderObject
    {
        var found = new List<T>();
        void Visit(RenderObject renderObject)
        {
            if (renderObject is T match)
            {
                found.Add(match);
            }

            renderObject.VisitChildren(Visit);
        }

        Visit(root);
        return found;
    }

    private static void TapAt(FrameworkDartTester tester, Point location)
    {
        int pointer = tester.StartGesture(location);
        tester.Up(pointer, location);
    }

    private static void Send(FrameworkDartTester tester, PointerEvent @event) =>
        GestureBinding.Instance.HandlePointerEvent(tester.RenderView, @event);

    private static Listener RecordingListener(List<PointerEvent> events, Key key) => new(
        onPointerDown: @event => events.Add(@event),
        onPointerUp: @event => events.Add(@event),
        onPointerMove: @event => events.Add(@event),
        onPointerSignal: @event => events.Add(@event),
        child: new Container(key: key, color: Red, height: 100, width: 100));

    /// <summary>The shared body of listener_test.dart's `transformed events` group.</summary>
    private static void CheckTransformedGesture(
        FrameworkDartTester tester,
        List<PointerEvent> events,
        Point downPosition,
        Matrix4 expectedTransform,
        Point localDownPosition,
        Point localDelta)
    {
        var moved = new Vector(20, 30);
        int pointer = tester.StartGesture(downPosition);
        Send(tester, new PointerMoveEvent(
            pointer,
            PointerDeviceKind.Touch,
            downPosition + moved,
            PointerButtons.Primary,
            DateTime.UtcNow));
        tester.Up(pointer, downPosition + moved);

        Assert.Equal(3, events.Count);
        var down = Assert.IsType<PointerDownEvent>(events[0]);
        var move = Assert.IsType<PointerMoveEvent>(events[1]);
        var up = Assert.IsType<PointerUpEvent>(events[2]);
        Point localMovedPosition = localDownPosition + (Vector)localDelta;

        AssertPoint(localDownPosition, down.LocalPosition);
        Assert.Equal(downPosition, down.Position);
        Assert.Equal(new Point(0, 0), down.Delta);
        Assert.Equal(new Point(0, 0), down.LocalDelta);
        AssertStorage(expectedTransform.Storage, down.Transform!.Storage, precision: 10);

        AssertPoint(localMovedPosition, move.LocalPosition);
        Assert.Equal(downPosition + moved, move.Position);
        Assert.Equal((Point)moved, move.Delta);
        AssertPoint(localDelta, move.LocalDelta);
        AssertStorage(expectedTransform.Storage, move.Transform!.Storage, precision: 10);

        AssertPoint(localMovedPosition, up.LocalPosition);
        Assert.Equal(downPosition + moved, up.Position);
        Assert.Equal(new Point(0, 0), up.Delta);
        Assert.Equal(new Point(0, 0), up.LocalDelta);
        AssertStorage(expectedTransform.Storage, up.Transform!.Storage, precision: 10);

        events.Clear();
        // gesture_utils.dart's `scrollAt`: a mouse hovers at the location, then scrolls.
        const int mouse = 7301;
        Send(
            tester,
            new PointerAddedEvent(mouse, PointerDeviceKind.Mouse, downPosition, timestampUtc: DateTime.UtcNow));
        Send(tester, new PointerScrollEvent(
            PointerDeviceKind.Mouse,
            downPosition,
            new Point(0.0, 20.0),
            DateTime.UtcNow,
            device: mouse));
        PointerEvent signal = Assert.Single(events);
        AssertPoint(localDownPosition, signal.LocalPosition);
        Assert.Equal(downPosition, signal.Position);
        Assert.Equal(new Point(0, 0), signal.Delta);
        Assert.Equal(new Point(0, 0), signal.LocalDelta);
        AssertStorage(expectedTransform.Storage, signal.Transform!.Storage, precision: 10);
    }

    private static void AssertPoint(Point expected, Point actual)
    {
        Assert.Equal(expected.X, actual.X, 3);
        Assert.Equal(expected.Y, actual.Y, 3);
    }

    private static List<string> Describe(DiagnosticPropertiesBuilder builder) => builder.Properties
        .Where(node => !node.IsFiltered(DiagnosticLevel.Info))
        .Select(node => node.ToString())
        .ToList();

    private sealed class ProbeFittedBox(RenderBox? child) : RenderFittedBox(child: child)
    {
        public bool InvokeHitTestChildren(BoxHitTestResult result, Point position) =>
            HitTestChildren(result, position);
    }

    /// <summary>proxy_box_test.dart's <c>_TestSemanticsUpdateRenderFractionalTranslation</c>.</summary>
    private sealed class TestSemanticsUpdateRenderFractionalTranslation(Point translation)
        : RenderFractionalTranslation(translation: translation)
    {
        public int MarkNeedsSemanticsUpdateCallCount { get; private set; }

        public override void MarkNeedsSemanticsUpdate()
        {
            MarkNeedsSemanticsUpdateCallCount++;
            base.MarkNeedsSemanticsUpdate();
        }
    }

    /// <summary>A leaf with a preferred size that counts its paints (rendering_tester's sized boxes).</summary>
    private sealed class PaintLogBox(Size preferredSize) : RenderBox
    {
        public int PaintCount { get; private set; }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(preferredSize);
        }

        protected override Size ComputeDryLayout(BoxConstraints constraints) => constraints.Constrain(preferredSize);

        public override void Paint(PaintingContext ctx, Point offset)
        {
            PaintCount++;
        }
    }

    /// <summary>mouse_region_test.dart's <c>_DelegatedPainter</c>.</summary>
    private sealed class DelegatedPainter(Action onPaint) : CustomPainter
    {
        public override void Paint(PaintingContext context, Size size)
        {
            onPaint();
        }

        public override bool ShouldRepaint(CustomPainter oldDelegate) => false;
    }
}
