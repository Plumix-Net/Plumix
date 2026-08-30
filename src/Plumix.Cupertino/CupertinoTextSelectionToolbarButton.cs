using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity sources:
// cupertino_ui/lib/src/text_selection_toolbar_button.dart
// cupertino_ui/lib/src/desktop_text_selection_toolbar_button.dart

/// <summary>An iOS-style text-selection toolbar button.</summary>
public sealed class CupertinoTextSelectionToolbarButton : StatefulWidget
{
    private static readonly TextStyle ToolbarButtonFontStyle = new(
        Inherit: false,
        FontSize: 15.0,
        LetterSpacing: -0.15,
        FontWeight: FontWeight.Normal);

    private static readonly CupertinoDynamicColor ToolbarTextColor =
        CupertinoDynamicColor.WithBrightness(CupertinoColors.Black, CupertinoColors.White);

    private static readonly CupertinoDynamicColor ToolbarPressedColor =
        CupertinoDynamicColor.WithBrightness(
            Color.FromArgb(0x10, 0x00, 0x00, 0x00),
            Color.FromArgb(0x10, 0xFF, 0xFF, 0xFF));

    // Value measured from screenshot of iOS 16.0.2.
    private static readonly Thickness ToolbarButtonPadding = new(16.0, 18.0);

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
            Widget child = new CupertinoButton(
                color: _isPressed
                    ? ToolbarPressedColor.ResolveFrom(context)
                    : CupertinoColors.Transparent,
                disabledColor: CupertinoColors.Transparent,
                // This CupertinoButton does not actually handle the onPressed callback, this is only
                // here to correctly enable/disable the button (see the GestureDetector below).
                onPressed: Current.OnPressed,
                padding: ToolbarButtonPadding,
                // There's no foreground fade on the iOS toolbar anymore, just the background is
                // darkened.
                pressedOpacity: 1.0,
                child: content);

            if (Current.OnPressed is null)
            {
                return child;
            }

            // As it's needed to change the CupertinoButton's background color when pressed, not its
            // opacity, this GestureDetector handles both the onPressed callback and the color change.
            return new GestureDetector(
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

            string label = Current.Text ?? GetButtonLabel(context, Current.ButtonItem!);
            Widget textWidget = new Text(
                label,
                style: ToolbarButtonFontStyle with
                {
                    Color = Current.OnPressed is not null
                        ? ToolbarTextColor.ResolveFrom(context)
                        : CupertinoColors.InactiveGray.ResolveFrom(context),
                },
                overflow: TextOverflow.Ellipsis);
            if (Current.ButtonItem?.Type != ContextMenuButtonType.LiveTextInput)
            {
                return textWidget;
            }

            return new SizedBox(
                width: 13.0,
                height: 13.0,
                child: new CustomPaint(
                    painter: new LiveTextIconPainter(ToolbarTextColor.ResolveFrom(context))));
        }

        private void SetPressed(bool value)
        {
            if (_isPressed == value)
            {
                return;
            }

            SetState(() => _isPressed = value);
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
            var origin = new Point(-size.Width / 2.0, -size.Height / 2.0);

            // Path for the one corner.
            var path = new Plumix.UI.Path();
            path.MoveTo(origin.X, origin.Y + 3.5);
            path.LineTo(origin.X, origin.Y + 1.0);
            path.ArcTo(
                new Rect(origin.X, origin.Y, 2.0, 2.0),
                Math.PI,
                Math.PI / 2.0,
                forceMoveTo: false);
            path.LineTo(origin.X + 3.5, origin.Y);

            context.Canvas.Save();
            context.Canvas.Translate(size.Width / 2.0, size.Height / 2.0);

            // Rotate to draw the corner four times.
            for (int quarter = 0; quarter < 4; quarter++)
            {
                context.Canvas.Save();
                context.Canvas.Rotate(quarter * Math.PI / 2.0);
                context.Canvas.DrawPath(path, null, pen);
                context.Canvas.Restore();
            }

            // Draw three lines.
            context.Canvas.DrawLine(pen, new Point(-3.0, -3.0), new Point(3.0, -3.0));
            context.Canvas.DrawLine(pen, new Point(-3.0, 0.0), new Point(3.0, 0.0));
            context.Canvas.DrawLine(pen, new Point(-3.0, 3.0), new Point(1.0, 3.0));
            context.Canvas.Restore();
        }

        public override bool ShouldRepaint(CustomPainter oldDelegate)
        {
            return oldDelegate is not LiveTextIconPainter oldPainter || oldPainter._color != _color;
        }
    }
}

/// <summary>A macOS-style text-selection toolbar button.</summary>
public sealed class CupertinoDesktopTextSelectionToolbarButton : StatefulWidget
{
    // These values were measured from a screenshot of the native context menu on macOS 13.2.
    private static readonly TextStyle ToolbarButtonFontStyle = new(
        Inherit: false,
        FontSize: 14.0,
        LetterSpacing: -0.15,
        FontWeight: FontWeight.Normal);

    private static readonly Thickness ToolbarButtonPadding = new(8.0, 2.0, 8.0, 5.0);

    private static readonly CupertinoDynamicColor ToolbarTextColor =
        CupertinoDynamicColor.WithBrightness(CupertinoColors.Black, CupertinoColors.White);

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
                style: ToolbarButtonFontStyle with { Color = ResolveTextColor(context) },
                overflow: TextOverflow.Ellipsis);
            CupertinoDynamicColor? backgroundColor = _isHovered
                ? CupertinoTheme.Of(context).PrimaryColor
                : null;
            Widget button = new CupertinoButton(
                alignment: Alignment.CenterLeft,
                borderRadius: BorderRadius.Circular(4.0),
                color: backgroundColor,
                minSize: 0.0,
                onPressed: Current.OnPressed,
                padding: ToolbarButtonPadding,
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
            return _isHovered
                ? CupertinoTheme.Of(context).PrimaryContrastingColor
                : ToolbarTextColor.ResolveFrom(context);
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
