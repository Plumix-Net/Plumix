using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Plumix.UI;

// C#-only backend bridge: draw framework native shaders through Avalonia's supported Skia lease.
// Geometry clips remain Avalonia-owned; the shader uses absolute canvas coordinates and local matrix.
internal static class ShaderDrawing
{
    internal static void DrawRectangle(DrawingContext context, IBrush? brush, IPen? pen, Rect rect) =>
        DrawRectangle(context, brush, pen, new RoundedRect(rect));

    internal static void DrawRectangle(
        DrawingContext context, IBrush? brush, IPen? pen, RoundedRect rect, BoxShadows shadows = default)
    {
        if (brush is not INativeShader && pen?.Brush is not INativeShader)
        {
            context.DrawRectangle(brush, pen, rect, shadows);
            return;
        }

        if (shadows.Count > 0)
        {
            context.DrawRectangle(null, null, rect, shadows);
        }

        var path = new Path();
        path.AddRRect(RRect.FromRectAndCorners(rect.Rect,
            new Rendering.Radius(rect.RadiiTopLeft.X, rect.RadiiTopLeft.Y),
            new Rendering.Radius(rect.RadiiTopRight.X, rect.RadiiTopRight.Y),
            new Rendering.Radius(rect.RadiiBottomRight.X, rect.RadiiBottomRight.Y),
            new Rendering.Radius(rect.RadiiBottomLeft.X, rect.RadiiBottomLeft.Y)));
        DrawGeometry(context, brush, pen, path.ToGeometry());
    }

    internal static void DrawGeometry(DrawingContext context, IBrush? brush, IPen? pen, Geometry geometry)
    {
        if (brush is not INativeShader && pen?.Brush is not INativeShader)
        {
            context.DrawGeometry(brush, pen, geometry);
            return;
        }

        if (brush is INativeShader shader)
        {
            using DrawingContext.PushedState clip = context.PushGeometryClip(geometry);
            Fill(context, shader, geometry.Bounds);
        }
        else if (brush is not null)
        {
            context.DrawGeometry(brush, null, geometry);
        }

        if (pen?.Brush is INativeShader stroke)
        {
            Geometry outline = geometry.GetWidenedGeometry(pen);
            using DrawingContext.PushedState clip = context.PushGeometryClip(outline);
            Fill(context, stroke, outline.Bounds);
        }
        else if (pen is not null)
        {
            context.DrawGeometry(null, pen, geometry);
        }
    }

    internal static void DrawEllipse(
        DrawingContext context, IBrush? brush, IPen? pen, Point center, double radiusX, double radiusY)
    {
        if (brush is not INativeShader && pen?.Brush is not INativeShader)
        {
            context.DrawEllipse(brush, pen, center, radiusX, radiusY);
            return;
        }

        DrawGeometry(context, brush, pen, new EllipseGeometry(new Rect(
            center.X - radiusX, center.Y - radiusY, radiusX * 2.0, radiusY * 2.0)));
    }

    internal static void DrawLine(DrawingContext context, IPen pen, Point start, Point end)
    {
        if (pen.Brush is INativeShader shader)
        {
            var path = new Path();
            path.MoveTo(start.X, start.Y);
            path.LineTo(end.X, end.Y);
            var paint = new Paint
            {
                Shader = (IBrush)shader,
                Style = PaintingStyle.Stroke,
                StrokeWidth = pen.Thickness,
                StrokeCap = pen.LineCap switch
                {
                    PenLineCap.Round => StrokeCap.Round,
                    PenLineCap.Square => StrokeCap.Square,
                    _ => StrokeCap.Butt,
                },
            };
            DrawPath(context, shader, path, paint);
        }
        else
        {
            context.DrawLine(pen, start, end);
        }
    }

    internal static void Fill(
        DrawingContext context, INativeShader shader, Rect bounds, Paint? source = null, bool fillCanvas = false) =>
        context.Custom(new NativeShaderDraw(shader, bounds, source: source, fillCanvas: fillCanvas));

    internal static void DrawPath(DrawingContext context, INativeShader shader, Path path, Paint paint) =>
        context.Custom(new NativeShaderDraw(shader, path.GetBounds().Inflate(
            paint.Style == PaintingStyle.Stroke ? Math.Max(1.0, paint.StrokeWidth * paint.StrokeMiterLimit) : 0.0),
            path, new Paint(paint)));

    private sealed class NativeShaderDraw(
        INativeShader shader, Rect bounds, Path? path = null, Paint? source = null, bool fillCanvas = false)
        : ICustomDrawOperation
    {
        public Rect Bounds => bounds;
        public bool HitTest(Point point) => false;
        public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);
        public void Dispose() { }

        public void Render(ImmediateDrawingContext context)
        {
            ISkiaSharpApiLeaseFeature feature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>()
                ?? throw new NotSupportedException("Framework native shaders require the Skia drawing backend.");
            using ISkiaSharpApiLease lease = feature.Lease();
            using SKShader? native = shader.CreateNativeShader();
            // A rejected/empty native shader must never become the paint's default black fill.
            if (native is null)
            {
                return;
            }

            using var paint = new SKPaint
            {
                Shader = native,
                Color = SKColors.White.WithAlpha((byte)Math.Round(
                    255.0 * lease.CurrentOpacity * (source?.Color.A ?? 1.0))),
                IsAntialias = source?.IsAntiAlias ?? true,
                IsDither = true,
                Style = source?.Style == PaintingStyle.Stroke ? SKPaintStyle.Stroke : SKPaintStyle.Fill,
                StrokeWidth = Path.Narrow(source?.StrokeWidth ?? 0.0),
                StrokeMiter = Path.Narrow(source?.StrokeMiterLimit ?? 4.0),
                StrokeCap = source?.StrokeCap switch
                {
                    StrokeCap.Round => SKStrokeCap.Round,
                    StrokeCap.Square => SKStrokeCap.Square,
                    _ => SKStrokeCap.Butt,
                },
                StrokeJoin = source?.StrokeJoin switch
                {
                    StrokeJoin.Round => SKStrokeJoin.Round,
                    StrokeJoin.Bevel => SKStrokeJoin.Bevel,
                    _ => SKStrokeJoin.Miter,
                },
            };
            if (fillCanvas)
            {
                lease.SkCanvas.DrawPaint(paint);
            }
            else if (path is null)
            {
                lease.SkCanvas.DrawRect(new SKRect((float)bounds.Left, (float)bounds.Top,
                    (float)bounds.Right, (float)bounds.Bottom), paint);
            }
            else
            {
                using SKPath nativePath = path.CloneNativePath();
                lease.SkCanvas.DrawPath(nativePath, paint);
            }
        }
    }
}
