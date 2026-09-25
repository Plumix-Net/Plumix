using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart
// Mirrors flutter/packages/flutter/test/widgets/render_object_element_test.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class RenderObjectElementDartParityTests
{
    // Flutter: render_object_element_test.dart:
    // "RenderObjectElement *RenderObjectChild methods get called with correct arguments"
    [Fact]
    public void RenderObjectElementRenderObjectChildMethodsGetCalledWithCorrectArguments()
    {
        using var tester = new FrameworkDartTester();
        Key redKey = new ValueKey<string>("red");
        Key blueKey = new ValueKey<string>("blue");
        Widget BuildWidget()
        {
            return new SwapperWithProperOverrides(
                stable: new ColoredBox(key: redKey, color: Avalonia.Media.Color.FromUInt32(0xffff0000)),
                swapper: new ColoredBox(key: blueKey, color: Avalonia.Media.Color.FromUInt32(0xff0000ff)));
        }

        tester.PumpWidget(BuildWidget());
        var swapper = (SwapperElement)tester.ElementOfType<SwapperWithProperOverrides>();
        var redBox = (RenderBox)tester.ElementsWithKey(redKey).Single().RenderObject!;
        var blueBox = (RenderBox)tester.ElementsWithKey(blueKey).Single().RenderObject!;
        Assert.Equal(2, swapper.InsertSlots.Count);
        Assert.Contains("stable", swapper.InsertSlots);
        Assert.Contains(true, swapper.InsertSlots);
        Assert.Empty(swapper.MoveSlots);
        Assert.Empty(swapper.RemoveSlots);
        Assert.Equal(new Point(0, 300), ParentDataFor(redBox).offset);
        Assert.Equal(new Point(0, 0), ParentDataFor(blueBox).offset);
        tester.PumpWidget(BuildWidget());
        Assert.Equal(2, swapper.InsertSlots.Count);
        Assert.Single(swapper.MoveSlots);
        Assert.Contains(new Pair<bool>(true, false), swapper.MoveSlots);
        Assert.Empty(swapper.RemoveSlots);
        Assert.Equal(new Point(0, 0), ParentDataFor(redBox).offset);
        Assert.Equal(new Point(0, 300), ParentDataFor(blueBox).offset);
        tester.PumpWidget(new SwapperWithProperOverrides());
        Assert.False(redBox.Attached);
        Assert.False(blueBox.Attached);
        Assert.Equal(2, swapper.InsertSlots.Count);
        Assert.Single(swapper.MoveSlots);
        Assert.Equal(2, swapper.RemoveSlots.Count);
        Assert.Contains("stable", swapper.RemoveSlots);
        Assert.Contains(false, swapper.RemoveSlots);
    }

    private static BoxParentData ParentDataFor(RenderObject renderObject) => (BoxParentData)renderObject.parentData!;

    /// <summary>Dart's <c>Pair&lt;T&gt;</c>: value equality over both fields.</summary>
    private sealed record Pair<T>(T? First, T Second)
    {
        public override string ToString() => $"({First},{Second})";
    }

    /// <summary>
    /// Dart's <c>Swapper</c>: lays one child out in the top half of its size and the other in the bottom
    /// half, and swaps which child is on top every time the widget is rendered.
    /// </summary>
    private abstract class Swapper(Widget? stable = null, Widget? swapper = null, Key? key = null)
        : RenderObjectWidget(key)
    {
        public Widget? Stable { get; } = stable;

        public Widget? SwapperChild { get; } = swapper;

        public abstract override SwapperElement CreateElement();

        public override RenderObject CreateRenderObject(BuildContext context) => new RenderSwapper();
    }

    /// <summary>Dart's <c>SwapperWithProperOverrides</c>.</summary>
    private sealed class SwapperWithProperOverrides(Widget? stable = null, Widget? swapper = null, Key? key = null)
        : Swapper(stable, swapper, key)
    {
        public override SwapperElement CreateElement() => new SwapperElementWithProperOverrides(this);
    }

    /// <summary>Dart's <c>SwapperElement</c>.</summary>
    private abstract class SwapperElement(Swapper widget) : RenderObjectElement(widget)
    {
        public Element? Stable { get; private set; }

        public Element? SwapperChild { get; private set; }

        public bool SwapperIsOnTop { get; private set; } = true;

        public List<object?> InsertSlots { get; } = [];

        public List<object> MoveSlots { get; } = [];

        public List<object?> RemoveSlots { get; } = [];

        protected RenderSwapper SwapperRenderObject => (RenderSwapper)RenderObject;

        public override void VisitChildren(Action<Element> visitor)
        {
            if (Stable != null)
            {
                visitor(Stable);
            }

            if (SwapperChild != null)
            {
                visitor(SwapperChild);
            }
        }

        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            UpdateChildren((Swapper)newWidget);
        }

        // Dart's `mount` override: runs after `super.mount(parent, newSlot)`.
        protected override void OnMount()
        {
            base.OnMount();
            UpdateChildren((Swapper)Widget);
        }

        private void UpdateChildren(Swapper widget)
        {
            Stable = UpdateChild(Stable, widget.Stable, "stable");
            SwapperChild = UpdateChild(SwapperChild, widget.SwapperChild, SwapperIsOnTop);
            SwapperIsOnTop = !SwapperIsOnTop;
        }

        public override void InsertRenderObjectChild(RenderObject child, object? slot)
        {
        }

        public override void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public override void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
        }
    }

    /// <summary>Dart's <c>SwapperElementWithProperOverrides</c>.</summary>
    private sealed class SwapperElementWithProperOverrides(Swapper widget) : SwapperElement(widget)
    {
        public override void InsertRenderObjectChild(RenderObject child, object? slot)
        {
            InsertSlots.Add(slot);
            if (Equals(slot, "stable"))
            {
                SwapperRenderObject.Stable = (RenderBox)child;
            }
            else
            {
                SwapperRenderObject.SetSwapper((RenderBox)child, (bool)slot!);
            }
        }

        public override void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
            bool oldIsOnTop = (bool)oldSlot!;
            bool newIsOnTop = (bool)newSlot!;
            MoveSlots.Add(new Pair<bool>(oldIsOnTop, newIsOnTop));
            Assert.True(oldIsOnTop == !newIsOnTop);
            SwapperRenderObject.SetSwapper((RenderBox)child, newIsOnTop);
        }

        public override void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            RemoveSlots.Add(slot);
            if (Equals(slot, "stable"))
            {
                SwapperRenderObject.Stable = null;
            }
            else
            {
                SwapperRenderObject.SetSwapper(null, (bool)slot!);
            }
        }
    }

    /// <summary>Dart's <c>RenderSwapper</c>.</summary>
    private sealed class RenderSwapper : RenderBox
    {
        private RenderBox? _stable;
        private bool? _swapperIsOnTop;
        private RenderBox? _swapper;

        public RenderBox? Stable
        {
            get => _stable;
            set
            {
                if (ReferenceEquals(value, _stable))
                {
                    return;
                }

                if (_stable != null)
                {
                    DropChild(_stable);
                }

                _stable = value;
                if (value != null)
                {
                    AdoptChild(value);
                }
            }
        }

        public RenderBox? Swapper => _swapper;

        public void SetSwapper(RenderBox? child, bool isOnTop)
        {
            if (isOnTop != _swapperIsOnTop)
            {
                _swapperIsOnTop = isOnTop;
                MarkNeedsLayout();
            }

            if (ReferenceEquals(child, _swapper))
            {
                return;
            }

            if (_swapper != null)
            {
                DropChild(_swapper);
            }

            _swapper = child;
            if (child != null)
            {
                AdoptChild(child);
            }
        }

        // Dart also overrides `attach`/`detach` to recurse into the children; Plumix's
        // `RenderObject.Attach`/`Detach` already recurse through `VisitChildren`.
        public override void VisitChildren(Action<RenderObject> visitor)
        {
            if (Stable != null)
            {
                visitor(Stable);
            }

            if (Swapper != null)
            {
                visitor(Swapper);
            }
        }

        protected override Size ComputeDryLayout(BoxConstraints constraints) => constraints.Biggest;

        protected override void PerformLayout()
        {
            Assert.True(Constraints.HasBoundedWidth);
            Assert.True(Constraints.HasTightHeight);
            Size = Constraints.Biggest;
            var topOffset = new Point(0, 0);
            var bottomOffset = new Point(0, Size.Height / 2);
            BoxConstraints childConstraints = Constraints.CopyWith(
                minHeight: Constraints.MinHeight / 2,
                maxHeight: Constraints.MaxHeight / 2);
            if (Stable != null)
            {
                var stableParentData = (BoxParentData)Stable.parentData!;
                Stable.Layout(childConstraints);
                stableParentData.offset = _swapperIsOnTop!.Value ? bottomOffset : topOffset;
            }

            if (Swapper != null)
            {
                var swapperParentData = (BoxParentData)Swapper.parentData!;
                Swapper.Layout(childConstraints);
                swapperParentData.offset = _swapperIsOnTop!.Value ? topOffset : bottomOffset;
            }
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
            VisitChildren(child =>
            {
                var childParentData = (BoxParentData)child.parentData!;
                ctx.PaintChild(child, offset + childParentData.offset);
            });
        }

        protected override void RedepthChildren() => VisitChildren(RedepthChild);
    }
}
