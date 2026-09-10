using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Physics;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/menu_anchor.dart

/// <summary>Dart parity source: <c>CupertinoMenuEntry</c>.</summary>
public interface CupertinoMenuEntry
{
    bool HasLeading(BuildContext context);

    bool IsDivider { get; }
}

public delegate void CupertinoMenuAnimationStatusChangedCallback(AnimationStatus status);

/// <summary>
/// The file-level constants and helper functions of <c>menu_anchor.dart</c>; C# has no top-level
/// functions, so Dart's privates live here.
/// </summary>
internal static class CupertinoMenuMetrics
{
    internal const string KBodyFont = "CupertinoSystemText";

    internal const string KDisplayFont = "CupertinoSystemDisplay";

    /// <summary>The unscaled iOS body font size that every dynamic-type step is measured against.</summary>
    internal const double KCupertinoMobileBaseFontSize = 17.0;

    internal const double KMinimumNormalizedLargeTextScale = 11.0;

    internal const double KMinimumTextScaleFactor = 1.0 - (3.0 / KCupertinoMobileBaseFontSize);

    internal const double KMaximumTextScaleFactor = 1.0 + (36.0 / KCupertinoMobileBaseFontSize);

    /// <summary>Dart parity source: <c>_isCupertino</c>.</summary>
    internal static bool IsCupertino => PlatformDefaults.TargetPlatform is
        TargetPlatform.IOS or TargetPlatform.MacOS;

    /// <summary>Dart parity source: <c>_normalizeTextScale</c>.</summary>
    internal static double NormalizeTextScale(TextScaler textScaler)
    {
        if (textScaler == TextScaler.NoScaling)
        {
            return 0.0;
        }

        return textScaler.Scale(KCupertinoMobileBaseFontSize) - KCupertinoMobileBaseFontSize;
    }

    /// <summary>Dart parity source: <c>_largeTextModeEnabled</c>.</summary>
    internal static bool LargeTextModeEnabled(BuildContext context)
    {
        TextScaler? textScaler = MediaQuery.MaybeTextScalerOf(context);
        if (textScaler is null)
        {
            return false;
        }

        return NormalizeTextScale(textScaler) >= KMinimumNormalizedLargeTextScale;
    }

    /// <summary>Dart parity source: <c>_computeSquaredDistanceToRect</c>.</summary>
    internal static double ComputeSquaredDistanceToRect(Point point, Rect rect)
    {
        double dx = point.X - Math.Clamp(point.X, rect.Left, rect.Right);
        double dy = point.Y - Math.Clamp(point.Y, rect.Top, rect.Bottom);
        return (dx * dx) + (dy * dy);
    }

    /// <summary>Dart parity source: <c>_roundToDivisible</c>.</summary>
    internal static double RoundToDivisible(double value, double to)
    {
        if (to == 0.0)
        {
            return value;
        }

        return Math.Round(value / to, MidpointRounding.AwayFromZero) * to;
    }
}

/// <summary>Dart parity source: <c>_CupertinoMenuWidth</c>.</summary>
internal enum CupertinoMenuWidth
{
    IPadOS,
    IPadOSAccessible,
    IOS,
    IOSAccessible,
}

/// <summary>
/// The members Dart declares on the <c>_CupertinoMenuWidth</c> enhanced enum; a C# enum carries no
/// fields, so they live on this companion.
/// </summary>
internal static class CupertinoMenuWidths
{
    internal const double KTabletWidthThreshold = 768.0;

    internal static double Points(this CupertinoMenuWidth width) => width switch
    {
        CupertinoMenuWidth.IPadOS => 262.0,
        CupertinoMenuWidth.IPadOSAccessible => 343.0,
        CupertinoMenuWidth.IOS => 250.0,
        _ => 370.0,
    };

    internal static CupertinoMenuWidth FromScreenWidth(double screenWidth, bool isLargeTextModeEnabled)
    {
        bool isMobile = screenWidth < KTabletWidthThreshold;
        return (isMobile, isLargeTextModeEnabled) switch
        {
            (false, false) => CupertinoMenuWidth.IPadOS,
            (false, true) => CupertinoMenuWidth.IPadOSAccessible,
            (true, false) => CupertinoMenuWidth.IOS,
            _ => CupertinoMenuWidth.IOSAccessible,
        };
    }
}

/// <summary>
/// Dart parity source: <c>_DynamicTypeStyle</c>. An enhanced Dart enum whose members carry the twelve
/// iOS dynamic-type steps; C# enums carry no fields, so the members are static instances.
/// </summary>
internal sealed class CupertinoDynamicTypeStyle
{
    private const int KScaleCount = 12;

    private static readonly int[] NormalizedBodyScales;

    static CupertinoDynamicTypeStyle()
    {
        Body = new CupertinoDynamicTypeStyle(
        [
            Style(14.0, 19.0 / 14.0, -0.15, display: false),
            Style(15.0, 20.0 / 15.0, -0.23, display: false),
            Style(16.0, 21.0 / 16.0, -0.31, display: false),
            Style(17.0, 22.0 / 17.0, -0.43, display: false),
            Style(19.0, 24.0 / 19.0, -0.44, display: false),
            Style(21.0, 26.0 / 21.0, -0.36, display: false),
            Style(23.0, 29.0 / 23.0, -0.10, display: true),
            Style(28.0, 34.0 / 28.0, 0.38, display: true),
            Style(33.0, 40.0 / 33.0, 0.40, display: true),
            Style(40.0, 48.0 / 40.0, 0.37, display: true),
            Style(47.0, 56.0 / 47.0, 0.37, display: true),
            Style(53.0, 62.0 / 53.0, 0.31, display: true),
        ]);
        Subhead = new CupertinoDynamicTypeStyle(
        [
            Style(12.0, 16.0 / 12.0, 0.0, display: false),
            Style(13.0, 18.0 / 13.0, -0.08, display: false),
            Style(14.0, 19.0 / 14.0, -0.15, display: false),
            Style(15.0, 20.0 / 15.0, -0.23, display: false),
            Style(17.0, 22.0 / 17.0, -0.43, display: false),
            Style(19.0, 24.0 / 19.0, -0.45, display: false),
            Style(21.0, 28.0 / 21.0, -0.36, display: false),
            Style(25.0, 31.0 / 25.0, 0.15, display: true),
            Style(30.0, 37.0 / 30.0, 0.40, display: true),
            Style(36.0, 43.0 / 36.0, 0.37, display: true),
            Style(42.0, 50.0 / 42.0, 0.37, display: true),
            Style(49.0, 58.0 / 49.0, 0.33, display: true),
        ]);
        NormalizedBodyScales = new int[KScaleCount];
        for (int index = 0; index < KScaleCount; index++)
        {
            NormalizedBodyScales[index] =
                (int)(Body.Styles[index].FontSize!.Value - CupertinoMenuMetrics.KCupertinoMobileBaseFontSize);
        }
    }

    private CupertinoDynamicTypeStyle(IReadOnlyList<TextStyle> styles)
    {
        Styles = styles;
    }

    internal static CupertinoDynamicTypeStyle Body { get; }

    internal static CupertinoDynamicTypeStyle Subhead { get; }

    internal IReadOnlyList<TextStyle> Styles { get; }

    internal TextStyle ResolveTextStyle(TextScaler textScaler)
    {
        double units = CupertinoMenuMetrics.NormalizeTextScale(textScaler);
        for (int index = 0; index < Styles.Count; index++)
        {
            int bodyUnits = NormalizedBodyScales[index];
            if (units > bodyUnits)
            {
                continue;
            }

            if (units == bodyUnits)
            {
                return Styles[index];
            }

            if (index == 0)
            {
                return Styles[0];
            }

            return TextStyle.Lerp(
                Styles[index - 1],
                Styles[index],
                InterpolateUnits(units, NormalizedBodyScales[index - 1], bodyUnits))!;
        }

        return Styles[^1];
    }

    private static double InterpolateUnits(double units, int minimum, int maximum) =>
        (units - minimum) / (maximum - minimum);

    private static TextStyle Style(double size, double height, double spacing, bool display) => new(
        FontFamily: new FontFamily(display
            ? CupertinoMenuMetrics.KDisplayFont
            : CupertinoMenuMetrics.KBodyFont),
        FontSize: size,
        Height: height,
        LetterSpacing: spacing);
}

/// <summary>Dart parity source: <c>_AnchorScope</c>.</summary>
internal sealed class CupertinoMenuAnchorScope : InheritedWidget
{
    public CupertinoMenuAnchorScope(bool hasLeading, Widget child) : base(child)
    {
        HasLeading = hasLeading;
    }

    public bool HasLeading { get; }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        HasLeading != ((CupertinoMenuAnchorScope)oldWidget).HasLeading;
}

/// <summary>Dart parity source: <c>_AnimationProduct</c>.</summary>
internal sealed class CupertinoMenuAnimationProduct : CompoundAnimation<double>
{
    public CupertinoMenuAnimationProduct(Animation<double> first, Animation<double> next)
        : base(first, next)
    {
    }

    public override double Value => First.Value * Next.Value;
}

/// <summary>Dart parity source: <c>_ClampTween</c>.</summary>
internal sealed class CupertinoMenuClampTween : Animatable<double>
{
    public CupertinoMenuClampTween(double begin, double end)
    {
        Begin = begin;
        End = end;
    }

    public double Begin { get; }

    public double End { get; }

    public override double Transform(double t)
    {
        if (t < Begin)
        {
            return Begin;
        }

        return t > End ? End : t;
    }
}

/// <summary>Dart parity source: <c>_FocusUpIntent</c>.</summary>
internal sealed class CupertinoMenuFocusUpIntent : DirectionalFocusIntent
{
    public CupertinoMenuFocusUpIntent() : base(TraversalDirection.Up)
    {
    }
}

/// <summary>Dart parity source: <c>_FocusDownIntent</c>.</summary>
internal sealed class CupertinoMenuFocusDownIntent : DirectionalFocusIntent
{
    public CupertinoMenuFocusDownIntent() : base(TraversalDirection.Down)
    {
    }
}

/// <summary>Dart parity source: <c>_FocusFirstIntent</c>.</summary>
internal sealed class CupertinoMenuFocusFirstIntent : Intent;

/// <summary>Dart parity source: <c>_FocusLastIntent</c>.</summary>
internal sealed class CupertinoMenuFocusLastIntent : Intent;

/// <summary>Dart parity source: <c>_FocusUpAction</c>.</summary>
internal sealed class CupertinoMenuFocusUpAction : ContextAction<DirectionalFocusIntent>
{
    public override object? Invoke(DirectionalFocusIntent intent, BuildContext? context)
    {
        ArgumentNullException.ThrowIfNull(intent);
        FocusTraversalPolicy policy = FocusTraversalGroup.MaybeOf(context!)
                                      ?? new ReadingOrderTraversalPolicy();
        FocusNode primary = FocusManager.Instance.PrimaryFocus!;
        if (CupertinoMenuMetrics.IsCupertino && !OperatingSystem.IsBrowser())
        {
            // Don't wrap on iOS or macOS.
            policy.InDirection(primary, intent.Direction);
            return null;
        }

        FocusNode? firstFocus = policy.FindFirstFocus(primary, ignoreCurrentFocus: true);
        FocusNode lastFocus = policy.FindLastFocus(primary, ignoreCurrentFocus: true);
        if (lastFocus.Context is not null
            && (ReferenceEquals(primary, lastFocus.EnclosingScope) || ReferenceEquals(primary, firstFocus)))
        {
            policy.RequestFocusCallback(lastFocus, ScrollPositionAlignmentPolicy.Explicit, null, null, null);
            return null;
        }

        policy.InDirection(primary, intent.Direction);
        return null;
    }
}

/// <summary>Dart parity source: <c>_FocusDownAction</c>.</summary>
internal sealed class CupertinoMenuFocusDownAction : ContextAction<DirectionalFocusIntent>
{
    public override object? Invoke(DirectionalFocusIntent intent, BuildContext? context)
    {
        ArgumentNullException.ThrowIfNull(intent);
        FocusTraversalPolicy policy = FocusTraversalGroup.MaybeOf(context!)
                                      ?? new ReadingOrderTraversalPolicy();
        FocusNode primary = FocusManager.Instance.PrimaryFocus!;
        if (CupertinoMenuMetrics.IsCupertino && !OperatingSystem.IsBrowser())
        {
            policy.InDirection(primary, intent.Direction);
            return null;
        }

        FocusNode? firstFocus = policy.FindFirstFocus(primary, ignoreCurrentFocus: true);
        FocusNode lastFocus = policy.FindLastFocus(primary, ignoreCurrentFocus: true);
        if (firstFocus?.Context is not null
            && (ReferenceEquals(primary, firstFocus.EnclosingScope) || ReferenceEquals(primary, lastFocus)))
        {
            policy.RequestFocusCallback(firstFocus, ScrollPositionAlignmentPolicy.Explicit, null, null, null);
            return null;
        }

        policy.InDirection(primary, intent.Direction);
        return null;
    }
}

/// <summary>Dart parity source: <c>_FocusFirstAction</c>.</summary>
internal sealed class CupertinoMenuFocusFirstAction : ContextAction<CupertinoMenuFocusFirstIntent>
{
    public override object? Invoke(CupertinoMenuFocusFirstIntent intent, BuildContext? context)
    {
        FocusTraversalPolicy policy = FocusTraversalGroup.MaybeOf(context!)
                                      ?? new ReadingOrderTraversalPolicy();
        FocusNode? firstFocus = policy.FindFirstFocus(
            FocusManager.Instance.PrimaryFocus!,
            ignoreCurrentFocus: true);
        if (firstFocus?.Context is null)
        {
            return null;
        }

        policy.RequestFocusCallback(firstFocus, ScrollPositionAlignmentPolicy.Explicit, null, null, null);
        return null;
    }
}

/// <summary>Dart parity source: <c>_FocusLastAction</c>.</summary>
internal sealed class CupertinoMenuFocusLastAction : ContextAction<CupertinoMenuFocusLastIntent>
{
    public override object? Invoke(CupertinoMenuFocusLastIntent intent, BuildContext? context)
    {
        FocusTraversalPolicy policy = FocusTraversalGroup.MaybeOf(context!)
                                      ?? new ReadingOrderTraversalPolicy();
        FocusNode lastFocus = policy.FindLastFocus(
            FocusManager.Instance.PrimaryFocus!,
            ignoreCurrentFocus: true);
        if (lastFocus.Context is null)
        {
            return null;
        }

        policy.RequestFocusCallback(lastFocus, ScrollPositionAlignmentPolicy.Explicit, null, null, null);
        return null;
    }
}

public sealed class CupertinoMenuAnchor : StatefulWidget
{
    public CupertinoMenuAnchor(
        IReadOnlyList<Widget> menuChildren,
        MenuController? controller = null,
        Action? onOpen = null,
        Action? onClose = null,
        CupertinoMenuAnimationStatusChangedCallback? onAnimationStatusChanged = null,
        BoxConstraints? constraints = null,
        bool constrainCrossAxis = false,
        bool consumeOutsideTaps = false,
        bool enableSwipe = true,
        bool enableLongPressToOpen = false,
        bool useRootOverlay = false,
        EdgeInsetsGeometry? overlayPadding = null,
        RawMenuAnchorChildBuilder? builder = null,
        Widget? child = null,
        FocusNode? childFocusNode = null,
        Key? key = null) : base(key)
    {
        if (enableLongPressToOpen && !enableSwipe)
        {
            throw new ArgumentException(
                "enableLongPressToOpen cannot be true if enableSwipe is false.",
                nameof(enableLongPressToOpen));
        }

        MenuChildren = menuChildren ?? throw new ArgumentNullException(nameof(menuChildren));
        Controller = controller;
        OnOpen = onOpen;
        OnClose = onClose;
        OnAnimationStatusChanged = onAnimationStatusChanged;
        Constraints = constraints;
        ConstrainCrossAxis = constrainCrossAxis;
        ConsumeOutsideTaps = consumeOutsideTaps;
        EnableSwipe = enableSwipe;
        EnableLongPressToOpen = enableLongPressToOpen;
        UseRootOverlay = useRootOverlay;
        OverlayPadding = overlayPadding ?? EdgeInsetsGeometry.All(8.0);
        Builder = builder;
        Child = child;
        ChildFocusNode = childFocusNode;
    }

    public IReadOnlyList<Widget> MenuChildren { get; }

    public MenuController? Controller { get; }

    public Action? OnOpen { get; }

    public Action? OnClose { get; }

    public CupertinoMenuAnimationStatusChangedCallback? OnAnimationStatusChanged { get; }

    public BoxConstraints? Constraints { get; }

    public bool ConstrainCrossAxis { get; }

    public bool ConsumeOutsideTaps { get; }

    public bool EnableSwipe { get; }

    public bool EnableLongPressToOpen { get; }

    public bool UseRootOverlay { get; }

    public EdgeInsetsGeometry OverlayPadding { get; }

    public RawMenuAnchorChildBuilder? Builder { get; }

    public Widget? Child { get; }

    public FocusNode? ChildFocusNode { get; }

    public static bool? MaybeHasLeadingOf(BuildContext context) =>
        context.DependOnInherited<CupertinoMenuAnchorScope>()?.HasLeading;

    public override State CreateState() => new CupertinoMenuAnchorState();
}

public sealed class CupertinoMenuAnchorState : State
{
    private static readonly TimeSpan KLongPressToOpenDuration = TimeSpan.FromMilliseconds(400);

    private static readonly Tolerance KSpringTolerance = new(velocity: 0.1);

    internal static SpringDescription ForwardSpring { get; } = SpringDescription.WithDurationAndBounce(
        TimeSpan.FromMilliseconds(337),
        bounce: 0.2);

    internal static SpringDescription ReverseSpring { get; } = SpringDescription.WithDurationAndBounce(
        TimeSpan.FromMilliseconds(409));

    private readonly FocusScopeNode _menuScopeNode = new(debugLabel: "Menu Scope");
    private readonly ValueNotifier<double> _swipeDistanceNotifier = new(0.0);
    private AnimationController _animationController = null!;
    private bool? _hasLeadingWidget;
    private MenuController? _internalMenuController;
    private AnimationStatus _animationStatus = AnimationStatus.Dismissed;

    private CupertinoMenuAnchor Current => (CupertinoMenuAnchor)StateWidget;

    internal MenuController MenuController => Current.Controller ?? _internalMenuController!;

    internal bool IsOpenOrOpening => _animationStatus.IsForwardOrCompleted();

    internal bool EnableSwipe => Current.EnableSwipe && _animationStatus switch
    {
        AnimationStatus.Forward or AnimationStatus.Completed or AnimationStatus.Dismissed => true,
        _ => false,
    };

    public override void InitState()
    {
        if (Current.Controller is null)
        {
            _internalMenuController = new MenuController();
        }

        _animationController = AnimationController.Unbounded(vsync: this);
        _animationController.AddStatusListener(HandleAnimationStatusChange);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var previous = (CupertinoMenuAnchor)oldWidget;
        if (!ReferenceEquals(previous.Controller, Current.Controller))
        {
            _internalMenuController = Current.Controller is null ? new MenuController() : null;
        }

        if (!ReferenceEquals(previous.MenuChildren, Current.MenuChildren))
        {
            _hasLeadingWidget = ResolveHasLeading();
        }
    }

    public override void DidChangeDependencies()
    {
        _hasLeadingWidget ??= ResolveHasLeading();
    }

    public override Widget Build(BuildContext context)
    {
        return new CupertinoMenuSwipeRegion(
            enabled: EnableSwipe,
            onDistanceChanged: HandleSwipeDistanceChange,
            child: new CupertinoMenuAnchorScope(
                hasLeading: _hasLeadingWidget!.Value,
                child: new RawMenuAnchor(
                    controller: MenuController,
                    overlayBuilder: BuildMenuOverlay,
                    onCloseRequested: HandleCloseRequested,
                    onOpenRequested: HandleOpenRequested,
                    useRootOverlay: Current.UseRootOverlay,
                    childFocusNode: Current.ChildFocusNode,
                    consumeOutsideTaps: Current.ConsumeOutsideTaps,
                    onClose: Current.OnClose,
                    onOpen: Current.OnOpen,
                    builder: BuildChild)));
    }

    public override void Dispose()
    {
        _menuScopeNode.Dispose();
        _animationController.Stop();
        _animationController.Dispose();
        _internalMenuController = null;
        _swipeDistanceNotifier.Dispose();

        base.Dispose();
    }

    private bool ResolveHasLeading() => Current.MenuChildren.Any(
        element => element is CupertinoMenuEntry entry && entry.HasLeading(Context));

    private void HandleAnimationStatusChange(AnimationStatus status)
    {
        SetState(() => _animationStatus = status);
        Current.OnAnimationStatusChanged?.Invoke(status);
    }

    private void HandleSwipeDistanceChange(double distance)
    {
        if (!MenuController.IsOpen)
        {
            return;
        }

        _swipeDistanceNotifier.Value = distance;
    }

    private void HandleAnchorSwipeStart()
    {
        if (IsOpenOrOpening || !Current.EnableLongPressToOpen)
        {
            return;
        }

        MenuController.Open();
    }

    private void HandleOpenRequested(Vector? position, Action showOverlay)
    {
        showOverlay();
        if (_animationStatus is AnimationStatus.Completed or AnimationStatus.Forward)
        {
            return;
        }

        _animationController.AnimateWith(new SpringSimulation(
            ForwardSpring,
            _animationController.Value,
            1.0,
            0.5));
        (FocusScope.MaybeOf(Context) ?? FocusManager.Instance.RootScope).SetFirstFocus(_menuScopeNode);
    }

    private void HandleCloseRequested(Action hideMenu)
    {
        if (_animationStatus is AnimationStatus.Reverse or AnimationStatus.Dismissed)
        {
            return;
        }

        var spring = new SpringSimulation(
            ReverseSpring,
            _animationController.Value,
            0.0,
            0.0,
            tolerance: KSpringTolerance);
        _animationController
            .AnimateBackWith(new ClampedSimulation(spring, xMin: 0.0, xMax: 1.0))
            .WhenComplete(hideMenu);
    }

    private Widget BuildMenuOverlay(BuildContext childContext, RawMenuOverlayInfo info)
    {
        bool excluded = !IsOpenOrOpening;
        return new ExcludeSemantics(
            excluding: excluded,
            child: new IgnorePointer(
                ignoring: excluded,
                child: new ExcludeFocus(
                    excluding: excluded,
                    child: new CupertinoMenuOverlay(
                        children: Current.MenuChildren,
                        focusScopeNode: _menuScopeNode,
                        consumeOutsideTaps: Current.ConsumeOutsideTaps,
                        constrainCrossAxis: Current.ConstrainCrossAxis,
                        constraints: Current.Constraints,
                        overlaySize: info.OverlaySize,
                        overlayPadding: Current.OverlayPadding,
                        anchorRect: info.AnchorRect,
                        anchorPosition: info.Position,
                        tapRegionGroupId: info.TapRegionGroupId,
                        visibilityAnimation: _animationController.View,
                        swipeDistanceListenable: _swipeDistanceNotifier))));
    }

    private Widget BuildChild(BuildContext context, MenuController controller, Widget? child)
    {
        Widget anchor = Current.Builder?.Invoke(context, MenuController, Current.Child)
                        ?? Current.Child
                        ?? SizedBox.Shrink();
        if (!Current.EnableLongPressToOpen || !EnableSwipe)
        {
            return anchor;
        }

        return new CupertinoMenuSwipeSurface(
            onStart: HandleAnchorSwipeStart,
            delay: KLongPressToOpenDuration,
            child: anchor);
    }
}
