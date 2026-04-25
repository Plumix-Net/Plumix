using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/proxy_box.dart (custom adaptation)

namespace Plumix;

public sealed class RenderButton : RenderBox
{
    private string _label;
    private Action? _onPressed;
    private Color _background;
    private Color _foreground;
    private double _fontSize;
    private readonly Thickness _padding;

    private TextLayout? _layout;

    public RenderButton(
        string label,
        Action? onPressed,
        Color background,
        Color foreground,
        double fontSize,
        Thickness? padding = null)
    {
        _label = label;
        _onPressed = onPressed;
        _background = background;
        _foreground = foreground;
        _fontSize = fontSize;
        _padding = padding ?? new Thickness(14, 10);
    }

    public string Label
    {
        get => _label;
        set
        {
            if (_label == value)
            {
                return;
            }

            _label = value;
            MarkNeedsLayout();
            MarkNeedsSemanticsUpdate();
        }
    }

    public Action? OnPressed
    {
        get => _onPressed;
        set
        {
            if (ReferenceEquals(_onPressed, value))
            {
                return;
            }

            _onPressed = value;
            MarkNeedsPaint();
            MarkNeedsSemanticsUpdate();
        }
    }

    public Color Background
    {
        get => _background;
        set
        {
            if (_background == value)
            {
                return;
            }

            _background = value;
            MarkNeedsPaint();
        }
    }

    public Color Foreground
    {
        get => _foreground;
        set
        {
            if (_foreground == value)
            {
                return;
            }

            _foreground = value;
            MarkNeedsLayout();
        }
    }

    public double FontSize
    {
        get => _fontSize;
        set
        {
            if (Math.Abs(_fontSize - value) < 0.01)
            {
                return;
            }

            _fontSize = value;
            MarkNeedsLayout();
        }
    }

    protected override void PerformLayout()
    {
        var maxTextWidth = double.IsInfinity(Constraints.MaxWidth)
            ? double.PositiveInfinity
            : Math.Max(0, Constraints.MaxWidth - _padding.Left - _padding.Right);
        Size measuredTextSize;

        try
        {
            _layout = new TextLayout(
                text: Label,
                typeface: new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal),
                fontSize: FontSize,
                foreground: new SolidColorBrush(Foreground),
                maxWidth: maxTextWidth);

            measuredTextSize = new Size(_layout.Width, _layout.Height);
        }
        catch (Exception exception) when (TextLayoutFallback.IsMissingFontManager(exception))
        {
            _layout = null;
            measuredTextSize = TextLayoutFallback.EstimateTextSize(Label, FontSize, maxTextWidth);
        }

        var desired = new Size(
            measuredTextSize.Width + _padding.Left + _padding.Right,
            measuredTextSize.Height + _padding.Top + _padding.Bottom);

        Size = Constraints.Constrain(desired);
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        var background = OnPressed == null
            ? new SolidColorBrush(Background, 0.45)
            : new SolidColorBrush(Background);

        var rect = new Rect(offset, Size);
        ctx.DrawRectangle(background, null, rect, 10, 10);

        if (_layout == null)
        {
            return;
        }

        var textX = offset.X + (Size.Width - _layout.Width) / 2;
        var textY = offset.Y + (Size.Height - _layout.Height) / 2;
        ctx.DrawTextLayout(_layout, new Point(textX, textY));
    }

    protected override bool HitTestSelf(Point position)
    {
        return OnPressed != null;
    }

    public override void HandleEvent(PointerEvent @event, HitTestEntry entry)
    {
        if (@event is PointerDownEvent)
        {
            OnPressed?.Invoke();
        }
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        configuration.IsSemanticBoundary = true;
        configuration.Label = Label;
        configuration.Flags |= SemanticsFlags.IsButton;

        if (OnPressed != null)
        {
            configuration.Flags |= SemanticsFlags.IsEnabled;
            configuration.AddActionHandler(SemanticsActions.Tap, () => OnPressed?.Invoke());
        }
    }
}
