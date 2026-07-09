using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using RelativeRect = Plumix.Rendering.RelativeRect;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/popup_menu.dart

public delegate IReadOnlyList<PopupMenuEntry> PopupMenuItemBuilder<T>(BuildContext context);
public delegate RelativeRect PopupMenuPositionBuilder(BuildContext context, BoxConstraints constraints);

public abstract class PopupMenuEntry : StatefulWidget
{
    protected PopupMenuEntry(Key? key = null) : base(key) { }

    public abstract double Height { get; }
    public abstract bool Represents(object? value);
    internal virtual bool IsEnabled => true;
}

public abstract class PopupMenuEntry<T> : PopupMenuEntry
{
    protected PopupMenuEntry(Key? key = null) : base(key) { }

    public abstract bool Represents(T? value);

    public sealed override bool Represents(object? value)
    {
        if (value is null) return Represents(default);
        return value is T typed && Represents(typed);
    }
}

public sealed class PopupMenuDivider : PopupMenuEntry
{
    public PopupMenuDivider(
        double height = 16,
        double? thickness = null,
        double? indent = null,
        double? endIndent = null,
        BorderRadius? radius = null,
        Color? color = null,
        Key? key = null) : base(key)
    {
        Divider.ValidateNonNegativeFinite(height, nameof(height));
        Divider.ValidateNonNegativeFinite(thickness, nameof(thickness));
        Divider.ValidateNonNegativeFinite(indent, nameof(indent));
        Divider.ValidateNonNegativeFinite(endIndent, nameof(endIndent));
        Height = height;
        Thickness = thickness;
        Indent = indent;
        EndIndent = endIndent;
        Radius = radius;
        Color = color;
    }

    public override double Height { get; }
    public double? Thickness { get; }
    public double? Indent { get; }
    public double? EndIndent { get; }
    public BorderRadius? Radius { get; }
    public Color? Color { get; }
    internal override bool IsEnabled => false;

    public override bool Represents(object? value) => false;

    public override State CreateState() => new PopupMenuDividerState();

    private sealed class PopupMenuDividerState : State
    {
        private PopupMenuDivider CurrentWidget => (PopupMenuDivider)StateWidget;

        public override Widget Build(BuildContext context) => new Divider(
            height: CurrentWidget.Height,
            thickness: CurrentWidget.Thickness,
            indent: CurrentWidget.Indent,
            endIndent: CurrentWidget.EndIndent,
            radius: CurrentWidget.Radius,
            color: CurrentWidget.Color);
    }
}

public class PopupMenuItem<T> : PopupMenuEntry<T>
{
    public PopupMenuItem(
        Widget? child,
        T? value = default,
        Action? onTap = null,
        bool enabled = true,
        double height = 48,
        Thickness? padding = null,
        TextStyle? textStyle = null,
        MaterialStateProperty<TextStyle?>? labelTextStyle = null,
        MouseCursor? mouseCursor = null,
        Key? key = null) : base(key)
    {
        if (!double.IsFinite(height) || height < 0) throw new ArgumentOutOfRangeException(nameof(height));
        ValidateInsets(padding, nameof(padding));
        Child = child;
        Value = value;
        OnTap = onTap;
        Enabled = enabled;
        Height = height;
        Padding = padding;
        TextStyle = textStyle;
        LabelTextStyle = labelTextStyle;
        MouseCursor = mouseCursor;
    }

    public Widget? Child { get; }
    public T? Value { get; }
    public Action? OnTap { get; }
    public bool Enabled { get; }
    public override double Height { get; }
    public Thickness? Padding { get; }
    public TextStyle? TextStyle { get; }
    public MaterialStateProperty<TextStyle?>? LabelTextStyle { get; }
    public MouseCursor? MouseCursor { get; }
    internal override bool IsEnabled => Enabled;

    public override bool Represents(T? value) => EqualityComparer<T?>.Default.Equals(value, Value);

    public override State CreateState() => new PopupMenuItemState<T>();

    internal void InvokeOnTap() => OnTap?.Invoke();

    private static void ValidateInsets(Thickness? value, string name)
    {
        if (!value.HasValue) return;
        var p = value.Value;
        if (!double.IsFinite(p.Left) || !double.IsFinite(p.Top)
            || !double.IsFinite(p.Right) || !double.IsFinite(p.Bottom)
            || p.Left < 0 || p.Top < 0 || p.Right < 0 || p.Bottom < 0)
            throw new ArgumentOutOfRangeException(name);
    }
}

public class PopupMenuItemState<T> : State
{
    protected PopupMenuItem<T> CurrentWidget => (PopupMenuItem<T>)StateWidget;

    protected virtual Widget? BuildChild() => CurrentWidget.Child;

    public override Widget Build(BuildContext context)
    {
        var widget = CurrentWidget;
        var theme = Theme.Of(context);
        var popupTheme = PopupMenuTheme.Of(context);
        var states = widget.Enabled ? MaterialState.None : MaterialState.Disabled;
        TextStyle style;
        if (theme.UseMaterial3)
        {
            style = widget.LabelTextStyle?.Resolve(states)
                    ?? popupTheme.LabelTextStyle?.Resolve(states)
                    ?? theme.TextTheme.LabelLarge.CopyWith(
                        color: widget.Enabled
                            ? theme.OnSurfaceColor
                            : ApplyOpacity(theme.OnSurfaceColor, 0.38));
        }
        else
        {
            style = widget.TextStyle ?? popupTheme.TextStyle ?? theme.TextTheme.TitleMedium;
            if (!widget.Enabled) style = style.CopyWith(color: ApplyOpacity(theme.OnSurfaceColor, 0.38));
        }

        var padding = widget.Padding ?? (theme.UseMaterial3 ? new Thickness(12, 0) : new Thickness(16, 0));
        var alignment = Directionality.Of(context) == TextDirection.Rtl
            ? Alignment.CenterRight
            : Alignment.CenterLeft;
        Widget item = new DefaultTextStyle(
            style,
            new ConstrainedBox(
                new BoxConstraints(MinHeight: widget.Height),
                new Padding(
                    padding,
                    new Align(
                        alignment: alignment,
                        widthFactor: 1,
                        child: BuildChild() ?? new SizedBox()))));
        item = new InkWell(
            onTap: widget.Enabled ? HandleTap : null,
            canRequestFocus: widget.Enabled,
            mouseCursor: ResolveMouseCursor(widget, popupTheme, states),
            child: new ListTileTheme(
                new ListTileThemeData(
                    ContentPadding: default,
                    TitleTextStyle: style),
                item));
        return new MergeSemantics(BuildSemantics(item));
    }

    protected virtual Widget BuildSemantics(Widget child) => new Semantics(
        role: SemanticsRole.MenuItem,
        flags: CurrentWidget.Enabled ? SemanticsFlags.IsEnabled : SemanticsFlags.None,
        onTap: CurrentWidget.Enabled ? HandleTap : null,
        child: child);

    protected virtual void HandleTap()
    {
        var widget = CurrentWidget;
        Navigator.Pop(Context, widget.Value);
        widget.InvokeOnTap();
    }

    private static MouseCursor ResolveMouseCursor(
        PopupMenuItem<T> widget,
        PopupMenuThemeData theme,
        MaterialState states) =>
        widget.MouseCursor ?? theme.MouseCursor?.Resolve(states) ?? SystemMouseCursors.Click;

    private static Color ApplyOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Round(color.A * Math.Clamp(opacity, 0, 1)), color.R, color.G, color.B);
}

public sealed class CheckedPopupMenuItem<T> : PopupMenuItem<T>
{
    public CheckedPopupMenuItem(
        Widget? child,
        T? value = default,
        bool @checked = false,
        Action? onTap = null,
        bool enabled = true,
        double height = 48,
        Thickness? padding = null,
        MaterialStateProperty<TextStyle?>? labelTextStyle = null,
        MouseCursor? mouseCursor = null,
        Key? key = null)
        : base(
            child: child,
            value: value,
            onTap: onTap,
            enabled: enabled,
            height: height,
            padding: padding,
            labelTextStyle: labelTextStyle,
            mouseCursor: mouseCursor,
            key: key)
    {
        Checked = @checked;
    }

    public bool Checked { get; }

    public override State CreateState() => new CheckedPopupMenuItemState<T>();
}

internal sealed class CheckedPopupMenuItemState<T> : PopupMenuItemState<T>
{
    private static readonly TimeSpan FadeDuration = TimeSpan.FromMilliseconds(150);
    private AnimationController? _controller;
    private double _opacity;

    private CheckedPopupMenuItem<T> CheckedWidget => (CheckedPopupMenuItem<T>)StateWidget;

    public override void InitState()
    {
        _opacity = CheckedWidget.Checked ? 1 : 0;
        _controller = new AnimationController(FadeDuration);
        _controller.Forward(from: _opacity);
        _controller.Stop();
        _controller.Changed += HandleAnimationChanged;
    }

    public override void Dispose()
    {
        if (_controller is null) return;
        _controller.Changed -= HandleAnimationChanged;
        _controller.Dispose();
        _controller = null;
    }

    protected override void HandleTap()
    {
        if (CheckedWidget.Checked)
        {
            _controller!.Reverse();
        }
        else
        {
            _controller!.Forward();
        }
        base.HandleTap();
    }

    protected override Widget BuildSemantics(Widget child) => new Semantics(
        role: SemanticsRole.MenuItemCheckbox,
        flags: CheckedWidget.Enabled ? SemanticsFlags.IsEnabled : SemanticsFlags.None,
        onTap: CheckedWidget.Enabled ? HandleTap : null,
        @checked: CheckedWidget.Checked,
        child: child);

    protected override Widget? BuildChild()
    {
        var theme = Theme.Of(Context);
        var popupTheme = PopupMenuTheme.Of(Context);
        var states = CheckedWidget.Checked ? MaterialState.Selected : MaterialState.None;
        var effectiveLabelTextStyle = CheckedWidget.LabelTextStyle?.Resolve(states)
                                      ?? popupTheme.LabelTextStyle?.Resolve(states)
                                      ?? (theme.UseMaterial3
                                          ? theme.TextTheme.LabelLarge.CopyWith(color: theme.OnSurfaceColor)
                                          : theme.TextTheme.TitleMedium);
        Widget leading = new Opacity(
            _opacity,
            new Icon(_opacity <= 0 ? null : Icons.Done));
        return new ListTile(
            enabled: CheckedWidget.Enabled,
            title: CheckedWidget.Child,
            leading: leading,
            titleTextStyle: effectiveLabelTextStyle,
            textColor: effectiveLabelTextStyle.Color,
            contentPadding: new Thickness(0));
    }

    private void HandleAnimationChanged()
    {
        if (_controller is null) return;
        SetState(() => _opacity = _controller.Value);
    }
}

public sealed class PopupMenuButton<T> : StatefulWidget
{
    public PopupMenuButton(
        PopupMenuItemBuilder<T> itemBuilder,
        T? initialValue = default,
        Action? onOpened = null,
        Action<T>? onSelected = null,
        Action? onCanceled = null,
        string? tooltip = null,
        double? elevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        Thickness? padding = null,
        Thickness? menuPadding = null,
        Widget? child = null,
        BorderRadius? borderRadius = null,
        double? splashRadius = null,
        Widget? icon = null,
        double? iconSize = null,
        Vector offset = default,
        bool enabled = true,
        ShapeBorder? shape = null,
        Color? color = null,
        Color? iconColor = null,
        bool? enableFeedback = null,
        BoxConstraints? constraints = null,
        PopupMenuPosition? position = null,
        Clip clipBehavior = Clip.None,
        bool useRootNavigator = false,
        AnimationStyle? popUpAnimationStyle = null,
        RouteSettings? routeSettings = null,
        ButtonStyle? style = null,
        bool? requestFocus = null,
        Key? key = null) : base(key)
    {
        if (child is not null && icon is not null) throw new ArgumentException("Only one of child and icon may be provided.");
        if (elevation.HasValue && (!double.IsFinite(elevation.Value) || elevation.Value < 0))
            throw new ArgumentOutOfRangeException(nameof(elevation));
        if (splashRadius.HasValue && (!double.IsFinite(splashRadius.Value) || splashRadius.Value <= 0))
            throw new ArgumentOutOfRangeException(nameof(splashRadius));
        if (iconSize.HasValue && (!double.IsFinite(iconSize.Value) || iconSize.Value < 0))
            throw new ArgumentOutOfRangeException(nameof(iconSize));
        ItemBuilder = itemBuilder ?? throw new ArgumentNullException(nameof(itemBuilder));
        InitialValue = initialValue;
        OnOpened = onOpened;
        OnSelected = onSelected;
        OnCanceled = onCanceled;
        Tooltip = tooltip;
        Elevation = elevation;
        ShadowColor = shadowColor;
        SurfaceTintColor = surfaceTintColor;
        Padding = padding ?? new Thickness(8);
        MenuPadding = menuPadding;
        Child = child;
        BorderRadius = borderRadius;
        SplashRadius = splashRadius;
        Icon = icon;
        IconSize = iconSize;
        Offset = offset;
        Enabled = enabled;
        Shape = shape;
        Color = color;
        IconColor = iconColor;
        EnableFeedback = enableFeedback;
        Constraints = constraints;
        Position = position;
        ClipBehavior = clipBehavior;
        UseRootNavigator = useRootNavigator;
        PopUpAnimationStyle = popUpAnimationStyle;
        RouteSettings = routeSettings;
        Style = style;
        RequestFocus = requestFocus;
    }

    public PopupMenuItemBuilder<T> ItemBuilder { get; }
    public T? InitialValue { get; }
    public Action? OnOpened { get; }
    public Action<T>? OnSelected { get; }
    public Action? OnCanceled { get; }
    public string? Tooltip { get; }
    public double? Elevation { get; }
    public Color? ShadowColor { get; }
    public Color? SurfaceTintColor { get; }
    public Thickness Padding { get; }
    public Thickness? MenuPadding { get; }
    public Widget? Child { get; }
    public BorderRadius? BorderRadius { get; }
    public double? SplashRadius { get; }
    public Widget? Icon { get; }
    public double? IconSize { get; }
    public Vector Offset { get; }
    public bool Enabled { get; }
    public ShapeBorder? Shape { get; }
    public Color? Color { get; }
    public Color? IconColor { get; }
    public bool? EnableFeedback { get; }
    public BoxConstraints? Constraints { get; }
    public PopupMenuPosition? Position { get; }
    public Clip ClipBehavior { get; }
    public bool UseRootNavigator { get; }
    public AnimationStyle? PopUpAnimationStyle { get; }
    public RouteSettings? RouteSettings { get; }
    public ButtonStyle? Style { get; }
    public bool? RequestFocus { get; }

    public override State CreateState() => new PopupMenuButtonState<T>();
}

public sealed class PopupMenuButtonState<T> : State
{
    private bool _isMenuExpanded;
    private RelativeRect? _lastPosition;
    private PopupMenuThemeData _popupMenuTheme = new();

    private PopupMenuButton<T> CurrentWidget => (PopupMenuButton<T>)StateWidget;

    public override Widget Build(BuildContext context)
    {
        var widget = CurrentWidget;
        _popupMenuTheme = PopupMenuTheme.Of(context);
        var localizations = MaterialLocalizations.Of(context);
        string tooltip = widget.Tooltip ?? localizations.ShowMenuTooltip;
        bool feedback = widget.EnableFeedback ?? _popupMenuTheme.EnableFeedback ?? true;
        Widget button;
        if (widget.Child is not null)
        {
            button = new InkWell(
                onTap: widget.Enabled ? ShowButtonMenu : null,
                borderRadius: widget.BorderRadius,
                enableFeedback: feedback,
                child: widget.Child);
        }
        else
        {
            var iconTheme = IconTheme.Of(context);
            var icon = widget.Icon ?? new Icon(
                Theme.Of(context).Platform is TargetPlatform.IOS or TargetPlatform.MacOS
                    ? Icons.MoreHoriz
                    : Icons.MoreVert);
            button = new IconButton(
                icon: icon,
                onPressed: widget.Enabled ? ShowButtonMenu : null,
                padding: widget.Padding,
                iconSize: widget.IconSize ?? _popupMenuTheme.IconSize ?? iconTheme.Size,
                color: widget.IconColor ?? _popupMenuTheme.IconColor ?? iconTheme.Color,
                splashRadius: widget.SplashRadius,
                enableFeedback: feedback,
                style: widget.Style);
        }

        button = new Tooltip(message: tooltip, child: button);
        return new Semantics(expanded: _isMenuExpanded, child: button);
    }

    public void ShowButtonMenu()
    {
        var widget = CurrentWidget;
        if (!widget.Enabled) return;
        var items = widget.ItemBuilder(Context);
        if (items.Count == 0) return;
        widget.OnOpened?.Invoke();
        _lastPosition = ResolvePosition(BoxConstraints.Tight(MediaQuery.Of(Context).Size));
        var task = PopupMenus.ShowMenu(
            context: Context,
            items: items,
            initialValue: widget.InitialValue,
            elevation: widget.Elevation,
            shadowColor: widget.ShadowColor,
            surfaceTintColor: widget.SurfaceTintColor,
            positionBuilder: (_, constraints) => ResolvePosition(constraints),
            shape: widget.Shape,
            menuPadding: widget.MenuPadding,
            color: widget.Color,
            useRootNavigator: widget.UseRootNavigator,
            constraints: widget.Constraints,
            clipBehavior: widget.ClipBehavior,
            routeSettings: widget.RouteSettings,
            popUpAnimationStyle: widget.PopUpAnimationStyle,
            requestFocus: widget.RequestFocus);
        SetState(() => _isMenuExpanded = true);
        _ = HandleResult(task);
    }

    private async Task HandleResult(Task<T?> task)
    {
        var value = await task;
        if (!Mounted) return;
        SetState(() => _isMenuExpanded = false);
        if (value is null) CurrentWidget.OnCanceled?.Invoke();
        else CurrentWidget.OnSelected?.Invoke(value);
    }

    private RelativeRect ResolvePosition(BoxConstraints constraints)
    {
        if (!Mounted || Context.FindRenderObject() is not RenderBox button || !button.HasSize)
        {
            return _lastPosition ?? RelativeRect.FromSize(new Rect(), constraints.Biggest);
        }

        var bounds = ResolveGlobalBounds(button);
        var position = CurrentWidget.Position ?? _popupMenuTheme.Position ?? PopupMenuPosition.Over;
        var offset = CurrentWidget.Offset;
        double yOffset = position == PopupMenuPosition.Under ? button.Size.Height : 0;
        var shifted = bounds.Translate(new Vector(offset.X, offset.Y + yOffset));
        var resolved = RelativeRect.FromSize(shifted, constraints.Biggest);
        _lastPosition = resolved;
        return resolved;
    }

    private static Rect ResolveGlobalBounds(RenderBox renderBox)
    {
        var transform = Matrix.Identity;
        RenderObject? child = renderBox;
        while (child?.Parent is not null)
        {
            var parent = child.Parent;
            var childOffset = child.parentData is BoxParentData data ? data.offset : default;
            var childTransform = Matrix.CreateTranslation(childOffset.X, childOffset.Y);
            if (parent is RenderTransform renderTransform) childTransform *= renderTransform.Transform;
            transform = childTransform * transform;
            child = parent;
        }

        var points = new[]
        {
            transform.Transform(new Point(0, 0)),
            transform.Transform(new Point(renderBox.Size.Width, 0)),
            transform.Transform(new Point(0, renderBox.Size.Height)),
            transform.Transform(new Point(renderBox.Size.Width, renderBox.Size.Height)),
        };
        double left = points.Min(p => p.X);
        double top = points.Min(p => p.Y);
        double right = points.Max(p => p.X);
        double bottom = points.Max(p => p.Y);
        return new Rect(left, top, right - left, bottom - top);
    }
}

public static class PopupMenus
{
    public static Task<T?> ShowMenu<T>(
        BuildContext context,
        IReadOnlyList<PopupMenuEntry<T>> items,
        RelativeRect? position = null,
        PopupMenuPositionBuilder? positionBuilder = null,
        T? initialValue = default,
        double? elevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        string? semanticLabel = null,
        ShapeBorder? shape = null,
        Thickness? menuPadding = null,
        Color? color = null,
        bool useRootNavigator = false,
        BoxConstraints? constraints = null,
        Clip clipBehavior = Clip.None,
        RouteSettings? routeSettings = null,
        AnimationStyle? popUpAnimationStyle = null,
        bool? requestFocus = null)
    {
        return ShowMenu(
            context,
            items.Cast<PopupMenuEntry>().ToArray(),
            position,
            positionBuilder,
            initialValue,
            elevation,
            shadowColor,
            surfaceTintColor,
            semanticLabel,
            shape,
            menuPadding,
            color,
            useRootNavigator,
            constraints,
            clipBehavior,
            routeSettings,
            popUpAnimationStyle,
            requestFocus);
    }

    public static Task<T?> ShowMenu<T>(
        BuildContext context,
        IReadOnlyList<PopupMenuEntry> items,
        RelativeRect? position = null,
        PopupMenuPositionBuilder? positionBuilder = null,
        T? initialValue = default,
        double? elevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        string? semanticLabel = null,
        ShapeBorder? shape = null,
        Thickness? menuPadding = null,
        Color? color = null,
        bool useRootNavigator = false,
        BoxConstraints? constraints = null,
        Clip clipBehavior = Clip.None,
        RouteSettings? routeSettings = null,
        AnimationStyle? popUpAnimationStyle = null,
        bool? requestFocus = null)
    {
        if (items is null || items.Count == 0) throw new ArgumentException("Popup menu items must not be empty.", nameof(items));
        if (position.HasValue == (positionBuilder is not null))
            throw new ArgumentException("Exactly one of position and positionBuilder must be provided.");
        var theme = Theme.Of(context);
        if (semanticLabel is null && theme.Platform is not (TargetPlatform.IOS or TargetPlatform.MacOS))
            semanticLabel = MaterialLocalizations.Of(context).PopupMenuLabel;
        var route = new PopupMenuRoute<T>(
            context,
            items,
            position,
            positionBuilder,
            initialValue,
            elevation,
            shadowColor,
            surfaceTintColor,
            semanticLabel,
            shape,
            menuPadding,
            color,
            constraints,
            clipBehavior,
            routeSettings,
            popUpAnimationStyle,
            requestFocus);
        Navigator.Of(context, rootNavigator: useRootNavigator).Push(route);
        return route.Completed;
    }
}

internal sealed class PopupMenuRoute<T> : PageRoute
{
    private readonly IReadOnlyList<PopupMenuEntry> _items;
    private readonly RelativeRect? _position;
    private readonly PopupMenuPositionBuilder? _positionBuilder;
    private readonly T? _initialValue;
    private readonly ThemeData _theme;
    private readonly PopupMenuThemeData _popupTheme;
    private readonly MediaQueryData _mediaQuery;
    private readonly TextDirection _direction;
    private readonly AnimationController _animation;
    private readonly TaskCompletionSource<T?> _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private object? _pendingResult;
    private bool _isExiting;
    private int _focusIndex;

    public PopupMenuRoute(
        BuildContext context,
        IReadOnlyList<PopupMenuEntry> items,
        RelativeRect? position,
        PopupMenuPositionBuilder? positionBuilder,
        T? initialValue,
        double? elevation,
        Color? shadowColor,
        Color? surfaceTintColor,
        string? semanticLabel,
        ShapeBorder? shape,
        Thickness? menuPadding,
        Color? color,
        BoxConstraints? constraints,
        Clip clipBehavior,
        RouteSettings? settings,
        AnimationStyle? animationStyle,
        bool? requestFocus) : base(settings)
    {
        _items = items;
        _position = position;
        _positionBuilder = positionBuilder;
        _initialValue = initialValue;
        Elevation = elevation;
        ShadowColor = shadowColor;
        SurfaceTintColor = surfaceTintColor;
        SemanticLabel = semanticLabel;
        Shape = shape;
        MenuPadding = menuPadding;
        Color = color;
        Constraints = constraints;
        ClipBehavior = clipBehavior;
        RequestFocus = requestFocus;
        _theme = Theme.Of(context);
        _popupTheme = PopupMenuTheme.Of(context);
        _mediaQuery = MediaQuery.Of(context);
        _direction = Directionality.Of(context);
        BarrierLabel = MaterialLocalizations.Of(context).MenuDismissLabel;
        AnimationStyle = animationStyle;
        var duration = animationStyle?.Duration ?? TimeSpan.FromMilliseconds(300);
        if (duration < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(animationStyle));
        _animation = new AnimationController(duration) { Curve = animationStyle?.Curve ?? Curves.Linear };
        _animation.Changed += HandleAnimationChanged;
        _animation.Dismissed += HandleDismissed;
        _focusIndex = ResolveInitialIndex();
    }

    public override bool Opaque => false;
    public double? Elevation { get; }
    public Color? ShadowColor { get; }
    public Color? SurfaceTintColor { get; }
    public string? SemanticLabel { get; }
    public ShapeBorder? Shape { get; }
    public Thickness? MenuPadding { get; }
    public Color? Color { get; }
    public BoxConstraints? Constraints { get; }
    public Clip ClipBehavior { get; }
    public bool? RequestFocus { get; }
    public string BarrierLabel { get; }
    public AnimationStyle? AnimationStyle { get; }
    public Task<T?> Completed => _completed.Task;

    protected override void OnAttach() => _animation.Forward(from: 0);

    public override bool WillPop(object? result)
    {
        if (_isExiting || _animation.Value <= 0) return base.WillPop(result);
        _pendingResult = result;
        _isExiting = true;
        var reverseDuration = AnimationStyle?.ReverseDuration ?? AnimationStyle?.Duration ?? TimeSpan.FromMilliseconds(300);
        _animation.Duration = reverseDuration;
        _animation.Curve = AnimationStyle?.ReverseCurve ?? DefaultReverseCurve;
        _animation.Reverse();
        return false;
    }

    public override void DidComplete(object? result)
    {
        if (result is null) _completed.TrySetResult(default);
        else if (result is T typed) _completed.TrySetResult(typed);
        else _completed.TrySetException(new InvalidCastException());
    }

    public override Widget BuildPage(BuildContext context)
    {
        var constraints = BoxConstraints.Tight(_mediaQuery.Size);
        var position = _positionBuilder?.Invoke(context, constraints)
                       ?? _position
                       ?? RelativeRect.FromSize(new Rect(), _mediaQuery.Size);
        Widget menu = new PopupMenuPanel<T>(this, _items, _initialValue, _focusIndex);
        menu = new PopupMenuTheme(_popupTheme, menu);
        menu = new Theme(_theme, menu);
        menu = new PopupMenuPositionLayout(position, _mediaQuery.Padding, menu);
        var barrier = new Semantics(
            label: BarrierLabel,
            onTap: () => Navigator?.MaybePop(),
            child: new GestureDetector(
                behavior: HitTestBehavior.Opaque,
                onTap: () => Navigator?.MaybePop(),
                child: new SizedBox()));
        Widget page = new Stack(
            fit: StackFit.Expand,
            children:
            [
                new Positioned(left: 0, top: 0, right: 0, bottom: 0, child: barrier),
                new Positioned(left: 0, top: 0, right: 0, bottom: 0, child: menu),
            ]);
        page = new Focus(
            autofocus: RequestFocus ?? true,
            onKeyEvent: HandleKeyEvent,
            child: page);
        page = MediaQuery.RemovePadding(context, page, true, true, true, true);
        page = new MediaQuery(_mediaQuery, page);
        page = new Directionality(_direction, page);
        return page;
    }

    public override void Dispose()
    {
        _animation.Changed -= HandleAnimationChanged;
        _animation.Dismissed -= HandleDismissed;
        _animation.Dispose();
        if (!_completed.Task.IsCompleted) _completed.TrySetResult(default);
        base.Dispose();
    }

    internal double Progress => Math.Clamp(_animation.Evaluate(), 0, 1);

    private int ResolveInitialIndex()
    {
        if (_initialValue is not null)
        {
            for (int i = 0; i < _items.Count; i++) if (_items[i].Represents(_initialValue)) return i;
        }
        for (int i = 0; i < _items.Count; i++) if (_items[i].IsEnabled) return i;
        return -1;
    }

    private KeyEventResult HandleKeyEvent(FocusNode node, KeyEvent @event)
    {
        if (!@event.IsDown) return KeyEventResult.Ignored;
        if (@event.Key is "Escape" or "Esc")
        {
            Navigator?.MaybePop();
            return KeyEventResult.Handled;
        }
        if (@event.Key is "ArrowDown" or "Down")
        {
            MoveFocus(1);
            return KeyEventResult.Handled;
        }
        if (@event.Key is "ArrowUp" or "Up")
        {
            MoveFocus(-1);
            return KeyEventResult.Handled;
        }
        if (@event.Key is "Enter" or "Return" or "NumPadEnter" or "NumpadEnter" or "Space" or "Spacebar")
        {
            ActivateFocused();
            return KeyEventResult.Handled;
        }
        return KeyEventResult.Ignored;
    }

    private void MoveFocus(int delta)
    {
        if (_items.Count == 0) return;
        int next = _focusIndex;
        for (int i = 0; i < _items.Count; i++)
        {
            next = (next + delta + _items.Count) % _items.Count;
            if (_items[next].IsEnabled)
            {
                _focusIndex = next;
                NotifyRouteChanged();
                return;
            }
        }
    }

    private void ActivateFocused()
    {
        if (_focusIndex < 0 || _focusIndex >= _items.Count || !_items[_focusIndex].IsEnabled) return;
        if (_items[_focusIndex] is PopupMenuItem<T> item)
        {
            Navigator?.MaybePop(item.Value);
            item.InvokeOnTap();
        }
    }

    private void HandleAnimationChanged() => NotifyRouteChanged();
    private void HandleDismissed() { if (_isExiting) Navigator?.MaybePop(_pendingResult); }

    private static double DefaultReverseCurve(double value) => Math.Clamp(value / (2.0 / 3.0), 0, 1);
}

internal sealed class PopupMenuPanel<T> : StatelessWidget
{
    private readonly PopupMenuRoute<T> _route;
    private readonly IReadOnlyList<PopupMenuEntry> _items;
    private readonly T? _initialValue;
    private readonly int _focusIndex;

    public PopupMenuPanel(PopupMenuRoute<T> route, IReadOnlyList<PopupMenuEntry> items, T? initialValue, int focusIndex)
    {
        _route = route;
        _items = items;
        _initialValue = initialValue;
        _focusIndex = focusIndex;
    }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var popupTheme = PopupMenuTheme.Of(context);
        bool useM3 = theme.UseMaterial3;
        double elevation = _route.Elevation ?? popupTheme.Elevation ?? (useM3 ? 3 : 8);
        var color = _route.Color ?? popupTheme.Color ?? (useM3 ? theme.SurfaceContainerColor : theme.CardColor);
        var surfaceTint = _route.SurfaceTintColor ?? popupTheme.SurfaceTintColor ?? Colors.Transparent;
        if (useM3 && surfaceTint.A > 0) color = NavigationSurfaceUtilities.ApplySurfaceTint(color, surfaceTint, elevation);
        var shadow = _route.ShadowColor ?? popupTheme.ShadowColor ?? theme.ShadowColor;
        var shape = _route.Shape ?? popupTheme.Shape ?? ShapeBorder.RoundedRectangle(useM3 ? 4 : 2);
        var children = new List<Widget>(_items.Count);
        double unit = 1.0 / (_items.Count + 1.5);
        for (int i = 0; i < _items.Count; i++)
        {
            Widget item = _items[i];
            if (i == _focusIndex || (_initialValue is not null && _items[i].Represents(_initialValue)))
            {
                item = new ColoredBox(ApplyOpacity(theme.OnSurfaceColor, 0.12), item);
            }
            double start = (i + 1) * unit;
            double end = Math.Min(1, start + (1.5 * unit));
            children.Add(new Opacity(Interval(_route.Progress, start, end), item));
        }

        Widget content = new SingleChildScrollView(
            padding: _route.MenuPadding ?? popupTheme.MenuPadding ?? new Thickness(0, 8),
            child: new ListBody(children: children));
        content = new Semantics(
            role: SemanticsRole.Menu,
            label: _route.SemanticLabel,
            scopesRoute: true,
            namesRoute: true,
            explicitChildNodes: true,
            child: content);
        content = new IntrinsicWidth(stepWidth: 56, child: content);
        content = new ConstrainedBox(
            _route.Constraints ?? new BoxConstraints(MinWidth: 112, MaxWidth: 280),
            content);
        content = new DecoratedBox(
            new BoxDecoration(
                Color: color,
                Border: shape.Side,
                BorderRadius: shape.BorderRadius,
                BoxShadows: BuildBoxShadows(shadow, elevation)),
            content);
        if (_route.ClipBehavior != Clip.None) content = new ClipRRect(shape.BorderRadius, content);
        var alignment = Directionality.Of(context) == TextDirection.Rtl
            ? Alignment.TopLeft
            : Alignment.TopRight;
        content = new Align(
            alignment: alignment,
            widthFactor: Interval(_route.Progress, 0, unit),
            heightFactor: Interval(_route.Progress, 0, unit * _items.Count),
            child: content);
        return new Opacity(Interval(_route.Progress, 0, 1.0 / 3.0), content);
    }

    private static BoxShadows? BuildBoxShadows(Color color, double elevation)
    {
        if (color.A == 0 || elevation <= 0) return null;
        return new BoxShadows(new BoxShadow
        {
            OffsetY = Math.Max(1, elevation * 0.5),
            Blur = Math.Max(2, elevation * 2.4),
            Color = ApplyOpacity(color, 0.20),
        });
    }

    private static Color ApplyOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Round(color.A * Math.Clamp(opacity, 0, 1)), color.R, color.G, color.B);

    private static double Interval(double value, double start, double end)
    {
        if (end <= start) return value >= end ? 1 : 0;
        return Math.Clamp((value - start) / (end - start), 0, 1);
    }
}

internal sealed class PopupMenuPositionLayout : SingleChildRenderObjectWidget
{
    public PopupMenuPositionLayout(RelativeRect position, Thickness safePadding, Widget child)
        : base(child)
    {
        Position = position;
        SafePadding = safePadding;
    }

    public RelativeRect Position { get; }
    public Thickness SafePadding { get; }

    internal override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderPopupMenuPositionLayout(Position, SafePadding, Directionality.Of(context));

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var layout = (RenderPopupMenuPositionLayout)renderObject;
        layout.Position = Position;
        layout.SafePadding = SafePadding;
        layout.TextDirection = Directionality.Of(context);
    }
}

internal sealed class RenderPopupMenuPositionLayout : RenderProxyBox
{
    private RelativeRect _position;
    private Thickness _safePadding;
    private TextDirection _textDirection;

    public RenderPopupMenuPositionLayout(RelativeRect position, Thickness safePadding, TextDirection textDirection)
    {
        _position = position;
        _safePadding = safePadding;
        _textDirection = textDirection;
    }

    public RelativeRect Position { get => _position; set { if (_position != value) { _position = value; MarkNeedsLayout(); } } }
    public Thickness SafePadding { get => _safePadding; set { if (_safePadding != value) { _safePadding = value; MarkNeedsLayout(); } } }
    public TextDirection TextDirection { get => _textDirection; set { if (_textDirection != value) { _textDirection = value; MarkNeedsLayout(); } } }

    protected override void PerformLayout()
    {
        Size = Constraints.Constrain(Constraints.Biggest);
        if (Child is null) return;
        double horizontalInset = 16 + SafePadding.Left + SafePadding.Right;
        double verticalInset = 16 + SafePadding.Top + SafePadding.Bottom;
        Child.Layout(new BoxConstraints(
            MaxWidth: Math.Max(0, Size.Width - horizontalInset),
            MaxHeight: Math.Max(0, Size.Height - verticalInset)), parentUsesSize: true);
        double x = Position.Left > Position.Right
            ? Size.Width - Position.Right - Child.Size.Width
            : Position.Left < Position.Right
                ? Position.Left
                : TextDirection == TextDirection.Rtl
                    ? Size.Width - Position.Right - Child.Size.Width
                    : Position.Left;
        double y = Position.Top;
        x = Math.Clamp(x, 8 + SafePadding.Left, Math.Max(8 + SafePadding.Left, Size.Width - Child.Size.Width - 8 - SafePadding.Right));
        y = Math.Clamp(y, 8 + SafePadding.Top, Math.Max(8 + SafePadding.Top, Size.Height - Child.Size.Height - 8 - SafePadding.Bottom));
        ((BoxParentData)Child.parentData!).offset = new Point(x, y);
    }
}
