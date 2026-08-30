using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialDropdownTests : IDisposable
{
    private readonly TargetPlatform? _previousPlatform;

    public MaterialDropdownTests()
    {
        // `ModalBarrier` only exposes its dismiss label where the a11y layer supports barrier dismissal
        // (Flutter's `platformSupportsDismissingBarrier`), so pin the platform the way `flutter_test` does
        // instead of inheriting the host OS — CI runs Linux, developers run macOS/Windows.
        _previousPlatform = PlatformDefaults.DebugTargetPlatformOverride;
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        Scheduler.ResetForTests();
        FocusManager.Instance.ResetForTests();
        GestureBinding.Instance.ResetForTests();
    }

    public void Dispose()
    {
        GestureBinding.Instance.ResetForTests();
        FocusManager.Instance.ResetForTests();
        Scheduler.ResetForTests();
        PlatformDefaults.DebugTargetPlatformOverride = _previousPlatform;
    }
    [Fact]
    public void DropdownButtonAndMenuItem_ExposeFlutterDefaultsAndValidateContracts()
    {
        var item = new DropdownMenuItem<string>(new Text("One"), value: "one");
        Assert.Equal("one", item.Value);
        Assert.True(item.Enabled);
        Assert.Null(item.OnTap);
        Assert.Equal((AlignmentGeometry)AlignmentDirectional.CenterStart, item.Alignment);

        var button = new DropdownButton<string>([item], _ => { }, value: "one");
        Assert.Equal(8, button.Elevation);
        Assert.Equal(24, button.IconSize);
        Assert.False(button.IsDense);
        Assert.False(button.IsExpanded);
        Assert.Equal(48, button.ItemHeight);
        Assert.Null(button.MenuWidth);
        Assert.Null(button.MenuMaxHeight);
        Assert.Null(button.EnableFeedback);
        Assert.True(button.BarrierDismissible);
        Assert.False(button.Autofocus);
        Assert.Equal((AlignmentGeometry)AlignmentDirectional.CenterStart, button.Alignment);

        // Dart declares exactly two asserts: a unique matching item, and a minimum item height.
        Assert.Throws<ArgumentException>(() => new DropdownButton<string>(
            [
                new DropdownMenuItem<string>(new Text("A"), value: "same"),
                new DropdownMenuItem<string>(new Text("B"), value: "same"),
            ],
            _ => { },
            value: "same"));
        Assert.Throws<ArgumentException>(() => new DropdownButton<string>(
            [new DropdownMenuItem<string>(new Text("A"), value: "a")],
            _ => { },
            value: "missing"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DropdownButton<string>([item], _ => { }, itemHeight: 47));
        _ = new DropdownButton<string>([item], _ => { }, value: null);
        _ = new DropdownButton<string>(null, null, value: "anything");
    }

    [Theory]
    [InlineData(TextDirection.Ltr, -1.0)]
    [InlineData(TextDirection.Rtl, 1.0)]
    public void DropdownButtonAndMenuItem_ResolveDirectionalAlignment(
        TextDirection direction,
        double expectedX)
    {
        AlignmentGeometry alignment = AlignmentDirectional.BottomStart;
        var item = new DropdownMenuItem<string>(
            child: new SizedBox(width: 20, height: 10),
            value: "one",
            alignment: alignment);
        using var menuItemHarness = new WidgetRenderHarness(Wrap(item, direction: direction));
        menuItemHarness.Pump(new Size(100, 80));
        var itemAlign = Assert.Single(FindDescendants<RenderPositionedBox>(menuItemHarness.RenderView));
        Assert.Equal(new Alignment(expectedX, 1.0), itemAlign.Alignment.Resolve(itemAlign.TextDirection));

        using var buttonHarness = new WidgetRenderHarness(Wrap(
            new DropdownButton<string>(
                items: [item],
                onChanged: _ => { },
                value: "one",
                isExpanded: true,
                alignment: alignment),
            direction: direction));
        buttonHarness.Pump(new Size(240, 100));
        var indexedStack = Assert.Single(FindDescendants<RenderIndexedStack>(buttonHarness.RenderView));
        Assert.Equal(new Alignment(expectedX, 1.0), indexedStack.Alignment.Resolve(indexedStack.TextDirection));
    }

    [Fact]
    public void DropdownButton_UsesSelectedHintDisabledHintAndLargestItemGeometry()
    {
        var items = new DropdownMenuItem<string>[]
        {
            new(new SizedBox(width: 40, child: new Text("Short")), value: "short"),
            new(new SizedBox(width: 180, child: new Text("Longest")), value: "long"),
        };
        using var selected = new WidgetRenderHarness(Wrap(
            new DropdownButton<string>(items, _ => { }, value: "short")));
        var selectedSemantics = selected.PumpAndGetSemantics(new Size(400, 160));
        Assert.NotNull(FindParagraph(selected.RenderView, "Short"));
        var indexed = Assert.Single(FindDescendants<RenderIndexedStack>(selected.RenderView));
        Assert.Equal(0, indexed.Index);
        Assert.True(indexed.Size.Width >= 180);
        Assert.Equal(48, indexed.Size.Height);
        Assert.NotNull(FindSemantics(selectedSemantics, node => node.Label == "Short"));
        Assert.Null(FindSemantics(selectedSemantics, node => node.Label == "Longest"));

        using var hint = new WidgetRenderHarness(Wrap(
            new DropdownButton<string>(items, _ => { }, hint: new Text("Choose"))));
        hint.Pump(new Size(400, 160));
        Assert.NotNull(FindParagraph(hint.RenderView, "Choose"));
        Assert.Equal(2, Assert.Single(FindDescendants<RenderIndexedStack>(hint.RenderView)).Index);

        // Disabled with no value falls back to disabledHint, and to hint when disabledHint is null.
        using var disabled = new WidgetRenderHarness(Wrap(
            new DropdownButton<string>(items, null,
                hint: new Text("Fallback"),
                disabledHint: new Text("Disabled"))));
        var semantics = disabled.PumpAndGetSemantics(new Size(400, 160));
        Assert.NotNull(FindParagraph(disabled.RenderView, "Disabled"));
        Assert.Equal(2, Assert.Single(FindDescendants<RenderIndexedStack>(disabled.RenderView)).Index);
        var disabledNode = FindSemantics(semantics, node => node.Flags.HasFlag(SemanticsFlags.IsButton));
        Assert.NotNull(disabledNode);
        Assert.False(disabledNode!.Actions.HasFlag(SemanticsActions.Tap));

        using var disabledFallback = new WidgetRenderHarness(Wrap(
            new DropdownButton<string>(items, null, hint: new Text("Fallback"))));
        disabledFallback.Pump(new Size(400, 160));
        Assert.NotNull(FindParagraph(disabledFallback.RenderView, "Fallback"));

        // Disabled with a value keeps showing the selected item.
        using var disabledSelected = new WidgetRenderHarness(Wrap(
            new DropdownButton<string>(items, null, value: "long", disabledHint: new Text("Disabled"))));
        disabledSelected.Pump(new Size(400, 160));
        Assert.Equal(1, Assert.Single(FindDescendants<RenderIndexedStack>(disabledSelected.RenderView)).Index);

        // With hint, disabledHint and value all null the stack has no selection at all.
        using var empty = new WidgetRenderHarness(Wrap(new DropdownButton<string>(items, _ => { })));
        empty.Pump(new Size(400, 160));
        Assert.Null(Assert.Single(FindDescendants<RenderIndexedStack>(empty.RenderView)).Index);
    }

    [Fact]
    public void DropdownButton_SelectedItemBuilderDensePaddingAndUnderlinePolicyMatchSourceComposition()
    {
        var items = new DropdownMenuItem<string>[]
        {
            new(new Text("Menu one"), value: "one"),
            new(new Text("Menu two"), value: "two"),
        };
        using var harness = new WidgetRenderHarness(Wrap(
            new DropdownButtonHideUnderline(
                new ButtonTheme(
                    new ButtonThemeData(AlignedDropdown: true),
                    new DropdownButton<string>(
                        items,
                        _ => { },
                        selectedItemBuilder: _ => [new Text("Selected one"), new Text("Selected two")],
                        value: "two",
                        isDense: true,
                        padding: EdgeInsetsGeometry.All(3))))));
        harness.Pump(new Size(400, 160));

        Assert.NotNull(FindParagraph(harness.RenderView, "Selected two"));
        Assert.Equal(1, Assert.Single(FindDescendants<RenderIndexedStack>(harness.RenderView)).Index);
        Assert.Contains(FindDescendants<RenderPadding>(harness.RenderView), value => value.Padding == new Thickness(3));
        // `_kAlignedButtonPadding` is directional: start 16, end 4.
        Assert.Contains(
            FindDescendants<RenderPadding>(harness.RenderView),
            value => value.Padding == new Thickness(16, 0, 4, 0));
        Assert.Empty(UnderlineBorders(harness));

        Assert.Throws<InvalidOperationException>(() => new WidgetRenderHarness(Wrap(
            new DropdownButton<string>(
                items,
                _ => { },
                selectedItemBuilder: _ => [new Text("Only one")],
                value: "one"))));
    }

    [Fact]
    public void DropdownButton_DefaultUnderlineIsAHairlineBottomBorderAndCanBeReplaced()
    {
        var items = new DropdownMenuItem<string>[] { new(new Text("One"), value: "one") };
        using var harness = new WidgetRenderHarness(Wrap(
            new DropdownButton<string>(items, _ => { }, value: "one")));
        harness.Pump(new Size(400, 160));
        BorderSide underline = Assert.Single(UnderlineBorders(harness));
        Assert.Equal(Color.Parse("#FFBDBDBD"), underline.Color);
        Assert.Equal(0.0, underline.Width);

        using var custom = new WidgetRenderHarness(Wrap(
            new DropdownButton<string>(
                items,
                _ => { },
                value: "one",
                underline: new SizedBox(height: 3, child: new ColoredBox(Colors.Purple)))));
        custom.Pump(new Size(400, 160));
        Assert.Empty(UnderlineBorders(custom));
        Assert.Contains(FindDescendants<RenderColoredBox>(custom.RenderView), box => box.Color == Colors.Purple);
    }

    [Fact]
    public void DropdownButton_OpensPositionedMenuAndCompletesKeyboardSelectionSkippingDisabled()
    {
        int buttonTap = 0;
        int firstTap = 0;
        string? selected = null;
        Widget page = new Align(
            alignment: Alignment.TopLeft,
            child: new DropdownButton<string>(
                items:
                [
                    new DropdownMenuItem<string>(new Text("One"), value: "one", onTap: () => firstTap++),
                    new DropdownMenuItem<string>(new Text("Disabled"), value: "disabled", enabled: false),
                    new DropdownMenuItem<string>(new Text("Three"), value: "three"),
                ],
                onChanged: value => selected = value,
                value: "one",
                onTap: () => buttonTap++,
                dropdownColor: Colors.Orange,
                menuWidth: 190,
                menuMaxHeight: 120,
                borderRadius: BorderRadius.Circular(9)));
        using var harness = new WidgetRenderHarness(Wrap(
            new Navigator(new BuilderPageRoute(_ => page))));
        var closedSemantics = harness.PumpAndGetSemantics(new Size(500, 360));
        var open = FindSemantics(closedSemantics, node =>
            node.Flags.HasFlag(SemanticsFlags.HasExpandedState)
            && node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(open);
        Assert.True(open!.PerformAction(SemanticsActions.Tap));
        Assert.Equal(1, buttonTap);
        PumpAnimation();
        var openedSemantics = harness.PumpAndGetSemantics(new Size(500, 360));

        var layout = Assert.Single(FindDescendants<RenderCustomSingleChildLayoutBox>(harness.RenderView));
        Assert.IsType<DropdownMenuRouteLayout<string>>(layout.LayoutDelegate);
        Assert.Equal(190, layout.Child!.Size.Width, precision: 3);
        Assert.True(layout.Child.Size.Height <= 120.01);
        // The route's scroll view now carries a scrollbar of its own, which is also a CustomPaint.
        var painter = Assert.IsType<DropdownMenuPainter>(
            Assert.Single(FindDescendants<RenderCustomPaint>(harness.RenderView)
                .Where(paint => paint.Painter is DropdownMenuPainter)).Painter);
        Assert.Equal(Colors.Orange, painter.Color);
        Assert.Equal(BorderRadius.Circular(9), painter.BorderRadius);
        Assert.Equal(8, painter.Elevation);
        // A non-null border radius adds the clip that Dart configures with `Clip.antiAlias`.
        Assert.Contains(
            FindDescendants<RenderClipRRect>(harness.RenderView),
            clip => clip.BorderRadius == BorderRadius.Circular(9));
        Assert.NotNull(FindSemantics(openedSemantics, node =>
            node.Role == SemanticsRole.Menu
            && node.Label == "Popup menu"));
        // The selected row autofocuses; arrow traversal skips the disabled row, which has no InkWell.
        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowDown)));
        harness.Pump(new Size(500, 360));
        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Enter)));
        PumpAnimation();
        harness.Pump(new Size(500, 360));
        Assert.Equal("three", selected);
        Assert.Equal(0, firstTap);
        Assert.Equal(1, buttonTap);
    }

    [Fact]
    public void DropdownButton_ItemTapRunsBeforeNullableSelectionAndBarrierPolicyIsHonored()
    {
        int itemTap = 0;
        bool changed = false;
        string? value = "one";
        using var harness = new WidgetRenderHarness(Wrap(
            new Navigator(new BuilderPageRoute(_ => new DropdownButton<string>(
                items:
                [
                    new DropdownMenuItem<string>(new Text("None"), value: null, onTap: () => itemTap++),
                    new DropdownMenuItem<string>(new Text("One"), value: "one"),
                ],
                onChanged: next =>
                {
                    Assert.Equal(1, itemTap);
                    value = next;
                    changed = true;
                },
                value: value,
                barrierDismissible: false)))));
        var semantics = harness.PumpAndGetSemantics(new Size(500, 360));
        Assert.True(FindSemantics(semantics, node => node.Actions.HasFlag(SemanticsActions.Tap))!
            .PerformAction(SemanticsActions.Tap));
        PumpAnimation();
        var openSemantics = harness.PumpAndGetSemantics(new Size(500, 360));
        Assert.Null(FindSemantics(openSemantics, node => node.Label == "Dismiss"));

        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowUp)));
        harness.Pump(new Size(500, 360));
        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Enter)));
        PumpAnimation();
        harness.Pump(new Size(500, 360));
        Assert.True(changed);
        Assert.Null(value);
        Assert.Equal(1, itemTap);
    }

    [Fact]
    public void DropdownButton_MenuMeasuresVariableItemHeightsAndRevealsOverTheResizeInterval()
    {
        Widget page = new Align(
            alignment: Alignment.TopLeft,
            child: new DropdownButton<string>(
                items:
                [
                    new DropdownMenuItem<string>(
                        new SizedBox(height: 72, child: new Text("Tall")),
                        value: "tall"),
                    new DropdownMenuItem<string>(new Text("Normal"), value: "normal"),
                ],
                onChanged: _ => { },
                value: "tall",
                itemHeight: null));
        using var harness = new WidgetRenderHarness(Wrap(
            new Navigator(new BuilderPageRoute(_ => page))));
        var semantics = harness.PumpAndGetSemantics(new Size(500, 360));
        Assert.True(FindSemantics(semantics, node =>
            node.Flags.HasFlag(SemanticsFlags.HasExpandedState)
            && node.Actions.HasFlag(SemanticsActions.Tap))!.PerformAction(SemanticsActions.Tap));

        // `_resize` runs over the [0.25, 0.5] interval of the 300 ms route animation.
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.03));
        harness.Pump(new Size(500, 360));
        // The route's scroll view now carries a scrollbar of its own, which is also a CustomPaint.
        var painter = Assert.IsType<DropdownMenuPainter>(
            Assert.Single(FindDescendants<RenderCustomPaint>(harness.RenderView)
                .Where(paint => paint.Painter is DropdownMenuPainter)).Painter);
        Assert.Equal(0.0, painter.Resize.Value, precision: 3);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.09));
        harness.Pump(new Size(500, 360));
        Assert.True(painter.Resize.Value > 0.0);

        PumpAnimation();
        harness.Pump(new Size(500, 360));
        Assert.Equal(1.0, painter.Resize.Value, precision: 3);
        var layout = Assert.Single(FindDescendants<RenderCustomSingleChildLayoutBox>(harness.RenderView));
        var layoutDelegate = Assert.IsType<DropdownMenuRouteLayout<string>>(layout.LayoutDelegate);
        Assert.True(layoutDelegate.Route.ItemHeights[0] >= 72);
        // Dart floors every measured row at the interactive minimum.
        Assert.Equal(48, layoutDelegate.Route.ItemHeights[1]);
    }

    [Fact]
    public void DropdownButton_AutofocusAndKeyboardActivationOpenTheRoute()
    {
        var focusNode = new FocusNode();
        using (var harness = new WidgetRenderHarness(Wrap(
                   new Navigator(new BuilderPageRoute(_ => new DropdownButton<string>(
                       items: [new DropdownMenuItem<string>(new Text("One"), value: "one")],
                       onChanged: _ => { },
                       value: "one",
                       focusNode: focusNode,
                       autofocus: true))))))
        {
            harness.Pump(new Size(500, 360));
            Assert.True(focusNode.HasFocus);
            Assert.Same(focusNode, FocusManager.Instance.PrimaryFocus);
            Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Enter)));
            PumpAnimation();
            var semantics = harness.PumpAndGetSemantics(new Size(500, 360));
            Assert.NotNull(FindSemantics(semantics, node => node.Role == SemanticsRole.Menu));
        }
        focusNode.Dispose();
    }

    [Fact]
    public void DropdownButton_DisabledButtonCannotTakeFocusEvenWithAutofocus()
    {
        var focusNode = new FocusNode();
        using (var harness = new WidgetRenderHarness(Wrap(new DropdownButton<string>(
                   items: [new DropdownMenuItem<string>(new Text("One"), value: "one")],
                   onChanged: null,
                   value: "one",
                   focusNode: focusNode,
                   autofocus: true))))
        {
            harness.Pump(new Size(500, 360));
            Assert.False(focusNode.HasFocus);
        }
        focusNode.Dispose();
    }

    [Theory]
    // Flutter's `Dropdown in middle/top/bottom/center ...` scroll-offset expectations, 100 items of 48.
    [InlineData(276.0, 50, 2180.0)]
    [InlineData(0.0, 99, 4312.0)]
    [InlineData(552.0, 0, 0.0)]
    [InlineData(276.0, 99, 4312.0)]
    public void DropdownRoute_MenuLimitsMatchTheSourceScrollOffsets(
        double buttonTop,
        int selectedIndex,
        double expectedScrollOffset)
    {
        DropdownRoute<string> route = BuildRoute(100, selectedIndex);
        var buttonRect = new Rect(0.0, buttonTop, 100.0, 48.0);
        DropdownMenuLimits limits = route.GetMenuLimits(buttonRect, 600.0, selectedIndex);
        Assert.Equal(expectedScrollOffset, limits.ScrollOffset, precision: 3);
        // The menu is one interactive row shy of the view on both edges.
        Assert.Equal(504.0, limits.Height, precision: 3);
        Assert.Equal(limits.Height, limits.Bottom - limits.Top, precision: 6);
    }

    [Fact]
    public void DropdownRoute_MenuLimitsAlignTheSelectedRowAndHonorMenuMaxHeight()
    {
        // Three rows fit, so the menu is placed so that the selected row covers the button.
        DropdownRoute<string> route = BuildRoute(3, 1);
        var buttonRect = new Rect(0.0, 276.0, 100.0, 48.0);
        DropdownMenuLimits limits = route.GetMenuLimits(buttonRect, 600.0, 1);
        Assert.Equal(8.0 + (3 * 48.0) + 8.0, limits.Height, precision: 3);
        Assert.Equal(276.0 - 8.0 - 48.0, limits.Top, precision: 3);
        Assert.Equal(0.0, limits.ScrollOffset, precision: 3);
        Assert.Equal(8.0, route.GetItemOffset(0), precision: 3);
        Assert.Equal(8.0 + 48.0, route.GetItemOffset(1), precision: 3);

        DropdownRoute<string> capped = BuildRoute(20, 0, menuMaxHeight: 7 * 48.0);
        DropdownMenuLimits cappedLimits = capped.GetMenuLimits(buttonRect, 600.0, 0);
        Assert.Equal(336.0, cappedLimits.Height, precision: 3);

        // A menuMaxHeight above the default cap is clamped back to it.
        DropdownRoute<string> tall = BuildRoute(20, 0, menuMaxHeight: 600.0);
        Assert.Equal(504.0, tall.GetMenuLimits(buttonRect, 600.0, 0).Height, precision: 3);
    }

    [Theory]
    [InlineData(TextDirection.Ltr, 120.0)]
    [InlineData(TextDirection.Rtl, 100.0)]
    public void DropdownMenuRouteLayout_ConstrainsAndPositionsLikeTheSource(
        TextDirection direction,
        double expectedLeft)
    {
        DropdownRoute<string> route = BuildRoute(3, 0);
        var buttonRect = new Rect(120.0, 100.0, 80.0, 48.0);
        var layout = new DropdownMenuRouteLayout<string>(buttonRect, route, direction, menuWidth: null);
        BoxConstraints constraints = layout.GetConstraintsForChild(BoxConstraints.Tight(new Size(400.0, 600.0)));
        Assert.Equal(80.0, constraints.MinWidth, precision: 3);
        Assert.Equal(80.0, constraints.MaxWidth, precision: 3);
        Assert.Equal(504.0, constraints.MaxHeight, precision: 3);

        Point position = layout.GetPositionForChild(new Size(400.0, 600.0), new Size(100.0, 112.0));
        Assert.Equal(expectedLeft, position.X, precision: 3);
        Assert.Equal(route.GetMenuLimits(buttonRect, 600.0, 0).Top, position.Y, precision: 3);

        // `menuWidth` overrides the button width, and never exceeds the view.
        var wide = new DropdownMenuRouteLayout<string>(buttonRect, route, direction, menuWidth: 200.0);
        Assert.Equal(200.0, wide.GetConstraintsForChild(BoxConstraints.Tight(new Size(400.0, 600.0))).MaxWidth, 3);
        var clamped = new DropdownMenuRouteLayout<string>(buttonRect, route, direction, menuWidth: 900.0);
        Assert.Equal(400.0, clamped.GetConstraintsForChild(BoxConstraints.Tight(new Size(400.0, 600.0))).MaxWidth, 3);

        // Dart relayouts only when the button rect or the text direction changed.
        var same = new DropdownMenuRouteLayout<string>(buttonRect, route, direction, menuWidth: null);
        var moved = new DropdownMenuRouteLayout<string>(
            new Rect(0.0, 0.0, 10.0, 10.0),
            route,
            direction,
            menuWidth: null);
        Assert.False(layout.ShouldRelayout(same));
        Assert.True(layout.ShouldRelayout(moved));
    }

    [Fact]
    public void DropdownMenuPainter_GrowsFromTheSelectedRowToTheWholeMenu()
    {
        var size = new Size(120.0, 208.0);
        Rect start = DropdownMenuPainter.ResolveRect(56.0, size, 0.0);
        Assert.Equal(new Rect(0.0, 56.0, 120.0, 48.0), start);

        Rect middle = DropdownMenuPainter.ResolveRect(56.0, size, 0.5);
        Assert.Equal(28.0, middle.Top, precision: 3);
        Assert.Equal(156.0, middle.Bottom, precision: 3);

        Rect end = DropdownMenuPainter.ResolveRect(56.0, size, 1.0);
        Assert.Equal(new Rect(0.0, 0.0, 120.0, 208.0), end);

        // A menu shorter than one row still produces a normalized rect.
        var shortSize = new Size(112.0, 47.0);
        Rect shortStart = DropdownMenuPainter.ResolveRect(8.0, shortSize, 0.0);
        Assert.Equal(0.0, shortStart.Top, precision: 3);
        Assert.Equal(47.0, shortStart.Bottom, precision: 3);
    }

    [Fact]
    public void MaterialShadows_MatchTheSourceElevationTable()
    {
        Assert.Empty(MaterialShadows.ForElevation(0)!);
        Assert.Null(MaterialShadows.ForElevation(5));
        Assert.Null(MaterialShadows.ForElevation(-1));

        IReadOnlyList<Plumix.Rendering.BoxShadow> eight = MaterialShadows.ForElevation(8)!;
        Assert.Equal(3, eight.Count);
        Assert.Equal(Color.FromArgb(0x33, 0, 0, 0), eight[0].Color);
        Assert.Equal(new Point(0.0, 5.0), eight[0].Offset);
        Assert.Equal(5.0, eight[0].BlurRadius);
        Assert.Equal(-3.0, eight[0].SpreadRadius);
        Assert.Equal(Color.FromArgb(0x24, 0, 0, 0), eight[1].Color);
        Assert.Equal(new Point(0.0, 8.0), eight[1].Offset);
        Assert.Equal(10.0, eight[1].BlurRadius);
        Assert.Equal(1.0, eight[1].SpreadRadius);
        Assert.Equal(Color.FromArgb(0x1F, 0, 0, 0), eight[2].Color);
        Assert.Equal(new Point(0.0, 3.0), eight[2].Offset);
        Assert.Equal(14.0, eight[2].BlurRadius);
        Assert.Equal(2.0, eight[2].SpreadRadius);

        Assert.Equal(new Point(0.0, 11.0), MaterialShadows.ForElevation(24)![0].Offset);
        Assert.Equal(46.0, MaterialShadows.ForElevation(24)![2].BlurRadius);
    }

    [Fact]
    public void DropdownButton_ClosesTheMenuOnOrientationChangeButNotOnHeightChange()
    {
        Action<Size>? resize = null;
        var current = new Size(500, 360);
        Widget root = new StatefulBuilder((_, setState) =>
        {
            resize = next => setState(() => current = next);
            return new Directionality(
                TextDirection.Ltr,
                new MediaQuery(
                    new MediaQueryData(Size: current),
                    new Theme(
                        ThemeData.Light,
                        new Overlay(initialEntries:
                        [
                            new OverlayEntry(_ => new Navigator(new BuilderPageRoute(_ =>
                                new DropdownButton<string>(
                                    items: [new DropdownMenuItem<string>(new Text("One"), value: "one")],
                                    onChanged: _ => { },
                                    value: "one")))),
                        ]))));
        });
        using var harness = new WidgetRenderHarness(root);
        var semantics = harness.PumpAndGetSemantics(new Size(500, 360));
        Assert.True(FindSemantics(semantics, node =>
            node.Flags.HasFlag(SemanticsFlags.HasExpandedState)
            && node.Actions.HasFlag(SemanticsActions.Tap))!.PerformAction(SemanticsActions.Tap));
        PumpAnimation();
        Assert.NotNull(FindSemantics(
            harness.PumpAndGetSemantics(new Size(500, 360)),
            node => node.Role == SemanticsRole.Menu));

        // A keyboard-sized height change keeps the same orientation, so the menu survives.
        resize!(new Size(500, 300));
        harness.Pump(new Size(500, 300));
        PumpAnimation();
        Assert.NotNull(FindSemantics(
            harness.PumpAndGetSemantics(new Size(500, 300)),
            node => node.Role == SemanticsRole.Menu));

        var layoutDelegate = (DropdownMenuRouteLayout<string>)Assert
            .Single(FindDescendants<RenderCustomSingleChildLayoutBox>(harness.RenderView)).LayoutDelegate;
        resize(new Size(300, 500));
        harness.Pump(new Size(300, 500));
        Assert.False(layoutDelegate.Route.IsActive);
        Assert.Contains(
            FindDescendants<RenderCustomSingleChildLayoutBox>(harness.RenderView),
            layout => layout.LayoutDelegate is DropdownMenuRouteLayout<string>);
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.01));
        harness.Pump(new Size(300, 500));
        Assert.DoesNotContain(
            FindDescendants<RenderCustomSingleChildLayoutBox>(harness.RenderView),
            layout => layout.LayoutDelegate is DropdownMenuRouteLayout<string>);
    }

    [Fact]
    public void DropdownRoute_ReportsCompletionFromTheMicrotaskQueue()
    {
        DropdownRoute<string> route = BuildRoute(1, 0);
        DropdownRouteResult<string>? completion = null;
        route.RouteCompleted += (_, result) => completion = result;

        route.DidComplete(new DropdownRouteResult<string>("one"));

        Assert.Null(completion);
        Scheduler.FlushMicrotasks();
        Assert.Equal("one", completion?.Result);
    }

    [Fact]
    public void DropdownButtonFormField_ExposesFlutterDefaultsAndValidatesContracts()
    {
        var items = new[] { new DropdownMenuItem<string>(new Text("One"), value: "one") };
        var field = new DropdownButtonFormField<string>(items, _ => { }, initialValue: "one");
        Assert.True(field.IsDense);
        Assert.False(field.IsExpanded);
        Assert.Null(field.ItemHeight);
        Assert.Equal(8, field.Elevation);
        Assert.Equal(24, field.IconSize);
        Assert.True(field.BarrierDismissible);
        Assert.NotNull(field.Decoration);
        Assert.Equal((AlignmentGeometry)AlignmentDirectional.CenterStart, field.Alignment);

        // The deprecated `value` parameter still seeds the form value.
        Assert.Equal("one", new DropdownButtonFormField<string>(items, _ => { }, value: "one").InitialValue);

        Assert.Throws<ArgumentException>(() =>
            new DropdownButtonFormField<string>(items, _ => { }, initialValue: "missing"));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DropdownButtonFormField<string>(items, _ => { }, itemHeight: 47));
        Assert.Throws<ArgumentException>(() => new DropdownButtonFormField<string>(
            items,
            _ => { },
            decoration: new InputDecoration(errorText: "fixed"),
            errorBuilder: (_, error) => new Text(error)));
    }

    [Fact]
    public void DropdownButtonFormField_ValidationChangeOrderingAndResetMatchSource()
    {
        var items = new[]
        {
            new DropdownMenuItem<string>(new Text("One"), value: "one"),
            new DropdownMenuItem<string>(new Text("Two"), value: "two"),
        };
        var callbacks = new List<string>();
        FormState? formState = null;
        using var harness = new WidgetRenderHarness(Wrap(new Form(
            onChanged: () => callbacks.Add("form"),
            child: new Builder(context =>
            {
                formState = Form.Of(context);
                return new DropdownButtonFormField<string>(
                    items,
                    value => callbacks.Add($"field:{value}"),
                    initialValue: "one",
                    decoration: new InputDecoration(labelText: "Choice"),
                    validator: value => value == "one" ? "Choose another" : null);
            }))));
        harness.Pump(new Size(420, 160));
        var state = Assert.IsType<DropdownButtonFormFieldState<string>>(Assert.Single(formState!.Fields));

        Assert.False(formState.Validate());
        harness.Pump(new Size(420, 180));
        Assert.Contains(
            FindDescendants<RenderParagraph>(harness.RenderView),
            value => value.PlainText == "Choose another");

        state.DidChange("two");
        Assert.Equal(new[] { "form", "field:two" }, callbacks.TakeLast(2));
        Assert.Equal("two", state.Value);
        Assert.True(formState.Validate());

        formState.Reset();
        harness.Pump(new Size(420, 160));
        Assert.Equal("one", state.Value);
        Assert.Equal(new[] { "form", "field:one", "form" }, callbacks.TakeLast(3));
    }

    [Fact]
    public void DropdownButtonFormField_UsesDecorationHintAndCustomErrorWidget()
    {
        FormState? formState = null;
        using var harness = new WidgetRenderHarness(Wrap(new Form(
            child: new Builder(context =>
            {
                formState = Form.Of(context);
                return new DropdownButtonFormField<string>(
                    [new DropdownMenuItem<string>(new Text("One"), value: "one")],
                    _ => { },
                    decoration: new InputDecoration(hintText: "Pick one"),
                    validator: _ => "Required",
                    errorBuilder: (_, error) => new Text($"custom {error}"));
            }))));
        harness.Pump(new Size(420, 140));
        Assert.NotNull(FindParagraph(harness.RenderView, "Pick one"));

        Assert.False(formState!.Validate());
        harness.Pump(new Size(420, 180));
        Assert.NotNull(FindParagraph(harness.RenderView, "custom Required"));
    }

    [Fact]
    public void DropdownButtonFormField_DecorationHintTextNeverOverridesAnExplicitHint()
    {
        var items = new[] { new DropdownMenuItem<string>(new Text("One"), value: "one") };
        using var explicitHint = new WidgetRenderHarness(Wrap(new DropdownButtonFormField<string>(
            items,
            _ => { },
            hint: new Text("Explicit"),
            decoration: new InputDecoration(hintText: "Decoration"))));
        explicitHint.Pump(new Size(420, 140));
        Assert.NotNull(FindParagraph(explicitHint.RenderView, "Explicit"));

        // With no items the field is disabled, so the decoration hint becomes the disabled hint.
        using var disabled = new WidgetRenderHarness(Wrap(new DropdownButtonFormField<string>(
            [],
            null,
            decoration: new InputDecoration(hintText: "Decoration"))));
        disabled.Pump(new Size(420, 140));
        Assert.NotNull(FindParagraph(disabled.RenderView, "Decoration"));
    }

    [Fact]
    public void DropdownButtonFormField_AlignedDropdownThemeDoesNotShiftTheFieldContent()
    {
        var items = new[] { new DropdownMenuItem<string>(new Text("One"), value: "one") };
        using var harness = new WidgetRenderHarness(Wrap(new ButtonTheme(
            new ButtonThemeData(AlignedDropdown: true),
            new DropdownButtonFormField<string>(items, _ => { }, initialValue: "one"))));
        harness.Pump(new Size(420, 140));
        // The form field always uses `_kUnalignedButtonPadding` because it carries an InputDecoration.
        Assert.DoesNotContain(
            FindDescendants<RenderPadding>(harness.RenderView),
            value => value.Padding == new Thickness(16, 0, 4, 0));
        Assert.NotNull(FindParagraph(harness.RenderView, "One"));
    }

    private static DropdownRoute<string> BuildRoute(
        int itemCount,
        int selectedIndex,
        double? itemHeight = 48.0,
        double? menuMaxHeight = null)
    {
        var items = new MenuItem<string>[itemCount];
        for (int index = 0; index < itemCount; index++)
        {
            items[index] = new MenuItem<string>(
                onLayout: _ => { },
                item: new DropdownMenuItem<string>(new Text($"Item {index}"), value: $"{index}"));
        }

        return new DropdownRoute<string>(
            items: items,
            padding: EdgeInsetsGeometry.Symmetric(horizontal: 16.0),
            buttonRect: new Rect(0.0, 0.0, 100.0, 48.0),
            selectedIndex: selectedIndex,
            capturedThemes: new CapturedThemes([]),
            style: ThemeData.Light.TextTheme.TitleMedium,
            itemHeight: itemHeight,
            menuMaxHeight: menuMaxHeight);
    }

    private static List<BorderSide> UnderlineBorders(WidgetRenderHarness harness) =>
        FindDescendants<RenderDecoratedBox>(harness.RenderView)
            .Select(box => box.Decoration)
            .OfType<BoxDecoration>()
            .Select(decoration => decoration.Border)
            .OfType<Plumix.Rendering.Border>()
            .Where(border => border.Bottom.Color == Color.Parse("#FFBDBDBD") && border.Top == BorderSide.None)
            .Select(border => border.Bottom)
            .ToList();


    [Fact]
    public void MenuAnchor_ControllerAndMenuItem_FollowOpenCloseAndActivationContracts()
    {
        var controller = new MenuController();
        int activations = 0;
        using var harness = new WidgetRenderHarness(Wrap(new MenuAnchor(
            controller: controller,
            child: new SizedBox(width: 80, height: 40),
            menuChildren:
            [
                new MenuItemButton(child: new Text("Run"), onPressed: () => activations++),
                new MenuItemButton(child: new Text("Disabled")),
            ])));
        harness.Pump(new Size(500, 360));

        Assert.False(controller.IsOpen);
        controller.Open();
        harness.Pump(new Size(500, 360));
        Assert.True(controller.IsOpen);
        Assert.NotNull(FindParagraph(harness.RenderView, "Run"));
        var semantics = harness.PumpAndGetSemantics(new Size(500, 360));
        var item = FindSemantics(semantics, node => node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(item);
        Assert.True(item!.PerformAction(SemanticsActions.Tap));
        Scheduler.PumpFrameForTests();
        harness.Pump(new Size(500, 360));
        Assert.False(controller.IsOpen);
        Assert.Equal(1, activations);
    }

    [Fact]
    public void MenuBarAndSubmenuButton_ManageNestedMenusSiblingClosingAndPanelOrientation()
    {
        var fileController = new MenuController();
        var editController = new MenuController();
        var recentController = new MenuController();
        var empty = new SubmenuButton([], new Text("Disabled"));
        using var harness = new WidgetRenderHarness(Wrap(new MenuBar(
            children:
            [
                new SubmenuButton(
                    [
                        new MenuItemButton(child: new Text("Open"), onPressed: () => { }),
                        new SubmenuButton(
                            [new MenuItemButton(child: new Text("Report"), onPressed: () => { })],
                            new Text("Recent"),
                            controller: recentController),
                    ],
                    new Text("File"),
                    controller: fileController),
                new SubmenuButton(
                    [new MenuItemButton(child: new Text("Paste"), onPressed: () => { })],
                    new Text("Edit"),
                    controller: editController),
                empty,
            ])));
        harness.Pump(new Size(500, 360));

        Assert.False(empty.Enabled);
        Assert.False(fileController.IsOpen);
        fileController.Open();
        harness.Pump(new Size(500, 360));
        Assert.True(fileController.IsOpen);
        Assert.NotNull(FindParagraph(harness.RenderView, "Open"));

        recentController.Open();
        harness.Pump(new Size(500, 360));
        Assert.True(fileController.IsOpen);
        Assert.True(recentController.IsOpen);

        editController.Open();
        harness.Pump(new Size(500, 360));
        Assert.False(fileController.IsOpen);
        Assert.False(recentController.IsOpen);
        Assert.True(editController.IsOpen);

        editController.Close();
        harness.Pump(new Size(500, 360));
        Assert.False(editController.IsOpen);

        fileController.Open();
        harness.Pump(new Size(500, 360));
        Assert.NotNull(FindParagraph(harness.RenderView, "Open"));
    }

    [Fact]
    public void MenuBarAndMenuButtonThemes_ResolveThemeLocalAndWidgetStylePrecedence()
    {
        Color themeBackground = Color.Parse("#FFE3F2FD");
        Color localBackground = Color.Parse("#FFFFF3E0");
        Color widgetBackground = Color.Parse("#FFE8F5E9");
        ThemeData theme = ThemeData.Light with
        {
            MenuBarTheme = new MenuBarThemeData(new MenuStyle(
                backgroundColor: MaterialStateProperty<Color?>.All(themeBackground))),
            MenuButtonTheme = new MenuButtonThemeData(new ButtonStyle(
                ForegroundColor: MaterialStateProperty<Color?>.All(Colors.CadetBlue))),
        };

        Widget themedBar = new MenuBar(
            [new SubmenuButton(
                [new MenuItemButton(child: new Text("Open"), onPressed: () => { })],
                new Text("File"))]);
        using var themed = new WidgetRenderHarness(Wrap(themedBar, theme));
        themed.Pump(new Size(500, 180));
        Assert.Contains(
            FindDescendants<RenderDecoratedBox>(themed.RenderView),
            box => box.Decoration.Color == themeBackground);
        Assert.Equal(
            Colors.CadetBlue,
            Assert.IsType<SolidColorBrush>(FindParagraph(themed.RenderView, "File")!.Foreground).Color);

        var itemController = new MenuController();
        Widget themedItem = new MenuButtonTheme(
            new MenuButtonThemeData(new ButtonStyle(
                ForegroundColor: MaterialStateProperty<Color?>.All(Colors.ForestGreen))),
            new MenuAnchor(
                [new MenuItemButton(child: new Text("Run"), onPressed: () => { })],
                child: new SizedBox(width: 80, height: 40),
                controller: itemController));
        using var item = new WidgetRenderHarness(Wrap(themedItem, theme));
        item.Pump(new Size(500, 180));
        itemController.Open();
        item.Pump(new Size(500, 180));
        Assert.Equal(
            Colors.ForestGreen,
            Assert.IsType<SolidColorBrush>(FindParagraph(item.RenderView, "Run")!.Foreground).Color);

        Widget localBar = new MenuBarTheme(
            new MenuBarThemeData(new MenuStyle(backgroundColor: MaterialStateProperty<Color?>.All(localBackground))),
            new MenuButtonTheme(
                new MenuButtonThemeData(new ButtonStyle(
                    ForegroundColor: MaterialStateProperty<Color?>.All(Colors.MediumVioletRed))),
                new MenuBar(
                    [new SubmenuButton(
                        [new MenuItemButton(child: new Text("Save"), onPressed: () => { })],
                        new Text("Edit"))])));
        using var local = new WidgetRenderHarness(Wrap(localBar, theme));
        local.Pump(new Size(500, 180));
        Assert.Contains(
            FindDescendants<RenderDecoratedBox>(local.RenderView),
            box => box.Decoration.Color == localBackground);
        Assert.Equal(
            Colors.MediumVioletRed,
            Assert.IsType<SolidColorBrush>(FindParagraph(local.RenderView, "Edit")!.Foreground).Color);

        Widget widgetBar = new MenuBar(
            [new SubmenuButton(
                [new MenuItemButton(child: new Text("Close"), onPressed: () => { })],
                new Text("View"),
                style: new ButtonStyle(ForegroundColor: MaterialStateProperty<Color?>.All(Colors.OrangeRed)))],
            style: new MenuStyle(backgroundColor: MaterialStateProperty<Color?>.All(widgetBackground)));
        using var widget = new WidgetRenderHarness(Wrap(widgetBar, theme));
        widget.Pump(new Size(500, 180));
        Assert.Contains(
            FindDescendants<RenderDecoratedBox>(widget.RenderView),
            box => box.Decoration.Color == widgetBackground);
        Assert.Equal(
            Colors.OrangeRed,
            Assert.IsType<SolidColorBrush>(FindParagraph(widget.RenderView, "View")!.Foreground).Color);
    }

    [Fact]
    public void MenuTheme_ResolvesGlobalLocalAndWidgetPanelStylePrecedence()
    {
        Color globalBackground = Color.Parse("#FFE3F2FD");
        Color localBackground = Color.Parse("#FFFFF3E0");
        Color widgetBackground = Color.Parse("#FFE8F5E9");
        ThemeData theme = ThemeData.Light with
        {
            MenuTheme = new MenuThemeData(new MenuStyle(
                backgroundColor: MaterialStateProperty<Color?>.All(globalBackground))),
        };

        var globalController = new MenuController();
        using var global = new WidgetRenderHarness(Wrap(new MenuAnchor(
            [new MenuItemButton(child: new Text("Global"), onPressed: () => { })],
            child: new SizedBox(width: 80, height: 40),
            controller: globalController), theme));
        global.Pump(new Size(500, 180));
        globalController.Open();
        global.Pump(new Size(500, 180));
        Assert.Contains(
            FindDescendants<RenderDecoratedBox>(global.RenderView),
            box => box.Decoration.Color == globalBackground);

        var localController = new MenuController();
        using var local = new WidgetRenderHarness(Wrap(new MenuTheme(
            new MenuThemeData(new MenuStyle(
                backgroundColor: MaterialStateProperty<Color?>.All(localBackground))),
            new MenuAnchor(
                [new MenuItemButton(child: new Text("Local"), onPressed: () => { })],
                child: new SizedBox(width: 80, height: 40),
                controller: localController)), theme));
        local.Pump(new Size(500, 180));
        localController.Open();
        local.Pump(new Size(500, 180));
        Assert.Contains(
            FindDescendants<RenderDecoratedBox>(local.RenderView),
            box => box.Decoration.Color == localBackground);

        var widgetController = new MenuController();
        using var widget = new WidgetRenderHarness(Wrap(new MenuTheme(
            new MenuThemeData(new MenuStyle(
                backgroundColor: MaterialStateProperty<Color?>.All(localBackground))),
            new MenuAnchor(
                [new MenuItemButton(child: new Text("Widget"), onPressed: () => { })],
                child: new SizedBox(width: 80, height: 40),
                controller: widgetController,
                style: new MenuStyle(
                    backgroundColor: MaterialStateProperty<Color?>.All(widgetBackground)))), theme));
        widget.Pump(new Size(500, 180));
        widgetController.Open();
        widget.Pump(new Size(500, 180));
        Assert.Contains(
            FindDescendants<RenderDecoratedBox>(widget.RenderView),
            box => box.Decoration.Color == widgetBackground);
    }

    [Fact]
    public void MenuTheme_SubmenuIconUsesWidgetThenLocalThenThemePrecedence()
    {
        ThemeData theme = ThemeData.Light with
        {
            MenuTheme = new MenuThemeData(
                submenuIcon: MaterialStateProperty<Widget?>.All(new Text("theme icon"))),
        };

        var controller = new MenuController();
        Widget themed = new MenuTheme(
            new MenuThemeData(
                submenuIcon: MaterialStateProperty<Widget?>.All(new Text("local icon"))),
            new MenuAnchor(
                [
                    new SubmenuButton(
                        [new MenuItemButton(child: new Text("Open"), onPressed: () => { })],
                        new Text("Local")),
                    new SubmenuButton(
                        [new MenuItemButton(child: new Text("Save"), onPressed: () => { })],
                        new Text("Widget"),
                        submenuIcon: MaterialStateProperty<Widget?>.All(new Text("widget icon"))),
                ],
                controller: controller,
                child: new SizedBox(width: 80, height: 40)));
        using var harness = new WidgetRenderHarness(Wrap(themed, theme));
        harness.Pump(new Size(500, 260));
        controller.Open();
        harness.Pump(new Size(500, 260));

        Assert.NotNull(FindParagraph(harness.RenderView, "local icon"));
        Assert.NotNull(FindParagraph(harness.RenderView, "widget icon"));
        Assert.Null(FindParagraph(harness.RenderView, "theme icon"));
    }

    [Fact]
    public void SubmenuButton_ShowsItsSubmenuIconOnlyInsideAVerticalMenu()
    {
        // Flutter's `_MenuItemLabel.showDecoration` is `parentOrientation == Axis.vertical`, so a
        // top-level `MenuBar` button paints no arrow while a nested submenu does.
        var controller = new MenuController();
        Widget bar = new MenuBar(
        [
            new SubmenuButton(
                [
                    new SubmenuButton(
                        [new MenuItemButton(child: new Text("Leaf"), onPressed: () => { })],
                        new Text("Nested"),
                        submenuIcon: MaterialStateProperty<Widget?>.All(new Text("nested arrow"))),
                ],
                new Text("Top"),
                controller: controller,
                submenuIcon: MaterialStateProperty<Widget?>.All(new Text("top arrow"))),
        ]);
        using var harness = new WidgetRenderHarness(Wrap(bar));
        harness.Pump(new Size(500, 260));

        Assert.Null(FindParagraph(harness.RenderView, "top arrow"));

        controller.Open();
        harness.Pump(new Size(500, 260));

        Assert.Null(FindParagraph(harness.RenderView, "top arrow"));
        Assert.NotNull(FindParagraph(harness.RenderView, "nested arrow"));
    }

    [Fact]
    public void MenuButtonDefaults_MatchTheSourceMaterial3Table()
    {
        ThemeData theme = ThemeData.Light;
        ButtonStyle style = CaptureMenuButtonDefaults(theme);
        ColorScheme colors = theme.ColorScheme;

        Assert.Equal(MaterialColors.Transparent, style.BackgroundColor!.Resolve(MaterialState.None));
        Assert.Equal(0.0, style.Elevation!.Resolve(MaterialState.None));
        Assert.Equal(colors.OnSurface, style.ForegroundColor!.Resolve(MaterialState.None));
        Assert.Equal(
            Opacity(colors.OnSurface, 0.38),
            style.ForegroundColor.Resolve(MaterialState.Disabled));
        Assert.Equal(colors.OnSurfaceVariant, style.IconColor!.Resolve(MaterialState.None));
        Assert.Equal(Opacity(colors.OnSurface, 0.38), style.IconColor.Resolve(MaterialState.Disabled));
        Assert.Equal(24.0, style.IconSize!.Resolve(MaterialState.None));
        Assert.Equal(new Size(64.0, 48.0), style.MinimumSize!.Resolve(MaterialState.None));
        Assert.Equal(
            new Size(double.PositiveInfinity, double.PositiveInfinity),
            style.MaximumSize!.Resolve(MaterialState.None));

        // Square corners, unlike `TextButton`'s 20-radius stadium-ish default.
        var shape = Assert.IsType<RoundedRectangleBorder>(style.Shape!.Resolve(MaterialState.None));
        Assert.Equal(BorderRadius.Zero, shape.BorderRadius.Resolve(TextDirection.Ltr));

        Assert.Equal(MaterialColors.Transparent, style.OverlayColor!.Resolve(MaterialState.None));
        Assert.Equal(Opacity(colors.OnSurface, 0.08), style.OverlayColor.Resolve(MaterialState.Hovered));
        Assert.Equal(Opacity(colors.OnSurface, 0.1), style.OverlayColor.Resolve(MaterialState.Focused));
        Assert.Equal(Opacity(colors.OnSurface, 0.1), style.OverlayColor.Resolve(MaterialState.Pressed));

        // Flutter's "Menu defaults" asserts labelLarge's 14 / 1.43 metrics on the button material.
        TextStyle? textStyle = style.TextStyle!.Resolve(MaterialState.None);
        Assert.Equal(14.0, textStyle!.FontSize);
        Assert.Equal(1.43, textStyle.Height);
        Assert.Equal(theme.VisualDensity, style.VisualDensity);
        Assert.Equal(theme.MaterialTapTargetSize, style.TapTargetSize);
        // `_MenuButtonDefaultsM3` uses AlignmentDirectional.centerStart, so it mirrors under RTL.
        Assert.Equal<AlignmentGeometry?>(AlignmentDirectional.CenterStart, style.Alignment);
        Assert.True(style.Alignment!.Value.IsDirectional);
        Assert.Equal(Alignment.CenterLeft, style.Alignment!.Value.Resolve(TextDirection.Ltr));
        Assert.Equal(Alignment.CenterRight, style.Alignment!.Value.Resolve(TextDirection.Rtl));
        Assert.Equal(TimeSpan.FromMilliseconds(200), style.AnimationDuration);
        Assert.True(style.EnableFeedback);
    }

    [Theory]
    // `_scaledPadding`: max(8, 12 + baseSizeAdjustment.dx) at 1x, and a positive horizontal density
    // is dropped before the adjustment is taken.
    [InlineData(0.0, 1.0, 12.0)]
    [InlineData(-2.0, 1.0, 8.0)]
    [InlineData(-1.0, 1.0, 8.0)]
    [InlineData(2.0, 1.0, 12.0)]
    // The 1x -> 2x -> 3x geometries lerp on `textScale * fontSize / 14`, with labelLarge at 14.
    [InlineData(0.0, 1.5, 10.0)]
    [InlineData(0.0, 2.0, 8.0)]
    [InlineData(0.0, 3.0, 8.0)]
    public void MenuButtonDefaults_ScaledPaddingFollowsDensityAndTextScale(
        double horizontalDensity,
        double textScaleFactor,
        double expectedHorizontal)
    {
        ThemeData theme = ThemeData.Light with
        {
            VisualDensity = new VisualDensity(horizontalDensity, horizontalDensity),
        };

        ButtonStyle style = CaptureMenuButtonDefaults(theme, textScaleFactor);

        Assert.Equal(
            new Thickness(expectedHorizontal, 0.0, expectedHorizontal, 0.0),
            style.Padding!.Resolve(MaterialState.None));
    }

    [Theory]
    // `_MenuItemLabel`: max(4, 12 + density.horizontal * 2) between the leading icon and the label.
    [InlineData(0.0, 12.0)]
    [InlineData(-2.0, 8.0)]
    [InlineData(2.0, 16.0)]
    // `VisualDensity.minimumDensity` is the lowest legal density, and it lands exactly on the floor.
    [InlineData(-4.0, 4.0)]
    public void MenuItemLabel_SpacesTheLeadingIconByTheSourceDensityFormula(
        double horizontalDensity,
        double expectedSpacing)
    {
        ThemeData theme = ThemeData.Light with
        {
            VisualDensity = new VisualDensity(horizontalDensity, 0.0),
        };
        var controller = new MenuController();
        Widget anchor = new MenuAnchor(
            [
                new MenuItemButton(
                    child: new Text("Item"),
                    onPressed: () => { },
                    leadingIcon: new SizedBox(width: 24, height: 24)),
            ],
            controller: controller,
            child: new SizedBox(width: 80, height: 40));
        using var harness = new WidgetRenderHarness(Wrap(anchor, theme));
        harness.Pump(new Size(500, 260));
        controller.Open();
        harness.Pump(new Size(500, 260));

        Assert.Contains(
            FindDescendants<RenderPadding>(harness.RenderView),
            padding => padding.Padding == new Thickness(expectedSpacing, 0.0, 0.0, 0.0));
    }

    [Fact]
    public void MenuItemLabel_PlacesTheShortcutAfterTheTrailingIcon()
    {
        // `_MenuItemLabel` orders its row leading -> trailing icon -> shortcut -> submenu arrow, and
        // pads every non-leading slot by the same directional start spacing.
        var controller = new MenuController();
        Widget anchor = new MenuAnchor(
            [
                new MenuItemButton(
                    child: new Text("Save"),
                    onPressed: () => { },
                    shortcut: new SingleActivator(LogicalKeyboardKey.KeyS, control: true),
                    trailingIcon: new Text("trailing")),
            ],
            controller: controller,
            child: new SizedBox(width: 80, height: 40));
        using var harness = new WidgetRenderHarness(Wrap(anchor, NonAppleTheme));
        harness.Pump(new Size(500, 260));
        controller.Open();
        harness.Pump(new Size(500, 260));

        RenderParagraph? label = FindParagraph(harness.RenderView, "Save");
        RenderParagraph? trailing = FindParagraph(harness.RenderView, "trailing");
        RenderParagraph? shortcut = FindParagraph(harness.RenderView, "Ctrl+S");

        Assert.NotNull(label);
        Assert.NotNull(trailing);
        Assert.NotNull(shortcut);
        Assert.True(GlobalLeft(label!) < GlobalLeft(trailing!));
        Assert.True(GlobalLeft(trailing!) < GlobalLeft(shortcut!));

        // The shortcut slot uses the same max(4, 12 + density.horizontal * 2) start padding.
        Assert.Equal(
            2,
            FindDescendants<RenderPadding>(harness.RenderView)
                .Count(padding => padding.Padding == new Thickness(12.0, 0.0, 0.0, 0.0)));
    }

    [Theory]
    // `_MenuButtonDefaultsM3.alignment` is `AlignmentDirectional.centerStart`, so the button's
    // content aligns to the text-direction start rather than always to the left.
    [InlineData(TextDirection.Ltr, -1.0)]
    [InlineData(TextDirection.Rtl, 1.0)]
    public void MenuItemButton_AlignsItsContentToTheTextDirectionStart(
        TextDirection direction,
        double expectedX)
    {
        var controller = new MenuController();
        Widget anchor = new MenuAnchor(
            [new MenuItemButton(child: new Text("Save"), onPressed: () => { })],
            controller: controller,
            child: new SizedBox(width: 80, height: 40));
        using var harness = new WidgetRenderHarness(Wrap(anchor, NonAppleTheme, direction));
        harness.Pump(new Size(500, 260));
        controller.Open();
        harness.Pump(new Size(500, 260));

        Assert.Contains(
            FindDescendants<RenderPositionedBox>(harness.RenderView),
            align => Close(align.Alignment.X, expectedX) && Close(align.Alignment.Y, 0.0));
    }

    [Fact]
    public void MenuItemLabel_OmitsTheShortcutWhenTheItemHasNone()
    {
        var controller = new MenuController();
        Widget anchor = new MenuAnchor(
            [new MenuItemButton(child: new Text("Save"), onPressed: () => { })],
            controller: controller,
            child: new SizedBox(width: 80, height: 40));
        using var harness = new WidgetRenderHarness(Wrap(anchor));
        harness.Pump(new Size(500, 260));
        controller.Open();
        harness.Pump(new Size(500, 260));

        Assert.NotNull(FindParagraph(harness.RenderView, "Save"));
        Assert.DoesNotContain(
            FindDescendants<RenderParagraph>(harness.RenderView),
            paragraph => paragraph.PlainText.Contains("Ctrl", StringComparison.Ordinal));
    }

    [Theory]
    // The rendered label follows the ambient `ThemeData.platform`, as Flutter's per-platform
    // "Shortcut mnemonics are displayed" variants do.
    [InlineData(TargetPlatform.Linux, "Ctrl+S")]
    [InlineData(TargetPlatform.Android, "Ctrl+S")]
    [InlineData(TargetPlatform.Windows, "Ctrl+S")]
    [InlineData(TargetPlatform.MacOS, "⌃ S")]
    [InlineData(TargetPlatform.IOS, "⌃ S")]
    public void MenuItemLabel_ResolvesTheShortcutLabelAgainstTheAmbientPlatform(
        TargetPlatform platform,
        string expected)
    {
        ThemeData theme = ThemeData.Light with { Platform = platform };
        var controller = new MenuController();
        Widget anchor = new MenuAnchor(
            [
                new MenuItemButton(
                    child: new Text("Save"),
                    onPressed: () => { },
                    shortcut: new SingleActivator(LogicalKeyboardKey.KeyS, control: true)),
            ],
            controller: controller,
            child: new SizedBox(width: 80, height: 40));
        using var harness = new WidgetRenderHarness(Wrap(anchor, theme));
        harness.Pump(new Size(500, 260));
        controller.Open();
        harness.Pump(new Size(500, 260));

        Assert.NotNull(FindParagraph(harness.RenderView, expected));
    }

    [Fact]
    public void MenuItemButton_SemanticsLabelHidesTheGeneratedShortcutText()
    {
        // Flutter's "MenuItemButton semantics respects label": `_MenuItemLabel` wraps the whole row
        // in Semantics(label:, excludeSemantics: true), so the generated shortcut text never reaches
        // the accessibility tree.
        var controller = new MenuController();
        Widget anchor = new MenuAnchor(
            [
                new MenuItemButton(
                    child: new Text("Save"),
                    onPressed: () => { },
                    shortcut: new SingleActivator(LogicalKeyboardKey.KeyS, control: true),
                    semanticsLabel: "TestWidget"),
            ],
            controller: controller,
            child: new SizedBox(width: 80, height: 40));
        using var harness = new WidgetRenderHarness(Wrap(anchor, NonAppleTheme));
        harness.Pump(new Size(500, 260));
        controller.Open();
        harness.Pump(new Size(500, 260));

        RenderParagraph shortcut = Assert.Single(
            FindDescendants<RenderParagraph>(harness.RenderView),
            paragraph => paragraph.PlainText == "Ctrl+S");
        Assert.Contains(
            FindDescendants<RenderExcludeSemantics>(harness.RenderView),
            excluded => excluded.Excluding && FindDescendants<RenderParagraph>(excluded).Contains(shortcut));
    }

    [Fact]
    public void CheckboxAndRadioMenuButtons_ForwardTheirShortcutToTheItemLabel()
    {
        var controller = new MenuController();
        Widget anchor = new MenuAnchor(
            [
                new CheckboxMenuButton(
                    true,
                    _ => { },
                    new Text("Pin"),
                    shortcut: new SingleActivator(LogicalKeyboardKey.KeyP, control: true)),
                new RadioMenuButton<string>(
                    "one",
                    "one",
                    _ => { },
                    new Text("Layout"),
                    shortcut: new SingleActivator(LogicalKeyboardKey.KeyL, alt: true)),
            ],
            controller: controller,
            child: new SizedBox(width: 80, height: 40));
        using var harness = new WidgetRenderHarness(Wrap(anchor, NonAppleTheme));
        harness.Pump(new Size(500, 320));
        controller.Open();
        harness.Pump(new Size(500, 320));

        Assert.NotNull(FindParagraph(harness.RenderView, "Ctrl+P"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Alt+L"));
    }

    [Fact]
    public void MenuItemButton_TakesFocusOnPointerHoverAndOnlyOnTheHoverEdge()
    {
        // Flutter uses MouseRegion.onHover rather than onEnter so that a button scrolling under a
        // stationary pointer does not steal focus; the callback is edge-detected by `_isHovered`.
        var hovers = new List<bool>();
        var focusNode = new FocusNode();
        var controller = new MenuController();
        Widget anchor = new MenuAnchor(
            [
                new MenuItemButton(
                    child: new Text("Save"),
                    onPressed: () => { },
                    focusNode: focusNode,
                    onHover: hovers.Add),
            ],
            controller: controller,
            child: new SizedBox(width: 80, height: 40));
        using var harness = new WidgetRenderHarness(Wrap(anchor));
        harness.Pump(new Size(500, 320));
        controller.Open();
        harness.Pump(new Size(500, 320));

        RenderParagraph label = FindParagraph(harness.RenderView, "Save")!;
        Point inside = GlobalCenter(label);
        Assert.False(focusNode.HasFocus);

        harness.SendPointer(HoverAt(inside));
        harness.Pump(new Size(500, 320));
        Assert.Equal([true], hovers);
        Assert.True(focusNode.HasFocus);

        // A second hover inside the same item is not a new edge.
        harness.SendPointer(HoverAt(new Point(inside.X + 1, inside.Y)));
        Assert.Equal([true], hovers);

        harness.SendPointer(HoverAt(new Point(inside.X, inside.Y + 400)));
        Assert.Equal([true, false], hovers);
    }

    [Fact]
    public void MenuItemButton_HoverDoesNotFocusADisabledItem()
    {
        // Flutter's `_handlePointerHover` requests focus without checking `enabled`; the disabled
        // button simply has no focusable subtree, so the request is a no-op.
        var hovers = new List<bool>();
        var focusNode = new FocusNode();
        var controller = new MenuController();
        Widget anchor = new MenuAnchor(
            [new MenuItemButton(child: new Text("Save"), focusNode: focusNode, onHover: hovers.Add)],
            controller: controller,
            child: new SizedBox(width: 80, height: 40));
        using var harness = new WidgetRenderHarness(Wrap(anchor, NonAppleTheme));
        harness.Pump(new Size(500, 320));
        controller.Open();
        harness.Pump(new Size(500, 320));

        harness.SendPointer(HoverAt(GlobalCenter(FindParagraph(harness.RenderView, "Save")!)));
        harness.Pump(new Size(500, 320));

        Assert.Equal([true], hovers);
        Assert.False(focusNode.HasFocus);
    }

    [Fact]
    public void MenuItemButton_ExposesFlutterDefaultsAndTheSourceStyleHooks()
    {
        var button = new MenuItemButton(child: new Text("Item"));

        Assert.False(button.Enabled);
        Assert.True(button.RequestFocusOnHover);
        Assert.True(button.CloseOnActivate);
        Assert.False(button.Autofocus);
        Assert.Equal(Clip.None, button.ClipBehavior);
        Assert.Equal(Axis.Horizontal, button.OverflowAxis);
        Assert.Null(button.SemanticsLabel);
        Assert.Null(button.Shortcut);
        Assert.Null(new CheckboxMenuButton(false, _ => { }, new Text("Item")).Shortcut);
        Assert.Null(new RadioMenuButton<string>("a", "a", _ => { }, new Text("Item")).Shortcut);

        // Flutter's `defaultStyleOf`/`themeStyleOf` protocol, so a `MenuButtonTheme` can layer over
        // `_MenuButtonDefaultsM3` without the button re-deriving either.
        Assert.IsType<MenuItemButtonState>(button.CreateState());
    }

    [Fact]
    public void SubmenuButton_ExposesFlutterDefaultsAndValidatesHoverDelay()
    {
        var button = new SubmenuButton([], null);
        Assert.Null(button.Child);
        Assert.Empty(button.MenuChildren);
        Assert.Equal(Clip.HardEdge, button.ClipBehavior);
        Assert.Equal(TimeSpan.Zero, button.HoverOpenDelay);
        Assert.False(button.Enabled);
        Assert.False(button.UseRootOverlay);
        Assert.False(button.Animated);
        Assert.Throws<ArgumentOutOfRangeException>(() => new SubmenuButton(
            [],
            null,
            hoverOpenDelay: TimeSpan.FromMilliseconds(-1)));
    }

    [Fact]
    public void CheckboxAndRadioMenuButtons_ExposeFlutterDefaultsAndContracts()
    {
        var checkbox = new CheckboxMenuButton(false, _ => { }, new Text("Check"));
        Assert.False(checkbox.Value);
        Assert.False(checkbox.Tristate);
        Assert.False(checkbox.IsError);
        Assert.True(checkbox.Enabled);
        Assert.Equal(Clip.None, checkbox.ClipBehavior);
        Assert.True(checkbox.CloseOnActivate);
        Assert.Null(checkbox.TrailingIcon);

        var radio = new RadioMenuButton<string>("one", "two", _ => { }, new Text("Radio"));
        Assert.Equal("one", radio.Value);
        Assert.Equal("two", radio.GroupValue);
        Assert.False(radio.Toggleable);
        Assert.True(radio.Enabled);
        Assert.Equal(Clip.None, radio.ClipBehavior);
        Assert.True(radio.CloseOnActivate);
        Assert.Null(radio.TrailingIcon);

        Assert.Throws<ArgumentException>(() => new CheckboxMenuButton(null, _ => { }, new Text("Invalid")));
        Assert.False(new CheckboxMenuButton(false, null, new Text("Disabled")).Enabled);
        Assert.False(new RadioMenuButton<string>("one", null, null, new Text("Disabled")).Enabled);
    }

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(true, true, null)]
    [InlineData(null, true, false)]
    public void CheckboxMenuButton_CyclesValuesAndHonorsClosePolicy(
        bool? value,
        bool tristate,
        bool? expected)
    {
        var controller = new MenuController();
        bool? changed = null;
        bool invoked = false;
        using var harness = new WidgetRenderHarness(Wrap(new MenuAnchor(
            controller: controller,
            child: new SizedBox(width: 80, height: 40),
            menuChildren:
            [
                new CheckboxMenuButton(
                    value,
                    next =>
                    {
                        changed = next;
                        invoked = true;
                    },
                    new Text("Toggle option"),
                    tristate: tristate,
                    closeOnActivate: false),
            ])));
        harness.Pump(new Size(500, 360));
        controller.Open();
        var semantics = harness.PumpAndGetSemantics(new Size(500, 360));

        Assert.Equal(1, CountSemantics(semantics, node => node.Actions.HasFlag(SemanticsActions.Tap)));
        var item = FindSemantics(semantics, node => node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(item);
        Assert.True(item!.PerformAction(SemanticsActions.Tap));
        Scheduler.PumpFrameForTests();
        harness.Pump(new Size(500, 360));

        Assert.True(invoked);
        Assert.Equal(expected, changed);
        Assert.True(controller.IsOpen);
        Assert.Contains(
            FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => Close(box.AdditionalConstraints.MaxWidth, Checkbox.Width)
                   && Close(box.AdditionalConstraints.MaxHeight, Checkbox.Width));
    }

    [Fact]
    public void RadioMenuButton_SelectsOrTogglesAndDisabledItemHasNoTapAction()
    {
        var controller = new MenuController();
        string? changed = "unchanged";
        using var harness = new WidgetRenderHarness(Wrap(new MenuAnchor(
            controller: controller,
            child: new SizedBox(width: 80, height: 40),
            menuChildren:
            [
                new RadioMenuButton<string>(
                    "one",
                    "one",
                    value => changed = value,
                    new Text("Selected radio"),
                    toggleable: true),
                new RadioMenuButton<string>("two", "one", null, new Text("Disabled radio")),
            ])));
        harness.Pump(new Size(500, 360));
        controller.Open();
        var semantics = harness.PumpAndGetSemantics(new Size(500, 360));

        Assert.Equal(1, CountSemantics(semantics, node => node.Actions.HasFlag(SemanticsActions.Tap)));
        var item = FindSemantics(semantics, node => node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(item);
        Assert.True(item!.PerformAction(SemanticsActions.Tap));
        Scheduler.PumpFrameForTests();
        harness.Pump(new Size(500, 360));

        Assert.Null(changed);
        Assert.False(controller.IsOpen);
    }

    [Fact]
    public void CheckboxAndRadioMenuButtons_DoNotCrashAtZeroArea()
    {
        using var checkbox = new WidgetRenderHarness(Wrap(new Center(
            child: new SizedBox(
                width: 0,
                height: 0,
                child: new CheckboxMenuButton(true, _ => { }, new Text("X"))))));
        checkbox.Pump(new Size(500, 360));

        using var radio = new WidgetRenderHarness(Wrap(new Center(
            child: new SizedBox(
                width: 0,
                height: 0,
                child: new RadioMenuButton<bool>(true, true, _ => { }, null)))));
        radio.Pump(new Size(500, 360));

        Assert.Contains(
            FindDescendants<RenderConstrainedBox>(checkbox.RenderView),
            box => box.AdditionalConstraints == BoxConstraints.Tight(new Size(0, 0)));
        Assert.Contains(
            FindDescendants<RenderConstrainedBox>(radio.RenderView),
            box => box.AdditionalConstraints == BoxConstraints.Tight(new Size(0, 0)));
    }

    [Fact]
    public void DropdownDemoPage_BuildsWithValidCheckboxMenuInitialValue()
    {
        using var harness = new WidgetRenderHarness(Wrap(new DropdownDemoPage()));
        harness.Pump(new Size(900, 1400));

        Assert.NotNull(FindParagraph(harness.RenderView, "MenuAnchor + MenuItemButton"));
    }

    [Fact]
    public void MenuPanelDefaults_MatchTheSourceM3TokenTableForBothOrientations()
    {
        // Flutter's "Menu defaults": the menu-bar strip and the dropped-down panel agree on
        // background/shadow/tint/elevation/shape, and every value comes from the ColorScheme.
        var theme = ThemeData.Light;
        var controller = new MenuController();
        Widget bar = new MenuBar(
            [new SubmenuButton(
                [new MenuItemButton(child: new Text("Open"), onPressed: () => { })],
                new Text("File"),
                controller: controller)]);

        using var harness = new WidgetRenderHarness(Wrap(bar, theme));
        harness.Pump(new Size(500, 240));
        List<RenderDecoratedBox> closed = MenuPanels(harness);
        controller.Open();
        harness.Pump(new Size(500, 240));
        List<RenderDecoratedBox> opened = MenuPanels(harness);

        Assert.Single(closed);
        Assert.Equal(2, opened.Count);
        foreach (RenderDecoratedBox panel in opened)
        {
            Assert.Equal(theme.ColorScheme.SurfaceContainer, panel.Decoration.Color);
            Assert.Equal(4.0, panel.Decoration.EffectiveBorderRadius.Radius);

            // elevation 3.0 draws its key and ambient shadows from the scheme's shadow role.
            Assert.All(panel.Decoration.BoxShadows!, shadow =>
            {
                Assert.Equal(theme.ColorScheme.Shadow.R, shadow.Color.R);
                Assert.Equal(theme.ColorScheme.Shadow.G, shadow.Color.G);
                Assert.Equal(theme.ColorScheme.Shadow.B, shadow.Color.B);
            });
        }
    }

    [Fact]
    public void MenuPanelDefaults_UseTheSourceDirectionalPaddingAndAlignment()
    {
        // `_MenuBarDefaultsM3` pads 4 horizontally and aligns bottomStart; `_MenuDefaultsM3` pads 8
        // vertically and aligns topEnd. Those are the only two fields that differ between them.
        MenuStyle bar = CaptureMenuStyleDefaults(horizontal: true);
        MenuStyle menu = CaptureMenuStyleDefaults(horizontal: false);

        Assert.Equal(
            new Thickness(4.0, 0.0, 4.0, 0.0),
            bar.Padding!.Resolve(MaterialState.None)!.Value.Resolve(TextDirection.Ltr));
        Assert.Equal(
            new Thickness(0.0, 8.0, 0.0, 8.0),
            menu.Padding!.Resolve(MaterialState.None)!.Value.Resolve(TextDirection.Ltr));
        Assert.Equal((AlignmentGeometry)AlignmentDirectional.BottomStart, bar.Alignment);
        Assert.Equal((AlignmentGeometry)AlignmentDirectional.TopEnd, menu.Alignment);

        // Everything else is shared, and min/fixed/max size and side stay unset in both tables.
        Assert.Equal(3.0, bar.Elevation!.Resolve(MaterialState.None));
        Assert.Equal(3.0, menu.Elevation!.Resolve(MaterialState.None));
        Assert.Equal(MaterialColors.Transparent, menu.SurfaceTintColor!.Resolve(MaterialState.None));
        foreach (MenuStyle style in new[] { bar, menu })
        {
            Assert.Null(style.MinimumSize);
            Assert.Null(style.FixedSize);
            Assert.Null(style.MaximumSize);
            Assert.Null(style.Side);
            Assert.Null(style.MouseCursor);
        }
    }

    [Fact]
    public void MenuPanel_FoldsTheResolvedSideIntoTheResolvedShape()
    {
        // Flutter's "Material parameters are honored": `shape!.copyWith(side: side)` runs even when
        // the shape itself came from the defaults, so a theme setting only `side` still draws it.
        Color outlineColor = Color.Parse("#FFD81B60");
        ThemeData theme = ThemeData.Light with
        {
            MenuBarTheme = new MenuBarThemeData(new MenuStyle(
                side: MaterialStateProperty<BorderSide?>.All(new BorderSide(outlineColor, 3.0)))),
        };

        using var harness = new WidgetRenderHarness(Wrap(
            new MenuBar([new MenuItemButton(child: new Text("Only"), onPressed: () => { })]),
            theme));
        harness.Pump(new Size(500, 240));

        // `Material` paints the outline as a separate foreground shape over the filled background,
        // so the fold shows up as an outline box carrying the default 4px radius.
        RenderDecoratedBox outline = Assert.Single(
            FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Border is not null && box.Decoration.Color is null);
        Assert.Equal(4.0, outline.Decoration.EffectiveBorderRadius.Radius);
        Assert.Equal(outlineColor, outline.Decoration.Border!.Top.Color);
        Assert.Equal(3.0, outline.Decoration.Border.Top.Width);
    }

    [Fact]
    public void MenuPanel_ClampsAFixedSizeInsideTheMinimumAndMaximumWindow()
    {
        // Flutter's "fixedSize/maximumSize/minimumSize affects geometry": the fixed size is run
        // through the min/max constraints before it tightens them.
        ThemeData theme = ThemeData.Light with
        {
            MenuBarTheme = new MenuBarThemeData(new MenuStyle(
                fixedSize: MaterialStateProperty<Size?>.All(new Size(600.0, 60.0)),
                maximumSize: MaterialStateProperty<Size?>.All(new Size(250.0, 40.0)))),
        };

        BoxConstraints constraints = MenuAnchorState.ResolveMenuConstraints(
            new MenuStyle(
                fixedSize: MaterialStateProperty<Size?>.All(new Size(600.0, 60.0)),
                maximumSize: MaterialStateProperty<Size?>.All(new Size(250.0, 40.0))),
            MaterialState.None,
            VisualDensity.Standard);

        Assert.Equal(250.0, constraints.MaxWidth);
        Assert.Equal(250.0, constraints.MinWidth);
        Assert.Equal(40.0, constraints.MaxHeight);
        Assert.Equal(40.0, constraints.MinHeight);

        using var harness = new WidgetRenderHarness(Wrap(
            new Align(
                alignment: Alignment.TopLeft,
                child: new MenuBar(
                    [new MenuItemButton(child: new Text("Fixed"), onPressed: () => { })])),
            theme));
        harness.Pump(new Size(500, 240));
        Assert.Equal(250.0, Assert.Single(MenuPanels(harness)).Size.Width, 3);
    }

    [Fact]
    public void MenuThemes_WrapRebuildsTheInheritedThemeAroundACapturedChild()
    {
        // All three menu themes are `InheritedTheme`s in Dart, so a captured theme can be replayed
        // into an overlay or route subtree.
        var menuData = new MenuThemeData(new MenuStyle(
            elevation: MaterialStateProperty<double?>.All(7.0)));
        var barData = new MenuBarThemeData(new MenuStyle(
            elevation: MaterialStateProperty<double?>.All(8.0)));
        var buttonData = new MenuButtonThemeData(new ButtonStyle(
            Elevation: MaterialStateProperty<double?>.All(9.0)));
        var leaf = new SizedBox();

        Assert.Equal(
            menuData,
            Assert.IsType<MenuTheme>(new MenuTheme(menuData, leaf).Wrap(default, leaf)).Data);
        Assert.Equal(
            barData,
            Assert.IsType<MenuBarTheme>(new MenuBarTheme(barData, leaf).Wrap(default, leaf)).Data);
        Assert.Equal(
            buttonData,
            Assert.IsType<MenuButtonTheme>(
                new MenuButtonTheme(buttonData, leaf).Wrap(default, leaf)).Data);
    }

    private static MenuStyle CaptureMenuStyleDefaults(bool horizontal)
    {
        MenuStyle? captured = null;
        using var harness = new WidgetRenderHarness(Wrap(new Builder(context =>
        {
            captured = horizontal ? new MenuBarDefaultsM3(context) : new MenuDefaultsM3(context);
            return new SizedBox();
        })));
        harness.Pump(new Size(500, 360));
        Assert.NotNull(captured);
        return captured;
    }

    /// <summary>The decorated boxes a menu panel's `Material` paints, in tree order.</summary>
    private static List<RenderDecoratedBox> MenuPanels(WidgetRenderHarness harness) =>
        FindDescendants<RenderDecoratedBox>(harness.RenderView)
            .Where(box => box.Decoration.BoxShadows is { Count: > 0 })
            .ToList();

    private static Widget Wrap(
        Widget child,
        ThemeData? theme = null,
        TextDirection direction = TextDirection.Ltr) =>
        // `MaterialApp` is what installs the traversal scope in Flutter's own tests; this helper
        // stands in for it, and the menu's arrow-key navigation moves nothing without it.
        AppTraversalScope.Wrap(new Directionality(
            direction,
            new MediaQuery(
                new MediaQueryData(Size: new Size(500, 360)),
                new Theme(
                    theme ?? ThemeData.Light,
                    new Overlay(initialEntries: [new OverlayEntry(_ => child)])))));

    private static Color Opacity(Color color, double opacity) =>
        color.WithOpacity(opacity);

    private static ButtonStyle CaptureMenuButtonDefaults(ThemeData theme, double textScaleFactor = 1.0)
    {
        ButtonStyle? captured = null;
        using var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Ltr,
            new MediaQuery(
                new MediaQueryData(Size: new Size(500, 360), TextScaleFactor: textScaleFactor),
                new Theme(
                    theme,
                    new Builder(context =>
                    {
                        captured = MenuButtonDefaults.M3(context);
                        return new SizedBox();
                    })))));
        harness.Pump(new Size(500, 360));
        Assert.NotNull(captured);
        return captured;
    }

    private static void PumpAnimation()
    {
        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.35));
    }

    private static async Task WaitForConditionAsync(Func<bool> condition)
    {
        for (int i = 0; i < 100 && !condition(); i++) await Task.Delay(10);
    }

    private static RenderParagraph? FindParagraph(RenderObject? root, string text) =>
        FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.PlainText == text);

    private static bool Close(double a, double b) => Math.Abs(a - b) < 0.001;

    /// <summary>`ThemeData.Light` resolves its platform from the host, so label tests pin it.</summary>
    private static ThemeData NonAppleTheme => ThemeData.Light with { Platform = TargetPlatform.Linux };

    private static double GlobalLeft(RenderBox box) => box.LocalToGlobal(default).X;

    private static Point GlobalCenter(RenderBox box) =>
        box.LocalToGlobal(new Point(box.Size.Width / 2.0, box.Size.Height / 2.0));

    private static PointerHoverEvent HoverAt(Point position) => new(
        pointer: 1,
        kind: PointerDeviceKind.Mouse,
        position: position,
        buttons: PointerButtons.None,
        timestampUtc: DateTime.UtcNow);

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

    private static int CountSemantics(SemanticsNode? node, Func<SemanticsNode, bool> predicate)
    {
        if (node is null) return 0;
        int count = predicate(node) ? 1 : 0;
        return count + node.Children.Sum(child => CountSemantics(child, predicate));
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

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public void SendPointer(PointerEvent @event)
        {
            GestureBinding.Instance.HandlePointerEvent(RenderView, @event);
            _owner.FlushBuild();
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
}
