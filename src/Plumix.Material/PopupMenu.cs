using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using RelativeRect = Plumix.Rendering.RelativeRect;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/popup_menu.dart

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
        BorderRadiusGeometry? radius = null,
        Color? color = null,
        Key? key = null) : base(key)
    {
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
    public BorderRadiusGeometry? Radius { get; }
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

}

public class PopupMenuItemState<T> : State
{
    protected PopupMenuItem<T> CurrentWidget => (PopupMenuItem<T>)StateWidget;

    protected virtual Widget? BuildChild() => CurrentWidget.Child;

    public override Widget Build(BuildContext context)
    {
        PopupMenuItem<T> widget = CurrentWidget;
        ThemeData theme = Theme.Of(context);
        PopupMenuThemeData popupTheme = PopupMenuTheme.Of(context);
        MaterialState states = widget.Enabled ? MaterialState.None : MaterialState.Disabled;
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
            if (!widget.Enabled)
            {
                style = style.CopyWith(color: theme.DisabledColor);
            }
        }

        Thickness padding = widget.Padding ?? (theme.UseMaterial3 ? new Thickness(12, 0) : new Thickness(16, 0));
        Alignment alignment = Directionality.Of(context) == TextDirection.Rtl
            ? Alignment.CenterRight
            : Alignment.CenterLeft;
        Widget item = new AnimatedDefaultTextStyle(
            style: style,
            duration: MaterialConstants.ThemeAnimationDuration,
            child: new ConstrainedBox(
                new BoxConstraints(MinHeight: widget.Height),
                new Padding(
                    padding,
                    new Align(
                        alignment: alignment,
                        widthFactor: 1,
                        child: BuildChild() ?? new SizedBox()))));
        if (!widget.Enabled)
        {
            double opacity = theme.Brightness == Brightness.Dark ? 0.5 : 0.38;
            item = IconTheme.Merge(
                new IconThemeData(Color: null, Size: null, Opacity: opacity),
                item);
        }
        item = ListTileTheme.Merge(
            child: item,
            contentPadding: EdgeInsetsGeometry.Zero,
            titleTextStyle: style);
        item = new InkWell(
            onTap: widget.Enabled ? HandleTap : null,
            canRequestFocus: widget.Enabled,
            mouseCursor: ResolveMouseCursor(widget, popupTheme),
            child: item);
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
        PopupMenuThemeData theme)
    {
        return WidgetStateMouseCursor.ResolveWith(states =>
        {
            MaterialState effectiveStates = widget.Enabled
                ? MaterialStateSet.Flags(states) & ~MaterialState.Disabled
                : MaterialStateSet.Flags(states) | MaterialState.Disabled;
            MouseCursor? widgetCursor = widget.MouseCursor is WidgetStateMouseCursor stateCursor
                ? stateCursor.Resolve(effectiveStates)
                : widget.MouseCursor;
            return widgetCursor
                   ?? theme.MouseCursor?.Resolve(effectiveStates)
                   ?? (effectiveStates.HasFlag(MaterialState.Disabled)
                       ? SystemMouseCursors.Basic
                       : SystemMouseCursors.Click);
        });
    }

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
        _controller = new AnimationController(duration: FadeDuration, vsync: this);
        _controller.SetValue(_opacity);
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
        ThemeData theme = Theme.Of(Context);
        PopupMenuThemeData popupTheme = PopupMenuTheme.Of(Context);
        MaterialState states = CheckedWidget.Checked ? MaterialState.Selected : MaterialState.None;
        TextStyle effectiveLabelTextStyle = CheckedWidget.LabelTextStyle?.Resolve(states)
                                            ?? popupTheme.LabelTextStyle?.Resolve(states)
                                            ?? (theme.UseMaterial3
                                                ? theme.TextTheme.LabelLarge.CopyWith(color: theme.OnSurfaceColor)
                                                : theme.TextTheme.TitleMedium);
        Widget leading = new Opacity(
            _opacity,
            new Icon(_opacity <= 0 ? null : Icons.Done));
        return new IgnorePointer(
            child: ListTileTheme.Merge(
                contentPadding: EdgeInsetsGeometry.Zero,
                child: new ListTile(
                    enabled: CheckedWidget.Enabled,
                    title: CheckedWidget.Child,
                    leading: leading,
                    titleTextStyle: effectiveLabelTextStyle,
                    textColor: effectiveLabelTextStyle.Color,
                    contentPadding: EdgeInsetsGeometry.Zero)));
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
        EdgeInsetsGeometry? padding = null,
        EdgeInsetsGeometry? menuPadding = null,
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
        if (child is not null && icon is not null)
        {
            throw new ArgumentException("Only one of child and icon may be provided.");
        }
        ItemBuilder = itemBuilder ?? throw new ArgumentNullException(nameof(itemBuilder));
        InitialValue = initialValue;
        OnOpened = onOpened;
        OnSelected = onSelected;
        OnCanceled = onCanceled;
        Tooltip = tooltip;
        Elevation = elevation;
        ShadowColor = shadowColor;
        SurfaceTintColor = surfaceTintColor;
        Padding = padding ?? EdgeInsetsGeometry.All(8);
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
    public EdgeInsetsGeometry Padding { get; }
    public EdgeInsetsGeometry? MenuPadding { get; }
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
    private RenderBox? _navigatorBox;

    private PopupMenuButton<T> CurrentWidget => (PopupMenuButton<T>)StateWidget;

    public override Widget Build(BuildContext context)
    {
        PopupMenuButton<T> widget = CurrentWidget;
        _popupMenuTheme = PopupMenuTheme.Of(context);
        MaterialLocalizations localizations = MaterialLocalizations.Of(context);
        string tooltip = widget.Tooltip ?? localizations.ShowMenuTooltip;
        bool feedback = widget.EnableFeedback ?? _popupMenuTheme.EnableFeedback ?? true;
        Widget button;
        if (widget.Child is not null)
        {
            bool canRequestFocus = widget.Enabled
                                   || MediaQuery.Of(context).NavigationMode == NavigationMode.Directional;
            button = new Tooltip(
                message: tooltip,
                child: new InkWell(
                    onTap: widget.Enabled ? ShowButtonMenu : null,
                    borderRadius: widget.BorderRadius,
                    radius: widget.SplashRadius,
                    canRequestFocus: canRequestFocus,
                    enableFeedback: feedback,
                    child: widget.Child));
            if (widget.Style?.TapTargetSize == MaterialTapTargetSize.Padded)
            {
                button = new ConstrainedBox(
                    new BoxConstraints(MinWidth: 48, MinHeight: 48),
                    button);
            }
            button = new Semantics(expanded: _isMenuExpanded, child: button);
        }
        else
        {
            IconThemeData iconTheme = IconTheme.Of(context);
            Widget icon = widget.Icon ?? new Icon(
                PlatformDefaults.TargetPlatform is TargetPlatform.IOS or TargetPlatform.MacOS
                    ? Icons.MoreHoriz
                    : Icons.MoreVert);
            button = new IconButton(
                icon: new Semantics(expanded: _isMenuExpanded, child: icon),
                onPressed: widget.Enabled ? ShowButtonMenu : null,
                padding: widget.Padding,
                iconSize: widget.IconSize ?? _popupMenuTheme.IconSize ?? iconTheme.Size,
                color: widget.IconColor ?? _popupMenuTheme.IconColor ?? iconTheme.Color,
                splashRadius: widget.SplashRadius,
                enableFeedback: feedback,
                tooltip: tooltip,
                style: widget.Style);
        }

        return button;
    }

    public void ShowButtonMenu()
    {
        PopupMenuButton<T> widget = CurrentWidget;
        IReadOnlyList<PopupMenuEntry> items = widget.ItemBuilder(Context);
        if (items.Count == 0) return;
        NavigatorState navigator = Navigator.Of(Context, rootNavigator: widget.UseRootNavigator);
        _navigatorBox = navigator.Context.FindRenderObject() as RenderBox;
        _popupMenuTheme = PopupMenuTheme.Of(Context);
        widget.OnOpened?.Invoke();
        Size overlaySize = _navigatorBox?.HasSize == true
            ? _navigatorBox.Size
            : MediaQuery.Of(Context).Size;
        _lastPosition = ResolvePosition(BoxConstraints.Tight(overlaySize));
        Task<T?> task = PopupMenus.ShowMenu(
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

        Rect bounds;
        try
        {
            bounds = ResolveBounds(button, _navigatorBox);
        }
        catch (InvalidOperationException)
        {
            return _lastPosition ?? RelativeRect.FromSize(new Rect(), constraints.Biggest);
        }

        PopupMenuPosition position = CurrentWidget.Position
                                     ?? _popupMenuTheme.Position
                                     ?? PopupMenuPosition.Over;
        Vector offset = CurrentWidget.Offset;
        double yOffset = position == PopupMenuPosition.Under ? button.Size.Height : 0;
        if (position == PopupMenuPosition.Under && CurrentWidget.Child is null)
        {
            Thickness padding = CurrentWidget.Padding.Resolve(Directionality.Of(Context));
            yOffset -= (padding.Top + padding.Bottom) / 2.0;
        }
        Rect shifted = bounds.Translate(new Vector(offset.X, offset.Y + yOffset));
        RelativeRect resolved = RelativeRect.FromSize(shifted, constraints.Biggest);
        _lastPosition = resolved;
        return resolved;
    }

    private static Rect ResolveBounds(RenderBox renderBox, RenderBox? ancestor)
    {
        Matrix4 transform = renderBox.GetTransformTo(ancestor);
        Point[] points =
        {
            MatrixUtils.TransformPoint(transform, new Point(0, 0)),
            MatrixUtils.TransformPoint(transform, new Point(renderBox.Size.Width, 0)),
            MatrixUtils.TransformPoint(transform, new Point(0, renderBox.Size.Height)),
            MatrixUtils.TransformPoint(transform, new Point(renderBox.Size.Width, renderBox.Size.Height)),
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
        EdgeInsetsGeometry? menuPadding = null,
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
        EdgeInsetsGeometry? menuPadding = null,
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
        if (semanticLabel is null
            && PlatformDefaults.TargetPlatform is not (TargetPlatform.IOS or TargetPlatform.MacOS))
        {
            semanticLabel = MaterialLocalizations.Of(context).PopupMenuLabel;
        }
        NavigatorState navigator = Navigator.Of(context, rootNavigator: useRootNavigator);
        CapturedThemes capturedThemes = InheritedTheme.Capture(context, navigator.Context);
        var route = new PopupMenuRoute<T>(
            context,
            capturedThemes,
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
        navigator.Push(route);
        return route.Completed;
    }
}

internal sealed class PopupMenuRoute<T> : PageRoute
{
    private readonly IReadOnlyList<PopupMenuEntry> _items;
    private readonly CapturedThemes _capturedThemes;
    private readonly RelativeRect? _position;
    private readonly PopupMenuPositionBuilder? _positionBuilder;
    private readonly T? _initialValue;
    private readonly TaskCompletionSource<T?> _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _focusIndex;

    public PopupMenuRoute(
        BuildContext context,
        CapturedThemes capturedThemes,
        IReadOnlyList<PopupMenuEntry> items,
        RelativeRect? position,
        PopupMenuPositionBuilder? positionBuilder,
        T? initialValue,
        double? elevation,
        Color? shadowColor,
        Color? surfaceTintColor,
        string? semanticLabel,
        ShapeBorder? shape,
        EdgeInsetsGeometry? menuPadding,
        Color? color,
        BoxConstraints? constraints,
        Clip clipBehavior,
        RouteSettings? settings,
        AnimationStyle? animationStyle,
        bool? requestFocus) : base(settings)
    {
        _capturedThemes = capturedThemes ?? throw new ArgumentNullException(nameof(capturedThemes));
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
        BarrierLabel = MaterialLocalizations.Of(context).MenuDismissLabel;
        AnimationStyle = animationStyle;
        _focusIndex = ResolveInitialIndex();
    }

    public override bool Opaque => false;
    public override TimeSpan TransitionDuration =>
        AnimationStyle?.Duration ?? TimeSpan.FromMilliseconds(300);
    public override TimeSpan ReverseTransitionDuration =>
        AnimationStyle?.ReverseDuration
        ?? AnimationStyle?.Duration
        ?? TimeSpan.FromMilliseconds(300);
    public double? Elevation { get; }
    public Color? ShadowColor { get; }
    public Color? SurfaceTintColor { get; }
    public string? SemanticLabel { get; }
    public ShapeBorder? Shape { get; }
    public EdgeInsetsGeometry? MenuPadding { get; }
    public Color? Color { get; }
    public BoxConstraints? Constraints { get; }
    public Clip ClipBehavior { get; }
    public bool? RequestFocus { get; }
    public override bool BarrierDismissible => true;
    public override string? BarrierLabel { get; }
    public AnimationStyle? AnimationStyle { get; }
    public Task<T?> Completed => _completed.Task;

    public override void DidComplete(object? result)
    {
        if (result is null) _completed.TrySetResult(default);
        else if (result is T typed) _completed.TrySetResult(typed);
        else _completed.TrySetException(new InvalidCastException());
    }

    public override Widget BuildPage(BuildContext context)
    {
        MediaQueryData mediaQuery = MediaQuery.Of(context);
        BoxConstraints constraints = BoxConstraints.Tight(mediaQuery.Size);
        RelativeRect position = _positionBuilder?.Invoke(context, constraints)
                                ?? _position
                                ?? RelativeRect.FromSize(new Rect(), mediaQuery.Size);
        Widget menu = new PopupMenuPanel<T>(this, _items, _initialValue);
        menu = _capturedThemes.Wrap(menu);
        menu = new PopupMenuPositionLayout(
            position,
            mediaQuery.Padding,
            mediaQuery.DisplayFeatures,
            menu);
        // The dismissal barrier is owned by ModalRoute and painted below this page.
        Widget page = new Stack(
            fit: StackFit.Expand,
            children:
            [
                new Positioned(left: 0, top: 0, right: 0, bottom: 0, child: menu),
            ]);
        page = new Focus(
            autofocus: RequestFocus ?? true,
            onKeyEvent: HandleKeyEvent,
            child: page);
        page = MediaQuery.RemovePadding(context, page, true, true, true, true);
        return page;
    }

    public override void Dispose()
    {
        if (!_completed.Task.IsCompleted) _completed.TrySetResult(default);
        base.Dispose();
    }

    internal double Progress
    {
        get
        {
            double value = Math.Clamp(Animation.Value, 0, 1);
            Curve curve = Animation.Status == AnimationStatus.Reverse
                ? AnimationStyle?.ReverseCurve ?? DefaultReverseCurve
                : AnimationStyle?.Curve ?? Curves.Linear;
            return Math.Clamp(curve(value), 0, 1);
        }
    }

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
        if (@event is not KeyDownEvent) return KeyEventResult.Ignored;
        if (@event.LogicalKey.Equals(LogicalKeyboardKey.Escape))
        {
            Navigator?.MaybePop();
            return KeyEventResult.Handled;
        }
        if (@event.LogicalKey.Equals(LogicalKeyboardKey.ArrowDown))
        {
            MoveFocus(1);
            return KeyEventResult.Handled;
        }
        if (@event.LogicalKey.Equals(LogicalKeyboardKey.ArrowUp))
        {
            MoveFocus(-1);
            return KeyEventResult.Handled;
        }
        if ((@event.LogicalKey.Equals(LogicalKeyboardKey.Enter)
    || @event.LogicalKey.Equals(LogicalKeyboardKey.NumpadEnter)
    || @event.LogicalKey.Equals(LogicalKeyboardKey.Space)))
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

    private static double DefaultReverseCurve(double value) => Math.Clamp(value / (2.0 / 3.0), 0, 1);
}

internal sealed class PopupMenuPanel<T> : StatelessWidget
{
    private readonly PopupMenuRoute<T> _route;
    private readonly IReadOnlyList<PopupMenuEntry> _items;
    private readonly T? _initialValue;

    public PopupMenuPanel(
        PopupMenuRoute<T> route,
        IReadOnlyList<PopupMenuEntry> items,
        T? initialValue)
    {
        _route = route;
        _items = items;
        _initialValue = initialValue;
    }

    public override Widget Build(BuildContext context)
    {
        ThemeData theme = Theme.Of(context);
        PopupMenuThemeData popupTheme = PopupMenuTheme.Of(context);
        bool useM3 = theme.UseMaterial3;
        double elevation = _route.Elevation ?? popupTheme.Elevation ?? (useM3 ? 3 : 8);
        Color? color = _route.Color
                       ?? popupTheme.Color
                       ?? (useM3 ? theme.SurfaceContainerColor : null);
        Color? surfaceTint = _route.SurfaceTintColor
                             ?? popupTheme.SurfaceTintColor
                             ?? (useM3 ? Colors.Transparent : null);
        Color? shadow = _route.ShadowColor
                        ?? popupTheme.ShadowColor
                        ?? (useM3 ? theme.ColorScheme.Shadow : null);
        ShapeBorder? shape = _route.Shape
                             ?? popupTheme.Shape
                             ?? (useM3 ? new RoundedRectangleBorder(borderRadius:
                                 Plumix.Rendering.BorderRadius.Circular(4)) : null);
        var children = new List<Widget>(_items.Count);
        bool selectedItemWrapped = false;
        double unit = 1.0 / (_items.Count + 1.5);
        for (int i = 0; i < _items.Count; i++)
        {
            Widget item = _items[i];
            if (_initialValue is not null && _items[i].Represents(_initialValue))
            {
                item = new ColoredBox(theme.HighlightColor, item);
                if (!selectedItemWrapped)
                {
                    item = new PopupMenuEnsureVisible(item);
                    selectedItemWrapped = true;
                }
            }
            double start = (i + 1) * unit;
            double end = Math.Min(1, start + (1.5 * unit));
            children.Add(new Opacity(Interval(_route.Progress, start, end), item));
        }

        EdgeInsetsGeometry menuPaddingGeometry = _route.MenuPadding
                                                 ?? popupTheme.MenuPadding
                                                 ?? EdgeInsetsGeometry.Symmetric(vertical: 8);
        Widget content = new SingleChildScrollView(
            padding: menuPaddingGeometry.Resolve(Directionality.Of(context)),
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
        Alignment alignment = Directionality.Of(context) == TextDirection.Rtl
            ? Alignment.TopLeft
            : Alignment.TopRight;
        content = new Align(
            alignment: alignment,
            widthFactor: Interval(_route.Progress, 0, unit),
            heightFactor: Interval(_route.Progress, 0, unit * _items.Count),
            child: content);
        content = new Material(
            type: MaterialType.Card,
            elevation: elevation,
            color: color,
            shadowColor: shadow,
            surfaceTintColor: surfaceTint,
            shape: shape,
            clipBehavior: _route.ClipBehavior,
            child: content);
        return new Opacity(Interval(_route.Progress, 0, 1.0 / 3.0), content);
    }

    private static double Interval(double value, double start, double end)
    {
        if (end <= start) return value >= end ? 1 : 0;
        return Math.Clamp((value - start) / (end - start), 0, 1);
    }
}

internal sealed class PopupMenuEnsureVisible : StatefulWidget
{
    public PopupMenuEnsureVisible(Widget child, Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public Widget Child { get; }

    public override State CreateState() => new PopupMenuEnsureVisibleState();

    private sealed class PopupMenuEnsureVisibleState : State
    {
        private int _attempts;

        private PopupMenuEnsureVisible CurrentWidget => (PopupMenuEnsureVisible)StateWidget;

        public override void InitState()
        {
            Scheduler.AddPostFrameCallback(EnsureVisible);
        }

        public override Widget Build(BuildContext context) => CurrentWidget.Child;

        private void EnsureVisible(TimeSpan timestamp)
        {
            _attempts++;
            if (!Mounted)
            {
                return;
            }

            // The menu's scrollable only exists once the route's list has been laid out, so retry a
            // couple of frames before giving up.
            if (Scrollable.MaybeOf(Context) != null)
            {
                _ = Scrollable.EnsureVisible(Context);
                return;
            }

            if (_attempts < 3)
            {
                Scheduler.AddPostFrameCallback(EnsureVisible);
            }
        }
    }
}

internal sealed class PopupMenuPositionLayout : SingleChildRenderObjectWidget
{
    public PopupMenuPositionLayout(
        RelativeRect position,
        Thickness safePadding,
        IReadOnlyList<DisplayFeature>? displayFeatures,
        Widget child)
        : base(child)
    {
        Position = position;
        SafePadding = safePadding;
        DisplayFeatures = displayFeatures ?? [];
    }

    public RelativeRect Position { get; }
    public Thickness SafePadding { get; }
    public IReadOnlyList<DisplayFeature> DisplayFeatures { get; }

    internal override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderPopupMenuPositionLayout(
            Position,
            SafePadding,
            DisplayFeatures,
            Directionality.Of(context));

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var layout = (RenderPopupMenuPositionLayout)renderObject;
        layout.Position = Position;
        layout.SafePadding = SafePadding;
        layout.DisplayFeatures = DisplayFeatures;
        layout.TextDirection = Directionality.Of(context);
    }
}

internal sealed class RenderPopupMenuPositionLayout : RenderProxyBox
{
    private RelativeRect _position;
    private Thickness _safePadding;
    private IReadOnlyList<DisplayFeature> _displayFeatures;
    private TextDirection _textDirection;

    public RenderPopupMenuPositionLayout(
        RelativeRect position,
        Thickness safePadding,
        IReadOnlyList<DisplayFeature> displayFeatures,
        TextDirection textDirection)
    {
        _position = position;
        _safePadding = safePadding;
        _displayFeatures = displayFeatures;
        _textDirection = textDirection;
    }

    public RelativeRect Position { get => _position; set { if (_position != value) { _position = value; MarkNeedsLayout(); } } }
    public Thickness SafePadding { get => _safePadding; set { if (_safePadding != value) { _safePadding = value; MarkNeedsLayout(); } } }
    public IReadOnlyList<DisplayFeature> DisplayFeatures
    {
        get => _displayFeatures;
        set
        {
            if (_displayFeatures.SequenceEqual(value)) return;
            _displayFeatures = value;
            MarkNeedsLayout();
        }
    }
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
        Rect availableRegion = ResolveAvailableRegion();
        double x = Position.Left > Position.Right
            ? Size.Width - Position.Right - Child.Size.Width
            : Position.Left < Position.Right
                ? Position.Left
                : TextDirection == TextDirection.Rtl
                    ? Size.Width - Position.Right - Child.Size.Width
                    : Position.Left;
        double y = Position.Top;
        double leftLimit = availableRegion.Left + 8 + SafePadding.Left;
        double rightLimit = availableRegion.Right - 8 - SafePadding.Right;
        double topLimit = availableRegion.Top + 8 + SafePadding.Top;
        double bottomLimit = availableRegion.Bottom - 8 - SafePadding.Bottom;
        x = Math.Clamp(x, leftLimit, Math.Max(leftLimit, rightLimit - Child.Size.Width));
        y = Math.Clamp(y, topLimit, Math.Max(topLimit, bottomLimit - Child.Size.Height));
        ((BoxParentData)Child.parentData!).offset = new Point(x, y);
    }

    private Rect ResolveAvailableRegion()
    {
        var regions = new List<Rect> { new(Size) };
        foreach (DisplayFeature feature in DisplayFeatures)
        {
            var next = new List<Rect>();
            foreach (Rect region in regions)
            {
                Rect bounds = feature.Bounds.Intersect(region);
                if (bounds.Width <= 0 || bounds.Height <= 0)
                {
                    next.Add(region);
                }
                else if (bounds.Height >= region.Height && bounds.Width < region.Width)
                {
                    next.Add(new Rect(region.Left, region.Top, bounds.Left - region.Left, region.Height));
                    next.Add(new Rect(bounds.Right, region.Top, region.Right - bounds.Right, region.Height));
                }
                else if (bounds.Width >= region.Width && bounds.Height < region.Height)
                {
                    next.Add(new Rect(region.Left, region.Top, region.Width, bounds.Top - region.Top));
                    next.Add(new Rect(region.Left, bounds.Bottom, region.Width, region.Bottom - bounds.Bottom));
                }
                else
                {
                    next.Add(region);
                }
            }
            regions = next.Where(region => region.Width > 0 && region.Height > 0).ToList();
        }

        Rect anchor = Position.ToRect(new Rect(Size));
        return regions.MinBy(region =>
        {
            double dx = region.Center.X - anchor.Center.X;
            double dy = region.Center.Y - anchor.Center.Y;
            return (dx * dx) + (dy * dy);
        });
    }
}
