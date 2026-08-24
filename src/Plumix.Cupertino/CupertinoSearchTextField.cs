using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/search_field.dart

/// <summary>A Cupertino text field styled and configured like UIKit's search field.</summary>
public sealed class CupertinoSearchTextField : StatefulWidget
{
    public CupertinoSearchTextField(
        TextEditingController? controller = null,
        Action<string>? onChanged = null,
        Action<string>? onSubmitted = null,
        TextStyle? style = null,
        string? placeholder = null,
        TextStyle? placeholderStyle = null,
        BoxDecoration? decoration = null,
        CupertinoDynamicColor? backgroundColor = null,
        BorderRadius? borderRadius = null,
        TextInputType? keyboardType = null,
        EdgeInsetsGeometry? padding = null,
        CupertinoDynamicColor? itemColor = null,
        double itemSize = 20.0,
        EdgeInsetsGeometry? prefixInsets = null,
        Widget? prefixIcon = null,
        EdgeInsetsGeometry? suffixInsets = null,
        Icon? suffixIcon = null,
        OverlayVisibilityMode suffixMode = OverlayVisibilityMode.Editing,
        Action? onSuffixTap = null,
        string? restorationId = null,
        FocusNode? focusNode = null,
        SmartQuotesType? smartQuotesType = null,
        SmartDashesType? smartDashesType = null,
        bool enableIMEPersonalizedLearning = true,
        bool autofocus = false,
        Action? onTap = null,
        bool autocorrect = true,
        bool? enabled = null,
        double cursorWidth = 2.0,
        double? cursorHeight = null,
        Radius? cursorRadius = null,
        bool cursorOpacityAnimates = true,
        CupertinoDynamicColor? cursorColor = null,
        Key? key = null) : base(key)
    {
        if (decoration is not null && backgroundColor is not null)
        {
            throw new ArgumentException(
                "Cannot provide both a background color and a decoration.",
                nameof(backgroundColor));
        }
        if (decoration is not null && borderRadius is not null)
        {
            throw new ArgumentException(
                "Cannot provide both a border radius and a decoration.",
                nameof(borderRadius));
        }

        Controller = controller;
        OnChanged = onChanged;
        OnSubmitted = onSubmitted;
        Style = style;
        Placeholder = placeholder;
        PlaceholderStyle = placeholderStyle;
        Decoration = decoration;
        BackgroundColor = backgroundColor;
        BorderRadius = borderRadius;
        KeyboardType = keyboardType ?? TextInputType.Text;
        Padding = padding ?? EdgeInsetsGeometry.DirectionalOnly(start: 5.5, top: 8.0, end: 5.5, bottom: 8.0);
        ItemColor = itemColor ?? CupertinoColors.SecondaryLabel;
        ItemSize = itemSize;
        PrefixInsets = prefixInsets
                       ?? EdgeInsetsGeometry.DirectionalOnly(start: 6.0, top: 8.0, bottom: 8.0);
        PrefixIcon = prefixIcon ?? new Icon(CupertinoIcons.Search);
        SuffixInsets = suffixInsets
                       ?? EdgeInsetsGeometry.DirectionalOnly(top: 8.0, end: 5.0, bottom: 8.0);
        SuffixIcon = suffixIcon ?? new Icon(CupertinoIcons.XmarkCircleFill);
        SuffixMode = suffixMode;
        OnSuffixTap = onSuffixTap;
        RestorationId = restorationId;
        FocusNode = focusNode;
        SmartQuotesType = smartQuotesType;
        SmartDashesType = smartDashesType;
        EnableIMEPersonalizedLearning = enableIMEPersonalizedLearning;
        Autofocus = autofocus;
        OnTap = onTap;
        Autocorrect = autocorrect;
        Enabled = enabled;
        CursorWidth = cursorWidth;
        CursorHeight = cursorHeight;
        CursorRadius = cursorRadius ?? Radius.Circular(2.0);
        CursorOpacityAnimates = cursorOpacityAnimates;
        CursorColor = cursorColor;
    }

    public TextEditingController? Controller { get; }

    public Action<string>? OnChanged { get; }

    public Action<string>? OnSubmitted { get; }

    public TextStyle? Style { get; }

    public string? Placeholder { get; }

    public TextStyle? PlaceholderStyle { get; }

    public BoxDecoration? Decoration { get; }

    public CupertinoDynamicColor? BackgroundColor { get; }

    public BorderRadius? BorderRadius { get; }

    public TextInputType? KeyboardType { get; }

    public EdgeInsetsGeometry Padding { get; }

    public CupertinoDynamicColor ItemColor { get; }

    public double ItemSize { get; }

    public EdgeInsetsGeometry PrefixInsets { get; }

    public Widget PrefixIcon { get; }

    public EdgeInsetsGeometry SuffixInsets { get; }

    public Icon SuffixIcon { get; }

    public OverlayVisibilityMode SuffixMode { get; }

    public Action? OnSuffixTap { get; }

    public string? RestorationId { get; }

    public FocusNode? FocusNode { get; }

    public bool Autofocus { get; }

    public Action? OnTap { get; }

    public bool Autocorrect { get; }

    public SmartQuotesType? SmartQuotesType { get; }

    public SmartDashesType? SmartDashesType { get; }

    public bool EnableIMEPersonalizedLearning { get; }

    public bool? Enabled { get; }

    public double CursorWidth { get; }

    public double? CursorHeight { get; }

    public Radius CursorRadius { get; }

    public bool CursorOpacityAnimates { get; }

    public CupertinoDynamicColor? CursorColor { get; }

    public override State CreateState() => new CupertinoSearchTextFieldState();
}

internal sealed class CupertinoSearchTextFieldState : RestorationState
{
    private const double MinHeightBeforeTotalTransparency = 4.0 / 5.0;
    private const double MaxPrefixIconSize = 30.0;
    private static readonly BorderRadius DefaultBorderRadius = BorderRadius.Circular(9.0);

    private RestorableTextEditingController? _controller;
    private FocusNode? _focusNode;
    private ScrollNotificationObserverState? _scrollNotificationObserver;
    private double _scaledIconSize;
    private double _fadeExtent;

    private CupertinoSearchTextField Current => (CupertinoSearchTextField)StateWidget;

    private TextEditingController EffectiveController => Current.Controller ?? _controller!.Value;

    private FocusNode EffectiveFocusNode => Current.FocusNode ?? _focusNode!;

    protected override string? RestorationId => Current.RestorationId;

    public override void InitState()
    {
        if (Current.Controller is null)
        {
            CreateLocalController();
        }
        if (Current.FocusNode is null)
        {
            _focusNode = new FocusNode();
        }
        EffectiveFocusNode.AddListener(HandleFocusChanged);
    }

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        _scrollNotificationObserver?.RemoveListener(HandleScrollNotification);
        _scrollNotificationObserver = ScrollNotificationObserver.MaybeOf(Context);
        _scrollNotificationObserver?.AddListener(HandleScrollNotification);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var old = (CupertinoSearchTextField)oldWidget;
        if (Current.Controller is null && old.Controller is not null)
        {
            CreateLocalController(old.Controller.Value);
        }
        else if (Current.Controller is not null && old.Controller is null)
        {
            UnregisterFromRestoration(_controller!);
            _controller!.Dispose();
            _controller = null;
        }

        if (!ReferenceEquals(Current.FocusNode, old.FocusNode))
        {
            FocusNode oldEffectiveFocusNode = old.FocusNode ?? _focusNode!;
            oldEffectiveFocusNode.RemoveListener(HandleFocusChanged);
            if (Current.FocusNode is null && old.FocusNode is not null)
            {
                _focusNode = new FocusNode();
            }
            else if (Current.FocusNode is not null && old.FocusNode is null)
            {
                _focusNode!.Dispose();
                _focusNode = null;
            }
            EffectiveFocusNode.AddListener(HandleFocusChanged);
        }
    }

    protected override void RestoreState(RestorationBucket? oldBucket, bool initialRestore)
    {
        if (_controller is not null)
        {
            RegisterController();
        }
    }

    public override void Dispose()
    {
        _scrollNotificationObserver?.RemoveListener(HandleScrollNotification);
        _scrollNotificationObserver = null;
        EffectiveFocusNode.RemoveListener(HandleFocusChanged);
        if (Current.FocusNode is null)
        {
            _focusNode?.Dispose();
        }
        if (Current.Controller is null)
        {
            _controller?.Dispose();
        }
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        string placeholder = Current.Placeholder
                             ?? CupertinoLocalizations.Of(context).SearchTextFieldPlaceholderLabel;
        Color defaultPlaceholderColor = CupertinoDynamicColor.Resolve(CupertinoColors.SecondaryLabel, context);
        byte placeholderAlpha = (byte)Math.Clamp(
            (int)Math.Round(defaultPlaceholderColor.A * (1.0 - _fadeExtent)),
            0,
            255);
        TextStyle placeholderStyle = Current.PlaceholderStyle ?? new TextStyle(
            Color: Color.FromArgb(
                placeholderAlpha,
                defaultPlaceholderColor.R,
                defaultPlaceholderColor.G,
                defaultPlaceholderColor.B));

        _scaledIconSize = MediaQuery.TextScalerOf(context).Scale(Current.ItemSize);

        CupertinoDynamicColor backgroundColor = Current.BackgroundColor ?? CupertinoColors.TertiarySystemFill;
        BoxDecoration decoration = Current.Decoration ?? new BoxDecoration(
            Color: CupertinoDynamicColor.Resolve(backgroundColor, context),
            BorderRadius: Current.BorderRadius ?? DefaultBorderRadius);

        Color iconColor = CupertinoDynamicColor.Resolve(Current.ItemColor, context);
        var suffixIconThemeData = new IconThemeData(Color: iconColor, Size: _scaledIconSize);
        var prefixIconThemeData = new IconThemeData(
            Color: iconColor,
            Size: _scaledIconSize >= MaxPrefixIconSize && EffectiveFocusNode.HasFocus
                ? 0.0
                : _scaledIconSize);

        Widget prefix = new Opacity(
            opacity: 1.0 - _fadeExtent,
            child: new Padding(
                AnimatedInsets(context, Current.PrefixInsets),
                new IconTheme(prefixIconThemeData, Current.PrefixIcon)));

        Widget suffix = new Opacity(
            opacity: 1.0 - _fadeExtent,
            child: new Padding(
                AnimatedInsets(context, Current.SuffixInsets),
                new CupertinoButton(
                    child: new IconTheme(suffixIconThemeData, Current.SuffixIcon),
                    onPressed: Current.OnSuffixTap ?? DefaultOnSuffixTap,
                    minSize: 0.0,
                    padding: EdgeInsetsGeometry.Zero)));

        return new CupertinoTextField(
            controller: EffectiveController,
            decoration: decoration,
            style: Current.Style,
            prefix: prefix,
            suffix: suffix,
            keyboardType: Current.KeyboardType,
            onTap: Current.OnTap,
            enabled: Current.Enabled ?? true,
            cursorWidth: Current.CursorWidth,
            cursorHeight: Current.CursorHeight,
            cursorRadius: Current.CursorRadius,
            cursorOpacityAnimates: Current.CursorOpacityAnimates,
            cursorColor: Current.CursorColor,
            suffixMode: Current.SuffixMode,
            placeholder: placeholder,
            placeholderStyle: placeholderStyle,
            padding: AnimatedInsets(context, Current.Padding),
            onChanged: Current.OnChanged,
            onSubmitted: Current.OnSubmitted,
            focusNode: EffectiveFocusNode,
            autofocus: Current.Autofocus,
            autocorrect: Current.Autocorrect,
            smartQuotesType: Current.SmartQuotesType,
            smartDashesType: Current.SmartDashesType,
            enableIMEPersonalizedLearning: Current.EnableIMEPersonalizedLearning,
            textInputAction: TextInputActionType.Search);
    }

    private void RegisterController()
    {
        RegisterForRestoration(_controller!, "controller");
    }

    private void CreateLocalController(TextEditingValue? value = null)
    {
        _controller = value is null
            ? new RestorableTextEditingController()
            : RestorableTextEditingController.FromValue(value.Value);
        if (!RestorePending)
        {
            RegisterController();
        }
    }

    private void DefaultOnSuffixTap()
    {
        bool textChanged = EffectiveController.Text.Length > 0;
        EffectiveController.Clear();
        if (textChanged)
        {
            Current.OnChanged?.Invoke(EffectiveController.Text);
        }
    }

    private void HandleFocusChanged()
    {
        if (Mounted)
        {
            SetState(() => { });
        }
    }

    private void HandleScrollNotification(ScrollNotification notification)
    {
        if (notification is not ScrollUpdateNotification)
        {
            return;
        }

        double currentHeight = Context.Size?.Height ?? 0.0;
        SetState(() => _fadeExtent = CalculateScrollOpacity(
            currentHeight,
            _scaledIconSize + Math.Max(Current.PrefixInsets.Vertical, Current.SuffixInsets.Vertical)));
    }

    private static double CalculateScrollOpacity(double currentHeight, double maxHeight)
    {
        double thresholdHeight = maxHeight * MinHeightBeforeTotalTransparency;
        if (currentHeight >= maxHeight)
        {
            return 0.0;
        }
        if (currentHeight <= thresholdHeight)
        {
            return 1.0;
        }

        double range = maxHeight - thresholdHeight;
        double progress = (currentHeight - thresholdHeight) / range;
        return 1.0 - progress;
    }

    private EdgeInsetsGeometry AnimatedInsets(BuildContext context, EdgeInsetsGeometry insets)
    {
        Thickness currentInsets = insets.Resolve(Directionality.Of(context));
        var collapsedInsets = new Thickness(
            currentInsets.Left,
            currentInsets.Top / 2.0,
            currentInsets.Right,
            currentInsets.Bottom);
        EdgeInsetsGeometry? animatedInsets = EdgeInsetsGeometry.Lerp(insets, collapsedInsets, _fadeExtent);
        return animatedInsets ?? insets;
    }
}
