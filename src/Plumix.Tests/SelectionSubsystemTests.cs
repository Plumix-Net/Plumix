using Avalonia;
using Avalonia.Media;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

/// Parity coverage for `rendering/selection.dart`, `widgets/selection_container.dart`
/// and the `_SelectableFragment` half of `rendering/paragraph.dart`.
public sealed class SelectionSubsystemTests
{
    private static readonly Rect TargetRect = new(100, 100, 200, 500);

    [Theory]
    // The exact table from Flutter's `selection_test.dart` > `selectionBasedOnRect works`.
    [InlineData(50, 50, SelectionResult.Previous)]
    [InlineData(50, 200, SelectionResult.Previous)]
    [InlineData(50, 700, SelectionResult.Next)]
    [InlineData(200, 50, SelectionResult.Previous)]
    [InlineData(350, 50, SelectionResult.Previous)]
    [InlineData(350, 200, SelectionResult.Next)]
    [InlineData(350, 700, SelectionResult.Next)]
    [InlineData(200, 700, SelectionResult.Next)]
    [InlineData(150, 300, SelectionResult.End)]
    public void SelectionUtils_GetResultBasedOnRectMatchesFlutterTable(double x, double y, SelectionResult expected)
    {
        Assert.Equal(expected, SelectionUtils.GetResultBasedOnRect(TargetRect, new Point(x, y)));
    }

    [Theory]
    // `adjustDragOffset works`: area 1 clamps to the leading corner, area 2 to the trailing one.
    [InlineData(50, 50, true)]
    [InlineData(50, 200, true)]
    [InlineData(50, 700, false)]
    [InlineData(200, 50, true)]
    [InlineData(350, 50, true)]
    [InlineData(350, 200, false)]
    [InlineData(350, 700, false)]
    [InlineData(200, 700, false)]
    public void SelectionUtils_AdjustDragOffsetClampsPerAreaAndDirection(double x, double y, bool leading)
    {
        var point = new Point(x, y);
        Assert.Equal(
            leading ? TargetRect.TopLeft : TargetRect.BottomRight,
            SelectionUtils.AdjustDragOffset(TargetRect, point));
        Assert.Equal(
            leading ? TargetRect.TopRight : TargetRect.BottomLeft,
            SelectionUtils.AdjustDragOffset(TargetRect, point, TextDirection.Rtl));
    }

    [Fact]
    public void SelectionUtils_AdjustDragOffsetKeepsPointsInsideTheRect()
    {
        var inside = new Point(150, 300);
        Assert.Equal(inside, SelectionUtils.AdjustDragOffset(TargetRect, inside));
        Assert.Equal(inside, SelectionUtils.AdjustDragOffset(TargetRect, inside, TextDirection.Rtl));
    }

    [Fact]
    public void SelectionGeometry_RejectsSelectionPointsWithoutAStatus()
    {
        Assert.Throws<ArgumentException>(() => new SelectionGeometry(
            SelectionStatus.None,
            hasContent: true,
            startSelectionPoint: new SelectionPoint(default, 10, TextSelectionHandleType.Left)));

        var geometry = new SelectionGeometry(SelectionStatus.Uncollapsed, hasContent: true);
        Assert.True(geometry.HasSelection);
        Assert.Empty(geometry.SelectionRects);
        Assert.False(new SelectionGeometry(SelectionStatus.None, hasContent: false).HasSelection);
    }

    [Fact]
    public void SelectionGeometry_ComparesRectsElementWiseAndCopiesWithoutClearing()
    {
        var start = new SelectionPoint(new Point(1, 2), 12, TextSelectionHandleType.Left);
        var first = new SelectionGeometry(
            SelectionStatus.Uncollapsed,
            hasContent: true,
            startSelectionPoint: start,
            selectionRects: [new Rect(0, 0, 4, 4)]);
        var second = new SelectionGeometry(
            SelectionStatus.Uncollapsed,
            hasContent: true,
            startSelectionPoint: new SelectionPoint(new Point(1, 2), 12, TextSelectionHandleType.Left),
            selectionRects: [new Rect(0, 0, 4, 4)]);

        Assert.Equal(first, second);
        Assert.NotEqual(first, first.CopyWith(status: SelectionStatus.Collapsed));

        // Dart's copyWith takes `arg ?? this.field`, so a null argument cannot clear a point.
        Assert.Same(start, first.CopyWith().StartSelectionPoint);
    }

    [Fact]
    public void SelectedContentRange_RejectsNegativeOffsetsAndComparesByValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SelectedContentRange(-1, 3));
        Assert.Equal(new SelectedContentRange(6, 56), new SelectedContentRange(6, 56));
        Assert.NotEqual(new SelectedContentRange(6, 56), new SelectedContentRange(56, 6));
    }

    [Fact]
    public void SelectionEvents_CarryFlutterDefaults()
    {
        SelectionEdgeUpdateEvent start = SelectionEdgeUpdateEvent.ForStart(new Point(1, 2));
        Assert.Equal(SelectionEventType.StartEdgeUpdate, start.Type);
        Assert.Equal(TextGranularity.Character, start.Granularity);

        SelectionEdgeUpdateEvent end = SelectionEdgeUpdateEvent.ForEnd(new Point(1, 2), TextGranularity.Word);
        Assert.Equal(SelectionEventType.EndEdgeUpdate, end.Type);
        Assert.Equal(TextGranularity.Word, end.Granularity);

        Assert.False(new SelectParagraphSelectionEvent(default).Absorb);
        Assert.Equal(SelectionEventType.SelectWord, new SelectWordSelectionEvent(default).Type);
        Assert.Equal(SelectionEventType.Clear, new ClearSelectionEvent().Type);
        Assert.Equal(SelectionEventType.SelectAll, new SelectAllSelectionEvent().Type);

        var directional = new DirectionallyExtendSelectionEvent(3.0, isEnd: true, SelectionExtendDirection.NextLine);
        DirectionallyExtendSelectionEvent copy = directional.CopyWith(
            direction: SelectionExtendDirection.Forward);
        Assert.Equal(3.0, copy.Dx);
        Assert.True(copy.IsEnd);
        Assert.Equal(SelectionExtendDirection.Forward, copy.Direction);
    }

    [Fact]
    public void TextBoundaries_MatchFlutterSemantics()
    {
        var paragraphs = new ParagraphBoundary("how are you\nI am fine\nThank you");
        Assert.Equal(12, paragraphs.GetLeadingTextBoundaryAt(15));
        Assert.Equal(22, paragraphs.GetTrailingTextBoundaryAt(15));
        Assert.Null(paragraphs.GetLeadingTextBoundaryAt(-1));

        var document = new DocumentBoundary("abcd");
        Assert.Equal(0, document.GetLeadingTextBoundaryAt(3));
        Assert.Equal(4, document.GetTrailingTextBoundaryAt(3));
        Assert.Null(document.GetTrailingTextBoundaryAt(4));

        var characters = new CharacterBoundary("abcd");
        Assert.Equal(2, characters.GetLeadingTextBoundaryAt(2));
        Assert.Equal(3, characters.GetTrailingTextBoundaryAt(2));
        Assert.Null(characters.GetTrailingTextBoundaryAt(4));

        // `\r\n` is one line terminator.
        var crlf = new ParagraphBoundary("a\r\nb");
        Assert.Equal(3, crlf.GetLeadingTextBoundaryAt(3));
        Assert.Equal(3, crlf.GetTrailingTextBoundaryAt(0));
    }

    [Fact]
    public void RenderParagraph_RegistersOneFragmentAndUnregistersWhenTextBecomesEmpty()
    {
        var registrar = new RecordingRegistrar();
        var paragraph = new RenderParagraph(new TextSpan("1234567"))
        {
            Registrar = registrar,
        };

        Assert.Single(registrar.Selectables);

        paragraph.Text = new TextSpan(string.Empty);
        Assert.Empty(registrar.Selectables);
    }

    [Fact]
    public void RenderParagraph_SplitsFragmentsAtPlaceholderBoundaries()
    {
        var registrar = new RecordingRegistrar();
        _ = new RenderParagraph(new TextSpan(children:
        [
            new TextSpan("before"),
            new WidgetSpan(new SizedBox(width: 10, height: 10)),
            new TextSpan("after"),
        ]))
        {
            Registrar = registrar,
        };

        // The placeholder itself belongs to no fragment; the runs around it do.
        Assert.Equal(2, registrar.Selectables.Count);
        Assert.Equal(["before", "after"], registrar.Selectables
            .Select(selectable =>
            {
                selectable.DispatchSelectionEvent(new SelectAllSelectionEvent());
                return selectable.GetSelectedContent()!.PlainText;
            })
            .ToArray());
    }

    [Fact]
    public void SelectableFragment_SelectAllAndClearDriveGeometryAndContent()
    {
        var registrar = new RecordingRegistrar();
        _ = new RenderParagraph(new TextSpan("hello"))
        {
            Registrar = registrar,
        };
        ISelectable selectable = registrar.Selectables.Single();

        Assert.Equal(SelectionStatus.None, selectable.Value.Status);
        Assert.True(selectable.Value.HasContent);
        Assert.Equal(5, selectable.ContentLength);
        Assert.Null(selectable.GetSelectedContent());
        Assert.Null(selectable.GetSelection());

        Assert.Equal(SelectionResult.None, selectable.DispatchSelectionEvent(new SelectAllSelectionEvent()));
        Assert.Equal(SelectionStatus.Uncollapsed, selectable.Value.Status);
        Assert.Equal("hello", selectable.GetSelectedContent()!.PlainText);
        Assert.Equal(new SelectedContentRange(0, 5), selectable.GetSelection());

        Assert.Equal(SelectionResult.None, selectable.DispatchSelectionEvent(new ClearSelectionEvent()));
        Assert.Equal(SelectionStatus.None, selectable.Value.Status);
        Assert.Null(selectable.GetSelectedContent());
    }

    [Theory]
    // Flutter's `can granularly extend selection - <granularity>` cases, using the
    // same fixture text and the same starting selection.
    [InlineData(TextGranularity.Character, true, 4, 6)]
    [InlineData(TextGranularity.Word, true, 4, 7)]
    [InlineData(TextGranularity.Line, true, 4, 11)]
    [InlineData(TextGranularity.Document, true, 4, 31)]
    [InlineData(TextGranularity.Word, false, 4, 4)]
    public void SelectableFragment_GranularlyExtendsSelection(
        TextGranularity granularity,
        bool forward,
        int expectedBase,
        int expectedExtent)
    {
        var registrar = new RecordingRegistrar();
        var paragraph = new RenderParagraph(new TextSpan("how are you\nI am fine\nThank you"))
        {
            Registrar = registrar,
        };
        ISelectable selectable = registrar.Selectables.Single();
        SelectRange(selectable, 4, 5);

        selectable.DispatchSelectionEvent(new GranularlyExtendSelectionEvent(forward, isEnd: true, granularity));

        TextSelection selection = paragraph.Selections.Single();
        Assert.Equal(expectedBase, selection.BaseOffset);
        Assert.Equal(expectedExtent, selection.ExtentOffset);
    }

    [Fact]
    public void SelectableFragment_GranularExtensionWithoutSelectionStartsAtTheEdges()
    {
        var registrar = new RecordingRegistrar();
        var paragraph = new RenderParagraph(new TextSpan("how are you\nI am fine\nThank you"))
        {
            Registrar = registrar,
        };
        ISelectable selectable = registrar.Selectables.Single();

        selectable.DispatchSelectionEvent(
            new GranularlyExtendSelectionEvent(forward: true, isEnd: true, TextGranularity.Word));
        Assert.Equal(new TextSelection(0, 3), paragraph.Selections.Single());

        selectable.DispatchSelectionEvent(new ClearSelectionEvent());
        Assert.Empty(paragraph.Selections);

        selectable.DispatchSelectionEvent(
            new GranularlyExtendSelectionEvent(forward: false, isEnd: true, TextGranularity.Word));
        Assert.Equal(new TextSelection(31, 28), paragraph.Selections.Single());
    }

    [Fact]
    public void SelectableFragment_ReportsHandleTypesAndFlipsThemWhenReversed()
    {
        var registrar = new RecordingRegistrar();
        _ = new RenderParagraph(new TextSpan("hello"))
        {
            Registrar = registrar,
        };
        ISelectable selectable = registrar.Selectables.Single();

        SelectRange(selectable, 1, 4);
        Assert.Equal(TextSelectionHandleType.Left, selectable.Value.StartSelectionPoint!.HandleType);
        Assert.Equal(TextSelectionHandleType.Right, selectable.Value.EndSelectionPoint!.HandleType);

        SelectRange(selectable, 4, 1);
        Assert.Equal(TextSelectionHandleType.Right, selectable.Value.StartSelectionPoint!.HandleType);
        Assert.Equal(TextSelectionHandleType.Left, selectable.Value.EndSelectionPoint!.HandleType);

        SelectRange(selectable, 2, 2);
        Assert.Equal(SelectionStatus.Collapsed, selectable.Value.Status);
        Assert.Equal(TextSelectionHandleType.Collapsed, selectable.Value.StartSelectionPoint!.HandleType);
        Assert.Equal(TextSelectionHandleType.Collapsed, selectable.Value.EndSelectionPoint!.HandleType);
    }

    [Fact]
    public void SelectionContainer_RegistersItselfOnceAndHostsItsChildren()
    {
        var registrar = new RecordingRegistrar();
        var containerDelegate = new TestContainerDelegate();
        using var harness = new SelectionHarness(new Directionality(
            TextDirection.Ltr,
            new SelectionContainer(
                containerDelegate,
                new Column(children: [new Text("a"), new Text("b"), new Text("c")]),
                registrar: registrar)));
        harness.Pump(new Size(200, 200));

        Assert.Single(registrar.Selectables);
        Assert.Equal(3, containerDelegate.Selectables.Count);
    }

    [Fact]
    public void SelectionContainer_DisabledSubtreeRegistersNothing()
    {
        var registrar = new RecordingRegistrar();
        var containerDelegate = new TestContainerDelegate();
        using var harness = new SelectionHarness(new Directionality(
            TextDirection.Ltr,
            new SelectionContainer(
                containerDelegate,
                SelectionContainer.Disabled(
                    new Column(children: [new Text("a"), new Text("b")])),
                registrar: registrar)));
        harness.Pump(new Size(200, 200));

        Assert.Empty(registrar.Selectables);
        Assert.Empty(containerDelegate.Selectables);
    }

    [Fact]
    public void SelectionContainer_WithoutSelectableChildrenStaysUnregistered()
    {
        var registrar = new RecordingRegistrar();
        var containerDelegate = new TestContainerDelegate();
        using var harness = new SelectionHarness(new Directionality(
            TextDirection.Ltr,
            new SelectionContainer(containerDelegate, new Column(children: []), registrar: registrar)));
        harness.Pump(new Size(200, 200));

        Assert.Empty(registrar.Selectables);
    }

    [Fact]
    public void SelectionContainer_TakesTheRegistrarFromContextWhenNoneIsGiven()
    {
        var registrar = new RecordingRegistrar();
        var containerDelegate = new TestContainerDelegate();
        using var harness = new SelectionHarness(new Directionality(
            TextDirection.Ltr,
            new SelectionRegistrarScope(
                registrar,
                new SelectionContainer(containerDelegate, new Text("a")))));
        harness.Pump(new Size(200, 200));

        Assert.Single(registrar.Selectables);
    }

    [Fact]
    public void MultiSelectableDelegate_AggregatesContentRangeAcrossChildren()
    {
        var containerDelegate = new TestContainerDelegate();
        using var harness = new SelectionHarness(new Directionality(
            TextDirection.Ltr,
            new SelectionContainer(
                containerDelegate,
                new Column(
                    crossAxisAlignment: CrossAxisAlignment.Start,
                    children: [new Text("Hello"), new Text("World")]))));
        harness.Pump(new Size(200, 200));

        containerDelegate.DispatchSelectionEvent(new SelectAllSelectionEvent());

        Assert.Equal("HelloWorld", containerDelegate.GetSelectedContent()!.PlainText);
        Assert.Equal(10, containerDelegate.ContentLength);
        Assert.Equal(new SelectedContentRange(0, 10), containerDelegate.GetSelection());
        Assert.True(containerDelegate.Value.HasSelection);

        containerDelegate.DispatchSelectionEvent(new ClearSelectionEvent());
        Assert.Null(containerDelegate.GetSelectedContent());
        Assert.Null(containerDelegate.GetSelection());
        Assert.Equal(SelectionStatus.None, containerDelegate.Value.Status);
    }

    private static void SelectRange(ISelectable selectable, int baseOffset, int extentOffset)
    {
        selectable.DispatchSelectionEvent(new SelectAllSelectionEvent());
        var fragment = (SelectableFragment)selectable;
        fragment.DebugSetSelection(baseOffset, extentOffset);
    }

    private sealed class RecordingRegistrar : ISelectionRegistrar
    {
        public List<ISelectable> Selectables { get; } = [];

        public void Add(ISelectable selectable) => Selectables.Add(selectable);

        public void Remove(ISelectable selectable) => Selectables.Remove(selectable);
    }

    private sealed class TestContainerDelegate : StaticSelectionContainerDelegate
    {
    }

    private sealed class SelectionHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _root;
        private readonly PipelineOwner _pipeline;

        public SelectionHarness(Widget widget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _root = new HarnessRootElement(RenderView, widget);
            _root.Attach(_owner);
            _root.Mount(null, null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
            Scheduler.PumpFrameForTests();
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushPaint();
        }

        public void Dispose() => _root.Unmount();

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;

            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget)
            {
                _renderView = renderView;
            }

            public override RenderObject? RenderObject => _child?.RenderObject;

            internal override Element? RenderObjectAttachingChild => _child;

            protected override void OnMount()
            {
                base.OnMount();
                Rebuild();
            }

            internal override void Rebuild()
            {
                Dirty = false;
                _child = UpdateChild(_child, Widget, null);
            }

            internal override void VisitChildren(Action<Element> visitor)
            {
                if (_child is not null)
                {
                    visitor(_child);
                }
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot) =>
                _renderView.Child = (RenderBox)child;

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot) => _renderView.Child = null;
        }
    }
}
