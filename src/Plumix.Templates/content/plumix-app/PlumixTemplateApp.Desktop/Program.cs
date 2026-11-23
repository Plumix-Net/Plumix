using Avalonia;
using Plumix;
using Plumix.Widgets;

namespace PlumixTemplateApp.Desktop;

public sealed class App : PlumixApplication
{
    protected override Widget CreateRootWidget() => new MyApp();

    protected override PlumixOptions CreateOptions() => new() { Title = "PlumixTemplateApp" };
}

public static class Program
{
    [STAThread]
    public static void Main(string[] args) => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace()
        .StartWithClassicDesktopLifetime(args);
}
