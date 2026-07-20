using Plumix.Foundation;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/value_listenable_builder.dart

public delegate Widget ValueWidgetBuilder<in T>(BuildContext context, T value, Widget? child);

public sealed class ValueListenableBuilder<T> : StatefulWidget
{
    public ValueListenableBuilder(
        IValueListenable<T> valueListenable,
        ValueWidgetBuilder<T> builder,
        Widget? child = null,
        Key? key = null) : base(key)
    {
        ValueListenable = valueListenable ?? throw new ArgumentNullException(nameof(valueListenable));
        Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        Child = child;
    }

    public IValueListenable<T> ValueListenable { get; }

    public ValueWidgetBuilder<T> Builder { get; }

    public Widget? Child { get; }

    public override State CreateState() => new ValueListenableBuilderState();

    private sealed class ValueListenableBuilderState : State
    {
        private T _value = default!;

        private ValueListenableBuilder<T> CurrentWidget => (ValueListenableBuilder<T>)StateWidget;

        public override void InitState()
        {
            _value = CurrentWidget.ValueListenable.Value;
            CurrentWidget.ValueListenable.AddListener(HandleValueChanged);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldBuilder = (ValueListenableBuilder<T>)oldWidget;
            if (ReferenceEquals(oldBuilder.ValueListenable, CurrentWidget.ValueListenable))
            {
                return;
            }

            oldBuilder.ValueListenable.RemoveListener(HandleValueChanged);
            _value = CurrentWidget.ValueListenable.Value;
            CurrentWidget.ValueListenable.AddListener(HandleValueChanged);
        }

        public override Widget Build(BuildContext context)
        {
            return CurrentWidget.Builder(context, _value, CurrentWidget.Child);
        }

        public override void Dispose()
        {
            CurrentWidget.ValueListenable.RemoveListener(HandleValueChanged);
        }

        private void HandleValueChanged()
        {
            if (!Mounted)
            {
                return;
            }

            SetState(() => _value = CurrentWidget.ValueListenable.Value);
        }
    }
}
