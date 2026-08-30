using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using Plumix.Rendering;

namespace Plumix.UI;

// Dart parity source: dart:ui Canvas (the drawing half; Avalonia brushes/pens stand in for `Paint`)

public sealed partial class Canvas
{
    // Dart parity source: dart:ui Canvas.drawRect / Canvas.drawRRect.
    public void DrawRectangle(
        IBrush? brush,
        IPen? pen,
        Rect rect,
        double radiusX = 0,
        double radiusY = 0,
        BoxShadows boxShadows = default,
        bool isAntiAlias = true)
    {
        AddDrawCommand(context =>
        {
            using var renderOptions = context.PushRenderOptions(new RenderOptions
            {
                EdgeMode = isAntiAlias ? EdgeMode.Antialias : EdgeMode.Aliased,
            });
            context.DrawRectangle(brush, pen, new RoundedRect(rect, radiusX, radiusY), boxShadows);
        });
    }

    // Dart parity source: dart:ui Canvas.drawRRect (corner radii given as a BorderRadius).
    public void DrawRectangle(
        IBrush? brush,
        IPen? pen,
        Rect rect,
        BorderRadius borderRadius,
        BoxShadows boxShadows = default)
    {
        AddDrawCommand(context =>
        {
            var roundedRect = new RoundedRect(
                rect,
                new Vector(borderRadius.TopLeftRadius.X, borderRadius.TopLeftRadius.Y),
                new Vector(borderRadius.TopRightRadius.X, borderRadius.TopRightRadius.Y),
                new Vector(borderRadius.BottomRightRadius.X, borderRadius.BottomRightRadius.Y),
                new Vector(borderRadius.BottomLeftRadius.X, borderRadius.BottomLeftRadius.Y));
            context.DrawRectangle(brush, pen, roundedRect, boxShadows);
        });
    }

    // Dart parity source: dart:ui Canvas.drawRRect.
    public void DrawRRect(RRect rrect, IBrush? brush, IPen? pen)
    {
        var path = new Path();
        path.AddRRect(rrect);
        DrawPath(path, brush, pen);
    }

    // Dart parity source: dart:ui Canvas.drawRSuperellipse.
    public void DrawRSuperellipse(RSuperellipse rsuperellipse, IBrush? brush, IPen? pen)
    {
        var path = new Path();
        path.AddRSuperellipse(rsuperellipse);
        DrawPath(path, brush, pen);
    }

    /// <summary>Draws a blurred box shadow using the exact rounded-superellipse contour.</summary>
    public void DrawRSuperellipseShadow(RSuperellipse rsuperellipse, Plumix.Rendering.BoxShadow shadow)
    {
        ArgumentNullException.ThrowIfNull(shadow);
        RSuperellipse shadowShape = rsuperellipse
            .Inflate(shadow.SpreadRadius)
            .Shift(shadow.Offset);
        Geometry geometry = shadowShape.ToPath().ToGeometry();
        if (shadow.BlurRadius <= 0.0)
        {
            DrawGeometry(new SolidColorBrush(shadow.Color), null, geometry);
            return;
        }

        // Avalonia's path API has no mask-filter paint. Concentric strokes sampled from the same
        // Gaussian falloff keep the superellipse contour exact while providing backend-independent blur.
        int steps = Math.Max(2, (int)Math.Ceiling(shadow.BlurRadius * 2.0));
        double sigma = shadow.BlurSigma;
        double outerRadius = Math.Max(shadow.BlurRadius, sigma * 3.0);
        double previousOpacity = 0.0;
        for (int step = 0; step < steps; step++)
        {
            double radius = outerRadius * (steps - step) / steps;
            double targetOpacity = Math.Exp(-(radius * radius) / (2.0 * sigma * sigma));
            double layerOpacity = 1.0 - ((1.0 - targetOpacity) / (1.0 - previousOpacity));
            previousOpacity = targetOpacity;
            byte layerAlpha = (byte)Math.Clamp(
                (int)Math.Round(shadow.Color.A * layerOpacity),
                0,
                byte.MaxValue);
            Color layerColor = Color.FromArgb(layerAlpha, shadow.Color.R, shadow.Color.G, shadow.Color.B);
            if (layerColor.A > 0)
            {
                DrawGeometry(null, new Pen(new SolidColorBrush(layerColor), radius * 2.0), geometry);
            }
        }

        DrawGeometry(new SolidColorBrush(shadow.Color), null, geometry);
    }

    // Dart parity source: dart:ui Canvas.drawDRRect (the ring between two rounded rectangles).
    public void DrawDRRect(RRect outer, RRect inner, IBrush brush)
    {
        var outerPath = new Path();
        outerPath.AddRRect(outer);
        var innerPath = new Path();
        innerPath.AddRRect(inner);
        Geometry? geometry = null;
        AddDrawCommand(context => context.DrawGeometry(
            brush,
            null,
            geometry ??= new CombinedGeometry(
                GeometryCombineMode.Exclude,
                outerPath.ToGeometry(),
                innerPath.ToGeometry())));
    }

    // Dart parity source: dart:ui Canvas.drawOval.
    public void DrawOval(Rect oval, IBrush? brush, IPen? pen)
    {
        AddDrawCommand(context =>
            context.DrawEllipse(brush, pen, oval.Center, oval.Width / 2.0, oval.Height / 2.0));
    }

    // Dart parity source: dart:ui Canvas.drawPath.
    public void DrawPath(Path path, IBrush? brush, IPen? pen)
    {
        ArgumentNullException.ThrowIfNull(path);

        // The backend geometry is built on playback: recording must not need a render backend.
        Geometry? geometry = null;
        AddDrawCommand(context => context.DrawGeometry(brush, pen, geometry ??= path.ToGeometry()));
    }

    // Dart parity source: dart:ui Canvas.drawCircle.
    public void DrawCircle(IBrush? brush, IPen? pen, Point center, double radius)
    {
        double clampedRadius = Math.Max(0, radius);
        AddDrawCommand(context => context.DrawEllipse(brush, pen, center, clampedRadius, clampedRadius));
    }

    // Dart parity source: dart:ui Canvas.drawArc.
    public void DrawArc(IPen pen, Rect rect, double startAngleRadians, double sweepAngleRadians)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        if (Math.Abs(sweepAngleRadians) <= 0.0001)
        {
            return;
        }

        AddDrawCommand(context =>
        {
            var geometry = new StreamGeometry();
            using (var geometryContext = geometry.Open())
            {
                Point startPoint = PointOnEllipse(rect, startAngleRadians);
                Point endPoint = PointOnEllipse(rect, startAngleRadians + sweepAngleRadians);
                geometryContext.BeginFigure(startPoint, isFilled: false);
                geometryContext.ArcTo(
                    point: endPoint,
                    size: new Size(rect.Width / 2.0, rect.Height / 2.0),
                    rotationAngle: 0.0,
                    isLargeArc: Math.Abs(sweepAngleRadians) > Math.PI,
                    sweepDirection: sweepAngleRadians >= 0
                        ? SweepDirection.Clockwise
                        : SweepDirection.CounterClockwise);
                geometryContext.EndFigure(isClosed: false);
            }

            context.DrawGeometry(brush: null, pen: pen, geometry: geometry);
        });
    }

    // Dart parity source: dart:ui Canvas.drawLine.
    public void DrawLine(IPen pen, Point startPoint, Point endPoint)
    {
        AddDrawCommand(context => context.DrawLine(pen, startPoint, endPoint));
    }

    // Dart parity source: dart:ui Canvas.drawPath over a closed polygon contour.
    public void DrawPolygon(IBrush? brush, IPen? pen, IReadOnlyList<Point> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count < 3)
        {
            return;
        }

        AddDrawCommand(context =>
        {
            var geometry = new StreamGeometry();
            using (var geometryContext = geometry.Open())
            {
                geometryContext.BeginFigure(points[0], isFilled: true);
                for (int index = 1; index < points.Count; index++)
                {
                    geometryContext.LineTo(points[index]);
                }

                geometryContext.EndFigure(isClosed: true);
            }

            context.DrawGeometry(brush, pen, geometry);
        });
    }

    /// <summary>Plumix-only: draws an Avalonia geometry the caller already built.</summary>
    public void DrawGeometry(
        IBrush? brush,
        IPen? pen,
        Geometry geometry,
        Point geometryOffset = default)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        if (geometryOffset.X == 0.0 && geometryOffset.Y == 0.0)
        {
            AddDrawCommand(context => context.DrawGeometry(brush, pen, geometry));
            return;
        }

        AddDrawCommand(context =>
        {
            using var transform = context.PushTransform(
                Matrix.CreateTranslation(geometryOffset.X, geometryOffset.Y));
            context.DrawGeometry(brush, pen, geometry);
        });
    }

    // Dart parity source: dart:ui Canvas.drawShadow.
    public void DrawShadow(
        Geometry geometry,
        Color color,
        double elevation,
        bool transparentOccluder,
        Point geometryOffset = default)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        if (elevation <= 0.0 || color.A == 0)
        {
            return;
        }

        AddDrawCommand(context =>
        {
            Point effectiveOffset = geometryOffset + new Vector(0.0, elevation * 0.5);
            using var transform = context.PushTransform(Matrix.CreateTranslation(
                effectiveOffset.X,
                effectiveOffset.Y));
            int steps = Math.Max(1, (int)Math.Ceiling(elevation * 2.0));
            for (int step = steps; step >= 1; step--)
            {
                double fraction = step / (double)steps;
                byte alpha = (byte)Math.Clamp(
                    (int)Math.Round(color.A * 0.12 * (1.0 - (fraction * 0.75))),
                    1,
                    byte.MaxValue);
                var shadowBrush = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
                var shadowPen = new Pen(shadowBrush, step * 2.0);
                context.DrawGeometry(transparentOccluder ? shadowBrush : null, shadowPen, geometry);
            }
        });
    }

    // Dart parity source: dart:ui Canvas.drawParagraph.
    public void DrawTextLayout(TextLayout layout, Point point)
    {
        ArgumentNullException.ThrowIfNull(layout);
        AddDrawCommand(context => layout.Draw(context, point));
    }

    /// <summary>Plumix-only: draws a paragraph under a horizontal fade mask (toolbar/app-bar fades).</summary>
    public void DrawTextLayoutWithHorizontalFade(
        TextLayout layout,
        Point point,
        Rect bounds,
        bool fadeTowardRight)
    {
        ArgumentNullException.ThrowIfNull(layout);
        AddDrawCommand(context =>
        {
            var mask = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                GradientStops = fadeTowardRight
                    ? new GradientStops
                    {
                        new GradientStop(Colors.White, 0),
                        new GradientStop(Colors.White, 0.8),
                        new GradientStop(Colors.Transparent, 1),
                    }
                    : new GradientStops
                    {
                        new GradientStop(Colors.Transparent, 0),
                        new GradientStop(Colors.White, 0.2),
                        new GradientStop(Colors.White, 1),
                    },
            };
            using var clip = context.PushClip(bounds);
            using var opacityMask = context.PushOpacityMask(mask, bounds);
            layout.Draw(context, point);
        });
    }

    // Dart parity source: dart:ui Canvas.drawImageRect.
    public void DrawImage(
        IImage image,
        Rect sourceRect,
        Rect destinationRect,
        double opacity = 1.0,
        Rect? clipRect = null,
        BorderRadius? clipRadius = null,
        bool flipHorizontally = false,
        double? horizontalFlipAxisX = null,
        Rect? ovalClipRect = null,
        FilterQuality filterQuality = FilterQuality.Medium,
        bool isAntiAlias = false,
        BitmapBlendingMode blendMode = BitmapBlendingMode.SourceOver)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (sourceRect.Width <= 0 || sourceRect.Height <= 0
            || destinationRect.Width <= 0 || destinationRect.Height <= 0)
        {
            return;
        }

        double effectiveOpacity = Math.Clamp(opacity, 0.0, 1.0);
        AddDrawCommand(context =>
        {
            DrawingContext.PushedState? clip = null;
            DrawingContext.PushedState? alpha = null;
            DrawingContext.PushedState? transform = null;
            DrawingContext.PushedState? renderOptions = null;
            try
            {
                if (ovalClipRect.HasValue)
                {
                    clip = context.PushGeometryClip(new EllipseGeometry(ovalClipRect.Value));
                }
                else if (clipRect.HasValue)
                {
                    clip = clipRadius.HasValue && clipRadius.Value.Radius > 0
                        ? Layer.PushRoundedRectClip(context, clipRect.Value, clipRadius.Value.Radius)
                        : context.PushClip(clipRect.Value);
                }

                if (effectiveOpacity < 1.0)
                {
                    alpha = context.PushOpacity(effectiveOpacity);
                }

                renderOptions = context.PushRenderOptions(new RenderOptions
                {
                    BitmapInterpolationMode = filterQuality switch
                    {
                        FilterQuality.None => BitmapInterpolationMode.None,
                        FilterQuality.Low => BitmapInterpolationMode.LowQuality,
                        FilterQuality.High => BitmapInterpolationMode.HighQuality,
                        _ => BitmapInterpolationMode.MediumQuality,
                    },
                    EdgeMode = isAntiAlias || ovalClipRect.HasValue || clipRadius?.Radius > 0
                        ? EdgeMode.Antialias
                        : EdgeMode.Aliased,
                    BitmapBlendingMode = blendMode,
                });

                if (flipHorizontally)
                {
                    double centerX = horizontalFlipAxisX ?? destinationRect.Center.X;
                    transform = context.PushTransform(new Matrix(-1, 0, 0, 1, centerX * 2, 0));
                }

                context.DrawImage(image, sourceRect, destinationRect);
            }
            finally
            {
                transform?.Dispose();
                renderOptions?.Dispose();
                alpha?.Dispose();
                clip?.Dispose();
            }
        });
    }

    private static Point PointOnEllipse(Rect rect, double angleRadians)
    {
        double centerX = rect.X + (rect.Width / 2.0);
        double centerY = rect.Y + (rect.Height / 2.0);
        double radiusX = rect.Width / 2.0;
        double radiusY = rect.Height / 2.0;
        return new Point(
            centerX + (Math.Cos(angleRadians) * radiusX),
            centerY + (Math.Sin(angleRadians) * radiusY));
    }
}
