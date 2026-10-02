using Avalonia;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using MaterialWidget = Plumix.Material.Material;

namespace Plumix.Tests;

// Plumix-side contract checks for the ink family (ink_well.dart, ink_decoration.dart, ink_splash.dart,
// ink_ripple.dart, ink_sparkle.dart, ink_highlight.dart, no_splash.dart) that Flutter's own test files
// do not pin directly. Flutter's tests are ported in InkWellDartParityTests / InkPaintDartParityTests.
[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialInkResponseTests : IDisposable
{
    public MaterialInkResponseTests()
    {
        FocusManager.Instance.ResetForTests();
    }

    public void Dispose()
    {
        FocusManager.Instance.ResetForTests();
    }

    [Fact]
    public void InkResponseAndInkWell_DefaultGeometryMatchesFlutter()
    {
        var response = new InkResponse();
        var well = new InkWell();

        Assert.False(response.ContainedInkWell);
        Assert.Equal(BoxShape.Circle, response.HighlightShape);
        Assert.True(well.ContainedInkWell);
        Assert.Equal(BoxShape.Rectangle, well.HighlightShape);
        Assert.True(response.EnableFeedback);
        Assert.True(response.CanRequestFocus);
        Assert.False(response.Autofocus);
        Assert.False(response.ExcludeFromSemantics);
        Assert.Null(response.GetRectCallback(new RenderConstrainedBox(BoxConstraints.Tight(new Size(1, 1)))));
    }

    [Fact]
    public void ThemeData_SplashFactoryDefaultsMatchMaterialModeAndPlatform()
    {
        var material3Android = new ThemeData(platform: TargetPlatform.Android, useMaterial3: true);
        var material3Windows = new ThemeData(platform: TargetPlatform.Windows, useMaterial3: true);
        var material2Android = new ThemeData(platform: TargetPlatform.Android, useMaterial3: false);

        Assert.Same(InkSparkle.SplashFactory, material3Android.SplashFactory);
        Assert.Same(InkRipple.SplashFactory, material3Windows.SplashFactory);
        Assert.Same(InkSplash.SplashFactory, material2Android.SplashFactory);
        Assert.Same(InkRipple.SplashFactory, new ThemeData(splashFactory: InkRipple.SplashFactory).SplashFactory);
    }

    [Fact]
    public void ButtonStylesExposeSplashFactoryWithWidgetPrecedence()
    {
        ButtonStyle baseStyle = TextButton.StyleFrom(splashFactory: InkRipple.SplashFactory);
        ButtonStyle overrideStyle = FilledButton.StyleFrom(splashFactory: InkSparkle.SplashFactory);
        ButtonStyle merged = baseStyle.Merge(overrideStyle);

        Assert.Same(InkRipple.SplashFactory, baseStyle.SplashFactory);
        Assert.Same(InkSparkle.SplashFactory, overrideStyle.SplashFactory);
        Assert.Same(InkRipple.SplashFactory, merged.SplashFactory);
        Assert.Same(
            InkSparkle.SplashFactory,
            IconButton.StyleFrom(splashFactory: InkSparkle.SplashFactory).SplashFactory);
    }

    [Fact]
    public void Material_OwnsInkDecorationAndResponseFeaturesInPaintOrder()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Host(new Ink(
            color: Colors.Blue,
            child: new InkWell(
                onTap: () => { },
                splashFactory: InkRipple.SplashFactory,
                child: new SizedBox(width: 80.0, height: 48.0)))));

        Element well = Single<InkWell>(tester);
        TestGesture gesture = tester.StartGesture(tester.GetCenter(well), PointerDeviceKind.Touch);
        tester.Pump();

        RenderInkFeatures controller = Controller(well);
        // The Ink decoration first, then the ripple, then the pressed highlight that follows it.
        Assert.Collection(
            controller.DebugInkFeatures!,
            feature => Assert.IsType<InkDecoration>(feature),
            feature => Assert.IsType<InkRipple>(feature),
            feature => Assert.IsType<InkHighlight>(feature));
        gesture.Up();
        tester.PumpAndSettle();
        Assert.IsType<InkDecoration>(Assert.Single(controller.DebugInkFeatures!));
    }

    [Fact]
    public void InkWell_WidgetSplashFactoryOverridesThemeFactory()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Host(
            new InkWell(
                splashFactory: InkRipple.SplashFactory,
                onTap: () => { },
                child: new SizedBox(width: 80.0, height: 48.0)),
            new ThemeData(platform: TargetPlatform.Android, splashFactory: InkSparkle.SplashFactory)));

        Element well = Single<InkWell>(tester);
        TestGesture gesture = tester.StartGesture(tester.GetCenter(well), PointerDeviceKind.Touch);
        tester.Pump();
        Assert.IsType<InkRipple>(Assert.Single(InkFeatureProbe.Splashes(Controller(well))));
        gesture.Up();
        tester.PumpAndSettle();
    }

    [Fact]
    public void ButtonStyleButton_UsesButtonStyleSplashFactory()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Host(new TextButton(
            onPressed: () => { },
            style: TextButton.StyleFrom(splashFactory: NoSplash.SplashFactory),
            child: new Text("No splash"))));

        Element well = Single<InkWell>(tester);
        TestGesture gesture = tester.StartGesture(tester.GetCenter(well), PointerDeviceKind.Touch);
        tester.Pump();
        // Dart's NoSplash never adds itself to the controller: only the pressed highlight is there.
        Assert.Empty(InkFeatureProbe.Splashes(Controller(well)));
        Assert.Single(InkFeatureProbe.Highlights(Controller(well)));
        Assert.Equal(0, PaintRecording.Record(Controller(well)).CountCalls("drawCircle"));
        gesture.Up();
        tester.PumpAndSettle();
        Assert.Empty(Controller(well).DebugInkFeatures!);
    }

    [Fact]
    public void InkWell_PrimaryTapCallbacksAndStatesControllerFollowGestureLifecycle()
    {
        var events = new List<string>();
        var states = new WidgetStatesController();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Host(new InkWell(
            statesController: states,
            onTapDown: _ => events.Add("down"),
            onTapUp: _ => events.Add("up"),
            onHighlightChanged: value => events.Add(value ? "highlight-on" : "highlight-off"),
            onTap: () => events.Add("tap"),
            child: new SizedBox(width: 80, height: 48))));

        Element well = Single<InkWell>(tester);
        TestGesture gesture = tester.StartGesture(tester.GetCenter(well), PointerDeviceKind.Touch);
        Assert.Contains(WidgetState.Pressed, states.Value);
        gesture.Up();

        Assert.DoesNotContain(WidgetState.Pressed, states.Value);
        Assert.Equal(["highlight-on", "down", "up", "highlight-off", "tap"], events);
        tester.PumpAndSettle();
    }

    [Fact]
    public void InkSparkle_ComputesDartUniformsFromItsAnimations()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Host(new SizedBox(
            width: 100.0,
            height: 50.0,
            child: new InkWell(
                splashFactory: InkSparkle.ConstantTurbulenceSeedSplashFactory,
                splashColor: new Color(0x80FF0000),
                onTap: () => { }))));

        Element well = Single<InkWell>(tester);
        Point topLeft = tester.GetTopLeft(well);
        TestGesture gesture = tester.StartGesture(topLeft + new Vector(10.0, 10.0), PointerDeviceKind.Touch);
        tester.Pump();
        var sparkle = Assert.IsType<InkSparkle>(Assert.Single(InkFeatureProbe.Splashes(Controller(well))));
        PaintRecording.Record(Controller(well));

        IReadOnlyList<double> u = sparkle.DebugUniforms;
        Assert.Equal(1.0, u[0], 6);
        Assert.Equal(128.0 / 255.0, u[3], 6);
        // At t == 0 every sequence is at its start: no alpha, no radius, centre at the touch.
        Assert.Equal(0.0, u[4], 6);
        Assert.Equal(0.0, u[5], 6);
        Assert.Equal(1.0, u[6], 6);
        Assert.Equal(0.0, u[7], 6);
        Assert.Equal(10.0, u[8], 6);
        Assert.Equal(10.0, u[9], 6);
        // _getTargetRadius is half the diagonal; the sparkle multiplies it by 2.3.
        Assert.Equal(Math.Sqrt((100.0 * 100.0) + (50.0 * 50.0)) / 2.0 * 2.3, u[10], 6);
        Assert.Equal(1.0 / 100.0, u[11], 9);
        Assert.Equal(2.1 / 50.0, u[14], 9);
        // The constant turbulence seed is 1337 in tests.
        Assert.Equal(1337.0 / 1000.0, u[15], 9);

        // 617ms total: radius reaches 1 at 75% of it, the centre at half of that.
        tester.Pump(TimeSpan.FromMilliseconds(617.0 * 0.40));
        PaintRecording.Record(Controller(well));
        Assert.Equal(1.0, u[4], 6);
        Assert.Equal(1.0, u[5], 6);
        tester.Pump(TimeSpan.FromMilliseconds(617.0 * 0.40));
        PaintRecording.Record(Controller(well));
        Assert.Equal(1.0, u[7], 6);
        Assert.Equal(50.0, u[8], 6);
        Assert.Equal(25.0, u[9], 6);
        gesture.Up();
        tester.PumpAndSettle();
        Assert.Empty(InkFeatureProbe.Splashes(Controller(well)));
    }

    [Fact]
    public void InkHighlight_FadesInAndDisposesAfterDeactivateFadesOut()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey boxKey = new LabeledGlobalKey<State>("box");
        tester.PumpWidget(Host(new SizedBox(key: boxKey, width: 40.0, height: 20.0)));
        var controller = (RenderInkFeatures)MaterialWidget.Of(boxKey.CurrentContext!);
        bool removed = false;
        var highlight = new InkHighlight(
            controller: controller,
            referenceBox: (RenderBox)boxKey.CurrentContext!.FindRenderObject()!,
            color: new Color(0xFF00FF00),
            textDirection: TextDirection.Ltr,
            onRemoved: () => removed = true,
            fadeDuration: TimeSpan.FromMilliseconds(100));

        // The fade's ticker starts on the next frame.
        tester.Pump();
        PaintAssert.Paints(controller, PaintPattern.Paints.Rect(color: new Color(0x0000FF00)));
        tester.Pump(TimeSpan.FromMilliseconds(50));
        PaintAssert.Paints(controller, PaintPattern.Paints.Rect(color: new Color(0x8000FF00)));
        tester.Pump(TimeSpan.FromMilliseconds(50));
        PaintAssert.Paints(controller, PaintPattern.Paints.Rect(color: new Color(0xFF00FF00)));

        highlight.Deactivate();
        Assert.False(highlight.Active);
        tester.Pump();
        Assert.False(removed);
        tester.Pump(TimeSpan.FromMilliseconds(100));
        // An interpolation is done only once its time is strictly past the duration.
        tester.Pump(TimeSpan.FromMilliseconds(1));
        Assert.True(removed);
        Assert.Empty(controller.DebugInkFeatures!);
    }

    [Fact]
    public void InkSplash_UncontainedSplashRecentersAndUsesDefaultRadius()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Host(new SizedBox(
            width: 100.0,
            height: 100.0,
            child: new InkResponse(
                splashFactory: InkSplash.SplashFactory,
                splashColor: new Color(0xFF0000FF),
                onTap: () => { }))));

        Element response = Single<InkResponse>(tester);
        Point topLeft = tester.GetTopLeft(response);
        TestGesture gesture = tester.StartGesture(topLeft, PointerDeviceKind.Touch);
        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(500));
        // Uncontained: no clip of its own (only the Material's), Material.defaultSplashRadius (35)
        // target, centre halfway to the box centre.
        PaintAssert.Paints(
            Controller(response),
            PaintPattern.Paints.Translate(topLeft.X, topLeft.Y).Circle(x: 25.0, y: 25.0, radius: 17.5));
        Assert.Equal(1, PaintRecording.Record(Controller(response)).CountCalls("clipRect"));
        gesture.Up();
        tester.PumpAndSettle();
    }

    private static Widget Host(Widget child, ThemeData? theme = null) =>
        new Theme(
            theme ?? new ThemeData(useMaterial3: false),
            new Directionality(
                TextDirection.Ltr,
                new MaterialWidget(child: new Align(alignment: Alignment.TopLeft, child: child))));

    private static Element Single<TWidget>(FrameworkDartTester tester) where TWidget : Widget =>
        Assert.Single(tester.AllElements(), element => element.Widget is TWidget);

    private static RenderInkFeatures Controller(Element element) =>
        (RenderInkFeatures)MaterialWidget.Of(element);
}
