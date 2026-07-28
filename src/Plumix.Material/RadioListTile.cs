using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/radio_list_tile.dart
public sealed class RadioListTile<T> : StatefulWidget
{
    private readonly bool _adaptive;

    public RadioListTile(
        T value,
        T? groupValue = default,
        Action<T?>? onChanged = null,
        MouseCursor? mouseCursor = null,
        bool toggleable = false,
        Color? activeColor = null,
        MaterialStateProperty<Color?>? fillColor = null,
        Color? hoverColor = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        double? splashRadius = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        Widget? title = null,
        Widget? subtitle = null,
        bool? isThreeLine = null,
        bool? dense = null,
        Widget? secondary = null,
        bool selected = false,
        ListTileControlAffinity? controlAffinity = null,
        bool autofocus = false,
        Thickness? contentPadding = null,
        BorderRadius? shape = null,
        Color? tileColor = null,
        Color? selectedTileColor = null,
        FocusNode? focusNode = null,
        Action<bool>? onFocusChange = null,
        bool? enableFeedback = null,
        double? horizontalTitleGap = null,
        double? minVerticalPadding = null,
        double? minLeadingWidth = null,
        double? minTileHeight = null,
        double radioScaleFactor = 1.0,
        bool? enabled = null,
        MaterialStateProperty<Color?>? radioBackgroundColor = null,
        BorderSide? radioSide = null,
        MaterialStateProperty<double?>? radioInnerRadius = null,
        Key? key = null) : this(
            value,
            groupValue,
            onChanged,
            mouseCursor,
            toggleable,
            activeColor,
            fillColor,
            hoverColor,
            overlayColor,
            splashRadius,
            materialTapTargetSize,
            title,
            subtitle,
            isThreeLine,
            dense,
            secondary,
            selected,
            controlAffinity,
            autofocus,
            contentPadding,
            shape,
            tileColor,
            selectedTileColor,
            focusNode,
            onFocusChange,
            enableFeedback,
            horizontalTitleGap,
            minVerticalPadding,
            minLeadingWidth,
            minTileHeight,
            radioScaleFactor,
            enabled,
            useCupertinoCheckmarkStyle: false,
            radioBackgroundColor,
            radioSide,
            radioInnerRadius,
            adaptive: false,
            key)
    {
    }

    private RadioListTile(
        T value,
        T? groupValue,
        Action<T?>? onChanged,
        MouseCursor? mouseCursor,
        bool toggleable,
        Color? activeColor,
        MaterialStateProperty<Color?>? fillColor,
        Color? hoverColor,
        MaterialStateProperty<Color?>? overlayColor,
        double? splashRadius,
        MaterialTapTargetSize? materialTapTargetSize,
        Widget? title,
        Widget? subtitle,
        bool? isThreeLine,
        bool? dense,
        Widget? secondary,
        bool selected,
        ListTileControlAffinity? controlAffinity,
        bool autofocus,
        Thickness? contentPadding,
        BorderRadius? shape,
        Color? tileColor,
        Color? selectedTileColor,
        FocusNode? focusNode,
        Action<bool>? onFocusChange,
        bool? enableFeedback,
        double? horizontalTitleGap,
        double? minVerticalPadding,
        double? minLeadingWidth,
        double? minTileHeight,
        double radioScaleFactor,
        bool? enabled,
        bool useCupertinoCheckmarkStyle,
        MaterialStateProperty<Color?>? radioBackgroundColor,
        BorderSide? radioSide,
        MaterialStateProperty<double?>? radioInnerRadius,
        bool adaptive,
        Key? key) : base(key)
    {
        if (isThreeLine == true && subtitle is null)
        {
            throw new ArgumentException("RadioListTile with isThreeLine=true requires a subtitle.", nameof(isThreeLine));
        }

        if (!double.IsFinite(radioScaleFactor) || radioScaleFactor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radioScaleFactor), "Radio scale factor must be finite and positive.");
        }

        Value = value;
        GroupValue = groupValue;
        OnChanged = onChanged;
        MouseCursor = mouseCursor;
        Toggleable = toggleable;
        ActiveColor = activeColor;
        FillColor = fillColor;
        HoverColor = hoverColor;
        OverlayColor = overlayColor;
        SplashRadius = splashRadius;
        MaterialTapTargetSize = materialTapTargetSize;
        Title = title;
        Subtitle = subtitle;
        IsThreeLine = isThreeLine;
        Dense = dense;
        Secondary = secondary;
        Selected = selected;
        ControlAffinity = controlAffinity;
        Autofocus = autofocus;
        ContentPadding = contentPadding;
        Shape = shape;
        TileColor = tileColor;
        SelectedTileColor = selectedTileColor;
        FocusNode = focusNode;
        OnFocusChange = onFocusChange;
        EnableFeedback = enableFeedback;
        HorizontalTitleGap = horizontalTitleGap;
        MinVerticalPadding = minVerticalPadding;
        MinLeadingWidth = minLeadingWidth;
        MinTileHeight = minTileHeight;
        RadioScaleFactor = radioScaleFactor;
        Enabled = enabled;
        UseCupertinoCheckmarkStyle = useCupertinoCheckmarkStyle;
        RadioBackgroundColor = radioBackgroundColor;
        RadioSide = radioSide;
        RadioInnerRadius = radioInnerRadius;
        _adaptive = adaptive;
    }

    public T Value { get; }
    public T? GroupValue { get; }
    public Action<T?>? OnChanged { get; }
    public MouseCursor? MouseCursor { get; }
    public bool Toggleable { get; }
    public Color? ActiveColor { get; }
    public MaterialStateProperty<Color?>? FillColor { get; }
    public Color? HoverColor { get; }
    public MaterialStateProperty<Color?>? OverlayColor { get; }
    public double? SplashRadius { get; }
    public MaterialTapTargetSize? MaterialTapTargetSize { get; }
    public Widget? Title { get; }
    public Widget? Subtitle { get; }
    public bool? IsThreeLine { get; }
    public bool? Dense { get; }
    public Widget? Secondary { get; }
    public bool Selected { get; }
    public ListTileControlAffinity? ControlAffinity { get; }
    public bool Autofocus { get; }
    public Thickness? ContentPadding { get; }
    public BorderRadius? Shape { get; }
    public Color? TileColor { get; }
    public Color? SelectedTileColor { get; }
    public FocusNode? FocusNode { get; }
    public Action<bool>? OnFocusChange { get; }
    public bool? EnableFeedback { get; }
    public double? HorizontalTitleGap { get; }
    public double? MinVerticalPadding { get; }
    public double? MinLeadingWidth { get; }
    public double? MinTileHeight { get; }
    public double RadioScaleFactor { get; }
    public bool? Enabled { get; }
    public bool UseCupertinoCheckmarkStyle { get; }
    public MaterialStateProperty<Color?>? RadioBackgroundColor { get; }
    public BorderSide? RadioSide { get; }
    public MaterialStateProperty<double?>? RadioInnerRadius { get; }

    public static RadioListTile<T> Adaptive(
        T value,
        T? groupValue = default,
        Action<T?>? onChanged = null,
        MouseCursor? mouseCursor = null,
        bool toggleable = false,
        Color? activeColor = null,
        MaterialStateProperty<Color?>? fillColor = null,
        Color? hoverColor = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        double? splashRadius = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        Widget? title = null,
        Widget? subtitle = null,
        bool? isThreeLine = null,
        bool? dense = null,
        Widget? secondary = null,
        bool selected = false,
        ListTileControlAffinity? controlAffinity = null,
        bool autofocus = false,
        Thickness? contentPadding = null,
        BorderRadius? shape = null,
        Color? tileColor = null,
        Color? selectedTileColor = null,
        FocusNode? focusNode = null,
        Action<bool>? onFocusChange = null,
        bool? enableFeedback = null,
        double? horizontalTitleGap = null,
        double? minVerticalPadding = null,
        double? minLeadingWidth = null,
        double? minTileHeight = null,
        double radioScaleFactor = 1.0,
        bool? enabled = null,
        bool useCupertinoCheckmarkStyle = false,
        MaterialStateProperty<Color?>? radioBackgroundColor = null,
        BorderSide? radioSide = null,
        MaterialStateProperty<double?>? radioInnerRadius = null,
        Key? key = null)
    {
        return new RadioListTile<T>(
            value,
            groupValue,
            onChanged,
            mouseCursor,
            toggleable,
            activeColor,
            fillColor,
            hoverColor,
            overlayColor,
            splashRadius,
            materialTapTargetSize,
            title,
            subtitle,
            isThreeLine,
            dense,
            secondary,
            selected,
            controlAffinity,
            autofocus,
            contentPadding,
            shape,
            tileColor,
            selectedTileColor,
            focusNode,
            onFocusChange,
            enableFeedback,
            horizontalTitleGap,
            minVerticalPadding,
            minLeadingWidth,
            minTileHeight,
            radioScaleFactor,
            enabled,
            useCupertinoCheckmarkStyle,
            radioBackgroundColor,
            radioSide,
            radioInnerRadius,
            adaptive: true,
            key);
    }

    public override State CreateState()
    {
        return new RadioListTileState();
    }

    private Widget BuildContent(
        BuildContext context,
        RadioGroupRegistry<T>? groupRegistry,
        RadioGroupRegistry<T> controlRegistry,
        FocusNode focusNode,
        bool isEnabled)
    {
        T? effectiveGroupValue = groupRegistry is not null ? groupRegistry.GroupValue : GroupValue;
        bool isChecked = EqualityComparer<T?>.Default.Equals(Value, effectiveGroupValue);
        Widget control = _adaptive
            ? Radio<T>.Adaptive(
                value: Value,
                groupValue: effectiveGroupValue,
                toggleable: Toggleable,
                activeColor: ActiveColor,
                fillColor: FillColor,
                overlayColor: OverlayColor,
                hoverColor: HoverColor,
                materialTapTargetSize: MaterialTapTargetSize ?? Plumix.Material.MaterialTapTargetSize.ShrinkWrap,
                backgroundColor: RadioBackgroundColor,
                side: RadioSide,
                innerRadius: RadioInnerRadius,
                splashRadius: SplashRadius,
                autofocus: Autofocus,
                enabled: isEnabled,
                groupRegistry: controlRegistry,
                useCupertinoCheckmarkStyle: UseCupertinoCheckmarkStyle)
            : new Radio<T>(
                value: Value,
                groupValue: effectiveGroupValue,
                toggleable: Toggleable,
                activeColor: ActiveColor,
                fillColor: FillColor,
                overlayColor: OverlayColor,
                hoverColor: HoverColor,
                materialTapTargetSize: MaterialTapTargetSize ?? Plumix.Material.MaterialTapTargetSize.ShrinkWrap,
                backgroundColor: RadioBackgroundColor,
                side: RadioSide,
                innerRadius: RadioInnerRadius,
                splashRadius: SplashRadius,
                autofocus: Autofocus,
                enabled: isEnabled,
                groupRegistry: controlRegistry);
        control = new ExcludeFocus(child: control);

        if (RadioScaleFactor != 1.0)
        {
            double center = Radio<T>.Width / 2.0;
            var scale = new Matrix(RadioScaleFactor, 0, 0, RadioScaleFactor, 0, 0);
            control = new Plumix.Widgets.Transform(
                transform: Matrix.CreateTranslation(center, center)
                           * scale
                           * Matrix.CreateTranslation(-center, -center),
                child: control);
        }

        var affinity = ControlAffinity
                       ?? ListTileTheme.Of(context).ControlAffinity
                       ?? ListTileControlAffinity.Platform;
        bool controlIsLeading = affinity is ListTileControlAffinity.Leading or ListTileControlAffinity.Platform;
        var leading = controlIsLeading ? control : Secondary;
        var trailing = controlIsLeading ? Secondary : control;
        var selectedStates = Selected ? MaterialState.Selected : MaterialState.None;
        var selectedColor = ActiveColor
                            ?? RadioTheme.Of(context).FillColor?.Resolve(selectedStates)
                            ?? Theme.Of(context).SecondaryColor;

        var tile = new ListTile(
            selectedColor: selectedColor,
            leading: leading,
            title: Title,
            subtitle: Subtitle,
            trailing: trailing,
            isThreeLine: IsThreeLine,
            dense: Dense,
            enabled: isEnabled,
            shape: Shape,
            tileColor: TileColor,
            selectedTileColor: SelectedTileColor,
            onTap: isEnabled ? HandleTileTap : null,
            selected: Selected,
            autofocus: Autofocus,
            contentPadding: ContentPadding,
            focusNode: focusNode,
            mouseCursor: MouseCursor,
            onFocusChange: OnFocusChange,
            enableFeedback: EnableFeedback,
            horizontalTitleGap: HorizontalTitleGap,
            minVerticalPadding: MinVerticalPadding,
            minLeadingWidth: MinLeadingWidth,
            minTileHeight: MinTileHeight);

        var flags = isChecked ? SemanticsFlags.IsChecked : SemanticsFlags.None;
        if (isEnabled)
        {
            flags |= SemanticsFlags.IsEnabled;
        }

        return new Semantics(
            child: new MergeSemantics(child: tile),
            flags: flags,
            onTap: isEnabled ? HandleTileTap : null,
            container: true);

        void HandleChange(T? value)
        {
            groupRegistry?.OnChanged(value);
            OnChanged?.Invoke(value);
        }

        void HandleTileTap()
        {
            if (!Toggleable && isChecked)
            {
                return;
            }

            HandleChange(isChecked ? default : Value);
        }
    }

    private sealed class RadioListTileState : State, RadioClient<T>
    {
        private FocusNode? _focusNode;
        private bool _ownsFocusNode;
        private RadioGroupRegistry<T>? _registry;
        private InternalRadioRegistry? _internalRegistry;

        private RadioListTile<T> CurrentWidget => (RadioListTile<T>)StateWidget;

        public bool Tristate => CurrentWidget.Toggleable;

        public T RadioValue => CurrentWidget.Value;

        public bool Enabled => CurrentWidget.Enabled
                               ?? (CurrentWidget.OnChanged is not null || _registry is not null);

        public FocusNode FocusNode => _focusNode!;

        public override void InitState()
        {
            AttachFocusNode(CurrentWidget.FocusNode);
            _internalRegistry = new InternalRadioRegistry(this);
        }

        public override void DidChangeDependencies()
        {
            SetRegistry(RadioGroup<T>.MaybeOf(Context));
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldTile = (RadioListTile<T>)oldWidget;
            if (!ReferenceEquals(oldTile.FocusNode, CurrentWidget.FocusNode))
            {
                SetRegistry(null);
                DetachFocusNode(disposeOwned: true);
                AttachFocusNode(CurrentWidget.FocusNode);
            }

            SetRegistry(RadioGroup<T>.MaybeOf(Context));
        }

        public override Widget Build(BuildContext context)
        {
            if (CurrentWidget.Enabled == true
                && CurrentWidget.OnChanged is null
                && _registry is null)
            {
                throw new InvalidOperationException(
                    "An enabled RadioListTile requires onChanged or an ancestor RadioGroup.");
            }

            return CurrentWidget.BuildContent(
                context,
                _registry,
                _internalRegistry!,
                _focusNode!,
                Enabled);
        }

        public override void Dispose()
        {
            SetRegistry(null);
            DetachFocusNode(disposeOwned: true);
        }

        private void SetRegistry(RadioGroupRegistry<T>? registry)
        {
            if (ReferenceEquals(_registry, registry))
            {
                return;
            }

            _registry?.UnregisterClient(this);
            _registry = registry;
            _registry?.RegisterClient(this);
        }

        private void AttachFocusNode(FocusNode? focusNode)
        {
            _focusNode = focusNode ?? new FocusNode();
            _ownsFocusNode = focusNode is null;
        }

        private void DetachFocusNode(bool disposeOwned)
        {
            if (_focusNode is null)
            {
                return;
            }

            if (disposeOwned && _ownsFocusNode)
            {
                _focusNode.Dispose();
            }

            _focusNode = null;
            _ownsFocusNode = false;
        }

        private sealed class InternalRadioRegistry : RadioGroupRegistry<T>
        {
            private readonly RadioListTileState _state;

            public InternalRadioRegistry(RadioListTileState state)
            {
                _state = state;
            }

            public override T? GroupValue => _state._registry is null
                ? _state.CurrentWidget.GroupValue
                : _state._registry.GroupValue;

            public override Action<T?> OnChanged => HandleChange;

            public override void RegisterClient(RadioClient<T> radio)
            {
            }

            public override void UnregisterClient(RadioClient<T> radio)
            {
            }

            private void HandleChange(T? value)
            {
                _state._registry?.OnChanged(value);
                _state.CurrentWidget.OnChanged?.Invoke(value);
            }
        }
    }
}
