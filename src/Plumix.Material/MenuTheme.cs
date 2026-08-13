using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/menu_theme.dart.

/// <summary>Visual overrides for submenu panels and submenu disclosure icons.</summary>
public sealed partial record MenuThemeData(
    MenuStyle? Style = null,
    MaterialStateProperty<Widget?>? SubmenuIcon = null);

/// <summary>
/// An inherited theme for menus created by <see cref="MenuAnchor"/> and
/// <see cref="SubmenuButton"/>.
/// </summary>
public sealed class MenuTheme : InheritedWidget
{
    public MenuTheme(MenuThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public MenuThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(Data, ((MenuTheme)oldWidget).Data);

    public static MenuThemeData? MaybeOf(BuildContext context) =>
        context.DependOnInherited<MenuTheme>()?.Data;

    public static MenuThemeData Of(BuildContext context) =>
        MaybeOf(context) ?? Theme.Of(context).MenuTheme;
}
