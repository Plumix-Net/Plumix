using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/scroll_configuration.dart

public enum AndroidOverscrollIndicator
{
    Stretch,
    Glow,
}

public enum ScrollViewKeyboardDismissBehavior
{
    Manual,
    OnDrag,
}

public sealed record ScrollableDetails(
    AxisDirection Direction,
    ScrollController? Controller = null,
    ScrollPhysics? Physics = null,
    Clip? DecorationClipBehavior = null)
{
    [Obsolete("Use DecorationClipBehavior; this clip applies to scroll decorators, not the viewport.")]
    public Clip? ClipBehavior => DecorationClipBehavior;

    public static ScrollableDetails Vertical(
        bool reverse = false,
        ScrollController? controller = null,
        ScrollPhysics? physics = null,
        Clip? decorationClipBehavior = null)
    {
        return new ScrollableDetails(
            reverse ? AxisDirection.Up : AxisDirection.Down,
            controller,
            physics,
            decorationClipBehavior);
    }

    public static ScrollableDetails Horizontal(
        bool reverse = false,
        ScrollController? controller = null,
        ScrollPhysics? physics = null,
        Clip? decorationClipBehavior = null)
    {
        return new ScrollableDetails(
            reverse ? AxisDirection.Left : AxisDirection.Right,
            controller,
            physics,
            decorationClipBehavior);
    }

    public ScrollableDetails CopyWith(
        AxisDirection? direction = null,
        ScrollController? controller = null,
        ScrollPhysics? physics = null,
        Clip? decorationClipBehavior = null)
    {
        return this with
        {
            Direction = direction ?? Direction,
            Controller = controller ?? Controller,
            Physics = physics ?? Physics,
            DecorationClipBehavior = decorationClipBehavior ?? DecorationClipBehavior,
        };
    }
}

public class ScrollBehavior
{
    private static readonly ScrollPhysics BouncingPhysics =
        new BouncingScrollPhysics(parent: new RangeMaintainingScrollPhysics());

    private static readonly ScrollPhysics BouncingDesktopPhysics =
        new BouncingScrollPhysics(
            decelerationRate: ScrollDecelerationRate.Fast,
            parent: new RangeMaintainingScrollPhysics());

    private static readonly ScrollPhysics ClampingPhysics =
        new ClampingScrollPhysics(parent: new RangeMaintainingScrollPhysics());

    private static readonly IReadOnlySet<PointerDeviceKind> DefaultDragDevices =
        new HashSet<PointerDeviceKind>
        {
            PointerDeviceKind.Touch,
            PointerDeviceKind.Stylus,
            PointerDeviceKind.InvertedStylus,
            PointerDeviceKind.Trackpad,
            PointerDeviceKind.Unknown,
        };

    private static readonly IReadOnlySet<LogicalKeyboardKey> DefaultPointerAxisModifiers =
        new HashSet<LogicalKeyboardKey>
        {
            LogicalKeyboardKey.ShiftLeft,
            LogicalKeyboardKey.ShiftRight,
        };

    public virtual TargetPlatform GetPlatform(BuildContext context) => PlatformDefaults.TargetPlatform;

    public virtual IReadOnlySet<PointerDeviceKind> DragDevices => DefaultDragDevices;

    public virtual IReadOnlySet<LogicalKeyboardKey> PointerAxisModifiers => DefaultPointerAxisModifiers;

    public virtual MultitouchDragStrategy GetMultitouchDragStrategy(BuildContext context)
    {
        return GetPlatform(context) is TargetPlatform.IOS or TargetPlatform.MacOS
            ? MultitouchDragStrategy.AverageBoundaryPointers
            : MultitouchDragStrategy.LatestPointer;
    }

    public virtual Widget BuildScrollbar(BuildContext context, Widget child, ScrollableDetails details)
    {
        return GetPlatform(context) is TargetPlatform.Linux or TargetPlatform.MacOS or TargetPlatform.Windows
            ? new RawScrollbar(child: child, controller: details.Controller)
            : child;
    }

    public virtual Widget BuildOverscrollIndicator(BuildContext context, Widget child, ScrollableDetails details)
    {
        return child;
    }

    public virtual GestureVelocityTrackerBuilder VelocityTrackerBuilder(BuildContext context)
    {
        return GetPlatform(context) switch
        {
            TargetPlatform.IOS => @event => new IOSScrollViewFlingVelocityTracker(@event.Kind),
            TargetPlatform.MacOS => @event => new MacOSScrollViewFlingVelocityTracker(@event.Kind),
            _ => @event => new VelocityTracker(@event.Kind),
        };
    }

    public virtual ScrollPhysics GetScrollPhysics(BuildContext context)
    {
        return GetPlatform(context) switch
        {
            TargetPlatform.IOS => BouncingPhysics,
            TargetPlatform.MacOS => BouncingDesktopPhysics,
            _ => ClampingPhysics,
        };
    }

    public virtual ScrollViewKeyboardDismissBehavior GetKeyboardDismissBehavior(BuildContext context)
    {
        return ScrollViewKeyboardDismissBehavior.Manual;
    }

    public virtual bool ShouldNotify(ScrollBehavior oldDelegate) => false;

    public virtual ScrollBehavior CopyWith(
        bool? scrollbars = null,
        bool? overscroll = null,
        IReadOnlySet<PointerDeviceKind>? dragDevices = null,
        MultitouchDragStrategy? multitouchDragStrategy = null,
        IReadOnlySet<LogicalKeyboardKey>? pointerAxisModifiers = null,
        ScrollPhysics? physics = null,
        TargetPlatform? platform = null,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior = null)
    {
        return new WrappedScrollBehavior(
            this,
            scrollbars ?? true,
            overscroll ?? true,
            dragDevices,
            multitouchDragStrategy,
            pointerAxisModifiers,
            physics,
            platform,
            keyboardDismissBehavior);
    }
}

internal sealed class WrappedScrollBehavior : ScrollBehavior
{
    public WrappedScrollBehavior(
        ScrollBehavior @delegate,
        bool scrollbars,
        bool overscroll,
        IReadOnlySet<PointerDeviceKind>? dragDevices,
        MultitouchDragStrategy? multitouchDragStrategy,
        IReadOnlySet<LogicalKeyboardKey>? pointerAxisModifiers,
        ScrollPhysics? physics,
        TargetPlatform? platform,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior)
    {
        Delegate = @delegate;
        Scrollbars = scrollbars;
        Overscroll = overscroll;
        CustomDragDevices = dragDevices;
        MultitouchDragStrategy = multitouchDragStrategy;
        CustomPointerAxisModifiers = pointerAxisModifiers;
        Physics = physics;
        Platform = platform;
        KeyboardDismissBehavior = keyboardDismissBehavior;
    }

    public ScrollBehavior Delegate { get; }

    public bool Scrollbars { get; }

    public bool Overscroll { get; }

    public IReadOnlySet<PointerDeviceKind>? CustomDragDevices { get; }

    public MultitouchDragStrategy? MultitouchDragStrategy { get; }

    public IReadOnlySet<LogicalKeyboardKey>? CustomPointerAxisModifiers { get; }

    public ScrollPhysics? Physics { get; }

    public TargetPlatform? Platform { get; }

    public ScrollViewKeyboardDismissBehavior? KeyboardDismissBehavior { get; }

    public override IReadOnlySet<PointerDeviceKind> DragDevices => CustomDragDevices ?? Delegate.DragDevices;

    public override IReadOnlySet<LogicalKeyboardKey> PointerAxisModifiers =>
        CustomPointerAxisModifiers ?? Delegate.PointerAxisModifiers;

    public override TargetPlatform GetPlatform(BuildContext context) => Platform ?? Delegate.GetPlatform(context);

    public override MultitouchDragStrategy GetMultitouchDragStrategy(BuildContext context)
    {
        return MultitouchDragStrategy ?? Delegate.GetMultitouchDragStrategy(context);
    }

    public override Widget BuildScrollbar(BuildContext context, Widget child, ScrollableDetails details)
    {
        return Scrollbars ? Delegate.BuildScrollbar(context, child, details) : child;
    }

    public override Widget BuildOverscrollIndicator(BuildContext context, Widget child, ScrollableDetails details)
    {
        return Overscroll ? Delegate.BuildOverscrollIndicator(context, child, details) : child;
    }

    public override ScrollPhysics GetScrollPhysics(BuildContext context)
    {
        return Physics ?? Delegate.GetScrollPhysics(context);
    }

    public override GestureVelocityTrackerBuilder VelocityTrackerBuilder(BuildContext context)
    {
        return Delegate.VelocityTrackerBuilder(context);
    }

    public override ScrollViewKeyboardDismissBehavior GetKeyboardDismissBehavior(BuildContext context)
    {
        return KeyboardDismissBehavior ?? Delegate.GetKeyboardDismissBehavior(context);
    }

    public override ScrollBehavior CopyWith(
        bool? scrollbars = null,
        bool? overscroll = null,
        IReadOnlySet<PointerDeviceKind>? dragDevices = null,
        MultitouchDragStrategy? multitouchDragStrategy = null,
        IReadOnlySet<LogicalKeyboardKey>? pointerAxisModifiers = null,
        ScrollPhysics? physics = null,
        TargetPlatform? platform = null,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior = null)
    {
        return Delegate.CopyWith(
            scrollbars: scrollbars ?? Scrollbars,
            overscroll: overscroll ?? Overscroll,
            dragDevices: dragDevices ?? DragDevices,
            multitouchDragStrategy: multitouchDragStrategy ?? MultitouchDragStrategy,
            pointerAxisModifiers: pointerAxisModifiers ?? PointerAxisModifiers,
            physics: physics ?? Physics,
            platform: platform ?? Platform,
            keyboardDismissBehavior: keyboardDismissBehavior ?? KeyboardDismissBehavior);
    }

    public override bool ShouldNotify(ScrollBehavior oldDelegate)
    {
        if (oldDelegate is not WrappedScrollBehavior old)
        {
            return true;
        }

        return old.Delegate.GetType() != Delegate.GetType()
               || old.Scrollbars != Scrollbars
               || old.Overscroll != Overscroll
               || !old.DragDevices.SetEquals(DragDevices)
               || old.MultitouchDragStrategy != MultitouchDragStrategy
               || !old.PointerAxisModifiers.SetEquals(PointerAxisModifiers)
               || !ReferenceEquals(old.Physics, Physics)
               || old.Platform != Platform
               || old.KeyboardDismissBehavior != KeyboardDismissBehavior
               || Delegate.ShouldNotify(old.Delegate);
    }
}

public sealed class ScrollConfiguration : InheritedWidget
{
    public ScrollConfiguration(
        ScrollBehavior behavior,
        Widget child,
        Key? key = null) : base(key)
    {
        Behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public ScrollBehavior Behavior { get; }

    public Widget Child { get; }

    public static ScrollBehavior Of(BuildContext context)
    {
        return context.DependOnInherited<ScrollConfiguration>()?.Behavior ?? new ScrollBehavior();
    }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        ScrollBehavior oldBehavior = ((ScrollConfiguration)oldWidget).Behavior;
        return Behavior.GetType() != oldBehavior.GetType()
               || (!ReferenceEquals(Behavior, oldBehavior) && Behavior.ShouldNotify(oldBehavior));
    }
}
