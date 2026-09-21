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
            case ISingleViewApplicationLifetime singleViewPlatform:
                singleViewPlatform.MainView = host;
                break;
        }

        host.RunApplication(application);
    }

    private static void RunWidgetCore(Widget application, WidgetsBinding binding)
    {
        binding.ScheduleAttachRootWidget(application);
        Scheduler.ScheduleWarmUpFrame();
    }
}
