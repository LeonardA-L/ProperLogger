using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProperLogger
{
    internal class CommonMethods
    {
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
    }
}