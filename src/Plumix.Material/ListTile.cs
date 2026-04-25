using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/list_tile.dart (approximate)

public sealed class ListTile : StatelessWidget
{
    private static readonly Action Noop = () => { };
    private static readonly Color M2LightDefaultIconColor = Color.FromArgb(0x73, 0x00, 0x00, 0x00);

    public ListTile(
        Widget? title = null,
        Widget? subtitle = null,
        Widget? leading = null,
        Widget? trailing = null,
        bool? isThreeLine = null,
        bool? dense = null,
        ListTileStyle? style = null,
        Color? selectedColor = null,
        Color? iconColor = null,
        Color? textColor = null,
        TextStyle? titleTextStyle = null,
        TextStyle? subtitleTextStyle = null,
        TextStyle? leadingAndTrailingTextStyle = null,
        Thickness? contentPadding = null,
        bool enabled = true,
        Action? onTap = null,
        Action? onLongPress = null,
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
        BorderRadius? shape = null,
        Key? key = null) : base(key)
    {
        if (isThreeLine == true && subtitle is null)
        {
            throw new ArgumentException("ListTile with isThreeLine=true requires a non-null subtitle.", nameof(isThreeLine));
        }

        HorizontalTitleGap = ValidateNonNegativeFinite(horizontalTitleGap, nameof(horizontalTitleGap));
        MinVerticalPadding = ValidateNonNegativeFinite(minVerticalPadding, nameof(minVerticalPadding));
        MinLeadingWidth = ValidateNonNegativeFinite(minLeadingWidth, nameof(minLeadingWidth));
        MinTileHeight = ValidateNonNegativeFinite(minTileHeight, nameof(minTileHeight));

        Title = title;
        Subtitle = subtitle;
        Leading = leading;
        Trailing = trailing;
        IsThreeLine = isThreeLine;
        Dense = dense;
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
        Shape = shape;
    }

    public Widget? Title { get; }

    public Widget? Subtitle { get; }

    public Widget? Leading { get; }

    public Widget? Trailing { get; }

    public bool? IsThreeLine { get; }

    public bool? Dense { get; }

    public ListTileStyle? Style { get; }

    public Color? SelectedColor { get; }

    public Color? IconColor { get; }

    public Color? TextColor { get; }

    public TextStyle? TitleTextStyle { get; }

    public TextStyle? SubtitleTextStyle { get; }

    public TextStyle? LeadingAndTrailingTextStyle { get; }

    public Thickness? ContentPadding { get; }

    public bool Enabled { get; }

    public Action? OnTap { get; }

    public Action? OnLongPress { get; }

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

    public BorderRadius? Shape { get; }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var tileTheme = ListTileTheme.Of(context);
        var useMaterial3 = theme.UseMaterial3;
        var effectiveStyle = Style ?? tileTheme.Style ?? ListTileStyle.List;
        var isDense = Dense ?? tileTheme.Dense ?? false;
        var hasSubtitle = Subtitle is not null;
        var isThreeLine = hasSubtitle && (IsThreeLine ?? tileTheme.IsThreeLine ?? false);
        var lineCount = hasSubtitle ? (isThreeLine ? 3 : 2) : 1;
        var effectiveHorizontalTitleGap = HorizontalTitleGap ?? tileTheme.HorizontalTitleGap ?? 16;
        var effectiveMinVerticalPadding = MinVerticalPadding ?? tileTheme.MinVerticalPadding ?? (useMaterial3 ? 8 : 4);
        var effectiveMinLeadingWidth = MinLeadingWidth ?? tileTheme.MinLeadingWidth ?? (useMaterial3 ? 24 : 40);
        var effectiveMinTileHeight = MinTileHeight ?? tileTheme.MinTileHeight ?? ResolveDefaultTileHeight(lineCount, isDense);
        var textDirection = Directionality.Of(context);
        var effectiveContentPadding = ContentPadding
            ?? tileTheme.ContentPadding
            ?? ResolveDefaultContentPadding(useMaterial3, textDirection);
        var effectiveShape = Shape ?? tileTheme.Shape ?? BorderRadius.Zero;
        var selectedColor = SelectedColor ?? tileTheme.SelectedColor ?? theme.PrimaryColor;
        var disabledColor = ApplyOpacity(theme.OnSurfaceColor, 0.38);
        var defaultIconColor = ResolveDefaultIconColor(theme);
        var defaultTitleColor = useMaterial3 ? theme.OnSurfaceColor : theme.OnSurfaceColor;
        var defaultSubtitleColor = useMaterial3
            ? theme.OnSurfaceVariantColor
            : ApplyOpacity(theme.OnSurfaceColor, 0.60);
        var defaultLeadingTrailingTextColor = useMaterial3
            ? theme.OnSurfaceVariantColor
            : theme.OnSurfaceColor;
        var configuredTextColor = TextColor ?? tileTheme.TextColor;
        var effectiveTitleColor = ResolveContentColor(
            enabled: Enabled,
            selected: Selected,
            selectedColor: selectedColor,
            configuredColor: configuredTextColor,
            defaultColor: defaultTitleColor,
            disabledColor: disabledColor);
        var effectiveSubtitleColor = ResolveContentColor(
            enabled: Enabled,
            selected: Selected,
            selectedColor: selectedColor,
            configuredColor: configuredTextColor,
            defaultColor: defaultSubtitleColor,
            disabledColor: disabledColor);
        var effectiveLeadingTrailingTextColor = ResolveContentColor(
            enabled: Enabled,
            selected: Selected,
            selectedColor: selectedColor,
            configuredColor: configuredTextColor,
            defaultColor: defaultLeadingTrailingTextColor,
            disabledColor: disabledColor);
        var effectiveIconColor = ResolveContentColor(
            enabled: Enabled,
            selected: Selected,
            selectedColor: selectedColor,
            configuredColor: IconColor ?? tileTheme.IconColor,
            defaultColor: defaultIconColor,
            disabledColor: disabledColor);
        var effectiveTileColor = ResolveTileColor(tileTheme);
        var interactionColor = Selected ? selectedColor : theme.PrimaryColor;
        var effectiveMouseCursor = ResolveMouseCursor(tileTheme);

        var titleStyle = ResolveTitleTextStyle(theme, tileTheme, effectiveStyle, isDense, effectiveTitleColor);
        var subtitleStyle = ResolveSubtitleTextStyle(theme, tileTheme, isDense, effectiveSubtitleColor);
        var leadingTrailingStyle = ResolveLeadingTrailingTextStyle(theme, tileTheme, effectiveLeadingTrailingTextColor);

        Widget BuildSlotWidget(Widget child)
        {
            return new IconTheme(
                data: new IconThemeData(Color: effectiveIconColor),
                child: new DefaultTextStyle(
                    style: leadingTrailingStyle,
                    child: child));
        }

        Widget textChild;
        if (Subtitle is null)
        {
            textChild = new DefaultTextStyle(
                style: titleStyle,
                child: ApplyTitleTextDefaults(Title ?? new SizedBox()));
        }
        else
        {
            textChild = new Column(
                mainAxisSize: MainAxisSize.Min,
                crossAxisAlignment: CrossAxisAlignment.Start,
                children:
                [
                    new DefaultTextStyle(
                        style: titleStyle,
                        child: ApplyTitleTextDefaults(Title ?? new SizedBox())),
                    new DefaultTextStyle(
                        style: subtitleStyle,
                        child: ApplySubtitleTextDefaults(Subtitle, isThreeLine)),
                ]);
        }

        var rowChildren = new List<Widget>();
        if (Leading is not null)
        {
            rowChildren.Add(new ConstrainedBox(
                constraints: new BoxConstraints(MinWidth: effectiveMinLeadingWidth),
                child: new Align(
                    alignment: Alignment.CenterLeft,
                    child: BuildSlotWidget(Leading))));
            rowChildren.Add(new SizedBox(width: effectiveHorizontalTitleGap));
        }

        rowChildren.Add(new Expanded(child: textChild));

        if (Trailing is not null)
        {
            rowChildren.Add(new SizedBox(width: effectiveHorizontalTitleGap));
            rowChildren.Add(BuildSlotWidget(Trailing));
        }

        var padding = new Thickness(
            effectiveContentPadding.Left,
            effectiveContentPadding.Top + effectiveMinVerticalPadding,
            effectiveContentPadding.Right,
            effectiveContentPadding.Bottom + effectiveMinVerticalPadding);

        var tileBody = new Align(
            alignment: Alignment.CenterLeft,
            heightFactor: 1,
            child: new Row(
                crossAxisAlignment: CrossAxisAlignment.Center,
                children: rowChildren));

        var child = new ConstrainedBox(
            constraints: new BoxConstraints(MinHeight: effectiveMinTileHeight),
            child: new Container(
                padding: padding,
                child: tileBody));

        var style = new ButtonStyle(
            ForegroundColor: MaterialStateProperty<Color?>.All(effectiveTitleColor),
            BackgroundColor: MaterialStateProperty<Color?>.All(effectiveTileColor),
            OverlayColor: ResolveOverlayColorProperty(interactionColor),
            SplashColor: ResolveSplashColorProperty(interactionColor),
            IconColor: MaterialStateProperty<Color?>.All(effectiveIconColor),
            Padding: MaterialStateProperty<Thickness?>.All(default),
            Shape: MaterialStateProperty<BorderRadius?>.All(effectiveShape),
            MinimumSize: MaterialStateProperty<Size?>.All(new Size(0, effectiveMinTileHeight)),
            TapTargetSize: MaterialTapTargetSize.ShrinkWrap);

        var hasAnyAction = OnTap is not null || OnLongPress is not null;
        Action? onPressed = null;
        if (Enabled && hasAnyAction)
        {
            onPressed = OnTap ?? Noop;
        }

        return new MaterialButtonCore(
            child: child,
            onPressed: onPressed,
            onLongPress: Enabled ? OnLongPress : null,
            style: style,
            mouseCursor: effectiveMouseCursor,
            focusNode: FocusNode,
            autofocus: Autofocus,
            isSelected: Selected,
            includeSemanticSelected: true,
            isSemanticButton: hasAnyAction,
            enableFeedback: EnableFeedback ?? tileTheme.EnableFeedback ?? true);
    }

    private MaterialStateProperty<Color?> ResolveOverlayColorProperty(Color interactionColor)
    {
        if (FocusColor.HasValue || HoverColor.HasValue)
        {
            return MaterialStateProperty<Color?>.ResolveWith(states =>
            {
                if (states.HasFlag(MaterialState.Disabled))
                {
                    return null;
                }

                if (states.HasFlag(MaterialState.Pressed))
                {
                    return FocusColor ?? HoverColor ?? ApplyOpacity(interactionColor, 0.10);
                }

                if (states.HasFlag(MaterialState.Hovered))
                {
                    return HoverColor ?? ApplyOpacity(interactionColor, 0.08);
                }

                if (states.HasFlag(MaterialState.Focused))
                {
                    return FocusColor ?? ApplyOpacity(interactionColor, 0.10);
                }

                return null;
            });
        }

        return MaterialButtonCore.CreateDefaultOverlayResolver(interactionColor);
    }

    private MaterialStateProperty<Color?> ResolveSplashColorProperty(Color interactionColor)
    {
        if (SplashColor.HasValue)
        {
            return MaterialButtonCore.CreateExplicitSplashResolver(SplashColor.Value);
        }

        return MaterialButtonCore.CreateDefaultSplashResolver(interactionColor);
    }

    private MouseCursor ResolveMouseCursor(ListTileThemeData tileTheme)
    {
        if (MouseCursor is not null)
        {
            return MouseCursor;
        }

        if (tileTheme.MouseCursor is not null)
        {
            return tileTheme.MouseCursor;
        }

        var hasAnyAction = OnTap is not null || OnLongPress is not null;
        return Enabled && hasAnyAction
            ? SystemMouseCursors.Click
            : SystemMouseCursors.Basic;
    }

    private Color ResolveTileColor(ListTileThemeData tileTheme)
    {
        if (Selected)
        {
            return SelectedTileColor
                   ?? tileTheme.SelectedTileColor
                   ?? Colors.Transparent;
        }

        return TileColor
               ?? tileTheme.TileColor
               ?? Colors.Transparent;
    }

    private TextStyle ResolveTitleTextStyle(
        ThemeData theme,
        ListTileThemeData tileTheme,
        ListTileStyle style,
        bool dense,
        Color color)
    {
        var fallback = style == ListTileStyle.Drawer
            ? theme.TextTheme.BodyMedium
            : theme.TextTheme.BodyMedium;
        var baseStyle = fallback with
        {
            Color = color
        };
        var styleFromTheme = TitleTextStyle ?? tileTheme.TitleTextStyle ?? baseStyle;
        var effective = styleFromTheme with
        {
            Color = color
        };

        if (dense)
        {
            effective = effective with
            {
                FontSize = 13
            };
        }

        return effective;
    }

    private TextStyle ResolveSubtitleTextStyle(
        ThemeData theme,
        ListTileThemeData tileTheme,
        bool dense,
        Color color)
    {
        var baseStyle = (SubtitleTextStyle
                         ?? tileTheme.SubtitleTextStyle
                         ?? theme.TextTheme.BodyMedium) with
        {
            Color = color
        };

        if (dense)
        {
            baseStyle = baseStyle with
            {
                FontSize = 12
            };
        }

        return baseStyle;
    }

    private TextStyle ResolveLeadingTrailingTextStyle(
        ThemeData theme,
        ListTileThemeData tileTheme,
        Color color)
    {
        return (LeadingAndTrailingTextStyle
                ?? tileTheme.LeadingAndTrailingTextStyle
                ?? theme.TextTheme.LabelLarge) with
        {
            Color = color
        };
    }

    private static double ResolveDefaultTileHeight(int lineCount, bool dense)
    {
        return lineCount switch
        {
            3 => dense ? 76 : 88,
            2 => dense ? 64 : 72,
            _ => dense ? 48 : 56,
        };
    }

    private static Thickness ResolveDefaultContentPadding(bool useMaterial3, TextDirection textDirection)
    {
        if (!useMaterial3)
        {
            return new Thickness(16, 0, 16, 0);
        }

        return textDirection == TextDirection.Rtl
            ? new Thickness(24, 0, 16, 0)
            : new Thickness(16, 0, 24, 0);
    }

    private static Color ResolveDefaultIconColor(ThemeData theme)
    {
        if (theme.UseMaterial3)
        {
            return theme.OnSurfaceVariantColor;
        }

        return theme.Brightness == Brightness.Light
            ? M2LightDefaultIconColor
            : theme.OnSurfaceColor;
    }

    private static Color ResolveContentColor(
        bool enabled,
        bool selected,
        Color selectedColor,
        Color? configuredColor,
        Color defaultColor,
        Color disabledColor)
    {
        if (!enabled)
        {
            return disabledColor;
        }

        if (selected)
        {
            return selectedColor;
        }

        return configuredColor ?? defaultColor;
    }

    private static Color ApplyOpacity(Color color, double opacity)
    {
        var baseOpacity = color.A / 255.0;
        var effectiveOpacity = Math.Clamp(baseOpacity * opacity, 0, 1);
        var alpha = (byte)Math.Clamp((int)(effectiveOpacity * 255), 0, 255);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    private static Widget ApplyTitleTextDefaults(Widget child)
    {
        return ApplyTextLineDefaults(child, maxLines: 1);
    }

    private static Widget ApplySubtitleTextDefaults(Widget child, bool isThreeLine)
    {
        return ApplyTextLineDefaults(child, maxLines: isThreeLine ? 2 : 1);
    }

    private static Widget ApplyTextLineDefaults(Widget child, int maxLines)
    {
        if (child is not Text text)
        {
            return child;
        }

        var effectiveMaxLines = text.MaxLines ?? maxLines;
        var effectiveSoftWrap = text.MaxLines.HasValue
            ? text.SoftWrap
            : effectiveMaxLines > 1;
        var effectiveOverflow = text.MaxLines.HasValue || text.Overflow != TextOverflow.Clip
            ? text.Overflow
            : TextOverflow.Ellipsis;

        return new Text(
            data: text.Data,
            fontSize: text.FontSize,
            color: text.Color,
            fontWeight: text.FontWeight,
            fontStyle: text.FontStyle,
            fontFamily: text.FontFamily,
            height: text.Height,
            letterSpacing: text.LetterSpacing,
            textAlign: text.TextAlign,
            softWrap: effectiveSoftWrap,
            maxLines: effectiveMaxLines,
            overflow: effectiveOverflow,
            textDirection: text.TextDirection,
            key: text.Key);
    }

    private static double? ValidateNonNegativeFinite(double? value, string parameterName)
    {
        if (!value.HasValue)
        {
            return null;
        }

        if (double.IsNaN(value.Value) || double.IsInfinity(value.Value) || value.Value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Value must be non-negative and finite.");
        }

        return value.Value;
    }
}
