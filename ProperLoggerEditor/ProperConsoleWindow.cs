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
using C = ProperLogger.CommonMethods;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ProperLogger")]

namespace ProperLogger
{
    //[Obfuscation(Exclude = true, ApplyToMembers = false)]
    internal class ProperConsoleWindow : EditorWindow, IHasCustomMenu, ILogObserver, IProperLogger
    {
        public bool IsGame => false;

        #region Members
        #region Consts

        private GUISkin m_skin = null;
        public GUISkin Skin => m_skin ?? (m_skin = EditorUtils.LoadAssetByName<GUISkin>(Strings.EditorSkin));

        [NonSerialized]
        private float m_doubleClickSpeed = 300 * 10000; // Could be a config ?

        #endregion Consts

        #region Configs

        private ConfigsProvider m_configs = new EditorConfigs();
        public ConfigsProvider Config => m_configs;


        private bool m_autoScroll = true;
        
        public bool SearchMessage { get; set; } = true;

        private bool m_isDarkSkin = false;

        #endregion Configs

        public bool NeedRegexRecompile { get; set; } = false;
        public DateTime LastRegexRecompile { get; set; }
        private bool m_callForRepaint = false;

        #region Logs

        public List<ConsoleLogEntry> Entries { get; set; } = null;
        private List<ConsoleLogEntry> m_filteredEntries = null;
        private List<ConsoleLogEntry> m_displayedEntries = null;
        public List<ConsoleLogEntry> CollapsedEntries { get; set; } = null;
        public bool TriggerFilteredEntryComputation { get; set; } = false;
        private bool m_triggerSyncWithUnityComputation = false;
        public CustomLogHandler LogHandler { get; set; } = null;
        public List<PendingContext> PendingContexts { get; set; } = null;
        public object EntriesLock { get; set; } = null;
        public bool Listening { get; set; } = false;
        #region Filters

        public string SearchString { get; set; } = null;
        public string[] SearchWords { get; set; } = null;
        public List<LogCategory> InactiveCategories { get; set; } = null;

        #endregion Filters

        #endregion Logs

        #region Layout

        //private int m_selectedIndex = -1;
        public List<ConsoleLogEntry> SelectedEntries { get; set; } = null;
        private int m_displayedEntriesCount = -1;
        private DateTime m_lastClick = default;

        private Vector2 m_entryListScrollPosition;
        private Vector2 m_inspectorScrollPosition;

        private float m_splitterPosition = 0;
        public Rect SplitterRect { get; set; } = default;
        private bool m_splitterDragging = false;
        private float m_innerScrollableHeight = 0;
        private float m_outerScrollableHeight = 0;

        public Rect ShowCategoriesButtonRect { get; set; } = default;
        public Rect ListDisplay { get; set; } = default;

        public Rect SearchFieldRect { get; set; } = default;
        public Rect ResetSearchButtonRect { get; set; } = default;

        public bool LastCLickIsDisplayList { get; set; } = false;

        #endregion Layout

        #region Loaded Textures

        public Texture2D IconInfo { get; set; } = null;
        public Texture2D IconWarning { get; set; } = null;
        public Texture2D IconError { get; set; } = null;
        public Texture2D IconInfoGray { get; set; } = null;
        public Texture2D IconWarningGray { get; set; } = null;
        public Texture2D IconErrorGray { get; set; } = null;
        public Texture2D IconConsole { get; set; } = null;
        public Texture2D ExceptionIcon { get; set; } = null;
        public Texture2D AssertIcon { get; set; } = null;


        public Texture2D ClearIcon { get; set; } = null;
        public Texture2D CollapseIcon { get; set; } = null;
        public Texture2D ClearOnPlayIcon { get; set; } = null;
        public Texture2D ClearOnBuildIcon { get; set; } = null;
        public Texture2D ErrorPauseIcon { get; set; } = null;
        public Texture2D RegexSearchIcon { get; set; } = null;
        public Texture2D CaseSensitiveIcon { get; set; } = null;
        public Texture2D AdvancedSearchIcon { get; set; } = null;

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

        public Regex SearchRegex { get; set; } = null;


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
        private MethodInfo setUnityConsoleFlag = null;
        private FieldInfo messageField = null;
        private FieldInfo fileField = null;
        private FieldInfo lineField = null;
        private FieldInfo modeField = null;

        private int m_logLog = 0;
        private int m_warnLog = 0;
        private int m_errLog = 0;

        public GUIContent ClearButtonContent { get; set; } = null;
        public GUIContent CollapseButtonContent { get; set; } = null;
        public GUIContent ErrorPauseButtonContent { get; set; } = null;
        public GUIContent ClearOnPlayButtonContent { get; set; } = null;
        public GUIContent ClearOnBuildButtonContent { get; set; } = null;
        public GUIContent AdvancedSearchButtonContent { get; set; } = null;
        public GUIContent CategoriesButtonContent { get; set; } = null;
        public GUIContent RegexSearchButtonNameOnlyContent { get; set; } = null;
        public GUIContent CaseSensitiveButtonContent { get; set; } = null;
        public GUIContent SearchInLogMessageButtonContent { get; set; } = null;
        public GUIContent SearchInObjectNameButtonContent { get; set; } = null;
        public GUIContent SearchInStackTraceButtonContent { get; set; } = null;
        public GUIContent PluginSettingsButtonContent { get; set; } = null;

        public GUIStyle OddEntry { get; set; } = null;
        public GUIStyle SelectedEntry { get; set; } = null;
        public GUIStyle SelectedEntryLabel { get; set; } = null;
        public GUIStyle EvenEntry { get; set; } = null;
        public GUIStyle EvenEntryLabel { get; set; } = null;
        public GUIStyle CategoryNameStyle { get; set; } = null;
        public GUIStyle CategoryColorStrip { get; set; } = null;
        public GUIStyle CollapseBubbleStyle { get; set; } = null;
        public GUIStyle CollapseBubbleWarningStyle { get; set; } = null;
        public GUIStyle CollapseBubbleErrorStyle { get; set; } = null;
        public GUIStyle ToolbarIconButtonStyle { get; set; } = null;
        public GUIStyle InspectorTextStyle { get; set; } = null;

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
        private MethodInfo SetUnityConsoleFlag => setUnityConsoleFlag ?? (setUnityConsoleFlag = LogEntries.GetMethod(Strings.SetUnityConsoleFlag));

        // Unused
        public bool OpenConsoleOnError => false;
        public bool Active { get; }

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

        [Obfuscation(Exclude = true)]
        private void OnEnable()
        {
            Entries = Entries ?? new List<ConsoleLogEntry>();
            PendingContexts = PendingContexts ?? new List<PendingContext>();
            SelectedEntries = SelectedEntries ?? new List<ConsoleLogEntry>();
            Listening = false;
            EntriesLock = new object();
            m_instance = this;
            TriggerFilteredEntryComputation = true;
            EditorApplication.playModeStateChanged += ModeChanged;
            C.InitListener(this);
            LoadIcons();
            C.CacheGUIContents(this);
            C.ClearStyles(this);
            m_autoScroll = true;
            ProperConsoleWindow.m_instance.titleContent = new GUIContent(Strings.WindowTitle, IconConsole);
            ResetUnityConsoleFlags();

            NeedRegexRecompile = true;
        }

        [Obfuscation(Exclude = true)]
        private void OnDisable()
        {
            C.RemoveListener(this);
            EditorApplication.playModeStateChanged -= ModeChanged;
            m_instance = null;
            C.ClearGUIContents(this);
        }

        private void ResetUnityConsoleFlags()
        {
            SetUnityConsoleFlag.Invoke(null, new object[] { 1 , false }); // Collapse
            SetUnityConsoleFlag.Invoke(null, new object[] { 2 , false }); // ClearOnPlay
            SetUnityConsoleFlag.Invoke(null, new object[] { 4 , false }); // ErrorPause
            SetUnityConsoleFlag.Invoke(null, new object[] { 8 , false }); // Verbose
            SetUnityConsoleFlag.Invoke(null, new object[] { 16 , false }); // StopForAssert
            SetUnityConsoleFlag.Invoke(null, new object[] { 32 , false }); // StopForError
            SetUnityConsoleFlag.Invoke(null, new object[] { 64 , false }); // Autoscroll
            SetUnityConsoleFlag.Invoke(null, new object[] { 128 , true }); // LogLevelLog
            SetUnityConsoleFlag.Invoke(null, new object[] { 256 , true }); // LogLevelWarning
            SetUnityConsoleFlag.Invoke(null, new object[] { 512 , true }); // LogLevelError
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
            // TODO check out EditorGUIUtility.Load
            IconInfo = (Texture2D)LoadIcon.Invoke(null, new object[] { "console.infoicon" });
            IconWarning = (Texture2D)LoadIcon.Invoke(null, new object[] { "console.warnicon" });
            IconError = (Texture2D)LoadIcon.Invoke(null, new object[] { "console.erroricon" });

            IconInfoGray = (Texture2D)LoadIcon.Invoke(null, new object[] { "console.infoicon.inactive.sml" });
            IconWarningGray = (Texture2D)LoadIcon.Invoke(null, new object[] { "console.warnicon.inactive.sml" });
            IconErrorGray = (Texture2D)LoadIcon.Invoke(null, new object[] { "console.erroricon.inactive.sml" });

            IconConsole = (Texture2D)LoadIcon.Invoke(null, new object[] { "UnityEditor.ConsoleWindow" });

            ClearIcon = EditorUtils.LoadAssetByName<Texture2D>(Strings.ClearIcon + (m_isDarkSkin ? "_d" : ""));
            CollapseIcon = EditorUtils.LoadAssetByName<Texture2D>(Strings.CollapseIcon + (m_isDarkSkin ? "_d" : ""));
            ClearOnBuildIcon = EditorUtils.LoadAssetByName<Texture2D>(Strings.ClearOnBuildIcon + (m_isDarkSkin ? "_d" : ""));
            ClearOnPlayIcon = EditorUtils.LoadAssetByName<Texture2D>(Strings.ClearOnPlayIcon + (m_isDarkSkin ? "_d" : ""));
            ErrorPauseIcon = EditorUtils.LoadAssetByName<Texture2D>(Strings.ErrorPauseIcon + (m_isDarkSkin ? "_d" : ""));
            RegexSearchIcon = EditorUtils.LoadAssetByName<Texture2D>(Strings.RegexSearchIcon + (m_isDarkSkin ? "_d" : ""));
            CaseSensitiveIcon = EditorUtils.LoadAssetByName<Texture2D>(Strings.CaseSensitiveIcon + (m_isDarkSkin ? "_d" : ""));
            AdvancedSearchIcon = EditorUtils.LoadAssetByName<Texture2D>(Strings.AdvancedSearchIcon + (m_isDarkSkin ? "_d" : ""));

            ExceptionIcon = EditorUtils.LoadAssetByName<Texture2D>(Strings.ExceptionIcon + (m_isDarkSkin ? "_d" : ""));
            AssertIcon = EditorUtils.LoadAssetByName<Texture2D>(Strings.AssertIcon + (m_isDarkSkin ? "_d" : ""));

            //m_exceptionIcon = (Texture2D)LoadIcon.Invoke(null, new object[] { "ExceptionIcon" });
            //m_assertIcon = (Texture2D)LoadIcon.Invoke(null, new object[] { "AssertIcon" });
        }

        public void HandleDoubleClick(ConsoleLogEntry entry)
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
            C.RemoveListener(this);
            PendingContexts?.Clear();
        }

        private void EnteredEditMode()
        {
            C.InitListener(this);
            PendingContexts?.Clear();
        }

        #endregion Mode Changes

        #endregion Editor Window

        #region Logs

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
                    for(int j = firstIndex; j < Entries.Count; j++)
                    {
                        if (foundEntries.Contains(j))
                        {
                            continue;
                        }

                        var consoleEntry = Entries[j];
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
                        var consoleEntry = C.Listener(this, unityEntry.message, null, GetLogTypeFromUnityMode(unityEntry.mode), unityEntry.file, unityEntry.line.ToString());
                        consoleEntry.unityMode = unityEntry.mode;
                        consoleEntry.message = Utils.ParseStackTrace(consoleEntry.originalMessage, out _, out _);
                        newConsoleEntries.Add(consoleEntry);
                    }
                }
            }

            endGettingEntries.Invoke(null, null);

            //lock (m_entries)
            {
                Entries.Clear();
                Entries.AddRange(newConsoleEntries);
            }
        }

        public void Listener(string condition, string stackTrace, LogType type)
        {
            C.Listener(this, condition, stackTrace, type, null, null);
        }

        public void Clear()
        {
            lock (EntriesLock)
            {
                //m_logCounter = 0;
                //m_warningCounter = 0;
                //m_errorCounter = 0;
                //m_entries.Clear();
                PendingContexts.Clear();
                SelectedEntries.Clear();

                ClearEntries.Invoke(null, null); // TODO check if this works in game

                SyncWithUnityEntries();
            }
            TriggerFilteredEntryComputation = true;
        }

        public void ContextListener(LogType type, UnityEngine.Object context, string format, params object[] args)
        {
            C.ContextListener(this, type, context, format, args);
        }

        #endregion Logs

        #region GUI

        [Obfuscation(Exclude = true)]
        void OnGUI()
        {
            DoGui();
        }

        private void DoGui()
        {
            C.HandleCopyToClipboard(this);
            EditorSelectableLabelInvisible();

            if(InspectorTextStyle == null)
            {
                C.CacheStyles(this);
            }

            m_callForRepaint = false;
            bool repaint = Event.current.type == EventType.Repaint;

            InactiveCategories?.Clear();
            InactiveCategories = m_configs.InactiveCategories;

            C.DisplayToolbar(this, ref m_callForRepaint);

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

            if (Entries.Count == 0) GUILayout.Space(10);

            if (m_triggerSyncWithUnityComputation)
            {
                lock (Entries)
                {
                    SyncWithUnityEntries();
                }
                TriggerFilteredEntryComputation = true;
                m_triggerSyncWithUnityComputation = false;
            }

            if (TriggerFilteredEntryComputation)
            {
                m_filteredEntries = Entries.FindAll(e => C.ValidFilter(this, e));
                if (m_configs.Collapse)
                {
                    C.ComputeCollapsedEntries(this, m_filteredEntries);
                }
                TriggerFilteredEntryComputation = false;
            }

            DisplayList(m_configs.Collapse ? CollapsedEntries : m_filteredEntries, out m_displayedEntries, totalWidth);

            if (m_displayedEntries.Count < m_displayedEntriesCount)
            {
                SelectedEntries.Clear();
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
                ListDisplay = GUILayoutUtility.GetLastRect();
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

            C.Splitter(this);

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
            if (SelectedEntries.Count > 0)
            {
                var entry = SelectedEntries[0];

                GUILayout.Space(1);
                float currentX = (GUILayoutUtility.GetLastRect()).xMin;

                string categoriesString = string.Empty;
                if (entry.categories != null && entry.categories.Count > 0)
                {
                    if (m_configs.CategoryDisplay.HasFlag(ECategoryDisplay.InInspector))
                    {
                        string format = "<color=#{1}>[{0}]</color> ";
                        categoriesString = string.Join(string.Empty, entry.categories.Select(c =>string.Format(format, c.Name, ColorUtility.ToHtmlStringRGB(Color.Lerp(c.Color, InspectorTextStyle.normal.textColor, m_configs.CategoryNameColorize)))));
                    }
                }

                SelectableLabel($"{categoriesString}{entry.message}", InspectorTextStyle, currentX); 

                if (entry.context != null)
                {
                    Color txtColor = InspectorTextStyle.normal.textColor;
                    if (!m_configs.ObjectNameColor.Equals(txtColor))
                    {
                        InspectorTextStyle.normal.textColor = m_configs.ObjectNameColor;
                    }
                    SelectableLabel(entry.context.name, InspectorTextStyle, currentX);
                    InspectorTextStyle.normal.textColor = txtColor;
                }
                if (!string.IsNullOrEmpty(entry.stackTrace))
                {
                    SelectableLabel(entry.stackTrace, InspectorTextStyle, currentX);
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
                Debug.Log($"Log {DateTime.Now.ToString()} {Listening}\r\nA\nB\nC\nD", Camera.main);
            }

            if (GUILayout.Button("Log Combat"))
            {
                Debug.Log($"[Combat] [Performance] Log {DateTime.Now.ToString()} {Listening}", Camera.main);
            }

            if (GUILayout.Button("Log Performance"))
            {
                Debug.Log($"[Performance] Log {DateTime.Now.ToString()} {Listening}", Camera.main);
            }

            if (GUILayout.Button("LogException"))
            {
                Debug.LogException(new Exception());
            }

            if (GUILayout.Button("LogWarning"))
            {
                Debug.LogWarning($"Warning {DateTime.Now.ToString()} {Listening} {m_autoScroll}");
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
                    Debug.Log($"Log {DateTime.Now.ToString()} {Listening}");
                }
            }
            if (GUILayout.Button("1000 syncs"))
            {
                lock (EntriesLock)
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
                        if (SplitterRect.Contains(Event.current.mousePosition))
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

#if UNITY_EDITOR
            if (m_isDarkSkin != EditorGUIUtility.isProSkin)
            {
                m_isDarkSkin = EditorGUIUtility.isProSkin;
                C.ClearStyles(this);
                C.ClearGUIContents(this);
                LoadIcons();
                C.CacheStyles(this);
                C.CacheGUIContents(this);
            }
#endif // UNITY_EDITOR
        }

        private void DisplayList(List<ConsoleLogEntry> filteredEntries, out List<ConsoleLogEntry> displayedEntries, float totalWidth)
        {
            int startI = 0;
            int endI = filteredEntries.Count;
            int lastVisibleIdx = 0;
            // Only display elements that are in view
            if (m_outerScrollableHeight + 100 <= m_innerScrollableHeight)
            {
                int firstVisibleIdx = Mathf.Clamp((int)(m_entryListScrollPosition.y / C.ItemHeight(this)) - 1, 0, filteredEntries.Count);
                lastVisibleIdx = Mathf.Clamp((int)((m_entryListScrollPosition.y + m_outerScrollableHeight) / C.ItemHeight(this)) + 1, 0, filteredEntries.Count);
                GUILayout.Space(firstVisibleIdx * C.ItemHeight(this));
                startI = firstVisibleIdx;
                endI = lastVisibleIdx;
            }

            for (int i = startI; i < endI; i++)
            {
                DisplayEntry(filteredEntries[i], i, totalWidth);
            }

            if (lastVisibleIdx != 0)
            {
                GUILayout.Space((filteredEntries.Count - lastVisibleIdx) * C.ItemHeight(this));
            }
            displayedEntries = filteredEntries;
        }

        #region GUI Components

        private Rect ComputeCategoryDropdownPosition(Rect dropdownRect)
        {
            Vector2 dropdownOffset = new Vector2(40, 23);
            return new Rect(dropdownRect.x + this.position.x, dropdownRect.y + this.position.y, dropdownOffset.x, ShowCategoriesButtonRect.height + dropdownOffset.y);
        }

        public void DrawCategoriesWindow(Rect dropdownRect, Vector2 size)
        {
            var window = (CategoriesFilterWindow)EditorWindow.CreateInstance<CategoriesFilterWindow>();
            window.ShowAsDropDown(ComputeCategoryDropdownPosition(dropdownRect), size);
            window.Repaint();
        }

        private void DisplaySearchToolbar()
        {

            GUILayout.BeginHorizontal(Strings.Toolbar);
            bool lastRegexSearch = m_configs.RegexSearch;
            m_configs.RegexSearch = GUILayout.Toggle(m_configs.RegexSearch, RegexSearchButtonNameOnlyContent, ToolbarIconButtonStyle, GUILayout.ExpandWidth(false));
            if(lastRegexSearch != m_configs.RegexSearch)
            {
                NeedRegexRecompile = true;
            }
            bool lastCaseSensitive = m_configs.CaseSensitive;
            m_configs.CaseSensitive = GUILayout.Toggle(m_configs.CaseSensitive, CaseSensitiveButtonContent, Strings.ToolbarButton, GUILayout.ExpandWidth(false));
            if (lastCaseSensitive != m_configs.CaseSensitive)
            {
                TriggerFilteredEntryComputation = true;
                NeedRegexRecompile = true;
            }
            bool lastSearchMessage = SearchMessage;
            SearchMessage = GUILayout.Toggle(SearchMessage, SearchInLogMessageButtonContent, Strings.ToolbarButton, GUILayout.ExpandWidth(false));
            if (lastSearchMessage != SearchMessage)
            {
                TriggerFilteredEntryComputation = true;
            }
            bool lastSearchObjectName = m_configs.SearchObjectName;
            m_configs.SearchObjectName = GUILayout.Toggle(m_configs.SearchObjectName, SearchInObjectNameButtonContent, Strings.ToolbarButton, GUILayout.ExpandWidth(false));
            if (lastSearchObjectName != m_configs.SearchObjectName)
            {
                TriggerFilteredEntryComputation = true;
            }
            bool lastSearchStackTRace = m_configs.SearchInStackTrace;
            m_configs.SearchInStackTrace = GUILayout.Toggle(m_configs.SearchInStackTrace, SearchInStackTraceButtonContent, Strings.ToolbarButton, GUILayout.ExpandWidth(false));
            if (lastSearchStackTRace != m_configs.SearchInStackTrace)
            {
                TriggerFilteredEntryComputation = true;
            }
            GUILayout.FlexibleSpace();
#if UNITY_EDITOR
            if (GUILayout.Button(PluginSettingsButtonContent))
            {
                SettingsService.OpenUserPreferences(ProperLoggerCustomSettingsProvider.s_pathToPreferences);
            }
#endif // UNITY_EDITOR
            GUILayout.EndHorizontal();
        }

        private void DisplayEntry(ConsoleLogEntry entry, int idx, float totalWidth)
        {
            GUIStyle currentStyle = OddEntry;
            GUIStyle textStyle = EvenEntryLabel;
            textStyle.normal.textColor = GUI.skin.label.normal.textColor;

            float imageSize = Math.Min(C.ItemHeight(this) - (2 * 3), 32); // We clamp it in case we display 3+ lines
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
                    categoryColumnWidth = CategoryNameStyle.CalcSize(new GUIContent(categoryString)).x + 10;
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

            
            if (SelectedEntries.Count > 0 && SelectedEntries.Contains(entry))
            {
                currentStyle = SelectedEntry;
                textStyle = SelectedEntryLabel;
            }
            else if (idx % 2 == 0)
            {
                currentStyle = EvenEntry;
            }

            var guiColor = GUI.color;
#if UNITY_EDITOR
            if (EditorGUIUtility.isProSkin)
            {
                GUI.color = new Color(1, 1, 1, 0.28f);
            }
#endif // UNITY_EDITOR
            GUILayout.BeginHorizontal(currentStyle, GUILayout.Height(C.ItemHeight(this)));
            {
            GUI.color = guiColor;
                //GUI.color = saveColor;
                // Picto space
                GUILayout.BeginHorizontal(GUILayout.Width(imageSize + sidePaddings));
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Box(C.GetEntryIcon(this, entry), GUIStyle.none, GUILayout.Width(imageSize), GUILayout.Height(imageSize));
                    GUILayout.FlexibleSpace();
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
                        var categoryColor = CategoryNameStyle.normal.textColor;
                        CategoryNameStyle.normal.textColor = Color.Lerp(CategoryNameStyle.normal.textColor, category.Color, m_configs.CategoryNameInLogListColorize);
                        GUILayout.Label(category.Name.ToString(), CategoryNameStyle, GUILayout.ExpandWidth(true));
                        CategoryNameStyle.normal.textColor = categoryColor;
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
                        GUI.Box(new Rect(lastRect.xMax + i * categoryStripWidth, lastRect.yMin - 4, categoryStripWidth, C.ItemHeight(this)), string.Empty, CategoryColorStrip);
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
                if (SelectedEntries.Count > 0 && SelectedEntries[0] == entry && DateTime.Now.Ticks - m_lastClick.Ticks < m_doubleClickSpeed)
                {
                    HandleDoubleClick(entry);
                }
                m_lastClick = DateTime.Now;

                if (Event.current.shift && SelectedEntries != null && SelectedEntries.Count > 0)
                {
                    int startIdx = m_displayedEntries.IndexOf(SelectedEntries[SelectedEntries.Count - 1]);
                    int thisIdx = idx;
                    for(int i = startIdx; i <= thisIdx; i++)
                    {
                        if (!SelectedEntries.Contains(m_displayedEntries[i]))
                        {
                            SelectedEntries.Add(m_displayedEntries[i]);
                        }
                    }
                }
                else if (Event.current.control)
                {
                    if (SelectedEntries.Contains(entry))
                    {
                        SelectedEntries.Remove(entry);
                    }
                    else
                    {
                        SelectedEntries.Add(entry);
                    }
                }
                else
                {
                    SelectedEntries.Clear();
                    SelectedEntries.Add(entry);
                }
                LastCLickIsDisplayList = true;

                if (m_configs.CopyOnSelect)
                {
                    C.CopySelection(this);
                }
            }
        }

        private void DisplayCollapseBubble(LogLevel level, int count, float collapseBubbleSize, float sidePaddings)
        {
            GUIStyle style;
            switch (level)
            {
                case LogLevel.Log:
                    style = CollapseBubbleStyle;
                    break;
                case LogLevel.Warning:
                    style = CollapseBubbleWarningStyle;
                    break;
                case LogLevel.Error:
                default:
                    style = CollapseBubbleErrorStyle;
                    break;
            }
            GUILayout.Label(count.ToString(), style, GUILayout.ExpandWidth(false), GUILayout.Width(collapseBubbleSize), GUILayout.Height(23));
            GUILayout.Space(sidePaddings);
        }

        public void SelectableLabel(string text, GUIStyle textStyle, float currentX)
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


#endregion GUI Components

#endregion GUI

        #region Utilities

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

        [Obfuscation(Exclude = true)]
        private void Update()
        {
            CheckForUnitySync();

            if (m_callForRepaint)
            {
                Repaint();
            }
            C.RegexCompilation(this);
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
            ExportToFile(Entries, "ConsoleLog");
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

        public void ExternalToggle() { }

        public void TriggerRepaint()
        {
            Repaint();
        }
    }
}