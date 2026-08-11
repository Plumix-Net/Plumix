using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/action_icons_theme.dart

public sealed partial record ActionIconThemeData(
    Func<BuildContext, Widget>? BackButtonIconBuilder = null,
    Func<BuildContext, Widget>? CloseButtonIconBuilder = null,
    Func<BuildContext, Widget>? DrawerButtonIconBuilder = null,
    Func<BuildContext, Widget>? EndDrawerButtonIconBuilder = null)
{
    public ActionIconThemeData CopyWith(
        Func<BuildContext, Widget>? backButtonIconBuilder = null,
        Func<BuildContext, Widget>? closeButtonIconBuilder = null,
        Func<BuildContext, Widget>? drawerButtonIconBuilder = null,
        Func<BuildContext, Widget>? endDrawerButtonIconBuilder = null)
    {
        return new ActionIconThemeData(
            BackButtonIconBuilder: backButtonIconBuilder ?? BackButtonIconBuilder,
            CloseButtonIconBuilder: closeButtonIconBuilder ?? CloseButtonIconBuilder,
            DrawerButtonIconBuilder: drawerButtonIconBuilder ?? DrawerButtonIconBuilder,
            EndDrawerButtonIconBuilder: endDrawerButtonIconBuilder ?? EndDrawerButtonIconBuilder);
    }
}

public sealed class ActionIconTheme : InheritedWidget
{
    public ActionIconTheme(
        ActionIconThemeData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public ActionIconThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((ActionIconTheme)oldWidget).Data, Data);
    }

    public static ActionIconThemeData? Of(BuildContext context)
    {
        return context.DependOnInherited<ActionIconTheme>()?.Data
               ?? Theme.Of(context).ActionIconTheme;
    }
}
