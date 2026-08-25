using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/dialog.dart

/// <summary>The file-level constants of Dart's `cupertino/dialog.dart` (dialogs and action sheets).</summary>
internal static class CupertinoDialogConstants
{
    public static readonly TextStyle TitleStyle = new(
        FontFamily: new FontFamily("CupertinoSystemText"),
        Inherit: false,
        FontSize: 17.0,
        FontWeight: FontWeight.SemiBold,
        Height: 1.3,
        LetterSpacing: -0.5,
        TextBaseline: TextBaseline.Alphabetic);

    public static readonly TextStyle ContentStyle = new(
        FontFamily: new FontFamily("CupertinoSystemText"),
        Inherit: false,
        FontSize: 13.0,
        FontWeight: FontWeight.Normal,
        Height: 1.35,
        LetterSpacing: -0.2,
        TextBaseline: TextBaseline.Alphabetic);

    public static readonly TextStyle ActionStyle = new(
        FontFamily: new FontFamily("CupertinoSystemText"),
        Inherit: false,
        FontSize: 16.8,
        FontWeight: FontWeight.Normal,
        TextBaseline: TextBaseline.Alphabetic);

    /// <summary>Dart's `_kActionSheetActionStyle`: the base style of an action sheet button label.</summary>
    public static readonly TextStyle ActionSheetActionStyle = new(
        FontFamily: new FontFamily("CupertinoSystemDisplay"),
        Inherit: false,
        FontSize: 17.0,
        FontWeight: FontWeight.Normal,
        TextBaseline: TextBaseline.Alphabetic);

    /// <summary>Dart's `_kActionSheetContentStyle`: the base style of the title/message section.</summary>
    public static readonly TextStyle ActionSheetContentStyle = new(
        FontFamily: new FontFamily("CupertinoSystemText"),
        Inherit: false,
        FontSize: 13.0,
        FontWeight: FontWeight.Normal,
        TextBaseline: TextBaseline.Alphabetic);

    public const double CornerRadius = 14.0;
    public const double DividerThickness = 0.3;
    public const double DialogWidth = 270.0;
    public const double AccessibilityDialogWidth = 310.0;
    public const double DialogEdgePadding = 20.0;
    public const double DialogMinButtonHeight = 45.0;
    public const double DialogMinButtonFontSize = 10.0;
    public const double ActionsSectionMinHeight = 67.8;
    public const double MaxRegularTextScaleFactor = 1.4;

    public const double ActionSheetEdgePadding = 8.0;
    public const double ActionSheetCancelButtonPadding = 8.0;
    public const double ActionSheetContentHorizontalPadding = 16.0;
    public const double ActionSheetContentVerticalPadding = 13.5;
    public const double ActionSheetActionsSectionMinHeight = 84.0;
    public const double ActionSheetButtonHorizontalPadding = 10.0;

    // The height of an action sheet button is proportional to the font size down to a minimum.
    public const double ActionSheetButtonMinHeight = 57.17;
    public const double ActionSheetButtonVerticalPaddingFactor = 0.4;
    public const double ActionSheetButtonVerticalPaddingBase = 1.8;

    public static readonly CupertinoDynamicColor DialogColor = CupertinoDynamicColor.WithBrightness(
        Color.FromUInt32(0xCCF2F2F2),
        Color.FromUInt32(0xCC2D2D2D));

    public static readonly CupertinoDynamicColor DialogPressedColor = CupertinoDynamicColor.WithBrightness(
        Color.FromUInt32(0xFFE1E1E1),
        Color.FromUInt32(0xFF404040));

    public static readonly CupertinoDynamicColor ActionSheetPressedColor =
        CupertinoDynamicColor.WithBrightness(
            Color.FromUInt32(0xCAE0E0E0),
            Color.FromUInt32(0xC1515151));

    public static readonly CupertinoDynamicColor ActionSheetCancelColor =
        CupertinoDynamicColor.WithBrightness(
            Color.FromUInt32(0xFFFFFFFF),
            Color.FromUInt32(0xFF2C2C2C));

    public static readonly CupertinoDynamicColor ActionSheetCancelPressedColor =
        CupertinoDynamicColor.WithBrightness(
            Color.FromUInt32(0xFFECECEC),
            Color.FromUInt32(0xFF494949));

    public static readonly CupertinoDynamicColor ActionSheetBackgroundColor =
        CupertinoDynamicColor.WithBrightness(
            Color.FromUInt32(0xC8FCFCFC),
            Color.FromUInt32(0xBE292929));

    public static readonly CupertinoDynamicColor ActionSheetContentTextColor =
        CupertinoDynamicColor.WithBrightness(
            Color.FromUInt32(0x851D1D1D),
            Color.FromUInt32(0x96F1F1F1));

    public static readonly CupertinoDynamicColor ActionSheetButtonDividerColor =
        CupertinoDynamicColor.WithBrightness(
            Color.FromUInt32(0xD4C9C9C9),
            Color.FromUInt32(0xD57D7D7D));

    /// <summary>Dart's `_isInAccessibilityMode`: effective text scale beyond 1.4 at 14 logical pixels.</summary>
    public static bool IsInAccessibilityMode(BuildContext context)
    {
        TextScaler? scaler = MediaQuery.MaybeOf(context)?.TextScaler;
        return scaler is not null && scaler.Scale(14.0) > 14.0 * MaxRegularTextScaleFactor;
    }
}

/// <summary>An iOS-style alert dialog.</summary>
public sealed class CupertinoAlertDialog : StatefulWidget
{
    public CupertinoAlertDialog(
        Widget? title = null,
        Widget? content = null,
        IReadOnlyList<Widget>? actions = null,
        ScrollController? scrollController = null,
        ScrollController? actionScrollController = null,
        TimeSpan? insetAnimationDuration = null,
        Curve? insetAnimationCurve = null,
        Key? key = null) : base(key)
    {
        Title = title;
        Content = content;
        Actions = actions ?? [];
        ScrollController = scrollController;
        ActionScrollController = actionScrollController;
        InsetAnimationDuration = insetAnimationDuration ?? TimeSpan.FromMilliseconds(100);
        InsetAnimationCurve = insetAnimationCurve ?? Curves.Decelerate;
    }

    public Widget? Title { get; }
    public Widget? Content { get; }
    public IReadOnlyList<Widget> Actions { get; }
    public ScrollController? ScrollController { get; }
    public ScrollController? ActionScrollController { get; }
    public TimeSpan InsetAnimationDuration { get; }
    public Curve InsetAnimationCurve { get; }

    public override State CreateState() => new CupertinoAlertDialogState();
}

internal sealed class CupertinoAlertDialogState : State
{
    private int? _pressedIndex;
    private ScrollController? _backupScrollController;
    private ScrollController? _backupActionScrollController;

    private CupertinoAlertDialog Current => (CupertinoAlertDialog)StateWidget;

    private ScrollController EffectiveScrollController =>
        Current.ScrollController ?? (_backupScrollController ??= new ScrollController());

    private ScrollController EffectiveActionScrollController =>
        Current.ActionScrollController ?? (_backupActionScrollController ??= new ScrollController());

    public override void Dispose()
    {
        _backupScrollController?.Dispose();
        _backupActionScrollController?.Dispose();
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return new CupertinoUserInterfaceLevel(
            CupertinoUserInterfaceLevelData.Elevated,
            new Builder(elevatedContext => MediaQuery.WithClampedTextScaling(
                elevatedContext,
                maxScaleFactor: double.MaxValue,
                minScaleFactor: 1.0,
                child: new ScrollConfiguration(
                    behavior: ScrollConfiguration.Of(elevatedContext).CopyWith(scrollbars: false),
                    child: new LayoutBuilder((layoutContext, _) => BuildDialog(layoutContext))))));
    }

    private Widget BuildDialog(BuildContext context)
    {
        bool accessibilityMode = CupertinoDialogConstants.IsInAccessibilityMode(context);
        Thickness viewInsets = MediaQuery.ViewInsetsOf(context);
        var effectivePadding = new Thickness(
            viewInsets.Left + 40.0,
            viewInsets.Top + 24.0,
            viewInsets.Right + 40.0,
            viewInsets.Bottom + 24.0);
        Widget dialog = new SizedBox(
            width: accessibilityMode
                ? CupertinoDialogConstants.AccessibilityDialogWidth
                : CupertinoDialogConstants.DialogWidth,
            child: new ActionSheetGestureDetector(
                child: new CupertinoPopupSurface(
                    isSurfacePainted: false,
                    child: new Semantics(
                        role: SemanticsRole.AlertDialog,
                        namesRoute: true,
                        scopesRoute: true,
                        explicitChildNodes: true,
                        label: CupertinoLocalizations.Of(context).AlertDialogLabel,
                        child: new Builder(BuildBody)))));
        return new AnimatedPadding(
            padding: effectivePadding,
            duration: Current.InsetAnimationDuration,
            curve: Current.InsetAnimationCurve,
            child: MediaQuery.RemoveViewInsets(
                context,
                new Center(
                    child: new Padding(
                        new Thickness(0, CupertinoDialogConstants.DialogEdgePadding),
                        dialog)),
                removeLeft: true,
                removeTop: true,
                removeRight: true,
                removeBottom: true));
    }

    private Widget BuildBody(BuildContext context)
    {
        Color dialogColor = CupertinoDialogConstants.DialogColor.ResolveFrom(context);
        return MediaQuery.RemovePadding(
            context,
            new LayoutBuilder((layoutContext, constraints) =>
            {
                Widget? contentSection = BuildContentSection(layoutContext, dialogColor);
                Widget? actionsSection = BuildActionsSection(layoutContext);
                if (actionsSection is null)
                {
                    return contentSection
                           ?? new LimitedBox(
                               maxWidth: 0,
                               child: new SizedBox(width: double.PositiveInfinity, height: 0));
                }

                Widget scrolledActions = new OverscrollBackground(color: dialogColor, child: actionsSection);
                if (contentSection is null)
                {
                    return scrolledActions;
                }

                bool accessibilityMode = CupertinoDialogConstants.IsInAccessibilityMode(layoutContext);
                double actionsMinHeight = accessibilityMode
                    ? (constraints.MaxHeight / 2.0) + CupertinoDialogConstants.DividerThickness
                    : CupertinoDialogConstants.ActionsSectionMinHeight + CupertinoDialogConstants.DividerThickness;
                return new PriorityColumn(
                    top: contentSection,
                    bottom: new Column(
                        mainAxisSize: MainAxisSize.Min,
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        children:
                        [
                            new SizedBox(
                                width: double.PositiveInfinity,
                                child: new CupertinoDialogDivider(
                                    dividerColor: CupertinoDynamicColor.Resolve(
                                        CupertinoColors.Separator, layoutContext),
                                    hiddenColor: dialogColor,
                                    hidden: false)),
                            new Flexible(scrolledActions),
                        ]),
                    bottomMinHeight: actionsMinHeight);
            }),
            removeLeft: true,
            removeTop: true,
            removeRight: true,
            removeBottom: true);
    }

    private Widget? BuildContentSection(BuildContext context, Color dialogColor)
    {
        if (Current.Title is null && Current.Content is null)
        {
            return null;
        }

        double effectiveTextScale = MediaQuery.TextScalerOf(context).Scale(14.0) / 14.0;
        return new ColoredBox(
            dialogColor,
            new CupertinoAlertContentSection(
                title: Current.Title,
                message: Current.Content,
                scrollController: EffectiveScrollController,
                titlePadding: new Thickness(
                    CupertinoDialogConstants.DialogEdgePadding,
                    CupertinoDialogConstants.DialogEdgePadding * effectiveTextScale,
                    CupertinoDialogConstants.DialogEdgePadding,
                    Current.Content is null ? CupertinoDialogConstants.DialogEdgePadding : 1.0),
                messagePadding: new Thickness(
                    CupertinoDialogConstants.DialogEdgePadding,
                    Current.Title is null ? CupertinoDialogConstants.DialogEdgePadding : 1.0,
                    CupertinoDialogConstants.DialogEdgePadding,
                    CupertinoDialogConstants.DialogEdgePadding * effectiveTextScale),
                titleTextStyle: CupertinoDialogConstants.TitleStyle.CopyWith(
                    color: CupertinoDynamicColor.Resolve(CupertinoColors.Label, context)),
                messageTextStyle: CupertinoDialogConstants.ContentStyle.CopyWith(
                    color: CupertinoDynamicColor.Resolve(CupertinoColors.Label, context))));
    }

    private Widget? BuildActionsSection(BuildContext context)
    {
        if (Current.Actions.Count == 0)
        {
            return null;
        }

        return new CupertinoAlertActionSection(
            actions: Current.Actions,
            scrollController: EffectiveActionScrollController,
            pressedIndex: _pressedIndex,
            onPressedUpdate: HandlePressedUpdate);
    }

    private void HandlePressedUpdate(int index, bool pressed)
    {
        if (pressed)
        {
            SetState(() => _pressedIndex = index);
        }
        else if (_pressedIndex == index)
        {
            SetState(() => _pressedIndex = null);
        }
    }
}

/// <summary>A button typically used in a <see cref="CupertinoAlertDialog"/>.</summary>
public sealed class CupertinoDialogAction : StatefulWidget
{
    public CupertinoDialogAction(
        Widget child,
        Action? onPressed = null,
        bool isDefaultAction = false,
        bool isDestructiveAction = false,
        TextStyle? textStyle = null,
        MouseCursor? mouseCursor = null,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        OnPressed = onPressed;
        IsDefaultAction = isDefaultAction;
        IsDestructiveAction = isDestructiveAction;
        TextStyle = textStyle;
        MouseCursor = mouseCursor;
    }

    public Widget Child { get; }
    public Action? OnPressed { get; }
    public bool IsDefaultAction { get; }
    public bool IsDestructiveAction { get; }
    public TextStyle? TextStyle { get; }
    public MouseCursor? MouseCursor { get; }

    public bool Enabled => OnPressed is not null;

    public override State CreateState() => new CupertinoDialogActionState();
}

internal sealed class CupertinoDialogActionState : State, ISlideTarget
{
    private CupertinoDialogAction Current => (CupertinoDialogAction)StateWidget;

    public bool DidEnter(bool fromPointerDown, bool innerEnabled) => Current.Enabled;

    public void DidLeave()
    {
    }

    public void DidConfirm() => Current.OnPressed?.Invoke();

    public override Widget Build(BuildContext context)
    {
        TextStyle style = CupertinoDialogConstants.ActionStyle.CopyWith(
            color: Current.IsDestructiveAction
                ? CupertinoDynamicColor.Resolve(CupertinoColors.SystemRed, context)
                : CupertinoTheme.Of(context).PrimaryColor);
        style = style.Merge(Current.TextStyle);
        if (Current.IsDefaultAction)
        {
            style = style.CopyWith(fontWeight: FontWeight.SemiBold);
        }

        if (!Current.Enabled)
        {
            Color color = style.Color ?? Colors.Black;
            style = style.CopyWith(color: Color.FromArgb((byte)Math.Round(color.A * 0.5), color.R, color.G, color.B));
        }

        TextScaler textScaler = MediaQuery.TextScalerOf(context);
        double fontSize = style.FontSize ?? 14.0;
        double fontSizeToScale = fontSize == 0.0 ? 14.0 : fontSize;
        double effectiveTextScale = textScaler.Scale(fontSizeToScale) / fontSizeToScale;
        double padding = 8.0 * effectiveTextScale;
        bool accessibilityMode = CupertinoDialogConstants.IsInAccessibilityMode(context);

        Widget sizedContent;
        if (accessibilityMode)
        {
            sizedContent = new DefaultTextStyle(style, Current.Child, textAlign: TextAlign.Center);
        }
        else
        {
            double dialogWidth = CupertinoDialogConstants.DialogWidth;
            double fontSizeRatio = textScaler.Scale(fontSizeToScale) / CupertinoDialogConstants.DialogMinButtonFontSize;
            sizedContent = new FittedBox(
                fit: BoxFit.ScaleDown,
                child: new ConstrainedBox(
                    new BoxConstraints(MaxWidth: fontSizeRatio * (dialogWidth - (2.0 * padding))),
                    new Semantics(
                        flags: SemanticsFlags.IsButton,
                        onTap: Current.OnPressed,
                        mergeDescendants: true,
                        child: new DefaultTextStyle(
                            style,
                            Current.Child,
                            textAlign: TextAlign.Center,
                            overflow: TextOverflow.Ellipsis,
                            maxLines: 1))));
        }

        return new MouseRegion(
            cursor: Current.MouseCursor ?? MouseCursor.Defer,
            child: new MetaData(
                metaData: this,
                behavior: HitTestBehavior.Opaque,
                child: new ConstrainedBox(
                    new BoxConstraints(MinHeight: CupertinoDialogConstants.DialogMinButtonHeight),
                    new Padding(
                        new Thickness(padding),
                        new Center(child: sizedContent)))));
    }
}

/// <summary>The rounded, blurred surface behind iOS-style popups.</summary>
public sealed class CupertinoPopupSurface : StatelessWidget
{
    public const double DefaultBlurSigma = 30.0;

    /// <summary>Debug-only kill switch for the saturation color filter.</summary>
    public static bool DebugIsVibrancePainted { get; set; } = true;

    private static readonly IReadOnlyList<double> LightSaturationMatrix =
    [
        1.74, -0.40, -0.17, 0, 0,
        -0.26, 1.60, -0.17, 0, 0,
        -0.26, -0.40, 1.83, 0, 0,
        0, 0, 0, 1.00, 0,
    ];

    private static readonly IReadOnlyList<double> DarkSaturationMatrix =
    [
        1.39, -0.56, -0.11, 0, 0.30,
        -0.32, 1.14, -0.11, 0, 0.30,
        -0.32, -0.56, 1.59, 0, 0.30,
        0, 0, 0, 1.00, 0,
    ];

    public CupertinoPopupSurface(
        Widget child,
        double blurSigma = DefaultBlurSigma,
        bool isSurfacePainted = true,
        Key? key = null) : base(key)
    {
        if (blurSigma < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(blurSigma), "CupertinoPopupSurface requires a non-negative blur sigma.");
        }

        Child = child ?? throw new ArgumentNullException(nameof(child));
        BlurSigma = blurSigma;
        IsSurfacePainted = isSurfacePainted;
    }

    public Widget Child { get; }
    public double BlurSigma { get; }
    public bool IsSurfacePainted { get; }

    public override Widget Build(BuildContext context)
    {
        ImageFilter? filter = null;
        if (DebugIsVibrancePainted)
        {
            bool dark = CupertinoTheme.BrightnessOf(context) == PlatformBrightness.Dark;
            ImageFilter saturation = new ImageFilter.ColorMatrix(
                dark ? DarkSaturationMatrix : LightSaturationMatrix);
            filter = BlurSigma > 0
                ? new ImageFilter.Compose(new ImageFilter.Blur(BlurSigma, BlurSigma), saturation)
                : saturation;
        }
        else if (BlurSigma > 0)
        {
            filter = new ImageFilter.Blur(BlurSigma, BlurSigma);
        }

        Widget contents = Child;
        if (IsSurfacePainted)
        {
            contents = new ColoredBox(
                CupertinoDialogConstants.DialogColor.ResolveFrom(context), contents);
        }

        if (filter is not null)
        {
            contents = new BackdropFilter(filter: filter, child: contents);
        }

        return new ClipRSuperellipse(
            borderRadius: BorderRadius.Circular(13),
            child: contents);
    }
}

/// <summary>The protocol the sliding-tap gesture uses to talk to the widgets under the pointer.</summary>
internal interface ISlideTarget
{
    /// <summary>The pointer entered; returns whether this target is enabled for outer targets.</summary>
    bool DidEnter(bool fromPointerDown, bool innerEnabled);

    void DidLeave();

    void DidConfirm();
}

/// <summary>
/// Dart's `_ActionSheetGestureDetector` + `_TargetSelectionGestureRecognizer`: tracks the primary
/// pointer from down to up, re-hit-testing on every move so a press can slide between actions.
/// The Dart version arbitrates against scrollables through the gesture arena; this port tracks the
/// raw pointer, which keeps selection live while the actions list scrolls (`docs/ai/DIVERGENCES.md`).
/// </summary>
internal sealed class ActionSheetGestureDetector : StatefulWidget
{
    public ActionSheetGestureDetector(Widget child, Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public Widget Child { get; }

    public override State CreateState() => new ActionSheetGestureDetectorState();
}

internal sealed class ActionSheetGestureDetectorState : State
{
    private int? _activePointer;
    private readonly List<ISlideTarget> _currentTargets = [];

    private ActionSheetGestureDetector Current => (ActionSheetGestureDetector)StateWidget;

    public override Widget Build(BuildContext context)
    {
        return new Listener(
            child: Current.Child,
            onPointerDown: HandlePointerDown,
            onPointerMove: HandlePointerMove,
            onPointerUp: HandlePointerUp,
            onPointerCancel: HandlePointerCancel);
    }

    public override void Dispose()
    {
        LeaveAllTargets();
        base.Dispose();
    }

    private void HandlePointerDown(PointerDownEvent @event)
    {
        if (_activePointer is not null)
        {
            return;
        }

        _activePointer = @event.Pointer;
        UpdateTargets(@event.Position, fromPointerDown: true);
    }

    private void HandlePointerMove(PointerMoveEvent @event)
    {
        if (_activePointer != @event.Pointer)
        {
            return;
        }

        UpdateTargets(@event.Position, fromPointerDown: false);
    }

    private void HandlePointerUp(PointerUpEvent @event)
    {
        if (_activePointer != @event.Pointer)
        {
            return;
        }

        _activePointer = null;
        foreach (ISlideTarget target in _currentTargets)
        {
            target.DidConfirm();
        }

        _currentTargets.Clear();
    }

    private void HandlePointerCancel(PointerCancelEvent @event)
    {
        if (_activePointer != @event.Pointer)
        {
            return;
        }

        _activePointer = null;
        LeaveAllTargets();
    }

    private void UpdateTargets(Point globalPosition, bool fromPointerDown)
    {
        List<ISlideTarget> targets = HitTestTargets(globalPosition);
        bool sameInnermost = targets.Count > 0
                             && _currentTargets.Count > 0
                             && ReferenceEquals(targets[0], _currentTargets[0]);
        if (sameInnermost && !fromPointerDown)
        {
            return;
        }

        if (!fromPointerDown)
        {
            LeaveAllTargets();
        }

        _currentTargets.Clear();
        _currentTargets.AddRange(targets);
        bool innerEnabled = true;
        foreach (ISlideTarget target in _currentTargets)
        {
            innerEnabled = target.DidEnter(fromPointerDown, innerEnabled);
        }
    }

    private void LeaveAllTargets()
    {
        foreach (ISlideTarget target in _currentTargets)
        {
            target.DidLeave();
        }

        _currentTargets.Clear();
    }

    private List<ISlideTarget> HitTestTargets(Point globalPosition)
    {
        var targets = new List<ISlideTarget>();
        if (Context.FindRenderObject() is not RenderBox renderBox || !renderBox.HasSize)
        {
            return targets;
        }

        var result = new BoxHitTestResult();
        renderBox.HitTest(result, renderBox.GlobalToLocal(globalPosition));
        foreach (HitTestEntry entry in result.Path)
        {
            if (entry.Target is RenderMetaData metaData && metaData.MetaData is ISlideTarget target)
            {
                targets.Add(target);
            }
        }

        return targets;
    }
}

/// <summary>Dart's `_AlertDialogButtonBackground`: paints the idle/pressed fill behind an action.</summary>
internal sealed class AlertDialogButtonBackground : StatefulWidget
{
    public AlertDialogButtonBackground(
        CupertinoDynamicColor idleColor,
        CupertinoDynamicColor pressedColor,
        bool pressed,
        Action<bool>? onPressStateChange,
        Widget child,
        Key? key = null) : base(key)
    {
        IdleColor = idleColor;
        PressedColor = pressedColor;
        Pressed = pressed;
        OnPressStateChange = onPressStateChange;
        Child = child;
    }

    public CupertinoDynamicColor IdleColor { get; }
    public CupertinoDynamicColor PressedColor { get; }
    public bool Pressed { get; }
    public Action<bool>? OnPressStateChange { get; }
    public Widget Child { get; }

    public override State CreateState() => new AlertDialogButtonBackgroundState();
}

internal sealed class AlertDialogButtonBackgroundState : State, ISlideTarget
{
    private AlertDialogButtonBackground Current => (AlertDialogButtonBackground)StateWidget;

    public bool DidEnter(bool fromPointerDown, bool innerEnabled)
    {
        Current.OnPressStateChange?.Invoke(innerEnabled);
        if (innerEnabled && !fromPointerDown)
        {
            HapticFeedback.SelectionClick();
        }

        return innerEnabled;
    }

    public void DidLeave() => Current.OnPressStateChange?.Invoke(false);

    public void DidConfirm() => Current.OnPressStateChange?.Invoke(false);

    public override Widget Build(BuildContext context)
    {
        Color color = Current.Pressed
            ? Current.PressedColor.ResolveFrom(context)
            : Current.IdleColor.ResolveFrom(context);
        return new MetaData(
            metaData: this,
            child: new MergeSemantics(new ColoredBox(color, Current.Child)));
    }
}

/// <summary>Dart's `_CupertinoAlertContentSection`: the scrollable title/message column.</summary>
internal sealed class CupertinoAlertContentSection : StatelessWidget
{
    public CupertinoAlertContentSection(
        Widget? title,
        Widget? message,
        ScrollController scrollController,
        Thickness titlePadding,
        Thickness messagePadding,
        TextStyle titleTextStyle,
        TextStyle messageTextStyle,
        Thickness? additionalPaddingBetweenTitleAndMessage = null,
        Key? key = null) : base(key)
    {
        Title = title;
        Message = message;
        ScrollController = scrollController;
        TitlePadding = titlePadding;
        MessagePadding = messagePadding;
        TitleTextStyle = titleTextStyle;
        MessageTextStyle = messageTextStyle;
        AdditionalPaddingBetweenTitleAndMessage = additionalPaddingBetweenTitleAndMessage;
    }

    public Widget? Title { get; }
    public Widget? Message { get; }
    public ScrollController ScrollController { get; }
    public Thickness TitlePadding { get; }
    public Thickness MessagePadding { get; }
    public TextStyle TitleTextStyle { get; }
    public TextStyle MessageTextStyle { get; }

    /// <summary>Extra spacing inserted between title and message; action sheets only.</summary>
    public Thickness? AdditionalPaddingBetweenTitleAndMessage { get; }

    public override Widget Build(BuildContext context)
    {
        if (Title is null && Message is null)
        {
            return new SingleChildScrollView(
                controller: ScrollController,
                child: new SizedBox(width: 0, height: 0));
        }

        var titleContentGroup = new List<Widget>();
        if (Title is not null)
        {
            titleContentGroup.Add(new Padding(
                TitlePadding,
                new DefaultTextStyle(TitleTextStyle, Title, textAlign: TextAlign.Center)));
        }

        if (Message is not null)
        {
            titleContentGroup.Add(new Padding(
                MessagePadding,
                new DefaultTextStyle(MessageTextStyle, Message, textAlign: TextAlign.Center)));
        }

        if (AdditionalPaddingBetweenTitleAndMessage is { } additionalPadding && titleContentGroup.Count > 1)
        {
            titleContentGroup.Insert(1, new Padding(additionalPadding));
        }

        return new CupertinoScrollbar(
            controller: ScrollController,
            child: new SingleChildScrollView(
                controller: ScrollController,
                child: new Column(
                    mainAxisSize: MainAxisSize.Max,
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    children: titleContentGroup)));
    }
}

/// <summary>Dart's `_CupertinoAlertActionSection`: the scrollable, divider-separated action list.</summary>
internal sealed class CupertinoAlertActionSection : StatelessWidget
{
    public CupertinoAlertActionSection(
        IReadOnlyList<Widget> actions,
        ScrollController scrollController,
        int? pressedIndex,
        Action<int, bool> onPressedUpdate,
        Key? key = null) : base(key)
    {
        if (actions.Count == 0)
        {
            throw new ArgumentException("An action section requires at least one action.", nameof(actions));
        }

        Actions = actions;
        ScrollController = scrollController;
        PressedIndex = pressedIndex;
        OnPressedUpdate = onPressedUpdate;
    }

    public IReadOnlyList<Widget> Actions { get; }
    public ScrollController ScrollController { get; }
    public int? PressedIndex { get; }
    public Action<int, bool> OnPressedUpdate { get; }

    public override Widget Build(BuildContext context)
    {
        var column = new List<Widget>();
        for (int index = 0; index < Actions.Count; index++)
        {
            if (index > 0)
            {
                column.Add(new CupertinoDialogDivider(
                    dividerColor: CupertinoDynamicColor.Resolve(CupertinoColors.Separator, context),
                    hiddenColor: CupertinoDialogConstants.DialogColor.ResolveFrom(context),
                    hidden: PressedIndex == index - 1 || PressedIndex == index));
            }

            int capturedIndex = index;
            column.Add(new AlertDialogButtonBackground(
                idleColor: CupertinoDialogConstants.DialogColor,
                pressedColor: CupertinoDialogConstants.DialogPressedColor,
                pressed: PressedIndex == index,
                onPressStateChange: pressed => OnPressedUpdate(capturedIndex, pressed),
                child: Actions[index]));
        }

        return new CupertinoScrollbar(
            controller: ScrollController,
            child: new SingleChildScrollView(
                controller: ScrollController,
                child: new AlertDialogActionsLayout(
                    dividerThickness: CupertinoDialogConstants.DividerThickness,
                    children: column)));
    }
}

/// <summary>
/// Dart's `_Divider`: fills the unconstrained axis and collapses the constrained one to the divider
/// thickness, painting the hidden color while an adjacent button is pressed.
/// </summary>
internal sealed class CupertinoDialogDivider : StatelessWidget
{
    public CupertinoDialogDivider(
        Color dividerColor,
        Color hiddenColor,
        bool hidden,
        Key? key = null) : base(key)
    {
        DividerColor = dividerColor;
        HiddenColor = hiddenColor;
        Hidden = hidden;
    }

    public Color DividerColor { get; }
    public Color HiddenColor { get; }
    public bool Hidden { get; }

    public override Widget Build(BuildContext context)
    {
        return new LimitedBox(
            maxWidth: CupertinoDialogConstants.DividerThickness,
            maxHeight: CupertinoDialogConstants.DividerThickness,
            child: new ConstrainedBox(
                new BoxConstraints(
                    MinWidth: CupertinoDialogConstants.DividerThickness,
                    MinHeight: CupertinoDialogConstants.DividerThickness),
                new ColoredBox(Hidden ? HiddenColor : DividerColor, new SizedBox())));
    }
}

/// <summary>
/// Dart's `_OverscrollBackground`: paints the dialog fill behind the scrollable actions while the
/// list is overscrolled, so the stretched region is not transparent.
/// </summary>
internal sealed class OverscrollBackground : StatefulWidget
{
    public OverscrollBackground(Color color, Widget child, Key? key = null) : base(key)
    {
        Color = color;
        Child = child;
    }

    public Color Color { get; }
    public Widget Child { get; }

    public override State CreateState() => new OverscrollBackgroundState();
}

internal sealed class OverscrollBackgroundState : State
{
    private double _topOverscroll;
    private double _bottomOverscroll;

    private OverscrollBackground Current => (OverscrollBackground)StateWidget;

    public override Widget Build(BuildContext context)
    {
        Widget background = new Column(
            mainAxisAlignment: MainAxisAlignment.SpaceBetween,
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            children:
            [
                new ColoredBox(Current.Color, new SizedBox(height: _topOverscroll)),
                new ColoredBox(Current.Color, new SizedBox(height: _bottomOverscroll)),
            ]);
        return new Stack(children:
        [
            new Positioned(left: 0, top: 0, right: 0, bottom: 0, child: background),
            new NotificationListener<ScrollUpdateNotification>(
                onNotification: OnScrollUpdate,
                child: Current.Child),
        ]);
    }

    private bool OnScrollUpdate(ScrollUpdateNotification notification)
    {
        IScrollMetrics metrics = notification.Metrics;
        SetState(() =>
        {
            _topOverscroll = Math.Min(
                Math.Max(metrics.MinScrollExtent - metrics.Pixels, 0), metrics.ViewportDimension);
            _bottomOverscroll = Math.Min(
                Math.Max(metrics.Pixels - metrics.MaxScrollExtent, 0), metrics.ViewportDimension);
        });
        return false;
    }
}

/// <summary>
/// Dart's `_PriorityColumn`: lays out the content section over the actions section, guaranteeing
/// the actions section a minimum height when both cannot fit.
/// </summary>
internal sealed class PriorityColumn : Flex
{
    public PriorityColumn(Widget top, Widget bottom, double bottomMinHeight, Key? key = null) : base(
        direction: Axis.Vertical,
        mainAxisSize: MainAxisSize.Min,
        crossAxisAlignment: CrossAxisAlignment.Stretch,
        children: [top, bottom],
        key: key)
    {
        BottomMinHeight = bottomMinHeight;
    }

    public double BottomMinHeight { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderPriorityColumn(BottomMinHeight);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        base.UpdateRenderObject(context, renderObject);
        ((RenderPriorityColumn)renderObject).BottomMinHeight = BottomMinHeight;
    }
}

internal sealed class RenderPriorityColumn : RenderFlex
{
    private double _bottomMinHeight;

    public RenderPriorityColumn(double bottomMinHeight) : base(
        children: null,
        direction: Axis.Vertical,
        mainAxisSize: MainAxisSize.Min,
        crossAxisAlignment: CrossAxisAlignment.Stretch)
    {
        _bottomMinHeight = bottomMinHeight;
    }

    public double BottomMinHeight
    {
        get => _bottomMinHeight;
        set
        {
            if (_bottomMinHeight == value)
            {
                return;
            }

            _bottomMinHeight = value;
            MarkNeedsLayout();
        }
    }

    protected override double ComputeMinIntrinsicHeight(double width)
    {
        (double top, double bottom) = ChildrenHeights(width, double.PositiveInfinity);
        return top + bottom;
    }

    protected override double ComputeMaxIntrinsicHeight(double width)
    {
        return ComputeMinIntrinsicHeight(width);
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        (double top, double bottom) = ChildrenHeights(constraints.MaxWidth, constraints.MaxHeight);
        return constraints.Constrain(new Size(constraints.MaxWidth, top + bottom));
    }

    protected override void PerformLayout()
    {
        double width = Constraints.MaxWidth;
        (double topHeight, double bottomHeight) = ChildrenHeights(width, Constraints.MaxHeight);
        Size = Constraints.Constrain(new Size(width, topHeight + bottomHeight));

        RenderBox top = FirstChild!;
        RenderBox bottom = ChildAfter(top)!;
        top.Layout(BoxConstraints.Tight(new Size(width, topHeight)), parentUsesSize: true);
        ((FlexParentData)top.parentData!).offset = new Point(0, 0);
        bottom.Layout(BoxConstraints.Tight(new Size(width, bottomHeight)), parentUsesSize: true);
        ((FlexParentData)bottom.parentData!).offset = new Point(0, topHeight);
    }

    private (double Top, double Bottom) ChildrenHeights(double width, double maxHeight)
    {
        RenderBox? top = FirstChild;
        RenderBox? bottom = top is null ? null : ChildAfter(top);
        if (top is null || bottom is null)
        {
            return (0, 0);
        }

        double topIntrinsic = top.GetMinIntrinsicHeight(width);
        double bottomIntrinsic = bottom.GetMinIntrinsicHeight(width);
        if (topIntrinsic + bottomIntrinsic <= maxHeight)
        {
            return (topIntrinsic, bottomIntrinsic);
        }

        double effectiveBottomMinHeight = Math.Min(_bottomMinHeight, bottomIntrinsic);
        if (maxHeight - topIntrinsic >= effectiveBottomMinHeight)
        {
            return (topIntrinsic, maxHeight - topIntrinsic);
        }

        if (maxHeight >= effectiveBottomMinHeight)
        {
            return (maxHeight - effectiveBottomMinHeight, effectiveBottomMinHeight);
        }

        return (0, maxHeight);
    }
}

/// <summary>
/// Dart's `_AlertDialogActionsLayout`: two buttons that both fit half the width sit side by side
/// around a vertical divider; anything else stacks vertically like a plain column.
/// </summary>
internal sealed class AlertDialogActionsLayout : Flex
{
    public AlertDialogActionsLayout(
        double dividerThickness,
        IReadOnlyList<Widget> children,
        Key? key = null) : base(
        direction: Axis.Vertical,
        mainAxisSize: MainAxisSize.Min,
        crossAxisAlignment: CrossAxisAlignment.Stretch,
        children: children,
        key: key)
    {
        DividerThickness = dividerThickness;
    }

    public double DividerThickness { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderAlertDialogActionsLayout(
            DividerThickness,
            Directionality.MaybeOf(context) ?? Plumix.UI.TextDirection.Ltr);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        base.UpdateRenderObject(context, renderObject);
        var layout = (RenderAlertDialogActionsLayout)renderObject;
        layout.DividerThickness = DividerThickness;
        layout.LayoutDirection = Directionality.MaybeOf(context) ?? Plumix.UI.TextDirection.Ltr;
    }
}

internal sealed class RenderAlertDialogActionsLayout : RenderFlex
{
    private double _dividerThickness;
    private TextDirection _layoutDirection;

    public RenderAlertDialogActionsLayout(double dividerThickness, TextDirection layoutDirection) : base(
        children: null,
        direction: Axis.Vertical,
        mainAxisSize: MainAxisSize.Min,
        crossAxisAlignment: CrossAxisAlignment.Stretch)
    {
        _dividerThickness = dividerThickness;
        _layoutDirection = layoutDirection;
    }

    public double DividerThickness
    {
        get => _dividerThickness;
        set
        {
            if (_dividerThickness == value)
            {
                return;
            }

            _dividerThickness = value;
            MarkNeedsLayout();
        }
    }

    public TextDirection LayoutDirection
    {
        get => _layoutDirection;
        set
        {
            if (_layoutDirection == value)
            {
                return;
            }

            _layoutDirection = value;
            MarkNeedsLayout();
        }
    }

    private double HorizontalSlotWidthFor(double overallWidth) =>
        (overallWidth - _dividerThickness) / 2.0;

    private bool UseHorizontalLayout(double overallWidth)
    {
        int count = 0;
        for (RenderBox? child = FirstChild; child != null; child = ChildAfter(child))
        {
            count++;
        }

        if (count != 3)
        {
            return false;
        }

        double slotWidth = HorizontalSlotWidthFor(overallWidth);
        int index = 0;
        for (RenderBox? child = FirstChild; child != null; child = ChildAfter(child), index++)
        {
            if (index % 2 == 1)
            {
                continue;
            }

            if (child.GetMaxIntrinsicWidth(double.PositiveInfinity) > slotWidth)
            {
                return false;
            }
        }

        return true;
    }

    protected override double ComputeMinIntrinsicHeight(double width)
    {
        if (!UseHorizontalLayout(width))
        {
            return base.ComputeMinIntrinsicHeight(width);
        }

        double slotWidth = HorizontalSlotWidthFor(width);
        double height = 0;
        int index = 0;
        for (RenderBox? child = FirstChild; child != null; child = ChildAfter(child), index++)
        {
            if (index % 2 == 0)
            {
                height = Math.Max(height, child.GetMinIntrinsicHeight(slotWidth));
            }
        }

        return height;
    }

    protected override double ComputeMaxIntrinsicHeight(double width)
    {
        return UseHorizontalLayout(width)
            ? ComputeMinIntrinsicHeight(width)
            : base.ComputeMaxIntrinsicHeight(width);
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        double overallWidth = constraints.MaxWidth;
        if (!UseHorizontalLayout(overallWidth))
        {
            return base.ComputeDryLayout(constraints);
        }

        return constraints.Constrain(
            new Size(overallWidth, ComputeMinIntrinsicHeight(overallWidth)));
    }

    protected override void PerformLayout()
    {
        double overallWidth = Constraints.MaxWidth;
        if (!UseHorizontalLayout(overallWidth))
        {
            base.PerformLayout();
            return;
        }

        double slotWidth = HorizontalSlotWidthFor(overallWidth);
        double height = ComputeMinIntrinsicHeight(overallWidth);
        Size = Constraints.Constrain(new Size(overallWidth, height));

        bool rightToLeft = _layoutDirection == Plumix.UI.TextDirection.Rtl;
        double x = rightToLeft ? overallWidth - slotWidth : 0;
        int index = 0;
        for (RenderBox? child = FirstChild; child != null; child = ChildAfter(child), index++)
        {
            bool isDivider = index % 2 == 1;
            double childWidth = isDivider ? _dividerThickness : slotWidth;
            child.Layout(BoxConstraints.Tight(new Size(childWidth, height)), parentUsesSize: true);
            if (rightToLeft)
            {
                ((FlexParentData)child.parentData!).offset = new Point(x, 0);
                x -= isDivider ? slotWidth : _dividerThickness;
            }
            else
            {
                ((FlexParentData)child.parentData!).offset = new Point(x, 0);
                x += childWidth;
            }
        }
    }
}
