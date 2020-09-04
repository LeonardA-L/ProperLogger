using System.Collections;
using System.Collections.Generic;
using UnityEditor;
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

        internal int LogEntryMessageFontSize
        {
            get
            {
                return GetInt("ProperConsole.LogEntryMessageFontSize", 14);
            }
            set
            {
                SetInt("ProperConsole.LogEntryMessageFontSize", value);
            }
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
            }
        }

        internal int InspectorMessageFontSize
        {
            get
            {
                return GetInt("ProperConsole.InspectorMessageFontSize", 12);
            } set
            {
                SetInt("ProperConsole.InspectorMessageFontSize", value);
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
            }
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
            }
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

    internal class EditorConfigs : ConfigsProvider
    {
        protected override bool GetBool(string key, bool defaultValue)
        {
            return EditorPrefs.GetBool(key, defaultValue);
        }

        protected override float GetFloat(string key, float defaultValue)
        {
            return EditorPrefs.GetFloat(key, defaultValue);
        }

        protected override int GetInt(string key, int defaultValue)
        {
            return EditorPrefs.GetInt(key, defaultValue);
        }

        protected override string GetString(string key, string defaultValue)
        {
            return EditorPrefs.GetString(key, defaultValue);
        }

        protected override void Save()
        {
        }

        protected override void SetBool(string key, bool newValue)
        {
            EditorPrefs.SetBool(key, newValue);
        }

        protected override void SetFloat(string key, float newValue)
        {
            EditorPrefs.SetFloat(key, newValue);
        }

        protected override void SetInt(string key, int newValue)
        {
            EditorPrefs.SetInt(key, newValue);
        }

        protected override void SetString(string key, string newValue)
        {
            EditorPrefs.SetString(key, newValue);
        }
    }

    internal class PlayerConfigs : ConfigsProvider
    {
        protected override bool GetBool(string key, bool defaultValue)
        {
            return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;
        }

        protected override float GetFloat(string key, float defaultValue)
        {
            return PlayerPrefs.GetFloat(key, defaultValue);
        }

        protected override int GetInt(string key, int defaultValue)
        {
            return PlayerPrefs.GetInt(key, defaultValue);
        }

        protected override string GetString(string key, string defaultValue)
        {
            return PlayerPrefs.GetString(key, defaultValue);
        }

        protected override void Save()
        {
            PlayerPrefs.Save();
        }

        protected override void SetBool(string key, bool newValue)
        {
            PlayerPrefs.SetInt(key, newValue ? 1 : 0);
        }

        protected override void SetFloat(string key, float newValue)
        {
            PlayerPrefs.SetFloat(key, newValue);
        }

        protected override void SetInt(string key, int newValue)
        {
            PlayerPrefs.SetInt(key, newValue);
        }

        protected override void SetString(string key, string newValue)
        {
            PlayerPrefs.SetString(key, newValue);
        }
    }
}