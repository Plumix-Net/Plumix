using Avalonia.Media;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/text_theme.dart

/// <summary>Cupertino typography: the text styles every Cupertino control resolves through.</summary>
/// <remarks>
/// Dart stores the (possibly dynamic) label/action colors inside the default <c>TextStyle</c>s and
/// resolves them again in <c>resolveFrom</c>. Plumix's <see cref="TextStyle.Color"/> is a plain
/// <see cref="Avalonia.Media.Color"/>, so the dynamic colors live in
/// <see cref="TextThemeDefaultsBuilder"/> instead and are applied after resolution — the defaults
/// behave identically, but a dynamic color placed in a caller-supplied <c>TextStyle</c> is captured
/// at its light/base variant (see `docs/ai/DIVERGENCES.md`).
/// </remarks>
public class CupertinoTextThemeData
{
    internal static readonly TextStyle DefaultTextStyle = new(
        Inherit: false,
        FontFamily: new FontFamily("CupertinoSystemText"),
        FontSize: 17.0,
        LetterSpacing: -0.41,
        Color: CupertinoColors.Label.Value,
        Decoration: Plumix.UI.TextDecoration.None);

    internal static readonly TextStyle DefaultActionTextStyle = new(
        Inherit: false,
        FontFamily: new FontFamily("CupertinoSystemText"),
        FontSize: 17.0,
        LetterSpacing: -0.41,
        Color: CupertinoColors.ActiveBlue.Value,
        Decoration: Plumix.UI.TextDecoration.None);

    internal static readonly TextStyle DefaultActionSmallTextStyle = new(
        Inherit: false,
        FontFamily: new FontFamily("CupertinoSystemText"),
        FontSize: 15.0,
        LetterSpacing: -0.23,
        Color: CupertinoColors.ActiveBlue.Value,
        Decoration: Plumix.UI.TextDecoration.None);

    internal static readonly TextStyle DefaultTabLabelTextStyle = new(
        Inherit: false,
        FontFamily: new FontFamily("CupertinoSystemText"),
        FontSize: 10.0,
        FontWeight: FontWeight.Medium,
        LetterSpacing: -0.24,
        Color: CupertinoColors.InactiveGray.Value);

    internal static readonly TextStyle DefaultMiddleTitleTextStyle = new(
        Inherit: false,
        FontFamily: new FontFamily("CupertinoSystemText"),
        FontSize: 17.0,
        FontWeight: FontWeight.SemiBold,
        LetterSpacing: -0.41,
        Color: CupertinoColors.Label.Value);

    internal static readonly TextStyle DefaultLargeTitleTextStyle = new(
        Inherit: false,
        FontFamily: new FontFamily("CupertinoSystemDisplay"),
        FontSize: 34.0,
        FontWeight: FontWeight.Bold,
        LetterSpacing: 0.38,
        Color: CupertinoColors.Label.Value);

    internal static readonly TextStyle DefaultPickerTextStyle = new(
        Inherit: false,
        FontFamily: new FontFamily("CupertinoSystemDisplay"),
        FontSize: 21.0,
        FontWeight: FontWeight.Regular,
        LetterSpacing: -0.6,
        Color: CupertinoColors.Label.Value);

    internal static readonly TextStyle DefaultDateTimePickerTextStyle = new(
        Inherit: false,
        FontFamily: new FontFamily("CupertinoSystemDisplay"),
        FontSize: 21.0,
        FontWeight: FontWeight.Normal,
        LetterSpacing: 0.4,
        Color: CupertinoColors.Label.Value);

    private readonly TextThemeDefaultsBuilder _defaults;
    private readonly CupertinoDynamicColor? _primaryColor;
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
        CupertinoDynamicColor? primaryColor = null,
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
        CupertinoDynamicColor? primaryColor,
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
            _primaryColor?.ResolveFrom(context),
            _textStyle,
            _actionTextStyle,
            _actionSmallTextStyle,
            _tabLabelTextStyle,
            _navTitleTextStyle,
            _navLargeTitleTextStyle,
            _navActionTextStyle,
            _pickerTextStyle,
            _dateTimePickerTextStyle);
    }

    public CupertinoTextThemeData CopyWith(
        CupertinoDynamicColor? primaryColor = null,
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
    internal TextThemeDefaultsBuilder(CupertinoDynamicColor labelColor, CupertinoDynamicColor inactiveGrayColor)
    {
        LabelColor = labelColor;
        InactiveGrayColor = inactiveGrayColor;
    }

    internal CupertinoDynamicColor LabelColor { get; }

    internal CupertinoDynamicColor InactiveGrayColor { get; }

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

    internal TextStyle ActionTextStyle(CupertinoDynamicColor? primaryColor)
    {
        return CupertinoTextThemeData.DefaultActionTextStyle.CopyWith(color: primaryColor?.Value);
    }

    internal TextStyle ActionSmallTextStyle(CupertinoDynamicColor? primaryColor)
    {
        return CupertinoTextThemeData.DefaultActionSmallTextStyle.CopyWith(color: primaryColor?.Value);
    }

    internal TextStyle NavActionTextStyle(CupertinoDynamicColor? primaryColor) => ActionTextStyle(primaryColor);

    internal TextThemeDefaultsBuilder ResolveFrom(BuildContext context)
    {
        CupertinoDynamicColor resolvedLabelColor = LabelColor.ResolveFrom(context);
        CupertinoDynamicColor resolvedInactiveGray = InactiveGrayColor.ResolveFrom(context);
        return resolvedLabelColor.Value == LabelColor.Value
               && resolvedInactiveGray.Value == CupertinoColors.InactiveGray.Value
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

    private static TextStyle ApplyLabelColor(TextStyle original, CupertinoDynamicColor color)
    {
        return original.Color == color.Value ? original : original.CopyWith(color: color.Value);
    }
}
