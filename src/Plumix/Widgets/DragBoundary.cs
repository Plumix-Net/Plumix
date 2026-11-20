using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/drag_boundary.dart

/// <summary>Defines how a dragged object is constrained to a boundary.</summary>
public abstract class DragBoundaryDelegate<T>
{
    public abstract bool IsWithinBoundary(T draggedObject);

    public abstract T NearestPositionWithinBoundary(T draggedObject);
}

/// <summary>Provides the bounds used to constrain descendant drag operations.</summary>
public sealed class DragBoundary : InheritedWidget
{
    public DragBoundary(Widget child, Key? key = null) : base(child, key)
    {
    }

    public static DragBoundaryDelegate<Rect> ForRectOf(
        BuildContext context,
        bool useGlobalPosition = true)
    {
        return ForRectMaybeOf(context, useGlobalPosition) ?? new RectDragBoundaryDelegate(boundary: null);
    }

    public static DragBoundaryDelegate<Rect>? ForRectMaybeOf(
        BuildContext context,
        bool useGlobalPosition = true)
    {
        InheritedElement? element = context.GetElementForInheritedWidgetOfExactType<DragBoundary>();
        if (element is null)
        {
            return null;
        }

        if (element.RenderObject is not RenderBox { HasSize: true } renderBox)
        {
            throw new InvalidOperationException("DragBoundary is not available before its child has been laid out.");
        }

        Rect boundary;
        if (useGlobalPosition)
        {
            if (!renderBox.TryGetTransformFromRoot(out Matrix4 transform))
            {
                throw new InvalidOperationException("DragBoundary is not attached to the render tree.");
            }

            Offset topLeft = MatrixUtils.TransformPoint(transform, default);
            Offset bottomRight = MatrixUtils.TransformPoint(
                transform,
                new Offset(renderBox.Size.Width, renderBox.Size.Height));
            boundary = Rect.FromLTRB(topLeft.Dx, topLeft.Dy, bottomRight.Dx, bottomRight.Dy);
        }
        else
        {
            boundary = Offset.Zero & renderBox.Size;
        }

        return new RectDragBoundaryDelegate(boundary);
    }

    public override bool UpdateShouldNotify(InheritedWidget oldWidget) => true;

    private sealed class RectDragBoundaryDelegate : DragBoundaryDelegate<Rect>
    {
        private readonly Rect? _boundary;

        public RectDragBoundaryDelegate(Rect? boundary)
        {
            _boundary = boundary;
        }

        public override bool IsWithinBoundary(Rect draggedObject)
        {
            if (!_boundary.HasValue)
            {
                return true;
            }

            Rect boundary = _boundary.Value;
            return Contains(boundary, draggedObject.TopLeft)
                   && Contains(boundary, draggedObject.BottomRight);
        }

        public override Rect NearestPositionWithinBoundary(Rect draggedObject)
        {
            if (!_boundary.HasValue)
            {
                return draggedObject;
            }

            Rect boundary = _boundary.Value;
            if ((boundary.Right - draggedObject.Width) < boundary.Left
                || (boundary.Bottom - draggedObject.Height) < boundary.Top)
            {
                throw new InvalidOperationException(
                    "The rect is larger than the boundary. The rect width must be less than the boundary width, " +
                    "and the rect height must be less than the boundary height.");
            }

            double left = Math.Clamp(
                draggedObject.Left,
                boundary.Left,
                boundary.Right - draggedObject.Width);
            double top = Math.Clamp(
                draggedObject.Top,
                boundary.Top,
                boundary.Bottom - draggedObject.Height);
            return Rect.FromLTWH(left, top, draggedObject.Width, draggedObject.Height);
        }

        private static bool Contains(Rect rect, Offset point)
        {
            return point.Dx >= rect.Left
                   && point.Dx < rect.Right
                   && point.Dy >= rect.Top
                   && point.Dy < rect.Bottom;
        }
    }
}
