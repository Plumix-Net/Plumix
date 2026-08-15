using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

/// <summary>
/// Parity coverage for `_ScaffoldLayout`, `ScaffoldGeometry`/`_ScaffoldGeometryNotifier` and
/// `_FloatingActionButtonTransition` (Dart parity source: `material_ui/lib/src/scaffold.dart`).
/// </summary>
[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialScaffoldGeometryTests
{
    private static readonly Size Viewport = new(800, 600);

    [Fact]
    public void ScaffoldGeometry_BottomNavigationBarTop_IsTheBarsTopEdge()
    {
        BuildContext? context = null;
        using var harness = new Harness(Wrap(new Scaffold(
            body: Capture(c => context = c),
            bottomNavigationBar: new SizedBox(height: 100))));
        harness.Pump(Viewport);

        ScaffoldGeometry geometry = RequireGeometry(context);
        Assert.Equal(500.0, geometry.BottomNavigationBarTop);
    }

    [Fact]
    public void ScaffoldGeometry_WithoutBottomNavigationBar_LeavesBottomNavigationBarTopNull()
    {
        BuildContext? context = null;
        using var harness = new Harness(Wrap(new Scaffold(body: Capture(c => context = c))));
        harness.Pump(Viewport);

        Assert.Null(RequireGeometry(context).BottomNavigationBarTop);
    }

    [Fact]
    public void ScaffoldGeometry_FloatingActionButtonArea_MatchesTheMeasuredButtonRect()
    {
        BuildContext? context = null;
        using var harness = new Harness(Wrap(new Scaffold(
            body: Capture(c => context = c),
            floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { }))));
        harness.Pump(Viewport);
        harness.SettleFloatingActionButton();

        RenderBox slot = harness.RequireSlot(ScaffoldSlot.FloatingActionButton);
        Assert.Equal(new Size(56, 56), slot.Size);

        Rect? area = RequireGeometry(context).FloatingActionButtonArea;
        Assert.NotNull(area);

        // endFloat: x = width - kFloatingActionButtonMargin - fabWidth, y = contentBottom - fabHeight - margin.
        Assert.Equal(new Rect(new Point(728, 528), new Size(56, 56)), area!.Value);
        Assert.Equal(area.Value.Position, ((MultiChildLayoutParentData)slot.parentData!).offset);
    }

    [Fact]
    public void ScaffoldGeometry_WithoutFloatingActionButton_LeavesTheAreaNull()
    {
        BuildContext? context = null;
        using var harness = new Harness(Wrap(new Scaffold(body: Capture(c => context = c))));
        harness.Pump(Viewport);

        // The slot is always present, so the null area comes from scaling the stored rect by 0.0.
        Assert.Null(RequireGeometry(context).FloatingActionButtonArea);
    }

    [Fact]
    public void ScaffoldGeometry_ScalesTheAreaAboutItsCenterWhileTheButtonEnters()
    {
        BuildContext? context = null;
        using var harness = new Harness(Wrap(new Scaffold(body: Capture(c => context = c))));
        harness.Pump(Viewport);

        harness.Update(Wrap(new Scaffold(
            body: Capture(c => context = c),
            floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { }))));
        harness.Pump(Viewport);
        harness.Tick(0.05);
        harness.Pump(Viewport);

        Rect? entering = RequireGeometry(context).FloatingActionButtonArea;
        Assert.NotNull(entering);
        Assert.True(entering!.Value.Width > 0.0);
        Assert.True(entering.Value.Width < 56.0);

        harness.SettleFloatingActionButton();
        Rect settled = RequireGeometry(context).FloatingActionButtonArea!.Value;
        Assert.Equal(56.0, settled.Width, 6);
        Assert.Equal(settled.Center.X, entering.Value.Center.X, 6);
        Assert.Equal(settled.Center.Y, entering.Value.Center.Y, 6);
    }

    [Fact]
    public void ScaffoldGeometry_NotifiesOnEveryAnimatingFrame()
    {
        BuildContext? context = null;
        using var harness = new Harness(Wrap(new Scaffold(body: Capture(c => context = c))));
        harness.Pump(Viewport);

        ScaffoldGeometryNotifier notifier = Scaffold.GeometryNotifierMaybeOf(context!.Value)!;
        int notifications = 0;
        notifier.AddListener(() => notifications++);

        int afterFirstFrame = notifications;
        harness.Update(Wrap(new Scaffold(
            body: Capture(c => context = c),
            floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { }))));
        harness.Pump(Viewport);
        Assert.True(notifications > afterFirstFrame);

        int afterAdding = notifications;
        harness.Tick(0.05);
        harness.Pump(Viewport);
        Assert.True(notifications > afterAdding);
    }

    [Fact]
    public void ScaffoldGeometryOf_WithoutAScaffoldAncestor_Throws()
    {
        BuildContext? context = null;
        using var harness = new Harness(Wrap(Capture(c => context = c)));
        harness.Pump(Viewport);

        Assert.Throws<InvalidOperationException>(() => Scaffold.GeometryOf(context!.Value));
    }

    [Fact]
    public void ScaffoldGeometryValue_OutsideThePaintPhase_Throws()
    {
        BuildContext? context = null;
        using var harness = new Harness(Wrap(new Scaffold(body: Capture(c => context = c))));
        harness.Pump(Viewport);

        IValueListenable<ScaffoldGeometry> listenable = Scaffold.GeometryOf(context!.Value);
        Assert.Throws<InvalidOperationException>(() => _ = listenable.Value);
    }

    [Fact]
    public void ScaffoldLayout_MeasuresTheBottomSheetSoTheFloatingButtonAvoidsIt()
    {
        BuildContext? context = null;
        using var harness = new Harness(Wrap(new Scaffold(
            body: Capture(c => context = c),
            bottomSheet: new SizedBox(height: 100),
            floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { }))));
        harness.Pump(Viewport);
        harness.SettleFloatingActionButton();

        Assert.Equal(100.0, harness.RequireSlot(ScaffoldSlot.BottomSheet).Size.Height);

        // FabFloatOffsetY clamps to contentBottom - bottomSheetHeight - fabHeight / 2.
        Rect area = RequireGeometry(context).FloatingActionButtonArea!.Value;
        Assert.Equal(600.0 - 100.0 - 28.0, area.Top);
    }

    [Fact]
    public void ScaffoldLayout_MeasuresAFixedSnackBarBeforePositioningTheFloatingButton()
    {
        BuildContext? context = null;
        using var harness = new Harness(Wrap(new ScaffoldMessenger(new Scaffold(
            body: Capture(c => context = c),
            floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { })))));
        harness.Pump(Viewport);
        harness.SettleFloatingActionButton();
        double withoutSnackBar = RequireGeometry(context).FloatingActionButtonArea!.Value.Top;

        harness.FindState<ScaffoldMessengerState>().ShowSnackBar(
            new SnackBar(content: new SizedBox(height: 40), behavior: SnackBarBehavior.Fixed));
        harness.Tick(0.4);
        harness.Pump(Viewport);

        double snackBarHeight = harness.RequireSlot(ScaffoldSlot.SnackBar).Size.Height;
        Assert.True(snackBarHeight > 0.0);

        // FabFloatOffsetY clamps to contentBottom - snackBarHeight - fabHeight - margin.
        Rect area = RequireGeometry(context).FloatingActionButtonArea!.Value;
        Assert.Equal(600.0 - snackBarHeight - 56.0 - 16.0, area.Top, 6);
        Assert.True(area.Top < withoutSnackBar);
    }

    [Fact]
    public void ScaffoldLayout_TopLocationIgnoresExtendBodyBehindAppBar()
    {
        BuildContext? plain = null;
        using var withAppBarBehind = new Harness(Wrap(new Scaffold(
            appBar: new AppBar(titleText: "Demo"),
            extendBodyBehindAppBar: true,
            body: Capture(c => plain = c),
            floatingActionButtonLocation: FloatingActionButtonLocation.EndTop,
            floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { }))));
        withAppBarBehind.Pump(Viewport);
        withAppBarBehind.SettleFloatingActionButton();
        double extended = RequireGeometry(plain).FloatingActionButtonArea!.Value.Top;

        BuildContext? second = null;
        using var normal = new Harness(Wrap(new Scaffold(
            appBar: new AppBar(titleText: "Demo"),
            body: Capture(c => second = c),
            floatingActionButtonLocation: FloatingActionButtonLocation.EndTop,
            floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { }))));
        normal.Pump(Viewport);
        normal.SettleFloatingActionButton();

        Assert.Equal(RequireGeometry(second).FloatingActionButtonArea!.Value.Top, extended);
    }

    [Fact]
    public void ScaffoldLayout_ExtendBodyBehindAppBarPlacesTheBodyUnderTheAppBar()
    {
        using var extended = new Harness(Wrap(new Scaffold(
            appBar: new AppBar(titleText: "Demo"),
            extendBodyBehindAppBar: true,
            body: new SizedBox())));
        extended.Pump(Viewport);
        Assert.Equal(
            0.0,
            ((MultiChildLayoutParentData)extended.RequireSlot(ScaffoldSlot.Body).parentData!).offset.Y);

        using var normal = new Harness(Wrap(new Scaffold(
            appBar: new AppBar(titleText: "Demo"),
            body: new SizedBox())));
        normal.Pump(Viewport);
        double appBarHeight = normal.RequireSlot(ScaffoldSlot.AppBar).Size.Height;
        Assert.True(appBarHeight > 0.0);
        Assert.Equal(
            appBarHeight,
            ((MultiChildLayoutParentData)normal.RequireSlot(ScaffoldSlot.Body).parentData!).offset.Y);
    }

    [Fact]
    public void ScaffoldLayout_ResizeToAvoidBottomInsetFalse_KeepsTheButtonAboveTheKeyboard()
    {
        BuildContext? context = null;
        var media = new MediaQueryData(Size: Viewport, ViewInsets: new Thickness(0, 0, 0, 300));
        using var harness = new Harness(Wrap(
            new Scaffold(
                body: Capture(c => context = c),
                resizeToAvoidBottomInset: false,
                floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { })),
            media));
        harness.Pump(Viewport);
        harness.SettleFloatingActionButton();

        Assert.Equal(528.0, RequireGeometry(context).FloatingActionButtonArea!.Value.Top);
    }

    [Fact]
    public void ScaffoldLayout_ResizeToAvoidBottomInset_LiftsTheButtonAboveTheKeyboard()
    {
        BuildContext? context = null;
        var media = new MediaQueryData(Size: Viewport, ViewInsets: new Thickness(0, 0, 0, 300));
        using var harness = new Harness(Wrap(
            new Scaffold(
                body: Capture(c => context = c),
                floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { })),
            media));
        harness.Pump(Viewport);
        harness.SettleFloatingActionButton();

        // contentBottom = 600 - 300, then the standard float margin.
        Assert.Equal(300.0 - 56.0 - 16.0, RequireGeometry(context).FloatingActionButtonArea!.Value.Top);
    }

    [Fact]
    public void ScaffoldState_LocationChange_AnimatesTheButtonThroughTheMotionAnimator()
    {
        BuildContext? context = null;
        using var harness = new Harness(Wrap(new Scaffold(
            body: Capture(c => context = c),
            floatingActionButtonLocation: FloatingActionButtonLocation.EndFloat,
            floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { }))));
        harness.Pump(Viewport);
        harness.SettleFloatingActionButton();
        Assert.Equal(728.0, RequireGeometry(context).FloatingActionButtonArea!.Value.Left);

        harness.Update(Wrap(new Scaffold(
            body: Capture(c => context = c),
            floatingActionButtonLocation: FloatingActionButtonLocation.CenterFloat,
            floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { }))));
        harness.Pump(Viewport);

        // The scaling animator holds the old offset for the first half of the 400ms segue.
        Assert.Equal(
            728.0,
            ((MultiChildLayoutParentData)harness.RequireSlot(ScaffoldSlot.FloatingActionButton).parentData!)
            .offset.X);

        harness.Tick(0.25);
        harness.Pump(Viewport);
        Assert.Equal(
            372.0,
            ((MultiChildLayoutParentData)harness.RequireSlot(ScaffoldSlot.FloatingActionButton).parentData!)
            .offset.X);
    }

    [Fact]
    public void ScaffoldState_InterruptedLocationChange_RestartsFromTheAnimatorRestartValue()
    {
        var scaffoldKey = new LabeledGlobalKey<ScaffoldState>("scaffold");
        using var harness = new Harness(Wrap(new Scaffold(
            key: scaffoldKey,
            body: new SizedBox(),
            floatingActionButtonLocation: FloatingActionButtonLocation.EndFloat,
            floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { }))));
        harness.Pump(Viewport);
        harness.SettleFloatingActionButton();

        harness.Update(Wrap(new Scaffold(
            key: scaffoldKey,
            body: new SizedBox(),
            floatingActionButtonLocation: FloatingActionButtonLocation.CenterFloat,
            floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { }))));
        harness.Pump(Viewport);
        harness.Tick(0.1);
        harness.Pump(Viewport);

        var state = harness.FindState<ScaffoldState>();
        double interruptedAt = state.FloatingActionButtonMoveProgressForTests;
        Assert.InRange(interruptedAt, 0.2, 0.3);

        harness.Update(Wrap(new Scaffold(
            key: scaffoldKey,
            body: new SizedBox(),
            floatingActionButtonLocation: FloatingActionButtonLocation.StartFloat,
            floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { }))));
        harness.Pump(Viewport);

        // getAnimationRestart(previous) == min(1 - previous, previous) avoids a size jump.
        Assert.Equal(
            Math.Min(1.0 - interruptedAt, interruptedAt),
            state.FloatingActionButtonMoveProgressForTests,
            6);
    }

    [Fact]
    public void ScaffoldLayout_MotionRelayoutsWithoutRebuildingTheScaffold()
    {
        var scaffoldKey = new LabeledGlobalKey<ScaffoldState>("scaffold");
        using var harness = new Harness(Wrap(new Scaffold(
            key: scaffoldKey,
            body: new SizedBox(),
            floatingActionButtonLocation: FloatingActionButtonLocation.EndFloat,
            floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { }))));
        harness.Pump(Viewport);
        harness.SettleFloatingActionButton();

        harness.Update(Wrap(new Scaffold(
            key: scaffoldKey,
            body: new SizedBox(),
            floatingActionButtonLocation: FloatingActionButtonLocation.CenterFloat,
            floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { }))));
        harness.Pump(Viewport);

        var layout = harness.RequireLayout();
        Assert.False(layout.NeedsLayout);

        // The delegate listens to the move controller, so a tick dirties layout on its own.
        harness.Tick(0.05);
        Assert.True(layout.NeedsLayout);
    }

    [Fact]
    public void ScaffoldState_NoAnimationAnimator_PlacesTheButtonImmediately()
    {
        BuildContext? context = null;
        using var harness = new Harness(Wrap(new Scaffold(
            body: Capture(c => context = c),
            floatingActionButtonAnimator: FloatingActionButtonAnimator.NoAnimation,
            floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { }))));
        harness.Pump(Viewport);

        Assert.Equal(
            new Rect(new Point(728, 528), new Size(56, 56)),
            RequireGeometry(context).FloatingActionButtonArea!.Value);
    }

    [Fact]
    public void PersistentFooter_SitsBelowTheBodyAndShrinksIt()
    {
        using var harness = new Harness(Wrap(new Scaffold(
            body: Filling(),
            persistentFooterButtons: [new SizedBox(width: 100, height: 90)])));
        harness.Pump(Viewport);

        RenderBox footer = harness.RequireSlot(ScaffoldSlot.PersistentFooter);
        RenderBox body = harness.RequireSlot(ScaffoldSlot.Body);

        // 90 plus the 8 top and bottom padding of the footer's EdgeInsets.all(8).
        Assert.Equal(106.0, footer.Size.Height);
        Assert.Equal(new Point(0, 494), SlotOffset(footer));
        Assert.Equal(494.0, body.Size.Height);
    }

    [Theory]
    [InlineData("CenterEnd", 692.0)]
    [InlineData("Center", 350.0)]
    [InlineData("CenterStart", 8.0)]
    public void PersistentFooter_AlignmentPlacesTheButtonRow(string alignment, double expectedLeft)
    {
        AlignmentDirectional resolved = alignment switch
        {
            "CenterEnd" => AlignmentDirectional.CenterEnd,
            "Center" => AlignmentDirectional.Center,
            _ => AlignmentDirectional.CenterStart,
        };
        using var harness = new Harness(Wrap(new Scaffold(
            body: new SizedBox(),
            persistentFooterAlignment: resolved,
            persistentFooterButtons: [new SizedBox(width: 100, height: 90)])));
        harness.Pump(Viewport);

        RenderBox button = harness.RequireRender<RenderConstrainedBox>(
            box => box.AdditionalConstraints.MaxWidth == 100.0);
        Assert.Equal(expectedLeft, button.GetPaintOffsetToRoot().X);
    }

    [Fact]
    public void PersistentFooter_AppliesMediaPadding()
    {
        using var harness = new Harness(Wrap(
            new Scaffold(
                body: new SizedBox(),
                persistentFooterButtons: [new SizedBox(width: 100, height: 90)]),
            mediaQuery: new MediaQueryData(
                Size: Viewport,
                Padding: new Thickness(10, 20, 30, 40))));
        harness.Pump(Viewport);

        RenderBox footer = harness.RequireSlot(ScaffoldSlot.PersistentFooter);
        RenderBox overflowBar = harness.RequireRender<RenderOverflowBar>();
        Point topLeft = overflowBar.GetPaintOffsetToRoot();

        // The footer's SafeArea keeps the left, right and bottom padding; only the top one is removed, so
        // the 106pt button row grows by the 40pt bottom inset and the buttons stay inside the safe area.
        Assert.Equal(146.0, footer.Size.Height);
        Assert.Equal(800.0 - 30.0 - 8.0, topLeft.X + overflowBar.Size.Width);
        Assert.Equal(600.0 - 40.0 - 8.0, topLeft.Y + overflowBar.Size.Height);
    }

    [Fact]
    public void PersistentFooter_WithBottomNavigationBar_DropsItsBottomSafeArea()
    {
        using var harness = new Harness(Wrap(
            new Scaffold(
                body: new SizedBox(),
                persistentFooterButtons: [new SizedBox(width: 100, height: 90)],
                bottomNavigationBar: new SizedBox(height: 60)),
            mediaQuery: new MediaQueryData(
                Size: Viewport,
                Padding: new Thickness(0, 0, 0, 40),
                ViewPadding: new Thickness(0, 0, 0, 40))));
        harness.Pump(Viewport);

        RenderBox footer = harness.RequireSlot(ScaffoldSlot.PersistentFooter);

        // removeBottomPadding: the bottom navigation bar already covers the safe area.
        Assert.Equal(106.0, footer.Size.Height);
        Assert.Equal(new Point(0, 600 - 60 - 106), SlotOffset(footer));
    }

    [Fact]
    public void PersistentFooter_KeepsItsBottomPaddingWhenViewInsetsAreIgnored()
    {
        var withoutKeyboard = new MediaQueryData(
            Size: Viewport,
            Padding: new Thickness(0, 0, 0, 20),
            ViewPadding: new Thickness(0, 0, 0, 20));
        using var harness = new Harness(Wrap(
            new Scaffold(
                body: new SizedBox(),
                resizeToAvoidBottomInset: false,
                persistentFooterButtons: [new SizedBox(width: 100, height: 90)]),
            mediaQuery: withoutKeyboard));
        harness.Pump(Viewport);
        double heightWithoutKeyboard = harness.RequireSlot(ScaffoldSlot.PersistentFooter).Size.Height;

        harness.Update(Wrap(
            new Scaffold(
                body: new SizedBox(),
                resizeToAvoidBottomInset: false,
                persistentFooterButtons: [new SizedBox(width: 100, height: 90)]),
            mediaQuery: withoutKeyboard with
            {
                Padding = default,
                ViewInsets = new Thickness(0, 0, 0, 300),
            }));
        harness.Pump(Viewport);

        Assert.Equal(heightWithoutKeyboard, harness.RequireSlot(ScaffoldSlot.PersistentFooter).Size.Height);
    }

    [Fact]
    public void PersistentFooter_DecorationReplacesTheDefaultDividerBorder()
    {
        using var defaultHarness = new Harness(Wrap(new Scaffold(
            body: new SizedBox(),
            persistentFooterButtons: [new SizedBox(width: 100, height: 90)])));
        defaultHarness.Pump(Viewport);

        var defaultDecoration = (BoxDecoration)defaultHarness
            .RequireWidget<Container>(container => container.Decoration is BoxDecoration { Border: not null })
            .Decoration!;
        Assert.Equal(
            ThemeData.Light.ColorScheme.OutlineVariant,
            ((Border)defaultDecoration.Border!).Top.Color);
        Assert.Equal(1.0, ((Border)defaultDecoration.Border!).Top.Width);

        var custom = new BoxDecoration(Color: Colors.Red);
        using var customHarness = new Harness(Wrap(new Scaffold(
            body: new SizedBox(),
            persistentFooterDecoration: custom,
            persistentFooterButtons: [new SizedBox(width: 100, height: 90)])));
        customHarness.Pump(Viewport);

        Assert.Same(
            custom,
            customHarness.RequireWidget<Container>(container => container.Decoration is BoxDecoration
            {
                Color: not null,
            }).Decoration);
    }

    [Fact]
    public void Body_WithExtendBody_RestoresTheBottomPaddingOfTheBottomWidgets()
    {
        MediaQueryData? bodyMetrics = null;
        using var harness = new Harness(Wrap(new Scaffold(
            extendBody: true,
            body: new Builder(context =>
            {
                bodyMetrics = MediaQuery.Of(context);
                return Filling();
            }),
            bottomNavigationBar: new SizedBox(height: 48))));
        harness.Pump(Viewport);

        Assert.Equal(new Size(800, 600), harness.RequireSlot(ScaffoldSlot.Body).Size);
        Assert.Equal(48.0, bodyMetrics!.Padding.Bottom);

        harness.Update(Wrap(new Scaffold(
            body: new Builder(context =>
            {
                bodyMetrics = MediaQuery.Of(context);
                return Filling();
            }),
            bottomNavigationBar: new SizedBox(height: 48))));
        harness.Pump(Viewport);

        Assert.Equal(new Size(800, 552), harness.RequireSlot(ScaffoldSlot.Body).Size);
        Assert.Equal(0.0, bodyMetrics!.Padding.Bottom);
    }

    [Theory]
    [InlineData(true, true, 600.0, 124.0)]
    [InlineData(true, false, 600.0, 24.0)]
    [InlineData(false, true, 476.0, 0.0)]
    [InlineData(false, false, 600.0, 24.0)]
    public void Body_WithExtendBodyBehindAppBar_RestoresTheAppBarTopPadding(
        bool extendBodyBehindAppBar,
        bool hasAppBar,
        double expectedBodyHeight,
        double expectedTopPadding)
    {
        MediaQueryData? bodyMetrics = null;
        using var harness = new Harness(Wrap(
            new Scaffold(
                extendBodyBehindAppBar: extendBodyBehindAppBar,
                appBar: hasAppBar
                    ? new AppBar(titleText: "Title", toolbarHeight: 100)
                    : null,
                body: new Builder(context =>
                {
                    bodyMetrics = MediaQuery.Of(context);
                    return Filling();
                })),
            mediaQuery: new MediaQueryData(Size: Viewport, Padding: new Thickness(0, 24, 0, 0))));
        harness.Pump(Viewport);

        Assert.Equal(expectedBodyHeight, harness.RequireSlot(ScaffoldSlot.Body).Size.Height);
        Assert.Equal(expectedTopPadding, bodyMetrics!.Padding.Top);
    }

    [Fact]
    public void Body_KeepsItsStateWhenExtendBodyBehindAppBarChanges()
    {
        var scaffoldKey = new LabeledGlobalKey<ScaffoldState>("scaffold");
        var controller = new ScrollController();
        Widget Build(bool extendBodyBehindAppBar) => Wrap(new Scaffold(
            key: scaffoldKey,
            extendBodyBehindAppBar: extendBodyBehindAppBar,
            appBar: new AppBar(titleText: "Title"),
            body: new ListView(
                controller: controller,
                children: [new SizedBox(height: 1200)])));

        using var harness = new Harness(Build(true));
        harness.Pump(Viewport);
        controller.JumpTo(100.0);
        harness.Pump(Viewport);

        harness.Update(Build(false));
        harness.Pump(Viewport);

        Assert.Equal(100.0, controller.Position.Pixels);
    }

    [Fact]
    public void Drawers_AreLaidOutTightAtTheOrigin()
    {
        using var harness = new Harness(Wrap(new Scaffold(
            body: new SizedBox(),
            drawer: new Drawer(),
            endDrawer: new Drawer())));
        harness.Pump(Viewport);

        foreach (ScaffoldSlot slot in new[] { ScaffoldSlot.Drawer, ScaffoldSlot.EndDrawer })
        {
            RenderBox drawer = harness.RequireSlot(slot);
            Assert.Equal(Viewport, drawer.Size);
            Assert.Equal(new Point(0, 0), SlotOffset(drawer));
        }
    }

    [Fact]
    public void Drawers_PaintTheOpenedEndDrawerAboveTheStartDrawer()
    {
        BuildContext? context = null;
        using var harness = new Harness(Wrap(new Scaffold(
            body: Capture(c => context = c),
            drawer: new Drawer(),
            endDrawer: new Drawer())));
        harness.Pump(Viewport);

        // Closed: the start drawer is appended last and therefore paints on top.
        Assert.True(harness.SlotIndex(ScaffoldSlot.Drawer) > harness.SlotIndex(ScaffoldSlot.EndDrawer));

        Scaffold.Of(context!.Value).OpenEndDrawer();
        harness.Pump(Viewport);

        Assert.True(harness.SlotIndex(ScaffoldSlot.EndDrawer) > harness.SlotIndex(ScaffoldSlot.Drawer));
    }

    [Fact]
    public void Drawer_DismissIntent_ClosesAnOpenDrawerUnlessTheBarrierIsLocked()
    {
        BuildContext? context = null;
        using var harness = new Harness(Wrap(new Scaffold(
            body: Capture(c => context = c),
            drawer: new Drawer())));
        harness.Pump(Viewport);

        ScaffoldState scaffold = Scaffold.Of(context!.Value);
        Assert.Null(Actions.Handler(context!.Value, new DismissIntent()));

        scaffold.OpenDrawer();
        harness.Pump(Viewport);

        Action? handler = Actions.Handler(context!.Value, new DismissIntent());
        Assert.NotNull(handler);
        handler!();
        harness.Pump(Viewport);

        Assert.False(scaffold.IsDrawerOpen);
    }

    [Fact]
    public void Drawer_DismissIntent_IsDisabledWhenTheBarrierIsNotDismissible()
    {
        BuildContext? context = null;
        using var harness = new Harness(Wrap(new Scaffold(
            body: Capture(c => context = c),
            drawerBarrierDismissible: false,
            drawer: new Drawer())));
        harness.Pump(Viewport);

        Scaffold.Of(context!.Value).OpenDrawer();
        harness.Pump(Viewport);

        Assert.Null(Actions.Handler(context!.Value, new DismissIntent()));
    }

    [Fact]
    public void Scaffold_OnDrawerChanged_FiresOnlyOnAChange()
    {
        var changes = new List<bool>();
        BuildContext? context = null;
        using var harness = new Harness(Wrap(new Scaffold(
            body: Capture(c => context = c),
            onDrawerChanged: changes.Add,
            drawer: new Drawer())));
        harness.Pump(Viewport);

        ScaffoldState scaffold = Scaffold.Of(context!.Value);
        scaffold.OpenDrawer();
        harness.Pump(Viewport);
        scaffold.OpenDrawer();
        harness.Pump(Viewport);
        scaffold.CloseDrawer();
        harness.Pump(Viewport);

        Assert.Equal([true, false], changes);
    }

    [Theory]
    [InlineData(TargetPlatform.IOS, true)]
    [InlineData(TargetPlatform.MacOS, true)]
    [InlineData(TargetPlatform.Android, false)]
    [InlineData(TargetPlatform.Windows, false)]
    public void StatusBar_SlotIsInstalledOnAppleHostsOnly(TargetPlatform platform, bool hasStatusBar)
    {
        using var harness = new Harness(Wrap(
            new Scaffold(body: new SizedBox()),
            theme: ThemeData.Light with { Platform = platform }));
        harness.Pump(Viewport);

        Assert.Equal(hasStatusBar, harness.HasSlot(ScaffoldSlot.StatusBar));
    }

    [Fact]
    public void StatusBar_SlotIsNotInstalledWhenPrimaryIsFalse()
    {
        using var harness = new Harness(Wrap(
            new Scaffold(body: new SizedBox(), primary: false),
            theme: ThemeData.Light with { Platform = TargetPlatform.IOS }));
        harness.Pump(Viewport);

        Assert.False(harness.HasSlot(ScaffoldSlot.StatusBar));
    }

    [Fact]
    public void StatusBar_SlotCoversTheTopPaddingAtTheOrigin()
    {
        using var harness = new Harness(Wrap(
            new Scaffold(body: new SizedBox()),
            mediaQuery: new MediaQueryData(Size: Viewport, Padding: new Thickness(0, 25, 0, 0)),
            theme: ThemeData.Light with { Platform = TargetPlatform.IOS }));
        harness.Pump(Viewport);

        RenderBox statusBar = harness.RequireSlot(ScaffoldSlot.StatusBar);
        Assert.Equal(new Size(800, 25), statusBar.Size);
        Assert.Equal(new Point(0, 0), SlotOffset(statusBar));
    }

    [Fact]
    public void StatusBarTap_AnimatesThePrimaryScrollableBackToTheTop()
    {
        var controller = new ScrollController();
        using var harness = new Harness(Wrap(
            new PrimaryScrollController(
                controller: controller,
                child: new Scaffold(
                    body: new ListView(controller: controller, children: [new SizedBox(height: 2400)]))),
            mediaQuery: new MediaQueryData(Size: Viewport, Padding: new Thickness(0, 25, 0, 0)),
            theme: ThemeData.Light with { Platform = TargetPlatform.IOS }));
        harness.Pump(Viewport);
        Assert.True(controller.HasClients);
        controller.JumpTo(1000.0);
        harness.Pump(Viewport);

        WidgetsBinding.Instance.HandleStatusBarTap();
        double start = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(start));

        // The source animation runs for 1000ms on Curves.easeOutCirc; these are the offsets Flutter's own
        // status-bar test samples.
        Assert.InRange(PixelsAt(0.25), 156.0, 160.0);
        Assert.InRange(PixelsAt(0.5), 39.0, 43.0);
        Assert.InRange(PixelsAt(0.75), 5.0, 9.0);
        // Past the end of the 1000ms run the activity reports the exact target.
        Assert.Equal(0.0, PixelsAt(1.1));

        double PixelsAt(double offset)
        {
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(start + offset));
            harness.Pump(Viewport);
            return controller.Position.Pixels;
        }
    }

    [Fact]
    public void StatusBarTap_OnlyScrollsTheForegroundScaffold()
    {
        var background = new ScrollController();
        var foreground = new ScrollController();
        using var harness = new Harness(Wrap(
            new Stack(
                children:
                [
                    new PrimaryScrollController(
                        controller: background,
                        child: new Scaffold(body: new ListView(
                            controller: background,
                            children: [new SizedBox(height: 2400)]))),
                    new PrimaryScrollController(
                        controller: foreground,
                        child: new Scaffold(body: new ListView(
                            controller: foreground,
                            children: [new SizedBox(height: 2400)]))),
                ]),
            mediaQuery: new MediaQueryData(Size: Viewport, Padding: new Thickness(0, 25, 0, 0)),
            theme: ThemeData.Light with { Platform = TargetPlatform.IOS }));
        harness.Pump(Viewport);
        background.JumpTo(1000.0);
        foreground.JumpTo(1000.0);
        harness.Pump(Viewport);

        WidgetsBinding.Instance.HandleStatusBarTap();
        harness.Tick(1.1);
        harness.Pump(Viewport);

        Assert.Equal(1000.0, background.Position.Pixels);
        Assert.Equal(0.0, foreground.Position.Pixels);
    }

    /// <summary>A body that expands to whatever the scaffold's body slot offers it.</summary>
    private static Widget Filling() =>
        new SizedBox(width: double.PositiveInfinity, height: double.PositiveInfinity);

    private static Point SlotOffset(RenderBox slot) =>
        ((MultiChildLayoutParentData)slot.parentData!).offset;

    private static ScaffoldGeometry RequireGeometry(BuildContext? context)
    {
        Assert.True(context.HasValue);
        ScaffoldGeometryNotifier notifier = Scaffold.GeometryNotifierMaybeOf(context!.Value)!;
        Assert.NotNull(notifier);
        return notifier.ValueForLayout;
    }

    private static Widget Capture(Action<BuildContext> capture) => new Builder(context =>
    {
        capture(context);
        return new SizedBox();
    });

    private static Widget Wrap(
        Widget child,
        MediaQueryData? mediaQuery = null,
        ThemeData? theme = null,
        TextDirection textDirection = TextDirection.Ltr) =>
        new Directionality(
            textDirection,
            new MediaQuery(
                mediaQuery ?? new MediaQueryData(Size: Viewport),
                new Theme(theme ?? AndroidLight, child)));

    /// <summary>
    /// The default theme for these tests. The platform is pinned because iOS and macOS scaffolds install an
    /// extra status-bar slot, which would otherwise make slot assertions depend on the test host.
    /// </summary>
    private static ThemeData AndroidLight => ThemeData.Light with { Platform = TargetPlatform.Android };

    private sealed class Harness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _rootElement;
        private readonly PipelineOwner _pipeline;

        public Harness(Widget rootWidget)
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

        public void Update(Widget widget)
        {
            _rootElement.Update(widget);
            _owner.FlushBuild();
        }

        public void Tick(double seconds)
        {
            AnimationPump.Advance(seconds);
        }

        /// <summary>Runs the 200ms entrance and the 400ms segue to completion.</summary>
        public void SettleFloatingActionButton()
        {
            Tick(0.05);
            Pump(Viewport);
            Tick(1.0);
            Pump(Viewport);
        }

        public RenderCustomMultiChildLayoutBox RequireLayout()
        {
            var found = new List<RenderCustomMultiChildLayoutBox>();
            Collect(RenderView, found);
            return found.First(layout => layout.Size == Viewport);
        }

        public RenderBox RequireSlot(ScaffoldSlot slot)
        {
            RenderCustomMultiChildLayoutBox layout = RequireLayout();
            for (RenderBox? child = layout.FirstChild; child is not null; child = layout.ChildAfter(child))
            {
                if (Equals(((MultiChildLayoutParentData)child.parentData!).Id, slot))
                {
                    return child;
                }
            }

            throw new InvalidOperationException($"Scaffold slot '{slot}' was not found.");
        }

        public bool HasSlot(ScaffoldSlot slot) => SlotIndex(slot) >= 0;

        /// <summary>The slot's position in the layout's child list, which is also its paint order.</summary>
        public int SlotIndex(ScaffoldSlot slot)
        {
            RenderCustomMultiChildLayoutBox layout = RequireLayout();
            int index = 0;
            for (RenderBox? child = layout.FirstChild; child is not null; child = layout.ChildAfter(child))
            {
                if (Equals(((MultiChildLayoutParentData)child.parentData!).Id, slot))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        public T RequireRender<T>(Func<T, bool>? predicate = null) where T : RenderObject
        {
            var found = new List<T>();
            Collect(RenderView, found);
            return found.First(render => predicate is null || predicate(render));
        }

        public T RequireWidget<T>(Func<T, bool>? predicate = null) where T : Widget
        {
            var found = new List<T>();
            CollectWidgets(_rootElement, found);
            return found.First(widget => predicate is null || predicate(widget));
        }

        public T FindState<T>() where T : State
        {
            var states = new List<T>();
            CollectStates(_rootElement, states);
            return states.First();
        }

        public void Dispose() => _rootElement.Unmount();

        private static void Collect<T>(RenderObject? root, List<T> found) where T : RenderObject
        {
            if (root is null)
            {
                return;
            }

            if (root is T match)
            {
                found.Add(match);
            }

            root.VisitChildren(child => Collect(child, found));
        }

        private static void CollectWidgets<T>(Element element, List<T> widgets) where T : Widget
        {
            if (element.Widget is T typed)
            {
                widgets.Add(typed);
            }

            element.VisitChildren(child => CollectWidgets(child, widgets));
        }

        private static void CollectStates<T>(Element element, List<T> states) where T : State
        {
            if (element is StatefulElement stateful && stateful.State is T state)
            {
                states.Add(state);
            }

            element.VisitChildren(child => CollectStates(child, states));
        }

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;

            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget) =>
                _renderView = renderView;

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
                if (_child is not null)
                {
                    visitor(_child);
                }
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot) =>
                _renderView.Child = (RenderBox)child;

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (ReferenceEquals(_renderView.Child, child))
                {
                    _renderView.Child = null;
                }
            }

            internal override void Unmount()
            {
                if (_child is not null)
                {
                    UnmountChild(_child);
                    _child = null;
                }

                base.Unmount();
            }
        }
    }
}
