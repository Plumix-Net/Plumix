// Dart parity source: material_ui/lib/src/ink_splash.dart
// Mirrors material-ui-src/test/ink_splash_test.dart

using Plumix.Foundation;
using Plumix.UI;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;
using MaterialWidget = Plumix.Material.Material;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class InkSplashDartParityTests
{
    // Regression test for https://github.com/flutter/flutter/issues/21506.
    // Flutter: "InkSplash receives textDirection"
    [Fact]
    public void InkSplashReceivesTextDirection()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(
            new MaterialApp(
                home: new Scaffold(
                    appBar: new AppBar(title: new Text("Button Border Test")),
                    body: new Center(child: new ElevatedButton(child: new Text("Test"), onPressed: () => { })))));
        Tap(tester, tester.ElementsWithText("Test").Single());
        // start ink animation which asserts for a textDirection.
        PumpAndSettle(tester, TimeSpan.FromMilliseconds(30));
        Assert.Null(tester.TakeException());
    }

    // Flutter: "Material2 - InkWell with NoSplash splashFactory paints nothing"
    [Fact]
    public void Material2InkWellWithNoSplashSplashFactoryPaintsNothing()
    {
        using FrameworkDartTester tester = CreateTester();
        Widget BuildFrame(InteractiveInkFeatureFactory? splashFactory = null)
        {
            return new MaterialApp(
                theme: new ThemeData(useMaterial3: false),
                home: new Scaffold(
                    body: new Center(
                        child: new MaterialWidget(
                            child: new InkWell(
                                splashFactory: splashFactory,
                                onTap: () => { },
                                child: new Text("test"))))));
        }

        // NoSplash.splashFactory, no splash circles drawn
        tester.PumpWidget(BuildFrame(splashFactory: NoSplash.SplashFactory));
        {
            TestGesture gesture = StartGesture(tester, tester.GetCenter(TextElement(tester)));
            RenderObject material = MaterialOf(TextElement(tester));
            tester.Pump(TimeSpan.FromMilliseconds(200));
            Assert.Equal(0, PaintRecording.Record(material).CountCalls("drawCircle"));
            gesture.Up();
            tester.PumpAndSettle();
        }

        // Default splashFactory (from Theme.of().splashFactory), one splash circle drawn.
        tester.PumpWidget(BuildFrame());
        {
            TestGesture gesture = StartGesture(tester, tester.GetCenter(TextElement(tester)));
            RenderObject material = MaterialOf(TextElement(tester));
            tester.Pump(TimeSpan.FromMilliseconds(200));
            Assert.Equal(1, PaintRecording.Record(material).CountCalls("drawCircle"));
            gesture.Up();
            tester.PumpAndSettle();
        }
    }

    // Flutter: "Material3 - InkWell with NoSplash splashFactory paints nothing"
    [Fact]
    public void Material3InkWellWithNoSplashSplashFactoryPaintsNothing()
    {
        using FrameworkDartTester tester = CreateTester();
        Widget BuildFrame(InteractiveInkFeatureFactory? splashFactory = null)
        {
            return new MaterialApp(
                home: new Scaffold(
                    body: new Center(
                        child: new MaterialWidget(
                            child: new InkWell(
                                splashFactory: splashFactory,
                                onTap: () => { },
                                child: new Text("test"))))));
        }

        // NoSplash.splashFactory, one rect is drawn for the highlight.
        tester.PumpWidget(BuildFrame(splashFactory: NoSplash.SplashFactory));
        {
            TestGesture gesture = StartGesture(tester, tester.GetCenter(TextElement(tester)));
            RenderObject material = MaterialOf(TextElement(tester));
            tester.Pump(TimeSpan.FromMilliseconds(200));
            Assert.Equal(1, PaintRecording.Record(material).CountCalls("drawRect"));
            gesture.Up();
            tester.PumpAndSettle();
        }

        // Default splashFactory (from Theme.of().splashFactory), two rects are drawn for the splash and highlight.
        tester.PumpWidget(BuildFrame());
        {
            TestGesture gesture = StartGesture(tester, tester.GetCenter(TextElement(tester)));
            RenderObject material = MaterialOf(TextElement(tester));
            tester.Pump(TimeSpan.FromMilliseconds(200));
            Assert.Equal(2, PaintRecording.Record(material).CountCalls("drawRect")); // (kIsWeb ? 1 : 2)
            gesture.Up();
            tester.PumpAndSettle();
        }
    }

    // Regression test for https://github.com/flutter/flutter/issues/136441.
    // Flutter: "PageView item can dispose when widget with NoSplash.splashFactory is tapped"
    [Fact]
    public void PageViewItemCanDisposeWhenWidgetWithNoSplashSplashFactoryIsTapped()
    {
        using FrameworkDartTester tester = CreateTester();
        var controller = new PageController();
        var disposedPageIndexes = new List<int>();
        tester.PumpWidget(
            new MaterialApp(
                theme: new ThemeData(splashFactory: NoSplash.SplashFactory),
                home: new Scaffold(
                    body: PageView.Builder(
                        controller: controller,
                        itemBuilder: (context, index) => new TestPage(
                            title: $"Page {index}",
                            onDispose: () => disposedPageIndexes.Add(index)),
                        itemCount: 3))));
        controller.JumpToPage(1);
        tester.PumpAndSettle();
        Tap(tester, tester.ElementsWithText("Page 1").Single());
        tester.PumpAndSettle();
        controller.JumpToPage(0);
        tester.PumpAndSettle();
        Assert.Equal([0, 1], disposedPageIndexes);
        controller.Dispose();
    }

    private static FrameworkDartTester CreateTester() => new(fakeGestureTimers: true);

    private static Element TextElement(FrameworkDartTester tester) => tester.ElementsWithText("test").Single();

    // `Material.of(element)`, the `_RenderInkFeatures` render box.
    private static RenderObject MaterialOf(Element element) => (RenderInkFeatures)MaterialWidget.Of(element);

    private static TestGesture StartGesture(FrameworkDartTester tester, Avalonia.Point location) =>
        tester.StartGesture(location, PointerDeviceKind.Touch);

    // `tester.tap(finder)`: a down and an up at the center, no pump.
    private static void Tap(FrameworkDartTester tester, Element element) =>
        StartGesture(tester, tester.GetCenter(element)).Up();

    // `tester.pumpAndSettle(step)`.
    private static void PumpAndSettle(FrameworkDartTester tester, TimeSpan step)
    {
        int count = 0;
        do
        {
            Assert.True(count++ < 1000, "pumpAndSettle timed out");
            tester.Pump(step);
        }
        while (Scheduler.HasScheduledFrame || Scheduler.TransientCallbackCount > 0);
    }

    // The test file's `Page`: a FilledButton page that reports its disposal.
    private sealed class TestPage(string title, Action? onDispose, Key? key = null) : StatefulWidget(key)
    {
        public string Title { get; } = title;

        public Action? OnDispose { get; } = onDispose;

        public override State CreateState() => new TestPageState();
    }

    private sealed class TestPageState : State<TestPage>
    {
        public override void Dispose()
        {
            Widget.OnDispose?.Invoke();
            base.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            return new Center(child: new FilledButton(onPressed: () => { }, child: new Text(Widget.Title)));
        }
    }
}
