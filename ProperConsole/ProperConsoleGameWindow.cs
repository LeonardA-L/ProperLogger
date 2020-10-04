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
        public bool IsGame => false;

        [NonSerialized]
        private float m_doubleClickSpeed = 300 * 10000; // Could be a config ?
        [NonSerialized]
        private float m_regexCompileDebounce = 200 * 10000;
        protected override string WindowName => "Proper Logger";

        #region Configs

        private ConfigsProvider m_configs = new PlayerConfigs();
        public ConfigsProvider Config => m_configs;

        private bool m_autoScroll = true;

        private bool m_searchMessage = true;

        private bool m_isDarkSkin = false;

        #endregion Configs

        private bool m_needRegexRecompile = false; // TODO find region
        private DateTime m_lastRegexRecompile;
        private bool m_callForRepaint = false;

        private PlayerSettingsWindow m_settingsWindow = null;
        private PlayerSettingsWindow SettingsWindow => m_settingsWindow ?? (m_settingsWindow = GetComponent<PlayerSettingsWindow>());

        #region Logs

        private List<ConsoleLogEntry> m_entries = null;
        private List<ConsoleLogEntry> m_filteredEntries = null;
        private List<ConsoleLogEntry> m_displayedEntries = null;
        public List<ConsoleLogEntry> CollapsedEntries { get; set; } = null;
        private bool m_triggerFilteredEntryComputation = false;
        private bool m_triggerSyncWithUnityComputation = false;
        private CustomLogHandler m_logHandler = null;
        public CustomLogHandler LogHandler { get; set; } = null;
        public List<PendingContext> PendingContexts { get; set; } = null;
        private object m_entriesLock = null;
        public bool Listening { get; set; } = false;

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

        [SerializeField, Obfuscation(Exclude = true)]
        private Texture2D m_iconInfo;
        [SerializeField, Obfuscation(Exclude = true)]
        private Texture2D m_iconWarning;
        [SerializeField, Obfuscation(Exclude = true)]
        private Texture2D m_iconError;

        [SerializeField, Obfuscation(Exclude = true)]
        private Texture2D m_iconInfoGray;
        [SerializeField, Obfuscation(Exclude = true)]
        private Texture2D m_iconWarningGray;
        [SerializeField, Obfuscation(Exclude = true)]
        private Texture2D m_iconErrorGray;

        [SerializeField, Obfuscation(Exclude = true)]
        private Texture2D m_clearIcon;
        [SerializeField, Obfuscation(Exclude = true)]
        private Texture2D m_collapseIcon;
        [SerializeField, Obfuscation(Exclude = true)]
        private Texture2D m_clearOnPlayIcon;
        [SerializeField, Obfuscation(Exclude = true)]
        private Texture2D m_errorPauseIcon;
        [SerializeField, Obfuscation(Exclude = true)]
        private Texture2D m_regexSearchIcon;
        [SerializeField, Obfuscation(Exclude = true)]
        private Texture2D m_caseSensitiveIcon;
        [SerializeField, Obfuscation(Exclude = true)]
        private Texture2D m_advancedSearchIcon;
        [SerializeField, Obfuscation(Exclude = true)]
        private Texture2D m_exceptionIcon;
        [SerializeField, Obfuscation(Exclude = true)]
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

        #region Caches

        private Regex m_searchRegex = null;

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

        public Texture2D ClearIcon { get => m_clearIcon; set { } }
        public Texture2D CollapseIcon { get => m_collapseIcon; set { } }
        public Texture2D ClearOnPlayIcon { get => m_clearOnPlayIcon; set { } }
        public Texture2D ClearOnBuildIcon { get => null; set { } }
        public Texture2D ErrorPauseIcon { get => m_errorPauseIcon; set { } }
        public Texture2D RegexSearchIcon { get => m_regexSearchIcon; set { } }
        public Texture2D CaseSensitiveIcon { get => m_caseSensitiveIcon; set { } }
        public Texture2D AdvancedSearchIcon { get => m_advancedSearchIcon; set { } }

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

        private Regex m_categoryParse = null;
        private Regex CategoryParse => m_categoryParse ?? (m_categoryParse = new Regex("\\[([^\\s\\[\\]]+)\\]"));

        public bool ShowCategoryFilter { get; set; } = false;
        public Rect CategoryFilterRect { get; private set; } = default;
        public Rect CategoryToggleRect { get; private set; } = default;

        #endregion Caches

        [Obfuscation(Exclude = true)]
        protected override void Awake()
        {
            base.Awake();
            m_entries = m_entries ?? new List<ConsoleLogEntry>();
            PendingContexts = PendingContexts ?? new List<PendingContext>();
            m_selectedEntries = m_selectedEntries ?? new List<ConsoleLogEntry>();
            Listening = false;
            m_entriesLock = new object();
            m_triggerFilteredEntryComputation = true;
            C.InitListener(this);
            m_autoScroll = true;

            m_needRegexRecompile = true;
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

        protected override void OnWindowEnabled()
        {
            ShowCategoryFilter = false;
            C.ClearStyles(this);
            C.CacheStyles(this);
            base.OnWindowEnabled();
        }

        protected override void OnWindowDisabled()
        {
            ShowCategoryFilter = false;
            C.ClearStyles(this);
            C.ClearGUIContents(this);
            base.OnWindowDisabled();
        }

        internal void Clear()
        {
            lock (m_entriesLock)
            {
                m_logLog = 0;
                m_warnLog = 0;
                m_errLog = 0;
                m_entries.Clear();
                PendingContexts.Clear();
                m_selectedEntries.Clear();
            }
            m_triggerFilteredEntryComputation = true;
        }

        // This doesn't work in play mode
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
            HandleCopyToClipboard();

            if (InspectorTextStyle == null)
            {
                C.CacheStyles(this);
            }

            if(ClearButtonContent == null)
            {
                C.CacheGUIContents(this);
            }

            m_callForRepaint = false;
            bool repaint = Event.current.type == EventType.Repaint;

            m_inactiveCategories?.Clear();
            m_inactiveCategories = m_configs.InactiveCategories;

            if (DisplayCloseButton())
            {
                return;
            }

            DisplayToolbar(ref m_callForRepaint);

            if (m_configs.AdvancedSearchToolbar)
            {
                DisplaySearchToolbar();
            }

            float startY = 0;
            float totalWidth = m_windowRect.width;
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
            m_entryListScrollPosition = GUILayout.BeginScrollView(m_entryListScrollPosition, false, false, GUIStyle.none, Skin.verticalScrollbar);

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
                if (m_configs.Collapse)
                {
                    C.ComputeCollapsedEntries(this, m_filteredEntries);
                }
                m_triggerFilteredEntryComputation = false;
            }

            DisplayList(m_configs.Collapse ? CollapsedEntries : m_filteredEntries, out m_displayedEntries, totalWidth);

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
                        categoriesString = string.Join(string.Empty, entry.categories.Select(c => string.Format(format, c.Name, ColorUtility.ToHtmlStringRGB(Color.Lerp(c.Color, InspectorTextStyle.normal.textColor, m_configs.CategoryNameColorize)))));
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
            base.DoGui(windowID);
        }

        private void DisplayToolbar(ref bool callForRepaint)
        {
            GUILayout.BeginHorizontal(Strings.Toolbar);

            if (GUILayout.Button(ClearButtonContent, ToolbarIconButtonStyle, GUILayout.ExpandWidth(false)))
            {
                Clear();
                GUIUtility.keyboardControl = 0;
            }
            bool lastCollapse = m_configs.Collapse;
            m_configs.Collapse = GUILayout.Toggle(m_configs.Collapse, CollapseButtonContent, ToolbarIconButtonStyle, GUILayout.ExpandWidth(false));
            callForRepaint = m_configs.Collapse != lastCollapse;
            if (m_configs.Collapse != lastCollapse)
            {
                m_triggerFilteredEntryComputation = true;
                m_selectedEntries.Clear();
            }

#if UNITY_EDITOR
            m_configs.ErrorPause = GUILayout.Toggle(m_configs.ErrorPause, ErrorPauseButtonContent, ToolbarIconButtonStyle, GUILayout.ExpandWidth(false));
#endif

            string lastSearchTerm = m_searchString;

            GUI.enabled = !(Event.current.isMouse && m_resetSearchButtonRect.Contains(Event.current.mousePosition));
            m_searchString = GUILayout.TextField(m_searchString, "ToolbarSearchTextField"/*Strings.ToolbarSeachTextField*/);
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

            m_configs.AdvancedSearchToolbar = GUILayout.Toggle(m_configs.AdvancedSearchToolbar, AdvancedSearchButtonContent, ToolbarIconButtonStyle, GUILayout.ExpandWidth(false));
            Rect dropdownRect = GUILayoutUtility.GetLastRect();

            if (GUILayout.Button(CategoriesButtonContent, ToolbarIconButtonStyle, GUILayout.ExpandWidth(false)))
            {
                Vector2 dropdownOffset = new Vector2(29, 1);
                //Rect dropDownPosition = new Rect(Event.current.mousePosition.x + this.position.x, Event.current.mousePosition.y + this.position.y, dropdownOffset.x, m_showCategoriesButtonRect.height + dropdownOffset.y);
                Vector2 dropDownPosition = new Vector2(dropdownRect.x + m_windowRect.x + dropdownOffset.x
                                                     , dropdownRect.y + m_windowRect.y + m_showCategoriesButtonRect.height + dropdownOffset.y);

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
                size.y += 45;
                ShowCategoryFilter = !ShowCategoryFilter;
                CategoryFilterRect = new Rect(dropDownPosition, size);
                CategoryToggleRect = new Rect(m_showCategoriesButtonRect.x + m_windowRect.x, m_showCategoriesButtonRect.y + m_windowRect.y, m_showCategoriesButtonRect.width, m_showCategoriesButtonRect.height);
            }
            if (Event.current.type == EventType.Repaint)
            {
                m_showCategoriesButtonRect = GUILayoutUtility.GetLastRect();
            }

            GetCounters(m_entries, out int logCounter, out int warnCounter, out int errCounter);

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
            m_configs.RegexSearch = GUILayout.Toggle(m_configs.RegexSearch, RegexSearchButtonNameOnlyContent, ToolbarIconButtonStyle, GUILayout.ExpandWidth(false));
            if (lastRegexSearch != m_configs.RegexSearch)
            {
                m_needRegexRecompile = true;
            }
            bool lastCaseSensitive = m_configs.CaseSensitive;
            m_configs.CaseSensitive = GUILayout.Toggle(m_configs.CaseSensitive, CaseSensitiveButtonContent, ToolbarIconButtonStyle, GUILayout.ExpandWidth(false));
            if (lastCaseSensitive != m_configs.CaseSensitive)
            {
                m_triggerFilteredEntryComputation = true;
                m_needRegexRecompile = true;
            }
            bool lastSearchMessage = m_searchMessage;
            m_searchMessage = GUILayout.Toggle(m_searchMessage, SearchInLogMessageButtonContent, ToolbarIconButtonStyle, GUILayout.ExpandWidth(false));
            if (lastSearchMessage != m_searchMessage)
            {
                m_triggerFilteredEntryComputation = true;
            }
            bool lastSearchObjectName = m_configs.SearchObjectName;
            m_configs.SearchObjectName = GUILayout.Toggle(m_configs.SearchObjectName, SearchInObjectNameButtonContent, ToolbarIconButtonStyle, GUILayout.ExpandWidth(false));
            if (lastSearchObjectName != m_configs.SearchObjectName)
            {
                m_triggerFilteredEntryComputation = true;
            }
            bool lastSearchStackTRace = m_configs.SearchInStackTrace;
            m_configs.SearchInStackTrace = GUILayout.Toggle(m_configs.SearchInStackTrace, SearchInStackTraceButtonContent, ToolbarIconButtonStyle, GUILayout.ExpandWidth(false));
            if (lastSearchStackTRace != m_configs.SearchInStackTrace)
            {
                m_triggerFilteredEntryComputation = true;
            }
            GUILayout.FlexibleSpace();

            if (SettingsWindow != null)
            {
                if (GUILayout.Button(PluginSettingsButtonContent))
                {
                    SettingsWindow.Toggle();
                }
            }

            GUILayout.EndHorizontal();
        }

        private int GetFlagButtonWidthFromCounter(int counter)
        {
            if (counter >= 1000)
            {
                return 65;
            }
            else if (counter >= 100)
            {
                return 60;
            }
            else if (counter >= 10)
            {
                return 52;
            }
            else
            {
                return 52;
            }
        }

        private void FlagButton(LogLevel level, Texture2D icon, Texture2D iconGray, int counter)
        {
            bool hasFlag = (m_configs.LogLevelFilter & level) != 0;
            bool newFlagValue = GUILayout.Toggle(hasFlag, new GUIContent($" {(counter > 999 ? Strings.NineNineNinePlus : counter.ToString())}", (counter > 0 ? icon : iconGray)),
                ToolbarIconButtonStyle
                , GUILayout.MaxWidth(GetFlagButtonWidthFromCounter(counter)), GUILayout.ExpandWidth(false)
                );
            if (hasFlag != newFlagValue)
            {
                m_configs.LogLevelFilter ^= level;
                m_triggerFilteredEntryComputation = true;
            }
        }

        private void GetCounters(List<ConsoleLogEntry> entries, out int logCounter, out int warnCounter, out int errCounter)
        {
            if (entries == null || entries.Count == 0)
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

        private void SelectableLabel(string text, GUIStyle textStyle, float currentX)
        {
            // TODO
            /*float width = m_configs.InspectorOnTheRight ? m_splitterPosition : EditorGUIUtility.currentViewWidth;
            float height = textStyle.CalcHeight(new GUIContent(text), width);
            var lastRect = GUILayoutUtility.GetLastRect();
            EditorGUI.SelectableLabel(new Rect(currentX, lastRect.yMax, width, height), text, textStyle);
            GUILayout.Space(height);*/
            GUILayout.Label(text, textStyle);
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

        private void DisplayEntry(ConsoleLogEntry entry, int idx, float totalWidth)
        {
            GUIStyle currentStyle = OddEntry;
            GUIStyle textStyle = EvenEntryLabel;
            textStyle.normal.textColor = Skin.label.normal.textColor;

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
                    var categoryString = string.Join(" ", entry.categories.Take(Mathf.Min(m_configs.CategoryCountInLogList, entry.categories.Count)).Select(c => c.Name));
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


            if (m_selectedEntries.Count > 0 && m_selectedEntries.Contains(entry))
            {
                currentStyle = SelectedEntry;
                textStyle = SelectedEntryLabel;
            }
            else if (idx % 2 == 0)
            {
                currentStyle = EvenEntry;
            }

            var guiColor = GUI.color;
            GUI.color = new Color(1, 1, 1, 0.28f);
            GUILayout.BeginHorizontal(currentStyle, GUILayout.Height(C.ItemHeight(this)));
            {
                GUI.color = guiColor;
                //GUI.color = saveColor;
                // Picto space
                GUILayout.BeginHorizontal(GUILayout.Width(imageSize + sidePaddings));
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Box(GetEntryIcon(entry), GUIStyle.none, GUILayout.Width(imageSize), GUILayout.Height(imageSize));
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
                    for (int i = 0; i < Mathf.Min(m_configs.CategoryCountInLogList, entry.categories.Count); i++)
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
                if (m_selectedEntries.Count > 0 && m_selectedEntries[0] == entry && DateTime.Now.Ticks - m_lastClick.Ticks < m_doubleClickSpeed)
                {
                    HandleDoubleClick(entry);
                }
                m_lastClick = DateTime.Now;

                if (Event.current.shift && m_selectedEntries != null && m_selectedEntries.Count > 0)
                {
                    int startIdx = m_displayedEntries.IndexOf(m_selectedEntries[m_selectedEntries.Count - 1]);
                    int thisIdx = idx;
                    for (int i = startIdx; i <= thisIdx; i++)
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

        private void CopySelection()
        {
            // TODO check if this works in game
            string result = string.Empty;

            foreach (var entry in m_selectedEntries)
            {
                result += entry.GetExportString() + Environment.NewLine + Environment.NewLine;
            }

            GUIUtility.systemCopyBuffer = result;
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


        private void Splitter()
        {
            int splitterSize = 5;
            if (m_configs.InspectorOnTheRight)
            {
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false), GUILayout.Width(1 + 2 * splitterSize));
                GUILayout.Space(splitterSize);
                GUILayout.Box(string.Empty,
                    Strings.Splitter,
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
                    Strings.Splitter,
                     GUILayout.Height(1),
                     GUILayout.MaxHeight(1),
                     GUILayout.MinHeight(1),
                     GUILayout.ExpandWidth(true));
                GUILayout.Space(splitterSize);
                GUILayout.EndVertical();
            }

            m_splitterRect = GUILayoutUtility.GetLastRect();
            // TODO find a way to change cursor
            //EditorGUIUtility.AddCursorRect(new Rect(m_splitterRect), m_configs.InspectorOnTheRight ? MouseCursor.ResizeHorizontal : MouseCursor.ResizeVertical); // TODO Editor
        }

        #region Search

        // TODO this is a copy of the editor one. Unify with a static tool
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
                if (m_searchRegex != null)
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


        private void HandleDoubleClick(ConsoleLogEntry entry)
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
            Listener(condition, stackTrace, type, null, null);
        }

        private ConsoleLogEntry Listener(string condition, string stackTrace, LogType type, string assetPath, string assetLine)
        {
            ConsoleLogEntry newConsoleEntry = null;
            lock (m_entriesLock)
            {
                UnityEngine.Object context = null;
                for (int i = 0; i < PendingContexts.Count; i++)
                {
                    if (PendingContexts[i].message.Equals(condition) && PendingContexts[i].logType == type)
                    {
                        context = PendingContexts[i].context;
                        PendingContexts.RemoveAt(i);
                        break;
                    }
                }

                Regex categoryParse = new Regex("\\[([^\\s\\[\\]]+)\\]"); // TODO cache
                List<LogCategory> categories = new List<LogCategory>();
                var categoryAsset = m_configs.CurrentCategoriesConfig;
                string categoryLessMessage = condition;
                if (categoryAsset != null && categoryAsset.Categories != null && categoryAsset.Categories.Count > 0)
                {
                    foreach (Match match in categoryParse.Matches(categoryLessMessage))
                    {
                        foreach (var category in categoryAsset.Categories)
                        {
                            if (category.Name == match.Groups[1].Value && !categories.Contains(category))
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
                    timestamp = now.ToString("T", System.Globalization.DateTimeFormatInfo.InvariantInfo),
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

            if(m_openConsoleOnError && !m_active && (type == LogType.Assert || type == LogType.Exception || type == LogType.Error))
            {
                Toggle();
            }

            m_triggerFilteredEntryComputation = true;

            //this.Repaint();
#if UNITY_EDITOR
            if (EditorApplication.isPlaying && m_configs.ErrorPause && (type == LogType.Assert || type == LogType.Error || type == LogType.Exception))
            {
                Debug.Break();
            }
#endif //UNITY_EDITOR
            return newConsoleEntry;
        }

        public void TriggerFilteredEntryComputation()
        {
            m_triggerFilteredEntryComputation = true;
        }
    }
}