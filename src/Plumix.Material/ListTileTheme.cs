using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/list_tile_theme.dart

public enum ListTileStyle
{
    List,
    Drawer,
}

public enum ListTileControlAffinity
{
    Leading,
    Trailing,
    Platform,
}

public enum ListTileTitleAlignment
{
    ThreeLine,
    TitleHeight,
    Top,
    Center,
    Bottom,
}

public sealed partial record ListTileThemeData(
    bool? Dense = null,
    ShapeBorder? Shape = null,
    ListTileStyle? Style = null,
    Color? SelectedColor = null,
    MaterialStateProperty<Color?>? IconColor = null,
    MaterialStateProperty<Color?>? TextColor = null,
    TextStyle? TitleTextStyle = null,
    TextStyle? SubtitleTextStyle = null,
    TextStyle? LeadingAndTrailingTextStyle = null,
    EdgeInsetsGeometry? ContentPadding = null,
    Color? TileColor = null,
    Color? SelectedTileColor = null,
    double? HorizontalTitleGap = null,
    double? MinVerticalPadding = null,
    double? MinLeadingWidth = null,
    double? MinTileHeight = null,
    bool? EnableFeedback = null,
    MaterialStateProperty<MouseCursor?>? MouseCursor = null,
    VisualDensity? VisualDensity = null,
    ListTileTitleAlignment? TitleAlignment = null,
    ListTileControlAffinity? ControlAffinity = null,
    bool? IsThreeLine = null)
{
    public ListTileThemeData CopyWith(
        bool? dense = null,
        ShapeBorder? shape = null,
        ListTileStyle? style = null,
        Color? selectedColor = null,
        MaterialStateProperty<Color?>? iconColor = null,
        MaterialStateProperty<Color?>? textColor = null,
        TextStyle? titleTextStyle = null,
        TextStyle? subtitleTextStyle = null,
        TextStyle? leadingAndTrailingTextStyle = null,
        EdgeInsetsGeometry? contentPadding = null,
        Color? tileColor = null,
        Color? selectedTileColor = null,
        double? horizontalTitleGap = null,
        double? minVerticalPadding = null,
        double? minLeadingWidth = null,
        double? minTileHeight = null,
        bool? enableFeedback = null,
        MaterialStateProperty<MouseCursor?>? mouseCursor = null,
        VisualDensity? visualDensity = null,
        ListTileTitleAlignment? titleAlignment = null,
        ListTileControlAffinity? controlAffinity = null,
        bool? isThreeLine = null)
    {
        return new ListTileThemeData(
            Dense: dense ?? Dense,
            Shape: shape ?? Shape,
            Style: style ?? Style,
            SelectedColor: selectedColor ?? SelectedColor,
            IconColor: iconColor ?? IconColor,
            TextColor: textColor ?? TextColor,
            TitleTextStyle: titleTextStyle ?? TitleTextStyle,
            SubtitleTextStyle: subtitleTextStyle ?? SubtitleTextStyle,
            LeadingAndTrailingTextStyle: leadingAndTrailingTextStyle ?? LeadingAndTrailingTextStyle,
            ContentPadding: contentPadding ?? ContentPadding,
            TileColor: tileColor ?? TileColor,
            SelectedTileColor: selectedTileColor ?? SelectedTileColor,
            HorizontalTitleGap: horizontalTitleGap ?? HorizontalTitleGap,
            MinVerticalPadding: minVerticalPadding ?? MinVerticalPadding,
            MinLeadingWidth: minLeadingWidth ?? MinLeadingWidth,
            MinTileHeight: minTileHeight ?? MinTileHeight,
            EnableFeedback: enableFeedback ?? EnableFeedback,
            MouseCursor: mouseCursor ?? MouseCursor,
            VisualDensity: visualDensity ?? VisualDensity,
            TitleAlignment: titleAlignment ?? TitleAlignment,
            ControlAffinity: controlAffinity ?? ControlAffinity,
            IsThreeLine: isThreeLine ?? IsThreeLine);
    }
}

public sealed class ListTileTheme : InheritedTheme
{
    private readonly ListTileThemeData? _data;
    private readonly bool? _dense;
    private readonly ShapeBorder? _shape;
    private readonly ListTileStyle? _style;
    private readonly Color? _selectedColor;
    private readonly MaterialStateProperty<Color?>? _iconColor;
    private readonly MaterialStateProperty<Color?>? _textColor;
    private readonly EdgeInsetsGeometry? _contentPadding;
    private readonly Color? _tileColor;
    private readonly Color? _selectedTileColor;
    private readonly bool? _enableFeedback;
    private readonly MaterialStateProperty<MouseCursor?>? _mouseCursor;
    private readonly double? _horizontalTitleGap;
    private readonly double? _minVerticalPadding;
    private readonly double? _minLeadingWidth;
    private readonly ListTileControlAffinity? _controlAffinity;

    public ListTileTheme(
        Widget child,
        ListTileThemeData? data = null,
        bool? dense = null,
        ShapeBorder? shape = null,
        ListTileStyle? style = null,
        Color? selectedColor = null,
        MaterialStateProperty<Color?>? iconColor = null,
        MaterialStateProperty<Color?>? textColor = null,
        EdgeInsetsGeometry? contentPadding = null,
        Color? tileColor = null,
        Color? selectedTileColor = null,
        bool? enableFeedback = null,
        MaterialStateProperty<MouseCursor?>? mouseCursor = null,
        double? horizontalTitleGap = null,
        double? minVerticalPadding = null,
        double? minLeadingWidth = null,
        ListTileControlAffinity? controlAffinity = null,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (data is not null
            && (shape is not null
                || selectedColor.HasValue
                || iconColor is not null
                || textColor is not null
                || contentPadding.HasValue
                || tileColor.HasValue
                || selectedTileColor.HasValue
                || enableFeedback.HasValue
                || mouseCursor is not null
                || horizontalTitleGap.HasValue
                || minVerticalPadding.HasValue
                || minLeadingWidth.HasValue
                || controlAffinity.HasValue))
        {
            throw new ArgumentException("ListTileTheme data cannot be combined with legacy override fields.");
        }

        Child = child;
        _data = data;
        _dense = dense;
        _shape = shape;
        _style = style;
        _selectedColor = selectedColor;
        _iconColor = iconColor;
        _textColor = textColor;
        _contentPadding = contentPadding;
        _tileColor = tileColor;
        _selectedTileColor = selectedTileColor;
        _enableFeedback = enableFeedback;
        _mouseCursor = mouseCursor;
        _horizontalTitleGap = horizontalTitleGap;
        _minVerticalPadding = minVerticalPadding;
        _minLeadingWidth = minLeadingWidth;
        _controlAffinity = controlAffinity;
    }

    public Widget Child { get; }

    public ListTileThemeData Data => _data ?? new ListTileThemeData(
        Dense: _dense,
        Shape: _shape,
        Style: _style,
        SelectedColor: _selectedColor,
        IconColor: _iconColor,
        TextColor: _textColor,
        ContentPadding: _contentPadding,
        TileColor: _tileColor,
        SelectedTileColor: _selectedTileColor,
        EnableFeedback: _enableFeedback,
        MouseCursor: _mouseCursor,
        HorizontalTitleGap: _horizontalTitleGap,
        MinVerticalPadding: _minVerticalPadding,
        MinLeadingWidth: _minLeadingWidth,
        ControlAffinity: _controlAffinity);

    public bool? Dense => _data?.Dense ?? _dense;

    public ShapeBorder? Shape => _data?.Shape ?? _shape;

    public ListTileStyle? Style => _data?.Style ?? _style;

    public Color? SelectedColor => _data?.SelectedColor ?? _selectedColor;

    public MaterialStateProperty<Color?>? IconColor => _data?.IconColor ?? _iconColor;

    public MaterialStateProperty<Color?>? TextColor => _data?.TextColor ?? _textColor;

    public EdgeInsetsGeometry? ContentPadding => _data?.ContentPadding ?? _contentPadding;

    public Color? TileColor => _data?.TileColor ?? _tileColor;

    public Color? SelectedTileColor => _data?.SelectedTileColor ?? _selectedTileColor;

    public bool? EnableFeedback => _data?.EnableFeedback ?? _enableFeedback;

    public double? HorizontalTitleGap => _data?.HorizontalTitleGap ?? _horizontalTitleGap;

    public double? MinVerticalPadding => _data?.MinVerticalPadding ?? _minVerticalPadding;

    public double? MinLeadingWidth => _data?.MinLeadingWidth ?? _minLeadingWidth;

    public ListTileControlAffinity? ControlAffinity => _data?.ControlAffinity ?? _controlAffinity;

    public override Widget Build(BuildContext context) => Child;

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new ListTileTheme(
            child: child,
            data: new ListTileThemeData(
                Dense: Dense,
                Shape: Shape,
                Style: Style,
                SelectedColor: SelectedColor,
                IconColor: IconColor,
                TextColor: TextColor,
                ContentPadding: ContentPadding,
                TileColor: TileColor,
                SelectedTileColor: SelectedTileColor,
                EnableFeedback: EnableFeedback,
                HorizontalTitleGap: HorizontalTitleGap,
                MinVerticalPadding: MinVerticalPadding,
                MinLeadingWidth: MinLeadingWidth,
                IsThreeLine: _data?.IsThreeLine));
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((ListTileTheme)oldWidget).Data, Data);
    }

    public static ListTileThemeData Of(BuildContext context)
    {
        return context.DependOnInherited<ListTileTheme>()?.Data ?? Theme.Of(context).ListTileTheme;
    }

    public static Widget Merge(
        Widget child,
        bool? dense = null,
        ShapeBorder? shape = null,
        ListTileStyle? style = null,
        Color? selectedColor = null,
        MaterialStateProperty<Color?>? iconColor = null,
        MaterialStateProperty<Color?>? textColor = null,
        TextStyle? titleTextStyle = null,
        TextStyle? subtitleTextStyle = null,
        TextStyle? leadingAndTrailingTextStyle = null,
        EdgeInsetsGeometry? contentPadding = null,
        Color? tileColor = null,
        Color? selectedTileColor = null,
        bool? enableFeedback = null,
        double? horizontalTitleGap = null,
        double? minVerticalPadding = null,
        double? minLeadingWidth = null,
        double? minTileHeight = null,
        ListTileTitleAlignment? titleAlignment = null,
        MaterialStateProperty<MouseCursor?>? mouseCursor = null,
        VisualDensity? visualDensity = null,
        ListTileControlAffinity? controlAffinity = null,
        bool? isThreeLine = null,
        Key? key = null)
    {
        return new Builder(context =>
        {
            ListTileThemeData parent = Of(context);
            return new ListTileTheme(
                key: key,
                data: new ListTileThemeData(
                    Dense: dense ?? parent.Dense,
                    Shape: shape ?? parent.Shape,
                    Style: style ?? parent.Style,
                    SelectedColor: selectedColor ?? parent.SelectedColor,
                    IconColor: iconColor ?? parent.IconColor,
                    TextColor: textColor ?? parent.TextColor,
                    TitleTextStyle: titleTextStyle ?? parent.TitleTextStyle,
                    SubtitleTextStyle: subtitleTextStyle ?? parent.SubtitleTextStyle,
                    LeadingAndTrailingTextStyle:
                        leadingAndTrailingTextStyle ?? parent.LeadingAndTrailingTextStyle,
                    ContentPadding: contentPadding ?? parent.ContentPadding,
                    TileColor: tileColor ?? parent.TileColor,
                    SelectedTileColor: selectedTileColor ?? parent.SelectedTileColor,
                    EnableFeedback: enableFeedback ?? parent.EnableFeedback,
                    HorizontalTitleGap: horizontalTitleGap ?? parent.HorizontalTitleGap,
                    MinVerticalPadding: minVerticalPadding ?? parent.MinVerticalPadding,
                    MinLeadingWidth: minLeadingWidth ?? parent.MinLeadingWidth,
                    MinTileHeight: minTileHeight ?? parent.MinTileHeight,
                    TitleAlignment: titleAlignment ?? parent.TitleAlignment,
                    MouseCursor: mouseCursor ?? parent.MouseCursor,
                    VisualDensity: visualDensity ?? parent.VisualDensity,
                    ControlAffinity: controlAffinity ?? parent.ControlAffinity,
                    IsThreeLine: isThreeLine ?? parent.IsThreeLine),
                child: child);
        });
    }
}
