using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/single_child_scroll_view.dart (adapted)

namespace Plumix.Widgets;

/// <summary>
/// A box in which a single widget can be scrolled.
/// </summary>
/// <remarks>
/// Adapted: the widget composes the <see cref="Scrollable"/>/<see cref="SingleChildViewport"/> pair
/// the way Flutter does, but resolves its primary controller through
/// <see cref="PrimaryScrollController.ShouldInherit"/> at build time rather than fixing
/// <c>primary</c> in the constructor. See `docs/ai/DIVERGENCES.md`.
/// </remarks>
public sealed class SingleChildScrollView : StatelessWidget
{
    public SingleChildScrollView(
        Widget child,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollController? controller = null,
        bool? primary = null,
        ScrollPhysics? physics = null,
        ScrollBehavior? scrollBehavior = null,
        Thickness? padding = null,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        string? restorationId = null,
        Clip clipBehavior = Clip.HardEdge,
        HitTestBehavior hitTestBehavior = HitTestBehavior.Opaque,
        Key? key = null) : base(key)
    {
        if (primary == true && controller != null)
        {
            throw new ArgumentException("Primary scroll views cannot be given an explicit controller.");
        }

        Child = child;
        ScrollDirection = scrollDirection;
        Reverse = reverse;
        Controller = controller;
        Primary = primary;
        Physics = physics;
        ScrollBehavior = scrollBehavior;
        Padding = padding;
        KeyboardDismissBehavior = keyboardDismissBehavior;
        DragStartBehavior = dragStartBehavior;
        RestorationId = restorationId;
        ClipBehavior = clipBehavior;
        HitTestBehavior = hitTestBehavior;
    }

    public Widget Child { get; }

    public Axis ScrollDirection { get; }

    public bool Reverse { get; }

    public ScrollController? Controller { get; }

    public bool? Primary { get; }

    public ScrollPhysics? Physics { get; }

    public ScrollBehavior? ScrollBehavior { get; }

    public Thickness? Padding { get; }

    public ScrollViewKeyboardDismissBehavior? KeyboardDismissBehavior { get; }

    public DragStartBehavior DragStartBehavior { get; }

    public string? RestorationId { get; }

    public Clip ClipBehavior { get; }

    public HitTestBehavior HitTestBehavior { get; }

    public override Widget Build(BuildContext context)
    {
        AxisDirection axisDirection = ScrollDirectionUtils.GetAxisDirectionFromAxisReverseAndDirectionality(
            context,
            ScrollDirection,
            Reverse);
        Widget contents = Child;
        if (Padding is { } padding)
        {
            contents = new Padding(padding, contents);
        }

        bool usePrimary = Primary
                          ?? (Controller is null
                              && PrimaryScrollController.ShouldInherit(context, ScrollDirection));
        ScrollController? scrollController = usePrimary
            ? PrimaryScrollController.MaybeOf(context)
            : Controller;

        Widget scrollable = new Scrollable(
            dragStartBehavior: DragStartBehavior,
            axisDirection: axisDirection,
            controller: scrollController,
            physics: Physics,
            scrollBehavior: ScrollBehavior,
            restorationId: RestorationId,
            clipBehavior: ClipBehavior,
            hitTestBehavior: HitTestBehavior,
            viewportBuilder: (viewportContext, offset) => new SingleChildViewport(
                child: contents,
                axisDirection: axisDirection,
                offset: offset));

        ScrollViewKeyboardDismissBehavior effectiveKeyboardDismissBehavior =
            KeyboardDismissBehavior
            ?? ScrollBehavior?.GetKeyboardDismissBehavior(context)
            ?? ScrollConfiguration.Of(context).GetKeyboardDismissBehavior(context);
        if (effectiveKeyboardDismissBehavior == ScrollViewKeyboardDismissBehavior.OnDrag)
        {
            scrollable = new NotificationListener<ScrollUpdateNotification>(
                onNotification: notification =>
                {
                    FocusScopeNode currentScope = FocusScope.Of(context);
                    if (notification.DragDetails is not null
                        && !currentScope.HasPrimaryFocus
                        && currentScope.HasFocus)
                    {
                        FocusManager.Instance.PrimaryFocus?.Unfocus();
                    }

                    return false;
                },
                child: scrollable);
        }

        // Further descendant scroll views must not inherit the same PrimaryScrollController.
        return usePrimary && scrollController is not null
            ? PrimaryScrollController.None(scrollable)
            : scrollable;
    }
}

internal sealed class SingleChildViewport : SingleChildRenderObjectWidget
{
    public SingleChildViewport(
        Widget child,
        AxisDirection axisDirection,
        ViewportOffset offset) : base(child)
    {
        AxisDirection = axisDirection;
        Offset = offset;
    }

    public AxisDirection AxisDirection { get; }

    public ViewportOffset Offset { get; }

    public override RenderObject CreateRenderObject(BuildContext context) => new RenderSingleChildViewport(
        axisDirection: AxisDirection,
        offset: Offset);

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var viewport = (RenderSingleChildViewport)renderObject;
        viewport.AxisDirection = AxisDirection;
        viewport.Offset = Offset;
    }
}

