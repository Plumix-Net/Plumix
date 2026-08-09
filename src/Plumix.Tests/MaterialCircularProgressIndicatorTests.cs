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
public sealed class MaterialCircularProgressIndicatorTests
{
    [Fact]
    public void CircularProgressIndicator_Constructor_MatchesFlutterAssertions()
    {
        _ = new CircularProgressIndicator(value: double.NaN);
        _ = new CircularProgressIndicator(strokeWidth: -1);
        _ = new CircularProgressIndicator(strokeAlign: 2);
        _ = new CircularProgressIndicator(trackGap: -1);
        using var controller = new AnimationController(TimeSpan.FromSeconds(1));
        Assert.Throws<ArgumentException>(() => new CircularProgressIndicator(value: 0.3, controller: controller));
    }

    [Fact]
    public void CircularProgressIndicator_AdaptiveIOS_Indeterminate_UsesCupertinoActivityIndicator()
    {
        using var valueColor = new ValueNotifier<Color?>(Colors.HotPink);
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with
                {
                    Platform = TargetPlatform.IOS
                },
                child: CircularProgressIndicator.Adaptive(
                    backgroundColor: Colors.OrangeRed,
                    valueColor: valueColor,
                    strokeWidth: 9,
                    strokeAlign: -1,
                    trackGap: 6,
                    year2023: false)));

        harness.Pump(new Size(140, 140));

        var cupertinoRender = FindDescendantByTypeName(harness.RenderView, "RenderCupertinoActivityIndicator");
        var materialRender = FindDescendantByTypeName(harness.RenderView, "RenderCircularProgressIndicator");

        Assert.NotNull(cupertinoRender);
        Assert.Null(materialRender);
        Assert.Equal(Colors.OrangeRed, ReadProperty<Color>(cupertinoRender!, "ActiveColor"));
        Assert.Equal(1.0, ReadProperty<double>(cupertinoRender, "Progress"), 3);
        Assert.Equal(10.0, ReadProperty<double>(cupertinoRender, "Radius"), 3);
    }

    [Fact]
    public void CircularProgressIndicator_AdaptiveIOS_Determinate_UsesPartiallyRevealedProgress()
    {
        using var valueColor = new ValueNotifier<Color?>(Colors.HotPink);
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with
                {
                    Platform = TargetPlatform.IOS
                },
                child: CircularProgressIndicator.Adaptive(
                    value: 0.37,
                    backgroundColor: Colors.SeaGreen,
                    valueColor: valueColor,
                    strokeCap: StrokeCap.Round)));

        harness.Pump(new Size(140, 140));

        var cupertinoRender = FindDescendantByTypeName(harness.RenderView, "RenderCupertinoActivityIndicator");
        var materialRender = FindDescendantByTypeName(harness.RenderView, "RenderCircularProgressIndicator");

        Assert.NotNull(cupertinoRender);
        Assert.Null(materialRender);
        Assert.Equal(Colors.SeaGreen, ReadProperty<Color>(cupertinoRender!, "ActiveColor"));
        Assert.Equal(0.37, ReadProperty<double>(cupertinoRender, "Progress"), 3);
    }

    [Fact]
    public void CircularProgressIndicator_AdaptiveIOS_IgnoresMaterialSemanticsParameters()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with
                {
                    Platform = TargetPlatform.IOS
                },
                child: CircularProgressIndicator.Adaptive(
                    value: 0.37,
                    semanticsLabel: "Loading")));

        var semanticsRoot = harness.PumpAndGetSemantics(new Size(140, 140));
        Assert.Null(FindFirstSemanticsNode(
            semanticsRoot!,
            node => node.Label != null && node.Label.Contains("Loading")));
    }

    [Fact]
    public void CircularProgressIndicator_AdaptiveAndroid_FallsBackToMaterialIndicator()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with
                {
                    Platform = TargetPlatform.Android,
                    ColorScheme = ThemeData.Light.ColorScheme with
                    {
                        Primary = Colors.DarkOrange,
                    },
                },
                child: CircularProgressIndicator.Adaptive(
                    value: 0.5)));

        harness.Pump(new Size(140, 140));

        var materialRender = FindDescendantByTypeName(harness.RenderView, "RenderCircularProgressIndicator");
        var cupertinoRender = FindDescendantByTypeName(harness.RenderView, "RenderCupertinoActivityIndicator");

        Assert.NotNull(materialRender);
        Assert.Null(cupertinoRender);
        Assert.Equal(Colors.DarkOrange, ReadProperty<Color>(materialRender!, "ValueColor"));
    }

    [Fact]
    public void CircularProgressIndicator_DefaultM3_Determinate_Uses2023Defaults()
    {
        var theme = ThemeData.Light with
        {
            UseMaterial3 = true,
            ColorScheme = ThemeData.Light.ColorScheme with
            {
                Primary = Colors.DarkOrange,
                SecondaryContainer = Colors.LightBlue,
            },
        };

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new Center(
                    child: new CircularProgressIndicator(value: 0.5))));

        harness.Pump(new Size(140, 140));

        var renderIndicator = FindDescendantByTypeName(harness.RenderView, "RenderCircularProgressIndicator");
        Assert.NotNull(renderIndicator);

        Assert.Equal(Colors.DarkOrange, ReadProperty<Color>(renderIndicator!, "ValueColor"));
        Assert.Null(ReadProperty<Color?>(renderIndicator, "TrackColor"));
        Assert.Equal(4.0, ReadProperty<double>(renderIndicator, "StrokeWidth"), 3);
        Assert.Equal(0.0, ReadProperty<double>(renderIndicator, "StrokeAlign"), 3);
        Assert.Equal(36.0, ReadProperty<Size>(renderIndicator, "Size").Width, 3);
        Assert.True(ReadProperty<bool>(renderIndicator, "Year2023"));
        Assert.Null(ReadNullableProperty<StrokeCap>(renderIndicator, "StrokeCap"));
        Assert.Null(ReadNullableProperty<double>(renderIndicator, "TrackGap"));

        double? value = ReadProperty<double?>(renderIndicator, "Value");
        Assert.True(value.HasValue);
        Assert.Equal(0.5, value.Value, 3);
    }

    [Fact]
    public void CircularProgressIndicator_Year2023False_Uses2024M3Defaults()
    {
        var theme = ThemeData.Light with
        {
            UseMaterial3 = true,
            ColorScheme = ThemeData.Light.ColorScheme with
            {
                Primary = Colors.DarkOrange,
                SecondaryContainer = Colors.LightBlue,
            },
        };

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new Center(
                    child: new CircularProgressIndicator(value: 0.5, year2023: false))));

        harness.Pump(new Size(140, 140));

        var renderIndicator = FindDescendantByTypeName(harness.RenderView, "RenderCircularProgressIndicator");
        Assert.NotNull(renderIndicator);

        Assert.Equal(Colors.DarkOrange, ReadProperty<Color>(renderIndicator!, "ValueColor"));
        Assert.Equal(Colors.LightBlue, ReadProperty<Color?>(renderIndicator, "TrackColor"));
        Assert.Equal(4.0, ReadProperty<double>(renderIndicator, "StrokeWidth"), 3);
        Assert.Equal(-1.0, ReadProperty<double>(renderIndicator, "StrokeAlign"), 3);
        Assert.Equal(40.0, ReadProperty<Size>(renderIndicator, "Size").Width, 3);
        Assert.False(ReadProperty<bool>(renderIndicator, "Year2023"));
        double? trackGap = ReadNullableProperty<double>(renderIndicator, "TrackGap");
        Assert.NotNull(trackGap);
        Assert.Equal(4.0, trackGap.Value, 3);

        var padding = FindDescendantByTypeName(harness.RenderView, "RenderPadding");
        Assert.NotNull(padding);
        Assert.Equal(48.0, ReadProperty<Size>(padding!, "Size").Width, 3);
    }

    [Fact]
    public void CircularProgressIndicator_PaddingUsesWidgetThemeDefaultPrecedence()
    {
        var theme = ThemeData.Light with
        {
            ProgressIndicatorTheme = new ProgressIndicatorThemeData(
                Constraints: new BoxConstraints(MinWidth: 40, MinHeight: 40),
                CircularTrackPadding: EdgeInsetsGeometry.All(3),
                Year2023: false),
        };

        using var themedHarness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new Center(child: new CircularProgressIndicator(value: 0.5))));
        themedHarness.Pump(new Size(140, 140));
        var themedPadding = FindDescendantByTypeName(themedHarness.RenderView, "RenderPadding");
        Assert.NotNull(themedPadding);
        Assert.Equal(46.0, ReadProperty<Size>(themedPadding!, "Size").Width, 3);

        using var widgetHarness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new Center(
                    child: new CircularProgressIndicator(
                        value: 0.5,
                        padding: EdgeInsetsGeometry.All(5)))));
        widgetHarness.Pump(new Size(140, 140));
        var widgetPadding = FindDescendantByTypeName(widgetHarness.RenderView, "RenderPadding");
        Assert.NotNull(widgetPadding);
        Assert.Equal(50.0, ReadProperty<Size>(widgetPadding!, "Size").Width, 3);
    }

    [Fact]
    public void CircularProgressIndicator_DefaultM2_Determinate_UsesPrimaryWithoutTrack()
    {
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            ColorScheme = ThemeData.Light.ColorScheme with
            {
                Primary = Colors.MediumVioletRed,
            },
        };

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new Center(
                    child: new CircularProgressIndicator(value: 0.5))));

        harness.Pump(new Size(140, 140));

        var renderIndicator = FindDescendantByTypeName(harness.RenderView, "RenderCircularProgressIndicator");
        Assert.NotNull(renderIndicator);

        Assert.Equal(Colors.MediumVioletRed, ReadProperty<Color>(renderIndicator!, "ValueColor"));
        Assert.Null(ReadProperty<Color?>(renderIndicator, "TrackColor"));
        Assert.Equal(4.0, ReadProperty<double>(renderIndicator, "StrokeWidth"), 3);
        Assert.Equal(0.0, ReadProperty<double>(renderIndicator, "StrokeAlign"), 3);
        Assert.Equal(36.0, ReadProperty<Size>(renderIndicator, "Size").Width, 3);
        Assert.Null(ReadNullableProperty<StrokeCap>(renderIndicator, "StrokeCap"));
        Assert.Null(ReadNullableProperty<double>(renderIndicator, "TrackGap"));
    }

    [Fact]
    public void CircularProgressIndicator_ResolvesPrecedence_WidgetOverThemeAndThemeOverDefaults()
    {
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.Orange,
            SecondaryContainerColor = Colors.SkyBlue,
            ProgressIndicatorTheme = new ProgressIndicatorThemeData(
                Color: Colors.Green,
                CircularTrackColor: Colors.MediumPurple,
                StrokeWidth: 6,
                StrokeAlign: -1.0,
                Constraints: new BoxConstraints(MinWidth: 48, MinHeight: 48),
                StrokeCap: StrokeCap.Round,
                TrackGap: 7.0,
                Year2023: false)
        };

        using var themedHarness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new Center(
                    child: new CircularProgressIndicator(value: 0.5))));

        themedHarness.Pump(new Size(140, 140));

        var themedRender = FindDescendantByTypeName(themedHarness.RenderView, "RenderCircularProgressIndicator");
        Assert.NotNull(themedRender);

        Assert.Equal(Colors.Green, ReadProperty<Color>(themedRender!, "ValueColor"));
        Assert.Equal(Colors.MediumPurple, ReadProperty<Color?>(themedRender, "TrackColor"));
        Assert.Equal(6.0, ReadProperty<double>(themedRender, "StrokeWidth"), 3);
        Assert.Equal(-1.0, ReadProperty<double>(themedRender, "StrokeAlign"), 3);
        Assert.Equal(48.0, ReadProperty<Size>(themedRender, "Size").Width, 3);
        Assert.False(ReadProperty<bool>(themedRender, "Year2023"));
        var themedStrokeCap = ReadNullableProperty<StrokeCap>(themedRender, "StrokeCap");
        Assert.NotNull(themedStrokeCap);
        Assert.Equal(StrokeCap.Round, themedStrokeCap.Value);
        double? themedTrackGap = ReadNullableProperty<double>(themedRender, "TrackGap");
        Assert.NotNull(themedTrackGap);
        Assert.Equal(7.0, themedTrackGap.Value, 3);

        using var widgetHarness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new Center(
                    child: new CircularProgressIndicator(
                        value: 0.5,
                        color: Colors.DarkRed,
                        backgroundColor: Colors.LightGoldenrodYellow,
                        strokeWidth: 8,
                        strokeAlign: 1.0,
                        constraints: new BoxConstraints(MinWidth: 52, MinHeight: 52),
                        strokeCap: StrokeCap.Square,
                        trackGap: 2.5,
                        year2023: false))));

        widgetHarness.Pump(new Size(160, 160));

        var widgetRender = FindDescendantByTypeName(widgetHarness.RenderView, "RenderCircularProgressIndicator");
        Assert.NotNull(widgetRender);

        Assert.Equal(Colors.DarkRed, ReadProperty<Color>(widgetRender!, "ValueColor"));
        Assert.Equal(Colors.LightGoldenrodYellow, ReadProperty<Color?>(widgetRender, "TrackColor"));
        Assert.Equal(8.0, ReadProperty<double>(widgetRender, "StrokeWidth"), 3);
        Assert.Equal(1.0, ReadProperty<double>(widgetRender, "StrokeAlign"), 3);
        Assert.Equal(52.0, ReadProperty<Size>(widgetRender, "Size").Width, 3);
        Assert.False(ReadProperty<bool>(widgetRender, "Year2023"));
        var widgetStrokeCap = ReadNullableProperty<StrokeCap>(widgetRender, "StrokeCap");
        Assert.NotNull(widgetStrokeCap);
        Assert.Equal(StrokeCap.Square, widgetStrokeCap.Value);
        double? widgetTrackGap = ReadNullableProperty<double>(widgetRender, "TrackGap");
        Assert.NotNull(widgetTrackGap);
        Assert.Equal(2.5, widgetTrackGap.Value, 3);
    }

    [Fact]
    public void CircularProgressIndicator_UsesExplicitAndThemeControllersForIndeterminateAnimation()
    {
        using var defaultHarness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new CircularProgressIndicator()));
        defaultHarness.Pump(new Size(140, 140));
        var defaultRender = FindDescendantByTypeName(defaultHarness.RenderView, "RenderCircularProgressIndicator");
        Assert.NotNull(defaultRender);
        double defaultArcStart = ReadProperty<double>(defaultRender!, "ArcStart");
        double defaultArcSweep = ReadProperty<double>(defaultRender, "ArcSweep");

        using var explicitController = new AnimationController(TimeSpan.FromSeconds(1))
        {
            Curve = Curves.EaseIn
        };
        explicitController.Forward(from: 0.5);
        explicitController.Stop();

        using var explicitHarness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new CircularProgressIndicator(controller: explicitController)));

        explicitHarness.Pump(new Size(140, 140));

        var explicitRender = FindDescendantByTypeName(explicitHarness.RenderView, "RenderCircularProgressIndicator");
        Assert.NotNull(explicitRender);
        Assert.Null(ReadProperty<double?>(explicitRender!, "Value"));
        double explicitArcStart = ReadProperty<double>(explicitRender, "ArcStart");
        double explicitArcSweep = ReadProperty<double>(explicitRender, "ArcSweep");
        Assert.True(Math.Abs(explicitArcStart - defaultArcStart) > 0.0001 || Math.Abs(explicitArcSweep - defaultArcSweep) > 0.0001);

        using var themedController = new AnimationController(TimeSpan.FromSeconds(1))
        {
            Curve = Curves.EaseIn
        };
        themedController.Forward(from: 0.5);
        themedController.Stop();

        using var themedHarness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with
                {
                    ProgressIndicatorTheme = new ProgressIndicatorThemeData(
                        Controller: themedController)
                },
                child: new CircularProgressIndicator()));

        themedHarness.Pump(new Size(140, 140));

        var themedRender = FindDescendantByTypeName(themedHarness.RenderView, "RenderCircularProgressIndicator");
        Assert.NotNull(themedRender);
        Assert.Null(ReadProperty<double?>(themedRender!, "Value"));
        double themedArcStart = ReadProperty<double>(themedRender, "ArcStart");
        double themedArcSweep = ReadProperty<double>(themedRender, "ArcSweep");
        Assert.Equal(explicitArcStart, themedArcStart, 3);
        Assert.Equal(explicitArcSweep, themedArcSweep, 3);
    }

    [Fact]
    public void CircularProgressIndicator_ValueColor_OverridesColorAndUpdatesFromNotifier()
    {
        var initialValueColor = Color.Parse("#FF00695C");
        var updatedValueColor = Color.Parse("#FF8E24AA");
        using var notifier = new ValueNotifier<Color?>(initialValueColor);

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with
                {
                    PrimaryColor = Colors.DarkOrange,
                    ProgressIndicatorTheme = new ProgressIndicatorThemeData(
                        Color: Colors.Navy)
                },
                child: new CircularProgressIndicator(
                    value: 0.5,
                    color: Colors.DarkRed,
                    valueColor: notifier)));

        harness.Pump(new Size(140, 140));

        var renderIndicator = FindDescendantByTypeName(harness.RenderView, "RenderCircularProgressIndicator");
        Assert.NotNull(renderIndicator);
        Assert.Equal(initialValueColor, ReadProperty<Color>(renderIndicator!, "ValueColor"));

        notifier.Value = null;
        harness.Pump(new Size(140, 140));

        renderIndicator = FindDescendantByTypeName(harness.RenderView, "RenderCircularProgressIndicator");
        Assert.NotNull(renderIndicator);
        Assert.Equal(Colors.DarkRed, ReadProperty<Color>(renderIndicator!, "ValueColor"));

        notifier.Value = updatedValueColor;
        harness.Pump(new Size(140, 140));

        renderIndicator = FindDescendantByTypeName(harness.RenderView, "RenderCircularProgressIndicator");
        Assert.NotNull(renderIndicator);
        Assert.Equal(updatedValueColor, ReadProperty<Color>(renderIndicator!, "ValueColor"));
    }

    [Fact]
    public void CircularProgressIndicator_ValueColor_UsesAlwaysStoppedAnimationValue()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light with
                {
                    PrimaryColor = Colors.DarkOrange
                },
                child: new CircularProgressIndicator(
                    value: 0.5,
                    color: Colors.DarkRed,
                    valueColor: new AlwaysStoppedAnimation<Color?>(Colors.ForestGreen))));

        harness.Pump(new Size(140, 140));

        var renderIndicator = FindDescendantByTypeName(harness.RenderView, "RenderCircularProgressIndicator");
        Assert.NotNull(renderIndicator);
        Assert.Equal(Colors.ForestGreen, ReadProperty<Color>(renderIndicator!, "ValueColor"));
    }

    [Fact]
    public void CircularProgressIndicator_Constraints_ReplaceDefaultSize()
    {
        var theme = ThemeData.Light with
        {
            ProgressIndicatorTheme = new ProgressIndicatorThemeData(
                Constraints: new BoxConstraints(MinWidth: 46, MinHeight: 46))
        };

        using var themedHarness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new Center(
                    child: new CircularProgressIndicator(value: 0.5))));

        themedHarness.Pump(new Size(140, 140));

        var themedRender = FindDescendantByTypeName(themedHarness.RenderView, "RenderCircularProgressIndicator");
        Assert.NotNull(themedRender);
        Assert.Equal(46.0, ReadProperty<Size>(themedRender!, "Size").Width, 3);

        using var widgetHarness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new Center(
                    child: new CircularProgressIndicator(
                        value: 0.5,
                        constraints: new BoxConstraints(MinWidth: 50, MinHeight: 50)))));

        widgetHarness.Pump(new Size(140, 140));

        var widgetRender = FindDescendantByTypeName(widgetHarness.RenderView, "RenderCircularProgressIndicator");
        Assert.NotNull(widgetRender);
        Assert.Equal(50.0, ReadProperty<Size>(widgetRender!, "Size").Width, 3);
    }

    [Fact]
    public void CircularProgressIndicator_DeterminateValue_IsClamped()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new CircularProgressIndicator(value: 1.5)));

        harness.Pump(new Size(140, 140));

        var renderIndicator = FindDescendantByTypeName(harness.RenderView, "RenderCircularProgressIndicator");
        Assert.NotNull(renderIndicator);

        double? clampedValue = ReadProperty<double?>(renderIndicator!, "Value");
        Assert.True(clampedValue.HasValue);
        Assert.Equal(1.0, clampedValue.Value, 3);
    }

    [Fact]
    public void CircularProgressIndicator_Indeterminate_AnimationArcChangesAcrossFrames()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new CircularProgressIndicator()));

        harness.Pump(new Size(140, 140));

        var renderIndicator = FindDescendantByTypeName(harness.RenderView, "RenderCircularProgressIndicator");
        Assert.NotNull(renderIndicator);

        Assert.Null(ReadProperty<double?>(renderIndicator!, "Value"));

        double firstStart = ReadProperty<double>(renderIndicator, "ArcStart");
        double firstSweep = ReadProperty<double>(renderIndicator, "ArcSweep");

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.30));
        harness.Pump(new Size(140, 140));

        renderIndicator = FindDescendantByTypeName(harness.RenderView, "RenderCircularProgressIndicator");
        Assert.NotNull(renderIndicator);

        double secondStart = ReadProperty<double>(renderIndicator!, "ArcStart");
        double secondSweep = ReadProperty<double>(renderIndicator, "ArcSweep");

        Assert.True(Math.Abs(secondStart - firstStart) > 0.0001 || Math.Abs(secondSweep - firstSweep) > 0.0001);
    }

    [Fact]
    public void CircularProgressIndicator_SemanticsExposeProgressRoleAndNumericRange()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new CircularProgressIndicator(
                    value: 0.37,
                    semanticsLabel: "Loading")));

        var semanticsRoot = harness.PumpAndGetSemantics(new Size(140, 140));
        Assert.NotNull(semanticsRoot);

        var semanticsNode = FindFirstSemanticsNode(
            semanticsRoot!,
            node => node.Label != null && node.Label.Contains("Loading"));
        Assert.NotNull(semanticsNode);
        Assert.Equal(SemanticsRole.ProgressBar, semanticsNode!.Role);
        Assert.Equal("37", semanticsNode.Value);
        Assert.Equal("0", semanticsNode.MinValue);
        Assert.Equal("100", semanticsNode.MaxValue);
    }

    private static T ReadProperty<T>(RenderObject target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        object? value = property!.GetValue(target);
        if (value is null)
        {
            return default!;
        }

        return (T)value;
    }

    private static T? ReadNullableProperty<T>(RenderObject target, string propertyName) where T : struct
    {
        var property = target.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        object? value = property!.GetValue(target);
        if (value is null)
        {
            return null;
        }

        return (T)value;
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
