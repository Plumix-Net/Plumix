using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/async_builder_demo_page.dart (exact sample parity)

public sealed class AsyncBuilderDemoPage : StatefulWidget
{
    public override State CreateState() => new AsyncBuilderDemoPageState();
}

internal sealed class AsyncBuilderDemoPageState : State
{
    private TaskCompletionSource<string>? _futureCompletion;
    private Task<string>? _future;
    private DemoObservable<int> _stream = new();
    private int _nextStreamValue;

    public override Widget Build(BuildContext context)
    {
        return new SingleChildScrollView(
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 16,
                children:
                [
                    new Text("FutureBuilder + StreamBuilder", fontSize: 20, color: Colors.Black),
                    new Text(
                        "Restart either source, then complete it with data or an error. Snapshots retain the " +
                        "previous value while a replacement source enters waiting.",
                        fontSize: 14,
                        color: Colors.DimGray),
                    BuildFutureSection(),
                    BuildStreamSection(),
                ]));
    }

    public override void Dispose()
    {
        _stream.Dispose();
        base.Dispose();
    }

    private Widget BuildFutureSection()
    {
        return new Container(
            color: Color.Parse("#FFF4F7FA"),
            padding: new Thickness(12),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 10,
                children:
                [
                    new Text("FutureBuilder", fontSize: 16, color: Colors.Black),
                    new FutureBuilder<string>(
                        future: _future,
                        initialData: "No result yet",
                        builder: BuildFutureSnapshot),
                    new Wrap(
                        spacing: 8,
                        runSpacing: 8,
                        children:
                        [
                            BuildButton("Restart future", RestartFuture),
                            BuildButton("Complete", CompleteFuture),
                            BuildButton("Fail", FailFuture),
                        ]),
                ]));
    }

    private Widget BuildStreamSection()
    {
        return new Container(
            color: Color.Parse("#FFF4F7FA"),
            padding: new Thickness(12),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 10,
                children:
                [
                    new Text("StreamBuilder", fontSize: 16, color: Colors.Black),
                    new StreamBuilder<int>(
                        stream: _stream,
                        initialData: 0,
                        builder: BuildStreamSnapshot),
                    new Wrap(
                        spacing: 8,
                        runSpacing: 8,
                        children:
                        [
                            BuildButton("Restart stream", RestartStream),
                            BuildButton("Add value", AddStreamValue),
                            BuildButton("Add error", AddStreamError),
                            BuildButton("Close", CloseStream),
                        ]),
                ]));
    }

    private static Widget BuildFutureSnapshot(BuildContext context, AsyncSnapshot<string> snapshot)
    {
        string value = snapshot.HasError
            ? $"error: {snapshot.Error!.Message}"
            : $"data: {snapshot.Data ?? "null"}";
        return new Text($"state: {snapshot.ConnectionState} · {value}", color: Color.Parse("#FF31506F"));
    }

    private static Widget BuildStreamSnapshot(BuildContext context, AsyncSnapshot<int> snapshot)
    {
        string value = snapshot.HasError
            ? $"error: {snapshot.Error!.Message}"
            : snapshot.HasData
                ? $"data: {snapshot.Data}"
                : "data: null";
        return new Text($"state: {snapshot.ConnectionState} · {value}", color: Color.Parse("#FF31506F"));
    }

    private static Widget BuildButton(string label, Action onPressed)
    {
        return new TextButton(
            onPressed: onPressed,
            child: new Text(label),
            style: TextButton.StyleFrom(
                backgroundColor: Color.Parse("#FFDCE3ED")));
    }

    private void RestartFuture()
    {
        SetState(() =>
        {
            _futureCompletion = new TaskCompletionSource<string>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _future = _futureCompletion.Task;
        });
    }

    private void CompleteFuture()
    {
        _futureCompletion?.TrySetResult("Future completed");
    }

    private void FailFuture()
    {
        _futureCompletion?.TrySetException(new InvalidOperationException("Future failed"));
    }

    private void RestartStream()
    {
        SetState(() =>
        {
            _stream.Dispose();
            _stream = new DemoObservable<int>();
            _nextStreamValue = 0;
        });
    }

    private void AddStreamValue()
    {
        _nextStreamValue += 1;
        _stream.Add(_nextStreamValue);
    }

    private void AddStreamError()
    {
        _stream.AddError(new InvalidOperationException("Stream failed"));
    }

    private void CloseStream()
    {
        _stream.Close();
    }
}

internal sealed class DemoObservable<T> : IObservable<T>, IDisposable
{
    private readonly List<IObserver<T>> _observers = [];
    private bool _isClosed;

    public IDisposable Subscribe(IObserver<T> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        if (_isClosed)
        {
            observer.OnCompleted();
            return EmptySubscription.Instance;
        }

        _observers.Add(observer);
        return new Subscription(this, observer);
    }

    public void Add(T value)
    {
        if (_isClosed)
        {
            return;
        }

        foreach (var observer in _observers.ToArray())
        {
            observer.OnNext(value);
        }
    }

    public void AddError(Exception error)
    {
        if (_isClosed)
        {
            return;
        }

        foreach (var observer in _observers.ToArray())
        {
            observer.OnError(error);
        }
    }

    public void Close()
    {
        if (_isClosed)
        {
            return;
        }

        _isClosed = true;
        foreach (var observer in _observers.ToArray())
        {
            observer.OnCompleted();
        }

        _observers.Clear();
    }

    public void Dispose()
    {
        Close();
    }

    private sealed class Subscription(DemoObservable<T> owner, IObserver<T> observer) : IDisposable
    {
        public void Dispose()
        {
            owner._observers.Remove(observer);
        }
    }

    private sealed class EmptySubscription : IDisposable
    {
        public static EmptySubscription Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
