using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


namespace ProperLogger
{
    [CreateAssetMenu(menuName = "Proper Logger/Categories")]
    [Obfuscation(Exclude = true, ApplyToMembers = false)]
    public class LogCategoriesConfig : ScriptableObject
    {
        private static LogCategoriesConfig m_instance = null;
        public static LogCategoriesConfig Instance => m_instance;

        [SerializeField, Obfuscation(Exclude = true)]
        protected List<LogCategory> m_categories = null;

        //[System.NonSerialized]
        //protected Dictionary<string, LogCategory> m_categoriesByName = null;

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

        public virtual void Add(string name)
        {
            m_categories = m_categories ?? new List<LogCategory>();
            var newCat = new LogCategory(name);
            m_categories.Add(newCat);
        }
    }
}