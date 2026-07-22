using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/lookup_boundary.dart

public sealed class LookupBoundary : InheritedWidget
{
    public LookupBoundary(Widget child, Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    public static T? DependOnInheritedWidgetOfExactType<T>(BuildContext context, object? aspect = null)
        where T : InheritedWidget
    {
        _ = context.DependOnInherited<LookupBoundary>();
        InheritedElement? candidate = GetElementForInheritedWidgetOfExactType<T>(context);
        if (candidate == null)
        {
            return null;
        }

        return (T)context.Owner.DependOnInheritedElement(candidate, aspect);
    }

    public static T? GetInheritedWidgetOfExactType<T>(BuildContext context, object? aspect = null)
        where T : InheritedWidget
    {
        return GetElementForInheritedWidgetOfExactType<T>(context)?.Widget as T;
    }

    public static InheritedElement? GetElementForInheritedWidgetOfExactType<T>(BuildContext context)
        where T : InheritedWidget
    {
        InheritedElement? candidate = context.GetElementForInheritedWidgetOfExactType<T>();
        if (candidate == null)
        {
            return null;
        }

        InheritedElement? boundary = context.GetElementForInheritedWidgetOfExactType<LookupBoundary>();
        return boundary != null && boundary.Depth > candidate.Depth ? null : candidate;
    }

    public static T? FindAncestorWidgetOfExactType<T>(BuildContext context) where T : Widget
    {
        T? result = null;
        context.VisitAncestorElements(ancestor =>
        {
            if (ancestor.Widget.GetType() == typeof(T))
            {
                result = (T)ancestor.Widget;
                return false;
            }

            return ancestor.Widget.GetType() != typeof(LookupBoundary);
        });
        return result;
    }

    public static T? FindAncestorStateOfType<T>(BuildContext context) where T : State
    {
        T? result = null;
        context.VisitAncestorElements(ancestor =>
        {
            if (ancestor is StatefulElement { State: T state })
            {
                result = state;
                return false;
            }

            return ancestor.Widget.GetType() != typeof(LookupBoundary);
        });
        return result;
    }

    public static T? FindRootAncestorStateOfType<T>(BuildContext context) where T : State
    {
        T? result = null;
        context.VisitAncestorElements(ancestor =>
        {
            if (ancestor is StatefulElement { State: T state })
            {
                result = state;
            }

            return ancestor.Widget.GetType() != typeof(LookupBoundary);
        });
        return result;
    }

    public static T? FindAncestorRenderObjectOfType<T>(BuildContext context) where T : RenderObject
    {
        T? result = null;
        context.VisitAncestorElements(ancestor =>
        {
            if (ancestor is RenderObjectElement { RenderObject: T renderObject })
            {
                result = renderObject;
                return false;
            }

            return ancestor.Widget.GetType() != typeof(LookupBoundary);
        });
        return result;
    }

    public static void VisitAncestorElements(BuildContext context, Func<Element, bool> visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        context.VisitAncestorElements(ancestor =>
            visitor(ancestor) && ancestor.Widget.GetType() != typeof(LookupBoundary));
    }

    public static void VisitChildElements(BuildContext context, Action<Element> visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        context.VisitChildElements(child =>
        {
            if (child.Widget.GetType() != typeof(LookupBoundary))
            {
                visitor(child);
            }
        });
    }

    public static bool DebugIsHidingAncestorWidgetOfExactType<T>(BuildContext context) where T : Widget
    {
        bool hiddenByBoundary = false;
        bool ancestorFound = false;
        context.VisitAncestorElements(ancestor =>
        {
            if (ancestor.Widget.GetType() == typeof(T))
            {
                ancestorFound = true;
                return false;
            }

            hiddenByBoundary |= ancestor.Widget.GetType() == typeof(LookupBoundary);
            return true;
        });
        return ancestorFound && hiddenByBoundary;
    }

    public static bool DebugIsHidingAncestorStateOfType<T>(BuildContext context) where T : State
    {
        bool hiddenByBoundary = false;
        bool ancestorFound = false;
        context.VisitAncestorElements(ancestor =>
        {
            if (ancestor is StatefulElement { State: T })
            {
                ancestorFound = true;
                return false;
            }

            hiddenByBoundary |= ancestor.Widget.GetType() == typeof(LookupBoundary);
            return true;
        });
        return ancestorFound && hiddenByBoundary;
    }

    public static bool DebugIsHidingAncestorRenderObjectOfType<T>(BuildContext context) where T : RenderObject
    {
        bool hiddenByBoundary = false;
        bool ancestorFound = false;
        context.VisitAncestorElements(ancestor =>
        {
            if (ancestor is RenderObjectElement { RenderObject: T })
            {
                ancestorFound = true;
                return false;
            }

            hiddenByBoundary |= ancestor.Widget.GetType() == typeof(LookupBoundary);
            return true;
        });
        return ancestorFound && hiddenByBoundary;
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) => false;
}
