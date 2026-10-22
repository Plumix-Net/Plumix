using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/paragraph.dart

namespace Plumix;

internal sealed partial class SelectableFragment
{
    private bool IsPlaceholder()
    {
        RenderObject? current = _paragraph.Parent;
        while (current is not null)
        {
            if (current is RenderParagraph) return true;
            current = current.Parent;
        }

        return false;
    }

    private RenderParagraph GetOriginParagraph()
    {
        DebugAssertions.Assert(_selectableContainsOriginTextBoundary);
        RenderObject? current = _paragraph.Parent;
        RenderParagraph? originParagraph = null;
        while (current is not null)
        {
            if (current is RenderParagraph { LastSelectableFragments: { } fragments } candidate)
            {
                bool containsOrigin = false;
                foreach (SelectableFragment fragment in fragments)
                {
                    if (fragment._selectableContainsOriginTextBoundary)
                    {
                        containsOrigin = true;
                        originParagraph = candidate;
                        break;
                    }
                }

                if (!containsOrigin) return originParagraph ?? _paragraph;
            }
            current = current.Parent;
        }

        return originParagraph ?? _paragraph;
    }

    private (RenderParagraph Paragraph, Point LocalPosition)? GetParagraphContainingPosition(Point globalPosition)
    {
        RenderObject? current = _paragraph;
        while (current is not null)
        {
            if (current is RenderParagraph candidate)
            {
                Matrix4 transform = candidate.GetTransformTo(null);
                transform.Invert();
                Point localPosition = MatrixUtils.TransformPoint(transform, globalPosition);
                if (SelectionUtils.RectContains(candidate.PaintBounds, localPosition))
                {
                    return (candidate, localPosition);
                }
            }
            current = current.Parent;
        }

        return null;
    }

    private TextPosition GetPositionInParagraph(RenderParagraph targetParagraph)
    {
        Matrix4 transform = _paragraph.GetTransformTo(targetParagraph);
        Rect bounds = _paragraph.PaintBounds;
        var centerLeft = new Point(bounds.Left, bounds.Center.Y);
        Point position = MatrixUtils.TransformPoint(transform, centerLeft);
        return targetParagraph.GetPositionForOffset(position);
    }

    private SelectionResult? UpdateSelectionStartEdgeAtPlaceholderByMultiSelectableTextBoundary(
        Point globalPosition,
        bool paragraphContainsPosition,
        TextPosition position,
        TextPosition? existingSelectionStart,
        TextPosition? existingSelectionEnd)
    {
        const bool isEnd = false;
        if (_selectableContainsOriginTextBoundary &&
            existingSelectionStart != null &&
            existingSelectionEnd != null)
        {
            bool forwardSelection = existingSelectionEnd.Value.Offset >= existingSelectionStart.Value.Offset;
            RenderParagraph originParagraph = GetOriginParagraph();
            bool fragmentBelongsToOriginParagraph = originParagraph == _paragraph;
            if (fragmentBelongsToOriginParagraph)
            {
                return UpdateSelectionStartEdgeByMultiSelectableTextBoundary(
                    paragraphContainsPosition,
                    position,
                    existingSelectionStart,
                    existingSelectionEnd);
            }

            Matrix4 originTransform = originParagraph.GetTransformTo(null);
            originTransform.Invert();
            Point originParagraphLocalPosition = MatrixUtils.TransformPoint(
                originTransform,
                globalPosition);
            bool positionWithinOriginParagraph = SelectionUtils.RectContains(
                originParagraph.PaintBounds, originParagraphLocalPosition);
            TextPosition positionRelativeToOriginParagraph = originParagraph.GetPositionForOffset(
                originParagraphLocalPosition);
            if (positionWithinOriginParagraph)
            {
                string originText = originParagraph.Text.ToPlainText(includeSemanticsLabels: false);
                TextBoundaryRecord boundaryAtPosition = GetParagraphBoundaryAtPosition(
                    positionRelativeToOriginParagraph,
                    originText);
                TextBoundaryRecord originTextBoundary = GetParagraphBoundaryAtPosition(
                    GetPositionInParagraph(originParagraph),
                    originText);
                TextPosition targetPosition;
                int pivotOffset = forwardSelection
                    ? originTextBoundary.BoundaryEnd.Offset
                    : originTextBoundary.BoundaryStart.Offset;
                bool shouldSwapEdges =
                    !forwardSelection != (positionRelativeToOriginParagraph.Offset > pivotOffset);
                if (positionRelativeToOriginParagraph.Offset < pivotOffset)
                {
                    targetPosition = boundaryAtPosition.BoundaryStart;
                }
                else if (positionRelativeToOriginParagraph.Offset > pivotOffset)
                {
                    targetPosition = boundaryAtPosition.BoundaryEnd;
                }
                else
                {
                    targetPosition = existingSelectionStart.Value;
                }

                if (shouldSwapEdges)
                {
                    SetSelectionPosition(existingSelectionStart, isEnd: true);
                }

                SetSelectionPosition(ClampTextPosition(targetPosition), isEnd: isEnd);
                bool finalSelectionIsForward =
                    TextSelectionEnd!.Value.Offset >= TextSelectionStart!.Value.Offset;
                TextPosition originParagraphPlaceholderTextPosition = GetPositionInParagraph(
                    originParagraph);
                var originParagraphPlaceholderRange = new TextRange(
                    originParagraphPlaceholderTextPosition.Offset,
                    originParagraphPlaceholderTextPosition.Offset + PlaceholderLength);
                if (boundaryAtPosition.BoundaryStart.Offset > originParagraphPlaceholderRange.End &&
                    boundaryAtPosition.BoundaryEnd.Offset > originParagraphPlaceholderRange.End)
                {
                    return SelectionResult.Next;
                }

                if (boundaryAtPosition.BoundaryStart.Offset < originParagraphPlaceholderRange.Start &&
                    boundaryAtPosition.BoundaryEnd.Offset < originParagraphPlaceholderRange.Start)
                {
                    return SelectionResult.Previous;
                }

                if (finalSelectionIsForward)
                {
                    if (boundaryAtPosition.BoundaryEnd.Offset <= originTextBoundary.BoundaryEnd.Offset)
                    {
                        return SelectionResult.End;
                    }

                    if (boundaryAtPosition.BoundaryEnd.Offset > originTextBoundary.BoundaryEnd.Offset)
                    {
                        return SelectionResult.Next;
                    }
                }
                else
                {
                    if (boundaryAtPosition.BoundaryStart.Offset >= originTextBoundary.BoundaryStart.Offset)
                    {
                        return SelectionResult.End;
                    }

                    if (boundaryAtPosition.BoundaryStart.Offset < originTextBoundary.BoundaryStart.Offset)
                    {
                        return SelectionResult.Previous;
                    }
                }
            }
            else
            {
                Point adjustedOffset = SelectionUtils.AdjustDragOffset(
                    originParagraph.PaintBounds,
                    originParagraphLocalPosition,
                    _paragraph.TextDirection);
                TextPosition adjustedPositionRelativeToOriginParagraph = originParagraph
                    .GetPositionForOffset(adjustedOffset);
                TextPosition originParagraphPlaceholderTextPosition = GetPositionInParagraph(
                    originParagraph);
                var originParagraphPlaceholderRange = new TextRange(
                    originParagraphPlaceholderTextPosition.Offset,
                    originParagraphPlaceholderTextPosition.Offset + PlaceholderLength);
                if (forwardSelection &&
                    adjustedPositionRelativeToOriginParagraph.Offset <=
                        originParagraphPlaceholderRange.Start)
                {
                    SetSelectionPosition(new TextPosition(_range.Start), isEnd: isEnd);
                    return SelectionResult.Previous;
                }

                if (!forwardSelection &&
                    adjustedPositionRelativeToOriginParagraph.Offset >=
                        originParagraphPlaceholderRange.End)
                {
                    SetSelectionPosition(new TextPosition(_range.End), isEnd: isEnd);
                    return SelectionResult.Next;
                }

                if (forwardSelection &&
                    adjustedPositionRelativeToOriginParagraph.Offset >=
                        originParagraphPlaceholderRange.End)
                {
                    SetSelectionPosition(existingSelectionStart, isEnd: true);
                    SetSelectionPosition(new TextPosition(_range.End), isEnd: isEnd);
                    return SelectionResult.Next;
                }

                if (!forwardSelection &&
                    adjustedPositionRelativeToOriginParagraph.Offset <=
                        originParagraphPlaceholderRange.Start)
                {
                    SetSelectionPosition(existingSelectionStart, isEnd: true);
                    SetSelectionPosition(new TextPosition(_range.Start), isEnd: isEnd);
                    return SelectionResult.Previous;
                }
            }
        }
        else
        {
            if (paragraphContainsPosition)
            {
                return UpdateSelectionStartEdgeByMultiSelectableTextBoundary(
                    paragraphContainsPosition,
                    position,
                    existingSelectionStart,
                    existingSelectionEnd);
            }

            if (existingSelectionEnd != null)
            {
                (RenderParagraph Paragraph, Point LocalPosition)? targetDetails =
                    GetParagraphContainingPosition(globalPosition);
                if (targetDetails == null)
                {
                    return null;
                }
                RenderParagraph targetParagraph = targetDetails.Value.Paragraph;
                TextPosition positionRelativeToTargetParagraph = targetParagraph.GetPositionForOffset(
                    targetDetails.Value.LocalPosition);
                string targetText = targetParagraph.Text.ToPlainText(includeSemanticsLabels: false);
                bool positionOnPlaceholder =
                    targetParagraph
                        .GetWordBoundary(positionRelativeToTargetParagraph)
                        .TextInside(targetText) ==
                    PlaceholderCharacter.ToString();
                if (positionOnPlaceholder)
                {
                    return null;
                }

                bool backwardSelection =
                    existingSelectionStart == null && existingSelectionEnd.Value.Offset == _range.Start ||
                    existingSelectionStart == existingSelectionEnd &&
                        existingSelectionEnd.Value.Offset == _range.Start ||
                    existingSelectionStart != null &&
                        existingSelectionStart.Value.Offset > existingSelectionEnd.Value.Offset;
                TextBoundaryRecord boundaryAtPositionRelativeToTargetParagraph = GetParagraphBoundaryAtPosition(
                    positionRelativeToTargetParagraph,
                    targetText);
                TextPosition targetParagraphPlaceholderTextPosition = GetPositionInParagraph(
                    targetParagraph);
                var targetParagraphPlaceholderRange = new TextRange(
                    targetParagraphPlaceholderTextPosition.Offset,
                    targetParagraphPlaceholderTextPosition.Offset + PlaceholderLength);
                if (boundaryAtPositionRelativeToTargetParagraph.BoundaryStart.Offset <
                        targetParagraphPlaceholderRange.Start &&
                    boundaryAtPositionRelativeToTargetParagraph.BoundaryEnd.Offset <
                        targetParagraphPlaceholderRange.Start)
                {
                    SetSelectionPosition(new TextPosition(_range.Start), isEnd: isEnd);
                    return SelectionResult.Previous;
                }

                if (boundaryAtPositionRelativeToTargetParagraph.BoundaryStart.Offset >
                        targetParagraphPlaceholderRange.End &&
                    boundaryAtPositionRelativeToTargetParagraph.BoundaryEnd.Offset >
                        targetParagraphPlaceholderRange.End)
                {
                    SetSelectionPosition(new TextPosition(_range.End), isEnd: isEnd);
                    return SelectionResult.Next;
                }

                if (backwardSelection)
                {
                    if (boundaryAtPositionRelativeToTargetParagraph.BoundaryEnd.Offset <=
                        targetParagraphPlaceholderRange.End)
                    {
                        SetSelectionPosition(new TextPosition(_range.End), isEnd: isEnd);
                        return SelectionResult.End;
                    }

                    if (boundaryAtPositionRelativeToTargetParagraph.BoundaryEnd.Offset >
                        targetParagraphPlaceholderRange.End)
                    {
                        SetSelectionPosition(new TextPosition(_range.End), isEnd: isEnd);
                        return SelectionResult.Next;
                    }
                }
                else
                {
                    if (boundaryAtPositionRelativeToTargetParagraph.BoundaryStart.Offset >=
                        targetParagraphPlaceholderRange.Start)
                    {
                        SetSelectionPosition(new TextPosition(_range.Start), isEnd: isEnd);
                        return SelectionResult.End;
                    }

                    if (boundaryAtPositionRelativeToTargetParagraph.BoundaryStart.Offset <
                        targetParagraphPlaceholderRange.Start)
                    {
                        SetSelectionPosition(new TextPosition(_range.Start), isEnd: isEnd);
                        return SelectionResult.Previous;
                    }
                }
            }
        }

        return null;
    }

    private SelectionResult? UpdateSelectionEndEdgeAtPlaceholderByMultiSelectableTextBoundary(
        Point globalPosition,
        bool paragraphContainsPosition,
        TextPosition position,
        TextPosition? existingSelectionStart,
        TextPosition? existingSelectionEnd)
    {
        const bool isEnd = true;
        if (_selectableContainsOriginTextBoundary &&
            existingSelectionStart != null &&
            existingSelectionEnd != null)
        {
            bool forwardSelection = existingSelectionEnd.Value.Offset >= existingSelectionStart.Value.Offset;
            RenderParagraph originParagraph = GetOriginParagraph();
            bool fragmentBelongsToOriginParagraph = originParagraph == _paragraph;
            if (fragmentBelongsToOriginParagraph)
            {
                return UpdateSelectionEndEdgeByMultiSelectableTextBoundary(
                    paragraphContainsPosition,
                    position,
                    existingSelectionStart,
                    existingSelectionEnd);
            }

            Matrix4 originTransform = originParagraph.GetTransformTo(null);
            originTransform.Invert();
            Point originParagraphLocalPosition = MatrixUtils.TransformPoint(
                originTransform,
                globalPosition);
            bool positionWithinOriginParagraph = SelectionUtils.RectContains(
                originParagraph.PaintBounds, originParagraphLocalPosition);
            TextPosition positionRelativeToOriginParagraph = originParagraph.GetPositionForOffset(
                originParagraphLocalPosition);
            if (positionWithinOriginParagraph)
            {
                string originText = originParagraph.Text.ToPlainText(includeSemanticsLabels: false);
                TextBoundaryRecord boundaryAtPosition = GetParagraphBoundaryAtPosition(
                    positionRelativeToOriginParagraph,
                    originText);
                TextBoundaryRecord originTextBoundary = GetParagraphBoundaryAtPosition(
                    GetPositionInParagraph(originParagraph),
                    originText);
                TextPosition targetPosition;
                int pivotOffset = forwardSelection
                    ? originTextBoundary.BoundaryStart.Offset
                    : originTextBoundary.BoundaryEnd.Offset;
                bool shouldSwapEdges =
                    !forwardSelection != (positionRelativeToOriginParagraph.Offset < pivotOffset);
                if (positionRelativeToOriginParagraph.Offset < pivotOffset)
                {
                    targetPosition = boundaryAtPosition.BoundaryStart;
                }
                else if (positionRelativeToOriginParagraph.Offset > pivotOffset)
                {
                    targetPosition = boundaryAtPosition.BoundaryEnd;
                }
                else
                {
                    targetPosition = existingSelectionEnd.Value;
                }

                if (shouldSwapEdges)
                {
                    SetSelectionPosition(existingSelectionEnd, isEnd: false);
                }

                SetSelectionPosition(ClampTextPosition(targetPosition), isEnd: isEnd);
                bool finalSelectionIsForward =
                    TextSelectionEnd!.Value.Offset >= TextSelectionStart!.Value.Offset;
                TextPosition originParagraphPlaceholderTextPosition = GetPositionInParagraph(
                    originParagraph);
                var originParagraphPlaceholderRange = new TextRange(
                    originParagraphPlaceholderTextPosition.Offset,
                    originParagraphPlaceholderTextPosition.Offset + PlaceholderLength);
                if (boundaryAtPosition.BoundaryStart.Offset > originParagraphPlaceholderRange.End &&
                    boundaryAtPosition.BoundaryEnd.Offset > originParagraphPlaceholderRange.End)
                {
                    return SelectionResult.Next;
                }

                if (boundaryAtPosition.BoundaryStart.Offset < originParagraphPlaceholderRange.Start &&
                    boundaryAtPosition.BoundaryEnd.Offset < originParagraphPlaceholderRange.Start)
                {
                    return SelectionResult.Previous;
                }

                if (finalSelectionIsForward)
                {
                    if (boundaryAtPosition.BoundaryEnd.Offset <= originTextBoundary.BoundaryEnd.Offset)
                    {
                        return SelectionResult.End;
                    }

                    if (boundaryAtPosition.BoundaryEnd.Offset > originTextBoundary.BoundaryEnd.Offset)
                    {
                        return SelectionResult.Next;
                    }
                }
                else
                {
                    if (boundaryAtPosition.BoundaryStart.Offset >= originTextBoundary.BoundaryStart.Offset)
                    {
                        return SelectionResult.End;
                    }

                    if (boundaryAtPosition.BoundaryStart.Offset < originTextBoundary.BoundaryStart.Offset)
                    {
                        return SelectionResult.Previous;
                    }
                }
            }
            else
            {
                Point adjustedOffset = SelectionUtils.AdjustDragOffset(
                    originParagraph.PaintBounds,
                    originParagraphLocalPosition,
                    _paragraph.TextDirection);
                TextPosition adjustedPositionRelativeToOriginParagraph = originParagraph
                    .GetPositionForOffset(adjustedOffset);
                TextPosition originParagraphPlaceholderTextPosition = GetPositionInParagraph(
                    originParagraph);
                var originParagraphPlaceholderRange = new TextRange(
                    originParagraphPlaceholderTextPosition.Offset,
                    originParagraphPlaceholderTextPosition.Offset + PlaceholderLength);
                if (forwardSelection &&
                    adjustedPositionRelativeToOriginParagraph.Offset <=
                        originParagraphPlaceholderRange.Start)
                {
                    SetSelectionPosition(existingSelectionEnd, isEnd: false);
                    SetSelectionPosition(new TextPosition(_range.Start), isEnd: isEnd);
                    return SelectionResult.Previous;
                }

                if (!forwardSelection &&
                    adjustedPositionRelativeToOriginParagraph.Offset >=
                        originParagraphPlaceholderRange.End)
                {
                    SetSelectionPosition(existingSelectionEnd, isEnd: false);
                    SetSelectionPosition(new TextPosition(_range.End), isEnd: isEnd);
                    return SelectionResult.Next;
                }

                if (forwardSelection &&
                    adjustedPositionRelativeToOriginParagraph.Offset >=
                        originParagraphPlaceholderRange.End)
                {
                    SetSelectionPosition(new TextPosition(_range.End), isEnd: isEnd);
                    return SelectionResult.Next;
                }

                if (!forwardSelection &&
                    adjustedPositionRelativeToOriginParagraph.Offset <=
                        originParagraphPlaceholderRange.Start)
                {
                    SetSelectionPosition(new TextPosition(_range.Start), isEnd: isEnd);
                    return SelectionResult.Previous;
                }
            }
        }
        else
        {
            if (paragraphContainsPosition)
            {
                return UpdateSelectionEndEdgeByMultiSelectableTextBoundary(
                    paragraphContainsPosition,
                    position,
                    existingSelectionStart,
                    existingSelectionEnd);
            }

            if (existingSelectionStart != null)
            {
                (RenderParagraph Paragraph, Point LocalPosition)? targetDetails =
                    GetParagraphContainingPosition(globalPosition);
                if (targetDetails == null)
                {
                    return null;
                }
                RenderParagraph targetParagraph = targetDetails.Value.Paragraph;
                TextPosition positionRelativeToTargetParagraph = targetParagraph.GetPositionForOffset(
                    targetDetails.Value.LocalPosition);
                string targetText = targetParagraph.Text.ToPlainText(includeSemanticsLabels: false);
                bool positionOnPlaceholder =
                    targetParagraph
                        .GetWordBoundary(positionRelativeToTargetParagraph)
                        .TextInside(targetText) ==
                    PlaceholderCharacter.ToString();
                if (positionOnPlaceholder)
                {
                    return null;
                }

                bool backwardSelection =
                    existingSelectionEnd == null && existingSelectionStart.Value.Offset == _range.End ||
                    existingSelectionStart == existingSelectionEnd &&
                        existingSelectionStart.Value.Offset == _range.End ||
                    existingSelectionEnd != null &&
                        existingSelectionStart.Value.Offset > existingSelectionEnd.Value.Offset;
                TextBoundaryRecord boundaryAtPositionRelativeToTargetParagraph = GetParagraphBoundaryAtPosition(
                    positionRelativeToTargetParagraph,
                    targetText);
                TextPosition targetParagraphPlaceholderTextPosition = GetPositionInParagraph(
                    targetParagraph);
                var targetParagraphPlaceholderRange = new TextRange(
                    targetParagraphPlaceholderTextPosition.Offset,
                    targetParagraphPlaceholderTextPosition.Offset + PlaceholderLength);
                if (boundaryAtPositionRelativeToTargetParagraph.BoundaryStart.Offset <
                        targetParagraphPlaceholderRange.Start &&
                    boundaryAtPositionRelativeToTargetParagraph.BoundaryEnd.Offset <
                        targetParagraphPlaceholderRange.Start)
                {
                    SetSelectionPosition(new TextPosition(_range.Start), isEnd: isEnd);
                    return SelectionResult.Previous;
                }

                if (boundaryAtPositionRelativeToTargetParagraph.BoundaryStart.Offset >
                        targetParagraphPlaceholderRange.End &&
                    boundaryAtPositionRelativeToTargetParagraph.BoundaryEnd.Offset >
                        targetParagraphPlaceholderRange.End)
                {
                    SetSelectionPosition(new TextPosition(_range.End), isEnd: isEnd);
                    return SelectionResult.Next;
                }

                if (backwardSelection)
                {
                    if (boundaryAtPositionRelativeToTargetParagraph.BoundaryStart.Offset >=
                        targetParagraphPlaceholderRange.Start)
                    {
                        SetSelectionPosition(new TextPosition(_range.Start), isEnd: isEnd);
                        return SelectionResult.End;
                    }

                    if (boundaryAtPositionRelativeToTargetParagraph.BoundaryStart.Offset <
                        targetParagraphPlaceholderRange.Start)
                    {
                        SetSelectionPosition(new TextPosition(_range.Start), isEnd: isEnd);
                        return SelectionResult.Previous;
                    }
                }
                else
                {
                    if (boundaryAtPositionRelativeToTargetParagraph.BoundaryEnd.Offset <=
                        targetParagraphPlaceholderRange.End)
                    {
                        SetSelectionPosition(new TextPosition(_range.End), isEnd: isEnd);
                        return SelectionResult.End;
                    }

                    if (boundaryAtPositionRelativeToTargetParagraph.BoundaryEnd.Offset >
                        targetParagraphPlaceholderRange.End)
                    {
                        SetSelectionPosition(new TextPosition(_range.End), isEnd: isEnd);
                        return SelectionResult.Next;
                    }
                }
            }
        }

        return null;
    }
}
