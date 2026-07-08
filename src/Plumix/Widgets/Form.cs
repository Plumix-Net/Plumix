using Plumix.Rendering;
using Plumix.Foundation;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/form.dart

public enum AutovalidateMode
{
    Disabled,
    Always,
    OnUserInteraction,
    OnUnfocus,
    OnUserInteractionIfError,
}

public delegate string? FormFieldValidator<T>(T? value);
public delegate Widget FormFieldErrorBuilder(BuildContext context, string errorText);
public delegate void FormFieldSetter<T>(T? value);
public delegate Widget FormFieldBuilder<T>(FormFieldState<T> field);

public sealed class Form : StatefulWidget
{
    public Form(
        Widget child,
        Action? onChanged = null,
        AutovalidateMode autovalidateMode = AutovalidateMode.Disabled,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        OnChanged = onChanged;
        AutovalidateMode = autovalidateMode;
    }

    public Widget Child { get; }
    public Action? OnChanged { get; }
    public AutovalidateMode AutovalidateMode { get; }

    public static FormState? MaybeOf(BuildContext context) =>
        context.DependOnInherited<FormScope>()?.FormState;

    public static FormState Of(BuildContext context) => MaybeOf(context)
        ?? throw new InvalidOperationException("Form.Of() was called with a context that does not contain a Form widget.");

    public override State CreateState() => new FormState();
}

public sealed class FormState : State
{
    private readonly HashSet<FormFieldState> _fields = [];
    private int _generation;
    private bool _hasInteractedByUser;

    private Form Current => (Form)StateWidget;

    public IEnumerable<FormFieldState> Fields => _fields;
    internal AutovalidateMode CurrentAutovalidateMode => Current.AutovalidateMode;

    internal void Register(FormFieldState field) => _fields.Add(field);

    internal void Unregister(FormFieldState field) => _fields.Remove(field);

    internal void FieldDidChange()
    {
        Current.OnChanged?.Invoke();
        _hasInteractedByUser = _fields.Any(field => field.HasInteractedByUser);
        ForceRebuild();
    }

    public void Save()
    {
        foreach (var field in _fields.ToArray()) field.Save();
    }

    public void Reset()
    {
        foreach (var field in _fields.ToArray()) field.Reset();
        _hasInteractedByUser = false;
        FieldDidChange();
    }

    public void ClearError()
    {
        foreach (var field in _fields.ToArray()) field.ClearErrorInternal();
        FieldDidChange();
    }

    public bool Validate()
    {
        _hasInteractedByUser = true;
        ForceRebuild();
        return ValidateFields(invalidFields: null);
    }

    public IReadOnlySet<FormFieldState> ValidateGranularly()
    {
        var invalidFields = new HashSet<FormFieldState>();
        _hasInteractedByUser = true;
        ForceRebuild();
        ValidateFields(invalidFields);
        return invalidFields;
    }

    public override Widget Build(BuildContext context)
    {
        var hasError = _fields.Any(field => field.HasError);
        switch (Current.AutovalidateMode)
        {
            case AutovalidateMode.Always:
                ValidateFields(invalidFields: null);
                break;
            case AutovalidateMode.OnUserInteraction when _hasInteractedByUser:
                ValidateFields(invalidFields: null);
                break;
            case AutovalidateMode.OnUserInteractionIfError when _hasInteractedByUser && hasError:
                ValidateFields(invalidFields: null);
                break;
        }

        return new Semantics(
            container: true,
            explicitChildNodes: true,
            role: SemanticsRole.Form,
            child: new FormScope(this, _generation, Current.Child));
    }

    private void ForceRebuild() => SetState(() => _generation++);

    private bool ValidateFields(ISet<FormFieldState>? invalidFields)
    {
        var valid = true;
        foreach (var field in _fields.ToArray())
        {
            if (field.Validate()) continue;
            valid = false;
            invalidFields?.Add(field);
        }

        return valid;
    }
}

internal sealed class FormScope : InheritedWidget
{
    public FormScope(FormState formState, int generation, Widget child) : base()
    {
        FormState = formState;
        Generation = generation;
        Child = child;
    }

    public FormState FormState { get; }
    public int Generation { get; }
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        Generation != ((FormScope)oldWidget).Generation;
}

public abstract class FormFieldState : State
{
    private FormState? _registeredForm;
    private bool _hadFocusWithin;

    public abstract string? ErrorText { get; }
    public bool HasError => ErrorText is not null;
    public abstract bool HasInteractedByUser { get; }
    public abstract bool IsValid { get; }

    public abstract void Save();
    public abstract void Reset();
    public abstract void ClearError();
    public abstract bool Validate();
    internal abstract void ClearErrorInternal();

    public override void InitState()
    {
        FocusManager.Instance.PrimaryFocusChanged += HandlePrimaryFocusChanged;
        _hadFocusWithin = HasFocusWithin();
    }

    public override void Deactivate()
    {
        _registeredForm?.Unregister(this);
        _registeredForm = null;
    }

    public override void Dispose()
    {
        FocusManager.Instance.PrimaryFocusChanged -= HandlePrimaryFocusChanged;
        _registeredForm?.Unregister(this);
        _registeredForm = null;
    }

    protected void RegisterWithForm(BuildContext context)
    {
        var form = Form.MaybeOf(context);
        if (ReferenceEquals(form, _registeredForm)) return;
        _registeredForm?.Unregister(this);
        _registeredForm = form;
        _registeredForm?.Register(this);
    }

    protected FormState? RegisteredForm => _registeredForm;
    protected abstract AutovalidateMode EffectiveAutovalidateMode { get; }
    protected abstract bool FieldEnabled { get; }

    private void HandlePrimaryFocusChanged()
    {
        var hasFocusWithin = HasFocusWithin();
        if (_hadFocusWithin && !hasFocusWithin
            && FieldEnabled
            && EffectiveAutovalidateMode == AutovalidateMode.OnUnfocus
            && Mounted)
        {
            Validate();
        }

        _hadFocusWithin = hasFocusWithin;
    }

    private bool HasFocusWithin()
    {
        for (var element = FocusManager.Instance.PrimaryFocus?.AttachmentElement;
             element is not null;
             element = element.Parent)
        {
            if (ReferenceEquals(element, Element)) return true;
        }

        return false;
    }
}

public class FormField<T> : StatefulWidget
{
    public FormField(
        FormFieldBuilder<T> builder,
        FormFieldSetter<T>? onSaved = null,
        Action? onReset = null,
        string? forceErrorText = null,
        FormFieldValidator<T>? validator = null,
        FormFieldErrorBuilder? errorBuilder = null,
        T? initialValue = default,
        bool enabled = true,
        AutovalidateMode autovalidateMode = AutovalidateMode.Disabled,
        string? restorationId = null,
        Key? key = null) : base(key)
    {
        Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        OnSaved = onSaved;
        OnReset = onReset;
        ForceErrorText = forceErrorText;
        Validator = validator;
        ErrorBuilder = errorBuilder;
        InitialValue = initialValue;
        Enabled = enabled;
        AutovalidateMode = autovalidateMode;
        RestorationId = restorationId;
    }

    public FormFieldBuilder<T> Builder { get; }
    public FormFieldSetter<T>? OnSaved { get; }
    public Action? OnReset { get; }
    public string? ForceErrorText { get; }
    public FormFieldValidator<T>? Validator { get; }
    public FormFieldErrorBuilder? ErrorBuilder { get; }
    public T? InitialValue { get; }
    public bool Enabled { get; }
    public AutovalidateMode AutovalidateMode { get; }
    public string? RestorationId { get; }

    public override State CreateState() => new FormFieldState<T>();
}

public class FormFieldState<T> : FormFieldState
{
    private T? _value;
    private string? _errorText;
    private bool _hasInteractedByUser;

    protected FormField<T> CurrentField => (FormField<T>)StateWidget;

    public T? Value => _value;
    public override string? ErrorText => _errorText;
    public override bool HasInteractedByUser => _hasInteractedByUser;
    public override bool IsValid => CurrentField.ForceErrorText is null
        && CurrentField.Validator?.Invoke(_value) is null;
    protected override AutovalidateMode EffectiveAutovalidateMode =>
        CurrentField.AutovalidateMode == AutovalidateMode.Disabled
            ? RegisteredForm?.CurrentAutovalidateMode ?? AutovalidateMode.Disabled
            : CurrentField.AutovalidateMode;
    protected override bool FieldEnabled => CurrentField.Enabled;

    public override void InitState()
    {
        _value = CurrentField.InitialValue;
        _errorText = CurrentField.ForceErrorText;
        base.InitState();
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var oldField = (FormField<T>)oldWidget;
        if (!string.Equals(oldField.ForceErrorText, CurrentField.ForceErrorText, StringComparison.Ordinal))
            _errorText = CurrentField.ForceErrorText;
    }

    public override void DidChangeDependencies()
    {
        var form = Form.MaybeOf(Context);
        if (form?.CurrentAutovalidateMode == AutovalidateMode.Always
            && CurrentField.Enabled
            && !HasError
            && !IsValid)
        {
            Validate();
        }
    }

    public override Widget Build(BuildContext context)
    {
        RegisterWithForm(context);
        if (CurrentField.Enabled)
        {
            switch (CurrentField.AutovalidateMode)
            {
                case AutovalidateMode.Always:
                    ValidateInternal();
                    break;
                case AutovalidateMode.OnUserInteraction when _hasInteractedByUser:
                    ValidateInternal();
                    break;
                case AutovalidateMode.OnUserInteractionIfError when _hasInteractedByUser && HasError:
                    ValidateInternal();
                    break;
            }
        }

        return new Semantics(
            flags: HasError ? SemanticsFlags.IsInvalid : SemanticsFlags.None,
            child: CurrentField.Builder(this));
    }

    public override void Save() => CurrentField.OnSaved?.Invoke(_value);

    public override void Reset()
    {
        SetState(() =>
        {
            _value = CurrentField.InitialValue;
            ClearErrorInternal();
        });
        CurrentField.OnReset?.Invoke();
        RegisteredForm?.FieldDidChange();
    }

    public override void ClearError()
    {
        SetState(ClearErrorInternal);
        RegisteredForm?.FieldDidChange();
    }

    internal override void ClearErrorInternal()
    {
        _errorText = null;
        _hasInteractedByUser = false;
    }

    public override bool Validate()
    {
        SetState(ValidateInternal);
        return !HasError;
    }

    public virtual void DidChange(T? value)
    {
        SetState(() =>
        {
            _value = value;
            _hasInteractedByUser = true;
        });
        RegisteredForm?.FieldDidChange();
    }

    protected void SetValue(T? value) => _value = value;

    private void ValidateInternal()
    {
        _errorText = CurrentField.ForceErrorText
            ?? CurrentField.Validator?.Invoke(_value);
    }
}
