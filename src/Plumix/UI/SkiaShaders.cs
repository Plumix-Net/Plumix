using Avalonia.Media;
using Avalonia.Media.Immutable;
using SkiaSharp;
using TileMode = Plumix.Rendering.TileMode;

namespace Plumix.UI;

// C#-only infrastructure: the Avalonia/Skia backend's view of a Shader. Engine adapter:
// engine/src/flutter/display_list/skia/dl_sk_conversions.cc (gradients) and
// engine/src/flutter/lib/ui/painting/fragment_shader.cc (runtime effects). A shader is framework data; this
// builds the IBrush the Avalonia drawing pipeline carries (an INativeShader, which ShaderDrawing draws
// through Skia) once per shader and keeps it on the shader.

/// <summary>Builds and caches the backend brush of a <see cref="Shader"/>.</summary>
internal static class SkiaShaders
{
    /// <summary>The shader's backend brush, built on first use and reused while the shader lives.</summary>
    internal static IBrush ToBrush(Shader shader)
    {
        ArgumentNullException.ThrowIfNull(shader);
        return (IBrush)(shader.BackendState ??= shader switch
        {
            GradientShader gradient => new GradientBrush(gradient),
            RuntimeFragmentShader fragment => new FragmentBrush(
                () => CreateNativeShader(fragment.Program, fragment.Values
                    ?? throw new ObjectDisposedException(nameof(RuntimeFragmentShader)))),
            FrozenFragmentShader frozen => new FragmentBrush(
                () => CreateNativeShader(frozen.Program, frozen.Values)),
            _ => throw new NotSupportedException($"Unknown shader {shader.GetType().Name}."),
        });
    }

    /// <summary>
    /// Compiles a fragment program into an SkRuntimeEffect, kept on the program; throws on errors and on
    /// anything but float scalar/vector uniforms.
    /// </summary>
    internal static void CompileProgram(RuntimeFragmentProgram program)
    {
        SKRuntimeEffect? effect = SKRuntimeEffect.CreateShader(program.Source, out string errors);
        if (effect is null)
        {
            throw new InvalidOperationException($"Fragment shader compilation failed: {errors}");
        }

        if (effect.UniformSize != program.FloatCount * sizeof(float)
            || !effect.Uniforms.SequenceEqual(program.Layout.Select(entry => entry.Name))
            || effect.Children.Count != 0)
        {
            effect.Dispose();
            throw new NotSupportedException("This runtime adapter accepts only float scalar/vector uniforms.");
        }

        program.BackendState = effect;
    }

    private static SKShader CreateNativeShader(RuntimeFragmentProgram program, float[] values)
    {
        var effect = (SKRuntimeEffect)program.BackendState!;
        using var uniforms = new SKRuntimeEffectUniforms(effect);
        int offset = 0;
        foreach ((string name, int count) in program.Layout)
        {
            if (count == 1)
            {
                uniforms[name] = values[offset];
            }
            else
            {
                uniforms[name] = values.AsSpan(offset, count);
            }

            offset += count;
        }

        return effect.ToShader(uniforms);
    }

    // Avalonia 12 prevents implementing IBrush directly; its immutable gradient base supplies that
    // transport contract, and ShaderDrawing consumes the native shader before a draw reaches
    // Avalonia's brush switch.
    private sealed class GradientBrush(GradientShader gradient)
        : ImmutableGradientBrush(
            [.. gradient.Colors.Select((color, index) => new ImmutableGradientStop(
                index < gradient.Stops.Count ? gradient.Stops[index] : 0.0, color.ToAvalonia()))],
            1.0, null, null, GradientSpreadMethod.Pad), INativeShader
    {
        public SKShader? CreateNativeShader()
        {
            SKShaderTileMode tileMode = gradient.TileMode switch
            {
                TileMode.Repeated => SKShaderTileMode.Repeat,
                TileMode.Mirror => SKShaderTileMode.Mirror,
                TileMode.Decal => SKShaderTileMode.Decal,
                _ => SKShaderTileMode.Clamp,
            };
            SKColor[] colors = [.. gradient.ColorValues.Select(value => new SKColor(value))];
            float[] stops = gradient.StopValues;
            SKMatrix matrix = gradient.LocalMatrix is { } m
                ? new SKMatrix(m[0], m[1], m[2], m[3], m[4], m[5], m[6], m[7], m[8])
                : SKMatrix.Identity;
            SKPoint start = new(Path.Narrow(gradient.Start.Dx), Path.Narrow(gradient.Start.Dy));
            SKPoint end = new(Path.Narrow(gradient.End.Dx), Path.Narrow(gradient.End.Dy));
            // Skia owns stop sanitization and degenerate geometry, just as in Flutter's Skia backend.
            return gradient.Kind switch
            {
                GradientKind.Linear => SKShader.CreateLinearGradient(start, end, colors, stops, tileMode, matrix),
                GradientKind.Radial => SKShader.CreateRadialGradient(
                    end, Path.Narrow(gradient.Radius), colors, stops, tileMode, matrix),
                GradientKind.Conical => SKShader.CreateTwoPointConicalGradient(
                    start, Path.Narrow(gradient.FocalRadius), end, Path.Narrow(gradient.Radius),
                    colors, stops, tileMode, matrix),
                _ => SKShader.CreateSweepGradient(end, colors, stops, tileMode,
                    Degrees(gradient.StartAngle), Degrees(gradient.EndAngle), matrix),
            };
        }

        private static float Degrees(double radians) => Path.Narrow(radians) * 180.0f / (float)Math.PI;
    }

    private sealed class FragmentBrush(Func<SKShader> create)
        : ImmutableGradientBrush([], 1.0, null, null, GradientSpreadMethod.Pad), INativeShader
    {
        public SKShader? CreateNativeShader() => create();
    }
}
