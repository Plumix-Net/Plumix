using Avalonia;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Tests;

// C#-only test infrastructure: reads the ink a render tree's Materials carry, the way Flutter's tests
// read `debugInkFeatures` and paint calls. InkResponse/InkWell ink lives on the nearest Material as
// InkFeatures (material_ui/lib/src/ink_well.dart), not on a render object below the response.
internal static class InkFeatureProbe
{
    /// <summary>Every Material ink controller below <paramref name="root"/>, in tree order.</summary>
    public static IReadOnlyList<RenderInkFeatures> Controllers(RenderObject? root)
    {
        var result = new List<RenderInkFeatures>();
        void Visit(RenderObject node)
        {
            if (node is RenderInkFeatures features)
            {
                result.Add(features);
            }

            node.VisitChildren(Visit);
        }

        if (root is not null)
        {
            Visit(root);
        }

        return result;
    }

    /// <summary>Every ink feature on the Materials below <paramref name="root"/>, in paint order.</summary>
    public static IReadOnlyList<InkFeature> Features(RenderObject? root) =>
        Controllers(root).SelectMany(controller => controller.DebugInkFeatures ?? []).ToList();

    /// <summary>The ink decorations (<see cref="Ink"/>) below <paramref name="root"/>.</summary>
    public static IReadOnlyList<InkDecoration> Decorations(RenderObject? root) =>
        Features(root).OfType<InkDecoration>().ToList();

    /// <summary>The splashes (every interactive feature that is not a highlight).</summary>
    public static IReadOnlyList<InteractiveInkFeature> Splashes(RenderObject? root) =>
        Features(root).OfType<InteractiveInkFeature>().Where(feature => feature is not InkHighlight).ToList();

    /// <summary>The highlights.</summary>
    public static IReadOnlyList<InkHighlight> Highlights(RenderObject? root) =>
        Features(root).OfType<InkHighlight>().ToList();

    /// <summary>The colour of the most recently added active highlight, or null when none is active.</summary>
    public static Color? HighlightColor(RenderObject? root) =>
        Highlights(root).LastOrDefault(highlight => highlight.Active)?.Color;

    /// <summary>The colour of the most recently added splash, or null when there is none.</summary>
    public static Color? SplashColor(RenderObject? root) => Splashes(root).LastOrDefault()?.Color;

    /// <summary>
    /// The ink responses below <paramref name="root"/> with their reference boxes, found through each
    /// render object's <see cref="DebugCreator"/> element (debug builds only).
    /// </summary>
    public static IReadOnlyList<InkResponseProbe> Responses(RenderObject? root)
    {
        var result = new List<InkResponseProbe>();
        var seen = new HashSet<RenderObject>();
        void Visit(RenderObject node)
        {
            if (node is RenderBox box && node.DebugCreator is DebugCreator creator && seen.Add(node))
            {
                Element? owner = null;
                creator.Element.VisitAncestorElements(element =>
                {
                    if (element.Widget is InkResponseStateWidget)
                    {
                        owner = element;
                        return false;
                    }

                    return true;
                });
                if (owner is not null
                    && ReferenceEquals(owner.FindRenderObject(), box)
                    && result.All(probe => !ReferenceEquals(probe.ReferenceBox, box)))
                {
                    result.Add(new InkResponseProbe((InkResponseStateWidget)owner.Widget, box));
                }
            }

            node.VisitChildren(Visit);
        }

        if (root is not null)
        {
            Visit(root);
        }

        return result;
    }
}

/// <summary>An ink response and the render box its ink is measured against.</summary>
internal sealed record InkResponseProbe(InkResponseStateWidget Widget, RenderBox ReferenceBox)
{
    /// <summary>The rect the response's highlight paints (its rect callback, else the reference box).</summary>
    public Rect ResolvedInkRect =>
        Widget.GetRectCallback?.Invoke(ReferenceBox)?.Invoke() ?? new Rect(ReferenceBox.Size);

    public Size Size => ReferenceBox.Size;
}

/// <summary>
/// The Material ancestor Flutter's tests get from <c>MaterialApp</c>/<c>Scaffold</c>: ink responses assert
/// <c>debugCheckHasMaterial</c>. A transparency material adds no surface of its own.
/// </summary>
internal static class MaterialHost
{
    public static Widget Transparent(Widget child) =>
        new Plumix.Material.Material(type: MaterialType.Transparency, child: child);
}
