using Avalonia.Media;

namespace Plumix.UI;

// Dart parity source: flutter/engine/src/flutter/lib/ui/painting.dart (Shader)

/// <summary>
/// Base class for objects such as gradients and fragment shaders that describe how a
/// <see cref="Paint"/> fills or strokes. Obtain one from <c>Gradient.CreateShader</c>; third-party
/// code cannot derive new kinds of shader.
/// </summary>
public abstract class Shader : IDisposable
{
    private bool _disposed;

    private protected Shader()
    {
    }

    /// <summary>Whether this shader has been disposed.</summary>
    public bool DebugDisposed => _disposed;

    /// <summary>The backend description of this shader.</summary>
    internal abstract IBrush Brush { get; }

    /// <summary>Releases the shader; using it afterwards is an error in Dart.</summary>
    public virtual void Dispose()
    {
        _disposed = true;
    }

    /// <summary>Wraps a backend brush that carries a shader description.</summary>
    internal static Shader FromBrush(IBrush brush) => new BrushShader(brush);

    /// <summary>
    /// This shader as it is now: a fragment shader whose uniforms can still change is copied, an
    /// immutable shader is returned as is. A canvas records a paint's shader this way, since Dart's
    /// canvas reads the paint when the call is made.
    /// </summary>
    internal Shader Snapshot()
    {
        if (Brush is not INativeShader native)
        {
            return this;
        }

        IBrush snapshot = native.Snapshot();
        return ReferenceEquals(snapshot, Brush) ? this : FromBrush(snapshot);
    }

    private sealed class BrushShader(IBrush brush) : Shader
    {
        internal override IBrush Brush { get; } = brush ?? throw new ArgumentNullException(nameof(brush));

        public override string ToString() => Brush.ToString() ?? nameof(Shader);
    }
}
