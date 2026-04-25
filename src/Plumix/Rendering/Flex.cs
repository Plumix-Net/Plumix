// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/flex.dart (approximate)

namespace Plumix.Rendering;

public enum MainAxisSize
{
    Min,
    Max
}

public enum MainAxisAlignment
{
    Start,
    Center,
    End,
    SpaceBetween,
    SpaceAround,
    SpaceEvenly
}

public enum CrossAxisAlignment
{
    Start,
    Center,
    End,
    Stretch,
    Baseline
}

public enum Axis
{
    Horizontal,
    Vertical
}

public enum FlexFit
{
    Tight,
    Loose,
}

public sealed class FlexParentData : ContainerBoxParentData<RenderBox>
{
    public int? flex;

    public FlexFit? fit;

    public override string ToString() => $"{base.ToString()}; flex={flex}; fit={fit}";
}
