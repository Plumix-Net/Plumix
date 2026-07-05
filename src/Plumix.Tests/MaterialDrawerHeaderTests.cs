using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialDrawerHeaderTests
{
    [Fact]
    public void DrawerHeader_UsesStatusBarHeightPaddingAndBottomDivider()
    {
        var theme = ThemeData.Light with
        {
            TextTheme = new MaterialTextTheme(
                bodyLarge: MaterialTextTheme.DefaultBodyLarge.CopyWith(fontSize: 19)),
            DividerColor = Colors.Crimson,
            UseMaterial3 = false,
        };
        using var harness = new WidgetRenderHarness(Root(
            new DrawerHeader(child: new Text("Header")),
            theme,
            padding: new Thickness(0, 24, 0, 0)));

        harness.Pump(new Size(304, 260));

        Assert.Contains(
            FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints == BoxConstraints.TightFor(height: 185));
        Assert.Contains(
            FindDescendants<RenderPadding>(harness.RenderView),
            padding => padding.Padding == new Thickness(16, 40, 16, 8));
        Assert.Contains(
            FindDescendants<RenderDividerLine>(harness.RenderView),
            divider => divider.Axis == Axis.Horizontal && divider.Color == Colors.Crimson);
        Assert.Equal(19, FindParagraph(harness.RenderView, "Header")!.FontSize);
    }

    [Fact]
    public void DrawerHeader_CustomDecorationMarginAndAnimationContractAreWired()
    {
        var decoration = new BoxDecoration(Color: Colors.DarkSlateBlue);
        var header = new DrawerHeader(
            child: new Text("Custom"),
            decoration: decoration,
            margin: new Thickness(3, 4, 5, 6),
            padding: new Thickness(7, 8, 9, 10),
            duration: TimeSpan.FromMilliseconds(400),
            curve: Curves.EaseOut);
        Assert.Equal(TimeSpan.FromMilliseconds(400), header.Duration);
        Assert.Equal(Curves.EaseOut(0.3), header.Curve(0.3));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DrawerHeader(
            child: null,
            duration: TimeSpan.FromMilliseconds(-1)));

        using var harness = new WidgetRenderHarness(Root(header, ThemeData.Light));
        harness.Pump(new Size(304, 260));

        Assert.Contains(
            FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration == decoration);
        Assert.Contains(
            FindDescendants<RenderPadding>(harness.RenderView),
            padding => padding.Padding == new Thickness(3, 4, 5, 6));
    }

    [Fact]
    public void DrawerHeader_DefaultCurveUsesFlutterFastOutSlowInCubic()
    {
        var header = new DrawerHeader(child: null);

        Assert.Equal(0, header.Curve(0));
        Assert.InRange(header.Curve(0.5), 0.77, 0.78);
        Assert.Equal(1, header.Curve(1));
    }

    [Fact]
    public void UserAccountsDrawerHeader_DefaultsPicturesTextAndSemanticsMatchFlutter()
    {
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.DarkGreen,
            PrimaryTextTheme = new MaterialTextTheme(
                bodyLarge: MaterialTextTheme.DefaultBodyLarge.CopyWith(fontSize: 17, color: Colors.White),
                bodyMedium: MaterialTextTheme.DefaultBodyMedium.CopyWith(fontSize: 13, color: Colors.White)),
        };
        using var harness = new WidgetRenderHarness(Root(
            new UserAccountsDrawerHeader(
                accountName: new Text("Ada"),
                accountEmail: new Text("ada@example.test"),
                currentAccountPicture: new Text("current"),
                otherAccountsPictures:
                [
                    new Text("other-1"),
                    new Text("other-2"),
                    new Text("other-3"),
                    new Text("other-4"),
                ]),
            theme));

        var semantics = harness.PumpAndGetSemantics(new Size(304, 260));

        Assert.Contains(
            FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Color == Colors.DarkGreen);
        Assert.Contains(
            FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints == BoxConstraints.TightFor(width: 72, height: 72));
        Assert.Equal(
            3,
            FindDescendants<RenderConstrainedBox>(harness.RenderView)
                .Count(box => box.AdditionalConstraints == BoxConstraints.TightFor(width: 40, height: 40)));
        Assert.Null(FindParagraph(harness.RenderView, "other-4"));
        Assert.Equal(17, FindParagraph(harness.RenderView, "Ada")!.FontSize);
        Assert.Equal(13, FindParagraph(harness.RenderView, "ada@example.test")!.FontSize);
        Assert.NotNull(FindSemantics(semantics, node => node.Label?.Contains("Signed in", StringComparison.Ordinal) == true));
    }

    [Fact]
    public void UserAccountsDrawerHeader_DetailsToggleInvokesCallbackAndUpdatesArrowSemantics()
    {
        var detailsPressed = 0;
        using var harness = new WidgetRenderHarness(Root(
            new UserAccountsDrawerHeader(
                accountName: new Text("Ada"),
                accountEmail: new Text("ada@example.test"),
                onDetailsPressed: () => detailsPressed++),
            ThemeData.Light));

        var semantics = harness.PumpAndGetSemantics(new Size(304, 260));
        Assert.NotNull(FindSemantics(semantics, node => node.Label?.Contains("Show accounts", StringComparison.Ordinal) == true));
        var button = FindSemantics(
            semantics,
            node => node.Flags.HasFlag(SemanticsFlags.IsButton)
                    && node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(button);
        Assert.True(button!.PerformAction(SemanticsActions.Tap));

        semantics = harness.PumpAndGetSemantics(new Size(304, 260));
        Assert.Equal(1, detailsPressed);
        Assert.NotNull(FindSemantics(semantics, node => node.Label?.Contains("Hide accounts", StringComparison.Ordinal) == true));
    }

    [Fact]
    public void UserAccountsDrawerHeader_RtlPlacesCurrentAccountAtVisualStartAndOthersAtEnd()
    {
        using var harness = new WidgetRenderHarness(Root(
            new UserAccountsDrawerHeader(
                accountName: new Text("Name"),
                accountEmail: new Text("Mail"),
                currentAccountPicture: new Text("current"),
                otherAccountsPictures: [new Text("other")]),
            ThemeData.Light,
            direction: TextDirection.Rtl));

        harness.Pump(new Size(304, 260));

        var pictures = FindDescendants<RenderStack>(harness.RenderView)
            .Single(stack => stack.ChildCount == 2);
        var current = Assert.IsAssignableFrom<RenderBox>(pictures.FirstChild);
        var others = Assert.IsAssignableFrom<RenderBox>(pictures.ChildAfter(current));
        Assert.True(
            ((StackParentData)current.parentData!).offset.X > ((StackParentData)others.parentData!).offset.X);
    }

    [Fact]
    public void UserAccountsDrawerHeader_ValidatesPictureSizes()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new UserAccountsDrawerHeader(
            accountName: null,
            accountEmail: null,
            currentAccountPictureSize: new Size(-1, 72)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UserAccountsDrawerHeader(
            accountName: null,
            accountEmail: null,
            otherAccountsPicturesSize: new Size(double.PositiveInfinity, 40)));
    }

    private static Widget Root(
        Widget child,
        ThemeData theme,
        Thickness padding = default,
        TextDirection direction = TextDirection.Ltr)
    {
        return new MediaQuery(
            data: new MediaQueryData(Size: new Size(304, 260), Padding: padding, ViewPadding: padding),
            child: new Directionality(direction, new Theme(theme, child)));
    }

    private static RenderParagraph? FindParagraph(RenderObject? root, string text)
    {
        return FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.Text == text);
    }

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null) return result;
        if (root is T target) result.Add(target);
        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private static SemanticsNode? FindSemantics(SemanticsNode? node, Func<SemanticsNode, bool> predicate)
    {
        if (node is null) return null;
        if (predicate(node)) return node;
        foreach (var child in node.Children)
        {
            var result = FindSemantics(child, predicate);
            if (result is not null) return result;
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
