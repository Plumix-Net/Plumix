using System.Text.RegularExpressions;

namespace Plumix.UI;

// C#-only infrastructure for the float-uniform runtime-effect path used by InkSparkle.
// Engine contract: flutter/engine/src/flutter/lib/ui/painting/fragment_{program,shader}.cc.
// This internal program consumes SkSL, not Flutter's compiled asset container or image samplers. It is data
// (the source and its uniform layout); the backend compiles it (SkiaShaders.CompileProgram).
internal sealed class RuntimeFragmentProgram
{
    private RuntimeFragmentProgram(string source)
    {
        Source = source;
        Layout = [.. Regex.Matches(source, @"\buniform\s+(?:float|half|vec)([234])?\s+(\w+)\s*;")
            .Select(match => (match.Groups[2].Value,
                match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 1))];
        FloatCount = Layout.Sum(entry => entry.Count);
    }

    internal string Source { get; }

    /// <summary>The uniforms in declaration order: name and float count.</summary>
    internal (string Name, int Count)[] Layout { get; }

    internal int FloatCount { get; }

    /// <summary>The backend's compiled program.</summary>
    internal object? BackendState { get; set; }

    /// <summary>Parses and compiles <paramref name="source"/>; compilation errors throw here.</summary>
    internal static RuntimeFragmentProgram Compile(string source)
    {
        var program = new RuntimeFragmentProgram(source);
        SkiaShaders.CompileProgram(program);
        return program;
    }

    internal RuntimeFragmentShader FragmentShader() => new(this);
}

/// <summary>A fragment shader of a <see cref="RuntimeFragmentProgram"/>: the program and its float uniforms.</summary>
internal sealed class RuntimeFragmentShader(RuntimeFragmentProgram program) : Shader
{
    private float[]? _values = new float[program.FloatCount];

    internal RuntimeFragmentProgram Program => program;

    /// <summary>The current uniforms; null once disposed.</summary>
    internal float[]? Values => _values;

    internal void SetFloat(int index, double value)
    {
        ObjectDisposedException.ThrowIf(_values is null, this);
        _values[index] = (float)value;
    }

    // Display-list draws copy the uniform buffer, so later setFloat/dispose calls cannot change a
    // previously recorded picture.
    internal override Shader Snapshot()
    {
        ObjectDisposedException.ThrowIf(_values is null, this);
        return new FrozenFragmentShader(program, (float[])_values.Clone());
    }

    public override void Dispose()
    {
        ObjectDisposedException.ThrowIf(_values is null, this);
        _values = null;
        base.Dispose();
    }
}

/// <summary>A recorded copy of a <see cref="RuntimeFragmentShader"/>'s program and uniforms.</summary>
internal sealed class FrozenFragmentShader(RuntimeFragmentProgram program, float[] values) : Shader
{
    internal RuntimeFragmentProgram Program => program;

    internal float[] Values => values;
}
