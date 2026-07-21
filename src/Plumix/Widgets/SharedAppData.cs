using Plumix.Foundation;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/shared_app_data.dart (exact structure)

namespace Plumix.Widgets;

public sealed class SharedAppData : StatefulWidget
{
    public SharedAppData(Widget child, Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public Widget Child { get; }

    public override State CreateState()
    {
        return new SharedAppDataState();
    }

    public static TValue GetValue<TKey, TValue>(
        BuildContext context,
        TKey key,
        Func<TValue> init)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(init);
        var model = InheritedModel<object>.InheritFrom<SharedAppModel>(context, key);
        if (model == null)
        {
            throw MissingAncestor(nameof(GetValue));
        }

        return model.State.GetValue(key, init);
    }

    public static void SetValue<TKey, TValue>(BuildContext context, TKey key, TValue value)
        where TKey : notnull
    {
        var model = context.GetInherited<SharedAppModel>();
        if (model == null)
        {
            throw MissingAncestor(nameof(SetValue));
        }

        model.State.SetValue(key, value);
    }

    private static InvalidOperationException MissingAncestor(string methodName)
    {
        return new InvalidOperationException(
            $"SharedAppData.{methodName} requires a SharedAppData widget ancestor.");
    }

    private sealed class SharedAppDataState : State
    {
        private Dictionary<object, object?> _data = [];

        private SharedAppData CurrentWidget => (SharedAppData)StateWidget;

        public override Widget Build(BuildContext context)
        {
            return new SharedAppModel(this, _data, CurrentWidget.Child);
        }

        public TValue GetValue<TKey, TValue>(TKey key, Func<TValue> init)
            where TKey : notnull
        {
            if (!_data.TryGetValue(key, out object? value) || value == null)
            {
                value = init();
                _data[key] = value;
            }

            return (TValue)value!;
        }

        public void SetValue<TKey, TValue>(TKey key, TValue value)
            where TKey : notnull
        {
            if (_data.TryGetValue(key, out object? current) && Equals(current, value))
            {
                return;
            }

            SetState(() =>
            {
                _data = new Dictionary<object, object?>(_data)
                {
                    [key] = value,
                };
            });
        }
    }

    private sealed class SharedAppModel : InheritedModel<object>
    {
        private readonly IReadOnlyDictionary<object, object?> _data;

        public SharedAppModel(
            SharedAppDataState state,
            IReadOnlyDictionary<object, object?> data,
            Widget child) : base()
        {
            State = state;
            _data = data;
            Child = child;
        }

        public SharedAppDataState State { get; }

        public Widget Child { get; }

        public override Widget Build(BuildContext context)
        {
            return Child;
        }

        protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
        {
            return !ReferenceEquals(_data, ((SharedAppModel)oldWidget)._data);
        }

        protected override bool UpdateShouldNotifyDependent(
            InheritedModel<object> oldWidget,
            IReadOnlySet<object> dependencies)
        {
            var oldModel = (SharedAppModel)oldWidget;
            foreach (object key in dependencies)
            {
                _data.TryGetValue(key, out object? value);
                oldModel._data.TryGetValue(key, out object? oldValue);
                if (!Equals(value, oldValue))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
