using UnityEngine;

namespace ProperLogger
{
    internal interface IProperLogger
    {
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
    }
}