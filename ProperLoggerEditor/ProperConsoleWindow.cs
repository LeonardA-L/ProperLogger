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

        #endregion Consts

        #region Configs
        public Rect WindowRect => default;

        private ConfigsProvider m_configs = EditorConfigs.Instance;
        public ConfigsProvider Config => m_configs;


        public bool AutoScroll { get; set; } = true;
        
        public bool SearchMessage { get; set; } = true;

        public bool IsDarkSkin { get; set; } = false;

        public bool PurgeGetLinesCache { get; set; } = true;

        #endregion Configs

        public bool NeedRegexRecompile { get; set; } = false;
        public DateTime LastRegexRecompile { get; set; }
        public bool CallForRepaint { get; set; } = false;

        #region Logs

        public List<ConsoleLogEntry> Entries { get; set; } = null;
        public List<ConsoleLogEntry> FilteredEntries { get; set; } = null;
        private List<ConsoleLogEntry> m_displayedEntries = null;
        public List<ConsoleLogEntry> DisplayedEntries { get => m_displayedEntries; set { m_displayedEntries = value; } }
        public List<ConsoleLogEntry> CollapsedEntries { get; set; } = null;
        public bool TriggerFilteredEntryComputation { get; set; } = false;
        public bool TriggerSyncWithUnityComputation { get; set; } = false;
        public CustomLogHandler LogHandler { get; set; } = null;
        public List<PendingContext> PendingContexts { get; set; } = null;
        public object EntriesLock { get; set; } = null;
        public bool Listening { get; set; } = false;
        #region Filters

        public string SearchString { get; set; } = null;
        public string[] SearchWords { get; set; } = null;
        public List<string> InactiveCategories { get; set; } = null;

        #endregion Filters

        #endregion Logs

        #region Layout

        //private int m_selectedIndex = -1;
        public List<ConsoleLogEntry> SelectedEntries { get; set; } = null;
        public int DisplayedEntriesCount { get; set; } = -1;
        public DateTime LastClick { get; set; } = default;

        public Vector2 EntryListScrollPosition { get; set; }
        public Vector2 InspectorScrollPosition { get; set; }

        public float SplitterPosition { get; set; } = 0;
        public Rect SplitterRect { get; set; } = default;
        public bool SplitterDragging { get; set; } = false;
        public float InnerScrollableHeight { get; set; } = 0;
        public float OuterScrollableHeight { get; set; } = 0;

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

        public int LogLog { get => m_logLog; set { m_logLog = value; } }
        public int WarnLog { get => m_warnLog; set { m_warnLog = value; } }
        public int ErrLog { get => m_errLog; set { m_errLog = value; } }

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
        public GUIStyle EntryIconStyle { get; set; } = null;

        #endregion Caches
        #endregion Members

        #region Properties

        private static ProperConsoleWindow m_instance = null;
        internal static ProperConsoleWindow Instance => m_instance;
        private Type LogEntries => logEntries ?? (logEntries = UnityAssembly.GetType(Strings.LogEntries));
        private Type LogEntry => logEntry ?? (logEntry = UnityAssembly.GetType(Strings.LogEntry));
        private Assembly UnityAssembly => assembly ?? (assembly = Assembly.GetAssembly(typeof(UnityEditor.ActiveEditorTracker)));
        private MethodInfo GetCountsByType => getCountsByType ?? (getCountsByType = LogEntries.GetMethod(Strings.GetCountsByType));
        private MethodInfo GetCount => getCount ?? (getCount = LogEntries.GetMethod(Strings.GetCount));
        private MethodInfo RowGotDoubleClicked => rowGotDoubleClicked ?? (rowGotDoubleClicked = LogEntries.GetMethod(Strings.RowGotDoubleClicked, new[] { typeof(int) }));
        private MethodInfo ClearEntries => clearEntries ?? (clearEntries = LogEntries.GetMethod(Strings.Clear));
        private MethodInfo SetUnityConsoleFlag => setUnityConsoleFlag ?? (setUnityConsoleFlag = LogEntries.GetMethod(Strings.SetUnityConsoleFlag, new[] { typeof(int), typeof(bool) }));


        private MethodInfo GetEntryInternal => getEntryInternal ?? (getEntryInternal = LogEntries.GetMethod(Strings.GetEntryInternal, new[] { typeof(int), LogEntry }));
        private MethodInfo StartGettingEntries => startGettingEntries ?? (startGettingEntries = LogEntries.GetMethod(Strings.StartGettingEntries));
        private MethodInfo EndGettingEntries => endGettingEntries ?? (endGettingEntries = LogEntries.GetMethod(Strings.EndGettingEntries));

        // Unused
        public EOpenOnError OpenConsoleOnError => EOpenOnError.Never;
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
            AutoScroll = true;
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

        public void LoadIcons()
        {
            // TODO check out EditorGUIUtility.Load
            IconInfo = (Texture2D)LoadIcon.Invoke(null, new object[] { "console.infoicon" });
            IconWarning = (Texture2D)LoadIcon.Invoke(null, new object[] { "console.warnicon" });
            IconError = (Texture2D)LoadIcon.Invoke(null, new object[] { "console.erroricon" });

            IconInfoGray = (Texture2D)LoadIcon.Invoke(null, new object[] { "console.infoicon.inactive.sml" });
            IconWarningGray = (Texture2D)LoadIcon.Invoke(null, new object[] { "console.warnicon.inactive.sml" });
            IconErrorGray = (Texture2D)LoadIcon.Invoke(null, new object[] { "console.erroricon.inactive.sml" });

            IconConsole = (Texture2D)LoadIcon.Invoke(null, new object[] { "UnityEditor.ConsoleWindow" });

            ClearIcon = EditorUtils.LoadAssetByName<Texture2D>(Strings.ClearIcon + (IsDarkSkin ? "_d" : ""));
            CollapseIcon = EditorUtils.LoadAssetByName<Texture2D>(Strings.CollapseIcon + (IsDarkSkin ? "_d" : ""));
            ClearOnBuildIcon = EditorUtils.LoadAssetByName<Texture2D>(Strings.ClearOnBuildIcon + (IsDarkSkin ? "_d" : ""));
            ClearOnPlayIcon = EditorUtils.LoadAssetByName<Texture2D>(Strings.ClearOnPlayIcon + (IsDarkSkin ? "_d" : ""));
            ErrorPauseIcon = EditorUtils.LoadAssetByName<Texture2D>(Strings.ErrorPauseIcon + (IsDarkSkin ? "_d" : ""));
            RegexSearchIcon = EditorUtils.LoadAssetByName<Texture2D>(Strings.RegexSearchIcon + (IsDarkSkin ? "_d" : ""));
            CaseSensitiveIcon = EditorUtils.LoadAssetByName<Texture2D>(Strings.CaseSensitiveIcon + (IsDarkSkin ? "_d" : ""));
            AdvancedSearchIcon = EditorUtils.LoadAssetByName<Texture2D>(Strings.AdvancedSearchIcon + (IsDarkSkin ? "_d" : ""));

            ExceptionIcon = EditorUtils.LoadAssetByName<Texture2D>(Strings.ExceptionIcon + (IsDarkSkin ? "_d" : ""));
            AssertIcon = EditorUtils.LoadAssetByName<Texture2D>(Strings.AssertIcon + (IsDarkSkin ? "_d" : ""));

            //m_exceptionIcon = (Texture2D)LoadIcon.Invoke(null, new object[] { "ExceptionIcon" });
            //m_assertIcon = (Texture2D)LoadIcon.Invoke(null, new object[] { "AssertIcon" });
        }

        public void HandleDoubleClick(ConsoleLogEntry entry)
        {
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

        public void SyncWithUnityEntries()
        {
            List<ConsoleLogEntry> newConsoleEntries = new List<ConsoleLogEntry>();
            int count = (int)GetCount.Invoke(null, null);

            StartGettingEntries.Invoke(null, null);
            int firstIndex = 0;
            object entry = Activator.CreateInstance(LogEntry);
            for (int i = 0; i < count; i++)
            {
                object[] objparameters = new object[] { i, entry };
                bool result = (bool)GetEntryInternal.Invoke(null, objparameters);
                if (result)
                {
                    CustomLogEntry unityEntry = ConvertUnityLogEntryToCustomLogEntry(entry, LogEntry);
                    bool found = false;
                    for(int j = firstIndex; j < Entries.Count; j++)
                    {
                        var consoleEntry = Entries[j];
                        if (CompareEntries(unityEntry, consoleEntry))
                        {
                            found = true;
                            consoleEntry.assetPath = unityEntry.file;
                            consoleEntry.assetLine = unityEntry.line.ToString();
                            consoleEntry.unityMode = unityEntry.mode;
                            consoleEntry.unityIndex = i;
                            newConsoleEntries.Add(consoleEntry);
                            firstIndex++;
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

            EndGettingEntries.Invoke(null, null);

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
            if(ProperConsoleGameWindow.Instance != null)
            {
                ProperConsoleGameWindow.Instance.Clear();
            }
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
            C.DoGui(this);
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

        public void SelectableLabel(string text, GUIStyle textStyle, float currentX)
        {
            float width = m_configs.InspectorOnTheRight ? SplitterPosition : EditorGUIUtility.currentViewWidth;
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
            object[] counters = new object[] { warnLogRef, errLogRef, logLogRef };
            GetCountsByType.Invoke(null, counters); // TODO check if this works in game

            int logLog  = (int)counters[2];
            int warnLog = (int)counters[1];
            int errLog  = (int)counters[0];

            if (m_logLog != logLog || m_warnLog != warnLog || m_errLog != errLog)
            {
                TriggerSyncWithUnityComputation = true;

                m_logLog = logLog;
                m_warnLog = warnLog;
                m_errLog = errLog;
            }
        }

        [Obfuscation(Exclude = true)]
        private void Update()
        {
            CheckForUnitySync();

            if (CallForRepaint)
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
            ExportToFile(FilteredEntries, "FilteredLog.txt");
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
            EditorApplication.delayCall += new EditorApplication.CallbackFunction(Repaint);
        }

        public void ToggleSettings()
        {
            if (GUILayout.Button(PluginSettingsButtonContent))
            {
                SettingsService.OpenUserPreferences(ProperLoggerCustomSettingsProvider.s_pathToPreferences);
            }
        }

        public void ExternalEditorSelectableLabelInvisible()
        {
            EditorSelectableLabelInvisible();
        }
        public bool ExternalDisplayCloseButton() => false;
    }
}