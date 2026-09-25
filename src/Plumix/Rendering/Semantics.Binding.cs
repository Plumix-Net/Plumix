using Avalonia;
using Plumix.Foundation;
using Plumix.UI;

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

/// <summary>
/// The glue between the semantics layer and the platform: whether semantics are being produced at
/// all, the listeners that see every platform semantics action, and the routing of those actions to
/// the render tree.
/// </summary>
/// <remarks>
/// <para>
/// Flutter's <c>SemanticsBinding</c> mixin. Dart composes the bindings into one object, so its abstract
/// <c>performSemanticsAction</c> and its <c>getRectOfSemanticsNodeInViewCoordinates</c> are implemented by
/// the <c>RendererBinding</c> mixed in with it. Plumix's bindings are separate singletons, so this one
/// forwards both to <see cref="RendererBinding.Instance"/> directly.
/// </para>
/// <para>
/// <c>accessibilityFeatures</c>, <c>handleAccessibilityFeaturesChanged</c>, <c>disableAnimations</c> and
/// <c>createSemanticsUpdateBuilder</c> are not ported here: the accessibility features live on
/// <c>WidgetsBinding</c> (see <c>docs/ai/BACKLOG.md</c>).
/// </para>
/// </remarks>
public sealed class SemanticsBinding
{
    private readonly ValueNotifier<bool> _semanticsEnabled;
    private readonly List<Action<SemanticsActionEvent>> _semanticsActionListeners = [];
    private int _outstandingHandles;
    private SemanticsHandle? _semanticsHandle;

    private SemanticsBinding()
    {
        PlatformDispatcher platformDispatcher = PlatformDispatcher.Instance;
        _semanticsEnabled = new ValueNotifier<bool>(platformDispatcher.SemanticsEnabled);
        platformDispatcher.OnSemanticsEnabledChanged = HandleSemanticsEnabledChanged;
        platformDispatcher.OnSemanticsActionEvent = HandleSemanticsActionEvent;
        HandleSemanticsEnabledChanged();
        AddSemanticsEnabledListener(HandleFrameworkSemanticsEnabledChanged);
        // Ensure the initial value is set.
        if (SemanticsEnabled)
        {
            HandleFrameworkSemanticsEnabledChanged();
        }
    }

    /// <summary>The ambient semantics binding.</summary>
    /// <remarks>Flutter's <c>SemanticsBinding.instance</c>.</remarks>
    public static SemanticsBinding Instance { get; } = new();

    /// <summary>Whether semantics information must be collected.</summary>
    /// <remarks>
    /// Flutter's <c>SemanticsBinding.semanticsEnabled</c>: true while any handle from
    /// <see cref="EnsureSemantics"/> is open, including the one the binding holds while the platform
    /// reports <see cref="PlatformDispatcher.SemanticsEnabled"/>.
    /// </remarks>
    public bool SemanticsEnabled
    {
        get
        {
            if (Constants.KDebugMode && _semanticsEnabled.Value != (_outstandingHandles > 0))
            {
                throw new AssertionError(
                    "'_semanticsEnabled.value == (_outstandingHandles > 0)': is not true.");
            }

            return _semanticsEnabled.Value;
        }
    }

    /// <summary>The number of clients holding a handle from <see cref="EnsureSemantics"/>.</summary>
    /// <remarks>Flutter's <c>SemanticsBinding.debugOutstandingSemanticsHandles</c>.</remarks>
    public int DebugOutstandingSemanticsHandles => _outstandingHandles;

    /// <summary>Adds a listener that is called every time <see cref="SemanticsEnabled"/> changes.</summary>
    /// <remarks>Flutter's <c>SemanticsBinding.addSemanticsEnabledListener</c>.</remarks>
    public void AddSemanticsEnabledListener(Action listener) => _semanticsEnabled.AddListener(listener);

    /// <summary>Removes a listener added with <see cref="AddSemanticsEnabledListener"/>.</summary>
    /// <remarks>Flutter's <c>SemanticsBinding.removeSemanticsEnabledListener</c>.</remarks>
    public void RemoveSemanticsEnabledListener(Action listener) => _semanticsEnabled.RemoveListener(listener);

    /// <summary>
    /// Adds a listener that is called for every semantics action the platform sends, before the
    /// action is performed.
    /// </summary>
    /// <remarks>Flutter's <c>SemanticsBinding.addSemanticsActionListener</c>.</remarks>
    public void AddSemanticsActionListener(Action<SemanticsActionEvent> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        _semanticsActionListeners.Add(listener);
    }

    /// <summary>Removes a listener added with <see cref="AddSemanticsActionListener"/>.</summary>
    /// <remarks>Flutter's <c>SemanticsBinding.removeSemanticsActionListener</c>.</remarks>
    public void RemoveSemanticsActionListener(Action<SemanticsActionEvent> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        _semanticsActionListeners.Remove(listener);
    }

    /// <summary>
    /// The global rect of the semantics node <paramref name="nodeId"/> in the view
    /// <paramref name="viewId"/>, or <see langword="null"/> when it cannot be found.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>SemanticsBinding.getRectOfSemanticsNodeInViewCoordinates</c>, which returns
    /// <c>null</c> and is overridden by <c>RendererBinding</c>; this forwards to that override.
    /// </remarks>
    public Rect? GetRectOfSemanticsNodeInViewCoordinates(int viewId, int nodeId) =>
        RendererBinding.Instance.GetRectOfSemanticsNodeInViewCoordinates(viewId, nodeId);

    /// <summary>Creates a new <see cref="SemanticsHandle"/> and requests that semantics be collected.</summary>
    /// <remarks>
    /// Flutter's <c>SemanticsBinding.ensureSemantics</c>. Semantics stay enabled until every handle
    /// returned here is disposed.
    /// </remarks>
    public SemanticsHandle EnsureSemantics()
    {
        DebugAssert(_outstandingHandles >= 0, "_outstandingHandles >= 0");
        _outstandingHandles++;
        DebugAssert(_outstandingHandles > 0, "_outstandingHandles > 0");
        _semanticsEnabled.Value = true;
        return new SemanticsHandle(DidDisposeSemanticsHandle);
    }

    /// <summary>
    /// Delivers a semantics action from the platform: every action listener sees it, then the render
    /// tree performs it.
    /// </summary>
    /// <remarks>
    /// Flutter's private <c>SemanticsBinding._handleSemanticsActionEvent</c>, installed as
    /// <see cref="PlatformDispatcher.OnSemanticsActionEvent"/>. Dart decodes <c>ByteData</c> arguments
    /// with the standard message codec first; Plumix hosts deliver decoded arguments.
    /// </remarks>
    internal void HandleSemanticsActionEvent(SemanticsActionEvent action)
    {
        NotifySemanticsActionListeners(action);
        RendererBinding.Instance.PerformSemanticsAction(action);
    }

    /// <summary>Runs the action listeners for <paramref name="action"/>, without performing it.</summary>
    /// <remarks>
    /// The listener half of <see cref="HandleSemanticsActionEvent"/>, for a host that performs the action
    /// on its own pipeline owner and reports whether a handler ran.
    /// </remarks>
    internal void NotifySemanticsActionListeners(SemanticsActionEvent action)
    {
        // Listeners may get added/removed while the iteration is in progress. Since the list cannot
        // be modified while iterating, we are creating a local copy for the iteration.
        Action<SemanticsActionEvent>[] localListeners = [.. _semanticsActionListeners];
        foreach (Action<SemanticsActionEvent> listener in localListeners)
        {
            if (_semanticsActionListeners.Contains(listener))
            {
                listener(action);
            }
        }
    }

    private void DidDisposeSemanticsHandle()
    {
        DebugAssert(_outstandingHandles > 0, "_outstandingHandles > 0");
        _outstandingHandles--;
        DebugAssert(_outstandingHandles >= 0, "_outstandingHandles >= 0");
        _semanticsEnabled.Value = _outstandingHandles > 0;
    }

    // Handle for semantics request from the platform.
    private void HandleSemanticsEnabledChanged()
    {
        if (PlatformDispatcher.Instance.SemanticsEnabled)
        {
            _semanticsHandle ??= EnsureSemantics();
        }
        else
        {
            _semanticsHandle?.Dispose();
            _semanticsHandle = null;
        }
    }

    private void HandleFrameworkSemanticsEnabledChanged()
    {
        PlatformDispatcher.Instance.SetSemanticsTreeEnabled(SemanticsEnabled);
    }

    private static void DebugAssert(bool condition, string expression)
    {
        if (Constants.KDebugMode && !condition)
        {
            throw new AssertionError($"'{expression}': is not true.");
        }
    }
}
