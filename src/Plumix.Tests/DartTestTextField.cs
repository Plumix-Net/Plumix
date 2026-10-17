using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity: flutter/packages/flutter/test/widgets/editable_text_tester.dart (`TestTextField`), an
// EditableText under a selection gesture detector, without a design library.

namespace Plumix.Tests;

internal sealed class TestTextField : StatefulWidget
{
    public TestTextField(
        bool autofocus = false,
        EditableTextContextMenuBuilder? contextMenuBuilder = null,
        FocusNode? focusNode = null,
        int? maxLines = 1,
        bool readOnly = false,
        TextSelectionControls? selectionControls = null,
        TextEditingController? controller = null,
        bool showSelectionHandles = false,
        Key? key = null) : base(key)
    {
        Autofocus = autofocus;
        ContextMenuBuilder = contextMenuBuilder;
        FocusNode = focusNode;
        MaxLines = maxLines;
        ReadOnly = readOnly;
        SelectionControls = selectionControls;
        Controller = controller;
        ShowSelectionHandles = showSelectionHandles;
    }

    public bool Autofocus { get; }

    public EditableTextContextMenuBuilder? ContextMenuBuilder { get; }

    public FocusNode? FocusNode { get; }

    public int? MaxLines { get; }

    public bool ReadOnly { get; }

    public TextSelectionControls? SelectionControls { get; }

    public TextEditingController? Controller { get; }

    public bool ShowSelectionHandles { get; }

    public override State CreateState() => new TestTextFieldState();

    private sealed class TestTextFieldState : State<TestTextField>, ITextSelectionGestureDetectorBuilderDelegate
    {
        private static readonly Color Red = new(0xFFF44336);

        private TextEditingController? _controller;
        private FocusNode? _focusNode;
        private TextSelectionGestureDetectorBuilder _selectionGestureDetectorBuilder = null!;

        private TextEditingController EffectiveController => Widget.Controller ?? (_controller ??= new());

        private FocusNode EffectiveFocusNode => Widget.FocusNode ?? (_focusNode ??= new FocusNode());

        public bool ForcePressEnabled => false;

        public GlobalKey<EditableText.EditableTextState> EditableTextKey { get; } =
            new LabeledGlobalKey<EditableText.EditableTextState>(null);

        public bool SelectionEnabled => true;

        public override void InitState()
        {
            base.InitState();
            _selectionGestureDetectorBuilder = new TestTextFieldSelectionGestureDetectorBuilder(this);
        }

        public override void Dispose()
        {
            _focusNode?.Dispose();
            _controller?.Dispose();
            base.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            return new Semantics(
                enabled: true,
                onTap: Widget.ReadOnly ? null : () => EffectiveFocusNode.RequestFocus(),
                child: _selectionGestureDetectorBuilder.BuildGestureDetector(
                    behavior: HitTestBehavior.Translucent,
                    child: new EditableText(
                        EffectiveController,
                        key: EditableTextKey,
                        autofocus: Widget.Autofocus,
                        backgroundCursorColor: Red,
                        contextMenuBuilder: Widget.ContextMenuBuilder,
                        cursorColor: Red,
                        focusNode: EffectiveFocusNode,
                        maxLines: Widget.MaxLines,
                        readOnly: Widget.ReadOnly,
                        rendererIgnoresPointer: true,
                        selectionControls: Widget.SelectionControls ?? TestTextSelectionHandleControls.Instance,
                        style: new TextStyle(),
                        showSelectionHandles: Widget.ShowSelectionHandles)));
        }
    }

    private sealed class TestTextFieldSelectionGestureDetectorBuilder(
        ITextSelectionGestureDetectorBuilderDelegate state) : TextSelectionGestureDetectorBuilder(state)
    {
        // TestTextField does not have an onTap callback.
        protected override void OnUserTap()
        {
        }

        protected override bool OnUserTapAlwaysCalled => false;
    }
}
