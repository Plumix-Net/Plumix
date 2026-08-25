using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialBottomSheetTests : IDisposable
{
    public MaterialBottomSheetTests()
    {
        Scheduler.ResetForTests();
        GestureBinding.Instance.ResetForTests();
    }

    public void Dispose()
    {
        GestureBinding.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void BottomSheetAndTheme_ValidateFlutterContracts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BottomSheet(() => { }, _ => new SizedBox(), elevation: -1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new BottomSheet(() => { }, _ => new SizedBox(), dragHandleSize: new Size(-1, 4)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BottomSheetThemeData(Elevation: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BottomSheetThemeData(ModalElevation: double.PositiveInfinity));
    }

    [Fact]
    public void BottomSheetThemeData_CopyWithLerpAndInheritedWrapMatchSource()
    {
        var shape = new RoundedRectangleBorder(borderRadius: BorderRadius.Circular(12));
        var data = new BottomSheetThemeData(
            BackgroundColor: Colors.Blue,
            SurfaceTintColor: Colors.Green,
            Elevation: 2,
            ModalBackgroundColor: Colors.Orange,
            ModalBarrierColor: Colors.Purple,
            ShadowColor: Colors.Red,
            ModalElevation: 4,
            Shape: shape,
            ShowDragHandle: true,
            DragHandleColor: Colors.White,
            DragHandleSize: new Size(32, 4),
            ClipBehavior: Clip.AntiAlias,
            Constraints: new BoxConstraints(MaxWidth: 640));

        Assert.Equal(data, data.CopyWith());
        Assert.Equal(Colors.Black, data.CopyWith(backgroundColor: Colors.Black).BackgroundColor);
        Assert.Same(data, BottomSheetThemeData.Lerp(data, data, 0.5));
        Assert.Null(BottomSheetThemeData.Lerp(null, null, 0.5));

        var diagnostics = new DiagnosticPropertiesBuilder();
        data.DebugFillProperties(diagnostics);
        Assert.Equal(
            [
                "backgroundColor",
                "surfaceTintColor",
                "elevation",
                "modalBackgroundColor",
                "shadowColor",
                "modalBarrierColor",
                "modalElevation",
                "shape",
                "showDragHandle",
                "dragHandleColor",
                "dragHandleSize",
                "clipBehavior",
                "constraints",
            ],
            diagnostics.Properties.Select(property => property.Name));

        var stateful = WidgetStateColor.ResolveWith(
            Colors.Blue,
            states => states.Contains(WidgetState.Hovered) ? Colors.Red : Colors.Blue);
        var lerped = BottomSheetThemeData.Lerp(
            new BottomSheetThemeData(DragHandleColor: stateful),
            new BottomSheetThemeData(DragHandleColor: Colors.Green),
            0.5);
        Assert.NotNull(lerped?.DragHandleColor);
        Assert.True(lerped!.DragHandleColor!.IsConstantColor);
        Assert.Equal(
            lerped.DragHandleColor.DefaultValue,
            lerped.DragHandleColor.Resolve(new HashSet<WidgetState> { WidgetState.Hovered }));

        var theme = new BottomSheetTheme(data, new SizedBox());
        var wrapped = Assert.IsType<BottomSheetTheme>(theme.Wrap(default, new Text("child")));
        Assert.Same(data, wrapped.Data);

        var replacement = new BottomSheetThemeData(BackgroundColor: Colors.Crimson);
        BottomSheetThemeData? resolved = null;
        Widget captureProbe = new BottomSheetTheme(
            data,
            new Builder(context =>
            {
                CapturedThemes capturedThemes = InheritedTheme.Capture(context);
                return new BottomSheetTheme(
                    replacement,
                    capturedThemes.Wrap(new Builder(capturedContext =>
                    {
                        resolved = BottomSheetTheme.Of(capturedContext);
                        return new SizedBox();
                    })));
            }));
        using var harness = new WidgetRenderHarness(Wrap(ThemeData.Light, captureProbe));
        harness.Pump(new Size(320, 120));
        Assert.Same(data, resolved);
    }

    [Fact]
    public void BottomSheet_Material3DefaultsMatchSource()
    {
        Color surfaceContainerLow = Color.Parse("#FF102030");
        Color onSurfaceVariant = Color.Parse("#FF405060");
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(
                surfaceContainerLow: surfaceContainerLow,
                onSurfaceVariant: onSurfaceVariant),
        };
        using var harness = new WidgetRenderHarness(Wrap(
            theme,
            new BottomSheet(
                onClosing: () => { },
                builder: _ => new SizedBox(width: 200, height: 80),
                animationController: BottomSheet.CreateAnimationController(),
                enableDrag: false,
                showDragHandle: true)));
        harness.Pump(new Size(800, 400));

        // Material 3 tokens: surfaceContainerLow, elevation 1 with a transparent shadow, and top-only 28px corners.
        var surface = Assert.Single(FindDescendants<RenderDecoratedBox>(harness.RenderView), box =>
            box.Decoration.Color == surfaceContainerLow);
        Assert.Equal(BorderRadius.Only(topLeft: 28.0, topRight: 28.0), surface.Decoration.EffectiveBorderRadius);
        Assert.Null(surface.Decoration.BoxShadows);
        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView), box =>
            box.Decoration.Color == onSurfaceVariant && box.Size == new Size(32, 4));
        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView), box =>
            box.AdditionalConstraints.MaxWidth == 640);
    }

    [Fact]
    public void BottomSheet_Material2DefaultsMatchSource()
    {
        var theme = ThemeData.Light with { UseMaterial3 = false };
        using var harness = new WidgetRenderHarness(Wrap(
            theme,
            new BottomSheet(
                onClosing: () => { },
                builder: _ => new SizedBox(width: 200, height: 80),
                animationController: BottomSheet.CreateAnimationController(),
                enableDrag: false)));
        harness.Pump(new Size(800, 400));

        // Material 2 supplies no defaults: the Material falls back to the canvas color, elevation 0 and no shape.
        var surface = Assert.Single(FindDescendants<RenderDecoratedBox>(harness.RenderView), box =>
            box.Decoration.Color == theme.CanvasColor);
        Assert.Equal(BorderRadius.Zero, surface.Decoration.EffectiveBorderRadius);
        Assert.Null(surface.Decoration.BoxShadows);
        Assert.DoesNotContain(FindDescendants<RenderConstrainedBox>(harness.RenderView), box =>
            box.AdditionalConstraints.MaxWidth == 640);
    }

    [Fact]
    public void BottomSheet_WidgetPropertiesTakePriorityOverTheme()
    {
        var theme = ThemeData.Light with
        {
            BottomSheetTheme = new BottomSheetThemeData(
                BackgroundColor: Colors.Purple,
                Elevation: 0,
                Shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(12)),
                ShowDragHandle: true,
                DragHandleColor: Colors.Green,
                DragHandleSize: new Size(40, 6),
                Constraints: new BoxConstraints(MaxWidth: 200)),
        };
        using var harness = new WidgetRenderHarness(Wrap(
            theme,
            new BottomSheet(
                onClosing: () => { },
                builder: _ => new SizedBox(width: 200, height: 80),
                animationController: BottomSheet.CreateAnimationController(),
                backgroundColor: Colors.Orange,
                shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(8)),
                showDragHandle: true,
                enableDrag: false)));
        var semantics = harness.PumpAndGetSemantics(new Size(800, 400));

        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView), box =>
            box.Decoration.Color == Colors.Orange
            && box.Decoration.EffectiveBorderRadius == BorderRadius.Circular(8));
        // The drag handle keeps resolving through the theme.
        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView), box =>
            box.Decoration.Color == Colors.Green
            && box.Decoration.EffectiveBorderRadius == BorderRadius.Circular(3));
        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView), box =>
            box.AdditionalConstraints.MaxWidth == 200);
        Assert.NotNull(FindSemantics(semantics, node =>
            node.Label == "Dismiss"
            && node.Flags.HasFlag(SemanticsFlags.IsButton)
            && node.Actions.HasFlag(SemanticsActions.Tap)));
    }

    [Fact]
    public void BottomSheet_ThemeValuesReachTheMaterial()
    {
        var theme = ThemeData.Light with
        {
            BottomSheetTheme = new BottomSheetThemeData(
                BackgroundColor: Colors.Purple,
                Elevation: 4,
                ShadowColor: Colors.Red,
                Shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(12))),
        };
        using var harness = new WidgetRenderHarness(Wrap(
            theme,
            new BottomSheet(
                onClosing: () => { },
                builder: _ => new SizedBox(width: 200, height: 80),
                animationController: BottomSheet.CreateAnimationController(),
                enableDrag: false)));
        harness.Pump(new Size(800, 400));

        var surface = Assert.Single(FindDescendants<RenderDecoratedBox>(harness.RenderView), box =>
            box.Decoration.EffectiveBorderRadius == BorderRadius.Circular(12));
        Assert.NotNull(surface.Decoration.BoxShadows);
    }

    [Fact]
    public void BottomSheet_DragHandleUsesMaterial3DefaultsAndHoverState()
    {
        var theme = ThemeData.Light with
        {
            BottomSheetTheme = new BottomSheetThemeData(
                DragHandleColor: WidgetStateColor.ResolveWith(
                    Colors.Green,
                    states => states.Contains(WidgetState.Hovered) ? Colors.Red : Colors.Green)),
        };
        using var harness = new WidgetRenderHarness(Wrap(
            theme,
            new BottomSheet(
                onClosing: () => { },
                builder: _ => new SizedBox(width: 200, height: 80),
                animationController: BottomSheet.CreateAnimationController(),
                showDragHandle: true,
                enableDrag: false)));
        harness.Pump(new Size(800, 400));

        // 32x4 pill inside a 48x48 interactive box, with the resting state color.
        var handle = Assert.Single(FindDescendants<RenderDecoratedBox>(harness.RenderView), box =>
            box.Decoration.Color == Colors.Green);
        Assert.Equal(new Size(32, 4), handle.Size);
        Assert.Equal(BorderRadius.Circular(2), handle.Decoration.EffectiveBorderRadius);
        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView), box =>
            box.AdditionalConstraints.MaxWidth == 48 && box.AdditionalConstraints.MaxHeight == 48);

        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerHoverEvent(
            9,
            PointerDeviceKind.Mouse,
            HandleCenter(harness),
            PointerButtons.None,
            DateTime.UtcNow));
        harness.Pump(new Size(800, 400));

        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView), box =>
            box.Decoration.Color == Colors.Red);
    }

    [Fact]
    public void BottomSheet_DragHandleInteractiveAreaFollowsDragHandleSize()
    {
        using var small = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new BottomSheet(
                onClosing: () => { },
                builder: _ => new SizedBox(width: 200, height: 80),
                animationController: BottomSheet.CreateAnimationController(),
                showDragHandle: true,
                enableDrag: false,
                dragHandleSize: new Size(20, 20))));
        small.Pump(new Size(800, 400));
        Assert.Contains(FindDescendants<RenderConstrainedBox>(small.RenderView), box =>
            box.AdditionalConstraints.MaxWidth == 48 && box.AdditionalConstraints.MaxHeight == 48);

        using var large = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new BottomSheet(
                onClosing: () => { },
                builder: _ => new SizedBox(width: 200, height: 80),
                animationController: BottomSheet.CreateAnimationController(),
                showDragHandle: true,
                enableDrag: false,
                dragHandleSize: new Size(100, 50))));
        large.Pump(new Size(800, 400));
        Assert.Contains(FindDescendants<RenderConstrainedBox>(large.RenderView), box =>
            box.AdditionalConstraints.MaxWidth == 100 && box.AdditionalConstraints.MaxHeight == 50);
    }

    [Fact]
    public void BottomSheet_DragHandleSemanticsPrecedeContent()
    {
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new BottomSheet(
                onClosing: () => { },
                builder: _ => new Text("Sheet content"),
                animationController: BottomSheet.CreateAnimationController(),
                showDragHandle: true,
                enableDrag: false)));
        var semantics = harness.PumpAndGetSemantics(new Size(800, 400));

        var order = CollectSemantics(semantics)
            .Where(node => node.Label is "Dismiss" or "Sheet content")
            .Select(node => node.Label)
            .ToList();
        Assert.Equal(["Dismiss", "Sheet content"], order);
    }

    [Fact]
    public void ModalLayout_UsesNineSixteenthsCapUnlessScrollControlled()
    {
        using var capped = new WidgetRenderHarness(new BottomSheetLayoutWithSizeListener(
            onChildSizeChanged: _ => { },
            animationValue: 1,
            isScrollControlled: false,
            scrollControlDisabledMaxHeightRatio: 9.0 / 16.0,
            child: new SizedBox(height: 1000)));
        capped.Pump(new Size(400, 320));
        var cappedLayout = Assert.Single(FindDescendants<RenderBottomSheetLayoutWithSizeListener>(capped.RenderView));
        Assert.Equal(180, cappedLayout.Child!.Size.Height, precision: 3);
        Assert.Equal(140, ((BoxParentData)cappedLayout.Child.parentData!).offset.Y, precision: 3);

        using var half = new WidgetRenderHarness(new BottomSheetLayoutWithSizeListener(
            onChildSizeChanged: _ => { },
            animationValue: 1,
            isScrollControlled: false,
            scrollControlDisabledMaxHeightRatio: 8.0 / 16.0,
            child: new SizedBox(height: 1000)));
        half.Pump(new Size(400, 320));
        Assert.Equal(
            160,
            Assert.Single(FindDescendants<RenderBottomSheetLayoutWithSizeListener>(half.RenderView)).Child!.Size.Height,
            precision: 3);

        using var full = new WidgetRenderHarness(new BottomSheetLayoutWithSizeListener(
            onChildSizeChanged: _ => { },
            animationValue: 1,
            isScrollControlled: true,
            scrollControlDisabledMaxHeightRatio: 9.0 / 16.0,
            child: new SizedBox(height: 1000)));
        full.Pump(new Size(400, 320));
        Assert.Equal(
            320,
            Assert.Single(FindDescendants<RenderBottomSheetLayoutWithSizeListener>(full.RenderView)).Child!.Size.Height,
            precision: 3);
    }

    [Fact]
    public void ModalLayout_ReportsChildSizeChangesOnce()
    {
        var reported = new List<Size>();
        using var harness = new WidgetRenderHarness(new BottomSheetLayoutWithSizeListener(
            onChildSizeChanged: size => reported.Add(size),
            animationValue: 0.5,
            isScrollControlled: true,
            scrollControlDisabledMaxHeightRatio: 9.0 / 16.0,
            child: new SizedBox(height: 120)));
        harness.Pump(new Size(400, 320));
        harness.Pump(new Size(400, 320));

        Assert.Equal(new Size(400, 120), Assert.Single(reported));
        var layout = Assert.Single(FindDescendants<RenderBottomSheetLayoutWithSizeListener>(harness.RenderView));
        Assert.Equal(260, ((BoxParentData)layout.Child!.parentData!).offset.Y, precision: 3);
    }

    [Fact]
    public void BottomSheet_DragBelowHalfClosesAndReportsClosing()
    {
        var controller = BottomSheet.CreateAnimationController();
        controller.SetValue(1);
        int closingCalls = 0;
        bool? reportedClosing = null;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new BottomSheet(
                onClosing: () => closingCalls++,
                builder: _ => new SizedBox(width: 240, height: 200),
                animationController: controller,
                onDragEnd: (_, closing) => reportedClosing = closing)));
        harness.Pump(new Size(240, 300));

        var binding = GestureBinding.Instance;
        var start = DateTime.UtcNow;
        DispatchDown(binding, harness.RenderView, 41, new Point(120, 20), start);
        DispatchMove(binding, harness.RenderView, 41, new Point(120, 145), start.AddMilliseconds(300));
        DispatchUp(binding, harness.RenderView, 41, new Point(120, 145), start.AddMilliseconds(600));

        Assert.True(reportedClosing);
        Assert.Equal(1, closingCalls);
        Assert.True(controller.Value < 0.5);
    }

    [Fact]
    public void BottomSheet_DragAboveThresholdReopensWithoutClosing()
    {
        var controller = BottomSheet.CreateAnimationController();
        controller.SetValue(1);
        int closingCalls = 0;
        bool? reportedClosing = null;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new BottomSheet(
                onClosing: () => closingCalls++,
                builder: _ => new SizedBox(width: 240, height: 200),
                animationController: controller,
                onDragEnd: (_, closing) => reportedClosing = closing)));
        harness.Pump(new Size(240, 300));

        var binding = GestureBinding.Instance;
        var start = DateTime.UtcNow;
        DispatchDown(binding, harness.RenderView, 42, new Point(120, 20), start);
        DispatchMove(binding, harness.RenderView, 42, new Point(120, 50), start.AddMilliseconds(300));
        DispatchUp(binding, harness.RenderView, 42, new Point(120, 50), start.AddMilliseconds(600));

        Assert.False(reportedClosing);
        Assert.Equal(0, closingCalls);
        Assert.Equal(AnimationStatus.Forward, controller.Status);
    }

    [Fact]
    public void BottomSheet_DragIsIgnoredWhenDragIsDisabledAndNoHandleIsShown()
    {
        var controller = BottomSheet.CreateAnimationController();
        controller.SetValue(1);
        int closingCalls = 0;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new BottomSheet(
                onClosing: () => closingCalls++,
                builder: _ => new SizedBox(width: 240, height: 200),
                animationController: controller,
                enableDrag: false)));
        harness.Pump(new Size(240, 300));

        var binding = GestureBinding.Instance;
        var start = DateTime.UtcNow;
        DispatchDown(binding, harness.RenderView, 43, new Point(120, 20), start);
        DispatchMove(binding, harness.RenderView, 43, new Point(120, 180), start.AddMilliseconds(300));
        DispatchUp(binding, harness.RenderView, 43, new Point(120, 180), start.AddMilliseconds(600));

        Assert.Equal(0, closingCalls);
        Assert.Equal(1.0, controller.Value, precision: 6);
    }

    [Fact]
    public void BottomSheet_DragWithoutAnimationControllerThrows()
    {
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new BottomSheet(
                onClosing: () => { },
                builder: _ => new SizedBox(width: 240, height: 200))));
        harness.Pump(new Size(240, 300));

        var binding = GestureBinding.Instance;
        var start = DateTime.UtcNow;
        DispatchDown(binding, harness.RenderView, 44, new Point(120, 20), start);
        Assert.Throws<InvalidOperationException>(() =>
            DispatchMove(binding, harness.RenderView, 44, new Point(120, 120), start.AddMilliseconds(200)));
    }

    [Fact]
    public async Task ModalBottomSheet_RouteKeepsUnderlyingPageAndReturnsTypedResult()
    {
        // The scrim is only exposed to semantics on platforms that support dismissing the barrier.
        TargetPlatform? previous = PlatformDefaults.DebugTargetPlatformOverride;
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        try
        {
            BuildContext captured = default;
            using var harness = new WidgetRenderHarness(Wrap(
                ThemeData.Light,
                new Navigator(new BuilderPageRoute(context => new CaptureContext(
                    value => captured = value,
                    child: new Text("Underlying"))))));
            harness.Pump(new Size(500, 400));

            var result = MaterialBottomSheets.ShowModalBottomSheet<string>(
                captured,
                _ => new SizedBox(height: 120, child: new Text("Modal sheet")));
            PumpAnimation();
            var semantics = harness.PumpAndGetSemantics(new Size(500, 400));

            Assert.NotNull(FindParagraph(harness.RenderView, "Underlying"));
            Assert.NotNull(FindParagraph(harness.RenderView, "Modal sheet"));
            // The scrim uses the localized default label and close hint; the sheet names/scopes the route.
            Assert.NotNull(FindSemantics(semantics, node =>
                node.Label == "Scrim"
                && node.OnTapHint == "Close Bottom Sheet"
                && node.Actions.HasFlag(SemanticsActions.Tap)));
            Assert.NotNull(FindSemantics(semantics, node =>
                node.Flags.HasFlag(SemanticsFlags.ScopesRoute)
                && node.Flags.HasFlag(SemanticsFlags.NamesRoute)));

            Navigator.Of(captured).Pop("done");
            PumpAnimation();
            harness.Pump(new Size(500, 400));
            Assert.Equal("done", await result);
            Assert.Null(FindParagraph(harness.RenderView, "Modal sheet"));
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = previous;
        }
    }

    [Theory]
    [InlineData(TargetPlatform.Android, "Dialog")]
    [InlineData(TargetPlatform.Windows, "Dialog")]
    [InlineData(TargetPlatform.IOS, "")]
    [InlineData(TargetPlatform.MacOS, "")]
    public void ModalBottomSheet_RouteLabelFollowsDefaultTargetPlatform(TargetPlatform platform, string expectedLabel)
    {
        TargetPlatform? previous = PlatformDefaults.DebugTargetPlatformOverride;
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        try
        {
            BuildContext captured = default;
            using var harness = new WidgetRenderHarness(Wrap(
                ThemeData.Light,
                new Navigator(new BuilderPageRoute(_ => new CaptureContext(
                    value => captured = value,
                    child: new Text("Underlying"))))));
            harness.Pump(new Size(500, 400));

            MaterialBottomSheets.ShowModalBottomSheet<string>(
                captured,
                _ => new SizedBox(height: 120, child: new Text("Modal sheet")));
            PumpAnimation();
            var semantics = harness.PumpAndGetSemantics(new Size(500, 400));

            var sheet = FindSemantics(semantics, node => node.Flags.HasFlag(SemanticsFlags.ScopesRoute));
            Assert.NotNull(sheet);
            Assert.Equal(expectedLabel, sheet.Label ?? string.Empty);
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = previous;
        }
    }

    [Fact]
    public void ModalBottomSheet_ClipsBarrierSemanticsToTheSheetHeight()
    {
        // The clipper is only inserted where the barrier participates in semantics.
        TargetPlatform? previous = PlatformDefaults.DebugTargetPlatformOverride;
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        try
        {
            BuildContext captured = default;
            using var harness = new WidgetRenderHarness(Wrap(
                ThemeData.Light,
                new Navigator(new BuilderPageRoute(_ => new CaptureContext(
                    value => captured = value,
                    child: new Text("Underlying"))))));
            harness.Pump(new Size(500, 400));

            MaterialBottomSheets.ShowModalBottomSheet<string>(
                captured,
                _ => new SizedBox(height: 120, child: new Text("Modal sheet")));
            PumpAnimation();
            harness.Pump(new Size(500, 400));

            var clipper = Assert.Single(FindDescendants<RenderSemanticsClipper>(harness.RenderView));
            Assert.Equal(120, clipper.ClipDetailsNotifier.Value.Bottom, precision: 3);
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = previous;
        }
    }

    [Fact]
    public void ModalBottomSheet_IsDismissibleFalseKeepsTheSheetOnBarrierTap()
    {
        BuildContext captured = default;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => new CaptureContext(
                value => captured = value,
                child: new Text("Underlying"))))));
        harness.Pump(new Size(500, 400));

        MaterialBottomSheets.ShowModalBottomSheet<string>(
            captured,
            _ => new SizedBox(height: 120, child: new Text("Modal sheet")),
            isDismissible: false);
        PumpAnimation();
        harness.Pump(new Size(500, 400));

        TapAt(harness, new Point(20, 20));
        PumpAnimation();
        harness.Pump(new Size(500, 400));
        Assert.NotNull(FindParagraph(harness.RenderView, "Modal sheet"));
    }

    [Fact]
    public void ModalBottomSheet_DismissibleBarrierTapPopsTheRoute()
    {
        BuildContext captured = default;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => new CaptureContext(
                value => captured = value,
                child: new Text("Underlying"))))));
        harness.Pump(new Size(500, 400));

        MaterialBottomSheets.ShowModalBottomSheet<string>(
            captured,
            _ => new SizedBox(height: 120, child: new Text("Modal sheet")));
        PumpAnimation();
        harness.Pump(new Size(500, 400));

        TapAt(harness, new Point(20, 20));
        PumpAnimation();
        harness.Pump(new Size(500, 400));
        Assert.Null(FindParagraph(harness.RenderView, "Modal sheet"));
    }

    [Fact]
    public void ModalBottomSheet_SafeAreaCompositionMatchesSource()
    {
        MediaQueryData withoutSafeArea = ShowModalAndCaptureMediaQuery(useSafeArea: false);
        Assert.Equal(0, withoutSafeArea.Padding.Top);
        Assert.Equal(50, withoutSafeArea.Padding.Left);
        Assert.Equal(50, withoutSafeArea.Padding.Bottom);

        MediaQueryData withSafeArea = ShowModalAndCaptureMediaQuery(useSafeArea: true);
        Assert.Equal(0, withSafeArea.Padding.Top);
        Assert.Equal(0, withSafeArea.Padding.Left);
        Assert.Equal(50, withSafeArea.Padding.Bottom);
    }

    [Fact]
    public void ModalBottomSheet_UsesModalSpecificThemeValues()
    {
        var theme = ThemeData.Light with
        {
            BottomSheetTheme = new BottomSheetThemeData(
                BackgroundColor: Colors.Purple,
                ModalBackgroundColor: Colors.Orange,
                Elevation: 0,
                ModalElevation: 3),
        };
        BuildContext captured = default;
        using var harness = new WidgetRenderHarness(Wrap(
            theme,
            new Navigator(new BuilderPageRoute(_ => new CaptureContext(
                value => captured = value,
                child: new Text("Underlying"))))));
        harness.Pump(new Size(500, 400));

        MaterialBottomSheets.ShowModalBottomSheet<string>(
            captured,
            _ => new SizedBox(height: 120, child: new Text("Modal sheet")));
        PumpAnimation();
        harness.Pump(new Size(500, 400));

        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView), box =>
            box.Decoration.Color == Colors.Orange);
        Assert.DoesNotContain(FindDescendants<RenderDecoratedBox>(harness.RenderView), box =>
            box.Decoration.Color == Colors.Purple);
    }

    [Fact]
    public void ModalBottomSheet_AvoidsDisplayFeaturesUsingAnchorPoint()
    {
        var media = new MediaQueryData(
            Size: new Size(800, 600),
            DisplayFeatures: [new DisplayFeature(new Rect(390, 0, 20, 600), DisplayFeatureType.Hinge)]);

        Rect trailing = ShowModalAndMeasureSheet(media, new Point(1000, 0));
        Assert.Equal(410, trailing.Left, precision: 3);
        Assert.Equal(800, trailing.Right, precision: 3);

        Rect leading = ShowModalAndMeasureSheet(media, anchorPoint: null);
        Assert.Equal(0, leading.Left, precision: 3);
        Assert.Equal(390, leading.Right, precision: 3);
    }

    [Fact]
    public void ModalBottomSheet_UsesSuppliedTransitionControllerAndDoesNotDisposeIt()
    {
        var controller = new AnimationController(
            duration: TimeSpan.FromSeconds(2)) { ReverseDuration = TimeSpan.FromSeconds(2) };
        BuildContext captured = default;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => new CaptureContext(
                value => captured = value,
                child: new Text("Underlying"))))));
        harness.Pump(new Size(500, 400));

        MaterialBottomSheets.ShowModalBottomSheet<string>(
            captured,
            _ => new SizedBox(height: 120, child: new Text("Modal sheet")),
            transitionAnimationController: controller);
        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1.0));
        harness.Pump(new Size(500, 400));
        Assert.InRange(controller.Value, 0.3, 0.7);

        Navigator.Of(captured).Pop();
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 3.5));
        harness.Pump(new Size(500, 400));
        Assert.Null(FindParagraph(harness.RenderView, "Modal sheet"));

        // A caller-supplied controller stays usable: the framework never disposes it.
        controller.SetValue(0.5);
        Assert.Equal(0.5, controller.Value, precision: 6);
        controller.Dispose();
    }

    [Fact]
    public async Task PersistentBottomSheet_ControllerRebuildCloseAndLocalHistoryMatchScaffoldFlow()
    {
        BuildContext captured = default;
        string label = "First";
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => new Scaffold(
                body: new CaptureContext(value => captured = value, new Text("Body")))))));
        harness.Pump(new Size(500, 400));

        var controller = MaterialBottomSheets.ShowBottomSheet(
            captured,
            _ => new SizedBox(height: 96, child: new Text(label)));
        PumpAnimation();
        harness.Pump(new Size(500, 400));
        Assert.NotNull(FindParagraph(harness.RenderView, "First"));
        Assert.True(ModalRoute.Of(captured).ImpliesAppBarDismissal);

        controller.SetState(() => label = "Second");
        harness.Pump(new Size(500, 400));
        Assert.NotNull(FindParagraph(harness.RenderView, "Second"));
        controller.Close();
        PumpAnimation();
        harness.Pump(new Size(500, 400));
        await controller.Closed;
        Assert.Null(FindParagraph(harness.RenderView, "Second"));
    }

    [Fact]
    public void PersistentBottomSheet_GrowsWithTheStandardHeightFactorCurve()
    {
        BuildContext captured = default;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => new Scaffold(
                body: new CaptureContext(value => captured = value, new Text("Body")))))));
        harness.Pump(new Size(500, 400));

        var transitionController = BottomSheet.CreateAnimationController(
            sheetAnimationStyle: new AnimationStyle(
                Duration: TimeSpan.FromMilliseconds(800),
                ReverseDuration: TimeSpan.FromMilliseconds(400)));
        MaterialBottomSheets.ShowBottomSheet(
            captured,
            _ => new SizedBox(height: 100, child: new Text("Persistent")),
            transitionAnimationController: transitionController);

        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.001));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.4));
        harness.Pump(new Size(500, 400));

        // The persistent sheet animates its height factor with fastOutSlowIn rather than translating.
        var sheet = Assert.Single(FindDescendants<RenderBox>(harness.RenderView), box =>
            box is RenderConstrainedBox constrained && constrained.AdditionalConstraints.MinHeight == 100);
        Assert.Equal(100, sheet.Size.Height, precision: 3);
        double expected = 100 * Curves.FastOutSlowIn(Math.Clamp(transitionController.Value, 0, 1));
        var align = Assert.Single(FindDescendants<RenderAlign>(harness.RenderView), box =>
            box.HeightFactor is not null && box.HeightFactor < 1.0);
        Assert.Equal(expected, align.Size.Height, precision: 1);
    }

    [Fact]
    public void ModalBottomSheet_PageSemanticsAreOpaqueToHitTesting()
    {
        BuildContext captured = default;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => new CaptureContext(
                value => captured = value,
                child: new Text("Underlying"))))));
        harness.Pump(new Size(500, 400));

        MaterialBottomSheets.ShowModalBottomSheet<string>(
            captured,
            _ => new SizedBox(height: 120, child: new Text("Modal sheet")));
        PumpAnimation();
        var semantics = harness.PumpAndGetSemantics(new Size(500, 400));

        // The route wraps the sheet in Semantics(hitTestBehavior: opaque) so clicks inside the sheet do not
        // pass through to the barrier.
        var opaque = FindSemantics(
            semantics,
            node => node.HitTestBehavior == SemanticsHitTestBehavior.Opaque);
        Assert.NotNull(opaque);
        // The route's own scope node is a descendant of the opaque one.
        Assert.NotNull(FindSemantics(opaque, node => node.Flags.HasFlag(SemanticsFlags.ScopesRoute)));
    }

    [Fact]
    public void ModalBottomSheet_DraggableChildAtItsMinimumExtentClosesTheSheet()
    {
        var controller = new DraggableScrollableController();
        using var harness = ShowModalDraggableSheet(controller, shouldCloseOnMinExtent: true, out var context);
        Assert.NotNull(FindParagraph(harness.RenderView, "Sheet content"));

        controller.JumpTo(0.25);
        PumpAnimation();
        harness.Pump(new Size(500, 400));

        Assert.Null(FindParagraph(harness.RenderView, "Sheet content"));
        Assert.False(Navigator.Of(context).CanPop);
    }

    [Fact]
    public void ModalBottomSheet_DraggableChildKeepsTheSheetWhenItShouldNotCloseOnMinExtent()
    {
        var controller = new DraggableScrollableController();
        using var harness = ShowModalDraggableSheet(controller, shouldCloseOnMinExtent: false, out _);

        controller.JumpTo(0.25);
        PumpAnimation();
        harness.Pump(new Size(500, 400));

        Assert.NotNull(FindParagraph(harness.RenderView, "Sheet content"));
    }

    [Fact]
    public void PersistentBottomSheet_DraggableChildAtItsMinimumExtentClosesTheSheet()
    {
        var controller = new DraggableScrollableController();
        BuildContext captured = default;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => new Scaffold(
                body: new CaptureContext(value => captured = value, new Text("Body")))))));
        harness.Pump(new Size(500, 400));

        MaterialBottomSheets.ShowBottomSheet(captured, _ => DraggableSheet(controller, shouldCloseOnMinExtent: true));
        PumpAnimation();
        harness.Pump(new Size(500, 400));
        Assert.NotNull(FindParagraph(harness.RenderView, "Sheet content"));

        controller.JumpTo(0.25);
        PumpAnimation();
        harness.Pump(new Size(500, 400));
        Assert.Null(FindParagraph(harness.RenderView, "Sheet content"));
    }

    [Fact]
    public void PersistentBottomSheet_DominatingDraggableChildDrivesTheBodyScrimAndTheFloatingActionButton()
    {
        var controller = new DraggableScrollableController();
        BuildContext captured = default;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => new Scaffold(
                body: new CaptureContext(value => captured = value, new Text("Body")),
                floatingActionButton: new FloatingActionButton(child: new Text("+"), onPressed: () => { }))))));
        harness.Pump(new Size(500, 400));

        MaterialBottomSheets.ShowBottomSheet(captured, _ => DraggableSheet(controller, shouldCloseOnMinExtent: true));
        PumpAnimation();
        harness.Pump(new Size(500, 400));

        // Below the dominating threshold there is no scrim and the button keeps its full size.
        Assert.DoesNotContain(FindDescendants<RenderConstrainedBox>(harness.RenderView), IsScrimBox);
        var scaffold = Scaffold.Of(captured);
        Assert.Equal(1.0, scaffold.FloatingActionButtonVisibilityController.Value, precision: 6);

        controller.JumpTo(0.9);
        PumpAnimation();
        harness.Pump(new Size(500, 400));

        double extentRemaining = 1.0 - 0.9;
        Assert.Equal(
            extentRemaining * Scaffold.BottomSheetDominatesPercentage * 10,
            scaffold.FloatingActionButtonVisibilityController.Value,
            precision: 6);
        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView), IsScrimBox);

        controller.JumpTo(0.5);
        PumpAnimation();
        harness.Pump(new Size(500, 400));
        Assert.Equal(1.0, scaffold.FloatingActionButtonVisibilityController.Value, precision: 6);
        Assert.DoesNotContain(FindDescendants<RenderConstrainedBox>(harness.RenderView), IsScrimBox);
    }

    [Fact]
    public void StaticBottomSheet_DraggableChildRegistersALocalHistoryEntryAboveItsInitialExtent()
    {
        var controller = new DraggableScrollableController();
        BuildContext captured = default;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => new Scaffold(
                body: new CaptureContext(value => captured = value, new Text("Body")),
                bottomSheet: DraggableSheet(controller, shouldCloseOnMinExtent: true))))));
        harness.Pump(new Size(500, 400));
        var route = ModalRoute.Of(captured);
        Assert.False(route.WillHandlePopInternally);

        controller.JumpTo(0.9);
        PumpAnimation();
        harness.Pump(new Size(500, 400));
        Assert.True(route.WillHandlePopInternally);

        // Popping the local history entry resets the sheet instead of closing it: a Scaffold.bottomSheet is
        // persistent, so reaching the minimum extent never closes it either.
        Navigator.Of(captured).MaybePop();
        PumpAnimation();
        harness.Pump(new Size(500, 400));
        Assert.False(route.WillHandlePopInternally);
        Assert.Equal(0.5, controller.Size, precision: 6);
        Assert.NotNull(FindParagraph(harness.RenderView, "Sheet content"));

        controller.JumpTo(0.25);
        PumpAnimation();
        harness.Pump(new Size(500, 400));
        Assert.NotNull(FindParagraph(harness.RenderView, "Sheet content"));
    }

[Fact]
    public void StaticBottomSheet_ReplacedThenRemoved_KeepsTheDismissedSheetUntilItsExitAnimationEnds()
    {
        // Dart: `scaffold_test.dart` "can rebuild and remove bottomSheet at the same time"
        // (flutter#117004). `_dismissedBottomSheets` holds sheets that are still animating out, so a
        // rebuild issued while one is leaving neither drops it early nor throws.
        Widget? sheet = new SizedBox(height: 100, child: new Text("First"));
        StateSetter? rebuild = null;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => new StatefulBuilder((_, setState) =>
            {
                rebuild = setState;
                return new Scaffold(body: new Text("Body"), bottomSheet: sheet);
            })))));
        harness.Pump(new Size(500, 400));
        Assert.NotNull(FindParagraph(harness.RenderView, "First"));

        // Replacing the sheet goes through _updatePersistentBottomSheet: the host StatefulBuilder is kept
        // and only its content is swapped.
        rebuild!(() => sheet = new SizedBox(height: 100, child: new Text("Second")));
        harness.Pump(new Size(500, 400));
        Assert.Null(FindParagraph(harness.RenderView, "First"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Second"));

        // Removing it starts the 200 ms exit animation; the sheet stays mounted meanwhile.
        rebuild!(() => sheet = null);
        harness.Pump(new Size(500, 400));
        Assert.NotNull(FindParagraph(harness.RenderView, "Second"));

        // An unrelated rebuild while the sheet is animating out must not throw or drop it.
        rebuild!(() => { });
        harness.Pump(new Size(500, 400));
        Assert.NotNull(FindParagraph(harness.RenderView, "Second"));

        PumpAnimation();
        harness.Pump(new Size(500, 400));
        Assert.Null(FindParagraph(harness.RenderView, "Second"));
    }

    [Fact]
    public void StaticBottomSheet_ReinstatedWhileTheOldOneAnimatesOut_UnmountsTheDismissedSheetImmediately()
    {
        // Dart's `_maybeBuildPersistentBottomSheet` resets every controller in `_dismissedBottomSheets`
        // before building the new sheet, otherwise a sub-tree carrying a GlobalKey would be mounted twice.
        // Source comment: "Stop the animation and unmount the dismissed sheets from the tree immediately".
        Widget? sheet = new SizedBox(height: 100, child: new Text("Sheet"));
        StateSetter? rebuild = null;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => new StatefulBuilder((_, setState) =>
            {
                rebuild = setState;
                return new Scaffold(body: new Text("Body"), bottomSheet: sheet);
            })))));
        harness.Pump(new Size(500, 400));

        rebuild!(() => sheet = null);
        harness.Pump(new Size(500, 400));
        Assert.NotNull(FindParagraph(harness.RenderView, "Sheet"));

        rebuild!(() => sheet = new SizedBox(height: 100, child: new Text("Sheet")));
        harness.Pump(new Size(500, 400));

        // Exactly one sheet is mounted: the dismissed one was reset and unmounted in the same frame.
        Assert.Single(FindDescendants<RenderParagraph>(harness.RenderView)
            .Where(paragraph => paragraph.PlainText == "Sheet"));
    }

    [Fact]
    public void StaticBottomSheet_UpdatedInPlace_KeepsItsRenderObject()
    {
        // Dart: `persistent_bottom_sheet_test.dart` "Scaffold.bottomSheet should be updated without
        // creating a new RO when the new widget has the same key and type." (flutter#71435).
        Widget sheet = new SizedBox(height: 100, child: new Text("First"));
        StateSetter? rebuild = null;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => new StatefulBuilder((_, setState) =>
            {
                rebuild = setState;
                return new Scaffold(body: new Text("Body"), bottomSheet: sheet);
            })))));
        harness.Pump(new Size(500, 400));
        RenderParagraph? before = FindParagraph(harness.RenderView, "First");
        Assert.NotNull(before);

        rebuild!(() => sheet = new SizedBox(height: 100, child: new Text("Second")));
        harness.Pump(new Size(500, 400));

        Assert.Same(before, FindParagraph(harness.RenderView, "Second"));
    }

    [Fact]
    public void ShowBottomSheet_WhileScaffoldBottomSheetIsSet_Throws()
    {
        // Dart: `scaffold_test.dart` "showBottomSheet() while Scaffold has bottom sheet".
        BuildContext captured = default;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => new Scaffold(
                body: new CaptureContext(value => captured = value, new Text("Body")),
                bottomSheet: new SizedBox(height: 100, child: new Text("Sheet")))))));
        harness.Pump(new Size(500, 400));

        var error = Assert.Throws<InvalidOperationException>(
            () => MaterialBottomSheets.ShowBottomSheet(captured, _ => new Text("Shown")));
        Assert.Contains("Rebuild the Scaffold with a null bottomSheet", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Scaffold_GivenABottomSheetWhileAShownSheetIsVisible_Throws()
    {
        // Dart: `scaffold_test.dart` "didUpdate bottomSheet while a previous bottom sheet is still
        // displayed" — the `didUpdateWidget` assert, whose hint names PersistentBottomSheetController.
        BuildContext captured = default;
        Widget? sheet = null;
        StateSetter? rebuild = null;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => new StatefulBuilder((_, setState) =>
            {
                rebuild = setState;
                return new Scaffold(
                    body: new CaptureContext(value => captured = value, new Text("Body")),
                    bottomSheet: sheet);
            })))));
        harness.Pump(new Size(500, 400));

        MaterialBottomSheets.ShowBottomSheet(captured, _ => new SizedBox(height: 100, child: new Text("Shown")));
        PumpAnimation();
        harness.Pump(new Size(500, 400));

        rebuild!(() => sheet = new SizedBox(height: 100, child: new Text("Static")));
        var error = Assert.Throws<InvalidOperationException>(() => harness.Pump(new Size(500, 400)));
        Assert.Contains("PersistentBottomSheetController", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ShowBottomSheet_ReleasesItsLocalHistoryEntryAsSoonAsTheControllerCloses()
    {
        // Dart: `bottom_sheet_test.dart` "The old route entry should be removed when a new sheet popup"
        // (flutter#99627): the entry goes away on `close()`, before the exit animation ends.
        BuildContext captured = default;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => new Scaffold(
                body: new CaptureContext(value => captured = value, new Text("Body")))))));
        harness.Pump(new Size(500, 400));
        var route = ModalRoute.Of(captured);
        Assert.False(route.WillHandlePopInternally);

        PersistentBottomSheetController first = MaterialBottomSheets.ShowBottomSheet(
            captured,
            _ => new SizedBox(height: 100, child: new Text("A")));
        PumpAnimation();
        harness.Pump(new Size(500, 400));
        Assert.True(route.WillHandlePopInternally);

        PersistentBottomSheetController second = MaterialBottomSheets.ShowBottomSheet(
            captured,
            _ => new SizedBox(height: 100, child: new Text("B")));
        PumpAnimation();
        harness.Pump(new Size(500, 400));
        Assert.True(route.WillHandlePopInternally);

        second.Close();
        Assert.False(route.WillHandlePopInternally);

        // Closing the controller of a sheet that a later sheet already replaced is a no-op, not a crash
        // (Dart: "Calling PersistentBottomSheetController.close does not crash when it is not the current
        // bottom sheet", flutter#93717).
        first.Close();
        PumpAnimation();
        harness.Pump(new Size(500, 400));
        Assert.Null(FindParagraph(harness.RenderView, "A"));
        Assert.Null(FindParagraph(harness.RenderView, "B"));
    }

    private static bool IsScrimBox(RenderConstrainedBox box) =>
        box.AdditionalConstraints.MinWidth == double.PositiveInfinity
        && box.AdditionalConstraints.MinHeight == double.PositiveInfinity
        && box.Child is RenderColoredBox;

    private static Widget DraggableSheet(DraggableScrollableController controller, bool shouldCloseOnMinExtent) =>
        new DraggableScrollableSheet(
            builder: (_, scrollController) => new ListView(
                controller: scrollController,
                itemExtent: 25.0,
                children: [new Text("Sheet content"), .. Enumerable
                    .Range(0, 40)
                    .Select(Widget (_) => new SizedBox(height: 25.0))]),
            initialChildSize: 0.5,
            minChildSize: 0.25,
            maxChildSize: 1.0,
            expand: false,
            controller: controller,
            shouldCloseOnMinExtent: shouldCloseOnMinExtent);

    private static WidgetRenderHarness ShowModalDraggableSheet(
        DraggableScrollableController controller,
        bool shouldCloseOnMinExtent,
        out BuildContext context)
    {
        BuildContext captured = default;
        var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => new CaptureContext(
                value => captured = value,
                child: new Text("Underlying"))))));
        harness.Pump(new Size(500, 400));

        MaterialBottomSheets.ShowModalBottomSheet<string>(
            captured,
            _ => DraggableSheet(controller, shouldCloseOnMinExtent),
            isScrollControlled: true);
        PumpAnimation();
        harness.Pump(new Size(500, 400));
        context = captured;
        return harness;
    }

    private static Rect ShowModalAndMeasureSheet(MediaQueryData media, Point? anchorPoint)
    {
        BuildContext captured = default;
        using var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Ltr,
            new MediaQuery(
                media,
                new MaterialLocalizationsScope(
                    DefaultMaterialLocalizations.Instance,
                    new Theme(
                        ThemeData.Light,
                        new Navigator(new BuilderPageRoute(_ => new CaptureContext(
                            value => captured = value,
                            child: new Text("Underlying")))))))));
        harness.Pump(media.Size);

        MaterialBottomSheets.ShowModalBottomSheet<string>(
            captured,
            _ => new SizedBox(height: 120, child: new Text("Modal sheet")),
            anchorPoint: anchorPoint);
        PumpAnimation();
        harness.Pump(media.Size);

        var layout = Assert.Single(FindDescendants<RenderBottomSheetLayoutWithSizeListener>(harness.RenderView));
        Point origin = layout.LocalToGlobal(default);
        return new Rect(origin, layout.Size);
    }

    private static MediaQueryData ShowModalAndCaptureMediaQuery(bool useSafeArea)
    {
        BuildContext captured = default;
        MediaQueryData? sheetMedia = null;
        using var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Ltr,
            new MediaQuery(
                new MediaQueryData(Size: new Size(500, 400), Padding: new Thickness(50)),
                new MaterialLocalizationsScope(
                    DefaultMaterialLocalizations.Instance,
                    new Theme(
                        ThemeData.Light,
                        new Navigator(new BuilderPageRoute(_ => new CaptureContext(
                            value => captured = value,
                            child: new Text("Underlying")))))))));
        harness.Pump(new Size(500, 400));

        MaterialBottomSheets.ShowModalBottomSheet<string>(
            captured,
            context =>
            {
                sheetMedia = MediaQuery.Of(context);
                return new SizedBox(height: 120, child: new Text("Modal sheet"));
            },
            useSafeArea: useSafeArea);
        PumpAnimation();
        harness.Pump(new Size(500, 400));
        return sheetMedia!;
    }

    private static Point HandleCenter(WidgetRenderHarness harness)
    {
        var handle = Assert.Single(FindDescendants<RenderConstrainedBox>(harness.RenderView), box =>
            box.AdditionalConstraints.MaxWidth == 48 && box.AdditionalConstraints.MaxHeight == 48);
        Point origin = handle.LocalToGlobal(default);
        return new Point(origin.X + (handle.Size.Width / 2), origin.Y + (handle.Size.Height / 2));
    }

    private static void TapAt(WidgetRenderHarness harness, Point position)
    {
        var binding = GestureBinding.Instance;
        var now = DateTime.UtcNow;
        DispatchDown(binding, harness.RenderView, 77, position, now);
        DispatchUp(binding, harness.RenderView, 77, position, now.AddMilliseconds(20));
    }

    private static Widget Wrap(ThemeData theme, Widget child) => new Directionality(
        TextDirection.Ltr,
        new MediaQuery(
            new MediaQueryData(Size: new Size(800, 600)),
            new MaterialLocalizationsScope(DefaultMaterialLocalizations.Instance, new Theme(theme, child))));

    private static void PumpAnimation()
    {
        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.30));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.60));
    }

    private static void DispatchDown(GestureBinding binding, RenderView view, int pointer, Point position, DateTime time) =>
        binding.HandlePointerEvent(view, new PointerDownEvent(pointer, PointerDeviceKind.Mouse, position, PointerButtons.Primary, time));
    private static void DispatchMove(GestureBinding binding, RenderView view, int pointer, Point position, DateTime time) =>
        binding.HandlePointerEvent(view, new PointerMoveEvent(pointer, PointerDeviceKind.Mouse, position, PointerButtons.Primary, true, time));
    private static void DispatchUp(GestureBinding binding, RenderView view, int pointer, Point position, DateTime time) =>
        binding.HandlePointerEvent(view, new PointerUpEvent(pointer, PointerDeviceKind.Mouse, position, PointerButtons.None, time));

    private static RenderParagraph? FindParagraph(RenderObject? root, string text) =>
        FindDescendants<RenderParagraph>(root).FirstOrDefault(value => value.PlainText == text);

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
        if (node is null || predicate(node)) return node;
        foreach (var child in node.Children)
        {
            var found = FindSemantics(child, predicate);
            if (found is not null) return found;
        }
        return null;
    }

    private static List<SemanticsNode> CollectSemantics(SemanticsNode? node)
    {
        var result = new List<SemanticsNode>();
        if (node is null) return result;
        result.Add(node);
        foreach (var child in node.Children)
        {
            result.AddRange(CollectSemantics(child));
        }
        return result;
    }

    private sealed class CaptureContext : StatelessWidget
    {
        private readonly Action<BuildContext> _capture;
        private readonly Widget _child;
        public CaptureContext(Action<BuildContext> capture, Widget child) { _capture = capture; _child = child; }
        public override Widget Build(BuildContext context) { _capture(context); return _child; }
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _root;
        private readonly PipelineOwner _pipeline;
        public WidgetRenderHarness(Widget widget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _root = new HarnessRootElement(RenderView, widget);
            _root.Attach(_owner);
            _root.Mount(null, null);
            _owner.FlushBuild();
        }
        public RenderView RenderView { get; }
        public void Pump(Size size) { _owner.FlushBuild(); _pipeline.RequestLayout(); _pipeline.FlushLayout(size); _pipeline.FlushCompositingBits(); _pipeline.FlushPaint(); }
        public SemanticsNode? PumpAndGetSemantics(Size size) { Pump(size); _pipeline.RequestSemanticsUpdate(); _pipeline.FlushSemantics(); return _pipeline.SemanticsOwner.RootNode; }
        public void Dispose() => _root.Unmount();

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _view;
            private Element? _child;
            public HarnessRootElement(RenderView view, Widget widget) : base(widget) => _view = view;
            public override RenderObject? RenderObject => _child?.RenderObject;
            internal override Element? RenderObjectAttachingChild => _child;
            protected override void OnMount() { base.OnMount(); Rebuild(); }
            internal override void Rebuild() { Dirty = false; _child = UpdateChild(_child, Widget, Slot); }
            internal override void Update(Widget newWidget) { base.Update(newWidget); Rebuild(); }
            internal override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
            internal override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
            public void InsertRenderObjectChild(RenderObject child, object? slot) => _view.Child = (RenderBox)child;
            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
            public void RemoveRenderObjectChild(RenderObject child, object? slot) { if (ReferenceEquals(_view.Child, child)) _view.Child = null; }
            internal override void Unmount() { if (_child is not null) { UnmountChild(_child); _child = null; } base.Unmount(); }
        }
    }
}
