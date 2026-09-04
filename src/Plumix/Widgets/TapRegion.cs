using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/tap_region.dart

public abstract class TapRegionRegistry
{
    public abstract void RegisterTapRegion(RenderTapRegion region);

    public abstract void UnregisterTapRegion(RenderTapRegion region);

    public static TapRegionRegistry Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException(
                   "TapRegionRegistry.Of() requires an ancestor TapRegionSurface.");
    }

    public static TapRegionRegistry? MaybeOf(BuildContext context)
    {
        return context.FindAncestorRenderObjectOfType<RenderTapRegionSurface>()?.Registry;
    }
}

public sealed class TapRegionSurface : SingleChildRenderObjectWidget
{
    public TapRegionSurface(Widget child, Key? key = null) : base(child, key)
    {
    }

    public override RenderObject CreateRenderObject(BuildContext context) => new RenderTapRegionSurface();
}

public class TapRegion : SingleChildRenderObjectWidget
{
    public TapRegion(
        Widget child,
        bool enabled = true,
        HitTestBehavior behavior = HitTestBehavior.DeferToChild,
        Action<PointerDownEvent>? onTapOutside = null,
        Action<PointerDownEvent>? onTapInside = null,
        Action<PointerUpEvent>? onTapUpOutside = null,
        Action<PointerUpEvent>? onTapUpInside = null,
        object? groupId = null,
        bool consumeOutsideTaps = false,
        string? debugLabel = null,
        Key? key = null) : base(child, key)
    {
        Enabled = enabled;
        Behavior = behavior;
        OnTapOutside = onTapOutside;
        OnTapInside = onTapInside;
        OnTapUpOutside = onTapUpOutside;
        OnTapUpInside = onTapUpInside;
        GroupId = groupId;
        ConsumeOutsideTaps = consumeOutsideTaps;
        DebugLabel = debugLabel;
    }

    public bool Enabled { get; }

    public HitTestBehavior Behavior { get; }

    public Action<PointerDownEvent>? OnTapOutside { get; }

    public Action<PointerDownEvent>? OnTapInside { get; }

    public Action<PointerUpEvent>? OnTapUpOutside { get; }

    public Action<PointerUpEvent>? OnTapUpInside { get; }

    public object? GroupId { get; }

    public bool ConsumeOutsideTaps { get; }

    public string? DebugLabel { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        bool isCurrent = ModalRoute.IsCurrentOf(context) ?? true;
        return new RenderTapRegion(
            registry: MaybeRegistryOf(context),
            enabled: Enabled,
            consumeOutsideTaps: isCurrent && ConsumeOutsideTaps,
            behavior: Behavior,
            onTapOutside: isCurrent ? OnTapOutside : null,
            onTapInside: OnTapInside,
            onTapUpOutside: isCurrent ? OnTapUpOutside : null,
            onTapUpInside: OnTapUpInside,
            groupId: GroupId,
            debugLabel: DebugLabel);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        bool isCurrent = ModalRoute.IsCurrentOf(context) ?? true;
        var region = (RenderTapRegion)renderObject;
        region.Registry = MaybeRegistryOf(context);
        region.Enabled = Enabled;
        region.ConsumeOutsideTaps = isCurrent && ConsumeOutsideTaps;
        region.Behavior = Behavior;
        region.OnTapOutside = isCurrent ? OnTapOutside : null;
        region.OnTapInside = OnTapInside;
        region.OnTapUpOutside = isCurrent ? OnTapUpOutside : null;
        region.OnTapUpInside = OnTapUpInside;
        region.GroupId = GroupId;
        region.DebugLabel = DebugLabel;
    }

    public static TapRegionRegistry Of(BuildContext context)
    {
        return TapRegionRegistry.Of(context);
    }

    public static TapRegionRegistry? MaybeOf(BuildContext context) => TapRegionRegistry.MaybeOf(context);

    private static TapRegionRegistry? MaybeRegistryOf(BuildContext context)
    {
        return TapRegionRegistry.MaybeOf(context);
    }
}

public sealed class TextFieldTapRegion : TapRegion
{
    public TextFieldTapRegion(
        Widget child,
        bool enabled = true,
        Action<PointerDownEvent>? onTapOutside = null,
        Action<PointerDownEvent>? onTapInside = null,
        Action<PointerUpEvent>? onTapUpOutside = null,
        Action<PointerUpEvent>? onTapUpInside = null,
        bool consumeOutsideTaps = false,
        string? debugLabel = null,
        object? groupId = null,
        Key? key = null) : base(
        child: child,
        enabled: enabled,
        onTapOutside: onTapOutside,
        onTapInside: onTapInside,
        onTapUpOutside: onTapUpOutside,
        onTapUpInside: onTapUpInside,
        groupId: groupId ?? typeof(EditableText),
        consumeOutsideTaps: consumeOutsideTaps,
        debugLabel: debugLabel,
        key: key)
    {
    }
}
