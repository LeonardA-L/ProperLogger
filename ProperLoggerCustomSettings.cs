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

            EditorGUI.BeginChangeCheck();

            m_configs.InspectorOnTheRight = EditorGUILayout.Toggle("Show log inspector on the right", m_configs.InspectorOnTheRight);
            GUILayout.Space(10);
            m_configs.CopyOnSelect = EditorGUILayout.Toggle("Copy on Select", m_configs.CopyOnSelect);
            EditorGUILayout.LabelField("Selecting a log will automatically copy\nits content to your clipboard", m_subtitleStyle);
            GUILayout.Space(12);
            m_configs.ShowContextNameInsteadOfStack = EditorGUILayout.Toggle("Show name of context object in log list", m_configs.ShowContextNameInsteadOfStack);
            EditorGUILayout.LabelField("If applicable, will show the name of the\ncontext objectin the log list, instead of\nthe first line of the stack trace", m_subtitleStyle);
            GUILayout.Space(12);

            if (EditorGUI.EndChangeCheck() && ProperConsoleWindow.Instance != null)
            {
                ProperConsoleWindow.Instance.Repaint();
            }
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