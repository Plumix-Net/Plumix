using Avalonia;
using Avalonia.Media;
using System.Text.RegularExpressions;
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

/// <summary>
/// Parity coverage for `material_ui/lib/src/dropdown_menu.dart`, `dropdown_menu_theme.dart` and
/// `dropdown_menu_form_field.dart`, mapped to the behaviors Flutter's own tests assert.
/// </summary>
[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialDropdownMenuTests : IDisposable
{
    private readonly TargetPlatform? _previousPlatform;

    public MaterialDropdownMenuTests()
    {
        // Desktop is the platform where `canRequestFocus` defaults to true, which is what makes the
        // field editable and therefore filterable/searchable — Flutter's own tests do the same.
        _previousPlatform = PlatformDefaults.DebugTargetPlatformOverride;
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Linux;
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

    private static IReadOnlyList<DropdownMenuEntry<string>> Entries(params string[] labels) =>
        labels.Select(label => new DropdownMenuEntry<string>(label.ToLowerInvariant(), label)).ToList();

    // ---- API surface and contracts ----

    [Fact]
    public void DropdownMenuAndEntry_ExposeFlutterDefaultsAndValidateContracts()
    {
        var entries = Entries("One");
        var menu = new DropdownMenu<string>(entries);
        Assert.True(menu.Enabled);
        Assert.Null(menu.Width);
        Assert.Null(menu.MenuHeight);
        Assert.True(menu.ShowTrailingIcon);
        Assert.Null(menu.TrailingIconFocusNode);
        Assert.False(menu.EnableFilter);
        Assert.True(menu.EnableSearch);
        Assert.Null(menu.KeyboardType);
        Assert.Equal(TextAlign.Start, menu.TextAlign);
        Assert.Null(menu.RequestFocusOnTap);
        Assert.False(menu.SelectOnly);
        Assert.Null(menu.ExpandedInsets);
        Assert.Null(menu.InputFormatters);
        Assert.Equal(DropdownMenuCloseBehavior.All, menu.CloseBehavior);
        Assert.Equal(1, menu.MaxLines);
        Assert.Null(menu.TextInputAction);
        Assert.Null(menu.CursorHeight);
        Assert.Null(menu.RestorationId);
        Assert.Null(menu.MenuController);
        Assert.Equal(new Thickness(20), menu.ScrollPadding);

        var entry = entries[0];
        Assert.True(entry.Enabled);
        Assert.Null(entry.LabelWidget);
        Assert.Null(entry.LeadingIcon);
        Assert.Null(entry.TrailingIcon);
        Assert.Null(entry.Style);

        // The four asserts Dart declares, in declaration order.
        Assert.Throws<ArgumentException>(() =>
            new DropdownMenu<string>(entries, filterCallback: (items, _) => items));
        Assert.Throws<ArgumentException>(() =>
            new DropdownMenu<string>(entries, showTrailingIcon: false, trailingIconFocusNode: new FocusNode()));
        Assert.Throws<ArgumentException>(() => new DropdownMenu<string>(
            entries,
            label: new Text("Label"),
            decorationBuilder: (_, _) => new InputDecoration()));
        Assert.Throws<ArgumentException>(() => new DropdownMenu<string>(
            entries,
            errorText: "Bad",
            decorationBuilder: (_, _) => new InputDecoration()));
    }

    [Fact]
    public void DropdownMenuThemeData_EqualityCopyWithAndLerpMatchSource()
    {
        Assert.Equal(new DropdownMenuThemeData(), new DropdownMenuThemeData() with { });
        Assert.Equal(new DropdownMenuThemeData().GetHashCode(), (new DropdownMenuThemeData() with { }).GetHashCode());

        var menuStyle = new MenuStyle(elevation: MaterialStateProperty<double?>.All(4));
        var decorationTheme = new InputDecorationThemeData(filled: true);
        var textStyle = new TextStyle(FontSize: 21);
        Assert.Equal(
            new DropdownMenuThemeData(textStyle, decorationTheme, menuStyle),
            new DropdownMenuThemeData() with
            {
                TextStyle = textStyle,
                InputDecorationTheme = decorationTheme,
                MenuStyle = menuStyle,
            });

        Assert.Equal(new DropdownMenuThemeData(), DropdownMenuThemeData.Lerp(null, null, 0));
        var data = new DropdownMenuThemeData(textStyle, decorationTheme, menuStyle, Color.Parse("#FF9E9E9E"));
        Assert.Same(data, DropdownMenuThemeData.Lerp(data, data, 0.5));
        // `inputDecorationTheme` is not lerpable in Dart; it flips at t == 0.5.
        Assert.Null(DropdownMenuThemeData.Lerp(new DropdownMenuThemeData(), data, 0.4).InputDecorationTheme);
        Assert.Same(decorationTheme, DropdownMenuThemeData.Lerp(new DropdownMenuThemeData(), data, 0.6)
            .InputDecorationTheme);
    }

    [Fact]
    public void DropdownMenuDefaults_MatchTheSourceMaterial3Table()
    {
        DropdownMenuThemeData? captured = null;
        var theme = ThemeData.Light;
        using var harness = new WidgetRenderHarness(Wrap(new Builder(context =>
        {
            captured = DropdownMenuTheme.Defaults(context);
            return new SizedBox();
        })));
        harness.Pump(new Size(500, 360));

        Assert.NotNull(captured);
        Assert.Equal(16.0, captured!.TextStyle!.FontSize);
        Assert.Equal(1.5, captured.TextStyle.Height);
        Assert.IsType<OutlineInputBorder>(captured.InputDecorationTheme!.Border);
        Assert.Equal(theme.ColorScheme.OnSurface.WithOpacity(0.38), captured.DisabledColor);

        var menuStyle = captured.MenuStyle!;
        Assert.Equal(new Size(112, 0), menuStyle.MinimumSize!.Resolve(MaterialState.None));
        Assert.Equal(
            new Size(double.PositiveInfinity, double.PositiveInfinity),
            menuStyle.MaximumSize!.Resolve(MaterialState.None));
        Assert.Equal(VisualDensity.Standard, menuStyle.VisualDensity);
        // The menu surface keeps resolving through `MenuAnchor`'s own defaults, exactly as in Dart:
        // `_DropdownMenuDefaultsM3` sets none of these.
        Assert.Null(menuStyle.BackgroundColor);
        Assert.Null(menuStyle.ShadowColor);
        Assert.Null(menuStyle.SurfaceTintColor);
        Assert.Null(menuStyle.Elevation);
        Assert.Null(menuStyle.Shape);
        Assert.Null(menuStyle.Padding);
    }

    // ---- Composition and defaults ----

    [Fact]
    public void DropdownMenu_FieldUsesBodyLargeAndOutlineBorderByDefault()
    {
        using var harness = Open(new DropdownMenu<string>(Entries("Item 0", "Item 1")), out var controller);
        var textField = Assert.Single(FindWidgets<TextField>(harness.RootElement));
        // Dart asserts the field text style is `TextTheme.bodyLarge`: fontSize 16, height 1.5.
        Assert.Equal(16.0, textField.Style!.FontSize);
        Assert.Equal(1.5, textField.Style.Height);
        Assert.IsType<OutlineInputBorder>(textField.Decoration!.Border);
        Assert.Equal(TextAlignVertical.Center, textField.TextAlignVertical);
        Assert.Equal(1, textField.MaxLines);
        Assert.Equal(new Thickness(20), textField.ScrollPadding);
        Assert.True(controller.IsOpen);
    }

    [Fact]
    public void DropdownMenu_BuildsOneMeasurementButtonPerEntryAndASecondOneWhenOpen()
    {
        var menu = new DropdownMenu<string>(Entries("Item 0", "Item 1", "Item 2"));
        using var harness = new WidgetRenderHarness(Wrap(menu));
        harness.Pump(new Size(500, 360));

        // Closed: only the measurement-only copies inside `_DropdownMenuBody`.
        Assert.Equal(3, FindWidgets<MenuItemButton>(harness.RootElement).Count);

        MenuControllerOf(harness).Open();
        harness.Pump(new Size(500, 360));
        Assert.Equal(6, FindWidgets<MenuItemButton>(harness.RootElement).Count);
    }

    [Fact]
    public void DropdownMenu_MenuSurfaceResolvesThroughMenuAnchorDefaults()
    {
        var theme = ThemeData.Light;
        using var harness = Open(new DropdownMenu<string>(Entries("Item 0", "Item 1")), out _);
        var panel = Assert.Single(FindDescendants<RenderDecoratedBox>(harness.RenderView)
            .Where(box => box.Decoration.BoxShadows is { Count: > 0 })
            .ToList());
        Assert.Equal(theme.ColorScheme.SurfaceContainer, panel.Decoration.Color);
        Assert.Equal(BorderRadius.Circular(4), panel.Decoration.EffectiveBorderRadius);
    }

    [Fact]
    public void DropdownMenu_HighlightsTheInitiallySelectedEntry()
    {
        var theme = ThemeData.Light;
        using var harness = Open(
            new DropdownMenu<string>(Entries("Item 0", "Item 1", "Item 2"), initialSelection: "item 1"),
            out _,
            out var state);

        Assert.Equal("Item 1", TextControllerOf(harness).Text);
        Assert.Equal(1, HighlightOf(state));
        var highlighted = Assert.Single(FindDescendants<RenderDecoratedBox>(harness.RenderView)
            .Where(box => box.Decoration.Color == theme.ColorScheme.OnSurface.WithOpacity(0.12))
            .ToList());
        Assert.NotNull(highlighted);
    }

    [Fact]
    public void DropdownMenu_EntryStyleTakesPrecedenceOverMenuButtonThemeAndMergesPerProperty()
    {
        Color entryBackground = Color.Parse("#FFEEDDCC");
        Color themeForeground = Color.Parse("#FF102030");
        var entries = new[]
        {
            new DropdownMenuEntry<string>(
                "one",
                "One",
                style: new ButtonStyle(BackgroundColor: MaterialStateProperty<Color?>.All(entryBackground))),
        };
        var theme = ThemeData.Light with
        {
            MenuButtonTheme = new MenuButtonThemeData(new ButtonStyle(
                BackgroundColor: MaterialStateProperty<Color?>.All(Colors.Red),
                ForegroundColor: MaterialStateProperty<Color?>.All(themeForeground))),
        };

        using var harness = Open(new DropdownMenu<string>(entries), out _, out _, theme);
        var style = Assert.Single(RealItems(harness)).Style!;
        // Entry level wins for background; the application theme still supplies foreground.
        Assert.Equal(entryBackground, style.BackgroundColor!.Resolve(MaterialState.None));
        Assert.Equal(themeForeground, style.ForegroundColor!.Resolve(MaterialState.None));
    }

    // ---- Enabled / disabled ----

    [Fact]
    public void DropdownMenu_DisabledUsesTheDisabledColorAndBlocksInteraction()
    {
        var theme = ThemeData.Light;
        var menu = new DropdownMenu<string>(Entries("Item 0"), enabled: false);
        using var harness = new WidgetRenderHarness(Wrap(menu));
        harness.Pump(new Size(500, 360));

        var textField = Assert.Single(FindWidgets<TextField>(harness.RootElement));
        Assert.False(textField.Enabled);
        Assert.Null(textField.OnTap);
        Assert.Equal(
            theme.ColorScheme.OnSurface.WithOpacity(0.38),
            textField.Style!.Color);
        Assert.Null(FindWidgets<IconButton>(harness.RootElement)[0].OnPressed);
    }

    [Fact]
    public void DropdownMenu_DisabledColorComesFromTheTheme()
    {
        var menu = new DropdownMenu<string>(Entries("Item 0"), enabled: false);
        using var harness = new WidgetRenderHarness(Wrap(
            new DropdownMenuTheme(new DropdownMenuThemeData(DisabledColor: Color.Parse("#FF9E9E9E")), menu)));
        harness.Pump(new Size(500, 360));
        Assert.Equal(Color.Parse("#FF9E9E9E"), Assert.Single(FindWidgets<TextField>(harness.RootElement)).Style!.Color);
    }

    [Fact]
    public void DropdownMenu_DisabledEntryHasNoPressHandler()
    {
        var entries = new[]
        {
            new DropdownMenuEntry<string>("one", "One"),
            new DropdownMenuEntry<string>("two", "Two", enabled: false),
        };
        using var harness = Open(new DropdownMenu<string>(entries), out _);
        var real = RealItems(harness);
        Assert.Equal(2, real.Count);
        Assert.NotNull(real[0].OnPressed);
        Assert.Null(real[1].OnPressed);
    }

    // ---- Width and layout ----

    [Fact]
    public void DropdownMenu_WidthSizesBothTheFieldAndTheMenuItems()
    {
        using var harness = Open(new DropdownMenu<string>(Entries("Item 0", "Item 1"), width: 250), out _);
        var body = Assert.Single(FindDescendants<RenderDropdownMenuBody>(harness.RenderView));
        Assert.Equal(250.0, body.Size.Width, 3);
    }

    [Fact]
    public void DropdownMenu_BodyNeverGoesBelowTheMinimumWidth()
    {
        using var harness = new WidgetRenderHarness(Wrap(new Align(
            alignment: Alignment.TopLeft,
            child: new DropdownMenu<string>(Entries("A")))));
        harness.Pump(new Size(500, 360));
        var body = Assert.Single(FindDescendants<RenderDropdownMenuBody>(harness.RenderView));
        Assert.True(body.Size.Width >= 112.0);
    }

    [Fact]
    public void DropdownMenuBody_DryLayoutDoesNotReadItsOwnConstraints()
    {
        using var harness = new WidgetRenderHarness(Wrap(new Align(
            alignment: Alignment.TopLeft,
            child: new DropdownMenu<string>(Entries("Item 0", "Item 1")))));
        harness.Pump(new Size(500, 360));
        var body = Assert.Single(FindDescendants<RenderDropdownMenuBody>(harness.RenderView));
        // Dart regression-tests that a parent may call `computeDryLayout` outside of this render
        // object's own layout pass; reading `this.constraints` there would throw.
        Size dry = body.GetDryLayout(new BoxConstraints(MaxWidth: 400, MaxHeight: 400));
        Assert.True(dry.Width >= 112.0);
        Assert.True(dry.Height > 0);
    }

    [Fact]
    public void DropdownMenu_ExpandedInsetsUseTheParentWidthAndIgnoreVerticalInsets()
    {
        var menu = new DropdownMenu<string>(
            Entries("I0", "I1", "I2"),
            expandedInsets: EdgeInsetsGeometry.Only(left: 35, top: 50, right: 20));
        using var harness = new WidgetRenderHarness(Wrap(new Align(
            alignment: Alignment.TopLeft,
            child: new SizedBox(width: 500, child: menu))));
        harness.Pump(new Size(500, 360));

        // `expandedInsets` never inserts a `_DropdownMenuBody`; the field fills the parent instead.
        Assert.Empty(FindDescendants<RenderDropdownMenuBody>(harness.RenderView));
        var padding = Assert.Single(FindWidgets<Padding>(harness.RootElement)
            .Where(item => item.InsetsGeometry.Left == 35 && item.InsetsGeometry.Right == 20)
            .ToList());
        // `expandedInsets` clamps top/bottom to zero, so only the horizontal insets survive.
        Assert.Equal(0.0, padding.InsetsGeometry.Top);
        Assert.Equal(0.0, padding.InsetsGeometry.Bottom);
        Assert.Empty(FindDescendants<RenderDropdownMenuBody>(harness.RenderView));
    }

    [Fact]
    public void DropdownMenu_DirectionalExpandedInsetsBehaveLikeTheirLtrEquivalent()
    {
        var menu = new DropdownMenu<string>(
            Entries("I0"),
            expandedInsets: EdgeInsetsGeometry.DirectionalOnly(start: 35, top: 50, end: 20));
        using var harness = new WidgetRenderHarness(Wrap(new SizedBox(width: 500, child: menu)));
        harness.Pump(new Size(500, 360));
        var padding = Assert.Single(FindWidgets<Padding>(harness.RootElement)
            .Where(item => item.InsetsGeometry.Start == 35 && item.InsetsGeometry.End == 20)
            .ToList());
        Assert.Equal(0.0, padding.InsetsGeometry.Top);
    }

    [Fact]
    public void DropdownMenu_MenuHeightCapsTheResolvedMaximumSize()
    {
        var menu = new DropdownMenu<string>(Entries("Item 0", "Item 1"), menuHeight: 100);
        using var harness = new WidgetRenderHarness(Wrap(menu));
        harness.Pump(new Size(500, 360));
        var anchor = Assert.Single(FindWidgets<MenuAnchor>(harness.RootElement));
        Assert.Equal(
            new Size(double.PositiveInfinity, 100),
            anchor.Style!.MaximumSize!.Resolve(MaterialState.None));
    }

    [Fact]
    public void DropdownMenu_PassesTheSourceMenuAnchorArguments()
    {
        var offset = new Vector(12, 34);
        var menu = new DropdownMenu<string>(Entries("Item 0"), alignmentOffset: offset);
        using var harness = new WidgetRenderHarness(Wrap(menu));
        harness.Pump(new Size(500, 360));
        var anchor = Assert.Single(FindWidgets<MenuAnchor>(harness.RootElement));
        Assert.Equal(offset, anchor.AlignmentOffset);
        Assert.Equal(EdgeInsetsGeometry.Zero, anchor.ReservedPadding);
        Assert.False(anchor.CrossAxisUnconstrained);
        Assert.False(anchor.ConsumeOutsideTap);
    }

    [Fact]
    public void DropdownMenu_RendersAtZeroAreaWithoutThrowing()
    {
        using var harness = new WidgetRenderHarness(Wrap(new DropdownMenu<string>(Entries("Item 0", "Item 1"))));
        harness.Pump(new Size(0, 0));
        var body = Assert.Single(FindDescendants<RenderDropdownMenuBody>(harness.RenderView));
        Assert.Equal(default(Size), body.Size);
    }

    // ---- Trailing icon ----

    [Fact]
    public void DropdownMenu_TrailingIconTogglesBetweenTheDownAndUpArrow()
    {
        var menu = new DropdownMenu<string>(Entries("Item 0"));
        using var harness = new WidgetRenderHarness(Wrap(menu));
        harness.Pump(new Size(500, 360));

        // The suffix icon is also handed to `_DropdownMenuBody` for measurement, so Dart finds two.
        Assert.Equal(2, FindWidgets<IconButton>(harness.RootElement).Count);
        var iconButton = FindWidgets<IconButton>(harness.RootElement)[0];
        Assert.Equal(Icons.ArrowDropDown, Assert.IsType<Icon>(iconButton.Icon).IconData);
        Assert.Equal(Icons.ArrowDropUp, Assert.IsType<Icon>(iconButton.SelectedIcon).IconData);
        Assert.False(iconButton.IsSelected);

        MenuControllerOf(harness).Open();
        harness.Pump(new Size(500, 360));
        Assert.True(FindWidgets<IconButton>(harness.RootElement)[0].IsSelected);
    }

    [Fact]
    public void DropdownMenu_ShowTrailingIconFalseRemovesTheButtonEntirely()
    {
        var menu = new DropdownMenu<string>(
            Entries("Item 0"),
            showTrailingIcon: false,
            trailingIcon: new Icon(Icons.Search));
        using var harness = new WidgetRenderHarness(Wrap(menu));
        harness.Pump(new Size(500, 360));
        Assert.Empty(FindWidgets<IconButton>(harness.RootElement));
    }

    [Fact]
    public void DropdownMenu_CollapsedDecorationThemeDropsTheTrailingIconPadding()
    {
        var menu = new DropdownMenu<string>(
            Entries("Item 0"),
            inputDecorationTheme: new InputDecorationThemeData(
                isCollapsed: true,
                suffixIconConstraints: new BoxConstraints(MaxWidth: 24, MaxHeight: 24)));
        using var harness = new WidgetRenderHarness(Wrap(menu));
        harness.Pump(new Size(500, 360));
        var iconButton = FindWidgets<IconButton>(harness.RootElement)[0];
        Assert.Equal(EdgeInsetsGeometry.Zero, iconButton.Padding);
        Assert.Equal(new BoxConstraints(MaxWidth: 24, MaxHeight: 24), iconButton.Constraints);
    }

    // ---- Keyboard ----

    [Fact]
    public void DropdownMenu_ArrowKeysMoveTheHighlightSkippingDisabledEntriesAndWrapping()
    {
        var entries = new[]
        {
            new DropdownMenuEntry<string>("0", "Item 0"),
            new DropdownMenuEntry<string>("1", "Item 1", enabled: false),
            new DropdownMenuEntry<string>("2", "Item 2", enabled: false),
            new DropdownMenuEntry<string>("3", "Item 3"),
        };
        var focusNode = new FocusNode();
        using var harness = Open(
            new DropdownMenu<string>(entries, focusNode: focusNode, requestFocusOnTap: true),
            out _,
            out var state);
        focusNode.RequestFocus();
        harness.Pump(new Size(500, 360));

        PressKey(harness, LogicalKeyboardKey.ArrowDown);
        Assert.Equal(0, HighlightOf(state));
        Assert.Equal("Item 0", TextControllerOf(harness).Text);

        PressKey(harness, LogicalKeyboardKey.ArrowDown);
        Assert.Equal(3, HighlightOf(state));
        Assert.Equal("Item 3", TextControllerOf(harness).Text);

        // Wraps forward past the end.
        PressKey(harness, LogicalKeyboardKey.ArrowDown);
        Assert.Equal(0, HighlightOf(state));

        PressKey(harness, LogicalKeyboardKey.ArrowUp);
        Assert.Equal(3, HighlightOf(state));
    }

    [Fact]
    public void DropdownMenu_EnterSelectsTheHighlightedEntryAndClosesTheMenu()
    {
        string? selected = null;
        int selections = 0;
        var focusNode = new FocusNode();
        using var harness = Open(
            new DropdownMenu<string>(
                Entries("Item 0", "Item 1"),
                focusNode: focusNode,
                requestFocusOnTap: true,
                onSelected: value => { selected = value; selections++; }),
            out var menuController);
        focusNode.RequestFocus();
        harness.Pump(new Size(500, 360));

        PressKey(harness, LogicalKeyboardKey.ArrowDown);
        PressKey(harness, LogicalKeyboardKey.Enter);
        harness.Pump(new Size(500, 360));

        Assert.Equal("item 0", selected);
        Assert.Equal(1, selections);
        Assert.False(menuController.IsOpen);
    }

    [Fact]
    public void DropdownMenu_EscapeClosesTheMenu()
    {
        var focusNode = new FocusNode();
        using var harness = Open(
            new DropdownMenu<string>(Entries("Item 0"), focusNode: focusNode, requestFocusOnTap: true),
            out var menuController);
        focusNode.RequestFocus();
        harness.Pump(new Size(500, 360));
        Assert.True(menuController.IsOpen);

        PressKey(harness, LogicalKeyboardKey.Escape);
        harness.Pump(new Size(500, 360));
        Assert.False(menuController.IsOpen);
    }

    [Fact]
    public void DropdownMenu_SelectOnlyOpensTheMenuOnEnterAndKeepsTheFieldReadOnly()
    {
        var focusNode = new FocusNode();
        var menuController = new MenuController();
        var menu = new DropdownMenu<string>(
            Entries("Item 0"),
            focusNode: focusNode,
            requestFocusOnTap: true,
            selectOnly: true,
            menuController: menuController);
        using var harness = new WidgetRenderHarness(Wrap(menu));
        harness.Pump(new Size(500, 360));

        var textField = Assert.Single(FindWidgets<TextField>(harness.RootElement));
        Assert.True(textField.ReadOnly);
        Assert.False(textField.EnableInteractiveSelection);
        Assert.Equal(SystemMouseCursors.Click, textField.MouseCursor);

        focusNode.RequestFocus();
        harness.Pump(new Size(500, 360));
        Assert.False(menuController.IsOpen);
        PressKey(harness, LogicalKeyboardKey.Enter);
        harness.Pump(new Size(500, 360));
        Assert.True(menuController.IsOpen);
    }

    // ---- Search ----

    [Fact]
    public void DropdownMenu_DefaultSearchIsCaseInsensitiveAndKeepsTheCurrentHighlight()
    {
        var focusNode = new FocusNode();
        using var harness = Open(
            new DropdownMenu<string>(
                Entries("Item 0", "Item 1", "Item 2"),
                focusNode: focusNode,
                requestFocusOnTap: true),
            out _,
            out var state);
        focusNode.RequestFocus();
        harness.Pump(new Size(500, 360));

        // Case-insensitive substring search.
        SetText(harness, "item 2");
        Assert.Equal(2, HighlightOf(state));

        // A broader query that the current highlight still matches keeps it where it is, instead of
        // jumping back to the first match.
        SetText(harness, "Item");
        Assert.Equal(2, HighlightOf(state));
    }

    [Fact]
    public void DropdownMenu_SearchCallbackOverridesTheDefaultSearch()
    {
        var focusNode = new FocusNode();
        using var harness = Open(
            new DropdownMenu<string>(
                Entries("Item 0", "Item 1", "Item 2"),
                focusNode: focusNode,
                requestFocusOnTap: true,
                searchCallback: (_, _) => 2),
            out _,
            out var state);
        focusNode.RequestFocus();
        harness.Pump(new Size(500, 360));

        SetText(harness, "z");
        Assert.Equal(2, HighlightOf(state));
    }

    [Fact]
    public void DropdownMenu_SearchDisabledNeverHighlights()
    {
        var focusNode = new FocusNode();
        using var harness = Open(
            new DropdownMenu<string>(
                Entries("Item 0", "Item 1"),
                enableSearch: false,
                focusNode: focusNode,
                requestFocusOnTap: true),
            out _,
            out var state);
        focusNode.RequestFocus();
        harness.Pump(new Size(500, 360));

        SetText(harness, "Item 1");
        Assert.Null(HighlightOf(state));
    }

    // ---- Filter ----

    [Fact]
    public void DropdownMenu_FilteringIsDisabledByDefault()
    {
        var focusNode = new FocusNode();
        using var harness = Open(
            new DropdownMenu<string>(Entries("Item 0", "Menu 1"), focusNode: focusNode, requestFocusOnTap: true),
            out _);
        focusNode.RequestFocus();
        harness.Pump(new Size(500, 360));

        SetText(harness, "Menu 1");
        // Two entries stay in the open menu: 2 measurement + 2 real buttons.
        Assert.Equal(4, FindWidgets<MenuItemButton>(harness.RootElement).Count);
    }

    [Fact]
    public void DropdownMenu_EnableFilterOnlyFiltersAfterTheUserTypes()
    {
        var focusNode = new FocusNode();
        using var harness = Open(
            new DropdownMenu<string>(
                Entries("Item 0", "Menu 1", "Item 2"),
                enableFilter: true,
                initialSelection: "item 0",
                focusNode: focusNode,
                requestFocusOnTap: true),
            out _);
        focusNode.RequestFocus();
        harness.Pump(new Size(500, 360));

        // The filter stays off until the user edits the field, even with an initial selection.
        Assert.Equal(6, FindWidgets<MenuItemButton>(harness.RootElement).Count);

        SetText(harness, "Menu 1");
        Assert.Equal(4, FindWidgets<MenuItemButton>(harness.RootElement).Count);
        Assert.Single(RealItems(harness));
    }

    [Fact]
    public void DropdownMenu_CustomFilterCallbackIsUsedVerbatim()
    {
        var focusNode = new FocusNode();
        using var harness = Open(
            new DropdownMenu<string>(
                Entries("Item 0", "Item 1", "Menu 2"),
                enableFilter: true,
                filterCallback: (entries, filter) => entries
                    .Where(entry => entry.Label.Contains(filter, StringComparison.Ordinal))
                    .ToList(),
                focusNode: focusNode,
                requestFocusOnTap: true),
            out _);
        focusNode.RequestFocus();
        harness.Pump(new Size(500, 360));

        // Case sensitive: 'item' matches nothing.
        SetText(harness, "item");
        Assert.Empty(RealItems(harness));
    }

    // ---- Selection and controller ----

    [Fact]
    public void DropdownMenu_SelectingAnEntrySetsTheControllerTextAndReportsTheValueOnce()
    {
        string? selected = null;
        int selections = 0;
        var controller = new TextEditingController();
        using var harness = Open(
            new DropdownMenu<string>(
                Entries("Item 0", "Item 1"),
                controller: controller,
                onSelected: value => { selected = value; selections++; }),
            out _);

        PressEntry(harness, 1);
        harness.Pump(new Size(500, 360));
        Assert.Equal("Item 1", controller.Text);
        Assert.Equal(controller.Text.Length, controller.Selection.BaseOffset);
        Assert.Equal("item 1", selected);
        Assert.Equal(1, selections);
    }

    [Fact]
    public void DropdownMenu_NullValuedEntryIsSelectable()
    {
        string? selected = "unset";
        var controller = new TextEditingController();
        var entries = new[] { new DropdownMenuEntry<string?>(null, "Select none") };
        using var harness = Open(
            new DropdownMenu<string?>(entries, controller: controller, onSelected: value => selected = value),
            out _);

        PressEntry(harness, 0);
        harness.Pump(new Size(500, 360));
        Assert.Equal("Select none", controller.Text);
        Assert.Null(selected);
    }

    [Fact]
    public void DropdownMenu_InitialSelectionOutsideTheEntriesLeavesTheControllerAlone()
    {
        var controller = new TextEditingController("Flutter");
        var menu = new DropdownMenu<string>(
            Entries("Item 0", "Item 1"),
            controller: controller,
            initialSelection: "missing");
        using var harness = new WidgetRenderHarness(Wrap(menu));
        harness.Pump(new Size(500, 360));
        Assert.Equal("Flutter", controller.Text);
    }

    [Fact]
    public void DropdownMenu_CloseBehaviorDecidesWhatClosesOnSelection()
    {
        // `.all` is delegated to `MenuItemButton.closeOnActivate`, which closes the whole menu tree;
        // `.self` closes this anchor explicitly and `.none` closes nothing.
        foreach ((DropdownMenuCloseBehavior behavior, bool closeOnActivate, bool? stillOpen) in new[]
                 {
                     (DropdownMenuCloseBehavior.All, true, (bool?)null),
                     (DropdownMenuCloseBehavior.Self, false, false),
                     (DropdownMenuCloseBehavior.None, false, true),
                 })
        {
            using var harness = Open(
                new DropdownMenu<string>(Entries("Item 0"), closeBehavior: behavior),
                out var menuController);
            Assert.Equal(closeOnActivate, Assert.Single(RealItems(harness)).CloseOnActivate);

            PressEntry(harness, 0);
            harness.Pump(new Size(500, 360));
            if (stillOpen is { } expected) Assert.Equal(expected, menuController.IsOpen);
        }
    }

    // ---- Focus, cursor, read-only ----

    [Theory]
    [InlineData(TargetPlatform.Android, false)]
    [InlineData(TargetPlatform.IOS, false)]
    [InlineData(TargetPlatform.Fuchsia, false)]
    [InlineData(TargetPlatform.MacOS, true)]
    [InlineData(TargetPlatform.Linux, true)]
    [InlineData(TargetPlatform.Windows, true)]
    public void DropdownMenu_CanRequestFocusFollowsThePlatform(TargetPlatform platform, bool canRequestFocus)
    {
        var menu = new DropdownMenu<string>(Entries("Item 0"));
        using var harness = new WidgetRenderHarness(Wrap(menu, ThemeData.Light with { Platform = platform }));
        harness.Pump(new Size(500, 360));
        var textField = Assert.Single(FindWidgets<TextField>(harness.RootElement));
        Assert.Equal(canRequestFocus, textField.CanRequestFocus);
        Assert.Equal(!canRequestFocus, textField.ReadOnly);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void DropdownMenu_RequestFocusOnTapDrivesReadOnlyAndTheHoverCursor(
        bool requestFocusOnTap,
        bool expectsClickCursor)
    {
        var menu = new DropdownMenu<string>(Entries("Item 0"), requestFocusOnTap: requestFocusOnTap);
        using var harness = new WidgetRenderHarness(Wrap(menu));
        harness.Pump(new Size(500, 360));
        var textField = Assert.Single(FindWidgets<TextField>(harness.RootElement));
        Assert.Equal(requestFocusOnTap, textField.CanRequestFocus);
        Assert.Equal(
            expectsClickCursor ? SystemMouseCursors.Click : SystemMouseCursors.Text,
            textField.MouseCursor);
    }

    [Fact]
    public void DropdownMenu_DisabledFieldSuppliesNoMouseCursor()
    {
        var menu = new DropdownMenu<string>(Entries("Item 0"), enabled: false, requestFocusOnTap: true);
        using var harness = new WidgetRenderHarness(Wrap(menu));
        harness.Pump(new Size(500, 360));
        // Dart passes `null`, letting `TextField` fall back to `SystemMouseCursors.basic`.
        Assert.Null(Assert.Single(FindWidgets<TextField>(harness.RootElement)).MouseCursor);
    }

    // ---- Semantics ----

    [Fact]
    public void DropdownMenu_ExposesExpandAndCollapseSemanticActions()
    {
        var menu = new DropdownMenu<string>(Entries("Item 0", "Item 1"));
        using var harness = new WidgetRenderHarness(Wrap(menu));
        var semantics = harness.PumpAndGetSemantics(new Size(500, 360));
        var node = FindSemantics(semantics, item => item.Actions.HasFlag(SemanticsActions.Expand));
        Assert.NotNull(node);
        Assert.False(node!.Flags.HasFlag(SemanticsFlags.IsExpanded));
        Assert.True(node.Flags.HasFlag(SemanticsFlags.HasExpandedState));
        Assert.False(node.Actions.HasFlag(SemanticsActions.Collapse));

        Assert.True(node.PerformAction(SemanticsActions.Expand));
        semantics = harness.PumpAndGetSemantics(new Size(500, 360));
        node = FindSemantics(semantics, item => item.Actions.HasFlag(SemanticsActions.Collapse));
        Assert.NotNull(node);
        Assert.True(node!.Flags.HasFlag(SemanticsFlags.IsExpanded));
    }

    [Fact]
    public void DropdownMenu_MeasurementOnlyEntriesStayOutOfTheSemanticsTree()
    {
        var menu = new DropdownMenu<string>(Entries("Item 0", "Item 1"));
        using var harness = new WidgetRenderHarness(Wrap(menu));
        var semantics = harness.PumpAndGetSemantics(new Size(500, 360));
        Assert.Equal(0, CountSemantics(semantics, node => node.Label == "Item 0"));

        MenuControllerOf(harness).Open();
        semantics = harness.PumpAndGetSemantics(new Size(500, 360));
        Assert.Equal(1, CountSemantics(semantics, node => node.Label == "Item 0"));
    }

    // ---- Decoration ----

    [Fact]
    public void DropdownMenu_ErrorTextReplacesHelperTextInTheDecoration()
    {
        var withHelper = new DropdownMenu<string>(Entries("Item 0"), helperText: "Helper");
        using (var harness = new WidgetRenderHarness(Wrap(withHelper)))
        {
            harness.Pump(new Size(500, 360));
            var decoration = Assert.Single(FindWidgets<TextField>(harness.RootElement)).Decoration!;
            Assert.Equal("Helper", decoration.HelperText);
            Assert.Null(decoration.ErrorText);
        }

        var withError = new DropdownMenu<string>(Entries("Item 0"), helperText: "Helper", errorText: "Error");
        using (var harness = new WidgetRenderHarness(Wrap(withError)))
        {
            harness.Pump(new Size(500, 360));
            var decoration = Assert.Single(FindWidgets<TextField>(harness.RootElement)).Decoration!;
            Assert.Equal("Error", decoration.ErrorText);
        }
    }

    [Fact]
    public void DropdownMenu_DecorationBuilderSuppliesTheDecorationAndKeepsTheDefaultSuffixIcon()
    {
        var menu = new DropdownMenu<string>(
            Entries("Item 0"),
            decorationBuilder: (_, _) => new InputDecoration
            {
                LabelText = "Built label",
                HelperText = "Built helper",
                Filled = true,
            });
        using var harness = new WidgetRenderHarness(Wrap(menu));
        harness.Pump(new Size(500, 360));
        var decoration = Assert.Single(FindWidgets<TextField>(harness.RootElement)).Decoration!;
        Assert.Equal("Built label", decoration.LabelText);
        Assert.Equal("Built helper", decoration.HelperText);
        Assert.True(decoration.Filled);
        Assert.Equal(2, FindWidgets<IconButton>(harness.RootElement).Count);
    }

    [Fact]
    public void DropdownMenu_DecorationBuilderSuffixIconReplacesTheDefaultTrailingButton()
    {
        var menu = new DropdownMenu<string>(
            Entries("Item 0"),
            decorationBuilder: (_, controller) => new InputDecoration
            {
                SuffixIcon = new Icon(controller.IsOpen ? Icons.ArrowDropUp : Icons.ArrowDropDown),
            });
        using var harness = new WidgetRenderHarness(Wrap(menu));
        harness.Pump(new Size(500, 360));
        Assert.Empty(FindWidgets<IconButton>(harness.RootElement));
        Assert.Contains(FindWidgets<Icon>(harness.RootElement), icon => icon.IconData == Icons.ArrowDropDown);

        MenuControllerOf(harness).Open();
        harness.Pump(new Size(500, 360));
        Assert.Contains(FindWidgets<Icon>(harness.RootElement), icon => icon.IconData == Icons.ArrowDropUp);
    }

    // ---- Pass-throughs ----

    [Fact]
    public void DropdownMenu_ForwardsTheTextFieldPassThroughs()
    {
        var formatters = new List<TextInputFormatter>
        {
            FilteringTextInputFormatter.Deny(new Regex("[0-9]")),
        };
        var menu = new DropdownMenu<string>(
            Entries("Item 0"),
            keyboardType: TextInputType.Number,
            textAlign: TextAlign.End,
            maxLines: 2,
            textInputAction: TextInputAction.Done,
            cursorHeight: 17,
            restorationId: "dropdown",
            inputFormatters: formatters,
            scrollPadding: new Thickness(7));
        using var harness = new WidgetRenderHarness(Wrap(menu));
        harness.Pump(new Size(500, 360));
        var textField = Assert.Single(FindWidgets<TextField>(harness.RootElement));
        Assert.Equal(TextInputType.Number, textField.KeyboardType);
        Assert.Equal(TextAlign.End, textField.TextAlign);
        Assert.Equal(2, textField.MaxLines);
        Assert.Equal(TextInputAction.Done, textField.TextInputAction);
        Assert.Equal(17.0, textField.CursorHeight);
        Assert.Equal("dropdown", textField.RestorationId);
        Assert.Same(formatters, textField.InputFormatters);
        Assert.Equal(new Thickness(7), textField.ScrollPadding);
    }

    [Fact]
    public void FilteringTextInputFormatter_RejectsDeniedCharactersAndKeepsTheCaret()
    {
        var formatter = FilteringTextInputFormatter.Deny(new Regex("[0-9]"));
        var formatted = formatter.FormatEditUpdate(
            new TextEditingValue("Green"),
            new TextEditingValue("Green2", TextSelection.Collapsed(6)));
        Assert.Equal("Green", formatted.Text);
        Assert.Equal(5, formatted.Selection.BaseOffset);

        var allow = FilteringTextInputFormatter.DigitsOnly;
        Assert.Equal("42", allow.FormatEditUpdate(new TextEditingValue(), new TextEditingValue("a4b2c")).Text);

        var replacing = FilteringTextInputFormatter.Deny(new Regex(" "), "-");
        Assert.Equal("a-b", replacing.FormatEditUpdate(new TextEditingValue(), new TextEditingValue("a b")).Text);
    }

    // ---- DropdownMenuFormField ----

    [Fact]
    public void DropdownMenuFormField_ForwardsItsDefaultsToTheInnerDropdownMenu()
    {
        var field = new DropdownMenuFormField<string>(Entries("Item 0", "Item 1"));
        using var harness = new WidgetRenderHarness(Wrap(field));
        harness.Pump(new Size(500, 360));
        var menu = Assert.Single(FindWidgets<DropdownMenu<string>>(harness.RootElement));

        Assert.True(menu.Enabled);
        Assert.Null(menu.Width);
        Assert.True(menu.ShowTrailingIcon);
        Assert.False(menu.EnableFilter);
        Assert.True(menu.EnableSearch);
        Assert.Equal(TextAlign.Start, menu.TextAlign);
        Assert.False(menu.SelectOnly);
        Assert.Equal(DropdownMenuCloseBehavior.All, menu.CloseBehavior);
        Assert.Equal(1, menu.MaxLines);
        // Labels always travel through the decoration builder, so the menu's own slots stay null.
        Assert.Null(menu.Label);
        Assert.Null(menu.HintText);
        Assert.Null(menu.HelperText);
        Assert.Null(menu.ErrorText);
        Assert.NotNull(menu.DecorationBuilder);
        // A form field always supplies a controller, even when the caller did not.
        Assert.NotNull(menu.Controller);
    }

    [Fact]
    public void DropdownMenuFormField_RoutesLabelsThroughTheDecoration()
    {
        var field = new DropdownMenuFormField<string>(
            Entries("Item 0"),
            label: new Text("Label"),
            hintText: "Hint",
            helperText: "Helper");
        using var harness = new WidgetRenderHarness(Wrap(field));
        harness.Pump(new Size(500, 360));
        var decoration = Assert.Single(FindWidgets<TextField>(harness.RootElement)).Decoration!;
        Assert.Equal("Hint", decoration.HintText);
        Assert.Equal("Helper", decoration.HelperText);
        Assert.NotNull(decoration.Label);
    }

    [Fact]
    public void DropdownMenuFormField_InitialSelectionSeedsTheValueAndTheFieldText()
    {
        var field = new DropdownMenuFormField<string>(Entries("Item 0", "Item 1"), initialSelection: "item 1");
        using var harness = new WidgetRenderHarness(Wrap(field));
        harness.Pump(new Size(500, 360));
        var state = Assert.Single(FindStates<DropdownMenuFormFieldState<string>>(harness.RootElement));
        Assert.Equal("item 1", state.Value);
        Assert.Equal("Item 1", state.TextFieldController.Text);
    }

    [Fact]
    public void DropdownMenuFormField_SelectionUpdatesTheValueSavesAndValidates()
    {
        string? saved = null;
        var field = new DropdownMenuFormField<string>(
            Entries("Item 0", "Item 1"),
            onSaved: value => saved = value,
            validator: value => value is null ? "Required" : null);
        using var harness = new WidgetRenderHarness(Wrap(field));
        harness.Pump(new Size(500, 360));
        var state = Assert.Single(FindStates<DropdownMenuFormFieldState<string>>(harness.RootElement));

        Assert.False(state.Validate());
        harness.Pump(new Size(500, 360));
        Assert.Equal("Required", state.ErrorText);
        Assert.Equal("Required", Assert.Single(FindWidgets<TextField>(harness.RootElement)).Decoration!.ErrorText);

        MenuControllerOf(harness).Open();
        harness.Pump(new Size(500, 360));
        PressEntry(harness, 1);
        harness.Pump(new Size(500, 360));
        Assert.Equal("item 1", state.Value);
        Assert.True(state.Validate());

        state.Save();
        Assert.Equal("item 1", saved);
    }

    [Fact]
    public void DropdownMenuFormField_ResetRestoresTheInitialValueAndReportsItOnce()
    {
        int selections = 0;
        var field = new DropdownMenuFormField<string>(
            Entries("Item 0", "Item 1"),
            onSelected: _ => selections++);
        using var harness = new WidgetRenderHarness(Wrap(field));
        harness.Pump(new Size(500, 360));
        var state = Assert.Single(FindStates<DropdownMenuFormFieldState<string>>(harness.RootElement));

        MenuControllerOf(harness).Open();
        harness.Pump(new Size(500, 360));
        PressEntry(harness, 1);
        harness.Pump(new Size(500, 360));
        Assert.Equal(1, selections);
        Assert.Equal("Item 1", state.TextFieldController.Text);

        state.Reset();
        harness.Pump(new Size(500, 360));
        Assert.Null(state.Value);
        Assert.Equal(2, selections);
        // With a null initial value Dart clears the field outright.
        Assert.Equal(string.Empty, state.TextFieldController.Text);
        Assert.Equal(0, state.TextFieldController.Selection.BaseOffset);
    }

    [Fact]
    public void DropdownMenuFormField_ErrorBuilderReplacesTheErrorText()
    {
        var errorKey = new ValueKey<string>("error");
        var field = new DropdownMenuFormField<string>(
            Entries("Item 0"),
            validator: _ => "Required",
            errorBuilder: (_, text) => new Text($"custom {text}", key: errorKey));
        using var harness = new WidgetRenderHarness(Wrap(field));
        harness.Pump(new Size(500, 360));
        var state = Assert.Single(FindStates<DropdownMenuFormFieldState<string>>(harness.RootElement));

        Assert.Null(Assert.Single(FindWidgets<TextField>(harness.RootElement)).Decoration!.Error);
        Assert.False(state.Validate());
        harness.Pump(new Size(500, 360));
        var decoration = Assert.Single(FindWidgets<TextField>(harness.RootElement)).Decoration!;
        Assert.Null(decoration.ErrorText);
        Assert.Equal(errorKey, decoration.Error!.Key);
    }

    [Fact]
    public void DropdownMenuFormField_RendersAtZeroAreaWithoutThrowing()
    {
        using var harness = new WidgetRenderHarness(Wrap(new DropdownMenuFormField<string>(Entries("Item 0"))));
        harness.Pump(new Size(0, 0));
        Assert.Equal(default(Size), Assert.Single(FindDescendants<RenderDropdownMenuBody>(harness.RenderView)).Size);
    }

    // ---- helpers ----

    private static WidgetRenderHarness Open<T>(DropdownMenu<T> menu, out MenuController controller) =>
        Open(menu, out controller, out _);

    private static WidgetRenderHarness Open<T>(
        DropdownMenu<T> menu,
        out MenuController controller,
        out DropdownMenuState<T> state,
        ThemeData? theme = null)
    {
        var harness = new WidgetRenderHarness(Wrap(menu, theme));
        harness.Pump(new Size(500, 360));
        controller = MenuControllerOf(harness);
        controller.Open();
        harness.Pump(new Size(500, 360));
        state = Assert.Single(FindStates<DropdownMenuState<T>>(harness.RootElement));
        return harness;
    }

    private static MenuController MenuControllerOf(WidgetRenderHarness harness) =>
        Assert.Single(FindWidgets<MenuAnchor>(harness.RootElement)).Controller!;

    private static TextEditingController TextControllerOf(WidgetRenderHarness harness) =>
        Assert.Single(FindWidgets<TextField>(harness.RootElement)).Controller!;

    private static int? HighlightOf<T>(DropdownMenuState<T> state) => state.DebugCurrentHighlight;

    private static void PressKey(WidgetRenderHarness harness, LogicalKeyboardKey key)
    {
        FocusManager.Instance.HandleKeyEvent(KeySim.Down(key));
        FocusManager.Instance.HandleKeyEvent(KeySim.Up(key));
        harness.Pump(new Size(500, 360));
    }

    /// <summary>Replaces the whole field text, the way selecting-all and typing would.</summary>
    private static void SetText(WidgetRenderHarness harness, string text)
    {
        var editable = Assert.Single(FindStates<EditableText.EditableTextState>(harness.RootElement));
        editable.UserUpdateTextEditingValue(
            new TextEditingValue(text, TextSelection.Collapsed(text.Length)),
            SelectionChangedCause.Keyboard);
        harness.Pump(new Size(500, 360));
    }

    /// <summary>
    /// The menu's real entries. Dart builds every entry twice — once keyed inside the open menu and
    /// once unkeyed inside `_DropdownMenuBody` purely to measure it — so the key tells them apart.
    /// </summary>
    private static List<MenuItemButton> RealItems(WidgetRenderHarness harness) =>
        FindWidgets<MenuItemButton>(harness.RootElement).Where(button => button.Key is not null).ToList();

    private static void PressEntry(WidgetRenderHarness harness, int index)
    {
        RealItems(harness)[index].OnPressed!();
        harness.Pump(new Size(500, 360));
    }

    private static Widget Wrap(Widget child, ThemeData? theme = null, TextDirection direction = TextDirection.Ltr) =>
        new Directionality(
            direction,
            new MediaQuery(
                new MediaQueryData(Size: new Size(500, 360)),
                new Theme(
                    theme ?? ThemeData.Light,
                    new Overlay(initialEntries: [new OverlayEntry(_ => child)]))));

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

        public Element RootElement => _rootElement;

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            Scheduler.PumpFrameForTests();
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

            public void InsertRenderObjectChild(RenderObject child, object? slot) =>
                _renderView.Child = (RenderBox)child;

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (ReferenceEquals(_renderView.Child, child)) _renderView.Child = null;
            }

            internal override void Unmount()
            {
                if (_child is not null) { UnmountChild(_child); _child = null; }
                base.Unmount();
            }
        }
    }
}
