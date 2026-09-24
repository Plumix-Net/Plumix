using Avalonia;
using Plumix.Foundation;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/layout_helper.dart

namespace Plumix.Rendering;

/// <summary>
/// The signature for a function that takes a <see cref="RenderBox"/> and returns the <see cref="Size"/>
/// that the <see cref="RenderBox"/> would have if it were laid out with the given
/// <see cref="BoxConstraints"/>.
/// </summary>
/// <remarks>
/// The methods of <see cref="ChildLayoutHelper"/> adhere to this signature.
/// </remarks>
public delegate Size ChildLayouter(RenderBox child, BoxConstraints constraints);

/// <summary>
/// The signature for a function that takes a <see cref="RenderBox"/> and returns the baseline offset
/// of that <see cref="RenderBox"/> if it were laid out with the given <see cref="BoxConstraints"/>.
/// </summary>
/// <remarks>
/// The methods of <see cref="ChildLayoutHelper"/> adhere to this signature.
/// </remarks>
public delegate double? ChildBaselineGetter(RenderBox child, BoxConstraints constraints, TextBaseline baseline);

/// <summary>
/// A collection of static functions to layout a <see cref="RenderBox"/> child with the given set of
/// <see cref="BoxConstraints"/>.
/// </summary>
/// <remarks>All of the functions adhere to the <see cref="ChildLayouter"/> signature.</remarks>
public static class ChildLayoutHelper
{
    /// <summary>
    /// Returns the <see cref="Size"/> that the <see cref="RenderBox"/> would have if it were to be laid
    /// out with the given <see cref="BoxConstraints"/>.
    /// </summary>
    /// <remarks>
    /// This method calls <see cref="RenderBox.GetDryLayout"/> on the given <see cref="RenderBox"/>. It
    /// should only be called by the parent of the provided child as it binds parent and child together
    /// (if the child is marked as dirty, the child will also be marked as dirty).
    /// </remarks>
    public static Size DryLayoutChild(RenderBox child, BoxConstraints constraints)
    {
        return child.GetDryLayout(constraints);
    }

    /// <summary>
    /// Lays out the <see cref="RenderBox"/> with the given constraints and returns its
    /// <see cref="Size"/>.
    /// </summary>
    /// <remarks>
    /// This method calls <see cref="RenderObject.Layout"/> on the given <see cref="RenderBox"/> with
    /// <c>parentUsesSize</c> set to true to receive its <see cref="Size"/>.
    /// </remarks>
    public static Size LayoutChild(RenderBox child, BoxConstraints constraints)
    {
        child.Layout(constraints, parentUsesSize: true);
        return child.Size;
    }

    /// <summary>Convenience function that calls <see cref="RenderBox.GetDryBaseline"/>.</summary>
    public static double? GetDryBaseline(RenderBox child, BoxConstraints constraints, TextBaseline baseline)
    {
        return child.GetDryBaseline(constraints, baseline);
    }

    /// <summary>Convenience function that calls <see cref="RenderBox.GetDistanceToBaseline"/>.</summary>
    /// <remarks>The given <paramref name="child"/> must be already laid out with
    /// <paramref name="constraints"/>.</remarks>
    public static double? GetBaseline(RenderBox child, BoxConstraints constraints, TextBaseline baseline)
    {
        if (Constants.KDebugMode && (child.DebugNeedsLayout || child.Constraints != constraints))
        {
            throw new AssertionError();
        }

        return child.GetDistanceToBaseline(baseline, onlyReal: true);
    }
}
