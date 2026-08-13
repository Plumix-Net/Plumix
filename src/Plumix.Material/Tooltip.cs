using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/tooltip.dart

public sealed class Tooltip : StatefulWidget
{
    public Tooltip(
        string? message = null,
        InlineSpan? richMessage = null,
        Widget? child = null,
        double? height = null,
        BoxConstraints? constraints = null,
        EdgeInsetsGeometry? padding = null,
        EdgeInsetsGeometry? margin = null,
        double? verticalOffset = null,
        bool? preferBelow = null,
        bool? excludeFromSemantics = null,
        Decoration? decoration = null,
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

        if ((message is null) == (richMessage is null))
        {
            throw new ArgumentException(
                "Either `message` or `richMessage` must be specified, but not both.",
                nameof(richMessage));
        }

        Message = message;
        RichMessage = richMessage;
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

    /// The text to display in the tooltip.
    ///
    /// Only one of [Message] and [RichMessage] may be non-null.
    public string? Message { get; }

    /// The rich text to display in the tooltip.
    ///
    /// Only one of [Message] and [RichMessage] may be non-null.
    public InlineSpan? RichMessage { get; }

    public Widget? Child { get; }

    public double? Height { get; }

    public BoxConstraints? Constraints { get; }

    public EdgeInsetsGeometry? Padding { get; }

    public EdgeInsetsGeometry? Margin { get; }

    public double? VerticalOffset { get; }

    public bool? PreferBelow { get; }

    public bool? ExcludeFromSemantics { get; }

    public Decoration? Decoration { get; }

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

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new StringProperty("message", Message, showName: Message is null));
        properties.Add(new StringProperty(
            "richMessage",
            RichMessage?.ToPlainText(),
            showName: RichMessage is null));
        properties.Add(new DoubleProperty("height", Height));
        properties.Add(new DiagnosticsProperty<BoxConstraints?>("constraints", Constraints));
        properties.Add(new DiagnosticsProperty<EdgeInsetsGeometry?>("padding", Padding));
        properties.Add(new DiagnosticsProperty<EdgeInsetsGeometry?>("margin", Margin));
        properties.Add(new DoubleProperty("vertical offset", VerticalOffset));
        properties.Add(new FlagProperty("position", PreferBelow, "below", "above", showName: true));
        properties.Add(new FlagProperty("semantics", ExcludeFromSemantics, "excluded", showName: true));
        properties.Add(new DiagnosticsProperty<TimeSpan?>("wait duration", WaitDuration));
        properties.Add(new DiagnosticsProperty<TimeSpan?>("show duration", ShowDuration));
        properties.Add(new DiagnosticsProperty<TimeSpan?>("exit duration", ExitDuration));
        properties.Add(new DiagnosticsProperty<TooltipTriggerMode?>("triggerMode", TriggerMode));
        properties.Add(new FlagProperty("enableFeedback", EnableFeedback, "true", showName: true));
        properties.Add(new DiagnosticsProperty<TextAlign?>("textAlign", TextAlign));
        properties.Add(new DiagnosticsProperty<TooltipPositionDelegate?>("positionDelegate", PositionDelegate));
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
        InlineSpan richMessage = CurrentWidget.RichMessage
                                 ?? new TextSpan(text: CurrentWidget.Message ?? string.Empty);
        string message = richMessage.ToPlainText();
        if (message.Length == 0)
        {
            return CurrentWidget.Child ?? new SizedBox();
        }

        Widget effectiveChild = new MouseRegion(
            cursor: CurrentWidget.MouseCursor ?? MouseCursor.Defer,
            child: CurrentWidget.Child ?? new SizedBox());

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
                child: BuildBubble(richMessage)),
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
            ignorePointer: CurrentWidget.IgnorePointer ?? CurrentWidget.Message is not null,
            child: effectiveChild);
    }

    public bool EnsureTooltipVisible()
    {
        return _visible && (_tooltipKey.CurrentState?.EnsureTooltipVisible() ?? false);
    }

    private Widget BuildBubble(InlineSpan richMessage)
    {
        bool desktop = _theme.Platform is TargetPlatform.MacOS
            or TargetPlatform.Linux
            or TargetPlatform.Windows;
        double defaultHeight = desktop ? 24.0 : 32.0;
        EdgeInsetsGeometry defaultPadding = desktop
            ? EdgeInsetsGeometry.Symmetric(horizontal: 8, vertical: 4)
            : EdgeInsetsGeometry.Symmetric(horizontal: 16, vertical: 4);
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
        Decoration decoration = CurrentWidget.Decoration
                                ?? _tooltipTheme.Decoration
                                ?? new BoxDecoration(
                                    Color: background,
                                    BorderRadius: BorderRadius.Circular(4));

        Widget bubble = new Center(
            widthFactor: 1,
            heightFactor: 1,
            child: Text.Rich(
                richMessage,
                style: style,
                textAlign: textAlign));
        bubble = new Container(
            decoration: decoration,
            padding: CurrentWidget.Padding ?? _tooltipTheme.Padding ?? defaultPadding,
            margin: CurrentWidget.Margin ?? _tooltipTheme.Margin ?? EdgeInsetsGeometry.Zero,
            child: bubble);
        bubble = new DefaultTextStyle(style, bubble, textAlign: textAlign);
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
