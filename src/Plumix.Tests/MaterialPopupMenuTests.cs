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
using RelativeRect = Plumix.Rendering.RelativeRect;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialPopupMenuTests : IDisposable
{
    public MaterialPopupMenuTests()
    {
        Scheduler.ResetForTests();
        FocusManager.Instance.ResetForTests();
        GestureBinding.Instance.ResetForTests();
        MouseCursorManager.ResetForTests();
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
    }

    public void Dispose()
    {
        GestureBinding.Instance.ResetForTests();
        FocusManager.Instance.ResetForTests();
        Scheduler.ResetForTests();
        MouseCursorManager.ResetForTests();
        PlatformDefaults.DebugTargetPlatformOverride = null;
    }

    [Fact]
    public void PopupMenuButtonAndItem_ExposeFlutterDefaultsAndValidateContracts()
    {
        var item = new PopupMenuItem<string>(new Text("One"), value: "one");
        Assert.True(item.Enabled);
        Assert.Equal(48, item.Height);
        Assert.True(item.Represents("one"));
        Assert.False(item.Represents("two"));

        var checkedItem = new CheckedPopupMenuItem<string>(new Text("Checked"), value: "checked");
        Assert.False(checkedItem.Checked);
        Assert.True(checkedItem.Enabled);
        Assert.Equal(48, checkedItem.Height);

        var divider = new PopupMenuDivider();
        Assert.Equal(16, divider.Height);
        Assert.False(divider.Represents("anything"));

        var button = new PopupMenuButton<string>(_ => [item]);
        Assert.True(button.Enabled);
        Assert.Equal(EdgeInsetsGeometry.All(8), button.Padding);
        Assert.Equal(Clip.None, button.ClipBehavior);
        Assert.False(button.UseRootNavigator);

        Assert.Throws<ArgumentException>(() => new PopupMenuButton<string>(
            _ => [item],
            child: new Text("child"),
            icon: new Icon(Icons.MoreVert)));
        Assert.Equal(-1, new PopupMenuButton<string>(_ => [item], elevation: -1).Elevation);
        Assert.Equal(-1, new PopupMenuItem<string>(new Text("bad"), height: -1).Height);
        Assert.Equal(-1, new CheckedPopupMenuItem<string>(new Text("bad"), height: -1).Height);
        Assert.Equal(-1, new PopupMenuDivider(height: -1).Height);
        Assert.Equal(-1, new PopupMenuThemeData(Elevation: -1).Elevation);
        Assert.Throws<ArgumentException>(() =>
        {
            _ = PopupMenus.ShowMenu(
                default,
                Array.Empty<PopupMenuEntry<string>>(),
                position: new RelativeRect(0, 0, 0, 0));
        });
    }

    [Fact]
    public void PopupMenuDivider_DelegatesGeometryAndColorToDivider()
    {
        var radius = BorderRadius.Circular(5);
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new PopupMenuDivider(
                height: 20,
                thickness: 5,
                indent: 7,
                endIndent: 9,
                radius: radius,
                color: Colors.Orange)));
        harness.Pump(new Size(240, 80));

        var line = Assert.Single(
            FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Border is Plumix.Rendering.Border { Bottom.Style: BorderStyle.Solid });
        BorderSide side = ((Plumix.Rendering.Border)line.Decoration.Border!).Bottom;
        Assert.Equal(5, side.Width);
        Assert.Equal(radius, line.Decoration.BorderRadius);
        Assert.Equal(Colors.Orange, side.Color);
        Assert.Contains(
            FindDescendants<RenderPadding>(harness.RenderView),
            padding => padding.Padding == new Thickness(7, 0, 9, 0));
        Assert.Equal(20, harness.RenderView.Child!.Size.Height);
    }

    [Fact]
    public void CheckedPopupMenuItem_UsesCheckmarkSelectedStyleAndCheckboxSemantics()
    {
        var labelStyle = MaterialStateProperty<TextStyle?>.ResolveWith(states =>
            states.HasFlag(MaterialState.Selected)
                ? new TextStyle(FontSize: 24, Color: Colors.Red)
                : new TextStyle(FontSize: 20, Color: Colors.Orange));
        using var checkedHarness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new CheckedPopupMenuItem<string>(
                new Text("Checked item"),
                value: "checked",
                @checked: true,
                labelTextStyle: labelStyle)));
        var checkedSemantics = checkedHarness.PumpAndGetSemantics(new Size(280, 100));

        var checkedText = FindParagraph(checkedHarness.RenderView, "Checked item");
        Assert.NotNull(checkedText);
        Assert.Equal(24, checkedText!.FontSize);
        Assert.Equal(Colors.Red, Assert.IsType<SolidColorBrush>(checkedText.Foreground).Color);
        Assert.NotNull(FindParagraph(checkedHarness.RenderView, char.ConvertFromUtf32(Icons.Done.CodePoint)));
        Assert.NotNull(FindSemantics(checkedSemantics, node =>
            node.Role == SemanticsRole.MenuItemCheckbox
            && node.Flags.HasFlag(SemanticsFlags.IsButton)
            && node.Flags.HasFlag(SemanticsFlags.IsEnabled)
            && node.Flags.HasFlag(SemanticsFlags.HasCheckedState)
            && node.Flags.HasFlag(SemanticsFlags.IsChecked)));

        using var uncheckedHarness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new CheckedPopupMenuItem<string>(
                new Text("Unchecked item"),
                labelTextStyle: labelStyle)));
        var uncheckedSemantics = uncheckedHarness.PumpAndGetSemantics(new Size(280, 100));
        var uncheckedText = FindParagraph(uncheckedHarness.RenderView, "Unchecked item");
        Assert.NotNull(uncheckedText);
        Assert.Equal(20, uncheckedText!.FontSize);
        Assert.Equal(Colors.Orange, Assert.IsType<SolidColorBrush>(uncheckedText.Foreground).Color);
        Assert.Null(FindParagraph(uncheckedHarness.RenderView, char.ConvertFromUtf32(Icons.Done.CodePoint)));
        Assert.NotNull(FindSemantics(uncheckedSemantics, node =>
            node.Role == SemanticsRole.MenuItemCheckbox
            && node.Flags.HasFlag(SemanticsFlags.HasCheckedState)
            && !node.Flags.HasFlag(SemanticsFlags.IsChecked)));

        using var m2Harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light with { UseMaterial3 = false },
            new CheckedPopupMenuItem<string>(new Text("M2 checked"), @checked: true)));
        m2Harness.Pump(new Size(280, 100));
        Assert.Equal(
            ThemeData.Localize(ThemeData.Light, Typography.EnglishLike2021).TextTheme.TitleMedium.FontSize,
            FindParagraph(m2Harness.RenderView, "M2 checked")!.FontSize);
    }

    [Fact]
    public void PopupMenuItem_UsesM3AndM2TypographyPaddingAndDisabledSemantics()
    {
        using var m3 = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new PopupMenuItem<string>(new Text("M3 item"), value: "m3")));
        var m3Semantics = m3.PumpAndGetSemantics(new Size(240, 80));
        Assert.Contains(FindDescendants<RenderPadding>(m3.RenderView), value => value.Padding == new Thickness(12, 0));
        Assert.Equal(
            ThemeData.Localize(ThemeData.Light, Typography.EnglishLike2021).TextTheme.LabelLarge.FontSize,
            FindParagraph(m3.RenderView, "M3 item")!.FontSize);
        Assert.Equal(
            ThemeData.Light.ColorScheme.OnSurface,
            Assert.IsType<SolidColorBrush>(FindParagraph(m3.RenderView, "M3 item")!.Foreground).Color);
        Assert.NotNull(FindSemantics(m3Semantics, node =>
            node.Flags.HasFlag(SemanticsFlags.IsButton)
            && node.Flags.HasFlag(SemanticsFlags.IsEnabled)));

        using var m2 = new WidgetRenderHarness(Wrap(
            ThemeData.Light with { UseMaterial3 = false },
            new PopupMenuItem<string>(new Text("M2 disabled"), enabled: false)));
        var m2Semantics = m2.PumpAndGetSemantics(new Size(240, 80));
        Assert.Contains(FindDescendants<RenderPadding>(m2.RenderView), value => value.Padding == new Thickness(16, 0));
        Assert.Equal(
            ThemeData.Localize(ThemeData.Light, Typography.EnglishLike2021).TextTheme.TitleMedium.FontSize,
            FindParagraph(m2.RenderView, "M2 disabled")!.FontSize);
        Assert.Equal(
            ThemeData.Light.DisabledColor,
            Assert.IsType<SolidColorBrush>(FindParagraph(m2.RenderView, "M2 disabled")!.Foreground).Color);
        Assert.NotNull(FindSemantics(m2Semantics, node => node.Flags.HasFlag(SemanticsFlags.IsButton)));
        Assert.Null(FindSemantics(m2Semantics, node => node.Actions.HasFlag(SemanticsActions.Tap)));
    }

    [Fact]
    public async Task ShowMenu_UsesPositionThemeSurfaceShrinkWrapAndCompletesSelection()
    {
        BuildContext captured = default;
        int itemTapCount = 0;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light with { Platform = TargetPlatform.Android },
            new Navigator(new BuilderPageRoute(context => new CaptureContext(
                value => captured = value,
                new Text("Home"))))));
        harness.Pump(new Size(500, 360));

        var result = PopupMenus.ShowMenu(
            captured,
            items:
            [
                new PopupMenuItem<string>(new Text("First"), value: "first", onTap: () => itemTapCount++),
                new PopupMenuItem<string>(new Text("Second"), value: "second"),
            ],
            position: new RelativeRect(40, 30, 380, 290));
        PumpAnimation();
        var semantics = harness.PumpAndGetSemantics(new Size(500, 360));

        var layout = Assert.Single(FindDescendants<RenderPopupMenuPositionLayout>(harness.RenderView));
        Assert.Equal(40, ((BoxParentData)layout.Child!.parentData!).offset.X, precision: 3);
        Assert.Equal(30, ((BoxParentData)layout.Child.parentData!).offset.Y, precision: 3);
        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView), box =>
            box.Decoration.Color == ThemeData.Light.ColorScheme.SurfaceContainer
            && box.Decoration.EffectiveBorderRadius == BorderRadius.Circular(4)
            && box.Decoration.BoxShadows is not null);
        var viewport = Assert.Single(FindDescendants<RenderSingleChildViewport>(harness.RenderView));
        Assert.True(viewport.Size.Height < 360);
        Assert.NotNull(FindSemantics(semantics, node =>
            HasLabelPart(node, "Popup menu")
            && node.Flags.HasFlag(SemanticsFlags.ScopesRoute)
            && node.Flags.HasFlag(SemanticsFlags.NamesRoute)));

        var itemAction = FindSemantics(semantics, node =>
            node.Actions.HasFlag(SemanticsActions.Tap)
            && HasLabelPart(node, "First"));
        Assert.True(itemAction is not null, DumpSemantics(semantics));
        Assert.True(itemAction!.PerformAction(SemanticsActions.Tap));
        Assert.Equal(1, itemTapCount);
        PumpAnimation();
        harness.Pump(new Size(500, 360));
        Assert.Equal("first", await result);
        Assert.Null(FindParagraph(harness.RenderView, "First"));
    }

    [Fact]
    public async Task CheckedPopupMenuItem_TapFadesCheckmarkInBeforeRouteCloses()
    {
        BuildContext captured = default;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(context => new CaptureContext(
                value => captured = value,
                new Text("Home"))))));
        harness.Pump(new Size(500, 360));

        var result = PopupMenus.ShowMenu<string>(
            captured,
            items:
            [
                new CheckedPopupMenuItem<string>(
                    new Text("Toggle"),
                    value: "toggle",
                    @checked: false),
            ],
            position: new RelativeRect(40, 30, 380, 290));
        PumpAnimation();
        var semantics = harness.PumpAndGetSemantics(new Size(500, 360));
        var action = FindSemantics(semantics, node =>
            node.Role == SemanticsRole.MenuItemCheckbox
            && node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(action);
        Assert.True(action!.PerformAction(SemanticsActions.Tap));

        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.075));
        harness.Pump(new Size(500, 360));
        Assert.NotNull(FindParagraph(harness.RenderView, char.ConvertFromUtf32(Icons.Done.CodePoint)));
        Assert.Contains(FindDescendants<RenderOpacity>(harness.RenderView), opacity =>
            opacity.Opacity > 0.35 && opacity.Opacity < 0.65);

        PumpAnimation();
        harness.Pump(new Size(500, 360));
        Assert.Equal("toggle", await result);
    }

    [Fact]
    public void ShowMenu_WidgetValuesOverrideLocalAndGlobalPopupMenuThemes()
    {
        BuildContext captured = default;
        var global = ThemeData.Light with
        {
            PopupMenuTheme = new PopupMenuThemeData(
                Color: Colors.Green,
                Shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(6)),
                Elevation: 2),
        };
        using var harness = new WidgetRenderHarness(Wrap(
            global,
            new Navigator(new BuilderPageRoute(context => new PopupMenuTheme(
                new PopupMenuThemeData(
                    Color: Colors.Purple,
                    Shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(10)),
                    MenuPadding: new Thickness(5)),
                new CaptureContext(value => captured = value, new Text("Home")))))));
        harness.Pump(new Size(500, 360));
        _ = PopupMenus.ShowMenu(
            captured,
            items: [new PopupMenuItem<string>(new Text("Override"), value: "override")],
            position: new RelativeRect(20, 20, 400, 290),
            color: Colors.Orange,
            shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(3)),
            elevation: 0,
            menuPadding: new Thickness(7));
        PumpAnimation();
        harness.Pump(new Size(500, 360));

        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView), box =>
            box.Decoration.Color == Colors.Orange
            && box.Decoration.EffectiveBorderRadius == BorderRadius.Circular(3)
            && box.Decoration.BoxShadows is null);
        Assert.Contains(FindDescendants<RenderPadding>(harness.RenderView), padding =>
            padding.Padding == new Thickness(7));
    }

    [Fact]
    public async Task PopupMenuButton_AnchorsUnderButtonSkipsDisabledKeyboardItemAndReportsCancel()
    {
        int opened = 0;
        int canceled = 0;
        string? selected = null;
        var selectedCompletion = new TaskCompletionSource<string>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var canceledCompletion = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => new Align(
                alignment: Alignment.TopLeft,
                child: new PopupMenuButton<string>(
                    itemBuilder: _ =>
                    [
                        new PopupMenuItem<string>(new Text("One"), value: "one"),
                        new PopupMenuDivider(),
                        new PopupMenuItem<string>(new Text("Disabled"), value: "disabled", enabled: false),
                        new PopupMenuItem<string>(new Text("Three"), value: "three"),
                    ],
                    onOpened: () => opened++,
                    onSelected: value =>
                    {
                        selected = value;
                        selectedCompletion.TrySetResult(value);
                    },
                    onCanceled: () =>
                    {
                        canceled++;
                        canceledCompletion.TrySetResult();
                    },
                    position: PopupMenuPosition.Under,
                    offset: new Vector(5, 7),
                    child: new SizedBox(width: 80, height: 32, child: new Text("OPEN"))))))));
        var initialSemantics = harness.PumpAndGetSemantics(new Size(500, 360));
        var openAction = FindSemantics(initialSemantics, node => node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(openAction);
        Assert.True(openAction!.PerformAction(SemanticsActions.Tap));
        Assert.Equal(1, opened);
        PumpAnimation();
        harness.Pump(new Size(500, 360));

        var positionLayout = Assert.Single(FindDescendants<RenderPopupMenuPositionLayout>(harness.RenderView));
        Assert.Equal(5, positionLayout.Position.Left, precision: 3);
        Assert.Equal(39, positionLayout.Position.Top, precision: 3);
        var expandedSemantics = harness.PumpAndGetSemantics(new Size(500, 360));
        // The route-owned modal barrier blocks the semantics painted before it, so the anchor leaves the tree
        // while the menu is open; the menu route and its dismiss barrier are what assistive technology sees.
        Assert.True(FindSemantics(expandedSemantics, node => HasLabelPart(node, "Popup menu")) is not null,
            DumpSemantics(expandedSemantics));
        Assert.True(FindSemantics(expandedSemantics, node => HasLabelPart(node, "Dismiss menu")) is not null,
            DumpSemantics(expandedSemantics));
        Assert.True(
            FindSemantics(expandedSemantics, node => node.Flags.HasFlag(SemanticsFlags.HasExpandedState)) is null,
            DumpSemantics(expandedSemantics));

        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowDown)));
        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Enter)));
        // `Route` completes its pop future with `RunContinuationsAsynchronously`, so
        // `_PopupMenuButtonState.HandleResult` — and the `SetState` that clears the expanded flag —
        // runs on a pool thread. Await it before pumping, or the `SetState` races the build scope.
        await selectedCompletion.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("three", selected);
        PumpAnimation();
        harness.Pump(new Size(500, 360));

        var reopenSemantics = harness.PumpAndGetSemantics(new Size(500, 360));
        var collapsedAnchor = FindSemantics(
            reopenSemantics,
            node => node.Flags.HasFlag(SemanticsFlags.HasExpandedState));
        Assert.True(collapsedAnchor is not null, DumpSemantics(reopenSemantics));
        Assert.False(collapsedAnchor!.Flags.HasFlag(SemanticsFlags.IsExpanded));
        var reopen = FindSemantics(reopenSemantics, node => node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(reopen);
        Assert.True(reopen!.PerformAction(SemanticsActions.Tap));
        PumpAnimation();
        var menuSemantics = harness.PumpAndGetSemantics(new Size(500, 360));
        var barrier = FindSemantics(menuSemantics, node => HasLabelPart(node, "Dismiss menu"));
        Assert.NotNull(barrier);
        Assert.True(barrier!.PerformAction(SemanticsActions.Tap));
        await canceledCompletion.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, canceled);
        PumpAnimation();
        harness.Pump(new Size(500, 360));
    }

    [Fact]
    public void PopupMenuThemeData_CopiesAndLerpsDirectionalConfiguration()
    {
        var from = new PopupMenuThemeData(
            Color: Colors.Red,
            MenuPadding: EdgeInsetsGeometry.DirectionalOnly(start: 4, top: 2, end: 8, bottom: 6),
            Elevation: 2,
            EnableFeedback: false,
            Position: PopupMenuPosition.Over,
            IconSize: 20);
        var to = new PopupMenuThemeData(
            Color: Colors.Blue,
            MenuPadding: EdgeInsetsGeometry.DirectionalOnly(start: 12, top: 6, end: 16, bottom: 10),
            Elevation: 6,
            EnableFeedback: true,
            Position: PopupMenuPosition.Under,
            IconSize: 28);

        PopupMenuThemeData copy = from.CopyWith(iconSize: 24);
        Assert.Equal(Colors.Red, copy.Color);
        Assert.Equal(24, copy.IconSize);
        Assert.False(copy.EnableFeedback);

        PopupMenuThemeData lerped = Assert.IsType<PopupMenuThemeData>(
            PopupMenuThemeData.Lerp(from, to, 0.5));
        Assert.Equal(4, lerped.Elevation);
        Assert.Equal(24, lerped.IconSize);
        Assert.Equal(new Thickness(8, 4, 12, 8), lerped.MenuPadding!.Value.Resolve(TextDirection.Ltr));
        Assert.Equal(new Thickness(12, 4, 8, 8), lerped.MenuPadding.Value.Resolve(TextDirection.Rtl));
        Assert.True(lerped.EnableFeedback);
        Assert.Equal(PopupMenuPosition.Under, lerped.Position);
        Assert.Same(from, PopupMenuThemeData.Lerp(from, from, 0.25));
        Assert.Null(PopupMenuThemeData.Lerp(null, null, 0.5));
        Assert.IsAssignableFrom<InheritedTheme>(
            new PopupMenuTheme(new PopupMenuThemeData(), new SizedBox()));
    }

    [Fact]
    public void PopupMenuButton_ProgrammaticDisabledOpenAndEmptyBuilderMatchFlutter()
    {
        int disabledBuilds = 0;
        int disabledOpened = 0;
        int disabledCanceled = 0;
        int emptyBuilds = 0;
        int emptyOpened = 0;
        var disabledKey = new LabeledGlobalKey<PopupMenuButtonState<string>>("disabled popup");
        var emptyKey = new LabeledGlobalKey<PopupMenuButtonState<string>>("empty popup");
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => new Column(children:
            [
                new PopupMenuButton<string>(
                    key: disabledKey,
                    enabled: false,
                    itemBuilder: _ =>
                    {
                        disabledBuilds++;
                        return [new PopupMenuItem<string>(new Text("Disabled programmatic"), value: "value")];
                    },
                    onOpened: () => disabledOpened++,
                    onCanceled: () => disabledCanceled++),
                new PopupMenuButton<string>(
                    key: emptyKey,
                    itemBuilder: _ =>
                    {
                        emptyBuilds++;
                        return [];
                    },
                    onOpened: () => emptyOpened++),
            ])))));
        SemanticsNode? initialSemantics = harness.PumpAndGetSemantics(new Size(500, 360));
        Assert.Null(FindSemantics(initialSemantics, node =>
            node.Actions.HasFlag(SemanticsActions.Tap)
            && HasLabelPart(node, "Disabled programmatic")));
        Assert.Equal(0, disabledBuilds);

        disabledKey.CurrentState!.ShowButtonMenu();
        Assert.Equal(1, disabledBuilds);
        Assert.Equal(1, disabledOpened);
        PumpAnimation();
        SemanticsNode? openSemantics = harness.PumpAndGetSemantics(new Size(500, 360));
        SemanticsNode? barrier = FindSemantics(openSemantics, node => HasLabelPart(node, "Dismiss menu"));
        Assert.NotNull(barrier);
        Assert.True(barrier!.PerformAction(SemanticsActions.Dismiss));
        PumpAnimation();
        harness.Pump(new Size(500, 360));
        Assert.Equal(1, disabledCanceled);

        emptyKey.CurrentState!.ShowButtonMenu();
        Assert.Equal(1, emptyBuilds);
        Assert.Equal(0, emptyOpened);
    }

    [Fact]
    public void ShowMenu_CapturesLocalThemeAndTracksGlobalThemeChanges()
    {
        BuildContext localContext = default;
        using var localHarness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => new PopupMenuTheme(
                new PopupMenuThemeData(Color: Colors.Purple),
                new Builder(context => new CaptureContext(value => localContext = value, new Text("Home"))))))));
        localHarness.Pump(new Size(500, 360));
        _ = PopupMenus.ShowMenu(
            localContext,
            items: [new PopupMenuItem<string>(new Text("Local theme"), value: "local")],
            position: new RelativeRect(20, 20, 400, 290));
        PumpAnimation();
        localHarness.Pump(new Size(500, 360));
        Assert.Contains(FindDescendants<RenderDecoratedBox>(localHarness.RenderView), box =>
            box.Decoration.Color == Colors.Purple);

        BuildContext globalContext = default;
        Widget BuildRoot(Color color) => Wrap(
            ThemeData.Light with
            {
                PopupMenuTheme = new PopupMenuThemeData(Color: color),
            },
            new Navigator(new BuilderPageRoute(context => new CaptureContext(
                value => globalContext = value,
                new Text("Global home")))));
        using var globalHarness = new WidgetRenderHarness(BuildRoot(Colors.Green));
        globalHarness.Pump(new Size(500, 360));
        _ = PopupMenus.ShowMenu(
            globalContext,
            items: [new PopupMenuItem<string>(new Text("Global theme"), value: "global")],
            position: new RelativeRect(20, 20, 400, 290));
        PumpAnimation();
        globalHarness.Pump(new Size(500, 360));
        Assert.Contains(FindDescendants<RenderDecoratedBox>(globalHarness.RenderView), box =>
            box.Decoration.Color == Colors.Green);

        globalHarness.UpdateRoot(BuildRoot(Colors.Orange));
        globalHarness.Pump(new Size(500, 360));
        Assert.Contains(FindDescendants<RenderDecoratedBox>(globalHarness.RenderView), box =>
            box.Decoration.Color == Colors.Orange);
    }

    [Fact]
    public void ShowMenu_SemanticLabelUsesDefaultTargetPlatformInsteadOfThemePlatform()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.IOS;
        BuildContext appleContext = default;
        using (var appleHarness = new WidgetRenderHarness(Wrap(
                   ThemeData.Light with { Platform = TargetPlatform.Android },
                   new Navigator(new BuilderPageRoute(context => new CaptureContext(
                       value => appleContext = value,
                       new Text("Apple home")))))))
        {
            appleHarness.Pump(new Size(500, 360));
            _ = PopupMenus.ShowMenu(
                appleContext,
                items: [new PopupMenuItem<string>(new Text("Apple item"), value: "apple")],
                position: new RelativeRect(20, 20, 400, 290));
            PumpAnimation();
            SemanticsNode? semantics = appleHarness.PumpAndGetSemantics(new Size(500, 360));
            SemanticsNode? menu = FindSemantics(semantics, node => node.Role == SemanticsRole.Menu);
            Assert.NotNull(menu);
            Assert.Null(menu!.Label);
        }

        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        BuildContext androidContext = default;
        using var androidHarness = new WidgetRenderHarness(Wrap(
            ThemeData.Light with { Platform = TargetPlatform.IOS },
            new Navigator(new BuilderPageRoute(context => new CaptureContext(
                value => androidContext = value,
                new Text("Android home"))))));
        androidHarness.Pump(new Size(500, 360));
        _ = PopupMenus.ShowMenu(
            androidContext,
            items: [new PopupMenuItem<string>(new Text("Android item"), value: "android")],
            position: new RelativeRect(20, 20, 400, 290));
        PumpAnimation();
        SemanticsNode? androidSemantics = androidHarness.PumpAndGetSemantics(new Size(500, 360));
        Assert.NotNull(FindSemantics(androidSemantics, node =>
            node.Role == SemanticsRole.Menu && HasLabelPart(node, "Popup menu")));
    }

    [Fact]
    public async Task PopupMenuItem_PopsBeforeOnTapPushesAnotherRoute()
    {
        BuildContext captured = default;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(context => new CaptureContext(
                value => captured = value,
                new Text("Home"))))));
        harness.Pump(new Size(500, 360));

        Task<string?> result = PopupMenus.ShowMenu(
            captured,
            items:
            [
                new PopupMenuItem<string>(
                    new Text("Push next"),
                    value: "next",
                    onTap: () => Navigator.Of(captured).Push(
                        new BuilderPageRoute(_ => new Text("Pushed route")))),
            ],
            position: new RelativeRect(20, 20, 400, 290));
        PumpAnimation();
        SemanticsNode? semantics = harness.PumpAndGetSemantics(new Size(500, 360));
        SemanticsNode? action = FindSemantics(semantics, node =>
            node.Actions.HasFlag(SemanticsActions.Tap)
            && HasLabelPart(node, "Push next"));
        Assert.NotNull(action);
        Assert.True(action!.PerformAction(SemanticsActions.Tap));
        Assert.Equal("next", await result);

        PumpAnimation();
        harness.Pump(new Size(500, 360));
        Assert.NotNull(FindParagraph(harness.RenderView, "Pushed route"));
    }

    [Fact]
    public void ShowMenu_UsesDisplayFeatureSubScreenAndDirectionalMenuPadding()
    {
        BuildContext captured = default;
        var mediaQuery = new MediaQueryData(
            Size: new Size(800, 600),
            DisplayFeatures:
            [
                new DisplayFeature(new Rect(390, 0, 20, 600), DisplayFeatureType.Hinge),
            ]);
        using var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Rtl,
            new MediaQuery(
                mediaQuery,
                new Theme(
                    ThemeData.Light,
                    new Navigator(new BuilderPageRoute(_ => new PopupMenuTheme(
                        new PopupMenuThemeData(
                            MenuPadding: EdgeInsetsGeometry.DirectionalOnly(
                                start: 20,
                                top: 6,
                                end: 4,
                                bottom: 10)),
                        new Builder(context => new CaptureContext(
                            value => captured = value,
                            new Text("Home"))))))))));
        harness.Pump(new Size(800, 600));
        _ = PopupMenus.ShowMenu(
            captured,
            items: [new PopupMenuItem<string>(new Text("Fold item"), value: "fold")],
            position: new RelativeRect(380, 20, 410, 500));
        PumpAnimation();
        harness.Pump(new Size(800, 600));

        RenderPopupMenuPositionLayout layout = Assert.Single(
            FindDescendants<RenderPopupMenuPositionLayout>(harness.RenderView));
        Point offset = ((BoxParentData)layout.Child!.parentData!).offset;
        Assert.Equal(382, offset.X + layout.Child.Size.Width, precision: 3);
        Assert.Contains(FindDescendants<RenderPadding>(harness.RenderView), padding =>
            padding.Padding == new Thickness(4, 6, 20, 10));
    }

    [Fact]
    public void ShowMenu_ScrollsTheFirstInitialValueIntoView()
    {
        BuildContext captured = default;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(context => new CaptureContext(
                value => captured = value,
                new Text("Home"))))));
        harness.Pump(new Size(300, 200));
        PopupMenuEntry<string>[] items = Enumerable.Range(0, 50)
            .Select(index => (PopupMenuEntry<string>)new PopupMenuItem<string>(
                new Text($"Item {index}"),
                value: index.ToString()))
            .ToArray();
        _ = PopupMenus.ShowMenu(
            captured,
            items: items,
            initialValue: "49",
            position: new RelativeRect(20, 20, 200, 100));
        PumpAnimation();
        harness.Pump(new Size(300, 200));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.02));
        harness.Pump(new Size(300, 200));

        RenderSingleChildViewport viewport = Assert.Single(
            FindDescendants<RenderSingleChildViewport>(harness.RenderView));
        Assert.True(viewport.OffsetPixels > 0);
    }

    [Fact]
    public void PopupMenuItems_TreatHeightAsMinimumAndRemainSafeAtZeroArea()
    {
        using var ordinary = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Column(
                mainAxisSize: MainAxisSize.Min,
                children:
                [
                    new PopupMenuItem<string>(
                        new SizedBox(width: 20, height: 75),
                        height: 16,
                        padding: new Thickness(0)),
                ])));
        ordinary.Pump(new Size(200, 200));
        RenderConstrainedBox ordinaryMinimum = Assert.Single(
            FindDescendants<RenderConstrainedBox>(ordinary.RenderView),
            box => box.AdditionalConstraints.MinHeight == 16);
        Assert.Equal(75, ordinaryMinimum.Size.Height);

        using var checkedItem = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Column(
                mainAxisSize: MainAxisSize.Min,
                children:
                [
                    new CheckedPopupMenuItem<string>(
                        new SizedBox(width: 20, height: 10),
                        height: 16,
                        padding: new Thickness(0)),
                ])));
        checkedItem.Pump(new Size(200, 200));
        RenderConstrainedBox checkedMinimum = Assert.Single(
            FindDescendants<RenderConstrainedBox>(checkedItem.RenderView),
            box => box.AdditionalConstraints.MinHeight == 16);
        Assert.Equal(56, checkedMinimum.Size.Height);

        using var zero = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new CheckedPopupMenuItem<string>(new Text("Zero"))));
        zero.Pump(new Size());
        Assert.Equal(new Size(), zero.RenderView.Child!.Size);
    }

    [Fact]
    public void PopupMenuItem_ResolvesThemeCursorFromHoveredAndDisabledStates()
    {
        var cursor = MaterialStateProperty<MouseCursor?>.ResolveWith(states =>
            states.HasFlag(MaterialState.Disabled)
                ? SystemMouseCursors.Grab
                : states.HasFlag(MaterialState.Hovered)
                    ? SystemMouseCursors.Text
                    : SystemMouseCursors.Click);
        using var enabled = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new PopupMenuTheme(
                new PopupMenuThemeData(MouseCursor: cursor),
                new PopupMenuItem<string>(new Text("Enabled")))));
        enabled.Pump(new Size(200, 80));
        RenderPointerListener enabledListener = Assert.Single(
            FindDescendants<RenderPointerListener>(enabled.RenderView),
            listener => listener.OnPointerEnter is not null && listener.OnPointerExit is not null);
        enabledListener.HandleEvent(
            new PointerEnterEvent(
                101,
                PointerDeviceKind.Mouse,
                new Point(10, 10),
                PointerButtons.None,
                DateTime.UtcNow),
            new BoxHitTestEntry(enabledListener, new Point(10, 10)));
        Assert.Equal(SystemMouseCursors.Text, MouseCursorManager.CurrentCursor);

        MouseCursorManager.ResetForTests();
        using var disabled = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new PopupMenuTheme(
                new PopupMenuThemeData(MouseCursor: cursor),
                new PopupMenuItem<string>(new Text("Disabled"), enabled: false))));
        disabled.Pump(new Size(200, 80));
        RenderPointerListener disabledListener = Assert.Single(
            FindDescendants<RenderPointerListener>(disabled.RenderView),
            listener => listener.OnPointerEnter is not null && listener.OnPointerExit is not null);
        disabledListener.HandleEvent(
            new PointerEnterEvent(
                102,
                PointerDeviceKind.Mouse,
                new Point(10, 10),
                PointerButtons.None,
                DateTime.UtcNow),
            new BoxHitTestEntry(disabledListener, new Point(10, 10)));
        Assert.Equal(SystemMouseCursors.Grab, MouseCursorManager.CurrentCursor);
    }

    [Fact]
    public void ShowMenu_ReevaluatesPositionBuilderAndHonorsNoAnimationConstraintsAndClip()
    {
        BuildContext captured = default;
        int positionBuilds = 0;
        Size lastConstraintSize = default;
        Widget BuildRoot(Size size) => new Directionality(
            TextDirection.Ltr,
            new MediaQuery(
                new MediaQueryData(Size: size),
                new Theme(
                    ThemeData.Light,
                    new Navigator(new BuilderPageRoute(context => new CaptureContext(
                        value => captured = value,
                        new Text("Home")))))));
        using var harness = new WidgetRenderHarness(BuildRoot(new Size(500, 360)));
        harness.Pump(new Size(500, 360));
        _ = PopupMenus.ShowMenu(
            captured,
            items: [new PopupMenuItem<string>(new Text("Sized"), value: "sized")],
            positionBuilder: (_, constraints) =>
            {
                positionBuilds++;
                lastConstraintSize = constraints.Biggest;
                return new RelativeRect(20, 20, 200, 200);
            },
            constraints: new BoxConstraints(MinWidth: 180, MaxWidth: 180),
            clipBehavior: Clip.HardEdge,
            popUpAnimationStyle: AnimationStyle.NoAnimation);
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.01));
        harness.Pump(new Size(500, 360));

        Assert.True(positionBuilds >= 1);
        Assert.Equal(new Size(500, 360), lastConstraintSize);
        RenderPopupMenuPositionLayout layout = Assert.Single(
            FindDescendants<RenderPopupMenuPositionLayout>(harness.RenderView));
        Assert.Equal(180, layout.Child!.Size.Width);
        Assert.NotEmpty(FindDescendants<RenderClipPath>(harness.RenderView));

        int previousBuilds = positionBuilds;
        harness.UpdateRoot(BuildRoot(new Size(640, 420)));
        harness.Pump(new Size(640, 420));
        Assert.True(positionBuilds > previousBuilds);
        Assert.Equal(new Size(640, 420), lastConstraintSize);
    }

    private static Widget Wrap(ThemeData theme, Widget child) =>
        new Directionality(
            TextDirection.Ltr,
            new MediaQuery(
                new MediaQueryData(Size: new Size(500, 360)),
                new Theme(theme, child)));

    private static void PumpAnimation()
    {
        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
    }

    private static RenderParagraph? FindParagraph(RenderObject? root, string text) =>
        FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.PlainText == text);

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

    private static string DumpSemantics(SemanticsNode? node, int depth = 0)
    {
        if (node is null) return "<null>";
        string line = $"{new string(' ', depth * 2)}label={node.Label ?? "<null>"}; flags={node.Flags}; actions={node.Actions}";
        return string.Join("\n", new[] { line }.Concat(node.Children.Select(child => DumpSemantics(child, depth + 1))));
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
            _rootElement.Mount(null, null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public void UpdateRoot(Widget widget)
        {
            _rootElement.Update(widget);
        }

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
            public override Element? RenderObjectAttachingChild => _child;
            protected override void OnMount() { base.OnMount(); Rebuild(); }
            public override void Rebuild() { Dirty = false; _child = UpdateChild(_child, Widget, Slot); }
            public override void Update(Widget newWidget) { base.Update(newWidget); Rebuild(); }
            public override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
            public override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
            public void InsertRenderObjectChild(RenderObject child, object? slot) => _renderView.Child = (RenderBox)child;
            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
            public void RemoveRenderObjectChild(RenderObject child, object? slot) { if (ReferenceEquals(_renderView.Child, child)) _renderView.Child = null; }
            public override void Unmount()
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

    /// <summary>
    /// Whether one of the node's merged label parts is <paramref name="part"/>. A merged node joins
    /// the labels it absorbed with a newline, exactly like Flutter's <c>_concatAttributedString</c>.
    /// </summary>
    private static bool HasLabelPart(SemanticsNode node, string part) =>
        node.Label?.Split('\n').Contains(part) == true;
}

