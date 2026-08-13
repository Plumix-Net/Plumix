using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/segmented_button.dart

public sealed class ButtonSegment<T>
{
    public ButtonSegment(
        T value,
        Widget? icon = null,
        Widget? label = null,
        string? tooltip = null,
        bool enabled = true)
    {
        if (icon is null && label is null)
        {
            throw new ArgumentException("A button segment requires an icon or label.");
        }

        Value = value;
        Icon = icon;
        Label = label;
        Tooltip = tooltip;
        Enabled = enabled;
    }

    public T Value { get; }
    public Widget? Icon { get; }
    public Widget? Label { get; }
    public string? Tooltip { get; }
    public bool Enabled { get; }
}

public sealed class SegmentedButton<T> : StatefulWidget
{
    public SegmentedButton(
        IReadOnlyList<ButtonSegment<T>> segments,
        IReadOnlySet<T> selected,
        Action<IReadOnlySet<T>>? onSelectionChanged = null,
        bool multiSelectionEnabled = false,
        bool emptySelectionAllowed = false,
        EdgeInsets? expandedInsets = null,
        ButtonStyle? style = null,
        bool showSelectedIcon = true,
        Widget? selectedIcon = null,
        Axis direction = Axis.Horizontal,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(segments);
        ArgumentNullException.ThrowIfNull(selected);
        if (segments.Count == 0)
        {
            throw new ArgumentException("SegmentedButton requires at least one segment.", nameof(segments));
        }
        if (selected.Count == 0 && !emptySelectionAllowed)
        {
            throw new ArgumentException(
                "Selection cannot be empty unless emptySelectionAllowed is true.",
                nameof(selected));
        }
        if (selected.Count > 1 && !multiSelectionEnabled)
        {
            throw new ArgumentException("Multiple selected values require multiSelectionEnabled.", nameof(selected));
        }

        Segments = segments;
        Selected = selected;
        OnSelectionChanged = onSelectionChanged;
        MultiSelectionEnabled = multiSelectionEnabled;
        EmptySelectionAllowed = emptySelectionAllowed;
        ExpandedInsets = expandedInsets;
        Style = style;
        ShowSelectedIcon = showSelectedIcon;
        SelectedIcon = selectedIcon;
        Direction = direction;
    }

    public IReadOnlyList<ButtonSegment<T>> Segments { get; }
    public IReadOnlySet<T> Selected { get; }
    public Action<IReadOnlySet<T>>? OnSelectionChanged { get; }
    public bool MultiSelectionEnabled { get; }
    public bool EmptySelectionAllowed { get; }
    public EdgeInsets? ExpandedInsets { get; }
    public ButtonStyle? Style { get; }
    public bool ShowSelectedIcon { get; }
    public Widget? SelectedIcon { get; }
    public Axis Direction { get; }

    public static ButtonStyle StyleFrom(
        Color? foregroundColor = null,
        Color? backgroundColor = null,
        Color? selectedForegroundColor = null,
        Color? selectedBackgroundColor = null,
        Color? disabledForegroundColor = null,
        Color? disabledBackgroundColor = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        Color? iconColor = null,
        double? iconSize = null,
        Color? disabledIconColor = null,
        Color? overlayColor = null,
        double? elevation = null,
        TextStyle? textStyle = null,
        Thickness? padding = null,
        Size? minimumSize = null,
        Size? fixedSize = null,
        Size? maximumSize = null,
        BorderSide? side = null,
        OutlinedBorder? shape = null,
        MouseCursor? enabledMouseCursor = null,
        MouseCursor? disabledMouseCursor = null,
        VisualDensity? visualDensity = null,
        MaterialTapTargetSize? tapTargetSize = null,
        TimeSpan? animationDuration = null,
        bool? enableFeedback = null,
        AlignmentGeometry? alignment = null,
        InteractiveInkFeatureFactory? splashFactory = null)
    {
        return new ButtonStyle(
            ForegroundColor: BuildStateColor(
                foregroundColor,
                disabledForegroundColor,
                selectedForegroundColor),
            BackgroundColor: BuildStateColor(
                backgroundColor,
                disabledBackgroundColor,
                selectedBackgroundColor),
            ShadowColor: shadowColor.HasValue ? MaterialStateProperty<Color?>.All(shadowColor) : null,
            SurfaceTintColor: surfaceTintColor.HasValue
                ? MaterialStateProperty<Color?>.All(surfaceTintColor)
                : null,
            OverlayColor: BuildOverlayColor(foregroundColor, selectedForegroundColor, overlayColor),
            Elevation: elevation.HasValue ? MaterialStateProperty<double?>.All(elevation) : null,
            IconColor: iconColor.HasValue || disabledIconColor.HasValue
                ? MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Disabled) ? disabledIconColor : iconColor)
                : null,
            IconSize: iconSize.HasValue ? MaterialStateProperty<double?>.All(iconSize) : null,
            Side: side.HasValue ? MaterialStateProperty<BorderSide?>.All(side) : null,
            Padding: padding.HasValue ? MaterialStateProperty<Thickness?>.All(padding) : null,
            Shape: shape is not null ? MaterialStateProperty<OutlinedBorder?>.All(shape) : null,
            MinimumSize: minimumSize.HasValue ? MaterialStateProperty<Size?>.All(minimumSize) : null,
            FixedSize: fixedSize.HasValue ? MaterialStateProperty<Size?>.All(fixedSize) : null,
            MaximumSize: maximumSize.HasValue ? MaterialStateProperty<Size?>.All(maximumSize) : null,
            Alignment: alignment,
            TapTargetSize: tapTargetSize,
            TextStyle: textStyle is not null ? MaterialStateProperty<TextStyle?>.All(textStyle) : null,
            MouseCursor: enabledMouseCursor is not null || disabledMouseCursor is not null
                ? MaterialStateProperty<MouseCursor?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Disabled) ? disabledMouseCursor : enabledMouseCursor)
                : null,
            VisualDensity: visualDensity,
            AnimationDuration: animationDuration,
            EnableFeedback: enableFeedback,
            SplashFactory: splashFactory);
    }

    public override State CreateState() => new SegmentedButtonState<T>();

    private static MaterialStateProperty<Color?>? BuildStateColor(
        Color? enabled,
        Color? disabled,
        Color? selected)
    {
        if (!enabled.HasValue && !disabled.HasValue && !selected.HasValue)
        {
            return null;
        }

        return MaterialStateProperty<Color?>.ResolveWith(states =>
            states.HasFlag(MaterialState.Disabled)
                ? disabled
                : states.HasFlag(MaterialState.Selected)
                    ? selected
                    : enabled);
    }

    private static MaterialStateProperty<Color?>? BuildOverlayColor(
        Color? foreground,
        Color? selectedForeground,
        Color? overlay)
    {
        if (!foreground.HasValue && !selectedForeground.HasValue && !overlay.HasValue)
        {
            return null;
        }
        if (overlay is { A: 0 })
        {
            return MaterialStateProperty<Color?>.All(Colors.Transparent);
        }

        return MaterialStateProperty<Color?>.ResolveWith(states =>
        {
            Color? stateColor = overlay
                                ?? (states.HasFlag(MaterialState.Selected) ? selectedForeground : foreground);
            if (!stateColor.HasValue)
            {
                return null;
            }
            if (states.HasFlag(MaterialState.Pressed) || states.HasFlag(MaterialState.Focused))
            {
                return NavigationSurfaceUtilities.WithOpacity(stateColor.Value, 0.10);
            }
            if (states.HasFlag(MaterialState.Hovered))
            {
                return NavigationSurfaceUtilities.WithOpacity(stateColor.Value, 0.08);
            }

            return Colors.Transparent;
        });
    }
}

public sealed class SegmentedButtonState<T> : State
{
    private bool _hovering;
    private bool _focused;

    private SegmentedButton<T> CurrentWidget => (SegmentedButton<T>)StateWidget;

    public Dictionary<ButtonSegment<T>, MaterialStatesController> StatesControllers { get; } = [];

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var retainedSegments = CurrentWidget.Segments.ToHashSet();
        foreach (ButtonSegment<T> segment in StatesControllers.Keys.ToArray())
        {
            if (retainedSegments.Contains(segment))
            {
                continue;
            }

            MaterialStatesController controller = StatesControllers[segment];
            StatesControllers.Remove(segment);
            controller.Dispose();
        }
    }

    public override Widget Build(BuildContext context)
    {
        SegmentedButton<T> widget = CurrentWidget;
        ThemeData theme = Theme.Of(context);
        SegmentedButtonThemeData segmentedTheme = SegmentedButtonTheme.Of(context);
        ButtonStyle defaults = DefaultStyle(theme);
        ButtonStyle segmentThemeStyle = SegmentStyleFor(segmentedTheme.Style)
            .Merge(SegmentStyleFor(defaults));
        ButtonStyle widgetSegmentStyle = SegmentStyleFor(widget.Style);
        Widget? selectedIcon = widget.ShowSelectedIcon
            ? widget.SelectedIcon ?? segmentedTheme.SelectedIcon ?? new Icon(Icons.Check)
            : null;
        MaterialState groupStates = ResolveGroupStates(widget);
        OutlinedBorder enabledShape = ResolveValue(
            static style => style.Shape,
            groupStates,
            widget.Style,
            segmentedTheme.Style,
            defaults) ?? new RoundedRectangleBorder();
        BorderSide enabledSide = ResolveValue(
            static style => style.Side,
            groupStates,
            widget.Style,
            segmentedTheme.Style,
            defaults) ?? BorderSide.None;
        OutlinedBorder disabledShape = ResolveValue(
            static style => style.Shape,
            MaterialState.Disabled,
            widget.Style,
            segmentedTheme.Style,
            defaults) ?? new RoundedRectangleBorder();
        BorderSide disabledSide = ResolveValue(
            static style => style.Side,
            MaterialState.Disabled,
            widget.Style,
            segmentedTheme.Style,
            defaults) ?? BorderSide.None;
        OutlinedBorder enabledBorder = enabledShape.CopyWith(enabledSide);
        OutlinedBorder disabledBorder = disabledShape.CopyWith(disabledSide);

        var children = new List<Widget>(widget.Segments.Count);
        var segmentEnabled = new List<bool>(widget.Segments.Count);
        foreach (ButtonSegment<T> segment in widget.Segments)
        {
            bool selected = widget.Selected.Contains(segment.Value);
            bool enabled = widget.OnSelectionChanged is not null && segment.Enabled;
            MaterialStatesController controller = StatesControllers.GetValueOrDefault(segment)
                ?? AddStatesController(segment);
            controller.Update(MaterialState.Selected, selected);
            Widget label = segment.Label ?? segment.Icon ?? new SizedBox();
            Widget? icon = selected && widget.ShowSelectedIcon
                ? selectedIcon
                : segment.Label is not null
                    ? segment.Icon
                    : null;
            T capturedValue = segment.Value;
            Widget button = TextButton.Segment(
                label: label,
                icon: icon,
                onPressed: enabled ? () => HandlePressed(capturedValue) : null,
                style: widgetSegmentStyle,
                onHover: HandleHover,
                onFocusChange: HandleFocus,
                statesController: controller,
                isSelected: selected);
            if (segment.Tooltip is not null)
            {
                button = new Tooltip(message: segment.Tooltip, child: button);
            }

            button = new Semantics(
                child: button,
                selected: selected,
                enabled: enabled,
                focusable: enabled,
                onTap: enabled ? () => HandlePressed(capturedValue) : null,
                flags: widget.MultiSelectionEnabled
                    ? SemanticsFlags.None
                    : SemanticsFlags.IsInMutuallyExclusiveGroup);
            children.Add(new MergeSemantics(button));
            segmentEnabled.Add(enabled);
        }

        double tapTargetVerticalPadding = ResolveTapTargetVerticalPadding(
            theme,
            segmentedTheme.Style,
            defaults,
            groupStates);
        Widget group = new SegmentedButtonRenderWidget(
            children: children,
            segmentEnabled: segmentEnabled,
            enabledBorder: enabledBorder,
            disabledBorder: disabledBorder,
            direction: widget.Direction,
            textDirection: Directionality.Of(context),
            expanded: widget.ExpandedInsets.HasValue,
            tapTargetVerticalPadding: tapTargetVerticalPadding);
        group = new Padding(widget.ExpandedInsets ?? EdgeInsets.Zero, group);
        group = new TextButtonTheme(new TextButtonThemeData(segmentThemeStyle), group);

        double elevation = ResolveValue(
            static style => style.Elevation,
            groupStates,
            widget.Style,
            segmentedTheme.Style,
            defaults) ?? 0.0;
        Color? shadowColor = ResolveValue(
            static style => style.ShadowColor,
            groupStates,
            widget.Style,
            segmentedTheme.Style,
            defaults);
        Color? surfaceTintColor = ResolveValue(
            static style => style.SurfaceTintColor,
            groupStates,
            widget.Style,
            segmentedTheme.Style,
            defaults);
        TimeSpan duration = widget.Style?.AnimationDuration
                            ?? segmentedTheme.Style?.AnimationDuration
                            ?? defaults.AnimationDuration
                            ?? MaterialConstants.ThemeAnimationDuration;
        return new Material(
            type: MaterialType.Transparency,
            elevation: elevation,
            shadowColor: shadowColor,
            surfaceTintColor: surfaceTintColor,
            animationDuration: duration,
            child: group);
    }

    public override void Dispose()
    {
        foreach (MaterialStatesController controller in StatesControllers.Values)
        {
            controller.Dispose();
        }
        StatesControllers.Clear();
        base.Dispose();
    }

    private MaterialStatesController AddStatesController(ButtonSegment<T> segment)
    {
        var controller = new MaterialStatesController();
        StatesControllers.Add(segment, controller);
        return controller;
    }

    private MaterialState ResolveGroupStates(SegmentedButton<T> widget)
    {
        MaterialState states = widget.OnSelectionChanged is null ? MaterialState.Disabled : MaterialState.None;
        if (_hovering)
        {
            states |= MaterialState.Hovered;
        }
        if (_focused)
        {
            states |= MaterialState.Focused;
        }
        if (widget.Selected.Count > 0)
        {
            states |= MaterialState.Selected;
        }
        return states;
    }

    private void HandleHover(bool value)
    {
        if (_hovering == value)
        {
            return;
        }
        SetState(() => _hovering = value);
    }

    private void HandleFocus(bool value)
    {
        if (_focused == value)
        {
            return;
        }
        SetState(() => _focused = value);
    }

    private void HandlePressed(T segmentValue)
    {
        SegmentedButton<T> widget = CurrentWidget;
        if (widget.OnSelectionChanged is null)
        {
            return;
        }

        bool onlySelected = widget.Selected.Count == 1 && widget.Selected.Contains(segmentValue);
        if (!widget.EmptySelectionAllowed && onlySelected)
        {
            return;
        }

        bool toggle = widget.MultiSelectionEnabled || (widget.EmptySelectionAllowed && onlySelected);
        HashSet<T> updated;
        if (toggle)
        {
            updated = new HashSet<T>(widget.Selected);
            if (!updated.Add(segmentValue))
            {
                updated.Remove(segmentValue);
            }
        }
        else
        {
            updated = [segmentValue];
        }

        if (!updated.SetEquals(widget.Selected))
        {
            widget.OnSelectionChanged(updated);
        }
    }

    private double ResolveTapTargetVerticalPadding(
        ThemeData theme,
        ButtonStyle? segmentedThemeStyle,
        ButtonStyle defaults,
        MaterialState states)
    {
        SegmentedButton<T> widget = CurrentWidget;
        VisualDensity density = widget.Style?.VisualDensity
                                ?? segmentedThemeStyle?.VisualDensity
                                ?? theme.VisualDensity;
        Vector densityAdjustment = density.BaseSizeAdjustment;
        Thickness padding = ResolveValue(
            static style => style.Padding,
            states,
            widget.Style,
            segmentedThemeStyle,
            defaults) ?? default;
        TextStyle? textStyle = ResolveValue(
            static style => style.TextStyle,
            states,
            widget.Style,
            segmentedThemeStyle,
            defaults);
        double fontSize = textStyle?.FontSize ?? 20.0;
        double adjustedMinimumHeight = 40.0 + densityAdjustment.Y;
        double effectiveVerticalPadding = padding.Top + padding.Bottom + (densityAdjustment.Y * 2.0);
        double buttonHeight = Math.Max(fontSize + effectiveVerticalPadding, adjustedMinimumHeight);
        MaterialTapTargetSize tapTargetSize = widget.Style?.TapTargetSize
                                               ?? segmentedThemeStyle?.TapTargetSize
                                               ?? theme.MaterialTapTargetSize;
        return tapTargetSize == MaterialTapTargetSize.ShrinkWrap
            ? 0.0
            : Math.Max(0.0, 48.0 + densityAdjustment.Y - buttonHeight);
    }

    private static ButtonStyle DefaultStyle(ThemeData theme)
    {
        ColorScheme colors = theme.ColorScheme;
        return new ButtonStyle(
            TextStyle: MaterialStateProperty<TextStyle?>.All(theme.TextTheme.LabelLarge),
            BackgroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Disabled)
                    ? null
                    : states.HasFlag(MaterialState.Selected)
                        ? colors.SecondaryContainer
                        : null),
            ForegroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Disabled)
                    ? NavigationSurfaceUtilities.WithOpacity(colors.OnSurface, 0.38)
                    : states.HasFlag(MaterialState.Selected)
                        ? colors.OnSecondaryContainer
                        : colors.OnSurface),
            OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
            {
                Color stateColor = states.HasFlag(MaterialState.Selected)
                    ? colors.OnSecondaryContainer
                    : colors.OnSurface;
                if (states.HasFlag(MaterialState.Pressed) || states.HasFlag(MaterialState.Focused))
                {
                    return NavigationSurfaceUtilities.WithOpacity(stateColor, 0.10);
                }
                if (states.HasFlag(MaterialState.Hovered))
                {
                    return NavigationSurfaceUtilities.WithOpacity(stateColor, 0.08);
                }
                return null;
            }),
            SurfaceTintColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
            Elevation: MaterialStateProperty<double?>.All(0.0),
            IconSize: MaterialStateProperty<double?>.All(18.0),
            Side: MaterialStateProperty<BorderSide?>.ResolveWith(states =>
                new BorderSide(
                    states.HasFlag(MaterialState.Disabled)
                        ? NavigationSurfaceUtilities.WithOpacity(colors.OnSurface, 0.12)
                        : colors.Outline)),
            Shape: MaterialStateProperty<OutlinedBorder?>.All(new StadiumBorder()),
            MinimumSize: MaterialStateProperty<Size?>.All(new Size(0.0, 40.0)));
    }

    private static ButtonStyle SegmentStyleFor(ButtonStyle? style)
    {
        return new ButtonStyle(
            TextStyle: style?.TextStyle,
            BackgroundColor: style?.BackgroundColor,
            ForegroundColor: style?.ForegroundColor,
            OverlayColor: style?.OverlayColor,
            SurfaceTintColor: style?.SurfaceTintColor,
            Elevation: style?.Elevation,
            Padding: style?.Padding,
            IconColor: style?.IconColor,
            IconSize: style?.IconSize,
            MouseCursor: style?.MouseCursor,
            VisualDensity: style?.VisualDensity,
            TapTargetSize: style?.TapTargetSize,
            AnimationDuration: style?.AnimationDuration,
            EnableFeedback: style?.EnableFeedback,
            Alignment: style?.Alignment,
            SplashFactory: style?.SplashFactory,
            Shape: MaterialStateProperty<OutlinedBorder?>.All(new RoundedRectangleBorder()));
    }

    private static TValue? ResolveValue<TValue>(
        Func<ButtonStyle, MaterialStateProperty<TValue?>?> selector,
        MaterialState states,
        params ButtonStyle?[] styles) where TValue : struct
    {
        foreach (ButtonStyle? style in styles)
        {
            TValue? value = style is null ? null : selector(style)?.Resolve(states);
            if (value.HasValue)
            {
                return value;
            }
        }
        return null;
    }

    private static TValue? ResolveValue<TValue>(
        Func<ButtonStyle, MaterialStateProperty<TValue?>?> selector,
        MaterialState states,
        params ButtonStyle?[] styles) where TValue : class
    {
        foreach (ButtonStyle? style in styles)
        {
            TValue? value = style is null ? null : selector(style)?.Resolve(states);
            if (value is not null)
            {
                return value;
            }
        }
        return null;
    }
}
