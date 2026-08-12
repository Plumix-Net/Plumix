using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialInputDecoratorTests
{
    private const double SubtextGapM3 = 4.0;
    private const double InputGapM3 = 4.0;
    private const double FinalLabelScale = 0.75;

    [Fact]
    public void InputDecoration_ValidatesExclusiveSlotsAndCollapsedDefaults()
    {
        Assert.Throws<ArgumentException>(() => new InputDecoration(label: new Text("A"), labelText: "B"));
        Assert.Throws<ArgumentException>(() => new InputDecoration(prefix: new Text("A"), prefixText: "B"));
        Assert.Throws<ArgumentException>(() => new InputDecoration(error: new Text("A"), errorText: "B"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InputDecoration(errorMaxLines: 0));

        InputDecoration collapsed = InputDecoration.Collapsed("Hint");
        Assert.True(collapsed.IsCollapsed);
        Assert.False(collapsed.IsDense);
        Assert.False(collapsed.Filled);
        Assert.Same(InputBorder.None, collapsed.Border);
        Assert.Equal(EdgeInsetsGeometry.Zero, collapsed.ContentPadding);
        Assert.True(collapsed.MaintainHintSize);
        Assert.False(collapsed.MaintainLabelSize);
    }

    [Fact]
    public void InputDecoration_ApplyDefaultsFillsOnlyNullFieldsFromTheme()
    {
        var theme = new InputDecorationThemeData(
            HelperMaxLines: 2,
            ErrorMaxLines: 3,
            FloatingLabelBehavior: FloatingLabelBehavior.Never,
            FloatingLabelAlignment: FloatingLabelAlignment.Center,
            IsDense: true,
            ContentPadding: EdgeInsetsGeometry.All(1.0),
            Filled: true,
            Border: InputBorder.None,
            AlignLabelWithHint: true,
            VisualDensity: VisualDensity.Compact);

        InputDecoration applied = new InputDecoration().ApplyDefaults(theme);
        Assert.Equal(2, applied.HelperMaxLines);
        Assert.Equal(3, applied.ErrorMaxLines);
        Assert.Equal(FloatingLabelBehavior.Never, applied.FloatingLabelBehavior);
        Assert.Equal(FloatingLabelAlignment.Center, applied.FloatingLabelAlignment);
        Assert.True(applied.IsDense);
        Assert.Equal(EdgeInsetsGeometry.All(1.0), applied.ContentPadding);
        Assert.True(applied.Filled);
        Assert.Same(InputBorder.None, applied.Border);
        Assert.True(applied.AlignLabelWithHint);
        Assert.Equal(VisualDensity.Compact, applied.VisualDensity);

        InputDecoration explicitValues = new InputDecoration(
            isDense: false,
            helperMaxLines: 9,
            floatingLabelAlignment: FloatingLabelAlignment.Start).ApplyDefaults(theme);
        Assert.False(explicitValues.IsDense);
        Assert.Equal(9, explicitValues.HelperMaxLines);
        Assert.Equal(FloatingLabelAlignment.Start, explicitValues.FloatingLabelAlignment);
    }

    [Fact]
    public void InputDecorator_Material3FilledGeometryFollowsContentPaddingAndFloatingLabelHeight()
    {
        using var harness = new DecoratorHarness(Decorator(new InputDecoration(
            labelText: "Label",
            helperText: "Helper",
            counterText: "0/10",
            filled: true)));
        harness.Pump();
        RenderDecoration decorator = harness.Decorator;

        // Material 3 filled + underline content padding is fromSTEB(12, 8, 12, 8) and the input gap is 4.
        Rect input = DecoratorHarness.RectOf(decorator.InputBox);
        Rect label = DecoratorHarness.RectOf(decorator.LabelBox);
        Assert.Equal(12.0 + InputGapM3, input.Left, precision: 6);
        Assert.Equal(12.0 + InputGapM3, label.Left, precision: 6);

        // floatingLabelHeight = textScale * (4 + 0.75 * labelFontSize); the label style is bodyLarge (16).
        const double floatingLabelHeight = 4.0 + (0.75 * 16.0);
        double containerHeight = decorator.ContainerBox!.Size.Height;
        Assert.Equal(8.0 + floatingLabelHeight + input.Height + 8.0, containerHeight, precision: 6);
        Assert.Equal(800.0, decorator.Size.Width, precision: 6);

        Rect helper = DecoratorHarness.RectOf(decorator.HelperErrorBox);
        Rect counter = DecoratorHarness.RectOf(decorator.CounterBox);
        Assert.Equal(12.0 + InputGapM3, helper.Left, precision: 6);
        Assert.Equal(800.0 - 12.0 - InputGapM3, counter.Right, precision: 6);
        Assert.Equal(containerHeight + SubtextGapM3, helper.Top, precision: 6);
        Assert.Equal(containerHeight + SubtextGapM3, counter.Top, precision: 6);
        Assert.Equal(
            containerHeight + Math.Max(helper.Height, counter.Height) + SubtextGapM3,
            decorator.Size.Height,
            precision: 6);
    }

    [Fact]
    public void InputDecorator_ClampsContainerToTheMinimumInteractiveDimension()
    {
        using var harness = new DecoratorHarness(Decorator(
            new InputDecoration(filled: true),
            child: new SizedBox(width: 100, height: 4)));
        harness.Pump();
        Assert.Equal(48.0, harness.Decorator.ContainerBox!.Size.Height, precision: 6);

        using var dense = new DecoratorHarness(Decorator(
            new InputDecoration(filled: true, isDense: true),
            child: new SizedBox(width: 100, height: 4)));
        dense.Pump();
        // isDense drops the minimum to the input height: 4 + 4 top + 4 bottom.
        Assert.Equal(12.0, dense.Decorator.ContainerBox!.Size.Height, precision: 6);
    }

    [Fact]
    public void InputDecorator_CollapsedDecorationHasNoPaddingAndNoBorder()
    {
        using var harness = new DecoratorHarness(Decorator(
            InputDecoration.Collapsed("Hint"),
            isEmpty: true,
            child: new SizedBox(width: 100, height: 24)));
        harness.Pump();
        RenderDecoration decorator = harness.Decorator;

        Assert.Equal(0.0, DecoratorHarness.OffsetOf(decorator.InputBox).X, precision: 6);
        Assert.Equal(0.0, DecoratorHarness.OffsetOf(decorator.InputBox).Y, precision: 6);
        Assert.Equal(
            Math.Max(24.0, decorator.HintBox!.Size.Height),
            decorator.ContainerBox!.Size.Height,
            precision: 6);
        Assert.Same(InputBorder.None, Painter(harness).Border);
    }

    [Fact]
    public void InputDecorator_DenseOutlinedDecorationUsesOutlineContentPadding()
    {
        using var harness = new DecoratorHarness(Decorator(new InputDecoration(
            labelText: "Label",
            isDense: true,
            border: new OutlineInputBorder())));
        harness.Pump();
        RenderDecoration decorator = harness.Decorator;
        Rect container = DecoratorHarness.RectOf(decorator.ContainerBox);
        Rect input = DecoratorHarness.RectOf(decorator.InputBox);

        // Outline gapPadding (4) is the Material 3 input gap; dense outline padding is fromSTEB(12,16,12,8).
        Assert.Equal(container.Left + 12.0 + 4.0, input.Left, precision: 6);
        Assert.Equal(container.Right - 12.0 - 4.0, input.Right, precision: 6);
        // An outline border floats the label out of the container, so no floating label height is reserved.
        Assert.Equal(16.0 + input.Height + 8.0, container.Height, precision: 6);
    }

    [Fact]
    public void InputDecorator_MirrorsSlotPositionsInRightToLeft()
    {
        using var harness = new DecoratorHarness(
            Decorator(new InputDecoration(labelText: "Label", helperText: "Helper", filled: true)),
            textDirection: TextDirection.Rtl);
        harness.Pump();
        RenderDecoration decorator = harness.Decorator;

        Rect input = DecoratorHarness.RectOf(decorator.InputBox);
        Rect helper = DecoratorHarness.RectOf(decorator.HelperErrorBox);
        Assert.Equal(800.0 - 12.0 - InputGapM3, input.Right, precision: 6);
        Assert.Equal(800.0 - 12.0 - InputGapM3, helper.Right, precision: 6);
    }

    [Fact]
    public void InputDecorator_PrefixAndSuffixIconsAreFortyEightSquareCenteredAndGapped()
    {
        using var harness = new DecoratorHarness(Decorator(new InputDecoration(
            labelText: "Label",
            filled: true,
            prefixIcon: new SizedBox(width: 8, height: 8),
            suffixIcon: new SizedBox(width: 8, height: 8))));
        harness.Pump();
        RenderDecoration decorator = harness.Decorator;

        Rect container = DecoratorHarness.RectOf(decorator.ContainerBox);
        Rect prefixIcon = DecoratorHarness.RectOf(decorator.PrefixIconBox);
        Rect suffixIcon = DecoratorHarness.RectOf(decorator.SuffixIconBox);
        Rect input = DecoratorHarness.RectOf(decorator.InputBox);

        Assert.Equal(new Size(48.0, 48.0), prefixIcon.Size);
        Assert.Equal(new Size(48.0, 48.0), suffixIcon.Size);
        Assert.Equal(container.Center.Y, prefixIcon.Center.Y, precision: 6);
        Assert.Equal(container.Center.Y, suffixIcon.Center.Y, precision: 6);
        Assert.Equal(0.0, prefixIcon.Left, precision: 6);
        Assert.Equal(800.0, suffixIcon.Right, precision: 6);
        Assert.Equal(prefixIcon.Right + InputGapM3, input.Left, precision: 6);
        Assert.Equal(suffixIcon.Left - InputGapM3, input.Right, precision: 6);
    }

    [Fact]
    public void InputDecorator_IconSlotSitsOutsideTheContainer()
    {
        using var harness = new DecoratorHarness(Decorator(new InputDecoration(
            labelText: "Label",
            filled: true,
            icon: new SizedBox(width: 24, height: 24))));
        harness.Pump();
        RenderDecoration decorator = harness.Decorator;
        Rect container = DecoratorHarness.RectOf(decorator.ContainerBox);

        // The icon reserves its own width plus the 16dp directional end padding.
        Assert.Equal(24.0 + 16.0, container.Left, precision: 6);
        Assert.Equal(800.0, container.Right, precision: 6);
        Assert.Equal(container.Left + 12.0 + InputGapM3, DecoratorHarness.RectOf(decorator.InputBox).Left, 6);
    }

    [Fact]
    public void InputDecorator_HintIsVerticallyCoLocatedWithInput()
    {
        using var harness = new DecoratorHarness(Decorator(
            new InputDecoration(hintText: "Hint", labelText: "Label", filled: true),
            isEmpty: true,
            child: new Text("value", fontSize: 16)));
        harness.Pump();
        RenderDecoration decorator = harness.Decorator;
        Rect hint = DecoratorHarness.RectOf(decorator.HintBox);
        Rect input = DecoratorHarness.RectOf(decorator.InputBox);

        // Both are baseline-aligned onto the decorator's single text baseline.
        double hintBaseline = decorator.HintBox!.GetDistanceToBaseline(TextBaseline.Alphabetic, onlyReal: true)!.Value;
        double inputBaseline =
            decorator.InputBox!.GetDistanceToBaseline(TextBaseline.Alphabetic, onlyReal: true)!.Value;
        Assert.Equal(input.Top + inputBaseline, hint.Top + hintBaseline, precision: 6);
        Assert.Equal(input.Left, hint.Left, precision: 6);
        Assert.Equal(input.Width, hint.Width, precision: 6);
    }

    [Fact]
    public void InputDecorator_FloatingLabelScalesAndLiftsAboveTheOutline()
    {
        using var inline = new DecoratorHarness(Decorator(
            new InputDecoration(labelText: "Label", border: new OutlineInputBorder()),
            isEmpty: true));
        inline.Pump();
        double inlineLabelTop = DecoratorHarness.RectOf(inline.Decorator.LabelBox).Top;

        using var floating = new DecoratorHarness(Decorator(new InputDecoration(
            labelText: "Label",
            border: new OutlineInputBorder(),
            floatingLabelBehavior: FloatingLabelBehavior.Always)));
        floating.Pump();
        RenderDecoration decorator = floating.Decorator;
        Assert.NotNull(decorator.LabelTransform);
        Matrix transform = decorator.LabelTransform!.Value;

        // scaleX/scaleY are the first and fourth matrix components.
        Assert.Equal(FinalLabelScale, transform.M11, precision: 6);
        Assert.Equal(FinalLabelScale, transform.M22, precision: 6);

        double labelHeight = decorator.LabelBox!.Size.Height;
        BorderSide side = Painter(floating).Border.BorderSide;
        double expectedFloatingY = (-labelHeight * FinalLabelScale / 2.0) - (side.StrokeOffset / 2.0);
        Assert.Equal(expectedFloatingY, transform.M32, precision: 6);
        Assert.True(expectedFloatingY < inlineLabelTop);
    }

    [Fact]
    public void InputDecorator_BorderGapTracksLabelWidthAndAlignment()
    {
        using var start = new DecoratorHarness(Decorator(new InputDecoration(
            labelText: "Label",
            border: new OutlineInputBorder(),
            floatingLabelBehavior: FloatingLabelBehavior.Always)));
        start.Pump();
        RenderDecoration startDecorator = start.Decorator;
        InputBorderPainter startPainter = Painter(start);
        double labelWidth = startDecorator.LabelBox!.Size.Width;

        Assert.Equal(labelWidth * FinalLabelScale, startPainter.GapExtent, precision: 6);
        Assert.NotNull(startPainter.GapStart);
        Assert.Equal(
            DecoratorHarness.OffsetOf(startDecorator.LabelBox).X,
            startPainter.GapStart!.Value,
            precision: 6);
        Assert.Equal(1.0, startPainter.GapPercentage, precision: 6);

        using var center = new DecoratorHarness(Decorator(new InputDecoration(
            labelText: "Label",
            border: new OutlineInputBorder(),
            floatingLabelAlignment: FloatingLabelAlignment.Center,
            floatingLabelBehavior: FloatingLabelBehavior.Always)));
        center.Pump();
        RenderDecoration centerDecorator = center.Decorator;
        InputBorderPainter centerPainter = Painter(center);
        double containerWidth = centerDecorator.ContainerBox!.Size.Width;
        double floatWidth = centerDecorator.LabelBox!.Size.Width * FinalLabelScale;
        Assert.NotNull(centerPainter.GapStart);
        Assert.Equal(
            (containerWidth / 2.0) - (floatWidth / 2.0),
            centerPainter.GapStart!.Value,
            precision: 6);
    }

    [Fact]
    public void InputDecorator_BorderGapIsClearedWithoutALabel()
    {
        using var harness = new DecoratorHarness(Decorator(
            new InputDecoration(border: new OutlineInputBorder())));
        harness.Pump();
        InputBorderPainter painter = Painter(harness);
        Assert.Null(painter.GapStart);
        Assert.Equal(0.0, painter.GapExtent, precision: 6);
    }

    [Fact]
    public void InputDecorator_TextAlignVerticalPositionsInputWhenExpanded()
    {
        double InputTop(TextAlignVertical? alignment)
        {
            using var harness = new DecoratorHarness(
                new SizedBox(
                    width: 800,
                    height: 200,
                    child: new InputDecorator(
                        decoration: new InputDecoration(filled: true),
                        expands: true,
                        textAlignVertical: alignment,
                        child: new SizedBox(width: 100, height: 20))));
            harness.Pump();
            return DecoratorHarness.RectOf(harness.Decorator.InputBox).Top;
        }

        double top = InputTop(TextAlignVertical.Top);
        double center = InputTop(TextAlignVertical.Center);
        double bottom = InputTop(TextAlignVertical.Bottom);

        Assert.Equal(8.0, top, precision: 6);
        Assert.Equal((200.0 - 20.0) / 2.0, center, precision: 6);
        Assert.Equal(200.0 - 8.0 - 20.0, bottom, precision: 6);
        Assert.Equal(top, InputTop(null), precision: 6);
    }

    [Fact]
    public void InputDecorator_VisualDensityAdjustsContainerHeight()
    {
        using var standard = new DecoratorHarness(Decorator(
            new InputDecoration(labelText: "Label", filled: true, isDense: true)));
        standard.Pump();
        double standardHeight = standard.Decorator.ContainerBox!.Size.Height;

        using var compact = new DecoratorHarness(Decorator(new InputDecoration(
            labelText: "Label",
            filled: true,
            isDense: true,
            visualDensity: VisualDensity.Compact)));
        compact.Pump();
        double compactHeight = compact.Decorator.ContainerBox!.Size.Height;

        // VisualDensity.compact has vertical -2, i.e. a base size adjustment of -8.
        Assert.Equal(standardHeight - 8.0, compactHeight, precision: 6);
    }

    [Fact]
    public void InputDecorator_IntrinsicHeightIncludesSubtextAndDensity()
    {
        using var harness = new DecoratorHarness(Decorator(new InputDecoration(
            labelText: "Label",
            helperText: "Helper",
            filled: true)));
        harness.Pump();
        RenderDecoration decorator = harness.Decorator;

        double intrinsic = decorator.GetMinIntrinsicHeight(800.0);
        Assert.Equal(decorator.GetMaxIntrinsicHeight(800.0), intrinsic, precision: 6);
        Assert.True(intrinsic >= 48.0);
        Assert.True(intrinsic >= decorator.HelperErrorBox!.Size.Height + SubtextGapM3);
    }

    [Fact]
    public void InputDecorator_ErrorReplacesHelperAndUsesErrorColors()
    {
        var theme = ThemeData.Light;
        using var harness = new DecoratorHarness(Decorator(new InputDecoration(
            labelText: "Label",
            helperText: "Helper",
            errorText: "Invalid",
            filled: true)));
        harness.Pump();

        List<RenderParagraph> paragraphs = DecoratorHarness.FindAll<RenderParagraph>(harness.RenderView);
        Assert.Contains(paragraphs, value => value.Text == "Invalid");
        Assert.DoesNotContain(paragraphs, value => value.Text == "Helper");
        Assert.Equal(theme.ErrorColor, Painter(harness).Border.BorderSide.Color);
    }

    [Fact]
    public void InputDecorator_Material3DefaultsResolveIndicatorAndFillPerState()
    {
        var theme = ThemeData.Light;

        InputBorderPainter Resolve(bool focused = false, bool hovering = false, bool enabled = true)
        {
            using var harness = new DecoratorHarness(new InputDecorator(
                decoration: new InputDecoration(labelText: "Label", filled: true, enabled: enabled),
                isFocused: focused,
                isHovering: hovering,
                child: new Text("value")));
            harness.Pump();
            return Painter(harness);
        }

        InputBorderPainter enabledPainter = Resolve();
        Assert.IsType<UnderlineInputBorder>(enabledPainter.Border);
        Assert.Equal(theme.OnSurfaceVariantColor, enabledPainter.Border.BorderSide.Color);
        Assert.Equal(1.0, enabledPainter.Border.BorderSide.Width, precision: 6);
        Assert.Equal(theme.SurfaceContainerHighestColor, enabledPainter.FillColor);

        InputBorderPainter focusedPainter = Resolve(focused: true);
        Assert.Equal(theme.PrimaryColor, focusedPainter.Border.BorderSide.Color);
        Assert.Equal(2.0, focusedPainter.Border.BorderSide.Width, precision: 6);

        InputBorderPainter hoveredPainter = Resolve(hovering: true);
        Assert.Equal(theme.OnSurfaceColor, hoveredPainter.Border.BorderSide.Color);
        Assert.Equal(
            InputDecoratorDefaults.AlphaBlend(theme.HoverColor, theme.SurfaceContainerHighestColor),
            hoveredPainter.BlendedColor);

        InputBorderPainter disabledPainter = Resolve(enabled: false, hovering: true);
        Assert.Equal(
            InputDecoratorDefaults.WithOpacity(theme.OnSurfaceColor, 0.38),
            disabledPainter.Border.BorderSide.Color);
        Assert.Equal(
            InputDecoratorDefaults.WithOpacity(theme.OnSurfaceColor, 0.04),
            disabledPainter.FillColor);
    }

    [Fact]
    public void InputDecorator_Material3OutlineBorderUsesOutlineTokens()
    {
        var theme = ThemeData.Light;
        using var harness = new DecoratorHarness(Decorator(
            new InputDecoration(labelText: "Label", border: new OutlineInputBorder())));
        harness.Pump();
        InputBorderPainter painter = Painter(harness);
        Assert.IsType<OutlineInputBorder>(painter.Border);
        Assert.Equal(theme.OutlineColor, painter.Border.BorderSide.Color);
        Assert.Equal(1.0, painter.Border.BorderSide.Width, precision: 6);
        Assert.Equal(Colors.Transparent, painter.FillColor);
    }

    [Fact]
    public void InputDecorator_ResolvesPerStateBorderSlotsBeforeDefaults()
    {
        var errorBorder = new OutlineInputBorder(new BorderSide(Colors.Orange, 3.0));
        var focusedErrorBorder = new OutlineInputBorder(new BorderSide(Colors.Purple, 5.0));
        var disabledBorder = new UnderlineInputBorder(new BorderSide(Colors.SlateGray, 2.0));

        using var errored = new DecoratorHarness(new InputDecorator(
            decoration: new InputDecoration(
                errorText: "Invalid",
                errorBorder: errorBorder,
                focusedErrorBorder: focusedErrorBorder,
                disabledBorder: disabledBorder),
            child: new Text("value")));
        errored.Pump();
        Assert.Same(errorBorder, Painter(errored).Border);

        using var focusedError = new DecoratorHarness(new InputDecorator(
            decoration: new InputDecoration(
                errorText: "Invalid",
                errorBorder: errorBorder,
                focusedErrorBorder: focusedErrorBorder),
            isFocused: true,
            child: new Text("value")));
        focusedError.Pump();
        Assert.Same(focusedErrorBorder, Painter(focusedError).Border);

        using var disabled = new DecoratorHarness(new InputDecorator(
            decoration: new InputDecoration(disabledBorder: disabledBorder, enabled: false),
            child: new Text("value")));
        disabled.Pump();
        Assert.Same(disabledBorder, Painter(disabled).Border);
    }

    [Fact]
    public void InputDecorator_StateInputBorderResolvesOnlyFromTheBorderSlot()
    {
        MaterialState receivedStates = MaterialState.None;
        MaterialStateOutlineInputBorder border = MaterialStateOutlineInputBorder.ResolveWith(states =>
        {
            receivedStates = states;
            return new OutlineInputBorder(new BorderSide(Colors.OrangeRed, 3.0));
        });

        using var harness = new DecoratorHarness(new InputDecorator(
            decoration: new InputDecoration(errorText: "Invalid", border: border),
            isFocused: true,
            isHovering: true,
            child: new Text("value")));
        harness.Pump();

        Assert.Equal(MaterialState.Focused | MaterialState.Hovered | MaterialState.Error, receivedStates);
        InputBorderPainter painter = Painter(harness);
        Assert.Equal(Colors.OrangeRed, painter.Border.BorderSide.Color);
        // A resolved state border is used verbatim: no default indicator side is applied on top.
        Assert.Equal(3.0, painter.Border.BorderSide.Width, precision: 6);
    }

    [Fact]
    public void InputDecorator_DisabledStateMasksHover()
    {
        MaterialState receivedStates = MaterialState.None;
        MaterialStateUnderlineInputBorder border = MaterialStateUnderlineInputBorder.ResolveWith(states =>
        {
            receivedStates = states;
            return new UnderlineInputBorder(new BorderSide(Colors.SlateGray, 2.0));
        });

        using var harness = new DecoratorHarness(new InputDecorator(
            decoration: new InputDecoration(border: border, enabled: false),
            isHovering: true,
            isEmpty: true,
            child: new Text("")));
        harness.Pump();

        Assert.Equal(MaterialState.Disabled, receivedStates);
        Assert.Equal(Colors.SlateGray, Painter(harness).Border.BorderSide.Color);
    }

    [Fact]
    public void InputDecorator_ConstraintsOverrideTheAmbientThemeConstraints()
    {
        var theme = ThemeData.Light with
        {
            InputDecorationTheme = new InputDecorationThemeData(
                Constraints: new BoxConstraints(0, 300, 0, 40)),
        };

        using var themed = new DecoratorHarness(Decorator(new InputDecoration(filled: true)), theme);
        themed.Pump();
        Assert.Equal(300.0, themed.Decorator.Size.Width, precision: 6);
        Assert.Equal(40.0, themed.Decorator.Size.Height, precision: 6);

        using var overridden = new DecoratorHarness(
            Decorator(new InputDecoration(filled: true, constraints: new BoxConstraints(0, 200, 0, 32))),
            theme);
        overridden.Pump();
        Assert.Equal(200.0, overridden.Decorator.Size.Width, precision: 6);
        Assert.Equal(32.0, overridden.Decorator.Size.Height, precision: 6);
    }

    [Fact]
    public void InputDecorator_AmbientThemeContentPaddingIsApplied()
    {
        var theme = ThemeData.Light with
        {
            InputDecorationTheme = new InputDecorationThemeData(
                ContentPadding: EdgeInsetsGeometry.DirectionalOnly(start: 11, top: 13, end: 15, bottom: 17)),
        };

        using var harness = new DecoratorHarness(
            Decorator(new InputDecoration(labelText: "Label", filled: true)),
            theme);
        harness.Pump();
        RenderDecoration decorator = harness.Decorator;
        Rect input = DecoratorHarness.RectOf(decorator.InputBox);

        Assert.Equal(11.0 + InputGapM3, input.Left, precision: 6);
        Assert.Equal(800.0 - 15.0 - InputGapM3, input.Right, precision: 6);
        Assert.Equal(
            13.0 + 4.0 + (0.75 * 16.0) + input.Height + 17.0,
            decorator.ContainerBox!.Size.Height,
            precision: 6);
    }

    [Fact]
    public void InputDecorator_HitTestingReachesEverySlot()
    {
        using var harness = new DecoratorHarness(Decorator(new InputDecoration(
            labelText: "Label",
            helperText: "Helper",
            filled: true,
            prefixIcon: new SizedBox(width: 8, height: 8))));
        harness.Pump();
        RenderDecoration decorator = harness.Decorator;

        var result = new BoxHitTestResult();
        Assert.True(decorator.HitTest(result, DecoratorHarness.RectOf(decorator.PrefixIconBox).Center));
        Assert.Contains(result.Path, entry => ReferenceEquals(entry.Target, decorator.PrefixIconBox));
    }

    [Fact]
    public void InputBorder_EqualityAndHashCodesCoverTheDeclaredFields()
    {
        Assert.Equal(
            new OutlineInputBorder(gapPadding: 32.0),
            new OutlineInputBorder(gapPadding: 32.0));
        Assert.NotEqual(
            new OutlineInputBorder(gapPadding: 32.0),
            new OutlineInputBorder(gapPadding: 33.0));
        Assert.Equal(
            new OutlineInputBorder(gapPadding: 32.0).GetHashCode(),
            new OutlineInputBorder(gapPadding: 32.0).GetHashCode());

        Assert.Equal(
            new UnderlineInputBorder(borderRadius: BorderRadius.Circular(5.0)),
            new UnderlineInputBorder(borderRadius: BorderRadius.Circular(5.0)));
        Assert.NotEqual(
            new UnderlineInputBorder(borderRadius: BorderRadius.Circular(5.0)),
            new UnderlineInputBorder(borderRadius: BorderRadius.Circular(6.0)));

        Assert.Throws<ArgumentOutOfRangeException>(() => new OutlineInputBorder(gapPadding: -1.0));
    }

    [Fact]
    public void InputBorder_DimensionsFollowStrokeInsetAndWidth()
    {
        var outline = new OutlineInputBorder(new BorderSide(Colors.Black, 4.0));
        // strokeAlign defaults to inside (-1), so strokeInset == width.
        Assert.Equal(new Thickness(4.0), outline.Dimensions);

        var centered = new OutlineInputBorder(
            new BorderSide(Colors.Black, 4.0, BorderStyle.Solid, BorderSide.StrokeAlignCenter));
        Assert.Equal(new Thickness(2.0), centered.Dimensions);

        var underline = new UnderlineInputBorder(new BorderSide(Colors.Black, 3.0));
        Assert.Equal(new Thickness(0, 0, 0, 3.0), underline.Dimensions);
        Assert.Equal(default, InputBorder.None.Dimensions);
        Assert.False(InputBorder.None.IsOutline);
        Assert.Same(InputBorder.None, InputBorder.None.CopyWith(new BorderSide(Colors.Red, 4.0)));
    }

    [Fact]
    public void InputBorder_ScaleAndLerpFollowFlutterQuirks()
    {
        var outline = new OutlineInputBorder(
            new BorderSide(Colors.Black, 4.0),
            BorderRadius.Circular(8.0),
            gapPadding: 6.0);
        var scaled = (OutlineInputBorder)outline.Scale(0.5);
        Assert.Equal(2.0, scaled.BorderSide.Width, precision: 6);
        Assert.Equal(4.0, scaled.BorderRadius.TopLeft, precision: 6);
        Assert.Equal(3.0, scaled.GapPadding, precision: 6);

        // UnderlineInputBorder.scale drops the radius, reverting to the default top radii.
        var underline = new UnderlineInputBorder(
            new BorderSide(Colors.Black, 4.0),
            BorderRadius.Circular(9.0));
        var scaledUnderline = (UnderlineInputBorder)underline.Scale(0.5);
        Assert.Equal(2.0, scaledUnderline.BorderSide.Width, precision: 6);
        Assert.Equal(4.0, scaledUnderline.BorderRadius.TopLeft, precision: 6);
        Assert.Equal(0.0, scaledUnderline.BorderRadius.BottomLeft, precision: 6);

        var target = new OutlineInputBorder(
            new BorderSide(Colors.Black, 8.0),
            BorderRadius.Circular(16.0),
            gapPadding: 10.0);
        var mid = (OutlineInputBorder)InputBorder.Lerp(outline, target, 0.5);
        Assert.Equal(6.0, mid.BorderSide.Width, precision: 6);
        Assert.Equal(12.0, mid.BorderRadius.TopLeft, precision: 6);
        // gapPadding is taken from the source border verbatim rather than interpolated.
        Assert.Equal(6.0, mid.GapPadding, precision: 6);

        Assert.Same(outline, InputBorder.Lerp(outline, target, 0.0));
        Assert.Same(target, InputBorder.Lerp(outline, target, 1.0));
    }

    [Fact]
    public void InputBorder_LerpAcrossKindsScalesThroughTheMidpoint()
    {
        var underline = new UnderlineInputBorder(new BorderSide(Colors.Black, 4.0));
        var outline = new OutlineInputBorder(new BorderSide(Colors.Black, 4.0));

        Assert.IsType<UnderlineInputBorder>(InputBorder.Lerp(underline, outline, 0.25));
        Assert.IsType<OutlineInputBorder>(InputBorder.Lerp(underline, outline, 0.75));
        Assert.Equal(2.0, InputBorder.Lerp(underline, outline, 0.25).BorderSide.Width, precision: 6);
        Assert.Equal(2.0, InputBorder.Lerp(underline, outline, 0.75).BorderSide.Width, precision: 6);
    }

    [Fact]
    public void InputBorder_AnimatesBetweenBordersWhenTheResolvedBorderChanges()
    {
        var decoration = new InputDecoration(labelText: "Label", filled: true);
        using var harness = new DecoratorHarness(new InputDecorator(
            decoration: decoration,
            child: new Text("value")));
        harness.Pump();
        Assert.Equal(1.0, Painter(harness).Border.BorderSide.Width, precision: 6);

        harness.Update(new InputDecorator(
            decoration: decoration,
            isFocused: true,
            child: new Text("value")));
        // The border tween starts at the previous border, so the first focused frame is still 1dp wide.
        Assert.Equal(1.0, Painter(harness).Border.BorderSide.Width, precision: 6);
    }

    private static Widget Decorator(
        InputDecoration decoration,
        bool isEmpty = false,
        Widget? child = null) => new InputDecorator(
        decoration: decoration,
        isEmpty: isEmpty,
        child: child ?? new Text("value"));

    private static InputBorderPainter Painter(DecoratorHarness harness)
    {
        RenderCustomPaint? paint = DecoratorHarness.Find<RenderCustomPaint>(harness.Decorator);
        Assert.NotNull(paint);
        return Assert.IsType<InputBorderPainter>(paint!.Painter);
    }
}
