using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity sources: flutter/packages/flutter/lib/src/material/menu_bar_theme.dart;
// flutter/packages/flutter/lib/src/material/menu_button_theme.dart

/// <summary>Visual overrides for a <see cref="MenuBar"/>, excluding its submenu panels.</summary>
public sealed record MenuBarThemeData(MenuStyle? Style = null);

/// <summary>Button-style overrides shared by <see cref="MenuItemButton"/> and <see cref="SubmenuButton"/>.</summary>
public sealed record MenuButtonThemeData(ButtonStyle? Style = null);

public sealed class MenuBarTheme : InheritedWidget
{
    public MenuBarTheme(MenuBarThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public MenuBarThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(Data, ((MenuBarTheme)oldWidget).Data);

    public static MenuBarThemeData Of(BuildContext context) =>
        context.DependOnInherited<MenuBarTheme>()?.Data ?? Theme.Of(context).MenuBarTheme;
}

public sealed class MenuButtonTheme : InheritedWidget
{
    public MenuButtonTheme(MenuButtonThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public MenuButtonThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(Data, ((MenuButtonTheme)oldWidget).Data);

    public static MenuButtonThemeData Of(BuildContext context) =>
        context.DependOnInherited<MenuButtonTheme>()?.Data ?? Theme.Of(context).MenuButtonTheme;
}
