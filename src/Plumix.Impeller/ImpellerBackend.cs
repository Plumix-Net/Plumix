using NImpeller;
using Plumix.UI;

namespace Plumix.Impeller;

// C#-only infrastructure: no Dart parity source. The Impeller implementation of the core's render seam
// (UI/RenderBackend.cs) for the stage 4 prototype. Impeller's C API cannot read pixels back, so
// Scene.toImage has no implementation (docs/product/avalonia-free-core.md, risks); fragment programs are
// accepted and drawn without their effect until an impellerc build of the shaders exists.

/// <summary>Installs the Impeller backend and holds the host's Impeller context.</summary>
internal static class ImpellerBackend
{
    /// <summary>The context of the surface the host renders to; null until the first surface exists.</summary>
    internal static ImpellerContext? Context { get; set; }

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

        public RasterImage RasterizeScene(ContainerFlowLayer root, int width, int height) =>
            throw new NotSupportedException("Impeller's C API cannot read pixels back: Scene.toImage is unavailable.");

        public void CompileFragmentProgram(RuntimeFragmentProgram program) =>
            ImpellerPaints.Unsupported("fragment programs (SkSL)");
    }
}
