using System.Globalization;
using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/viewport_offset.dart

namespace Plumix.Rendering;

/// <summary>
/// The direction of a scroll, relative to the positive scroll offset axis given by an
/// <see cref="AxisDirection"/> and a <see cref="GrowthDirection"/>.
/// </summary>
/// <remarks>
/// Similar to <see cref="GrowthDirection"/>, but contrasts in that it has a third value,
/// <see cref="Idle"/>, for the case where no scroll is occurring.
/// </remarks>
public enum ScrollDirection
{
    /// <summary>No scrolling is underway.</summary>
    Idle,

    /// <summary>Scrolling is happening in the negative scroll offset direction.</summary>
    Forward,

    /// <summary>Scrolling is happening in the positive scroll offset direction.</summary>
    Reverse
}

/// <summary>
/// Which part of the content inside the viewport should be visible.
/// </summary>
/// <remarks>
/// The <see cref="Pixels"/> value determines the scroll offset that the viewport uses to select
/// which part of its content to display. As the user scrolls the viewport, this value changes,
/// which changes the content that is displayed.
/// </remarks>
public abstract class ViewportOffset : ChangeNotifier
{
    /// <summary>
    /// The number of pixels to offset the children in the opposite of the axis direction.
    /// </summary>
    /// <remarks>
    /// This object notifies its listeners when this value changes (except when the value changes due
    /// to <see cref="CorrectBy"/>).
    /// </remarks>
    public abstract double Pixels { get; }

    /// <summary>Whether the <see cref="Pixels"/> property is available.</summary>
    public abstract bool HasPixels { get; }

    /// <summary>
    /// Called when the viewport's extents are established, with the main-axis dimension of the
    /// viewport.
    /// </summary>
    /// <returns>
    /// False when applying the viewport dimension changed the scroll offset, in which case the
    /// viewport is laid out again with the new offset; true when the dimension was accepted
    /// unconditionally.
    /// </returns>
    public abstract bool ApplyViewportDimension(double viewportDimension);

    /// <summary>Called when the viewport's content extents are established.</summary>
    /// <returns>
    /// False when applying the content dimensions changed the scroll offset, in which case the
    /// viewport is laid out again with the new offset; true when they were accepted unconditionally.
    /// </returns>
    public abstract bool ApplyContentDimensions(double minScrollExtent, double maxScrollExtent);

    /// <summary>
    /// Applies a layout-time correction to the scroll offset, changing <see cref="Pixels"/> by
    /// <paramref name="correction"/> without notifying listeners.
    /// </summary>
    public abstract void CorrectBy(double correction);

    /// <summary>
    /// Jumps <see cref="Pixels"/> from its current value to the given value, without animation, and
    /// without checking if the new value is in range.
    /// </summary>
    public abstract void JumpTo(double pixels);

    /// <summary>Animates <see cref="Pixels"/> from its current value to the given value.</summary>
    /// <remarks>
    /// Flutter returns a <c>Future&lt;void&gt;</c>; Plumix returns the equivalent <see cref="Task"/>.
    /// </remarks>
    public abstract Task AnimateTo(double to, TimeSpan duration, Curve? curve = null);

    /// <summary>
    /// Calls <see cref="JumpTo"/> if <paramref name="duration"/> is null or zero, otherwise
    /// <see cref="AnimateTo"/> with <see cref="Curves.Ease"/> as the default curve.
    /// </summary>
    /// <param name="clamp">
    /// Ignored by this stub implementation; <see cref="ScrollPosition"/> honors it by clamping
    /// <paramref name="to"/> into the scroll extents.
    /// </param>
    public virtual Task MoveTo(
        double to,
        TimeSpan? duration = null,
        Curve? curve = null,
        bool? clamp = null)
    {
        if (duration is not { } animationDuration || animationDuration == TimeSpan.Zero)
        {
            JumpTo(to);
            return Task.CompletedTask;
        }

        return AnimateTo(to, animationDuration, curve ?? Curves.Ease);
    }

    /// <summary>
    /// The direction in which the user is trying to change <see cref="Pixels"/>, relative to the
    /// viewport's <see cref="RenderViewportBase.AxisDirection"/>.
    /// </summary>
    public abstract ScrollDirection UserScrollDirection { get; }

    /// <summary>
    /// Whether a viewport is allowed to change <see cref="Pixels"/> implicitly to respond to a call
    /// to <see cref="RenderObject.ShowOnScreen"/>.
    /// </summary>
    public abstract bool AllowImplicitScrolling { get; }

    /// <summary>Creates a viewport offset with the given <see cref="Pixels"/> value.</summary>
    /// <remarks>Flutter's <c>ViewportOffset.fixed</c>. The value only changes on a correction.</remarks>
    public static ViewportOffset Fixed(double value) => new FixedViewportOffset(value);

    /// <summary>Creates a viewport offset with a <see cref="Pixels"/> value of 0.0.</summary>
    /// <remarks>Flutter's <c>ViewportOffset.zero</c>.</remarks>
    public static ViewportOffset Zero() => new FixedViewportOffset(0.0);

    public override string ToString()
    {
        var description = new List<string>();
        DebugFillDescription(description);
        return $"{GetType().Name}#{GetHashCode():x}({string.Join(", ", description)})";
    }

    /// <summary>Adds additional information to the given description for use by <see cref="ToString"/>.</summary>
    protected virtual void DebugFillDescription(List<string> description)
    {
        if (HasPixels)
        {
            description.Add($"offset: {Pixels.ToString("F1", CultureInfo.InvariantCulture)}");
        }
    }
}

/// <summary>Dart's private <c>_FixedViewportOffset</c>.</summary>
internal sealed class FixedViewportOffset(double pixels) : ViewportOffset
{
    private double _pixels = pixels;

    public override double Pixels => _pixels;

    public override bool HasPixels => true;

    public override bool ApplyViewportDimension(double viewportDimension) => true;

    public override bool ApplyContentDimensions(double minScrollExtent, double maxScrollExtent) => true;

    public override void CorrectBy(double correction)
    {
        _pixels += correction;
    }

    public override void JumpTo(double pixels)
    {
        // Do nothing, viewport is fixed.
    }

    public override Task AnimateTo(double to, TimeSpan duration, Curve? curve = null)
    {
        return Task.CompletedTask;
    }

    public override ScrollDirection UserScrollDirection => ScrollDirection.Idle;

    public override bool AllowImplicitScrolling => false;
}
