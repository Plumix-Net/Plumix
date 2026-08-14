using Avalonia;
using Avalonia.Media;
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
public sealed class MaterialAboutTests : IDisposable
{
    private const double NestedWidth = 700;
    private const double LateralWidth = 1200;

    public MaterialAboutTests()
    {
        Scheduler.ResetForTests();
        FocusManager.Instance.ResetForTests();
        GestureBinding.Instance.ResetForTests();
        LicenseRegistry.Reset();
    }

    public void Dispose()
    {
        LicenseRegistry.Reset();
        GestureBinding.Instance.ResetForTests();
        FocusManager.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void LicenseEntryWithLineBreaks_ParsesParagraphsAndIndentation()
    {
        var entry = new LicenseEntryWithLineBreaks(["sample"],
            "Heading line\ncontinued line\n\n   indented item\n   continuation\n\n            centered");

        Assert.Equal(["sample"], entry.Packages);
        Assert.Equal(
        [
            new LicenseParagraph("Heading line continued line", 0),
            new LicenseParagraph("indented item continuation", 1),
            new LicenseParagraph("centered", LicenseParagraph.CenteredIndent),
        ], entry.Paragraphs);
    }

    [Fact]
    public async Task LicenseRegistry_IsLazyOrderedAndResettable()
    {
        int calls = 0;
        LicenseRegistry.AddLicense(() =>
        {
            calls++;
            return [new LicenseEntryWithLineBreaks(["first"], "First license")];
        });
        LicenseRegistry.AddLicense(() => [new LicenseEntryWithLineBreaks(["second"], "Second license")]);

        Assert.Equal(0, calls);
        var entries = new List<LicenseEntry>();
        await foreach (var entry in LicenseRegistry.Licenses()) entries.Add(entry);
        Assert.Equal(1, calls);
        Assert.Equal(["first", "second"], entries.Select(entry => entry.Packages.Single()));

        LicenseRegistry.Reset();
        entries.Clear();
        await foreach (var entry in LicenseRegistry.Licenses()) entries.Add(entry);
        Assert.Empty(entries);
    }

    // ---- Scheduler.ScheduleTask (primitive landed for _PackageLicensePageState._initLicenses) ----

    [Fact]
    public void ScheduleTask_RunsInPriorityOrderAndKeepsInsertionOrderWithinAPriority()
    {
        var order = new List<string>();
        _ = Scheduler.ScheduleTask(() => { order.Add("idle-a"); return 0; }, Priority.Idle);
        _ = Scheduler.ScheduleTask(() => { order.Add("touch"); return 0; }, Priority.Touch);
        _ = Scheduler.ScheduleTask(() => { order.Add("idle-b"); return 0; }, Priority.Idle);
        _ = Scheduler.ScheduleTask(() => { order.Add("animation"); return 0; }, Priority.Animation);

        Assert.Empty(order);
        while (Scheduler.HandleEventLoopCallback())
        {
        }

        Assert.Equal(["touch", "animation", "idle-a", "idle-b"], order);
    }

    [Fact]
    public async Task ScheduleTask_CompletesWithTheResultAndSurfacesFailures()
    {
        var completed = Scheduler.ScheduleTask(() => 42, Priority.Animation, debugLabel: "answer");
        var failing = Scheduler.ScheduleTask<int>(
            () => throw new InvalidOperationException("boom"),
            Priority.Animation);

        while (Scheduler.HandleEventLoopCallback())
        {
        }

        Assert.Equal(42, await completed);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => failing);
        Assert.Equal("boom", error.Message);
    }

    [Fact]
    public void DefaultSchedulingStrategy_SkipsSubAnimationTasksWhileTransientCallbacksArePending()
    {
        // With no ticking tickers every priority is admitted.
        Assert.True(Scheduler.DefaultSchedulingStrategy(Priority.Idle.Value));

        Scheduler.SchedulingStrategy = _ => false;
        bool ran = false;
        _ = Scheduler.ScheduleTask(() => { ran = true; return 0; }, Priority.Idle);

        Assert.True(Scheduler.HandleEventLoopCallback());
        Assert.False(ran);
    }

    [Fact]
    public void Priority_ClampsRelativeOffsets()
    {
        Assert.Equal(0, Priority.Idle.Value);
        Assert.Equal(100000, Priority.Animation.Value);
        Assert.Equal(200000, Priority.Touch.Value);
        Assert.Equal(100001, (Priority.Animation + 1).Value);
        Assert.Equal(99999, (Priority.Animation - 1).Value);
        Assert.Equal(100000 + Priority.MaxOffset, (Priority.Animation + 999999).Value);
        Assert.Equal(100000 - Priority.MaxOffset, (Priority.Animation - 999999).Value);
    }

    // ---- AboutDialog / AboutListTile ----

    [Fact]
    public void AboutDialog_ComposesApplicationDetailsChildrenAndM3Actions()
    {
        var dialog = new AboutDialog(
            applicationName: "Plumix Demo",
            applicationVersion: "2.4.0",
            applicationIcon: new Icon(Icons.InfoOutline),
            applicationLegalese: "Copyright 2026",
            children: [new Text("Built with widgets")]);
        using var harness = new WidgetRenderHarness(BuildThemed(dialog));

        harness.Pump(new Size(640, 480));
        Assert.NotNull(FindParagraph(harness.RenderView, "Plumix Demo"));
        Assert.NotNull(FindParagraph(harness.RenderView, "2.4.0"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Copyright 2026"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Built with widgets"));
        Assert.NotNull(FindParagraph(harness.RenderView, "View licenses"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Close"));
        Assert.Contains(FindDescendants<RenderPadding>(harness.RenderView), padding =>
            padding.Padding == new Thickness(24, 0));
    }

    [Fact]
    public void AboutDialog_M2UppercasesActions()
    {
        using var harness = new WidgetRenderHarness(BuildThemed(
            new AboutDialog(applicationName: "Legacy"),
            ThemeData.Light with { UseMaterial3 = false }));
        harness.Pump(new Size(560, 420));
        Assert.NotNull(FindParagraph(harness.RenderView, "VIEW LICENSES"));
        Assert.NotNull(FindParagraph(harness.RenderView, "CLOSE"));
    }

    [Theory]
    [InlineData(TargetPlatform.IOS, true)]
    [InlineData(TargetPlatform.MacOS, true)]
    [InlineData(TargetPlatform.Android, false)]
    [InlineData(TargetPlatform.Linux, false)]
    [InlineData(TargetPlatform.Windows, false)]
    public void AdaptiveAboutDialog_UsesCupertinoActionsOnApplePlatformsOnly(
        TargetPlatform platform,
        bool expectsCupertino)
    {
        var dialog = AboutDialog.Adaptive(applicationName: "Adaptive", applicationVersion: "1");
        Assert.Equal("Adaptive", dialog.ApplicationName);
        Assert.Equal("1", dialog.ApplicationVersion);

        using var harness = new WidgetRenderHarness(BuildThemed(
            dialog,
            ThemeData.Light with { Platform = platform }));
        harness.Pump(new Size(640, 480));

        Assert.Equal(expectsCupertino, FindWidgets<CupertinoDialogAction>(harness.RootElement).Count > 0);
        Assert.NotNull(FindParagraph(harness.RenderView, "View licenses"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Close"));
    }

    [Fact]
    public void AboutListTile_UsesLocalizedDefaultTitleAndDenseForwarding()
    {
        var tile = new AboutListTile(
            applicationName: "Plumix",
            icon: new Icon(Icons.InfoOutline),
            dense: true);
        Assert.True(tile.Dense);
        Assert.NotNull(tile.Icon);

        using var harness = new WidgetRenderHarness(BuildThemed(tile));
        harness.Pump(new Size(420, 100));
        Assert.NotNull(FindParagraph(harness.RenderView, "About Plumix"));
    }

    [Theory]
    [InlineData(null, 56.0)]
    [InlineData(false, 56.0)]
    [InlineData(true, 48.0)]
    public void AboutListTile_DensePropertyIsApplied(bool? dense, double expectedHeight)
    {
        using var harness = new WidgetRenderHarness(BuildThemed(
            new Plumix.Material.Material(child: new AboutListTile(applicationName: "Plumix", dense: dense))));
        harness.Pump(new Size(420, 200));

        var tile = FindDescendants<RenderParagraph>(harness.RenderView)
            .First(paragraph => paragraph.PlainText == "About Plumix");
        var box = FindEnclosingListTileBox(tile);
        Assert.Equal(expectedHeight, box.Size.Height, 3);
    }

    [Fact]
    public void AboutListTile_WithExplicitChildAndNoIconRendersNoIcon()
    {
        using var harness = new WidgetRenderHarness(BuildThemed(
            new AboutListTile(child: new Text("Custom about"))));
        harness.Pump(new Size(420, 100));

        Assert.NotNull(FindParagraph(harness.RenderView, "Custom about"));
        Assert.Empty(FindWidgets<Icon>(harness.RootElement));
    }

    [Fact]
    public void AboutControls_DefaultApplicationNameComesFromNearestTitle()
    {
        using var harness = new WidgetRenderHarness(new Title(
            title: "Titled application",
            color: Colors.CornflowerBlue,
            child: BuildThemed(new Column(
                children:
                [
                    new AboutListTile(),
                    new AboutDialog(),
                ]))));

        harness.Pump(new Size(640, 480));

        Assert.NotNull(FindParagraph(harness.RenderView, "About Titled application"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Titled application"));
    }

    [Fact]
    public void ShowAboutDialog_PushesDialogAndClosePopsIt()
    {
        BuildContext? capturedContext = null;
        var navigator = new Navigator(
            onGenerateRoute: settings => new BuilderPageRoute(
                builder: context => new CaptureContext(
                    value => capturedContext = value,
                    new Text("Home")),
                settings: settings));
        using var harness = new WidgetRenderHarness(BuildThemed(navigator));
        harness.Pump(new Size(640, 480));

        Assert.NotNull(capturedContext);
        AboutDialogs.ShowAboutDialog(capturedContext!.Value, applicationName: "Dialog app");
        harness.Pump(new Size(640, 480));
        Assert.NotNull(FindParagraph(harness.RenderView, "Dialog app"));

        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.25));
        var semantics = harness.PumpAndGetSemantics(new Size(640, 480));
        var actionNodes = FindNodes(semantics!, node => node.Actions.HasFlag(SemanticsActions.Tap)).ToArray();
        Assert.True(actionNodes.Length >= 2);
        Assert.True(harness.PerformSemanticsAction(actionNodes[^1].Id, SemanticsActions.Tap));
        now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.25));
        harness.Pump(new Size(640, 480));
        Assert.Null(FindParagraph(harness.RenderView, "Dialog app"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Home"));
    }

    [Fact]
    public void ShowAboutDialog_DefaultsToRootNavigator()
    {
        BuildContext? innerContext = null;
        var outer = new Navigator(
            onGenerateRoute: settings => new BuilderPageRoute(
                builder: _ => new Navigator(
                    onGenerateRoute: innerSettings => new BuilderPageRoute(
                        builder: context => new CaptureContext(
                            value => innerContext = value,
                            new Text("Nested home")),
                        settings: innerSettings)),
                settings: settings));
        using var harness = new WidgetRenderHarness(BuildThemed(outer));
        harness.Pump(new Size(640, 480));

        Assert.True(innerContext.HasValue);
        AboutDialogs.ShowAboutDialog(innerContext.Value, applicationName: "Root dialog");
        harness.Pump(new Size(640, 480));

        Assert.True(Navigator.Of(innerContext.Value, rootNavigator: true).CanPop);
        Assert.False(Navigator.Of(innerContext.Value).CanPop);
        Assert.NotNull(FindParagraph(harness.RenderView, "Root dialog"));
    }

    [Fact]
    public void ShowAboutDialog_UsesNestedNavigatorWhenUseRootNavigatorIsFalse()
    {
        BuildContext? innerContext = null;
        var outer = new Navigator(
            onGenerateRoute: settings => new BuilderPageRoute(
                builder: _ => new Navigator(
                    onGenerateRoute: innerSettings => new BuilderPageRoute(
                        builder: context => new CaptureContext(
                            value => innerContext = value,
                            new Text("Nested home")),
                        settings: innerSettings)),
                settings: settings));
        using var harness = new WidgetRenderHarness(BuildThemed(outer));
        harness.Pump(new Size(640, 480));

        AboutDialogs.ShowAboutDialog(innerContext!.Value, applicationName: "Nested dialog", useRootNavigator: false);
        harness.Pump(new Size(640, 480));

        Assert.True(Navigator.Of(innerContext.Value).CanPop);
        Assert.NotNull(FindParagraph(harness.RenderView, "Nested dialog"));
    }

    [Fact]
    public void ShowLicensePage_UsesNestedNavigatorByDefaultAndRootWhenAsked()
    {
        BuildContext? innerContext = null;
        var outer = new Navigator(
            onGenerateRoute: settings => new BuilderPageRoute(
                builder: _ => new Navigator(
                    onGenerateRoute: innerSettings => new BuilderPageRoute(
                        builder: context => new CaptureContext(
                            value => innerContext = value,
                            new Text("Nested home")),
                        settings: innerSettings)),
                settings: settings));
        using var harness = new WidgetRenderHarness(BuildThemed(outer));
        harness.Pump(new Size(NestedWidth, 560));

        AboutDialogs.ShowLicensePage(innerContext!.Value, applicationName: "Nested licenses");
        harness.Pump(new Size(NestedWidth, 560));
        Assert.True(Navigator.Of(innerContext.Value).CanPop);
        Assert.False(Navigator.Of(innerContext.Value, rootNavigator: true).CanPop);

        AboutDialogs.ShowLicensePage(
            innerContext.Value,
            applicationName: "Root licenses",
            useRootNavigator: true);
        harness.Pump(new Size(NestedWidth, 560));
        Assert.True(Navigator.Of(innerContext.Value, rootNavigator: true).CanPop);
    }

    // ---- LicensePage: shared behavior ----

    [Fact]
    public void LicensePage_GroupsSortsAndNavigatesToPackageParagraphs()
    {
        RegisterSampleLicenses();
        using var harness = LicensePageHarness();

        PumpUntilLoaded(harness, NestedWidth, () => FindParagraph(harness.RenderView, "app") is not null);
        string[] paragraphs = Paragraphs(harness);
        Assert.True(Array.IndexOf(paragraphs, "app") < Array.IndexOf(paragraphs, "alpha"));
        Assert.True(Array.IndexOf(paragraphs, "alpha") < Array.IndexOf(paragraphs, "zeta"));
        Assert.Contains("1 license.", paragraphs);
        Assert.Contains("2 licenses.", paragraphs);
        Assert.Contains("Powered by Flutter", paragraphs);
        Assert.Contains("Licenses", paragraphs);

        TapText(harness, NestedWidth, "app");
        PumpUntilLoaded(harness, NestedWidth, () => FindParagraph(harness.RenderView, "App license") is not null);
        Assert.NotNull(FindParagraph(harness.RenderView, "App license"));
    }

    [Fact]
    public void LicensePage_UsesExplicitApplicationMetadataOverTheAncestorTitle()
    {
        RegisterSampleLicenses();
        using var harness = new WidgetRenderHarness(new Title(
            title: "Ancestor title",
            color: Colors.CornflowerBlue,
            child: BuildThemed(NavigatorHosting(new LicensePage(
                applicationName: "License Demo",
                applicationVersion: "1.0",
                applicationIcon: new Icon(Icons.InfoOutline),
                applicationLegalese: "Legal")))));

        PumpUntilLoaded(harness, NestedWidth, () => FindParagraph(harness.RenderView, "License Demo") is not null);
        string[] paragraphs = Paragraphs(harness);
        Assert.Contains("License Demo", paragraphs);
        Assert.Contains("1.0", paragraphs);
        Assert.Contains("Legal", paragraphs);
        Assert.DoesNotContain("Ancestor title", paragraphs);
    }

    [Fact]
    public void LicensePage_DefaultsToTheAncestorTitleForTheApplicationName()
    {
        RegisterSampleLicenses();
        using var harness = new WidgetRenderHarness(new Title(
            title: "Ancestor title",
            color: Colors.CornflowerBlue,
            child: BuildThemed(NavigatorHosting(new LicensePage()))));

        PumpUntilLoaded(harness, NestedWidth, () => FindParagraph(harness.RenderView, "Ancestor title") is not null);
        Assert.NotNull(FindParagraph(harness.RenderView, "Ancestor title"));
    }

    [Fact]
    public void LicensePage_PackagesListUsesSafeAreaPaddingWithoutAGutter()
    {
        RegisterSampleLicenses();
        using var harness = new WidgetRenderHarness(BuildThemed(
            NavigatorHosting(new LicensePage(applicationName: "Padding demo")),
            padding: new Thickness(20.0, 27.0, 12.0, 34.0)));

        PumpUntilLoaded(harness, NestedWidth, () => FindParagraph(harness.RenderView, "app") is not null);

        var sliverPadding = FindWidgets<SliverPadding>(harness.RootElement).First();
        Assert.Equal(new Thickness(20.0, 0.0, 12.0, 34.0), sliverPadding.Padding);
    }

    [Theory]
    [InlineData(700.0, 12.0)]
    [InlineData(720.0, 24.0)]
    [InlineData(800.0, 24.0)]
    public void PackageLicensePage_AddsTheGutterToTheSafeAreaPadding(double width, double expectedGutter)
    {
        RegisterSampleLicenses();
        using var harness = new WidgetRenderHarness(BuildThemed(
            NavigatorHosting(new LicensePage(applicationName: "Gutter demo")),
            padding: new Thickness(20.0, 27.0, 12.0, 34.0),
            width: width));

        PumpUntilLoaded(harness, width, () => FindParagraph(harness.RenderView, "app") is not null);
        TapText(harness, width, "app");
        PumpUntilLoaded(harness, width, () => FindParagraph(harness.RenderView, "App license") is not null);

        var detailPadding = FindWidgets<SliverPadding>(harness.RootElement).Last();
        Assert.Equal(
            new Thickness(expectedGutter + 20.0, 0.0, expectedGutter + 12.0, expectedGutter + 34.0),
            detailPadding.Padding);
    }

    [Fact]
    public void PackageLicensePage_RendersCenteredAndIndentedParagraphs()
    {
        LicenseRegistry.AddLicense(() =>
        [
            new LicenseEntryWithLineBreaks(["app"],
                "Plain line\n\n   one level in\n\n      two levels in\n\n            centered heading"),
        ]);
        using var harness = LicensePageHarness();

        PumpUntilLoaded(harness, NestedWidth, () => FindParagraph(harness.RenderView, "app") is not null);
        TapText(harness, NestedWidth, "app");
        PumpUntilLoaded(harness, NestedWidth, () => FindParagraph(harness.RenderView, "Plain line") is not null);

        Assert.Equal(new Thickness(0.0, 8.0, 0.0, 0.0), PaddingAroundText(harness, "Plain line"));
        Assert.Equal(new Thickness(16.0, 8.0, 0.0, 0.0), PaddingAroundText(harness, "one level in"));
        Assert.Equal(new Thickness(32.0, 8.0, 0.0, 0.0), PaddingAroundText(harness, "two levels in"));
        Assert.Equal(new Thickness(0.0, 16.0, 0.0, 0.0), PaddingAroundText(harness, "centered heading"));

        // The divider that separates each license entry.
        Assert.Contains(FindDescendants<RenderPadding>(harness.RenderView), padding =>
            padding.Padding == new Thickness(18.0));
    }

    [Fact]
    public void PackageLicensePage_UsesExactlyOneScrollbarAndAPrimaryListView()
    {
        RegisterSampleLicenses();
        using var harness = LicensePageHarness();

        PumpUntilLoaded(harness, NestedWidth, () => FindParagraph(harness.RenderView, "app") is not null);
        TapText(harness, NestedWidth, "app");
        PumpUntilLoaded(harness, NestedWidth, () => FindParagraph(harness.RenderView, "App license") is not null);

        Assert.Single(FindWidgets<Plumix.Material.Scrollbar>(harness.RootElement));
        Assert.Empty(FindWidgets<Plumix.Widgets.Scrollbar>(harness.RootElement));
        var detailList = FindWidgets<ListView>(harness.RootElement).Last();
        Assert.True(detailList.Primary);
        Assert.NotEmpty(FindWidgets<ScrollConfiguration>(harness.RootElement));
    }

    [Fact]
    public void LicensePage_AboutProgramKeepsFlutterVerticalRhythm()
    {
        RegisterSampleLicenses();
        using var harness = new WidgetRenderHarness(BuildThemed(NavigatorHosting(new LicensePage(
            applicationName: "Rhythm",
            applicationVersion: "9.9",
            applicationLegalese: "Legalese line"))));

        PumpUntilLoaded(harness, NestedWidth, () => FindParagraph(harness.RenderView, "Rhythm") is not null);

        double nameBottom = Bottom(harness, "Rhythm");
        double versionTop = Top(harness, "9.9");
        double versionBottom = Bottom(harness, "9.9");
        double legaleseTop = Top(harness, "Legalese line");
        double legaleseBottom = Bottom(harness, "Legalese line");
        double poweredTop = Top(harness, "Powered by Flutter");

        Assert.Equal(0.0, versionTop - nameBottom, 3);
        Assert.Equal(AboutDialog.TextVerticalSeparation, legaleseTop - versionBottom, 3);
        Assert.Equal(AboutDialog.TextVerticalSeparation, poweredTop - legaleseBottom, 3);
    }

    [Fact]
    public void LicensePage_SurfacesAStreamErrorAsCenteredText()
    {
        LicenseRegistry.AddLicense(() => ThrowingCollector());
        using var harness = LicensePageHarness();

        PumpUntilLoaded(
            harness,
            NestedWidth,
            () => FindDescendants<RenderParagraph>(harness.RenderView)
                .Any(paragraph => paragraph.PlainText.Contains("Injected failure", StringComparison.Ordinal)));

        Assert.Contains(
            FindDescendants<RenderParagraph>(harness.RenderView),
            paragraph => paragraph.PlainText.Contains("Injected failure", StringComparison.Ordinal));
    }

    [Fact]
    public void LicensePage_LoadingAndLoadedStatesShareTheCardColor()
    {
        RegisterSampleLicenses();
        using var harness = LicensePageHarness();

        harness.Pump(new Size(NestedWidth, 560));
        var loadingSurfaces = FindWidgets<Plumix.Material.Material>(harness.RootElement)
            .Where(material => material.Color == ThemeData.Light.CardColor)
            .ToList();
        Assert.NotEmpty(loadingSurfaces);
        // The loading branch has no elevation; the done branch raises the same colored surface.
        Assert.All(loadingSurfaces, material => Assert.Equal(0.0, material.Elevation));

        PumpUntilLoaded(harness, NestedWidth, () => FindParagraph(harness.RenderView, "app") is not null);
        var loadedSurfaces = FindWidgets<Plumix.Material.Material>(harness.RootElement)
            .Where(material => material.Color == ThemeData.Light.CardColor)
            .ToList();
        Assert.Contains(loadedSurfaces, material => material.Elevation == AboutDialogs.CardElevation);
    }

    [Fact]
    public void LicensePage_ConstrainsThePackagesListTo600Logical()
    {
        RegisterSampleLicenses();
        using var harness = LicensePageHarness();

        PumpUntilLoaded(harness, NestedWidth, () => FindParagraph(harness.RenderView, "app") is not null);

        Assert.Contains(
            FindWidgets<ConstrainedBox>(harness.RootElement),
            box => box.Constraints.MaxWidth == 600.0);
    }

    [Fact]
    public void LicensePage_RendersAtZeroArea()
    {
        RegisterSampleLicenses();
        using var harness = LicensePageHarness();

        harness.Pump(new Size(0, 0));
        Assert.NotNull(harness.RenderView.Child);
    }

    [Fact]
    public void AboutDialogAndListTile_RenderAtZeroArea()
    {
        using var dialogHarness = new WidgetRenderHarness(BuildThemed(new AboutDialog()));
        dialogHarness.Pump(new Size(0, 0));
        Assert.NotNull(dialogHarness.RenderView.Child);

        using var tileHarness = new WidgetRenderHarness(BuildThemed(new AboutListTile()));
        tileHarness.Pump(new Size(0, 0));
        Assert.NotNull(tileHarness.RenderView.Child);
    }

    // ---- LicensePage: master-detail flow ----

    [Fact]
    public void LicensePage_NestedLayoutBelowTheBreakpointPushesAndPopsTheDetailRoute()
    {
        RegisterSampleLicenses();
        using var harness = LicensePageHarness(839.0);

        PumpUntilLoaded(harness, 839.0, () => FindParagraph(harness.RenderView, "app") is not null);
        Assert.Empty(FindWidgets<MasterDetailScaffold>(harness.RootElement));
        Assert.Single(FindWidgets<MasterDetailFlow>(harness.RootElement));

        var innerNavigator = FindStates<NavigatorState>(harness.RootElement).Last();
        Assert.False(innerNavigator.CanPop);

        TapText(harness, 839.0, "app");
        PumpUntilLoaded(harness, 839.0, () => FindParagraph(harness.RenderView, "App license") is not null);

        // The nested flow pushed the detail route onto its own navigator, not onto the host navigator.
        Assert.Single(FindStates<MasterDetailFlowState>(harness.RootElement));
        Assert.True(innerNavigator.CanPop);

        innerNavigator.Pop();
        PumpUntilLoaded(harness, 839.0, () => FindParagraph(harness.RenderView, "App license") is null);
        Assert.NotNull(FindParagraph(harness.RenderView, "Licenses"));
        Assert.False(innerNavigator.CanPop);
    }

    [Fact]
    public void LicensePage_LateralLayoutAtOrAboveTheBreakpointShowsMasterAndDetailTogether()
    {
        RegisterSampleLicenses();
        using var harness = LicensePageHarness(840.0);

        PumpUntilLoaded(harness, 840.0, () => FindParagraph(harness.RenderView, "App license") is not null);

        Assert.Single(FindWidgets<MasterDetailScaffold>(harness.RootElement));
        // Master list and the initial detail page are on screen at the same time.
        Assert.NotNull(FindParagraph(harness.RenderView, "Licenses"));
        Assert.NotNull(FindParagraph(harness.RenderView, "alpha"));
        Assert.NotNull(FindParagraph(harness.RenderView, "App license"));
        Assert.NotEmpty(FindWidgets<DraggableScrollableSheet>(harness.RootElement));
    }

    [Fact]
    public void LicensePage_LateralMasterPanelIsWidthLimitedAndTheDetailStartsAtTheCardOverlap()
    {
        RegisterSampleLicenses();
        using var harness = LicensePageHarness(LateralWidth);

        PumpUntilLoaded(harness, LateralWidth, () => FindParagraph(harness.RenderView, "App license") is not null);

        var masterPanel = FindWidgets<ConstrainedBox>(harness.RootElement)
            .First(box => box.Constraints.MaxWidth == AboutDialogs.MasterViewWidth);
        Assert.Equal(AboutDialogs.MasterViewWidth, masterPanel.Constraints.MaxWidth);

        Assert.Contains(
            FindWidgets<Padding>(harness.RootElement).Select(padding => padding.InsetsGeometry),
            geometry => Math.Abs(
                geometry.Start - (AboutDialogs.MasterViewWidth - AboutDialogs.CardElevation)) < 0.001);
    }

    [Fact]
    public void LicensePage_LateralSelectionSwapsTheDetailPage()
    {
        RegisterSampleLicenses();
        using var harness = LicensePageHarness(LateralWidth);

        PumpUntilLoaded(harness, LateralWidth, () => FindParagraph(harness.RenderView, "App license") is not null);

        TapText(harness, LateralWidth, "alpha");
        PumpUntilLoaded(harness, LateralWidth, () => FindParagraph(harness.RenderView, "Alpha license") is not null);

        Assert.NotNull(FindParagraph(harness.RenderView, "Alpha license"));
        // The master list is still visible next to the detail sheet.
        Assert.NotNull(FindParagraph(harness.RenderView, "alpha"));
    }

    [Fact]
    public void LicensePage_LateralDetailTitleUsesTheTextThemeNotTheAppBarForegroundColor()
    {
        RegisterSampleLicenses();
        var theme = ThemeData.Light with
        {
            AppBarTheme = ThemeData.Light.AppBarTheme with { ForegroundColor = Colors.Magenta },
        };
        BuildContext? inner = null;
        using var harness = new WidgetRenderHarness(BuildThemed(
            new CaptureContext(
                value => inner = value,
                NavigatorHosting(new LicensePage(applicationName: "License ABC"))),
            theme,
            width: LateralWidth));

        PumpUntilLoaded(harness, LateralWidth, () => FindParagraph(harness.RenderView, "App license") is not null);

        var resolved = Theme.Of(inner!.Value);
        var title = FindWidgets<PackageLicensePageTitle>(harness.RootElement).Single();
        Assert.Null(title.ForegroundColor);
        Assert.Equal(resolved.TextTheme.TitleLarge, title.TitleTextStyle);
        Assert.Equal(resolved.TextTheme, title.Theme);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LicensePage_NestedDetailTitleFollowsTheMaterialVersionTextTheme(bool useMaterial3)
    {
        RegisterSampleLicenses();
        var theme = ThemeData.Light with { UseMaterial3 = useMaterial3 };
        BuildContext? inner = null;
        using var harness = new WidgetRenderHarness(BuildThemed(
            new CaptureContext(
                value => inner = value,
                NavigatorHosting(new LicensePage(applicationName: "License ABC"))),
            theme));

        PumpUntilLoaded(harness, NestedWidth, () => FindParagraph(harness.RenderView, "app") is not null);
        TapText(harness, NestedWidth, "app");
        PumpUntilLoaded(harness, NestedWidth, () => FindParagraph(harness.RenderView, "App license") is not null);

        var resolved = Theme.Of(inner!.Value);
        var title = FindWidgets<PackageLicensePageTitle>(harness.RootElement).Single();
        Assert.Equal(useMaterial3 ? resolved.TextTheme : resolved.PrimaryTextTheme, title.Theme);
        Assert.NotEqual(useMaterial3 ? resolved.PrimaryTextTheme : resolved.TextTheme, title.Theme);
        Assert.Equal(resolved.AppBarTheme.ForegroundColor, title.ForegroundColor);
        Assert.Equal(resolved.AppBarTheme.TitleTextStyle, title.TitleTextStyle);
    }

    [Fact]
    public void LicenseData_PinsTheFirstPackageAndSortsTheRestCaseInsensitively()
    {
        var data = new LicenseData();
        data.AddLicense(new LicenseEntryWithLineBreaks(["zApp", "Beta"], "first"));
        data.AddLicense(new LicenseEntryWithLineBreaks(["alpha"], "second"));
        data.AddLicense(new LicenseEntryWithLineBreaks(["Beta"], "third"));

        Assert.Equal("zApp", data.FirstPackage);
        data.SortPackages();
        Assert.Equal(["zApp", "alpha", "Beta"], data.Packages);
        Assert.Equal([0], data.PackageLicenseBindings["zApp"]);
        Assert.Equal([0, 2], data.PackageLicenseBindings["Beta"]);
        Assert.Equal([1], data.PackageLicenseBindings["alpha"]);
        Assert.Equal(3, data.Licenses.Count);
    }

    [Fact]
    public void DetailArguments_CompareByPackageNameOnly()
    {
        var first = new DetailArguments("pkg", [new LicenseEntryWithLineBreaks(["pkg"], "a")]);
        var second = new DetailArguments("pkg", [new LicenseEntryWithLineBreaks(["pkg"], "b")]);
        var third = new DetailArguments("other", [new LicenseEntryWithLineBreaks(["other"], "a")]);

        Assert.Equal(first, second);
        Assert.NotEqual(first, third);
    }

    [Fact]
    public void MasterDetailFlow_OfThrowsOutsideTheFlow()
    {
        BuildContext? captured = null;
        using var harness = new WidgetRenderHarness(BuildThemed(
            new CaptureContext(value => captured = value, new Text("bare"))));
        harness.Pump(new Size(400, 300));

        Assert.Throws<InvalidOperationException>(() => MasterDetailFlow.Of(captured!.Value));
    }

    // ---- helpers ----

    private static void RegisterSampleLicenses()
    {
        LicenseRegistry.AddLicense(() =>
        [
            new LicenseEntryWithLineBreaks(["app", "zeta"], "App license"),
            new LicenseEntryWithLineBreaks(["alpha"], "Alpha license"),
            new LicenseEntryWithLineBreaks(["zeta"], "Second zeta license"),
        ]);
    }

    private static async IAsyncEnumerable<LicenseEntry> ThrowingCollector()
    {
        await Task.CompletedTask;
        throw new InvalidOperationException("Injected failure");
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }

    private static Widget NavigatorHosting(Widget page) => new Navigator(
        onGenerateRoute: settings => new BuilderPageRoute(builder: _ => page, settings: settings));

    private static WidgetRenderHarness LicensePageHarness(double width = NestedWidth) => new(BuildThemed(
        NavigatorHosting(new LicensePage(
            applicationName: "License Demo",
            applicationVersion: "1.0",
            applicationLegalese: "Legal")),
        width: width));

    private static Widget BuildThemed(
        Widget child,
        ThemeData? theme = null,
        Thickness? padding = null,
        double width = NestedWidth) =>
        new MediaQuery(
            new MediaQueryData(Size: new Size(width, 560), Padding: padding ?? default),
            new Directionality(
                TextDirection.Ltr,
                new Localizations(
                    locale: new Locale("en", "US"),
                    delegates: [DefaultWidgetsLocalizations.Delegate, DefaultMaterialLocalizations.Delegate],
                    child: new Theme(theme ?? ThemeData.Light, child))));

    private static void PumpUntilLoaded(WidgetRenderHarness harness, double width, Func<bool> condition)
    {
        var size = new Size(width, 560);
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            while (Scheduler.HandleEventLoopCallback())
            {
            }

            Scheduler.FlushMicrotasks();
            harness.Pump(size);
            double now = Scheduler.CurrentSeconds;
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.6));
            harness.Pump(size);
            Thread.Sleep(2);
        }

        harness.Pump(size);
        Assert.True(condition(), "Asynchronous license data did not reach the expected state.");
    }

    private static void TapText(WidgetRenderHarness harness, double width, string text)
    {
        var size = new Size(width, 560);
        var semantics = harness.PumpAndGetSemantics(size);
        var target = FindNodes(semantics!, node => node.Actions.HasFlag(SemanticsActions.Tap)
                                                   && NodeLabels(node).Contains(text)).First();
        Assert.True(harness.PerformSemanticsAction(target.Id, SemanticsActions.Tap));
        harness.Pump(size);
    }

    private static IEnumerable<string> NodeLabels(SemanticsNode node)
    {
        if (!string.IsNullOrEmpty(node.Label))
        {
            foreach (string part in node.Label.Split('\n'))
            {
                yield return part;
            }
        }

        foreach (var child in node.Children)
        {
            foreach (string label in NodeLabels(child))
            {
                yield return label;
            }
        }
    }

    private static string[] Paragraphs(WidgetRenderHarness harness) =>
        FindDescendants<RenderParagraph>(harness.RenderView).Select(item => item.PlainText).ToArray();

    private static double Top(WidgetRenderHarness harness, string text) =>
        ParagraphFor(harness, text).GetPaintOffsetToRoot().Y;

    private static double Bottom(WidgetRenderHarness harness, string text)
    {
        var paragraph = ParagraphFor(harness, text);
        return paragraph.GetPaintOffsetToRoot().Y + paragraph.Size.Height;
    }

    private static RenderParagraph ParagraphFor(WidgetRenderHarness harness, string text) =>
        FindDescendants<RenderParagraph>(harness.RenderView).First(item => item.PlainText == text);

    private static Thickness PaddingAroundText(WidgetRenderHarness harness, string text)
    {
        var paragraph = ParagraphFor(harness, text);
        for (var node = paragraph.Parent; node is not null; node = node.Parent)
        {
            if (node is RenderPadding padding)
            {
                return padding.Padding;
            }
        }

        throw new InvalidOperationException($"No RenderPadding found above '{text}'.");
    }

    private static RenderBox FindEnclosingListTileBox(RenderObject leaf)
    {
        for (var node = leaf.Parent; node is not null; node = node.Parent)
        {
            if (node is RenderBox box && box.GetType().Name.Contains("ListTile", StringComparison.Ordinal))
            {
                return box;
            }
        }

        throw new InvalidOperationException("No list-tile render box found.");
    }

    private static RenderParagraph? FindParagraph(RenderObject? root, string text) =>
        FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.PlainText == text);

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null) return result;
        if (root is T typed) result.Add(typed);
        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private static List<T> FindWidgets<T>(Element? root) where T : Widget
    {
        var result = new List<T>();
        if (root is null) return result;
        if (root.Widget is T typed) result.Add(typed);
        root.VisitChildren(child => result.AddRange(FindWidgets<T>(child)));
        return result;
    }

    private static List<T> FindStates<T>(Element? root) where T : State
    {
        var result = new List<T>();
        if (root is null) return result;
        if (root is StatefulElement stateful && stateful.State is T typed) result.Add(typed);
        root.VisitChildren(child => result.AddRange(FindStates<T>(child)));
        return result;
    }

    private static IEnumerable<SemanticsNode> FindNodes(SemanticsNode node, Func<SemanticsNode, bool> predicate)
    {
        if (predicate(node)) yield return node;
        foreach (var child in node.Children)
        foreach (var result in FindNodes(child, predicate))
            yield return result;
    }

    private sealed class CaptureContext(Action<BuildContext> capture, Widget child) : StatelessWidget
    {
        public override Widget Build(BuildContext context)
        {
            capture(context);
            return child;
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
            _rootElement.Mount(null, null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public Element RootElement => _rootElement;

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

        public bool PerformSemanticsAction(int id, SemanticsActions action) =>
            _pipeline.SemanticsOwner.PerformAction(id, action);

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
            public void InsertRenderObjectChild(RenderObject child, object? slot) => _renderView.Child = Assert.IsAssignableFrom<RenderBox>(child);
            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
            public void RemoveRenderObjectChild(RenderObject child, object? slot) { if (ReferenceEquals(_renderView.Child, child)) _renderView.Child = null; }
            internal override void Unmount() { if (_child is not null) { UnmountChild(_child); _child = null; } base.Unmount(); }
        }
    }
}
