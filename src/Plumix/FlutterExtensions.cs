using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Plumix.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/binding.dart; flutter/packages/flutter/lib/src/rendering/binding.dart (host integration, adapted)

namespace Plumix;

public static class PlumixExtensions
{
    private const double TargetWindowWidthPixels = 512;
    private const double TargetWindowHeightPixels = 1820;

    public static void Run<T>(T application, IApplicationLifetime? applicationLifetime) where T : Widget
    {
        var host = new WidgetHost
        {
            RootWidget = application
        };


        switch (applicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                var window = new Window
                {
                    Title = "Plumix.Sample.NET",
                    Content = host
                };

                // Keep target desktop window size stable in physical pixels across DPI scales.
                ApplyPixelSizedWindowBounds(window, TargetWindowWidthPixels, TargetWindowHeightPixels);
                window.Opened += (_, _) =>
                    ApplyPixelSizedWindowBounds(window, TargetWindowWidthPixels, TargetWindowHeightPixels);

                desktop.MainWindow = window;
                break;
            case ISingleViewApplicationLifetime singleViewPlatform:
                singleViewPlatform.MainView = host;
                break;
        }
    }

    private static void ApplyPixelSizedWindowBounds(Window window, double widthPixels, double heightPixels)
    {
        var scale = window.RenderScaling;
        if (double.IsNaN(scale) || double.IsInfinity(scale) || scale <= 0)
        {
            scale = window.DesktopScaling;
        }

        if (double.IsNaN(scale) || double.IsInfinity(scale) || scale <= 0)
        {
            scale = 1;
        }

        window.Width = widthPixels / scale;
        window.Height = heightPixels / scale;
    }
}
