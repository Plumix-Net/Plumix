using Plumix.UI;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/test/services/process_text_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class ProcessTextTests
{
    [Fact]
    public async Task QueryTextActionsEmitsCorrectMethodCall()
    {
        using var processText = new MockMethodCallHandler(SystemChannels.ProcessText);
        var processTextService = new DefaultProcessTextService();

        await processTextService.QueryTextActions();

        MethodCall call = Assert.Single(processText.Log);
        Assert.Equal("ProcessText.queryTextActions", call.Method);
        Assert.Null(call.Arguments);
    }

    [Fact]
    public async Task ProcessTextActionEmitsCorrectMethodCall()
    {
        using var processText = new MockMethodCallHandler(SystemChannels.ProcessText);
        var processTextService = new DefaultProcessTextService();

        const string fakeActionId = "fakeActivity.fakeAction";
        const string textToProcess = "Flutter";
        await processTextService.ProcessTextAction(fakeActionId, textToProcess, false);

        MethodCall call = Assert.Single(processText.Log);
        Assert.Equal("ProcessText.processTextAction", call.Method);
        Assert.Equal(
            new object?[] { fakeActionId, textToProcess, false },
            ((System.Collections.IEnumerable)call.Arguments!).Cast<object?>());
    }

    [Fact]
    public async Task HandlesEngineAnswersOverTheChannel()
    {
        const string action1Id = "fakeActivity.fakeAction1";
        const string action2Id = "fakeActivity.fakeAction2";

        // Fake channel that simulates responses returned from the engine.
        var fakeChannel = new MethodChannel("flutter/processtext", new StandardMethodCodec());
        using var engine = new MockMethodCallHandler(fakeChannel, call => call.Method switch
        {
            "ProcessText.queryTextActions" => new Dictionary<object, object>
            {
                [action1Id] = "Action1",
                [action2Id] = "Action2",
            },
            "ProcessText.processTextAction" => ((System.Collections.IList)call.Arguments!)[0] as string == action1Id
                ? (string)((System.Collections.IList)call.Arguments!)[1]! + "!!!"
                : null,
            _ => null,
        });
        var processTextService = new DefaultProcessTextService();
        processTextService.SetChannel(fakeChannel);

        List<ProcessTextAction> actions = await processTextService.QueryTextActions();
        Assert.Equal(2, actions.Count);
        Assert.Equal(new ProcessTextAction(action1Id, "Action1"), actions[0]);
        Assert.Equal(new ProcessTextAction(action2Id, "Action2"), actions[1]);

        const string textToProcess = "Flutter";
        string? processedText = await processTextService.ProcessTextAction(action1Id, textToProcess, false);
        Assert.Equal("Flutter!!!", processedText);

        processedText = await processTextService.ProcessTextAction(action2Id, textToProcess, false);
        Assert.Null(processedText);
    }

    [Fact]
    public async Task QueryTextActionsReturnsAnEmptyListWhenThePlatformAnswersNothingOrFails()
    {
        using var processText = new MockMethodCallHandler(SystemChannels.ProcessText);
        var processTextService = new DefaultProcessTextService();
        Assert.Empty(await processTextService.QueryTextActions());

        processText.Respond = _ => throw new PlatformException("error", "Channel failed");
        Assert.Empty(await processTextService.QueryTextActions());

        // Without any handler the optional channel answers null.
        processText.Dispose();
        Assert.Empty(await processTextService.QueryTextActions());
    }

    [Fact]
    public void ProcessTextActionComparesIdAndLabel()
    {
        Assert.Equal(new ProcessTextAction("a", "A"), new ProcessTextAction("a", "A"));
        Assert.Equal(
            new ProcessTextAction("a", "A").GetHashCode(),
            new ProcessTextAction("a", "A").GetHashCode());
        Assert.NotEqual(new ProcessTextAction("a", "A"), new ProcessTextAction("a", "B"));
        Assert.NotEqual(new ProcessTextAction("a", "A"), new ProcessTextAction("b", "A"));
        Assert.True(new ProcessTextAction("a", "A") == new ProcessTextAction("a", "A"));
    }
}
