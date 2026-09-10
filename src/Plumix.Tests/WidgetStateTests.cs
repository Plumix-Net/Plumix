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
}
