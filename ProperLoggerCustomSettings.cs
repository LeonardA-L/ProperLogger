using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

//https://docs.unity3d.com/ScriptReference/SettingsProvider.html

namespace ProperLogger
{
    class ProperLoggerCustomSettingsProvider : SettingsProvider
    {
        private ConfigsProvider m_configs = null;
        private GUIStyle m_subtitleStyle = null;
        private int m_currentSelectedTab = 0;
        private string m_defaultPath = "Assets/LogCategories.asset";

        public ProperLoggerCustomSettingsProvider(string path, SettingsScope scope = SettingsScope.User)
            : base(path, scope) { }

        public override void OnGUI(string searchContext)
        {
            m_configs = m_configs ?? new EditorConfigs();
            if(m_subtitleStyle == null)
            {
                m_subtitleStyle = new GUIStyle();
                m_subtitleStyle.fontSize = 11;
                Color textColor = m_subtitleStyle.normal.textColor;
                textColor.a = 0.6f;
                m_subtitleStyle.padding.left = 10;
                m_subtitleStyle.normal.textColor = textColor;
                m_subtitleStyle.richText = true;
            }

            EditorGUIUtility.labelWidth = 280f;

            m_currentSelectedTab = GUILayout.Toolbar(m_currentSelectedTab, new string[] { "General", "Categories", "Appearance" });

            GUILayout.Space(20);

            EditorGUI.BeginChangeCheck();

            switch (m_currentSelectedTab)
            {
                case 0:
                default:
                    DisplayGeneralTab();
                    break;

                case 1:
                    DisplayCategoriesTab();
                    break;

                case 2:
                    DisplayAppearanceTab();
                    break;
            }

            if (EditorGUI.EndChangeCheck() && ProperConsoleWindow.Instance != null)
            {
                ProperConsoleWindow.Instance.Repaint();
            }

        }

        private void DisplayCategoriesTab()
        {
            // TODO all texts here were written late at night
            LogCategoriesConfig asset = m_configs.CurrentCategoriesConfig;

            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Assets containing log categories");
            var newAsset = (LogCategoriesConfig)EditorGUILayout.ObjectField(asset, typeof(LogCategoriesConfig), false);
            if (newAsset != asset)
            {
                m_configs.CurrentCategoriesConfig = newAsset;
            }
            GUILayout.EndHorizontal();

            if (asset == null)
            {
                GUILayout.Space(10);
                if(GUILayout.Button("Create Asset", GUILayout.Height(60)))
                {
                    asset = ScriptableObject.CreateInstance<LogCategoriesConfig>();

                    asset.Add("Combat");
                    asset.Add("Dialogue");
                    asset.Add("Performance");

                    AssetDatabase.CreateAsset(asset, m_defaultPath);
                    AssetDatabase.SaveAssets();
                    m_configs.CurrentCategoriesConfig = asset;
                }
            }
            else
            {
                EditorGUILayout.LabelField("Please configure categories directly in the dedicated asset.\nThis asset can be versioned along with your project", m_subtitleStyle);
                GUILayout.Space(22);

                if (GUILayout.Button("Select Categories Asset", GUILayout.Height(60)))
                {
                    Selection.activeObject = asset;
                }
                GUILayout.Space(10);
                GUILayout.Label("Display", EditorStyles.boldLabel);
                GUILayout.Space(10);

                CategoryDisplayToggle("Show Category Color in Log List", ECategoryDisplay.ColorStrip);
                CategoryDisplayToggle("Show Category Name in column in Log List", ECategoryDisplay.NameColumn);
                //CategoryDisplayToggle("Show Category Icon in Log List", ECategoryDisplay.Icon);
                EditorGUILayout.LabelField("If the log entry is in multiple categories,\nonly the name of the first category will be displayed", m_subtitleStyle);
                GUILayout.Space(12);
                CategoryDisplayToggle("Show [CategoryName] in Message", ECategoryDisplay.InMessage);
                CategoryDisplayToggle("Show categories in inspector", ECategoryDisplay.InInspector);
            }
        }

        private void CategoryDisplayToggle(string label, ECategoryDisplay flag)
        {
            bool hasFlag = m_configs.CategoryDisplay.HasFlag(flag);
            bool newFlag = EditorGUILayout.Toggle(label, hasFlag);
            if (hasFlag != newFlag)
            {
                m_configs.CategoryDisplay ^= flag;
            }
        }

        private void DisplayAppearanceTab()
        {
            GUILayout.Label("Log Entries List", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Log Entry First Line Font Size");
            m_configs.LogEntryMessageFontSize = EditorGUILayout.IntField(m_configs.LogEntryMessageFontSize);
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Log Entry Second Line Font Size");
            m_configs.LogEntryStackTraceFontSize = EditorGUILayout.IntField(m_configs.LogEntryStackTraceFontSize);
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.Label("Log Inspector", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Inspector Log Message Font Size");
            m_configs.InspectorMessageFontSize = EditorGUILayout.IntField(m_configs.InspectorMessageFontSize);
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Context Object's Name");
            Color color = EditorGUILayout.ColorField(m_configs.ObjectNameColor);
            if (color != m_configs.ObjectNameColor)
            {
                m_configs.ObjectNameColor = color;
            }
            GUILayout.EndHorizontal();
        }

        private void DisplayGeneralTab()
        {
            m_configs.InspectorOnTheRight = EditorGUILayout.Toggle("Show log inspector on the right", m_configs.InspectorOnTheRight);
            GUILayout.Space(10);
            m_configs.CopyOnSelect = EditorGUILayout.Toggle("Copy on Select", m_configs.CopyOnSelect);
            EditorGUILayout.LabelField("Selecting a log will automatically copy\nits content to your clipboard", m_subtitleStyle);
            GUILayout.Space(12);
            m_configs.ShowContextNameInsteadOfStack = EditorGUILayout.Toggle("Show name of context object in log list", m_configs.ShowContextNameInsteadOfStack);
            EditorGUILayout.LabelField("If applicable, will show the name of the\ncontext objectin the log list, instead of\nthe first line of the stack trace", m_subtitleStyle);
            GUILayout.Space(12);
        }

        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            var provider = new ProperLoggerCustomSettingsProvider("Preferences/Proper Logger", SettingsScope.User);

            // Automatically extract all keywords from the Styles.
            //provider.keywords = GetSearchKeywordsFromGUIContentProperties<Styles>();
            return provider;
        }
    }
}