using Avalonia;
using Plumix;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class SafeAreaTests
{
    [Fact]
    public void SafeArea_AppliesPaddingAndRemovesConsumedPaddingFromDescendants()
    {
        MediaQueryProbe.Reset();

        var owner = new BuildOwner();
        var root = new TestRootElement(
            new MediaQuery(
                data: new MediaQueryData(
                    Padding: new Thickness(10, 20, 30, 40),
                    ViewPadding: new Thickness(10, 20, 30, 40)),
                child: new SafeArea(
                    child: new MediaQueryProbeWidget())));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var outerPadding = RequireRenderObject<RenderPadding>(root.ChildElement);
        Assert.Equal(new Thickness(10, 20, 30, 40), outerPadding.Padding);
        Assert.Equal(new Thickness(), MediaQueryProbe.Padding);
        Assert.Equal(new Thickness(), MediaQueryProbe.ViewPadding);
    }

    [Fact]
    public void SafeArea_MaintainBottomViewPadding_UsesViewPaddingBottom()
    {
        MediaQueryProbe.Reset();

        var owner = new BuildOwner();
        var root = new TestRootElement(
            new MediaQuery(
                data: new MediaQueryData(
                    Padding: new Thickness(12, 4, 6, 0),
                    ViewPadding: new Thickness(12, 4, 6, 34)),
                child: new SafeArea(
                    maintainBottomViewPadding: true,
                    child: new MediaQueryProbeWidget())));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var outerPadding = RequireRenderObject<RenderPadding>(root.ChildElement);
        Assert.Equal(new Thickness(12, 4, 6, 34), outerPadding.Padding);
        Assert.Equal(new Thickness(), MediaQueryProbe.Padding);
        Assert.Equal(new Thickness(0, 0, 0, 34), MediaQueryProbe.ViewPadding);
    }

    [Fact]
    public void SafeArea_RespectsMinimumAndSideFlags()
    {
        MediaQueryProbe.Reset();

        var owner = new BuildOwner();
        var root = new TestRootElement(
            new MediaQuery(
                data: new MediaQueryData(
                    Padding: new Thickness(4, 10, 3, 2),
                    ViewPadding: new Thickness(4, 10, 3, 2)),
                child: new SafeArea(
                    left: false,
                    top: true,
                    right: true,
                    bottom: false,
                    minimum: new Thickness(5, 1, 8, 4),
                    child: new MediaQueryProbeWidget())));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var outerPadding = RequireRenderObject<RenderPadding>(root.ChildElement);
        Assert.Equal(new Thickness(5, 10, 8, 4), outerPadding.Padding);
        Assert.Equal(new Thickness(4, 0, 0, 2), MediaQueryProbe.Padding);
    }

    [Fact]
    public void MediaQueryData_RemovePadding_AdjustsViewPaddingLikeFlutter()
    {
        var data = new MediaQueryData(
            Padding: new Thickness(4, 6, 8, 10),
            ViewPadding: new Thickness(9, 11, 13, 15));

        var updated = data.RemovePadding(removeLeft: true, removeBottom: true);

        Assert.Equal(new Thickness(0, 6, 8, 0), updated.Padding);
        Assert.Equal(new Thickness(5, 11, 13, 5), updated.ViewPadding);
    }

    [Fact]
    public void MediaQueryData_RemoveViewInsets_AdjustsViewPaddingAndZeroesSelectedInsets()
    {
        var data = new MediaQueryData(
            Padding: new Thickness(3, 4, 5, 6),
            ViewPadding: new Thickness(9, 10, 11, 12),
            ViewInsets: new Thickness(2, 20, 4, 6));

        var updated = data.RemoveViewInsets(removeLeft: true, removeBottom: true);

        Assert.Equal(new Thickness(3, 4, 5, 6), updated.Padding);
        Assert.Equal(new Thickness(0, 20, 4, 0), updated.ViewInsets);
        Assert.Equal(new Thickness(7, 10, 11, 6), updated.ViewPadding);
    }

    [Fact]
    public void MediaQueryData_RemoveViewInsets_ClampsViewPaddingToZeroWhenInsetsExceedPadding()
    {
        var data = new MediaQueryData(
            ViewPadding: new Thickness(5, 6, 7, 8),
            ViewInsets: new Thickness(9, 10, 3, 2));

        var updated = data.RemoveViewInsets(removeLeft: true, removeTop: true);

        Assert.Equal(new Thickness(0, 0, 3, 2), updated.ViewInsets);
        Assert.Equal(new Thickness(0, 0, 7, 8), updated.ViewPadding);
    }

    [Fact]
    public void MediaQueryData_RemoveViewPadding_ZerosSelectedPaddingSides()
    {
        var data = new MediaQueryData(
            Padding: new Thickness(3, 4, 5, 6),
            ViewPadding: new Thickness(7, 8, 9, 10));

        var updated = data.RemoveViewPadding(removeTop: true, removeRight: true);

        Assert.Equal(new Thickness(3, 0, 0, 6), updated.Padding);
        Assert.Equal(new Thickness(7, 0, 0, 10), updated.ViewPadding);
    }

    [Fact]
    public void WidgetHost_ProvidesAmbientMediaQueryToRootWidget()
    {
        MediaQueryProbe.Reset();

        var host = new WidgetHost
        {
            RootWidget = new MediaQueryProbeWidget()
        };

        Assert.True(MediaQueryProbe.DidBuild);
        Assert.Equal(new Thickness(), MediaQueryProbe.Padding);
        Assert.Equal(new Thickness(), MediaQueryProbe.ViewPadding);
        Assert.Equal(new Size(0, 0), MediaQueryProbe.Size);

        _ = host;
    }

    private static T RequireRenderObject<T>(Element? element) where T : RenderObject
    {
        Assert.NotNull(element);
        Assert.NotNull(element!.RenderObject);
        return Assert.IsType<T>(element.RenderObject);
    }

    private static class MediaQueryProbe
    {
        public static bool DidBuild { get; private set; }

        public static Thickness Padding { get; private set; }

        public static Thickness ViewPadding { get; private set; }

        public static Size Size { get; private set; }

        public static void Record(Thickness padding, Thickness viewPadding, Size size)
        {
            DidBuild = true;
            Padding = padding;
            ViewPadding = viewPadding;
            Size = size;
        }

        public static void Reset()
        {
            DidBuild = false;
            Padding = default;
            ViewPadding = default;
            Size = default;
        }
    }

    private sealed class MediaQueryProbeWidget : StatelessWidget
    {
        public override Widget Build(BuildContext context)
        {
            var mediaQuery = MediaQuery.Of(context);
            MediaQueryProbe.Record(mediaQuery.Padding, mediaQuery.ViewPadding, mediaQuery.Size);
            return new SizedBox(width: 1, height: 1);
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
            if (_child != null)
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
            if (_child != null)
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
