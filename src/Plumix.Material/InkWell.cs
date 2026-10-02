using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/ink_well.dart

/// <summary>
/// An ink feature that is drawn as a reaction to user input: splashes (<see cref="InkSplash"/>,
/// <see cref="InkRipple"/>, <see cref="InkSparkle"/>) and highlights (<see cref="InkHighlight"/>).
/// </summary>
public abstract class InteractiveInkFeature : InkFeature
{
    private Color _color;
    private ShapeBorder? _customBorder;

    /// <summary>Creates an InteractiveInkFeature.</summary>
    protected InteractiveInkFeature(
        MaterialInkController controller,
        RenderBox referenceBox,
        Color color,
        ShapeBorder? customBorder = null,
        Action? onRemoved = null)
        : base(controller, referenceBox, onRemoved)
    {
        _color = color;
        _customBorder = customBorder;
    }

    /// <summary>
    /// Called when the user input that triggered this feature's appearance was confirmed. Typically
    /// causes the ink to propagate faster across the material. By default this method does nothing.
    /// </summary>
    public virtual void Confirm()
    {
    }

    /// <summary>
    /// Called when the user input that triggered this feature's appearance was canceled. Typically
    /// causes the ink to gradually disappear. By default this method does nothing.
    /// </summary>
    public virtual void Cancel()
    {
    }

    /// <summary>The ink's color.</summary>
    public Color Color
    {
        get => _color;
        set
        {
            if (value == _color)
            {
                return;
            }

            _color = value;
            Controller.MarkNeedsPaint();
        }
    }

    /// <summary>The ink's optional custom border.</summary>
    public ShapeBorder? CustomBorder
    {
        get => _customBorder;
        set
        {
            if (value == _customBorder)
            {
                return;
            }

            _customBorder = value;
            Controller.MarkNeedsPaint();
        }
    }

    /// <summary>
    /// Draws an ink splash or ink ripple on the passed in <paramref name="canvas"/>, clipped to
    /// <paramref name="clipCallback"/>'s rect by <paramref name="customBorder"/>, a non-zero
    /// <paramref name="borderRadius"/>, or the rect itself.
    /// </summary>
    protected void PaintInkCircle(
        Canvas canvas,
        Matrix4 transform,
        Paint paint,
        Point center,
        double radius,
        TextDirection? textDirection = null,
        ShapeBorder? customBorder = null,
        BorderRadius? borderRadius = null,
        RectCallback? clipCallback = null)
    {
        BorderRadius resolvedBorderRadius = borderRadius ?? BorderRadius.Zero;
        Point? originOffset = MatrixUtils.GetAsTranslation(transform);
        canvas.Save();
        if (originOffset is null)
        {
            canvas.Transform(transform);
        }
        else
        {
            canvas.Translate(originOffset.Value.X, originOffset.Value.Y);
        }

        if (clipCallback is not null)
        {
            Rect rect = clipCallback();
            if (customBorder is not null)
            {
                canvas.ClipPath(customBorder.GetOuterPath(rect, textDirection: textDirection));
            }
            else if (resolvedBorderRadius != BorderRadius.Zero)
            {
                canvas.ClipRRect(RRect.FromRectAndCorners(rect, resolvedBorderRadius));
            }
            else
            {
                canvas.ClipRect(rect);
            }
        }

        canvas.DrawCircle(center, radius, paint);
        canvas.Restore();
    }
}

/// <summary>An encapsulation of an <see cref="InteractiveInkFeature"/> constructor used by
/// <see cref="InkWell"/>, <see cref="InkResponse"/>, and <see cref="ThemeData"/>.</summary>
public abstract class InteractiveInkFeatureFactory
{
    /// <summary>The factory method. Subclasses should override this method to return a new instance of an
    /// <see cref="InteractiveInkFeature"/>.</summary>
    public abstract InteractiveInkFeature Create(
        MaterialInkController controller,
        RenderBox referenceBox,
        Point position,
        Color color,
        TextDirection textDirection,
        bool containedInkWell = false,
        RectCallback? rectCallback = null,
        BorderRadius? borderRadius = null,
        ShapeBorder? customBorder = null,
        double? radius = null,
        Action? onRemoved = null);
}

// Dart's `_ParentInkResponseState`.
internal interface IParentInkResponseState
{
    void MarkChildInkResponsePressed(IParentInkResponseState childState, bool value);
}

// Dart's `_ParentInkResponseProvider`.
internal sealed class ParentInkResponseProvider : InheritedWidget
{
    public ParentInkResponseProvider(IParentInkResponseState state, Widget child) : base(child: child)
    {
        State = state;
    }

    public IParentInkResponseState State { get; }

    public override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !ReferenceEquals(State, ((ParentInkResponseProvider)oldWidget).State);

    public static IParentInkResponseState? MaybeOf(BuildContext context) =>
        context.DependOnInheritedWidgetOfExactType<ParentInkResponseProvider>()?.State;
}

/// <summary>An area of a <see cref="Material"/> that responds to touch, with configurable shape and
/// clipping of its ink.</summary>
/// <remarks>
/// Requires a <see cref="Material"/> ancestor. Splashes and highlights are <see cref="InkFeature"/>s
/// painted by that Material, below its children.
/// </remarks>
public class InkResponse : StatelessWidget
{
    /// <summary>Creates an area of a <see cref="Material"/> that responds to touch.</summary>
    public InkResponse(
        Widget? child = null,
        Action? onTap = null,
        Action<TapDownDetails>? onTapDown = null,
        Action<TapUpDetails>? onTapUp = null,
        Action? onTapCancel = null,
        Action? onDoubleTap = null,
        Action? onLongPress = null,
        Action? onLongPressUp = null,
        Action? onSecondaryTap = null,
        Action<TapUpDetails>? onSecondaryTapUp = null,
        Action<TapDownDetails>? onSecondaryTapDown = null,
        Action? onSecondaryTapCancel = null,
        Action<bool>? onHighlightChanged = null,
        Action<bool>? onHover = null,
        MouseCursor? mouseCursor = null,
        bool containedInkWell = false,
        BoxShape highlightShape = BoxShape.Circle,
        double? radius = null,
        BorderRadius? borderRadius = null,
        ShapeBorder? customBorder = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        Color? highlightColor = null,
        WidgetStateProperty<Color?>? overlayColor = null,
        Color? splashColor = null,
        InteractiveInkFeatureFactory? splashFactory = null,
        bool enableFeedback = true,
        bool excludeFromSemantics = false,
        FocusNode? focusNode = null,
        bool canRequestFocus = true,
        Action<bool>? onFocusChange = null,
        bool autofocus = false,
        WidgetStatesController? statesController = null,
        TimeSpan? hoverDuration = null,
        Key? key = null) : base(key)
    {
        Child = child;
        OnTap = onTap;
        OnTapDown = onTapDown;
        OnTapUp = onTapUp;
        OnTapCancel = onTapCancel;
        OnDoubleTap = onDoubleTap;
        OnLongPress = onLongPress;
        OnLongPressUp = onLongPressUp;
        OnSecondaryTap = onSecondaryTap;
        OnSecondaryTapUp = onSecondaryTapUp;
        OnSecondaryTapDown = onSecondaryTapDown;
        OnSecondaryTapCancel = onSecondaryTapCancel;
        OnHighlightChanged = onHighlightChanged;
        OnHover = onHover;
        MouseCursor = mouseCursor;
        ContainedInkWell = containedInkWell;
        HighlightShape = highlightShape;
        Radius = radius;
        BorderRadius = borderRadius;
        CustomBorder = customBorder;
        FocusColor = focusColor;
        HoverColor = hoverColor;
        HighlightColor = highlightColor;
        OverlayColor = overlayColor;
        SplashColor = splashColor;
        SplashFactory = splashFactory;
        EnableFeedback = enableFeedback;
        ExcludeFromSemantics = excludeFromSemantics;
        FocusNode = focusNode;
        CanRequestFocus = canRequestFocus;
        OnFocusChange = onFocusChange;
        Autofocus = autofocus;
        StatesController = statesController;
        HoverDuration = hoverDuration;
    }

    /// <summary>The widget below this widget in the tree.</summary>
    public Widget? Child { get; }

    /// <summary>Called when the user taps this part of the material.</summary>
    public Action? OnTap { get; }

    /// <summary>Called when the user taps down this part of the material.</summary>
    public Action<TapDownDetails>? OnTapDown { get; }

    /// <summary>Called when the user releases a tap that was started on this part of the material.</summary>
    public Action<TapUpDetails>? OnTapUp { get; }

    /// <summary>Called when the user cancels a tap that was started on this part of the material.</summary>
    public Action? OnTapCancel { get; }

    /// <summary>Called when the user double taps this part of the material.</summary>
    public Action? OnDoubleTap { get; }

    /// <summary>Called when the user long-presses on this part of the material.</summary>
    public Action? OnLongPress { get; }

    /// <summary>Called when the user lifts their finger after a long press on the button.</summary>
    public Action? OnLongPressUp { get; }

    /// <summary>Called when the user taps this part of the material with a secondary button.</summary>
    public Action? OnSecondaryTap { get; }

    /// <summary>Called when the user taps down on this part of the material with a secondary button.</summary>
    public Action<TapDownDetails>? OnSecondaryTapDown { get; }

    /// <summary>Called when the user releases a secondary button tap that was started on this part of the
    /// material.</summary>
    public Action<TapUpDetails>? OnSecondaryTapUp { get; }

    /// <summary>Called when the user cancels a secondary button tap.</summary>
    public Action? OnSecondaryTapCancel { get; }

    /// <summary>Called when this part of the material either becomes highlighted or stops being
    /// highlighted.</summary>
    public Action<bool>? OnHighlightChanged { get; }

    /// <summary>Called when a pointer enters or exits the ink response area.</summary>
    public Action<bool>? OnHover { get; }

    /// <summary>The cursor for a mouse pointer when it enters or is hovering over the widget.</summary>
    public MouseCursor? MouseCursor { get; }

    /// <summary>Whether this ink response should be clipped its bounds.</summary>
    public bool ContainedInkWell { get; }

    /// <summary>The shape (e.g., circle, rectangle) to use for the highlight drawn around this part of the
    /// material when pressed, hovered over, or focused.</summary>
    public BoxShape HighlightShape { get; }

    /// <summary>The radius of the ink splash.</summary>
    public double? Radius { get; }

    /// <summary>The border radius of the containing rectangle.</summary>
    public BorderRadius? BorderRadius { get; }

    /// <summary>The custom clip border.</summary>
    public ShapeBorder? CustomBorder { get; }

    /// <summary>The color of the ink response when the parent widget is focused.</summary>
    public Color? FocusColor { get; }

    /// <summary>The color of the ink response when a pointer is hovering over it.</summary>
    public Color? HoverColor { get; }

    /// <summary>The highlight color of the ink response when pressed.</summary>
    public Color? HighlightColor { get; }

    /// <summary>Defines the ink response focus, hover, and splash colors.</summary>
    public WidgetStateProperty<Color?>? OverlayColor { get; }

    /// <summary>The splash color of the ink response.</summary>
    public Color? SplashColor { get; }

    /// <summary>Defines the appearance of the splash.</summary>
    public InteractiveInkFeatureFactory? SplashFactory { get; }

    /// <summary>Whether detected gestures should provide acoustic and/or haptic feedback.</summary>
    public bool EnableFeedback { get; }

    /// <summary>Whether to exclude the gestures introduced by this widget from the semantics tree.</summary>
    public bool ExcludeFromSemantics { get; }

    /// <summary>Handler called when the focus changes.</summary>
    public Action<bool>? OnFocusChange { get; }

    /// <summary>True if this widget will be selected as the initial focus when no other node in its scope
    /// is currently focused.</summary>
    public bool Autofocus { get; }

    /// <summary>An optional focus node to use as the focus node for this widget.</summary>
    public FocusNode? FocusNode { get; }

    /// <summary>If true, this widget may request the primary focus.</summary>
    public bool CanRequestFocus { get; }

    /// <summary>Represents the interactive "state" of this widget in terms of a set of
    /// <see cref="WidgetState"/>s, like pressed and focused.</summary>
    public WidgetStatesController? StatesController { get; }

    /// <summary>The duration of the animation that animates the hover effect.</summary>
    public TimeSpan? HoverDuration { get; }

    /// <summary>
    /// The rectangle to use for the highlight effect and for clipping the splash effects if
    /// <see cref="ContainedInkWell"/> is true. Defaults to <see langword="null"/> (the reference box).
    /// </summary>
    public virtual RectCallback? GetRectCallback(RenderBox referenceBox) => null;

    /// <summary>Asserts that the given context satisfies the prerequisites for this class.</summary>
    public virtual bool DebugCheckContext(BuildContext context)
    {
        DebugAssertions.Assert(MaterialDebug.DebugCheckHasMaterial(context));
        DebugAssertions.Assert(WidgetsDebug.DebugCheckHasDirectionality(context));
        return true;
    }

    public override Widget Build(BuildContext context)
    {
        IParentInkResponseState? parentState = ParentInkResponseProvider.MaybeOf(context);
        return new InkResponseStateWidget(
            onTap: OnTap,
            onTapDown: OnTapDown,
            onTapUp: OnTapUp,
            onTapCancel: OnTapCancel,
            onDoubleTap: OnDoubleTap,
            onLongPress: OnLongPress,
            onLongPressUp: OnLongPressUp,
            onSecondaryTap: OnSecondaryTap,
            onSecondaryTapUp: OnSecondaryTapUp,
            onSecondaryTapDown: OnSecondaryTapDown,
            onSecondaryTapCancel: OnSecondaryTapCancel,
            onHighlightChanged: OnHighlightChanged,
            onHover: OnHover,
            mouseCursor: MouseCursor,
            containedInkWell: ContainedInkWell,
            highlightShape: HighlightShape,
            radius: Radius,
            borderRadius: BorderRadius,
            customBorder: CustomBorder,
            focusColor: FocusColor,
            hoverColor: HoverColor,
            highlightColor: HighlightColor,
            overlayColor: OverlayColor,
            splashColor: SplashColor,
            splashFactory: SplashFactory,
            enableFeedback: EnableFeedback,
            excludeFromSemantics: ExcludeFromSemantics,
            focusNode: FocusNode,
            canRequestFocus: CanRequestFocus,
            onFocusChange: OnFocusChange,
            autofocus: Autofocus,
            parentState: parentState,
            getRectCallback: GetRectCallback,
            debugCheckContext: DebugCheckContext,
            statesController: StatesController,
            hoverDuration: HoverDuration,
            child: Child);
    }
}

// Dart's `_InkResponseStateWidget`.
internal sealed class InkResponseStateWidget : StatefulWidget
{
    public InkResponseStateWidget(
        Func<BuildContext, bool> debugCheckContext,
        Widget? child = null,
        Action? onTap = null,
        Action<TapDownDetails>? onTapDown = null,
        Action<TapUpDetails>? onTapUp = null,
        Action? onTapCancel = null,
        Action? onDoubleTap = null,
        Action? onLongPress = null,
        Action? onLongPressUp = null,
        Action? onSecondaryTap = null,
        Action<TapUpDetails>? onSecondaryTapUp = null,
        Action<TapDownDetails>? onSecondaryTapDown = null,
        Action? onSecondaryTapCancel = null,
        Action<bool>? onHighlightChanged = null,
        Action<bool>? onHover = null,
        MouseCursor? mouseCursor = null,
        bool containedInkWell = false,
        BoxShape highlightShape = BoxShape.Circle,
        double? radius = null,
        BorderRadius? borderRadius = null,
        ShapeBorder? customBorder = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        Color? highlightColor = null,
        WidgetStateProperty<Color?>? overlayColor = null,
        Color? splashColor = null,
        InteractiveInkFeatureFactory? splashFactory = null,
        bool enableFeedback = true,
        bool excludeFromSemantics = false,
        Action<bool>? onFocusChange = null,
        bool autofocus = false,
        FocusNode? focusNode = null,
        bool canRequestFocus = true,
        IParentInkResponseState? parentState = null,
        Func<RenderBox, RectCallback?>? getRectCallback = null,
        WidgetStatesController? statesController = null,
        TimeSpan? hoverDuration = null)
    {
        Child = child;
        OnTap = onTap;
        OnTapDown = onTapDown;
        OnTapUp = onTapUp;
        OnTapCancel = onTapCancel;
        OnDoubleTap = onDoubleTap;
        OnLongPress = onLongPress;
        OnLongPressUp = onLongPressUp;
        OnSecondaryTap = onSecondaryTap;
        OnSecondaryTapUp = onSecondaryTapUp;
        OnSecondaryTapDown = onSecondaryTapDown;
        OnSecondaryTapCancel = onSecondaryTapCancel;
        OnHighlightChanged = onHighlightChanged;
        OnHover = onHover;
        MouseCursor = mouseCursor;
        ContainedInkWell = containedInkWell;
        HighlightShape = highlightShape;
        Radius = radius;
        BorderRadius = borderRadius;
        CustomBorder = customBorder;
        FocusColor = focusColor;
        HoverColor = hoverColor;
        HighlightColor = highlightColor;
        OverlayColor = overlayColor;
        SplashColor = splashColor;
        SplashFactory = splashFactory;
        EnableFeedback = enableFeedback;
        ExcludeFromSemantics = excludeFromSemantics;
        OnFocusChange = onFocusChange;
        Autofocus = autofocus;
        FocusNode = focusNode;
        CanRequestFocus = canRequestFocus;
        ParentState = parentState;
        GetRectCallback = getRectCallback;
        DebugCheckContext = debugCheckContext;
        StatesController = statesController;
        HoverDuration = hoverDuration;
    }

    public Widget? Child { get; }
    public Action? OnTap { get; }
    public Action<TapDownDetails>? OnTapDown { get; }
    public Action<TapUpDetails>? OnTapUp { get; }
    public Action? OnTapCancel { get; }
    public Action? OnDoubleTap { get; }
    public Action? OnLongPress { get; }
    public Action? OnLongPressUp { get; }
    public Action? OnSecondaryTap { get; }
    public Action<TapUpDetails>? OnSecondaryTapUp { get; }
    public Action<TapDownDetails>? OnSecondaryTapDown { get; }
    public Action? OnSecondaryTapCancel { get; }
    public Action<bool>? OnHighlightChanged { get; }
    public Action<bool>? OnHover { get; }
    public MouseCursor? MouseCursor { get; }
    public bool ContainedInkWell { get; }
    public BoxShape HighlightShape { get; }
    public double? Radius { get; }
    public BorderRadius? BorderRadius { get; }
    public ShapeBorder? CustomBorder { get; }
    public Color? FocusColor { get; }
    public Color? HoverColor { get; }
    public Color? HighlightColor { get; }
    public WidgetStateProperty<Color?>? OverlayColor { get; }
    public Color? SplashColor { get; }
    public InteractiveInkFeatureFactory? SplashFactory { get; }
    public bool EnableFeedback { get; }
    public bool ExcludeFromSemantics { get; }
    public Action<bool>? OnFocusChange { get; }
    public bool Autofocus { get; }
    public FocusNode? FocusNode { get; }
    public bool CanRequestFocus { get; }
    public IParentInkResponseState? ParentState { get; }
    public Func<RenderBox, RectCallback?>? GetRectCallback { get; }
    public Func<BuildContext, bool> DebugCheckContext { get; }
    public WidgetStatesController? StatesController { get; }
    public TimeSpan? HoverDuration { get; }

    public override State CreateState() => new InkResponseState();

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        var gestures = new List<string>();
        if (OnTap is not null) gestures.Add("tap");
        if (OnDoubleTap is not null) gestures.Add("double tap");
        if (OnLongPress is not null) gestures.Add("long press");
        if (OnLongPressUp is not null) gestures.Add("long press up");
        if (OnTapDown is not null) gestures.Add("tap down");
        if (OnTapUp is not null) gestures.Add("tap up");
        if (OnTapCancel is not null) gestures.Add("tap cancel");
        if (OnSecondaryTap is not null) gestures.Add("secondary tap");
        if (OnSecondaryTapUp is not null) gestures.Add("secondary tap up");
        if (OnSecondaryTapDown is not null) gestures.Add("secondary tap down");
        if (OnSecondaryTapCancel is not null) gestures.Add("secondary tap cancel");
        properties.Add(new IterableProperty<string>("gestures", gestures, ifEmpty: "<none>"));
        properties.Add(new DiagnosticsProperty<MouseCursor>("mouseCursor", MouseCursor));
        properties.Add(new DiagnosticsProperty<bool>(
            "containedInkWell",
            ContainedInkWell,
            level: DiagnosticLevel.Fine));
        properties.Add(new DiagnosticsProperty<BoxShape>(
            "highlightShape",
            HighlightShape,
            description: $"{(ContainedInkWell ? "clipped to " : "")}{Diagnostics.DescribeEnum(HighlightShape)}",
            showName: false));
    }
}

// Dart's `_HighlightType`.
internal enum InkHighlightType
{
    Pressed,
    Hover,
    Focus,
}

// Dart's `_InkResponseState`.
internal sealed class InkResponseState
    : AutomaticKeepAliveClientMixin<InkResponseStateWidget>, IParentInkResponseState
{
    private static readonly TimeSpan ActivationDuration = TimeSpan.FromMilliseconds(100);

    private HashSet<InteractiveInkFeature>? _splashes;
    private InteractiveInkFeature? _currentSplash;
    private bool _hovering;
    private readonly Dictionary<InkHighlightType, InkHighlight?> _highlights = [];
    private Dictionary<Type, FlutterAction>? _actionMap;
    private readonly ObserverList<IParentInkResponseState> _activeChildren = new();
    private GestureTimer? _activationTimer;
    private bool _hasFocus;

    // Dart's `MaterialStatesController? internalStatesController`.
    internal WidgetStatesController? InternalStatesController { get; private set; }

    private IReadOnlyDictionary<Type, FlutterAction> ActionMap => _actionMap ??= new Dictionary<Type, FlutterAction>
    {
        [typeof(ActivateIntent)] = new CallbackAction<ActivateIntent>(intent =>
        {
            ActivateOnIntent(intent);
            return null;
        }),
        [typeof(ButtonActivateIntent)] = new CallbackAction<ButtonActivateIntent>(intent =>
        {
            ActivateOnIntent(intent);
            return null;
        }),
    };

    private bool HighlightsExist => _highlights.Values.Any(highlight => highlight is not null);

    public void MarkChildInkResponsePressed(IParentInkResponseState childState, bool value)
    {
        bool lastAnyPressed = AnyChildInkResponsePressed;
        if (value)
        {
            _activeChildren.Add(childState);
        }
        else
        {
            _activeChildren.Remove(childState);
        }

        bool nowAnyPressed = AnyChildInkResponsePressed;
        if (nowAnyPressed != lastAnyPressed)
        {
            Widget.ParentState?.MarkChildInkResponsePressed(this, nowAnyPressed);
        }
    }

    private bool AnyChildInkResponsePressed => _activeChildren.IsNotEmpty;

    internal void ActivateOnIntent(Intent? intent)
    {
        _activationTimer?.Cancel();
        _activationTimer = null;
        StartNewSplash(context: Context);
        _currentSplash?.Confirm();
        _currentSplash = null;
        if (Widget.OnTap is not null)
        {
            if (Widget.EnableFeedback)
            {
                _ = Feedback.ForTap(Context);
            }

            Widget.OnTap?.Invoke();
        }

        // Delay the call to `updateHighlight` to simulate a pressed delay
        // and give WidgetStatesController listeners a chance to react.
        _activationTimer = GestureTimer.Start(ActivationDuration, () =>
        {
            UpdateHighlight(InkHighlightType.Pressed, value: false);
        });
    }

    internal void SimulateTap(Intent? intent = null)
    {
        StartNewSplash(context: Context);
        HandleTap();
    }

    internal void SimulateLongPress()
    {
        StartNewSplash(context: Context);
        HandleLongPress();
    }

    private void HandleStatesControllerChange()
    {
        // Force a rebuild to resolve widget.overlayColor, widget.mouseCursor
        SetState(() => { });
    }

    internal WidgetStatesController StatesController => Widget.StatesController ?? InternalStatesController!;

    private void InitStatesController()
    {
        if (Widget.StatesController is null)
        {
            InternalStatesController = new WidgetStatesController();
        }

        StatesController.Update(WidgetState.Disabled, !Enabled);
        StatesController.AddListener(HandleStatesControllerChange);
    }

    public override void InitState()
    {
        base.InitState();
        InitStatesController();
        FocusManager.Instance.AddHighlightModeListener(HandleFocusHighlightModeChange);
    }

    public override void DidUpdateWidget(InkResponseStateWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        if (!ReferenceEquals(Widget.StatesController, oldWidget.StatesController))
        {
            oldWidget.StatesController?.RemoveListener(HandleStatesControllerChange);
            if (Widget.StatesController is not null)
            {
                InternalStatesController?.Dispose();
                InternalStatesController = null;
            }

            InitStatesController();
        }

        if (Widget.Radius != oldWidget.Radius
            || Widget.HighlightShape != oldWidget.HighlightShape
            || Widget.BorderRadius != oldWidget.BorderRadius)
        {
            InkHighlight? hoverHighlight = HighlightOf(InkHighlightType.Hover);
            if (hoverHighlight is not null)
            {
                hoverHighlight.Dispose();
                UpdateHighlight(InkHighlightType.Hover, value: _hovering, callOnHover: false);
            }

            InkHighlight? focusHighlight = HighlightOf(InkHighlightType.Focus);
            // Do not call updateFocusHighlights() here because it is called below
            focusHighlight?.Dispose();
        }

        if (Widget.CustomBorder != oldWidget.CustomBorder)
        {
            UpdateHighlightsAndSplashes();
        }

        if (Enabled != IsWidgetEnabled(oldWidget))
        {
            StatesController.Update(WidgetState.Disabled, !Enabled);
            if (!Enabled)
            {
                StatesController.Update(WidgetState.Pressed, false);
                // Remove the existing hover highlight immediately when enabled is false.
                // Do not rely on updateHighlight or InkHighlight.deactivate to not break
                // the expected lifecycle which is updating _hovering when the mouse exit.
                // Manually updating _hovering here or calling InkHighlight.deactivate
                // will lead to onHover not being called or call when it is not allowed.
                HighlightOf(InkHighlightType.Hover)?.Dispose();
            }

            // Don't call widget.onHover because many widgets, including the button
            // widgets, apply setState to an ancestor context from onHover.
            UpdateHighlight(InkHighlightType.Hover, value: _hovering, callOnHover: false);
        }

        UpdateFocusHighlights();
    }

    public override void Dispose()
    {
        FocusManager.Instance.RemoveHighlightModeListener(HandleFocusHighlightModeChange);
        StatesController.RemoveListener(HandleStatesControllerChange);
        InternalStatesController?.Dispose();
        _activationTimer?.Cancel();
        _activationTimer = null;
        base.Dispose();
    }

    protected override bool WantKeepAlive => HighlightsExist || (_splashes is { Count: > 0 });

    private InkHighlight? HighlightOf(InkHighlightType type) => _highlights.GetValueOrDefault(type);

    private Color GetHighlightColorForType(InkHighlightType type)
    {
        return type switch
        {
            // The pressed state triggers a ripple (ink splash), per the current
            // Material Design spec. A separate highlight is no longer used.
            // See https://material.io/design/interaction/states.html#pressed
            InkHighlightType.Pressed => Widget.HighlightColor ?? Theme.Of(Context).HighlightColor,
            InkHighlightType.Focus => Widget.FocusColor ?? Theme.Of(Context).FocusColor,
            _ => Widget.HoverColor ?? Theme.Of(Context).HoverColor,
        };
    }

    internal TimeSpan GetFadeDurationForType(InkHighlightType type)
    {
        return type switch
        {
            InkHighlightType.Pressed => TimeSpan.FromMilliseconds(200),
            _ => Widget.HoverDuration ?? TimeSpan.FromMilliseconds(50),
        };
    }

    internal void UpdateHighlight(InkHighlightType type, bool value, bool callOnHover = true)
    {
        InkHighlight? highlight = HighlightOf(type);
        void HandleInkRemoval()
        {
            DebugAssertions.Assert(HighlightOf(type) is not null);
            _highlights[type] = null;
            UpdateKeepAlive();
        }

        switch (type)
        {
            case InkHighlightType.Pressed:
                StatesController.Update(WidgetState.Pressed, value);
                break;
            case InkHighlightType.Hover:
                if (callOnHover)
                {
                    StatesController.Update(WidgetState.Hovered, value);
                }

                break;
            case InkHighlightType.Focus:
                // see handleFocusUpdate()
                break;
        }

        if (type == InkHighlightType.Pressed)
        {
            Widget.ParentState?.MarkChildInkResponsePressed(this, value);
        }

        if (value == (highlight is not null && highlight.Active))
        {
            return;
        }

        if (value)
        {
            if (highlight is null)
            {
                Color resolvedOverlayColor = Widget.OverlayColor?.Resolve(StatesController.Value)
                                             ?? GetHighlightColorForType(type);
                var referenceBox = (RenderBox)Context.FindRenderObject()!;
                _highlights[type] = new InkHighlight(
                    controller: Material.Of(Context),
                    referenceBox: referenceBox,
                    color: Enabled ? resolvedOverlayColor : resolvedOverlayColor.WithAlpha(0),
                    shape: Widget.HighlightShape,
                    radius: Widget.Radius,
                    borderRadius: Widget.BorderRadius,
                    customBorder: Widget.CustomBorder,
                    rectCallback: Widget.GetRectCallback!(referenceBox),
                    onRemoved: HandleInkRemoval,
                    textDirection: Directionality.Of(Context),
                    fadeDuration: GetFadeDurationForType(type));
                UpdateKeepAlive();
            }
            else
            {
                highlight.Activate();
            }
        }
        else
        {
            highlight!.Deactivate();
        }

        DebugAssertions.Assert(value == (HighlightOf(type) is { Active: true }));

        switch (type)
        {
            case InkHighlightType.Pressed:
                Widget.OnHighlightChanged?.Invoke(value);
                break;
            case InkHighlightType.Hover:
                if (callOnHover)
                {
                    Widget.OnHover?.Invoke(value);
                }

                break;
            case InkHighlightType.Focus:
                break;
        }
    }

    private void UpdateHighlightsAndSplashes()
    {
        foreach (InkHighlight? inkHighlight in _highlights.Values)
        {
            if (inkHighlight is not null)
            {
                inkHighlight.CustomBorder = Widget.CustomBorder;
            }
        }

        if (_currentSplash is not null)
        {
            _currentSplash.CustomBorder = Widget.CustomBorder;
        }

        if (_splashes is { Count: > 0 })
        {
            foreach (InteractiveInkFeature inkFeature in _splashes)
            {
                inkFeature.CustomBorder = Widget.CustomBorder;
            }
        }
    }

    private InteractiveInkFeature CreateSplash(Point globalPosition)
    {
        MaterialInkController inkController = Material.Of(Context);
        var referenceBox = (RenderBox)Context.FindRenderObject()!;
        Point position = referenceBox.GlobalToLocal(globalPosition);
        Color color = Widget.OverlayColor?.Resolve(StatesController.Value)
                      ?? Widget.SplashColor
                      ?? Theme.Of(Context).SplashColor;
        RectCallback? rectCallback = Widget.ContainedInkWell ? Widget.GetRectCallback!(referenceBox) : null;
        BorderRadius? borderRadius = Widget.BorderRadius;
        ShapeBorder? customBorder = Widget.CustomBorder;

        InteractiveInkFeature? splash = null;
        void OnRemoved()
        {
            if (_splashes is not null)
            {
                DebugAssertions.Assert(_splashes.Contains(splash!));
                _splashes.Remove(splash!);
                if (ReferenceEquals(_currentSplash, splash))
                {
                    _currentSplash = null;
                }

                UpdateKeepAlive();
            } // else we're probably in deactivate()
        }

        splash = (Widget.SplashFactory ?? Theme.Of(Context).SplashFactory).Create(
            controller: inkController,
            referenceBox: referenceBox,
            position: position,
            color: color,
            containedInkWell: Widget.ContainedInkWell,
            rectCallback: rectCallback,
            radius: Widget.Radius,
            borderRadius: borderRadius,
            customBorder: customBorder,
            onRemoved: OnRemoved,
            textDirection: Directionality.Of(Context));

        return splash;
    }

    private void HandleFocusHighlightModeChange(FocusHighlightMode mode)
    {
        if (!Mounted)
        {
            return;
        }

        SetState(UpdateFocusHighlights);
    }

    private bool ShouldShowFocus => MediaQuery.MaybeNavigationModeOf(Context) switch
    {
        NavigationMode.Directional => _hasFocus,
        _ => Enabled && _hasFocus,
    };

    private void UpdateFocusHighlights()
    {
        bool showFocus = FocusManager.Instance.HighlightMode switch
        {
            FocusHighlightMode.Touch => false,
            _ => ShouldShowFocus,
        };
        UpdateHighlight(InkHighlightType.Focus, value: showFocus);
    }

    private void HandleFocusUpdate(bool hasFocus)
    {
        _hasFocus = hasFocus;
        // Set here rather than updateHighlight because this widget's
        // (WidgetState) states include WidgetState.focused if
        // the InkWell _has_ the focus, rather than if it's showing
        // the focus per FocusManager.instance.highlightMode.
        StatesController.Update(WidgetState.Focused, hasFocus);
        UpdateFocusHighlights();
        Widget.OnFocusChange?.Invoke(hasFocus);
    }

    internal void HandleAnyTapDown(TapDownDetails details)
    {
        if (AnyChildInkResponsePressed)
        {
            return;
        }

        StartNewSplash(details: details);
    }

    private void HandleTapDown(TapDownDetails details)
    {
        HandleAnyTapDown(details);
        Widget.OnTapDown?.Invoke(details);
    }

    private void HandleTapUp(TapUpDetails details)
    {
        Widget.OnTapUp?.Invoke(details);
    }

    private void HandleSecondaryTapDown(TapDownDetails details)
    {
        HandleAnyTapDown(details);
        Widget.OnSecondaryTapDown?.Invoke(details);
    }

    private void HandleSecondaryTapUp(TapUpDetails details)
    {
        Widget.OnSecondaryTapUp?.Invoke(details);
    }

    private void StartNewSplash(TapDownDetails? details = null, BuildContext? context = null)
    {
        DebugAssertions.Assert(details is not null || context is not null);

        Point globalPosition;
        if (context is not null)
        {
            var referenceBox = (RenderBox)context.FindRenderObject()!;
            DebugAssertions.Assert(
                referenceBox.HasSize,
                "InkResponse must be done with layout before starting a splash.");
            globalPosition = referenceBox.LocalToGlobal(referenceBox.PaintBounds.Center);
        }
        else
        {
            globalPosition = details!.GlobalPosition;
        }

        StatesController.Update(WidgetState.Pressed, true); // ... before creating the splash
        InteractiveInkFeature splash = CreateSplash(globalPosition);
        _splashes ??= [];
        _splashes.Add(splash);
        _currentSplash?.Cancel();
        _currentSplash = splash;
        UpdateKeepAlive();
        UpdateHighlight(InkHighlightType.Pressed, value: true);
    }

    private void HandleTap()
    {
        _currentSplash?.Confirm();
        _currentSplash = null;
        UpdateHighlight(InkHighlightType.Pressed, value: false);
        if (Widget.OnTap is not null)
        {
            if (Widget.EnableFeedback)
            {
                _ = Feedback.ForTap(Context);
            }

            Widget.OnTap?.Invoke();
        }
    }

    private void HandleTapCancel()
    {
        _currentSplash?.Cancel();
        _currentSplash = null;
        Widget.OnTapCancel?.Invoke();
        UpdateHighlight(InkHighlightType.Pressed, value: false);
    }

    private void HandleDoubleTap()
    {
        _currentSplash?.Confirm();
        _currentSplash = null;
        UpdateHighlight(InkHighlightType.Pressed, value: false);
        Widget.OnDoubleTap?.Invoke();
    }

    private void HandleLongPress()
    {
        _currentSplash?.Confirm();
        _currentSplash = null;
        if (Widget.OnLongPress is not null)
        {
            if (Widget.EnableFeedback)
            {
                _ = Feedback.ForLongPress(Context);
            }

            Widget.OnLongPress!();
        }
    }

    private void HandleLongPressUp()
    {
        _currentSplash?.Confirm();
        _currentSplash = null;
        Widget.OnLongPressUp?.Invoke();
    }

    private void HandleSecondaryTap()
    {
        _currentSplash?.Confirm();
        _currentSplash = null;
        UpdateHighlight(InkHighlightType.Pressed, value: false);
        Widget.OnSecondaryTap?.Invoke();
    }

    private void HandleSecondaryTapCancel()
    {
        _currentSplash?.Cancel();
        _currentSplash = null;
        Widget.OnSecondaryTapCancel?.Invoke();
        UpdateHighlight(InkHighlightType.Pressed, value: false);
    }

    public override void Deactivate()
    {
        if (_splashes is not null)
        {
            HashSet<InteractiveInkFeature> splashes = _splashes;
            _splashes = null;
            foreach (InteractiveInkFeature splash in splashes)
            {
                splash.Dispose();
            }

            _currentSplash = null;
        }

        DebugAssertions.Assert(_currentSplash is null);
        foreach (InkHighlightType highlight in _highlights.Keys.ToList())
        {
            HighlightOf(highlight)?.Dispose();
            _highlights[highlight] = null;
        }

        Widget.ParentState?.MarkChildInkResponsePressed(this, false);
        base.Deactivate();
    }

    private static bool IsWidgetEnabled(InkResponseStateWidget widget) =>
        PrimaryButtonEnabled(widget) || SecondaryButtonEnabled(widget);

    private static bool PrimaryButtonEnabled(InkResponseStateWidget widget) =>
        widget.OnTap is not null
        || widget.OnDoubleTap is not null
        || widget.OnLongPress is not null
        || widget.OnLongPressUp is not null
        || widget.OnTapUp is not null
        || widget.OnTapDown is not null;

    private static bool SecondaryButtonEnabled(InkResponseStateWidget widget) =>
        widget.OnSecondaryTap is not null
        || widget.OnSecondaryTapUp is not null
        || widget.OnSecondaryTapDown is not null;

    internal bool Enabled => IsWidgetEnabled(Widget);

    private bool PrimaryEnabled => PrimaryButtonEnabled(Widget);

    private bool SecondaryEnabled => SecondaryButtonEnabled(Widget);

    private void HandleMouseEnter(PointerEnterEvent @event)
    {
        _hovering = true;
        if (Enabled)
        {
            HandleHoverChange();
        }
    }

    private void HandleMouseExit(PointerExitEvent @event)
    {
        _hovering = false;
        // If the exit occurs after we've been disabled, we still
        // want to take down the highlights and run widget.onHover.
        HandleHoverChange();
    }

    private void HandleHoverChange()
    {
        UpdateHighlight(InkHighlightType.Hover, value: _hovering);
    }

    private bool CanRequestFocus => MediaQuery.MaybeNavigationModeOf(Context) switch
    {
        NavigationMode.Directional => true,
        _ => Enabled && Widget.CanRequestFocus,
    };

    public override Widget Build(BuildContext context)
    {
        DebugAssertions.Assert(Widget.DebugCheckContext(context));
        // Dart's `super.build(context)` from AutomaticKeepAliveClientMixin.
        if (WantKeepAlive)
        {
            EnsureKeepAlive();
        }

        IReadOnlySet<WidgetState> nonHighlightable = StatesController.Value
            .Except([WidgetState.Focused, WidgetState.Hovered, WidgetState.Pressed])
            .ToHashSet();
        var pressed = new HashSet<WidgetState>(nonHighlightable) { WidgetState.Pressed };
        var focused = new HashSet<WidgetState>(nonHighlightable) { WidgetState.Focused };
        var hovered = new HashSet<WidgetState>(nonHighlightable) { WidgetState.Hovered };

        Color GetHighlightColorForTypeInBuild(InkHighlightType type)
        {
            return type switch
            {
                // The pressed state triggers a ripple (ink splash), per the current
                // Material Design spec. A separate highlight is no longer used.
                // See https://material.io/design/interaction/states.html#pressed
                InkHighlightType.Pressed => Widget.OverlayColor?.Resolve(pressed)
                                            ?? Widget.HighlightColor
                                            ?? Theme.Of(context).HighlightColor,
                InkHighlightType.Focus => Widget.OverlayColor?.Resolve(focused)
                                          ?? Widget.FocusColor
                                          ?? Theme.Of(context).FocusColor,
                _ => Widget.OverlayColor?.Resolve(hovered)
                     ?? Widget.HoverColor
                     ?? Theme.Of(context).HoverColor,
            };
        }

        foreach (InkHighlightType type in _highlights.Keys)
        {
            InkHighlight? highlight = HighlightOf(type);
            if (highlight is not null)
            {
                highlight.Color = GetHighlightColorForTypeInBuild(type);
            }
        }

        if (_currentSplash is not null)
        {
            _currentSplash.Color = Widget.OverlayColor?.Resolve(StatesController.Value)
                                   ?? Widget.SplashColor
                                   ?? Theme.Of(context).SplashColor;
        }

        // Dart's resolve is non-nullable; a C# resolver returning null falls back to the basic cursor.
        MouseCursor effectiveMouseCursor = WidgetStateProperty<MouseCursor?>.ResolveAs(
            Widget.MouseCursor ?? WidgetStateMouseCursor.AdaptiveClickable,
            StatesController.Value) ?? SystemMouseCursors.Basic;

        return new ParentInkResponseProvider(
            state: this,
            child: new Actions(
                actions: ActionMap,
                child: new Focus(
                    focusNode: Widget.FocusNode,
                    canRequestFocus: CanRequestFocus,
                    onFocusChange: HandleFocusUpdate,
                    autofocus: Widget.Autofocus,
                    child: new MouseRegion(
                        cursor: effectiveMouseCursor,
                        onEnter: HandleMouseEnter,
                        onExit: HandleMouseExit,
                        child: DefaultSelectionStyle.Merge(
                            mouseCursor: effectiveMouseCursor,
                            child: new Semantics(
                                onTap: Widget.ExcludeFromSemantics || Widget.OnTap is null
                                    ? null
                                    : () => SimulateTap(),
                                onLongPress: Widget.ExcludeFromSemantics || Widget.OnLongPress is null
                                    ? null
                                    : SimulateLongPress,
                                child: new GestureDetector(
                                    onTapDown: PrimaryEnabled ? HandleTapDown : null,
                                    onTapUp: PrimaryEnabled ? HandleTapUp : null,
                                    onTap: PrimaryEnabled ? HandleTap : null,
                                    onTapCancel: PrimaryEnabled ? HandleTapCancel : null,
                                    onDoubleTap: Widget.OnDoubleTap is not null ? HandleDoubleTap : null,
                                    onLongPress: Widget.OnLongPress is not null ? HandleLongPress : null,
                                    onLongPressUp: Widget.OnLongPressUp is not null ? HandleLongPressUp : null,
                                    onSecondaryTapDown: SecondaryEnabled ? HandleSecondaryTapDown : null,
                                    onSecondaryTapUp: SecondaryEnabled ? HandleSecondaryTapUp : null,
                                    onSecondaryTap: SecondaryEnabled ? HandleSecondaryTap : null,
                                    onSecondaryTapCancel: SecondaryEnabled ? HandleSecondaryTapCancel : null,
                                    behavior: HitTestBehavior.Opaque,
                                    excludeFromSemantics: true,
                                    child: Widget.Child)))))));
    }
}

/// <summary>A rectangular area of a <see cref="Material"/> that responds to touch.</summary>
/// <remarks>
/// An <see cref="InkResponse"/> with <see cref="InkResponse.ContainedInkWell"/> true and a rectangular
/// <see cref="InkResponse.HighlightShape"/>: its splashes are clipped to its bounds.
/// </remarks>
public class InkWell : InkResponse
{
    /// <summary>Creates an ink well.</summary>
    public InkWell(
        Widget? child = null,
        Action? onTap = null,
        Action? onDoubleTap = null,
        Action? onLongPress = null,
        Action? onLongPressUp = null,
        Action<TapDownDetails>? onTapDown = null,
        Action<TapUpDetails>? onTapUp = null,
        Action? onTapCancel = null,
        Action? onSecondaryTap = null,
        Action<TapUpDetails>? onSecondaryTapUp = null,
        Action<TapDownDetails>? onSecondaryTapDown = null,
        Action? onSecondaryTapCancel = null,
        Action<bool>? onHighlightChanged = null,
        Action<bool>? onHover = null,
        MouseCursor? mouseCursor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        Color? highlightColor = null,
        WidgetStateProperty<Color?>? overlayColor = null,
        Color? splashColor = null,
        InteractiveInkFeatureFactory? splashFactory = null,
        double? radius = null,
        BorderRadius? borderRadius = null,
        ShapeBorder? customBorder = null,
        bool enableFeedback = true,
        bool excludeFromSemantics = false,
        FocusNode? focusNode = null,
        bool canRequestFocus = true,
        Action<bool>? onFocusChange = null,
        bool autofocus = false,
        WidgetStatesController? statesController = null,
        TimeSpan? hoverDuration = null,
        Key? key = null)
        : base(
            child: child,
            onTap: onTap,
            onDoubleTap: onDoubleTap,
            onLongPress: onLongPress,
            onLongPressUp: onLongPressUp,
            onTapDown: onTapDown,
            onTapUp: onTapUp,
            onTapCancel: onTapCancel,
            onSecondaryTap: onSecondaryTap,
            onSecondaryTapUp: onSecondaryTapUp,
            onSecondaryTapDown: onSecondaryTapDown,
            onSecondaryTapCancel: onSecondaryTapCancel,
            onHighlightChanged: onHighlightChanged,
            onHover: onHover,
            mouseCursor: mouseCursor,
            containedInkWell: true,
            highlightShape: BoxShape.Rectangle,
            focusColor: focusColor,
            hoverColor: hoverColor,
            highlightColor: highlightColor,
            overlayColor: overlayColor,
            splashColor: splashColor,
            splashFactory: splashFactory,
            radius: radius,
            borderRadius: borderRadius,
            customBorder: customBorder,
            enableFeedback: enableFeedback,
            excludeFromSemantics: excludeFromSemantics,
            focusNode: focusNode,
            canRequestFocus: canRequestFocus,
            onFocusChange: onFocusChange,
            autofocus: autofocus,
            statesController: statesController,
            hoverDuration: hoverDuration,
            key: key)
    {
    }
}
