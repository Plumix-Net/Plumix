using Plumix.Foundation;
using Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/sliver_persistent_header.dart

namespace Plumix.Widgets;

/// <summary>
/// Delegate for configuring a <see cref="SliverPersistentHeader"/>.
/// </summary>
public abstract class SliverPersistentHeaderDelegate
{
    /// <summary>Builds the header, given the shrink offset and whether it overlaps following content.</summary>
    public abstract Widget Build(BuildContext context, double shrinkOffset, bool overlapsContent);

    /// <summary>The smallest size to allow the header to reach when it shrinks at the leading edge.</summary>
    public abstract double MinExtent { get; }

    /// <summary>The size of the header when it is not shrinking at the leading edge.</summary>
    public abstract double MaxExtent { get; }

    /// <summary>The ticker provider the floating header's snap and reveal animations run on.</summary>
    public virtual ITickerProvider? Vsync => null;

    /// <summary>Specifies how a floating header animates into view, or null to disable snapping.</summary>
    public virtual FloatingHeaderSnapConfiguration? SnapConfiguration => null;

    /// <summary>Specifies how the header stretches into an overscroll, or null to disable stretching.</summary>
    public virtual OverScrollHeaderStretchConfiguration? StretchConfiguration => null;

    /// <summary>Specifies how far a reveal request may expand the header.</summary>
    public virtual PersistentHeaderShowOnScreenConfiguration? ShowOnScreenConfiguration => null;

    /// <summary>Whether this delegate is meaningfully different from the previous one.</summary>
    public abstract bool ShouldRebuild(SliverPersistentHeaderDelegate oldDelegate);
}

/// <summary>
/// A sliver whose size varies when the sliver is scrolled to the leading edge of the viewport.
/// </summary>
public sealed class SliverPersistentHeader : StatelessWidget
{
    public SliverPersistentHeader(
        SliverPersistentHeaderDelegate @delegate,
        bool pinned = false,
        bool floating = false,
        Key? key = null) : base(key)
    {
        Delegate = @delegate ?? throw new ArgumentNullException(nameof(@delegate));
        ValidateDelegate(@delegate);
        Pinned = pinned;
        Floating = floating;
    }

    public SliverPersistentHeaderDelegate Delegate { get; }

    /// <summary>Whether to stick the header to the start of the viewport once it is scrolled.</summary>
    public bool Pinned { get; }

    /// <summary>Whether the header should immediately grow again when the user reverses direction.</summary>
    public bool Floating { get; }

    public override Widget Build(BuildContext context)
    {
        ValidateDelegate(Delegate);
        if (Floating && Pinned)
        {
            return new SliverFloatingPinnedPersistentHeader(Delegate);
        }

        if (Pinned)
        {
            return new SliverPinnedPersistentHeader(Delegate);
        }

        if (Floating)
        {
            return new SliverFloatingPersistentHeader(Delegate);
        }

        return new SliverScrollingPersistentHeader(Delegate);
    }

    internal static void ValidateDelegate(SliverPersistentHeaderDelegate value)
    {
        if (!double.IsFinite(value.MinExtent) || value.MinExtent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "minExtent must be finite and non-negative.");
        }

        if (!double.IsFinite(value.MaxExtent) || value.MaxExtent < value.MinExtent)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "maxExtent must be finite and >= minExtent.");
        }
    }
}

/// <summary>Base class for the four persistent-header render object widgets.</summary>
internal abstract class SliverPersistentHeaderRenderObjectWidget : RenderObjectWidget
{
    protected SliverPersistentHeaderRenderObjectWidget(
        SliverPersistentHeaderDelegate @delegate,
        bool floating = false)
    {
        Delegate = @delegate;
        Floating = floating;
    }

    public SliverPersistentHeaderDelegate Delegate { get; }

    public bool Floating { get; }

    internal override Element CreateElement() => new SliverPersistentHeaderElement(this, Floating);
}

internal sealed class SliverScrollingPersistentHeader : SliverPersistentHeaderRenderObjectWidget
{
    public SliverScrollingPersistentHeader(SliverPersistentHeaderDelegate @delegate) : base(@delegate)
    {
    }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverScrollingPersistentHeader(
            minExtent: Delegate.MinExtent,
            maxExtent: Delegate.MaxExtent,
            stretchConfiguration: Delegate.StretchConfiguration);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var header = (RenderSliverScrollingPersistentHeader)renderObject;
        header.MinExtent = Delegate.MinExtent;
        header.MaxExtent = Delegate.MaxExtent;
        header.StretchConfiguration = Delegate.StretchConfiguration;
    }
}

internal sealed class SliverPinnedPersistentHeader : SliverPersistentHeaderRenderObjectWidget
{
    public SliverPinnedPersistentHeader(SliverPersistentHeaderDelegate @delegate) : base(@delegate)
    {
    }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverPinnedPersistentHeader(
            minExtent: Delegate.MinExtent,
            maxExtent: Delegate.MaxExtent,
            stretchConfiguration: Delegate.StretchConfiguration,
            showOnScreenConfiguration: Delegate.ShowOnScreenConfiguration);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var header = (RenderSliverPinnedPersistentHeader)renderObject;
        header.MinExtent = Delegate.MinExtent;
        header.MaxExtent = Delegate.MaxExtent;
        header.StretchConfiguration = Delegate.StretchConfiguration;
        header.ShowOnScreenConfiguration = Delegate.ShowOnScreenConfiguration;
    }
}

internal sealed class SliverFloatingPersistentHeader : SliverPersistentHeaderRenderObjectWidget
{
    public SliverFloatingPersistentHeader(SliverPersistentHeaderDelegate @delegate)
        : base(@delegate, floating: true)
    {
    }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverFloatingPersistentHeader(
            minExtent: Delegate.MinExtent,
            maxExtent: Delegate.MaxExtent,
            vsync: Delegate.Vsync,
            snapConfiguration: Delegate.SnapConfiguration,
            stretchConfiguration: Delegate.StretchConfiguration,
            showOnScreenConfiguration: Delegate.ShowOnScreenConfiguration);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var header = (RenderSliverFloatingPersistentHeader)renderObject;
        header.MinExtent = Delegate.MinExtent;
        header.MaxExtent = Delegate.MaxExtent;
        header.Vsync = Delegate.Vsync;
        header.SnapConfiguration = Delegate.SnapConfiguration;
        header.StretchConfiguration = Delegate.StretchConfiguration;
        header.ShowOnScreenConfiguration = Delegate.ShowOnScreenConfiguration;
    }
}

internal sealed class SliverFloatingPinnedPersistentHeader : SliverPersistentHeaderRenderObjectWidget
{
    public SliverFloatingPinnedPersistentHeader(SliverPersistentHeaderDelegate @delegate)
        : base(@delegate, floating: true)
    {
    }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverFloatingPinnedPersistentHeader(
            minExtent: Delegate.MinExtent,
            maxExtent: Delegate.MaxExtent,
            vsync: Delegate.Vsync,
            snapConfiguration: Delegate.SnapConfiguration,
            stretchConfiguration: Delegate.StretchConfiguration,
            showOnScreenConfiguration: Delegate.ShowOnScreenConfiguration);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var header = (RenderSliverFloatingPinnedPersistentHeader)renderObject;
        header.MinExtent = Delegate.MinExtent;
        header.MaxExtent = Delegate.MaxExtent;
        header.Vsync = Delegate.Vsync;
        header.SnapConfiguration = Delegate.SnapConfiguration;
        header.StretchConfiguration = Delegate.StretchConfiguration;
        header.ShowOnScreenConfiguration = Delegate.ShowOnScreenConfiguration;
    }
}

/// <summary>
/// The element that rebuilds a persistent header's child during layout, from the shrink offset the
/// render object computed.
/// </summary>
internal sealed class SliverPersistentHeaderElement : RenderObjectElement
{
    private Element? _child;

    public SliverPersistentHeaderElement(
        SliverPersistentHeaderRenderObjectWidget widget,
        bool floating = false) : base(widget)
    {
        Floating = floating;
    }

    /// <summary>Whether the built child is wrapped in the snap-driving <see cref="FloatingHeader"/>.</summary>
    public bool Floating { get; }

    private SliverPersistentHeaderRenderObjectWidget HeaderWidget =>
        (SliverPersistentHeaderRenderObjectWidget)Widget;

    private RenderSliverPersistentHeader HeaderRenderObject =>
        (RenderSliverPersistentHeader)RequireRenderObject();

    protected override void OnMount()
    {
        base.OnMount();
        HeaderRenderObject.ChildBuilder = BuildChildDuringLayout;
    }

    internal override void Unmount()
    {
        HeaderRenderObject.ChildBuilder = null;
        if (_child != null)
        {
            Element mountedChild = _child;
            _child = null;
            UnmountChild(mountedChild);
        }

        base.Unmount();
    }

    internal override void Update(Widget newWidget)
    {
        SliverPersistentHeaderDelegate oldDelegate = HeaderWidget.Delegate;
        base.Update(newWidget);
        HeaderRenderObject.ChildBuilder = BuildChildDuringLayout;
        SliverPersistentHeaderDelegate newDelegate = HeaderWidget.Delegate;
        if (!ReferenceEquals(newDelegate, oldDelegate)
            && (newDelegate.GetType() != oldDelegate.GetType() || newDelegate.ShouldRebuild(oldDelegate)))
        {
            UpdateHeaderChild(
                newDelegate,
                HeaderRenderObject.LastShrinkOffset,
                HeaderRenderObject.LastOverlapsContent);
            HeaderRenderObject.MarkNeedsLayout();
        }
    }

    internal override void Rebuild()
    {
        base.Rebuild();
        HeaderRenderObject.MarkNeedsLayout();
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
        if (ReferenceEquals(child, _child))
        {
            _child = null;
        }
    }

    public override void InsertRenderObjectChild(RenderObject child, object? slot)
    {
        HeaderRenderObject.Child = (RenderBox)child;
    }

    public override void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
    {
        throw new InvalidOperationException("SliverPersistentHeader does not support moving its child.");
    }

    public override void RemoveRenderObjectChild(RenderObject child, object? slot)
    {
        if (ReferenceEquals(HeaderRenderObject.Child, child))
        {
            HeaderRenderObject.Child = null;
        }
    }

    private void BuildChildDuringLayout(double shrinkOffset, bool overlapsContent)
    {
        UpdateHeaderChild(HeaderWidget.Delegate, shrinkOffset, overlapsContent);
    }

    private void UpdateHeaderChild(
        SliverPersistentHeaderDelegate @delegate,
        double shrinkOffset,
        bool overlapsContent)
    {
        Widget built = @delegate.Build(new BuildContext(this), shrinkOffset, overlapsContent);
        _child = UpdateChild(_child, Floating ? new FloatingHeader(built) : built, null);
    }
}

/// <summary>
/// Wraps a floating header's child so the header learns when a scroll gesture starts and ends, which
/// is what drives its snap animation.
/// </summary>
internal sealed class FloatingHeader : StatefulWidget
{
    public FloatingHeader(Widget child)
    {
        Child = child;
    }

    public Widget Child { get; }

    public override State CreateState() => new FloatingHeaderState();
}

internal sealed class FloatingHeaderState : State
{
    private ScrollPosition? _position;

    private FloatingHeader CurrentWidget => (FloatingHeader)StateWidget;

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
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

        RenderSliverFloatingPersistentHeader? header = null;
        Context.VisitAncestorElements(ancestor =>
        {
            if (ancestor.RenderObject is RenderSliverFloatingPersistentHeader floatingHeader)
            {
                header = floatingHeader;
                return false;
            }

            return true;
        });

        if (header == null)
        {
            return;
        }

        if (_position.IsScrollingNotifier.Value)
        {
            header.UpdateScrollStartDirection(_position.UserScrollDirection);
            header.MaybeStopSnapAnimation(_position.UserScrollDirection);
        }
        else
        {
            header.MaybeStartSnapAnimation(_position.UserScrollDirection);
        }
    }
}
