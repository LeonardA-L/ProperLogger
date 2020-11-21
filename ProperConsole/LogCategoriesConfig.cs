using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProperLogger
{
    [CreateAssetMenu(menuName = "Proper Logger/Categories")]
    [Obfuscation(Exclude = true, ApplyToMembers = false)]
    public class LogCategoriesConfig : ScriptableObject
    {
        internal static int s_categoryIndent = 20;
        internal static int s_categoryCharacterSize = 6;

        private static LogCategoriesConfig m_instance = null;
        public static LogCategoriesConfig Instance => m_instance;

        [SerializeField, Obfuscation(Exclude = true)]
        [Tooltip("Log Categories. Use them to filter and group your log messages.")]
        protected List<LogCategory> m_categories = null;

        //[System.NonSerialized]
        //protected Dictionary<string, LogCategory> m_categoriesByName = null;
        [NonSerialized]
        private int m_longestName;
        public int LongestName => m_longestName;
        [NonSerialized]
        protected List<LogCategory> m_rootCategories = null;
        public List<LogCategory> RootCategories => m_rootCategories;

        public List<LogCategory> Categories => m_categories;
        //public Dictionary<string, LogCategory> CategoriesByName => m_categoriesByName ?? (m_categoriesByName = RebuildCategories());

        protected virtual void OnEnable()
        {
            m_instance = this;
            //RebuildCategories();
        }
        /*
        internal Dictionary<string, LogCategory> RebuildCategories()
        {
            if(m_categories == null || m_categories.Count == 0)
            {
                return null;
            }

            m_categoriesByName?.Clear();
            m_categoriesByName = new Dictionary<string, LogCategory>();

            foreach (var category in m_categories)
            {
                m_categoriesByName.Add(category.Name, category);
            }

            return m_categoriesByName;
        }*/

        public virtual LogCategory Add(string name, Color color)
        {
            m_categories = m_categories ?? new List<LogCategory>();
            var newCat = new LogCategory(name);
            newCat.Color = color;
            m_categories.Add(newCat);
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
            return newCat;
        }

        public virtual LogCategory Add(string name)
        {
            return Add(name, Color.red);
        }

        public LogCategory this[string n]
        {
            get
            {
                foreach (var category in Categories)
                {
                    if(category.Name == n)
                    {
                        return category;
                    }
                }
                return null;
            }
        }

        internal List<LogCategory> PopulateRootCategories()
        {
            Dictionary<string, LogCategory> categories = new Dictionary<string, LogCategory>();
            m_rootCategories = m_rootCategories ?? new List<LogCategory>();
            m_rootCategories.Clear();

            // Clear Children in Categories and register them
            foreach (var category in Categories)
            {
                category.ClearChildren();
                if (!categories.ContainsKey(category.Name))
                {
                    categories.Add(category.Name, category);
                }
                m_rootCategories.Add(category);
            }

            // Populate children
            foreach (var category in Categories)
            {
                if (!String.IsNullOrEmpty(category.Parent) && categories.ContainsKey(category.Parent))
                {
                    categories[category.Parent].AddChild(category);
                    m_rootCategories.Remove(category);
                }
            }

            m_longestName = 0;
            FindLongestName(m_rootCategories, 0, ref m_longestName);

            return m_rootCategories;
        }

        private void FindLongestName(List<LogCategory> roots, int level, ref int length)
        {
            for (int i = 0; i < roots.Count; i++)
            {
                var category = roots[i];

                int catLength = (level + 2) * s_categoryIndent + s_categoryCharacterSize * category.Name.Length;
                if (catLength > length)
                {
                    length = catLength;
                }

                if (category.Children != null && category.Children.Count > 0)
                {
                    FindLongestName(category.Children, level + 1, ref length);
                }
            }
        }
    }
}