using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/shared_app_data.dart

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
        DebugHasSharedAppData(model, context, "getValue");
        return model!.State.GetValue(key, init);
    }

    public static void SetValue<TKey, TValue>(BuildContext context, TKey key, TValue value)
        where TKey : notnull
    {
        var model = context.GetInheritedWidgetOfExactType<SharedAppModel>();
        DebugHasSharedAppData(model, context, "setValue");
        model!.State.SetValue(key, value);
    }

    private static bool DebugHasSharedAppData(SharedAppModel? model, BuildContext context, string methodName)
    {
        if (Constants.KDebugMode && model is null)
        {
            throw new FlutterError(
            [
                new ErrorSummary("No SharedAppData widget found."),
                new ErrorDescription(
                    $"SharedAppData.{methodName} requires an SharedAppData widget ancestor.\n"),
                context.DescribeWidget(
                    "The specific widget that could not find an SharedAppData ancestor was"),
                context.DescribeOwnershipChain("The ownership chain for the affected widget is"),
                new ErrorHint(
                    "Typically, the SharedAppData widget is introduced by the MaterialApp "
                    + "or WidgetsApp widget at the top of your application widget tree. It "
                    + "provides a key/value map of data that is shared with the entire "
                    + "application."),
            ]);
        }

        return true;
    }

    private sealed class SharedAppDataState : State<SharedAppData>
    {
        private Dictionary<object, object?> _data = [];

        public override Widget Build(BuildContext context)
        {
            return new SharedAppModel(this, _data, Widget.Child);
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
            Widget child) : base(child)
        {
            State = state;
            _data = data;
        }

        public SharedAppDataState State { get; }

        public override bool UpdateShouldNotify(InheritedWidget oldWidget)
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
