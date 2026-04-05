using Avalonia;
using Avalonia.Media;
using Flutter.Foundation;
using Flutter.Rendering;
using Flutter.UI;
using Flutter.Widgets;

namespace Flutter.Cupertino;

// Dart parity source (reference): flutter/packages/flutter/lib/src/cupertino/checkbox.dart (adapted)

public sealed class CupertinoCheckbox : StatefulWidget
{
    private const double DefaultPressedOverlayOpacity = 0.15;
    private const double FocusOpacity = 0.80;
    private const double FocusLightness = 0.69;
    private const double FocusSaturation = 0.835;

    private static readonly Color DisabledCheckColorLight = Color.FromArgb(64, 0, 0, 0);
    private static readonly Color DisabledCheckColorDark = Color.FromArgb(64, 255, 255, 255);
    private static readonly Color DisabledBorderColor = Color.FromArgb(13, 0, 0, 0);
    private static readonly Color DefaultBorderColorLight = Color.FromArgb(255, 209, 209, 214);
    private static readonly Color DefaultBorderColorDark = Color.FromArgb(50, 128, 128, 128);
    private static readonly Color DefaultFillColorLight = Color.FromArgb(255, 0, 122, 255);
    private static readonly Color DefaultFillColorDark = Color.FromArgb(255, 50, 100, 215);
    private static readonly Color DefaultCheckColorDark = Color.FromArgb(255, 222, 232, 248);

    public const double Width = 14.0;

    public CupertinoCheckbox(
        bool? value,
        Action<bool?>? onChanged,
        bool tristate = false,
        Color? activeColor = null,
        Color? checkColor = null,
        Color? focusColor = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        BorderSide? side = null,
        BorderRadius? shape = null,
        Size? tapTargetSize = null,
        bool isDark = false,
        string? semanticLabel = null,
        Key? key = null) : base(key)
    {
        if (!tristate && value is null)
        {
            throw new ArgumentException("Checkbox value cannot be null when tristate is false.", nameof(value));
        }

        Value = value;
        OnChanged = onChanged;
        Tristate = tristate;
        ActiveColor = activeColor;
        CheckColor = checkColor;
        FocusColor = focusColor;
        FocusNode = focusNode;
        Autofocus = autofocus;
        Side = side;
        Shape = shape;
        TapTargetSize = tapTargetSize;
        IsDark = isDark;
        SemanticLabel = semanticLabel;
    }

    public bool? Value { get; }

    public Action<bool?>? OnChanged { get; }

    public bool Tristate { get; }

    public Color? ActiveColor { get; }

    public Color? CheckColor { get; }

    public Color? FocusColor { get; }

    public FocusNode? FocusNode { get; }

    public bool Autofocus { get; }

    public BorderSide? Side { get; }

    public BorderRadius? Shape { get; }

    public Size? TapTargetSize { get; }

    public bool IsDark { get; }

    public string? SemanticLabel { get; }

    public override State CreateState()
    {
        return new CupertinoCheckboxState();
    }

    private sealed class CupertinoCheckboxState : State
    {
        private AnimationController? _transitionController;
        private bool? _previousValue;
        private bool _isTransitioning;
        private double _transitionProgress = 1.0;

        private bool _isPressed;
        private bool _hasFocus;
        private bool _isHovered;

        private FocusNode? _focusNode;
        private bool _ownsFocusNode;

        private CupertinoCheckbox CurrentWidget => (CupertinoCheckbox)StateWidget;

        private bool Enabled => CurrentWidget.OnChanged is not null;

        public override void InitState()
        {
            _previousValue = CurrentWidget.Value;
            _transitionController = new AnimationController(TimeSpan.FromMilliseconds(200))
            {
                Curve = Curves.EaseInOut
            };
            _transitionController.Changed += HandleTransitionTick;
            _transitionController.Completed += HandleTransitionCompleted;

            AttachFocusNode(CurrentWidget.FocusNode);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldCheckbox = (CupertinoCheckbox)oldWidget;
            if (oldCheckbox.Value != CurrentWidget.Value)
            {
                _previousValue = oldCheckbox.Value;
                StartTransition();
            }

            if (!ReferenceEquals(oldCheckbox.FocusNode, CurrentWidget.FocusNode))
            {
                DetachFocusNode(disposeOwned: true);
                AttachFocusNode(CurrentWidget.FocusNode);
            }

            if (!Enabled && _focusNode is { HasFocus: true })
            {
                _focusNode.Unfocus();
            }

            if (!Enabled)
            {
                _isPressed = false;
                _isHovered = false;
            }
        }

        public override void Dispose()
        {
            if (_transitionController is not null)
            {
                _transitionController.Changed -= HandleTransitionTick;
                _transitionController.Completed -= HandleTransitionCompleted;
                _transitionController.Dispose();
                _transitionController = null;
            }

            DetachFocusNode(disposeOwned: true);
        }

        public override Widget Build(BuildContext context)
        {
            var shape = CurrentWidget.Shape ?? Flutter.Rendering.BorderRadius.Circular(4.0);
            var targetSize = ResolveTapTargetSize();
            var selected = IsSelected(CurrentWidget.Value);
            var activeColor = ResolveActiveColor();
            var fillColor = ResolveFillColor(selected, activeColor);
            var checkColor = ResolveCheckColor(selected);
            var borderSide = ResolveBorderSide(selected);
            var overlayColor = ResolveOverlayColor(activeColor);
            var focusRingColor = ResolveFocusRingColor(activeColor);

            var indicator = BuildIndicator(checkColor);
            var body = BuildCheckboxBody(
                shape,
                fillColor,
                borderSide,
                overlayColor,
                indicator);

            if (_hasFocus && Enabled)
            {
                body = new DecoratedBox(
                    decoration: new BoxDecoration(
                        Border: new BorderSide(focusRingColor, 3.5),
                        BorderRadius: Flutter.Rendering.BorderRadius.Circular(shape.Radius + 1.0)),
                    child: new Padding(
                        new Thickness(1),
                        body));
            }

            Widget result = new SizedBox(
                width: targetSize.Width,
                height: targetSize.Height,
                child: new Center(child: body));

            if (Enabled)
            {
                result = new GestureDetector(
                    behavior: HitTestBehavior.Opaque,
                    onTap: HandleTap,
                    child: result);

                result = new Listener(
                    behavior: HitTestBehavior.Opaque,
                    onPointerDown: HandlePointerDown,
                    onPointerUp: HandlePointerUp,
                    onPointerCancel: HandlePointerCancel,
                    onPointerEnter: _ => SetHovered(true),
                    onPointerExit: _ => SetHovered(false),
                    child: result);
            }

            return new Focus(
                focusNode: _focusNode,
                autofocus: CurrentWidget.Autofocus,
                canRequestFocus: Enabled,
                onKeyEvent: HandleKeyEvent,
                child: result);
        }

        private Widget BuildCheckboxBody(
            BorderRadius shape,
            Color fillColor,
            BorderSide? borderSide,
            Color? overlayColor,
            Widget indicator)
        {
            var layers = new List<Widget>
            {
                new DecoratedBox(
                    decoration: new BoxDecoration(
                        Color: fillColor,
                        Border: borderSide,
                        BorderRadius: shape),
                    child: new SizedBox(width: Width, height: Width)),
            };

            if (overlayColor.HasValue && overlayColor.Value.A > 0)
            {
                layers.Add(new Container(
                    width: Width,
                    height: Width,
                    color: overlayColor.Value));
            }

            layers.Add(indicator);

            return new SizedBox(
                width: Width,
                height: Width,
                child: new ClipRRect(
                    borderRadius: shape,
                    child: new Stack(
                        alignment: Alignment.Center,
                        children: layers)));
        }

        private Widget BuildIndicator(Color color)
        {
            if (!_isTransitioning || _previousValue == CurrentWidget.Value)
            {
                return BuildStaticIndicator(CurrentWidget.Value, color);
            }

            var progress = Math.Clamp(_transitionProgress, 0, 1);
            var previousOpacity = Math.Clamp(1.0 - progress, 0, 1);
            var targetOpacity = progress;

            return new SizedBox(
                width: Width,
                height: Width,
                child: new Stack(
                    alignment: Alignment.Center,
                    children:
                    [
                        BuildFadedIndicator(_previousValue, color, previousOpacity),
                        BuildFadedIndicator(CurrentWidget.Value, color, targetOpacity)
                    ]));
        }

        private static Widget BuildStaticIndicator(bool? value, Color color)
        {
            return value switch
            {
                true => new Text(
                    "✓",
                    fontSize: 11,
                    fontWeight: FontWeight.Bold,
                    color: color,
                    softWrap: false,
                    maxLines: 1,
                    textAlign: TextAlign.Center),
                null => new Container(width: 7, height: 2, color: color),
                _ => new SizedBox()
            };
        }

        private static Widget BuildFadedIndicator(bool? value, Color color, double opacity)
        {
            if (opacity <= 0.001 || value is false)
            {
                return new SizedBox();
            }

            return new Opacity(
                opacity,
                child: BuildStaticIndicator(value, color));
        }

        private void HandleTap()
        {
            CurrentWidget.OnChanged?.Invoke(NextValue());
        }

        private void HandlePointerDown(PointerDownEvent @event)
        {
            if (!Enabled)
            {
                return;
            }

            SetPressed(true);
        }

        private void HandlePointerUp(PointerUpEvent @event)
        {
            SetPressed(false);
        }

        private void HandlePointerCancel(PointerCancelEvent @event)
        {
            SetPressed(false);
        }

        private void SetPressed(bool value)
        {
            if (!Enabled || _isPressed == value)
            {
                return;
            }

            SetState(() => _isPressed = value);
        }

        private void SetHovered(bool value)
        {
            if (!Enabled || _isHovered == value)
            {
                return;
            }

            SetState(() => _isHovered = value);
        }

        private bool? NextValue()
        {
            if (!CurrentWidget.Tristate)
            {
                return !(CurrentWidget.Value ?? false);
            }

            return CurrentWidget.Value switch
            {
                false => true,
                true => null,
                _ => false
            };
        }

        private KeyEventResult HandleKeyEvent(FocusNode node, KeyEvent @event)
        {
            if (!IsActivateKey(@event))
            {
                return KeyEventResult.Ignored;
            }

            if (!Enabled)
            {
                return KeyEventResult.Handled;
            }

            if (@event.IsDown)
            {
                HandleTap();
            }

            return KeyEventResult.Handled;
        }

        private static bool IsActivateKey(KeyEvent @event)
        {
            if (@event.IsShiftPressed
                || @event.IsControlPressed
                || @event.IsAltPressed
                || @event.IsMetaPressed)
            {
                return false;
            }

            return string.Equals(@event.Key, "Enter", StringComparison.Ordinal)
                   || string.Equals(@event.Key, "Return", StringComparison.Ordinal)
                   || string.Equals(@event.Key, "NumPadEnter", StringComparison.Ordinal)
                   || string.Equals(@event.Key, "NumpadEnter", StringComparison.Ordinal)
                   || string.Equals(@event.Key, "Space", StringComparison.Ordinal)
                   || string.Equals(@event.Key, "Spacebar", StringComparison.Ordinal);
        }

        private Color ResolveActiveColor()
        {
            if (CurrentWidget.ActiveColor.HasValue)
            {
                return CurrentWidget.ActiveColor.Value;
            }

            return CurrentWidget.IsDark
                ? DefaultFillColorDark
                : DefaultFillColorLight;
        }

        private Color ResolveFillColor(bool selected, Color activeColor)
        {
            if (!Enabled)
            {
                return ApplyOpacity(Colors.White, 0.50);
            }

            return selected
                ? activeColor
                : Colors.White;
        }

        private Color ResolveCheckColor(bool selected)
        {
            if (!selected)
            {
                return Colors.White;
            }

            if (CurrentWidget.CheckColor.HasValue)
            {
                return CurrentWidget.CheckColor.Value;
            }

            if (!Enabled)
            {
                return CurrentWidget.IsDark
                    ? DisabledCheckColorDark
                    : DisabledCheckColorLight;
            }

            return CurrentWidget.IsDark
                ? DefaultCheckColorDark
                : Colors.White;
        }

        private BorderSide ResolveBorderSide(bool selected)
        {
            if (CurrentWidget.Side.HasValue)
            {
                return selected
                    ? new BorderSide(Colors.Transparent, 0)
                    : CurrentWidget.Side.Value;
            }

            if (!Enabled)
            {
                return new BorderSide(DisabledBorderColor, 1.0);
            }

            if (selected || _hasFocus)
            {
                return new BorderSide(Colors.Transparent, 0);
            }

            var color = CurrentWidget.IsDark
                ? DefaultBorderColorDark
                : DefaultBorderColorLight;
            return new BorderSide(color, 1.0);
        }

        private Color ResolveFocusRingColor(Color activeColor)
        {
            if (CurrentWidget.FocusColor.HasValue)
            {
                return CurrentWidget.FocusColor.Value;
            }

            var hsl = ToHsl(activeColor);
            var focus = FromHsl(hsl.H, FocusSaturation, FocusLightness);
            return ApplyOpacity(focus, FocusOpacity);
        }

        private Color? ResolveOverlayColor(Color activeColor)
        {
            if (_isPressed)
            {
                return CurrentWidget.IsDark
                    ? ApplyOpacity(Colors.White, DefaultPressedOverlayOpacity)
                    : ApplyOpacity(Colors.Black, DefaultPressedOverlayOpacity);
            }

            if (_hasFocus)
            {
                return ResolveFocusRingColor(activeColor);
            }

            // Cupertino checkbox has no hover overlay by default.
            _ = _isHovered;
            return null;
        }

        private Size ResolveTapTargetSize()
        {
            var source = CurrentWidget.TapTargetSize;
            if (!source.HasValue)
            {
                return new Size(44, 44);
            }

            var size = source.Value;
            if (double.IsNaN(size.Width) || double.IsInfinity(size.Width) || size.Width <= 0
                || double.IsNaN(size.Height) || double.IsInfinity(size.Height) || size.Height <= 0)
            {
                return new Size(44, 44);
            }

            return size;
        }

        private void StartTransition()
        {
            if (_transitionController is null)
            {
                return;
            }

            SetState(() =>
            {
                _isTransitioning = true;
                _transitionProgress = 0;
            });

            _transitionController.Forward(0);
        }

        private void HandleTransitionTick()
        {
            if (_transitionController is null || !_isTransitioning)
            {
                return;
            }

            SetState(() =>
            {
                _transitionProgress = Math.Clamp(_transitionController.Evaluate(), 0, 1);
            });
        }

        private void HandleTransitionCompleted()
        {
            SetState(() =>
            {
                _transitionProgress = 1;
                _isTransitioning = false;
                _previousValue = CurrentWidget.Value;
            });
        }

        private void AttachFocusNode(FocusNode? externalNode)
        {
            _focusNode = externalNode ?? new FocusNode();
            _ownsFocusNode = externalNode is null;
            _focusNode.AddListener(HandleFocusChanged);
            _hasFocus = _focusNode.HasFocus;
        }

        private void DetachFocusNode(bool disposeOwned)
        {
            if (_focusNode is null)
            {
                return;
            }

            _focusNode.RemoveListener(HandleFocusChanged);
            if (disposeOwned && _ownsFocusNode)
            {
                _focusNode.Dispose();
            }

            _focusNode = null;
            _ownsFocusNode = false;
            _hasFocus = false;
        }

        private void HandleFocusChanged()
        {
            var hasFocus = _focusNode?.HasFocus ?? false;
            if (_hasFocus == hasFocus)
            {
                return;
            }

            SetState(() => _hasFocus = hasFocus);
        }

        private static bool IsSelected(bool? value)
        {
            return value ?? true;
        }

        private static (double H, double S, double L) ToHsl(Color color)
        {
            var r = color.R / 255.0;
            var g = color.G / 255.0;
            var b = color.B / 255.0;

            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));
            var delta = max - min;

            var l = (max + min) / 2.0;
            if (delta <= 0.000001)
            {
                return (0, 0, l);
            }

            var s = l < 0.5
                ? delta / (max + min)
                : delta / (2.0 - max - min);

            double h;
            if (Math.Abs(max - r) <= 0.000001)
            {
                h = ((g - b) / delta + (g < b ? 6.0 : 0.0)) / 6.0;
            }
            else if (Math.Abs(max - g) <= 0.000001)
            {
                h = ((b - r) / delta + 2.0) / 6.0;
            }
            else
            {
                h = ((r - g) / delta + 4.0) / 6.0;
            }

            return (h, s, l);
        }

        private static Color FromHsl(double h, double s, double l)
        {
            h = Normalize(h);
            s = Math.Clamp(s, 0, 1);
            l = Math.Clamp(l, 0, 1);

            if (s <= 0.000001)
            {
                var gray = (byte)Math.Clamp((int)Math.Round(l * 255), 0, 255);
                return Color.FromArgb(255, gray, gray, gray);
            }

            var q = l < 0.5
                ? l * (1 + s)
                : l + s - l * s;
            var p = 2 * l - q;

            var r = HueToRgb(p, q, h + 1.0 / 3.0);
            var g = HueToRgb(p, q, h);
            var b = HueToRgb(p, q, h - 1.0 / 3.0);

            return Color.FromArgb(
                255,
                (byte)Math.Clamp((int)Math.Round(r * 255), 0, 255),
                (byte)Math.Clamp((int)Math.Round(g * 255), 0, 255),
                (byte)Math.Clamp((int)Math.Round(b * 255), 0, 255));
        }

        private static double HueToRgb(double p, double q, double t)
        {
            t = Normalize(t);
            if (t < 1.0 / 6.0)
            {
                return p + (q - p) * 6 * t;
            }

            if (t < 1.0 / 2.0)
            {
                return q;
            }

            if (t < 2.0 / 3.0)
            {
                return p + (q - p) * (2.0 / 3.0 - t) * 6;
            }

            return p;
        }

        private static double Normalize(double value)
        {
            var result = value % 1.0;
            if (result < 0)
            {
                result += 1.0;
            }

            return result;
        }

        private static Color ApplyOpacity(Color color, double opacity)
        {
            var clampedOpacity = Math.Clamp(opacity, 0, 1);
            var alpha = (byte)Math.Clamp((int)Math.Round(color.A * clampedOpacity), 0, 255);
            return Color.FromArgb(alpha, color.R, color.G, color.B);
        }
    }
}
