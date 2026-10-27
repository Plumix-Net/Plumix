// Dart parity source: flutter/packages/flutter/lib/src/widgets/snapshot_widget.dart
// Mirrors flutter/packages/flutter/test/widgets/snapshot_widget_test.dart. The two golden-file tests
// sample the RepaintBoundary's image instead of comparing against the checked-in PNGs.
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using TextDirection = Plumix.UI.TextDirection;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class SnapshotWidgetDartParityTests
{
    private static readonly Color KYellowColor = new(0xFFAABB11);
    private static readonly Color KRedColor = new(0xFFAA0000);
    private static readonly Color KBlackColor = new(0xFF000000);
    private static readonly Color KBlueColor = new(0xFF0000FF);

    // Flutter: "SnapshotWidget can rasterize child"
    [Fact]
    public void SnapshotWidgetCanRasterizeChild()
    {
        using var tester = new FrameworkDartTester();
        var controller = new SnapshotController(allowSnapshotting: true);
        try
        {
            Key key = new UniqueKey();
            Widget Build(Color color) => new RepaintBoundary(
                key: key,
                child: new TestDependencies(
                    child: new SnapshotWidget(
                        controller: controller,
                        child: new Container(width: 100, height: 100, color: color))));

            tester.PumpWidget(Build(KYellowColor));
            AssertBoundaryColor(tester, key, KYellowColor);

            // Now change the color and assert the old snapshot still matches.
            tester.PumpWidget(Build(KRedColor));
            AssertBoundaryColor(tester, key, KYellowColor);

            // Now invoke clear and the raster is re-generated.
            controller.Clear();
            tester.Pump();

            AssertBoundaryColor(tester, key, KRedColor);
        }
        finally
        {
            controller.Dispose();
        }
    }

    // Flutter: "Changing devicePixelRatio does not repaint if snapshotting is not enabled"
    [Fact]
    public void ChangingDevicePixelRatioDoesNotRepaintIfSnapshottingIsNotEnabled()
    {
        using var tester = new FrameworkDartTester();
        var controller = new SnapshotController();
        var painter = new TestPainter();
        try
        {
            double devicePixelRatio = 1.0;
            StateSetter? localSetState = null;

            tester.PumpWidget(new StatefulBuilder((context, setState) =>
            {
                localSetState = setState;
                return new Center(
                    child: new TestDependencies(
                        devicePixelRatio: devicePixelRatio,
                        child: new SnapshotWidget(
                            controller: controller,
                            painter: painter,
                            child: new SizedBox(width: 100, height: 100))));
            }));

            Assert.Equal(1, painter.Count);

            localSetState!(() => devicePixelRatio = 2.0);
            tester.Pump();

            // Not repainted as dpr was not used.
            Assert.Equal(1, painter.Count);
        }
        finally
        {
            painter.Dispose();
            controller.Dispose();
        }
    }

    // Flutter: "Changing devicePixelRatio forces raster regeneration"
    [Fact]
    public void ChangingDevicePixelRatioForcesRasterRegeneration()
    {
        using var tester = new FrameworkDartTester();
        var controller = new SnapshotController(allowSnapshotting: true);
        var painter = new TestPainter();
        try
        {
            double devicePixelRatio = 1.0;
            StateSetter? localSetState = null;

            tester.PumpWidget(new StatefulBuilder((context, setState) =>
            {
                localSetState = setState;
                return new Center(
                    child: new TestDependencies(
                        devicePixelRatio: devicePixelRatio,
                        child: new SnapshotWidget(
                            controller: controller,
                            painter: painter,
                            child: new SizedBox(width: 100, height: 100))));
            }));
            IImage? raster = painter.LastImage;

            Assert.NotNull(raster);
            Assert.Equal(1, painter.Count);

            localSetState!(() => devicePixelRatio = 2.0);
            tester.Pump();

            IImage? newRaster = painter.LastImage;

            Assert.Equal(2, painter.Count);
            Assert.NotSame(raster, newRaster);
        }
        finally
        {
            painter.Dispose();
            controller.Dispose();
        }
    }

    // Flutter: "SnapshotWidget paints its child as a single picture layer"
    [Fact]
    public void SnapshotWidgetPaintsItsChildAsASinglePictureLayer()
    {
        using var tester = new FrameworkDartTester();
        var controller = new SnapshotController(allowSnapshotting: true);
        try
        {
            tester.PumpWidget(new RepaintBoundary(
                child: new Center(
                    child: new TestDependencies(
                        child: new SnapshotWidget(
                            controller: controller,
                            child: new Container(width: 100, height: 100, color: KYellowColor))))));

            List<Layer> layers = PlatformViewTestKit.Layers(tester);
            Assert.Equal(3, layers.Count);
            Assert.IsType<PictureLayer>(layers[^1]);

            controller.AllowSnapshotting = false;
            tester.Pump();

            layers = PlatformViewTestKit.Layers(tester);
            Assert.Equal(3, layers.Count);
            Assert.IsType<PictureLayer>(layers[^1]);
        }
        finally
        {
            controller.Dispose();
        }
    }

    // Flutter: "SnapshotWidget can update the painter type"
    [Fact]
    public void SnapshotWidgetCanUpdateThePainterType()
    {
        using var tester = new FrameworkDartTester();
        var controller = new SnapshotController(allowSnapshotting: true);
        var painter1 = new TestPainter();
        var painter2 = new TestPainter2();
        try
        {
            tester.PumpWidget(new Center(
                child: new TestDependencies(
                    child: new SnapshotWidget(controller: controller, painter: painter1, child: new SizedBox()))));

            tester.PumpWidget(new Center(
                child: new TestDependencies(
                    child: new SnapshotWidget(controller: controller, painter: painter2, child: new SizedBox()))));

            Assert.Null(tester.TakeException());
        }
        finally
        {
            painter2.Dispose();
            painter1.Dispose();
            controller.Dispose();
        }
    }

    // Flutter: "RenderSnapshotWidget does not error on rasterization of child with empty size"
    [Fact]
    public void RenderSnapshotWidgetDoesNotErrorOnRasterizationOfChildWithEmptySize()
    {
        using var tester = new FrameworkDartTester();
        var controller = new SnapshotController(allowSnapshotting: true);
        try
        {
            tester.PumpWidget(new Center(
                child: new TestDependencies(
                    child: new SnapshotWidget(controller: controller, child: new SizedBox()))));

            Assert.Null(tester.TakeException());
        }
        finally
        {
            controller.Dispose();
        }
    }

    // Flutter: "RenderSnapshotWidget throws assertion if platform view is encountered"
    [Fact]
    public void RenderSnapshotWidgetThrowsAssertionIfPlatformViewIsEncountered()
    {
        using var tester = new FrameworkDartTester();
        var controller = new SnapshotController(allowSnapshotting: true);
        try
        {
            tester.PumpWidget(new Center(
                child: new TestDependencies(
                    child: new SnapshotWidget(
                        controller: controller,
                        child: new SizedBox(width: 100, height: 100, child: new TestPlatformView())))));

            var error = Assert.IsType<FlutterError>(tester.TakeException());
            Assert.Contains("SnapshotWidget used with a child that contains a PlatformView", error.Message);
        }
        finally
        {
            controller.Dispose();
        }
    }

    // Flutter: "RenderSnapshotWidget does not assert if SnapshotMode.forced"
    [Fact]
    public void RenderSnapshotWidgetDoesNotAssertIfSnapshotModeForced()
    {
        using var tester = new FrameworkDartTester();
        var controller = new SnapshotController(allowSnapshotting: true);
        try
        {
            tester.PumpWidget(new Center(
                child: new TestDependencies(
                    child: new SnapshotWidget(
                        controller: controller,
                        mode: SnapshotMode.Forced,
                        child: new SizedBox(width: 100, height: 100, child: new TestPlatformView())))));

            Assert.Null(tester.TakeException());
        }
        finally
        {
            controller.Dispose();
        }
    }

    // Flutter: "RenderSnapshotWidget does not take a snapshot if a platform view is encountered with
    // SnapshotMode.permissive"
    [Fact]
    public void RenderSnapshotWidgetDoesNotTakeASnapshotIfAPlatformViewIsEncounteredWithSnapshotModePermissive()
    {
        using var tester = new FrameworkDartTester();
        var controller = new SnapshotController(allowSnapshotting: true);
        try
        {
            tester.PumpWidget(new Center(
                child: new TestDependencies(
                    child: new SnapshotWidget(
                        controller: controller,
                        mode: SnapshotMode.Permissive,
                        child: new SizedBox(width: 100, height: 100, child: new TestPlatformView())))));

            Assert.Null(tester.TakeException());
            Assert.IsType<PlatformViewLayer>(PlatformViewTestKit.Layers(tester)[^1]);
        }
        finally
        {
            controller.Dispose();
        }
    }

    // Flutter: "SnapshotWidget should have same result when enabled"
    [Fact]
    public void SnapshotWidgetShouldHaveSameResultWhenEnabled()
    {
        using var tester = new FrameworkDartTester();
        tester.View.PhysicalSize = new Size(10, 10);
        tester.View.DevicePixelRatio = 1;
        var repaintBoundaryKey = new ValueKey<string>("boundary");
        var controller = new SnapshotController();
        try
        {
            tester.PumpWidget(new RepaintBoundary(
                key: repaintBoundaryKey,
                child: new TestWidgetsApp(
                    home: new Container(
                        color: KBlackColor,
                        padding: EdgeInsets.Only(right: 0.6, bottom: 0.6),
                        child: new SnapshotWidget(
                            controller: controller,
                            child: new Container(
                                margin: EdgeInsets.Only(right: 0.4, bottom: 0.4),
                                color: KBlueColor))))));

            using Bitmap imageWhenDisabled =
                tester.RenderObject<RenderRepaintBoundary>(Find.ByKey(repaintBoundaryKey)).ToImageSync();

            controller.AllowSnapshotting = true;
            tester.Pump();

            using Bitmap imageWhenEnabled =
                tester.RenderObject<RenderRepaintBoundary>(Find.ByKey(repaintBoundaryKey)).ToImageSync();
            Assert.Equal(imageWhenDisabled.PixelSize, imageWhenEnabled.PixelSize);
            Assert.Equal(RasterBackend.ReadPixels(imageWhenDisabled), RasterBackend.ReadPixels(imageWhenEnabled));
        }
        finally
        {
            controller.Dispose();
            tester.View.ResetPhysicalSize();
            tester.View.ResetDevicePixelRatio();
        }
    }

    // Flutter: "SnapshotPainter dispatches memory events"
    [Fact]
    public void SnapshotPainterDispatchesMemoryEvents()
    {
        var events = new List<ObjectEvent>();
        void Listener(ObjectEvent e)
        {
            if (e.Object.GetType() == typeof(TestPainter))
            {
                events.Add(e);
            }
        }

        FlutterMemoryAllocations.Instance.AddListener(Listener);
        try
        {
            new TestPainter().Dispose();
        }
        finally
        {
            FlutterMemoryAllocations.Instance.RemoveListener(Listener);
        }

        if (!Constants.KDebugMode)
        {
            Assert.Empty(events);
            return;
        }

        Assert.Collection(
            events,
            e =>
            {
                ObjectCreated created = Assert.IsType<ObjectCreated>(e);
                Assert.Equal("package:flutter/foundation.dart", created.Library);
                Assert.Equal("ChangeNotifier", created.ClassName);
            },
            e => Assert.IsType<ObjectDisposed>(e));
        Assert.Same(events[0].Object, events[1].Object);
    }

    private static void AssertBoundaryColor(FrameworkDartTester tester, Key key, Color expected)
    {
        using Bitmap image = tester.RenderObject<RenderRepaintBoundary>(Find.ByKey(key)).ToImageSync();
        Assert.Equal((expected.Red, expected.Green, expected.Blue, expected.Alpha), RasterBackend.PixelAt(image, 50, 50));
    }

    private sealed class TestPlatformView : SingleChildRenderObjectWidget
    {
        public TestPlatformView() : base(null, null)
        {
        }

        public override RenderObject CreateRenderObject(BuildContext context) => new RenderTestPlatformView();
    }

    private sealed class RenderTestPlatformView : RenderProxyBox
    {
        public override void Paint(PaintingContext context, Point offset)
        {
            context.AddLayer(new PlatformViewLayer(new Rect(offset, Size), viewId: 1));
        }
    }

    private class TestPainter : SnapshotPainter
    {
        public int Count { get; private set; }

        public bool ShouldRepaintValue { get; set; }

        public IImage? LastImage { get; private set; }

        public int AddedListenerCount { get; private set; }

        public int RemovedListenerCount { get; private set; }

        public override void AddListener(Action listener)
        {
            AddedListenerCount += 1;
            base.AddListener(listener);
        }

        public override void RemoveListener(Action listener)
        {
            RemovedListenerCount += 1;
            base.RemoveListener(listener);
        }

        public override void PaintSnapshot(
            PaintingContext context,
            Point offset,
            Size size,
            IImage image,
            Size sourceSize,
            double pixelRatio)
        {
            Count += 1;
            LastImage = image;
        }

        public override void Paint(PaintingContext context, Point offset, Size size, PaintingContextCallback painter)
        {
            Count += 1;
        }

        public override bool ShouldRepaint(SnapshotPainter oldPainter) => ShouldRepaintValue;
    }

    private sealed class TestPainter2 : TestPainter
    {
        public override bool ShouldRepaint(SnapshotPainter oldPainter) => ShouldRepaintValue;
    }

    private sealed class TestDependencies(Widget child, double? devicePixelRatio = null) : StatelessWidget
    {
        public override Widget Build(BuildContext context)
        {
            return new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MediaQuery(
                    data: new MediaQueryData().CopyWith(devicePixelRatio: devicePixelRatio),
                    child: child));
        }
    }
}
