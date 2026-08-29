using System.Runtime.CompilerServices;
using Avalonia;
using Plumix.Gestures;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/tap_region.dart

public sealed class RenderTapRegionSurface : RenderProxyBoxWithHitTestBehavior
{
    private readonly ConditionalWeakTable<HitTestEntry, BoxHitTestResult> _cachedResults = new();
    private readonly HashSet<RenderTapRegion> _registeredRegions = [];
    private readonly Dictionary<object, HashSet<RenderTapRegion>> _groupIdToRegions = [];
    private readonly TapRegionRegistry _registry;

    public RenderTapRegionSurface()
    {
        _registry = new SurfaceRegistry(this);
    }

    public TapRegionRegistry Registry => _registry;

    public int RegisteredRegionCount => _registeredRegions.Count;

    protected override void OnAttach()
    {
        base.OnAttach();
        Owner?.SemanticsOwner?.AddSemanticsActionListener(HandleSemanticsAction);
    }

    protected override void OnDetach()
    {
        Owner?.SemanticsOwner?.RemoveSemanticsActionListener(HandleSemanticsAction);
        base.OnDetach();
    }

    /// <summary>
    /// Reports taps that arrive over the accessibility channel instead of the pointer channel.
    /// </summary>
    /// <remarks>
    /// Flutter's private <c>RenderTapRegionSurface._handleSemanticsAction</c>. Tap regions are
    /// tracked as render objects, not semantics nodes, so the only way to learn which region a
    /// semantics tap landed in is to hit-test the render tree at the acting node's centre.
    /// Flutter's <c>consumeOutsideTaps</c> cannot stop a semantics action from propagating, and
    /// neither can this.
    /// </remarks>
    private void HandleSemanticsAction(SemanticsActionEvent actionEvent)
    {
        if (actionEvent.Type != SemanticsActions.Tap && actionEvent.Type != SemanticsActions.LongPress)
        {
            return;
        }

        if (_registeredRegions.Count == 0 || Owner is not { } owner)
        {
            return;
        }

        if (owner.SemanticsOwner?.GetRectOfSemanticsNode(actionEvent.NodeId) is not { } globalRect)
        {
            return;
        }

        Point globalCenter = globalRect.Center;
        Point localPosition = GlobalToLocal(globalCenter);

        var hitResult = new BoxHitTestResult();
        if (!HitTest(hitResult, localPosition))
        {
            return;
        }

        (HashSet<RenderTapRegion> inside, RenderTapRegion[] outside) = ClassifyRegions(hitResult);

        var syntheticEvent = new PointerDownEvent(
            pointer: 0,
            kind: PointerDeviceKind.Touch,
            position: globalCenter,
            buttons: PointerButtons.Primary,
            timestampUtc: DateTime.UtcNow);

        foreach (RenderTapRegion region in outside)
        {
            region.OnTapOutside?.Invoke(syntheticEvent);
        }

        foreach (RenderTapRegion region in inside)
        {
            region.OnTapInside?.Invoke(syntheticEvent);
        }
    }

    /// <summary>
    /// Splits the registered regions into the ones the hit path reached and the ones it did not.
    /// </summary>
    /// <remarks>
    /// Flutter's private <c>RenderTapRegionSurface._classifyRegions</c>. Grouped regions count as one
    /// unit: hitting any member puts the whole group inside. The outside list is materialized so a
    /// callback that unregisters a region mid-iteration cannot disturb the walk.
    /// </remarks>
    private (HashSet<RenderTapRegion> Inside, RenderTapRegion[] Outside) ClassifyRegions(
        BoxHitTestResult result)
    {
        var hitRegions = result.Path
            .Select(hitEntry => hitEntry.Target)
            .OfType<RenderTapRegion>()
            .Where(_registeredRegions.Contains)
            .ToHashSet();
        var insideRegions = new HashSet<RenderTapRegion>();
        foreach (RenderTapRegion region in hitRegions)
        {
            if (region.GroupId is null)
            {
                insideRegions.Add(region);
                continue;
            }

            if (_groupIdToRegions.TryGetValue(region.GroupId, out var groupedRegions))
            {
                insideRegions.UnionWith(groupedRegions);
            }
        }

        RenderTapRegion[] outsideRegions = _registeredRegions
            .Where(region => !insideRegions.Contains(region))
            .ToArray();
        return (insideRegions, outsideRegions);
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        if (!HasSize
            || position.X < 0.0
            || position.Y < 0.0
            || position.X > Size.Width
            || position.Y > Size.Height)
        {
            return false;
        }

        bool hitTarget = HitTestChildren(result, position) || HitTestSelf(position);
        if (!hitTarget)
        {
            return false;
        }

        var entry = new BoxHitTestEntry(this, position);
        _cachedResults.Add(entry, result);
        result.Add(entry);
        return true;
    }

    public override void HandleEvent(PointerEvent @event, HitTestEntry entry)
    {
        DebugHandleEvent(@event, entry);
        if (@event is not PointerDownEvent && @event is not PointerUpEvent)
        {
            return;
        }

        if (_registeredRegions.Count == 0 || !_cachedResults.TryGetValue(entry, out var result))
        {
            return;
        }

        (HashSet<RenderTapRegion> insideRegions, RenderTapRegion[] outsideRegions) = ClassifyRegions(result);
        bool consumeOutsideTaps = false;
        foreach (RenderTapRegion region in outsideRegions)
        {
            if (@event is PointerDownEvent downEvent)
            {
                region.OnTapOutside?.Invoke(downEvent);
            }
            else if (@event is PointerUpEvent upEvent)
            {
                region.OnTapUpOutside?.Invoke(upEvent);
            }

            consumeOutsideTaps |= region.ConsumeOutsideTaps;
        }

        foreach (RenderTapRegion region in insideRegions)
        {
            if (@event is PointerDownEvent downEvent)
            {
                region.OnTapInside?.Invoke(downEvent);
            }
            else if (@event is PointerUpEvent upEvent)
            {
                region.OnTapUpInside?.Invoke(upEvent);
            }
        }

        if (consumeOutsideTaps && @event is PointerDownEvent consumedDownEvent)
        {
            GestureBinding.Instance.GestureArena
                .Add(consumedDownEvent.Pointer, new DummyTapRecognizer())
                .Resolve(GestureDisposition.Accepted);
        }

        if (@event is PointerUpEvent)
        {
            _cachedResults.Remove(entry);
        }
    }

    private void Register(RenderTapRegion region)
    {
        if (!_registeredRegions.Add(region) || region.GroupId is null)
        {
            return;
        }

        if (!_groupIdToRegions.TryGetValue(region.GroupId, out var regions))
        {
            regions = [];
            _groupIdToRegions[region.GroupId] = regions;
        }

        regions.Add(region);
    }

    private void Unregister(RenderTapRegion region)
    {
        if (!_registeredRegions.Remove(region) || region.GroupId is null)
        {
            return;
        }

        if (!_groupIdToRegions.TryGetValue(region.GroupId, out var regions))
        {
            return;
        }

        regions.Remove(region);
        if (regions.Count == 0)
        {
            _groupIdToRegions.Remove(region.GroupId);
        }
    }

    private sealed class SurfaceRegistry(RenderTapRegionSurface surface) : TapRegionRegistry
    {
        public override void RegisterTapRegion(RenderTapRegion region)
        {
            surface.Register(region);
        }

        public override void UnregisterTapRegion(RenderTapRegion region)
        {
            surface.Unregister(region);
        }
    }

    private sealed class DummyTapRecognizer : IGestureArenaMember
    {
        public void AcceptGesture(int pointer)
        {
        }

        public void RejectGesture(int pointer)
        {
        }
    }
}

public sealed class RenderTapRegion : RenderProxyBoxWithHitTestBehavior
{
    private TapRegionRegistry? _registry;
    private bool _enabled;
    private bool _consumeOutsideTaps;
    private object? _groupId;
    private bool _isRegistered;

    public RenderTapRegion(
        TapRegionRegistry? registry = null,
        bool enabled = true,
        bool consumeOutsideTaps = false,
        HitTestBehavior behavior = HitTestBehavior.DeferToChild,
        Action<PointerDownEvent>? onTapOutside = null,
        Action<PointerDownEvent>? onTapInside = null,
        Action<PointerUpEvent>? onTapUpOutside = null,
        Action<PointerUpEvent>? onTapUpInside = null,
        object? groupId = null,
        string? debugLabel = null) : base(behavior)
    {
        _registry = registry;
        _enabled = enabled;
        _consumeOutsideTaps = consumeOutsideTaps;
        _groupId = groupId;
        OnTapOutside = onTapOutside;
        OnTapInside = onTapInside;
        OnTapUpOutside = onTapUpOutside;
        OnTapUpInside = onTapUpInside;
        DebugLabel = debugLabel;
    }

    public Action<PointerDownEvent>? OnTapOutside { get; set; }

    public Action<PointerDownEvent>? OnTapInside { get; set; }

    public Action<PointerUpEvent>? OnTapUpOutside { get; set; }

    public Action<PointerUpEvent>? OnTapUpInside { get; set; }

    public string? DebugLabel { get; set; }

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value)
            {
                return;
            }

            _enabled = value;
            MarkNeedsLayout();
        }
    }

    public bool ConsumeOutsideTaps
    {
        get => _consumeOutsideTaps;
        set
        {
            if (_consumeOutsideTaps == value)
            {
                return;
            }

            _consumeOutsideTaps = value;
            MarkNeedsLayout();
        }
    }

    public object? GroupId
    {
        get => _groupId;
        set
        {
            if (Equals(_groupId, value))
            {
                return;
            }

            Unregister();
            _groupId = value;
            MarkNeedsLayout();
        }
    }

    public TapRegionRegistry? Registry
    {
        get => _registry;
        set
        {
            if (ReferenceEquals(_registry, value))
            {
                return;
            }

            Unregister();
            _registry = value;
            MarkNeedsLayout();
        }
    }

    public override void Layout(BoxConstraints constraints, bool parentUsesSize = false)
    {
        base.Layout(constraints, parentUsesSize);
        Unregister();
        if (_enabled && _registry is not null)
        {
            _registry.RegisterTapRegion(this);
            _isRegistered = true;
        }
    }

    protected override void OnDetach()
    {
        Unregister();
        base.OnDetach();
    }

    private void Unregister()
    {
        if (!_isRegistered)
        {
            return;
        }

        _registry!.UnregisterTapRegion(this);
        _isRegistered = false;
    }
}
