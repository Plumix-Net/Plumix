using Plumix.UI;
using Xunit;

namespace Plumix.Tests;

/// <summary>
/// Ports every case of the engine's
/// <c>engine/src/flutter/shell/platform/common/text_input_model_unittests.cc</c> against
/// <see cref="TextInputModel"/>.
/// </summary>
public sealed class TextInputModelTests
{
    [Fact]
    public void SetText()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SetTextWideCharacters()
    {
        var model = new TextInputModel();
        model.SetText("😄🙃🤪🧐");
        Assert.Equal("😄🙃🤪🧐", model.GetText());
    }

    [Fact]
    public void SetTextEmpty()
    {
        var model = new TextInputModel();
        model.SetText("");
        Assert.Equal("", model.GetText());
    }

    [Fact]
    public void SetTextReplaceText()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.Equal("ABCDE", model.GetText());
        model.SetText("");
        Assert.Equal("", model.GetText());
    }

    [Fact]
    public void SetTextResetsSelection()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(3)));
        Assert.Equal(new TextInputModel.Range(3), model.Selection);
        model.SetText("FGHJI");
        Assert.Equal(new TextInputModel.Range(0), model.Selection);
    }

    [Fact]
    public void SetSelectionStart()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(0)));
        Assert.Equal(new TextInputModel.Range(0), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SetSelectionComposingStart()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 0));
        Assert.True(model.SetSelection(new TextInputModel.Range(1)));
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 4), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SetSelectionMiddle()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(2)));
        Assert.Equal(new TextInputModel.Range(2), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SetSelectionComposingMiddle()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 0));
        Assert.True(model.SetSelection(new TextInputModel.Range(2)));
        Assert.Equal(new TextInputModel.Range(2), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 4), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SetSelectionEnd()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(5)));
        Assert.Equal(new TextInputModel.Range(5), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SetSelectionComposingEnd()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 0));
        Assert.True(model.SetSelection(new TextInputModel.Range(4)));
        Assert.Equal(new TextInputModel.Range(4), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 4), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SetSelectionWthExtent()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(1, 4)));
        Assert.Equal(new TextInputModel.Range(1, 4), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SetSelectionWthExtentComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 0));
        Assert.False(model.SetSelection(new TextInputModel.Range(1, 4)));
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 4), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SetSelectionReverseExtent()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(4, 1)));
        Assert.Equal(new TextInputModel.Range(4, 1), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SetSelectionReverseExtentComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 0));
        Assert.False(model.SetSelection(new TextInputModel.Range(4, 1)));
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 4), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SetSelectionOutsideString()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.False(model.SetSelection(new TextInputModel.Range(4, 6)));
        Assert.False(model.SetSelection(new TextInputModel.Range(5, 6)));
        Assert.False(model.SetSelection(new TextInputModel.Range(6)));
    }

    [Fact]
    public void SetSelectionOutsideComposingRange()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 0));
        Assert.False(model.SetSelection(new TextInputModel.Range(0)));
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.False(model.SetSelection(new TextInputModel.Range(5)));
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 4), model.ComposingRange);
    }

    [Fact]
    public void SetComposingRangeStart()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(0, 0), 0));
        Assert.Equal(new TextInputModel.Range(0), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SetComposingRangeMiddle()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(2, 2), 0));
        Assert.Equal(new TextInputModel.Range(2), model.Selection);
        Assert.Equal(new TextInputModel.Range(2), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SetComposingRangeEnd()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(5, 5), 0));
        Assert.Equal(new TextInputModel.Range(5), model.Selection);
        Assert.Equal(new TextInputModel.Range(5), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SetComposingRangeWithExtent()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 3));
        Assert.Equal(new TextInputModel.Range(4), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 4), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SetComposingRangeReverseExtent()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(4, 1), 3));
        Assert.Equal(new TextInputModel.Range(4), model.Selection);
        Assert.Equal(new TextInputModel.Range(4, 1), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SetComposingRangeOutsideString()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.False(model.SetComposingRange(new TextInputModel.Range(4, 6), 0));
        Assert.False(model.SetComposingRange(new TextInputModel.Range(5, 6), 0));
        Assert.False(model.SetComposingRange(new TextInputModel.Range(6, 6), 0));
    }

    // Composing sequence with no initial selection and no text input.
    [Fact]
    public void CommitComposingNoTextWithNoSelection()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.SetSelection(new TextInputModel.Range(0));

        // Verify no changes on BeginComposing.
        model.BeginComposing();
        Assert.Equal(new TextInputModel.Range(0), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());

        // Verify no changes on CommitComposing.
        model.CommitComposing();
        Assert.Equal(new TextInputModel.Range(0), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());

        // Verify no changes on CommitComposing.
        model.EndComposing();
        Assert.Equal(new TextInputModel.Range(0), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    // Composing sequence with an initial selection and no text input.
    [Fact]
    public void CommitComposingNoTextWithSelection()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.SetSelection(new TextInputModel.Range(1, 3));

        // Verify no changes on BeginComposing.
        model.BeginComposing();
        Assert.Equal(new TextInputModel.Range(1, 3), model.Selection);
        Assert.Equal(new TextInputModel.Range(1), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());

        // Verify no changes on CommitComposing.
        model.CommitComposing();
        Assert.Equal(new TextInputModel.Range(1, 3), model.Selection);
        Assert.Equal(new TextInputModel.Range(1), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());

        // Verify no changes on CommitComposing.
        model.EndComposing();
        Assert.Equal(new TextInputModel.Range(1, 3), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    // Composing sequence with no initial selection.
    [Fact]
    public void CommitComposingTextWithNoSelection()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.SetSelection(new TextInputModel.Range(1));

        // Verify no changes on BeginComposing.
        model.BeginComposing();
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(1), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());

        // Verify selection base, extent and composing extent increment as text is
        // entered. Verify composing base does not change.
        model.UpdateComposingText("つ");
        Assert.Equal(new TextInputModel.Range(2), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 2), model.ComposingRange);
        Assert.Equal("AつBCDE", model.GetText());
        model.UpdateComposingText("つる");
        Assert.Equal(new TextInputModel.Range(3), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 3), model.ComposingRange);
        Assert.Equal("AつるBCDE", model.GetText());

        // Verify that cursor position is set to correct offset from composing base.
        model.UpdateComposingText("鶴");
        Assert.True(model.SetSelection(new TextInputModel.Range(1)));
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 2), model.ComposingRange);
        Assert.Equal("A鶴BCDE", model.GetText());

        // Verify composing base is set to composing extent on commit.
        model.CommitComposing();
        Assert.Equal(new TextInputModel.Range(2), model.Selection);
        Assert.Equal(new TextInputModel.Range(2), model.ComposingRange);
        Assert.Equal("A鶴BCDE", model.GetText());

        // Verify that further text entry increments the selection base, extent and
        // the composing extent. Verify that composing base does not change.
        model.UpdateComposingText("が");
        Assert.Equal(new TextInputModel.Range(3), model.Selection);
        Assert.Equal(new TextInputModel.Range(2, 3), model.ComposingRange);
        Assert.Equal("A鶴がBCDE", model.GetText());

        // Verify composing base is set to composing extent on commit.
        model.CommitComposing();
        Assert.Equal(new TextInputModel.Range(3), model.Selection);
        Assert.Equal(new TextInputModel.Range(3), model.ComposingRange);
        Assert.Equal("A鶴がBCDE", model.GetText());

        // Verify no changes on EndComposing.
        model.EndComposing();
        Assert.Equal(new TextInputModel.Range(3), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("A鶴がBCDE", model.GetText());
    }

    // Composing sequence with an initial selection.
    [Fact]
    public void CommitComposingTextWithSelection()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.SetSelection(new TextInputModel.Range(1, 3));

        // Verify no changes on BeginComposing.
        model.BeginComposing();
        Assert.Equal(new TextInputModel.Range(1, 3), model.Selection);
        Assert.Equal(new TextInputModel.Range(1), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());

        // Verify selection is replaced and selection base, extent and composing
        // extent increment to the position immediately after the composing text.
        // Verify composing base does not change.
        model.UpdateComposingText("つ");
        Assert.Equal(new TextInputModel.Range(2), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 2), model.ComposingRange);
        Assert.Equal("AつDE", model.GetText());

        // Verify that further text entry increments the selection base, extent and
        // the composing extent. Verify that composing base does not change.
        model.UpdateComposingText("つる");
        Assert.Equal(new TextInputModel.Range(3), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 3), model.ComposingRange);
        Assert.Equal("AつるDE", model.GetText());

        // Verify that cursor position is set to correct offset from composing base.
        model.UpdateComposingText("鶴");
        Assert.True(model.SetSelection(new TextInputModel.Range(1)));
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 2), model.ComposingRange);
        Assert.Equal("A鶴DE", model.GetText());

        // Verify composing base is set to composing extent on commit.
        model.CommitComposing();
        Assert.Equal(new TextInputModel.Range(2), model.Selection);
        Assert.Equal(new TextInputModel.Range(2), model.ComposingRange);
        Assert.Equal("A鶴DE", model.GetText());

        // Verify that further text entry increments the selection base, extent and
        // the composing extent. Verify that composing base does not change.
        model.UpdateComposingText("が");
        Assert.Equal(new TextInputModel.Range(3), model.Selection);
        Assert.Equal(new TextInputModel.Range(2, 3), model.ComposingRange);
        Assert.Equal("A鶴がDE", model.GetText());

        // Verify composing base is set to composing extent on commit.
        model.CommitComposing();
        Assert.Equal(new TextInputModel.Range(3), model.Selection);
        Assert.Equal(new TextInputModel.Range(3), model.ComposingRange);
        Assert.Equal("A鶴がDE", model.GetText());

        // Verify no changes on EndComposing.
        model.EndComposing();
        Assert.Equal(new TextInputModel.Range(3), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("A鶴がDE", model.GetText());
    }

    [Fact]
    public void UpdateComposingRemovesLastComposingCharacter()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        model.SetComposingRange(new TextInputModel.Range(1, 2), 1);
        model.UpdateComposingText("");
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(1), model.ComposingRange);
        model.SetText("ACDE");
    }

    [Fact]
    public void UpdateSelectionWhileComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        model.SetComposingRange(new TextInputModel.Range(4, 5), 1);
        model.UpdateComposingText("ぴょんぴょん", new TextInputModel.Range(3, 6));
        Assert.Equal("ABCDぴょんぴょん", model.GetText());
        Assert.Equal(new TextInputModel.Range(7, 10), model.Selection);
        Assert.Equal(new TextInputModel.Range(4, 10), model.ComposingRange);
    }

    [Fact]
    public void AddCodePoint()
    {
        var model = new TextInputModel();
        model.AddCodePoint('A');
        model.AddCodePoint('B');
        model.AddCodePoint(0x1f604);
        model.AddCodePoint('D');
        model.AddCodePoint('E');
        Assert.Equal(new TextInputModel.Range(6), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("AB😄DE", model.GetText());
    }

    [Fact]
    public void AddCodePointSelection()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(1, 4)));
        model.AddCodePoint('x');
        Assert.Equal(new TextInputModel.Range(2), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("AxE", model.GetText());
    }

    [Fact]
    public void AddCodePointReverseSelection()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(4, 1)));
        model.AddCodePoint('x');
        Assert.Equal(new TextInputModel.Range(2), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("AxE", model.GetText());
    }

    [Fact]
    public void AddCodePointSelectionWideCharacter()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(1, 4)));
        model.AddCodePoint(0x1f604);
        Assert.Equal(new TextInputModel.Range(3), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("A😄E", model.GetText());
    }

    [Fact]
    public void AddCodePointReverseSelectionWideCharacter()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(4, 1)));
        model.AddCodePoint(0x1f604);
        Assert.Equal(new TextInputModel.Range(3), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("A😄E", model.GetText());
    }

    [Fact]
    public void AddText()
    {
        var model = new TextInputModel();
        model.AddText("ABCDE");
        model.AddText("😄");
        model.AddText("FGHIJ");
        Assert.Equal(new TextInputModel.Range(12), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE😄FGHIJ", model.GetText());
    }

    [Fact]
    public void AddTextSelection()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(1, 4)));
        model.AddText("xy");
        Assert.Equal(new TextInputModel.Range(3), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("AxyE", model.GetText());
    }

    [Fact]
    public void AddTextReverseSelection()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(4, 1)));
        model.AddText("xy");
        Assert.Equal(new TextInputModel.Range(3), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("AxyE", model.GetText());
    }

    [Fact]
    public void AddTextSelectionWideCharacter()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(1, 4)));
        model.AddText("😄🙃");
        Assert.Equal(new TextInputModel.Range(5), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("A😄🙃E", model.GetText());
    }

    [Fact]
    public void AddTextReverseSelectionWideCharacter()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(4, 1)));
        model.AddText("😄🙃");
        Assert.Equal(new TextInputModel.Range(5), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("A😄🙃E", model.GetText());
    }

    [Fact]
    public void DeleteStart()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(0)));
        Assert.True(model.Delete());
        Assert.Equal(new TextInputModel.Range(0), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("BCDE", model.GetText());
    }

    [Fact]
    public void DeleteMiddle()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(2)));
        Assert.True(model.Delete());
        Assert.Equal(new TextInputModel.Range(2), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABDE", model.GetText());
    }

    [Fact]
    public void DeleteEnd()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(5)));
        Assert.False(model.Delete());
        Assert.Equal(new TextInputModel.Range(5), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void DeleteWideCharacters()
    {
        var model = new TextInputModel();
        model.SetText("😄🙃🤪🧐");
        Assert.True(model.SetSelection(new TextInputModel.Range(4)));
        Assert.True(model.Delete());
        Assert.Equal(new TextInputModel.Range(4), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("😄🙃🧐", model.GetText());
    }

    [Fact]
    public void DeleteSelection()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(1, 4)));
        Assert.True(model.Delete());
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("AE", model.GetText());
    }

    [Fact]
    public void DeleteReverseSelection()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(4, 1)));
        Assert.True(model.Delete());
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("AE", model.GetText());
    }

    [Fact]
    public void DeleteStartComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 0));
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 4), model.ComposingRange);
        Assert.True(model.Delete());
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 3), model.ComposingRange);
        Assert.Equal("ACDE", model.GetText());
    }

    [Fact]
    public void DeleteStartReverseComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(4, 1), 0));
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(4, 1), model.ComposingRange);
        Assert.True(model.Delete());
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(3, 1), model.ComposingRange);
        Assert.Equal("ACDE", model.GetText());
    }

    [Fact]
    public void DeleteMiddleComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 1));
        Assert.True(model.Delete());
        Assert.Equal(new TextInputModel.Range(2), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 3), model.ComposingRange);
        Assert.Equal("ABDE", model.GetText());
    }

    [Fact]
    public void DeleteMiddleReverseComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(4, 1), 1));
        Assert.True(model.Delete());
        Assert.Equal(new TextInputModel.Range(2), model.Selection);
        Assert.Equal(new TextInputModel.Range(3, 1), model.ComposingRange);
        Assert.Equal("ABDE", model.GetText());
    }

    [Fact]
    public void DeleteEndComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 3));
        Assert.False(model.Delete());
        Assert.Equal(new TextInputModel.Range(4), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 4), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void DeleteEndReverseComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(4, 1), 3));
        Assert.False(model.Delete());
        Assert.Equal(new TextInputModel.Range(4), model.Selection);
        Assert.Equal(new TextInputModel.Range(4, 1), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void DeleteSurroundingAtCursor()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(2)));
        Assert.True(model.DeleteSurrounding(0, 1));
        Assert.Equal(new TextInputModel.Range(2), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABDE", model.GetText());
    }

    [Fact]
    public void DeleteSurroundingAtCursorComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 1));
        Assert.True(model.DeleteSurrounding(0, 1));
        Assert.Equal(new TextInputModel.Range(2), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 3), model.ComposingRange);
        Assert.Equal("ABDE", model.GetText());
    }

    [Fact]
    public void DeleteSurroundingAtCursorAll()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(2)));
        Assert.True(model.DeleteSurrounding(0, 3));
        Assert.Equal(new TextInputModel.Range(2), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("AB", model.GetText());
    }

    [Fact]
    public void DeleteSurroundingAtCursorAllComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 1));
        Assert.True(model.DeleteSurrounding(0, 2));
        Assert.Equal(new TextInputModel.Range(2), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 2), model.ComposingRange);
        Assert.Equal("ABE", model.GetText());
    }

    [Fact]
    public void DeleteSurroundingAtCursorGreedy()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(2)));
        Assert.True(model.DeleteSurrounding(0, 4));
        Assert.Equal(new TextInputModel.Range(2), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("AB", model.GetText());
    }

    [Fact]
    public void DeleteSurroundingAtCursorGreedyComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 1));
        Assert.True(model.DeleteSurrounding(0, 4));
        Assert.Equal(new TextInputModel.Range(2), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 2), model.ComposingRange);
        Assert.Equal("ABE", model.GetText());
    }

    [Fact]
    public void DeleteSurroundingBeforeCursor()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(2)));
        Assert.True(model.DeleteSurrounding(-1, 1));
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ACDE", model.GetText());
    }

    [Fact]
    public void DeleteSurroundingBeforeCursorComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 2));
        Assert.True(model.DeleteSurrounding(-1, 1));
        Assert.Equal(new TextInputModel.Range(2), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 3), model.ComposingRange);
        Assert.Equal("ABDE", model.GetText());
    }

    [Fact]
    public void DeleteSurroundingBeforeCursorAll()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(2)));
        Assert.True(model.DeleteSurrounding(-2, 2));
        Assert.Equal(new TextInputModel.Range(0), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("CDE", model.GetText());
    }

    [Fact]
    public void DeleteSurroundingBeforeCursorAllComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 2));
        Assert.True(model.DeleteSurrounding(-2, 2));
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 2), model.ComposingRange);
        Assert.Equal("ADE", model.GetText());
    }

    [Fact]
    public void DeleteSurroundingBeforeCursorGreedy()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(2)));
        Assert.True(model.DeleteSurrounding(-3, 3));
        Assert.Equal(new TextInputModel.Range(0), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("CDE", model.GetText());
    }

    [Fact]
    public void DeleteSurroundingBeforeCursorGreedyComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 2));
        Assert.True(model.DeleteSurrounding(-3, 3));
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 2), model.ComposingRange);
        Assert.Equal("ADE", model.GetText());
    }

    [Fact]
    public void DeleteSurroundingAfterCursor()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(2)));
        Assert.True(model.DeleteSurrounding(1, 1));
        Assert.Equal(new TextInputModel.Range(2), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCE", model.GetText());
    }

    [Fact]
    public void DeleteSurroundingAfterCursorComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 0));
        Assert.True(model.DeleteSurrounding(1, 1));
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 3), model.ComposingRange);
        Assert.Equal("ABDE", model.GetText());
    }

    [Fact]
    public void DeleteSurroundingAfterCursorAll()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(2)));
        Assert.True(model.DeleteSurrounding(1, 2));
        Assert.Equal(new TextInputModel.Range(2), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABC", model.GetText());
    }

    [Fact]
    public void DeleteSurroundingAfterCursorAllComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 0));
        Assert.True(model.DeleteSurrounding(1, 2));
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 2), model.ComposingRange);
        Assert.Equal("ABE", model.GetText());
    }

    [Fact]
    public void DeleteSurroundingAfterCursorGreedy()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(2)));
        Assert.True(model.DeleteSurrounding(1, 3));
        Assert.Equal(new TextInputModel.Range(2), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABC", model.GetText());
    }

    [Fact]
    public void DeleteSurroundingAfterCursorGreedyComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 0));
        Assert.True(model.DeleteSurrounding(1, 3));
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 2), model.ComposingRange);
        Assert.Equal("ABE", model.GetText());
    }

    [Fact]
    public void DeleteSurroundingSelection()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(2, 3)));
        Assert.True(model.DeleteSurrounding(0, 1));
        Assert.Equal(new TextInputModel.Range(3), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCE", model.GetText());
    }

    [Fact]
    public void DeleteSurroundingReverseSelection()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(4, 3)));
        Assert.True(model.DeleteSurrounding(0, 1));
        Assert.Equal(new TextInputModel.Range(3), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCE", model.GetText());
    }

    [Fact]
    public void BackspaceStart()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(0)));
        Assert.False(model.Backspace());
        Assert.Equal(new TextInputModel.Range(0), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void BackspaceMiddle()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(2)));
        Assert.True(model.Backspace());
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ACDE", model.GetText());
    }

    [Fact]
    public void BackspaceEnd()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(5)));
        Assert.True(model.Backspace());
        Assert.Equal(new TextInputModel.Range(4), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCD", model.GetText());
    }

    [Fact]
    public void BackspaceWideCharacters()
    {
        var model = new TextInputModel();
        model.SetText("😄🙃🤪🧐");
        Assert.True(model.SetSelection(new TextInputModel.Range(4)));
        Assert.True(model.Backspace());
        Assert.Equal(new TextInputModel.Range(2), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("😄🤪🧐", model.GetText());
    }

    [Fact]
    public void BackspaceSelection()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(1, 4)));
        Assert.True(model.Delete());
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("AE", model.GetText());
    }

    [Fact]
    public void BackspaceReverseSelection()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(4, 1)));
        Assert.True(model.Delete());
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("AE", model.GetText());
    }

    [Fact]
    public void BackspaceStartComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 0));
        Assert.False(model.Backspace());
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 4), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void BackspaceStartReverseComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(4, 1), 0));
        Assert.False(model.Backspace());
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(4, 1), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void BackspaceMiddleComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 1));
        Assert.True(model.Backspace());
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 3), model.ComposingRange);
        Assert.Equal("ACDE", model.GetText());
    }

    [Fact]
    public void BackspaceMiddleReverseComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(4, 1), 1));
        Assert.True(model.Backspace());
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(3, 1), model.ComposingRange);
        Assert.Equal("ACDE", model.GetText());
    }

    [Fact]
    public void BackspaceEndComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 3));
        Assert.True(model.Backspace());
        Assert.Equal(new TextInputModel.Range(3), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 3), model.ComposingRange);
        Assert.Equal("ABCE", model.GetText());
    }

    [Fact]
    public void BackspaceEndReverseComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(4, 1), 3));
        Assert.True(model.Backspace());
        Assert.Equal(new TextInputModel.Range(3), model.Selection);
        Assert.Equal(new TextInputModel.Range(3, 1), model.ComposingRange);
        Assert.Equal("ABCE", model.GetText());
    }

    [Fact]
    public void MoveCursorForwardStart()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(0)));
        Assert.True(model.MoveCursorForward());
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorForwardMiddle()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(2)));
        Assert.True(model.MoveCursorForward());
        Assert.Equal(new TextInputModel.Range(3), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorForwardEnd()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(5)));
        Assert.False(model.MoveCursorForward());
        Assert.Equal(new TextInputModel.Range(5), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorForwardWideCharacters()
    {
        var model = new TextInputModel();
        model.SetText("😄🙃🤪🧐");
        Assert.True(model.SetSelection(new TextInputModel.Range(4)));
        Assert.True(model.MoveCursorForward());
        Assert.Equal(new TextInputModel.Range(6), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("😄🙃🤪🧐", model.GetText());
    }

    [Fact]
    public void MoveCursorForwardSelection()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(1, 4)));
        Assert.True(model.MoveCursorForward());
        Assert.Equal(new TextInputModel.Range(4), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorForwardReverseSelection()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(4, 1)));
        Assert.True(model.MoveCursorForward());
        Assert.Equal(new TextInputModel.Range(4), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorForwardStartComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 0));
        Assert.True(model.MoveCursorForward());
        Assert.Equal(new TextInputModel.Range(2), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 4), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorForwardStartReverseComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(4, 1), 0));
        Assert.True(model.MoveCursorForward());
        Assert.Equal(new TextInputModel.Range(2), model.Selection);
        Assert.Equal(new TextInputModel.Range(4, 1), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorForwardMiddleComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 1));
        Assert.True(model.MoveCursorForward());
        Assert.Equal(new TextInputModel.Range(3), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 4), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorForwardMiddleReverseComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(4, 1), 1));
        Assert.True(model.MoveCursorForward());
        Assert.Equal(new TextInputModel.Range(3), model.Selection);
        Assert.Equal(new TextInputModel.Range(4, 1), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorForwardEndComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 3));
        Assert.False(model.MoveCursorForward());
        Assert.Equal(new TextInputModel.Range(4), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 4), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorForwardEndReverseComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(4, 1), 3));
        Assert.False(model.MoveCursorForward());
        Assert.Equal(new TextInputModel.Range(4), model.Selection);
        Assert.Equal(new TextInputModel.Range(4, 1), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorBackStart()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(0)));
        Assert.False(model.MoveCursorBack());
        Assert.Equal(new TextInputModel.Range(0), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorBackMiddle()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(2)));
        Assert.True(model.MoveCursorBack());
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorBackEnd()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(5)));
        Assert.True(model.MoveCursorBack());
        Assert.Equal(new TextInputModel.Range(4), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorBackWideCharacters()
    {
        var model = new TextInputModel();
        model.SetText("😄🙃🤪🧐");
        Assert.True(model.SetSelection(new TextInputModel.Range(4)));
        Assert.True(model.MoveCursorBack());
        Assert.Equal(new TextInputModel.Range(2), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("😄🙃🤪🧐", model.GetText());
    }

    [Fact]
    public void MoveCursorBackSelection()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(1, 4)));
        Assert.True(model.MoveCursorBack());
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorBackReverseSelection()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(4, 1)));
        Assert.True(model.MoveCursorBack());
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorBackStartComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 0));
        Assert.True(model.SetSelection(new TextInputModel.Range(1)));
        Assert.False(model.MoveCursorBack());
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 4), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorBackStartReverseComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(4, 1), 0));
        Assert.True(model.SetSelection(new TextInputModel.Range(1)));
        Assert.False(model.MoveCursorBack());
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(4, 1), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorBackMiddleComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 1));
        Assert.True(model.MoveCursorBack());
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 4), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorBackMiddleReverseComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(4, 1), 1));
        Assert.True(model.MoveCursorBack());
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(4, 1), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorBackEndComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 3));
        Assert.True(model.MoveCursorBack());
        Assert.Equal(new TextInputModel.Range(3), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 4), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorBackEndReverseComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(4, 1), 3));
        Assert.True(model.MoveCursorBack());
        Assert.Equal(new TextInputModel.Range(3), model.Selection);
        Assert.Equal(new TextInputModel.Range(4, 1), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorToBeginningStart()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(0)));
        Assert.False(model.MoveCursorToBeginning());
        Assert.Equal(new TextInputModel.Range(0), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SelectToBeginningStart()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(0)));
        Assert.False(model.SelectToBeginning());
        Assert.Equal(new TextInputModel.Range(0), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorToBeginningMiddle()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(2)));
        Assert.True(model.MoveCursorToBeginning());
        Assert.Equal(new TextInputModel.Range(0), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SelectToBeginningMiddle()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(2)));
        Assert.True(model.SelectToBeginning());
        Assert.Equal(new TextInputModel.Range(2, 0), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorToBeginningEnd()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(5)));
        Assert.True(model.MoveCursorToBeginning());
        Assert.Equal(new TextInputModel.Range(0), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SelectToBeginningEnd()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(5)));
        Assert.True(model.SelectToBeginning());
        Assert.Equal(new TextInputModel.Range(5, 0), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorToBeginningSelection()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(1, 4)));
        Assert.True(model.MoveCursorToBeginning());
        Assert.Equal(new TextInputModel.Range(0), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SelectToBeginningSelection()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(1, 4)));
        Assert.True(model.SelectToBeginning());
        Assert.Equal(new TextInputModel.Range(1, 0), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorToBeginningReverseSelection()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(4, 1)));
        Assert.True(model.MoveCursorToBeginning());
        Assert.Equal(new TextInputModel.Range(0), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SelectToBeginningReverseSelection()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(4, 1)));
        Assert.True(model.SelectToBeginning());
        Assert.Equal(new TextInputModel.Range(4, 0), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorToBeginningStartComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 0));
        Assert.False(model.MoveCursorToBeginning());
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 4), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SelectToBeginningStartComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 0));
        Assert.False(model.SelectToBeginning());
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 4), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorToBeginningStartReverseComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(4, 1), 0));
        Assert.False(model.MoveCursorToBeginning());
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(4, 1), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SelectToBeginningStartReverseComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(4, 1), 0));
        Assert.False(model.SelectToBeginning());
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(4, 1), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorToBeginningMiddleComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 1));
        Assert.True(model.MoveCursorToBeginning());
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 4), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SelectToBeginningMiddleComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 1));
        Assert.True(model.SelectToBeginning());
        Assert.Equal(new TextInputModel.Range(2, 1), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 4), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorToBeginningMiddleReverseComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(4, 1), 1));
        Assert.True(model.MoveCursorToBeginning());
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(4, 1), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SelectToBeginningMiddleReverseComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(4, 1), 1));
        Assert.True(model.SelectToBeginning());
        Assert.Equal(new TextInputModel.Range(2, 1), model.Selection);
        Assert.Equal(new TextInputModel.Range(4, 1), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorToBeginningEndComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 3));
        Assert.True(model.MoveCursorToBeginning());
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 4), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SelectToBeginningEndComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 3));
        Assert.True(model.MoveCursorToBeginning());
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 4), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorToBeginningEndReverseComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(4, 1), 3));
        Assert.True(model.MoveCursorToBeginning());
        Assert.Equal(new TextInputModel.Range(1), model.Selection);
        Assert.Equal(new TextInputModel.Range(4, 1), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SelectToBeginningEndReverseComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(4, 1), 3));
        Assert.True(model.SelectToBeginning());
        Assert.Equal(new TextInputModel.Range(4, 1), model.Selection);
        Assert.Equal(new TextInputModel.Range(4, 1), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorToEndStart()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(0)));
        Assert.True(model.MoveCursorToEnd());
        Assert.Equal(new TextInputModel.Range(5), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SelectToEndStart()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(0)));
        Assert.True(model.SelectToEnd());
        Assert.Equal(new TextInputModel.Range(0, 5), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorToEndMiddle()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(2)));
        Assert.True(model.MoveCursorToEnd());
        Assert.Equal(new TextInputModel.Range(5), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SelectToEndMiddle()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(2)));
        Assert.True(model.SelectToEnd());
        Assert.Equal(new TextInputModel.Range(2, 5), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorToEndEnd()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(5)));
        Assert.False(model.MoveCursorToEnd());
        Assert.Equal(new TextInputModel.Range(5), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SelectToEndEnd()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(5)));
        Assert.False(model.SelectToEnd());
        Assert.Equal(new TextInputModel.Range(5), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorToEndSelection()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(1, 4)));
        Assert.True(model.MoveCursorToEnd());
        Assert.Equal(new TextInputModel.Range(5), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SelectToEndSelection()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(1, 4)));
        Assert.True(model.SelectToEnd());
        Assert.Equal(new TextInputModel.Range(1, 5), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorToEndReverseSelection()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(4, 1)));
        Assert.True(model.MoveCursorToEnd());
        Assert.Equal(new TextInputModel.Range(5), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SelectToEndReverseSelection()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(4, 1)));
        Assert.True(model.SelectToEnd());
        Assert.Equal(new TextInputModel.Range(4, 5), model.Selection);
        Assert.Equal(new TextInputModel.Range(0), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorToEndStartComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 0));
        Assert.True(model.MoveCursorToEnd());
        Assert.Equal(new TextInputModel.Range(4), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 4), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SelectToEndStartComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 0));
        Assert.True(model.SelectToEnd());
        Assert.Equal(new TextInputModel.Range(1, 4), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 4), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorToEndStartReverseComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 0));
        Assert.True(model.MoveCursorToEnd());
        Assert.Equal(new TextInputModel.Range(4), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 4), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SelectToEndStartReverseComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 0));
        Assert.True(model.SelectToEnd());
        Assert.Equal(new TextInputModel.Range(1, 4), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 4), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorToEndMiddleComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 1));
        Assert.True(model.MoveCursorToEnd());
        Assert.Equal(new TextInputModel.Range(4), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 4), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SelectToEndMiddleComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 1));
        Assert.True(model.SelectToEnd());
        Assert.Equal(new TextInputModel.Range(2, 4), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 4), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorToEndMiddleReverseComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(4, 1), 1));
        Assert.True(model.MoveCursorToEnd());
        Assert.Equal(new TextInputModel.Range(4), model.Selection);
        Assert.Equal(new TextInputModel.Range(4, 1), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SelectToEndMiddleReverseComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(4, 1), 1));
        Assert.True(model.SelectToEnd());
        Assert.Equal(new TextInputModel.Range(2, 4), model.Selection);
        Assert.Equal(new TextInputModel.Range(4, 1), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorToEndEndComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 3));
        Assert.False(model.MoveCursorToEnd());
        Assert.Equal(new TextInputModel.Range(4), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 4), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SelectToEndEndComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(1, 4), 3));
        Assert.False(model.SelectToEnd());
        Assert.Equal(new TextInputModel.Range(4), model.Selection);
        Assert.Equal(new TextInputModel.Range(1, 4), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void MoveCursorToEndEndReverseComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(4, 1), 3));
        Assert.False(model.MoveCursorToEnd());
        Assert.Equal(new TextInputModel.Range(4), model.Selection);
        Assert.Equal(new TextInputModel.Range(4, 1), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void SelectToEndEndReverseComposing()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        model.BeginComposing();
        Assert.True(model.SetComposingRange(new TextInputModel.Range(4, 1), 3));
        Assert.False(model.SelectToEnd());
        Assert.Equal(new TextInputModel.Range(4), model.Selection);
        Assert.Equal(new TextInputModel.Range(4, 1), model.ComposingRange);
        Assert.Equal("ABCDE", model.GetText());
    }

    [Fact]
    public void GetCursorOffset()
    {
        var model = new TextInputModel();
        // These characters take 1, 2, 3 and 4 bytes in UTF-8.
        model.SetText("$¢€𐍈");
        Assert.True(model.SetSelection(new TextInputModel.Range(0)));
        Assert.Equal(0, model.GetCursorOffset());
        Assert.True(model.MoveCursorForward());
        Assert.Equal(1, model.GetCursorOffset());
        Assert.True(model.MoveCursorForward());
        Assert.Equal(3, model.GetCursorOffset());
        Assert.True(model.MoveCursorForward());
        Assert.Equal(6, model.GetCursorOffset());
        Assert.True(model.MoveCursorForward());
        Assert.Equal(10, model.GetCursorOffset());
    }

    [Fact]
    public void GetCursorOffsetSelection()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(1, 4)));
        Assert.Equal(4, model.GetCursorOffset());
    }

    [Fact]
    public void GetCursorOffsetReverseSelection()
    {
        var model = new TextInputModel();
        model.SetText("ABCDE");
        Assert.True(model.SetSelection(new TextInputModel.Range(4, 1)));
        Assert.Equal(1, model.GetCursorOffset());
    }
}
