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
public sealed class MaterialRangeSliderTests
{
    [Fact]
    public void RangeSlider_Constructor_Throws_OnInvalidArguments()
    {
        Assert.Throws<ArgumentException>(() => new RangeSlider(values: new RangeValues(0.2, 0.8), min: 1, max: 0, onChanged: _ => { }));
        Assert.Throws<ArgumentException>(() => new RangeSlider(values: new RangeValues(0.8, 0.2), min: 0, max: 1, onChanged: _ => { }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RangeSlider(values: new RangeValues(-0.1, 0.2), min: 0, max: 1, onChanged: _ => { }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RangeSlider(values: new RangeValues(0.2, 1.1), min: 0, max: 1, onChanged: _ => { }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RangeSlider(values: new RangeValues(0.2, 0.7), min: 0, max: 1, divisions: 0, onChanged: _ => { }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RangeSlider(values: new RangeValues(double.NaN, 0.7), min: 0, max: 1, onChanged: _ => { }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RangeSlider(
            values: new RangeValues(0.2, 0.7),
            onChanged: _ => { },
            padding: new Thickness(0, -1, 0, 0)));
    }

    [Fact]
    public void RangeSlider_ExtendedApi_StoresFlutterParityValues()
    {
        var cursor = MaterialStateProperty<MouseCursor?>.All(new SystemMouseCursor("range-slider"));
        var slider = new RangeSlider(
            values: new RangeValues(0.2, 0.8),
            onChanged: _ => { },
            divisions: 5,
            labels: new RangeLabels("20", "80"),
            mouseCursor: cursor,
            padding: new Thickness(10, 4),
            year2023: false);

        Assert.Equal(new RangeLabels("20", "80"), slider.Labels);
        Assert.Same(cursor, slider.MouseCursor);
        Assert.Equal(new Thickness(10, 4), slider.Padding);
        Assert.False(slider.Year2023);
    }

    [Fact]
    public void RangeSlider_ExtendedThemeTokensReachRenderObject()
    {
        var theme = ThemeData.Light with
        {
            SliderTheme = new SliderThemeData(
                ActiveTickMarkColor: Colors.Gold,
                InactiveTickMarkColor: Colors.DarkSlateBlue,
                TickMarkRadius: 2.5,
                ValueIndicatorColor: Colors.OrangeRed,
                ShowValueIndicator: ShowValueIndicator.AlwaysVisible,
                Padding: new Thickness(16, 6),
                TrackGap: 8,
                Year2023: false)
        };

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new SizedBox(
                    width: 220,
                    child: new RangeSlider(
                        values: new RangeValues(0.2, 0.8),
                        divisions: 5,
                        labels: new RangeLabels("20", "80"),
                        onChanged: _ => { }))));

        harness.Pump(new Size(260, 120));
        object? render = FindDescendantByTypeName(harness.RenderView, "RenderRangeSlider");
        Assert.NotNull(render);
        Assert.Equal(Colors.Gold, ReadProperty<Color>(render!, "ActiveTickMarkColor"));
        Assert.Equal(Colors.DarkSlateBlue, ReadProperty<Color>(render, "InactiveTickMarkColor"));
        Assert.Equal(2.5, ReadProperty<double>(render, "TickMarkRadius"));
        Assert.Equal(Colors.OrangeRed, ReadProperty<Color>(render, "ValueIndicatorColor"));
        Assert.Equal(ShowValueIndicator.AlwaysVisible, ReadProperty<ShowValueIndicator>(render, "ShowValueIndicator"));
        Assert.Equal(new Thickness(16, 6), ReadProperty<Thickness>(render, "Padding"));
        Assert.Equal(8, ReadProperty<double>(render, "TrackGap"));
        Assert.Equal(new Size(4, 44), ReadProperty<Size>(render, "ThumbSize"));
        Assert.Equal(16, ReadProperty<double>(render, "TrackHeight"));
        Assert.Equal(theme.SecondaryContainerColor, ReadProperty<Color>(render, "InactiveTrackColor"));
    }

    [Fact]
    public void RangeSlider_DefaultM3Year2023_UsesM2TrackColors()
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
                    child: new RangeSlider(
                        values: new RangeValues(0.2, 0.7),
                        onChanged: _ => { }))));

        harness.Pump(new Size(260, 120));

        object? render = FindDescendantByTypeName(harness.RenderView, "RenderRangeSlider");
        Assert.NotNull(render);
        Assert.Equal(Colors.Coral, ReadProperty<Color>(render!, "ActiveTrackColor"));
        Assert.Equal(ApplyOpacity(Colors.Coral, 0.24), ReadProperty<Color>(render, "InactiveTrackColor"));
        Assert.Equal(Colors.Coral, ReadProperty<Color>(render, "ThumbColor"));
    }

    [Fact]
    public void RangeSlider_ThemeColors_Apply_WhenWidgetColorsAreMissing()
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
                    child: new RangeSlider(
                        values: new RangeValues(0.2, 0.7),
                        onChanged: _ => { }))));

        harness.Pump(new Size(260, 120));

        object? render = FindDescendantByTypeName(harness.RenderView, "RenderRangeSlider");
        Assert.NotNull(render);
        Assert.Equal(Colors.DarkGreen, ReadProperty<Color>(render!, "ActiveTrackColor"));
        Assert.Equal(Colors.LightGreen, ReadProperty<Color>(render, "InactiveTrackColor"));
        Assert.Equal(Colors.Gold, ReadProperty<Color>(render, "ThumbColor"));
    }

    [Fact]
    public void RangeSlider_WidgetColors_OverrideThemeColors()
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
                    child: new RangeSlider(
                        values: new RangeValues(0.2, 0.7),
                        activeColor: Colors.DarkRed,
                        inactiveColor: Colors.MistyRose,
                        onChanged: _ => { }))));

        harness.Pump(new Size(260, 120));

        object? render = FindDescendantByTypeName(harness.RenderView, "RenderRangeSlider");
        Assert.NotNull(render);
        Assert.Equal(Colors.DarkRed, ReadProperty<Color>(render!, "ActiveTrackColor"));
        Assert.Equal(Colors.MistyRose, ReadProperty<Color>(render, "InactiveTrackColor"));
        Assert.Equal(Colors.DarkRed, ReadProperty<Color>(render, "ThumbColor"));
    }

    [Fact]
    public void RangeSlider_DragStartThumb_InvokesLifecycleCallbacksAndUpdatesStartValue()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        try
        {
            RangeValues? start = null;
            RangeValues? changed = null;
            RangeValues? end = null;

            using var harness = new WidgetRenderHarness(
                new Theme(
                    data: ThemeData.Light,
                    child: new Align(
                        alignment: Alignment.TopLeft,
                        child: new SizedBox(
                            width: 220,
                            child: new RangeSlider(
                                values: new RangeValues(0.2, 0.7),
                                onChangeStart: values => start = values,
                                onChanged: values => changed = values,
                                onChangeEnd: values => end = values)))));

            harness.Pump(new Size(280, 120));

            DispatchPointerDown(binding, harness.RenderView, pointer: 700, position: new Point(50, 24));
            DispatchPointerMove(binding, harness.RenderView, pointer: 700, position: new Point(90, 24));
            DispatchPointerUp(binding, harness.RenderView, pointer: 700, position: new Point(90, 24));
            harness.Pump(new Size(280, 120));

            Assert.NotNull(start);
            Assert.NotNull(changed);
            Assert.NotNull(end);
            Assert.Equal(0.2, start!.Value.Start, 2);
            Assert.Equal(0.7, start.Value.End, 2);
            Assert.Equal(0.4, changed!.Value.Start, 2);
            Assert.Equal(0.7, changed.Value.End, 2);
            Assert.Equal(0.4, end!.Value.Start, 2);
            Assert.Equal(0.7, end.Value.End, 2);
        }
        finally
        {
            binding.ResetForTests();
        }
    }

    [Fact]
    public void RangeSlider_DiscreteDrag_SnapsToDivisions()
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        try
        {
            RangeValues? changed = null;

            using var harness = new WidgetRenderHarness(
                new Theme(
                    data: ThemeData.Light,
                    child: new Align(
                        alignment: Alignment.TopLeft,
                        child: new SizedBox(
                            width: 220,
                            child: new RangeSlider(
                                values: new RangeValues(0.2, 0.6),
                                divisions: 5,
                                onChanged: values => changed = values)))));

            harness.Pump(new Size(280, 120));

            DispatchPointerDown(binding, harness.RenderView, pointer: 701, position: new Point(130, 24));
            DispatchPointerMove(binding, harness.RenderView, pointer: 701, position: new Point(150, 24));
            DispatchPointerUp(binding, harness.RenderView, pointer: 701, position: new Point(150, 24));
            harness.Pump(new Size(280, 120));

            Assert.NotNull(changed);
            Assert.Equal(0.2, changed!.Value.Start, 2);
            Assert.Equal(0.8, changed.Value.End, 2);
        }
        finally
        {
            binding.ResetForTests();
        }
    }

    [Fact]
    public void RangeSlider_KeyboardArrowRight_IncrementsEndValueInLtr()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            var focusNode = new FocusNode();
            RangeValues next = new(0, 0);

            var owner = new BuildOwner();
            var root = new TestRootElement(
                new Theme(
                    data: new ThemeData(platform: TargetPlatform.MacOS),
                    child: new SizedBox(
                        width: 220,
                        child: new RangeSlider(
                            values: new RangeValues(0.2, 0.6),
                            focusNode: focusNode,
                            onChanged: values => next = values))));

            root.Attach(owner);
            root.Mount(parent: null, newSlot: null);
            owner.FlushBuild();

            Assert.True(focusNode.RequestFocus());
            owner.FlushBuild();

            bool handled = FocusManager.Instance.HandleKeyEvent(new KeyEvent(key: "ArrowRight", isDown: true));
            Assert.True(handled);
            owner.FlushBuild();

            Assert.Equal(0.2, next.Start, 3);
            Assert.Equal(0.7, next.End, 3);
        }
        finally
        {
            FocusManager.Instance.ResetForTests();
        }
    }

    [Fact]
    public void RangeSlider_Semantics_ExposeSliderFlagEnabledFlagAndFormattedLabel()
    {
        FocusManager.Instance.ResetForTests();
        try
        {
            using var harness = new WidgetRenderHarness(
                new Theme(
                    data: ThemeData.Light,
                    child: new SizedBox(
                        width: 220,
                        child: new RangeSlider(
                            values: new RangeValues(0.2, 0.7),
                            onChanged: _ => { },
                            semanticFormatterCallback: value => $"{Math.Round(value * 100):0}%"))));

            var semanticsRoot = harness.PumpAndGetSemantics(new Size(260, 120));
            Assert.NotNull(semanticsRoot);

            var semanticsNode = FindFirstSemanticsNode(
                semanticsRoot!,
                static node => !string.IsNullOrWhiteSpace(node.Label) && node.Label!.Contains("20% - 70%", StringComparison.Ordinal));
            Assert.NotNull(semanticsNode);
            Assert.True(semanticsNode!.Flags.HasFlag(SemanticsFlags.IsSlider));
            Assert.True(semanticsNode.Flags.HasFlag(SemanticsFlags.IsEnabled));
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
