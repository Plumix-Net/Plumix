using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/binding.dart
// Dart parity source: flutter/packages/flutter/lib/src/rendering/object.dart

namespace Plumix.Rendering;

public static partial class RenderingDebug
{
    /// <summary>Prints every registered render tree through <see cref="Print.DebugPrint"/>.</summary>
    /// <remarks>Flutter's top-level <c>debugDumpRenderTree</c>; no layout or paint is flushed.</remarks>
    public static void DebugDumpRenderTree() => Print.DebugPrint(DebugCollectRenderTrees());

    private static string DebugCollectRenderTrees()
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

    private static string DebugCollectLayerTrees()
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
