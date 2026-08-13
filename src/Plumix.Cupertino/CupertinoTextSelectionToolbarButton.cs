using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity sources:
// flutter/packages/flutter/lib/src/cupertino/text_selection_toolbar_button.dart
// flutter/packages/flutter/lib/src/cupertino/desktop_text_selection_toolbar_button.dart

/// <summary>An iOS-style text-selection toolbar button.</summary>
public sealed class CupertinoTextSelectionToolbarButton : StatefulWidget
{
    private CupertinoTextSelectionToolbarButton(
        Action? onPressed,
        Widget? child,
        string? text,
        ContextMenuButtonItem? buttonItem,
        Key? key) : base(key)
    {
        OnPressed = onPressed;
        Child = child;
        Text = text;
        ButtonItem = buttonItem;
    }

    public CupertinoTextSelectionToolbarButton(
        Action? onPressed,
        Widget child,
        Key? key = null) : this(
            onPressed,
            child ?? throw new ArgumentNullException(nameof(child)),
            null,
            null,
            key)
    {
    }

    public Action? OnPressed { get; }

    public Widget? Child { get; }

    public string? Text { get; }

    public ContextMenuButtonItem? ButtonItem { get; }

    public static CupertinoTextSelectionToolbarButton TextButton(
        Action? onPressed,
        string text,
        Key? key = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new CupertinoTextSelectionToolbarButton(onPressed, null, text, null, key);
    }

    public static CupertinoTextSelectionToolbarButton FromButtonItem(
        ContextMenuButtonItem buttonItem,
        Key? key = null)
    {
        ArgumentNullException.ThrowIfNull(buttonItem);
        return new CupertinoTextSelectionToolbarButton(
            buttonItem.OnPressed,
            null,
            null,
            buttonItem,
            key);
    }

    public static string GetButtonLabel(BuildContext context, ContextMenuButtonItem buttonItem)
    {
        ArgumentNullException.ThrowIfNull(buttonItem);
        if (buttonItem.Label is not null)
        {
            return buttonItem.Label;
        }

        CupertinoLocalizations localizations = CupertinoLocalizations.Of(context);
        return buttonItem.Type switch
        {
            ContextMenuButtonType.Cut => localizations.CutButtonLabel,
            ContextMenuButtonType.Copy => localizations.CopyButtonLabel,
            ContextMenuButtonType.Paste => localizations.PasteButtonLabel,
            ContextMenuButtonType.SelectAll => localizations.SelectAllButtonLabel,
            ContextMenuButtonType.LookUp => localizations.LookUpButtonLabel,
            ContextMenuButtonType.SearchWeb => localizations.SearchWebButtonLabel,
            ContextMenuButtonType.Share => localizations.ShareButtonLabel,
            ContextMenuButtonType.Delete or
                ContextMenuButtonType.LiveTextInput or
                ContextMenuButtonType.Custom => string.Empty,
            _ => string.Empty,
        };
    }

    public override State CreateState() => new CupertinoTextSelectionToolbarButtonState();

    private sealed class CupertinoTextSelectionToolbarButtonState : State
    {
        private bool _isPressed;

        private CupertinoTextSelectionToolbarButton Current =>
            (CupertinoTextSelectionToolbarButton)StateWidget;

        public override Widget Build(BuildContext context)
        {
            Widget content = BuildContent(context);
            Color pressedColor = IsDark(context)
                ? Color.FromArgb(0x10, 0xFF, 0xFF, 0xFF)
                : Color.FromArgb(0x10, 0x00, 0x00, 0x00);
            Widget child = new CupertinoButton(
                color: _isPressed ? pressedColor : CupertinoColors.Transparent,
                disabledColor: CupertinoColors.Transparent,
                onPressed: Current.OnPressed,
                padding: new Thickness(16.0, 18.0),
                pressedOpacity: 1.0,
                child: content);

            if (Current.OnPressed is null)
            {
                return child;
            }

            return new GestureDetector(
                behavior: HitTestBehavior.Opaque,
                onTapDown: _ => SetPressed(true),
                onTapUp: _ =>
                {
                    SetPressed(false);
                    Current.OnPressed?.Invoke();
                },
                onTapCancel: () => SetPressed(false),
                child: child);
        }

        private Widget BuildContent(BuildContext context)
        {
            if (Current.Child is not null)
            {
                return Current.Child;
            }

            if (Current.ButtonItem?.Type == ContextMenuButtonType.LiveTextInput)
            {
                return new SizedBox(
                    width: 13.0,
                    height: 13.0,
                    child: new CustomPaint(
                        painter: new LiveTextIconPainter(ResolveTextColor(context, enabled: true))));
            }

            string label = Current.Text ?? GetButtonLabel(context, Current.ButtonItem!);
            return new Text(
                label,
                style: new TextStyle(
                    FontSize: 15.0,
                    LetterSpacing: -0.15,
                    FontWeight: FontWeight.Normal,
                    Color: ResolveTextColor(context, Current.OnPressed is not null),
                    Inherit: false),
                overflow: TextOverflow.Ellipsis);
        }

        private void SetPressed(bool value)
        {
            if (_isPressed == value)
            {
                return;
            }

            SetState(() => _isPressed = value);
        }

        private static Color ResolveTextColor(BuildContext context, bool enabled)
        {
            if (!enabled)
            {
                return CupertinoColors.InactiveGray;
            }

            return IsDark(context) ? CupertinoColors.White : CupertinoColors.Black;
        }
    }

    private sealed class LiveTextIconPainter : CustomPainter
    {
        private readonly Color _color;

        public LiveTextIconPainter(Color color)
        {
            _color = color;
        }

        public override void Paint(PaintingContext context, Size size)
        {
            var pen = new Pen(new SolidColorBrush(_color), 1.0)
            {
                LineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round,
            };
            double right = size.Width;
            double bottom = size.Height;
            context.DrawLine(pen, new Point(0.0, 3.5), new Point(0.0, 1.0));
            context.DrawLine(pen, new Point(1.0, 0.0), new Point(3.5, 0.0));
            context.DrawLine(pen, new Point(right - 3.5, 0.0), new Point(right - 1.0, 0.0));
            context.DrawLine(pen, new Point(right, 1.0), new Point(right, 3.5));
            context.DrawLine(pen, new Point(right, bottom - 3.5), new Point(right, bottom - 1.0));
            context.DrawLine(pen, new Point(right - 1.0, bottom), new Point(right - 3.5, bottom));
            context.DrawLine(pen, new Point(3.5, bottom), new Point(1.0, bottom));
            context.DrawLine(pen, new Point(0.0, bottom - 1.0), new Point(0.0, bottom - 3.5));
            context.DrawLine(pen, new Point(3.5, 3.5), new Point(right - 3.5, 3.5));
            context.DrawLine(pen, new Point(3.5, size.Height / 2.0), new Point(right - 3.5, size.Height / 2.0));
            context.DrawLine(pen, new Point(3.5, bottom - 3.5), new Point(right - 4.5, bottom - 3.5));
        }

        public override bool ShouldRepaint(CustomPainter oldDelegate)
        {
            return oldDelegate is not LiveTextIconPainter oldPainter || oldPainter._color != _color;
        }
    }

    internal static bool IsDark(BuildContext context)
    {
        return MediaQuery.MaybeOf(context)?.PlatformBrightness == PlatformBrightness.Dark;
    }
}

/// <summary>A macOS-style text-selection toolbar button.</summary>
public sealed class CupertinoDesktopTextSelectionToolbarButton : StatefulWidget
{
    private CupertinoDesktopTextSelectionToolbarButton(
        Action? onPressed,
        Widget? child,
        string? text,
        ContextMenuButtonItem? buttonItem,
        Key? key) : base(key)
    {
        OnPressed = onPressed;
        Child = child;
        Text = text;
        ButtonItem = buttonItem;
    }

    public CupertinoDesktopTextSelectionToolbarButton(
        Action? onPressed,
        Widget child,
        Key? key = null) : this(
            onPressed,
            child ?? throw new ArgumentNullException(nameof(child)),
            null,
            null,
            key)
    {
    }

    public Action? OnPressed { get; }

    public Widget? Child { get; }

    public string? Text { get; }

    public ContextMenuButtonItem? ButtonItem { get; }

    public static CupertinoDesktopTextSelectionToolbarButton TextButton(
        Action? onPressed,
        string text,
        Key? key = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new CupertinoDesktopTextSelectionToolbarButton(onPressed, null, text, null, key);
    }

    public static CupertinoDesktopTextSelectionToolbarButton FromButtonItem(
        ContextMenuButtonItem buttonItem,
        Key? key = null)
    {
        ArgumentNullException.ThrowIfNull(buttonItem);
        return new CupertinoDesktopTextSelectionToolbarButton(
            buttonItem.OnPressed,
            null,
            null,
            buttonItem,
            key);
    }

    public override State CreateState() => new CupertinoDesktopTextSelectionToolbarButtonState();

    private sealed class CupertinoDesktopTextSelectionToolbarButtonState : State
    {
        private bool _isHovered;

        private CupertinoDesktopTextSelectionToolbarButton Current =>
            (CupertinoDesktopTextSelectionToolbarButton)StateWidget;

        public override Widget Build(BuildContext context)
        {
            Widget child = Current.Child ?? new Text(
                Current.Text ?? CupertinoTextSelectionToolbarButton.GetButtonLabel(context, Current.ButtonItem!),
                style: new TextStyle(
                    FontSize: 14.0,
                    LetterSpacing: -0.15,
                    FontWeight: FontWeight.Normal,
                    Color: ResolveTextColor(context),
                    Inherit: false),
                overflow: TextOverflow.Ellipsis);
            Color? backgroundColor = _isHovered
                ? ResolvePrimaryColor(context)
                : null;
            Widget button = new CupertinoButton(
                alignment: Alignment.CenterLeft,
                borderRadius: BorderRadius.Circular(4.0),
                color: backgroundColor,
                minSize: 0.0,
                onPressed: Current.OnPressed,
                padding: new Thickness(8.0, 2.0, 8.0, 5.0),
                pressedOpacity: 0.7,
                child: child);

            return new SizedBox(
                width: double.PositiveInfinity,
                child: new MouseRegion(
                    onEnter: _ => SetHovered(true),
                    onExit: _ => SetHovered(false),
                    child: button));
        }

        private Color ResolveTextColor(BuildContext context)
        {
            if (_isHovered)
            {
                return CupertinoTheme.Of(context).EffectivePrimaryContrastingColor;
            }

            return CupertinoTextSelectionToolbarButton.IsDark(context)
                ? CupertinoColors.White
                : CupertinoColors.Black;
        }

        private static Color ResolvePrimaryColor(BuildContext context)
        {
            return CupertinoTheme.Of(context).EffectivePrimaryColor;
        }

        private void SetHovered(bool value)
        {
            if (_isHovered == value)
            {
                return;
            }

            SetState(() => _isHovered = value);
        }

    }
}
