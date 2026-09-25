using Plumix.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/binding.dart; flutter/packages/flutter/lib/src/rendering/binding.dart (host integration, adapted)

namespace Plumix;

/// <summary>
/// The Avalonia adapter for the implicit view owned by the process <see cref="WidgetsBinding"/>.
/// </summary>
/// <remarks>
/// The binding owns the <see cref="BuildOwner"/> and <see cref="RootElement"/>. This control supplies
/// the implicit <see cref="View"/>, its legacy single-view render pipeline, platform metrics and the
/// Avalonia frame/paint boundary. Assigning <see cref="RootWidget"/> remains a synchronous embedding
/// convenience; <see cref="PlumixExtensions.RunApp"/> uses Flutter's queued bootstrap.
/// </remarks>
public sealed class WidgetHost : PlumixHost
{
    private Widget? _rootWidget;

    public Widget? RootWidget
    {
        get => _rootWidget;
        set
        {
            if (ReferenceEquals(_rootWidget, value))
            {
                return;
            }

            _rootWidget = value;
            if (value is null)
            {
                ClearApplication();
            }
            else
            {
                SetAsImplicitView();
                WidgetsBinding.Instance.AttachRootWidgetSynchronously(
                    WidgetsBinding.Instance.WrapWithDefaultView(value));
            }
        }
    }

    /// <summary>Starts the queued <c>runApp</c> bootstrap for the Avalonia lifetime adapter.</summary>
    internal void RunApplication(Widget application)
    {
        ArgumentNullException.ThrowIfNull(application);
        _rootWidget = application;
        SetAsImplicitView();
        PlumixExtensions.RunApp(application);
    }

    private void SetAsImplicitView()
    {
        // The implicit view's host is the platform side of `flutter/platform` from here on, as the
        // engine is before `runApp`: the first build already sends `SystemChrome` messages (the
        // `Title` description), usually before the control is attached to a window.
        AttachPlatformChannelHandler();
        WidgetsBinding.Instance.SetImplicitView(
            RootFlutterView,
            Pipeline,
            RootRenderView,
            GetMediaQueryData());
    }

    private void ClearApplication()
    {
        if (WidgetsBinding.IsImplicitView(RootFlutterView))
        {
            WidgetsBinding.Instance.AttachRootWidgetSynchronously(new ViewCollection([]));
            ScheduleVisualUpdate();
        }
    }

    protected override void OnBuildBeforeLayout()
    {
        if (WidgetsBinding.IsImplicitView(RootFlutterView))
        {
            using Scheduler.FrameworkThreadScope scope = Scheduler.EnterFrameworkThread();
            WidgetsBinding.Instance.BuildDirtyWidgets();
        }
    }

    protected override void OnFinalizeFrame()
    {
        if (WidgetsBinding.IsImplicitView(RootFlutterView))
        {
            WidgetsBinding.Instance.FinalizeTree();
        }
    }

    protected override void PerformReassemble()
    {
        if (WidgetsBinding.IsImplicitView(RootFlutterView))
        {
            WidgetsBinding.Instance.ReassembleApplication();
        }

        base.PerformReassemble();
    }
}
