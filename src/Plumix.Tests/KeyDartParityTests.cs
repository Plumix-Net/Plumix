using Plumix.Foundation;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart
// Mirrors flutter/packages/flutter/test/widgets/key_test.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class KeyDartParityTests
{
    // Flutter: key_test.dart: "Keys"
    [Fact]
    public void Keys()
    {
        Assert.True(new ValueKey<int>(3) == new ValueKey<int>(3));
        // Dart's `ValueKey<num>`: C# has no common numeric supertype, so a different numeric type
        // argument stands in for it.
        Assert.False(new ValueKey<double>(3) == new ValueKey<int>(3));
        Assert.False(new ValueKey<int>(3) == new ValueKey<int>(2));
        Assert.False(new ValueKey<double>(double.NaN) == new ValueKey<double>(double.NaN));

        Assert.True(Key.Create(string.Empty) == new ValueKey<string>(string.Empty));
        Assert.True(new ValueKey<string>(string.Empty) == new ValueKey<string>(string.Empty));
        Assert.False(new TestValueKey<string>(string.Empty) == new ValueKey<string>(string.Empty));
        Assert.True(new TestValueKey<string>(string.Empty) == new TestValueKey<string>(string.Empty));

        // Dart's `dynamic` type argument is C#'s `object`.
        Assert.False(new ValueKey<string>(string.Empty) == new ValueKey<object>(string.Empty));
        Assert.False(new TestValueKey<string>(string.Empty) == new TestValueKey<object>(string.Empty));

        Assert.False(new UniqueKey() == new UniqueKey());
        var k = new UniqueKey();
        Assert.False(new UniqueKey() == new UniqueKey());
#pragma warning disable CS1718 // Comparison made to same variable: `k == k` is the point.
        Assert.True(k == k);
#pragma warning restore CS1718

        Assert.True(new ValueKey<LocalKey>(k) == new ValueKey<LocalKey>(k));
        Assert.False(new ValueKey<LocalKey>(k) == new ValueKey<UniqueKey>(k));
        Assert.True(new ObjectKey(k) == new ObjectKey(k));

        var constNotEquals = new NotEquals();
        Assert.False(new ValueKey<NotEquals>(constNotEquals) == new ValueKey<NotEquals>(constNotEquals));
        Assert.True(new ObjectKey(constNotEquals) == new ObjectKey(constNotEquals));

        object constObject = new();
        Assert.True(new ObjectKey(constObject) == new ObjectKey(constObject));
        Assert.False(new ObjectKey(new object()) == new ObjectKey(new object()));

        AssertHasOneLineDescription(new ValueKey<bool>(true));
        AssertHasOneLineDescription(new UniqueKey());
        AssertHasOneLineDescription(new ObjectKey(true));
        AssertHasOneLineDescription(new LabeledGlobalKey<State>(null));
        AssertHasOneLineDescription(new LabeledGlobalKey<State>("hello"));
        AssertHasOneLineDescription(new GlobalObjectKey<State>(true));
    }

    /// <summary>Flutter's <c>hasOneLineDescription</c> matcher.</summary>
    private static void AssertHasOneLineDescription(object value)
    {
        string description = value.ToString()!;
        Assert.NotEmpty(description);
        Assert.DoesNotContain('\n', description);
        Assert.False(description.StartsWith("Instance of '", StringComparison.Ordinal));
    }

    private sealed class TestValueKey<T>(T value) : ValueKey<T>(value);

    private sealed class NotEquals
    {
        public override bool Equals(object? obj) => false;

        public override int GetHashCode() => 0;
    }
}
