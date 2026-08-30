using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialDialogTests : IDisposable
{
    public MaterialDialogTests()
    {
        Scheduler.ResetForTests();
        GestureBinding.Instance.ResetForTests();
    }

    public void Dispose()
    {
        PlatformDefaults.DebugTargetPlatformOverride = null;
        GestureBinding.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void DialogAndTheme_ValidateContractsAndFullscreenDefaults()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Dialog(elevation: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Dialog(insetPadding: new Thickness(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DialogThemeData(Elevation: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AlertDialog(actionsOverflowButtonSpacing: -1));

        var fullscreen = Dialog.Fullscreen(child: new Text("full"));
        Assert.True(fullscreen.IsFullscreen);
        Assert.Equal(0, fullscreen.Elevation);
        Assert.Equal(TimeSpan.Zero, fullscreen.InsetAnimationDuration);
        Assert.Equal(default(Thickness), fullscreen.InsetPadding);
        Assert.Equal(Clip.None, fullscreen.ClipBehavior);
    }

    [Fact]
    public void Dialog_M3DefaultsApplyInsetsConstraintsShapeAndSemantics()
    {
        var media = new MediaQueryData(
            Size: new Size(600, 400),
            ViewInsets: new Thickness(3, 5, 7, 11));
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light with { Platform = TargetPlatform.Android },
            new Dialog(child: new SizedBox(width: 80, height: 40)),
            mediaQuery: media));
        var semantics = harness.PumpAndGetSemantics(new Size(600, 400));

        Assert.Contains(FindDescendants<RenderPadding>(harness.RenderView), padding =>
            padding.Padding == new Thickness(43, 29, 47, 35));
        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView), box =>
            box.AdditionalConstraints.MinWidth == 280);
        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView), box =>
            box.Decoration.Color == ThemeData.Light.ColorScheme.SurfaceContainerHigh
            && box.Decoration.EffectiveBorderRadius == BorderRadius.Circular(28));
        Assert.NotNull(FindSemantics(semantics, node => node.Role == SemanticsRole.Dialog));
    }

    [Fact]
    public void Dialog_LocalThemeAndWidgetOverridesUseFlutterPrecedence()
    {
        var global = ThemeData.Light with
        {
            DialogTheme = new DialogThemeData(
                BackgroundColor: Colors.Green,
                Elevation: 2,
                Shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(10))),
        };
        var local = new DialogThemeData(
            BackgroundColor: Colors.Purple,
            Elevation: 0,
            Shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(12)),
            InsetPadding: new Thickness(9));
        using var themed = new WidgetRenderHarness(Wrap(
            global,
            new DialogTheme(local, new Dialog(child: new Text("themed")))));
        themed.Pump(new Size(500, 300));
        Assert.Contains(FindDescendants<RenderDecoratedBox>(themed.RenderView), box =>
            box.Decoration.Color == Colors.Purple
            && box.Decoration.EffectiveBorderRadius == BorderRadius.Circular(12));
        Assert.Contains(FindDescendants<RenderPadding>(themed.RenderView), padding => padding.Padding == new Thickness(9));

        using var widget = new WidgetRenderHarness(Wrap(
            global,
            new Dialog(
                backgroundColor: Colors.Orange,
                elevation: 0,
                shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(6)),
                child: new Text("widget"))));
        widget.Pump(new Size(500, 300));
        Assert.Contains(FindDescendants<RenderDecoratedBox>(widget.RenderView), box =>
            box.Decoration.Color == Colors.Orange
            && box.Decoration.EffectiveBorderRadius == BorderRadius.Circular(6));
    }

    [Fact]
    public void Dialog_M2DefaultsUseFourPixelShapeAndElevationShadow()
    {
        var theme = ThemeData.Light with { UseMaterial3 = false };
        using var harness = new WidgetRenderHarness(Wrap(
            theme,
            new Dialog(child: new Text("M2 dialog"))));
        harness.Pump(new Size(500, 300));

        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView), box =>
            box.Decoration.Color == Colors.White
            && box.Decoration.EffectiveBorderRadius == BorderRadius.Circular(4)
            && box.Decoration.BoxShadows is not null);
    }

    [Fact]
    public void AlertDialog_ComposesIconTitleContentActionsAndM3Defaults()
    {
        // The route label follows the host platform (Dart's defaultTargetPlatform), not theme.platform.
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light with { Platform = TargetPlatform.Android },
            new AlertDialog(
                icon: new Icon(Icons.InfoOutline),
                title: new Text("Title"),
                content: new Text("Content"),
                actions:
                [
                    new TextButton(new Text("CANCEL"), () => { }),
                    new TextButton(new Text("OK"), () => { }),
                ])));
        var semantics = harness.PumpAndGetSemantics(new Size(600, 400));

        Assert.Single(FindDescendants<RenderIntrinsicWidth>(harness.RenderView));
        Assert.Single(FindDescendants<RenderOverflowBar>(harness.RenderView));
        ThemeData localizedTheme = ThemeData.Localize(ThemeData.Light, Typography.EnglishLike2021);
        Assert.Equal(
            localizedTheme.TextTheme.HeadlineSmall.FontSize,
            FindParagraph(harness.RenderView, "Title")!.FontSize);
        Assert.Equal(
            localizedTheme.TextTheme.BodyMedium.FontSize,
            FindParagraph(harness.RenderView, "Content")!.FontSize);
        Assert.Contains(FindDescendants<RenderPadding>(harness.RenderView), value => value.Padding == new Thickness(24, 24, 24, 16));
        Assert.Contains(FindDescendants<RenderPadding>(harness.RenderView), value => value.Padding == new Thickness(24, 16, 24, 24));
        Assert.Contains(FindDescendants<RenderPadding>(harness.RenderView), value => value.Padding == new Thickness(24, 0, 24, 24));
        Assert.NotNull(FindSemantics(semantics, node =>
            HasLabelPart(node, "Alert")
            && node.Flags.HasFlag(SemanticsFlags.ScopesRoute)
            && node.Flags.HasFlag(SemanticsFlags.NamesRoute)));
        Assert.NotNull(FindSemantics(semantics, node => node.Role == SemanticsRole.AlertDialog));
    }

    [Fact]
    public void AlertDialog_TextScaleShrinksHorizontalAndTopPaddingLikeFlutter()
    {
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new AlertDialog(
                title: new Text("Title"),
                content: new Text("Content")),
            mediaQuery: new MediaQueryData(Size: new Size(500, 300), TextScaleFactor: 2)));
        harness.Pump(new Size(500, 300));

        Assert.Contains(FindDescendants<RenderPadding>(harness.RenderView), value =>
            Close(value.Padding.Left, 8)
            && Close(value.Padding.Top, 8)
            && Close(value.Padding.Right, 8)
            && Close(value.Padding.Bottom, 0));
        Assert.Contains(FindDescendants<RenderPadding>(harness.RenderView), value =>
            Close(value.Padding.Left, 8)
            && Close(value.Padding.Top, 16)
            && Close(value.Padding.Right, 8)
            && Close(value.Padding.Bottom, 24));
    }

    [Fact]
    public void AlertDialog_ScrollableWrapsTitleAndContentButKeepsActionsOutside()
    {
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new AlertDialog(
                scrollable: true,
                title: new Text("Scrollable title"),
                content: new Column(children: Enumerable.Range(0, 12).Select(index => (Widget)new Text($"Line {index}")).ToArray()),
                actions: [new TextButton(new Text("DONE"), () => { })])));
        harness.Pump(new Size(420, 240));

        Assert.Single(FindDescendants<RenderSingleChildViewport>(harness.RenderView));
        var overflow = Assert.Single(FindDescendants<RenderOverflowBar>(harness.RenderView));
        Assert.Equal(1, overflow.ChildCount);
        Assert.NotNull(FindParagraph(harness.RenderView, "DONE"));
    }

    [Fact]
    public void AlertDialog_NarrowActionsOverflowVerticallyAtDialogWidth()
    {
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new AlertDialog(
                constraints: new BoxConstraints(MinWidth: 200, MaxWidth: 200),
                title: new Text("Narrow"),
                actionsOverflowButtonSpacing: 6,
                actions:
                [
                    new SizedBox(width: 120, height: 40, child: new Text("FIRST")),
                    new SizedBox(width: 120, height: 40, child: new Text("SECOND")),
                ])));
        harness.Pump(new Size(360, 300));

        var overflow = Assert.Single(FindDescendants<RenderOverflowBar>(harness.RenderView));
        var firstOffset = ((OverflowBarParentData)overflow.FirstChild!.parentData!).offset;
        var secondOffset = ((OverflowBarParentData)overflow.LastChild!.parentData!).offset;
        Assert.True(secondOffset.Y >= firstOffset.Y + overflow.FirstChild.Size.Height + 6);
    }

    [Fact]
    public void SimpleDialogAndOption_ExposeFlutterDefaultsAndValidateContracts()
    {
        var dialog = new SimpleDialog();
        Assert.Equal(new Thickness(24, 24, 24, 0), dialog.TitlePadding);
        Assert.Equal(new Thickness(0, 12, 0, 16), dialog.ContentPadding);
        Assert.Null(dialog.Children);
        Assert.Throws<ArgumentOutOfRangeException>(() => new SimpleDialog(elevation: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SimpleDialogOption(padding: new Thickness(-1)));
    }

    [Fact]
    public void SimpleDialog_ComposesScrollableListBodyWithFlutterPaddingTypographyAndSemantics()
    {
        // The route label follows the host platform (Dart's defaultTargetPlatform), not theme.platform.
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light with { Platform = TargetPlatform.Android },
            new SimpleDialog(
                title: new Text("Choose account"),
                children:
                [
                    new SimpleDialogOption(child: new Text("Personal")),
                    new SimpleDialogOption(child: new Text("Work")),
                ])));
        var semantics = harness.PumpAndGetSemantics(new Size(600, 320));

        Assert.Single(FindDescendants<RenderIntrinsicWidth>(harness.RenderView));
        Assert.Single(FindDescendants<RenderSingleChildViewport>(harness.RenderView));
        var listBody = Assert.Single(FindDescendants<RenderListBody>(harness.RenderView));
        Assert.Equal(AxisDirection.Down, listBody.AxisDirection);
        Assert.Equal(2, listBody.ChildCount);
        Assert.Contains(FindDescendants<RenderPadding>(harness.RenderView), value =>
            value.Padding == new Thickness(24, 24, 24, 0));
        Assert.Contains(FindDescendants<RenderPadding>(harness.RenderView), value =>
            value.Padding == new Thickness(0, 12, 0, 16));
        Assert.Equal(2, FindDescendants<RenderPadding>(harness.RenderView).Count(value =>
            value.Padding == new Thickness(24, 8)));
        ThemeData localizedTheme = ThemeData.Localize(ThemeData.Light, Typography.EnglishLike2021);
        Assert.Equal(
            localizedTheme.TextTheme.TitleLarge.FontSize,
            FindParagraph(harness.RenderView, "Choose account")!.FontSize);
        Assert.Equal(
            localizedTheme.TextTheme.BodyMedium.FontSize,
            FindParagraph(harness.RenderView, "Personal")!.FontSize);
        Assert.NotNull(FindSemantics(semantics, node =>
            HasLabelPart(node, "Dialog")
            && node.Flags.HasFlag(SemanticsFlags.ScopesRoute)
            && node.Flags.HasFlag(SemanticsFlags.NamesRoute)));
        Assert.NotNull(FindSemantics(semantics, node => node.Role == SemanticsRole.Dialog));
    }

    [Fact]
    public void SimpleDialog_TextScaleAndThemeOverridesFollowSourcePrecedence()
    {
        var titleStyle = ThemeData.Light.TextTheme.TitleLarge.CopyWith(fontSize: 30, color: Colors.Purple);
        var contentStyle = ThemeData.Light.TextTheme.BodyMedium.CopyWith(fontSize: 18, color: Colors.Green);
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new DialogTheme(
                new DialogThemeData(TitleTextStyle: titleStyle, ContentTextStyle: contentStyle),
                new SimpleDialog(
                    title: new Text("Scaled title"),
                    children: [new SimpleDialogOption(child: new Text("Scaled option"))])),
            mediaQuery: new MediaQueryData(Size: new Size(500, 300), TextScaleFactor: 2)));
        harness.Pump(new Size(500, 300));

        Assert.Contains(FindDescendants<RenderPadding>(harness.RenderView), value =>
            Close(value.Padding.Left, 8)
            && Close(value.Padding.Top, 8)
            && Close(value.Padding.Right, 8)
            && Close(value.Padding.Bottom, 0));
        Assert.Contains(FindDescendants<RenderPadding>(harness.RenderView), value =>
            Close(value.Padding.Left, 0)
            && Close(value.Padding.Top, 12)
            && Close(value.Padding.Right, 0)
            && Close(value.Padding.Bottom, 16.0 / 3.0));
        Assert.Equal(60, ScaledFontSize(FindParagraph(harness.RenderView, "Scaled title")!));
        Assert.Equal(36, ScaledFontSize(FindParagraph(harness.RenderView, "Scaled option")!));
    }

    [Fact]
    public void SimpleDialogOption_UsesInkWellTapSemanticsAndSupportsDisabledState()
    {
        int taps = 0;
        using var enabled = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new SimpleDialogOption(onPressed: () => taps++, child: new Text("Enabled option"))));
        var semantics = enabled.PumpAndGetSemantics(new Size(240, 80));
        var tappable = FindSemantics(semantics, node => node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(tappable);
        Assert.True(tappable!.PerformAction(SemanticsActions.Tap));
        Assert.Equal(1, taps);
        Assert.Single(FindDescendants<RenderInkResponsePaint>(enabled.RenderView));

        using var disabled = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new SimpleDialogOption(child: new Text("Disabled option"))));
        var disabledSemantics = disabled.PumpAndGetSemantics(new Size(240, 80));
        Assert.Null(FindSemantics(disabledSemantics, node => node.Actions.HasFlag(SemanticsActions.Tap)));
    }

    [Fact]
    public async Task SimpleDialogOption_CompletesTypedDialogResult()
    {
        BuildContext captured = default;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(context => new CaptureContext(
                value => captured = value,
                new Text("Home"))))));
        harness.Pump(new Size(500, 320));
        var result = MaterialDialogs.ShowDialog<string>(
            captured,
            routeContext => new SimpleDialog(
                title: new Text("Select workspace"),
                children:
                [
                    new SimpleDialogOption(
                        onPressed: () => Navigator.Pop(routeContext, "team"),
                        child: new Text("Team workspace")),
                ]));
        PumpAnimation();
        var semantics = harness.PumpAndGetSemantics(new Size(500, 320));
        var option = FindSemantics(semantics, node =>
            node.Actions.HasFlag(SemanticsActions.Tap)
            && HasLabelPart(node, "Team workspace"));
        Assert.NotNull(option);
        Assert.True(option!.PerformAction(SemanticsActions.Tap));

        PumpAnimation();
        harness.Pump(new Size(500, 320));
        Assert.Equal("team", await result);
        Assert.Null(FindParagraph(harness.RenderView, "Select workspace"));
    }

    [Fact]
    public async Task ShowDialog_KeepsUnderlyingRouteCompletesResultAfterReverseAnimation()
    {
        BuildContext captured = default;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(context => new DialogTheme(
                new DialogThemeData(BackgroundColor: Colors.Purple),
                new CaptureContext(
                    value => captured = value,
                    new Text("Underlying")))))));
        harness.Pump(new Size(600, 400));

        var result = MaterialDialogs.ShowDialog<string>(
            captured,
            _ => new AlertDialog(title: new Text("Route dialog"), content: new Text("Body")));
        PumpAnimation();
        harness.Pump(new Size(600, 400));
        Assert.NotNull(FindParagraph(harness.RenderView, "Underlying"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Route dialog"));
        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView), box =>
            box.Decoration.Color == Colors.Purple);
        Assert.False(result.IsCompleted);

        // Flutter completes the dialog future on pop; the exit fade still runs afterwards.
        Navigator.Of(captured).Pop("accepted");
        Assert.Equal("accepted", await result);
        PumpAnimation();
        harness.Pump(new Size(600, 400));
        Assert.Null(FindParagraph(harness.RenderView, "Route dialog"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Underlying"));
    }

    [Theory]
    [InlineData(TargetPlatform.Android, true)]
    [InlineData(TargetPlatform.IOS, true)]
    [InlineData(TargetPlatform.MacOS, true)]
    [InlineData(TargetPlatform.Linux, false)]
    [InlineData(TargetPlatform.Windows, false)]
    public async Task ShowDialog_BarrierSemanticsDismissesOnlyWhenEnabled(
        TargetPlatform platform,
        bool platformSupportsDismissingBarrier)
    {
        TargetPlatform? previous = PlatformDefaults.DebugTargetPlatformOverride;
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        try
        {
            BuildContext captured = default;
            using var harness = new WidgetRenderHarness(Wrap(
                ThemeData.Light,
                new Navigator(new BuilderPageRoute(context => new CaptureContext(
                    value => captured = value,
                    new Text("Home"))))));
            harness.Pump(new Size(500, 320));
            var result = MaterialDialogs.ShowDialog<string>(
                captured,
                _ => new Dialog(child: new Text("Dismiss me")),
                barrierLabel: "Close modal");
            PumpAnimation();
            var semantics = harness.PumpAndGetSemantics(new Size(500, 320));
            var barrier = FindSemantics(semantics, node => HasLabelPart(node, "Close modal"));
            if (platformSupportsDismissingBarrier)
            {
                Assert.NotNull(barrier);
                Assert.True(barrier!.Actions.HasFlag(SemanticsActions.Tap));
                Assert.True(barrier.PerformAction(SemanticsActions.Tap));
            }
            else
            {
                Assert.Null(barrier);
                Navigator.Of(captured).Pop();
            }

            PumpAnimation();
            harness.Pump(new Size(500, 320));
            Assert.Null(await result);
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = previous;
        }
    }

    [Theory]
    [InlineData(TargetPlatform.IOS, true)]
    [InlineData(TargetPlatform.MacOS, true)]
    [InlineData(TargetPlatform.Android, false)]
    [InlineData(TargetPlatform.Windows, false)]
    [InlineData(TargetPlatform.Linux, false)]
    public void AlertDialogAdaptive_SelectsCupertinoOnApplePlatforms(TargetPlatform platform, bool expectCupertino)
    {
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light with { Platform = platform },
            AlertDialog.Adaptive(
                title: new Text("Adaptive title"),
                content: new Text("Adaptive content"),
                actions: [new TextButton(new Text("OK"), () => { })])));
        harness.Pump(new Size(600, 600));

        bool hasCupertinoSurface = FindDescendants<RenderColoredBox>(harness.RenderView)
            .Any(box => box.Color == Color.FromUInt32(0xCCF2F2F2));
        Assert.Equal(expectCupertino, hasCupertinoSurface);
        Assert.NotNull(FindParagraph(harness.RenderView, "Adaptive title"));
    }

    [Fact]
    public void DialogTitle_CentersWhenIconPresentAndStartsWithoutIcon()
    {
        using var withIcon = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new AlertDialog(icon: new Icon(Icons.InfoOutline), title: new Text("Centered"))));
        withIcon.Pump(new Size(600, 400));
        Assert.Equal(TextAlign.Center, FindParagraph(withIcon.RenderView, "Centered")!.TextAlign);

        using var withoutIcon = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new AlertDialog(title: new Text("Start"))));
        withoutIcon.Pump(new Size(600, 400));
        Assert.Equal(TextAlign.Start, FindParagraph(withoutIcon.RenderView, "Start")!.TextAlign);
    }

    [Fact]
    public async Task ShowDialog_ClosedLoopTraversalCyclesInsideDialogAndLeaveViewEscapes()
    {
        BuildContext captured = default;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(context => new CaptureContext(
                value => captured = value,
                new Text("Home"))))));
        harness.Pump(new Size(600, 400));

        var result = MaterialDialogs.ShowDialog<string>(
            captured,
            _ => new AlertDialog(
                title: new Text("Traversal"),
                actions:
                [
                    new TextButton(new Text("CANCEL"), () => { }),
                    new TextButton(new Text("OK"), () => { }),
                ]));
        PumpAnimation();
        harness.Pump(new Size(600, 400));

        // The dialog route focus scope defaults to a closed loop: Tab cycles endlessly.
        Assert.True(PumpFocus(FocusManager.Instance.FocusNext));
        FocusNode first = FocusManager.Instance.PrimaryFocus!;
        Assert.True(PumpFocus(FocusManager.Instance.FocusNext));
        Assert.True(PumpFocus(FocusManager.Instance.FocusNext));
        Assert.Same(first, FocusManager.Instance.PrimaryFocus);

        Navigator.Of(captured).Pop();
        PumpAnimation();
        harness.Pump(new Size(600, 400));
        Assert.Null(await result);

        var escaping = MaterialDialogs.ShowDialog<string>(
            captured,
            _ => new AlertDialog(
                title: new Text("Escaping"),
                actions: [new TextButton(new Text("ONLY"), () => { })]),
            traversalEdgeBehavior: TraversalEdgeBehavior.LeaveFlutterView);
        PumpAnimation();
        harness.Pump(new Size(600, 400));
        Assert.True(PumpFocus(FocusManager.Instance.FocusNext));
        // At the loop edge the focus leaves the view instead of wrapping.
        Assert.False(PumpFocus(FocusManager.Instance.FocusNext));
        // Dart's `unfocus()` hands the focus back to the enclosing scope rather than clearing it.
        Assert.IsType<FocusScopeNode>(FocusManager.Instance.PrimaryFocus);
        Navigator.Of(captured).Pop();
        PumpAnimation();
        harness.Pump(new Size(600, 400));
        Assert.Null(await escaping);
    }

    [Fact]
    public async Task ShowDialog_RequestFocusFalseKeepsPreviousFocus()
    {
        BuildContext captured = default;
        var homeFocus = new FocusNode();
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(context => new CaptureContext(
                value => captured = value,
                new Focus(focusNode: homeFocus, child: new Text("Home")))))));
        harness.Pump(new Size(600, 400));
        homeFocus.RequestFocus();
        Scheduler.FlushMicrotasks();
        Assert.Same(homeFocus, FocusManager.Instance.PrimaryFocus);

        var kept = MaterialDialogs.ShowDialog<string>(
            captured,
            _ => new AlertDialog(title: new Text("Silent")),
            requestFocus: false);
        PumpAnimation();
        harness.Pump(new Size(600, 400));
        Assert.Same(homeFocus, FocusManager.Instance.PrimaryFocus);
        Navigator.Of(captured).Pop();
        PumpAnimation();
        harness.Pump(new Size(600, 400));
        Assert.Null(await kept);

        var taken = MaterialDialogs.ShowDialog<string>(
            captured,
            _ => new AlertDialog(title: new Text("Focused")));
        PumpAnimation();
        harness.Pump(new Size(600, 400));
        Assert.NotSame(homeFocus, FocusManager.Instance.PrimaryFocus);
        Navigator.Of(captured).Pop();
        PumpAnimation();
        harness.Pump(new Size(600, 400));
        Assert.Null(await taken);
    }

    [Fact]
    public async Task ShowDialog_AppliesAnimationStyleDuration()
    {
        BuildContext captured = default;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(context => new CaptureContext(
                value => captured = value,
                new Text("Home"))))));
        harness.Pump(new Size(600, 400));

        ModalRoute? route = null;
        var result = MaterialDialogs.ShowDialog<string>(
            captured,
            dialogContext =>
            {
                route = ModalRoute.MaybeOf(dialogContext);
                return new AlertDialog(title: new Text("Slow"));
            },
            animationStyle: new AnimationStyle(Duration: TimeSpan.FromSeconds(1)));
        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.5));
        harness.Pump(new Size(600, 400));
        Assert.Equal(TimeSpan.FromSeconds(1), Assert.IsType<DialogRoute<string>>(route).TransitionDuration);
        Assert.True(route!.Animation.Value < 1.0);
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1.2));
        harness.Pump(new Size(600, 400));
        Assert.Equal(1.0, route.Animation.Value, precision: 3);

        Navigator.Of(captured).Pop();
        PumpAnimation();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 1.2));
        harness.Pump(new Size(600, 400));
        Assert.Null(await result);
    }

    [Fact]
    public void IntrinsicWidth_RoundsToStepAndFlexStretchHandlesUnboundedProbe()
    {
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new IntrinsicWidth(
                stepWidth: 56,
                child: new Column(
                    mainAxisSize: MainAxisSize.Min,
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    children: [new SizedBox(width: 70, height: 20)]))));
        harness.Pump(new Size(400, 100));
        var intrinsic = Assert.Single(FindDescendants<RenderIntrinsicWidth>(harness.RenderView));
        Assert.Equal(112, intrinsic.Size.Width, precision: 3);
    }

    private static Widget Wrap(ThemeData theme, Widget child, MediaQueryData? mediaQuery = null) =>
        new Directionality(
            TextDirection.Ltr,
            new MediaQuery(
                mediaQuery ?? new MediaQueryData(Size: new Size(600, 400)),
                new Localizations(
                    locale: new Locale("en", "US"),
                    delegates:
                    [
                        DefaultWidgetsLocalizations.Delegate,
                        DefaultMaterialLocalizations.Delegate,
                        DefaultCupertinoLocalizations.Delegate,
                    ],
                    child: new Theme(theme, child))));

    private static void PumpAnimation()
    {
        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.25));
    }

    private static bool Close(double actual, double expected) => Math.Abs(actual - expected) < 0.01;

    private static double ScaledFontSize(RenderParagraph paragraph) =>
        paragraph.TextScaler.Scale(paragraph.FontSize);

    private static RenderParagraph? FindParagraph(RenderObject? root, string text) =>
        FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.PlainText == text);

    private static bool PumpFocus(Func<bool> action)
    {
        bool result = action();
        Scheduler.FlushMicrotasks();
        return result;
    }

    private static void PumpFocus(Action action)
    {
        action();
        Scheduler.FlushMicrotasks();
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
        if (node is null || predicate(node)) return node;
        foreach (var child in node.Children)
        {
            var result = FindSemantics(child, predicate);
            if (result is not null) return result;
        }
        return null;
    }

    private sealed class CaptureContext : StatelessWidget
    {
        private readonly Action<BuildContext> _capture;
        private readonly Widget _child;

        public CaptureContext(Action<BuildContext> capture, Widget child)
        {
            _capture = capture;
            _child = child;
        }

        public override Widget Build(BuildContext context)
        {
            _capture(context);
            return _child;
        }
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
            return _pipeline.SemanticsOwner!.RootNode;
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

    /// <summary>
    /// Whether one of the node's merged label parts is <paramref name="part"/>. A merged node joins
    /// the labels it absorbed with a newline, exactly like Flutter's <c>_concatAttributedString</c>.
    /// </summary>
    private static bool HasLabelPart(SemanticsNode node, string part) =>
        node.Label?.Split('\n').Contains(part) == true;
}
