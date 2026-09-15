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
    public void ValueKeySubclassIgnoresExtraFieldsAsDartDoes()
    {
        Key first = new SaltedValueKey(7, "one");
        Key second = new SaltedValueKey(7, "two");

        Assert.True(first == second);
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.Single(new HashSet<Key> { first, second });
        Assert.NotEqual(first, new ValueKey<int>(7));
        Assert.NotEqual(first, new OtherValueKey(7));
        Assert.True(Widget.CanUpdate(new SizedBox(key: first), new SizedBox(key: second)));
    }

    [Fact]
    public void OtherKeyFamiliesRetainTheirOwnEqualityContracts()
    {
        object value = new object();
        Key objectKey = new ObjectKey(value);
        Key matchingObjectKey = new ObjectKey(value);
        Key globalObjectKey = new GlobalObjectKey<State>(value);

        Assert.True(objectKey == matchingObjectKey);
        Assert.NotEqual(objectKey, new ObjectKey(new object()));
        Assert.Equal(globalObjectKey, new GlobalObjectKey<State>(value));
        Assert.NotEqual(globalObjectKey, new OtherGlobalObjectKey(value));
        Assert.NotEqual(new LabeledGlobalKey<State>("label"), new LabeledGlobalKey<State>("label"));
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

    private sealed class SaltedValueKey(int value, string salt) : ValueKey<int>(value)
    {
        public string Salt { get; } = salt;
    }

    private sealed class OtherValueKey(int value) : ValueKey<int>(value);

    private sealed class OtherGlobalObjectKey(object value) : GlobalObjectKey<State>(value);
}
