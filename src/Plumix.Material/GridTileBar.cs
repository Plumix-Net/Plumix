using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/grid_tile_bar.dart (strict structure/behavior port)

/// <summary>
/// A one- or two-line header or footer for a <see cref="GridTile"/>.
/// </summary>
public sealed class GridTileBar : StatelessWidget
{
    public GridTileBar(
        Color? backgroundColor = null,
        Widget? leading = null,
        Widget? title = null,
        Widget? subtitle = null,
        Widget? trailing = null,
        Key? key = null) : base(key)
    {
        BackgroundColor = backgroundColor;
        Leading = leading;
        Title = title;
        Subtitle = subtitle;
        Trailing = trailing;
    }

    public Color? BackgroundColor { get; }

    public Widget? Leading { get; }

    public Widget? Title { get; }

    public Widget? Subtitle { get; }

    public Widget? Trailing { get; }

    public override Widget Build(BuildContext context)
    {
        var direction = Directionality.Of(context);
        var startPadding = Leading is not null ? 8.0 : 16.0;
        var endPadding = Trailing is not null ? 8.0 : 16.0;
        var padding = direction == TextDirection.Ltr
            ? new Thickness(startPadding, 0, endPadding, 0)
            : new Thickness(endPadding, 0, startPadding, 0);
        var darkTheme = ThemeData.Dark;

        var children = new List<Widget>();
        if (Leading is not null)
        {
            children.Add(new Padding(
                insets: direction == TextDirection.Ltr
                    ? new Thickness(0, 0, 8, 0)
                    : new Thickness(8, 0, 0, 0),
                child: Leading));
        }

        if (Title is not null && Subtitle is not null)
        {
            children.Add(new Expanded(
                child: new Column(
                    mainAxisAlignment: MainAxisAlignment.Center,
                    crossAxisAlignment: CrossAxisAlignment.Start,
                    textDirection: direction,
                    children:
                    [
                        new DefaultTextStyle(
                            style: darkTheme.TextTheme.TitleMedium,
                            softWrap: false,
                            overflow: TextOverflow.Ellipsis,
                            child: ApplyTextDirection(Title, direction)),
                        new DefaultTextStyle(
                            style: darkTheme.TextTheme.BodySmall,
                            softWrap: false,
                            overflow: TextOverflow.Ellipsis,
                            child: ApplyTextDirection(Subtitle, direction)),
                    ])));
        }
        else if (Title is not null || Subtitle is not null)
        {
            children.Add(new Expanded(
                child: new DefaultTextStyle(
                    style: darkTheme.TextTheme.TitleMedium,
                    softWrap: false,
                    overflow: TextOverflow.Ellipsis,
                    child: ApplyTextDirection(Title ?? Subtitle!, direction))));
        }

        if (Trailing is not null)
        {
            children.Add(new Padding(
                insets: direction == TextDirection.Ltr
                    ? new Thickness(8, 0, 0, 0)
                    : new Thickness(0, 0, 8, 0),
                child: Trailing));
        }

        Widget child = new Theme(
            data: darkTheme,
            child: new IconTheme(
                data: new IconThemeData(Color: Colors.White),
                child: new Row(children: children, textDirection: direction)));

        return new Container(
            padding: padding,
            decoration: BackgroundColor.HasValue
                ? new BoxDecoration(Color: BackgroundColor.Value)
                : null,
            height: Title is not null && Subtitle is not null ? 68 : 48,
            child: child);
    }

    private static Widget ApplyTextDirection(Widget child, TextDirection direction)
    {
        if (child is not Text text)
        {
            return child;
        }

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
            softWrap: text.SoftWrap,
            maxLines: text.MaxLines,
            overflow: text.Overflow,
            textDirection: direction,
            key: text.Key);
    }
}
