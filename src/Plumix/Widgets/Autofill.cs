using Plumix.Foundation;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/autofill.dart

/// <summary>
/// Predefined autofill context clean up actions.
/// </summary>
public enum AutofillContextAction
{
    /// <summary>The values within the current autofill context should be saved.</summary>
    Commit,

    /// <summary>The values within the current autofill context should not be saved.</summary>
    Cancel,
}

/// <summary>
/// An <see cref="IAutofillScope"/> widget that groups <see cref="IAutofillClient"/>s together.
/// </summary>
/// <remarks>
/// Autofill clients that share the same closest <see cref="AutofillGroup"/> ancestor are
/// considered to be in the same autofill group, and are cross-referenced to each other by the
/// platform when it decides what values to fill in.
/// </remarks>
public sealed class AutofillGroup : StatefulWidget
{
    /// <summary>Creates a scope for autofillable input fields.</summary>
    public AutofillGroup(
        Widget child,
        AutofillContextAction onDisposeAction = AutofillContextAction.Commit,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        OnDisposeAction = onDisposeAction;
    }

    /// <summary>The widget below this widget in the tree.</summary>
    public Widget Child { get; }

    /// <summary>The action to be run when this <see cref="AutofillGroup"/> is the topmost group and
    /// it is being disposed, in order to clean up the current autofill context.</summary>
    public AutofillContextAction OnDisposeAction { get; }

    /// <summary>Returns the closest <see cref="AutofillGroupState"/> ancestor, or <c>null</c>.
    /// </summary>
    public static AutofillGroupState? MaybeOf(BuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.DependOnInherited<AutofillScopeInherited>()?.Scope;
    }

    /// <summary>Returns the closest <see cref="AutofillGroupState"/> ancestor.</summary>
    /// <exception cref="InvalidOperationException">No <see cref="AutofillGroup"/> ancestor exists.
    /// </exception>
    public static AutofillGroupState Of(BuildContext context)
    {
        return MaybeOf(context) ?? throw new InvalidOperationException(
            "AutofillGroup.Of() was called with a context that does not contain an "
            + "AutofillGroup widget.\n"
            + "No AutofillGroup widget ancestor could be found starting from the "
            + "context that was passed to AutofillGroup.Of(). This can happen "
            + "because you are using a widget that looks for an AutofillGroup "
            + "ancestor, but no such ancestor exists.");
    }

    /// <inheritdoc/>
    public override State CreateState() => new AutofillGroupState();
}

/// <summary>
/// State associated with an <see cref="AutofillGroup"/> widget.
/// </summary>
/// <remarks>
/// C# has no mixins, so Dart's <c>AutofillScopeMixin</c> body is reached through the
/// <see cref="AutofillScopeMixin"/> companion.
/// </remarks>
public sealed class AutofillGroupState : State, IAutofillScope
{
    private readonly Dictionary<string, IAutofillClient> _clients = [];
    private readonly List<string> _registrationOrder = [];
    private bool _isTopmostAutofillGroup;

    private AutofillGroup Widget => (AutofillGroup)StateWidget;

    /// <summary>The autofill clients in this group, in first-registration order, filtered to the
    /// ones that have autofill enabled.</summary>
    public IEnumerable<IAutofillClient> AutofillClients =>
        _registrationOrder
            .Select(autofillId => _clients[autofillId])
            .Where(client => client.TextInputConfiguration.AutofillConfiguration.Enabled);

    /// <inheritdoc/>
    public IAutofillClient? GetAutofillClient(string autofillId) =>
        _clients.GetValueOrDefault(autofillId);

    /// <inheritdoc/>
    public TextInputConnection Attach(ITextInputClient trigger, TextInputConfiguration configuration) =>
        AutofillScopeMixin.Attach(this, trigger, configuration);

    /// <summary>Adds the <see cref="IAutofillClient"/> to this <see cref="AutofillGroup"/>.
    /// </summary>
    /// <remarks>A client whose id is already registered is ignored, so the first registration wins
    /// and keeps its position in <see cref="AutofillClients"/>.</remarks>
    public void Register(IAutofillClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        if (_clients.TryAdd(client.AutofillId, client))
        {
            _registrationOrder.Add(client.AutofillId);
        }
    }

    /// <summary>Removes the <see cref="IAutofillClient"/> with the given id.</summary>
    public void Unregister(string autofillId)
    {
        if (_clients.Remove(autofillId))
        {
            _registrationOrder.Remove(autofillId);
        }
    }

    /// <inheritdoc/>
    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        _isTopmostAutofillGroup = AutofillGroup.MaybeOf(Context) is null;
    }

    /// <inheritdoc/>
    public override Widget Build(BuildContext context) =>
        new AutofillScopeInherited(Widget.Child, this);

    /// <inheritdoc/>
    public override void Dispose()
    {
        base.Dispose();
        if (!_isTopmostAutofillGroup)
        {
            return;
        }

        switch (Widget.OnDisposeAction)
        {
            case AutofillContextAction.Cancel:
                TextInput.FinishAutofillContext(shouldSave: false);
                break;
            case AutofillContextAction.Commit:
                TextInput.FinishAutofillContext();
                break;
        }
    }
}

/// <summary>Carries the ambient <see cref="AutofillGroupState"/> down the tree.</summary>
internal sealed class AutofillScopeInherited : InheritedWidget
{
    internal AutofillScopeInherited(Widget child, AutofillGroupState? scope, Key? key = null) : base(key)
    {
        Child = child;
        Scope = scope;
    }

    internal Widget Child { get; }

    internal AutofillGroupState? Scope { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !ReferenceEquals(((AutofillScopeInherited)oldWidget).Scope, Scope);
}
