// Dart parity source: flutter/packages/flutter/lib/src/services/process_text.dart

using System.Collections;
using Plumix.Foundation;

namespace Plumix.UI;

/// <summary>A data structure describing text processing actions.</summary>
public sealed class ProcessTextAction : IEquatable<ProcessTextAction>
{
    /// <summary>Creates text processing actions based on those returned by the engine.</summary>
    public ProcessTextAction(string id, string label)
    {
        Id = id;
        Label = label;
    }

    /// <summary>The action unique id.</summary>
    public string Id { get; }

    /// <summary>The action localized label.</summary>
    public string Label { get; }

    public bool Equals(ProcessTextAction? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is not null && other.Id == Id && other.Label == Label;
    }

    public override bool Equals(object? obj) => Equals(obj as ProcessTextAction);

    public override int GetHashCode() => HashCode.Combine(Id, Label);

    public static bool operator ==(ProcessTextAction? left, ProcessTextAction? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(ProcessTextAction? left, ProcessTextAction? right) => !(left == right);
}

/// <summary>Determines how to interact with the text processing feature.</summary>
public interface IProcessTextService
{
    /// <summary>Returns a <see cref="Task"/> that resolves to a list of
    /// <see cref="ProcessTextAction"/>s containing all text processing actions available.</summary>
    /// <remarks>If there are no actions available, an empty list will be returned.</remarks>
    Task<List<ProcessTextAction>> QueryTextActions();

    /// <summary>Returns a <see cref="Task"/> that resolves to the text returned by the text processing
    /// action identified by <paramref name="id"/>, or <c>null</c> when it returns nothing.</summary>
    /// <remarks><paramref name="readOnly"/> tells the platform whether the text may be replaced.</remarks>
    Task<string?> ProcessTextAction(string id, string text, bool readOnly);
}

/// <summary>The service used by default for the text processing feature, over
/// <see cref="SystemChannels.ProcessText"/>.</summary>
/// <remarks>Any widget may use this service to get a list of text processing actions and send
/// requests to activate these text actions. This is currently only supported on Android.</remarks>
public class DefaultProcessTextService : IProcessTextService
{
    private MethodChannel _processTextChannel;

    /// <summary>Creates the default service to interact with the platform text processing feature
    /// via communication over the text processing <see cref="MethodChannel"/>.</summary>
    public DefaultProcessTextService()
    {
        _processTextChannel = SystemChannels.ProcessText;
    }

    /// <summary>Sets the <see cref="MethodChannel"/> used to communicate with the platform text
    /// processing feature. Debug-only, like Dart's assert-guarded setter.</summary>
    internal void SetChannel(MethodChannel newChannel)
    {
        if (Constants.KDebugMode)
        {
            _processTextChannel = newChannel;
        }
    }

    /// <inheritdoc/>
    public async Task<List<ProcessTextAction>> QueryTextActions()
    {
        IDictionary rawResults;

        try
        {
            object? raw = await _processTextChannel.InvokeMethod<object>("ProcessText.queryTextActions");
            if (raw is null)
            {
                return [];
            }

            rawResults = (IDictionary)raw;
        }
        catch (Exception)
        {
            return [];
        }

        var actions = new List<ProcessTextAction>();
        foreach (object? id in rawResults.Keys)
        {
            actions.Add(new ProcessTextAction((string)id!, (string)rawResults[id!]!));
        }

        return actions;
    }

    /// <inheritdoc/>
    public async Task<string?> ProcessTextAction(string id, string text, bool readOnly)
    {
        object? processedText = await _processTextChannel.InvokeMethod<object>(
            "ProcessText.processTextAction",
            new List<object?> { id, text, readOnly });

        return (string?)processedText;
    }
}
