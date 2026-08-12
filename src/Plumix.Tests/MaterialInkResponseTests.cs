using Avalonia;
using Avalonia.Media;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialInkResponseTests : IDisposable
{
    public MaterialInkResponseTests()
    {
        Scheduler.ResetForTests();
        FocusManager.Instance.ResetForTests();
        GestureBinding.Instance.ResetForTests();
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
        FocusManager.Instance.ResetForTests();
        GestureBinding.Instance.ResetForTests();
    }

    [Fact]
    public void Ink_ValidatesShorthandAndDimensions()
    {
        Assert.Throws<ArgumentException>(() => new Ink(
            color: Colors.Red,
            decoration: new BoxDecoration(Color: Colors.Blue)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Ink(width: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Ink(padding: new Thickness(-1, 0, 0, 0)));

        var provider = new MemoryImage([1, 2, 3]);
        var image = Ink.Image(provider);
        var decoration = Assert.IsType<BoxDecoration>(image.Decoration);
        Assert.NotNull(decoration.Image);
        Assert.Same(provider, decoration.Image!.Image);
    }

    [Fact]
    public void Ink_PaintsDecorationBelowInkWellAndAppliesPaddingAndSize()
    {
        using var harness = CreateHarness(new Ink(
            width: 100,
            height: 56,
            padding: new Thickness(8, 4),
            color: Color.Parse("#FFEADDFF"),
            child: new InkWell(
                onTap: () => { },
                child: new Center(child: new Text("Ink")))));
        harness.Pump(new Size(160, 100));

        var decoration = Assert.Single(FindDescendants<RenderDecoratedBox>(harness.RenderView));
        Assert.Equal(Color.Parse("#FFEADDFF"), decoration.Decoration.Color);
        Assert.Equal(100, decoration.Size.Width, 3);
        Assert.Equal(56, decoration.Size.Height, 3);
        Assert.Contains(
            FindDescendants<RenderPadding>(harness.RenderView),
            padding => padding.Padding == new Thickness(8, 4));
        Assert.Single(FindDescendants<RenderInkResponsePaint>(harness.RenderView));
    }

    [Fact]
    public void Ink_WithoutChild_ExpandsToTheParentConstraints()
    {
        using var harness = CreateHarness(new Ink(color: Colors.Blue));
        harness.Pump(new Size(160, 100));

        var decoration = Assert.Single(FindDescendants<RenderDecoratedBox>(harness.RenderView));
        Assert.Equal(new Size(160, 100), decoration.Size);
    }

    [Fact]
    public void Material_OwnsInkDecorationAndResponseFeaturesBelowItsChild()
    {
        using var harness = CreateHarness(new Plumix.Material.Material(
            color: Colors.White,
            child: new Ink(
                color: Colors.Blue,
                child: new InkWell(
                    onTap: () => { },
                    child: new SizedBox(width: 80.0, height: 48.0)))));
        harness.Pump(new Size(120.0, 80.0));

        RenderMaterialInkFeatures controller = Assert.Single(
            FindDescendants<RenderMaterialInkFeatures>(harness.RenderView));
        RenderInkDecoration decoration = Assert.Single(
            FindDescendants<RenderInkDecoration>(harness.RenderView));
        RenderInkResponsePaint response = Assert.Single(
            FindDescendants<RenderInkResponsePaint>(harness.RenderView));

        Assert.Equal(2, controller.FeatureCount);
        Assert.Same(controller.Controller, decoration.Controller);
        Assert.Same(controller.Controller, response.Controller);
    }

    [Fact]
    public void InkWell_RapidTapsKeepOlderFadingSplashAlive()
    {
        using var harness = CreateHarness(new Plumix.Material.Material(
            child: new InkWell(
                splashFactory: InkRipple.SplashFactory,
                onTap: () => { },
                child: new SizedBox(width: 80.0, height: 48.0))));
        harness.Pump(new Size(120.0, 80.0));

        DateTime now = DateTime.UtcNow;
        Tap(harness, pointer: 801, now: now);
        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerDownEvent(
                802,
                PointerDeviceKind.Mouse,
                new Point(30.0, 20.0),
                PointerButtons.Primary,
                now.AddMilliseconds(30.0)));
        harness.Pump(new Size(120.0, 80.0));

        RenderInkResponsePaint response = Assert.Single(
            FindDescendants<RenderInkResponsePaint>(harness.RenderView));
        Assert.Equal(2, response.SplashCount);
    }

    [Fact]
    public void InkWell_HoverHighlightUsesConfiguredFadeDuration()
    {
        Color hoverColor = Color.Parse("#FF00AA00");
        using var harness = CreateHarness(new Plumix.Material.Material(
            child: new InkWell(
                hoverColor: hoverColor,
                hoverDuration: TimeSpan.FromMilliseconds(100.0),
                onTap: () => { },
                child: new SizedBox(width: 80.0, height: 48.0))));
        harness.Pump(new Size(120.0, 80.0));

        RenderPointerListener hoverListener = FindDescendants<RenderPointerListener>(harness.RenderView)
            .Single(listener => listener.OnPointerEnter is not null && listener.OnPointerExit is not null);
        hoverListener.HandleEvent(
            new PointerEnterEvent(
                803,
                PointerDeviceKind.Mouse,
                new Point(10.0, 10.0),
                PointerButtons.None,
                DateTime.UtcNow),
            new BoxHitTestEntry(hoverListener, new Point(10.0, 10.0)));
        harness.Pump(new Size(120.0, 80.0));

        PumpAnimation(harness, new Size(120.0, 80.0), TimeSpan.FromMilliseconds(50.0));
        RenderInkResponsePaint response = Assert.Single(
            FindDescendants<RenderInkResponsePaint>(harness.RenderView));
        InkHighlightVisual hover = Assert.Single(
            response.Highlights!,
            highlight => highlight.Kind == InkHighlightKind.Hover);

        Assert.Equal(hoverColor, hover.Color);
        Assert.InRange(hover.Opacity, 0.49, 0.51);
    }

    [Fact]
    public void NestedInkWells_CreateOnlyTheInnerSplash()
    {
        using var harness = CreateHarness(new Plumix.Material.Material(
            child: new InkWell(
                onTap: () => { },
                child: new InkWell(
                    onTap: () => { },
                    child: new SizedBox(width: 80.0, height: 48.0)))));
        harness.Pump(new Size(120.0, 80.0));

        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerDownEvent(
                804,
                PointerDeviceKind.Mouse,
                new Point(20.0, 20.0),
                PointerButtons.Primary,
                DateTime.UtcNow));
        harness.Pump(new Size(120.0, 80.0));

        List<RenderInkResponsePaint> responses = FindDescendants<RenderInkResponsePaint>(harness.RenderView);
        Assert.Equal(2, responses.Count);
        Assert.Equal(0, responses[0].SplashCount);
        Assert.Equal(1, responses[1].SplashCount);
    }

    [Fact]
    public void CircleMaterialUsesOvalClipForNonSquareChildren()
    {
        using var harness = CreateHarness(new Plumix.Material.Material(
            type: MaterialType.Circle,
            clipBehavior: Clip.AntiAlias,
            child: new SizedBox(width: 80.0, height: 48.0)));
        harness.Pump(new Size(120.0, 80.0));

        RenderClipPath clip = Assert.Single(FindDescendants<RenderClipPath>(harness.RenderView));
        var clipper = Assert.IsType<ShapeBorderClipper>(clip.Clipper);
        Assert.IsType<CircleBorder>(clipper.Shape);
        Assert.Empty(FindDescendants<RenderClipRRect>(harness.RenderView));
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
        Assert.Throws<ArgumentOutOfRangeException>(() => new InkResponse(radius: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InkWell(hoverDuration: TimeSpan.FromMilliseconds(-1)));
    }

    [Fact]
    public void ThemeData_SplashFactoryDefaultsMatchMaterialModeAndPlatform()
    {
        var material3Android = new ThemeData(
            platform: TargetPlatform.Android,
            useMaterial3: true);
        var material3Windows = new ThemeData(
            platform: TargetPlatform.Windows,
            useMaterial3: true);
        var material2Android = new ThemeData(
            platform: TargetPlatform.Android,
            useMaterial3: false);

        Assert.Same(InkSparkle.SplashFactory, material3Android.SplashFactory);
        Assert.Same(InkRipple.SplashFactory, material3Windows.SplashFactory);
        Assert.Same(Plumix.Material.InkSplash.SplashFactory, material2Android.SplashFactory);

        var explicitTheme = new ThemeData(splashFactory: InkRipple.SplashFactory);
        Assert.Same(InkRipple.SplashFactory, explicitTheme.SplashFactory);
    }

    [Fact]
    public void InkRipple_MatchesFlutterRadiusCenterAndTimingContract()
    {
        var feature = Assert.IsType<InkRipple>(InkRipple.SplashFactory.Create(
            new InkFeatureConfiguration(
                Position: new Point(10.0, 20.0),
                Color: Colors.Blue,
                ContainedInkWell: true)));

        Assert.Equal(TimeSpan.FromSeconds(1.0), feature.UnconfirmedDuration);
        Assert.Equal(TimeSpan.FromMilliseconds(375.0), feature.ConfirmDuration);
        Assert.Equal(TimeSpan.FromMilliseconds(75.0), feature.CancelDuration);

        InkFeatureFrame initial = feature.ResolveFrame(
            new Size(100.0, 60.0),
            progress: 0.0,
            confirmed: true,
            canceled: false);
        InkFeatureFrame completed = feature.ResolveFrame(
            new Size(100.0, 60.0),
            progress: 1.0,
            confirmed: true,
            canceled: false);
        double targetRadius = Math.Sqrt((100.0 * 100.0) + (60.0 * 60.0)) / 2.0;

        Assert.Equal(InkFeatureKind.Ripple, initial.Kind);
        Assert.Equal(new Point(10.0, 20.0), initial.Center);
        Assert.Equal(targetRadius * 0.30, initial.Radius, 3);
        Assert.Equal(new Point(50.0, 30.0), completed.Center);
        Assert.Equal(targetRadius + 5.0, completed.Radius, 3);
        Assert.Equal(0.0, completed.Opacity, 3);
    }

    [Fact]
    public void InkSparkle_UsesFlutterSequencesAndDeterministicTestSeed()
    {
        var feature = Assert.IsType<InkSparkle>(
            InkSparkle.ConstantTurbulenceSeedSplashFactory.Create(
                new InkFeatureConfiguration(
                    Position: new Point(12.0, 18.0),
                    Color: Colors.Purple,
                    ContainedInkWell: true)));

        Assert.Equal(TimeSpan.FromMilliseconds(617.0), feature.ConfirmDuration);
        Assert.Equal(1337.0, feature.TurbulenceSeed);

        InkFeatureFrame hold = feature.ResolveFrame(
            new Size(100.0, 60.0),
            progress: 0.30,
            confirmed: true,
            canceled: false);
        InkFeatureFrame completed = feature.ResolveFrame(
            new Size(100.0, 60.0),
            progress: 1.0,
            confirmed: true,
            canceled: false);

        Assert.Equal(InkFeatureKind.Sparkle, hold.Kind);
        Assert.Equal(1.0, hold.Opacity, 3);
        Assert.Equal(1.0, hold.SparkleOpacity, 3);
        Assert.True(hold.Radius > 0.0);
        Assert.Equal(0.0, completed.Opacity, 3);
        Assert.Equal(0.0, completed.SparkleOpacity, 3);
    }

    [Fact]
    public void NoSplash_FactoryCreatesNonPaintingImmediateFeature()
    {
        var configuration = new InkFeatureConfiguration(
            Position: new Point(12.0, 18.0),
            Color: Colors.Blue,
            ContainedInkWell: true);
        var feature = Assert.IsType<NoSplash>(NoSplash.SplashFactory.Create(configuration));

        Assert.Equal(TimeSpan.Zero, feature.UnconfirmedDuration);
        Assert.Equal(TimeSpan.Zero, feature.ConfirmDuration);
        Assert.Equal(TimeSpan.Zero, feature.CancelDuration);

        InkFeatureFrame frame = feature.ResolveFrame(
            new Size(100.0, 60.0),
            progress: 0.5,
            confirmed: true,
            canceled: false);
        Assert.Equal(InkFeatureKind.None, frame.Kind);
        Assert.Equal(new Point(12.0, 18.0), frame.Center);
        Assert.Equal(0.0, frame.Radius);
        Assert.Equal(0.0, frame.Opacity);
    }

    [Fact]
    public void NoSplash_CanOverrideInkWellAndButtonStyleFactories()
    {
        using var harness = CreateHarness(new InkWell(
            splashFactory: NoSplash.SplashFactory,
            onTap: () => { },
            child: new SizedBox(width: 80.0, height: 48.0)));
        harness.Pump(new Size(120.0, 80.0));

        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerDownEvent(
                707,
                PointerDeviceKind.Mouse,
                new Point(20.0, 20.0),
                PointerButtons.Primary,
                DateTime.UtcNow));
        harness.Pump(new Size(120.0, 80.0));

        RenderInkResponsePaint paint = Assert.Single(
            FindDescendants<RenderInkResponsePaint>(harness.RenderView));
        Assert.IsType<NoSplash>(paint.SplashFeature);
        Assert.Same(
            NoSplash.SplashFactory,
            TextButton.StyleFrom(splashFactory: NoSplash.SplashFactory).SplashFactory);

        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerUpEvent(
                707,
                PointerDeviceKind.Mouse,
                new Point(20.0, 20.0),
                PointerButtons.None,
                DateTime.UtcNow.AddMilliseconds(20.0)));
        harness.Pump(new Size(120.0, 80.0));
        Assert.Null(Assert.Single(FindDescendants<RenderInkResponsePaint>(harness.RenderView)).SplashFeature);
    }

    [Fact]
    public void InkWell_WidgetSplashFactoryOverridesThemeFactory()
    {
        var theme = new ThemeData(
            platform: TargetPlatform.Android,
            splashFactory: InkSparkle.SplashFactory);
        using var harness = CreateHarness(
            new InkWell(
                splashFactory: InkRipple.SplashFactory,
                onTap: () => { },
                child: new SizedBox(width: 80.0, height: 48.0)),
            theme);
        harness.Pump(new Size(120.0, 80.0));

        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerDownEvent(
                705,
                PointerDeviceKind.Mouse,
                new Point(20.0, 20.0),
                PointerButtons.Primary,
                DateTime.UtcNow));
        harness.Pump(new Size(120.0, 80.0));

        RenderInkResponsePaint paint = Assert.Single(
            FindDescendants<RenderInkResponsePaint>(harness.RenderView));
        Assert.IsType<InkRipple>(paint.SplashFeature);
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
    public void MaterialButtonCore_UsesButtonStyleSplashFactory()
    {
        ButtonStyle style = TextButton.StyleFrom(
            foregroundColor: Colors.Blue,
            splashFactory: InkRipple.SplashFactory);
        using var harness = CreateHarness(new TextButton(
            onPressed: () => { },
            style: style,
            child: new Text("Ripple")));
        harness.Pump(new Size(160.0, 80.0));

        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerDownEvent(
                706,
                PointerDeviceKind.Mouse,
                new Point(30.0, 20.0),
                PointerButtons.Primary,
                DateTime.UtcNow));
        harness.Pump(new Size(160.0, 80.0));

        RenderInkResponsePaint paint = Assert.Single(
            FindDescendants<RenderInkResponsePaint>(harness.RenderView));
        Assert.IsType<InkRipple>(paint.SplashFeature);
    }

    [Fact]
    public void InkResponse_UsesCircleAndUncontainedSplashWhileInkWellClipsRectangle()
    {
        using var responseHarness = CreateHarness(new InkResponse(
            radius: 30,
            borderRadius: BorderRadius.Circular(12),
            onTap: () => { },
            child: new SizedBox(width: 80, height: 48)));
        responseHarness.Pump(new Size(120, 80));
        var responsePaint = Assert.Single(FindDescendants<RenderInkResponsePaint>(responseHarness.RenderView));
        Assert.Equal(BoxShape.Circle, responsePaint.HighlightShape);
        Assert.False(responsePaint.ContainedInkWell);
        Assert.Equal(30, responsePaint.SplashRadius);

        using var wellHarness = CreateHarness(new InkWell(
            radius: 24,
            borderRadius: BorderRadius.Circular(12),
            onTap: () => { },
            child: new SizedBox(width: 80, height: 48)));
        wellHarness.Pump(new Size(120, 80));
        var wellPaint = Assert.Single(FindDescendants<RenderInkResponsePaint>(wellHarness.RenderView));
        Assert.Equal(BoxShape.Rectangle, wellPaint.HighlightShape);
        Assert.True(wellPaint.ContainedInkWell);
        Assert.Equal(BorderRadius.Circular(12), wellPaint.BorderRadius);
    }

    [Fact]
    public void InkWell_PrimaryTapCallbacksAndStatesControllerFollowGestureLifecycle()
    {
        var events = new List<string>();
        var states = new MaterialStatesController();
        using var harness = CreateHarness(new InkWell(
            statesController: states,
            onTapDown: _ => events.Add("down"),
            onTapUp: _ => events.Add("up"),
            onHighlightChanged: value => events.Add(value ? "highlight-on" : "highlight-off"),
            onTap: () => events.Add("tap"),
            child: new SizedBox(width: 80, height: 48)));
        harness.Pump(new Size(120, 80));

        var now = DateTime.UtcNow;
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerDownEvent(
            701, PointerDeviceKind.Mouse, new Point(20, 20), PointerButtons.Primary, now));
        Assert.True(states.Value.HasFlag(MaterialState.Pressed));
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerUpEvent(
            701, PointerDeviceKind.Mouse, new Point(20, 20), PointerButtons.None, now.AddMilliseconds(20)));

        Assert.False(states.Value.HasFlag(MaterialState.Pressed));
        Assert.Equal(["highlight-on", "down", "up", "highlight-off", "tap"], events);
    }

    [Fact]
    public void InkResponse_SecondaryTapUsesDedicatedCallbacksWithoutPrimaryTap()
    {
        var events = new List<string>();
        using var harness = CreateHarness(new InkResponse(
            onTap: () => events.Add("primary"),
            onSecondaryTapDown: _ => events.Add("secondary-down"),
            onSecondaryTapUp: _ => events.Add("secondary-up"),
            onSecondaryTap: () => events.Add("secondary"),
            child: new SizedBox(width: 80, height: 48)));
        harness.Pump(new Size(120, 80));

        var now = DateTime.UtcNow;
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerDownEvent(
            702, PointerDeviceKind.Mouse, new Point(20, 20), PointerButtons.Secondary, now));
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerUpEvent(
            702, PointerDeviceKind.Mouse, new Point(20, 20), PointerButtons.None, now.AddMilliseconds(20)));

        Assert.Equal(["secondary-down", "secondary-up", "secondary"], events);
    }

    [Fact]
    public void InkResponse_OverlayColorResolvesHoveredAndPressedStates()
    {
        var hovered = Color.Parse("#2200FF00");
        var pressed = Color.Parse("#330000FF");
        var controller = new MaterialStatesController();
        using var harness = CreateHarness(new InkResponse(
            statesController: controller,
            overlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Pressed) ? pressed
                : states.HasFlag(MaterialState.Hovered) ? hovered
                : null),
            onTap: () => { },
            child: new SizedBox(width: 80, height: 48)));
        harness.Pump(new Size(120, 80));

        var hoverListener = FindDescendants<RenderPointerListener>(harness.RenderView)
            .Single(listener => listener.OnPointerEnter is not null && listener.OnPointerExit is not null);
        hoverListener.HandleEvent(
            new PointerEnterEvent(703, PointerDeviceKind.Mouse, new Point(10, 10), PointerButtons.None, DateTime.UtcNow),
            new BoxHitTestEntry(hoverListener, new Point(10, 10)));
        harness.Pump(new Size(120, 80));
        Assert.True(controller.Value.HasFlag(MaterialState.Hovered));
        Assert.Equal(hovered, Assert.Single(FindDescendants<RenderInkResponsePaint>(harness.RenderView)).HighlightColor);

        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerDownEvent(
            704, PointerDeviceKind.Mouse, new Point(20, 20), PointerButtons.Primary, DateTime.UtcNow));
        harness.Pump(new Size(120, 80));
        Assert.Equal(pressed, Assert.Single(FindDescendants<RenderInkResponsePaint>(harness.RenderView)).HighlightColor);
    }

    [Fact]
    public void InkResponse_SemanticsExposeOnlyConfiguredPrimaryActions()
    {
        int taps = 0;
        int longPresses = 0;
        using var harness = CreateHarness(new InkResponse(
            onTap: () => taps++,
            onLongPress: () => longPresses++,
            child: new SizedBox(width: 80, height: 48)));

        var semantics = harness.PumpAndGetSemantics(new Size(120, 80));
        var actionNode = FindSemantics(semantics, node =>
            node.Actions.HasFlag(SemanticsActions.Tap)
            && node.Actions.HasFlag(SemanticsActions.LongPress));
        Assert.NotNull(actionNode);
        Assert.True(actionNode!.PerformAction(SemanticsActions.Tap));
        Assert.True(actionNode.PerformAction(SemanticsActions.LongPress));
        Assert.Equal(1, taps);
        Assert.Equal(1, longPresses);

        using var excludedHarness = CreateHarness(new InkResponse(
            excludeFromSemantics: true,
            onTap: () => { },
            child: new SizedBox(width: 80, height: 48)));
        var excluded = excludedHarness.PumpAndGetSemantics(new Size(120, 80));
        Assert.Null(FindSemantics(excluded, node => node.Actions.HasFlag(SemanticsActions.Tap)));
    }

    private static WidgetRenderHarness CreateHarness(Widget child, ThemeData? theme = null) => new(
        new Theme(theme ?? ThemeData.Light, new Directionality(TextDirection.Ltr, child)));

    private static void Tap(WidgetRenderHarness harness, int pointer, DateTime now)
    {
        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerDownEvent(
                pointer,
                PointerDeviceKind.Mouse,
                new Point(20.0, 20.0),
                PointerButtons.Primary,
                now));
        GestureBinding.Instance.HandlePointerEvent(
            harness.RenderView,
            new PointerUpEvent(
                pointer,
                PointerDeviceKind.Mouse,
                new Point(20.0, 20.0),
                PointerButtons.None,
                now.AddMilliseconds(20.0)));
        harness.Pump(new Size(120.0, 80.0));
    }

    private static void PumpAnimation(WidgetRenderHarness harness, Size size, TimeSpan elapsed)
    {
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds) + elapsed);
        harness.Pump(size);
    }

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null) return result;
        if (root is T target) result.Add(target);
        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private static SemanticsNode? FindSemantics(SemanticsNode? node, Func<SemanticsNode, bool> predicate)
    {
        if (node is null) return null;
        if (predicate(node)) return node;
        foreach (var child in node.Children)
        {
            var result = FindSemantics(child, predicate);
            if (result is not null) return result;
        }
        return null;
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _rootElement;
        private readonly PipelineOwner _pipeline;

        public WidgetRenderHarness(Widget rootWidget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _rootElement = new HarnessRootElement(RenderView, rootWidget);
            _rootElement.Attach(_owner);
            _rootElement.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public SemanticsNode? PumpAndGetSemantics(Size size)
        {
            Pump(size);
            _pipeline.RequestSemanticsUpdate();
            _pipeline.FlushSemantics();
            return _pipeline.SemanticsOwner.RootNode;
        }

        public void Dispose() => _rootElement.Unmount();

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;

            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget) => _renderView = renderView;
            public override RenderObject? RenderObject => _child?.RenderObject;
            internal override Element? RenderObjectAttachingChild => _child;
            protected override void OnMount() { base.OnMount(); Rebuild(); }
            internal override void Rebuild() { Dirty = false; _child = UpdateChild(_child, Widget, Slot); }
            internal override void Update(Widget newWidget) { base.Update(newWidget); Rebuild(); }
            internal override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
            internal override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
            public void InsertRenderObjectChild(RenderObject child, object? slot) => _renderView.Child = (RenderBox)child;
            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
            public void RemoveRenderObjectChild(RenderObject child, object? slot) { if (ReferenceEquals(_renderView.Child, child)) _renderView.Child = null; }
            internal override void Unmount() { if (_child is not null) { UnmountChild(_child); _child = null; } base.Unmount(); }
        }
    }
}
