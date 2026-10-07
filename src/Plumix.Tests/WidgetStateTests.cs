using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/widget_state.dart
public sealed class WidgetStateTests
{
    [Fact]
    public void StatesController_NotifiesOnlyWhenTheSetActuallyChanges()
    {
        var controller = new WidgetStatesController();
        int notifications = 0;
        controller.AddListener(() => notifications++);

        controller.Update(WidgetState.Hovered, add: true);
        Assert.Equal(1, notifications);
        Assert.Contains(WidgetState.Hovered, controller.Value);

        controller.Update(WidgetState.Hovered, add: true);
        Assert.Equal(1, notifications);

        controller.Update(WidgetState.Hovered, add: false);
        Assert.Equal(2, notifications);
        Assert.DoesNotContain(WidgetState.Hovered, controller.Value);

        controller.Update(WidgetState.Hovered, add: false);
        Assert.Equal(2, notifications);
    }

    [Fact]
    public void StatesController_SeedsFromTheConstructorValue()
    {
        var controller = new WidgetStatesController([WidgetState.Disabled, WidgetState.Focused]);

        Assert.Contains(WidgetState.Disabled, controller.Value);
        Assert.Contains(WidgetState.Focused, controller.Value);
        Assert.Equal(2, controller.Value.Count);
    }

    // Dart's `update` mutates `value`, so it acts on a set assigned through the setter (the date
    // range picker's `_DayItem` assigns its states in build, then `InkResponse` adds `hovered`).
    [Fact]
    public void StatesController_UpdateActsOnTheAssignedValue()
    {
        var controller = new WidgetStatesController();
        int notifications = 0;
        controller.AddListener(() => notifications++);

        var assigned = new HashSet<WidgetState> { WidgetState.Selected };
        controller.Value = assigned;
        Assert.Equal(1, notifications);

        controller.Update(WidgetState.Hovered, add: true);
        Assert.Equal(2, notifications);
        Assert.Same(assigned, controller.Value);
        Assert.Equal(new HashSet<WidgetState> { WidgetState.Selected, WidgetState.Hovered }, controller.Value);

        controller.Update(WidgetState.Selected, add: false);
        Assert.Equal(3, notifications);
        Assert.Equal(new HashSet<WidgetState> { WidgetState.Hovered }, controller.Value);
    }

    [Fact]
    public void FromMap_ReturnsTheFirstSatisfiedEntryInDeclarationOrder()
    {
        WidgetStateProperty<string> property = WidgetStateProperty<string>.FromMap(
        [
            new(WidgetState.Dragged, "dragged"),
            new(WidgetState.Pressed, "pressed"),
            new(WidgetStatesConstraint.Any, "rest"),
        ]);

        Assert.Equal("dragged", property.Resolve(new HashSet<WidgetState> { WidgetState.Dragged }));
        Assert.Equal("pressed", property.Resolve(new HashSet<WidgetState> { WidgetState.Pressed }));

        // Both satisfied: the earlier entry wins, exactly like Dart's ordered map.
        Assert.Equal(
            "dragged",
            property.Resolve(new HashSet<WidgetState> { WidgetState.Pressed, WidgetState.Dragged }));
        Assert.Equal("rest", property.Resolve(new HashSet<WidgetState>()));
    }

    [Fact]
    public void FromMap_WithNoMatchingEntryYieldsTheDefaultOrThrows()
    {
        WidgetStateProperty<string?> nullable = WidgetStateProperty<string?>.FromMap(
            [new(WidgetState.Pressed, "pressed")]);
        Assert.Null(nullable.Resolve(new HashSet<WidgetState>()));

        WidgetStateProperty<int> nonNullable = WidgetStateProperty<int>.FromMap(
            [new(WidgetState.Pressed, 1)]);
        Assert.Throws<ArgumentException>(() => nonNullable.Resolve(new HashSet<WidgetState>()));
    }

    [Fact]
    public void Constraints_CombineWithAndOrAndNot()
    {
        WidgetStatesConstraint hovered = WidgetState.Hovered;
        WidgetStatesConstraint focused = WidgetState.Focused;
        var hover = new HashSet<WidgetState> { WidgetState.Hovered };
        var both = new HashSet<WidgetState> { WidgetState.Hovered, WidgetState.Focused };

        Assert.False((hovered & focused).IsSatisfiedBy(hover));
        Assert.True((hovered & focused).IsSatisfiedBy(both));
        Assert.True((hovered | focused).IsSatisfiedBy(hover));
        Assert.False((~hovered).IsSatisfiedBy(hover));
        Assert.True((~focused).IsSatisfiedBy(hover));
        Assert.True(WidgetStatesConstraint.Any.IsSatisfiedBy(new HashSet<WidgetState>()));
    }
    [Fact]
    public void TextStyle_ResolveWithIsLazyAndHasAnEmptyBaseStyle()
    {
        int calls = 0;
        IReadOnlySet<WidgetState>? received = null;
        var resolved = new TextStyle(Color: Colors.Red, FontSize: 27.0);
        TextStyle style = WidgetStateTextStyle.ResolveWith(states =>
        {
            calls++;
            received = states;
            return resolved;
        });

        Assert.Equal(0, calls);
        Assert.True(style.Inherit);
        Assert.Null(style.Color);
        Assert.Null(style.FontSize);
        Assert.Equal(new TextStyle(FontSize: 14.0), new TextStyle(FontSize: 14.0).Merge(style));
        Assert.Equal(0, calls);

        foreach (WidgetState state in Enum.GetValues<WidgetState>())
        {
            var states = new HashSet<WidgetState> { state };
            Assert.Same(resolved, WidgetStateProperty<TextStyle>.ResolveAs(style, states));
            Assert.Same(states, received);
        }
        Assert.Equal(Enum.GetValues<WidgetState>().Length, calls);
        Assert.Same(resolved, WidgetStateProperty<TextStyle>.ResolveAs(style, new HashSet<WidgetState>()));
    }

    [Fact]
    public void TextStyle_SubclassesAndPlainStylesUseTheSameValueSlot()
    {
        TextStyle custom = new FocusTextStyle();
        var focused = new HashSet<WidgetState> { WidgetState.Focused };
        Assert.Equal(Colors.Red, WidgetStateProperty<TextStyle>.ResolveAs(custom, focused).Color);
        Assert.Equal(Colors.Blue, WidgetStateProperty<TextStyle>.ResolveAs(custom, new HashSet<WidgetState>()).Color);
        Assert.Null(custom.Color);
        var plain = new TextStyle(Color: Colors.Green);
        Assert.Same(plain, WidgetStateProperty<TextStyle>.ResolveAs(plain, focused));
        Assert.Null(WidgetStateProperty<TextStyle?>.ResolveAs(null, focused));
    }

    [Fact]
    public void TextStyle_ResolversInheritTextStyleEqualityInsteadOfComparingCallbacks()
    {
        TextStyle first = WidgetStateTextStyle.ResolveWith(_ => new TextStyle(Color: Colors.Red));
        TextStyle second = WidgetStateTextStyle.ResolveWith(_ => new TextStyle(Color: Colors.Blue));
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(new TextStyle(), first);
    }

    [Fact]
    public void TextStyle_FromMapResolvesInOrderAndRequiresAFallback()
    {
        var first = new TextStyle(Color: Colors.Red);
        var fallback = new TextStyle(Color: Colors.Blue);
        var style = WidgetStateTextStyle.FromMap(
        [
            new((WidgetStatesConstraint)WidgetState.Hovered | WidgetState.Focused, first),
            new(WidgetState.Focused, fallback),
            new(WidgetStatesConstraint.Any, fallback),
        ]);
        Assert.Same(first, style.Resolve(new HashSet<WidgetState> { WidgetState.Focused }));
        Assert.Same(first, style.Resolve(new HashSet<WidgetState> { WidgetState.Hovered }));
        Assert.Same(fallback, style.Resolve(new HashSet<WidgetState>()));

        var noFallback = WidgetStateTextStyle.FromMap([new(WidgetState.Focused, first)]);
        Assert.Throws<ArgumentException>(() => noFallback.Resolve(new HashSet<WidgetState>()));
    }

    [Fact]
    public void TextStyle_FromMapEqualityAndHashIgnoreEntryOrderAndMapperSubtype()
    {
        var white = new TextStyle(Color: Colors.White);
        var black = new TextStyle(Color: Colors.Black);
        IReadOnlyList<KeyValuePair<WidgetStatesConstraint, TextStyle>> map =
        [
            new((WidgetStatesConstraint)WidgetState.Focused | WidgetState.Hovered, white),
            new(WidgetStatesConstraint.Any, black),
        ];
        TextStyle first = WidgetStateTextStyle.FromMap(map);
        TextStyle equal = WidgetStateTextStyle.FromMap(map.Reverse().ToList());
        TextStyle different = WidgetStateTextStyle.FromMap(
        [
            new((WidgetStatesConstraint)WidgetState.Focused | WidgetState.Hovered, black),
            new(WidgetStatesConstraint.Any, white),
        ]);
        WidgetStateProperty<TextStyle> generic = WidgetStateProperty<TextStyle>.FromMap(map);
        Assert.True(first == equal);
        Assert.False(first == different);
        Assert.True(first.Equals(generic));
        Assert.True(generic.Equals(first));
        Assert.Equal(first.GetHashCode(), equal.GetHashCode());
        Assert.Equal(first.GetHashCode(), generic.GetHashCode());
    }

    [Fact]
    public void TextStyle_FromMapRejectsOrdinaryTextStyleFieldsAndOperations()
    {
        TextStyle style = WidgetStateTextStyle.FromMap([new(WidgetStatesConstraint.Any, new TextStyle())]);
        foreach (var property in typeof(TextStyle).GetProperties())
        {
            var error = Assert.Throws<System.Reflection.TargetInvocationException>(() => property.GetValue(style));
            Assert.IsType<FlutterError>(error.InnerException);
        }
        Assert.Throws<FlutterError>(() => style.Merge(null));
        Assert.Throws<FlutterError>(() => style.CopyWith());
        Assert.Throws<FlutterError>(() => style.Apply());
        Assert.Throws<FlutterError>(() => style.CompareTo(style));
        Assert.Throws<FlutterError>(() => style.GetTextStyle());
        Assert.Throws<FlutterError>(() => style.GetParagraphStyle());
        var diagnostics = new DiagnosticPropertiesBuilder();
        style.DebugFillProperties(diagnostics);
        Assert.Equal("map", Assert.Single(diagnostics.Properties).Name);
        Assert.Equal("WidgetStateMapper<TextStyle>({WidgetState.any: " + new TextStyle() + "})",
            style.ToString());
    }

    [Theory]
    [InlineData(0.0, 14.0)]
    [InlineData(0.5, 17.0)]
    [InlineData(1.0, 20.0)]
    public void TextStyle_StatePropertyLerpResolvesEachState(double t, double expected)
    {
        var first = WidgetStateProperty<TextStyle?>.ResolveWith(states => new TextStyle(
            FontSize: states.Contains(WidgetState.Focused) ? 14.0 : 10.0));
        var second = WidgetStateProperty<TextStyle?>.ResolveWith(states => new TextStyle(
            FontSize: states.Contains(WidgetState.Focused) ? 20.0 : 10.0));
        WidgetStateProperty<TextStyle?>? lerp = WidgetStateProperty<TextStyle?>.Lerp(first, second, t, TextStyle.Lerp);
        Assert.Equal(expected, lerp!.Resolve(new HashSet<WidgetState> { WidgetState.Focused })!.FontSize);
        Assert.Equal(10.0, lerp.Resolve(new HashSet<WidgetState>())!.FontSize);
        Assert.Null(WidgetStateProperty<TextStyle?>.Lerp(null, null, t, TextStyle.Lerp));
    }

    private sealed class FocusTextStyle : WidgetStateTextStyle
    {
        public override TextStyle Resolve(IReadOnlySet<WidgetState> states) => new(
            Color: states.Contains(WidgetState.Focused) ? Colors.Red : Colors.Blue);
    }
}
