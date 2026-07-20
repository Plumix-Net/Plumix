using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using IOPath = System.IO.Path;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/about.dart
public sealed class AboutListTile : StatelessWidget
{
    public AboutListTile(
        Widget? icon = null,
        Widget? child = null,
        string? applicationName = null,
        string? applicationVersion = null,
        Widget? applicationIcon = null,
        string? applicationLegalese = null,
        IReadOnlyList<Widget>? aboutBoxChildren = null,
        bool? dense = null,
        Key? key = null) : base(key)
    {
        Icon = icon;
        Child = child;
        ApplicationName = applicationName;
        ApplicationVersion = applicationVersion;
        ApplicationIcon = applicationIcon;
        ApplicationLegalese = applicationLegalese;
        AboutBoxChildren = aboutBoxChildren;
        Dense = dense;
    }

    public Widget? Icon { get; }
    public Widget? Child { get; }
    public string? ApplicationName { get; }
    public string? ApplicationVersion { get; }
    public Widget? ApplicationIcon { get; }
    public string? ApplicationLegalese { get; }
    public IReadOnlyList<Widget>? AboutBoxChildren { get; }
    public bool? Dense { get; }

    public override Widget Build(BuildContext context)
    {
        string name = ApplicationName ?? AboutDialogs.DefaultApplicationName;
        return new ListTile(
            leading: Icon,
            title: Child ?? new Text(MaterialLocalizations.Of(context).AboutListTileTitle(name)),
            dense: Dense,
            onTap: () => _ = AboutDialogs.ShowAboutDialog(
                context,
                applicationName: ApplicationName,
                applicationVersion: ApplicationVersion,
                applicationIcon: ApplicationIcon,
                applicationLegalese: ApplicationLegalese,
                children: AboutBoxChildren));
    }
}

public sealed class AboutDialog : StatelessWidget
{
    private const double TextVerticalSeparation = 18;

    public AboutDialog(
        string? applicationName = null,
        string? applicationVersion = null,
        Widget? applicationIcon = null,
        string? applicationLegalese = null,
        IReadOnlyList<Widget>? children = null,
        Key? key = null) : base(key)
    {
        ApplicationName = applicationName;
        ApplicationVersion = applicationVersion;
        ApplicationIcon = applicationIcon;
        ApplicationLegalese = applicationLegalese;
        Children = children;
    }

    public string? ApplicationName { get; }
    public string? ApplicationVersion { get; }
    public Widget? ApplicationIcon { get; }
    public string? ApplicationLegalese { get; }
    public IReadOnlyList<Widget>? Children { get; }

    public static AboutDialog Adaptive(
        string? applicationName = null,
        string? applicationVersion = null,
        Widget? applicationIcon = null,
        string? applicationLegalese = null,
        IReadOnlyList<Widget>? children = null,
        Key? key = null) => new(
        applicationName,
        applicationVersion,
        applicationIcon,
        applicationLegalese,
        children,
        key);

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var localizations = MaterialLocalizations.Of(context);
        string name = ApplicationName ?? AboutDialogs.DefaultApplicationName;
        string version = ApplicationVersion ?? string.Empty;
        var information = new List<Widget>
        {
            new DefaultTextStyle(theme.TextTheme.HeadlineSmall, new Text(name)),
            new DefaultTextStyle(theme.TextTheme.BodyMedium, new Text(version)),
            new SizedBox(height: TextVerticalSeparation),
            new DefaultTextStyle(theme.TextTheme.BodySmall, new Text(ApplicationLegalese ?? string.Empty)),
        };
        var row = new List<Widget>();
        if (ApplicationIcon is not null)
        {
            row.Add(new IconTheme(theme.IconTheme, ApplicationIcon));
        }
        row.Add(new Expanded(new Padding(
            new Thickness(24, 0),
            new Column(
                mainAxisSize: MainAxisSize.Min,
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children: information))));

        var content = new List<Widget> { new Row(crossAxisAlignment: CrossAxisAlignment.Start, children: row) };
        if (Children is not null) content.AddRange(Children);
        string licenseLabel = theme.UseMaterial3
            ? localizations.ViewLicensesButtonLabel
            : localizations.ViewLicensesButtonLabel.ToUpperInvariant();
        string closeLabel = theme.UseMaterial3
            ? localizations.CloseButtonLabel
            : localizations.CloseButtonLabel.ToUpperInvariant();

        return new AlertDialog(
            content: new ListBody(content),
            actions:
            [
                new TextButton(
                    child: new Text(licenseLabel),
                    onPressed: () => AboutDialogs.ShowLicensePage(
                        context,
                        applicationName: ApplicationName,
                        applicationVersion: ApplicationVersion,
                        applicationIcon: ApplicationIcon,
                        applicationLegalese: ApplicationLegalese)),
                new TextButton(
                    child: new Text(closeLabel),
                    onPressed: () => Navigator.MaybeOf(context)?.MaybePop()),
            ],
            scrollable: true);
    }
}

public sealed class LicensePage : StatefulWidget
{
    public LicensePage(
        string? applicationName = null,
        string? applicationVersion = null,
        Widget? applicationIcon = null,
        string? applicationLegalese = null,
        Key? key = null) : base(key)
    {
        ApplicationName = applicationName;
        ApplicationVersion = applicationVersion;
        ApplicationIcon = applicationIcon;
        ApplicationLegalese = applicationLegalese;
    }

    public string? ApplicationName { get; }
    public string? ApplicationVersion { get; }
    public Widget? ApplicationIcon { get; }
    public string? ApplicationLegalese { get; }

    public override State CreateState() => new LicensePageState();

    private sealed class LicensePageState : State
    {
        private LicenseData? _data;
        private Exception? _error;
        private CancellationTokenSource? _loadCancellation;

        private LicensePage CurrentWidget => (LicensePage)StateWidget;

        public override void InitState()
        {
            _loadCancellation = new CancellationTokenSource();
            _ = LoadLicensesAsync(_loadCancellation.Token);
        }

        public override void Dispose()
        {
            _loadCancellation?.Cancel();
            _loadCancellation?.Dispose();
            _loadCancellation = null;
        }

        public override Widget Build(BuildContext context)
        {
            var localizations = MaterialLocalizations.Of(context);
            Widget body;
            if (_error is not null)
            {
                body = new Center(child: new Text(_error.Message));
            }
            else if (_data is null)
            {
                body = new Column(
                    mainAxisSize: MainAxisSize.Min,
                    children: [BuildAboutProgram(context), new Center(child: new CircularProgressIndicator())]);
            }
            else
            {
                var rows = new List<Widget> { BuildAboutProgram(context) };
                for (int index = 0; index < _data.Packages.Count; index++)
                {
                    int captured = index;
                    string package = _data.Packages[index];
                    var entries = _data.EntriesFor(package);
                    rows.Add(new ListTile(
                        title: new Text(package),
                        subtitle: new Text(localizations.LicensesPackageDetailText(entries.Count)),
                        onTap: () => ShowPackage(context, captured)));
                }
                body = new Center(child: new ConstrainedBox(
                    new BoxConstraints(MaxWidth: 600),
                    new Card(
                        elevation: 4,
                        margin: default,
                        child: new ListView(children: rows))));
            }

            return new Scaffold(
                appBar: new AppBar(titleText: localizations.LicensesPageTitle),
                body: body);
        }

        private Widget BuildAboutProgram(BuildContext context)
        {
            var theme = Theme.Of(context);
            double width = MediaQuery.MaybeOf(context)?.Size.Width ?? 0;
            double gutter = width >= 720 ? 24.0 : 12.0;
            var children = new List<Widget>
            {
                new DefaultTextStyle(
                    theme.TextTheme.HeadlineSmall,
                    new Text(CurrentWidget.ApplicationName ?? AboutDialogs.DefaultApplicationName, textAlign: TextAlign.Center)),
            };
            if (CurrentWidget.ApplicationIcon is not null)
            {
                children.Add(new IconTheme(theme.IconTheme, CurrentWidget.ApplicationIcon));
            }
            string version = CurrentWidget.ApplicationVersion ?? string.Empty;
            if (version.Length > 0)
            {
                children.Add(new Padding(
                    new Thickness(0, 0, 0, 18),
                    new DefaultTextStyle(theme.TextTheme.BodyMedium, new Text(version, textAlign: TextAlign.Center))));
            }
            if (!string.IsNullOrEmpty(CurrentWidget.ApplicationLegalese))
            {
                children.Add(new DefaultTextStyle(
                    theme.TextTheme.BodySmall,
                    new Text(CurrentWidget.ApplicationLegalese!, textAlign: TextAlign.Center)));
            }
            children.Add(new SizedBox(height: 18));
            children.Add(new DefaultTextStyle(
                theme.TextTheme.BodyMedium,
                new Text("Powered by Flutter", textAlign: TextAlign.Center)));
            return new Padding(
                new Thickness(gutter, 24),
                new Column(mainAxisSize: MainAxisSize.Min, children: children));
        }

        private void ShowPackage(BuildContext context, int packageIndex)
        {
            if (_data is null) return;
            string package = _data.Packages[packageIndex];
            var entries = _data.EntriesFor(package);
            Navigator.Of(context).Push(new BuilderPageRoute(
                builder: _ => new PackageLicensePage(package, entries)));
        }

        private async Task LoadLicensesAsync(CancellationToken cancellationToken)
        {
            try
            {
                var data = new LicenseData();
                await foreach (var entry in LicenseRegistry.Licenses(cancellationToken))
                {
                    data.AddLicense(entry);
                }
                data.SortPackages();
                if (!Mounted || cancellationToken.IsCancellationRequested) return;
                SetState(() => _data = data);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                if (!Mounted) return;
                SetState(() => _error = exception);
            }
        }
    }

    private sealed class LicenseData
    {
        private readonly List<LicenseEntry> _licenses = [];
        private readonly Dictionary<string, List<int>> _bindings = new(StringComparer.Ordinal);
        private string? _firstPackage;

        public List<string> Packages { get; } = [];

        public void AddLicense(LicenseEntry entry)
        {
            foreach (string package in entry.Packages)
            {
                if (!_bindings.TryGetValue(package, out var indexes))
                {
                    indexes = [];
                    _bindings[package] = indexes;
                    _firstPackage ??= package;
                    Packages.Add(package);
                }
                indexes.Add(_licenses.Count);
            }
            _licenses.Add(entry);
        }

        public IReadOnlyList<LicenseEntry> EntriesFor(string package) =>
            _bindings[package].Select(index => _licenses[index]).ToArray();

        public void SortPackages()
        {
            Packages.Sort((left, right) =>
            {
                if (left == _firstPackage) return -1;
                if (right == _firstPackage) return 1;
                return StringComparer.OrdinalIgnoreCase.Compare(left, right);
            });
        }
    }
}

internal sealed class PackageLicensePage : StatelessWidget
{
    public PackageLicensePage(string packageName, IReadOnlyList<LicenseEntry> licenseEntries)
    {
        PackageName = packageName;
        LicenseEntries = licenseEntries;
    }

    public string PackageName { get; }
    public IReadOnlyList<LicenseEntry> LicenseEntries { get; }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var localizations = MaterialLocalizations.Of(context);
        var content = new List<Widget>();
        foreach (var entry in LicenseEntries)
        {
            content.Add(new Padding(new Thickness(18), new Divider()));
            foreach (var paragraph in entry.Paragraphs)
            {
                Widget text = new Text(
                    paragraph.Text,
                    textAlign: paragraph.Indent == LicenseParagraph.CenteredIndent ? TextAlign.Center : TextAlign.Start,
                    fontWeight: paragraph.Indent == LicenseParagraph.CenteredIndent ? FontWeight.Bold : null);
                double start = paragraph.Indent < 0 ? 0 : paragraph.Indent * 16.0;
                var direction = Directionality.Of(context);
                text = new Padding(
                    direction == TextDirection.Rtl
                        ? new Thickness(0, 8, start, 0)
                        : new Thickness(start, 8, 0, 0),
                    text);
                content.Add(text);
            }
        }

        var safe = MediaQuery.MaybePaddingOf(context) ?? default;
        double width = MediaQuery.MaybeOf(context)?.Size.Width ?? 0;
        double gutter = width >= 720 ? 24.0 : 12.0;
        var padding = new Thickness(gutter + safe.Left, 0, gutter + safe.Right, gutter + safe.Bottom);
        Widget body = new Center(child: new ConstrainedBox(
            new BoxConstraints(MaxWidth: 600),
            new Card(
                elevation: 4,
                margin: default,
                child: new Scrollbar(
                    child: new SingleChildScrollView(
                        padding: padding,
                        child: new Column(
                            mainAxisSize: MainAxisSize.Min,
                            crossAxisAlignment: CrossAxisAlignment.Stretch,
                            children: content))))));
        return new DefaultTextStyle(
            theme.TextTheme.BodySmall,
            new Scaffold(
                appBar: new AppBar(title: new Column(
                    mainAxisSize: MainAxisSize.Min,
                    crossAxisAlignment: CrossAxisAlignment.Start,
                    children:
                    [
                        new Text(PackageName),
                        new Text(localizations.LicensesPackageDetailText(LicenseEntries.Count), fontSize: 12),
                    ])),
                body: body));
    }
}

public static class AboutDialogs
{
    public static string DefaultApplicationName =>
        IOPath.GetFileName(Environment.ProcessPath) is { Length: > 0 } name ? name : "application";

    public static Task<object?> ShowAboutDialog(
        BuildContext context,
        string? applicationName = null,
        string? applicationVersion = null,
        Widget? applicationIcon = null,
        string? applicationLegalese = null,
        IReadOnlyList<Widget>? children = null,
        bool barrierDismissible = true,
        Color? barrierColor = null,
        string? barrierLabel = null,
        bool useRootNavigator = true,
        RouteSettings? routeSettings = null)
    {
        return MaterialDialogs.ShowDialog<object>(
            context,
            _ => new AboutDialog(
                applicationName,
                applicationVersion,
                applicationIcon,
                applicationLegalese,
                children),
            barrierDismissible,
            barrierColor,
            barrierLabel,
            routeSettings: routeSettings,
            useRootNavigator: useRootNavigator);
    }

    public static Task<object?> ShowAdaptiveAboutDialog(
        BuildContext context,
        string? applicationName = null,
        string? applicationVersion = null,
        Widget? applicationIcon = null,
        string? applicationLegalese = null,
        IReadOnlyList<Widget>? children = null,
        bool barrierDismissible = true,
        Color? barrierColor = null,
        string? barrierLabel = null,
        bool useRootNavigator = true,
        RouteSettings? routeSettings = null)
    {
        return MaterialDialogs.ShowDialog<object>(
            context,
            _ => AboutDialog.Adaptive(
                applicationName,
                applicationVersion,
                applicationIcon,
                applicationLegalese,
                children),
            barrierDismissible,
            barrierColor,
            barrierLabel,
            routeSettings: routeSettings,
            useRootNavigator: useRootNavigator);
    }

    public static void ShowLicensePage(
        BuildContext context,
        string? applicationName = null,
        string? applicationVersion = null,
        Widget? applicationIcon = null,
        string? applicationLegalese = null,
        bool useRootNavigator = false)
    {
        Navigator.Of(context, rootNavigator: useRootNavigator).Push(new BuilderPageRoute(
            builder: _ => new LicensePage(
                applicationName,
                applicationVersion,
                applicationIcon,
                applicationLegalese)));
    }
}
