using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): material_ui/lib/src/checkbox_list_tile.dart
public sealed class CheckboxListTile : StatelessWidget
{
    private readonly bool _adaptive;

    public CheckboxListTile(
        bool? value,
        Action<bool?>? onChanged,
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
        Thickness? contentPadding = null,
        bool tristate = false,
        ShapeBorder? checkboxShape = null,
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
        MouseCursor? mouseCursor = null,
        Key? key = null) : this(
            value,
            onChanged,
            activeColor,
            fillColor,
            checkColor,
            hoverColor,
            overlayColor,
            splashRadius,
            materialTapTargetSize,
            visualDensity,
            focusNode,
            statesController,
            autofocus,
            shape,
            side,
            isError,
            enabled,
            tileColor,
            title,
            subtitle,
            isThreeLine,
            dense,
            secondary,
            selected,
            controlAffinity,
            contentPadding,
            tristate,
            checkboxShape,
            selectedTileColor,
            onFocusChange,
            enableFeedback,
            horizontalTitleGap,
            minVerticalPadding,
            minLeadingWidth,
            minTileHeight,
            checkboxSemanticLabel,
            checkboxScaleFactor,
            titleAlignment,
            internalAddSemanticForOnTap,
            mouseCursor,
            adaptive: false,
            key)
    {
    }

    private CheckboxListTile(
        bool? value,
        Action<bool?>? onChanged,
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
        Thickness? contentPadding,
        bool tristate,
        ShapeBorder? checkboxShape,
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
        MouseCursor? mouseCursor,
        bool adaptive,
        Key? key) : base(key)
    {
        if (!tristate && value is null)
        {
            throw new ArgumentException("CheckboxListTile value cannot be null when tristate is false.", nameof(value));
        }

        if (isThreeLine == true && subtitle is null)
        {
            throw new ArgumentException("CheckboxListTile with isThreeLine=true requires a subtitle.", nameof(isThreeLine));
        }

        if (!double.IsFinite(checkboxScaleFactor) || checkboxScaleFactor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(checkboxScaleFactor), "Checkbox scale factor must be finite and positive.");
        }

        Value = value;
        OnChanged = onChanged;
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
        MouseCursor = mouseCursor;
        _adaptive = adaptive;
    }

    public bool? Value { get; }
    public Action<bool?>? OnChanged { get; }
    public Color? ActiveColor { get; }
    public MaterialStateProperty<Color?>? FillColor { get; }
    public Color? CheckColor { get; }
    public Color? HoverColor { get; }
    public MaterialStateProperty<Color?>? OverlayColor { get; }
    public double? SplashRadius { get; }
    public MaterialTapTargetSize? MaterialTapTargetSize { get; }
    public VisualDensity? VisualDensity { get; }
    public FocusNode? FocusNode { get; }
    public MaterialStatesController? StatesController { get; }
    public bool Autofocus { get; }
    public ShapeBorder? Shape { get; }
    public WidgetStateBorderSide? Side { get; }
    public bool IsError { get; }
    public bool? Enabled { get; }
    public Color? TileColor { get; }
    public Widget? Title { get; }
    public Widget? Subtitle { get; }
    public bool? IsThreeLine { get; }
    public bool? Dense { get; }
    public Widget? Secondary { get; }
    public bool Selected { get; }
    public ListTileControlAffinity? ControlAffinity { get; }
    public Thickness? ContentPadding { get; }
    public bool Tristate { get; }
    public ShapeBorder? CheckboxShape { get; }
    public Color? SelectedTileColor { get; }
    public Action<bool>? OnFocusChange { get; }
    public bool? EnableFeedback { get; }
    public double? HorizontalTitleGap { get; }
    public double? MinVerticalPadding { get; }
    public double? MinLeadingWidth { get; }
    public double? MinTileHeight { get; }
    public string? CheckboxSemanticLabel { get; }
    public double CheckboxScaleFactor { get; }
    public ListTileTitleAlignment? TitleAlignment { get; }
    public bool InternalAddSemanticForOnTap { get; }
    public MouseCursor? MouseCursor { get; }

    public static CheckboxListTile Adaptive(
        bool? value,
        Action<bool?>? onChanged,
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
        Thickness? contentPadding = null,
        bool tristate = false,
        ShapeBorder? checkboxShape = null,
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
        MouseCursor? mouseCursor = null,
        Key? key = null)
    {
        return new CheckboxListTile(
            value,
            onChanged,
            activeColor,
            fillColor,
            checkColor,
            hoverColor,
            overlayColor,
            splashRadius,
            materialTapTargetSize,
            visualDensity,
            focusNode,
            statesController,
            autofocus,
            shape,
            side,
            isError,
            enabled,
            tileColor,
            title,
            subtitle,
            isThreeLine,
            dense,
            secondary,
            selected,
            controlAffinity,
            contentPadding,
            tristate,
            checkboxShape,
            selectedTileColor,
            onFocusChange,
            enableFeedback,
            horizontalTitleGap,
            minVerticalPadding,
            minLeadingWidth,
            minTileHeight,
            checkboxSemanticLabel,
            checkboxScaleFactor,
            titleAlignment,
            internalAddSemanticForOnTap,
            mouseCursor,
            adaptive: true,
            key);
    }

    public override Widget Build(BuildContext context)
    {
        var controlOnChanged = (Enabled ?? true) ? OnChanged : null;
        Widget control = _adaptive
            ? Checkbox.Adaptive(
                value: Value,
                onChanged: controlOnChanged,
                tristate: Tristate,
                activeColor: ActiveColor,
                fillColor: FillColor,
                checkColor: CheckColor,
                overlayColor: OverlayColor,
                hoverColor: HoverColor,
                materialTapTargetSize: MaterialTapTargetSize ?? Plumix.Material.MaterialTapTargetSize.ShrinkWrap,
                shape: CheckboxShape,
                side: Side,
                splashRadius: SplashRadius,
                isError: IsError,
                semanticLabel: CheckboxSemanticLabel,
                autofocus: Autofocus)
            : new Checkbox(
                value: Value,
                onChanged: controlOnChanged,
                tristate: Tristate,
                activeColor: ActiveColor,
                fillColor: FillColor,
                checkColor: CheckColor,
                overlayColor: OverlayColor,
                hoverColor: HoverColor,
                materialTapTargetSize: MaterialTapTargetSize ?? Plumix.Material.MaterialTapTargetSize.ShrinkWrap,
                shape: CheckboxShape,
                side: Side,
                splashRadius: SplashRadius,
                isError: IsError,
                semanticLabel: CheckboxSemanticLabel,
                autofocus: Autofocus);
        control = new ExcludeFocus(child: control);

        if (CheckboxScaleFactor != 1.0)
        {
            double center = Checkbox.Width / 2.0;
            Matrix4 scale = Matrix4.TranslationValues(center, center, 0.0);
            scale.ScaleByDouble(CheckboxScaleFactor, CheckboxScaleFactor, 1.0, 1);
            scale.TranslateByDouble(-center, -center, 0, 1);
            control = new Plumix.Widgets.Transform(transform: scale, child: control);
        }

        var affinity = ControlAffinity
                       ?? ListTileTheme.Of(context).ControlAffinity
                       ?? ListTileControlAffinity.Platform;
        var leading = affinity == ListTileControlAffinity.Leading ? control : Secondary;
        var trailing = affinity == ListTileControlAffinity.Leading ? Secondary : control;
        var selectedStates = Selected ? MaterialState.Selected : MaterialState.None;
        var selectedColor = ActiveColor
                            ?? CheckboxTheme.Of(context).FillColor?.Resolve(selectedStates)
                            ?? Theme.Of(context).ColorScheme.Secondary;

        bool tileEnabled = Enabled ?? OnChanged is not null;
        var tile = new ListTile(
                selectedColor: selectedColor,
                leading: leading,
                title: Title,
                subtitle: Subtitle,
                trailing: trailing,
                isThreeLine: IsThreeLine,
                dense: Dense,
                enabled: tileEnabled,
                onTap: OnChanged is not null ? HandleValueChange : null,
                selected: Selected,
                autofocus: Autofocus,
                contentPadding: ContentPadding,
                shape: Shape,
                selectedTileColor: SelectedTileColor,
                tileColor: TileColor,
                focusNode: FocusNode,
                mouseCursor: MouseCursor,
                onFocusChange: OnFocusChange,
                enableFeedback: EnableFeedback,
                horizontalTitleGap: HorizontalTitleGap,
                minVerticalPadding: MinVerticalPadding,
                minLeadingWidth: MinLeadingWidth,
                minTileHeight: MinTileHeight,
                visualDensity: VisualDensity,
                titleAlignment: TitleAlignment,
                internalAddSemanticForOnTap: InternalAddSemanticForOnTap,
                statesController: StatesController);
        var semanticFlags = Value == true ? SemanticsFlags.IsChecked : SemanticsFlags.None;
        if (tileEnabled)
        {
            semanticFlags |= SemanticsFlags.IsEnabled;
        }

        return new Semantics(
            child: new MergeSemantics(child: tile),
            flags: semanticFlags,
            onTap: tileEnabled && OnChanged is not null ? HandleValueChange : null,
            container: true);
    }

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
}

// Dart parity source (reference): material_ui/lib/src/switch_list_tile.dart
public sealed class SwitchListTile : StatelessWidget
{
    private readonly bool _adaptive;

    public SwitchListTile(
        bool value,
        Action<bool>? onChanged,
        Color? activeColor = null,
        Color? activeThumbColor = null,
        Color? activeTrackColor = null,
        Color? inactiveThumbColor = null,
        Color? inactiveTrackColor = null,
        MaterialStateProperty<Color?>? thumbColor = null,
        MaterialStateProperty<Color?>? trackColor = null,
        MaterialStateProperty<Color?>? trackOutlineColor = null,
        MaterialStateProperty<Icon?>? thumbIcon = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        VisualDensity? visualDensity = null,
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
        Widget? secondary = null,
        bool? isThreeLine = null,
        bool? dense = null,
        Thickness? contentPadding = null,
        bool selected = false,
        ListTileControlAffinity? controlAffinity = null,
        ShapeBorder? shape = null,
        Color? selectedTileColor = null,
        bool? enableFeedback = null,
        double? horizontalTitleGap = null,
        double? minVerticalPadding = null,
        double? minLeadingWidth = null,
        double? minTileHeight = null,
        Color? hoverColor = null,
        bool internalAddSemanticForOnTap = false,
        Key? key = null) : this(
            value,
            onChanged,
            activeColor,
            activeThumbColor,
            activeTrackColor,
            inactiveThumbColor,
            inactiveTrackColor,
            thumbColor,
            trackColor,
            trackOutlineColor,
            thumbIcon,
            materialTapTargetSize,
            visualDensity,
            mouseCursor,
            overlayColor,
            splashRadius,
            focusNode,
            statesController,
            onFocusChange,
            autofocus,
            tileColor,
            title,
            subtitle,
            secondary,
            isThreeLine,
            dense,
            contentPadding,
            selected,
            controlAffinity,
            shape,
            selectedTileColor,
            enableFeedback,
            horizontalTitleGap,
            minVerticalPadding,
            minLeadingWidth,
            minTileHeight,
            hoverColor,
            internalAddSemanticForOnTap,
            adaptive: false,
            key)
    {
    }

    private SwitchListTile(
        bool value,
        Action<bool>? onChanged,
        Color? activeColor,
        Color? activeThumbColor,
        Color? activeTrackColor,
        Color? inactiveThumbColor,
        Color? inactiveTrackColor,
        MaterialStateProperty<Color?>? thumbColor,
        MaterialStateProperty<Color?>? trackColor,
        MaterialStateProperty<Color?>? trackOutlineColor,
        MaterialStateProperty<Icon?>? thumbIcon,
        MaterialTapTargetSize? materialTapTargetSize,
        VisualDensity? visualDensity,
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
        Widget? secondary,
        bool? isThreeLine,
        bool? dense,
        Thickness? contentPadding,
        bool selected,
        ListTileControlAffinity? controlAffinity,
        ShapeBorder? shape,
        Color? selectedTileColor,
        bool? enableFeedback,
        double? horizontalTitleGap,
        double? minVerticalPadding,
        double? minLeadingWidth,
        double? minTileHeight,
        Color? hoverColor,
        bool internalAddSemanticForOnTap,
        bool adaptive,
        Key? key) : base(key)
    {
        if (isThreeLine == true && subtitle is null)
        {
            throw new ArgumentException("SwitchListTile with isThreeLine=true requires a subtitle.", nameof(isThreeLine));
        }

        Value = value;
        OnChanged = onChanged;
        ActiveColor = activeColor;
        ActiveThumbColor = activeThumbColor;
        ActiveTrackColor = activeTrackColor;
        InactiveThumbColor = inactiveThumbColor;
        InactiveTrackColor = inactiveTrackColor;
        ThumbColor = thumbColor;
        TrackColor = trackColor;
        TrackOutlineColor = trackOutlineColor;
        ThumbIcon = thumbIcon;
        MaterialTapTargetSize = materialTapTargetSize;
        VisualDensity = visualDensity;
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
        Secondary = secondary;
        IsThreeLine = isThreeLine;
        Dense = dense;
        ContentPadding = contentPadding;
        Selected = selected;
        ControlAffinity = controlAffinity;
        Shape = shape;
        SelectedTileColor = selectedTileColor;
        EnableFeedback = enableFeedback;
        HorizontalTitleGap = horizontalTitleGap;
        MinVerticalPadding = minVerticalPadding;
        MinLeadingWidth = minLeadingWidth;
        MinTileHeight = minTileHeight;
        HoverColor = hoverColor;
        InternalAddSemanticForOnTap = internalAddSemanticForOnTap;
        _adaptive = adaptive;
    }

    public bool Value { get; }
    public Action<bool>? OnChanged { get; }
    public Color? ActiveColor { get; }
    public Color? ActiveThumbColor { get; }
    public Color? ActiveTrackColor { get; }
    public Color? InactiveThumbColor { get; }
    public Color? InactiveTrackColor { get; }
    public MaterialStateProperty<Color?>? ThumbColor { get; }
    public MaterialStateProperty<Color?>? TrackColor { get; }
    public MaterialStateProperty<Color?>? TrackOutlineColor { get; }
    public MaterialStateProperty<Icon?>? ThumbIcon { get; }
    public MaterialTapTargetSize? MaterialTapTargetSize { get; }
    public VisualDensity? VisualDensity { get; }
    public MouseCursor? MouseCursor { get; }
    public MaterialStateProperty<Color?>? OverlayColor { get; }
    public double? SplashRadius { get; }
    public FocusNode? FocusNode { get; }
    public MaterialStatesController? StatesController { get; }
    public Action<bool>? OnFocusChange { get; }
    public bool Autofocus { get; }
    public Color? TileColor { get; }
    public Widget? Title { get; }
    public Widget? Subtitle { get; }
    public Widget? Secondary { get; }
    public bool? IsThreeLine { get; }
    public bool? Dense { get; }
    public Thickness? ContentPadding { get; }
    public bool Selected { get; }
    public ListTileControlAffinity? ControlAffinity { get; }
    public ShapeBorder? Shape { get; }
    public Color? SelectedTileColor { get; }
    public bool? EnableFeedback { get; }
    public double? HorizontalTitleGap { get; }
    public double? MinVerticalPadding { get; }
    public double? MinLeadingWidth { get; }
    public double? MinTileHeight { get; }
    public Color? HoverColor { get; }
    public bool InternalAddSemanticForOnTap { get; }

    public static SwitchListTile Adaptive(
        bool value,
        Action<bool>? onChanged,
        Color? activeColor = null,
        Color? activeThumbColor = null,
        Color? activeTrackColor = null,
        Color? inactiveThumbColor = null,
        Color? inactiveTrackColor = null,
        MaterialStateProperty<Color?>? thumbColor = null,
        MaterialStateProperty<Color?>? trackColor = null,
        MaterialStateProperty<Color?>? trackOutlineColor = null,
        MaterialStateProperty<Icon?>? thumbIcon = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        VisualDensity? visualDensity = null,
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
        Widget? secondary = null,
        bool? isThreeLine = null,
        bool? dense = null,
        Thickness? contentPadding = null,
        bool selected = false,
        ListTileControlAffinity? controlAffinity = null,
        ShapeBorder? shape = null,
        Color? selectedTileColor = null,
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
            value,
            onChanged,
            activeColor,
            activeThumbColor,
            activeTrackColor,
            inactiveThumbColor,
            inactiveTrackColor,
            thumbColor,
            trackColor,
            trackOutlineColor,
            thumbIcon,
            materialTapTargetSize,
            visualDensity,
            mouseCursor,
            overlayColor,
            splashRadius,
            focusNode,
            statesController,
            onFocusChange,
            autofocus,
            tileColor,
            title,
            subtitle,
            secondary,
            isThreeLine,
            dense,
            contentPadding,
            selected,
            controlAffinity,
            shape,
            selectedTileColor,
            enableFeedback,
            horizontalTitleGap,
            minVerticalPadding,
            minLeadingWidth,
            minTileHeight,
            hoverColor,
            internalAddSemanticForOnTap,
            adaptive: true,
            key);
    }

    public override Widget Build(BuildContext context)
    {
        Widget control = _adaptive
            ? Switch.Adaptive(
                value: Value,
                onChanged: OnChanged,
                activeColor: ActiveColor,
                activeThumbColor: ActiveThumbColor,
                activeTrackColor: ActiveTrackColor,
                inactiveThumbColor: InactiveThumbColor,
                inactiveTrackColor: InactiveTrackColor,
                thumbColor: ThumbColor,
                trackColor: TrackColor,
                trackOutlineColor: TrackOutlineColor,
                thumbIcon: ThumbIcon,
                materialTapTargetSize: MaterialTapTargetSize ?? Plumix.Material.MaterialTapTargetSize.ShrinkWrap,
                overlayColor: OverlayColor,
                splashRadius: SplashRadius,
                onFocusChange: OnFocusChange,
                autofocus: Autofocus)
            : new Switch(
                value: Value,
                onChanged: OnChanged,
                activeColor: ActiveColor,
                activeThumbColor: ActiveThumbColor,
                activeTrackColor: ActiveTrackColor,
                inactiveThumbColor: InactiveThumbColor,
                inactiveTrackColor: InactiveTrackColor,
                thumbColor: ThumbColor,
                trackColor: TrackColor,
                trackOutlineColor: TrackOutlineColor,
                thumbIcon: ThumbIcon,
                materialTapTargetSize: MaterialTapTargetSize ?? Plumix.Material.MaterialTapTargetSize.ShrinkWrap,
                overlayColor: OverlayColor,
                splashRadius: SplashRadius,
                onFocusChange: OnFocusChange,
                autofocus: Autofocus);
        control = new ExcludeFocus(child: control);

        var affinity = ControlAffinity
                       ?? ListTileTheme.Of(context).ControlAffinity
                       ?? ListTileControlAffinity.Platform;
        var leading = affinity == ListTileControlAffinity.Leading ? control : Secondary;
        var trailing = affinity == ListTileControlAffinity.Leading ? Secondary : control;
        var selectedStates = Selected ? MaterialState.Selected : MaterialState.None;
        var selectedColor = ActiveThumbColor
                            ?? ActiveColor
                            ?? SwitchTheme.Of(context).ThumbColor?.Resolve(selectedStates)
                            ?? Theme.Of(context).ColorScheme.Secondary;

        var tile = new ListTile(
                selectedColor: selectedColor,
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
                focusNode: FocusNode,
                mouseCursor: MouseCursor,
                onFocusChange: OnFocusChange,
                enableFeedback: EnableFeedback,
                horizontalTitleGap: HorizontalTitleGap,
                minVerticalPadding: MinVerticalPadding,
                minLeadingWidth: MinLeadingWidth,
                minTileHeight: MinTileHeight,
                hoverColor: HoverColor,
                visualDensity: VisualDensity,
                internalAddSemanticForOnTap: InternalAddSemanticForOnTap,
                statesController: StatesController);
        var semanticFlags = Value ? SemanticsFlags.IsChecked : SemanticsFlags.None;
        if (OnChanged is not null)
        {
            semanticFlags |= SemanticsFlags.IsEnabled;
        }

        return new Semantics(
            child: new MergeSemantics(child: tile),
            flags: semanticFlags,
            onTap: OnChanged is not null ? () => OnChanged(!Value) : null,
            container: true);
    }
}
