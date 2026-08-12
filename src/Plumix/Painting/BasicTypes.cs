namespace Plumix.Painting;

/// The description of the difference between two objects, in the context of how
/// it will affect the rendering.
///
/// Used by [TextSpan.compareTo] and [TextStyle.compareTo].
///
/// The values in this enum are ordered such that they are in increasing order
/// of cost. A value with index N implies all the values with index less than N.
/// For example, [Layout] (index 3) implies [Paint] (2).
public enum RenderComparison
{
    /// The two objects are identical (meaning deeply equal, not necessarily
    /// reference-equal).
    Identical,

    /// The two objects are identical for the purpose of layout, but may be
    /// different in other ways.
    ///
    /// For example, maybe some event handlers changed.
    Metadata,

    /// The two objects are different but only in ways that affect paint, not layout.
    ///
    /// For example, only the color is changed.
    Paint,

    /// The two objects are different in ways that affect layout (and therefore paint).
    ///
    /// For example, the size is changed.
    ///
    /// This is the most drastic level of change possible.
    Layout
}

/// A direction in which boxes flow vertically.
///
/// This is used by the flex algorithm (e.g. [Column]) to decide in which
/// direction to draw boxes.
///
/// This is also used to disambiguate `start` and `end` values (e.g.
/// [MainAxisAlignment.start] or [CrossAxisAlignment.end]).
///
/// See also:
///
///  * [TextDirection], which controls the same thing but horizontally.
public enum VerticalDirection
{
    /// Boxes should start at the bottom and be stacked vertically towards the top.
    ///
    /// The "start" is at the bottom, the "end" is at the top.
    Up,

    /// Boxes should start at the top and be stacked vertically towards the bottom.
    ///
    /// The "start" is at the top, the "end" is at the bottom.
    Down
}

// Dart parity source (reference): flutter/packages/flutter/lib/src/painting/basic_types.dart (approximate)
