using Plumix.Rendering;

// C#-only test infrastructure: associates an isolated render tree with the binding's hit testing.

namespace Plumix.Tests;

internal sealed class TestRenderViewRegistration : IDisposable
{
    private static readonly Dictionary<int, TestRenderViewRegistration> Scopes = [];
    private readonly RenderView _view;
    private readonly RenderView? _original;
    private readonly TestRenderViewRegistration? _previous;
    private bool _disposed;

    public TestRenderViewRegistration(RenderView view)
    {
        _view = view;
        int viewId = view.FlutterView.ViewId;
        RendererBinding binding = RendererBinding.Instance;
        _previous = Scopes.GetValueOrDefault(viewId);
        RenderView? previousView = binding.RenderViews.SingleOrDefault(
            candidate => candidate.FlutterView.ViewId == viewId);
        _original = _previous is null ? previousView : _previous._original;
        if (previousView is not null)
        {
            binding.RemoveRenderView(previousView);
        }

        RegisterPreservingConfiguration(binding, view);
        Scopes[viewId] = this;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        int viewId = _view.FlutterView.ViewId;
        if (!ReferenceEquals(Scopes.GetValueOrDefault(viewId), this))
        {
            return;
        }

        RendererBinding binding = RendererBinding.Instance;
        binding.RemoveRenderView(_view);
        TestRenderViewRegistration? previous = _previous;
        while (previous is { _disposed: true })
        {
            previous = previous._previous;
        }

        if (previous is not null)
        {
            Scopes[viewId] = previous;
            RegisterPreservingConfiguration(binding, previous._view);
        }
        else
        {
            Scopes.Remove(viewId);
            if (_original is not null)
            {
                RegisterPreservingConfiguration(binding, _original);
            }
        }
    }

    private static void RegisterPreservingConfiguration(RendererBinding binding, RenderView view)
    {
        ViewConfiguration configuration = view.Configuration;
        binding.AddRenderView(view);
        view.Configuration = configuration;
    }
}
