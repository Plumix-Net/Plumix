using Plumix.Widgets;

// C#-only infrastructure: the Avalonia control that makes a `PlumixHost` the platform's implicit view and
// starts `runApp` in it. Every frame-producing step belongs to the bindings.

namespace Plumix;

/// <summary>
/// The Avalonia adapter for the implicit view owned by the process <see cref="WidgetsBinding"/>.
/// </summary>
/// <remarks>
/// The binding owns the <see cref="BuildOwner"/> and <see cref="RootElement"/> and produces every frame.
/// This control supplies the implicit <see cref="View"/> with its deprecated single-view pipeline owner
/// and render view, and the platform metrics. Assigning <see cref="RootWidget"/> remains a synchronous
/// embedding convenience; <see cref="PlumixExtensions.RunApp"/> uses Flutter's queued bootstrap.
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
        // Likewise the platform side of `flutter/textinput`: an autofocused field opens its
        // connection during that first build.
        AttachTextInputPlugin();
        // The engine knows the platform locales before `runApp`; the first build resolves against them.
        ReportLocales();
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
            // No widget tree, no text input clients; setting a root widget attaches the plugin again.
            DetachTextInputPlugin();
        }
    }
}
