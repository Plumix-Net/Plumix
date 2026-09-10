using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/menu_anchor.dart

/// <summary>Dart parity source: <c>_CupertinoMenuImplicitDivider</c>.</summary>
internal sealed class CupertinoMenuImplicitDivider : StatelessWidget
{
    internal static CupertinoDynamicColor KOverlayColor { get; } = CupertinoDynamicColor.WithBrightness(
        Color.FromArgb(77, 140, 140, 140),
        Color.FromArgb(64, 255, 255, 255));

    internal static CupertinoDynamicColor KDividerColor { get; } = CupertinoDynamicColor.WithBrightness(
        Color.FromArgb(64, 0, 0, 0),
        Color.FromArgb(64, 255, 255, 255));

    public CupertinoMenuImplicitDivider(Key? key = null) : base(key)
    {
    }

    public override Widget Build(BuildContext context)
    {
        double pixelRatio = MediaQuery.MaybeOf(context)?.DevicePixelRatio ?? 1.0;
        return new CustomPaint(
            size: new Size(double.PositiveInfinity, 1.0 / pixelRatio),
            painter: new CupertinoMenuDividerPainter(
                color: KDividerColor.ResolveFrom(context),
                overlayColor: KOverlayColor.ResolveFrom(context),
                antiAlias: pixelRatio < 1.0));
    }
}

/// <summary>Dart parity source: <c>_CupertinoDividerPainter</c>.</summary>
internal sealed class CupertinoMenuDividerPainter : CustomPainter
{
    public CupertinoMenuDividerPainter(Color color, Color overlayColor, bool antiAlias)
    {
        Color = color;
        OverlayColor = overlayColor;
        AntiAlias = antiAlias;
    }

    public Color Color { get; }

    public Color OverlayColor { get; }

    public bool AntiAlias { get; }

    public override void Paint(PaintingContext context, Size size)
    {
        var start = new Point(0.0, size.Height / 2.0);
        var end = new Point(size.Width, size.Height / 2.0);

        // Dart draws `overlayColor` first with `BlendMode.overlay` (skipped on web, where the blend
        // mode is unsupported) and `color` second with the default `srcOver`. Avalonia can blend only
        // bitmap draws, never a stroked geometry, so only the `srcOver` pass is drawn here; see
        // docs/ai/DIVERGENCES.md.
        var colorPen = new Pen(new SolidColorBrush(Color), 0.0);
        context.Canvas.DrawLine(colorPen, start, end);
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate) =>
        oldDelegate is not CupertinoMenuDividerPainter old
        || old.Color != Color
        || old.OverlayColor != OverlayColor
        || old.AntiAlias != AntiAlias;
}

/// <summary>Dart parity source: <c>CupertinoMenuDivider</c>.</summary>
public sealed class CupertinoMenuDivider : StatelessWidget, CupertinoMenuEntry
{
    private const double KDividerHeight = 8.0;

    public static CupertinoDynamicColor KDefaultColor { get; } = CupertinoDynamicColor.WithBrightness(
        Avalonia.Media.Color.FromArgb(20, 0, 0, 0),
        Avalonia.Media.Color.FromArgb(41, 0, 0, 0));

    public CupertinoMenuDivider(CupertinoDynamicColor? color = null, Key? key = null) : base(key)
    {
        Color = color ?? KDefaultColor;
    }

    public CupertinoDynamicColor Color { get; }

    public bool IsDivider => true;

    public bool HasLeading(BuildContext context) => false;

    public override Widget Build(BuildContext context) => new ColoredBox(
        Color.ResolveFrom(context),
        child: new SizedBox(height: KDividerHeight, width: double.PositiveInfinity));
}

/// <summary>Dart parity source: <c>CupertinoMenuItem</c>.</summary>
public sealed class CupertinoMenuItem : StatelessWidget, CupertinoMenuEntry
{
    private const int KDefaultMaxLines = 2;

    private const int KDefaultLargeTextModeMaxLines = 100;

    private static readonly TextStyle KLeadingDefaultTextStyle =
        new(FontSize: 15.0, FontWeight: FontWeight.DemiBold);

    private static readonly IconThemeData KLeadingDefaultIconTheme =
        new(Size: 15.0, Weight: 600.0, ApplyTextScaling: true);

    private static readonly TextStyle KTrailingDefaultTextStyle = new(FontSize: 21.0);

    private static readonly IconThemeData KTrailingDefaultIconTheme =
        new(Size: 21.0, ApplyTextScaling: true);

    private static readonly CupertinoDynamicColor KDefaultTextColor =
        CupertinoDynamicColor.WithBrightness(
            Color.FromArgb(245, 0, 0, 0),
            Color.FromArgb(245, 255, 255, 255));

    private static readonly CupertinoDynamicColor KDefaultSubtitleTextColor =
        CupertinoDynamicColor.WithBrightness(
            Color.FromArgb(140, 0, 0, 0),
            Color.FromArgb(102, 255, 255, 255));

    private static readonly WidgetStateProperty<MouseCursor> KDefaultCursor =
        WidgetStateProperty<MouseCursor>.ResolveWith(states =>
            !states.Contains(WidgetState.Disabled) && OperatingSystem.IsBrowser()
                ? SystemMouseCursors.Click
                : Plumix.UI.MouseCursor.Defer);

    /// <summary>
    /// Dart parity source: <c>CupertinoMenuItem.kDefaultDecoration</c>, resolved for the light
    /// brightness.
    /// </summary>
    /// <remarks>
    /// Dart stores an unresolved <c>CupertinoDynamicColor</c> in the <c>BoxDecoration</c> and resolves
    /// it against the context in <c>_buildStatefulAppearance</c>. Plumix's <c>BoxDecoration.Color</c>
    /// is an Avalonia <c>Color</c>, which cannot carry a dynamic color, so the brightness switch moves
    /// from the color to the map (see <see cref="KDefaultDarkDecoration"/> and
    /// <c>docs/ai/DIVERGENCES.md</c>).
    /// </remarks>
    public static WidgetStateProperty<BoxDecoration> KDefaultDecoration { get; } =
        BuildDefaultDecoration(dark: false);

    internal static WidgetStateProperty<BoxDecoration> KDefaultDarkDecoration { get; } =
        BuildDefaultDecoration(dark: true);

    public CupertinoMenuItem(
        Widget child,
        Widget? subtitle = null,
        Widget? leading = null,
        double? leadingWidth = null,
        AlignmentGeometry? leadingMidpointAlignment = null,
        Widget? trailing = null,
        double? trailingWidth = null,
        AlignmentGeometry? trailingMidpointAlignment = null,
        EdgeInsetsGeometry? padding = null,
        BoxConstraints? constraints = null,
        bool autofocus = false,
        FocusNode? focusNode = null,
        Action<bool>? onFocusChange = null,
        Action<bool>? onHover = null,
        Action? onPressed = null,
        WidgetStateProperty<BoxDecoration>? decoration = null,
        WidgetStateProperty<MouseCursor>? mouseCursor = null,
        HitTestBehavior behavior = HitTestBehavior.Opaque,
        bool requestCloseOnActivate = true,
        bool requestFocusOnHover = true,
        bool isDestructiveAction = false,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Subtitle = subtitle;
        Leading = leading;
        LeadingWidth = leadingWidth;
        LeadingMidpointAlignment = leadingMidpointAlignment;
        Trailing = trailing;
        TrailingWidth = trailingWidth;
        TrailingMidpointAlignment = trailingMidpointAlignment;
        Padding = padding;
        Constraints = constraints;
        Autofocus = autofocus;
        FocusNode = focusNode;
        OnFocusChange = onFocusChange;
        OnHover = onHover;
        OnPressed = onPressed;
        Decoration = decoration;
        MouseCursor = mouseCursor;
        Behavior = behavior;
        RequestCloseOnActivate = requestCloseOnActivate;
        RequestFocusOnHover = requestFocusOnHover;
        IsDestructiveAction = isDestructiveAction;
    }

    public Widget Child { get; }

    public Widget? Subtitle { get; }

    public Widget? Leading { get; }

    public double? LeadingWidth { get; }

    public AlignmentGeometry? LeadingMidpointAlignment { get; }

    public Widget? Trailing { get; }

    public double? TrailingWidth { get; }

    public AlignmentGeometry? TrailingMidpointAlignment { get; }

    public EdgeInsetsGeometry? Padding { get; }

    public BoxConstraints? Constraints { get; }

    public bool Autofocus { get; }

    public FocusNode? FocusNode { get; }

    public Action<bool>? OnFocusChange { get; }

    public Action<bool>? OnHover { get; }

    public Action? OnPressed { get; }

    public WidgetStateProperty<BoxDecoration>? Decoration { get; }

    public WidgetStateProperty<MouseCursor>? MouseCursor { get; }

    public HitTestBehavior Behavior { get; }

    public bool RequestCloseOnActivate { get; }

    public bool RequestFocusOnHover { get; }

    public bool IsDestructiveAction { get; }

    public bool IsDivider => false;

    public bool HasLeading(BuildContext context) => Leading is not null;

    public override Widget Build(BuildContext context)
    {
        TextScaler textScaler = MediaQuery.MaybeTextScalerOf(context) ?? TextScaler.NoScaling;
        TextStyle defaultTextStyle = ResolveDefaultTextStyle(context, textScaler);
        bool isLargeTextModeEnabled = CupertinoMenuMetrics.LargeTextModeEnabled(context);
        Widget? leadingWidget = Leading is null
            ? null
            : DefaultTextStyle.Merge(
                child: IconTheme.Merge(KLeadingDefaultIconTheme, Leading),
                style: KLeadingDefaultTextStyle);
        Widget? trailingWidget = Trailing is null || isLargeTextModeEnabled
            ? null
            : DefaultTextStyle.Merge(
                child: IconTheme.Merge(KTrailingDefaultIconTheme, Trailing),
                style: KTrailingDefaultTextStyle);

        Widget label = new CupertinoMenuItemLabel(
            child: DefaultTextStyle.Merge(child: Child, style: defaultTextStyle),
            subtitle: Subtitle is null
                ? null
                : DefaultTextStyle.Merge(
                    child: Subtitle,
                    style: ResolveDefaultSubtitleStyle(context, textScaler)),
            leading: leadingWidget,
            trailing: trailingWidget,
            leadingWidth: LeadingWidth,
            trailingWidth: TrailingWidth,
            leadingMidpointAlignment: LeadingMidpointAlignment,
            trailingMidpointAlignment: TrailingMidpointAlignment,
            padding: Padding,
            constraints: Constraints);
        label = IconTheme.Merge(new IconThemeData(Color: defaultTextStyle.Color), label);
        label = DefaultTextStyle.Merge(
            child: label,
            style: new TextStyle(Color: defaultTextStyle.Color),
            maxLines: isLargeTextModeEnabled ? KDefaultLargeTextModeMaxLines : KDefaultMaxLines,
            overflow: TextOverflow.Ellipsis,
            softWrap: true);

        return MediaQuery.WithClampedTextScaling(
            context,
            new CupertinoMenuItemInteractionHandler(
                mouseCursor: MouseCursor ?? KDefaultCursor,
                requestFocusOnHover: RequestFocusOnHover,
                onPressed: OnPressed is null ? null : () => HandleSelect(context),
                onHover: OnHover,
                onFocusChange: OnFocusChange,
                autofocus: Autofocus,
                focusNode: FocusNode,
                decoration: Decoration ?? ResolveDefaultDecoration(context),
                behavior: Behavior,
                child: label),
            minScaleFactor: CupertinoMenuMetrics.KMinimumTextScaleFactor,
            maxScaleFactor: CupertinoMenuMetrics.KMaximumTextScaleFactor);
    }

    /// <summary>Dart parity source: <c>CupertinoMenuItem._resolveDefaultTextStyle</c>.</summary>
    internal TextStyle ResolveDefaultTextStyle(BuildContext context, TextScaler textScaler)
    {
        CupertinoDynamicColor color = OnPressed is null
            ? CupertinoColors.SystemGrey
            : IsDestructiveAction
                ? CupertinoColors.SystemRed
                : KDefaultTextColor;
        return CupertinoDynamicTypeStyle.Body
            .ResolveTextStyle(textScaler)
            .CopyWith(
                fontSize: CupertinoMenuMetrics.KCupertinoMobileBaseFontSize,
                color: color.ResolveFrom(context));
    }

    /// <summary>Dart parity source: <c>CupertinoMenuItem._resolveDefaultSubtitleStyle</c>.</summary>
    /// <remarks>
    /// Dart puts the color on a <c>foreground</c> <c>Paint</c> with a <c>BlendMode.hardLight</c>
    /// (light) / <c>BlendMode.plus</c> (dark) blend; Plumix's <c>TextStyle</c> carries no
    /// <c>Paint</c>, so the color is set directly (see <c>docs/ai/DIVERGENCES.md</c>).
    /// </remarks>
    internal static TextStyle ResolveDefaultSubtitleStyle(BuildContext context, TextScaler textScaler)
    {
        return CupertinoDynamicTypeStyle.Subhead
            .ResolveTextStyle(textScaler)
            .CopyWith(
                fontSize: 15.0,
                textBaseline: Plumix.UI.TextBaseline.Alphabetic,
                color: KDefaultSubtitleTextColor.ResolveFrom(context));
    }

    /// <summary>Dart parity source: <c>CupertinoMenuItem._handleSelect</c>.</summary>
    private void HandleSelect(BuildContext context)
    {
        if (RequestCloseOnActivate)
        {
            MenuController.MaybeOf(context)?.Close();
        }

        OnPressed?.Invoke();
    }

    private static WidgetStateProperty<BoxDecoration> ResolveDefaultDecoration(BuildContext context) =>
        CupertinoTheme.MaybeBrightnessOf(context) == PlatformBrightness.Dark
            ? KDefaultDarkDecoration
            : KDefaultDecoration;

    private static WidgetStateProperty<BoxDecoration> BuildDefaultDecoration(bool dark)
    {
        BoxDecoration Fill(double opacity) => new(Color: dark
            ? Color.FromArgb((byte)Math.Round(opacity * 255.0), 255, 255, 255)
            : Color.FromArgb((byte)Math.Round(opacity * 255.0), 50, 50, 50));

        return WidgetStateProperty<BoxDecoration>.FromMap(
        [
            new(WidgetState.Dragged, Fill(0.1)),
            new(WidgetState.Pressed, Fill(0.1)),
            new(WidgetState.Focused, Fill(0.075)),
            new(WidgetState.Hovered, Fill(0.05)),
            new(WidgetStatesConstraint.Any, new BoxDecoration()),
        ]);
    }
}

/// <summary>Dart parity source: <c>_CupertinoMenuItemLabel</c>.</summary>
internal sealed class CupertinoMenuItemLabel : StatelessWidget
{
    private const double KDefaultHorizontalWidth = 16.0;

    private const double KLeadingWidthSlope = -311.0 / 1000.0;

    private const double KLeadingWidthYIntercept = 10.0;

    private const double KLeadingMidpointSlope = 118.0 / 1000000.0;

    private const double KLeadingMidpointYIntercept = 73.0 / 125.0;

    private const double KTrailingWidthSlope = 1.0 / 10.0;

    private const double KTrailingWidthYIntercept = 22.0;

    private const double KFirstBaselineToTopSlope = 14.0 / 11.0;

    private const double KLastBaselineToBottomSlope = 71.0 / 100.0;

    public CupertinoMenuItemLabel(
        Widget child,
        Widget? subtitle = null,
        Widget? leading = null,
        Widget? trailing = null,
        double? leadingWidth = null,
        double? trailingWidth = null,
        AlignmentGeometry? leadingMidpointAlignment = null,
        AlignmentGeometry? trailingMidpointAlignment = null,
        EdgeInsetsGeometry? padding = null,
        BoxConstraints? constraints = null,
        Key? key = null) : base(key)
    {
        Child = child;
        Subtitle = subtitle;
        Leading = leading;
        Trailing = trailing;
        LeadingWidth = leadingWidth;
        TrailingWidth = trailingWidth;
        LeadingAlignment = leadingMidpointAlignment;
        TrailingAlignment = trailingMidpointAlignment;
        Padding = padding;
        Constraints = constraints;
    }

    public Widget Child { get; }

    public Widget? Subtitle { get; }

    public Widget? Leading { get; }

    public Widget? Trailing { get; }

    public double? LeadingWidth { get; }

    public double? TrailingWidth { get; }

    public AlignmentGeometry? LeadingAlignment { get; }

    public AlignmentGeometry? TrailingAlignment { get; }

    public EdgeInsetsGeometry? Padding { get; }

    public BoxConstraints? Constraints { get; }

    public override Widget Build(BuildContext context)
    {
        TextDirection textDirection = Directionality.MaybeOf(context) ?? TextDirection.Ltr;
        TextScaler textScaler = MediaQuery.MaybeTextScalerOf(context) ?? TextScaler.NoScaling;
        double pixelRatio = MediaQuery.MaybeOf(context)?.DevicePixelRatio ?? 1.0;
        TextStyle dynamicBodyText = CupertinoDynamicTypeStyle.Body.ResolveTextStyle(textScaler);
        double lineHeight = dynamicBodyText.FontSize!.Value * dynamicBodyText.Height!.Value;
        bool showLeadingWidget = Leading is not null
                                 || (CupertinoMenuAnchor.MaybeHasLeadingOf(context) ?? false);
        double minimumHeight = ResolveFirstBaselineToTop(lineHeight, pixelRatio)
                               + ResolveLastBaselineToBottom(lineHeight, pixelRatio);
        BoxConstraints constraints = Constraints ?? new BoxConstraints(MinHeight: minimumHeight);
        EdgeInsetsGeometry resolvedPadding = Padding ?? ResolvePadding(minimumHeight, lineHeight);
        double resolvedLeadingWidth = LeadingWidth ?? (showLeadingWidget
            ? ResolveLeadingWidth(textScaler, lineHeight, pixelRatio)
            : KDefaultHorizontalWidth);
        double resolvedTrailingWidth = TrailingWidth ?? (Trailing is not null
            ? ResolveTrailingWidth(textScaler, lineHeight, pixelRatio)
            : KDefaultHorizontalWidth);

        var stackChildren = new List<Widget>();
        if (showLeadingWidget)
        {
            stackChildren.Add(new PositionedDirectional(
                start: 0.0,
                top: 0.0,
                bottom: 0.0,
                width: resolvedLeadingWidth,
                child: new CupertinoAlignMidpoint(
                    alignment: LeadingAlignment ?? ResolveLeadingAlignment(textScaler),
                    child: Leading)));
        }

        stackChildren.Add(new Padding(
            EdgeInsetsGeometry
                .DirectionalOnly(start: resolvedLeadingWidth, end: resolvedTrailingWidth)
                .Resolve(textDirection),
            Subtitle is null
                ? new Align(alignment: AlignmentDirectional.CenterStart, child: Child)
                : new Column(
                    mainAxisSize: MainAxisSize.Min,
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    mainAxisAlignment: MainAxisAlignment.Center,
                    children: [Child, new SizedBox(height: 1.0), Subtitle])));

        if (Trailing is not null)
        {
            stackChildren.Add(new PositionedDirectional(
                end: 0.0,
                top: 0.0,
                bottom: 0.0,
                width: resolvedTrailingWidth,
                child: new CupertinoAlignMidpoint(
                    alignment: TrailingAlignment ?? ResolveTrailingAlignment(resolvedTrailingWidth),
                    child: Trailing)));
        }

        return new ConstrainedBox(
            constraints,
            new Padding(resolvedPadding.Resolve(textDirection), new Stack(children: stackChildren)));
    }

    private static double ResolveLeadingWidth(TextScaler textScaler, double lineHeight, double pixelRatio)
    {
        double units = CupertinoMenuMetrics.NormalizeTextScale(textScaler);
        return CupertinoMenuMetrics.RoundToDivisible(
            (KLeadingWidthSlope * units) + KLeadingWidthYIntercept + lineHeight,
            1.0 / pixelRatio);
    }

    private static double ResolveTrailingWidth(TextScaler textScaler, double lineHeight, double pixelRatio)
    {
        double units = CupertinoMenuMetrics.NormalizeTextScale(textScaler);
        return CupertinoMenuMetrics.RoundToDivisible(
            (KTrailingWidthSlope * units) + KTrailingWidthYIntercept + lineHeight,
            1.0 / pixelRatio);
    }

    private static AlignmentGeometry ResolveLeadingAlignment(TextScaler textScaler)
    {
        double units = CupertinoMenuMetrics.NormalizeTextScale(textScaler);
        double ratio = (KLeadingMidpointSlope * units) + KLeadingMidpointYIntercept;
        return new AlignmentDirectional((ratio * 2.0) - 1.0, 0.0);
    }

    private static AlignmentGeometry ResolveTrailingAlignment(double trailingWidth)
    {
        double horizontalOffset = (trailingWidth / 2.0) + 6.0;
        double ratio = (trailingWidth - horizontalOffset) / trailingWidth;
        return new AlignmentDirectional((ratio * 2.0) - 1.0, 0.0);
    }

    private static double ResolveFirstBaselineToTop(double lineHeight, double pixelRatio) =>
        CupertinoMenuMetrics.RoundToDivisible(lineHeight * KFirstBaselineToTopSlope, 1.0 / pixelRatio);

    private static double ResolveLastBaselineToBottom(double lineHeight, double pixelRatio) =>
        CupertinoMenuMetrics.RoundToDivisible(lineHeight * KLastBaselineToBottomSlope, 1.0 / pixelRatio);

    private static EdgeInsetsGeometry ResolvePadding(double minimumHeight, double lineHeight) =>
        EdgeInsetsGeometry.Symmetric(vertical: Math.Max(0.0, minimumHeight - lineHeight) / 2.0);
}

/// <summary>Dart parity source: <c>_AlignMidpoint</c>.</summary>
internal sealed class CupertinoAlignMidpoint : SingleChildRenderObjectWidget
{
    public CupertinoAlignMidpoint(
        AlignmentGeometry alignment,
        Widget? child = null,
        Key? key = null) : base(child, key)
    {
        Alignment = alignment;
    }

    public AlignmentGeometry Alignment { get; }

    public override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderCupertinoAlignMidpoint(Alignment, Directionality.MaybeOf(context));

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var midpoint = (RenderCupertinoAlignMidpoint)renderObject;
        midpoint.Alignment = Alignment;
        midpoint.TextDirection = Directionality.MaybeOf(context);
    }
}

/// <summary>Dart parity source: <c>_RenderAlignMidpoint</c>.</summary>
internal sealed class RenderCupertinoAlignMidpoint : RenderPositionedBox
{
    public RenderCupertinoAlignMidpoint(AlignmentGeometry alignment, TextDirection? textDirection = null)
        : base(alignment: alignment, textDirection: textDirection)
    {
    }

    protected override void AlignChild()
    {
        RenderBox child = Child ?? throw new AssertionError("AlignChild requires a child.");
        Point midpoint = ResolvedAlignment.AlongSize(Size);
        double dx = Math.Clamp(midpoint.X - (child.Size.Width / 2.0), 0.0, Size.Width - child.Size.Width);
        double dy = Math.Clamp(midpoint.Y - (child.Size.Height / 2.0), 0.0, Size.Height - child.Size.Height);
        ((BoxParentData)child.parentData!).offset = new Point(dx, dy);
    }
}

/// <summary>Dart parity source: <c>_CupertinoMenuItemInteractionHandler</c>.</summary>
internal sealed class CupertinoMenuItemInteractionHandler : StatefulWidget
{
    public CupertinoMenuItemInteractionHandler(
        Widget child,
        WidgetStateProperty<MouseCursor> mouseCursor,
        WidgetStateProperty<BoxDecoration> decoration,
        HitTestBehavior behavior,
        bool autofocus,
        bool requestFocusOnHover,
        FocusNode? focusNode = null,
        Action<bool>? onHover = null,
        Action<bool>? onFocusChange = null,
        Action? onPressed = null,
        Key? key = null) : base(key)
    {
        Child = child;
        MouseCursor = mouseCursor;
        Decoration = decoration;
        Behavior = behavior;
        Autofocus = autofocus;
        RequestFocusOnHover = requestFocusOnHover;
        FocusNode = focusNode;
        OnHover = onHover;
        OnFocusChange = onFocusChange;
        OnPressed = onPressed;
    }

    public Widget Child { get; }

    public WidgetStateProperty<MouseCursor> MouseCursor { get; }

    public WidgetStateProperty<BoxDecoration> Decoration { get; }

    public HitTestBehavior Behavior { get; }

    public bool Autofocus { get; }

    public bool RequestFocusOnHover { get; }

    public FocusNode? FocusNode { get; }

    public Action<bool>? OnHover { get; }

    public Action<bool>? OnFocusChange { get; }

    public Action? OnPressed { get; }

    public override State CreateState() => new CupertinoMenuItemInteractionHandlerState();
}

/// <summary>Dart parity source: <c>_CupertinoMenuItemInteractionHandlerState</c>.</summary>
internal sealed class CupertinoMenuItemInteractionHandlerState : State
{
    private readonly WidgetStatesController _statesController = new();
    private IReadOnlyDictionary<Type, FlutterAction>? _actions;
    private IReadOnlyDictionary<Type, IGestureRecognizerFactory>? _gestures;
    private DeviceGestureSettings? _gestureSettings;
    private FocusNode? _internalFocusNode;

    private CupertinoMenuItemInteractionHandler Current =>
        (CupertinoMenuItemInteractionHandler)StateWidget;

    private FocusNode EffectiveFocusNode => Current.FocusNode ?? _internalFocusNode!;

    private bool IsEnabled
    {
        get => !_statesController.Value.Contains(WidgetState.Disabled);
        set => _statesController.Update(WidgetState.Disabled, !value);
    }

    private bool IsHovered
    {
        get => _statesController.Value.Contains(WidgetState.Hovered);
        set => _statesController.Update(WidgetState.Hovered, value);
    }

    private bool IsPressed
    {
        get => _statesController.Value.Contains(WidgetState.Pressed);
        set => _statesController.Update(WidgetState.Pressed, value);
    }

    private bool IsSwiped
    {
        get => _statesController.Value.Contains(WidgetState.Dragged);
        set => _statesController.Update(WidgetState.Dragged, value);
    }

    private bool IsFocused
    {
        get => _statesController.Value.Contains(WidgetState.Focused);
        set => _statesController.Update(WidgetState.Focused, value);
    }

    public override void InitState()
    {
        _actions = new Dictionary<Type, FlutterAction>
        {
            [typeof(ActivateIntent)] = new CallbackAction<ActivateIntent>(HandleActivation),
            [typeof(ButtonActivateIntent)] = new CallbackAction<ButtonActivateIntent>(HandleActivation),
        };
        if (Current.FocusNode is null)
        {
            _internalFocusNode = new FocusNode();
        }

        IsEnabled = Current.OnPressed is not null;
        IsFocused = EffectiveFocusNode.HasPrimaryFocus;
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var previous = (CupertinoMenuItemInteractionHandler)oldWidget;
        if (!ReferenceEquals(previous.FocusNode, Current.FocusNode))
        {
            _internalFocusNode?.Dispose();
            _internalFocusNode = Current.FocusNode is null ? new FocusNode() : null;
            IsFocused = EffectiveFocusNode.HasPrimaryFocus;
        }

        if (!ReferenceEquals(previous.OnPressed, Current.OnPressed))
        {
            if (Current.OnPressed is null)
            {
                IsEnabled = false;
                IsHovered = false;
                IsPressed = false;
                IsSwiped = false;
                IsFocused = false;
            }
            else
            {
                IsEnabled = true;
            }
        }
    }

    public override Widget Build(BuildContext context)
    {
        DeviceGestureSettings? newGestureSettings = MediaQuery.MaybeGestureSettingsOf(context);
        if (_gestureSettings != newGestureSettings)
        {
            _gestureSettings = newGestureSettings;
            _gestures = null;
        }

        _gestures ??= new Dictionary<Type, IGestureRecognizerFactory>
        {
            [typeof(TapGestureRecognizer)] = new GestureRecognizerFactoryWithHandlers<TapGestureRecognizer>(
                () => new TapGestureRecognizer { DebugOwner = this },
                instance =>
                {
                    instance.OnTapDown = HandleTapDown;
                    instance.OnTapUp = HandleTapUp;
                    instance.OnTapCancel = HandleTapCancel;
                    instance.GestureSettings = _gestureSettings;
                }),
        };

        return new MergeSemantics(new Semantics(
            enabled: IsEnabled,
            onDismiss: IsEnabled ? HandleDismissMenu : null,
            child: new Actions(
                actions: IsEnabled ? _actions! : new Dictionary<Type, FlutterAction>(),
                child: new Focus(
                    autofocus: IsEnabled && Current.Autofocus,
                    focusNode: EffectiveFocusNode,
                    canRequestFocus: IsEnabled,
                    skipTraversal: !IsEnabled,
                    includeSemantics: true,
                    onFocusChange: HandleFocusChange,
                    child: new CupertinoMenuSwipeTarget(
                        onEnter: HandleSwipeEnter,
                        onExit: HandleSwipeExit,
                        onCompletion: HandleSwipeCompleted,
                        child: new ValueListenableBuilder<IReadOnlySet<WidgetState>>(
                            valueListenable: _statesController,
                            builder: BuildStatefulAppearance,
                            child: new RawGestureDetector(
                                behavior: Current.Behavior,
                                gestures: IsEnabled ? _gestures! : RawGestureDetector.NoGestures,
                                child: Current.Child)))))));
    }

    public override void Dispose()
    {
        _statesController.Dispose();
        _internalFocusNode?.Dispose();
        _internalFocusNode = null;

        base.Dispose();
    }

    /// <summary>Dart parity source: <c>_buildStatefulAppearance</c>.</summary>
    private Widget BuildStatefulAppearance(
        BuildContext context,
        IReadOnlySet<WidgetState> value,
        Widget? child)
    {
        MouseCursor cursor = Current.MouseCursor.Resolve(value);
        BoxDecoration decoration = Current.Decoration.Resolve(value);

        // Dart injects a `BlendMode.multiply` (light) / `BlendMode.plus` (dark) background blend mode
        // here; `BoxDecoration` carries no blend mode in Plumix because Avalonia cannot blend a
        // geometry fill (docs/ai/DIVERGENCES.md).
        return new MouseRegion(
            onHover: IsEnabled ? HandlePointerHover : null,
            onExit: IsEnabled ? HandlePointerExit : null,
            hitTestBehavior: HitTestBehavior.DeferToChild,
            cursor: cursor,
            opaque: false,
            child: new DecoratedBox(decoration, child: child));
    }

    private object? HandleActivation(Intent intent)
    {
        IsSwiped = false;
        IsPressed = false;
        Current.OnPressed?.Invoke();
        return null;
    }

    private void HandleTapDown(TapDownDetails details) => IsPressed = true;

    private void HandleTapUp(TapUpDetails details)
    {
        IsPressed = false;
        Current.OnPressed?.Invoke();
    }

    private void HandleTapCancel() => IsPressed = false;

    private void HandleFocusChange(bool focused)
    {
        IsFocused = EffectiveFocusNode.HasPrimaryFocus;
        Current.OnFocusChange?.Invoke(IsFocused);
    }

    private void HandlePointerHover(PointerHoverEvent evt)
    {
        if (IsHovered)
        {
            return;
        }

        IsHovered = true;
        Current.OnHover?.Invoke(true);
        if (Current.RequestFocusOnHover)
        {
            EffectiveFocusNode.RequestFocus();
            FocusTraversalGroup.Of(Context).InvalidateScopeData(
                FocusScope.MaybeOf(Context) ?? FocusManager.Instance.RootScope);
        }
    }

    private void HandlePointerExit(PointerExitEvent evt)
    {
        if (!IsHovered)
        {
            return;
        }

        IsHovered = false;
        IsFocused = false;
        Current.OnHover?.Invoke(false);
    }

    private void HandleDismissMenu() => Actions.Invoke(Context, new DismissIntent());

    private void HandleSwipeEnter()
    {
        if (!IsEnabled)
        {
            return;
        }

        switch (PlatformDefaults.TargetPlatform)
        {
            case TargetPlatform.IOS:
            case TargetPlatform.Android:
                HapticFeedback.SelectionClick();
                break;
        }

        IsSwiped = true;
    }

    private void HandleSwipeExit()
    {
        if (Mounted)
        {
            IsSwiped = false;
        }
    }

    private void HandleSwipeCompleted()
    {
        if (Mounted && IsEnabled)
        {
            HandleActivation(new ActivateIntent());
        }
    }
}
