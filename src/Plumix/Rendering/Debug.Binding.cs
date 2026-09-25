using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/binding.dart
// Dart parity source: flutter/packages/flutter/lib/src/rendering/object.dart

namespace Plumix.Rendering;

public static partial class RenderingDebug
{
    /// <summary>Prints every registered render tree through <see cref="Print.DebugPrint"/>.</summary>
    /// <remarks>Flutter's top-level <c>debugDumpRenderTree</c>; no layout or paint is flushed.</remarks>
    public static void DebugDumpRenderTree() => Print.DebugPrint(DebugCollectRenderTrees());

    internal static string DebugCollectRenderTrees()
    {
        IEnumerable<RenderView> renderViews = RendererBinding.Instance.RenderViews;
        if (!renderViews.Any())
        {
            return "No render tree root was added to the binding.";
        }

        return string.Join("\n\n", renderViews.Select(static view => view.ToStringDeep()));
    }

    /// <summary>Prints every registered view's retained layer tree through <see cref="Print.DebugPrint"/>.</summary>
    /// <remarks>Flutter's top-level <c>debugDumpLayerTree</c>; unavailable layers are reported per view.</remarks>
    public static void DebugDumpLayerTree() => Print.DebugPrint(DebugCollectLayerTrees());

    internal static string DebugCollectLayerTrees()
    {
        IEnumerable<RenderView> renderViews = RendererBinding.Instance.RenderViews;
        if (!renderViews.Any())
        {
            return "No render tree root was added to the binding.";
        }

        return string.Join("\n\n", renderViews.Select(static view =>
        {
            // Flutter's debugLayer getter exposes the retained layer only inside assert().
            Layer? layer = Constants.KDebugMode ? view.DebugLayer : null;
            return layer?.ToStringDeep() ?? $"Layer tree unavailable for {view}.";
        }));
    }

    /// <summary>
    /// Prints the semantics tree of every registered view through <see cref="Print.DebugPrint"/>, with
    /// each node's children in <paramref name="childOrder"/>.
    /// </summary>
    /// <remarks>
    /// Flutter's top-level <c>debugDumpSemanticsTree</c>: a view that has produced no semantics prints
    /// a note instead, and the first such note explains how semantics get enabled.
    /// </remarks>
    public static void DebugDumpSemanticsTree(
        DebugSemanticsDumpOrder childOrder = DebugSemanticsDumpOrder.TraversalOrder)
    {
        Print.DebugPrint(DebugCollectSemanticsTrees(childOrder));
    }

    internal static string DebugCollectSemanticsTrees(DebugSemanticsDumpOrder childOrder)
    {
        IEnumerable<RenderView> renderViews = RendererBinding.Instance.RenderViews;
        if (!renderViews.Any())
        {
            return "No render tree root was added to the binding.";
        }

        const string Explanation =
            "For performance reasons, the framework only generates semantics when asked to do so by the platform.\n"
            + "Usually, platforms only ask for semantics when assistive technologies (like screen readers) are "
            + "running.\n"
            + "To generate semantics, try turning on an assistive technology (like VoiceOver or TalkBack) on your "
            + "device.";
        List<string> trees = [];
        bool printedExplanation = false;
        foreach (RenderView renderView in renderViews)
        {
            string? tree = renderView.DebugSemantics?.ToStringDeep(childOrder);
            if (tree is not null)
            {
                trees.Add(tree);
            }
            else
            {
                string message = $"Semantics not generated for {renderView}.";
                if (!printedExplanation)
                {
                    printedExplanation = true;
                    message = $"{message}\n{Explanation}";
                }

                trees.Add(message);
            }
        }

        return string.Join("\n\n", trees);
    }

    /// <summary>Prints the pipeline owner tree through <see cref="Print.DebugPrint"/>.</summary>
    /// <remarks>Flutter's top-level <c>debugDumpPipelineOwnerTree</c>.</remarks>
    public static void DebugDumpPipelineOwnerTree()
    {
        Print.DebugPrint(RendererBinding.Instance.RootPipelineOwner.ToStringDeep());
    }

    /// <summary>Prints the render-object semantics-fragment trees, not the final platform semantics nodes.</summary>
    /// <remarks>Flutter's top-level <c>debugDumpRenderObjectSemanticsTree</c>.</remarks>
    public static void DebugDumpRenderObjectSemanticsTree()
    {
        IEnumerable<RenderView> renderViews = RendererBinding.Instance.RenderViews;
        if (!renderViews.Any())
        {
            Print.DebugPrint("No render tree root was added to the binding.");
            return;
        }

        Print.DebugPrint(string.Join("\n\n", renderViews.Select(static view => view.Semantics.ToStringDeep())));
    }
}
