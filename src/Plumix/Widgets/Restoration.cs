using Plumix.Foundation;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/restoration.dart

namespace Plumix.Widgets;

/// <summary>
/// Creates a new scope for restoration IDs used by descendant widgets to claim
/// <see cref="RestorationBucket"/>s.
/// </summary>
public sealed class RestorationScope : StatefulWidget
{
    public RestorationScope(string? restorationId, Widget child, Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        RestorationId = restorationId;
    }

    public Widget Child { get; }

    /// <summary>The restoration ID used by this scope to claim its bucket, or null to disable it.</summary>
    public string? RestorationId { get; }

    /// <summary>Returns the <see cref="RestorationBucket"/> inserted by the closest ancestor scope.</summary>
    public static RestorationBucket? MaybeOf(BuildContext context)
    {
        return context.DependOnInherited<UnmanagedRestorationScope>()?.Bucket;
    }

    /// <summary>Same as <see cref="MaybeOf"/>, but throws when no bucket is available.</summary>
    public static RestorationBucket Of(BuildContext context)
    {
        return MaybeOf(context)
            ?? throw new InvalidOperationException(
                "RestorationScope.Of() was called with a context that does not contain a "
                + "RestorationScope widget.\n"
                + "State restoration must be enabled for a RestorationScope to exist. This can be done by "
                + "passing a restorationScopeId to MaterialApp, CupertinoApp, or WidgetsApp at the root of "
                + "the widget tree or by wrapping the widget tree in a RootRestorationScope.");
    }

    public override State CreateState() => new RestorationScopeState();

    private sealed class RestorationScopeState : RestorationState
    {
        private RestorationScope CurrentWidget => (RestorationScope)StateWidget;

        protected override string? RestorationId => CurrentWidget.RestorationId;

        protected override void RestoreState(RestorationBucket? oldBucket, bool initialRestore)
        {
        }

        public override Widget Build(BuildContext context)
        {
            return new UnmanagedRestorationScope(bucket: Bucket, child: CurrentWidget.Child);
        }
    }
}

/// <summary>
/// Inserts a provided <see cref="RestorationBucket"/> into the widget tree and makes it available to
/// descendants via <see cref="RestorationScope.MaybeOf"/>.
/// </summary>
public sealed class UnmanagedRestorationScope : InheritedWidget
{
    public UnmanagedRestorationScope(Widget child, RestorationBucket? bucket = null, Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Bucket = bucket;
    }

    public Widget Child { get; }

    /// <summary>The bucket made available to descendants, or null to disable restoration below.</summary>
    public RestorationBucket? Bucket { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !ReferenceEquals(((UnmanagedRestorationScope)oldWidget).Bucket, Bucket);
    }
}

/// <summary>
/// Inserts a child bucket of <see cref="RestorationManager.GetRootBucket"/> into the widget tree and
/// makes it available to descendants via <see cref="RestorationScope.MaybeOf"/>.
/// </summary>
public sealed class RootRestorationScope : StatefulWidget
{
    public RootRestorationScope(string? restorationId, Widget child, Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        RestorationId = restorationId;
    }

    public Widget Child { get; }

    /// <summary>The restoration ID used by this widget to claim its bucket from the root bucket.</summary>
    public string? RestorationId { get; }

    public override State CreateState() => new RootRestorationScopeState();

    private sealed class RootRestorationScopeState : State
    {
        private bool? _okToRenderBlankContainer;
        private bool _rootBucketValid;
        private RestorationBucket? _rootBucket;
        private RestorationBucket? _ancestorBucket;
        private bool _isLoadingRootBucket;

        private RootRestorationScope CurrentWidget => (RootRestorationScope)StateWidget;

        private bool NeedsRootBucketInserted => _ancestorBucket is null;

        private bool IsWaitingForRootBucket =>
            CurrentWidget.RestorationId is not null && NeedsRootBucketInserted && !_rootBucketValid;

        public override void DidChangeDependencies()
        {
            base.DidChangeDependencies();
            _ancestorBucket = RestorationScope.MaybeOf(Context);
            LoadRootBucketIfNecessary();
            _okToRenderBlankContainer ??= CurrentWidget.RestorationId is not null && NeedsRootBucketInserted;
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            base.DidUpdateWidget(oldWidget);
            LoadRootBucketIfNecessary();
        }

        public override void Dispose()
        {
            if (_rootBucketValid)
            {
                RestorationManager.Instance.RemoveListener(ReplaceRootBucket);
            }

            base.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            if (_okToRenderBlankContainer == true && IsWaitingForRootBucket)
            {
                return new SizedBox(width: 0.0, height: 0.0);
            }

            return new UnmanagedRestorationScope(
                bucket: _ancestorBucket ?? _rootBucket,
                child: new RestorationScope(
                    restorationId: CurrentWidget.RestorationId,
                    child: CurrentWidget.Child));
        }

        private void LoadRootBucketIfNecessary()
        {
            if (!IsWaitingForRootBucket || _isLoadingRootBucket)
            {
                return;
            }

            _isLoadingRootBucket = true;
            RestorationManager.Instance.GetRootBucket(bucket =>
            {
                _isLoadingRootBucket = false;
                if (!Mounted)
                {
                    return;
                }

                RestorationManager.Instance.AddListener(ReplaceRootBucket);
                SetState(() =>
                {
                    _rootBucket = bucket;
                    _rootBucketValid = true;
                    _okToRenderBlankContainer = false;
                });
            });
        }

        private void ReplaceRootBucket()
        {
            _rootBucketValid = false;
            _rootBucket = null;
            RestorationManager.Instance.RemoveListener(ReplaceRootBucket);
            LoadRootBucketIfNecessary();
        }
    }
}

/// <summary>
/// Manages an object of type <c>T</c>, whose value a <see cref="RestorationState"/> object wants to
/// have restored during state restoration.
/// </summary>
public abstract class RestorableProperty : ChangeNotifier
{
    private string? _restorationId;
    private RestorationState? _owner;
    private bool _propertyDisposed;

    /// <summary>Whether the object currently returned by the property should be serialized.</summary>
    public virtual bool Enabled => true;

    /// <summary>Returns the serializable representation of the current value.</summary>
    public abstract object? ToPrimitives();

    internal string? RegisteredRestorationId => _restorationId;

    internal RestorationState? Owner => _owner;

    internal bool PropertyDisposed => _propertyDisposed;

    /// <summary>Whether this property is currently registered with a <see cref="RestorationState"/>.</summary>
    protected bool IsRegistered
    {
        get
        {
            AssertPropertyNotDisposed();
            return _restorationId is not null;
        }
    }

    /// <summary>The <see cref="State"/> object that this property is registered with.</summary>
    protected State State
    {
        get
        {
            AssertRegistered();
            return _owner!;
        }
    }

    public override void Dispose()
    {
        AssertPropertyNotDisposed();
        _owner?.UnregisterProperty(this);
        base.Dispose();
        _propertyDisposed = true;
    }

    /// <summary>Throws unless this property is registered; used by the typed value accessors.</summary>
    protected void AssertRegistered()
    {
        if (!IsRegistered)
        {
            throw new InvalidOperationException(
                $"A {GetType().Name} must be registered with RegisterForRestoration before its value "
                + "can be accessed.");
        }
    }

    internal abstract object? CreateDefaultValueObject();

    internal abstract object? FromPrimitivesObject(object? data);

    internal abstract void InitWithValueObject(object? value);

    internal void Register(string restorationId, RestorationState owner)
    {
        AssertPropertyNotDisposed();
        _restorationId = restorationId;
        _owner = owner;
    }

    internal void Unregister()
    {
        AssertPropertyNotDisposed();
        _restorationId = null;
        _owner = null;
    }

    private void AssertPropertyNotDisposed()
    {
        if (_propertyDisposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }
}

/// <inheritdoc cref="RestorableProperty"/>
public abstract class RestorableProperty<T> : RestorableProperty
{
    /// <summary>Creates the default value for this property when no restoration data is available.</summary>
    public abstract T CreateDefaultValue();

    /// <summary>Restores the value from the serialized representation.</summary>
    public abstract T FromPrimitives(object? data);

    /// <summary>Called to initialize the property with the default or restored value.</summary>
    public abstract void InitWithValue(T value);

    internal sealed override object? CreateDefaultValueObject() => CreateDefaultValue();

    internal sealed override object? FromPrimitivesObject(object? data) => FromPrimitives(data);

    internal sealed override void InitWithValueObject(object? value) => InitWithValue((T)value!);
}

/// <summary>
/// Manages the restoration data for a <see cref="State"/> object.
/// </summary>
/// <remarks>
/// Dart parity source: <c>RestorationMixin</c>. C# has no mixins, so the mixin becomes an abstract
/// <see cref="State"/> subclass; overrides of <see cref="DidUpdateWidget"/>,
/// <see cref="DidChangeDependencies"/> and <see cref="Dispose"/> must chain to the base.
/// </remarks>
public abstract class RestorationState : State
{
    private readonly Dictionary<RestorableProperty, Action> _properties = [];

    private List<RestorableProperty>? _propertiesWaitingForReregistration;
    private RestorationBucket? _bucket;
    private RestorationBucket? _currentParent;
    private bool _firstRestorePending = true;

    /// <summary>The restoration ID used to claim this state's bucket, or null to disable restoration.</summary>
    protected abstract string? RestorationId { get; }

    /// <summary>The bucket this state stores its restoration data in, or null when unavailable.</summary>
    public RestorationBucket? Bucket => _bucket;

    /// <summary>Whether <see cref="RestoreState"/> will be called during the next build.</summary>
    protected bool RestorePending
    {
        get
        {
            if (_firstRestorePending)
            {
                return true;
            }

            if (RestorationId is null)
            {
                return false;
            }

            RestorationBucket? potentialNewParent = RestorationScope.MaybeOf(Context);
            return !ReferenceEquals(potentialNewParent, _currentParent)
                && (potentialNewParent?.IsReplacing ?? false);
        }
    }

    /// <summary>
    /// Restores the state from the given bucket. Every property registered before must be
    /// re-registered here.
    /// </summary>
    protected abstract void RestoreState(RestorationBucket? oldBucket, bool initialRestore);

    /// <summary>Called when <see cref="Bucket"/> switches between null and non-null.</summary>
    protected virtual void DidToggleBucket(RestorationBucket? oldBucket)
    {
    }

    /// <summary>Registers <paramref name="property"/> for restoration under <paramref name="restorationId"/>.</summary>
    protected void RegisterForRestoration(RestorableProperty property, string restorationId)
    {
        ArgumentNullException.ThrowIfNull(property);
        ArgumentNullException.ThrowIfNull(restorationId);

        bool doingRestore = _propertiesWaitingForReregistration is not null;
        if (property.RegisteredRestorationId is not null
            && !(doingRestore
                && string.Equals(property.RegisteredRestorationId, restorationId, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"Property is already registered under {property.RegisteredRestorationId}.");
        }

        if (!doingRestore
            && _properties.Keys.Any(registered =>
                string.Equals(registered.RegisteredRestorationId, restorationId, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"\"{restorationId}\" is already registered to another property.");
        }

        bool hasSerializedValue = _bucket?.Contains(restorationId) ?? false;
        object? initialValue = hasSerializedValue
            ? property.FromPrimitivesObject(_bucket!.Read<object>(restorationId))
            : property.CreateDefaultValueObject();

        if (property.RegisteredRestorationId is null)
        {
            void Listener()
            {
                if (_bucket is null)
                {
                    return;
                }

                UpdateProperty(property);
            }

            property.Register(restorationId, this);
            property.AddListener(Listener);
            _properties[property] = Listener;
        }

        property.InitWithValueObject(initialValue);
        if (!hasSerializedValue && property.Enabled && _bucket is not null)
        {
            UpdateProperty(property);
        }

        _propertiesWaitingForReregistration?.Remove(property);
    }

    /// <summary>Unregisters <paramref name="property"/> and removes its data from the bucket.</summary>
    protected void UnregisterFromRestoration(RestorableProperty property)
    {
        ArgumentNullException.ThrowIfNull(property);
        if (!ReferenceEquals(property.Owner, this))
        {
            throw new InvalidOperationException("The property is not registered with this state object.");
        }

        _bucket?.Remove<object>(property.RegisteredRestorationId!);
        UnregisterProperty(property);
    }

    /// <summary>Call when <see cref="RestorationId"/> changed outside of a widget update.</summary>
    protected void DidUpdateRestorationId()
    {
        if (_currentParent is null
            || string.Equals(_bucket?.RestorationId, RestorationId, StringComparison.Ordinal)
            || RestorePending)
        {
            return;
        }

        RestorationBucket? oldBucket = _bucket;
        bool didReplaceBucket = UpdateBucketIfNecessary(_currentParent, restorePending: false);
        if (didReplaceBucket)
        {
            oldBucket?.Dispose();
        }
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        DidUpdateRestorationId();
    }

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        RestorationBucket? oldBucket = _bucket;
        bool needsRestore = RestorePending;
        _currentParent = RestorationScope.MaybeOf(Context);

        bool didReplaceBucket = UpdateBucketIfNecessary(_currentParent, needsRestore);
        if (needsRestore)
        {
            DoRestore(oldBucket);
        }

        if (didReplaceBucket)
        {
            oldBucket?.Dispose();
        }
    }

    public override void Dispose()
    {
        foreach ((RestorableProperty property, Action listener) in _properties)
        {
            if (!property.PropertyDisposed)
            {
                property.RemoveListener(listener);
            }
        }

        _bucket?.Dispose();
        _bucket = null;
        base.Dispose();
    }

    internal void UnregisterProperty(RestorableProperty property)
    {
        if (!_properties.Remove(property, out Action? listener))
        {
            return;
        }

        _propertiesWaitingForReregistration?.Remove(property);
        property.RemoveListener(listener);
        property.Unregister();
    }

    private void DoRestore(RestorationBucket? oldBucket)
    {
        _propertiesWaitingForReregistration = [.. _properties.Keys];
        RestoreState(oldBucket, _firstRestorePending);
        _firstRestorePending = false;

        List<RestorableProperty> waiting = _propertiesWaitingForReregistration;
        _propertiesWaitingForReregistration = null;
        if (waiting.Count > 0)
        {
            string ids = string.Join(
                Environment.NewLine,
                waiting.Select(property => $" * {property.RegisteredRestorationId}"));
            throw new InvalidOperationException(
                "Previously registered RestorableProperties must be re-registered in \"RestoreState\".\n"
                + $"The RestorableProperties with the following IDs were not re-registered to {this} when "
                + $"\"RestoreState\" was called:{Environment.NewLine}{ids}");
        }
    }

    private bool UpdateBucketIfNecessary(RestorationBucket? parent, bool restorePending)
    {
        if (RestorationId is null || parent is null)
        {
            return SetNewBucketIfNecessary(newBucket: null, restorePending: restorePending);
        }

        if (restorePending || _bucket is null)
        {
            var newBucket = parent.ClaimChild(RestorationId, debugOwner: this);
            return SetNewBucketIfNecessary(newBucket: newBucket, restorePending: restorePending);
        }

        // The bucket and its data survive an id change: rename it in place and re-parent it.
        _bucket.Rename(RestorationId);
        parent.AdoptChild(_bucket);
        return false;
    }

    private bool SetNewBucketIfNecessary(RestorationBucket? newBucket, bool restorePending)
    {
        if (ReferenceEquals(newBucket, _bucket))
        {
            return false;
        }

        RestorationBucket? oldBucket = _bucket;
        _bucket = newBucket;
        if (!restorePending)
        {
            if (_bucket is not null)
            {
                foreach (RestorableProperty property in _properties.Keys.ToList())
                {
                    UpdateProperty(property);
                }
            }

            DidToggleBucket(oldBucket);
        }

        return true;
    }

    private void UpdateProperty(RestorableProperty property)
    {
        if (property.Enabled)
        {
            _bucket?.Write(property.RegisteredRestorationId!, property.ToPrimitives());
        }
        else
        {
            _bucket?.Remove<object>(property.RegisteredRestorationId!);
        }
    }
}
