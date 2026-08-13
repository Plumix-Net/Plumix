using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/snack_bar.dart

public enum SnackBarClosedReason
{
    Action,
    Dismiss,
    Swipe,
    Hide,
    Remove,
    Timeout,
}

public sealed class SnackBarAction : StatefulWidget
{
    public SnackBarAction(
        string label,
        Action onPressed,
        Color? textColor = null,
        Color? disabledTextColor = null,
        Color? backgroundColor = null,
        Color? disabledBackgroundColor = null,
        Key? key = null) : base(key)
    {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        OnPressed = onPressed ?? throw new ArgumentNullException(nameof(onPressed));
        TextColor = textColor;
        DisabledTextColor = disabledTextColor;
        BackgroundColor = backgroundColor;
        DisabledBackgroundColor = disabledBackgroundColor;
    }

    public Color? TextColor { get; }
    public Color? DisabledTextColor { get; }
    public Color? BackgroundColor { get; }
    public Color? DisabledBackgroundColor { get; }
    public string Label { get; }
    public Action OnPressed { get; }

    public override State CreateState() => new SnackBarActionState();

    private sealed class SnackBarActionState : State
    {
        private bool _haveTriggeredAction;
        private SnackBarAction CurrentWidget => (SnackBarAction)StateWidget;

        public override Widget Build(BuildContext context)
        {
            var widget = CurrentWidget;
            var theme = Theme.Of(context);
            var snackBarTheme = SnackBarTheme.Of(context);
            var enabledForeground = widget.TextColor
                                    ?? snackBarTheme.ActionTextColor
                                    ?? (theme.UseMaterial3 ? theme.InversePrimaryColor : theme.SecondaryColor);
            var disabledForeground = widget.DisabledTextColor
                                     ?? snackBarTheme.DisabledActionTextColor
                                     ?? (theme.UseMaterial3
                                         ? theme.InversePrimaryColor
                                         : ApplyOpacity(theme.OnSurfaceColor, theme.Brightness == Brightness.Light ? 0.38 : 0.30));
            var enabledBackground = widget.BackgroundColor
                                    ?? snackBarTheme.ActionBackgroundColor
                                    ?? Colors.Transparent;
            var disabledBackground = widget.DisabledBackgroundColor
                                     ?? snackBarTheme.DisabledActionBackgroundColor
                                     ?? Colors.Transparent;

            return new TextButton(
                child: new Text(widget.Label),
                onPressed: _haveTriggeredAction ? null : HandlePressed,
                style: TextButton.StyleFrom(
                    foregroundColor: enabledForeground,
                    disabledForegroundColor: disabledForeground,
                    backgroundColor: enabledBackground,
                    disabledBackgroundColor: disabledBackground,
                    overlayColor: enabledForeground,
                    padding: new Thickness(8, 0),
                    minimumSize: new Size(0, 36)));
        }

        private void HandlePressed()
        {
            if (_haveTriggeredAction) return;
            SetState(() => _haveTriggeredAction = true);
            CurrentWidget.OnPressed();
            ScaffoldMessenger.MaybeOf(Context)?.HideCurrentSnackBar(SnackBarClosedReason.Action);
        }
    }

    private static Color ApplyOpacity(Color color, double opacity)
    {
        byte alpha = (byte)Math.Round(color.A * Math.Clamp(opacity, 0, 1));
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }
}

public sealed class SnackBar : StatefulWidget
{
    public static readonly TimeSpan TransitionDuration = TimeSpan.FromMilliseconds(250);
    public static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(4);

    public SnackBar(
        Widget content,
        Color? backgroundColor = null,
        double? elevation = null,
        Thickness? margin = null,
        Thickness? padding = null,
        double? width = null,
        ShapeBorder? shape = null,
        HitTestBehavior? hitTestBehavior = null,
        SnackBarBehavior? behavior = null,
        SnackBarAction? action = null,
        double? actionOverflowThreshold = null,
        bool? showCloseIcon = null,
        Color? closeIconColor = null,
        TimeSpan? duration = null,
        bool? persist = null,
        AnimationController? animation = null,
        Action? onVisible = null,
        DismissDirection? dismissDirection = null,
        Clip clipBehavior = Clip.HardEdge,
        Key? key = null) : base(key)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        ValidateNonNegativeFinite(elevation, nameof(elevation));
        ValidatePositiveFinite(width, nameof(width));
        ValidateUnitInterval(actionOverflowThreshold, nameof(actionOverflowThreshold));
        ValidateInsets(margin, nameof(margin));
        ValidateInsets(padding, nameof(padding));
        if (width.HasValue && margin.HasValue)
        {
            throw new ArgumentException("SnackBar width and margin cannot both be specified.", nameof(width));
        }

        if (behavior == SnackBarBehavior.Fixed && (width.HasValue || margin.HasValue))
        {
            throw new ArgumentException("SnackBar width and margin require floating behavior.", nameof(behavior));
        }

        BackgroundColor = backgroundColor;
        Elevation = elevation;
        Margin = margin;
        Padding = padding;
        Width = width;
        Shape = shape;
        HitTestBehavior = hitTestBehavior;
        Behavior = behavior;
        Action = action;
        ActionOverflowThreshold = actionOverflowThreshold;
        ShowCloseIcon = showCloseIcon;
        CloseIconColor = closeIconColor;
        Duration = duration ?? DefaultDuration;
        if (Duration < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration));
        Persist = persist ?? action is not null;
        Animation = animation;
        OnVisible = onVisible;
        DismissDirection = dismissDirection;
        ClipBehavior = clipBehavior;
    }

    public Widget Content { get; }
    public Color? BackgroundColor { get; }
    public double? Elevation { get; }
    public Thickness? Margin { get; }
    public Thickness? Padding { get; }
    public double? Width { get; }
    public ShapeBorder? Shape { get; }
    public HitTestBehavior? HitTestBehavior { get; }
    public SnackBarBehavior? Behavior { get; }
    public SnackBarAction? Action { get; }
    public double? ActionOverflowThreshold { get; }
    public bool? ShowCloseIcon { get; }
    public Color? CloseIconColor { get; }
    public TimeSpan Duration { get; }
    public bool Persist { get; }
    public AnimationController? Animation { get; }
    public Action? OnVisible { get; }
    public DismissDirection? DismissDirection { get; }
    public Clip ClipBehavior { get; }

    public static AnimationController CreateAnimationController() => new(TransitionDuration);

    public SnackBar WithAnimation(AnimationController animation, Key? fallbackKey = null)
    {
        ArgumentNullException.ThrowIfNull(animation);
        return new SnackBar(
            content: Content,
            backgroundColor: BackgroundColor,
            elevation: Elevation,
            margin: Margin,
            padding: Padding,
            width: Width,
            shape: Shape,
            hitTestBehavior: HitTestBehavior,
            behavior: Behavior,
            action: Action,
            actionOverflowThreshold: ActionOverflowThreshold,
            showCloseIcon: ShowCloseIcon,
            closeIconColor: CloseIconColor,
            duration: Duration,
            persist: Persist,
            animation: animation,
            onVisible: OnVisible,
            dismissDirection: DismissDirection,
            clipBehavior: ClipBehavior,
            key: Key ?? fallbackKey);
    }

    public override State CreateState() => new SnackBarState();

    private sealed class SnackBarState : State
    {
        private readonly object _heroTag = new();
        private bool _wasVisible;
        private double _dragExtent;
        private SnackBar CurrentWidget => (SnackBar)StateWidget;

        public override void InitState()
        {
            Subscribe(CurrentWidget.Animation);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldSnackBar = (SnackBar)oldWidget;
            if (ReferenceEquals(oldSnackBar.Animation, CurrentWidget.Animation)) return;
            Unsubscribe(oldSnackBar.Animation);
            Subscribe(CurrentWidget.Animation);
        }

        public override void Dispose()
        {
            Unsubscribe(CurrentWidget.Animation);
        }

        public override Widget Build(BuildContext context)
        {
            var widget = CurrentWidget;
            var theme = Theme.Of(context);
            var snackBarTheme = SnackBarTheme.Of(context);
            var behavior = widget.Behavior ?? snackBarTheme.Behavior ?? SnackBarBehavior.Fixed;
            double? width = widget.Width ?? snackBarTheme.Width;
            if (behavior != SnackBarBehavior.Floating && (widget.Margin.HasValue || width.HasValue))
            {
                throw new InvalidOperationException("SnackBar width and margin require floating behavior.");
            }

            bool showCloseIcon = widget.ShowCloseIcon ?? snackBarTheme.ShowCloseIcon ?? false;
            var direction = Directionality.Of(context);
            double horizontalPadding = behavior == SnackBarBehavior.Floating ? 16.0 : 24.0;
            var effectivePadding = widget.Padding ?? ResolveDirectionalPadding(
                direction,
                start: horizontalPadding,
                end: widget.Action is not null || showCloseIcon ? 0 : horizontalPadding);
            double actionHorizontalMargin = (widget.Padding?.Right ?? horizontalPadding) / 2.0;
            double iconHorizontalMargin = (widget.Padding?.Right ?? horizontalPadding) / 12.0;

            var trailingChildren = new List<Widget>();
            if (widget.Action is not null)
            {
                trailingChildren.Add(new Padding(
                    new Thickness(actionHorizontalMargin, 0),
                    widget.Action));
            }

            if (showCloseIcon)
            {
                trailingChildren.Add(new Padding(
                    new Thickness(iconHorizontalMargin, 0),
                    new IconButton(
                        icon: new Icon(Icons.Close),
                        onPressed: () => ScaffoldMessenger.MaybeOf(context)?.HideCurrentSnackBar(SnackBarClosedReason.Dismiss),
                        iconSize: 24,
                        color: widget.CloseIconColor
                               ?? snackBarTheme.CloseIconColor
                               ?? (theme.UseMaterial3 ? theme.OnInverseSurfaceColor : theme.OnSurfaceColor),
                        padding: default,
                        constraints: new BoxConstraints(MinWidth: 48, MinHeight: 48))));
            }

            Widget content = new DefaultTextStyle(
                snackBarTheme.ContentTextStyle
                ?? ResolveDefaultContentTextStyle(theme),
                widget.Content);
            if (!widget.Padding.HasValue)
            {
                content = new Padding(new Thickness(0, 14), content);
            }

            Widget trailing = trailingChildren.Count == 0
                ? new SizedBox()
                : new Row(mainAxisSize: MainAxisSize.Min, children: trailingChildren, textDirection: direction);
            Widget snackBar = new Padding(
                effectivePadding,
                new SnackBarContentLayout(
                    content: content,
                    trailing: trailing,
                    actionOverflowThreshold: widget.ActionOverflowThreshold
                                             ?? snackBarTheme.ActionOverflowThreshold
                                             ?? 0.25,
                    textDirection: direction));

            if (behavior == SnackBarBehavior.Fixed)
            {
                snackBar = new SafeArea(top: false, child: snackBar);
            }

            double elevation = widget.Elevation ?? snackBarTheme.Elevation ?? 6.0;
            var background = widget.BackgroundColor
                             ?? snackBarTheme.BackgroundColor
                             ?? ResolveDefaultBackground(theme);
            var shape = widget.Shape
                        ?? snackBarTheme.Shape
                        ?? (behavior == SnackBarBehavior.Floating ? new RoundedRectangleBorder(borderRadius:
                            Plumix.Rendering.BorderRadius.Circular(4)) : null);
            var radius = ShapeBorderGeometry.ResolveRadiusOrNull(shape) ?? BorderRadius.Zero;
            var shadow = theme.ShadowColor;
            snackBar = new DecoratedBox(
                new BoxDecoration(
                    Color: background,
                    Border: ShapeBorderGeometry.SideOrNull(
                        shape) is { } shapeSide ? Plumix.Rendering.Border.FromBorderSide(shapeSide) : null,
                    BorderRadius: radius,
                    BoxShadows: BuildBoxShadows(shadow, elevation)),
                snackBar);

            if (widget.ClipBehavior != Clip.None && radius != BorderRadius.Zero)
            {
                snackBar = new ClipRRect(radius, snackBar);
            }

            var margin = widget.Margin ?? snackBarTheme.InsetPadding ?? new Thickness(15, 5, 15, 10);
            if (behavior == SnackBarBehavior.Floating)
            {
                snackBar = width.HasValue
                    ? new Padding(
                        new Thickness(0, margin.Top, 0, margin.Bottom),
                        new Align(alignment: Alignment.BottomCenter, child: new SizedBox(width: width, child: snackBar)))
                    : new Padding(margin, snackBar);
                snackBar = new SafeArea(top: false, bottom: false, child: snackBar);
            }

            var dismissDirection = widget.DismissDirection
                                   ?? snackBarTheme.DismissDirection
                                   ?? Plumix.Material.DismissDirection.Down;
            snackBar = BuildDismissible(snackBar, dismissDirection, widget.HitTestBehavior
                ?? (widget.Margin.HasValue || snackBarTheme.InsetPadding.HasValue
                    ? Plumix.Rendering.HitTestBehavior.DeferToChild
                    : Plumix.Rendering.HitTestBehavior.Opaque));
            snackBar = new Semantics(
                container: true,
                liveRegion: true,
                onDismiss: () => ScaffoldMessenger.MaybeOf(context)?.RemoveCurrentSnackBar(SnackBarClosedReason.Dismiss),
                child: snackBar);

            snackBar = ApplyTransition(snackBar, behavior, theme, MediaQuery.AccessibleNavigationOf(context));
            if (widget.ClipBehavior != Clip.None)
            {
                snackBar = new ClipRect(child: snackBar);
            }

            return new Hero(
                tag: _heroTag,
                transitionOnUserGestures: true,
                child: snackBar);
        }

        private Widget BuildDismissible(Widget child, DismissDirection direction, Plumix.Rendering.HitTestBehavior behavior)
        {
            if (direction == Plumix.Material.DismissDirection.None) return child;
            _dragExtent = 0;
            if (direction is Plumix.Material.DismissDirection.Up or Plumix.Material.DismissDirection.Down or Plumix.Material.DismissDirection.Vertical)
            {
                return new GestureDetector(
                    behavior: behavior,
                    onVerticalDragStart: _ => _dragExtent = 0,
                    onVerticalDragUpdate: details => _dragExtent += details.PrimaryDelta,
                    onVerticalDragEnd: details =>
                    {
                        double velocity = details.PrimaryVelocity;
                        bool matches = direction switch
                        {
                            Plumix.Material.DismissDirection.Up => _dragExtent < -40 || velocity < -365,
                            Plumix.Material.DismissDirection.Down => _dragExtent > 40 || velocity > 365,
                            _ => Math.Abs(_dragExtent) > 40 || Math.Abs(velocity) > 365,
                        };
                        if (matches)
                        {
                            ScaffoldMessenger.MaybeOf(Context)?.RemoveCurrentSnackBar(SnackBarClosedReason.Swipe);
                        }
                    },
                    child: child);
            }

            return new GestureDetector(
                behavior: behavior,
                onHorizontalDragStart: _ => _dragExtent = 0,
                onHorizontalDragUpdate: details => _dragExtent += details.PrimaryDelta,
                onHorizontalDragEnd: details =>
                {
                    var directionality = Directionality.Of(Context);
                    double velocity = details.PrimaryVelocity;
                    bool matches = direction switch
                    {
                        Plumix.Material.DismissDirection.StartToEnd => directionality == TextDirection.Ltr
                            ? _dragExtent > 40 || velocity > 365
                            : _dragExtent < -40 || velocity < -365,
                        Plumix.Material.DismissDirection.EndToStart => directionality == TextDirection.Ltr
                            ? _dragExtent < -40 || velocity < -365
                            : _dragExtent > 40 || velocity > 365,
                        _ => Math.Abs(_dragExtent) > 40 || Math.Abs(velocity) > 365,
                    };
                    if (matches)
                    {
                        ScaffoldMessenger.MaybeOf(Context)?.RemoveCurrentSnackBar(SnackBarClosedReason.Swipe);
                    }
                },
                child: child);
        }

        private Widget ApplyTransition(Widget child, SnackBarBehavior behavior, ThemeData theme, bool accessibleNavigation)
        {
            if (accessibleNavigation || CurrentWidget.Animation is null) return child;
            double raw = Math.Clamp(CurrentWidget.Animation.Value, 0, 1);
            if (behavior == SnackBarBehavior.Floating)
            {
                double fade = theme.UseMaterial3
                    ? EvaluateInterval(raw, 0.4, 0.6, Curves.EaseIn)
                    : EvaluateInterval(raw, 0.4, 1.0, Curves.Linear);
                Widget result = new Opacity(fade, child);
                if (theme.UseMaterial3)
                {
                    result = new Align(
                        alignment: Alignment.BottomLeft,
                        heightFactor: Curves.EaseInOut(raw),
                        child: result);
                }

                return result;
            }

            return new Align(
                alignment: Alignment.TopLeft,
                heightFactor: Curves.FastOutSlowIn(raw),
                child: child);
        }

        private void Subscribe(AnimationController? animation)
        {
            if (animation is null)
            {
                NotifyVisible();
                return;
            }

            animation.Changed += HandleAnimationChanged;
            animation.Completed += NotifyVisible;
        }

        private void Unsubscribe(AnimationController? animation)
        {
            if (animation is null) return;
            animation.Changed -= HandleAnimationChanged;
            animation.Completed -= NotifyVisible;
        }

        private void HandleAnimationChanged() => SetState(() => { });

        private void NotifyVisible()
        {
            if (_wasVisible) return;
            _wasVisible = true;
            CurrentWidget.OnVisible?.Invoke();
        }
    }

    private static TextStyle ResolveDefaultContentTextStyle(ThemeData theme)
    {
        return theme.UseMaterial3
            ? theme.TextTheme.BodyMedium.CopyWith(color: theme.OnInverseSurfaceColor)
            : theme.TextTheme.TitleMedium.CopyWith(
                color: theme.Brightness == Brightness.Light ? Colors.White : Colors.Black);
    }

    private static Color ResolveDefaultBackground(ThemeData theme)
    {
        if (theme.UseMaterial3) return theme.InverseSurfaceColor;
        if (theme.Brightness == Brightness.Dark) return theme.OnSurfaceColor;
        return AlphaBlend(ApplyOpacity(theme.OnSurfaceColor, 0.80), theme.SurfaceColor);
    }

    private static Thickness ResolveDirectionalPadding(TextDirection direction, double start, double end)
    {
        return direction == TextDirection.Ltr
            ? new Thickness(start, 0, end, 0)
            : new Thickness(end, 0, start, 0);
    }

    private static BoxShadows? BuildBoxShadows(Color color, double elevation)
    {
        if (elevation <= 0 || color.A == 0) return null;
        return new BoxShadows(new BoxShadow
        {
            OffsetX = 0,
            OffsetY = Math.Max(1, elevation * 0.5),
            Blur = Math.Max(2, elevation * 2.4),
            Color = ApplyOpacity(color, 0.22),
        });
    }

    private static Color ApplyOpacity(Color color, double opacity)
    {
        return Color.FromArgb(
            (byte)Math.Round(color.A * Math.Clamp(opacity, 0, 1)),
            color.R,
            color.G,
            color.B);
    }

    private static Color AlphaBlend(Color foreground, Color background)
    {
        double alpha = foreground.A / 255.0;
        byte Blend(byte foregroundChannel, byte backgroundChannel) =>
            (byte)Math.Round((foregroundChannel * alpha) + (backgroundChannel * (1 - alpha)));
        return Color.FromArgb(
            255,
            Blend(foreground.R, background.R),
            Blend(foreground.G, background.G),
            Blend(foreground.B, background.B));
    }

    private static double EvaluateInterval(double value, double begin, double end, Curve curve)
    {
        if (value <= begin) return 0;
        if (value >= end) return 1;
        return curve((value - begin) / (end - begin));
    }

    private static void ValidateNonNegativeFinite(double? value, string name)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0))
            throw new ArgumentOutOfRangeException(name);
    }

    private static void ValidatePositiveFinite(double? value, string name)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value <= 0))
            throw new ArgumentOutOfRangeException(name);
    }

    private static void ValidateUnitInterval(double? value, string name)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0 || value.Value > 1))
            throw new ArgumentOutOfRangeException(name);
    }

    private static void ValidateInsets(Thickness? value, string name)
    {
        if (!value.HasValue) return;
        var insets = value.Value;
        if (!double.IsFinite(insets.Left) || !double.IsFinite(insets.Top)
            || !double.IsFinite(insets.Right) || !double.IsFinite(insets.Bottom)
            || insets.Left < 0 || insets.Top < 0 || insets.Right < 0 || insets.Bottom < 0)
            throw new ArgumentOutOfRangeException(name);
    }
}

internal sealed class SnackBarContentLayout : MultiChildRenderObjectWidget
{
    public SnackBarContentLayout(
        Widget content,
        Widget trailing,
        double actionOverflowThreshold,
        TextDirection textDirection,
        Key? key = null) : base([content, trailing], key)
    {
        ActionOverflowThreshold = actionOverflowThreshold;
        TextDirection = textDirection;
    }

    public double ActionOverflowThreshold { get; }
    public TextDirection TextDirection { get; }

    internal override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderSnackBarContentLayout(ActionOverflowThreshold, TextDirection);

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var layout = (RenderSnackBarContentLayout)renderObject;
        layout.ActionOverflowThreshold = ActionOverflowThreshold;
        layout.TextDirection = TextDirection;
    }
}

internal sealed class SnackBarContentParentData : ContainerBoxParentData<RenderBox>;

internal sealed class RenderSnackBarContentLayout : RenderBox,
    IRenderBoxContainerDefaultsMixin<RenderBox, SnackBarContentParentData>,
    IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderBox, SnackBarContentParentData> _container;
    private double _actionOverflowThreshold;
    private TextDirection _textDirection;

    public RenderSnackBarContentLayout(double actionOverflowThreshold, TextDirection textDirection)
    {
        _actionOverflowThreshold = actionOverflowThreshold;
        _textDirection = textDirection;
        _container = new RenderBoxContainerDefaultsMixin<RenderBox, SnackBarContentParentData>(this);
    }

    public double ActionOverflowThreshold
    {
        get => _actionOverflowThreshold;
        set { if (_actionOverflowThreshold != value) { _actionOverflowThreshold = value; MarkNeedsLayout(); } }
    }

    public TextDirection TextDirection
    {
        get => _textDirection;
        set { if (_textDirection != value) { _textDirection = value; MarkNeedsLayout(); } }
    }

    public int ChildCount => _container.ChildCount;
    public RenderBox? FirstChild => _container.FirstChild;
    public RenderBox? LastChild => _container.LastChild;
    public void AddAll(List<RenderBox> children) => _container.AddAll(children);
    public RenderBox? ChildBefore(RenderBox child) => _container.ChildBefore(child);
    public RenderBox? ChildAfter(RenderBox child) => _container.ChildAfter(child);

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not SnackBarContentParentData) child.parentData = new SnackBarContentParentData();
    }

    protected override void PerformLayout()
    {
        var content = FirstChild;
        var trailing = content is null ? null : ChildAfter(content);
        if (content is null || trailing is null)
        {
            Size = Constraints.Smallest;
            return;
        }

        if (!Constraints.HasBoundedWidth)
        {
            trailing.Layout(new BoxConstraints(), parentUsesSize: true);
            content.Layout(new BoxConstraints(), parentUsesSize: true);
            Size = Constraints.Constrain(new Size(
                content.Size.Width + trailing.Size.Width,
                Math.Max(content.Size.Height, trailing.Size.Height)));
            ((SnackBarContentParentData)content.parentData!).offset = new Point(0, 0);
            ((SnackBarContentParentData)trailing.parentData!).offset = new Point(
                content.Size.Width,
                (Size.Height - trailing.Size.Height) / 2);
            return;
        }

        double maxWidth = Constraints.MaxWidth;
        trailing.Layout(new BoxConstraints(MaxWidth: maxWidth), parentUsesSize: true);
        bool overflow = maxWidth > 0 && trailing.Size.Width / maxWidth > ActionOverflowThreshold;
        double contentWidth = overflow
            ? maxWidth * 0.6
            : Math.Max(0, maxWidth - trailing.Size.Width);
        content.Layout(new BoxConstraints(MaxWidth: contentWidth), parentUsesSize: true);

        double height = overflow
            ? content.Size.Height + trailing.Size.Height + 14
            : Math.Max(content.Size.Height, trailing.Size.Height);
        Size = Constraints.Constrain(new Size(maxWidth, height));
        var contentData = (SnackBarContentParentData)content.parentData!;
        var trailingData = (SnackBarContentParentData)trailing.parentData!;
        contentData.offset = TextDirection == TextDirection.Ltr
            ? new Point(0, 0)
            : new Point(Size.Width - content.Size.Width, 0);
        if (overflow)
        {
            trailingData.offset = new Point(
                TextDirection == TextDirection.Ltr ? Size.Width - trailing.Size.Width : 0,
                content.Size.Height);
        }
        else
        {
            trailingData.offset = new Point(
                TextDirection == TextDirection.Ltr ? Size.Width - trailing.Size.Width : 0,
                (Size.Height - trailing.Size.Height) / 2);
        }
    }

    public override void Paint(PaintingContext context, Point offset) => _container.DefaultPaint(context, offset);
    protected override bool HitTestChildren(BoxHitTestResult result, Point position) => _container.DefaultHitTestChildren(result, position);
    public void DefaultPaint(PaintingContext context, Point offset) => _container.DefaultPaint(context, offset);
    public bool DefaultHitTestChildren(BoxHitTestResult result, Point position) => _container.DefaultHitTestChildren(result, position);
    public override void VisitChildren(Action<RenderObject> visitor) { for (var child = FirstChild; child is not null; child = ChildAfter(child)) visitor(child); }
    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor) { for (var child = FirstChild; child is not null; child = ChildAfter(child)) { var data = (SnackBarContentParentData)child.parentData!; visitor(child, data.offset, Matrix.Identity); } }
    public void Insert(RenderBox child, RenderBox? after = null) => _container.Insert(child, after);
    public void Move(RenderBox child, RenderBox? after = null) => _container.Move(child, after);
    public void Remove(RenderBox child) => _container.Remove(child);
    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after) => Insert((RenderBox)child, after as RenderBox);
    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after) => Move((RenderBox)child, after as RenderBox);
    void IRenderObjectContainer.Remove(RenderObject child) => Remove((RenderBox)child);
}
