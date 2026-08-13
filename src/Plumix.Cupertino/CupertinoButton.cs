using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: flutter/packages/flutter/lib/src/cupertino/button.dart

/// <summary>A Cupertino push button with pressed-opacity and colored-background behavior.</summary>
public sealed class CupertinoButton : StatefulWidget
{
    public CupertinoButton(
        Widget child,
        Action? onPressed,
        EdgeInsetsGeometry? padding = null,
        Color? color = null,
        Color? disabledColor = null,
        double? minSize = 44.0,
        double pressedOpacity = 0.4,
        BorderRadius? borderRadius = null,
        AlignmentGeometry alignment = default,
        Key? key = null) : base(key)
    {
        if (minSize is < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(minSize));
        }

        if (pressedOpacity < 0.0 || pressedOpacity > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(pressedOpacity));
        }

        Child = child ?? throw new ArgumentNullException(nameof(child));
        OnPressed = onPressed;
        Padding = padding ?? new Thickness(16.0);
        Color = color;
        DisabledColor = disabledColor ?? Avalonia.Media.Color.Parse("#FFBDBDBD");
        MinSize = minSize;
        PressedOpacity = pressedOpacity;
        BorderRadius = borderRadius ?? Plumix.Rendering.BorderRadius.Circular(8.0);
        Alignment = alignment == default ? Plumix.Rendering.Alignment.Center : alignment;
    }

    public Widget Child { get; }

    public Action? OnPressed { get; }

    public EdgeInsetsGeometry Padding { get; }

    public Color? Color { get; }

    public Color DisabledColor { get; }

    public double? MinSize { get; }

    public double PressedOpacity { get; }

    public BorderRadius BorderRadius { get; }

    public AlignmentGeometry Alignment { get; }

    public bool Enabled => OnPressed is not null;

    public override State CreateState() => new CupertinoButtonState();

    private sealed class CupertinoButtonState : State
    {
        private bool _pressed;

        private CupertinoButton Current => (CupertinoButton)StateWidget;

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            if (!Current.Enabled)
            {
                _pressed = false;
            }
        }

        public override Widget Build(BuildContext context)
        {
            Widget child = new Align(
                alignment: Current.Alignment,
                child: Current.Child);
            child = new Padding(Current.Padding, child);
            if (Current.Color.HasValue || !Current.Enabled)
            {
                Color background = Current.Enabled
                    ? Current.Color ?? CupertinoColors.Transparent
                    : Current.DisabledColor;
                child = new DecoratedBox(
                    decoration: new BoxDecoration(
                        Color: background,
                        BorderRadius: Current.BorderRadius),
                    child: child);
            }

            if (Current.MinSize.HasValue)
            {
                child = new ConstrainedBox(
                    new BoxConstraints(
                        MinWidth: Current.MinSize.Value,
                        MinHeight: Current.MinSize.Value),
                    child);
            }

            child = new Opacity(_pressed ? Current.PressedOpacity : 1.0, child);
            if (!Current.Enabled)
            {
                return new Semantics(flags: SemanticsFlags.IsButton, child: child);
            }

            return new Semantics(
                flags: SemanticsFlags.IsButton | SemanticsFlags.IsEnabled,
                onTap: Current.OnPressed,
                child: new GestureDetector(
                    behavior: HitTestBehavior.Opaque,
                    onTapDown: _ => SetPressed(true),
                    onTapUp: _ =>
                    {
                        SetPressed(false);
                        Current.OnPressed?.Invoke();
                    },
                    onTapCancel: () => SetPressed(false),
                    child: child));
        }

        private void SetPressed(bool value)
        {
            if (_pressed == value)
            {
                return;
            }

            SetState(() => _pressed = value);
        }
    }
}
