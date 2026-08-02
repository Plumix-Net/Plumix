using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/action_buttons.dart

public sealed class BackButtonIcon : StatelessWidget
{
    public BackButtonIcon(Key? key = null) : base(key)
    {
    }

    public override Widget Build(BuildContext context)
    {
        var themedBuilder = ActionIconTheme.Of(context)?.BackButtonIconBuilder;
        if (themedBuilder is not null)
        {
            return themedBuilder(context);
        }

        var theme = Theme.Of(context);
        var iconData = !OperatingSystem.IsBrowser()
                       && theme.Platform is TargetPlatform.IOS or TargetPlatform.MacOS
            ? Icons.ArrowBackIosNewRounded
            : Icons.ArrowBack;
        string? semanticLabel = theme.Platform == TargetPlatform.Android
            ? MaterialLocalizations.Of(context).BackButtonTooltip
            : null;
        return new Icon(iconData, semanticLabel: semanticLabel);
    }
}

public sealed class CloseButtonIcon : StatelessWidget
{
    public CloseButtonIcon(Key? key = null) : base(key)
    {
    }

    public override Widget Build(BuildContext context)
    {
        var themedBuilder = ActionIconTheme.Of(context)?.CloseButtonIconBuilder;
        if (themedBuilder is not null)
        {
            return themedBuilder(context);
        }

        string? semanticLabel = Theme.Of(context).Platform == TargetPlatform.Android
            ? MaterialLocalizations.Of(context).CloseButtonTooltip
            : null;
        return new Icon(Icons.Close, semanticLabel: semanticLabel);
    }
}

public sealed class BackButton : StatelessWidget
{
    public BackButton(
        Color? color = null,
        ButtonStyle? style = null,
        Action? onPressed = null,
        Key? key = null) : base(key)
    {
        Color = color;
        Style = style;
        OnPressed = onPressed;
    }

    public Color? Color { get; }

    public ButtonStyle? Style { get; }

    public Action? OnPressed { get; }

    public override Widget Build(BuildContext context)
    {
        return BuildActionButton(
            context,
            new BackButtonIcon(),
            MaterialLocalizations.Of(context).BackButtonTooltip);
    }

    private Widget BuildActionButton(BuildContext context, Widget icon, string tooltip)
    {
        return new Tooltip(
            message: tooltip,
            child: new IconButton(
                icon: icon,
                color: Color,
                style: Style,
                onPressed: OnPressed ?? (() => Navigator.MaybePop(context))));
    }
}

public sealed class CloseButton : StatelessWidget
{
    public CloseButton(
        Color? color = null,
        Action? onPressed = null,
        ButtonStyle? style = null,
        Key? key = null) : base(key)
    {
        Color = color;
        OnPressed = onPressed;
        Style = style;
    }

    public Color? Color { get; }

    public Action? OnPressed { get; }

    public ButtonStyle? Style { get; }

    public override Widget Build(BuildContext context)
    {
        return new Tooltip(
            message: MaterialLocalizations.Of(context).CloseButtonTooltip,
            child: new IconButton(
                icon: new CloseButtonIcon(),
                color: Color,
                style: Style,
                onPressed: OnPressed ?? (() => Navigator.MaybePop(context))));
    }
}

public sealed class DrawerButtonIcon : StatelessWidget
{
    public DrawerButtonIcon(Key? key = null) : base(key)
    {
    }

    public override Widget Build(BuildContext context)
    {
        var themedBuilder = ActionIconTheme.Of(context)?.DrawerButtonIconBuilder;
        if (themedBuilder is not null)
        {
            return themedBuilder(context);
        }

        return BuildMenuIcon(context);
    }

    internal static Widget BuildMenuIcon(BuildContext context)
    {
        string? semanticLabel = Theme.Of(context).Platform == TargetPlatform.Android
            ? MaterialLocalizations.Of(context).OpenAppDrawerTooltip
            : null;
        return new Icon(Icons.Menu, semanticLabel: semanticLabel);
    }
}

public sealed class EndDrawerButtonIcon : StatelessWidget
{
    public EndDrawerButtonIcon(Key? key = null) : base(key)
    {
    }

    public override Widget Build(BuildContext context)
    {
        var themedBuilder = ActionIconTheme.Of(context)?.EndDrawerButtonIconBuilder;
        return themedBuilder is not null
            ? themedBuilder(context)
            : DrawerButtonIcon.BuildMenuIcon(context);
    }
}

public sealed class DrawerButton : StatelessWidget
{
    public DrawerButton(
        Color? color = null,
        ButtonStyle? style = null,
        Action? onPressed = null,
        Key? key = null) : base(key)
    {
        Color = color;
        Style = style;
        OnPressed = onPressed;
    }

    public Color? Color { get; }

    public ButtonStyle? Style { get; }

    public Action? OnPressed { get; }

    public override Widget Build(BuildContext context)
    {
        return new Tooltip(
            message: MaterialLocalizations.Of(context).OpenAppDrawerTooltip,
            child: new IconButton(
                icon: new DrawerButtonIcon(),
                color: Color,
                style: Style,
                onPressed: OnPressed ?? (() => Scaffold.Of(context).OpenDrawer())));
    }
}

public sealed class EndDrawerButton : StatelessWidget
{
    public EndDrawerButton(
        Color? color = null,
        ButtonStyle? style = null,
        Action? onPressed = null,
        Key? key = null) : base(key)
    {
        Color = color;
        Style = style;
        OnPressed = onPressed;
    }

    public Color? Color { get; }

    public ButtonStyle? Style { get; }

    public Action? OnPressed { get; }

    public override Widget Build(BuildContext context)
    {
        return new Tooltip(
            message: MaterialLocalizations.Of(context).OpenAppDrawerTooltip,
            child: new IconButton(
                icon: new EndDrawerButtonIcon(),
                color: Color,
                style: Style,
                onPressed: OnPressed ?? (() => Scaffold.Of(context).OpenEndDrawer())));
    }
}
