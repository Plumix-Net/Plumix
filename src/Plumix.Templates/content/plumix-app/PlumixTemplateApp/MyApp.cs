using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace PlumixTemplateApp;

// The counter app of `flutter create`, in C#. This project holds the widgets every platform shares; the
// PlumixTemplateApp.* projects are thin hosts that show MyApp in a window (desktop), an Activity (Android) or a view
// controller (iOS).

public sealed class MyApp : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        return new MaterialApp(
            title: "Plumix Demo",
            theme: new ThemeData(
                colorScheme: ColorScheme.FromSeed(seedColor: Colors.DeepPurple)),
            home: new MyHomePage(title: "Plumix Demo Home Page"));
    }
}

public sealed class MyHomePage : StatefulWidget
{
    public MyHomePage(string title)
    {
        Title = title;
    }

    public string Title { get; }

    public override State CreateState() => new MyHomePageState();
}

internal sealed class MyHomePageState : State<MyHomePage>
{
    private int _counter;

    private void IncrementCounter()
    {
        SetState(() => _counter++);
    }

    public override Widget Build(BuildContext context)
    {
        ThemeData theme = Theme.Of(context);
        return new Scaffold(
            appBar: new AppBar(
                backgroundColor: theme.ColorScheme.InversePrimary,
                title: new Text(Widget.Title)),
            body: new Center(
                child: new Column(
                    mainAxisAlignment: MainAxisAlignment.Center,
                    children:
                    [
                        new Text("You have pushed the button this many times:"),
                        new Text($"{_counter}", style: theme.TextTheme.HeadlineMedium),
                    ])),
            floatingActionButton: new FloatingActionButton(
                onPressed: IncrementCounter,
                tooltip: "Increment",
                child: new Icon(Icons.Add)));
    }
}
