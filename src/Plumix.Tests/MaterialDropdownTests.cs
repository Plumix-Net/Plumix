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
public sealed class MaterialDropdownTests : IDisposable
{
    public MaterialDropdownTests()
    {
        Scheduler.ResetForTests();
        FocusManager.Instance.ResetForTests();
        GestureBinding.Instance.ResetForTests();
    }

    public void Dispose()
    {
        GestureBinding.Instance.ResetForTests();
        FocusManager.Instance.ResetForTests();
        Scheduler.ResetForTests();
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
        Assert.True(button.BarrierDismissible);
        Assert.False(button.Autofocus);
        Assert.Equal((AlignmentGeometry)AlignmentDirectional.CenterStart, button.Alignment);

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
        Assert.Throws<ArgumentOutOfRangeException>(() => new DropdownButton<string>([item], _ => { }, iconSize: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DropdownButton<string>([item], _ => { }, menuWidth: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DropdownButton<string>([item], _ => { }, menuMaxHeight: -1));
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
        var itemAlign = Assert.Single(FindDescendants<RenderAlign>(menuItemHarness.RenderView));
        Assert.Equal(new Alignment(expectedX, 1.0), itemAlign.Alignment);

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
        Assert.Equal(new Alignment(expectedX, 1.0), indexedStack.Alignment);
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

        using var disabled = new WidgetRenderHarness(Wrap(
            new DropdownButton<string>(items, null,
                hint: new Text("Fallback"),
                disabledHint: new Text("Disabled"))));
        var semantics = disabled.PumpAndGetSemantics(new Size(400, 160));
        Assert.NotNull(FindParagraph(disabled.RenderView, "Disabled"));
        Assert.Equal(2, Assert.Single(FindDescendants<RenderIndexedStack>(disabled.RenderView)).Index);
        var disabledNode = FindSemantics(semantics, node => node.Flags.HasFlag(SemanticsFlags.IsButton));
        Assert.NotNull(disabledNode);
        Assert.False(disabledNode!.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.False(disabledNode.Actions.HasFlag(SemanticsActions.Tap));
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
                        padding: new Thickness(3))))));
        harness.Pump(new Size(400, 160));

        Assert.NotNull(FindParagraph(harness.RenderView, "Selected two"));
        Assert.Equal(1, Assert.Single(FindDescendants<RenderIndexedStack>(harness.RenderView)).Index);
        Assert.Contains(FindDescendants<RenderPadding>(harness.RenderView), value => value.Padding == new Thickness(3));
        Assert.Contains(FindDescendants<RenderPadding>(harness.RenderView), value => value.Padding == new Thickness(16, 0, 4, 0));
        Assert.DoesNotContain(FindDescendants<RenderColoredBox>(harness.RenderView),
            box => box.Color == Color.Parse("#FFBDBDBD"));

        Assert.Throws<InvalidOperationException>(() => new WidgetRenderHarness(Wrap(
            new DropdownButton<string>(
                items,
                _ => { },
                selectedItemBuilder: _ => [new Text("Only one")],
                value: "one"))));
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

        var layout = Assert.Single(FindDescendants<RenderDropdownMenuPositionLayout<string>>(harness.RenderView));
        Assert.Equal(190, layout.Child!.Size.Width, precision: 3);
        Assert.True(layout.Child.Size.Height <= 120.01);
        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView), box =>
            box.Decoration.Color == Colors.Orange
            && box.Decoration.EffectiveBorderRadius == BorderRadius.Circular(9));
        Assert.NotNull(FindSemantics(openedSemantics, node =>
            node.Role == SemanticsRole.Menu
            && node.Label == "Popup menu"));

        Assert.True(FocusManager.Instance.HandleKeyEvent(new KeyEvent("ArrowDown", true)));
        Assert.True(FocusManager.Instance.HandleKeyEvent(new KeyEvent("Enter", true)));
        PumpAnimation();
        harness.Pump(new Size(500, 360));
        Assert.Equal("three", selected);
        Assert.Equal(0, firstTap);
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

        Assert.True(FocusManager.Instance.HandleKeyEvent(new KeyEvent("ArrowUp", true)));
        Assert.True(FocusManager.Instance.HandleKeyEvent(new KeyEvent("Enter", true)));
        PumpAnimation();
        harness.Pump(new Size(500, 360));
        Assert.True(changed);
        Assert.Null(value);
        Assert.Equal(1, itemTap);
    }

    [Fact]
    public void DropdownButton_MenuUsesThreeStageRevealAndMeasuresVariableItemHeights()
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

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.03));
        harness.Pump(new Size(500, 360));
        var reveal = Assert.Single(FindDescendants<RenderDropdownMenuReveal>(harness.RenderView));
        Assert.InRange(reveal.RevealRect.Height, 47.9, 48.1);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.09));
        harness.Pump(new Size(500, 360));
        reveal = Assert.Single(FindDescendants<RenderDropdownMenuReveal>(harness.RenderView));
        Assert.True(reveal.RevealRect.Height > 48);

        PumpAnimation();
        harness.Pump(new Size(500, 360));
        var layout = Assert.Single(FindDescendants<RenderDropdownMenuPositionLayout<string>>(harness.RenderView));
        Assert.True(layout.Route.ItemHeights[0] >= 72);
        Assert.Equal(layout.Child!.Size.Height, reveal.Size.Height, precision: 3);
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
            Assert.True(FocusManager.Instance.HandleKeyEvent(new KeyEvent("Enter", true)));
            PumpAnimation();
            var semantics = harness.PumpAndGetSemantics(new Size(500, 360));
            Assert.NotNull(FindSemantics(semantics, node => node.Role == SemanticsRole.Menu));
        }
        focusNode.Dispose();
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

        Assert.Throws<ArgumentException>(() => new DropdownButtonFormField<string>(items, _ => { }, initialValue: "missing"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DropdownButtonFormField<string>(items, _ => { }, itemHeight: 47));
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
        Assert.Contains(FindDescendants<RenderParagraph>(harness.RenderView), value => value.Text == "Choose another");

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
    public void DropdownMenuAndEntry_ExposeFlutterDefaultsAndValidateContracts()
    {
        var entries = new[] { new DropdownMenuEntry<string>("one", "One") };
        var menu = new DropdownMenu<string>(entries);
        Assert.True(menu.Enabled);
        Assert.True(menu.ShowTrailingIcon);
        Assert.False(menu.EnableFilter);
        Assert.True(menu.EnableSearch);
        Assert.False(menu.SelectOnly);
        Assert.Equal(DropdownMenuCloseBehavior.All, menu.CloseBehavior);
        Assert.Equal(1, menu.MaxLines);
        Assert.Equal(new Thickness(20), menu.ScrollPadding);
        Assert.True(entries[0].Enabled);

        Assert.Throws<ArgumentOutOfRangeException>(() => new DropdownMenu<string>(entries, width: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DropdownMenu<string>(entries, menuHeight: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DropdownMenu<string>(entries, maxLines: 0));
        Assert.Throws<ArgumentException>(() => new DropdownMenu<string>(entries,
            filterCallback: (items, _) => items));
        Assert.Throws<ArgumentException>(() => new DropdownMenu<string>(entries,
            showTrailingIcon: false,
            trailingIconFocusNode: new FocusNode()));
        Assert.Throws<ArgumentException>(() => new DropdownMenu<string>(entries,
            label: new Text("Label"),
            decorationBuilder: (_, _) => new InputDecoration()));
        var unattachedController = new MenuController();
        unattachedController.Open();
        Assert.False(unattachedController.IsOpen);
    }

    [Fact]
    public void DropdownMenu_InitialSelectionThemeAndPreferredWidthMatchSourceComposition()
    {
        var controller = new TextEditingController();
        var background = Color.Parse("#FF123456");
        using var harness = new WidgetRenderHarness(Wrap(new DropdownMenuTheme(
            new DropdownMenuThemeData(
                TextStyle: new TextStyle(FontSize: 19, Color: Colors.Purple),
                MenuStyle: new MenuStyle(BackgroundColor: MaterialStateProperty<Color?>.All(background))),
            new DropdownMenu<string>(
                dropdownMenuEntries:
                [
                    new DropdownMenuEntry<string>("short", "Short"),
                    new DropdownMenuEntry<string>("long", "A much wider menu label"),
                ],
                controller: controller,
                initialSelection: "short"))));
        harness.Pump(new Size(500, 180));

        Assert.Equal("Short", controller.Text);
        var body = Assert.Single(FindDescendants<RenderDropdownMenuBody>(harness.RenderView));
        Assert.True(body.Size.Width >= 112);
        Assert.Contains(FindDescendants<RenderParagraph>(harness.RenderView), paragraph =>
            paragraph.Text == "Short" && Close(paragraph.FontSize, 19));
    }

    [Fact]
    public void DropdownMenu_OpensBelowAnchorWithResolvedMenuStyleAndExpandedSemantics()
    {
        var background = Color.Parse("#FF345678");
        var shadow = Color.Parse("#FF112233");
        var side = new BorderSide(Colors.Red, 2);
        Widget page = new Align(
            alignment: Alignment.TopLeft,
            child: new DropdownMenu<string>(
                dropdownMenuEntries:
                [
                    new DropdownMenuEntry<string>("one", "One"),
                    new DropdownMenuEntry<string>("two", "Two"),
                ],
                width: 210,
                menuHeight: 120,
                menuStyle: new MenuStyle(
                    BackgroundColor: MaterialStateProperty<Color?>.All(background),
                    ShadowColor: MaterialStateProperty<Color?>.All(shadow),
                    Elevation: MaterialStateProperty<double?>.All(5),
                    Padding: MaterialStateProperty<Thickness?>.All(new Thickness(0, 6)),
                    Side: MaterialStateProperty<BorderSide?>.All(side),
                    Shape: MaterialStateProperty<ShapeBorder?>.All(ShapeBorder.RoundedRectangle(11)))));
        using var harness = new WidgetRenderHarness(Wrap(new Navigator(new BuilderPageRoute(_ => page))));
        var semantics = harness.PumpAndGetSemantics(new Size(500, 360));
        var anchor = FindSemantics(semantics, node =>
            node.Flags.HasFlag(SemanticsFlags.HasExpandedState)
            && node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(anchor);
        Assert.True(anchor!.PerformAction(SemanticsActions.Tap));
        PumpAnimation();
        semantics = harness.PumpAndGetSemantics(new Size(500, 360));

        // The menu route's modal barrier blocks the anchor's semantics while the menu is open, so the expanded
        // state is observed on the route instead; the barrier itself carries the dismiss label.
        Assert.NotNull(FindSemantics(semantics, node => node.Label == "Dismiss"));
        var layout = Assert.Single(FindDescendants<RenderDropdownMenuPositionLayout<string>>(harness.RenderView));
        Assert.True(layout.Route.MenuBelowAnchor);
        Assert.Equal(210, layout.Child!.Size.Width, precision: 3);
        Assert.True(layout.Child.Size.Height <= 120.01);
        Assert.True(((BoxParentData)layout.Child.parentData!).offset.Y >= layout.Route.ButtonRect.Bottom - 0.01);
        Assert.Equal(shadow, layout.Route.ShadowColor);
        Assert.Equal(side, layout.Route.Side);
        Assert.Equal(new Thickness(0, 6), layout.Route.MenuPadding);
        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView), box =>
            box.Decoration.Color == background
            && box.Decoration.Border == side
            && box.Decoration.EffectiveBorderRadius == BorderRadius.Circular(11));
    }

    [Fact]
    public async Task DropdownMenu_FilterSearchAndKeyboardTraversalSkipDisabledEntries()
    {
        string? selected = null;
        var controller = new TextEditingController();
        using var harness = new WidgetRenderHarness(Wrap(new Navigator(new BuilderPageRoute(_ =>
            new DropdownMenu<string>(
                dropdownMenuEntries:
                [
                    new DropdownMenuEntry<string>("one", "One"),
                    new DropdownMenuEntry<string>("two", "Two", enabled: false),
                    new DropdownMenuEntry<string>("three", "Three"),
                ],
                controller: controller,
                enableFilter: true,
                onSelected: value => selected = value)))));
        var semantics = harness.PumpAndGetSemantics(new Size(500, 360));
        Assert.True(FindSemantics(semantics, node =>
                node.Flags.HasFlag(SemanticsFlags.HasExpandedState)
                && node.Actions.HasFlag(SemanticsActions.Tap))!
            .PerformAction(SemanticsActions.Tap));
        Assert.True(FocusManager.Instance.HandleTextInput("T"));
        PumpAnimation();
        semantics = harness.PumpAndGetSemantics(new Size(500, 360));
        Assert.NotNull(FindSemantics(semantics, node => node.Role == SemanticsRole.Menu));
        Assert.Equal(2, Assert.Single(
            FindDescendants<RenderDropdownMenuPositionLayout<string>>(harness.RenderView)).Route.ItemHeights.Length);

        Assert.True(FocusManager.Instance.HandleKeyEvent(new KeyEvent("ArrowDown", true)));
        Assert.Equal("Three", controller.Text);
        Assert.True(FocusManager.Instance.HandleKeyEvent(new KeyEvent("Enter", true)));
        PumpAnimation();
        harness.Pump(new Size(500, 360));
        await WaitForConditionAsync(() => selected is not null);
        Assert.Equal("three", selected);
    }

    [Fact]
    public void DropdownMenu_CloseBehaviorNoneKeepsMenuOpenAfterPointerSelection()
    {
        string selected = string.Empty;
        using var harness = new WidgetRenderHarness(Wrap(new Navigator(new BuilderPageRoute(_ =>
            new DropdownMenu<string>(
                dropdownMenuEntries: [new DropdownMenuEntry<string>("one", "One")],
                closeBehavior: DropdownMenuCloseBehavior.None,
                onSelected: value => selected = value ?? string.Empty)))));
        var semantics = harness.PumpAndGetSemantics(new Size(500, 360));
        Assert.True(FindSemantics(semantics, node => node.Flags.HasFlag(SemanticsFlags.HasExpandedState))!
            .PerformAction(SemanticsActions.Tap));
        PumpAnimation();
        semantics = harness.PumpAndGetSemantics(new Size(500, 360));
        var item = FindSemantics(semantics, node => node.Role == SemanticsRole.MenuItem
            && node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(item);
        Assert.True(item!.PerformAction(SemanticsActions.Tap));
        harness.Pump(new Size(500, 360));
        Assert.Equal("one", selected);
        Assert.NotNull(FindDescendants<RenderDropdownMenuPositionLayout<string>>(harness.RenderView).SingleOrDefault());
    }

    [Fact]
    public void DropdownMenu_ExternalMenuControllerOpensAndClosesRoute()
    {
        var controller = new MenuController();
        using var harness = new WidgetRenderHarness(Wrap(new Navigator(new BuilderPageRoute(_ =>
            new DropdownMenu<string>(
                dropdownMenuEntries: [new DropdownMenuEntry<string>("one", "One")],
                menuController: controller)))));
        harness.Pump(new Size(500, 360));
        Assert.False(controller.IsOpen);
        controller.Open();
        PumpAnimation();
        harness.Pump(new Size(500, 360));
        Assert.True(controller.IsOpen);
        Assert.Single(FindDescendants<RenderDropdownMenuPositionLayout<string>>(harness.RenderView));
        controller.CloseChildren();
        Assert.True(controller.IsOpen);

        controller.Close();
        Assert.False(controller.IsOpen);
    }

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
                BackgroundColor: MaterialStateProperty<Color?>.All(themeBackground))),
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
            new MenuBarThemeData(new MenuStyle(BackgroundColor: MaterialStateProperty<Color?>.All(localBackground))),
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
            style: new MenuStyle(BackgroundColor: MaterialStateProperty<Color?>.All(widgetBackground)));
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
                BackgroundColor: MaterialStateProperty<Color?>.All(globalBackground))),
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
                BackgroundColor: MaterialStateProperty<Color?>.All(localBackground))),
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
                BackgroundColor: MaterialStateProperty<Color?>.All(localBackground))),
            new MenuAnchor(
                [new MenuItemButton(child: new Text("Widget"), onPressed: () => { })],
                child: new SizedBox(width: 80, height: 40),
                controller: widgetController,
                style: new MenuStyle(
                    BackgroundColor: MaterialStateProperty<Color?>.All(widgetBackground)))), theme));
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
                SubmenuIcon: MaterialStateProperty<Widget?>.All(new Text("theme icon"))),
        };

        Widget themed = new MenuTheme(
            new MenuThemeData(
                SubmenuIcon: MaterialStateProperty<Widget?>.All(new Text("local icon"))),
            new MenuBar(
            [
                new SubmenuButton(
                    [new MenuItemButton(child: new Text("Open"), onPressed: () => { })],
                    new Text("Local")),
                new SubmenuButton(
                    [new MenuItemButton(child: new Text("Save"), onPressed: () => { })],
                    new Text("Widget"),
                    submenuIcon: MaterialStateProperty<Widget?>.All(new Text("widget icon"))),
            ]));
        using var harness = new WidgetRenderHarness(Wrap(themed, theme));
        harness.Pump(new Size(500, 180));

        Assert.NotNull(FindParagraph(harness.RenderView, "local icon"));
        Assert.NotNull(FindParagraph(harness.RenderView, "widget icon"));
        Assert.Null(FindParagraph(harness.RenderView, "theme icon"));
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
    public void DropdownMenuFormField_ValidationSelectionSaveAndResetSynchronizeController()
    {
        var entries = new[]
        {
            new DropdownMenuEntry<string>("one", "One"),
            new DropdownMenuEntry<string>("two", "Two"),
        };
        var controller = new TextEditingController();
        var callbacks = new List<string>();
        string? saved = null;
        FormState? form = null;
        using var harness = new WidgetRenderHarness(Wrap(new Form(
            onChanged: () => callbacks.Add("form"),
            child: new Builder(context =>
            {
                form = Form.Of(context);
                return new DropdownMenuFormField<string>(
                    dropdownMenuEntries: entries,
                    controller: controller,
                    initialSelection: "one",
                    onSelected: value => callbacks.Add($"field:{value}"),
                    onSaved: value => saved = value,
                    validator: value => value == "one" ? "Choose another" : null);
            }))));
        harness.Pump(new Size(500, 180));
        var state = Assert.IsType<DropdownMenuFormFieldState<string>>(Assert.Single(form!.Fields));
        Assert.Equal("One", controller.Text);
        Assert.False(form.Validate());
        harness.Pump(new Size(500, 200));
        Assert.NotNull(FindParagraph(harness.RenderView, "Choose another"));

        state.DidChange("two");
        Assert.Equal("Two", controller.Text);
        Assert.Equal(new[] { "form", "field:two" }, callbacks.TakeLast(2));
        Assert.True(form.Validate());
        form.Save();
        Assert.Equal("two", saved);

        form.Reset();
        harness.Pump(new Size(500, 180));
        Assert.Equal("one", state.Value);
        Assert.Equal("One", controller.Text);
        Assert.Equal(new[] { "form", "field:one", "form" }, callbacks.TakeLast(3));
    }

    [Fact]
    public void DropdownMenuFormField_CustomErrorBuilderAndNullResetClearText()
    {
        var key = new LabeledGlobalKey<DropdownMenuFormFieldState<string>>("modern-dropdown");
        using var harness = new WidgetRenderHarness(Wrap(new DropdownMenuFormField<string>(
            key: key,
            dropdownMenuEntries: [new DropdownMenuEntry<string>("one", "One")],
            hintText: "Pick one",
            validator: _ => "Required",
            errorBuilder: (_, error) => new Text($"custom {error}"))));
        harness.Pump(new Size(500, 180));
        Assert.NotNull(FindParagraph(harness.RenderView, "Pick one"));
        Assert.False(key.CurrentState!.Validate());
        harness.Pump(new Size(500, 200));
        Assert.NotNull(FindParagraph(harness.RenderView, "custom Required"));
        key.CurrentState.Reset();
        Assert.Equal(string.Empty, key.CurrentState.EffectiveController.Text);
    }

    private static Widget Wrap(
        Widget child,
        ThemeData? theme = null,
        TextDirection direction = TextDirection.Ltr) => new Directionality(
        direction,
        new MediaQuery(
            new MediaQueryData(Size: new Size(500, 360)),
            new Theme(
                theme ?? ThemeData.Light,
                new Overlay(initialEntries: [new OverlayEntry(_ => child)]))));

    private static void PumpAnimation()
    {
        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.35));
    }

    private static async Task WaitForConditionAsync(Func<bool> condition)
    {
        for (int i = 0; i < 100 && !condition(); i++) await Task.Delay(10);
    }

    private static RenderParagraph? FindParagraph(RenderObject? root, string text) =>
        FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.Text == text);

    private static bool Close(double a, double b) => Math.Abs(a - b) < 0.001;

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
