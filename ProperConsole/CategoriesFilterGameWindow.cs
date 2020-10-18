using System.Collections;
using UnityEngine;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif //UNITY_EDITOR

namespace ProperLogger
{
    // TODO mutualize
    [Obfuscation(Exclude = true, ApplyToMembers = false)]
    internal class CategoriesFilterGameWindow : MonoBehaviour
    {
        private ConfigsProvider m_configs = null;

        [SerializeField, Obfuscation(Exclude = true)]
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

            GUILayout.BeginArea(ProperConsoleGameWindow.Instance.CategoryFilterRect, (GUIStyle)"Box");

            m_configs = m_configs ?? PlayerConfigs.Instance;

            if(m_configs.CurrentCategoriesConfig == null)
            {
                GUILayout.Label("No categories asset have been configured.\nOpen plugin settings in the editor\nto fix this.");

                GUILayout.Space(15);
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
                            if(ProperConsoleGameWindow.Instance != null)
                            {
                                ProperConsoleGameWindow.Instance.InactiveCategories = null;
                                ProperConsoleGameWindow.Instance.TriggerFilteredEntryComputation = true;
                                ProperConsoleGameWindow.Instance.TriggerRepaint();
                            }
                        }
                    }
                    GUI.color = defaultColor;
                }

                GUILayout.Space(10);

                if (GUI.changed)
                {
                    ProperConsoleGameWindow.Instance.SetTriggerFilteredEntryComputation();
                }

#if UNITY_EDITOR
                if (GUILayout.Button("Open Categories Settings"))
                {
                    Selection.activeObject = m_configs.CurrentCategoriesConfig;
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