using Plumix.Foundation;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/foundation/key.dart
// Covers key.dart's contracts and the UniqueKey control test in widgets/framework_test.dart.

namespace Plumix.Tests;

public sealed class KeyParityTests
{
    [Fact]
    public void KeyFactoryCreatesAStringValueKey()
    {
        Key key = Key.Create("dependent");

        Assert.IsType<ValueKey<string>>(key);
        Assert.Equal(new ValueKey<string>("dependent"), key);
        Assert.Equal("[<'dependent'>]", key.ToString());
    }

    [Fact]
    public void UniqueKeysUseIdentityAndHaveASingleLineDescription()
    {
        var first = new UniqueKey();
        var second = new UniqueKey();

        Assert.Equal(first, first);
        Assert.NotEqual(first, second);
        Assert.Matches(@"^\[#\w+\]$", first.ToString());
    }

    [Fact]
    public void ValueKeysCompareTheirRuntimeTypeAndValue()
    {
        var first = new ValueKey<int>(7);
        var equal = new ValueKey<int>(7);

        Assert.Equal(first, equal);
        Assert.Equal(first.GetHashCode(), equal.GetHashCode());
        Assert.NotEqual(first, new ValueKey<int>(8));
        Assert.NotEqual<Key>(first, new ValueKey<long>(7));
        Assert.NotEqual<Key>(first, new PageStorageKey<int>(7));
        Assert.Equal(new PageStorageKey<int>(7), new PageStorageKey<int>(7));
    }

    [Fact]
    public void ValueKeySubclassInheritsTheFlutterPrinter()
    {
        Assert.Equal("[<7>]", new ValueKey<int>(7).ToString());
        Assert.Equal("[int <7>]", new PageStorageKey<int>(7).ToString());
        Assert.Equal("[bool <true>]", new PageStorageKey<bool>(true).ToString());
        Assert.Equal("[String <'row'>]", new PageStorageKey<string>("row").ToString());
        Assert.Equal("[<null>]", new ValueKey<object?>(null).ToString());
        Assert.Equal("[int? <null>]", new PageStorageKey<int?>(null).ToString());
    }
}
