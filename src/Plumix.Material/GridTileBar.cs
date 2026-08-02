using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/grid_tile_bar.dart

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
        BoxDecoration? decoration = null;
        if (BackgroundColor.HasValue)
        {
            decoration = new BoxDecoration(Color: BackgroundColor.Value);
        }

        var padding = EdgeInsetsDirectional.Only(
            start: Leading is not null ? 8.0 : 16.0,
            end: Trailing is not null ? 8.0 : 16.0);
        var darkTheme = ThemeData.Dark;

        var children = new List<Widget>();
        if (Leading is not null)
        {
            children.Add(new Padding(
                insets: EdgeInsetsDirectional.Only(end: 8.0),
                child: Leading));
        }

        if (Title is not null && Subtitle is not null)
        {
            children.Add(new Expanded(
                child: new Column(
                    mainAxisAlignment: MainAxisAlignment.Center,
                    crossAxisAlignment: CrossAxisAlignment.Start,
                    children:
                    [
                        new DefaultTextStyle(
                            style: darkTheme.TextTheme.TitleMedium,
                            softWrap: false,
                            overflow: TextOverflow.Ellipsis,
                            child: Title),
                        new DefaultTextStyle(
                            style: darkTheme.TextTheme.BodySmall,
                            softWrap: false,
                            overflow: TextOverflow.Ellipsis,
                            child: Subtitle),
                    ])));
        }
        else if (Title is not null || Subtitle is not null)
        {
            children.Add(new Expanded(
                child: new DefaultTextStyle(
                    style: darkTheme.TextTheme.TitleMedium,
                    softWrap: false,
                    overflow: TextOverflow.Ellipsis,
                    child: Title ?? Subtitle!)));
        }

        if (Trailing is not null)
        {
            children.Add(new Padding(
                insets: EdgeInsetsDirectional.Only(start: 8.0),
                child: Trailing));
        }

        return new Container(
            decoration: decoration,
            height: Title is not null && Subtitle is not null ? 68.0 : 48.0,
            child: new Padding(
                insets: padding,
                child: new Theme(
                    data: darkTheme,
                    child: IconTheme.Merge(
                        data: new IconThemeData(Color: Colors.White),
                        child: new Row(children: children)))));
    }
}
