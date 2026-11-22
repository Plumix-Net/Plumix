namespace Plumix.UI;

// Dart parity source: flutter/engine/src/flutter/lib/ui/painting.dart (Shader)

/// <summary>
/// Base class for objects such as gradients and fragment shaders that describe how a
/// <see cref="Paint"/> fills or strokes. Obtain one from <c>Gradient.CreateShader</c>; third-party
/// code cannot derive new kinds of shader.
/// </summary>
/// <remarks>
/// A shader is a description: framework data only (<see cref="GradientShader"/>, the fragment shaders).
/// The drawing backend builds its own native shader from it (Plumix.Avalonia's SkiaShaders).
/// </remarks>
public abstract class Shader : IDisposable
{
    private bool _disposed;

    private protected Shader()
    {
    }

    /// <summary>Whether this shader has been disposed.</summary>
    public bool DebugDisposed => _disposed;

    /// <summary>What the backend built from this shader, reused while the shader lives.</summary>
    internal object? BackendState { get; set; }

    /// <summary>Releases the shader; using it afterwards is an error in Dart.</summary>
    public virtual void Dispose()
    {
        _disposed = true;
    }

    /// <summary>
    /// This shader as it is now: a fragment shader whose uniforms can still change is copied, an
    /// immutable shader is returned as is. A canvas records a paint's shader this way, since Dart's
    /// canvas reads the paint when the call is made.
    /// </summary>
    internal virtual Shader Snapshot() => this;
}
