using System.Collections;
using UnityEngine;
using UnityEditor;
using C = ProperLogger.CommonMethods;

namespace ProperLogger
{
    internal class CategoriesFilterWindow : EditorWindow, ICategoryWindow
    {
        public ConfigsProvider Config => EditorConfigs.Instance;
        public IProperLogger Console => ProperConsoleWindow.Instance;

        [System.Reflection.Obfuscation(Exclude = true)]
        private void OnGUI()
        {
            if(Config.CurrentCategoriesConfig == null)
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
                if(Config.CurrentCategoriesConfig.Categories == null || Config.CurrentCategoriesConfig.Categories.Count == 0)
                {
                    GUILayout.Label("No categories found.");
                }
                else
                {
                    C.DisplayCategoryFilterContent(this);
                }

                GUILayout.Space(10);

                if (GUILayout.Button("Open Categories Settings"))
                {
                    Selection.activeObject = Config.CurrentCategoriesConfig;
                }
            }
        }
    }
}