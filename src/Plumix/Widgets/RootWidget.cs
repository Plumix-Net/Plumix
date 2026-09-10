using Plumix.Foundation;
using Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/binding.dart

namespace Plumix.Widgets;

/// <summary>
/// The widget at the root of the widget tree.
/// </summary>
/// <remarks>
/// Dart's <c>RootWidget</c>. <see cref="Attach"/> inflates it and bootstraps the element tree
/// against a <see cref="BuildOwner"/>; the hosts reach it through
/// <c>WidgetHost.AttachRootWidget</c>, which is Plumix's <c>WidgetsBinding.attachRootWidget</c>.
/// The root hosts no render object of its own: the <see cref="View"/> below it publishes the render
/// tree through a <see cref="RenderTreeRootElement"/>.
/// </remarks>
public class RootWidget : Widget
{
    /// <summary>Creates a <see cref="RootWidget"/>.</summary>
    public RootWidget(
        Widget? child = null,
        string? debugShortDescription = null,
        Key? key = null)
        : base(key)
    {
        Child = child;
        DebugShortDescription = debugShortDescription;
    }

    /// <summary>The widget below this widget in the tree.</summary>
    public Widget? Child { get; }

    /// <summary>A short description of this widget used by debugging aids.</summary>
    public string? DebugShortDescription { get; }

    /// <inheritdoc />
    public override Element CreateElement() => new RootElement(this);

    /// <summary>
    /// Inflates this widget and attaches it to <paramref name="owner"/>.
    /// </summary>
    /// <remarks>
    /// Dart's <c>RootWidget.attach</c>. With no <paramref name="element"/> the element is created
    /// under <see cref="BuildOwner.LockState"/> and mounted inside a build scope; with one, the
    /// element is not remounted — the new widget is parked and picked up by the next rebuild.
    /// </remarks>
    public RootElement Attach(BuildOwner owner, RootElement? element = null)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (element is null)
        {
            RootElement? created = null;
            owner.LockState(() =>
            {
                created = (RootElement)CreateElement();
                created.AssignOwner(owner);
            });

            RootElement mounted = created!;
            owner.BuildScope(mounted, () => mounted.Mount(parent: null, newSlot: null));
            return mounted;
        }

        element.NewWidget = this;
        element.MarkNeedsBuild();
        return element;
    }

    /// <inheritdoc />
    public override string ToStringShort() => DebugShortDescription ?? base.ToStringShort();
}

/// <summary>
/// The root of the element tree.
/// </summary>
/// <remarks>
/// Dart's <c>RootElement</c>, folded together with <c>RootElementMixin</c> — C# has no mixins, and
/// Dart's only other mixin user (<c>RootRenderObjectElement</c>) is deprecated and unused. It can
/// be used only as the root of an element tree: its parent must be <see langword="null"/>. It does
/// not expect a render object from its child (<see cref="DebugExpectsRenderObjectForSlot"/> is
/// <see langword="false"/>): a render tree below it must be rooted by a <see cref="View"/>.
/// </remarks>
public class RootElement : Element
{
    private Element? _child;

    /// <summary>Creates a <see cref="RootElement"/> for <paramref name="widget"/>.</summary>
    public RootElement(RootWidget widget) : base(widget)
    {
    }

    /// <summary>The element this root inflated from <see cref="RootWidget.Child"/>.</summary>
    public Element? ChildElement => _child;

    /// <inheritdoc />
    public override Element? RenderObjectAttachingChild => _child;

    /// <inheritdoc />
    public override RenderObject? RenderObject => _child?.RenderObject;

    /// <summary>
    /// The widget a re-<see cref="RootWidget.Attach"/> parked here until the next rebuild picks it up.
    /// </summary>
    /// <remarks>Dart's <c>RootElement._newWidget</c>.</remarks>
    internal RootWidget? NewWidget { get; set; }

    /// <summary>
    /// Sets the owner of this element; it is propagated to every descendant by
    /// <see cref="Element.Mount"/>.
    /// </summary>
    /// <remarks>
    /// Dart's <c>RootElementMixin.assignOwner</c>. Only root elements may have their owner set
    /// explicitly, and it must happen before <see cref="Element.Mount"/>, which reads it.
    /// </remarks>
    public void AssignOwner(BuildOwner owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        Attach(owner);
    }

    /// <inheritdoc />
    public override void VisitChildren(Action<Element> visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        if (_child is not null)
        {
            visitor(_child);
        }
    }

    /// <inheritdoc />
    public override void ForgetChild(Element child)
    {
        if (!ReferenceEquals(child, _child))
        {
            throw new AssertionError("RootElement.ForgetChild was called for an element that is not its child.");
        }

        _child = null;
        base.ForgetChild(child);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Dart's <c>RootElement.mount</c> runs <c>_rebuild()</c> and then <c>super.performRebuild()</c>
    /// directly, bypassing <see cref="PerformRebuild"/> so the just-consumed <see cref="NewWidget"/>
    /// path is not re-entered. Plumix's mount hook is <see cref="Element.OnMount"/>.
    /// </remarks>
    protected override void OnMount()
    {
        if (Parent is not null)
        {
            throw new AssertionError("A RootElement must be mounted without a parent.");
        }

        base.OnMount();
        RebuildChild();
        if (_child is null)
        {
            throw new AssertionError("A RootWidget must have a child when it is first mounted.");
        }

        base.PerformRebuild();
    }

    /// <inheritdoc />
    public override void Update(Widget newWidget)
    {
        base.Update(newWidget);
        RebuildChild();
    }

    /// <inheritdoc />
    /// <remarks>Dart's <c>RootElement.performRebuild</c>.</remarks>
    protected override void PerformRebuild()
    {
        if (NewWidget is { } newWidget)
        {
            // NewWidget is null when the rebuild came from something else — a reassemble, say.
            NewWidget = null;
            Update(newWidget);
        }

        base.PerformRebuild();
        if (NewWidget is not null)
        {
            throw new AssertionError("RootElement.Update must not park another widget.");
        }
    }

    /// <inheritdoc />
    /// <inheritdoc />
    /// <remarks>Dart's <c>RootElement.debugExpectsRenderObjectForSlot</c>.</remarks>
    public override bool DebugExpectsRenderObjectForSlot(object? slot) => false;

    /// <summary>Dart's <c>RootElement._rebuild</c>.</summary>
    /// <remarks>
    /// A build failure is reported rather than replaced by an <c>ErrorWidget</c>: there is no view
    /// left to render one into.
    /// </remarks>
    private void RebuildChild()
    {
        try
        {
            _child = UpdateChild(_child, ((RootWidget)Widget).Child, newSlot: null);
        }
        catch (Exception exception)
        {
            FlutterError.ReportError(new FlutterErrorDetails(
                exception,
                stack: exception.StackTrace,
                library: "widgets library",
                context: new ErrorDescription("attaching to the render tree")));
            _child = null;
        }
    }
}
