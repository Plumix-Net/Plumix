using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/list_tile_theme.dart (approximate)

public enum ListTileStyle
{
    List,
    Drawer,
}

public sealed record ListTileThemeData(
    bool? Dense = null,
    BorderRadius? Shape = null,
    ListTileStyle? Style = null,
    Color? SelectedColor = null,
    Color? IconColor = null,
    Color? TextColor = null,
    TextStyle? TitleTextStyle = null,
    TextStyle? SubtitleTextStyle = null,
    TextStyle? LeadingAndTrailingTextStyle = null,
    Thickness? ContentPadding = null,
    Color? TileColor = null,
    Color? SelectedTileColor = null,
    double? HorizontalTitleGap = null,
    double? MinVerticalPadding = null,
    double? MinLeadingWidth = null,
    double? MinTileHeight = null,
    bool? EnableFeedback = null,
    MouseCursor? MouseCursor = null,
    bool? IsThreeLine = null);

public sealed class ListTileTheme : InheritedWidget
{
    public ListTileTheme(
        ListTileThemeData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public ListTileThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((ListTileTheme)oldWidget).Data, Data);
    }

    public static ListTileThemeData Of(BuildContext context)
    {
        var localTheme = context.DependOnInherited<ListTileTheme>();
        if (localTheme is not null)
        {
            return localTheme.Data;
        }

        return Theme.Of(context).ListTileTheme;
    }
}
