using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialGridTileTests
{
    [Fact]
    public void GridTile_Constructor_PreservesSourceDefaultsAndSlots()
    {
        var child = new SizedBox(width: 40, height: 30);
        var header = new SizedBox(height: 12);
        var footer = new SizedBox(height: 16);

        var defaultTile = new GridTile(child: child);
        var slottedTile = new GridTile(child: child, header: header, footer: footer);

        Assert.Same(child, defaultTile.Child);
        Assert.Null(defaultTile.Header);
        Assert.Null(defaultTile.Footer);
        Assert.Same(child, slottedTile.Child);
        Assert.Same(header, slottedTile.Header);
        Assert.Same(footer, slottedTile.Footer);
    }

    [Fact]
    public void GridTile_RequiresChild()
    {
        Assert.Throws<ArgumentNullException>(() => new GridTile(child: null!));
    }

    [Fact]
    public void GridTile_WithoutHeaderOrFooter_ReturnsChildDirectly()
    {
        using var harness = new WidgetRenderHarness(
            new GridTile(child: new SizedBox(width: 40, height: 30)));

        harness.Pump(new Size(100, 100));

        Assert.Null(FindDescendant<RenderStack>(harness.RenderView));
        Assert.Contains(
            FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints == BoxConstraints.TightFor(width: 40, height: 30));
    }

    [Fact]
    public void GridTile_HeaderAndFooter_UseFlutterPositionedGeometry()
    {
        using var harness = new WidgetRenderHarness(
            new SizedBox(
                width: 200,
                height: 120,
                child: new GridTile(
                    child: new ColoredBox(Colors.Blue),
                    header: new SizedBox(height: 24),
                    footer: new SizedBox(height: 32))));

        harness.Pump(new Size(300, 200));

        var stack = Assert.IsType<RenderStack>(FindDescendant<RenderStack>(harness.RenderView));
        Assert.Equal(new Size(200, 120), stack.Size);
        Assert.Equal(3, stack.ChildCount);

        var content = Assert.IsAssignableFrom<RenderBox>(stack.FirstChild);
        var header = Assert.IsAssignableFrom<RenderBox>(stack.ChildAfter(content));
        var footer = Assert.IsAssignableFrom<RenderBox>(stack.ChildAfter(header));
        var contentData = Assert.IsType<StackParentData>(content.parentData);
        var headerData = Assert.IsType<StackParentData>(header.parentData);
        var footerData = Assert.IsType<StackParentData>(footer.parentData);

        Assert.Equal((0d, 0d, 0d, 0d), (contentData.Left, contentData.Top, contentData.Right, contentData.Bottom));
        Assert.Equal((0d, 0d, 0d), (headerData.Left, headerData.Top, headerData.Right));
        Assert.Null(headerData.Bottom);
        Assert.Equal((0d, 0d, 0d), (footerData.Left, footerData.Right, footerData.Bottom));
        Assert.Null(footerData.Top);
        Assert.Equal(new Size(200, 24), header.Size);
        Assert.Equal(new Point(0, 0), headerData.offset);
        Assert.Equal(new Size(200, 32), footer.Size);
        Assert.Equal(new Point(0, 88), footerData.offset);
    }

    [Fact]
    public void GridTileBar_OneLine_UsesFortyEightHeightDarkTitleStyleAndTransparentBackground()
    {
        using var harness = new WidgetRenderHarness(
            new Directionality(
                TextDirection.Ltr,
                new SizedBox(
                    width: 240,
                    child: new GridTileBar(title: new Text("Title")))));

        harness.Pump(new Size(300, 100));

        Assert.Contains(
            FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints == BoxConstraints.TightFor(height: 48));
        Assert.Contains(
            FindDescendants<RenderPadding>(harness.RenderView),
            padding => padding.Padding == new Thickness(16, 0, 16, 0));
        Assert.Empty(FindDescendants<RenderDecoratedBox>(harness.RenderView));

        var title = FindParagraph(harness.RenderView, "Title");
        Assert.NotNull(title);
        Assert.Equal(14, title!.FontSize);
        Assert.Equal(
            ThemeData.Dark.TextTheme.TitleMedium.Color,
            Assert.IsType<SolidColorBrush>(title.Foreground).Color);
        Assert.False(title.SoftWrap);
        Assert.Equal(TextOverflow.Ellipsis, title.Overflow);
    }

    [Fact]
    public void GridTileBar_TwoLinesAndSlots_UseSixtyEightHeightStylesIconsAndBackground()
    {
        var background = Color.Parse("#CC102030");
        using var harness = new WidgetRenderHarness(
            new Directionality(
                TextDirection.Ltr,
                new SizedBox(
                    width: 240,
                    child: new GridTileBar(
                        backgroundColor: background,
                        leading: new Icon(Icons.Menu),
                        title: new Text("Title"),
                        subtitle: new Text("Subtitle"),
                        trailing: new Icon(Icons.Close)))));

        harness.Pump(new Size(300, 100));

        Assert.Contains(
            FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints == BoxConstraints.TightFor(height: 68));
        Assert.Contains(
            FindDescendants<RenderPadding>(harness.RenderView),
            padding => padding.Padding == new Thickness(8, 0, 8, 0));
        Assert.Contains(
            FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Color == background);

        var title = FindParagraph(harness.RenderView, "Title");
        var subtitle = FindParagraph(harness.RenderView, "Subtitle");
        Assert.NotNull(title);
        Assert.NotNull(subtitle);
        Assert.Equal(14, title!.FontSize);
        Assert.Equal(14, subtitle!.FontSize);
        Assert.Equal(
            ThemeData.Dark.TextTheme.TitleMedium.Color,
            Assert.IsType<SolidColorBrush>(title.Foreground).Color);
        Assert.Equal(
            ThemeData.Dark.TextTheme.BodySmall.Color,
            Assert.IsType<SolidColorBrush>(subtitle.Foreground).Color);
        Assert.False(title.SoftWrap);
        Assert.False(subtitle.SoftWrap);
        Assert.Equal(TextOverflow.Ellipsis, title.Overflow);
        Assert.Equal(TextOverflow.Ellipsis, subtitle.Overflow);

        var iconParagraphs = FindDescendants<RenderParagraph>(harness.RenderView)
            .Where(paragraph => paragraph.PlainText is "\ue3dc" or "\ue16a")
            .ToList();
        Assert.Equal(2, iconParagraphs.Count);
        Assert.All(iconParagraphs, paragraph =>
            Assert.Equal(Colors.White, Assert.IsType<SolidColorBrush>(paragraph.Foreground).Color));
    }

    [Fact]
    public void GridTileBar_Rtl_ResolvesDirectionalPaddingAndFlexOrder()
    {
        using var harness = new WidgetRenderHarness(
            new Directionality(
                TextDirection.Rtl,
                new SizedBox(
                    width: 240,
                    child: new GridTileBar(
                        leading: new SizedBox(width: 12, height: 12),
                        title: new Text("عنوان")))));

        harness.Pump(new Size(300, 100));

        Assert.Contains(
            FindDescendants<RenderPadding>(harness.RenderView),
            padding => padding.Padding == new Thickness(16, 0, 8, 0));
        var row = FindDescendants<RenderFlex>(harness.RenderView)
            .Single(flex => flex.Direction == Axis.Horizontal);
        var leading = Assert.IsAssignableFrom<RenderBox>(row.FirstChild);
        var title = Assert.IsAssignableFrom<RenderBox>(row.ChildAfter(leading));
        Assert.True(
            ((FlexParentData)leading.parentData!).offset.X > ((FlexParentData)title.parentData!).offset.X,
            "RTL leading slot should be laid out on the visual right edge.");
        Assert.Equal(TextDirection.Rtl, FindParagraph(harness.RenderView, "عنوان")!.TextDirection);
    }

    [Fact]
    public void GridTileBar_ZeroArea_DoesNotCrashAndRemainsZeroSized()
    {
        using var harness = new WidgetRenderHarness(
            new Directionality(
                TextDirection.Ltr,
                new SizedBox(
                    width: 0,
                    height: 0,
                    child: new GridTileBar(title: new Text("X")))));

        harness.Pump(new Size(300, 100));

        Assert.Equal(new Size(0, 0), Assert.IsAssignableFrom<RenderBox>(harness.RenderView.Child).Size);
    }

    private static RenderParagraph? FindParagraph(RenderObject? root, string text)
    {
        return FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.PlainText == text);
    }

    private static T? FindDescendant<T>(RenderObject? root) where T : RenderObject
    {
        return FindDescendants<T>(root).FirstOrDefault();
    }

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null)
        {
            return result;
        }

        if (root is T target)
        {
            result.Add(target);
        }

        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
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

            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget)
            {
                _renderView = renderView;
            }

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
