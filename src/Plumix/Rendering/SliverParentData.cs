using System.Globalization;
using Avalonia;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/sliver.dart

namespace Plumix.Rendering;

/// <summary>Parent data for a child positioned by a scroll offset.</summary>
public class SliverLogicalParentData : ParentData
{
    /// <summary>The child's main-axis position at the zero scroll offset, or null before layout.</summary>
    public double? LayoutOffset { get; set; }

    /// <inheritdoc />
    public override string ToString() =>
        $"layoutOffset={(LayoutOffset is null
            ? "None"
            : LayoutOffset.Value.ToString("F1", CultureInfo.InvariantCulture))}";
}

/// <summary>Logical parent data for a sliver in a doubly-linked child list.</summary>
public class SliverLogicalContainerParentData : SliverLogicalParentData, IContainerParentDataMixin<RenderSliver>
{
    public RenderSliver? previousSibling { get; set; }

    public RenderSliver? nextSibling { get; set; }

    /// <inheritdoc />
    public override void Detach()
    {
        DebugAssertions.Assert(previousSibling is null,
            "Pointers to siblings must be nulled before detaching ParentData.");
        DebugAssertions.Assert(nextSibling is null,
            "Pointers to siblings must be nulled before detaching ParentData.");
        base.Detach();
    }
}

/// <summary>Parent data for a child positioned in its parent's physical coordinate system.</summary>
public class SliverPhysicalParentData : ParentData
{
    /// <summary>The offset at which to paint the child.</summary>
    public Point PaintOffset { get; set; }

    /// <summary>The child's proportional cross-axis flexibility, or null when inflexible.</summary>
    public int? CrossAxisFlex { get; set; }

    /// <summary>Compatibility spelling for the former box parent-data offset.</summary>
    public Point offset
    {
        get => PaintOffset;
        set => PaintOffset = value;
    }

    /// <summary>Applies the child's paint offset to the supplied transform.</summary>
    public void ApplyPaintTransform(Matrix4 transform)
    {
        transform.TranslateByDouble(PaintOffset.X, PaintOffset.Y, 0, 1);
    }

    /// <inheritdoc />
    public override string ToString() => $"paintOffset={PaintOffset}";
}

/// <summary>Physical parent data for a sliver in a doubly-linked child list.</summary>
public class SliverPhysicalContainerParentData : SliverPhysicalParentData, IContainerParentDataMixin<RenderSliver>
{
    public RenderSliver? previousSibling { get; set; }

    public RenderSliver? nextSibling { get; set; }

    /// <inheritdoc />
    public override void Detach()
    {
        DebugAssertions.Assert(previousSibling is null,
            "Pointers to siblings must be nulled before detaching ParentData.");
        DebugAssertions.Assert(nextSibling is null,
            "Pointers to siblings must be nulled before detaching ParentData.");
        base.Detach();
    }
}
