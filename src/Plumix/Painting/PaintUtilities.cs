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
/// class, the way <c>Plumix.Foundation.Print</c> hosts <c>debugPrint</c>. Dart draws through a
/// <c>Canvas</c> with a save/translate/rotate transform; Plumix's <see cref="PaintingContext"/>
/// exposes no canvas transform stack, so the same points are computed in the render object's own
/// coordinate space and handed to <see cref="PaintingContext.DrawPath"/> directly.
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
        context.DrawPath(BuildZigZagPath(start, end, zigs, width), brush: null, pen: pen);
    }

    /// <summary>The path <see cref="PaintZigZag"/> strokes.</summary>
    /// <remarks>
    /// Dart builds this inside <c>paintZigZag</c> under a canvas translate/rotate. Plumix has no
    /// canvas transform stack, so the rotation is folded into the point math and the path is built
    /// separately, which also makes the geometry testable without a drawing backend.
    /// </remarks>
    internal static UI.Path BuildZigZagPath(Point start, Point end, int zigs, double width)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(zigs, 1);

        Point delta = end - start;
        double angle = Math.Atan2(delta.Y, delta.X);
        double cos = Math.Cos(angle);
        double sin = Math.Sin(angle);
        double length = Math.Sqrt((delta.X * delta.X) + (delta.Y * delta.Y));
        double spacing = length / (zigs * 2.0);

        var path = new UI.Path();
        Point rotated = Rotate(0.0, 0.0, cos, sin, start);
        path.MoveTo(rotated.X, rotated.Y);
        for (int index = 0; index < zigs; index += 1)
        {
            double x = ((index * 2.0) + 1.0) * spacing;
            double y = width * ((PositiveModulo(index, 2.0) * 2.0) - 1.0);
            rotated = Rotate(x, y, cos, sin, start);
            path.LineTo(rotated.X, rotated.Y);
        }

        rotated = Rotate(length, 0.0, cos, sin, start);
        path.LineTo(rotated.X, rotated.Y);
        return path;
    }

    private static Point Rotate(double x, double y, double cos, double sin, Point origin)
    {
        return new Point(origin.X + (x * cos) - (y * sin), origin.Y + (x * sin) + (y * cos));
    }

    private static double PositiveModulo(double value, double modulus) => ((value % modulus) + modulus) % modulus;
}
