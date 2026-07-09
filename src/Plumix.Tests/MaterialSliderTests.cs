using System.Reflection;
using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialSliderTests
{
    [Fact]
    public void Slider_Constructor_Throws_OnInvalidArguments()
    {
        Assert.Throws<ArgumentException>(() => new Slider(value: 0.5, min: 1, max: 0, onChanged: _ => { }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Slider(value: -0.1, min: 0, max: 1, onChanged: _ => { }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Slider(value: 0.5, min: 0, max: 1, divisions: 0, onChanged: _ => { }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Slider(value: double.NaN, min: 0, max: 1, onChanged: _ => { }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Slider(value: 0.5, min: 0, max: 1, secondaryTrackValue: 1.1, onChanged: _ => { }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Slider(value: 0.5, min: 0, max: 1, secondaryTrackValue: double.NaN, onChanged: _ => { }));
    }

    [Fact]
    public void Slider_DefaultM3_UsesPrimaryAndSurfaceContainerHighestColors()
    {
        var theme = ThemeData.Light with
        {
            UseMaterial3 = true,
            PrimaryColor = Colors.Coral,
            SurfaceContainerHighestColor = Colors.PowderBlue
        };

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new SizedBox(
                    width: 220,
                    child: new Slider(
                        value: 0.4,
                        onChanged: _ => { }))));

        harness.Pump(new Size(260, 120));

        object? render = FindDescendantByTypeName(harness.RenderView, "RenderSlider");
        Assert.NotNull(render);
        Assert.Equal(Colors.Coral, ReadProperty<Color>(render!, "ActiveTrackColor"));
        Assert.Equal(Colors.PowderBlue, ReadProperty<Color>(render, "InactiveTrackColor"));
        Assert.Equal(Colors.Coral, ReadProperty<Color>(render, "ThumbColor"));
    }

    [Fact]
    public void Slider_DefaultM2_UsesPrimaryTrackWithOpacityForInactive()
    {
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            PrimaryColor = Colors.CadetBlue
        };

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new SizedBox(
                    width: 220,
                    child: new Slider(
                        value: 0.4,
                        onChanged: _ => { }))));

        harness.Pump(new Size(260, 120));

        object? render = FindDescendantByTypeName(harness.RenderView, "RenderSlider");
        Assert.NotNull(render);
        Assert.Equal(Colors.CadetBlue, ReadProperty<Color>(render!, "ActiveTrackColor"));
        Assert.Equal(ApplyOpacity(Colors.CadetBlue, 0.24), ReadProperty<Color>(render, "InactiveTrackColor"));
    }

    [Fact]
    public void Slider_SecondaryTrack_DefaultAndNormalizationFollowFlutterParity()
    {
        var theme = ThemeData.Light with
        {
            UseMaterial3 = true,
            PrimaryColor = Colors.CadetBlue
        };

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new SizedBox(
                    width: 220,
                    child: new Slider(
                        value: 15,
                        min: 0,
                        max: 20,
                        secondaryTrackValue: 18,
                        onChanged: _ => { }))));

        harness.Pump(new Size(260, 120));

        object? render = FindDescendantByTypeName(harness.RenderView, "RenderSlider");
        Assert.NotNull(render);
        Assert.Equal(0.9, ReadProperty<double>(render!, "SecondaryTrackValueNormalized"), 3);
        Assert.Equal(ApplyOpacity(Colors.CadetBlue, 0.54), ReadProperty<Color>(render, "SecondaryActiveTrackColor"));
    }

    [Fact]
    public void Slider_SecondaryTrack_ThemeAndWidgetColorsFollowPrecedence()
    {
        var theme = ThemeData.Light with
        {
            SliderTheme = new SliderThemeData(
                SecondaryActiveTrackColor: Colors.OrangeRed)
        };

        using var themeHarness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new SizedBox(
                    width: 220,
                    child: new Slider(
                        value: 0.3,
                        secondaryTrackValue: 0.8,
                        onChanged: _ => { }))));

        themeHarness.Pump(new Size(260, 120));
        object? themeRender = FindDescendantByTypeName(themeHarness.RenderView, "RenderSlider");
        Assert.NotNull(themeRender);
        Assert.Equal(Colors.OrangeRed, ReadProperty<Color>(themeRender!, "SecondaryActiveTrackColor"));

        using var widgetHarness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new SizedBox(
                    width: 220,
                    child: new Slider(
                        value: 0.3,
                        secondaryTrackValue: 0.8,
                        secondaryActiveColor: Colors.MediumPurple,
                        onChanged: _ => { }))));

        widgetHarness.Pump(new Size(260, 120));
        object? widgetRender = FindDescendantByTypeName(widgetHarness.RenderView, "RenderSlider");
        Assert.NotNull(widgetRender);
        Assert.Equal(Colors.MediumPurple, ReadProperty<Color>(widgetRender!, "SecondaryActiveTrackColor"));
    }

    [Fact]
    public void Slider_SecondaryTrack_DisabledUsesDisabledThemeColor()
    {
        var theme = ThemeData.Light with
        {
            SliderTheme = new SliderThemeData(
                DisabledSecondaryActiveTrackColor: Colors.Gainsboro)
        };

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new SizedBox(
                    width: 220,
                    child: new Slider(
                        value: 0.4,
                        secondaryTrackValue: 0.9,
                        onChanged: null))));

        harness.Pump(new Size(260, 120));

        object? render = FindDescendantByTypeName(harness.RenderView, "RenderSlider");
        Assert.NotNull(render);
        Assert.Equal(Colors.Gainsboro, ReadProperty<Color>(render!, "SecondaryActiveTrackColor"));
    }

    [Fact]
    public void Slider_ThemeColors_Apply_WhenWidgetColorsAreMissing()
    {
        var theme = ThemeData.Light with
        {
            SliderTheme = new SliderThemeData(
                ActiveTrackColor: Colors.DarkGreen,
                InactiveTrackColor: Colors.LightGreen,
                ThumbColor: Colors.Gold)
        };

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new SizedBox(
                    width: 220,
                    child: new Slider(
                        value: 0.3,
                        onChanged: _ => { }))));

        harness.Pump(new Size(260, 120));

        object? render = FindDescendantByTypeName(harness.RenderView, "RenderSlider");
        Assert.NotNull(render);
        Assert.Equal(Colors.DarkGreen, ReadProperty<Color>(render!, "ActiveTrackColor"));
        Assert.Equal(Colors.LightGreen, ReadProperty<Color>(render, "InactiveTrackColor"));
        Assert.Equal(Colors.Gold, ReadProperty<Color>(render, "ThumbColor"));
    }

    [Fact]
    public void Slider_WidgetColors_OverrideThemeColors()
    {
        var theme = ThemeData.Light with
        {
            SliderTheme = new SliderThemeData(
                ActiveTrackColor: Colors.DarkGreen,
                InactiveTrackColor: Colors.LightGreen,
                ThumbColor: Colors.Gold)
        };

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new SizedBox(
                    width: 220,
                    child: new Slider(
                        value: 0.3,
                        activeColor: Colors.DarkRed,
                        inactiveColor: Colors.MistyRose,
                        thumbColor: Colors.DarkMagenta,
                        onChanged: _ => { }))));

        harness.Pump(new Size(260, 120));

        object? render = FindDescendantByTypeName(harness.RenderView, "RenderSlider");
        Assert.NotNull(render);
        Assert.Equal(Colors.DarkRed, ReadProperty<Color>(render!, "ActiveTrackColor"));
        Assert.Equal(Colors.MistyRose, ReadProperty<Color>(render, "InactiveTrackColor"));
        Assert.Equal(Colors.DarkMagenta, ReadProperty<Color>(render, "ThumbColor"));
    }

    [Fact]
    public void Slider_Drag_InvokesOnChangeStartOnChangedAndOnChangeEnd_WithDiscreteSnapping()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        try
        {
            double? start = null;
            double? end = null;
            double changed = 0;

            using var harness = new WidgetRenderHarness(
                new Theme(
                    data: ThemeData.Light,
                    child: new Align(
                        alignment: Alignment.TopLeft,
                        child: new SizedBox(
                            width: 220,
                            child: new Slider(
                                value: 0.2,
                                divisions: 5,
                                onChangeStart: value => start = value,
                                onChanged: value => changed = value,
                                onChangeEnd: value => end = value)))));

            harness.Pump(new Size(280, 120));

            DispatchPointerDown(binding, harness.RenderView, pointer: 700, position: new Point(20, 24));
            DispatchPointerMove(binding, harness.RenderView, pointer: 700, position: new Point(214, 24));
            DispatchPointerUp(binding, harness.RenderView, pointer: 700, position: new Point(214, 24));
            harness.Pump(new Size(280, 120));

            Assert.NotNull(start);
            Assert.NotNull(end);
            Assert.Equal(0.2, start!.Value, 3);
            Assert.Equal(1.0, end!.Value, 3);
            Assert.Equal(1.0, changed, 3);
        }
        finally
        {
            binding.ResetForTests();
        }
    }

    [Fact]
    public void Slider_KeyboardArrowRight_IncrementsValueInLtr()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            var focusNode = new FocusNode();
            double next = 0.0;

            var owner = new BuildOwner();
            var root = new TestRootElement(
                new Theme(
                    data: new ThemeData(platform: TargetPlatform.MacOS),
                    child: new SizedBox(
                        width: 220,
                        child: new Slider(
                            value: 0.40,
                            focusNode: focusNode,
                            onChanged: value => next = value))));

            root.Attach(owner);
            root.Mount(parent: null, newSlot: null);
            owner.FlushBuild();

            Assert.True(focusNode.RequestFocus());
            owner.FlushBuild();

            bool handled = FocusManager.Instance.HandleKeyEvent(new KeyEvent(key: "ArrowRight", isDown: true));
            Assert.True(handled);
            owner.FlushBuild();

            Assert.Equal(0.5, next, 3);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void Slider_KeyboardArrowLeft_InRtl_IncrementsValue()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            var focusNode = new FocusNode();
            double next = 0.0;

            var owner = new BuildOwner();
            var root = new TestRootElement(
                new Theme(
                    data: new ThemeData(platform: TargetPlatform.MacOS),
                    child: new Directionality(
                        textDirection: TextDirection.Rtl,
                        child: new SizedBox(
                            width: 220,
                            child: new Slider(
                                value: 0.40,
                                focusNode: focusNode,
                                onChanged: value => next = value)))));

            root.Attach(owner);
            root.Mount(parent: null, newSlot: null);
            owner.FlushBuild();

            Assert.True(focusNode.RequestFocus());
            owner.FlushBuild();

            bool handled = FocusManager.Instance.HandleKeyEvent(new KeyEvent(key: "ArrowLeft", isDown: true));
            Assert.True(handled);
            owner.FlushBuild();

            Assert.Equal(0.5, next, 3);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void Slider_Semantics_ExposeSliderFlagEnabledFlagAndLabel()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            using var harness = new WidgetRenderHarness(
                new Theme(
                    data: ThemeData.Light,
                    child: new SizedBox(
                        width: 220,
                        child: new Slider(
                            value: 0.5,
                            onChanged: _ => { },
                            semanticLabel: "Volume"))));

            var semanticsRoot = harness.PumpAndGetSemantics(new Size(260, 120));
            Assert.NotNull(semanticsRoot);

            var semanticsNode = FindFirstSemanticsNode(
                semanticsRoot!,
                static node => node.Label == "Volume");
            Assert.NotNull(semanticsNode);
            Assert.True(semanticsNode!.Flags.HasFlag(SemanticsFlags.IsSlider));
            Assert.True(semanticsNode.Flags.HasFlag(SemanticsFlags.IsEnabled));
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void Slider_SemanticFormatterCallback_OverridesSemanticLabel()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            using var harness = new WidgetRenderHarness(
                new Theme(
                    data: ThemeData.Light,
                    child: new SizedBox(
                        width: 220,
                        child: new Slider(
                            value: 0.5,
                            onChanged: _ => { },
                            semanticLabel: "Volume",
                            semanticFormatterCallback: value => $"{Math.Round(value * 100)} percent"))));

            var semanticsRoot = harness.PumpAndGetSemantics(new Size(260, 120));
            Assert.NotNull(semanticsRoot);

            var semanticsNode = FindFirstSemanticsNode(
                semanticsRoot!,
                static node => node.Label == "50 percent");
            Assert.NotNull(semanticsNode);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    private static object? FindDescendantByTypeName(RenderObject? root, string typeName)
    {
        if (root is null)
        {
            return null;
        }

        if (root.GetType().Name == typeName)
        {
            return root;
        }

        object? result = null;
        root.VisitChildren(child =>
        {
            if (result is not null)
            {
                return;
            }

            result = FindDescendantByTypeName(child, typeName);
        });

        return result;
    }

    private static T ReadProperty<T>(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property);

        object? value = property!.GetValue(target);
        Assert.NotNull(value);
        return (T)value!;
    }

    private static Color ApplyOpacity(Color color, double opacity)
    {
        byte alpha = (byte)Math.Clamp((int)(255 * opacity), 0, 255);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    private static void DispatchPointerDown(GestureBinding binding, RenderView renderView, int pointer, Point position)
    {
        binding.HandlePointerEvent(
            renderView,
            new PointerDownEvent(
                pointer: pointer,
                kind: PointerDeviceKind.Mouse,
                position: position,
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow));
    }

    private static void DispatchPointerMove(GestureBinding binding, RenderView renderView, int pointer, Point position)
    {
        binding.HandlePointerEvent(
            renderView,
            new PointerMoveEvent(
                pointer: pointer,
                kind: PointerDeviceKind.Mouse,
                position: position,
                buttons: PointerButtons.Primary,
                down: true,
                timestampUtc: DateTime.UtcNow));
    }

    private static void DispatchPointerUp(GestureBinding binding, RenderView renderView, int pointer, Point position)
    {
        binding.HandlePointerEvent(
            renderView,
            new PointerUpEvent(
                pointer: pointer,
                kind: PointerDeviceKind.Mouse,
                position: position,
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow));
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
                if (_child is not null)
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
                if (_child is not null)
                {
                    UnmountChild(_child);
                    _child = null;
                }

                base.Unmount();
            }
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
            if (_child is not null)
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
            if (_child is not null)
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
