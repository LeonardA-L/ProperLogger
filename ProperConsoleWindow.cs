using System.Collections;
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
        private LogLevel m_logLevelFilter = LogLevel.All;

        #endregion Filters

        #endregion Logs

        #region Layout

        private int m_selectedIndex = -1;
        private int m_displayedEntriesCount = -1;
        private DateTime m_lastClick = default;

        private Vector2 m_entryListScrollPosition;
        private Vector2 m_inspectorScrollPosition;

        private float m_splitterPosition = 0;
        private Rect m_splitterRect = default;
        private bool m_splitterDragging = false;
        private float m_innerScrollableHeight = 0;
        private float m_outerScrollableHeight = 0;

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
                m_selectedIndex = -1;
            }
        }

        #endregion Logs

        #region Search

        private bool ValidFilter(ConsoleLogEntry e)
        {
            bool valid = true;

            if (m_logLevelFilter != LogLevel.All)
            {
                valid &= (e.level & m_logLevelFilter) == e.level;
                if (!valid)
                {
                    return false;
                }
            }

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
                if (!string.IsNullOrEmpty(m_searchString))
                {
                    string[] searchWords = m_searchString.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); // TODO opti
                    valid &= searchWords.All(p => searchableText.IndexOf(p, m_configs.CaseSensitive ? StringComparison.Ordinal : System.StringComparison.OrdinalIgnoreCase) >= 0);
                    if (!valid)
                    {
                        return false;
                    }
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

            var filteredEntries = m_entries.FindAll(e => ValidFilter(e));
            List<ConsoleLogEntry> displayedEntries;
            if (m_configs.Collapse)
            {
                DisplayCollapse(filteredEntries, out displayedEntries, totalWidth);
            }
            else
            {
                DisplayList(filteredEntries, out displayedEntries, totalWidth);
            }

            if (displayedEntries.Count != m_displayedEntriesCount)
            {
                m_selectedIndex = -1;
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
            if (m_selectedIndex != -1)
            {
                GUIStyle textStyle = GUI.skin.label;
                textStyle.richText = true;
                textStyle.normal.textColor = Color.black;

                var entry = displayedEntries[m_selectedIndex];

                GUILayout.Space(1);
                float currentX = (GUILayoutUtility.GetLastRect()).xMin;

                EditorSelectableLabel(entry.message, textStyle, currentX); // TODO if editor
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
                m_selectedIndex = -1;
            }
            m_configs.ClearOnPlay = GUILayout.Toggle(m_configs.ClearOnPlay, "Clear on Play", "ToolbarButton", GUILayout.ExpandWidth(false));
            m_configs.ClearOnBuild = GUILayout.Toggle(m_configs.ClearOnBuild, "Clear on Build", "ToolbarButton", GUILayout.ExpandWidth(false));
            m_configs.ErrorPause = GUILayout.Toggle(m_configs.ErrorPause, "Error Pause", "ToolbarButton", GUILayout.ExpandWidth(false));

            string lastSearchTerm = m_searchString;
            m_searchString = GUILayout.TextField(m_searchString, "ToolbarSeachTextField");
            if (m_configs.RegexSearch && lastSearchTerm != m_searchString)
            {
                m_lastRegexRecompile = DateTime.Now;
                m_needRegexRecompile = true;
            }

            m_configs.AdvancedSearchToolbar = GUILayout.Toggle(m_configs.AdvancedSearchToolbar, "S", "ToolbarButton", GUILayout.ExpandWidth(false));

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
                m_needRegexRecompile = true;
            }
            m_searchMessage = GUILayout.Toggle(m_searchMessage, "Search in Log Message", "ToolbarButton", GUILayout.ExpandWidth(false));
            m_configs.SearchObjectName = GUILayout.Toggle(m_configs.SearchObjectName, "Search in Object Name", "ToolbarButton", GUILayout.ExpandWidth(false));
            m_configs.SearchInStackTrace = GUILayout.Toggle(m_configs.SearchInStackTrace, "Search in Stack Trace", "ToolbarButton", GUILayout.ExpandWidth(false));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DisplayEntry(ConsoleLogEntry entry, int idx, float totalWidth)
        {
            var saveColor = GUI.color;
            var saveBGColor = GUI.backgroundColor;
            float imageSize = 35;
            float sidePaddings = 10;
            float collapseBubbleSize = m_configs.Collapse ? (40 - sidePaddings) : 0; // Globally accessible ?
            float empiricalPaddings = 20 + sidePaddings;
            float itemHeight = 40;
            GUIStyle currentStyle = m_skin.FindStyle("OddEntry");
            GUIStyle textStyle = m_skin.FindStyle("EntryLabel"); // Cache styles
            textStyle.normal.textColor = GUI.skin.label.normal.textColor;
            if (idx == m_selectedIndex)
            {
                currentStyle = m_skin.FindStyle("SelectedEntry");
                textStyle = m_skin.FindStyle("EntryLabelSelected"); // Cache styles
            }
            else if (idx % 2 == 0)
            {
                currentStyle = m_skin.FindStyle("EvenEntry"); // Cache styles
            }
            GUILayout.BeginHorizontal(currentStyle, GUILayout.Height(itemHeight));
            //GUI.color = saveColor;
            // Picto space
            GUILayout.BeginHorizontal(GUILayout.Width(imageSize + sidePaddings));
            GUILayout.FlexibleSpace();
            GUILayout.Box(GetEntryIcon(entry), GUIStyle.none, GUILayout.Width(imageSize), GUILayout.Height(imageSize));
            GUILayout.EndHorizontal();
            // Text space
            GUILayout.BeginVertical();
            GUILayout.Label($"[{entry.timestamp}] {entry.messageFirstLine}", textStyle, GUILayout.Width(totalWidth - imageSize - collapseBubbleSize - empiricalPaddings));
            if(m_configs.ShowContextNameInsteadOfStack && entry.context != null)
            {
                GUILayout.Label($"{entry.context.name}", textStyle, GUILayout.Width(totalWidth - imageSize - collapseBubbleSize - empiricalPaddings)); // TODO cache this line
            }
            else if (!string.IsNullOrEmpty(entry.stackTrace))
            {
                GUILayout.Label($"{GetFirstLine(entry.stackTrace, true)}", textStyle, GUILayout.Width(totalWidth - imageSize - collapseBubbleSize - empiricalPaddings)); // TODO cache this line
            }
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            // Collapse Space
            if (m_configs.Collapse)
            {
                DisplayCollapseBubble(entry.level, entry.count, collapseBubbleSize, sidePaddings);
            }
            // Category Space
            GUILayout.EndHorizontal();

            Rect r = GUILayoutUtility.GetLastRect();
            if (GUI.Button(r, GUIContent.none, GUIStyle.none))
            {
                if (entry.context != null)
                {
                    EditorGUIUtility.PingObject(entry.context); // TODO Editor
                }
                if (m_selectedIndex == idx && DateTime.Now.Ticks - m_lastClick.Ticks < m_doubleClickSpeed)
                {
                    HandleDoubleClick(entry);
                }
                m_selectedIndex = idx;
                m_lastClick = DateTime.Now;
            }

            GUI.color = saveColor;
            GUI.backgroundColor = saveBGColor;
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
            var content = new GUIContent(text);
            float height = textStyle.CalcHeight(content, EditorGUIUtility.currentViewWidth);
            var lastRect = GUILayoutUtility.GetLastRect();
            EditorGUI.SelectableLabel(new Rect(currentX, lastRect.yMax, EditorGUIUtility.currentViewWidth, height), text, textStyle);
            GUILayout.Space(height);
        }
        
        private void FlagButton(LogLevel level, Texture2D icon, Texture2D iconGray)
        {
            bool hasFlag = (m_logLevelFilter & level) != 0;
            int counter = GetCounter(level);

            bool newFlagValue = GUILayout.Toggle(hasFlag, new GUIContent($" {(counter > 999 ? "999+" : counter.ToString())}", (counter > 0 ? icon : iconGray)),
                (GUIStyle)"ToolbarButton"
                , GUILayout.MaxWidth(GetFlagButtonWidthFromCounter(counter)), GUILayout.ExpandWidth(false)
                );
            if (hasFlag != newFlagValue)
            {
                m_logLevelFilter ^= level;
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