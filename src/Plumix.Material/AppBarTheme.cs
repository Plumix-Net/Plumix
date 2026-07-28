using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/app_bar_theme.dart

public sealed class AppBarTheme : InheritedTheme
{
    public AppBarTheme(
        Widget? child = null,
        Color? color = null,
        Color? backgroundColor = null,
        Color? foregroundColor = null,
        double? elevation = null,
        double? scrolledUnderElevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        ShapeBorder? shape = null,
        IconThemeData? iconTheme = null,
        IconThemeData? actionsIconTheme = null,
        bool? centerTitle = null,
        double? titleSpacing = null,
        double? leadingWidth = null,
        double? toolbarHeight = null,
        TextStyle? toolbarTextStyle = null,
        TextStyle? titleTextStyle = null,
        SystemUiOverlayStyle? systemOverlayStyle = null,
        Thickness? actionsPadding = null,
        AppBarThemeData? data = null,
        Key? key = null) : base(key)
    {
        if (color.HasValue && backgroundColor.HasValue)
        {
            throw new ArgumentException(
                "color and backgroundColor mean the same thing. Only specify one.");
        }

        bool hasLegacyProperties = color.HasValue
                                   || backgroundColor.HasValue
                                   || foregroundColor.HasValue
                                   || elevation.HasValue
                                   || scrolledUnderElevation.HasValue
                                   || shadowColor.HasValue
                                   || surfaceTintColor.HasValue
                                   || shape is not null
                                   || iconTheme is not null
                                   || actionsIconTheme is not null
                                   || centerTitle.HasValue
                                   || titleSpacing.HasValue
                                   || leadingWidth.HasValue
                                   || toolbarHeight.HasValue
                                   || toolbarTextStyle is not null
                                   || titleTextStyle is not null
                                   || systemOverlayStyle is not null
                                   || actionsPadding.HasValue;
        if (data is not null && hasLegacyProperties)
        {
            throw new ArgumentException(
                "data cannot be combined with individual AppBarTheme properties.",
                nameof(data));
        }

        Data = data ?? new AppBarThemeData(
            BackgroundColor: backgroundColor ?? color,
            ForegroundColor: foregroundColor,
            IconTheme: iconTheme,
            ActionsIconTheme: actionsIconTheme,
            CenterTitle: centerTitle,
            TitleSpacing: titleSpacing,
            LeadingWidth: leadingWidth,
            ToolbarHeight: toolbarHeight,
            ToolbarTextStyle: toolbarTextStyle,
            TitleTextStyle: titleTextStyle,
            ActionsPadding: actionsPadding,
            SystemOverlayStyle: systemOverlayStyle,
            Elevation: elevation,
            ScrolledUnderElevation: scrolledUnderElevation,
            ShadowColor: shadowColor,
            SurfaceTintColor: surfaceTintColor,
            Shape: shape);
        Child = child ?? new SizedBox();
    }

    public AppBarThemeData Data { get; }

    public Widget Child { get; }

    public Color? BackgroundColor => Data.BackgroundColor;

    public Color? ForegroundColor => Data.ForegroundColor;

    public double? Elevation => Data.Elevation;

    public double? ScrolledUnderElevation => Data.ScrolledUnderElevation;

    public Color? ShadowColor => Data.ShadowColor;

    public Color? SurfaceTintColor => Data.SurfaceTintColor;

    public ShapeBorder? Shape => Data.Shape;

    public IconThemeData? IconTheme => Data.IconTheme;

    public IconThemeData? ActionsIconTheme => Data.ActionsIconTheme;

    public bool? CenterTitle => Data.CenterTitle;

    public double? TitleSpacing => Data.TitleSpacing;

    public double? LeadingWidth => Data.LeadingWidth;

    public double? ToolbarHeight => Data.ToolbarHeight;

    public TextStyle? ToolbarTextStyle => Data.ToolbarTextStyle;

    public TextStyle? TitleTextStyle => Data.TitleTextStyle;

    public SystemUiOverlayStyle? SystemOverlayStyle => Data.SystemOverlayStyle;

    public Thickness? ActionsPadding => Data.ActionsPadding;

    public AppBarTheme CopyWith(
        Color? color = null,
        Color? backgroundColor = null,
        Color? foregroundColor = null,
        double? elevation = null,
        double? scrolledUnderElevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        ShapeBorder? shape = null,
        IconThemeData? iconTheme = null,
        IconThemeData? actionsIconTheme = null,
        bool? centerTitle = null,
        double? titleSpacing = null,
        double? leadingWidth = null,
        double? toolbarHeight = null,
        TextStyle? toolbarTextStyle = null,
        TextStyle? titleTextStyle = null,
        SystemUiOverlayStyle? systemOverlayStyle = null,
        Thickness? actionsPadding = null)
    {
        if (color.HasValue && backgroundColor.HasValue)
        {
            throw new ArgumentException(
                "color and backgroundColor mean the same thing. Only specify one.");
        }

        return new AppBarTheme(
            backgroundColor: backgroundColor ?? color ?? BackgroundColor,
            foregroundColor: foregroundColor ?? ForegroundColor,
            elevation: elevation ?? Elevation,
            scrolledUnderElevation: scrolledUnderElevation ?? ScrolledUnderElevation,
            shadowColor: shadowColor ?? ShadowColor,
            surfaceTintColor: surfaceTintColor ?? SurfaceTintColor,
            shape: shape ?? Shape,
            iconTheme: iconTheme ?? IconTheme,
            actionsIconTheme: actionsIconTheme ?? ActionsIconTheme,
            centerTitle: centerTitle ?? CenterTitle,
            titleSpacing: titleSpacing ?? TitleSpacing,
            leadingWidth: leadingWidth ?? LeadingWidth,
            toolbarHeight: toolbarHeight ?? ToolbarHeight,
            toolbarTextStyle: toolbarTextStyle ?? ToolbarTextStyle,
            titleTextStyle: titleTextStyle ?? TitleTextStyle,
            systemOverlayStyle: systemOverlayStyle ?? SystemOverlayStyle,
            actionsPadding: actionsPadding ?? ActionsPadding);
    }

    public override Widget Build(BuildContext context) => Child;

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new AppBarTheme(
            data: Data,
            child: child);
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((AppBarTheme)oldWidget).Data, Data);
    }

    public static AppBarThemeData Of(BuildContext context)
    {
        return context.DependOnInherited<AppBarTheme>()?.Data ?? Theme.Of(context).AppBarTheme;
    }

    public static AppBarTheme Lerp(AppBarTheme? a, AppBarTheme? b, double t)
    {
        if (ReferenceEquals(a, b) && a is not null)
        {
            return a;
        }

        return new AppBarTheme(data: AppBarThemeData.Lerp(a?.Data, b?.Data, t));
    }
}
