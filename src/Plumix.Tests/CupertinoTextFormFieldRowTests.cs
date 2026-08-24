using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: cupertino_ui/test/text_form_field_row_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoTextFormFieldRowTests : IDisposable
{
    private static readonly Size ViewSize = new(360.0, 180.0);

    public CupertinoTextFormFieldRowTests() => FocusManager.Instance.ResetForTests();

    public void Dispose() => FocusManager.Instance.ResetForTests();

    [Fact]
    public void Constructor_ExposesFlutterDefaultsAndContracts()
    {
        var field = new CupertinoTextFormFieldRow();

        Assert.Equal(string.Empty, field.InitialValue);
        Assert.True(field.Enabled);
        Assert.Equal(AutovalidateMode.Disabled, field.AutovalidateMode);
        Assert.Null(field.Prefix);
        Assert.Null(field.Padding);
        Assert.Null(field.Controller);
        Assert.Null(field.OnChanged);

        var controller = new TextEditingController("external");
        Assert.Throws<ArgumentException>(() => new CupertinoTextFormFieldRow(
            controller: controller,
            initialValue: "duplicate"));
        Assert.Throws<ArgumentException>(() => new CupertinoTextFormFieldRow(obscuringCharacter: string.Empty));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoTextFormFieldRow(maxLines: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoTextFormFieldRow(minLines: 0));
        Assert.Throws<ArgumentException>(() => new CupertinoTextFormFieldRow(maxLines: 1, minLines: 2));
        Assert.Throws<ArgumentException>(() => new CupertinoTextFormFieldRow(expands: true));
        Assert.Throws<ArgumentException>(() => new CupertinoTextFormFieldRow(obscureText: true, maxLines: 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoTextFormFieldRow(maxLength: 0));
        controller.Dispose();
    }

    [Fact]
    public void Build_ForwardsSourceParametersToBorderlessTextField()
    {
        var controller = new TextEditingController("value");
        var focusNode = new FocusNode();
        var decoration = new BoxDecoration(Color: Color.FromUInt32(0xFF123456));
        var style = new TextStyle(FontSize: 19.0);
        var strutStyle = new StrutStyle(FontSize: 18.0);
        var toolbarOptions = new ToolbarOptions(Copy: false);
        var inputFormatters = new TextInputFormatter[] { FilteringTextInputFormatter.DigitsOnly };
        var scrollPhysics = new ScrollPhysics();
        var spellCheck = new SpellCheckConfiguration(spellCheckService: new StubSpellCheckService());
        var prefix = new Text("Prefix");
        var padding = EdgeInsetsGeometry.All(4.0);
        Action editingComplete = () => { };
        Action tapped = () => { };
        Action<string> submitted = _ => { };

        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoTextFormFieldRow(
            prefix: prefix,
            padding: padding,
            controller: controller,
            focusNode: focusNode,
            decoration: decoration,
            keyboardType: TextInputType.EmailAddress,
            textCapitalization: TextCapitalization.Words,
            textInputAction: TextInputActionType.Next,
            style: style,
            strutStyle: strutStyle,
            textDirection: TextDirection.Rtl,
            textAlign: TextAlign.Center,
            textAlignVertical: TextAlignVertical.Bottom,
            autofocus: true,
            readOnly: true,
            toolbarOptions: toolbarOptions,
            showCursor: false,
            obscuringCharacter: "#",
            autocorrect: false,
            smartDashesType: SmartDashesType.Disabled,
            smartQuotesType: SmartQuotesType.Disabled,
            enableSuggestions: false,
            maxLines: 3,
            minLines: 2,
            maxLength: 8,
            onTap: tapped,
            onEditingComplete: editingComplete,
            onFieldSubmitted: submitted,
            inputFormatters: inputFormatters,
            enabled: false,
            cursorWidth: 3.14,
            cursorHeight: 6.28,
            cursorColor: CupertinoColors.SystemPurple,
            keyboardAppearance: PlatformBrightness.Dark,
            scrollPadding: EdgeInsetsGeometry.All(12.0),
            enableInteractiveSelection: false,
            scrollPhysics: scrollPhysics,
            autofillHints: ["countryName"],
            placeholder: "Email",
            placeholderStyle: new TextStyle(FontWeight: FontWeight.Bold),
            spellCheckConfiguration: spellCheck,
            selectionHeightStyle: BoxHeightStyle.Max,
            selectionWidthStyle: BoxWidthStyle.Max)));

        harness.Pump(ViewSize);

        CupertinoFormRow row = Assert.Single(harness.FindWidgets<CupertinoFormRow>());
        Assert.Same(prefix, row.Prefix);
        Assert.Equal(padding, row.Padding);
        CupertinoTextField textField = Assert.Single(harness.FindWidgets<CupertinoTextField>());
        Assert.True(textField.IsBorderless);
        Assert.Same(controller, textField.Controller);
        Assert.Same(focusNode, textField.FocusNode);
        Assert.Same(decoration, textField.Decoration);
        Assert.Equal(TextInputType.EmailAddress, textField.KeyboardType);
        Assert.Equal(TextCapitalization.Words, textField.TextCapitalization);
        Assert.Equal(TextInputActionType.Next, textField.TextInputAction);
        Assert.Same(style, textField.Style);
        Assert.Same(strutStyle, textField.StrutStyle);
        Assert.Equal(TextDirection.Rtl, textField.TextDirection);
        Assert.Equal(TextAlign.Center, textField.TextAlign);
        Assert.Equal(TextAlignVertical.Bottom, textField.TextAlignVertical);
        Assert.True(textField.Autofocus);
        Assert.True(textField.ReadOnly);
        Assert.Same(toolbarOptions, textField.ToolbarOptions);
        Assert.False(textField.ShowCursor);
        Assert.Equal("#", textField.ObscuringCharacter);
        Assert.False(textField.Autocorrect);
        Assert.Equal(3, textField.MaxLines);
        Assert.Equal(2, textField.MinLines);
        Assert.Equal(8, textField.MaxLength);
        Assert.Same(tapped, textField.OnTap);
        Assert.Same(editingComplete, textField.OnEditingComplete);
        Assert.Same(submitted, textField.OnSubmitted);
        Assert.Same(inputFormatters, textField.InputFormatters);
        Assert.False(textField.Enabled);
        Assert.Equal(3.14, textField.CursorWidth);
        Assert.Equal(6.28, textField.CursorHeight);
        Assert.Equal(CupertinoColors.SystemPurple, textField.CursorColor);
        Assert.Equal(PlatformBrightness.Dark, textField.KeyboardAppearance);
        Assert.Equal(EdgeInsetsGeometry.All(12.0), textField.ScrollPadding);
        Assert.False(textField.EnableInteractiveSelection);
        Assert.Same(scrollPhysics, textField.ScrollPhysics);
        Assert.Equal(["countryName"], textField.AutofillHints);
        Assert.Equal("Email", textField.Placeholder);
        Assert.Same(spellCheck, textField.SpellCheckConfiguration);
        Assert.Equal(BoxHeightStyle.Max, textField.SelectionHeightStyle);
        Assert.Equal(BoxWidthStyle.Max, textField.SelectionWidthStyle);

        focusNode.Dispose();
        controller.Dispose();
    }

    [Fact]
    public void FormField_SynchronizesControllerValidationSaveAndReset()
    {
        var controller = new TextEditingController("initial");
        var formKey = new LabeledGlobalKey<FormState>("form");
        string? saved = null;
        var changes = new List<string>();
        using var harness = new CupertinoThemeTestHarness(Wrap(new Form(
            key: formKey,
            child: new CupertinoTextFormFieldRow(
                prefix: new Text("Name"),
                controller: controller,
                validator: value => string.IsNullOrEmpty(value) ? "Required" : null,
                onSaved: value => saved = value,
                onChanged: changes.Add))));

        harness.Pump(ViewSize);
        FormState formState = formKey.CurrentState!;
        var state = Assert.IsAssignableFrom<FormFieldState<string>>(Assert.Single(formState!.Fields));
        Assert.Equal("initial", state.Value);
        Assert.Contains(harness.FindWidgets<Text>(), text => text.Data == "Name");

        controller.Text = string.Empty;
        harness.Pump(ViewSize);
        Assert.Equal(string.Empty, state.Value);
        Assert.False(formState.Validate());
        harness.Pump(ViewSize);
        Assert.Contains(harness.FindWidgets<Text>(), text => text.Data == "Required");

        formState.Save();
        Assert.Equal(string.Empty, saved);
        formState.Reset();
        harness.Pump(ViewSize);
        Assert.Equal("initial", controller.Text);
        Assert.Equal("initial", state.Value);
        Assert.Contains("initial", changes);
        controller.Dispose();
    }

    [Fact]
    public void LocalController_DidChangeAndResetStaySynchronized()
    {
        var changes = new List<string>();
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoTextFormFieldRow(
            initialValue: "initial",
            onChanged: changes.Add)));
        harness.Pump(ViewSize);

        var state = harness.FindState<FormFieldState<string>>();
        CupertinoTextField textField = Assert.Single(harness.FindWidgets<CupertinoTextField>());
        TextEditingController controller = textField.Controller!;
        Assert.Equal("initial", controller.Text);

        state.DidChange("changed");
        harness.Pump(ViewSize);
        Assert.Equal("changed", controller.Text);
        Assert.Equal("changed", state.Value);

        state.Reset();
        harness.Pump(ViewSize);
        Assert.Equal("initial", controller.Text);
        Assert.Equal("initial", state.Value);
        Assert.Equal(["initial"], changes);
    }

    [Fact]
    public void UserChangeAndSubmissionCallbacksObserveSynchronizedState()
    {
        string? changed = null;
        string? submitted = null;
        Action? editingComplete = null;
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoTextFormFieldRow(
            onChanged: value => changed = value,
            onFieldSubmitted: value => submitted = value,
            onEditingComplete: () => editingComplete = static () => { })));
        harness.Pump(ViewSize);

        var state = harness.FindState<FormFieldState<string>>();
        CupertinoTextField textField = Assert.Single(harness.FindWidgets<CupertinoTextField>());
        textField.OnChanged!("Soup");
        Assert.Equal("Soup", changed);
        Assert.Equal("Soup", state.Value);
        textField.OnSubmitted!("Soup");
        textField.OnEditingComplete!();
        Assert.Equal("Soup", submitted);
        Assert.NotNull(editingComplete);
    }

    [Fact]
    public void AutovalidationShowsErrorAndHonorsEnabledState()
    {
        int enabledValidations = 0;
        using var enabled = new CupertinoThemeTestHarness(Wrap(new CupertinoTextFormFieldRow(
            initialValue: "value",
            autovalidateMode: AutovalidateMode.Always,
            validator: _ =>
            {
                enabledValidations++;
                return "Error";
            })));
        enabled.Pump(ViewSize);

        Assert.True(enabledValidations > 0);
        Assert.Contains(enabled.FindWidgets<Text>(), text => text.Data == "Error");

        int disabledValidations = 0;
        using var disabled = new CupertinoThemeTestHarness(Wrap(new CupertinoTextFormFieldRow(
            enabled: false,
            autovalidateMode: AutovalidateMode.Always,
            validator: _ =>
            {
                disabledValidations++;
                return "Hidden";
            })));
        disabled.Pump(ViewSize);

        Assert.Equal(0, disabledValidations);
        Assert.DoesNotContain(disabled.FindWidgets<Text>(), text => text.Data == "Hidden");
    }

    [Fact]
    public void ControllerChangesTriggerOnUserInteractionValidation()
    {
        var controller = new TextEditingController();
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoTextFormFieldRow(
            controller: controller,
            autovalidateMode: AutovalidateMode.OnUserInteraction,
            validator: _ => "Error")));
        harness.Pump(ViewSize);
        Assert.DoesNotContain(harness.FindWidgets<Text>(), text => text.Data == "Error");

        controller.Text = "Value";
        harness.Pump(ViewSize);

        Assert.Contains(harness.FindWidgets<Text>(), text => text.Data == "Error");
        controller.Dispose();
    }

    [Fact]
    public void ControllerOwnershipTransitionsPreserveOrAdoptTheSourceValue()
    {
        var first = new TextEditingController("first");
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoTextFormFieldRow(controller: first)));
        harness.Pump(ViewSize);

        harness.PumpWidget(Wrap(new CupertinoTextFormFieldRow(initialValue: "unused")));
        harness.Pump(ViewSize);
        CupertinoTextField local = Assert.Single(harness.FindWidgets<CupertinoTextField>());
        Assert.NotSame(first, local.Controller);
        Assert.Equal("first", local.Controller!.Text);

        var second = new TextEditingController("second");
        harness.PumpWidget(Wrap(new CupertinoTextFormFieldRow(controller: second)));
        harness.Pump(ViewSize);
        CupertinoTextField external = Assert.Single(harness.FindWidgets<CupertinoTextField>());
        Assert.Same(second, external.Controller);
        Assert.Equal("second", harness.FindState<FormFieldState<string>>().Value);

        first.Dispose();
        second.Dispose();
    }

    [Fact]
    public void InternalControllerRestoresThroughTheFormFieldBucket()
    {
        var manager = new MockRestorationManager();
        var rawData = RawRestorationData.Build();
        Dictionary<object, object?>? snapshot = null;

        using (var harness = new CupertinoThemeTestHarness(new UnmanagedRestorationScope(
                   bucket: RestorationBucket.Root(manager, rawData),
                   child: Wrap(new CupertinoTextFormFieldRow(
                       restorationId: "field",
                       initialValue: "seed")))))
        {
            harness.Pump(ViewSize);
            harness.FindState<FormFieldState<string>>().DidChange("restored");
            harness.Pump(ViewSize);
            manager.DoSerialization();
            snapshot = RestorationSerialization.CopyRestorationData(rawData);
        }

        using var restart = new CupertinoThemeTestHarness(new UnmanagedRestorationScope(
            bucket: RestorationBucket.Root(manager, snapshot),
            child: Wrap(new CupertinoTextFormFieldRow(
                restorationId: "field",
                initialValue: "seed"))));
        restart.Pump(ViewSize);

        Assert.Equal("restored", restart.FindState<FormFieldState<string>>().Value);
        Assert.Equal("restored", Assert.Single(restart.FindWidgets<CupertinoTextField>()).Controller!.Text);
    }

    [Fact]
    public void Layout_ZeroAreaDoesNotCrash()
    {
        var controller = new TextEditingController("X");
        using var harness = new CupertinoThemeTestHarness(Wrap(new SizedBox(
            width: 0.0,
            height: 0.0,
            child: new CupertinoTextFormFieldRow(controller: controller))));

        harness.Pump(ViewSize);
        controller.Selection = TextSelection.Collapsed(0);
        harness.Pump(ViewSize);

        Assert.Contains(
            FindAll<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints == BoxConstraints.Tight(default) && box.Size == default);
        controller.Dispose();
    }

    private static Widget Wrap(Widget child)
    {
        return new MediaQuery(
            data: new MediaQueryData(PlatformBrightness: PlatformBrightness.Light),
            child: new Localizations(
                locale: new Locale("en", "US"),
                delegates:
                [
                    DefaultWidgetsLocalizations.Delegate,
                    DefaultCupertinoLocalizations.Delegate,
                ],
                child: new Directionality(
                    TextDirection.Ltr,
                    new CupertinoTheme(
                        new CupertinoThemeData(brightness: PlatformBrightness.Light),
                        new Center(child: child)))));
    }

    private static IReadOnlyList<T> FindAll<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null)
        {
            return result;
        }
        if (root is T typed)
        {
            result.Add(typed);
        }
        root.VisitChildren(child => result.AddRange(FindAll<T>(child)));
        return result;
    }

    private sealed class StubSpellCheckService : ISpellCheckService
    {
        public Task<IReadOnlyList<SuggestionSpan>?> FetchSpellCheckSuggestions(Locale locale, string text)
        {
            return Task.FromResult<IReadOnlyList<SuggestionSpan>?>([]);
        }
    }
}
