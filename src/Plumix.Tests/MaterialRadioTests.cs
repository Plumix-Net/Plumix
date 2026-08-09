using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class MaterialRadioTests
{
    [Fact]
    public void Radio_DefaultM3_Selected_UsesPrimaryColorForBorderAndDot()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(primary: Colors.Coral)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new Radio<string>(
                    value: "first",
                    groupValue: "first",
                    onChanged: _ => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        RadioPainter painter = FindRadioPainter(renderRoot);

        Assert.Equal(Colors.Coral, painter.ActiveSide.Color);
        Assert.Equal(2, painter.ActiveSide.Width);
        Assert.Equal(Colors.Coral, painter.ActiveColor);
    }

    [Fact]
    public void Radio_DefaultM3_Unselected_UsesOnSurfaceVariantBorderAndNoDot()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(onSurfaceVariant: Colors.CadetBlue)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new Radio<string>(
                    value: "first",
                    groupValue: "second",
                    onChanged: _ => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        RadioPainter painter = FindRadioPainter(renderRoot);

        Assert.Equal(Colors.CadetBlue, painter.InactiveSide.Color);
    }

    [Fact]
    public void Radio_DefaultM3_DisabledSelected_UsesOnSurfaceOpacityForBorderAndDot()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(onSurface: Colors.Brown)
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new Radio<string>(
                    value: "first",
                    groupValue: "first",
                    onChanged: null)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        RadioPainter painter = FindRadioPainter(renderRoot);
        var expected = ApplyOpacity(Colors.Brown, 0.38);

        Assert.Equal(expected, painter.ActiveSide.Color);
        Assert.Equal(expected, painter.ActiveColor);
    }

    [Fact]
    public void Radio_WidgetFillColor_PrecedesActiveColorAndThemeFillColor()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    RadioTheme = new RadioThemeData(
                        FillColor: MaterialStateProperty<Color?>.All(Colors.MediumPurple))
                },
                child: new Radio<string>(
                    value: "first",
                    groupValue: "first",
                    onChanged: _ => { },
                    activeColor: Colors.Orange,
                    fillColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                    {
                        return states.HasFlag(MaterialState.Selected)
                            ? Colors.ForestGreen
                            : Colors.SlateGray;
                    }))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        RadioPainter painter = FindRadioPainter(renderRoot);

        Assert.Equal(Colors.ForestGreen, painter.ActiveSide.Color);
        Assert.Equal(Colors.ForestGreen, painter.ActiveColor);
    }

    [Fact]
    public void Radio_ThemeFillColor_Applies_WhenWidgetFillColorIsMissing()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    RadioTheme = new RadioThemeData(
                        FillColor: MaterialStateProperty<Color?>.All(Colors.MediumPurple))
                },
                child: new Radio<string>(
                    value: "first",
                    groupValue: "first",
                    onChanged: _ => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        RadioPainter painter = FindRadioPainter(renderRoot);

        Assert.Equal(Colors.MediumPurple, painter.ActiveSide.Color);
        Assert.Equal(Colors.MediumPurple, painter.ActiveColor);
    }

    [Fact]
    public void Radio_KeyboardActivation_Unselected_CallsOnChangedWithValue()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            var owner = new BuildOwner();
            string? nextValue = null;
            var focusNode = new FocusNode();
            var root = new TestRootElement(
                new Theme(
                    data: ThemeData.Light,
                    child: new Radio<string>(
                        value: "first",
                        groupValue: "second",
                        focusNode: focusNode,
                        onChanged: value => nextValue = value)));

            root.Attach(owner);
            root.Mount(parent: null, newSlot: null);
            owner.FlushBuild();

            Assert.True(focusNode.RequestFocus());

            bool handled = FocusManager.Instance.HandleKeyEvent(new KeyEvent(key: "Space", isDown: true));
            Assert.True(handled);
            owner.FlushBuild();

            Assert.Equal("first", nextValue);
            focusNode.Dispose();
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void Radio_KeyboardActivation_SelectedToggleable_CallsOnChangedWithNull()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            var owner = new BuildOwner();
            string? nextValue = "first";
            var focusNode = new FocusNode();
            var root = new TestRootElement(
                new Theme(
                    data: ThemeData.Light,
                    child: new Radio<string>(
                        value: "first",
                        groupValue: "first",
                        toggleable: true,
                        focusNode: focusNode,
                        onChanged: value => nextValue = value)));

            root.Attach(owner);
            root.Mount(parent: null, newSlot: null);
            owner.FlushBuild();

            Assert.True(focusNode.RequestFocus());

            bool handled = FocusManager.Instance.HandleKeyEvent(new KeyEvent(key: "Space", isDown: true));
            Assert.True(handled);
            owner.FlushBuild();

            Assert.Null(nextValue);
            focusNode.Dispose();
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void Radio_DefaultTapTarget_Padded_ExpandsHitArea()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new SizedBox(
                    width: 120,
                    child: new Radio<string>(
                        value: "first",
                        groupValue: "second",
                        onChanged: _ => { }))));

        harness.Pump(new Size(220, 120));

        var hitResult = new BoxHitTestResult();
        Assert.True(harness.RenderView.HitTest(hitResult, new Point(60, 46)));
    }

    [Fact]
    public void Radio_ThemeTapTarget_ShrinkWrap_DoesNotExpandHitArea()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with { MaterialTapTargetSize = MaterialTapTargetSize.ShrinkWrap },
                child: new SizedBox(
                    width: 120,
                    child: new Radio<string>(
                        value: "first",
                        groupValue: "second",
                        onChanged: _ => { }))));

        harness.Pump(new Size(220, 120));

        var hitResult = new BoxHitTestResult();
        Assert.False(harness.RenderView.HitTest(hitResult, new Point(60, 46)));
    }

    [Fact]
    public void Radio_AdaptiveIOS_Selected_UsesCupertinoDefaults()
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
                child: Radio<string>.Adaptive(
                    value: "first",
                    groupValue: "first",
                    onChanged: _ => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var outer = FindOuterDecoration(renderRoot);
        var dot = FindInnerDotDecoration(renderRoot);

        Assert.NotNull(outer);
        Assert.NotNull(dot);
        Assert.Equal(Color.FromArgb(255, 0, 122, 255), outer!.Decoration.Color);
        Assert.True(outer.Decoration.Border.HasValue);
        Assert.Equal(0, outer.Decoration.Border!.Value.Width);
        Assert.Equal(Colors.Transparent, outer.Decoration.Border.Value.Color);
        Assert.Equal(Colors.White, dot!.Decoration.Color);
    }

    [Fact]
    public void Radio_AdaptiveIOS_FillColorParameter_IsIgnored()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    Platform = TargetPlatform.IOS
                },
                child: Radio<string>.Adaptive(
                    value: "first",
                    groupValue: "first",
                    onChanged: _ => { },
                    activeColor: Colors.Orange,
                    fillColor: MaterialStateProperty<Color?>.All(Colors.MediumPurple))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var outer = FindOuterDecoration(renderRoot);
        var dot = FindInnerDotDecoration(renderRoot);

        Assert.NotNull(outer);
        Assert.NotNull(dot);
        Assert.Equal(Colors.Orange, outer!.Decoration.Color);
        Assert.Equal(Colors.White, dot!.Decoration.Color);
    }

    [Fact]
    public void Radio_AdaptiveIOS_UseCheckmarkStyle_RendersStrokeGlyph()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with
                {
                    Platform = TargetPlatform.IOS
                },
                child: Radio<string>.Adaptive(
                    value: "first",
                    groupValue: "first",
                    useCupertinoCheckmarkStyle: true,
                    onChanged: _ => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var glyph = FindDescendants<RenderStrokeGlyph>(renderRoot).FirstOrDefault();
        var dot = FindInnerDotDecoration(renderRoot);

        Assert.NotNull(glyph);
        Assert.Null(dot);
    }

    [Fact]
    public void Radio_AdaptiveMacOS_UsesCupertinoVisualWidth()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with
                {
                    Platform = TargetPlatform.MacOS
                },
                child: Radio<string>.Adaptive(
                    value: "first",
                    groupValue: "second",
                    onChanged: _ => { })));

        harness.Pump(new Size(220, 120));

        var radioBody = FindDecoratedBoxBySize(harness.RenderView, width: 18, height: 18, tolerance: 0.02);
        Assert.NotNull(radioBody);
    }

    [Fact]
    public void Radio_DefaultM2_UsesSecondaryUnselectedAndDisabledColors()
    {
        var selectedColor = Colors.Orange;
        var unselectedColor = Colors.SlateGray;
        var disabledColor = Colors.Brown;
        ThemeData theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(secondary: selectedColor),
            UnselectedWidgetColor = unselectedColor,
            DisabledColor = disabledColor
        };

        using var enabledHarness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new Radio<string>(
                    value: "a",
                    groupValue: "a",
                    onChanged: _ => { })));
        enabledHarness.Pump(new Size(80, 80));
        RadioPainter enabledPainter = FindRadioPainter(enabledHarness.RenderView);

        Assert.Equal(selectedColor, enabledPainter.ActiveColor);
        Assert.Equal(unselectedColor, enabledPainter.InactiveColor);

        using var disabledHarness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new Radio<string>(
                    value: "a",
                    groupValue: "a")));
        disabledHarness.Pump(new Size(80, 80));
        RadioPainter disabledPainter = FindRadioPainter(disabledHarness.RenderView);

        Assert.Equal(disabledColor, disabledPainter.ActiveColor);
        Assert.Equal(disabledColor, disabledPainter.InactiveColor);
    }

    [Fact]
    public void Radio_StatefulBackgroundSideAndInnerRadius_ResolveAllSelectionStates()
    {
        var activeBackground = Colors.Pink;
        var inactiveBackground = Colors.Beige;
        var activeSide = new BorderSide(Colors.Green, 4.0);
        var inactiveSide = new BorderSide(Colors.Blue, 3.0);
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new Radio<string>(
                    value: "a",
                    groupValue: "a",
                    onChanged: _ => { },
                    backgroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                        states.HasFlag(MaterialState.Selected) ? activeBackground : inactiveBackground),
                    side: WidgetStateBorderSide.ResolveWith(states =>
                        states.HasFlag(MaterialState.Selected) ? activeSide : inactiveSide),
                    innerRadius: MaterialStateProperty<double?>.ResolveWith(states =>
                        states.HasFlag(MaterialState.Selected) ? 6.0 : 2.0))));

        harness.Pump(new Size(80, 80));
        RadioPainter painter = FindRadioPainter(harness.RenderView);

        Assert.Equal(activeBackground, painter.ActiveBackgroundColor);
        Assert.Equal(inactiveBackground, painter.InactiveBackgroundColor);
        Assert.Equal(activeSide, painter.ActiveSide);
        Assert.Equal(inactiveSide, painter.InactiveSide);
        Assert.Equal(6.0, painter.InnerRadius);
    }

    [Fact]
    public void Radio_PlainSide_AppliesOnlyToUnselectedState()
    {
        var side = new BorderSide(Colors.Orange, 5.0);
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new Radio<string>(
                    value: "a",
                    groupValue: "a",
                    side: side,
                    onChanged: _ => { })));

        harness.Pump(new Size(80, 80));
        RadioPainter painter = FindRadioPainter(harness.RenderView);

        Assert.Equal(side, painter.InactiveSide);
        Assert.NotEqual(side, painter.ActiveSide);
        Assert.Equal(2.0, painter.ActiveSide.Width);
    }

    [Fact]
    public void Radio_VisualDensity_AdjustsPaddedTargetSize()
    {
        (VisualDensity density, Size expected)[] cases =
        [
            (VisualDensity.Standard, new Size(48.0, 48.0)),
            (new VisualDensity(3.0, 3.0), new Size(60.0, 60.0)),
            (new VisualDensity(-3.0, -3.0), new Size(36.0, 36.0)),
            (new VisualDensity(3.0, -3.0), new Size(60.0, 36.0)),
        ];

        foreach ((VisualDensity density, Size expected) in cases)
        {
            using var harness = new WidgetRenderHarness(
                new Theme(
                    data: ThemeData.Light,
                    child: new Radio<string>(
                        value: "a",
                        groupValue: "b",
                        visualDensity: density,
                        onChanged: _ => { })));
            harness.Pump(new Size(100, 100));

            RenderCustomPaint customPaint = Assert.Single(
                FindDescendants<RenderCustomPaint>(harness.RenderView));
            Assert.Equal(expected, customPaint.Size);
        }
    }

    [Fact]
    public void RadioThemeData_CopyWithAndLerp_CoverCompleteThemeSurface()
    {
        var cursor = MaterialStateProperty<MouseCursor?>.All(SystemMouseCursors.Click);
        var fill = MaterialStateProperty<Color?>.All(Colors.Red);
        var overlay = MaterialStateProperty<Color?>.All(Colors.Blue);
        var background = MaterialStateProperty<Color?>.All(Colors.Green);
        var innerRadius = MaterialStateProperty<double?>.All(3.0);
        var side = WidgetStateBorderSide.All(new BorderSide(Colors.Purple, 2.0));
        var data = new RadioThemeData(
            MouseCursor: cursor,
            FillColor: fill,
            OverlayColor: overlay,
            SplashRadius: 24.0,
            MaterialTapTargetSize: MaterialTapTargetSize.ShrinkWrap,
            VisualDensity: new VisualDensity(1.0, -1.0),
            BackgroundColor: background,
            Side: side,
            InnerRadius: innerRadius);

        Assert.Equal(data, data.CopyWith());
        Assert.Same(data, RadioThemeData.Lerp(data, data, 0.25));

        RadioThemeData midpoint = RadioThemeData.Lerp(
            new RadioThemeData(
                SplashRadius: 10.0,
                Side: WidgetStateBorderSide.All(new BorderSide(Colors.Red, 1.0))),
            new RadioThemeData(
                SplashRadius: 30.0,
                Side: WidgetStateBorderSide.All(new BorderSide(Colors.Blue, 3.0))),
            0.5);
        Assert.Equal(20.0, midpoint.SplashRadius);
        BorderSide midpointSide = midpoint.Side!.Resolve(MaterialState.None)!.Value;
        Assert.Equal(2.0, midpointSide.Width);
    }

    [Fact]
    public void LocalRadioTheme_OverridesGlobalThemeAndWidgetOverridesLocal()
    {
        using var themedHarness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with
                {
                    RadioTheme = new RadioThemeData(
                        FillColor: MaterialStateProperty<Color?>.All(Colors.Red))
                },
                child: new RadioTheme(
                    data: new RadioThemeData(
                        FillColor: MaterialStateProperty<Color?>.All(Colors.Blue)),
                    child: new Radio<string>(
                        value: "a",
                        groupValue: "a",
                        onChanged: _ => { }))));
        themedHarness.Pump(new Size(80, 80));
        Assert.Equal(Colors.Blue, FindRadioPainter(themedHarness.RenderView).ActiveColor);

        using var widgetHarness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new RadioTheme(
                    data: new RadioThemeData(
                        FillColor: MaterialStateProperty<Color?>.All(Colors.Blue)),
                    child: new Radio<string>(
                        value: "a",
                        groupValue: "a",
                        fillColor: MaterialStateProperty<Color?>.All(Colors.Green),
                        onChanged: _ => { }))));
        widgetHarness.Pump(new Size(80, 80));
        Assert.Equal(Colors.Green, FindRadioPainter(widgetHarness.RenderView).ActiveColor);
    }

    [Fact]
    public void Radio_DoesNotCrashAtZeroArea()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new SizedBox(
                    width: 0.0,
                    height: 0.0,
                    child: new Radio<string>(
                        value: "a",
                        groupValue: "b",
                        onChanged: _ => { }))));

        var zeroSize = new Size(0.0, 0.0);
        harness.Pump(zeroSize);
        RenderCustomPaint customPaint = Assert.Single(
            FindDescendants<RenderCustomPaint>(harness.RenderView));
        Assert.Equal(zeroSize, customPaint.Size);
    }

    private static T RequireRenderObject<T>(Element? element) where T : RenderObject
    {
        Assert.NotNull(element);
        Assert.NotNull(element!.RenderObject);
        return Assert.IsAssignableFrom<T>(element.RenderObject);
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

    private static RenderDecoratedBox? FindOuterDecoration(RenderObject root)
    {
        return FindDescendants<RenderDecoratedBox>(root)
            .FirstOrDefault(box => box.Decoration.Border.HasValue);
    }

    private static RenderDecoratedBox? FindInnerDotDecoration(RenderObject root)
    {
        return FindDescendants<RenderDecoratedBox>(root)
            .FirstOrDefault(box =>
                !box.Decoration.Border.HasValue
                && box.Decoration.Color.HasValue
                && box.Decoration.Color.Value.A > 0
                && box.Decoration.Color.Value != Colors.Transparent);
    }

    private static RadioPainter FindRadioPainter(RenderObject root)
    {
        RenderCustomPaint customPaint = Assert.IsType<RenderCustomPaint>(
            FindDescendants<RenderCustomPaint>(root).Single());
        return Assert.IsType<RadioPainter>(customPaint.Painter);
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

    private static RenderPointerListener? FindFocusPointerListener(RenderObject? root)
    {
        if (root is null)
        {
            return null;
        }

        if (root is RenderPointerListener listener
            && listener.OnPointerDown != null
            && listener.OnPointerUp == null
            && listener.OnPointerCancel == null
            && listener.OnPointerEnter == null
            && listener.OnPointerExit == null)
        {
            return listener;
        }

        RenderPointerListener? result = null;
        root.VisitChildren(child =>
        {
            if (result is not null)
            {
                return;
            }

            result = FindFocusPointerListener(child);
        });

        return result;
    }

    private static Color ApplyOpacity(Color color, double opacity)
    {
        byte alpha = (byte)Math.Clamp((int)Math.Round(255 * opacity), 0, 255);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
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
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (ReferenceEquals(_renderView.Child, child))
                {
                    _renderView.Child = null;
                }
            }
        }
    }
}
