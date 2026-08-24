using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// cupertino_ui/test/text_field_test.dart
// cupertino_ui/test/text_field_cursor_test.dart
// cupertino_ui/test/text_field_restoration_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoTextFieldTests : IDisposable
{
    private static readonly Size ViewSize = new(320.0, 140.0);

    public CupertinoTextFieldTests() => FocusManager.Instance.ResetForTests();

    public void Dispose() => FocusManager.Instance.ResetForTests();

    [Fact]
    public void Constructors_ExposeFlutterDefaultsAndContracts()
    {
        var field = new CupertinoTextField();

        Assert.Equal(typeof(EditableText), field.GroupId);
        Assert.NotNull(field.Decoration);
        Assert.Equal(EdgeInsetsGeometry.All(7.0), field.Padding);
        Assert.Equal(OverlayVisibilityMode.Always, field.PrefixMode);
        Assert.Equal(OverlayVisibilityMode.Always, field.SuffixMode);
        Assert.Equal(OverlayVisibilityMode.Never, field.ClearButtonMode);
        Assert.Equal(TextInputType.Text, field.KeyboardType);
        Assert.True(field.Autocorrect);
        Assert.Equal(1, field.MaxLines);
        Assert.Equal(2.0, field.CursorWidth);
        Assert.Equal(Radius.Circular(2.0), field.CursorRadius);
        Assert.True(field.CursorOpacityAnimates);
        Assert.True(field.SelectionEnabled);
        Assert.Equal(Clip.HardEdge, field.ClipBehavior);
        Assert.NotNull(field.ContextMenuBuilder);

        CupertinoTextField borderless = CupertinoTextField.Borderless();
        Assert.Null(borderless.Decoration);
        Assert.Null(borderless.Autocorrect);
        Assert.True(borderless.IsBorderless);

        Assert.Throws<ArgumentException>(() => new CupertinoTextField(obscuringCharacter: ""));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoTextField(maxLines: 0));
        Assert.Throws<ArgumentException>(() => new CupertinoTextField(maxLines: 1, minLines: 2));
        Assert.Throws<ArgumentException>(() => new CupertinoTextField(expands: true));
        Assert.Throws<ArgumentException>(() => new CupertinoTextField(obscureText: true, maxLines: 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoTextField(maxLength: 0));
        Assert.False(new CupertinoTextField(readOnly: true, obscureText: true).SelectionEnabled);
    }

    [Theory]
    [InlineData(PlatformBrightness.Light, 0xFFFFFFFFu, 0x33000000u, 0x4C3C3C43u)]
    [InlineData(PlatformBrightness.Dark, 0xFF000000u, 0x33FFFFFFu, 0x4CEBEBF5u)]
    public void Build_ResolvesDecorationPlaceholderAndEditableDefaults(
        PlatformBrightness brightness,
        uint background,
        uint border,
        uint placeholder)
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoTextField(placeholder: "Search"),
            brightness));

        harness.Pump(ViewSize);

        DecoratedBox decorated = Assert.Single(harness.FindWidgets<DecoratedBox>());
        BoxDecoration decoration = Assert.IsType<BoxDecoration>(decorated.Decoration);
        Assert.Equal(Color.FromUInt32(background), decoration.Color);
        Border resolvedBorder = Assert.IsType<Border>(decoration.Border);
        Assert.Equal(Color.FromUInt32(border), resolvedBorder.Top.Color);
        Assert.Equal(BorderRadius.Circular(5.0), decoration.BorderRadius);

        Text placeholderText = Assert.Single(harness.FindWidgets<Text>(), text => text.Data == "Search");
        Assert.Equal(Color.FromUInt32(placeholder), placeholderText.Style?.Color);
        EditableText editable = Assert.Single(harness.FindWidgets<EditableText>());
        Assert.Equal(new Thickness(0.0), editable.Padding);
        Assert.Equal(2.0, editable.CursorWidth);
        Assert.Equal(Radius.Circular(2.0), editable.CursorRadius);
        Assert.Equal(new Point(-2.0, 0.0), editable.CursorOffset);
        Assert.True(editable.PaintCursorAboveText);
        Assert.True(editable.RendererIgnoresPointer);
        Assert.Equal(brightness, editable.KeyboardAppearance);
    }

    [Fact]
    public void Attachments_RespectVisibilityAndClearButtonPrecedence()
    {
        var controller = new TextEditingController();
        int changed = 0;
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoTextField(
            controller: controller,
            placeholder: "Placeholder",
            prefix: new Text("Prefix"),
            prefixMode: OverlayVisibilityMode.Editing,
            suffix: new Text("Suffix"),
            suffixMode: OverlayVisibilityMode.NotEditing,
            clearButtonMode: OverlayVisibilityMode.Always,
            onChanged: _ => changed++)));

        harness.Pump(ViewSize);
        Assert.DoesNotContain(harness.FindWidgets<Text>(), text => text.Data == "Prefix");
        Assert.Contains(harness.FindWidgets<Text>(), text => text.Data == "Suffix");
        Assert.Empty(harness.FindWidgets<Icon>());

        controller.Text = "value";
        harness.Pump(ViewSize);
        Assert.Contains(harness.FindWidgets<Text>(), text => text.Data == "Prefix");
        Assert.DoesNotContain(harness.FindWidgets<Text>(), text => text.Data == "Suffix");
        Assert.Single(harness.FindWidgets<Icon>(), icon => icon.IconData == CupertinoIcons.ClearThickCircled);

        GestureDetector clear = Assert.Single(harness.FindWidgets<GestureDetector>(), detector =>
            detector.OnTap is not null && detector.Child is Padding);
        clear.OnTap!();
        harness.Pump(ViewSize);
        Assert.Equal(string.Empty, controller.Text);
        Assert.Equal(1, changed);
    }

    [Fact]
    public void DisabledState_UsesSystemBackgroundButRetainsCustomDecoration()
    {
        using var disabled = new CupertinoThemeTestHarness(Wrap(new CupertinoTextField(enabled: false)));
        disabled.Pump(ViewSize);
        BoxDecoration defaultDecoration = Assert.IsType<BoxDecoration>(
            Assert.Single(disabled.FindWidgets<DecoratedBox>()).Decoration);
        Assert.Equal(Color.FromUInt32(0xFFFAFAFA), defaultDecoration.Color);
        Assert.True(Assert.Single(disabled.FindWidgets<IgnorePointer>()).Ignoring);

        var custom = new BoxDecoration(Color: Color.FromUInt32(0xFF123456));
        using var overridden = new CupertinoThemeTestHarness(Wrap(new CupertinoTextField(
            enabled: false,
            decoration: custom)));
        overridden.Pump(ViewSize);
        Assert.Same(custom, Assert.Single(overridden.FindWidgets<DecoratedBox>()).Decoration);

        using var borderless = new CupertinoThemeTestHarness(Wrap(CupertinoTextField.Borderless(enabled: false)));
        borderless.Pump(ViewSize);
        Assert.Contains(borderless.FindWidgets<ColoredBox>(), box => box.Color == Color.FromUInt32(0xFFFAFAFA));
    }

    [Theory]
    [InlineData(TextDirection.Ltr, false)]
    [InlineData(TextDirection.Rtl, true)]
    public void Layout_MirrorsPrefixAndSuffixAndUsesBaselineStack(TextDirection direction, bool prefixAfterSuffix)
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoTextField(
                controller: new TextEditingController("value"),
                prefix: new Text("Prefix"),
                suffix: new Text("Suffix")),
            direction: direction));

        harness.Pump(ViewSize);

        RenderParagraph prefix = Assert.Single(FindAll<RenderParagraph>(harness.RenderView), p => p.PlainText == "Prefix");
        RenderParagraph suffix = Assert.Single(FindAll<RenderParagraph>(harness.RenderView), p => p.PlainText == "Suffix");
        Assert.Equal(prefixAfterSuffix, prefix.LocalToGlobal(default).X > suffix.LocalToGlobal(default).X);
        Assert.Single(FindAll<RenderCupertinoBaselineAlignedStack>(harness.RenderView));
    }

    [Fact]
    public void MaxLength_AppendsFormatterWithoutMutatingCallerListAndHonorsEnforcement()
    {
        var supplied = new List<TextInputFormatter> { FilteringTextInputFormatter.DigitsOnly };
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoTextField(
            inputFormatters: supplied,
            maxLength: 3,
            maxLengthEnforcement: MaxLengthEnforcement.Enforced)));

        harness.Pump(ViewSize);

        EditableText editable = Assert.Single(harness.FindWidgets<EditableText>());
        Assert.Single(supplied);
        Assert.Equal(2, editable.InputFormatters!.Count);
        var limiter = Assert.IsType<LengthLimitingTextInputFormatter>(editable.InputFormatters[1]);
        Assert.Equal(MaxLengthEnforcement.Enforced, limiter.MaxLengthEnforcement);
        TextEditingValue value = limiter.FormatEditUpdate(
            new TextEditingValue(),
            new TextEditingValue("a😀bc", TextSelection.Collapsed(5)));
        Assert.Equal("a😀b", value.Text);
    }

    [Fact]
    public void MaxLengthEnforcement_AllowsDisabledAndActiveCompositionEdits()
    {
        var oldValue = new TextEditingValue("ab", TextSelection.Collapsed(2));
        var newValue = new TextEditingValue(
            "abcd",
            TextSelection.Collapsed(4),
            new TextRange(2, 4));
        var disabled = new LengthLimitingTextInputFormatter(2, MaxLengthEnforcement.None);
        var afterComposition = new LengthLimitingTextInputFormatter(
            2,
            MaxLengthEnforcement.TruncateAfterCompositionEnds);

        Assert.Equal(newValue, disabled.FormatEditUpdate(oldValue, newValue));
        Assert.Equal(newValue, afterComposition.FormatEditUpdate(oldValue, newValue));

        TextEditingValue committed = afterComposition.FormatEditUpdate(
            oldValue,
            new TextEditingValue("abcd", TextSelection.Collapsed(4), TextRange.Empty));
        Assert.Equal("ab", committed.Text);
    }

    [Fact]
    public void ToolbarOptionsAndSelectAllOnFocus_AreForwardedToEditableText()
    {
        var controller = new TextEditingController("value", TextSelection.Collapsed(2));
        using var focusNode = new FocusNode();
        var options = new ToolbarOptions(Copy: false, Cut: false, Paste: false, SelectAll: true);
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoTextField(
            controller: controller,
            focusNode: focusNode,
            toolbarOptions: options,
            selectAllOnFocus: true)));

        harness.Pump(ViewSize);
        EditableText editable = Assert.Single(harness.FindWidgets<EditableText>());
        Assert.Same(options, editable.ToolbarOptions);
        Assert.True(editable.SelectAllOnFocus);
        Assert.True(focusNode.RequestFocus());
        harness.Pump(ViewSize);
        Assert.Equal(new TextSelection(0, controller.Text.Length), controller.Selection);
        Assert.Empty(harness.FindState<EditableText.EditableTextState>().ContextMenuButtonItems);
    }

    [Fact]
    public void SpellCheckInference_FillsOnlyMissingCupertinoDefaults()
    {
        var service = new StubSpellCheckService();
        var configuration = new SpellCheckConfiguration(spellCheckService: service);

        SpellCheckConfiguration inferred = CupertinoTextField.InferIOSSpellCheckConfiguration(configuration);

        Assert.Same(service, inferred.SpellCheckService);
        Assert.Equal(CupertinoTextField.CupertinoMisspelledTextStyle, inferred.MisspelledTextStyle);
        Assert.Equal(CupertinoTextField.KMisspelledSelectionColor, inferred.MisspelledSelectionColor);
        Assert.NotNull(inferred.SpellCheckSuggestionsToolbarBuilder);
        Assert.Same(
            SpellCheckConfiguration.Disabled,
            CupertinoTextField.InferIOSSpellCheckConfiguration(SpellCheckConfiguration.Disabled));
    }

    private static Widget Wrap(
        Widget child,
        PlatformBrightness brightness = PlatformBrightness.Light,
        TextDirection direction = TextDirection.Ltr)
    {
        return new MediaQuery(
            data: new MediaQueryData(
                PlatformBrightness: brightness,
                DevicePixelRatio: 1.0),
            child: new Localizations(
                locale: new Locale("en", "US"),
                delegates:
                [
                    DefaultWidgetsLocalizations.Delegate,
                    DefaultCupertinoLocalizations.Delegate,
                ],
                child: new Directionality(
                    direction,
                    new CupertinoTheme(
                        new CupertinoThemeData(brightness: brightness),
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
