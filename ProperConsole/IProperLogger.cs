using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ProperLogger
{
    internal interface IProperLogger : ILogObserver
    {
        bool IsGame { get; }
        GUISkin Skin { get; }
        ConfigsProvider Config { get; }
        bool AutoScroll { get; set; }
        bool Listening { get; set; }
        CustomLogHandler LogHandler { get; set; }
        bool SplitterDragging { get; set; }
        float InnerScrollableHeight { get; set; }
        float OuterScrollableHeight { get; set; }
        float SplitterPosition { get; set; }
        System.DateTime LastClick { get; set; }
        bool LastCLickIsDisplayList { get; set; }
        Rect ListDisplay { get; set; }
        Vector2 EntryListScrollPosition { get; set; }
        Vector2 InspectorScrollPosition { get; set; }
        List<ConsoleLogEntry> SelectedEntries { get; set; }
        List<ConsoleLogEntry> FilteredEntries { get; set; }
        List<ConsoleLogEntry> DisplayedEntries { get; set; }
        bool TriggerFilteredEntryComputation { get; set; }
        bool TriggerSyncWithUnityComputation { get; set; }
        object EntriesLock { get; set; }
        List<ConsoleLogEntry> Entries { get; set; }
        bool OpenConsoleOnError { get; }
        bool Active { get; }
        bool SearchMessage { get; set; }
        Regex SearchRegex { get; set; }
        string[] SearchWords { get; set; }
        List<string> InactiveCategories { get; set; }
        Rect SplitterRect { get; set; }
        bool NeedRegexRecompile { get; set; }
        System.DateTime LastRegexRecompile { get; set; }
        string SearchString { get; set; }
        Rect SearchFieldRect { get; set; }
        Rect ResetSearchButtonRect { get; set; }
        Rect ShowCategoriesButtonRect { get; set; }
        bool CallForRepaint { get; set; }
        int DisplayedEntriesCount { get; set; }
        bool IsDarkSkin { get; set; }
        Rect WindowRect { get; }
        bool PurgeGetLinesCache { get; set; }

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
        GUIStyle EntryIconStyle { get; set; }

        List<ConsoleLogEntry> CollapsedEntries { get; set; }
        List<PendingContext> PendingContexts { get; set; }

        void Clear();
        void Listener(string condition, string stackTrace, LogType type);
        void ExternalToggle();
        void TriggerRepaint();
        void SelectableLabel(string text, GUIStyle textStyle, float currentX);
        void HandleDoubleClick(ConsoleLogEntry entry);
        void DrawCategoriesWindow(Rect dropdownRect, Vector2 size);
        void ToggleSettings();
        void ExternalEditorSelectableLabelInvisible();
        void SyncWithUnityEntries();
        void LoadIcons();
        bool ExternalDisplayCloseButton();
    }
}