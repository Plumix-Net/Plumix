// C#-only infrastructure; no Dart parity source. The engine rasterizes scenes and pictures itself; Plumix
// asks the host's drawing backend through this seam, so the framework names no host type
// (docs/product avalonia-free-core.md, stage 3). Plumix.Avalonia installs the Avalonia/Skia rasterizer
// (AvaloniaRenderBackend over SceneRasterizer) and its SkSL compiler (SkiaShaders.CompileProgram).

namespace Plumix.UI;

/// <summary>The drawing backend's scene rasterizer and fragment-program compiler.</summary>
internal interface IRenderBackend
{
    /// <summary>
    /// Rasterizes the layer tree <paramref name="root"/> into a new <paramref name="width"/> by
    /// <paramref name="height"/> image, one scene pixel per image pixel: the engine's
    /// <c>Scene::toImageSync</c>.
    /// </summary>
    RasterImage RasterizeScene(ContainerFlowLayer root, int width, int height);

    /// <summary>
    /// Compiles <paramref name="program"/> into the backend's runtime effect, kept in its
    /// <see cref="RuntimeFragmentProgram.BackendState"/>; throws on compilation errors and on uniforms the
    /// backend does not support, as the engine's <c>FragmentProgram</c> does when it is created.
    /// </summary>
    void CompileFragmentProgram(RuntimeFragmentProgram program);
}

/// <summary>The render backend the host installed.</summary>
internal static class RenderBackend
{
    internal static IRenderBackend Current { get; set; } = Missing.Instance;

    private sealed class Missing : IRenderBackend
    {
        internal static readonly Missing Instance = new();

        public RasterImage RasterizeScene(ContainerFlowLayer root, int width, int height) => throw NoHost();

        public void CompileFragmentProgram(RuntimeFragmentProgram program) => throw NoHost();

        private static InvalidOperationException NoHost() => new(
            "No Plumix host is running. Start the app through a host package such as Plumix.Avalonia "
            + "(PlumixApplication, WidgetHost).");
    }
}
