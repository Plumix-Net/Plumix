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
using MaterialWidget = global::Plumix.Material.Material;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialScaffoldTests
{
    public MaterialScaffoldTests()
    {
        NavigatorBackButtonDispatcher.ResetForTests();
        SystemChrome.ResetSystemUiOverlayStyleForTests();
    }

    /// <summary>
    /// Runs the drawer's 246ms settle animation to completion. Source <c>DrawerController.open</c>/
    /// <c>close</c> fling their controller, so the panel only enters or leaves the tree as the animation
    /// advances.
    /// </summary>
    private static void SettleDrawerAnimation(BuildOwner owner)
    {
        owner.FlushBuild();
        AnimationPump.Advance(0.5);
        owner.FlushBuild();
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

        // Dart's `ScaffoldState.build` roots the scaffold in `Material(color: ...)`, so the background is
        // the first Material descendant's color (`scaffold_test.dart`, "Scaffold background color
        // defaults to ColorScheme.surface").
        MaterialWidget background = FindWidgets<MaterialWidget>(root.ChildElement)[0];
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

        MaterialWidget background = FindWidgets<MaterialWidget>(root.ChildElement)[0];
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
    public void Drawer_ExpandsToAvailableHeightLikeFlutterConstrainedBox()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new Drawer(child: new Text("Expanded drawer"))));

        harness.Pump(new Size(420, 300));

        var constrained = FindConstrainedBox(
            harness.RenderView,
            constraints =>
                Math.Abs(constraints.MinWidth - 304) < 0.001
                && Math.Abs(constraints.MaxWidth - 304) < 0.001);
        Assert.NotNull(constrained);
        Assert.Equal(300, constrained!.Size.Height, 3);
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
        Assert.NotNull(decorated.Decoration.BoxShadows);

        var shadows = decorated.Decoration.BoxShadows!;
        Assert.True(shadows.Count > 0);
        for (int i = 0; i < shadows.Count; i++)
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
        Assert.NotNull(decorated.Decoration.BoxShadows);

        var shadows = decorated.Decoration.BoxShadows!;
        Assert.True(shadows.Count > 0);
        for (int i = 0; i < shadows.Count; i++)
        {
            var shadow = shadows[i];
            Assert.Equal(Colors.DarkGreen.R, shadow.Color.R);
            Assert.Equal(Colors.DarkGreen.G, shadow.Color.G);
            Assert.Equal(Colors.DarkGreen.B, shadow.Color.B);
            Assert.True(shadow.Color.A > 0);
        }
    }

    [Fact]
    public void Drawer_ZeroThemedWidth_IsAcceptedLikeFlutterBoxConstraints()
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

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var constrained = FindConstrainedBox(
            root.ChildElement?.RenderObject,
            constraints => constraints.MinWidth == 0.0 && constraints.MaxWidth == 0.0);
        Assert.NotNull(constrained);
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

        Assert.Equal("elevation", exception.ParamName);
    }

    [Fact]
    public void AppBar_AutomaticallyImplyLeading_ShowsMenuIcon_WhenScaffoldHasDrawer()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new Scaffold(
                    appBar: new AppBar(title: new Text("Root")),
                    drawer: new Drawer(
                        child: new SizedBox(width: 80, height: 40)),
                    body: new SizedBox(width: 24, height: 12))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        string menuGlyph = char.ConvertFromUtf32(Icons.Menu.CodePoint);
        var menuParagraph = FindParagraphByText(root.ChildElement?.RenderObject, menuGlyph);
        Assert.NotNull(menuParagraph);

        string arrowBackGlyph = char.ConvertFromUtf32(
            ThemeData.Light.Platform is TargetPlatform.IOS or TargetPlatform.MacOS
                ? Icons.ArrowBackIosNewRounded.CodePoint
                : Icons.ArrowBack.CodePoint);
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
                        title: new Text("Root"),
                        automaticallyImplyLeading: false),
                    drawer: new Drawer(
                        child: new SizedBox(width: 80, height: 40)),
                    body: new SizedBox(width: 24, height: 12))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        string menuGlyph = char.ConvertFromUtf32(Icons.Menu.CodePoint);
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
                    appBar: new AppBar(title: new Text("Root")),
                    endDrawer: new Drawer(
                        child: new SizedBox(width: 80, height: 40)),
                    body: new SizedBox(width: 24, height: 12))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        string menuGlyph = char.ConvertFromUtf32(Icons.Menu.CodePoint);
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
                        title: new Text("Root"),
                        automaticallyImplyActions: false),
                    endDrawer: new Drawer(
                        child: new SizedBox(width: 80, height: 40)),
                    body: new SizedBox(width: 24, height: 12))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        string menuGlyph = char.ConvertFromUtf32(Icons.Menu.CodePoint);
        var menuParagraph = FindParagraphByText(root.ChildElement?.RenderObject, menuGlyph);
        Assert.Null(menuParagraph);
    }


    // ---------------------------------------------------------------- state restoration

    [Fact]
    public void Scaffold_RestoresBothDrawerFlagsThroughItsRestorationId()
    {
        // Dart: `drawer_test.dart` "Scaffold.drawer state restoration test", "Scaffold.endDrawer state
        // restoration test" and "Both drawer and endDrawer state restoration test". `ScaffoldState` mixes
        // in `RestorationMixin` and registers `_drawerOpened`/`_endDrawerOpened` as `drawer_open` and
        // `end_drawer_open`; the restored values feed `DrawerController.isDrawerOpen` back.
        var manager = new MockRestorationManager();
        var rawData = RawRestorationData.Build();
        Dictionary<object, object?>? snapshot;

        {
            var owner = new BuildOwner();
            BuildContext? scaffoldContext = null;
            var root = new TestRootElement(new UnmanagedRestorationScope(
                bucket: RestorationBucket.Root(manager, rawData),
                child: RestorableScaffold(context => scaffoldContext = context, restorationId: "scaffold")));
            root.Attach(owner);
            root.Mount(parent: null, newSlot: null);
            owner.FlushBuild();

            var state = Scaffold.Of(scaffoldContext!.Value);
            Assert.False(state.IsDrawerOpen);
            Assert.False(state.IsEndDrawerOpen);

            state.OpenEndDrawer();
            SettleDrawerAnimation(owner);
            Assert.True(state.IsEndDrawerOpen);

            manager.DoSerialization();
            snapshot = RestorationSerialization.CopyRestorationData(rawData);
            root.Unmount();
        }

        Dictionary<object, object?> values = RawRestorationData.Values(
            RawRestorationData.Child(snapshot!, "scaffold")!)!;
        Assert.Equal(false, values["drawer_open"]);
        Assert.Equal(true, values["end_drawer_open"]);

        var restartOwner = new BuildOwner();
        BuildContext? restoredContext = null;
        var restarted = new TestRootElement(new UnmanagedRestorationScope(
            bucket: RestorationBucket.Root(manager, snapshot),
            child: RestorableScaffold(context => restoredContext = context, restorationId: "scaffold")));
        restarted.Attach(restartOwner);
        restarted.Mount(parent: null, newSlot: null);
        restartOwner.FlushBuild();

        var restored = Scaffold.Of(restoredContext!.Value);
        Assert.False(restored.IsDrawerOpen);
        Assert.True(restored.IsEndDrawerOpen);
        Assert.NotNull(FindParagraphByText(restarted.ChildElement?.RenderObject, "End drawer panel"));
        restarted.Unmount();
    }

    [Fact]
    public void Scaffold_WithoutARestorationId_DoesNotPersistItsDrawerFlags()
    {
        // Dart: `drawer_test.dart` "Scaffold.drawer - null restorationId" / "Scaffold.endDrawer - null
        // restorationId": with no id the state object claims no bucket, so a restart loses the open drawer.
        var manager = new MockRestorationManager();
        var rawData = RawRestorationData.Build();
        Dictionary<object, object?>? snapshot;

        {
            var owner = new BuildOwner();
            BuildContext? scaffoldContext = null;
            var root = new TestRootElement(new UnmanagedRestorationScope(
                bucket: RestorationBucket.Root(manager, rawData),
                child: RestorableScaffold(context => scaffoldContext = context, restorationId: null)));
            root.Attach(owner);
            root.Mount(parent: null, newSlot: null);
            owner.FlushBuild();

            Scaffold.Of(scaffoldContext!.Value).OpenDrawer();
            SettleDrawerAnimation(owner);
            Assert.True(Scaffold.Of(scaffoldContext!.Value).IsDrawerOpen);

            manager.DoSerialization();
            snapshot = RestorationSerialization.CopyRestorationData(rawData);
            root.Unmount();
        }

        Assert.Null(RawRestorationData.Child(snapshot!, "scaffold"));

        var restartOwner = new BuildOwner();
        BuildContext? restoredContext = null;
        var restarted = new TestRootElement(new UnmanagedRestorationScope(
            bucket: RestorationBucket.Root(manager, snapshot),
            child: RestorableScaffold(context => restoredContext = context, restorationId: null)));
        restarted.Attach(restartOwner);
        restarted.Mount(parent: null, newSlot: null);
        restartOwner.FlushBuild();

        Assert.False(Scaffold.Of(restoredContext!.Value).IsDrawerOpen);
        Assert.Null(FindParagraphByText(restarted.ChildElement?.RenderObject, "Drawer panel"));
        restarted.Unmount();
    }

    private static Widget RestorableScaffold(Action<BuildContext> capture, string? restorationId) => new Theme(
        data: ThemeData.Light,
        child: new Scaffold(
            drawer: new Drawer(child: new Text("Drawer panel")),
            endDrawer: new Drawer(child: new Text("End drawer panel")),
            restorationId: restorationId,
            body: new CaptureBuildContextWidget(
                capture: capture,
                child: new SizedBox(width: 24, height: 12))));

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
        SettleDrawerAnimation(owner);

        Assert.True(state.IsDrawerOpen);
        Assert.NotNull(FindParagraphByText(root.ChildElement?.RenderObject, "Drawer panel"));

        state.CloseDrawer();
        SettleDrawerAnimation(owner);

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
        SettleDrawerAnimation(owner);

        Assert.True(state.IsEndDrawerOpen);
        Assert.NotNull(FindParagraphByText(root.ChildElement?.RenderObject, "End drawer panel"));

        state.CloseEndDrawer();
        SettleDrawerAnimation(owner);

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
    public void Scaffold_EdgeDrag_OpensStartDrawer_InRtl()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        BuildContext? scaffoldContext = null;
        using var harness = new WidgetRenderHarness(
            new Directionality(
                textDirection: TextDirection.Rtl,
                child: new Theme(
                    data: ThemeData.Light with
                    {
                        Platform = TargetPlatform.Android,
                    },
                    child: new Scaffold(
                        drawer: new Drawer(child: new Text("RTL start drawer panel")),
                        body: new CaptureBuildContextWidget(
                            capture: context => scaffoldContext = context,
                            child: new SizedBox())))));

        try
        {
            harness.Pump(new Size(400, 300));
            Assert.True(scaffoldContext.HasValue);

            DispatchPointerDown(binding, harness.RenderView, pointer: 7006, position: new Point(398, 120));
            DispatchPointerMove(binding, harness.RenderView, pointer: 7006, position: new Point(180, 120));
            DispatchPointerUp(binding, harness.RenderView, pointer: 7006, position: new Point(180, 120));
            harness.Pump(new Size(400, 300));

            var state = Scaffold.Of(scaffoldContext!.Value);
            Assert.True(state.IsDrawerOpen);
            Assert.NotNull(FindParagraphByText(harness.RenderView, "RTL start drawer panel"));
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
    public void Scaffold_EdgeDrag_OpensEndDrawer_InRtl()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        BuildContext? scaffoldContext = null;
        using var harness = new WidgetRenderHarness(
            new Directionality(
                textDirection: TextDirection.Rtl,
                child: new Theme(
                    data: ThemeData.Light with
                    {
                        Platform = TargetPlatform.Android,
                    },
                    child: new Scaffold(
                        endDrawer: new Drawer(child: new Text("RTL end drawer panel")),
                        body: new CaptureBuildContextWidget(
                            capture: context => scaffoldContext = context,
                            child: new SizedBox())))));

        try
        {
            harness.Pump(new Size(400, 300));
            Assert.True(scaffoldContext.HasValue);

            DispatchPointerDown(binding, harness.RenderView, pointer: 7007, position: new Point(2, 120));
            DispatchPointerMove(binding, harness.RenderView, pointer: 7007, position: new Point(220, 120));
            DispatchPointerUp(binding, harness.RenderView, pointer: 7007, position: new Point(220, 120));
            harness.Pump(new Size(400, 300));

            var state = Scaffold.Of(scaffoldContext!.Value);
            Assert.True(state.IsEndDrawerOpen);
            Assert.NotNull(FindParagraphByText(harness.RenderView, "RTL end drawer panel"));
        }
        finally
        {
            binding.ResetForTests();
        }
    }

    [Fact]
    public void Scaffold_EdgeDrag_DoesNotOpenStartDrawer_WhenDrawerOpenDragGestureDisabled()
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
                    drawerEnableOpenDragGesture: false,
                    drawer: new Drawer(child: new Text("Disabled start drawer panel")),
                    body: new CaptureBuildContextWidget(
                        capture: context => scaffoldContext = context,
                        child: new SizedBox()))));

        try
        {
            harness.Pump(new Size(400, 300));
            Assert.True(scaffoldContext.HasValue);

            DispatchPointerDown(binding, harness.RenderView, pointer: 7010, position: new Point(2, 120));
            DispatchPointerMove(binding, harness.RenderView, pointer: 7010, position: new Point(220, 120));
            DispatchPointerUp(binding, harness.RenderView, pointer: 7010, position: new Point(220, 120));
            harness.Pump(new Size(400, 300));

            var state = Scaffold.Of(scaffoldContext!.Value);
            Assert.False(state.IsDrawerOpen);
            Assert.Null(FindParagraphByText(harness.RenderView, "Disabled start drawer panel"));
        }
        finally
        {
            binding.ResetForTests();
        }
    }

    [Fact]
    public void Scaffold_EdgeDrag_DoesNotOpenEndDrawer_WhenEndDrawerOpenDragGestureDisabled()
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
                    endDrawerEnableOpenDragGesture: false,
                    endDrawer: new Drawer(child: new Text("Disabled end drawer panel")),
                    body: new CaptureBuildContextWidget(
                        capture: context => scaffoldContext = context,
                        child: new SizedBox()))));

        try
        {
            harness.Pump(new Size(400, 300));
            Assert.True(scaffoldContext.HasValue);

            DispatchPointerDown(binding, harness.RenderView, pointer: 7011, position: new Point(398, 120));
            DispatchPointerMove(binding, harness.RenderView, pointer: 7011, position: new Point(180, 120));
            DispatchPointerUp(binding, harness.RenderView, pointer: 7011, position: new Point(180, 120));
            harness.Pump(new Size(400, 300));

            var state = Scaffold.Of(scaffoldContext!.Value);
            Assert.False(state.IsEndDrawerOpen);
            Assert.Null(FindParagraphByText(harness.RenderView, "Disabled end drawer panel"));
        }
        finally
        {
            binding.ResetForTests();
        }
    }

    [Fact]
    public void Scaffold_EdgeDrag_DoesNotOpenDrawer_OnDesktopPlatform()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        BuildContext? scaffoldContext = null;
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with
                {
                    Platform = TargetPlatform.Windows,
                },
                child: new Scaffold(
                    drawer: new Drawer(child: new Text("Desktop drawer panel")),
                    body: new CaptureBuildContextWidget(
                        capture: context => scaffoldContext = context,
                        child: new SizedBox()))));

        try
        {
            harness.Pump(new Size(400, 300));
            Assert.True(scaffoldContext.HasValue);

            DispatchPointerDown(binding, harness.RenderView, pointer: 7012, position: new Point(2, 120));
            DispatchPointerMove(binding, harness.RenderView, pointer: 7012, position: new Point(220, 120));
            DispatchPointerUp(binding, harness.RenderView, pointer: 7012, position: new Point(220, 120));
            harness.Pump(new Size(400, 300));

            var state = Scaffold.Of(scaffoldContext!.Value);
            Assert.False(state.IsDrawerOpen);
            Assert.Null(FindParagraphByText(harness.RenderView, "Desktop drawer panel"));
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
    public void Scaffold_EdgeDrag_UsesMediaPaddingForStartDrawerActivationWidth_InRtl()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        BuildContext? scaffoldContext = null;
        using var harness = new WidgetRenderHarness(
            new Directionality(
                textDirection: TextDirection.Rtl,
                child: new Theme(
                    data: ThemeData.Light with
                    {
                        Platform = TargetPlatform.Android,
                    },
                    child: new MediaQuery(
                        data: new MediaQueryData(
                            Padding: new Thickness(0, 0, 30, 0)),
                        child: new Scaffold(
                            drawer: new Drawer(child: new Text("RTL padded edge start drawer panel")),
                            body: new CaptureBuildContextWidget(
                                capture: context => scaffoldContext = context,
                                child: new SizedBox()))))));

        try
        {
            harness.Pump(new Size(400, 300));
            Assert.True(scaffoldContext.HasValue);

            DispatchPointerDown(binding, harness.RenderView, pointer: 7008, position: new Point(360, 120));
            DispatchPointerMove(binding, harness.RenderView, pointer: 7008, position: new Point(180, 120));
            DispatchPointerUp(binding, harness.RenderView, pointer: 7008, position: new Point(180, 120));
            harness.Pump(new Size(400, 300));

            var state = Scaffold.Of(scaffoldContext!.Value);
            Assert.True(state.IsDrawerOpen);
            Assert.NotNull(FindParagraphByText(harness.RenderView, "RTL padded edge start drawer panel"));
        }
        finally
        {
            binding.ResetForTests();
        }
    }

    [Fact]
    public void Scaffold_EdgeDrag_UsesMediaPaddingForEndDrawerActivationWidth_InRtl()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();

        BuildContext? scaffoldContext = null;
        using var harness = new WidgetRenderHarness(
            new Directionality(
                textDirection: TextDirection.Rtl,
                child: new Theme(
                    data: ThemeData.Light with
                    {
                        Platform = TargetPlatform.Android,
                    },
                    child: new MediaQuery(
                        data: new MediaQueryData(
                            Padding: new Thickness(30, 0, 0, 0)),
                        child: new Scaffold(
                            endDrawer: new Drawer(child: new Text("RTL padded edge end drawer panel")),
                            body: new CaptureBuildContextWidget(
                                capture: context => scaffoldContext = context,
                                child: new SizedBox()))))));

        try
        {
            harness.Pump(new Size(400, 300));
            Assert.True(scaffoldContext.HasValue);

            DispatchPointerDown(binding, harness.RenderView, pointer: 7009, position: new Point(40, 120));
            DispatchPointerMove(binding, harness.RenderView, pointer: 7009, position: new Point(220, 120));
            DispatchPointerUp(binding, harness.RenderView, pointer: 7009, position: new Point(220, 120));
            harness.Pump(new Size(400, 300));

            var state = Scaffold.Of(scaffoldContext!.Value);
            Assert.True(state.IsEndDrawerOpen);
            Assert.NotNull(FindParagraphByText(harness.RenderView, "RTL padded edge end drawer panel"));
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

            // The controller is still dismissed on the frame `open()` was called, so the closed drawer -
            // and no scrim - is what the scaffold builds first.
            Assert.Null(FindColoredBox(harness.RenderView, IsBlackScrim));

            double now = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.05));
            harness.Pump(size);

            RenderColoredBox? scrimAtStart = FindColoredBox(harness.RenderView, IsBlackScrim);
            Assert.NotNull(scrimAtStart);
            byte alphaAtStart = scrimAtStart!.Color.A;
            Assert.True(alphaAtStart < 0x8A);

            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.15));
            harness.Pump(size);

            RenderColoredBox? scrimMid = FindColoredBox(harness.RenderView, IsBlackScrim);
            Assert.NotNull(scrimMid);
            Assert.True(scrimMid!.Color.A > alphaAtStart);

            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
            harness.Pump(size);

            RenderColoredBox? scrimFull = FindColoredBox(harness.RenderView, IsBlackScrim);
            Assert.NotNull(scrimFull);
            Assert.Equal(0x8A, scrimFull!.Color.A);
            Assert.True(state.IsDrawerOpen);
        }
        finally
        {
            binding.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void Scaffold_ScrimTap_ClosesDrawer_WhenBarrierDismissible()
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
                    drawer: new Drawer(child: new Text("Scrim dismissible drawer panel")),
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

            double now = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
            harness.Pump(size);
            Assert.True(state.IsDrawerOpen);

            DispatchPointerDown(binding, harness.RenderView, pointer: 7115, position: new Point(360, 120));
            DispatchPointerUp(binding, harness.RenderView, pointer: 7115, position: new Point(360, 120));
            harness.Pump(size);

            now = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
            harness.Pump(size);

            Assert.False(state.IsDrawerOpen);
            Assert.Null(FindParagraphByText(harness.RenderView, "Scrim dismissible drawer panel"));
        }
        finally
        {
            binding.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void Scaffold_ScrimTap_DoesNotCloseDrawer_WhenBarrierIsNotDismissible()
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
                    drawerBarrierDismissible: false,
                    drawer: new Drawer(child: new Text("Scrim locked drawer panel")),
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

            double now = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
            harness.Pump(size);
            Assert.True(state.IsDrawerOpen);

            DispatchPointerDown(binding, harness.RenderView, pointer: 7116, position: new Point(360, 120));
            DispatchPointerUp(binding, harness.RenderView, pointer: 7116, position: new Point(360, 120));
            harness.Pump(size);

            now = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
            harness.Pump(size);

            Assert.True(state.IsDrawerOpen);
            Assert.NotNull(FindParagraphByText(harness.RenderView, "Scrim locked drawer panel"));
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
            DispatchPointerMove(
                binding, harness.RenderView, 7101, new Point(40, 120), start.AddMilliseconds(30));
            DispatchPointerMove(
                binding, harness.RenderView, 7101, new Point(80, 120), start.AddMilliseconds(60));
            DispatchPointerUp(
                binding, harness.RenderView, 7101, new Point(260, 120), start.AddMilliseconds(90));
            harness.Pump(size);

            double now = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
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

            double now = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
            harness.Pump(size);
            Assert.True(state.IsDrawerOpen);

            var start = new DateTime(2026, 4, 12, 8, 1, 0, DateTimeKind.Utc);
            DispatchPointerDown(binding, harness.RenderView, pointer: 7102, position: new Point(240, 120), timestampUtc: start);
            DispatchPointerMove(
                binding, harness.RenderView, 7102, new Point(220, 120), start.AddMilliseconds(30));
            DispatchPointerMove(
                binding, harness.RenderView, 7102, new Point(180, 120), start.AddMilliseconds(60));
            DispatchPointerUp(
                binding, harness.RenderView, 7102, new Point(40, 120), start.AddMilliseconds(90));
            harness.Pump(size);

            now = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
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

            double now = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
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

            double now = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
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
            DispatchPointerMove(binding, harness.RenderView, pointer: 7105, position: new Point(122, 120));
            DispatchPointerCancel(binding, harness.RenderView, pointer: 7105, position: new Point(122, 120));
            harness.Pump(size);

            double now = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
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
            DispatchPointerMove(
                binding, harness.RenderView, 7111, new Point(360, 120), start.AddMilliseconds(30));
            DispatchPointerMove(
                binding, harness.RenderView, 7111, new Point(320, 120), start.AddMilliseconds(60));
            DispatchPointerUp(
                binding, harness.RenderView, 7111, new Point(120, 120), start.AddMilliseconds(90));
            harness.Pump(size);

            double now = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
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

            double now = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
            harness.Pump(size);
            Assert.True(state.IsEndDrawerOpen);

            var start = new DateTime(2026, 4, 12, 8, 6, 0, DateTimeKind.Utc);
            DispatchPointerDown(binding, harness.RenderView, pointer: 7112, position: new Point(160, 120), timestampUtc: start);
            DispatchPointerMove(
                binding, harness.RenderView, 7112, new Point(180, 120), start.AddMilliseconds(30));
            DispatchPointerMove(
                binding, harness.RenderView, 7112, new Point(220, 120), start.AddMilliseconds(60));
            DispatchPointerUp(
                binding, harness.RenderView, 7112, new Point(360, 120), start.AddMilliseconds(90));
            harness.Pump(size);

            now = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
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

            double now = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
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

            double now = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
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
    public void Scaffold_AlternatingDrawerDrags_StartThenEnd_KeepSingleDrawerVisible()
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
                    drawer: new Drawer(child: new Text("Alternating start drawer panel")),
                    endDrawer: new Drawer(child: new Text("Alternating end drawer panel")),
                    body: new CaptureBuildContextWidget(
                        capture: context => scaffoldContext = context,
                        child: new SizedBox()))));

        try
        {
            var size = new Size(400, 300);
            harness.Pump(size);
            Assert.True(scaffoldContext.HasValue);

            var state = Scaffold.Of(scaffoldContext!.Value);

            var startOpenAt = new DateTime(2026, 5, 3, 9, 0, 0, DateTimeKind.Utc);
            DispatchPointerDown(binding, harness.RenderView, pointer: 7121, position: new Point(2, 120), timestampUtc: startOpenAt);
            DispatchPointerMove(
                binding, harness.RenderView, 7121, new Point(40, 120), startOpenAt.AddMilliseconds(30));
            DispatchPointerMove(
                binding, harness.RenderView, 7121, new Point(80, 120), startOpenAt.AddMilliseconds(60));
            DispatchPointerUp(
                binding, harness.RenderView, 7121, new Point(260, 120), startOpenAt.AddMilliseconds(90));
            harness.Pump(size);

            double now = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
            harness.Pump(size);

            Assert.True(state.IsDrawerOpen);
            Assert.False(state.IsEndDrawerOpen);
            Assert.NotNull(FindParagraphByText(harness.RenderView, "Alternating start drawer panel"));
            Assert.Null(FindParagraphByText(harness.RenderView, "Alternating end drawer panel"));

            var startCloseAt = new DateTime(2026, 5, 3, 9, 1, 0, DateTimeKind.Utc);
            DispatchPointerDown(binding, harness.RenderView, pointer: 7122, position: new Point(240, 120), timestampUtc: startCloseAt);
            DispatchPointerMove(
                binding, harness.RenderView, 7122, new Point(220, 120), startCloseAt.AddMilliseconds(30));
            DispatchPointerMove(
                binding, harness.RenderView, 7122, new Point(180, 120), startCloseAt.AddMilliseconds(60));
            DispatchPointerUp(
                binding, harness.RenderView, 7122, new Point(40, 120), startCloseAt.AddMilliseconds(90));
            harness.Pump(size);

            now = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
            harness.Pump(size);

            Assert.False(state.IsDrawerOpen);
            Assert.False(state.IsEndDrawerOpen);
            Assert.Null(FindParagraphByText(harness.RenderView, "Alternating start drawer panel"));

            var endOpenAt = new DateTime(2026, 5, 3, 9, 2, 0, DateTimeKind.Utc);
            DispatchPointerDown(binding, harness.RenderView, pointer: 7123, position: new Point(398, 120), timestampUtc: endOpenAt);
            DispatchPointerMove(
                binding, harness.RenderView, 7123, new Point(360, 120), endOpenAt.AddMilliseconds(30));
            DispatchPointerMove(
                binding, harness.RenderView, 7123, new Point(320, 120), endOpenAt.AddMilliseconds(60));
            DispatchPointerUp(
                binding, harness.RenderView, 7123, new Point(120, 120), endOpenAt.AddMilliseconds(90));
            harness.Pump(size);

            now = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
            harness.Pump(size);

            Assert.False(state.IsDrawerOpen);
            Assert.True(state.IsEndDrawerOpen);
            Assert.Null(FindParagraphByText(harness.RenderView, "Alternating start drawer panel"));
            Assert.NotNull(FindParagraphByText(harness.RenderView, "Alternating end drawer panel"));
        }
        finally
        {
            binding.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void Scaffold_AlternatingDrawerDrags_EndThenStart_KeepSingleDrawerVisible()
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
                    drawer: new Drawer(child: new Text("Alternating second start drawer panel")),
                    endDrawer: new Drawer(child: new Text("Alternating first end drawer panel")),
                    body: new CaptureBuildContextWidget(
                        capture: context => scaffoldContext = context,
                        child: new SizedBox()))));

        try
        {
            var size = new Size(400, 300);
            harness.Pump(size);
            Assert.True(scaffoldContext.HasValue);

            var state = Scaffold.Of(scaffoldContext!.Value);

            var endOpenAt = new DateTime(2026, 5, 3, 9, 3, 0, DateTimeKind.Utc);
            DispatchPointerDown(binding, harness.RenderView, pointer: 7124, position: new Point(398, 120), timestampUtc: endOpenAt);
            DispatchPointerMove(
                binding, harness.RenderView, 7124, new Point(360, 120), endOpenAt.AddMilliseconds(30));
            DispatchPointerMove(
                binding, harness.RenderView, 7124, new Point(320, 120), endOpenAt.AddMilliseconds(60));
            DispatchPointerUp(
                binding, harness.RenderView, 7124, new Point(120, 120), endOpenAt.AddMilliseconds(90));
            harness.Pump(size);

            double now = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
            harness.Pump(size);

            Assert.False(state.IsDrawerOpen);
            Assert.True(state.IsEndDrawerOpen);
            Assert.NotNull(FindParagraphByText(harness.RenderView, "Alternating first end drawer panel"));
            Assert.Null(FindParagraphByText(harness.RenderView, "Alternating second start drawer panel"));

            var endCloseAt = new DateTime(2026, 5, 3, 9, 4, 0, DateTimeKind.Utc);
            DispatchPointerDown(binding, harness.RenderView, pointer: 7125, position: new Point(160, 120), timestampUtc: endCloseAt);
            DispatchPointerMove(
                binding, harness.RenderView, 7125, new Point(180, 120), endCloseAt.AddMilliseconds(30));
            DispatchPointerMove(
                binding, harness.RenderView, 7125, new Point(220, 120), endCloseAt.AddMilliseconds(60));
            DispatchPointerUp(
                binding, harness.RenderView, 7125, new Point(360, 120), endCloseAt.AddMilliseconds(90));
            harness.Pump(size);

            now = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
            harness.Pump(size);

            Assert.False(state.IsDrawerOpen);
            Assert.False(state.IsEndDrawerOpen);
            Assert.Null(FindParagraphByText(harness.RenderView, "Alternating first end drawer panel"));

            var startOpenAt = new DateTime(2026, 5, 3, 9, 5, 0, DateTimeKind.Utc);
            DispatchPointerDown(binding, harness.RenderView, pointer: 7126, position: new Point(2, 120), timestampUtc: startOpenAt);
            DispatchPointerMove(
                binding, harness.RenderView, 7126, new Point(40, 120), startOpenAt.AddMilliseconds(30));
            DispatchPointerMove(
                binding, harness.RenderView, 7126, new Point(80, 120), startOpenAt.AddMilliseconds(60));
            DispatchPointerUp(
                binding, harness.RenderView, 7126, new Point(260, 120), startOpenAt.AddMilliseconds(90));
            harness.Pump(size);

            now = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
            harness.Pump(size);

            Assert.True(state.IsDrawerOpen);
            Assert.False(state.IsEndDrawerOpen);
            Assert.NotNull(FindParagraphByText(harness.RenderView, "Alternating second start drawer panel"));
            Assert.Null(FindParagraphByText(harness.RenderView, "Alternating first end drawer panel"));
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

            double now = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
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

            double now = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
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
                                appBar: new AppBar(title: new Text("Root")),
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

        // Source `DrawerController` adds its local history entry with `impliesAppBarDismissal: false`, so an
        // open drawer keeps the app bar's drawer button instead of turning it into a back button.
        Assert.False(rootRoute!.ImpliesAppBarDismissal);
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
            // Pinned: on iOS/macOS the scaffold also installs a status-bar slot.
            Platform = TargetPlatform.Android,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(surface: Colors.DarkSlateBlue),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new Scaffold(
                    appBar: new AppBar(title: new Text("Demo")),
                    body: new SizedBox(width: 24, height: 12))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RenderObject scaffoldRoot = RequireRenderObject(root.ChildElement);
        var scaffoldLayout = FindDescendant<RenderCustomMultiChildLayoutBox>(scaffoldRoot);
        Assert.NotNull(scaffoldLayout);
        Assert.Contains(
            FindWidgets<MaterialWidget>(root.ChildElement),
            material => material.Color == Colors.DarkSlateBlue);

        // Body, app bar, and the always-present floating action button slot.
        Assert.Equal(3, scaffoldLayout.ChildCount);
    }

    [Fact]
    public void Scaffold_WithAppBar_UsesThemePrimaryColorForAppBarBackground_WhenUseMaterial3IsDisabled()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            // Pinned: on iOS/macOS the scaffold also installs a status-bar slot.
            Platform = TargetPlatform.Android,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(primary: Colors.DarkSlateBlue),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new Scaffold(
                    appBar: new AppBar(title: new Text("Demo")),
                    body: new SizedBox(width: 24, height: 12))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RenderObject scaffoldRoot = RequireRenderObject(root.ChildElement);
        var scaffoldLayout = FindDescendant<RenderCustomMultiChildLayoutBox>(scaffoldRoot);
        Assert.NotNull(scaffoldLayout);
        Assert.Contains(
            FindWidgets<MaterialWidget>(root.ChildElement),
            material => material.Color == Colors.DarkSlateBlue);

        // Body, app bar, and the always-present floating action button slot.
        Assert.Equal(3, scaffoldLayout.ChildCount);
    }

    [Fact]
    public void Scaffold_WithAppBar_UsesThemeCanvasColorForAppBarBackground_WhenUseMaterial3IsDisabledAndBrightnessDark()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            Brightness = Brightness.Dark,
            // Pinned: on iOS/macOS the scaffold also installs a status-bar slot.
            Platform = TargetPlatform.Android,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(
                brightness: Brightness.Dark,
                surface: Colors.DarkSlateBlue),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new Scaffold(
                    appBar: new AppBar(title: new Text("Demo")),
                    body: new SizedBox(width: 24, height: 12))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RenderObject scaffoldRoot = RequireRenderObject(root.ChildElement);
        var scaffoldLayout = FindDescendant<RenderCustomMultiChildLayoutBox>(scaffoldRoot);
        Assert.NotNull(scaffoldLayout);
        Assert.Contains(
            FindWidgets<MaterialWidget>(root.ChildElement),
            material => material.Color == Colors.DarkSlateBlue);

        // Body, app bar, and the always-present floating action button slot.
        Assert.Equal(3, scaffoldLayout.ChildCount);
    }

    [Fact]
    public void AppBar_DefaultTitle_UsesThemeOnSurfaceColor_WhenUseMaterial3IsEnabled()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(
                surface: Colors.DarkSlateBlue,
                onSurface: Colors.Bisque),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(title: new Text("Demo"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RenderObject appBarBackground = RequireRenderObject(root.ChildElement);
        Assert.Contains(
            FindWidgets<MaterialWidget>(root.ChildElement),
            material => material.Color == Colors.DarkSlateBlue);

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
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(
                primary: Colors.DarkSlateBlue,
                onPrimary: Colors.Bisque),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(title: new Text("Demo"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RenderObject appBarBackground = RequireRenderObject(root.ChildElement);
        Assert.Contains(
            FindWidgets<MaterialWidget>(root.ChildElement),
            material => material.Color == Colors.DarkSlateBlue);

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
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(
                brightness: Brightness.Dark,
                surface: Colors.DarkSlateBlue,
                onSurface: Colors.Bisque),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(title: new Text("Demo"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RenderObject appBarBackground = RequireRenderObject(root.ChildElement);
        Assert.Contains(
            FindWidgets<MaterialWidget>(root.ChildElement),
            material => material.Color == Colors.DarkSlateBlue);

        var paragraph = FindDescendant<RenderParagraph>(appBarBackground);
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.Bisque, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void AppBar_Title_InheritsSingleLineEllipsisFromTheDefaultTextStyle()
    {
        // Dart puts `softWrap: false, overflow: TextOverflow.ellipsis` on the title's DefaultTextStyle
        // and leaves `maxLines` unset.
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new AppBar(title: new Text(string.Empty))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RenderObject appBarBackground = RequireRenderObject(root.ChildElement);
        var paragraph = FindParagraphByText(appBarBackground, string.Empty);
        Assert.NotNull(paragraph);
        Assert.False(paragraph!.SoftWrap);
        Assert.Null(paragraph.MaxLines);
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
                child: new AppBar(title: new Text("Demo"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RenderObject appBarBackground = RequireRenderObject(root.ChildElement);
        Assert.Contains(
            FindWidgets<MaterialWidget>(root.ChildElement),
            material => material.Color == Colors.Crimson);
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
                    title: new Text("Demo"),
                    backgroundColor: Colors.DarkOliveGreen)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RenderObject appBarBackground = RequireRenderObject(root.ChildElement);
        Assert.Contains(
            FindWidgets<MaterialWidget>(root.ChildElement),
            material => material.Color == Colors.DarkOliveGreen);
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
                child: new AppBar(title: new Text("Demo"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RenderObject appBarBackground = RequireRenderObject(root.ChildElement);
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
                    title: new Text("Demo"),
                    foregroundColor: Colors.CadetBlue)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RenderObject appBarBackground = RequireRenderObject(root.ChildElement);
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
                child: new AppBar(title: new Text("Demo"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Equal(
            themedStyle,
            Assert.Single(FindWidgets<AnnotatedRegion<SystemUiOverlayStyle>>(root.ChildElement)).Value);
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
                    title: new Text("Demo"),
                    systemOverlayStyle: widgetStyle)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Equal(
            widgetStyle,
            Assert.Single(FindWidgets<AnnotatedRegion<SystemUiOverlayStyle>>(root.ChildElement)).Value);
    }

    [Fact]
    public void AppBar_CenterTitleTrue_ConfiguresNavigationToolbar()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new AppBar(
                    title: new Text("Centered"),
                    centerTitle: true)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        NavigationToolbar toolbar = Assert.Single(FindWidgets<NavigationToolbar>(root.ChildElement));
        Assert.True(toolbar.CenterMiddle);
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
                child: new AppBar(title: new Text("Centered by theme"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        NavigationToolbar toolbar = Assert.Single(FindWidgets<NavigationToolbar>(root.ChildElement));
        Assert.True(toolbar.CenterMiddle);
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
                    title: new Text("Not centered"),
                    centerTitle: false)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        NavigationToolbar toolbar = Assert.Single(FindWidgets<NavigationToolbar>(root.ChildElement));
        Assert.False(toolbar.CenterMiddle);
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
                    title: new Text("Centered by platform"),
                    actions:
                    [
                        new SizedBox(width: 8, height: 8),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        NavigationToolbar toolbar = Assert.Single(FindWidgets<NavigationToolbar>(root.ChildElement));
        Assert.True(toolbar.CenterMiddle);
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
                    title: new Text("Not centered by platform"),
                    actions:
                    [
                        new SizedBox(width: 8, height: 8),
                        new SizedBox(width: 8, height: 8),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        NavigationToolbar toolbar = Assert.Single(FindWidgets<NavigationToolbar>(root.ChildElement));
        Assert.False(toolbar.CenterMiddle);
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
                    title: new Text("Title"),
                    leading: new SizedBox(width: 12, height: 12))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RenderObject appBarBackground = RequireRenderObject(root.ChildElement);
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
                    title: new Text("Title"),
                    leading: new SizedBox(width: 12, height: 12),
                    leadingWidth: 64)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RenderObject appBarBackground = RequireRenderObject(root.ChildElement);
        var leadingBox = FindConstrainedBox(appBarBackground, constraints =>
            Math.Abs(constraints.MinWidth - 64) < 0.001
            && Math.Abs(constraints.MaxWidth - 64) < 0.001);

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
                            appBar: new AppBar(title: new Text("Root")),
                            body: BuildBody());
                    },
                    settings: settings),
                "/details" => new BuilderPageRoute(
                    builder: _ => new Scaffold(
                        appBar: new AppBar(title: new Text("Details")),
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

        string arrowBackGlyph = char.ConvertFromUtf32(
            ThemeData.Light.Platform is TargetPlatform.IOS or TargetPlatform.MacOS
                ? Icons.ArrowBackIosNewRounded.CodePoint
                : Icons.ArrowBack.CodePoint);
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
                                appBar: new AppBar(title: new Text("Root")),
                                body: new SizedBox(width: 24, height: 12));
                        },
                        settings: new RouteSettings(Name: "/")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        string arrowBackGlyph = char.ConvertFromUtf32(
            ThemeData.Light.Platform is TargetPlatform.IOS or TargetPlatform.MacOS
                ? Icons.ArrowBackIosNewRounded.CodePoint
                : Icons.ArrowBack.CodePoint);
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
                            appBar: new AppBar(title: new Text("Root")),
                            body: BuildBody());
                    },
                    settings: settings),
                "/dialog" => new BuilderPageRoute(
                    builder: _ => new Scaffold(
                        appBar: new AppBar(title: new Text("Dialog")),
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

        string closeGlyph = char.ConvertFromUtf32(Icons.Close.CodePoint);
        var closeParagraph = FindParagraphByText(root.ChildElement?.RenderObject, closeGlyph);
        Assert.NotNull(closeParagraph);

        string arrowBackGlyph = char.ConvertFromUtf32(Icons.ArrowBack.CodePoint);
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
                            appBar: new AppBar(title: new Text("Root")),
                            body: BuildBody());
                    },
                    settings: settings),
                "/details" => new BuilderPageRoute(
                    builder: _ => new Scaffold(
                        appBar: new AppBar(
                            title: new Text("Details"),
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

        string arrowBackGlyph = char.ConvertFromUtf32(Icons.ArrowBack.CodePoint);
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
                    title: new Text("Title"),
                    actions:
                    [
                        new Text("Action"),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RenderObject appBarBackground = RequireRenderObject(root.ChildElement);
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
                    title: new Text("Title"),
                    actionsPadding: new Thickness(4, 6, 8, 10),
                    actions:
                    [
                        new Text("Action"),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RenderObject appBarBackground = RequireRenderObject(root.ChildElement);
        var actionsPadding = FindPadding(appBarBackground, padding =>
            Math.Abs(padding.Left - 4) < 0.001
            && Math.Abs(padding.Top - 6) < 0.001
            && Math.Abs(padding.Right - 8) < 0.001
            && Math.Abs(padding.Bottom - 10) < 0.001);

        Assert.NotNull(actionsPadding);
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
                    child: new AppBar(title: new Text("Title")))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RenderObject appBarBackground = RequireRenderObject(root.ChildElement);
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
                        title: new Text("Title"),
                        primary: false))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RenderObject appBarBackground = RequireRenderObject(root.ChildElement);
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
                    title: new Text("Title"),
                    actionsPadding: new Thickness(3, 4, 5, 6),
                    actions:
                    [
                        new Text("One"),
                        new Text("Two"),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RenderObject appBarBackground = RequireRenderObject(root.ChildElement);
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
                    title: new Text("Title"),
                    actionsPadding: new Thickness(7, 8, 9, 10),
                    actions:
                    [
                        new Text("One"),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RenderObject appBarBackground = RequireRenderObject(root.ChildElement);
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
                    title: new Text("Title"),
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
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(onSurface: Colors.CadetBlue),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    title: new Text("Title"),
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
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(onPrimary: Colors.CadetBlue),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    title: new Text("Title"),
                    leading: new CaptureIconThemeWidget(themeData => capturedTheme = themeData))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(Colors.CadetBlue, capturedTheme!.Color);
        Assert.Equal(24, capturedTheme.Size);
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
                    title: new Text("Title"),
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
    public void AppBar_IconTheme_WithNullColor_KeepsTheAmbientColor_ForLeading()
    {
        // Dart resolves `widget.iconTheme ?? appBarTheme.iconTheme ?? defaults…copyWith(foregroundColor)`:
        // a supplied icon theme ends the chain, so a null color falls through `IconTheme.merge` to the
        // ambient theme rather than to `foregroundColor`.
        IconThemeData? capturedTheme = null;
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new AppBar(
                    title: new Text("Title"),
                    foregroundColor: Colors.DarkRed,
                    iconTheme: new IconThemeData(Size: 22),
                    leading: new CaptureIconThemeWidget(themeData => capturedTheme = themeData))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(capturedTheme);
        Assert.Equal(ThemeData.Light.IconTheme.Color, capturedTheme!.Color);
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
                    title: new Text("Title"),
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
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(
                onSurface: Colors.CadetBlue,
                onSurfaceVariant: Colors.Goldenrod),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    title: new Text("Title"),
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
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(onPrimary: Colors.CadetBlue),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(
                    title: new Text("Title"),
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
                    title: new Text("Title"),
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
                    title: new Text("Title"),
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
                    title: new Text("Title"),
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
                    title: new Text("Title"),
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
                    title: new Text("Title"),
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
    public void AppBar_TitleSpacing_ConfiguresNavigationToolbar()
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

        NavigationToolbar toolbar = Assert.Single(FindWidgets<NavigationToolbar>(root.ChildElement));
        Assert.Equal(24, toolbar.MiddleSpacing);
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

        NavigationToolbar toolbar = Assert.Single(FindWidgets<NavigationToolbar>(root.ChildElement));
        Assert.Equal(22, toolbar.MiddleSpacing);
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

        NavigationToolbar toolbar = Assert.Single(FindWidgets<NavigationToolbar>(root.ChildElement));
        Assert.Equal(30, toolbar.MiddleSpacing);
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
                child: new AppBar(title: new Text("Title"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RenderObject appBarBackground = RequireRenderObject(root.ChildElement);
        Assert.Equal(72, ResolvedToolbarHeight(appBarBackground));
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
                    title: new Text("Title"),
                    toolbarHeight: 64)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RenderObject appBarBackground = RequireRenderObject(root.ChildElement);
        Assert.Equal(64, ResolvedToolbarHeight(appBarBackground));
    }

    [Fact]
    public void AppBar_ToolbarHeight_DefaultsTo56_WhenUseMaterial3IsEnabled()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new AppBar(title: new Text("Title"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RenderObject appBarBackground = RequireRenderObject(root.ChildElement);
        Assert.Equal(56, ResolvedToolbarHeight(appBarBackground));
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
                child: new AppBar(title: new Text("Title"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RenderObject appBarBackground = RequireRenderObject(root.ChildElement);
        Assert.Equal(56, ResolvedToolbarHeight(appBarBackground));
    }

    [Fact]
    public void AppBar_TitleTextStyle_DefaultsFromTextThemeTitleLarge_WithThemeForegroundFallback()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(onSurface: Colors.Bisque),
            TextTheme = new MaterialTextTheme(
                titleLarge: new TextStyle(
                    FontSize: 29,
                    Color: Colors.Crimson,
                    FontWeight: FontWeight.Bold)),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new AppBar(title: new Text("Title"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RenderObject appBarBackground = RequireRenderObject(root.ChildElement);
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
                child: new AppBar(title: new Text("Title"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RenderObject appBarBackground = RequireRenderObject(root.ChildElement);
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
                    title: new Text("Title"),
                    titleTextStyle: new TextStyle(
                        FontSize: 18,
                        Color: Colors.LimeGreen,
                        FontWeight: FontWeight.Normal))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RenderObject appBarBackground = RequireRenderObject(root.ChildElement);
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
                    title: new Text("Title"),
                    actions:
                    [
                        new Text("Action"),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RenderObject appBarBackground = RequireRenderObject(root.ChildElement);
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
                    title: new Text("Title"),
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

        RenderObject appBarBackground = RequireRenderObject(root.ChildElement);
        var actionParagraph = FindParagraphByText(appBarBackground, "Action");
        Assert.NotNull(actionParagraph);
        Assert.Equal(15, actionParagraph!.FontSize);
        Assert.Equal(FontWeight.Normal, actionParagraph.FontWeight);
        Assert.Equal(Colors.CadetBlue, Assert.IsType<SolidColorBrush>(actionParagraph.Foreground).Color);
    }

    [Fact]
    public void AppBar_NegativeTitleSpacing_IsAcceptedLikeFlutter()
    {
        var appBar = new AppBar(
            title: new Text("Invalid"),
            titleSpacing: -1);

        Assert.Equal(-1, appBar.TitleSpacing);
    }

    [Fact]
    public void AppBar_ZeroThemeToolbarHeight_IsAcceptedLikeFlutter()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    AppBarTheme = new AppBarThemeData(ToolbarHeight: 0),
                },
                child: new AppBar(title: new Text("Invalid"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Equal(0, ResolvedToolbarHeight(root.ChildElement?.RenderObject));
    }

    [Fact]
    public void AppBar_ZeroThemeLeadingWidth_IsAcceptedLikeFlutter()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    AppBarTheme = new AppBarThemeData(LeadingWidth: 0),
                },
                child: new AppBar(
                    title: new Text("Invalid"),
                    leading: new SizedBox(width: 8, height: 8))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var leading = FindConstrainedBox(
            root.ChildElement?.RenderObject,
            constraints => constraints.MinWidth == 0 && constraints.MaxWidth == 0);
        Assert.NotNull(leading);
    }

    [Fact]
    public void AppBarTheme_LocalData_OverridesThemeData_AndWidgetOverridesLocalData()
    {
        var localOwner = new BuildOwner();
        var localRoot = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    AppBarTheme = new AppBarThemeData(BackgroundColor: Colors.CadetBlue),
                },
                child: new AppBarTheme(
                    data: new AppBarThemeData(BackgroundColor: Colors.Crimson),
                    child: new AppBar(title: new Text("Local theme")))));

        localRoot.Attach(localOwner);
        localRoot.Mount(parent: null, newSlot: null);
        localOwner.FlushBuild();
        Assert.Contains(
            FindWidgets<MaterialWidget>(localRoot.ChildElement),
            material => material.Color == Colors.Crimson);

        var widgetOwner = new BuildOwner();
        var widgetRoot = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new AppBarTheme(
                    data: new AppBarThemeData(BackgroundColor: Colors.Crimson),
                    child: new AppBar(
                        title: new Text("Widget override"),
                        backgroundColor: Colors.DarkGreen))));

        widgetRoot.Attach(widgetOwner);
        widgetRoot.Mount(parent: null, newSlot: null);
        widgetOwner.FlushBuild();
        Assert.Contains(
            FindWidgets<MaterialWidget>(widgetRoot.ChildElement),
            material => material.Color == Colors.DarkGreen);
    }

    [Fact]
    public void AppBarTheme_Of_UsesNearestLocalData()
    {
        var owner = new BuildOwner();
        AppBarThemeData? captured = null;
        var localData = new AppBarThemeData(
            ForegroundColor: Colors.Goldenrod,
            ToolbarHeight: 72);
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    AppBarTheme = new AppBarThemeData(ForegroundColor: Colors.CadetBlue),
                },
                child: new AppBarTheme(
                    data: localData,
                    child: new CaptureBuildContextWidget(
                        capture: context => captured = AppBarTheme.Of(context)))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Equal(localData, captured);
    }

    [Fact]
    public void AppBarTheme_RejectsDataCombinedWithIndividualProperties()
    {
        Assert.Throws<ArgumentException>(() => new AppBarTheme(
            data: new AppBarThemeData(),
            backgroundColor: Colors.Crimson));
        Assert.Throws<ArgumentException>(() => new AppBarTheme(
            color: Colors.Crimson,
            backgroundColor: Colors.CadetBlue));
    }

    [Fact]
    public void AppBarThemeData_Lerp_InterpolatesContinuousAndStepsDiscreteProperties()
    {
        var begin = new AppBarThemeData(
            BackgroundColor: Colors.Black,
            Elevation: 2,
            ToolbarHeight: 40,
            CenterTitle: false);
        var end = new AppBarThemeData(
            BackgroundColor: Colors.White,
            Elevation: 10,
            ToolbarHeight: 80,
            CenterTitle: true);

        var firstHalf = AppBarThemeData.Lerp(begin, end, 0.25);
        var secondHalf = AppBarThemeData.Lerp(begin, end, 0.75);

        Assert.Equal(4, firstHalf.Elevation);
        Assert.Equal(50, firstHalf.ToolbarHeight);
        Assert.False(firstHalf.CenterTitle);
        Assert.True(secondHalf.CenterTitle);
        Assert.NotEqual(begin.BackgroundColor, firstHalf.BackgroundColor);
        Assert.NotEqual(end.BackgroundColor, firstHalf.BackgroundColor);

        var copied = new AppBarTheme(
            backgroundColor: Colors.Crimson,
            centerTitle: false)
            .CopyWith(
                toolbarHeight: 72,
                centerTitle: true);
        Assert.Equal(Colors.Crimson, copied.BackgroundColor);
        Assert.Equal(72, copied.ToolbarHeight);
        Assert.True(copied.CenterTitle);

        var aliased = begin.CopyWith(color: Colors.Crimson);
        Assert.Equal(Colors.Crimson, aliased.BackgroundColor);
        Assert.Throws<ArgumentException>(() => begin.CopyWith(
            color: Colors.Crimson,
            backgroundColor: Colors.CadetBlue));
    }

    [Fact]
    public void DrawerController_DefaultsAndGuards_MatchFlutterContract()
    {
        var controller = new DrawerController(
            child: new SizedBox(),
            alignment: DrawerAlignment.Start);

        Assert.False(controller.IsDrawerOpen);
        Assert.Null(controller.DrawerCallback);
        Assert.Equal(DragStartBehavior.Start, controller.DragStartBehavior);
        Assert.Null(controller.ScrimColor);
        Assert.Null(controller.EdgeDragWidth);
        Assert.True(controller.EnableOpenDragGesture);
        Assert.True(controller.DrawerBarrierDismissible);
        Assert.Throws<ArgumentOutOfRangeException>(() => new DrawerController(
            child: new SizedBox(),
            alignment: DrawerAlignment.Start,
            edgeDragWidth: 0));
    }

    [Fact]
    public void DrawerController_Of_ExposesAlignmentInsideOpenDrawer()
    {
        var owner = new BuildOwner();
        DrawerController? captured = null;
        var root = new TestRootElement(
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new Theme(
                    data: ThemeData.Light,
                    child: new DrawerController(
                        alignment: DrawerAlignment.End,
                        isDrawerOpen: true,
                        child: new CaptureBuildContextWidget(
                            capture: context => captured = DrawerController.Of(context))))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(captured);
        Assert.Equal(DrawerAlignment.End, captured!.Alignment);
    }

    [Fact]
    public void Drawer_UsesAlignmentSpecificThemeShapeFromDrawerControllerScope()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new Theme(
                    data: ThemeData.Light with
                    {
                        DrawerTheme = new DrawerThemeData(
                            Shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(4)),
                            EndShape: new RoundedRectangleBorder(borderRadius:
                                Plumix.Rendering.BorderRadius.Circular(18))),
                    },
                    child: new DrawerController(
                        alignment: DrawerAlignment.End,
                        isDrawerOpen: true,
                        child: new Drawer(child: new Text("End shape"))))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(root.ChildElement?.RenderObject);
        Assert.NotNull(decorated);
        Assert.Equal(BorderRadius.Circular(18), decorated!.Decoration.EffectiveBorderRadius);
    }

    [Fact]
    public void DrawerController_OpenAndClose_DriveAnimationAndCallback()
    {
        var key = new LabeledGlobalKey<DrawerControllerState>("drawer-controller");
        var callbacks = new List<bool>();
        using var harness = new WidgetRenderHarness(
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new Theme(
                    data: ThemeData.Light with { Platform = TargetPlatform.Android },
                    child: new DrawerController(
                        key: key,
                        alignment: DrawerAlignment.Start,
                        drawerCallback: callbacks.Add,
                        child: new Drawer(child: new Text("Drawer content"))))));

        harness.Pump(new Size(400, 300));
        Assert.NotNull(key.CurrentState);
        Assert.False(key.CurrentState!.IsOpen);

        key.CurrentState.Open();
        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
        harness.Pump(new Size(400, 300));
        Assert.True(key.CurrentState.IsOpen);

        key.CurrentState.Close();
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.80));
        harness.Pump(new Size(400, 300));
        Assert.False(key.CurrentState.IsOpen);
        Assert.Equal([true, false], callbacks);
    }

    [Fact]
    public void DrawerController_EdgeDrag_UsesSafePaddingAndPaintsAnimatedScrim()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        var key = new LabeledGlobalKey<DrawerControllerState>("edge-drag-drawer");
        var callbacks = new List<bool>();
        using var harness = new WidgetRenderHarness(
            new MediaQuery(
                data: new MediaQueryData(Padding: new Thickness(12, 0, 0, 0)),
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new Theme(
                        data: ThemeData.Light with { Platform = TargetPlatform.Android },
                        child: new DrawerController(
                            key: key,
                            alignment: DrawerAlignment.Start,
                            drawerCallback: callbacks.Add,
                            child: new Drawer(
                                width: 240,
                                child: new Text("Dragged drawer")))))));

        try
        {
            harness.Pump(new Size(400, 300));
            var edgeArea = FindDescendant<RenderPointerListener>(harness.RenderView);
            Assert.NotNull(edgeArea);
            Assert.Equal(32, edgeArea!.Size.Width, 3);

            DateTime start = DateTime.UtcNow;
            DispatchPointerDown(
                binding,
                harness.RenderView,
                pointer: 7201,
                position: new Point(2, 120),
                timestampUtc: start);
            DispatchPointerMove(
                binding,
                harness.RenderView,
                pointer: 7201,
                position: new Point(180, 120),
                timestampUtc: start.AddMilliseconds(100));
            DispatchPointerUp(
                binding,
                harness.RenderView,
                pointer: 7201,
                position: new Point(200, 120),
                timestampUtc: start.AddMilliseconds(150));

            double now = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
            harness.Pump(new Size(400, 300));

            Assert.True(key.CurrentState!.IsOpen);
            Assert.Contains(true, callbacks);
            Assert.NotNull(FindParagraphByText(harness.RenderView, "Dragged drawer"));
            Assert.NotNull(FindColoredBox(
                harness.RenderView,
                color => color == Color.FromArgb(0x8A, 0, 0, 0)));
        }
        finally
        {
            binding.ResetForTests();
        }
    }

    [Fact]
    public void DrawerController_DragSettle_UsesMeasuredChildWidth()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        var key = new LabeledGlobalKey<DrawerControllerState>("measured-drawer");
        using var harness = new WidgetRenderHarness(
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new Theme(
                    data: ThemeData.Light with { Platform = TargetPlatform.Android },
                    child: new DrawerController(
                        key: key,
                        alignment: DrawerAlignment.Start,
                        isDrawerOpen: true,
                        child: new SizedBox(
                            width: 180,
                            child: new Text("Narrow drawer"))))));

        try
        {
            harness.Pump(new Size(400, 300));
            DateTime start = DateTime.UtcNow;
            DispatchPointerDown(
                binding,
                harness.RenderView,
                pointer: 7202,
                position: new Point(160, 120),
                timestampUtc: start);
            DispatchPointerMove(
                binding,
                harness.RenderView,
                pointer: 7202,
                position: new Point(40, 120),
                timestampUtc: start.AddSeconds(1));
            DispatchPointerUp(
                binding,
                harness.RenderView,
                pointer: 7202,
                position: new Point(40, 120),
                timestampUtc: start.AddSeconds(1.1));

            double now = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
            harness.Pump(new Size(400, 300));

            Assert.False(key.CurrentState!.IsOpen);
        }
        finally
        {
            binding.ResetForTests();
        }
    }

    [Fact]
    public void DrawerController_Open_AddsLocalHistoryAndBackClosesWithoutPoppingRoute()
    {
        var owner = new BuildOwner();
        var drawerKey = new LabeledGlobalKey<DrawerControllerState>("history-drawer");
        var navigatorKey = new LabeledGlobalKey<NavigatorState>("history-navigator");
        var callbacks = new List<bool>();
        var root = new TestRootElement(
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new Theme(
                    data: ThemeData.Light,
                    child: new Navigator(
                        key: navigatorKey,
                        initialRoute: new BuilderPageRoute(
                            _ => new DrawerController(
                                key: drawerKey,
                                alignment: DrawerAlignment.Start,
                                drawerCallback: callbacks.Add,
                                child: new Drawer(child: new Text("History drawer"))))))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Route? initialRoute = navigatorKey.CurrentState!.CurrentRoute;
        drawerKey.CurrentState!.Open();
        owner.FlushBuild();
        Assert.True(navigatorKey.CurrentState.MaybePop());
        owner.FlushBuild();

        Assert.Same(initialRoute, navigatorKey.CurrentState.CurrentRoute);
        Assert.Equal([true, false], callbacks);
    }

    [Fact]
    public void Scaffold_OpenEndDrawer_ProvidesEndAlignedDrawerControllerScope()
    {
        var owner = new BuildOwner();
        BuildContext? scaffoldContext = null;
        DrawerController? captured = null;
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new Scaffold(
                    endDrawer: new Drawer(
                        child: new CaptureBuildContextWidget(
                            capture: context => captured = DrawerController.Of(context))),
                    body: new CaptureBuildContextWidget(
                        capture: context => scaffoldContext = context))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
        Scaffold.Of(scaffoldContext!.Value).OpenEndDrawer();
        SettleDrawerAnimation(owner);

        Assert.NotNull(captured);
        Assert.Equal(DrawerAlignment.End, captured!.Alignment);
    }

    [Fact]
    public void AppBar_StatefulWidgetAndThemeBackgrounds_ResolveScrolledUnderFromVerticalUpdates()
    {
        BuildContext? emitterContext = null;
        WidgetStateColor widgetBackground = WidgetStateColor.ResolveWith(
            states => states.Contains(WidgetState.ScrolledUnder)
                ? Colors.DarkGreen
                : Colors.Goldenrod);
        WidgetStateColor themeBackground = WidgetStateColor.ResolveWith(
            states => states.Contains(WidgetState.ScrolledUnder)
                ? Colors.DarkSlateBlue
                : Colors.Crimson);
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    AppBarTheme = new AppBarThemeData(BackgroundColorState: themeBackground),
                },
                child: new ScrollNotificationObserver(
                    child: new Column(
                        children:
                        [
                            new AppBar(title: new Text("Theme state")),
                            new AppBar(title: new Text("Widget state"), backgroundColor: widgetBackground),
                            new CaptureBuildContextWidget(context => emitterContext = context),
                        ]))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(emitterContext);
        Assert.Contains(
            FindWidgets<MaterialWidget>(root.ChildElement),
            material => material.Color == Colors.Crimson);
        Assert.Contains(
            FindWidgets<MaterialWidget>(root.ChildElement),
            material => material.Color == Colors.Goldenrod);

        var notification = new ScrollUpdateNotification(
            new FixedScrollMetrics(
                minScrollExtent: 0,
                maxScrollExtent: 100,
                pixels: 12,
                viewportDimension: 40,
                axisDirection: AxisDirection.Down,
                devicePixelRatio: 1.0));
        notification.Dispatch(emitterContext);
        owner.FlushBuild();

        Assert.Contains(
            FindWidgets<MaterialWidget>(root.ChildElement),
            material => material.Color == Colors.DarkSlateBlue);
        Assert.Contains(
            FindWidgets<MaterialWidget>(root.ChildElement),
            material => material.Color == Colors.DarkGreen);
    }

    [Fact]
    public void AppBar_ExposesFlutterVisualAndSemanticsConfiguration()
    {
        var shape = new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(12));
        var appBar = new AppBar(
            title: new Text("Configured"),
            elevation: 2,
            scrolledUnderElevation: 5,
            shadowColor: Colors.Black,
            surfaceTintColor: Colors.CadetBlue,
            shape: shape,
            excludeHeaderSemantics: true,
            toolbarOpacity: 0.75,
            bottomOpacity: 0.5,
            forceMaterialTransparency: true,
            useDefaultSemanticsOrder: false,
            clipBehavior: Clip.AntiAlias,
            animateColor: true);

        Assert.Equal(2, appBar.Elevation);
        Assert.Equal(5, appBar.ScrolledUnderElevation);
        Assert.Equal(Colors.Black, appBar.ShadowColor);
        Assert.Equal(Colors.CadetBlue, appBar.SurfaceTintColor);
        Assert.Same(shape, appBar.Shape);
        Assert.True(appBar.ExcludeHeaderSemantics);
        Assert.Equal(0.75, appBar.ToolbarOpacity);
        Assert.Equal(0.5, appBar.BottomOpacity);
        Assert.True(appBar.ForceMaterialTransparency);
        Assert.False(appBar.UseDefaultSemanticsOrder);
        Assert.Equal(Clip.AntiAlias, appBar.ClipBehavior);
        Assert.True(appBar.AnimateColor);
        Assert.Throws<ArgumentOutOfRangeException>(() => new AppBar(elevation: -1));
    }

    /// <summary>
    /// The toolbar height the app bar resolved, read off the `_ToolbarContainerLayout` delegate that sizes
    /// and bottom-justifies the toolbar.
    /// </summary>
    private static double ResolvedToolbarHeight(RenderObject? root)
    {
        var box = FindDescendant<RenderCustomSingleChildLayoutBox>(root);
        Assert.NotNull(box);
        return Assert.IsType<ToolbarContainerLayout>(box!.LayoutDelegate).ToolbarHeight;
    }

    private static T RequireRenderObject<T>(Element? element) where T : RenderObject
    {
        Assert.NotNull(element);
        Assert.NotNull(element!.RenderObject);
        T? result = FindDescendant<T>(element.RenderObject);
        return Assert.IsType<T>(result);
    }

    private static RenderObject RequireRenderObject(Element? element)
    {
        Assert.NotNull(element);
        Assert.NotNull(element!.RenderObject);
        return element.RenderObject;
    }

    private static IReadOnlyList<T> FindWidgets<T>(Element? element) where T : Widget
    {
        var widgets = new List<T>();
        FindWidgets(element, widgets);
        return widgets;
    }

    private static void FindWidgets<T>(Element? element, List<T> widgets) where T : Widget
    {
        if (element is null)
        {
            return;
        }

        if (element.Widget is T widget)
        {
            widgets.Add(widget);
        }

        element.VisitChildren(child => FindWidgets(child, widgets));
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

        if (root is RenderParagraph paragraph && string.Equals(paragraph.PlainText, text, StringComparison.Ordinal))
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

    /// <summary>Matches the drawer scrim, whose default color is black at 54% opacity.</summary>
    private static bool IsBlackScrim(Color color) =>
        color.R == 0 && color.G == 0 && color.B == 0 && color.A > 0;

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
