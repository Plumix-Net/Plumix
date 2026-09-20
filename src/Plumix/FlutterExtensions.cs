using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Plumix.Widgets;

// C#-only infrastructure: maps Plumix widget bootstrap onto Avalonia application lifetimes.

namespace Plumix;

public static class PlumixExtensions
{
    public static void Run<T>(T application, IApplicationLifetime? applicationLifetime, PlumixOptions? options = null)
        where T : Widget
    {
        options ??= new PlumixOptions();

        var host = new WidgetHost
        {
            RootWidget = application
        };

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
    }
}
