using System.Diagnostics;
using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/media_query.dart

public enum Orientation
{
    Portrait,
    Landscape,
}

public enum NavigationMode
{
    Traditional,
    Directional,
}

public enum DisplayFeatureType
{
    Fold,
    Hinge,
    Cutout,
    Unknown,
}

public enum DisplayFeatureState
{
    Unknown,
    PostureFlat,
    PostureHalfOpened,
}

public sealed record DisplayFeature(
    Rect Bounds,
    DisplayFeatureType Type = DisplayFeatureType.Unknown,
    DisplayFeatureState State = DisplayFeatureState.Unknown);

public sealed record MediaQueryData
{
    public MediaQueryData(
        Size Size = default,
        double DevicePixelRatio = 1.0,
        Thickness Padding = default,
        Thickness ViewInsets = default,
        Thickness SystemGestureInsets = default,
        Thickness ViewPadding = default,
        double TextScaleFactor = 1.0,
        TextScaler? TextScaler = null,
        bool AccessibleNavigation = false,
        bool AlwaysUse24HourFormat = false,
        bool DisableAnimations = false,
        bool InvertColors = false,
        NavigationMode NavigationMode = NavigationMode.Traditional,
        Brightness PlatformBrightness = Brightness.Light,
        bool HighContrast = false,
        bool SupportsAnnounce = false,
        int ViewId = 0,
        BorderRadius? DisplayCornerRadii = null,
        IReadOnlyList<DisplayFeature>? DisplayFeatures = null,
        bool OnOffSwitchLabels = false,
        DeviceGestureSettings? GestureSettings = null,
        bool BoldText = false,
        bool SupportsShowingSystemContextMenu = false,
        double? LineHeightScaleFactorOverride = null,
        double? LetterSpacingOverride = null,
        double? WordSpacingOverride = null,
        double? ParagraphSpacingOverride = null)
    {
        if (TextScaler is not null && TextScaleFactor != 1.0)
        {
            throw new ArgumentException(
                "TextScaleFactor cannot be specified when TextScaler is specified.",
                nameof(TextScaleFactor));
        }

        this.Size = Size;
        this.DevicePixelRatio = DevicePixelRatio;
        this.Padding = Padding;
        this.ViewInsets = ViewInsets;
        this.SystemGestureInsets = SystemGestureInsets;
        this.ViewPadding = ViewPadding;
        this.TextScaler = TextScaler ?? Painting.TextScaler.Linear(TextScaleFactor);
        this.AccessibleNavigation = AccessibleNavigation;
        this.AlwaysUse24HourFormat = AlwaysUse24HourFormat;
        this.DisableAnimations = DisableAnimations;
        this.InvertColors = InvertColors;
        this.NavigationMode = NavigationMode;
        this.PlatformBrightness = PlatformBrightness;
        this.HighContrast = HighContrast;
        this.OnOffSwitchLabels = OnOffSwitchLabels;
        this.SupportsAnnounce = SupportsAnnounce;
        this.ViewId = ViewId;
        this.DisplayCornerRadii = DisplayCornerRadii;
        this.DisplayFeatures = DisplayFeatures;
        this.GestureSettings = GestureSettings;
        this.BoldText = BoldText;
        this.SupportsShowingSystemContextMenu = SupportsShowingSystemContextMenu;
        this.LineHeightScaleFactorOverride = LineHeightScaleFactorOverride;
        this.LetterSpacingOverride = LetterSpacingOverride;
        this.WordSpacingOverride = WordSpacingOverride;
        this.ParagraphSpacingOverride = ParagraphSpacingOverride;
    }

    public Size Size { get; init; }

    public double DevicePixelRatio { get; init; }

    public Thickness Padding { get; init; }

    public Thickness ViewInsets { get; init; }

    public Thickness SystemGestureInsets { get; init; }

    public Thickness ViewPadding { get; init; }

    public double TextScaleFactor => TextScaler.TextScaleFactor;

    public TextScaler TextScaler { get; init; }

    public bool AccessibleNavigation { get; init; }

    public bool AlwaysUse24HourFormat { get; init; }

    public bool DisableAnimations { get; init; }

    public bool InvertColors { get; init; }

    public NavigationMode NavigationMode { get; init; }

    public Brightness PlatformBrightness { get; init; }

    public bool HighContrast { get; init; }

    public bool OnOffSwitchLabels { get; init; }

    /// <summary>
    /// Whether the platform is requesting that text be drawn with a bold font weight. Ports Dart's
    /// `MediaQueryData.boldText`.
    /// </summary>
    public bool BoldText { get; init; }

    public bool SupportsAnnounce { get; init; }

    public int ViewId { get; init; }

    public BorderRadius? DisplayCornerRadii { get; init; }

    public IReadOnlyList<DisplayFeature>? DisplayFeatures { get; init; }

    /// <summary>
    /// The gesture settings for the view this media query is for, used by gesture recognizers to
    /// size their slop thresholds. Ports Dart's `MediaQueryData.gestureSettings`.
    /// </summary>
    public DeviceGestureSettings? GestureSettings { get; init; }

    /// <summary>
    /// Whether showing the system context menu is supported; for example, on iOS 16.0 and above the
    /// system text selection context menu may be shown instead of the Flutter-drawn one to avoid the
    /// clipboard access notification. Ports Dart's <c>MediaQueryData.supportsShowingSystemContextMenu</c>.
    /// </summary>
    public bool SupportsShowingSystemContextMenu { get; init; }

    /// <summary>
    /// The text height, as a multiple of the font size, that overrides <c>TextStyle.Height</c> and
    /// <c>StrutStyle.Height</c>; <see langword="null"/> when the platform has not set an override.
    /// Ports Dart's <c>MediaQueryData.lineHeightScaleFactorOverride</c>.
    /// </summary>
    public double? LineHeightScaleFactorOverride { get; init; }

    /// <summary>
    /// The space (in logical pixels) between each letter that overrides <c>TextStyle.LetterSpacing</c>;
    /// <see langword="null"/> when the platform has not set an override. Ports Dart's
    /// <c>MediaQueryData.letterSpacingOverride</c>.
    /// </summary>
    public double? LetterSpacingOverride { get; init; }

    /// <summary>
    /// The space (in logical pixels) between each word that overrides <c>TextStyle.WordSpacing</c>;
    /// <see langword="null"/> when the platform has not set an override. Ports Dart's
    /// <c>MediaQueryData.wordSpacingOverride</c>.
    /// </summary>
    public double? WordSpacingOverride { get; init; }

    /// <summary>
    /// The amount of space (in logical pixels) to add following each paragraph in a piece of text;
    /// <see langword="null"/> when the platform has not set an override for text paragraph spacing.
    /// Ports Dart's <c>MediaQueryData.paragraphSpacingOverride</c>.
    /// </summary>
    public double? ParagraphSpacingOverride { get; init; }

    public Orientation Orientation => Size.Width > Size.Height
        ? Orientation.Landscape
        : Orientation.Portrait;

    /// <summary>
    /// The dimensions of the view in physical pixels: <see cref="Size"/> scaled by
    /// <see cref="DevicePixelRatio"/>.
    /// </summary>
    public Size PhysicalSize => new(Size.Width * DevicePixelRatio, Size.Height * DevicePixelRatio);

    public MediaQueryData CopyWith(
        Size? size = null,
        double? devicePixelRatio = null,
        Thickness? padding = null,
        Thickness? viewInsets = null,
        Thickness? systemGestureInsets = null,
        Thickness? viewPadding = null,
        double? textScaleFactor = null,
        TextScaler? textScaler = null,
        bool? accessibleNavigation = null,
        bool? alwaysUse24HourFormat = null,
        bool? disableAnimations = null,
        bool? invertColors = null,
        NavigationMode? navigationMode = null,
        Brightness? platformBrightness = null,
        bool? highContrast = null,
        bool? supportsAnnounce = null,
        int? viewId = null,
        IReadOnlyList<DisplayFeature>? displayFeatures = null,
        bool? onOffSwitchLabels = null,
        DeviceGestureSettings? gestureSettings = null,
        bool? boldText = null,
        bool? supportsShowingSystemContextMenu = null)
    {
        if (textScaleFactor is not null && textScaler is not null)
        {
            throw new ArgumentException("TextScaleFactor and TextScaler cannot both be specified.");
        }

        TextScaler effectiveTextScaler = textScaleFactor is { } scaleFactor
            ? Painting.TextScaler.Linear(scaleFactor)
            : textScaler ?? TextScaler;
        return new MediaQueryData(
            Size: size ?? Size,
            DevicePixelRatio: devicePixelRatio ?? DevicePixelRatio,
            Padding: padding ?? Padding,
            ViewInsets: viewInsets ?? ViewInsets,
            SystemGestureInsets: systemGestureInsets ?? SystemGestureInsets,
            ViewPadding: viewPadding ?? ViewPadding,
            TextScaler: effectiveTextScaler,
            AccessibleNavigation: accessibleNavigation ?? AccessibleNavigation,
            AlwaysUse24HourFormat: alwaysUse24HourFormat ?? AlwaysUse24HourFormat,
            DisableAnimations: disableAnimations ?? DisableAnimations,
            InvertColors: invertColors ?? InvertColors,
            NavigationMode: navigationMode ?? NavigationMode,
            PlatformBrightness: platformBrightness ?? PlatformBrightness,
            HighContrast: highContrast ?? HighContrast,
            OnOffSwitchLabels: onOffSwitchLabels ?? OnOffSwitchLabels,
            SupportsAnnounce: supportsAnnounce ?? SupportsAnnounce,
            ViewId: viewId ?? ViewId,
            DisplayCornerRadii: DisplayCornerRadii,
            DisplayFeatures: displayFeatures ?? DisplayFeatures,
            GestureSettings: gestureSettings ?? GestureSettings,
            BoldText: boldText ?? BoldText,
            SupportsShowingSystemContextMenu: supportsShowingSystemContextMenu
                                              ?? SupportsShowingSystemContextMenu,
            LineHeightScaleFactorOverride: LineHeightScaleFactorOverride,
            LetterSpacingOverride: LetterSpacingOverride,
            WordSpacingOverride: WordSpacingOverride,
            ParagraphSpacingOverride: ParagraphSpacingOverride);
    }

    /// <summary>
    /// Creates a copy of this media query data but with the <c>lineHeightScaleFactorOverride</c>,
    /// <c>letterSpacingOverride</c>, <c>wordSpacingOverride</c>, and <c>paragraphSpacingOverride</c>
    /// replaced with the given values. If an argument is <see langword="null"/>, the corresponding
    /// override is set to <see langword="null"/>.
    /// </summary>
    /// <remarks>Dart's <c>MediaQueryData.applyTextStyleOverrides</c>.</remarks>
    public MediaQueryData ApplyTextStyleOverrides(
        double? lineHeightScaleFactorOverride,
        double? letterSpacingOverride,
        double? wordSpacingOverride,
        double? paragraphSpacingOverride)
    {
        return new MediaQueryData(
            Size: Size,
            DevicePixelRatio: DevicePixelRatio,
            TextScaler: TextScaler,
            PlatformBrightness: PlatformBrightness,
            Padding: Padding,
            ViewPadding: ViewPadding,
            ViewInsets: ViewInsets,
            SystemGestureInsets: SystemGestureInsets,
            AlwaysUse24HourFormat: AlwaysUse24HourFormat,
            InvertColors: InvertColors,
            HighContrast: HighContrast,
            OnOffSwitchLabels: OnOffSwitchLabels,
            DisableAnimations: DisableAnimations,
            AccessibleNavigation: AccessibleNavigation,
            BoldText: BoldText,
            SupportsAnnounce: SupportsAnnounce,
            NavigationMode: NavigationMode,
            ViewId: ViewId,
            GestureSettings: GestureSettings,
            DisplayFeatures: DisplayFeatures,
            SupportsShowingSystemContextMenu: SupportsShowingSystemContextMenu,
            LineHeightScaleFactorOverride: lineHeightScaleFactorOverride,
            LetterSpacingOverride: letterSpacingOverride,
            WordSpacingOverride: wordSpacingOverride,
            ParagraphSpacingOverride: paragraphSpacingOverride,
            DisplayCornerRadii: DisplayCornerRadii);
    }

    /// <summary>
    /// Creates a copy of this media query data but with <see cref="DisplayCornerRadii"/> replaced
    /// with <paramref name="displayCornerRadii"/>; a <see langword="null"/> argument clears it.
    /// </summary>
    /// <remarks>Dart's <c>MediaQueryData.applyDisplayCornerRadii</c>.</remarks>
    public MediaQueryData ApplyDisplayCornerRadii(BorderRadius? displayCornerRadii)
    {
        return new MediaQueryData(
            Size: Size,
            DevicePixelRatio: DevicePixelRatio,
            TextScaler: TextScaler,
            PlatformBrightness: PlatformBrightness,
            Padding: Padding,
            ViewPadding: ViewPadding,
            ViewInsets: ViewInsets,
            SystemGestureInsets: SystemGestureInsets,
            AlwaysUse24HourFormat: AlwaysUse24HourFormat,
            InvertColors: InvertColors,
            HighContrast: HighContrast,
            OnOffSwitchLabels: OnOffSwitchLabels,
            DisableAnimations: DisableAnimations,
            AccessibleNavigation: AccessibleNavigation,
            BoldText: BoldText,
            SupportsAnnounce: SupportsAnnounce,
            NavigationMode: NavigationMode,
            ViewId: ViewId,
            GestureSettings: GestureSettings,
            DisplayFeatures: DisplayFeatures,
            SupportsShowingSystemContextMenu: SupportsShowingSystemContextMenu,
            LineHeightScaleFactorOverride: LineHeightScaleFactorOverride,
            LetterSpacingOverride: LetterSpacingOverride,
            WordSpacingOverride: WordSpacingOverride,
            ParagraphSpacingOverride: ParagraphSpacingOverride,
            DisplayCornerRadii: displayCornerRadii);
    }

    public MediaQueryData RemovePadding(
        bool removeLeft = false,
        bool removeTop = false,
        bool removeRight = false,
        bool removeBottom = false)
    {
        if (!(removeLeft || removeTop || removeRight || removeBottom))
        {
            return this;
        }

        return CopyWith(
            padding: CopyThickness(
                Padding,
                left: removeLeft ? 0.0 : null,
                top: removeTop ? 0.0 : null,
                right: removeRight ? 0.0 : null,
                bottom: removeBottom ? 0.0 : null),
            viewPadding: CopyThickness(
                ViewPadding,
                left: removeLeft ? Math.Max(0.0, ViewPadding.Left - Padding.Left) : null,
                top: removeTop ? Math.Max(0.0, ViewPadding.Top - Padding.Top) : null,
                right: removeRight ? Math.Max(0.0, ViewPadding.Right - Padding.Right) : null,
                bottom: removeBottom ? Math.Max(0.0, ViewPadding.Bottom - Padding.Bottom) : null));
    }

    public MediaQueryData RemoveViewInsets(
        bool removeLeft = false,
        bool removeTop = false,
        bool removeRight = false,
        bool removeBottom = false)
    {
        if (!(removeLeft || removeTop || removeRight || removeBottom))
        {
            return this;
        }

        return CopyWith(
            viewPadding: CopyThickness(
                ViewPadding,
                left: removeLeft ? Math.Max(0.0, ViewPadding.Left - ViewInsets.Left) : null,
                top: removeTop ? Math.Max(0.0, ViewPadding.Top - ViewInsets.Top) : null,
                right: removeRight ? Math.Max(0.0, ViewPadding.Right - ViewInsets.Right) : null,
                bottom: removeBottom ? Math.Max(0.0, ViewPadding.Bottom - ViewInsets.Bottom) : null),
            viewInsets: CopyThickness(
                ViewInsets,
                left: removeLeft ? 0.0 : null,
                top: removeTop ? 0.0 : null,
                right: removeRight ? 0.0 : null,
                bottom: removeBottom ? 0.0 : null));
    }

    public MediaQueryData RemoveViewPadding(
        bool removeLeft = false,
        bool removeTop = false,
        bool removeRight = false,
        bool removeBottom = false)
    {
        if (!(removeLeft || removeTop || removeRight || removeBottom))
        {
            return this;
        }

        return CopyWith(
            padding: CopyThickness(
                Padding,
                left: removeLeft ? 0.0 : null,
                top: removeTop ? 0.0 : null,
                right: removeRight ? 0.0 : null,
                bottom: removeBottom ? 0.0 : null),
            viewPadding: CopyThickness(
                ViewPadding,
                left: removeLeft ? 0.0 : null,
                top: removeTop ? 0.0 : null,
                right: removeRight ? 0.0 : null,
                bottom: removeBottom ? 0.0 : null));
    }

    /// <summary>
    /// Creates a copy of this media query data by removing <see cref="DisplayFeatures"/> that are
    /// completely outside the given sub-screen and adjusting the <see cref="Padding"/>,
    /// <see cref="ViewInsets"/> and <see cref="ViewPadding"/> to be zero on the sides that are not
    /// included in the sub-screen.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>MediaQueryData.removeDisplayFeatures</c>. The <see cref="Size"/> is unchanged:
    /// the sub-screen only drops the features it does not overlap and the insets it excludes.
    /// </remarks>
    public MediaQueryData RemoveDisplayFeatures(Rect subScreen)
    {
        DebugAssertions.Assert(
            subScreen.Left >= 0.0
            && subScreen.Top >= 0.0
            && subScreen.Right <= Size.Width
            && subScreen.Bottom <= Size.Height,
            "'subScreen' argument cannot be outside the bounds of the screen");
        if (subScreen.Size == Size && subScreen.Position == default)
        {
            return this;
        }

        double rightInset = Size.Width - subScreen.Right;
        double bottomInset = Size.Height - subScreen.Bottom;
        return CopyWith(
            padding: ShrinkToSubScreen(Padding, subScreen, rightInset, bottomInset),
            viewPadding: ShrinkToSubScreen(ViewPadding, subScreen, rightInset, bottomInset),
            viewInsets: ShrinkToSubScreen(ViewInsets, subScreen, rightInset, bottomInset),
            displayFeatures: (DisplayFeatures ?? [])
                .Where(displayFeature => RectOverlaps(subScreen, displayFeature.Bounds))
                .ToList());
    }

    // dart:ui `Rect.overlaps`.
    private static bool RectOverlaps(Rect a, Rect b)
    {
        if (a.Right <= b.Left || b.Right <= a.Left)
        {
            return false;
        }

        if (a.Bottom <= b.Top || b.Bottom <= a.Top)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Creates data for a <see cref="MediaQuery"/> from <paramref name="view"/>: view-specific values
    /// (size, insets, features) come from the view, platform-specific values from
    /// <paramref name="platformData"/> when given and from the view's platform dispatcher otherwise.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>MediaQueryData.fromView</c>. The accessibility flags and the text scale factor come
    /// from <c>view.platformDispatcher</c> exactly as in Dart. Plumix's dispatcher does not carry the
    /// platform brightness or the 24-hour setting itself: they come from the platform data the implicit
    /// view's host installed
    /// (<see cref="PlatformDispatcher.ImplicitViewPlatformData"/>) for every view, as Dart reads them from
    /// the one process-wide dispatcher, and fall back to the defaults before a host reported any.
    /// </remarks>
    public static MediaQueryData FromView(FlutterView view, MediaQueryData? platformData = null)
    {
        ArgumentNullException.ThrowIfNull(view);
        PlatformDispatcher platformDispatcher = view.PlatformDispatcher;
        MediaQueryData? dispatcherData = platformDispatcher.ImplicitViewPlatformData;
        AccessibilityFeatures accessibilityFeatures = platformDispatcher.AccessibilityFeatures;

        double devicePixelRatio = view.DevicePixelRatio;
        return new MediaQueryData(
            Size: new Size(view.PhysicalSize.Width / devicePixelRatio, view.PhysicalSize.Height / devicePixelRatio),
            DevicePixelRatio: devicePixelRatio,
            Padding: FromViewPadding(view.Padding, devicePixelRatio),
            ViewInsets: FromViewPadding(view.ViewInsets, devicePixelRatio),
            SystemGestureInsets: FromViewPadding(view.SystemGestureInsets, devicePixelRatio),
            ViewPadding: FromViewPadding(view.ViewPadding, devicePixelRatio),
            TextScaler: platformData?.TextScaler ?? new SystemTextScaler(platformDispatcher),
            AccessibleNavigation: platformData?.AccessibleNavigation ?? accessibilityFeatures.AccessibleNavigation,
            AlwaysUse24HourFormat: platformData?.AlwaysUse24HourFormat
                                   ?? dispatcherData?.AlwaysUse24HourFormat
                                   ?? false,
            DisableAnimations: platformData?.DisableAnimations ?? accessibilityFeatures.DisableAnimations,
            InvertColors: platformData?.InvertColors ?? accessibilityFeatures.InvertColors,
            NavigationMode: platformData?.NavigationMode ?? NavigationMode.Traditional,
            PlatformBrightness: platformData?.PlatformBrightness
                                ?? dispatcherData?.PlatformBrightness
                                ?? Brightness.Light,
            HighContrast: platformData?.HighContrast ?? accessibilityFeatures.HighContrast,
            SupportsAnnounce: platformData?.SupportsAnnounce ?? accessibilityFeatures.SupportsAnnounce,
            ViewId: view.ViewId,
            DisplayCornerRadii: view.DisplayCornerRadii,
            DisplayFeatures: view.DisplayFeatures,
            OnOffSwitchLabels: platformData?.OnOffSwitchLabels ?? accessibilityFeatures.OnOffSwitchLabels,
            GestureSettings: view.GestureSettings,
            BoldText: platformData?.BoldText ?? accessibilityFeatures.BoldText,
            SupportsShowingSystemContextMenu: platformData?.SupportsShowingSystemContextMenu
                                              ?? platformDispatcher.SupportsShowingSystemContextMenu,
            LineHeightScaleFactorOverride: platformData?.LineHeightScaleFactorOverride
                                           ?? platformDispatcher.LineHeightScaleFactorOverride,
            LetterSpacingOverride: platformData?.LetterSpacingOverride
                                   ?? platformDispatcher.LetterSpacingOverride,
            WordSpacingOverride: platformData?.WordSpacingOverride ?? platformDispatcher.WordSpacingOverride,
            ParagraphSpacingOverride: platformData?.ParagraphSpacingOverride
                                      ?? platformDispatcher.ParagraphSpacingOverride);
    }

    /// <summary>Dart's <c>EdgeInsets.fromViewPadding</c>: physical insets to logical pixels.</summary>
    private static Thickness FromViewPadding(Thickness padding, double devicePixelRatio)
    {
        return new Thickness(
            padding.Left / devicePixelRatio,
            padding.Top / devicePixelRatio,
            padding.Right / devicePixelRatio,
            padding.Bottom / devicePixelRatio);
    }

    public static Thickness ComputePadding(Thickness viewPadding, Thickness viewInsets)
    {
        return new Thickness(
            Math.Max(0.0, viewPadding.Left - viewInsets.Left),
            Math.Max(0.0, viewPadding.Top - viewInsets.Top),
            Math.Max(0.0, viewPadding.Right - viewInsets.Right),
            Math.Max(0.0, viewPadding.Bottom - viewInsets.Bottom));
    }

    private static Thickness ShrinkToSubScreen(
        Thickness insets,
        Rect subScreen,
        double rightInset,
        double bottomInset)
    {
        return new Thickness(
            Math.Max(0.0, insets.Left - subScreen.Left),
            Math.Max(0.0, insets.Top - subScreen.Top),
            Math.Max(0.0, insets.Right - rightInset),
            Math.Max(0.0, insets.Bottom - bottomInset));
    }

    private static Thickness CopyThickness(
        Thickness source,
        double? left = null,
        double? top = null,
        double? right = null,
        double? bottom = null)
    {
        return new Thickness(
            left ?? source.Left,
            top ?? source.Top,
            right ?? source.Right,
            bottom ?? source.Bottom);
    }
}

public sealed class MediaQuery : InheritedModel<object>
{
    /// <summary>Dart's <c>_MediaQueryAspect</c>: the properties a dependent can depend on alone.</summary>
    private enum Aspect
    {
        Size,
        Width,
        Height,
        Orientation,
        DevicePixelRatio,
        TextScaleFactor,
        TextScaler,
        PlatformBrightness,
        Padding,
        ViewInsets,
        SystemGestureInsets,
        ViewPadding,
        AlwaysUse24HourFormat,
        AccessibleNavigation,
        InvertColors,
        HighContrast,
        OnOffSwitchLabels,
        DisableAnimations,
        BoldText,
        SupportsAnnounce,
        NavigationMode,
        GestureSettings,
        DisplayFeatures,
        SupportsShowingSystemContextMenu,
        LineHeightScaleFactorOverride,
        LetterSpacingOverride,
        WordSpacingOverride,
        ParagraphSpacingOverride,
        DisplayCornerRadii,
    }

    public MediaQuery(
        MediaQueryData data,
        Widget child,
        Key? key = null) : base(child, key)
    {
        Data = data;
    }

    public MediaQueryData Data { get; }

    /// <summary>
    /// Wraps <paramref name="child"/> in a <see cref="MediaQuery"/> built from <paramref name="view"/>:
    /// the view-specific data comes from the view, the platform-specific data from the surrounding
    /// <see cref="MediaQuery"/> when there is one. The injected query updates when the view's
    /// metrics change.
    /// </summary>
    /// <remarks>Flutter's <c>MediaQuery.fromView</c>.</remarks>
    public static Widget FromView(FlutterView view, Widget child, Key? key = null)
    {
        return new MediaQueryFromView(view: view, child: child, key: key);
    }

    public override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((MediaQuery)oldWidget).Data, Data);
    }

    protected override bool UpdateShouldNotifyDependent(
        InheritedModel<object> oldWidget,
        IReadOnlySet<object> dependencies)
    {
        MediaQueryData oldData = ((MediaQuery)oldWidget).Data;
        MediaQueryData data = Data;
        return dependencies.Any(aspect => aspect switch
        {
            Aspect.Size => data.Size != oldData.Size,
            Aspect.Width => data.Size.Width != oldData.Size.Width,
            Aspect.Height => data.Size.Height != oldData.Size.Height,
            Aspect.Orientation => data.Orientation != oldData.Orientation,
            Aspect.DevicePixelRatio => data.DevicePixelRatio != oldData.DevicePixelRatio,
            Aspect.TextScaleFactor => data.TextScaleFactor != oldData.TextScaleFactor,
            Aspect.TextScaler => !Equals(data.TextScaler, oldData.TextScaler),
            Aspect.PlatformBrightness => data.PlatformBrightness != oldData.PlatformBrightness,
            Aspect.Padding => data.Padding != oldData.Padding,
            Aspect.ViewInsets => data.ViewInsets != oldData.ViewInsets,
            Aspect.ViewPadding => data.ViewPadding != oldData.ViewPadding,
            Aspect.InvertColors => data.InvertColors != oldData.InvertColors,
            Aspect.HighContrast => data.HighContrast != oldData.HighContrast,
            Aspect.OnOffSwitchLabels => data.OnOffSwitchLabels != oldData.OnOffSwitchLabels,
            Aspect.DisableAnimations => data.DisableAnimations != oldData.DisableAnimations,
            Aspect.BoldText => data.BoldText != oldData.BoldText,
            Aspect.SupportsAnnounce => data.SupportsAnnounce != oldData.SupportsAnnounce,
            Aspect.NavigationMode => data.NavigationMode != oldData.NavigationMode,
            Aspect.GestureSettings => !Equals(data.GestureSettings, oldData.GestureSettings),
            Aspect.DisplayFeatures => !Equals(data.DisplayFeatures, oldData.DisplayFeatures),
            Aspect.SystemGestureInsets => data.SystemGestureInsets != oldData.SystemGestureInsets,
            Aspect.AccessibleNavigation => data.AccessibleNavigation != oldData.AccessibleNavigation,
            Aspect.AlwaysUse24HourFormat => data.AlwaysUse24HourFormat != oldData.AlwaysUse24HourFormat,
            Aspect.SupportsShowingSystemContextMenu =>
                data.SupportsShowingSystemContextMenu != oldData.SupportsShowingSystemContextMenu,
            Aspect.LineHeightScaleFactorOverride =>
                data.LineHeightScaleFactorOverride != oldData.LineHeightScaleFactorOverride,
            Aspect.LetterSpacingOverride => data.LetterSpacingOverride != oldData.LetterSpacingOverride,
            Aspect.WordSpacingOverride => data.WordSpacingOverride != oldData.WordSpacingOverride,
            Aspect.ParagraphSpacingOverride => data.ParagraphSpacingOverride != oldData.ParagraphSpacingOverride,
            Aspect.DisplayCornerRadii => !Equals(data.DisplayCornerRadii, oldData.DisplayCornerRadii),
            // C#-only: a dependency without an aspect follows the whole data.
            _ => !Equals(oldData, data),
        });
    }

    public static MediaQueryData Of(BuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (MaybeOf(context) is { } data)
        {
            return data;
        }

        // Flutter reports this through debugCheckHasMediaQuery, which names the widget and its
        // ownership chain rather than only the missing ancestor type.
        throw new FlutterError(
        [
            new ErrorSummary("No MediaQuery widget ancestor found."),
            new ErrorDescription(
                $"{Diagnostics.DescribeType(context.Widget.GetType())} widgets require a MediaQuery "
                + "widget ancestor."),
            context.DescribeWidget("The specific widget that could not find a MediaQuery ancestor was"),
            context.DescribeOwnershipChain("The ownership chain for the affected widget is"),
            new ErrorHint(
                "No MediaQuery ancestor could be found starting from the context that was passed to "
                + "MediaQuery.Of(). This can happen because the context used is not a descendant of "
                + "a View widget, which introduces a MediaQuery."),
        ]);
    }

    public static MediaQueryData? MaybeOf(BuildContext context)
    {
        return context.DependOnInheritedWidgetOfExactType<MediaQuery>()?.Data;
    }

    private static MediaQueryData? MaybeOf(BuildContext context, Aspect aspect)
    {
        return InheritedModel<object>.InheritFrom<MediaQuery>(context, aspect)?.Data;
    }

    // Dart's `_of(context, aspect)`: the aspect-scoped dependency, reporting a missing ancestor like Of.
    private static MediaQueryData Of(BuildContext context, Aspect aspect)
    {
        return MaybeOf(context, aspect) ?? Of(context);
    }

    public static Size SizeOf(BuildContext context) => Of(context, Aspect.Size).Size;

    public static Size? MaybeSizeOf(BuildContext context) => MaybeOf(context, Aspect.Size)?.Size;

    public static double WidthOf(BuildContext context) => Of(context, Aspect.Width).Size.Width;

    public static double? MaybeWidthOf(BuildContext context) => MaybeOf(context, Aspect.Width)?.Size.Width;

    public static double HeightOf(BuildContext context) => Of(context, Aspect.Height).Size.Height;

    public static double? MaybeHeightOf(BuildContext context) => MaybeOf(context, Aspect.Height)?.Size.Height;

    public static Orientation OrientationOf(BuildContext context) => Of(context, Aspect.Orientation).Orientation;

    public static Orientation? MaybeOrientationOf(BuildContext context) =>
        MaybeOf(context, Aspect.Orientation)?.Orientation;

    public static double DevicePixelRatioOf(BuildContext context) =>
        Of(context, Aspect.DevicePixelRatio).DevicePixelRatio;

    public static double? MaybeDevicePixelRatioOf(BuildContext context) =>
        MaybeOf(context, Aspect.DevicePixelRatio)?.DevicePixelRatio;

    public static double TextScaleFactorOf(BuildContext context) =>
        MaybeTextScaleFactorOf(context) ?? 1.0;

    public static double? MaybeTextScaleFactorOf(BuildContext context) =>
        MaybeOf(context, Aspect.TextScaleFactor)?.TextScaleFactor;

    public static TextScaler TextScalerOf(BuildContext context) =>
        MaybeTextScalerOf(context) ?? Painting.TextScaler.NoScaling;

    public static TextScaler? MaybeTextScalerOf(BuildContext context) =>
        MaybeOf(context, Aspect.TextScaler)?.TextScaler;

    /// <summary>
    /// Dart's <c>MediaQuery.platformBrightnessOf</c>: unlike the other <c>...Of</c> accessors this
    /// one falls back to <see cref="Brightness.Light"/> instead of throwing when no
    /// <see cref="MediaQuery"/> ancestor exists.
    /// </summary>
    public static Brightness PlatformBrightnessOf(BuildContext context) =>
        MaybePlatformBrightnessOf(context) ?? Brightness.Light;

    public static Brightness? MaybePlatformBrightnessOf(BuildContext context) =>
        MaybeOf(context, Aspect.PlatformBrightness)?.PlatformBrightness;

    public static Thickness PaddingOf(BuildContext context) => Of(context, Aspect.Padding).Padding;

    public static Thickness? MaybePaddingOf(BuildContext context) => MaybeOf(context, Aspect.Padding)?.Padding;

    public static Thickness ViewInsetsOf(BuildContext context) => Of(context, Aspect.ViewInsets).ViewInsets;

    public static Thickness? MaybeViewInsetsOf(BuildContext context) =>
        MaybeOf(context, Aspect.ViewInsets)?.ViewInsets;

    public static Thickness SystemGestureInsetsOf(BuildContext context) =>
        Of(context, Aspect.SystemGestureInsets).SystemGestureInsets;

    public static Thickness? MaybeSystemGestureInsetsOf(BuildContext context) =>
        MaybeOf(context, Aspect.SystemGestureInsets)?.SystemGestureInsets;

    public static Thickness ViewPaddingOf(BuildContext context) => Of(context, Aspect.ViewPadding).ViewPadding;

    public static Thickness? MaybeViewPaddingOf(BuildContext context) =>
        MaybeOf(context, Aspect.ViewPadding)?.ViewPadding;

    public static bool AlwaysUse24HourFormatOf(BuildContext context) =>
        Of(context, Aspect.AlwaysUse24HourFormat).AlwaysUse24HourFormat;

    public static bool? MaybeAlwaysUse24HourFormatOf(BuildContext context) =>
        MaybeOf(context, Aspect.AlwaysUse24HourFormat)?.AlwaysUse24HourFormat;

    public static bool AccessibleNavigationOf(BuildContext context) =>
        Of(context, Aspect.AccessibleNavigation).AccessibleNavigation;

    public static bool? MaybeAccessibleNavigationOf(BuildContext context) =>
        MaybeOf(context, Aspect.AccessibleNavigation)?.AccessibleNavigation;

    public static bool InvertColorsOf(BuildContext context) => Of(context, Aspect.InvertColors).InvertColors;

    public static bool? MaybeInvertColorsOf(BuildContext context) =>
        MaybeOf(context, Aspect.InvertColors)?.InvertColors;

    public static bool HighContrastOf(BuildContext context) => MaybeHighContrastOf(context) ?? false;

    public static bool? MaybeHighContrastOf(BuildContext context) =>
        MaybeOf(context, Aspect.HighContrast)?.HighContrast;

    public static bool OnOffSwitchLabelsOf(BuildContext context) => MaybeOnOffSwitchLabelsOf(context) ?? false;

    public static bool? MaybeOnOffSwitchLabelsOf(BuildContext context) =>
        MaybeOf(context, Aspect.OnOffSwitchLabels)?.OnOffSwitchLabels;

    public static bool DisableAnimationsOf(BuildContext context) =>
        Of(context, Aspect.DisableAnimations).DisableAnimations;

    public static bool? MaybeDisableAnimationsOf(BuildContext context) =>
        MaybeOf(context, Aspect.DisableAnimations)?.DisableAnimations;

    /// <summary>Dart's <c>MediaQuery.boldTextOf</c>: false when there is no ancestor.</summary>
    public static bool BoldTextOf(BuildContext context) => MaybeBoldTextOf(context) ?? false;

    /// <summary>
    /// Dart's <c>MediaQuery.maybeBoldTextOf</c>: the calling context rebuilds only when
    /// <see cref="MediaQueryData.BoldText"/> changes.
    /// </summary>
    public static bool? MaybeBoldTextOf(BuildContext context) => MaybeOf(context, Aspect.BoldText)?.BoldText;

    /// <summary>
    /// Dart's <c>MediaQuery.supportsAnnounceOf</c>: the nearest ancestor's
    /// <see cref="MediaQueryData.SupportsAnnounce"/>, or false when there is no ancestor.
    /// </summary>
    public static bool SupportsAnnounceOf(BuildContext context) => MaybeSupportsAnnounceOf(context) ?? false;

    /// <summary>
    /// Dart's <c>MediaQuery.maybeSupportsAnnounceOf</c>: the nearest ancestor's
    /// <see cref="MediaQueryData.SupportsAnnounce"/>, or null when there is no ancestor. The calling context
    /// rebuilds only when that property changes.
    /// </summary>
    public static bool? MaybeSupportsAnnounceOf(BuildContext context) =>
        MaybeOf(context, Aspect.SupportsAnnounce)?.SupportsAnnounce;

    public static NavigationMode NavigationModeOf(BuildContext context) =>
        Of(context, Aspect.NavigationMode).NavigationMode;

    public static NavigationMode? MaybeNavigationModeOf(BuildContext context) =>
        MaybeOf(context, Aspect.NavigationMode)?.NavigationMode;

    public static DeviceGestureSettings? GestureSettingsOf(BuildContext context) =>
        Of(context, Aspect.GestureSettings).GestureSettings;

    public static DeviceGestureSettings? MaybeGestureSettingsOf(BuildContext context) =>
        MaybeOf(context, Aspect.GestureSettings)?.GestureSettings;

    public static IReadOnlyList<DisplayFeature> DisplayFeaturesOf(BuildContext context) =>
        Of(context, Aspect.DisplayFeatures).DisplayFeatures ?? [];

    public static IReadOnlyList<DisplayFeature>? MaybeDisplayFeaturesOf(BuildContext context) =>
        MaybeOf(context, Aspect.DisplayFeatures)?.DisplayFeatures;

    /// <summary>
    /// Dart's <c>MediaQuery.supportsShowingSystemContextMenu</c>: the nearest ancestor's
    /// <see cref="MediaQueryData.SupportsShowingSystemContextMenu"/>; throws when there is no ancestor.
    /// The calling context rebuilds only when that property changes.
    /// </summary>
    public static bool SupportsShowingSystemContextMenu(BuildContext context) =>
        Of(context, Aspect.SupportsShowingSystemContextMenu).SupportsShowingSystemContextMenu;

    /// <summary>
    /// Dart's <c>MediaQuery.maybeSupportsShowingSystemContextMenu</c>: the nearest ancestor's
    /// <see cref="MediaQueryData.SupportsShowingSystemContextMenu"/>, or null when there is no ancestor.
    /// </summary>
    public static bool? MaybeSupportsShowingSystemContextMenu(BuildContext context) =>
        MaybeOf(context, Aspect.SupportsShowingSystemContextMenu)?.SupportsShowingSystemContextMenu;

    /// <summary>
    /// Dart's <c>MediaQuery.maybeLineHeightScaleFactorOverrideOf</c>: the nearest ancestor's
    /// <see cref="MediaQueryData.LineHeightScaleFactorOverride"/>, or null when there is no ancestor or
    /// the platform has not specified an override. The calling context rebuilds only when that
    /// property changes.
    /// </summary>
    public static double? MaybeLineHeightScaleFactorOverrideOf(BuildContext context) =>
        MaybeOf(context, Aspect.LineHeightScaleFactorOverride)?.LineHeightScaleFactorOverride;

    /// <summary>
    /// Dart's <c>MediaQuery.maybeLetterSpacingOverrideOf</c>: the nearest ancestor's
    /// <see cref="MediaQueryData.LetterSpacingOverride"/>, or null when there is no ancestor or the
    /// platform has not specified an override.
    /// </summary>
    public static double? MaybeLetterSpacingOverrideOf(BuildContext context) =>
        MaybeOf(context, Aspect.LetterSpacingOverride)?.LetterSpacingOverride;

    /// <summary>
    /// Dart's <c>MediaQuery.maybeWordSpacingOverrideOf</c>: the nearest ancestor's
    /// <see cref="MediaQueryData.WordSpacingOverride"/>, or null when there is no ancestor or the
    /// platform has not specified an override.
    /// </summary>
    public static double? MaybeWordSpacingOverrideOf(BuildContext context) =>
        MaybeOf(context, Aspect.WordSpacingOverride)?.WordSpacingOverride;

    /// <summary>
    /// Dart's <c>MediaQuery.maybeParagraphSpacingOverrideOf</c>: the nearest ancestor's
    /// <see cref="MediaQueryData.ParagraphSpacingOverride"/>, or null when there is no ancestor or the
    /// platform has not specified an override. As in Flutter, no framework text widget applies it.
    /// </summary>
    public static double? MaybeParagraphSpacingOverrideOf(BuildContext context) =>
        MaybeOf(context, Aspect.ParagraphSpacingOverride)?.ParagraphSpacingOverride;

    public static BorderRadius? DisplayCornerRadiiOf(BuildContext context) =>
        Of(context, Aspect.DisplayCornerRadii).DisplayCornerRadii;

    public static BorderRadius? MaybeDisplayCornerRadiiOf(BuildContext context) =>
        MaybeOf(context, Aspect.DisplayCornerRadii)?.DisplayCornerRadii;

    /// <summary>C#-only: the nearest ancestor's <see cref="MediaQueryData.ViewId"/>.</summary>
    public static int ViewIdOf(BuildContext context) => Of(context).ViewId;

    /// <summary>
    /// Dart's <c>MediaQuery.withClampedTextScaling</c>: wraps <paramref name="child"/> in a
    /// <see cref="MediaQuery"/> whose <see cref="MediaQueryData.TextScaler"/> is the ambient one clamped
    /// to [<paramref name="minScaleFactor"/>, <paramref name="maxScaleFactor"/>].
    /// </summary>
    /// <remarks>
    /// The returned <see cref="Builder"/> reads the ambient <see cref="MediaQuery"/> from its own context,
    /// so the call itself needs no <see cref="BuildContext"/>.
    /// </remarks>
    public static Widget WithClampedTextScaling(
        Widget child,
        Key? key = null,
        double minScaleFactor = 0.0,
        double maxScaleFactor = double.PositiveInfinity)
    {
        if (Constants.KDebugMode)
        {
            if (!(maxScaleFactor >= minScaleFactor))
            {
                throw new AssertionError("'maxScaleFactor >= minScaleFactor': is not true.");
            }

            if (double.IsNaN(maxScaleFactor))
            {
                throw new AssertionError("'!maxScaleFactor.isNaN': is not true.");
            }

            if (!double.IsFinite(minScaleFactor))
            {
                throw new AssertionError("'minScaleFactor.isFinite': is not true.");
            }

            if (!(minScaleFactor >= 0))
            {
                throw new AssertionError("'minScaleFactor >= 0': is not true.");
            }
        }

        return new Builder(
            key: key,
            builder: context =>
            {
                // Dart's `assert(debugCheckHasMediaQuery(context))`: Of reports the same error.
                MediaQueryData data = Of(context);
                return new MediaQuery(
                    data: data.CopyWith(
                        textScaler: data.TextScaler.Clamp(
                            minScaleFactor: minScaleFactor,
                            maxScaleFactor: maxScaleFactor)),
                    child: child);
            });
    }

    /// <summary>
    /// Wraps <paramref name="child"/> in a <see cref="MediaQuery"/> whose
    /// <see cref="MediaQueryData.LineHeightScaleFactorOverride"/>,
    /// <see cref="MediaQueryData.LetterSpacingOverride"/>, <see cref="MediaQueryData.WordSpacingOverride"/>
    /// and <see cref="MediaQueryData.ParagraphSpacingOverride"/> are set to the specified values; a
    /// <see langword="null"/> argument sets the corresponding override to <see langword="null"/>. The
    /// returned widget must be inserted in a widget tree below an existing <see cref="MediaQuery"/>.
    /// </summary>
    /// <remarks>Dart's <c>MediaQuery.applyTextStyleOverrides</c>.</remarks>
    public static Widget ApplyTextStyleOverrides(
        double? lineHeightScaleFactorOverride,
        double? letterSpacingOverride,
        double? wordSpacingOverride,
        double? paragraphSpacingOverride,
        Widget child,
        Key? key = null)
    {
        return new Builder(
            key: key,
            builder: context =>
            {
                // Dart's `assert(debugCheckHasMediaQuery(context))`: Of reports the same error.
                return new MediaQuery(
                    data: Of(context).ApplyTextStyleOverrides(
                        lineHeightScaleFactorOverride: lineHeightScaleFactorOverride,
                        letterSpacingOverride: letterSpacingOverride,
                        wordSpacingOverride: wordSpacingOverride,
                        paragraphSpacingOverride: paragraphSpacingOverride),
                    child: child);
            });
    }

    public static Widget WithNoTextScaling(BuildContext context, Widget child)
    {
        return new MediaQuery(
            data: Of(context).CopyWith(textScaler: Painting.TextScaler.NoScaling),
            child: child);
    }

    public static Widget RemovePadding(
        BuildContext context,
        Widget child,
        bool removeLeft = false,
        bool removeTop = false,
        bool removeRight = false,
        bool removeBottom = false)
    {
        return new MediaQuery(
            data: Of(context).RemovePadding(
                removeLeft: removeLeft,
                removeTop: removeTop,
                removeRight: removeRight,
                removeBottom: removeBottom),
            child: child);
    }

    public static Widget RemoveViewInsets(
        BuildContext context,
        Widget child,
        bool removeLeft = false,
        bool removeTop = false,
        bool removeRight = false,
        bool removeBottom = false)
    {
        return new MediaQuery(
            data: Of(context).RemoveViewInsets(
                removeLeft: removeLeft,
                removeTop: removeTop,
                removeRight: removeRight,
                removeBottom: removeBottom),
            child: child);
    }

    public static Widget RemoveViewPadding(
        BuildContext context,
        Widget child,
        bool removeLeft = false,
        bool removeTop = false,
        bool removeRight = false,
        bool removeBottom = false)
    {
        return new MediaQuery(
            data: Of(context).RemoveViewPadding(
                removeLeft: removeLeft,
                removeTop: removeTop,
                removeRight: removeRight,
                removeBottom: removeBottom),
            child: child);
    }
}

/// <summary>Flutter's <c>_MediaQueryFromView</c>: the widget behind <see cref="MediaQuery.FromView"/>.</summary>
internal sealed class MediaQueryFromView : StatefulWidget
{
    public MediaQueryFromView(FlutterView view, Widget child, bool ignoreParentData = false, Key? key = null)
        : base(key)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(child);
        View = view;
        IgnoreParentData = ignoreParentData;
        Child = child;
    }

    public FlutterView View { get; }

    public bool IgnoreParentData { get; }

    public Widget Child { get; }

    public override State CreateState() => new MediaQueryFromViewState();

    /// <summary>Flutter's <c>_MediaQueryFromViewState</c>.</summary>
    private sealed class MediaQueryFromViewState : State<MediaQueryFromView>, WidgetsBindingObserver
    {
        private MediaQueryData? _parentData;
        private MediaQueryData? _data;

        private MediaQueryFromView TypedWidget => (MediaQueryFromView)StateWidget;

        public override void InitState()
        {
            base.InitState();
            WidgetsBinding.Instance.AddObserver(this);
        }

        public override void DidChangeDependencies()
        {
            base.DidChangeDependencies();
            UpdateParentData();
            UpdateData();
            Debug.Assert(_data is not null);
        }

        public override void DidUpdateWidget(MediaQueryFromView oldWidget)
        {
            base.DidUpdateWidget(oldWidget);
            var old = (MediaQueryFromView)oldWidget;
            if (TypedWidget.IgnoreParentData != old.IgnoreParentData)
            {
                UpdateParentData();
            }

            if (_data is null || !ReferenceEquals(old.View, TypedWidget.View))
            {
                UpdateData();
            }

            Debug.Assert(_data is not null);
        }

        private void UpdateParentData()
        {
            _parentData = TypedWidget.IgnoreParentData ? null : MediaQuery.MaybeOf(Context);
            _data = null; // UpdateData must be called again after changing parent data.
        }

        private void UpdateData()
        {
            MediaQueryData newData = MediaQueryData.FromView(TypedWidget.View, platformData: _parentData);
            if (!Equals(newData, _data))
            {
                SetState(() => _data = newData);
            }
        }

        public void DidChangeAccessibilityFeatures()
        {
            // If we have a parent, it dictates our accessibility features. If we don't have a parent,
            // we get our accessibility features straight from the platform and need to update our
            // data in response to the platform changing its accessibility features setting.
            if (_parentData is null)
            {
                UpdateData();
            }
        }

        public void DidChangeMetrics()
        {
            UpdateData();
        }

        public void DidChangeTextScaleFactor()
        {
            // If we have a parent, it dictates our text scale factor. If we don't have a parent, we
            // get our text scale factor from the platform and need to update our data in response to
            // the platform changing its text scale factor setting.
            if (_parentData is null)
            {
                UpdateData();
            }
        }

        public void DidChangePlatformBrightness()
        {
            // If we have a parent, it dictates our platform brightness. If we don't have a parent, we
            // get our platform brightness from the platform and need to update our data in response
            // to the platform changing its platform brightness setting.
            if (_parentData is null)
            {
                UpdateData();
            }
        }

        public override void Dispose()
        {
            WidgetsBinding.Instance.RemoveObserver(this);
            base.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            MediaQueryData effectiveData = _data!;
            // If we get our platformBrightness from the PlatformDispatcher (i.e. we have no parentData)
            // replace it with the debugBrightnessOverride in non-release mode.
            if (!Constants.KReleaseMode
                && _parentData is null
                && FoundationDebug.DebugBrightnessOverride is { } brightnessOverride
                && effectiveData.PlatformBrightness != brightnessOverride)
            {
                effectiveData = effectiveData with { PlatformBrightness = brightnessOverride };
            }

            return new MediaQuery(data: effectiveData, child: TypedWidget.Child);
        }
    }
}

/// <summary>
/// A <see cref="TextScaler"/> that reflects the user's font scale preferences from the platform's
/// accessibility settings.
/// </summary>
/// <remarks>
/// Dart's <c>SystemTextScaler</c>: <see cref="Scale"/> asks <see cref="PlatformDispatcher.ScaleFontSize"/>,
/// which follows the platform's (possibly nonlinear) font scaling. Two instances with the same
/// <see cref="TextScaleFactor"/> are equal. Dart also lets one with a factor of 1.0 equal
/// <c>TextScaler.noScaling</c>; a C# record cannot equal a record of another type, so here it does not.
/// </remarks>
public sealed record SystemTextScaler : TextScaler
{
    private readonly PlatformDispatcher _platformDispatcher;

    internal SystemTextScaler(PlatformDispatcher platformDispatcher)
    {
        _platformDispatcher = platformDispatcher;
        TextScaleFactor = platformDispatcher.TextScaleFactor;
    }

    /// <inheritdoc/>
    public override double Scale(double fontSize) => _platformDispatcher.ScaleFontSize(fontSize);

    /// <summary>A value that represents the current user preference for the scaling factor for fonts.</summary>
    /// <remarks>
    /// This numeric value is typically used to compare <see cref="SystemTextScaler"/>s: two instances with the
    /// same factor produce the same output for the same input font size. It should not be used in
    /// arithmetic operations.
    /// </remarks>
    public override double TextScaleFactor { get; }

    /// <summary>
    /// The system's text scale factor decides equality because the <see cref="Scale"/> function's output
    /// monotonically increases with it.
    /// </summary>
    public bool Equals(SystemTextScaler? other) =>
        other is not null && TextScaleFactor == other.TextScaleFactor;

    public override int GetHashCode() => TextScaleFactor.GetHashCode();

    public override string ToString() =>
        $"SystemTextScaler ({(TextScaleFactor == 1.0
            ? "no scaling"
            : $"{TextScaleFactor.ToString(System.Globalization.CultureInfo.InvariantCulture)}x")})";
}
