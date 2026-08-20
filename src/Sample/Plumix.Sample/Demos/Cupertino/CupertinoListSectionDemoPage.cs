using System.Threading.Tasks;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source (reference): dart_sample/lib/demos/cupertino/cupertino_list_section_demo_page.dart
// (exact sample parity)

public sealed class CupertinoListSectionDemoPage : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        Color label = CupertinoDynamicColor.Resolve(CupertinoColors.Label, context);
        Color secondaryLabel = CupertinoDynamicColor.Resolve(CupertinoColors.SecondaryLabel, context);
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12.0,
            children:
            [
                new Text("Cupertino list + form sections", fontSize: 20.0, color: label),
                new Text(
                    "Compare list rows with split form rows, helper/error content, and inset-grouped decoration.",
                    fontSize: 14.0,
                    color: secondaryLabel),
                new CupertinoListSection(
                    header: new Text("CONNECTIVITY"),
                    footer: new Text("The base section draws full-width borders above and below its rows."),
                    children:
                    [
                        new CupertinoListTile(
                            title: new Text("Wi-Fi"),
                            additionalInfo: new Text("Studio"),
                            leading: new Icon(CupertinoIcons.Wifi),
                            trailing: new CupertinoListTileChevron(),
                            onTap: CompleteImmediately),
                        new CupertinoListTile(
                            title: new Text("Bluetooth"),
                            additionalInfo: new Text("On"),
                            leading: new Icon(CupertinoIcons.Bluetooth),
                            trailing: new CupertinoListTileChevron(),
                            onTap: CompleteImmediately),
                    ]),
                CupertinoListSection.InsetGrouped(
                    header: new Text("Account"),
                    footer: new Text("Inset groups clip their rows to a 10 px rounded superellipse."),
                    hasLeading: false,
                    separatorColor: CupertinoColors.SystemGrey4,
                    children:
                    [
                        CupertinoListTile.Notched(
                            title: new Text("Profile"),
                            additionalInfo: new Text("Egor"),
                            trailing: new CupertinoListTileChevron(),
                            onTap: CompleteImmediately),
                        CupertinoListTile.Notched(
                            title: new Text("Subscriptions"),
                            trailing: new CupertinoListTileChevron(),
                            onTap: CompleteImmediately),
                    ]),
                CupertinoFormSection.InsetGrouped(
                    header: new Text("PROFILE"),
                    footer: new Text("Form rows keep values trailing-aligned and supporting text below the row."),
                    children:
                    [
                        new CupertinoFormRow(
                            prefix: new Text("Name"),
                            child: new Text("Egor"),
                            helper: new Text("Shown on shared pages.")),
                        new CupertinoFormRow(
                            prefix: new Text("Email"),
                            child: new Text("egor@example.com"),
                            error: new Text("Address has not been verified.")),
                    ]),
            ]);
    }

    private static Task CompleteImmediately() => Task.CompletedTask;
}
