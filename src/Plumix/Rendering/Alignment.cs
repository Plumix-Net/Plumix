using Avalonia;

// Dart parity source (reference): flutter/packages/flutter/lib/src/painting/alignment.dart (approximate)

namespace Plumix.Rendering;

public readonly record struct Alignment(double X, double Y)
{
    public static Alignment TopLeft => new(-1, -1);
    public static Alignment TopCenter => new(0, -1);
    public static Alignment TopRight => new(1, -1);
    public static Alignment CenterLeft => new(-1, 0);
    public static Alignment Center => new(0, 0);
    public static Alignment CenterRight => new(1, 0);
    public static Alignment BottomLeft => new(-1, 1);
    public static Alignment BottomCenter => new(0, 1);
    public static Alignment BottomRight => new(1, 1);

    public Point AlongOffset(Size parentSize, Size childSize)
    {
        double freeWidth = parentSize.Width - childSize.Width;
        double freeHeight = parentSize.Height - childSize.Height;
        return new Point(
            freeWidth * (X + 1) / 2.0,
            freeHeight * (Y + 1) / 2.0);
    }
}

public readonly record struct TextAlignVertical
{
    public TextAlignVertical(double y)
    {
        if (y < -1.0 || y > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(y), y, "TextAlignVertical.y must be between -1.0 and 1.0.");
        }

        Y = y;
    }

    public double Y { get; }

    public static TextAlignVertical Top => new(-1.0);

    public static TextAlignVertical Center => new(0.0);

    public static TextAlignVertical Bottom => new(1.0);
}
