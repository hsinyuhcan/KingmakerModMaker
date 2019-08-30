using Kingmaker;
using Kingmaker.UI;
using Kingmaker.UI.SettingsUI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModMaker.Utility
{
    public static class HotkeyHelper
    {
        public static bool CanBeRegistered(string bindingName, BindingKeysData bindingKey, 
            KeyboardAccess.GameModesGroup gameMode = KeyboardAccess.GameModesGroup.World)
        {
            bool result = Game.Instance.Keyboard.CanBeRegistered(
                bindingName,
                bindingKey.Key,
                gameMode,
                bindingKey.IsCtrlDown,
                bindingKey.IsAltDown,
                bindingKey.IsShiftDown);

            return result;
        }

        public static string GetKeyText(BindingKeysData bindingKey)
        {
            if (bindingKey == null)
            {
                return "None";
            }
            else
            {
                return string.Concat(
                    bindingKey.IsCtrlDown ? "Ctrl+" : null,
                    bindingKey.IsAltDown ? "Alt+" : null,
                    bindingKey.IsShiftDown ? "Shift+" : null,
                    bindingKey.Key.ToString());
            }
        }

        public static bool ReadKey(out BindingKeysData bindingKey)
        {
            KeyCode keyCode = KeyCode.None;

            foreach (KeyCode keyHeld in Enum.GetValues(typeof(KeyCode)))
            {
                if (keyHeld  == KeyCode.None || keyHeld  > KeyCode.PageDown)
                    continue;

                if (Input.GetKey(keyHeld ))
                {
                    keyCode = keyHeld ;
                    break;
                }
            }

            if (keyCode != KeyCode.None)
            {
                bindingKey = new BindingKeysData()
                {
                    Key = keyCode,
                    IsCtrlDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl),
                    IsAltDown = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt),
                    IsShiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
                };
                return true;
            }
            else
            {
                bindingKey = null;
                return false;
            }
        }

        public static void RegisterKey(string bindingName, BindingKeysData bindingKey,
            KeyboardAccess.GameModesGroup gameMode = KeyboardAccess.GameModesGroup.World)
        {
            Game.Instance.Keyboard.UnregisterBinding(bindingName);

            if (bindingKey != null && bindingKey.Key != KeyCode.None)
            {
                Game.Instance.Keyboard.RegisterBinding(bindingName, bindingKey, gameMode, false);
            }
        }

        public static void UnregisterKey(string bindingName)
        {
            Game.Instance.Keyboard.UnregisterBinding(bindingName);
        }

        public static void Bind(string bindingName, Action callback)
        {
            Unbind(bindingName, callback);
            Game.Instance.Keyboard.Bind(bindingName, callback);
        }

        public static void Unbind(string bindingName, Action callback)
        {
            if (Game.Instance.Keyboard.GetFieldValue<KeyboardAccess, Dictionary<string, List<Action>>>("m_BindingCallbacks").
                TryGetValue(bindingName, out List<Action> value))
            {
                while (value.Remove(callback)) { }
            }
        }
    }
}
