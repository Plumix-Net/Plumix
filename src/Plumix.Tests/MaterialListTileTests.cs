using System;
using System.Collections.Generic;
using System.Linq;
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

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialListTileTests
{
    public MaterialListTileTests()
    {
        FocusManager.Instance.ResetForTests();
        GestureBinding.Instance.ResetForTests();
    }

    [Fact]
    public void ListTile_Throws_WhenIsThreeLineAndSubtitleIsNull()
    {
        Assert.Throws<ArgumentException>(() => new ListTile(
            title: new Text("Tile"),
            isThreeLine: true));
    }

    [Fact]
    public void ListTile_DefaultM3_OneLine_UsesMinHeight56()
    {
        using var harness = new WidgetRenderHarness(
            BuildThemedTile(new ListTile(
                title: new Text("One line"),
                onTap: () => { })));

        harness.Pump(new Size(400, 200));

        var material = FindDescendant<RenderDecoratedBox>(harness.RenderView);
        Assert.NotNull(material);
        Assert.Equal(300, material!.Size.Width, 3);
        Assert.Equal(56, material.Size.Height, 3);
    }

    [Fact]
    public void ListTile_DenseOneLine_UsesMinHeight48()
    {
        using var harness = new WidgetRenderHarness(
            BuildThemedTile(new ListTile(
                title: new Text("Dense"),
                dense: true,
                onTap: () => { })));

        harness.Pump(new Size(400, 200));

        var material = FindDescendant<RenderDecoratedBox>(harness.RenderView);
        Assert.NotNull(material);
        Assert.Equal(48, material!.Size.Height, 3);
    }

    [Fact]
    public void ListTile_DefaultM3_TwoLine_UsesMinHeight72()
    {
        using var harness = new WidgetRenderHarness(
            BuildThemedTile(new ListTile(
                title: new Text("Title"),
                subtitle: new Text("Subtitle"),
                onTap: () => { })));

        harness.Pump(new Size(400, 220));

        var material = FindDescendant<RenderDecoratedBox>(harness.RenderView);
        Assert.NotNull(material);
        Assert.Equal(72, material!.Size.Height, 3);
    }

    [Fact]
    public void ListTile_DefaultM3_ThreeLine_UsesMinHeight88()
    {
        using var harness = new WidgetRenderHarness(
            BuildThemedTile(new ListTile(
                title: new Text("Title"),
                subtitle: new Text("Subtitle"),
                isThreeLine: true,
                onTap: () => { })));

        harness.Pump(new Size(400, 240));

        var material = FindDescendant<RenderDecoratedBox>(harness.RenderView);
        Assert.NotNull(material);
        Assert.Equal(88, material!.Size.Height, 3);
    }

    [Fact]
    public void ListTile_Selected_UsesSelectedColorForTitleAndLeadingIcon()
    {
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme with { Primary = Colors.Coral }
        };

        using var harness = new WidgetRenderHarness(
            BuildThemedTile(
                new ListTile(
                    title: new Text("Selected"),
                    leading: new Icon(Icons.StarOutline),
                    selected: true,
                    onTap: () => { }),
                theme));

        harness.Pump(new Size(400, 220));
        var renderRoot = harness.RenderView.Child;

        var titleParagraph = FindParagraphByText(renderRoot, "Selected");
        Assert.NotNull(titleParagraph);
        Assert.Equal(Colors.Coral, Assert.IsType<SolidColorBrush>(titleParagraph!.Foreground).Color);

        var iconParagraph = FindParagraphByText(renderRoot, char.ConvertFromUtf32(Icons.StarOutline.CodePoint));
        Assert.NotNull(iconParagraph);
        Assert.Equal(Colors.Coral, Assert.IsType<SolidColorBrush>(iconParagraph!.Foreground).Color);
    }

    [Fact]
    public void ListTile_SelectedTileColor_OverridesDefaultBackground()
    {
        var selectedTileColor = Color.Parse("#FFE7D6FF");
        using var harness = new WidgetRenderHarness(
            BuildThemedTile(new ListTile(
                title: new Text("Selected tile"),
                selected: true,
                selectedTileColor: selectedTileColor,
                onTap: () => { })));

        harness.Pump(new Size(400, 200));

        var ink = FindDescendant<RenderInkDecoration>(harness.RenderView);
        var decoration = Assert.IsType<ShapeDecoration>(ink!.Decoration);
        Assert.Equal(selectedTileColor, decoration.Color);
    }

    [Fact]
    public void ListTile_ThemeColors_ApplyWhenWidgetOverridesMissing()
    {
        var themedText = Color.Parse("#FF0F5A4A");
        var themedIcon = Color.Parse("#FF904E1A");
        var themedTile = Color.Parse("#FFF3F8E8");
        var theme = ThemeData.Light with
        {
            ListTileTheme = new ListTileThemeData(
                TextColor: themedText,
                IconColor: themedIcon,
                TileColor: themedTile)
        };

        using var harness = new WidgetRenderHarness(
            BuildThemedTile(
                new ListTile(
                    title: new Text("Themed tile"),
                    leading: new Icon(Icons.InfoOutline),
                    onTap: () => { }),
                theme));

        harness.Pump(new Size(420, 220));
        var renderRoot = harness.RenderView.Child;

        var titleParagraph = FindParagraphByText(renderRoot, "Themed tile");
        Assert.NotNull(titleParagraph);
        Assert.Equal(themedText, Assert.IsType<SolidColorBrush>(titleParagraph!.Foreground).Color);

        var iconParagraph = FindParagraphByText(renderRoot, char.ConvertFromUtf32(Icons.InfoOutline.CodePoint));
        Assert.NotNull(iconParagraph);
        Assert.Equal(themedIcon, Assert.IsType<SolidColorBrush>(iconParagraph!.Foreground).Color);

        var ink = FindDescendant<RenderInkDecoration>(renderRoot);
        var decoration = Assert.IsType<ShapeDecoration>(ink!.Decoration);
        Assert.Equal(themedTile, decoration.Color);
    }

    [Fact]
    public void ListTile_LocalThemeMergeAndWidget_UseSourcePrecedence()
    {
        var globalColor = Color.Parse("#FF8E2430");
        var localColor = Color.Parse("#FF176B52");
        var mergedColor = Color.Parse("#FF4051B5");
        var widgetColor = Color.Parse("#FF9A5B00");
        ThemeData theme = ThemeData.Light with
        {
            ListTileTheme = new ListTileThemeData(TextColor: globalColor)
        };
        using var harness = new WidgetRenderHarness(BuildThemedTile(
            new Column(children:
            [
                new ListTile(title: new Text("Global")),
                new ListTileTheme(
                    child: new ListTile(title: new Text("Local")),
                    data: new ListTileThemeData(TextColor: localColor)),
                ListTileTheme.Merge(
                    child: new ListTile(title: new Text("Merged")),
                    textColor: mergedColor),
                new ListTile(title: new Text("Widget"), textColor: widgetColor),
            ]),
            theme));

        harness.Pump(new Size(400, 280));

        AssertParagraphColor(harness.RenderView, "Global", globalColor);
        AssertParagraphColor(harness.RenderView, "Local", localColor);
        AssertParagraphColor(harness.RenderView, "Merged", mergedColor);
        AssertParagraphColor(harness.RenderView, "Widget", widgetColor);
    }

    [Theory]
    [InlineData(true, 16.0, 24.0, 56.0)]
    [InlineData(false, 16.0, 16.0, 72.0)]
    public void ListTile_MaterialVersionDefaults_UseExactDirectionalGeometry(
        bool useMaterial3,
        double startPadding,
        double endPadding,
        double titleX)
    {
        ThemeData theme = ThemeData.Light with { UseMaterial3 = useMaterial3 };
        using var harness = new WidgetRenderHarness(
            BuildThemedTile(
                new ListTile(
                    leading: new SizedBox(width: 24, height: 24),
                    title: new Text("Geometry"),
                    trailing: new SizedBox(width: 24, height: 24)),
                theme));

        harness.Pump(new Size(400, 200));

        var tile = Assert.IsType<RenderListTile>(FindDescendant<RenderListTile>(harness.RenderView));
        Assert.Equal(300.0 - startPadding - endPadding, tile.Size.Width, 3);
        Assert.Equal(startPadding, GlobalOffsetOf(tile.Leading!).X, 3);
        Assert.Equal(titleX, GlobalOffsetOf(tile.Title).X, 3);
        Assert.Equal(300.0 - endPadding - 24.0, GlobalOffsetOf(tile.Trailing!).X, 3);
    }

    [Fact]
    public void ListTile_M3DirectionalPaddingAndSlots_MirrorInRtl()
    {
        using var harness = new WidgetRenderHarness(
            BuildThemedTile(new Directionality(
                TextDirection.Rtl,
                new ListTile(
                    leading: new SizedBox(width: 24, height: 24),
                    title: new Text("RTL"),
                    trailing: new SizedBox(width: 24, height: 24)))));

        harness.Pump(new Size(400, 200));

        var tile = Assert.IsType<RenderListTile>(FindDescendant<RenderListTile>(harness.RenderView));
        Assert.Equal(260, GlobalOffsetOf(tile.Leading!).X, 3);
        Assert.Equal(64, GlobalOffsetOf(tile.Title).X, 3);
        Assert.Equal(24, GlobalOffsetOf(tile.Trailing!).X, 3);
    }

    [Fact]
    public void ListTile_StateColors_ResolveDisabledAndSelectedTogether()
    {
        var disabledSelected = Color.Parse("#FF8B1E3F");
        MaterialStateProperty<Color?> stateColor = MaterialStateProperty<Color?>.ResolveWith(states =>
            states.HasFlag(MaterialState.Disabled) && states.HasFlag(MaterialState.Selected)
                ? disabledSelected
                : Colors.Teal);
        using var harness = new WidgetRenderHarness(
            BuildThemedTile(new ListTile(
                leading: new Icon(Icons.StarOutline),
                title: new Text("State colors"),
                selected: true,
                enabled: false,
                iconColor: stateColor,
                textColor: stateColor)));

        harness.Pump(new Size(400, 200));

        var title = Assert.IsType<RenderParagraph>(FindParagraphByText(harness.RenderView, "State colors"));
        Assert.Equal(disabledSelected, Assert.IsType<SolidColorBrush>(title.Foreground).Color);
        string glyph = char.ConvertFromUtf32(Icons.StarOutline.CodePoint);
        var icon = Assert.IsType<RenderParagraph>(FindParagraphByText(harness.RenderView, glyph));
        Assert.Equal(disabledSelected, Assert.IsType<SolidColorBrush>(icon.Foreground).Color);
    }

    [Fact]
    public void ListTileThemeData_CopyWithAndLerp_PreserveAndInterpolateStateValues()
    {
        var start = new ListTileThemeData(
            Dense: true,
            IconColor: MaterialStateProperty<Color?>.All(Colors.Black),
            TextColor: MaterialStateProperty<Color?>.All(Colors.Red),
            ContentPadding: EdgeInsetsGeometry.DirectionalOnly(start: 8, end: 12),
            MinTileHeight: 48);
        ListTileThemeData copy = start.CopyWith(minTileHeight: 64);
        var end = new ListTileThemeData(
            IconColor: MaterialStateProperty<Color?>.All(Colors.White),
            TextColor: MaterialStateProperty<Color?>.All(Colors.Blue),
            ContentPadding: EdgeInsetsGeometry.DirectionalOnly(start: 16, end: 24),
            MinTileHeight: 72);

        Assert.True(copy.Dense);
        Assert.Equal(64, copy.MinTileHeight);
        Assert.Equal(start.ContentPadding, copy.ContentPadding);

        ListTileThemeData lerped = Assert.IsType<ListTileThemeData>(ListTileThemeData.Lerp(start, end, 0.5));
        Assert.Equal(
            MaterialThemeLerp.Color(Colors.Black, Colors.White, 0.5),
            lerped.IconColor!.Resolve(MaterialState.Selected));
        Assert.Equal(
            MaterialThemeLerp.Color(Colors.Red, Colors.Blue, 0.5),
            lerped.TextColor!.Resolve(MaterialState.Disabled));
        Assert.Equal(12, lerped.ContentPadding!.Value.Start, 3);
        Assert.Equal(18, lerped.ContentPadding.Value.End, 3);
        Assert.Equal(60, lerped.MinTileHeight);
        Assert.Throws<ArgumentException>(() => new ListTileTheme(
            child: new SizedBox(),
            data: start,
            selectedColor: Colors.Coral));
    }

    [Fact]
    public void ListTile_IntrinsicAndDryLayout_UseSlottedChildren()
    {
        using var harness = new WidgetRenderHarness(
            BuildThemedTile(new ListTile(
                leading: new SizedBox(width: 24, height: 24),
                title: new Text("Intrinsic tile"),
                trailing: new SizedBox(width: 24, height: 24))));

        harness.Pump(new Size(400, 200));

        var tile = Assert.IsType<RenderListTile>(FindDescendant<RenderListTile>(harness.RenderView));
        Size drySize = tile.GetDryLayout(new BoxConstraints(MaxWidth: tile.Size.Width));
        Assert.Equal(tile.Size, drySize);
        Assert.Equal(56, tile.GetMinIntrinsicHeight(tile.Size.Width), 3);
        Assert.True(tile.GetMinIntrinsicWidth(56) > 64);
        Assert.NotNull(tile.GetDryBaseline(new BoxConstraints(MaxWidth: tile.Size.Width), TextBaseline.Alphabetic));
    }

    [Fact]
    public void ListTile_SlottedElement_ReconcilesAddedAndRemovedSlots()
    {
        using var showLeading = new ValueNotifier<bool>(false);
        using var harness = new WidgetRenderHarness(
            BuildThemedTile(new ValueListenableBuilder<bool>(
                showLeading,
                (_, visible, _) => new ListTile(
                    leading: visible ? new Icon(Icons.StarOutline) : null,
                    title: new Text(visible ? "Leading visible" : "Trailing visible"),
                    trailing: visible ? null : new Icon(Icons.InfoOutline)))));

        harness.Pump(new Size(400, 200));
        var tile = Assert.IsType<RenderListTile>(FindDescendant<RenderListTile>(harness.RenderView));
        Assert.Null(tile.Leading);
        Assert.NotNull(tile.Trailing);

        showLeading.Value = true;
        harness.Pump(new Size(400, 200));

        var updatedTile = Assert.IsType<RenderListTile>(FindDescendant<RenderListTile>(harness.RenderView));
        Assert.Same(tile, updatedTile);
        Assert.NotNull(updatedTile.Leading);
        Assert.Null(updatedTile.Trailing);
        Assert.NotNull(FindParagraphByText(harness.RenderView, "Leading visible"));
    }

    [Fact]
    public void ListTile_DivideTiles_AddsOnlyForegroundBottomBorders()
    {
        var dividerColor = Color.Parse("#FF123456");
        Widget last = new Text("last");
        IReadOnlyList<Widget> divided = ListTile.DivideTiles(
            [new Text("first"), new Text("second"), last],
            color: dividerColor);

        Assert.Equal(3, divided.Count);
        foreach (Widget widget in divided.Take(2))
        {
            var decorated = Assert.IsType<DecoratedBox>(widget);
            Assert.Equal(DecorationPosition.Foreground, decorated.Position);
            var decoration = Assert.IsType<BoxDecoration>(decorated.Decoration);
            Assert.Equal(dividerColor, ((Plumix.Rendering.Border)decoration.Border!).Bottom.Color);
        }

        Assert.Same(last, divided[2]);
        Assert.Throws<ArgumentException>(() => ListTile.DivideTiles([new Text("tile")]));
    }

    [Fact]
    public void ListTile_OnTap_InvokesCallback()
    {
        int tapCount = 0;
        using var harness = new WidgetRenderHarness(
            BuildThemedTile(new ListTile(
                title: new Text("Tap target"),
                onTap: () => tapCount += 1)));

        harness.Pump(new Size(400, 200));

        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        try
        {
            binding.HandlePointerEvent(
                harness.RenderView,
                new PointerDownEvent(
                    pointer: 320,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(150, 28),
                    buttons: PointerButtons.Primary,
                    timestampUtc: DateTime.UtcNow));
            binding.HandlePointerEvent(
                harness.RenderView,
                new PointerUpEvent(
                    pointer: 320,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(150, 28),
                    buttons: PointerButtons.None,
                    timestampUtc: DateTime.UtcNow));
        }
        finally
        {
            binding.ResetForTests();
        }

        Assert.Equal(1, tapCount);
    }

    [Fact]
    public void ListTile_Disabled_SemanticsOmitEnabledAndTapAction()
    {
        using var harness = new WidgetRenderHarness(
            BuildThemedTile(new ListTile(
                title: new Text("Disabled"),
                enabled: false,
                onTap: () => { })));

        var semanticsRoot = harness.PumpAndGetSemantics(new Size(400, 200));
        Assert.NotNull(semanticsRoot);

        var buttonNode = FindFirstSemanticsNode(
            semanticsRoot!,
            static node => node.Flags.HasFlag(SemanticsFlags.IsButton));
        Assert.NotNull(buttonNode);
        Assert.False(buttonNode!.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.False(buttonNode.Actions.HasFlag(SemanticsActions.Tap));
    }

    [Fact]
    public void ListTile_Selected_SemanticsIncludeSelectedFlag()
    {
        using var harness = new WidgetRenderHarness(
            BuildThemedTile(new ListTile(
                title: new Text("Selected semantics"),
                selected: true,
                onTap: () => { })));

        var semanticsRoot = harness.PumpAndGetSemantics(new Size(400, 200));
        Assert.NotNull(semanticsRoot);

        var selectedNode = FindFirstSemanticsNode(
            semanticsRoot!,
            static node => node.Flags.HasFlag(SemanticsFlags.IsButton) && node.Flags.HasFlag(SemanticsFlags.IsSelected));
        Assert.NotNull(selectedNode);
        Assert.True(selectedNode!.Flags.HasFlag(SemanticsFlags.IsEnabled));
    }

    [Fact]
    public void ListTile_DemoLikeState_DoesNotProduceFlexOverflow()
    {
        var theme = ThemeData.Light with
        {
            ListTileTheme = new ListTileThemeData(
                TextColor: Color.Parse("#FF27526B"),
                IconColor: Color.Parse("#FF7A4021"),
                TileColor: Color.Parse("#FFF5F9EE"),
                SelectedTileColor: Color.Parse("#FFE4EEFF"))
        };

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new MediaQuery(
                    data: new MediaQueryData(Size: new Size(720, 420)),
                    child: new Plumix.Material.Material(
                        child: new SizedBox(
                            width: 720,
                            child: new Column(
                                crossAxisAlignment: CrossAxisAlignment.Stretch,
                                children:
                                [
                                    new ListTile(
                                        title: new Text("One-line tile"),
                                        leading: new Icon(Icons.Menu),
                                        trailing: new Icon(Icons.InfoOutline),
                                        enabled: false),
                                    new ListTile(
                                        title: new Text("Two-line tile"),
                                        subtitle: new Text("Subtitle text demonstrates two-line default height."),
                                        leading: new Icon(Icons.Add),
                                        trailing: new Text("meta", fontSize: 12),
                                        enabled: false),
                                    new ListTile(
                                        title: new Text("Three-line probe"),
                                        subtitle: new Text(
                                            "When 3-line is enabled this tile uses the taller baseline height "
                                            + "for parity checks."),
                                        leading: new Icon(Icons.StarOutline),
                                        trailing: new Icon(Icons.Close),
                                        enabled: false),
                                ]))))));

        harness.Pump(new Size(760, 420));

        var flexes = FindDescendants<RenderFlex>(harness.RenderView).ToList();
        Assert.NotEmpty(flexes);
        int overflowCount = flexes.Count(static flex => flex._hasOverflow);
        Assert.True(
            overflowCount == 0,
            $"Expected no flex overflow in demo-like ListTile layout, but found {overflowCount} overflowing flex nodes.");
    }

    [Fact]
    public void CheckboxListTile_Constructor_ValidatesFlutterGuards()
    {
        Assert.Throws<ArgumentException>(() => new CheckboxListTile(value: null, onChanged: _ => { }));
        Assert.Throws<ArgumentException>(() => new CheckboxListTile(
            value: false,
            onChanged: _ => { },
            isThreeLine: true));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CheckboxListTile(
            value: false,
            onChanged: _ => { },
            checkboxScaleFactor: 0));
    }

    [Fact]
    public void SwitchListTile_Constructor_RequiresSubtitleForThreeLineLayout()
    {
        Assert.Throws<ArgumentException>(() => new SwitchListTile(
            value: false,
            onChanged: _ => { },
            isThreeLine: true));
    }

    [Fact]
    public void CheckboxListTile_WholeTileTap_UsesFlutterTristateCycle()
    {
        bool? nextValue = false;
        using var harness = new WidgetRenderHarness(
            BuildThemedTile(new CheckboxListTile(
                value: true,
                tristate: true,
                title: new Text("Tristate tile"),
                onChanged: value => nextValue = value)));

        harness.Pump(new Size(400, 200));
        Tap(harness.RenderView, new Point(120, 28), pointer: 610);

        Assert.Null(nextValue);
    }

    [Fact]
    public void SwitchListTile_WholeTileTap_TogglesControlledValue()
    {
        bool? nextValue = null;
        using var harness = new WidgetRenderHarness(
            BuildThemedTile(new SwitchListTile(
                value: false,
                title: new Text("Switch tile"),
                onChanged: value => nextValue = value)));

        harness.Pump(new Size(400, 200));
        Tap(harness.RenderView, new Point(120, 28), pointer: 611);

        Assert.True(nextValue);
    }

    [Fact]
    public void CheckboxListTile_SelectedTitle_UsesCheckboxThemeThenSecondaryFallback()
    {
        var themedSelected = Color.Parse("#FF006C4C");
        var theme = ThemeData.Light with
        {
            SecondaryColor = Color.Parse("#FF735C00"),
            CheckboxTheme = new CheckboxThemeData(
                FillColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Selected) ? themedSelected : null))
        };

        using var harness = new WidgetRenderHarness(
            BuildThemedTile(
                new CheckboxListTile(
                    value: true,
                    selected: true,
                    title: new Text("Selected checkbox tile"),
                    onChanged: _ => { }),
                theme));

        harness.Pump(new Size(400, 200));
        var title = FindParagraphByText(harness.RenderView, "Selected checkbox tile");
        Assert.NotNull(title);
        Assert.Equal(themedSelected, Assert.IsType<SolidColorBrush>(title!.Foreground).Color);
    }

    [Fact]
    public void SwitchListTile_SelectedTitle_UsesActiveThumbColorPrecedence()
    {
        var activeThumb = Color.Parse("#FF8C1D40");
        using var harness = new WidgetRenderHarness(
            BuildThemedTile(new SwitchListTile(
                value: true,
                selected: true,
                activeColor: Colors.Coral,
                activeThumbColor: activeThumb,
                title: new Text("Selected switch tile"),
                onChanged: _ => { })));

        harness.Pump(new Size(400, 200));
        var title = FindParagraphByText(harness.RenderView, "Selected switch tile");
        Assert.NotNull(title);
        Assert.Equal(activeThumb, Assert.IsType<SolidColorBrush>(title!.Foreground).Color);
    }

    [Fact]
    public void ListTileControls_RespectThemeAffinityAndLayoutOverrides()
    {
        var theme = ThemeData.Light with
        {
            ListTileTheme = new ListTileThemeData(ControlAffinity: ListTileControlAffinity.Leading)
        };
        using var harness = new WidgetRenderHarness(
            BuildThemedTile(
                new CheckboxListTile(
                    value: false,
                    title: new Text("Leading control"),
                    secondary: new Icon(Icons.InfoOutline),
                    minTileHeight: 72,
                    horizontalTitleGap: 24,
                    onChanged: _ => { }),
                theme));

        harness.Pump(new Size(400, 200));

        var materials = FindDescendants<RenderDecoratedBox>(harness.RenderView).ToList();
        var material = materials.FirstOrDefault(box => Math.Abs(box.Size.Height - 72) < 0.001);
        Assert.True(
            material is not null,
            $"Expected a 72dp tile material. Actual boxes: {string.Join(", ", materials.Select(box => $"{box.Size.Width}x{box.Size.Height}"))}");
        string secondaryGlyph = char.ConvertFromUtf32(Icons.InfoOutline.CodePoint);
        Assert.NotNull(FindParagraphByText(harness.RenderView, secondaryGlyph));

        var renderTile = FindDescendant<RenderListTile>(harness.RenderView);
        Assert.NotNull(renderTile);
        Assert.Contains(
            FindDescendants<RenderCustomPaint>(renderTile!.Leading),
            static paint => paint.Painter is CheckboxPainter);
        Assert.Contains(
            FindDescendants<RenderParagraph>(renderTile.Trailing),
            paragraph => paragraph.PlainText == secondaryGlyph);
    }

    [Fact]
    public void CheckboxAndSwitchListTile_VisualDensityAdjustsSharedTileGeometry()
    {
        using var checkboxHarness = new WidgetRenderHarness(
            BuildThemedTile(new CheckboxListTile(
                value: false,
                onChanged: _ => { },
                visualDensity: VisualDensity.Compact,
                title: new Text("Compact checkbox"))));
        using var switchHarness = new WidgetRenderHarness(
            BuildThemedTile(new SwitchListTile(
                value: false,
                onChanged: _ => { },
                visualDensity: VisualDensity.Compact,
                title: new Text("Compact switch"))));

        checkboxHarness.Pump(new Size(400, 200));
        switchHarness.Pump(new Size(400, 200));

        Assert.NotNull(FindTileSurface(checkboxHarness.RenderView, expectedHeight: 48));
        Assert.NotNull(FindTileSurface(switchHarness.RenderView, expectedHeight: 48));
    }

    [Fact]
    public void CheckboxListTile_TitleAlignment_OverridesThemeAndMovesSecondarySlot()
    {
        var theme = ThemeData.Light with
        {
            ListTileTheme = new ListTileThemeData(TitleAlignment: ListTileTitleAlignment.Bottom)
        };
        using var topHarness = new WidgetRenderHarness(
            BuildThemedTile(
                new CheckboxListTile(
                    value: false,
                    onChanged: _ => { },
                    title: new Text("Top title"),
                    subtitle: new Text("Subtitle"),
                    isThreeLine: true,
                    secondary: new Icon(Icons.InfoOutline),
                    titleAlignment: ListTileTitleAlignment.Top),
                theme));
        using var bottomHarness = new WidgetRenderHarness(
            BuildThemedTile(
                new CheckboxListTile(
                    value: false,
                    onChanged: _ => { },
                    title: new Text("Bottom title"),
                    subtitle: new Text("Subtitle"),
                    isThreeLine: true,
                    secondary: new Icon(Icons.InfoOutline)),
                theme));

        topHarness.Pump(new Size(400, 200));
        bottomHarness.Pump(new Size(400, 200));

        string glyph = char.ConvertFromUtf32(Icons.InfoOutline.CodePoint);
        var topIcon = Assert.IsType<RenderParagraph>(FindParagraphByText(topHarness.RenderView, glyph));
        var bottomIcon = Assert.IsType<RenderParagraph>(FindParagraphByText(bottomHarness.RenderView, glyph));
        Assert.True(GlobalOffsetOf(topIcon).Y < GlobalOffsetOf(bottomIcon).Y);
    }

    [Fact]
    public void CheckboxAndSwitchListTile_ExternalStatesControllerDrivesTileOverlay()
    {
        var checkboxStates = new MaterialStatesController();
        var switchStates = new MaterialStatesController();
        using var checkboxHarness = new WidgetRenderHarness(
            BuildThemedTile(new CheckboxListTile(
                value: false,
                onChanged: _ => { },
                selected: true,
                statesController: checkboxStates,
                title: new Text("Stateful checkbox"))));
        using var switchHarness = new WidgetRenderHarness(
            BuildThemedTile(new SwitchListTile(
                value: false,
                onChanged: _ => { },
                statesController: switchStates,
                title: new Text("Stateful switch"))));

        checkboxHarness.Pump(new Size(400, 200));
        switchHarness.Pump(new Size(400, 200));
        Assert.False(checkboxStates.Value.HasFlag(MaterialState.Selected));

        checkboxStates.Update(MaterialState.Pressed, true);
        switchStates.Update(MaterialState.Pressed, true);
        checkboxHarness.Pump(new Size(400, 200));
        switchHarness.Pump(new Size(400, 200));

        Assert.True(checkboxStates.Value.HasFlag(MaterialState.Pressed));
        Assert.True(switchStates.Value.HasFlag(MaterialState.Pressed));
    }

    [Fact]
    public void CheckboxListTile_ScaleFactor_AppliesCenteredPaintTransform()
    {
        const double scaleFactor = 1.5;
        using var harness = new WidgetRenderHarness(
            BuildThemedTile(new CheckboxListTile(
                value: false,
                checkboxScaleFactor: scaleFactor,
                title: new Text("Scaled checkbox"),
                onChanged: _ => { })));

        harness.Pump(new Size(400, 200));

        var transform = FindDescendant<RenderTransform>(harness.RenderView);
        Assert.NotNull(transform);
        double center = Checkbox.Width / 2.0;
        Matrix4 expected = Matrix4.TranslationValues(center, center, 0.0);
        expected.ScaleByDouble(scaleFactor, scaleFactor, 1.0, 1);
        expected.TranslateByDouble(-center, -center, 0, 1);
        Assert.Equal(expected, transform!.Transform);
    }

    [Fact]
    public void CheckboxListTile_MergeSemantics_ExposesCheckedEnabledAndTap()
    {
        using var harness = new WidgetRenderHarness(
            BuildThemedTile(new CheckboxListTile(
                value: true,
                title: new Text("Terms"),
                checkboxSemanticLabel: "Accept terms",
                onChanged: _ => { })));

        var semanticsRoot = harness.PumpAndGetSemantics(new Size(400, 200));
        Assert.NotNull(semanticsRoot);
        var checkedNode = FindFirstSemanticsNode(
            semanticsRoot!,
            static node => node.Flags.HasFlag(SemanticsFlags.IsChecked));
        Assert.NotNull(checkedNode);
        Assert.True(checkedNode!.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.True(checkedNode.Actions.HasFlag(SemanticsActions.Tap));
    }

    [Fact]
    public void CheckboxListTile_ExplicitDisabledState_BlocksTileTapAndEnabledSemantics()
    {
        int changeCount = 0;
        using var harness = new WidgetRenderHarness(
            BuildThemedTile(new CheckboxListTile(
                value: true,
                enabled: false,
                title: new Text("Disabled checkbox tile"),
                onChanged: _ => changeCount += 1)));

        var semanticsRoot = harness.PumpAndGetSemantics(new Size(400, 200));
        Tap(harness.RenderView, new Point(120, 28), pointer: 612);

        Assert.Equal(0, changeCount);
        var checkedNode = FindFirstSemanticsNode(
            semanticsRoot!,
            static node => node.Flags.HasFlag(SemanticsFlags.IsChecked));
        Assert.NotNull(checkedNode);
        Assert.False(checkedNode!.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.False(checkedNode.Actions.HasFlag(SemanticsActions.Tap));
    }

    [Fact]
    public void SwitchListTile_MergeSemantics_ExposesCheckedEnabledAndTap()
    {
        using var harness = new WidgetRenderHarness(
            BuildThemedTile(new SwitchListTile(
                value: true,
                title: new Text("Notifications"),
                onChanged: _ => { })));

        var semanticsRoot = harness.PumpAndGetSemantics(new Size(400, 200));
        var checkedNode = FindFirstSemanticsNode(
            semanticsRoot!,
            static node => node.Flags.HasFlag(SemanticsFlags.IsChecked));

        Assert.NotNull(checkedNode);
        Assert.True(checkedNode!.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.True(checkedNode.Actions.HasFlag(SemanticsActions.Tap));
    }

    [Fact]
    public void AdaptiveListTileControls_BuildOnCupertinoPlatform()
    {
        var theme = ThemeData.Light with { Platform = TargetPlatform.IOS };
        using var harness = new WidgetRenderHarness(
            BuildThemedTile(
                new Column(
                    children:
                    [
                        CheckboxListTile.Adaptive(
                            value: true,
                            title: new Text("Adaptive checkbox tile"),
                            onChanged: _ => { }),
                        SwitchListTile.Adaptive(
                            value: true,
                            title: new Text("Adaptive switch tile"),
                            onChanged: _ => { }),
                    ]),
                theme));

        harness.Pump(new Size(400, 240));

        Assert.NotNull(FindParagraphByText(harness.RenderView, "Adaptive checkbox tile"));
        Assert.NotNull(FindParagraphByText(harness.RenderView, "Adaptive switch tile"));
    }

    [Fact]
    public void ListTile_WithLeading_RemainsSafeAtZeroArea()
    {
        using var harness = new WidgetRenderHarness(new Theme(
            ThemeData.Light,
            new MediaQuery(
                new MediaQueryData(Size: new Size()),
                new Plumix.Material.Material(
                    child: new ListTile(
                        leading: new Icon(Icons.Done),
                        title: new Text("Zero"))))));

        harness.Pump(new Size());

        RenderListTile tile = Assert.IsType<RenderListTile>(
            FindDescendant<RenderListTile>(harness.RenderView));
        Assert.Equal(new Size(), tile.Size);
    }

    private static Widget BuildThemedTile(Widget tile, ThemeData? theme = null)
    {
        return new Theme(
            data: theme ?? ThemeData.Light,
            child: new MediaQuery(
                data: new MediaQueryData(Size: new Size(300, 800)),
                child: new SizedBox(
                    width: 300,
                    child: new Plumix.Material.Material(child: tile))));
    }

    private static void Tap(RenderView renderView, Point position, int pointer)
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        try
        {
            var timestamp = DateTime.UtcNow;
            binding.HandlePointerEvent(
                renderView,
                new PointerDownEvent(
                    pointer: pointer,
                    kind: PointerDeviceKind.Mouse,
                    position: position,
                    buttons: PointerButtons.Primary,
                    timestampUtc: timestamp));
            binding.HandlePointerEvent(
                renderView,
                new PointerUpEvent(
                    pointer: pointer,
                    kind: PointerDeviceKind.Mouse,
                    position: position,
                    buttons: PointerButtons.None,
                    timestampUtc: timestamp.AddMilliseconds(20)));
        }
        finally
        {
            binding.ResetForTests();
        }
    }

    private static RenderParagraph? FindParagraphByText(RenderObject? root, string text)
    {
        return FindDescendants<RenderParagraph>(root)
            .FirstOrDefault(paragraph => string.Equals(paragraph.PlainText, text, StringComparison.Ordinal));
    }

    private static void AssertParagraphColor(RenderObject? root, string text, Color expectedColor)
    {
        var paragraph = Assert.IsType<RenderParagraph>(FindParagraphByText(root, text));
        Assert.Equal(expectedColor, Assert.IsType<SolidColorBrush>(paragraph.Foreground).Color);
    }

    private static RenderDecoratedBox? FindTileSurface(RenderObject? root, double expectedHeight)
    {
        return FindDescendants<RenderDecoratedBox>(root).FirstOrDefault(box =>
            Math.Abs(box.Size.Width - 300) < 0.001
            && Math.Abs(box.Size.Height - expectedHeight) < 0.001);
    }

    private static Point GlobalOffsetOf(RenderObject renderObject)
    {
        var result = new Point();
        RenderObject? current = renderObject;
        while (current is not null)
        {
            if (current.parentData is BoxParentData parentData)
            {
                result = new Point(
                    result.X + parentData.offset.X,
                    result.Y + parentData.offset.Y);
            }

            current = current.Parent;
        }

        return result;
    }

    private static IEnumerable<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var results = new List<T>();
        CollectDescendants(root, results);
        return results;
    }

    private static List<RenderObject> ImmediateChildren(RenderObject root)
    {
        var children = new List<RenderObject>();
        root.VisitChildren(children.Add);
        return children;
    }

    private static void CollectDescendants<T>(RenderObject? root, List<T> results) where T : RenderObject
    {
        if (root is null)
        {
            return;
        }

        if (root is T typed)
        {
            results.Add(typed);
        }

        root.VisitChildren(child => CollectDescendants(child, results));
    }

    private static T? FindDescendant<T>(RenderObject? root) where T : RenderObject
    {
        return FindDescendants<T>(root).FirstOrDefault();
    }

    private static SemanticsNode? FindFirstSemanticsNode(SemanticsNode node, Func<SemanticsNode, bool> predicate)
    {
        if (predicate(node))
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            var found = FindFirstSemanticsNode(child, predicate);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
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

        public SemanticsNode? PumpAndGetSemantics(Size size)
        {
            Pump(size);
            _pipeline.RequestSemanticsUpdate();
            _pipeline.FlushSemantics();
            return _pipeline.SemanticsOwner.RootNode;
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
}
