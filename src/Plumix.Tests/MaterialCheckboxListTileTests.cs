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

/// Mirrors `material_ui/test/checkbox_list_tile_test.dart`.
[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialCheckboxListTileTests
{
    public MaterialCheckboxListTileTests()
    {
        FocusManager.Instance.ResetForTests();
        GestureBinding.Instance.ResetForTests();
    }

    [Fact]
    public void Constructor_RejectsNullValueWhenNotTristate()
    {
        Assert.Throws<ArgumentException>(() => new CheckboxListTile(value: null, onChanged: _ => { }));
        Assert.Throws<ArgumentException>(() => CheckboxListTile.Adaptive(value: null, onChanged: _ => { }));
    }

    [Fact]
    public void Constructor_RequiresSubtitleForThreeLine()
    {
        Assert.Throws<ArgumentException>(() => new CheckboxListTile(
            value: false,
            onChanged: _ => { },
            isThreeLine: true));
    }

    [Fact]
    public void Constructor_AcceptsNullValueWhenTristate()
    {
        var tile = new CheckboxListTile(value: null, tristate: true, onChanged: _ => { });

        Assert.Null(tile.Value);
        Assert.True(tile.Tristate);
    }

    /// "CheckboxListTile control test" — tapping the tile toggles through `onChanged`.
    [Fact]
    public void ControlTest_WholeTileTapTogglesValue()
    {
        var log = new List<bool?>();
        using var harness = new ListTileControlHarness(new CheckboxListTile(
            value: false,
            onChanged: log.Add,
            title: new Text("Hello")));

        harness.Pump();
        harness.TapTile(pointer: 701);

        Assert.Equal([true], log);
    }

    /// "CheckboxListTile tristate test" — false -> true -> null -> false.
    [Fact]
    public void TristateTest_TileTapCyclesThroughNull()
    {
        bool? value = false;
        using var harness = new ListTileControlHarness(BuildTristate(value, next => value = next));

        harness.Pump();
        harness.TapTile(pointer: 702);
        Assert.True(value);

        harness.Update(BuildTristate(value, next => value = next));
        harness.TapTile(pointer: 703);
        Assert.Null(value);

        harness.Update(BuildTristate(value, next => value = next));
        harness.TapTile(pointer: 704);
        Assert.False(value);

        static CheckboxListTile BuildTristate(bool? current, Action<bool?> onChanged) =>
            new(value: current, tristate: true, onChanged: onChanged, title: new Text("Tristate"));
    }

    /// "CheckboxListTile can be disabled" — `enabled: false` stops the tile tap and the checkbox.
    [Fact]
    public void Disabled_BlocksTileTapAndDisablesTheCheckbox()
    {
        int changes = 0;
        using var harness = new ListTileControlHarness(new CheckboxListTile(
            value: true,
            enabled: false,
            onChanged: _ => changes += 1,
            title: new Text("Disabled")));

        harness.Pump();
        harness.TapTile(pointer: 705);

        Assert.Equal(0, changes);
        Checkbox checkbox = Assert.IsType<Checkbox>(harness.FindWidget<Checkbox>());
        Assert.Null(checkbox.OnChanged);
    }

    /// A null `onChanged` leaves the backing tile disabled even when `enabled` is not given.
    [Fact]
    public void NullOnChanged_DisablesTheTile()
    {
        using var harness = new ListTileControlHarness(new CheckboxListTile(
            value: true,
            onChanged: null,
            title: new Text("Read only")));

        harness.Pump();

        ListTile tile = Assert.IsType<ListTile>(harness.FindWidget<ListTile>());
        Assert.False(tile.Enabled);
        Assert.Null(tile.OnTap);
    }

    /// "CheckboxListTile respects mouseCursor when hovered" — the cursor reaches the `Checkbox`,
    /// and Flutter never puts it on the backing `ListTile`.
    [Fact]
    public void MouseCursor_ReachesTheCheckboxAndNotTheTile()
    {
        using var harness = new ListTileControlHarness(new CheckboxListTile(
            value: true,
            onChanged: _ => { },
            mouseCursor: SystemMouseCursors.Text,
            title: new Text("Cursor")));

        harness.Pump();

        Assert.Same(SystemMouseCursors.Text, harness.FindWidget<Checkbox>()!.MouseCursor);
        Assert.Null(harness.FindWidget<ListTile>()!.MouseCursor);
    }

    /// "CheckboxListTile respects checkbox shape and side" plus the checkbox-only colour arguments.
    [Fact]
    public void CheckboxArguments_AreForwardedToTheCheckbox()
    {
        var shape = new RoundedRectangleBorder(borderRadius: BorderRadius.Circular(4));
        var side = WidgetStateBorderSide.ResolveWith(_ => new BorderSide(color: Colors.Red, width: 4));
        MaterialStateProperty<Color?> fill = MaterialStateProperty<Color?>.All(Colors.Green);
        MaterialStateProperty<Color?> overlay = MaterialStateProperty<Color?>.All(Colors.Blue);
        using var harness = new ListTileControlHarness(new CheckboxListTile(
            value: true,
            onChanged: _ => { },
            checkboxShape: shape,
            side: side,
            fillColor: fill,
            checkColor: Colors.White,
            hoverColor: Colors.Yellow,
            overlayColor: overlay,
            splashRadius: 27.0,
            isError: true,
            checkboxSemanticLabel: "there",
            title: new Text("Args")));

        harness.Pump();

        Checkbox checkbox = harness.FindWidget<Checkbox>()!;
        Assert.Same(shape, checkbox.Shape);
        Assert.Same(side, checkbox.Side);
        Assert.Same(fill, checkbox.FillColor);
        Assert.Equal(Colors.White, checkbox.CheckColor);
        Assert.Equal(Colors.Yellow, checkbox.HoverColor);
        Assert.Same(overlay, checkbox.OverlayColor);
        Assert.Equal(27.0, checkbox.SplashRadius);
        Assert.True(checkbox.IsError);
        Assert.Equal("there", checkbox.SemanticLabel);
    }

    /// "CheckboxListTile respects materialTapTargetSize" — the default is `shrinkWrap`, not the
    /// checkbox's own `padded` default.
    [Fact]
    public void MaterialTapTargetSize_DefaultsToShrinkWrap()
    {
        using var harness = new ListTileControlHarness(new CheckboxListTile(
            value: true,
            onChanged: _ => { },
            title: new Text("Tap target")));

        harness.Pump();

        Assert.Equal(MaterialTapTargetSize.ShrinkWrap, harness.FindWidget<Checkbox>()!.MaterialTapTargetSize);

        harness.Update(new CheckboxListTile(
            value: true,
            onChanged: _ => { },
            materialTapTargetSize: MaterialTapTargetSize.Padded,
            title: new Text("Tap target")));

        Assert.Equal(MaterialTapTargetSize.Padded, harness.FindWidget<Checkbox>()!.MaterialTapTargetSize);
    }

    /// "CheckboxListTile.control widget should not request focus on traversal" — the checkbox sits
    /// under an `ExcludeFocus`, so only the tile is focusable.
    [Fact]
    public void EmbeddedCheckbox_IsWrappedInExcludeFocus()
    {
        using var harness = new ListTileControlHarness(new CheckboxListTile(
            value: true,
            onChanged: _ => { },
            title: new Text("Focus")));

        harness.Pump();

        Assert.NotNull(harness.FindWidget<ExcludeFocus>());
    }

    /// "CheckboxListTile uses ListTileTheme controlAffinity" — and the widget argument wins over it.
    [Fact]
    public void ControlAffinity_ReadsTheThemeAndIsOverriddenByTheArgument()
    {
        var theme = ThemeData.Light with
        {
            ListTileTheme = new ListTileThemeData(ControlAffinity: ListTileControlAffinity.Leading)
        };
        using var harness = new ListTileControlHarness(
            new CheckboxListTile(
                value: false,
                onChanged: _ => { },
                title: new Text("Affinity"),
                secondary: new Icon(Icons.InfoOutline)),
            theme);

        harness.Pump();
        Assert.True(HasCheckbox(harness.Tile.Leading));
        Assert.False(HasCheckbox(harness.Tile.Trailing));

        harness.Update(
            new CheckboxListTile(
                value: false,
                onChanged: _ => { },
                controlAffinity: ListTileControlAffinity.Trailing,
                title: new Text("Affinity"),
                secondary: new Icon(Icons.InfoOutline)),
            theme);

        Assert.False(HasCheckbox(harness.Tile.Leading));
        Assert.True(HasCheckbox(harness.Tile.Trailing));
    }

    /// `ListTileControlAffinity.platform` puts the checkbox on the trailing edge, unlike
    /// `RadioListTile`, which treats `platform` as leading.
    [Fact]
    public void ControlAffinity_PlatformKeepsTheCheckboxTrailing()
    {
        using var harness = new ListTileControlHarness(new CheckboxListTile(
            value: false,
            onChanged: _ => { },
            controlAffinity: ListTileControlAffinity.Platform,
            title: new Text("Platform"),
            secondary: new Icon(Icons.InfoOutline)));

        harness.Pump();

        Assert.False(HasCheckbox(harness.Tile.Leading));
        Assert.True(HasCheckbox(harness.Tile.Trailing));
    }

    /// "CheckboxListTile renders with default scale" — no `Transform` is inserted at 1.0.
    [Fact]
    public void ScaleFactor_DefaultInsertsNoTransform()
    {
        using var harness = new ListTileControlHarness(new CheckboxListTile(
            value: false,
            onChanged: _ => { },
            title: new Text("Unscaled")));

        harness.Pump();

        Assert.Null(ListTileControlHarness.Find<RenderTransform>(harness.RenderView));
    }

    /// "CheckboxListTile respects checkboxScaleFactor" — `Transform.scale`, i.e. a centre-aligned
    /// uniform scale, not a translate/scale/translate around the checkbox's own corner.
    [Fact]
    public void ScaleFactor_UsesCenteredUniformScale()
    {
        const double scaleFactor = 1.5;
        using var harness = new ListTileControlHarness(new CheckboxListTile(
            value: false,
            onChanged: _ => { },
            checkboxScaleFactor: scaleFactor,
            title: new Text("Scaled")));

        harness.Pump();

        RenderTransform transform = Assert.IsType<RenderTransform>(
            ListTileControlHarness.Find<RenderTransform>(harness.RenderView));
        Assert.Equal(Matrix4.Diagonal3Values(scaleFactor, scaleFactor, 1.0), transform.Transform);
        Assert.Equal(Alignment.Center, transform.Alignment);
    }

    /// "CheckboxListTile selected item text Color" — `activeColor`, then the checkbox theme's
    /// selected fill, then `colorScheme.secondary`.
    [Fact]
    public void SelectedColor_FollowsFlutterPrecedence()
    {
        var active = Color.Parse("#FF8C1D40");
        var themed = Color.Parse("#FF006C4C");
        var secondary = Color.Parse("#FF735C00");
        var bare = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(secondary: secondary)
        };
        var themedFill = bare with
        {
            CheckboxTheme = new CheckboxThemeData(
                FillColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Selected) ? themed : null))
        };

        Assert.Equal(active, TitleColor(activeColor: active, theme: themedFill));
        Assert.Equal(themed, TitleColor(activeColor: null, theme: themedFill));
        Assert.Equal(secondary, TitleColor(activeColor: null, theme: bare));

        static Color? TitleColor(Color? activeColor, ThemeData theme)
        {
            using var harness = new ListTileControlHarness(
                new CheckboxListTile(
                    value: true,
                    selected: true,
                    activeColor: activeColor,
                    onChanged: _ => { },
                    title: new Text("Title")),
                theme);
            harness.Pump();
            return ListTileControlHarness.ForegroundOf(
                ListTileControlHarness.FindText(harness.RenderView, "Title"));
        }
    }

    /// "CheckboxListTile contentPadding test" plus the tile-geometry arguments the list tile owns.
    [Fact]
    public void TileArguments_AreForwardedToTheListTile()
    {
        var statesController = new MaterialStatesController();
        var focusNode = new FocusNode();
        var shape = new RoundedRectangleBorder(borderRadius: BorderRadius.Circular(12));
        using var harness = new ListTileControlHarness(new CheckboxListTile(
            value: false,
            onChanged: _ => { },
            contentPadding: EdgeInsets.Symmetric(horizontal: 10, vertical: 4),
            shape: shape,
            tileColor: Colors.Beige,
            selectedTileColor: Colors.Aqua,
            enableFeedback: false,
            horizontalTitleGap: 24,
            minVerticalPadding: 12,
            minLeadingWidth: 60,
            minTileHeight: 72,
            visualDensity: VisualDensity.Compact,
            titleAlignment: ListTileTitleAlignment.Top,
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
        Assert.False(tile.EnableFeedback);
        Assert.Equal(24, tile.HorizontalTitleGap);
        Assert.Equal(12, tile.MinVerticalPadding);
        Assert.Equal(60, tile.MinLeadingWidth);
        Assert.Equal(72, tile.MinTileHeight);
        Assert.Equal(VisualDensity.Compact, tile.VisualDensity);
        Assert.Equal(ListTileTitleAlignment.Top, tile.TitleAlignment);
        Assert.True(tile.Dense);
        Assert.Same(statesController, tile.StatesController);
        Assert.Same(focusNode, tile.FocusNode);
        Assert.True(tile.InternalAddSemanticForOnTap);
    }

    /// "CheckboxListTile has proper semantics" — the tile is one merge root that carries the
    /// checkbox's checked state and the tile's tap action, with no extra `Semantics` wrapper of
    /// Plumix's own. Flutter asserts the *flattened* label `'Hello\nthere'`; Plumix keeps the
    /// annotations on the nodes under the merge root (`docs/ai/DIVERGENCES.md`, `SemanticsData` /
    /// `getSemanticsData` are not ported), so the labels are asserted where they live.
    [Fact]
    public void Semantics_MergesCheckboxStateAndTileTap()
    {
        using var harness = new ListTileControlHarness(new CheckboxListTile(
            value: true,
            onChanged: _ => { },
            title: new Text("Hello"),
            checkboxSemanticLabel: "there",
            internalAddSemanticForOnTap: true));

        SemanticsNode root = harness.PumpSemantics();

        SemanticsNode mergeRoot = Assert.IsType<SemanticsNode>(ListTileControlHarness.FindSemantics(
            root,
            static candidate => candidate.MergeAllDescendantsIntoThisNode));
        Assert.True(mergeRoot.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.True(mergeRoot.Actions.HasFlag(SemanticsActions.Tap));

        SemanticsNode tileNode = Assert.IsType<SemanticsNode>(ListTileControlHarness.FindSemantics(
            mergeRoot,
            static candidate => candidate.Flags.HasFlag(SemanticsFlags.IsButton)));
        Assert.Equal("Hello", tileNode.Label);
        Assert.True(tileNode.Flags.HasFlag(SemanticsFlags.HasSelectedState));

        SemanticsNode checkboxNode = Assert.IsType<SemanticsNode>(ListTileControlHarness.FindSemantics(
            mergeRoot,
            static candidate => candidate.Flags.HasFlag(SemanticsFlags.IsChecked)));
        Assert.True(checkboxNode.Flags.HasFlag(SemanticsFlags.HasCheckedState));
        Assert.True(checkboxNode.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.Equal("there", checkboxNode.Label);
    }

    [Fact]
    public void Semantics_DisabledTileDropsEnabledAndTap()
    {
        using var harness = new ListTileControlHarness(new CheckboxListTile(
            value: true,
            enabled: false,
            onChanged: _ => { },
            title: new Text("Disabled")));

        SemanticsNode root = harness.PumpSemantics();

        SemanticsNode node = Assert.IsType<SemanticsNode>(ListTileControlHarness.FindSemantics(
            root,
            static candidate => candidate.Flags.HasFlag(SemanticsFlags.IsChecked)));
        Assert.False(node.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.False(node.Actions.HasFlag(SemanticsActions.Tap));
    }

    /// "CheckboxListTile.adaptive shows the correct checkbox platform widget".
    [Fact]
    public void Adaptive_UsesTheCupertinoCheckboxOnApplePlatforms()
    {
        var theme = ThemeData.Light with { Platform = TargetPlatform.IOS };
        using var harness = new ListTileControlHarness(
            CheckboxListTile.Adaptive(
                value: true,
                onChanged: _ => { },
                title: new Text("Adaptive")),
            theme);

        harness.Pump();

        Assert.NotNull(harness.FindWidget<Plumix.Cupertino.CupertinoCheckbox>());

        harness.Update(
            CheckboxListTile.Adaptive(
                value: true,
                onChanged: _ => { },
                title: new Text("Adaptive")),
            ThemeData.Light with { Platform = TargetPlatform.Android });

        Assert.Null(harness.FindWidget<Plumix.Cupertino.CupertinoCheckbox>());
    }

    /// "CheckboxListTile respects visualDensity" and "minTileHeight" — the tile's `Material` surface
    /// takes the resolved geometry, and the secondary widget lands in the slot the affinity picked.
    [Fact]
    public void VisualDensityAndMinTileHeight_DriveTheTileSurface()
    {
        var theme = ThemeData.Light with
        {
            ListTileTheme = new ListTileThemeData(ControlAffinity: ListTileControlAffinity.Leading)
        };
        using var harness = new ListTileControlHarness(
            new CheckboxListTile(
                value: false,
                onChanged: _ => { },
                title: new Text("Leading control"),
                secondary: new Icon(Icons.InfoOutline),
                minTileHeight: 72,
                horizontalTitleGap: 24),
            theme);

        harness.Pump();
        Assert.Equal(72, harness.Tile.Size.Height, 3);
        Assert.True(HasCheckbox(harness.Tile.Leading));

        harness.Update(
            new CheckboxListTile(
                value: false,
                onChanged: _ => { },
                title: new Text("Compact"),
                visualDensity: VisualDensity.Compact),
            theme);
        Assert.Equal(48, harness.Tile.Size.Height, 3);
    }

    /// "titleAlignment position with title and subtitle widgets" — the widget argument overrides the
    /// ambient `ListTileThemeData.titleAlignment` and moves the secondary slot vertically.
    [Fact]
    public void TitleAlignment_OverridesTheThemeAndMovesTheSecondarySlot()
    {
        var theme = ThemeData.Light with
        {
            ListTileTheme = new ListTileThemeData(TitleAlignment: ListTileTitleAlignment.Bottom)
        };
        double topY = SecondaryOffset(ListTileTitleAlignment.Top, theme);
        double bottomY = SecondaryOffset(null, theme);

        Assert.True(topY < bottomY, $"Expected the top-aligned secondary above the bottom one ({topY} < {bottomY}).");

        static double SecondaryOffset(ListTileTitleAlignment? alignment, ThemeData theme)
        {
            using var harness = new ListTileControlHarness(
                new CheckboxListTile(
                    value: false,
                    onChanged: _ => { },
                    title: new Text("Title"),
                    subtitle: new Text("Subtitle"),
                    isThreeLine: true,
                    secondary: new Icon(Icons.InfoOutline),
                    titleAlignment: alignment),
                theme);
            harness.Pump();
            string glyph = char.ConvertFromUtf32(Icons.InfoOutline.CodePoint);
            RenderParagraph icon = Assert.IsType<RenderParagraph>(
                ListTileControlHarness.FindText(harness.RenderView, glyph));
            return ListTileControlHarness.GlobalOffsetOf(icon).Y;
        }
    }

    /// "CheckboxListTile forwards statesController to ListTile" — the caller's controller is the one
    /// the tile drives, so external updates land on it.
    [Fact]
    public void StatesController_IsDrivenByTheTile()
    {
        var statesController = new MaterialStatesController();
        using var harness = new ListTileControlHarness(new CheckboxListTile(
            value: false,
            onChanged: _ => { },
            selected: true,
            statesController: statesController,
            title: new Text("Stateful")));

        harness.Pump();
        Assert.Same(statesController, harness.FindWidget<ListTile>()!.StatesController);
        Assert.False(statesController.Value.HasFlag(MaterialState.Selected));

        statesController.Update(MaterialState.Pressed, true);
        harness.Pump();
        Assert.True(statesController.Value.HasFlag(MaterialState.Pressed));
    }

    /// "CheckboxListTile does not crash at zero area".
    [Fact]
    public void ZeroArea_LaysOutWithoutCrashing()
    {
        using var harness = new ListTileControlHarness(
            new CheckboxListTile(
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
            new SizedBox(height: 520, child: new CheckboxListTileDemoPage()),
            width: 760.0,
            height: 520.0,
            viewWidth: 760.0,
            viewHeight: 520.0);

        harness.Pump();

        Assert.DoesNotContain(
            ListTileControlHarness.FindAll<RenderFlex>(harness.RenderView),
            static flex => flex._hasOverflow);
    }

    private static bool HasCheckbox(RenderObject? slot) =>
        ListTileControlHarness.FindAll<RenderCustomPaint>(slot)
            .Any(static paint => paint.Painter is CheckboxPainter);
}
