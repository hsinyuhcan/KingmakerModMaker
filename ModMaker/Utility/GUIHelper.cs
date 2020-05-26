using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ModMaker.Utility.RichTextExtensions;

namespace ModMaker.Utility
{
    public static class GUIHelper
    {
        public static string FormatOn = "Y".Color(RGBA.lime) + " - {0}";
        public static string FormatOff = "N".Color(RGBA.red) + " - {0}";

        public static string GetToggleText(bool toggle, string text)
        {
            return string.Format(toggle ? FormatOn : FormatOff, text);
        }

        public static int AdjusterButton(int value, string text, int min = int.MinValue, int max = int.MaxValue)
        {
            AdjusterButton(ref value, text, min, max);
            return value;
        }

        public static void AdjusterButton(ref int value, string text, int min = int.MinValue, int max = int.MaxValue)
        {
            GUILayout.Label(text, GUILayout.ExpandWidth(false));
            if (GUILayout.Button("-", GUILayout.ExpandWidth(false)) && value > min)
                value--;
            GUILayout.Label(value.ToString(), GUILayout.ExpandWidth(false));
            if (GUILayout.Button("+", GUILayout.ExpandWidth(false)) && value < max)
                value++;
        }

        public static void Hyperlink(string url, Color normalColor, Color hoverColor, GUIStyle style)
        {
            Hyperlink(url, url, normalColor, hoverColor, style);
        }

        public static void Hyperlink(string text, string url, Color normalColor, Color hoverColor, GUIStyle style)
        {
            Color color = GUI.color;
            GUI.color = Color.clear;
            GUILayout.Label(text, style, GUILayout.ExpandWidth(false));
            Rect lastRect = GUILayoutUtility.GetLastRect();
            GUI.color = lastRect.Contains(Event.current.mousePosition) ? hoverColor : normalColor;
            if (GUI.Button(lastRect, text, style))
                Application.OpenURL(url);
            lastRect.y += lastRect.height - 2;
            lastRect.height = 1;
            GUI.DrawTexture(lastRect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = color;
        }

        public static void TextField(ref string value, GUIStyle style = null, params GUILayoutOption[] options)
        {
            value = GUILayout.TextField(value, style ?? GUI.skin.textField, options);
        }

        public static void TextField(ref string value, Action onChanged, GUIStyle style = null, params GUILayoutOption[] options)
        {
            TextField(ref value, null, onChanged, style, options);
        }

        public static void TextField(ref string value, Action onClear, Action onChanged, GUIStyle style = null, params GUILayoutOption[] options)
        {
            string old = value;
            TextField(ref value, style, options);
            if (value != old)
            {
                if (onClear != null && string.IsNullOrEmpty(value))
                    onClear();
                else
                    onChanged();
            }
        }

        public static bool ToggleButton(bool toggle, string text, GUIStyle style = null, params GUILayoutOption[] options)
        {
            ToggleButton(ref toggle, text, style, options);
            return toggle;
        }

        public static void ToggleButton(ref bool toggle, string text, GUIStyle style = null, params GUILayoutOption[] options)
        {
            if (GUILayout.Button(GetToggleText(toggle, text), style ?? GUI.skin.button, options))
                toggle = !toggle;
        }

        public static bool ToggleButton(bool toggle, string text, Action on, Action off, GUIStyle style = null, params GUILayoutOption[] options)
        {
            ToggleButton(ref toggle, text, on, off, style, options);
            return toggle;
        }

        public static void ToggleButton(ref bool toggle, string text, Action on, Action off, GUIStyle style = null, params GUILayoutOption[] options)
        {
            bool old = toggle;
            ToggleButton(ref toggle, text, style, options);
            if (toggle != old)
            {
                if (toggle)
                    on?.Invoke();
                else
                    off?.Invoke();
            }
        }

        public static void ToggleButton(ref bool toggle, string text, ref float minWidth, GUIStyle style = null, params GUILayoutOption[] options)
        {
            GUIContent content = new GUIContent(GetToggleText(toggle, text));
            style = style ?? GUI.skin.button;
            minWidth = Math.Max(minWidth, style.CalcSize(content).x);
            if (GUILayout.Button(content, style, options?.Concat(new[] { GUILayout.Width(minWidth) }).ToArray() ?? new[] { GUILayout.Width(minWidth) }))
                toggle = !toggle;
        }

        public static void ToggleButton(ref bool toggle, string text, ref float minWidth, Action on, Action off, GUIStyle style = null, params GUILayoutOption[] options)
        {
            bool old = toggle;
            ToggleButton(ref toggle, text, ref minWidth, style, options);
            if (toggle != old)
            {
                if (toggle)
                    on?.Invoke();
                else
                    off?.Invoke();
            }
        }

        public static bool ToggleTypeList(bool toggle, string text, HashSet<string> selectedTypes, HashSet<Type> allTypes, GUIStyle style = null, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal();

            ToggleButton(ref toggle, text, style, options);

            if (toggle)
            {
                using(new GUILayout.VerticalScope())
                {
                    using(new GUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Select All"))
                        {
                            foreach (Type type in allTypes)
                            {
                                selectedTypes.Add(type.FullName);
                            }
                        }
                        if (GUILayout.Button("Deselect All"))
                        {
                            selectedTypes.Clear();
                        }
                    }

                    foreach (Type type in allTypes)
                    {
                        ToggleButton(selectedTypes.Contains(type.FullName), type.Name.ToSentence(),
                            () => selectedTypes.Add(type.FullName),
                            () => selectedTypes.Remove(type.FullName),
                            style, options);
                    }
                }
            }

            GUILayout.EndHorizontal();

            return toggle;
        }

        public static void Toolbar(ref int selected, string[] texts, GUIStyle style = null, params GUILayoutOption[] options)
        {
            selected = GUILayout.Toolbar(selected, texts, style ?? GUI.skin.button, options);
        }

        public static void SelectionGrid(ref int selected, string[] texts, int xCount, GUIStyle style = null, params GUILayoutOption[] options)
        {
            selected = GUILayout.SelectionGrid(selected, texts, xCount, style ?? GUI.skin.button, options);
        }

        public static void SelectionGrid(ref int selected, string[] texts, int xCount, Action onChanged, GUIStyle style = null, params GUILayoutOption[] options)
        {
            int old = selected;
            SelectionGrid(ref selected, texts, xCount, style, options);
            if (selected != old)
            {
                onChanged?.Invoke();
            }
        }

        public static float RoundedHorizontalSlider(float value, int digits, float leftValue, float rightValue, params GUILayoutOption[] options)
        {
            if (digits < 0)
            {
                float num = (float)Math.Pow(10d, -digits);
                return (float)Math.Round(GUILayout.HorizontalSlider(value, leftValue, rightValue, options) / num, 0) * num;
            }
            else
            {
                return (float)Math.Round(GUILayout.HorizontalSlider(value, leftValue, rightValue, options), digits);
            }
        }
    }
}
