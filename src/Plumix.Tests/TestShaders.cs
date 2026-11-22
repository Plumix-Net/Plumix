using Plumix.UI;
using TileMode = Plumix.Rendering.TileMode;

// C#-only test infrastructure: no Dart parity source.

namespace Plumix.Tests;

/// <summary>Shaders for tests that only need some shader, not a particular gradient.</summary>
internal static class TestShaders
{
    /// <summary>A shader that paints <paramref name="color"/> everywhere.</summary>
    internal static Shader Solid(Color color) =>
        GradientShader.Linear(Offset.Zero, new Offset(1, 0), [color, color], [0.0, 1.0], TileMode.Clamp, null);
}
