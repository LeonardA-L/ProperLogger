﻿using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

//https://docs.unity3d.com/ScriptReference/SettingsProvider.html

namespace ProperLogger
{
    internal class ProperLoggerCustomSettingsProvider : SettingsProvider
    {
        internal static string s_pathToPreferences = "Preferences/Proper Logger";

        private ConfigsProvider m_configs = null;
        private GUIStyle m_subtitleStyle = null;
        private int m_currentSelectedTab = 0;
        private string m_defaultPath = "Assets/LogCategories.asset";

        private GUIContent m_resetButtonContent = new GUIContent("R");

        private static ProperLoggerCustomSettingsProvider m_instance = null;
        internal static ProperLoggerCustomSettingsProvider Instance => m_instance;

        internal ProperLoggerCustomSettingsProvider(string path, SettingsScope scope = SettingsScope.User)
            : base(path, scope)
        {
            m_instance = this;
        }

        internal void SetCurrentSelectedTab(int idx)
        {
            m_currentSelectedTab = idx;
        }

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
            GUILayout.Space(10);
        }

        private void DisplayAppearanceTab()
        {
            GUILayout.Label("Log Entries List", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Log Entry Message Font Size");
            m_configs.LogEntryMessageFontSize = EditorGUILayout.IntSlider(m_configs.LogEntryMessageFontSize, 8, 20);
            if (GUILayout.Button(m_resetButtonContent, GUILayout.ExpandWidth(false)))
            {
                m_configs.ResetLogEntryMessageFontSize();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Log Entry Message Line Count");
            m_configs.LogEntryMessageLineCount = EditorGUILayout.IntSlider(m_configs.LogEntryMessageLineCount, 1, 5);
            if (GUILayout.Button(m_resetButtonContent, GUILayout.ExpandWidth(false)))
            {
                m_configs.ResetLogEntryMessageLineCount();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Log Entry Stack Trace Font Size");
            m_configs.LogEntryStackTraceFontSize = EditorGUILayout.IntSlider(m_configs.LogEntryStackTraceFontSize, 8, 20);
            if (GUILayout.Button(m_resetButtonContent, GUILayout.ExpandWidth(false)))
            {
                m_configs.ResetLogEntryStackTraceFontSize();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Log Entry Stack Trace Line Count");
            m_configs.LogEntryStackTraceLineCount = EditorGUILayout.IntSlider(m_configs.LogEntryStackTraceLineCount, 0, 5);
            if (GUILayout.Button(m_resetButtonContent, GUILayout.ExpandWidth(false)))
            {
                m_configs.ResetLogEntryStackTraceLineCount();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            m_configs.ShowContextNameInsteadOfStack = EditorGUILayout.Toggle("Show name of context object in log list", m_configs.ShowContextNameInsteadOfStack);
            GUILayout.EndHorizontal();
            EditorGUILayout.LabelField("If applicable, will show the name of the\ncontext objectin the log list, instead of\nthe first line of the stack trace", m_subtitleStyle);
            GUILayout.Space(23);

            GUILayout.BeginHorizontal();
            m_configs.ShowCustomErrorIcons = EditorGUILayout.Toggle("Use Special Assert and Exception Icons", m_configs.ShowCustomErrorIcons);
            GUILayout.EndHorizontal();
            GUILayout.Space(20);

            GUILayout.Label("Log Inspector", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Inspector Log Message Font Size");
            m_configs.InspectorMessageFontSize = EditorGUILayout.IntField(m_configs.InspectorMessageFontSize);
            if (GUILayout.Button(m_resetButtonContent, GUILayout.ExpandWidth(false)))
            {
                m_configs.ResetInspectorMessageFontSize();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Context Object's Name");
            Color color = EditorGUILayout.ColorField(m_configs.ObjectNameColor);
            if (color != m_configs.ObjectNameColor)
            {
                m_configs.ObjectNameColor = color;
            }
            if (GUILayout.Button(m_resetButtonContent, GUILayout.ExpandWidth(false)))
            {
                m_configs.ResetObjectNameColor();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("Categories", EditorStyles.boldLabel);
            GUILayout.Space(10);
            CategoryDisplayToggle("Show [CategoryName] in inspector", ECategoryDisplay.InInspector);

            CategoryDisplayToggle("Show [CategoryName] in Message", ECategoryDisplay.InMessage);
            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("[CategoryName] Colorization");
            m_configs.CategoryNameColorize = EditorGUILayout.Slider(m_configs.CategoryNameColorize, 0, 1);
            if (GUILayout.Button(m_resetButtonContent, GUILayout.ExpandWidth(false)))
            {
                m_configs.ResetCategoryNameColorize();
            }
            GUILayout.EndHorizontal();
            CategoryDisplayToggle("Show Category Color in Log List", ECategoryDisplay.ColorStrip);
            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Color Strip Width");
            m_configs.ColorStripWidth = EditorGUILayout.IntSlider(m_configs.ColorStripWidth, 3, 15);
            if (GUILayout.Button(m_resetButtonContent, GUILayout.ExpandWidth(false)))
            {
                m_configs.ResetColorStripWidth();
            }
            GUILayout.EndHorizontal();
            CategoryDisplayToggle("Show Category Name in column in Log List", ECategoryDisplay.NameColumn);
            //CategoryDisplayToggle("Show Category Icon in Log List", ECategoryDisplay.Icon);
            EditorGUILayout.LabelField("If the log entry is in multiple categories,\nonly the name of the first category will be displayed", m_subtitleStyle);
            GUILayout.Space(12);
            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Category Name column Colorization");
            m_configs.CategoryNameInLogListColorize = EditorGUILayout.Slider(m_configs.CategoryNameInLogListColorize, 0, 1);
            if (GUILayout.Button(m_resetButtonContent, GUILayout.ExpandWidth(false)))
            {
                m_configs.ResetCategoryNameInLogListColorize();
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

            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Log List Category Count");
            m_configs.CategoryCountInLogList = Mathf.Max(0, EditorGUILayout.IntField(m_configs.CategoryCountInLogList));
            GUILayout.EndHorizontal();
            EditorGUILayout.LabelField("How many [CategoryName] to display\nin the console list", m_subtitleStyle);
            GUILayout.Space(12);

            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(" ");
            if (GUILayout.Button("Reset Everything to Default Values"))
            {
                m_configs.ResetAll();
            }
            GUILayout.EndHorizontal();
        }

        [SettingsProvider]
        internal static SettingsProvider CreateMyCustomSettingsProvider()
        {
            var provider = new ProperLoggerCustomSettingsProvider(s_pathToPreferences, SettingsScope.User);

            // Automatically extract all keywords from the Styles.
            //provider.keywords = GetSearchKeywordsFromGUIContentProperties<Styles>();
            return provider;
        }
    }
}