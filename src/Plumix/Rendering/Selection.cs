using System.Runtime.CompilerServices;
using Avalonia;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/selection.dart

/// The result after handling a [SelectionEvent].
public enum SelectionResult
{
    /// There is nothing left to select forward in this [ISelectable], and further
    /// selection should extend to the next [ISelectable] in screen order.
    Next,

    /// Selection does not reach this [ISelectable] and is located before it in
    /// screen order.
    Previous,

    /// Selection ends in this [ISelectable].
    End,

    /// The result can't be determined in this frame.
    Pending,

    /// There is no result for the selection event.
    None,
}

/// The abstract interface for a selection under a [ISelectionRegistrar].
public interface ISelectionHandler : IValueListenable<SelectionGeometry>
{
    /// Marks this handler to be responsible for pushing [LayerLink]s for the
    /// selection handles.
    void PushHandleLayers(LayerLink? startHandle, LayerLink? endHandle);

    /// Gets the selected content in this object, or null when nothing is selected.
    SelectedContent? GetSelectedContent();

    /// Gets the selected range in this object, or null when nothing is selected.
    SelectedContentRange? GetSelection();

    /// Handles the [SelectionEvent] sent to this object.
    SelectionResult DispatchSelectionEvent(SelectionEvent @event);

    /// The length of the content in this object.
    int ContentLength { get; }
}

/// The length and the offsets of a selection relative to the content it belongs to.
public sealed class SelectedContentRange : IEquatable<SelectedContentRange>
{
    public SelectedContentRange(int startOffset, int endOffset)
    {
        if (startOffset < 0 || endOffset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startOffset), "Selection offsets must not be negative.");
        }

        StartOffset = startOffset;
        EndOffset = endOffset;
    }

    /// The start of the selection relative to the start of the content.
    public int StartOffset { get; }

    /// The end of the selection relative to the start of the content.
    public int EndOffset { get; }

    public bool Equals(SelectedContentRange? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is not null && other.StartOffset == StartOffset && other.EndOffset == EndOffset;
    }

    public override bool Equals(object? obj) => Equals(obj as SelectedContentRange);

    public override int GetHashCode() => HashCode.Combine(StartOffset, EndOffset);

    public override string ToString() => $"SelectedContentRange({StartOffset}, {EndOffset})";
}

/// The selected content in a [ISelectionHandler].
public sealed class SelectedContent
{
    public SelectedContent(string plainText)
    {
        PlainText = plainText ?? string.Empty;
    }

    /// The selected content in plain text format.
    public string PlainText { get; }

    public override string ToString() => $"SelectedContent({PlainText})";
}

/// A [ISelectionHandler] that receives selection events and paints the selection
/// highlight for the content it owns.
public interface ISelectable : ISelectionHandler
{
    /// Gets the transform from this object's local coordinates to `ancestor`.
    Matrix4 GetTransformTo(RenderObject? ancestor);

    /// The size of this [ISelectable].
    Size Size { get; }

    /// A list of [Rect]s, in local coordinates, that bound this [ISelectable].
    IReadOnlyList<Rect> BoundingBoxes { get; }

    /// Disposes the resources held by this [ISelectable].
    void Dispose();
}

/// Reproduces Dart's `SelectionRegistrant` mixin: keeps its owner registered with
/// a [ISelectionRegistrar] exactly while the owner's geometry reports content.
///
/// C# has no mixins, so the algorithm lives in this helper and the owner exposes
/// it through its own `Registrar` property.
public sealed class SelectionRegistrant
{
    private readonly ISelectable _owner;
    private ISelectionRegistrar? _registrar;
    private bool _subscribedToSelectionRegistrar;

    public SelectionRegistrant(ISelectable owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public ISelectionRegistrar? Registrar
    {
        get => _registrar;
        set
        {
            if (ReferenceEquals(value, _registrar))
            {
                return;
            }

            if (value is null)
            {
                _owner.RemoveListener(UpdateSelectionRegistrarSubscription);
            }
            else if (_registrar is null)
            {
                _owner.AddListener(UpdateSelectionRegistrarSubscription);
            }

            RemoveSelectionRegistrarSubscription();
            _registrar = value;
            UpdateSelectionRegistrarSubscription();
        }
    }

    public void Dispose() => RemoveSelectionRegistrarSubscription();

    private void UpdateSelectionRegistrarSubscription()
    {
        if (_registrar is null)
        {
            _subscribedToSelectionRegistrar = false;
            return;
        }

        if (_subscribedToSelectionRegistrar && !_owner.Value.HasContent)
        {
            _registrar.Remove(_owner);
            _subscribedToSelectionRegistrar = false;
        }
        else if (!_subscribedToSelectionRegistrar && _owner.Value.HasContent)
        {
            _registrar.Add(_owner);
            _subscribedToSelectionRegistrar = true;
        }
    }

    private void RemoveSelectionRegistrarSubscription()
    {
        if (_subscribedToSelectionRegistrar)
        {
            _registrar!.Remove(_owner);
            _subscribedToSelectionRegistrar = false;
        }
    }
}

/// A utility class that provides useful methods for handling selections.
public static class SelectionUtils
{
    /// Determines [SelectionResult] purely based on the target rectangle.
    public static SelectionResult GetResultBasedOnRect(Rect targetRect, Point point)
    {
        if (RectContains(targetRect, point))
        {
            return SelectionResult.End;
        }

        if (point.Y < targetRect.Top)
        {
            return SelectionResult.Previous;
        }

        if (point.Y > targetRect.Bottom)
        {
            return SelectionResult.Next;
        }

        return point.X >= targetRect.Right ? SelectionResult.Next : SelectionResult.Previous;
    }

    /// Adjusts the drag position to be within the target rect.
    public static Point AdjustDragOffset(
        Rect targetRect,
        Point point,
        TextDirection direction = TextDirection.Ltr)
    {
        if (RectContains(targetRect, point))
        {
            return point;
        }

        if (point.Y <= targetRect.Top || (point.Y <= targetRect.Bottom && point.X <= targetRect.Left))
        {
            // Area 1.
            return direction == TextDirection.Ltr ? targetRect.TopLeft : targetRect.TopRight;
        }

        // Area 2.
        return direction == TextDirection.Ltr ? targetRect.BottomRight : targetRect.BottomLeft;
    }

    /// Dart's `Rect.contains` is half-open on the right and bottom edges;
    /// Avalonia's `Rect.Contains` is closed, so the predicate is reimplemented.
    internal static bool RectContains(Rect rect, Point point)
    {
        return point.X >= rect.Left && point.X < rect.Right && point.Y >= rect.Top && point.Y < rect.Bottom;
    }
}

/// The type of a [SelectionEvent].
public enum SelectionEventType
{
    /// An event that updates the start edge of the selection.
    StartEdgeUpdate,

    /// An event that updates the end edge of the selection.
    EndEdgeUpdate,

    /// An event that clears the selection.
    Clear,

    /// An event that selects all selectable content.
    SelectAll,

    /// An event that selects a word at the location.
    SelectWord,

    /// An event that selects a paragraph at the location.
    SelectParagraph,

    /// An event that extends the selection by a specific [TextGranularity].
    GranularlyExtendSelection,

    /// An event that extends the selection in a directional movement.
    DirectionallyExtendSelection,
}

/// The unit of how selection handles move in a text.
public enum TextGranularity
{
    /// Treats each character as an atomic unit.
    Character,

    /// Treats a word as an atomic unit.
    Word,

    /// Treats a paragraph as an atomic unit.
    Paragraph,

    /// Treats each line break as an atomic unit.
    Line,

    /// Treats the entire document as an atomic unit.
    Document,
}

/// The direction to extend a selection.
public enum SelectionExtendDirection
{
    /// Move one edge of the selection vertically to the previous adjacent line.
    PreviousLine,

    /// Move one edge of the selection vertically to the next adjacent line.
    NextLine,

    /// Move the selection edges forward to a certain horizontal offset in the same line.
    Forward,

    /// Move the selection edges backward to a certain horizontal offset in the same line.
    Backward,
}

/// An abstract base class for selection events.
public abstract class SelectionEvent
{
    private protected SelectionEvent(SelectionEventType type)
    {
        Type = type;
    }

    /// The type of this selection event.
    public SelectionEventType Type { get; }
}

/// Selects all selectable contents.
public sealed class SelectAllSelectionEvent : SelectionEvent
{
    public SelectAllSelectionEvent() : base(SelectionEventType.SelectAll)
    {
    }
}

/// Clears the selection from the [ISelectable] and removes any existing highlight.
public sealed class ClearSelectionEvent : SelectionEvent
{
    public ClearSelectionEvent() : base(SelectionEventType.Clear)
    {
    }
}

/// Selects the whole word at the location.
public sealed class SelectWordSelectionEvent : SelectionEvent
{
    public SelectWordSelectionEvent(Point globalPosition) : base(SelectionEventType.SelectWord)
    {
        GlobalPosition = globalPosition;
    }

    /// The position in global coordinates to select word at.
    public Point GlobalPosition { get; }
}

/// Selects the whole paragraph at the location.
public sealed class SelectParagraphSelectionEvent : SelectionEvent
{
    public SelectParagraphSelectionEvent(Point globalPosition, bool absorb = false)
        : base(SelectionEventType.SelectParagraph)
    {
        GlobalPosition = globalPosition;
        Absorb = absorb;
    }

    /// The position in global coordinates to select the paragraph at.
    public Point GlobalPosition { get; }

    /// Whether the selectable receiving the event should be absorbed into an
    /// encompassing paragraph.
    public bool Absorb { get; }
}

/// Updates a selection edge.
public sealed class SelectionEdgeUpdateEvent : SelectionEvent
{
    private SelectionEdgeUpdateEvent(
        SelectionEventType type,
        Point globalPosition,
        TextGranularity? granularity)
        : base(type)
    {
        GlobalPosition = globalPosition;
        Granularity = granularity ?? TextGranularity.Character;
    }

    /// Creates a selection start edge update event.
    public static SelectionEdgeUpdateEvent ForStart(
        Point globalPosition,
        TextGranularity? granularity = null)
    {
        return new SelectionEdgeUpdateEvent(SelectionEventType.StartEdgeUpdate, globalPosition, granularity);
    }

    /// Creates a selection end edge update event.
    public static SelectionEdgeUpdateEvent ForEnd(
        Point globalPosition,
        TextGranularity? granularity = null)
    {
        return new SelectionEdgeUpdateEvent(SelectionEventType.EndEdgeUpdate, globalPosition, granularity);
    }

    /// The new location of the selection edge, in global coordinates.
    public Point GlobalPosition { get; }

    /// The granularity for which the selection moves.
    public TextGranularity Granularity { get; }
}

/// Extends the current selection with respect to a [TextGranularity].
public sealed class GranularlyExtendSelectionEvent : SelectionEvent
{
    public GranularlyExtendSelectionEvent(bool forward, bool isEnd, TextGranularity granularity)
        : base(SelectionEventType.GranularlyExtendSelection)
    {
        Forward = forward;
        IsEnd = isEnd;
        Granularity = granularity;
    }

    /// Whether the selection is extended forward.
    public bool Forward { get; }

    /// Whether the end edge is being moved.
    public bool IsEnd { get; }

    /// The granularity for which the selection extends.
    public TextGranularity Granularity { get; }
}

/// Extends the current selection with respect to a [SelectionExtendDirection].
public sealed class DirectionallyExtendSelectionEvent : SelectionEvent
{
    public DirectionallyExtendSelectionEvent(double dx, bool isEnd, SelectionExtendDirection direction)
        : base(SelectionEventType.DirectionallyExtendSelection)
    {
        Dx = dx;
        IsEnd = isEnd;
        Direction = direction;
    }

    /// The horizontal offset, in global coordinates, the edge should move to.
    public double Dx { get; }

    /// Whether the end edge is being moved.
    public bool IsEnd { get; }

    /// The direction in which to extend the selection.
    public SelectionExtendDirection Direction { get; }

    /// Makes a copy of this object with the given fields replaced.
    public DirectionallyExtendSelectionEvent CopyWith(
        double? dx = null,
        bool? isEnd = null,
        SelectionExtendDirection? direction = null)
    {
        return new DirectionallyExtendSelectionEvent(
            dx ?? Dx,
            isEnd ?? IsEnd,
            direction ?? Direction);
    }
}

/// A registrar that keeps track of [ISelectable]s in a subtree.
public interface ISelectionRegistrar
{
    /// Adds the [ISelectable] into the registrar.
    void Add(ISelectable selectable);

    /// Removes the [ISelectable] from the registrar.
    void Remove(ISelectable selectable);
}

/// The current status of a selection.
public enum SelectionStatus
{
    /// The selection is not collapsed.
    Uncollapsed,

    /// The selection is collapsed.
    Collapsed,

    /// No selection.
    None,
}

/// The geometry of the current selection.
public sealed class SelectionGeometry : IEquatable<SelectionGeometry>
{
    private static readonly IReadOnlyList<Rect> EmptyRects = [];

    public SelectionGeometry(
        SelectionStatus status,
        bool hasContent,
        SelectionPoint? startSelectionPoint = null,
        SelectionPoint? endSelectionPoint = null,
        IReadOnlyList<Rect>? selectionRects = null)
    {
        if ((startSelectionPoint is not null || endSelectionPoint is not null)
            && status == SelectionStatus.None)
        {
            throw new ArgumentException(
                "A selection point requires a status other than SelectionStatus.None.",
                nameof(status));
        }

        Status = status;
        HasContent = hasContent;
        StartSelectionPoint = startSelectionPoint;
        EndSelectionPoint = endSelectionPoint;
        SelectionRects = selectionRects ?? EmptyRects;
    }

    /// The geometry information at the selection start.
    public SelectionPoint? StartSelectionPoint { get; }

    /// The geometry information at the selection end.
    public SelectionPoint? EndSelectionPoint { get; }

    /// The status of the ongoing selection.
    public SelectionStatus Status { get; }

    /// The rects in the local coordinates of the containing [ISelectable] that
    /// represent the selection.
    public IReadOnlyList<Rect> SelectionRects { get; }

    /// Whether there is any selectable content in the [ISelectionHandler].
    public bool HasContent { get; }

    /// Whether there is an ongoing selection.
    public bool HasSelection => Status != SelectionStatus.None;

    /// Makes a copy of this object with the given fields replaced.
    ///
    /// Matching Dart, a null argument keeps the existing value, so the selection
    /// points cannot be cleared through this method.
    public SelectionGeometry CopyWith(
        SelectionPoint? startSelectionPoint = null,
        SelectionPoint? endSelectionPoint = null,
        IReadOnlyList<Rect>? selectionRects = null,
        SelectionStatus? status = null,
        bool? hasContent = null)
    {
        return new SelectionGeometry(
            status: status ?? Status,
            hasContent: hasContent ?? HasContent,
            startSelectionPoint: startSelectionPoint ?? StartSelectionPoint,
            endSelectionPoint: endSelectionPoint ?? EndSelectionPoint,
            selectionRects: selectionRects ?? SelectionRects);
    }

    public bool Equals(SelectionGeometry? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is not null
               && Equals(other.StartSelectionPoint, StartSelectionPoint)
               && Equals(other.EndSelectionPoint, EndSelectionPoint)
               && other.SelectionRects.SequenceEqual(SelectionRects)
               && other.Status == Status
               && other.HasContent == HasContent;
    }

    public override bool Equals(object? obj) => Equals(obj as SelectionGeometry);

    // Dart hashes the rect list by identity (`List.hashCode`); reproduced as-is.
    public override int GetHashCode()
    {
        return HashCode.Combine(
            StartSelectionPoint,
            EndSelectionPoint,
            RuntimeHelpers.GetHashCode(SelectionRects),
            Status,
            HasContent);
    }
}

/// The geometry information of a selection point.
public sealed class SelectionPoint : IEquatable<SelectionPoint>
{
    public SelectionPoint(Point localPosition, double lineHeight, TextSelectionHandleType handleType)
    {
        LocalPosition = localPosition;
        LineHeight = lineHeight;
        HandleType = handleType;
    }

    /// The position of the selection point in the local coordinates of the
    /// containing [ISelectable].
    public Point LocalPosition { get; }

    /// The line height at the selection point.
    public double LineHeight { get; }

    /// The selection handle type that should be used at this point.
    public TextSelectionHandleType HandleType { get; }

    public bool Equals(SelectionPoint? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is not null
               && other.LocalPosition == LocalPosition
               && other.LineHeight.Equals(LineHeight)
               && other.HandleType == HandleType;
    }

    public override bool Equals(object? obj) => Equals(obj as SelectionPoint);

    public override int GetHashCode() => HashCode.Combine(LocalPosition, LineHeight, HandleType);
}
