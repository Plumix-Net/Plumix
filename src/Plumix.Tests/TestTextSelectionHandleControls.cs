using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity: flutter/packages/flutter/test/widgets/editable_text_tester.dart
// (`TestTextSelectionHandleControls`), shared by the editable and selection tests.

namespace Plumix.Tests;

internal sealed class TestTextSelectionHandleControls : TextSelectionControls, ITextSelectionHandleControls
{
    public static TextSelectionControls Instance { get; } = new TestTextSelectionHandleControls();

    public override Widget BuildHandle(
        BuildContext context,
        TextSelectionHandleType type,
        double textLineHeight,
        Action? onTap = null)
    {
        return new SizedBox(width: 0.0, height: 0.0);
    }

    public override Point GetHandleAnchor(TextSelectionHandleType type, double textLineHeight) => default;

    public override Size GetHandleSize(double textLineHeight) => default;

    [Obsolete("Use a contextMenuBuilder instead. This feature was deprecated after Flutter v3.3.0-0.5.pre.")]
    public override Widget BuildToolbar(
        BuildContext context,
        Rect globalEditableRegion,
        double textLineHeight,
        Point selectionMidpoint,
        IReadOnlyList<TextSelectionPoint> endpoints,
        ITextSelectionDelegate @delegate,
        IValueListenable<ClipboardStatus>? clipboardStatus,
        Point? lastSecondaryTapDownPosition)
    {
        return new SizedBox(width: 0.0, height: 0.0);
    }
}
