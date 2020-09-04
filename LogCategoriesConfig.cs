using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ProperLogger
{
    [CreateAssetMenu(menuName = "Proper Logger/Categories")]
    public class LogCategoriesConfig : ScriptableObject
    {
        private static LogCategoriesConfig m_instance = null;
        public static LogCategoriesConfig Instance => m_instance;

        [SerializeField]
        protected List<LogCategory> m_categories = null;

        [System.NonSerialized]
        protected Dictionary<string, LogCategory> m_categoriesByName = null;

        protected virtual void OnEnable()
        {
            m_instance = this;
            RebuildCategories();
        }

        private void RebuildCategories()
        {
            m_categoriesByName?.Clear();
            m_categoriesByName = new Dictionary<string, LogCategory>();

            if(m_categories == null || m_categories.Count == 0)
            {
                return;
            }

            foreach (var category in m_categories)
            {
                m_categoriesByName.Add(category.Name, category);
            }
        }

        public virtual void Add(string name)
        {
            m_categories = m_categories ?? new List<LogCategory>();
            var newCat = new LogCategory(name);
            m_categories.Add(newCat);
        }
    }
}