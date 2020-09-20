using System.Collections;
using UnityEngine;
using UnityEditor;

namespace ProperLogger
{
    public class CategoriesFilterWindow : EditorWindow
    {
        private ConfigsProvider m_configs = null;

        void OnGUI()
        {
            m_configs = m_configs ?? new EditorConfigs();

            if(m_configs.CurrentCategoriesConfig == null)
            {
                GUILayout.Label("No categories asset have been configured.\nPlease open the preference window to setup categories."); // TODO style
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
                    if (GUILayout.Button("Open Categories Settings"))
                    {
                        Selection.activeObject = m_configs.CurrentCategoriesConfig;
                    }
                }
                else
                {
                    var inactiveCategories = m_configs.InactiveCategories;
                    foreach (var category in m_configs.CurrentCategoriesConfig.Categories)
                    {
                        bool lastActive = !inactiveCategories.Contains(category);
                        bool isActive = GUILayout.Toggle(lastActive, category.Name);

                        if(isActive != lastActive)
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
                }
            }
        }

    }
}