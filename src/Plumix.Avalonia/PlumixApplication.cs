using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Plumix.Rendering;
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
    /// Shows <paramref name="application"/> in the Avalonia lifetime's main window or main view.
    /// </summary>
    public static void Run<T>(T application, IApplicationLifetime? applicationLifetime, PlumixOptions? options = null)
        where T : Widget
    {
        options ??= new PlumixOptions();
        // Without an entry assembly, relative asset keys belong to the assembly that defines the Avalonia
        // application. Set before the first build.
        PlatformAssetBundle.ApplicationAssemblyName ??= DeriveApplicationAssemblyName(
            Current?.GetType(),
            Assembly.GetEntryAssembly());

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

    // The application assembly for a runtime without an entry assembly; null where the entry assembly
    // answers (desktop, browser), so that the resolution there stays unchanged.
    internal static string? DeriveApplicationAssemblyName(Type? applicationType, Assembly? entryAssembly)
    {
        if (entryAssembly is not null || applicationType is null || applicationType == typeof(Avalonia.Application))
        {
            return null;
        }

        return applicationType.Assembly.GetName().Name;
    }
}
