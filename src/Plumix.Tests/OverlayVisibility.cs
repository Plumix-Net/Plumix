using System;
using Plumix.Rendering;
using Plumix.Widgets;

// C#-only test infrastructure: mirrors the `skipOffstage: true` default of Flutter's widget finders.

namespace Plumix.Tests;

/// <summary>
/// Walks a render subtree the way Flutter's finders do by default: entries an <see cref="Overlay"/> keeps
/// alive through <c>maintainState</c> but does not lay out or paint are skipped, so a route hidden behind an
/// opaque route reads as absent.
/// </summary>
internal static class OverlayVisibility
{
    public static void VisitOnstage(RenderObject? root, Action<RenderObject> visitor)
    {
        if (root is null)
        {
            return;
        }

        visitor(root);
        root.VisitChildren(child =>
        {
            if (child.parentData is OverlayTheaterParentData { IsOnstage: false })
            {
                return;
            }

            VisitOnstage(child, visitor);
        });
    }

    public static TRenderObject? FindOnstage<TRenderObject>(
        RenderObject? root,
        Func<TRenderObject, bool>? predicate = null)
        where TRenderObject : RenderObject
    {
        TRenderObject? found = null;
        VisitOnstage(root, node =>
        {
            if (found is null && node is TRenderObject typed && (predicate is null || predicate(typed)))
            {
                found = typed;
            }
        });

        return found;
    }

    public static int CountOnstage<TRenderObject>(RenderObject? root) where TRenderObject : RenderObject
    {
        int count = 0;
        VisitOnstage(root, node =>
        {
            if (node is TRenderObject)
            {
                count += 1;
            }
        });

        return count;
    }
}
