using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;

namespace Plumix.Painting;

// Dart parity source: flutter/packages/flutter/lib/src/painting/paint_utilities.dart

/// <summary>
/// Free painting helpers from Flutter's `painting/paint_utilities.dart`.
/// </summary>
/// <remarks>
/// C# has no top-level functions, so Dart's library-level <c>paintZigZag</c> lives on this static
/// class, the way <c>Plumix.Foundation.Print</c> hosts <c>debugPrint</c>.
/// </remarks>
public static class PaintUtilities
{
    /// <summary>
    /// Draws a line between two points, which cuts diagonally back and forth across the line that
    /// connects the two points. The line crosses the direct line <c>zigs - 1</c> times.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>paintZigZag</c>. If <paramref name="width"/> is positive, the first zig is to
    /// the left of <paramref name="start"/> when facing <paramref name="end"/>; a negative width
    /// reverses the zigging polarity.
    /// </remarks>
    public static void PaintZigZag(
        PaintingContext context,
        IPen pen,
        Point start,
        Point end,
        int zigs,
        double width)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentOutOfRangeException.ThrowIfLessThan(zigs, 1);

        Point delta = end - start;
        context.Canvas.Save();
        context.Canvas.Translate(start.X, start.Y);
        context.Canvas.Rotate(Math.Atan2(delta.X, delta.Y));
        context.Canvas.DrawPath(
            BuildZigZagPath(Math.Sqrt((delta.X * delta.X) + (delta.Y * delta.Y)), zigs, width),
            brush: null,
            pen: pen);
        context.Canvas.Restore();
    }

    /// <summary>The path <see cref="PaintZigZag"/> strokes, in the rotated local space.</summary>
    /// <remarks>
    /// Flutter builds this inline in <c>paintZigZag</c>, after the canvas has been translated to
    /// <c>start</c> and rotated so that the line runs down the local y axis.
    /// </remarks>
    internal static UI.Path BuildZigZagPath(double length, int zigs, double width)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(zigs, 1);

        double spacing = length / (zigs * 2.0);
        var path = new UI.Path();
        path.MoveTo(0.0, 0.0);
        for (int index = 0; index < zigs; index += 1)
        {
            double x = PositiveModulo(index, 2.0) == 1.0 ? width : -width;
            path.LineTo(x, ((index * 2.0) + 1.0) * spacing);
            path.LineTo(0.0, ((index * 2.0) + 2.0) * spacing);
        }

        return path;
    }

    private static double PositiveModulo(double value, double modulus) => ((value % modulus) + modulus) % modulus;
}
