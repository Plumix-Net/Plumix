using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity sources: material_ui/lib/src/menu_bar_theme.dart;
// material_ui/lib/src/menu_button_theme.dart

/// <summary>Visual overrides for a <see cref="MenuBar"/>, excluding its submenu panels.</summary>
public class MenuBarThemeData : MenuThemeData
{
    public MenuBarThemeData(MenuStyle? style = null) : base(style)
    {
    }

    /// <summary>Linearly interpolates between two <see cref="MenuBar"/> themes.</summary>
    public static MenuBarThemeData? Lerp(MenuBarThemeData? a, MenuBarThemeData? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new MenuBarThemeData(MenuStyle.Lerp(a?.Style, b?.Style, t));
    }
}

/// <summary>Button-style overrides shared by <see cref="MenuItemButton"/> and <see cref="SubmenuButton"/>.</summary>
public class MenuButtonThemeData : IDiagnosticable
{
    public MenuButtonThemeData(ButtonStyle? style = null)
    {
        Style = style;
    }

    /// <summary>
    /// Overrides for <see cref="SubmenuButton"/> and <see cref="MenuItemButton"/>'s default style.
    /// </summary>
    public virtual ButtonStyle? Style { get; }

    /// <summary>Linearly interpolates between two menu button themes.</summary>
    public static MenuButtonThemeData? Lerp(MenuButtonThemeData? a, MenuButtonThemeData? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new MenuButtonThemeData(ButtonStyle.Lerp(a?.Style, b?.Style, t));
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        // Dart's `operator ==` starts with a runtimeType check.
        if (obj is not MenuButtonThemeData other || other.GetType() != GetType())
        {
            return false;
        }

        return Equals(Style, other.Style);
    }

    public virtual void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        properties.Add(new DiagnosticsProperty<ButtonStyle?>(
            "style",
            Style,
            defaultValue: DiagnosticsDefaults.NullValue));
    }

    public override int GetHashCode() => Style?.GetHashCode() ?? 0;
}

/// <summary>
/// An inherited widget that defines the configuration for the <see cref="MenuBar"/> widgets in this
/// widget's descendants, but not for their submenus.
/// </summary>
public class MenuBarTheme : InheritedTheme
{
    public MenuBarTheme(MenuBarThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public MenuBarThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    public override Widget Wrap(BuildContext context, Widget child) => new MenuBarTheme(Data, child);

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(Data, ((MenuBarTheme)oldWidget).Data);

    public static MenuBarThemeData Of(BuildContext context) =>
        context.DependOnInherited<MenuBarTheme>()?.Data ?? Theme.Of(context).MenuBarTheme;
}

/// <summary>
/// Overrides the default <see cref="ButtonStyle"/> of its <see cref="MenuItemButton"/> and
/// <see cref="SubmenuButton"/> descendants.
/// </summary>
public class MenuButtonTheme : InheritedTheme
{
    public MenuButtonTheme(MenuButtonThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public MenuButtonThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    public override Widget Wrap(BuildContext context, Widget child) =>
        new MenuButtonTheme(Data, child);

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(Data, ((MenuButtonTheme)oldWidget).Data);

    public static MenuButtonThemeData Of(BuildContext context) =>
        context.DependOnInherited<MenuButtonTheme>()?.Data ?? Theme.Of(context).MenuButtonTheme;
}
