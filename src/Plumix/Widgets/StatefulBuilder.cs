using Plumix.Foundation;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart (StatefulBuilder)

public delegate void StateSetter(Action callback);

public delegate Widget StatefulWidgetBuilder(BuildContext context, StateSetter setState);

public sealed class StatefulBuilder : StatefulWidget
{
    public StatefulBuilder(StatefulWidgetBuilder builder, Key? key = null) : base(key)
    {
        BuilderCallback = builder ?? throw new ArgumentNullException(nameof(builder));
    }

    public StatefulWidgetBuilder BuilderCallback { get; }

    public override State CreateState() => new StatefulBuilderState();

    private sealed class StatefulBuilderState : State
    {
        public override Widget Build(BuildContext context)
        {
            return ((StatefulBuilder)StateWidget).BuilderCallback(context, SetState);
        }
    }
}
