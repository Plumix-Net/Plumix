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
