using Avalonia.Markup.Xaml;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/main.dart (sample app bootstrap, adapted)

namespace Plumix;

public class App : PlumixApplication
{
    // Load App.axaml to apply FluentTheme from Avalonia.Themes.Fluent.
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    protected override Widget CreateRootWidget() => new CounterApp();

    protected override PlumixOptions CreateOptions() => new PlumixOptions
    {
        Title = "Plumix Sample",
        InitialWindowSize = new Avalonia.Size(350, 700)
    };
}
