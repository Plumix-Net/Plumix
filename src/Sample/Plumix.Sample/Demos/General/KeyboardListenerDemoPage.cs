using System;
using System.Collections.Generic;
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
    private readonly FocusNode _shortcutFocusNode = new();
    private readonly FocusNode _excludedFocusNode = new();
    private IReadOnlyDictionary<ShortcutActivator, Intent> _shortcuts = null!;
    private IReadOnlyDictionary<Type, FlutterAction> _actions = null!;
    private string _keyboardEvent = "none";
    private string _rawEvent = "none";
    private int _shortcutCount;
    private int _excludedClickCount;
    private bool _shortcutFocusHighlight;
    private bool _shortcutHoverHighlight;

    public override void InitState()
    {
        _keyboardFocusNode.AddListener(HandleFocusChanged);
        _rawFocusNode.AddListener(HandleFocusChanged);
        _shortcutFocusNode.AddListener(HandleFocusChanged);
        _excludedFocusNode.AddListener(HandleFocusChanged);
        _shortcuts = new Dictionary<ShortcutActivator, Intent>
        {
            [new SingleActivator("K", control: true)] = new CounterShortcutIntent(1),
            [new SingleActivator("J", control: true)] = new CounterShortcutIntent(-1),
        };
        _actions = new Dictionary<Type, FlutterAction>
        {
            [typeof(CounterShortcutIntent)] = new CallbackAction<CounterShortcutIntent>(
                intent =>
                {
                    SetState(() => _shortcutCount += intent.Delta);
                    return _shortcutCount;
                })
        };
    }

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12,
            children:
            [
                new Text("Keyboard + focus action detectors", fontSize: 20, color: Colors.Black),
                new Text(
                    "Click a panel or use its button, then press and release keyboard keys.",
                    fontSize: 14,
                    color: Colors.DimGray),
                BuildKeyboardListenerProbe(),
                BuildRawKeyboardListenerProbe(),
                BuildActionsShortcutsProbe(),
            ]);
    }

    public override void Dispose()
    {
        _keyboardFocusNode.RemoveListener(HandleFocusChanged);
        _rawFocusNode.RemoveListener(HandleFocusChanged);
        _shortcutFocusNode.RemoveListener(HandleFocusChanged);
        _excludedFocusNode.RemoveListener(HandleFocusChanged);
        _keyboardFocusNode.Dispose();
        _rawFocusNode.Dispose();
        _shortcutFocusNode.Dispose();
        _excludedFocusNode.Dispose();
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

    private Widget BuildActionsShortcutsProbe()
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 8,
            children:
            [
                new TextButton(
                    onPressed: () => _shortcutFocusNode.RequestFocus(),
                    child: new Text("Focus Actions + Shortcuts")),
                new FocusableActionDetector(
                    focusNode: _shortcutFocusNode,
                    shortcuts: _shortcuts,
                    actions: _actions,
                    onShowFocusHighlight: value => SetState(() => _shortcutFocusHighlight = value),
                    onShowHoverHighlight: value => SetState(() => _shortcutHoverHighlight = value),
                    child: BuildPanel(
                        title: "FocusableActionDetector",
                        detail:
                        $"count {_shortcutCount} — Ctrl+K / Ctrl+J — focus highlight "
                        + $"{OnOff(_shortcutFocusHighlight)} — hover {OnOff(_shortcutHoverHighlight)}",
                        focused: _shortcutFocusNode.HasFocus)),
                new ExcludeFocusTraversal(
                    child: new TextButton(
                        focusNode: _excludedFocusNode,
                        onPressed: () => SetState(() => _excludedClickCount++),
                        child: new Text(
                            $"ExcludeFocusTraversal: Tab skips, click works ({_excludedClickCount})"))),
            ]);
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

    private static string OnOff(bool value) => value ? "on" : "off";

    private void HandleFocusChanged()
    {
        SetState(static () => { });
    }
}

internal sealed class CounterShortcutIntent : Intent
{
    public CounterShortcutIntent(int delta)
    {
        Delta = delta;
    }

    public int Delta { get; }
}
