using System.Diagnostics;
using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.Rendering;

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

public enum PlatformBrightness
{
    Light,
    Dark,
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
        PlatformBrightness PlatformBrightness = PlatformBrightness.Light,
        bool HighContrast = false,
        bool SupportsAnnounce = false,
        int ViewId = 0,
        BorderRadius? DisplayCornerRadii = null,
        IReadOnlyList<DisplayFeature>? DisplayFeatures = null,
        bool OnOffSwitchLabels = false,
        DeviceGestureSettings? GestureSettings = null)
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

    public PlatformBrightness PlatformBrightness { get; init; }

    public bool HighContrast { get; init; }

    public bool OnOffSwitchLabels { get; init; }

    public bool SupportsAnnounce { get; init; }

    public int ViewId { get; init; }

    public BorderRadius? DisplayCornerRadii { get; init; }

    public IReadOnlyList<DisplayFeature>? DisplayFeatures { get; init; }

    /// <summary>
    /// The gesture settings for the view this media query is for, used by gesture recognizers to
    /// size their slop thresholds. Ports Dart's `MediaQueryData.gestureSettings`.
    /// </summary>
    public DeviceGestureSettings? GestureSettings { get; init; }

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
        PlatformBrightness? platformBrightness = null,
        bool? highContrast = null,
        bool? supportsAnnounce = null,
        int? viewId = null,
        BorderRadius? displayCornerRadii = null,
        bool clearDisplayCornerRadii = false,
        IReadOnlyList<DisplayFeature>? displayFeatures = null,
        bool? onOffSwitchLabels = null,
        DeviceGestureSettings? gestureSettings = null)
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
            DisplayCornerRadii: clearDisplayCornerRadii ? null : displayCornerRadii ?? DisplayCornerRadii,
            DisplayFeatures: displayFeatures ?? DisplayFeatures,
            GestureSettings: gestureSettings ?? GestureSettings);
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
    /// Returns the data for the sub-screen described by <paramref name="subScreen"/>, with the display features
    /// removed and every inset shrunk by the amount the sub-screen already excludes.
    /// </summary>
    public MediaQueryData RemoveDisplayFeatures(Rect subScreen)
    {
        if (subScreen.Left < 0.0
            || subScreen.Top < 0.0
            || subScreen.Right > Size.Width
            || subScreen.Bottom > Size.Height)
        {
            throw new ArgumentOutOfRangeException(
                nameof(subScreen),
                "The sub-screen cannot be outside the bounds of the screen.");
        }

        if (subScreen.Size == Size && subScreen.Position == default)
        {
            return this;
        }

        double rightInset = Size.Width - subScreen.Right;
        double bottomInset = Size.Height - subScreen.Bottom;
        return CopyWith(
            size: subScreen.Size,
            padding: ShrinkToSubScreen(Padding, subScreen, rightInset, bottomInset),
            viewPadding: ShrinkToSubScreen(ViewPadding, subScreen, rightInset, bottomInset),
            viewInsets: ShrinkToSubScreen(ViewInsets, subScreen, rightInset, bottomInset),
            displayFeatures: []);
    }

    /// <summary>
    /// Creates data for a <see cref="MediaQuery"/> from <paramref name="view"/>: view-specific values
    /// (size, insets, features) come from the view, platform-specific values from
    /// <paramref name="platformData"/> when given.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>MediaQueryData.fromView</c>. With no <paramref name="platformData"/>, Flutter
    /// reads the platform values from <c>view.platformDispatcher</c>; Plumix has no platform
    /// dispatcher, so they fall back to the defaults, except <see cref="DisableAnimations"/>, which
    /// <see cref="WidgetsBinding.AccessibilityFeatures"/> carries.
    /// </remarks>
    public static MediaQueryData FromView(FlutterView view, MediaQueryData? platformData = null)
    {
        ArgumentNullException.ThrowIfNull(view);
        double devicePixelRatio = view.DevicePixelRatio;
        return new MediaQueryData(
            Size: new Size(view.PhysicalSize.Width / devicePixelRatio, view.PhysicalSize.Height / devicePixelRatio),
            DevicePixelRatio: devicePixelRatio,
            Padding: FromViewPadding(view.Padding, devicePixelRatio),
            ViewInsets: FromViewPadding(view.ViewInsets, devicePixelRatio),
            SystemGestureInsets: FromViewPadding(view.SystemGestureInsets, devicePixelRatio),
            ViewPadding: FromViewPadding(view.ViewPadding, devicePixelRatio),
            TextScaler: platformData?.TextScaler,
            AccessibleNavigation: platformData?.AccessibleNavigation ?? false,
            AlwaysUse24HourFormat: platformData?.AlwaysUse24HourFormat ?? false,
            DisableAnimations: platformData?.DisableAnimations
                               ?? WidgetsBinding.Instance.AccessibilityFeatures.DisableAnimations,
            InvertColors: platformData?.InvertColors ?? false,
            NavigationMode: platformData?.NavigationMode ?? NavigationMode.Traditional,
            PlatformBrightness: platformData?.PlatformBrightness ?? PlatformBrightness.Light,
            HighContrast: platformData?.HighContrast ?? false,
            SupportsAnnounce: platformData?.SupportsAnnounce ?? false,
            ViewId: view.ViewId,
            DisplayCornerRadii: view.DisplayCornerRadii,
            DisplayFeatures: view.DisplayFeatures,
            OnOffSwitchLabels: platformData?.OnOffSwitchLabels ?? false,
            GestureSettings: view.GestureSettings);
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
    private enum Aspect
    {
        TextScaleFactor,
        TextScaler,
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

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((MediaQuery)oldWidget).Data, Data);
    }

    protected override bool UpdateShouldNotifyDependent(
        InheritedModel<object> oldWidget,
        IReadOnlySet<object> dependencies)
    {
        MediaQueryData oldData = ((MediaQuery)oldWidget).Data;
        return dependencies.Any(aspect => aspect switch
        {
            Aspect.TextScaleFactor => oldData.TextScaleFactor != Data.TextScaleFactor,
            Aspect.TextScaler => oldData.TextScaler != Data.TextScaler,
            _ => !Equals(oldData, Data),
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
        return context.DependOnInherited<MediaQuery>()?.Data;
    }

    private static MediaQueryData? MaybeOf(BuildContext context, Aspect aspect)
    {
        return InheritedModel<object>.InheritFrom<MediaQuery>(context, aspect)?.Data;
    }

    public static Thickness PaddingOf(BuildContext context) => Of(context).Padding;

    public static Thickness? MaybePaddingOf(BuildContext context) => MaybeOf(context)?.Padding;

    public static Thickness ViewInsetsOf(BuildContext context) => Of(context).ViewInsets;

    public static Thickness? MaybeViewInsetsOf(BuildContext context) => MaybeOf(context)?.ViewInsets;

    public static Thickness ViewPaddingOf(BuildContext context) => Of(context).ViewPadding;

    public static Thickness? MaybeViewPaddingOf(BuildContext context) => MaybeOf(context)?.ViewPadding;

    public static double DevicePixelRatioOf(BuildContext context) => Of(context).DevicePixelRatio;

    public static double? MaybeDevicePixelRatioOf(BuildContext context) =>
        MaybeOf(context)?.DevicePixelRatio;

    public static Size SizeOf(BuildContext context) => Of(context).Size;

    public static Size? MaybeSizeOf(BuildContext context) => MaybeOf(context)?.Size;

    public static double WidthOf(BuildContext context) => SizeOf(context).Width;

    public static double HeightOf(BuildContext context) => SizeOf(context).Height;

    /// <summary>
    /// Dart's <c>MediaQuery.platformBrightnessOf</c>: unlike the other <c>...Of</c> accessors this
    /// one falls back to <see cref="PlatformBrightness.Light"/> instead of throwing when no
    /// <see cref="MediaQuery"/> ancestor exists.
    /// </summary>
    public static PlatformBrightness PlatformBrightnessOf(BuildContext context) =>
        MaybePlatformBrightnessOf(context) ?? PlatformBrightness.Light;

    public static PlatformBrightness? MaybePlatformBrightnessOf(BuildContext context) =>
        MaybeOf(context)?.PlatformBrightness;

    public static DeviceGestureSettings? GestureSettingsOf(BuildContext context) =>
        Of(context).GestureSettings;

    public static DeviceGestureSettings? MaybeGestureSettingsOf(BuildContext context) =>
        MaybeOf(context)?.GestureSettings;

    public static bool HighContrastOf(BuildContext context) => Of(context).HighContrast;

    public static bool? MaybeHighContrastOf(BuildContext context) => MaybeOf(context)?.HighContrast;

    public static bool OnOffSwitchLabelsOf(BuildContext context) => Of(context).OnOffSwitchLabels;

    public static bool? MaybeOnOffSwitchLabelsOf(BuildContext context) => MaybeOf(context)?.OnOffSwitchLabels;

    public static bool SupportsAnnounceOf(BuildContext context) => Of(context).SupportsAnnounce;

    public static int ViewIdOf(BuildContext context) => Of(context).ViewId;

    public static BorderRadius? DisplayCornerRadiiOf(BuildContext context) => Of(context).DisplayCornerRadii;

    public static BorderRadius? MaybeDisplayCornerRadiiOf(BuildContext context) =>
        MaybeOf(context)?.DisplayCornerRadii;

    public static double TextScaleFactorOf(BuildContext context) =>
        MaybeTextScaleFactorOf(context) ?? 1.0;

    public static double? MaybeTextScaleFactorOf(BuildContext context) =>
        MaybeOf(context, Aspect.TextScaleFactor)?.TextScaleFactor;

    public static TextScaler TextScalerOf(BuildContext context) =>
        MaybeTextScalerOf(context) ?? Painting.TextScaler.NoScaling;

    public static TextScaler? MaybeTextScalerOf(BuildContext context) =>
        MaybeOf(context, Aspect.TextScaler)?.TextScaler;

    public static bool AccessibleNavigationOf(BuildContext context) => Of(context).AccessibleNavigation;

    public static bool AlwaysUse24HourFormatOf(BuildContext context) => Of(context).AlwaysUse24HourFormat;

    public static bool? MaybeAlwaysUse24HourFormatOf(BuildContext context) => MaybeOf(context)?.AlwaysUse24HourFormat;

    public static bool DisableAnimationsOf(BuildContext context) => Of(context).DisableAnimations;

    public static bool? MaybeDisableAnimationsOf(BuildContext context) => MaybeOf(context)?.DisableAnimations;

    public static bool InvertColorsOf(BuildContext context) => Of(context).InvertColors;

    public static bool? MaybeInvertColorsOf(BuildContext context) => MaybeOf(context)?.InvertColors;

    public static NavigationMode NavigationModeOf(BuildContext context) => Of(context).NavigationMode;

    public static NavigationMode? MaybeNavigationModeOf(BuildContext context) => MaybeOf(context)?.NavigationMode;

    public static Orientation OrientationOf(BuildContext context) => Of(context).Orientation;

    public static Orientation? MaybeOrientationOf(BuildContext context) => MaybeOf(context)?.Orientation;

    public static Widget WithClampedTextScaling(
        BuildContext context,
        Widget child,
        double maxScaleFactor,
        double minScaleFactor = 0)
    {
        if (double.IsNaN(maxScaleFactor) || maxScaleFactor < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxScaleFactor));
        }

        if (!double.IsFinite(minScaleFactor) || minScaleFactor < 0 || minScaleFactor > maxScaleFactor)
        {
            throw new ArgumentOutOfRangeException(nameof(minScaleFactor));
        }
        MediaQueryData data = Of(context);
        return new MediaQuery(
            data.CopyWith(textScaler: data.TextScaler.Clamp(minScaleFactor, maxScaleFactor)),
            child);
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
    private sealed class MediaQueryFromViewState : State, WidgetsBindingObserver
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

        public override void DidUpdateWidget(StatefulWidget oldWidget)
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
            return new MediaQuery(data: _data!, child: TypedWidget.Child);
        }
    }
}
