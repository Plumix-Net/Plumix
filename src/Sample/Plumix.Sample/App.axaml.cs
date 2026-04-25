using Avalonia;
using Avalonia.Markup.Xaml;

// Dart parity source (reference): dart_sample/lib/main.dart (sample app bootstrap, adapted)

namespace Plumix;

public class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        PlumixExtensions.Run(new CounterApp(), ApplicationLifetime);

        base.OnFrameworkInitializationCompleted();
    }
}
