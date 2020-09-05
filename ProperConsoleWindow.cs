﻿using System.Collections;
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Linq;

namespace ProperLogger
{
    internal class ProperConsoleWindow : EditorWindow
    {
        #region Members
        #region Consts

        [SerializeField]
        private GUISkin m_skin = null;
        [NonSerialized]
        private float m_doubleClickSpeed = 300 * 10000; // Could be a config ?
        [NonSerialized]
        private float m_regexCompileDebounce = 200 * 10000;

        float m_itemHeight = 40;

        #endregion Consts

        #region Configs

        private ConfigsProvider m_configs = new EditorConfigs();

        private bool m_autoScroll = true;
        
        private bool m_searchMessage = true;

        #endregion Configs

        private bool m_needRegexRecompile = false; // TODO find region
        private DateTime m_lastRegexRecompile;

        #region Logs

        private List<ConsoleLogEntry> m_entries = null;
        private List<ConsoleLogEntry> m_filteredEntries = null;
        private bool m_triggerFilteredEntryComputation = false;
        private CustomLogHandler m_logHandler = null;
        private List<PendingContext> m_pendingContexts = null;
        private object m_entriesLock = null;
        private bool m_listening = false;

        // This could be a dictionnary, but Dictionnaries are not Unity-serializable which causes problems when switching Modes
        private int m_logCounter = 0;
        private int m_warningCounter = 0;
        private int m_errorCounter = 0;

        #region Filters

        private string m_searchString = null;
        private string[] m_searchWords = null;
        private List<LogCategory> m_inactiveCategories = null;

        #endregion Filters

        #endregion Logs

        #region Layout

        //private int m_selectedIndex = -1;
        private ConsoleLogEntry m_selectedEntry = null;
        private int m_displayedEntriesCount = -1;
        private DateTime m_lastClick = default;

        private Vector2 m_entryListScrollPosition;
        private Vector2 m_inspectorScrollPosition;

        private float m_splitterPosition = 0;
        private Rect m_splitterRect = default;
        private bool m_splitterDragging = false;
        private float m_innerScrollableHeight = 0;
        private float m_outerScrollableHeight = 0;

        private Rect m_showCategoriesButtonRect = default;

        #endregion Layout

        #region Loaded Textures

        private static Texture2D m_iconInfo;
        private static Texture2D m_iconWarning;
        private static Texture2D m_iconError;

        private static Texture2D m_iconInfoGray;
        private static Texture2D m_iconWarningGray;
        private static Texture2D m_iconErrorGray;

        #endregion Loaded Textures

        #region Caches

        //Reflection
        MethodInfo _loadIcon = null;
        MethodInfo LoadIcon
        {
            get
            {
                if(_loadIcon != null)
                {
                    return _loadIcon;
                }
                Type editorGuiUtility = typeof(EditorGUIUtility);
                _loadIcon = editorGuiUtility.GetMethod("LoadIcon", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic);
                return _loadIcon;
            }
        }

        private Regex m_searchRegex = null;

        #endregion Caches
        #endregion Members

        #region Properties
        private static ProperConsoleWindow m_instance = null;
        internal static ProperConsoleWindow Instance => m_instance;
        #endregion Properties

        #region Editor Window

        [MenuItem("Leonard/Console")]
        static void Init()
        {
            Debug.Log("Open window");
            // Get existing open window or if none, make a new one:
            if (ProperConsoleWindow.m_instance != null)
            {
                ProperConsoleWindow.m_instance.Show(true);
            }
            else
            {
                ShowWindow();
            }
        }

        public static void ShowWindow()
        {
            if (m_instance != null)
            {
                m_instance.Show(true);
                m_instance.Focus();
            }
            else
            {
                m_instance = ScriptableObject.CreateInstance<ProperConsoleWindow>();
                m_instance.Show(true);
                m_instance.Focus();
            }
        }

        private void OnEnable()
        {
            Debug.Log("OnEnable");
            m_entries = m_entries ?? new List<ConsoleLogEntry>();
            m_pendingContexts = m_pendingContexts ?? new List<PendingContext>();
            m_listening = false;
            m_entriesLock = new object();
            m_instance = this;
            m_triggerFilteredEntryComputation = true;
            EditorApplication.playModeStateChanged += ModeChanged;
            InitListener();
            LoadIcons();

            m_needRegexRecompile = true;
        }

        private void OnDisable()
        {
            Debug.Log("OnDisable");
            RemoveListener();
            EditorApplication.playModeStateChanged -= ModeChanged;
            m_instance = null;
        }

        public void OnBuild()
        {
            if (m_configs.ClearOnBuild)
            {
                Clear();
            }
        }

        private void LoadIcons()
        {
            m_iconInfo = (Texture2D)LoadIcon.Invoke(null, new object[] { "console.infoicon" });
            m_iconWarning = (Texture2D)LoadIcon.Invoke(null, new object[] { "console.warnicon" });
            m_iconError = (Texture2D)LoadIcon.Invoke(null, new object[] { "console.erroricon" });

            m_iconInfoGray = (Texture2D)LoadIcon.Invoke(null, new object[] { "console.infoicon.inactive.sml" });
            m_iconWarningGray = (Texture2D)LoadIcon.Invoke(null, new object[] { "console.warnicon.inactive.sml" });
            m_iconErrorGray = (Texture2D)LoadIcon.Invoke(null, new object[] { "console.erroricon.inactive.sml" });
        }

        private void HandleDoubleClick(ConsoleLogEntry entry) // TODO could this be used in play mode ?
        {
            if (!string.IsNullOrEmpty(entry.firstAsset))
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(entry.firstAsset);
                if (!string.IsNullOrEmpty(entry.firstLine))
                {
                    AssetDatabase.OpenAsset(asset, int.Parse(entry.firstLine));
                }
                else
                {
                    AssetDatabase.OpenAsset(asset);
                }
            }
        }

        #region Mode Changes

        private void ModeChanged(PlayModeStateChange obj)
        {
            Debug.Log(obj);
            switch (obj)
            {
                case PlayModeStateChange.ExitingPlayMode:
                    ExitingPlayMode();
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    EnteredEditMode();
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    ExitingEditMode();
                    break;
            }
        }

        private void ExitingEditMode()
        {
            if (m_configs.ClearOnPlay)
            {
                Clear();
            }
        }

        private void ExitingPlayMode()
        {
            RemoveListener();
            m_pendingContexts?.Clear();
        }

        private void EnteredEditMode()
        {
            InitListener();
            m_pendingContexts?.Clear();
        }

        #endregion Mode Changes

        #endregion Editor Window

        #region Logs

        public void InitListener()
        {
            if (!m_listening)
            {
                m_logHandler = new CustomLogHandler(Debug.unityLogger.logHandler, this);
                Debug.unityLogger.logHandler = m_logHandler;
                Application.logMessageReceivedThreaded += Listener;
                m_listening = true;
            }
        }

        public void RemoveListener()
        {
            Application.logMessageReceivedThreaded -= Listener;
            Debug.unityLogger.logHandler = m_logHandler.OriginalHandler;
            m_listening = false;
        }

        private void Listener(string condition, string stackTrace, LogType type)
        {
            lock (m_entriesLock)
            {
                UnityEngine.Object context = null;
                for (int i = 0; i < m_pendingContexts.Count; i++)
                {
                    if (m_pendingContexts[i].message.Equals(condition) && m_pendingContexts[i].logType == type)
                    {
                        context = m_pendingContexts[i].context;
                        m_pendingContexts.RemoveAt(i);
                        break;
                    }
                }

                Regex categoryParse = new Regex("\\[([^\\s\\[\\]]+)\\]"); // TODO cache
                List<LogCategory> categories = new List<LogCategory>();
                var categoryAsset = m_configs.CurrentCategoriesConfig;
                if (categoryAsset != null && categoryAsset.Categories != null && categoryAsset.Categories.Count > 0)
                {
                    foreach (Match match in categoryParse.Matches(condition))
                    {
                        foreach (var category in categoryAsset.Categories)
                        {
                            if(category.Name == match.Groups[1].Value && !categories.Contains(category))
                            {
                                categories.Add(category);
                                condition = condition.Replace($"[{category.Name}] ", "");
                            }
                        }
                    }
                }

                var now = DateTime.Now;
                string newStackTrace = ParseStackTrace(stackTrace, out string firstAsset, out string firstLine);
                m_entries.Add(new ConsoleLogEntry()
                {
                    date = now.Ticks,
                    timestamp = now.ToString("T", DateTimeFormatInfo.InvariantInfo),
                    level = Utils.GetLogLevelFromUnityLogType(type),
                    message = condition,
                    messageFirstLine = GetFirstLine(condition, false),
                    stackTrace = newStackTrace,
                    count = 1,
                    context = context,
                    firstAsset = firstAsset,
                    firstLine = firstLine,
                    categories = categories,
                });

                switch (type)
                {
                    case LogType.Log:
                        m_logCounter++;
                        break;
                    case LogType.Warning:
                        m_warningCounter++;
                        break;
                    case LogType.Error:
                    case LogType.Exception:
                    case LogType.Assert:
                        m_errorCounter++;
                        break;
                }
            }

            m_triggerFilteredEntryComputation = true;

            this.Repaint();
            if (m_configs.ErrorPause && (type == LogType.Assert || type == LogType.Error || type == LogType.Exception))
            {
                Debug.Break();
            }
        }

        public void ContextListener(LogType type, UnityEngine.Object context, string format, params object[] args)
        {
            m_pendingContexts = m_pendingContexts ?? new List<PendingContext>();
            if (context != null && args.Length > 0)
            {
                m_pendingContexts.Add(new PendingContext()
                {
                    logType = type,
                    context = context,
                    message = args[0] as string
                });
            }
        }

        private void Clear()
        {
            lock (m_entriesLock)
            {
                m_logCounter = 0;
                m_warningCounter = 0;
                m_errorCounter = 0;
                m_entries.Clear();
                m_pendingContexts.Clear();
                m_selectedEntry = null;
            }
            m_triggerFilteredEntryComputation = true;
        }

        #endregion Logs

        #region Search

        private bool ValidFilter(ConsoleLogEntry e)
        {
            bool valid = true;

            // Log Level
            if (m_configs.LogLevelFilter != LogLevel.All)
            {
                valid &= (e.level & m_configs.LogLevelFilter) == e.level;
                if (!valid)
                {
                    return false;
                }
            }

            // Text Search
            string searchableText = (m_searchMessage ? e.message : "") + (m_configs.SearchInStackTrace ? e.stackTrace : "") + ((m_configs.SearchObjectName && e.context != null) ? e.context.name : ""); // TODO opti
            if (m_configs.RegexSearch)
            {
                if(m_searchRegex != null)
                {
                    valid &= m_searchRegex.IsMatch(searchableText);
                }
            }
            else
            {
                if (m_searchWords != null && m_searchWords.Length > 0)
                {
                    valid &= m_searchWords.All(p => searchableText.IndexOf(p, m_configs.CaseSensitive ? StringComparison.Ordinal : System.StringComparison.OrdinalIgnoreCase) >= 0);
                    if (!valid)
                    {
                        return false;
                    }
                }
            }

            // Categories
            if (m_inactiveCategories.Count > 0)
            {
                valid &= m_inactiveCategories.Intersect(e.categories).Count() == 0;
                if (!valid)
                {
                    return false;
                }
            }

            return valid;
        }

        #endregion Search

        #region GUI

        void OnGUI()
        {
            bool callForRepaint = false;
            bool repaint = Event.current.type == EventType.Repaint;

            m_inactiveCategories?.Clear();
            m_inactiveCategories = m_configs.InactiveCategories;

            DisplayToolbar(ref callForRepaint);

            if (m_configs.AdvancedSearchToolbar)
            {
                DisplaySearchToolbar();
            }

            Rect windowRect = GUILayoutUtility.GetLastRect();

            float startY = 0;
            float totalWidth = Screen.width;
            GUILayout.Space(1);
            if (repaint)
            {
                Rect r = GUILayoutUtility.GetLastRect();
                startY = r.yMax;
            }

            if (m_configs.InspectorOnTheRight)
            {
                GUILayout.BeginHorizontal();
            }

            #region DisplayList
            GUILayout.BeginVertical(); // Display list
            m_entryListScrollPosition = GUILayout.BeginScrollView(m_entryListScrollPosition, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);

            if (repaint)
            {
                float scrollTolerance = 0;
                m_autoScroll = m_entryListScrollPosition.y >= (m_innerScrollableHeight - m_outerScrollableHeight - scrollTolerance + startY);
            }

            GUILayout.BeginVertical();

            if (m_entries.Count == 0) GUILayout.Space(10);

            if (m_triggerFilteredEntryComputation)
            {
                m_filteredEntries = m_entries.FindAll(e => ValidFilter(e));
                m_triggerFilteredEntryComputation = false;
            }

            List<ConsoleLogEntry> displayedEntries;
            if (m_configs.Collapse)
            {
                DisplayCollapse(m_filteredEntries, out displayedEntries, totalWidth);
            }
            else
            {
                DisplayList(m_filteredEntries, out displayedEntries, totalWidth);
            }

            if (displayedEntries.Count < m_displayedEntriesCount)
            {
                m_selectedEntry = null;
            }
            m_displayedEntriesCount = displayedEntries.Count;

            GUILayout.EndVertical();

            if (repaint)
            {
                Rect r = GUILayoutUtility.GetLastRect();
                m_innerScrollableHeight = r.yMax;
            }

            GUILayout.EndScrollView();

            GUILayout.Space(1);
            if (repaint)
            {
                Rect r = GUILayoutUtility.GetLastRect();
                m_outerScrollableHeight = r.yMin;
            }

            if (repaint && m_autoScroll)
            {
                m_entryListScrollPosition.y = m_innerScrollableHeight - m_outerScrollableHeight + startY;
            }
            GUILayout.EndVertical(); // Display list
            #endregion DisplayList

            #region Inspector
            if (m_configs.InspectorOnTheRight)
            {
                GUILayout.BeginHorizontal(); // Inspector
            }
            else
            {
                GUILayout.BeginVertical(); // Inspector
            }

            m_splitterPosition = Mathf.Clamp(m_splitterPosition, 100, (m_configs.InspectorOnTheRight ? Screen.width : Screen.height) - 200);

            Splitter();

            if (m_configs.InspectorOnTheRight)
            {
                GUILayout.BeginVertical(GUILayout.Width(m_splitterPosition),
                GUILayout.MaxWidth(m_splitterPosition),
                GUILayout.MinWidth(m_splitterPosition));
                m_inspectorScrollPosition = GUILayout.BeginScrollView(m_inspectorScrollPosition);
            }
            else
            {
                GUILayout.BeginVertical(GUILayout.Height(m_splitterPosition),
                GUILayout.MaxHeight(m_splitterPosition),
                GUILayout.MinHeight(m_splitterPosition));
                m_inspectorScrollPosition = GUILayout.BeginScrollView(m_inspectorScrollPosition);
            }
            if (m_selectedEntry != null)
            {
                GUIStyle textStyle = new GUIStyle(GUI.skin.label);
                textStyle.richText = true;
                textStyle.normal.textColor = Color.black;
                textStyle.fontSize = m_configs.InspectorMessageFontSize;
                textStyle.wordWrap = true;
                textStyle.stretchWidth = false;
                textStyle.clipping = TextClipping.Clip;

                var entry = m_selectedEntry;

                GUILayout.Space(1);
                float currentX = (GUILayoutUtility.GetLastRect()).xMin;

                string categoriesString = "";
                if (entry.categories != null && entry.categories.Count > 0)
                {
                    if (m_configs.CategoryDisplay.HasFlag(ECategoryDisplay.InInspector))
                    {
                        string format = "<color=#{1}>[{0}]</color> ";
                        categoriesString = string.Join("", entry.categories.Select(c =>string.Format(format, c.Name, ColorUtility.ToHtmlStringRGB(Color.Lerp(c.Color, textStyle.normal.textColor, m_configs.CategoryNameColorize)))));
                    }
                }

                EditorSelectableLabel($"{categoriesString}{entry.message}", textStyle, currentX); // TODO if editor
                                                                           //GUILayout.Label($"{entry.message}", textStyle); // TODO if not editor
                if (entry.context != null)
                {
                    EditorSelectableLabel(entry.context.name, textStyle, currentX); // TODO if editor// TODO if not editor
                }
                if (!string.IsNullOrEmpty(entry.stackTrace))
                {
                    EditorSelectableLabel(entry.stackTrace, textStyle, currentX); // TODO if editor// TODO if not editor
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            if (m_configs.InspectorOnTheRight)
            {
                GUILayout.EndHorizontal(); // Inspector
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.EndVertical(); // Inspector
            }
            #endregion Inspector

            #region Debug Buttons
            if (GUILayout.Button("Log"))
            {
                Debug.Log($"Log {DateTime.Now.ToString()} {m_listening}", Camera.main);
            }

            if (GUILayout.Button("Log Combat"))
            {
                Debug.Log($"[Combat] Log {DateTime.Now.ToString()} {m_listening}", Camera.main);
            }

            if (GUILayout.Button("Log Performance"))
            {
                Debug.Log($"[Performance] [Combat] Log {DateTime.Now.ToString()} {m_listening}", Camera.main);
            }

            if (GUILayout.Button("LogWarning"))
            {
                Debug.LogWarning($"Warning {DateTime.Now.ToString()} {m_listening} {m_autoScroll}");
            }

            if (GUILayout.Button("LogError"))
            {
                Debug.LogError("Error");
            }

            if (GUILayout.Button("LogAssert"))
            {
                Debug.Assert(false);
            }

            if (GUILayout.Button("Add 1000 Log"))
            {
                for (int i = 0; i < 1000; i++)
                {
                    Debug.Log($"Log {DateTime.Now.ToString()} {m_listening}");
                }
            }
            #endregion Debug Buttons

            if (Event.current != null)
            {
                switch (Event.current.rawType)
                {
                    case EventType.MouseDown:
                        if (m_splitterRect.Contains(Event.current.mousePosition))
                        {
                            //Debug.Log("Start dragging");
                            m_splitterDragging = true;
                        }
                        break;
                    case EventType.MouseDrag:
                        if (m_splitterDragging)
                        {
                            //Debug.Log("moving splitter");
                            m_splitterPosition -= m_configs.InspectorOnTheRight ? Event.current.delta.x : Event.current.delta.y;
                            Repaint();
                        }
                        break;
                    case EventType.MouseUp:
                        if (m_splitterDragging)
                        {
                            //Debug.Log("Done dragging");
                            m_splitterDragging = false;
                        }
                        break;
                    case EventType.MouseMove:
                        if (m_splitterRect.Contains(Event.current.mousePosition))
                        {
                        }
                        else if (!m_splitterDragging)
                        {

                        }
                        break;
                }
            }

            if (callForRepaint)
            {
                Repaint();
            }
        }

        private void DisplayList(List<ConsoleLogEntry> filteredEntries, out List<ConsoleLogEntry> displayedEntries, float totalWidth)
        {
            for (int i = 0; i < filteredEntries.Count; i++)
            {
                DisplayEntry(filteredEntries[i], i, totalWidth);
            }
            displayedEntries = filteredEntries;
        }
        private void DisplayCollapse(List<ConsoleLogEntry> filteredEntries, out List<ConsoleLogEntry> displayedEntries, float totalWidth)
        {
            List<ConsoleLogEntry> collapsedEntries = new List<ConsoleLogEntry>();

            for (int i = 0; i < filteredEntries.Count; i++)
            {
                bool found = false;
                int foundIdx = 0;
                for (int j = 0; j < collapsedEntries.Count; j++)
                {
                    if (collapsedEntries[j].message == filteredEntries[i].message)
                    {
                        foundIdx = j;
                        found = true;
                    }
                }
                if (found)
                {
                    collapsedEntries[foundIdx] = new ConsoleLogEntry()
                    {
                        count = collapsedEntries[foundIdx].count + 1,
                        date = collapsedEntries[foundIdx].date,
                        message = collapsedEntries[foundIdx].message,
                        level = collapsedEntries[foundIdx].level,
                        stackTrace = collapsedEntries[foundIdx].stackTrace,
                        timestamp = collapsedEntries[foundIdx].timestamp,
                        messageFirstLine = collapsedEntries[foundIdx].messageFirstLine,
                        categories = collapsedEntries[foundIdx].categories,
                    };
                }
                else
                {
                    collapsedEntries.Add(filteredEntries[i]);
                }
            }

            for (int i = 0; i < collapsedEntries.Count; i++)
            {
                DisplayEntry(collapsedEntries[i], i, totalWidth);
            }

            displayedEntries = collapsedEntries;
        }

        #region GUI Components

        private void DisplayToolbar(ref bool callForRepaint)
        {
            GUILayout.BeginHorizontal("Toolbar");
            if (GUILayout.Button("Clear", "ToolbarButton", GUILayout.ExpandWidth(false)))
            {
                Clear();
                GUIUtility.keyboardControl = 0;
            }
            bool lastCollapse = m_configs.Collapse;
            m_configs.Collapse = GUILayout.Toggle(m_configs.Collapse, "Collapse", "ToolbarButton", GUILayout.ExpandWidth(false));
            callForRepaint = m_configs.Collapse != lastCollapse;
            if (m_configs.Collapse != lastCollapse)
            {
                m_selectedEntry = null;
            }
            m_configs.ClearOnPlay = GUILayout.Toggle(m_configs.ClearOnPlay, "Clear on Play", "ToolbarButton", GUILayout.ExpandWidth(false));
            m_configs.ClearOnBuild = GUILayout.Toggle(m_configs.ClearOnBuild, "Clear on Build", "ToolbarButton", GUILayout.ExpandWidth(false));
            m_configs.ErrorPause = GUILayout.Toggle(m_configs.ErrorPause, "Error Pause", "ToolbarButton", GUILayout.ExpandWidth(false));

            string lastSearchTerm = m_searchString;
            m_searchString = GUILayout.TextField(m_searchString, "ToolbarSeachTextField");
            if (lastSearchTerm != m_searchString)
            {
                m_triggerFilteredEntryComputation = true;
                if (m_configs.RegexSearch)
                {
                    m_lastRegexRecompile = DateTime.Now;
                    m_needRegexRecompile = true;
                }
                m_searchWords = m_searchString.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            }

            m_configs.AdvancedSearchToolbar = GUILayout.Toggle(m_configs.AdvancedSearchToolbar, "S", "ToolbarButton", GUILayout.ExpandWidth(false));

            if(GUILayout.Button("Categories", "ToolbarButton", GUILayout.ExpandWidth(false)))
            {
                Vector2 dropdownOffset = new Vector2(10, 10);
                Rect dropDownPosition = new Rect(Event.current.mousePosition.x + this.position.x, Event.current.mousePosition.y + this.position.y, dropdownOffset.x, m_showCategoriesButtonRect.height + dropdownOffset.y);

                var categoriesAsset = m_configs.CurrentCategoriesConfig;
                Vector2 size = new Vector2(250, 150);
                if (categoriesAsset != null)
                {
                    int categoriesCount = m_configs.CurrentCategoriesConfig.Categories == null ? 0 : m_configs.CurrentCategoriesConfig.Categories.Count;
                    size.y = (categoriesCount) * 20; // TODO put this somewhere in a style
                }
                Debug.Log("Open dropdown");
                // Get existing open window or if none, make a new one:
                var window = (CategoriesFilterWindow)EditorWindow.CreateInstance<CategoriesFilterWindow>();
                window.ShowAsDropDown(dropDownPosition, size);
                window.Repaint();
            }
            if (Event.current.type == EventType.Repaint) m_showCategoriesButtonRect = GUILayoutUtility.GetLastRect();

            // Log Level Flags
            FlagButton(LogLevel.Log, m_iconInfo, m_iconInfoGray);
            FlagButton(LogLevel.Warning, m_iconWarning, m_iconWarningGray);
            FlagButton(LogLevel.Error, m_iconError, m_iconErrorGray);

            GUILayout.EndHorizontal();
        }

        private void DisplaySearchToolbar()
        {
            GUILayout.BeginHorizontal("Toolbar");
            bool lastRegexSearch = m_configs.RegexSearch;
            m_configs.RegexSearch = GUILayout.Toggle(m_configs.RegexSearch, "Regex Search", "ToolbarButton", GUILayout.ExpandWidth(false));
            if(lastRegexSearch != m_configs.RegexSearch)
            {
                m_needRegexRecompile = true;
            }
            bool lastCaseSensitive = m_configs.CaseSensitive;
            m_configs.CaseSensitive = GUILayout.Toggle(m_configs.CaseSensitive, "Case Sensitive", "ToolbarButton", GUILayout.ExpandWidth(false));
            if (lastCaseSensitive != m_configs.CaseSensitive)
            {
                m_triggerFilteredEntryComputation = true;
                m_needRegexRecompile = true;
            }
            bool lastSearchMessage = m_searchMessage;
            m_searchMessage = GUILayout.Toggle(m_searchMessage, "Search in Log Message", "ToolbarButton", GUILayout.ExpandWidth(false));
            if (lastSearchMessage != m_searchMessage)
            {
                m_triggerFilteredEntryComputation = true;
            }
            bool lastSearchObjectName = m_configs.SearchObjectName;
            m_configs.SearchObjectName = GUILayout.Toggle(m_configs.SearchObjectName, "Search in Object Name", "ToolbarButton", GUILayout.ExpandWidth(false));
            if (lastSearchObjectName != m_configs.SearchObjectName)
            {
                m_triggerFilteredEntryComputation = true;
            }
            bool lastSearchStackTRace = m_configs.SearchInStackTrace;
            m_configs.SearchInStackTrace = GUILayout.Toggle(m_configs.SearchInStackTrace, "Search in Stack Trace", "ToolbarButton", GUILayout.ExpandWidth(false));
            if (lastSearchStackTRace != m_configs.SearchInStackTrace)
            {
                m_triggerFilteredEntryComputation = true;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DisplayEntry(ConsoleLogEntry entry, int idx, float totalWidth)
        {
            // Only show entries that are in view
            if ((idx < (m_entryListScrollPosition.y / m_itemHeight) - 1)
              || idx > ((m_entryListScrollPosition.y + m_outerScrollableHeight) / m_itemHeight))
            {
                GUILayout.Space(m_itemHeight);
                return;
            }

            bool repaint = Event.current.type == EventType.Repaint;
            GUIStyle currentStyle = m_skin.FindStyle("OddEntry");
            GUIStyle textStyle = new GUIStyle(m_skin.FindStyle("EntryLabel")); // Cache styles
            textStyle.normal.textColor = GUI.skin.label.normal.textColor;

            float imageSize = 35;
            float sidePaddings = 10;
            float collapseBubbleSize = m_configs.Collapse ? (40 - sidePaddings) : 0; // Globally accessible ?
            float empiricalPaddings = 30 + sidePaddings;

            bool displayCategoryNameInColumn = m_configs.CategoryDisplay.HasFlag(ECategoryDisplay.NameColumn);
            bool displayCategoryIconInColumn = m_configs.CategoryDisplay.HasFlag(ECategoryDisplay.Icon);
            bool displayCategoryStrips = m_configs.CategoryDisplay.HasFlag(ECategoryDisplay.ColorStrip);
            bool categoryColumn = displayCategoryNameInColumn || displayCategoryIconInColumn;
            float categoryColumnWidth = 0;

            float categoryStripWidth = m_configs.ColorStripWidth;
            float categoriesStripsTotalWidth = 0;

            float rightSplitterWidth = m_configs.InspectorOnTheRight ? m_splitterPosition : 0;

            GUIStyle categoryNameStyle = new GUIStyle(textStyle);
            categoryNameStyle.alignment = TextAnchor.MiddleCenter;
            categoryNameStyle.fontSize = m_configs.LogEntryStackTraceFontSize;
            categoryNameStyle.padding.top = (int)((m_itemHeight / 2f) - categoryNameStyle.fontSize);
            categoryNameStyle.fontStyle = FontStyle.Bold;

            string categoriesString = "";

            if (entry.categories != null && entry.categories.Count > 0)
            {
                if (categoryColumn)
                {
                    var category = entry.categories[0];
                    categoryColumnWidth = categoryNameStyle.CalcSize(new GUIContent(category.Name)).x + 10;
                }
                if (displayCategoryStrips)
                {
                    categoriesStripsTotalWidth = entry.categories.Count * categoryStripWidth;
                }
                if (m_configs.CategoryDisplay.HasFlag(ECategoryDisplay.InMessage))
                {
                    string format = "<color=#{1}>[{0}]</color> ";
                    categoriesString = string.Join("", entry.categories.Select(c => string.Format(format, c.Name, ColorUtility.ToHtmlStringRGB(Color.Lerp(c.Color, textStyle.normal.textColor, m_configs.CategoryNameColorize)))));
                }
            }

            float entrywidth = totalWidth - imageSize - collapseBubbleSize - categoryColumnWidth - empiricalPaddings - rightSplitterWidth - categoriesStripsTotalWidth;

            
            if (entry == m_selectedEntry)
            {
                currentStyle = m_skin.FindStyle("SelectedEntry");
                textStyle = m_skin.FindStyle("EntryLabelSelected"); // Cache styles
                categoryNameStyle.normal.textColor = textStyle.normal.textColor;
            }
            else if (idx % 2 == 0)
            {
                currentStyle = m_skin.FindStyle("EvenEntry"); // Cache styles
            }

            GUILayout.BeginHorizontal(currentStyle, GUILayout.Height(m_itemHeight));
            //GUI.color = saveColor;
            // Picto space
            GUILayout.BeginHorizontal(GUILayout.Width(imageSize + sidePaddings));
            GUILayout.FlexibleSpace();
            GUILayout.Box(GetEntryIcon(entry), GUIStyle.none, GUILayout.Width(imageSize), GUILayout.Height(imageSize));
            GUILayout.EndHorizontal();
            // Text space
            GUILayout.BeginVertical();
            textStyle.fontSize = m_configs.LogEntryMessageFontSize;
            GUILayout.Label($"[{entry.timestamp}] {categoriesString}{entry.messageFirstLine}", textStyle, GUILayout.Width(entrywidth));
            textStyle.fontSize = m_configs.LogEntryStackTraceFontSize;
            if(m_configs.ShowContextNameInsteadOfStack && entry.context != null)
            {
                GUILayout.Label($"{entry.context.name}", textStyle, GUILayout.Width(entrywidth));
            }
            else if (!string.IsNullOrEmpty(entry.stackTrace))
            {
                GUILayout.Label($"{GetFirstLine(entry.stackTrace, true)}", textStyle, GUILayout.Width(entrywidth)); // TODO cache this line
            }
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            // First Category space
            if (categoryColumn && entry.categories != null && entry.categories.Count > 0)
            {
                var category = entry.categories[0];
                if (displayCategoryNameInColumn)
                {
                    categoryNameStyle.normal.textColor = Color.Lerp(category.Color, categoryNameStyle.normal.textColor, m_configs.CategoryNameInLogListColorize);
                    GUILayout.Label($"{category.Name}", categoryNameStyle, GUILayout.Width(categoryColumnWidth));
                }
                /*
                if (displayCategoryIconInColumn && category.Icon != null)
                {
                    //GUILayout.Box(category.Icon, GUILayout.Width(categoryColumnWidth - 20));
                }
                */
            }
            // Collapse Space
            if (m_configs.Collapse)
            {
                DisplayCollapseBubble(entry.level, entry.count, collapseBubbleSize, sidePaddings);
            }
            // Category strips space
            if (displayCategoryStrips && entry.categories != null && entry.categories.Count > 0)
            {
                Rect lastRect = GUILayoutUtility.GetLastRect();
                Color saveColor = GUI.color;
                Color saveContentColor = GUI.contentColor;
                Color saveBGColor = GUI.backgroundColor;
                int i = 0;
                GUIStyle boxStyle = new GUIStyle(m_skin.FindStyle("CategoryColorStrip"));
                foreach (var category in entry.categories)
                {
                    GUI.color = category.Color;
                    GUI.backgroundColor = Color.white;
                    GUI.contentColor = Color.white;
                    GUI.Box(new Rect(lastRect.xMax + i* categoryStripWidth, lastRect.yMin - 4, categoryStripWidth, m_itemHeight), "", boxStyle);
                    GUILayout.Space(categoryStripWidth);
                    i++;
                }
                GUI.contentColor = saveContentColor;
                GUI.backgroundColor = saveBGColor;
                GUI.color = saveColor;
            }

            GUILayout.EndHorizontal();

            Rect r = GUILayoutUtility.GetLastRect();
            if (GUI.Button(r, GUIContent.none, GUIStyle.none))
            {
                if (entry.context != null)
                {
                    EditorGUIUtility.PingObject(entry.context); // TODO Editor
                }
                if (m_selectedEntry == entry && DateTime.Now.Ticks - m_lastClick.Ticks < m_doubleClickSpeed)
                {
                    HandleDoubleClick(entry);
                }
                m_selectedEntry = entry;
                m_lastClick = DateTime.Now;
            }
        }

        private void DisplayCollapseBubble(LogLevel level, int count, float collapseBubbleSize, float sidePaddings)
        {
            // TODO cache FindStyle
            GUIStyle style;
            switch (level)
            {
                case LogLevel.Log:
                    style = m_skin.FindStyle("CollapseBubble");
                    break;
                case LogLevel.Warning:
                    style = m_skin.FindStyle("CollapseBubbleWarning");
                    break;
                case LogLevel.Error:
                default:
                    style = m_skin.FindStyle("CollapseBubbleError");
                    break;
            }
            GUILayout.Label($"{count}", style, GUILayout.ExpandWidth(false), GUILayout.Width(collapseBubbleSize), GUILayout.Height(23)); // TODO style
            GUILayout.Space(sidePaddings);
        }

        private void EditorSelectableLabel(string text, GUIStyle textStyle, float currentX)
        {
            float width = m_configs.InspectorOnTheRight ? m_splitterPosition : EditorGUIUtility.currentViewWidth;
            var content = new GUIContent(text);
            float height = textStyle.CalcHeight(content, width);
            var lastRect = GUILayoutUtility.GetLastRect();
            EditorGUI.SelectableLabel(new Rect(currentX, lastRect.yMax, width, height), text, textStyle);
            GUILayout.Space(height);
        }
        
        private void FlagButton(LogLevel level, Texture2D icon, Texture2D iconGray)
        {
            bool hasFlag = (m_configs.LogLevelFilter & level) != 0;
            int counter = GetCounter(level);

            bool newFlagValue = GUILayout.Toggle(hasFlag, new GUIContent($" {(counter > 999 ? "999+" : counter.ToString())}", (counter > 0 ? icon : iconGray)),
                (GUIStyle)"ToolbarButton"
                , GUILayout.MaxWidth(GetFlagButtonWidthFromCounter(counter)), GUILayout.ExpandWidth(false)
                );
            if (hasFlag != newFlagValue)
            {
                m_configs.LogLevelFilter ^= level;
                m_triggerFilteredEntryComputation = true;
            }
        }

        private void Splitter()
        {
            float splitterSize = 10f;
            if (m_configs.InspectorOnTheRight)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space((int)(splitterSize / 2f));
                GUILayout.Box("",
                     GUILayout.Width(1),
                     GUILayout.MaxWidth(1),
                     GUILayout.MinWidth(1),
                     GUILayout.ExpandHeight(true));
                GUILayout.Space((int)(splitterSize / 2f));
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.BeginVertical();
                GUILayout.Space((int)(splitterSize / 2f));
                GUILayout.Box("",
                     GUILayout.Height(1),
                     GUILayout.MaxHeight(1),
                     GUILayout.MinHeight(1),
                     GUILayout.ExpandWidth(true));
                GUILayout.Space((int)(splitterSize / 2f));
                GUILayout.EndVertical();
            }

            m_splitterRect = GUILayoutUtility.GetLastRect();
            EditorGUIUtility.AddCursorRect(new Rect(m_splitterRect), m_configs.InspectorOnTheRight ? MouseCursor.ResizeHorizontal : MouseCursor.ResizeVertical); // TODO Editor
        }

        #endregion GUI Components

        #endregion GUI

        #region Utilities

        private int GetFlagButtonWidthFromCounter(int counter)
        {
            if (counter >= 1000)
            {
                return 60;
            }
            else if (counter >= 100)
            {
                return 52;
            }
            else if (counter >= 10)
            {
                return 43;
            }
            else
            {
                return 38;
            }
        }

        private int GetCounter(LogLevel level)
        {
            if (level.HasFlag(LogLevel.Log)) { return m_logCounter; }
            if (level.HasFlag(LogLevel.Warning)) { return m_warningCounter; }
            return m_errorCounter;
        }
        private Texture GetEntryIcon(ConsoleLogEntry entry)
        {
            if (entry.level.HasFlag(LogLevel.Log)) { return m_iconInfo; }
            if (entry.level.HasFlag(LogLevel.Warning)) { return m_iconWarning; }
            return m_iconError;
        }

        #region Text Manipulation

        private string GetFirstLine(string text, bool isCallStack)
        {
            var split = text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length == 0)
            {
                return "";
            }
            if (isCallStack && split.Length > 1)
            {
                return split[1];
            }
            return split[0];
        }
        private string ParseStackTrace(string stackTrace, out string firstAsset, out string firstLine)
        {
            firstAsset = null;
            firstLine = null;
            if (string.IsNullOrEmpty(stackTrace))
            {
                return null;
            }

            var split = stackTrace.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            string result = "";

            Regex scriptMatch = new Regex("^((.+)[:\\.](.+)(\\s?\\(.*\\))\\s?)\\(at\\s([a-zA-Z0-9\\-_\\.\\/]+)\\:(\\d+)\\)", RegexOptions.IgnoreCase); // TODO cache

            for (int i = 0; i < split.Length; i++)
            {
                Match m = scriptMatch.Match(split[i]);
                if (m.Success)
                {
                    List<string> groups = new List<string>();
                    for(int k=0;k<m.Groups.Count;k++)
                    {
                        groups.Add(m.Groups[k].Value);
                    }

                    bool isHidden = false;
                    try
                    {
                        /*if (m.Groups[2].Value == typeof(CustomLogHandler).FullName)
                        {
                            result = "";
                            continue;
                        }*/ // TODO uncomment

                        Type type = Type.GetType(m.Groups[2].Value);
                        MethodInfo method = type.GetMethod(m.Groups[3].Value, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Instance);
                        isHidden = method.GetCustomAttribute<HideInCallStackAttribute>() != null;
                    }
                    catch (Exception) { }

                    if (isHidden)
                    {
                        continue;
                    }
                    result += split[i].Replace(m.Value, $"{m.Groups[1].Value}(at <a href=\"{ m.Groups[5].Value }\" line=\"{ m.Groups[6].Value }\">{ m.Groups[5].Value }:{ m.Groups[6].Value }</a>)") + "\n";

                    if (string.IsNullOrEmpty(firstAsset))
                    {
                        firstAsset = m.Groups[5].Value;
                        firstLine = m.Groups[6].Value;
                    }
                }
                else
                {
                    result += $"{split[i]}\n";
                }
            }

            return result;
        }

        #endregion Text Manipulation

        #endregion Utilities

        private void Update()
        {
            if (m_configs.RegexSearch && string.IsNullOrEmpty(m_searchString))
            {
                m_searchRegex = null;
            }
            else if (m_configs.RegexSearch && m_needRegexRecompile && DateTime.Now.Ticks - m_lastRegexRecompile.Ticks > m_regexCompileDebounce)
            {
                m_needRegexRecompile = false;
                m_lastRegexRecompile = DateTime.Now;
                m_triggerFilteredEntryComputation = true;
                if (m_configs.CaseSensitive)
                {
                    m_searchRegex = new Regex(m_searchString.Trim());
                }
                else
                {
                    m_searchRegex = new Regex(m_searchString.Trim(), RegexOptions.IgnoreCase);
                }
            }
            // TODO code below will not execute if regex compilation failed
        }
    }
}