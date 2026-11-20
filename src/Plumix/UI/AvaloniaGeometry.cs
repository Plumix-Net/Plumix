namespace Plumix;

// C#-only infrastructure: no Dart parity source.
// Conversions between the framework geometry and Avalonia's at the drawing/host boundary. Values
// are copied; nothing allocates. Avalonia's Rect stores x/y/width/height, so an edge at infinity
// can come back as NaN.
internal static class AvaloniaGeometry
{
    extension(Offset offset)
    {
        public global::Avalonia.Point ToAvalonia() => new(offset.Dx, offset.Dy);
        public global::Avalonia.Vector ToAvaloniaVector() => new(offset.Dx, offset.Dy);
    }

    extension(Size size)
    {
        public global::Avalonia.Size ToAvalonia() => new(size.Width, size.Height);
    }

    extension(Rect rect)
    {
        public global::Avalonia.Rect ToAvalonia() => new(rect.Left, rect.Top, rect.Width, rect.Height);
    }

    extension(global::Avalonia.Point point)
    {
        public Offset ToPlumix() => new(point.X, point.Y);
    }

    extension(global::Avalonia.Vector vector)
    {
        public Offset ToPlumix() => new(vector.X, vector.Y);
    }

    extension(global::Avalonia.Size size)
    {
        public Size ToPlumix() => new(size.Width, size.Height);
    }

    extension(global::Avalonia.Rect rect)
    {
        public Rect ToPlumix() => Rect.FromLTWH(rect.X, rect.Y, rect.Width, rect.Height);
    }
}
