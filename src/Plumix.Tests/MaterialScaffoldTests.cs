using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialScaffoldTests
{
    public MaterialScaffoldTests()
    {
        NavigatorBackButtonDispatcher.ResetForTests();
        SystemChrome.ResetSystemUiOverlayStyleForTests();
    }

    [Fact]
    public void Scaffold_UsesThemeScaffoldBackgroundColor()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ScaffoldBackgroundColor = Colors.Beige
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new Scaffold(
                    body: new SizedBox(width: 24, height: 12))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var background = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        Assert.Equal(Colors.Beige, background.Color);
    }

    [Fact]
    public void Scaffold_UsesExplicitBackgroundColorOverride()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    ScaffoldBackgroundColor = Colors.White
                },
                child: new Scaffold(
                    backgroundColor: Colors.Crimson,
                    body: new SizedBox(width: 24, height: 12))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var background = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        Assert.Equal(Colors.Crimson, background.Color);
    }

    [Fact]
    public void Drawer_DefaultWidth_Is304()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new Drawer(
                    child: new SizedBox(width: 24, height: 12))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var constrained = FindConstrainedBox(
            root.ChildElement?.RenderObject,
            constraints =>
                Math.Abs(constraints.MinWidth - 304) < 0.001
                && Math.Abs(constraints.MaxWidth - 304) < 0.001);

        Assert.NotNull(constrained);
    }

    [Fact]
    public void Drawer_UsesDrawerThemeDefaults_WhenWidgetValuesAreNull()
    {
        var owner = new BuildOwner();
        var drawerTheme = new DrawerThemeData(
            BackgroundColor: Colors.CadetBlue,
            ScrimColor: Color.FromArgb(0x99, 0x11, 0x22, 0x33),
            Elevation: 12,
            ShadowColor: Colors.Goldenrod,
            Width: 280);

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    DrawerTheme = drawerTheme
                },
                child: new Drawer(
                    child: new SizedBox(width: 24, height: 12))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var constrained = FindConstrainedBox(
            root.ChildElement?.RenderObject,
            constraints =>
                Math.Abs(constraints.MinWidth - 280) < 0.001
                && Math.Abs(constraints.MaxWidth - 280) < 0.001);
        Assert.NotNull(constrained);

        var decorated = FindDescendant<RenderDecoratedBox>(root.ChildElement?.RenderObject);
        Assert.NotNull(decorated);
        Assert.Equal(Colors.CadetBlue, decorated!.Decoration.Color);
        Assert.True(decorated.Decoration.BoxShadows.HasValue);

        var shadows = decorated.Decoration.BoxShadows!.Value;
        Assert.True(shadows.Count > 0);
        for (var i = 0; i < shadows.Count; i++)
        {
            var shadow = shadows[i];
            Assert.Equal(Colors.Goldenrod.R, shadow.Color.R);
            Assert.Equal(Colors.Goldenrod.G, shadow.Color.G);
            Assert.Equal(Colors.Goldenrod.B, shadow.Color.B);
            Assert.True(shadow.Color.A > 0);
        }
    }

    [Fact]
    public void Drawer_WidgetValues_OverrideDrawerThemeDefaults()
    {
        var owner = new BuildOwner();
        var drawerTheme = new DrawerThemeData(
            BackgroundColor: Colors.CadetBlue,
            ScrimColor: Color.FromArgb(0x99, 0x11, 0x22, 0x33),
            Elevation: 12,
            ShadowColor: Colors.Goldenrod,
            Width: 280);

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    DrawerTheme = drawerTheme
                },
                child: new Drawer(
                    backgroundColor: Colors.Crimson,
                    elevation: 6,
                    shadowColor: Colors.DarkGreen,
                    width: 240,
                    child: new SizedBox(width: 24, height: 12))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var constrained = FindConstrainedBox(
            root.ChildElement?.RenderObject,
            constraints =>
                Math.Abs(constraints.MinWidth - 240) < 0.001
                && Math.Abs(constraints.MaxWidth - 240) < 0.001);
        Assert.NotNull(constrained);

        var decorated = FindDescendant<RenderDecoratedBox>(root.ChildElement?.RenderObject);
        Assert.NotNull(decorated);
        Assert.Equal(Colors.Crimson, decorated!.Decoration.Color);
        Assert.True(decorated.Decoration.BoxShadows.HasValue);

        var shadows = decorated.Decoration.BoxShadows!.Value;
        Assert.True(shadows.Count > 0);
        for (var i = 0; i < shadows.Count; i++)
        {
            var shadow = shadows[i];
            Assert.Equal(Colors.DarkGreen.R, shadow.Color.R);
            Assert.Equal(Colors.DarkGreen.G, shadow.Color.G);
            Assert.Equal(Colors.DarkGreen.B, shadow.Color.B);
            Assert.True(shadow.Color.A > 0);
        }
    }

    [Fact]
    public void Drawer_InvalidThemedWidth_ThrowsArgumentOutOfRange()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    DrawerTheme = new DrawerThemeData(Width: 0)
                },
                child: new Drawer(
                    child: new SizedBox(width: 24, height: 12))));

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            root.Attach(owner);
            root.Mount(parent: null, newSlot: null);
            owner.FlushBuild();
        });

        Assert.Equal(nameof(DrawerThemeData.Width), exception.ParamName);
    }

    [Fact]
    public void Drawer_InvalidThemedElevation_ThrowsArgumentOutOfRange()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    DrawerTheme = new DrawerThemeData(Elevation: -1)
                },
                child: new Drawer(
                    child: new SizedBox(width: 24, height: 12))));

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            root.Attach(owner);
            root.Mount(parent: null, newSlot: null);
            owner.FlushBuild();
        });

        Assert.Equal(nameof(DrawerThemeData.Elevation), exception.ParamName);
    }

    [Fact]
    public void AppBar_AutomaticallyImplyLeading_ShowsMenuIcon_WhenScaffoldHasDrawer()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new Scaffold(
                    appBar: new AppBar(titleText: "Root"),
                    drawer: new Drawer(
                        child: new SizedBox(width: 80, height: 40)),
                    body: new SizedBox(width: 24, height: 12))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var menuGlyph = char.ConvertFromUtf32(Icons.Menu.CodePoint);
        var menuParagraph = FindParagraphByText(root.ChildElement?.RenderObject, menuGlyph);
        Assert.NotNull(menuParagraph);

        var arrowBackGlyph = char.ConvertFromUtf32(Icons.ArrowBack.CodePoint);
        var arrowBackParagraph = FindParagraphByText(root.ChildElement?.RenderObject, arrowBackGlyph);
        Assert.Null(arrowBackParagraph);
    }

    [Fact]
    public void AppBar_AutomaticallyImplyLeading_False_HidesMenuIcon_WhenScaffoldHasDrawer()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new Scaffold(
                    appBar: new AppBar(
                        titleText: "Root",
                        automaticallyImplyLeading: false),
                    drawer: new Drawer(
                        child: new SizedBox(width: 80, height: 40)),
                    body: new SizedBox(width: 24, height: 12))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var menuGlyph = char.ConvertFromUtf32(Icons.Menu.CodePoint);
        var menuParagraph = FindParagraphByText(root.ChildElement?.RenderObject, menuGlyph);
        Assert.Null(menuParagraph);
    }

    [Fact]
    public void AppBar_AutomaticallyImplyActions_ShowsMenuIcon_WhenScaffoldHasEndDrawer()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new Scaffold(
                    appBar: new AppBar(titleText: "Root"),
                    endDrawer: new Drawer(
                        child: new SizedBox(width: 80, height: 40)),
                    body: new SizedBox(width: 24, height: 12))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var menuGlyph = char.ConvertFromUtf32(Icons.Menu.CodePoint);
        var menuParagraph = FindParagraphByText(root.ChildElement?.RenderObject, menuGlyph);
        Assert.NotNull(menuParagraph);
    }

    [Fact]
    public void AppBar_AutomaticallyImplyActions_False_HidesMenuIcon_WhenScaffoldHasEndDrawer()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new Scaffold(
                    appBar: new AppBar(
                        titleText: "Root",
                        automaticallyImplyActions: false),
                    endDrawer: new Drawer(
                        child: new SizedBox(width: 80, height: 40)),
                    body: new SizedBox(width: 24, height: 12))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var menuGlyph = char.ConvertFromUtf32(Icons.Menu.CodePoint);
        var menuParagraph = FindParagraphByText(root.ChildElement?.RenderObject, menuGlyph);
        Assert.Null(menuParagraph);
    }

    [Fact]
    public void ScaffoldState_OpenDrawer_AndCloseDrawer_TogglesDrawerVisibility()
    {
        var owner = new BuildOwner();
        BuildContext? scaffoldContext = null;

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new Scaffold(
                    drawer: new Drawer(
                        child: new Text("Drawer panel")),
                    body: new CaptureBuildContextWidget(
                        capture: context => scaffoldContext = context,
                        child: new SizedBox(width: 24, height: 12)))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.True(scaffoldContext.HasValue);
        var state = Scaffold.Of(scaffoldContext!.Value);
        Assert.False(state.IsDrawerOpen);
        Assert.Null(FindParagraphByText(root.ChildElement?.RenderObject, "Drawer panel"));

        state.OpenDrawer();
        owner.FlushBuild();

        Assert.True(state.IsDrawerOpen);
        Assert.NotNull(FindParagraphByText(root.ChildElement?.RenderObject, "Drawer panel"));

        state.CloseDrawer();
        owner.FlushBuild();

        Assert.False(state.IsDrawerOpen);
        Assert.Null(FindParagraphByText(root.ChildElement?.RenderObject, "Drawer panel"));
    }

    [Fact]
    public void ScaffoldState_OpenDrawer_WithoutDrawer_DoesNothing()
    {
        var owner = new BuildOwner();
        BuildContext? scaffoldContext = null;

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new Scaffold(
                    body: new CaptureBuildContextWidget(
                        capture: context => scaffoldContext = context,
                        child: new SizedBox(width: 24, height: 12)))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.True(scaffoldContext.HasValue);
        var state = Scaffold.Of(scaffoldContext!.Value);
        state.OpenDrawer();
        owner.FlushBuild();

        Assert.False(state.IsDrawerOpen);
    }

    [Fact]
    public void ScaffoldState_OpenEndDrawer_AndCloseEndDrawer_TogglesDrawerVisibility()
    {
        var owner = new BuildOwner();
        BuildContext? scaffoldContext = null;

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new Scaffold(
                    endDrawer: new Drawer(
                        child: new Text("End drawer panel")),
                    body: new CaptureBuildContextWidget(
                        capture: context => scaffoldContext = context,
                        child: new SizedBox(width: 24, height: 12)))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.True(scaffoldContext.HasValue);
        var state = Scaffold.Of(scaffoldContext!.Value);
        Assert.False(state.IsEndDrawerOpen);
        Assert.Null(FindParagraphByText(root.ChildElement?.RenderObject, "End drawer panel"));

        state.OpenEndDrawer();
        owner.FlushBuild();

        Assert.True(state.IsEndDrawerOpen);
        Assert.NotNull(FindParagraphByText(root.ChildElement?.RenderObject, "End drawer panel"));

        state.CloseEndDrawer();
        owner.FlushBuild();

        Assert.False(state.IsEndDrawerOpen);
        Assert.Null(FindParagraphByText(root.ChildElement?.RenderObject, "End drawer panel"));
    }

    [Fact]
    public void ScaffoldState_OpenEndDrawer_WithoutEndDrawer_DoesNothing()
    {
        var owner = new BuildOwner();
        BuildContext? scaffoldContext = null;

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new Scaffold(
                    body: new CaptureBuildContextWidget(
                        capture: context => scaffoldContext = context,
                        child: new SizedBox(width: 24, height: 12)))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.True(scaffoldContext.HasValue);
        var state = Scaffold.Of(scaffoldContext!.Value);
        state.OpenEndDrawer();
        owner.FlushBuild();

        Assert.False(state.IsEndDrawerOpen);
    }

    [Fact]
    public void ScaffoldState_OpenDrawer_ClosesEndDrawer()
    {
        var owner = new BuildOwner();
        BuildContext? scaffoldContext = null;

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new Scaffold(
                    drawer: new Drawer(child: new Text("Start drawer panel")),
                    endDrawer: new Drawer(child: new Text("End drawer panel")),
                    body: new CaptureBuildContextWidget(
                        capture: context => scaffoldContext = context,
                        child: new SizedBox(width: 24, height: 12)))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.True(scaffoldContext.HasValue);
        var state = Scaffold.Of(scaffoldContext!.Value);

        state.OpenEndDrawer();
        owner.FlushBuild();
        Assert.True(state.IsEndDrawerOpen);
        Assert.False(state.IsDrawerOpen);

        state.OpenDrawer();
        owner.FlushBuild();
        Assert.True(state.IsDrawerOpen);
        Assert.False(state.IsEndDrawerOpen);
    }

    [Fact]
    public void Scaffold_EdgeDrag_OpensStartDrawer()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        BuildContext? scaffoldContext = null;
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with
                {
                    Platform = TargetPlatform.Android,
                },
                child: new Scaffold(
                    drawer: new Drawer(child: new Text("Start drawer panel")),
                    body: new CaptureBuildContextWidget(
                        capture: context => scaffoldContext = context,
                        child: new SizedBox()))));

        try
        {
            harness.Pump(new Size(400, 300));
            Assert.True(scaffoldContext.HasValue);

            DispatchPointerDown(binding, harness.RenderView, pointer: 7001, position: new Point(2, 120));
            DispatchPointerMove(binding, harness.RenderView, pointer: 7001, position: new Point(220, 120));
            DispatchPointerUp(binding, harness.RenderView, pointer: 7001, position: new Point(220, 120));
            harness.Pump(new Size(400, 300));

            var state = Scaffold.Of(scaffoldContext!.Value);
            Assert.True(state.IsDrawerOpen);
            Assert.NotNull(FindParagraphByText(harness.RenderView, "Start drawer panel"));
        }
        finally
        {
            binding.ResetForTests();
        }
    }

    [Fact]
    public void Scaffold_EdgeDrag_OpensEndDrawer()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        BuildContext? scaffoldContext = null;
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with
                {
                    Platform = TargetPlatform.Android,
                },
                child: new Scaffold(
                    endDrawer: new Drawer(child: new Text("End drawer panel")),
                    body: new CaptureBuildContextWidget(
                        capture: context => scaffoldContext = context,
                        child: new SizedBox()))));

        try
        {
            harness.Pump(new Size(400, 300));
            Assert.True(scaffoldContext.HasValue);

            DispatchPointerDown(binding, harness.RenderView, pointer: 7002, position: new Point(398, 120));
            DispatchPointerMove(binding, harness.RenderView, pointer: 7002, position: new Point(180, 120));
            DispatchPointerUp(binding, harness.RenderView, pointer: 7002, position: new Point(180, 120));
            harness.Pump(new Size(400, 300));

            var state = Scaffold.Of(scaffoldContext!.Value);
            Assert.True(state.IsEndDrawerOpen);
            Assert.NotNull(FindParagraphByText(harness.RenderView, "End drawer panel"));
        }
        finally
        {
            binding.ResetForTests();
        }
    }

    [Fact]
    public void Scaffold_EdgeDrag_UsesMediaPaddingForStartDrawerActivationWidth()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        BuildContext? scaffoldContext = null;
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with
                {
                    Platform = TargetPlatform.Android,
                },
                child: new MediaQuery(
                    data: new MediaQueryData(
                        Padding: new Thickness(30, 0, 0, 0)),
                    child: new Scaffold(
                        drawer: new Drawer(child: new Text("Padded edge drawer panel")),
                        body: new CaptureBuildContextWidget(
                            capture: context => scaffoldContext = context,
                            child: new SizedBox())))));

        try
        {
            harness.Pump(new Size(400, 300));
            Assert.True(scaffoldContext.HasValue);

            DispatchPointerDown(binding, harness.RenderView, pointer: 7003, position: new Point(40, 120));
            DispatchPointerMove(binding, harness.RenderView, pointer: 7003, position: new Point(220, 120));
            DispatchPointerUp(binding, harness.RenderView, pointer: 7003, position: new Point(220, 120));
            harness.Pump(new Size(400, 300));

            var state = Scaffold.Of(scaffoldContext!.Value);
            Assert.True(state.IsDrawerOpen);
            Assert.NotNull(FindParagraphByText(harness.RenderView, "Padded edge drawer panel"));
        }
        finally
        {
            binding.ResetForTests();
        }
    }

    [Fact]
    public void Scaffold_OpenDrawer_AnimatesScrimOpacity_ToFullValue()
    {
        Scheduler.ResetForTests();
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        BuildContext? scaffoldContext = null;
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with
                {
                    Platform = TargetPlatform.Android,
                },
                child: new Scaffold(
                    drawer: new Drawer(child: new Text("Animated drawer panel")),
                    body: new CaptureBuildContextWidget(
                        capture: context => scaffoldContext = context,
                        child: new SizedBox()))));

        try
        {
            var size = new Size(400, 300);
            harness.Pump(size);
            Assert.True(scaffoldContext.HasValue);

            var state = Scaffold.Of(scaffoldContext!.Value);
            state.OpenDrawer();
            harness.Pump(size);

            var scrimAtStart = FindColoredBox(
                harness.RenderView,
                color => color.R == 0 && color.G == 0 && color.B == 0 && color.A < 0x8A);
            Assert.NotNull(scrimAtStart);

            var now = Scheduler.CurrentSeconds;
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.15));
            harness.Pump(size);

            var scrimMid = FindColoredBox(
                harness.RenderView,
                color => color.R == 0 && color.G == 0 && color.B == 0 && color.A > 0 && color.A < 0x8A);
            Assert.NotNull(scrimMid);

            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
            harness.Pump(size);

            var scrimFull = FindColoredBox(
                harness.RenderView,
                color => color.R == 0 && color.G == 0 && color.B == 0 && color.A == 0x8A);
            Assert.NotNull(scrimFull);
            Assert.True(state.IsDrawerOpen);
        }
        finally
        {
            binding.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void Scaffold_DragReleaseVelocity_OpensDrawer_BelowHalfThreshold()
    {
        Scheduler.ResetForTests();
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        BuildContext? scaffoldContext = null;
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with
                {
                    Platform = TargetPlatform.Android,
                },
                child: new Scaffold(
                    drawer: new Drawer(child: new Text("Velocity drawer panel")),
                    body: new CaptureBuildContextWidget(
                        capture: context => scaffoldContext = context,
                        child: new SizedBox()))));

        try
        {
            var size = new Size(400, 300);
            harness.Pump(size);
            Assert.True(scaffoldContext.HasValue);

            var start = new DateTime(2026, 4, 12, 8, 0, 0, DateTimeKind.Utc);
            DispatchPointerDown(binding, harness.RenderView, pointer: 7101, position: new Point(2, 120), timestampUtc: start);
            DispatchPointerMove(binding, harness.RenderView, pointer: 7101, position: new Point(80, 120), timestampUtc: start.AddMilliseconds(100));
            DispatchPointerUp(binding, harness.RenderView, pointer: 7101, position: new Point(260, 120), timestampUtc: start.AddMilliseconds(150));
            harness.Pump(size);

            var now = Scheduler.CurrentSeconds;
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
            harness.Pump(size);

            var state = Scaffold.Of(scaffoldContext!.Value);
            Assert.True(state.IsDrawerOpen);
            Assert.NotNull(FindParagraphByText(harness.RenderView, "Velocity drawer panel"));
        }
        finally
        {
            binding.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void Scaffold_DragReleaseVelocity_ClosesDrawer_AboveHalfThreshold()
    {
        Scheduler.ResetForTests();
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        BuildContext? scaffoldContext = null;
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with
                {
                    Platform = TargetPlatform.Android,
                },
                child: new Scaffold(
                    drawer: new Drawer(child: new Text("Velocity close drawer panel")),
                    body: new CaptureBuildContextWidget(
                        capture: context => scaffoldContext = context,
                        child: new SizedBox()))));

        try
        {
            var size = new Size(400, 300);
            harness.Pump(size);
            Assert.True(scaffoldContext.HasValue);

            var state = Scaffold.Of(scaffoldContext!.Value);
            state.OpenDrawer();
            harness.Pump(size);

            var now = Scheduler.CurrentSeconds;
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
            harness.Pump(size);
            Assert.True(state.IsDrawerOpen);

            var start = new DateTime(2026, 4, 12, 8, 1, 0, DateTimeKind.Utc);
            DispatchPointerDown(binding, harness.RenderView, pointer: 7102, position: new Point(240, 120), timestampUtc: start);
            DispatchPointerMove(binding, harness.RenderView, pointer: 7102, position: new Point(220, 120), timestampUtc: start.AddMilliseconds(100));
            DispatchPointerUp(binding, harness.RenderView, pointer: 7102, position: new Point(40, 120), timestampUtc: start.AddMilliseconds(150));
            harness.Pump(size);

            now = Scheduler.CurrentSeconds;
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
            harness.Pump(size);

            Assert.False(state.IsDrawerOpen);
            Assert.Null(FindParagraphByText(harness.RenderView, "Velocity close drawer panel"));
        }
        finally
        {
            binding.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void Scaffold_DragCancel_SettlesDrawerClosed_BelowHalfThreshold()
    {
        Scheduler.ResetForTests();
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        BuildContext? scaffoldContext = null;
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with
                {
                    Platform = TargetPlatform.Android,
                },
                child: new Scaffold(
                    drawer: new Drawer(child: new Text("Cancel close drawer panel")),
                    body: new CaptureBuildContextWidget(
                        capture: context => scaffoldContext = context,
                        child: new SizedBox()))));

        try
        {
            var size = new Size(400, 300);
            harness.Pump(size);
            Assert.True(scaffoldContext.HasValue);

            DispatchPointerDown(binding, harness.RenderView, pointer: 7103, position: new Point(2, 120));
            DispatchPointerMove(binding, harness.RenderView, pointer: 7103, position: new Point(80, 120));
            DispatchPointerCancel(binding, harness.RenderView, pointer: 7103, position: new Point(80, 120));
            harness.Pump(size);

            var now = Scheduler.CurrentSeconds;
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
            harness.Pump(size);

            var state = Scaffold.Of(scaffoldContext!.Value);
            Assert.False(state.IsDrawerOpen);
            Assert.Null(FindParagraphByText(harness.RenderView, "Cancel close drawer panel"));
        }
        finally
        {
            binding.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void Scaffold_DragCancel_SettlesDrawerOpen_AboveHalfThreshold()
    {
        Scheduler.ResetForTests();
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        BuildContext? scaffoldContext = null;
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with
                {
                    Platform = TargetPlatform.Android,
                },
                child: new Scaffold(
                    drawer: new Drawer(child: new Text("Cancel open drawer panel")),
                    body: new CaptureBuildContextWidget(
                        capture: context => scaffoldContext = context,
                        child: new SizedBox()))));

        try
        {
            var size = new Size(400, 300);
            harness.Pump(size);
            Assert.True(scaffoldContext.HasValue);

            DispatchPointerDown(binding, harness.RenderView, pointer: 7104, position: new Point(2, 120));
            DispatchPointerMove(binding, harness.RenderView, pointer: 7104, position: new Point(220, 120));
            DispatchPointerCancel(binding, harness.RenderView, pointer: 7104, position: new Point(220, 120));
            harness.Pump(size);

            var now = Scheduler.CurrentSeconds;
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
            harness.Pump(size);

            var state = Scaffold.Of(scaffoldContext!.Value);
            Assert.True(state.IsDrawerOpen);
            Assert.NotNull(FindParagraphByText(harness.RenderView, "Cancel open drawer panel"));
        }
        finally
        {
            binding.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void Scaffold_DragCancel_UsesThemedDrawerWidth_ForProgressThreshold()
    {
        Scheduler.ResetForTests();
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        BuildContext? scaffoldContext = null;
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with
                {
                    Platform = TargetPlatform.Android,
                    DrawerTheme = new DrawerThemeData(Width: 200),
                },
                child: new Scaffold(
                    drawer: new Drawer(child: new Text("Themed width drawer panel")),
                    body: new CaptureBuildContextWidget(
                        capture: context => scaffoldContext = context,
                        child: new SizedBox()))));

        try
        {
            var size = new Size(400, 300);
            harness.Pump(size);
            Assert.True(scaffoldContext.HasValue);

            DispatchPointerDown(binding, harness.RenderView, pointer: 7105, position: new Point(2, 120));
            DispatchPointerMove(binding, harness.RenderView, pointer: 7105, position: new Point(112, 120));
            DispatchPointerCancel(binding, harness.RenderView, pointer: 7105, position: new Point(112, 120));
            harness.Pump(size);

            var now = Scheduler.CurrentSeconds;
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
            harness.Pump(size);

            var state = Scaffold.Of(scaffoldContext!.Value);
            Assert.True(state.IsDrawerOpen);
            Assert.NotNull(FindParagraphByText(harness.RenderView, "Themed width drawer panel"));
        }
        finally
        {
            binding.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void Scaffold_EndDrawer_DragReleaseVelocity_OpensDrawer_BelowHalfThreshold()
    {
        Scheduler.ResetForTests();
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        BuildContext? scaffoldContext = null;
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with
                {
                    Platform = TargetPlatform.Android,
                },
                child: new Scaffold(
                    endDrawer: new Drawer(child: new Text("End velocity drawer panel")),
                    body: new CaptureBuildContextWidget(
                        capture: context => scaffoldContext = context,
                        child: new SizedBox()))));

        try
        {
            var size = new Size(400, 300);
            harness.Pump(size);
            Assert.True(scaffoldContext.HasValue);

            var start = new DateTime(2026, 4, 12, 8, 5, 0, DateTimeKind.Utc);
            DispatchPointerDown(binding, harness.RenderView, pointer: 7111, position: new Point(398, 120), timestampUtc: start);
            DispatchPointerMove(binding, harness.RenderView, pointer: 7111, position: new Point(320, 120), timestampUtc: start.AddMilliseconds(100));
            DispatchPointerUp(binding, harness.RenderView, pointer: 7111, position: new Point(120, 120), timestampUtc: start.AddMilliseconds(150));
            harness.Pump(size);

            var now = Scheduler.CurrentSeconds;
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
            harness.Pump(size);

            var state = Scaffold.Of(scaffoldContext!.Value);
            Assert.True(state.IsEndDrawerOpen);
            Assert.NotNull(FindParagraphByText(harness.RenderView, "End velocity drawer panel"));
        }
        finally
        {
            binding.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void Scaffold_EndDrawer_DragReleaseVelocity_ClosesDrawer_AboveHalfThreshold()
    {
        Scheduler.ResetForTests();
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        BuildContext? scaffoldContext = null;
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with
                {
                    Platform = TargetPlatform.Android,
                },
                child: new Scaffold(
                    endDrawer: new Drawer(child: new Text("End velocity close drawer panel")),
                    body: new CaptureBuildContextWidget(
                        capture: context => scaffoldContext = context,
                        child: new SizedBox()))));

        try
        {
            var size = new Size(400, 300);
            harness.Pump(size);
            Assert.True(scaffoldContext.HasValue);

            var state = Scaffold.Of(scaffoldContext!.Value);
            state.OpenEndDrawer();
            harness.Pump(size);

            var now = Scheduler.CurrentSeconds;
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
            harness.Pump(size);
            Assert.True(state.IsEndDrawerOpen);

            var start = new DateTime(2026, 4, 12, 8, 6, 0, DateTimeKind.Utc);
            DispatchPointerDown(binding, harness.RenderView, pointer: 7112, position: new Point(160, 120), timestampUtc: start);
            DispatchPointerMove(binding, harness.RenderView, pointer: 7112, position: new Point(180, 120), timestampUtc: start.AddMilliseconds(100));
            DispatchPointerUp(binding, harness.RenderView, pointer: 7112, position: new Point(360, 120), timestampUtc: start.AddMilliseconds(150));
            harness.Pump(size);

            now = Scheduler.CurrentSeconds;
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
            harness.Pump(size);

            Assert.False(state.IsEndDrawerOpen);
            Assert.Null(FindParagraphByText(harness.RenderView, "End velocity close drawer panel"));
        }
        finally
        {
            binding.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void Scaffold_EndDrawer_DragCancel_SettlesDrawerClosed_BelowHalfThreshold()
    {
        Scheduler.ResetForTests();
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        BuildContext? scaffoldContext = null;
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with
                {
                    Platform = TargetPlatform.Android,
                },
                child: new Scaffold(
                    endDrawer: new Drawer(child: new Text("End cancel close drawer panel")),
                    body: new CaptureBuildContextWidget(
                        capture: context => scaffoldContext = context,
                        child: new SizedBox()))));

        try
        {
            var size = new Size(400, 300);
            harness.Pump(size);
            Assert.True(scaffoldContext.HasValue);

            DispatchPointerDown(binding, harness.RenderView, pointer: 7113, position: new Point(398, 120));
            DispatchPointerMove(binding, harness.RenderView, pointer: 7113, position: new Point(320, 120));
            DispatchPointerCancel(binding, harness.RenderView, pointer: 7113, position: new Point(320, 120));
            harness.Pump(size);

            var now = Scheduler.CurrentSeconds;
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
            harness.Pump(size);

            var state = Scaffold.Of(scaffoldContext!.Value);
            Assert.False(state.IsEndDrawerOpen);
            Assert.Null(FindParagraphByText(harness.RenderView, "End cancel close drawer panel"));
        }
        finally
        {
            binding.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void Scaffold_EndDrawer_DragCancel_SettlesDrawerOpen_AboveHalfThreshold()
    {
        Scheduler.ResetForTests();
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        BuildContext? scaffoldContext = null;
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with
                {
                    Platform = TargetPlatform.Android,
                },
                child: new Scaffold(
                    endDrawer: new Drawer(child: new Text("End cancel open drawer panel")),
                    body: new CaptureBuildContextWidget(
                        capture: context => scaffoldContext = context,
                        child: new SizedBox()))));

        try
        {
            var size = new Size(400, 300);
            harness.Pump(size);
            Assert.True(scaffoldContext.HasValue);

            DispatchPointerDown(binding, harness.RenderView, pointer: 7114, position: new Point(398, 120));
            DispatchPointerMove(binding, harness.RenderView, pointer: 7114, position: new Point(150, 120));
            DispatchPointerCancel(binding, harness.RenderView, pointer: 7114, position: new Point(150, 120));
            harness.Pump(size);

            var now = Scheduler.CurrentSeconds;
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
            harness.Pump(size);

            var state = Scaffold.Of(scaffoldContext!.Value);
            Assert.True(state.IsEndDrawerOpen);
            Assert.NotNull(FindParagraphByText(harness.RenderView, "End cancel open drawer panel"));
        }
        finally
        {
            binding.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void Scaffold_OpenDrawer_UsesDrawerThemeScrimColor_WhenWidgetScrimColorIsNull()
    {
        Scheduler.ResetForTests();
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        BuildContext? scaffoldContext = null;
        var themedScrim = Color.FromArgb(0x99, 0x11, 0x22, 0x33);
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with
                {
                    Platform = TargetPlatform.Android,
                    DrawerTheme = new DrawerThemeData(ScrimColor: themedScrim),
                },
                child: new Scaffold(
                    drawer: new Drawer(child: new Text("Themed scrim drawer panel")),
                    body: new CaptureBuildContextWidget(
                        capture: context => scaffoldContext = context,
                        child: new SizedBox()))));

        try
        {
            var size = new Size(400, 300);
            harness.Pump(size);
            Assert.True(scaffoldContext.HasValue);

            var state = Scaffold.Of(scaffoldContext!.Value);
            state.OpenDrawer();
            harness.Pump(size);

            var now = Scheduler.CurrentSeconds;
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
            harness.Pump(size);

            var scrim = FindColoredBox(harness.RenderView, color => color == themedScrim);
            Assert.NotNull(scrim);
            Assert.True(state.IsDrawerOpen);
        }
        finally
        {
            binding.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void Scaffold_OpenDrawer_WidgetScrimColor_OverridesDrawerThemeScrimColor()
    {
        Scheduler.ResetForTests();
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        BuildContext? scaffoldContext = null;
        var themedScrim = Color.FromArgb(0x99, 0x11, 0x22, 0x33);
        var widgetScrim = Color.FromArgb(0x88, 0x44, 0x55, 0x66);
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with
                {
                    Platform = TargetPlatform.Android,
                    DrawerTheme = new DrawerThemeData(ScrimColor: themedScrim),
                },
                child: new Scaffold(
                    drawerScrimColor: widgetScrim,
                    drawer: new Drawer(child: new Text("Widget scrim drawer panel")),
                    body: new CaptureBuildContextWidget(
                        capture: context => scaffoldContext = context,
                        child: new SizedBox()))));

        try
        {
            var size = new Size(400, 300);
            harness.Pump(size);
            Assert.True(scaffoldContext.HasValue);

            var state = Scaffold.Of(scaffoldContext!.Value);
            state.OpenDrawer();
            harness.Pump(size);

            var now = Scheduler.CurrentSeconds;
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
            harness.Pump(size);

            var scrim = FindColoredBox(harness.RenderView, color => color == widgetScrim);
            Assert.NotNull(scrim);
            Assert.True(state.IsDrawerOpen);
        }
        finally
        {
            binding.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void Scaffold_NavigatorMaybePop_ClosesDrawerOnRootRoute_WithoutPoppingRoute()
    {
        var owner = new BuildOwner();
        BuildContext? scaffoldContext = null;
        NavigatorState? navigatorState = null;
        ModalRoute? rootRoute = null;

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new Navigator(
                    initialRoute: new BuilderPageRoute(
                        builder: context =>
                        {
                            navigatorState ??= Navigator.Of(context);
                            rootRoute ??= ModalRoute.Of(context);

                            return new Scaffold(
                                appBar: new AppBar(titleText: "Root"),
                                drawer: new Drawer(child: new Text("Root drawer panel")),
                                body: new CaptureBuildContextWidget(
                                    capture: captured => scaffoldContext = captured,
                                    child: new SizedBox(width: 24, height: 12)));
                        },
                        settings: new RouteSettings(Name: "/")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.True(scaffoldContext.HasValue);
        Assert.NotNull(navigatorState);
        Assert.NotNull(rootRoute);
        Assert.False(navigatorState!.CanPop);

        var scaffoldState = Scaffold.Of(scaffoldContext!.Value);
        scaffoldState.OpenDrawer();
        owner.FlushBuild();

        Assert.True(scaffoldState.IsDrawerOpen);
        Assert.True(rootRoute!.ImpliesAppBarDismissal);
        Assert.Same(rootRoute, navigatorState.CurrentRoute);

        Assert.True(Navigator.MaybePop(scaffoldContext.Value));
        owner.FlushBuild();

        Assert.False(scaffoldState.IsDrawerOpen);
        Assert.Same(rootRoute, navigatorState.CurrentRoute);
        Assert.False(navigatorState.CanPop);
    }

    [Fact]
    public void ThemeData_DefaultsUseMaterial3ToTrue()
    {
        Assert.True(ThemeData.Light.UseMaterial3);
    }

    [Fact]
    public void ThemeData_DefaultsBrightnessToLight()
    {
        Assert.Equal(Brightness.Light, ThemeData.Light.Brightness);
    }

    [Fact]
    public void Scaffold_WithAppBar_UsesThemeCanvasColorForAppBarBackground_WhenUseMaterial3IsEnabled()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            CanvasColor = Colors.DarkSlateBlue,
            PrimaryColor = Colors.Crimson
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new Scaffold(
                    appBar: new AppBar(titleText: "Demo"),
                    body: new SizedBox(width: 24, height: 12))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var scaffoldBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var contentColumn = Assert.IsType<RenderFlex>(scaffoldBackground.Child);
        var appBarBackground = Assert.IsType<RenderColoredBox>(contentColumn.FirstChild);

        Assert.Equal(Colors.DarkSlateBlue, appBarBackground.Color);
        Assert.NotNull(contentColumn.ChildAfter(appBarBackground));
    }

    [Fact]
    public void Scaffold_WithAppBar_UsesThemePrimaryColorForAppBarBackground_WhenUseMaterial3IsDisabled()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            CanvasColor = Colors.Goldenrod,
            PrimaryColor = Colors.DarkSlateBlue
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new Scaffold(
                    appBar: new AppBar(titleText: "Demo"),
                    body: new SizedBox(width: 24, height: 12))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var scaffoldBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var contentColumn = Assert.IsType<RenderFlex>(scaffoldBackground.Child);
        var appBarBackground = Assert.IsType<RenderColoredBox>(contentColumn.FirstChild);

        Assert.Equal(Colors.DarkSlateBlue, appBarBackground.Color);
        Assert.NotNull(contentColumn.ChildAfter(appBarBackground));
    }

    [Fact]
    public void Scaffold_WithAppBar_UsesThemeCanvasColorForAppBarBackground_WhenUseMaterial3IsDisabledAndBrightnessDark()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            Brightness = Brightness.Dark,
            CanvasColor = Colors.DarkSlateBlue,
            PrimaryColor = Colors.Crimson
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new Scaffold(
                    appBar: new AppBar(titleText: "Demo"),
                    body: new SizedBox(width: 24, height: 12))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var scaffoldBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var contentColumn = Assert.IsType<RenderFlex>(scaffoldBackground.Child);
        var appBarBackground = Assert.IsType<RenderColoredBox>(contentColumn.FirstChild);

        Assert.Equal(Colors.DarkSlateBlue, appBarBackground.Color);
        Assert.NotNull(contentColumn.ChildAfter(appBarBackground));
    }

    [Fact]
    public void AppBar_DefaultTitle_UsesThemeOnSurfaceColor_WhenUseMaterial3IsEnabled()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            CanvasColor = Colors.DarkSlateBlue,
            OnSurfaceColor = Colors.Bisque,
            OnPrimaryColor = Colors.Crimson
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(titleText: "Demo")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        Assert.Equal(Colors.DarkSlateBlue, appBarBackground.Color);

        var paragraph = FindDescendant<RenderParagraph>(appBarBackground);
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.Bisque, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void AppBar_DefaultTitle_UsesThemeOnPrimaryColor_WhenUseMaterial3IsDisabled()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            PrimaryColor = Colors.DarkSlateBlue,
            OnSurfaceColor = Colors.Crimson,
            OnPrimaryColor = Colors.Bisque
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(titleText: "Demo")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        Assert.Equal(Colors.DarkSlateBlue, appBarBackground.Color);

        var paragraph = FindDescendant<RenderParagraph>(appBarBackground);
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.Bisque, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void AppBar_DefaultTitle_UsesThemeOnSurfaceColor_WhenUseMaterial3IsDisabledAndBrightnessDark()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            Brightness = Brightness.Dark,
            CanvasColor = Colors.DarkSlateBlue,
            PrimaryColor = Colors.Crimson,
            OnSurfaceColor = Colors.Bisque,
            OnPrimaryColor = Colors.CadetBlue
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(titleText: "Demo")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        Assert.Equal(Colors.DarkSlateBlue, appBarBackground.Color);

        var paragraph = FindDescendant<RenderParagraph>(appBarBackground);
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.Bisque, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void AppBar_DefaultTitle_UsesSingleLineEllipsisDefaults()
    {
        var owner = new BuildOwner();
        const string title = "Very long default app bar title";
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new AppBar(titleText: title)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var paragraph = FindParagraphByText(appBarBackground, title);
        Assert.NotNull(paragraph);
        Assert.False(paragraph!.SoftWrap);
        Assert.Equal(1, paragraph.MaxLines);
        Assert.Equal(TextOverflow.Ellipsis, paragraph.Overflow);
    }

    [Fact]
    public void AppBar_DefaultTitle_EmptyString_IsRenderedAsText()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new AppBar(titleText: string.Empty)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var paragraph = FindParagraphByText(appBarBackground, string.Empty);
        Assert.NotNull(paragraph);
        Assert.False(paragraph!.SoftWrap);
        Assert.Equal(1, paragraph.MaxLines);
        Assert.Equal(TextOverflow.Ellipsis, paragraph.Overflow);
    }

    [Fact]
    public void AppBar_BackgroundColor_DefaultsFromThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.DarkSlateBlue,
            AppBarTheme = new AppBarThemeData(BackgroundColor: Colors.Crimson),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(titleText: "Demo")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        Assert.Equal(Colors.Crimson, appBarBackground.Color);
    }

    [Fact]
    public void AppBar_BackgroundColor_WidgetValue_OverridesThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(BackgroundColor: Colors.Crimson),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Demo",
                    backgroundColor: Colors.DarkOliveGreen)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        Assert.Equal(Colors.DarkOliveGreen, appBarBackground.Color);
    }

    [Fact]
    public void AppBar_ForegroundColor_DefaultsFromThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            OnPrimaryColor = Colors.Bisque,
            AppBarTheme = new AppBarThemeData(ForegroundColor: Colors.Goldenrod),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(titleText: "Demo")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var paragraph = FindParagraphByText(appBarBackground, "Demo");
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.Goldenrod, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void AppBar_ForegroundColor_WidgetValue_OverridesThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(ForegroundColor: Colors.Goldenrod),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Demo",
                    foregroundColor: Colors.CadetBlue)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var paragraph = FindParagraphByText(appBarBackground, "Demo");
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.CadetBlue, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void AppBar_SystemOverlayStyle_DefaultsFromThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var themedStyle = new SystemUiOverlayStyle(
            StatusBarColor: Colors.Crimson,
            NavigationBarColor: Colors.DarkGreen,
            StatusBarIconBrightness: SystemUiIconBrightness.Light,
            NavigationBarIconBrightness: SystemUiIconBrightness.Light);
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(SystemOverlayStyle: themedStyle),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(titleText: "Demo")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Equal(themedStyle, SystemChrome.CurrentSystemUiOverlayStyle);
    }

    [Fact]
    public void AppBar_SystemOverlayStyle_WidgetValue_OverridesThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var themedStyle = new SystemUiOverlayStyle(
            StatusBarColor: Colors.Crimson,
            NavigationBarColor: Colors.DarkGreen,
            StatusBarIconBrightness: SystemUiIconBrightness.Light,
            NavigationBarIconBrightness: SystemUiIconBrightness.Light);
        var widgetStyle = new SystemUiOverlayStyle(
            StatusBarColor: Colors.Bisque,
            NavigationBarColor: Colors.CadetBlue,
            StatusBarIconBrightness: SystemUiIconBrightness.Dark,
            NavigationBarIconBrightness: SystemUiIconBrightness.Dark);
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(SystemOverlayStyle: themedStyle),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Demo",
                    systemOverlayStyle: widgetStyle)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Equal(widgetStyle, SystemChrome.CurrentSystemUiOverlayStyle);
    }

    [Fact]
    public void AppBar_CenterTitleTrue_WrapsTitleInCenterAlign()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new AppBar(
                    titleText: "Centered",
                    centerTitle: true)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var align = FindDescendant<RenderAlign>(appBarBackground);
        Assert.NotNull(align);
        Assert.Equal(Alignment.Center, align!.Alignment);
    }

    [Fact]
    public void AppBar_CenterTitle_DefaultsFromThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            Platform = TargetPlatform.Android,
            AppBarTheme = new AppBarThemeData(CenterTitle: true),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(titleText: "Centered by theme")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var align = FindDescendant<RenderAlign>(appBarBackground);
        Assert.NotNull(align);
        Assert.Equal(Alignment.Center, align!.Alignment);
    }

    [Fact]
    public void AppBar_CenterTitle_ExplicitValue_OverridesThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            Platform = TargetPlatform.MacOS,
            AppBarTheme = new AppBarThemeData(CenterTitle: true),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Not centered",
                    centerTitle: false)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var align = FindDescendant<RenderAlign>(appBarBackground);
        Assert.Null(align);
    }

    [Fact]
    public void AppBar_CenterTitle_DefaultsFromPlatform_MacOS_WhenActionsCountLessThanTwo()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            Platform = TargetPlatform.MacOS,
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Centered by platform",
                    actions:
                    [
                        new SizedBox(width: 8, height: 8),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var align = FindDescendant<RenderAlign>(appBarBackground);
        Assert.NotNull(align);
        Assert.Equal(Alignment.Center, align!.Alignment);
    }

    [Fact]
    public void AppBar_CenterTitle_DefaultsFromPlatform_MacOS_WithTwoActions_IsNotCentered()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            Platform = TargetPlatform.MacOS,
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Not centered by platform",
                    actions:
                    [
                        new SizedBox(width: 8, height: 8),
                        new SizedBox(width: 8, height: 8),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var align = FindDescendant<RenderAlign>(appBarBackground);
        Assert.Null(align);
    }

    [Fact]
    public void AppBar_LeadingWidth_DefaultsFromThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(LeadingWidth: 80),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    leading: new SizedBox(width: 12, height: 12))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var leadingBox = FindConstrainedBox(appBarBackground, constraints =>
            Math.Abs(constraints.MinWidth - 80) < 0.001
            && Math.Abs(constraints.MaxWidth - 80) < 0.001);

        Assert.NotNull(leadingBox);
    }

    [Fact]
    public void AppBar_LeadingWidth_WidgetValue_OverridesThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(LeadingWidth: 80),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    leading: new SizedBox(width: 12, height: 12),
                    leadingWidth: 64)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var leadingBox = FindConstrainedBox(appBarBackground, constraints =>
            Math.Abs(constraints.MinWidth - 64) < 0.001
            && Math.Abs(constraints.MaxWidth - 64) < 0.001);

        Assert.NotNull(leadingBox);
    }

    [Fact]
    public void AppBar_LeadingSlot_IsConstrainedByLeadingWidthAndToolbarHeight()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new AppBar(
                    titleText: "Title",
                    leading: new SizedBox(width: 12, height: 12),
                    leadingWidth: 64,
                    toolbarHeight: 72)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var leadingBox = FindConstrainedBox(appBarBackground, constraints =>
            Math.Abs(constraints.MinWidth - 64) < 0.001
            && Math.Abs(constraints.MaxWidth - 64) < 0.001
            && Math.Abs(constraints.MinHeight - 72) < 0.001
            && Math.Abs(constraints.MaxHeight - 72) < 0.001);

        Assert.NotNull(leadingBox);
    }

    [Fact]
    public void AppBar_AutomaticallyImplyLeading_ShowsBackIcon_OnNonRootRoute()
    {
        var owner = new BuildOwner();
        BuildContext? rootContext = null;

        static Widget BuildBody() => new SizedBox(width: 24, height: 12);

        Route? BuildRoute(RouteSettings settings)
        {
            return settings.Name switch
            {
                "/" => new BuilderPageRoute(
                    builder: context =>
                    {
                        rootContext ??= context;
                        return new Scaffold(
                            appBar: new AppBar(titleText: "Root"),
                            body: BuildBody());
                    },
                    settings: settings),
                "/details" => new BuilderPageRoute(
                    builder: _ => new Scaffold(
                        appBar: new AppBar(titleText: "Details"),
                        body: BuildBody()),
                    settings: settings),
                _ => null,
            };
        }

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new Navigator(
                    onGenerateRoute: BuildRoute,
                    initialRouteName: "/")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.True(rootContext.HasValue);
        Navigator.PushNamed(rootContext!.Value, "/details");
        owner.FlushBuild();

        var arrowBackGlyph = char.ConvertFromUtf32(Icons.ArrowBack.CodePoint);
        var arrowBackParagraph = FindParagraphByText(root.ChildElement?.RenderObject, arrowBackGlyph);
        Assert.NotNull(arrowBackParagraph);
    }

    [Fact]
    public void AppBar_AutomaticallyImplyLeading_ShowsBackIcon_OnRootRouteWithLocalHistory()
    {
        var owner = new BuildOwner();
        NavigatorState? navigatorState = null;
        ModalRoute? rootRoute = null;

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new Navigator(
                    initialRoute: new BuilderPageRoute(
                        builder: context =>
                        {
                            navigatorState ??= Navigator.Of(context);
                            rootRoute ??= ModalRoute.Of(context);
                            return new Scaffold(
                                appBar: new AppBar(titleText: "Root"),
                                body: new SizedBox(width: 24, height: 12));
                        },
                        settings: new RouteSettings(Name: "/")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var arrowBackGlyph = char.ConvertFromUtf32(Icons.ArrowBack.CodePoint);
        var arrowBackBefore = FindParagraphByText(root.ChildElement?.RenderObject, arrowBackGlyph);
        Assert.Null(arrowBackBefore);

        rootRoute!.AddLocalHistoryEntry(new LocalHistoryEntry());
        navigatorState!.InvokeSetState(() => { });
        owner.FlushBuild();

        var arrowBackAfter = FindParagraphByText(root.ChildElement?.RenderObject, arrowBackGlyph);
        Assert.NotNull(arrowBackAfter);
    }

    [Fact]
    public void AppBar_AutomaticallyImplyLeading_UsesCloseIcon_OnFullscreenDialogRoute()
    {
        var owner = new BuildOwner();
        BuildContext? rootContext = null;

        static Widget BuildBody() => new SizedBox(width: 24, height: 12);

        Route? BuildRoute(RouteSettings settings)
        {
            return settings.Name switch
            {
                "/" => new BuilderPageRoute(
                    builder: context =>
                    {
                        rootContext ??= context;
                        return new Scaffold(
                            appBar: new AppBar(titleText: "Root"),
                            body: BuildBody());
                    },
                    settings: settings),
                "/dialog" => new BuilderPageRoute(
                    builder: _ => new Scaffold(
                        appBar: new AppBar(titleText: "Dialog"),
                        body: BuildBody()),
                    settings: settings,
                    fullscreenDialog: true),
                _ => null,
            };
        }

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new Navigator(
                    onGenerateRoute: BuildRoute,
                    initialRouteName: "/")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.True(rootContext.HasValue);
        Navigator.PushNamed(rootContext!.Value, "/dialog");
        owner.FlushBuild();

        var closeGlyph = char.ConvertFromUtf32(Icons.Close.CodePoint);
        var closeParagraph = FindParagraphByText(root.ChildElement?.RenderObject, closeGlyph);
        Assert.NotNull(closeParagraph);

        var arrowBackGlyph = char.ConvertFromUtf32(Icons.ArrowBack.CodePoint);
        var arrowBackParagraph = FindParagraphByText(root.ChildElement?.RenderObject, arrowBackGlyph);
        Assert.Null(arrowBackParagraph);
    }

    [Fact]
    public void AppBar_AutomaticallyImplyLeading_False_HidesBackIcon_OnNonRootRoute()
    {
        var owner = new BuildOwner();
        BuildContext? rootContext = null;

        static Widget BuildBody() => new SizedBox(width: 24, height: 12);

        Route? BuildRoute(RouteSettings settings)
        {
            return settings.Name switch
            {
                "/" => new BuilderPageRoute(
                    builder: context =>
                    {
                        rootContext ??= context;
                        return new Scaffold(
                            appBar: new AppBar(titleText: "Root"),
                            body: BuildBody());
                    },
                    settings: settings),
                "/details" => new BuilderPageRoute(
                    builder: _ => new Scaffold(
                        appBar: new AppBar(
                            titleText: "Details",
                            automaticallyImplyLeading: false),
                        body: BuildBody()),
                    settings: settings),
                _ => null,
            };
        }

        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new Navigator(
                    onGenerateRoute: BuildRoute,
                    initialRouteName: "/")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.True(rootContext.HasValue);
        Navigator.PushNamed(rootContext!.Value, "/details");
        owner.FlushBuild();

        var arrowBackGlyph = char.ConvertFromUtf32(Icons.ArrowBack.CodePoint);
        var arrowBackParagraph = FindParagraphByText(root.ChildElement?.RenderObject, arrowBackGlyph);
        Assert.Null(arrowBackParagraph);
    }

    [Fact]
    public void AppBar_ActionsPadding_DefaultsFromThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(ActionsPadding: new Thickness(13, 5, 19, 7)),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    actions:
                    [
                        new Text("Action"),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var actionsPadding = FindPadding(appBarBackground, padding =>
            Math.Abs(padding.Left - 13) < 0.001
            && Math.Abs(padding.Top - 5) < 0.001
            && Math.Abs(padding.Right - 19) < 0.001
            && Math.Abs(padding.Bottom - 7) < 0.001);

        Assert.NotNull(actionsPadding);
    }

    [Fact]
    public void AppBar_ActionsPadding_WidgetValue_OverridesThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(ActionsPadding: new Thickness(13, 5, 19, 7)),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    actionsPadding: new Thickness(4, 6, 8, 10),
                    actions:
                    [
                        new Text("Action"),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var actionsPadding = FindPadding(appBarBackground, padding =>
            Math.Abs(padding.Left - 4) < 0.001
            && Math.Abs(padding.Top - 6) < 0.001
            && Math.Abs(padding.Right - 8) < 0.001
            && Math.Abs(padding.Bottom - 10) < 0.001);

        Assert.NotNull(actionsPadding);
    }

    [Fact]
    public void AppBar_DefaultOuterPadding_IsZero()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new AppBar(titleText: "Title")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var outerPadding = FindPadding(appBarBackground, padding =>
            Math.Abs(padding.Left) < 0.001
            && Math.Abs(padding.Top) < 0.001
            && Math.Abs(padding.Right) < 0.001
            && Math.Abs(padding.Bottom) < 0.001);

        Assert.NotNull(outerPadding);
    }

    [Fact]
    public void AppBar_PrimaryTrue_AppliesMediaQueryTopPadding()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new MediaQuery(
                    data: new MediaQueryData(
                        Padding: new Thickness(0, 24, 0, 0),
                        ViewPadding: new Thickness(0, 24, 0, 0)),
                    child: new AppBar(titleText: "Title"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var safeAreaPadding = FindPadding(appBarBackground, padding =>
            Math.Abs(padding.Left) < 0.001
            && Math.Abs(padding.Top - 24) < 0.001
            && Math.Abs(padding.Right) < 0.001
            && Math.Abs(padding.Bottom) < 0.001);

        Assert.NotNull(safeAreaPadding);
    }

    [Fact]
    public void AppBar_PrimaryFalse_DoesNotApplyMediaQueryTopPadding()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new MediaQuery(
                    data: new MediaQueryData(
                        Padding: new Thickness(0, 24, 0, 0),
                        ViewPadding: new Thickness(0, 24, 0, 0)),
                    child: new AppBar(
                        titleText: "Title",
                        primary: false))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var safeAreaPadding = FindPadding(appBarBackground, padding =>
            Math.Abs(padding.Left) < 0.001
            && Math.Abs(padding.Top - 24) < 0.001
            && Math.Abs(padding.Right) < 0.001
            && Math.Abs(padding.Bottom) < 0.001);

        Assert.Null(safeAreaPadding);
    }

    [Fact]
    public void AppBar_ActionsRow_DoesNotApplyExtraSpacing()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new AppBar(
                    titleText: "Title",
                    actionsPadding: new Thickness(3, 4, 5, 6),
                    actions:
                    [
                        new Text("One"),
                        new Text("Two"),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var actionsPadding = FindPadding(appBarBackground, padding =>
            Math.Abs(padding.Left - 3) < 0.001
            && Math.Abs(padding.Top - 4) < 0.001
            && Math.Abs(padding.Right - 5) < 0.001
            && Math.Abs(padding.Bottom - 6) < 0.001);
        Assert.NotNull(actionsPadding);

        var actionsRow = FindDescendant<RenderFlex>(actionsPadding);
        Assert.NotNull(actionsRow);
        Assert.Equal(Axis.Horizontal, actionsRow!.Direction);
        Assert.Equal(MainAxisSize.Min, actionsRow.MainAxisSize);
        Assert.Equal(CrossAxisAlignment.Center, actionsRow.CrossAxisAlignment);
        Assert.Equal(0, actionsRow.Spacing);
    }

    [Fact]
    public void AppBar_ActionsRow_UsesStretchCrossAxisAlignment_WhenUseMaterial3IsDisabled()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false
        };
        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    actionsPadding: new Thickness(7, 8, 9, 10),
                    actions:
                    [
                        new Text("One"),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var actionsPadding = FindPadding(appBarBackground, padding =>
            Math.Abs(padding.Left - 7) < 0.001
            && Math.Abs(padding.Top - 8) < 0.001
            && Math.Abs(padding.Right - 9) < 0.001
            && Math.Abs(padding.Bottom - 10) < 0.001);
        Assert.NotNull(actionsPadding);

        var actionsRow = FindDescendant<RenderFlex>(actionsPadding);
        Assert.NotNull(actionsRow);
        Assert.Equal(MainAxisSize.Min, actionsRow!.MainAxisSize);
        Assert.Equal(CrossAxisAlignment.Stretch, actionsRow.CrossAxisAlignment);
        Assert.Equal(0, actionsRow.Spacing);
    }

    [Fact]
    public void AppBar_IconTheme_DefaultsFromThemeAppBarTheme_ForLeading()
    {
        IconThemeData? capturedTheme = null;
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(
                IconTheme: new IconThemeData(
                    Color: Colors.Crimson,
                    Size: 19)),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    leading: new CaptureIconThemeWidget(themeData => capturedTheme = themeData))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.Crimson, capturedTheme!.Color);
        Assert.Equal(19, capturedTheme.Size);
    }

    [Fact]
    public void AppBar_IconTheme_DefaultsToOnSurfaceAndSize24_ForLeading_WhenUseMaterial3IsEnabled()
    {
        IconThemeData? capturedTheme = null;
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            OnSurfaceColor = Colors.CadetBlue,
            OnPrimaryColor = Colors.Crimson
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    leading: new CaptureIconThemeWidget(themeData => capturedTheme = themeData))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.CadetBlue, capturedTheme!.Color);
        Assert.Equal(24, capturedTheme.Size);
    }

    [Fact]
    public void AppBar_IconTheme_DefaultsToOnPrimary_ForLeading_WhenUseMaterial3IsDisabled()
    {
        IconThemeData? capturedTheme = null;
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            OnSurfaceColor = Colors.Crimson,
            OnPrimaryColor = Colors.CadetBlue
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    leading: new CaptureIconThemeWidget(themeData => capturedTheme = themeData))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.CadetBlue, capturedTheme!.Color);
        Assert.Null(capturedTheme.Size);
    }

    [Fact]
    public void AppBar_IconTheme_WidgetValue_OverridesThemeAppBarTheme_ForLeading()
    {
        IconThemeData? capturedTheme = null;
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(
                IconTheme: new IconThemeData(
                    Color: Colors.Crimson,
                    Size: 19)),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    iconTheme: new IconThemeData(
                        Color: Colors.CadetBlue,
                        Size: 21),
                    leading: new CaptureIconThemeWidget(themeData => capturedTheme = themeData))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.CadetBlue, capturedTheme!.Color);
        Assert.Equal(21, capturedTheme.Size);
    }

    [Fact]
    public void AppBar_IconTheme_WithNullColor_FallsBackToForeground_ForLeading()
    {
        IconThemeData? capturedTheme = null;
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new AppBar(
                    titleText: "Title",
                    foregroundColor: Colors.DarkRed,
                    iconTheme: new IconThemeData(Size: 22),
                    leading: new CaptureIconThemeWidget(themeData => capturedTheme = themeData))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.DarkRed, capturedTheme!.Color);
        Assert.Equal(22, capturedTheme.Size);
    }

    [Fact]
    public void AppBar_ActionsIconTheme_DefaultsFromThemeAppBarTheme_ForActions()
    {
        IconThemeData? capturedTheme = null;
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(
                ActionsIconTheme: new IconThemeData(
                    Color: Colors.Goldenrod,
                    Size: 17)),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    actions:
                    [
                        new CaptureIconThemeWidget(themeData => capturedTheme = themeData),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.Goldenrod, capturedTheme!.Color);
        Assert.Equal(17, capturedTheme.Size);
    }

    [Fact]
    public void AppBar_ActionsIconTheme_DefaultsToOnSurfaceVariant_WhenUseMaterial3IsEnabled()
    {
        IconThemeData? capturedTheme = null;
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            OnSurfaceColor = Colors.CadetBlue,
            OnSurfaceVariantColor = Colors.Goldenrod,
            OnPrimaryColor = Colors.Crimson
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    actions:
                    [
                        new CaptureIconThemeWidget(themeData => capturedTheme = themeData),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.Goldenrod, capturedTheme!.Color);
        Assert.Equal(24, capturedTheme.Size);
    }

    [Fact]
    public void AppBar_ActionsIconTheme_DefaultsToOnPrimary_WhenUseMaterial3IsDisabled()
    {
        IconThemeData? capturedTheme = null;
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            OnSurfaceVariantColor = Colors.Goldenrod,
            OnPrimaryColor = Colors.CadetBlue
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    actions:
                    [
                        new CaptureIconThemeWidget(themeData => capturedTheme = themeData),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.CadetBlue, capturedTheme!.Color);
    }

    [Fact]
    public void AppBar_ActionsIconTheme_WidgetValue_OverridesThemeAppBarTheme_ForActions()
    {
        IconThemeData? capturedTheme = null;
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(
                ActionsIconTheme: new IconThemeData(
                    Color: Colors.Goldenrod,
                    Size: 17)),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    actionsIconTheme: new IconThemeData(
                        Color: Colors.LimeGreen,
                        Size: 23),
                    actions:
                    [
                        new CaptureIconThemeWidget(themeData => capturedTheme = themeData),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.LimeGreen, capturedTheme!.Color);
        Assert.Equal(23, capturedTheme.Size);
    }

    [Fact]
    public void AppBar_ActionsIconTheme_FallsBackToAppBarIconTheme_WhenActionsThemeMissing()
    {
        IconThemeData? capturedTheme = null;
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(
                IconTheme: new IconThemeData(
                    Color: Colors.DarkCyan,
                    Size: 14)),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    actions:
                    [
                        new CaptureIconThemeWidget(themeData => capturedTheme = themeData),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.DarkCyan, capturedTheme!.Color);
        Assert.Equal(14, capturedTheme.Size);
    }

    [Fact]
    public void AppBar_ActionsIconTheme_FallsBackToWidgetIconTheme_WhenActionsThemeMissing()
    {
        IconThemeData? capturedTheme = null;
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new AppBar(
                    titleText: "Title",
                    iconTheme: new IconThemeData(
                        Color: Colors.DarkOrange,
                        Size: 11),
                    actions:
                    [
                        new CaptureIconThemeWidget(themeData => capturedTheme = themeData),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.DarkOrange, capturedTheme!.Color);
        Assert.Equal(11, capturedTheme.Size);
    }

    [Fact]
    public void AppBar_ActionsIconTheme_WithNullColor_FallsBackToForeground_ForActions()
    {
        IconThemeData? capturedTheme = null;
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new AppBar(
                    titleText: "Title",
                    foregroundColor: Colors.LimeGreen,
                    actionsIconTheme: new IconThemeData(Size: 24),
                    actions:
                    [
                        new CaptureIconThemeWidget(themeData => capturedTheme = themeData),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.LimeGreen, capturedTheme!.Color);
        Assert.Equal(24, capturedTheme.Size);
    }

    [Fact]
    public void AppBar_Actions_ReceiveToolbarTextStyle_AndActionsIconTheme()
    {
        ActionContextSnapshot? snapshot = null;
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new AppBar(
                    titleText: "Title",
                    toolbarTextStyle: new TextStyle(
                        FontSize: 18,
                        Color: Colors.CadetBlue,
                        FontWeight: FontWeight.Bold),
                    actionsIconTheme: new IconThemeData(
                        Color: Colors.Goldenrod,
                        Size: 20),
                    actions:
                    [
                        new CaptureActionContextWidget(data => snapshot = data),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(snapshot);
        Assert.NotNull(snapshot!.TextStyle.Color);
        Assert.Equal(18, snapshot.TextStyle.FontSize);
        Assert.Equal(Colors.CadetBlue, snapshot.TextStyle.Color!.Value);
        Assert.Equal(FontWeight.Bold, snapshot.TextStyle.FontWeight);
        Assert.Equal(Colors.Goldenrod, snapshot.IconThemeData.Color);
        Assert.Equal(20, snapshot.IconThemeData.Size);
    }

    [Fact]
    public void AppBar_TitleSpacing_AppliesHorizontalPaddingToTitle()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new AppBar(
                    title: new SizedBox(width: 40, height: 12),
                    titleSpacing: 24)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var titlePadding = FindPadding(appBarBackground, padding =>
            Math.Abs(padding.Left - 24) < 0.001
            && Math.Abs(padding.Right - 24) < 0.001
            && Math.Abs(padding.Top) < 0.001
            && Math.Abs(padding.Bottom) < 0.001);

        Assert.NotNull(titlePadding);
    }

    [Fact]
    public void AppBar_TitleSpacing_DefaultsFromThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(TitleSpacing: 22),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(title: new SizedBox(width: 40, height: 12))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var titlePadding = FindPadding(appBarBackground, padding =>
            Math.Abs(padding.Left - 22) < 0.001
            && Math.Abs(padding.Right - 22) < 0.001
            && Math.Abs(padding.Top) < 0.001
            && Math.Abs(padding.Bottom) < 0.001);

        Assert.NotNull(titlePadding);
    }

    [Fact]
    public void AppBar_TitleSpacing_WidgetValue_OverridesThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(TitleSpacing: 22),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    title: new SizedBox(width: 40, height: 12),
                    titleSpacing: 30)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var titlePadding = FindPadding(appBarBackground, padding =>
            Math.Abs(padding.Left - 30) < 0.001
            && Math.Abs(padding.Right - 30) < 0.001
            && Math.Abs(padding.Top) < 0.001
            && Math.Abs(padding.Bottom) < 0.001);

        Assert.NotNull(titlePadding);
    }

    [Fact]
    public void AppBar_ToolbarHeight_DefaultsFromThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(ToolbarHeight: 72),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(titleText: "Title")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var toolbarBox = FindConstrainedBox(appBarBackground, constraints =>
            Math.Abs(constraints.MinHeight - 72) < 0.001
            && Math.Abs(constraints.MaxHeight - 72) < 0.001);

        Assert.NotNull(toolbarBox);
    }

    [Fact]
    public void AppBar_ToolbarHeight_WidgetValue_OverridesThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(ToolbarHeight: 72),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    toolbarHeight: 64)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var toolbarBox = FindConstrainedBox(appBarBackground, constraints =>
            Math.Abs(constraints.MinHeight - 64) < 0.001
            && Math.Abs(constraints.MaxHeight - 64) < 0.001);

        Assert.NotNull(toolbarBox);
    }

    [Fact]
    public void AppBar_ToolbarHeight_DefaultsTo56_WhenUseMaterial3IsEnabled()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new AppBar(titleText: "Title")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var toolbarBox = FindConstrainedBox(appBarBackground, constraints =>
            Math.Abs(constraints.MinHeight - 56) < 0.001
            && Math.Abs(constraints.MaxHeight - 56) < 0.001);

        Assert.NotNull(toolbarBox);
    }

    [Fact]
    public void AppBar_ToolbarHeight_DefaultsTo56_WhenUseMaterial3IsDisabled()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false
        };
        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(titleText: "Title")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var toolbarBox = FindConstrainedBox(appBarBackground, constraints =>
            Math.Abs(constraints.MinHeight - 56) < 0.001
            && Math.Abs(constraints.MaxHeight - 56) < 0.001);

        Assert.NotNull(toolbarBox);
    }

    [Fact]
    public void AppBar_TitleTextStyle_DefaultsFromTextThemeTitleLarge_WithThemeForegroundFallback()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            OnSurfaceColor = Colors.Bisque,
            OnPrimaryColor = Colors.Crimson,
            TextTheme = new MaterialTextTheme(
                titleLarge: new TextStyle(
                    FontSize: 29,
                    Color: Colors.Crimson,
                    FontWeight: FontWeight.Bold)),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(titleText: "Title")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var titleParagraph = FindParagraphByText(appBarBackground, "Title");
        Assert.NotNull(titleParagraph);
        Assert.Equal(29, titleParagraph!.FontSize);
        Assert.Equal(FontWeight.Bold, titleParagraph.FontWeight);
        Assert.Equal(Colors.Bisque, Assert.IsType<SolidColorBrush>(titleParagraph.Foreground).Color);
    }

    [Fact]
    public void AppBar_TitleTextStyle_DefaultsFromThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(
                TitleTextStyle: new TextStyle(
                    FontSize: 26,
                    Color: Colors.Crimson,
                    FontWeight: FontWeight.Bold)),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(titleText: "Title")));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var titleParagraph = FindParagraphByText(appBarBackground, "Title");
        Assert.NotNull(titleParagraph);
        Assert.Equal(26, titleParagraph!.FontSize);
        Assert.Equal(FontWeight.Bold, titleParagraph.FontWeight);
        Assert.Equal(Colors.Crimson, Assert.IsType<SolidColorBrush>(titleParagraph.Foreground).Color);
    }

    [Fact]
    public void AppBar_TitleTextStyle_WidgetValue_OverridesThemeAppBarTheme()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(
                TitleTextStyle: new TextStyle(
                    FontSize: 26,
                    Color: Colors.Crimson,
                    FontWeight: FontWeight.Bold)),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    titleTextStyle: new TextStyle(
                        FontSize: 18,
                        Color: Colors.LimeGreen,
                        FontWeight: FontWeight.Normal))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var titleParagraph = FindParagraphByText(appBarBackground, "Title");
        Assert.NotNull(titleParagraph);
        Assert.Equal(18, titleParagraph!.FontSize);
        Assert.Equal(FontWeight.Normal, titleParagraph.FontWeight);
        Assert.Equal(Colors.LimeGreen, Assert.IsType<SolidColorBrush>(titleParagraph.Foreground).Color);
    }

    [Fact]
    public void AppBar_ToolbarTextStyle_DefaultsFromThemeAppBarTheme_ForActionsText()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(
                ToolbarTextStyle: new TextStyle(
                    FontSize: 17,
                    Color: Colors.Goldenrod,
                    FontWeight: FontWeight.Bold)),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    actions:
                    [
                        new Text("Action"),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var actionParagraph = FindParagraphByText(appBarBackground, "Action");
        Assert.NotNull(actionParagraph);
        Assert.Equal(17, actionParagraph!.FontSize);
        Assert.Equal(FontWeight.Bold, actionParagraph.FontWeight);
        Assert.Equal(Colors.Goldenrod, Assert.IsType<SolidColorBrush>(actionParagraph.Foreground).Color);
    }

    [Fact]
    public void AppBar_ToolbarTextStyle_WidgetValue_OverridesThemeAppBarTheme_ForActionsText()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(
                ToolbarTextStyle: new TextStyle(
                    FontSize: 17,
                    Color: Colors.Goldenrod,
                    FontWeight: FontWeight.Bold)),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    titleText: "Title",
                    toolbarTextStyle: new TextStyle(
                        FontSize: 15,
                        Color: Colors.CadetBlue,
                        FontWeight: FontWeight.Normal),
                    actions:
                    [
                        new Text("Action"),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var appBarBackground = RequireRenderObject<RenderColoredBox>(root.ChildElement);
        var actionParagraph = FindParagraphByText(appBarBackground, "Action");
        Assert.NotNull(actionParagraph);
        Assert.Equal(15, actionParagraph!.FontSize);
        Assert.Equal(FontWeight.Normal, actionParagraph.FontWeight);
        Assert.Equal(Colors.CadetBlue, Assert.IsType<SolidColorBrush>(actionParagraph.Foreground).Color);
    }

    [Fact]
    public void AppBar_NegativeTitleSpacing_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AppBar(
            titleText: "Invalid",
            titleSpacing: -1));
    }

    [Fact]
    public void AppBar_NonPositiveThemeToolbarHeight_Throws()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    AppBarTheme = new AppBarThemeData(ToolbarHeight: 0),
                },
                child: new AppBar(titleText: "Invalid")));

        root.Attach(owner);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            root.Mount(parent: null, newSlot: null);
            owner.FlushBuild();
        });
    }

    [Fact]
    public void AppBar_NonPositiveThemeLeadingWidth_Throws()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    AppBarTheme = new AppBarThemeData(LeadingWidth: 0),
                },
                child: new AppBar(
                    titleText: "Invalid",
                    leading: new SizedBox(width: 8, height: 8))));

        root.Attach(owner);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            root.Mount(parent: null, newSlot: null);
            owner.FlushBuild();
        });
    }

    private static T RequireRenderObject<T>(Element? element) where T : RenderObject
    {
        Assert.NotNull(element);
        Assert.NotNull(element!.RenderObject);
        return Assert.IsType<T>(element.RenderObject);
    }

    private static T? FindDescendant<T>(RenderObject? root) where T : RenderObject
    {
        if (root is null)
        {
            return null;
        }

        if (root is T match)
        {
            return match;
        }

        T? result = null;
        root.VisitChildren(child =>
        {
            if (result is not null)
            {
                return;
            }

            result = FindDescendant<T>(child);
        });

        return result;
    }

    private static RenderParagraph? FindParagraphByText(RenderObject? root, string text)
    {
        if (root is null)
        {
            return null;
        }

        if (root is RenderParagraph paragraph && string.Equals(paragraph.Text, text, StringComparison.Ordinal))
        {
            return paragraph;
        }

        RenderParagraph? result = null;
        root.VisitChildren(child =>
        {
            if (result is not null)
            {
                return;
            }

            result = FindParagraphByText(child, text);
        });

        return result;
    }

    private static RenderPadding? FindPadding(RenderObject? root, Predicate<Thickness> predicate)
    {
        if (root is null)
        {
            return null;
        }

        if (root is RenderPadding padding && predicate(padding.Padding))
        {
            return padding;
        }

        RenderPadding? result = null;
        root.VisitChildren(child =>
        {
            if (result is not null)
            {
                return;
            }

            result = FindPadding(child, predicate);
        });

        return result;
    }

    private static RenderConstrainedBox? FindConstrainedBox(RenderObject? root, Predicate<BoxConstraints> predicate)
    {
        if (root is null)
        {
            return null;
        }

        if (root is RenderConstrainedBox constrainedBox && predicate(constrainedBox.AdditionalConstraints))
        {
            return constrainedBox;
        }

        RenderConstrainedBox? result = null;
        root.VisitChildren(child =>
        {
            if (result is not null)
            {
                return;
            }

            result = FindConstrainedBox(child, predicate);
        });

        return result;
    }

    private static RenderColoredBox? FindColoredBox(RenderObject? root, Predicate<Color> predicate)
    {
        if (root is null)
        {
            return null;
        }

        if (root is RenderColoredBox coloredBox && predicate(coloredBox.Color))
        {
            return coloredBox;
        }

        RenderColoredBox? result = null;
        root.VisitChildren(child =>
        {
            if (result is not null)
            {
                return;
            }

            result = FindColoredBox(child, predicate);
        });

        return result;
    }

    private static void DispatchPointerDown(
        GestureBinding binding,
        RenderView renderView,
        int pointer,
        Point position,
        DateTime? timestampUtc = null)
    {
        binding.HandlePointerEvent(
            renderView,
            new PointerDownEvent(
                pointer: pointer,
                kind: PointerDeviceKind.Mouse,
                position: position,
                buttons: PointerButtons.Primary,
                timestampUtc: timestampUtc ?? DateTime.UtcNow));
    }

    private static void DispatchPointerMove(
        GestureBinding binding,
        RenderView renderView,
        int pointer,
        Point position,
        DateTime? timestampUtc = null)
    {
        binding.HandlePointerEvent(
            renderView,
            new PointerMoveEvent(
                pointer: pointer,
                kind: PointerDeviceKind.Mouse,
                position: position,
                buttons: PointerButtons.Primary,
                down: true,
                timestampUtc: timestampUtc ?? DateTime.UtcNow));
    }

    private static void DispatchPointerUp(
        GestureBinding binding,
        RenderView renderView,
        int pointer,
        Point position,
        DateTime? timestampUtc = null)
    {
        binding.HandlePointerEvent(
            renderView,
            new PointerUpEvent(
                pointer: pointer,
                kind: PointerDeviceKind.Mouse,
                position: position,
                buttons: PointerButtons.None,
                timestampUtc: timestampUtc ?? DateTime.UtcNow));
    }

    private static void DispatchPointerCancel(
        GestureBinding binding,
        RenderView renderView,
        int pointer,
        Point position,
        DateTime? timestampUtc = null)
    {
        binding.HandlePointerEvent(
            renderView,
            new PointerCancelEvent(
                pointer: pointer,
                kind: PointerDeviceKind.Mouse,
                position: position,
                buttons: PointerButtons.None,
                timestampUtc: timestampUtc ?? DateTime.UtcNow));
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

        public void Dispose()
        {
            _rootElement.Unmount();
        }

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;

            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget)
            {
                _renderView = renderView;
            }

            public override RenderObject? RenderObject => _child?.RenderObject;

            internal override Element? RenderObjectAttachingChild => _child;

            protected override void OnMount()
            {
                base.OnMount();
                Rebuild();
            }

            internal override void Rebuild()
            {
                Dirty = false;
                _child = UpdateChild(_child, Widget, Slot);
            }

            internal override void Update(Widget newWidget)
            {
                base.Update(newWidget);
                Rebuild();
            }

            internal override void ForgetChild(Element child)
            {
                if (ReferenceEquals(_child, child))
                {
                    _child = null;
                }
            }

            internal override void VisitChildren(Action<Element> visitor)
            {
                if (_child != null)
                {
                    visitor(_child);
                }
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot)
            {
                if (slot != null)
                {
                    throw new InvalidOperationException("HarnessRootElement expects null slot.");
                }

                if (child is not RenderBox renderBox)
                {
                    throw new InvalidOperationException("HarnessRootElement can host only RenderBox.");
                }

                _renderView.Child = renderBox;
            }

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
                if (!Equals(oldSlot, newSlot))
                {
                    throw new InvalidOperationException("HarnessRootElement does not support non-null slot moves.");
                }
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (slot != null)
                {
                    throw new InvalidOperationException("HarnessRootElement expects null slot.");
                }

                if (ReferenceEquals(_renderView.Child, child))
                {
                    _renderView.Child = null;
                }
            }

            internal override void Unmount()
            {
                if (_child != null)
                {
                    UnmountChild(_child);
                    _child = null;
                }

                base.Unmount();
            }
        }
    }

    private sealed class CaptureBuildContextWidget : StatelessWidget
    {
        private readonly Action<BuildContext> _capture;
        private readonly Widget _child;

        public CaptureBuildContextWidget(Action<BuildContext> capture, Widget? child = null)
        {
            _capture = capture ?? throw new ArgumentNullException(nameof(capture));
            _child = child ?? new SizedBox();
        }

        public override Widget Build(BuildContext context)
        {
            _capture(context);
            return _child;
        }
    }

    private sealed class CaptureIconThemeWidget : StatelessWidget
    {
        private readonly Action<IconThemeData> _capture;

        public CaptureIconThemeWidget(Action<IconThemeData> capture)
        {
            _capture = capture;
        }

        public override Widget Build(BuildContext context)
        {
            _capture(IconTheme.Of(context));
            return new SizedBox(width: 8, height: 8);
        }
    }

    private sealed record ActionContextSnapshot(TextStyle TextStyle, IconThemeData IconThemeData);

    private sealed class CaptureActionContextWidget : StatelessWidget
    {
        private readonly Action<ActionContextSnapshot> _capture;

        public CaptureActionContextWidget(Action<ActionContextSnapshot> capture)
        {
            _capture = capture;
        }

        public override Widget Build(BuildContext context)
        {
            _capture(new ActionContextSnapshot(
                TextStyle: DefaultTextStyle.Of(context),
                IconThemeData: IconTheme.Of(context)));
            return new SizedBox(width: 8, height: 8);
        }
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

        public Element? ChildElement => _child;

        protected override void OnMount()
        {
            base.OnMount();
            Rebuild();
        }

        internal override void Rebuild()
        {
            Dirty = false;
            _child = UpdateChild(_child, Widget, Slot);
        }

        internal override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild();
        }

        internal override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
            }
        }

        internal override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        internal override void Unmount()
        {
            if (_child != null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot != null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
            if (!Equals(oldSlot, newSlot))
            {
                throw new InvalidOperationException("TestRootElement does not support slot moves.");
            }
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot != null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
        }
    }
}
