using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/material/refresh_indicator_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class RefreshIndicatorDemoPage : StatefulWidget
{
    public override State CreateState() => new RefreshIndicatorDemoPageState();
}

internal sealed class RefreshIndicatorDemoPageState : State
{
    private int _variant;
    private bool _useCupertinoPlatform;
    private bool _useThemeOverrides;
    private bool _useSchemeColor;
    private int _refreshCount;
    private string _status = "idle";

    public override Widget Build(BuildContext context)
    {
        var baseTheme = Theme.Of(context);
        var theme = baseTheme with
        {
            Platform = _useCupertinoPlatform ? TargetPlatform.IOS : TargetPlatform.Android,
            PrimaryColor = Color.Parse("#FFFF6F00"),
            ColorScheme = baseTheme.ColorScheme.CopyWith(primary: Color.Parse("#FF00897B")),
            ProgressIndicatorTheme = _useThemeOverrides
                ? new ProgressIndicatorThemeData(
                    Color: Color.Parse("#FF6A1B9A"),
                    RefreshBackgroundColor: Color.Parse("#FFFFF3E0"),
                    StrokeAlign: -1,
                    StrokeCap: StrokeCap.Round)
                : new ProgressIndicatorThemeData()
        };

        return new Theme(
            data: theme,
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 10,
                children:
                [
                    new Text("RefreshIndicator + RefreshProgressIndicator", fontSize: 20, color: Colors.Black),
                    new Text(
                        "Pull the list down from its top edge. Cycle Material/adaptive/no-spinner paths and theme the refresh surface.",
                        fontSize: 14,
                        color: Color.Parse("#8A000000")),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            BuildButton(VariantLabel(), () => SetState(() => _variant = (_variant + 1) % 3), 126),
                            BuildButton(_useCupertinoPlatform ? "platform=iOS" : "platform=Android", () => SetState(() => _useCupertinoPlatform = !_useCupertinoPlatform), 132),
                            BuildButton(_useThemeOverrides ? "theme=on" : "theme=off", () => SetState(() => _useThemeOverrides = !_useThemeOverrides), 104),
                        ]),
                    new Row(
                        children:
                        [
                            BuildButton(
                                _useSchemeColor ? "color=scheme" : "color=widget",
                                () => SetState(() => _useSchemeColor = !_useSchemeColor),
                                126),
                            new Text(
                                "scheme teal; legacy primary orange",
                                fontSize: 12,
                                color: Color.Parse("#FF607D8B")),
                        ]),
                    new Text(
                        $"status={_status}, refreshCount={_refreshCount}; drag past the armed threshold, then release",
                        fontSize: 12,
                        color: Color.Parse("#FF607D8B")),
                    new Expanded(child: BuildRefreshWrapper(BuildList())),
                ]));
    }

    private Widget BuildRefreshWrapper(Widget child)
    {
        return _variant switch
        {
            1 => RefreshIndicator.Adaptive(
                onRefresh: HandleRefresh,
                child: child,
                color: _useSchemeColor ? null : Color.Parse("#FF1565C0"),
                semanticsLabel: "Refresh sample list"),
            2 => RefreshIndicator.NoSpinner(
                onRefresh: HandleRefresh,
                onStatusChange: status => SetState(() => _status = status?.ToString().ToLowerInvariant() ?? "idle"),
                child: child,
                semanticsLabel: "Refresh sample list"),
            _ => new RefreshIndicator(
                onRefresh: HandleRefresh,
                child: child,
                color: _useSchemeColor ? null : Color.Parse("#FF1565C0"),
                backgroundColor: _useThemeOverrides ? null : Colors.White,
                semanticsLabel: "Refresh sample list"),
        };
    }

    private Widget BuildList()
    {
        return ListView.Builder(
            itemCount: 24,
            itemExtent: 54,
            padding: new Thickness(8),
            addAutomaticKeepAlives: false,
            itemBuilder: (_, index) => new Container(
                color: index % 2 == 0 ? Colors.White : Color.Parse("#FFF5F7FA"),
                padding: new Thickness(12, 10),
                child: new Text($"refresh row #{index + 1}", fontSize: 13, color: Colors.Black)));
    }

    private async Task HandleRefresh()
    {
        if (Mounted) SetState(() => _status = "refresh");
        await Task.Delay(650);
        if (Mounted)
        {
            SetState(() =>
            {
                _refreshCount += 1;
                _status = "done";
            });
        }
    }

    private string VariantLabel() => _variant switch
    {
        1 => "adaptive",
        2 => "noSpinner",
        _ => "material",
    };

    private static Widget BuildButton(string label, Action onTap, double width) =>
        new SizedBox(
            width: width,
            child: new TextButton(
                onPressed: onTap,
                child: new Text(label, fontSize: 12),
                style: TextButton.StyleFrom(
                    foregroundColor: Colors.Black,
                    backgroundColor: Color.Parse("#FFE9F0FF"),
                    padding: new Thickness(10, 8),
                    minimumSize: new Size(64, 36),
                    shape: new RoundedRectangleBorder(
                        borderRadius: BorderRadius.Circular(8)))));
}
