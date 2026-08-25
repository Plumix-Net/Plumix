using Plumix.Rendering;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/constants.dart

public static class CupertinoConstants
{
    /// <summary>
    /// Flutter's `kMinInteractiveDimensionCupertino`: the minimum dimension of any interactive
    /// region, per the iOS Human Interface Guidelines.
    /// </summary>
    public const double MinInteractiveDimensionCupertino = 44.0;

    /// <summary>
    /// Flutter's `kCupertinoFocusColorOpacity`: the relative opacity applied to a color to make it
    /// suitable for a focus ring.
    /// </summary>
    public const double CupertinoFocusColorOpacity = 0.80;

    /// <summary>
    /// Flutter's `kCupertinoFocusColorBrightness`: the HSL lightness applied to a color to make it
    /// suitable for a focus ring.
    /// </summary>
    public const double CupertinoFocusColorBrightness = 0.69;

    /// <summary>
    /// Flutter's `kCupertinoFocusColorSaturation`: the HSL saturation applied to a color to make it
    /// suitable for a focus ring.
    /// </summary>
    public const double CupertinoFocusColorSaturation = 0.835;

    /// <summary>
    /// Flutter's `kCupertinoButtonTintedOpacityLight`: the background opacity of a
    /// <see cref="CupertinoButton"/> created with <see cref="CupertinoButton.Tinted"/> in light mode.
    /// </summary>
    public const double CupertinoButtonTintedOpacityLight = 0.12;

    /// <summary>
    /// Flutter's `kCupertinoButtonTintedOpacityDark`: the background opacity of a
    /// <see cref="CupertinoButton"/> created with <see cref="CupertinoButton.Tinted"/> in dark mode.
    /// </summary>
    public const double CupertinoButtonTintedOpacityDark = 0.26;

    /// <summary>
    /// Flutter's `kCupertinoButtonDefaultIconSize`: the icon size a <see cref="CupertinoButton"/>
    /// falls back to when its action text style has no font size.
    /// </summary>
    public const double CupertinoButtonDefaultIconSize = 20.0;

    /// <summary>
    /// Flutter's `kCupertinoButtonTapMoveSlop`: how far a pressed button may be dragged away from
    /// before it stops looking pressed. Effective on mobile platforms only.
    /// </summary>
    public const double CupertinoButtonTapMoveSlop = 70.0;

    /// <summary>Flutter's `kCupertinoButtonPadding`: the padding for each button size.</summary>
    public static IReadOnlyDictionary<CupertinoButtonSize, EdgeInsetsGeometry> CupertinoButtonPadding
    { get; } = new Dictionary<CupertinoButtonSize, EdgeInsetsGeometry>
    {
        [CupertinoButtonSize.Small] = EdgeInsets.Symmetric(vertical: 6, horizontal: 12),
        [CupertinoButtonSize.Medium] = EdgeInsets.Symmetric(vertical: 10, horizontal: 15),
        [CupertinoButtonSize.Large] = EdgeInsets.Symmetric(vertical: 16, horizontal: 20),
    };

    /// <summary>
    /// Flutter's `kCupertinoButtonSizeBorderRadius`: the corner radius for each button size.
    /// </summary>
    public static IReadOnlyDictionary<CupertinoButtonSize, BorderRadius> CupertinoButtonSizeBorderRadius
    { get; } = new Dictionary<CupertinoButtonSize, BorderRadius>
    {
        [CupertinoButtonSize.Small] = BorderRadius.Circular(40),
        [CupertinoButtonSize.Medium] = BorderRadius.Circular(40),
        [CupertinoButtonSize.Large] = BorderRadius.Circular(12),
    };

    /// <summary>Flutter's `kCupertinoButtonMinSize`: the minimum size for each button size.</summary>
    public static IReadOnlyDictionary<CupertinoButtonSize, double> CupertinoButtonMinSize { get; } =
        new Dictionary<CupertinoButtonSize, double>
        {
            [CupertinoButtonSize.Small] = 28,
            [CupertinoButtonSize.Medium] = 32,
            [CupertinoButtonSize.Large] = 44,
        };
}
