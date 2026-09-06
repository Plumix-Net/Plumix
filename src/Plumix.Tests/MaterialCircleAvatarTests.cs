using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialCircleAvatarTests : IDisposable
{
    public MaterialCircleAvatarTests()
    {
        Scheduler.ResetForTests();
        ImageCache.Shared.Clear();
        ImageCache.Shared.ClearLiveImages();
    }

    public void Dispose()
    {
        ImageCache.Shared.Clear();
        ImageCache.Shared.ClearLiveImages();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void CircleAvatar_ValidatesRadiusAndImageErrorContracts()
    {
        Assert.Throws<ArgumentException>(() => new CircleAvatar(radius: 20, minRadius: 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CircleAvatar(radius: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CircleAvatar(minRadius: double.PositiveInfinity));
        Assert.Throws<ArgumentException>(() => new CircleAvatar(minRadius: 30, maxRadius: 20));
        Assert.Throws<ArgumentException>(() => new CircleAvatar(onBackgroundImageError: (_, _) => { }));
        Assert.Throws<ArgumentException>(() => new CircleAvatar(onForegroundImageError: (_, _) => { }));
    }

    [Fact]
    public void CircleAvatar_DefaultM3CompositionUsesFortyPixelCircleAndDirectColorSchemeTokens()
    {
        var titleMedium = MaterialTextTheme.DefaultTitleMedium.CopyWith(fontSize: 18);
        var colorScheme = ThemeData.Light.ColorScheme.CopyWith(
            primaryContainer: Colors.CornflowerBlue,
            onPrimaryContainer: Colors.MidnightBlue);
        var theme = ThemeData.Light with
        {
            ColorScheme = colorScheme,
            TextTheme = new MaterialTextTheme(titleMedium: titleMedium),
        };
        using var harness = new WidgetRenderHarness(BuildRoot(
            theme,
            new CircleAvatar(child: new Text("AB"))));

        harness.Pump(new Size(100, 100));

        var avatarBox = Assert.Single(FindDescendants<RenderDecoratedBox>(harness.RenderView));
        Assert.Equal(BoxShape.Circle, avatarBox.Decoration.Shape);
        Assert.Equal(Colors.CornflowerBlue, avatarBox.Decoration.Color);
        Assert.Equal(DecorationPosition.Background, avatarBox.Position);
        Assert.Contains(
            FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints == BoxConstraints.Tight(new Size(40, 40)));
        var paragraph = Assert.Single(FindDescendants<RenderParagraph>(harness.RenderView));
        Assert.Equal("AB", paragraph.PlainText);
        Assert.Equal(18, paragraph.FontSize);
        Assert.Equal(Colors.MidnightBlue, Assert.IsType<SolidColorBrush>(paragraph.Foreground).Color);
    }

    [Fact]
    public void CircleAvatar_RadiusAndMinMaxMapToFlutterDiameterConstraints()
    {
        using var exact = new WidgetRenderHarness(BuildRoot(
            ThemeData.Light,
            new CircleAvatar(radius: 24)));
        exact.Pump(new Size(100, 100));
        Assert.Contains(
            FindDescendants<RenderConstrainedBox>(exact.RenderView),
            box => box.AdditionalConstraints == BoxConstraints.Tight(new Size(48, 48)));

        using var ranged = new WidgetRenderHarness(BuildRoot(
            ThemeData.Light,
            new CircleAvatar(minRadius: 12, maxRadius: 28)));
        ranged.Pump(new Size(100, 100));
        Assert.Contains(
            FindDescendants<RenderConstrainedBox>(ranged.RenderView),
            box => box.AdditionalConstraints == new BoxConstraints(24, 56, 24, 56));
    }

    [Fact]
    public void CircleAvatar_BackgroundAndForegroundImagesUseCoverAndCorrectPaintOrder()
    {
        var background = new TestImageProvider("background");
        var foreground = new TestImageProvider("foreground");
        ImageErrorListener backgroundError = (_, _) => { };
        ImageErrorListener foregroundError = (_, _) => { };
        using var harness = new WidgetRenderHarness(BuildRoot(
            ThemeData.Light,
            new CircleAvatar(
                backgroundImage: background,
                foregroundImage: foreground,
                onBackgroundImageError: backgroundError,
                onForegroundImageError: foregroundError,
                child: new Text("fallback"))));

        harness.Pump(new Size(100, 100));

        var decorations = FindDescendants<RenderDecoratedBox>(harness.RenderView);
        Assert.Equal(2, decorations.Count);
        var foregroundBox = Assert.Single(decorations, box => box.Position == DecorationPosition.Foreground);
        var backgroundBox = Assert.Single(decorations, box => box.Position == DecorationPosition.Background);
        Assert.Equal(BoxShape.Circle, foregroundBox.Decoration.Shape);
        Assert.Equal(BoxShape.Circle, backgroundBox.Decoration.Shape);
        Assert.Equal(BoxFit.Cover, foregroundBox.Decoration.Image!.Fit);
        Assert.Equal(BoxFit.Cover, backgroundBox.Decoration.Image!.Fit);
        Assert.Same(foreground, foregroundBox.Decoration.Image.Image);
        Assert.Same(background, backgroundBox.Decoration.Image.Image);
        Assert.Same(foregroundError, foregroundBox.Decoration.Image.OnError);
        Assert.Same(backgroundError, backgroundBox.Decoration.Image.OnError);
        Assert.NotNull(FindDescendants<RenderParagraph>(harness.RenderView)
            .SingleOrDefault(paragraph => paragraph.PlainText == "fallback"));
    }

    [Fact]
    public async Task CircleAvatar_ForegroundImageErrorKeepsBackgroundFallbackMounted()
    {
        var error = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var background = new TestImageProvider("fallback-background");
        using var harness = new WidgetRenderHarness(BuildRoot(
            ThemeData.Light,
            new CircleAvatar(
                backgroundImage: background,
                foregroundImage: new FailingImageProvider(),
                onForegroundImageError: (exception, _) => error.TrySetResult(exception),
                child: new Text("initials"))));

        harness.Pump(new Size(100, 100));
        Assert.IsType<InvalidOperationException>(await error.Task.WaitAsync(TimeSpan.FromSeconds(2)));
        harness.Pump(new Size(100, 100));

        var decorations = FindDescendants<RenderDecoratedBox>(harness.RenderView);
        Assert.Contains(decorations, box => ReferenceEquals(box.Decoration.Image?.Image, background));
        Assert.Contains(
            FindDescendants<RenderParagraph>(harness.RenderView),
            paragraph => paragraph.PlainText == "initials");
    }

    [Fact]
    public void CircleAvatar_M2BrightnessFallbackMatchesFlutterColorSelection()
    {
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            PrimaryColorLight = Colors.Gold,
            PrimaryColorDark = Colors.Navy,
            PrimaryTextTheme = new MaterialTextTheme(
                titleMedium: MaterialTextTheme.DefaultTitleMedium.CopyWith(color: Colors.White)),
        };
        using var defaultHarness = new WidgetRenderHarness(BuildRoot(
            theme,
            new CircleAvatar(child: new Text("default"))));
        defaultHarness.Pump(new Size(100, 100));
        Assert.Contains(
            FindDescendants<RenderDecoratedBox>(defaultHarness.RenderView),
            box => box.Decoration.Color == Colors.Navy);
        var defaultParagraph = FindDescendants<RenderParagraph>(defaultHarness.RenderView)
            .Single(value => value.PlainText == "default");
        Assert.Equal(Colors.White, Assert.IsType<SolidColorBrush>(defaultParagraph.Foreground).Color);

        using var explicitHarness = new WidgetRenderHarness(BuildRoot(
            theme,
            new CircleAvatar(backgroundColor: Colors.Black, child: new Text("explicit"))));
        explicitHarness.Pump(new Size(100, 100));
        var paragraph = FindDescendants<RenderParagraph>(explicitHarness.RenderView)
            .Single(value => value.PlainText == "explicit");
        Assert.Equal(Colors.Gold, Assert.IsType<SolidColorBrush>(paragraph.Foreground).Color);

        using var lightBackgroundHarness = new WidgetRenderHarness(BuildRoot(
            theme,
            new CircleAvatar(backgroundColor: Colors.White, child: new Text("light"))));
        lightBackgroundHarness.Pump(new Size(100, 100));
        var lightParagraph = FindDescendants<RenderParagraph>(lightBackgroundHarness.RenderView)
            .Single(value => value.PlainText == "light");
        Assert.Equal(Colors.Navy, Assert.IsType<SolidColorBrush>(lightParagraph.Foreground).Color);

        using var foregroundHarness = new WidgetRenderHarness(BuildRoot(
            theme,
            new CircleAvatar(foregroundColor: Colors.White, child: new Text("foreground"))));
        foregroundHarness.Pump(new Size(100, 100));
        Assert.Contains(
            FindDescendants<RenderDecoratedBox>(foregroundHarness.RenderView),
            box => box.Decoration.Color == Colors.Navy);
    }

    [Fact]
    public void CircleAvatar_UpdatesAnimateColorAndDiameterOverThemeDuration()
    {
        using var harness = new WidgetRenderHarness(BuildRoot(
            ThemeData.Light,
            new CircleAvatar(radius: 20, backgroundColor: Colors.Red)));
        harness.Pump(new Size(120, 120));

        harness.Update(BuildRoot(
            ThemeData.Light,
            new CircleAvatar(radius: 40, backgroundColor: Colors.Blue)));
        harness.Pump(new Size(120, 120));
        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.10));
        harness.Pump(new Size(120, 120));

        var midDecoration = FindDescendants<RenderDecoratedBox>(harness.RenderView)
            .Single(box => box.Decoration.Shape == BoxShape.Circle)
            .Decoration;
        var midConstraints = FindDescendants<RenderConstrainedBox>(harness.RenderView)
            .Single(box => box.AdditionalConstraints.MinWidth is > 40 and < 80)
            .AdditionalConstraints;
        Assert.NotEqual(Colors.Red, midDecoration.Color);
        Assert.NotEqual(Colors.Blue, midDecoration.Color);
        Assert.InRange(midConstraints.MinWidth, 40.1, 79.9);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.25));
        harness.Pump(new Size(120, 120));
        Assert.Contains(
            FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints == BoxConstraints.Tight(new Size(80, 80)));
        Assert.Contains(
            FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Color == Colors.Blue);
    }

    private static Widget BuildRoot(ThemeData theme, Widget child)
    {
        return new MediaQuery(
            data: new MediaQueryData(Size: new Size(120, 120), TextScaleFactor: 3),
            child: new Directionality(
                Plumix.UI.TextDirection.Ltr,
                new Theme(theme, child)));
    }

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null) return result;
        if (root is T target) result.Add(target);
        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private sealed class TestImageProvider : ImageProvider<string>
    {
        private readonly string _key;
        public TestImageProvider(string key) => _key = key;
        public override ValueTask<string> ObtainKey(ImageConfiguration configuration) => ValueTask.FromResult(_key);
        protected override ImageStreamCompleter LoadImage(string key)
        {
            return new OneFrameImageStreamCompleter(
                Task.FromResult(new ImageInfo(new FakeImage(new Size(10, 10)), debugLabel: key)));
        }
    }

    private sealed class FailingImageProvider : ImageProvider<string>
    {
        public override ValueTask<string> ObtainKey(ImageConfiguration configuration) => ValueTask.FromResult("failed-foreground");
        protected override ImageStreamCompleter LoadImage(string key)
        {
            return new OneFrameImageStreamCompleter(
                Task.FromException<ImageInfo>(new InvalidOperationException("foreground failed")));
        }
    }

    private sealed class FakeImage : IImage, IDisposable
    {
        public FakeImage(Size size) => Size = size;
        public Size Size { get; }
        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect) { }
        public void Dispose() { }
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

        public void Update(Widget widget)
        {
            _rootElement.UpdateRoot(widget);
            _owner.FlushBuild();
        }

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
            public override Element? RenderObjectAttachingChild => _child;
            public void UpdateRoot(Widget widget) => Update(widget);
            protected override void OnMount() { base.OnMount(); Rebuild(); }
            protected override void PerformRebuild()
            {
                base.PerformRebuild();
                _child = UpdateChild(_child, Widget, Slot);
            }
            public override void Update(Widget newWidget) { base.Update(newWidget); Rebuild(force: true); }
            public override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
            public override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
            public void InsertRenderObjectChild(RenderObject child, object? slot) => _renderView.Child = (RenderBox)child;
            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
            public void RemoveRenderObjectChild(RenderObject child, object? slot) { if (ReferenceEquals(_renderView.Child, child)) _renderView.Child = null; }
            public override void Unmount()
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
}
