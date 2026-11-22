using NImpeller;
using Plumix.Rendering;
using Plumix.UI;
using Path = Plumix.UI.Path;

namespace Plumix.Impeller;

// C#-only infrastructure: no Dart parity source. Framework paints to Impeller paints for the stage 4
// prototype. Colour filters, image filters, inverted colours and fragment shaders are not converted yet:
// they draw without the effect (Unsupported lists each one once).

/// <summary>Builds Impeller paints from framework paints.</summary>
internal static class ImpellerPaints
{
    private static readonly HashSet<string> s_reported = [];

    /// <summary>A new Impeller paint carrying <paramref name="paint"/>'s state.</summary>
    internal static ImpellerPaint Create(Paint paint)
    {
        ImpellerPaint native = ImpellerPaint.New() ?? throw new InvalidOperationException("No Impeller paint.");
        native.SetColor(paint.Color.ToImpeller());
        native.SetBlendMode(paint.BlendMode.ToImpeller());
        if (paint.Style == PaintingStyle.Stroke)
        {
            native.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
            native.SetStrokeWidth((float)paint.StrokeWidth);
            native.SetStrokeMiter((float)paint.StrokeMiterLimit);
            native.SetStrokeCap(paint.StrokeCap switch
            {
                StrokeCap.Round => ImpellerStrokeCap.kImpellerStrokeCapRound,
                StrokeCap.Square => ImpellerStrokeCap.kImpellerStrokeCapSquare,
                _ => ImpellerStrokeCap.kImpellerStrokeCapButt,
            });
            native.SetStrokeJoin(paint.StrokeJoin switch
            {
                StrokeJoin.Round => ImpellerStrokeJoin.kImpellerStrokeJoinRound,
                StrokeJoin.Bevel => ImpellerStrokeJoin.kImpellerStrokeJoinBevel,
                _ => ImpellerStrokeJoin.kImpellerStrokeJoinMiter,
            });
        }

        if (paint.Shader is GradientShader gradient && CreateGradient(gradient) is { } source)
        {
            native.SetColorSource(source);
        }
        else if (paint.Shader is { } shader)
        {
            Unsupported(shader.GetType().Name);
        }

        if (paint.MaskFilter is { } mask && ImpellerMaskFilter.CreateBlurNew(BlurStyle(mask.Style), (float)mask.Sigma)
                is { } blur)
        {
            native.SetMaskFilter(blur);
        }

        if (paint.ColorFilter is not null)
        {
            Unsupported("ColorFilter");
        }

        if (paint.ImageFilter is not null)
        {
            Unsupported("ImageFilter");
        }

        if (paint.InvertColors)
        {
            Unsupported("InvertColors");
        }

        return native;
    }

    /// <summary>A plain fill paint of <paramref name="color"/>.</summary>
    internal static ImpellerPaint Solid(Color color)
    {
        ImpellerPaint native = ImpellerPaint.New() ?? throw new InvalidOperationException("No Impeller paint.");
        native.SetColor(color.ToImpeller());
        return native;
    }

    internal static void Unsupported(string feature)
    {
        lock (s_reported)
        {
            if (s_reported.Add(feature))
            {
                Console.Error.WriteLine($"Plumix.Impeller: {feature} is not supported yet; drawn without it.");
            }
        }
    }

    private static ImpellerColorSource? CreateGradient(GradientShader gradient)
    {
        ImpellerColor[] colors = [.. gradient.ColorValues.Select(ImpellerConversions.ColorFromArgb)];
        float[] stops = gradient.StopValues;
        ImpellerTileMode tile = gradient.TileMode.ToImpeller();
        ImpellerMatrix? local = gradient.LocalMatrix is { } m
            ? new System.Numerics.Matrix4x4(
                m[0], m[3], 0, m[6],
                m[1], m[4], 0, m[7],
                0, 0, 1, 0,
                m[2], m[5], 0, m[8])
            : null;
        return gradient.Kind switch
        {
            GradientKind.Linear => ImpellerColorSource.CreateLinearGradientNew(
                gradient.Start.ToImpeller(), gradient.End.ToImpeller(), colors, stops, tile, local),
            GradientKind.Radial => ImpellerColorSource.CreateRadialGradientNew(
                gradient.Start.ToImpeller(), (float)gradient.Radius, colors, stops, tile, local),
            GradientKind.Conical => ImpellerColorSource.CreateConicalGradientNew(
                gradient.Start.ToImpeller(), (float)gradient.FocalRadius,
                gradient.End.ToImpeller(), (float)gradient.Radius, colors, stops, tile, local),
            GradientKind.Sweep => ImpellerColorSource.CreateSweepGradientNew(
                gradient.End.ToImpeller(), (float)(gradient.StartAngle * 180 / Math.PI),
                (float)(gradient.EndAngle * 180 / Math.PI), colors, stops, tile, local),
            _ => null,
        };
    }

    private static ImpellerBlurStyle BlurStyle(BlurStyle style) => style switch
    {
        Rendering.BlurStyle.Solid => ImpellerBlurStyle.kImpellerBlurStyleSolid,
        Rendering.BlurStyle.Outer => ImpellerBlurStyle.kImpellerBlurStyleOuter,
        Rendering.BlurStyle.Inner => ImpellerBlurStyle.kImpellerBlurStyleInner,
        _ => ImpellerBlurStyle.kImpellerBlurStyleNormal,
    };
}
