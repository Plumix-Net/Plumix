using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/dialog.dart

public class Dialog : StatelessWidget
{
    internal static readonly Thickness DefaultInsetPadding = new(40, 24);

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
        AlignmentGeometry? alignment = null,
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
        insetAnimationCurve: insetAnimationCurve ?? Curves.Decelerate,
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
        AlignmentGeometry? alignment,
        Widget? child,
        SemanticsRole semanticsRole,
        BoxConstraints? constraints,
        Key? key) : base(key)
    {
        if (elevation.HasValue && (!double.IsFinite(elevation.Value) || elevation.Value < 0))
            throw new ArgumentOutOfRangeException(nameof(elevation));
        if (insetAnimationDuration < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(insetAnimationDuration));
        DialogThemeData.ValidateInsets(insetPadding, nameof(insetPadding));
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
    public AlignmentGeometry? Alignment { get; }
    public Widget? Child { get; }
    public SemanticsRole SemanticsRole { get; }
    public BoxConstraints? Constraints { get; }
    public bool IsFullscreen { get; }

    /// <summary>Dart's `Dialog.fullscreen` named constructor.</summary>
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
        insetAnimationCurve: insetAnimationCurve ?? Curves.Decelerate,
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
            dialogChild = new Material(
                color: BackgroundColor ?? dialogTheme.BackgroundColor ?? defaults.BackgroundColor!.Value,
                child: Child);
        }
        else
        {
            var constraints = Constraints ?? dialogTheme.Constraints ?? new BoxConstraints(MinWidth: 280);
            dialogChild = new Align(
                alignment: Alignment ?? dialogTheme.Alignment ?? defaults.Alignment!.Value,
                child: new ConstrainedBox(
                    constraints,
                    new Material(
                        color: BackgroundColor ?? dialogTheme.BackgroundColor ?? defaults.BackgroundColor!.Value,
                        elevation: Elevation ?? dialogTheme.Elevation ?? defaults.Elevation!.Value,
                        shadowColor: ShadowColor ?? dialogTheme.ShadowColor ?? defaults.ShadowColor,
                        surfaceTintColor: SurfaceTintColor
                                          ?? dialogTheme.SurfaceTintColor
                                          ?? defaults.SurfaceTintColor,
                        shape: Shape ?? dialogTheme.Shape ?? defaults.Shape!,
                        type: MaterialType.Card,
                        clipBehavior: ClipBehavior ?? dialogTheme.ClipBehavior ?? defaults.ClipBehavior ?? Clip.None,
                        child: Child)));
        }

        dialogChild = MediaQuery.RemoveViewInsets(
            context,
            dialogChild,
            removeLeft: true,
            removeTop: true,
            removeRight: true,
            removeBottom: true);
        dialogChild = new AnimatedPadding(
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
                Shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(4)),
                Alignment: Plumix.Rendering.Alignment.Center,
                IconColor: theme.IconTheme.Color,
                TitleTextStyle: theme.TextTheme.TitleLarge,
                ContentTextStyle: theme.TextTheme.TitleMedium,
                ActionsPadding: EdgeInsetsGeometry.Zero,
                ClipBehavior: Clip.None);
        }

        return new DialogThemeData(
            BackgroundColor: theme.SurfaceContainerHighColor,
            Elevation: 6,
            ShadowColor: Colors.Transparent,
            SurfaceTintColor: Colors.Transparent,
            Shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(28)),
            Alignment: Plumix.Rendering.Alignment.Center,
            IconColor: theme.SecondaryColor,
            TitleTextStyle: theme.TextTheme.HeadlineSmall,
            ContentTextStyle: theme.TextTheme.BodyMedium,
            ActionsPadding: EdgeInsetsGeometry.Only(left: 24, right: 24, bottom: 24),
            ClipBehavior: Clip.None);
    }

    /// <summary>Dart's `_scalePadding`: 1.0 at text scale 1.0, shrinking to 1/3 at scale 2.0.</summary>
    internal static double ScalePadding(double textScaleFactor)
    {
        double clamped = Math.Clamp(textScaleFactor, 1, 2);
        return 1.0 + ((1.0 / 3.0 - 1.0) * (clamped - 1.0));
    }

    internal static Thickness Add(Thickness a, Thickness b) => new(
        a.Left + b.Left,
        a.Top + b.Top,
        a.Right + b.Right,
        a.Bottom + b.Bottom);
}

public sealed class SimpleDialogOption : StatelessWidget
{
    public SimpleDialogOption(
        Action? onPressed = null,
        Thickness? padding = null,
        Widget? child = null,
        Key? key = null) : base(key)
    {
        DialogThemeData.ValidateInsets(padding, nameof(padding));
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
    public SimpleDialog(
        Widget? title = null,
        EdgeInsetsGeometry? titlePadding = null,
        TextStyle? titleTextStyle = null,
        IReadOnlyList<Widget>? children = null,
        EdgeInsetsGeometry? contentPadding = null,
        TextStyle? contentTextStyle = null,
        Color? backgroundColor = null,
        double? elevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        string? semanticLabel = null,
        Thickness? insetPadding = null,
        Clip? clipBehavior = null,
        ShapeBorder? shape = null,
        AlignmentGeometry? alignment = null,
        BoxConstraints? constraints = null,
        Key? key = null) : base(key)
    {
        if (elevation.HasValue && (!double.IsFinite(elevation.Value) || elevation.Value < 0))
            throw new ArgumentOutOfRangeException(nameof(elevation));
        DialogThemeData.ValidateInsets(insetPadding, nameof(insetPadding));
        Title = title;
        TitlePadding = titlePadding ?? EdgeInsetsGeometry.FromLTRB(24, 24, 24, 0);
        TitleTextStyle = titleTextStyle;
        Children = children;
        ContentPadding = contentPadding ?? EdgeInsetsGeometry.FromLTRB(0, 12, 0, 16);
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
    public EdgeInsetsGeometry TitlePadding { get; }
    public TextStyle? TitleTextStyle { get; }
    public IReadOnlyList<Widget>? Children { get; }
    public EdgeInsetsGeometry ContentPadding { get; }
    public TextStyle? ContentTextStyle { get; }
    public Color? BackgroundColor { get; }
    public double? Elevation { get; }
    public Color? ShadowColor { get; }
    public Color? SurfaceTintColor { get; }
    public string? SemanticLabel { get; }
    public Thickness? InsetPadding { get; }
    public Clip? ClipBehavior { get; }
    public ShapeBorder? Shape { get; }
    public AlignmentGeometry? Alignment { get; }
    public BoxConstraints? Constraints { get; }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var dialogTheme = DialogTheme.Of(context);
        var defaults = Dialog.ResolveDefaults(theme);
        TargetPlatform hostPlatform = PlatformDefaults.TargetPlatform;
        string? label = hostPlatform is TargetPlatform.IOS or TargetPlatform.MacOS
            ? SemanticLabel
            : SemanticLabel ?? MaterialLocalizations.Of(context).DialogLabel;
        var effectiveTitleTextStyle = TitleTextStyle ?? dialogTheme.TitleTextStyle ?? theme.TextTheme.TitleLarge;
        double fontSizeToScale = effectiveTitleTextStyle.FontSize is { } fontSize && fontSize != 0.0
            ? fontSize
            : 14.0;
        double paddingScale = Dialog.ScalePadding(
            MediaQuery.TextScalerOf(context).Scale(fontSizeToScale) / fontSizeToScale);
        TextDirection direction = Directionality.MaybeOf(context) ?? TextDirection.Ltr;

        Widget? titleWidget = null;
        if (Title is not null)
        {
            Thickness titlePadding = TitlePadding.Resolve(direction);
            var padding = new Thickness(
                titlePadding.Left * paddingScale,
                titlePadding.Top * paddingScale,
                titlePadding.Right * paddingScale,
                Children is null ? titlePadding.Bottom * paddingScale : titlePadding.Bottom);
            titleWidget = new Padding(
                padding,
                new DefaultTextStyle(
                    effectiveTitleTextStyle,
                    new Semantics(
                        namesRoute: label is null && hostPlatform != TargetPlatform.IOS,
                        container: true,
                        child: Title)));
        }

        Widget? contentWidget = null;
        if (Children is not null)
        {
            Thickness contentPadding = ContentPadding.Resolve(direction);
            var padding = new Thickness(
                contentPadding.Left * paddingScale,
                Title is null ? contentPadding.Top * paddingScale : contentPadding.Top,
                contentPadding.Right * paddingScale,
                contentPadding.Bottom * paddingScale);
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
}

public class AlertDialog : StatelessWidget
{
    public AlertDialog(
        Widget? icon = null,
        EdgeInsetsGeometry? iconPadding = null,
        Color? iconColor = null,
        Widget? title = null,
        EdgeInsetsGeometry? titlePadding = null,
        TextStyle? titleTextStyle = null,
        Widget? content = null,
        EdgeInsetsGeometry? contentPadding = null,
        TextStyle? contentTextStyle = null,
        IReadOnlyList<Widget>? actions = null,
        EdgeInsetsGeometry? actionsPadding = null,
        MainAxisAlignment? actionsAlignment = null,
        OverflowBarAlignment? actionsOverflowAlignment = null,
        VerticalDirection? actionsOverflowDirection = null,
        double? actionsOverflowButtonSpacing = null,
        EdgeInsetsGeometry? buttonPadding = null,
        Color? backgroundColor = null,
        double? elevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        string? semanticLabel = null,
        Thickness? insetPadding = null,
        Clip? clipBehavior = null,
        ShapeBorder? shape = null,
        AlignmentGeometry? alignment = null,
        BoxConstraints? constraints = null,
        bool scrollable = false,
        Key? key = null) : base(key)
    {
        if (elevation.HasValue && (!double.IsFinite(elevation.Value) || elevation.Value < 0))
            throw new ArgumentOutOfRangeException(nameof(elevation));
        if (actionsOverflowButtonSpacing.HasValue
            && (!double.IsFinite(actionsOverflowButtonSpacing.Value) || actionsOverflowButtonSpacing.Value < 0))
            throw new ArgumentOutOfRangeException(nameof(actionsOverflowButtonSpacing));
        DialogThemeData.ValidateInsets(insetPadding, nameof(insetPadding));
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
    public EdgeInsetsGeometry? IconPadding { get; }
    public Color? IconColor { get; }
    public Widget? Title { get; }
    public EdgeInsetsGeometry? TitlePadding { get; }
    public TextStyle? TitleTextStyle { get; }
    public Widget? Content { get; }
    public EdgeInsetsGeometry? ContentPadding { get; }
    public TextStyle? ContentTextStyle { get; }
    public IReadOnlyList<Widget>? Actions { get; }
    public EdgeInsetsGeometry? ActionsPadding { get; }
    public MainAxisAlignment? ActionsAlignment { get; }
    public OverflowBarAlignment? ActionsOverflowAlignment { get; }
    public VerticalDirection? ActionsOverflowDirection { get; }
    public double? ActionsOverflowButtonSpacing { get; }
    public EdgeInsetsGeometry? ButtonPadding { get; }
    public Color? BackgroundColor { get; }
    public double? Elevation { get; }
    public Color? ShadowColor { get; }
    public Color? SurfaceTintColor { get; }
    public string? SemanticLabel { get; }
    public Thickness? InsetPadding { get; }
    public Clip? ClipBehavior { get; }
    public ShapeBorder? Shape { get; }
    public AlignmentGeometry? Alignment { get; }
    public BoxConstraints? Constraints { get; }
    public bool Scrollable { get; }

    /// <summary>Dart's `AlertDialog.adaptive`: Cupertino on iOS/macOS, Material elsewhere.</summary>
    public static AlertDialog Adaptive(
        Widget? icon = null,
        EdgeInsetsGeometry? iconPadding = null,
        Color? iconColor = null,
        Widget? title = null,
        EdgeInsetsGeometry? titlePadding = null,
        TextStyle? titleTextStyle = null,
        Widget? content = null,
        EdgeInsetsGeometry? contentPadding = null,
        TextStyle? contentTextStyle = null,
        IReadOnlyList<Widget>? actions = null,
        EdgeInsetsGeometry? actionsPadding = null,
        MainAxisAlignment? actionsAlignment = null,
        OverflowBarAlignment? actionsOverflowAlignment = null,
        VerticalDirection? actionsOverflowDirection = null,
        double? actionsOverflowButtonSpacing = null,
        EdgeInsetsGeometry? buttonPadding = null,
        Color? backgroundColor = null,
        double? elevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        string? semanticLabel = null,
        Thickness? insetPadding = null,
        Clip? clipBehavior = null,
        ShapeBorder? shape = null,
        AlignmentGeometry? alignment = null,
        BoxConstraints? constraints = null,
        bool scrollable = false,
        ScrollController? scrollController = null,
        ScrollController? actionScrollController = null,
        TimeSpan? insetAnimationDuration = null,
        Curve? insetAnimationCurve = null,
        Key? key = null) => new AdaptiveAlertDialog(
        icon, iconPadding, iconColor, title, titlePadding, titleTextStyle, content, contentPadding,
        contentTextStyle, actions, actionsPadding, actionsAlignment, actionsOverflowAlignment,
        actionsOverflowDirection, actionsOverflowButtonSpacing, buttonPadding, backgroundColor,
        elevation, shadowColor, surfaceTintColor, semanticLabel, insetPadding, clipBehavior, shape,
        alignment, constraints, scrollable, scrollController, actionScrollController,
        insetAnimationDuration ?? TimeSpan.FromMilliseconds(100), insetAnimationCurve ?? Curves.Decelerate,
        key);

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var dialogTheme = DialogTheme.Of(context);
        var defaults = Dialog.ResolveDefaults(theme);
        TargetPlatform hostPlatform = PlatformDefaults.TargetPlatform;
        string? label = hostPlatform is TargetPlatform.IOS or TargetPlatform.MacOS
            ? SemanticLabel
            : SemanticLabel ?? MaterialLocalizations.Of(context).AlertDialogLabel;
        double paddingScale = Dialog.ScalePadding(MediaQuery.TextScalerOf(context).Scale(14.0) / 14.0);
        TextDirection direction = Directionality.MaybeOf(context) ?? TextDirection.Ltr;

        Widget? iconWidget = null;
        if (Icon is not null)
        {
            bool belowIsTitle = Title is not null;
            bool belowIsContent = !belowIsTitle && Content is not null;
            Thickness padding = (IconPadding ?? EdgeInsetsGeometry.Only(
                left: 24,
                top: 24,
                right: 24,
                bottom: belowIsTitle ? 16 : belowIsContent ? 0 : 24)).Resolve(direction);
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
            Thickness padding = (TitlePadding ?? EdgeInsetsGeometry.Only(
                left: 24,
                top: Icon is null ? 24 : 0,
                right: 24,
                bottom: Content is null ? 20 : 0)).Resolve(direction);
            padding = ScaleTopAndHorizontal(padding, paddingScale, scaleTop: Icon is null);
            titleWidget = new Padding(
                padding,
                new DefaultTextStyle(
                    TitleTextStyle ?? dialogTheme.TitleTextStyle ?? defaults.TitleTextStyle!,
                    textAlign: Icon is null ? TextAlign.Start : TextAlign.Center,
                    child: new Semantics(
                        namesRoute: label is null && hostPlatform != TargetPlatform.IOS,
                        container: true,
                        child: Title)));
        }

        Widget? contentWidget = null;
        if (Content is not null)
        {
            Thickness padding = (ContentPadding ?? EdgeInsetsGeometry.Only(
                left: 24,
                top: theme.UseMaterial3 ? 16 : 20,
                right: 24,
                bottom: 24)).Resolve(direction);
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
            double spacing = (ButtonPadding?.Horizontal ?? 16) / 2.0;
            Thickness defaultActionsPadding = theme.UseMaterial3
                ? defaults.ActionsPadding!.Value.Resolve(direction)
                : defaults.ActionsPadding!.Value.Add(EdgeInsetsGeometry.All(spacing)).Resolve(direction);
            actionsWidget = new Padding(
                (ActionsPadding ?? dialogTheme.ActionsPadding)?.Resolve(direction) ?? defaultActionsPadding,
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

    private static Thickness ScaleTopAndHorizontal(Thickness padding, double factor, bool scaleTop) => new(
        padding.Left * factor,
        scaleTop ? padding.Top * factor : padding.Top,
        padding.Right * factor,
        padding.Bottom);
}

/// <summary>Dart's private `_AdaptiveAlertDialog`.</summary>
internal sealed class AdaptiveAlertDialog : AlertDialog
{
    private readonly ScrollController? _scrollController;
    private readonly ScrollController? _actionScrollController;
    private readonly TimeSpan _insetAnimationDuration;
    private readonly Curve _insetAnimationCurve;

    public AdaptiveAlertDialog(
        Widget? icon,
        EdgeInsetsGeometry? iconPadding,
        Color? iconColor,
        Widget? title,
        EdgeInsetsGeometry? titlePadding,
        TextStyle? titleTextStyle,
        Widget? content,
        EdgeInsetsGeometry? contentPadding,
        TextStyle? contentTextStyle,
        IReadOnlyList<Widget>? actions,
        EdgeInsetsGeometry? actionsPadding,
        MainAxisAlignment? actionsAlignment,
        OverflowBarAlignment? actionsOverflowAlignment,
        VerticalDirection? actionsOverflowDirection,
        double? actionsOverflowButtonSpacing,
        EdgeInsetsGeometry? buttonPadding,
        Color? backgroundColor,
        double? elevation,
        Color? shadowColor,
        Color? surfaceTintColor,
        string? semanticLabel,
        Thickness? insetPadding,
        Clip? clipBehavior,
        ShapeBorder? shape,
        AlignmentGeometry? alignment,
        BoxConstraints? constraints,
        bool scrollable,
        ScrollController? scrollController,
        ScrollController? actionScrollController,
        TimeSpan insetAnimationDuration,
        Curve insetAnimationCurve,
        Key? key) : base(
        icon, iconPadding, iconColor, title, titlePadding, titleTextStyle, content, contentPadding,
        contentTextStyle, actions, actionsPadding, actionsAlignment, actionsOverflowAlignment,
        actionsOverflowDirection, actionsOverflowButtonSpacing, buttonPadding, backgroundColor,
        elevation, shadowColor, surfaceTintColor, semanticLabel, insetPadding, clipBehavior, shape,
        alignment, constraints, scrollable, key)
    {
        _scrollController = scrollController;
        _actionScrollController = actionScrollController;
        _insetAnimationDuration = insetAnimationDuration;
        _insetAnimationCurve = insetAnimationCurve;
    }

    public override Widget Build(BuildContext context)
    {
        return Theme.Of(context).Platform switch
        {
            TargetPlatform.IOS or TargetPlatform.MacOS => new CupertinoAlertDialog(
                title: Title,
                content: Content,
                actions: Actions ?? [],
                scrollController: _scrollController,
                actionScrollController: _actionScrollController,
                insetAnimationDuration: _insetAnimationDuration,
                insetAnimationCurve: _insetAnimationCurve),
            _ => base.Build(context),
        };
    }
}

public sealed class DialogRoute<T> : RawDialogRoute<T>
{
    private readonly AnimationStyle? _animationStyle;
    private CurvedAnimation? _curvedAnimation;
    private Animation<double>? _curvedAnimationParent;

    public DialogRoute(
        BuildContext context,
        WidgetBuilder builder,
        CapturedThemes? themes = null,
        Color? barrierColor = null,
        bool barrierDismissible = true,
        string? barrierLabel = null,
        bool useSafeArea = true,
        RouteSettings? settings = null,
        bool? requestFocus = null,
        Point? anchorPoint = null,
        TraversalEdgeBehavior? traversalEdgeBehavior = null,
        bool fullscreenDialog = false,
        AnimationStyle? animationStyle = null) : base(
        pageBuilder: (pageContext, _, _) => BuildDialogPage(builder, themes, useSafeArea),
        barrierDismissible: barrierDismissible,
        barrierColor: barrierColor ?? Color.FromArgb(0x8A, 0, 0, 0),
        barrierLabel: barrierLabel ?? MaterialLocalizations.Of(context).ModalBarrierDismissLabel,
        transitionDuration: animationStyle?.Duration ?? TimeSpan.FromMilliseconds(150),
        transitionBuilder: static (_, _, _, child) => child,
        settings: settings,
        requestFocus: requestFocus,
        anchorPoint: anchorPoint,
        traversalEdgeBehavior: traversalEdgeBehavior,
        fullscreenDialog: fullscreenDialog)
    {
        _animationStyle = animationStyle;
    }

    private static Widget BuildDialogPage(WidgetBuilder builder, CapturedThemes? themes, bool useSafeArea)
    {
        Widget pageChild = new Builder(builder);
        Widget dialog = themes?.Wrap(pageChild) ?? pageChild;
        if (useSafeArea)
        {
            dialog = new SafeArea(child: dialog);
        }

        // Blocks taps on the dialog surface from reaching the dismissing barrier behind it.
        return new Semantics(hitTestBehavior: SemanticsHitTestBehavior.Opaque, child: dialog);
    }

    public override Widget BuildTransitions(
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        Widget child)
    {
        if (!ReferenceEquals(_curvedAnimationParent, animation))
        {
            _curvedAnimation?.Dispose();
            _curvedAnimation = new CurvedAnimation(
                animation,
                _animationStyle?.Curve ?? Curves.EaseOut,
                _animationStyle?.ReverseCurve ?? Curves.EaseOut);
            _curvedAnimationParent = animation;
        }

        return new FadeTransition(
            opacity: _curvedAnimation!,
            child: base.BuildTransitions(context, animation, secondaryAnimation, child));
    }

    public override void Dispose()
    {
        _curvedAnimation?.Dispose();
        base.Dispose();
    }
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
        bool useRootNavigator = true,
        Point? anchorPoint = null,
        TraversalEdgeBehavior? traversalEdgeBehavior = null,
        bool? requestFocus = null,
        AnimationStyle? animationStyle = null)
    {
        NavigatorState navigator = Navigator.Of(context, rootNavigator: useRootNavigator);
        CapturedThemes themes = InheritedTheme.Capture(from: context, to: navigator.Context);
        var route = new DialogRoute<T>(
            context,
            builder,
            themes: themes,
            barrierColor: barrierColor
                          ?? DialogTheme.Of(context).BarrierColor
                          ?? Color.FromArgb(0x8A, 0, 0, 0),
            barrierDismissible: barrierDismissible,
            barrierLabel: barrierLabel,
            useSafeArea: useSafeArea,
            settings: routeSettings,
            requestFocus: requestFocus,
            anchorPoint: anchorPoint,
            traversalEdgeBehavior: traversalEdgeBehavior ?? TraversalEdgeBehavior.ClosedLoop,
            fullscreenDialog: fullscreenDialog,
            animationStyle: animationStyle);
        navigator.Push(route);
        return route.Completed;
    }

    /// <summary>Dart's `showAdaptiveDialog`: Cupertino presentation on iOS/macOS.</summary>
    public static Task<T?> ShowAdaptiveDialog<T>(
        BuildContext context,
        WidgetBuilder builder,
        bool? barrierDismissible = null,
        Color? barrierColor = null,
        string? barrierLabel = null,
        bool useSafeArea = true,
        bool useRootNavigator = true,
        RouteSettings? routeSettings = null,
        Point? anchorPoint = null,
        TraversalEdgeBehavior? traversalEdgeBehavior = null,
        bool? requestFocus = null,
        AnimationStyle? animationStyle = null)
    {
        return Theme.Of(context).Platform switch
        {
            TargetPlatform.IOS or TargetPlatform.MacOS => CupertinoDialogs.ShowCupertinoDialog<T>(
                context,
                builder,
                barrierLabel: barrierLabel,
                useRootNavigator: useRootNavigator,
                barrierDismissible: barrierDismissible ?? false,
                routeSettings: routeSettings,
                anchorPoint: anchorPoint,
                requestFocus: requestFocus),
            _ => ShowDialog<T>(
                context,
                builder,
                barrierDismissible: barrierDismissible ?? true,
                barrierColor: barrierColor,
                barrierLabel: barrierLabel,
                useSafeArea: useSafeArea,
                routeSettings: routeSettings,
                useRootNavigator: useRootNavigator,
                anchorPoint: anchorPoint,
                traversalEdgeBehavior: traversalEdgeBehavior,
                requestFocus: requestFocus,
                animationStyle: animationStyle),
        };
    }
}
