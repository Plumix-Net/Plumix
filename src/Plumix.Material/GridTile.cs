using System.Collections.Generic;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/grid_tile.dart (strict structure/behavior port)

/// <summary>
/// A tile in a Material grid, with optional content overlaid at its top and bottom edges.
/// </summary>
public sealed class GridTile : StatelessWidget
{
    public GridTile(
        Widget child,
        Widget? header = null,
        Widget? footer = null,
        Key? key = null) : base(key)
    {
        Child = child;
        Header = header;
        Footer = footer;
    }

    public Widget? Header { get; }

    public Widget? Footer { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        if (Header is null && Footer is null)
        {
            return Child;
        }

        var children = new List<Widget>
        {
            new Positioned(
                left: 0,
                top: 0,
                right: 0,
                bottom: 0,
                child: Child),
        };

        if (Header is not null)
        {
            children.Add(new Positioned(
                left: 0,
                top: 0,
                right: 0,
                child: Header));
        }

        if (Footer is not null)
        {
            children.Add(new Positioned(
                left: 0,
                right: 0,
                bottom: 0,
                child: Footer));
        }

        return new Stack(children: children);
    }
}
