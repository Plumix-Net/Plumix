using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/search_bar_theme.dart

public sealed partial record SearchBarThemeData(
    WidgetStateProperty<double?>? Elevation = null,
    WidgetStateProperty<Color?>? BackgroundColor = null,
    WidgetStateProperty<Color?>? ShadowColor = null,
    WidgetStateProperty<Color?>? SurfaceTintColor = null,
    WidgetStateProperty<Color?>? OverlayColor = null,
    WidgetStateProperty<BorderSide?>? Side = null,
    WidgetStateProperty<OutlinedBorder?>? Shape = null,
    WidgetStateProperty<EdgeInsetsGeometry?>? Padding = null,
    WidgetStateProperty<TextStyle?>? TextStyle = null,
    WidgetStateProperty<TextStyle?>? HintStyle = null,
    BoxConstraints? Constraints = null,
    TextCapitalization? TextCapitalization = null);

public sealed class SearchBarTheme : InheritedWidget
{
    public SearchBarTheme(
        SearchBarThemeData data,
        Widget child,
        Key? key = null) : base(child, key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    public SearchBarThemeData Data { get; }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((SearchBarTheme)oldWidget).Data, Data);
    }

    public static SearchBarThemeData Of(BuildContext context)
    {
        var localTheme = context.DependOnInherited<SearchBarTheme>();
        if (localTheme is not null)
        {
            return localTheme.Data;
        }

        return Theme.Of(context).SearchBarTheme;
    }
}
