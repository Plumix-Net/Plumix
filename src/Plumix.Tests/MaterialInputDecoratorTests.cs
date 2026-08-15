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
            helperMaxLines: 2,
            errorMaxLines: 3,
            floatingLabelBehavior: FloatingLabelBehavior.Never,
            floatingLabelAlignment: FloatingLabelAlignment.Center,
            isDense: true,
            contentPadding: EdgeInsetsGeometry.All(1.0),
            filled: true,
            border: InputBorder.None,
            alignLabelWithHint: true,
            visualDensity: VisualDensity.Compact);

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
        Matrix4 transform = decorator.LabelTransform!;

        // scaleX/scaleY are the first and fourth matrix components.
        Assert.Equal(FinalLabelScale, transform[0], precision: 6);
        Assert.Equal(FinalLabelScale, transform[5], precision: 6);

        double labelHeight = decorator.LabelBox!.Size.Height;
        BorderSide side = Painter(floating).Border.BorderSide;
        double expectedFloatingY = (-labelHeight * FinalLabelScale / 2.0) - (side.StrokeOffset / 2.0);
        Assert.Equal(expectedFloatingY, transform[13], precision: 6);
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
        Assert.Contains(paragraphs, value => value.PlainText == "Invalid");
        Assert.DoesNotContain(paragraphs, value => value.PlainText == "Helper");
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
                constraints: new BoxConstraints(0, 300, 0, 40)),
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
                contentPadding: EdgeInsetsGeometry.DirectionalOnly(start: 11, top: 13, end: 15, bottom: 17)),
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
        // _NoInputBorder.copyWith ignores its argument and returns another empty border.
        Assert.Equal(InputBorder.None, InputBorder.None.CopyWith(new BorderSide(Colors.Red, 4.0)));
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
        var mid = (OutlineInputBorder)ShapeBorder.Lerp(outline, target, 0.5)!;
        Assert.Equal(6.0, mid.BorderSide.Width, precision: 6);
        Assert.Equal(12.0, mid.BorderRadius.TopLeft, precision: 6);
        // gapPadding is taken from the source border verbatim rather than interpolated.
        Assert.Equal(6.0, mid.GapPadding, precision: 6);

        // ShapeBorder.Lerp does not short-circuit the endpoints, so the interpolated border keeps the
        // gapPadding of the border it started from even at t == 1.
        var start = (OutlineInputBorder)ShapeBorder.Lerp(outline, target, 0.0)!;
        Assert.Equal(outline.BorderSide, start.BorderSide);
        Assert.Equal(outline.BorderRadius, start.BorderRadius);
        var end = (OutlineInputBorder)ShapeBorder.Lerp(outline, target, 1.0)!;
        Assert.Equal(target.BorderSide, end.BorderSide);
        Assert.Equal(target.BorderRadius, end.BorderRadius);
        Assert.Equal(outline.GapPadding, end.GapPadding, precision: 6);
    }

    [Fact]
    public void InputBorder_LerpAcrossKindsSwitchesAtTheMidpoint()
    {
        var underline = new UnderlineInputBorder(new BorderSide(Colors.Black, 4.0));
        var outline = new OutlineInputBorder(new BorderSide(Colors.Black, 4.0));

        // Neither border can interpolate to the other kind, so ShapeBorder.Lerp falls back to the
        // hard switch at the midpoint instead of scaling either border out.
        Assert.Same(underline, ShapeBorder.Lerp(underline, outline, 0.25));
        Assert.Same(outline, ShapeBorder.Lerp(underline, outline, 0.75));
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

    [Fact]
    public void InputDecorator_FloatingLabelRectFollowsThePaintTransform()
    {
        using var inline = new DecoratorHarness(Decorator(
            new InputDecoration(labelText: "Label", border: new OutlineInputBorder()),
            isEmpty: true));
        inline.Pump();
        RenderBox inlineLabel = inline.Decorator.LabelBox!;
        Point inlineTopLeft = inlineLabel.LocalToGlobal(default);
        Size inlineSize = inlineLabel.Size;

        using var floating = new DecoratorHarness(Decorator(new InputDecoration(
            labelText: "Label",
            border: new OutlineInputBorder(),
            floatingLabelBehavior: FloatingLabelBehavior.Always)));
        floating.Pump();
        RenderDecoration decorator = floating.Decorator;
        RenderBox label = decorator.LabelBox!;

        // Flutter asserts the same shape through tester.getRect, which resolves applyPaintTransform:
        // the label keeps its left edge, is lifted above the outline and painted at 0.75 scale.
        Point floatingTopLeft = label.LocalToGlobal(default);
        Point floatingBottomRight = label.LocalToGlobal(new Point(label.Size.Width, label.Size.Height));
        BorderSide side = Painter(floating).Border.BorderSide;
        double expectedTop = (-label.Size.Height * FinalLabelScale / 2.0) - (side.StrokeOffset / 2.0);

        Assert.Equal(inlineTopLeft.X, floatingTopLeft.X, precision: 6);
        Assert.Equal(expectedTop, floatingTopLeft.Y, precision: 6);
        Assert.True(floatingTopLeft.Y < inlineTopLeft.Y);
        Assert.Equal(inlineSize.Width * FinalLabelScale, floatingBottomRight.X - floatingTopLeft.X, precision: 6);
        Assert.Equal(inlineSize.Height * FinalLabelScale, floatingBottomRight.Y - floatingTopLeft.Y, precision: 6);

        // The inverse maps a painted point back into the label's own coordinate space.
        Assert.Equal(new Point(0, 0), label.GlobalToLocal(floatingTopLeft));

        // Every other slot keeps the plain parent-data offset.
        RenderBox input = decorator.InputBox!;
        Assert.Equal(DecoratorHarness.OffsetOf(input), input.LocalToGlobal(default));
    }

    [Fact]
    public void InputDecorator_PrefixAndSuffixFormSiblingSemanticsNodes()
    {
        using var harness = new DecoratorHarness(Decorator(
            new InputDecoration(prefixText: "Prefix", suffixText: "Suffix"),
            child: new Text("value")));
        SemanticsNode root = harness.PumpSemantics();

        // Flutter: "TextField prefix and suffix create a sibling node" — the three parts stay apart
        // instead of merging into one concatenated label.
        Assert.Contains(root.Children, static node => node.Label == "Prefix");
        Assert.Contains(root.Children, static node => node.Label == "Suffix");
        Assert.Contains(root.Children, static node => node.Label == "value");
        Assert.DoesNotContain(root.Children, static node => node.Label == "Prefix value Suffix");
    }

    [Fact]
    public void InputDecorator_PrefixAndSuffixIconsFormTheirOwnSiblingNodes()
    {
        using var harness = new DecoratorHarness(Decorator(
            new InputDecoration(
                prefixIcon: new Semantics(label: "Leading", child: new SizedBox(width: 24, height: 24)),
                suffixIcon: new Semantics(label: "Trailing", child: new SizedBox(width: 24, height: 24))),
            child: new Text("value")));
        SemanticsNode root = harness.PumpSemantics();

        Assert.Contains(root.Children, static node => node.Label == "Leading");
        Assert.Contains(root.Children, static node => node.Label == "Trailing");
    }

    [Fact]
    public void InputDecorator_AffixSortOrderIsScopedPerDecoratorAndOnlyAppliedWhenNeeded()
    {
        using var withAffixes = new DecoratorHarness(Decorator(
            new InputDecoration(prefixText: "Prefix", suffixText: "Suffix"),
            child: new Text("value")));
        SemanticsNode root = withAffixes.PumpSemantics();

        var keys = root.Children
            .Where(static node => node.SortKey is OrdinalSortKey)
            .Select(static node => (OrdinalSortKey)node.SortKey!)
            .ToList();
        Assert.Equal(3, keys.Count);
        Assert.Equal([0.0, 1.0, 2.0], keys.Select(static key => key.Order).Order());
        Assert.Single(keys.Select(static key => key.GroupName).Distinct());
        Assert.All(keys, static key => Assert.False(string.IsNullOrEmpty(key.GroupName)));

        // With no affix at all the decorator does not need a traversal order.
        using var plain = new DecoratorHarness(Decorator(
            new InputDecoration(labelText: "Label"),
            child: new Text("value")));
        SemanticsNode plainRoot = plain.PumpSemantics();
        Assert.All(plainRoot.Children, static node => Assert.Null(node.SortKey));
    }

    [Fact]
    public void ShapedInputBorder_WrapsAnArbitraryShapeAndOpensTheLabelGap()
    {
        var shape = new StadiumBorder();
        var border = new ShapedInputBorder(shape, new BorderSide(Colors.Black, 2.0), gapPadding: 6.0);

        Assert.True(border.IsOutline);
        Assert.Equal(EdgeInsets.All(2.0), border.Dimensions);
        Assert.Equal(shape.PreferPaintInterior, border.PreferPaintInterior);
        Assert.Equal(shape.GetOuterPath(new Rect(0, 0, 40, 20)).GetBounds(),
            border.GetOuterPath(new Rect(0, 0, 40, 20)).GetBounds());

        var scaled = (ShapedInputBorder)border.Scale(0.5);
        Assert.Equal(1.0, scaled.BorderSide.Width, precision: 6);
        Assert.Equal(3.0, scaled.GapPadding, precision: 6);

        var target = new ShapedInputBorder(shape, new BorderSide(Colors.Black, 6.0), gapPadding: 10.0);
        var mid = (ShapedInputBorder)ShapeBorder.Lerp(border, target, 0.5)!;
        Assert.Equal(4.0, mid.BorderSide.Width, precision: 6);
        // gapPadding follows the source border, matching OutlineInputBorder's quirk.
        Assert.Equal(6.0, mid.GapPadding, precision: 6);

        ShapedInputBorder copied = border.CopyWith(null, new CircleBorder(), gapPadding: null);
        Assert.IsType<CircleBorder>(copied.Shape);
        Assert.Equal(6.0, copied.GapPadding, precision: 6);

        // The gap cuts the top edge of the outline out of the painted path.
        var probe = new PaintingContext(new OffsetLayer());
        border.Paint(probe, new Rect(0, 0, 200, 56), gapStart: 40.0, gapExtent: 30.0, gapPercentage: 1.0,
            textDirection: TextDirection.Ltr);
        border.Paint(probe, new Rect(0, 0, 200, 56), gapStart: null, textDirection: TextDirection.Ltr);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ShapedInputBorder(shape, gapPadding: -1.0));
    }

    [Fact]
    public void UnderlineInputBorder_RoundedBottomCornersPaintThroughTheNonUniformRing()
    {
        var border = new UnderlineInputBorder(
            new BorderSide(Colors.Black, 1.0),
            BorderRadius.Circular(12.0));

        // The inner edge is the rect deflated by the bottom side, which is what the ring is filled
        // between; Flutter asserts the same outer/inner pair through paintNonUniformBorder.
        Plumix.UI.Path inner = border.GetInnerPath(new Rect(0, 0, 100, 100));
        Assert.Equal(new Rect(0, 0, 100, 99), inner.GetBounds());
        Assert.Equal(new Rect(0, 0, 100, 100), border.GetOuterPath(new Rect(0, 0, 100, 100)).GetBounds());

        var probe = new PaintingContext(new OffsetLayer());
        var squared = new UnderlineInputBorder(new BorderSide(Colors.Black, 2.0), BorderRadius.Zero);
        squared.Paint(probe, new Rect(0, 0, 100, 100), gapStart: null);
        Assert.Equal(new Thickness(0, 0, 0, 2.0), squared.Dimensions);

        // A border with no style paints nothing at all.
        var invisible = new UnderlineInputBorder(BorderSide.None);
        var emptyProbe = new PaintingContext(new OffsetLayer());
        invisible.Paint(emptyProbe, new Rect(0, 0, 100, 100), gapStart: null);
        Assert.Equal(EdgeInsetsGeometry.Zero, invisible.Dimensions);
    }

    [Fact]
    public void InputDecorationThemeData_MergeNeverOverridesTheNonNullableFields()
    {
        var overrideTheme = new InputDecorationThemeData(
            helperMaxLines: 7,
            hintMaxLines: 5,
            contentPadding: EdgeInsetsGeometry.All(3.0),
            floatingLabelBehavior: FloatingLabelBehavior.Never,
            floatingLabelAlignment: FloatingLabelAlignment.Center,
            isDense: true,
            isCollapsed: true,
            filled: true,
            alignLabelWithHint: true,
            visualDensity: VisualDensity.Compact);

        InputDecorationThemeData merged = new InputDecorationThemeData().Merge(overrideTheme);

        // Nullable fields are taken from `other`...
        Assert.Equal(7, merged.HelperMaxLines);
        Assert.Equal(5, merged.HintMaxLines);
        Assert.Equal(EdgeInsetsGeometry.All(3.0), merged.ContentPadding);
        Assert.Equal(VisualDensity.Compact, merged.VisualDensity);

        // ...while Flutter deliberately omits the six non-nullable ones from the merge call.
        Assert.Equal(FloatingLabelBehavior.Auto, merged.FloatingLabelBehavior);
        Assert.Equal(FloatingLabelAlignment.Start, merged.FloatingLabelAlignment);
        Assert.False(merged.IsDense);
        Assert.False(merged.IsCollapsed);
        Assert.False(merged.Filled);
        Assert.False(merged.AlignLabelWithHint);

        Assert.Same(overrideTheme, overrideTheme.Merge(null));
    }

    [Fact]
    public void InputDecorationThemeData_CopyWithAndEqualityFollowTheSourceFieldList()
    {
        var theme = new InputDecorationThemeData(
            fillColor: Colors.Red,
            iconColor: Colors.Blue,
            helperMaxLines: 2);

        InputDecorationThemeData copy = theme.CopyWith(fillColor: Colors.Green);
        Assert.Equal(Colors.Green, copy.FillColor!.DefaultValue);
        // Copying one field leaves the rest alone, and the original is never mutated.
        Assert.Equal(Colors.Blue, copy.IconColor!.DefaultValue);
        Assert.Equal(2, copy.HelperMaxLines);
        Assert.Equal(Colors.Red, theme.FillColor!.DefaultValue);

        Assert.Equal(new InputDecorationThemeData(), new InputDecorationThemeData().CopyWith());
        Assert.Equal(
            new InputDecorationThemeData().GetHashCode(),
            new InputDecorationThemeData().CopyWith().GetHashCode());
        Assert.True(new InputDecorationThemeData(isDense: true) != new InputDecorationThemeData());

        // Dart's `==` opens with a runtimeType check, so a defaults subclass is never equal to a
        // plain InputDecorationThemeData — not even the empty one.
        InputDecorationThemeData defaults = InputDecoratorDefaults.Resolve(ThemeData.Light);
        Assert.NotEqual(new InputDecorationThemeData(), defaults);
        Assert.NotEqual(defaults, new InputDecorationThemeData());
    }

    [Fact]
    public void InputDecorationTheme_LegacyFieldSurfaceProjectsThroughData()
    {
        var fieldBased = new InputDecorationTheme(
            child: new SizedBox(),
            helperMaxLines: 4,
            isDense: true,
            filled: true,
            border: InputBorder.None);

        // The obsolete per-field constructor normalizes null to the source defaults at construction.
        Assert.Equal(4, fieldBased.HelperMaxLines);
        Assert.True(fieldBased.IsDense);
        Assert.True(fieldBased.Filled);
        Assert.Equal(FloatingLabelBehavior.Auto, fieldBased.FloatingLabelBehavior);
        Assert.False(fieldBased.AlignLabelWithHint);

        InputDecorationThemeData projected = fieldBased.Data;
        Assert.Equal(4, projected.HelperMaxLines);
        Assert.True(projected.IsDense);
        Assert.True(projected.Filled);
        Assert.Same(InputBorder.None, projected.Border);

        // A data-backed theme forwards every getter to the data instead.
        var dataBased = new InputDecorationTheme(
            new InputDecorationThemeData(helperMaxLines: 9, isCollapsed: true),
            new SizedBox());
        Assert.Equal(9, dataBased.HelperMaxLines);
        Assert.True(dataBased.IsCollapsed);

        // copyWith is field-backed: it keeps neither the data argument nor the child.
        InputDecorationTheme copied = dataBased.CopyWith(helperMaxLines: 3);
        Assert.Equal(3, copied.HelperMaxLines);
        Assert.True(copied.IsCollapsed);
        Assert.NotSame(dataBased.Child, copied.Child);

        // merge omits the same six non-nullable fields as InputDecorationThemeData.merge.
        InputDecorationTheme merged = new InputDecorationTheme(child: new SizedBox())
            .Merge(new InputDecorationTheme(child: new SizedBox(), helperMaxLines: 6, isDense: true));
        Assert.Equal(6, merged.HelperMaxLines);
        Assert.False(merged.IsDense);
    }

    [Fact]
    public void InputDecorationTheme_RejectsDataAndFieldArgumentsTogether()
    {
        Assert.Throws<ArgumentException>(() => new InputDecorationTheme(
            data: new InputDecorationThemeData(),
            child: new SizedBox(),
            isDense: true));
    }

    [Fact]
    public void InputDecoration_ApplyDefaultsAcceptsTheThemeWidget()
    {
        var theme = new InputDecorationTheme(child: new SizedBox(), helperMaxLines: 5, filled: true);
        InputDecoration applied = new InputDecoration().ApplyDefaults(theme);
        Assert.Equal(5, applied.HelperMaxLines);
        Assert.True(applied.Filled);

        // The theme's six non-nullable fields make these non-null after applyDefaults, which is what
        // lets the decorator read them without a fallback.
        Assert.NotNull(applied.IsDense);
        Assert.NotNull(applied.IsCollapsed);
        Assert.NotNull(applied.AlignLabelWithHint);
        Assert.NotNull(applied.FloatingLabelBehavior);
        Assert.NotNull(applied.FloatingLabelAlignment);
    }

    [Fact]
    public void InputDecorator_Material3LabelStyleResolvesPerState()
    {
        ColorScheme colors = ThemeData.Light.ColorScheme;

        Color LabelColor(bool focused = false, bool hovering = false, bool enabled = true, bool error = false)
        {
            using var harness = new DecoratorHarness(new InputDecorator(
                decoration: new InputDecoration(
                    labelText: "Label",
                    filled: true,
                    enabled: enabled,
                    errorText: error ? "Invalid" : null),
                isFocused: focused,
                isHovering: hovering,
                child: new Text("value")));
            harness.Pump();
            return StyleOf(harness, "Label").Color!.Value;
        }

        Assert.Equal(colors.OnSurfaceVariant, LabelColor());
        Assert.Equal(colors.OnSurfaceVariant, LabelColor(hovering: true));
        Assert.Equal(colors.Primary, LabelColor(focused: true));
        // focused wins over hovered for InputDecorator, the inverse of most M3 components.
        Assert.Equal(colors.Primary, LabelColor(focused: true, hovering: true));
        Assert.Equal(InputDecoratorDefaults.WithOpacity(colors.OnSurface, 0.38), LabelColor(enabled: false));
        Assert.Equal(colors.Error, LabelColor(error: true));
        Assert.Equal(colors.Error, LabelColor(error: true, focused: true));
        Assert.Equal(colors.OnErrorContainer, LabelColor(error: true, hovering: true));
        Assert.Equal(colors.Error, LabelColor(error: true, focused: true, hovering: true));
    }

    [Fact]
    public void InputDecorator_Material3SubtextAndHintStylesResolvePerState()
    {
        ColorScheme colors = ThemeData.Light.ColorScheme;
        Color disabled = InputDecoratorDefaults.WithOpacity(colors.OnSurface, 0.38);

        using var enabled = new DecoratorHarness(Decorator(
            new InputDecoration(hintText: "Hint", helperText: "Helper", counterText: "0/10", filled: true),
            isEmpty: true,
            child: new SizedBox()));
        enabled.Pump();
        Assert.Equal(colors.OnSurfaceVariant, StyleOf(enabled, "Hint").Color);
        Assert.Equal(colors.OnSurfaceVariant, StyleOf(enabled, "Helper").Color);
        Assert.Equal(colors.OnSurfaceVariant, StyleOf(enabled, "0/10").Color);

        using var off = new DecoratorHarness(Decorator(
            new InputDecoration(
                hintText: "Hint", helperText: "Helper", counterText: "0/10", filled: true, enabled: false),
            isEmpty: true,
            child: new SizedBox()));
        off.Pump();
        Assert.Equal(disabled, StyleOf(off, "Hint").Color);
        Assert.Equal(disabled, StyleOf(off, "Helper").Color);
        Assert.Equal(disabled, StyleOf(off, "0/10").Color);

        // The error style carries no state branches at all, and the counter keeps the helper color.
        using var errored = new DecoratorHarness(Decorator(
            new InputDecoration(errorText: "Invalid", counterText: "0/10", filled: true)));
        errored.Pump();
        Assert.Equal(colors.Error, StyleOf(errored, "Invalid").Color);
        // Helper and error share the bodySmall slot, and the counter keeps the helper color.
        Assert.Equal(StyleOf(enabled, "Helper").FontSize, StyleOf(errored, "Invalid").FontSize);
        Assert.Equal(colors.OnSurfaceVariant, StyleOf(errored, "0/10").Color);
    }

    [Fact]
    public void InputDecorator_Material3IconColorsFollowErrorAndHoverOnTheSuffixOnly()
    {
        ColorScheme colors = ThemeData.Light.ColorScheme;

        (Color Prefix, Color Suffix, Color Icon) Resolve(
            bool error = false, bool hovering = false, bool enabled = true)
        {
            using var harness = new DecoratorHarness(new InputDecorator(
                decoration: new InputDecoration(
                    icon: new Icon(Icons.Email),
                    prefixIcon: new Icon(Icons.Lock),
                    suffixIcon: new Icon(Icons.Visibility),
                    enabled: enabled,
                    errorText: error ? "Invalid" : null),
                isHovering: hovering,
                child: new Text("value")));
            harness.Pump();
            List<RenderParagraph> icons = DecoratorHarness.FindAll<RenderParagraph>(harness.RenderView);
            return (
                IconColor(icons, Icons.Lock),
                IconColor(icons, Icons.Visibility),
                IconColor(icons, Icons.Email));
        }

        (Color prefix, Color suffix, Color icon) = Resolve();
        Assert.Equal(colors.OnSurfaceVariant, prefix);
        Assert.Equal(colors.OnSurfaceVariant, suffix);
        Assert.Equal(colors.OnSurfaceVariant, icon);

        // Only the suffix icon reacts to the error state; the prefix keeps the enabled color.
        (prefix, suffix, _) = Resolve(error: true);
        Assert.Equal(colors.OnSurfaceVariant, prefix);
        Assert.Equal(colors.Error, suffix);

        (prefix, suffix, _) = Resolve(error: true, hovering: true);
        Assert.Equal(colors.OnSurfaceVariant, prefix);
        Assert.Equal(colors.OnErrorContainer, suffix);

        Color disabled = InputDecoratorDefaults.WithOpacity(colors.OnSurface, 0.38);
        (prefix, suffix, _) = Resolve(enabled: false);
        Assert.Equal(disabled, prefix);
        Assert.Equal(disabled, suffix);
    }

    [Fact]
    public void InputDecorator_PrefixAndSuffixIconColorFallsBackToTheIconButtonTheme()
    {
        ColorScheme colors = ThemeData.Light.ColorScheme;
        var iconButtonTheme = new IconButtonThemeData(
            new ButtonStyle(ForegroundColor: MaterialStateProperty<Color?>.ResolveWith(
                states => states.HasFlag(MaterialState.Error) ? Colors.Orange : Colors.Purple)));

        (Color Prefix, Color Suffix) Resolve(bool error, InputDecoration decoration)
        {
            using var harness = new DecoratorHarness(new IconButtonTheme(
                iconButtonTheme,
                new InputDecorator(
                    decoration: decoration with { ErrorText = error ? "Invalid" : null },
                    child: new Text("value"))));
            harness.Pump();
            List<RenderParagraph> icons = DecoratorHarness.FindAll<RenderParagraph>(harness.RenderView);
            return (IconColor(icons, Icons.Lock), IconColor(icons, Icons.Visibility));
        }

        var plain = new InputDecoration(
            prefixIcon: new Icon(Icons.Lock),
            suffixIcon: new Icon(Icons.Visibility));

        // The ambient IconButtonTheme sits between the decoration and the decorator defaults, and it
        // sees the decorator's own states.
        (Color prefix, Color suffix) = Resolve(error: false, plain);
        Assert.Equal(Colors.Purple, prefix);
        Assert.Equal(Colors.Purple, suffix);

        (prefix, suffix) = Resolve(error: true, plain);
        Assert.Equal(Colors.Orange, prefix);
        Assert.Equal(Colors.Orange, suffix);

        // An explicit decoration color still wins over the button theme.
        (prefix, suffix) = Resolve(error: false, plain with
        {
            PrefixIconColor = Colors.Teal,
            SuffixIconColor = Colors.Teal,
        });
        Assert.Equal(Colors.Teal, prefix);
        Assert.Equal(Colors.Teal, suffix);
        Assert.NotEqual(colors.OnSurfaceVariant, prefix);
    }

    [Fact]
    public void InputDecorator_Material2DefaultsResolvePerState()
    {
        var light = ThemeData.Light with { UseMaterial3 = false };
        var dark = ThemeData.Dark with { UseMaterial3 = false };

        InputBorderPainter Fill(ThemeData theme, bool enabled = true)
        {
            using var harness = new DecoratorHarness(
                Decorator(new InputDecoration(labelText: "Label", filled: true, enabled: enabled)),
                theme);
            harness.Pump();
            return Painter(harness);
        }

        Assert.Equal(Color.FromArgb(0x0A, 0x00, 0x00, 0x00), Fill(light).FillColor);
        Assert.Equal(Color.FromArgb(0x05, 0x00, 0x00, 0x00), Fill(light, enabled: false).FillColor);
        Assert.Equal(Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF), Fill(dark).FillColor);
        Assert.Equal(Color.FromArgb(0x0D, 0xFF, 0xFF, 0xFF), Fill(dark, enabled: false).FillColor);

        // M2 label/hint use hintColor, and the floating label switches on error/focus.
        using var inline = new DecoratorHarness(
            Decorator(new InputDecoration(labelText: "Label", hintText: "Hint"), isEmpty: true,
                child: new SizedBox()),
            light);
        inline.Pump();
        Assert.Equal(light.HintColor, StyleOf(inline, "Label").Color);

        using var floating = new DecoratorHarness(
            Decorator(new InputDecoration(
                labelText: "Label",
                errorText: "Invalid",
                floatingLabelBehavior: FloatingLabelBehavior.Always)),
            light);
        floating.Pump();
        Assert.Equal(light.ColorScheme.Error, StyleOf(floating, "Label").Color);

        // Disabled M2 helper and error text collapse to transparent rather than a dimmed color.
        using var disabled = new DecoratorHarness(
            Decorator(new InputDecoration(helperText: "Helper", enabled: false)),
            light);
        disabled.Pump();
        Assert.Equal(Colors.Transparent, StyleOf(disabled, "Helper").Color);
    }

    [Fact]
    public void InputDecorator_Material2IconColorsFollowFocusBeforeDisabled()
    {
        var light = ThemeData.Light with { UseMaterial3 = false };
        Color unfocused = Color.FromArgb(0x73, 0x00, 0x00, 0x00);

        (Color Prefix, Color Suffix) Resolve(bool focused = false, bool enabled = true, bool error = false)
        {
            using var harness = new DecoratorHarness(
                new InputDecorator(
                    decoration: new InputDecoration(
                        prefixIcon: new Icon(Icons.Lock),
                        suffixIcon: new Icon(Icons.Visibility),
                        enabled: enabled,
                        errorText: error ? "Invalid" : null),
                    isFocused: focused,
                    child: new Text("value")),
                light);
            harness.Pump();
            List<RenderParagraph> icons = DecoratorHarness.FindAll<RenderParagraph>(harness.RenderView);
            return (IconColor(icons, Icons.Lock), IconColor(icons, Icons.Visibility));
        }

        Assert.Equal(unfocused, Resolve().Prefix);
        Assert.Equal(light.ColorScheme.Primary, Resolve(focused: true).Prefix);
        Assert.Equal(light.DisabledColor, Resolve(enabled: false).Prefix);
        // `disabled && !focused` is the guard, so a focused disabled field still uses the primary color.
        Assert.Equal(light.ColorScheme.Primary, Resolve(enabled: false, focused: true).Prefix);
        // Only the suffix has an error branch in M2 as well.
        Assert.Equal(light.ColorScheme.Error, Resolve(error: true).Suffix);
        Assert.Equal(unfocused, Resolve(error: true).Prefix);
    }

    [Fact]
    public void InputDecorator_Material3OutlineAndIndicatorLetFocusBeatHover()
    {
        ColorScheme colors = ThemeData.Light.ColorScheme;

        BorderSide Side(bool filled, bool focused = false, bool hovering = false, bool error = false)
        {
            using var harness = new DecoratorHarness(new InputDecorator(
                decoration: new InputDecoration(
                    labelText: "Label",
                    filled: filled,
                    border: filled ? null : new OutlineInputBorder(),
                    errorText: error ? "Invalid" : null),
                isFocused: focused,
                isHovering: hovering,
                child: new Text("value")));
            harness.Pump();
            return Painter(harness).Border.BorderSide;
        }

        Assert.Equal(colors.Primary, Side(filled: true, focused: true, hovering: true).Color);
        Assert.Equal(2.0, Side(filled: true, focused: true, hovering: true).Width, precision: 6);
        Assert.Equal(colors.Primary, Side(filled: false, focused: true, hovering: true).Color);

        Assert.Equal(colors.OnErrorContainer, Side(filled: true, hovering: true, error: true).Color);
        Assert.Equal(colors.Error, Side(filled: true, focused: true, hovering: true, error: true).Color);
        Assert.Equal(2.0, Side(filled: true, focused: true, error: true).Width, precision: 6);
        Assert.Equal(colors.OnErrorContainer, Side(filled: false, hovering: true, error: true).Color);

        // The outlined disabled color is the 12% one, not the indicator's 38%.
        Assert.Equal(colors.Outline, Side(filled: false).Color);
    }

    [Fact]
    public void InputDecorationTheme_StateResolvingValuesAreResolvedByTheDecorator()
    {
        var stateFill = WidgetStateColor.ResolveWith(states =>
            states.Contains(WidgetState.Focused) ? Colors.Goldenrod : Colors.Gainsboro);
        var stateLabel = WidgetStateTextStyle.ResolveWith(states =>
            new TextStyle(Color: states.Contains(WidgetState.Focused) ? Colors.Crimson : Colors.DarkSlateGray));
        var theme = ThemeData.Light with
        {
            InputDecorationTheme = new InputDecorationThemeData(
                filled: true,
                fillColor: stateFill,
                floatingLabelStyle: stateLabel,
                activeIndicatorBorder: WidgetStateBorderSide.ResolveWith(
                    states => new BorderSide(
                        states.HasFlag(MaterialState.Focused) ? Colors.Magenta : Colors.SeaGreen,
                        2.0))),
        };

        using var resting = new DecoratorHarness(
            Decorator(new InputDecoration(labelText: "Label")), theme);
        resting.Pump();
        Assert.Equal(Colors.Gainsboro, Painter(resting).FillColor);
        Assert.Equal(Colors.DarkSlateGray, StyleOf(resting, "Label").Color);
        Assert.Equal(Colors.SeaGreen, Painter(resting).Border.BorderSide.Color);

        using var focused = new DecoratorHarness(
            new InputDecorator(
                decoration: new InputDecoration(labelText: "Label"),
                isFocused: true,
                child: new Text("value")),
            theme);
        focused.Pump();
        Assert.Equal(Colors.Goldenrod, Painter(focused).FillColor);
        Assert.Equal(Colors.Crimson, StyleOf(focused, "Label").Color);
        Assert.Equal(Colors.Magenta, Painter(focused).Border.BorderSide.Color);
    }

    [Fact]
    public void InputDecorator_FloatingLabelStyleMergesTheBaseStyle()
    {
        // Under M2 the default floating-label style carries only a color, so the base style's own
        // metrics survive the merge chain and the omission is observable.
        using var harness = new DecoratorHarness(
            new InputDecorator(
                decoration: new InputDecoration(
                    labelText: "Label",
                    floatingLabelBehavior: FloatingLabelBehavior.Always),
                baseStyle: new TextStyle(FontSize: 31.0, LetterSpacing: 3.0),
                child: new Text("value")),
            ThemeData.Light with { UseMaterial3 = false });
        harness.Pump();

        TextStyle style = StyleOf(harness, "Label");
        Assert.Equal(31.0, style.FontSize);
        Assert.Equal(3.0, style.LetterSpacing);
        Assert.Equal(1.0, style.Height);
    }

    private static Color IconColor(List<RenderParagraph> paragraphs, IconData icon)
    {
        RenderParagraph paragraph = paragraphs.Single(
            value => value.PlainText == char.ConvertFromUtf32(icon.CodePoint));
        return paragraph.Text.Style?.Color
               ?? throw new InvalidOperationException("The icon carries no resolved color.");
    }

    private static TextStyle StyleOf(DecoratorHarness harness, string text) =>
        DecoratorHarness.FindAll<RenderParagraph>(harness.RenderView)
            .Single(value => value.PlainText == text)
            .Text.Style
        ?? throw new InvalidOperationException($"No resolved style for \"{text}\".");

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
