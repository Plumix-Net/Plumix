// Dart parity source (reference): dart_sample/lib/main.dart (platform host bootstrap, adapted)

﻿using System;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using Avalonia.Threading;
using Plumix;
using Plumix.UI;
using Plumix.Widgets;

internal sealed partial class Program
{
    private const string LifecycleModule = "plumix-lifecycle";
    private static readonly Action<string> LifecycleCallback = HandleLifecycleState;

    private static async Task Main(string[] args)
    {
        await BuildAvaloniaApp()
            .WithInterFont()
            .StartBrowserAppAsync("out");
        await JSHost.ImportAsync(LifecycleModule, "./plumix-lifecycle.js");
        SubscribeToLifecycle(LifecycleCallback);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();

    [JSImport("subscribe", LifecycleModule)]
    private static partial void SubscribeToLifecycle(
        [JSMarshalAs<JSType.Function<JSType.String>>] Action<string> callback);

    private static void HandleLifecycleState(string state)
    {
        AppLifecycleState lifecycleState = state switch
        {
            "resumed" => AppLifecycleState.Resumed,
            "inactive" => AppLifecycleState.Inactive,
            "hidden" => AppLifecycleState.Hidden,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown browser lifecycle state."),
        };
        Dispatcher.UIThread.Post(
            () => WidgetsBinding.Instance.HandleAppLifecycleStateChanged(lifecycleState));
    }
}
