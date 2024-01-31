using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;

namespace LCSoundTool.Utilities
{
    public static class InputHelper
    {
        public static Dictionary<string, Keybind> keybinds;

        public static void Initialize()
        {
            keybinds = new Dictionary<string, Keybind>();
        }

        public static bool CheckInput(string keybind)
        {
            if (keybinds.ContainsKey(keybind))
            {
                if (keybinds[keybind].shortcut.IsDown() && !keybinds[keybind].wasPressed)
                {
                    keybinds[keybind].wasPressed = true;
                }
            }
            return CheckInputResult(keybind);
        }

        private static bool CheckInputResult(string keybind)
        {
            if (keybinds.ContainsKey(keybind))
            {
                if (keybinds[keybind].shortcut.IsUp() && keybinds[keybind].wasPressed)
                {
                    keybinds[keybind].wasPressed = false;
                    keybinds[keybind].onPress.Invoke();
                    return true;
                }
            }
            return false;
        }
    }

    [System.Serializable]
    public class Keybind
    {
        public KeyboardShortcut shortcut;
        public Action onPress;
        public bool wasPressed;

        public Keybind(KeyboardShortcut shortcut, Action onPress, bool wasPressed = false)
        {
            this.shortcut = shortcut;
            this.onPress = onPress;
            this.wasPressed = wasPressed;
        }
    }
}
