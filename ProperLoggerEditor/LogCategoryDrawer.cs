using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ProperLogger
{
    public class LogCategoryDrawer
    {
        [CustomPropertyDrawer(typeof(CategoryParentAttribute))]
        public class CategoryParentDrawer : PropertyDrawer
        {
            private static string s_none = "<None>";
            private static ConfigsProvider m_configs = null;

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                m_configs = m_configs ?? EditorConfigs.Instance;
                var categoriesConfig = m_configs.CurrentCategoriesConfig;
                if(categoriesConfig == null)
                {
                    property.stringValue = null;
                    return;
                }
                var categoryNames = categoriesConfig.Categories.Select(c => c.Name).ToList();
                categoryNames.Insert(0, s_none);
                var categoryArray = categoryNames.ToArray();
                int index = Mathf.Clamp(Array.IndexOf(categoryArray, property.stringValue), 0, categoryArray.Length);
                if (!categoryNames.Contains(property.stringValue))
                {
                    index = 0;
                    property.stringValue = categoryNames[0];
                }
                else
                {
                    var stringValue = categoryNames[EditorGUI.Popup(position, "Parent Category", index, categoryArray)];
                    if (stringValue == s_none)
                    {
                        property.stringValue = null;
                    }
                    else
                    {
                        property.stringValue = stringValue;
                    }
                }
            }
        }
    }
}