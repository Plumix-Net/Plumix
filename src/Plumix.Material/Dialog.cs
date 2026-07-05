using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/dialog.dart

public sealed class Dialog : StatelessWidget
{
    private static readonly Thickness DefaultInsetPadding = new(40, 24);

    public Dialog(
        Color? backgroundColor = null,
        double? elevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        TimeSpan? insetAnimationDuration = null,
        Curve? insetAnimationCurve = null,
        Thickness? insetPadding = null,
        Clip? clipBehavior = null,
        ShapeBorder? shape = null,
        Alignment? alignment = null,
        Widget? child = null,
        SemanticsRole semanticsRole = SemanticsRole.Dialog,
        BoxConstraints? constraints = null,
        Key? key = null) : this(
        fullscreen: false,
        backgroundColor: backgroundColor,
        elevation: elevation,
        shadowColor: shadowColor,
        surfaceTintColor: surfaceTintColor,
        insetAnimationDuration: insetAnimationDuration ?? TimeSpan.FromMilliseconds(100),
        insetAnimationCurve: insetAnimationCurve ?? Curves.EaseOut,
        insetPadding: insetPadding,
        clipBehavior: clipBehavior,
        shape: shape,
        alignment: alignment,
        child: child,
        semanticsRole: semanticsRole,
        constraints: constraints,
        key: key)
    {
    }

    private Dialog(
        bool fullscreen,
        Color? backgroundColor,
        double? elevation,
        Color? shadowColor,
        Color? surfaceTintColor,
        TimeSpan insetAnimationDuration,
        Curve insetAnimationCurve,
        Thickness? insetPadding,
        Clip? clipBehavior,
        ShapeBorder? shape,
        Alignment? alignment,
        Widget? child,
        SemanticsRole semanticsRole,
        BoxConstraints? constraints,
        Key? key) : base(key)
    {
        if (elevation.HasValue && (!double.IsFinite(elevation.Value) || elevation.Value < 0))
            throw new ArgumentOutOfRangeException(nameof(elevation));
        if (insetAnimationDuration < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(insetAnimationDuration));
        ValidateInsets(insetPadding, nameof(insetPadding));
        IsFullscreen = fullscreen;
        BackgroundColor = backgroundColor;
        Elevation = elevation;
        ShadowColor = shadowColor;
        SurfaceTintColor = surfaceTintColor;
        InsetAnimationDuration = insetAnimationDuration;
        InsetAnimationCurve = insetAnimationCurve;
        InsetPadding = insetPadding;
        ClipBehavior = clipBehavior;
        Shape = shape;
        Alignment = alignment;
        Child = child;
        SemanticsRole = semanticsRole;
        Constraints = constraints;
    }

    public Color? BackgroundColor { get; }
    public double? Elevation { get; }
    public Color? ShadowColor { get; }
    public Color? SurfaceTintColor { get; }
    public TimeSpan InsetAnimationDuration { get; }
    public Curve InsetAnimationCurve { get; }
    public Thickness? InsetPadding { get; }
    public Clip? ClipBehavior { get; }
    public ShapeBorder? Shape { get; }
    public Alignment? Alignment { get; }
    public Widget? Child { get; }
    public SemanticsRole SemanticsRole { get; }
    public BoxConstraints? Constraints { get; }
    public bool IsFullscreen { get; }

    public static Dialog Fullscreen(
        Color? backgroundColor = null,
        TimeSpan? insetAnimationDuration = null,
        Curve? insetAnimationCurve = null,
        Widget? child = null,
        SemanticsRole semanticsRole = SemanticsRole.Dialog,
        Key? key = null) => new(
        fullscreen: true,
        backgroundColor: backgroundColor,
        elevation: 0,
        shadowColor: null,
        surfaceTintColor: null,
        insetAnimationDuration: insetAnimationDuration ?? TimeSpan.Zero,
        insetAnimationCurve: insetAnimationCurve ?? Curves.EaseOut,
        insetPadding: default(Thickness),
        clipBehavior: Clip.None,
        shape: null,
        alignment: null,
        child: child,
        semanticsRole: semanticsRole,
        constraints: null,
        key: key);

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var dialogTheme = DialogTheme.Of(context);
        var viewInsets = MediaQuery.ViewInsetsOf(context);
        var effectivePadding = Add(viewInsets, InsetPadding ?? dialogTheme.InsetPadding ?? DefaultInsetPadding);
        var defaults = ResolveDefaults(theme, IsFullscreen);

        Widget dialogChild;
        if (IsFullscreen)
        {
            dialogChild = new ColoredBox(
                BackgroundColor ?? dialogTheme.BackgroundColor ?? defaults.BackgroundColor!.Value,
                Child);
        }
        else
        {
            var constraints = Constraints ?? dialogTheme.Constraints ?? new BoxConstraints(MinWidth: 280);
            var background = BackgroundColor ?? dialogTheme.BackgroundColor ?? defaults.BackgroundColor!.Value;
            var elevation = Elevation ?? dialogTheme.Elevation ?? defaults.Elevation!.Value;
            var shadow = ShadowColor ?? dialogTheme.ShadowColor ?? defaults.ShadowColor ?? Colors.Transparent;
            var surfaceTint = SurfaceTintColor ?? dialogTheme.SurfaceTintColor ?? defaults.SurfaceTintColor;
            if (theme.UseMaterial3 && surfaceTint.HasValue)
            {
                background = NavigationSurfaceUtilities.ApplySurfaceTint(background, surfaceTint.Value, elevation);
            }

            var shape = Shape ?? dialogTheme.Shape ?? defaults.Shape!;
            var clip = ClipBehavior ?? dialogTheme.ClipBehavior ?? defaults.ClipBehavior ?? Clip.None;
            Widget surface = new DecoratedBox(
                new BoxDecoration(
                    Color: background,
                    Border: shape.Side,
                    BorderRadius: shape.BorderRadius,
                    BoxShadows: BuildBoxShadows(shadow, elevation)),
                Child ?? new SizedBox());
            if (clip != Clip.None)
            {
                surface = new ClipRRect(shape.BorderRadius, surface);
            }

            dialogChild = new Align(
                alignment: Alignment ?? dialogTheme.Alignment ?? defaults.Alignment ?? Plumix.Rendering.Alignment.Center,
                child: new ConstrainedBox(constraints, surface));
        }

        dialogChild = MediaQuery.RemoveViewInsets(
            context,
            dialogChild,
            removeLeft: true,
            removeTop: true,
            removeRight: true,
            removeBottom: true);
        dialogChild = new AnimatedContainer(
            duration: InsetAnimationDuration,
            curve: InsetAnimationCurve,
            padding: effectivePadding,
            child: dialogChild);
        return new Semantics(role: SemanticsRole, child: dialogChild);
    }

    internal static DialogThemeData ResolveDefaults(ThemeData theme, bool fullscreen = false)
    {
        if (fullscreen && theme.UseMaterial3)
        {
            return new DialogThemeData(
                BackgroundColor: theme.SurfaceColor,
                ClipBehavior: Clip.None);
        }

        if (!theme.UseMaterial3)
        {
            return new DialogThemeData(
                BackgroundColor: theme.Brightness == Brightness.Dark ? Color.Parse("#FF424242") : Colors.White,
                Elevation: 24,
                ShadowColor: theme.ShadowColor,
                Shape: ShapeBorder.RoundedRectangle(4),
                Alignment: Plumix.Rendering.Alignment.Center,
                IconColor: theme.IconTheme.Color,
                TitleTextStyle: theme.TextTheme.TitleLarge,
                ContentTextStyle: theme.TextTheme.TitleMedium,
                ActionsPadding: default,
                ClipBehavior: Clip.None);
        }

        return new DialogThemeData(
            BackgroundColor: theme.SurfaceContainerHighColor,
            Elevation: 6,
            ShadowColor: Colors.Transparent,
            SurfaceTintColor: Colors.Transparent,
            Shape: ShapeBorder.RoundedRectangle(28),
            Alignment: Plumix.Rendering.Alignment.Center,
            IconColor: theme.SecondaryColor,
            TitleTextStyle: theme.TextTheme.HeadlineSmall,
            ContentTextStyle: theme.TextTheme.BodyMedium,
            ActionsPadding: new Thickness(24, 0, 24, 24),
            ClipBehavior: Clip.None);
    }

    private static BoxShadows? BuildBoxShadows(Color color, double elevation)
    {
        if (color.A == 0 || elevation <= 0) return null;
        return new BoxShadows(
            new BoxShadow
            {
                OffsetY = Math.Max(1, elevation * 0.5),
                Blur = Math.Max(2, elevation * 2.4),
                Color = ApplyOpacity(color, 0.20),
            },
            [new BoxShadow
            {
                OffsetY = Math.Max(1, elevation * 0.25),
                Blur = Math.Max(3, elevation * 3.2),
                Color = ApplyOpacity(color, 0.14),
            }]);
    }

    private static Color ApplyOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Round(color.A * Math.Clamp(opacity, 0, 1)), color.R, color.G, color.B);

    private static Thickness Add(Thickness a, Thickness b) => new(
        a.Left + b.Left,
        a.Top + b.Top,
        a.Right + b.Right,
        a.Bottom + b.Bottom);

    internal static void ValidateInsets(Thickness? value, string parameterName)
    {
        if (!value.HasValue) return;
        var insets = value.Value;
        if (!double.IsFinite(insets.Left) || !double.IsFinite(insets.Top)
            || !double.IsFinite(insets.Right) || !double.IsFinite(insets.Bottom)
            || insets.Left < 0 || insets.Top < 0 || insets.Right < 0 || insets.Bottom < 0)
            throw new ArgumentOutOfRangeException(parameterName);
    }
}

public sealed class SimpleDialogOption : StatelessWidget
{
    public SimpleDialogOption(
        Action? onPressed = null,
        Thickness? padding = null,
        Widget? child = null,
        Key? key = null) : base(key)
    {
        Dialog.ValidateInsets(padding, nameof(padding));
        OnPressed = onPressed;
        Padding = padding;
        Child = child;
    }

    public Action? OnPressed { get; }

    public Thickness? Padding { get; }

    public Widget? Child { get; }

    public override Widget Build(BuildContext context)
    {
        return new InkWell(
            onTap: OnPressed,
            child: new Padding(
                Padding ?? new Thickness(24, 8),
                Child ?? new SizedBox()));
    }
}

public sealed class SimpleDialog : StatelessWidget
{
    private static readonly Thickness DefaultTitlePadding = new(24, 24, 24, 0);
    private static readonly Thickness DefaultContentPadding = new(0, 12, 0, 16);

    public SimpleDialog(
        Widget? title = null,
        Thickness? titlePadding = null,
        TextStyle? titleTextStyle = null,
        IReadOnlyList<Widget>? children = null,
        Thickness? contentPadding = null,
        TextStyle? contentTextStyle = null,
        Color? backgroundColor = null,
        double? elevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        string? semanticLabel = null,
        Thickness? insetPadding = null,
        Clip? clipBehavior = null,
        ShapeBorder? shape = null,
        Alignment? alignment = null,
        BoxConstraints? constraints = null,
        Key? key = null) : base(key)
    {
        if (elevation.HasValue && (!double.IsFinite(elevation.Value) || elevation.Value < 0))
            throw new ArgumentOutOfRangeException(nameof(elevation));
        Dialog.ValidateInsets(titlePadding, nameof(titlePadding));
        Dialog.ValidateInsets(contentPadding, nameof(contentPadding));
        Dialog.ValidateInsets(insetPadding, nameof(insetPadding));
        Title = title;
        TitlePadding = titlePadding ?? DefaultTitlePadding;
        TitleTextStyle = titleTextStyle;
        Children = children;
        ContentPadding = contentPadding ?? DefaultContentPadding;
        ContentTextStyle = contentTextStyle;
        BackgroundColor = backgroundColor;
        Elevation = elevation;
        ShadowColor = shadowColor;
        SurfaceTintColor = surfaceTintColor;
        SemanticLabel = semanticLabel;
        InsetPadding = insetPadding;
        ClipBehavior = clipBehavior;
        Shape = shape;
        Alignment = alignment;
        Constraints = constraints;
    }

    public Widget? Title { get; }
    public Thickness TitlePadding { get; }
    public TextStyle? TitleTextStyle { get; }
    public IReadOnlyList<Widget>? Children { get; }
    public Thickness ContentPadding { get; }
    public TextStyle? ContentTextStyle { get; }
    public Color? BackgroundColor { get; }
    public double? Elevation { get; }
    public Color? ShadowColor { get; }
    public Color? SurfaceTintColor { get; }
    public string? SemanticLabel { get; }
    public Thickness? InsetPadding { get; }
    public Clip? ClipBehavior { get; }
    public ShapeBorder? Shape { get; }
    public Alignment? Alignment { get; }
    public BoxConstraints? Constraints { get; }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var dialogTheme = DialogTheme.Of(context);
        var defaults = Dialog.ResolveDefaults(theme);
        var label = theme.Platform is TargetPlatform.IOS or TargetPlatform.MacOS
            ? SemanticLabel
            : SemanticLabel ?? MaterialLocalizations.Of(context).DialogLabel;
        var effectiveTitleTextStyle = TitleTextStyle ?? dialogTheme.TitleTextStyle ?? theme.TextTheme.TitleLarge;
        var paddingScale = ScalePadding(MediaQuery.TextScaleFactorOf(context));

        Widget? titleWidget = null;
        if (Title is not null)
        {
            var padding = new Thickness(
                TitlePadding.Left * paddingScale,
                TitlePadding.Top * paddingScale,
                TitlePadding.Right * paddingScale,
                Children is null ? TitlePadding.Bottom * paddingScale : TitlePadding.Bottom);
            titleWidget = new Padding(
                padding,
                new DefaultTextStyle(
                    effectiveTitleTextStyle,
                    new Semantics(
                        namesRoute: label is null && theme.Platform != TargetPlatform.IOS,
                        container: true,
                        child: Title)));
        }

        Widget? contentWidget = null;
        if (Children is not null)
        {
            var padding = new Thickness(
                ContentPadding.Left * paddingScale,
                Title is null ? ContentPadding.Top * paddingScale : ContentPadding.Top,
                ContentPadding.Right * paddingScale,
                ContentPadding.Bottom * paddingScale);
            contentWidget = new Flexible(
                new SingleChildScrollView(
                    padding: padding,
                    child: new DefaultTextStyle(
                        ContentTextStyle ?? dialogTheme.ContentTextStyle ?? defaults.ContentTextStyle!,
                        new ListBody(children: Children))));
        }

        var children = new List<Widget>();
        if (titleWidget is not null) children.Add(titleWidget);
        if (contentWidget is not null) children.Add(contentWidget);
        Widget dialogChild = new IntrinsicWidth(
            stepWidth: 56,
            child: new Column(
                mainAxisSize: MainAxisSize.Min,
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children: children));
        if (label is not null)
        {
            dialogChild = new Semantics(
                label: label,
                scopesRoute: true,
                namesRoute: true,
                explicitChildNodes: true,
                child: dialogChild);
        }

        return new Dialog(
            backgroundColor: BackgroundColor,
            elevation: Elevation,
            shadowColor: ShadowColor,
            surfaceTintColor: SurfaceTintColor,
            insetPadding: InsetPadding,
            clipBehavior: ClipBehavior,
            shape: Shape,
            alignment: Alignment,
            constraints: Constraints,
            child: dialogChild);
    }

    private static double ScalePadding(double textScaleFactor)
    {
        var clamped = Math.Clamp(textScaleFactor, 1, 2);
        return 1.0 + ((1.0 / 3.0 - 1.0) * (clamped - 1.0));
    }
}

public sealed class AlertDialog : StatelessWidget
{
    public AlertDialog(
        Widget? icon = null,
        Thickness? iconPadding = null,
        Color? iconColor = null,
        Widget? title = null,
        Thickness? titlePadding = null,
        TextStyle? titleTextStyle = null,
        Widget? content = null,
        Thickness? contentPadding = null,
        TextStyle? contentTextStyle = null,
        IReadOnlyList<Widget>? actions = null,
        Thickness? actionsPadding = null,
        MainAxisAlignment? actionsAlignment = null,
        OverflowBarAlignment? actionsOverflowAlignment = null,
        VerticalDirection? actionsOverflowDirection = null,
        double? actionsOverflowButtonSpacing = null,
        Thickness? buttonPadding = null,
        Color? backgroundColor = null,
        double? elevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        string? semanticLabel = null,
        Thickness? insetPadding = null,
        Clip? clipBehavior = null,
        ShapeBorder? shape = null,
        Alignment? alignment = null,
        BoxConstraints? constraints = null,
        bool scrollable = false,
        Key? key = null) : base(key)
    {
        if (elevation.HasValue && (!double.IsFinite(elevation.Value) || elevation.Value < 0))
            throw new ArgumentOutOfRangeException(nameof(elevation));
        if (actionsOverflowButtonSpacing.HasValue
            && (!double.IsFinite(actionsOverflowButtonSpacing.Value) || actionsOverflowButtonSpacing.Value < 0))
            throw new ArgumentOutOfRangeException(nameof(actionsOverflowButtonSpacing));
        Dialog.ValidateInsets(iconPadding, nameof(iconPadding));
        Dialog.ValidateInsets(titlePadding, nameof(titlePadding));
        Dialog.ValidateInsets(contentPadding, nameof(contentPadding));
        Dialog.ValidateInsets(actionsPadding, nameof(actionsPadding));
        Dialog.ValidateInsets(buttonPadding, nameof(buttonPadding));
        Dialog.ValidateInsets(insetPadding, nameof(insetPadding));
        Icon = icon;
        IconPadding = iconPadding;
        IconColor = iconColor;
        Title = title;
        TitlePadding = titlePadding;
        TitleTextStyle = titleTextStyle;
        Content = content;
        ContentPadding = contentPadding;
        ContentTextStyle = contentTextStyle;
        Actions = actions;
        ActionsPadding = actionsPadding;
        ActionsAlignment = actionsAlignment;
        ActionsOverflowAlignment = actionsOverflowAlignment;
        ActionsOverflowDirection = actionsOverflowDirection;
        ActionsOverflowButtonSpacing = actionsOverflowButtonSpacing;
        ButtonPadding = buttonPadding;
        BackgroundColor = backgroundColor;
        Elevation = elevation;
        ShadowColor = shadowColor;
        SurfaceTintColor = surfaceTintColor;
        SemanticLabel = semanticLabel;
        InsetPadding = insetPadding;
        ClipBehavior = clipBehavior;
        Shape = shape;
        Alignment = alignment;
        Constraints = constraints;
        Scrollable = scrollable;
    }

    public Widget? Icon { get; }
    public Thickness? IconPadding { get; }
    public Color? IconColor { get; }
    public Widget? Title { get; }
    public Thickness? TitlePadding { get; }
    public TextStyle? TitleTextStyle { get; }
    public Widget? Content { get; }
    public Thickness? ContentPadding { get; }
    public TextStyle? ContentTextStyle { get; }
    public IReadOnlyList<Widget>? Actions { get; }
    public Thickness? ActionsPadding { get; }
    public MainAxisAlignment? ActionsAlignment { get; }
    public OverflowBarAlignment? ActionsOverflowAlignment { get; }
    public VerticalDirection? ActionsOverflowDirection { get; }
    public double? ActionsOverflowButtonSpacing { get; }
    public Thickness? ButtonPadding { get; }
    public Color? BackgroundColor { get; }
    public double? Elevation { get; }
    public Color? ShadowColor { get; }
    public Color? SurfaceTintColor { get; }
    public string? SemanticLabel { get; }
    public Thickness? InsetPadding { get; }
    public Clip? ClipBehavior { get; }
    public ShapeBorder? Shape { get; }
    public Alignment? Alignment { get; }
    public BoxConstraints? Constraints { get; }
    public bool Scrollable { get; }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var dialogTheme = DialogTheme.Of(context);
        var defaults = Dialog.ResolveDefaults(theme);
        var label = theme.Platform is TargetPlatform.IOS or TargetPlatform.MacOS
            ? SemanticLabel
            : SemanticLabel ?? MaterialLocalizations.Of(context).AlertDialogLabel;
        var paddingScale = ScalePadding(MediaQuery.TextScaleFactorOf(context));

        Widget? iconWidget = null;
        if (Icon is not null)
        {
            var belowIsTitle = Title is not null;
            var belowIsContent = !belowIsTitle && Content is not null;
            var padding = IconPadding ?? new Thickness(
                24,
                24,
                24,
                belowIsTitle ? 16 : belowIsContent ? 0 : 24);
            padding = ScaleTopAndHorizontal(padding, paddingScale, scaleTop: true);
            iconWidget = new Padding(
                padding,
                new IconTheme(
                    new IconThemeData(Color: IconColor ?? dialogTheme.IconColor ?? defaults.IconColor),
                    Icon));
        }

        Widget? titleWidget = null;
        if (Title is not null)
        {
            var padding = TitlePadding ?? new Thickness(24, Icon is null ? 24 : 0, 24, Content is null ? 20 : 0);
            padding = ScaleTopAndHorizontal(padding, paddingScale, scaleTop: Icon is null);
            Widget title = new DefaultTextStyle(
                TitleTextStyle ?? dialogTheme.TitleTextStyle ?? defaults.TitleTextStyle!,
                Title);
            if (Icon is not null) title = new Center(child: title);
            titleWidget = new Padding(padding, new Semantics(container: true, child: title));
        }

        Widget? contentWidget = null;
        if (Content is not null)
        {
            var padding = ContentPadding ?? new Thickness(24, theme.UseMaterial3 ? 16 : 20, 24, 24);
            padding = ScaleTopAndHorizontal(padding, paddingScale, scaleTop: Title is null && Icon is null);
            contentWidget = new Padding(
                padding,
                new DefaultTextStyle(
                    ContentTextStyle ?? dialogTheme.ContentTextStyle ?? defaults.ContentTextStyle!,
                    new Semantics(container: true, explicitChildNodes: true, child: Content)));
        }

        Widget? actionsWidget = null;
        if (Actions is not null)
        {
            var spacing = ((ButtonPadding?.Left ?? 8) + (ButtonPadding?.Right ?? 8)) / 2.0;
            var defaultActionsPadding = theme.UseMaterial3
                ? defaults.ActionsPadding!.Value
                : Add(defaults.ActionsPadding ?? default, new Thickness(spacing));
            actionsWidget = new Padding(
                ActionsPadding ?? dialogTheme.ActionsPadding ?? defaultActionsPadding,
                new OverflowBar(
                    children: Actions,
                    alignment: ActionsAlignment ?? MainAxisAlignment.End,
                    spacing: spacing,
                    overflowAlignment: ActionsOverflowAlignment ?? OverflowBarAlignment.End,
                    overflowDirection: ActionsOverflowDirection ?? VerticalDirection.Down,
                    overflowSpacing: ActionsOverflowButtonSpacing ?? 0));
        }

        var columnChildren = new List<Widget>();
        if (Scrollable)
        {
            var scrollChildren = new List<Widget>();
            if (iconWidget is not null) scrollChildren.Add(iconWidget);
            if (titleWidget is not null) scrollChildren.Add(titleWidget);
            if (contentWidget is not null) scrollChildren.Add(contentWidget);
            if (scrollChildren.Count > 0)
            {
                columnChildren.Add(new Flexible(new SingleChildScrollView(
                    new Column(
                        mainAxisSize: MainAxisSize.Min,
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        children: scrollChildren))));
            }
        }
        else
        {
            if (iconWidget is not null) columnChildren.Add(iconWidget);
            if (titleWidget is not null) columnChildren.Add(titleWidget);
            if (contentWidget is not null) columnChildren.Add(new Flexible(contentWidget));
        }

        if (actionsWidget is not null) columnChildren.Add(actionsWidget);
        Widget dialogChild = new IntrinsicWidth(
            child: new Column(
                mainAxisSize: MainAxisSize.Min,
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children: columnChildren));
        if (label is not null)
        {
            dialogChild = new Semantics(
                label: label,
                scopesRoute: true,
                namesRoute: true,
                explicitChildNodes: true,
                child: dialogChild);
        }

        return new Dialog(
            backgroundColor: BackgroundColor,
            elevation: Elevation,
            shadowColor: ShadowColor,
            surfaceTintColor: SurfaceTintColor,
            insetPadding: InsetPadding,
            clipBehavior: ClipBehavior,
            shape: Shape,
            alignment: Alignment,
            constraints: Constraints,
            semanticsRole: SemanticsRole.AlertDialog,
            child: dialogChild);
    }

    private static double ScalePadding(double textScaleFactor)
    {
        var clamped = Math.Clamp(textScaleFactor, 1, 2);
        return 1.0 + ((1.0 / 3.0 - 1.0) * (clamped - 1.0));
    }

    private static Thickness ScaleTopAndHorizontal(Thickness padding, double factor, bool scaleTop) => new(
        padding.Left * factor,
        scaleTop ? padding.Top * factor : padding.Top,
        padding.Right * factor,
        padding.Bottom);

    private static Thickness Add(Thickness a, Thickness b) => new(
        a.Left + b.Left,
        a.Top + b.Top,
        a.Right + b.Right,
        a.Bottom + b.Bottom);
}

public sealed class DialogRoute<T> : PageRoute
{
    private readonly WidgetBuilder _builder;
    private readonly ThemeData _capturedTheme;
    private readonly DialogThemeData _capturedDialogTheme;
    private readonly MediaQueryData _capturedMediaQuery;
    private readonly TextDirection _capturedDirection;
    private readonly Plumix.AnimationController _animation;
    private readonly TaskCompletionSource<T?> _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private object? _pendingResult;
    private bool _isExiting;

    public DialogRoute(
        BuildContext context,
        WidgetBuilder builder,
        Color? barrierColor = null,
        bool barrierDismissible = true,
        string? barrierLabel = null,
        bool useSafeArea = true,
        RouteSettings? settings = null,
        bool fullscreenDialog = false,
        TimeSpan? transitionDuration = null) : base(settings, fullscreenDialog)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _capturedTheme = Theme.Of(context);
        _capturedDialogTheme = DialogTheme.Of(context);
        _capturedMediaQuery = MediaQuery.Of(context);
        _capturedDirection = Directionality.Of(context);
        BarrierColor = barrierColor
                       ?? _capturedDialogTheme.BarrierColor
                       ?? Color.FromArgb(0x8A, 0, 0, 0);
        BarrierDismissible = barrierDismissible;
        BarrierLabel = barrierLabel ?? MaterialLocalizations.Of(context).ModalBarrierDismissLabel;
        UseSafeArea = useSafeArea;
        TransitionDuration = transitionDuration ?? TimeSpan.FromMilliseconds(150);
        if (TransitionDuration < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(transitionDuration));
        _animation = new Plumix.AnimationController(TransitionDuration) { Curve = Curves.EaseOut };
        _animation.Changed += HandleAnimationChanged;
        _animation.Dismissed += HandleDismissed;
    }

    public override bool Opaque => false;
    public Color BarrierColor { get; }
    public bool BarrierDismissible { get; }
    public string BarrierLabel { get; }
    public bool UseSafeArea { get; }
    public TimeSpan TransitionDuration { get; }
    public Task<T?> Completed => _completed.Task;

    protected override void OnAttach()
    {
        _animation.Forward(from: 0);
    }

    public override bool WillPop(object? result)
    {
        if (_isExiting || _animation.Value <= 0) return base.WillPop(result);
        _pendingResult = result;
        _isExiting = true;
        _animation.Reverse();
        return false;
    }

    public override void DidComplete(object? result)
    {
        if (result is null)
        {
            _completed.TrySetResult(default);
        }
        else if (result is T typed)
        {
            _completed.TrySetResult(typed);
        }
        else
        {
            _completed.TrySetException(new InvalidCastException(
                $"Dialog result of type {result.GetType().Name} cannot be converted to {typeof(T).Name}."));
        }
    }

    public override Widget BuildPage(BuildContext context)
    {
        var progress = Math.Clamp(_animation.Value, 0, 1);
        var barrier = new Semantics(
            label: BarrierLabel,
            container: true,
            onTap: BarrierDismissible ? () => Navigator?.MaybePop() : null,
            child: new GestureDetector(
                behavior: HitTestBehavior.Opaque,
                onTap: BarrierDismissible ? () => Navigator?.MaybePop() : null,
                child: new ColoredBox(ApplyOpacity(BarrierColor, progress))));

        Widget page = new Builder(_builder);
        if (UseSafeArea) page = new SafeArea(child: page);
        page = new Opacity(progress, page);
        page = new DialogTheme(_capturedDialogTheme, page);
        page = new Theme(_capturedTheme, page);
        page = new MediaQuery(_capturedMediaQuery, page);
        page = new Directionality(_capturedDirection, page);
        return new Stack(
            fit: StackFit.Expand,
            children:
            [
                new Positioned(left: 0, top: 0, right: 0, bottom: 0, child: barrier),
                page,
            ]);
    }

    public override void Dispose()
    {
        _animation.Changed -= HandleAnimationChanged;
        _animation.Dismissed -= HandleDismissed;
        _animation.Dispose();
        if (!_completed.Task.IsCompleted) _completed.TrySetResult(default);
        base.Dispose();
    }

    private void HandleAnimationChanged() => NotifyRouteChanged();

    private void HandleDismissed()
    {
        if (_isExiting) Navigator?.MaybePop(_pendingResult);
    }

    private static Color ApplyOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Round(color.A * Math.Clamp(opacity, 0, 1)), color.R, color.G, color.B);
}

public static class MaterialDialogs
{
    public static Task<T?> ShowDialog<T>(
        BuildContext context,
        WidgetBuilder builder,
        bool barrierDismissible = true,
        Color? barrierColor = null,
        string? barrierLabel = null,
        bool useSafeArea = true,
        RouteSettings? routeSettings = null,
        bool fullscreenDialog = false,
        TimeSpan? transitionDuration = null)
    {
        var route = new DialogRoute<T>(
            context,
            builder,
            barrierColor: barrierColor,
            barrierDismissible: barrierDismissible,
            barrierLabel: barrierLabel,
            useSafeArea: useSafeArea,
            settings: routeSettings,
            fullscreenDialog: fullscreenDialog,
            transitionDuration: transitionDuration);
        Navigator.Of(context).Push(route);
        return route.Completed;
    }
}
