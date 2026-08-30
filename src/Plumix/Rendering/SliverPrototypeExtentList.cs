using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/sliver_prototype_extent_list.dart

namespace Plumix.Rendering;

/// <summary>
/// A sliver that constrains every child to the main-axis extent of an offstage prototype child.
/// </summary>
/// <remarks>
/// Flutter's <c>_RenderSliverPrototypeExtentList</c>, which is private to
/// <c>sliver_prototype_extent_list.dart</c>; Plumix keeps it public so the render tree can be
/// asserted on from tests, the way Flutter's own tests reach it through the element.
/// </remarks>
public class RenderSliverPrototypeExtentList : RenderSliverFixedExtentBoxAdaptor
{
    private RenderBox? _prototypeChild;

    public RenderSliverPrototypeExtentList(
        RenderBox? prototypeChild = null,
        IRenderSliverBoxChildManager? childManager = null)
        : base(childManager)
    {
        PrototypeChild = prototypeChild;
    }

    /// <summary>The offstage child whose main-axis size fixes every other child's extent.</summary>
    /// <remarks>Dart's <c>child</c>.</remarks>
    public RenderBox? PrototypeChild
    {
        get => _prototypeChild;
        set
        {
            if (ReferenceEquals(_prototypeChild, value))
            {
                return;
            }

            if (_prototypeChild is not null)
            {
                DropChild(_prototypeChild);
            }

            _prototypeChild = value;
            if (_prototypeChild is not null)
            {
                AdoptChild(_prototypeChild);
            }

            MarkNeedsLayout();
        }
    }

    /// <inheritdoc />
    public override double? ItemExtent
    {
        get
        {
            if (_prototypeChild is null)
            {
                return 0;
            }

            return ConstraintsForSliver.Axis == Axis.Vertical
                ? _prototypeChild.Size.Height
                : _prototypeChild.Size.Width;
        }
    }

    /// <inheritdoc />
    public override void VisitChildren(Action<RenderObject> visitor)
    {
        if (_prototypeChild is not null)
        {
            visitor(_prototypeChild);
        }

        base.VisitChildren(visitor);
    }

    /// <inheritdoc />
    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        if (_prototypeChild is null)
        {
            Geometry = default;
            return;
        }

        _prototypeChild.Layout(constraints.AsBoxConstraints(), parentUsesSize: true);
        base.PerformSliverLayout(constraints);
    }
}
