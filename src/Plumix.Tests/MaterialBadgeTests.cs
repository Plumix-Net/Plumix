using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialBadgeTests
{
    [Fact]
    public void BadgeCount_FormatsCountAndValidatesArguments()
    {
        var badge = Badge.Count(1000, maxCount: 99);

        Assert.Equal("99+", Assert.IsType<Text>(badge.Label).Data);
        Assert.Throws<ArgumentOutOfRangeException>(() => Badge.Count(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Badge.Count(1, maxCount: 0));
    }

    [Fact]
    public void Badge_DefaultLabeledStyle_UsesErrorTokensAndLabelSmall()
    {
        var theme = ThemeData.Light with
        {
            ErrorColor = Colors.OrangeRed,
            OnErrorColor = Colors.MidnightBlue,
            TextTheme = new MaterialTextTheme(
                labelSmall: new TextStyle(FontSize: 9, FontWeight: FontWeight.Bold)),
        };
        using var harness = new WidgetRenderHarness(
            new Directionality(
                TextDirection.Ltr,
                new Theme(
                    theme,
                    new Badge(label: new Text("7"), child: new SizedBox(width: 24, height: 24)))));

        harness.Pump(new Size(100, 100));

        var positioner = FindDescendant<RenderBadgePositioner>(harness.RenderView);
        var paragraph = FindParagraph(harness.RenderView, "7");
        var decorations = FindDescendants<RenderDecoratedBox>(harness.RenderView);
        Assert.NotNull(positioner);
        Assert.True(positioner!.HasLabel);
        Assert.Equal(16, positioner.WidthOffset);
        Assert.Equal(new Vector(4, 4), positioner.Offset);
        Assert.NotNull(paragraph);
        Assert.Equal(9, paragraph!.FontSize);
        Assert.Equal(FontWeight.Bold, paragraph.FontWeight);
        Assert.Equal(Colors.MidnightBlue, Assert.IsType<SolidColorBrush>(paragraph.Foreground).Color);
        Assert.Contains(decorations, box => box.Decoration.Color == Colors.OrangeRed);
    }

    [Fact]
    public void Badge_ThemeAndWidgetOverrides_FollowWidgetThemeDefaultPrecedence()
    {
        var theme = ThemeData.Light with
        {
            BadgeTheme = new BadgeThemeData(
                BackgroundColor: Colors.DarkGreen,
                TextColor: Colors.Gold,
                LargeSize: 20,
                Padding: new Thickness(6, 0),
                Alignment: Alignment.BottomLeft,
                Offset: new Vector(2, 3)),
        };
        using var harness = new WidgetRenderHarness(
            new Directionality(
                TextDirection.Ltr,
                new Theme(
                    theme,
                    new Badge(
                        backgroundColor: Colors.Purple,
                        largeSize: 22,
                        alignment: Alignment.BottomRight,
                        label: new Text("A"),
                        child: new SizedBox(width: 30, height: 30)))));

        harness.Pump(new Size(100, 100));

        var positioner = FindDescendant<RenderBadgePositioner>(harness.RenderView);
        var paragraph = FindParagraph(harness.RenderView, "A");
        Assert.NotNull(positioner);
        Assert.Equal(22, positioner!.WidthOffset);
        Assert.Equal(Alignment.BottomRight, positioner.Alignment);
        Assert.Equal(new Vector(2, 11), positioner.Offset);
        Assert.Equal(Colors.Gold, Assert.IsType<SolidColorBrush>(paragraph!.Foreground).Color);
        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView), box => box.Decoration.Color == Colors.Purple);
    }

    [Fact]
    public void Badge_SmallBadge_UsesSixPixelCircleAndZeroOffset()
    {
        using var harness = new WidgetRenderHarness(
            new Directionality(
                TextDirection.Rtl,
                new Theme(
                    ThemeData.Light,
                    new Badge(child: new SizedBox(width: 24, height: 24)))));

        harness.Pump(new Size(100, 100));

        var positioner = FindDescendant<RenderBadgePositioner>(harness.RenderView);
        Assert.NotNull(positioner);
        Assert.False(positioner!.HasLabel);
        Assert.Equal(6, positioner.WidthOffset);
        Assert.Equal(default, positioner.Offset);
        var sixPixelBox = FindDescendants<RenderConstrainedBox>(harness.RenderView)
            .Single(box => box.AdditionalConstraints == BoxConstraints.TightFor(width: 6, height: 6));
        Assert.NotNull(sixPixelBox);
    }

    [Fact]
    public void Badge_HiddenLabel_ReturnsChildWithoutBadgeOverlay()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                ThemeData.Light,
                new Badge(
                    isLabelVisible: false,
                    label: new Text("hidden"),
                    child: new SizedBox(width: 24, height: 24))));

        harness.Pump(new Size(100, 100));

        Assert.Null(FindDescendant<RenderBadgePositioner>(harness.RenderView));
        Assert.Null(FindParagraph(harness.RenderView, "hidden"));
    }

    private static RenderParagraph? FindParagraph(RenderObject? root, string text)
    {
        return FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.Text == text);
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
