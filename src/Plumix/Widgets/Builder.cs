using Plumix.Foundation;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart (Builder)

public delegate Widget WidgetBuilder(BuildContext context);

public sealed class Builder : StatelessWidget
{
    public Builder(WidgetBuilder builder, Key? key = null) : base(key)
    {
        BuilderCallback = builder ?? throw new ArgumentNullException(nameof(builder));
    }

    public WidgetBuilder BuilderCallback { get; }

    public override Widget Build(BuildContext context) => BuilderCallback(context);
}
