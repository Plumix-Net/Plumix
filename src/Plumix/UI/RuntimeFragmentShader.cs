using System.Text.RegularExpressions;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using SkiaSharp;

namespace Plumix.UI;

// C#-only Skia adapter for the float-uniform runtime-effect path used by InkSparkle.
// Engine contract: flutter/engine/src/flutter/lib/ui/painting/fragment_{program,shader}.cc.
// This internal adapter consumes SkSL, not Flutter's compiled asset container or image samplers.
internal sealed class RuntimeFragmentProgram
{
    private readonly SKRuntimeEffect _effect;
    private readonly (string Name, int Count)[] _layout;

    private RuntimeFragmentProgram(SKRuntimeEffect effect, string source)
    {
        _effect = effect;
        _layout = [.. Regex.Matches(source, @"\buniform\s+(?:float|half|vec)([234])?\s+(\w+)\s*;")
            .Select(match => (match.Groups[2].Value,
                match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 1))];
        FloatCount = _layout.Sum(entry => entry.Count);
        if (effect.UniformSize != FloatCount * sizeof(float)
            || !effect.Uniforms.SequenceEqual(_layout.Select(entry => entry.Name))
            || effect.Children.Count != 0)
        {
            throw new NotSupportedException("This runtime adapter accepts only float scalar/vector uniforms.");
        }
    }

    internal int FloatCount { get; }

    internal static RuntimeFragmentProgram Compile(string source)
    {
        SKRuntimeEffect? effect = SKRuntimeEffect.CreateShader(source, out string errors);
        if (effect is null)
        {
            throw new InvalidOperationException($"Fragment shader compilation failed: {errors}");
        }

        try
        {
            return new RuntimeFragmentProgram(effect, source);
        }
        catch
        {
            effect.Dispose();
            throw;
        }
    }

    internal RuntimeFragmentShader FragmentShader() => new(this);

    internal SKShader CreateNativeShader(float[] values)
    {
        using var uniforms = new SKRuntimeEffectUniforms(_effect);
        int offset = 0;
        foreach ((string name, int count) in _layout)
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

        return _effect.ToShader(uniforms);
    }
}

internal sealed class RuntimeFragmentShader(RuntimeFragmentProgram program)
    : ImmutableGradientBrush([], 1.0, null, null, GradientSpreadMethod.Pad), INativeShader, IDisposable
{
    private float[]? _values = new float[program.FloatCount];

    internal bool DebugDisposed => _values is null;

    internal void SetFloat(int index, double value)
    {
        ObjectDisposedException.ThrowIf(_values is null, this);
        _values[index] = (float)value;
    }

    public IBrush Snapshot()
    {
        ObjectDisposedException.ThrowIf(_values is null, this);
        return new FrozenShader(program, (float[])_values.Clone());
    }

    public SKShader CreateNativeShader()
    {
        ObjectDisposedException.ThrowIf(_values is null, this);
        return program.CreateNativeShader(_values);
    }

    public void Dispose()
    {
        ObjectDisposedException.ThrowIf(_values is null, this);
        _values = null;
    }

    // Display-list draws copy the uniform buffer. The program and copied values outlive the mutable
    // shader, so later setFloat/dispose calls cannot change a previously recorded picture.
    private sealed class FrozenShader(RuntimeFragmentProgram program, float[] values)
        : ImmutableGradientBrush([], 1.0, null, null, GradientSpreadMethod.Pad), INativeShader
    {
        public IBrush Snapshot() => this;
        public SKShader CreateNativeShader() => program.CreateNativeShader(values);
    }
}
