using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum GameAction
{
    MoveUp,
    MoveDown,
    MoveLeft,
    MoveRight,
    Sprint,
    Attack,
    Jump,
    Interact,
    Inventory,
    ToggleMap,
    Pickup,
    Pause
}

public static class KeyBindings
{
    private const string PrefKeyPrefix = "KeyBinding_";

    private static readonly Dictionary<GameAction, Key> DefaultBindings = new Dictionary<GameAction, Key>
    {
        { GameAction.MoveUp, Key.W },
        { GameAction.MoveDown, Key.S },
        { GameAction.MoveLeft, Key.A },
        { GameAction.MoveRight, Key.D },
        { GameAction.Sprint, Key.LeftShift },
        { GameAction.Attack, Key.E },
        { GameAction.Jump, Key.Space },
        { GameAction.Interact, Key.F },
        { GameAction.Inventory, Key.Tab },
        { GameAction.ToggleMap, Key.M },
        { GameAction.Pickup, Key.Q },
        { GameAction.Pause, Key.Escape }
    };

    private static readonly Dictionary<GameAction, Key> Bindings = new Dictionary<GameAction, Key>();

    static KeyBindings()
    {
        LoadAllBindings();
    }

    private static void LoadAllBindings()
    {
        Bindings.Clear();
        foreach (var pair in DefaultBindings)
        {
            int value = PlayerPrefs.GetInt(PrefKeyPrefix + pair.Key, (int)pair.Value);
            Bindings[pair.Key] = (Key)value;
        }
    }

    public static Key GetKey(GameAction action)
    {
        if (Bindings.TryGetValue(action, out Key key))
        {
            return key;
        }
        return DefaultBindings.TryGetValue(action, out key) ? key : Key.None;
    }

    public static void SetKey(GameAction action, Key newKey)
    {
        if (Bindings.TryGetValue(action, out Key currentKey) && currentKey == newKey)
            return;

        Bindings[action] = newKey;
        PlayerPrefs.SetInt(PrefKeyPrefix + action, (int)newKey);
        PlayerPrefs.Save();
        OnBindingChanged?.Invoke(action, newKey);
    }

    public static event Action<GameAction, Key> OnBindingChanged;

    public static string GetKeyDisplayName(GameAction action)
    {
        return GetKeyDisplayName(GetKey(action));
    }

    public static string GetKeyDisplayName(Key key)
    {
        switch (key)
        {
            case Key.Space: return "Espacio";
            case Key.Tab: return "Tab";
            case Key.LeftShift: return "Shift Izq";
            case Key.RightShift: return "Shift Der";
            case Key.LeftCtrl: return "Ctrl Izq";
            case Key.RightCtrl: return "Ctrl Der";
            case Key.LeftAlt: return "Alt Izq";
            case Key.RightAlt: return "Alt Der";
            case Key.Escape: return "Esc";
            case Key.Enter: return "Enter";
            case Key.Backspace: return "Retroceso";
            case Key.Delete: return "Supr";
            case Key.UpArrow: return "Flecha Arriba";
            case Key.DownArrow: return "Flecha Abajo";
            case Key.LeftArrow: return "Flecha Izq";
            case Key.RightArrow: return "Flecha Der";
            case Key.Period: return ".";
            case Key.Comma: return ",";
            default:
                string name = key.ToString();
                name = name.Replace("Arrow", " Flecha");
                name = name.Replace("Digit", "");
                if (name.StartsWith("Numpad"))
                    return "Numpad " + name.Substring(6);
                return name;
        }
    }
}
