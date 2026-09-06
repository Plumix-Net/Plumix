using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/basic.dart
// flutter/packages/flutter/lib/src/widgets/container.dart
// flutter/packages/flutter/lib/src/widgets/indexed_stack.dart
// flutter/packages/flutter/lib/src/widgets/spacer.dart

/// <summary>
/// The members `widgets/basic.dart` and `widgets/container.dart` assert on that the earlier
/// approximate port did not carry: the named `SizedBox` constructors, `Container`'s decoration
/// padding / transform alignment / anti-aliasing, the directional `Stack` default, and the
/// `Positioned` factories.
/// </summary>
public sealed class BasicWidgetsParityTests
{
    [Fact]
    public void SizedBox_NamedConstructorsMatchDart()
    {
        Assert.Equal(double.PositiveInfinity, SizedBox.Expand().Width);
        Assert.Equal(double.PositiveInfinity, SizedBox.Expand().Height);
        Assert.Equal(0.0, SizedBox.Shrink().Width);
        Assert.Equal(0.0, SizedBox.Shrink().Height);
        Assert.Equal(12.0, SizedBox.Square(12).Width);
        Assert.Equal(12.0, SizedBox.Square(12).Height);
        Assert.Equal(7.0, SizedBox.FromSize(new Size(7, 9)).Width);
        Assert.Equal(9.0, SizedBox.FromSize(new Size(7, 9)).Height);
        Assert.Null(SizedBox.FromSize().Width);
    }

    [Fact]
    public void SizedBox_ToStringShort_NamesTheExpandAndShrinkForms()
    {
        Assert.Equal("SizedBox.Expand", SizedBox.Expand().ToStringShort());
        Assert.Equal("SizedBox.Shrink", SizedBox.Shrink().ToStringShort());
        Assert.Equal("SizedBox", new SizedBox(width: 4).ToStringShort());
        Assert.StartsWith("SizedBox.Shrink-", SizedBox.Shrink(key: new ValueKey<int>(3)).ToStringShort());
    }

    [Fact]
    public void Spacer_UsesAShrunkSizedBox()
    {
        IReadOnlyList<Widget> widgets = Mount(new Row(children: [new Spacer(flex: 3)]));

        Expanded expanded = Assert.Single(Of<Expanded>(widgets));
        Assert.Equal(3, expanded.Flex);
        SizedBox box = Assert.Single(Of<SizedBox>(widgets));
        Assert.Equal(0.0, box.Width);
        Assert.Equal(0.0, box.Height);
    }

    [Fact]
    public void Container_FoldsWidthAndHeightIntoItsConstraints()
    {
        var container = new Container(width: 20, height: 30);
        Assert.Equal(BoxConstraints.TightFor(width: 20, height: 30), container.Constraints);

        var tightened = new Container(
            width: 20,
            constraints: new BoxConstraints(MinWidth: 0, MaxWidth: 100, MinHeight: 5, MaxHeight: 50));
        Assert.Equal(20.0, tightened.Constraints!.Value.MinWidth);
        Assert.Equal(20.0, tightened.Constraints.Value.MaxWidth);
        Assert.Equal(5.0, tightened.Constraints.Value.MinHeight);
        Assert.Equal(50.0, tightened.Constraints.Value.MaxHeight);

        Assert.Null(new Container().Constraints);
    }

    [DebugOnlyFact]
    public void Container_RejectsColorWithDecorationAndClipWithoutDecoration()
    {
        Assert.Throws<AssertionError>(() => new Container(
            color: Colors.Green,
            decoration: new BoxDecoration(Color: Colors.Red)));
        Assert.Throws<AssertionError>(() => new Container(clipBehavior: Clip.AntiAlias));
        Assert.Throws<AssertionError>(() => new Container(padding: EdgeInsets.Only(left: -1)));
        Assert.Throws<AssertionError>(() => new Container(margin: EdgeInsets.Only(top: -1)));
    }

    [Fact]
    public void Container_AddsTheDecorationsOwnPaddingToItsPadding()
    {
        IReadOnlyList<Widget> widgets = Mount(new Container(
            padding: EdgeInsets.All(4),
            decoration: new BoxDecoration(
                Border: new Border(top: new BorderSide(Colors.Black, 2))),
            child: new SizedBox()));

        Padding padding = Assert.Single(Of<Padding>(widgets));
        Assert.Equal(new Thickness(4, 6, 4, 4), padding.Insets.Resolve(TextDirection.Ltr));
    }

    [Fact]
    public void Container_PassesTransformAlignmentAndAntiAliasThrough()
    {
        IReadOnlyList<Widget> widgets = Mount(new Container(
            color: Colors.Red,
            isAntiAlias: false,
            transform: Matrix4.Identity(),
            transformAlignment: Alignment.TopLeft,
            child: new SizedBox()));

        Widgets.Transform transform = Assert.Single(Of<Widgets.Transform>(widgets));
        Assert.Equal((AlignmentGeometry)Alignment.TopLeft, transform.Alignment);
        Assert.False(Assert.Single(Of<ColoredBox>(widgets)).IsAntiAlias);
    }

    [Fact]
    public void Container_KeepsANullChildInsteadOfSubstitutingASizedBox()
    {
        // Dart keeps `current` null when the container only tightens constraints, so the wrappers
        // take a null child instead of an inserted SizedBox.
        IReadOnlyList<Widget> widgets = Mount(new Container(width: 10, height: 10));

        Assert.Null(Assert.Single(Of<ConstrainedBox>(widgets)).Child);
        Assert.Empty(Of<SizedBox>(widgets));
    }

    [Fact]
    public void Stack_And_IndexedStack_DefaultToDirectionalTopStart()
    {
        Assert.Equal((AlignmentGeometry)AlignmentDirectional.TopStart, new Stack().Alignment);
        Assert.Equal((AlignmentGeometry)AlignmentDirectional.TopStart, new IndexedStack().Alignment);
        Assert.Equal(Clip.HardEdge, new Stack().ClipBehavior);
        Assert.Equal(Clip.HardEdge, new IndexedStack().ClipBehavior);
        Assert.Equal(StackFit.Loose, new Stack().Fit);
        Assert.Equal(StackFit.Loose, new IndexedStack().Sizing);
    }

    [Fact]
    public void IndexedStack_ForwardsSizingClipAndDirectionToItsRenderObject()
    {
        IReadOnlyList<Widget> widgets = Mount(new IndexedStack(
            children: [new SizedBox(width: 10, height: 10)],
            textDirection: TextDirection.Rtl,
            clipBehavior: Clip.AntiAlias,
            sizing: StackFit.Expand));

        var raw = Assert.Single(Of<RawIndexedStack>(widgets));
        Assert.Equal(TextDirection.Rtl, raw.TextDirection);
        Assert.Equal(Clip.AntiAlias, raw.ClipBehavior);
        Assert.Equal(StackFit.Expand, raw.Fit);
    }

    [Fact]
    public void RenderIndexedStack_ExpandFitGivesChildrenTightConstraints()
    {
        var loose = new RenderIndexedStack(index: 0, textDirection: TextDirection.Ltr);
        var looseChild = new StubRenderBox(new Size(10, 10));
        loose.Insert(looseChild);
        loose.Layout(BoxConstraints.Tight(new Size(100, 50)));
        Assert.Equal(new Size(10, 10), looseChild.Size);

        var expanded = new RenderIndexedStack(
            index: 0,
            textDirection: TextDirection.Ltr,
            fit: StackFit.Expand);
        var expandedChild = new StubRenderBox(new Size(10, 10));
        expanded.Insert(expandedChild);
        expanded.Layout(BoxConstraints.Tight(new Size(100, 50)));
        Assert.Equal(new Size(100, 50), expandedChild.Size);
    }

    [Fact]
    public void FittedBox_DefaultsAndClipBehaviorReachTheRenderObject()
    {
        var widget = new FittedBox(clipBehavior: Clip.AntiAlias, child: new SizedBox());
        Assert.Equal(BoxFit.Contain, widget.Fit);
        Assert.Equal(default, widget.Alignment);
        Assert.Equal(Clip.AntiAlias, widget.ClipBehavior);

        var box = new RenderFittedBox(
            fit: BoxFit.None,
            alignment: AlignmentDirectional.CenterEnd,
            textDirection: TextDirection.Rtl,
            clipBehavior: Clip.HardEdge);
        Assert.Equal(Clip.HardEdge, box.ClipBehavior);
        Assert.Equal((AlignmentGeometry)AlignmentDirectional.CenterEnd, box.Alignment);
        Assert.Equal(TextDirection.Rtl, box.TextDirection);
    }

    [DebugOnlyFact]
    public void RenderFittedBox_ChangingANonScaleDownFitDoesNotRelayout()
    {
        var child = new StubRenderBox(new Size(40, 20));
        var box = new RenderFittedBox(fit: BoxFit.Contain, child: child);
        box.Layout(BoxConstraints.Tight(new Size(80, 40)));
        Assert.False(box.DebugNeedsLayout);

        // Dart only marks a layout when scaleDown is involved on either side of the change.
        box.Fit = BoxFit.Cover;
        Assert.False(box.DebugNeedsLayout);

        box.Fit = BoxFit.ScaleDown;
        Assert.True(box.DebugNeedsLayout);
    }

    [Fact]
    public void Positioned_FactoriesMatchDart()
    {
        var child = new SizedBox();

        Positioned fromRect = Positioned.FromRect(new Rect(3, 4, 10, 20), child);
        Assert.Equal(3.0, fromRect.Left);
        Assert.Equal(4.0, fromRect.Top);
        Assert.Equal(10.0, fromRect.Width);
        Assert.Equal(20.0, fromRect.Height);
        Assert.Null(fromRect.Right);
        Assert.Null(fromRect.Bottom);

        Positioned fill = Positioned.Fill(child, left: 2);
        Assert.Equal(2.0, fill.Left);
        Assert.Equal(0.0, fill.Top);
        Assert.Equal(0.0, fill.Right);
        Assert.Equal(0.0, fill.Bottom);
        Assert.Null(fill.Width);
        Assert.Null(fill.Height);

        Positioned rtl = Positioned.Directional(TextDirection.Rtl, child, start: 5, end: 9);
        Assert.Equal(9.0, rtl.Left);
        Assert.Equal(5.0, rtl.Right);
    }

    private static IReadOnlyList<T> Of<T>(IReadOnlyList<Widget> widgets) where T : Widget =>
        widgets.OfType<T>().ToList();

    /// <summary>Mounts the widget under a Directionality and returns every widget in the tree.</summary>
    private static IReadOnlyList<Widget> Mount(Widget widget)
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new Directionality(TextDirection.Ltr, widget));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        List<Widget> widgets = [];
        Visit(root);
        return widgets;

        void Visit(Element element)
        {
            widgets.Add(element.Widget);
            element.VisitChildren(Visit);
        }
    }

    private sealed class StubRenderBox : RenderBox
    {
        private readonly Size _size;

        public StubRenderBox(Size size)
        {
            _size = size;
        }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(_size);
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
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

        protected override void PerformRebuild()
        {
            base.PerformRebuild();
            _child = UpdateChild(_child, Widget, Slot);
        }

        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild(force: true);
        }

        public override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
            }
        }

        public override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        public override void Unmount()
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
