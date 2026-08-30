using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

/// Mirrors `material_ui/test/switch_list_tile_test.dart`.
[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialSwitchListTileTests
{
    public MaterialSwitchListTileTests()
    {
        FocusManager.Instance.ResetForTests();
        GestureBinding.Instance.ResetForTests();
    }

    [Fact]
    public void Constructor_RequiresSubtitleForThreeLine()
    {
        Assert.Throws<ArgumentException>(() => new SwitchListTile(
            value: false,
            onChanged: _ => { },
            isThreeLine: true));
        Assert.Throws<ArgumentException>(() => SwitchListTile.Adaptive(
            value: false,
            onChanged: _ => { },
            isThreeLine: true));
    }

    /// The thumb-image error callbacks require their image, exactly as Flutter's two asserts do.
    [Fact]
    public void Constructor_ThumbImageErrorCallbacksRequireTheirImage()
    {
        Assert.Throws<ArgumentException>(() => new SwitchListTile(
            value: false,
            onChanged: _ => { },
            onActiveThumbImageError: (_, _) => { }));
        Assert.Throws<ArgumentException>(() => new SwitchListTile(
            value: false,
            onChanged: _ => { },
            onInactiveThumbImageError: (_, _) => { }));
    }

    /// "SwitchListTile control test" — tapping the tile toggles the value.
    [Fact]
    public void ControlTest_WholeTileTapTogglesValue()
    {
        var log = new List<bool>();
        using var harness = new ListTileControlHarness(new SwitchListTile(
            value: false,
            onChanged: log.Add,
            title: new Text("Hello")));

        harness.Pump();
        harness.TapTile(pointer: 711);

        Assert.Equal([true], log);
    }

    [Fact]
    public void ControlTest_WholeTileTapTogglesBackToFalse()
    {
        var log = new List<bool>();
        using var harness = new ListTileControlHarness(new SwitchListTile(
            value: true,
            onChanged: log.Add,
            title: new Text("Hello")));

        harness.Pump();
        harness.TapTile(pointer: 712);

        Assert.Equal([false], log);
    }

    /// A null `onChanged` disables the tile and its switch.
    [Fact]
    public void NullOnChanged_DisablesTheTileAndTheSwitch()
    {
        using var harness = new ListTileControlHarness(new SwitchListTile(
            value: true,
            onChanged: null,
            title: new Text("Read only")));

        harness.Pump();

        ListTile tile = harness.FindWidget<ListTile>()!;
        Assert.False(tile.Enabled);
        Assert.Null(tile.OnTap);
        Assert.Null(harness.FindWidget<Switch>()!.OnChanged);
    }

    /// "Switch on SwitchListTile changes mouse cursor when hovered" — the cursor reaches the
    /// `Switch`, and Flutter never puts it on the backing `ListTile`.
    [Fact]
    public void MouseCursor_ReachesTheSwitchAndNotTheTile()
    {
        using var harness = new ListTileControlHarness(new SwitchListTile(
            value: false,
            onChanged: _ => { },
            mouseCursor: SystemMouseCursors.Text,
            title: new Text("Cursor")));

        harness.Pump();

        Assert.Same(SystemMouseCursors.Text, harness.FindWidget<Switch>()!.MouseCursor);
        Assert.Null(harness.FindWidget<ListTile>()!.MouseCursor);
    }

    /// "SwitchListTile passes the value of dragStartBehavior to Switch".
    [Fact]
    public void DragStartBehavior_ReachesTheSwitch()
    {
        using var harness = new ListTileControlHarness(new SwitchListTile(
            value: false,
            onChanged: _ => { },
            dragStartBehavior: DragStartBehavior.Down,
            title: new Text("Drag")));

        harness.Pump();

        Assert.Equal(DragStartBehavior.Down, harness.FindWidget<Switch>()!.DragStartBehavior);

        harness.Update(new SwitchListTile(
            value: false,
            onChanged: _ => { },
            title: new Text("Drag")));

        Assert.Equal(DragStartBehavior.Start, harness.FindWidget<Switch>()!.DragStartBehavior);
    }

    /// The switch-only arguments — colours, thumb icon, splash radius, overlay — reach the `Switch`.
    [Fact]
    public void SwitchArguments_AreForwardedToTheSwitch()
    {
        MaterialStateProperty<Color?> thumb = MaterialStateProperty<Color?>.All(Colors.Green);
        MaterialStateProperty<Color?> track = MaterialStateProperty<Color?>.All(Colors.Blue);
        MaterialStateProperty<Color?> outline = MaterialStateProperty<Color?>.All(Colors.Red);
        MaterialStateProperty<Icon?> thumbIcon = MaterialStateProperty<Icon?>.All(new Icon(Icons.Done));
        MaterialStateProperty<Color?> overlay = MaterialStateProperty<Color?>.All(Colors.Purple);
        using var harness = new ListTileControlHarness(new SwitchListTile(
            value: true,
            onChanged: _ => { },
            activeThumbColor: Colors.Coral,
            activeTrackColor: Colors.Aqua,
            inactiveThumbColor: Colors.Gray,
            inactiveTrackColor: Colors.Silver,
            thumbColor: thumb,
            trackColor: track,
            trackOutlineColor: outline,
            thumbIcon: thumbIcon,
            overlayColor: overlay,
            splashRadius: 27.0,
            title: new Text("Args")));

        harness.Pump();

        Switch control = harness.FindWidget<Switch>()!;
        Assert.Equal(Colors.Coral, control.ActiveThumbColor);
        Assert.Equal(Colors.Aqua, control.ActiveTrackColor);
        Assert.Equal(Colors.Gray, control.InactiveThumbColor);
        Assert.Equal(Colors.Silver, control.InactiveTrackColor);
        Assert.Same(thumb, control.ThumbColor);
        Assert.Same(track, control.TrackColor);
        Assert.Same(outline, control.TrackOutlineColor);
        Assert.Same(thumbIcon, control.ThumbIcon);
        Assert.Same(overlay, control.OverlayColor);
        Assert.Equal(27.0, control.SplashRadius);
    }

    /// "Material3 - SwitchListTile respects materialTapTargetSize" — the default is `shrinkWrap`.
    [Fact]
    public void MaterialTapTargetSize_DefaultsToShrinkWrap()
    {
        using var harness = new ListTileControlHarness(new SwitchListTile(
            value: false,
            onChanged: _ => { },
            title: new Text("Tap target")));

        harness.Pump();

        Assert.Equal(MaterialTapTargetSize.ShrinkWrap, harness.FindWidget<Switch>()!.MaterialTapTargetSize);
    }

    /// "SwitchListTile.control widget should not request focus on traversal".
    [Fact]
    public void EmbeddedSwitch_IsWrappedInExcludeFocus()
    {
        using var harness = new ListTileControlHarness(new SwitchListTile(
            value: false,
            onChanged: _ => { },
            title: new Text("Focus")));

        harness.Pump();

        Assert.NotNull(harness.FindWidget<ExcludeFocus>());
    }

    /// "SwitchListTile controlAffinity test" and "controlAffinity default value test" — the theme
    /// supplies the default, the widget argument overrides it, and `platform` reads as trailing.
    [Fact]
    public void ControlAffinity_ReadsTheThemeAndIsOverriddenByTheArgument()
    {
        var theme = ThemeData.Light with
        {
            ListTileTheme = new ListTileThemeData(ControlAffinity: ListTileControlAffinity.Leading)
        };
        using var harness = new ListTileControlHarness(
            new SwitchListTile(
                value: false,
                onChanged: _ => { },
                title: new Text("Affinity"),
                secondary: new Icon(Icons.InfoOutline)),
            theme);

        harness.Pump();
        Assert.True(HasSwitch(harness.Tile.Leading));
        Assert.False(HasSwitch(harness.Tile.Trailing));

        harness.Update(
            new SwitchListTile(
                value: false,
                onChanged: _ => { },
                controlAffinity: ListTileControlAffinity.Trailing,
                title: new Text("Affinity"),
                secondary: new Icon(Icons.InfoOutline)),
            theme);
        Assert.False(HasSwitch(harness.Tile.Leading));
        Assert.True(HasSwitch(harness.Tile.Trailing));
    }

    [Fact]
    public void ControlAffinity_DefaultsToTrailing()
    {
        using var harness = new ListTileControlHarness(new SwitchListTile(
            value: false,
            onChanged: _ => { },
            title: new Text("Default"),
            secondary: new Icon(Icons.InfoOutline)));

        harness.Pump();

        Assert.False(HasSwitch(harness.Tile.Leading));
        Assert.True(HasSwitch(harness.Tile.Trailing));
    }

    /// "SwitchListTile selected item text Color" and "Material3 - SwitchListTile activeThumbColor" —
    /// `activeThumbColor`, then the deprecated `activeColor`, then the switch theme's selected thumb
    /// colour, then `colorScheme.secondary`.
    [Fact]
    public void SelectedColor_FollowsFlutterPrecedence()
    {
        var activeThumb = Color.Parse("#FF8C1D40");
        var active = Color.Parse("#FF2D6A4F");
        var themed = Color.Parse("#FF006C4C");
        var secondary = Color.Parse("#FF735C00");
        var bare = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(secondary: secondary)
        };
        var themedThumb = bare with
        {
            SwitchTheme = new SwitchThemeData(
                ThumbColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Selected) ? themed : null))
        };

        Assert.Equal(activeThumb, TitleColor(activeThumb, active, themedThumb));
        Assert.Equal(active, TitleColor(null, active, themedThumb));
        Assert.Equal(themed, TitleColor(null, null, themedThumb));
        Assert.Equal(secondary, TitleColor(null, null, bare));

        static Color? TitleColor(Color? activeThumbColor, Color? activeColor, ThemeData theme)
        {
            using var harness = new ListTileControlHarness(
                new SwitchListTile(
                    value: true,
                    selected: true,
                    activeThumbColor: activeThumbColor,
                    activeColor: activeColor,
                    onChanged: _ => { },
                    title: new Text("Title")),
                theme);
            harness.Pump();
            return ListTileControlHarness.ForegroundOf(
                ListTileControlHarness.FindText(harness.RenderView, "Title"));
        }
    }

    /// "SwitchListTile contentPadding" plus the tile-geometry arguments the list tile owns.
    [Fact]
    public void TileArguments_AreForwardedToTheListTile()
    {
        var statesController = new MaterialStatesController();
        var focusNode = new FocusNode();
        var shape = new RoundedRectangleBorder(borderRadius: BorderRadius.Circular(12));
        using var harness = new ListTileControlHarness(new SwitchListTile(
            value: false,
            onChanged: _ => { },
            contentPadding: EdgeInsets.Symmetric(horizontal: 10, vertical: 4),
            shape: shape,
            tileColor: Colors.Beige,
            selectedTileColor: Colors.Aqua,
            hoverColor: Colors.Yellow,
            enableFeedback: false,
            horizontalTitleGap: 24,
            minVerticalPadding: 12,
            minLeadingWidth: 60,
            minTileHeight: 72,
            visualDensity: VisualDensity.Compact,
            dense: true,
            statesController: statesController,
            focusNode: focusNode,
            internalAddSemanticForOnTap: true,
            title: new Text("Args")));

        harness.Pump();

        ListTile tile = harness.FindWidget<ListTile>()!;
        Assert.Equal(EdgeInsets.Symmetric(horizontal: 10, vertical: 4), tile.ContentPadding);
        Assert.Same(shape, tile.Shape);
        Assert.Equal(Colors.Beige, tile.TileColor);
        Assert.Equal(Colors.Aqua, tile.SelectedTileColor);
        Assert.Equal(Colors.Yellow, tile.HoverColor);
        Assert.False(tile.EnableFeedback);
        Assert.Equal(24, tile.HorizontalTitleGap);
        Assert.Equal(12, tile.MinVerticalPadding);
        Assert.Equal(60, tile.MinLeadingWidth);
        Assert.Equal(72, tile.MinTileHeight);
        Assert.Equal(VisualDensity.Compact, tile.VisualDensity);
        Assert.True(tile.Dense);
        Assert.Same(statesController, tile.StatesController);
        Assert.Same(focusNode, tile.FocusNode);
        Assert.True(tile.InternalAddSemanticForOnTap);
    }

    /// "SwitchListTile semantics test" — the tile is one merge root carrying the switch's checked
    /// state and the tile's tap action (see `docs/ai/DIVERGENCES.md` on flattened `SemanticsData`).
    [Fact]
    public void Semantics_MergesSwitchStateAndTileTap()
    {
        using var harness = new ListTileControlHarness(new SwitchListTile(
            value: true,
            onChanged: _ => { },
            title: new Text("Notifications")));

        SemanticsNode root = harness.PumpSemantics();

        SemanticsNode mergeRoot = Assert.IsType<SemanticsNode>(ListTileControlHarness.FindSemantics(
            root,
            static candidate => candidate.MergeAllDescendantsIntoThisNode));
        Assert.True(mergeRoot.Actions.HasFlag(SemanticsActions.Tap));

        SemanticsNode toggledNode = Assert.IsType<SemanticsNode>(ListTileControlHarness.FindSemantics(
            mergeRoot,
            static candidate => candidate.Flags.HasFlag(SemanticsFlags.IsToggled)));
        Assert.True(toggledNode.Flags.HasFlag(SemanticsFlags.HasToggledState));
    }

    /// "SwitchListTile.adaptive only uses material switch" — the adaptive branch reaches
    /// `Switch.adaptive`, which on Apple platforms paints the Cupertino geometry (Plumix keeps that
    /// inside `SwitchPainter` instead of building a `CupertinoSwitch`; see `MaterialSwitchTests`).
    /// `applyCupertinoTheme` exists only on the adaptive constructor.
    [Fact]
    public void Adaptive_PaintsTheCupertinoGeometryOnApplePlatforms()
    {
        using var harness = new ListTileControlHarness(
            SwitchListTile.Adaptive(
                value: true,
                onChanged: _ => { },
                applyCupertinoTheme: true,
                title: new Text("Adaptive")),
            ThemeData.Light with { Platform = TargetPlatform.IOS });

        harness.Pump();

        Assert.True(harness.FindWidget<Switch>()!.ApplyCupertinoTheme);
        Assert.True(SwitchPainterOf(harness).IsCupertino);

        harness.Update(
            SwitchListTile.Adaptive(
                value: true,
                onChanged: _ => { },
                title: new Text("Adaptive")),
            ThemeData.Light with { Platform = TargetPlatform.Android });

        Assert.False(SwitchPainterOf(harness).IsCupertino);
    }

    [Fact]
    public void Material_NeverAppliesTheCupertinoTheme()
    {
        using var harness = new ListTileControlHarness(
            new SwitchListTile(value: true, onChanged: _ => { }, title: new Text("Material")),
            ThemeData.Light with { Platform = TargetPlatform.IOS });

        harness.Pump();

        Assert.False(harness.FindWidget<Switch>()!.ApplyCupertinoTheme);
        Assert.False(SwitchPainterOf(harness).IsCupertino);
    }

    /// "SwitchListTile respects visualDensity" and "minTileHeight" — the resolved geometry reaches
    /// the tile the same way it does for `CheckboxListTile`.
    [Fact]
    public void VisualDensityAndMinTileHeight_DriveTheTileSurface()
    {
        using var harness = new ListTileControlHarness(new SwitchListTile(
            value: false,
            onChanged: _ => { },
            title: new Text("Tall"),
            minTileHeight: 72));

        harness.Pump();
        Assert.Equal(72, harness.Tile.Size.Height, 3);

        harness.Update(new SwitchListTile(
            value: false,
            onChanged: _ => { },
            title: new Text("Compact"),
            visualDensity: VisualDensity.Compact));
        Assert.Equal(48, harness.Tile.Size.Height, 3);
    }

    /// "SwitchListTile forwards statesController to ListTile".
    [Fact]
    public void StatesController_IsDrivenByTheTile()
    {
        var statesController = new MaterialStatesController();
        using var harness = new ListTileControlHarness(new SwitchListTile(
            value: false,
            onChanged: _ => { },
            statesController: statesController,
            title: new Text("Stateful")));

        harness.Pump();
        Assert.Same(statesController, harness.FindWidget<ListTile>()!.StatesController);

        statesController.Update(MaterialState.Pressed, true);
        harness.Pump();
        Assert.True(statesController.Value.HasFlag(MaterialState.Pressed));
    }

    /// "SwitchListTile does not crash at zero area".
    [Fact]
    public void ZeroArea_LaysOutWithoutCrashing()
    {
        using var harness = new ListTileControlHarness(
            new SwitchListTile(
                value: false,
                onChanged: _ => { },
                title: new Text("Zero"),
                secondary: new Icon(Icons.Done)),
            width: 0.0,
            height: 0.0,
            viewWidth: 0.0,
            viewHeight: 0.0);

        harness.Pump();

        Assert.Equal(new Size(), harness.Tile.Size);
    }

    /// The gallery demo page pumps end to end, so the sample stays in step with the control.
    [Fact]
    public void DemoPage_PumpsWithoutOverflow()
    {
        using var harness = new ListTileControlHarness(
            new SizedBox(height: 520, child: new SwitchListTileDemoPage()),
            width: 760.0,
            height: 520.0,
            viewWidth: 760.0,
            viewHeight: 520.0);

        harness.Pump();

        Assert.DoesNotContain(
            ListTileControlHarness.FindAll<RenderFlex>(harness.RenderView),
            static flex => flex._hasOverflow);
    }

    private static SwitchPainter SwitchPainterOf(ListTileControlHarness harness) =>
        Assert.IsType<SwitchPainter>(
            ListTileControlHarness.FindAll<RenderCustomPaint>(harness.RenderView)
                .Select(static paint => paint.Painter)
                .OfType<SwitchPainter>()
                .FirstOrDefault());

    private static bool HasSwitch(RenderObject? slot) =>
        ListTileControlHarness.FindAll<RenderCustomPaint>(slot)
            .Any(static paint => paint.Painter is SwitchPainter);
}
