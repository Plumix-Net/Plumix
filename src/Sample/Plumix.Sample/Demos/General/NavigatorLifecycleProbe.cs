using System;
using System.Threading.Tasks;
using Avalonia;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source: dart_sample/lib/demos/general/navigator_lifecycle_probe.dart

namespace Plumix;

#pragma warning disable CS0618 // Demonstrate awaiting the pinned legacy willPop callback.

internal sealed class NavigatorLifecycleProbe : StatefulWidget
{
    public override State CreateState() => new ProbeState();

    private sealed class ProbeState : State<NavigatorLifecycleProbe>
    {
        private readonly GlobalKey<NavigatorState> _navigatorKey = new GlobalObjectKey<NavigatorState>(new object());
        private TaskCompletionSource<bool>? _decision;
        private int _page;
        private int _localEntries;
        private string _lastResult = "none";

        private Task<bool> ConfirmBack()
        {
            if (_decision is null)
            {
                SetState(() => _decision = new TaskCompletionSource<bool>());
            }

            return _decision!.Task;
        }

        private void PushPage()
        {
            int number = ++_page;
            EventLoop.RunAsync(async () =>
            {
                object? result = await _navigatorKey.CurrentState!.Push(new PageRouteBuilder(
                    settings: new RouteSettings($"page {number}"),
                    pageBuilder: (_, _, _) => new WillPopScope(
                        onWillPop: ConfirmBack,
                        child: PageBody($"Page {number}: Back waits for Allow back.")),
                    transitionDuration: TimeSpan.FromMilliseconds(300)));
                if (Mounted)
                {
                    SetState(() => _lastResult = $"page {number}: {result ?? "null"}");
                }
            });
        }

        private static Widget PageBody(string message) => new Container(
            color: new Color(0xFFE8F0FE),
            alignment: Alignment.Center,
            child: new Text(message, style: new TextStyle(FontSize: 13, Color: Colors.Black)));

        public override void Dispose()
        {
            _decision?.TrySetResult(false);
            base.Dispose();
        }

        public override Widget Build(BuildContext context) => new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 6,
            children:
            [
                new Text("Route lifecycle", style: new TextStyle(FontSize: 16, Color: Colors.Black)),
                new Text(
                    $"Result: {_lastResult}; local entries: {_localEntries}; "
                    + (_decision is null ? "ready" : "waiting for Allow back"),
                    style: new TextStyle(FontSize: 12, Color: Colors.Black)),
                new Wrap(spacing: 6, runSpacing: 6, children:
                [
                    ActionButton("Push page", PushPage),
                    ActionButton("Back", () =>
                    {
                        if (_decision is null) _navigatorKey.CurrentState!.MaybePop("back");
                    }),
                    ActionButton("Allow back", () =>
                    {
                        TaskCompletionSource<bool>? decision = _decision;
                        SetState(() => _decision = null);
                        decision?.TrySetResult(true);
                    }),
                    ActionButton("Add local history", () =>
                    {
                        _navigatorKey.CurrentState!.CurrentRoute!.AddLocalHistoryEntry(new LocalHistoryEntry(() =>
                        {
                            if (Mounted) SetState(() => _localEntries--);
                        }));
                        SetState(() => _localEntries++);
                    }),
                    ActionButton("Pop to home with result", () =>
                        _navigatorKey.CurrentState!.PopUntilWithResult(route => route.IsFirst, "home-result")),
                ]),
                new SizedBox(height: 100, child: new Navigator(
                    key: _navigatorKey,
                    initialRoute: new PageRouteBuilder(
                        pageBuilder: (_, _, _) => PageBody("Home: push two pages, then pop to home with a result.")))),
            ]);

        private static Widget ActionButton(string label, Action action) => new CounterTapButton(
            label: label,
            onTap: action,
            background: new Color(0xFFE0F2F1),
            foreground: Colors.Black,
            fontSize: 12,
            padding: EdgeInsets.Symmetric(horizontal: 8, vertical: 6));
    }
}

#pragma warning restore CS0618
