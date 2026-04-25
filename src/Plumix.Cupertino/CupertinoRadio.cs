using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source (reference): flutter/packages/flutter/lib/src/cupertino/radio.dart (adapted)

public sealed class CupertinoRadio<T> : StatefulWidget
{
    private const double FocusOpacity = 0.80;
    private const double FocusLightness = 0.69;
    private const double FocusSaturation = 0.835;
    private const double InnerRadius = 2.975;
    private const double BorderWidth = 0.30;
    private const double PressedOverlayOpacity = 0.15;
    private const double FocusRingWidth = 3.0;
    private const double DarkGradientTopOpacity = 0.14;
    private const double DarkGradientBottomOpacity = 0.29;
    private const double DisabledDarkGradientTopOpacity = 0.08;
    private const double DisabledDarkGradientBottomOpacity = 0.14;

    private static readonly Color DisabledOuterColor = Color.FromArgb(128, 255, 255, 255);
    private static readonly Color DisabledInnerColorLight = Color.FromArgb(64, 0, 0, 0);
    private static readonly Color DisabledInnerColorDark = Color.FromArgb(64, 255, 255, 255);
    private static readonly Color DisabledBorderColor = Color.FromArgb(64, 0, 0, 0);
    private static readonly Color DefaultBorderColorLight = Color.FromArgb(255, 209, 209, 214);
    private static readonly Color DefaultBorderColorDark = Color.FromArgb(64, 0, 0, 0);
    private static readonly Color DefaultOuterColorLight = Color.FromArgb(255, 0, 122, 255);
    private static readonly Color DefaultOuterColorDark = Color.FromArgb(255, 50, 100, 215);
    private static readonly Color DefaultInnerColorDark = Color.FromArgb(255, 222, 232, 248);

    public const double Width = 18.0;

    public CupertinoRadio(
        T value,
        T? groupValue,
        Action<T?>? onChanged,
        bool toggleable = false,
        Color? activeColor = null,
        Color? inactiveColor = null,
        Color? fillColor = null,
        Color? focusColor = null,
        bool useCheckmarkStyle = false,
        FocusNode? focusNode = null,
        bool autofocus = false,
        Size? tapTargetSize = null,
        bool isDark = false,
        Key? key = null) : base(key)
    {
        Value = value;
        GroupValue = groupValue;
        OnChanged = onChanged;
        Toggleable = toggleable;
        ActiveColor = activeColor;
        InactiveColor = inactiveColor;
        FillColor = fillColor;
        FocusColor = focusColor;
        UseCheckmarkStyle = useCheckmarkStyle;
        FocusNode = focusNode;
        Autofocus = autofocus;
        TapTargetSize = tapTargetSize;
        IsDark = isDark;
    }

    public T Value { get; }

    public T? GroupValue { get; }

    public Action<T?>? OnChanged { get; }

    public bool Toggleable { get; }

    public Color? ActiveColor { get; }

    public Color? InactiveColor { get; }

    public Color? FillColor { get; }

    public Color? FocusColor { get; }

    public bool UseCheckmarkStyle { get; }

    public FocusNode? FocusNode { get; }

    public bool Autofocus { get; }

    public Size? TapTargetSize { get; }

    public bool IsDark { get; }

    public override State CreateState()
    {
        return new CupertinoRadioState();
    }

    private sealed class CupertinoRadioState : State
    {
        private FocusNode? _focusNode;
        private bool _ownsFocusNode;
        private bool _hasFocus;
        private bool _isPressed;

        private CupertinoRadio<T> CurrentWidget => (CupertinoRadio<T>)StateWidget;

        private bool Enabled => CurrentWidget.OnChanged is not null;

        public override void InitState()
        {
            AttachFocusNode(CurrentWidget.FocusNode);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldRadio = (CupertinoRadio<T>)oldWidget;
            if (!ReferenceEquals(oldRadio.FocusNode, CurrentWidget.FocusNode))
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
            }
        }

        public override void Dispose()
        {
            DetachFocusNode(disposeOwned: true);
        }

        public override Widget Build(BuildContext context)
        {
            var selected = IsSelected();
            var shape = Plumix.Rendering.BorderRadius.Circular(Width / 2);
            var activeColor = ResolveActiveColor();
            var outerColor = ResolveOuterColor(selected, activeColor);
            var innerColor = ResolveInnerColor(selected);
            var borderColor = ResolveBorderColor(selected);
            var overlayColor = ResolvePressedOverlayColor();
            var focusRingColor = ResolveFocusRingColor(activeColor);
            var bodyColor = CurrentWidget.IsDark ? Colors.Transparent : outerColor;
            var borderWidth = borderColor.A == 0 ? 0 : BorderWidth;

            var layers = new List<Widget>
            {
                new DecoratedBox(
                    decoration: new BoxDecoration(
                        Color: bodyColor,
                        Border: new BorderSide(borderColor, borderWidth),
                        BorderRadius: shape),
                    child: new SizedBox(width: Width, height: Width))
            };

            if (CurrentWidget.IsDark)
            {
                layers.Add(new DecoratedBox(
                    decoration: new BoxDecoration(
                        Brush: CreateDarkGradientBrush(outerColor, Enabled),
                        BorderRadius: shape),
                    child: new SizedBox(width: Width, height: Width)));
            }

            if (overlayColor.HasValue && overlayColor.Value.A > 0)
            {
                layers.Add(new Container(
                    width: Width,
                    height: Width,
                    color: overlayColor.Value));
            }

            layers.Add(BuildIndicator(selected, innerColor, activeColor));

            Widget body = new SizedBox(
                width: Width,
                height: Width,
                child: new ClipRRect(
                    borderRadius: shape,
                    child: new Stack(
                        alignment: Alignment.Center,
                        children: layers)));

            if (_hasFocus && Enabled)
            {
                body = new DecoratedBox(
                    decoration: new BoxDecoration(
                        Border: new BorderSide(focusRingColor, FocusRingWidth),
                        BorderRadius: Plumix.Rendering.BorderRadius.Circular(shape.Radius + 1.5)),
                    child: new Padding(
                        new Thickness(1.5),
                        body));
            }

            var tapTarget = ResolveTapTargetSize();
            Widget result = new SizedBox(
                width: tapTarget.Width,
                height: tapTarget.Height,
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
                    child: result);
            }

            return new Focus(
                focusNode: _focusNode,
                autofocus: CurrentWidget.Autofocus,
                canRequestFocus: Enabled,
                onKeyEvent: HandleKeyEvent,
                child: result);
        }

        private Widget BuildIndicator(bool selected, Color innerColor, Color activeColor)
        {
            if (!selected)
            {
                return new SizedBox();
            }

            if (CurrentWidget.UseCheckmarkStyle)
            {
                return new StrokeGlyph(StrokeGlyphKind.Check, activeColor, Width);
            }

            return new DecoratedBox(
                decoration: new BoxDecoration(
                    Color: innerColor,
                    BorderRadius: Plumix.Rendering.BorderRadius.Circular(InnerRadius)),
                child: new SizedBox(
                    width: InnerRadius * 2,
                    height: InnerRadius * 2));
        }

        private void HandleTap()
        {
            if (CurrentWidget.OnChanged is null)
            {
                return;
            }

            if (IsSelected())
            {
                if (CurrentWidget.Toggleable)
                {
                    CurrentWidget.OnChanged.Invoke(default);
                }

                return;
            }

            CurrentWidget.OnChanged.Invoke(CurrentWidget.Value);
        }

        private void HandlePointerDown(PointerDownEvent @event)
        {
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

        private bool IsSelected()
        {
            return EqualityComparer<T?>.Default.Equals(CurrentWidget.Value, CurrentWidget.GroupValue);
        }

        private Color ResolveActiveColor()
        {
            if (CurrentWidget.ActiveColor.HasValue)
            {
                return CurrentWidget.ActiveColor.Value;
            }

            return CurrentWidget.IsDark
                ? DefaultOuterColorDark
                : DefaultOuterColorLight;
        }

        private Color ResolveOuterColor(bool selected, Color activeColor)
        {
            if (!Enabled)
            {
                return DisabledOuterColor;
            }

            if (selected)
            {
                return activeColor;
            }

            return CurrentWidget.InactiveColor ?? Colors.White;
        }

        private Color ResolveInnerColor(bool selected)
        {
            if (!selected)
            {
                return Colors.White;
            }

            if (CurrentWidget.FillColor.HasValue)
            {
                return CurrentWidget.FillColor.Value;
            }

            if (!Enabled)
            {
                return CurrentWidget.IsDark
                    ? DisabledInnerColorDark
                    : DisabledInnerColorLight;
            }

            return CurrentWidget.IsDark
                ? DefaultInnerColorDark
                : Colors.White;
        }

        private Color ResolveBorderColor(bool selected)
        {
            if (Enabled && (selected || _hasFocus))
            {
                return Colors.Transparent;
            }

            if (!Enabled)
            {
                return DisabledBorderColor;
            }

            return CurrentWidget.IsDark
                ? DefaultBorderColorDark
                : DefaultBorderColorLight;
        }

        private Color? ResolvePressedOverlayColor()
        {
            if (!_isPressed || !Enabled)
            {
                return null;
            }

            return CurrentWidget.IsDark
                ? ApplyOpacity(Colors.White, PressedOverlayOpacity)
                : ApplyOpacity(Colors.Black, PressedOverlayOpacity);
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

        private Size ResolveTapTargetSize()
        {
            var source = CurrentWidget.TapTargetSize;
            if (!source.HasValue)
            {
                return new Size(Width, Width);
            }

            var size = source.Value;
            if (double.IsNaN(size.Width) || double.IsInfinity(size.Width) || size.Width <= 0
                || double.IsNaN(size.Height) || double.IsInfinity(size.Height) || size.Height <= 0)
            {
                return new Size(Width, Width);
            }

            return size;
        }

        private static IBrush CreateDarkGradientBrush(Color baseColor, bool isEnabled)
        {
            var topOpacity = isEnabled ? DarkGradientTopOpacity : DisabledDarkGradientTopOpacity;
            var bottomOpacity = isEnabled ? DarkGradientBottomOpacity : DisabledDarkGradientBottomOpacity;
            return new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0.5, 0.0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0.5, 1.0, RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(ApplyOpacity(baseColor, topOpacity), 0.0),
                    new GradientStop(ApplyOpacity(baseColor, bottomOpacity), 1.0),
                }
            };
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
