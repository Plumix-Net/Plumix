using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialLinearProgressIndicatorTests
{
    [Fact]
    public void LinearProgressIndicator_Constructors_Throw_OnInvalidNumericValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LinearProgressIndicator(value: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LinearProgressIndicator(value: double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LinearProgressIndicator(minHeight: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LinearProgressIndicator(minHeight: -1));
    }

    [Fact]
    public void LinearProgressIndicator_DefaultM3_UsesPrimarySecondaryContainerAndRoundedTrack()
    {
        var theme = ThemeData.Light with
        {
            UseMaterial3 = true,
            PrimaryColor = Colors.DarkOrange,
            SecondaryContainerColor = Colors.LightBlue
        };

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new SizedBox(
                    width: 200,
                    child: new LinearProgressIndicator(value: 0.5))));

        harness.Pump(new Size(240, 80));

        var renderIndicator = FindDescendantByTypeName(harness.RenderView, "RenderLinearProgressIndicator");
        Assert.NotNull(renderIndicator);

        Assert.Equal(Colors.DarkOrange, ReadProperty<Color>(renderIndicator!, "ValueColor"));
        Assert.Equal(Colors.LightBlue, ReadProperty<Color>(renderIndicator, "TrackColor"));
        Assert.Equal(4.0, ReadProperty<double>(renderIndicator, "MinHeight"), 3);

        var radius = ReadProperty<BorderRadius>(renderIndicator, "BorderRadius");
        Assert.Equal(2.0, radius.Radius, 3);
    }

    [Fact]
    public void LinearProgressIndicator_DefaultM2_UsesPrimaryCanvasAndSquareTrack()
    {
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            PrimaryColor = Colors.MediumVioletRed,
            CanvasColor = Colors.Wheat
        };

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new SizedBox(
                    width: 200,
                    child: new LinearProgressIndicator(value: 0.5))));

        harness.Pump(new Size(240, 80));

        var renderIndicator = FindDescendantByTypeName(harness.RenderView, "RenderLinearProgressIndicator");
        Assert.NotNull(renderIndicator);

        Assert.Equal(Colors.MediumVioletRed, ReadProperty<Color>(renderIndicator!, "ValueColor"));
        Assert.Equal(Colors.Wheat, ReadProperty<Color>(renderIndicator, "TrackColor"));

        var radius = ReadProperty<BorderRadius>(renderIndicator, "BorderRadius");
        Assert.Equal(0.0, radius.Radius, 3);
    }

    [Fact]
    public void LinearProgressIndicator_ResolvesPrecedence_WidgetOverThemeAndThemeOverDefaults()
    {
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.Orange,
            SecondaryContainerColor = Colors.SkyBlue,
            ProgressIndicatorTheme = new ProgressIndicatorThemeData(
                Color: Colors.Green,
                LinearTrackColor: Colors.MediumPurple,
                LinearMinHeight: 6,
                BorderRadius: BorderRadius.Circular(3))
        };

        using var themedHarness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new SizedBox(
                    width: 200,
                    child: new LinearProgressIndicator(value: 0.5))));

        themedHarness.Pump(new Size(240, 80));

        var themedRender = FindDescendantByTypeName(themedHarness.RenderView, "RenderLinearProgressIndicator");
        Assert.NotNull(themedRender);

        Assert.Equal(Colors.Green, ReadProperty<Color>(themedRender!, "ValueColor"));
        Assert.Equal(Colors.MediumPurple, ReadProperty<Color>(themedRender, "TrackColor"));
        Assert.Equal(6.0, ReadProperty<double>(themedRender, "MinHeight"), 3);
        Assert.Equal(3.0, ReadProperty<BorderRadius>(themedRender, "BorderRadius").Radius, 3);

        using var widgetHarness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new SizedBox(
                    width: 200,
                    child: new LinearProgressIndicator(
                        value: 0.5,
                        color: Colors.DarkRed,
                        backgroundColor: Colors.LightGoldenrodYellow,
                        minHeight: 8,
                        borderRadius: BorderRadius.Circular(4)))));

        widgetHarness.Pump(new Size(240, 80));

        var widgetRender = FindDescendantByTypeName(widgetHarness.RenderView, "RenderLinearProgressIndicator");
        Assert.NotNull(widgetRender);

        Assert.Equal(Colors.DarkRed, ReadProperty<Color>(widgetRender!, "ValueColor"));
        Assert.Equal(Colors.LightGoldenrodYellow, ReadProperty<Color>(widgetRender, "TrackColor"));
        Assert.Equal(8.0, ReadProperty<double>(widgetRender, "MinHeight"), 3);
        Assert.Equal(4.0, ReadProperty<BorderRadius>(widgetRender, "BorderRadius").Radius, 3);
    }

    [Fact]
    public void LinearProgressIndicator_DeterminateValue_IsClamped()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new SizedBox(
                    width: 200,
                    child: new LinearProgressIndicator(value: 1.5))));

        harness.Pump(new Size(240, 80));

        var renderIndicator = FindDescendantByTypeName(harness.RenderView, "RenderLinearProgressIndicator");
        Assert.NotNull(renderIndicator);

        var clampedValue = ReadProperty<double?>(renderIndicator!, "Value");
        Assert.True(clampedValue.HasValue);
        Assert.Equal(1.0, clampedValue.Value, 3);
    }

    [Fact]
    public void LinearProgressIndicator_Indeterminate_AnimationValueAdvancesAcrossFrames()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new SizedBox(
                    width: 200,
                    child: new LinearProgressIndicator())));

        harness.Pump(new Size(240, 80));

        var renderIndicator = FindDescendantByTypeName(harness.RenderView, "RenderLinearProgressIndicator");
        Assert.NotNull(renderIndicator);

        var first = ReadProperty<double>(renderIndicator!, "AnimationValue");

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.30));
        harness.Pump(new Size(240, 80));

        renderIndicator = FindDescendantByTypeName(harness.RenderView, "RenderLinearProgressIndicator");
        Assert.NotNull(renderIndicator);

        var second = ReadProperty<double>(renderIndicator!, "AnimationValue");
        Assert.True(second > first);
    }

    [Fact]
    public void LinearProgressIndicator_ResolvesRtlTextDirection()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new Directionality(
                    textDirection: TextDirection.Rtl,
                    child: new SizedBox(
                        width: 200,
                        child: new LinearProgressIndicator(value: 0.25)))));

        harness.Pump(new Size(240, 80));

        var renderIndicator = FindDescendantByTypeName(harness.RenderView, "RenderLinearProgressIndicator");
        Assert.NotNull(renderIndicator);

        Assert.Equal(TextDirection.Rtl, ReadProperty<TextDirection>(renderIndicator!, "TextDirection"));
    }

    [Fact]
    public void LinearProgressIndicator_SemanticsLabel_IncludesComputedPercentForDeterminateValue()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new SizedBox(
                    width: 200,
                    child: new LinearProgressIndicator(
                        value: 0.37,
                        semanticsLabel: "Loading"))));

        var semanticsRoot = harness.PumpAndGetSemantics(new Size(240, 80));
        Assert.NotNull(semanticsRoot);

        var semanticsNode = FindFirstSemanticsNode(
            semanticsRoot!,
            node => node.Label != null && node.Label.Contains("Loading"));
        Assert.NotNull(semanticsNode);
        Assert.Contains("37%", semanticsNode!.Label);
    }

    private static T ReadProperty<T>(RenderObject target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        var value = property!.GetValue(target);
        Assert.NotNull(value);
        return (T)value!;
    }

    private static RenderObject? FindDescendantByTypeName(RenderObject? root, string typeName)
    {
        if (root is null)
        {
            return null;
        }

        if (root.GetType().Name == typeName)
        {
            return root;
        }

        RenderObject? match = null;
        root.VisitChildren(child =>
        {
            if (match is not null)
            {
                return;
            }

            match = FindDescendantByTypeName(child, typeName);
        });

        return match;
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
                if (ReferenceEquals(_renderView.Child, child))
                {
                    _renderView.Child = null;
                }
            }
        }
    }
}
