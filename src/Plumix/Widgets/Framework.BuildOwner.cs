using Plumix.Foundation;

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

    // Debug-only global-key bookkeeping. Dart keeps the same three structures on BuildOwner and reads
    // them from finalizeTree, so a duplicated key produces a readable report instead of a silently
    // truncated widget tree.
    private readonly HashSet<Element> _debugIllFatedElements = [];
    private readonly Dictionary<Element, Dictionary<Element, GlobalKey>> _debugGlobalKeyReservations = [];
    private Dictionary<Element, HashSet<GlobalKey>>? _debugElementsThatWillNeedToBeRebuilt;
    private int _debugStateLockLevel;

    /// <summary>
    /// Whether this owner is currently rebuilding dirty elements. Dart's
    /// <c>BuildOwner.debugBuilding</c>.
    /// </summary>
    public bool DebugBuilding => _building;

    /// <summary>Dart's <c>BuildOwner._debugStateLocked</c>.</summary>
    internal bool DebugStateLocked => _debugStateLockLevel > 0;

    /// <summary>Dart's <c>BuildOwner._debugCurrentBuildTarget</c>.</summary>
    internal Element? DebugCurrentBuildTarget { get; set; }

    /// <summary>
    /// Dart's <c>BuildOwner.lockState</c>: runs <paramref name="callback"/> with the widget tree
    /// locked, so a <see cref="Element.MarkNeedsBuild"/> during it reports the mistake instead of
    /// quietly scheduling a build that would never run.
    /// </summary>
    public void LockState(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _debugStateLockLevel += 1;
        try
        {
            callback();
        }
        finally
        {
            _debugStateLockLevel -= 1;
        }
    }

    /// <summary>Dart's <c>BuildOwner._debugReserveGlobalKeyFor</c>.</summary>
    internal void DebugReserveGlobalKeyFor(Element parent, Element child, GlobalKey key)
    {
        if (!Constants.KDebugMode)
        {
            return;
        }

        if (!_debugGlobalKeyReservations.TryGetValue(parent, out Dictionary<Element, GlobalKey>? childToKey))
        {
            childToKey = [];
            _debugGlobalKeyReservations[parent] = childToKey;
        }

        childToKey[child] = key;
    }

    /// <summary>Dart's <c>BuildOwner._debugRemoveGlobalKeyReservationFor</c>.</summary>
    internal void DebugRemoveGlobalKeyReservationFor(Element parent, Element child)
    {
        if (Constants.KDebugMode && _debugGlobalKeyReservations.TryGetValue(parent, out var childToKey))
        {
            childToKey.Remove(child);
        }
    }

    /// <summary>
    /// Dart's <c>BuildOwner._debugTrackElementThatWillNeedToBeRebuiltDueToGlobalKeyShenanigans</c>:
    /// remembers a parent that lost a global-keyed child to a different parent, so
    /// <see cref="FinalizeTree"/> can report it if that parent never rebuilt in this frame.
    /// </summary>
    internal void DebugTrackElementThatWillNeedToBeRebuilt(Element node, GlobalKey key)
    {
        if (!Constants.KDebugMode)
        {
            return;
        }

        _debugElementsThatWillNeedToBeRebuilt ??= [];
        if (!_debugElementsThatWillNeedToBeRebuilt.TryGetValue(node, out HashSet<GlobalKey>? keys))
        {
            keys = [];
            _debugElementsThatWillNeedToBeRebuilt[node] = keys;
        }

        keys.Add(key);
    }

    /// <summary>
    /// Dart's <c>BuildOwner._debugElementWasRebuilt</c>: a parent that did rebuild this frame is
    /// exonerated of the reparenting complaint above.
    /// </summary>
    internal void DebugElementWasRebuilt(Element node)
    {
        _debugElementsThatWillNeedToBeRebuilt?.Remove(node);
    }

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

    /// <summary>
    /// Dart's <c>BuildOwner._registerGlobalKey</c>: the registry is last-writer-wins, and a key that
    /// is claimed twice records the previous occupant as "ill fated" so <see cref="FinalizeTree"/>
    /// can report the duplication with both widgets named.
    /// </summary>
    internal void RegisterGlobalKey(GlobalKey key, Element element)
    {
        if (Constants.KDebugMode
            && _globalKeyRegistry.TryGetValue(key, out var existing)
            && !ReferenceEquals(existing, element))
        {
            _debugIllFatedElements.Add(existing);
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
                throw new FlutterError(
                [
                    new ErrorSummary("A GlobalKey was used multiple times inside one widget's child list."),
                    new DiagnosticsProperty<GlobalKey>("The offending GlobalKey was", key),
                    element.Parent.DescribeElement("The parent of the widgets with that key was"),
                    element.DescribeElement("The first child to get instantiated with that key became"),
                    new DiagnosticsProperty<Widget>(
                        "The second child that was to be instantiated with that key was",
                        widget,
                        style: DiagnosticsTreeStyle.ErrorProperty),
                    new ErrorDescription(
                        "A GlobalKey can only be specified on one widget at a time in the widget tree."),
                ]);
            }

            element.Parent.Owner?.DebugTrackElementThatWillNeedToBeRebuilt(element.Parent, key);

            if (Constants.KDebugMode && WidgetsDebug.DebugPrintGlobalKeyedWidgetLifecycle)
            {
                Print.DebugPrint($"Attempting to take {element} from {element.Parent} to put in {newParent}.");
            }

            element.Parent.ForgetChild(element);
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
    /// <summary>
    /// Dart's <c>BuildOwner.finalizeTree</c>: unmounts everything that stayed inactive through the
    /// frame, then runs the three debug-only global-key checks. A failure is reported rather than
    /// thrown, because the tree is already inconsistent and raising an <c>ErrorWidget</c> here would
    /// only produce follow-on exceptions.
    /// </summary>
    public void FinalizeTree()
    {
        FinalizeInactiveElements();

        if (!Constants.KDebugMode)
        {
            return;
        }

        try
        {
            DebugVerifyGlobalKeyReservation();
            DebugVerifyIllFatedPopulation();
            DebugVerifyNoUnrebuiltReparentingVictims();
        }
        catch (FlutterError error)
        {
            FrameworkErrors.ReportException(new ErrorSummary("while finalizing the widget tree"), error);
        }
        finally
        {
            _debugElementsThatWillNeedToBeRebuilt?.Clear();
        }
    }

    /// <summary>Dart's <c>BuildOwner._debugVerifyGlobalKeyReservation</c>.</summary>
    private void DebugVerifyGlobalKeyReservation()
    {
        var keyToParent = new Dictionary<GlobalKey, Element>();
        foreach ((Element parent, Dictionary<Element, GlobalKey> childToKey) in _debugGlobalKeyReservations)
        {
            // Parents that are gone, or whose render object has been detached, are not evidence.
            if (parent.DebugIsDefunct || parent.RenderObject?.Attached == false)
            {
                continue;
            }

            foreach ((Element child, GlobalKey key) in childToKey)
            {
                // A child with no parent was deactivated and never re-attached elsewhere.
                if (child.Parent is null)
                {
                    continue;
                }

                if (!keyToParent.TryGetValue(key, out Element? older) || ReferenceEquals(older, parent))
                {
                    keyToParent[key] = parent;
                    continue;
                }

                Element newer = parent;
                FlutterError error = older.ToString() != newer.ToString()
                    ? new FlutterError(
                    [
                        new ErrorSummary("Multiple widgets used the same GlobalKey."),
                        new ErrorDescription(
                            $"The key {key} was used by multiple widgets. The parents of those widgets "
                            + $"were:\n- {older}\n- {newer}\nA GlobalKey can only be specified on one "
                            + "widget at a time in the widget tree."),
                    ])
                    : new FlutterError(
                    [
                        new ErrorSummary("Multiple widgets used the same GlobalKey."),
                        new ErrorDescription(
                            $"The key {key} was used by multiple widgets. The parents of those widgets were "
                            + $"different widgets that both had the following description:\n  {newer}\n"
                            + "A GlobalKey can only be specified on one widget at a time in the widget tree."),
                    ]);

                // Repair the tree before reporting, so tearing it down does not pile on more errors.
                DebugForgetDuplicate(older, child);
                DebugForgetDuplicate(newer, child);
                _debugGlobalKeyReservations.Clear();
                throw error;
            }
        }

        _debugGlobalKeyReservations.Clear();
    }

    private static void DebugForgetDuplicate(Element parent, Element child)
    {
        if (ReferenceEquals(child.Parent, parent))
        {
            return;
        }

        parent.VisitChildren(currentChild =>
        {
            if (ReferenceEquals(currentChild, child))
            {
                parent.ForgetChild(child);
            }
        });
    }

    /// <summary>Dart's <c>BuildOwner._debugVerifyIllFatedPopulation</c>.</summary>
    private void DebugVerifyIllFatedPopulation()
    {
        Dictionary<GlobalKey, List<Element>>? duplicates = null;
        foreach (Element element in _debugIllFatedElements)
        {
            if (element.DebugIsDefunct || element.Widget.Key is not GlobalKey key)
            {
                continue;
            }

            if (!_globalKeyRegistry.TryGetValue(key, out Element? current))
            {
                continue;
            }

            duplicates ??= [];
            if (!duplicates.TryGetValue(key, out List<Element>? elements))
            {
                elements = [];
                duplicates[key] = elements;
            }

            // Insertion-ordered, so the ill-fated element is reported before the current occupant.
            if (!elements.Contains(element))
            {
                elements.Add(element);
            }

            if (!elements.Contains(current))
            {
                elements.Add(current);
            }
        }

        _debugIllFatedElements.Clear();
        if (duplicates is null)
        {
            return;
        }

        var information = new List<DiagnosticsNode>
        {
            new ErrorSummary("Multiple widgets used the same GlobalKey."),
        };
        foreach ((GlobalKey key, List<Element> elements) in duplicates)
        {
            information.Add(Element.DescribeElements($"The key {key} was used by {elements.Count} widgets", elements));
        }

        information.Add(new ErrorDescription(
            "A GlobalKey can only be specified on one widget at a time in the widget tree."));
        throw new FlutterError(information);
    }

    /// <summary>
    /// Dart's third <c>finalizeTree</c> check: a parent that lost a global-keyed child to another
    /// parent and then never rebuilt still believes it owns that child.
    /// </summary>
    private void DebugVerifyNoUnrebuiltReparentingVictims()
    {
        if (_debugElementsThatWillNeedToBeRebuilt is not { Count: > 0 } victims)
        {
            return;
        }

        var keys = new HashSet<GlobalKey>();
        foreach ((Element element, HashSet<GlobalKey> elementKeys) in victims)
        {
            if (!element.DebugIsDefunct)
            {
                keys.UnionWith(elementKeys);
            }
        }

        if (keys.Count == 0)
        {
            return;
        }

        List<string> keyLabels = DebugCountedLabels(keys.Select(key => key.ToString() ?? string.Empty), "keys");
        List<string> elementLabels = DebugCountedLabels(
            victims.Keys.Select(element => element.ToString() ?? string.Empty),
            "elements");

        bool oneKey = keys.Count == 1;
        bool oneElement = elementLabels.Count == 1;
        string the = oneKey ? " the" : string.Empty;
        string s = oneKey ? string.Empty : "s";
        string were = oneKey ? "was" : "were";
        string their = oneKey ? "its" : "their";
        string respective = oneElement ? string.Empty : " respective";
        string those = oneKey ? "that" : "those";
        string s2 = oneElement ? string.Empty : "s";
        string those2 = oneElement ? "that" : "those";
        string they = oneElement ? "it" : "they";
        string think = oneElement ? "thinks" : "think";
        string are = oneElement ? "is" : "are";

        throw new FlutterError(
        [
            new ErrorSummary($"Duplicate GlobalKey{s} detected in widget tree."),
            new ErrorDescription(
                $"The following GlobalKey{s} {were} specified multiple times in the widget tree. This will "
                + "lead to parts of the widget tree being truncated unexpectedly, because the second time a "
                + $"key is seen, the previous instance is moved to the new location. The key{s} {were}:\n"
                + $"- {string.Join("\n  ", keyLabels)}\n"
                + $"This was determined by noticing that after{the} widget{s} with the above global key{s} "
                + $"{were} moved out of {their}{respective} previous parent{s2}, {those2} previous parent{s2} "
                + "never updated during this frame, meaning that "
                + $"{they} either did not update at all or updated before the widget{s} {were} moved, in "
                + $"either case implying that {they} still {think} that {they} should have a child with "
                + $"{those} global key{s}.\n"
                + $"The specific parent{s2} that did not update after having one or more children forcibly "
                + $"removed due to GlobalKey reparenting {are}:\n"
                + $"- {string.Join("\n  ", elementLabels)}"
                + "\nA GlobalKey can only be specified on one widget at a time in the widget tree."),
        ]);
    }

    private static List<string> DebugCountedLabels(IEnumerable<string> values, string noun)
    {
        var counts = new Dictionary<string, int>();
        foreach (string value in values)
        {
            counts[value] = counts.TryGetValue(value, out int count) ? count + 1 : 1;
        }

        return
        [
            .. counts.Select(entry => entry.Value == 1
                ? entry.Key
                : $"{entry.Key} ({entry.Value} different affected {noun} had this toString representation)"),
        ];
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
            if (!element.IsInactive || element.Parent is not null)
            {
                continue;
            }

            if (Constants.KDebugMode
                && WidgetsDebug.DebugPrintGlobalKeyedWidgetLifecycle
                && element.Widget.Key is GlobalKey)
            {
                Print.DebugPrint($"Discarding {element} from inactive elements list.");
            }

            element.Unmount();
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
