using Plumix.UI;

// C#-only engine adapter: Scene.toImage/toImageSync over the Avalonia scene rasterizer, and fragment
// programs compiled into Skia runtime effects (IRenderBackend, src/Plumix/UI/RenderBackend.cs).

namespace Plumix;

/// <summary>Rasterizes scenes into Avalonia bitmaps.</summary>
internal sealed class AvaloniaRenderBackend : IRenderBackend
{
    internal static readonly AvaloniaRenderBackend Instance = new();

    private AvaloniaRenderBackend()
    {
    }

    public RasterImage RasterizeScene(ContainerFlowLayer root, int width, int height) =>
        AvaloniaImages.Wrap(SceneRasterizer.RasterizeToImage(root, width, height));

    public void CompileFragmentProgram(RuntimeFragmentProgram program) => SkiaShaders.CompileProgram(program);
}
