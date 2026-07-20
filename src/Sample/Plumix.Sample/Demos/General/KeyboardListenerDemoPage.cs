using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/keyboard_listener_demo_page.dart (exact sample parity)

public sealed class KeyboardListenerDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new KeyboardListenerDemoPageState();
    }
}

internal sealed class KeyboardListenerDemoPageState : State
{
    private readonly FocusNode _keyboardFocusNode = new();
    private readonly FocusNode _rawFocusNode = new();
    private string _keyboardEvent = "none";
    private string _rawEvent = "none";

    public override void InitState()
    {
        _keyboardFocusNode.AddListener(HandleFocusChanged);
        _rawFocusNode.AddListener(HandleFocusChanged);
    }

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12,
            children:
            [
                new Text("KeyboardListener + RawKeyboardListener", fontSize: 20, color: Colors.Black),
                new Text(
                    "Click a panel or use its button, then press and release keyboard keys.",
                    fontSize: 14,
                    color: Colors.DimGray),
                BuildKeyboardListenerProbe(),
                BuildRawKeyboardListenerProbe(),
            ]);
    }

    public override void Dispose()
    {
        _keyboardFocusNode.RemoveListener(HandleFocusChanged);
        _rawFocusNode.RemoveListener(HandleFocusChanged);
        _keyboardFocusNode.Dispose();
        _rawFocusNode.Dispose();
    }

    private Widget BuildKeyboardListenerProbe()
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 8,
            children:
            [
                new TextButton(
                    onPressed: () => _keyboardFocusNode.RequestFocus(),
                    child: new Text("Focus KeyboardListener")),
                new KeyboardListener(
                    focusNode: _keyboardFocusNode,
                    onKeyEvent: keyEvent => SetState(() =>
                    {
                        _keyboardEvent = DescribeKeyEvent(keyEvent);
                    }),
                    child: BuildPanel(
                        title: "KeyboardListener",
                        detail: _keyboardEvent,
                        focused: _keyboardFocusNode.HasFocus)),
            ]);
    }

    private Widget BuildRawKeyboardListenerProbe()
    {
#pragma warning disable CS0618
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 8,
            children:
            [
                new TextButton(
                    onPressed: () => _rawFocusNode.RequestFocus(),
                    child: new Text("Focus RawKeyboardListener")),
                new RawKeyboardListener(
                    focusNode: _rawFocusNode,
                    onKey: keyEvent => SetState(() =>
                    {
                        string phase = keyEvent is RawKeyDownEvent ? "down" : "up";
                        _rawEvent = $"{keyEvent.Key} — {phase}";
                    }),
                    child: BuildPanel(
                        title: "RawKeyboardListener (deprecated compatibility)",
                        detail: _rawEvent,
                        focused: _rawFocusNode.HasFocus)),
            ]);
#pragma warning restore CS0618
    }

    private static Widget BuildPanel(string title, string detail, bool focused)
    {
        return new Container(
            height: 88,
            padding: new Thickness(12),
            decoration: new BoxDecoration(
                Color: focused ? Color.Parse("#FFE0F2F1") : Color.Parse("#FFF1F3F4"),
                Border: new BorderSide(
                    color: focused ? Color.Parse("#FF00796B") : Color.Parse("#FF9AA0A6"),
                    width: focused ? 2 : 1),
                BorderRadius: BorderRadius.Circular(10)),
            child: new Column(
                mainAxisAlignment: MainAxisAlignment.Center,
                crossAxisAlignment: CrossAxisAlignment.Start,
                spacing: 6,
                children:
                [
                    new Text(title, fontSize: 15, color: Colors.Black),
                    new Text($"Last event: {detail}", fontSize: 13, color: Colors.DimGray),
                ]));
    }

    private static string DescribeKeyEvent(KeyEvent keyEvent)
    {
        return $"{keyEvent.Key} — {(keyEvent.IsDown ? "down" : "up")}";
    }

    private void HandleFocusChanged()
    {
        SetState(static () => { });
    }
}
