using Plumix.Rendering;
using Plumix.Foundation;
using Plumix.UI;

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
        Key? key = null,
        bool? canPop = null,
        PopInvokedCallback? onPopInvoked = null,
        PopInvokedWithResultCallback<object?>? onPopInvokedWithResult = null) : base(key)
    {
        if (onPopInvokedWithResult != null && onPopInvoked != null)
        {
            throw new ArgumentException(
                "onPopInvoked and onPopInvokedWithResult cannot both be provided.",
                nameof(onPopInvoked));
        }

        Child = child ?? throw new ArgumentNullException(nameof(child));
        CanPop = canPop;
#pragma warning disable CS0618
        OnPopInvoked = onPopInvoked;
#pragma warning restore CS0618
        OnPopInvokedWithResult = onPopInvokedWithResult;
        OnChanged = onChanged;
        AutovalidateMode = autovalidateMode;
    }

    public Widget Child { get; }
    public bool? CanPop { get; }
    [Obsolete("Use OnPopInvokedWithResult instead.")]
    public PopInvokedCallback? OnPopInvoked { get; }
    public PopInvokedWithResultCallback<object?>? OnPopInvokedWithResult { get; }
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
        bool hasError = _fields.Any(field => field.HasError);
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

        Widget form = new FormScope(this, _generation, Current.Child);
        if (Current.CanPop != null || Current.OnPopInvokedWithResult != null
#pragma warning disable CS0618
            || Current.OnPopInvoked != null)
#pragma warning restore CS0618
        {
            form = new PopScope<object?>(
                canPop: Current.CanPop ?? true,
                onPopInvokedWithResult: CallPopInvoked,
                child: form);
        }

        return new Semantics(
            container: true,
            explicitChildNodes: true,
            role: SemanticsRole.Form,
            child: form);
    }

    private void CallPopInvoked(bool didPop, object? result)
    {
        if (Current.OnPopInvokedWithResult != null)
        {
            Current.OnPopInvokedWithResult(didPop, result);
            return;
        }

#pragma warning disable CS0618
        Current.OnPopInvoked?.Invoke(didPop);
#pragma warning restore CS0618
    }

    private void ForceRebuild() => SetState(() => _generation++);

    private bool ValidateFields(ISet<FormFieldState>? invalidFields)
    {
        bool valid = true;
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

public abstract class FormFieldState : RestorationState
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
        base.Dispose();
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
        bool hasFocusWithin = HasFocusWithin();
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
    private readonly RestorableBool _hasInteractedByUser = new(false);
    private RestorableStringN _errorText = null!;
    private T? _value;

    protected FormField<T> CurrentField => (FormField<T>)StateWidget;

    public T? Value => _value;
    public override string? ErrorText => _errorText.Value;
    public override bool HasInteractedByUser => _hasInteractedByUser.Value;
    protected override string? RestorationId => CurrentField.RestorationId;
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
        _errorText = new RestorableStringN(CurrentField.ForceErrorText);
        base.InitState();
    }

    protected override void RestoreState(RestorationBucket? oldBucket, bool initialRestore)
    {
        RegisterForRestoration(_errorText, "error_text");
        RegisterForRestoration(_hasInteractedByUser, "has_interacted_by_user");
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var oldField = (FormField<T>)oldWidget;
        if (!string.Equals(oldField.ForceErrorText, CurrentField.ForceErrorText, StringComparison.Ordinal))
            _errorText.Value = CurrentField.ForceErrorText;
    }

    public override void Dispose()
    {
        _errorText.Dispose();
        _hasInteractedByUser.Dispose();
        base.Dispose();
    }

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
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
                case AutovalidateMode.OnUserInteraction when HasInteractedByUser:
                    ValidateInternal();
                    break;
                case AutovalidateMode.OnUserInteractionIfError when HasInteractedByUser && HasError:
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
        _errorText.Value = null;
        _hasInteractedByUser.Value = false;
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
            _hasInteractedByUser.Value = true;
        });
        RegisteredForm?.FieldDidChange();
    }

    protected void SetValue(T? value) => _value = value;

    private void ValidateInternal()
    {
        _errorText.Value = CurrentField.ForceErrorText
            ?? CurrentField.Validator?.Invoke(_value);
    }
}
