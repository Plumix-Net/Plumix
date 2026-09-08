using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart

namespace Plumix.Widgets;

/// <summary>
/// A <see cref="RenderObjectElement"/> used to manage the root of a render tree.
/// </summary>
/// <remarks>
/// Flutter's <c>RenderTreeRootElement</c>. Unlike any other render object element, it does not
/// attempt to attach its render object to the closest ancestor render object element; instead it
/// asserts that no such ancestor exists, because the render object it manages is the root of an
/// independent render tree. Subclasses decide how the root is handed to a <c>PipelineOwner</c>.
/// </remarks>
public abstract class RenderTreeRootElement : RenderObjectElement
{
    /// <summary>Creates an element that uses <paramref name="widget"/> as its configuration.</summary>
    protected RenderTreeRootElement(RenderObjectWidget widget) : base(widget)
    {
    }

    /// <inheritdoc />
    /// <remarks>Records the slot and asserts that no ancestor expects to receive the render object.</remarks>
    public override void AttachRenderObject(object? newSlot)
    {
        // RenderObjectElement.UpdateSlot only forwards to an ancestor host, and this element never
        // registers one, so it reduces to Dart's `_slot = newSlot`.
        base.UpdateSlot(newSlot);
        DebugCheckMustNotAttachRenderObjectToAncestor();
    }

    /// <inheritdoc />
    public override void DetachRenderObject()
    {
        base.UpdateSlot(null);
    }

    /// <inheritdoc />
    public override void UpdateSlot(object? newSlot)
    {
        base.UpdateSlot(newSlot);
        DebugCheckMustNotAttachRenderObjectToAncestor();
    }

    private void DebugCheckMustNotAttachRenderObjectToAncestor()
    {
        if (!Constants.KDebugMode)
        {
            return;
        }

        if (FindAncestorRenderObjectHost().host is not null)
        {
            throw new FlutterError(
            [
                new ErrorSummary(
                    $"The RenderObject for {ToStringShort()} cannot maintain an independent render tree at "
                    + "its current location."),
                new ErrorDescription(
                    "The ownership chain for the RenderObject in question was:\n  "
                    + DebugGetCreatorChain(10)),
                new ErrorDescription(
                    "This RenderObject is the root of an independent render tree and it cannot attach "
                    + "itself to an ancestor in an existing tree. The ancestor RenderObject, however, "
                    + "expects that a child will be attached."),
                new ErrorHint(
                    $"Try moving the subtree that contains the {ToStringShort()} widget to a location where "
                    + "it is not expected to attach its RenderObject to a parent. This could mean moving the "
                    + "subtree into the view property of a \"ViewAnchor\" widget or - if the subtree is the "
                    + "root of your widget tree - passing it to \"runWidget\" instead of \"runApp\"."),
                new ErrorHint(
                    $"If you are seeing this error in a test and the subtree containing the {ToStringShort()} "
                    + "widget is passed to \"WidgetTester.pumpWidget\", consider setting the \"wrapWithView\" "
                    + "parameter of that method to false."),
            ]);
        }
    }
}
