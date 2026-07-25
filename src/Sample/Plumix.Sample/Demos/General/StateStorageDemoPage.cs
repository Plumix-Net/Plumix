using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
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
                new Text("PageStorage + SharedAppData", fontSize: 20, color: Colors.Black),
                new Text(
                    "Jump the list, unmount it, then restore it. The same PageStorageKey restores the offset; " +
                    "the shared counter rebuilds only its keyed dependent. The list inherits its controller " +
                    "through PrimaryScrollController and its desktop chrome through ScrollConfiguration.",
                    fontSize: 14,
                    color: Colors.DimGray),
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
                            color: Color.Parse("#FFE8EEF6"),
                            alignment: Alignment.Center,
                            child: new Text(
                                "List is unmounted. Restore it to verify the saved offset.",
                                color: Color.Parse("#FF31506F")))),
            ]);
    }

    private static Widget BuildButton(string label, Action onTap)
    {
        return new SizedBox(
            width: 160,
            child: new CounterTapButton(
                label: label,
                onTap: onTap,
                background: Color.Parse("#FFDCE3ED"),
                foreground: Colors.Black,
                fontSize: 12,
                padding: new Thickness(8, 7)));
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

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 8,
            children:
            [
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("Jump to offset 240", () => _controller.JumpTo(240)),
                        BuildButton(
                            _showScrollbar ? "Hide config scrollbar" : "Show config scrollbar",
                            () => SetState(() => _showScrollbar = !_showScrollbar)),
                    ]),
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
                            child: new SingleChildScrollView(
                                key: new PageStorageKey<string>("state-storage-list"),
                                child: new Column(
                                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                                    spacing: 6,
                                    children: Enumerable.Range(0, 18)
                                        .Select(BuildRow)
                                        .ToArray()))))),
            ]);
    }

    public override void Dispose()
    {
        _controller.Dispose();
        base.Dispose();
    }

    private static Widget BuildRow(int index)
    {
        return new Container(
            height: 44,
            color: index % 2 == 0 ? Color.Parse("#FFF4F7FA") : Color.Parse("#FFE6EDF5"),
            padding: new Thickness(12, 0),
            alignment: Alignment.CenterLeft,
            child: new Text($"Stored row {index + 1}", color: Colors.Black));
    }

    private static Widget BuildButton(string label, Action onTap)
    {
        return new SizedBox(
            width: 180,
            child: new CounterTapButton(
                label: label,
                onTap: onTap,
                background: Color.Parse("#FF31506F"),
                foreground: Colors.White,
                fontSize: 12,
                padding: new Thickness(8, 7)));
    }
}

internal sealed class DesktopDemoScrollBehavior : ScrollBehavior
{
    public override TargetPlatform GetPlatform(BuildContext context) => TargetPlatform.Windows;
}
