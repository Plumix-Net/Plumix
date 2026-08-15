using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialCheckboxTests
{
    [Fact]
    public void MaterialIcons_Check_UsesExpectedCodePoint()
    {
        Assert.Equal(0xe156, Icons.Check.CodePoint);
    }

    [Fact]
    public void Constructor_Throws_WhenValueIsNullAndTristateIsFalse()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            _ = new Checkbox(
                value: null,
                onChanged: _ => { },
                tristate: false);
        });
    }

    [Fact]
    public void Checkbox_DefaultM3_Checked_UsesPrimaryFillAndTransparentBorder()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme with
            {
                Primary = Colors.Coral
            }
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new Checkbox(
                    value: true,
                    onChanged: _ => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        CheckboxPainter painter = FindCheckboxPainter(root.ChildElement);
        Assert.Equal(Colors.Coral, painter.ActiveColor);
        Assert.True(painter.ActiveSide.HasValue);
        Assert.Equal(0, painter.ActiveSide!.Value.Width);
        Assert.Equal(Colors.Transparent, painter.ActiveSide.Value.Color);
    }

    [Fact]
    public void Checkbox_DefaultM3_Unchecked_UsesTransparentFillAndOnSurfaceVariantBorder()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme with
            {
                OnSurfaceVariant = Colors.CadetBlue
            }
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new Checkbox(
                    value: false,
                    onChanged: _ => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        CheckboxPainter painter = FindCheckboxPainter(root.ChildElement);
        Assert.Equal(Colors.Transparent, painter.InactiveColor);
        Assert.True(painter.InactiveSide.HasValue);
        Assert.Equal(2, painter.InactiveSide!.Value.Width);
        Assert.Equal(Colors.CadetBlue, painter.InactiveSide.Value.Color);
    }

    [Fact]
    public void Checkbox_DefaultM3_DisabledChecked_UsesOnSurfaceOpacityFill()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme with
            {
                OnSurface = Colors.Brown
            }
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new Checkbox(
                    value: true,
                    onChanged: null)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        CheckboxPainter painter = FindCheckboxPainter(root.ChildElement);
        Assert.Equal(ApplyOpacity(Colors.Brown, 0.38), painter.ActiveColor);
    }

    [Fact]
    public void Checkbox_Checkmark_UsesCheckColorOverride()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new Checkbox(
                    value: true,
                    checkColor: Colors.Lime,
                    onChanged: _ => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        CheckboxPainter painter = FindCheckboxPainter(root.ChildElement);
        Assert.Equal(Colors.Lime, painter.CheckColor);
    }

    [Fact]
    public void Checkbox_Unchecked_DoesNotRenderCheckmarkParagraph()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new Checkbox(
                    value: false,
                    onChanged: _ => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.Null(paragraph);
    }

    [Fact]
    public void Checkbox_TristateNull_RendersDashIndicator()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new Checkbox(
                    value: null,
                    tristate: true,
                    onChanged: _ => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        CheckboxPainter painter = FindCheckboxPainter(root.ChildElement);
        Assert.Null(painter.Value);
        Assert.Equal(ThemeData.Light.ColorScheme.OnPrimary, painter.CheckColor);
    }

    [Fact]
    public void Checkbox_DefaultTapTarget_Padded_ExpandsHitArea()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new SizedBox(
                    width: 120,
                    child: new Checkbox(
                        value: false,
                        onChanged: _ => { }))));

        harness.Pump(new Size(220, 120));

        var hitResult = new BoxHitTestResult();
        Assert.True(harness.RenderView.HitTest(hitResult, new Point(60, 46)));
    }

    [Fact]
    public void Checkbox_ThemeTapTarget_ShrinkWrap_DoesNotExpandHitArea()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with { MaterialTapTargetSize = MaterialTapTargetSize.ShrinkWrap },
                child: new SizedBox(
                    width: 120,
                    child: new Checkbox(
                        value: false,
                        onChanged: _ => { }))));

        harness.Pump(new Size(220, 120));

        var hitResult = new BoxHitTestResult();
        Assert.False(harness.RenderView.HitTest(hitResult, new Point(60, 46)));
    }

    [Fact]
    public void Checkbox_SemanticLabel_PropagatesCheckedAndEnabledSemantics()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            using var harness = new WidgetRenderHarness(
                new Theme(
                    data: ThemeData.Light,
                    child: new Checkbox(
                        value: true,
                        onChanged: _ => { },
                        semanticLabel: "Accept terms")));

            var semanticsRoot = harness.PumpAndGetSemantics(new Size(220, 120));
            Assert.NotNull(semanticsRoot);

            var semanticsNode = FindFirstSemanticsNode(
                semanticsRoot!,
                static node => node.Label == "Accept terms");
            Assert.NotNull(semanticsNode);
            Assert.True(semanticsNode!.Flags.HasFlag(SemanticsFlags.IsChecked));
            Assert.True(semanticsNode.Flags.HasFlag(SemanticsFlags.IsEnabled));
            Assert.True(semanticsNode.Actions.HasFlag(SemanticsActions.Tap));
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void Checkbox_Activation_EmitsTapSemanticEvent()
    {
        FocusManager.Instance.ResetForTests();
        SemanticsEvent? received = null;
        void HandleEvent(SemanticsEvent semanticsEvent) => received = semanticsEvent;
        SemanticsService.SemanticsEventRequested += HandleEvent;
        try
        {
            var focusNode = new FocusNode();
            using var harness = new WidgetRenderHarness(
                new Theme(
                    data: ThemeData.Light,
                    child: new Checkbox(
                        value: false,
                        onChanged: _ => { },
                        focusNode: focusNode,
                        semanticLabel: "Event checkbox")));
            _ = harness.PumpAndGetSemantics(new Size(120, 120));

            Assert.True(focusNode.RequestFocus());
            Assert.True(FocusManager.Instance.HandleKeyEvent(
                KeySim.Down(LogicalKeyboardKey.Space)));

            TapSemanticEvent tapEvent = Assert.IsType<TapSemanticEvent>(received);
            Assert.Equal("tap", tapEvent.Type);
            Assert.NotNull(tapEvent.NodeId);
        }
        finally
        {
            SemanticsService.SemanticsEventRequested -= HandleEvent;
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void Checkbox_KeyboardActivation_TogglesFalseToTrue()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            var owner = new BuildOwner();
            bool? nextValue = null;
            var focusNode = new FocusNode();
            var root = new TestRootElement(
                new Theme(
                    data: ThemeData.Light,
                    child: new Checkbox(
                        value: false,
                        focusNode: focusNode,
                        onChanged: value => nextValue = value)));

            root.Attach(owner);
            root.Mount(parent: null, newSlot: null);
            owner.FlushBuild();

            Assert.True(focusNode.RequestFocus());
            owner.FlushBuild();

            bool handled = FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Space));
            Assert.True(handled);
            owner.FlushBuild();

            Assert.Equal(true, nextValue);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void Checkbox_KeyboardActivation_TristateCyclesTrueToNull()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            var owner = new BuildOwner();
            bool? nextValue = true;
            var focusNode = new FocusNode();
            var root = new TestRootElement(
                new Theme(
                    data: ThemeData.Light,
                    child: new Checkbox(
                        value: true,
                        tristate: true,
                        focusNode: focusNode,
                        onChanged: value => nextValue = value)));

            root.Attach(owner);
            root.Mount(parent: null, newSlot: null);
            owner.FlushBuild();

            Assert.True(focusNode.RequestFocus());
            owner.FlushBuild();

            bool handled = FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Space));
            Assert.True(handled);
            owner.FlushBuild();

            Assert.Null(nextValue);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void Checkbox_KeyboardActivation_TristateCyclesNullToFalse()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            var owner = new BuildOwner();
            bool? nextValue = null;
            var focusNode = new FocusNode();
            var root = new TestRootElement(
                new Theme(
                    data: ThemeData.Light,
                    child: new Checkbox(
                        value: null,
                        tristate: true,
                        focusNode: focusNode,
                        onChanged: value => nextValue = value)));

            root.Attach(owner);
            root.Mount(parent: null, newSlot: null);
            owner.FlushBuild();

            Assert.True(focusNode.RequestFocus());
            owner.FlushBuild();

            bool handled = FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Space));
            Assert.True(handled);
            owner.FlushBuild();

            Assert.Equal(false, nextValue);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void Checkbox_ThemeFillColor_IsApplied_WhenWidgetFillIsNotProvided()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    CheckboxTheme = new CheckboxThemeData(
                        FillColor: MaterialStateProperty<Color?>.All(Colors.MediumPurple))
                },
                child: new Checkbox(
                    value: true,
                    onChanged: _ => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        CheckboxPainter painter = FindCheckboxPainter(root.ChildElement);
        Assert.Equal(Colors.MediumPurple, painter.ActiveColor);
    }

    [Fact]
    public void Checkbox_WidgetFillColor_PrecedesCheckboxThemeFillColor()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    CheckboxTheme = new CheckboxThemeData(
                        FillColor: MaterialStateProperty<Color?>.All(Colors.MediumPurple))
                },
                child: new Checkbox(
                    value: true,
                    fillColor: MaterialStateProperty<Color?>.All(Colors.ForestGreen),
                    onChanged: _ => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        CheckboxPainter painter = FindCheckboxPainter(root.ChildElement);
        Assert.Equal(Colors.ForestGreen, painter.ActiveColor);
    }

    [Fact]
    public void Checkbox_ErrorState_Checked_UsesErrorFillAndOnErrorCheckColor()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme with
            {
                Error = Colors.OrangeRed,
                OnError = Colors.AliceBlue
            }
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new Checkbox(
                    value: true,
                    isError: true,
                    onChanged: _ => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        CheckboxPainter painter = FindCheckboxPainter(root.ChildElement);
        Assert.Equal(Colors.OrangeRed, painter.ActiveColor);
        Assert.Equal(Colors.AliceBlue, painter.CheckColor);
    }

    [Fact]
    public void Checkbox_ErrorState_Unchecked_UsesErrorBorder()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme with
            {
                Error = Colors.OrangeRed
            }
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new Checkbox(
                    value: false,
                    isError: true,
                    onChanged: _ => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        CheckboxPainter painter = FindCheckboxPainter(root.ChildElement);
        Assert.True(painter.InactiveSide.HasValue);
        Assert.Equal(Colors.OrangeRed, painter.InactiveSide!.Value.Color);
        Assert.Equal(2, painter.InactiveSide.Value.Width);
    }

    [Fact]
    public void Checkbox_AdaptiveConstructor_Throws_WhenValueIsNullAndTristateIsFalse()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            _ = Checkbox.Adaptive(
                value: null,
                onChanged: _ => { },
                tristate: false);
        });
    }

    [Fact]
    public void Checkbox_AdaptiveIOS_Checked_UsesCupertinoDefaults()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            Platform = TargetPlatform.IOS,
            PrimaryColor = Colors.Coral
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: Checkbox.Adaptive(
                    value: true,
                    onChanged: _ => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(Color.FromArgb(255, 0, 122, 255), decorated!.Decoration.Color);
        Assert.True(decorated.Decoration.Border is not null);
        Assert.Equal(0, ((Plumix.Rendering.Border)decorated.Decoration.Border!).Top.Width);
        Assert.Equal(Colors.Transparent, ((Plumix.Rendering.Border)decorated.Decoration.Border!).Top.Color);
    }

    [Fact]
    public void Checkbox_AdaptiveIOS_Checked_UsesVectorIndicatorInsteadOfTextParagraph()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    Platform = TargetPlatform.IOS
                },
                child: Checkbox.Adaptive(
                    value: true,
                    onChanged: _ => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.Null(paragraph);
    }

    [Fact]
    public void Checkbox_AdaptiveIOS_FillColorParameter_IsIgnored()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    Platform = TargetPlatform.IOS
                },
                child: Checkbox.Adaptive(
                    value: true,
                    activeColor: Colors.Orange,
                    fillColor: MaterialStateProperty<Color?>.All(Colors.MediumPurple),
                    onChanged: _ => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(Colors.Orange, decorated!.Decoration.Color);
    }

    [Fact]
    public void Checkbox_AdaptiveIOS_MaterialTapTargetSizeParameter_IsIgnored_AndUsesCupertinoTapTarget()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with
                {
                    Platform = TargetPlatform.IOS
                },
                child: new SizedBox(
                    width: 120,
                    child: Checkbox.Adaptive(
                        value: false,
                        materialTapTargetSize: MaterialTapTargetSize.ShrinkWrap,
                        onChanged: _ => { }))));

        harness.Pump(new Size(220, 120));

        var hitInsideCupertinoTarget = new BoxHitTestResult();
        Assert.True(harness.RenderView.HitTest(hitInsideCupertinoTarget, new Point(60, 30)));

        var hitOutsideCupertinoTarget = new BoxHitTestResult();
        Assert.False(harness.RenderView.HitTest(hitOutsideCupertinoTarget, new Point(60, 46)));
    }

    [Fact]
    public void Checkbox_AdaptiveMacOS_DefaultTapTarget_DoesNotExpandHitArea()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with
                {
                    Platform = TargetPlatform.MacOS
                },
                child: new SizedBox(
                    width: 120,
                    child: Checkbox.Adaptive(
                        value: false,
                        onChanged: _ => { }))));

        harness.Pump(new Size(220, 120));

        var hitResult = new BoxHitTestResult();
        Assert.False(harness.RenderView.HitTest(hitResult, new Point(60, 46)));
    }

    [Fact]
    public void Checkbox_AdaptiveMacOS_UsesCupertinoVisualWidth()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with
                {
                    Platform = TargetPlatform.MacOS
                },
                child: Checkbox.Adaptive(
                    value: false,
                    onChanged: _ => { })));

        harness.Pump(new Size(220, 120));

        var checkboxBody = FindDecoratedBoxBySize(harness.RenderView, width: 14, height: 14, tolerance: 0.02);
        Assert.NotNull(checkboxBody);
    }

    [Fact]
    public void Checkbox_AdaptiveDarkUnchecked_UsesGradientFillBrush()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    Platform = TargetPlatform.IOS,
                    Brightness = Brightness.Dark
                },
                child: Checkbox.Adaptive(
                    value: false,
                    onChanged: _ => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        bool hasGradientBrush = HasGradientBrushFill(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.True(hasGradientBrush);
    }

    [Fact]
    public void Checkbox_AdaptiveDarkCheckedEnabled_DoesNotUseGradientFillBrush()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    Platform = TargetPlatform.IOS,
                    Brightness = Brightness.Dark
                },
                child: Checkbox.Adaptive(
                    value: true,
                    onChanged: _ => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        bool hasGradientBrush = HasGradientBrushFill(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.False(hasGradientBrush);
    }

    [Fact]
    public void Checkbox_ThemeSplashRadius_PropagatesToInkSplash()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    CheckboxTheme = new CheckboxThemeData(SplashRadius: 7)
                },
                child: new Checkbox(
                    value: true,
                    onChanged: _ => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        CheckboxPainter painter = FindCheckboxPainter(root.ChildElement);
        Assert.Equal(7, painter.ResolvedSplashRadius);
    }

    [Fact]
    public void Checkbox_Transition_FromCheckedToNull_CrossfadesCheckAndDash()
    {
        Scheduler.ResetForTests();
        try
        {
            Action<bool?>? setValue = null;
            using var harness = new WidgetRenderHarness(
                new CheckboxTransitionHost(registerSetValue: callback => setValue = callback));

            harness.Pump(new Size(160, 120));
            Assert.NotNull(setValue);

            setValue!(null);
            harness.Pump(new Size(160, 120));

            double now = Scheduler.CurrentSeconds;
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.10));
            harness.Pump(new Size(160, 120));

            CheckboxPainter duringTransition = FindCheckboxPainter(harness.RenderView);
            Assert.Equal(true, duringTransition.PreviousValue);
            Assert.Null(duringTransition.Value);

            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.30));
            harness.Pump(new Size(160, 120));

            CheckboxPainter afterTransition = FindCheckboxPainter(harness.RenderView);
            Assert.Null(afterTransition.Value);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void Checkbox_DefaultM2_UsesSecondaryWhiteCheckAndUnselectedWidgetTokens()
    {
        var colorScheme = ColorScheme.Light(secondary: Colors.Coral);
        var theme = new ThemeData(
            useMaterial3: false,
            colorScheme: colorScheme,
            unselectedWidgetColor: Colors.CadetBlue);
        var owner = new BuildOwner();
        var checkedRoot = new TestRootElement(
            new Theme(
                data: theme,
                child: new Checkbox(value: true, onChanged: _ => { })));

        checkedRoot.Attach(owner);
        checkedRoot.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        CheckboxPainter checkedPainter = FindCheckboxPainter(checkedRoot.ChildElement);
        Assert.Equal(Colors.Coral, checkedPainter.ActiveColor);
        Assert.Equal(Colors.White, checkedPainter.CheckColor);
        Assert.Equal(1.0, ShapeBorderGeometry.ResolveRadius(checkedPainter.Shape).Radius);
        Assert.Equal(2.0, checkedPainter.ActiveSide!.Value.Width);

        var uncheckedRoot = new TestRootElement(
            new Theme(
                data: theme,
                child: new Checkbox(value: false, onChanged: _ => { })));
        uncheckedRoot.Attach(owner);
        uncheckedRoot.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        CheckboxPainter uncheckedPainter = FindCheckboxPainter(uncheckedRoot.ChildElement);
        Assert.Equal(Colors.CadetBlue, uncheckedPainter.InactiveSide!.Value.Color);
    }

    [Fact]
    public void Checkbox_M3ThemeVisualDensity_IsIgnored_ButWidgetDensityApplies()
    {
        using var themedHarness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with { VisualDensity = VisualDensity.Compact },
                child: new Checkbox(value: false, onChanged: _ => { })));
        themedHarness.Pump(new Size(120, 120));

        RenderCustomPaint themedPaint = FindDescendant<RenderCustomPaint>(themedHarness.RenderView)!;
        Assert.Equal(new Size(48, 48), themedPaint.Size);

        using var widgetHarness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new Checkbox(
                    value: false,
                    onChanged: _ => { },
                    visualDensity: new VisualDensity(3, -3))));
        widgetHarness.Pump(new Size(120, 120));

        RenderCustomPaint widgetPaint = FindDescendant<RenderCustomPaint>(widgetHarness.RenderView)!;
        Assert.Equal(new Size(60, 36), widgetPaint.Size);
    }

    [Fact]
    public void Checkbox_M2ThemeVisualDensity_AdjustsTapTarget()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: new ThemeData(
                    useMaterial3: false,
                    visualDensity: VisualDensity.Compact),
                child: new Checkbox(value: false, onChanged: _ => { })));
        harness.Pump(new Size(120, 120));

        RenderCustomPaint customPaint = FindDescendant<RenderCustomPaint>(harness.RenderView)!;
        Assert.Equal(new Size(40, 40), customPaint.Size);
    }

    [Fact]
    public void Checkbox_TristateNull_ExposesMixedSemantics()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new Checkbox(
                    value: null,
                    onChanged: _ => { },
                    tristate: true,
                    semanticLabel: "Mixed checkbox")));

        SemanticsNode? semanticsRoot = harness.PumpAndGetSemantics(new Size(120, 120));
        SemanticsNode? checkboxNode = FindFirstSemanticsNode(
            semanticsRoot!,
            static node => node.Label == "Mixed checkbox");

        Assert.NotNull(checkboxNode);
        Assert.True(checkboxNode!.Flags.HasFlag(SemanticsFlags.HasCheckedState));
        Assert.True(checkboxNode.Flags.HasFlag(SemanticsFlags.IsCheckStateMixed));
        Assert.False(checkboxNode.Flags.HasFlag(SemanticsFlags.IsChecked));
    }

    [Fact]
    public void Checkbox_FixedSide_AppliesOnlyWhenUnselected()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new Checkbox(
                    value: true,
                    onChanged: _ => { },
                    side: new BorderSide(Colors.Red, 4))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        CheckboxPainter painter = FindCheckboxPainter(root.ChildElement);
        Assert.Equal(0.0, painter.ActiveSide!.Value.Width);
        Assert.Equal(4.0, painter.InactiveSide!.Value.Width);
        Assert.Equal(Colors.Red, painter.InactiveSide.Value.Color);
    }

    [Fact]
    public void Checkbox_WidgetStateBorderSide_AppliesWhenSelectedAndInError()
    {
        bool sawSelectedError = false;
        WidgetStateBorderSide side = WidgetStateBorderSide.ResolveWith(states =>
        {
            sawSelectedError |= states.HasFlag(MaterialState.Selected)
                                && states.HasFlag(MaterialState.Error);
            return new BorderSide(Colors.Red, 4);
        });
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new Checkbox(
                    value: true,
                    onChanged: _ => { },
                    side: side,
                    isError: true)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        CheckboxPainter painter = FindCheckboxPainter(root.ChildElement);
        Assert.Equal(4.0, painter.ActiveSide!.Value.Width);
        Assert.Equal(Colors.Red, painter.ActiveSide.Value.Color);
        Assert.True(sawSelectedError);
    }

    [Fact]
    public void Checkbox_UncheckedFillColor_IsRetainedAlongsideDefaultBorder()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new Checkbox(
                    value: false,
                    onChanged: _ => { },
                    fillColor: MaterialStateProperty<Color?>.All(Colors.ForestGreen))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        CheckboxPainter painter = FindCheckboxPainter(root.ChildElement);
        Assert.Equal(Colors.ForestGreen, painter.InactiveColor);
        Assert.Equal(ThemeData.Light.ColorScheme.OnSurfaceVariant, painter.InactiveSide!.Value.Color);
    }

    [Fact]
    public void Checkbox_DefaultM3OverlayColors_MatchActiveAndInactiveTokens()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new Checkbox(value: true, onChanged: _ => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        CheckboxPainter painter = FindCheckboxPainter(root.ChildElement);
        Assert.Equal(ApplyOpacity(ThemeData.Light.ColorScheme.OnSurface, 0.10), painter.ActiveReactionColor);
        Assert.Equal(ApplyOpacity(ThemeData.Light.ColorScheme.Primary, 0.10), painter.InactiveReactionColor);
        Assert.Equal(ApplyOpacity(ThemeData.Light.ColorScheme.Primary, 0.08), painter.ResolvedHoverColor);
        Assert.Equal(ApplyOpacity(ThemeData.Light.ColorScheme.Primary, 0.10), painter.ResolvedFocusColor);
    }

    [Fact]
    public void CheckboxThemeData_CopyWithAndLerp_CoverEveryField()
    {
        var first = new CheckboxThemeData(
            MouseCursor: MaterialStateProperty<MouseCursor?>.All(SystemMouseCursors.Basic),
            FillColor: MaterialStateProperty<Color?>.All(Colors.Black),
            CheckColor: MaterialStateProperty<Color?>.All(Colors.Red),
            OverlayColor: MaterialStateProperty<Color?>.All(Colors.Blue),
            SplashRadius: 10,
            MaterialTapTargetSize: MaterialTapTargetSize.Padded,
            VisualDensity: VisualDensity.Compact,
            Shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(2)),
            Side: new BorderSide(Colors.Black, 2));
        CheckboxThemeData copied = first.CopyWith(splashRadius: 14);
        Assert.Equal(14, copied.SplashRadius);
        Assert.Same(first.MouseCursor, copied.MouseCursor);
        Assert.Same(first.FillColor, copied.FillColor);
        Assert.Equal(first.Shape, copied.Shape);

        var second = new CheckboxThemeData(
            MouseCursor: MaterialStateProperty<MouseCursor?>.All(SystemMouseCursors.Click),
            FillColor: MaterialStateProperty<Color?>.All(Colors.White),
            CheckColor: MaterialStateProperty<Color?>.All(Colors.Blue),
            OverlayColor: MaterialStateProperty<Color?>.All(Colors.Red),
            SplashRadius: 20,
            MaterialTapTargetSize: MaterialTapTargetSize.ShrinkWrap,
            VisualDensity: VisualDensity.Standard,
            Shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(6)),
            Side: WidgetStateBorderSide.All(new BorderSide(Colors.White, 4)));
        CheckboxThemeData midpoint = CheckboxThemeData.Lerp(first, second, 0.5);

        Assert.Equal(15, midpoint.SplashRadius);
        Assert.Same(second.MouseCursor, midpoint.MouseCursor);
        Assert.Equal(MaterialTapTargetSize.ShrinkWrap, midpoint.MaterialTapTargetSize);
        Assert.Equal(VisualDensity.Standard, midpoint.VisualDensity);
        Assert.Equal(4, ShapeBorderGeometry.ResolveRadius(midpoint.Shape).Radius);
        Assert.Equal(3, midpoint.Side!.Resolve(MaterialState.None)!.Value.Width);
    }

    [Fact]
    public void Checkbox_RendersUnderZeroConstraints()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new SizedBox(
                    width: 0,
                    height: 0,
                    child: new Checkbox(value: true, onChanged: null))));

        harness.Pump(new Size(0, 0));

        RenderCustomPaint customPaint = FindDescendant<RenderCustomPaint>(harness.RenderView)!;
        Assert.Equal(new Size(0, 0), customPaint.Size);
    }

    private static T RequireRenderObject<T>(Element? element) where T : RenderObject
    {
        Assert.NotNull(element);
        Assert.NotNull(element!.RenderObject);
        return Assert.IsAssignableFrom<T>(element.RenderObject);
    }

    private static CheckboxPainter FindCheckboxPainter(Element? element)
    {
        return FindCheckboxPainter(RequireRenderObject<RenderObject>(element));
    }

    private static CheckboxPainter FindCheckboxPainter(RenderObject root)
    {
        RenderCustomPaint? customPaint = FindDescendant<RenderCustomPaint>(root);
        Assert.NotNull(customPaint);
        return Assert.IsType<CheckboxPainter>(customPaint!.Painter);
    }

    private static T? FindDescendant<T>(RenderObject? root) where T : RenderObject
    {
        if (root is null)
        {
            return null;
        }

        if (root is T match)
        {
            return match;
        }

        T? result = null;
        root.VisitChildren(child =>
        {
            if (result is not null)
            {
                return;
            }

            result = FindDescendant<T>(child);
        });

        return result;
    }

    private static RenderDecoratedBox? FindDecoratedBoxBySize(
        RenderObject root,
        double width,
        double height,
        double tolerance = 0.01)
    {
        return FindDescendants<RenderDecoratedBox>(root)
            .FirstOrDefault(box =>
                Math.Abs(box.Size.Width - width) <= tolerance
                && Math.Abs(box.Size.Height - height) <= tolerance);
    }

    private static bool HasGradientBrushFill(RenderObject root)
    {
        return FindDescendants<RenderDecoratedBox>(root)
            .Any(box => box.Decoration.Gradient is Plumix.Rendering.LinearGradient);
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

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
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

    private static Color ApplyOpacity(Color color, double opacity)
    {
        byte alpha = (byte)Math.Clamp((int)Math.Round(255 * opacity), 0, 255);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
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

    private sealed class CheckboxTransitionHost : StatefulWidget
    {
        public CheckboxTransitionHost(Action<Action<bool?>> registerSetValue, Key? key = null) : base(key)
        {
            RegisterSetValue = registerSetValue;
        }

        public Action<Action<bool?>> RegisterSetValue { get; }

        public override State CreateState()
        {
            return new CheckboxTransitionHostState();
        }
    }

    private sealed class CheckboxTransitionHostState : State
    {
        private bool? _value = true;
        private CheckboxTransitionHost CurrentWidget => (CheckboxTransitionHost)StateWidget;

        public override void InitState()
        {
            CurrentWidget.RegisterSetValue(next => SetState(() => _value = next));
        }

        public override Widget Build(BuildContext context)
        {
            return new Theme(
                data: ThemeData.Light,
                child: new Checkbox(
                    value: _value,
                    tristate: true,
                    onChanged: _ => { }));
        }
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

        public Element? ChildElement => _child;

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

        internal override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
            }
        }

        internal override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
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

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot != null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
            if (!Equals(oldSlot, newSlot))
            {
                throw new InvalidOperationException("TestRootElement does not support slot moves.");
            }
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot != null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
        }
    }
}
