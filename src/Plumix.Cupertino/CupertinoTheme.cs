using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/theme.dart

/// <summary>Applies a visual styling theme to descendant Cupertino widgets.</summary>
public sealed class CupertinoTheme : StatelessWidget
{
    internal static readonly CupertinoThemeDefaults KDefaultTheme = new(
        brightness: null,
        primaryColor: CupertinoColors.SystemBlue,
        primaryContrastingColor: CupertinoColors.White,
        barBackgroundColor: CupertinoDynamicColor.WithBrightness(
            Color.FromUInt32(0xF0F9F9F9),
            Color.FromUInt32(0xF01D1D1D)),
        scaffoldBackgroundColor: CupertinoColors.SystemBackground,
        selectionHandleColor: CupertinoColors.SystemBlue,
        applyThemeToAll: false,
        textThemeDefaults: new CupertinoTextThemeDefaults(
            CupertinoColors.Label,
            CupertinoColors.InactiveGray));

    public CupertinoTheme(CupertinoThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public CupertinoThemeData Data { get; }

    public Widget Child { get; }

    /// <summary>The nearest ancestor theme, with every dynamic color resolved against the context.</summary>
    public static CupertinoThemeData Of(BuildContext context)
    {
        InheritedCupertinoTheme? inheritedTheme = context.DependOnInherited<InheritedCupertinoTheme>();
        return (inheritedTheme?.Theme.Data ?? new CupertinoThemeData()).ResolveFrom(context);
    }

    /// <summary>
    /// The brightness the nearest ancestor theme declares, falling back to the platform brightness.
    /// </summary>
    public static PlatformBrightness BrightnessOf(BuildContext context)
    {
        InheritedCupertinoTheme? inheritedTheme = context.DependOnInherited<InheritedCupertinoTheme>();
        return inheritedTheme?.Theme.Data.Brightness ?? MediaQuery.PlatformBrightnessOf(context);
    }

    /// <summary>The null-tolerant form of <see cref="BrightnessOf"/>.</summary>
    public static PlatformBrightness? MaybeBrightnessOf(BuildContext context)
    {
        InheritedCupertinoTheme? inheritedTheme = context.DependOnInherited<InheritedCupertinoTheme>();
        return inheritedTheme?.Theme.Data.Brightness ?? MediaQuery.MaybePlatformBrightnessOf(context);
    }

    public override Widget Build(BuildContext context)
    {
        // Dart wraps the child in `IconTheme(data: CupertinoIconThemeData(color: data.primaryColor))`
        // and lets `CupertinoIconThemeData.resolve` resolve at the consumer's context.
        // `CupertinoIconThemeData` needs a subclassable core `IconThemeData`, which Plumix does not
        // have yet (`docs/CUPERTINO_TODO.md` > `icon_theme_data.dart`), so the color is resolved once,
        // below the inherited theme, where the brightness this theme declares is already visible.
        return new InheritedCupertinoTheme(
            theme: this,
            child: new Builder(iconContext => new IconTheme(
                data: new IconThemeData(Color: CupertinoDynamicColor.Resolve(Data.PrimaryColor, iconContext)),
                child: Child)));
    }
}

/// <summary>Provides a <see cref="CupertinoTheme"/> to the widgets below it.</summary>
public sealed class InheritedCupertinoTheme : InheritedTheme
{
    public InheritedCupertinoTheme(CupertinoTheme theme, Widget child, Key? key = null) : base(key)
    {
        Theme = theme ?? throw new ArgumentNullException(nameof(theme));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public CupertinoTheme Theme { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new CupertinoTheme(Theme.Data, child);
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((InheritedCupertinoTheme)oldWidget).Theme.Data, Theme.Data);
    }
}

/// <summary>
/// Styling specifications for a <see cref="CupertinoTheme"/>. Unspecified members fall back to the
/// iOS defaults; use <see cref="NoDefault"/> to observe what was actually specified.
/// </summary>
public class CupertinoThemeData : NoDefaultCupertinoThemeData
{
    private readonly CupertinoThemeDefaults _defaults;

    public CupertinoThemeData(
        PlatformBrightness? brightness = null,
        CupertinoDynamicColor? primaryColor = null,
        CupertinoDynamicColor? primaryContrastingColor = null,
        CupertinoTextThemeData? textTheme = null,
        CupertinoDynamicColor? barBackgroundColor = null,
        CupertinoDynamicColor? scaffoldBackgroundColor = null,
        CupertinoDynamicColor? selectionHandleColor = null,
        bool? applyThemeToAll = null)
        : this(
            brightness,
            primaryColor,
            primaryContrastingColor,
            textTheme,
            barBackgroundColor,
            scaffoldBackgroundColor,
            selectionHandleColor,
            applyThemeToAll,
            CupertinoTheme.KDefaultTheme)
    {
    }

    private protected CupertinoThemeData(
        PlatformBrightness? brightness,
        CupertinoDynamicColor? primaryColor,
        CupertinoDynamicColor? primaryContrastingColor,
        CupertinoTextThemeData? textTheme,
        CupertinoDynamicColor? barBackgroundColor,
        CupertinoDynamicColor? scaffoldBackgroundColor,
        CupertinoDynamicColor? selectionHandleColor,
        bool? applyThemeToAll,
        CupertinoThemeDefaults defaults)
        : base(
            brightness,
            primaryColor,
            primaryContrastingColor,
            textTheme,
            barBackgroundColor,
            scaffoldBackgroundColor,
            selectionHandleColor,
            applyThemeToAll)
    {
        _defaults = defaults;
    }

    public override CupertinoDynamicColor PrimaryColor => base.PrimaryColor ?? _defaults.PrimaryColor;

    public override CupertinoDynamicColor PrimaryContrastingColor =>
        base.PrimaryContrastingColor ?? _defaults.PrimaryContrastingColor;

    public override CupertinoTextThemeData TextTheme =>
        base.TextTheme ?? _defaults.TextThemeDefaults.CreateDefaults(PrimaryColor);

    public override CupertinoDynamicColor BarBackgroundColor =>
        base.BarBackgroundColor ?? _defaults.BarBackgroundColor;

    public override CupertinoDynamicColor ScaffoldBackgroundColor =>
        base.ScaffoldBackgroundColor ?? _defaults.ScaffoldBackgroundColor;

    public override CupertinoDynamicColor SelectionHandleColor =>
        base.SelectionHandleColor ?? _defaults.SelectionHandleColor;

    // Dart overrides `applyThemeToAll` to a non-nullable `bool`; C# covariant returns do not cover
    // `bool?` -> `bool`, so the defaulted value hides the nullable one instead of overriding it.
    public new bool ApplyThemeToAll => base.ApplyThemeToAll ?? _defaults.ApplyThemeToAll;

    public override NoDefaultCupertinoThemeData NoDefault()
    {
        return new NoDefaultCupertinoThemeData(
            base.Brightness,
            base.PrimaryColor,
            base.PrimaryContrastingColor,
            base.TextTheme,
            base.BarBackgroundColor,
            base.ScaffoldBackgroundColor,
            base.SelectionHandleColor,
            base.ApplyThemeToAll);
    }

    public override CupertinoThemeData ResolveFrom(BuildContext context)
    {
        return new CupertinoThemeData(
            Brightness,
            base.PrimaryColor?.ResolveFrom(context),
            base.PrimaryContrastingColor?.ResolveFrom(context),
            base.TextTheme?.ResolveFrom(context),
            base.BarBackgroundColor?.ResolveFrom(context),
            base.ScaffoldBackgroundColor?.ResolveFrom(context),
            base.SelectionHandleColor?.ResolveFrom(context),
            ApplyThemeToAll,
            _defaults.ResolveFrom(context, resolveTextTheme: base.TextTheme is null));
    }

    public override CupertinoThemeData CopyWith(
        PlatformBrightness? brightness = null,
        CupertinoDynamicColor? primaryColor = null,
        CupertinoDynamicColor? primaryContrastingColor = null,
        CupertinoTextThemeData? textTheme = null,
        CupertinoDynamicColor? barBackgroundColor = null,
        CupertinoDynamicColor? scaffoldBackgroundColor = null,
        CupertinoDynamicColor? selectionHandleColor = null,
        bool? applyThemeToAll = null)
    {
        return new CupertinoThemeData(
            brightness ?? base.Brightness,
            primaryColor ?? base.PrimaryColor,
            primaryContrastingColor ?? base.PrimaryContrastingColor,
            textTheme ?? base.TextTheme,
            barBackgroundColor ?? base.BarBackgroundColor,
            scaffoldBackgroundColor ?? base.ScaffoldBackgroundColor,
            selectionHandleColor ?? base.SelectionHandleColor,
            applyThemeToAll ?? base.ApplyThemeToAll,
            _defaults);
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

        var other = (CupertinoThemeData)obj;
        return other.Brightness == Brightness
               && other.PrimaryColor == PrimaryColor
               && other.PrimaryContrastingColor == PrimaryContrastingColor
               && Equals(other.TextTheme, TextTheme)
               && other.BarBackgroundColor == BarBackgroundColor
               && other.ScaffoldBackgroundColor == ScaffoldBackgroundColor
               && other.SelectionHandleColor == SelectionHandleColor
               && other.ApplyThemeToAll == ApplyThemeToAll;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Brightness);
        hash.Add(PrimaryColor);
        hash.Add(PrimaryContrastingColor);
        hash.Add(TextTheme);
        hash.Add(BarBackgroundColor);
        hash.Add(ScaffoldBackgroundColor);
        hash.Add(SelectionHandleColor);
        hash.Add(ApplyThemeToAll);
        return hash.ToHashCode();
    }
}

/// <summary>
/// A <see cref="CupertinoThemeData"/> that reports what was actually specified — every member is
/// null unless a value was given. Used by widgets that need to distinguish "unset" from "default".
/// </summary>
public class NoDefaultCupertinoThemeData
{
    private readonly PlatformBrightness? _brightness;
    private readonly CupertinoDynamicColor? _primaryColor;
    private readonly CupertinoDynamicColor? _primaryContrastingColor;
    private readonly CupertinoTextThemeData? _textTheme;
    private readonly CupertinoDynamicColor? _barBackgroundColor;
    private readonly CupertinoDynamicColor? _scaffoldBackgroundColor;
    private readonly CupertinoDynamicColor? _selectionHandleColor;
    private readonly bool? _applyThemeToAll;

    public NoDefaultCupertinoThemeData(
        PlatformBrightness? brightness = null,
        CupertinoDynamicColor? primaryColor = null,
        CupertinoDynamicColor? primaryContrastingColor = null,
        CupertinoTextThemeData? textTheme = null,
        CupertinoDynamicColor? barBackgroundColor = null,
        CupertinoDynamicColor? scaffoldBackgroundColor = null,
        CupertinoDynamicColor? selectionHandleColor = null,
        bool? applyThemeToAll = null)
    {
        _brightness = brightness;
        _primaryColor = primaryColor;
        _primaryContrastingColor = primaryContrastingColor;
        _textTheme = textTheme;
        _barBackgroundColor = barBackgroundColor;
        _scaffoldBackgroundColor = scaffoldBackgroundColor;
        _selectionHandleColor = selectionHandleColor;
        _applyThemeToAll = applyThemeToAll;
    }

    /// <summary>The brightness descendants should assume, or null to defer to the platform.</summary>
    public PlatformBrightness? Brightness => _brightness;

    public virtual CupertinoDynamicColor? PrimaryColor => _primaryColor;

    public virtual CupertinoDynamicColor? PrimaryContrastingColor => _primaryContrastingColor;

    public virtual CupertinoTextThemeData? TextTheme => _textTheme;

    public virtual CupertinoDynamicColor? BarBackgroundColor => _barBackgroundColor;

    public virtual CupertinoDynamicColor? ScaffoldBackgroundColor => _scaffoldBackgroundColor;

    public virtual CupertinoDynamicColor? SelectionHandleColor => _selectionHandleColor;

    /// <summary>Whether Cupertino theming also applies to Material descendants.</summary>
    public virtual bool? ApplyThemeToAll => _applyThemeToAll;

    public virtual NoDefaultCupertinoThemeData NoDefault() => this;

    public virtual NoDefaultCupertinoThemeData ResolveFrom(BuildContext context)
    {
        return new NoDefaultCupertinoThemeData(
            _brightness,
            _primaryColor?.ResolveFrom(context),
            _primaryContrastingColor?.ResolveFrom(context),
            _textTheme?.ResolveFrom(context),
            _barBackgroundColor?.ResolveFrom(context),
            _scaffoldBackgroundColor?.ResolveFrom(context),
            _selectionHandleColor?.ResolveFrom(context),
            _applyThemeToAll);
    }

    public virtual NoDefaultCupertinoThemeData CopyWith(
        PlatformBrightness? brightness = null,
        CupertinoDynamicColor? primaryColor = null,
        CupertinoDynamicColor? primaryContrastingColor = null,
        CupertinoTextThemeData? textTheme = null,
        CupertinoDynamicColor? barBackgroundColor = null,
        CupertinoDynamicColor? scaffoldBackgroundColor = null,
        CupertinoDynamicColor? selectionHandleColor = null,
        bool? applyThemeToAll = null)
    {
        return new NoDefaultCupertinoThemeData(
            brightness ?? _brightness,
            primaryColor ?? _primaryColor,
            primaryContrastingColor ?? _primaryContrastingColor,
            textTheme ?? _textTheme,
            barBackgroundColor ?? _barBackgroundColor,
            scaffoldBackgroundColor ?? _scaffoldBackgroundColor,
            selectionHandleColor ?? _selectionHandleColor,
            applyThemeToAll ?? _applyThemeToAll);
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

        // Flutter's `NoDefaultCupertinoThemeData` leaves `selectionHandleColor` out of `==` and
        // `hashCode`; kept as-is so the two implementations agree member for member.
        var other = (NoDefaultCupertinoThemeData)obj;
        return other._brightness == _brightness
               && other._primaryColor == _primaryColor
               && other._primaryContrastingColor == _primaryContrastingColor
               && Equals(other._textTheme, _textTheme)
               && other._barBackgroundColor == _barBackgroundColor
               && other._scaffoldBackgroundColor == _scaffoldBackgroundColor
               && other._applyThemeToAll == _applyThemeToAll;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_brightness);
        hash.Add(_primaryColor);
        hash.Add(_primaryContrastingColor);
        hash.Add(_textTheme);
        hash.Add(_barBackgroundColor);
        hash.Add(_scaffoldBackgroundColor);
        hash.Add(_applyThemeToAll);
        return hash.ToHashCode();
    }
}

/// <summary>Dart's private `_CupertinoThemeDefaults`: the iOS fallbacks a theme falls back to.</summary>
internal sealed class CupertinoThemeDefaults
{
    internal CupertinoThemeDefaults(
        PlatformBrightness? brightness,
        CupertinoDynamicColor primaryColor,
        CupertinoDynamicColor primaryContrastingColor,
        CupertinoDynamicColor barBackgroundColor,
        CupertinoDynamicColor scaffoldBackgroundColor,
        CupertinoDynamicColor selectionHandleColor,
        bool applyThemeToAll,
        CupertinoTextThemeDefaults textThemeDefaults)
    {
        Brightness = brightness;
        PrimaryColor = primaryColor;
        PrimaryContrastingColor = primaryContrastingColor;
        BarBackgroundColor = barBackgroundColor;
        ScaffoldBackgroundColor = scaffoldBackgroundColor;
        SelectionHandleColor = selectionHandleColor;
        ApplyThemeToAll = applyThemeToAll;
        TextThemeDefaults = textThemeDefaults;
    }

    internal PlatformBrightness? Brightness { get; }

    internal CupertinoDynamicColor PrimaryColor { get; }

    internal CupertinoDynamicColor PrimaryContrastingColor { get; }

    internal CupertinoDynamicColor BarBackgroundColor { get; }

    internal CupertinoDynamicColor ScaffoldBackgroundColor { get; }

    internal CupertinoDynamicColor SelectionHandleColor { get; }

    internal bool ApplyThemeToAll { get; }

    internal CupertinoTextThemeDefaults TextThemeDefaults { get; }

    internal CupertinoThemeDefaults ResolveFrom(BuildContext context, bool resolveTextTheme)
    {
        return new CupertinoThemeDefaults(
            Brightness,
            PrimaryColor.ResolveFrom(context),
            PrimaryContrastingColor.ResolveFrom(context),
            BarBackgroundColor.ResolveFrom(context),
            ScaffoldBackgroundColor.ResolveFrom(context),
            SelectionHandleColor.ResolveFrom(context),
            ApplyThemeToAll,
            resolveTextTheme ? TextThemeDefaults.ResolveFrom(context) : TextThemeDefaults);
    }
}

/// <summary>Dart's private `_CupertinoTextThemeDefaults`.</summary>
internal sealed class CupertinoTextThemeDefaults
{
    internal CupertinoTextThemeDefaults(CupertinoDynamicColor labelColor, CupertinoDynamicColor inactiveGray)
    {
        LabelColor = labelColor;
        InactiveGray = inactiveGray;
    }

    internal CupertinoDynamicColor LabelColor { get; }

    internal CupertinoDynamicColor InactiveGray { get; }

    internal CupertinoTextThemeDefaults ResolveFrom(BuildContext context)
    {
        return new CupertinoTextThemeDefaults(
            LabelColor.ResolveFrom(context),
            InactiveGray.ResolveFrom(context));
    }

    internal CupertinoTextThemeData CreateDefaults(CupertinoDynamicColor primaryColor)
    {
        return new DefaultCupertinoTextThemeData(LabelColor, InactiveGray, primaryColor);
    }
}

/// <summary>Dart's private `_DefaultCupertinoTextThemeData`.</summary>
internal sealed class DefaultCupertinoTextThemeData : CupertinoTextThemeData
{
    private readonly CupertinoDynamicColor _labelColor;
    private readonly CupertinoDynamicColor _inactiveGray;

    internal DefaultCupertinoTextThemeData(
        CupertinoDynamicColor labelColor,
        CupertinoDynamicColor inactiveGray,
        CupertinoDynamicColor primaryColor) : base(primaryColor: primaryColor)
    {
        _labelColor = labelColor;
        _inactiveGray = inactiveGray;
    }

    public override TextStyle TextStyle => base.TextStyle.CopyWith(color: _labelColor.Value);

    public override TextStyle TabLabelTextStyle => base.TabLabelTextStyle.CopyWith(color: _inactiveGray.Value);

    public override TextStyle NavTitleTextStyle => base.NavTitleTextStyle.CopyWith(color: _labelColor.Value);

    public override TextStyle NavLargeTitleTextStyle =>
        base.NavLargeTitleTextStyle.CopyWith(color: _labelColor.Value);

    public override TextStyle PickerTextStyle => base.PickerTextStyle.CopyWith(color: _labelColor.Value);

    public override TextStyle DateTimePickerTextStyle =>
        base.DateTimePickerTextStyle.CopyWith(color: _labelColor.Value);
}
