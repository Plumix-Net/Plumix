using Avalonia;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/gestures/binding.dart (approximate)

namespace Plumix.Gestures;

public sealed class GestureBinding
{
    public static GestureBinding Instance { get; } = new();

    private readonly Dictionary<int, HitTestResult> _hitTests = [];
    private readonly Dictionary<int, HitTestResult> _hoverHitTests = [];
    private readonly Dictionary<int, Point> _lastPositions = [];

    public PointerRouter PointerRouter { get; } = new();

    public GestureArenaManager GestureArena { get; } = new();

    public void HandlePointerEvent(RenderView root, PointerEvent @event)
    {
        var eventWithDelta = AttachDelta(@event);
        HitTestResult? hitTestResult = null;

        switch (@event)
        {
            case PointerDownEvent:
            {
                var result = new BoxHitTestResult();
                root.HitTest(result, @event.Position);
                _hitTests[@event.Pointer] = result;
                hitTestResult = result;
                break;
            }
            case PointerMoveEvent or PointerUpEvent or PointerCancelEvent:
            {
                _hitTests.TryGetValue(@event.Pointer, out hitTestResult);
                break;
            }
            case PointerHoverEvent:
            {
                var result = new BoxHitTestResult();
                root.HitTest(result, @event.Position);
                DispatchHoverTransitions((PointerHoverEvent)eventWithDelta, GetHoverHitTest(@event.Pointer), result);
                _hoverHitTests[@event.Pointer] = result;
                hitTestResult = result;
                break;
            }
            case PointerSignalEvent:
            {
                var result = new BoxHitTestResult();
                root.HitTest(result, @event.Position);
                hitTestResult = result;
                break;
            }
        }

        DispatchEvent(eventWithDelta, hitTestResult);

        if (@event is PointerDownEvent)
        {
            GestureArena.Close(@event.Pointer);
        }

        if (@event is PointerUpEvent or PointerCancelEvent)
        {
            GestureArena.Sweep(@event.Pointer);
            _hitTests.Remove(@event.Pointer);
            _lastPositions.Remove(@event.Pointer);
        }

        if (@event is PointerCancelEvent)
        {
            _hoverHitTests.Remove(@event.Pointer);
        }
    }

    public void DispatchEvent(PointerEvent @event, HitTestResult? hitTestResult)
    {
        if (hitTestResult != null)
        {
            foreach (var entry in hitTestResult.Path)
            {
                var transformedEvent = entry.TransformEvent(@event);
                entry.Target.HandleEvent(transformedEvent, entry);
            }
        }

        PointerRouter.Route(@event);
    }

    internal void ResetForTests()
    {
        _hitTests.Clear();
        _hoverHitTests.Clear();
        _lastPositions.Clear();
        PointerRouter.Reset();
        GestureArena.Reset();
    }

    private HitTestResult? GetHoverHitTest(int pointer)
    {
        _hoverHitTests.TryGetValue(pointer, out var result);
        return result;
    }

    private void DispatchHoverTransitions(PointerHoverEvent hoverEvent, HitTestResult? previousResult, HitTestResult currentResult)
    {
        var previousEntries = BuildEntryMap(previousResult);
        var currentEntries = BuildEntryMap(currentResult);

        var exitEvent = new PointerExitEvent(
            pointer: hoverEvent.Pointer,
            kind: hoverEvent.Kind,
            position: hoverEvent.Position,
            buttons: hoverEvent.Buttons,
            timestampUtc: hoverEvent.TimestampUtc);

        foreach (var entry in previousEntries)
        {
            if (currentEntries.ContainsKey(entry.Key))
            {
                continue;
            }

            DispatchTransformedEvent(exitEvent, entry.Value);
        }

        var enterEvent = new PointerEnterEvent(
            pointer: hoverEvent.Pointer,
            kind: hoverEvent.Kind,
            position: hoverEvent.Position,
            buttons: hoverEvent.Buttons,
            timestampUtc: hoverEvent.TimestampUtc);

        foreach (var entry in currentEntries)
        {
            if (previousEntries.ContainsKey(entry.Key))
            {
                continue;
            }

            DispatchTransformedEvent(enterEvent, entry.Value);
        }
    }

    private static Dictionary<RenderObject, HitTestEntry> BuildEntryMap(HitTestResult? result)
    {
        var map = new Dictionary<RenderObject, HitTestEntry>();
        if (result is null)
        {
            return map;
        }

        foreach (var entry in result.Path)
        {
            map[entry.Target] = entry;
        }

        return map;
    }

    private static void DispatchTransformedEvent(PointerEvent @event, HitTestEntry entry)
    {
        var transformedEvent = entry.TransformEvent(@event);
        entry.Target.HandleEvent(transformedEvent, entry);
    }

    private PointerEvent AttachDelta(PointerEvent @event)
    {
        if (@event is PointerSignalEvent)
        {
            return @event.WithDelta(default);
        }

        var pointer = @event.Pointer;
        if (!_lastPositions.TryGetValue(pointer, out var previousPosition))
        {
            _lastPositions[pointer] = @event.Position;
            return @event.WithDelta(default);
        }

        var delta = @event.Position - previousPosition;
        _lastPositions[pointer] = @event.Position;
        return @event.WithDelta(delta);
    }
}
