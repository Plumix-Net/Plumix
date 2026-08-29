using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/object.dart

namespace Plumix;

/// <summary>
/// Manages the shared state a tree of <see cref="PipelineOwner"/>s needs from the layer above it —
/// today, whether semantics are being produced at all and how a visual update is requested.
/// </summary>
/// <remarks>
/// Flutter's <c>PipelineManifold</c>. A <see cref="PipelineOwner"/> is attached to a manifold with
/// <see cref="PipelineOwner.Attach(PipelineManifold)"/>; the manifold notifies its listeners when
/// <see cref="SemanticsEnabled"/> changes, and every attached owner creates or disposes its
/// <see cref="Rendering.SemanticsOwner"/> in response.
/// </remarks>
public abstract class PipelineManifold : IListenable
{
    /// <summary>Whether the owners attached to this manifold should produce a semantics tree.</summary>
    /// <remarks>Flutter's <c>PipelineManifold.semanticsEnabled</c>.</remarks>
    public abstract bool SemanticsEnabled { get; }

    /// <summary>Asks the layer above the pipeline to schedule a new frame.</summary>
    /// <remarks>Flutter's <c>PipelineManifold.requestVisualUpdate</c>.</remarks>
    public abstract void RequestVisualUpdate();

    /// <inheritdoc />
    public abstract void AddListener(Action listener);

    /// <inheritdoc />
    public abstract void RemoveListener(Action listener);
}

/// <summary>
/// A <see cref="PipelineManifold"/> a host drives directly: it owns the semantics-enabled flag and
/// forwards visual-update requests to a callback.
/// </summary>
/// <remarks>
/// C#-only infrastructure. Flutter's only implementation is the private
/// <c>_BindingPipelineManifold</c> inside <c>rendering/binding.dart</c>, which forwards
/// <c>SemanticsBinding.semanticsEnabled</c> and <c>RendererBinding.ensureVisualUpdate</c>. Plumix has
/// no binding layer, so a host constructs this instead and drives both itself.
/// </remarks>
public sealed class HostPipelineManifold : PipelineManifold, IDisposable
{
    private readonly ChangeNotifier _notifier = new();
    private bool _semanticsEnabled;

    /// <summary>Creates a manifold that forwards visual updates to <paramref name="onNeedVisualUpdate"/>.</summary>
    public HostPipelineManifold(Action? onNeedVisualUpdate = null, bool semanticsEnabled = false)
    {
        OnNeedVisualUpdate = onNeedVisualUpdate;
        _semanticsEnabled = semanticsEnabled;
    }

    /// <summary>Invoked by <see cref="RequestVisualUpdate"/>.</summary>
    public Action? OnNeedVisualUpdate { get; set; }

    /// <inheritdoc />
    public override bool SemanticsEnabled => _semanticsEnabled;

    /// <summary>
    /// Turns semantics production on or off for every <see cref="PipelineOwner"/> attached to this
    /// manifold, notifying them when the value changes.
    /// </summary>
    public void SetSemanticsEnabled(bool value)
    {
        if (_semanticsEnabled == value)
        {
            return;
        }

        _semanticsEnabled = value;
        _notifier.NotifyListeners();
    }

    /// <inheritdoc />
    public override void RequestVisualUpdate() => OnNeedVisualUpdate?.Invoke();

    /// <inheritdoc />
    public override void AddListener(Action listener) => _notifier.AddListener(listener);

    /// <inheritdoc />
    public override void RemoveListener(Action listener) => _notifier.RemoveListener(listener);

    /// <inheritdoc />
    public void Dispose() => _notifier.Dispose();
}
