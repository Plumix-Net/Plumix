using Avalonia;
using Avalonia.Media;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// C#-only infrastructure: an early hand-written demo render object with no Dart counterpart.

namespace Plumix;

public sealed class RenderButton : RenderBox
{
    private string _label;
    private Action? _onPressed;
    private Color _background;
    private Color _foreground;
    private double _fontSize;
    private readonly Thickness _padding;

    private readonly TextPainter _textPainter = new(textDirection: TextDirection.Ltr);

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
        double maxTextWidth = double.IsInfinity(Constraints.MaxWidth)
            ? double.PositiveInfinity
            : Math.Max(0, Constraints.MaxWidth - _padding.Left - _padding.Right);
        _textPainter.Text = new TextSpan(
            text: Label,
            style: new TextStyle(Color: Foreground, FontSize: FontSize));
        _textPainter.Layout(maxWidth: maxTextWidth);
        Size measuredTextSize = _textPainter.Size;

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
        ctx.Canvas.DrawRectangle(background, null, rect, 10, 10);

        double textX = offset.X + (Size.Width - _textPainter.Width) / 2;
        double textY = offset.Y + (Size.Height - _textPainter.Height) / 2;
        _textPainter.Paint(ctx.Canvas, new Point(textX, textY));
    }

    public override void Dispose()
    {
        _textPainter.Dispose();
        base.Dispose();
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
        configuration.TextDirection = TextDirection.Ltr;
        configuration.Flags |= SemanticsFlags.IsButton;

        if (OnPressed != null)
        {
            configuration.Flags |= SemanticsFlags.IsEnabled;
            configuration.AddActionHandler(SemanticsActions.Tap, () => OnPressed?.Invoke());
        }
    }
}
