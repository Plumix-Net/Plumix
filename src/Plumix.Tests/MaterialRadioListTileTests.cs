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

/// Mirrors `material_ui/test/radio_list_tile_test.dart`.
[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialRadioListTileTests
{
    public MaterialRadioListTileTests()
    {
        FocusManager.Instance.ResetForTests();
        GestureBinding.Instance.ResetForTests();
    }

    [Fact]
    public void Constructor_RequiresSubtitleForThreeLine()
    {
        Assert.Throws<ArgumentException>(() => new RadioListTile<int>(
            value: 1,
            onChanged: _ => { },
            isThreeLine: true));
        Assert.Throws<ArgumentException>(() => RadioListTile<int>.Adaptive(
            value: 1,
            onChanged: _ => { },
            isThreeLine: true));
    }

    /// "RadioListTile should initialize according to groupValue" and "simple control test" — the
    /// tile whose value matches the group value is the checked one, and tapping another selects it.
    [Fact]
    public void ControlTest_TileTapSelectsTheValue()
    {
        var log = new List<string?>();
        using var harness = new ListTileControlHarness(BuildGroup("one", log.Add));

        harness.Pump();
        harness.TapTile(pointer: 721, position: new Point(150, 80));

        Assert.Equal(["two"], log);
    }

    /// "Selected RadioListTile should not trigger onChanged" — a second tap on the checked tile is
    /// a no-op unless `toggleable` is set.
    [Fact]
    public void SelectedTile_DoesNotTriggerOnChanged()
    {
        var log = new List<string?>();
        using var harness = new ListTileControlHarness(BuildGroup("one", log.Add));

        harness.Pump();
        harness.TapTile(pointer: 722, position: new Point(150, 28));

        Assert.Empty(log);
    }

    /// "Selected RadioListTile should trigger onChanged when toggleable" — the checked tile clears
    /// the group value.
    [Fact]
    public void SelectedTile_TogglesToNullWhenToggleable()
    {
        var log = new List<string?>();
        using var harness = new ListTileControlHarness(BuildGroup("one", log.Add, toggleable: true));

        harness.Pump();
        harness.TapTile(pointer: 723, position: new Point(150, 28));

        Assert.Equal([null], log);
    }

    /// The `RadioGroup` ancestor and the (deprecated) `groupValue`/`onChanged` pair both drive the
    /// tile; without either, `enabled: true` is a contract error.
    [Fact]
    public void EnabledWithoutOnChangedOrGroup_Throws()
    {
        using var harness = new ListTileControlHarness(new SizedBox());

        Assert.ThrowsAny<Exception>(() => harness.Update(new RadioListTile<int>(
            value: 1,
            enabled: true,
            title: new Text("Orphan"))));
    }

    [Fact]
    public void WithoutOnChangedOrGroup_TileIsDisabled()
    {
        using var harness = new ListTileControlHarness(new RadioListTile<int>(
            value: 1,
            title: new Text("Orphan")));

        harness.Pump();

        ListTile tile = harness.FindWidget<ListTile>()!;
        Assert.False(tile.Enabled);
        Assert.Null(tile.OnTap);
    }

    /// "Radio changes mouse cursor when hovered" — the cursor reaches the `Radio`, and Flutter never
    /// puts it on the backing `ListTile`.
    [Fact]
    public void MouseCursor_ReachesTheRadioAndNotTheTile()
    {
        using var harness = new ListTileControlHarness(new RadioListTile<int>(
            value: 1,
            groupValue: 1,
            onChanged: _ => { },
            mouseCursor: SystemMouseCursors.Text,
            title: new Text("Cursor")));

        harness.Pump();

        Assert.Same(SystemMouseCursors.Text, harness.FindWidget<Radio<int>>()!.MouseCursor);
        Assert.Null(harness.FindWidget<ListTile>()!.MouseCursor);
    }

    /// "radioSide is passed to the Radio", "radioInnerRadius is passed to the Radio",
    /// "RadioListTile respects radioBackgroundColor ...", plus the remaining radio-only arguments.
    [Fact]
    public void RadioArguments_AreForwardedToTheRadio()
    {
        var side = WidgetStateBorderSide.ResolveWith(_ => new BorderSide(color: Colors.Red, width: 3));
        MaterialStateProperty<Color?> background = MaterialStateProperty<Color?>.All(Colors.Green);
        MaterialStateProperty<double?> innerRadius = MaterialStateProperty<double?>.All(6.0);
        MaterialStateProperty<Color?> fill = MaterialStateProperty<Color?>.All(Colors.Blue);
        MaterialStateProperty<Color?> overlay = MaterialStateProperty<Color?>.All(Colors.Purple);
        using var harness = new ListTileControlHarness(new RadioListTile<int>(
            value: 1,
            groupValue: 1,
            onChanged: _ => { },
            radioSide: side,
            radioBackgroundColor: background,
            radioInnerRadius: innerRadius,
            fillColor: fill,
            overlayColor: overlay,
            hoverColor: Colors.Yellow,
            splashRadius: 27.0,
            toggleable: true,
            title: new Text("Args")));

        harness.Pump();

        Radio<int> radio = harness.FindWidget<Radio<int>>()!;
        Assert.Same(side, radio.Side);
        Assert.Same(background, radio.BackgroundColor);
        Assert.Same(innerRadius, radio.InnerRadius);
        Assert.Same(fill, radio.FillColor);
        Assert.Same(overlay, radio.OverlayColor);
        Assert.Equal(Colors.Yellow, radio.HoverColor);
        Assert.Equal(27.0, radio.SplashRadius);
        Assert.True(radio.Toggleable);
    }

    /// "Radio respects materialTapTargetSize" — the default is `shrinkWrap`.
    [Fact]
    public void MaterialTapTargetSize_DefaultsToShrinkWrap()
    {
        using var harness = new ListTileControlHarness(new RadioListTile<int>(
            value: 1,
            groupValue: 1,
            onChanged: _ => { },
            title: new Text("Tap target")));

        harness.Pump();

        Assert.Equal(MaterialTapTargetSize.ShrinkWrap, harness.FindWidget<Radio<int>>()!.MaterialTapTargetSize);
    }

    /// "RadioListTile.control widget should not request focus on traversal".
    [Fact]
    public void EmbeddedRadio_IsWrappedInExcludeFocus()
    {
        using var harness = new ListTileControlHarness(new RadioListTile<int>(
            value: 1,
            groupValue: 1,
            onChanged: _ => { },
            title: new Text("Focus")));

        harness.Pump();

        Assert.NotNull(harness.FindWidget<ExcludeFocus>());
    }

    /// "RadioListTile uses ListTileTheme controlAffinity" — unlike the checkbox and switch tiles,
    /// `platform` puts the radio on the *leading* edge.
    [Fact]
    public void ControlAffinity_PlatformAndLeadingBothPutTheRadioFirst()
    {
        using var harness = new ListTileControlHarness(new RadioListTile<int>(
            value: 1,
            groupValue: 1,
            onChanged: _ => { },
            controlAffinity: ListTileControlAffinity.Platform,
            title: new Text("Affinity"),
            secondary: new Icon(Icons.InfoOutline)));

        harness.Pump();
        Assert.True(HasRadio(harness.Tile.Leading));
        Assert.False(HasRadio(harness.Tile.Trailing));

        harness.Update(new RadioListTile<int>(
            value: 1,
            groupValue: 1,
            onChanged: _ => { },
            controlAffinity: ListTileControlAffinity.Trailing,
            title: new Text("Affinity"),
            secondary: new Icon(Icons.InfoOutline)));
        Assert.False(HasRadio(harness.Tile.Leading));
        Assert.True(HasRadio(harness.Tile.Trailing));
    }

    [Fact]
    public void ControlAffinity_ReadsTheListTileTheme()
    {
        var theme = ThemeData.Light with
        {
            ListTileTheme = new ListTileThemeData(ControlAffinity: ListTileControlAffinity.Trailing)
        };
        using var harness = new ListTileControlHarness(
            new RadioListTile<int>(
                value: 1,
                groupValue: 1,
                onChanged: _ => { },
                title: new Text("Themed"),
                secondary: new Icon(Icons.InfoOutline)),
            theme);

        harness.Pump();

        Assert.False(HasRadio(harness.Tile.Leading));
        Assert.True(HasRadio(harness.Tile.Trailing));
    }

    /// "RadioListTile renders with default scale".
    [Fact]
    public void ScaleFactor_DefaultInsertsNoTransform()
    {
        using var harness = new ListTileControlHarness(new RadioListTile<int>(
            value: 1,
            groupValue: 1,
            onChanged: _ => { },
            title: new Text("Unscaled")));

        harness.Pump();

        Assert.Null(ListTileControlHarness.Find<RenderTransform>(harness.RenderView));
    }

    /// "RadioListTile respects radioScaleFactor" — `Transform.scale`, i.e. a centre-aligned uniform
    /// scale.
    [Fact]
    public void ScaleFactor_UsesCenteredUniformScale()
    {
        const double scaleFactor = 1.5;
        using var harness = new ListTileControlHarness(new RadioListTile<int>(
            value: 1,
            groupValue: 1,
            onChanged: _ => { },
            radioScaleFactor: scaleFactor,
            title: new Text("Scaled")));

        harness.Pump();

        RenderTransform transform = Assert.IsType<RenderTransform>(
            ListTileControlHarness.Find<RenderTransform>(harness.RenderView));
        Assert.Equal(Matrix4.Diagonal3Values(scaleFactor, scaleFactor, 1.0), transform.Transform);
        Assert.Equal(Alignment.Center, transform.Alignment);
    }

    /// "RadioListTile selected item text Color" — `activeColor`, then the radio theme's selected
    /// fill, then `colorScheme.secondary`.
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
            RadioTheme = new RadioThemeData(
                FillColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Selected) ? themed : null))
        };

        Assert.Equal(active, TitleColor(active, themedFill));
        Assert.Equal(themed, TitleColor(null, themedFill));
        Assert.Equal(secondary, TitleColor(null, bare));

        static Color? TitleColor(Color? activeColor, ThemeData theme)
        {
            using var harness = new ListTileControlHarness(
                new RadioListTile<int>(
                    value: 1,
                    groupValue: 1,
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

    /// "RadioListTile contentPadding test" plus the tile-geometry arguments the list tile owns.
    [Fact]
    public void TileArguments_AreForwardedToTheListTile()
    {
        var statesController = new MaterialStatesController();
        var focusNode = new FocusNode();
        var shape = new RoundedRectangleBorder(borderRadius: BorderRadius.Circular(12));
        using var harness = new ListTileControlHarness(new RadioListTile<int>(
            value: 1,
            groupValue: 1,
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

    /// "RadioListTile semantics" — the tile is one merge root carrying the radio's checked state and
    /// the tile's tap action (see `docs/ai/DIVERGENCES.md` on flattened `SemanticsData`).
    [Fact]
    public void Semantics_MergesRadioStateAndTileTap()
    {
        using var harness = new ListTileControlHarness(new RadioListTile<int>(
            value: 1,
            groupValue: 1,
            onChanged: _ => { },
            title: new Text("Lafayette")));

        SemanticsNode root = harness.PumpSemantics();

        SemanticsNode mergeRoot = Assert.IsType<SemanticsNode>(ListTileControlHarness.FindSemantics(
            root,
            static candidate => candidate.MergeAllDescendantsIntoThisNode));
        Assert.True(mergeRoot.Actions.HasFlag(SemanticsActions.Tap));

        SemanticsNode checkedNode = Assert.IsType<SemanticsNode>(ListTileControlHarness.FindSemantics(
            mergeRoot,
            static candidate => candidate.Flags.HasFlag(SemanticsFlags.IsChecked)));
        Assert.True(checkedNode.Flags.HasFlag(SemanticsFlags.HasCheckedState));
    }

    /// "RadioListTile.adaptive shows the correct radio platform widget".
    [Fact]
    public void Adaptive_UsesTheCupertinoRadioOnApplePlatforms()
    {
        using var harness = new ListTileControlHarness(
            RadioListTile<int>.Adaptive(
                value: 1,
                groupValue: 1,
                onChanged: _ => { },
                title: new Text("Adaptive")),
            ThemeData.Light with { Platform = TargetPlatform.IOS });

        harness.Pump();

        Assert.NotNull(harness.FindWidget<Plumix.Cupertino.CupertinoRadio<int>>());

        harness.Update(
            RadioListTile<int>.Adaptive(
                value: 1,
                groupValue: 1,
                onChanged: _ => { },
                title: new Text("Adaptive")),
            ThemeData.Light with { Platform = TargetPlatform.Android });

        Assert.Null(harness.FindWidget<Plumix.Cupertino.CupertinoRadio<int>>());
    }

    /// "RadioListTile respects visualDensity" and "minTileHeight".
    [Fact]
    public void VisualDensityAndMinTileHeight_DriveTheTileSurface()
    {
        using var harness = new ListTileControlHarness(new RadioListTile<int>(
            value: 1,
            groupValue: 1,
            onChanged: _ => { },
            title: new Text("Tall"),
            minTileHeight: 72));

        harness.Pump();
        Assert.Equal(72, harness.Tile.Size.Height, 3);

        harness.Update(new RadioListTile<int>(
            value: 1,
            groupValue: 1,
            onChanged: _ => { },
            title: new Text("Compact"),
            visualDensity: VisualDensity.Compact));
        Assert.Equal(48, harness.Tile.Size.Height, 3);
    }

    /// "RadioListTile forwards statesController to ListTile".
    [Fact]
    public void StatesController_IsDrivenByTheTile()
    {
        var statesController = new MaterialStatesController();
        using var harness = new ListTileControlHarness(new RadioListTile<int>(
            value: 1,
            groupValue: 1,
            onChanged: _ => { },
            statesController: statesController,
            title: new Text("Stateful")));

        harness.Pump();
        Assert.Same(statesController, harness.FindWidget<ListTile>()!.StatesController);

        statesController.Update(MaterialState.Pressed, true);
        harness.Pump();
        Assert.True(statesController.Value.HasFlag(MaterialState.Pressed));
    }

    /// "RadioListTile does not crash at zero area".
    [Fact]
    public void ZeroArea_LaysOutWithoutCrashing()
    {
        using var harness = new ListTileControlHarness(
            new RadioListTile<int>(
                value: 1,
                groupValue: 1,
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
            new SizedBox(height: 520, child: new RadioListTileDemoPage()),
            width: 760.0,
            height: 520.0,
            viewWidth: 760.0,
            viewHeight: 520.0);

        harness.Pump();

        Assert.DoesNotContain(
            ListTileControlHarness.FindAll<RenderFlex>(harness.RenderView),
            static flex => flex._hasOverflow);
    }

    private static Widget BuildGroup(string? groupValue, Action<string?> onChanged, bool toggleable = false)
    {
        return new RadioGroup<string>(
            groupValue: groupValue,
            onChanged: onChanged,
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children:
                [
                    new RadioListTile<string>(value: "one", toggleable: toggleable, title: new Text("One")),
                    new RadioListTile<string>(value: "two", toggleable: toggleable, title: new Text("Two")),
                ]));
    }

    private static bool HasRadio(RenderObject? slot) =>
        ListTileControlHarness.FindAll<RenderCustomPaint>(slot)
            .Any(static paint => paint.Painter is RadioPainter);
}
