#if !DEMO
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ProperLogger
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public class PlayerSettingsWindow : ImGuiWindow<PlayerSettingsWindow>
    {
        private GUIContent m_resetButtonContent = null;
        private GUIStyle m_resetButtonStyle = null;
        private GUILayoutOption[] m_resetButtonOptions = null;
        private GUIStyle m_subtitleStyle = null;
        private GUIStyle m_boldLabel = null;
        private ConfigsProvider m_configs = null;
        private float m_labelWidth = 280f;
        private Vector2 m_scrollPos = new Vector2();

        [SerializeField, Obfuscation(Exclude = true)]
#if !DEBUG
        [HideInInspector]
#endif
        private Texture2D m_resetIcon = null;
        protected override string WindowName => "Console Settings";

        protected override void OnWindowEnabled()
        {
            base.OnWindowEnabled();

            m_resetButtonContent = new GUIContent(m_resetIcon);
            m_resetButtonOptions = new GUILayoutOption[] { GUILayout.Height(20), GUILayout.Width(20), GUILayout.ExpandWidth(false) };
            m_configs = PlayerConfigs.Instance;
            m_scrollPos = new Vector2();

            if(ProperConsoleGameWindow.Instance != null)
            {
                ProperConsoleGameWindow.Instance.ShowCategoryFilter = false;
            }
        }

        protected override void DoGui(int windowID)
        {
            if (DisplayCloseButton())
            {
                return;
            }

            m_scrollPos = GUILayout.BeginScrollView(m_scrollPos);

            if (m_subtitleStyle == null)
            {
                m_subtitleStyle = new GUIStyle(Skin.label);
                m_subtitleStyle.fontSize = 11;
                Color textColor = GUI.skin.label.normal.textColor;
                textColor.a = 0.6f;
                m_subtitleStyle.padding.left = 10;
                m_subtitleStyle.normal.textColor = textColor;
                m_subtitleStyle.richText = true;
            }

            if (m_resetButtonStyle == null)
            {
                m_resetButtonStyle = new GUIStyle(Skin.button);
                m_resetButtonStyle.padding = new RectOffset(3, 3, 3, 3);
            }

            if(m_boldLabel == null)
            {
                m_boldLabel = new GUIStyle(Skin.label);
                m_boldLabel.fontStyle = FontStyle.Bold;
            }

            GUILayout.Space(20);

            DisplayGeneralTab();
            DisplayAppearanceTab();

            GUILayout.EndScrollView();

            if (GUI.changed)
            {
                if(ProperConsoleGameWindow.Instance != null)
                {
                    ProperConsoleGameWindow.Instance.PurgeGetLinesCache = true;
                    CommonMethods.ClearStyles(ProperConsoleGameWindow.Instance);
                }
            }
            base.DoGui(windowID);
        }
        private void CategoryDisplayToggle(string label, ECategoryDisplay flag)
        {
            bool hasFlag = m_configs.CategoryDisplay.HasFlag(flag);

            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(m_labelWidth));
            bool newFlag = GUILayout.Toggle(hasFlag, "");
            GUILayout.EndHorizontal();
            if (hasFlag != newFlag)
            {
                m_configs.CategoryDisplay ^= flag;
            }
            GUILayout.Space(10);
        }

        private void DisplayAppearanceTab()
        {
            GUILayout.Label("Log Entries List", m_boldLabel);
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Console Toolbar Button Display", GUILayout.Width(m_labelWidth));
            m_configs.DisplayIcons = GUILayout.Toolbar(m_configs.DisplayIcons, new string[] { "Name Only", "Name and Icon", "Icon Only" });
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("Log Entries List", m_boldLabel);
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Log Entry Message Font Size", GUILayout.Width(m_labelWidth));
            m_configs.LogEntryMessageFontSize = (int)GUILayout.HorizontalSlider(m_configs.LogEntryMessageFontSize, 8, 20);
            if (GUILayout.Button(m_resetButtonContent, m_resetButtonStyle, m_resetButtonOptions))
            {
                m_configs.ResetLogEntryMessageFontSize();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Log Entry Message Line Count", GUILayout.Width(m_labelWidth));
            m_configs.LogEntryMessageLineCount = (int)GUILayout.HorizontalSlider(m_configs.LogEntryMessageLineCount, 1, 5);
            if (GUILayout.Button(m_resetButtonContent, m_resetButtonStyle, m_resetButtonOptions))
            {
                m_configs.ResetLogEntryMessageLineCount();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Log Entry Stack Trace Font Size");
            m_configs.LogEntryStackTraceFontSize = (int)GUILayout.HorizontalSlider(m_configs.LogEntryStackTraceFontSize, 8, 20);
            if (GUILayout.Button(m_resetButtonContent, m_resetButtonStyle, m_resetButtonOptions))
            {
                m_configs.ResetLogEntryStackTraceFontSize();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Log Entry Stack Trace Line Count", GUILayout.Width(m_labelWidth));
            m_configs.LogEntryStackTraceLineCount = (int)GUILayout.HorizontalSlider(m_configs.LogEntryStackTraceLineCount, 0, 5);
            if (GUILayout.Button(m_resetButtonContent, m_resetButtonStyle, m_resetButtonOptions))
            {
                m_configs.ResetLogEntryStackTraceLineCount();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Show name of context object in log list", GUILayout.Width(m_labelWidth));
            m_configs.ShowContextNameInsteadOfStack = GUILayout.Toggle(m_configs.ShowContextNameInsteadOfStack, "");
            GUILayout.EndHorizontal();
            GUILayout.Label("If applicable, will show the name of the\ncontext objectin the log list, instead of\nthe first line of the stack trace", m_subtitleStyle);
            GUILayout.Space(23);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Use Special Assert and Exception Icons", GUILayout.Width(m_labelWidth));
            m_configs.ShowCustomErrorIcons = GUILayout.Toggle(m_configs.ShowCustomErrorIcons, "");
            GUILayout.EndHorizontal();
            GUILayout.Space(20);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Show full file paths", GUILayout.Width(m_labelWidth));
            m_configs.ShowPathInMessage = GUILayout.Toggle(m_configs.ShowPathInMessage, "");
            GUILayout.EndHorizontal();
            GUILayout.Label("Uncheck this to hide full path to files in\nerrors and warnings for increased readability", m_subtitleStyle);
            GUILayout.Space(20);

            GUILayout.Label("Log Inspector", m_boldLabel);
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Inspector Log Message Font Size");
            m_configs.InspectorMessageFontSize = (int)GUILayout.HorizontalSlider(m_configs.InspectorMessageFontSize, 8, 20);
            if (GUILayout.Button(m_resetButtonContent, m_resetButtonStyle, m_resetButtonOptions))
            {
                m_configs.ResetInspectorMessageFontSize();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Context Object's Name", GUILayout.Width(m_labelWidth));
            string color = GUILayout.TextField(m_configs.ObjectNameColorString);
            if (color != m_configs.ObjectNameColorString)
            {
                ColorUtility.TryParseHtmlString(color, out Color result);
                m_configs.ObjectNameColor = result;
            }
            if (GUILayout.Button(m_resetButtonContent, m_resetButtonStyle, m_resetButtonOptions))
            {
                m_configs.ResetObjectNameColor();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(15);
            GUILayout.Label("Categories", m_boldLabel);
            GUILayout.Space(10);
            CategoryDisplayToggle("Show [CategoryName] in inspector", ECategoryDisplay.InInspector);

            CategoryDisplayToggle("Show [CategoryName] in Message", ECategoryDisplay.InMessage);
            GUILayout.BeginHorizontal();
            GUILayout.Label("[CategoryName] Colorization");
            m_configs.CategoryNameColorize = GUILayout.HorizontalSlider(m_configs.CategoryNameColorize, 0, 1);
            if (GUILayout.Button(m_resetButtonContent, m_resetButtonStyle, m_resetButtonOptions))
            {
                m_configs.ResetCategoryNameColorize();
            }
            GUILayout.EndHorizontal();
            CategoryDisplayToggle("Show Category Color in Log List", ECategoryDisplay.ColorStrip);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Color Strip Width", GUILayout.Width(m_labelWidth));
            m_configs.ColorStripWidth = (int)GUILayout.HorizontalSlider(m_configs.ColorStripWidth, 3, 15);
            if (GUILayout.Button(m_resetButtonContent, m_resetButtonStyle, m_resetButtonOptions))
            {
                m_configs.ResetColorStripWidth();
            }
            GUILayout.EndHorizontal();
            CategoryDisplayToggle("Show Category Name in column in Log List", ECategoryDisplay.NameColumn);
            //CategoryDisplayToggle("Show Category Icon in Log List", ECategoryDisplay.Icon);
            GUILayout.Label("If the log entry is in multiple categories,\nonly the name of the first category will be displayed", m_subtitleStyle);
            GUILayout.Space(12);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Category Name column Colorization", GUILayout.Width(m_labelWidth));
            m_configs.CategoryNameInLogListColorize = GUILayout.HorizontalSlider(m_configs.CategoryNameInLogListColorize, 0, 1);
            if (GUILayout.Button(m_resetButtonContent, m_resetButtonStyle, m_resetButtonOptions))
            {
                m_configs.ResetCategoryNameInLogListColorize();
            }
            GUILayout.EndHorizontal();
        }

        private void DisplayGeneralTab()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Show log inspector on the right", GUILayout.Width(m_labelWidth));
            m_configs.InspectorOnTheRight = GUILayout.Toggle(m_configs.InspectorOnTheRight, "");
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Log List Category Count Limit", GUILayout.Width(m_labelWidth));
            float parseResult = 0;
            float.TryParse(GUILayout.TextField("" + m_configs.CategoryCountInLogList), out parseResult);
            m_configs.CategoryCountInLogList = (int)Mathf.Max(0, parseResult);
            GUILayout.EndHorizontal();
            GUILayout.Label("How many [CategoryName] to display\nin the console list", m_subtitleStyle);
            GUILayout.Space(12);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Time format in messages");
            int prevTimeFormat = m_configs.TimeFormat;
            m_configs.TimeFormat = GUILayout.Toolbar(m_configs.TimeFormat, new string[] { "Date", "Time.time", "Frame", "Not Displayed" });
            if (m_configs.TimeFormat != prevTimeFormat) {
                if (ProperConsoleGameWindow.Instance != null) {
                    ProperConsoleGameWindow.Instance.PurgeGetLinesCache = true;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(12);

            GUILayout.BeginHorizontal();
            GUILayout.Label(" ", GUILayout.Width(m_labelWidth));
            if (GUILayout.Button(new GUIContent("  Reset Everything to Default Values", m_resetIcon), GUILayout.Height(25)))
            {
                m_configs.ResetAll();
                if (ProperConsoleGameWindow.Instance != null)
                {
                    ProperConsoleGameWindow.Instance.PurgeGetLinesCache = true;
                    CommonMethods.ClearStyles(ProperConsoleGameWindow.Instance);
                }
            }
            GUILayout.EndHorizontal();
        }
    }
}
#endif