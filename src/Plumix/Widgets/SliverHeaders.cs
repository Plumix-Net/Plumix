using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/sliver_resizing_header.dart
// flutter/packages/flutter/lib/src/widgets/sliver_floating_header.dart

public sealed class SliverResizingHeader : StatelessWidget
{
    public SliverResizingHeader(
        Widget? minExtentPrototype = null,
        Widget? maxExtentPrototype = null,
        Widget? child = null,
        Key? key = null) : base(key)
    {
        MinExtentPrototype = minExtentPrototype;
        MaxExtentPrototype = maxExtentPrototype;
        Child = child;
    }

    public Widget? MinExtentPrototype { get; }

    public Widget? MaxExtentPrototype { get; }

    public Widget? Child { get; }

    public override Widget Build(BuildContext context)
    {
        return new SliverResizingHeaderRenderObjectWidget(
            minExtentPrototype: ExcludePrototypeFocus(MinExtentPrototype),
            maxExtentPrototype: ExcludePrototypeFocus(MaxExtentPrototype),
            child: new Semantics(
                container: true,
                explicitChildNodes: true,
                child: Child ?? new SizedBox()));
    }

    private static Widget? ExcludePrototypeFocus(Widget? prototype)
    {
        return prototype == null ? null : new ExcludeFocus(prototype);
    }
}

internal enum SliverResizingHeaderSlot
{
    MinExtent,
    MaxExtent,
    Child
}

internal sealed class SliverResizingHeaderRenderObjectWidget : RenderObjectWidget
{
    public SliverResizingHeaderRenderObjectWidget(
        Widget? minExtentPrototype,
        Widget? maxExtentPrototype,
        Widget child) : base()
    {
        MinExtentPrototype = minExtentPrototype;
        MaxExtentPrototype = maxExtentPrototype;
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public Widget? MinExtentPrototype { get; }

    public Widget? MaxExtentPrototype { get; }

    public Widget Child { get; }

    internal override Element CreateElement() => new SliverResizingHeaderElement(this);

    public override RenderObject CreateRenderObject(BuildContext context) => new RenderSliverResizingHeader();
}

internal sealed class SliverResizingHeaderElement : RenderObjectElement
{
    private Element? _minExtentPrototype;
    private Element? _maxExtentPrototype;
    private Element? _child;

    public SliverResizingHeaderElement(SliverResizingHeaderRenderObjectWidget widget) : base(widget)
    {
    }

    private SliverResizingHeaderRenderObjectWidget CurrentWidget =>
        (SliverResizingHeaderRenderObjectWidget)Widget;

    private RenderSliverResizingHeader HeaderRenderObject =>
        (RenderSliverResizingHeader)RequireRenderObject();

    protected override void OnMount()
    {
        base.OnMount();
        UpdateSlotChildren();
    }

    internal override void Rebuild()
    {
        base.Rebuild();
        UpdateSlotChildren();
    }

    internal override void Update(Widget newWidget)
    {
        base.Update(newWidget);
        UpdateSlotChildren();
    }

    internal override void VisitChildren(Action<Element> visitor)
    {
        if (_minExtentPrototype != null)
        {
            visitor(_minExtentPrototype);
        }

        if (_maxExtentPrototype != null)
        {
            visitor(_maxExtentPrototype);
        }

        if (_child != null)
        {
            visitor(_child);
        }
    }

    internal override void ForgetChild(Element child)
    {
        if (ReferenceEquals(child, _minExtentPrototype))
        {
            _minExtentPrototype = null;
        }
        else if (ReferenceEquals(child, _maxExtentPrototype))
        {
            _maxExtentPrototype = null;
        }
        else if (ReferenceEquals(child, _child))
        {
            _child = null;
        }
    }

    public override void InsertRenderObjectChild(RenderObject child, object? slot)
    {
        SetRenderObjectChild(slot, (RenderBox)child);
    }

    public override void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
    {
        if (!Equals(oldSlot, newSlot))
        {
            throw new InvalidOperationException("SliverResizingHeader children cannot move between slots.");
        }
    }

    public override void RemoveRenderObjectChild(RenderObject child, object? slot)
    {
        SetRenderObjectChild(slot, null);
    }

    internal override void Unmount()
    {
        UnmountSlotChild(ref _minExtentPrototype);
        UnmountSlotChild(ref _maxExtentPrototype);
        UnmountSlotChild(ref _child);
        base.Unmount();
    }

    private void UpdateSlotChildren()
    {
        _minExtentPrototype = UpdateChild(
            _minExtentPrototype,
            CurrentWidget.MinExtentPrototype,
            SliverResizingHeaderSlot.MinExtent);
        _maxExtentPrototype = UpdateChild(
            _maxExtentPrototype,
            CurrentWidget.MaxExtentPrototype,
            SliverResizingHeaderSlot.MaxExtent);
        _child = UpdateChild(_child, CurrentWidget.Child, SliverResizingHeaderSlot.Child);
    }

    private void SetRenderObjectChild(object? slot, RenderBox? child)
    {
        switch (slot)
        {
            case SliverResizingHeaderSlot.MinExtent:
                HeaderRenderObject.MinExtentPrototype = child;
                break;
            case SliverResizingHeaderSlot.MaxExtent:
                HeaderRenderObject.MaxExtentPrototype = child;
                break;
            case SliverResizingHeaderSlot.Child:
                HeaderRenderObject.Child = child;
                break;
            default:
                throw new InvalidOperationException("Unknown SliverResizingHeader child slot.");
        }
    }

    private void UnmountSlotChild(ref Element? child)
    {
        if (child == null)
        {
            return;
        }

        Element mountedChild = child;
        child = null;
        UnmountChild(mountedChild);
    }
}

public enum FloatingHeaderSnapMode
{
    Overlay,
    Scroll
}

public sealed class SliverFloatingHeader : StatefulWidget
{
    public SliverFloatingHeader(
        Widget child,
        AnimationStyle? animationStyle = null,
        FloatingHeaderSnapMode? snapMode = null,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        AnimationStyle = animationStyle;
        SnapMode = snapMode;
    }

    public AnimationStyle? AnimationStyle { get; }

    public FloatingHeaderSnapMode? SnapMode { get; }

    /// <summary>
    /// How far a show-on-screen request may expand this header. Null lets the enclosing viewport
    /// scroll the request into view instead of expanding the header.
    /// </summary>
    public PersistentHeaderShowOnScreenConfiguration? ShowOnScreenConfiguration { get; init; }

    public Widget Child { get; }

    public override State CreateState() => new SliverFloatingHeaderState();
}

internal sealed class SliverFloatingHeaderState : State
{
    private SliverFloatingHeader CurrentWidget => (SliverFloatingHeader)StateWidget;

    public override Widget Build(BuildContext context)
    {
        return new SliverFloatingHeaderRenderObjectWidget(
            animationStyle: CurrentWidget.AnimationStyle,
            snapMode: CurrentWidget.SnapMode,
            child: new SliverFloatingHeaderSnapTrigger(CurrentWidget.Child))
        {
            ShowOnScreenConfiguration = CurrentWidget.ShowOnScreenConfiguration,
        };
    }
}

internal sealed class SliverFloatingHeaderSnapTrigger : StatefulWidget
{
    public SliverFloatingHeaderSnapTrigger(Widget child)
    {
        Child = child;
    }

    public Widget Child { get; }

    public override State CreateState() => new SliverFloatingHeaderSnapTriggerState();
}

internal sealed class SliverFloatingHeaderSnapTriggerState : State
{
    private ScrollPosition? _position;

    private SliverFloatingHeaderSnapTrigger CurrentWidget =>
        (SliverFloatingHeaderSnapTrigger)StateWidget;

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        ReplacePosition(Scrollable.MaybeOf(Context)?.Position);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        ReplacePosition(Scrollable.MaybeOf(Context)?.Position);
    }

    public override void Dispose()
    {
        ReplacePosition(null);
        base.Dispose();
    }

    public override Widget Build(BuildContext context) => CurrentWidget.Child;

    private void ReplacePosition(ScrollPosition? position)
    {
        if (ReferenceEquals(_position, position))
        {
            return;
        }

        _position?.IsScrollingNotifier.RemoveListener(HandleIsScrollingChanged);
        _position = position;
        _position?.IsScrollingNotifier.AddListener(HandleIsScrollingChanged);
    }

    private void HandleIsScrollingChanged()
    {
        if (_position == null)
        {
            return;
        }

        RenderSliverFloatingHeader? renderer = null;
        Context.VisitAncestorElements(ancestor =>
        {
            if (ancestor.RenderObject is RenderSliverFloatingHeader floatingHeader)
            {
                renderer = floatingHeader;
                return false;
            }

            return true;
        });
        renderer?.IsScrollingUpdate(_position);
    }
}

internal sealed class SliverFloatingHeaderRenderObjectWidget : SingleChildRenderObjectWidget
{
    public SliverFloatingHeaderRenderObjectWidget(
        AnimationStyle? animationStyle,
        FloatingHeaderSnapMode? snapMode,
        Widget child) : base(child)
    {
        AnimationStyle = animationStyle;
        SnapMode = snapMode;
    }

    public AnimationStyle? AnimationStyle { get; }

    public FloatingHeaderSnapMode? SnapMode { get; }

    public PersistentHeaderShowOnScreenConfiguration? ShowOnScreenConfiguration { get; init; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverFloatingHeader(
            animationStyle: AnimationStyle,
            snapMode: SnapMode)
        {
            ShowOnScreenConfiguration = ShowOnScreenConfiguration,
        };
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var floatingHeader = (RenderSliverFloatingHeader)renderObject;
        floatingHeader.AnimationStyle = AnimationStyle;
        floatingHeader.SnapMode = SnapMode;
        floatingHeader.ShowOnScreenConfiguration = ShowOnScreenConfiguration;
    }
}
