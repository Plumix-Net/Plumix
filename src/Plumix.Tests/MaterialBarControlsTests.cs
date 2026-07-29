using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialBarControlsTests
{
    [Fact]
    public void BottomAppBar_ValidatesNumericContracts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BottomAppBar(elevation: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BottomAppBar(elevation: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BottomAppBar(notchMargin: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BottomAppBar(height: -1));
    }

    [Fact]
    public void BottomAppBar_ResolvesM3AndM2Defaults()
    {
        var m3Theme = ThemeData.Light with
        {
            UseMaterial3 = true,
            SurfaceContainerColor = Colors.MistyRose,
        };
        using var m3 = new WidgetRenderHarness(Wrap(
            new BottomAppBar(child: new SizedBox(height: 20)),
            m3Theme));
        m3.Pump(new Size(320, 160));
        var m3Surface = FindDescendant<RenderBottomAppBarSurface>(m3.RenderView);
        Assert.NotNull(m3Surface);
        Assert.Equal(Colors.MistyRose, m3Surface!.Color);
        Assert.Equal(3, m3Surface.Elevation);
        Assert.Equal(Colors.Transparent, m3Surface.ShadowColor);
        Assert.IsType<AutomaticNotchedShape>(m3Surface.Shape);
        Assert.Equal(80, m3Surface.Size.Height, 3);
        Assert.Contains(
            FindDescendants<RenderPadding>(m3Surface),
            padding => padding.Padding == new Thickness(16, 12));

        using var m2 = new WidgetRenderHarness(Wrap(
            new BottomAppBar(child: new SizedBox(height: 20)),
            ThemeData.Light with { UseMaterial3 = false }));
        m2.Pump(new Size(320, 160));
        var m2Surface = FindDescendant<RenderBottomAppBarSurface>(m2.RenderView);
        Assert.NotNull(m2Surface);
        Assert.Equal(Colors.White, m2Surface!.Color);
        Assert.Equal(8, m2Surface.Elevation);
        Assert.Equal(Colors.Black, m2Surface.ShadowColor);
        Assert.Null(m2Surface.Shape);
        Assert.Equal(20, m2Surface.Size.Height, 3);
    }

    [Fact]
    public void BottomAppBar_ThemeAndWidgetValuesFollowPrecedence()
    {
        var themedShape = new CircularNotchedRectangle();
        var theme = ThemeData.Light with
        {
            BottomAppBarTheme = new BottomAppBarThemeData(
                Color: Colors.SeaGreen,
                Elevation: 5,
                Shape: themedShape,
                Height: 72,
                SurfaceTintColor: Colors.Transparent,
                ShadowColor: Colors.DarkSlateGray,
                Padding: new Thickness(9)),
        };

        using var themed = new WidgetRenderHarness(Wrap(new BottomAppBar(), theme));
        themed.Pump(new Size(320, 160));
        var themedSurface = FindDescendant<RenderBottomAppBarSurface>(themed.RenderView);
        Assert.NotNull(themedSurface);
        Assert.Equal(Colors.SeaGreen, themedSurface!.Color);
        Assert.Equal(5, themedSurface.Elevation);
        Assert.Same(themedShape, themedSurface.Shape);
        Assert.Equal(72, themedSurface.Size.Height, 3);

        var widgetShape = new CircularNotchedRectangle(inverted: true);
        using var explicitHarness = new WidgetRenderHarness(Wrap(
            new BottomAppBar(
                color: Colors.Gold,
                elevation: 7,
                shape: widgetShape,
                height: 64,
                shadowColor: Colors.Crimson,
                padding: new Thickness(3)),
            theme));
        explicitHarness.Pump(new Size(320, 160));
        var explicitSurface = FindDescendant<RenderBottomAppBarSurface>(explicitHarness.RenderView);
        Assert.NotNull(explicitSurface);
        Assert.Equal(Colors.Gold, explicitSurface!.Color);
        Assert.Equal(7, explicitSurface.Elevation);
        Assert.Same(widgetShape, explicitSurface.Shape);
        Assert.Equal(Colors.Crimson, explicitSurface.ShadowColor);
        Assert.Equal(64, explicitSurface.Size.Height, 3);
    }

    [Fact]
    public void BottomAppBar_M2DarkSurfaceUsesElevationOverlayPolicy()
    {
        Color surface = Color.Parse("#FF121212");
        Color onSurface = Color.Parse("#FF69F0AE");
        var theme = new ThemeData(
            brightness: Brightness.Dark,
            useMaterial3: false,
            applyElevationOverlayColor: true,
            surfaceColor: surface,
            onSurfaceColor: onSurface);
        using var harness = new WidgetRenderHarness(Wrap(
            new BottomAppBar(
                color: surface,
                elevation: 8.0,
                child: new SizedBox(height: 20)),
            theme));

        harness.Pump(new Size(320, 160));

        var rendered = FindDescendant<RenderBottomAppBarSurface>(harness.RenderView);
        Assert.NotNull(rendered);
        Assert.Equal(ElevationOverlay.ColorWithOverlay(surface, onSurface, 8.0), rendered!.Color);
    }

    [Fact]
    public void BottomAppBar_SafeAreaAddsBottomInsetOutsideConfiguredHeight()
    {
        using var harness = new WidgetRenderHarness(Wrap(
            new BottomAppBar(height: 80),
            mediaPadding: new Thickness(0, 0, 0, 24)));
        harness.Pump(new Size(320, 180));
        var surface = FindDescendant<RenderBottomAppBarSurface>(harness.RenderView);
        Assert.NotNull(surface);
        Assert.Equal(104, surface!.Size.Height, 3);
    }

    [Fact]
    public void BottomAppBar_WithScaffoldFab_ProducesDirectionalInflatedGuestRect()
    {
        Widget Build(TextDirection direction) => new Directionality(
            direction,
            new MediaQuery(
                new MediaQueryData(Size: new Size(400, 600)),
                new Theme(
                    ThemeData.Light,
                    new Scaffold(
                        body: new SizedBox(),
                        floatingActionButton: new FloatingActionButton(new Icon(Icons.Add), () => { }),
                        bottomNavigationBar: new BottomAppBar(
                            shape: new CircularNotchedRectangle(),
                            notchMargin: 4,
                            height: 80)))));

        using var ltr = new WidgetRenderHarness(Build(TextDirection.Ltr));
        ltr.Pump(new Size(400, 600));
        var ltrSurface = FindDescendant<RenderBottomAppBarSurface>(ltr.RenderView);
        Assert.NotNull(ltrSurface);
        Assert.True(ltrSurface!.HasFloatingActionButton);
        var ltrGuest = Assert.IsType<Rect>(ltrSurface.GuestRect);
        Assert.Equal(64, ltrGuest.Width, 3);
        Assert.Equal(356, ltrGuest.Center.X, 3);
        Assert.Equal(0, ltrGuest.Center.Y, 3);

        using var rtl = new WidgetRenderHarness(Build(TextDirection.Rtl));
        rtl.Pump(new Size(400, 600));
        var rtlSurface = FindDescendant<RenderBottomAppBarSurface>(rtl.RenderView);
        Assert.NotNull(rtlSurface);
        var rtlGuest = Assert.IsType<Rect>(rtlSurface!.GuestRect);
        Assert.Equal(44, rtlGuest.Center.X, 3);
    }

    [Fact]
    public void BottomAppBar_WithoutFab_DoesNotCutNotchAndPropagatesClipBehavior()
    {
        using var harness = new WidgetRenderHarness(Wrap(
            new Scaffold(
                body: new SizedBox(),
                bottomNavigationBar: new BottomAppBar(
                    shape: new CircularNotchedRectangle(),
                    clipBehavior: Clip.HardEdge,
                    height: 80))));
        harness.Pump(new Size(320, 400));
        var surface = FindDescendant<RenderBottomAppBarSurface>(harness.RenderView);
        Assert.NotNull(surface);
        Assert.False(surface!.HasFloatingActionButton);
        Assert.Null(surface.GuestRect);
        Assert.Equal(Clip.HardEdge, surface.ClipBehavior);
    }

    [Fact]
    public void ButtonBar_ValidatesNumericContracts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ButtonBar(buttonMinWidth: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ButtonBar(buttonHeight: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ButtonBar(overflowButtonSpacing: -1));
    }

    [Fact]
    public void ButtonBar_DefaultsOverrideParentButtonTheme()
    {
        ButtonThemeData? captured = null;
        using var harness = new WidgetRenderHarness(Wrap(
            new ButtonTheme(
                new ButtonThemeData(
                    AlignedDropdown: true,
                    TextTheme: ButtonTextTheme.Normal,
                    MinWidth: 12,
                    Height: 13,
                    Padding: new Thickness(2),
                    LayoutBehavior: ButtonBarLayoutBehavior.Constrained),
                new ButtonBar(children: [new ButtonThemeProbe(value => captured = value)]))));
        harness.Pump(new Size(320, 100));

        Assert.NotNull(captured);
        Assert.Equal(ButtonTextTheme.Primary, captured!.TextTheme);
        Assert.Equal(64, captured.MinWidth);
        Assert.Equal(36, captured.Height);
        Assert.Equal(new Thickness(8, 0), captured.Padding);
        Assert.False(captured.AlignedDropdown);
        Assert.Equal(ButtonBarLayoutBehavior.Padded, captured.LayoutBehavior);
    }

    [Fact]
    public void ButtonBar_LocalThemeAndWidgetValuesFollowPrecedence()
    {
        ButtonThemeData? themeCaptured = null;
        var data = new ButtonBarThemeData(
            Alignment: MainAxisAlignment.Center,
            MainAxisSize: MainAxisSize.Min,
            ButtonTextTheme: ButtonTextTheme.Accent,
            ButtonMinWidth: 70,
            ButtonHeight: 42,
            ButtonPadding: new Thickness(10, 2),
            ButtonAlignedDropdown: true,
            LayoutBehavior: ButtonBarLayoutBehavior.Constrained,
            OverflowDirection: VerticalDirection.Up);
        using var themed = new WidgetRenderHarness(Wrap(
            new ButtonBarTheme(
                data,
                new ButtonBar(children: [new ButtonThemeProbe(value => themeCaptured = value)]))));
        themed.Pump(new Size(320, 100));
        Assert.NotNull(themeCaptured);
        Assert.Equal(ButtonTextTheme.Accent, themeCaptured!.TextTheme);
        Assert.Equal(70, themeCaptured.MinWidth);
        Assert.Equal(42, themeCaptured.Height);
        Assert.True(themeCaptured.AlignedDropdown);
        Assert.Equal(ButtonBarLayoutBehavior.Constrained, themeCaptured.LayoutBehavior);

        ButtonThemeData? widgetCaptured = null;
        using var explicitHarness = new WidgetRenderHarness(Wrap(
            new ButtonBarTheme(
                data,
                new ButtonBar(
                    buttonTextTheme: ButtonTextTheme.Primary,
                    buttonMinWidth: 90,
                    buttonHeight: 48,
                    buttonPadding: new Thickness(12, 4),
                    buttonAlignedDropdown: false,
                    layoutBehavior: ButtonBarLayoutBehavior.Padded,
                    children: [new ButtonThemeProbe(value => widgetCaptured = value)]))));
        explicitHarness.Pump(new Size(320, 100));
        Assert.NotNull(widgetCaptured);
        Assert.Equal(ButtonTextTheme.Primary, widgetCaptured!.TextTheme);
        Assert.Equal(90, widgetCaptured.MinWidth);
        Assert.Equal(48, widgetCaptured.Height);
        Assert.False(widgetCaptured.AlignedDropdown);
        Assert.Equal(ButtonBarLayoutBehavior.Padded, widgetCaptured.LayoutBehavior);
    }

    [Fact]
    public void ButtonBar_WideLayoutUsesRowAlignmentAndMainAxisSize()
    {
        using var maxHarness = new WidgetRenderHarness(Wrap(new ButtonBar(
            alignment: MainAxisAlignment.End,
            children:
            [
                new SizedBox(width: 40, height: 20),
                new SizedBox(width: 50, height: 20),
            ])));
        maxHarness.Pump(new Size(240, 100));
        var maxRow = FindDescendant<RenderButtonBarRow>(maxHarness.RenderView);
        Assert.NotNull(maxRow);
        Assert.True(maxRow!.Size.Width > 200);
        var first = maxRow.FirstChild!;
        var second = maxRow.ChildAfter(first)!;
        Assert.True(((ButtonBarParentData)first.parentData!).offset.X > 100);
        Assert.True(((ButtonBarParentData)second.parentData!).offset.X > ((ButtonBarParentData)first.parentData!).offset.X);

        using var minHarness = new WidgetRenderHarness(Wrap(new ButtonBar(
            mainAxisSize: MainAxisSize.Min,
            children:
            [
                new SizedBox(width: 40, height: 20),
                new SizedBox(width: 50, height: 20),
            ])));
        minHarness.Pump(new Size(240, 100));
        var minRow = FindDescendant<RenderButtonBarRow>(minHarness.RenderView);
        Assert.NotNull(minRow);
        Assert.True(minRow!.Size.Width < 120);
    }

    [Fact]
    public void ButtonBar_NarrowLayoutStacksWithSpacingAndVerticalDirection()
    {
        using var downHarness = new WidgetRenderHarness(Wrap(new ButtonBar(
            alignment: MainAxisAlignment.End,
            overflowButtonSpacing: 10,
            children:
            [
                new SizedBox(width: 100, height: 20),
                new SizedBox(width: 100, height: 20),
            ])));
        downHarness.Pump(new Size(150, 120));
        var down = FindDescendant<RenderButtonBarRow>(downHarness.RenderView);
        Assert.NotNull(down);
        Assert.Equal(50, down!.Size.Height, 3);
        var downFirst = down.FirstChild!;
        var downSecond = down.ChildAfter(downFirst)!;
        Assert.Equal(0, ((ButtonBarParentData)downFirst.parentData!).offset.Y, 3);
        Assert.Equal(30, ((ButtonBarParentData)downSecond.parentData!).offset.Y, 3);

        using var upHarness = new WidgetRenderHarness(Wrap(new ButtonBar(
            overflowDirection: VerticalDirection.Up,
            overflowButtonSpacing: 10,
            children:
            [
                new SizedBox(width: 100, height: 20),
                new SizedBox(width: 100, height: 20),
            ])));
        upHarness.Pump(new Size(150, 120));
        var up = FindDescendant<RenderButtonBarRow>(upHarness.RenderView);
        Assert.NotNull(up);
        var upFirst = up!.FirstChild!;
        var upSecond = up.ChildAfter(upFirst)!;
        Assert.Equal(30, ((ButtonBarParentData)upFirst.parentData!).offset.Y, 3);
        Assert.Equal(0, ((ButtonBarParentData)upSecond.parentData!).offset.Y, 3);
    }

    [Fact]
    public void ButtonBar_ConstrainedLayoutHasMinimumHeight52()
    {
        using var harness = new WidgetRenderHarness(Wrap(new ButtonBar(
            layoutBehavior: ButtonBarLayoutBehavior.Constrained,
            children: [new SizedBox(width: 40, height: 10)])));
        harness.Pump(new Size(200, 100));
        Assert.True(harness.RenderView.Child!.Size.Height >= 52);
    }

    private static Widget Wrap(Widget child, ThemeData? theme = null, Thickness? mediaPadding = null) =>
        new Directionality(
            TextDirection.Ltr,
            new MediaQuery(
                new MediaQueryData(Size: new Size(400, 600), Padding: mediaPadding ?? default),
                new Theme(theme ?? ThemeData.Light, child)));

    private static T? FindDescendant<T>(RenderObject? root) where T : RenderObject
    {
        if (root is null) return null;
        if (root is T match) return match;
        T? result = null;
        root.VisitChildren(child => result ??= FindDescendant<T>(child));
        return result;
    }

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null) return result;
        if (root is T match) result.Add(match);
        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private sealed class ButtonThemeProbe(Action<ButtonThemeData> capture) : StatelessWidget
    {
        public override Widget Build(BuildContext context)
        {
            capture(ButtonTheme.Of(context));
            return new SizedBox(width: 20, height: 20);
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

        public void Dispose() => _rootElement.Unmount();

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;
            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget) => _renderView = renderView;
            public override RenderObject? RenderObject => _child?.RenderObject;
            internal override Element? RenderObjectAttachingChild => _child;
            protected override void OnMount() { base.OnMount(); Rebuild(); }
            internal override void Rebuild() { Dirty = false; _child = UpdateChild(_child, Widget, Slot); }
            internal override void Update(Widget newWidget) { base.Update(newWidget); Rebuild(); }
            internal override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
            internal override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
            public void InsertRenderObjectChild(RenderObject child, object? slot) => _renderView.Child = (RenderBox)child;
            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
            public void RemoveRenderObjectChild(RenderObject child, object? slot) { if (ReferenceEquals(_renderView.Child, child)) _renderView.Child = null; }
            internal override void Unmount() { if (_child is not null) { UnmountChild(_child); _child = null; } base.Unmount(); }
        }
    }
}
