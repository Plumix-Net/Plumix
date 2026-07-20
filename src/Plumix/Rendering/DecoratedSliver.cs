using Avalonia;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/decorated_sliver.dart

namespace Plumix.Rendering;

public sealed class RenderDecoratedSliver : RenderProxySliver
{
    private Decoration _decoration;
    private DecorationPosition _position;
    private ImageConfiguration _configuration;
    private BoxPainter? _painter;

    public RenderDecoratedSliver(
        Decoration decoration,
        DecorationPosition position = DecorationPosition.Background,
        ImageConfiguration? configuration = null,
        RenderSliver? sliver = null) : base(sliver)
    {
        _decoration = decoration ?? throw new ArgumentNullException(nameof(decoration));
        _position = position;
        _configuration = configuration ?? ImageConfiguration.Empty;
    }

    public Decoration Decoration
    {
        get => _decoration;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (Equals(_decoration, value))
            {
                return;
            }

            _decoration = value;
            ResetPainter();
            MarkNeedsPaint();
        }
    }

    public DecorationPosition Position
    {
        get => _position;
        set
        {
            if (_position == value)
            {
                return;
            }

            _position = value;
            MarkNeedsPaint();
        }
    }

    public ImageConfiguration Configuration
    {
        get => _configuration;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (_configuration == value)
            {
                return;
            }

            _configuration = value;
            MarkNeedsPaint();
        }
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (Child == null || Geometry.PaintExtent <= 0.0)
        {
            return;
        }

        Rect paintRect = GetMaxPaintRect();
        void PaintDecoration()
        {
            _painter ??= _decoration.CreateBoxPainter(MarkNeedsPaint);
            _painter.Paint(
                context,
                offset + paintRect.Position,
                _configuration.CopyWith(size: paintRect.Size));
        }

        if (_position == DecorationPosition.Background)
        {
            PaintDecoration();
        }

        base.Paint(context, offset);

        if (_position == DecorationPosition.Foreground)
        {
            PaintDecoration();
        }
    }

    protected override void OnAttach()
    {
        _painter = _decoration.CreateBoxPainter(MarkNeedsPaint);
        base.OnAttach();
    }

    protected override void OnDetach()
    {
        DisposePainter();
        base.OnDetach();
    }

    private void ResetPainter()
    {
        DisposePainter();
        if (Attached)
        {
            _painter = _decoration.CreateBoxPainter(MarkNeedsPaint);
        }
    }

    private void DisposePainter()
    {
        _painter?.Dispose();
        _painter = null;
    }
}
