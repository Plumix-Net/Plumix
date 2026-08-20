using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/list_tile.dart

/// <summary>An iOS-style single-line list tile.</summary>
public sealed class CupertinoListTile : StatefulWidget
{
    private const double LeadingSizeDefault = 28.0;
    private const double NotchedLeadingSizeDefault = 30.0;
    private const double LeadingToTitleDefault = 16.0;
    private const double NotchedLeadingToTitleDefault = 12.0;

    private CupertinoListTile(
        CupertinoListTileType type,
        Widget title,
        Widget? subtitle,
        Widget? additionalInfo,
        Widget? leading,
        Widget? trailing,
        Func<Task>? onTap,
        CupertinoDynamicColor? backgroundColor,
        CupertinoDynamicColor? backgroundColorActivated,
        EdgeInsetsGeometry? padding,
        double leadingSize,
        double leadingToTitle,
        Key? key) : base(key)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Type = type;
        Subtitle = subtitle;
        AdditionalInfo = additionalInfo;
        Leading = leading;
        Trailing = trailing;
        OnTap = onTap;
        BackgroundColor = backgroundColor;
        BackgroundColorActivated = backgroundColorActivated;
        Padding = padding;
        LeadingSize = leadingSize;
        LeadingToTitle = leadingToTitle;
    }

    public CupertinoListTile(
        Widget title,
        Widget? subtitle = null,
        Widget? additionalInfo = null,
        Widget? leading = null,
        Widget? trailing = null,
        Func<Task>? onTap = null,
        CupertinoDynamicColor? backgroundColor = null,
        CupertinoDynamicColor? backgroundColorActivated = null,
        EdgeInsetsGeometry? padding = null,
        double leadingSize = LeadingSizeDefault,
        double leadingToTitle = LeadingToTitleDefault,
        Key? key = null) : this(
        CupertinoListTileType.Base,
        title,
        subtitle,
        additionalInfo,
        leading,
        trailing,
        onTap,
        backgroundColor,
        backgroundColorActivated,
        padding,
        leadingSize,
        leadingToTitle,
        key)
    {
    }

    public Widget Title { get; }

    public Widget? Subtitle { get; }

    public Widget? AdditionalInfo { get; }

    public Widget? Leading { get; }

    public Widget? Trailing { get; }

    public Func<Task>? OnTap { get; }

    public CupertinoDynamicColor? BackgroundColor { get; }

    public CupertinoDynamicColor? BackgroundColorActivated { get; }

    public EdgeInsetsGeometry? Padding { get; }

    public double LeadingSize { get; }

    public double LeadingToTitle { get; }

    private CupertinoListTileType Type { get; }

    public static CupertinoListTile Notched(
        Widget title,
        Widget? subtitle = null,
        Widget? additionalInfo = null,
        Widget? leading = null,
        Widget? trailing = null,
        Func<Task>? onTap = null,
        CupertinoDynamicColor? backgroundColor = null,
        CupertinoDynamicColor? backgroundColorActivated = null,
        EdgeInsetsGeometry? padding = null,
        double leadingSize = NotchedLeadingSizeDefault,
        double leadingToTitle = NotchedLeadingToTitleDefault,
        Key? key = null)
    {
        return new CupertinoListTile(
            CupertinoListTileType.Notched,
            title,
            subtitle,
            additionalInfo,
            leading,
            trailing,
            onTap,
            backgroundColor,
            backgroundColorActivated,
            padding,
            leadingSize,
            leadingToTitle,
            key);
    }

    public override State CreateState() => new CupertinoListTileState();

    private enum CupertinoListTileType
    {
        Base,
        Notched,
    }

    private sealed class CupertinoListTileState : State
    {
        private const double MinHeight = 44.0;
        private const double MinHeightWithSubtitle = 48.0;
        private const double NotchedMinHeight = 54.0;
        private const double NotchedMinHeightWithoutLeading = 50.0;
        private const double NotchedTitleToSubtitle = 3.0;
        private const double AdditionalInfoToTrailing = 6.0;
        private const double NotchedTitleWithSubtitleFontSize = 16.0;
        private const double SubtitleFontSize = 12.0;
        private const double NotchedSubtitleFontSize = 14.0;

        private static readonly EdgeInsetsGeometry DefaultPadding =
            EdgeInsetsGeometry.DirectionalOnly(start: 20.0, end: 14.0);
        private static readonly EdgeInsetsGeometry NotchedPadding =
            EdgeInsetsGeometry.Symmetric(horizontal: 14.0);
        private static readonly EdgeInsetsGeometry NotchedPaddingWithoutLeading =
            EdgeInsetsGeometry.DirectionalOnly(start: 28.0, top: 10.0, end: 14.0, bottom: 10.0);

        private bool _tapped;

        private CupertinoListTile CurrentWidget => (CupertinoListTile)StateWidget;

        public override Widget Build(BuildContext context)
        {
            TextStyle textStyle = CupertinoTheme.Of(context).TextTheme.TextStyle;
            TextStyle coloredStyle = textStyle.CopyWith(
                color: CupertinoDynamicColor.Resolve(CupertinoColors.SecondaryLabel, context));
            bool baseType = CurrentWidget.Type == CupertinoListTileType.Base;
            TextStyle titleStyle = baseType || CurrentWidget.Subtitle is null
                ? textStyle
                : textStyle.CopyWith(
                    fontWeight: FontWeight.DemiBold,
                    fontSize: CurrentWidget.Leading is null ? NotchedTitleWithSubtitleFontSize : null);
            Widget title = new DefaultTextStyle(
                style: titleStyle,
                maxLines: 1,
                overflow: TextOverflow.Ellipsis,
                child: CurrentWidget.Title);
            EdgeInsetsGeometry padding = CurrentWidget.Padding ?? ResolvePadding(baseType);
            Color backgroundColor = ResolveBackgroundColor(context);
            double minHeight = ResolveMinHeight(baseType);

            Widget child = new ConstrainedBox(
                constraints: new BoxConstraints(
                    MinWidth: double.PositiveInfinity,
                    MinHeight: minHeight),
                child: new ColoredBox(
                    color: backgroundColor,
                    child: new Padding(
                        insets: padding,
                        child: new Row(children: BuildRowChildren(title, coloredStyle, baseType)))));
            if (CurrentWidget.OnTap is null)
            {
                return child;
            }

            return new GestureDetector(
                onTapDown: _ => SetTapped(true),
                onTapCancel: () => SetTapped(false),
                onTap: HandleTap,
                behavior: HitTestBehavior.Opaque,
                child: child);
        }

        private IReadOnlyList<Widget> BuildRowChildren(
            Widget title,
            TextStyle coloredStyle,
            bool baseType)
        {
            var children = new List<Widget>();
            if (CurrentWidget.Leading is not null)
            {
                children.Add(new SizedBox(
                    width: CurrentWidget.LeadingSize,
                    height: CurrentWidget.LeadingSize,
                    child: new Center(child: CurrentWidget.Leading)));
                children.Add(new SizedBox(width: CurrentWidget.LeadingToTitle));
            }
            else
            {
                children.Add(new SizedBox(height: CurrentWidget.LeadingSize));
            }

            var titleChildren = new List<Widget> { title };
            if (CurrentWidget.Subtitle is not null)
            {
                titleChildren.Add(new SizedBox(height: NotchedTitleToSubtitle));
                titleChildren.Add(new DefaultTextStyle(
                    style: coloredStyle.CopyWith(
                        fontSize: baseType ? SubtitleFontSize : NotchedSubtitleFontSize),
                    maxLines: 1,
                    overflow: TextOverflow.Ellipsis,
                    child: CurrentWidget.Subtitle));
            }

            children.Add(new Expanded(
                child: new Column(
                    mainAxisAlignment: MainAxisAlignment.SpaceBetween,
                    crossAxisAlignment: CrossAxisAlignment.Start,
                    children: titleChildren)));
            if (CurrentWidget.AdditionalInfo is not null)
            {
                children.Add(new DefaultTextStyle(
                    style: coloredStyle,
                    maxLines: 1,
                    child: CurrentWidget.AdditionalInfo));
                if (CurrentWidget.Trailing is not null)
                {
                    children.Add(new SizedBox(width: AdditionalInfoToTrailing));
                }
            }

            if (CurrentWidget.Trailing is not null)
            {
                children.Add(CurrentWidget.Trailing);
            }

            return children;
        }

        private EdgeInsetsGeometry ResolvePadding(bool baseType)
        {
            if (baseType)
            {
                return DefaultPadding;
            }

            return CurrentWidget.Leading is null
                ? NotchedPaddingWithoutLeading
                : NotchedPadding;
        }

        private double ResolveMinHeight(bool baseType)
        {
            if (baseType)
            {
                return CurrentWidget.Subtitle is null ? MinHeight : MinHeightWithSubtitle;
            }

            return CurrentWidget.Leading is null ? NotchedMinHeightWithoutLeading : NotchedMinHeight;
        }

        private Color ResolveBackgroundColor(BuildContext context)
        {
            if (_tapped)
            {
                return CupertinoDynamicColor.Resolve(
                    CurrentWidget.BackgroundColorActivated ?? CupertinoColors.SystemGrey4,
                    context);
            }

            return CupertinoDynamicColor.Resolve(
                CurrentWidget.BackgroundColor ?? CupertinoColors.Transparent,
                context);
        }

        private async void HandleTap()
        {
            await CurrentWidget.OnTap!();
            if (Mounted)
            {
                SetTapped(false);
            }
        }

        private void SetTapped(bool value)
        {
            if (_tapped != value)
            {
                SetState(() => _tapped = value);
            }
        }
    }
}

/// <summary>The standard trailing chevron for an actionable <see cref="CupertinoListTile"/>.</summary>
public sealed class CupertinoListTileChevron : StatelessWidget
{
    public CupertinoListTileChevron(Key? key = null) : base(key)
    {
    }

    public override Widget Build(BuildContext context)
    {
        return new Icon(
            CupertinoIcons.RightChevron,
            size: CupertinoTheme.Of(context).TextTheme.TextStyle.FontSize,
            color: CupertinoDynamicColor.Resolve(CupertinoColors.SystemGrey2, context));
    }
}
