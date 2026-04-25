using Xunit;

// Dart parity source (reference): flutter/packages/flutter/lib/src/scheduler/binding.dart; flutter/packages/flutter/lib/src/rendering/object.dart (parity regression tests)

namespace Plumix.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SchedulerTestCollection
{
    public const string Name = "Scheduler serial";
}
