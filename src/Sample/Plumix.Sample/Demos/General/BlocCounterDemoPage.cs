using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Plumix.Bloc;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source (reference): pub.dev/packages/bloc; pub.dev/packages/flutter_bloc (sample parity demo)

namespace Plumix;

public sealed class BlocCounterDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new BlocCounterDemoPageState();
    }
}

internal sealed class BlocCounterDemoPageState : State
{
    private string _milestoneMessage = "Milestone listener: waiting for count divisible by 5.";

    public override Widget Build(BuildContext context)
    {
        return new BlocProvider<BlocCounterBloc>(
            create: static _ => new BlocCounterBloc(),
            child: new BlocListener<BlocCounterBloc, BlocCounterState>(
                listenWhen: static (previous, next) =>
                    previous.Count != next.Count
                    && next.Count != 0
                    && next.Count % 5 == 0,
                listener: (_, state) =>
                {
                    SetState(() =>
                    {
                        _milestoneMessage = $"Milestone listener: count={state.Count} at {DateTime.Now:HH:mm:ss}.";
                    });
                },
                child: new Column(
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    spacing: 10,
                    children:
                    [
                        new Text("Bloc counter demo", fontSize: 20, color: Colors.Black),
                        new Text(
                            "BlocProvider + BlocBuilder + BlocListener + BlocSelector. Refresh event uses restartable transformer.",
                            fontSize: 14,
                            color: Color.Parse("#8A000000")),
                        new Text(_milestoneMessage, fontSize: 12, color: Color.Parse("#FF607D8B")),
                        new BlocSelector<BlocCounterBloc, BlocCounterState, int>(
                            selector: static state => state.Count,
                            builder: static (_, count) =>
                                new Text($"count={count}", fontSize: 18, color: Colors.DarkSlateBlue)),
                        new BlocBuilder<BlocCounterBloc, BlocCounterState>(
                            buildWhen: static (previous, next) => previous.IsLoading != next.IsLoading,
                            builder: static (_, state) => new Text(
                                state.IsLoading
                                    ? "loading=true (refresh in-flight, restartable)"
                                    : "loading=false",
                                fontSize: 12,
                                color: state.IsLoading ? Color.Parse("#FF8E24AA") : Color.Parse("#FF2E7D32"))),
                        new BlocCounterActionButtons(),
                    ])));
    }

    internal static Widget BuildActionButton(string label, Color background, Action onPressed)
    {
        return new Expanded(
            child: new TextButton(
                onPressed: onPressed,
                backgroundColor: background,
                foregroundColor: Colors.Black,
                minHeight: 38,
                padding: new Thickness(10, 8),
                borderRadius: BorderRadius.Circular(8),
                child: new Text(label, fontSize: 12, textAlign: TextAlign.Center)));
    }
}

internal sealed class BlocCounterActionButtons : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        var bloc = context.Read<BlocCounterBloc>();

        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 8,
            children:
            [
                new Row(
                    spacing: 8,
                    children:
                    [
                        BlocCounterDemoPageState.BuildActionButton(
                            label: "-1",
                            background: Color.Parse("#FFECEFF1"),
                            onPressed: () => bloc.Add(new DecrementCounterEvent())),
                        BlocCounterDemoPageState.BuildActionButton(
                            label: "+1",
                            background: Color.Parse("#FFE3F2FD"),
                            onPressed: () => bloc.Add(new IncrementCounterEvent())),
                        BlocCounterDemoPageState.BuildActionButton(
                            label: "+5",
                            background: Color.Parse("#FFE8F5E9"),
                            onPressed: () => bloc.Add(new IncrementByCounterEvent(5))),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BlocCounterDemoPageState.BuildActionButton(
                            label: "Refresh +10 (350ms)",
                            background: Color.Parse("#FFFFF3E0"),
                            onPressed: () => bloc.Add(new RefreshCounterEvent(10, 350))),
                        BlocCounterDemoPageState.BuildActionButton(
                            label: "Refresh +10 (80ms)",
                            background: Color.Parse("#FFF3E5F5"),
                            onPressed: () => bloc.Add(new RefreshCounterEvent(10, 80))),
                    ]),
                BlocCounterDemoPageState.BuildActionButton(
                    label: "Reset",
                    background: Color.Parse("#FFFFEBEE"),
                    onPressed: () => bloc.Add(new ResetCounterEvent())),
            ]);
    }
}

internal readonly record struct BlocCounterState(int Count, bool IsLoading);

internal abstract record CounterEvent;
internal sealed record IncrementCounterEvent : CounterEvent;
internal sealed record DecrementCounterEvent : CounterEvent;
internal sealed record IncrementByCounterEvent(int Delta) : CounterEvent;
internal sealed record RefreshCounterEvent(int Delta, int DelayMs) : CounterEvent;
internal sealed record ResetCounterEvent : CounterEvent;

internal sealed class BlocCounterBloc : Bloc<CounterEvent, BlocCounterState>
{
    public BlocCounterBloc() : base(new BlocCounterState(0, IsLoading: false))
    {
        On<IncrementCounterEvent>(HandleIncrement);
        On<DecrementCounterEvent>(HandleDecrement);
        On<IncrementByCounterEvent>(HandleIncrementBy);
        On<ResetCounterEvent>(HandleReset);
        On<RefreshCounterEvent>(HandleRefreshAsync, EventTransformers.Restartable<RefreshCounterEvent>());
    }

    private ValueTask HandleIncrement(IncrementCounterEvent @event, IEmitter<BlocCounterState> emitter, CancellationToken cancellationToken)
    {
        emitter.Emit(State with { Count = State.Count + 1 });
        return ValueTask.CompletedTask;
    }

    private ValueTask HandleDecrement(DecrementCounterEvent @event, IEmitter<BlocCounterState> emitter, CancellationToken cancellationToken)
    {
        emitter.Emit(State with { Count = State.Count - 1 });
        return ValueTask.CompletedTask;
    }

    private ValueTask HandleIncrementBy(IncrementByCounterEvent @event, IEmitter<BlocCounterState> emitter, CancellationToken cancellationToken)
    {
        emitter.Emit(State with { Count = State.Count + @event.Delta });
        return ValueTask.CompletedTask;
    }

    private ValueTask HandleReset(ResetCounterEvent @event, IEmitter<BlocCounterState> emitter, CancellationToken cancellationToken)
    {
        emitter.Emit(new BlocCounterState(0, IsLoading: false));
        return ValueTask.CompletedTask;
    }

    private async ValueTask HandleRefreshAsync(
        RefreshCounterEvent @event,
        IEmitter<BlocCounterState> emitter,
        CancellationToken cancellationToken)
    {
        emitter.Emit(State with { IsLoading = true });
        await Task.Delay(@event.DelayMs, cancellationToken);
        emitter.Emit(new BlocCounterState(State.Count + @event.Delta, IsLoading: false));
    }
}
