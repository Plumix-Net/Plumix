// Dart parity source: flutter/packages/flutter/lib/src/widgets/routes.dart

namespace Plumix.Widgets;

public abstract partial class TransitionRoute
{
    private readonly TaskCompletionSource<object?> _transitionCompleter = new();
    private Action? _trainHoppingListenerRemover;

    /// <summary>Preserves the typed Dart result on C# route subclasses.</summary>
    protected static Task<T?> ResultAs<T>(Task<object?> future) => future.ContinueWith(
        static completed =>
        {
            object? result = completed.GetAwaiter().GetResult();
            if (result is null)
            {
                return default;
            }

            return result is T typed ? typed : throw new InvalidCastException(
                $"Route result of type {result.GetType().Name} cannot be converted to {typeof(T).Name}.");
        },
        CancellationToken.None,
        TaskContinuationOptions.ExecuteSynchronously,
        TaskScheduler.Default);

    private void UpdateSecondaryAnimation(Route? nextRoute)
    {
        Action? previousRemover = _trainHoppingListenerRemover;
        _trainHoppingListenerRemover = null;
        if (nextRoute is TransitionRoute transition
            && CanTransitionTo(transition) && transition.CanTransitionFrom(this))
        {
            Animation<double>? current = _secondaryAnimation.Parent;
            if (current is not null)
            {
                Animation<double> currentTrain = current is TrainHoppingAnimation hopping
                    ? hopping.CurrentTrain!
                    : current;
                Animation<double> nextTrain = transition.RawAnimation;
                if (currentTrain.Value == nextTrain.Value || !nextTrain.Status.IsAnimating())
                {
                    SetSecondaryAnimation(nextTrain, transition.Completed);
                }
                else
                {
                    TrainHoppingAnimation? newAnimation = null;
                    void FinishHopping()
                    {
                        Action? remover = _trainHoppingListenerRemover;
                        _trainHoppingListenerRemover = null;
                        remover?.Invoke();
                    }

                    void JumpOnAnimationEnd(AnimationStatus status)
                    {
                        if (!status.IsAnimating())
                        {
                            SetSecondaryAnimation(nextTrain, transition.Completed);
                            FinishHopping();
                        }
                    }

                    _trainHoppingListenerRemover = () =>
                    {
                        nextTrain.RemoveStatusListener(JumpOnAnimationEnd);
                        newAnimation?.Dispose();
                    };
                    nextTrain.AddStatusListener(JumpOnAnimationEnd);
                    newAnimation = new TrainHoppingAnimation(currentTrain, nextTrain, onSwitchedTrain: () =>
                    {
                        SetSecondaryAnimation(newAnimation!.CurrentTrain, transition.Completed);
                        FinishHopping();
                    });
                    SetSecondaryAnimation(newAnimation, transition.Completed);
                }
            }
            else
            {
                SetSecondaryAnimation(transition.RawAnimation, transition.Completed);
            }
        }
        else
        {
            SetSecondaryAnimation(AlwaysDismissedAnimation);
        }

        previousRemover?.Invoke();
    }

    private void SetSecondaryAnimation(Animation<double>? animation, Task<object?>? disposed = null)
    {
        _secondaryAnimation.Parent = animation;
        if (disposed is not null)
        {
            EventLoop.RunAsync(async () =>
            {
                await disposed;
                await Task.Yield();
                if (ReferenceEquals(_secondaryAnimation.Parent, animation))
                {
                    _secondaryAnimation.Parent = AlwaysDismissedAnimation;
                    if (animation is TrainHoppingAnimation hopping)
                    {
                        hopping.Dispose();
                    }
                }
            });
        }
    }
}
