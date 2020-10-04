using System.Collections.Generic;
using UnityEngine;

namespace ProperLogger
{
    internal interface IProperLogger : ILogObserver
    {
        bool IsGame { get; }
        GUISkin Skin { get; }
        ConfigsProvider Config { get; }
        bool Listening { get; set; }
        CustomLogHandler LogHandler { get; set; }
        bool LastCLickIsDisplayList { get; set; }
        Rect ListDisplay { get; set; }
        List<ConsoleLogEntry> SelectedEntries { get; set; }
        bool TriggerFilteredEntryComputation { get; set; }
        object EntriesLock { get; set; }
        List<ConsoleLogEntry> Entries { get; set; }
        bool OpenConsoleOnError { get; }
        bool Active { get; }

        GUIContent ClearButtonContent { get; set; }
        GUIContent CollapseButtonContent { get; set; }
        GUIContent ErrorPauseButtonContent { get; set; }
        GUIContent ClearOnPlayButtonContent { get; set; }
        GUIContent ClearOnBuildButtonContent { get; set; }
        GUIContent AdvancedSearchButtonContent { get; set; }
        GUIContent CategoriesButtonContent { get; set; }
        GUIContent RegexSearchButtonNameOnlyContent { get; set; }
        GUIContent CaseSensitiveButtonContent { get; set; }
        GUIContent SearchInLogMessageButtonContent { get; set; }
        GUIContent SearchInObjectNameButtonContent { get; set; }
        GUIContent SearchInStackTraceButtonContent { get; set; }
        GUIContent PluginSettingsButtonContent { get; set; }
        Texture2D ClearIcon { get; set; }
        Texture2D CollapseIcon { get; set; }
        Texture2D ClearOnPlayIcon { get; set; }
        Texture2D ClearOnBuildIcon { get; set; }
        Texture2D ErrorPauseIcon { get; set; }
        Texture2D RegexSearchIcon { get; set; }
        Texture2D CaseSensitiveIcon { get; set; }
        Texture2D AdvancedSearchIcon { get; set; }

        Texture2D IconInfo { get; set; }
        Texture2D IconWarning { get; set; }
        Texture2D IconError { get; set; }
        Texture2D IconInfoGray { get; set; }
        Texture2D IconWarningGray { get; set; }
        Texture2D IconErrorGray { get; set; }
        Texture2D IconConsole { get; set; }
        Texture2D ExceptionIcon { get; set; }
        Texture2D AssertIcon { get; set; }
        GUIStyle OddEntry { get; set; }
        GUIStyle SelectedEntry { get; set; }
        GUIStyle SelectedEntryLabel { get; set; }
        GUIStyle EvenEntry { get; set; }
        GUIStyle EvenEntryLabel { get; set; }
        GUIStyle CategoryNameStyle { get; set; }
        GUIStyle CategoryColorStrip { get; set; }
        GUIStyle CollapseBubbleStyle { get; set; }
        GUIStyle CollapseBubbleWarningStyle { get; set; }
        GUIStyle CollapseBubbleErrorStyle { get; set; }
        GUIStyle ToolbarIconButtonStyle { get; set; }
        GUIStyle InspectorTextStyle { get; set; }

        List<ConsoleLogEntry> CollapsedEntries { get; set; }
        List<PendingContext> PendingContexts { get; set; }

        void Listener(string condition, string stackTrace, LogType type);
        void ExternalToggle();
        void TriggerRepaint();
    }
}