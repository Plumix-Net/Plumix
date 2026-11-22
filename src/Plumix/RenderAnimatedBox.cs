using Plumix.Rendering;
using Plumix.UI;

// C#-only infrastructure: an early hand-written demo render object with no Dart counterpart.

namespace Plumix;

static class MathEx
{
    public static bool Near(double a, double b, double eps = 0.005) => Math.Abs(a - b) <= eps;
}

internal sealed class RenderAnimatedBox : RenderBox, IDisposable
{
    private readonly AnimationController _controller;

    // Интерполируем ширину, высоту и цвет

    private readonly double _fromW, _toW;
    private readonly double _fromH, _toH;
    private readonly Color _fromC, _toC;

    private readonly Paint _paint = new() { Color = new Color(0xFFFFFFFF) };

    public RenderAnimatedBox(
        AnimationController controller,
        double fromWidth, double toWidth,
        double fromHeight, double toHeight,
        Color fromColor, Color toColor)
    {
        _controller = controller;
        _fromW = fromWidth;
        _toW = toWidth;
        _fromH = fromHeight;
        _toH = toHeight;
        _fromC = fromColor;
        _toC = toColor;

        _controller.Changed += OnTick;
    }

    private double _currentW, _currentH;

    private void OnTick()
    {
        double t = _controller.Evaluate();

        double nextW = _fromW + ((_toW - _fromW) * t);
        double nextH = _fromH + ((_toH - _fromH) * t);
        var nextC = Color.Lerp(_fromC, _toC, t)!;

        bool affectsLayout = !MathEx.Near(nextW, _currentW) || !MathEx.Near(nextH, _currentH);

        _currentW = nextW;
        _currentH = nextH;
        _paint.Color = nextC;

        if (affectsLayout) MarkNeedsLayout();
        else MarkNeedsPaint();
    }

    protected override void PerformLayout()
    {
        // Задаёмся анимируемыми размерами, но не превышаем constraints
        var desired = new Size(_currentW <= 0 ? _fromW : _currentW,
            _currentH <= 0 ? _fromH : _currentH);

        Size = Constraints.Constrain(desired);
    }

    public override void Paint(PaintingContext ctx, Offset offset)
    {
        var rect = offset & Size;
        ctx.Canvas.DrawRRect(RRect.FromRectXY(rect, 16, 16), _paint);
    }

    public override void Dispose()
    {
        _controller.Changed -= OnTick;
        base.Dispose();
    }
}
