using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/dialog.dart

/// <summary>Dart's `_PressedUpdateHandler`: reports which action index is being held.</summary>
internal delegate void PressedUpdateHandler(int actionIndex, bool pressed);

/// <summary>
/// An iOS-style action sheet: a specific style of alert that presents the user with a set of two or
/// more choices related to the current context. Typically passed as the child widget to
/// <see cref="CupertinoDialogs.ShowCupertinoModalPopup{T}"/>.
/// </summary>
public sealed class CupertinoActionSheet : StatefulWidget
{
    public CupertinoActionSheet(
        Widget? title = null,
        Widget? message = null,
        IReadOnlyList<Widget>? actions = null,
        ScrollController? messageScrollController = null,
        ScrollController? actionScrollController = null,
        Widget? cancelButton = null,
        Key? key = null) : base(key)
    {
        if (actions is null && title is null && message is null && cancelButton is null)
        {
            throw new ArgumentException(
                "An action sheet must have a non-null value for at least one of the following "
                + "arguments: actions, title, message, or cancelButton.");
        }

        Title = title;
        Message = message;
        Actions = actions;
        MessageScrollController = messageScrollController;
        ActionScrollController = actionScrollController;
        CancelButton = cancelButton;
    }

    /// <summary>An optional title; bold when <see cref="Message"/> is also given.</summary>
    public Widget? Title { get; }

    /// <summary>An optional descriptive message below the title.</summary>
    public Widget? Message { get; }

    /// <summary>The actions displayed for the user to select, as `CupertinoActionSheetAction`s.</summary>
    public IReadOnlyList<Widget>? Actions { get; }

    /// <summary>Controls scrolling of the <see cref="Message"/> section; created internally if null.</summary>
    public ScrollController? MessageScrollController { get; }

    /// <summary>Controls scrolling of the <see cref="Actions"/> section; created internally if null.</summary>
    public ScrollController? ActionScrollController { get; }

    /// <summary>The optional cancel button, grouped separately below the other actions.</summary>
    public Widget? CancelButton { get; }

    public override State CreateState() => new CupertinoActionSheetState();
}

internal sealed class CupertinoActionSheetState : State
{
    private const int CancelButtonIndex = -1;

    private int? _pressedIndex;
    private ScrollController? _backupMessageScrollController;
    private ScrollController? _backupActionScrollController;

    private CupertinoActionSheet Current => (CupertinoActionSheet)StateWidget;

    private ScrollController EffectiveMessageScrollController =>
        Current.MessageScrollController ?? (_backupMessageScrollController ??= new ScrollController());

    private ScrollController EffectiveActionScrollController =>
        Current.ActionScrollController ?? (_backupActionScrollController ??= new ScrollController());

    private bool HasContent => Current.Title is not null || Current.Message is not null;

    public override void Dispose()
    {
        _backupMessageScrollController?.Dispose();
        _backupActionScrollController?.Dispose();
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        //  ╭─────────────────╮  ↑                ↑
        //  │    The title    │ Content section   |
        //  │   The message   │  ↓                |
        //  ├─────────────────┤  ↑             Main sheet
        //  │    Action 1     │  |                |
        //  ├─────────────────┤ Actions section   |
        //  │    Action 2     │  |                |
        //  ╰─────────────────╯  ↓                ↓
        //  ╭─────────────────╮
        //  │     Cancel      │
        //  ╰─────────────────╯
        var children = new List<Widget>
        {
            new Flexible(
                new ClipRSuperellipse(
                    borderRadius: BorderRadius.Circular(12.0),
                    child: new BackdropFilter(
                        filter: new ImageFilter.Blur(
                            CupertinoPopupSurface.DefaultBlurSigma,
                            CupertinoPopupSurface.DefaultBlurSigma),
                        child: new ActionSheetMainSheet(
                            pressedIndex: _pressedIndex,
                            onPressedUpdate: HandlePressedUpdate,
                            scrollController: EffectiveActionScrollController,
                            contentSection: BuildContent(context),
                            actions: Current.Actions ?? [],
                            dividerColor: CupertinoDynamicColor.Resolve(
                                CupertinoDialogConstants.ActionSheetButtonDividerColor, context))))),
        };
        if (Current.CancelButton is not null)
        {
            children.Add(BuildCancelButton());
        }

        double actionSheetWidth = MediaQuery.OrientationOf(context) == Orientation.Portrait
            ? MediaQuery.WidthOf(context)
            : MediaQuery.HeightOf(context);

        return new SafeArea(
            minimum: new Thickness(0, 0, 0, CupertinoDialogConstants.ActionSheetEdgePadding),
            // A CupertinoScrollbar is built-in below.
            child: new ScrollConfiguration(
                behavior: ScrollConfiguration.Of(context).CopyWith(scrollbars: false),
                child: new Semantics(
                    namesRoute: true,
                    scopesRoute: true,
                    explicitChildNodes: true,
                    role: SemanticsRole.Dialog,
                    label: "Alert",
                    child: new CupertinoUserInterfaceLevel(
                        CupertinoUserInterfaceLevelData.Elevated,
                        new Padding(
                            // The bottom padding is set on SafeArea.Minimum, allowing it to be
                            // consumed by the bottom view padding.
                            new Thickness(
                                CupertinoDialogConstants.ActionSheetEdgePadding,
                                TopPadding(context),
                                CupertinoDialogConstants.ActionSheetEdgePadding,
                                0),
                            new SizedBox(
                                width: actionSheetWidth - (CupertinoDialogConstants.ActionSheetEdgePadding * 2),
                                child: new ActionSheetGestureDetector(
                                    child: new Semantics(
                                        explicitChildNodes: true,
                                        child: new Column(
                                            mainAxisAlignment: MainAxisAlignment.End,
                                            mainAxisSize: MainAxisSize.Min,
                                            crossAxisAlignment: CrossAxisAlignment.Stretch,
                                            children: children)))))))));
    }

    private Widget? BuildContent(BuildContext context)
    {
        if (!HasContent)
        {
            return null;
        }

        TextStyle textStyle = CupertinoDialogConstants.ActionSheetContentStyle.CopyWith(
            color: CupertinoDynamicColor.Resolve(
                CupertinoDialogConstants.ActionSheetContentTextColor, context));
        return new ColoredBox(
            CupertinoDynamicColor.Resolve(CupertinoDialogConstants.ActionSheetBackgroundColor, context),
            child: new CupertinoAlertContentSection(
                title: Current.Title,
                message: Current.Message,
                scrollController: EffectiveMessageScrollController,
                titlePadding: new Thickness(
                    CupertinoDialogConstants.ActionSheetContentHorizontalPadding,
                    CupertinoDialogConstants.ActionSheetContentVerticalPadding,
                    CupertinoDialogConstants.ActionSheetContentHorizontalPadding,
                    Current.Message is null ? CupertinoDialogConstants.ActionSheetContentVerticalPadding : 0.0),
                messagePadding: new Thickness(
                    CupertinoDialogConstants.ActionSheetContentHorizontalPadding,
                    Current.Title is null ? CupertinoDialogConstants.ActionSheetContentVerticalPadding : 0.0,
                    CupertinoDialogConstants.ActionSheetContentHorizontalPadding,
                    CupertinoDialogConstants.ActionSheetContentVerticalPadding),
                titleTextStyle: Current.Message is null
                    ? textStyle
                    : textStyle.CopyWith(fontWeight: FontWeight.SemiBold),
                messageTextStyle: Current.Title is null
                    ? textStyle.CopyWith(fontWeight: FontWeight.SemiBold)
                    : textStyle,
                additionalPaddingBetweenTitleAndMessage: new Thickness(0, 4.0, 0, 0)));
    }

    private Widget BuildCancelButton()
    {
        double cancelPadding =
            Current.Actions is not null || Current.Message is not null || Current.Title is not null
                ? CupertinoDialogConstants.ActionSheetCancelButtonPadding
                : 0.0;
        return new Padding(
            new Thickness(0, cancelPadding, 0, 0),
            CupertinoFocusHalo.WithRRect(
                borderRadius: CupertinoConstants.CupertinoButtonSizeBorderRadius[CupertinoButtonSize.Large],
                child: new ActionSheetButtonBackground(
                    isCancel: true,
                    pressed: _pressedIndex == CancelButtonIndex,
                    onPressStateChange: pressed => HandlePressedUpdate(CancelButtonIndex, pressed),
                    child: Current.CancelButton!)));
    }

    private void HandlePressedUpdate(int actionIndex, bool pressed)
    {
        if (!pressed)
        {
            if (_pressedIndex == actionIndex)
            {
                SetState(() => _pressedIndex = null);
            }
        }
        else
        {
            SetState(() => _pressedIndex = actionIndex);
        }
    }

    /// <summary>
    /// Dart's `_lerp`: linear interpolation between two data points that extrapolates flatly beyond
    /// them.
    /// </summary>
    private static double Lerp(double x, double x1, double y1, double x2, double y2)
    {
        if (x <= x1)
        {
            return y1;
        }

        if (x >= x2)
        {
            return y2;
        }

        return y1 + ((y2 - y1) * ((x - x1) / (x2 - x1)));
    }

    /// <summary>
    /// Dart's `_topPadding`: the distance between the top of a full-height action sheet and the top
    /// of the safe area, derived by measuring on the simulator.
    /// </summary>
    private static double TopPadding(BuildContext context)
    {
        if (MediaQuery.OrientationOf(context) == Orientation.Landscape)
        {
            return CupertinoDialogConstants.ActionSheetEdgePadding;
        }

        // The x for the lerp is the top view padding, the y is the ratio of action sheet padding to
        // top view padding: 47.0 -> 1.0 (notch) and 59.0 -> 54/59 (capsule).
        const double viewPaddingData1 = 47.0;
        const double paddingRatioData1 = 1.0;
        const double viewPaddingData2 = 59.0;
        const double paddingRatioData2 = 54.0 / 59.0;

        double currentViewPadding = MediaQuery.ViewPaddingOf(context).Top;
        double currentPaddingRatio = Lerp(
            currentViewPadding,
            viewPaddingData1,
            paddingRatioData1,
            viewPaddingData2,
            paddingRatioData2);
        double padding = Math.Round(currentPaddingRatio * currentViewPadding, MidpointRounding.AwayFromZero);
        // In case there is no view padding, there should still be some space between the action
        // sheet and the edge.
        return Math.Max(padding, CupertinoDialogConstants.DialogEdgePadding);
    }
}

/// <summary>The content of a typical action button in a <see cref="CupertinoActionSheet"/>.</summary>
public sealed class CupertinoActionSheetAction : StatefulWidget
{
    public CupertinoActionSheetAction(
        Widget child,
        Action onPressed,
        bool isDefaultAction = false,
        bool isDestructiveAction = false,
        MouseCursor? mouseCursor = null,
        FocusNode? focusNode = null,
        Color? focusColor = null,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        OnPressed = onPressed ?? throw new ArgumentNullException(nameof(onPressed));
        IsDefaultAction = isDefaultAction;
        IsDestructiveAction = isDestructiveAction;
        MouseCursor = mouseCursor;
        FocusNode = focusNode;
        FocusColor = focusColor;
    }

    /// <summary>
    /// Called when the button is selected, either by tapping it or by pressing elsewhere and
    /// sliding onto this button before releasing.
    /// </summary>
    public Action OnPressed { get; }

    /// <summary>Whether this action is the default choice; default buttons have bold text.</summary>
    public bool IsDefaultAction { get; }

    /// <summary>Whether this action might change or delete data; destructive buttons are red.</summary>
    public bool IsDestructiveAction { get; }

    /// <summary>Defaults to `SystemMouseCursors.Click` on web and `MouseCursor.Defer` elsewhere.</summary>
    public MouseCursor? MouseCursor { get; }

    public FocusNode? FocusNode { get; }

    /// <summary>The focus highlight color; defaults to `CupertinoColors.ActiveBlue`.</summary>
    public Color? FocusColor { get; }

    public Widget Child { get; }

    public override State CreateState() => new CupertinoActionSheetActionState();
}

internal sealed class CupertinoActionSheetActionState : State, ISlideTarget
{
    private bool _showHighlight;

    private CupertinoActionSheetAction Current => (CupertinoActionSheetAction)StateWidget;

    public bool DidEnter(bool fromPointerDown, bool innerEnabled) => innerEnabled;

    public void DidLeave()
    {
    }

    public void DidConfirm() => Current.OnPressed();

    public override Widget Build(BuildContext context)
    {
        Widget content = new ActionSheetActionContent(
            isDestructiveAction: Current.IsDestructiveAction,
            isDefaultAction: Current.IsDefaultAction,
            child: Current.Child);
        if (_showHighlight)
        {
            content = new DecoratedBox(
                new BoxDecoration(Color: EffectiveFocusBackgroundColor(context)),
                child: content);
        }

        return new MouseRegion(
            cursor: Current.MouseCursor
                    ?? (PlatformDefaults.IsWeb ? SystemMouseCursors.Click : MouseCursor.Defer),
            child: new MetaData(
                metaData: this,
                behavior: HitTestBehavior.Opaque,
                child: new ConstrainedBox(
                    new BoxConstraints(MinHeight: CupertinoDialogConstants.ActionSheetButtonMinHeight),
                    new FocusableActionDetector(
                        actions: new Dictionary<Type, FlutterAction>
                        {
                            [typeof(ActivateIntent)] = new CallbackAction<ActivateIntent>(_ => HandleTap()),
                        },
                        focusNode: Current.FocusNode,
                        onShowFocusHighlight: OnShowFocusHighlight,
                        child: new Semantics(
                            flags: SemanticsFlags.IsButton,
                            onTap: Current.OnPressed,
                            child: content)))));
    }

    private object? HandleTap()
    {
        Current.OnPressed();
        SemanticsService.SendEvent(new TapSemanticEvent(Context.FindRenderObject()?.SemanticsNodeId));
        return null;
    }

    private void OnShowFocusHighlight(bool showHighlight)
    {
        SetState(() => _showHighlight = showHighlight);
    }

    private Color EffectiveFocusBackgroundColor(BuildContext context)
    {
        double opacity = CupertinoTheme.BrightnessOf(context) == PlatformBrightness.Light
            ? CupertinoConstants.CupertinoButtonTintedOpacityLight
            : CupertinoConstants.CupertinoButtonTintedOpacityDark;
        Color baseColor = Current.FocusColor ?? CupertinoColors.ActiveBlue.Color;
        byte alpha = (byte)Math.Clamp(
            (int)Math.Round(byte.MaxValue * Math.Clamp(opacity, 0.0, 1.0)),
            0,
            byte.MaxValue);
        return HSLColor.FromColor(Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B)).ToColor();
    }
}

/// <summary>
/// Dart's `_ActionSheetActionContent`: the label of an action sheet button. The background is drawn
/// by <see cref="ActionSheetButtonBackground"/> instead.
/// </summary>
internal sealed class ActionSheetActionContent : StatelessWidget
{
    public ActionSheetActionContent(
        bool isDestructiveAction,
        bool isDefaultAction,
        Widget child,
        Key? key = null) : base(key)
    {
        IsDestructiveAction = isDestructiveAction;
        IsDefaultAction = isDefaultAction;
        Child = child;
    }

    public bool IsDestructiveAction { get; }
    public bool IsDefaultAction { get; }
    public Widget Child { get; }

    /// <summary>
    /// Dart's `_buttonFontSize`: native action sheet buttons deviate from the HIG body sizes in a
    /// non-linear way, so mid-sized text is interpolated piecewise.
    /// </summary>
    internal static double ButtonFontSize(double contextBodySize)
    {
        return contextBodySize switch
        {
            <= 17.0 => 21.0,
            <= 19.0 => 21.0 + ((23.0 - 21.0) * ((contextBodySize - 17.0) / (19.0 - 17.0))),
            <= 21.0 => 23.0 + ((24.0 - 23.0) * ((contextBodySize - 19.0) / (21.0 - 19.0))),
            <= 24.0 => 24.0,
            _ => contextBodySize,
        };
    }

    public override Widget Build(BuildContext context)
    {
        // The context scale factor is derived from the current body size and the standard body size
        // in "large".
        const double higLargeBodySize = 17.0;
        double contextBodySize = MediaQuery.TextScalerOf(context).Scale(higLargeBodySize);
        double contextScaleFactor = contextBodySize / higLargeBodySize;
        double fontSize = ButtonFontSize(contextBodySize);

        TextStyle style = CupertinoDialogConstants.ActionSheetActionStyle.CopyWith(
            // `Text` scales the provided font size inside, so its parameter is unscaled first.
            fontSize: fontSize / contextScaleFactor,
            color: IsDestructiveAction
                ? CupertinoDynamicColor.Resolve(CupertinoColors.SystemRed, context)
                : CupertinoTheme.Of(context).PrimaryColor);
        if (IsDefaultAction)
        {
            style = style.CopyWith(fontWeight: FontWeight.SemiBold);
        }

        double verticalPadding = CupertinoDialogConstants.ActionSheetButtonVerticalPaddingBase
                                 + (fontSize * CupertinoDialogConstants.ActionSheetButtonVerticalPaddingFactor);
        return new Padding(
            new Thickness(
                CupertinoDialogConstants.ActionSheetButtonHorizontalPadding,
                verticalPadding,
                CupertinoDialogConstants.ActionSheetButtonHorizontalPadding,
                verticalPadding),
            new DefaultTextStyle(
                style,
                new Center(child: Child),
                textAlign: TextAlign.Center));
    }
}

/// <summary>
/// Dart's `_ActionSheetButtonBackground`: paints the idle/pressed background of an action sheet
/// button and reports its press state to the parent.
/// </summary>
internal sealed class ActionSheetButtonBackground : StatefulWidget
{
    public ActionSheetButtonBackground(
        bool pressed,
        Widget child,
        bool isCancel = false,
        Action<bool>? onPressStateChange = null,
        Key? key = null) : base(key)
    {
        Pressed = pressed;
        Child = child;
        IsCancel = isCancel;
        OnPressStateChange = onPressStateChange;
    }

    public bool IsCancel { get; }

    /// <summary>Whether the user is holding on this button.</summary>
    public bool Pressed { get; }

    /// <summary>Called with true when the user taps down on the button, false when they lift up.</summary>
    public Action<bool>? OnPressStateChange { get; }

    public Widget Child { get; }

    public override State CreateState() => new ActionSheetButtonBackgroundState();
}

internal sealed class ActionSheetButtonBackgroundState : State, ISlideTarget
{
    private ActionSheetButtonBackground Current => (ActionSheetButtonBackground)StateWidget;

    public bool DidEnter(bool fromPointerDown, bool innerEnabled)
    {
        // Action sheets do not support disabled buttons, so `innerEnabled` is always true.
        Current.OnPressStateChange?.Invoke(true);
        if (!fromPointerDown)
        {
            EmitVibration();
        }

        return innerEnabled;
    }

    public void DidLeave() => Current.OnPressStateChange?.Invoke(false);

    public void DidConfirm() => Current.OnPressStateChange?.Invoke(false);

    public override Widget Build(BuildContext context)
    {
        Widget child;
        if (!Current.IsCancel)
        {
            child = new ColoredBox(
                CupertinoDynamicColor.Resolve(
                    Current.Pressed
                        ? CupertinoDialogConstants.ActionSheetPressedColor
                        : CupertinoDialogConstants.ActionSheetBackgroundColor,
                    context),
                child: Current.Child);
        }
        else
        {
            child = new ClipRSuperellipse(
                borderRadius: BorderRadius.Circular(CupertinoDialogConstants.CornerRadius),
                child: new DecoratedBox(
                    new BoxDecoration(
                        Color: CupertinoDynamicColor.Resolve(
                            Current.Pressed
                                ? CupertinoDialogConstants.ActionSheetCancelPressedColor
                                : CupertinoDialogConstants.ActionSheetCancelColor,
                            context)),
                    child: Current.Child));
        }

        return new MetaData(metaData: this, child: child);
    }

    private static void EmitVibration()
    {
        switch (PlatformDefaults.TargetPlatform)
        {
            case TargetPlatform.IOS:
            case TargetPlatform.Android:
                _ = HapticFeedback.SelectionClick();
                break;
            default:
                break;
        }
    }
}

/// <summary>
/// Dart's `_ActionSheetActionSection`: the scrollable, divider-separated list of actions.
/// </summary>
internal sealed class ActionSheetActionSection : StatelessWidget
{
    public ActionSheetActionSection(
        IReadOnlyList<Widget>? actions,
        int? pressedIndex,
        Color dividerColor,
        Color backgroundColor,
        PressedUpdateHandler onPressedUpdate,
        ScrollController scrollController,
        Key? key = null) : base(key)
    {
        Actions = actions;
        PressedIndex = pressedIndex;
        DividerColor = dividerColor;
        BackgroundColor = backgroundColor;
        OnPressedUpdate = onPressedUpdate;
        ScrollController = scrollController;
    }

    public IReadOnlyList<Widget>? Actions { get; }
    public int? PressedIndex { get; }
    public Color DividerColor { get; }
    public Color BackgroundColor { get; }
    public PressedUpdateHandler OnPressedUpdate { get; }
    public ScrollController ScrollController { get; }

    public override Widget Build(BuildContext context)
    {
        if (Actions is null || Actions.Count == 0)
        {
            return new LimitedBox(
                maxWidth: 0,
                child: new SizedBox(width: double.PositiveInfinity, height: 0));
        }

        var column = new List<Widget>();
        for (int actionIndex = 0; actionIndex < Actions.Count; actionIndex++)
        {
            if (actionIndex != 0)
            {
                column.Add(new CupertinoDialogDivider(
                    dividerColor: DividerColor,
                    hiddenColor: CupertinoDynamicColor.Resolve(
                        CupertinoDialogConstants.ActionSheetBackgroundColor, context),
                    hidden: PressedIndex == actionIndex - 1 || PressedIndex == actionIndex));
            }

            int capturedIndex = actionIndex;
            column.Add(new ActionSheetButtonBackground(
                pressed: PressedIndex == actionIndex,
                onPressStateChange: pressed => OnPressedUpdate(capturedIndex, pressed),
                child: Actions[actionIndex]));
        }

        return new CupertinoScrollbar(
            controller: ScrollController,
            child: new SingleChildScrollView(
                controller: ScrollController,
                child: new Column(crossAxisAlignment: CrossAxisAlignment.Stretch, children: column)));
    }
}

/// <summary>Dart's `_ActionSheetMainSheet`: the part of an action sheet without the cancel button.</summary>
internal sealed class ActionSheetMainSheet : StatelessWidget
{
    public ActionSheetMainSheet(
        int? pressedIndex,
        PressedUpdateHandler onPressedUpdate,
        ScrollController scrollController,
        IReadOnlyList<Widget> actions,
        Widget? contentSection,
        Color dividerColor,
        Key? key = null) : base(key)
    {
        PressedIndex = pressedIndex;
        OnPressedUpdate = onPressedUpdate;
        ScrollController = scrollController;
        Actions = actions;
        ContentSection = contentSection;
        DividerColor = dividerColor;
    }

    public int? PressedIndex { get; }
    public PressedUpdateHandler OnPressedUpdate { get; }
    public ScrollController ScrollController { get; }
    public IReadOnlyList<Widget> Actions { get; }
    public Widget? ContentSection { get; }
    public Color DividerColor { get; }

    public override Widget Build(BuildContext context)
    {
        if (Actions.Count == 0)
        {
            return ContentSection ?? Empty();
        }

        if (ContentSection is null)
        {
            return ScrolledActionsSection(context);
        }

        return new PriorityColumn(
            top: ContentSection,
            bottom: DividerAndActionsSection(context),
            bottomMinHeight: CupertinoDialogConstants.ActionSheetActionsSectionMinHeight
                             + CupertinoDialogConstants.DividerThickness);
    }

    private Widget ScrolledActionsSection(BuildContext context)
    {
        Color backgroundColor = CupertinoDynamicColor.Resolve(
            CupertinoDialogConstants.ActionSheetBackgroundColor, context);
        return new OverscrollBackground(
            color: backgroundColor,
            child: CupertinoFocusHalo.WithRRect(
                borderRadius: CupertinoConstants
                    .CupertinoButtonSizeBorderRadius[CupertinoButtonSize.Large]
                    .CopyWith(topLeft: Radius.Zero, topRight: Radius.Zero),
                child: new ActionSheetActionSection(
                    actions: Actions,
                    scrollController: ScrollController,
                    dividerColor: DividerColor,
                    backgroundColor: backgroundColor,
                    pressedIndex: PressedIndex,
                    onPressedUpdate: OnPressedUpdate)));
    }

    private Widget DividerAndActionsSection(BuildContext context)
    {
        Color backgroundColor = CupertinoDynamicColor.Resolve(
            CupertinoDialogConstants.ActionSheetBackgroundColor, context);
        return new Column(
            mainAxisSize: MainAxisSize.Min,
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            children:
            [
                new CupertinoDialogDivider(
                    dividerColor: DividerColor,
                    hiddenColor: backgroundColor,
                    hidden: false),
                new Flexible(ScrolledActionsSection(context)),
            ]);
    }

    private static Widget Empty() =>
        new LimitedBox(maxWidth: 0, child: new SizedBox(width: double.PositiveInfinity, height: 0));
}
