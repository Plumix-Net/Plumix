using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/paragraph.dart

namespace Plumix;

public sealed partial class RenderParagraph
{
    private const char PlaceholderCharacter = (char)PlaceholderSpan.PlaceholderCodeUnit;

    private ISelectionRegistrar? _registrar;
    private List<SelectableFragment>? _lastSelectableFragments;

    /// The [ISelectionRegistrar] this paragraph registers its fragments with.
    public ISelectionRegistrar? Registrar
    {
        get => _registrar;
        set
        {
            if (ReferenceEquals(value, _registrar))
            {
                return;
            }

            RemoveSelectionRegistrarSubscription();
            DisposeSelectableFragments();
            _registrar = value;
            UpdateSelectionRegistrarSubscription();
        }
    }

    /// The selection highlight color, or null when the paragraph paints no highlight.
    public Color? SelectionColor
    {
        get => _selectionColor;
        set
        {
            if (_selectionColor == value)
            {
                return;
            }

            _selectionColor = value;
            if (_lastSelectableFragments?.Any(fragment => fragment.Value.HasSelection) ?? false)
            {
                MarkNeedsPaint();
            }
        }
    }

    protected override bool AlwaysNeedsCompositing => _lastSelectableFragments?.Count > 0;

    /// Whether the given selectable is one of this paragraph's fragments.
    public bool SelectableBelongsToParagraph(ISelectable selectable)
    {
        return _lastSelectableFragments?.Contains(selectable) ?? false;
    }

    /// The raw, unnormalized selections currently held by this paragraph's fragments.
    internal IReadOnlyList<TextSelection> Selections
    {
        get
        {
            if (_lastSelectableFragments is null)
            {
                return [];
            }

            var results = new List<TextSelection>();
            foreach (SelectableFragment fragment in _lastSelectableFragments)
            {
                if (fragment.TextSelectionStart is { } start && fragment.TextSelectionEnd is { } end)
                {
                    results.Add(new TextSelection(start.Offset, end.Offset));
                }
            }

            return results;
        }
    }

    public override void MarkNeedsLayout()
    {
        if (_lastSelectableFragments is not null)
        {
            foreach (SelectableFragment fragment in _lastSelectableFragments)
            {
                fragment.DidChangeParagraphLayout();
            }
        }

        base.MarkNeedsLayout();
    }

    /// Releases the fragments this paragraph owns.
    public void Dispose()
    {
        RemoveSelectionRegistrarSubscription();
        DisposeSelectableFragments();
    }

    private void UpdateSelectionRegistrarSubscription()
    {
        if (_registrar is null)
        {
            return;
        }

        _lastSelectableFragments ??= GetSelectableFragments();
        foreach (SelectableFragment fragment in _lastSelectableFragments)
        {
            _registrar.Add(fragment);
        }

        if (_lastSelectableFragments.Count > 0)
        {
            MarkNeedsCompositingBitsUpdate();
        }
    }

    private void RemoveSelectionRegistrarSubscription()
    {
        if (_registrar is null || _lastSelectableFragments is null)
        {
            return;
        }

        foreach (SelectableFragment fragment in _lastSelectableFragments)
        {
            _registrar.Remove(fragment);
        }
    }

    private void DisposeSelectableFragments()
    {
        if (_lastSelectableFragments is null)
        {
            return;
        }

        foreach (SelectableFragment fragment in _lastSelectableFragments)
        {
            fragment.Dispose();
        }

        _lastSelectableFragments = null;
    }

    private void RebuildSelectableFragments()
    {
        RemoveSelectionRegistrarSubscription();
        DisposeSelectableFragments();
        UpdateSelectionRegistrarSubscription();
    }

    private List<SelectableFragment> GetSelectableFragments()
    {
        string plainText = _text.ToPlainText(includeSemanticsLabels: false);
        var result = new List<SelectableFragment>();
        int start = 0;
        while (start < plainText.Length)
        {
            int end = plainText.IndexOf(PlaceholderCharacter, start);
            if (start != end)
            {
                if (end == -1)
                {
                    end = plainText.Length;
                }

                result.Add(new SelectableFragment(this, plainText, new TextRange(start, end)));
                start = end;
            }

            start += 1;
        }

        return result;
    }

    internal void MarkSelectionNeedsPaint() => MarkNeedsPaint();

    private void PaintSelectionHighlights(PaintingContext context, Point offset)
    {
        if (_lastSelectableFragments is null)
        {
            return;
        }

        foreach (SelectableFragment fragment in _lastSelectableFragments)
        {
            fragment.PaintSelection(context, offset);
        }
    }

    private void PaintSelectionHandles(PaintingContext context, Point offset)
    {
        if (_lastSelectableFragments is null)
        {
            return;
        }

        foreach (SelectableFragment fragment in _lastSelectableFragments)
        {
            fragment.PaintHandles(context, offset);
        }
    }
}

/// One selectable run of a [RenderParagraph], split at placeholder boundaries.
internal sealed class SelectableFragment : ChangeNotifier, ISelectable, ITextLayoutMetrics
{
    private const char PlaceholderCharacter = (char)PlaceholderSpan.PlaceholderCodeUnit;
    private const int PlaceholderLength = 1;

    private readonly RenderParagraph _paragraph;
    private readonly string _fullText;
    private readonly TextRange _range;
    private SelectionGeometry _selectionGeometry;
    private bool _selectableContainsOriginTextBoundary;
    private LayerLink? _startHandleLayerLink;
    private LayerLink? _endHandleLayerLink;
    private IReadOnlyList<Rect>? _cachedBoundingBoxes;
    private Rect? _cachedRect;

    public SelectableFragment(RenderParagraph paragraph, string fullText, TextRange range)
    {
        if (!range.IsValid || range.IsCollapsed || !range.IsNormalized)
        {
            throw new ArgumentException("A selectable fragment requires a valid, non-empty range.", nameof(range));
        }

        _paragraph = paragraph;
        _fullText = fullText;
        _range = range;
        _selectionGeometry = GetSelectionGeometry();
    }

    internal TextPosition? TextSelectionStart { get; private set; }

    internal TextPosition? TextSelectionEnd { get; private set; }

    internal TextRange Range => _range;

    public SelectionGeometry Value => _selectionGeometry;

    public int ContentLength => _range.End - _range.Start;

    public Size Size => Rect.Size;

    public Matrix4 GetTransformTo(RenderObject? ancestor) => _paragraph.GetTransformTo(ancestor);

    public IReadOnlyList<Rect> BoundingBoxes
    {
        get
        {
            if (_cachedBoundingBoxes is null)
            {
                IReadOnlyList<TextBox> boxes = _paragraph.GetBoxesForSelection(
                    new TextSelection(_range.Start, _range.End),
                    BoxHeightStyle.Max);
                if (boxes.Count > 0)
                {
                    _cachedBoundingBoxes = boxes.Select(box => box.ToRect()).ToList();
                }
                else
                {
                    _cachedBoundingBoxes = [EmptyRangeRect()];
                }
            }

            return _cachedBoundingBoxes;
        }
    }

    private Rect Rect
    {
        get
        {
            if (_cachedRect is null)
            {
                IReadOnlyList<TextBox> boxes = _paragraph.GetBoxesForSelection(
                    new TextSelection(_range.Start, _range.End),
                    BoxHeightStyle.Max);
                if (boxes.Count > 0)
                {
                    Rect result = boxes[0].ToRect();
                    for (int index = 1; index < boxes.Count; index += 1)
                    {
                        result = result.Union(boxes[index].ToRect());
                    }

                    _cachedRect = result;
                }
                else
                {
                    _cachedRect = EmptyRangeRect();
                }
            }

            return _cachedRect.Value;
        }
    }

    /// Test-only hook that places both selection edges directly, mirroring the
    /// state Dart's tests reach through `SelectionEdgeUpdateEvent`s.
    internal void DebugSetSelection(int baseOffset, int extentOffset)
    {
        TextSelectionStart = new TextPosition(baseOffset);
        TextSelectionEnd = new TextPosition(extentOffset);
        DidChangeSelection();
    }

    internal void DidChangeParagraphLayout()
    {
        _cachedRect = null;
        _cachedBoundingBoxes = null;
    }

    private Rect EmptyRangeRect()
    {
        Point offset = _paragraph.GetPositionOffset(new TextPosition(_range.Start));
        return new Rect(
            new Point(offset.X, offset.Y - _paragraph.PreferredLineHeight),
            offset);
    }

    private bool BoundingBoxesContains(Point position)
    {
        return BoundingBoxes.Any(rect => SelectionUtils.RectContains(rect, position));
    }

    // -- Event dispatch -------------------------------------------------------

    public SelectionResult DispatchSelectionEvent(SelectionEvent @event)
    {
        SelectionResult result;
        TextPosition? existingSelectionStart = TextSelectionStart;
        TextPosition? existingSelectionEnd = TextSelectionEnd;
        switch (@event)
        {
            case SelectionEdgeUpdateEvent edgeUpdate:
                bool isEnd = edgeUpdate.Type == SelectionEventType.EndEdgeUpdate;
                result = edgeUpdate.Granularity switch
                {
                    TextGranularity.Character => UpdateSelectionEdge(edgeUpdate.GlobalPosition, isEnd),
                    TextGranularity.Word => UpdateSelectionEdgeByTextBoundary(
                        edgeUpdate.GlobalPosition,
                        isEnd,
                        GetWordBoundaryAtPosition),
                    TextGranularity.Paragraph => UpdateSelectionEdgeByMultiSelectableTextBoundary(
                        edgeUpdate.GlobalPosition,
                        isEnd),
                    _ => throw new InvalidOperationException(
                        "Moving the selection edge by line or document is not supported."),
                };
                break;
            case ClearSelectionEvent:
                result = HandleClearSelection();
                break;
            case SelectAllSelectionEvent:
                result = HandleSelectAll();
                break;
            case SelectWordSelectionEvent selectWord:
                result = HandleSelectWord(selectWord.GlobalPosition);
                break;
            case SelectParagraphSelectionEvent selectParagraph:
                if (selectParagraph.Absorb)
                {
                    HandleSelectAll();
                    result = SelectionResult.Next;
                    _selectableContainsOriginTextBoundary = true;
                }
                else
                {
                    result = HandleSelectParagraph(selectParagraph.GlobalPosition);
                }

                break;
            case GranularlyExtendSelectionEvent granular:
                result = HandleGranularlyExtendSelection(granular.Forward, granular.IsEnd, granular.Granularity);
                break;
            case DirectionallyExtendSelectionEvent directional:
                result = HandleDirectionallyExtendSelection(
                    directional.Dx,
                    directional.IsEnd,
                    directional.Direction);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(@event));
        }

        if (existingSelectionStart != TextSelectionStart || existingSelectionEnd != TextSelectionEnd)
        {
            DidChangeSelection();
        }

        return result;
    }

    private SelectionResult HandleClearSelection()
    {
        TextSelectionStart = null;
        TextSelectionEnd = null;
        _selectableContainsOriginTextBoundary = false;
        return SelectionResult.None;
    }

    private SelectionResult HandleSelectAll()
    {
        TextSelectionStart = new TextPosition(_range.Start);
        TextSelectionEnd = new TextPosition(_range.End, TextAffinity.Upstream);
        return SelectionResult.None;
    }

    private SelectionResult HandleSelectWord(Point globalPosition)
    {
        TextPosition position = _paragraph.GetPositionForOffset(_paragraph.GlobalToLocal(globalPosition));
        if (PositionIsWithinCurrentSelection(position) && TextSelectionStart != TextSelectionEnd)
        {
            return SelectionResult.End;
        }

        return HandleSelectTextBoundary(GetWordBoundaryAtPosition(position));
    }

    private SelectionResult HandleSelectParagraph(Point globalPosition)
    {
        Point localPosition = _paragraph.GlobalToLocal(globalPosition);
        TextPosition position = _paragraph.GetPositionForOffset(localPosition);
        return HandleSelectMultiFragmentTextBoundary(GetParagraphBoundaryAtPosition(position, _fullText));
    }

    private SelectionResult HandleSelectTextBoundary(TextBoundaryRecord boundary)
    {
        if (boundary.BoundaryStart.Offset < _range.Start && boundary.BoundaryEnd.Offset <= _range.Start)
        {
            return SelectionResult.Previous;
        }

        if (boundary.BoundaryStart.Offset >= _range.End && boundary.BoundaryEnd.Offset > _range.End)
        {
            return SelectionResult.Next;
        }

        TextSelectionStart = boundary.BoundaryStart;
        TextSelectionEnd = boundary.BoundaryEnd;
        _selectableContainsOriginTextBoundary = true;
        return SelectionResult.End;
    }

    private SelectionResult HandleSelectMultiFragmentTextBoundary(TextBoundaryRecord boundary)
    {
        if (boundary.BoundaryStart.Offset < _range.Start && boundary.BoundaryEnd.Offset <= _range.Start)
        {
            return SelectionResult.Previous;
        }

        if (boundary.BoundaryStart.Offset >= _range.End && boundary.BoundaryEnd.Offset > _range.End)
        {
            return SelectionResult.Next;
        }

        TextRange? intersection = Intersect(
            _range,
            new TextRange(boundary.BoundaryStart.Offset, boundary.BoundaryEnd.Offset));
        if (intersection is { } intersectRange)
        {
            TextSelectionStart = new TextPosition(intersectRange.Start);
            TextSelectionEnd = new TextPosition(intersectRange.End);
            _selectableContainsOriginTextBoundary = true;
            return _range.End < boundary.BoundaryEnd.Offset ? SelectionResult.Next : SelectionResult.End;
        }

        return SelectionResult.None;
    }

    private static TextRange? Intersect(TextRange a, TextRange b)
    {
        int startMax = Math.Max(a.Start, b.Start);
        int endMin = Math.Min(a.End, b.End);
        return startMax <= endMin ? new TextRange(startMax, endMin) : null;
    }

    // -- Selection edge updates ----------------------------------------------

    private SelectionResult UpdateSelectionEdge(Point globalPosition, bool isEnd)
    {
        SetSelectionPosition(null, isEnd);
        if (!TryGlobalToLocal(globalPosition, out Point localPosition))
        {
            return SelectionResult.None;
        }

        if (Rect.Width <= 0 || Rect.Height <= 0)
        {
            return HandleEmptyRect(localPosition, isEnd);
        }

        Point adjustedOffset = SelectionUtils.AdjustDragOffset(Rect, localPosition, _paragraph.TextDirection);
        TextPosition position = ClampTextPosition(_paragraph.GetPositionForOffset(adjustedOffset));
        SetSelectionPosition(position, isEnd);
        if (position.Offset == _range.End)
        {
            return SelectionResult.Next;
        }

        if (position.Offset == _range.Start)
        {
            return SelectionResult.Previous;
        }

        return SelectionUtils.GetResultBasedOnRect(Rect, localPosition);
    }

    private SelectionResult HandleEmptyRect(Point localPosition, bool isEnd)
    {
        SelectionResult result = SelectionUtils.GetResultBasedOnRect(Rect, localPosition);
        SetSelectionPosition(
            result == SelectionResult.Next
                ? new TextPosition(_range.End)
                : new TextPosition(_range.Start, TextAffinity.Upstream),
            isEnd);
        return result;
    }

    private SelectionResult UpdateSelectionEdgeByTextBoundary(
        Point globalPosition,
        bool isEnd,
        Func<TextPosition, TextBoundaryRecord> getTextBoundary)
    {
        TextPosition? existingSelectionStart = TextSelectionStart;
        TextPosition? existingSelectionEnd = TextSelectionEnd;
        SetSelectionPosition(null, isEnd);
        if (!TryGlobalToLocal(globalPosition, out Point localPosition))
        {
            return SelectionResult.None;
        }

        if (Rect.Width <= 0 || Rect.Height <= 0)
        {
            return HandleEmptyRect(localPosition, isEnd);
        }

        Point adjustedOffset = SelectionUtils.AdjustDragOffset(Rect, localPosition, _paragraph.TextDirection);
        TextPosition position = _paragraph.GetPositionForOffset(adjustedOffset);
        TextBoundaryRecord? textBoundary = SelectionUtils.RectContains(Rect, localPosition)
            ? getTextBoundary(position)
            : null;
        if (textBoundary is { } candidate && IsBoundaryOutsideRange(candidate))
        {
            textBoundary = null;
        }

        TextPosition targetPosition = ClampTextPosition(isEnd
            ? UpdateSelectionEndEdgeByTextBoundary(
                textBoundary,
                getTextBoundary,
                position,
                existingSelectionStart,
                existingSelectionEnd)
            : UpdateSelectionStartEdgeByTextBoundary(
                textBoundary,
                getTextBoundary,
                position,
                existingSelectionStart,
                existingSelectionEnd));
        SetSelectionPosition(targetPosition, isEnd);
        if (targetPosition.Offset == _range.End)
        {
            return SelectionResult.Next;
        }

        if (targetPosition.Offset == _range.Start)
        {
            return SelectionResult.Previous;
        }

        return SelectionUtils.GetResultBasedOnRect(Rect, localPosition);
    }

    private bool IsBoundaryOutsideRange(TextBoundaryRecord boundary)
    {
        return (boundary.BoundaryStart.Offset < _range.Start && boundary.BoundaryEnd.Offset <= _range.Start)
               || (boundary.BoundaryStart.Offset >= _range.End && boundary.BoundaryEnd.Offset > _range.End);
    }

    private TextPosition UpdateSelectionStartEdgeByTextBoundary(
        TextBoundaryRecord? textBoundary,
        Func<TextPosition, TextBoundaryRecord> getTextBoundary,
        TextPosition position,
        TextPosition? existingSelectionStart,
        TextPosition? existingSelectionEnd)
    {
        TextPosition? targetPosition = null;
        if (textBoundary is { } boundary)
        {
            if (_selectableContainsOriginTextBoundary
                && existingSelectionStart is { } start
                && existingSelectionEnd is { } end)
            {
                bool isSamePosition = position.Offset == end.Offset;
                bool isSelectionInverted = start.Offset > end.Offset;
                bool shouldSwapEdges = !isSamePosition && isSelectionInverted != position.Offset > end.Offset;
                if (shouldSwapEdges)
                {
                    targetPosition = position.Offset < end.Offset ? boundary.BoundaryStart : boundary.BoundaryEnd;
                    TextBoundaryRecord localTextBoundary = getTextBoundary(end);
                    SetSelectionPosition(
                        end.Offset == localTextBoundary.BoundaryStart.Offset
                            ? localTextBoundary.BoundaryEnd
                            : localTextBoundary.BoundaryStart,
                        isEnd: true);
                }
                else if (position.Offset < end.Offset)
                {
                    targetPosition = boundary.BoundaryStart;
                }
                else if (position.Offset > end.Offset)
                {
                    targetPosition = boundary.BoundaryEnd;
                }
                else
                {
                    targetPosition = start;
                }
            }
            else if (existingSelectionEnd is { } existingEnd)
            {
                targetPosition = position.Offset < existingEnd.Offset
                    ? boundary.BoundaryStart
                    : boundary.BoundaryEnd;
            }
            else
            {
                targetPosition = ClosestTextBoundary(boundary, position);
            }
        }
        else if (_selectableContainsOriginTextBoundary
                 && existingSelectionStart is { } originStart
                 && existingSelectionEnd is { } originEnd)
        {
            bool isSamePosition = position.Offset == originEnd.Offset;
            bool isSelectionInverted = originStart.Offset > originEnd.Offset;
            bool shouldSwapEdges = !isSamePosition && isSelectionInverted != position.Offset > originEnd.Offset;
            if (shouldSwapEdges)
            {
                TextBoundaryRecord localTextBoundary = getTextBoundary(originEnd);
                SetSelectionPosition(
                    isSelectionInverted ? localTextBoundary.BoundaryEnd : localTextBoundary.BoundaryStart,
                    isEnd: true);
            }
        }

        return targetPosition ?? position;
    }

    private TextPosition UpdateSelectionEndEdgeByTextBoundary(
        TextBoundaryRecord? textBoundary,
        Func<TextPosition, TextBoundaryRecord> getTextBoundary,
        TextPosition position,
        TextPosition? existingSelectionStart,
        TextPosition? existingSelectionEnd)
    {
        TextPosition? targetPosition = null;
        if (textBoundary is { } boundary)
        {
            if (_selectableContainsOriginTextBoundary
                && existingSelectionStart is { } start
                && existingSelectionEnd is { } end)
            {
                bool isSamePosition = position.Offset == start.Offset;
                bool isSelectionInverted = start.Offset > end.Offset;
                bool shouldSwapEdges = !isSamePosition && isSelectionInverted != position.Offset < start.Offset;
                if (shouldSwapEdges)
                {
                    targetPosition = position.Offset < start.Offset ? boundary.BoundaryStart : boundary.BoundaryEnd;
                    TextBoundaryRecord localTextBoundary = getTextBoundary(start);
                    SetSelectionPosition(
                        start.Offset == localTextBoundary.BoundaryStart.Offset
                            ? localTextBoundary.BoundaryEnd
                            : localTextBoundary.BoundaryStart,
                        isEnd: false);
                }
                else if (position.Offset < start.Offset)
                {
                    targetPosition = boundary.BoundaryStart;
                }
                else if (position.Offset > start.Offset)
                {
                    targetPosition = boundary.BoundaryEnd;
                }
                else
                {
                    targetPosition = end;
                }
            }
            else if (existingSelectionStart is { } existingStart)
            {
                targetPosition = position.Offset < existingStart.Offset
                    ? boundary.BoundaryStart
                    : boundary.BoundaryEnd;
            }
            else
            {
                targetPosition = ClosestTextBoundary(boundary, position);
            }
        }
        else if (_selectableContainsOriginTextBoundary
                 && existingSelectionStart is { } originStart
                 && existingSelectionEnd is { } originEnd)
        {
            bool isSamePosition = position.Offset == originStart.Offset;
            bool isSelectionInverted = originStart.Offset > originEnd.Offset;
            bool shouldSwapEdges = isSelectionInverted != position.Offset < originStart.Offset || isSamePosition;
            if (shouldSwapEdges)
            {
                TextBoundaryRecord localTextBoundary = getTextBoundary(originStart);
                SetSelectionPosition(
                    isSelectionInverted ? localTextBoundary.BoundaryStart : localTextBoundary.BoundaryEnd,
                    isEnd: false);
            }
        }

        return targetPosition ?? position;
    }

    private static TextPosition ClosestTextBoundary(TextBoundaryRecord boundary, TextPosition position)
    {
        int startDistance = Math.Abs(position.Offset - boundary.BoundaryStart.Offset);
        int endDistance = Math.Abs(position.Offset - boundary.BoundaryEnd.Offset);
        return startDistance < endDistance ? boundary.BoundaryStart : boundary.BoundaryEnd;
    }

    private SelectionResult UpdateSelectionEdgeByMultiSelectableTextBoundary(Point globalPosition, bool isEnd)
    {
        TextPosition? existingSelectionStart = TextSelectionStart;
        TextPosition? existingSelectionEnd = TextSelectionEnd;
        SetSelectionPosition(null, isEnd);
        if (!TryGlobalToLocal(globalPosition, out Point localPosition))
        {
            return SelectionResult.None;
        }

        if (Rect.Width <= 0 || Rect.Height <= 0)
        {
            return HandleEmptyRect(localPosition, isEnd);
        }

        Point adjustedOffset = SelectionUtils.AdjustDragOffset(Rect, localPosition, _paragraph.TextDirection);
        Point adjustedOffsetRelativeToParagraph = SelectionUtils.AdjustDragOffset(
            _paragraph.PaintBounds,
            localPosition,
            _paragraph.TextDirection);
        TextPosition position = _paragraph.GetPositionForOffset(adjustedOffset);
        TextPosition positionInFullText = _paragraph.GetPositionForOffset(adjustedOffsetRelativeToParagraph);
        bool paragraphContainsPosition = SelectionUtils.RectContains(_paragraph.PaintBounds, localPosition);
        SelectionResult? result = isEnd
            ? UpdateSelectionEndEdgeByMultiSelectableTextBoundary(
                paragraphContainsPosition,
                positionInFullText,
                existingSelectionStart,
                existingSelectionEnd)
            : UpdateSelectionStartEdgeByMultiSelectableTextBoundary(
                paragraphContainsPosition,
                positionInFullText,
                existingSelectionStart,
                existingSelectionEnd);
        if (result is { } resolved)
        {
            return resolved;
        }

        TextBoundaryRecord? textBoundary = BoundingBoxesContains(localPosition)
            ? GetClampedParagraphBoundaryAtPosition(position)
            : null;
        if (textBoundary is { } candidate && IsBoundaryOutsideRange(candidate))
        {
            textBoundary = null;
        }

        TextPosition targetPosition = ClampTextPosition(isEnd
            ? UpdateSelectionEndEdgeByTextBoundary(
                textBoundary,
                GetClampedParagraphBoundaryAtPosition,
                position,
                existingSelectionStart,
                existingSelectionEnd)
            : UpdateSelectionStartEdgeByTextBoundary(
                textBoundary,
                GetClampedParagraphBoundaryAtPosition,
                position,
                existingSelectionStart,
                existingSelectionEnd));
        SetSelectionPosition(targetPosition, isEnd);
        if (targetPosition.Offset == _range.End)
        {
            return SelectionResult.Next;
        }

        if (targetPosition.Offset == _range.Start)
        {
            return SelectionResult.Previous;
        }

        return SelectionUtils.GetResultBasedOnRect(Rect, localPosition);
    }

    private SelectionResult? UpdateSelectionStartEdgeByMultiSelectableTextBoundary(
        bool paragraphContainsPosition,
        TextPosition position,
        TextPosition? existingSelectionStart,
        TextPosition? existingSelectionEnd)
    {
        if (_selectableContainsOriginTextBoundary
            && existingSelectionStart is { } start
            && existingSelectionEnd is { } end)
        {
            bool forwardSelection = end.Offset >= start.Offset;
            TextBoundaryRecord originTextBoundary = GetParagraphBoundaryAtPosition(
                forwardSelection ? new TextPosition(end.Offset - 1, end.Affinity) : end,
                _fullText);
            if (paragraphContainsPosition)
            {
                TextBoundaryRecord boundaryAtPosition = GetParagraphBoundaryAtPosition(position, _fullText);
                int pivotOffset = forwardSelection
                    ? originTextBoundary.BoundaryEnd.Offset
                    : originTextBoundary.BoundaryStart.Offset;
                bool shouldSwapEdges = !forwardSelection != position.Offset > pivotOffset;
                TextPosition targetPosition = position.Offset < pivotOffset
                    ? boundaryAtPosition.BoundaryStart
                    : position.Offset > pivotOffset
                        ? boundaryAtPosition.BoundaryEnd
                        : forwardSelection ? start : end;
                if (shouldSwapEdges)
                {
                    SetSelectionPosition(
                        ClampTextPosition(forwardSelection
                            ? originTextBoundary.BoundaryStart
                            : originTextBoundary.BoundaryEnd),
                        isEnd: true);
                }

                SetSelectionPosition(ClampTextPosition(targetPosition), isEnd: false);
                if (boundaryAtPosition.BoundaryStart.Offset > _range.End
                    && boundaryAtPosition.BoundaryEnd.Offset > _range.End)
                {
                    return SelectionResult.Next;
                }

                if (boundaryAtPosition.BoundaryStart.Offset < _range.Start
                    && boundaryAtPosition.BoundaryEnd.Offset < _range.Start)
                {
                    return SelectionResult.Previous;
                }

                bool finalSelectionIsForward = TextSelectionEnd!.Value.Offset >= TextSelectionStart!.Value.Offset;
                if (finalSelectionIsForward)
                {
                    return boundaryAtPosition.BoundaryStart.Offset >= originTextBoundary.BoundaryStart.Offset
                        ? SelectionResult.End
                        : SelectionResult.Previous;
                }

                return boundaryAtPosition.BoundaryEnd.Offset <= originTextBoundary.BoundaryEnd.Offset
                    ? SelectionResult.End
                    : SelectionResult.Next;
            }

            TextPosition clampedPosition = ClampTextPosition(position);
            if (forwardSelection && clampedPosition.Offset == _range.Start)
            {
                SetSelectionPosition(clampedPosition, isEnd: false);
                return SelectionResult.Previous;
            }

            if (!forwardSelection && clampedPosition.Offset == _range.End)
            {
                SetSelectionPosition(clampedPosition, isEnd: false);
                return SelectionResult.Next;
            }

            if (forwardSelection && clampedPosition.Offset == _range.End)
            {
                SetSelectionPosition(ClampTextPosition(originTextBoundary.BoundaryStart), isEnd: true);
                SetSelectionPosition(clampedPosition, isEnd: false);
                return SelectionResult.Next;
            }

            if (!forwardSelection && clampedPosition.Offset == _range.Start)
            {
                SetSelectionPosition(ClampTextPosition(originTextBoundary.BoundaryEnd), isEnd: true);
                SetSelectionPosition(clampedPosition, isEnd: false);
                return SelectionResult.Previous;
            }

            return null;
        }

        bool positionOnPlaceholder = _paragraph.GetWordBoundary(position).TextInside(_fullText)
                                     == PlaceholderCharacter.ToString();
        if (!paragraphContainsPosition || positionOnPlaceholder)
        {
            return null;
        }

        if (existingSelectionEnd is { } currentEnd)
        {
            TextBoundaryRecord boundaryAtPosition = GetParagraphBoundaryAtPosition(position, _fullText);
            bool backwardSelection = (existingSelectionStart is null && currentEnd.Offset == _range.Start)
                                     || (existingSelectionStart == currentEnd && currentEnd.Offset == _range.Start)
                                     || (existingSelectionStart is { } s && s.Offset > currentEnd.Offset);
            if (boundaryAtPosition.BoundaryStart.Offset < _range.Start
                && boundaryAtPosition.BoundaryEnd.Offset < _range.Start)
            {
                SetSelectionPosition(new TextPosition(_range.Start), isEnd: false);
                return SelectionResult.Previous;
            }

            if (boundaryAtPosition.BoundaryStart.Offset > _range.End
                && boundaryAtPosition.BoundaryEnd.Offset > _range.End)
            {
                SetSelectionPosition(new TextPosition(_range.End), isEnd: false);
                return SelectionResult.Next;
            }

            if (backwardSelection)
            {
                if (boundaryAtPosition.BoundaryEnd.Offset <= _range.End)
                {
                    SetSelectionPosition(ClampTextPosition(boundaryAtPosition.BoundaryEnd), isEnd: false);
                    return SelectionResult.End;
                }

                SetSelectionPosition(new TextPosition(_range.End), isEnd: false);
                return SelectionResult.Next;
            }

            SetSelectionPosition(ClampTextPosition(boundaryAtPosition.BoundaryStart), isEnd: false);
            return boundaryAtPosition.BoundaryStart.Offset < _range.Start
                ? SelectionResult.Previous
                : SelectionResult.End;
        }

        return null;
    }

    private SelectionResult? UpdateSelectionEndEdgeByMultiSelectableTextBoundary(
        bool paragraphContainsPosition,
        TextPosition position,
        TextPosition? existingSelectionStart,
        TextPosition? existingSelectionEnd)
    {
        if (_selectableContainsOriginTextBoundary
            && existingSelectionStart is { } start
            && existingSelectionEnd is { } end)
        {
            bool forwardSelection = end.Offset >= start.Offset;
            TextBoundaryRecord originTextBoundary = GetParagraphBoundaryAtPosition(
                forwardSelection ? start : new TextPosition(start.Offset - 1, start.Affinity),
                _fullText);
            if (paragraphContainsPosition)
            {
                TextBoundaryRecord boundaryAtPosition = GetParagraphBoundaryAtPosition(position, _fullText);
                int pivotOffset = forwardSelection
                    ? originTextBoundary.BoundaryStart.Offset
                    : originTextBoundary.BoundaryEnd.Offset;
                bool shouldSwapEdges = !forwardSelection != position.Offset < pivotOffset;
                TextPosition targetPosition = position.Offset < pivotOffset
                    ? boundaryAtPosition.BoundaryStart
                    : position.Offset > pivotOffset
                        ? boundaryAtPosition.BoundaryEnd
                        : forwardSelection ? end : start;
                if (shouldSwapEdges)
                {
                    SetSelectionPosition(
                        ClampTextPosition(forwardSelection
                            ? originTextBoundary.BoundaryEnd
                            : originTextBoundary.BoundaryStart),
                        isEnd: false);
                }

                SetSelectionPosition(ClampTextPosition(targetPosition), isEnd: true);
                if (boundaryAtPosition.BoundaryStart.Offset > _range.End
                    && boundaryAtPosition.BoundaryEnd.Offset > _range.End)
                {
                    return SelectionResult.Next;
                }

                if (boundaryAtPosition.BoundaryStart.Offset < _range.Start
                    && boundaryAtPosition.BoundaryEnd.Offset < _range.Start)
                {
                    return SelectionResult.Previous;
                }

                bool finalSelectionIsForward = TextSelectionEnd!.Value.Offset >= TextSelectionStart!.Value.Offset;
                if (finalSelectionIsForward)
                {
                    return boundaryAtPosition.BoundaryEnd.Offset <= originTextBoundary.BoundaryEnd.Offset
                        ? SelectionResult.End
                        : SelectionResult.Next;
                }

                return boundaryAtPosition.BoundaryStart.Offset >= originTextBoundary.BoundaryStart.Offset
                    ? SelectionResult.End
                    : SelectionResult.Previous;
            }

            TextPosition clampedPosition = ClampTextPosition(position);
            if (forwardSelection && clampedPosition.Offset == _range.End)
            {
                SetSelectionPosition(clampedPosition, isEnd: true);
                return SelectionResult.Next;
            }

            if (!forwardSelection && clampedPosition.Offset == _range.Start)
            {
                SetSelectionPosition(clampedPosition, isEnd: true);
                return SelectionResult.Previous;
            }

            if (forwardSelection && clampedPosition.Offset == _range.Start)
            {
                SetSelectionPosition(ClampTextPosition(originTextBoundary.BoundaryEnd), isEnd: false);
                SetSelectionPosition(clampedPosition, isEnd: true);
                return SelectionResult.Previous;
            }

            if (!forwardSelection && clampedPosition.Offset == _range.End)
            {
                SetSelectionPosition(ClampTextPosition(originTextBoundary.BoundaryStart), isEnd: false);
                SetSelectionPosition(clampedPosition, isEnd: true);
                return SelectionResult.Next;
            }

            return null;
        }

        bool positionOnPlaceholder = _paragraph.GetWordBoundary(position).TextInside(_fullText)
                                     == PlaceholderCharacter.ToString();
        if (!paragraphContainsPosition || positionOnPlaceholder)
        {
            return null;
        }

        if (existingSelectionStart is { } currentStart)
        {
            TextBoundaryRecord boundaryAtPosition = GetParagraphBoundaryAtPosition(position, _fullText);
            bool backwardSelection = (existingSelectionEnd is null && currentStart.Offset == _range.End)
                                     || (existingSelectionEnd == currentStart && currentStart.Offset == _range.End)
                                     || (existingSelectionEnd is { } e && currentStart.Offset > e.Offset);
            if (boundaryAtPosition.BoundaryStart.Offset < _range.Start
                && boundaryAtPosition.BoundaryEnd.Offset < _range.Start)
            {
                SetSelectionPosition(new TextPosition(_range.Start), isEnd: true);
                return SelectionResult.Previous;
            }

            if (boundaryAtPosition.BoundaryStart.Offset > _range.End
                && boundaryAtPosition.BoundaryEnd.Offset > _range.End)
            {
                SetSelectionPosition(new TextPosition(_range.End), isEnd: true);
                return SelectionResult.Next;
            }

            if (backwardSelection)
            {
                if (boundaryAtPosition.BoundaryStart.Offset >= _range.Start)
                {
                    SetSelectionPosition(ClampTextPosition(boundaryAtPosition.BoundaryStart), isEnd: true);
                    return SelectionResult.End;
                }

                SetSelectionPosition(new TextPosition(_range.Start), isEnd: true);
                return SelectionResult.Previous;
            }

            SetSelectionPosition(ClampTextPosition(boundaryAtPosition.BoundaryEnd), isEnd: true);
            return boundaryAtPosition.BoundaryEnd.Offset > _range.End
                ? SelectionResult.Next
                : SelectionResult.End;
        }

        return null;
    }

    // -- Granular and directional extension -----------------------------------

    private SelectionResult HandleGranularlyExtendSelection(bool forward, bool isExtent, TextGranularity granularity)
    {
        TextSelectionEnd ??= forward
            ? new TextPosition(_range.Start)
            : new TextPosition(_range.End, TextAffinity.Upstream);
        TextSelectionStart ??= TextSelectionEnd;
        TextPosition targetedEdge = isExtent ? TextSelectionEnd!.Value : TextSelectionStart!.Value;
        if (forward && targetedEdge.Offset == _range.End)
        {
            return SelectionResult.Next;
        }

        if (!forward && targetedEdge.Offset == _range.Start)
        {
            return SelectionResult.Previous;
        }

        TextPosition newPosition;
        SelectionResult result;
        switch (granularity)
        {
            case TextGranularity.Character:
                newPosition = MoveBeyondTextBoundaryAtDirection(
                    targetedEdge,
                    forward,
                    new CharacterBoundary(_range.TextInside(_fullText)));
                result = SelectionResult.End;
                break;
            case TextGranularity.Word:
                newPosition = MoveBeyondTextBoundaryAtDirection(targetedEdge, forward, _paragraph.MoveByWordBoundary);
                result = SelectionResult.End;
                break;
            case TextGranularity.Paragraph:
                newPosition = MoveBeyondTextBoundaryAtDirection(
                    targetedEdge,
                    forward,
                    new ParagraphBoundary(_range.TextInside(_fullText)));
                result = SelectionResult.End;
                break;
            case TextGranularity.Line:
                newPosition = MoveToTextBoundaryAtDirection(targetedEdge, forward, new LineBoundary(this));
                result = SelectionResult.End;
                break;
            case TextGranularity.Document:
                newPosition = MoveBeyondTextBoundaryAtDirection(
                    targetedEdge,
                    forward,
                    new DocumentBoundary(_range.TextInside(_fullText)));
                result = forward && newPosition.Offset == _range.End
                    ? SelectionResult.Next
                    : !forward && newPosition.Offset == _range.Start
                        ? SelectionResult.Previous
                        : SelectionResult.End;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(granularity));
        }

        SetSelectionPosition(newPosition, isExtent);
        return result;
    }

    private TextPosition MoveBeyondTextBoundaryAtDirection(TextPosition end, bool forward, TextBoundary boundary)
    {
        return new TextPosition(forward
            ? boundary.GetTrailingTextBoundaryAt(end.Offset) ?? _range.End
            : boundary.GetLeadingTextBoundaryAt(end.Offset - 1) ?? _range.Start);
    }

    private TextPosition MoveToTextBoundaryAtDirection(TextPosition end, bool forward, TextBoundary boundary)
    {
        int caretOffset;
        if (end.Affinity == TextAffinity.Upstream)
        {
            if (end.Offset < 1 && !forward)
            {
                return new TextPosition(0);
            }

            int leading = new CharacterBoundary(_fullText).GetLeadingTextBoundaryAt(_range.Start + end.Offset)
                          ?? _range.Start;
            caretOffset = Math.Max(0, leading) - 1;
        }
        else
        {
            caretOffset = end.Offset;
        }

        return new TextPosition(forward
            ? boundary.GetTrailingTextBoundaryAt(caretOffset) ?? _range.End
            : boundary.GetLeadingTextBoundaryAt(caretOffset) ?? _range.Start);
    }

    private SelectionResult HandleDirectionallyExtendSelection(
        double horizontalBaseline,
        bool isExtent,
        SelectionExtendDirection movement)
    {
        Matrix4 transform = _paragraph.GetTransformTo(null);
        Matrix4? inverse = Matrix4.TryInvert(transform);
        if (inverse is null)
        {
            return movement is SelectionExtendDirection.PreviousLine or SelectionExtendDirection.Backward
                ? SelectionResult.Previous
                : SelectionResult.Next;
        }

        double baselineInParagraphCoordinates =
            MatrixUtils.TransformPoint(inverse, new Point(horizontalBaseline, 0)).X;
        TextPosition newPosition;
        SelectionResult result;
        switch (movement)
        {
            case SelectionExtendDirection.PreviousLine:
            case SelectionExtendDirection.NextLine:
                TextPosition verticalEdge = isExtent ? TextSelectionEnd!.Value : TextSelectionStart!.Value;
                (newPosition, result) = HandleVerticalMovement(
                    verticalEdge,
                    baselineInParagraphCoordinates,
                    below: movement == SelectionExtendDirection.NextLine);
                break;
            case SelectionExtendDirection.Forward:
            case SelectionExtendDirection.Backward:
                TextSelectionEnd ??= movement == SelectionExtendDirection.Forward
                    ? new TextPosition(_range.Start)
                    : new TextPosition(_range.End, TextAffinity.Upstream);
                TextSelectionStart ??= TextSelectionEnd;
                TextPosition targetedEdge = isExtent ? TextSelectionEnd!.Value : TextSelectionStart!.Value;
                Point edgeOffset = _paragraph.GetPositionOffset(targetedEdge);
                newPosition = _paragraph.GetPositionForOffset(new Point(
                    baselineInParagraphCoordinates,
                    edgeOffset.Y - (_paragraph.PreferredLineHeight / 2)));
                result = SelectionResult.End;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(movement));
        }

        SetSelectionPosition(newPosition, isExtent);
        return result;
    }

    private (TextPosition Position, SelectionResult Result) HandleVerticalMovement(
        TextPosition position,
        double horizontalBaselineInParagraphCoordinates,
        bool below)
    {
        IReadOnlyList<LineMetrics> lines = _paragraph.ComputeLineMetrics();
        if (lines.Count == 0)
        {
            return (position, SelectionResult.End);
        }

        Point offset = _paragraph.GetOffsetForCaret(position, default);
        int currentLine = lines.Count - 1;
        foreach (LineMetrics lineMetrics in lines)
        {
            if (lineMetrics.Baseline > offset.Y)
            {
                currentLine = lineMetrics.LineNumber;
                break;
            }
        }

        TextPosition newPosition;
        if (below && currentLine == lines.Count - 1)
        {
            newPosition = new TextPosition(_range.End, TextAffinity.Upstream);
        }
        else if (!below && currentLine == 0)
        {
            newPosition = new TextPosition(_range.Start);
        }
        else
        {
            newPosition = ClampTextPosition(_paragraph.GetPositionForOffset(new Point(
                horizontalBaselineInParagraphCoordinates,
                lines[below ? currentLine + 1 : currentLine - 1].Baseline)));
        }

        SelectionResult result = newPosition.Offset == _range.Start
            ? SelectionResult.Previous
            : newPosition.Offset == _range.End
                ? SelectionResult.Next
                : SelectionResult.End;
        return (newPosition, result);
    }

    // -- Text boundary helpers -------------------------------------------------

    private TextBoundaryRecord GetWordBoundaryAtPosition(TextPosition position)
    {
        TextRange word = _paragraph.GetWordBoundary(position);
        return AdjustTextBoundaryAtPosition(word, position);
    }

    private static TextBoundaryRecord AdjustTextBoundaryAtPosition(TextRange textBoundary, TextPosition position)
    {
        if (position.Offset > textBoundary.End)
        {
            return new TextBoundaryRecord(
                new TextPosition(position.Offset),
                new TextPosition(position.Offset));
        }

        return new TextBoundaryRecord(
            new TextPosition(textBoundary.Start),
            new TextPosition(textBoundary.End, TextAffinity.Upstream));
    }

    private static TextBoundaryRecord GetParagraphBoundaryAtPosition(TextPosition position, string text)
    {
        var paragraphBoundary = new ParagraphBoundary(text);
        int paragraphStart = paragraphBoundary.GetLeadingTextBoundaryAt(
            position.Offset == text.Length || position.Affinity == TextAffinity.Upstream
                ? position.Offset - 1
                : position.Offset) ?? 0;
        int paragraphEnd = paragraphBoundary.GetTrailingTextBoundaryAt(position.Offset) ?? text.Length;
        return AdjustTextBoundaryAtPosition(new TextRange(paragraphStart, paragraphEnd), position);
    }

    private TextBoundaryRecord GetClampedParagraphBoundaryAtPosition(TextPosition position)
    {
        var paragraphBoundary = new ParagraphBoundary(_fullText);
        int paragraphStart = paragraphBoundary.GetLeadingTextBoundaryAt(
            position.Offset == _fullText.Length || position.Affinity == TextAffinity.Upstream
                ? position.Offset - 1
                : position.Offset) ?? 0;
        int paragraphEnd = paragraphBoundary.GetTrailingTextBoundaryAt(position.Offset) ?? _fullText.Length;
        paragraphStart = paragraphStart < _range.Start
            ? _range.Start
            : paragraphStart > _range.End
                ? _range.End
                : paragraphStart;
        paragraphEnd = paragraphEnd > _range.End
            ? _range.End
            : paragraphEnd < _range.Start
                ? _range.Start
                : paragraphEnd;
        return AdjustTextBoundaryAtPosition(new TextRange(paragraphStart, paragraphEnd), position);
    }

    private TextPosition ClampTextPosition(TextPosition position)
    {
        if (position.Offset > _range.End
            || (position.Offset == _range.End && position.Affinity == TextAffinity.Downstream))
        {
            return new TextPosition(_range.End, TextAffinity.Upstream);
        }

        if (position.Offset < _range.Start)
        {
            return new TextPosition(_range.Start);
        }

        return position;
    }

    private void SetSelectionPosition(TextPosition? position, bool isEnd)
    {
        if (isEnd)
        {
            TextSelectionEnd = position;
        }
        else
        {
            TextSelectionStart = position;
        }
    }

    private bool PositionIsWithinCurrentSelection(TextPosition position)
    {
        if (TextSelectionStart is not { } start || TextSelectionEnd is not { } end)
        {
            return false;
        }

        TextPosition currentStart;
        TextPosition currentEnd;
        if (CompareTextPositions(start, end) > 0)
        {
            currentStart = start;
            currentEnd = end;
        }
        else
        {
            currentStart = end;
            currentEnd = start;
        }

        return CompareTextPositions(currentStart, position) >= 0 && CompareTextPositions(currentEnd, position) <= 0;
    }

    private static int CompareTextPositions(TextPosition position, TextPosition other)
    {
        if (position.Offset < other.Offset)
        {
            return 1;
        }

        if (position.Offset > other.Offset)
        {
            return -1;
        }

        if (position.Affinity == other.Affinity)
        {
            return 0;
        }

        return position.Affinity == TextAffinity.Upstream ? 1 : -1;
    }

    private bool TryGlobalToLocal(Point globalPosition, out Point localPosition)
    {
        Matrix4 transform = _paragraph.GetTransformTo(null);
        Matrix4? inverse = Matrix4.TryInvert(transform);
        if (inverse is null)
        {
            localPosition = default;
            return false;
        }

        localPosition = MatrixUtils.TransformPoint(inverse, globalPosition);
        return true;
    }

    // -- Content ---------------------------------------------------------------

    public SelectedContent? GetSelectedContent()
    {
        if (TextSelectionStart is not { } start || TextSelectionEnd is not { } end)
        {
            return null;
        }

        int from = Math.Clamp(Math.Min(start.Offset, end.Offset), 0, _fullText.Length);
        int to = Math.Clamp(Math.Max(start.Offset, end.Offset), from, _fullText.Length);
        return new SelectedContent(_fullText[from..to]);
    }

    public SelectedContentRange? GetSelection()
    {
        if (TextSelectionStart is not { } start || TextSelectionEnd is not { } end)
        {
            return null;
        }

        return new SelectedContentRange(start.Offset, end.Offset);
    }

    // -- ITextLayoutMetrics ----------------------------------------------------

    public TextSelection GetLineAtOffset(TextPosition position)
    {
        TextRange line = _paragraph.GetLineBoundary(position);
        return new TextSelection(
            Math.Clamp(line.Start, _range.Start, _range.End),
            Math.Clamp(line.End, _range.Start, _range.End));
    }

    public TextRange GetWordBoundary(TextPosition position) => _paragraph.GetWordBoundary(position);

    public TextPosition GetTextPositionAbove(TextPosition position)
    {
        return ClampTextPosition(_paragraph.GetTextPositionAbove(position));
    }

    public TextPosition GetTextPositionBelow(TextPosition position)
    {
        return ClampTextPosition(_paragraph.GetTextPositionBelow(position));
    }

    // -- Geometry and paint ----------------------------------------------------

    private void DidChangeSelection()
    {
        _paragraph.MarkSelectionNeedsPaint();
        UpdateSelectionGeometry();
    }

    private void UpdateSelectionGeometry()
    {
        SelectionGeometry newValue = GetSelectionGeometry();
        if (_selectionGeometry.Equals(newValue))
        {
            return;
        }

        _selectionGeometry = newValue;
        NotifyListeners();
    }

    private SelectionGeometry GetSelectionGeometry()
    {
        if (TextSelectionStart is not { } start || TextSelectionEnd is not { } end)
        {
            return new SelectionGeometry(SelectionStatus.None, hasContent: true);
        }

        int selectionStart = start.Offset;
        int selectionEnd = end.Offset;
        bool isReversed = selectionStart > selectionEnd;
        Point startOffsetInParagraphCoordinates = _paragraph.GetPositionOffset(start);
        Point endOffsetInParagraphCoordinates = selectionStart == selectionEnd
            ? startOffsetInParagraphCoordinates
            : _paragraph.GetPositionOffset(end);
        bool flipHandles = isReversed != (_paragraph.TextDirection == TextDirection.Rtl);
        var selection = new TextSelection(selectionStart, selectionEnd);
        IReadOnlyList<Rect> selectionRects = _paragraph
            .GetBoxesForSelection(selection)
            .Select(box => box.ToRect())
            .ToList();
        bool selectionCollapsed = selectionStart == selectionEnd;
        TextSelectionHandleType startHandleType;
        TextSelectionHandleType endHandleType;
        if (selectionCollapsed)
        {
            startHandleType = TextSelectionHandleType.Collapsed;
            endHandleType = TextSelectionHandleType.Collapsed;
        }
        else if (flipHandles)
        {
            startHandleType = TextSelectionHandleType.Right;
            endHandleType = TextSelectionHandleType.Left;
        }
        else
        {
            startHandleType = TextSelectionHandleType.Left;
            endHandleType = TextSelectionHandleType.Right;
        }

        return new SelectionGeometry(
            status: selectionCollapsed ? SelectionStatus.Collapsed : SelectionStatus.Uncollapsed,
            hasContent: true,
            startSelectionPoint: new SelectionPoint(
                startOffsetInParagraphCoordinates,
                _paragraph.PreferredLineHeight,
                startHandleType),
            endSelectionPoint: new SelectionPoint(
                endOffsetInParagraphCoordinates,
                _paragraph.PreferredLineHeight,
                endHandleType),
            selectionRects: selectionRects);
    }

    public void PushHandleLayers(LayerLink? startHandle, LayerLink? endHandle)
    {
        if (!_paragraph.Attached)
        {
            return;
        }

        if (!ReferenceEquals(_startHandleLayerLink, startHandle))
        {
            _startHandleLayerLink = startHandle;
            _paragraph.MarkSelectionNeedsPaint();
        }

        if (!ReferenceEquals(_endHandleLayerLink, endHandle))
        {
            _endHandleLayerLink = endHandle;
            _paragraph.MarkSelectionNeedsPaint();
        }
    }

    internal void PaintSelection(PaintingContext context, Point offset)
    {
        if (TextSelectionStart is not { } start || TextSelectionEnd is not { } end)
        {
            return;
        }

        if (_paragraph.SelectionColor is not { } color)
        {
            return;
        }

        var selection = new TextSelection(start.Offset, end.Offset);
        var brush = new SolidColorBrush(color);
        foreach (TextBox box in _paragraph.GetBoxesForSelection(selection))
        {
            Rect rect = box.ToRect();
            context.DrawRectangle(brush, null, new Rect(rect.Position + offset, rect.Size));
        }
    }

    internal void PaintHandles(PaintingContext context, Point offset)
    {
        if (TextSelectionStart is null || TextSelectionEnd is null)
        {
            return;
        }

        if (_startHandleLayerLink is { } startLink && Value.StartSelectionPoint is { } startPoint)
        {
            context.PushLayer(new LeaderLayer(startLink, offset + startPoint.LocalPosition), _ => { });
        }

        if (_endHandleLayerLink is { } endLink && Value.EndSelectionPoint is { } endPoint)
        {
            context.PushLayer(new LeaderLayer(endLink, offset + endPoint.LocalPosition), _ => { });
        }
    }
}

/// The start and end positions of a text boundary, matching Dart's private
/// `_TextBoundaryRecord` record type.
internal readonly record struct TextBoundaryRecord(TextPosition BoundaryStart, TextPosition BoundaryEnd);
