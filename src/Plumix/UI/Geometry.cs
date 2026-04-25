using Avalonia;

// Dart parity source (reference): flutter/engine/src/flutter/lib/ui/geometry.dart (engine parity, approximate)

namespace Plumix.UI;

public static class SizeExtensions
{
    extension(Size size)
    {
        public Size Flipped => new(size.Height, size.Width);

        public bool IsEmpty => size.Width <= 0.0 || size.Height <= 0.0;
    }
}
