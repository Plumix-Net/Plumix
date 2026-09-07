using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/action_icons_theme.dart

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
        Key? key = null) : base(child, key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    public ActionIconThemeData Data { get; }

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
