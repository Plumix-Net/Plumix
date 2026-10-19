using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// C#-only test infrastructure: flutter/test/widgets/button_tester.dart and editable_text_tester.dart.

namespace Plumix.Tests;

internal sealed class TestButton(
    Widget child,
    FocusNode? focusNode = null,
    bool autofocus = false,
    Action? onPressed = null,
    HitTestBehavior? behavior = null,
    Key? key = null) : StatelessWidget(key)
{
    public override Widget Build(BuildContext context) => new Semantics(
        label: "button",
        button: true,
        enabled: onPressed is not null,
        onTap: onPressed,
        onFocus: () => focusNode?.RequestFocus(),
        focusable: true,
        child: new FocusableActionDetector(
            enabled: onPressed is not null,
            focusNode: focusNode,
            autofocus: autofocus,
            child: new GestureDetector(behavior: behavior, onTap: onPressed, child: child)));
}

