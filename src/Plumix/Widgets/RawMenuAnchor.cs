using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/raw_menu_anchor.dart (approximate)

public sealed record RawMenuOverlayInfo(
    Rect AnchorRect,
    Size OverlaySize,
    object TapRegionGroupId,
    Vector? Position = null);

public delegate Widget RawMenuOverlayBuilder(
    BuildContext context,
    RawMenuOverlayInfo info);

public sealed class RawMenuAnchorController
{
    private readonly OverlayPortalController _overlayController = new("RawMenuAnchor");

    public bool IsOpen => _overlayController.IsShowing;

    public Vector? Position { get; private set; }

    public void Open(Vector? position = null)
    {
        Position = position;
        _overlayController.Show();
    }

    public void Close()
    {
        _overlayController.Hide();
        Position = null;
    }

    internal OverlayPortalController OverlayController => _overlayController;
}

public sealed class RawMenuAnchor : StatelessWidget
{
    private readonly object _fallbackTapRegionGroupId = new();

    public RawMenuAnchor(
        RawMenuAnchorController controller,
        RawMenuOverlayBuilder overlayBuilder,
        Widget? child = null,
        Action? onTapOutside = null,
        bool consumeOutsideTaps = false,
        bool useRootOverlay = false,
        object? tapRegionGroupId = null,
        Key? key = null) : base(key)
    {
        Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        OverlayBuilder = overlayBuilder ?? throw new ArgumentNullException(nameof(overlayBuilder));
        Child = child;
        OnTapOutside = onTapOutside;
        ConsumeOutsideTaps = consumeOutsideTaps;
        UseRootOverlay = useRootOverlay;
        TapRegionGroupId = tapRegionGroupId;
    }

    public RawMenuAnchorController Controller { get; }

    public RawMenuOverlayBuilder OverlayBuilder { get; }

    public Widget? Child { get; }

    public Action? OnTapOutside { get; }

    public bool ConsumeOutsideTaps { get; }

    public bool UseRootOverlay { get; }

    public object? TapRegionGroupId { get; }

    public override Widget Build(BuildContext context)
    {
        object groupId = TapRegionGroupId
                         ?? RawMenuTapRegionScope.MaybeOf(context)?.GroupId
                         ?? _fallbackTapRegionGroupId;
        bool useRootOverlay = RawMenuTapRegionScope.MaybeOf(context)?.UseRootOverlay
                              ?? UseRootOverlay;
        Widget anchor = new TapRegion(
            child: Child ?? new SizedBox(),
            groupId: groupId,
            consumeOutsideTaps: Controller.IsOpen && ConsumeOutsideTaps,
            onTapOutside: _ => OnTapOutside?.Invoke(),
            debugLabel: "RawMenuAnchor anchor");
        Widget portal = OverlayPortal.WithLayoutBuilder(
            controller: Controller.OverlayController,
            overlayLocation: useRootOverlay
                ? OverlayChildLocation.RootOverlay
                : OverlayChildLocation.NearestOverlay,
            child: anchor,
            overlayChildBuilder: (overlayContext, layoutInfo) => BuildOverlay(
                overlayContext,
                layoutInfo,
                groupId));
        return new RawMenuTapRegionScope(groupId, portal, useRootOverlay);
    }

    private Widget BuildOverlay(
        BuildContext context,
        OverlayChildLayoutInfo layoutInfo,
        object groupId)
    {
        Rect anchorRect = RenderObject.TransformRect(
            layoutInfo.ChildPaintTransform,
            new Rect(default, layoutInfo.ChildSize));
        return OverlayBuilder(
            context,
            new RawMenuOverlayInfo(
                anchorRect,
                layoutInfo.OverlaySize,
                groupId,
                Controller.Position));
    }
}

public sealed class RawMenuAnchorGroup : InheritedWidget
{
    private readonly object _tapRegionGroupId = new();

    public RawMenuAnchorGroup(Widget child, Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public Widget Child { get; }

    public object TapRegionGroupId => _tapRegionGroupId;

    public override Widget Build(BuildContext context) => new RawMenuTapRegionScope(_tapRegionGroupId, Child);

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) => false;

    public static RawMenuAnchorGroup? MaybeOf(BuildContext context) =>
        context.DependOnInherited<RawMenuAnchorGroup>();
}

internal sealed class RawMenuTapRegionScope : InheritedWidget
{
    public RawMenuTapRegionScope(
        object groupId,
        Widget child,
        bool? useRootOverlay = null) : base()
    {
        GroupId = groupId;
        Child = child;
        UseRootOverlay = useRootOverlay;
    }

    public object GroupId { get; }

    public Widget Child { get; }

    public bool? UseRootOverlay { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !ReferenceEquals(GroupId, ((RawMenuTapRegionScope)oldWidget).GroupId)
        || UseRootOverlay != ((RawMenuTapRegionScope)oldWidget).UseRootOverlay;

    public static RawMenuTapRegionScope? MaybeOf(BuildContext context) =>
        context.DependOnInherited<RawMenuTapRegionScope>();
}
