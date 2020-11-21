using System.Collections;
using UnityEngine;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif //UNITY_EDITOR
using C = ProperLogger.CommonMethods;

namespace ProperLogger
{
    [Obfuscation(Exclude = true, ApplyToMembers = false)]
    internal class CategoriesFilterGameWindow : MonoBehaviour, ICategoryWindow
    {
        public ConfigsProvider Config => PlayerConfigs.Instance;
        public IProperLogger Console => ProperConsoleGameWindow.Instance;

        [SerializeField, Obfuscation(Exclude = true)]
#if !DEBUG
        [HideInInspector]
#endif
        protected int m_depth = 1;

        [Obfuscation(Exclude = true)]
        private void OnGUI()
        {
            if(ProperConsoleGameWindow.Instance == null)
            {
                return;
            }

            if (!ProperConsoleGameWindow.Instance.ShowCategoryFilter)
            {
                return;
            }

            if(ProperConsoleGameWindow.Instance.Skin != null)
            {
                GUI.skin = ProperConsoleGameWindow.Instance.Skin;
            }

            float refScreenWidth = 1280f;
            float refScreenHeight = 768f;

            float xFactor = Screen.width / refScreenWidth;
            float yFactor = Screen.height / refScreenHeight;
            GUIUtility.ScaleAroundPivot(new Vector2(xFactor, yFactor), Vector2.zero);

            GUI.depth = m_depth;

            GUILayout.BeginArea(ProperConsoleGameWindow.Instance.CategoryFilterRect, Strings.Box);

            if(Config.CurrentCategoriesConfig == null)
            {
                GUILayout.Label("No categories asset have been configured.\nPlease open the preference window\nto setup categories."); // TODO style

                GUILayout.Space(15);
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

                if (GUI.changed)
                {
                    ProperConsoleGameWindow.Instance.SetTriggerFilteredEntryComputation();
                }

#if UNITY_EDITOR
                if (GUILayout.Button("Open Categories Settings"))
                {
                    Selection.activeObject = Config.CurrentCategoriesConfig;
                }
#endif //UNITY_EDITOR
            }
            GUILayout.EndArea();

            if((Event.current.type == EventType.ContextClick || Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseUp))
            {
                if (ProperConsoleGameWindow.Instance.CategoryFilterRect.Contains(Event.current.mousePosition))
                {
                    Event.current.Use();
                }
                else if(!ProperConsoleGameWindow.Instance.CategoryToggleRect.Contains(Event.current.mousePosition))
                {
                    ProperConsoleGameWindow.Instance.ShowCategoryFilter = false;
                }
            }
        }
    }
}