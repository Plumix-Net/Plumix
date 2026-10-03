using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using Transform = Plumix.Widgets.Transform;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/interactive_viewer.dart
// Mirrors flutter/packages/flutter/test/widgets/interactive_viewer_test.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class InteractiveViewerDartParityTests
{
    private const double Eps = 1e-10;

    // ------------------------------------------------------------------ group('InteractiveViewer')

    // Flutter: "child fits in viewport"
    [Fact]
    public void ChildFitsInViewport()
    {
        using var controller = new TransformationController();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Standard(controller));

        Assert.Equal(Matrix4.Identity(), controller.Value);

        // Attempting to drag to pan doesn't work because the child fits inside the viewport and has
        // a tight boundary.
        Point childOffset = tester.GetTopLeft(Find.ByType<SizedBox>());
        Point childInterior = childOffset + new Point(20.0, 20.0);
        Drag(tester, childInterior, childOffset);
        Assert.Equal(Matrix4.Identity(), controller.Value);

        // Pinch to zoom works.
        Pinch(
            tester,
            childInterior,
            childInterior + new Point(10.0, 0.0),
            childInterior + new Point(-10.0, 0.0),
            childInterior + new Point(20.0, 0.0));
        Assert.NotEqual(Matrix4.Identity(), controller.Value);
    }

    // Flutter: "boundary slightly bigger than child"
    [Fact]
    public void BoundarySlightlyBiggerThanChild()
    {
        using var controller = new TransformationController();
        using var tester = new FrameworkDartTester();
        const double boundaryMargin = 10.0;
        tester.PumpWidget(Standard(controller, boundaryMargin: EdgeInsets.All(boundaryMargin)));

        Assert.Equal(Matrix4.Identity(), controller.Value);

        // Dragging to pan works only until it hits the boundary.
        Point childOffset = tester.GetTopLeft(Find.ByType<SizedBox>());
        Point childInterior = childOffset + new Point(20.0, 20.0);
        Drag(tester, childInterior, childOffset);
        Vector3 translation = controller.Value.GetTranslation();
        Assert.Equal(-boundaryMargin, translation.X);
        Assert.Equal(-boundaryMargin, translation.Y);

        // Pinch to zoom also only works until it expands to the boundary.
        Point scaleStart1 = childInterior;
        Point scaleStart2 = childInterior + new Point(20.0, 0.0);
        Pinch(
            tester,
            scaleStart1,
            scaleStart2,
            scaleStart1 + new Point(5.0, 0.0),
            scaleStart2 + new Point(-5.0, 0.0));
        Assert.Equal(200.0 / 220.0, controller.Value.GetMaxScaleOnAxis());
    }

    // Flutter: "child bigger than viewport"
    [Fact]
    public void ChildBiggerThanViewport()
    {
        using var controller = new TransformationController();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Center(child: new InteractiveViewer(
            constrained: false,
            scaleEnabled: false,
            transformationController: controller,
            child: new SizedBox(width: 2000.0, height: 2000.0))));

        Assert.Equal(Matrix4.Identity(), controller.Value);

        // Attempting to move against the boundary doesn't work.
        Point childOffset = tester.GetTopLeft(Find.ByType<SizedBox>());
        Point childInterior = childOffset + new Point(20.0, 20.0);
        Drag(tester, childOffset, childInterior);
        Assert.Equal(Matrix4.Identity(), controller.Value);

        // Attempting to pinch to zoom doesn't work because it's disabled.
        StartedPinch(
            tester,
            childInterior,
            childInterior + new Point(10.0, 0.0),
            childInterior + new Point(-10.0, 0.0),
            childInterior + new Point(20.0, 0.0));
        Assert.Equal(Matrix4.Identity(), controller.Value);

        // Attempting to pinch to rotate doesn't work because it's disabled.
        StartedPinch(
            tester,
            childInterior,
            childInterior + new Point(10.0, 0.0),
            childInterior + new Point(5.0, 5.0),
            childInterior + new Point(-5.0, -5.0));
        Assert.Equal(Matrix4.Identity(), controller.Value);

        // Drag to pan away from the boundary.
        Drag(tester, childInterior, childOffset);
        Assert.NotEqual(Matrix4.Identity(), controller.Value);
    }

    // Flutter: "child has no dimensions"
    [Fact]
    public void ChildHasNoDimensions()
    {
        using var controller = new TransformationController();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Center(child: new InteractiveViewer(
            constrained: false,
            scaleEnabled: false,
            transformationController: controller,
            child: SizedBox.Shrink())));
        Assert.Equal(Matrix4.Identity(), controller.Value);

        // Interacting throws an error because the child has no size.
        Point childOffset = tester.GetTopLeft(Find.ByType<SizedBox>());
        Point childInterior = childOffset + new Point(20.0, 20.0);
        Drag(tester, childOffset, childInterior);

        Assert.Equal(Matrix4.Identity(), controller.Value);
        Assert.IsType<AssertionError>(tester.TakeException());
    }

    // Flutter: "no boundary"
    [Fact]
    public void NoBoundary()
    {
        using var controller = new TransformationController();
        using var tester = new FrameworkDartTester();
        const double minScale = 0.8;
        tester.PumpWidget(Standard(controller, boundaryMargin: EdgeInsets.All(double.PositiveInfinity)));

        Assert.Equal(Matrix4.Identity(), controller.Value);

        // Drag to pan works because even though the viewport fits perfectly around the child, there
        // is no boundary.
        Point childOffset = tester.GetTopLeft(Find.ByType<SizedBox>());
        Point childInterior = childOffset + new Point(20.0, 20.0);
        Drag(tester, childInterior, childOffset);
        Vector3 translation = controller.Value.GetTranslation();
        Assert.Equal(childOffset.X - childInterior.X, translation.X);
        Assert.Equal(childOffset.Y - childInterior.Y, translation.Y);

        // It's also possible to zoom out and view beyond the child because there is no boundary.
        Point scaleStart1 = childInterior;
        Point scaleStart2 = childInterior + new Point(20.0, 0.0);
        Pinch(
            tester,
            scaleStart1,
            scaleStart2,
            childInterior + new Point(5.0, 0.0),
            childInterior + new Point(-5.0, 0.0));
        Assert.Equal(minScale, controller.Value.GetMaxScaleOnAxis());
    }

    // Flutter: "PanAxis.free allows panning in all directions for diagonal gesture"
    // Flutter: "PanAxis.aligned allows panning in one direction only for diagonal gesture"
    // Flutter: "PanAxis.aligned allows panning in one direction only for horizontal leaning gesture"
    // Flutter: "PanAxis.horizontal allows panning in the horizontal direction only for diagonal gesture"
    // Flutter: "PanAxis.horizontal allows panning in the horizontal direction only for horizontal
    //           leaning gesture"
    // Flutter: "PanAxis.horizontal does not allow panning in vertical direction on vertical gesture"
    // Flutter: "PanAxis.vertical allows panning in the vertical direction only for diagonal gesture"
    // Flutter: "PanAxis.vertical allows panning in the vertical direction only for vertical leaning
    //           gesture" (the source drags by (20, 10) despite the name)
    // Flutter: "PanAxis.vertical does not allow panning in horizontal direction on vertical gesture"
    [Theory]
    [InlineData(PanAxis.Free, 20.0, 20.0, true, true)]
    [InlineData(PanAxis.Aligned, 20.0, 20.0, false, true)]
    [InlineData(PanAxis.Aligned, 20.0, 10.0, true, false)]
    [InlineData(PanAxis.Horizontal, 20.0, 20.0, true, false)]
    [InlineData(PanAxis.Horizontal, 20.0, 10.0, true, false)]
    [InlineData(PanAxis.Horizontal, 0.0, 10.0, false, false)]
    [InlineData(PanAxis.Vertical, 20.0, 20.0, false, true)]
    [InlineData(PanAxis.Vertical, 20.0, 10.0, false, true)]
    [InlineData(PanAxis.Vertical, 10.0, 0.0, false, false)]
    public void PanAxisRestrictsTheDragDirection(PanAxis panAxis, double dx, double dy, bool movesX, bool movesY)
    {
        using var controller = new TransformationController();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Standard(
            controller,
            boundaryMargin: EdgeInsets.All(double.PositiveInfinity),
            panAxis: panAxis));

        Assert.Equal(Matrix4.Identity(), controller.Value);

        Point childOffset = tester.GetTopLeft(Find.ByType<SizedBox>());
        Point childInterior = childOffset + new Point(dx, dy);
        Drag(tester, childInterior, childOffset);

        Vector3 translation = controller.Value.GetTranslation();
        Assert.Equal(movesX ? childOffset.X - childInterior.X : 0.0, translation.X);
        Assert.Equal(movesY ? childOffset.Y - childInterior.Y : 0.0, translation.Y);
    }

    // Flutter: "inertia fling and boundary sliding"
    [Fact]
    public void InertiaFlingAndBoundarySliding()
    {
        using var controller = new TransformationController();
        using var tester = new FrameworkDartTester();
        const double boundaryMargin = 50.0;
        tester.PumpWidget(Standard(controller, boundaryMargin: EdgeInsets.All(boundaryMargin)));

        Assert.Equal(Matrix4.Identity(), controller.Value);

        // Fling the child.
        Point childOffset = tester.GetTopLeft(Find.ByType<SizedBox>());
        var flingEnd = new Vector(20.0, 15.0);
        tester.FlingFrom(childOffset, flingEnd, 1000.0);
        tester.Pump();

        // Immediately after the gesture, the child has moved to exactly follow the gesture.
        Vector3 translation = controller.Value.GetTranslation();
        Assert.Equal(flingEnd.X, translation.X);
        Assert.Equal(flingEnd.Y, translation.Y);

        // A short time after the gesture was released, it continues to move with inertia.
        tester.Pump(TimeSpan.FromMilliseconds(10));
        translation = controller.Value.GetTranslation();
        Assert.True(translation.X > flingEnd.X);
        Assert.True(translation.Y > flingEnd.Y);
        Assert.True(translation.X < boundaryMargin);
        Assert.True(translation.Y < boundaryMargin);

        // It hits the boundary in the x direction first.
        tester.Pump(TimeSpan.FromMilliseconds(60));
        translation = controller.Value.GetTranslation();
        AssertMoreOrLessEquals(boundaryMargin, translation.X, 1e-9);
        Assert.True(translation.Y < boundaryMargin);
        double yWhenXHits = translation.Y;

        // x is held to the boundary, while y continues to move.
        tester.Pump(TimeSpan.FromMilliseconds(50));
        translation = controller.Value.GetTranslation();
        AssertMoreOrLessEquals(boundaryMargin, translation.X, 1e-9);
        Assert.True(translation.Y > yWhenXHits);
        Assert.True(translation.Y < boundaryMargin);

        // Eventually it stops in the corner.
        tester.PumpAndSettle();
        translation = controller.Value.GetTranslation();
        AssertMoreOrLessEquals(boundaryMargin, translation.X, 1e-9);
        AssertMoreOrLessEquals(boundaryMargin, translation.Y, 1e-9);
    }

    // Flutter: "Scaling automatically causes a centering translation"
    // Flutter: "Scaling automatically causes a centering translation even when alignPanAxis is set"
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ScalingAutomaticallyCausesACenteringTranslation(bool aligned)
    {
        using var controller = new TransformationController();
        using var tester = new FrameworkDartTester();
        const double boundaryMargin = 50.0;
        const double minScale = 0.1;
        tester.PumpWidget(Standard(
            controller,
            boundaryMargin: EdgeInsets.All(boundaryMargin),
            minScale: minScale,
            panAxis: aligned ? PanAxis.Aligned : PanAxis.Free));

        Vector3 translation = controller.Value.GetTranslation();
        Assert.Equal(0.0, translation.X);
        Assert.Equal(0.0, translation.Y);

        // Pan into the corner of the boundaries.
        Point childOffset = tester.GetTopLeft(Find.ByType<SizedBox>());
        if (aligned)
        {
            tester.FlingFrom(childOffset, new Vector(20.0, 0.0), 1000.0);
            tester.PumpAndSettle();
            tester.Pump(TimeSpan.FromSeconds(5));
            Point childOffset2 = tester.GetTopLeft(Find.ByType<SizedBox>());
            tester.FlingFrom(childOffset2, new Vector(0.0, 15.0), 1000.0);
        }
        else
        {
            tester.FlingFrom(childOffset, new Vector(20.0, 15.0), 1000.0);
        }

        tester.PumpAndSettle();
        translation = controller.Value.GetTranslation();
        AssertMoreOrLessEquals(boundaryMargin, translation.X, 1e-9);
        AssertMoreOrLessEquals(boundaryMargin, translation.Y, 1e-9);

        // Zoom out so the entire child is visible. The child will also be translated in order to
        // keep it inside the boundaries.
        Point childCenter = tester.GetCenter(Find.ByType<SizedBox>());
        Pinch(
            tester,
            childCenter + new Point(-40.0, 0.0),
            childCenter + new Point(40.0, 0.0),
            childCenter + new Point(-10.0, 0.0),
            childCenter + new Point(10.0, 0.0));
        Assert.True(controller.Value.GetMaxScaleOnAxis() < 1.0);
        translation = controller.Value.GetTranslation();
        Assert.True(translation.X < boundaryMargin);
        Assert.True(translation.Y < boundaryMargin);
        Assert.True(translation.X > 0.0);
        Assert.True(translation.Y > 0.0);
        AssertMoreOrLessEquals(translation.Y, translation.X, 1e-9);

        // Zoom in on a point that's not the center, and see that it remains at roughly the same
        // location in the viewport after the zoom.
        var viewportFocalPoint = new Point(childCenter.X - 40.0 - childOffset.X, childCenter.Y - childOffset.Y);
        Point sceneFocalPoint = controller.ToScene(viewportFocalPoint);
        Pinch(
            tester,
            childCenter + new Point(-50.0, 0.0),
            childCenter + new Point(-30.0, 0.0),
            childCenter + new Point(-51.0, 0.0),
            childCenter + new Point(-29.0, 0.0));
        Point newSceneFocalPoint = controller.ToScene(viewportFocalPoint);
        AssertMoreOrLessEquals(sceneFocalPoint.X, newSceneFocalPoint.X, 1.0);
        AssertMoreOrLessEquals(sceneFocalPoint.Y, newSceneFocalPoint.Y, 1.0);
    }

    // Flutter: "Can scale with mouse"
    [Fact]
    public void CanScaleWithMouse()
    {
        using var controller = new TransformationController();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Standard(controller));

        Point center = tester.GetCenter(Find.ByType<InteractiveViewer>());
        ScrollAt(center, tester, new Vector(0.0, -20.0));
        tester.PumpAndSettle();

        Assert.True(controller.Value.GetMaxScaleOnAxis() > 1.0);
    }

    // Flutter: "Cannot scale with mouse when scale is disabled"
    [Fact]
    public void CannotScaleWithMouseWhenScaleIsDisabled()
    {
        using var controller = new TransformationController();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Standard(controller, scaleEnabled: false));

        Point center = tester.GetCenter(Find.ByType<InteractiveViewer>());
        ScrollAt(center, tester, new Vector(0.0, -20.0));
        tester.PumpAndSettle();

        Assert.Equal(1.0, controller.Value.GetMaxScaleOnAxis());
    }

    // Flutter: "Scale with mouse returns onInteraction properties"
    [Fact]
    public void ScaleWithMouseReturnsOnInteractionProperties()
    {
        using var controller = new TransformationController();
        using var tester = new FrameworkDartTester();
        var recorder = new InteractionRecorder();
        tester.PumpWidget(Standard(controller, recorder: recorder));

        Point center = tester.GetCenter(Find.ByType<InteractiveViewer>());
        ScrollAt(center, tester, new Vector(0.0, -20.0));
        tester.PumpAndSettle();
        double afterScaling = controller.Value.GetMaxScaleOnAxis();

        Assert.True(recorder.ScaleChange > 1.0);
        Assert.Equal(afterScaling, recorder.ScaleChange);
        Assert.Equal(Velocity.Zero, recorder.CurrentVelocity);
        Assert.True(recorder.CalledStart);
        Assert.Equal(center, recorder.FocalPoint);
        Assert.Equal(new Point(100, 100), recorder.LocalFocalPoint);
        Assert.Equal(new Point(100, 100), controller.ToScene(recorder.LocalFocalPoint!.Value));
    }

    // Flutter: "Scaling amount is equal forth and back with a mouse scroll"
    [Fact]
    public void ScalingAmountIsEqualForthAndBackWithAMouseScroll()
    {
        using var controller = new TransformationController();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Center(child: new InteractiveViewer(
            constrained: false,
            maxScale: 100000,
            minScale: 0.01,
            transformationController: controller,
            child: new SizedBox(width: 1000.0, height: 1000.0))));

        Point center = tester.GetCenter(Find.ByType<InteractiveViewer>());
        ScrollAt(center, tester, new Vector(0.0, -200.0));
        tester.PumpAndSettle();
        // Scaling in here.
        Assert.Equal(Math.Exp(200 / 200.0), controller.Value.GetMaxScaleOnAxis());

        ScrollAt(center, tester, new Vector(0.0, -200.0));
        tester.PumpAndSettle();
        // Scaling in again here.
        Assert.InRange(
            controller.Value.GetMaxScaleOnAxis(),
            Math.Exp(400 / 200.0) - 0.000000000000001,
            Math.Exp(400 / 200.0) + 0.000000000000001);

        ScrollAt(center, tester, new Vector(0.0, 200.0));
        ScrollAt(center, tester, new Vector(0.0, 200.0));
        tester.PumpAndSettle();
        // Scaling out here, back to the original scale.
        Assert.Equal(1.0, controller.Value.GetMaxScaleOnAxis());
    }

    // Flutter: "onInteraction can be used to get scene point"
    [Fact]
    public void OnInteractionCanBeUsedToGetScenePoint()
    {
        using var controller = new TransformationController();
        using var tester = new FrameworkDartTester();
        var recorder = new InteractionRecorder();
        tester.PumpWidget(Standard(controller, recorder: recorder));

        Point center = tester.GetCenter(Find.ByType<InteractiveViewer>());
        Point offCenter = center + new Point(-20.0, -20.0);
        ScrollAt(offCenter, tester, new Vector(0.0, -20.0));
        tester.PumpAndSettle();
        double afterScaling = controller.Value.GetMaxScaleOnAxis();

        Assert.True(recorder.ScaleChange > 1.0);
        Assert.Equal(afterScaling, recorder.ScaleChange);
        Assert.Equal(Velocity.Zero, recorder.CurrentVelocity);
        Assert.True(recorder.CalledStart);
        Assert.Equal(offCenter, recorder.FocalPoint);
        Assert.Equal(new Point(80, 80), recorder.LocalFocalPoint);

        // The top left corner of the viewport is not at the top left corner of the scene.
        Point scenePoint = controller.ToScene(default);
        Assert.True(scenePoint.X > 0.0);
        Assert.True(scenePoint.Y > 0.0);
    }

    // Flutter: "onInteraction is called even when disabled (touch)"
    [Theory]
    [InlineData(TargetPlatform.Android)]
    [InlineData(TargetPlatform.IOS)]
    public void OnInteractionIsCalledEvenWhenDisabledTouch(TargetPlatform platform)
    {
        using IDisposable variant = TargetPlatformVariant.Override(platform);
        using var controller = new TransformationController();
        using var tester = new FrameworkDartTester();
        var recorder = new InteractionRecorder();
        var sizedBox = new SizedBox(width: 200.0, height: 200.0);
        tester.PumpWidget(new Center(child: new InteractiveViewer(
            transformationController: controller,
            scaleEnabled: false,
            onInteractionStart: _ => recorder.CalledStart = true,
            onInteractionUpdate: _ => recorder.CalledUpdate = true,
            onInteractionEnd: _ => recorder.CalledEnd = true,
            child: sizedBox)));

        Point childOffset = tester.GetTopLeft(Find.ByWidget(sizedBox));
        Point childInterior = childOffset + new Point(20.0, 20.0);

        // Pan does nothing against the tight boundary, but the callbacks are called.
        Drag(tester, childOffset, childInterior);
        Assert.Equal(Matrix4.Identity(), controller.Value);
        Assert.True(recorder.CalledStart);
        Assert.True(recorder.CalledUpdate);
        Assert.True(recorder.CalledEnd);

        recorder.CalledStart = false;
        recorder.CalledUpdate = false;
        recorder.CalledEnd = false;

        // Pinch is disabled, but the callbacks are called.
        StartedPinch(
            tester,
            childInterior,
            childInterior + new Point(10.0, 0.0),
            childInterior + new Point(-10.0, 0.0),
            childInterior + new Point(20.0, 0.0));
        Assert.Equal(Matrix4.Identity(), controller.Value);
        Assert.True(recorder.CalledStart);
        Assert.True(recorder.CalledUpdate);
        Assert.True(recorder.CalledEnd);
    }

    // Flutter: "onInteraction is called even when disabled (mouse)"
    [Theory]
    [InlineData(TargetPlatform.MacOS)]
    [InlineData(TargetPlatform.Linux)]
    [InlineData(TargetPlatform.Windows)]
    public void OnInteractionIsCalledEvenWhenDisabledMouse(TargetPlatform platform)
    {
        using IDisposable variant = TargetPlatformVariant.Override(platform);
        using var controller = new TransformationController();
        using var tester = new FrameworkDartTester();
        var recorder = new InteractionRecorder();
        var sizedBox = new SizedBox(width: 200.0, height: 200.0);
        tester.PumpWidget(new Center(child: new InteractiveViewer(
            transformationController: controller,
            scaleEnabled: false,
            onInteractionStart: _ => recorder.CalledStart = true,
            onInteractionUpdate: _ => recorder.CalledUpdate = true,
            onInteractionEnd: _ => recorder.CalledEnd = true,
            child: sizedBox)));

        Point childOffset = tester.GetTopLeft(Find.ByWidget(sizedBox));
        Point childInterior = childOffset + new Point(20.0, 20.0);

        // Pan does nothing against the tight boundary, but the callbacks are called.
        TestGesture gesture = tester.StartGesture(childOffset, PointerDeviceKind.Mouse);
        tester.Pump();
        gesture.MoveTo(childInterior);
        tester.Pump();
        gesture.Up();
        tester.PumpAndSettle();
        Assert.Equal(Matrix4.Identity(), controller.Value);
        Assert.True(recorder.CalledStart);
        Assert.True(recorder.CalledUpdate);
        Assert.True(recorder.CalledEnd);

        recorder.CalledStart = false;
        recorder.CalledUpdate = false;
        recorder.CalledEnd = false;

        // Mouse scroll is disabled, but the callbacks are called.
        ScrollAt(childInterior, tester, new Vector(0.0, -20.0));
        tester.PumpAndSettle();
        Assert.Equal(Matrix4.Identity(), controller.Value);
        Assert.True(recorder.CalledStart);
        Assert.True(recorder.CalledUpdate);
        Assert.True(recorder.CalledEnd);
    }

    // Flutter: "viewport changes size"
    [Fact]
    public void ViewportChangesSize()
    {
        using var controller = new TransformationController();
        using var tester = new FrameworkDartTester(devicePixelRatio: 3.0);
        tester.PumpWidget(new Center(child: new InteractiveViewer(
            transformationController: controller,
            child: new Container())));

        Assert.Equal(Matrix4.Identity(), controller.Value);

        // Attempting to drag to pan doesn't work because the child fits inside the viewport and has
        // a tight boundary.
        Point childOffset = tester.GetTopLeft(Find.ByType<Container>());
        Point childInterior = childOffset + new Point(20.0, 20.0);
        Drag(tester, childInterior, childOffset);
        Assert.Equal(Matrix4.Identity(), controller.Value);

        // Shrink the size of the screen.
        tester.View.PhysicalSize = new Size(100.0, 100.0);
        tester.Pump();

        // Attempting to drag to pan still doesn't work, because the image has resized itself to fit
        // the new screen size, and InteractiveViewer has updated its measurements to take that into
        // consideration.
        Drag(tester, childInterior, childOffset);
        Assert.Equal(Matrix4.Identity(), controller.Value);
        tester.View.ResetPhysicalSize();
    }

    // Flutter: "gesture can start as pan and become scale"
    [Fact]
    public void GestureCanStartAsPanAndBecomeScale()
    {
        using var controller = new TransformationController();
        using var tester = new FrameworkDartTester();
        const double boundaryMargin = 50.0;
        tester.PumpWidget(Standard(controller, boundaryMargin: EdgeInsets.All(boundaryMargin)));

        Vector3 translation = controller.Value.GetTranslation();
        Assert.Equal(0.0, translation.X);
        Assert.Equal(0.0, translation.Y);

        // Start a pan gesture.
        Point childCenter = tester.GetCenter(Find.ByType<SizedBox>());
        TestGesture gesture = tester.CreateGesture();
        gesture.Down(childCenter);
        tester.Pump();
        gesture.MoveTo(childCenter + new Point(5.0, 5.0));
        tester.Pump();
        translation = controller.Value.GetTranslation();
        Assert.True(translation.X > 0.0);
        Assert.True(translation.Y > 0.0);

        // Put another finger down and turn it into a scale gesture.
        TestGesture gesture2 = tester.CreateGesture();
        gesture2.Down(childCenter + new Point(-5.0, -5.0));
        tester.Pump();
        gesture.MoveTo(childCenter + new Point(25.0, 25.0));
        gesture2.MoveTo(childCenter + new Point(-25.0, -25.0));
        tester.Pump();
        gesture.Up();
        gesture2.Up();
        tester.PumpAndSettle();
        Assert.True(controller.Value.GetMaxScaleOnAxis() > 1.0);
    }

    // Flutter: "can view beyond boundary when necessary for a small child"
    // Regression test for https://github.com/flutter/flutter/issues/65304
    [Fact]
    public void CanViewBeyondBoundaryWhenNecessaryForASmallChild()
    {
        using var controller = new TransformationController();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Center(child: new InteractiveViewer(
            constrained: false,
            minScale: 1.0,
            maxScale: 1.0,
            transformationController: controller,
            child: new SizedBox(width: 200.0, height: 200.0))));

        Assert.Equal(Matrix4.Identity(), controller.Value);

        // Pinch to zoom doesn't cause a jump; the child stays where it is.
        Point center = tester.GetCenter(Find.ByType<SizedBox>());
        Pinch(
            tester,
            center + new Point(-10.0, -10.0),
            center + new Point(10.0, 10.0),
            center + new Point(-20.0, -20.0),
            center + new Point(20.0, 20.0));
        Assert.Equal(Matrix4.Identity(), controller.Value);
    }

    // Flutter: "scale does not jump when wrapped in GestureDetector"
    [Fact]
    public void ScaleDoesNotJumpWhenWrappedInGestureDetector()
    {
        using var controller = new TransformationController();
        using var tester = new FrameworkDartTester();
        double? initialScale = null;
        double? scale = null;
        tester.PumpWidget(new Center(child: new GestureDetector(
            onTapUp: _ => { },
            child: new InteractiveViewer(
                onInteractionUpdate: details =>
                {
                    initialScale ??= details.Scale;
                    scale = details.Scale;
                },
                transformationController: controller,
                child: new SizedBox(width: 200.0, height: 200.0)))));

        Assert.Equal(Matrix4.Identity(), controller.Value);
        Assert.Null(initialScale);
        Assert.Null(scale);

        // Pinch to zoom isn't immediately detected for a small amount of movement due to the
        // GestureDetector.
        Point childOffset = tester.GetTopLeft(Find.ByType<SizedBox>());
        Point childInterior = childOffset + new Point(20.0, 20.0);
        Pinch(
            tester,
            childInterior,
            childInterior + new Point(10.0, 0.0),
            childInterior + new Point(-10.0, 0.0),
            childInterior + new Point(20.0, 0.0));
        Assert.Equal(Matrix4.Identity(), controller.Value);
        Assert.Null(initialScale);
        Assert.Null(scale);

        // Pinch to zoom for a larger amount is detected. It starts smoothly at 1.0 despite the
        // fact that the gesture has already moved a bit.
        Pinch(
            tester,
            childInterior,
            childInterior + new Point(10.0, 0.0),
            childInterior + new Point(-38.0, 0.0),
            childInterior + new Point(48.0, 0.0));
        Assert.Equal(1.0, initialScale);
        Assert.True(scale > 1.0);
        Assert.True(controller.Value.GetMaxScaleOnAxis() > 1.0);
    }

    // Flutter: "Check if ClipRect is present in the tree"
    [Fact]
    public void CheckIfClipRectIsPresentInTheTree()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Center(child: new InteractiveViewer(
            constrained: false,
            clipBehavior: Clip.None,
            minScale: 1.0,
            maxScale: 1.0,
            child: new SizedBox(width: 200.0, height: 200.0))));

        RenderClipRect renderClip = tester.AllRenderObjects.OfType<RenderClipRect>().First();
        Assert.Equal(Clip.None, renderClip.ClipBehavior);

        tester.PumpWidget(new Center(child: new InteractiveViewer(
            constrained: false,
            minScale: 1.0,
            maxScale: 1.0,
            child: new SizedBox(width: 200.0, height: 200.0))));

        Assert.Single(Find.ByType<ClipRect>().Evaluate(tester));
    }

    // Flutter: "builder can change widgets that are off-screen"
    [Fact]
    public void BuilderCanChangeWidgetsThatAreOffScreen()
    {
        using var controller = new TransformationController();
        using var tester = new FrameworkDartTester();
        const double childHeight = 10.0;
        var green = new Color(0xFF00FF00);
        var red = new Color(0xFFFF0000);
        var outerKey = Key.Create("outer box");
        tester.PumpWidget(new Center(child: new SizedBox(
            key: outerKey,
            height: 50.0,
            child: InteractiveViewer.Builder(
                transformationController: controller,
                scaleEnabled: false,
                boundaryMargin: EdgeInsets.All(double.PositiveInfinity),
                // Build visible children green, off-screen children red.
                builder: (_, viewportQuad) =>
                {
                    Rect viewport = AxisAlignedBoundingBox(viewportQuad);
                    var children = new List<Widget>();
                    for (int i = 0; i < 10; i++)
                    {
                        double childTop = i * childHeight;
                        double childBottom = childTop + childHeight;
                        bool visible = (childBottom >= viewport.Top && childBottom <= viewport.Bottom)
                                       || (childTop >= viewport.Top && childTop <= viewport.Bottom);
                        children.Add(new Container(height: childHeight, color: visible ? green : red));
                    }

                    return new Column(children: children);
                }))));

        Assert.Equal(Matrix4.Identity(), controller.Value);

        // The first six are partially visible and therefore green.
        int index = 0;
        foreach (Element element in Find.ByType<Container>(skipOffstage: false).Evaluate(tester))
        {
            var container = (Container)element.Widget;
            Assert.Equal(index < 6 ? green : red, container.Color);
            index++;
        }

        // Drag on the InteractiveViewer to move it upwards.
        Point childOffset = tester.GetTopLeft(Find.ByKey(outerKey));
        const double translationY = 15.0;
        var childInterior = new Point(childOffset.X, childOffset.Y + translationY);
        Drag(tester, childInterior, childOffset);

        Assert.NotEqual(Matrix4.Identity(), controller.Value);
        Assert.Equal(-translationY, controller.Value.GetTranslation().Y);

        // After scrolling down a bit, the first child is not visible, the next six are, and the
        // final three are not.
        index = 0;
        foreach (Element element in Find.ByType<Container>(skipOffstage: false).Evaluate(tester))
        {
            var container = (Container)element.Widget;
            Assert.Equal(index > 0 && index < 7 ? green : red, container.Color);
            index++;
        }
    }

    // Flutter: "LayoutBuilder is only used for InteractiveViewer.builder"
    [Fact]
    public void LayoutBuilderIsOnlyUsedForInteractiveViewerBuilder()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Center(child: new InteractiveViewer(
            child: new SizedBox(width: 200.0, height: 200.0))));

        Assert.Empty(Find.ByType<LayoutBuilder>().Evaluate(tester));

        tester.PumpWidget(new Center(child: InteractiveViewer.Builder(
            builder: (_, _) => new SizedBox(width: 200.0, height: 200.0))));

        Assert.Single(Find.ByType<LayoutBuilder>().Evaluate(tester));
    }

    // Flutter: "scaleFactor"
    [Fact]
    public void ScaleFactor()
    {
        using var controller = new TransformationController();
        using var tester = new FrameworkDartTester();
        const double scrollAmount = 30.0;

        void PumpScaleFactor(double scaleFactor)
        {
            tester.PumpWidget(new Center(child: new InteractiveViewer(
                boundaryMargin: EdgeInsets.All(double.PositiveInfinity),
                transformationController: controller,
                scaleFactor: scaleFactor,
                child: new SizedBox(width: 200.0, height: 200.0))));
        }

        // Start with the default scaleFactor.
        PumpScaleFactor(200.0);

        Assert.Equal(Matrix4.Identity(), controller.Value);

        // Zoom out. The scale decreases.
        Point center = tester.GetCenter(Find.ByType<InteractiveViewer>());
        ScrollAt(center, tester, new Vector(0.0, scrollAmount));
        tester.PumpAndSettle();
        double scaleZoomedOut = controller.Value.GetMaxScaleOnAxis();
        Assert.True(scaleZoomedOut < 1.0);

        // Zoom in. The scale increases.
        ScrollAt(center, tester, new Vector(0.0, -scrollAmount));
        tester.PumpAndSettle();
        double scaleZoomedIn = controller.Value.GetMaxScaleOnAxis();
        Assert.True(scaleZoomedIn > scaleZoomedOut);

        // Reset and decrease the scaleFactor below the default, so that scaling will happen more
        // quickly.
        controller.Value = Matrix4.Identity();
        PumpScaleFactor(100.0);

        // Zoom out. The scale decreases more quickly than with the default (higher) scaleFactor.
        ScrollAt(center, tester, new Vector(0.0, scrollAmount));
        tester.PumpAndSettle();
        double scaleLowZoomedOut = controller.Value.GetMaxScaleOnAxis();
        Assert.True(scaleLowZoomedOut < 1.0);
        Assert.True(scaleLowZoomedOut < scaleZoomedOut);

        // Zoom in. The scale increases more quickly than with the default (higher) scaleFactor.
        ScrollAt(center, tester, new Vector(0.0, -scrollAmount));
        tester.PumpAndSettle();
        double scaleLowZoomedIn = controller.Value.GetMaxScaleOnAxis();
        Assert.True(scaleLowZoomedIn > scaleLowZoomedOut);
        Assert.True(scaleLowZoomedIn - scaleLowZoomedOut > scaleZoomedIn - scaleZoomedOut);

        // Reset and increase the scaleFactor above the default.
        controller.Value = Matrix4.Identity();
        PumpScaleFactor(400.0);

        // Zoom out. The scale decreases, but not by as much as with the default (higher)
        // scaleFactor.
        ScrollAt(center, tester, new Vector(0.0, scrollAmount));
        tester.PumpAndSettle();
        double scaleHighZoomedOut = controller.Value.GetMaxScaleOnAxis();
        Assert.True(scaleHighZoomedOut < 1.0);
        Assert.True(scaleHighZoomedOut > scaleZoomedOut);

        // Zoom in. The scale increases, but not by as much as with the default (higher)
        // scaleFactor.
        ScrollAt(center, tester, new Vector(0.0, -scrollAmount));
        tester.PumpAndSettle();
        double scaleHighZoomedIn = controller.Value.GetMaxScaleOnAxis();
        Assert.True(scaleHighZoomedIn > scaleHighZoomedOut);
        Assert.True(scaleHighZoomedIn - scaleHighZoomedOut < scaleZoomedIn - scaleZoomedOut);
    }

    // Flutter: "alignment argument is used properly"
    [Fact]
    public void AlignmentArgumentIsUsedProperly()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new InteractiveViewer(alignment: Alignment.Center, child: new Container()));

        var transform = tester.FirstWidget<Transform>(Find.ByType<Transform>());
        Assert.Equal((AlignmentGeometry)Alignment.Center, transform.Alignment);
    }

    // Flutter: "interactionEndFrictionCoefficient"
    [Fact]
    public void InteractionEndFrictionCoefficient()
    {
        // Use the default interactionEndFrictionCoefficient.
        double translation1Y;
        using (var controller1 = new TransformationController())
        using (var tester = new FrameworkDartTester())
        {
            tester.PumpWidget(SizedBox.Square(
                dimension: 200.0,
                child: new InteractiveViewer(
                    constrained: false,
                    transformationController: controller1,
                    child: new SizedBox(width: 2000.0, height: 2000.0))));

            Assert.Equal(Matrix4.Identity(), controller1.Value);

            tester.FlingFrom(new Point(100, 100), new Vector(0, -50), 100.0);
            tester.PumpAndSettle();
            translation1Y = controller1.Value.GetTranslation().Y;
            Assert.True(translation1Y < -58.0);
        }

        // Next try a custom interactionEndFrictionCoefficient.
        using (var controller2 = new TransformationController())
        using (var tester = new FrameworkDartTester())
        {
            tester.PumpWidget(SizedBox.Square(
                dimension: 200.0,
                child: new InteractiveViewer(
                    constrained: false,
                    interactionEndFrictionCoefficient: 0.01,
                    transformationController: controller2,
                    child: new SizedBox(width: 2000.0, height: 2000.0))));

            Assert.Equal(Matrix4.Identity(), controller2.Value);

            tester.FlingFrom(new Point(100, 100), new Vector(0, -50), 100.0);
            tester.PumpAndSettle();

            // The coefficient 0.01 is greater than the default of 0.0000135, so the translation
            // comes to a stop more quickly.
            Assert.True(controller2.Value.GetTranslation().Y < translation1Y);
        }
    }

    // Flutter: "discrete scroll pointer events"
    [Fact]
    public void DiscreteScrollPointerEvents()
    {
        using var controller = new TransformationController();
        using var tester = new FrameworkDartTester();
        const double boundaryMargin = 50.0;
        tester.PumpWidget(Standard(controller, boundaryMargin: EdgeInsets.All(boundaryMargin)));

        Assert.Equal(1.0, controller.Value.GetMaxScaleOnAxis());
        Vector3 translation = controller.Value.GetTranslation();
        Assert.Equal(0, translation.X);
        Assert.Equal(0, translation.Y);

        // Send a mouse scroll event, it should cause a scale.
        var mouse = new TestPointer(tester, 1, PointerDeviceKind.Mouse);
        tester.SendEventToBinding(mouse.Hover(tester.GetCenter(Find.ByType<SizedBox>())));
        tester.SendEventToBinding(mouse.Scroll(new Vector(300, -200)));
        tester.Pump();
        Assert.Equal(2.5, controller.Value.GetMaxScaleOnAxis());
        translation = controller.Value.GetTranslation();
        // Will be translated to maintain centering.
        Assert.Equal(-150, translation.X);
        Assert.Equal(-150, translation.Y);

        // Send a trackpad scroll event, it should cause a pan and no scale.
        var trackpad = new TestPointer(tester, 1, PointerDeviceKind.Trackpad);
        tester.SendEventToBinding(trackpad.Hover(tester.GetCenter(Find.ByType<SizedBox>())));
        tester.SendEventToBinding(trackpad.Scroll(new Vector(100, -25)));
        tester.Pump();
        Assert.Equal(2.5, controller.Value.GetMaxScaleOnAxis());
        translation = controller.Value.GetTranslation();
        Assert.Equal(-250, translation.X);
        Assert.Equal(-125, translation.Y);
    }

    // Flutter: "discrete scale pointer event"
    [Fact]
    public void DiscreteScalePointerEvent()
    {
        using var controller = new TransformationController();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Standard(controller, boundaryMargin: EdgeInsets.All(50.0)));

        Assert.Equal(1.0, controller.Value.GetMaxScaleOnAxis());

        // Send a scale event.
        var pointer = new TestPointer(tester, 1, PointerDeviceKind.Trackpad);
        tester.SendEventToBinding(pointer.Hover(tester.GetCenter(Find.ByType<SizedBox>())));
        tester.SendEventToBinding(pointer.Scale(1.5));
        tester.Pump();
        Assert.Equal(1.5, controller.Value.GetMaxScaleOnAxis());

        // Send another scale event.
        tester.SendEventToBinding(pointer.Scale(1.5));
        tester.Pump();
        Assert.Equal(2.25, controller.Value.GetMaxScaleOnAxis());

        // Send another scale event. Will not be allowed to scale beyond maxScale.
        tester.SendEventToBinding(pointer.Scale(1.5));
        tester.Pump();
        Assert.Equal(2.5, controller.Value.GetMaxScaleOnAxis());
    }

    // Flutter: "trackpadScrollCausesScale"
    [Fact]
    public void TrackpadScrollCausesScale()
    {
        using var controller = new TransformationController();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Standard(
            controller,
            boundaryMargin: EdgeInsets.All(50.0),
            trackpadScrollCausesScale: true));

        Assert.Equal(1.0, controller.Value.GetMaxScaleOnAxis());

        // Send a vertical scroll.
        var pointer = new TestPointer(tester, 1, PointerDeviceKind.Trackpad);
        Point center = tester.GetCenter(Find.ByType<SizedBox>());
        tester.SendEventToBinding(pointer.PanZoomStart(center));
        tester.Pump();
        Assert.Equal(1.0, controller.Value.GetMaxScaleOnAxis());
        tester.SendEventToBinding(pointer.PanZoomUpdate(center, pan: new Point(0, -81)));
        tester.Pump();
        AssertMoreOrLessEquals(1.499302500056767, controller.Value.GetMaxScaleOnAxis());

        // Send a horizontal scroll (should have no effect).
        tester.SendEventToBinding(pointer.PanZoomUpdate(center, pan: new Point(81, -81)));
        tester.Pump();
        AssertMoreOrLessEquals(1.499302500056767, controller.Value.GetMaxScaleOnAxis());
    }

    // Flutter: "trackpad pointer scroll events cause scale"
    [Fact]
    public void TrackpadPointerScrollEventsCauseScale()
    {
        using var controller = new TransformationController();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Standard(
            controller,
            boundaryMargin: EdgeInsets.All(50.0),
            trackpadScrollCausesScale: true));

        Assert.Equal(1.0, controller.Value.GetMaxScaleOnAxis());

        // Send a vertical scroll.
        var pointer = new TestPointer(tester, 1, PointerDeviceKind.Trackpad);
        Point center = tester.GetCenter(Find.ByType<SizedBox>());
        tester.SendEventToBinding(pointer.Hover(center));
        tester.Pump();
        Assert.Equal(1.0, controller.Value.GetMaxScaleOnAxis());
        tester.SendEventToBinding(pointer.Scroll(new Vector(0, -138.0)));
        tester.Pump();
        AssertMoreOrLessEquals(1.9937155332430823, controller.Value.GetMaxScaleOnAxis());
        Vector3 translation = controller.Value.GetTranslation();
        AssertMoreOrLessEquals(-99.37155332430822, translation.X);
        AssertMoreOrLessEquals(-99.37155332430822, translation.Y);

        // Send a horizontal scroll (should have no effect).
        tester.SendEventToBinding(pointer.Scroll(new Vector(-138, 0)));
        tester.Pump();
        AssertMoreOrLessEquals(1.9937155332430823, controller.Value.GetMaxScaleOnAxis());
        translation = controller.Value.GetTranslation();
        AssertMoreOrLessEquals(-99.37155332430822, translation.X);
        AssertMoreOrLessEquals(-99.37155332430822, translation.Y);
    }

    // Flutter: "Scaling inertia"
    [Fact]
    public void ScalingInertia()
    {
        using var controller = new TransformationController();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Standard(
            controller,
            boundaryMargin: EdgeInsets.All(50.0),
            trackpadScrollCausesScale: true));

        Assert.Equal(1.0, controller.Value.GetMaxScaleOnAxis());

        // Send a vertical scroll fling, which will cause inertia.
        tester.TrackpadFling(tester.Element(Find.ByType<InteractiveViewer>()), new Vector(0, -100), 3000);
        tester.Pump();
        AssertMoreOrLessEquals(1.6487212707001282, controller.Value.GetMaxScaleOnAxis());
        tester.Pump(TimeSpan.FromMilliseconds(80));
        AssertMoreOrLessEquals(1.7966838346780103, controller.Value.GetMaxScaleOnAxis());
        tester.PumpAndSettle();
        AssertMoreOrLessEquals(1.9984509673751225, controller.Value.GetMaxScaleOnAxis());
        tester.Pump(TimeSpan.FromSeconds(10));
        AssertMoreOrLessEquals(1.9984509673751225, controller.Value.GetMaxScaleOnAxis());
    }

    // Flutter: "does not accumulate listeners"
    [Fact]
    public void DoesNotAccumulateListeners()
    {
        using var controller = new TransformationController();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new InteractiveViewer(
            transformationController: controller,
            child: new SizedBox(width: 2000.0, height: 2000.0)));

        var events = new List<ObjectEvent>();
        void Listener(ObjectEvent @event)
        {
            if (@event.Object is CurvedAnimation)
            {
                events.Add(@event);
            }
        }

        FlutterMemoryAllocations.Instance.AddListener(Listener);
        try
        {
            for (int i = 0; i < 10; i++)
            {
                tester.Fling(Find.ByType<InteractiveViewer>(), new Vector(0, -100), 3000);
                tester.Pump();
            }
        }
        finally
        {
            FlutterMemoryAllocations.Instance.RemoveListener(Listener);
        }

        // CurvedAnimation must not be repeatedly created without disposal (flutter#185468).
        Assert.True(events.Count <= 2);
    }

    // ------------------------------------------------------------- group('getNearestPointOnLine')

    // Flutter: "does not modify parameters"
    [Fact]
    public void GetNearestPointOnLine_DoesNotModifyParameters()
    {
        var point = new Vector3(5.0, 5.0, 0.0);
        var a = new Vector3(0.0, 0.0, 0.0);
        var b = new Vector3(10.0, 0.0, 0.0);

        Vector3 closestPoint = InteractiveViewer.GetNearestPointOnLine(point, a, b);

        Assert.Equal(new Vector3(5.0, 0.0, 0.0), closestPoint);
        Assert.Equal(new Vector3(5.0, 5.0, 0.0), point);
        Assert.Equal(new Vector3(0.0, 0.0, 0.0), a);
        Assert.Equal(new Vector3(10.0, 0.0, 0.0), b);
    }

    // Flutter: "simple example", "closest to a", "closest to b", "point already on the line returns
    // the point"
    [Fact]
    public void GetNearestPointOnLine_Examples()
    {
        var a = new Vector3(0.0, 0.0, 0.0);
        var b = new Vector3(5.0, 5.0, 0.0);
        Assert.Equal(
            new Vector3(2.5, 2.5, 0.0),
            InteractiveViewer.GetNearestPointOnLine(new Vector3(0.0, 5.0, 0.0), a, b));
        Assert.Equal(a, InteractiveViewer.GetNearestPointOnLine(new Vector3(-1.0, -1.0, 0.0), a, b));
        Assert.Equal(b, InteractiveViewer.GetNearestPointOnLine(new Vector3(6.0, 6.0, 0.0), a, b));
        var point = new Vector3(2.0, 2.0, 0.0);
        Assert.Equal(point, InteractiveViewer.GetNearestPointOnLine(point, a, b));
    }

    // Flutter: "real example"
    [Fact]
    public void GetNearestPointOnLine_RealExample()
    {
        Vector3 closestPoint = InteractiveViewer.GetNearestPointOnLine(
            new Vector3(-436.9, 433.6, 0.0),
            new Vector3(-1114.0, -60.3, 0.0),
            new Vector3(288.8, 432.7, 0.0));
        AssertMoreOrLessEquals(-356.8, closestPoint.X, 0.1);
        AssertMoreOrLessEquals(205.8, closestPoint.Y, 0.1);
    }

    // --------------------------------------------------------- group('getAxisAlignedBoundingBox')

    // Flutter: "rectangle already axis aligned returns the rectangle"
    [Fact]
    public void GetAxisAlignedBoundingBox_RectangleAlreadyAxisAlignedReturnsTheRectangle()
    {
        Quad quad = Quad.Points(
            new Vector3(0.0, 0.0, 0.0),
            new Vector3(10.0, 0.0, 0.0),
            new Vector3(10.0, 10.0, 0.0),
            new Vector3(0.0, 10.0, 0.0));

        Quad aabb = InteractiveViewer.GetAxisAlignedBoundingBox(quad);

        Assert.Equal(quad.Point0, aabb.Point0);
        Assert.Equal(quad.Point1, aabb.Point1);
        Assert.Equal(quad.Point2, aabb.Point2);
        Assert.Equal(quad.Point3, aabb.Point3);
    }

    // Flutter: "rectangle rotated by 45 degrees", "rectangle rotated very slightly", "example from
    // hexagon board"
    [Theory]
    [InlineData(0.0, 5.0, 5.0, 10.0, 10.0, 5.0, 5.0, 0.0, 0.0, 0.0, 10.0, 10.0)]
    [InlineData(0.0, 1.0, 1.0, 11.0, 11.0, 9.0, 9.0, -1.0, 0.0, -1.0, 11.0, 11.0)]
    [InlineData(-462.7, 165.9, 690.6, -576.7, 1188.1, 196.0, 34.9, 938.6, -462.7, -576.7, 1188.1, 938.6)]
    public void GetAxisAlignedBoundingBox_RotatedQuads(
        double x0,
        double y0,
        double x1,
        double y1,
        double x2,
        double y2,
        double x3,
        double y3,
        double minX,
        double minY,
        double maxX,
        double maxY)
    {
        Quad quad = Quad.Points(
            new Vector3(x0, y0, 0.0),
            new Vector3(x1, y1, 0.0),
            new Vector3(x2, y2, 0.0),
            new Vector3(x3, y3, 0.0));

        Quad aabb = InteractiveViewer.GetAxisAlignedBoundingBox(quad);

        Assert.Equal(new Vector3(minX, minY, 0.0), aabb.Point0);
        Assert.Equal(new Vector3(maxX, minY, 0.0), aabb.Point1);
        Assert.Equal(new Vector3(maxX, maxY, 0.0), aabb.Point2);
        Assert.Equal(new Vector3(minX, maxY, 0.0), aabb.Point3);
    }

    // ------------------------------------------------------------------- group('pointIsInside')

    // Flutter: "inside", "outside", "on the edge"
    [Theory]
    [InlineData(5.0, 5.0, true)]
    [InlineData(12.0, 0.0, false)]
    [InlineData(0.0, 0.0, true)]
    public void PointIsInside(double x, double y, bool expected)
    {
        Quad quad = Quad.Points(
            new Vector3(0.0, 0.0, 0.0),
            new Vector3(0.0, 10.0, 0.0),
            new Vector3(10.0, 10.0, 0.0),
            new Vector3(10.0, 0.0, 0.0));

        Assert.Equal(expected, InteractiveViewer.PointIsInside(new Vector3(x, y, 0.0), quad));
    }

    // ---------------------------------------------------------- group('getNearestPointInside')

    // Flutter: "point already inside quad", "axis aligned quad"
    [Fact]
    public void GetNearestPointInside_AxisAlignedQuad()
    {
        Quad quad = Quad.Points(
            new Vector3(0.0, 0.0, 0.0),
            new Vector3(0.0, 10.0, 0.0),
            new Vector3(10.0, 10.0, 0.0),
            new Vector3(10.0, 0.0, 0.0));

        var inside = new Vector3(5.0, 5.0, 0.0);
        Assert.Equal(inside, InteractiveViewer.GetNearestPointInside(inside, quad));
        Assert.Equal(
            new Vector3(5.0, 10.0, 0.0),
            InteractiveViewer.GetNearestPointInside(new Vector3(5.0, 15.0, 0.0), quad));
    }

    // Flutter: "not axis aligned quad"
    [Fact]
    public void GetNearestPointInside_NotAxisAlignedQuad()
    {
        Quad quad = Quad.Points(
            new Vector3(0.0, 0.0, 0.0),
            new Vector3(2.0, 10.0, 0.0),
            new Vector3(12.0, 12.0, 0.0),
            new Vector3(10.0, 2.0, 0.0));

        Vector3 nearestInside = InteractiveViewer.GetNearestPointInside(new Vector3(5.0, 15.0, 0.0), quad);
        AssertMoreOrLessEquals(5.8, nearestInside.X, 0.1);
        AssertMoreOrLessEquals(10.8, nearestInside.Y, 0.1);
    }

    // ------------------------------------------------------------------------------- top level

    // Flutter: "InteractiveViewer does not crash at zero area"
    [Fact]
    public void InteractiveViewerDoesNotCrashAtZeroArea()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            textDirection: TextDirection.Ltr,
            child: new Center(child: SizedBox.Shrink(child: new InteractiveViewer(child: new Text("X"))))));

        Assert.Equal(default, tester.GetSize(Find.ByType<InteractiveViewer>()));
    }

    // -------------------------------------------------------------------------- C#-only checks

    [Fact]
    public void ConstructorDefaultsMatchDart()
    {
        var viewer = new InteractiveViewer(child: new SizedBox());
        Assert.Equal(Clip.HardEdge, viewer.ClipBehavior);
        Assert.Equal(PanAxis.Free, viewer.PanAxis);
        Assert.Equal(EdgeInsets.Zero, viewer.BoundaryMargin);
        Assert.True(viewer.Constrained);
        Assert.Equal(2.5, viewer.MaxScale);
        Assert.Equal(0.8, viewer.MinScale);
        Assert.Equal(0.0000135, viewer.InteractionEndFrictionCoefficient);
        Assert.True(viewer.PanEnabled);
        Assert.True(viewer.ScaleEnabled);
        Assert.Equal(ScaleGestureRecognizer.KDefaultMouseScrollToScaleFactor, viewer.ScaleFactor);
        Assert.False(viewer.TrackpadScrollCausesScale);
        Assert.Null(viewer.Alignment);
        Assert.Null(viewer.Builder_);

        InteractiveViewer built = InteractiveViewer.Builder(builder: (_, _) => new SizedBox());
        Assert.False(built.Constrained);
        Assert.Equal(200.0, built.ScaleFactor);
        Assert.Null(built.Child);
        Assert.NotNull(built.Builder_);
    }

    [Fact]
    public void ConstructorAssertsInvalidScales()
    {
        Assert.Throws<AssertionError>(() => new InteractiveViewer(child: new SizedBox(), minScale: 0.0));
        Assert.Throws<AssertionError>(() => new InteractiveViewer(child: new SizedBox(), maxScale: 0.5));
        Assert.Throws<AssertionError>(() => new InteractiveViewer(
            child: new SizedBox(),
            boundaryMargin: EdgeInsets.Only(left: double.PositiveInfinity)));
    }

    [Fact]
    public void TransformationControllerToScene()
    {
        using var controller = new TransformationController();
        Assert.Equal(Matrix4.Identity(), controller.Value);
        Assert.Equal(new Point(10.0, 20.0), controller.ToScene(new Point(10.0, 20.0)));

        Matrix4 matrix = Matrix4.Identity();
        matrix.TranslateByDouble(50.0, -10.0, 0.0, 1.0);
        matrix.ScaleByDouble(2.0, 2.0, 2.0, 1.0);
        controller.Value = matrix;
        Assert.Equal(new Point(25.0, 15.0), controller.ToScene(new Point(100.0, 20.0)));
    }

    // ------------------------------------------------------------------------------- helpers

    private static Widget Standard(
        TransformationController controller,
        EdgeInsets? boundaryMargin = null,
        PanAxis panAxis = PanAxis.Free,
        double minScale = 0.8,
        bool scaleEnabled = true,
        bool trackpadScrollCausesScale = false,
        InteractionRecorder? recorder = null)
    {
        return new Center(child: new InteractiveViewer(
            boundaryMargin: boundaryMargin,
            panAxis: panAxis,
            minScale: minScale,
            scaleEnabled: scaleEnabled,
            trackpadScrollCausesScale: trackpadScrollCausesScale,
            transformationController: controller,
            onInteractionStart: recorder is null ? null : _ => recorder.CalledStart = true,
            onInteractionUpdate: recorder is null
                ? null
                : details =>
                {
                    recorder.ScaleChange = details.Scale;
                    recorder.FocalPoint = details.FocalPoint;
                    recorder.LocalFocalPoint = details.LocalFocalPoint;
                },
            onInteractionEnd: recorder is null ? null : details => recorder.CurrentVelocity = details.Velocity,
            child: new SizedBox(width: 200.0, height: 200.0)));
    }

    // The test's standard one-finger drag: down, pump, move, pump, up, settle.
    private static void Drag(FrameworkDartTester tester, Point from, Point to)
    {
        TestGesture gesture = tester.StartGesture(from, PointerDeviceKind.Touch);
        tester.Pump();
        gesture.MoveTo(to);
        tester.Pump();
        gesture.Up();
        tester.PumpAndSettle();
    }

    // The test's standard two-finger gesture through createGesture: both downs, pump, both moves,
    // pump, both ups, settle.
    private static void Pinch(FrameworkDartTester tester, Point start1, Point start2, Point end1, Point end2)
    {
        TestGesture gesture = tester.CreateGesture();
        TestGesture gesture2 = tester.CreateGesture();
        gesture.Down(start1);
        gesture2.Down(start2);
        tester.Pump();
        gesture.MoveTo(end1);
        gesture2.MoveTo(end2);
        tester.Pump();
        gesture.Up();
        gesture2.Up();
        tester.PumpAndSettle();
    }

    // The same two-finger gesture through startGesture, which sends each down as it is created.
    private static void StartedPinch(FrameworkDartTester tester, Point start1, Point start2, Point end1, Point end2)
    {
        TestGesture gesture = tester.StartGesture(start1, PointerDeviceKind.Touch);
        TestGesture gesture2 = tester.StartGesture(start2, PointerDeviceKind.Touch);
        tester.Pump();
        gesture.MoveTo(end1);
        gesture2.MoveTo(end2);
        tester.Pump();
        gesture.Up();
        gesture2.Up();
        tester.PumpAndSettle();
    }

    // gesture_utils.dart's scrollAt: a fresh mouse pointer whose hover only sets its location, then
    // one scroll event.
    private static void ScrollAt(Point position, FrameworkDartTester tester, Vector offset)
    {
        var testPointer = new TestPointer(tester, 1, PointerDeviceKind.Mouse);
        testPointer.Hover(position);
        tester.SendEventToBinding(testPointer.Scroll(offset));
    }

    // The test file's _axisAlignedBoundingBox.
    private static Rect AxisAlignedBoundingBox(Quad quad)
    {
        double? xMin = null;
        double? xMax = null;
        double? yMin = null;
        double? yMax = null;
        foreach (Vector3 point in new[] { quad.Point0, quad.Point1, quad.Point2, quad.Point3 })
        {
            if (xMin is null || point.X < xMin)
            {
                xMin = point.X;
            }

            if (xMax is null || point.X > xMax)
            {
                xMax = point.X;
            }

            if (yMin is null || point.Y < yMin)
            {
                yMin = point.Y;
            }

            if (yMax is null || point.Y > yMax)
            {
                yMax = point.Y;
            }
        }

        return new Rect(new Point(xMin!.Value, yMin!.Value), new Point(xMax!.Value, yMax!.Value));
    }

    private static void AssertMoreOrLessEquals(double expected, double actual, double epsilon = Eps)
    {
        Assert.True(
            Math.Abs(expected - actual) <= epsilon,
            $"Expected {expected} (±{epsilon}) but was {actual}.");
    }

    private sealed class InteractionRecorder
    {
        public bool CalledStart { get; set; }

        public bool CalledUpdate { get; set; }

        public bool CalledEnd { get; set; }

        public double? ScaleChange { get; set; }

        public Point? FocalPoint { get; set; }

        public Point? LocalFocalPoint { get; set; }

        public Velocity? CurrentVelocity { get; set; }
    }
}
