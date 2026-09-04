// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/framework.dart (approximate)

namespace Plumix.Widgets;

/// <summary>
/// Build owner and scheduler.
/// </summary>
public sealed class BuildOwner
{
    // Flutter parity: a list plus a membership set, not a depth-ordered set. An element's depth changes when
    // it is reparented, which would corrupt an ordered container it is already sitting in.
    private readonly List<Element> _dirty = [];
    private readonly HashSet<Element> _dirtyMembership = [];
    private bool _dirtyNeedsResorting;
    private readonly HashSet<Element> _tracked = [];
    private readonly HashSet<Element> _inactive = [];
    private readonly Dictionary<GlobalKey, Element> _globalKeyRegistry = [];

    private bool _scheduled;
    private bool _building;
    public Action? OnBuildScheduled { get; set; }

    /// <summary>Whether this owner is currently executing a build-scope callback or flushing dirty elements.</summary>
    /// <remarks>Flutter's <c>BuildOwner.debugBuilding</c>, which Plumix keeps outside the debug-only surface.</remarks>
    public bool IsBuilding => _building;

    /// <summary>The number of <see cref="GlobalKey"/> instances currently registered with this owner.</summary>
    /// <remarks>Flutter's <c>BuildOwner.globalKeyCount</c>.</remarks>
    public int GlobalKeyCount => _globalKeyRegistry.Count;

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
        key.AttachElement(element);
    }

    internal void UnregisterGlobalKey(GlobalKey key, Element element)
    {
        if (_globalKeyRegistry.TryGetValue(key, out var existing) && ReferenceEquals(existing, element))
        {
            _globalKeyRegistry.Remove(key);
            key.DetachElement(element);
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

        if (_dirtyMembership.Add(element))
        {
            _dirty.Add(element);
        }
        else
        {
            // Already queued. It may have been rebuilt already in the current scope, so ask the scope to
            // re-sort and rewind onto it (Flutter's `_dirtyElementsNeedsResorting`).
            _dirtyNeedsResorting = true;
        }

        if (_scheduled || _building)
        {
            return;
        }

        _scheduled = true;
        OnBuildScheduled?.Invoke();
    }

    internal void UnscheduleBuild(Element element)
    {
        // The list entry is left behind as a tombstone: the build scope skips inactive or clean elements,
        // and removing by value from an ordered container whose key (depth) may have changed is unsafe.
        _dirtyMembership.Remove(element);
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

    /// Cause the entire subtree rooted at the given [Element] to be entirely
    /// rebuilt. This is used by development tools when the application code has
    /// changed and is being hot-reloaded, to cause the widget tree to pick up
    /// any changed implementations.
    ///
    /// This is expensive and should not be called except during development.
    public void Reassemble(Element root)
    {
        root.Reassemble();
    }

    internal void BuildScope()
    {
        if (_building)
        {
            throw new InvalidOperationException("BuildOwner.buildScope must not be re-entered.");
        }

        _scheduled = false;

        using IDisposable buildPhase = Scheduler.BuildScope();

        _building = true;
        try
        {
            FlushDirtyElements();
            FinalizeInactiveElements();
        }
        finally
        {
            _building = false;
        }
    }

    /// <summary>
    /// Establishes <paramref name="context"/> as the target of a build-scope callback, then flushes
    /// elements dirtied by that callback.
    /// </summary>
    /// <remarks>Flutter's <c>BuildOwner.buildScope(Element, [VoidCallback])</c>.</remarks>
    public void BuildScope(Element context, Action? callback = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!ReferenceEquals(context.Owner, this))
        {
            throw new InvalidOperationException("The build-scope context belongs to a different BuildOwner.");
        }

        if (callback == null && _dirty.Count == 0)
        {
            return;
        }

        if (_building)
        {
            throw new InvalidOperationException("BuildOwner.buildScope must not be re-entered.");
        }

        _scheduled = false;
        using IDisposable buildPhase = Scheduler.BuildScope();
        _building = true;
        try
        {
            callback?.Invoke();
            FlushDirtyElements();
        }
        finally
        {
            _building = false;
        }
    }

    /// <summary>
    /// Runs <paramref name="callback"/> as a build scope rooted at <paramref name="context"/> from
    /// inside a layout pass, flushing only the elements the callback itself dirtied.
    /// </summary>
    /// <remarks>
    /// Dart's <c>BuildOwner.buildScope</c> flushes the whole dirty list, which is safe there because
    /// a frame builds before it lays out and nothing dirties an element in between. Plumix drains the
    /// scheduler microtask queue at the pump boundary, so `FocusManager.MarkNeedsUpdate` and friends
    /// can leave elements dirty when layout starts. Rebuilding one of those mid-layout re-dirties a
    /// render subtree whose ancestor is already being laid out; that ancestor then clears its own
    /// flag and the subtree stays dirty under a clean parent. Deferring the pre-existing entries to
    /// the next build keeps the lazy child mutation itself faithful to Dart.
    /// </remarks>
    internal void BuildScopeDuringLayout(Element context, Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        Element[] deferred = [.. _dirty];
        _dirty.Clear();
        _dirtyMembership.Clear();
        try
        {
            BuildScope(context, callback);
        }
        finally
        {
            foreach (Element element in deferred)
            {
                if (element.IsActive && ReferenceEquals(element.Owner, this) && element.Dirty
                    && _dirtyMembership.Add(element))
                {
                    _dirty.Add(element);
                    _dirtyNeedsResorting = true;
                }
            }
        }
    }

    private void FlushDirtyElements()
    {
        // Flutter parity (`BuildOwner.buildScope`): process the dirty list in order of increasing depth so
        // parents rebuild before children; elements cleaned by an ancestor's rebuild are skipped via the
        // Dirty check instead of rebuilding twice. The list is re-sorted whenever it grew or an element was
        // re-dirtied, and the cursor rewinds onto whatever became dirty behind it.
        _dirty.Sort(ElementDepthComparer.Instance.Compare);
        _dirtyNeedsResorting = false;
        int dirtyCount = _dirty.Count;
        int index = 0;
        while (index < dirtyCount)
        {
            Element element = _dirty[index];
            if (element.IsActive && element.Owner == this && element.Dirty)
            {
                element.Rebuild();
            }

            index += 1;
            if (dirtyCount >= _dirty.Count && !_dirtyNeedsResorting)
            {
                continue;
            }

            _dirty.Sort(ElementDepthComparer.Instance.Compare);
            _dirtyNeedsResorting = false;
            dirtyCount = _dirty.Count;
            while (index > 0 && _dirty[index - 1].Dirty)
            {
                index -= 1;
            }
        }

        _dirty.Clear();
        _dirtyMembership.Clear();
    }

    internal void FlushBuild()
    {
        Scheduler.FlushMicrotasks();
        BuildScope();

        // Test harnesses use FlushBuild as their pump boundary. Production frame flow calls
        // BuildScope directly and drains microtasks after the frame in Scheduler.HandleFrame.
        Scheduler.FlushMicrotasks();
    }

    /// <summary>
    /// Unmounts every element that was deactivated during the current build and never reactivated.
    /// </summary>
    /// <remarks>Flutter's <c>BuildOwner.finalizeTree</c>.</remarks>
    public void FinalizeTree()
    {
        FinalizeInactiveElements();
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
            if (element.IsInactive && element.Parent is null)
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

            int depthCompare = x.Depth.CompareTo(y.Depth);
            if (depthCompare != 0)
            {
                return depthCompare;
            }

            return x.SequenceId.CompareTo(y.SequenceId);
        }
    }
}
