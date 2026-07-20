using Plumix.Foundation;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/keyboard_listener.dart
public sealed class KeyboardListener : StatelessWidget
{
    public KeyboardListener(
        FocusNode focusNode,
        Widget child,
        bool autofocus = false,
        bool includeSemantics = true,
        Action<KeyEvent>? onKeyEvent = null,
        Key? key = null) : base(key)
    {
        FocusNode = focusNode ?? throw new ArgumentNullException(nameof(focusNode));
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Autofocus = autofocus;
        IncludeSemantics = includeSemantics;
        OnKeyEvent = onKeyEvent;
    }

    public FocusNode FocusNode { get; }

    public bool Autofocus { get; }

    public bool IncludeSemantics { get; }

    public Action<KeyEvent>? OnKeyEvent { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return new Focus(
            focusNode: FocusNode,
            autofocus: Autofocus,
            canRequestFocus: FocusNode.CanRequestFocus,
            skipTraversal: FocusNode.SkipTraversal,
            includeSemantics: IncludeSemantics,
            onKeyEvent: HandleKeyEvent,
            child: Child);
    }

    private KeyEventResult HandleKeyEvent(FocusNode node, KeyEvent keyEvent)
    {
        OnKeyEvent?.Invoke(keyEvent);
        return KeyEventResult.Ignored;
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/widgets/raw_keyboard_listener.dart
[Obsolete(
    "Use KeyboardListener instead. This API mirrors Flutter's RawKeyboardListener deprecated after v3.18.0-2.0.pre.")]
public sealed class RawKeyboardListener : StatefulWidget
{
#pragma warning disable CS0618
    public RawKeyboardListener(
        FocusNode focusNode,
        Widget child,
        bool autofocus = false,
        bool includeSemantics = true,
        Action<RawKeyEvent>? onKey = null,
        Key? key = null) : base(key)
#pragma warning restore CS0618
    {
        FocusNode = focusNode ?? throw new ArgumentNullException(nameof(focusNode));
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Autofocus = autofocus;
        IncludeSemantics = includeSemantics;
        OnKey = onKey;
    }

    public FocusNode FocusNode { get; }

    public bool Autofocus { get; }

    public bool IncludeSemantics { get; }

#pragma warning disable CS0618
    public Action<RawKeyEvent>? OnKey { get; }
#pragma warning restore CS0618

    public Widget Child { get; }

    public override State CreateState()
    {
        return new RawKeyboardListenerState();
    }

    private sealed class RawKeyboardListenerState : State
    {
        private bool _listening;

        private RawKeyboardListener CurrentWidget => (RawKeyboardListener)Element.Widget;

        public override void InitState()
        {
            CurrentWidget.FocusNode.AddListener(HandleFocusChanged);
            HandleFocusChanged();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldListener = (RawKeyboardListener)oldWidget;
            if (ReferenceEquals(oldListener.FocusNode, CurrentWidget.FocusNode))
            {
                return;
            }

            oldListener.FocusNode.RemoveListener(HandleFocusChanged);
            DetachKeyboardIfAttached();
            CurrentWidget.FocusNode.AddListener(HandleFocusChanged);
            HandleFocusChanged();
        }

        public override Widget Build(BuildContext context)
        {
            return new Focus(
                focusNode: CurrentWidget.FocusNode,
                autofocus: CurrentWidget.Autofocus,
                canRequestFocus: CurrentWidget.FocusNode.CanRequestFocus,
                skipTraversal: CurrentWidget.FocusNode.SkipTraversal,
                includeSemantics: CurrentWidget.IncludeSemantics,
                child: CurrentWidget.Child);
        }

        public override void Dispose()
        {
            CurrentWidget.FocusNode.RemoveListener(HandleFocusChanged);
            DetachKeyboardIfAttached();
        }

        private void HandleFocusChanged()
        {
            if (CurrentWidget.FocusNode.HasFocus)
            {
                AttachKeyboardIfDetached();
            }
            else
            {
                DetachKeyboardIfAttached();
            }
        }

        private void AttachKeyboardIfDetached()
        {
            if (_listening)
            {
                return;
            }

#pragma warning disable CS0618
            RawKeyboard.Instance.AddListener(HandleRawKeyEvent);
#pragma warning restore CS0618
            _listening = true;
        }

        private void DetachKeyboardIfAttached()
        {
            if (!_listening)
            {
                return;
            }

#pragma warning disable CS0618
            RawKeyboard.Instance.RemoveListener(HandleRawKeyEvent);
#pragma warning restore CS0618
            _listening = false;
        }

#pragma warning disable CS0618
        private void HandleRawKeyEvent(RawKeyEvent keyEvent)
        {
            CurrentWidget.OnKey?.Invoke(keyEvent);
        }
#pragma warning restore CS0618
    }
}
