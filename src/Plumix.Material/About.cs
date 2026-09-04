using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using IOPath = System.IO.Path;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/about.dart

/// <summary>Dart's private `_MasterViewBuilder`.</summary>
internal delegate Widget MasterViewBuilder(BuildContext context, bool isLateralUI);

/// <summary>Dart's private `_DetailPageBuilder`.</summary>
internal delegate Widget DetailPageBuilder(
    BuildContext context,
    object? arguments,
    ScrollController? scrollController);

/// <summary>Dart's private `_ActionBuilder`.</summary>
internal delegate IReadOnlyList<Widget> MasterDetailActionBuilder(BuildContext context, ActionLevel actionLevel);

/// <summary>Dart's private `_ActionLevel`.</summary>
internal enum ActionLevel
{
    /// <summary>Actions on the master-detail lateral top app bar.</summary>
    Top,

    /// <summary>Actions on the master view's app bar.</summary>
    View,
}

/// <summary>Dart's private `_LayoutMode`.</summary>
internal enum LayoutMode
{
    Lateral,
    Nested,
}

/// <summary>Dart's private `_Focus`; renamed because `Focus` is a core widget.</summary>
internal enum MasterDetailFocus
{
    Master,
    Detail,
}

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
        return new ListTile(
            leading: Icon,
            title: Child ?? new Text(MaterialLocalizations.Of(context).AboutListTileTitle(
                ApplicationName ?? AboutDialogs.DefaultApplicationNameOf(context))),
            dense: Dense,
            onTap: () => AboutDialogs.ShowAboutDialog(
                context,
                applicationName: ApplicationName,
                applicationVersion: ApplicationVersion,
                applicationIcon: ApplicationIcon,
                applicationLegalese: ApplicationLegalese,
                children: AboutBoxChildren));
    }
}

public class AboutDialog : StatelessWidget
{
    internal const double TextVerticalSeparation = 18.0;

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

    /// <summary>Dart's `AboutDialog.adaptive`: Cupertino dialog actions on iOS/macOS.</summary>
    public static AboutDialog Adaptive(
        string? applicationName = null,
        string? applicationVersion = null,
        Widget? applicationIcon = null,
        string? applicationLegalese = null,
        IReadOnlyList<Widget>? children = null,
        Key? key = null) => new AdaptiveAboutDialog(
        applicationName,
        applicationVersion,
        applicationIcon,
        applicationLegalese,
        children,
        key);

    /// <summary>Dart's shared content tree for both the Material and the adaptive dialog.</summary>
    private protected Widget BuildContent(BuildContext context)
    {
        var themeData = Theme.Of(context);
        string name = ApplicationName ?? AboutDialogs.DefaultApplicationNameOf(context);
        string version = ApplicationVersion ?? AboutDialogs.DefaultApplicationVersionOf(context);
        Widget? icon = ApplicationIcon ?? AboutDialogs.DefaultApplicationIconOf(context);

        var row = new List<Widget>();
        if (icon is not null)
        {
            row.Add(new IconTheme(themeData.IconTheme, icon));
        }

        row.Add(new Expanded(new Padding(
            new Thickness(24.0, 0.0),
            new ListBody(
            [
                new Text(name, style: themeData.TextTheme.HeadlineSmall),
                new Text(version, style: themeData.TextTheme.BodyMedium),
                new SizedBox(height: TextVerticalSeparation),
                new Text(ApplicationLegalese ?? string.Empty, style: themeData.TextTheme.BodySmall),
            ]))));

        var content = new List<Widget>
        {
            new Row(crossAxisAlignment: CrossAxisAlignment.Start, children: row),
        };
        if (Children is not null)
        {
            content.AddRange(Children);
        }

        return new ListBody(content);
    }

    private protected string ViewLicensesLabel(BuildContext context)
    {
        var localizations = MaterialLocalizations.Of(context);
        return Theme.Of(context).UseMaterial3
            ? localizations.ViewLicensesButtonLabel
            : localizations.ViewLicensesButtonLabel.ToUpperInvariant();
    }

    private protected string CloseLabel(BuildContext context)
    {
        var localizations = MaterialLocalizations.Of(context);
        return Theme.Of(context).UseMaterial3
            ? localizations.CloseButtonLabel
            : localizations.CloseButtonLabel.ToUpperInvariant();
    }

    private protected void OnViewLicensesPressed(BuildContext context)
    {
        AboutDialogs.ShowLicensePage(
            context,
            applicationName: ApplicationName,
            applicationVersion: ApplicationVersion,
            applicationIcon: ApplicationIcon,
            applicationLegalese: ApplicationLegalese);
    }

    public override Widget Build(BuildContext context)
    {
        return new AlertDialog(
            content: BuildContent(context),
            actions:
            [
                new TextButton(
                    child: new Text(ViewLicensesLabel(context)),
                    onPressed: () => OnViewLicensesPressed(context)),
                new TextButton(
                    child: new Text(CloseLabel(context)),
                    onPressed: () => Navigator.Of(context).Pop()),
            ],
            scrollable: true);
    }
}

/// <summary>Dart's private `_AdaptiveAboutDialog`.</summary>
internal sealed class AdaptiveAboutDialog : AboutDialog
{
    public AdaptiveAboutDialog(
        string? applicationName,
        string? applicationVersion,
        Widget? applicationIcon,
        string? applicationLegalese,
        IReadOnlyList<Widget>? children,
        Key? key) : base(
        applicationName,
        applicationVersion,
        applicationIcon,
        applicationLegalese,
        children,
        key)
    {
    }

    private IReadOnlyList<Widget> Actions(BuildContext context)
    {
        switch (Theme.Of(context).Platform)
        {
            case TargetPlatform.IOS:
            case TargetPlatform.MacOS:
                return
                [
                    new CupertinoDialogAction(
                        child: new Text(ViewLicensesLabel(context)),
                        onPressed: () => OnViewLicensesPressed(context)),
                    new CupertinoDialogAction(
                        child: new Text(CloseLabel(context)),
                        onPressed: () => Navigator.Of(context).Pop()),
                ];
            default:
                return
                [
                    new TextButton(
                        child: new Text(ViewLicensesLabel(context)),
                        onPressed: () => OnViewLicensesPressed(context)),
                    new TextButton(
                        child: new Text(CloseLabel(context)),
                        onPressed: () => Navigator.Of(context).Pop()),
                ];
        }
    }

    public override Widget Build(BuildContext context)
    {
        return AlertDialog.Adaptive(
            content: BuildContent(context),
            actions: Actions(context),
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
}

/// <summary>Dart's private `_LicensePageState`.</summary>
internal sealed class LicensePageState : State
{
    private readonly ValueNotifier<int?> _selectedId = new(null);

    private LicensePage CurrentWidget => (LicensePage)StateWidget;

    public override void Dispose()
    {
        _selectedId.Dispose();
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return new MasterDetailFlow(
            detailPageBuilder: PackageLicensePageBuilder,
            masterViewBuilder: PackagesViewBuilder,
            detailPageFABlessGutterWidth: AboutDialogs.GutterSize(context),
            title: new Text(MaterialLocalizations.Of(context).LicensesPageTitle));
    }

    private Widget PackagesViewBuilder(BuildContext context, bool isLateral)
    {
        Widget about = new AboutProgram(
            name: CurrentWidget.ApplicationName ?? AboutDialogs.DefaultApplicationNameOf(context),
            icon: CurrentWidget.ApplicationIcon ?? AboutDialogs.DefaultApplicationIconOf(context),
            version: CurrentWidget.ApplicationVersion ?? AboutDialogs.DefaultApplicationVersionOf(context),
            legalese: CurrentWidget.ApplicationLegalese);
        return new PackagesView(about: about, isLateral: isLateral, selectedId: _selectedId);
    }

    private static Widget PackageLicensePageBuilder(
        BuildContext context,
        object? arguments,
        ScrollController? scrollController)
    {
        _ = context;
        if (arguments is not DetailArguments detail)
        {
            throw new ArgumentException("Detail arguments must be _DetailArguments.", nameof(arguments));
        }

        return new PackageLicensePage(
            packageName: detail.PackageName,
            licenseEntries: detail.LicenseEntries,
            scrollController: scrollController);
    }
}

/// <summary>Dart's private `_AboutProgram`.</summary>
internal sealed class AboutProgram : StatelessWidget
{
    public AboutProgram(string name, string version, Widget? icon = null, string? legalese = null, Key? key = null)
        : base(key)
    {
        Name = name;
        Version = version;
        Icon = icon;
        Legalese = legalese;
    }

    public string Name { get; }
    public string Version { get; }
    public Widget? Icon { get; }
    public string? Legalese { get; }

    public override Widget Build(BuildContext context)
    {
        var textTheme = Theme.Of(context).TextTheme;
        var children = new List<Widget>
        {
            new Text(Name, style: textTheme.HeadlineSmall, textAlign: TextAlign.Center),
        };

        if (Icon is not null)
        {
            children.Add(new IconTheme(Theme.Of(context).IconTheme, Icon));
        }

        if (Version != string.Empty)
        {
            children.Add(new Padding(
                new Thickness(0.0, 0.0, 0.0, AboutDialog.TextVerticalSeparation),
                new Text(Version, style: textTheme.BodyMedium, textAlign: TextAlign.Center)));
        }

        if (Legalese is not null && Legalese != string.Empty)
        {
            children.Add(new Text(Legalese, style: textTheme.BodySmall, textAlign: TextAlign.Center));
        }

        children.Add(new SizedBox(height: AboutDialog.TextVerticalSeparation));
        children.Add(new Text("Powered by Flutter", style: textTheme.BodyMedium, textAlign: TextAlign.Center));

        return new Padding(
            new Thickness(AboutDialogs.GutterSize(context), 24.0),
            new Column(children: children));
    }
}

/// <summary>Dart's private `_PackagesView`.</summary>
internal sealed class PackagesView : StatefulWidget
{
    public PackagesView(Widget about, bool isLateral, ValueNotifier<int?> selectedId, Key? key = null) : base(key)
    {
        About = about;
        IsLateral = isLateral;
        SelectedId = selectedId;
    }

    public Widget About { get; }
    public bool IsLateral { get; }
    public ValueNotifier<int?> SelectedId { get; }

    public override State CreateState() => new PackagesViewState();
}

/// <summary>Dart's private `_PackagesViewState`.</summary>
internal sealed class PackagesViewState : State
{
    private readonly Task<LicenseData> _licenses = LoadLicensesAsync();

    private PackagesView CurrentWidget => (PackagesView)StateWidget;

    private static async Task<LicenseData> LoadLicensesAsync()
    {
        var data = new LicenseData();
        await foreach (var license in LicenseRegistry.Licenses().ConfigureAwait(false))
        {
            data.AddLicense(license);
        }

        data.SortPackages();
        return data;
    }

    public override Widget Build(BuildContext context)
    {
        return new FutureBuilder<LicenseData>(
            _licenses,
            (_, snapshot) => new LayoutBuilder(
                (layoutContext, _) => BuildForState(layoutContext, snapshot),
                key: new ValueKey<ConnectionState>(snapshot.ConnectionState)));
    }

    private Widget BuildForState(BuildContext context, AsyncSnapshot<LicenseData> snapshot)
    {
        switch (snapshot.ConnectionState)
        {
            case ConnectionState.Done:
                if (snapshot.HasError)
                {
                    return new Center(child: new Text(snapshot.Error!.ToString()));
                }

                InitDefaultDetailPage(snapshot.Data!, context);
                return new ValueListenableBuilder<int?>(
                    CurrentWidget.SelectedId,
                    (builderContext, selectedId, _) => new Center(
                        child: new Material(
                            color: Theme.Of(builderContext).CardColor,
                            elevation: AboutDialogs.CardElevation,
                            child: new ConstrainedBox(
                                new BoxConstraints(MaxWidth: 600.0),
                                BuildPackagesList(
                                    builderContext,
                                    selectedId,
                                    snapshot.Data!,
                                    CurrentWidget.IsLateral)))));
            default:
                return new Material(
                    color: Theme.Of(context).CardColor,
                    child: new Column(children:
                    [
                        CurrentWidget.About,
                        new Center(child: new CircularProgressIndicator()),
                    ]));
        }
    }

    private void InitDefaultDetailPage(LicenseData data, BuildContext context)
    {
        if (data.Packages.Count == 0)
        {
            return;
        }

        string packageName = data.Packages[CurrentWidget.SelectedId.Value ?? 0];
        var bindings = data.PackageLicenseBindings[packageName];
        MasterDetailFlow.Of(context).SetInitialDetailPage(
            new DetailArguments(packageName, bindings.Select(index => data.Licenses[index]).ToArray()));
    }

    private Widget BuildPackagesList(
        BuildContext context,
        int? selectedId,
        LicenseData data,
        bool drawSelection)
    {
        var safeAreaPadding = MediaQuery.PaddingOf(context);
        var padding = new Thickness(safeAreaPadding.Left, 0.0, safeAreaPadding.Right, safeAreaPadding.Bottom);
        return ListView.Builder(
            itemCount: data.Packages.Count + 1,
            itemBuilder: (itemContext, index) =>
            {
                if (index == 0)
                {
                    return CurrentWidget.About;
                }

                int packageIndex = index - 1;
                string packageName = data.Packages[packageIndex];
                var bindings = data.PackageLicenseBindings[packageName];
                return new PackageListTile(
                    packageName: packageName,
                    index: packageIndex,
                    isSelected: drawSelection && packageIndex == (selectedId ?? 0),
                    numberLicenses: bindings.Count,
                    onTap: () =>
                    {
                        CurrentWidget.SelectedId.Value = packageIndex;
                        MasterDetailFlow.Of(itemContext).OpenDetailPage(new DetailArguments(
                            packageName,
                            bindings.Select(binding => data.Licenses[binding]).ToArray()));
                    });
            },
            padding: padding);
    }
}

/// <summary>Dart's private `_PackageListTile`.</summary>
internal sealed class PackageListTile : StatelessWidget
{
    public PackageListTile(
        string packageName,
        bool isSelected,
        int numberLicenses,
        int? index = null,
        Action? onTap = null,
        Key? key = null) : base(key)
    {
        PackageName = packageName;
        IsSelected = isSelected;
        NumberLicenses = numberLicenses;
        Index = index;
        OnTap = onTap;
    }

    public string PackageName { get; }
    public int? Index { get; }
    public bool IsSelected { get; }
    public int NumberLicenses { get; }
    public Action? OnTap { get; }

    public override Widget Build(BuildContext context)
    {
        return new Ink(
            color: IsSelected ? Theme.Of(context).HighlightColor : Theme.Of(context).CardColor,
            child: new ListTile(
                title: new Text(PackageName),
                subtitle: new Text(MaterialLocalizations.Of(context).LicensesPackageDetailText(NumberLicenses)),
                selected: IsSelected,
                onTap: OnTap));
    }
}

/// <summary>Dart's private `_LicenseData`.</summary>
internal sealed class LicenseData
{
    public List<LicenseEntry> Licenses { get; } = [];

    public Dictionary<string, List<int>> PackageLicenseBindings { get; } = new(StringComparer.Ordinal);

    public List<string> Packages { get; } = [];

    /// <summary>The first package listed, which is assumed to be the package itself.</summary>
    public string? FirstPackage { get; private set; }

    public void AddLicense(LicenseEntry entry)
    {
        // Before the license can be added, we must first record the packages it belongs to.
        foreach (string package in entry.Packages)
        {
            AddPackage(package);
            PackageLicenseBindings[package].Add(Licenses.Count);
        }

        Licenses.Add(entry);
    }

    private void AddPackage(string package)
    {
        if (PackageLicenseBindings.ContainsKey(package))
        {
            return;
        }

        PackageLicenseBindings[package] = [];
        FirstPackage ??= package;
        Packages.Add(package);
    }

    /// <summary>Sort the packages using some comparison method, or by the default manner, which is to
    /// put the application package first, followed by every other package in case-insensitive
    /// alphabetical order.</summary>
    public void SortPackages(Comparison<string>? compare = null)
    {
        Packages.Sort(compare ?? DefaultCompare);
    }

    private int DefaultCompare(string left, string right)
    {
        // Based on how LicenseRegistry currently behaves, the first package returned is the end user
        // application license. This should be presented first in the list.
        if (left == FirstPackage)
        {
            return -1;
        }

        if (right == FirstPackage)
        {
            return 1;
        }

        return string.CompareOrdinal(left.ToLowerInvariant(), right.ToLowerInvariant());
    }
}

/// <summary>Dart's private `_DetailArguments`.</summary>
internal sealed class DetailArguments : IEquatable<DetailArguments>
{
    public DetailArguments(string packageName, IReadOnlyList<LicenseEntry> licenseEntries)
    {
        PackageName = packageName;
        LicenseEntries = licenseEntries;
    }

    public string PackageName { get; }

    public IReadOnlyList<LicenseEntry> LicenseEntries { get; }

    public bool Equals(DetailArguments? other) => other is not null && other.PackageName == PackageName;

    public override bool Equals(object? obj) => Equals(obj as DetailArguments);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(PackageName);
        foreach (var entry in LicenseEntries)
        {
            hash.Add(entry);
        }

        return hash.ToHashCode();
    }
}

/// <summary>Dart's private `_PackageLicensePage`.</summary>
internal sealed class PackageLicensePage : StatefulWidget
{
    public PackageLicensePage(
        string packageName,
        IReadOnlyList<LicenseEntry> licenseEntries,
        ScrollController? scrollController,
        Key? key = null) : base(key)
    {
        PackageName = packageName;
        LicenseEntries = licenseEntries;
        ScrollController = scrollController;
    }

    public string PackageName { get; }
    public IReadOnlyList<LicenseEntry> LicenseEntries { get; }
    public ScrollController? ScrollController { get; }

    public override State CreateState() => new PackageLicensePageState();
}

/// <summary>Dart's private `_PackageLicensePageState`.</summary>
internal sealed class PackageLicensePageState : State
{
    private static readonly Locale LicenseLocale = new("en", "US");

    private readonly List<Widget> _licenses = [];
    private bool _loaded;

    private PackageLicensePage CurrentWidget => (PackageLicensePage)StateWidget;

    public override void InitState()
    {
        base.InitState();
        _ = InitLicensesAsync();
    }

    private async Task InitLicensesAsync()
    {
        foreach (var license in CurrentWidget.LicenseEntries)
        {
            if (!Mounted)
            {
                return;
            }

            var paragraphs = await Scheduler.ScheduleTask(
                () => license.Paragraphs.ToList(),
                Priority.Animation,
                debugLabel: "License").ConfigureAwait(true);

            if (!Mounted)
            {
                return;
            }

            SetState(() =>
            {
                _licenses.Add(new Padding(new Thickness(18.0), new Divider()));
                foreach (var paragraph in paragraphs)
                {
                    if (paragraph.Indent == LicenseParagraph.CenteredIndent)
                    {
                        _licenses.Add(new Padding(
                            new Thickness(0.0, 16.0, 0.0, 0.0),
                            new Text(
                                paragraph.Text,
                                style: new TextStyle(FontWeight: FontWeight.Bold),
                                textAlign: TextAlign.Center)));
                    }
                    else
                    {
                        _licenses.Add(new Padding(
                            EdgeInsetsDirectional.Only(top: 8.0, start: 16.0 * paragraph.Indent),
                            new Text(paragraph.Text)));
                    }
                }
            });
        }

        if (!Mounted)
        {
            return;
        }

        SetState(() => _loaded = true);
    }

    public override Widget Build(BuildContext context)
    {
        var localizations = MaterialLocalizations.Of(context);
        var theme = Theme.Of(context);
        string title = CurrentWidget.PackageName;
        string subtitle = localizations.LicensesPackageDetailText(CurrentWidget.LicenseEntries.Count);
        double pad = AboutDialogs.GutterSize(context);
        var safeAreaPadding = MediaQuery.PaddingOf(context);
        var padding = new Thickness(
            pad + safeAreaPadding.Left,
            0.0,
            pad + safeAreaPadding.Right,
            pad + safeAreaPadding.Bottom);

        var listWidgets = new List<Widget>(_licenses);
        if (!_loaded)
        {
            listWidgets.Add(new Padding(
                new Thickness(0.0, 24.0),
                new Center(child: new CircularProgressIndicator())));
        }

        Widget page;
        if (CurrentWidget.ScrollController is null)
        {
            page = new Scaffold(
                appBar: new AppBar(title: new PackageLicensePageTitle(
                    title: title,
                    subtitle: subtitle,
                    theme: theme.UseMaterial3 ? theme.TextTheme : theme.PrimaryTextTheme,
                    titleTextStyle: theme.AppBarTheme.TitleTextStyle,
                    foregroundColor: theme.AppBarTheme.ForegroundColor)),
                body: new Center(child: new Material(
                    color: theme.CardColor,
                    elevation: AboutDialogs.CardElevation,
                    child: new ConstrainedBox(
                        new BoxConstraints(MaxWidth: 600.0),
                        Localizations.Override(
                            context,
                            new ScrollConfiguration(
                                ScrollConfiguration.Of(context).CopyWith(scrollbars: false),
                                new Scrollbar(
                                    child: new ListView(
                                        children: listWidgets,
                                        primary: true,
                                        padding: padding))),
                            locale: LicenseLocale)))));
        }
        else
        {
            page = new CustomScrollView(
                controller: CurrentWidget.ScrollController,
                slivers:
                [
                    new SliverAppBar(
                        automaticallyImplyLeading: false,
                        pinned: true,
                        backgroundColor: theme.CardColor,
                        title: new PackageLicensePageTitle(
                            title: title,
                            subtitle: subtitle,
                            theme: theme.TextTheme,
                            titleTextStyle: theme.TextTheme.TitleLarge)),
                    new SliverPadding(
                        padding,
                        SliverList.Builder(
                            itemCount: listWidgets.Count,
                            itemBuilder: (itemContext, index) => Localizations.Override(
                                itemContext,
                                listWidgets[index],
                                locale: LicenseLocale))),
                ]);
        }

        return new DefaultTextStyle(theme.TextTheme.BodySmall, page);
    }
}

/// <summary>Dart's private `_PackageLicensePageTitle`.</summary>
internal sealed class PackageLicensePageTitle : StatelessWidget
{
    public PackageLicensePageTitle(
        string title,
        string subtitle,
        TextTheme theme,
        TextStyle? titleTextStyle = null,
        Color? foregroundColor = null,
        Key? key = null) : base(key)
    {
        Title = title;
        Subtitle = subtitle;
        Theme = theme;
        TitleTextStyle = titleTextStyle;
        ForegroundColor = foregroundColor;
    }

    public string Title { get; }
    public string Subtitle { get; }
    public TextTheme Theme { get; }
    public TextStyle? TitleTextStyle { get; }
    public Color? ForegroundColor { get; }

    public override Widget Build(BuildContext context)
    {
        var effectiveTitleTextStyle = TitleTextStyle ?? Theme.TitleLarge;
        return new Column(
            mainAxisAlignment: MainAxisAlignment.Center,
            crossAxisAlignment: CrossAxisAlignment.Start,
            children:
            [
                new Text(Title, style: effectiveTitleTextStyle?.CopyWith(color: ForegroundColor)),
                new Text(Subtitle, style: Theme.TitleSmall?.CopyWith(color: ForegroundColor)),
            ]);
    }
}

/// <summary>Dart's private `_PageOpener`.</summary>
internal interface IPageOpener
{
    void OpenDetailPage(object arguments);

    void SetInitialDetailPage(object arguments);
}

/// <summary>Dart's private `_MasterDetailFlowProxy`.</summary>
internal sealed class MasterDetailFlowProxy : IPageOpener
{
    private readonly IPageOpener _pageOpener;

    internal MasterDetailFlowProxy(IPageOpener pageOpener) => _pageOpener = pageOpener;

    /// <summary>Open detail page with arguments.</summary>
    public void OpenDetailPage(object arguments) => _pageOpener.OpenDetailPage(arguments);

    /// <summary>Set the initial page to be open for the lateral layout, this is ignored for nested.</summary>
    public void SetInitialDetailPage(object arguments) => _pageOpener.SetInitialDetailPage(arguments);
}

/// <summary>
/// Dart's private `_MasterDetailFlow`: a nested master/detail navigator below
/// <see cref="AboutDialogs.MaterialWideDisplayThreshold"/> logical pixels, and a side-by-side layout above it.
/// </summary>
internal sealed class MasterDetailFlow : StatefulWidget
{
    public MasterDetailFlow(
        DetailPageBuilder detailPageBuilder,
        MasterViewBuilder masterViewBuilder,
        double? detailPageFABlessGutterWidth = null,
        Widget? title = null,
        Key? key = null) : base(key)
    {
        DetailPageBuilder = detailPageBuilder;
        MasterViewBuilder = masterViewBuilder;
        DetailPageFABlessGutterWidth = detailPageFABlessGutterWidth;
        Title = title;
    }

    public MasterViewBuilder MasterViewBuilder { get; }
    public DetailPageBuilder DetailPageBuilder { get; }
    public double? DetailPageFABlessGutterWidth { get; }
    public Widget? Title { get; }

    public override State CreateState() => new MasterDetailFlowState();

    /// <summary>The master detail flow proxy from the closest instance of this class that encloses the
    /// given context.</summary>
    public static MasterDetailFlowProxy Of(BuildContext context)
    {
        IPageOpener? pageOpener = context.FindAncestorStateOfType<MasterDetailScaffoldState>();
        pageOpener ??= context.FindAncestorStateOfType<MasterDetailFlowState>();
        if (pageOpener is null)
        {
            throw new InvalidOperationException(
                "Master Detail operation requested with a context that does not include a Master Detail Flow.");
        }

        return new MasterDetailFlowProxy(pageOpener);
    }
}

/// <summary>Dart's private `_MasterDetailFlowState`.</summary>
internal sealed class MasterDetailFlowState : State, IPageOpener
{
    private readonly LabeledGlobalKey<NavigatorState> _navigatorKey = new("_MasterDetailFlow");

    private MasterDetailFocus _focus = MasterDetailFocus.Master;
    private object? _cachedDetailArguments;
    private LayoutMode? _builtLayout;

    private MasterDetailFlow CurrentWidget => (MasterDetailFlow)StateWidget;

    public void OpenDetailPage(object arguments)
    {
        _cachedDetailArguments = arguments;
        if (_builtLayout == LayoutMode.Nested)
        {
            _navigatorKey.CurrentState!.PushNamed(AboutDialogs.NavDetail, arguments);
        }
        else
        {
            _focus = MasterDetailFocus.Detail;
        }
    }

    public void SetInitialDetailPage(object arguments)
    {
        _cachedDetailArguments = arguments;
    }

    public override Widget Build(BuildContext context)
    {
        return new LayoutBuilder((layoutContext, constraints) =>
        {
            double availableWidth = constraints.MaxWidth;
            return availableWidth >= AboutDialogs.MaterialWideDisplayThreshold
                ? BuildLateralUI(layoutContext)
                : BuildNestedUI(layoutContext);
        });
    }

    private Widget BuildNestedUI(BuildContext context)
    {
        _builtLayout = LayoutMode.Nested;
        var masterPageRoute = MasterPageRoute(context);

        return new NavigatorPopHandler<object?>(
            child: new Navigator(
                onGenerateRoute: settings =>
                {
                    switch (settings.Name)
                    {
                        case AboutDialogs.NavMaster:
                            _focus = MasterDetailFocus.Master;
                            return masterPageRoute;
                        case AboutDialogs.NavDetail:
                            _focus = MasterDetailFocus.Detail;
                            _cachedDetailArguments = settings.Arguments;
                            return DetailPageRoute(_cachedDetailArguments);
                        default:
                            throw new InvalidOperationException($"Unknown route {settings.Name}");
                    }
                },
                initialRouteName: "initial",
                onGenerateInitialRoutes: (_, _) => _focus == MasterDetailFocus.Master
                    ? [masterPageRoute]
                    : [masterPageRoute, DetailPageRoute(_cachedDetailArguments)],
                key: _navigatorKey),
            onPop: () => _navigatorKey.CurrentState!.MaybePop());
    }

    private Route MasterPageRoute(BuildContext context)
    {
        return new MaterialPageRoute(_ => new BlockSemantics(new MasterPage(
            leading: Navigator.Of(context).CanPop
                ? new BackButton(onPressed: () => Navigator.Of(context).Pop())
                : null,
            title: CurrentWidget.Title,
            masterViewBuilder: CurrentWidget.MasterViewBuilder)));
    }

    private Route DetailPageRoute(object? arguments)
    {
        return new MaterialPageRoute(routeContext => new PopScope<object?>(
            child: new BlockSemantics(CurrentWidget.DetailPageBuilder(routeContext, arguments, null)),
            onPopInvokedWithResult: (_, _) => _focus = MasterDetailFocus.Master));
    }

    private Widget BuildLateralUI(BuildContext context)
    {
        _ = context;
        _builtLayout = LayoutMode.Lateral;
        return new MasterDetailScaffold(
            detailPageBuilder: (detailContext, arguments, scrollController) =>
                CurrentWidget.DetailPageBuilder(detailContext, arguments ?? _cachedDetailArguments, scrollController),
            masterViewBuilder: (masterContext, isLateral) => CurrentWidget.MasterViewBuilder(masterContext, isLateral),
            actionBuilder: (_, _) => [],
            initialArguments: _cachedDetailArguments,
            title: CurrentWidget.Title,
            detailPageFABlessGutterWidth: CurrentWidget.DetailPageFABlessGutterWidth);
    }
}

/// <summary>Dart's private `_MasterPage`.</summary>
internal sealed class MasterPage : StatelessWidget
{
    public MasterPage(
        MasterViewBuilder? masterViewBuilder = null,
        Widget? title = null,
        Widget? leading = null,
        Key? key = null) : base(key)
    {
        MasterViewBuilder = masterViewBuilder;
        Title = title;
        Leading = leading;
    }

    public MasterViewBuilder? MasterViewBuilder { get; }
    public Widget? Title { get; }
    public Widget? Leading { get; }

    public override Widget Build(BuildContext context)
    {
        return new Scaffold(
            appBar: new AppBar(title: Title, leading: Leading, actions: []),
            body: MasterViewBuilder!(context, false));
    }
}

/// <summary>Dart's private `_MasterDetailScaffold`.</summary>
internal sealed class MasterDetailScaffold : StatefulWidget
{
    public MasterDetailScaffold(
        DetailPageBuilder detailPageBuilder,
        MasterViewBuilder masterViewBuilder,
        MasterDetailActionBuilder? actionBuilder = null,
        object? initialArguments = null,
        Widget? title = null,
        double? detailPageFABlessGutterWidth = null,
        Key? key = null) : base(key)
    {
        DetailPageBuilder = detailPageBuilder;
        MasterViewBuilder = masterViewBuilder;
        ActionBuilder = actionBuilder;
        InitialArguments = initialArguments;
        Title = title;
        DetailPageFABlessGutterWidth = detailPageFABlessGutterWidth;
    }

    public MasterViewBuilder MasterViewBuilder { get; }
    public DetailPageBuilder DetailPageBuilder { get; }
    public MasterDetailActionBuilder? ActionBuilder { get; }
    public object? InitialArguments { get; }
    public Widget? Title { get; }
    public double? DetailPageFABlessGutterWidth { get; }

    public override State CreateState() => new MasterDetailScaffoldState();
}

/// <summary>Dart's private `_MasterDetailScaffoldState`.</summary>
internal sealed class MasterDetailScaffoldState : State, IPageOpener
{
    private readonly ValueNotifier<object?> _detailArguments = new(null);

    private FloatingActionButtonLocation _floatingActionButtonLocation = null!;
    private double _detailPageFABGutterWidth;
    private double _detailPageFABlessGutterWidth;
    private double _masterViewWidth;

    private MasterDetailScaffold CurrentWidget => (MasterDetailScaffold)StateWidget;

    public override void InitState()
    {
        base.InitState();
        _detailPageFABlessGutterWidth =
            CurrentWidget.DetailPageFABlessGutterWidth ?? AboutDialogs.DetailPageFABlessGutterWidth;
        _detailPageFABGutterWidth = AboutDialogs.DetailPageFABGutterWidth;
        _masterViewWidth = AboutDialogs.MasterViewWidth;
        _floatingActionButtonLocation = FloatingActionButtonLocation.EndTop;
        _ = _detailPageFABGutterWidth;
    }

    public override void Dispose()
    {
        _detailArguments.Dispose();
        base.Dispose();
    }

    public void OpenDetailPage(object arguments)
    {
        Scheduler.AddPostFrameCallback(_ => _detailArguments.Value = arguments, scheduleFrame: false);
        MasterDetailFlow.Of(Context).OpenDetailPage(arguments);
    }

    public void SetInitialDetailPage(object arguments)
    {
        Scheduler.AddPostFrameCallback(_ => _detailArguments.Value = arguments, scheduleFrame: false);
        MasterDetailFlow.Of(Context).SetInitialDetailPage(arguments);
    }

    public override Widget Build(BuildContext context)
    {
        return new Stack(children:
        [
            new Scaffold(
                floatingActionButtonLocation: _floatingActionButtonLocation,
                appBar: new AppBar(
                    title: CurrentWidget.Title,
                    actions: CurrentWidget.ActionBuilder!(context, ActionLevel.Top),
                    bottom: new PreferredSize(
                        new Size(double.PositiveInfinity, AboutDialogs.ToolbarHeight),
                        new Row(children:
                        [
                            new SizedBox(
                                width: _masterViewWidth,
                                child: new IconTheme(
                                    Theme.Of(context).PrimaryIconTheme,
                                    new Padding(
                                        new Thickness(8),
                                        new Align(
                                            alignment: AlignmentDirectional.CenterEnd,
                                            child: new OverflowBar(
                                                spacing: 8,
                                                overflowAlignment: OverflowBarAlignment.End,
                                                children: CurrentWidget.ActionBuilder!(
                                                    context,
                                                    ActionLevel.View)))))),
                        ]))),
                body: new Align(
                    alignment: AlignmentDirectional.CenterStart,
                    child: BuildMasterPanel(context))),
            // Detail view stacked above main scaffold and master view.
            new SafeArea(
                child: new Padding(
                    EdgeInsetsDirectional.Only(
                        start: _masterViewWidth - AboutDialogs.CardElevation,
                        end: _detailPageFABlessGutterWidth),
                    new ValueListenableBuilder<object?>(
                        _detailArguments,
                        (_, value, _) => new AnimatedSwitcher(
                            duration: TimeSpan.FromMilliseconds(500),
                            transitionBuilder: (transitionChild, animation) =>
                                new FadeUpwardsPageTransitionsBuilder().BuildTransitions(animation, transitionChild),
                            child: new SizedBox(
                                width: double.PositiveInfinity,
                                height: double.PositiveInfinity,
                                child: new DetailView(
                                    builder: CurrentWidget.DetailPageBuilder,
                                    arguments: value ?? CurrentWidget.InitialArguments),
                                key: new ValueKey<object?>(value ?? CurrentWidget.InitialArguments)))))),
        ]);
    }

    private Widget BuildMasterPanel(BuildContext context)
    {
        return new ConstrainedBox(
            new BoxConstraints(MaxWidth: _masterViewWidth),
            CurrentWidget.MasterViewBuilder(context, true));
    }
}

/// <summary>Dart's private `_DetailView`.</summary>
internal sealed class DetailView : StatelessWidget
{
    public DetailView(DetailPageBuilder builder, object? arguments = null, Key? key = null) : base(key)
    {
        Builder = builder;
        Arguments = arguments;
    }

    public DetailPageBuilder Builder { get; }
    public object? Arguments { get; }

    public override Widget Build(BuildContext context)
    {
        if (Arguments is null)
        {
            return new SizedBox(width: 0.0, height: 0.0);
        }

        double screenHeight = MediaQuery.HeightOf(context);
        double minHeight = (screenHeight - AboutDialogs.ToolbarHeight) / screenHeight;

        return new DraggableScrollableSheet(
            initialChildSize: minHeight,
            minChildSize: minHeight,
            expand: false,
            builder: (sheetContext, controller) => new MouseRegion(
                // TODO(https://github.com/flutter/flutter/issues/59741): Remove this workaround.
                child: new Card(
                    color: Theme.Of(sheetContext).CardColor,
                    elevation: AboutDialogs.CardElevation,
                    clipBehavior: Clip.AntiAlias,
                    margin: new Thickness(AboutDialogs.CardElevation, 0.0, AboutDialogs.CardElevation, 0.0),
                    shape: new RoundedRectangleBorder(
                        borderRadius: BorderRadius.Vertical(top: new Radius(3.0, 3.0))),
                    child: Builder(sheetContext, Arguments, controller))));
    }
}

public static class AboutDialogs
{
    internal const int MaterialGutterThreshold = 720;
    internal const double WideGutterSize = 24.0;
    internal const double NarrowGutterSize = 12.0;
    internal const string NavMaster = "master";
    internal const string NavDetail = "detail";

    /// <summary>Minimum width for the lateral master-detail layout, per Material's adaptive breakpoints.</summary>
    internal const int MaterialWideDisplayThreshold = 840;

    internal const double CardElevation = 4.0;
    internal const double MasterViewWidth = 320.0;
    internal const double DetailPageFABlessGutterWidth = 40.0;
    internal const double DetailPageFABGutterWidth = 84.0;
    internal const double ToolbarHeight = 56.0;

    /// <summary>Dart's `_defaultApplicationName` fallback when no ancestor `Title` is found.</summary>
    public static string DefaultApplicationName =>
        IOPath.GetFileName(Environment.ProcessPath) is { Length: > 0 } name ? name : "application";

    internal static double GutterSize(BuildContext context) =>
        MediaQuery.WidthOf(context) >= MaterialGutterThreshold ? WideGutterSize : NarrowGutterSize;

    internal static string DefaultApplicationNameOf(BuildContext context)
    {
        // This doesn't handle the case of the application's title dynamically changing.
        return context.FindAncestorWidgetOfExactType<Title>()?.TitleText ?? DefaultApplicationName;
    }

    internal static string DefaultApplicationVersionOf(BuildContext context)
    {
        _ = context;
        return string.Empty;
    }

    internal static Widget? DefaultApplicationIconOf(BuildContext context)
    {
        _ = context;
        return null;
    }

    /// <summary>Dart's `showAboutDialog`.</summary>
    public static void ShowAboutDialog(
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
        RouteSettings? routeSettings = null,
        Point? anchorPoint = null)
    {
        _ = MaterialDialogs.ShowDialog<object>(
            context,
            _ => new AboutDialog(
                applicationName,
                applicationVersion,
                applicationIcon,
                applicationLegalese,
                children),
            barrierDismissible: barrierDismissible,
            barrierColor: barrierColor,
            barrierLabel: barrierLabel,
            useRootNavigator: useRootNavigator,
            routeSettings: routeSettings,
            anchorPoint: anchorPoint);
    }

    /// <summary>Dart's `showAdaptiveAboutDialog`: a Cupertino dialog on iOS/macOS.</summary>
    public static void ShowAdaptiveAboutDialog(
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
        RouteSettings? routeSettings = null,
        Point? anchorPoint = null)
    {
        _ = MaterialDialogs.ShowAdaptiveDialog<object>(
            context,
            _ => AboutDialog.Adaptive(
                applicationName,
                applicationVersion,
                applicationIcon,
                applicationLegalese,
                children),
            barrierDismissible: barrierDismissible,
            barrierColor: barrierColor,
            barrierLabel: barrierLabel,
            useRootNavigator: useRootNavigator,
            routeSettings: routeSettings,
            anchorPoint: anchorPoint);
    }

    /// <summary>Dart's `showLicensePage`.</summary>
    public static void ShowLicensePage(
        BuildContext context,
        string? applicationName = null,
        string? applicationVersion = null,
        Widget? applicationIcon = null,
        string? applicationLegalese = null,
        bool useRootNavigator = false)
    {
        var themes = InheritedTheme.Capture(
            from: context,
            to: Navigator.Of(context, rootNavigator: useRootNavigator).Context);
        Navigator.Of(context, rootNavigator: useRootNavigator).Push(new MaterialPageRoute(
            _ => themes.Wrap(new LicensePage(
                applicationName,
                applicationVersion,
                applicationIcon,
                applicationLegalese))));
    }
}
