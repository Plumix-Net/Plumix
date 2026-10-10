using Plumix.Foundation;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/platform_menu_bar.dart

/// <summary>
/// Flutter's `MenuSerializableShortcut` mixin: a <see cref="ShortcutActivator"/> that can describe
/// itself to a platform menu channel. C# has no mixins, so it is an interface that
/// <see cref="SingleActivator"/> and <see cref="CharacterActivator"/> implement directly.
/// </summary>
public interface IMenuSerializableShortcut : ShortcutActivator
{
    ShortcutSerialization SerializeForMenu();
}

/// <summary>
/// Flutter's `ShortcutSerialization`: a platform-channel description of an activator.
/// </summary>
public sealed class ShortcutSerialization
{
    private const string ShortcutCharacterKey = "shortcutCharacter";
    private const string ShortcutTriggerKey = "shortcutTrigger";
    private const string ShortcutModifiersKey = "shortcutModifiers";

    private const int ShortcutModifierMeta = 1 << 0;
    private const int ShortcutModifierShift = 1 << 1;
    private const int ShortcutModifierAlt = 1 << 2;
    private const int ShortcutModifierControl = 1 << 3;

    private readonly Dictionary<string, object?> _internal;

    private ShortcutSerialization(
        LogicalKeyboardKey? trigger,
        string? character,
        bool? alt,
        bool? control,
        bool? meta,
        bool? shift,
        Dictionary<string, object?> serialized)
    {
        Trigger = trigger;
        Character = character;
        Alt = alt;
        Control = control;
        Meta = meta;
        Shift = shift;
        _internal = serialized;
    }

    /// <summary>The trigger key, set only by <see cref="Modifier"/>.</summary>
    public LogicalKeyboardKey? Trigger { get; }

    /// <summary>The literal character, set only by <see cref="Character"/>.</summary>
    public string? Character { get; }

    public bool? Alt { get; }

    public bool? Control { get; }

    public bool? Meta { get; }

    /// <summary>Always <see langword="null"/> for a character serialization: the character encodes shift.</summary>
    public bool? Shift { get; }

    /// <summary>Flutter's `ShortcutSerialization.character`.</summary>
    public static ShortcutSerialization ForCharacter(
        string character,
        bool alt = false,
        bool control = false,
        bool meta = false)
    {
        ArgumentNullException.ThrowIfNull(character);
        DebugAssertions.Assert(character.Length == 1);

        return new ShortcutSerialization(
            trigger: null,
            character: character,
            alt: alt,
            control: control,
            meta: meta,
            shift: null,
            serialized: new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                [ShortcutCharacterKey] = character,
                [ShortcutModifiersKey] = (control ? ShortcutModifierControl : 0)
                                         | (alt ? ShortcutModifierAlt : 0)
                                         | (meta ? ShortcutModifierMeta : 0)
            });
    }

    /// <summary>Flutter's `ShortcutSerialization.modifier`.</summary>
    public static ShortcutSerialization Modifier(
        LogicalKeyboardKey trigger,
        bool alt = false,
        bool control = false,
        bool meta = false,
        bool shift = false)
    {
        ArgumentNullException.ThrowIfNull(trigger);
        DebugAssertions.Assert(
            !SingleActivator.IsModifierKey(trigger),
            "Specifying a modifier key as a trigger is not allowed. Use provided boolean parameters instead.");

        return new ShortcutSerialization(
            trigger: trigger,
            character: null,
            alt: alt,
            control: control,
            meta: meta,
            shift: shift,
            serialized: new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                [ShortcutTriggerKey] = trigger.KeyId,
                [ShortcutModifiersKey] = (alt ? ShortcutModifierAlt : 0)
                                         | (control ? ShortcutModifierControl : 0)
                                         | (meta ? ShortcutModifierMeta : 0)
                                         | (shift ? ShortcutModifierShift : 0)
            });
    }

    /// <summary>Flutter's `toChannelRepresentation`; returns the backing map, as Dart does.</summary>
    public IReadOnlyDictionary<string, object?> ToChannelRepresentation() => _internal;
}

/// <summary>Manages a platform-rendered menu hierarchy.</summary>
public abstract class PlatformMenuDelegate
{
    public abstract void SetMenus(IReadOnlyList<PlatformMenuItem> topLevelMenus);

    public abstract void ClearMenus();

    public abstract bool DebugLockDelegate(BuildContext context);

    public abstract bool DebugUnlockDelegate(BuildContext context);
}

public delegate int MenuItemSerializableIdGenerator(PlatformMenuItem item);

/// <summary>The built-in delegate for the flutter/menu channel.</summary>
public class DefaultPlatformMenuDelegate : PlatformMenuDelegate
{
    private readonly Dictionary<int, PlatformMenuItem> _idMap = [];
    private int _serial;
    private BuildContext? _lockedContext;

    public DefaultPlatformMenuDelegate(MethodChannel? channel = null)
    {
        Channel = channel ?? SystemChannels.Menu;
        Channel.SetMethodCallHandler(MethodCallHandler);
    }

    public override void ClearMenus() => SetMenus([]);

    public override void SetMenus(IReadOnlyList<PlatformMenuItem> topLevelMenus)
    {
        _idMap.Clear();
        var representation = new List<Dictionary<string, object?>>();
        foreach (PlatformMenuItem item in topLevelMenus)
        {
            representation.AddRange(item.ToChannelRepresentation(this, GetId));
        }

        var windowMenu = new Dictionary<string, object?> { ["0"] = representation };
        Scheduler.RunAsync(async () =>
        {
            try
            {
                await Channel.InvokeMethod<object>("Menu.setMenus", windowMenu);
            }
            catch (Exception error)
            {
                FlutterError.ReportError(new FlutterErrorDetails(
                    exception: error,
                    stack: error.StackTrace,
                    library: "widget library",
                    context: new ErrorDescription("while setting the platform menu")));
            }
        });
    }

    public MethodChannel Channel { get; }

    private int GetId(PlatformMenuItem item)
    {
        _serial += 1;
        _idMap[_serial] = item;
        return _serial;
    }

    public override bool DebugLockDelegate(BuildContext context)
    {
        if (Constants.KDebugMode)
        {
            DebugAssertions.Assert(_lockedContext is null || ReferenceEquals(_lockedContext, context));
            _lockedContext = context;
        }

        return true;
    }

    public override bool DebugUnlockDelegate(BuildContext context)
    {
        if (Constants.KDebugMode)
        {
            DebugAssertions.Assert(_lockedContext is null || ReferenceEquals(_lockedContext, context));
            _lockedContext = null;
        }

        return true;
    }

    private Task<object?> MethodCallHandler(MethodCall call)
    {
        int id = call.Arguments switch
        {
            int value => value,
            long value => checked((int)value),
            _ => throw new InvalidCastException("A platform menu callback ID must be an integer."),
        };
        DebugAssertions.Assert(
            _idMap.ContainsKey(id),
            $"Received a menu {call.Method} for a menu item with an ID that was not recognized: {id}");
        if (!_idMap.TryGetValue(id, out PlatformMenuItem? item))
        {
            return Task.FromResult<object?>(null);
        }

        switch (call.Method)
        {
            case "Menu.selectedCallback":
                DebugAssertions.Assert(
                    item.OnSelected is null || item.OnSelectedIntent is null,
                    "Only one of PlatformMenuItem.onSelected or PlatformMenuItem.onSelectedIntent may be specified");
                item.OnSelected?.Invoke();
                if (item.OnSelectedIntent is { } intent)
                {
                    Actions.MaybeInvoke(FocusManager.Instance.PrimaryFocus!.Context!, intent);
                }

                break;
            case "Menu.opened":
                item.OnOpen?.Invoke();
                break;
            case "Menu.closed":
                item.OnClose?.Invoke();
                break;
        }

        return Task.FromResult<object?>(null);
    }
}

/// <summary>Publishes menu data to the platform and lays out its optional child.</summary>
public class PlatformMenuBar : StatefulWidget
{
    public PlatformMenuBar(IReadOnlyList<PlatformMenuItem> menus, Widget? child = null, Key? key = null) : base(key)
    {
        Menus = menus;
        Child = child;
    }

    public Widget? Child { get; }

    public IReadOnlyList<PlatformMenuItem> Menus { get; }

    public override State CreateState() => new PlatformMenuBarState();

    public override string ToStringShort() => Diagnostics.DescribeIdentity(this);

    // DiagnosticableTreeMixin replaces Widget's dense diagnostic defaults in Dart.
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
    }

    public override List<DiagnosticsNode> DebugDescribeChildren() =>
        Menus.Select(item => item.ToDiagnosticsNode()).ToList();
}

internal sealed class PlatformMenuBarState : State<PlatformMenuBar>
{
    private IReadOnlyList<PlatformMenuItem> _descendants = [];

    public override void InitState()
    {
        base.InitState();
        if (Constants.KDebugMode)
        {
            DebugAssertions.Assert(
                WidgetsBinding.Instance.PlatformMenuDelegate.DebugLockDelegate(Context),
                "More than one active PlatformMenuBar detected. Only one active "
                + "platform-rendered menu bar is allowed at a time.");
        }

        WidgetsBinding.Instance.PlatformMenuDelegate.ClearMenus();
        UpdateMenu();
    }

    public override void Dispose()
    {
        if (Constants.KDebugMode)
        {
            DebugAssertions.Assert(
                WidgetsBinding.Instance.PlatformMenuDelegate.DebugUnlockDelegate(Context),
                $"tried to unlock the DefaultPlatformMenuDelegate more than once with context {Context}.");
        }

        WidgetsBinding.Instance.PlatformMenuDelegate.ClearMenus();
        base.Dispose();
    }

    public override void DidUpdateWidget(PlatformMenuBar oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var newDescendants = new List<PlatformMenuItem>();
        foreach (PlatformMenuItem item in Widget.Menus)
        {
            newDescendants.Add(item);
            newDescendants.AddRange(item.Descendants);
        }

        if (!newDescendants.SequenceEqual(_descendants))
        {
            _descendants = newDescendants;
            UpdateMenu();
        }
    }

    private void UpdateMenu() => WidgetsBinding.Instance.PlatformMenuDelegate.SetMenus(Widget.Menus);

    public override Widget Build(BuildContext context) => Widget.Child ?? new SizedBox();
}

/// <summary>A platform submenu, whose children are data objects rather than widgets.</summary>
public class PlatformMenu : PlatformMenuItem, IDiagnosticableTree
{
    public PlatformMenu(
        string label,
        IReadOnlyList<PlatformMenuItem> menus,
        string? tooltip = null,
        Action? onOpen = null,
        Action? onClose = null) : base(label, tooltip)
    {
        OnOpen = onOpen;
        OnClose = onClose;
        Menus = menus;
    }

    public override Action? OnOpen { get; }

    public override Action? OnClose { get; }

    public IReadOnlyList<PlatformMenuItem> Menus { get; }

    public override IReadOnlyList<PlatformMenuItem> Descendants => GetDescendants(this);

    public static List<PlatformMenuItem> GetDescendants(PlatformMenu item)
    {
        var result = new List<PlatformMenuItem>();
        foreach (PlatformMenuItem child in item.Menus)
        {
            result.Add(child);
            result.AddRange(child.Descendants);
        }

        return result;
    }

    public override IEnumerable<Dictionary<string, object?>> ToChannelRepresentation(
        PlatformMenuDelegate menuDelegate,
        MenuItemSerializableIdGenerator getId) => [Serialize(this, menuDelegate, getId)];

    public static Dictionary<string, object?> Serialize(
        PlatformMenu item,
        PlatformMenuDelegate menuDelegate,
        MenuItemSerializableIdGenerator getId)
    {
        var result = new List<Dictionary<string, object?>>();
        foreach (PlatformMenuItem child in item.Menus)
        {
            result.AddRange(child.ToChannelRepresentation(menuDelegate, getId));
        }

        Dictionary<string, object?>? previousItem = null;
        result.RemoveAll(entry =>
        {
            bool divider = entry.GetValueOrDefault("isDivider") is true;
            if (divider && (previousItem is null || previousItem.GetValueOrDefault("isDivider") is true))
            {
                return true;
            }

            previousItem = entry;
            return false;
        });
        if (result.Count > 0 && result[^1].GetValueOrDefault("isDivider") is true)
        {
            result.RemoveAt(result.Count - 1);
        }

        var representation = new Dictionary<string, object?>
        {
            ["id"] = getId(item),
            ["label"] = item.Label,
            ["enabled"] = item.Menus.Count > 0,
            ["children"] = result,
        };
        if (item.Tooltip is not null)
        {
            representation["tooltip"] = item.Tooltip;
        }

        return representation;
    }

    public List<DiagnosticsNode> DebugDescribeChildren() => Menus.Select(item => item.ToDiagnosticsNode()).ToList();

    public override DiagnosticsNode ToDiagnosticsNode(string? name = null, DiagnosticsTreeStyle? style = null) =>
        new DiagnosticableTreeNode(name, this, style);

    public override string ToStringShort() => Diagnostics.DescribeIdentity(this);

    public string ToStringShallow(string joiner = ", ", DiagnosticLevel minLevel = DiagnosticLevel.Debug)
    {
        if (!Constants.KDebugMode)
        {
            return ToString();
        }

        var properties = new DiagnosticPropertiesBuilder();
        DebugFillProperties(properties);
        return ToStringShort() + joiner + string.Join(joiner,
            properties.Properties.Where(property => !property.IsFiltered(minLevel)));
    }

    public string ToStringDeep(
        string prefixLineOne = "",
        string? prefixOtherLines = null,
        DiagnosticLevel minLevel = DiagnosticLevel.Debug,
        int wrapWidth = 65) => ToDiagnosticsNode().ToStringDeep(
            prefixLineOne: prefixLineOne,
            prefixOtherLines: prefixOtherLines,
            minLevel: minLevel,
            wrapWidth: wrapWidth);

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        // Dart's DiagnosticableTreeMixin replaces PlatformMenuItem's property body.
        properties.Add(new StringProperty("label", Label));
        properties.Add(new FlagProperty("enabled", value: Menus.Count > 0, ifFalse: "DISABLED"));
    }
}

public class PlatformMenuItemGroup : PlatformMenuItem
{
    public PlatformMenuItemGroup(IReadOnlyList<PlatformMenuItem> members) : base(label: "") => Members = members;

    public override IReadOnlyList<PlatformMenuItem> Members { get; }

    public override IEnumerable<Dictionary<string, object?>> ToChannelRepresentation(
        PlatformMenuDelegate menuDelegate,
        MenuItemSerializableIdGenerator getId)
    {
        DebugAssertions.Assert(Members.Count > 0, "There must be at least one member in a PlatformMenuItemGroup");
        return Serialize(this, menuDelegate, getId);
    }

    public new static IEnumerable<Dictionary<string, object?>> Serialize(
        PlatformMenuItem group,
        PlatformMenuDelegate menuDelegate,
        MenuItemSerializableIdGenerator getId)
    {
        var result = new List<Dictionary<string, object?>> { new() { ["id"] = getId(group), ["isDivider"] = true } };
        foreach (PlatformMenuItem item in group.Members)
        {
            result.AddRange(item.ToChannelRepresentation(menuDelegate, getId));
        }

        result.Add(new Dictionary<string, object?> { ["id"] = getId(group), ["isDivider"] = true });
        return result;
    }

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new IterableProperty<PlatformMenuItem>("members", Members));
    }
}

public class PlatformMenuItem : Diagnosticable
{
    public PlatformMenuItem(
        string label,
        string? tooltip = null,
        IMenuSerializableShortcut? shortcut = null,
        Action? onSelected = null,
        Intent? onSelectedIntent = null)
    {
        DebugAssertions.Assert(
            onSelected is null || onSelectedIntent is null,
            "Only one of onSelected or onSelectedIntent may be specified");
        Label = label;
        Tooltip = tooltip;
        Shortcut = shortcut;
        OnSelected = onSelected;
        OnSelectedIntent = onSelectedIntent;
    }

    public string Label { get; }

    public string? Tooltip { get; }

    public IMenuSerializableShortcut? Shortcut { get; }

    public Action? OnSelected { get; }

    public Intent? OnSelectedIntent { get; }

    public virtual Action? OnOpen => null;

    public virtual Action? OnClose => null;

    public virtual IReadOnlyList<PlatformMenuItem> Descendants => [];

    public virtual IReadOnlyList<PlatformMenuItem> Members => [];

    public virtual IEnumerable<Dictionary<string, object?>> ToChannelRepresentation(
        PlatformMenuDelegate menuDelegate,
        MenuItemSerializableIdGenerator getId) => [Serialize(this, menuDelegate, getId)];

    public static Dictionary<string, object?> Serialize(
        PlatformMenuItem item,
        PlatformMenuDelegate menuDelegate,
        MenuItemSerializableIdGenerator getId)
    {
        var representation = new Dictionary<string, object?>
        {
            ["id"] = getId(item),
            ["label"] = item.Label,
            ["enabled"] = item.OnSelected is not null || item.OnSelectedIntent is not null,
        };
        if (item.Tooltip is not null)
        {
            representation["tooltip"] = item.Tooltip;
        }

        if (item.Shortcut is not null)
        {
            foreach ((string key, object? value) in item.Shortcut.SerializeForMenu().ToChannelRepresentation())
            {
                representation[key] = value;
            }
        }

        return representation;
    }

    public override string ToStringShort() => $"{Diagnostics.DescribeIdentity(this)}({Label})";

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new StringProperty("label", Label));
        properties.Add(new StringProperty("tooltip", Tooltip, defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DiagnosticsProperty<IMenuSerializableShortcut>(
            "shortcut", Shortcut, defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new FlagProperty("enabled", value: OnSelected is not null, ifFalse: "DISABLED"));
    }
}

public class PlatformProvidedMenuItem : PlatformMenuItem
{
    public PlatformProvidedMenuItem(PlatformProvidedMenuItemType type, bool enabled = true) : base(label: "")
    {
        Type = type;
        Enabled = enabled;
    }

    public PlatformProvidedMenuItemType Type { get; }

    public bool Enabled { get; }

    public static bool HasMenu(PlatformProvidedMenuItemType menu) =>
        PlatformDefaults.TargetPlatform == TargetPlatform.MacOS && Enum.IsDefined(menu);

    public override IEnumerable<Dictionary<string, object?>> ToChannelRepresentation(
        PlatformMenuDelegate menuDelegate,
        MenuItemSerializableIdGenerator getId)
    {
        if (Constants.KDebugMode && !HasMenu(Type))
        {
            throw new ArgumentException(
                $"Platform {Diagnostics.EnumName(PlatformDefaults.TargetPlatform)} has no platform provided menu for "
                + $"{Type}. Call PlatformProvidedMenuItem.HasMenu to determine this before instantiating one.");
        }

        return [new Dictionary<string, object?>
        {
            ["id"] = getId(this),
            ["enabled"] = Enabled,
            ["platformProvidedMenu"] = (int)Type,
        }];
    }

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new FlagProperty("enabled", value: Enabled, ifFalse: "DISABLED"));
    }
}

// Ordinals are shared with the native menu plugin.
public enum PlatformProvidedMenuItemType
{
    About,
    Quit,
    ServicesSubmenu,
    Hide,
    HideOtherApplications,
    ShowAllApplications,
    StartSpeaking,
    StopSpeaking,
    ToggleFullScreen,
    MinimizeWindow,
    ZoomWindow,
    ArrangeWindowsInFront,
}
