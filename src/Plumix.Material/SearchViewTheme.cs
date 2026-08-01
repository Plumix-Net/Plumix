using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/search_view_theme.dart

public sealed partial record SearchViewThemeData(
    Color? BackgroundColor = null,
    double? Elevation = null,
    Color? SurfaceTintColor = null,
    BorderSide? Side = null,
    ShapeBorder? Shape = null,
    double? HeaderHeight = null,
    TextStyle? HeaderTextStyle = null,
    TextStyle? HeaderHintStyle = null,
    BoxConstraints? Constraints = null,
    Thickness? Padding = null,
    Thickness? BarPadding = null,
    bool? ShrinkWrap = null,
    Color? DividerColor = null);

public sealed class SearchViewTheme : InheritedWidget
{
    public SearchViewTheme(
        SearchViewThemeData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public SearchViewThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((SearchViewTheme)oldWidget).Data, Data);
    }

    public static SearchViewThemeData Of(BuildContext context)
    {
        var localTheme = context.DependOnInherited<SearchViewTheme>();
        if (localTheme is not null)
        {
            return localTheme.Data;
        }

        return Theme.Of(context).SearchViewTheme;
    }
}
