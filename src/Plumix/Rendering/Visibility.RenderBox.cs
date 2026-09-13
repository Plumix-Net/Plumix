using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/indexed_stack.dart (_RenderVisibility)

namespace Plumix.Rendering;

internal sealed class RenderVisibility : RenderProxyBox
{
    private bool _visible;
    private bool _maintainSemantics;

    public RenderVisibility(bool visible, bool maintainSemantics, RenderBox? child = null)
    {
        _visible = visible;
        _maintainSemantics = maintainSemantics;
        Child = child;
    }

    public bool Visible
    {
        get => _visible;
        set
        {
            if (_visible == value)
            {
                return;
            }

            _visible = value;
            MarkNeedsPaint();
        }
    }

    public bool MaintainSemantics
    {
        get => _maintainSemantics;
        set
        {
            if (_maintainSemantics == value)
            {
                return;
            }

            _maintainSemantics = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (_maintainSemantics || _visible)
        {
            base.VisitChildrenForSemantics(visitor);
        }
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_visible)
        {
            base.Paint(ctx, offset);
        }
    }
}
