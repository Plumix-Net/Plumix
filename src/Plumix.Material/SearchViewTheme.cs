using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/search_view_theme.dart

public sealed partial record SearchViewThemeData(
    Color? BackgroundColor = null,
    double? Elevation = null,
    Color? SurfaceTintColor = null,
    BoxConstraints? Constraints = null,
    EdgeInsetsGeometry? Padding = null,
    EdgeInsetsGeometry? BarPadding = null,
    bool? ShrinkWrap = null,
    BorderSide? Side = null,
    OutlinedBorder? Shape = null,
    double? HeaderHeight = null,
    TextStyle? HeaderTextStyle = null,
    TextStyle? HeaderHintStyle = null,
    Color? DividerColor = null);

public sealed class SearchViewTheme : InheritedTheme
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

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new SearchViewTheme(Data, child);
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
