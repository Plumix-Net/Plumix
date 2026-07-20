using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/selectable_region.dart
// flutter/packages/flutter/lib/src/widgets/selection_container.dart

public enum SelectionChangedCause
{
    Tap,
    DoubleTap,
    LongPress,
    Drag,
    Keyboard,
    ForcePress,
    Toolbar,
    StylusHandwriting,
    [Obsolete("Use StylusHandwriting instead.")]
    Scribble = StylusHandwriting,
}

public sealed record SelectedContent(string PlainText);

internal interface ITextSelectionRegistrar
{
    void Register(RenderParagraph paragraph);
    void Unregister(RenderParagraph paragraph);
    void StartSelection(RenderParagraph paragraph, Point globalPosition);
    void UpdateSelection(Point globalPosition);
    void EndSelection();
}

internal sealed class TextSelectionRegistrar : ITextSelectionRegistrar
{
    private readonly HashSet<RenderParagraph> _paragraphs = [];
    private readonly Action<SelectedContent?, TextSelection, SelectionChangedCause?> _onSelectionChanged;
    private RenderParagraph? _anchorParagraph;
    private int _anchorOffset;
    private RenderParagraph? _extentParagraph;
    private int _extentOffset;
    private bool _dragging;
    private Point? _lastGlobalPosition;

    public TextSelectionRegistrar(
        Action<SelectedContent?, TextSelection, SelectionChangedCause?> onSelectionChanged)
    {
        _onSelectionChanged = onSelectionChanged;
    }

    public void Register(RenderParagraph paragraph)
    {
        _paragraphs.Add(paragraph);
    }

    public void Unregister(RenderParagraph paragraph)
    {
        if (!_paragraphs.Remove(paragraph))
        {
            return;
        }

        if (ReferenceEquals(_anchorParagraph, paragraph) || ReferenceEquals(_extentParagraph, paragraph))
        {
            foreach (var remaining in _paragraphs)
            {
                remaining.SetSelection(0, 0);
            }
            _anchorParagraph = null;
            _extentParagraph = null;
            _anchorOffset = 0;
            _extentOffset = 0;
            _dragging = false;
        }
    }

    public void StartSelection(RenderParagraph paragraph, Point globalPosition)
    {
        _anchorParagraph = paragraph;
        _extentParagraph = paragraph;
        _anchorOffset = paragraph.GetTextPosition(globalPosition);
        _extentOffset = _anchorOffset;
        _dragging = true;
        _lastGlobalPosition = globalPosition;
        ApplySelection(SelectionChangedCause.Tap);
    }

    public void UpdateSelection(Point globalPosition)
    {
        if (!_dragging || _anchorParagraph is null)
        {
            return;
        }

        var target = FindParagraph(globalPosition) ?? FindNearestParagraph(globalPosition);
        if (target is null)
        {
            return;
        }

        int offset = target.GetTextPosition(globalPosition);
        if (ReferenceEquals(_extentParagraph, target) && _extentOffset == offset)
        {
            return;
        }

        _extentParagraph = target;
        _extentOffset = offset;
        _lastGlobalPosition = globalPosition;
        ApplySelection(SelectionChangedCause.Drag);
    }

    public void EndSelection()
    {
        _dragging = false;
    }

    public void SelectAll(SelectionChangedCause cause = SelectionChangedCause.Keyboard)
    {
        var ordered = OrderedParagraphs();
        if (ordered.Count == 0)
        {
            return;
        }

        _anchorParagraph = ordered[0];
        _anchorOffset = 0;
        _extentParagraph = ordered[^1];
        _extentOffset = ordered[^1].Text.Length;
        _lastGlobalPosition = null;
        ApplySelection(cause);
    }

    public void SelectWord(SelectionChangedCause cause = SelectionChangedCause.DoubleTap)
    {
        if (_extentParagraph is null)
        {
            return;
        }

        string text = _extentParagraph.Text;
        if (text.Length == 0)
        {
            return;
        }

        int index = Math.Clamp(_extentOffset, 0, text.Length - 1);
        int start = index;
        int end = index;
        bool whitespace = char.IsWhiteSpace(text[index]);
        while (start > 0 && char.IsWhiteSpace(text[start - 1]) == whitespace)
        {
            start--;
        }
        while (end < text.Length && char.IsWhiteSpace(text[end]) == whitespace)
        {
            end++;
        }

        if (!whitespace)
        {
            while (start > 0 && !char.IsWhiteSpace(text[start - 1]))
            {
                start--;
            }
            while (end < text.Length && !char.IsWhiteSpace(text[end]))
            {
                end++;
            }
        }

        _anchorParagraph = _extentParagraph;
        _anchorOffset = start;
        _extentOffset = end;
        ApplySelection(cause);
    }

    public void ClearSelection(SelectionChangedCause cause)
    {
        foreach (var paragraph in _paragraphs)
        {
            paragraph.SetSelection(0, 0);
        }

        _anchorParagraph = null;
        _extentParagraph = null;
        _anchorOffset = 0;
        _extentOffset = 0;
        _dragging = false;
        _lastGlobalPosition = null;
        _onSelectionChanged(null, TextSelection.Collapsed(0), cause);
    }

    public bool HasSelection => !string.IsNullOrEmpty(SelectedText());

    public bool IsEntireSelection
    {
        get
        {
            List<RenderParagraph> ordered = OrderedParagraphs();
            if (!TryGetOrderedEndpoints(
                    ordered,
                    out int startIndex,
                    out int endIndex,
                    out int startOffset,
                    out int endOffset))
            {
                return false;
            }

            return startIndex == 0
                   && endIndex == ordered.Count - 1
                   && startOffset == 0
                   && endOffset == ordered[^1].Text.Length;
        }
    }

    public TextSelectionToolbarAnchors ContextMenuAnchors
    {
        get
        {
            Point anchor = _lastGlobalPosition ?? ResolveFallbackAnchor();
            return new TextSelectionToolbarAnchors(
                PrimaryAnchor: anchor,
                SecondaryAnchor: anchor);
        }
    }

    public string SelectedText()
    {
        var ordered = OrderedParagraphs();
        if (!TryGetOrderedEndpoints(ordered, out int startIndex, out int endIndex, out int startOffset,
                out int endOffset))
        {
            return string.Empty;
        }

        var parts = new List<string>();
        for (int index = startIndex; index <= endIndex; index++)
        {
            RenderParagraph paragraph = ordered[index];
            int start = index == startIndex ? startOffset : 0;
            int end = index == endIndex ? endOffset : paragraph.Text.Length;
            if (end > start)
            {
                parts.Add(paragraph.Text[start..end]);
            }
        }

        return string.Concat(parts);
    }

    public TextSelection FlattenedSelection()
    {
        var ordered = OrderedParagraphs();
        if (_anchorParagraph is null || _extentParagraph is null)
        {
            return TextSelection.Collapsed(0);
        }

        int baseOffset = FlattenOffset(ordered, _anchorParagraph, _anchorOffset);
        int extentOffset = FlattenOffset(ordered, _extentParagraph, _extentOffset);
        return new TextSelection(baseOffset, extentOffset);
    }

    private void ApplySelection(SelectionChangedCause cause)
    {
        var ordered = OrderedParagraphs();
        if (!TryGetOrderedEndpoints(ordered, out int startIndex, out int endIndex, out int startOffset,
                out int endOffset))
        {
            return;
        }

        for (int index = 0; index < ordered.Count; index++)
        {
            RenderParagraph paragraph = ordered[index];
            if (index < startIndex || index > endIndex)
            {
                paragraph.SetSelection(0, 0);
                continue;
            }

            int start = index == startIndex ? startOffset : 0;
            int end = index == endIndex ? endOffset : paragraph.Text.Length;
            paragraph.SetSelection(start, end);
        }

        string selectedText = SelectedText();
        _onSelectionChanged(
            string.IsNullOrEmpty(selectedText) ? null : new SelectedContent(selectedText),
            FlattenedSelection(),
            cause);
    }

    private bool TryGetOrderedEndpoints(
        IReadOnlyList<RenderParagraph> ordered,
        out int startIndex,
        out int endIndex,
        out int startOffset,
        out int endOffset)
    {
        startIndex = -1;
        endIndex = -1;
        startOffset = 0;
        endOffset = 0;
        if (_anchorParagraph is null || _extentParagraph is null)
        {
            return false;
        }

        int anchorIndex = IndexOf(ordered, _anchorParagraph);
        int extentIndex = IndexOf(ordered, _extentParagraph);
        if (anchorIndex < 0 || extentIndex < 0)
        {
            return false;
        }

        bool forward = anchorIndex < extentIndex
                       || (anchorIndex == extentIndex && _anchorOffset <= _extentOffset);
        startIndex = forward ? anchorIndex : extentIndex;
        endIndex = forward ? extentIndex : anchorIndex;
        startOffset = forward ? _anchorOffset : _extentOffset;
        endOffset = forward ? _extentOffset : _anchorOffset;
        return true;
    }

    private RenderParagraph? FindParagraph(Point globalPosition)
    {
        foreach (var paragraph in OrderedParagraphs())
        {
            if (paragraph.ContainsGlobalPosition(globalPosition))
            {
                return paragraph;
            }
        }

        return null;
    }

    private RenderParagraph? FindNearestParagraph(Point globalPosition)
    {
        RenderParagraph? nearest = null;
        double nearestDistance = double.PositiveInfinity;
        foreach (var paragraph in OrderedParagraphs())
        {
            double distance = paragraph.DistanceToGlobalPosition(globalPosition);
            if (distance < nearestDistance)
            {
                nearest = paragraph;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private List<RenderParagraph> OrderedParagraphs()
    {
        RenderObject? root = _paragraphs.FirstOrDefault(paragraph => paragraph.Owner is not null)?.Owner?.Root;
        if (root is null)
        {
            return _paragraphs.ToList();
        }

        var ordered = new List<RenderParagraph>();
        Visit(root);
        return ordered;

        void Visit(RenderObject node)
        {
            if (node is RenderParagraph paragraph && _paragraphs.Contains(paragraph))
            {
                ordered.Add(paragraph);
            }
            node.VisitChildren(Visit);
        }
    }

    private Point ResolveFallbackAnchor()
    {
        RenderParagraph? paragraph = _extentParagraph ?? _anchorParagraph ?? OrderedParagraphs().FirstOrDefault();
        if (paragraph is null || !paragraph.TryGetTransformFromRoot(out Matrix transform))
        {
            return default;
        }

        Rect bounds = RenderObject.TransformRect(transform, new Rect(default, paragraph.Size));
        return new Point(bounds.Center.X, bounds.Top);
    }

    private static int FlattenOffset(
        IReadOnlyList<RenderParagraph> ordered,
        RenderParagraph target,
        int localOffset)
    {
        int offset = 0;
        foreach (var paragraph in ordered)
        {
            if (ReferenceEquals(paragraph, target))
            {
                return offset + Math.Clamp(localOffset, 0, paragraph.Text.Length);
            }
            offset += paragraph.Text.Length;
        }
        return offset;
    }

    private static int IndexOf(IReadOnlyList<RenderParagraph> paragraphs, RenderParagraph target)
    {
        for (int index = 0; index < paragraphs.Count; index++)
        {
            if (ReferenceEquals(paragraphs[index], target))
            {
                return index;
            }
        }
        return -1;
    }
}

internal sealed class SelectionContainer : InheritedWidget
{
    public SelectionContainer(
        TextSelectionRegistrar registrar,
        Color selectionColor,
        Color cursorColor,
        bool showCursor,
        double cursorWidth,
        double? cursorHeight,
        bool enabled,
        Widget child,
        Key? key = null) : base(key)
    {
        Registrar = registrar;
        SelectionColor = selectionColor;
        CursorColor = cursorColor;
        ShowCursor = showCursor;
        CursorWidth = cursorWidth;
        CursorHeight = cursorHeight;
        Enabled = enabled;
        Child = child;
    }

    public TextSelectionRegistrar Registrar { get; }
    public Color SelectionColor { get; }
    public Color CursorColor { get; }
    public bool ShowCursor { get; }
    public double CursorWidth { get; }
    public double? CursorHeight { get; }
    public bool Enabled { get; }
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        var old = (SelectionContainer)oldWidget;
        return !ReferenceEquals(old.Registrar, Registrar)
               || old.SelectionColor != SelectionColor
               || old.CursorColor != CursorColor
               || old.ShowCursor != ShowCursor
               || old.CursorWidth != CursorWidth
               || old.CursorHeight != CursorHeight
               || old.Enabled != Enabled;
    }

    public static SelectionContainer? MaybeOf(BuildContext context)
    {
        return context.DependOnInherited<SelectionContainer>();
    }
}

public sealed class SelectableRegion : StatefulWidget
{
    public SelectableRegion(
        Widget child,
        Color selectionColor,
        Color cursorColor,
        FocusNode? focusNode = null,
        bool autofocus = false,
        bool enabled = true,
        bool showCursor = false,
        double cursorWidth = 2.0,
        double? cursorHeight = null,
        Action<SelectedContent?>? onSelectionChanged = null,
        Action<TextSelection, SelectionChangedCause?>? onTextSelectionChanged = null,
        Action? onTap = null,
        SelectableRegionContextMenuBuilder? contextMenuBuilder = null,
        TextMagnifierConfiguration? magnifierConfiguration = null,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        SelectionColor = selectionColor;
        CursorColor = cursorColor;
        FocusNode = focusNode;
        Autofocus = autofocus;
        Enabled = enabled;
        ShowCursor = showCursor;
        CursorWidth = cursorWidth;
        CursorHeight = cursorHeight;
        OnSelectionChanged = onSelectionChanged;
        OnTextSelectionChanged = onTextSelectionChanged;
        OnTap = onTap;
        ContextMenuBuilder = contextMenuBuilder;
        MagnifierConfiguration = magnifierConfiguration ?? TextMagnifierConfiguration.Disabled;

        if (!double.IsFinite(cursorWidth) || cursorWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cursorWidth));
        }
        if (cursorHeight.HasValue && (!double.IsFinite(cursorHeight.Value) || cursorHeight.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(cursorHeight));
        }
    }

    public Widget Child { get; }
    public Color SelectionColor { get; }
    public Color CursorColor { get; }
    public FocusNode? FocusNode { get; }
    public bool Autofocus { get; }
    public bool Enabled { get; }
    public bool ShowCursor { get; }
    public double CursorWidth { get; }
    public double? CursorHeight { get; }
    public Action<SelectedContent?>? OnSelectionChanged { get; }
    public Action<TextSelection, SelectionChangedCause?>? OnTextSelectionChanged { get; }
    public Action? OnTap { get; }
    public SelectableRegionContextMenuBuilder? ContextMenuBuilder { get; }
    public TextMagnifierConfiguration MagnifierConfiguration { get; }

    public override State CreateState() => new SelectableRegionState();
}

public sealed class SelectableRegionState : State
{
    private TextSelectionRegistrar _registrar = null!;
    private SelectedContent? _selectedContent;
    private FocusNode? _focusNode;
    private bool _ownsFocusNode;
    private readonly ContextMenuController _contextMenuController = new();
    private SelectableRegion Current => (SelectableRegion)StateWidget;

    public SelectedContent? SelectedContent => _selectedContent;

    public IReadOnlyList<ContextMenuButtonItem> ContextMenuButtonItems
    {
        get
        {
            var items = new List<ContextMenuButtonItem>();
            if (_registrar.HasSelection)
            {
                items.Add(new ContextMenuButtonItem(CopyAndHide, ContextMenuButtonType.Copy));
            }
            if (!_registrar.IsEntireSelection)
            {
                items.Add(new ContextMenuButtonItem(SelectAllAndHide, ContextMenuButtonType.SelectAll));
            }
            return items;
        }
    }

    public TextSelectionToolbarAnchors ContextMenuAnchors => _registrar.ContextMenuAnchors;

    public bool ContextMenuIsVisible => _contextMenuController.IsShown;

    public override void InitState()
    {
        _registrar = new TextSelectionRegistrar(HandleSelectionChanged);
        AttachFocusNode(Current.FocusNode);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var oldRegion = (SelectableRegion)oldWidget;
        if (!ReferenceEquals(oldRegion.FocusNode, Current.FocusNode))
        {
            DetachFocusNode();
            AttachFocusNode(Current.FocusNode);
        }
        if (!Current.Enabled
            || oldRegion.ContextMenuBuilder != Current.ContextMenuBuilder)
        {
            HideToolbar();
        }
    }

    public override void Dispose()
    {
        _contextMenuController.Hide();
        DetachFocusNode();
    }

    public override Widget Build(BuildContext context)
    {
        Widget child = new SelectionContainer(
            registrar: _registrar,
            selectionColor: Current.SelectionColor,
            cursorColor: Current.CursorColor,
            showCursor: Current.ShowCursor && _focusNode!.HasFocus,
            cursorWidth: Current.CursorWidth,
            cursorHeight: Current.CursorHeight,
            enabled: Current.Enabled,
            child: Current.Child);

        child = new Focus(
            focusNode: _focusNode,
            autofocus: Current.Autofocus,
            canRequestFocus: Current.Enabled,
            onKeyEvent: HandleKeyEvent,
            child: child);

        return new GestureDetector(
            behavior: HitTestBehavior.Translucent,
            onTap: Current.OnTap,
            onDoubleTap: () => _registrar.SelectWord(),
            onLongPress: () =>
            {
                _registrar.SelectWord(SelectionChangedCause.LongPress);
                ShowToolbar();
            },
            onSecondaryTap: () => ShowToolbar(),
            child: child);
    }

    public void SelectAll() => _registrar.SelectAll();

    public void ClearSelection() => _registrar.ClearSelection(SelectionChangedCause.Keyboard);

    public void CopySelection()
    {
        string text = _registrar.SelectedText();
        if (!string.IsNullOrEmpty(text))
        {
            TextClipboard.SetText(text);
        }
    }

    public bool ShowToolbar()
    {
        if (!Current.Enabled || Current.ContextMenuBuilder is null || ContextMenuButtonItems.Count == 0)
        {
            return false;
        }

        return _contextMenuController.Show(
            Context,
            context => Current.ContextMenuBuilder(context, this));
    }

    public void HideToolbar() => _contextMenuController.Hide();

    private void HandleSelectionChanged(
        SelectedContent? selectedContent,
        TextSelection selection,
        SelectionChangedCause? cause)
    {
        _selectedContent = selectedContent;
        if (selectedContent is null)
        {
            HideToolbar();
        }
        Current.OnSelectionChanged?.Invoke(selectedContent);
        Current.OnTextSelectionChanged?.Invoke(selection, cause);
    }

    private KeyEventResult HandleKeyEvent(FocusNode node, KeyEvent @event)
    {
        if (!Current.Enabled || !@event.IsDown)
        {
            return KeyEventResult.Ignored;
        }

        bool shortcut = @event.IsControlPressed || @event.IsMetaPressed;
        if (shortcut && string.Equals(@event.Key, "A", StringComparison.Ordinal))
        {
            SelectAll();
            return KeyEventResult.Handled;
        }
        if (shortcut && string.Equals(@event.Key, "C", StringComparison.Ordinal))
        {
            CopySelection();
            return KeyEventResult.Handled;
        }
        if (string.Equals(@event.Key, "Escape", StringComparison.Ordinal))
        {
            ClearSelection();
            return KeyEventResult.Handled;
        }

        return KeyEventResult.Ignored;
    }

    private void AttachFocusNode(FocusNode? externalNode)
    {
        _focusNode = externalNode ?? new FocusNode();
        _ownsFocusNode = externalNode is null;
        if (_ownsFocusNode)
        {
            _focusNode.SkipTraversal = true;
        }
        _focusNode.AddListener(HandleFocusChanged);
    }

    private void DetachFocusNode()
    {
        if (_focusNode is null)
        {
            return;
        }

        _focusNode.RemoveListener(HandleFocusChanged);
        if (_ownsFocusNode)
        {
            _focusNode.Dispose();
        }
        _focusNode = null;
        _ownsFocusNode = false;
    }

    private void HandleFocusChanged()
    {
        if (_focusNode?.HasFocus == false)
        {
            _registrar.ClearSelection(SelectionChangedCause.Keyboard);
        }
        SetState(() => { });
    }

    private void CopyAndHide()
    {
        CopySelection();
        HideToolbar();
    }

    private void SelectAllAndHide()
    {
        _registrar.SelectAll(SelectionChangedCause.Toolbar);
        HideToolbar();
    }
}
