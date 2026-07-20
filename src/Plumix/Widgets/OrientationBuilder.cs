using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/orientation_builder.dart

/// <summary>The signature of an <see cref="OrientationBuilder"/> builder callback.</summary>
public delegate Widget OrientationWidgetBuilder(BuildContext context, Orientation orientation);

/// <summary>Builds a widget tree from the orientation of the incoming layout constraints.</summary>
public sealed class OrientationBuilder : StatelessWidget
{
    public OrientationBuilder(OrientationWidgetBuilder builder, Key? key = null) : base(key)
    {
        Builder = builder ?? throw new ArgumentNullException(nameof(builder));
    }

    public OrientationWidgetBuilder Builder { get; }

    public override Widget Build(BuildContext context)
    {
        return new LayoutBuilder((builderContext, constraints) =>
        {
            Orientation orientation = constraints.MaxWidth > constraints.MaxHeight
                ? Orientation.Landscape
                : Orientation.Portrait;
            return Builder(builderContext, orientation);
        });
    }
}
