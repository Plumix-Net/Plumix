using Plumix.Rendering;
using Plumix.UI;

// C#-only test infrastructure; no Dart parity source.

namespace Plumix.Tests;

/// <summary>
/// Reads a laid-out box's baseline from outside its parent's layout or paint.
/// </summary>
/// <remarks>
/// Flutter's <c>RenderBox.getDistanceToBaseline</c> asserts that only the parent calls it, during that
/// parent's layout or paint (<c>flex_test.dart</c>: "We can't call the getDistanceToBaseline method
/// directly"). The framework's own debug check (<c>_debugVerifyDryBaselines</c>) reads the real baseline
/// from paint by raising <see cref="RenderObject.DebugCheckingIntrinsics"/>; this does the same for a
/// test, which also bypasses the baseline cache.
/// </remarks>
internal static class BaselineProbe
{
    public static double? ProbeBaseline(this RenderBox box, TextBaseline baseline, bool onlyReal = true)
    {
        bool previous = RenderObject.DebugCheckingIntrinsics;
        RenderObject.DebugCheckingIntrinsics = true;
        try
        {
            return box.GetDistanceToBaseline(baseline, onlyReal);
        }
        finally
        {
            RenderObject.DebugCheckingIntrinsics = previous;
        }
    }
}
