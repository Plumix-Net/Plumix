using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Plumix.Widgets;

// C#-only infrastructure: the Avalonia application and lifetime adapter around runApp.

namespace Plumix;

/// <summary>
/// Base class for Plumix applications. Inherit from this class and override
/// <see cref="CreateRootWidget"/> to specify the root widget of your app.
/// </summary>
public abstract class PlumixApplication : Avalonia.Application
{
    public override void Initialize() { }

    public override void OnFrameworkInitializationCompleted()
    {
        Run(CreateRootWidget(), ApplicationLifetime, CreateOptions());
        base.OnFrameworkInitializationCompleted();
    }

    protected abstract Widget CreateRootWidget();

    protected virtual PlumixOptions CreateOptions() => new PlumixOptions();

    /// <summary>
    /// Shows <paramref name="application"/> in the Avalonia lifetime's main window, main view or
    /// Android activity.
    /// </summary>
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
}
