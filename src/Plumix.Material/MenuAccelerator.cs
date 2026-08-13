using System.Globalization;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/menu_anchor.dart

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
        TextStyle defaultStyle = DefaultTextStyle.Of(context);
        var children = new List<InlineSpan>(3);
        if (index > 0)
        {
            children.Add(new TextSpan(
                text: string.Concat(characters.Take(index)),
                style: defaultStyle));
        }

        children.Add(new TextSpan(
            text: characters[index],
            style: defaultStyle.CopyWith(decoration: TextDecoration.Underline)));

        if (index < characters.Count - 1)
        {
            children.Add(new TextSpan(
                text: string.Concat(characters.Skip(index + 1)),
                style: defaultStyle));
        }

        return new RichText(new TextSpan(children: children));
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
    private ShortcutRegistry? _shortcutRegistry;
    private ShortcutRegistryEntry? _shortcutRegistryEntry;
    private bool _platformSupportsAccelerators;
    private bool _showAccelerators;
    private bool _listeningToKeyboard;

    private MenuAcceleratorLabel Current => (MenuAcceleratorLabel)StateWidget;

    public override void InitState()
    {
        UpdateDisplayLabel();
    }

    public override void DidChangeDependencies()
    {
        _platformSupportsAccelerators = MenuAcceleratorLabel.PlatformSupportsAccelerators(Context);
        if (!_platformSupportsAccelerators)
        {
            StopListeningToKeyboard();
            _showAccelerators = false;
            _binding = null;
            _menuController = null;
            _shortcutRegistry = null;
            UpdateAcceleratorShortcut();
            return;
        }

        if (!_listeningToKeyboard)
        {
            _showAccelerators = HardwareKeyboard.Instance.IsAltPressed;
            HardwareKeyboard.Instance.AddHandler(HandleKeyEvent);
            _listeningToKeyboard = true;
        }

        _binding = MenuAcceleratorCallbackBinding.MaybeOf(Context);
        _menuController = MenuController.MaybeOf(Context);
        _shortcutRegistry = ShortcutRegistry.MaybeOf(Context);
        UpdateAcceleratorShortcut();
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
        int index = _showAccelerators ? _acceleratorIndex : -1;
        return Current.Builder(context, _displayLabel, index);
    }

    public override void Dispose()
    {
        _shortcutRegistryEntry?.Dispose();
        _shortcutRegistryEntry = null;
        _shortcutRegistry = null;
        StopListeningToKeyboard();
        _displayLabel = string.Empty;
        _binding = null;
        _menuController = null;
    }

    private bool HandleKeyEvent(KeyEvent keyEvent)
    {
        SetState(() =>
        {
            _showAccelerators = HardwareKeyboard.Instance.IsAltPressed;
            UpdateAcceleratorShortcut();
        });
        return false;
    }

    private void StopListeningToKeyboard()
    {
        if (!_listeningToKeyboard)
        {
            return;
        }

        HardwareKeyboard.Instance.RemoveHandler(HandleKeyEvent);
        _listeningToKeyboard = false;
    }

    private void UpdateAcceleratorShortcut()
    {
        _shortcutRegistryEntry?.Dispose();
        _shortcutRegistryEntry = null;

        if (!_showAccelerators
            || _acceleratorIndex < 0
            || _binding?.OnInvoke is null
            || (_binding.HasSubmenu && (_menuController?.IsOpen ?? false)))
        {
            return;
        }

        string acceleratorCharacter = _displayLabel[_acceleratorIndex]
            .ToString()
            .ToLowerInvariant();
        _shortcutRegistryEntry = _shortcutRegistry?.AddAll(
            new Dictionary<ShortcutActivator, Intent>
            {
                [new CharacterActivator(acceleratorCharacter, alt: true)] =
                    new VoidCallbackIntent(_binding.OnInvoke),
            });
    }

    private void UpdateDisplayLabel()
    {
        _displayLabel = MenuAcceleratorLabel.StripAcceleratorMarkers(
            Current.Label,
            index => _acceleratorIndex = index);
    }
}
