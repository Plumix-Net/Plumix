using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart

namespace Plumix.Widgets;

/// <summary>
/// Manager class for the widgets framework: tracks which widgets need rebuilding and handles the
/// other tasks that apply to widget trees as a whole. Dart's <c>BuildOwner</c>.
/// </summary>
public sealed class BuildOwner
{
    // Dart reads `GlobalKey.currentContext` through the one binding's build owner. Plumix has no
    // binding singleton — every host and harness owns its BuildOwner — so the key asks the live owners.
    private static readonly List<WeakReference<BuildOwner>> Owners = [];
    private static readonly object OwnersLock = new();

    private readonly Dictionary<GlobalKey, Element> _globalKeyRegistry = [];

    private bool _scheduledFlushDirtyElements;

    /// <summary>Creates an object that manages widgets.</summary>
    /// <remarks>
    /// Flutter's <c>BuildOwner({onBuildScheduled, focusManager})</c>. When no manager is supplied,
    /// the owner creates one and registers its global input handlers. Additional/off-screen owners
    /// should pass an explicit manager when they must not replace those handlers.
    /// </remarks>
    public BuildOwner(Action? onBuildScheduled = null, FocusManager? focusManager = null)
    {
        OnBuildScheduled = onBuildScheduled;
        FocusManager = focusManager ?? new FocusManager();
        if (focusManager is null)
        {
            FocusManager.RegisterGlobalHandlers();
        }

        lock (OwnersLock)
        {
            Owners.Add(new WeakReference<BuildOwner>(this));
        }
    }

    /// <summary>
    /// Called on each build pass when the first buildable element is marked dirty. Dart's
    /// <c>BuildOwner.onBuildScheduled</c>.
    /// </summary>
    public Action? OnBuildScheduled { get; set; }

    /// <summary>The focus-tree owner associated with this widget-tree owner.</summary>
    /// <remarks>Flutter's mutable <c>BuildOwner.focusManager</c> field.</remarks>
    public FocusManager FocusManager { get; set; }

    /// <summary>Dart's <c>BuildOwner._inactiveElements</c>.</summary>
    internal InactiveElements InactiveElements { get; } = new();

    /// <summary>
    /// The element mounted without a parent, used as the debug build root of the parameterless
    /// harness pump. Dart reads it from <c>WidgetsBinding.rootElement</c>.
    /// </summary>
    private Element? _rootElement;

    internal void RegisterRootElement(Element element) => _rootElement = element;

    /// <summary>The element registered under <paramref name="key"/> by any live owner.</summary>
    internal static Element? LookupGlobalKey(GlobalKey key)
    {
        lock (OwnersLock)
        {
            for (int i = Owners.Count - 1; i >= 0; i--)
            {
                if (!Owners[i].TryGetTarget(out BuildOwner? owner))
                {
                    Owners.RemoveAt(i);
                    continue;
                }

                if (owner._globalKeyRegistry.TryGetValue(key, out Element? element))
                {
                    return element;
                }
            }
        }

        return null;
    }

    /// <summary>The element this owner registered under <paramref name="key"/>.</summary>
    internal Element? GlobalKeyElement(GlobalKey key) =>
        _globalKeyRegistry.TryGetValue(key, out Element? element) ? element : null;

    /// <summary>
    /// Adds <paramref name="element"/> to its <see cref="Widgets.BuildScope"/>'s dirty list and asks
    /// for a frame if none is pending. Dart's <c>BuildOwner.scheduleBuildFor</c>.
    /// </summary>
    public void ScheduleBuildFor(Element element)
    {
        ArgumentNullException.ThrowIfNull(element);
        DebugAssertions.Assert(ReferenceEquals(element.Owner, this));
        DebugAssertions.Assert(element.HasParentBuildScope);
        if (Constants.KDebugMode)
        {
            if (WidgetsDebug.DebugPrintScheduleBuildForStacks)
            {
                string suffix = element.BuildScope.DirtyElements.Contains(element)
                    ? " (ALREADY IN LIST)"
                    : string.Empty;
                Assertions.DebugPrintStack(label: $"scheduleBuildFor() called for {element}{suffix}");
            }

            if (!element.Dirty)
            {
                throw new FlutterError(
                [
                    new ErrorSummary("scheduleBuildFor() called for a widget that is not marked as dirty."),
                    element.DescribeElement("The method was called for the following element"),
                    new ErrorDescription(
                        "This element is not current marked as dirty. Make sure to set the dirty flag before "
                        + "calling scheduleBuildFor()."),
                    new ErrorHint(
                        "If you did not attempt to call scheduleBuildFor() yourself, then this probably "
                        + "indicates a bug in the widgets framework. Please report it:\n"
                        + "  https://github.com/flutter/flutter/issues/new?template=02_bug.yml"),
                ]);
            }
        }

        BuildScope buildScope = element.BuildScope;
        if (Constants.KDebugMode)
        {
            if (WidgetsDebug.DebugPrintScheduleBuildForStacks && element.InDirtyList)
            {
                Assertions.DebugPrintStack(
                    label: "BuildOwner.scheduleBuildFor() called; _dirtyElementsNeedsResorting was "
                    + $"{buildScope.DebugDirtyElementsNeedsResortingDescription} (now true); "
                    + "The dirty list for the current build scope is: "
                    + $"[{string.Join(", ", buildScope.DirtyElements)}]");
            }

            if (!DebugBuilding && element.InDirtyList)
            {
                throw new FlutterError(
                [
                    new ErrorSummary("BuildOwner.scheduleBuildFor() called inappropriately."),
                    new ErrorHint(
                        "The BuildOwner.scheduleBuildFor() method called on an Element "
                        + "that is already in the dirty list."),
                    element.DescribeElement("the dirty Element was"),
                ]);
            }
        }

        if (!_scheduledFlushDirtyElements && OnBuildScheduled != null)
        {
            _scheduledFlushDirtyElements = true;
            OnBuildScheduled();
        }

        buildScope.ScheduleBuildFor(element);
        if (Constants.KDebugMode && WidgetsDebug.DebugPrintScheduleBuildForStacks)
        {
            Print.DebugPrint(
                $"...the build scope's dirty list is now: [{string.Join(", ", buildScope.DirtyElements)}]");
        }
    }

    private int _debugStateLockLevel;

    /// <summary>Dart's <c>BuildOwner._debugStateLocked</c>.</summary>
    internal bool DebugStateLocked => _debugStateLockLevel > 0;

    /// <summary>
    /// Whether this widget tree is in the build phase. Only valid when asserts are enabled. Dart's
    /// <c>BuildOwner.debugBuilding</c>.
    /// </summary>
    public bool DebugBuilding { get; private set; }

    /// <summary>Dart's <c>BuildOwner._debugCurrentBuildTarget</c>.</summary>
    internal Element? DebugCurrentBuildTarget { get; set; }

    /// <summary>
    /// Establishes a scope in which calls to <see cref="State.SetState"/> are forbidden, and calls
    /// <paramref name="callback"/> in that scope. Dart's <c>BuildOwner.lockState</c>.
    /// </summary>
    public void LockState(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        DebugAssertions.Assert(_debugStateLockLevel >= 0);
        if (Constants.KDebugMode)
        {
            _debugStateLockLevel += 1;
        }

        try
        {
            callback();
        }
        finally
        {
            if (Constants.KDebugMode)
            {
                _debugStateLockLevel -= 1;
            }
        }

        DebugAssertions.Assert(_debugStateLockLevel >= 0);
    }

    /// <summary>
    /// Establishes <paramref name="context"/> as the target of a build-scope callback, then flushes
    /// the dirty elements of that context's <see cref="Widgets.BuildScope"/>.
    /// </summary>
    /// <remarks>Flutter's <c>BuildOwner.buildScope(Element, [VoidCallback])</c>.</remarks>
    public void BuildScope(Element context, Action? callback = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        BuildScope buildScope = context.BuildScope;
        if (callback == null && buildScope.DirtyElements.Count == 0)
        {
            return;
        }

        RunBuildScope(buildScope, context, callback);
    }

    private void RunBuildScope(BuildScope buildScope, Element? context, Action? callback)
    {
        using Scheduler.FrameworkThreadScope scope = Scheduler.EnterFrameworkThread();
        DebugAssertions.Assert(_debugStateLockLevel >= 0);
        DebugAssertions.Assert(!DebugBuilding);
        if (Constants.KDebugMode)
        {
            if (WidgetsDebug.DebugPrintBuildScope)
            {
                Print.DebugPrint(
                    $"buildScope called with context {context}; its build scope's dirty list is: "
                    + $"[{string.Join(", ", buildScope.DirtyElements)}]");
            }

            _debugStateLockLevel += 1;
            DebugBuilding = true;
        }

        using IDisposable buildPhase = Scheduler.BuildScope();
        try
        {
            _scheduledFlushDirtyElements = true;
            buildScope.Building = true;
            if (callback != null)
            {
                DebugAssertions.Assert(DebugStateLocked);
                Element? debugPreviousBuildTarget = null;
                if (Constants.KDebugMode)
                {
                    debugPreviousBuildTarget = DebugCurrentBuildTarget;
                    DebugCurrentBuildTarget = context;
                }

                try
                {
                    callback();
                }
                finally
                {
                    if (Constants.KDebugMode)
                    {
                        DebugAssertions.Assert(ReferenceEquals(DebugCurrentBuildTarget, context));
                        DebugCurrentBuildTarget = debugPreviousBuildTarget;
                        if (context != null)
                        {
                            DebugElementWasRebuilt(context);
                        }
                    }
                }
            }

            buildScope.FlushDirtyElements(context);
        }
        finally
        {
            buildScope.Building = false;
            _scheduledFlushDirtyElements = false;
            DebugAssertions.Assert(!Constants.KDebugMode || DebugBuilding);
            if (Constants.KDebugMode)
            {
                DebugBuilding = false;
                _debugStateLockLevel -= 1;
                if (WidgetsDebug.DebugPrintBuildScope)
                {
                    Print.DebugPrint("buildScope finished");
                }
            }
        }

        DebugAssertions.Assert(_debugStateLockLevel >= 0);
    }

    /// <summary>
    /// Plumix-only harness pump: runs the transient frame callbacks, flushes the root build scope and
    /// finalizes the tree, the way a frame does between its build and post-frame phases.
    /// </summary>
    internal void FlushBuild()
    {
        using Scheduler.FrameworkThreadScope scope = Scheduler.EnterFrameworkThread();
        Scheduler.FlushMicrotasks();

        // Dart runs the transient frame callbacks before the build phase of every frame, and
        // `LayoutBuilder`'s scope defers its rebuild request to one. A harness pump produces no
        // frame, so drain them here or that rebuild is dropped.
        Scheduler.RunScheduledFrameCallbacksOutsideFrame();
        if (_rootElement is not null)
        {
            BuildScope(_rootElement);
        }

        // A harness pump has no render phase of its own to finalize after, so it finalizes here.
        FinalizeTree();

        // The harness finishes its render frame in PipelineOwner.CompositeFrame. Microtasks
        // scheduled during build must wait until then, as they do in a production frame.
    }

    // Debug-only global-key bookkeeping. Dart keeps the same structures on BuildOwner and reads them
    // from finalizeTree, so a duplicated key produces a readable report instead of a silently
    // truncated widget tree.
    private Dictionary<Element, HashSet<GlobalKey>>? _debugElementsThatWillNeedToBeRebuilt;
    private readonly HashSet<Element>? _debugIllFatedElements = Constants.KDebugMode ? [] : null;
    private readonly Dictionary<Element, Dictionary<Element, GlobalKey>>? _debugGlobalKeyReservations =
        Constants.KDebugMode ? [] : null;

    /// <summary>
    /// Dart's <c>BuildOwner._debugTrackElementThatWillNeedToBeRebuiltDueToGlobalKeyShenanigans</c>:
    /// remembers a parent that lost a global-keyed child to a different parent, so
    /// <see cref="FinalizeTree"/> can report it if that parent never rebuilt in this frame.
    /// </summary>
    internal void DebugTrackElementThatWillNeedToBeRebuilt(Element node, GlobalKey key)
    {
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

    /// <summary>The number of <see cref="GlobalKey"/> instances currently registered with this owner.</summary>
    /// <remarks>Flutter's <c>BuildOwner.globalKeyCount</c>.</remarks>
    public int GlobalKeyCount => _globalKeyRegistry.Count;

    /// <summary>Dart's <c>BuildOwner._debugRemoveGlobalKeyReservationFor</c>.</summary>
    internal void DebugRemoveGlobalKeyReservationFor(Element parent, Element child)
    {
        if (Constants.KDebugMode
            && _debugGlobalKeyReservations!.TryGetValue(parent, out Dictionary<Element, GlobalKey>? childToKey))
        {
            childToKey.Remove(child);
        }
    }

    /// <summary>
    /// Dart's <c>BuildOwner._registerGlobalKey</c>: the registry is last-writer-wins, and a key that
    /// is claimed twice records the previous occupant as "ill fated" so <see cref="FinalizeTree"/>
    /// can report the duplication with both widgets named.
    /// </summary>
    internal void RegisterGlobalKey(GlobalKey key, Element element)
    {
        if (Constants.KDebugMode && _globalKeyRegistry.TryGetValue(key, out Element? oldElement))
        {
            DebugAssertions.Assert(element.Widget.GetType() != oldElement.Widget.GetType());
            _debugIllFatedElements!.Add(oldElement);
        }

        _globalKeyRegistry[key] = element;
    }

    /// <summary>Dart's <c>BuildOwner._unregisterGlobalKey</c>.</summary>
    internal void UnregisterGlobalKey(GlobalKey key, Element element)
    {
        if (Constants.KDebugMode
            && _globalKeyRegistry.TryGetValue(key, out Element? oldElement)
            && !ReferenceEquals(oldElement, element))
        {
            DebugAssertions.Assert(element.Widget.GetType() != oldElement.Widget.GetType());
        }

        if (_globalKeyRegistry.TryGetValue(key, out Element? registered) && ReferenceEquals(registered, element))
        {
            _globalKeyRegistry.Remove(key);
        }
    }

    /// <summary>Dart's <c>BuildOwner._debugReserveGlobalKeyFor</c>.</summary>
    internal void DebugReserveGlobalKeyFor(Element parent, Element child, GlobalKey key)
    {
        if (!Constants.KDebugMode)
        {
            return;
        }

        if (!_debugGlobalKeyReservations!.TryGetValue(parent, out Dictionary<Element, GlobalKey>? childToKey))
        {
            childToKey = [];
            _debugGlobalKeyReservations[parent] = childToKey;
        }

        childToKey[child] = key;
    }

    /// <summary>Dart's <c>BuildOwner._debugVerifyGlobalKeyReservation</c>.</summary>
    private void DebugVerifyGlobalKeyReservation()
    {
        var keyToParent = new Dictionary<GlobalKey, Element>();
        foreach ((Element parent, Dictionary<Element, GlobalKey> childToKey) in _debugGlobalKeyReservations!)
        {
            // Parents that are gone, or whose render object has been detached, are not evidence.
            if (parent.LifecycleState == ElementLifecycleState.Defunct || parent.RenderObject?.Attached == false)
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

                if (keyToParent.TryGetValue(key, out Element? older) && !ReferenceEquals(older, parent))
                {
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
                                $"The key {key} was used by multiple widgets. The parents of those widgets "
                                + "were different widgets that both had the following description:\n  "
                                + $"{parent}\nA GlobalKey can only be specified on one widget at a time in the "
                                + "widget tree."),
                        ]);

                    // Repair the tree before reporting, so tearing it down does not pile on more errors.
                    DebugForgetDuplicate(older, child);
                    DebugForgetDuplicate(newer, child);
                    throw error;
                }

                keyToParent[key] = parent;
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
        foreach (Element element in _debugIllFatedElements!)
        {
            if (element.LifecycleState == ElementLifecycleState.Defunct)
            {
                continue;
            }

            DebugAssertions.Assert(element.Widget.Key != null);
            var key = (GlobalKey)element.Widget.Key!;
            DebugAssertions.Assert(_globalKeyRegistry.ContainsKey(key));
            duplicates ??= [];
            if (!duplicates.TryGetValue(key, out List<Element>? elements))
            {
                elements = [];
                duplicates[key] = elements;
            }

            // Insertion-ordered, like Dart's set literal, so the ill-fated element comes first.
            if (!elements.Contains(element))
            {
                elements.Add(element);
            }

            Element current = _globalKeyRegistry[key];
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
            information.Add(Element.DescribeElements(
                $"The key {key} was used by {elements.Count} widgets",
                elements));
        }

        information.Add(new ErrorDescription(
            "A GlobalKey can only be specified on one widget at a time in the widget tree."));
        throw new FlutterError(information);
    }

    /// <summary>
    /// Complete the element build pass by unmounting any elements that are no longer active, then run
    /// the three debug-only global-key checks. Dart's <c>BuildOwner.finalizeTree</c>: a failure is
    /// reported rather than thrown, because the tree is already inconsistent and raising an
    /// <c>ErrorWidget</c> here would only produce follow-on exceptions.
    /// </summary>
    public void FinalizeTree()
    {
        try
        {
            // This unregisters the GlobalKeys.
            if (!InactiveElements.IsEmpty)
            {
                LockState(InactiveElements.UnmountAll);
            }

            if (Constants.KDebugMode)
            {
                try
                {
                    DebugVerifyGlobalKeyReservation();
                    DebugVerifyIllFatedPopulation();
                    DebugVerifyNoUnrebuiltReparentingVictims();
                }
                finally
                {
                    _debugElementsThatWillNeedToBeRebuilt?.Clear();
                }
            }
        }
        catch (Exception exception)
        {
            FrameworkErrors.ReportException(new ErrorSummary("while finalizing the widget tree"), exception);
        }
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
            if (element.LifecycleState != ElementLifecycleState.Defunct)
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
        DebugAssertions.Assert(keyLabels.Count > 0);

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

    /// <summary>
    /// Cause the entire subtree rooted at the given <see cref="Element"/> to be entirely rebuilt.
    /// Used by development tools when the application code has changed and is being hot-reloaded.
    /// Dart's <c>BuildOwner.reassemble</c>.
    /// </summary>
    public void Reassemble(Element root)
    {
        DebugAssertions.Assert(root.Parent == null);
        DebugAssertions.Assert(ReferenceEquals(root.Owner, this));
        root.Reassemble();
    }
}
