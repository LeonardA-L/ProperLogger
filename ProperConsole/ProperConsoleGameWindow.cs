using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using C = ProperLogger.CommonMethods;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ProperLoggerEditor")]
[assembly: ObfuscateAssembly(true)]

namespace ProperLogger
{
    [Obfuscation(Exclude = true, ApplyToMembers = false)]
    internal class ProperConsoleGameWindow : ImGuiWindow<ProperConsoleGameWindow>, ILogObserver, IProperLogger
    {
        public bool IsGame => true;

        protected override string WindowName => "Proper Logger";

        #region Configs

        private ConfigsProvider m_configs = new PlayerConfigs();
        public ConfigsProvider Config => m_configs;

        public bool AutoScroll { get; set; } = true;

        public bool SearchMessage { get; set; } = true;

        public bool IsDarkSkin { get; set; } = false;

        #endregion Configs

        public bool NeedRegexRecompile { get; set; } = false;
        public DateTime LastRegexRecompile { get; set; }
        public bool CallForRepaint { get; set; } = false;

        private PlayerSettingsWindow m_settingsWindow = null;
        private PlayerSettingsWindow SettingsWindow => m_settingsWindow ?? (m_settingsWindow = GetComponent<PlayerSettingsWindow>());

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
        public List<LogCategory> InactiveCategories { get; set; } = null;

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

        [SerializeField, Obfuscation(Exclude = true)]
#if !DEBUG
        [HideInInspector]
#endif
        private Texture2D m_iconInfo;
        [SerializeField, Obfuscation(Exclude = true)]
#if !DEBUG
        [HideInInspector]
#endif
        private Texture2D m_iconWarning;
        [SerializeField, Obfuscation(Exclude = true)]
#if !DEBUG
        [HideInInspector]
#endif
        private Texture2D m_iconError;

        [SerializeField, Obfuscation(Exclude = true)]
#if !DEBUG
        [HideInInspector]
#endif
        private Texture2D m_iconInfoGray;
        [SerializeField, Obfuscation(Exclude = true)]
#if !DEBUG
        [HideInInspector]
#endif
        private Texture2D m_iconWarningGray;
        [SerializeField, Obfuscation(Exclude = true)]
#if !DEBUG
        [HideInInspector]
#endif
        private Texture2D m_iconErrorGray;

        [SerializeField, Obfuscation(Exclude = true)]
#if !DEBUG
        [HideInInspector]
#endif
        private Texture2D m_clearIcon;
        [SerializeField, Obfuscation(Exclude = true)]
#if !DEBUG
        [HideInInspector]
#endif
        private Texture2D m_collapseIcon;
        [SerializeField, Obfuscation(Exclude = true)]
#if !DEBUG
        [HideInInspector]
#endif
        private Texture2D m_clearOnPlayIcon;
        [SerializeField, Obfuscation(Exclude = true)]
#if !DEBUG
        [HideInInspector]
#endif
        private Texture2D m_errorPauseIcon;
        [SerializeField, Obfuscation(Exclude = true)]
#if !DEBUG
        [HideInInspector]
#endif
        private Texture2D m_regexSearchIcon;
        [SerializeField, Obfuscation(Exclude = true)]
#if !DEBUG
        [HideInInspector]
#endif
        private Texture2D m_caseSensitiveIcon;
        [SerializeField, Obfuscation(Exclude = true)]
#if !DEBUG
        [HideInInspector]
#endif
        private Texture2D m_advancedSearchIcon;
        [SerializeField, Obfuscation(Exclude = true)]
#if !DEBUG
        [HideInInspector]
#endif
        private Texture2D m_exceptionIcon;
        [SerializeField, Obfuscation(Exclude = true)]
#if !DEBUG
        [HideInInspector]
#endif
        private Texture2D m_assertIcon;

        #endregion Loaded Textures

        [SerializeField, Obfuscation(Exclude = true)]
        private LogCategoriesConfig m_categoriesAsset = null;
        public LogCategoriesConfig CategoriesAsset
        {
            get => m_categoriesAsset;
            set
            {
                m_categoriesAsset = value;
            }
        }

        [SerializeField, Obfuscation(Exclude = true)]
        private bool m_hideUnityBuiltInConsole = true;

        [SerializeField, Obfuscation(Exclude = true)]
        private bool m_openConsoleOnError = true;
        public bool OpenConsoleOnError => m_openConsoleOnError;
        public bool Active => m_active;
        public Rect WindowRect => m_windowRect;

        #region Caches

        public Regex SearchRegex { get; set; } = null;

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

        public Texture2D IconInfo { get => m_iconInfo; set { } }
        public Texture2D IconWarning { get => m_iconWarning; set { } }
        public Texture2D IconError { get => m_iconError; set { } }
        public Texture2D IconInfoGray { get => m_iconInfoGray; set { } }
        public Texture2D IconWarningGray { get => m_iconWarningGray; set { } }
        public Texture2D IconErrorGray { get => m_iconErrorGray; set { } }
        public Texture2D IconConsole { get => null; set { } }
        public Texture2D ClearIcon { get => m_clearIcon; set { } }
        public Texture2D CollapseIcon { get => m_collapseIcon; set { } }
        public Texture2D ClearOnPlayIcon { get => m_clearOnPlayIcon; set { } }
        public Texture2D ClearOnBuildIcon { get => null; set { } }
        public Texture2D ErrorPauseIcon { get => m_errorPauseIcon; set { } }
        public Texture2D RegexSearchIcon { get => m_regexSearchIcon; set { } }
        public Texture2D CaseSensitiveIcon { get => m_caseSensitiveIcon; set { } }
        public Texture2D AdvancedSearchIcon { get => m_advancedSearchIcon; set { } }
        public Texture2D ExceptionIcon { get=>m_exceptionIcon; set { } }
        public Texture2D AssertIcon { get=>m_assertIcon; set { } }

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

        public bool ShowCategoryFilter { get; set; } = false;
        public Rect CategoryFilterRect { get; private set; } = default;
        public Rect CategoryToggleRect { get; private set; } = default;

        #endregion Caches

        [Obfuscation(Exclude = true)]
        protected override void Awake()
        {
            base.Awake();
            Entries = Entries ?? new List<ConsoleLogEntry>();
            PendingContexts = PendingContexts ?? new List<PendingContext>();
            SelectedEntries = SelectedEntries ?? new List<ConsoleLogEntry>();
            Listening = false;
            EntriesLock = new object();
            TriggerFilteredEntryComputation = true;
            C.InitListener(this);
            AutoScroll = true;

            NeedRegexRecompile = true;
        }

        [Obfuscation(Exclude = true)]
        protected override void OnDestroy()
        {
            C.RemoveListener(this);
            C.ClearGUIContents(this);
            base.OnDestroy();
        }

        [Obfuscation(Exclude = true)]
        protected override void OnDisable()
        {
            if (m_active)
            {
                Toggle();
            }
            base.OnDisable();
        }

        [Obfuscation(Exclude = true)]
        protected override void Update()
        {
            base.Update();

            Debug.developerConsoleVisible = false;

            C.RegexCompilation(this);
        }

        protected override void OnWindowEnabled()
        {
            ShowCategoryFilter = false;
            C.ClearStyles(this);
            base.OnWindowEnabled();
        }

        protected override void OnWindowDisabled()
        {
            ShowCategoryFilter = false;
            C.ClearStyles(this);
            C.ClearGUIContents(this);
            base.OnWindowDisabled();
        }

        public void Clear()
        {
            lock (EntriesLock)
            {
                m_logLog = 0;
                m_warnLog = 0;
                m_errLog = 0;
                Entries.Clear();
                PendingContexts.Clear();
                SelectedEntries.Clear();
            }
            TriggerFilteredEntryComputation = true;
        }

        [Obfuscation(Exclude = true)]
        protected override void OnGUI()
        {
            if (m_hideUnityBuiltInConsole)
            {
                Debug.developerConsoleVisible = false;
            }
            base.OnGUI();
        }

        protected override void DoGui(int windowID)
        {
            C.DoGui(this);
            base.DoGui(windowID);
        }


        private Vector2 ComputeCategoryDropdownPosition(Rect dropdownRect)
        {
            Vector2 dropdownOffset = new Vector2(29, 1);
            return new Vector2(dropdownRect.x + m_windowRect.x + dropdownOffset.x
                             , dropdownRect.y + m_windowRect.y + ShowCategoriesButtonRect.height + dropdownOffset.y);
        }

        public void DrawCategoriesWindow(Rect dropdownRect, Vector2 size)
        {
            ShowCategoryFilter = !ShowCategoryFilter;
            CategoryFilterRect = new Rect(ComputeCategoryDropdownPosition(dropdownRect), size);
            CategoryToggleRect = new Rect(ShowCategoriesButtonRect.x + m_windowRect.x, ShowCategoriesButtonRect.y + m_windowRect.y, ShowCategoriesButtonRect.width, ShowCategoriesButtonRect.height);
        }

        // TODO clickable and selectable?
        public void SelectableLabel(string text, GUIStyle textStyle, float currentX)
        {
            // TODO
            /*float width = m_configs.InspectorOnTheRight ? m_splitterPosition : EditorGUIUtility.currentViewWidth;
            float height = textStyle.CalcHeight(new GUIContent(text), width);
            var lastRect = GUILayoutUtility.GetLastRect();
            EditorGUI.SelectableLabel(new Rect(currentX, lastRect.yMax, width, height), text, textStyle);
            GUILayout.Space(height);*/
            GUILayout.Label(text, textStyle);
        }

        public void HandleDoubleClick(ConsoleLogEntry entry)
        {
#if UNITY_EDITOR
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

        public void ContextListener(LogType type, UnityEngine.Object context, string format, params object[] args)
        {
            C.ContextListener(this, type, context, format, args);
        }

        public void Listener(string condition, string stackTrace, LogType type)
        {
            C.Listener(this, condition, stackTrace, type, null, null);
        }

        public void SetTriggerFilteredEntryComputation()
        {
            TriggerFilteredEntryComputation = true;
        }

        public void ExternalToggle()
        {
            Toggle();
        }

        public void TriggerRepaint() { }

        public void ToggleSettings()
        {
            if (SettingsWindow != null)
            {
                if (GUILayout.Button(PluginSettingsButtonContent))
                {
                    SettingsWindow.Toggle();
                }
            }
        }

        public void ExternalEditorSelectableLabelInvisible() { }
        public void SyncWithUnityEntries() { }
        public void LoadIcons() { }
        public bool ExternalDisplayCloseButton()
        {
            return DisplayCloseButton();
        }
    }
}