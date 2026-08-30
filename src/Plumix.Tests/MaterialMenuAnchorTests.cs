using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class MaterialMenuAnchorTests
{
    [DebugOnlyFact]
    public void MenuThemeTypes_DebugFillProperties_ListEveryFieldDartDoes()
    {
        var styleProperties = new DiagnosticPropertiesBuilder();
        new MenuStyle().DebugFillProperties(styleProperties);
        Assert.Equal(
            [
                "backgroundColor",
                "shadowColor",
                "surfaceTintColor",
                "elevation",
                "padding",
                "minimumSize",
                "fixedSize",
                "maximumSize",
                "side",
                "shape",
                "mouseCursor",
                "visualDensity",
                "alignment",
            ],
            styleProperties.Properties.Select(property => property.Name).ToList());

        var menuProperties = new DiagnosticPropertiesBuilder();
        new MenuThemeData().DebugFillProperties(menuProperties);
        Assert.Equal(["style", "submenuIcon"], menuProperties.Properties.Select(p => p.Name).ToList());

        // `MenuBarThemeData` extends `MenuThemeData` in Dart too and adds no properties of its own.
        var barProperties = new DiagnosticPropertiesBuilder();
        new MenuBarThemeData().DebugFillProperties(barProperties);
        Assert.Equal(["style", "submenuIcon"], barProperties.Properties.Select(p => p.Name).ToList());

        var buttonProperties = new DiagnosticPropertiesBuilder();
        new MenuButtonThemeData().DebugFillProperties(buttonProperties);
        Assert.Equal(["style"], buttonProperties.Properties.Select(property => property.Name).ToList());

        // A default instance elides every property, exactly as Dart's `defaultValue: null` does.
        Assert.Empty(styleProperties.Properties.Where(property => property.Value is not null));
        Assert.Empty(menuProperties.Properties.Where(property => property.Value is not null));
        Assert.Empty(buttonProperties.Properties.Where(property => property.Value is not null));
    }

    [Fact]
    public void MenuMouseCursor_FallsBackToUncontrolledWhenNoStyleSuppliesACursor()
    {
        // Dart's `_MouseCursor`: the resolved menu cursor, or `MouseCursor.uncontrolled`.
        var empty = new HashSet<WidgetState>();
        var withoutCursor = new MenuMouseCursor(_ => null);
        var withCursor = new MenuMouseCursor(_ => SystemMouseCursors.Click);

        Assert.Equal(MouseCursor.Uncontrolled, withoutCursor.Resolve(empty));
        Assert.Equal(SystemMouseCursors.Click, withCursor.Resolve(empty));
        Assert.NotEqual(MouseCursor.Defer, MouseCursor.Uncontrolled);
    }

    [Fact]
    public void MouseCursorUncontrolled_BlocksTheRegionBehindWithoutChangingTheCursor()
    {
        MouseCursorManager.ResetForTests();
        try
        {
            using IDisposable text = MouseCursorManager.PushCursor(SystemMouseCursors.Text);
            Assert.Equal(SystemMouseCursors.Text, MouseCursorManager.CurrentCursor);

            // The uncontrolled region keeps the cursor it entered with, and the click request under
            // it cannot take over while it is on top.
            using IDisposable uncontrolled = MouseCursorManager.PushCursor(MouseCursor.Uncontrolled);
            Assert.Equal(SystemMouseCursors.Text, MouseCursorManager.CurrentCursor);

            uncontrolled.Dispose();
            Assert.Equal(SystemMouseCursors.Text, MouseCursorManager.CurrentCursor);
        }
        finally
        {
            MouseCursorManager.ResetForTests();
        }
    }

    [Fact]
    public void MenuController_UnattachedOperationsMatchFlutterContract()
    {
        var controller = new MenuController();

        Assert.False(controller.IsOpen);
        controller.Close();
        Assert.Throws<InvalidOperationException>(() => controller.Open(new Vector(12.0, 24.0)));
        Assert.Throws<InvalidOperationException>(controller.CloseChildren);
    }

    [Fact]
    public void MenuAnchor_ExposesFlutterDefaults()
    {
        var anchor = new MenuAnchor(
            menuChildren: [],
            child: new SizedBox());

        Assert.Equal(default, anchor.AlignmentOffset);
        Assert.Null(anchor.ReservedPadding);
        Assert.Equal(Clip.HardEdge, anchor.ClipBehavior);
        Assert.False(anchor.ConsumeOutsideTap);
        Assert.True(anchor.CrossAxisUnconstrained);
        Assert.False(anchor.UseRootOverlay);
        Assert.False(anchor.Animated);
    }

    [Fact]
    public void MenuLayout_ReservedPaddingDeflatesTheChildConstraints()
    {
        // Flutter: "Menu panel default reserved padding" / "Menu panel accepts custom reserved padding".
        BoxConstraints defaultPadding = Layout(
            anchorRect: AnchorRect,
            reservedPadding: EdgeInsetsGeometry.All(8.0))
            .GetConstraintsForChild(BoxConstraints.Tight(new Size(800.0, 600.0)));
        BoxConstraints customPadding = Layout(
            anchorRect: AnchorRect,
            reservedPadding: EdgeInsetsGeometry.Symmetric(horizontal: 13.0))
            .GetConstraintsForChild(BoxConstraints.Tight(new Size(800.0, 600.0)));

        Assert.Equal(0.0, defaultPadding.MinWidth);
        Assert.Equal(800.0 - 16.0, defaultPadding.MaxWidth);
        Assert.Equal(600.0 - 16.0, defaultPadding.MaxHeight);
        Assert.Equal(800.0 - 26.0, customPadding.MaxWidth);
        Assert.Equal(600.0, customPadding.MaxHeight);
    }

    [Theory]
    // Flutter's "menu alignment and offset in LTR": anchor (328, 14, 472, 62), menu 274 x 112.
    [InlineData(-1.0, 1.0, 328.0, 62.0)]
    [InlineData(-1.0, -1.0, 328.0, 14.0)]
    [InlineData(0.0, 0.0, 400.0, 38.0)]
    [InlineData(1.0, 1.0, 472.0, 62.0)]
    public void MenuLayout_ResolvesDirectionalAlignmentWithinTheAnchorRectInLtr(
        double start,
        double y,
        double expectedX,
        double expectedY)
    {
        MenuLayout layout = Layout(
            anchorRect: AnchorRect,
            alignment: new AlignmentDirectional(start, y));

        Assert.Equal(
            new Point(expectedX, expectedY),
            layout.GetPositionForChild(OverlaySize, MenuSize));
    }

    [Theory]
    // Flutter's "menu alignment and offset in RTL": the same anchor mirrors and anchors the right edge.
    [InlineData(-1.0, 1.0, 198.0, 62.0)]
    [InlineData(-1.0, -1.0, 198.0, 14.0)]
    [InlineData(0.0, 0.0, 126.0, 38.0)]
    [InlineData(1.0, 1.0, 54.0, 62.0)]
    public void MenuLayout_ResolvesDirectionalAlignmentWithinTheAnchorRectInRtl(
        double start,
        double y,
        double expectedX,
        double expectedY)
    {
        MenuLayout layout = Layout(
            anchorRect: AnchorRect,
            alignment: new AlignmentDirectional(start, y),
            textDirection: TextDirection.Rtl);

        Assert.Equal(
            new Point(expectedX, expectedY),
            layout.GetPositionForChild(OverlaySize, MenuSize));
    }

    [Fact]
    public void MenuLayout_MirrorsTheAlignmentOffsetOnlyForDirectionalAlignments()
    {
        Point ltr = Layout(
            anchorRect: AnchorRect,
            alignment: AlignmentDirectional.TopStart,
            alignmentOffset: new Vector(10.0, 20.0))
            .GetPositionForChild(OverlaySize, MenuSize);
        Point rtl = Layout(
            anchorRect: AnchorRect,
            alignment: AlignmentDirectional.TopStart,
            alignmentOffset: new Vector(10.0, 20.0),
            textDirection: TextDirection.Rtl)
            .GetPositionForChild(OverlaySize, MenuSize);
        Point rtlPhysical = Layout(
            anchorRect: AnchorRect,
            alignment: Alignment.TopLeft,
            alignmentOffset: new Vector(10.0, 20.0),
            textDirection: TextDirection.Rtl)
            .GetPositionForChild(OverlaySize, MenuSize);

        // Flutter asserts deltas of (10, 20) in LTR and (-10, 20) in RTL against the un-offset menu.
        Assert.Equal(new Point(338.0, 34.0), ltr);
        Assert.Equal(new Point(188.0, 34.0), rtl);

        // A plain `Alignment` never mirrors dx. `Alignment.topLeft` resolves to x = 328 in both
        // directions, and RTL still anchors the menu's right edge.
        Assert.Equal(new Point(328.0 + 10.0 - 274.0, 34.0), rtlPhysical);
    }

    [Fact]
    public void MenuLayout_ExplicitPositionIsAnchorRelativeAndIgnoresAlignment()
    {
        // Flutter's "menu position in LTR"/"in RTL": both land on the same clamped rect.
        Point ltr = Layout(
            anchorRect: AnchorRect,
            alignmentOffset: new Vector(100.0, 50.0),
            menuPosition: new Vector(200.0, 200.0))
            .GetPositionForChild(OverlaySize, MenuSize);
        Point rtl = Layout(
            anchorRect: AnchorRect,
            alignmentOffset: new Vector(100.0, 50.0),
            menuPosition: new Vector(400.0, 200.0),
            textDirection: TextDirection.Rtl)
            .GetPositionForChild(OverlaySize, MenuSize);
        Point offsetOnly = Layout(
            anchorRect: AnchorRect,
            alignmentOffset: new Vector(100.0, 50.0))
            .GetPositionForChild(OverlaySize, MenuSize);

        Assert.Equal(new Point(526.0, 214.0), ltr);
        Assert.Equal(new Point(526.0, 214.0), rtl);
        Assert.Equal(new Point(428.0, 112.0), offsetOnly);
    }

    [Fact]
    public void MenuLayout_FlipsAboveTheAnchorAndSubtractsTheOffsetUnderAHorizontalParent()
    {
        // Flutter's "vertically constrained menus are positioned above the anchor by default" and
        // "…with the provided offset": a 122 x 64 menu under a bottom-aligned 552..600 anchor.
        var anchorRect = new Rect(0.0, 552.0, 116.0, 48.0);
        var childSize = new Size(122.0, 64.0);

        Point noOffset = Layout(anchorRect: anchorRect)
            .GetPositionForChild(OverlaySize, childSize);
        Point withOffset = Layout(anchorRect: anchorRect, alignmentOffset: new Vector(0.0, 50.0))
            .GetPositionForChild(OverlaySize, childSize);

        Assert.Equal(new Point(0.0, 488.0), noOffset);
        Assert.Equal(new Point(0.0, 438.0), withOffset);
    }

    [Fact]
    public void MenuLayout_CascadingSubmenuFlipsToTheOtherSideOfItsAnchor()
    {
        // A submenu of a vertical menu has parentOrientation == orientation, so an overflowing menu
        // flips across the anchor instead of being clamped to the screen edge.
        MenuLayout Cascading(Rect anchorRect) => new(
            anchorRect: anchorRect,
            textDirection: TextDirection.Ltr,
            alignment: AlignmentDirectional.TopEnd,
            alignmentOffset: new Vector(0.0, -8.0),
            menuPosition: null,
            menuPadding: EdgeInsetsGeometry.Zero,
            orientation: Axis.Vertical,
            parentOrientation: Axis.Vertical,
            reservedPadding: EdgeInsetsGeometry.Zero,
            avoidBounds: null,
            heightFactor: 1.0);

        Point fits = Cascading(new Rect(120.0, 40.0, 80.0, 40.0))
            .GetPositionForChild(new Size(500.0, 360.0), new Size(100.0, 120.0));
        Point flipped = Cascading(new Rect(120.0, 40.0, 80.0, 40.0))
            .GetPositionForChild(new Size(260.0, 360.0), new Size(100.0, 120.0));
        Point clamped = Cascading(new Rect(20.0, 40.0, 80.0, 40.0))
            .GetPositionForChild(new Size(150.0, 360.0), new Size(100.0, 120.0));

        Assert.Equal(new Point(200.0, 32.0), fits);
        Assert.Equal(new Point(20.0, 32.0), flipped);
        Assert.Equal(new Point(50.0, 32.0), clamped);
    }

    [Fact]
    public void MenuLayout_ClampsToTheScreenEdgeWhenTheParentOrientationDiffers()
    {
        // A menu bar's submenu (parent horizontal, menu vertical) is clamped, never flipped.
        Point position = Layout(anchorRect: new Rect(700.0, 0.0, 80.0, 48.0))
            .GetPositionForChild(OverlaySize, MenuSize);

        Assert.Equal(new Point(800.0 - 274.0, 48.0), position);
    }

    [Fact]
    public void MenuLayout_AvoidsTheSoftwareKeyboardAndTheViewPadding()
    {
        // Flutter's "menu is positioned to avoid the software keyboard" (flutter/flutter#142921).
        var overlaySize = new Size(600.0, 800.0);
        MenuLayout layout = Layout(
            anchorRect: new Rect(0.0, 500.0, 100.0, 60.0),
            viewInsets: new Thickness(0.0, 0.0, 0.0, 200.0));

        Point position = layout.GetPositionForChild(overlaySize, new Size(200.0, 100.0));

        Assert.Equal(new Point(0.0, 400.0), position);
        Assert.True(position.Y + 100.0 <= overlaySize.Height - 200.0);
    }

    [Fact]
    public void MenuLayout_SelectsTheDisplayFeatureSubScreenClosestToTheAnchor()
    {
        // A vertical hinge splits the 500-wide overlay into 0..246 and 254..500; the anchor sits in
        // the right sub-screen, and a menu wider than it is pinned to that sub-screen's left edge.
        MenuLayout layout = Layout(
            anchorRect: new Rect(360.0, 40.0, 80.0, 40.0),
            avoidBounds: [new Rect(246.0, 0.0, 8.0, 360.0)]);

        Point wide = layout.GetPositionForChild(new Size(500.0, 360.0), new Size(300.0, 100.0));
        Point narrow = layout.GetPositionForChild(new Size(500.0, 360.0), new Size(120.0, 100.0));

        Assert.Equal(new Point(254.0, 80.0), wide);
        Assert.Equal(new Point(360.0, 80.0), narrow);
    }

    [Fact]
    public void MenuAnimationCurvesMatchOpeningOpacityAndHeightCheckpoints()
    {
        Curve opacity = MenuConstants.PanelOpacityForwardCurve;
        Curve height = MenuConstants.PanelHeightForwardCurve;

        Assert.Equal(0.2, opacity(0.02), 6);
        Assert.Equal(1.0, opacity(0.1), 6);
        Assert.InRange(height(0.2) * 160.0, 59.5, 61.5);
        Assert.InRange(height(0.8) * 160.0, 156.5, 158.5);
    }

    [Fact]
    public void MenuPanelClosingCurvesMatchTheSourceFadeOutAndHeightEaseIn()
    {
        Curve opacity = MenuConstants.PanelOpacityReverseCurve;
        Curve height = MenuConstants.PanelHeightReverseCurve;

        // Curves take the controller value, which runs 1 -> 0 while closing. The panel keeps full
        // opacity for the first 100 of 150 ms (value 1 -> 1/3) and fades over the last 50 ms.
        Assert.Equal(1.0, opacity(1.0), 6);
        Assert.Equal(1.0, opacity(1.0 / 3.0), 6);
        Assert.Equal(0.5, opacity(1.0 / 6.0), 6);
        Assert.Equal(0.0, opacity(0.0), 6);

        // `_TweenCurve(0.35, 1)` remaps the output range, so a full reverse still lands on zero.
        Assert.Equal(1.0, height(1.0), 6);
        Assert.Equal(0.35, height(0.0), 6);
        Assert.InRange(height(50.0 / 150.0) * 160.0, 60.0, 130.0);
    }

    [Fact]
    public void MenuTraversalShortcutsMatchTheSourceEightEntryMap()
    {
        IReadOnlyDictionary<ShortcutActivator, Intent> shortcuts = MenuConstants.TraversalShortcuts;

        Assert.Equal(8, shortcuts.Count);
        Assert.IsType<ActivateIntent>(shortcuts[new SingleActivator(LogicalKeyboardKey.GameButtonA)]);
        Assert.IsType<DismissIntent>(shortcuts[new SingleActivator(LogicalKeyboardKey.Escape)]);
        Assert.IsType<NextFocusIntent>(shortcuts[new SingleActivator(LogicalKeyboardKey.Tab)]);
        Assert.IsType<PreviousFocusIntent>(shortcuts[new SingleActivator(LogicalKeyboardKey.Tab, shift: true)]);
        Assert.Equal(
            TraversalDirection.Down,
            ((DirectionalFocusIntent)shortcuts[new SingleActivator(LogicalKeyboardKey.ArrowDown)]).Direction);
        Assert.Equal(
            TraversalDirection.Up,
            ((DirectionalFocusIntent)shortcuts[new SingleActivator(LogicalKeyboardKey.ArrowUp)]).Direction);
        Assert.Equal(
            TraversalDirection.Left,
            ((DirectionalFocusIntent)shortcuts[new SingleActivator(LogicalKeyboardKey.ArrowLeft)]).Direction);
        Assert.Equal(
            TraversalDirection.Right,
            ((DirectionalFocusIntent)shortcuts[new SingleActivator(LogicalKeyboardKey.ArrowRight)]).Direction);
    }

    [Fact]
    public void MenuItemFadeCurvesStaggerOverTheSourceIntervals()
    {
        // Flutter asserts item opacities of [0.400, 0.0667, 0, 0] at t = 100 ms of a 500 ms open.
        double[] expectedAt100Ms = [0.4, 1.0 / 15.0, 0.0, 0.0];
        for (int index = 0; index < 4; index++)
        {
            (Curve forward, _) = MenuConstants.ItemFadeCurves(index, 4);
            Assert.Equal(expectedAt100Ms[index], forward(0.2), 3);
        }

        // A single item spans the first half of the opening and the first third of the closing.
        (Curve onlyForward, Curve onlyReverse) = MenuConstants.ItemFadeCurves(0, 1);
        Assert.Equal(1.0, onlyForward(0.5), 6);
        Assert.Equal(1.0, onlyReverse(1.0 / 3.0), 6);

        // The last of four items finishes exactly at the end of the opening animation.
        (Curve lastForward, Curve lastReverse) = MenuConstants.ItemFadeCurves(3, 4);
        Assert.Equal(0.0, lastForward(0.5), 6);
        Assert.Equal(1.0, lastForward(1.0), 6);
        Assert.Equal(1.0, lastReverse(2.0 / 3.0), 6);
    }

    [Fact]
    public void MenuStylePaddingIsDirectionalGeometryResolvedAgainstTextDirection()
    {
        var style = new MenuStyle(
            padding: MaterialStateProperty<EdgeInsetsGeometry?>.All(
                EdgeInsetsGeometry.DirectionalOnly(start: 10, top: 12, end: 11, bottom: 13)));

        EdgeInsetsGeometry padding = style.Padding!.Resolve(MaterialState.None)!.Value;

        Assert.Equal(new Thickness(10, 12, 11, 13), padding.Resolve(TextDirection.Ltr));
        Assert.Equal(new Thickness(11, 12, 10, 13), padding.Resolve(TextDirection.Rtl));
    }

    [Fact]
    public void MenuLayout_LerpsADownwardGrowingPanelFromTheAnchorsBottomEdge()
    {
        MenuLayout Growing(double heightFactor) => Layout(
            anchorRect: AnchorRect,
            alignment: AlignmentDirectional.TopStart,
            heightFactor: heightFactor);

        Point settled = Growing(1.0).GetPositionForChild(OverlaySize, MenuSize);
        Point half = Growing(0.5).GetPositionForChild(OverlaySize, new Size(274.0, 56.0));

        Assert.Equal(new Point(328.0, 14.0), settled);
        Assert.Equal(new Point(328.0, 38.0), half);
    }

    [Fact]
    public void MenuLayout_KeepsAnUpwardGrowingPanelPinnedToItsBottomEdge()
    {
        MenuLayout Growing(double heightFactor) => Layout(
            anchorRect: new Rect(0.0, 552.0, 116.0, 48.0),
            heightFactor: heightFactor);

        Point settled = Growing(1.0).GetPositionForChild(OverlaySize, new Size(122.0, 64.0));
        Point half = Growing(0.5).GetPositionForChild(OverlaySize, new Size(122.0, 32.0));

        Assert.Equal(new Point(0.0, 488.0), settled);
        Assert.Equal(new Point(0.0, 520.0), half);
    }

    [Fact]
    public void MenuLayout_PositionedMenusNeverAnimateTheirOrigin()
    {
        // Flutter's "Positioned menus always begin animating at the target position".
        MenuLayout Growing(double heightFactor) => Layout(
            anchorRect: AnchorRect,
            menuPosition: new Vector(20.0, 30.0),
            heightFactor: heightFactor);

        Assert.Equal(
            new Point(348.0, 44.0),
            Growing(0.02).GetPositionForChild(OverlaySize, new Size(274.0, 2.0)));
        Assert.Equal(
            new Point(348.0, 44.0),
            Growing(1.0).GetPositionForChild(OverlaySize, MenuSize));
    }

    [Fact]
    public void MenuLayout_ShouldRelayoutTracksEveryPositioningInput()
    {
        MenuLayout baseline = Layout(anchorRect: AnchorRect);

        Assert.False(baseline.ShouldRelayout(Layout(anchorRect: AnchorRect)));
        Assert.True(baseline.ShouldRelayout(Layout(anchorRect: new Rect(0.0, 0.0, 10.0, 10.0))));
        Assert.True(baseline.ShouldRelayout(
            Layout(anchorRect: AnchorRect, textDirection: TextDirection.Rtl)));
        Assert.True(baseline.ShouldRelayout(
            Layout(anchorRect: AnchorRect, alignment: AlignmentDirectional.TopEnd)));
        Assert.True(baseline.ShouldRelayout(
            Layout(anchorRect: AnchorRect, alignmentOffset: new Vector(1.0, 0.0))));
        Assert.True(baseline.ShouldRelayout(
            Layout(anchorRect: AnchorRect, menuPosition: new Vector(1.0, 1.0))));
        Assert.True(baseline.ShouldRelayout(
            Layout(anchorRect: AnchorRect, menuPadding: EdgeInsetsGeometry.All(4.0))));
        Assert.True(baseline.ShouldRelayout(
            Layout(anchorRect: AnchorRect, parentOrientation: Axis.Vertical)));
        Assert.True(baseline.ShouldRelayout(
            Layout(anchorRect: AnchorRect, reservedPadding: EdgeInsetsGeometry.All(4.0))));
        Assert.True(baseline.ShouldRelayout(Layout(anchorRect: AnchorRect, heightFactor: 0.5)));
        Assert.True(baseline.ShouldRelayout(
            Layout(anchorRect: AnchorRect, avoidBounds: [new Rect(0.0, 0.0, 1.0, 1.0)])));
    }

    // ---- MenuStyle and the menu theme datas ----

    [Fact]
    public void MenuStyle_CopyWithKeepsUnspecifiedFieldsAndMergeLetsTheReceiverWin()
    {
        var baseStyle = new MenuStyle(
            backgroundColor: MaterialStateProperty<Color?>.All(Colors.Red),
            elevation: MaterialStateProperty<double?>.All(3.0));
        var other = new MenuStyle(
            backgroundColor: MaterialStateProperty<Color?>.All(Colors.Green),
            shadowColor: MaterialStateProperty<Color?>.All(Colors.Blue),
            alignment: AlignmentDirectional.TopEnd);

        MenuStyle copied = baseStyle.CopyWith(elevation: MaterialStateProperty<double?>.All(9.0));
        MenuStyle merged = baseStyle.Merge(other);

        // `copyWith` replaces only what it is given; every other field is carried over.
        Assert.Equal(Colors.Red, copied.BackgroundColor!.Resolve(MaterialState.None));
        Assert.Equal(9.0, copied.Elevation!.Resolve(MaterialState.None));

        // `merge` fills this style's null fields from the argument; non-null receiver fields win.
        Assert.Equal(Colors.Red, merged.BackgroundColor!.Resolve(MaterialState.None));
        Assert.Equal(Colors.Blue, merged.ShadowColor!.Resolve(MaterialState.None));
        Assert.Equal(3.0, merged.Elevation!.Resolve(MaterialState.None));
        Assert.Equal((AlignmentGeometry)AlignmentDirectional.TopEnd, merged.Alignment);
        Assert.Same(baseStyle, baseStyle.Merge(null));
    }

    [Fact]
    public void MenuStyle_EqualityComparesEveryFieldUnderTheSourceRuntimeTypeGuard()
    {
        var left = new MenuStyle(elevation: MaterialStateProperty<double?>.All(3.0));
        var right = new MenuStyle(elevation: MaterialStateProperty<double?>.All(3.0));

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
        Assert.NotEqual(left, new MenuStyle(elevation: MaterialStateProperty<double?>.All(4.0)));
        Assert.NotEqual(left, new MenuStyle());
    }

    [Fact]
    public void MenuStyle_LerpMatchesTheSourceSpecialCases()
    {
        // Flutter's "MenuStyle lerp special cases".
        var data = new MenuStyle(elevation: MaterialStateProperty<double?>.All(3.0));

        Assert.Null(MenuStyle.Lerp(null, null, 0.0));
        Assert.Same(data, MenuStyle.Lerp(data, data, 0.5));
    }

    [Fact]
    public void MenuStyle_LerpUsesDiscreteSwitchesForCursorDensityAndContinuousColors()
    {
        var a = new MenuStyle(
            backgroundColor: MaterialStateProperty<Color?>.All(Color.FromArgb(255, 0, 0, 0)),
            mouseCursor: MaterialStateProperty<MouseCursor?>.All(SystemMouseCursors.Basic),
            visualDensity: VisualDensity.Standard);
        var b = new MenuStyle(
            backgroundColor: MaterialStateProperty<Color?>.All(Color.FromArgb(255, 255, 255, 255)),
            mouseCursor: MaterialStateProperty<MouseCursor?>.All(SystemMouseCursors.Click),
            visualDensity: VisualDensity.Compact);

        MenuStyle mid = MenuStyle.Lerp(a, b, 0.5)!;
        MenuStyle late = MenuStyle.Lerp(a, b, 0.75)!;

        Assert.Equal(127, mid.BackgroundColor!.Resolve(MaterialState.None)!.Value.R);
        Assert.Equal(SystemMouseCursors.Click, mid.MouseCursor!.Resolve(MaterialState.None));
        Assert.Equal(VisualDensity.Compact, late.VisualDensity);
    }

    [Fact]
    public void MenuThemeDatas_DefaultToNullAndFollowTheSourceLerpSpecialCases()
    {
        // Flutter's "MenuThemeData defaults" plus the three `lerp special cases` tests.
        Assert.Null(new MenuThemeData().Style);
        Assert.Null(new MenuThemeData().SubmenuIcon);
        Assert.Null(new MenuBarThemeData().Style);
        Assert.Null(new MenuButtonThemeData().Style);

        var menu = new MenuThemeData();
        var bar = new MenuBarThemeData();
        var button = new MenuButtonThemeData();

        Assert.Null(MenuThemeData.Lerp(null, null, 0.0));
        Assert.Null(MenuBarThemeData.Lerp(null, null, 0.0));
        Assert.Null(MenuButtonThemeData.Lerp(null, null, 0.0));
        Assert.Same(menu, MenuThemeData.Lerp(menu, menu, 0.5));
        Assert.Same(bar, MenuBarThemeData.Lerp(bar, bar, 0.5));
        Assert.Same(button, MenuButtonThemeData.Lerp(button, button, 0.5));
    }

    [Fact]
    public void MenuBarThemeData_ExtendsMenuThemeDataButNeverComparesEqualToIt()
    {
        var style = new MenuStyle(elevation: MaterialStateProperty<double?>.All(3.0));
        var bar = new MenuBarThemeData(style);

        // Dart declares `MenuBarThemeData extends MenuThemeData` but keeps `MenuThemeData`'s
        // runtimeType guard in `operator ==`, and never exposes `submenuIcon` on the bar theme.
        Assert.IsAssignableFrom<MenuThemeData>(bar);
        Assert.Null(bar.SubmenuIcon);
        Assert.NotEqual<MenuThemeData>(new MenuThemeData(style), bar);
        Assert.Equal(bar, new MenuBarThemeData(style));
    }

    private static readonly Rect AnchorRect = new(328.0, 14.0, 144.0, 48.0);

    private static readonly Size OverlaySize = new(800.0, 600.0);

    private static readonly Size MenuSize = new(274.0, 112.0);

    private static MenuLayout Layout(
        Rect anchorRect,
        AlignmentGeometry? alignment = null,
        Vector alignmentOffset = default,
        Vector? menuPosition = null,
        EdgeInsetsGeometry menuPadding = default,
        Axis orientation = Axis.Vertical,
        Axis parentOrientation = Axis.Horizontal,
        EdgeInsetsGeometry reservedPadding = default,
        IReadOnlyList<Rect>? avoidBounds = null,
        double heightFactor = 1.0,
        TextDirection textDirection = TextDirection.Ltr,
        Thickness viewPadding = default,
        Thickness viewInsets = default) => new(
        anchorRect: anchorRect,
        textDirection: textDirection,
        alignment: alignment ?? AlignmentDirectional.BottomStart,
        alignmentOffset: alignmentOffset,
        menuPosition: menuPosition,
        menuPadding: menuPadding,
        orientation: orientation,
        parentOrientation: parentOrientation,
        reservedPadding: reservedPadding,
        avoidBounds: avoidBounds,
        heightFactor: heightFactor,
        viewPadding: viewPadding,
        viewInsets: viewInsets);

    // ---- Shortcut serialization and localized shortcut labels ----

    [Fact]
    public void ShortcutSerialization_ModifierMatchesTheSourceChannelShape()
    {
        ShortcutSerialization serialized =
            new SingleActivator(LogicalKeyboardKey.KeyA, control: true, shift: true).SerializeForMenu();

        Assert.Equal(LogicalKeyboardKey.KeyA, serialized.Trigger);
        Assert.Null(serialized.Character);
        Assert.True(serialized.Control);
        Assert.True(serialized.Shift);
        Assert.False(serialized.Alt);
        Assert.False(serialized.Meta);

        IReadOnlyDictionary<string, object?> channel = serialized.ToChannelRepresentation();
        Assert.Equal(LogicalKeyboardKey.KeyA.KeyId, channel["shortcutTrigger"]);
        // control (1 << 3) | shift (1 << 1)
        Assert.Equal(10, channel["shortcutModifiers"]);
        Assert.False(channel.ContainsKey("shortcutCharacter"));
    }

    [Fact]
    public void ShortcutSerialization_CharacterMatchesTheSourceChannelShapeAndCarriesNoShift()
    {
        ShortcutSerialization serialized =
            new CharacterActivator("a", alt: true, meta: true).SerializeForMenu();

        Assert.Equal("a", serialized.Character);
        Assert.Null(serialized.Trigger);
        Assert.Null(serialized.Shift);
        Assert.True(serialized.Alt);
        Assert.True(serialized.Meta);
        Assert.False(serialized.Control);

        IReadOnlyDictionary<string, object?> channel = serialized.ToChannelRepresentation();
        Assert.Equal("a", channel["shortcutCharacter"]);
        // alt (1 << 2) | meta (1 << 0)
        Assert.Equal(5, channel["shortcutModifiers"]);
        Assert.False(channel.ContainsKey("shortcutTrigger"));
    }

    [Fact]
    public void ShortcutSerialization_RejectsModifierTriggersAndNonSingleCharacters()
    {
        Assert.Throws<ArgumentException>(
            () => ShortcutSerialization.Modifier(LogicalKeyboardKey.ShiftLeft));
        Assert.Throws<ArgumentException>(
            () => ShortcutSerialization.Modifier(LogicalKeyboardKey.Control));
        Assert.Throws<ArgumentException>(() => ShortcutSerialization.ForCharacter("ab"));
        Assert.Throws<ArgumentException>(() => ShortcutSerialization.ForCharacter(string.Empty));
    }

    [Theory]
    // Flutter's "Shortcut mnemonics are displayed", one row per modifier and platform family.
    [InlineData(TargetPlatform.Android, "KeyA", true, false, false, false, "Ctrl+A")]
    [InlineData(TargetPlatform.Linux, "KeyA", true, false, false, false, "Ctrl+A")]
    [InlineData(TargetPlatform.Windows, "KeyA", true, false, false, false, "Ctrl+A")]
    [InlineData(TargetPlatform.MacOS, "KeyA", true, false, false, false, "\u2303 A")]
    [InlineData(TargetPlatform.IOS, "KeyA", true, false, false, false, "\u2303 A")]
    [InlineData(TargetPlatform.Android, "KeyB", false, true, false, false, "Shift+B")]
    [InlineData(TargetPlatform.MacOS, "KeyB", false, true, false, false, "\u21e7 B")]
    [InlineData(TargetPlatform.Android, "KeyC", false, false, true, false, "Alt+C")]
    [InlineData(TargetPlatform.MacOS, "KeyC", false, false, true, false, "\u2325 C")]
    [InlineData(TargetPlatform.Android, "KeyD", false, false, false, true, "Meta+D")]
    [InlineData(TargetPlatform.Linux, "KeyD", false, false, false, true, "Meta+D")]
    [InlineData(TargetPlatform.Fuchsia, "KeyD", false, false, false, true, "Meta+D")]
    [InlineData(TargetPlatform.Windows, "KeyD", false, false, false, true, "Win+D")]
    [InlineData(TargetPlatform.MacOS, "KeyD", false, false, false, true, "\u2318 D")]
    public void LocalizedShortcutLabeler_SingleActivatorModifierLabelsMatchTheSource(
        TargetPlatform platform,
        string trigger,
        bool control,
        bool shift,
        bool alt,
        bool meta,
        string expected)
    {
        Assert.Equal(expected, Label(new SingleActivator(Trigger(trigger), control, shift, alt, meta), platform));
    }

    [Theory]
    // The graphic table wins on every platform; `enter` never uses a localized name.
    [InlineData("ArrowLeft", "\u2190")]
    [InlineData("ArrowRight", "\u2192")]
    [InlineData("ArrowUp", "\u2191")]
    [InlineData("ArrowDown", "\u2193")]
    [InlineData("Enter", "\u21b5")]
    // Localized names come next, then the single-character upper-cased fallback.
    [InlineData("Escape", "Esc")]
    [InlineData("Fn", "Fn")]
    [InlineData("Delete", "Del")]
    [InlineData("PageDown", "PgDown")]
    [InlineData("NumpadEnter", "Num Enter")]
    [InlineData("KeyA", "A")]
    [InlineData("Digit5", "5")]
    // Nothing matches, so the key label itself is used.
    [InlineData("F4", "F4")]
    public void LocalizedShortcutLabeler_TriggerNamesFollowTheSourceFallbackChain(
        string trigger,
        string expected)
    {
        foreach (TargetPlatform platform in Enum.GetValues<TargetPlatform>())
        {
            Assert.Equal(expected, Label(new SingleActivator(Trigger(trigger)), platform));
        }
    }

    [Theory]
    // Flutter's "CharacterActivator shortcut mnemonics include modifiers" (flutter/flutter#145040).
    [InlineData(TargetPlatform.Android, "A", true, false, false, "Ctrl+A")]
    [InlineData(TargetPlatform.MacOS, "A", true, false, false, "\u2303 A")]
    [InlineData(TargetPlatform.Android, "B", false, true, false, "Alt+B")]
    [InlineData(TargetPlatform.MacOS, "B", false, true, false, "\u2325 B")]
    [InlineData(TargetPlatform.Android, "C", false, false, true, "Meta+C")]
    [InlineData(TargetPlatform.Windows, "C", false, false, true, "Win+C")]
    [InlineData(TargetPlatform.MacOS, "C", false, false, true, "\u2318 C")]
    // The character is emitted verbatim, never upper-cased.
    [InlineData(TargetPlatform.Android, "\u00f1", false, false, false, "\u00f1")]
    [InlineData(TargetPlatform.MacOS, "\u00f1", false, false, false, "\u00f1")]
    public void LocalizedShortcutLabeler_CharacterActivatorLabelsMatchTheSource(
        TargetPlatform platform,
        string character,
        bool control,
        bool alt,
        bool meta,
        string expected)
    {
        Assert.Equal(expected, Label(new CharacterActivator(character, control, alt, meta), platform));
    }

    [Theory]
    // Flutter's "getShortcutLabel returns the right labels": modifier order and separator per family.
    [InlineData(TargetPlatform.Android, "Alt+Ctrl+Meta+Shift+A")]
    [InlineData(TargetPlatform.Linux, "Alt+Ctrl+Meta+Shift+A")]
    [InlineData(TargetPlatform.Fuchsia, "Alt+Ctrl+Meta+Shift+A")]
    [InlineData(TargetPlatform.Windows, "Alt+Ctrl+Win+Shift+A")]
    [InlineData(TargetPlatform.MacOS, "\u2303 \u2325 \u21e7 \u2318 A")]
    [InlineData(TargetPlatform.IOS, "\u2303 \u2325 \u21e7 \u2318 A")]
    public void LocalizedShortcutLabeler_EveryModifierUsesTheSourceOrderAndSeparator(
        TargetPlatform platform,
        string expected)
    {
        var activator = new SingleActivator(LogicalKeyboardKey.KeyA, control: true, shift: true, alt: true, meta: true);

        Assert.Equal(expected, Label(activator, platform));
    }

    [Fact]
    public void LocalizedShortcutLabeler_LabelsCustomSerializableActivators()
    {
        // The labeler works off `SerializeForMenu`, so a third-party activator labels like the
        // built-in ones, matching Dart's `MenuSerializableShortcut` mixin contract.
        Assert.Equal("Ctrl+Home", Label(new CustomActivator(), TargetPlatform.Linux));
        Assert.Equal("\u2303 Home", Label(new CustomActivator(), TargetPlatform.MacOS));
    }

    [Fact]
    public void LocalizedShortcutLabeler_CachesTheLocalizedNameTablePerLocalizations()
    {
        // Flutter memoizes `_cachedShortcutKeys` per `MaterialLocalizations` instance; a second
        // lookup must return the same string without rebuilding the table.
        var overridden = new KeyLabelOverrideLocalizations();

        Assert.Equal("Ctrl+Escape!", Label(new SingleActivator(LogicalKeyboardKey.Escape, control: true), overridden));
        Assert.Equal("Ctrl+Escape!", Label(new SingleActivator(LogicalKeyboardKey.Escape, control: true), overridden));
        Assert.Equal(
            "Ctrl+Esc",
            Label(new SingleActivator(LogicalKeyboardKey.Escape, control: true), TargetPlatform.Linux));
    }

    private static string Label(IMenuSerializableShortcut shortcut, TargetPlatform platform) =>
        LocalizedShortcutLabeler.Instance.GetShortcutLabel(
            shortcut,
            DefaultMaterialLocalizations.Instance,
            platform);

    private static string Label(IMenuSerializableShortcut shortcut, MaterialLocalizations localizations) =>
        LocalizedShortcutLabeler.Instance.GetShortcutLabel(
            shortcut,
            localizations,
            TargetPlatform.Linux);

    /// <summary>Resolves a generated key member name for the theory data above.</summary>
    private static LogicalKeyboardKey Trigger(string name) =>
        LogicalKeyboardKey.FindKeyByGeneratedName(name)!;

    /// <summary>A third-party activator, standing in for a Dart class mixing in the shortcut.</summary>
    private sealed class CustomActivator : IMenuSerializableShortcut
    {
        public IReadOnlySet<LogicalKeyboardKey>? Triggers => null;

        public bool Accepts(KeyEvent @event, HardwareKeyboard state) => false;

        public string DebugDescribeKeys() => "Control + Home";

        public ShortcutSerialization SerializeForMenu() =>
            ShortcutSerialization.Modifier(LogicalKeyboardKey.Home, control: true);
    }

    private sealed class KeyLabelOverrideLocalizations : DefaultMaterialLocalizations
    {
        public override string TabLabel(int tabIndex, int tabCount) => $"Tab {tabIndex} of {tabCount}";

        public override string KeyboardKeyEscape => "Escape!";
    }
}
