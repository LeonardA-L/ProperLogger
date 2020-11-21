using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;
using System.Reflection;

namespace ProperLogger
{
    [CustomEditor(typeof(LogCategoriesConfig))]
    public class LogCategoriesConfigEditor : Editor
    {
        private List<string> m_parentOptions = null;
        private GUIStyle m_boxStyle = null;
        private GUISkin m_consoleSkin = null;
        private GUIStyle m_categoryNameStyle = null;

        private List<string> m_issues = null;
        private bool m_issuesDirty = false;
        private Texture2D m_iconError = null;

        private List<LogCategory> m_collapsedCategories = null;

        public override void OnInspectorGUI()
        {
            m_consoleSkin = EditorUtils.LoadAssetByName<GUISkin>(Strings.EditorSkin);
            m_issuesDirty = false;
            //m_issues = m_issues ?? new List<string>();
            m_collapsedCategories = m_collapsedCategories ?? new List<LogCategory>();

            if (m_iconError == null)
            {
                var LoadIcon = typeof(EditorGUIUtility).GetMethod("LoadIcon", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic);
                m_iconError = (Texture2D)LoadIcon.Invoke(null, new object[] { "console.erroricon" });
            }

            DisplayErrors();

            m_issues = new List<string>();

            Debug.Assert(m_consoleSkin != null, $"Could not find {Strings.EditorSkin} skin. Try reimporting package.");

            LogCategoriesConfig config = target as LogCategoriesConfig;
            config.PopulateRootCategories();

            var parentOptionsList = new List<string>() { "<None>" };
            PopulateParentOptions(config.RootCategories, 0, parentOptionsList);
            m_parentOptions = new List<string>(parentOptionsList);

            m_categoryNameStyle = m_consoleSkin.FindStyle("CategoryConfigTitle");
            m_categoryNameStyle.normal.textColor = GUI.skin.label.normal.textColor;

            var rootCategories = config.RootCategories;
            DisplayCategories(rootCategories, 0);


            if (GUILayout.Button("Add Category", GUILayout.Height(30)))
            {
                AddCategory(null);
            }

            DisplayErrors();
        }

        private void DisplayErrors()
        {
            if (m_issues.Count > 0)
            {
                GUILayout.Space(15);
            }

            foreach (var item in m_issues)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Box(new GUIContent(m_iconError), GUIStyle.none, GUILayout.Width(25), GUILayout.Height(25));
                EditorGUILayout.LabelField(item, EditorStyles.boldLabel);
                GUILayout.EndHorizontal();
            }

            if(m_issues.Count > 0)
            {
                GUILayout.Space(15);
            }
        }

        private void PopulateParentOptions(List<LogCategory> roots, int level, List<string> accumulator)
        {
            for (int i = 0; i < roots.Count; i++)
            {
                var category = roots[i];
                if (accumulator.Contains(category.Name))
                {
                    if(!m_issuesDirty)
                    {
                        m_issues.Clear();
                        m_issuesDirty = true;
                    }
                    m_issues.Add($"There are multiple categories named {category.Name} !");
                }
                else
                {
                    accumulator.Add(new String(' ', level * 3) + category.Name);
                }
                if (category.Children != null && category.Children.Count > 0)
                {
                    PopulateParentOptions(category.Children, level + 1, accumulator);
                }
            }
        }

        private void AddCategory(LogCategory parent)
        {
            LogCategoriesConfig config = target as LogCategoriesConfig;
            string basename = "NewCategory";
            if(parent != null)
            {
                basename = $"New{parent.Name}Category";
            }
            string name = basename;
            int tryId = 0;

            bool bad = true;
            while (bad)
            {
                bad = false;

                name = basename + (tryId > 0 ? tryId.ToString() : "");

                for (int i = 0; i < config.Categories.Count; i++)
                {
                    var category = config.Categories[i];

                    if(category.Name == name)
                    {
                        bad = true;
                        tryId++;
                        break;
                    }
                }
            }

            var newCategory = config.Add(name, LogCategory.s_categoryColors[config.Categories.Count % LogCategory.s_categoryColors.Count]);
            if (parent != null)
            {
                newCategory.Parent = parent.Name;
            }
        }

        private void DisplayCategories(List<LogCategory> roots, int level)
        {
            for (int i = 0; i < roots.Count; i++)
            {
                GUILayout.BeginVertical(m_consoleSkin.FindStyle("CategoryConfigBox"));
                var category = roots[i];
                DisplayCategory(category);
                if (!IsCollapsed(category) && category.Children != null && category.Children.Count > 0)
                {
                    GUILayout.Space(15);
                    DisplayCategories(category.Children, level + 1);
                }
                GUILayout.EndVertical();
                //GUILayout.Space(15);
            }
        }

        private bool IsCollapsed(LogCategory category)
        {
            return m_collapsedCategories.Contains(category);
        }

        private void DisplayCategory(LogCategory category)
        {
            GUILayout.BeginHorizontal();
            bool collapsed = IsCollapsed(category);
            if (GUILayout.Button(new GUIContent(collapsed ? "▼" : "▲", collapsed ? "Expand" : "Collapse"), GUILayout.ExpandWidth(false), GUILayout.Width(35)))
            {
                if (collapsed)
                {
                    m_collapsedCategories.Remove(category);
                }
                else
                {
                    m_collapsedCategories.Add(category);
                }
                collapsed = !collapsed;
            }
            EditorGUILayout.LabelField(" " + category.Name, m_categoryNameStyle);
            if (collapsed)
            {
                GUILayout.EndHorizontal();
                return;
            }
            if (GUILayout.Button(new GUIContent("+", "Add Child Category"), GUILayout.ExpandWidth(false), GUILayout.Width(35)))
            {
                AddCategory(category);
            }
            if (category.Children == null || category.Children.Count == 0)
            {
                if (GUILayout.Button(new GUIContent("X", "Remove Category"), GUILayout.ExpandWidth(false), GUILayout.Width(35)))
                {
                    LogCategoriesConfig config = target as LogCategoriesConfig;
                    config.Categories.Remove(category);
                    EditorUtility.SetDirty(target);
                    return;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(15);
            EditorGUI.BeginChangeCheck();
            category.Name = EditorGUILayout.TextField("Category Name", category.Name);
            category.Color = EditorGUILayout.ColorField("Color", category.Color);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
            }
            int currentParentIndex = -1;
            if (!string.IsNullOrEmpty(category.Parent))
            {
                for (int i = 0; i < m_parentOptions.Count; i++)
                {
                    if (category.Parent == Trimmed(m_parentOptions[i]))
                    {
                        currentParentIndex = i;
                        break;
                    }
                }
            }
            currentParentIndex = Mathf.Max(0, currentParentIndex);

            var parentIndex = EditorGUILayout.Popup("Parent Category", currentParentIndex, m_parentOptions.ToArray());

            if(parentIndex != currentParentIndex)
            {
                ReorderLast(category);
                category.Parent = Trimmed(m_parentOptions[parentIndex]);
                EditorUtility.SetDirty(target);
            }
        }

        private void ReorderLast(LogCategory category)
        {
            LogCategoriesConfig config = target as LogCategoriesConfig;
            config.Categories.Remove(category);
            config.Categories.Add(category);
        }

        private string Trimmed(string str)
        {
            return str.Trim();
        }
    }
}