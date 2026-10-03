using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/platform_view_link_demo_page.dart (exact sample parity)

public sealed class PlatformViewLinkDemoPage : StatefulWidget
{
    public override State CreateState() => new PlatformViewLinkDemoPageState();
}

internal sealed class PlatformViewLinkDemoPageState : State
{
    private static readonly Dictionary<string, Color> ViewColors = new()
    {
        ["demo/blue"] = new Color(0xFF9FC5E8),
        ["demo/green"] = new Color(0xFFB6D7A8),
    };

    private readonly FocusNode _otherNode = new(debugLabel: "outside the platform view");
    private readonly List<string> _log = [];
    private string _viewType = "demo/blue";
    private System.Action<bool>? _requestPlatformFocus;

    public override void Dispose()
    {
        _otherNode.Dispose();
        base.Dispose();
    }

    private void Log(string entry)
    {
        if (!Mounted)
        {
            return;
        }

        SetState(() =>
        {
            _log.Insert(0, entry);
            if (_log.Count > 6)
            {
                _log.RemoveAt(_log.Count - 1);
            }
        });
    }

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12,
            children:
            [
                new Text("PlatformViewLink", fontSize: 20, color: new Color(0xFF000000)),
                new Text(
                    "The link lays out a placeholder, creates the controller with the placeholder's size and "
                    + "position, then shows the surface factory's widget. Focus moves both ways.",
                    fontSize: 14,
                    color: new Color(0xFF696969)),
                new Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children:
                    [
                        BuildButton(
                            $"viewType: {_viewType}",
                            () => SetState(() => _viewType = _viewType == "demo/blue" ? "demo/green" : "demo/blue")),
                        BuildButton("platform requests focus", () => _requestPlatformFocus?.Invoke(true)),
                        BuildButton("focus outside", () => _otherNode.RequestFocus()),
                    ]),
                new SizedBox(
                    height: 120,
                    child: new PlatformViewLink(
                        viewType: _viewType,
                        onCreatePlatformView: parameters =>
                        {
                            _requestPlatformFocus = parameters.OnFocusChanged;
                            return new DemoPlatformViewController(parameters, Log);
                        },
                        surfaceFactory: (context, controller) => new Container(
                            color: ViewColors[_viewType],
                            alignment: Alignment.Center,
                            child: new Builder(builder: context => new Text(
                                $"platform view #{controller.ViewId}: "
                                + (Focus.Of(context).HasFocus ? "focused" : "not focused"),
                                fontSize: 16,
                                color: new Color(0xFF000000)))))),
                new Focus(
                    focusNode: _otherNode,
                    child: new Builder(builder: context => new Container(
                        height: 32,
                        color: Focus.Of(context).HasFocus ? new Color(0xFFFFE082) : new Color(0xFFDCE3ED),
                        alignment: Alignment.Center,
                        child: new Text("outside focus target", fontSize: 12, color: new Color(0xFF000000))))),
                .. _log.ConvertAll(Widget (entry) =>
                    new Text(entry, fontSize: 12, color: new Color(0xFF2F4F4F))),
            ]);
    }

    private static Widget BuildButton(string label, System.Action onTap)
    {
        return new CounterTapButton(
            label: label,
            onTap: onTap,
            background: new Color(0xFFDCE3ED),
            foreground: new Color(0xFF000000),
            fontSize: 12,
            padding: new Thickness(10, 8));
    }
}

/// <summary>A controller whose "platform view" is created once the link reports a non-empty size.</summary>
internal sealed class DemoPlatformViewController(
    PlatformViewCreationParams parameters,
    System.Action<string> log) : PlatformViewController
{
    private bool _created;

    public override int ViewId => parameters.Id;

    public override bool AwaitingCreation => !_created;

    public override Task Create(Size? size = null, Point? position = null)
    {
        if (size is not { } createSize || position is not { } createPosition)
        {
            return Task.CompletedTask;
        }

        _created = true;
        log($"create #{ViewId} ({parameters.ViewType}) {createSize.Width:0}x{createSize.Height:0} "
            + $"at ({createPosition.X:0}, {createPosition.Y:0})");
        parameters.OnPlatformViewCreated(ViewId);
        return Task.CompletedTask;
    }

    public override Task DispatchPointerEvent(PointerEvent @event) => Task.CompletedTask;

    public override Task ClearFocus()
    {
        log($"clearFocus #{ViewId}");
        return Task.CompletedTask;
    }

    public override Task Dispose() => Task.CompletedTask;
}
