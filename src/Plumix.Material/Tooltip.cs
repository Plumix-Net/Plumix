using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/tooltip.dart

public delegate void TooltipTriggeredCallback();

public sealed class Tooltip : StatefulWidget
{
    public Tooltip(
        string? message = null,
        Widget? child = null,
        double? height = null,
        BoxConstraints? constraints = null,
        Thickness? padding = null,
        Thickness? margin = null,
        double? verticalOffset = null,
        bool? preferBelow = null,
        bool? excludeFromSemantics = null,
        BoxDecoration? decoration = null,
        TextStyle? textStyle = null,
        TextAlign? textAlign = null,
        TimeSpan? waitDuration = null,
        TimeSpan? showDuration = null,
        TimeSpan? exitDuration = null,
        bool enableTapToDismiss = true,
        TooltipTriggerMode? triggerMode = null,
        bool? enableFeedback = null,
        TooltipTriggeredCallback? onTriggered = null,
        MouseCursor? mouseCursor = null,
        bool? ignorePointer = null,
        Key? key = null) : base(key)
    {
        if (height.HasValue && constraints.HasValue)
        {
            throw new ArgumentException("Only one of height and constraints may be specified.");
        }

        ValidateFiniteNonNegative(height, nameof(height));
        ValidateFiniteNonNegative(verticalOffset, nameof(verticalOffset));
        ValidateDuration(waitDuration, nameof(waitDuration));
        ValidateDuration(showDuration, nameof(showDuration));
        ValidateDuration(exitDuration, nameof(exitDuration));

        Message = message;
        Child = child;
        Height = height;
        Constraints = constraints;
        Padding = padding;
        Margin = margin;
        VerticalOffset = verticalOffset;
        PreferBelow = preferBelow;
        ExcludeFromSemantics = excludeFromSemantics;
        Decoration = decoration;
        TextStyle = textStyle;
        TextAlign = textAlign;
        WaitDuration = waitDuration;
        ShowDuration = showDuration;
        ExitDuration = exitDuration;
        EnableTapToDismiss = enableTapToDismiss;
        TriggerMode = triggerMode;
        EnableFeedback = enableFeedback;
        OnTriggered = onTriggered;
        MouseCursor = mouseCursor;
        IgnorePointer = ignorePointer;
    }

    public string? Message { get; }
    public Widget? Child { get; }
    public double? Height { get; }
    public BoxConstraints? Constraints { get; }
    public Thickness? Padding { get; }
    public Thickness? Margin { get; }
    public double? VerticalOffset { get; }
    public bool? PreferBelow { get; }
    public bool? ExcludeFromSemantics { get; }
    public BoxDecoration? Decoration { get; }
    public TextStyle? TextStyle { get; }
    public TextAlign? TextAlign { get; }
    public TimeSpan? WaitDuration { get; }
    public TimeSpan? ShowDuration { get; }
    public TimeSpan? ExitDuration { get; }
    public bool EnableTapToDismiss { get; }
    public TooltipTriggerMode? TriggerMode { get; }
    public bool? EnableFeedback { get; }
    public TooltipTriggeredCallback? OnTriggered { get; }
    public MouseCursor? MouseCursor { get; }
    public bool? IgnorePointer { get; }

    public static bool DismissAllToolTips() => TooltipState.DismissAll();

    public override State CreateState() => new TooltipState();

    private static void ValidateFiniteNonNegative(double? value, string name)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0))
        {
            throw new ArgumentOutOfRangeException(name, "Tooltip values must be finite and non-negative.");
        }
    }

    private static void ValidateDuration(TimeSpan? value, string name)
    {
        if (value.HasValue && value.Value < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(name, "Tooltip durations must be non-negative.");
        }
    }
}

public sealed class TooltipState : State
{
    private static readonly object RegistrySync = new();
    private static readonly HashSet<TooltipState> Registry = [];
    private static readonly TimeSpan FadeDuration = TimeSpan.FromMilliseconds(150);
    private static readonly TimeSpan DefaultShowDuration = TimeSpan.FromMilliseconds(1500);
    private static readonly TimeSpan DefaultExitDuration = TimeSpan.FromMilliseconds(100);

    private AnimationController? _fadeController;
    private AnimationController? _showTimer;
    private AnimationController? _hideTimer;
    private IDisposable? _cursorHandle;
    private TooltipThemeData _tooltipTheme = new();
    private ThemeData _theme = ThemeData.Light;
    private bool _isMounted;
    private bool _isShown;
    private bool _isVisible = true;

    private Tooltip CurrentWidget => (Tooltip)StateWidget;

    public override void InitState()
    {
        _fadeController = new AnimationController(FadeDuration) { Curve = Curves.EaseOut };
        _fadeController.Changed += HandleAnimationTick;
        _fadeController.Dismissed += HandleAnimationDismissed;
        _isMounted = true;
        lock (RegistrySync)
        {
            Registry.Add(this);
        }
    }

    public override void Dispose()
    {
        _isMounted = false;
        CancelTimer(ref _showTimer);
        CancelTimer(ref _hideTimer);
        _cursorHandle?.Dispose();
        _cursorHandle = null;
        lock (RegistrySync)
        {
            Registry.Remove(this);
        }

        if (_fadeController is not null)
        {
            _fadeController.Changed -= HandleAnimationTick;
            _fadeController.Dismissed -= HandleAnimationDismissed;
            _fadeController.Dispose();
            _fadeController = null;
        }
    }

    public override Widget Build(BuildContext context)
    {
        _theme = Theme.Of(context);
        _tooltipTheme = TooltipTheme.Of(context);
        _isVisible = TooltipVisibility.Of(context);
        if (!_isVisible && _isShown)
        {
            CancelTimer(ref _showTimer);
            CancelTimer(ref _hideTimer);
            _fadeController?.Stop();
            _isShown = false;
        }

        string message = CurrentWidget.Message ?? string.Empty;
        if (message.Length == 0)
        {
            return CurrentWidget.Child ?? new SizedBox();
        }

        Widget child = CurrentWidget.Child ?? new SizedBox();
        bool excludeFromSemantics = CurrentWidget.ExcludeFromSemantics
                                    ?? _tooltipTheme.ExcludeFromSemantics
                                    ?? false;
        if (!excludeFromSemantics)
        {
            child = new Semantics(label: message, child: child);
        }

        var triggerMode = CurrentWidget.TriggerMode
                          ?? _tooltipTheme.TriggerMode
                          ?? TooltipTriggerMode.LongPress;
        child = new GestureDetector(
            behavior: HitTestBehavior.DeferToChild,
            onTap: triggerMode == TooltipTriggerMode.Tap ? HandleTapTrigger : null,
            onLongPress: triggerMode == TooltipTriggerMode.LongPress ? HandleLongPressTrigger : null,
            child: child);
        child = new Listener(
            behavior: HitTestBehavior.DeferToChild,
            onPointerEnter: _ => HandlePointerEnter(),
            onPointerExit: _ => HandlePointerExit(),
            onPointerDown: _ => HandlePointerDown(),
            child: child);

        double opacity = _fadeController?.Evaluate() ?? 0;
        if (!_isShown && opacity <= 0)
        {
            return child;
        }

        bool preferBelow = CurrentWidget.PreferBelow ?? _tooltipTheme.PreferBelow ?? true;
        double verticalOffset = CurrentWidget.VerticalOffset ?? _tooltipTheme.VerticalOffset ?? 24.0;
        var bubble = new Opacity(opacity, BuildBubble(message));
        var positionedBubble = preferBelow
            ? new Positioned(
                left: 0,
                right: 0,
                bottom: -verticalOffset,
                child: new Center(child: bubble))
            : new Positioned(
                left: 0,
                right: 0,
                top: -verticalOffset,
                child: new Center(child: bubble));

        return new Stack(children: [child, positionedBubble]);
    }

    public bool EnsureTooltipVisible()
    {
        if (!_isMounted || !_isVisible || _isShown)
        {
            return false;
        }

        CancelTimer(ref _showTimer);
        CancelTimer(ref _hideTimer);
        ShowTooltip(triggered: false);
        return true;
    }

    internal static bool DismissAll()
    {
        TooltipState[] states;
        lock (RegistrySync)
        {
            states = [.. Registry];
        }

        bool dismissed = false;
        foreach (var state in states)
        {
            dismissed |= state.DismissTooltip(immediate: false);
        }

        return dismissed;
    }

    private Widget BuildBubble(string message)
    {
        bool desktop = _theme.Platform is TargetPlatform.MacOS or TargetPlatform.Linux or TargetPlatform.Windows;
        double defaultHeight = desktop ? 24.0 : 32.0;
        var defaultPadding = desktop ? new Thickness(8, 4) : new Thickness(16, 4);
        var foreground = _theme.Brightness == Brightness.Dark ? Colors.Black : Colors.White;
        var background = _theme.Brightness == Brightness.Dark
            ? Color.FromArgb(0xE6, 0xFF, 0xFF, 0xFF)
            : Color.FromArgb(0xE6, 0x61, 0x61, 0x61);
        var defaultTextStyle = _theme.TextTheme.BodyMedium with
        {
            Color = foreground,
            FontSize = desktop ? 12 : 14,
        };
        var constraints = CurrentWidget.Constraints
                          ?? _tooltipTheme.Constraints
                          ?? new BoxConstraints(
                              MinHeight: CurrentWidget.Height ?? _tooltipTheme.Height ?? defaultHeight);
        var style = CurrentWidget.TextStyle ?? _tooltipTheme.TextStyle ?? defaultTextStyle;
        var textAlign = CurrentWidget.TextAlign ?? _tooltipTheme.TextAlign ?? Plumix.UI.TextAlign.Start;
        var decoration = CurrentWidget.Decoration
                         ?? _tooltipTheme.Decoration
                         ?? new BoxDecoration(
                             Color: background,
                             BorderRadius: BorderRadius.Circular(4));

        Widget bubble = new Center(
            widthFactor: 1,
            heightFactor: 1,
            child: new Text(
                message,
                textAlign: textAlign,
                textDirection: TextDirection.Ltr));
        bubble = new Container(
            decoration: decoration,
            padding: CurrentWidget.Padding ?? _tooltipTheme.Padding ?? defaultPadding,
            margin: CurrentWidget.Margin ?? _tooltipTheme.Margin ?? new Thickness(),
            child: bubble);
        bubble = new DefaultTextStyle(style, bubble);
        return new ConstrainedBox(constraints, bubble);
    }

    private void HandlePointerEnter()
    {
        if (!_isVisible)
        {
            return;
        }

        _cursorHandle?.Dispose();
        _cursorHandle = CurrentWidget.MouseCursor is null
            ? null
            : MouseCursorManager.PushCursor(CurrentWidget.MouseCursor);
        CancelTimer(ref _hideTimer);
        Schedule(
            ref _showTimer,
            CurrentWidget.WaitDuration ?? _tooltipTheme.WaitDuration ?? TimeSpan.Zero,
            () => ShowTooltip(triggered: true));
    }

    private void HandlePointerExit()
    {
        _cursorHandle?.Dispose();
        _cursorHandle = null;
        CancelTimer(ref _showTimer);
        Schedule(
            ref _hideTimer,
            CurrentWidget.ExitDuration ?? _tooltipTheme.ExitDuration ?? DefaultExitDuration,
            () => DismissTooltip(immediate: false));
    }

    private void HandlePointerDown()
    {
        if (!_isVisible)
        {
            return;
        }

        if (_isShown && CurrentWidget.EnableTapToDismiss)
        {
            DismissTooltip(immediate: false);
        }
    }

    private void HandleTapTrigger()
    {
        TriggerTooltip(FeedbackType.Tap);
    }

    private void HandleLongPressTrigger()
    {
        TriggerTooltip(FeedbackType.LongPress);
    }

    private void TriggerTooltip(FeedbackType feedbackType)
    {
        if (!_isVisible)
        {
            return;
        }

        ShowTooltip(triggered: true);
        if (CurrentWidget.EnableFeedback ?? _tooltipTheme.EnableFeedback ?? true)
        {
            if (feedbackType == FeedbackType.LongPress)
            {
                Feedback.ForLongPress();
            }
            else
            {
                Feedback.ForTap();
            }
        }

        Schedule(
            ref _hideTimer,
            CurrentWidget.ShowDuration ?? _tooltipTheme.ShowDuration ?? DefaultShowDuration,
            () => DismissTooltip(immediate: false));
    }

    private void ShowTooltip(bool triggered)
    {
        if (!_isMounted || !_isVisible)
        {
            return;
        }

        CancelTimer(ref _hideTimer);
        if (!_isShown)
        {
            SetState(() => _isShown = true);
            _fadeController?.Forward(from: _fadeController.Value);
            if (triggered)
            {
                CurrentWidget.OnTriggered?.Invoke();
            }
        }
    }

    private bool DismissTooltip(bool immediate)
    {
        CancelTimer(ref _showTimer);
        CancelTimer(ref _hideTimer);
        if (!_isMounted || !_isShown)
        {
            return false;
        }

        if (immediate)
        {
            _fadeController?.Stop();
            SetState(() => _isShown = false);
        }
        else
        {
            _fadeController?.Reverse(from: _fadeController.Value);
        }

        return true;
    }

    private void HandleAnimationTick()
    {
        if (_isMounted)
        {
            SetState(() => { });
        }
    }

    private void HandleAnimationDismissed()
    {
        if (_isMounted)
        {
            SetState(() => _isShown = false);
        }
    }

    private void Schedule(ref AnimationController? slot, TimeSpan delay, Action callback)
    {
        CancelTimer(ref slot);
        if (delay <= TimeSpan.Zero)
        {
            callback();
            return;
        }

        var timer = new AnimationController(delay);
        Action? completed = null;
        completed = () =>
        {
            timer.Completed -= completed;
            timer.Dispose();
            if (_isMounted)
            {
                callback();
            }
        };
        timer.Completed += completed;
        slot = timer;
        timer.Forward(from: 0);
    }

    private static void CancelTimer(ref AnimationController? timer)
    {
        var previous = timer;
        timer = null;
        if (previous is null)
        {
            return;
        }

        previous.Dispose();
    }
}
