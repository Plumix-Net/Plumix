using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Avalonia.Threading;
using Plumix.UI;

// C#-only infrastructure: the web engine's typography observer (`_addTypographySettingsObserver` in
// engine/src/flutter/lib/web_ui/lib/src/engine/platform_dispatcher.dart), measured by
// wwwroot/plumix-typography.js and reported to the platform dispatcher.
internal static partial class BrowserTypographySettings
{
    private const string Module = "plumix-typography";
    private static readonly Action Changed = HandleChanged;

    public static async Task InitializeAsync()
    {
        await JSHost.ImportAsync(Module, "./plumix-typography.js");
        Observe(Changed);
    }

    [JSImport("observe", Module)]
    private static partial void Observe([JSMarshalAs<JSType.Function>] Action callback);

    [JSImport("read", Module)]
    [return: JSMarshalAs<JSType.Array<JSType.Number>>]
    private static partial double[] Read();

    // The engine updates the text scale factor and the four overrides, then invokes
    // onTextScaleFactorChanged and onMetricsChanged for whichever changed; the two dispatcher entry
    // points notify in that order and only on a change.
    private static void HandleChanged()
    {
        double[] values = Read();
        Dispatcher.UIThread.Post(() =>
        {
            PlatformDispatcher dispatcher = PlatformDispatcher.Instance;
            // A zero root font size would make the factor zero, which the dispatcher rejects.
            if (double.IsFinite(values[0]) && values[0] > 0)
            {
                dispatcher.UpdateTextScaleFactor(values[0]);
            }

            dispatcher.UpdateTextSpacingOverrides(
                lineHeightScaleFactorOverride: OrNull(values[1]),
                letterSpacingOverride: OrNull(values[2]),
                wordSpacingOverride: OrNull(values[3]),
                paragraphSpacingOverride: OrNull(values[4]));
        });
    }

    private static double? OrNull(double value) => double.IsNaN(value) ? null : value;
}
