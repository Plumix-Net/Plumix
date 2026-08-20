using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/list_section.dart

/// <summary>Denotes the visual variant of a <see cref="CupertinoListSection"/>.</summary>
public enum CupertinoListSectionType
{
    Base,
    InsetGrouped,
}

/// <summary>An iOS-style list section with headers, footers, row dividers, and grouped decoration.</summary>
public sealed class CupertinoListSection : StatelessWidget
{
    private const double MarginTop = 22.0;
    private const double BaseDividerMargin = 20.0;
    private const double BaseAdditionalDividerMargin = 44.0;
    private const double InsetDividerMargin = 14.0;
    private const double InsetAdditionalDividerMargin = 42.0;
    private const double InsetAdditionalDividerMarginWithoutLeading = 14.0;

    private static readonly EdgeInsetsGeometry DefaultHeaderMargin =
        EdgeInsetsGeometry.DirectionalOnly(start: 20.0, end: 20.0, bottom: 6.0);
    private static readonly EdgeInsetsGeometry InsetGroupedDefaultHeaderMargin =
        EdgeInsetsGeometry.DirectionalOnly(start: 20.0, top: 16.0, end: 20.0, bottom: 6.0);
    private static readonly EdgeInsetsGeometry DefaultFooterMargin =
        EdgeInsetsGeometry.DirectionalOnly(start: 20.0, end: 20.0);
    private static readonly EdgeInsetsGeometry InsetGroupedDefaultFooterMargin =
        EdgeInsetsGeometry.DirectionalOnly(start: 20.0, end: 20.0, bottom: 10.0);
    private static readonly EdgeInsetsGeometry DefaultRowsMargin =
        EdgeInsetsGeometry.Only(bottom: 8.0);
    private static readonly EdgeInsetsGeometry DefaultInsetGroupedRowsMargin =
        EdgeInsetsGeometry.DirectionalOnly(start: 20.0, top: 20.0, end: 20.0, bottom: 10.0);
    private static readonly EdgeInsetsGeometry DefaultInsetGroupedRowsMarginWithHeader =
        EdgeInsetsGeometry.DirectionalOnly(start: 20.0, end: 20.0, bottom: 10.0);
    private static readonly BorderRadius DefaultInsetGroupedBorderRadius = BorderRadius.Circular(10.0);
    private static readonly CupertinoDynamicColor HeaderFooterColor = new(
        color: Color.FromRgb(108, 108, 108),
        darkColor: Color.FromRgb(142, 142, 146),
        highContrastColor: Color.FromRgb(74, 74, 77),
        darkHighContrastColor: Color.FromRgb(176, 176, 183),
        elevatedColor: Color.FromRgb(108, 108, 108),
        darkElevatedColor: Color.FromRgb(142, 142, 146),
        highContrastElevatedColor: Color.FromRgb(108, 108, 108),
        darkHighContrastElevatedColor: Color.FromRgb(142, 142, 146));

    private CupertinoListSection(
        CupertinoListSectionType type,
        IReadOnlyList<Widget>? children,
        Widget? header,
        Widget? footer,
        EdgeInsetsGeometry margin,
        CupertinoDynamicColor backgroundColor,
        BoxDecoration? decoration,
        Clip clipBehavior,
        double dividerMargin,
        double additionalDividerMargin,
        double? topMargin,
        CupertinoDynamicColor? separatorColor,
        Key? key) : base(key)
    {
        if ((children is null || children.Count == 0) && header is null)
        {
            throw new ArgumentException("A list section requires at least one child or a header.");
        }

        Type = type;
        Header = header;
        Footer = footer;
        Margin = margin;
        Children = children;
        Decoration = decoration;
        BackgroundColor = backgroundColor;
        ClipBehavior = clipBehavior;
        DividerMargin = dividerMargin;
        AdditionalDividerMargin = additionalDividerMargin;
        TopMargin = topMargin;
        SeparatorColor = separatorColor;
    }

    public CupertinoListSection(
        IReadOnlyList<Widget>? children = null,
        Widget? header = null,
        Widget? footer = null,
        EdgeInsetsGeometry? margin = null,
        CupertinoDynamicColor? backgroundColor = null,
        BoxDecoration? decoration = null,
        Clip clipBehavior = Clip.None,
        double dividerMargin = BaseDividerMargin,
        double? additionalDividerMargin = null,
        double? topMargin = MarginTop,
        bool hasLeading = true,
        CupertinoDynamicColor? separatorColor = null,
        Key? key = null) : this(
        CupertinoListSectionType.Base,
        children,
        header,
        footer,
        margin ?? DefaultRowsMargin,
        backgroundColor ?? CupertinoColors.SystemGroupedBackground,
        decoration,
        clipBehavior,
        dividerMargin,
        additionalDividerMargin ?? (hasLeading ? BaseAdditionalDividerMargin : 0.0),
        topMargin,
        separatorColor,
        key)
    {
    }

    public CupertinoListSectionType Type { get; }

    public Widget? Header { get; }

    public Widget? Footer { get; }

    public EdgeInsetsGeometry Margin { get; }

    public IReadOnlyList<Widget>? Children { get; }

    public BoxDecoration? Decoration { get; }

    public CupertinoDynamicColor BackgroundColor { get; }

    public Clip ClipBehavior { get; }

    public double DividerMargin { get; }

    public double AdditionalDividerMargin { get; }

    public double? TopMargin { get; }

    public CupertinoDynamicColor? SeparatorColor { get; }

    public static CupertinoListSection InsetGrouped(
        IReadOnlyList<Widget>? children = null,
        Widget? header = null,
        Widget? footer = null,
        EdgeInsetsGeometry? margin = null,
        CupertinoDynamicColor? backgroundColor = null,
        BoxDecoration? decoration = null,
        Clip clipBehavior = Clip.HardEdge,
        double dividerMargin = InsetDividerMargin,
        double? additionalDividerMargin = null,
        double? topMargin = null,
        bool hasLeading = true,
        CupertinoDynamicColor? separatorColor = null,
        Key? key = null)
    {
        return new CupertinoListSection(
            CupertinoListSectionType.InsetGrouped,
            children,
            header,
            footer,
            margin ?? (header is null
                ? DefaultInsetGroupedRowsMargin
                : DefaultInsetGroupedRowsMarginWithHeader),
            backgroundColor ?? CupertinoColors.SystemGroupedBackground,
            decoration,
            clipBehavior,
            dividerMargin,
            additionalDividerMargin ?? (hasLeading
                ? InsetAdditionalDividerMargin
                : InsetAdditionalDividerMarginWithoutLeading),
            topMargin,
            separatorColor,
            key);
    }

    public override Widget Build(BuildContext context)
    {
        Color dividerColor = CupertinoDynamicColor.Resolve(SeparatorColor ?? CupertinoColors.Separator, context);
        double dividerHeight = 1.0 / MediaQuery.Of(context).DevicePixelRatio;
        Widget longDivider = new Container(color: dividerColor, height: dividerHeight);
        Widget shortDivider = new Container(
            margin: EdgeInsetsGeometry.DirectionalOnly(start: DividerMargin + AdditionalDividerMargin),
            color: dividerColor,
            height: dividerHeight);

        TextStyle style = CupertinoTheme.Of(context).TextTheme.TextStyle;
        Widget? headerWidget = null;
        Widget? footerWidget = null;
        switch (Type)
        {
            case CupertinoListSectionType.Base:
                style = style.Merge(new TextStyle(
                    FontSize: 13.0,
                    Color: CupertinoDynamicColor.Resolve(HeaderFooterColor, context)));
                if (Header is not null)
                {
                    headerWidget = new DefaultTextStyle(style, Header);
                }

                if (Footer is not null)
                {
                    footerWidget = new DefaultTextStyle(style, Footer);
                }

                break;
            case CupertinoListSectionType.InsetGrouped:
                if (Header is not null)
                {
                    headerWidget = new DefaultTextStyle(
                        style.Merge(new TextStyle(FontSize: 20.0, FontWeight: FontWeight.Bold)),
                        Header);
                }

                if (Footer is not null)
                {
                    footerWidget = new DefaultTextStyle(style, Footer);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        Widget? decoratedChildrenGroup = BuildChildrenGroup(context, longDivider, shortDivider);
        var sectionChildren = new List<Widget>();
        if (Type == CupertinoListSectionType.Base)
        {
            sectionChildren.Add(new SizedBox(height: TopMargin));
        }

        if (headerWidget is not null)
        {
            sectionChildren.Add(new Align(
                alignment: AlignmentDirectional.CenterStart,
                child: new Padding(
                    Type == CupertinoListSectionType.Base
                        ? DefaultHeaderMargin
                        : InsetGroupedDefaultHeaderMargin,
                    headerWidget)));
        }

        if (decoratedChildrenGroup is not null)
        {
            sectionChildren.Add(decoratedChildrenGroup);
        }

        if (footerWidget is not null)
        {
            sectionChildren.Add(new Align(
                alignment: AlignmentDirectional.CenterStart,
                child: new Padding(
                    Type == CupertinoListSectionType.Base
                        ? DefaultFooterMargin
                        : InsetGroupedDefaultFooterMargin,
                    footerWidget)));
        }

        return new DecoratedBox(
            decoration: new BoxDecoration(
                Color: CupertinoDynamicColor.Resolve(BackgroundColor, context)),
            child: new Column(children: sectionChildren));
    }

    private Widget? BuildChildrenGroup(BuildContext context, Widget longDivider, Widget shortDivider)
    {
        if (Children is null || Children.Count == 0)
        {
            return null;
        }

        var childrenWithDividers = new List<Widget>();
        if (Type == CupertinoListSectionType.Base)
        {
            childrenWithDividers.Add(longDivider);
        }

        for (int index = 0; index < Children.Count - 1; index += 1)
        {
            childrenWithDividers.Add(Children[index]);
            childrenWithDividers.Add(shortDivider);
        }

        childrenWithDividers.Add(Children[^1]);
        if (Type == CupertinoListSectionType.Base)
        {
            childrenWithDividers.Add(longDivider);
        }

        BorderRadius borderRadius = Type == CupertinoListSectionType.InsetGrouped
            ? DefaultInsetGroupedBorderRadius
            : BorderRadius.Zero;
        Decoration effectiveDecoration = Decoration ?? (Decoration)new ShapeDecoration(
            Shape: new RoundedSuperellipseBorder(borderRadius: borderRadius),
            Color: CupertinoDynamicColor.Resolve(CupertinoColors.SecondarySystemGroupedBackground, context));
        Widget decoratedChildrenGroup = new DecoratedBox(
            decoration: effectiveDecoration,
            child: new Column(children: childrenWithDividers));

        if (ClipBehavior != Clip.None)
        {
            decoratedChildrenGroup = new ClipRSuperellipse(
                borderRadius: borderRadius,
                clipBehavior: ClipBehavior,
                child: decoratedChildrenGroup);
        }

        return new Padding(Margin, decoratedChildrenGroup);
    }
}
