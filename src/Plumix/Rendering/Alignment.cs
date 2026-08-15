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

    /// <summary>The offset that is this fraction within a rect of the given size.</summary>
    public Point AlongSize(Size other)
    {
        double centerX = other.Width / 2.0;
        double centerY = other.Height / 2.0;
        return new Point(centerX + (X * centerX), centerY + (Y * centerY));
    }

    /// <summary>The point that is this fraction within the given rect.</summary>
    public Point WithinRect(Rect rect)
    {
        double halfWidth = rect.Width / 2.0;
        double halfHeight = rect.Height / 2.0;
        return new Point(
            rect.Left + halfWidth + (X * halfWidth),
            rect.Top + halfHeight + (Y * halfHeight));
    }

    public override string ToString()
    {
        return (X, Y) switch
        {
            (-1.0, -1.0) => "Alignment.topLeft",
            (0.0, -1.0) => "Alignment.topCenter",
            (1.0, -1.0) => "Alignment.topRight",
            (-1.0, 0.0) => "Alignment.centerLeft",
            (0.0, 0.0) => "Alignment.center",
            (1.0, 0.0) => "Alignment.centerRight",
            (-1.0, 1.0) => "Alignment.bottomLeft",
            (0.0, 1.0) => "Alignment.bottomCenter",
            (1.0, 1.0) => "Alignment.bottomRight",
            _ => $"Alignment({DartFormat.Fixed(X)}, {DartFormat.Fixed(Y)})",
        };
    }

    /// <summary>A rect of the given size, aligned within <paramref name="rect"/> by this alignment.</summary>
    public Rect Inscribe(Size size, Rect rect)
    {
        double halfWidthDelta = (rect.Width - size.Width) / 2.0;
        double halfHeightDelta = (rect.Height - size.Height) / 2.0;
        return new Rect(
            rect.Left + halfWidthDelta + (X * halfWidthDelta),
            rect.Top + halfHeightDelta + (Y * halfHeightDelta),
            size.Width,
            size.Height);
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
