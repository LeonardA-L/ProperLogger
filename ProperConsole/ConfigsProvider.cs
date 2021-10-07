using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProperLogger
{
    internal abstract class ConfigsProvider<T> : ConfigsProvider where T : new()
    {
        private static T s_instance = default;
        internal static T Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = new T();
                }
                return s_instance;
            }
        }
    }

    internal abstract class ConfigsProvider { 
        protected abstract void Save();

        protected abstract float GetFloat(string key, float defaultValue);
        protected abstract int GetInt(string key, int defaultValue);
        protected abstract string GetString(string key, string defaultValue);
        protected abstract bool GetBool(string key, bool defaultValue);

        protected abstract void SetFloat(string key, float newValue);
        protected abstract void SetInt(string key, int newValue);
        protected abstract void SetString(string key, string newValue);
        protected abstract void SetBool(string key, bool newValue);

        protected abstract void Reset(string key);

        internal void ResetAll()
        {
            ResetadvancedSearchToolbar();
            ResetcaseSensitive();
            ResetCategoriesConfigPath();
            ResetCategoryCountInLogList();
            ResetCategoryDisplay();
            ResetCategoryNameColorize();
            ResetCategoryNameInLogListColorize();
            ResetclearOnBuild();
            ResetclearOnPlay();
            Resetcollapse();
            ResetColorStripWidth();
            ReseterrorPause();
            ResetInactiveCategories();
            ResetInspectorMessageFontSize();
            ResetinspectorOnTheRight();
            ResetLogEntryMessageFontSize();
            ResetLogEntryMessageLineCount();
            ResetLogEntryStackTraceFontSize();
            ResetLogEntryStackTraceLineCount();
            ResetLogLevelFilter();
            ResetObjectNameColor();
            ResetregexSearch();
            ResetsearchInStackTrace();
            ResetsearchObjectName();
            ResetshowContextNameInsteadOfStack();
            ResetShowCustomErrorIcons();
            ResetDisplayIcons();
            ResetTimeFormat();
        }

        internal void ResetShowPathInMessage()
        {
            Reset("ProperConsole.ShowPathInMessage");
        }
        internal bool ShowPathInMessage
        {
            get
            {
                return GetBool("ProperConsole.ShowPathInMessage", false);
            }
            set
            {
                SetBool("ProperConsole.ShowPathInMessage", value);
                Save();
            }
        }

        internal void ResetDisplayIcons()
        {
            Reset("ProperConsole.DisplayIcons");
        }
        internal int DisplayIcons
        {
            get
            {
                return GetInt("ProperConsole.DisplayIcons", 1);
            }
            set
            {
                SetInt("ProperConsole.DisplayIcons", value);
                Save();
            }
        }

        internal void ResetTimeFormat() {
            Reset("ProperConsole.TimeFormat");
        }
        internal int TimeFormat {
            get {
                return GetInt("ProperConsole.TimeFormat", 0);
            }
            set {
                SetInt("ProperConsole.TimeFormat", value);
                Save();
            }
        }

        private bool ColorStripWidthIsCached = false;
        private int ColorStripWidthCache = 1;
        internal void ResetColorStripWidth()
        {
            Reset("ProperConsole.ColorStripWidth");
            ColorStripWidthIsCached = false;
        }
        internal int ColorStripWidth
        {
            get
            {
                if (!ColorStripWidthIsCached)
                {
                    ColorStripWidthCache = GetInt("ProperConsole.ColorStripWidth", 5);
                    ColorStripWidthIsCached = true;
                }
                return ColorStripWidthCache;
            }
            set
            {
                SetInt("ProperConsole.ColorStripWidth", value);
                ColorStripWidthCache = value;
                Save();
            }
        }

        internal void ResetCategoryNameColorize()
        {
            Reset("ProperConsole.CategoryNameColorize");
        }
        internal float CategoryNameColorize
        {
            get
            {
                return GetFloat("ProperConsole.CategoryNameColorize", 0.4f);
            }
            set
            {
                SetFloat("ProperConsole.CategoryNameColorize", value);
                Save();
            }
        }

        internal void ResetCategoryCountInLogList()
        {
            Reset("ProperConsole.CategoryCountInLogList");
        }
        internal int CategoryCountInLogList
        {
            get
            {
                return GetInt("ProperConsole.CategoryCountInLogList", 3);
            }
            set
            {
                SetInt("ProperConsole.CategoryCountInLogList", value);
                Save();
            }
        }

        private bool CategoryNameInLogListColorizeIsCached = false;
        private float CategoryNameInLogListColorizeCache = 0.4f;
        internal void ResetCategoryNameInLogListColorize()
        {
            Reset("ProperConsole.CategoryNameInLogListColorize");
            CategoryNameInLogListColorizeIsCached = false;
        }
        internal float CategoryNameInLogListColorize
        {
            get
            {
                if (!CategoryNameInLogListColorizeIsCached)
                {
                    CategoryNameInLogListColorizeCache = GetFloat("ProperConsole.CategoryNameInLogListColorize", 0.4f);
                    CategoryNameInLogListColorizeIsCached = true;
                }
                return CategoryNameInLogListColorizeCache;
            }
            set
            {
                CategoryNameInLogListColorizeCache = value;
                SetFloat("ProperConsole.CategoryNameInLogListColorize", value);
                Save();
            }
        }

        private bool CategoryDisplayIsCached = false;
        private ECategoryDisplay CategoryDisplayCache = (ECategoryDisplay.NameColumn | ECategoryDisplay.ColorStrip | ECategoryDisplay.InInspector);
        internal void ResetCategoryDisplay()
        {
            Reset("ProperConsole.CategoryDisplay");
            CategoryDisplayIsCached = false;
        }
        internal ECategoryDisplay CategoryDisplay
        {
            get
            {
                if (!CategoryDisplayIsCached)
                {
                    CategoryDisplayCache = (ECategoryDisplay)GetInt("ProperConsole.CategoryDisplay", (int)(ECategoryDisplay.NameColumn | ECategoryDisplay.ColorStrip | ECategoryDisplay.InInspector));
                    CategoryDisplayIsCached = true;
                }
                return CategoryDisplayCache;
            }
            set
            {
                CategoryDisplayCache = value;
                SetInt("ProperConsole.CategoryDisplay", (int)value);
                Save();
            }
        }

        internal void ResetInactiveCategories()
        {
            Reset("ProperConsole.InactiveCategories");
        }
        internal List<LogCategory> InactiveCategories
        {
            get
            {
                if(CurrentCategoriesConfig == null)
                {
                    return new List<LogCategory>();
                }
                string inactiveCategories = GetString("ProperConsole.InactiveCategories", "");
                string[] inactiveCategoriesArray = inactiveCategories.Split(new char[] { '|' }, System.StringSplitOptions.RemoveEmptyEntries);
                List<LogCategory> inactiveCategoryObjects = new List<LogCategory>();
                foreach (var activeStr in inactiveCategoriesArray)
                {
                    foreach (var category in CurrentCategoriesConfig.Categories)
                    {
                        if(category.Name == activeStr)
                        {
                            inactiveCategoryObjects.Add(category);
                            break;
                        }
                    }
                }
                return inactiveCategoryObjects;
            }
            set
            {
                SetString("ProperConsole.InactiveCategories", string.Join("|", value.Select(c=>c.Name)));
                Save();
            }
        }

        internal void ResetCategoriesConfigPath()
        {
            Reset("ProperConsole.CategoriesConfigPath");
        }
        internal abstract LogCategoriesConfig CurrentCategoriesConfig
        {
            get;set;
        }

        private bool LogEntryMessageLineCountIsCached = false;
        private int LogEntryMessageLineCountCache = 1;
        internal void ResetLogEntryMessageLineCount()
        {
            Reset("ProperConsole.LogEntryMessageLineCount");
            LogEntryMessageLineCountIsCached = false;
        }
        internal int LogEntryMessageLineCount
        {
            get
            {
                if (!LogEntryMessageLineCountIsCached)
                {
                    LogEntryMessageLineCountCache = GetInt("ProperConsole.LogEntryMessageLineCount", 1);
                    LogEntryMessageLineCountIsCached = true;
                }
                return LogEntryMessageLineCountCache;
            }
            set
            {
                LogEntryMessageLineCountCache = value;
                SetInt("ProperConsole.LogEntryMessageLineCount", value);
                Save();
            }
        }

        private bool LogEntryStackTraceLineCountIsCached = false;
        private int LogEntryStackTraceLineCountCache = 1;
        internal void ResetLogEntryStackTraceLineCount()
        {
            Reset("ProperConsole.LogEntryStackTraceLineCount");
            LogEntryStackTraceLineCountIsCached = false;
        }
        internal int LogEntryStackTraceLineCount
        {
            get
            {
                if (!LogEntryStackTraceLineCountIsCached)
                {
                    LogEntryStackTraceLineCountCache = GetInt("ProperConsole.LogEntryStackTraceLineCount", 1);
                    LogEntryStackTraceLineCountIsCached = true;
                }
                return LogEntryStackTraceLineCountCache;
            }
            set
            {
                LogEntryStackTraceLineCountCache = value;
                SetInt("ProperConsole.LogEntryStackTraceLineCount", value);
                Save();
            }
        }

        private bool LogEntryMessageFontSizeIsCached = false;
        private int LogEntryMessageFontSizeCache = 14;
        internal void ResetLogEntryMessageFontSize()
        {
            Reset("ProperConsole.LogEntryMessageFontSize");
            LogEntryMessageFontSizeIsCached = false;
        }
        internal int LogEntryMessageFontSize
        {
            get
            {
                if (!LogEntryMessageFontSizeIsCached)
                {
                    LogEntryMessageFontSizeCache = GetInt("ProperConsole.LogEntryMessageFontSize", 14);
                    LogEntryMessageFontSizeIsCached = true;
                }
                return LogEntryMessageFontSizeCache;
            }
            set
            {
                LogEntryMessageFontSizeCache = value;
                SetInt("ProperConsole.LogEntryMessageFontSize", value);
                Save();
            }
        }

        private bool LogEntryStackTraceFontSizeIsCached = false;
        private int LogEntryStackTraceFontSizeCache = 12;
        internal void ResetLogEntryStackTraceFontSize()
        {
            Reset("ProperConsole.LogEntryStackTraceFontSize");
            LogEntryStackTraceFontSizeIsCached = false;
        }
        internal int LogEntryStackTraceFontSize
        {
            get
            {
                if (!LogEntryStackTraceFontSizeIsCached)
                {
                    LogEntryStackTraceFontSizeCache = GetInt("ProperConsole.LogEntryStackTraceFontSize", 12);
                    LogEntryStackTraceFontSizeIsCached = true;
                }
                return LogEntryStackTraceFontSizeCache;
            }
            set
            {
                LogEntryStackTraceFontSizeCache = value;
                SetInt("ProperConsole.LogEntryStackTraceFontSize", value);
                Save();
            }
        }

        internal void ResetInspectorMessageFontSize()
        {
            Reset("ProperConsole.InspectorMessageFontSize");
        }
        internal int InspectorMessageFontSize
        {
            get
            {
                return GetInt("ProperConsole.InspectorMessageFontSize", 12);
            } set
            {
                SetInt("ProperConsole.InspectorMessageFontSize", value);
                Save();
            }
        }

        internal void ResetObjectNameColor()
        {
            Reset("ProperConsole.ObjectNameColor");
        }
        internal string ObjectNameColorString
        {
            get
            {
                return "#" + GetString("ProperConsole.ObjectNameColor", "540814FF");
            }
        }

        internal Color ObjectNameColor
        {
            get
            {
                ColorUtility.TryParseHtmlString($"#{GetString("ProperConsole.ObjectNameColor", "540814FF")}", out Color result);
                return result;
            }
            set
            {
                SetString("ProperConsole.ObjectNameColor", ColorUtility.ToHtmlStringRGBA(value));
                Save();
            }
        }


        private bool LogLevelFilterIsCached = false;
        private int LogLevelFilterCache = (int)LogLevel.All;
        internal void ResetLogLevelFilter()
        {
            Reset("ProperConsole.LogLevelFilter");
            LogLevelFilterIsCached = false;
        }
        internal LogLevel LogLevelFilter
        {
            get
            {
                if (!LogLevelFilterIsCached)
                {
                    LogLevelFilterCache = GetInt("ProperConsole.LogLevelFilter", (int)LogLevel.All);
                    LogLevelFilterIsCached = true;
                }
                return (LogLevel)LogLevelFilterCache;
            }
            set
            {
                LogLevelFilterCache = (int)value;
                SetInt("ProperConsole.LogLevelFilter", (int)value);
                Save();
            }
        }

        private bool ShowContextNameInsteadOfStackIsCached = false;
        private bool ShowContextNameInsteadOfStackCache = true;
        internal void ResetshowContextNameInsteadOfStack()
        {
            Reset("ProperConsole.showContextNameInsteadOfStack");
            ShowContextNameInsteadOfStackIsCached = false;
        }
        internal bool ShowContextNameInsteadOfStack
        {
            get
            {
                if (!ShowContextNameInsteadOfStackIsCached)
                {
                    ShowContextNameInsteadOfStackCache = GetBool("ProperConsole.showContextNameInsteadOfStack", true);
                    ShowContextNameInsteadOfStackIsCached = true;
                }
                return ShowContextNameInsteadOfStackCache;
            }
            set
            {
                ShowContextNameInsteadOfStackCache = value;
                SetBool("ProperConsole.showContextNameInsteadOfStack", value);
                Save();
            }
        }

        internal void ResetShowCustomErrorIcons()
        {
            Reset("ProperConsole.showContextNameInsteadOfStack");
        }
        internal bool ShowCustomErrorIcons
        {
            get
            {
                return GetBool("ProperConsole.ShowCustomErrorIcons", true);
            }
            set
            {
                SetBool("ProperConsole.ShowCustomErrorIcons", value);
                Save();
            }
        }

        internal void ResetinspectorOnTheRight()
        {
            Reset("ProperConsole.inspectorOnTheRight");
        }
        internal bool InspectorOnTheRight
        {
            get
            {
                return GetBool("ProperConsole.inspectorOnTheRight", false);
            }
            set
            {
                SetBool("ProperConsole.inspectorOnTheRight", value);
                Save();
            }
        }

        internal void ResetclearOnPlay()
        {
            Reset("ProperConsole.clearOnPlay");
        }
        internal bool ClearOnPlay
        {
            get
            {
                return GetBool("ProperConsole.clearOnPlay", false);
            }
            set
            {
                SetBool("ProperConsole.clearOnPlay", value);
                Save();
            }
        }

        internal void ResetclearOnBuild()
        {
            Reset("ProperConsole.clearOnBuild");
        }
        internal bool ClearOnBuild
        {
            get
            {
                return GetBool("ProperConsole.clearOnBuild", false);
            }
            set
            {
                SetBool("ProperConsole.clearOnBuild", value);
                Save();
            }
        }

        internal void ReseterrorPause()
        {
            Reset("ProperConsole.errorPause");
        }
        internal bool ErrorPause
        {
            get
            {
                return GetBool("ProperConsole.errorPause", false);
            }
            set
            {
                SetBool("ProperConsole.errorPause", value);
                Save();
            }
        }

        internal void Resetcollapse()
        {
            Reset("ProperConsole.collapse");
        }
        internal bool Collapse
        {
            get
            {
                return GetBool("ProperConsole.collapse", false);
            }
            set
            {
                SetBool("ProperConsole.collapse", value);
                Save();
            }
        }

        internal void ResetadvancedSearchToolbar()
        {
            Reset("ProperConsole.advancedSearchToolbar");
        }
        internal bool AdvancedSearchToolbar
        {
            get
            {
                return GetBool("ProperConsole.advancedSearchToolbar", false);
            }
            set
            {
                SetBool("ProperConsole.advancedSearchToolbar", value);
                Save();
            }
        }

        internal void ResetregexSearch()
        {
            Reset("ProperConsole.regexSearch");
        }
        internal bool RegexSearch
        {
            get
            {
                return GetBool("ProperConsole.regexSearch", false);
            }
            set
            {
                SetBool("ProperConsole.regexSearch", value);
                Save();
            }
        }

        internal void ResetcaseSensitive()
        {
            Reset("ProperConsole.caseSensitive");
        }
        internal bool CaseSensitive
        {
            get
            {
                return GetBool("ProperConsole.caseSensitive", false);
            }
            set
            {
                SetBool("ProperConsole.caseSensitive", value);
                Save();
            }
        }

        internal void ResetsearchObjectName()
        {
            Reset("ProperConsole.searchObjectName");
        }
        internal bool SearchObjectName
        {
            get
            {
                return GetBool("ProperConsole.searchObjectName", true);
            }
            set
            {
                SetBool("ProperConsole.searchObjectName", value);
                Save();
            }
        }

        internal void ResetsearchInStackTrace()
        {
            Reset("ProperConsole.searchInStackTrace");
        }
        internal bool SearchInStackTrace
        {
            get
            {
                return GetBool("ProperConsole.searchInStackTrace", false);
            }
            set
            {
                SetBool("ProperConsole.searchInStackTrace", value);
                Save();
            }
        }
    }
}