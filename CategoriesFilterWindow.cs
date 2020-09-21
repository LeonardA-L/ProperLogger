﻿using System.Collections;
using UnityEngine;
using UnityEditor;

namespace ProperLogger
{
    public class CategoriesFilterWindow : EditorWindow
    {
        [SerializeField]
        private GUISkin m_skin = null;
        private ConfigsProvider m_configs = null;

        void OnGUI()
        {
            m_configs = m_configs ?? new EditorConfigs();

            if(m_configs.CurrentCategoriesConfig == null)
            {
                GUILayout.Label("No categories asset have been configured.\nPlease open the preference window\nto setup categories."); // TODO style

                GUILayout.Space(15);

                if (GUILayout.Button("Open Categories Settings"))
                {
                    SettingsService.OpenUserPreferences(ProperLoggerCustomSettingsProvider.s_pathToPreferences);
                    if (ProperLoggerCustomSettingsProvider.Instance != null)
                    {
                        ProperLoggerCustomSettingsProvider.Instance.SetCurrentSelectedTab(1);
                    }
                }
            }
            else
            {
                if(m_configs.CurrentCategoriesConfig.Categories == null || m_configs.CurrentCategoriesConfig.Categories.Count == 0)
                {
                    GUILayout.Label("No categories found.");
                }
                else
                {
                    Color defaultColor = GUI.color;
                    var inactiveCategories = m_configs.InactiveCategories;
                    foreach (var category in m_configs.CurrentCategoriesConfig.Categories)
                    {
                        bool lastActive = !inactiveCategories.Contains(category);
                        GUI.color = Color.Lerp(category.Color, defaultColor, m_configs.CategoryNameColorize);
                        bool isActive = GUILayout.Toggle(lastActive, category.Name);
                        if (isActive != lastActive)
                        {
                            if (isActive)
                            {
                                inactiveCategories.Remove(category);
                                m_configs.InactiveCategories = inactiveCategories;
                            } else
                            {
                                inactiveCategories.Add(category);
                                m_configs.InactiveCategories = inactiveCategories;
                            }
                            if(ProperConsoleWindow.Instance != null)
                            {
                                ProperConsoleWindow.Instance.Repaint();
                            }
                        }
                    }
                    GUI.color = defaultColor;
                }

                GUILayout.Space(10);

                if (GUILayout.Button("Open Categories Settings"))
                {
                    Selection.activeObject = m_configs.CurrentCategoriesConfig;
                }
            }
        }

    }
}