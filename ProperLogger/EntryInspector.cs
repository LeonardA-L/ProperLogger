#if PROPER_LOGGER_DEBUG && UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProperLogger
{
    public class EntryInspector : EditorWindow
    {
        private static EntryInspector s_instance = null;
        private Vector2 m_scrollPosition = default;
        private DebugConsoleEntry m_debugEntry;

        [MenuItem("Window/Proper Logger/Entry Inspector")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            if (EntryInspector.s_instance != null)
            {
                EntryInspector.s_instance.Show(true);
            }
            else
            {
                ShowWindow();
            }
            EntryInspector.s_instance.Focus();
        }

        internal static void ShowWindow()
        {
            if (s_instance != null)
            {
                s_instance.Show(true);
                s_instance.Focus();
            }
            else
            {
                s_instance = ScriptableObject.CreateInstance<EntryInspector>();
                s_instance.Show(true);
                s_instance.Focus();
            }
        }

        private void OnEnable()
        {
            s_instance = this;
            EntryInspector.s_instance.titleContent = new GUIContent("Entry Inspector");
            m_debugEntry = CreateInstance<DebugConsoleEntry>();
            m_debugEntry.entries = new List<ConsoleLogEntry>();
        }

        private void OnDisable()
        {

        }

        void OnGUI()
        {
            var properInstance = ProperConsoleWindow.Instance;
            if (properInstance != null && properInstance.SelectedEntries != null)
            {
                GUILayout.Label($"{properInstance.SelectedEntries.Count}");
                //m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition);
                m_debugEntry.entries.Clear();
                foreach (var item in properInstance.SelectedEntries)
                {
                    //Debug.Log(item.message);
                    m_debugEntry.entries.Add(item);
                }
                var editor = Editor.CreateEditor(m_debugEntry);
                if (editor != null)
                {
                    editor.OnInspectorGUI();
                } else
                {
                    Debug.Log("Null Editor");
                }
                //GUILayout.EndScrollView();
            }
        }
    }

    [Serializable]
    internal class DebugConsoleEntry : ScriptableObject
    {
        public List<ConsoleLogEntry> entries = null;
    }
}
#endif