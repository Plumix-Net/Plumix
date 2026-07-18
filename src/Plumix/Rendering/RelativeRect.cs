using Avalonia;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/relative_rect.dart

public readonly record struct RelativeRect(double Left, double Top, double Right, double Bottom)
{
    public static RelativeRect Fill => new(0.0, 0.0, 0.0, 0.0);

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

    public static RelativeRect Lerp(RelativeRect? a, RelativeRect? b, double t)
    {
        RelativeRect begin = a ?? Fill;
        RelativeRect end = b ?? Fill;
        return new RelativeRect(
            begin.Left + ((end.Left - begin.Left) * t),
            begin.Top + ((end.Top - begin.Top) * t),
            begin.Right + ((end.Right - begin.Right) * t),
            begin.Bottom + ((end.Bottom - begin.Bottom) * t));
    }
}
