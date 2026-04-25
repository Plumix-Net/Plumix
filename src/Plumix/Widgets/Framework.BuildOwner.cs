// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/framework.dart (approximate)

namespace Plumix.Widgets;

/// <summary>
/// Build owner and scheduler.
/// </summary>
public sealed class BuildOwner
{
    private readonly SortedSet<Element> _dirty = new(ElementDepthComparer.Instance);
    private readonly HashSet<Element> _tracked = [];
    private readonly HashSet<Element> _inactive = [];
    private readonly Dictionary<GlobalKey, Element> _globalKeyRegistry = [];

    private bool _scheduled;
    internal Action? OnBuildScheduled { get; set; }

    public void RegisterElement(Element element)
    {
        _tracked.Add(element);
    }

    public void UnregisterElement(Element element)
    {
        _tracked.Remove(element);
        _inactive.Remove(element);
        UnscheduleBuild(element);
    }

    internal void RegisterGlobalKey(GlobalKey key, Element element)
    {
        if (_globalKeyRegistry.TryGetValue(key, out var existing) && !ReferenceEquals(existing, element))
        {
            if (!existing.IsInactive)
            {
                throw new InvalidOperationException($"Duplicate GlobalKey detected: {key}.");
            }
        }

        _globalKeyRegistry[key] = element;
    }

    internal void UnregisterGlobalKey(GlobalKey key, Element element)
    {
        if (_globalKeyRegistry.TryGetValue(key, out var existing) && ReferenceEquals(existing, element))
        {
            _globalKeyRegistry.Remove(key);
        }
    }

    internal Element? RetakeInactiveElement(Element newParent, Widget widget)
    {
        if (widget.Key is not GlobalKey key)
        {
            return null;
        }

        if (!_globalKeyRegistry.TryGetValue(key, out var element))
        {
            return null;
        }

        if (!Widget.CanUpdate(element.Widget, widget))
        {
            return null;
        }

        if (element.Parent != null)
        {
            if (ReferenceEquals(element.Parent, newParent))
            {
                throw new InvalidOperationException($"Duplicate GlobalKey detected in a single parent: {key}.");
            }

            element.Parent.DeactivateChild(element);
        }

        if (!element.IsInactive)
        {
            return null;
        }

        _inactive.Remove(element);
        return element;
    }

    internal void TrackInactive(Element element)
    {
        _inactive.Add(element);
    }

    internal void Deactivate(Element element)
    {
        element.DeactivateRecursively();
    }

    public void ScheduleBuild(Element element)
    {
        if (!element.IsActive)
        {
            return;
        }

        _dirty.Add(element);

        if (_scheduled)
        {
            return;
        }

        _scheduled = true;
        OnBuildScheduled?.Invoke();
    }

    internal void UnscheduleBuild(Element element)
    {
        _dirty.Remove(element);
    }

    public void MarkSubtreeNeedsBuild(Element root)
    {
        foreach (var element in _tracked.Where(x => x.IsActive && IsDescendantOf(x, root)))
        {
            ScheduleBuild(element);
        }
    }

    private static bool IsDescendantOf(Element node, Element root)
    {
        for (var parent = node.Parent; parent != null; parent = parent.Parent)
        {
            if (ReferenceEquals(parent, root))
            {
                return true;
            }
        }

        return false;
    }

    internal void BuildScope()
    {
        _scheduled = false;

        while (_dirty.Count > 0)
        {
            var element = _dirty.Max!;
            _dirty.Remove(element);

            if (!element.IsActive || element.Owner != this)
            {
                continue;
            }

            element.Rebuild();
        }

        FinalizeInactiveElements();
    }

    internal void FlushBuild()
    {
        BuildScope();
    }

    private void FinalizeInactiveElements()
    {
        if (_inactive.Count == 0)
        {
            return;
        }

        var toUnmount = _inactive.ToArray();
        _inactive.Clear();

        foreach (var element in toUnmount)
        {
            if (element.IsInactive)
            {
                element.Unmount();
            }
        }
    }

    private sealed class ElementDepthComparer : IComparer<Element>
    {
        public static readonly ElementDepthComparer Instance = new();

        public int Compare(Element? x, Element? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            var depthCompare = x.Depth.CompareTo(y.Depth);
            if (depthCompare != 0)
            {
                return depthCompare;
            }

            return x.SequenceId.CompareTo(y.SequenceId);
        }
    }
}
