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
    public void Badge_DefaultLabeledStyle_UsesColorSchemeErrorTokensAndLabelSmall()
    {
        var theme = ThemeData.Light with
        {
            ErrorColor = Colors.OrangeRed,
            OnErrorColor = Colors.MidnightBlue,
            ColorScheme = ThemeData.Light.ColorScheme with
            {
                Error = Colors.Crimson,
                OnError = Colors.LightBlue,
            },
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
        Assert.Equal(Colors.LightBlue, Assert.IsType<SolidColorBrush>(paragraph.Foreground).Color);
        Assert.Contains(decorations, box => box.Decoration.Color == Colors.Crimson);
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
    public void Badge_DirectionalAlignment_ResolvesTopEndForLtrAndRtl()
    {
        using var ltrHarness = BuildBadgeHarness(
            TextDirection.Ltr,
            new Badge(
                alignment: AlignmentDirectional.TopEnd,
                label: new Text("L"),
                child: new SizedBox(width: 24, height: 24)));
        using var rtlHarness = BuildBadgeHarness(
            TextDirection.Rtl,
            new Badge(
                alignment: AlignmentDirectional.TopEnd,
                label: new Text("R"),
                child: new SizedBox(width: 24, height: 24)));

        ltrHarness.Pump(new Size(100, 100));
        rtlHarness.Pump(new Size(100, 100));

        RenderBadgePositioner ltr = FindDescendant<RenderBadgePositioner>(ltrHarness.RenderView)!;
        RenderBadgePositioner rtl = FindDescendant<RenderBadgePositioner>(rtlHarness.RenderView)!;
        Point ltrOffset = ((BoxParentData)ltr.Child!.parentData!).offset;
        Point rtlOffset = ((BoxParentData)rtl.Child!.parentData!).offset;

        Assert.Equal((AlignmentGeometry)AlignmentDirectional.TopEnd, ltr.Alignment);
        Assert.Equal(TextDirection.Ltr, ltr.TextDirection);
        Assert.Equal(TextDirection.Rtl, rtl.TextDirection);
        Assert.True(ltrOffset.X > rtlOffset.X);
        Assert.Equal(ltrOffset.Y, rtlOffset.Y, precision: 6);
        Assert.Equal(Clip.None, Assert.IsType<RenderStack>(ltr.Parent).ClipBehavior);
    }

    [Fact]
    public void Badge_PhysicalAlignment_DoesNotMirrorInRtl()
    {
        using var physicalHarness = BuildBadgeHarness(
            TextDirection.Rtl,
            new Badge(
                alignment: Alignment.TopRight,
                label: new Text("P"),
                child: new SizedBox(width: 24, height: 24)));
        using var directionalHarness = BuildBadgeHarness(
            TextDirection.Rtl,
            new Badge(
                alignment: AlignmentDirectional.TopEnd,
                label: new Text("D"),
                child: new SizedBox(width: 24, height: 24)));

        physicalHarness.Pump(new Size(100, 100));
        directionalHarness.Pump(new Size(100, 100));

        RenderBadgePositioner physical = FindDescendant<RenderBadgePositioner>(physicalHarness.RenderView)!;
        RenderBadgePositioner directional = FindDescendant<RenderBadgePositioner>(directionalHarness.RenderView)!;
        Point physicalOffset = ((BoxParentData)physical.Child!.parentData!).offset;
        Point directionalOffset = ((BoxParentData)directional.Child!.parentData!).offset;

        Assert.True(physicalOffset.X > directionalOffset.X);
    }

    [Fact]
    public void Badge_DefaultAlignment_IsDirectionalTopEnd()
    {
        using var harness = BuildBadgeHarness(
            TextDirection.Rtl,
            new Badge(label: new Text("1"), child: new SizedBox(width: 24, height: 24)));

        harness.Pump(new Size(100, 100));

        RenderBadgePositioner positioner = FindDescendant<RenderBadgePositioner>(harness.RenderView)!;
        Assert.Equal((AlignmentGeometry)AlignmentDirectional.TopEnd, positioner.Alignment);
        Assert.NotEmpty(FindDescendants<RenderClipRRect>(harness.RenderView));
    }

    [Fact]
    public void BadgeThemeData_CopyWithAndLerp_FollowSourceValueSemantics()
    {
        var begin = new BadgeThemeData(
            BackgroundColor: Colors.Black,
            TextColor: Colors.White,
            SmallSize: 4,
            LargeSize: 12,
            TextStyle: new TextStyle(FontSize: 10),
            Padding: new Thickness(2, 0),
            Alignment: Alignment.TopLeft,
            Offset: new Vector(2, -2));
        var end = new BadgeThemeData(
            BackgroundColor: Colors.White,
            TextColor: Colors.Black,
            SmallSize: 8,
            LargeSize: 20,
            TextStyle: new TextStyle(FontSize: 14),
            Padding: new Thickness(6, 2),
            Alignment: AlignmentDirectional.TopEnd,
            Offset: new Vector(6, 2));

        BadgeThemeData copy = begin.CopyWith(largeSize: 18, alignment: AlignmentDirectional.BottomEnd);
        BadgeThemeData halfway = BadgeThemeData.Lerp(begin, end, 0.5);
        Alignment mixedLtr = halfway.Alignment!.Value.Resolve(TextDirection.Ltr);
        Alignment mixedRtl = halfway.Alignment.Value.Resolve(TextDirection.Rtl);

        Assert.Equal(18, copy.LargeSize);
        Assert.Equal((AlignmentGeometry)AlignmentDirectional.BottomEnd, copy.Alignment);
        Assert.Equal(6, halfway.SmallSize);
        Assert.Equal(16, halfway.LargeSize);
        Assert.Equal(12, halfway.TextStyle!.FontSize);
        Assert.Equal(new Thickness(4, 1), halfway.Padding);
        Assert.Equal(new Vector(4, 0), halfway.Offset);
        Assert.Equal(0, mixedLtr.X, precision: 6);
        Assert.Equal(-1, mixedRtl.X, precision: 6);
        Assert.Equal(-1, mixedLtr.Y, precision: 6);
        Assert.Equal(new BadgeThemeData(), BadgeThemeData.Lerp(null, null, 0.0));
        Assert.Same(begin, BadgeThemeData.Lerp(begin, begin, 0.5));
    }

    [Fact]
    public void Badge_LabeledAlignmentPreservesNegativeWidthOffsetForNarrowChild()
    {
        using var harness = BuildBadgeHarness(
            TextDirection.Ltr,
            new Badge(
                alignment: Alignment.TopRight,
                offset: new Vector(0, -8),
                label: new SizedBox(width: 1, height: 1),
                child: new SizedBox(width: 4, height: 4)));

        harness.Pump(new Size(100, 100));

        RenderBadgePositioner positioner = FindDescendant<RenderBadgePositioner>(harness.RenderView)!;
        Point badgeOffset = ((BoxParentData)positioner.Child!.parentData!).offset;

        Assert.Equal(-12, badgeOffset.X, precision: 6);
    }

    [Fact]
    public void RenderBadgeHorizontalStadium_ExpandsBeyondLargeSizeForLargeContent()
    {
        var content = new RenderConstrainedBox(
            BoxConstraints.TightFor(width: 38, height: 30));
        var stadium = new RenderBadgeHorizontalStadium(16)
        {
            Child = content,
        };

        stadium.Layout(new BoxConstraints(MaxWidth: 100, MaxHeight: 100));

        Assert.Equal(new Size(38, 30), stadium.Size);
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

    private static WidgetRenderHarness BuildBadgeHarness(TextDirection direction, Badge badge)
    {
        return new WidgetRenderHarness(
            new Directionality(
                direction,
                new Theme(ThemeData.Light, badge)));
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
