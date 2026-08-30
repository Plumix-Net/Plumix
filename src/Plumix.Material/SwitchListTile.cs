using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/switch_list_tile.dart

internal enum SwitchListTileType
{
    Material,
    Adaptive
}

/// <summary>
/// A <see cref="ListTile"/> with a <see cref="Switch"/>. In other words, a switch with a label.
/// </summary>
public sealed class SwitchListTile : StatelessWidget
{
    /// <summary>Creates a combination of a list tile and a switch.</summary>
    public SwitchListTile(
        bool value,
        Action<bool>? onChanged,
        Color? activeColor = null,
        Color? activeThumbColor = null,
        Color? activeTrackColor = null,
        Color? inactiveThumbColor = null,
        Color? inactiveTrackColor = null,
        ImageProvider? activeThumbImage = null,
        ImageErrorListener? onActiveThumbImageError = null,
        ImageProvider? inactiveThumbImage = null,
        ImageErrorListener? onInactiveThumbImageError = null,
        MaterialStateProperty<Color?>? thumbColor = null,
        MaterialStateProperty<Color?>? trackColor = null,
        MaterialStateProperty<Color?>? trackOutlineColor = null,
        MaterialStateProperty<Icon?>? thumbIcon = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        MouseCursor? mouseCursor = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        double? splashRadius = null,
        FocusNode? focusNode = null,
        MaterialStatesController? statesController = null,
        Action<bool>? onFocusChange = null,
        bool autofocus = false,
        Color? tileColor = null,
        Widget? title = null,
        Widget? subtitle = null,
        bool? isThreeLine = null,
        bool? dense = null,
        EdgeInsetsGeometry? contentPadding = null,
        Widget? secondary = null,
        bool selected = false,
        ListTileControlAffinity? controlAffinity = null,
        ShapeBorder? shape = null,
        Color? selectedTileColor = null,
        VisualDensity? visualDensity = null,
        bool? enableFeedback = null,
        double? horizontalTitleGap = null,
        double? minVerticalPadding = null,
        double? minLeadingWidth = null,
        double? minTileHeight = null,
        Color? hoverColor = null,
        bool internalAddSemanticForOnTap = false,
        Key? key = null) : this(
            switchListTileType: SwitchListTileType.Material,
            applyCupertinoTheme: false,
            value: value,
            onChanged: onChanged,
            activeColor: activeColor,
            activeThumbColor: activeThumbColor,
            activeTrackColor: activeTrackColor,
            inactiveThumbColor: inactiveThumbColor,
            inactiveTrackColor: inactiveTrackColor,
            activeThumbImage: activeThumbImage,
            onActiveThumbImageError: onActiveThumbImageError,
            inactiveThumbImage: inactiveThumbImage,
            onInactiveThumbImageError: onInactiveThumbImageError,
            thumbColor: thumbColor,
            trackColor: trackColor,
            trackOutlineColor: trackOutlineColor,
            thumbIcon: thumbIcon,
            materialTapTargetSize: materialTapTargetSize,
            dragStartBehavior: dragStartBehavior,
            mouseCursor: mouseCursor,
            overlayColor: overlayColor,
            splashRadius: splashRadius,
            focusNode: focusNode,
            statesController: statesController,
            onFocusChange: onFocusChange,
            autofocus: autofocus,
            tileColor: tileColor,
            title: title,
            subtitle: subtitle,
            isThreeLine: isThreeLine,
            dense: dense,
            contentPadding: contentPadding,
            secondary: secondary,
            selected: selected,
            controlAffinity: controlAffinity,
            shape: shape,
            selectedTileColor: selectedTileColor,
            visualDensity: visualDensity,
            enableFeedback: enableFeedback,
            horizontalTitleGap: horizontalTitleGap,
            minVerticalPadding: minVerticalPadding,
            minLeadingWidth: minLeadingWidth,
            minTileHeight: minTileHeight,
            hoverColor: hoverColor,
            internalAddSemanticForOnTap: internalAddSemanticForOnTap,
            key: key)
    {
    }

    private SwitchListTile(
        SwitchListTileType switchListTileType,
        bool? applyCupertinoTheme,
        bool value,
        Action<bool>? onChanged,
        Color? activeColor,
        Color? activeThumbColor,
        Color? activeTrackColor,
        Color? inactiveThumbColor,
        Color? inactiveTrackColor,
        ImageProvider? activeThumbImage,
        ImageErrorListener? onActiveThumbImageError,
        ImageProvider? inactiveThumbImage,
        ImageErrorListener? onInactiveThumbImageError,
        MaterialStateProperty<Color?>? thumbColor,
        MaterialStateProperty<Color?>? trackColor,
        MaterialStateProperty<Color?>? trackOutlineColor,
        MaterialStateProperty<Icon?>? thumbIcon,
        MaterialTapTargetSize? materialTapTargetSize,
        DragStartBehavior dragStartBehavior,
        MouseCursor? mouseCursor,
        MaterialStateProperty<Color?>? overlayColor,
        double? splashRadius,
        FocusNode? focusNode,
        MaterialStatesController? statesController,
        Action<bool>? onFocusChange,
        bool autofocus,
        Color? tileColor,
        Widget? title,
        Widget? subtitle,
        bool? isThreeLine,
        bool? dense,
        EdgeInsetsGeometry? contentPadding,
        Widget? secondary,
        bool selected,
        ListTileControlAffinity? controlAffinity,
        ShapeBorder? shape,
        Color? selectedTileColor,
        VisualDensity? visualDensity,
        bool? enableFeedback,
        double? horizontalTitleGap,
        double? minVerticalPadding,
        double? minLeadingWidth,
        double? minTileHeight,
        Color? hoverColor,
        bool internalAddSemanticForOnTap,
        Key? key) : base(key)
    {
        if (activeThumbImage is null && onActiveThumbImageError is not null)
        {
            throw new ArgumentException(
                "SwitchListTile onActiveThumbImageError requires activeThumbImage.",
                nameof(onActiveThumbImageError));
        }

        if (inactiveThumbImage is null && onInactiveThumbImageError is not null)
        {
            throw new ArgumentException(
                "SwitchListTile onInactiveThumbImageError requires inactiveThumbImage.",
                nameof(onInactiveThumbImageError));
        }

        if (isThreeLine == true && subtitle is null)
        {
            throw new ArgumentException(
                "SwitchListTile with isThreeLine=true requires a subtitle.",
                nameof(isThreeLine));
        }

        SwitchType = switchListTileType;
        ApplyCupertinoTheme = applyCupertinoTheme;
        Value = value;
        OnChanged = onChanged;
#pragma warning disable CS0618 // Mirrors Flutter's deprecated SwitchListTile.activeColor.
        #pragma warning disable CS0618 // Mirrors Flutter's deprecated activeColor.
        ActiveColor = activeColor;
        #pragma warning restore CS0618
#pragma warning restore CS0618
        ActiveThumbColor = activeThumbColor;
        ActiveTrackColor = activeTrackColor;
        InactiveThumbColor = inactiveThumbColor;
        InactiveTrackColor = inactiveTrackColor;
        ActiveThumbImage = activeThumbImage;
        OnActiveThumbImageError = onActiveThumbImageError;
        InactiveThumbImage = inactiveThumbImage;
        OnInactiveThumbImageError = onInactiveThumbImageError;
        ThumbColor = thumbColor;
        TrackColor = trackColor;
        TrackOutlineColor = trackOutlineColor;
        ThumbIcon = thumbIcon;
        MaterialTapTargetSize = materialTapTargetSize;
        DragStartBehavior = dragStartBehavior;
        MouseCursor = mouseCursor;
        OverlayColor = overlayColor;
        SplashRadius = splashRadius;
        FocusNode = focusNode;
        StatesController = statesController;
        OnFocusChange = onFocusChange;
        Autofocus = autofocus;
        TileColor = tileColor;
        Title = title;
        Subtitle = subtitle;
        IsThreeLine = isThreeLine;
        Dense = dense;
        ContentPadding = contentPadding;
        Secondary = secondary;
        Selected = selected;
        ControlAffinity = controlAffinity;
        Shape = shape;
        SelectedTileColor = selectedTileColor;
        VisualDensity = visualDensity;
        EnableFeedback = enableFeedback;
        HorizontalTitleGap = horizontalTitleGap;
        MinVerticalPadding = minVerticalPadding;
        MinLeadingWidth = minLeadingWidth;
        MinTileHeight = minTileHeight;
        HoverColor = hoverColor;
        InternalAddSemanticForOnTap = internalAddSemanticForOnTap;
    }

    /// <summary>Creates a Material <see cref="ListTile"/> with an adaptive <see cref="Switch"/>.</summary>
    public static SwitchListTile Adaptive(
        bool value,
        Action<bool>? onChanged,
        Color? activeColor = null,
        Color? activeThumbColor = null,
        Color? activeTrackColor = null,
        Color? inactiveThumbColor = null,
        Color? inactiveTrackColor = null,
        ImageProvider? activeThumbImage = null,
        ImageErrorListener? onActiveThumbImageError = null,
        ImageProvider? inactiveThumbImage = null,
        ImageErrorListener? onInactiveThumbImageError = null,
        MaterialStateProperty<Color?>? thumbColor = null,
        MaterialStateProperty<Color?>? trackColor = null,
        MaterialStateProperty<Color?>? trackOutlineColor = null,
        MaterialStateProperty<Icon?>? thumbIcon = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        MouseCursor? mouseCursor = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        double? splashRadius = null,
        FocusNode? focusNode = null,
        MaterialStatesController? statesController = null,
        Action<bool>? onFocusChange = null,
        bool autofocus = false,
        bool? applyCupertinoTheme = null,
        Color? tileColor = null,
        Widget? title = null,
        Widget? subtitle = null,
        bool? isThreeLine = null,
        bool? dense = null,
        EdgeInsetsGeometry? contentPadding = null,
        Widget? secondary = null,
        bool selected = false,
        ListTileControlAffinity? controlAffinity = null,
        ShapeBorder? shape = null,
        Color? selectedTileColor = null,
        VisualDensity? visualDensity = null,
        bool? enableFeedback = null,
        double? horizontalTitleGap = null,
        double? minVerticalPadding = null,
        double? minLeadingWidth = null,
        double? minTileHeight = null,
        Color? hoverColor = null,
        bool internalAddSemanticForOnTap = false,
        Key? key = null)
    {
        return new SwitchListTile(
            switchListTileType: SwitchListTileType.Adaptive,
            applyCupertinoTheme: applyCupertinoTheme,
            value: value,
            onChanged: onChanged,
            activeColor: activeColor,
            activeThumbColor: activeThumbColor,
            activeTrackColor: activeTrackColor,
            inactiveThumbColor: inactiveThumbColor,
            inactiveTrackColor: inactiveTrackColor,
            activeThumbImage: activeThumbImage,
            onActiveThumbImageError: onActiveThumbImageError,
            inactiveThumbImage: inactiveThumbImage,
            onInactiveThumbImageError: onInactiveThumbImageError,
            thumbColor: thumbColor,
            trackColor: trackColor,
            trackOutlineColor: trackOutlineColor,
            thumbIcon: thumbIcon,
            materialTapTargetSize: materialTapTargetSize,
            dragStartBehavior: dragStartBehavior,
            mouseCursor: mouseCursor,
            overlayColor: overlayColor,
            splashRadius: splashRadius,
            focusNode: focusNode,
            statesController: statesController,
            onFocusChange: onFocusChange,
            autofocus: autofocus,
            tileColor: tileColor,
            title: title,
            subtitle: subtitle,
            isThreeLine: isThreeLine,
            dense: dense,
            contentPadding: contentPadding,
            secondary: secondary,
            selected: selected,
            controlAffinity: controlAffinity,
            shape: shape,
            selectedTileColor: selectedTileColor,
            visualDensity: visualDensity,
            enableFeedback: enableFeedback,
            horizontalTitleGap: horizontalTitleGap,
            minVerticalPadding: minVerticalPadding,
            minLeadingWidth: minLeadingWidth,
            minTileHeight: minTileHeight,
            hoverColor: hoverColor,
            internalAddSemanticForOnTap: internalAddSemanticForOnTap,
            key: key);
    }

    /// <summary>Whether this switch is checked.</summary>
    public bool Value { get; }

    /// <summary>Called when the user toggles the switch on or off.</summary>
    public Action<bool>? OnChanged { get; }

    /// <summary>Use <see cref="ActiveThumbColor"/> instead.</summary>
    [Obsolete("Use ActiveThumbColor instead. Mirrors Flutter's deprecation after v3.31.0-2.0.pre.")]
    public Color? ActiveColor { get; }

    /// <summary>The color to use when this switch is on.</summary>
    public Color? ActiveThumbColor { get; }

    /// <summary>The color to use on the track when this switch is on.</summary>
    public Color? ActiveTrackColor { get; }

    /// <summary>The color to use on the thumb when this switch is off.</summary>
    public Color? InactiveThumbColor { get; }

    /// <summary>The color to use on the track when this switch is off.</summary>
    public Color? InactiveTrackColor { get; }

    /// <summary>An image to use on the thumb of this switch when the switch is on.</summary>
    public ImageProvider? ActiveThumbImage { get; }

    /// <summary>An optional error callback for errors emitted when loading <see cref="ActiveThumbImage"/>.</summary>
    public ImageErrorListener? OnActiveThumbImageError { get; }

    /// <summary>An image to use on the thumb of this switch when the switch is off.</summary>
    public ImageProvider? InactiveThumbImage { get; }

    /// <summary>An optional error callback for errors emitted when loading <see cref="InactiveThumbImage"/>.</summary>
    public ImageErrorListener? OnInactiveThumbImageError { get; }

    /// <summary>The color of this switch's thumb, in all <see cref="MaterialState"/>s.</summary>
    public MaterialStateProperty<Color?>? ThumbColor { get; }

    /// <summary>The color of this switch's track, in all <see cref="MaterialState"/>s.</summary>
    public MaterialStateProperty<Color?>? TrackColor { get; }

    /// <summary>The outline color of this switch's track, in all <see cref="MaterialState"/>s.</summary>
    public MaterialStateProperty<Color?>? TrackOutlineColor { get; }

    /// <summary>The icon to use on the thumb of this switch, in all <see cref="MaterialState"/>s.</summary>
    public MaterialStateProperty<Icon?>? ThumbIcon { get; }

    /// <summary>Configures the minimum size of the tap target. Defaults to <see cref="MaterialTapTargetSize.ShrinkWrap"/>.</summary>
    public MaterialTapTargetSize? MaterialTapTargetSize { get; }

    /// <summary>Determines the way that drag start behavior is handled.</summary>
    public DragStartBehavior DragStartBehavior { get; }

    /// <summary>The cursor for a mouse pointer when it enters or is hovering over the switch.</summary>
    public MouseCursor? MouseCursor { get; }

    /// <summary>The color for the switch's <see cref="Plumix.Material.Material"/>.</summary>
    public MaterialStateProperty<Color?>? OverlayColor { get; }

    /// <summary>The splash radius of the circular ink response.</summary>
    public double? SplashRadius { get; }

    /// <summary>An optional focus node to use as the focus node for this widget.</summary>
    public FocusNode? FocusNode { get; }

    /// <summary>Controls the interactive states of the backing <see cref="ListTile"/>.</summary>
    public MaterialStatesController? StatesController { get; }

    /// <summary>Handler called when the focus changes.</summary>
    public Action<bool>? OnFocusChange { get; }

    /// <summary>Whether this widget should focus itself if nothing else is already focused.</summary>
    public bool Autofocus { get; }

    /// <summary>Whether the descendant <see cref="Plumix.Cupertino.CupertinoSwitch"/> reads the Cupertino theme.</summary>
    public bool? ApplyCupertinoTheme { get; }

    /// <summary>The color for the tile's <see cref="Plumix.Material.Material"/>.</summary>
    public Color? TileColor { get; }

    /// <summary>The primary content of the list tile.</summary>
    public Widget? Title { get; }

    /// <summary>Additional content displayed below the title.</summary>
    public Widget? Subtitle { get; }

    /// <summary>A widget to display on the opposite side of the tile from the switch.</summary>
    public Widget? Secondary { get; }

    /// <summary>Whether this list tile is intended to display three lines of text.</summary>
    public bool? IsThreeLine { get; }

    /// <summary>Whether this list tile is part of a vertically dense list.</summary>
    public bool? Dense { get; }

    /// <summary>Defines insets surrounding the tile's contents.</summary>
    public EdgeInsetsGeometry? ContentPadding { get; }

    /// <summary>Whether to render icons and text in the <see cref="ActiveThumbColor"/>.</summary>
    public bool Selected { get; }

    /// <summary>Where to place the control relative to the text.</summary>
    public ListTileControlAffinity? ControlAffinity { get; }

    /// <summary>The tile's shape.</summary>
    public ShapeBorder? Shape { get; }

    /// <summary>If non-null, defines the background color when <see cref="Selected"/> is true.</summary>
    public Color? SelectedTileColor { get; }

    /// <summary>Defines how compact the list tile's layout will be.</summary>
    public VisualDensity? VisualDensity { get; }

    /// <summary>Whether detected gestures should provide acoustic and/or haptic feedback.</summary>
    public bool? EnableFeedback { get; }

    /// <summary>The horizontal gap between the titles and the leading/trailing widgets.</summary>
    public double? HorizontalTitleGap { get; }

    /// <summary>The minimum padding on the top and bottom of the title and subtitle widgets.</summary>
    public double? MinVerticalPadding { get; }

    /// <summary>The minimum width allocated for the leading widget.</summary>
    public double? MinLeadingWidth { get; }

    /// <summary>The minimum height allocated for the tile.</summary>
    public double? MinTileHeight { get; }

    /// <summary>The color for the tile's <see cref="Plumix.Material.Material"/> when a pointer is hovering over it.</summary>
    public Color? HoverColor { get; }

    /// <summary>Whether to add button:true to the semantics if onTap is provided.</summary>
    public bool InternalAddSemanticForOnTap { get; }

    internal SwitchListTileType SwitchType { get; }

    /// <inheritdoc />
    public override Widget Build(BuildContext context)
    {
        Widget control;
        switch (SwitchType)
        {
            case SwitchListTileType.Adaptive:
                control = new ExcludeFocus(
                    child: Switch.Adaptive(
                        value: Value,
                        onChanged: OnChanged,
                        #pragma warning disable CS0618 // Mirrors Flutter's deprecated activeColor.
                        activeColor: ActiveColor,
                        #pragma warning restore CS0618
                        activeThumbColor: ActiveThumbColor,
                        activeThumbImage: ActiveThumbImage,
                        inactiveThumbImage: InactiveThumbImage,
                        materialTapTargetSize: MaterialTapTargetSize
                                               ?? Plumix.Material.MaterialTapTargetSize.ShrinkWrap,
                        activeTrackColor: ActiveTrackColor,
                        inactiveTrackColor: InactiveTrackColor,
                        inactiveThumbColor: InactiveThumbColor,
                        autofocus: Autofocus,
                        onFocusChange: OnFocusChange,
                        onActiveThumbImageError: OnActiveThumbImageError,
                        onInactiveThumbImageError: OnInactiveThumbImageError,
                        thumbColor: ThumbColor,
                        trackColor: TrackColor,
                        trackOutlineColor: TrackOutlineColor,
                        thumbIcon: ThumbIcon,
                        applyCupertinoTheme: ApplyCupertinoTheme,
                        dragStartBehavior: DragStartBehavior,
                        mouseCursor: MouseCursor,
                        splashRadius: SplashRadius,
                        overlayColor: OverlayColor));
                break;
            default:
                control = new ExcludeFocus(
                    child: new Switch(
                        value: Value,
                        onChanged: OnChanged,
                        #pragma warning disable CS0618 // Mirrors Flutter's deprecated activeColor.
                        activeColor: ActiveColor,
                        #pragma warning restore CS0618
                        activeThumbColor: ActiveThumbColor,
                        activeThumbImage: ActiveThumbImage,
                        inactiveThumbImage: InactiveThumbImage,
                        materialTapTargetSize: MaterialTapTargetSize
                                               ?? Plumix.Material.MaterialTapTargetSize.ShrinkWrap,
                        activeTrackColor: ActiveTrackColor,
                        inactiveTrackColor: InactiveTrackColor,
                        inactiveThumbColor: InactiveThumbColor,
                        autofocus: Autofocus,
                        onFocusChange: OnFocusChange,
                        onActiveThumbImageError: OnActiveThumbImageError,
                        onInactiveThumbImageError: OnInactiveThumbImageError,
                        thumbColor: ThumbColor,
                        trackColor: TrackColor,
                        trackOutlineColor: TrackOutlineColor,
                        thumbIcon: ThumbIcon,
                        dragStartBehavior: DragStartBehavior,
                        mouseCursor: MouseCursor,
                        splashRadius: SplashRadius,
                        overlayColor: OverlayColor));
                break;
        }

        ListTileThemeData listTileTheme = ListTileTheme.Of(context);
        ListTileControlAffinity effectiveControlAffinity =
            ControlAffinity ?? listTileTheme.ControlAffinity ?? ListTileControlAffinity.Platform;
        (Widget? leading, Widget? trailing) = effectiveControlAffinity switch
        {
            ListTileControlAffinity.Leading => (control, Secondary),
            _ => (Secondary, control)
        };

        ThemeData theme = Theme.Of(context);
        SwitchThemeData switchTheme = SwitchTheme.Of(context);
        MaterialState states = Selected ? MaterialState.Selected : MaterialState.None;
        Color effectiveActiveColor = ActiveThumbColor
                                     #pragma warning disable CS0618 // Mirrors Flutter's deprecated activeColor.
                                     ?? ActiveColor
                                     #pragma warning restore CS0618
                                     ?? switchTheme.ThumbColor?.Resolve(states)
                                     ?? theme.ColorScheme.Secondary;
        return new MergeSemantics(
            child: new ListTile(
                selectedColor: effectiveActiveColor,
                leading: leading,
                title: Title,
                subtitle: Subtitle,
                trailing: trailing,
                isThreeLine: IsThreeLine,
                dense: Dense,
                contentPadding: ContentPadding,
                enabled: OnChanged is not null,
                onTap: OnChanged is not null ? () => OnChanged(!Value) : null,
                selected: Selected,
                selectedTileColor: SelectedTileColor,
                autofocus: Autofocus,
                shape: Shape,
                tileColor: TileColor,
                visualDensity: VisualDensity,
                focusNode: FocusNode,
                statesController: StatesController,
                onFocusChange: OnFocusChange,
                enableFeedback: EnableFeedback,
                horizontalTitleGap: HorizontalTitleGap,
                minVerticalPadding: MinVerticalPadding,
                minLeadingWidth: MinLeadingWidth,
                minTileHeight: MinTileHeight,
                hoverColor: HoverColor,
                internalAddSemanticForOnTap: InternalAddSemanticForOnTap));
    }
}
