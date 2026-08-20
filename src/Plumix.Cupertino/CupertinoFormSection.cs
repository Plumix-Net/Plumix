using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/form_section.dart

/// <summary>An iOS-style form section composed from <see cref="CupertinoListSection"/>.</summary>
public sealed class CupertinoFormSection : StatelessWidget
{
    private static readonly EdgeInsetsGeometry DefaultInsetGroupedRowsMargin =
        EdgeInsetsGeometry.DirectionalOnly(start: 20.0, end: 20.0, bottom: 10.0);

    private CupertinoFormSection(
        CupertinoListSectionType type,
        IReadOnlyList<Widget> children,
        Widget? header,
        Widget? footer,
        EdgeInsetsGeometry margin,
        CupertinoDynamicColor backgroundColor,
        BoxDecoration? decoration,
        Clip clipBehavior,
        Key? key) : base(key)
    {
        if (children is null || children.Count == 0)
        {
            throw new ArgumentException("A form section requires at least one child.", nameof(children));
        }

        Type = type;
        Header = header;
        Footer = footer;
        Margin = margin;
        Children = children;
        Decoration = decoration;
        BackgroundColor = backgroundColor;
        ClipBehavior = clipBehavior;
    }

    public CupertinoFormSection(
        IReadOnlyList<Widget> children,
        Widget? header = null,
        Widget? footer = null,
        EdgeInsetsGeometry? margin = null,
        CupertinoDynamicColor? backgroundColor = null,
        BoxDecoration? decoration = null,
        Clip clipBehavior = Clip.None,
        Key? key = null) : this(
        CupertinoListSectionType.Base,
        children,
        header,
        footer,
        margin ?? EdgeInsetsGeometry.Zero,
        backgroundColor ?? CupertinoColors.SystemGroupedBackground,
        decoration,
        clipBehavior,
        key)
    {
    }

    /// <summary>The section header displayed above the rows.</summary>
    public Widget? Header { get; }

    /// <summary>The section footer displayed below the rows.</summary>
    public Widget? Footer { get; }

    /// <summary>Margin around the decorated row group.</summary>
    public EdgeInsetsGeometry Margin { get; }

    /// <summary>The form rows in this section.</summary>
    public IReadOnlyList<Widget> Children { get; }

    /// <summary>The decoration around the row group.</summary>
    public BoxDecoration? Decoration { get; }

    /// <summary>The background behind the section.</summary>
    public CupertinoDynamicColor BackgroundColor { get; }

    /// <summary>The clip applied to the decorated row group.</summary>
    public Clip ClipBehavior { get; }

    private CupertinoListSectionType Type { get; }

    public static CupertinoFormSection InsetGrouped(
        IReadOnlyList<Widget> children,
        Widget? header = null,
        Widget? footer = null,
        EdgeInsetsGeometry? margin = null,
        CupertinoDynamicColor? backgroundColor = null,
        BoxDecoration? decoration = null,
        Clip clipBehavior = Clip.None,
        Key? key = null)
    {
        return new CupertinoFormSection(
            CupertinoListSectionType.InsetGrouped,
            children,
            header,
            footer,
            margin ?? DefaultInsetGroupedRowsMargin,
            backgroundColor ?? CupertinoColors.SystemGroupedBackground,
            decoration,
            clipBehavior,
            key);
    }

    public override Widget Build(BuildContext context)
    {
        Widget? headerWidget = Header is null
            ? null
            : new DefaultTextStyle(
                style: new TextStyle(
                    FontSize: 13.0,
                    Color: CupertinoDynamicColor.Resolve(CupertinoColors.SecondaryLabel, context)),
                child: Header);
        Widget? footerWidget = Footer is null
            ? null
            : new DefaultTextStyle(
                style: new TextStyle(
                    FontSize: 13.0,
                    Color: CupertinoDynamicColor.Resolve(CupertinoColors.SecondaryLabel, context)),
                child: Footer);

        switch (Type)
        {
            case CupertinoListSectionType.Base:
                return new CupertinoListSection(
                    header: headerWidget,
                    footer: footerWidget,
                    margin: Margin,
                    backgroundColor: BackgroundColor,
                    decoration: Decoration,
                    clipBehavior: ClipBehavior,
                    hasLeading: false,
                    children: Children);
            case CupertinoListSectionType.InsetGrouped:
                return CupertinoListSection.InsetGrouped(
                    header: headerWidget,
                    footer: footerWidget,
                    margin: Margin,
                    backgroundColor: BackgroundColor,
                    decoration: Decoration,
                    clipBehavior: ClipBehavior,
                    hasLeading: false,
                    children: Children);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
