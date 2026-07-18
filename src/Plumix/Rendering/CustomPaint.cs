using Avalonia;
using Plumix.Foundation;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/custom_paint.dart

public abstract class CustomPainter : IDisposable
{
    protected CustomPainter() : this(null)
    {
    }

    protected CustomPainter(IListenable? repaint)
    {
        Repaint = repaint;
    }

    internal IListenable? Repaint { get; }

    public abstract void Paint(PaintingContext context, Size size);

    public abstract bool ShouldRepaint(CustomPainter oldDelegate);

    public virtual bool? HitTest(Point position) => null;

    public virtual void Dispose()
    {
    }
}

public sealed class RenderCustomPaint : RenderProxyBox
{
    private CustomPainter? _painter;
    private CustomPainter? _foregroundPainter;
    private Size _preferredSize;

    public RenderCustomPaint(
        CustomPainter? painter = null,
        CustomPainter? foregroundPainter = null,
        Size preferredSize = default,
        RenderBox? child = null)
    {
        _painter = painter;
        _foregroundPainter = foregroundPainter;
        _preferredSize = preferredSize;
        Child = child;
    }

    public CustomPainter? Painter
    {
        get => _painter;
        set => SetPainter(ref _painter, value);
    }

    public CustomPainter? ForegroundPainter
    {
        get => _foregroundPainter;
        set => SetPainter(ref _foregroundPainter, value);
    }

    public Size PreferredSize
    {
        get => _preferredSize;
        set
        {
            if (_preferredSize == value) return;
            _preferredSize = value;
            MarkNeedsLayout();
        }
    }

    protected override void PerformLayout()
    {
        if (Child is not null)
        {
            base.PerformLayout();
            return;
        }
        Size = Constraints.Constrain(PreferredSize);
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (Painter is not null)
        {
            context.PushTransform(Matrix.CreateTranslation(offset.X, offset.Y),
                childContext => Painter.Paint(childContext, Size));
        }
        base.Paint(context, offset);
        if (ForegroundPainter is not null)
        {
            context.PushTransform(Matrix.CreateTranslation(offset.X, offset.Y),
                childContext => ForegroundPainter.Paint(childContext, Size));
        }
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        return (ForegroundPainter?.HitTest(position) ?? false)
               || base.HitTestChildren(result, position);
    }

    protected override bool HitTestSelf(Point position) =>
        Painter is not null && (Painter.HitTest(position) ?? true);

    private void SetPainter(ref CustomPainter? field, CustomPainter? value)
    {
        if (ReferenceEquals(field, value)) return;
        bool shouldRepaint = field is null || value is null || value.ShouldRepaint(field);
        if (Attached)
        {
            field?.Repaint?.RemoveListener(MarkNeedsPaint);
            value?.Repaint?.AddListener(MarkNeedsPaint);
        }
        field = value;
        if (shouldRepaint) MarkNeedsPaint();
    }

    protected override void OnAttach()
    {
        base.OnAttach();
        Painter?.Repaint?.AddListener(MarkNeedsPaint);
        ForegroundPainter?.Repaint?.AddListener(MarkNeedsPaint);
    }

    protected override void OnDetach()
    {
        Painter?.Repaint?.RemoveListener(MarkNeedsPaint);
        ForegroundPainter?.Repaint?.RemoveListener(MarkNeedsPaint);
        base.OnDetach();
    }
}
