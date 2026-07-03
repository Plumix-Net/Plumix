using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/segmented_button.dart

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
        Thickness? expandedInsets = null,
        ButtonStyle? style = null,
        bool showSelectedIcon = true,
        Widget? selectedIcon = null,
        Axis direction = Axis.Horizontal,
        Key? key = null) : base(key)
    {
        if (segments is null) throw new ArgumentNullException(nameof(segments));
        if (selected is null) throw new ArgumentNullException(nameof(selected));
        if (segments.Count == 0)
        {
            throw new ArgumentException("SegmentedButton requires at least one segment.", nameof(segments));
        }
        if (selected.Count == 0 && !emptySelectionAllowed)
        {
            throw new ArgumentException("Selection cannot be empty unless emptySelectionAllowed is true.", nameof(selected));
        }
        if (selected.Count > 1 && !multiSelectionEnabled)
        {
            throw new ArgumentException("Multiple selected values require multiSelectionEnabled.", nameof(selected));
        }
        var values = new HashSet<T>();
        foreach (var segment in segments)
        {
            if (!values.Add(segment.Value))
            {
                throw new ArgumentException("Segment values must be unique.", nameof(segments));
            }
        }
        if (expandedInsets is { } insets
            && (insets.Left < 0 || insets.Top < 0 || insets.Right < 0 || insets.Bottom < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(expandedInsets), "Expanded insets must be non-negative.");
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
    public Thickness? ExpandedInsets { get; }
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
        BorderRadius? shape = null,
        MouseCursor? enabledMouseCursor = null,
        MouseCursor? disabledMouseCursor = null,
        VisualDensity? visualDensity = null,
        MaterialTapTargetSize? tapTargetSize = null,
        TimeSpan? animationDuration = null,
        bool? enableFeedback = null,
        Alignment? alignment = null)
    {
        return new ButtonStyle(
            ForegroundColor: BuildStateColor(foregroundColor, disabledForegroundColor, selectedForegroundColor),
            BackgroundColor: BuildStateColor(backgroundColor, disabledBackgroundColor, selectedBackgroundColor),
            ShadowColor: shadowColor.HasValue ? MaterialStateProperty<Color?>.All(shadowColor) : null,
            SurfaceTintColor: surfaceTintColor.HasValue ? MaterialStateProperty<Color?>.All(surfaceTintColor) : null,
            OverlayColor: BuildOverlayColor(foregroundColor, selectedForegroundColor, overlayColor),
            SplashColor: BuildOverlayColor(foregroundColor, selectedForegroundColor, overlayColor),
            Elevation: elevation.HasValue ? MaterialStateProperty<double?>.All(elevation) : null,
            IconColor: iconColor.HasValue || disabledIconColor.HasValue
                ? MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Disabled) ? disabledIconColor : iconColor)
                : null,
            IconSize: iconSize.HasValue ? MaterialStateProperty<double?>.All(iconSize) : null,
            Side: side.HasValue ? MaterialStateProperty<BorderSide?>.All(side) : null,
            Padding: padding.HasValue ? MaterialStateProperty<Thickness?>.All(padding) : null,
            Shape: shape.HasValue ? MaterialStateProperty<BorderRadius?>.All(shape) : null,
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
            EnableFeedback: enableFeedback);
    }

    public override State CreateState() => new SegmentedButtonState<T>();

    private static MaterialStateProperty<Color?>? BuildStateColor(
        Color? enabled,
        Color? disabled,
        Color? selected)
    {
        if (!enabled.HasValue && !disabled.HasValue && !selected.HasValue) return null;
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
        if (!foreground.HasValue && !selectedForeground.HasValue && !overlay.HasValue) return null;
        if (overlay is { A: 0 }) return MaterialStateProperty<Color?>.All(Colors.Transparent);
        return MaterialStateProperty<Color?>.ResolveWith(states =>
        {
            var baseColor = overlay
                            ?? (states.HasFlag(MaterialState.Selected) ? selectedForeground : foreground);
            if (!baseColor.HasValue) return null;
            if (states.HasFlag(MaterialState.Pressed) || states.HasFlag(MaterialState.Focused))
            {
                return NavigationSurfaceUtilities.WithOpacity(baseColor.Value, 0.10);
            }
            if (states.HasFlag(MaterialState.Hovered))
            {
                return NavigationSurfaceUtilities.WithOpacity(baseColor.Value, 0.08);
            }
            return Colors.Transparent;
        });
    }
}

internal sealed class SegmentedButtonState<T> : State
{
    private SegmentedButton<T> CurrentWidget => (SegmentedButton<T>)StateWidget;

    public override Widget Build(BuildContext context)
    {
        var widget = CurrentWidget;
        var theme = Theme.Of(context);
        var segmentedTheme = SegmentedButtonTheme.Of(context);
        var defaults = DefaultStyle(theme);
        var selectedIcon = widget.ShowSelectedIcon
            ? widget.SelectedIcon ?? segmentedTheme.SelectedIcon ?? new Icon(Icons.Check)
            : null;
        var groupStates = widget.OnSelectionChanged is null ? MaterialState.Disabled : MaterialState.None;
        if (widget.Selected.Count > 0) groupStates |= MaterialState.Selected;
        var outerRadius = ResolveValue(
                              style => style.Shape,
                              groupStates,
                              widget.Style,
                              segmentedTheme.Style,
                              defaults)
                          ?? Plumix.Rendering.BorderRadius.Circular(20);

        var children = new List<Widget>(widget.Segments.Count);
        foreach (var segment in widget.Segments)
        {
            var selected = widget.Selected.Contains(segment.Value);
            var enabled = widget.OnSelectionChanged is not null && segment.Enabled;
            var baseStates = enabled ? MaterialState.None : MaterialState.Disabled;
            if (selected) baseStates |= MaterialState.Selected;
            var segmentStyle = ComposeStyle(
                baseStates,
                widget.Style,
                segmentedTheme.Style,
                defaults);

            Widget label = segment.Label ?? segment.Icon ?? new SizedBox();
            Widget? icon = selected && widget.ShowSelectedIcon
                ? selectedIcon
                : segment.Label is not null
                    ? segment.Icon
                    : null;
            Widget content = label;
            if (icon is not null)
            {
                var iconAlignment = widget.Style?.IconAlignment
                                    ?? segmentedTheme.Style?.IconAlignment
                                    ?? defaults.IconAlignment
                                    ?? IconAlignment.Start;
                var rowChildren = iconAlignment == IconAlignment.Start
                    ? new List<Widget> { icon, new Flexible(label) }
                    : new List<Widget> { new Flexible(label), icon };
                content = new Row(
                    mainAxisSize: MainAxisSize.Min,
                    spacing: 8,
                    children: rowChildren);
            }

            var capturedValue = segment.Value;
            Widget button = new MaterialButtonCore(
                child: content,
                onPressed: enabled ? () => HandlePressed(capturedValue) : null,
                style: segmentStyle,
                isSelected: selected,
                isSemanticButton: true,
                clipBehavior: Clip.HardEdge);
            if (segment.Tooltip is not null)
            {
                button = new Tooltip(message: segment.Tooltip, child: button);
            }
            if (!widget.MultiSelectionEnabled)
            {
                button = new Semantics(
                    child: button,
                    flags: SemanticsFlags.IsInMutuallyExclusiveGroup,
                    container: true);
            }
            button = new MergeSemantics(button);
            children.Add(button);
        }

        Widget group = new SegmentedControlLayout(
            children: children,
            direction: widget.Direction,
            textDirection: Directionality.Of(context),
            expanded: widget.ExpandedInsets.HasValue);

        group = new ClipRRect(outerRadius, group);
        if (widget.ExpandedInsets is { } insets)
        {
            group = new Padding(insets, group);
        }
        return group;
    }

    private void HandlePressed(T segmentValue)
    {
        var widget = CurrentWidget;
        if (widget.OnSelectionChanged is null) return;
        var onlySelected = widget.Selected.Count == 1 && widget.Selected.Contains(segmentValue);
        if (!widget.EmptySelectionAllowed && onlySelected) return;

        HashSet<T> updated;
        var toggle = widget.MultiSelectionEnabled || (widget.EmptySelectionAllowed && onlySelected);
        if (toggle)
        {
            updated = new HashSet<T>(widget.Selected);
            if (!updated.Add(segmentValue)) updated.Remove(segmentValue);
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

    private static ButtonStyle DefaultStyle(ThemeData theme)
    {
        return new ButtonStyle(
            ForegroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Disabled)
                    ? NavigationSurfaceUtilities.WithOpacity(theme.OnSurfaceColor, 0.38)
                    : states.HasFlag(MaterialState.Selected)
                        ? theme.OnSecondaryContainerColor
                        : theme.OnSurfaceColor),
            BackgroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                !states.HasFlag(MaterialState.Disabled) && states.HasFlag(MaterialState.Selected)
                    ? theme.SecondaryContainerColor
                    : Colors.Transparent),
            ShadowColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
            SurfaceTintColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
            OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
            {
                var color = states.HasFlag(MaterialState.Selected)
                    ? theme.OnSecondaryContainerColor
                    : theme.OnSurfaceColor;
                if (states.HasFlag(MaterialState.Pressed) || states.HasFlag(MaterialState.Focused))
                {
                    return NavigationSurfaceUtilities.WithOpacity(color, 0.10);
                }
                if (states.HasFlag(MaterialState.Hovered))
                {
                    return NavigationSurfaceUtilities.WithOpacity(color, 0.08);
                }
                return null;
            }),
            SplashColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                NavigationSurfaceUtilities.WithOpacity(
                    states.HasFlag(MaterialState.Selected)
                        ? theme.OnSecondaryContainerColor
                        : theme.OnSurfaceColor,
                    0.10)),
            Elevation: MaterialStateProperty<double?>.All(0),
            IconColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Disabled)
                    ? NavigationSurfaceUtilities.WithOpacity(theme.OnSurfaceColor, 0.38)
                    : states.HasFlag(MaterialState.Selected)
                        ? theme.OnSecondaryContainerColor
                        : theme.OnSurfaceColor),
            IconSize: MaterialStateProperty<double?>.All(18),
            Side: MaterialStateProperty<BorderSide?>.ResolveWith(states =>
                new BorderSide(
                    states.HasFlag(MaterialState.Disabled)
                        ? NavigationSurfaceUtilities.WithOpacity(theme.OnSurfaceColor, 0.12)
                        : theme.OutlineColor)),
            Padding: MaterialStateProperty<Thickness?>.All(new Thickness(12, 8)),
            Shape: MaterialStateProperty<BorderRadius?>.All(Plumix.Rendering.BorderRadius.Circular(20)),
            MinimumSize: MaterialStateProperty<Size?>.All(new Size(0, 40)),
            Alignment: Alignment.Center,
            TapTargetSize: theme.MaterialTapTargetSize,
            TextStyle: MaterialStateProperty<TextStyle?>.All(theme.TextTheme.LabelLarge),
            MouseCursor: MaterialStateProperty<MouseCursor?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Disabled)
                    ? SystemMouseCursors.Basic
                    : SystemMouseCursors.Click),
            VisualDensity: theme.VisualDensity,
            AnimationDuration: TimeSpan.FromMilliseconds(200),
            EnableFeedback: true);
    }

    private static ButtonStyle ComposeStyle(
        MaterialState baseStates,
        ButtonStyle? widget,
        ButtonStyle? localTheme,
        ButtonStyle defaults)
    {
        return new ButtonStyle(
            ForegroundColor: Compose(style => style.ForegroundColor, baseStates, widget, localTheme, defaults),
            BackgroundColor: Compose(style => style.BackgroundColor, baseStates, widget, localTheme, defaults),
            ShadowColor: Compose(style => style.ShadowColor, baseStates, widget, localTheme, defaults),
            SurfaceTintColor: Compose(style => style.SurfaceTintColor, baseStates, widget, localTheme, defaults),
            OverlayColor: Compose(style => style.OverlayColor, baseStates, widget, localTheme, defaults),
            SplashColor: Compose(style => style.SplashColor, baseStates, widget, localTheme, defaults),
            Elevation: Compose(style => style.Elevation, baseStates, widget, localTheme, defaults),
            IconColor: Compose(style => style.IconColor, baseStates, widget, localTheme, defaults),
            IconSize: Compose(style => style.IconSize, baseStates, widget, localTheme, defaults),
            Side: Compose(style => style.Side, baseStates, widget, localTheme, defaults),
            Padding: Compose(style => style.Padding, baseStates, widget, localTheme, defaults),
            Shape: MaterialStateProperty<BorderRadius?>.All(Plumix.Rendering.BorderRadius.Zero),
            MinimumSize: Compose(style => style.MinimumSize, baseStates, widget, localTheme, defaults),
            FixedSize: Compose(style => style.FixedSize, baseStates, widget, localTheme, defaults),
            MaximumSize: Compose(style => style.MaximumSize, baseStates, widget, localTheme, defaults),
            Alignment: widget?.Alignment ?? localTheme?.Alignment ?? defaults.Alignment,
            IconAlignment: widget?.IconAlignment ?? localTheme?.IconAlignment ?? defaults.IconAlignment,
            TapTargetSize: widget?.TapTargetSize ?? localTheme?.TapTargetSize ?? defaults.TapTargetSize,
            TextStyle: Compose(style => style.TextStyle, baseStates, widget, localTheme, defaults),
            MouseCursor: Compose(style => style.MouseCursor, baseStates, widget, localTheme, defaults),
            VisualDensity: widget?.VisualDensity ?? localTheme?.VisualDensity ?? defaults.VisualDensity,
            AnimationDuration: widget?.AnimationDuration ?? localTheme?.AnimationDuration ?? defaults.AnimationDuration,
            EnableFeedback: widget?.EnableFeedback ?? localTheme?.EnableFeedback ?? defaults.EnableFeedback);
    }

    private static MaterialStateProperty<TValue?> Compose<TValue>(
        Func<ButtonStyle, MaterialStateProperty<TValue?>?> selector,
        MaterialState baseStates,
        params ButtonStyle?[] layers) where TValue : struct
    {
        return MaterialStateProperty<TValue?>.ResolveWith(runtimeStates =>
            ResolveValue(selector, runtimeStates | baseStates, layers));
    }

    private static MaterialStateProperty<TValue?> Compose<TValue>(
        Func<ButtonStyle, MaterialStateProperty<TValue?>?> selector,
        MaterialState baseStates,
        ButtonStyle? widget,
        ButtonStyle? localTheme,
        ButtonStyle defaults) where TValue : class
    {
        return MaterialStateProperty<TValue?>.ResolveWith(runtimeStates =>
            ResolveValue(selector, runtimeStates | baseStates, widget, localTheme, defaults));
    }

    private static TValue? ResolveValue<TValue>(
        Func<ButtonStyle, MaterialStateProperty<TValue?>?> selector,
        MaterialState states,
        params ButtonStyle?[] layers) where TValue : struct
    {
        foreach (var layer in layers)
        {
            var value = layer is null ? null : selector(layer)?.Resolve(states);
            if (value.HasValue) return value;
        }
        return null;
    }

    private static TValue? ResolveValue<TValue>(
        Func<ButtonStyle, MaterialStateProperty<TValue?>?> selector,
        MaterialState states,
        params ButtonStyle?[] layers) where TValue : class
    {
        foreach (var layer in layers)
        {
            var value = layer is null ? null : selector(layer)?.Resolve(states);
            if (value is not null) return value;
        }
        return null;
    }
}
