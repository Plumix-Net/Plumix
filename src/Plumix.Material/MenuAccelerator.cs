using System.Globalization;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/menu_anchor.dart

public delegate Widget MenuAcceleratorChildBuilder(BuildContext context, string label, int index);

public sealed class MenuAcceleratorCallbackBinding : InheritedWidget
{
    public MenuAcceleratorCallbackBinding(
        Widget child,
        Action? onInvoke = null,
        bool hasSubmenu = false,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        OnInvoke = onInvoke;
        HasSubmenu = hasSubmenu;
    }

    public Widget Child { get; }

    public Action? OnInvoke { get; }

    public bool HasSubmenu { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        var oldBinding = (MenuAcceleratorCallbackBinding)oldWidget;
        return !Equals(oldBinding.OnInvoke, OnInvoke) || oldBinding.HasSubmenu != HasSubmenu;
    }

    public static MenuAcceleratorCallbackBinding? MaybeOf(BuildContext context)
    {
        return context.DependOnInherited<MenuAcceleratorCallbackBinding>();
    }

    public static MenuAcceleratorCallbackBinding Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException(
                   "MenuAcceleratorCallbackBinding.Of() requires a "
                   + "MenuAcceleratorCallbackBinding ancestor.");
    }
}

public sealed class MenuAcceleratorLabel : StatefulWidget
{
    public MenuAcceleratorLabel(
        string label,
        MenuAcceleratorChildBuilder? builder = null,
        Key? key = null) : base(key)
    {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Builder = builder ?? DefaultLabelBuilder;
    }

    public string Label { get; }

    public MenuAcceleratorChildBuilder Builder { get; }

    public string DisplayLabel => StripAcceleratorMarkers(Label);

    public bool HasAccelerator
    {
        get
        {
            int acceleratorIndex = -1;
            StripAcceleratorMarkers(Label, index => acceleratorIndex = index);
            return acceleratorIndex >= 0;
        }
    }

    public override State CreateState() => new MenuAcceleratorLabelState();

    public static Widget DefaultLabelBuilder(BuildContext context, string label, int index)
    {
        if (index < 0)
        {
            return new Text(label);
        }

        IReadOnlyList<string> characters = GetTextElements(label);
        if (index >= characters.Count)
        {
            return new Text(label);
        }

        // Flutter builds the default label as one RichText paragraph so the runs
        // share a single line layout instead of being baseline-aligned siblings.
        var children = new List<InlineSpan>(3);
        if (index > 0)
        {
            children.Add(new TextSpan(text: string.Concat(characters.Take(index))));
        }

        children.Add(new TextSpan(
            text: characters[index],
            style: new TextStyle(Decoration: Plumix.UI.TextDecoration.Underline)));

        if (index < characters.Count - 1)
        {
            children.Add(new TextSpan(text: string.Concat(characters.Skip(index + 1))));
        }

        return new MergeSemantics(Text.Rich(new TextSpan(children: children)));
    }

    public static string StripAcceleratorMarkers(string label, Action<int>? setIndex = null)
    {
        ArgumentNullException.ThrowIfNull(label);
        IReadOnlyList<string> characters = GetTextElements(label);
        var displayLabel = new System.Text.StringBuilder(label.Length);
        int quotedAmpersands = 0;
        int acceleratorIndex = -1;
        bool lastWasAmpersand = false;

        for (int index = 0; index < characters.Count; index++)
        {
            string character = characters[index];
            if (lastWasAmpersand)
            {
                lastWasAmpersand = false;
                displayLabel.Append(character);
                continue;
            }

            if (!string.Equals(character, "&", StringComparison.Ordinal))
            {
                displayLabel.Append(character);
                continue;
            }

            if (index == characters.Count - 1)
            {
                break;
            }

            lastWasAmpersand = true;
            string acceleratorCharacter = characters[index + 1];
            if (acceleratorIndex == -1
                && !string.Equals(acceleratorCharacter, "&", StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(acceleratorCharacter))
            {
                acceleratorIndex = index - quotedAmpersands;
            }

            quotedAmpersands++;
        }

        setIndex?.Invoke(acceleratorIndex);
        return displayLabel.ToString();
    }

    internal static bool PlatformSupportsAccelerators(BuildContext context)
    {
        TargetPlatform platform = Theme.Of(context).Platform;
        return platform is not (TargetPlatform.IOS or TargetPlatform.MacOS);
    }

    internal static IReadOnlyList<string> GetTextElements(string value)
    {
        var result = new List<string>();
        TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(value);
        while (enumerator.MoveNext())
        {
            result.Add(enumerator.GetTextElement());
        }

        return result;
    }
}

internal sealed class MenuAcceleratorLabelState : State
{
    private string _displayLabel = string.Empty;
    private int _acceleratorIndex = -1;
    private MenuAcceleratorCallbackBinding? _binding;
    private MenuController? _menuController;
    private bool _platformSupportsAccelerators;
    private bool _showAccelerators;

    private MenuAcceleratorLabel Current => (MenuAcceleratorLabel)StateWidget;

    public override void InitState()
    {
        UpdateDisplayLabel();
        MenuAcceleratorKeyboardRegistry.Register(this);
    }

    public override void DidChangeDependencies()
    {
        _binding = MenuAcceleratorCallbackBinding.MaybeOf(Context);
        _menuController = MenuController.MaybeOf(Context);
        _platformSupportsAccelerators = MenuAcceleratorLabel.PlatformSupportsAccelerators(Context);
        SetAcceleratorsVisible(MenuAcceleratorKeyboardRegistry.ShowAccelerators);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var oldLabel = (MenuAcceleratorLabel)oldWidget;
        if (!string.Equals(oldLabel.Label, Current.Label, StringComparison.Ordinal))
        {
            UpdateDisplayLabel();
        }
    }

    public override Widget Build(BuildContext context)
    {
        int index = _showAccelerators && _platformSupportsAccelerators ? _acceleratorIndex : -1;
        return Current.Builder(context, _displayLabel, index);
    }

    public override void Dispose()
    {
        MenuAcceleratorKeyboardRegistry.Unregister(this);
        _displayLabel = string.Empty;
        _binding = null;
        _menuController = null;
    }

    internal void SetAcceleratorsVisible(bool visible)
    {
        bool next = visible && _platformSupportsAccelerators;
        if (_showAccelerators == next)
        {
            return;
        }

        SetState(() => _showAccelerators = next);
    }

    internal bool TryInvoke(string character)
    {
        if (!_platformSupportsAccelerators
            || !_showAccelerators
            || _acceleratorIndex < 0
            || _binding?.OnInvoke is null
            || (_binding.HasSubmenu && (_menuController?.IsOpen ?? false)))
        {
            return false;
        }

        IReadOnlyList<string> characters = MenuAcceleratorLabel.GetTextElements(_displayLabel);
        if (_acceleratorIndex >= characters.Count
            || !string.Equals(
                characters[_acceleratorIndex],
                character,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        _binding.OnInvoke();
        return true;
    }

    private void UpdateDisplayLabel()
    {
        _displayLabel = MenuAcceleratorLabel.StripAcceleratorMarkers(
            Current.Label,
            index => _acceleratorIndex = index);
    }
}

internal static class MenuAcceleratorKeyboardRegistry
{
    private static readonly List<MenuAcceleratorLabelState> Entries = [];
    private static bool _showAccelerators;

    internal static bool ShowAccelerators => _showAccelerators;

    internal static void Register(MenuAcceleratorLabelState entry)
    {
        if (Entries.Contains(entry))
        {
            return;
        }

        if (Entries.Count == 0)
        {
            HardwareKeyboard.Instance.AddHandler(HandleKeyEvent);
            _showAccelerators = HardwareKeyboard.Instance.IsAltPressed;
        }

        Entries.Add(entry);
        entry.SetAcceleratorsVisible(_showAccelerators);
    }

    internal static void Unregister(MenuAcceleratorLabelState entry)
    {
        Entries.Remove(entry);
        if (Entries.Count != 0)
        {
            return;
        }

        HardwareKeyboard.Instance.RemoveHandler(HandleKeyEvent);
        _showAccelerators = false;
    }

    private static bool HandleKeyEvent(KeyEvent keyEvent)
    {
        bool altKey = IsAltKey(keyEvent.Key);
        bool showAccelerators = altKey
            ? HardwareKeyboard.Instance.IsAltPressed
            : keyEvent.IsAltPressed || HardwareKeyboard.Instance.IsAltPressed;
        if (_showAccelerators != showAccelerators)
        {
            _showAccelerators = showAccelerators;
            foreach (MenuAcceleratorLabelState entry in Entries.ToArray())
            {
                entry.SetAcceleratorsVisible(_showAccelerators);
            }
        }

        if (!keyEvent.IsDown || altKey || !_showAccelerators)
        {
            return false;
        }

        string? character = NormalizeCharacterKey(keyEvent.Key);
        if (character is null)
        {
            return false;
        }

        for (int index = Entries.Count - 1; index >= 0; index--)
        {
            if (Entries[index].TryInvoke(character))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAltKey(string key)
    {
        return key is "Alt" or "LeftAlt" or "RightAlt";
    }

    private static string? NormalizeCharacterKey(string key)
    {
        IReadOnlyList<string> characters = MenuAcceleratorLabel.GetTextElements(key);
        return characters.Count == 1
            ? characters[0].ToLowerInvariant()
            : null;
    }
}
