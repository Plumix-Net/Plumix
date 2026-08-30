using Avalonia;
using Avalonia.Media;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart (_RenderColoredBox)

namespace Plumix.Rendering;

public sealed class RenderColoredBox : RenderProxyBoxWithHitTestBehavior
{
    private Color _color;
    private bool _isAntiAlias;

    public RenderColoredBox(
        Color color,
        bool isAntiAlias = true,
        RenderBox? child = null) : base(HitTestBehavior.Opaque, child)
    {
        _color = color;
        _isAntiAlias = isAntiAlias;
    }

    public Color Color
    {
        get => _color;
        set
        {
            if (_color == value)
            {
                return;
            }

            _color = value;
            MarkNeedsPaint();
        }
    }

    public bool IsAntiAlias
    {
        get => _isAntiAlias;
        set
        {
            if (_isAntiAlias == value)
            {
                return;
            }

            _isAntiAlias = value;
            MarkNeedsPaint();
        }
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (Size.Width > 0.0 && Size.Height > 0.0)
        {
            context.Canvas.DrawRectangle(
                new SolidColorBrush(Color),
                null,
                new Rect(offset, Size),
                isAntiAlias: IsAntiAlias);
        }

        base.Paint(context, offset);
    }
}
