using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace ProperLogger
{
    internal class CommonMethods
    {
        private static Regex m_categoryParse = null;
        private static Regex CategoryParse => m_categoryParse ?? (m_categoryParse = new Regex("\\[([^\\s\\[\\]]+)\\]"));

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
            inspectorTextStyle.stretchWidth = false;
            inspectorTextStyle.clipping = TextClipping.Clip;
            console.InspectorTextStyle = inspectorTextStyle;
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
            bool newFlagValue = GUILayout.Toggle(hasFlag, new GUIContent($" {(counter > 999 ? Strings.NineNineNinePlus : counter.ToString())}", (counter > 0 ? icon : iconGray)),
                console.ToolbarIconButtonStyle
                , GUILayout.MaxWidth(GetFlagButtonWidthFromCounter(counter)), GUILayout.ExpandWidth(false)
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
            }

            console.TriggerFilteredEntryComputation = true;

            if (console.IsGame)
            {
                if (console.OpenConsoleOnError && !console.Active && (type == LogType.Assert || type == LogType.Exception || type == LogType.Error))
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

    }
}