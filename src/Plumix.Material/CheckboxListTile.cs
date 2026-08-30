using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/checkbox_list_tile.dart

internal enum CheckboxListTileType
{
    Material,
    Adaptive
}

/// <summary>
/// A <see cref="ListTile"/> with a <see cref="Checkbox"/>. In other words, a checkbox with a label.
/// </summary>
public sealed class CheckboxListTile : StatelessWidget
{
    /// <summary>Creates a combination of a list tile and a checkbox.</summary>
    public CheckboxListTile(
        bool? value,
        Action<bool?>? onChanged,
        MouseCursor? mouseCursor = null,
        Color? activeColor = null,
        MaterialStateProperty<Color?>? fillColor = null,
        Color? checkColor = null,
        Color? hoverColor = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        double? splashRadius = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        VisualDensity? visualDensity = null,
        FocusNode? focusNode = null,
        MaterialStatesController? statesController = null,
        bool autofocus = false,
        ShapeBorder? shape = null,
        WidgetStateBorderSide? side = null,
        bool isError = false,
        bool? enabled = null,
        Color? tileColor = null,
        Widget? title = null,
        Widget? subtitle = null,
        bool? isThreeLine = null,
        bool? dense = null,
        Widget? secondary = null,
        bool selected = false,
        ListTileControlAffinity? controlAffinity = null,
        EdgeInsetsGeometry? contentPadding = null,
        bool tristate = false,
        OutlinedBorder? checkboxShape = null,
        Color? selectedTileColor = null,
        Action<bool>? onFocusChange = null,
        bool? enableFeedback = null,
        double? horizontalTitleGap = null,
        double? minVerticalPadding = null,
        double? minLeadingWidth = null,
        double? minTileHeight = null,
        string? checkboxSemanticLabel = null,
        double checkboxScaleFactor = 1.0,
        ListTileTitleAlignment? titleAlignment = null,
        bool internalAddSemanticForOnTap = false,
        Key? key = null) : this(
            checkboxType: CheckboxListTileType.Material,
            value: value,
            onChanged: onChanged,
            mouseCursor: mouseCursor,
            activeColor: activeColor,
            fillColor: fillColor,
            checkColor: checkColor,
            hoverColor: hoverColor,
            overlayColor: overlayColor,
            splashRadius: splashRadius,
            materialTapTargetSize: materialTapTargetSize,
            visualDensity: visualDensity,
            focusNode: focusNode,
            statesController: statesController,
            autofocus: autofocus,
            shape: shape,
            side: side,
            isError: isError,
            enabled: enabled,
            tileColor: tileColor,
            title: title,
            subtitle: subtitle,
            isThreeLine: isThreeLine,
            dense: dense,
            secondary: secondary,
            selected: selected,
            controlAffinity: controlAffinity,
            contentPadding: contentPadding,
            tristate: tristate,
            checkboxShape: checkboxShape,
            selectedTileColor: selectedTileColor,
            onFocusChange: onFocusChange,
            enableFeedback: enableFeedback,
            horizontalTitleGap: horizontalTitleGap,
            minVerticalPadding: minVerticalPadding,
            minLeadingWidth: minLeadingWidth,
            minTileHeight: minTileHeight,
            checkboxSemanticLabel: checkboxSemanticLabel,
            checkboxScaleFactor: checkboxScaleFactor,
            titleAlignment: titleAlignment,
            internalAddSemanticForOnTap: internalAddSemanticForOnTap,
            key: key)
    {
    }

    private CheckboxListTile(
        CheckboxListTileType checkboxType,
        bool? value,
        Action<bool?>? onChanged,
        MouseCursor? mouseCursor,
        Color? activeColor,
        MaterialStateProperty<Color?>? fillColor,
        Color? checkColor,
        Color? hoverColor,
        MaterialStateProperty<Color?>? overlayColor,
        double? splashRadius,
        MaterialTapTargetSize? materialTapTargetSize,
        VisualDensity? visualDensity,
        FocusNode? focusNode,
        MaterialStatesController? statesController,
        bool autofocus,
        ShapeBorder? shape,
        WidgetStateBorderSide? side,
        bool isError,
        bool? enabled,
        Color? tileColor,
        Widget? title,
        Widget? subtitle,
        bool? isThreeLine,
        bool? dense,
        Widget? secondary,
        bool selected,
        ListTileControlAffinity? controlAffinity,
        EdgeInsetsGeometry? contentPadding,
        bool tristate,
        OutlinedBorder? checkboxShape,
        Color? selectedTileColor,
        Action<bool>? onFocusChange,
        bool? enableFeedback,
        double? horizontalTitleGap,
        double? minVerticalPadding,
        double? minLeadingWidth,
        double? minTileHeight,
        string? checkboxSemanticLabel,
        double checkboxScaleFactor,
        ListTileTitleAlignment? titleAlignment,
        bool internalAddSemanticForOnTap,
        Key? key) : base(key)
    {
        if (!tristate && value is null)
        {
            throw new ArgumentException(
                "CheckboxListTile value cannot be null when tristate is false.",
                nameof(value));
        }

        if (isThreeLine == true && subtitle is null)
        {
            throw new ArgumentException(
                "CheckboxListTile with isThreeLine=true requires a subtitle.",
                nameof(isThreeLine));
        }

        CheckboxType = checkboxType;
        Value = value;
        OnChanged = onChanged;
        MouseCursor = mouseCursor;
        ActiveColor = activeColor;
        FillColor = fillColor;
        CheckColor = checkColor;
        HoverColor = hoverColor;
        OverlayColor = overlayColor;
        SplashRadius = splashRadius;
        MaterialTapTargetSize = materialTapTargetSize;
        VisualDensity = visualDensity;
        FocusNode = focusNode;
        StatesController = statesController;
        Autofocus = autofocus;
        Shape = shape;
        Side = side;
        IsError = isError;
        Enabled = enabled;
        TileColor = tileColor;
        Title = title;
        Subtitle = subtitle;
        IsThreeLine = isThreeLine;
        Dense = dense;
        Secondary = secondary;
        Selected = selected;
        ControlAffinity = controlAffinity;
        ContentPadding = contentPadding;
        Tristate = tristate;
        CheckboxShape = checkboxShape;
        SelectedTileColor = selectedTileColor;
        OnFocusChange = onFocusChange;
        EnableFeedback = enableFeedback;
        HorizontalTitleGap = horizontalTitleGap;
        MinVerticalPadding = minVerticalPadding;
        MinLeadingWidth = minLeadingWidth;
        MinTileHeight = minTileHeight;
        CheckboxSemanticLabel = checkboxSemanticLabel;
        CheckboxScaleFactor = checkboxScaleFactor;
        TitleAlignment = titleAlignment;
        InternalAddSemanticForOnTap = internalAddSemanticForOnTap;
    }

    /// <summary>Creates a combination of a list tile and a platform adaptive checkbox.</summary>
    public static CheckboxListTile Adaptive(
        bool? value,
        Action<bool?>? onChanged,
        MouseCursor? mouseCursor = null,
        Color? activeColor = null,
        MaterialStateProperty<Color?>? fillColor = null,
        Color? checkColor = null,
        Color? hoverColor = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        double? splashRadius = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        VisualDensity? visualDensity = null,
        FocusNode? focusNode = null,
        MaterialStatesController? statesController = null,
        bool autofocus = false,
        ShapeBorder? shape = null,
        WidgetStateBorderSide? side = null,
        bool isError = false,
        bool? enabled = null,
        Color? tileColor = null,
        Widget? title = null,
        Widget? subtitle = null,
        bool? isThreeLine = null,
        bool? dense = null,
        Widget? secondary = null,
        bool selected = false,
        ListTileControlAffinity? controlAffinity = null,
        EdgeInsetsGeometry? contentPadding = null,
        bool tristate = false,
        OutlinedBorder? checkboxShape = null,
        Color? selectedTileColor = null,
        Action<bool>? onFocusChange = null,
        bool? enableFeedback = null,
        double? horizontalTitleGap = null,
        double? minVerticalPadding = null,
        double? minLeadingWidth = null,
        double? minTileHeight = null,
        string? checkboxSemanticLabel = null,
        double checkboxScaleFactor = 1.0,
        ListTileTitleAlignment? titleAlignment = null,
        bool internalAddSemanticForOnTap = false,
        Key? key = null)
    {
        return new CheckboxListTile(
            checkboxType: CheckboxListTileType.Adaptive,
            value: value,
            onChanged: onChanged,
            mouseCursor: mouseCursor,
            activeColor: activeColor,
            fillColor: fillColor,
            checkColor: checkColor,
            hoverColor: hoverColor,
            overlayColor: overlayColor,
            splashRadius: splashRadius,
            materialTapTargetSize: materialTapTargetSize,
            visualDensity: visualDensity,
            focusNode: focusNode,
            statesController: statesController,
            autofocus: autofocus,
            shape: shape,
            side: side,
            isError: isError,
            enabled: enabled,
            tileColor: tileColor,
            title: title,
            subtitle: subtitle,
            isThreeLine: isThreeLine,
            dense: dense,
            secondary: secondary,
            selected: selected,
            controlAffinity: controlAffinity,
            contentPadding: contentPadding,
            tristate: tristate,
            checkboxShape: checkboxShape,
            selectedTileColor: selectedTileColor,
            onFocusChange: onFocusChange,
            enableFeedback: enableFeedback,
            horizontalTitleGap: horizontalTitleGap,
            minVerticalPadding: minVerticalPadding,
            minLeadingWidth: minLeadingWidth,
            minTileHeight: minTileHeight,
            checkboxSemanticLabel: checkboxSemanticLabel,
            checkboxScaleFactor: checkboxScaleFactor,
            titleAlignment: titleAlignment,
            internalAddSemanticForOnTap: internalAddSemanticForOnTap,
            key: key);
    }

    /// <summary>Whether this checkbox is checked.</summary>
    public bool? Value { get; }

    /// <summary>Called when the value of the checkbox should change.</summary>
    public Action<bool?>? OnChanged { get; }

    /// <summary>The cursor for a mouse pointer when it enters or is hovering over the checkbox.</summary>
    public MouseCursor? MouseCursor { get; }

    /// <summary>The color to use when this checkbox is checked.</summary>
    public Color? ActiveColor { get; }

    /// <summary>The color that fills the checkbox, in all <see cref="MaterialState"/>s.</summary>
    public MaterialStateProperty<Color?>? FillColor { get; }

    /// <summary>The color to use for the check icon when this checkbox is checked.</summary>
    public Color? CheckColor { get; }

    /// <summary>The color for the checkbox's <see cref="Plumix.Material.Material"/> when a pointer is hovering over it.</summary>
    public Color? HoverColor { get; }

    /// <summary>The color for the checkbox's <see cref="Plumix.Material.Material"/>.</summary>
    public MaterialStateProperty<Color?>? OverlayColor { get; }

    /// <summary>The splash radius of the circular ink response.</summary>
    public double? SplashRadius { get; }

    /// <summary>Configures the minimum size of the tap target. Defaults to <see cref="MaterialTapTargetSize.ShrinkWrap"/>.</summary>
    public MaterialTapTargetSize? MaterialTapTargetSize { get; }

    /// <summary>Defines how compact the list tile's layout will be.</summary>
    public VisualDensity? VisualDensity { get; }

    /// <summary>An optional focus node to use as the focus node for this widget.</summary>
    public FocusNode? FocusNode { get; }

    /// <summary>Controls the interactive states of the backing <see cref="ListTile"/>.</summary>
    public MaterialStatesController? StatesController { get; }

    /// <summary>Whether this widget should focus itself if nothing else is already focused.</summary>
    public bool Autofocus { get; }

    /// <summary>The tile's shape.</summary>
    public ShapeBorder? Shape { get; }

    /// <summary>The color and width of the checkbox's border.</summary>
    public WidgetStateBorderSide? Side { get; }

    /// <summary>Whether the checkbox is in an error state. Defaults to false.</summary>
    public bool IsError { get; }

    /// <summary>The color for the tile's <see cref="Plumix.Material.Material"/>.</summary>
    public Color? TileColor { get; }

    /// <summary>The primary content of the list tile.</summary>
    public Widget? Title { get; }

    /// <summary>Additional content displayed below the title.</summary>
    public Widget? Subtitle { get; }

    /// <summary>A widget to display on the opposite side of the tile from the checkbox.</summary>
    public Widget? Secondary { get; }

    /// <summary>Whether this list tile is intended to display three lines of text.</summary>
    public bool? IsThreeLine { get; }

    /// <summary>Whether this list tile is part of a vertically dense list.</summary>
    public bool? Dense { get; }

    /// <summary>Whether to render icons and text in the <see cref="ActiveColor"/>.</summary>
    public bool Selected { get; }

    /// <summary>Where to place the control relative to the text.</summary>
    public ListTileControlAffinity? ControlAffinity { get; }

    /// <summary>Defines insets surrounding the tile's contents.</summary>
    public EdgeInsetsGeometry? ContentPadding { get; }

    /// <summary>If true the checkbox's <see cref="Value"/> can be true, false, or null.</summary>
    public bool Tristate { get; }

    /// <summary>The shape of the checkbox.</summary>
    public OutlinedBorder? CheckboxShape { get; }

    /// <summary>If non-null, defines the background color when <see cref="Selected"/> is true.</summary>
    public Color? SelectedTileColor { get; }

    /// <summary>Handler called when the focus changes.</summary>
    public Action<bool>? OnFocusChange { get; }

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

    /// <summary>Whether the <see cref="CheckboxListTile"/> is interactive.</summary>
    public bool? Enabled { get; }

    /// <summary>Defines how the leading and trailing widgets are vertically aligned.</summary>
    public ListTileTitleAlignment? TitleAlignment { get; }

    /// <summary>Whether to add button:true to the semantics if onTap is provided.</summary>
    public bool InternalAddSemanticForOnTap { get; }

    /// <summary>The scaling factor applied to the <see cref="Checkbox"/>. Defaults to 1.0.</summary>
    public double CheckboxScaleFactor { get; }

    /// <summary>The semantic label for the checkbox.</summary>
    public string? CheckboxSemanticLabel { get; }

    internal CheckboxListTileType CheckboxType { get; }

    private void HandleValueChange()
    {
        switch (Value)
        {
            case false:
                OnChanged!(true);
                break;
            case true:
                OnChanged!(Tristate ? null : false);
                break;
            default:
                OnChanged!(false);
                break;
        }
    }

    /// <inheritdoc />
    public override Widget Build(BuildContext context)
    {
        Widget control;
        switch (CheckboxType)
        {
            case CheckboxListTileType.Material:
                control = new ExcludeFocus(
                    child: new Checkbox(
                        value: Value,
                        onChanged: (Enabled ?? true) ? OnChanged : null,
                        mouseCursor: MouseCursor,
                        activeColor: ActiveColor,
                        fillColor: FillColor,
                        checkColor: CheckColor,
                        hoverColor: HoverColor,
                        overlayColor: OverlayColor,
                        splashRadius: SplashRadius,
                        materialTapTargetSize: MaterialTapTargetSize
                                               ?? Plumix.Material.MaterialTapTargetSize.ShrinkWrap,
                        autofocus: Autofocus,
                        tristate: Tristate,
                        shape: CheckboxShape,
                        side: Side,
                        isError: IsError,
                        semanticLabel: CheckboxSemanticLabel));
                break;
            default:
                control = new ExcludeFocus(
                    child: Checkbox.Adaptive(
                        value: Value,
                        onChanged: (Enabled ?? true) ? OnChanged : null,
                        mouseCursor: MouseCursor,
                        activeColor: ActiveColor,
                        fillColor: FillColor,
                        checkColor: CheckColor,
                        hoverColor: HoverColor,
                        overlayColor: OverlayColor,
                        splashRadius: SplashRadius,
                        materialTapTargetSize: MaterialTapTargetSize
                                               ?? Plumix.Material.MaterialTapTargetSize.ShrinkWrap,
                        autofocus: Autofocus,
                        tristate: Tristate,
                        shape: CheckboxShape,
                        side: Side,
                        isError: IsError,
                        semanticLabel: CheckboxSemanticLabel));
                break;
        }

        if (CheckboxScaleFactor != 1.0)
        {
            control = Widgets.Transform.Scale(scale: CheckboxScaleFactor, child: control);
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
        CheckboxThemeData checkboxTheme = CheckboxTheme.Of(context);
        MaterialState states = Selected ? MaterialState.Selected : MaterialState.None;
        Color effectiveActiveColor = ActiveColor
                                     ?? checkboxTheme.FillColor?.Resolve(states)
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
                enabled: Enabled ?? OnChanged is not null,
                onTap: OnChanged is not null ? HandleValueChange : null,
                selected: Selected,
                autofocus: Autofocus,
                contentPadding: ContentPadding,
                shape: Shape,
                selectedTileColor: SelectedTileColor,
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
                titleAlignment: TitleAlignment,
                internalAddSemanticForOnTap: InternalAddSemanticForOnTap));
    }
}
