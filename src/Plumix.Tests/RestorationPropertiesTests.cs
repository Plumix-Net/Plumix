using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

internal enum RestorableTestEnum
{
    One,
    Two,
    Three,
    Four,
}

/// <summary>Ports Flutter's <c>test/widgets/restorable_property_test.dart</c>.</summary>
[Collection(SchedulerTestCollection.Name)]
public sealed class RestorationPropertiesTests : IDisposable
{
    private readonly MockRestorationManager _manager = new();

    public RestorationPropertiesTests() => Scheduler.ResetForTests();

    public void Dispose() => Scheduler.ResetForTests();

    [Fact]
    public void Value_IsNotAccessibleBeforeTheePropertyIsRegistered()
    {
        Assert.Throws<InvalidOperationException>(() => new RestorableNum<double>(0.0).Value);
        Assert.Throws<InvalidOperationException>(() => new RestorableDouble(1.0).Value);
        Assert.Throws<InvalidOperationException>(() => new RestorableInt(1).Value);
        Assert.Throws<InvalidOperationException>(() => new RestorableString("hello").Value);
        Assert.Throws<InvalidOperationException>(() => new RestorableBool(true).Value);
        Assert.Throws<InvalidOperationException>(() => new RestorableNumN<double>(0.0).Value);
        Assert.Throws<InvalidOperationException>(() => new RestorableDoubleN(1.0).Value);
        Assert.Throws<InvalidOperationException>(() => new RestorableIntN(1).Value);
        Assert.Throws<InvalidOperationException>(() => new RestorableStringN("hello").Value);
        Assert.Throws<InvalidOperationException>(() => new RestorableBoolN(true).Value);
        Assert.Throws<InvalidOperationException>(() => new RestorableDateTime(new DateTime(2020, 4, 3)).Value);
        Assert.Throws<InvalidOperationException>(() => new RestorableDateTimeN(new DateTime(2020, 4, 3)).Value);
        Assert.Throws<InvalidOperationException>(() => new RestorableTextEditingController().Value);
        Assert.Throws<InvalidOperationException>(() => new RestorableEnum<RestorableTestEnum>(
            RestorableTestEnum.One,
            Enum.GetValues<RestorableTestEnum>()).Value);
        Assert.Throws<InvalidOperationException>(() => new RestorableEnumN<RestorableTestEnum>(
            RestorableTestEnum.One,
            Enum.GetValues<RestorableTestEnum>()).Value);
    }

    [Fact]
    public void UnregisteredProperties_CanStillBeDisposed()
    {
        var property = new RestorableInt(5);

        property.Dispose();
    }

    [Fact]
    public void Properties_UseTheirDefaultsWhenNoRestorationDataIsAvailable()
    {
        var bag = new PropertyBag();

        using var harness = new RestorationHarness(new UnmanagedRestorationScope(
            bucket: null,
            child: bag.Widget(restorationId: "widget")));

        AssertDefaults(bag);
    }

    [Fact]
    public void Properties_CanBeMutatedWithoutARestorationBucket()
    {
        var bag = new PropertyBag();

        using var harness = new RestorationHarness(new UnmanagedRestorationScope(
            bucket: null,
            child: bag.Widget(restorationId: "widget")));
        bag.Mutate();

        AssertMutatedValues(bag);
    }

    [Fact]
    public void Properties_RoundTripThroughTheRestorationData()
    {
        var rawData = RawRestorationData.Build();
        var first = new PropertyBag();
        using (var harness = new RestorationHarness(new UnmanagedRestorationScope(
            bucket: RestorationBucket.Root(_manager, rawData),
            child: first.Widget(restorationId: "widget"))))
        {
            first.Mutate();
            _manager.DoSerialization();
        }

        var second = new PropertyBag();
        using var restarted = new RestorationHarness(new UnmanagedRestorationScope(
            bucket: RestorationBucket.Root(_manager, rawData),
            child: second.Widget(restorationId: "widget")));

        AssertMutatedValues(second);
        Assert.NotSame(first.Controller.Value, second.Controller.Value);
    }

    [Fact]
    public void Properties_NotifyOnceOnRealChangesAndNeverOnEqualAssignments()
    {
        var bag = new PropertyBag();
        using var harness = new RestorationHarness(new UnmanagedRestorationScope(
            bucket: RestorationBucket.Root(_manager, RawRestorationData.Build()),
            child: bag.Widget(restorationId: "widget")));

        var log = new List<string>();
        bag.Num.AddListener(() => log.Add("num"));
        bag.Str.AddListener(() => log.Add("string"));
        bag.Flag.AddListener(() => log.Add("bool"));
        bag.Moment.AddListener(() => log.Add("dateTime"));
        bag.Choice.AddListener(() => log.Add("enum"));
        bag.Controller.AddListener(() => log.Add("controller"));

        bag.Num.Value = 42.2;
        bag.Str.Value = "guten tag";
        bag.Flag.Value = true;
        bag.Moment.Value = new DateTime(2020, 7, 4);
        bag.Choice.Value = RestorableTestEnum.Two;
        bag.Controller.Value.Text = "blabla";
        Assert.Equal(new[] { "num", "string", "bool", "dateTime", "enum", "controller" }, log);

        log.Clear();
        bag.Num.Value = 42.2;
        bag.Str.Value = "guten tag";
        bag.Flag.Value = true;
        bag.Moment.Value = new DateTime(2020, 7, 4);
        bag.Choice.Value = RestorableTestEnum.Two;
        bag.Controller.Value.Text = "blabla";
        Assert.Empty(log);
    }

    [Fact]
    public void RestorableValue_CallsDidUpdateValueOnlyOnRealChanges()
    {
        var property = new CountingRestorableValue(55);
        var bag = new PropertyBag();
        using var harness = new RestorationHarness(new UnmanagedRestorationScope(
            bucket: RestorationBucket.Root(_manager, RawRestorationData.Build()),
            child: bag.Widget(restorationId: "widget", extra: (property, "counting"))));

        Assert.Equal(0, property.DidUpdateValueCallCount);

        property.Value = 42;
        Assert.Equal(1, property.DidUpdateValueCallCount);

        property.Value = 42;
        Assert.Equal(1, property.DidUpdateValueCallCount);
    }

    [Fact]
    public void RestorableEnum_RejectsADefaultThatIsNotInTheValueSet()
    {
        var values = new[] { RestorableTestEnum.One, RestorableTestEnum.Two, RestorableTestEnum.Three };

        Assert.Throws<ArgumentException>(() => new RestorableEnum<RestorableTestEnum>(
            RestorableTestEnum.Four,
            values));
        Assert.Throws<ArgumentException>(() => new RestorableEnumN<RestorableTestEnum>(
            RestorableTestEnum.Four,
            values));
    }

    [Fact]
    public void RestorableEnum_RejectsUnknownValuesOnSetAndOnRestore()
    {
        var values = new[] { RestorableTestEnum.One, RestorableTestEnum.Two, RestorableTestEnum.Three };
        var strict = new RestorableEnum<RestorableTestEnum>(RestorableTestEnum.One, values);
        var nullable = new RestorableEnumN<RestorableTestEnum>(null, values);

        Assert.Throws<ArgumentException>(() => strict.Value = RestorableTestEnum.Four);
        Assert.Throws<ArgumentException>(() => nullable.Value = RestorableTestEnum.Four);
        Assert.Throws<ArgumentException>(() => strict.FromPrimitives("Four"));
        Assert.Throws<ArgumentException>(() => nullable.FromPrimitives("Four"));

        Assert.Equal(RestorableTestEnum.Two, strict.FromPrimitives("Two"));
        Assert.Equal(RestorableTestEnum.Two, nullable.FromPrimitives("Two"));
        Assert.Null(nullable.FromPrimitives(null));
        Assert.Equal(RestorableTestEnum.One, strict.FromPrimitives(42));
    }

    [Fact]
    public void RestorableDateTime_UsesMillisecondsSinceEpochAsItsPrimitive()
    {
        var moment = new DateTime(2021, 3, 16, 12, 30, 15);
        var bag = new PropertyBag();
        using var harness = new RestorationHarness(new UnmanagedRestorationScope(
            bucket: RestorationBucket.Root(_manager, RawRestorationData.Build()),
            child: bag.Widget(restorationId: "widget")));

        bag.Moment.Value = moment;

        Assert.Equal(RestorationSerialization.MillisecondsSinceEpoch(moment), bag.Moment.ToPrimitives());
        Assert.Equal(moment, bag.Moment.FromPrimitives(bag.Moment.ToPrimitives()));
        Assert.Null(bag.NullableMoment.ToPrimitives());
    }

    [Fact]
    public void RestorableChangeNotifier_DisposesTheReplacedObjectOnAMicrotask()
    {
        var bag = new PropertyBag();
        using var harness = new RestorationHarness(new UnmanagedRestorationScope(
            bucket: RestorationBucket.Root(_manager, RawRestorationData.Build()),
            child: bag.Widget(restorationId: "widget")));
        TextEditingController original = bag.Controller.Value;

        bag.Controller.InitWithValue(new TextEditingController("replacement"));

        original.AddListener(() => { });
        Scheduler.FlushMicrotasks();
        Assert.Throws<ObjectDisposedException>(() => original.AddListener(() => { }));
        Assert.Equal("replacement", bag.Controller.Value.Text);
    }

    [Fact]
    public void RestorableTextEditingController_SerializesOnlyTheText()
    {
        var bag = new PropertyBag();
        using var harness = new RestorationHarness(new UnmanagedRestorationScope(
            bucket: RestorationBucket.Root(_manager, RawRestorationData.Build()),
            child: bag.Widget(restorationId: "widget")));

        bag.Controller.Value.Text = "hello";

        Assert.Equal("hello", bag.Controller.ToPrimitives());
        Assert.Equal("restored", bag.Controller.FromPrimitives("restored").Text);
    }

    private static void AssertDefaults(PropertyBag bag)
    {
        Assert.Equal(99.0, bag.Num.Value);
        Assert.Equal(123.2, bag.Real.Value);
        Assert.Equal(42, bag.Whole.Value);
        Assert.Equal("hello world", bag.Str.Value);
        Assert.False(bag.Flag.Value);
        Assert.Equal(new DateTime(2021, 3, 16), bag.Moment.Value);
        Assert.Equal(RestorableTestEnum.One, bag.Choice.Value);
        Assert.Null(bag.NullableNum.Value);
        Assert.Null(bag.NullableReal.Value);
        Assert.Null(bag.NullableWhole.Value);
        Assert.Null(bag.NullableStr.Value);
        Assert.Null(bag.NullableFlag.Value);
        Assert.Null(bag.NullableMoment.Value);
        Assert.Null(bag.NullableChoice.Value);
        Assert.Equal("FooBar", bag.Controller.Value.Text);
    }

    private static void AssertMutatedValues(PropertyBag bag)
    {
        Assert.Equal(42.2, bag.Num.Value);
        Assert.Equal(441.3, bag.Real.Value);
        Assert.Equal(10, bag.Whole.Value);
        Assert.Equal("guten tag", bag.Str.Value);
        Assert.True(bag.Flag.Value);
        Assert.Equal(new DateTime(2020, 7, 4), bag.Moment.Value);
        Assert.Equal(RestorableTestEnum.Two, bag.Choice.Value);
        Assert.Equal(5.0, bag.NullableNum.Value);
        Assert.Equal(2.0, bag.NullableReal.Value);
        Assert.Equal(1, bag.NullableWhole.Value);
        Assert.Equal("hullo", bag.NullableStr.Value);
        Assert.False(bag.NullableFlag.Value);
        Assert.Equal(new DateTime(2020, 4, 4), bag.NullableMoment.Value);
        Assert.Equal(RestorableTestEnum.Three, bag.NullableChoice.Value);
        Assert.Equal("blabla", bag.Controller.Value.Text);
    }

    /// <summary>One instance of every built-in restorable property, plus the widget that owns them.</summary>
    private sealed class PropertyBag
    {
        public RestorableNum<double> Num { get; } = new(99.0);

        public RestorableDouble Real { get; } = new(123.2);

        public RestorableInt Whole { get; } = new(42);

        public RestorableString Str { get; } = new("hello world");

        public RestorableBool Flag { get; } = new(false);

        public RestorableDateTime Moment { get; } = new(new DateTime(2021, 3, 16));

        public RestorableEnum<RestorableTestEnum> Choice { get; } =
            new(RestorableTestEnum.One, Enum.GetValues<RestorableTestEnum>());

        public RestorableNumN<double> NullableNum { get; } = new(null);

        public RestorableDoubleN NullableReal { get; } = new(null);

        public RestorableIntN NullableWhole { get; } = new(null);

        public RestorableStringN NullableStr { get; } = new(null);

        public RestorableBoolN NullableFlag { get; } = new(null);

        public RestorableDateTimeN NullableMoment { get; } = new(null);

        public RestorableEnumN<RestorableTestEnum> NullableChoice { get; } =
            new(null, Enum.GetValues<RestorableTestEnum>());

        public RestorableTextEditingController Controller { get; } = new(text: "FooBar");

        public Widget Widget(string? restorationId, (RestorableProperty Property, string Id)? extra = null)
        {
            var registrations = new List<(RestorableProperty, string)>
            {
                (Num, "num"),
                (Real, "double"),
                (Whole, "int"),
                (Str, "string"),
                (Flag, "bool"),
                (Moment, "dateTime"),
                (Choice, "enum"),
                (NullableNum, "nullableNum"),
                (NullableReal, "nullableDouble"),
                (NullableWhole, "nullableInt"),
                (NullableStr, "nullableString"),
                (NullableFlag, "nullableBool"),
                (NullableMoment, "nullableDateTime"),
                (NullableChoice, "nullableEnum"),
                (Controller, "controller"),
            };
            if (extra is { } value)
            {
                registrations.Add((value.Property, value.Id));
            }

            return new PropertyBagWidget(restorationId, registrations);
        }

        public void Mutate()
        {
            Num.Value = 42.2;
            Real.Value = 441.3;
            Whole.Value = 10;
            Str.Value = "guten tag";
            Flag.Value = true;
            Moment.Value = new DateTime(2020, 7, 4);
            Choice.Value = RestorableTestEnum.Two;
            NullableNum.Value = 5.0;
            NullableReal.Value = 2.0;
            NullableWhole.Value = 1;
            NullableStr.Value = "hullo";
            NullableFlag.Value = false;
            NullableMoment.Value = new DateTime(2020, 4, 4);
            NullableChoice.Value = RestorableTestEnum.Three;
            Controller.Value.Text = "blabla";
        }
    }

    private sealed class PropertyBagWidget : StatefulWidget
    {
        public PropertyBagWidget(
            string? restorationId,
            IReadOnlyList<(RestorableProperty Property, string Id)> properties)
        {
            RestorationId = restorationId;
            Properties = properties;
        }

        public string? RestorationId { get; }

        public IReadOnlyList<(RestorableProperty Property, string Id)> Properties { get; }

        public override State CreateState() => new PropertyBagState();

        private sealed class PropertyBagState : RestorationState
        {
            private PropertyBagWidget CurrentWidget => (PropertyBagWidget)StateWidget;

            protected override string? RestorationId => CurrentWidget.RestorationId;

            protected override void RestoreState(RestorationBucket? oldBucket, bool initialRestore)
            {
                foreach ((RestorableProperty property, string id) in CurrentWidget.Properties)
                {
                    RegisterForRestoration(property, id);
                }
            }

            public override Widget Build(BuildContext context) => new SizedBox(width: 0.0, height: 0.0);
        }
    }

    private sealed class CountingRestorableValue : RestorableValue<int>
    {
        private readonly int _defaultValue;

        public CountingRestorableValue(int defaultValue)
        {
            _defaultValue = defaultValue;
        }

        public int DidUpdateValueCallCount { get; private set; }

        public override int CreateDefaultValue() => _defaultValue;

        public override int FromPrimitives(object? data) => (int)data!;

        public override object? ToPrimitives() => Value;

        protected override void DidUpdateValue(int oldValue)
        {
            DidUpdateValueCallCount++;
            NotifyListeners();
        }
    }
}
