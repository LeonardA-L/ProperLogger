using UnityEngine;

namespace ProperLogger
{
    internal interface IProperLogger
    {
        bool IsGame { get; }
        GUISkin Skin { get; }
        ConfigsProvider Config { get; }

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
    }
}