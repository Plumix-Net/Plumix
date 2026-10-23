using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Plumix.UI;
using Plumix.Widgets;

// C#-only infrastructure: the Avalonia lifetime adapter over widgets/binding.dart's application bootstrap.

namespace Plumix;

public static class PlumixExtensions
{
    /// <summary>Runs an application in the platform's implicit view.</summary>
    /// <remarks>Flutter's top-level <c>runApp</c>.</remarks>
    public static void RunApp(Widget application)
    {
        ArgumentNullException.ThrowIfNull(application);
        WidgetsBinding binding = WidgetsFlutterBinding.EnsureInitialized();
        RunWidgetCore(binding.WrapWithDefaultView(application), binding);
    }

    /// <summary>Runs a caller-owned view tree without adding an implicit <see cref="View"/>.</summary>
    /// <remarks>Flutter's top-level <c>runWidget</c>.</remarks>
    public static void RunWidget(Widget application)
    {
        ArgumentNullException.ThrowIfNull(application);
        WidgetsBinding binding = WidgetsFlutterBinding.EnsureInitialized();
        RunWidgetCore(application, binding);
    }

    public static void Run<T>(T application, IApplicationLifetime? applicationLifetime, PlumixOptions? options = null)
        where T : Widget
    {
        options ??= new PlumixOptions();

        var host = new WidgetHost();

        switch (applicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                var window = new Window
                {
                    Title = options.Title,
                    Content = host
                };

                if (options.InitialWindowSize is { } size)
                {
                    window.Width = size.Width;
                    window.Height = size.Height;
                }

                desktop.MainWindow = window;
                break;
            case IActivityApplicationLifetime activityLifetime:
                // Android creates a view per Activity and asks the factory for its content each time.
                // The host — and with it the widget tree and its state — belongs to the process, so a
                // recreated Activity gets the same host instead of a new application.
                activityLifetime.MainViewFactory = () => DetachFromPreviousTopLevel(host);
                break;
            case ISingleViewApplicationLifetime singleViewPlatform:
                singleViewPlatform.MainView = host;
                break;
        }

        host.RunApplication(application);
    }

    // An Activity normally clears its content in onDestroy before its successor asks for the view; if
    // the old one is still alive (a second instance), the host moves to the new Activity.
    private static Control DetachFromPreviousTopLevel(WidgetHost host)
    {
        if (host.Parent is ContentControl previous && ReferenceEquals(previous.Content, host))
        {
            previous.Content = null;
        }

        return host;
    }

    private static void RunWidgetCore(Widget application, WidgetsBinding binding)
    {
        binding.ScheduleAttachRootWidget(application);
        SchedulerBinding.Instance.ScheduleWarmUpFrame();
    }
}
