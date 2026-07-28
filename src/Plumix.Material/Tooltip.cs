using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/tooltip.dart

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
        TooltipPositionDelegate? positionDelegate = null,
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
        PositionDelegate = positionDelegate;
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

    public TooltipPositionDelegate? PositionDelegate { get; }

    public static bool DismissAllToolTips() => RawTooltip.DismissAllToolTips();

    public override State CreateState() => new TooltipState();

    private static void ValidateFiniteNonNegative(double? value, string parameterName)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Tooltip values must be finite and non-negative.");
        }
    }

    private static void ValidateDuration(TimeSpan? value, string parameterName)
    {
        if (value.HasValue && value.Value < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Tooltip durations must be non-negative.");
        }
    }
}

public sealed class TooltipState : State
{
    private static readonly TimeSpan DefaultShowDuration = TimeSpan.FromMilliseconds(1500);
    private static readonly TimeSpan DefaultExitDuration = TimeSpan.FromMilliseconds(100);
    private GlobalKey<RawTooltipState> _tooltipKey = null!;
    private TooltipThemeData _tooltipTheme = new();
    private ThemeData _theme = ThemeData.Light;
    private bool _visible;

    private Tooltip CurrentWidget => (Tooltip)StateWidget;

    public override void InitState()
    {
        base.InitState();
        _tooltipKey = new GlobalObjectKey<RawTooltipState>(this);
    }

    public override Widget Build(BuildContext context)
    {
        _theme = Theme.Of(context);
        _tooltipTheme = TooltipTheme.Of(context);
        _visible = TooltipVisibility.Of(context);
        string message = CurrentWidget.Message ?? string.Empty;
        if (message.Length == 0)
        {
            return CurrentWidget.Child ?? new SizedBox();
        }

        Widget effectiveChild = CurrentWidget.Child ?? new SizedBox();
        if (CurrentWidget.MouseCursor is not null)
        {
            effectiveChild = new MouseRegion(
                cursor: CurrentWidget.MouseCursor,
                child: effectiveChild);
        }

        bool excludeFromSemantics = CurrentWidget.ExcludeFromSemantics
                                    ?? _tooltipTheme.ExcludeFromSemantics
                                    ?? false;
        if (!_visible)
        {
            return effectiveChild;
        }

        return new RawTooltip(
            key: _tooltipKey,
            semanticsTooltip: excludeFromSemantics ? null : message,
            tooltipBuilder: (_, animation) => new FadeTransition(
                opacity: animation,
                child: BuildBubble(message)),
            hoverDelay: CurrentWidget.WaitDuration
                        ?? _tooltipTheme.WaitDuration
                        ?? TimeSpan.Zero,
            touchDelay: CurrentWidget.ShowDuration
                        ?? _tooltipTheme.ShowDuration
                        ?? DefaultShowDuration,
            dismissDelay: CurrentWidget.ExitDuration
                          ?? _tooltipTheme.ExitDuration
                          ?? DefaultExitDuration,
            enableTapToDismiss: CurrentWidget.EnableTapToDismiss,
            triggerMode: CurrentWidget.TriggerMode
                         ?? _tooltipTheme.TriggerMode
                         ?? TooltipTriggerMode.LongPress,
            enableFeedback: CurrentWidget.EnableFeedback
                            ?? _tooltipTheme.EnableFeedback
                            ?? true,
            onTriggered: CurrentWidget.OnTriggered,
            positionDelegate: ResolvePosition,
            ignorePointer: CurrentWidget.IgnorePointer ?? true,
            child: effectiveChild);
    }

    public bool EnsureTooltipVisible()
    {
        return _visible && (_tooltipKey.CurrentState?.EnsureTooltipVisible() ?? false);
    }

    private Widget BuildBubble(string message)
    {
        bool desktop = _theme.Platform is TargetPlatform.MacOS
            or TargetPlatform.Linux
            or TargetPlatform.Windows;
        double defaultHeight = desktop ? 24.0 : 32.0;
        var defaultPadding = desktop ? new Thickness(8, 4) : new Thickness(16, 4);
        Color foreground = _theme.Brightness == Brightness.Dark ? Colors.Black : Colors.White;
        Color background = _theme.Brightness == Brightness.Dark
            ? Color.FromArgb(0xE6, 0xFF, 0xFF, 0xFF)
            : Color.FromArgb(0xE6, 0x61, 0x61, 0x61);
        TextStyle defaultTextStyle = _theme.TextTheme.BodyMedium with
        {
            Color = foreground,
            FontSize = desktop ? 12 : 14,
        };
        BoxConstraints constraints = CurrentWidget.Constraints
                                     ?? _tooltipTheme.Constraints
                                     ?? new BoxConstraints(
                                         MinHeight: CurrentWidget.Height
                                                    ?? _tooltipTheme.Height
                                                    ?? defaultHeight);
        TextStyle style = CurrentWidget.TextStyle
                          ?? _tooltipTheme.TextStyle
                          ?? defaultTextStyle;
        TextAlign textAlign = CurrentWidget.TextAlign
                              ?? _tooltipTheme.TextAlign
                              ?? Plumix.UI.TextAlign.Start;
        BoxDecoration decoration = CurrentWidget.Decoration
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

    private Point ResolvePosition(TooltipPositionContext context)
    {
        double verticalOffset = CurrentWidget.VerticalOffset
                                ?? _tooltipTheme.VerticalOffset
                                ?? 24.0;
        bool preferBelow = CurrentWidget.PreferBelow
                           ?? _tooltipTheme.PreferBelow
                           ?? true;
        var resolvedContext = context with
        {
            VerticalOffset = verticalOffset,
            PreferBelow = preferBelow,
        };
        return CurrentWidget.PositionDelegate?.Invoke(resolvedContext)
               ?? RawTooltipPositionLayoutDelegate.PositionDependentBox(
                   size: context.OverlaySize,
                   childSize: context.TooltipSize,
                   target: context.Target,
                   preferBelow: preferBelow,
                   verticalOffset: verticalOffset);
    }
}
