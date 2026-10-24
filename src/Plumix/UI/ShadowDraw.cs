using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Plumix.UI;

// Dart parity source: flutter/engine/src/flutter/lib/ui/painting.dart (Canvas.drawShadow)
// Engine-side port: DlSkCanvasDispatcher::DrawShadow (display_list/skia/dl_sk_dispatcher.cc) feeding Skia's
// SkShadowUtils::ComputeTonalColors and SkDevice::drawShadow (src/utils/SkShadowUtils.cpp, Skia 8df24be6).
// dart:ui paths are volatile, so Skia takes the uncached branch: both meshes are tessellated in device space
// (ShadowTessellator) and drawn with SkCanvas::drawVertices through the Gaussian colour filter; a path the
// tessellator refuses (a concave outline) falls back to Skia's blurred-path approximation.
internal sealed class ShadowDraw(Path path, Color color, double elevation, bool transparentOccluder, double dpr)
    : ICustomDrawOperation
{
    // DlCanvas::kShadowLightHeight / kShadowLightRadius.
    private const float LightHeight = 600;
    private const float LightRadius = 800;
    private const float AmbientAlpha = 0.039f;
    private const float SpotAlpha = 0.25f;

    // SkRasterPipeline's gauss_a_to_rgba: exp(-(1 - a)^2 * 4) - 0.018, approximated with a quartic.
    private const string GaussianSksl = """
        half4 main(half4 color) {
            float a = color.a;
            a = (((-2.26661229133605957031 * a + 2.89795351028442382812) * a
                + 0.21345567703247070312) * a + 0.15489584207534790039) * a + 0.00030726194381713867;
            return half4(a);
        }
        """;

    private static readonly Lazy<SKRuntimeEffect> GaussianEffect = new(() =>
        SKRuntimeEffect.CreateColorFilter(GaussianSksl, out string errors)
        ?? throw new InvalidOperationException($"Shadow Gaussian filter failed to compile: {errors}"));

    private readonly Path _path = new(path);

    public Rect Bounds { get; } = ComputeShadowBounds(path.GetBounds(), elevation, dpr);

    public bool HitTest(Point point) => false;

    public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);

    public void Dispose()
    {
    }

    /// <summary>
    /// DlCanvas::ComputeShadowBounds under an identity transform: the ambient outset joined with the spot
    /// shadow, which the directional light at (0, -1, 1) pushes down by the occluder height.
    /// </summary>
    internal static Rect ComputeShadowBounds(Rect pathBounds, double elevation, double dpr)
    {
        float occluderZ = (float)(dpr * elevation);
        float ambientBlur = ShadowMetrics.AmbientBlurRadius(occluderZ);
        ShadowMetrics.GetDirectionalParams(occluderZ, 0, -1, 1, LightRadius / LightHeight,
            out float spotBlur, out _, out ShadowPoint spotOffset);
        Rect ambient = pathBounds.Inflate(ambientBlur);
        Rect spot = pathBounds.Translate(new Vector(spotOffset.X, spotOffset.Y)).Inflate(spotBlur);
        return ambient.Union(spot).Inflate(1.0);
    }

    /// <summary>SkShadowUtils::ComputeTonalColors: a greyscale ambient colour and a tonal spot colour.</summary>
    internal static (SKColor Ambient, SKColor Spot) ComputeTonalColors(SKColor inAmbient, SKColor inSpot)
    {
        // For tonal colour only the spot shadow gets colour values; the ambient shadow is greyscale.
        var ambient = new SKColor(0, 0, 0, inAmbient.Alpha);

        int spotR = inSpot.Red;
        int spotG = inSpot.Green;
        int spotB = inSpot.Blue;
        int max = Math.Max(Math.Max(spotR, spotG), spotB);
        int min = Math.Min(Math.Min(spotR, spotG), spotB);
        float luminance = 0.5f * (max + min) / 255.0f;
        float origA = inSpot.Alpha / 255.0f;

        // A colour alpha based on the luminance, scaled by an adjusted alpha value, chosen to match the
        // UX examples (assuming a = 0.25) and to stay reasonable for black and/or zero alpha.
        float alphaAdjust = (2.6f + ((-2.66667f + (1.06667f * origA)) * origA)) * origA;
        float colorAlpha = (3.544762f + ((-4.891428f + (2.3466f * luminance)) * luminance)) * luminance;
        colorAlpha = ShadowMetrics.Pin(alphaAdjust * colorAlpha, 0.0f, 1.0f);

        // Similarly the greyscale alpha, based on luminance and alpha.
        float greyscaleAlpha = ShadowMetrics.Pin(origA * (1 - (0.4f * luminance)), 0.0f, 1.0f);

        // The colour shadow rendered under the black one, folded into one premultiplied-equivalent colour.
        float colorScale = colorAlpha * (1.0f - greyscaleAlpha);
        float tonalAlpha = colorScale + greyscaleAlpha;
        float unPremulScale = colorScale / tonalAlpha;
        var spot = new SKColor(
            (byte)(unPremulScale * spotR),
            (byte)(unPremulScale * spotG),
            (byte)(unPremulScale * spotB),
            (byte)(tonalAlpha * 255.999f));
        return (ambient, spot);
    }

    public void Render(ImmediateDrawingContext context)
    {
        ISkiaSharpApiLeaseFeature feature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>()
            ?? throw new NotSupportedException("Canvas.drawShadow requires the Skia drawing backend.");
        using ISkiaSharpApiLease lease = feature.Lease();
        SKCanvas canvas = lease.SkCanvas;
        SKMatrix viewMatrix = canvas.TotalMatrix;

        // DlSkCanvasDispatcher::DrawShadow: the alpha multiply truncates, as SkColorSetA's U8CPU argument.
        uint inputAlpha = (uint)Math.Round(color.Alpha * Math.Clamp(lease.CurrentOpacity, 0.0, 1.0));
        var rgb = new SKColor((byte)color.Red, (byte)color.Green, (byte)color.Blue);
        SKColor inAmbient = rgb.WithAlpha((byte)(AmbientAlpha * inputAlpha));
        SKColor inSpot = rgb.WithAlpha((byte)(SpotAlpha * inputAlpha));
        (SKColor ambientColor, SKColor spotColor) = ComputeTonalColors(inAmbient, inSpot);

        var zPlaneParams = new ShadowPoint3(0, 0, (float)(dpr * elevation));
        // A directional light: the position is a direction in device space.
        var lightPos = new ShadowPoint3(0, -1, 1);
        const float lightRadius = LightRadius / LightHeight;
        if (!zPlaneParams.IsFinite || !float.IsFinite(lightRadius))
        {
            return;
        }

        using SKPath skPath = _path.CloneNativePath();
        canvas.Save();
        try
        {
            canvas.ResetMatrix();
            if (ambientColor.Alpha > 0)
            {
                ShadowVertices? vertices = ShadowTessellator.MakeAmbient(
                    skPath, viewMatrix, zPlaneParams, transparentOccluder);
                if (vertices is not null)
                {
                    DrawVertices(canvas, vertices, ambientColor);
                }
                else
                {
                    DrawAmbientBlur(canvas, skPath, viewMatrix, zPlaneParams.Z, ambientColor);
                }
            }

            if (spotColor.Alpha > 0)
            {
                ShadowVertices? vertices = ShadowTessellator.MakeSpot(
                    skPath, viewMatrix, zPlaneParams, lightPos, lightRadius, transparentOccluder, directional: true);
                if (vertices is not null)
                {
                    DrawVertices(canvas, vertices, spotColor);
                }
                else
                {
                    DrawSpotBlur(canvas, skPath, viewMatrix, zPlaneParams, lightPos, lightRadius, spotColor);
                }
            }
        }
        finally
        {
            canvas.Restore();
        }
    }

    private static void DrawVertices(SKCanvas canvas, ShadowVertices vertices, SKColor color)
    {
        if (vertices.Positions.Length == 0)
        {
            return;
        }

        using SKVertices mesh = SKVertices.CreateCopy(
            SKVertexMode.Triangles, vertices.Positions, null, vertices.Colors, vertices.Indices);
        // Run the vertex colour through the Gaussian filter, then modulate that greyscale against 'color'.
        using SKColorFilter gaussian = GaussianEffect.Value.ToColorFilter();
        using SKColorFilter modulate = SKColorFilter.CreateBlendMode(color, SKBlendMode.Modulate);
        using SKColorFilter filter = SKColorFilter.CreateCompose(modulate, gaussian);
        using var paint = new SKPaint { ColorFilter = filter };
        canvas.DrawVertices(mesh, SKBlendMode.Dst, paint);
    }

    // SkDevice::drawShadow's ambient fallback: the device-space path outset by a stroke, then blurred.
    private static void DrawAmbientBlur(
        SKCanvas canvas,
        SKPath path,
        SKMatrix viewMatrix,
        float occluderZ,
        SKColor color)
    {
        using SKPath devicePath = new(path);
        devicePath.Transform(viewMatrix);
        float deviceOutset = ShadowMetrics.AmbientBlurRadius(occluderZ);
        float oneOverA = ShadowMetrics.AmbientRecipAlpha(occluderZ);
        float blurRadius = 0.5f * deviceOutset * oneOverA;
        float strokeWidth = 0.5f * (deviceOutset - blurRadius);
        using SKMaskFilter blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, ConvertRadiusToSigma(blurRadius), false);
        using var paint = new SKPaint
        {
            Color = color,
            StrokeWidth = strokeWidth,
            Style = SKPaintStyle.StrokeAndFill,
            MaskFilter = blur,
        };
        canvas.DrawPath(devicePath, paint);
    }

    // SkDevice::drawShadow's spot fallback: the path under the spot shadow transform, blurred.
    private static void DrawSpotBlur(
        SKCanvas canvas,
        SKPath path,
        SKMatrix viewMatrix,
        ShadowPoint3 zPlaneParams,
        ShadowPoint3 lightPos,
        float lightRadius,
        SKColor color)
    {
        ShadowMetrics.GetSpotShadowTransform(lightPos, lightRadius, viewMatrix, zPlaneParams, path.Bounds,
            directional: true, out SKMatrix shadowMatrix, out float radius);
        canvas.SetMatrix(shadowMatrix);
        using SKMaskFilter blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, ConvertRadiusToSigma(radius), false);
        using var paint = new SKPaint { Color = color, MaskFilter = blur };
        canvas.DrawPath(path, paint);
        canvas.ResetMatrix();
    }

    // SkBlurMask::ConvertRadiusToSigma.
    private static float ConvertRadiusToSigma(float radius) => radius > 0 ? (0.57735f * radius) + 0.5f : 0.0f;
}
