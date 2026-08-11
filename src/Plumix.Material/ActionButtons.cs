using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/action_buttons.dart

internal static class ActionButton
{
    public static Widget Build(
        BuildContext context,
        IconButton button,
        StandardComponentType? standardComponent,
        string tooltip,
        Action onPressedCallback)
    {
        return new IconButton(
            key: standardComponent?.Key(),
            icon: button.Icon,
            style: button.Style,
            color: button.Color,
            tooltip: tooltip,
            onPressed: () =>
            {
                if (button.OnPressed is not null)
                {
                    button.OnPressed();
                }
                else
                {
                    onPressedCallback();
                }
            });
    }
}

internal sealed class ActionIcon : StatelessWidget
{
    private readonly Func<ActionIconThemeData?, Func<BuildContext, Widget>?> _iconBuilderCallback;
    private readonly Func<BuildContext, IconData> _getIcon;
    private readonly Func<MaterialLocalizations, string> _getAndroidSemanticsLabel;

    public ActionIcon(
        Func<ActionIconThemeData?, Func<BuildContext, Widget>?> iconBuilderCallback,
        Func<BuildContext, IconData> getIcon,
        Func<MaterialLocalizations, string> getAndroidSemanticsLabel)
    {
        _iconBuilderCallback = iconBuilderCallback;
        _getIcon = getIcon;
        _getAndroidSemanticsLabel = getAndroidSemanticsLabel;
    }

    public override Widget Build(BuildContext context)
    {
        var iconBuilder = _iconBuilderCallback(ActionIconTheme.Of(context));
        if (iconBuilder is not null)
        {
            return iconBuilder(context);
        }

        IconData data = _getIcon(context);
        string? semanticsLabel = PlatformDefaults.TargetPlatform == TargetPlatform.Android
            ? _getAndroidSemanticsLabel(MaterialLocalizations.Of(context))
            : null;
        return new Icon(data, semanticLabel: semanticsLabel);
    }
}

public sealed class BackButtonIcon : StatelessWidget
{
    public BackButtonIcon(Key? key = null) : base(key)
    {
    }

    public override Widget Build(BuildContext context)
    {
        return new ActionIcon(
            iconBuilderCallback: theme => theme?.BackButtonIconBuilder,
            getIcon: ResolveIcon,
            getAndroidSemanticsLabel: localizations => localizations.BackButtonTooltip);
    }

    private static IconData ResolveIcon(BuildContext context)
    {
        if (OperatingSystem.IsBrowser())
        {
            return Icons.ArrowBack;
        }

        return Theme.Of(context).Platform is TargetPlatform.IOS or TargetPlatform.MacOS
            ? Icons.ArrowBackIosNewRounded
            : Icons.ArrowBack;
    }
}

public sealed class BackButton : IconButton
{
    public BackButton(
        Color? color = null,
        ButtonStyle? style = null,
        Action? onPressed = null,
        Key? key = null) : base(
            icon: new BackButtonIcon(),
            onPressed: onPressed,
            color: color,
            style: style,
            key: key)
    {
    }

    public override Widget Build(BuildContext context)
    {
        return ActionButton.Build(
            context,
            this,
            StandardComponentType.BackButton,
            MaterialLocalizations.Of(context).BackButtonTooltip,
            () => Navigator.MaybePop(context));
    }
}

public sealed class CloseButtonIcon : StatelessWidget
{
    public CloseButtonIcon(Key? key = null) : base(key)
    {
    }

    public override Widget Build(BuildContext context)
    {
        return new ActionIcon(
            iconBuilderCallback: theme => theme?.CloseButtonIconBuilder,
            getIcon: _ => Icons.Close,
            getAndroidSemanticsLabel: localizations => localizations.CloseButtonTooltip);
    }
}

public sealed class CloseButton : IconButton
{
    public CloseButton(
        Color? color = null,
        Action? onPressed = null,
        ButtonStyle? style = null,
        Key? key = null) : base(
            icon: new CloseButtonIcon(),
            onPressed: onPressed,
            color: color,
            style: style,
            key: key)
    {
    }

    public override Widget Build(BuildContext context)
    {
        return ActionButton.Build(
            context,
            this,
            StandardComponentType.CloseButton,
            MaterialLocalizations.Of(context).CloseButtonTooltip,
            () => Navigator.MaybePop(context));
    }
}

public sealed class DrawerButtonIcon : StatelessWidget
{
    public DrawerButtonIcon(Key? key = null) : base(key)
    {
    }

    public override Widget Build(BuildContext context)
    {
        return new ActionIcon(
            iconBuilderCallback: theme => theme?.DrawerButtonIconBuilder,
            getIcon: _ => Icons.Menu,
            getAndroidSemanticsLabel: localizations => localizations.OpenAppDrawerTooltip);
    }
}

public sealed class DrawerButton : IconButton
{
    public DrawerButton(
        Color? color = null,
        ButtonStyle? style = null,
        Action? onPressed = null,
        Key? key = null) : base(
            icon: new DrawerButtonIcon(),
            onPressed: onPressed,
            color: color,
            style: style,
            key: key)
    {
    }

    public override Widget Build(BuildContext context)
    {
        return ActionButton.Build(
            context,
            this,
            StandardComponentType.DrawerButton,
            MaterialLocalizations.Of(context).OpenAppDrawerTooltip,
            () => Scaffold.Of(context).OpenDrawer());
    }
}

public sealed class EndDrawerButtonIcon : StatelessWidget
{
    public EndDrawerButtonIcon(Key? key = null) : base(key)
    {
    }

    public override Widget Build(BuildContext context)
    {
        return new ActionIcon(
            iconBuilderCallback: theme => theme?.EndDrawerButtonIconBuilder,
            getIcon: _ => Icons.Menu,
            getAndroidSemanticsLabel: localizations => localizations.OpenAppDrawerTooltip);
    }
}

public sealed class EndDrawerButton : IconButton
{
    public EndDrawerButton(
        Color? color = null,
        ButtonStyle? style = null,
        Action? onPressed = null,
        Key? key = null) : base(
            icon: new EndDrawerButtonIcon(),
            onPressed: onPressed,
            color: color,
            style: style,
            key: key)
    {
    }

    public override Widget Build(BuildContext context)
    {
        return ActionButton.Build(
            context,
            this,
            standardComponent: null,
            MaterialLocalizations.Of(context).OpenAppDrawerTooltip,
            () => Scaffold.Of(context).OpenEndDrawer());
    }
}
