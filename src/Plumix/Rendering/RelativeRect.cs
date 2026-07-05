using Avalonia;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/relative_rect.dart

public readonly record struct RelativeRect(double Left, double Top, double Right, double Bottom)
{
    public static RelativeRect FromRect(Rect rect, Rect container) => new(
        rect.Left - container.Left,
        rect.Top - container.Top,
        container.Right - rect.Right,
        container.Bottom - rect.Bottom);

    public static RelativeRect FromSize(Rect rect, Size containerSize) =>
        FromRect(rect, new Rect(containerSize));

    public Rect ToRect(Rect container) => new(
        container.Left + Left,
        container.Top + Top,
        Math.Max(0, container.Width - Left - Right),
        Math.Max(0, container.Height - Top - Bottom));
}
