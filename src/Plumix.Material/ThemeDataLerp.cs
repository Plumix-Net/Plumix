using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// C#-only infrastructure: centralized partial record bodies for Material theme interpolation.

public sealed partial record ActionIconThemeData
{
    public static ActionIconThemeData? Lerp(ActionIconThemeData? a, ActionIconThemeData? b, double t)
    {
        if (a is null && b is null)
        {
            return null;
        }

        return new ActionIconThemeData(
            BackButtonIconBuilder: t < 0.5 ? a?.BackButtonIconBuilder : b?.BackButtonIconBuilder,
            CloseButtonIconBuilder: t < 0.5 ? a?.CloseButtonIconBuilder : b?.CloseButtonIconBuilder,
            DrawerButtonIconBuilder: t < 0.5 ? a?.DrawerButtonIconBuilder : b?.DrawerButtonIconBuilder,
            EndDrawerButtonIconBuilder: t < 0.5
                ? a?.EndDrawerButtonIconBuilder
                : b?.EndDrawerButtonIconBuilder);
    }
}

public sealed partial record TextButtonThemeData
{
    public static TextButtonThemeData? Lerp(TextButtonThemeData? a, TextButtonThemeData? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new TextButtonThemeData(ButtonStyle.Lerp(a?.Style, b?.Style, t));
    }
}

public sealed partial record ElevatedButtonThemeData
{
    public static ElevatedButtonThemeData? Lerp(ElevatedButtonThemeData? a, ElevatedButtonThemeData? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new ElevatedButtonThemeData(ButtonStyle.Lerp(a?.Style, b?.Style, t));
    }
}

public sealed partial record OutlinedButtonThemeData
{
    public static OutlinedButtonThemeData? Lerp(OutlinedButtonThemeData? a, OutlinedButtonThemeData? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new OutlinedButtonThemeData(ButtonStyle.Lerp(a?.Style, b?.Style, t));
    }
}

public sealed partial record FilledButtonThemeData
{
    public static FilledButtonThemeData? Lerp(FilledButtonThemeData? a, FilledButtonThemeData? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new FilledButtonThemeData(ButtonStyle.Lerp(a?.Style, b?.Style, t));
    }
}

public sealed partial record CarouselViewThemeData
{
    public static CarouselViewThemeData Lerp(CarouselViewThemeData? a, CarouselViewThemeData? b, double t)
    {
        if (ReferenceEquals(a, b) && a is not null)
        {
            return a;
        }

        return new CarouselViewThemeData(
            Elevation: MaterialThemeLerp.Double(a?.Elevation, b?.Elevation, t),
            BackgroundColor: MaterialThemeLerp.Color(a?.BackgroundColor, b?.BackgroundColor, t),
            OverlayColor: MaterialThemeLerp.ColorStateProperty(a?.OverlayColor, b?.OverlayColor, t),
            Shape: MaterialThemeLerp.Shape(a?.Shape, b?.Shape, t),
            Padding: MaterialThemeLerp.Thickness(a?.Padding, b?.Padding, t),
            ItemClipBehavior: t < 0.5 ? a?.ItemClipBehavior : b?.ItemClipBehavior);
    }
}

public sealed partial record CheckboxThemeData
{
    public static CheckboxThemeData Lerp(CheckboxThemeData? a, CheckboxThemeData? b, double t)
    {
        if (ReferenceEquals(a, b) && a is not null)
        {
            return a;
        }

        return new CheckboxThemeData(
            MouseCursor: t < 0.5 ? a?.MouseCursor : b?.MouseCursor,
            FillColor: MaterialThemeLerp.ColorStateProperty(a?.FillColor, b?.FillColor, t),
            CheckColor: MaterialThemeLerp.ColorStateProperty(a?.CheckColor, b?.CheckColor, t),
            OverlayColor: MaterialThemeLerp.ColorStateProperty(a?.OverlayColor, b?.OverlayColor, t),
            SplashRadius: MaterialThemeLerp.Double(a?.SplashRadius, b?.SplashRadius, t),
            MaterialTapTargetSize: t < 0.5 ? a?.MaterialTapTargetSize : b?.MaterialTapTargetSize,
            VisualDensity: t < 0.5 ? a?.VisualDensity : b?.VisualDensity,
            Shape: MaterialThemeLerp.Shape(a?.Shape, b?.Shape, t),
            Side: WidgetStateBorderSide.Lerp(a?.Side, b?.Side, t));
    }
}

public sealed partial record DividerThemeData
{
    public static DividerThemeData Lerp(DividerThemeData? a, DividerThemeData? b, double t)
    {
        if (ReferenceEquals(a, b) && a is not null)
        {
            return a;
        }

        return new DividerThemeData(
            Color: MaterialThemeLerp.Color(a?.Color, b?.Color, t),
            Space: MaterialThemeLerp.Double(a?.Space, b?.Space, t),
            Thickness: MaterialThemeLerp.Double(a?.Thickness, b?.Thickness, t),
            Indent: MaterialThemeLerp.Double(a?.Indent, b?.Indent, t),
            EndIndent: MaterialThemeLerp.Double(a?.EndIndent, b?.EndIndent, t),
            Radius: MaterialThemeLerp.BorderRadiusGeometry(a?.Radius, b?.Radius, t));
    }
}

public sealed partial record DrawerThemeData
{
    public static DrawerThemeData? Lerp(DrawerThemeData? a, DrawerThemeData? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new DrawerThemeData(
            BackgroundColor: MaterialThemeLerp.Color(a?.BackgroundColor, b?.BackgroundColor, t),
            ScrimColor: MaterialThemeLerp.Color(a?.ScrimColor, b?.ScrimColor, t),
            Elevation: MaterialThemeLerp.Double(a?.Elevation, b?.Elevation, t),
            ShadowColor: MaterialThemeLerp.Color(a?.ShadowColor, b?.ShadowColor, t),
            Width: MaterialThemeLerp.Double(a?.Width, b?.Width, t),
            SurfaceTintColor: MaterialThemeLerp.Color(a?.SurfaceTintColor, b?.SurfaceTintColor, t),
            Shape: MaterialThemeLerp.Shape(a?.Shape, b?.Shape, t),
            EndShape: MaterialThemeLerp.Shape(a?.EndShape, b?.EndShape, t),
            ClipBehavior: t < 0.5 ? a?.ClipBehavior : b?.ClipBehavior);
    }
}

public sealed partial record TextSelectionThemeData
{
    public static TextSelectionThemeData? Lerp(
        TextSelectionThemeData? a,
        TextSelectionThemeData? b,
        double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new TextSelectionThemeData(
            CursorColor: MaterialThemeLerp.Color(a?.CursorColor, b?.CursorColor, t),
            SelectionColor: MaterialThemeLerp.Color(a?.SelectionColor, b?.SelectionColor, t),
            SelectionHandleColor: MaterialThemeLerp.Color(
                a?.SelectionHandleColor,
                b?.SelectionHandleColor,
                t));
    }
}

public sealed partial record ToggleButtonsThemeData
{
    public static ToggleButtonsThemeData? Lerp(
        ToggleButtonsThemeData? a,
        ToggleButtonsThemeData? b,
        double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new ToggleButtonsThemeData(
            TextStyle: MaterialThemeLerp.TextStyle(a?.TextStyle, b?.TextStyle, t),
            Constraints: MaterialThemeLerp.BoxConstraints(a?.Constraints, b?.Constraints, t),
            Color: MaterialThemeLerp.Color(a?.Color, b?.Color, t),
            SelectedColor: MaterialThemeLerp.Color(a?.SelectedColor, b?.SelectedColor, t),
            DisabledColor: MaterialThemeLerp.Color(a?.DisabledColor, b?.DisabledColor, t),
            FillColor: MaterialThemeLerp.Color(a?.FillColor, b?.FillColor, t),
            FocusColor: MaterialThemeLerp.Color(a?.FocusColor, b?.FocusColor, t),
            HighlightColor: MaterialThemeLerp.Color(a?.HighlightColor, b?.HighlightColor, t),
            HoverColor: MaterialThemeLerp.Color(a?.HoverColor, b?.HoverColor, t),
            SplashColor: MaterialThemeLerp.Color(a?.SplashColor, b?.SplashColor, t),
            BorderColor: MaterialThemeLerp.Color(a?.BorderColor, b?.BorderColor, t),
            SelectedBorderColor: MaterialThemeLerp.Color(
                a?.SelectedBorderColor,
                b?.SelectedBorderColor,
                t),
            DisabledBorderColor: MaterialThemeLerp.Color(
                a?.DisabledBorderColor,
                b?.DisabledBorderColor,
                t),
            BorderRadius: MaterialThemeLerp.BorderRadius(a?.BorderRadius, b?.BorderRadius, t),
            BorderWidth: MaterialThemeLerp.Double(a?.BorderWidth, b?.BorderWidth, t));
    }
}

public sealed partial record TooltipThemeData
{
    public static TooltipThemeData? Lerp(TooltipThemeData? a, TooltipThemeData? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new TooltipThemeData(
            Height: MaterialThemeLerp.Double(a?.Height, b?.Height, t),
            Constraints: MaterialThemeLerp.BoxConstraints(a?.Constraints, b?.Constraints, t),
            Padding: MaterialThemeLerp.Thickness(a?.Padding, b?.Padding, t),
            Margin: MaterialThemeLerp.Thickness(a?.Margin, b?.Margin, t),
            VerticalOffset: MaterialThemeLerp.Double(a?.VerticalOffset, b?.VerticalOffset, t),
            PreferBelow: t < 0.5 ? a?.PreferBelow : b?.PreferBelow,
            ExcludeFromSemantics: t < 0.5 ? a?.ExcludeFromSemantics : b?.ExcludeFromSemantics,
            Decoration: MaterialThemeLerp.Decoration(a?.Decoration, b?.Decoration, t),
            TextStyle: MaterialThemeLerp.TextStyle(a?.TextStyle, b?.TextStyle, t),
            TextAlign: t < 0.5 ? a?.TextAlign : b?.TextAlign,
            WaitDuration: t < 0.5 ? a?.WaitDuration : b?.WaitDuration,
            ShowDuration: t < 0.5 ? a?.ShowDuration : b?.ShowDuration,
            ExitDuration: t < 0.5 ? a?.ExitDuration : b?.ExitDuration,
            TriggerMode: t < 0.5 ? a?.TriggerMode : b?.TriggerMode,
            EnableFeedback: t < 0.5 ? a?.EnableFeedback : b?.EnableFeedback);
    }
}

public sealed partial record BottomSheetThemeData
{
    public static BottomSheetThemeData? Lerp(BottomSheetThemeData? a, BottomSheetThemeData? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        BottomSheetThemeData selected = t < 0.5
            ? a ?? new BottomSheetThemeData()
            : b ?? new BottomSheetThemeData();
        return selected with
        {
            BackgroundColor = MaterialThemeLerp.Color(a?.BackgroundColor, b?.BackgroundColor, t),
            SurfaceTintColor = MaterialThemeLerp.Color(a?.SurfaceTintColor, b?.SurfaceTintColor, t),
            Elevation = MaterialThemeLerp.Double(a?.Elevation, b?.Elevation, t),
            ModalBackgroundColor = MaterialThemeLerp.Color(
                a?.ModalBackgroundColor,
                b?.ModalBackgroundColor,
                t),
            ModalBarrierColor = MaterialThemeLerp.Color(a?.ModalBarrierColor, b?.ModalBarrierColor, t),
            ShadowColor = MaterialThemeLerp.Color(a?.ShadowColor, b?.ShadowColor, t),
            ModalElevation = MaterialThemeLerp.Double(a?.ModalElevation, b?.ModalElevation, t),
            Shape = MaterialThemeLerp.Shape(a?.Shape, b?.Shape, t),
            ShowDragHandle = t < 0.5 ? a?.ShowDragHandle : b?.ShowDragHandle,
            DragHandleColor = MaterialThemeLerp.ColorStateProperty(
                a?.DragHandleColor,
                b?.DragHandleColor,
                t),
            DragHandleSize = MaterialThemeLerp.Size(a?.DragHandleSize, b?.DragHandleSize, t),
            ClipBehavior = t < 0.5 ? a?.ClipBehavior : b?.ClipBehavior,
            Constraints = MaterialThemeLerp.BoxConstraints(a?.Constraints, b?.Constraints, t),
        };
    }
}

public sealed partial record ChipThemeData
{
    public static ChipThemeData? Lerp(ChipThemeData? a, ChipThemeData? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new ChipThemeData(
            Color: MaterialThemeLerp.ColorStateProperty(a?.Color, b?.Color, t),
            BackgroundColor: MaterialThemeLerp.Color(a?.BackgroundColor, b?.BackgroundColor, t),
            DeleteIconColor: MaterialThemeLerp.Color(a?.DeleteIconColor, b?.DeleteIconColor, t),
            DisabledColor: MaterialThemeLerp.Color(a?.DisabledColor, b?.DisabledColor, t),
            SelectedColor: MaterialThemeLerp.Color(a?.SelectedColor, b?.SelectedColor, t),
            SecondarySelectedColor: MaterialThemeLerp.Color(
                a?.SecondarySelectedColor,
                b?.SecondarySelectedColor,
                t),
            ShadowColor: MaterialThemeLerp.Color(a?.ShadowColor, b?.ShadowColor, t),
            SurfaceTintColor: MaterialThemeLerp.Color(a?.SurfaceTintColor, b?.SurfaceTintColor, t),
            SelectedShadowColor: MaterialThemeLerp.Color(
                a?.SelectedShadowColor,
                b?.SelectedShadowColor,
                t),
            ShowCheckmark: t < 0.5 ? a?.ShowCheckmark ?? true : b?.ShowCheckmark ?? true,
            CheckmarkColor: MaterialThemeLerp.Color(a?.CheckmarkColor, b?.CheckmarkColor, t),
            LabelPadding: MaterialThemeLerp.Thickness(a?.LabelPadding, b?.LabelPadding, t),
            Padding: MaterialThemeLerp.Thickness(a?.Padding, b?.Padding, t),
            Side: MaterialThemeLerp.BorderSide(a?.Side, b?.Side, t),
            Shape: MaterialThemeLerp.Shape(a?.Shape, b?.Shape, t),
            LabelStyle: MaterialThemeLerp.TextStyle(a?.LabelStyle, b?.LabelStyle, t),
            SecondaryLabelStyle: MaterialThemeLerp.TextStyle(
                a?.SecondaryLabelStyle,
                b?.SecondaryLabelStyle,
                t),
            Brightness: t < 0.5
                ? a?.Brightness ?? global::Plumix.Material.Brightness.Light
                : b?.Brightness ?? global::Plumix.Material.Brightness.Light,
            Elevation: MaterialThemeLerp.Double(a?.Elevation, b?.Elevation, t),
            PressElevation: MaterialThemeLerp.Double(a?.PressElevation, b?.PressElevation, t),
            IconTheme: a?.IconTheme is not null || b?.IconTheme is not null
                ? IconThemeData.Lerp(a?.IconTheme, b?.IconTheme, t)
                : null,
            AvatarBoxConstraints: MaterialThemeLerp.BoxConstraints(
                a?.AvatarBoxConstraints,
                b?.AvatarBoxConstraints,
                t),
            DeleteIconBoxConstraints: MaterialThemeLerp.BoxConstraints(
                a?.DeleteIconBoxConstraints,
                b?.DeleteIconBoxConstraints,
                t));
    }
}

public sealed partial record DataTableThemeData
{
    public static DataTableThemeData Lerp(DataTableThemeData a, DataTableThemeData b, double t)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        DataTableThemeData selected = t < 0.5 ? a : b;
        return selected with
        {
            Decoration = MaterialThemeLerp.Decoration(a.Decoration, b.Decoration, t),
            DataRowColor = MaterialThemeLerp.ColorStateProperty(a.DataRowColor, b.DataRowColor, t),
            DataRowMinHeight = MaterialThemeLerp.Double(a.DataRowMinHeight, b.DataRowMinHeight, t),
            DataRowMaxHeight = MaterialThemeLerp.Double(a.DataRowMaxHeight, b.DataRowMaxHeight, t),
            DataTextStyle = MaterialThemeLerp.TextStyle(a.DataTextStyle, b.DataTextStyle, t),
            HeadingRowColor = MaterialThemeLerp.ColorStateProperty(
                a.HeadingRowColor,
                b.HeadingRowColor,
                t),
            HeadingRowHeight = MaterialThemeLerp.Double(a.HeadingRowHeight, b.HeadingRowHeight, t),
            HeadingTextStyle = MaterialThemeLerp.TextStyle(a.HeadingTextStyle, b.HeadingTextStyle, t),
            HorizontalMargin = MaterialThemeLerp.Double(a.HorizontalMargin, b.HorizontalMargin, t),
            ColumnSpacing = MaterialThemeLerp.Double(a.ColumnSpacing, b.ColumnSpacing, t),
            DividerThickness = MaterialThemeLerp.Double(a.DividerThickness, b.DividerThickness, t),
            CheckboxHorizontalMargin = MaterialThemeLerp.Double(
                a.CheckboxHorizontalMargin,
                b.CheckboxHorizontalMargin,
                t),
            HeadingCellCursor = t < 0.5 ? a.HeadingCellCursor : b.HeadingCellCursor,
            DataRowCursor = t < 0.5 ? a.DataRowCursor : b.DataRowCursor,
            HeadingRowAlignment = t < 0.5 ? a.HeadingRowAlignment : b.HeadingRowAlignment,
        };
    }
}

public sealed partial record DialogThemeData
{
    public static DialogThemeData Lerp(DialogThemeData? a, DialogThemeData? b, double t)
    {
        if (ReferenceEquals(a, b) && a is not null)
        {
            return a;
        }

        DialogThemeData selected = t < 0.5
            ? a ?? new DialogThemeData()
            : b ?? new DialogThemeData();
        return selected with
        {
            BackgroundColor = MaterialThemeLerp.Color(a?.BackgroundColor, b?.BackgroundColor, t),
            Elevation = MaterialThemeLerp.Double(a?.Elevation, b?.Elevation, t),
            ShadowColor = MaterialThemeLerp.Color(a?.ShadowColor, b?.ShadowColor, t),
            SurfaceTintColor = MaterialThemeLerp.Color(a?.SurfaceTintColor, b?.SurfaceTintColor, t),
            Shape = MaterialThemeLerp.Shape(a?.Shape, b?.Shape, t),
            Alignment = t < 0.5 ? a?.Alignment : b?.Alignment,
            IconColor = MaterialThemeLerp.Color(a?.IconColor, b?.IconColor, t),
            TitleTextStyle = MaterialThemeLerp.TextStyle(a?.TitleTextStyle, b?.TitleTextStyle, t),
            ContentTextStyle = MaterialThemeLerp.TextStyle(a?.ContentTextStyle, b?.ContentTextStyle, t),
            ActionsPadding = MaterialThemeLerp.Thickness(a?.ActionsPadding, b?.ActionsPadding, t),
            BarrierColor = MaterialThemeLerp.Color(a?.BarrierColor, b?.BarrierColor, t),
            InsetPadding = MaterialThemeLerp.Thickness(a?.InsetPadding, b?.InsetPadding, t),
            ClipBehavior = t < 0.5 ? a?.ClipBehavior : b?.ClipBehavior,
            Constraints = MaterialThemeLerp.BoxConstraints(a?.Constraints, b?.Constraints, t),
        };
    }
}

public sealed partial record MaterialBannerThemeData
{
    public static MaterialBannerThemeData Lerp(
        MaterialBannerThemeData? a,
        MaterialBannerThemeData? b,
        double t)
    {
        if (ReferenceEquals(a, b) && a is not null)
        {
            return a;
        }

        MaterialBannerThemeData selected = t < 0.5
            ? a ?? new MaterialBannerThemeData()
            : b ?? new MaterialBannerThemeData();
        return selected with
        {
            BackgroundColor = MaterialThemeLerp.Color(a?.BackgroundColor, b?.BackgroundColor, t),
            SurfaceTintColor = MaterialThemeLerp.Color(a?.SurfaceTintColor, b?.SurfaceTintColor, t),
            ShadowColor = MaterialThemeLerp.Color(a?.ShadowColor, b?.ShadowColor, t),
            DividerColor = MaterialThemeLerp.Color(a?.DividerColor, b?.DividerColor, t),
            ContentTextStyle = MaterialThemeLerp.TextStyle(a?.ContentTextStyle, b?.ContentTextStyle, t),
            Elevation = MaterialThemeLerp.Double(a?.Elevation, b?.Elevation, t),
            Padding = EdgeInsetsGeometry.Lerp(a?.Padding, b?.Padding, t),
            LeadingPadding = EdgeInsetsGeometry.Lerp(a?.LeadingPadding, b?.LeadingPadding, t),
        };
    }
}

public sealed partial record PopupMenuThemeData
{
    public static PopupMenuThemeData? Lerp(PopupMenuThemeData? a, PopupMenuThemeData? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        PopupMenuThemeData selected = t < 0.5
            ? a ?? new PopupMenuThemeData()
            : b ?? new PopupMenuThemeData();
        return selected with
        {
            Color = MaterialThemeLerp.Color(a?.Color, b?.Color, t),
            Shape = MaterialThemeLerp.Shape(a?.Shape, b?.Shape, t),
            MenuPadding = MaterialThemeLerp.Thickness(a?.MenuPadding, b?.MenuPadding, t),
            Elevation = MaterialThemeLerp.Double(a?.Elevation, b?.Elevation, t),
            ShadowColor = MaterialThemeLerp.Color(a?.ShadowColor, b?.ShadowColor, t),
            SurfaceTintColor = MaterialThemeLerp.Color(a?.SurfaceTintColor, b?.SurfaceTintColor, t),
            TextStyle = MaterialThemeLerp.TextStyle(a?.TextStyle, b?.TextStyle, t),
            LabelTextStyle = MaterialThemeLerp.TextStyleStateProperty(
                a?.LabelTextStyle,
                b?.LabelTextStyle,
                t),
            EnableFeedback = t < 0.5 ? a?.EnableFeedback : b?.EnableFeedback,
            MouseCursor = t < 0.5 ? a?.MouseCursor : b?.MouseCursor,
            Position = t < 0.5 ? a?.Position : b?.Position,
            IconColor = MaterialThemeLerp.Color(a?.IconColor, b?.IconColor, t),
            IconSize = MaterialThemeLerp.Double(a?.IconSize, b?.IconSize, t),
        };
    }
}

public sealed partial record ScrollbarThemeData
{
    public static ScrollbarThemeData Lerp(ScrollbarThemeData a, ScrollbarThemeData b, double t)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        ScrollbarThemeData selected = t < 0.5 ? a : b;
        return selected with
        {
            ThumbVisibility = t < 0.5 ? a.ThumbVisibility : b.ThumbVisibility,
            Thickness = MaterialThemeLerp.DoubleStateProperty(a.Thickness, b.Thickness, t),
            TrackVisibility = t < 0.5 ? a.TrackVisibility : b.TrackVisibility,
            Interactive = t < 0.5 ? a.Interactive : b.Interactive,
            Radius = MaterialThemeLerp.Double(a.Radius, b.Radius, t),
            ThumbColor = MaterialThemeLerp.ColorStateProperty(a.ThumbColor, b.ThumbColor, t),
            TrackColor = MaterialThemeLerp.ColorStateProperty(a.TrackColor, b.TrackColor, t),
            TrackBorderColor = MaterialThemeLerp.ColorStateProperty(
                a.TrackBorderColor,
                b.TrackBorderColor,
                t),
            CrossAxisMargin = MaterialThemeLerp.Double(a.CrossAxisMargin, b.CrossAxisMargin, t),
            MainAxisMargin = MaterialThemeLerp.Double(a.MainAxisMargin, b.MainAxisMargin, t),
            MinThumbLength = MaterialThemeLerp.Double(a.MinThumbLength, b.MinThumbLength, t),
        };
    }
}

public sealed partial record SnackBarThemeData
{
    public static SnackBarThemeData Lerp(SnackBarThemeData a, SnackBarThemeData b, double t)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        SnackBarThemeData selected = t < 0.5 ? a : b;
        return selected with
        {
            BackgroundColor = MaterialThemeLerp.Color(a.BackgroundColor, b.BackgroundColor, t),
            ActionTextColor = MaterialThemeLerp.Color(a.ActionTextColor, b.ActionTextColor, t),
            DisabledActionTextColor = MaterialThemeLerp.Color(
                a.DisabledActionTextColor,
                b.DisabledActionTextColor,
                t),
            ContentTextStyle = MaterialThemeLerp.TextStyle(a.ContentTextStyle, b.ContentTextStyle, t),
            Elevation = MaterialThemeLerp.Double(a.Elevation, b.Elevation, t),
            Shape = MaterialThemeLerp.Shape(a.Shape, b.Shape, t),
            Behavior = t < 0.5 ? a.Behavior : b.Behavior,
            Width = MaterialThemeLerp.Double(a.Width, b.Width, t),
            InsetPadding = MaterialThemeLerp.Thickness(a.InsetPadding, b.InsetPadding, t),
            ShowCloseIcon = t < 0.5 ? a.ShowCloseIcon : b.ShowCloseIcon,
            CloseIconColor = MaterialThemeLerp.Color(a.CloseIconColor, b.CloseIconColor, t),
            ActionOverflowThreshold = MaterialThemeLerp.Double(
                a.ActionOverflowThreshold,
                b.ActionOverflowThreshold,
                t),
            ActionBackgroundColor = MaterialThemeLerp.Color(
                a.ActionBackgroundColor,
                b.ActionBackgroundColor,
                t),
            DisabledActionBackgroundColor = MaterialThemeLerp.Color(
                a.DisabledActionBackgroundColor,
                b.DisabledActionBackgroundColor,
                t),
            DismissDirection = t < 0.5 ? a.DismissDirection : b.DismissDirection,
        };
    }
}

public sealed partial record MenuStyle
{
    public static MenuStyle? Lerp(MenuStyle? a, MenuStyle? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new MenuStyle(
            BackgroundColor: MaterialThemeLerp.ColorStateProperty(
                a?.BackgroundColor,
                b?.BackgroundColor,
                t),
            ShadowColor: MaterialThemeLerp.ColorStateProperty(a?.ShadowColor, b?.ShadowColor, t),
            SurfaceTintColor: MaterialThemeLerp.ColorStateProperty(
                a?.SurfaceTintColor,
                b?.SurfaceTintColor,
                t),
            Elevation: MaterialThemeLerp.DoubleStateProperty(a?.Elevation, b?.Elevation, t),
            Padding: MaterialThemeLerp.ThicknessStateProperty(a?.Padding, b?.Padding, t),
            MinimumSize: MaterialThemeLerp.SizeStateProperty(a?.MinimumSize, b?.MinimumSize, t),
            FixedSize: MaterialThemeLerp.SizeStateProperty(a?.FixedSize, b?.FixedSize, t),
            MaximumSize: MaterialThemeLerp.SizeStateProperty(a?.MaximumSize, b?.MaximumSize, t),
            Side: MaterialThemeLerp.BorderSideStateProperty(a?.Side, b?.Side, t),
            Shape: MaterialThemeLerp.ShapeStateProperty(a?.Shape, b?.Shape, t),
            MouseCursor: t < 0.5 ? a?.MouseCursor : b?.MouseCursor,
            Alignment: MaterialThemeLerp.Alignment(a?.Alignment, b?.Alignment, t),
            VisualDensity: t < 0.5 ? a?.VisualDensity : b?.VisualDensity);
    }
}

public sealed partial record DropdownMenuThemeData
{
    public static DropdownMenuThemeData Lerp(
        DropdownMenuThemeData? a,
        DropdownMenuThemeData? b,
        double t)
    {
        if (ReferenceEquals(a, b) && a is not null)
        {
            return a;
        }

        return new DropdownMenuThemeData(
            TextStyle: MaterialThemeLerp.TextStyle(a?.TextStyle, b?.TextStyle, t),
            InputDecorationTheme: t < 0.5 ? a?.InputDecorationTheme : b?.InputDecorationTheme,
            MenuStyle: MenuStyle.Lerp(a?.MenuStyle, b?.MenuStyle, t),
            DisabledColor: MaterialThemeLerp.Color(a?.DisabledColor, b?.DisabledColor, t));
    }
}

public sealed partial record MenuThemeData
{
    public static MenuThemeData? Lerp(MenuThemeData? a, MenuThemeData? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new MenuThemeData(
            Style: MenuStyle.Lerp(a?.Style, b?.Style, t),
            SubmenuIcon: t < 0.5 ? a?.SubmenuIcon : b?.SubmenuIcon);
    }
}

public sealed partial record MenuBarThemeData
{
    public static MenuBarThemeData? Lerp(MenuBarThemeData? a, MenuBarThemeData? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new MenuBarThemeData(MenuStyle.Lerp(a?.Style, b?.Style, t));
    }
}

public sealed partial record MenuButtonThemeData
{
    public static MenuButtonThemeData? Lerp(MenuButtonThemeData? a, MenuButtonThemeData? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new MenuButtonThemeData(ButtonStyle.Lerp(a?.Style, b?.Style, t));
    }
}

public sealed partial record ExpansionTileThemeData
{
    public static ExpansionTileThemeData? Lerp(
        ExpansionTileThemeData? a,
        ExpansionTileThemeData? b,
        double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new ExpansionTileThemeData(
            BackgroundColor: MaterialThemeLerp.Color(a?.BackgroundColor, b?.BackgroundColor, t),
            CollapsedBackgroundColor: MaterialThemeLerp.Color(
                a?.CollapsedBackgroundColor,
                b?.CollapsedBackgroundColor,
                t),
            TilePadding: MaterialThemeLerp.Thickness(a?.TilePadding, b?.TilePadding, t),
            ExpandedAlignment: MaterialThemeLerp.Alignment(
                a?.ExpandedAlignment,
                b?.ExpandedAlignment,
                t),
            ExpandedCrossAxisAlignment: t < 0.5
                ? a?.ExpandedCrossAxisAlignment
                : b?.ExpandedCrossAxisAlignment,
            ChildrenPadding: MaterialThemeLerp.Thickness(a?.ChildrenPadding, b?.ChildrenPadding, t),
            IconColor: MaterialThemeLerp.Color(a?.IconColor, b?.IconColor, t),
            CollapsedIconColor: MaterialThemeLerp.Color(
                a?.CollapsedIconColor,
                b?.CollapsedIconColor,
                t),
            TextColor: MaterialThemeLerp.Color(a?.TextColor, b?.TextColor, t),
            CollapsedTextColor: MaterialThemeLerp.Color(
                a?.CollapsedTextColor,
                b?.CollapsedTextColor,
                t),
            Shape: MaterialThemeLerp.BorderRadius(a?.Shape, b?.Shape, t),
            CollapsedShape: MaterialThemeLerp.BorderRadius(a?.CollapsedShape, b?.CollapsedShape, t),
            ClipBehavior: t < 0.5 ? a?.ClipBehavior : b?.ClipBehavior,
            ControlAffinity: t < 0.5 ? a?.ControlAffinity : b?.ControlAffinity,
            Dense: t < 0.5 ? a?.Dense : b?.Dense,
            MinTileHeight: MaterialThemeLerp.Double(a?.MinTileHeight, b?.MinTileHeight, t),
            EnableFeedback: t < 0.5 ? a?.EnableFeedback : b?.EnableFeedback,
            ExpansionAnimationStyle: t < 0.5
                ? a?.ExpansionAnimationStyle
                : b?.ExpansionAnimationStyle);
    }
}

public sealed partial record ListTileThemeData
{
    public static ListTileThemeData? Lerp(ListTileThemeData? a, ListTileThemeData? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new ListTileThemeData(
            Dense: t < 0.5 ? a?.Dense : b?.Dense,
            Shape: MaterialThemeLerp.BorderRadius(a?.Shape, b?.Shape, t),
            Style: t < 0.5 ? a?.Style : b?.Style,
            SelectedColor: MaterialThemeLerp.Color(a?.SelectedColor, b?.SelectedColor, t),
            IconColor: MaterialThemeLerp.Color(a?.IconColor, b?.IconColor, t),
            TextColor: MaterialThemeLerp.Color(a?.TextColor, b?.TextColor, t),
            TitleTextStyle: MaterialThemeLerp.TextStyle(a?.TitleTextStyle, b?.TitleTextStyle, t),
            SubtitleTextStyle: MaterialThemeLerp.TextStyle(
                a?.SubtitleTextStyle,
                b?.SubtitleTextStyle,
                t),
            LeadingAndTrailingTextStyle: MaterialThemeLerp.TextStyle(
                a?.LeadingAndTrailingTextStyle,
                b?.LeadingAndTrailingTextStyle,
                t),
            ContentPadding: MaterialThemeLerp.Thickness(a?.ContentPadding, b?.ContentPadding, t),
            TileColor: MaterialThemeLerp.Color(a?.TileColor, b?.TileColor, t),
            SelectedTileColor: MaterialThemeLerp.Color(a?.SelectedTileColor, b?.SelectedTileColor, t),
            HorizontalTitleGap: MaterialThemeLerp.Double(
                a?.HorizontalTitleGap,
                b?.HorizontalTitleGap,
                t),
            MinVerticalPadding: MaterialThemeLerp.Double(
                a?.MinVerticalPadding,
                b?.MinVerticalPadding,
                t),
            MinLeadingWidth: MaterialThemeLerp.Double(a?.MinLeadingWidth, b?.MinLeadingWidth, t),
            MinTileHeight: MaterialThemeLerp.Double(a?.MinTileHeight, b?.MinTileHeight, t),
            EnableFeedback: t < 0.5 ? a?.EnableFeedback : b?.EnableFeedback,
            MouseCursor: t < 0.5 ? a?.MouseCursor : b?.MouseCursor,
            IsThreeLine: t < 0.5 ? a?.IsThreeLine : b?.IsThreeLine,
            ControlAffinity: t < 0.5 ? a?.ControlAffinity : b?.ControlAffinity,
            VisualDensity: t < 0.5 ? a?.VisualDensity : b?.VisualDensity,
            TitleAlignment: t < 0.5 ? a?.TitleAlignment : b?.TitleAlignment);
    }
}

public sealed partial record SearchBarThemeData
{
    public static SearchBarThemeData? Lerp(SearchBarThemeData? a, SearchBarThemeData? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new SearchBarThemeData(
            Elevation: MaterialThemeLerp.DoubleStateProperty(a?.Elevation, b?.Elevation, t),
            BackgroundColor: MaterialThemeLerp.ColorStateProperty(
                a?.BackgroundColor,
                b?.BackgroundColor,
                t),
            ShadowColor: MaterialThemeLerp.ColorStateProperty(a?.ShadowColor, b?.ShadowColor, t),
            SurfaceTintColor: MaterialThemeLerp.ColorStateProperty(
                a?.SurfaceTintColor,
                b?.SurfaceTintColor,
                t),
            OverlayColor: MaterialThemeLerp.ColorStateProperty(a?.OverlayColor, b?.OverlayColor, t),
            Side: MaterialThemeLerp.BorderSideStateProperty(a?.Side, b?.Side, t),
            Shape: MaterialThemeLerp.ShapeStateProperty(a?.Shape, b?.Shape, t),
            Padding: MaterialThemeLerp.ThicknessStateProperty(a?.Padding, b?.Padding, t),
            TextStyle: MaterialThemeLerp.TextStyleStateProperty(a?.TextStyle, b?.TextStyle, t),
            HintStyle: MaterialThemeLerp.TextStyleStateProperty(a?.HintStyle, b?.HintStyle, t),
            Constraints: MaterialThemeLerp.BoxConstraints(a?.Constraints, b?.Constraints, t),
            TextCapitalization: t < 0.5 ? a?.TextCapitalization : b?.TextCapitalization);
    }
}

public sealed partial record SearchViewThemeData
{
    public static SearchViewThemeData? Lerp(SearchViewThemeData? a, SearchViewThemeData? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new SearchViewThemeData(
            BackgroundColor: MaterialThemeLerp.Color(a?.BackgroundColor, b?.BackgroundColor, t),
            Elevation: MaterialThemeLerp.Double(a?.Elevation, b?.Elevation, t),
            SurfaceTintColor: MaterialThemeLerp.Color(a?.SurfaceTintColor, b?.SurfaceTintColor, t),
            Side: MaterialThemeLerp.BorderSide(a?.Side, b?.Side, t),
            Shape: MaterialThemeLerp.Shape(a?.Shape, b?.Shape, t),
            HeaderHeight: MaterialThemeLerp.Double(a?.HeaderHeight, b?.HeaderHeight, t),
            HeaderTextStyle: MaterialThemeLerp.TextStyle(
                a?.HeaderTextStyle,
                b?.HeaderTextStyle,
                t),
            HeaderHintStyle: MaterialThemeLerp.TextStyle(
                a?.HeaderHintStyle,
                b?.HeaderHintStyle,
                t),
            Constraints: MaterialThemeLerp.BoxConstraints(a?.Constraints, b?.Constraints, t),
            Padding: MaterialThemeLerp.Thickness(a?.Padding, b?.Padding, t),
            BarPadding: MaterialThemeLerp.Thickness(a?.BarPadding, b?.BarPadding, t),
            ShrinkWrap: t < 0.5 ? a?.ShrinkWrap : b?.ShrinkWrap,
            DividerColor: MaterialThemeLerp.Color(a?.DividerColor, b?.DividerColor, t));
    }
}

public sealed partial record SegmentedButtonThemeData
{
    public static SegmentedButtonThemeData? Lerp(
        SegmentedButtonThemeData? a,
        SegmentedButtonThemeData? b,
        double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new SegmentedButtonThemeData(
            Style: ButtonStyle.Lerp(a?.Style, b?.Style, t),
            SelectedIcon: t < 0.5 ? a?.SelectedIcon : b?.SelectedIcon);
    }
}

public sealed partial record ProgressIndicatorThemeData
{
    public static ProgressIndicatorThemeData? Lerp(
        ProgressIndicatorThemeData? a,
        ProgressIndicatorThemeData? b,
        double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new ProgressIndicatorThemeData(
            Color: MaterialThemeLerp.Color(a?.Color, b?.Color, t),
            LinearTrackColor: MaterialThemeLerp.Color(a?.LinearTrackColor, b?.LinearTrackColor, t),
            LinearMinHeight: MaterialThemeLerp.Double(a?.LinearMinHeight, b?.LinearMinHeight, t),
            CircularTrackColor: MaterialThemeLerp.Color(
                a?.CircularTrackColor,
                b?.CircularTrackColor,
                t),
            RefreshBackgroundColor: MaterialThemeLerp.Color(
                a?.RefreshBackgroundColor,
                b?.RefreshBackgroundColor,
                t),
            BorderRadius: BorderRadiusGeometry.Lerp(a?.BorderRadius, b?.BorderRadius, t),
            StopIndicatorColor: MaterialThemeLerp.Color(
                a?.StopIndicatorColor,
                b?.StopIndicatorColor,
                t),
            StopIndicatorRadius: MaterialThemeLerp.Double(
                a?.StopIndicatorRadius,
                b?.StopIndicatorRadius,
                t),
            StrokeWidth: MaterialThemeLerp.Double(a?.StrokeWidth, b?.StrokeWidth, t),
            StrokeAlign: MaterialThemeLerp.Double(a?.StrokeAlign, b?.StrokeAlign, t),
            StrokeCap: t < 0.5 ? a?.StrokeCap : b?.StrokeCap,
            Constraints: MaterialThemeLerp.BoxConstraints(a?.Constraints, b?.Constraints, t),
            TrackGap: MaterialThemeLerp.Double(a?.TrackGap, b?.TrackGap, t),
            CircularTrackPadding: EdgeInsetsGeometry.Lerp(
                a?.CircularTrackPadding,
                b?.CircularTrackPadding,
                t),
            Year2023: t < 0.5 ? a?.Year2023 : b?.Year2023,
            Controller: t < 0.5 ? a?.Controller : b?.Controller);
    }
}

public sealed partial record RadioThemeData
{
    public static RadioThemeData Lerp(RadioThemeData? a, RadioThemeData? b, double t)
    {
        if (ReferenceEquals(a, b) && a is not null)
        {
            return a;
        }

        return new RadioThemeData(
            FillColor: MaterialThemeLerp.ColorStateProperty(a?.FillColor, b?.FillColor, t),
            OverlayColor: MaterialThemeLerp.ColorStateProperty(a?.OverlayColor, b?.OverlayColor, t),
            MaterialTapTargetSize: t < 0.5 ? a?.MaterialTapTargetSize : b?.MaterialTapTargetSize,
            SplashRadius: MaterialThemeLerp.Double(a?.SplashRadius, b?.SplashRadius, t),
            BackgroundColor: MaterialThemeLerp.ColorStateProperty(
                a?.BackgroundColor,
                b?.BackgroundColor,
                t),
            Side: MaterialThemeLerp.BorderSide(a?.Side, b?.Side, t),
            InnerRadius: MaterialThemeLerp.DoubleStateProperty(a?.InnerRadius, b?.InnerRadius, t));
    }
}

public sealed partial record SwitchThemeData
{
    public static SwitchThemeData Lerp(SwitchThemeData? a, SwitchThemeData? b, double t)
    {
        if (ReferenceEquals(a, b) && a is not null)
        {
            return a;
        }

        return new SwitchThemeData(
            ThumbColor: MaterialThemeLerp.ColorStateProperty(a?.ThumbColor, b?.ThumbColor, t),
            TrackColor: MaterialThemeLerp.ColorStateProperty(a?.TrackColor, b?.TrackColor, t),
            TrackOutlineColor: MaterialThemeLerp.ColorStateProperty(
                a?.TrackOutlineColor,
                b?.TrackOutlineColor,
                t),
            TrackOutlineWidth: MaterialThemeLerp.DoubleStateProperty(
                a?.TrackOutlineWidth,
                b?.TrackOutlineWidth,
                t),
            MaterialTapTargetSize: t < 0.5 ? a?.MaterialTapTargetSize : b?.MaterialTapTargetSize,
            MouseCursor: t < 0.5 ? a?.MouseCursor : b?.MouseCursor,
            OverlayColor: MaterialThemeLerp.ColorStateProperty(a?.OverlayColor, b?.OverlayColor, t),
            SplashRadius: MaterialThemeLerp.Double(a?.SplashRadius, b?.SplashRadius, t),
            ThumbIcon: t < 0.5 ? a?.ThumbIcon : b?.ThumbIcon,
            Padding: MaterialThemeLerp.Thickness(a?.Padding, b?.Padding, t));
    }
}

public sealed partial record TabBarThemeData
{
    public static TabBarThemeData Lerp(TabBarThemeData a, TabBarThemeData b, double t)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new TabBarThemeData(
            Indicator: MaterialThemeLerp.Decoration(a.Indicator, b.Indicator, t),
            IndicatorColor: MaterialThemeLerp.Color(a.IndicatorColor, b.IndicatorColor, t),
            IndicatorSize: t < 0.5 ? a.IndicatorSize : b.IndicatorSize,
            DividerColor: MaterialThemeLerp.Color(a.DividerColor, b.DividerColor, t),
            DividerHeight: t < 0.5 ? a.DividerHeight : b.DividerHeight,
            LabelColor: MaterialThemeLerp.Color(a.LabelColor, b.LabelColor, t),
            LabelPadding: MaterialThemeLerp.Thickness(a.LabelPadding, b.LabelPadding, t),
            LabelStyle: MaterialThemeLerp.TextStyle(a.LabelStyle, b.LabelStyle, t),
            UnselectedLabelColor: MaterialThemeLerp.Color(
                a.UnselectedLabelColor,
                b.UnselectedLabelColor,
                t),
            UnselectedLabelStyle: MaterialThemeLerp.TextStyle(
                a.UnselectedLabelStyle,
                b.UnselectedLabelStyle,
                t),
            OverlayColor: MaterialThemeLerp.ColorStateProperty(a.OverlayColor, b.OverlayColor, t),
            MouseCursor: t < 0.5 ? a.MouseCursor : b.MouseCursor,
            TabAlignment: t < 0.5 ? a.TabAlignment : b.TabAlignment,
            IndicatorAnimation: t < 0.5 ? a.IndicatorAnimation : b.IndicatorAnimation,
            SplashBorderRadius: MaterialThemeLerp.BorderRadius(
                a.SplashBorderRadius,
                a.SplashBorderRadius,
                t));
    }
}

public sealed partial record SliderThemeData
{
    public static SliderThemeData Lerp(SliderThemeData a, SliderThemeData b, double t)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new SliderThemeData(
            ActiveTrackColor: MaterialThemeLerp.Color(a.ActiveTrackColor, b.ActiveTrackColor, t),
            InactiveTrackColor: MaterialThemeLerp.Color(a.InactiveTrackColor, b.InactiveTrackColor, t),
            SecondaryActiveTrackColor: MaterialThemeLerp.Color(
                a.SecondaryActiveTrackColor,
                b.SecondaryActiveTrackColor,
                t),
            DisabledActiveTrackColor: MaterialThemeLerp.Color(
                a.DisabledActiveTrackColor,
                b.DisabledActiveTrackColor,
                t),
            DisabledInactiveTrackColor: MaterialThemeLerp.Color(
                a.DisabledInactiveTrackColor,
                b.DisabledInactiveTrackColor,
                t),
            DisabledSecondaryActiveTrackColor: MaterialThemeLerp.Color(
                a.DisabledSecondaryActiveTrackColor,
                b.DisabledSecondaryActiveTrackColor,
                t),
            ThumbColor: MaterialThemeLerp.Color(a.ThumbColor, b.ThumbColor, t),
            DisabledThumbColor: MaterialThemeLerp.Color(a.DisabledThumbColor, b.DisabledThumbColor, t),
            OverlayColor: MaterialThemeLerp.ColorStateProperty(a.OverlayColor, b.OverlayColor, t),
            TrackHeight: MaterialThemeLerp.Double(a.TrackHeight, b.TrackHeight, t),
            ThumbRadius: MaterialThemeLerp.Double(a.ThumbRadius, b.ThumbRadius, t),
            MaterialTapTargetSize: t < 0.5 ? a.MaterialTapTargetSize : b.MaterialTapTargetSize,
            ActiveTickMarkColor: MaterialThemeLerp.Color(
                a.ActiveTickMarkColor,
                b.ActiveTickMarkColor,
                t),
            InactiveTickMarkColor: MaterialThemeLerp.Color(
                a.InactiveTickMarkColor,
                b.InactiveTickMarkColor,
                t),
            DisabledActiveTickMarkColor: MaterialThemeLerp.Color(
                a.DisabledActiveTickMarkColor,
                b.DisabledActiveTickMarkColor,
                t),
            DisabledInactiveTickMarkColor: MaterialThemeLerp.Color(
                a.DisabledInactiveTickMarkColor,
                b.DisabledInactiveTickMarkColor,
                t),
            OverlappingShapeStrokeColor: MaterialThemeLerp.Color(
                a.OverlappingShapeStrokeColor,
                b.OverlappingShapeStrokeColor,
                t),
            ValueIndicatorColor: MaterialThemeLerp.Color(
                a.ValueIndicatorColor,
                b.ValueIndicatorColor,
                t),
            ValueIndicatorStrokeColor: MaterialThemeLerp.Color(
                a.ValueIndicatorStrokeColor,
                b.ValueIndicatorStrokeColor,
                t),
            OverlayRadius: MaterialThemeLerp.Double(a.OverlayRadius, b.OverlayRadius, t),
            TickMarkRadius: MaterialThemeLerp.Double(a.TickMarkRadius, b.TickMarkRadius, t),
            ShowValueIndicator: t < 0.5 ? a.ShowValueIndicator : b.ShowValueIndicator,
            ValueIndicatorTextStyle: MaterialThemeLerp.TextStyle(
                a.ValueIndicatorTextStyle,
                b.ValueIndicatorTextStyle,
                t),
            MinThumbSeparation: MaterialThemeLerp.Double(
                a.MinThumbSeparation,
                b.MinThumbSeparation,
                t),
            MouseCursor: t < 0.5 ? a.MouseCursor : b.MouseCursor,
            AllowedInteraction: t < 0.5 ? a.AllowedInteraction : b.AllowedInteraction,
            Padding: MaterialThemeLerp.Thickness(a.Padding, b.Padding, t),
            ThumbSize: MaterialThemeLerp.SizeStateProperty(a.ThumbSize, b.ThumbSize, t),
            TrackGap: MaterialThemeLerp.Double(a.TrackGap, b.TrackGap, t),
            Year2023: t < 0.5 ? a.Year2023 : b.Year2023);
    }
}

public sealed partial record DatePickerThemeData
{
    public static DatePickerThemeData Lerp(DatePickerThemeData? a, DatePickerThemeData? b, double t)
    {
        if (ReferenceEquals(a, b) && a is not null)
        {
            return a;
        }

        return new DatePickerThemeData(
            BackgroundColor: MaterialThemeLerp.Color(a?.BackgroundColor, b?.BackgroundColor, t),
            Elevation: MaterialThemeLerp.Double(a?.Elevation, b?.Elevation, t),
            ShadowColor: MaterialThemeLerp.Color(a?.ShadowColor, b?.ShadowColor, t),
            SurfaceTintColor: MaterialThemeLerp.Color(a?.SurfaceTintColor, b?.SurfaceTintColor, t),
            Shape: MaterialThemeLerp.Shape(a?.Shape, b?.Shape, t),
            HeaderBackgroundColor: MaterialThemeLerp.Color(
                a?.HeaderBackgroundColor,
                b?.HeaderBackgroundColor,
                t),
            HeaderForegroundColor: MaterialThemeLerp.Color(
                a?.HeaderForegroundColor,
                b?.HeaderForegroundColor,
                t),
            HeaderHeadlineStyle: MaterialThemeLerp.TextStyle(
                a?.HeaderHeadlineStyle,
                b?.HeaderHeadlineStyle,
                t),
            HeaderHelpStyle: MaterialThemeLerp.TextStyle(
                a?.HeaderHelpStyle,
                b?.HeaderHelpStyle,
                t),
            WeekdayStyle: MaterialThemeLerp.TextStyle(a?.WeekdayStyle, b?.WeekdayStyle, t),
            DayStyle: MaterialThemeLerp.TextStyle(a?.DayStyle, b?.DayStyle, t),
            DayForegroundColor: MaterialThemeLerp.ColorStateProperty(
                a?.DayForegroundColor,
                b?.DayForegroundColor,
                t),
            DayBackgroundColor: MaterialThemeLerp.ColorStateProperty(
                a?.DayBackgroundColor,
                b?.DayBackgroundColor,
                t),
            DayOverlayColor: MaterialThemeLerp.ColorStateProperty(
                a?.DayOverlayColor,
                b?.DayOverlayColor,
                t),
            DayShape: MaterialThemeLerp.ShapeStateProperty(a?.DayShape, b?.DayShape, t),
            TodayForegroundColor: MaterialThemeLerp.ColorStateProperty(
                a?.TodayForegroundColor,
                b?.TodayForegroundColor,
                t),
            TodayBackgroundColor: MaterialThemeLerp.ColorStateProperty(
                a?.TodayBackgroundColor,
                b?.TodayBackgroundColor,
                t),
            TodayBorder: MaterialThemeLerp.BorderSide(a?.TodayBorder, b?.TodayBorder, t),
            YearStyle: MaterialThemeLerp.TextStyle(a?.YearStyle, b?.YearStyle, t),
            YearForegroundColor: MaterialThemeLerp.ColorStateProperty(
                a?.YearForegroundColor,
                b?.YearForegroundColor,
                t),
            YearBackgroundColor: MaterialThemeLerp.ColorStateProperty(
                a?.YearBackgroundColor,
                b?.YearBackgroundColor,
                t),
            YearOverlayColor: MaterialThemeLerp.ColorStateProperty(
                a?.YearOverlayColor,
                b?.YearOverlayColor,
                t),
            YearShape: MaterialThemeLerp.ShapeStateProperty(a?.YearShape, b?.YearShape, t),
            DividerColor: MaterialThemeLerp.Color(a?.DividerColor, b?.DividerColor, t),
            InputDecorationTheme: t < 0.5 ? a?.InputDecorationTheme : b?.InputDecorationTheme,
            CancelButtonStyle: ButtonStyle.Lerp(a?.CancelButtonStyle, b?.CancelButtonStyle, t),
            ConfirmButtonStyle: ButtonStyle.Lerp(a?.ConfirmButtonStyle, b?.ConfirmButtonStyle, t),
            ToggleButtonTextStyle: MaterialThemeLerp.TextStyle(
                a?.ToggleButtonTextStyle,
                b?.ToggleButtonTextStyle,
                t),
            SubHeaderForegroundColor: MaterialThemeLerp.Color(
                a?.SubHeaderForegroundColor,
                b?.SubHeaderForegroundColor,
                t),
            RangePickerBackgroundColor: MaterialThemeLerp.Color(
                a?.RangePickerBackgroundColor,
                b?.RangePickerBackgroundColor,
                t),
            RangePickerElevation: MaterialThemeLerp.Double(
                a?.RangePickerElevation,
                b?.RangePickerElevation,
                t),
            RangePickerShadowColor: MaterialThemeLerp.Color(
                a?.RangePickerShadowColor,
                b?.RangePickerShadowColor,
                t),
            RangePickerSurfaceTintColor: MaterialThemeLerp.Color(
                a?.RangePickerSurfaceTintColor,
                b?.RangePickerSurfaceTintColor,
                t),
            RangePickerShape: MaterialThemeLerp.Shape(a?.RangePickerShape, b?.RangePickerShape, t),
            RangePickerHeaderBackgroundColor: MaterialThemeLerp.Color(
                a?.RangePickerHeaderBackgroundColor,
                b?.RangePickerHeaderBackgroundColor,
                t),
            RangePickerHeaderForegroundColor: MaterialThemeLerp.Color(
                a?.RangePickerHeaderForegroundColor,
                b?.RangePickerHeaderForegroundColor,
                t),
            RangePickerHeaderHeadlineStyle: MaterialThemeLerp.TextStyle(
                a?.RangePickerHeaderHeadlineStyle,
                b?.RangePickerHeaderHeadlineStyle,
                t),
            RangePickerHeaderHelpStyle: MaterialThemeLerp.TextStyle(
                a?.RangePickerHeaderHelpStyle,
                b?.RangePickerHeaderHelpStyle,
                t),
            RangeSelectionBackgroundColor: MaterialThemeLerp.Color(
                a?.RangeSelectionBackgroundColor,
                b?.RangeSelectionBackgroundColor,
                t),
            RangeSelectionOverlayColor: MaterialThemeLerp.ColorStateProperty(
                a?.RangeSelectionOverlayColor,
                b?.RangeSelectionOverlayColor,
                t));
    }
}

public sealed partial record TimePickerThemeData
{
    public static TimePickerThemeData Lerp(TimePickerThemeData? a, TimePickerThemeData? b, double t)
    {
        if (ReferenceEquals(a, b) && a is not null)
        {
            return a;
        }

        return new TimePickerThemeData(
            BackgroundColor: MaterialThemeLerp.Color(a?.BackgroundColor, b?.BackgroundColor, t),
            CancelButtonStyle: ButtonStyle.Lerp(a?.CancelButtonStyle, b?.CancelButtonStyle, t),
            ConfirmButtonStyle: ButtonStyle.Lerp(a?.ConfirmButtonStyle, b?.ConfirmButtonStyle, t),
            DayPeriodBorderSide: MaterialThemeLerp.BorderSide(
                a?.DayPeriodBorderSide,
                b?.DayPeriodBorderSide,
                t),
            DayPeriodColor: MaterialThemeLerp.ColorStateProperty(
                a?.DayPeriodColor,
                b?.DayPeriodColor,
                t),
            DayPeriodShape: MaterialThemeLerp.Shape(a?.DayPeriodShape, b?.DayPeriodShape, t),
            DayPeriodTextColor: MaterialThemeLerp.ColorStateProperty(
                a?.DayPeriodTextColor,
                b?.DayPeriodTextColor,
                t),
            DayPeriodTextStyle: MaterialThemeLerp.TextStyle(
                a?.DayPeriodTextStyle,
                b?.DayPeriodTextStyle,
                t),
            DialBackgroundColor: MaterialThemeLerp.Color(
                a?.DialBackgroundColor,
                b?.DialBackgroundColor,
                t),
            DialHandColor: MaterialThemeLerp.Color(a?.DialHandColor, b?.DialHandColor, t),
            DialTextColor: MaterialThemeLerp.ColorStateProperty(
                a?.DialTextColor,
                b?.DialTextColor,
                t),
            DialTextStyle: MaterialThemeLerp.TextStyle(a?.DialTextStyle, b?.DialTextStyle, t),
            Elevation: MaterialThemeLerp.Double(a?.Elevation, b?.Elevation, t),
            EntryModeIconColor: MaterialThemeLerp.Color(
                a?.EntryModeIconColor,
                b?.EntryModeIconColor,
                t),
            HelpTextStyle: MaterialThemeLerp.TextStyle(a?.HelpTextStyle, b?.HelpTextStyle, t),
            HourMinuteColor: MaterialThemeLerp.ColorStateProperty(
                a?.HourMinuteColor,
                b?.HourMinuteColor,
                t),
            HourMinuteShape: MaterialThemeLerp.Shape(a?.HourMinuteShape, b?.HourMinuteShape, t),
            HourMinuteTextColor: MaterialThemeLerp.ColorStateProperty(
                a?.HourMinuteTextColor,
                b?.HourMinuteTextColor,
                t),
            HourMinuteTextStyle: MaterialThemeLerp.TextStyle(
                a?.HourMinuteTextStyle,
                b?.HourMinuteTextStyle,
                t),
            InputDecorationTheme: t < 0.5 ? a?.InputDecorationTheme : b?.InputDecorationTheme,
            Padding: MaterialThemeLerp.Thickness(a?.Padding, b?.Padding, t),
            Shape: MaterialThemeLerp.Shape(a?.Shape, b?.Shape, t),
            TimeSelectorSeparatorColor: MaterialThemeLerp.ColorStateProperty(
                a?.TimeSelectorSeparatorColor,
                b?.TimeSelectorSeparatorColor,
                t),
            TimeSelectorSeparatorTextStyle: MaterialThemeLerp.TextStyleStateProperty(
                a?.TimeSelectorSeparatorTextStyle,
                b?.TimeSelectorSeparatorTextStyle,
                t));
    }
}
