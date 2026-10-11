using System.Collections;
using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/painting/binding.dart (subset: system-font notifications)

namespace Plumix.Painting;

/// <summary>
/// The system-font notification surface of Dart's painting binding. Image caching and codec
/// services live in the framework's image pipeline; services dispatches system messages here.
/// </summary>
public sealed class PaintingBinding
{
    public static PaintingBinding Instance { get; } = new();

    private readonly SystemFontsNotifier _systemFonts = new();

    public IListenable SystemFonts => _systemFonts;

    public Task HandleSystemMessage(object systemMessage)
    {
        var message = (IDictionary)systemMessage;
        if ((string)message["type"]! == "fontsChange")
        {
            _systemFonts.NotifyListeners();
        }

        return Task.CompletedTask;
    }

    private sealed class SystemFontsNotifier : IListenable
    {
        private readonly HashSet<Action> _callbacks = [];

        public void AddListener(Action listener) => _callbacks.Add(listener);

        public void RemoveListener(Action listener) => _callbacks.Remove(listener);

        public void NotifyListeners()
        {
            foreach (Action callback in _callbacks)
            {
                callback();
            }
        }
    }
}
