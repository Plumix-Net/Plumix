using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart (_InactiveElements)

namespace Plumix.Widgets;

/// <summary>
/// The set of elements that have been removed from the tree but may still be reactivated before the
/// end of the frame. Dart's private <c>_InactiveElements</c>.
/// </summary>
/// <remarks>
/// Owned by <see cref="BuildOwner"/> and never handed out: an element joins through
/// <see cref="Element.DeactivateChild"/> and leaves either through
/// <c>BuildOwner.RetakeInactiveElement</c> (reactivated) or through <see cref="UnmountAll"/> at the
/// end of the frame (unmounted).
/// </remarks>
internal sealed class InactiveElements
{
    private readonly HashSet<Element> _elements = [];
    private bool _locked;

    /// <summary>Whether anything is parked, so <c>finalizeTree</c> can skip the whole pass.</summary>
    internal bool IsEmpty => _elements.Count == 0;

    /// <summary>
    /// Dart's <c>_InactiveElements._deactivateRecursively</c>: deactivates one element and then every
    /// descendant, deepest last. A throwing <c>deactivate()</c> forces the whole subtree into
    /// <see cref="ElementLifecycleState.Failed"/> and rethrows, so the element never reaches the
    /// inactive list.
    /// </summary>
    internal static void DeactivateRecursively(Element element)
    {
        if (Constants.KDebugMode && !element.IsActive)
        {
            throw new AssertionError($"{element} must be active to be deactivated.");
        }

        try
        {
            element.Deactivate();
        }
        catch (Exception)
        {
            Element.DeactivateFailedSubtreeRecursively(element);
            throw;
        }

        element.VisitChildren(DeactivateRecursively);
        if (Constants.KDebugMode)
        {
            element.DebugDeactivated();
        }
    }

    /// <summary>
    /// Plumix-only: the ownerless fallback of <see cref="Element.DeactivateChild"/>. Dart asserts an
    /// owner there and always parks the child; an element with no owner has no inactive list to park
    /// it in, so the subtree is torn down inline in the order <see cref="UnmountAll"/> would use.
    /// </summary>
    internal static void DeactivateAndUnmount(Element element)
    {
        if (element.IsActive)
        {
            DeactivateRecursively(element);
        }

        Unmount(element);
    }

    /// <summary>
    /// Dart's <c>_InactiveElements.add</c>. An element is only recorded once the whole subtree below
    /// it has been deactivated successfully.
    /// </summary>
    internal void Add(Element element)
    {
        if (_locked)
        {
            throw new AssertionError("An element cannot be deactivated while the inactive list is unmounting.");
        }

        if (_elements.Contains(element))
        {
            throw new AssertionError($"{element} is already in the inactive elements list.");
        }

        if (element.Parent is not null)
        {
            throw new AssertionError($"{element} must be detached from its parent before being deactivated.");
        }

        switch (element.LifecycleState)
        {
            case ElementLifecycleState.Active:
                DeactivateRecursively(element);
                _elements.Add(element);
                break;
            case ElementLifecycleState.Inactive:
                _elements.Add(element);
                break;
            default:
                throw new AssertionError(
                    $"{element} must not be deactivated when in {element.LifecycleState} state.");
        }
    }

    /// <summary>Dart's <c>_InactiveElements.remove</c>: the element is being reactivated.</summary>
    internal void Remove(Element element)
    {
        if (_locked)
        {
            throw new AssertionError("An element cannot be reactivated while the inactive list is unmounting.");
        }

        _elements.Remove(element);
    }

    /// <summary>
    /// Dart's <c>_InactiveElements._unmountAll</c>: unmounts everything that stayed inactive through
    /// the frame, deepest subtree first, so a sibling that was removed from further down the tree is
    /// torn down before a shallower one.
    /// </summary>
    internal void UnmountAll()
    {
        _locked = true;
        List<Element> elements = [.. _elements];
        elements.Sort(Element.Sort);
        _elements.Clear();
        try
        {
            for (int index = elements.Count - 1; index >= 0; index--)
            {
                Unmount(elements[index]);
            }
        }
        finally
        {
            _locked = false;
        }
    }

    /// <summary>
    /// Dart's <c>_InactiveElements._unmount</c>: children before their parent, so an element is only
    /// made defunct once everything below it already is.
    /// </summary>
    private static void Unmount(Element element)
    {
        if (Constants.KDebugMode
            && WidgetsDebug.DebugPrintGlobalKeyedWidgetLifecycle
            && element.Widget.Key is GlobalKey)
        {
            Print.DebugPrint($"Discarding {element} from inactive elements list.");
        }

        element.VisitChildren(child =>
        {
            if (Constants.KDebugMode
                && child.LifecycleState != ElementLifecycleState.Defunct
                && !ReferenceEquals(child.Parent, element))
            {
                throw new AssertionError($"{child} is visited by {element} but is not its child.");
            }

            Unmount(child);
        });
        element.Unmount();
    }
}
