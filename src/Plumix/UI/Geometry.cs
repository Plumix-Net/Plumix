using Avalonia;

// Dart parity source (reference): flutter/engine/src/flutter/lib/ui/geometry.dart (engine parity, approximate)

namespace Plumix.UI;

public static class SizeExtensions
{
    extension(Size size)
    {
        public Size Flipped => new(size.Height, size.Width);

        public bool IsEmpty => size.Width <= 0.0 || size.Height <= 0.0;

        /// <summary>
        /// Dart's <c>Size.contains</c>: whether the offset lies between the size's origin
        /// (inclusive) and its bottom-right corner (exclusive).
        /// </summary>
        public bool Contains(Point offset) =>
            offset.X >= 0.0 && offset.X < size.Width && offset.Y >= 0.0 && offset.Y < size.Height;
    }
}

public static class RectExtensions
{
    extension(Rect rect)
    {
        /// <summary>
        /// Dart's <c>Rect.contains</c>: whether the point lies inside the rectangle, where the left
        /// and top edges are inside and the right and bottom edges are outside. Avalonia's
        /// <c>Rect.Contains</c> treats all four edges as inside.
        /// </summary>
        public bool ContainsHalfOpen(Point offset) =>
            offset.X >= rect.Left && offset.X < rect.Right && offset.Y >= rect.Top && offset.Y < rect.Bottom;
    }
}
