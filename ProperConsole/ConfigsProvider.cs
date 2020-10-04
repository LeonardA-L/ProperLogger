﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProperLogger
{
    internal abstract class ConfigsProvider
    {
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
            ResetcopyOnSelect();
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

        internal void ResetColorStripWidth()
        {
            Reset("ProperConsole.ColorStripWidth");
        }
        internal int ColorStripWidth
        {
            get
            {
                return GetInt("ProperConsole.ColorStripWidth", 5);
            }
            set
            {
                SetInt("ProperConsole.ColorStripWidth", value);
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

        internal void ResetCategoryNameInLogListColorize()
        {
            Reset("ProperConsole.CategoryNameInLogListColorize");
        }
        internal float CategoryNameInLogListColorize
        {
            get
            {
                return GetFloat("ProperConsole.CategoryNameInLogListColorize", 0.4f);
            }
            set
            {
                SetFloat("ProperConsole.CategoryNameInLogListColorize", value);
                Save();
            }
        }

        internal void ResetCategoryDisplay()
        {
            Reset("ProperConsole.CategoryDisplay");
        }
        internal ECategoryDisplay CategoryDisplay
        {
            get
            {
                return (ECategoryDisplay)GetInt("ProperConsole.CategoryDisplay", (int)(ECategoryDisplay.NameColumn | ECategoryDisplay.ColorStrip | ECategoryDisplay.InInspector));
            }
            set
            {
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

        internal void ResetLogEntryMessageLineCount()
        {
            Reset("ProperConsole.LogEntryMessageLineCount");
        }
        internal int LogEntryMessageLineCount
        {
            get
            {
                return GetInt("ProperConsole.LogEntryMessageLineCount", 1);
            }
            set
            {
                SetInt("ProperConsole.LogEntryMessageLineCount", value);
                Save();
            }
        }

        internal void ResetLogEntryStackTraceLineCount()
        {
            Reset("ProperConsole.LogEntryStackTraceLineCount");
        }
        internal int LogEntryStackTraceLineCount
        {
            get
            {
                return GetInt("ProperConsole.LogEntryStackTraceLineCount", 1);
            }
            set
            {
                SetInt("ProperConsole.LogEntryStackTraceLineCount", value);
                Save();
            }
        }

        internal void ResetLogEntryMessageFontSize()
        {
            Reset("ProperConsole.LogEntryMessageFontSize");
        }
        internal int LogEntryMessageFontSize
        {
            get
            {
                return GetInt("ProperConsole.LogEntryMessageFontSize", 14);
            }
            set
            {
                SetInt("ProperConsole.LogEntryMessageFontSize", value);
                Save();
            }
        }

        internal void ResetLogEntryStackTraceFontSize()
        {
            Reset("ProperConsole.LogEntryStackTraceFontSize");
        }
        internal int LogEntryStackTraceFontSize
        {
            get
            {
                return GetInt("ProperConsole.LogEntryStackTraceFontSize", 12);
            }
            set
            {
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

        internal void ResetLogLevelFilter()
        {
            Reset("ProperConsole.LogLevelFilter");
        }
        internal LogLevel LogLevelFilter
        {
            get
            {
                return (LogLevel) GetInt("ProperConsole.LogLevelFilter", (int)LogLevel.All);
            }
            set
            {
                SetInt("ProperConsole.LogLevelFilter", (int)value);
                Save();
            }
        }

        internal void ResetcopyOnSelect()
        {
            Reset("ProperConsole.copyOnSelect");
        }
        internal bool CopyOnSelect
        {
            get
            {
                return GetBool("ProperConsole.copyOnSelect", false);
            }
            set
            {
                SetBool("ProperConsole.copyOnSelect", value);
                Save();
            }
        }

        internal void ResetshowContextNameInsteadOfStack()
        {
            Reset("ProperConsole.showContextNameInsteadOfStack");
        }
        internal bool ShowContextNameInsteadOfStack
        {
            get
            {
                return GetBool("ProperConsole.showContextNameInsteadOfStack", true);
            }
            set
            {
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