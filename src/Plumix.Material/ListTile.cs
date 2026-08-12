using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/list_tile.dart

public sealed class ListTile : StatelessWidget
{
    private static readonly TimeSpan ThemeChangeDuration = TimeSpan.FromMilliseconds(200);
    private static readonly Color M2LightDefaultIconColor = Color.FromArgb(0x73, 0x00, 0x00, 0x00);

    public ListTile(
        Widget? leading = null,
        Widget? title = null,
        Widget? subtitle = null,
        Widget? trailing = null,
        bool? isThreeLine = null,
        bool? dense = null,
        VisualDensity? visualDensity = null,
        ShapeBorder? shape = null,
        ListTileStyle? style = null,
        Color? selectedColor = null,
        MaterialStateProperty<Color?>? iconColor = null,
        MaterialStateProperty<Color?>? textColor = null,
        TextStyle? titleTextStyle = null,
        TextStyle? subtitleTextStyle = null,
        TextStyle? leadingAndTrailingTextStyle = null,
        EdgeInsetsGeometry? contentPadding = null,
        bool enabled = true,
        Action? onTap = null,
        Action? onLongPress = null,
        Action<bool>? onFocusChange = null,
        MouseCursor? mouseCursor = null,
        bool selected = false,
        Color? focusColor = null,
        Color? hoverColor = null,
        Color? splashColor = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        Color? tileColor = null,
        Color? selectedTileColor = null,
        bool? enableFeedback = null,
        double? horizontalTitleGap = null,
        double? minVerticalPadding = null,
        double? minLeadingWidth = null,
        double? minTileHeight = null,
        ListTileTitleAlignment? titleAlignment = null,
        bool internalAddSemanticForOnTap = true,
        MaterialStatesController? statesController = null,
        Key? key = null) : base(key)
    {
        if (isThreeLine == true && subtitle is null)
        {
            throw new ArgumentException("ListTile with isThreeLine=true requires a non-null subtitle.", nameof(isThreeLine));
        }

        Leading = leading;
        Title = title;
        Subtitle = subtitle;
        Trailing = trailing;
        IsThreeLine = isThreeLine;
        Dense = dense;
        VisualDensity = visualDensity;
        Shape = shape;
        Style = style;
        SelectedColor = selectedColor;
        IconColor = iconColor;
        TextColor = textColor;
        TitleTextStyle = titleTextStyle;
        SubtitleTextStyle = subtitleTextStyle;
        LeadingAndTrailingTextStyle = leadingAndTrailingTextStyle;
        ContentPadding = contentPadding;
        Enabled = enabled;
        OnTap = onTap;
        OnLongPress = onLongPress;
        OnFocusChange = onFocusChange;
        MouseCursor = mouseCursor;
        Selected = selected;
        FocusColor = focusColor;
        HoverColor = hoverColor;
        SplashColor = splashColor;
        FocusNode = focusNode;
        Autofocus = autofocus;
        TileColor = tileColor;
        SelectedTileColor = selectedTileColor;
        EnableFeedback = enableFeedback;
        HorizontalTitleGap = horizontalTitleGap;
        MinVerticalPadding = minVerticalPadding;
        MinLeadingWidth = minLeadingWidth;
        MinTileHeight = minTileHeight;
        TitleAlignment = titleAlignment;
        InternalAddSemanticForOnTap = internalAddSemanticForOnTap;
        StatesController = statesController;
    }

    public Widget? Leading { get; }
    public Widget? Title { get; }
    public Widget? Subtitle { get; }
    public Widget? Trailing { get; }
    public bool? IsThreeLine { get; }
    public bool? Dense { get; }
    public VisualDensity? VisualDensity { get; }
    public ShapeBorder? Shape { get; }
    public ListTileStyle? Style { get; }
    public Color? SelectedColor { get; }
    public MaterialStateProperty<Color?>? IconColor { get; }
    public MaterialStateProperty<Color?>? TextColor { get; }
    public TextStyle? TitleTextStyle { get; }
    public TextStyle? SubtitleTextStyle { get; }
    public TextStyle? LeadingAndTrailingTextStyle { get; }
    public EdgeInsetsGeometry? ContentPadding { get; }
    public bool Enabled { get; }
    public Action? OnTap { get; }
    public Action? OnLongPress { get; }
    public Action<bool>? OnFocusChange { get; }
    public MouseCursor? MouseCursor { get; }
    public bool Selected { get; }
    public Color? FocusColor { get; }
    public Color? HoverColor { get; }
    public Color? SplashColor { get; }
    public FocusNode? FocusNode { get; }
    public bool Autofocus { get; }
    public Color? TileColor { get; }
    public Color? SelectedTileColor { get; }
    public bool? EnableFeedback { get; }
    public double? HorizontalTitleGap { get; }
    public double? MinVerticalPadding { get; }
    public double? MinLeadingWidth { get; }
    public double? MinTileHeight { get; }
    public ListTileTitleAlignment? TitleAlignment { get; }
    public bool InternalAddSemanticForOnTap { get; }
    public MaterialStatesController? StatesController { get; }

    public static IReadOnlyList<Widget> DivideTiles(
        IEnumerable<Widget> tiles,
        BuildContext? context = null,
        Color? color = null)
    {
        ArgumentNullException.ThrowIfNull(tiles);
        if (!color.HasValue && !context.HasValue)
        {
            throw new ArgumentException("ListTile.DivideTiles requires either a context or an explicit color.");
        }

        List<Widget> tileList = tiles.ToList();
        if (tileList.Count <= 1)
        {
            return tileList;
        }

        var result = new List<Widget>(tileList.Count);
        for (int index = 0; index < tileList.Count - 1; index++)
        {
            result.Add(new DecoratedBox(
                position: DecorationPosition.Foreground,
                decoration: new BoxDecoration(
                    Border: new Plumix.Rendering.Border(
                        bottom: Divider.CreateBorderSide(context, color: color))),
                child: tileList[index]));
        }

        result.Add(tileList[^1]);
        return result;
    }

    public override Widget Build(BuildContext context)
    {
        ThemeData theme = Theme.Of(context);
        IconButtonThemeData iconButtonTheme = IconButtonTheme.Of(context);
        ListTileThemeData tileTheme = ListTileTheme.Of(context);
        ListTileStyle listTileStyle = Style
                                      ?? tileTheme.Style
                                      ?? theme.ListTileTheme.Style
                                      ?? ListTileStyle.List;
        ListTileThemeData defaults = ResolveDefaults(theme, listTileStyle);

        Color backgroundColor = TileColor
                                ?? tileTheme.TileColor
                                ?? theme.ListTileTheme.TileColor
                                ?? defaults.TileColor!.Value;
        Color selectedBackgroundColor = SelectedTileColor
                                        ?? tileTheme.SelectedTileColor
                                        ?? theme.ListTileTheme.SelectedTileColor
                                        ?? defaults.TileColor!.Value;
        Color effectiveTileColor = Selected ? selectedBackgroundColor : backgroundColor;
        MaterialState states = (Enabled ? MaterialState.None : MaterialState.Disabled)
                               | (Selected ? MaterialState.Selected : MaterialState.None);

        Color? preDefaultIconColor = ResolveContentColor(IconColor, SelectedColor, null, null, states)
                                     ?? ResolveContentColor(
                                         tileTheme.IconColor,
                                         tileTheme.SelectedColor,
                                         null,
                                         null,
                                         states)
                                     ?? ResolveContentColor(
                                         theme.ListTileTheme.IconColor,
                                         theme.ListTileTheme.SelectedColor,
                                         null,
                                         null,
                                         states);
        Color? defaultIconColor = ResolveContentColor(
            null,
            defaults.SelectedColor,
            defaults.IconColor?.Resolve(MaterialState.None),
            theme.DisabledColor,
            states);
        Color? effectiveIconButtonColor = preDefaultIconColor
                                          ?? iconButtonTheme.Style?.ForegroundColor?.Resolve(states)
                                          ?? defaultIconColor;
        Color? effectiveIconColor = preDefaultIconColor ?? defaultIconColor;

        Color? effectiveTextColor = ResolveContentColor(TextColor, SelectedColor, null, null, states)
                                    ?? ResolveContentColor(
                                        tileTheme.TextColor,
                                        tileTheme.SelectedColor,
                                        null,
                                        null,
                                        states)
                                    ?? ResolveContentColor(
                                        theme.ListTileTheme.TextColor,
                                        theme.ListTileTheme.SelectedColor,
                                        null,
                                        null,
                                        states)
                                    ?? ResolveContentColor(
                                        null,
                                        defaults.SelectedColor,
                                        defaults.TextColor?.Resolve(MaterialState.None),
                                        theme.DisabledColor,
                                        states);

        bool isDense = Dense ?? tileTheme.Dense ?? theme.ListTileTheme.Dense ?? false;
        TextStyle leadingAndTrailingStyle = (LeadingAndTrailingTextStyle
                                             ?? tileTheme.LeadingAndTrailingTextStyle
                                             ?? defaults.LeadingAndTrailingTextStyle!)
            .CopyWith(color: effectiveTextColor);
        TextStyle titleStyle = (TitleTextStyle ?? tileTheme.TitleTextStyle ?? defaults.TitleTextStyle!)
            .CopyWith(color: effectiveTextColor, fontSize: isDense ? 13.0 : null);
        TextStyle subtitleStyle = (SubtitleTextStyle ?? tileTheme.SubtitleTextStyle ?? defaults.SubtitleTextStyle!)
            .CopyWith(color: effectiveTextColor, fontSize: isDense ? 12.0 : null);

        Widget? leading = WrapSlot(Leading, leadingAndTrailingStyle);
        Widget title = new AnimatedDefaultTextStyle(
            style: titleStyle,
            duration: ThemeChangeDuration,
            child: Title ?? new SizedBox());
        Widget? subtitle = Subtitle is null
            ? null
            : new AnimatedDefaultTextStyle(
                style: subtitleStyle,
                duration: ThemeChangeDuration,
                child: Subtitle);
        Widget? trailing = WrapSlot(Trailing, leadingAndTrailingStyle);

        TextDirection textDirection = Directionality.Of(context);
        Thickness resolvedContentPadding = (ContentPadding
                                            ?? tileTheme.ContentPadding
                                            ?? defaults.ContentPadding!.Value)
            .Resolve(textDirection);
        MaterialState mouseStates = !Enabled || (OnTap is null && OnLongPress is null)
            ? MaterialState.Disabled
            : MaterialState.None;
        MouseCursor effectiveMouseCursor = MouseCursor
                                           ?? tileTheme.MouseCursor?.Resolve(mouseStates)
                                           ?? (mouseStates.HasFlag(MaterialState.Disabled)
                                               ? SystemMouseCursors.Basic
                                               : SystemMouseCursors.Click);
        ListTileTitleAlignment effectiveTitleAlignment = TitleAlignment
                                                         ?? tileTheme.TitleAlignment
                                                         ?? (theme.UseMaterial3
                                                             ? ListTileTitleAlignment.ThreeLine
                                                             : ListTileTitleAlignment.TitleHeight);
        ShapeBorder effectiveShape = Shape ?? tileTheme.Shape ?? new RoundedRectangleBorder(borderRadius:
            Plumix.Rendering.BorderRadius.Circular(0.0));
        ButtonStyle effectiveIconButtonStyle = iconButtonTheme.Style is null
            ? new ButtonStyle(
                ForegroundColor: MaterialStateProperty<Color?>.All(effectiveIconButtonColor))
            : iconButtonTheme.Style with
            {
                ForegroundColor = MaterialStateProperty<Color?>.All(effectiveIconButtonColor)
            };

        Widget content = new ListTileRenderWidget(
            leading: leading,
            title: title,
            subtitle: subtitle,
            trailing: trailing,
            isThreeLine: IsThreeLine
                         ?? tileTheme.IsThreeLine
                         ?? theme.ListTileTheme.IsThreeLine
                         ?? false,
            isDense: isDense,
            visualDensity: VisualDensity ?? tileTheme.VisualDensity ?? theme.VisualDensity,
            textDirection: textDirection,
            titleBaselineType: titleStyle.TextBaseline ?? defaults.TitleTextStyle!.TextBaseline!.Value,
            subtitleBaselineType: subtitleStyle.TextBaseline ?? defaults.SubtitleTextStyle!.TextBaseline,
            horizontalTitleGap: HorizontalTitleGap ?? tileTheme.HorizontalTitleGap ?? 16.0,
            minVerticalPadding: MinVerticalPadding
                                ?? tileTheme.MinVerticalPadding
                                ?? defaults.MinVerticalPadding!.Value,
            minLeadingWidth: MinLeadingWidth ?? tileTheme.MinLeadingWidth ?? defaults.MinLeadingWidth!.Value,
            minTileHeight: MinTileHeight ?? tileTheme.MinTileHeight,
            titleAlignment: effectiveTitleAlignment);
        content = IconTheme.Merge(
            new IconThemeData(Color: effectiveIconColor),
            new IconButtonTheme(
                data: new IconButtonThemeData(effectiveIconButtonStyle),
                child: content));
        content = new SafeArea(
            top: false,
            bottom: false,
            minimum: resolvedContentPadding,
            child: content);
        content = new Ink(
            decoration: new ShapeDecoration(effectiveShape, effectiveTileColor),
            child: content);
        content = new Semantics(
            flags: (Enabled ? SemanticsFlags.IsEnabled : SemanticsFlags.None)
                   | (InternalAddSemanticForOnTap && (OnTap is not null || OnLongPress is not null)
                       ? SemanticsFlags.IsButton
                       : SemanticsFlags.None),
            selected: Selected,
            child: content);

        return new InkWell(
            customBorder: Shape ?? tileTheme.Shape,
            onTap: Enabled ? OnTap : null,
            onLongPress: Enabled ? OnLongPress : null,
            onFocusChange: OnFocusChange,
            mouseCursor: effectiveMouseCursor,
            canRequestFocus: Enabled,
            focusNode: FocusNode,
            focusColor: FocusColor,
            hoverColor: HoverColor,
            splashColor: SplashColor,
            autofocus: Autofocus,
            enableFeedback: EnableFeedback ?? tileTheme.EnableFeedback ?? true,
            excludeFromSemantics: false,
            statesController: StatesController,
            child: content);
    }

    private static Color? ResolveContentColor(
        MaterialStateProperty<Color?>? explicitColor,
        Color? selectedColor,
        Color? enabledColor,
        Color? disabledColor,
        MaterialState states)
    {
        if (explicitColor is not null)
        {
            return explicitColor.Resolve(states);
        }

        if (states.HasFlag(MaterialState.Disabled))
        {
            return disabledColor;
        }

        return states.HasFlag(MaterialState.Selected) ? selectedColor : enabledColor;
    }

    private static Widget? WrapSlot(Widget? child, TextStyle style)
    {
        return child is null
            ? null
            : new AnimatedDefaultTextStyle(
                style: style,
                duration: ThemeChangeDuration,
                child: child);
    }

    private static ListTileThemeData ResolveDefaults(ThemeData theme, ListTileStyle style)
    {
        if (theme.UseMaterial3)
        {
            return new ListTileThemeData(
                Shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(0.0)),
                SelectedColor: theme.ColorScheme.Primary,
                IconColor: MaterialStateProperty<Color?>.All(theme.ColorScheme.OnSurfaceVariant),
                TitleTextStyle: theme.TextTheme.BodyLarge.CopyWith(color: theme.ColorScheme.OnSurface),
                SubtitleTextStyle: theme.TextTheme.BodyMedium.CopyWith(color: theme.ColorScheme.OnSurfaceVariant),
                LeadingAndTrailingTextStyle:
                    theme.TextTheme.LabelSmall.CopyWith(color: theme.ColorScheme.OnSurfaceVariant),
                ContentPadding: EdgeInsetsGeometry.DirectionalOnly(start: 16.0, end: 24.0),
                TileColor: Colors.Transparent,
                MinLeadingWidth: 24.0,
                MinVerticalPadding: 8.0);
        }

        TextStyle titleStyle = style == ListTileStyle.Drawer
            ? theme.TextTheme.BodyLarge
            : theme.TextTheme.TitleMedium;
        return new ListTileThemeData(
            Shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(0.0)),
            SelectedColor: theme.ColorScheme.Primary,
            IconColor: theme.Brightness == Brightness.Light
                ? MaterialStateProperty<Color?>.All(M2LightDefaultIconColor)
                : null,
            TitleTextStyle: titleStyle,
            SubtitleTextStyle: theme.TextTheme.BodyMedium.CopyWith(color: theme.TextTheme.BodySmall.Color),
            LeadingAndTrailingTextStyle: theme.TextTheme.BodyMedium,
            ContentPadding: EdgeInsetsGeometry.Symmetric(horizontal: 16.0),
            TileColor: Colors.Transparent,
            MinLeadingWidth: 40.0,
            MinVerticalPadding: 4.0,
            Style: style);
    }
}
