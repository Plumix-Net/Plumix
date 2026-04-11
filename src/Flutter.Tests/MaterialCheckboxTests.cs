using System.Linq;
using Avalonia;
using Avalonia.Media;
using Flutter;
using Flutter.Foundation;
using Flutter.Material;
using Flutter.Rendering;
using Flutter.UI;
using Flutter.Widgets;
using Xunit;

namespace Flutter.Tests;

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
            PrimaryColor = Colors.Coral
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

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(Colors.Coral, decorated!.Decoration.Color);
        Assert.True(decorated.Decoration.Border.HasValue);
        Assert.Equal(0, decorated.Decoration.Border!.Value.Width);
        Assert.Equal(Colors.Transparent, decorated.Decoration.Border.Value.Color);
    }

    [Fact]
    public void Checkbox_DefaultM3_Unchecked_UsesTransparentFillAndOnSurfaceVariantBorder()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            OnSurfaceVariantColor = Colors.CadetBlue
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

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(Colors.Transparent, decorated!.Decoration.Color);
        Assert.True(decorated.Decoration.Border.HasValue);
        Assert.Equal(2, decorated.Decoration.Border!.Value.Width);
        Assert.Equal(Colors.CadetBlue, decorated.Decoration.Border.Value.Color);
    }

    [Fact]
    public void Checkbox_DefaultM3_DisabledChecked_UsesOnSurfaceOpacityFill()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            OnSurfaceColor = Colors.Brown
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

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(ApplyOpacity(Colors.Brown, 0.38), decorated!.Decoration.Color);
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

        var paragraph = FindDescendant<RenderParagraph>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.Lime, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
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

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var paragraph = FindDescendant<RenderParagraph>(renderRoot);
        var dash = FindDescendant<RenderColoredBox>(renderRoot);
        Assert.Null(paragraph);
        Assert.NotNull(dash);
        Assert.Equal(ThemeData.Light.OnPrimaryColor, dash!.Color);
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

            var handled = FocusManager.Instance.HandleKeyEvent(new KeyEvent(key: "Space", isDown: true));
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

            var handled = FocusManager.Instance.HandleKeyEvent(new KeyEvent(key: "Space", isDown: true));
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

            var handled = FocusManager.Instance.HandleKeyEvent(new KeyEvent(key: "Space", isDown: true));
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

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(Colors.MediumPurple, decorated!.Decoration.Color);
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

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(Colors.ForestGreen, decorated!.Decoration.Color);
    }

    [Fact]
    public void Checkbox_ErrorState_Checked_UsesErrorFillAndOnErrorCheckColor()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ErrorColor = Colors.OrangeRed,
            OnErrorColor = Colors.AliceBlue
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

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var decorated = FindDescendant<RenderDecoratedBox>(renderRoot);
        var paragraph = FindDescendant<RenderParagraph>(renderRoot);
        Assert.NotNull(decorated);
        Assert.NotNull(paragraph);
        Assert.Equal(Colors.OrangeRed, decorated!.Decoration.Color);
        Assert.Equal(Colors.AliceBlue, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
    }

    [Fact]
    public void Checkbox_ErrorState_Unchecked_UsesErrorBorder()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ErrorColor = Colors.OrangeRed
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

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.True(decorated!.Decoration.Border.HasValue);
        Assert.Equal(Colors.OrangeRed, decorated.Decoration.Border!.Value.Color);
        Assert.Equal(2, decorated.Decoration.Border.Value.Width);
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
        Assert.True(decorated.Decoration.Border.HasValue);
        Assert.Equal(0, decorated.Decoration.Border!.Value.Width);
        Assert.Equal(Colors.Transparent, decorated.Decoration.Border.Value.Color);
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

        var hasGradientBrush = HasGradientBrushFill(RequireRenderObject<RenderObject>(root.ChildElement));
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

        var hasGradientBrush = HasGradientBrushFill(RequireRenderObject<RenderObject>(root.ChildElement));
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

        var inkSplash = FindDescendant<RenderInkSplash>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(inkSplash);
        Assert.Equal(7, inkSplash!.SplashRadius);
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

            var now = Scheduler.CurrentSeconds;
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.10));
            harness.Pump(new Size(160, 120));

            var paragraphDuringTransition = FindDescendant<RenderParagraph>(harness.RenderView);
            var dashDuringTransition = FindDescendant<RenderColoredBox>(harness.RenderView);
            Assert.NotNull(paragraphDuringTransition);
            Assert.NotNull(dashDuringTransition);

            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.30));
            harness.Pump(new Size(160, 120));

            var paragraphAfterTransition = FindDescendant<RenderParagraph>(harness.RenderView);
            var dashAfterTransition = FindDescendant<RenderColoredBox>(harness.RenderView);
            Assert.Null(paragraphAfterTransition);
            Assert.NotNull(dashAfterTransition);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    private static T RequireRenderObject<T>(Element? element) where T : RenderObject
    {
        Assert.NotNull(element);
        Assert.NotNull(element!.RenderObject);
        return Assert.IsAssignableFrom<T>(element.RenderObject);
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
            .Any(box => box.Decoration.Brush is LinearGradientBrush);
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
        var alpha = (byte)Math.Clamp((int)(255 * opacity), 0, 255);
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
