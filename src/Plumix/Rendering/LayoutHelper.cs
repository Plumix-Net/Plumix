using System.Diagnostics;
using Avalonia;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/box.dart (adapted helper logic)

namespace Plumix.Rendering;

/// A collection of static functions to layout a [RenderBox] child with the
/// given set of [BoxConstraints].
///
/// All of the functions adhere to the [ChildLayouter] signature.
public static class ChildLayoutHelper {
  /// Returns the [Size] that the [RenderBox] would have if it were to
  /// be laid out with the given [BoxConstraints].
  ///
  /// This method calls [RenderBox.getDryLayout] on the given [RenderBox].
  ///
  /// This method should only be called by the parent of the provided
  /// [RenderBox] child as it binds parent and child together (if the child
  /// is marked as dirty, the child will also be marked as dirty).
  ///
  /// See also:
  ///
  ///  * [layoutChild], which actually lays out the child with the given
  ///    constraints.
  // public static Size dryLayoutChild(RenderBox child, BoxConstraints constraints) {
  //   return child.getDryLayout(constraints);
  // }

  /// Lays out the [RenderBox] with the given constraints and returns its
  /// [Size].
  ///
  /// This method calls [RenderBox.layout] on the given [RenderBox] with
  /// `parentUsesSize` set to true to receive its [Size].
  ///
  /// This method should only be called by the parent of the provided
  /// [RenderBox] child as it binds parent and child together (if the child
  /// is marked as dirty, the child will also be marked as dirty).
  ///
  /// See also:
  ///
  ///  * [dryLayoutChild], which does not perform a real layout of the child.
  public static Size layoutChild(RenderBox child, BoxConstraints constraints) {
    child.Layout(constraints, parentUsesSize: true);
    return child.Size;
  }

  /// Convenience function that calls [RenderBox.getDryBaseline].
  // static double? getDryBaseline(
  //   RenderBox child,
  //   BoxConstraints constraints,
  //   TextBaseline baseline
  // ) {
  //   return child.getDryBaseline(constraints, baseline);
  // }

  /// Convenience function that calls [RenderBox.getDistanceToBaseline].
  ///
  /// The given `child` must be already laid out with `constraints`.
  public static double? getBaseline(RenderBox child, BoxConstraints constraints, TextBaseline baseline) {
    //Debug.Assert(!child.debugNeedsLayout);
    Debug.Assert(child.Constraints == constraints);
    
    return child.GetDistanceToBaseline(baseline, onlyReal: true);
  }
}
