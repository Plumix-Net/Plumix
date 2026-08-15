using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/menu_theme.dart

/// <summary>Visual overrides for submenu panels and submenu disclosure icons.</summary>
/// <remarks>
/// Flutter declares this as an ordinary class so <see cref="MenuBarThemeData"/> can extend it; Plumix
/// keeps that shape, which is why the members are `virtual` rather than record properties.
/// </remarks>
public class MenuThemeData
{
    public MenuThemeData(
        MenuStyle? style = null,
        MaterialStateProperty<Widget?>? submenuIcon = null)
    {
        Style = style;
        SubmenuIcon = submenuIcon;
    }

    /// <summary>The <see cref="MenuStyle"/> of a <see cref="SubmenuButton"/> menu.</summary>
    public virtual MenuStyle? Style { get; }

    /// <summary>
    /// If provided, replaces the default <see cref="SubmenuButton"/> arrow icon. Resolves in the
    /// disabled, hovered and focused states.
    /// </summary>
    public virtual MaterialStateProperty<Widget?>? SubmenuIcon { get; }

    /// <summary>Linearly interpolates between two menu themes.</summary>
    public static MenuThemeData? Lerp(MenuThemeData? a, MenuThemeData? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new MenuThemeData(
            style: MenuStyle.Lerp(a?.Style, b?.Style, t),
            submenuIcon: t < 0.5 ? a?.SubmenuIcon : b?.SubmenuIcon);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        // Dart's `operator ==` starts with a runtimeType check, so a `MenuBarThemeData` is never
        // equal to a plain `MenuThemeData` carrying the same style.
        if (obj is not MenuThemeData other || other.GetType() != GetType())
        {
            return false;
        }

        return Equals(Style, other.Style) && Equals(SubmenuIcon, other.SubmenuIcon);
    }

    public override int GetHashCode() => HashCode.Combine(Style, SubmenuIcon);
}

/// <summary>
/// An inherited theme for menus created by <see cref="MenuAnchor"/> and
/// <see cref="SubmenuButton"/>.
/// </summary>
public class MenuTheme : InheritedTheme
{
    public MenuTheme(MenuThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public MenuThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    public override Widget Wrap(BuildContext context, Widget child) => new MenuTheme(Data, child);

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(Data, ((MenuTheme)oldWidget).Data);

    public static MenuThemeData Of(BuildContext context) =>
        context.DependOnInherited<MenuTheme>()?.Data ?? Theme.Of(context).MenuTheme;
}
