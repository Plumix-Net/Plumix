using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/form_row.dart

/// <summary>An iOS-style split form row with optional helper and error content.</summary>
public sealed class CupertinoFormRow : StatelessWidget
{
    private static readonly EdgeInsetsGeometry DefaultPadding =
        EdgeInsetsGeometry.DirectionalOnly(start: 20.0, top: 6.0, end: 6.0, bottom: 6.0);

    public CupertinoFormRow(
        Widget child,
        Widget? prefix = null,
        EdgeInsetsGeometry? padding = null,
        Widget? helper = null,
        Widget? error = null,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Prefix = prefix;
        Padding = padding;
        Helper = helper;
        Error = error;
    }

    /// <summary>A widget displayed at the start of the row.</summary>
    public Widget? Prefix { get; }

    /// <summary>Content padding for the row, or the standard iOS form-row padding when null.</summary>
    public EdgeInsetsGeometry? Padding { get; }

    /// <summary>Informational content displayed below the prefix and child.</summary>
    public Widget? Helper { get; }

    /// <summary>Error content displayed below the helper.</summary>
    public Widget? Error { get; }

    /// <summary>The trailing, horizontally flexible form control.</summary>
    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        CupertinoThemeData theme = CupertinoTheme.Of(context);
        TextStyle textStyle = theme.TextTheme.TextStyle.CopyWith(
            color: theme.TextTheme.TextStyle.Color);

        var rowChildren = new List<Widget>();
        if (Prefix is not null)
        {
            rowChildren.Add(new DefaultTextStyle(textStyle, Prefix));
        }

        rowChildren.Add(new Flexible(
            child: new Align(
                alignment: AlignmentDirectional.CenterEnd,
                child: Child)));

        var columnChildren = new List<Widget>
        {
            new Row(
                mainAxisAlignment: MainAxisAlignment.SpaceBetween,
                children: rowChildren),
        };
        if (Helper is not null)
        {
            columnChildren.Add(new Align(
                alignment: AlignmentDirectional.CenterStart,
                child: new DefaultTextStyle(textStyle, Helper)));
        }

        if (Error is not null)
        {
            columnChildren.Add(new Align(
                alignment: AlignmentDirectional.CenterStart,
                child: new DefaultTextStyle(
                    style: new TextStyle(
                        Color: CupertinoColors.DestructiveRed.Value,
                        FontWeight: FontWeight.Medium),
                    child: Error)));
        }

        return new Padding(
            insets: Padding ?? DefaultPadding,
            child: new Column(children: columnChildren));
    }
}
