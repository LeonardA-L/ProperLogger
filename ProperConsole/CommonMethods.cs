using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace ProperLogger
{
    internal class CommonMethods
    {
        private static Color s_darkerColor = new Color(1, 1, 1, 0.28f);
        private static float s_doubleClickSpeed = 300 * 10000; // Could be a config ?
        private static float s_regexCompileDebounce => 200 * 10000;
        private static Regex s_categoryParse = null;
        private static Regex CategoryParse => s_categoryParse ?? (s_categoryParse = new Regex("\\[([^\\s\\[\\]]+)\\]"));

        internal static float ItemHeight(IProperLogger console) => (console.Config.LogEntryMessageFontSize + (console.Config.LogEntryMessageFontSize < 15 ? 3 : 4)) * console.Config.LogEntryMessageLineCount
                                  + (console.Config.LogEntryStackTraceFontSize + (console.Config.LogEntryStackTraceFontSize < 15 ? 3 : 4)) * console.Config.LogEntryStackTraceLineCount
                                  + 8; // padding
        internal static void ClearGUIContents(IProperLogger console)
        {
            console.ClearButtonContent = null;
            console.CollapseButtonContent = null;
            console.ClearOnPlayButtonContent = null;
            console.ClearOnBuildButtonContent = null;
            console.ErrorPauseButtonContent = null;

            console.AdvancedSearchButtonContent = null;
            console.CategoriesButtonContent = null;
            console.RegexSearchButtonNameOnlyContent = null;
            console.CaseSensitiveButtonContent = null;
            console.SearchInLogMessageButtonContent = null;
            console.SearchInObjectNameButtonContent = null;
            console.SearchInStackTraceButtonContent = null;
            console.PluginSettingsButtonContent = null;
        }
        private static GUIContent CreateButtonGUIContent(IProperLogger console, Texture2D icon, string text)
        {
            if (icon == null)
            {
                return new GUIContent(text);
            }
            switch (console.Config.DisplayIcons)
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
        internal static void CacheGUIContents(IProperLogger console)
        {
            console.ClearButtonContent = CreateButtonGUIContent(console, console.ClearIcon, "Clear");
            console.CollapseButtonContent = CreateButtonGUIContent(console, console.CollapseIcon, "Collapse");
            console.ClearOnPlayButtonContent = CreateButtonGUIContent(console, console.ClearOnPlayIcon, "Clear on Play");
            console.ClearOnBuildButtonContent = CreateButtonGUIContent(console, console.ClearOnBuildIcon, "Clear on Build");
            console.ErrorPauseButtonContent = CreateButtonGUIContent(console, console.ErrorPauseIcon, "Error Pause");

            console.AdvancedSearchButtonContent = new GUIContent(console.AdvancedSearchIcon, "Advanced Search");
            console.CategoriesButtonContent = new GUIContent("Categories");
            console.RegexSearchButtonNameOnlyContent = CreateButtonGUIContent(console, console.RegexSearchIcon, "Regex Search");
            console.CaseSensitiveButtonContent = CreateButtonGUIContent(console, console.CaseSensitiveIcon, "Case Sensitive");
            console.SearchInLogMessageButtonContent = new GUIContent("Search in Log Message");
            console.SearchInObjectNameButtonContent = new GUIContent("Search in Object Name");
            console.SearchInStackTraceButtonContent = new GUIContent("Search in Stack Trace");
            console.PluginSettingsButtonContent = new GUIContent("Plugin Settings");
        }
        internal static void ClearStyles(IProperLogger console)
        {
            console.OddEntry = null;
            console.SelectedEntry = null;
            console.SelectedEntryLabel = null;
            console.EvenEntry = null;
            console.EvenEntryLabel = null;

            console.CategoryNameStyle = null;

            console.CategoryColorStrip = null;

            console.CollapseBubbleStyle = null;
            console.CollapseBubbleWarningStyle = null;
            console.CollapseBubbleErrorStyle = null;

            console.ToolbarIconButtonStyle = null;

            console.InspectorTextStyle = null;
        }
        internal static void CacheStyles(IProperLogger console)
        {
            // TODO some styles don't need "new" style instantiation

            console.OddEntry = new GUIStyle(console.Skin.FindStyle("OddEntry"));
            console.SelectedEntry = new GUIStyle(console.Skin.FindStyle("SelectedEntry"));
            console.SelectedEntryLabel = new GUIStyle(console.Skin.FindStyle("EntryLabelSelected"));
            console.EvenEntry = new GUIStyle(console.Skin.FindStyle("EvenEntry"));
            console.EvenEntryLabel = new GUIStyle(console.Skin.FindStyle("EntryLabel"));

            var categoryNameStyle = new GUIStyle(console.EvenEntryLabel);
            categoryNameStyle.normal.textColor = GUI.skin.label.normal.textColor;
            categoryNameStyle.alignment = TextAnchor.MiddleCenter;
            categoryNameStyle.fontSize = console.Config.LogEntryStackTraceFontSize;
            categoryNameStyle.padding.top = (int)((ItemHeight(console) / 2f) - categoryNameStyle.fontSize);
            categoryNameStyle.fontStyle = FontStyle.Bold;
            categoryNameStyle.fontSize = console.Config.LogEntryMessageFontSize;
            console.CategoryNameStyle = categoryNameStyle;

            console.CategoryColorStrip = new GUIStyle(console.Skin.FindStyle("CategoryColorStrip"));

            console.CollapseBubbleStyle = new GUIStyle(console.Skin.FindStyle("CollapseBubble"));
            console.CollapseBubbleWarningStyle = new GUIStyle(console.Skin.FindStyle("CollapseBubbleWarning"));
            console.CollapseBubbleErrorStyle = new GUIStyle(console.Skin.FindStyle("CollapseBubbleError"));

            console.ToolbarIconButtonStyle = new GUIStyle(Strings.ToolbarButton);

            var inspectorTextStyle = new GUIStyle(console.EvenEntryLabel);
            inspectorTextStyle = new GUIStyle(GUI.skin.label);
            inspectorTextStyle.richText = true;
            inspectorTextStyle.fontSize = console.Config.InspectorMessageFontSize;
            inspectorTextStyle.wordWrap = true;
            inspectorTextStyle.stretchWidth = true;
            inspectorTextStyle.clipping = TextClipping.Clip;
            console.InspectorTextStyle = inspectorTextStyle;

            console.EntryIconStyle = new GUIStyle(console.Skin.FindStyle("EntryIcon"));

            console.RemoteConnectionUtilityStyle = new GUIStyle("ToolbarDropDown");
            console.RemoteConnectionUtilityStyle.stretchWidth = true;
        }
        internal static void ComputeCollapsedEntries(IProperLogger console, List<ConsoleLogEntry> filteredEntries)
        {
            console.CollapsedEntries = new List<ConsoleLogEntry>();

            for (int i = 0; i < filteredEntries.Count; i++)
            {
                bool found = false;
                int foundIdx = 0;
                for (int j = 0; j < console.CollapsedEntries.Count; j++)
                {
                    if (console.CollapsedEntries[j].originalMessage == filteredEntries[i].originalMessage)
                    {
                        foundIdx = j;
                        found = true;
                    }
                }
                if (found)
                {
                    console.CollapsedEntries[foundIdx] = new ConsoleLogEntry()
                    {
                        count = console.CollapsedEntries[foundIdx].count + 1,
                        date = console.CollapsedEntries[foundIdx].date,
                        message = console.CollapsedEntries[foundIdx].message,
                        level = console.CollapsedEntries[foundIdx].level,
                        stackTrace = console.CollapsedEntries[foundIdx].stackTrace,
                        timestamp = console.CollapsedEntries[foundIdx].timestamp,
                        messageLines = console.CollapsedEntries[foundIdx].messageLines,
                        traceLines = console.CollapsedEntries[foundIdx].traceLines,
                        categories = console.CollapsedEntries[foundIdx].categories,
                        context = console.CollapsedEntries[foundIdx].context,
                        assetPath = console.CollapsedEntries[foundIdx].assetPath,
                        assetLine = console.CollapsedEntries[foundIdx].assetLine,
                        originalStackTrace = console.CollapsedEntries[foundIdx].originalStackTrace,
                        originalMessage = console.CollapsedEntries[foundIdx].originalMessage,
                        unityIndex = console.CollapsedEntries[foundIdx].unityIndex,
                        unityMode = console.CollapsedEntries[foundIdx].unityMode,
                    };
                }
                else
                {
                    console.CollapsedEntries.Add(filteredEntries[i]);
                }
            }
        }
        internal static void ContextListener(IProperLogger console, LogType type, UnityEngine.Object context, string format, params object[] args)
        {
            console.PendingContexts = console.PendingContexts ?? new List<PendingContext>();
            if (context != null && args.Length > 0)
            {
                console.PendingContexts.Add(new PendingContext()
                {
                    logType = type,
                    context = context,
                    message = args[0] as string
                });
            }
        }
        internal static void InitListener(IProperLogger console)
        {
            if (!console.Listening)
            {
                if (Debug.unityLogger.logHandler is CustomLogHandler customLogHandler)
                {
                    customLogHandler.RemoveObserver(console);
                    customLogHandler.AddObserver(console);
                }
                else
                {
                    console.LogHandler = new CustomLogHandler(Debug.unityLogger.logHandler);
                    console.LogHandler.AddObserver(console);
                    Debug.unityLogger.logHandler = console.LogHandler;
                }
                Application.logMessageReceivedThreaded += console.Listener;
                console.Listening = true;
            }
        }
        internal static void RemoveListener(IProperLogger console)
        {
            Application.logMessageReceivedThreaded -= console.Listener;
            if (Debug.unityLogger.logHandler is CustomLogHandler customLogHandler)
            {
                customLogHandler.RemoveObserver(console);
            }
            console.Listening = false;
        }
        // TODO This doesn't work in play mode
        internal static void HandleCopyToClipboard(IProperLogger console)
        {
            if (console.LastCLickIsDisplayList && console.SelectedEntries != null && console.SelectedEntries.Count > 0)
            {
                if (Event.current.type == EventType.ValidateCommand && Event.current.commandName == Strings.CopyCommandName)
                {
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == Strings.CopyCommandName)
                {
                    CopySelection(console);
                }
            }
            if (Event.current.type == EventType.MouseDown)
            {
                if (!console.ListDisplay.Contains(Event.current.mousePosition))
                {
                    console.LastCLickIsDisplayList = false;
                }
            }
        }
        internal static void CopySelection(IProperLogger console)
        {
            // TODO check if this works in game
            string result = string.Empty;

            foreach (var entry in console.SelectedEntries)
            {
                result += entry.GetExportString() + Environment.NewLine + Environment.NewLine;
            }

            GUIUtility.systemCopyBuffer = result;
        }
        internal static void FlagButton(IProperLogger console, LogLevel level, Texture2D icon, Texture2D iconGray, int counter)
        {
            bool hasFlag = (console.Config.LogLevelFilter & level) != 0;
            bool newFlagValue = GUILayout.Toggle(hasFlag, new GUIContent((counter > 999 ? Strings.NineNineNinePlus : counter.ToString()), (counter > 0 ? icon : iconGray), counter.ToString()),
                console.ToolbarIconButtonStyle
                , GUILayout.MaxWidth(GetFlagButtonWidthFromCounter(counter))
                );
            if (hasFlag != newFlagValue)
            {
                console.Config.LogLevelFilter ^= level;
                console.TriggerFilteredEntryComputation = true;
            }
        }
        private static int GetFlagButtonWidthFromCounter(int counter)
        {
            if (counter >= 1000)
            {
                return 60;
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

        // TODO is there not a faster way? Like just caching this at all time?
        internal static void GetCounters(List<ConsoleLogEntry> entries, out int logCounter, out int warnCounter, out int errCounter)
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
        internal static Texture GetEntryIcon(IProperLogger console, ConsoleLogEntry entry)
        {
            if (entry.level.HasFlag(LogLevel.Log)) { return console.IconInfo; }
            if (entry.level.HasFlag(LogLevel.Warning)) { return console.IconWarning; }
            if (console.Config.ShowCustomErrorIcons)
            {
                if (entry.level.HasFlag(LogLevel.Exception)) { return console.ExceptionIcon; }
                if (entry.level.HasFlag(LogLevel.Assert)) { return console.AssertIcon; }
            }
            return console.IconError;
        }
        internal static ConsoleLogEntry Listener(IProperLogger console, string condition, string stackTrace, LogType type, string assetPath, string assetLine)
        {
            ConsoleLogEntry newConsoleEntry = null;
            lock (console.EntriesLock)
            {
                UnityEngine.Object context = null;
                for (int i = 0; i < console.PendingContexts.Count; i++)
                {
                    if (console.PendingContexts[i].message.Equals(condition) && console.PendingContexts[i].logType == type)
                    {
                        context = console.PendingContexts[i].context;
                        console.PendingContexts.RemoveAt(i);
                        break;
                    }
                }

                List<LogCategory> categories = new List<LogCategory>();
                var categoryAsset = console.Config.CurrentCategoriesConfig;
                string categoryLessMessage = condition;
                if (categoryAsset != null && categoryAsset.Categories != null && categoryAsset.Categories.Count > 0)
                {
                    foreach (Match match in CategoryParse.Matches(categoryLessMessage))
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

                console.Entries.Add(newConsoleEntry);

                switch (type)
                {
                    case LogType.Warning:
                        console.WarnLog++;
                        break;
                    case LogType.Assert:
                    case LogType.Error:
                    case LogType.Exception:
                        console.ErrLog++;
                        break;
                    case LogType.Log:
                    default:
                        console.LogLog++;
                        break;
                }
            }

            console.TriggerFilteredEntryComputation = true;

            if (console.IsGame)
            {
                bool openConsoleOnError = false;
                switch (console.OpenConsoleOnError)
                {
                    case EOpenOnError.Never:
                    default:
                        openConsoleOnError = false;
                        break;
                    case EOpenOnError.Always:
                        openConsoleOnError = true;
                        break;
                    case EOpenOnError.DebugBuild:
                        openConsoleOnError = Debug.isDebugBuild;
                        break;
                }
                if (openConsoleOnError && !console.Active && (type == LogType.Assert || type == LogType.Exception || type == LogType.Error))
                {
                    console.ExternalToggle();
                }
            } else
            {
                console.TriggerRepaint();
            }


#if UNITY_EDITOR
            if (EditorApplication.isPlaying && console.Config.ErrorPause && (type == LogType.Assert || type == LogType.Error || type == LogType.Exception))
            {
                Debug.Break();
            }
#endif //UNITY_EDITOR
            return newConsoleEntry;
        }

        private static bool IsActiveCategory(LogCategory category, string[] inactiveRoots, List<string> observed, LogCategoriesConfig config)
        {
            if (category == null || observed.Contains(category.Name))
            {
                return true;
            }
            if (inactiveRoots.Contains(category.Name))
            {
                return false;
            }
            if (string.IsNullOrEmpty(category.Parent))
            {
                return true;
            }
            observed.Add(category.Name);
            return IsActiveCategory(config[category.Parent], inactiveRoots, observed, config);
        }

        internal static bool ValidFilter(IProperLogger console, ConsoleLogEntry e)
        {
            bool valid = true;

            // Log Level
            if (console.Config.LogLevelFilter != LogLevel.All)
            {
                valid &= (e.level & console.Config.LogLevelFilter) == e.level;
                if (!valid)
                {
                    return false;
                }
            }

            // Text Search
            string searchableText = (console.SearchMessage ? e.originalMessage : string.Empty) + (console.Config.SearchInStackTrace ? e.stackTrace : string.Empty) + ((console.Config.SearchObjectName && e.context != null) ? e.context.name : string.Empty); // TODO opti
            if (console.Config.RegexSearch)
            {
                if (console.SearchRegex != null)
                {
                    valid &= console.SearchRegex.IsMatch(searchableText);
                }
            }
            else
            {
                if (console.SearchWords != null && console.SearchWords.Length > 0)
                {
                    valid &= console.SearchWords.All(p => searchableText.IndexOf(p, console.Config.CaseSensitive ? StringComparison.Ordinal : System.StringComparison.OrdinalIgnoreCase) >= 0);
                    if (!valid)
                    {
                        return false;
                    }
                }
            }

            // Categories
            if (console.InactiveCategories.Count > 0)
            {
                valid &= console.InactiveCategories.Intersect(e.categoriesStrings).Count() == 0;
                if (!valid)
                {
                    return false;
                }
            }

            return valid;
        }
        internal static void Splitter(IProperLogger console)
        {
            int splitterSize = 5;
            if (console.Config.InspectorOnTheRight)
            {
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false), GUILayout.Width(1 + 2 * splitterSize));
                GUILayout.Space(splitterSize);
                GUILayout.Box(string.Empty,
                     console.IsGame ? (GUIStyle)Strings.Splitter : (GUIStyle)"Box", // TODO string
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
                     console.IsGame ? (GUIStyle)Strings.Splitter : (GUIStyle)"Box", // TODO string
                     GUILayout.Height(1),
                     GUILayout.MaxHeight(1),
                     GUILayout.MinHeight(1),
                     GUILayout.ExpandWidth(true));
                GUILayout.Space(splitterSize);
                GUILayout.EndVertical();
            }

            console.SplitterRect = GUILayoutUtility.GetLastRect();
            // TODO find a way to change cursor in game
            if (!console.IsGame)
            {
#if UNITY_EDITOR
                EditorGUIUtility.AddCursorRect(new Rect(console.SplitterRect), console.Config.InspectorOnTheRight ? MouseCursor.ResizeHorizontal : MouseCursor.ResizeVertical);
#endif //UNITY_EDITOR
            }
        }
        internal static void RegexCompilation(IProperLogger console)
        {
            try
            {
                if (console.Config.RegexSearch && string.IsNullOrEmpty(console.SearchString))
                {
                    console.SearchRegex = null;
                }
                else if (console.Config.RegexSearch && console.NeedRegexRecompile && DateTime.Now.Ticks - console.LastRegexRecompile.Ticks > s_regexCompileDebounce)
                {
                    console.NeedRegexRecompile = false;
                    console.LastRegexRecompile = DateTime.Now;
                    console.TriggerFilteredEntryComputation = true;
                    if (console.Config.CaseSensitive)
                    {
                        console.SearchRegex = new Regex(console.SearchString.Trim());
                    }
                    else
                    {
                        console.SearchRegex = new Regex(console.SearchString.Trim(), RegexOptions.IgnoreCase);
                    }
                }
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }
        }
        internal static void DisplayToolbar(IProperLogger console, ref bool callForRepaint)
        {
            GUILayout.BeginHorizontal(Strings.Toolbar);

            if (GUILayout.Button(console.ClearButtonContent, console.ToolbarIconButtonStyle, GUILayout.ExpandWidth(false)))
            {
                console.Clear();
                GUIUtility.keyboardControl = 0;
            }
            bool lastCollapse = console.Config.Collapse;
            console.Config.Collapse = GUILayout.Toggle(console.Config.Collapse, console.CollapseButtonContent, console.ToolbarIconButtonStyle, GUILayout.ExpandWidth(false));
            callForRepaint = console.Config.Collapse != lastCollapse;
            if (console.Config.Collapse != lastCollapse)
            {
                console.TriggerFilteredEntryComputation = true;
                console.SelectedEntries.Clear();
            }
            if (!console.IsGame)
            {
                console.Config.ClearOnPlay = GUILayout.Toggle(console.Config.ClearOnPlay, console.ClearOnPlayButtonContent, console.ToolbarIconButtonStyle, GUILayout.ExpandWidth(false));
                console.Config.ClearOnBuild = GUILayout.Toggle(console.Config.ClearOnBuild, console.ClearOnBuildButtonContent, console.ToolbarIconButtonStyle, GUILayout.ExpandWidth(false));
            }
#if UNITY_EDITOR
            console.Config.ErrorPause = GUILayout.Toggle(console.Config.ErrorPause, console.ErrorPauseButtonContent, console.ToolbarIconButtonStyle, GUILayout.ExpandWidth(false));
#endif

            string lastSearchTerm = console.SearchString;

            GUI.enabled = !(Event.current.isMouse && console.ResetSearchButtonRect.Contains(Event.current.mousePosition));
            console.SearchString = GUILayout.TextField(console.SearchString, console.IsGame ? Strings.ToolbarSearchTextField : Strings.ToolbarSeachTextField);
            if (lastSearchTerm != console.SearchString)
            {
                console.TriggerFilteredEntryComputation = true;
                if (console.Config.RegexSearch)
                {
                    console.LastRegexRecompile = DateTime.Now;
                    console.NeedRegexRecompile = true;
                }
                console.SearchWords = console.SearchString.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            }
            GUI.enabled = true;
            if (!string.IsNullOrEmpty(console.SearchString))
            {
                if (Event.current.type == EventType.Repaint)
                {
                    console.SearchFieldRect = GUILayoutUtility.GetLastRect();
                }
                float resetSearchButtonWidth = 15;
                console.ResetSearchButtonRect = new Rect(console.SearchFieldRect.xMax - resetSearchButtonWidth, console.SearchFieldRect.y, resetSearchButtonWidth, console.SearchFieldRect.height);
                if (GUI.Button(console.ResetSearchButtonRect, GUIContent.none, "SearchCancelButton"))
                {
                    console.SearchString = null;
                    console.TriggerFilteredEntryComputation = true;
                    if (console.Config.RegexSearch)
                    {
                        console.LastRegexRecompile = DateTime.Now;
                        console.NeedRegexRecompile = true;
                    }
                    console.SearchWords = null;
                }
            }

            console.Config.AdvancedSearchToolbar = GUILayout.Toggle(console.Config.AdvancedSearchToolbar, console.AdvancedSearchButtonContent, Strings.ToolbarButton, GUILayout.ExpandWidth(false));
            Rect dropdownRect = GUILayoutUtility.GetLastRect();

            if (GUILayout.Button(console.CategoriesButtonContent, Strings.ToolbarButton, GUILayout.ExpandWidth(false)))
            {
                var categoriesAsset = console.Config.CurrentCategoriesConfig;
                Vector2 size = new Vector2(250, 150);
                if (categoriesAsset != null)
                {
                    categoriesAsset.PopulateRootCategories();
                    size.x = Mathf.Max(size.x, categoriesAsset.LongestName);
                    if (console.Config.CurrentCategoriesConfig.Categories == null || console.Config.CurrentCategoriesConfig.Categories.Count == 0)
                    {
                        size.y = 30;
                    }
                    else
                    {
                        size.y = (console.Config.CurrentCategoriesConfig.Categories.Count) * 18; // TODO put this somewhere in a style
                    }
                }

                size.y += console.IsGame ? 45 : 25;
                console.DrawCategoriesWindow(dropdownRect, size);
            }
            if (Event.current.type == EventType.Repaint) console.ShowCategoriesButtonRect = GUILayoutUtility.GetLastRect();

            GetCounters(console.Entries, out int logCounter, out int warnCounter, out int errCounter);

            // Log Level Flags
            FlagButton(console, LogLevel.Log, console.IconInfo, console.IconInfoGray, logCounter);
            FlagButton(console, LogLevel.Warning, console.IconWarning, console.IconWarningGray, warnCounter);
            FlagButton(console, LogLevel.Error, console.IconError, console.IconErrorGray, errCounter);

            GUILayout.EndHorizontal();
        }
        internal static void DisplaySearchToolbar(IProperLogger console)
        {
            GUILayout.BeginHorizontal(Strings.Toolbar);
            bool lastRegexSearch = console.Config.RegexSearch;
            console.Config.RegexSearch = GUILayout.Toggle(console.Config.RegexSearch, console.RegexSearchButtonNameOnlyContent, console.ToolbarIconButtonStyle, GUILayout.ExpandWidth(false));
            if (lastRegexSearch != console.Config.RegexSearch)
            {
                console.NeedRegexRecompile = true;
            }
            bool lastCaseSensitive = console.Config.CaseSensitive;
            console.Config.CaseSensitive = GUILayout.Toggle(console.Config.CaseSensitive, console.CaseSensitiveButtonContent, console.ToolbarIconButtonStyle, GUILayout.ExpandWidth(false));
            if (lastCaseSensitive != console.Config.CaseSensitive)
            {
                console.TriggerFilteredEntryComputation = true;
                console.NeedRegexRecompile = true;
            }
            bool lastSearchMessage = console.SearchMessage;
            console.SearchMessage = GUILayout.Toggle(console.SearchMessage, console.SearchInLogMessageButtonContent, console.ToolbarIconButtonStyle, GUILayout.ExpandWidth(false));
            if (lastSearchMessage != console.SearchMessage)
            {
                console.TriggerFilteredEntryComputation = true;
            }
            bool lastSearchObjectName = console.Config.SearchObjectName;
            console.Config.SearchObjectName = GUILayout.Toggle(console.Config.SearchObjectName, console.SearchInObjectNameButtonContent, console.ToolbarIconButtonStyle, GUILayout.ExpandWidth(false));
            if (lastSearchObjectName != console.Config.SearchObjectName)
            {
                console.TriggerFilteredEntryComputation = true;
            }
            bool lastSearchStackTRace = console.Config.SearchInStackTrace;
            console.Config.SearchInStackTrace = GUILayout.Toggle(console.Config.SearchInStackTrace, console.SearchInStackTraceButtonContent, console.ToolbarIconButtonStyle, GUILayout.ExpandWidth(false));
            if (lastSearchStackTRace != console.Config.SearchInStackTrace)
            {
                console.TriggerFilteredEntryComputation = true;
            }

            console.ShowRemoteConnectionUtility();

            GUILayout.FlexibleSpace();

            console.ToggleSettings();

            GUILayout.EndHorizontal();
        }
        internal static void DisplayList(IProperLogger console, List<ConsoleLogEntry> filteredEntries, out List<ConsoleLogEntry> displayedEntries, float totalWidth)
        {
            int startI = 0;
            int endI = filteredEntries.Count;
            int lastVisibleIdx = 0;
            // Only display elements that are in view
            if (console.OuterScrollableHeight + 100 <= console.InnerScrollableHeight)
            {
                int firstVisibleIdx = Mathf.Clamp((int)(console.EntryListScrollPosition.y / ItemHeight(console)) - 1, 0, filteredEntries.Count);
                lastVisibleIdx = Mathf.Clamp((int)((console.EntryListScrollPosition.y + console.OuterScrollableHeight) / ItemHeight(console)) + 1, 0, filteredEntries.Count);
                GUILayout.Space(firstVisibleIdx * ItemHeight(console));
                startI = firstVisibleIdx;
                endI = lastVisibleIdx;
            }

            for (int i = startI; i < endI; i++)
            {
                try
                {
                    DisplayEntry(console, filteredEntries[i], i, totalWidth);
                }
                catch (ArgumentException)
                {
                    GUIUtility.ExitGUI();
                    console.RepaintImmediate();
                }
            }

            if (lastVisibleIdx != 0)
            {
                GUILayout.Space((filteredEntries.Count - lastVisibleIdx) * ItemHeight(console));
            }
            displayedEntries = filteredEntries;
        }
        internal static void DisplayEntry(IProperLogger console, ConsoleLogEntry entry, int idx, float totalWidth)
        {
            GUIStyle currentStyle = console.OddEntry;
            GUIStyle textStyle = console.EvenEntryLabel;
            textStyle.normal.textColor = console.IsGame ? console.Skin.label.normal.textColor : GUI.skin.label.normal.textColor;

            float imageSize = Math.Min(ItemHeight(console) - (2 * 3), 32); // We clamp it in case we display 3+ lines
            imageSize += imageSize % 2;
            float sidePaddings = 10;
            float collapseBubbleSize = console.Config.Collapse ? (40 - sidePaddings) : 0; // Globally accessible ?
            float empiricalPaddings = 30 + sidePaddings;

            bool displayCategoryNameInColumn = console.Config.CategoryDisplay.HasFlag(ECategoryDisplay.NameColumn);
            bool displayCategoryIconInColumn = console.Config.CategoryDisplay.HasFlag(ECategoryDisplay.Icon);
            bool displayCategoryStrips = console.Config.CategoryDisplay.HasFlag(ECategoryDisplay.ColorStrip);
            bool categoryColumn = displayCategoryNameInColumn || displayCategoryIconInColumn;
            float categoryColumnWidth = 0;

            float categoryStripWidth = console.Config.ColorStripWidth;
            float categoriesStripsTotalWidth = 0;

            float rightSplitterWidth = console.Config.InspectorOnTheRight ? console.SplitterPosition : 0;

            string categoriesString = string.Empty;

            if (entry.categories != null && entry.categories.Count > 0)
            {
                if (categoryColumn)
                {
                    var categoryString = string.Join(" ", entry.categories.Take(Mathf.Min(console.Config.CategoryCountInLogList, entry.categories.Count)).Select(c => c.Name));
                    categoryColumnWidth = console.CategoryNameStyle.CalcSize(new GUIContent(categoryString)).x + 10;
                }
                if (displayCategoryStrips)
                {
                    categoriesStripsTotalWidth = entry.categories.Count * categoryStripWidth;
                }
                if (console.Config.CategoryDisplay.HasFlag(ECategoryDisplay.InMessage))
                {
                    string format = "<color=#{1}>[{0}]</color> ";
                    categoriesString = string.Join(string.Empty, entry.categories.Select(c => string.Format(format, c.Name, ColorUtility.ToHtmlStringRGB(Color.Lerp(c.Color, textStyle.normal.textColor, console.Config.CategoryNameColorize)))));
                }
            }

            float entrywidth = totalWidth - imageSize - collapseBubbleSize - categoryColumnWidth - empiricalPaddings - rightSplitterWidth - categoriesStripsTotalWidth;

            if (console.SelectedEntries.Count > 0 && console.SelectedEntries.Contains(entry))
            {
                currentStyle = console.SelectedEntry;
                textStyle = console.SelectedEntryLabel;
            }
            else if (idx % 2 == 0)
            {
                currentStyle = console.EvenEntry;
            }

            var guiColor = GUI.color;
            if (console.IsGame)
            {
                GUI.color = s_darkerColor;
            }
            else
            {
#if UNITY_EDITOR
                if (EditorGUIUtility.isProSkin)
                {
                    GUI.color = s_darkerColor;
                }
#endif // UNITY_EDITOR
            }
            GUILayout.BeginHorizontal(currentStyle, GUILayout.Height(ItemHeight(console)));
            {
                GUI.color = guiColor;
                // Picto space
                GUILayout.Box(GetEntryIcon(console, entry), console.EntryIconStyle, GUILayout.Width(imageSize), GUILayout.Height(imageSize)); // TODO cache
                // Text space
                GUILayout.BeginVertical(GUILayout.Width(entrywidth));
                {
                    textStyle.fontSize = console.Config.LogEntryMessageFontSize;
                    if (string.IsNullOrEmpty(entry.cachedFirstLine) || console.PurgeGetLinesCache)
                    {
                        entry.cachedFirstLine = $"[{entry.timestamp}] {categoriesString}{Utils.GetFirstLines(entry.messageLines, 0, console.Config.LogEntryMessageLineCount, false)}";
                    }
                    GUILayout.Label(entry.cachedFirstLine, textStyle);
                    textStyle.fontSize = console.Config.LogEntryStackTraceFontSize;
                    if (console.Config.LogEntryStackTraceLineCount > 0)
                    {
                        if (string.IsNullOrEmpty(entry.cachedSecondLine) || console.PurgeGetLinesCache)
                        {
                            if (console.Config.ShowContextNameInsteadOfStack && entry.context != null)
                            {
                                entry.cachedSecondLine = entry.context.name;
                            }
                            else if (!string.IsNullOrEmpty(entry.stackTrace))
                            {
                                entry.cachedSecondLine = $"{Utils.GetFirstLines(entry.traceLines, 0, console.Config.LogEntryStackTraceLineCount, true)}";
                            }
                            else
                            {
                                entry.cachedSecondLine = $"{Utils.GetFirstLines(entry.messageLines, console.Config.LogEntryMessageLineCount, console.Config.LogEntryStackTraceLineCount, false)}";
                            }
                        }
                        GUILayout.Label(entry.cachedSecondLine, textStyle);
                    }
                }
                GUILayout.EndVertical();
                // First Category space
                if (categoryColumn && entry.categories != null && entry.categories.Count > 0)
                {
                    GUILayout.BeginHorizontal(GUILayout.Width(categoryColumnWidth));
                    for (int i = 0; i < Mathf.Min(console.Config.CategoryCountInLogList, entry.categories.Count); i++)
                    {
                        var category = entry.categories[i];
                        var categoryColor = console.CategoryNameStyle.normal.textColor;
                        console.CategoryNameStyle.normal.textColor = Color.Lerp(console.CategoryNameStyle.normal.textColor, category.Color, console.Config.CategoryNameInLogListColorize);
                        GUILayout.Label(category.Name, console.CategoryNameStyle);
                        console.CategoryNameStyle.normal.textColor = categoryColor;
                    }
                    GUILayout.EndHorizontal();
                }
                // Collapse Space
                if (console.Config.Collapse)
                {
                    DisplayCollapseBubble(console, entry.level, entry.count, collapseBubbleSize, sidePaddings);
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
                        GUI.Box(new Rect(lastRect.xMax + i * categoryStripWidth, lastRect.yMin - 4, categoryStripWidth, ItemHeight(console)), string.Empty, console.CategoryColorStrip);
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
                if (console.SelectedEntries.Count > 0 && console.SelectedEntries[0] == entry && DateTime.Now.Ticks - console.LastClick.Ticks < s_doubleClickSpeed)
                {
                    console.HandleDoubleClick(entry);
                }
                console.LastClick = DateTime.Now;

                if (Event.current.shift && console.SelectedEntries != null && console.SelectedEntries.Count > 0)
                {
                    int startIdx = console.DisplayedEntries.IndexOf(console.SelectedEntries[console.SelectedEntries.Count - 1]);
                    int thisIdx = idx;
                    for (int i = startIdx; i <= thisIdx; i++)
                    {
                        if (!console.SelectedEntries.Contains(console.DisplayedEntries[i]))
                        {
                            console.SelectedEntries.Add(console.DisplayedEntries[i]);
                        }
                    }
                }
                else if (Event.current.control)
                {
                    if (console.SelectedEntries.Contains(entry))
                    {
                        console.SelectedEntries.Remove(entry);
                    }
                    else
                    {
                        console.SelectedEntries.Add(entry);
                    }
                }
                else
                {
                    console.SelectedEntries.Clear();
                    console.SelectedEntries.Add(entry);
                }
                console.LastCLickIsDisplayList = true;

                if (console.Config.CopyOnSelect)
                {
                    CopySelection(console);
                }
            }
        }
        internal static void DisplayCollapseBubble(IProperLogger console, LogLevel level, int count, float collapseBubbleSize, float sidePaddings)
        {
            GUIStyle style;
            switch (level)
            {
                case LogLevel.Log:
                    style = console.CollapseBubbleStyle;
                    break;
                case LogLevel.Warning:
                    style = console.CollapseBubbleWarningStyle;
                    break;
                case LogLevel.Error:
                default:
                    style = console.CollapseBubbleErrorStyle;
                    break;
            }
            GUILayout.Label(count.ToString(), style, GUILayout.ExpandWidth(false), GUILayout.Width(collapseBubbleSize), GUILayout.Height(23));
            GUILayout.Space(sidePaddings);
        }

        private static void ComputeInactiveCategories(IProperLogger console)
        {
            console.InactiveCategories?.Clear();
            console.InactiveCategories = new List<string>();

            if (console.Config.CurrentCategoriesConfig != null && console.Config.CurrentCategoriesConfig.Categories != null)
            {
                var inactiveCategoryConfig = console.Config.InactiveCategories.Select(s => s.Name).ToArray();
                foreach (var category in console.Config.CurrentCategoriesConfig.Categories)
                {
                    if (!IsActiveCategory(category, inactiveCategoryConfig, new List<string>(), console.Config.CurrentCategoriesConfig))
                    {
                        console.InactiveCategories.Add(category.Name);
                    }
                }
            }
        }

        internal static void DoGui(IProperLogger console)
        {
            HandleCopyToClipboard(console);
            console.ExternalEditorSelectableLabelInvisible();

            if (console.ClearButtonContent == null)
            {
                CacheGUIContents(console);
            }

            if (console.InspectorTextStyle == null)
            {
                CacheStyles(console);
            }

            bool callForRepaint = false;
            bool repaint = Event.current.type == EventType.Repaint;

            if (console.IsGame)
            {
                if (console.ExternalDisplayCloseButton())
	            {
	                return;
	            }
            }

            DisplayToolbar(console, ref callForRepaint);

            console.CallForRepaint = callForRepaint;

            if (console.Config.AdvancedSearchToolbar)
            {
                DisplaySearchToolbar(console);
            }

            float startY = 0;
            float totalWidth = console.IsGame ? console.WindowRect.width : Screen.width;
            GUILayout.Space(1);
            if (repaint)
            {
                Rect r = GUILayoutUtility.GetLastRect();
                startY = r.yMax;
            }

            if (console.Config.InspectorOnTheRight)
            {
                GUILayout.BeginHorizontal();
            }

#region DisplayList
            GUILayout.BeginVertical(); // Display list
            console.EntryListScrollPosition = GUILayout.BeginScrollView(console.EntryListScrollPosition, false, false, GUIStyle.none, console.IsGame ? console.Skin.verticalScrollbar : GUI.skin.verticalScrollbar);

            if (repaint)
            {
                float scrollTolerance = 0;
                console.AutoScroll = console.EntryListScrollPosition.y >= (console.InnerScrollableHeight - console.OuterScrollableHeight - scrollTolerance + startY);
            }

            GUILayout.BeginVertical();

            if (console.Entries.Count == 0) GUILayout.Space(10);

            if (!console.IsGame)
            {
                if (console.TriggerSyncWithUnityComputation)
                {
                    Debug.Log("Trigger Unity Sync");
                    lock (console.Entries)
                    {
                        console.SyncWithUnityEntries();
                    }
                    console.TriggerFilteredEntryComputation = true;
                    console.TriggerSyncWithUnityComputation = false;
                }
            }

            if (console.TriggerFilteredEntryComputation)
            {
                if(console.InactiveCategories == null)
                {
                    ComputeInactiveCategories(console);
                }

                console.FilteredEntries = console.Entries.FindAll(e => ValidFilter(console, e));
                if (console.Config.Collapse)
                {
                    ComputeCollapsedEntries(console, console.FilteredEntries);
                }
                console.TriggerFilteredEntryComputation = false;
                console.TriggerRepaint();
                console.RepaintImmediate();
            }

            DisplayList(console, console.Config.Collapse ? console.CollapsedEntries : console.FilteredEntries, out List<ConsoleLogEntry> displayedEntries, totalWidth);
            console.DisplayedEntries = displayedEntries;

            if (console.DisplayedEntries.Count < console.DisplayedEntriesCount)
            {
                console.SelectedEntries.Clear();
            }
            console.DisplayedEntriesCount = console.DisplayedEntries.Count;

            GUILayout.EndVertical();

            if (repaint)
            {
                Rect r = GUILayoutUtility.GetLastRect();
                console.InnerScrollableHeight = r.yMax;
            }

            GUILayout.EndScrollView();

            GUILayout.Space(1);
            if (repaint)
            {
                Rect r = GUILayoutUtility.GetLastRect();
                console.OuterScrollableHeight = r.yMin;
            }

            if (repaint && console.AutoScroll)
            {
                console.EntryListScrollPosition = new Vector2(console.EntryListScrollPosition.x, console.InnerScrollableHeight - console.OuterScrollableHeight + startY);
            }
            GUILayout.EndVertical(); // Display list
            if (repaint)
            {
                console.ListDisplay = GUILayoutUtility.GetLastRect();
            }
#endregion DisplayList

#region Inspector
            if (console.Config.InspectorOnTheRight)
            {
                GUILayout.BeginHorizontal(); // Inspector
            }
            else
            {
                GUILayout.BeginVertical(); // Inspector
            }

            console.SplitterPosition = Mathf.Clamp(console.SplitterPosition, 100, (console.Config.InspectorOnTheRight ? Screen.width : Screen.height) - 200);

            Splitter(console);

            if (console.Config.InspectorOnTheRight)
            {
                GUILayout.BeginVertical(GUILayout.Width(console.SplitterPosition),
                GUILayout.MaxWidth(console.SplitterPosition),
                GUILayout.MinWidth(console.SplitterPosition));
                console.InspectorScrollPosition = GUILayout.BeginScrollView(console.InspectorScrollPosition);
            }
            else
            {
                GUILayout.BeginVertical(GUILayout.Height(console.SplitterPosition),
                GUILayout.MaxHeight(console.SplitterPosition),
                GUILayout.MinHeight(console.SplitterPosition));
                console.InspectorScrollPosition = GUILayout.BeginScrollView(console.InspectorScrollPosition);
            }
            if (console.SelectedEntries.Count > 0)
            {
                var entry = console.SelectedEntries[0];

                GUILayout.Space(1);
                float currentX = (GUILayoutUtility.GetLastRect()).xMin;

                string categoriesString = string.Empty;
                if (entry.categories != null && entry.categories.Count > 0)
                {
                    if (console.Config.CategoryDisplay.HasFlag(ECategoryDisplay.InInspector))
                    {
                        string format = "<color=#{1}>[{0}]</color> ";
                        categoriesString = string.Join(string.Empty, entry.categories.Select(c => string.Format(format, c.Name, ColorUtility.ToHtmlStringRGB(Color.Lerp(c.Color, console.InspectorTextStyle.normal.textColor, console.Config.CategoryNameColorize)))));
                    }
                }

                console.SelectableLabel($"{categoriesString}{entry.message}", console.InspectorTextStyle, currentX);

                if (entry.context != null)
                {
                    Color txtColor = console.InspectorTextStyle.normal.textColor;
                    if (!console.Config.ObjectNameColor.Equals(txtColor))
                    {
                        console.InspectorTextStyle.normal.textColor = console.Config.ObjectNameColor;
                    }
                    console.SelectableLabel(entry.context.name, console.InspectorTextStyle, currentX);
                    console.InspectorTextStyle.normal.textColor = txtColor;
                }
                if (!string.IsNullOrEmpty(entry.stackTrace))
                {
                    console.SelectableLabel(entry.stackTrace, console.InspectorTextStyle, currentX);
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            if (console.Config.InspectorOnTheRight)
            {
                GUILayout.EndHorizontal(); // Inspector
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.EndVertical(); // Inspector
            }
            #endregion Inspector

#if DEBUG
#region Debug Buttons
            if (!console.IsGame)
            {
                if (GUILayout.Button("Log"))
                {
                    Debug.Log($"Log {DateTime.Now.ToString()} {console.Listening}\r\nA\nB\nC\nD", Camera.main);
                }

                if (GUILayout.Button("Log Combat"))
                {
                    Debug.Log($"[Combat] [Performance] Log {DateTime.Now.ToString()} {console.Listening}", Camera.main);
                }

                if (GUILayout.Button("Log Dialogue"))
                {
                    Debug.Log($"[Dialogue] Log {DateTime.Now.ToString()} {console.Listening}", Camera.main);
                }

                if (GUILayout.Button("Log Performance2"))
                {
                    Debug.Log($"[Performance2] Log {DateTime.Now.ToString()} {console.Listening}", Camera.main);
                }

                if (GUILayout.Button("Log Performance"))
                {
                    Debug.Log($"[Performance] Log {DateTime.Now.ToString()} {console.Listening}", Camera.main);
                }

                if (GUILayout.Button("LogException"))
                {
                    Debug.LogException(new Exception());
                }

                if (GUILayout.Button("LogWarning"))
                {
                    Debug.LogWarning($"Warning {DateTime.Now.ToString()} {console.Listening}");
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
                        Debug.Log($"Log {DateTime.Now.ToString()} {console.Listening}");
                    }
                }
                if (GUILayout.Button("1000 syncs"))
                {
                    var watch = new System.Diagnostics.Stopwatch();
                    watch.Start();
                    lock (console.EntriesLock)
                    {
                        for (int i = 0; i < 10000; i++)
                        {
                            console.SyncWithUnityEntries();
                        }
                    }
                    watch.Stop();
                    Debug.Log($"Elapsed: {watch.ElapsedMilliseconds}");
                }
                if (GUILayout.Button("Trigger Sync"))
                {
                    console.TriggerSyncWithUnityComputation = true;
                }
            }
#endregion Debug Buttons
#endif // DEBUG

            if (Event.current != null)
            {
                switch (Event.current.rawType)
                {
                    case EventType.MouseDown:
                        if (console.SplitterRect.Contains(Event.current.mousePosition))
                        {
                            //Debug.Log("Start dragging");
                            console.SplitterDragStartPosition = Event.current.mousePosition;
                            console.SplitterDragging = true;
                        }
                        break;
                    case EventType.MouseDrag:
                        /*if (console.SplitterDragging)
                        {
                            //Debug.Log("moving splitter");
                            console.SplitterPosition -= console.Config.InspectorOnTheRight ? Event.current.delta.x : Event.current.delta.y;
                            console.TriggerRepaint();
                        }*/
                        break;
                    case EventType.MouseUp:
                        if (console.SplitterDragging)
                        {
                            //Debug.Log("Done dragging");
                            console.SplitterDragging = false;
                        }
                        break;
                }


                if (console.SplitterDragging)
                {
                    //Debug.Log("moving splitter");
                    var delta = Event.current.mousePosition - console.SplitterDragStartPosition;
                    console.SplitterPosition += console.Config.InspectorOnTheRight ? -delta.x : -delta.y;
                    console.SplitterDragStartPosition = Event.current.mousePosition;
                    console.RepaintImmediate();
                }
            }

            if (!console.IsGame)
            {
#if UNITY_EDITOR
                if (console.IsDarkSkin != EditorGUIUtility.isProSkin)
                {
                    console.IsDarkSkin = EditorGUIUtility.isProSkin;
                    ClearStyles(console);
                    ClearGUIContents(console);
                    console.LoadIcons();
                    CacheStyles(console);
                    CacheGUIContents(console);
                }
#endif // UNITY_EDITOR
            }
            console.PurgeGetLinesCache = false;
        }

        public static void DisplayCategoryFilterContent(ICategoryWindow categoryWindow)
        {
            Color defaultColor = GUI.color;
            var inactiveCategories = categoryWindow.Config.InactiveCategories;
            GoThroughCategoryTree(categoryWindow.Config.CurrentCategoriesConfig.RootCategories, 0, categoryWindow, inactiveCategories, defaultColor);
            GUI.color = defaultColor;
        }

        private static void GoThroughCategoryTree(List<LogCategory> roots, int level, ICategoryWindow categoryWindow, List<LogCategory> inactiveCategories, Color defaultColor)
        {
            for (int i = 0; i < roots.Count; i++)
            {
                var category = roots[i];
                bool lastActive = !inactiveCategories.Contains(category);
                GUI.color = Color.Lerp(category.Color, defaultColor, categoryWindow.Config.CategoryNameColorize);
                GUILayout.BeginHorizontal();
                GUILayout.Space(LogCategoriesConfig.s_categoryIndent * level);
                bool isActive = GUILayout.Toggle(lastActive, category.Name);
                GUILayout.EndHorizontal();
                if (isActive != lastActive)
                {
                    if (isActive)
                    {
                        inactiveCategories.Remove(category);
                        categoryWindow.Config.InactiveCategories = inactiveCategories;
                    }
                    else
                    {
                        inactiveCategories.Add(category);
                        categoryWindow.Config.InactiveCategories = inactiveCategories;
                    }
                    if (categoryWindow.Console != null)
                    {
                        categoryWindow.Console.InactiveCategories = null;
                        categoryWindow.Console.TriggerFilteredEntryComputation = true;
                        categoryWindow.Console.TriggerRepaint();
                    }
                }

                if(category.Children != null && category.Children.Count > 0)
                {
                    GoThroughCategoryTree(category.Children, level + 1, categoryWindow, inactiveCategories, defaultColor);
                }
            }
        }
    }
}