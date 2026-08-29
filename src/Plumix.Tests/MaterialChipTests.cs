using Avalonia;
using Avalonia.Media;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialChipTests : IDisposable
{
    public MaterialChipTests()
    {
        Scheduler.ResetForTests();
    }

    public void Dispose()
    {
        GestureBinding.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void ChipConstructors_ValidateCallbacksAndElevation()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ActionChip(new Text("action"), () => { }, elevation: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ChoiceChip(new Text("choice"), false, _ => { }, pressElevation: double.NaN));
        Assert.Throws<ArgumentException>(() =>
            new RawChip(
                label: new Text("raw"),
                onPressed: () => { },
                onSelected: _ => { }));

        Assert.Equal(ChipVariant.Elevated, ActionChip.Elevated(new Text("a"), () => { }).Variant);
        Assert.Equal(ChipVariant.Elevated, ChoiceChip.Elevated(new Text("c"), false, _ => { }).Variant);
    }

    [Fact]
    public void ChipThemeData_FromDefaultsMatchesFlutterAlphaAndGeometryDefaults()
    {
        ChipThemeData defaults = ChipThemeData.FromDefaults(
            secondaryColor: Colors.CadetBlue,
            labelStyle: new TextStyle(FontSize: 14),
            brightness: Brightness.Light);

        Assert.Equal(Color.FromArgb(0x1f, 0, 0, 0), defaults.BackgroundColor);
        Assert.Equal(Color.FromArgb(0x0c, 0, 0, 0), defaults.DisabledColor);
        Assert.Equal(Color.FromArgb(0x3d, 0, 0, 0), defaults.SelectedColor);
        Assert.Equal(Color.FromArgb(0x3d, 95, 158, 160), defaults.SecondarySelectedColor);
        Assert.Equal(Color.FromArgb(0xde, 95, 158, 160), defaults.SecondaryLabelStyle!.Color);
        Assert.True(defaults.ShowCheckmark);
        Assert.Equal(new Thickness(4), defaults.Padding);
        Assert.Equal(8.0, defaults.PressElevation);
        Assert.Equal(18.0, defaults.IconTheme!.Size);

        Color customPrimary = Color.FromArgb(0x80, 0x11, 0x22, 0x33);
        ChipThemeData primaryDefaults = ChipThemeData.FromDefaults(
            secondaryColor: Colors.CadetBlue,
            labelStyle: new TextStyle(FontSize: 14),
            primaryColor: customPrimary);
        Assert.Equal(Color.FromArgb(0x1f, 0x11, 0x22, 0x33), primaryDefaults.BackgroundColor);
        Assert.Equal(Color.FromArgb(0xde, 0x11, 0x22, 0x33), primaryDefaults.LabelStyle!.Color);
        Assert.Throws<ArgumentException>(() => ChipThemeData.FromDefaults(
            secondaryColor: Colors.CadetBlue,
            labelStyle: new TextStyle(),
            brightness: Brightness.Light,
            primaryColor: Colors.Black));
    }

    [Fact]
    public void ChipThemeData_CopyWithRetainsUnspecifiedFieldsAndOverridesSelectedFields()
    {
        var color = MaterialStateProperty<Color?>.All(Colors.CadetBlue);
        var original = new ChipThemeData(
            Color: color,
            BackgroundColor: Colors.Black,
            DeleteIconColor: Colors.Blue,
            DisabledColor: Colors.Crimson,
            SelectedColor: Colors.DarkGreen,
            SecondarySelectedColor: Colors.Gold,
            ShadowColor: Colors.Gray,
            SurfaceTintColor: Colors.Green,
            SelectedShadowColor: Colors.Indigo,
            ShowCheckmark: false,
            CheckmarkColor: Colors.Lime,
            LabelPadding: new Thickness(1),
            Padding: new Thickness(2),
            Side: new BorderSide(Colors.Maroon, 3),
            Shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(4)),
            LabelStyle: new TextStyle(FontSize: 12),
            SecondaryLabelStyle: new TextStyle(FontSize: 13),
            Brightness: Brightness.Dark,
            Elevation: 5,
            PressElevation: 6,
            IconTheme: new IconThemeData(Color: Colors.Navy, Size: 17),
            AvatarBoxConstraints: new BoxConstraints(MinWidth: 18),
            DeleteIconBoxConstraints: new BoxConstraints(MinWidth: 19));

        Assert.Equal(original, original.CopyWith());

        ChipThemeData changed = original.CopyWith(
            backgroundColor: Colors.White,
            showCheckmark: true,
            elevation: 9);
        Assert.Same(color, changed.Color);
        Assert.Equal(Colors.White, changed.BackgroundColor);
        Assert.True(changed.ShowCheckmark);
        Assert.Equal(9, changed.Elevation);
        Assert.Equal(original.DeleteIconBoxConstraints, changed.DeleteIconBoxConstraints);
    }

    [Fact]
    public void ChipThemeData_LerpMatchesFlutterContinuousDiscreteAndNullEndpointRules()
    {
        var begin = new ChipThemeData(
            Color: MaterialStateProperty<Color?>.All(Colors.Black),
            BackgroundColor: Colors.Black,
            DeleteIconColor: Colors.Black,
            DisabledColor: Colors.Black,
            SelectedColor: Colors.Black,
            SecondarySelectedColor: Colors.Black,
            ShadowColor: Colors.Black,
            SurfaceTintColor: Colors.Black,
            SelectedShadowColor: Colors.Black,
            ShowCheckmark: false,
            CheckmarkColor: Colors.Black,
            LabelPadding: new Thickness(8, 0),
            Padding: new Thickness(4),
            Side: new BorderSide(Colors.Black, 2),
            Shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(2)),
            LabelStyle: new TextStyle(Color: Colors.Black, FontSize: 10),
            SecondaryLabelStyle: new TextStyle(Color: Colors.Black, FontSize: 12),
            Brightness: Brightness.Dark,
            Elevation: 1,
            PressElevation: 4,
            IconTheme: new IconThemeData(Color: Colors.Black, Size: 26, Opacity: 0.2),
            AvatarBoxConstraints: new BoxConstraints(MinWidth: 4, MaxWidth: 8),
            DeleteIconBoxConstraints: new BoxConstraints(MinHeight: 6, MaxHeight: 10));
        var end = new ChipThemeData(
            Color: MaterialStateProperty<Color?>.All(Colors.White),
            BackgroundColor: Colors.White,
            DeleteIconColor: Colors.White,
            DisabledColor: Colors.White,
            SelectedColor: Colors.White,
            SecondarySelectedColor: Colors.White,
            ShadowColor: Colors.White,
            SurfaceTintColor: Colors.White,
            SelectedShadowColor: Colors.White,
            ShowCheckmark: true,
            CheckmarkColor: Colors.White,
            LabelPadding: new Thickness(0, 8),
            Padding: new Thickness(2),
            Side: new BorderSide(Colors.White, 4),
            Shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(10)),
            LabelStyle: new TextStyle(Color: Colors.White, FontSize: 20),
            SecondaryLabelStyle: new TextStyle(Color: Colors.White, FontSize: 22),
            Brightness: Brightness.Light,
            Elevation: 5,
            PressElevation: 10,
            IconTheme: new IconThemeData(Color: Colors.White, Size: 22, Opacity: 1.0),
            AvatarBoxConstraints: new BoxConstraints(MinWidth: 8, MaxWidth: 12),
            DeleteIconBoxConstraints: new BoxConstraints(MinHeight: 10, MaxHeight: 14));

        Assert.Null(ChipThemeData.Lerp(null, null, 0.25));
        Assert.Same(begin, ChipThemeData.Lerp(begin, begin, 0.5));

        ChipThemeData midpoint = ChipThemeData.Lerp(begin, end, 0.5)!;
        Color middleGray = Color.FromArgb(0xff, 0x7f, 0x7f, 0x7f);
        Assert.Equal(middleGray, midpoint.Color!.Resolve(MaterialState.Pressed));
        Assert.Equal(middleGray, midpoint.BackgroundColor);
        Assert.Equal(middleGray, midpoint.DeleteIconColor);
        Assert.Equal(middleGray, midpoint.CheckmarkColor);
        Assert.True(midpoint.ShowCheckmark);
        Assert.Equal(new Thickness(4, 4), midpoint.LabelPadding);
        Assert.Equal(new Thickness(3), midpoint.Padding);
        Assert.Equal(middleGray, midpoint.Side!.Value.Color);
        Assert.Equal(3, midpoint.Side.Value.Width);
        Assert.Equal(6, ShapeBorderGeometry.ResolveRadius(midpoint.Shape).Radius);
        Assert.Equal(15, midpoint.LabelStyle!.FontSize);
        Assert.Equal(Brightness.Light, midpoint.Brightness);
        Assert.Equal(3, midpoint.Elevation);
        Assert.Equal(7, midpoint.PressElevation);
        Assert.Equal(24, midpoint.IconTheme!.Size);
        Assert.Equal(0.6, midpoint.IconTheme.Opacity!.Value, precision: 12);
        Assert.Equal(new BoxConstraints(MinWidth: 6, MaxWidth: 10), midpoint.AvatarBoxConstraints);
        Assert.Equal(new BoxConstraints(MinHeight: 8, MaxHeight: 12), midpoint.DeleteIconBoxConstraints);

        ChipThemeData fromNull = ChipThemeData.Lerp(null, end, 0.25)!;
        Assert.Equal(5.5, fromNull.IconTheme!.Size);
        Assert.Equal(0.25, fromNull.IconTheme.Opacity!.Value, precision: 12);
    }

    [Fact]
    public void ChipTheme_ParticipatesInInheritedThemeCapture()
    {
        var capturedData = new ChipThemeData(BackgroundColor: Colors.CadetBlue);
        var replacementData = new ChipThemeData(BackgroundColor: Colors.Crimson);
        ChipThemeData? resolvedData = null;
        Widget captureProbe = new ChipTheme(
            capturedData,
            new Builder(context =>
            {
                CapturedThemes capturedThemes = InheritedTheme.Capture(context);
                return new ChipTheme(
                    replacementData,
                    capturedThemes.Wrap(new Builder(capturedContext =>
                    {
                        resolvedData = ChipTheme.Of(capturedContext);
                        return new SizedBox();
                    })));
            }));

        using var harness = new WidgetRenderHarness(Root(ThemeData.Light, captureProbe));
        harness.Pump(new Size(320, 120));

        Assert.Same(capturedData, resolvedData);
    }

    [Fact]
    public void ActionChip_M3FlatDefaultsMatchOutlineLabelAndGeometryTokens()
    {
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(
                outlineVariant: Colors.CadetBlue,
                onSurface: Colors.DarkSlateBlue),
        };
        using var harness = new WidgetRenderHarness(Root(
            theme,
            new ActionChip(new Text("Action"), () => { })));

        harness.Pump(new Size(320, 120));

        var decoration = FindChipDecoration(harness.RenderView);
        Assert.Equal(MaterialColors.Transparent, decoration.Color);
        Assert.Equal(8, ChipRadius(decoration));
        Assert.Equal(Colors.CadetBlue, ChipSide(decoration).Color);
        Assert.Equal(1, ChipSide(decoration).Width);
        Assert.Equal(Colors.DarkSlateBlue, ForegroundColor(Paragraph(harness.RenderView, "Action")));
        RenderChip renderChip = FindChipRender(harness.RenderView);
        Assert.True(renderChip.Size.Height >= 32);
        Assert.Equal(new Thickness(8), renderChip.Padding);
    }

    [Fact]
    public void ActionChip_ElevatedUsesSurfaceContainerAndElevationDefaults()
    {
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(
                surfaceContainerLow: Colors.MediumPurple),
            ShadowColor = Colors.DarkGreen,
        };
        using var harness = new WidgetRenderHarness(Root(
            theme,
            ActionChip.Elevated(new Text("Elevated"), () => { })));

        harness.Pump(new Size(320, 120));

        var decoration = FindChipDecoration(harness.RenderView);
        Assert.Equal(Colors.MediumPurple, decoration.Color);
        Assert.Equal(MaterialColors.Transparent, ChipSide(decoration).Color);
        // The elevation shadow is painted by the chip's `Material`, not by its `Ink` decoration.
        Assert.Contains(
            FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.BoxShadows is { Count: > 0 });
    }

    [Fact]
    public void ChoiceChip_SelectedUsesSecondaryContainerCheckmarkAndSelectedSemantics()
    {
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(
                secondaryContainer: Colors.DarkGreen,
                onSecondaryContainer: Colors.Gold),
        };
        using var harness = new WidgetRenderHarness(Root(
            theme,
            new ChoiceChip(new Text("Selected"), true, _ => { })));

        var semantics = harness.PumpAndGetSemantics(new Size(320, 120));

        Assert.Equal(Colors.DarkGreen, FindChipDecoration(harness.RenderView).Color);
        Assert.Equal(Colors.Gold, ForegroundColor(Paragraph(harness.RenderView, "Selected")));
        Assert.Equal(1.0, FindChipRender(harness.RenderView).CheckmarkProgress);
        // Off the web Dart reports the chip's selection through `selected`, never `checked`.
        var selected = FindSemantics(semantics, node => node.Flags.HasFlag(SemanticsFlags.IsSelected));
        Assert.NotNull(selected);
        Assert.False(selected!.Flags.HasFlag(SemanticsFlags.IsChecked));
        Assert.True(selected.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.NotNull(FindSemantics(selected, node => node.Actions.HasFlag(SemanticsActions.Tap)));
    }

    [Fact]
    public void ChoiceChip_DisabledSelectedUsesDisabledSelectedTokenAndNoTapAction()
    {
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(onSurface: Colors.Crimson),
        };
        using var harness = new WidgetRenderHarness(Root(
            theme,
            new ChoiceChip(new Text("Disabled"), true, onSelected: null)));

        var semantics = harness.PumpAndGetSemantics(new Size(320, 120));

        Assert.Equal(WithOpacity(Colors.Crimson, 0.12), FindChipDecoration(harness.RenderView).Color);
        Assert.Equal(Colors.Crimson, ForegroundColor(Paragraph(harness.RenderView, "Disabled")));
        Assert.Equal(0.0, FindChipRender(harness.RenderView).EnableProgress);
        var selected = FindSemantics(semantics, node => node.Flags.HasFlag(SemanticsFlags.IsSelected));
        Assert.NotNull(selected);
        Assert.False(selected!.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.Null(FindSemantics(selected, node => node.Actions.HasFlag(SemanticsActions.Tap)));
    }

    [Fact]
    public void Chips_ResolveWidgetThenLocalThemeThenDefaults()
    {
        var themeData = new ChipThemeData(
            BackgroundColor: Colors.Purple,
            SelectedColor: Colors.DarkGreen,
            LabelStyle: new TextStyle(Color: Colors.Orange),
            Shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(13)));
        using var themedHarness = new WidgetRenderHarness(Root(
            ThemeData.Light,
            new ChipTheme(
                themeData,
                new ActionChip(new Text("Themed"), () => { }))));
        themedHarness.Pump(new Size(320, 120));

        var themed = FindChipDecoration(themedHarness.RenderView);
        Assert.Equal(Colors.Purple, themed.Color);
        Assert.Equal(13, ChipRadius(themed));
        Assert.Equal(Colors.Orange, ForegroundColor(Paragraph(themedHarness.RenderView, "Themed")));

        using var widgetHarness = new WidgetRenderHarness(Root(
            ThemeData.Light,
            new ChipTheme(
                themeData,
                new ChoiceChip(
                    new Text("Widget"),
                    selected: true,
                    onSelected: _ => { },
                    selectedColor: Colors.Gold,
                    labelStyle: new TextStyle(Color: Colors.Navy),
                    shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(3))))));
        widgetHarness.Pump(new Size(320, 120));

        var widget = FindChipDecoration(widgetHarness.RenderView);
        Assert.Equal(Colors.Gold, widget.Color);
        Assert.Equal(3, ChipRadius(widget));
        Assert.Equal(Colors.Navy, ForegroundColor(Paragraph(widgetHarness.RenderView, "Widget")));
    }

    [Fact]
    public void WidgetStateColorOverridesLegacyColorsAndHandlesDisabledSelectedCombination()
    {
        var stateColor = MaterialStateProperty<Color?>.ResolveWith(states =>
            states.HasFlag(MaterialState.Disabled) && states.HasFlag(MaterialState.Selected)
                ? Colors.Crimson
                : states.HasFlag(MaterialState.Selected)
                    ? Colors.Gold
                    : Colors.CadetBlue);
        using var harness = new WidgetRenderHarness(Root(
            ThemeData.Light,
            new ChoiceChip(
                new Text("State"),
                selected: true,
                onSelected: null,
                color: stateColor,
                selectedColor: Colors.DarkGreen,
                disabledColor: Colors.Gray)));

        harness.Pump(new Size(320, 120));

        Assert.Equal(Colors.Crimson, FindChipDecoration(harness.RenderView).Color);
    }

    [Fact]
    public void RawChip_LegacySelectedColorAnimatesOverConfiguredSelectDuration()
    {
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(secondaryContainer: Colors.DarkGreen),
        };
        var animation = new ChipAnimationStyle(
            SelectAnimation: new AnimationStyle(Duration: TimeSpan.FromSeconds(10)));
        using var harness = new WidgetRenderHarness(Root(
            theme,
            new RawChip(
                label: new Text("Animated"),
                selected: false,
                onSelected: _ => { },
                selectedColor: Colors.DarkGreen,
                chipAnimationStyle: animation)));
        harness.Pump(new Size(320, 120));

        harness.Update(Root(
            theme,
            new RawChip(
                label: new Text("Animated"),
                selected: true,
                onSelected: _ => { },
                selectedColor: Colors.DarkGreen,
                chipAnimationStyle: animation)));
        harness.Pump(new Size(320, 120));
        var start = FindChipDecoration(harness.RenderView).Color;

        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1));
        harness.Pump(new Size(320, 120));
        var middle = FindChipDecoration(harness.RenderView).Color;
        Assert.NotEqual(start, middle);
        Assert.NotEqual(Colors.DarkGreen, middle);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 11));
        harness.Pump(new Size(320, 120));
        Assert.Equal(Colors.DarkGreen, FindChipDecoration(harness.RenderView).Color);
    }

    [Fact]
    public void Chips_InvokeActionAndInverseSelectionCallbacks()
    {
        int actionCount = 0;
        bool? selected = null;
        using var actionHarness = new WidgetRenderHarness(Root(
            ThemeData.Light,
            new ActionChip(new Text("Action"), () => actionCount++)));
        actionHarness.Pump(new Size(320, 120));
        Tap(actionHarness.RenderView, new Point(160, 60), 21);
        Assert.Equal(1, actionCount);

        using var choiceHarness = new WidgetRenderHarness(Root(
            ThemeData.Light,
            new ChoiceChip(new Text("Choice"), selected: true, onSelected: value => selected = value)));
        choiceHarness.Pump(new Size(320, 120));
        Tap(choiceHarness.RenderView, new Point(160, 60), 22);
        Assert.False(selected);
    }

    [Fact]
    public void Chips_M2DefaultsAndCompactTapTargetMatchFlutterPolicies()
    {
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            VisualDensity = VisualDensity.Compact,
        };
        using var harness = new WidgetRenderHarness(Root(
            theme,
            new ChoiceChip(new Text("M2"), selected: true, onSelected: _ => { })));

        harness.Pump(new Size(320, 120));

        Assert.Equal(WithOpacity(theme.PrimaryColor, 0x3d / 255.0), FindChipDecoration(harness.RenderView).Color);
        Assert.Equal(10_000, ChipRadius(FindChipDecoration(harness.RenderView)));
        Assert.Single(FindDescendants<RenderParagraph>(harness.RenderView));
        Assert.Contains(FindDescendants<RenderChipRedirectingHitDetection>(harness.RenderView),
            box => box.AdditionalConstraints.MinWidth == 40 && box.AdditionalConstraints.MinHeight == 40);
    }

    [Fact]
    public void FilterAndInputChip_ConstructorsValidateContractsAndElevatedFactory()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new FilterChip(new Text("Filter"), _ => { }, elevation: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new InputChip(new Text("Input"), pressElevation: double.NaN));
        Assert.Throws<ArgumentException>(() =>
            new InputChip(
                new Text("Input"),
                onSelected: _ => { },
                onPressed: () => { }));

        Assert.Equal(
            ChipVariant.Elevated,
            FilterChip.Elevated(new Text("Elevated"), _ => { }).Variant);
    }

    [Fact]
    public void Chip_IsDeleteOnlyAndForwardsDeleteAppearanceAndSemanticsToRawChip()
    {
        int deleted = 0;
        var deleteConstraints = BoxConstraints.Tight(new Size(20, 20));
        using var harness = new WidgetRenderHarness(Root(
            ThemeData.Light,
            new Chip(
                label: new Text("Information"),
                avatar: new Icon(Icons.InfoOutline),
                onDeleted: () => deleted++,
                deleteIcon: new Icon(Icons.Clear),
                deleteIconColor: Colors.Purple,
                deleteIconBoxConstraints: deleteConstraints,
                deleteButtonTooltipMessage: "Remove information")));

        var semantics = harness.PumpAndGetSemantics(new Size(320, 120));

        Assert.Equal(MaterialColors.Transparent, FindChipDecoration(harness.RenderView).Color);
        RenderParagraph clear = FindDescendants<RenderParagraph>(harness.RenderView)
            .Single(paragraph => paragraph.PlainText == IconText(Icons.Clear));
        Assert.Equal(Colors.Purple, ForegroundColor(clear));
        Assert.Equal(deleteConstraints, FindChipRender(harness.RenderView).DeleteIconBoxConstraints);

        var body = FindSemantics(semantics, node => HasLabelPart(node, "Information")
                                                 && node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.Null(body);
        var delete = FindSemantics(semantics, node => HasLabelPart(node, "Remove information"));
        Assert.NotNull(delete);
        Assert.True(delete!.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.True(delete.PerformAction(SemanticsActions.Tap));
        Assert.Equal(1, deleted);
    }

    [Fact]
    public void FilterChip_M3SelectedDefaultsUseSecondaryTokensCheckmarkAndClearDeleteIcon()
    {
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(
                secondaryContainer: Colors.DarkGreen,
                onSecondaryContainer: Colors.Gold),
        };
        using var harness = new WidgetRenderHarness(Root(
            theme,
            new FilterChip(
                new Text("Filter"),
                onSelected: _ => { },
                selected: true,
                onDeleted: () => { })));

        harness.Pump(new Size(320, 120));

        Assert.Equal(Colors.DarkGreen, FindChipDecoration(harness.RenderView).Color);
        Assert.Equal(Colors.Gold, ForegroundColor(Paragraph(harness.RenderView, "Filter")));
        Assert.Equal(1.0, FindChipRender(harness.RenderView).CheckmarkProgress);
        Assert.Contains(FindDescendants<RenderParagraph>(harness.RenderView),
            paragraph => paragraph.PlainText == IconText(Icons.Clear));
    }

    [Fact]
    public void FilterChip_ElevatedAndDisabledDefaultsMatchFlutterStatePolicy()
    {
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(
                surfaceContainerLow: Colors.MediumPurple,
                onSurface: Colors.Crimson),
        };
        using var enabled = new WidgetRenderHarness(Root(
            theme,
            FilterChip.Elevated(new Text("Enabled"), _ => { })));
        enabled.Pump(new Size(320, 120));
        Assert.Equal(Colors.MediumPurple, FindChipDecoration(enabled.RenderView).Color);
        Assert.Contains(
            FindDescendants<RenderDecoratedBox>(enabled.RenderView),
            box => box.Decoration.BoxShadows is { Count: > 0 });

        using var disabled = new WidgetRenderHarness(Root(
            theme,
            FilterChip.Elevated(
                new Text("Disabled"),
                onSelected: null,
                selected: true,
                onDeleted: () => { })));
        var semantics = disabled.PumpAndGetSemantics(new Size(320, 120));
        Assert.Equal(WithOpacity(Colors.Crimson, 0.12), FindChipDecoration(disabled.RenderView).Color);
        var delete = FindSemantics(
            semantics,
            node => HasLabelPart(node, "Delete") && node.Flags.HasFlag(SemanticsFlags.IsButton));
        Assert.NotNull(delete);
        Assert.False(delete!.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.False(delete.Actions.HasFlag(SemanticsActions.Tap));
    }

    [Fact]
    public void FilterAndInputChip_M2DefaultsUseLegacySelectionAndCancelIcon()
    {
        var theme = ThemeData.Light with { UseMaterial3 = false };
        using var filter = new WidgetRenderHarness(Root(
            theme,
            new FilterChip(
                new Text("Filter M2"),
                onSelected: _ => { },
                selected: true,
                onDeleted: () => { })));
        filter.Pump(new Size(320, 120));
        Assert.Equal(WithOpacity(Colors.Black, 0x3d / 255.0), FindChipDecoration(filter.RenderView).Color);
        Assert.Contains(FindDescendants<RenderParagraph>(filter.RenderView),
            paragraph => paragraph.PlainText == IconText(Icons.Cancel));
        Assert.DoesNotContain(FindDescendants<RenderParagraph>(filter.RenderView),
            paragraph => paragraph.PlainText == IconText(Icons.Check));

        using var input = new WidgetRenderHarness(Root(
            theme,
            new InputChip(
                new Text("Input M2"),
                selected: true,
                onSelected: _ => { },
                onDeleted: () => { })));
        input.Pump(new Size(320, 120));
        Assert.Equal(WithOpacity(Colors.Black, 0x3d / 255.0), FindChipDecoration(input.RenderView).Color);
        Assert.Contains(FindDescendants<RenderParagraph>(input.RenderView),
            paragraph => paragraph.PlainText == IconText(Icons.Cancel));
        Assert.DoesNotContain(FindDescendants<RenderParagraph>(input.RenderView),
            paragraph => paragraph.PlainText == IconText(Icons.Check));
    }

    [Fact]
    public void FilterChip_SelectionAndDeleteCallbacksRemainIndependent()
    {
        bool? selected = null;
        int deleted = 0;
        using var harness = new WidgetRenderHarness(Root(
            ThemeData.Light,
            new FilterChip(
                new Text("Filter"),
                onSelected: value => selected = value,
                onDeleted: () => deleted++,
                deleteButtonTooltipMessage: "Remove filter")));

        var semantics = harness.PumpAndGetSemantics(new Size(320, 120));
        var body = FindSemantics(
            semantics,
            node => node.Label != "Remove filter"
                    && node.Actions.HasFlag(SemanticsActions.Tap));
        var delete = FindSemantics(semantics, node => HasLabelPart(node, "Remove filter"));
        Assert.NotNull(body);
        Assert.NotNull(delete);

        Rect deleteRect = delete!.GlobalRect;
        var deleteCenter = new Point(
            deleteRect.X + (deleteRect.Width / 2),
            deleteRect.Y + (deleteRect.Height / 2));
        Tap(harness.RenderView, deleteCenter, 31);
        Assert.Equal(1, deleted);
        Assert.Null(selected);

        Assert.True(body!.PerformAction(SemanticsActions.Tap));
        Assert.True(selected);
        Assert.Equal(1, deleted);
    }

    [Fact]
    public void InputChip_DeleteOnlyPathStaysVisuallyEnabledWithoutBodyTap()
    {
        int deleted = 0;
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(
                outlineVariant: Colors.CadetBlue,
                onSurface: Colors.Crimson),
        };
        using var harness = new WidgetRenderHarness(Root(
            theme,
            new InputChip(
                new Text("Person"),
                avatar: new CircleAvatar(child: new Text("P")),
                onDeleted: () => deleted++)));

        var semantics = harness.PumpAndGetSemantics(new Size(320, 120));

        var decoration = FindChipDecoration(harness.RenderView);
        Assert.Equal(Colors.CadetBlue, ChipSide(decoration).Color);
        Assert.Equal(MaterialColors.Transparent, decoration.Color);
        var body = FindSemantics(semantics, node => node.Flags.HasFlag(SemanticsFlags.IsSelected));
        Assert.Null(body);
        var delete = FindSemantics(
            semantics,
            node => HasLabelPart(node, "Delete") && node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(delete);
        Assert.True(delete!.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.True(delete.PerformAction(SemanticsActions.Tap));
        Assert.Equal(1, deleted);
    }

    [Fact]
    public void InputChip_ExplicitDisabledStateDisablesBodyAndDelete()
    {
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(onSurface: Colors.Crimson),
        };
        using var harness = new WidgetRenderHarness(Root(
            theme,
            new InputChip(
                new Text("Disabled"),
                selected: true,
                isEnabled: false,
                onSelected: _ => { },
                onDeleted: () => { })));

        var semantics = harness.PumpAndGetSemantics(new Size(320, 120));

        Assert.Equal(WithOpacity(Colors.Crimson, 0.12), FindChipDecoration(harness.RenderView).Color);
        var selected = FindSemantics(semantics, node => node.Flags.HasFlag(SemanticsFlags.IsSelected));
        Assert.NotNull(selected);
        Assert.False(selected!.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.False(selected.Actions.HasFlag(SemanticsActions.Tap));
        var delete = FindSemantics(
            semantics,
            node => HasLabelPart(node, "Delete") && node.Flags.HasFlag(SemanticsFlags.IsButton));
        Assert.NotNull(delete);
        Assert.False(delete!.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.False(delete.Actions.HasFlag(SemanticsActions.Tap));
    }

    [Fact]
    public void InputChip_M3SelectedDefaultsResolveLabelCheckmarkAndDeleteTokens()
    {
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.CadetBlue,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(
                secondaryContainer: Colors.DarkGreen,
                onSecondaryContainer: Colors.Gold),
        };
        using var harness = new WidgetRenderHarness(Root(
            theme,
            new InputChip(
                new Text("Selected input"),
                selected: true,
                onSelected: _ => { },
                onDeleted: () => { })));

        harness.Pump(new Size(320, 120));

        Assert.Equal(Colors.DarkGreen, FindChipDecoration(harness.RenderView).Color);
        Assert.Equal(Colors.Gold, ForegroundColor(Paragraph(harness.RenderView, "Selected input")));
        RenderChip renderChip = FindChipRender(harness.RenderView);
        var clear = FindDescendants<RenderParagraph>(harness.RenderView)
            .Single(paragraph => paragraph.PlainText == IconText(Icons.Clear));
        Assert.Equal(Colors.CadetBlue, renderChip.CheckmarkColor);
        Assert.Equal(1.0, renderChip.CheckmarkProgress);
        Assert.Equal(Colors.Gold, ForegroundColor(clear));
    }

    [Fact]
    public void InputChip_OnPressedAndOnSelectedPathsMatchBodyCallbackContract()
    {
        int presses = 0;
        using var harness = new WidgetRenderHarness(Root(
            ThemeData.Light,
            new InputChip(new Text("Press"), onPressed: () => presses++)));

        var semantics = harness.PumpAndGetSemantics(new Size(320, 120));
        var body = FindSemantics(
            semantics,
            node => node.Flags.HasFlag(SemanticsFlags.IsEnabled)
                    && node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(body);
        Assert.True(body!.PerformAction(SemanticsActions.Tap));
        Assert.Equal(1, presses);
    }

    [Fact]
    public void DeleteIconUsesWidgetColorConstraintsAndLocalizedTooltipPrecedence()
    {
        var constraints = BoxConstraints.Tight(new Size(20, 20));
        using var harness = new WidgetRenderHarness(Root(
            ThemeData.Light,
            new InputChip(
                new Text("Localized"),
                onDeleted: () => { },
                deleteIcon: new Icon(Icons.Clear),
                deleteIconColor: Colors.Purple,
                deleteIconBoxConstraints: constraints),
            new TestMaterialLocalizations("Effacer")));

        var semantics = harness.PumpAndGetSemantics(new Size(320, 120));

        var clear = FindDescendants<RenderParagraph>(harness.RenderView)
            .Single(paragraph => paragraph.PlainText == IconText(Icons.Clear));
        Assert.Equal(Colors.Purple, ForegroundColor(clear));
        Assert.Equal(constraints, FindChipRender(harness.RenderView).DeleteIconBoxConstraints);
        var delete = FindSemantics(semantics, node => HasLabelPart(node, "Effacer"));
        Assert.NotNull(delete);
        Assert.True(delete!.Actions.HasFlag(SemanticsActions.Tap));
        Assert.True(delete.Rect.Width >= 48);
        Assert.True(delete.Rect.Height >= 48);
    }

    [Fact]
    public void RawChip_UsesSlottedRenderGeometryAndMirrorsOffsetsInRtl()
    {
        var avatarConstraints = BoxConstraints.Tight(new Size(18, 18));
        var deleteConstraints = BoxConstraints.Tight(new Size(20, 20));
        Widget Chip() => new RawChip(
            label: new Text("Geometry"),
            avatar: new Icon(Icons.Star),
            onDeleted: () => { },
            avatarBoxConstraints: avatarConstraints,
            deleteIconBoxConstraints: deleteConstraints);

        using var ltrHarness = new WidgetRenderHarness(Root(ThemeData.Light, Chip()));
        ltrHarness.Pump(new Size(320, 120));
        RenderChip ltr = FindChipRender(ltrHarness.RenderView);
        Point ltrAvatar = ParentOffset(ltr.Avatar!);
        Point ltrLabel = ParentOffset(ltr.Label);
        Point ltrDelete = ParentOffset(ltr.DeleteIcon!);
        Assert.True(ltrAvatar.X < ltrLabel.X);
        Assert.True(ltrLabel.X < ltrDelete.X);
        Assert.Equal(avatarConstraints, ltr.AvatarBoxConstraints);
        Assert.Equal(deleteConstraints, ltr.DeleteIconBoxConstraints);
        Assert.True(ltr.DeleteButtonRect.Width <= ltr.Size.Width * 0.5);

        using var rtlHarness = new WidgetRenderHarness(Root(
            ThemeData.Light,
            new Directionality(TextDirection.Rtl, Chip())));
        rtlHarness.Pump(new Size(320, 120));
        RenderChip rtl = FindChipRender(rtlHarness.RenderView);
        Point rtlAvatar = ParentOffset(rtl.Avatar!);
        Point rtlLabel = ParentOffset(rtl.Label);
        Point rtlDelete = ParentOffset(rtl.DeleteIcon!);
        Assert.True(rtlDelete.X < rtlLabel.X);
        Assert.True(rtlLabel.X < rtlAvatar.X);
    }

    [Fact]
    public void RawChip_AvatarAndDeleteDrawersHonorForwardAndReverseAnimationStyles()
    {
        var animations = new ChipAnimationStyle(
            AvatarDrawerAnimation: new AnimationStyle(
                Duration: TimeSpan.FromMilliseconds(800),
                ReverseDuration: TimeSpan.FromMilliseconds(400)),
            DeleteDrawerAnimation: new AnimationStyle(
                Duration: TimeSpan.FromMilliseconds(500),
                ReverseDuration: TimeSpan.FromMilliseconds(250)));
        using var harness = new WidgetRenderHarness(Root(
            ThemeData.Light,
            new RawChip(label: new Text("Animated slots"), chipAnimationStyle: animations)));
        harness.Pump(new Size(320, 120));
        Assert.Equal(0.0, FindChipRender(harness.RenderView).AvatarDrawerProgress);
        Assert.Equal(0.0, FindChipRender(harness.RenderView).DeleteDrawerProgress);

        harness.Update(Root(
            ThemeData.Light,
            new RawChip(
                label: new Text("Animated slots"),
                avatar: new Icon(Icons.Star),
                onDeleted: () => { },
                chipAnimationStyle: animations)));
        harness.Pump(new Size(320, 120));
        double forwardStart = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(forwardStart + 0.25));
        harness.Pump(new Size(320, 120));
        RenderChip opening = FindChipRender(harness.RenderView);
        Assert.InRange(opening.AvatarDrawerProgress, 0.001, 0.999);
        Assert.InRange(opening.DeleteDrawerProgress, 0.001, 0.999);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(forwardStart + 1.0));
        harness.Pump(new Size(320, 120));
        Assert.Equal(1.0, FindChipRender(harness.RenderView).AvatarDrawerProgress);
        Assert.Equal(1.0, FindChipRender(harness.RenderView).DeleteDrawerProgress);

        harness.Update(Root(
            ThemeData.Light,
            new RawChip(label: new Text("Animated slots"), chipAnimationStyle: animations)));
        harness.Pump(new Size(320, 120));
        double reverseStart = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(reverseStart + 0.125));
        harness.Pump(new Size(320, 120));
        RenderChip closing = FindChipRender(harness.RenderView);
        Assert.InRange(closing.AvatarDrawerProgress, 0.001, 0.999);
        Assert.InRange(closing.DeleteDrawerProgress, 0.001, 0.999);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(reverseStart + 1.0));
        harness.Pump(new Size(320, 120));
        RenderChip closed = FindChipRender(harness.RenderView);
        Assert.Equal(0.0, closed.AvatarDrawerProgress);
        Assert.Equal(0.0, closed.DeleteDrawerProgress);
        Assert.Null(closed.Avatar);
        Assert.Null(closed.DeleteIcon);
    }

    [Fact]
    public void RawChip_StatefulShapeAndSideResolveSelectedAndDisabledStates()
    {
        var shape = MaterialStateProperty<ShapeBorder?>.ResolveWith(states =>
            states.HasFlag(MaterialState.Selected)
                ? new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(13))
                : new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(3)));
        var side = MaterialStateProperty<BorderSide?>.ResolveWith(states =>
            states.HasFlag(MaterialState.Disabled)
                ? new BorderSide(Colors.Crimson, 2)
                : new BorderSide(Colors.CadetBlue, 1));
        using var harness = new WidgetRenderHarness(Root(
            ThemeData.Light,
            new RawChip(
                label: new Text("State geometry"),
                selected: true,
                isEnabled: false,
                shape: shape,
                side: side)));

        harness.Pump(new Size(320, 120));

        ShapeDecoration decoration = FindChipDecoration(harness.RenderView);
        Assert.Equal(13, ChipRadius(decoration));
        Assert.Equal(Colors.Crimson, ChipSide(decoration).Color);
        Assert.Equal(2, ChipSide(decoration).Width);
    }

    [Fact]
    public void RawChip_HorizontalDensityDoesNotChangeContentWidth()
    {
        using var standardHarness = new WidgetRenderHarness(Root(
            ThemeData.Light,
            new RawChip(label: new Text("Density"))));
        standardHarness.Pump(new Size(320, 120));
        double standardWidth = FindChipRender(standardHarness.RenderView).Size.Width;

        using var horizontalHarness = new WidgetRenderHarness(Root(
            ThemeData.Light,
            new RawChip(
                label: new Text("Density"),
                visualDensity: new VisualDensity(horizontal: 4, vertical: 0))));
        horizontalHarness.Pump(new Size(320, 120));
        double horizontalWidth = FindChipRender(horizontalHarness.RenderView).Size.Width;

        Assert.Equal(standardWidth, horizontalWidth);
    }

    private static Widget Root(
        ThemeData theme,
        Widget child,
        MaterialLocalizations? localizations = null)
    {
        Widget result = new MediaQuery(
            data: new MediaQueryData(Size: new Size(320, 120)),
            child: new Directionality(
                TextDirection.Ltr,
                new Theme(
                    theme,
                    new Align(alignment: Alignment.Center, child: child))));
        return localizations is null
            ? result
            : new MaterialLocalizationsScope(localizations, result);
    }

    private static string IconText(IconData icon) => char.ConvertFromUtf32(icon.CodePoint);

    /// <summary>
    /// The chip's own background: Dart paints it with `Ink`, so it lands on the chip's material as
    /// an ink decoration rather than as a `DecoratedBox`.
    /// </summary>
    private static ShapeDecoration FindChipDecoration(RenderObject root)
    {
        return FindDescendants<RenderInkDecoration>(root)
            .Select(ink => ink.Decoration)
            .OfType<ShapeDecoration>()
            .First();
    }

    private static double ChipRadius(ShapeDecoration decoration) =>
        ShapeBorderGeometry.ResolveRadius(decoration.Shape).TopLeft;

    private static BorderSide ChipSide(ShapeDecoration decoration) =>
        ShapeBorderGeometry.SideOrNone(decoration.Shape);

    private static RenderChip FindChipRender(RenderObject root)
    {
        return FindDescendants<RenderChip>(root).Single();
    }

    private static Point ParentOffset(RenderBox child)
    {
        return ((BoxParentData)child.parentData!).offset;
    }

    private static RenderParagraph Paragraph(RenderObject root, string text)
    {
        return FindDescendants<RenderParagraph>(root).Single(paragraph => paragraph.PlainText == text);
    }

    private static Color ForegroundColor(RenderParagraph paragraph)
    {
        return Assert.IsType<SolidColorBrush>(paragraph.Foreground).Color;
    }

    private static Color WithOpacity(Color color, double opacity)
    {
        return Color.FromArgb(
            (byte)Math.Round(Math.Clamp(opacity, 0, 1) * 255),
            color.R,
            color.G,
            color.B);
    }

    private static void Tap(RenderView view, Point point, int pointer)
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        try
        {
            var now = DateTime.UtcNow;
            binding.HandlePointerEvent(view, new PointerDownEvent(
                pointer, PointerDeviceKind.Mouse, point, PointerButtons.Primary, now));
            binding.HandlePointerEvent(view, new PointerUpEvent(
                pointer, PointerDeviceKind.Mouse, point, PointerButtons.None, now.AddMilliseconds(16)));
        }
        finally
        {
            binding.ResetForTests();
        }
    }

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null) return result;
        if (root is T target) result.Add(target);
        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private static SemanticsNode? FindSemantics(SemanticsNode? root, Func<SemanticsNode, bool> predicate)
    {
        if (root is null) return null;
        if (predicate(root)) return root;
        foreach (var child in root.Children)
        {
            var found = FindSemantics(child, predicate);
            if (found is not null) return found;
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
            _rootElement.Mount(null, null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public void Update(Widget widget)
        {
            _rootElement.UpdateRoot(widget);
            _owner.FlushBuild();
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
            private readonly RenderView _view;
            private Element? _child;

            public HarnessRootElement(RenderView view, Widget widget) : base(widget) => _view = view;
            public override RenderObject? RenderObject => _child?.RenderObject;
            internal override Element? RenderObjectAttachingChild => _child;
            public void UpdateRoot(Widget widget) => Update(widget);
            protected override void OnMount() { base.OnMount(); Rebuild(); }
            internal override void Rebuild() { Dirty = false; _child = UpdateChild(_child, Widget, Slot); }
            internal override void Update(Widget widget) { base.Update(widget); Rebuild(); }
            internal override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
            internal override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
            public void InsertRenderObjectChild(RenderObject child, object? slot) => _view.Child = (RenderBox)child;
            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
            public void RemoveRenderObjectChild(RenderObject child, object? slot) { if (ReferenceEquals(_view.Child, child)) _view.Child = null; }
            internal override void Unmount() { if (_child is not null) { UnmountChild(_child); _child = null; } base.Unmount(); }
        }
    }

    private sealed class TestMaterialLocalizations(string deleteTooltip) : MaterialLocalizations
    {
        public override string DeleteButtonTooltip => deleteTooltip;

        public override string TabLabel(int tabIndex, int tabCount) => $"{tabIndex}/{tabCount}";
    }

    /// <summary>
    /// Whether one of the node's merged label parts is <paramref name="part"/>. A merged node joins
    /// the labels it absorbed with a newline, exactly like Flutter's <c>_concatAttributedString</c>.
    /// </summary>
    private static bool HasLabelPart(SemanticsNode node, string part) =>
        node.Label?.Split('\n').Contains(part) == true;
}

