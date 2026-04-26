using Plumix.Widgets;

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
        PlumixExtensions.Run(CreateRootWidget(), ApplicationLifetime, CreateOptions());
        base.OnFrameworkInitializationCompleted();
    }

    protected abstract Widget CreateRootWidget();

    protected virtual PlumixOptions CreateOptions() => new PlumixOptions();
}
