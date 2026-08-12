using System;
using System.Collections.Generic;
using System.Linq;
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
public sealed class MaterialRadioExpansionTileTests : IDisposable
{
    public MaterialRadioExpansionTileTests()
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
    public void RadioListTile_Constructor_ValidatesThreeLineAndRetainsScaleFactor()
    {
        Assert.Throws<ArgumentException>(() => new RadioListTile<string>(
            value: "a",
            isThreeLine: true));
        var tile = new RadioListTile<string>(
            value: "a",
            radioScaleFactor: 0);
        Assert.Equal(0, tile.RadioScaleFactor);
    }

    [Fact]
    public void RadioListTile_RadioGroupTap_SelectsTileValue()
    {
        string? changed = null;
        using var harness = new WidgetRenderHarness(
            BuildThemed(new RadioGroup<string>(
                groupValue: "a",
                onChanged: value => changed = value,
                child: new RadioListTile<string>(
                    value: "b",
                    title: new Text("Option B")))));

        harness.Pump(new Size(360, 180));
        Tap(harness.RenderView, new Point(140, 28), pointer: 701);

        Assert.Equal("b", changed);
    }

    [Fact]
    public void RadioListTile_RadioGroupArrowKey_SelectsAndFocusesNextTile()
    {
        string? changed = null;
        var firstFocus = new FocusNode();
        var secondFocus = new FocusNode();
        using var harness = new WidgetRenderHarness(
            BuildThemed(new RadioGroup<string>(
                groupValue: "a",
                onChanged: value => changed = value,
                child: new Column(
                    children:
                    [
                        new RadioListTile<string>(
                            value: "a",
                            title: new Text("Option A"),
                            focusNode: firstFocus),
                        new RadioListTile<string>(
                            value: "b",
                            title: new Text("Option B"),
                            focusNode: secondFocus),
                    ]))));

        harness.Pump(new Size(360, 180));
        Assert.True(firstFocus.RequestFocus());
        Assert.True(FocusManager.Instance.HandleKeyEvent(new KeyEvent("ArrowRight", isDown: true)));

        Assert.Equal("b", changed);
        Assert.True(secondFocus.HasFocus);

        firstFocus.Dispose();
        secondFocus.Dispose();
    }

    [Fact]
    public void RadioListTile_ToggleableSelectedTap_ReturnsNull()
    {
        string? changed = "unchanged";
        using var harness = new WidgetRenderHarness(
            BuildThemed(new RadioListTile<string>(
                value: "a",
                groupValue: "a",
                toggleable: true,
                title: new Text("Option A"),
                onChanged: value => changed = value)));

        harness.Pump(new Size(360, 180));
        Tap(harness.RenderView, new Point(140, 28), pointer: 702);

        Assert.Null(changed);
    }

    [Fact]
    public void RadioListTile_SelectedNonToggleableTap_DoesNotInvokeCallback()
    {
        int calls = 0;
        using var harness = new WidgetRenderHarness(
            BuildThemed(new RadioListTile<string>(
                value: "a",
                groupValue: "a",
                title: new Text("Selected option"),
                onChanged: _ => calls += 1)));

        harness.Pump(new Size(360, 180));
        Tap(harness.RenderView, new Point(140, 28), pointer: 703);

        Assert.Equal(0, calls);
    }

    [Fact]
    public void RadioListTile_PlatformAffinity_DefaultsToLeadingAndUsesSelectedThemeColor()
    {
        var selectedColor = Color.Parse("#FF006C4C");
        var theme = ThemeData.Light with
        {
            RadioTheme = new RadioThemeData(
                FillColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Selected) ? selectedColor : null))
        };
        using var harness = new WidgetRenderHarness(
            BuildThemed(
                new RadioListTile<string>(
                    value: "a",
                    groupValue: "a",
                    selected: true,
                    title: new Text("Leading selected radio"),
                    secondary: new Icon(Icons.InfoOutline),
                    onChanged: _ => { }),
                theme));

        harness.Pump(new Size(360, 180));

        var title = FindParagraphByText(harness.RenderView, "Leading selected radio");
        Assert.NotNull(title);
        Assert.Equal(selectedColor, Assert.IsType<SolidColorBrush>(title!.Foreground).Color);

        string secondaryGlyph = char.ConvertFromUtf32(Icons.InfoOutline.CodePoint);
        var tile = Assert.IsType<RenderListTile>(FindDescendant<RenderListTile>(harness.RenderView));
        Assert.Contains(
            FindDescendants<RenderCustomPaint>(tile.Leading),
            paint => Math.Abs(paint.Size.Width - 40.0) < 0.001);
        Assert.Contains(
            FindDescendants<RenderParagraph>(tile.Trailing),
            paragraph => paragraph.PlainText == secondaryGlyph);
    }

    [Fact]
    public void RadioListTile_MergedSemantics_ExposeCheckedEnabledAndTap()
    {
        using var harness = new WidgetRenderHarness(
            BuildThemed(new RadioListTile<string>(
                value: "a",
                groupValue: "a",
                title: new Text("Checked radio"),
                onChanged: _ => { })));

        var semantics = harness.PumpAndGetSemantics(new Size(360, 180));
        var checkedNode = FindFirstSemanticsNode(
            semantics!,
            node => node.Flags.HasFlag(SemanticsFlags.IsChecked));

        Assert.NotNull(checkedNode);
        Assert.True(checkedNode!.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.True(checkedNode.Actions.HasFlag(SemanticsActions.Tap));
    }

    [Fact]
    public void RadioListTile_ScaleFactor_AppliesCenteredPaintTransform()
    {
        const double scaleFactor = 1.4;
        using var harness = new WidgetRenderHarness(
            BuildThemed(new RadioListTile<string>(
                value: "a",
                groupValue: "b",
                radioScaleFactor: scaleFactor,
                title: new Text("Scaled radio"),
                onChanged: _ => { })));

        harness.Pump(new Size(360, 180));

        var transform = FindDescendant<RenderTransform>(harness.RenderView);
        Assert.NotNull(transform);
        var expected = new Matrix(scaleFactor, 0, 0, scaleFactor, 0, 0);
        Assert.Equal(expected, transform!.Transform);
        Assert.Equal(Alignment.Center, transform.Alignment);
    }

    [Fact]
    public void RadioListTile_AdaptiveIOS_BuildsCupertinoBranch()
    {
        using var harness = new WidgetRenderHarness(
            BuildThemed(
                RadioListTile<string>.Adaptive(
                    value: "a",
                    groupValue: "a",
                    useCupertinoCheckmarkStyle: true,
                    title: new Text("Adaptive radio tile"),
                    onChanged: _ => { }),
                ThemeData.Light with { Platform = TargetPlatform.IOS }));

        harness.Pump(new Size(360, 180));

        Assert.NotNull(FindParagraphByText(harness.RenderView, "Adaptive radio tile"));
        Assert.NotNull(FindDescendant<RenderStrokeGlyph>(harness.RenderView));
    }

    [Fact]
    public void RadioListTile_AdaptiveIOS_RegistersWithRadioGroupForKeyboardTraversal()
    {
        string? changed = null;
        var firstFocus = new FocusNode();
        var secondFocus = new FocusNode();
        using var harness = new WidgetRenderHarness(
            BuildThemed(
                new RadioGroup<string>(
                    groupValue: "a",
                    onChanged: value => changed = value,
                    child: new Column(
                        children:
                        [
                            RadioListTile<string>.Adaptive(
                                value: "a",
                                title: new Text("Adaptive A"),
                                focusNode: firstFocus),
                            RadioListTile<string>.Adaptive(
                                value: "b",
                                title: new Text("Adaptive B"),
                                focusNode: secondFocus),
                        ])),
                ThemeData.Light with { Platform = TargetPlatform.IOS }));

        harness.Pump(new Size(360, 180));
        Assert.True(firstFocus.RequestFocus());
        Assert.True(FocusManager.Instance.HandleKeyEvent(new KeyEvent("ArrowRight", isDown: true)));

        Assert.Equal("b", changed);
        Assert.True(secondFocus.HasFocus);
        firstFocus.Dispose();
        secondFocus.Dispose();
    }

    [Fact]
    public void ExpansionTile_Constructor_RejectsBaselineChildrenAlignment()
    {
        Assert.Throws<ArgumentException>(() => new ExpansionTile(
            title: new Text("Invalid"),
            expandedCrossAxisAlignment: CrossAxisAlignment.Baseline));
    }

    [Fact]
    public void ExpansionTile_InitiallyExpanded_ShowsBodyAndExpandedSemantics()
    {
        using var harness = new WidgetRenderHarness(
            BuildThemed(new ExpansionTile(
                title: new Text("Initially expanded"),
                initiallyExpanded: true,
                children: [new Text("Expanded body")])));

        var semantics = harness.PumpAndGetSemantics(new Size(360, 240));

        Assert.NotNull(FindParagraphByText(harness.RenderView, "Expanded body"));
        var expandedNode = FindFirstSemanticsNode(
            semantics!,
            node => node.OnTapHint == DefaultMaterialLocalizations.Instance.ExpansionTileExpandedTapHint);
        Assert.NotNull(expandedNode);
        Assert.Equal(
            DefaultMaterialLocalizations.Instance.ExpansionTileExpandedTapHint,
            expandedNode!.OnTapHint);
    }

    [Fact]
    public void ExpansionTile_ControllerExpand_InvokesCallbackAndAnimatesBodyAndArrow()
    {
        using var controller = new ExpansibleController();
        bool? callbackValue = null;
        using var harness = new WidgetRenderHarness(
            BuildThemed(new ExpansionTile(
                title: new Text("Controller tile"),
                controller: controller,
                onExpansionChanged: value => callbackValue = value,
                children: [new SizedBox(height: 40, child: new Text("Controller body"))])));

        harness.Pump(new Size(360, 240));
        Assert.Null(FindParagraphByText(harness.RenderView, "Controller body"));

        controller.Expand();
        harness.Pump(new Size(360, 240));
        Assert.True(callbackValue);

        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.10));
        harness.Pump(new Size(360, 240));
        var midAlign = FindDescendants<RenderAlign>(harness.RenderView)
            .FirstOrDefault(align => align.HeightFactor is > 0 and < 1);
        Assert.NotNull(midAlign);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.30));
        harness.Pump(new Size(360, 240));
        Assert.NotNull(FindParagraphByText(harness.RenderView, "Controller body"));
        var arrowTransform = FindDescendants<RenderTransform>(harness.RenderView).FirstOrDefault();
        Assert.NotNull(arrowTransform);
        Assert.InRange(arrowTransform!.Transform.M11, -1.01, -0.99);
    }

    [Fact]
    public void ExpansionTile_CollapseWithoutMaintainState_RemovesBodyAfterAnimation()
    {
        using var controller = new ExpansibleController();
        controller.Expand();
        using var harness = new WidgetRenderHarness(
            BuildThemed(new ExpansionTile(
                title: new Text("Disposable body tile"),
                controller: controller,
                maintainState: false,
                children: [new Text("Disposable body")])));

        harness.Pump(new Size(360, 240));
        Assert.NotNull(FindParagraphByText(harness.RenderView, "Disposable body"));

        controller.Collapse();
        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.30));
        harness.Pump(new Size(360, 240));

        Assert.Null(FindParagraphByText(harness.RenderView, "Disposable body"));
    }

    [Fact]
    public void ExpansionTile_MaintainState_KeepsOffstageBodyMountedWhenCollapsed()
    {
        using var harness = new WidgetRenderHarness(
            BuildThemed(new ExpansionTile(
                title: new Text("Maintained tile"),
                maintainState: true,
                children: [new Text("Maintained body")])));

        harness.Pump(new Size(360, 240));

        Assert.NotNull(FindParagraphByText(harness.RenderView, "Maintained body"));
        var offstage = FindDescendant<RenderOffstage>(harness.RenderView);
        Assert.NotNull(offstage);
        Assert.True(offstage!.Offstage);
    }

    [Fact]
    public void ExpansionTile_ThemeAndWidgetOverrides_ResolveBackgroundAndAffinity()
    {
        var themeBackground = Color.Parse("#FFE3F4E8");
        var widgetBackground = Color.Parse("#FFFFE8D6");
        using var controller = new ExpansibleController();
        controller.Expand();
        var theme = ThemeData.Light with
        {
            ExpansionTileTheme = new ExpansionTileThemeData(
                BackgroundColor: themeBackground,
                IconColor: Colors.ForestGreen),
            ListTileTheme = new ListTileThemeData(
                ControlAffinity: ListTileControlAffinity.Leading)
        };
        using var harness = new WidgetRenderHarness(
            BuildThemed(
                new ExpansionTile(
                    title: new Text("Themed expansion"),
                    controller: controller,
                    backgroundColor: widgetBackground,
                    children: [new Text("Themed body")]),
                theme));

        harness.Pump(new Size(360, 240));

        var background = FindDescendants<RenderDecoratedBox>(harness.RenderView)
            .FirstOrDefault(box => DecorationColor(box) == widgetBackground);
        Assert.NotNull(background);
        string iconGlyph = char.ConvertFromUtf32(Icons.ExpandMore.CodePoint);
        Assert.NotNull(FindParagraphByText(harness.RenderView, iconGlyph));
    }

    [Fact]
    public void ExpansionTile_LocalTheme_AppliesCollapsedBackgroundAndIconColor()
    {
        var collapsedBackground = Color.Parse("#FFFFF0D8");
        var collapsedIcon = Color.Parse("#FF3F51B5");
        using var harness = new WidgetRenderHarness(
            BuildThemed(new ExpansionTileTheme(
                data: new ExpansionTileThemeData(
                    CollapsedBackgroundColor: collapsedBackground,
                    CollapsedIconColor: collapsedIcon),
                child: new ExpansionTile(
                    title: new Text("Local themed expansion"),
                    children: [new Text("Local body")]))));

        harness.Pump(new Size(360, 200));

        Assert.Contains(
            FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => DecorationColor(box) == collapsedBackground);
        var icon = FindParagraphByText(harness.RenderView, char.ConvertFromUtf32(Icons.ExpandMore.CodePoint));
        Assert.NotNull(icon);
        Assert.Equal(collapsedIcon, Assert.IsType<SolidColorBrush>(icon!.Foreground).Color);
    }

    [Fact]
    public void ExpansionTile_DisabledTap_DoesNotExpand()
    {
        using var controller = new ExpansibleController();
        using var harness = new WidgetRenderHarness(
            BuildThemed(new ExpansionTile(
                title: new Text("Disabled expansion"),
                controller: controller,
                enabled: false,
                children: [new Text("Hidden body")])));

        harness.Pump(new Size(360, 200));
        Tap(harness.RenderView, new Point(140, 28), pointer: 704);

        Assert.False(controller.IsExpanded);
        Assert.Null(FindParagraphByText(harness.RenderView, "Hidden body"));
    }

    [Fact]
    public void ExpansionTile_SourceDefaultsAndHeaderParameters_AreForwarded()
    {
        var statesController = new WidgetStatesController();
        var density = new VisualDensity(-2.0, -1.0);
        var splash = Color.Parse("#FF123456");
        var tile = new ExpansionTile(
            title: new Text("Forwarded header"),
            dense: true,
            splashColor: splash,
            visualDensity: density,
            enableFeedback: false,
            statesController: statesController);

        Assert.False(tile.InitiallyExpanded);
        Assert.False(tile.MaintainState);
        Assert.True(tile.ShowTrailingIcon);
        Assert.True(tile.Enabled);
        Assert.False(tile.InternalAddSemanticForOnTap);
        Assert.False(tile.EnableFeedback);
        Assert.True(new ExpansionTile(title: new Text("Defaults")).EnableFeedback);

        using var harness = new WidgetRenderHarness(BuildThemed(tile));
        harness.Pump(new Size(360, 160));

        ListTile header = Assert.Single(harness.FindWidgets<ListTile>());
        Assert.True(header.Dense);
        Assert.Equal(splash, header.SplashColor);
        Assert.Equal(density, header.VisualDensity);
        Assert.False(header.EnableFeedback);
        Assert.Same(statesController, header.StatesController);
        statesController.Dispose();
    }

    [Fact]
    public void ExpansionTile_M2AndM3Defaults_ReadDirectThemeRoles()
    {
        Color primary = Color.Parse("#FF2255AA");
        Color onSurface = Color.Parse("#FF112233");
        Color onSurfaceVariant = Color.Parse("#FF445566");
        Color m2Title = Color.Parse("#FF778899");
        Color m2Unselected = Color.Parse("#FF667788");
        ColorScheme scheme = ThemeData.Light.ColorScheme.CopyWith(
            primary: primary,
            onSurface: onSurface,
            onSurfaceVariant: onSurfaceVariant);
        ThemeData m3 = ThemeData.Light with
        {
            UseMaterial3 = true,
            ColorScheme = scheme,
        };
        using (var harness = new WidgetRenderHarness(BuildThemed(
                   new ExpansionTile(
                       title: new Text("M3 title"),
                       children: [new Text("M3 body")]),
                   m3)))
        {
            harness.Pump(new Size(360, 180));
            Assert.Equal(onSurface, ParagraphColor(harness.RenderView, "M3 title"));
            Assert.Equal(
                onSurfaceVariant,
                ParagraphColor(harness.RenderView, char.ConvertFromUtf32(Icons.ExpandMore.CodePoint)));
        }

        ThemeData m2 = ThemeData.Light with
        {
            UseMaterial3 = false,
            ColorScheme = scheme,
            UnselectedWidgetColor = m2Unselected,
            TextTheme = ThemeData.Light.TextTheme.CopyWith(
                titleMedium: ThemeData.Light.TextTheme.TitleMedium.CopyWith(color: m2Title)),
        };
        using var m2Harness = new WidgetRenderHarness(BuildThemed(
            new ExpansionTile(
                title: new Text("M2 title"),
                children: [new Text("M2 body")]),
            m2));
        m2Harness.Pump(new Size(360, 180));
        Assert.Equal(m2Title, ParagraphColor(m2Harness.RenderView, "M2 title"));
        Assert.Equal(
            m2Unselected,
            ParagraphColor(m2Harness.RenderView, char.ConvertFromUtf32(Icons.ExpandMore.CodePoint)));
    }

    [Fact]
    public void ExpansionTile_DefaultAndCustomShapes_UseSourceBordersAndClip()
    {
        Color divider = Color.Parse("#FF884422");
        ThemeData theme = ThemeData.Light with { DividerColor = divider };
        using (var harness = new WidgetRenderHarness(BuildThemed(
                   new ExpansionTile(
                       title: new Text("Default border"),
                       initiallyExpanded: true),
                   theme)))
        {
            harness.Pump(new Size(360, 160));
            ShapeDecoration decoration = FindDescendants<RenderDecoratedBox>(harness.RenderView)
                .Select(box => box.DecorationValue)
                .OfType<ShapeDecoration>()
                .First(value => value.Shape is Plumix.Rendering.Border);
            Assert.Equal(divider, ((Plumix.Rendering.Border)decoration.Shape).Top.Color);
            Assert.Equal(divider, ((Plumix.Rendering.Border)decoration.Shape).Bottom.Color);
            Assert.Equal(BorderStyle.None, ((Plumix.Rendering.Border)decoration.Shape).Left.Style);
            Assert.Equal(BorderStyle.None, ((Plumix.Rendering.Border)decoration.Shape).Right.Style);
            Assert.Equal(new Thickness(0, 1, 0, 1), decoration.Shape.Dimensions.Resolve(TextDirection.Ltr));
        }

        ShapeBorder shape = new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(12.0));
        using var customHarness = new WidgetRenderHarness(BuildThemed(
            new ExpansionTile(
                title: new Text("Custom shape"),
                shape: shape,
                collapsedShape: shape,
                clipBehavior: Clip.None)));
        customHarness.Pump(new Size(360, 160));
        Plumix.Material.Material material = Assert.Single(
            customHarness.FindWidgets<Plumix.Material.Material>());
        Assert.Equal(shape, material.Shape);
        Assert.Equal(Clip.None, material.ClipBehavior);
    }

    [Fact]
    public void ExpansionTile_ShowTrailingIconFalse_LeavesHeaderTrailingEmpty()
    {
        using var harness = new WidgetRenderHarness(BuildThemed(new ExpansionTile(
            title: new Text("No arrow"),
            showTrailingIcon: false,
            tilePadding: EdgeInsetsGeometry.Zero)));

        harness.Pump(new Size(360, 100));

        Assert.Null(Assert.Single(harness.FindWidgets<ListTile>()).Trailing);
        Assert.Null(FindParagraphByText(harness.RenderView, char.ConvertFromUtf32(Icons.ExpandMore.CodePoint)));
    }

    [Fact]
    public void ExpansionTileTheme_CopyLerpAndCapture_AreSourceShaped()
    {
        var first = new ExpansionTileThemeData(
            TilePadding: EdgeInsetsGeometry.DirectionalOnly(start: 4.0),
            ExpandedAlignment: AlignmentDirectional.CenterStart,
            Shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(4.0)),
            ClipBehavior: Clip.None,
            ExpansionAnimationStyle: AnimationStyle.NoAnimation);
        ExpansionTileThemeData copy = first.CopyWith(
            backgroundColor: Colors.ForestGreen,
            shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(12.0)));
        ExpansionTileThemeData lerped = ExpansionTileThemeData.Lerp(first, copy, 0.5)!;

        Assert.Equal(Colors.ForestGreen, copy.BackgroundColor);
        Assert.Equal(8.0, ShapeBorderGeometry.ResolveRadius(lerped.Shape).Radius);
        Assert.Equal(first.TilePadding, lerped.TilePadding);
        Assert.Equal(Clip.None, lerped.ClipBehavior);

        var local = new ExpansionTileTheme(first, new SizedBox());
        Widget wrapped = local.Wrap(default, new Text("captured"));
        Assert.Equal(first, Assert.IsType<ExpansionTileTheme>(wrapped).Data);
    }

    [Fact]
    public void Expansible_PageStorage_RestoresExpansionFromPageStorageKey()
    {
        var bucket = new PageStorageBucket();
        var key = new PageStorageKey<string>("restored-expansion");
        using (var controller = new ExpansibleController())
        using (var firstHarness = new WidgetRenderHarness(BuildThemed(
                   new PageStorage(
                       bucket,
                       new ExpansionTile(
                           key: key,
                           controller: controller,
                           title: new Text("Stored tile"),
                           children: [new Text("Stored body")])))))
        {
            firstHarness.Pump(new Size(360, 180));
            controller.Expand();
            firstHarness.Pump(new Size(360, 180));
            Assert.True(controller.IsExpanded);
        }

        using var restoredController = new ExpansibleController();
        using var restoredHarness = new WidgetRenderHarness(BuildThemed(
            new PageStorage(
                bucket,
                new ExpansionTile(
                    key: key,
                    controller: restoredController,
                    title: new Text("Stored tile"),
                    children: [new Text("Stored body")]))));
        restoredHarness.Pump(new Size(360, 180));

        Assert.True(restoredController.IsExpanded);
        Assert.NotNull(FindParagraphByText(restoredHarness.RenderView, "Stored body"));
    }

    [Fact]
    public void ExpansibleController_OfAndMaybeOf_ResolveNearestController()
    {
        using var controller = new ExpansibleController();
        ExpansibleController? resolved = null;
        ExpansibleController? optional = null;
        using var harness = new WidgetRenderHarness(BuildThemed(new Expansible(
            controller: controller,
            maintainState: true,
            headerBuilder: (context, animation) => new Text("Header"),
            bodyBuilder: (context, animation) => new Builder(probeContext =>
            {
                resolved = ExpansibleController.Of(probeContext);
                optional = ExpansibleController.MaybeOf(probeContext);
                return new Text("Body");
            }))));

        harness.Pump(new Size(360, 120));

        Assert.Same(controller, resolved);
        Assert.Same(controller, optional);
    }

    [Theory]
    [InlineData(ListTileControlAffinity.Platform, false)]
    [InlineData(ListTileControlAffinity.Trailing, false)]
    [InlineData(ListTileControlAffinity.Leading, true)]
    public void ExpansionTile_ControlAffinity_PlacesDefaultArrowInSourceSlot(
        ListTileControlAffinity affinity,
        bool expectedLeading)
    {
        using var harness = new WidgetRenderHarness(BuildThemed(new ExpansionTile(
            title: new Text("Affinity"),
            controlAffinity: affinity)));

        harness.Pump(new Size(360, 100));

        ListTile header = Assert.Single(harness.FindWidgets<ListTile>());
        Assert.Equal(expectedLeading, header.Leading is RotationTransition);
        Assert.Equal(!expectedLeading, header.Trailing is RotationTransition);
    }

    [Fact]
    public void ExpansionTile_NoAnimationAndDisabledProgrammaticExpansion_MatchSource()
    {
        using var controller = new ExpansibleController();
        using var harness = new WidgetRenderHarness(BuildThemed(new ExpansionTile(
            title: new Text("Instant tile"),
            controller: controller,
            enabled: false,
            expansionAnimationStyle: AnimationStyle.NoAnimation,
            children: [new Text("Instant body")])));

        harness.Pump(new Size(360, 140));
        controller.Expand();
        harness.Pump(new Size(360, 140));

        Assert.True(controller.IsExpanded);
        Assert.NotNull(FindParagraphByText(harness.RenderView, "Instant body"));
        Assert.Contains(
            FindDescendants<RenderAlign>(harness.RenderView),
            align => align.HeightFactor == 1.0);
    }

    [Fact]
    public void ExpansionTile_DirectionalBodyGeometry_ResolvesInRtl()
    {
        using var harness = new WidgetRenderHarness(BuildThemed(new Directionality(
            TextDirection.Rtl,
            new ExpansionTile(
                title: new Text("RTL tile"),
                initiallyExpanded: true,
                expandedAlignment: AlignmentDirectional.CenterStart,
                childrenPadding: EdgeInsetsGeometry.DirectionalOnly(start: 10.0, end: 20.0),
                children: [new SizedBox(width: 40, height: 20)]))));

        harness.Pump(new Size(360, 160));

        Assert.Contains(
            FindDescendants<RenderPadding>(harness.RenderView),
            padding => padding.Padding.Left == 20.0 && padding.Padding.Right == 10.0);
        Assert.Contains(
            FindDescendants<RenderAlign>(harness.RenderView),
            align => align.Alignment == Alignment.CenterRight);
    }

    [Fact]
    public void ExpansionTile_NonAndroidExpansion_AnnouncesOppositeActionState()
    {
        if (PlatformDefaults.TargetPlatform is TargetPlatform.Android or TargetPlatform.IOS)
        {
            return;
        }

        SemanticsService.ResetForTests();
        using var controller = new ExpansibleController();
        SemanticsAnnouncement? announcement = null;
        SemanticsService.AnnouncementRequested += value => announcement = value;
        using var harness = new WidgetRenderHarness(BuildThemed(new ExpansionTile(
            title: new Text("Announced tile"),
            controller: controller)));
        harness.Pump(new Size(360, 100));

        controller.Expand();

        Assert.NotNull(announcement);
        Assert.Equal(DefaultMaterialLocalizations.Instance.CollapsedHint, announcement!.Message);
        SemanticsService.ResetForTests();
    }

    private static Widget BuildThemed(Widget child, ThemeData? theme = null)
    {
        return new Theme(
            data: theme ?? ThemeData.Light,
            child: new MediaQuery(
                data: new MediaQueryData(Size: new Size(320, 800)),
                child: new SizedBox(width: 320, child: child)));
    }

    private static void Tap(RenderView renderView, Point position, int pointer)
    {
        var timestamp = DateTime.UtcNow;
        GestureBinding.Instance.HandlePointerEvent(
            renderView,
            new PointerDownEvent(
                pointer,
                PointerDeviceKind.Mouse,
                position,
                PointerButtons.Primary,
                timestamp));
        GestureBinding.Instance.HandlePointerEvent(
            renderView,
            new PointerUpEvent(
                pointer,
                PointerDeviceKind.Mouse,
                position,
                PointerButtons.None,
                timestamp.AddMilliseconds(20)));
    }

    private static RenderParagraph? FindParagraphByText(RenderObject? root, string text)
    {
        return FindDescendants<RenderParagraph>(root)
            .FirstOrDefault(paragraph => paragraph.PlainText == text);
    }

    private static Color ParagraphColor(RenderObject? root, string text)
    {
        RenderParagraph paragraph = Assert.IsType<RenderParagraph>(FindParagraphByText(root, text));
        return Assert.IsType<SolidColorBrush>(paragraph.Foreground).Color;
    }

    private static Color? DecorationColor(RenderDecoratedBox box)
    {
        return box.DecorationValue switch
        {
            BoxDecoration decoration => decoration.Color,
            ShapeDecoration decoration => decoration.Color,
            _ => null,
        };
    }

    private static T? FindDescendant<T>(RenderObject? root) where T : RenderObject
    {
        return FindDescendants<T>(root).FirstOrDefault();
    }

    private static IEnumerable<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var results = new List<T>();
        CollectDescendants(root, results);
        return results;
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

    private static List<RenderObject> ImmediateChildren(RenderObject root)
    {
        var children = new List<RenderObject>();
        root.VisitChildren(children.Add);
        return children;
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
            Widget appRoot = new Actions(
                actions: new Dictionary<Type, FlutterAction>
                {
                    [typeof(VoidCallbackIntent)] = new VoidCallbackAction(),
                },
                child: rootWidget);
            _rootElement = new HarnessRootElement(RenderView, appRoot);
            _rootElement.Attach(_owner);
            _rootElement.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public IReadOnlyList<T> FindWidgets<T>() where T : Widget
        {
            var widgets = new List<T>();
            CollectWidgets(_rootElement, widgets);
            return widgets;
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
            return _pipeline.SemanticsOwner.RootNode;
        }

        public void Dispose() => _rootElement.Unmount();

        private static void CollectWidgets<T>(Element element, List<T> widgets) where T : Widget
        {
            if (element.Widget is T typed)
            {
                widgets.Add(typed);
            }

            element.VisitChildren(child => CollectWidgets(child, widgets));
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
                if (_child is not null)
                {
                    visitor(_child);
                }
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot)
            {
                _renderView.Child = Assert.IsAssignableFrom<RenderBox>(child);
            }

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (ReferenceEquals(_renderView.Child, child))
                {
                    _renderView.Child = null;
                }
            }

            internal override void Unmount()
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
}
