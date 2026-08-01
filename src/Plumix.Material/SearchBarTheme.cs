using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/search_bar_theme.dart

public sealed partial record SearchBarThemeData(
    MaterialStateProperty<double?>? Elevation = null,
    MaterialStateProperty<Color?>? BackgroundColor = null,
    MaterialStateProperty<Color?>? ShadowColor = null,
    MaterialStateProperty<Color?>? SurfaceTintColor = null,
    MaterialStateProperty<Color?>? OverlayColor = null,
    MaterialStateProperty<BorderSide?>? Side = null,
    MaterialStateProperty<ShapeBorder?>? Shape = null,
    MaterialStateProperty<Thickness?>? Padding = null,
    MaterialStateProperty<TextStyle?>? TextStyle = null,
    MaterialStateProperty<TextStyle?>? HintStyle = null,
    BoxConstraints? Constraints = null,
    TextCapitalization? TextCapitalization = null);

public sealed class SearchBarTheme : InheritedWidget
{
    public SearchBarTheme(
        SearchBarThemeData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public SearchBarThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

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
