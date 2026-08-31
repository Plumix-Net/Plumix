using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source (reference): dart_sample/lib/demos/cupertino/app_demo_page.dart (exact sample parity)

public sealed class CupertinoAppDemoPage : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        return new CupertinoApp(
            debugShowCheckedModeBanner: false,
            title: "CupertinoApp demo",
            theme: new CupertinoThemeData(primaryColor: CupertinoColors.SystemIndigo),
            routes: new Dictionary<string, WidgetBuilder>
            {
                ["/"] = BuildHome,
                ["/details"] = BuildDetails,
            });
    }

    private static Widget BuildHome(BuildContext context)
    {
        CupertinoThemeData theme = CupertinoTheme.Of(context);
        Color label = CupertinoDynamicColor.Resolve(CupertinoColors.Label, context);
        Color secondaryLabel = CupertinoDynamicColor.Resolve(CupertinoColors.SecondaryLabel, context);
        return BuildPage(
            children:
            [
                new Text("CupertinoApp", fontSize: 22.0, color: label),
                new Text(
                    "The nested shell supplies Cupertino theme, localization, selection, scroll, and route defaults.",
                    fontSize: 14.0,
                    color: secondaryLabel),
                new Text(
                    $"locale action: {CupertinoLocalizations.Of(context).SelectAllButtonLabel}",
                    fontSize: 13.0,
                    color: secondaryLabel),
                new CupertinoButton(
                    color: theme.PrimaryColor.Value,
                    onPressed: () => Navigator.Of(context).PushNamed("/details"),
                    child: new Text("Push Cupertino route", color: theme.PrimaryContrastingColor.Value)),
            ]);
    }

    private static Widget BuildDetails(BuildContext context)
    {
        CupertinoThemeData theme = CupertinoTheme.Of(context);
        Color label = CupertinoDynamicColor.Resolve(CupertinoColors.Label, context);
        return BuildPage(
            children:
            [
                new Text("Details route", fontSize: 22.0, color: label),
                new CupertinoButton(
                    color: theme.PrimaryColor.Value,
                    onPressed: () => Navigator.Of(context).Pop(),
                    child: new Text("Pop route", color: theme.PrimaryContrastingColor.Value)),
            ]);
    }

    private static Widget BuildPage(IReadOnlyList<Widget> children)
    {
        return new CupertinoPageScaffold(
            child: new SafeArea(
                minimum: EdgeInsets.All(20.0),
                child: new Center(
                    child: new Column(
                        mainAxisSize: MainAxisSize.Min,
                        spacing: 14.0,
                        children: children))));
    }
}
