using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/state_storage_demo_page.dart (exact sample parity)

public sealed class StateStorageDemoPage : StatefulWidget
{
    public override State CreateState() => new StateStorageDemoPageState();
}

internal sealed class StateStorageDemoPageState : State
{
    private const string SharedCounterKey = "shared-counter";
    private readonly PageStorageBucket _bucket = new();
    private bool _showScrollable = true;

    public override Widget Build(BuildContext context)
    {
        return new PageStorage(
            _bucket,
            new SharedAppData(
                new Builder(BuildContent)));
    }

    private Widget BuildContent(BuildContext context)
    {
        int sharedCounter = SharedAppData.GetValue(context, SharedCounterKey, () => 0);
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12,
            children:
            [
                new Text("PageStorage + SharedAppData", style: new TextStyle(FontSize: 20, Color: Colors.Black)),
                new Text(
                    "Jump the list, unmount it, then restore it. The same PageStorageKey restores the offset; " +
                    "the shared counter rebuilds only its keyed dependent. The list inherits its controller " +
                    "through PrimaryScrollController and its desktop chrome through ScrollConfiguration. " +
                    "Drag past an edge to compare Flutter glow and stretch indicators; the observer readout " +
                    "receives scroll and dimension notifications across sibling subtrees. The readout is also " +
                    "wrapped in translucent MetaData and IndexedSemantics(index: 0) without changing layout; " +
                    "its identified status node merges the readout label while optional text stays empty. " +
                    "The list and its rows expose list/listItem roles with an explicit list boundary.",
                    style: new TextStyle(FontSize: 14, Color: MaterialColors.Black54)),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton(
                            $"Shared value: {sharedCounter}",
                            () => SharedAppData.SetValue(context, SharedCounterKey, sharedCounter + 1)),
                        BuildButton(
                            _showScrollable ? "Unmount list" : "Restore list",
                            () => SetState(() => _showScrollable = !_showScrollable)),
                    ]),
                new Expanded(
                    child: _showScrollable
                        ? new RestorableStorageList()
                        : new Container(
                            color: new Color(0xFFE8EEF6),
                            alignment: Alignment.Center,
                            child: new Text(
                                "List is unmounted. Restore it to verify the saved offset.",
                                style: new TextStyle(Color: new Color(0xFF31506F))))),
            ]);
    }

    private static Widget BuildButton(string label, Action onTap)
    {
        return new SizedBox(
            width: 160,
            child: new OutlinedButton(
                onPressed: onTap,
                child: new Text(label)));
    }
}

internal sealed class RestorableStorageList : StatefulWidget
{
    public override State CreateState() => new RestorableStorageListState();
}

internal sealed class RestorableStorageListState : State
{
    private readonly ScrollController _controller = new();
    private bool _showScrollbar = true;
    private bool _useStretch = true;

    public override Widget Build(BuildContext context)
    {
        Widget scrollView = new SingleChildScrollView(
            key: new PageStorageKey<string>("state-storage-list"),
            child: new Semantics(
                container: true,
                explicitChildNodes: true,
                role: SemanticsRole.List,
                child: new Column(
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    spacing: 6,
                    children: Enumerable.Range(0, 18)
                        .Select(BuildRow)
                        .ToArray())));
        Widget indicatedScrollView = _useStretch
            ? new StretchingOverscrollIndicator(
                axisDirection: AxisDirection.Down,
                child: scrollView)
            : new GlowingOverscrollIndicator(
                axisDirection: AxisDirection.Down,
                color: new Color(0xFF625B71),
                child: scrollView);

        return new ScrollNotificationObserver(
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 8,
                children:
                [
                    new Wrap(
                        spacing: 8,
                        runSpacing: 8,
                        children:
                        [
                            BuildButton("Jump to offset 240", () => _controller.JumpTo(240)),
                            BuildButton(
                                _showScrollbar ? "Hide config scrollbar" : "Show config scrollbar",
                                () => SetState(() => _showScrollbar = !_showScrollbar)),
                            BuildButton(
                                _useStretch ? "Effect: stretch" : "Effect: glow",
                                () => SetState(() => _useStretch = !_useStretch)),
                        ]),
                    new MetaData(
                        metaData: "scroll-observer-readout",
                        behavior: HitTestBehavior.Translucent,
                        child: new Semantics(
                            identifier: "scroll-observer-readout",
                            role: SemanticsRole.Status,
                            button: false,
                            label: string.Empty,
                            value: string.Empty,
                            hint: string.Empty,
                            tooltip: string.Empty,
                            child: new IndexedSemantics(
                                index: 0,
                                child: new ScrollObserverReadout()))),
                    new Expanded(
                        child: new ScrollConfiguration(
                            behavior: new DesktopDemoScrollBehavior().CopyWith(
                                scrollbars: _showScrollbar,
                                dragDevices: new HashSet<PointerDeviceKind>
                                {
                                    PointerDeviceKind.Touch,
                                    PointerDeviceKind.Mouse,
                                    PointerDeviceKind.Trackpad,
                                }),
                            child: new PrimaryScrollController(
                                controller: _controller,
                                automaticallyInheritForPlatforms:
                                    new HashSet<TargetPlatform> { TargetPlatform.Windows },
                                child: indicatedScrollView))),
                ]));
    }

    public override void Dispose()
    {
        _controller.Dispose();
        base.Dispose();
    }

    private static Widget BuildRow(int index)
    {
        return new Semantics(
            container: true,
            role: SemanticsRole.ListItem,
            child: new Container(
                height: 44,
                color: index % 2 == 0 ? new Color(0xFFF4F7FA) : new Color(0xFFE6EDF5),
                padding: EdgeInsets.Symmetric(horizontal: 12),
                alignment: Alignment.CenterLeft,
                child: new Text($"Stored row {index + 1}")));
    }

    private static Widget BuildButton(string label, Action onTap)
    {
        return new SizedBox(
            width: 180,
            child: new FilledButton(
                onPressed: onTap,
                child: new Text(label)));
    }
}

internal sealed class ScrollObserverReadout : StatefulWidget
{
    public override State CreateState() => new ScrollObserverReadoutState();
}

internal sealed class ScrollObserverReadoutState : State
{
    private ScrollNotificationObserverState? _observer;
    private string _summary = "Observer: waiting for viewport metrics";

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        ScrollNotificationObserverState observer = ScrollNotificationObserver.Of(Context);
        if (ReferenceEquals(observer, _observer))
        {
            return;
        }

        _observer?.RemoveListener(HandleNotification);
        _observer = observer;
        _observer.AddListener(HandleNotification);
    }

    public override Widget Build(BuildContext context)
    {
        return new Text(_summary, style: new TextStyle(FontSize: 12, Color: new Color(0xFF31506F)));
    }

    public override void Dispose()
    {
        _observer?.RemoveListener(HandleNotification);
        _observer = null;
        base.Dispose();
    }

    private void HandleNotification(ScrollNotification notification)
    {
        IScrollMetrics metrics = notification.Metrics;
        SetState(() =>
        {
            _summary =
                $"Observer: {notification.GetType().Name}, offset {metrics.Pixels:0}, " +
                $"viewport {metrics.ViewportDimension:0}, max {metrics.MaxScrollExtent:0}";
        });
    }
}

internal sealed class DesktopDemoScrollBehavior : ScrollBehavior
{
    public override TargetPlatform GetPlatform(BuildContext context) => TargetPlatform.Windows;
}
