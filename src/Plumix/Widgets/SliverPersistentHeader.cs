using Plumix.Foundation;
using Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/sliver_persistent_header.dart

namespace Plumix.Widgets;

/// <summary>
/// Delegate for configuring a <see cref="SliverPersistentHeader"/>.
/// </summary>
public abstract class SliverPersistentHeaderDelegate
{
    /// <summary>The context in which the header is built.</summary>
    /// <remarks>
    /// <paramref name="shrinkOffset"/> is a distance from <see cref="MaxExtent"/> towards
    /// <see cref="MinExtent"/> representing the current amount by which the sliver has been shrunk.
    /// When the <paramref name="shrinkOffset"/> is zero, the contents will be rendered with a
    /// dimension of <see cref="MaxExtent"/> in the main axis. When <paramref name="shrinkOffset"/>
    /// equals the difference between <see cref="MaxExtent"/> and <see cref="MinExtent"/> (a positive
    /// number), the contents will be rendered with a dimension of <see cref="MinExtent"/> in the main
    /// axis. The <paramref name="shrinkOffset"/> will always be a positive number in that range.
    /// <paramref name="overlapsContent"/> is true if subsequent slivers (if any) will be rendered
    /// beneath this one, and false if the sliver will not have any contents below it.
    /// </remarks>
    public abstract Widget Build(BuildContext context, double shrinkOffset, bool overlapsContent);

    /// <summary>The smallest size to allow the header to reach, when it shrinks at the start of the viewport.</summary>
    /// <remarks>This must return a value equal to or less than <see cref="MaxExtent"/>.</remarks>
    public abstract double MinExtent { get; }

    /// <summary>The size of the header when it is not shrinking at the top of the viewport.</summary>
    /// <remarks>This must return a value equal to or greater than <see cref="MinExtent"/>.</remarks>
    public abstract double MaxExtent { get; }

    /// <summary>
    /// A <see cref="ITickerProvider"/> to use to drive the snap and <c>showOnScreen</c> animations
    /// of floating headers, or null if they should not animate.
    /// </summary>
    public virtual ITickerProvider? Vsync => null;

    /// <summary>Specifies how floating headers should animate in and out of view.</summary>
    /// <remarks>If the value of this property is null, then floating headers will not animate.</remarks>
    public virtual FloatingHeaderSnapConfiguration? SnapConfiguration => null;

    /// <summary>Specifies an <c>AsyncCallback</c> and offset for execution.</summary>
    /// <remarks>If the value of this property is null, then callback will not be triggered.</remarks>
    public virtual OverScrollHeaderStretchConfiguration? StretchConfiguration => null;

    /// <summary>
    /// Specifies how floating headers and pinned headers should behave in response to
    /// <c>showOnScreen</c>.
    /// </summary>
    /// <remarks>
    /// If set to null, the persistent header will delegate the <c>showOnScreen</c> call to its parent.
    /// </remarks>
    public virtual PersistentHeaderShowOnScreenConfiguration? ShowOnScreenConfiguration => null;

    /// <summary>
    /// Whether this delegate is meaningfully different from the old delegate.
    /// </summary>
    /// <remarks>
    /// If this returns false, then the header might not be rebuilt, even though the instance of the
    /// delegate changed.
    /// </remarks>
    public abstract bool ShouldRebuild(SliverPersistentHeaderDelegate oldDelegate);
}

/// <summary>
/// A sliver whose size varies when the sliver is scrolled to the edge of the viewport opposite the
/// sliver's <see cref="GrowthDirection"/>.
/// </summary>
/// <remarks>
/// In the normal case of a <see cref="CustomScrollView"/> with no centered sliver, this sliver will
/// vary its size when scrolled to the leading edge of the viewport. This is the layout primitive
/// that <c>SliverAppBar</c> uses for its shrinking/growing effect.
/// </remarks>
public class SliverPersistentHeader : StatelessWidget
{
    /// <summary>Creates a sliver that varies its size when it is scrolled to the start of a viewport.</summary>
    public SliverPersistentHeader(
        SliverPersistentHeaderDelegate @delegate,
        bool pinned = false,
        bool floating = false,
        Key? key = null) : base(key)
    {
        Delegate = @delegate;
        Pinned = pinned;
        Floating = floating;
    }

    /// <summary>Configuration for the sliver's layout.</summary>
    public SliverPersistentHeaderDelegate Delegate { get; }

    /// <summary>
    /// Whether to stick the header to the start of the viewport once it has reached its minimum size.
    /// </summary>
    public bool Pinned { get; }

    /// <summary>Whether the header should immediately grow again if the user reverses scroll direction.</summary>
    public bool Floating { get; }

    public override Widget Build(BuildContext context)
    {
        if (Floating && Pinned)
        {
            return new SliverFloatingPinnedPersistentHeader(@delegate: Delegate);
        }

        if (Pinned)
        {
            return new SliverPinnedPersistentHeader(@delegate: Delegate);
        }

        if (Floating)
        {
            return new SliverFloatingPersistentHeader(@delegate: Delegate);
        }

        return new SliverScrollingPersistentHeader(@delegate: Delegate);
    }

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<SliverPersistentHeaderDelegate>("delegate", Delegate));
        var flags = new List<string>();
        if (Pinned)
        {
            flags.Add("pinned");
        }

        if (Floating)
        {
            flags.Add("floating");
        }

        if (flags.Count == 0)
        {
            flags.Add("normal");
        }

        properties.Add(new IterableProperty<string>("mode", flags));
    }
}

/// <summary>Dart's private <c>_FloatingHeader</c>.</summary>
internal sealed class FloatingHeader : StatefulWidget
{
    public FloatingHeader(Widget child)
    {
        Child = child;
    }

    public Widget Child { get; }

    public override State CreateState() => new FloatingHeaderState();
}

/// <summary>Dart's private <c>_FloatingHeaderState</c>.</summary>
/// <remarks>
/// A <see cref="ScrollPosition.IsScrollingNotifier"/> listener that drives the floating header's
/// snap animation: it starts when a scroll gesture ends and stops when one begins.
/// </remarks>
internal sealed class FloatingHeaderState : State<FloatingHeader>
{
    private ScrollPosition? _position;

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        _position?.IsScrollingNotifier.RemoveListener(IsScrollingListener);

        _position = Scrollable.MaybeOf(Context)?.Position;
        _position?.IsScrollingNotifier.AddListener(IsScrollingListener);
    }

    public override void Dispose()
    {
        _position?.IsScrollingNotifier.RemoveListener(IsScrollingListener);

        base.Dispose();
    }

    private RenderSliverFloatingPersistentHeader? HeaderRenderer()
    {
        return Context.FindAncestorRenderObjectOfType<RenderSliverFloatingPersistentHeader>();
    }

    private void IsScrollingListener()
    {
        DebugAssertions.Assert(_position != null);

        // When a scroll stops, then maybe snap the app bar into view.
        // Similarly, when a scroll starts, then maybe stop the snap animation.
        // Update the scrolling direction as well for pointer scrolling updates.
        RenderSliverFloatingPersistentHeader? header = HeaderRenderer();
        if (_position!.IsScrollingNotifier.Value)
        {
            header?.UpdateScrollStartDirection(_position.UserScrollDirection);
            // Only SliverAppBars support snapping, headers will not snap.
            header?.MaybeStopSnapAnimation(_position.UserScrollDirection);
        }
        else
        {
            // Only SliverAppBars support snapping, headers will not snap.
            header?.MaybeStartSnapAnimation(_position.UserScrollDirection);
        }
    }

    public override Widget Build(BuildContext context) => Widget.Child;
}

/// <summary>Dart's private <c>_SliverPersistentHeaderElement</c>.</summary>
internal sealed class SliverPersistentHeaderElement : RenderObjectElement
{
    private Element? _child;

    public SliverPersistentHeaderElement(
        SliverPersistentHeaderRenderObjectWidget widget,
        bool floating = false) : base(widget)
    {
        Floating = floating;
    }

    public bool Floating { get; }

    /// <summary>Dart's covariant <c>renderObject</c> getter.</summary>
    private IRenderSliverPersistentHeaderForWidgets HeaderRenderObject =>
        (IRenderSliverPersistentHeaderForWidgets)RenderObject;

    protected override void OnMount()
    {
        base.OnMount();
        HeaderRenderObject.Element = this;
    }

    public override void Unmount()
    {
        HeaderRenderObject.Element = null;
        base.Unmount();
    }

    public override void Update(Widget newWidget)
    {
        var oldWidget = (SliverPersistentHeaderRenderObjectWidget)Widget;
        base.Update(newWidget);
        var updatedWidget = (SliverPersistentHeaderRenderObjectWidget)newWidget;
        SliverPersistentHeaderDelegate newDelegate = updatedWidget.Delegate;
        SliverPersistentHeaderDelegate oldDelegate = oldWidget.Delegate;
        if (!ReferenceEquals(newDelegate, oldDelegate)
            && (newDelegate.GetType() != oldDelegate.GetType() || newDelegate.ShouldRebuild(oldDelegate)))
        {
            var header = (RenderSliverPersistentHeader)RenderObject;
            UpdateChild(newDelegate, header.LastShrinkOffset, header.LastOverlapsContent);
            HeaderRenderObject.TriggerRebuild();
        }
    }

    protected override void PerformRebuild()
    {
        base.PerformRebuild();
        HeaderRenderObject.TriggerRebuild();
    }

    private void UpdateChild(
        SliverPersistentHeaderDelegate @delegate,
        double shrinkOffset,
        bool overlapsContent)
    {
        Widget newWidget = @delegate.Build(this, shrinkOffset, overlapsContent);
        _child = UpdateChild(_child, Floating ? new FloatingHeader(child: newWidget) : newWidget, null);
    }

    /// <summary>Dart's <c>_build</c>: rebuilds the child from the render object's layout pass.</summary>
    internal void Build(double shrinkOffset, bool overlapsContent)
    {
        Owner!.BuildScope(this, () =>
        {
            var widget = (SliverPersistentHeaderRenderObjectWidget)Widget;
            UpdateChild(widget.Delegate, shrinkOffset, overlapsContent);
        });
    }

    public override void ForgetChild(Element child)
    {
        DebugAssertions.Assert(ReferenceEquals(child, _child));
        _child = null;
        base.ForgetChild(child);
    }

    public override void InsertRenderObjectChild(RenderObject child, object? slot)
    {
        var header = (RenderSliverPersistentHeader)RenderObject;
        DebugAssertions.Assert(Rendering.RenderObject.DebugValidateChildType<RenderBox>(header, child));
        header.Child = (RenderBox)child;
    }

    public override void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
    {
        DebugAssertions.Assert(false);
    }

    public override void RemoveRenderObjectChild(RenderObject child, object? slot)
    {
        ((RenderSliverPersistentHeader)RenderObject).Child = null;
    }

    public override void VisitChildren(Action<Element> visitor)
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }
}

/// <summary>Dart's private <c>_SliverPersistentHeaderRenderObjectWidget</c>.</summary>
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

    public override Element CreateElement() => new SliverPersistentHeaderElement(this, floating: Floating);

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<SliverPersistentHeaderDelegate>("delegate", Delegate));
    }
}

/// <summary>
/// The members Dart's private <c>_RenderSliverPersistentHeaderForWidgetsMixin</c> adds to a
/// <see cref="RenderSliverPersistentHeader"/>.
/// </summary>
/// <remarks>
/// C# has no mixins: each of the four <c>_RenderSliver*PersistentHeaderForWidgets</c> classes
/// implements this interface by forwarding to <see cref="RenderSliverPersistentHeaderForWidgetsMixin"/>.
/// </remarks>
internal interface IRenderSliverPersistentHeaderForWidgets
{
    SliverPersistentHeaderElement? Element { get; set; }

    void TriggerRebuild();
}

/// <summary>The bodies of Dart's private <c>_RenderSliverPersistentHeaderForWidgetsMixin</c>.</summary>
internal sealed class RenderSliverPersistentHeaderForWidgetsMixin(RenderSliverPersistentHeader renderObject)
{
    public SliverPersistentHeaderElement? Element { get; set; }

    private SliverPersistentHeaderDelegate Delegate =>
        ((SliverPersistentHeaderRenderObjectWidget)Element!.Widget).Delegate;

    public double MinExtent => Delegate.MinExtent;

    public double MaxExtent => Delegate.MaxExtent;

    public void UpdateChild(double shrinkOffset, bool overlapsContent)
    {
        DebugAssertions.Assert(Element != null);
        Element!.Build(shrinkOffset, overlapsContent);
    }

    public void TriggerRebuild()
    {
        renderObject.MarkNeedsLayout();
    }
}

/// <summary>Dart's private <c>_SliverScrollingPersistentHeader</c>.</summary>
internal sealed class SliverScrollingPersistentHeader : SliverPersistentHeaderRenderObjectWidget
{
    public SliverScrollingPersistentHeader(SliverPersistentHeaderDelegate @delegate) : base(@delegate)
    {
    }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverScrollingPersistentHeaderForWidgets(
            stretchConfiguration: Delegate.StretchConfiguration);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderSliverScrollingPersistentHeaderForWidgets)renderObject).StretchConfiguration =
            Delegate.StretchConfiguration;
    }
}

/// <summary>Dart's private <c>_RenderSliverScrollingPersistentHeaderForWidgets</c>.</summary>
internal sealed class RenderSliverScrollingPersistentHeaderForWidgets
    : RenderSliverScrollingPersistentHeader, IRenderSliverPersistentHeaderForWidgets
{
    private readonly RenderSliverPersistentHeaderForWidgetsMixin _mixin;

    public RenderSliverScrollingPersistentHeaderForWidgets(
        OverScrollHeaderStretchConfiguration? stretchConfiguration = null)
        : base(stretchConfiguration: stretchConfiguration)
    {
        _mixin = new RenderSliverPersistentHeaderForWidgetsMixin(this);
    }

    SliverPersistentHeaderElement? IRenderSliverPersistentHeaderForWidgets.Element
    {
        get => _mixin.Element;
        set => _mixin.Element = value;
    }

    public override double MinExtent => _mixin.MinExtent;

    public override double MaxExtent => _mixin.MaxExtent;

    protected override void UpdateChild(double shrinkOffset, bool overlapsContent) =>
        _mixin.UpdateChild(shrinkOffset, overlapsContent);

    public void TriggerRebuild() => _mixin.TriggerRebuild();
}

/// <summary>Dart's private <c>_SliverPinnedPersistentHeader</c>.</summary>
internal sealed class SliverPinnedPersistentHeader : SliverPersistentHeaderRenderObjectWidget
{
    public SliverPinnedPersistentHeader(SliverPersistentHeaderDelegate @delegate) : base(@delegate)
    {
    }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverPinnedPersistentHeaderForWidgets(
            stretchConfiguration: Delegate.StretchConfiguration,
            showOnScreenConfiguration: Delegate.ShowOnScreenConfiguration);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var header = (RenderSliverPinnedPersistentHeaderForWidgets)renderObject;
        header.StretchConfiguration = Delegate.StretchConfiguration;
        header.ShowOnScreenConfiguration = Delegate.ShowOnScreenConfiguration;
    }
}

/// <summary>Dart's private <c>_RenderSliverPinnedPersistentHeaderForWidgets</c>.</summary>
internal sealed class RenderSliverPinnedPersistentHeaderForWidgets
    : RenderSliverPinnedPersistentHeader, IRenderSliverPersistentHeaderForWidgets
{
    private readonly RenderSliverPersistentHeaderForWidgetsMixin _mixin;

    public RenderSliverPinnedPersistentHeaderForWidgets(
        OverScrollHeaderStretchConfiguration? stretchConfiguration,
        PersistentHeaderShowOnScreenConfiguration? showOnScreenConfiguration)
        : base(stretchConfiguration: stretchConfiguration)
    {
        _mixin = new RenderSliverPersistentHeaderForWidgetsMixin(this);

        // Dart's super-parameter forwards the delegate's value, null included; the base
        // constructor only substitutes its default for an omitted argument.
        ShowOnScreenConfiguration = showOnScreenConfiguration;
    }

    SliverPersistentHeaderElement? IRenderSliverPersistentHeaderForWidgets.Element
    {
        get => _mixin.Element;
        set => _mixin.Element = value;
    }

    public override double MinExtent => _mixin.MinExtent;

    public override double MaxExtent => _mixin.MaxExtent;

    protected override void UpdateChild(double shrinkOffset, bool overlapsContent) =>
        _mixin.UpdateChild(shrinkOffset, overlapsContent);

    public void TriggerRebuild() => _mixin.TriggerRebuild();
}

/// <summary>Dart's private <c>_SliverFloatingPersistentHeader</c>.</summary>
internal sealed class SliverFloatingPersistentHeader : SliverPersistentHeaderRenderObjectWidget
{
    public SliverFloatingPersistentHeader(SliverPersistentHeaderDelegate @delegate)
        : base(@delegate, floating: true)
    {
    }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverFloatingPersistentHeaderForWidgets(
            vsync: Delegate.Vsync,
            snapConfiguration: Delegate.SnapConfiguration,
            stretchConfiguration: Delegate.StretchConfiguration,
            showOnScreenConfiguration: Delegate.ShowOnScreenConfiguration);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var header = (RenderSliverFloatingPersistentHeaderForWidgets)renderObject;
        header.Vsync = Delegate.Vsync;
        header.SnapConfiguration = Delegate.SnapConfiguration;
        header.StretchConfiguration = Delegate.StretchConfiguration;
        header.ShowOnScreenConfiguration = Delegate.ShowOnScreenConfiguration;
    }
}

/// <summary>Dart's private <c>_RenderSliverFloatingPersistentHeaderForWidgets</c>.</summary>
internal sealed class RenderSliverFloatingPersistentHeaderForWidgets
    : RenderSliverFloatingPersistentHeader, IRenderSliverPersistentHeaderForWidgets
{
    private readonly RenderSliverPersistentHeaderForWidgetsMixin _mixin;

    public RenderSliverFloatingPersistentHeaderForWidgets(
        ITickerProvider? vsync,
        FloatingHeaderSnapConfiguration? snapConfiguration = null,
        OverScrollHeaderStretchConfiguration? stretchConfiguration = null,
        PersistentHeaderShowOnScreenConfiguration? showOnScreenConfiguration = null)
        : base(
            showOnScreenConfiguration,
            vsync: vsync,
            snapConfiguration: snapConfiguration,
            stretchConfiguration: stretchConfiguration)
    {
        _mixin = new RenderSliverPersistentHeaderForWidgetsMixin(this);
    }

    SliverPersistentHeaderElement? IRenderSliverPersistentHeaderForWidgets.Element
    {
        get => _mixin.Element;
        set => _mixin.Element = value;
    }

    public override double MinExtent => _mixin.MinExtent;

    public override double MaxExtent => _mixin.MaxExtent;

    protected override void UpdateChild(double shrinkOffset, bool overlapsContent) =>
        _mixin.UpdateChild(shrinkOffset, overlapsContent);

    public void TriggerRebuild() => _mixin.TriggerRebuild();
}

/// <summary>Dart's private <c>_SliverFloatingPinnedPersistentHeader</c>.</summary>
internal sealed class SliverFloatingPinnedPersistentHeader : SliverPersistentHeaderRenderObjectWidget
{
    public SliverFloatingPinnedPersistentHeader(SliverPersistentHeaderDelegate @delegate)
        : base(@delegate, floating: true)
    {
    }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverFloatingPinnedPersistentHeaderForWidgets(
            vsync: Delegate.Vsync,
            snapConfiguration: Delegate.SnapConfiguration,
            stretchConfiguration: Delegate.StretchConfiguration,
            showOnScreenConfiguration: Delegate.ShowOnScreenConfiguration);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var header = (RenderSliverFloatingPinnedPersistentHeaderForWidgets)renderObject;
        header.Vsync = Delegate.Vsync;
        header.SnapConfiguration = Delegate.SnapConfiguration;
        header.StretchConfiguration = Delegate.StretchConfiguration;
        header.ShowOnScreenConfiguration = Delegate.ShowOnScreenConfiguration;
    }
}

/// <summary>Dart's private <c>_RenderSliverFloatingPinnedPersistentHeaderForWidgets</c>.</summary>
internal sealed class RenderSliverFloatingPinnedPersistentHeaderForWidgets
    : RenderSliverFloatingPinnedPersistentHeader, IRenderSliverPersistentHeaderForWidgets
{
    private readonly RenderSliverPersistentHeaderForWidgetsMixin _mixin;

    public RenderSliverFloatingPinnedPersistentHeaderForWidgets(
        ITickerProvider? vsync,
        FloatingHeaderSnapConfiguration? snapConfiguration = null,
        OverScrollHeaderStretchConfiguration? stretchConfiguration = null,
        PersistentHeaderShowOnScreenConfiguration? showOnScreenConfiguration = null)
        : base(
            vsync: vsync,
            snapConfiguration: snapConfiguration,
            stretchConfiguration: stretchConfiguration,
            showOnScreenConfiguration: showOnScreenConfiguration)
    {
        _mixin = new RenderSliverPersistentHeaderForWidgetsMixin(this);
    }

    SliverPersistentHeaderElement? IRenderSliverPersistentHeaderForWidgets.Element
    {
        get => _mixin.Element;
        set => _mixin.Element = value;
    }

    public override double MinExtent => _mixin.MinExtent;

    public override double MaxExtent => _mixin.MaxExtent;

    protected override void UpdateChild(double shrinkOffset, bool overlapsContent) =>
        _mixin.UpdateChild(shrinkOffset, overlapsContent);

    public void TriggerRebuild() => _mixin.TriggerRebuild();
}
