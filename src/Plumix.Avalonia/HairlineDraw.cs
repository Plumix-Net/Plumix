using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Plumix.UI;

// C#-only backend bridge (Plumix.Avalonia, under CanvasPlayback): Skia's zero-width stroke remains one
// device pixel under every transform.
internal sealed class HairlineDraw(Offset start, Offset end, Color color, bool isAntiAlias, StrokeCap cap)
    : ICustomDrawOperation
{
    public global::Avalonia.Rect Bounds => Rect.FromLTRB(start.Dx, start.Dy, end.Dx, end.Dy).Inflate(1.0).ToAvalonia();

    public bool HitTest(global::Avalonia.Point point) => false;

    public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);

    public void Dispose()
    {
    }

    public void Render(ImmediateDrawingContext context)
    {
        ISkiaSharpApiLeaseFeature feature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>()
            ?? throw new NotSupportedException("Device-pixel hairlines require the Skia drawing backend.");
        using ISkiaSharpApiLease lease = feature.Lease();
        using var paint = new SKPaint
        {
            Color = new SKColor(color.ToARGB32()),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 0.0f,
            IsAntialias = isAntiAlias,
            StrokeCap = cap switch
            {
                StrokeCap.Round => SKStrokeCap.Round,
                StrokeCap.Square => SKStrokeCap.Square,
                _ => SKStrokeCap.Butt,
            },
        };
        lease.SkCanvas.DrawLine((float)start.Dx, (float)start.Dy, (float)end.Dx, (float)end.Dy, paint);
    }
}
