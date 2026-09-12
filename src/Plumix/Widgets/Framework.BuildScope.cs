using Plumix.Foundation;
using Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart

namespace Plumix.Widgets;

/// <summary>
/// An <see cref="Element"/> subtree that is built independently of the rest of the tree.
/// </summary>
/// <remarks>
/// <para>
/// Dart's <c>BuildScope</c>. Elements in the same scope share one dirty list, which
/// <see cref="BuildOwner.BuildScope(Element, Action?)"/> flushes in depth order. Most elements
/// inherit the scope of their parent, so a widget tree normally has exactly one scope — the one
/// <see cref="BuildOwner.RootBuildScope"/> hands to the root element. A widget that builds its
/// children at a different time than the enclosing tree (Plumix's <c>LayoutBuilder</c> and
/// <c>SliverLayoutBuilder</c>, which build during layout) gives its element its own scope by
/// overriding <see cref="Element.BuildScope"/>, so those descendants are never rebuilt by the
/// ambient build pass.
/// </para>
/// <para>
/// <see cref="ScheduleRebuild"/> is how a scope that is not driven by the frame's build phase asks
/// to be flushed: it is invoked the first time an element in the scope becomes dirty while the
/// scope is not building. The root scope leaves it <see langword="null"/>, because
/// <see cref="BuildOwner.OnBuildScheduled"/> already asks the host for a frame.
/// </para>
/// </remarks>
public sealed class BuildScope
{
    /// <summary>Creates a build scope.</summary>
    /// <param name="scheduleRebuild">
    /// Called when the first element in this scope becomes dirty while the scope is not building.
    /// </param>
    public BuildScope(Action? scheduleRebuild = null)
    {
        ScheduleRebuild = scheduleRebuild;
    }

    /// <summary>Dart's <c>BuildScope.scheduleRebuild</c>.</summary>
    public Action? ScheduleRebuild { get; }

    // Flutter parity: a list, not an ordered set. An element's depth changes when it is reparented,
    // which would corrupt an ordered container it is already sitting in. Membership is tracked by
    // Element.InDirtyList (Dart's `_inDirtyList`) rather than by a side set, so an element that is
    // deactivated and reactivated inside one flush is not enqueued twice.
    private readonly List<Element> _dirtyElements = [];

    private bool _buildScheduled;
    private bool _building;

    // Dart's tri-state `_dirtyElementsNeedsResorting`: null outside a flush (which is also how
    // reentrancy is detected), false while flushing with the list in sort order, true once the list
    // changed under the cursor and has to be re-sorted before the walk continues.
    private bool? _dirtyElementsNeedsResorting;

    /// <summary>Whether <see cref="BuildOwner.BuildScope(Element, Action?)"/> is flushing this scope.</summary>
    internal bool Building
    {
        get => _building;
        set => _building = value;
    }

    /// <summary>The elements queued for a rebuild in this scope, in the order they will be walked.</summary>
    internal IReadOnlyList<Element> DirtyElements => _dirtyElements;

    /// <summary>
    /// Empties the dirty list without rebuilding anything, so the caller can re-queue the entries it
    /// still wants. Plumix-only, used by <see cref="BuildOwner.BuildScopeDuringLayout"/>.
    /// </summary>
    internal void TakeDirtyElements()
    {
        foreach (Element element in _dirtyElements)
        {
            if (ReferenceEquals(element.BuildScope, this))
            {
                element.InDirtyList = false;
            }
        }

        _dirtyElements.Clear();
    }

    /// <summary>Dart's <c>BuildScope._scheduleBuildFor</c>.</summary>
    internal void ScheduleBuildFor(Element element)
    {
        if (!ReferenceEquals(element.BuildScope, this))
        {
            throw new AssertionError("An element can only be scheduled to build in its own build scope.");
        }

        if (!element.InDirtyList)
        {
            _dirtyElements.Add(element);
            element.InDirtyList = true;
        }

        if (!_buildScheduled && !_building)
        {
            _buildScheduled = true;
            ScheduleRebuild?.Invoke();
        }

        // Raised on every schedule made during a flush, including one for an element that is already
        // queued: its dirty flag just changed, and that is a sort key.
        if (_dirtyElementsNeedsResorting != null)
        {
            _dirtyElementsNeedsResorting = true;
        }
    }

    /// <summary>Dart's <c>BuildScope._tryRebuild</c>.</summary>
    private void TryRebuild(Element element)
    {
        if (!ReferenceEquals(element.BuildScope, this))
        {
            throw new AssertionError("An element can only be rebuilt by its own build scope.");
        }

        try
        {
            element.Rebuild();
        }
        catch (Exception exception)
        {
            FrameworkErrors.ReportException(
                new ErrorDescription("while rebuilding dirty elements"),
                exception,
                informationCollector: () =>
                {
                    var information = new List<DiagnosticsNode>();
                    if (Constants.KDebugMode)
                    {
                        information.Add(new DiagnosticsDebugCreator(new DebugCreator(element)));
                    }

                    information.Add(element.DescribeElement("The element being rebuilt at the time was"));
                    return information;
                });
        }
    }

    /// <summary>Dart's <c>BuildScope._debugAssertElementInScope</c>.</summary>
    private static void DebugAssertElementInScope(Element element, Element? debugBuildRoot)
    {
        // A null root means the flush was started without a context (Plumix's parameterless
        // BuildOwner.BuildScope), which has nothing to compare the element against.
        if (!Constants.KDebugMode || debugBuildRoot is null)
        {
            return;
        }

        if (element.DebugIsDescendantOf(debugBuildRoot) || !element.DebugIsActive)
        {
            return;
        }

        throw new FlutterError(
        [
            new ErrorSummary("Tried to build dirty widget in the wrong build scope."),
            new ErrorDescription(
                "A widget which was marked as dirty and is still active was scheduled to be built, "
                + "but the current build scope unexpectedly does not contain that widget."),
            new ErrorHint(
                "Sometimes this is detected when an element is removed from the widget tree, but the "
                + "element somehow did not get marked as inactive. In that case, it might be caused by "
                + "an ancestor element failing to implement visitChildren correctly, thus preventing "
                + "some or all of its descendants from being correctly deactivated."),
            new DiagnosticsProperty<Element>(
                "The root of the build scope was",
                debugBuildRoot,
                style: DiagnosticsTreeStyle.ErrorProperty),
            new DiagnosticsProperty<Element>(
                "The offending element (which does not appear to be a descendant of the root of the "
                + "build scope) was",
                element,
                style: DiagnosticsTreeStyle.ErrorProperty),
        ]);
    }

    /// <summary>Dart's <c>BuildScope._flushDirtyElements</c>.</summary>
    /// <remarks>
    /// Walks the dirty list in order of increasing depth so parents rebuild before children;
    /// an element cleaned by an ancestor's rebuild is skipped by <see cref="Element.Rebuild"/>
    /// instead of building twice. The list is re-sorted whenever an element was queued or re-dirtied
    /// under the cursor, and the cursor rewinds onto whatever became dirty behind it.
    /// </remarks>
    internal void FlushDirtyElements(Element? debugBuildRoot)
    {
        if (_dirtyElementsNeedsResorting != null)
        {
            throw new AssertionError("FlushDirtyElements must be non-reentrant.");
        }

        _dirtyElements.Sort(Element.Sort);
        _dirtyElementsNeedsResorting = false;
        try
        {
            for (int index = 0; index < _dirtyElements.Count; index = DirtyElementIndexAfter(index))
            {
                Element element = _dirtyElements[index];

                // A scope migration leaves an entry for the old scope to skip. Inactive elements
                // keep their membership and reach Rebuild, which ignores their lifecycle state.
                if (!ReferenceEquals(element.BuildScope, this))
                {
                    continue;
                }

                DebugAssertElementInScope(element, debugBuildRoot);
                TryRebuild(element);
            }

            DebugAssertNoMissedElements(debugBuildRoot);
        }
        finally
        {
            foreach (Element element in _dirtyElements)
            {
                if (ReferenceEquals(element.BuildScope, this))
                {
                    element.InDirtyList = false;
                }
            }

            _dirtyElements.Clear();
            _dirtyElementsNeedsResorting = null;
            _buildScheduled = false;
        }
    }

    /// <summary>Dart's closing assert in <c>_flushDirtyElements</c>.</summary>
    private void DebugAssertNoMissedElements(Element? debugBuildRoot)
    {
        if (!Constants.KDebugMode)
        {
            return;
        }

        List<Element> missed =
        [
            .. _dirtyElements.Where(x => x.DebugIsActive && x.Dirty && ReferenceEquals(x.BuildScope, this)),
        ];
        if (missed.Count == 0)
        {
            return;
        }

        throw new FlutterError(
        [
            new ErrorSummary("buildScope missed some dirty elements."),
            new ErrorHint("This probably indicates that the dirty list should have been resorted but was not."),
            new DiagnosticsProperty<Element?>(
                "The context argument of the buildScope call was",
                debugBuildRoot,
                style: DiagnosticsTreeStyle.ErrorProperty),
            Element.DescribeElements("The list of missed elements at the end of the buildScope call was", missed),
        ]);
    }

    /// <summary>Dart's <c>BuildScope._dirtyElementIndexAfter</c>.</summary>
    private int DirtyElementIndexAfter(int index)
    {
        if (_dirtyElementsNeedsResorting != true)
        {
            return index + 1;
        }

        index += 1;
        _dirtyElements.Sort(Element.Sort);
        _dirtyElementsNeedsResorting = false;

        // Previously dirty but now inactive elements can move right in the list, so the cursor has to
        // move left until it sits just after the right-most clean node.
        while (index > 0 && _dirtyElements[index - 1].Dirty)
        {
            index -= 1;
        }

        return index;
    }
}
