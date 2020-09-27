using System.Collections;
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Linq;
using System.IO;

namespace ProperLogger
{
    internal class ProperConsoleWindow : EditorWindow, IHasCustomMenu, ILogObserver
    {
        #region Members
        #region Consts

        private GUISkin m_skin = null;
        private GUISkin Skin => m_skin ?? (m_skin = Utils.LoadAssetByName<GUISkin>(Strings.EditorSkin));

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
        private bool m_callForRepaint = false;

        #region Logs

        private List<ConsoleLogEntry> m_entries = null;
        private List<ConsoleLogEntry> m_filteredEntries = null;
        private List<ConsoleLogEntry> m_collapsedEntries = null;
        private List<ConsoleLogEntry> m_displayedEntries = null;
        private bool m_triggerFilteredEntryComputation = false;
        private bool m_triggerSyncWithUnityComputation = false;
        private CustomLogHandler m_logHandler = null;
        private List<PendingContext> m_pendingContexts = null;
        private object m_entriesLock = null;
        private bool m_listening = false;

        #region Filters

        private string m_searchString = null;
        private string[] m_searchWords = null;
        private List<LogCategory> m_inactiveCategories = null;

        #endregion Filters

        #endregion Logs

        #region Layout

        //private int m_selectedIndex = -1;
        private List<ConsoleLogEntry> m_selectedEntries = null;
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
        private Rect m_listDisplay = default;

        private Rect m_searchFieldRect = default;
        private Rect m_resetSearchButtonRect = default;

        private bool m_lastCLickIsDisplayList = false;

        #endregion Layout

        #region Loaded Textures

        private static Texture2D m_iconInfo;
        private static Texture2D m_iconWarning;
        private static Texture2D m_iconError;

        private static Texture2D m_iconInfoGray;
        private static Texture2D m_iconWarningGray;
        private static Texture2D m_iconErrorGray;

        private static Texture2D m_iconConsole;

        private static Texture2D m_clearIcon;
        private static Texture2D m_collapseIcon;
        private static Texture2D m_clearOnPlayIcon;
        private static Texture2D m_clearOnBuildIcon;
        private static Texture2D m_errorPauseIcon;
        private static Texture2D m_regexSearchIcon;
        private static Texture2D m_caseSensitiveIcon;
        private static Texture2D m_advancedSearchIcon;

        [SerializeField]
        private Texture2D m_exceptionIcon; // TODO can't have serialized fields
        [SerializeField]
        private Texture2D m_assertIcon; // TODO can't have serialized fields

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


        private Assembly assembly = null;
        private Type logEntries = null;
        private Type logEntry = null;
        private MethodInfo startGettingEntries = null;
        private MethodInfo endGettingEntries = null;
        private MethodInfo getEntryInternal = null;
        private MethodInfo getCountsByType = null;
        private MethodInfo getCount = null;
        private MethodInfo rowGotDoubleClicked = null;
        private MethodInfo clearEntries = null;
        private FieldInfo messageField = null;
        private FieldInfo fileField = null;
        private FieldInfo lineField = null;
        private FieldInfo modeField = null;

        private int m_logLog = 0;
        private int m_warnLog = 0;
        private int m_errLog = 0;

        private GUIContent m_clearButtonContent = null;
        private GUIContent m_collapseButtonContent = null;
        private GUIContent m_clearOnPlayButtonContent = null;
        private GUIContent m_clearOnBuildButtonContent = null;
        private GUIContent m_errorPauseButtonContent = null;

        private GUIContent m_advancedSearchButtonContent = null;
        private GUIContent m_categoriesButtonContent = null;
        private GUIContent m_regexSearchButtonNameOnlyContent = null;
        private GUIContent m_caseSensitiveButtonContent = null;
        private GUIContent m_searchInLogMessageButtonContent = null;
        private GUIContent m_searchInObjectNameButtonContent = null;
        private GUIContent m_searchInStackTraceButtonContent = null;
        private GUIContent m_pluginSettingsButtonContent = null;

        private GUIStyle m_oddEntry = null;
        private GUIStyle m_selectedEntry = null;
        private GUIStyle m_selectedEntryLabel = null;
        private GUIStyle m_evenEntry = null;
        private GUIStyle m_evenEntryLabel = null;
        private GUIStyle m_categoryNameStyle = null;
        private GUIStyle m_categoryColorStrip = null;
        private GUIStyle m_collapseBubbleStyle = null;
        private GUIStyle m_collapseBubbleWarningStyle = null;
        private GUIStyle m_collapseBubbleErrorStyle = null;
        private GUIStyle m_toolbarIconButtonStyle = null;
        private GUIStyle m_inspectorTextStyle = null;

        private Regex m_categoryParse = null;
        private Regex CategoryParse => m_categoryParse ?? (m_categoryParse = new Regex("\\[([^\\s\\[\\]]+)\\]"));

        #endregion Caches
        #endregion Members

        #region Properties

        private static ProperConsoleWindow m_instance = null;
        internal static ProperConsoleWindow Instance => m_instance;
        private Type LogEntries => logEntries ?? (logEntries = UnityAssembly.GetType(Strings.LogEntries));
        private Assembly UnityAssembly => assembly ?? (assembly = Assembly.GetAssembly(typeof(UnityEditor.ActiveEditorTracker)));
        private MethodInfo GetCountsByType => getCountsByType ?? (getCountsByType = LogEntries.GetMethod(Strings.GetCountsByType));
        private MethodInfo GetCount => getCount ?? (getCount = LogEntries.GetMethod(Strings.GetCount));
        private MethodInfo RowGotDoubleClicked => rowGotDoubleClicked ?? (rowGotDoubleClicked = LogEntries.GetMethod(Strings.RowGotDoubleClicked));
        private MethodInfo ClearEntries => clearEntries ?? (clearEntries = LogEntries.GetMethod(Strings.Clear));

        private float ItemHeight => (m_configs.LogEntryMessageFontSize + (m_configs.LogEntryMessageFontSize < 15 ? 3 : 4)) * m_configs.LogEntryMessageLineCount
                                  + (m_configs.LogEntryStackTraceFontSize + (m_configs.LogEntryStackTraceFontSize < 15 ? 3 : 4)) * m_configs.LogEntryStackTraceLineCount
                                  + 8; // padding

        #endregion Properties

        #region Editor Window

        [MenuItem("Leonard/Console")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            if (ProperConsoleWindow.m_instance != null)
            {
                ProperConsoleWindow.m_instance.Show(true);
            }
            else
            {
                ShowWindow();
            }
            ProperConsoleWindow.m_instance.Focus();
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
            m_entries = m_entries ?? new List<ConsoleLogEntry>();
            m_pendingContexts = m_pendingContexts ?? new List<PendingContext>();
            m_selectedEntries = m_selectedEntries ?? new List<ConsoleLogEntry>();
            m_listening = false;
            m_entriesLock = new object();
            m_instance = this;
            m_triggerFilteredEntryComputation = true;
            EditorApplication.playModeStateChanged += ModeChanged;
            InitListener();
            LoadIcons();
            CacheGUIContents();
            ClearStyles();
            m_autoScroll = true;
            ProperConsoleWindow.m_instance.titleContent = new GUIContent(Strings.WindowTitle, m_iconConsole);

            m_needRegexRecompile = true;
        }

        private void OnDisable()
        {
            RemoveListener();
            EditorApplication.playModeStateChanged -= ModeChanged;
            m_instance = null;
            ClearGUIContents();
        }

        public void OnBuild()
        {
            if (m_configs.ClearOnBuild)
            {
                Clear();
            }
        }

        internal void ClearGUIContents()
        {
            m_clearButtonContent = null;
            m_collapseButtonContent = null;
            m_clearOnPlayButtonContent = null;
            m_clearOnBuildButtonContent = null;
            m_errorPauseButtonContent = null;

            m_advancedSearchButtonContent = null;
            m_categoriesButtonContent = null;
            m_regexSearchButtonNameOnlyContent = null;
            m_caseSensitiveButtonContent = null;
            m_searchInLogMessageButtonContent = null;
            m_searchInObjectNameButtonContent = null;
            m_searchInStackTraceButtonContent = null;
            m_pluginSettingsButtonContent = null;
        }

        private GUIContent CreateButtonGUIContent(Texture2D icon, string text)
        {
            if(icon == null)
            {
                return new GUIContent(text);
            }
            switch(m_configs.DisplayIcons)
            {
                case 0: // Name Only
                default:
                    return new GUIContent(text);
                case 1: // Name and Icon
                    return new GUIContent($" {text}", icon);
                case 2: // Icon Only
                    return new GUIContent(icon, text);
            }
        }

        internal void CacheGUIContents()
        {
            m_clearButtonContent = CreateButtonGUIContent(m_clearIcon, "Clear");
            m_collapseButtonContent = CreateButtonGUIContent(m_collapseIcon, "Collapse");
            m_clearOnPlayButtonContent = CreateButtonGUIContent(m_clearOnPlayIcon, "Clear on Play");
            m_clearOnBuildButtonContent = CreateButtonGUIContent(m_clearOnBuildIcon, "Clear on Build");
            m_errorPauseButtonContent = CreateButtonGUIContent(m_errorPauseIcon, "Error Pause");

            m_advancedSearchButtonContent = new GUIContent(m_advancedSearchIcon, "Advanced Search");
            m_categoriesButtonContent = new GUIContent("Categories");
            m_regexSearchButtonNameOnlyContent = CreateButtonGUIContent(m_regexSearchIcon, "Regex Search");
            m_caseSensitiveButtonContent = CreateButtonGUIContent(m_caseSensitiveIcon, "Case Sensitive");
            m_searchInLogMessageButtonContent = new GUIContent("Search in Log Message");
            m_searchInObjectNameButtonContent = new GUIContent("Search in Object Name");
            m_searchInStackTraceButtonContent = new GUIContent("Search in Stack Trace");
            m_pluginSettingsButtonContent = new GUIContent("Plugin Settings");
        }

        private void ClearStyles()
        {
            m_oddEntry = null;
            m_selectedEntry = null;
            m_selectedEntryLabel = null;
            m_evenEntry = null;
            m_evenEntryLabel = null;

            m_categoryNameStyle = null;

            m_categoryColorStrip = null;

            m_collapseBubbleStyle = null;
            m_collapseBubbleWarningStyle = null;
            m_collapseBubbleErrorStyle = null;

            m_toolbarIconButtonStyle = null;

            m_inspectorTextStyle = null;
        }

        private void CacheStyles()
        {
            // TODO some styles don't need "new" style instantiation

            m_oddEntry = new GUIStyle(Skin.FindStyle("OddEntry"));
            m_selectedEntry = new GUIStyle(Skin.FindStyle("SelectedEntry"));
            m_selectedEntryLabel = new GUIStyle(Skin.FindStyle("EntryLabelSelected"));
            m_evenEntry = new GUIStyle(Skin.FindStyle("EvenEntry"));
            m_evenEntryLabel = new GUIStyle(Skin.FindStyle("EntryLabel"));

            m_categoryNameStyle = new GUIStyle(m_evenEntryLabel);
            m_categoryNameStyle.normal.textColor = GUI.skin.label.normal.textColor;
            m_categoryNameStyle.alignment = TextAnchor.MiddleCenter;
            m_categoryNameStyle.fontSize = m_configs.LogEntryStackTraceFontSize;
            m_categoryNameStyle.padding.top = (int)((ItemHeight / 2f) - m_categoryNameStyle.fontSize);
            m_categoryNameStyle.fontStyle = FontStyle.Bold;
            m_categoryNameStyle.fontSize = m_configs.LogEntryMessageFontSize;

            m_categoryColorStrip = new GUIStyle(Skin.FindStyle("CategoryColorStrip"));

            m_collapseBubbleStyle = new GUIStyle(Skin.FindStyle("CollapseBubble"));
            m_collapseBubbleWarningStyle = new GUIStyle(Skin.FindStyle("CollapseBubbleWarning"));
            m_collapseBubbleErrorStyle = new GUIStyle(Skin.FindStyle("CollapseBubbleError"));

            m_toolbarIconButtonStyle = new GUIStyle(Strings.ToolbarButton);

            m_inspectorTextStyle = new GUIStyle(GUI.skin.label);
            m_inspectorTextStyle.richText = true;
            m_inspectorTextStyle.normal.textColor = Color.black;
            m_inspectorTextStyle.fontSize = m_configs.InspectorMessageFontSize;
            m_inspectorTextStyle.wordWrap = true;
            m_inspectorTextStyle.stretchWidth = false;
            m_inspectorTextStyle.clipping = TextClipping.Clip;
        }

        private void LoadIcons()
        {
            // TODO check out EditorGUIUtility.Load
            m_iconInfo = (Texture2D)LoadIcon.Invoke(null, new object[] { "console.infoicon" });
            m_iconWarning = (Texture2D)LoadIcon.Invoke(null, new object[] { "console.warnicon" });
            m_iconError = (Texture2D)LoadIcon.Invoke(null, new object[] { "console.erroricon" });

            m_iconInfoGray = (Texture2D)LoadIcon.Invoke(null, new object[] { "console.infoicon.inactive.sml" });
            m_iconWarningGray = (Texture2D)LoadIcon.Invoke(null, new object[] { "console.warnicon.inactive.sml" });
            m_iconErrorGray = (Texture2D)LoadIcon.Invoke(null, new object[] { "console.erroricon.inactive.sml" });

            m_iconConsole = (Texture2D)LoadIcon.Invoke(null, new object[] { "UnityEditor.ConsoleWindow" });

            m_clearIcon = Utils.LoadAssetByName<Texture2D>(Strings.ClearIcon);
            m_collapseIcon = Utils.LoadAssetByName<Texture2D>(Strings.CollapseIcon);
            m_clearOnBuildIcon = Utils.LoadAssetByName<Texture2D>(Strings.ClearOnBuildIcon);
            m_clearOnPlayIcon = Utils.LoadAssetByName<Texture2D>(Strings.ClearOnPlayIcon);
            m_errorPauseIcon = Utils.LoadAssetByName<Texture2D>(Strings.ErrorPauseIcon);
            m_regexSearchIcon = Utils.LoadAssetByName<Texture2D>(Strings.RegexSearchIcon);
            m_caseSensitiveIcon = Utils.LoadAssetByName<Texture2D>(Strings.CaseSensitiveIcon);
            m_advancedSearchIcon = Utils.LoadAssetByName<Texture2D>(Strings.AdvancedSearchIcon);

            //m_exceptionIcon = (Texture2D)LoadIcon.Invoke(null, new object[] { "ExceptionIcon" });
            //m_assertIcon = (Texture2D)LoadIcon.Invoke(null, new object[] { "AssertIcon" });
        }

        private void HandleDoubleClick(ConsoleLogEntry entry) // TODO could this be used in play mode ?
        {
#if UNITY_EDITOR
            if (entry.unityIndex >= 0)
            {
                RowGotDoubleClicked.Invoke(null, new object[] { entry.unityIndex }); // TODO check if this works in game
                return;
            }

            if (!string.IsNullOrEmpty(entry.assetPath))
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(entry.assetPath);
                if (!string.IsNullOrEmpty(entry.assetLine))
                {
                    AssetDatabase.OpenAsset(asset, int.Parse(entry.assetLine));
                }
                else
                {
                    AssetDatabase.OpenAsset(asset);
                }
            }
#endif // UNITY_EDITOR
        }

        public void HandleCopyToClipboard()
        {
            if (m_lastCLickIsDisplayList && m_selectedEntries != null && m_selectedEntries.Count > 0)
            {
                if (Event.current.type == EventType.ValidateCommand && Event.current.commandName == Strings.CopyCommandName)
                {
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == Strings.CopyCommandName)
                {
                    CopySelection();
                }
            }
            if (Event.current.type == EventType.MouseDown)
            {
                if (!m_listDisplay.Contains(Event.current.mousePosition))
                {
                    m_lastCLickIsDisplayList = false;
                }
            }
        }

        private void CopySelection()
        {
            string result = string.Empty;

            foreach (var entry in m_selectedEntries)
            {
                result += entry.GetExportString() + Environment.NewLine + Environment.NewLine;
            }

            GUIUtility.systemCopyBuffer = result;
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
                if (Debug.unityLogger.logHandler is CustomLogHandler customLogHandler)
                {
                    customLogHandler.RemoveObserver(this);
                    customLogHandler.AddObserver(this);
                }
                else
                {
                    m_logHandler = new CustomLogHandler(Debug.unityLogger.logHandler);
                    m_logHandler.AddObserver(this);
                    Debug.unityLogger.logHandler = m_logHandler;
                }
                Application.logMessageReceivedThreaded += Listener;
                m_listening = true;
            }
        }

        public void RemoveListener()
        {
            Application.logMessageReceivedThreaded -= Listener;
            if (Debug.unityLogger.logHandler is CustomLogHandler customLogHandler)
            {
                customLogHandler.RemoveObserver(this);
            }
            m_listening = false;
        }

        private static bool IsError(int mode)
        {
            return HasMode(mode, UnityLogMode.Error | UnityLogMode.Fatal | UnityLogMode.Assert | UnityLogMode.AssetImportError | UnityLogMode.GraphCompileError | UnityLogMode.ScriptCompileError | UnityLogMode.ScriptingError | UnityLogMode.StickyError | UnityLogMode.ScriptingAssertion);
        }

        private static bool IsWarning(int mode)
        {
            return HasMode(mode, UnityLogMode.AssetImportWarning | UnityLogMode.ScriptCompileWarning | UnityLogMode.ScriptingWarning);
        }

        private static bool IsLog(int mode)
        {
            return HasMode(mode, UnityLogMode.Log | UnityLogMode.ScriptingLog);
        }

        private static UnityEngine.LogType GetLogTypeFromUnityMode(int unityMode)
        {
            if (HasMode(unityMode, UnityLogMode.Assert | UnityLogMode.ScriptingAssertion))
            {
                return LogType.Assert;
            }
            if (HasMode(unityMode, UnityLogMode.ScriptingException))
            {
                return LogType.Exception;
            }
            if (IsError(unityMode))
            {
                return LogType.Error;
            }
            if (IsWarning(unityMode))
            {
                return LogType.Warning;
            }
            return LogType.Log;
        }

        private static LogLevel GetLogLevelFromUnityMode(int unityMode)
        {
            if (HasMode(unityMode, UnityLogMode.Assert | UnityLogMode.ScriptingAssertion))
            {
                return LogLevel.Assert;
            }
            if (HasMode(unityMode, UnityLogMode.ScriptingException))
            {
                return LogLevel.Exception;
            }
            if (IsError(unityMode))
            {
                return LogLevel.Error;
            }
            if (IsWarning(unityMode))
            {
                return LogLevel.Warning;
            }
            return LogLevel.Log;
        }

        private static bool CompareModes(int unityMode, LogLevel loglevel)
        {
            return GetLogLevelFromUnityMode(unityMode) == loglevel;
        }

        private static bool HasMode(int mode, UnityLogMode modeToCheck) => ((UnityLogMode)mode & modeToCheck) != (UnityLogMode)0;

        private static bool CompareEntries(CustomLogEntry unityEntry, ConsoleLogEntry consoleEntry)
        {
            return unityEntry.message.IndexOf(consoleEntry.originalMessage, StringComparison.Ordinal) == 0 && CompareModes(unityEntry.mode, consoleEntry.level);
        }

        private void PopulateLogEntryFields(Type type)
        {
            if (messageField == null || fileField == null || lineField == null || modeField == null)
            {
                var props = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.Instance);
                foreach (var prop in props)
                {
                    if (prop.Name == "message") // TODO no string, cache properties directly instead of array
                    {
                        messageField = prop;
                    }
                    if (prop.Name == "file")
                    {
                        fileField = prop;
                    }
                    if (prop.Name == "line")
                    {
                        lineField = prop;
                    }
                    if (prop.Name == "mode")
                    {
                        modeField = prop;
                    }
                }
            }
        }

        public CustomLogEntry ConvertUnityLogEntryToCustomLogEntry(object unityLogEntry, Type type)
        {
            PopulateLogEntryFields(type);

            var ret = new CustomLogEntry();

            ret.line = (int)lineField.GetValue(unityLogEntry); // TODO only call when needed ?
            ret.message = (string)messageField.GetValue(unityLogEntry);
            ret.file = (string)fileField.GetValue(unityLogEntry); // TODO only call when needed ?
            ret.mode = (int)modeField.GetValue(unityLogEntry);

            return ret;
        }

        private void SyncWithUnityEntries()
        {
            List<ConsoleLogEntry> newConsoleEntries = new List<ConsoleLogEntry>();
            logEntry = logEntry ?? UnityAssembly.GetType(Strings.LogEntry); // TODO better caches
            startGettingEntries = startGettingEntries ?? LogEntries.GetMethod(Strings.StartGettingEntries); // TODO better caches
            endGettingEntries = endGettingEntries ?? LogEntries.GetMethod(Strings.EndGettingEntries); // TODO better caches
            getEntryInternal = getEntryInternal ?? LogEntries.GetMethod(Strings.GetEntryInternal); // TODO better caches
            int count = (int)GetCount.Invoke(null, null);

            List<int> foundEntries = new List<int>(); // TODO this is dirty. The goal is to make sure similar ConsoleEntries don't find the same (first) UnityEntry

            startGettingEntries.Invoke(null, null);
            int firstIndex = 0;
            for (int i = 0; i < count; i++)
            {
                object entry = Activator.CreateInstance(logEntry);
                object[] objparameters = new object[] { i, entry };
                bool result = (bool)getEntryInternal.Invoke(null, objparameters);
                if (result)
                {
                    CustomLogEntry unityEntry = ConvertUnityLogEntryToCustomLogEntry(entry, logEntry);
                    bool found = false;
                    for(int j = firstIndex; j < m_entries.Count; j++)
                    {
                        if (foundEntries.Contains(j))
                        {
                            continue;
                        }

                        var consoleEntry = m_entries[j];
                        if (CompareEntries(unityEntry, consoleEntry))
                        {
                            found = true;
                            foundEntries.Add(j);
                            consoleEntry.assetPath = unityEntry.file;
                            consoleEntry.assetLine = unityEntry.line.ToString();
                            consoleEntry.unityMode = unityEntry.mode;
                            consoleEntry.unityIndex = i;
                            newConsoleEntries.Add(consoleEntry);
                            if(j == firstIndex)
                            {
                                firstIndex++;
                            }
                            break;
                        }
                    }
                    //Debug.Assert(found);
                    if (!found)
                    {
                        var consoleEntry = Listener(unityEntry.message, null, GetLogTypeFromUnityMode(unityEntry.mode), unityEntry.file, unityEntry.line.ToString());
                        consoleEntry.unityMode = unityEntry.mode;
                        consoleEntry.message = Utils.ParseStackTrace(consoleEntry.originalMessage, out _, out _);
                        newConsoleEntries.Add(consoleEntry);
                    }
                }
            }

            endGettingEntries.Invoke(null, null);

            //lock (m_entries)
            {
                m_entries.Clear();
                m_entries.AddRange(newConsoleEntries);
            }
        }

        private void Listener(string condition, string stackTrace, LogType type)
        {
            Listener(condition, stackTrace, type, null, null);
        }

        private ConsoleLogEntry Listener(string condition, string stackTrace, LogType type, string assetPath, string assetLine)
        {
            ConsoleLogEntry newConsoleEntry = null;
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

                List<LogCategory> categories = new List<LogCategory>();
                var categoryAsset = m_configs.CurrentCategoriesConfig;
                string categoryLessMessage = condition;
                if (categoryAsset != null && categoryAsset.Categories != null && categoryAsset.Categories.Count > 0)
                {
                    foreach (Match match in CategoryParse.Matches(categoryLessMessage))
                    {
                        foreach (var category in categoryAsset.Categories)
                        {
                            if(category.Name == match.Groups[1].Value && !categories.Contains(category))
                            {
                                categories.Add(category);
                                categoryLessMessage = categoryLessMessage.Replace($"[{category.Name}] ", string.Empty);
                            }
                        }
                    }
                }

                var now = DateTime.Now;
                string tempAssetPath = null;
                string tempAssetLine = null;
                string newStackTrace = string.IsNullOrEmpty(stackTrace) ? null : Utils.ParseStackTrace(stackTrace, out tempAssetPath, out tempAssetLine);

                newConsoleEntry = new ConsoleLogEntry()
                {
                    date = now.Ticks,
                    timestamp = now.ToString("T", DateTimeFormatInfo.InvariantInfo),
                    level = Utils.GetLogLevelFromUnityLogType(type),
                    message = categoryLessMessage,
                    messageLines = Utils.GetLines(categoryLessMessage),
                    traceLines = Utils.GetLines(newStackTrace),
                    stackTrace = newStackTrace,
                    count = 1,
                    context = context,
                    assetPath = string.IsNullOrEmpty(assetPath) ? tempAssetPath : assetPath,
                    assetLine = string.IsNullOrEmpty(assetLine) ? tempAssetLine : assetLine,
                    categories = categories,
                    originalMessage = condition,
                    originalStackTrace = stackTrace,
                };

                m_entries.Add(newConsoleEntry);
            }

            m_triggerFilteredEntryComputation = true;

            this.Repaint();
#if UNITY_EDITOR
            if (EditorApplication.isPlaying && m_configs.ErrorPause && (type == LogType.Assert || type == LogType.Error || type == LogType.Exception))
            {
                Debug.Break();
            }
#endif //UNITY_EDITOR
            return newConsoleEntry;
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

        internal void Clear()
        {
            lock (m_entriesLock)
            {
                //m_logCounter = 0;
                //m_warningCounter = 0;
                //m_errorCounter = 0;
                //m_entries.Clear();
                m_pendingContexts.Clear();
                m_selectedEntries.Clear();

                ClearEntries.Invoke(null, null); // TODO check if this works in game

                SyncWithUnityEntries();
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
            string searchableText = (m_searchMessage ? e.originalMessage : string.Empty) + (m_configs.SearchInStackTrace ? e.stackTrace : string.Empty) + ((m_configs.SearchObjectName && e.context != null) ? e.context.name : string.Empty); // TODO opti
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
            HandleCopyToClipboard();
            EditorSelectableLabelInvisible();

            if(m_inspectorTextStyle == null)
            {
                CacheStyles();
            }

            m_callForRepaint = false;
            bool repaint = Event.current.type == EventType.Repaint;

            m_inactiveCategories?.Clear();
            m_inactiveCategories = m_configs.InactiveCategories;

            DisplayToolbar(ref m_callForRepaint);

            if (m_configs.AdvancedSearchToolbar)
            {
                DisplaySearchToolbar();
            }

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

            if (m_triggerSyncWithUnityComputation)
            {
                lock (m_entries)
                {
                    SyncWithUnityEntries();
                }
                m_triggerFilteredEntryComputation = true;
                m_triggerSyncWithUnityComputation = false;
            }

            if (m_triggerFilteredEntryComputation)
            {
                m_filteredEntries = m_entries.FindAll(e => ValidFilter(e));
                if (m_configs.Collapse)
                {
                    ComputeCollapsedEntries(m_filteredEntries);
                }
                m_triggerFilteredEntryComputation = false;
            }

            DisplayList(m_configs.Collapse ? m_collapsedEntries : m_filteredEntries, out m_displayedEntries, totalWidth);

            if (m_displayedEntries.Count < m_displayedEntriesCount)
            {
                m_selectedEntries.Clear();
            }
            m_displayedEntriesCount = m_displayedEntries.Count;

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
            if (repaint)
            {
                m_listDisplay = GUILayoutUtility.GetLastRect();
            }
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
            if (m_selectedEntries.Count > 0)
            {
                var entry = m_selectedEntries[0];

                GUILayout.Space(1);
                float currentX = (GUILayoutUtility.GetLastRect()).xMin;

                string categoriesString = string.Empty;
                if (entry.categories != null && entry.categories.Count > 0)
                {
                    if (m_configs.CategoryDisplay.HasFlag(ECategoryDisplay.InInspector))
                    {
                        string format = "<color=#{1}>[{0}]</color> ";
                        categoriesString = string.Join(string.Empty, entry.categories.Select(c =>string.Format(format, c.Name, ColorUtility.ToHtmlStringRGB(Color.Lerp(c.Color, m_inspectorTextStyle.normal.textColor, m_configs.CategoryNameColorize)))));
                    }
                }

                SelectableLabel($"{categoriesString}{entry.message}", m_inspectorTextStyle, currentX); 

                if (entry.context != null)
                {
                    Color txtColor = m_inspectorTextStyle.normal.textColor;
                    if (!m_configs.ObjectNameColor.Equals(txtColor))
                    {
                        m_inspectorTextStyle.normal.textColor = m_configs.ObjectNameColor;
                    }
                    SelectableLabel(entry.context.name, m_inspectorTextStyle, currentX);
                    m_inspectorTextStyle.normal.textColor = txtColor;
                }
                if (!string.IsNullOrEmpty(entry.stackTrace))
                {
                    SelectableLabel(entry.stackTrace, m_inspectorTextStyle, currentX);
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
                Debug.Log($"Log {DateTime.Now.ToString()} {m_listening}\r\nA\nB\nC\nD", Camera.main);
            }

            if (GUILayout.Button("Log Combat"))
            {
                Debug.Log($"[Combat] [Performance] Log {DateTime.Now.ToString()} {m_listening}", Camera.main);
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
                DDebug.Assert(false);
            }

            if (GUILayout.Button("Add 1000 Log"))
            {
                for (int i = 0; i < 1000; i++)
                {
                    Debug.Log($"Log {DateTime.Now.ToString()} {m_listening}");
                }
            }
            if (GUILayout.Button("1000 syncs"))
            {
                lock (m_entriesLock)
                {
                    for (int i = 0; i < 10000; i++)
                    {
                        SyncWithUnityEntries();
                    }
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
                }
            }
        }

        private void DisplayList(List<ConsoleLogEntry> filteredEntries, out List<ConsoleLogEntry> displayedEntries, float totalWidth)
        {
            int startI = 0;
            int endI = filteredEntries.Count;
            int lastVisibleIdx = 0;
            // Only display elements that are in view
            if (m_outerScrollableHeight + 100 <= m_innerScrollableHeight)
            {
                int firstVisibleIdx = Mathf.Clamp((int)(m_entryListScrollPosition.y / ItemHeight) - 1, 0, filteredEntries.Count);
                lastVisibleIdx = Mathf.Clamp((int)((m_entryListScrollPosition.y + m_outerScrollableHeight) / ItemHeight) + 1, 0, filteredEntries.Count);
                GUILayout.Space(firstVisibleIdx * ItemHeight);
                startI = firstVisibleIdx;
                endI = lastVisibleIdx;
            }

            for (int i = startI; i < endI; i++)
            {
                DisplayEntry(filteredEntries[i], i, totalWidth);
            }

            if (lastVisibleIdx != 0)
            {
                GUILayout.Space((filteredEntries.Count - lastVisibleIdx) * ItemHeight);
            }
            displayedEntries = filteredEntries;
        }

        #region GUI Components

        private void DisplayToolbar(ref bool callForRepaint)
        {
            GUILayout.BeginHorizontal(Strings.Toolbar);

            if (GUILayout.Button(m_clearButtonContent, m_toolbarIconButtonStyle, GUILayout.ExpandWidth(false)))
            {
                Clear();
                GUIUtility.keyboardControl = 0;
            }
            bool lastCollapse = m_configs.Collapse;
            m_configs.Collapse = GUILayout.Toggle(m_configs.Collapse, m_collapseButtonContent, m_toolbarIconButtonStyle, GUILayout.ExpandWidth(false));
            callForRepaint = m_configs.Collapse != lastCollapse;
            if (m_configs.Collapse != lastCollapse)
            {
                m_triggerFilteredEntryComputation = true;
                m_selectedEntries.Clear();
            }
            m_configs.ClearOnPlay = GUILayout.Toggle(m_configs.ClearOnPlay, m_clearOnPlayButtonContent, m_toolbarIconButtonStyle, GUILayout.ExpandWidth(false));
            m_configs.ClearOnBuild = GUILayout.Toggle(m_configs.ClearOnBuild, m_clearOnBuildButtonContent, m_toolbarIconButtonStyle, GUILayout.ExpandWidth(false));
            m_configs.ErrorPause = GUILayout.Toggle(m_configs.ErrorPause, m_errorPauseButtonContent, m_toolbarIconButtonStyle, GUILayout.ExpandWidth(false));

            string lastSearchTerm = m_searchString;

            GUI.enabled = !(Event.current.isMouse && m_resetSearchButtonRect.Contains(Event.current.mousePosition));
            m_searchString = GUILayout.TextField(m_searchString, Strings.ToolbarSeachTextField);
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
            GUI.enabled = true;
            if (!string.IsNullOrEmpty(m_searchString))
            {
                if (Event.current.type == EventType.Repaint)
                {
                    m_searchFieldRect = GUILayoutUtility.GetLastRect();
                }
                float resetSearchButtonWidth = 15;
                m_resetSearchButtonRect = new Rect(m_searchFieldRect.xMax - resetSearchButtonWidth, m_searchFieldRect.y, resetSearchButtonWidth, m_searchFieldRect.height);
                if (GUI.Button(m_resetSearchButtonRect, GUIContent.none, "SearchCancelButton"))
                {
                    m_searchString = null;
                    m_triggerFilteredEntryComputation = true;
                    if (m_configs.RegexSearch)
                    {
                        m_lastRegexRecompile = DateTime.Now;
                        m_needRegexRecompile = true;
                    }
                    m_searchWords = null;
                }
            }

            m_configs.AdvancedSearchToolbar = GUILayout.Toggle(m_configs.AdvancedSearchToolbar, m_advancedSearchButtonContent, Strings.ToolbarButton, GUILayout.ExpandWidth(false));
            Rect dropdownRect = GUILayoutUtility.GetLastRect();

            if (GUILayout.Button(m_categoriesButtonContent, Strings.ToolbarButton, GUILayout.ExpandWidth(false)))
            {
                Vector2 dropdownOffset = new Vector2(40, 23);
                //Rect dropDownPosition = new Rect(Event.current.mousePosition.x + this.position.x, Event.current.mousePosition.y + this.position.y, dropdownOffset.x, m_showCategoriesButtonRect.height + dropdownOffset.y);
                Rect dropDownPosition = new Rect(dropdownRect.x + this.position.x, dropdownRect.y + this.position.y, dropdownOffset.x, m_showCategoriesButtonRect.height + dropdownOffset.y);

                var categoriesAsset = m_configs.CurrentCategoriesConfig;
                Vector2 size = new Vector2(250, 150);
                if (categoriesAsset != null)
                {
                    if (m_configs.CurrentCategoriesConfig.Categories == null || m_configs.CurrentCategoriesConfig.Categories.Count == 0)
                    {
                        size.y = 30;
                    }
                    else
                    {
                        size.y = (m_configs.CurrentCategoriesConfig.Categories.Count) * 20; // TODO put this somewhere in a style
                    }
                }
                size.y += 25;
                // Get existing open window or if none, make a new one:
                var window = (CategoriesFilterWindow)EditorWindow.CreateInstance<CategoriesFilterWindow>();
                window.ShowAsDropDown(dropDownPosition, size);
                window.Repaint();
            }
            if (Event.current.type == EventType.Repaint) m_showCategoriesButtonRect = GUILayoutUtility.GetLastRect();

            GetCounters(m_displayedEntries, out int logCounter, out int warnCounter, out int errCounter);

            // Log Level Flags
            FlagButton(LogLevel.Log, m_iconInfo, m_iconInfoGray, logCounter);
            FlagButton(LogLevel.Warning, m_iconWarning, m_iconWarningGray, warnCounter);
            FlagButton(LogLevel.Error, m_iconError, m_iconErrorGray, errCounter);

            GUILayout.EndHorizontal();
        }

        private void DisplaySearchToolbar()
        {

            GUILayout.BeginHorizontal(Strings.Toolbar);
            bool lastRegexSearch = m_configs.RegexSearch;
            m_configs.RegexSearch = GUILayout.Toggle(m_configs.RegexSearch, m_regexSearchButtonNameOnlyContent, m_toolbarIconButtonStyle, GUILayout.ExpandWidth(false));
            if(lastRegexSearch != m_configs.RegexSearch)
            {
                m_needRegexRecompile = true;
            }
            bool lastCaseSensitive = m_configs.CaseSensitive;
            m_configs.CaseSensitive = GUILayout.Toggle(m_configs.CaseSensitive, m_caseSensitiveButtonContent, Strings.ToolbarButton, GUILayout.ExpandWidth(false));
            if (lastCaseSensitive != m_configs.CaseSensitive)
            {
                m_triggerFilteredEntryComputation = true;
                m_needRegexRecompile = true;
            }
            bool lastSearchMessage = m_searchMessage;
            m_searchMessage = GUILayout.Toggle(m_searchMessage, m_searchInLogMessageButtonContent, Strings.ToolbarButton, GUILayout.ExpandWidth(false));
            if (lastSearchMessage != m_searchMessage)
            {
                m_triggerFilteredEntryComputation = true;
            }
            bool lastSearchObjectName = m_configs.SearchObjectName;
            m_configs.SearchObjectName = GUILayout.Toggle(m_configs.SearchObjectName, m_searchInObjectNameButtonContent, Strings.ToolbarButton, GUILayout.ExpandWidth(false));
            if (lastSearchObjectName != m_configs.SearchObjectName)
            {
                m_triggerFilteredEntryComputation = true;
            }
            bool lastSearchStackTRace = m_configs.SearchInStackTrace;
            m_configs.SearchInStackTrace = GUILayout.Toggle(m_configs.SearchInStackTrace, m_searchInStackTraceButtonContent, Strings.ToolbarButton, GUILayout.ExpandWidth(false));
            if (lastSearchStackTRace != m_configs.SearchInStackTrace)
            {
                m_triggerFilteredEntryComputation = true;
            }
            GUILayout.FlexibleSpace();
#if UNITY_EDITOR
            if (GUILayout.Button(m_pluginSettingsButtonContent))
            {
                SettingsService.OpenUserPreferences(ProperLoggerCustomSettingsProvider.s_pathToPreferences);
            }
#endif // UNITY_EDITOR
            GUILayout.EndHorizontal();
        }

        private void DisplayEntry(ConsoleLogEntry entry, int idx, float totalWidth)
        {
            GUIStyle currentStyle = m_oddEntry;
            GUIStyle textStyle = m_evenEntryLabel;
            textStyle.normal.textColor = GUI.skin.label.normal.textColor;

            float imageSize = Math.Min(ItemHeight - (2 * 3), 40); // We clamp it in case we display 3+ lines
            imageSize += imageSize % 2;
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

            string categoriesString = string.Empty;

            if (entry.categories != null && entry.categories.Count > 0)
            {
                if (categoryColumn)
                {
                    var categoryString = string.Join(" ", entry.categories.Take(Mathf.Min(m_configs.CategoryCountInLogList, entry.categories.Count)).Select(c=>c.Name));
                    categoryColumnWidth = m_categoryNameStyle.CalcSize(new GUIContent(categoryString)).x + 10;
                }
                if (displayCategoryStrips)
                {
                    categoriesStripsTotalWidth = entry.categories.Count * categoryStripWidth;
                }
                if (m_configs.CategoryDisplay.HasFlag(ECategoryDisplay.InMessage))
                {
                    string format = "<color=#{1}>[{0}]</color> ";
                    categoriesString = string.Join(string.Empty, entry.categories.Select(c => string.Format(format, c.Name, ColorUtility.ToHtmlStringRGB(Color.Lerp(c.Color, textStyle.normal.textColor, m_configs.CategoryNameColorize)))));
                }
            }

            float entrywidth = totalWidth - imageSize - collapseBubbleSize - categoryColumnWidth - empiricalPaddings - rightSplitterWidth - categoriesStripsTotalWidth;

            
            if (m_selectedEntries.Count > 0 && m_selectedEntries.Contains(entry))
            {
                currentStyle = m_selectedEntry;
                textStyle = m_selectedEntryLabel;
            }
            else if (idx % 2 == 0)
            {
                currentStyle = m_evenEntry;
            }

            GUILayout.BeginHorizontal(currentStyle, GUILayout.Height(ItemHeight));
            {
                //GUI.color = saveColor;
                // Picto space
                GUILayout.BeginHorizontal(GUILayout.Width(imageSize + sidePaddings));
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Box(GetEntryIcon(entry), GUIStyle.none, GUILayout.Width(imageSize), GUILayout.Height(imageSize));
                }
                GUILayout.EndHorizontal();
                // Text space
                GUILayout.BeginVertical();
                {
                    textStyle.fontSize = m_configs.LogEntryMessageFontSize;
                    GUILayout.Label($"[{entry.timestamp}] {categoriesString}{Utils.GetFirstLines(entry.messageLines, 0, m_configs.LogEntryMessageLineCount, false)}", textStyle, GUILayout.Width(entrywidth));
                    textStyle.fontSize = m_configs.LogEntryStackTraceFontSize;
                    if (m_configs.LogEntryStackTraceLineCount > 0)
                    {
                        if (m_configs.ShowContextNameInsteadOfStack && entry.context != null)
                        {
                            GUILayout.Label($"{entry.context.name}", textStyle, GUILayout.Width(entrywidth));
                        }
                        else if (!string.IsNullOrEmpty(entry.stackTrace))
                        {
                            GUILayout.Label($"{Utils.GetFirstLines(entry.traceLines, 0, m_configs.LogEntryStackTraceLineCount, true)}", textStyle, GUILayout.Width(entrywidth)); // TODO cache this line
                        }
                        else
                        {
                            GUILayout.Label($"{Utils.GetFirstLines(entry.messageLines, m_configs.LogEntryMessageLineCount, m_configs.LogEntryStackTraceLineCount, false)}", textStyle, GUILayout.Width(entrywidth)); // TODO cache this line
                        }
                    }
                }
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                // First Category space
                if (categoryColumn && entry.categories != null && entry.categories.Count > 0)
                {
                    GUILayout.BeginHorizontal(GUILayout.Width(categoryColumnWidth));
                    for (int i=0;i< Mathf.Min(m_configs.CategoryCountInLogList, entry.categories.Count);i++)
                    {
                        var category = entry.categories[i];
                        var categoryColor = m_categoryNameStyle.normal.textColor;
                        m_categoryNameStyle.normal.textColor = Color.Lerp(m_categoryNameStyle.normal.textColor, category.Color, m_configs.CategoryNameInLogListColorize);
                        GUILayout.Label(category.Name.ToString(), m_categoryNameStyle, GUILayout.ExpandWidth(true));
                        m_categoryNameStyle.normal.textColor = categoryColor;
                    }
                    GUILayout.EndHorizontal();
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
                    foreach (var category in entry.categories)
                    {
                        GUI.color = category.Color;
                        GUI.backgroundColor = Color.white;
                        GUI.contentColor = Color.white;
                        GUI.Box(new Rect(lastRect.xMax + i * categoryStripWidth, lastRect.yMin - 4, categoryStripWidth, ItemHeight), string.Empty, m_categoryColorStrip);
                        GUILayout.Space(categoryStripWidth);
                        i++;
                    }
                    GUI.contentColor = saveContentColor;
                    GUI.backgroundColor = saveBGColor;
                    GUI.color = saveColor;
                }
            }
            GUILayout.EndHorizontal();

            Rect r = GUILayoutUtility.GetLastRect();
            if (GUI.Button(r, GUIContent.none, GUIStyle.none))
            {
                if (entry.context != null)
                {
#if UNITY_EDITOR
                    EditorGUIUtility.PingObject(entry.context);
#endif
                }
                if (m_selectedEntries.Count > 0 && m_selectedEntries[0] == entry && DateTime.Now.Ticks - m_lastClick.Ticks < m_doubleClickSpeed)
                {
                    HandleDoubleClick(entry);
                }
                m_lastClick = DateTime.Now;

                if (Event.current.shift && m_selectedEntries != null && m_selectedEntries.Count > 0)
                {
                    int startIdx = m_displayedEntries.IndexOf(m_selectedEntries[m_selectedEntries.Count - 1]);
                    int thisIdx = idx;
                    for(int i = startIdx; i <= thisIdx; i++)
                    {
                        if (!m_selectedEntries.Contains(m_displayedEntries[i]))
                        {
                            m_selectedEntries.Add(m_displayedEntries[i]);
                        }
                    }
                }
                else if (Event.current.control)
                {
                    if (m_selectedEntries.Contains(entry))
                    {
                        m_selectedEntries.Remove(entry);
                    }
                    else
                    {
                        m_selectedEntries.Add(entry);
                    }
                }
                else
                {
                    m_selectedEntries.Clear();
                    m_selectedEntries.Add(entry);
                }
                m_lastCLickIsDisplayList = true;

                if (m_configs.CopyOnSelect)
                {
                    CopySelection();
                }
            }
        }

        private void DisplayCollapseBubble(LogLevel level, int count, float collapseBubbleSize, float sidePaddings)
        {
            GUIStyle style;
            switch (level)
            {
                case LogLevel.Log:
                    style = m_collapseBubbleStyle;
                    break;
                case LogLevel.Warning:
                    style = m_collapseBubbleWarningStyle;
                    break;
                case LogLevel.Error:
                default:
                    style = m_collapseBubbleErrorStyle;
                    break;
            }
            GUILayout.Label(count.ToString(), style, GUILayout.ExpandWidth(false), GUILayout.Width(collapseBubbleSize), GUILayout.Height(23));
            GUILayout.Space(sidePaddings);
        }

        // TODO if not editor
        private void SelectableLabel(string text, GUIStyle textStyle, float currentX)
        {
            float width = m_configs.InspectorOnTheRight ? m_splitterPosition : EditorGUIUtility.currentViewWidth;
            float height = textStyle.CalcHeight(new GUIContent(text), width);
            var lastRect = GUILayoutUtility.GetLastRect();
            EditorGUI.SelectableLabel(new Rect(currentX, lastRect.yMax, width, height), text, textStyle);
            GUILayout.Space(height);
        }

        private void EditorSelectableLabelInvisible()
        {
            EditorGUI.SelectableLabel(new Rect(0,0,0,0), string.Empty);
        }

        private void FlagButton(LogLevel level, Texture2D icon, Texture2D iconGray, int counter)
        {
            bool hasFlag = (m_configs.LogLevelFilter & level) != 0;
            bool newFlagValue = GUILayout.Toggle(hasFlag, new GUIContent($" {(counter > 999 ? Strings.NineNineNinePlus : counter.ToString())}", (counter > 0 ? icon : iconGray)),
                m_toolbarIconButtonStyle,
                GUILayout.MaxWidth(GetFlagButtonWidthFromCounter(counter)), GUILayout.ExpandWidth(false)
                );
            if (hasFlag != newFlagValue)
            {
                m_configs.LogLevelFilter ^= level;
                m_triggerFilteredEntryComputation = true;
            }
        }

        private void Splitter()
        {
            int splitterSize = 5;
            if (m_configs.InspectorOnTheRight)
            {
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false), GUILayout.Width(1 + 2 * splitterSize));
                GUILayout.Space(splitterSize);
                GUILayout.Box(string.Empty,
                     GUILayout.Width(1),
                     GUILayout.MaxWidth(1),
                     GUILayout.MinWidth(1),
                     GUILayout.ExpandHeight(true));
                GUILayout.Space(splitterSize);
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.BeginVertical(GUILayout.ExpandHeight(false), GUILayout.Height(1 + 2 * splitterSize));
                GUILayout.Space(splitterSize);
                GUILayout.Box(string.Empty,
                     GUILayout.Height(1),
                     GUILayout.MaxHeight(1),
                     GUILayout.MinHeight(1),
                     GUILayout.ExpandWidth(true));
                GUILayout.Space(splitterSize);
                GUILayout.EndVertical();
            }

            m_splitterRect = GUILayoutUtility.GetLastRect();
            EditorGUIUtility.AddCursorRect(new Rect(m_splitterRect), m_configs.InspectorOnTheRight ? MouseCursor.ResizeHorizontal : MouseCursor.ResizeVertical); // TODO Editor
        }

#endregion GUI Components

#endregion GUI

                #region Utilities

        private void ComputeCollapsedEntries(List<ConsoleLogEntry> filteredEntries)
        {
            m_collapsedEntries = new List<ConsoleLogEntry>();

            for (int i = 0; i < filteredEntries.Count; i++)
            {
                bool found = false;
                int foundIdx = 0;
                for (int j = 0; j < m_collapsedEntries.Count; j++)
                {
                    if (m_collapsedEntries[j].originalMessage == filteredEntries[i].originalMessage)
                    {
                        foundIdx = j;
                        found = true;
                    }
                }
                if (found)
                {
                    m_collapsedEntries[foundIdx] = new ConsoleLogEntry()
                    {
                        count = m_collapsedEntries[foundIdx].count + 1,
                        date = m_collapsedEntries[foundIdx].date,
                        message = m_collapsedEntries[foundIdx].message,
                        level = m_collapsedEntries[foundIdx].level,
                        stackTrace = m_collapsedEntries[foundIdx].stackTrace,
                        timestamp = m_collapsedEntries[foundIdx].timestamp,
                        messageLines = m_collapsedEntries[foundIdx].messageLines,
                        traceLines = m_collapsedEntries[foundIdx].traceLines,
                        categories = m_collapsedEntries[foundIdx].categories,
                        context = m_collapsedEntries[foundIdx].context,
                        assetPath = m_collapsedEntries[foundIdx].assetPath,
                        assetLine = m_collapsedEntries[foundIdx].assetLine,
                        originalStackTrace = m_collapsedEntries[foundIdx].originalStackTrace,
                        originalMessage = m_collapsedEntries[foundIdx].originalMessage,
                        unityIndex = m_collapsedEntries[foundIdx].unityIndex,
                        unityMode = m_collapsedEntries[foundIdx].unityMode,
                    };
                }
                else
                {
                    m_collapsedEntries.Add(filteredEntries[i]);
                }
            }
        }

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

        private void GetCounters(List<ConsoleLogEntry> entries, out int logCounter, out int warnCounter, out int errCounter)
        {
            if(entries == null || entries.Count == 0)
            {
                logCounter = 0;
                warnCounter = 0;
                errCounter = 0;
                return;
            }
            logCounter = warnCounter = errCounter = 0;
            foreach (var entry in entries)
            {
                switch (entry.level)
                {
                    case LogLevel.Log:
                        logCounter++;
                        break;
                    case LogLevel.Warning:
                        warnCounter++;
                        break;
                    case LogLevel.Error:
                    case LogLevel.Exception:
                    case LogLevel.Assert:
                        errCounter++;
                        break;
                }
            }
        }

        private Texture GetEntryIcon(ConsoleLogEntry entry)
        {
            if (entry.level.HasFlag(LogLevel.Log)) { return m_iconInfo; }
            if (entry.level.HasFlag(LogLevel.Warning)) { return m_iconWarning; }
            if (m_configs.ShowCustomErrorIcons)
            {
                if (entry.level.HasFlag(LogLevel.Exception)) { return m_exceptionIcon; }
                if (entry.level.HasFlag(LogLevel.Assert)) { return m_assertIcon; }
            }
            return m_iconError;
        }

                #endregion Utilities

        private void CheckForUnitySync()
        {
            int logLogRef = 0, warnLogRef = 0, errLogRef = 0;
            object[] counters = new object[] { logLogRef, warnLogRef, errLogRef };
            GetCountsByType.Invoke(null, counters); // TODO check if this works in game

            int logLog  = (int)counters[0];
            int warnLog = (int)counters[1];
            int errLog  = (int)counters[2];

            if (m_logLog != logLog || m_warnLog != warnLog || m_errLog != errLog)
            {
                m_triggerSyncWithUnityComputation = true;

                m_logLog = logLog;
                m_warnLog = warnLog;
                m_errLog = errLog;
            }
        }

        private void Update()
        {
            CheckForUnitySync();

            if (m_callForRepaint)
            {
                Repaint();
            }

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

        public void AddItemsToMenu(GenericMenu menu)
        {
            //menu.AddItem(EditorGUIUtility.TrTextContent("Open Player Log"), false, UnityEditorInternal.InternalEditorUtility.OpenPlayerConsole);
            menu.AddItem(EditorGUIUtility.TrTextContent("Open Editor Log"), false, UnityEditorInternal.InternalEditorUtility.OpenEditorConsole);
            menu.AddItem(EditorGUIUtility.TrTextContent("Export All Logs to File"), false, ExportAllToFile);
            menu.AddItem(EditorGUIUtility.TrTextContent("Export Filtered Logs to File"), false, ExportFilteredToFile);
        }

        private void ExportFilteredToFile()
        {
            ExportToFile(m_filteredEntries, "FilteredLog.txt");
        }

        private void ExportAllToFile()
        {
            ExportToFile(m_entries, "ConsoleLog");
        }

        private void ExportToFile(List<ConsoleLogEntry> list, string title)
        {
            var path = EditorUtility.SaveFilePanel(
            "Save Logs",
            "",
            title,
            "txt");

            if (path.Length != 0 && list != null)
            {
                if(list.Count == 0)
                {
                    Debug.LogWarning("No entries to export.");
                    return;
                }
                string result = string.Empty;

                foreach (var entry in list)
                {
                    result += entry.GetExportString() + Environment.NewLine + Environment.NewLine;
                }
                File.WriteAllBytes(path, System.Text.Encoding.ASCII.GetBytes(result));
            }
        }
    }
}