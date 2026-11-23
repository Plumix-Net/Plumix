using NImpeller;
using Plumix.UI;

namespace Plumix.Impeller;

// C#-only infrastructure: no Dart parity source. The Impeller implementation of the core's render seam
// (UI/RenderBackend.cs) for the stage 4 prototype. Impeller's C API cannot read pixels back, so Scene.toImage goes
// through the host's readback (ReadPixels; GLES: an offscreen framebuffer and glReadPixels); fragment programs run when
// an impellerc bundle of their source is embedded (ImpellerShaders) and draw nothing otherwise.

/// <summary>Installs the Impeller backend and holds the host's Impeller context.</summary>
internal static class ImpellerBackend
{
    /// <summary>The context of the surface the host renders to; null until the first surface exists.</summary>
    internal static ImpellerContext? Context { get; set; }

    /// <summary>
    /// Runs GPU-resource work (texture uploads) on the thread that owns <see cref="Context"/>: Impeller's GLES
    /// reactor performs a thread's queued GL operations only when that thread reacts, so a texture created on
    /// the UI thread would never upload. A host with a raster thread replaces this; by default it runs inline.
    /// </summary>
    internal static Func<Func<ImpellerTexture?>, ImpellerTexture?> OnContextThread { get; set; } =
        create => create();

    /// <summary>
    /// Draws a display list offscreen and returns its premultiplied RGBA8888 rows, top to bottom; null when it
    /// cannot. Impeller's C API has no readback, so the host provides one for its graphics API (GLES: an offscreen
    /// framebuffer and glReadPixels). Without it Scene.toImage is unavailable.
    /// </summary>
    internal static Func<ImpellerDisplayList, int, int, byte[]?>? ReadPixels { get; set; }

    /// <summary>Installs the Impeller render backend into the core.</summary>
    internal static void Install()
    {
        RenderBackend.Current = RenderBackendImpl.Instance;
    }

    /// <summary>
    /// Installs the HarfBuzz/glyph-run text engine, with the platform's default font loaded from the first
    /// of <paramref name="defaultFontPaths"/> that exists.
    /// </summary>
    internal static ImpellerTextShaper InstallText(params string[] defaultFontPaths)
    {
        var shaper = new ImpellerTextShaper();
        shaper.LoadDefault(defaultFontPaths);
        TextBackend.Current = new ImpellerTextBackend(shaper);
        return shaper;
    }

    private sealed class RenderBackendImpl : IRenderBackend
    {
        internal static readonly RenderBackendImpl Instance = new();

        public bool SupportsBackdropFilters => true;

        public RasterImage RasterizeScene(ContainerFlowLayer root, int width, int height)
        {
            if (ReadPixels is not { } readPixels)
            {
                throw new NotSupportedException(
                    "Impeller's C API cannot read pixels back and the host provides no readback: "
                    + "Scene.toImage is unavailable.");
            }

            using ImpellerDisplayList displayList = ImpellerLayerPainter.Build(root, width, height);
            byte[] pixels = readPixels(displayList, width, height)
                ?? throw new InvalidOperationException("The host could not read the scene's pixels back.");
            return ImpellerImage.Wrap(pixels, width, height);
        }

        public void CompileFragmentProgram(RuntimeFragmentProgram program)
        {
            program.BackendState = ImpellerShaders.Load(program);
            if (program.BackendState is null)
            {
                ImpellerPaints.Unsupported("fragment programs without an impellerc bundle");
            }
        }
    }
}
