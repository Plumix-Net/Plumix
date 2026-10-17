using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/windowing_demo_page.dart (exact sample parity)
public sealed class WindowingDemoPage : StatefulWidget
{
    public override State CreateState() => new WindowingDemoPageState();
}

internal sealed class WindowingDemoPageState : State<WindowingDemoPage>
{
    private int _openWindows;
    private int _nextWindow = 1;
    private string _lastEvent = "none";

    private void Record(string message) => SetState(() => _lastEvent = message);

    private void OpenRegularWindow(BuildContext context)
    {
        WindowRegistry registry = WindowRegistry.Of(context);
        int number = _nextWindow++;
        WindowEntry? entry = null;
        var @delegate = new DemoWindowDelegate(onDestroyed: () =>
        {
            registry.Unregister(entry!);
            entry!.Controller.Dispose();
            SetState(() => _openWindows--);
            Record($"window {number} destroyed");
        });
        RegularWindowController controller = RegularWindowController.Create(
            size: new Size(360, 220),
            title: $"Plumix window {number}",
            @delegate: @delegate);
        entry = new WindowEntry(
            controller: controller,
            builder: _ => new DemoWindowContent(controller));
        registry.Register(entry);
        SetState(() => _openWindows++);
        Record($"window {number} opened");
    }

    private async void ShowDemoDialog(BuildContext context)
    {
        string? result = await MaterialDialogs.ShowDialog<string>(
            context,
            dialogContext => new AlertDialog(
                title: new Text("Dialog window"),
                content: new Text("With windowing enabled this dialog is its own window."),
                actions:
                [
                    new TextButton(
                        onPressed: () => Navigator.Of(dialogContext).Pop("cancel"),
                        child: new Text("Cancel")),
                    new TextButton(
                        onPressed: () => Navigator.Of(dialogContext).Pop("ok"),
                        child: new Text("OK")),
                ]));
        Record($"dialog closed: {result ?? "dismissed"}");
    }

    public override Widget Build(BuildContext context)
    {
        bool enabled = Features.IsWindowingEnabled;
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 16,
            children:
            [
                new Text("Windowing", fontSize: 20, color: Colors.Black),
                new Text(
                    "Regular windows and dialog windows through WindowRegistry, "
                    + "RegularWindowController and showDialog. "
                    + "Run with FLUTTER_ENABLED_FEATURE_FLAGS=windowing on a desktop host.",
                    color: Colors.DimGray),
                new Text($"Windowing: {(enabled ? "enabled" : "disabled")}", color: Colors.Black),
                new TextButton(
                    onPressed: enabled ? () => OpenRegularWindow(context) : null,
                    child: new Text("Open regular window")),
                new TextButton(
                    onPressed: () => ShowDemoDialog(context),
                    child: new Text("Show dialog")),
                new Text($"Open windows: {_openWindows}", color: Colors.Black),
                new Text($"Last event: {_lastEvent}", color: new Color(0xFF31506F)),
            ]);
    }
}

internal sealed class DemoWindowDelegate(Action onDestroyed) : RegularWindowControllerDelegate
{
    public override void OnWindowDestroyed()
    {
        base.OnWindowDestroyed();
        onDestroyed();
    }
}

internal sealed class DemoWindowContent(RegularWindowController controller) : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        Size size = WindowScope.ContentSizeOf(context);
        return new Container(
            color: new Color(0xFFF4F7FA),
            padding: new Thickness(16),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Start,
                spacing: 8,
                children:
                [
                    new Text(WindowScope.TitleOf(context), fontSize: 18, color: Colors.Black),
                    new Text($"Content size: {size.Width:0} x {size.Height:0}", color: Colors.Black),
                    new Text(
                        $"Activated: {(WindowScope.IsActivatedOf(context) ? "true" : "false")}",
                        color: Colors.Black),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            new TextButton(
                                onPressed: () => controller.SetMaximized(!WindowScope.IsMaximizedOf(context)),
                                child: new Text("Toggle maximize")),
                            new TextButton(
                                onPressed: controller.Destroy,
                                child: new Text("Close")),
                        ]),
                ]));
    }
}
