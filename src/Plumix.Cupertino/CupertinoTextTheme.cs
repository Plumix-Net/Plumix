using Avalonia.Media;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/text_theme.dart

/// <summary>Cupertino typography: the text styles every Cupertino control resolves through.</summary>
public class CupertinoTextThemeData
{
    internal static readonly TextStyle DefaultTextStyle = new(
        Inherit: false,
        FontFamily: new FontFamily("CupertinoSystemText"),
        FontSize: 17.0,
        LetterSpacing: -0.41,
        Color: CupertinoColors.Label,
        Decoration: Plumix.UI.TextDecoration.None);

    internal static readonly TextStyle DefaultActionTextStyle = new(
        Inherit: false,
        FontFamily: new FontFamily("CupertinoSystemText"),
        FontSize: 17.0,
        LetterSpacing: -0.41,
        Color: CupertinoColors.ActiveBlue,
        Decoration: Plumix.UI.TextDecoration.None);

    internal static readonly TextStyle DefaultActionSmallTextStyle = new(
        Inherit: false,
        FontFamily: new FontFamily("CupertinoSystemText"),
        FontSize: 15.0,
        LetterSpacing: -0.23,
        Color: CupertinoColors.ActiveBlue,
        Decoration: Plumix.UI.TextDecoration.None);

    internal static readonly TextStyle DefaultTabLabelTextStyle = new(
        Inherit: false,
        FontFamily: new FontFamily("CupertinoSystemText"),
        FontSize: 10.0,
        FontWeight: FontWeight.Medium,
        LetterSpacing: -0.24,
        Color: CupertinoColors.InactiveGray);

    internal static readonly TextStyle DefaultMiddleTitleTextStyle = new(
        Inherit: false,
        FontFamily: new FontFamily("CupertinoSystemText"),
        FontSize: 17.0,
        FontWeight: FontWeight.SemiBold,
        LetterSpacing: -0.41,
        Color: CupertinoColors.Label);

    internal static readonly TextStyle DefaultLargeTitleTextStyle = new(
        Inherit: false,
        FontFamily: new FontFamily("CupertinoSystemDisplay"),
        FontSize: 34.0,
        FontWeight: FontWeight.Bold,
        LetterSpacing: 0.38,
        Color: CupertinoColors.Label);

    internal static readonly TextStyle DefaultPickerTextStyle = new(
        Inherit: false,
        FontFamily: new FontFamily("CupertinoSystemDisplay"),
        FontSize: 21.0,
        FontWeight: FontWeight.Regular,
        LetterSpacing: -0.6,
        Color: CupertinoColors.Label);

    internal static readonly TextStyle DefaultDateTimePickerTextStyle = new(
        Inherit: false,
        FontFamily: new FontFamily("CupertinoSystemDisplay"),
        FontSize: 21.0,
        FontWeight: FontWeight.Normal,
        LetterSpacing: 0.4,
        Color: CupertinoColors.Label);

    private readonly TextThemeDefaultsBuilder _defaults;
    private readonly Color? _primaryColor;
    private readonly TextStyle? _textStyle;
    private readonly TextStyle? _actionTextStyle;
    private readonly TextStyle? _actionSmallTextStyle;
    private readonly TextStyle? _tabLabelTextStyle;
    private readonly TextStyle? _navTitleTextStyle;
    private readonly TextStyle? _navLargeTitleTextStyle;
    private readonly TextStyle? _navActionTextStyle;
    private readonly TextStyle? _pickerTextStyle;
    private readonly TextStyle? _dateTimePickerTextStyle;

    public CupertinoTextThemeData(
        Color? primaryColor = null,
        TextStyle? textStyle = null,
        TextStyle? actionTextStyle = null,
        TextStyle? actionSmallTextStyle = null,
        TextStyle? tabLabelTextStyle = null,
        TextStyle? navTitleTextStyle = null,
        TextStyle? navLargeTitleTextStyle = null,
        TextStyle? navActionTextStyle = null,
        TextStyle? pickerTextStyle = null,
        TextStyle? dateTimePickerTextStyle = null)
        : this(
            new TextThemeDefaultsBuilder(CupertinoColors.Label, CupertinoColors.InactiveGray),
            primaryColor ?? CupertinoColors.SystemBlue,
            textStyle,
            actionTextStyle,
            actionSmallTextStyle,
            tabLabelTextStyle,
            navTitleTextStyle,
            navLargeTitleTextStyle,
            navActionTextStyle,
            pickerTextStyle,
            dateTimePickerTextStyle)
    {
    }

    private protected CupertinoTextThemeData(
        TextThemeDefaultsBuilder defaults,
        Color? primaryColor,
        TextStyle? textStyle,
        TextStyle? actionTextStyle,
        TextStyle? actionSmallTextStyle,
        TextStyle? tabLabelTextStyle,
        TextStyle? navTitleTextStyle,
        TextStyle? navLargeTitleTextStyle,
        TextStyle? navActionTextStyle,
        TextStyle? pickerTextStyle,
        TextStyle? dateTimePickerTextStyle)
    {
        _defaults = defaults;
        _primaryColor = primaryColor;
        _textStyle = textStyle;
        _actionTextStyle = actionTextStyle;
        _actionSmallTextStyle = actionSmallTextStyle;
        _tabLabelTextStyle = tabLabelTextStyle;
        _navTitleTextStyle = navTitleTextStyle;
        _navLargeTitleTextStyle = navLargeTitleTextStyle;
        _navActionTextStyle = navActionTextStyle;
        _pickerTextStyle = pickerTextStyle;
        _dateTimePickerTextStyle = dateTimePickerTextStyle;
    }

    /// <summary>The style for body text.</summary>
    public virtual TextStyle TextStyle => _textStyle ?? _defaults.TextStyle;

    /// <summary>The style for interactive text, e.g. a dialog action.</summary>
    public virtual TextStyle ActionTextStyle =>
        _actionTextStyle ?? _defaults.ActionTextStyle(_primaryColor);

    /// <summary>The style for the smaller interactive text used by compact controls.</summary>
    public virtual TextStyle ActionSmallTextStyle =>
        _actionSmallTextStyle ?? _defaults.ActionSmallTextStyle(_primaryColor);

    /// <summary>The style for tab labels.</summary>
    public virtual TextStyle TabLabelTextStyle => _tabLabelTextStyle ?? _defaults.TabLabelTextStyle;

    /// <summary>The style for a navigation bar's middle title.</summary>
    public virtual TextStyle NavTitleTextStyle => _navTitleTextStyle ?? _defaults.NavTitleTextStyle;

    /// <summary>The style for a navigation bar's large title.</summary>
    public virtual TextStyle NavLargeTitleTextStyle =>
        _navLargeTitleTextStyle ?? _defaults.NavLargeTitleTextStyle;

    /// <summary>The style for a navigation bar's action text.</summary>
    public virtual TextStyle NavActionTextStyle =>
        _navActionTextStyle ?? _defaults.NavActionTextStyle(_primaryColor);

    /// <summary>The style for a picker's rows.</summary>
    public virtual TextStyle PickerTextStyle => _pickerTextStyle ?? _defaults.PickerTextStyle;

    /// <summary>The style for a date-time picker's rows.</summary>
    public virtual TextStyle DateTimePickerTextStyle =>
        _dateTimePickerTextStyle ?? _defaults.DateTimePickerTextStyle;

    /// <summary>Returns a copy of this text theme with every dynamic color resolved.</summary>
    public CupertinoTextThemeData ResolveFrom(BuildContext context)
    {
        return new CupertinoTextThemeData(
            _defaults.ResolveFrom(context),
            CupertinoDynamicColor.MaybeResolve(_primaryColor, context),
            ResolveTextStyle(_textStyle, context),
            ResolveTextStyle(_actionTextStyle, context),
            ResolveTextStyle(_actionSmallTextStyle, context),
            ResolveTextStyle(_tabLabelTextStyle, context),
            ResolveTextStyle(_navTitleTextStyle, context),
            ResolveTextStyle(_navLargeTitleTextStyle, context),
            ResolveTextStyle(_navActionTextStyle, context),
            ResolveTextStyle(_pickerTextStyle, context),
            ResolveTextStyle(_dateTimePickerTextStyle, context));
    }

    // Dart's top-level `_resolveTextStyle`. This does not resolve the shadow color, foreground,
    // background, etc.
    private static TextStyle? ResolveTextStyle(TextStyle? style, BuildContext context) =>
        style?.CopyWith(
            color: CupertinoDynamicColor.MaybeResolve(style.Color, context),
            backgroundColor: CupertinoDynamicColor.MaybeResolve(style.BackgroundColor, context),
            decorationColor: CupertinoDynamicColor.MaybeResolve(style.DecorationColor, context));

    public CupertinoTextThemeData CopyWith(
        Color? primaryColor = null,
        TextStyle? textStyle = null,
        TextStyle? actionTextStyle = null,
        TextStyle? actionSmallTextStyle = null,
        TextStyle? tabLabelTextStyle = null,
        TextStyle? navTitleTextStyle = null,
        TextStyle? navLargeTitleTextStyle = null,
        TextStyle? navActionTextStyle = null,
        TextStyle? pickerTextStyle = null,
        TextStyle? dateTimePickerTextStyle = null)
    {
        return new CupertinoTextThemeData(
            _defaults,
            primaryColor ?? _primaryColor,
            textStyle ?? _textStyle,
            actionTextStyle ?? _actionTextStyle,
            actionSmallTextStyle ?? _actionSmallTextStyle,
            tabLabelTextStyle ?? _tabLabelTextStyle,
            navTitleTextStyle ?? _navTitleTextStyle,
            navLargeTitleTextStyle ?? _navLargeTitleTextStyle,
            navActionTextStyle ?? _navActionTextStyle,
            pickerTextStyle ?? _pickerTextStyle,
            dateTimePickerTextStyle ?? _dateTimePickerTextStyle);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is null || obj.GetType() != GetType())
        {
            return false;
        }

        var other = (CupertinoTextThemeData)obj;
        return Equals(other._defaults, _defaults)
               && other._primaryColor == _primaryColor
               && other._textStyle == _textStyle
               && other._actionTextStyle == _actionTextStyle
               && other._actionSmallTextStyle == _actionSmallTextStyle
               && other._tabLabelTextStyle == _tabLabelTextStyle
               && other._navTitleTextStyle == _navTitleTextStyle
               && other._navLargeTitleTextStyle == _navLargeTitleTextStyle
               && other._navActionTextStyle == _navActionTextStyle
               && other._pickerTextStyle == _pickerTextStyle
               && other._dateTimePickerTextStyle == _dateTimePickerTextStyle;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_defaults);
        hash.Add(_primaryColor);
        hash.Add(_textStyle);
        hash.Add(_actionTextStyle);
        hash.Add(_actionSmallTextStyle);
        hash.Add(_tabLabelTextStyle);
        hash.Add(_navTitleTextStyle);
        hash.Add(_navLargeTitleTextStyle);
        hash.Add(_navActionTextStyle);
        hash.Add(_pickerTextStyle);
        hash.Add(_dateTimePickerTextStyle);
        return hash.ToHashCode();
    }
}

/// <summary>Dart's private `_TextThemeDefaultsBuilder`: the label/action colors the defaults use.</summary>
internal sealed class TextThemeDefaultsBuilder
{
    internal TextThemeDefaultsBuilder(Color labelColor, Color inactiveGrayColor)
    {
        LabelColor = labelColor;
        InactiveGrayColor = inactiveGrayColor;
    }

    internal Color LabelColor { get; }

    internal Color InactiveGrayColor { get; }

    internal TextStyle TextStyle => ApplyLabelColor(CupertinoTextThemeData.DefaultTextStyle, LabelColor);

    internal TextStyle TabLabelTextStyle =>
        ApplyLabelColor(CupertinoTextThemeData.DefaultTabLabelTextStyle, InactiveGrayColor);

    internal TextStyle NavTitleTextStyle =>
        ApplyLabelColor(CupertinoTextThemeData.DefaultMiddleTitleTextStyle, LabelColor);

    internal TextStyle NavLargeTitleTextStyle =>
        ApplyLabelColor(CupertinoTextThemeData.DefaultLargeTitleTextStyle, LabelColor);

    internal TextStyle PickerTextStyle =>
        ApplyLabelColor(CupertinoTextThemeData.DefaultPickerTextStyle, LabelColor);

    internal TextStyle DateTimePickerTextStyle =>
        ApplyLabelColor(CupertinoTextThemeData.DefaultDateTimePickerTextStyle, LabelColor);

    internal TextStyle ActionTextStyle(Color? primaryColor)
    {
        return CupertinoTextThemeData.DefaultActionTextStyle.CopyWith(color: primaryColor);
    }

    internal TextStyle ActionSmallTextStyle(Color? primaryColor)
    {
        return CupertinoTextThemeData.DefaultActionSmallTextStyle.CopyWith(color: primaryColor);
    }

    internal TextStyle NavActionTextStyle(Color? primaryColor) => ActionTextStyle(primaryColor);

    internal TextThemeDefaultsBuilder ResolveFrom(BuildContext context)
    {
        Color resolvedLabelColor = CupertinoDynamicColor.Resolve(LabelColor, context);
        Color resolvedInactiveGray = CupertinoDynamicColor.Resolve(InactiveGrayColor, context);
        return resolvedLabelColor == LabelColor && resolvedInactiveGray == CupertinoColors.InactiveGray
            ? this
            : new TextThemeDefaultsBuilder(resolvedLabelColor, resolvedInactiveGray);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return obj is TextThemeDefaultsBuilder other
               && other.LabelColor == LabelColor
               && other.InactiveGrayColor == InactiveGrayColor;
    }

    public override int GetHashCode() => HashCode.Combine(LabelColor, InactiveGrayColor);

    private static TextStyle ApplyLabelColor(TextStyle original, Color color)
    {
        return original.Color == color ? original : original.CopyWith(color: color);
    }
}
