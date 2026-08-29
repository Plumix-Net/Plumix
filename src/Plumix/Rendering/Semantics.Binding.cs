// Dart parity source: flutter/packages/flutter/lib/src/semantics/binding.dart

namespace Plumix.Rendering;

/// <summary>
/// A handle that keeps a semantics tree alive for as long as it is not disposed.
/// </summary>
/// <remarks>
/// Flutter's <c>SemanticsHandle</c>. Semantics information is only collected while some client is
/// interested in it; a client expresses that interest by holding a handle and closes it with
/// <see cref="Dispose"/>. When every outstanding handle is closed — and nothing else is asking for
/// semantics — the framework stops producing them.
/// <para>
/// Dart splits this in two: the public <c>SemanticsHandle</c> from <c>semantics/binding.dart</c> and
/// the private <c>_LocalSemanticsHandle</c> in <c>rendering/object.dart</c> that
/// <c>PipelineOwner.ensureSemantics</c> returns. Both are just a <c>dispose()</c> over a callback, so
/// Plumix has one class whose callback the creator supplies.
/// </para>
/// </remarks>
public class SemanticsHandle
{
    private readonly Action _onDispose;
    private bool _disposed;

    internal SemanticsHandle(Action onDispose)
    {
        ArgumentNullException.ThrowIfNull(onDispose);
        _onDispose = onDispose;
    }

    /// <summary>Closes this semantics handle.</summary>
    /// <remarks>Flutter's <c>SemanticsHandle.dispose</c>. Closing twice is a no-op.</remarks>
    public virtual void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _onDispose();
    }
}
