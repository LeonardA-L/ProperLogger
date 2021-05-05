#if UNITY_EDITOR

using System.Collections;
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Reflection;
using System.IO;
using C = ProperLogger.CommonMethods;
#if UNITY_2020_1_OR_NEWER
using UnityEngine.Networking.PlayerConnection;
using UnityEditor.Networking.PlayerConnection;
#endif

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

        private double m_autoRepaintDebounce = 200;

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

        private DateTime LastSuccessfulRepaint { get; set; } = default;

#region Logs

        public List<ConsoleLogEntry> Entries { get; set; } = null;
        public List<ConsoleLogEntry> FilteredEntries { get; set; } = null;
        public List<ConsoleLogEntry> DisplayedEntries { get; set; }
        public List<ConsoleLogEntry> CollapsedEntries { get; set; } = null;
        public bool TriggerFilteredEntryComputation { get; set; } = false;
        public bool TriggerSyncWithUnityComputation { get; set; } = false;
        public CustomLogHandler LogHandler { get; set; } = null;
        public List<PendingContext> PendingContexts { get; set; } = null;
        public object EntriesLock { get; set; } = null;
        public bool Listening { get; set; } = false;
        public bool FilterOutUncategorized { get; set; } = false;

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
        public Vector2 SplitterDragStartPosition { get; set; } = default;
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
        MethodInfo m_loadIcon = null;
        MethodInfo LoadIcon
        {
            get
            {
                if(m_loadIcon != null)
                {
                    return m_loadIcon;
                }
                Type editorGuiUtility = typeof(EditorGUIUtility);
                m_loadIcon = editorGuiUtility.GetMethod("LoadIcon", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic);
                return m_loadIcon;
            }
        }

        public Regex SearchRegex { get; set; } = null;

        private Assembly m_assembly = null;
        private Type m_logEntries = null;
        private Type m_logEntry = null;
        private MethodInfo m_startGettingEntries = null;
        private MethodInfo m_endGettingEntries = null;
        private MethodInfo m_getEntryInternal = null;
        private MethodInfo m_getCountsByType = null;
        private MethodInfo m_getCount = null;
        private MethodInfo m_rowGotDoubleClicked = null;
        private MethodInfo m_clearEntries = null;
        private MethodInfo m_setUnityConsoleFlag = null;

        private FieldInfo m_messageField = null;
        private FieldInfo m_fileField = null;
        private FieldInfo m_lineField = null;
        private FieldInfo m_modeField = null;

        public LogCategoriesConfig LastMainThreadCategoriesConfig { get; set; }

        public int LogLog { get; set; } = 0;
        public int WarnLog { get; set; } = 0;
        public int ErrLog { get; set; } = 0;

        private System.Threading.Thread m_mainThread = null;
        public System.Threading.Thread MainThread => m_mainThread;

        public bool ShowCategoryFilter { get; set; } = false;
        public bool CategoryFilterButtonUp { get; set; } = true;
        public Rect CategoryFilterRect { get; private set; } = default;

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
        public GUIStyle RemoteConnectionUtilityStyle { get; set; } = null;
        public GUIStyle DropdownToggleStyle { get; set; } = null;

#endregion Caches
#if UNITY_2020_1_OR_NEWER
        IConnectionState m_attachProfilerState;
#endif
#endregion Members

#region Properties

        private static ProperConsoleWindow s_instance = null;
        internal static ProperConsoleWindow Instance => s_instance;
        private Type LogEntries => m_logEntries ?? (m_logEntries = UnityAssembly.GetType(Strings.LogEntries));
        private Type LogEntry => m_logEntry ?? (m_logEntry = UnityAssembly.GetType(Strings.LogEntry));
        private Assembly UnityAssembly => m_assembly ?? (m_assembly = Assembly.GetAssembly(typeof(UnityEditor.ActiveEditorTracker)));
        private MethodInfo GetCountsByType => m_getCountsByType ?? (m_getCountsByType = LogEntries.GetMethod(Strings.GetCountsByType));
        private MethodInfo GetCount => m_getCount ?? (m_getCount = LogEntries.GetMethod(Strings.GetCount));
        private MethodInfo RowGotDoubleClicked => m_rowGotDoubleClicked ?? (m_rowGotDoubleClicked = LogEntries.GetMethod(Strings.RowGotDoubleClicked, new[] { typeof(int) }));
        private MethodInfo ClearEntries => m_clearEntries ?? (m_clearEntries = LogEntries.GetMethod(Strings.Clear));
        private MethodInfo SetUnityConsoleFlag => m_setUnityConsoleFlag ?? (m_setUnityConsoleFlag = LogEntries.GetMethod(Strings.SetUnityConsoleFlag, new[] { typeof(int), typeof(bool) }));


        private MethodInfo GetEntryInternal => m_getEntryInternal ?? (m_getEntryInternal = LogEntries.GetMethod(Strings.GetEntryInternal, new[] { typeof(int), LogEntry }));
        private MethodInfo StartGettingEntries => m_startGettingEntries ?? (m_startGettingEntries = LogEntries.GetMethod(Strings.StartGettingEntries));
        private MethodInfo EndGettingEntries => m_endGettingEntries ?? (m_endGettingEntries = LogEntries.GetMethod(Strings.EndGettingEntries));
        public MethodInfo EditorDropdownToggle { get; set; } = null;
        public object[] m_clearButtonReflectionParameters = null;
        public object[] ClearButtonReflectionParameters => m_clearButtonReflectionParameters ?? (m_clearButtonReflectionParameters = new object[] { false, ClearButtonContent, DropdownToggleStyle });
        public GenericMenu ClearButtonMenu { get; set; } = null;

        // Unused
        public EOpenOnError OpenConsoleOnError => EOpenOnError.Never;
        public bool Active { get; }

#endregion Properties

#region Editor Window

        [MenuItem("Window/Proper Logger/Console")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            if (ProperConsoleWindow.s_instance != null)
            {
                ProperConsoleWindow.s_instance.Show(true);
            }
            else
            {
                ShowWindow();
            }
            ProperConsoleWindow.s_instance.Focus();
        }

        internal static void ShowWindow()
        {
            if (s_instance != null)
            {
                s_instance.Show(true);
                s_instance.Focus();
            }
            else
            {
                s_instance = ScriptableObject.CreateInstance<ProperConsoleWindow>();
                s_instance.Show(true);
                s_instance.Focus();
            }
        }

        [Obfuscation(Exclude = true)]
        private void OnEnable()
        {
            Utils.ClearAssemblies();
            m_mainThread = System.Threading.Thread.CurrentThread;
            Entries = Entries ?? new List<ConsoleLogEntry>();
            PendingContexts = PendingContexts ?? new List<PendingContext>();
            SelectedEntries = SelectedEntries ?? new List<ConsoleLogEntry>();
            CollapsedEntries = new List<ConsoleLogEntry>();
            Listening = false;
            EntriesLock = new object();
            s_instance = this;
            TriggerFilteredEntryComputation = true;
            TriggerSyncWithUnityComputation = true;
            EditorApplication.playModeStateChanged += ModeChanged;
            C.InitListener(this);
            LoadIcons();
            C.CacheGUIContents(this);
            C.ClearStyles(this);
            AutoScroll = true;
            ProperConsoleWindow.s_instance.titleContent = new GUIContent(Strings.WindowTitle, IconConsole);
            ResetUnityConsoleFlags();

#if UNITY_2020_1_OR_NEWER
            m_attachProfilerState = PlayerConnectionGUIUtility.GetConnectionState(this, OnRemotePlayerAttached);
#endif

            NeedRegexRecompile = true;

            LastMainThreadCategoriesConfig = Config.CurrentCategoriesConfig;
            ShowCategoryFilter = false;
        }

        [Obfuscation(Exclude = true)]
        private void OnDisable()
        {
#if UNITY_2020_1_OR_NEWER
            m_attachProfilerState.Dispose();
#endif
            C.RemoveListener(this);
            EditorApplication.playModeStateChanged -= ModeChanged;
            s_instance = null;
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

        internal void OnBuild()
        {
            if (m_configs.ClearOnBuild)
            {
                Clear();
            }
        }

        public void LoadIcons()
        {
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
                RowGotDoubleClicked.Invoke(null, new object[] { entry.unityIndex });
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
#if DEBUG
            Debug.Log(obj);
#endif
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
            if (m_messageField == null || m_fileField == null || m_lineField == null || m_modeField == null)
            {
                var props = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.Instance);
                foreach (var prop in props)
                {
                    if (prop.Name == "message")
                    {
                        m_messageField = prop;
                    }
                    if (prop.Name == "file")
                    {
                        m_fileField = prop;
                    }
                    if (prop.Name == "line")
                    {
                        m_lineField = prop;
                    }
                    if (prop.Name == "mode")
                    {
                        m_modeField = prop;
                    }
                }
            }
        }
        public CustomLogEntry ConvertUnityLogEntryToCustomLogEntry(object unityLogEntry)
        {
            return new CustomLogEntry()
            {
                line = (int)m_lineField.GetValue(unityLogEntry),
                message = (string)m_messageField.GetValue(unityLogEntry),
                file = (string)m_fileField.GetValue(unityLogEntry),
                mode = (int)m_modeField.GetValue(unityLogEntry),
            };
        }

        public void SyncWithUnityEntries()
        {
            List<ConsoleLogEntry> newConsoleEntries = new List<ConsoleLogEntry>();
            int count = (int)GetCount.Invoke(null, null);

            StartGettingEntries.Invoke(null, null);
            int firstIndex = -1;
            object entry = Activator.CreateInstance(LogEntry);
            PopulateLogEntryFields(LogEntry);
            for (int i = 0; i < count; i++)
            {
                object[] objparameters = new object[] { i, entry };
                if ((bool)GetEntryInternal.Invoke(null, objparameters))
                {
                    CustomLogEntry unityEntry = ConvertUnityLogEntryToCustomLogEntry(entry);
                    bool found = false;
                    for(int j = firstIndex+1; j < Entries.Count; j++)
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
                            firstIndex = j;
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
                PendingContexts.Clear();
                SelectedEntries.Clear();

                ClearEntries.Invoke(null, null);

                SyncWithUnityEntries();
            }
            TriggerFilteredEntryComputation = true;
#if !DEMO
            if(ProperConsoleGameWindow.Instance != null)
            {
                ProperConsoleGameWindow.Instance.Clear();
            }
#endif
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
            if (CategoryFilterButtonUp && ShowCategoryFilter && ShowCategoriesButtonRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown)
            {
                CategoryFilterButtonUp = false;
            }
            else if (!CategoryFilterButtonUp && Event.current.type == EventType.MouseDown)
            {
                CategoryFilterButtonUp = true;
            }

            C.DoGui(this);

            if(Event.current.type == EventType.Repaint)
            {
                LastSuccessfulRepaint = DateTime.Now;
            }
        }

#region GUI Components

        private Rect ComputeCategoryDropdownPosition(Rect dropdownRect)
        {
            Vector2 dropdownOffset = new Vector2(40, 23);
            return new Rect(dropdownRect.x + this.position.x, dropdownRect.y + this.position.y, dropdownOffset.x, ShowCategoriesButtonRect.height + dropdownOffset.y);
        }

        public void DrawCategoriesWindow(Rect dropdownRect, Vector2 size)
        {
            ShowCategoryFilter = true;
            CategoryFilterRect = ComputeCategoryDropdownPosition(dropdownRect);
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

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            EditorApplication.delayCall += () =>
            {
#if DEBUG
                Debug.Log("Reloaded Scripts");
#endif
                if (Instance != null)
                {
                    Instance.TriggerSyncWithUnityComputation = true;
                }
            };
        }

        internal void AfterAssetProcess()
        {
            EditorApplication.delayCall += () =>
            {
#if DEBUG
                Debug.Log("Reloaded Assets");
#endif
                Instance.TriggerSyncWithUnityComputation = true;
            };
        }

        [Obfuscation(Exclude = true)]
        private void Update()
        {
            //CheckForUnitySync();

            if((DateTime.Now - LastSuccessfulRepaint).TotalMilliseconds > m_autoRepaintDebounce)
            {
                RepaintImmediate();
            }

            C.RegexCompilation(this);
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            // TODO why is this commented?
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

        public void DoubleTriggerRepaint()
        {
            EditorApplication.delayCall += new EditorApplication.CallbackFunction(TriggerRepaint);
        }

        public void RepaintImmediate()
        {
            Repaint();
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

        private void OnRemotePlayerAttached(string player)
        {
            Debug.Log($"Successfuly connected to {player}");
        }

        public void ShowRemoteConnectionUtility()
        {
#if UNITY_2020_1_OR_NEWER
            PlayerConnectionGUILayout.ConnectionTargetSelectionDropdown(m_attachProfilerState, RemoteConnectionUtilityStyle);
#endif
        }
    }
}
#endif