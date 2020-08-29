using System.Collections;
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using UnityEditor.PackageManager;

public class ProperConsoleWindow : EditorWindow
{
    [Serializable]
    [Flags]
    public enum LogLevel
    {
        Log = 1,
        Warning = 2,
        Error = 4,
        Exception = 8 | Error,
        Assert = 12 | Error,

        All = Log | Warning | Error | Exception | Assert
    }

    private static LogLevel GetLogLevelFromUnityLogType (LogType type)
    {
        switch (type)
        {
            case LogType.Error:
                return LogLevel.Error;
            case LogType.Assert:
                return LogLevel.Assert;
            case LogType.Warning:
                return LogLevel.Warning;
            case LogType.Log:
            default:
                return LogLevel.Log;
            case LogType.Exception:
                return LogLevel.Exception;
        }
    }

    [Serializable]
    private struct ConsoleLogEntry
    {
        public DateTime date;
        public string message;
        public LogLevel level;
        public string stackTrace;
    }

    private static ProperConsoleWindow m_instance = null;
    private bool m_listening = false;
    private object m_entriesLock;

    private List<ConsoleLogEntry> m_entries = null;

    private Vector2 m_scrollPosition;
    private bool m_autoScroll = true;
    private bool m_clearOnPlay = false;
    private bool m_clearOnBuild = false;
    private bool m_errorPause = false;

    string searchString = null;
    private LogLevel m_logLevelFilter = LogLevel.All;
    float innerScrollableHeight = 0;
    float outerScrollableHeight = 0;

    private int m_logCounter = 0;
    private int m_warningCounter = 0;
    private int m_errorCounter = 0;

    public static ProperConsoleWindow Instance => m_instance;

    // Add menu named "My Window" to the Window menu
    [MenuItem("Leonard/Console")]
    static void Init()
    {
        Debug.Log("Open window");
        // Get existing open window or if none, make a new one:
        if (ProperConsoleWindow.m_instance != null)
        {
            ProperConsoleWindow.m_instance.Show(true);
        }
        else
        {
            ShowWindow();
        }
    }

    public static void ShowWindow()
    {
        if (m_instance != null)
        {
            m_instance.Show(true);
            m_instance.Focus();
        }
        else
        {
            m_instance = ScriptableObject.CreateInstance<ProperConsoleWindow>();
            m_instance.Show(true);
            m_instance.Focus();
        }
    }

    private void OnEnable()
    {
        Debug.Log("OnEnable");
        m_entries = m_entries ?? new List<ConsoleLogEntry>();
        m_listening = false;
        m_entriesLock = new object();
        m_instance = this;
        EditorApplication.playModeStateChanged += ModeChanged;
        InitListener();
    }

    private void ModeChanged(PlayModeStateChange obj)
    {
        Debug.Log(obj);
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
        if (m_clearOnPlay)
        {
            Clear();
        }
    }

    private void ExitingPlayMode()
    {
        RemoveListener();
    }

    private void EnteredEditMode()
    {
        InitListener();
    }

    private void OnDisable()
    {
        Debug.Log("OnDisable");
        RemoveListener();
        EditorApplication.playModeStateChanged -= ModeChanged;
        m_instance = null;
    }

    public void InitListener()
    {
        if (!m_listening)
        {
           Application.logMessageReceivedThreaded += Listener;
           m_listening = true;
        }
    }

    public void RemoveListener()
    {
        Application.logMessageReceivedThreaded -= Listener;
        m_listening = false;
    }

    private void Listener(string condition, string stackTrace, LogType type)
    {
        lock (m_entriesLock)
        {
            m_entries.Add(new ConsoleLogEntry()
            {
                date = DateTime.Now,
                level = GetLogLevelFromUnityLogType(type),
                message = condition,
                stackTrace = stackTrace,
            });

            switch (type)
            {
                case LogType.Log:
                    m_logCounter++;
                    break;
                case LogType.Warning:
                    m_warningCounter++;
                    break;
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    m_errorCounter++;
                    break;
            }
        }

        this.Repaint();
        if(m_errorPause && (type == LogType.Assert || type == LogType.Error || type == LogType.Exception))
        {
            Debug.Break();
        }
    }

    private void Clear()
    {
        m_entries.Clear();
    }

    private int GetCounter(LogLevel level)
    {
        if (level.HasFlag(LogLevel.Log)) { return m_logCounter; }
        if (level.HasFlag(LogLevel.Warning)) { return m_warningCounter; }
        return m_errorCounter;
    }

    private void FlagButton(LogLevel level, string label)
    {
        bool hasFlag = (m_logLevelFilter & level) != 0;
        bool newFlagValue = GUILayout.Toggle(hasFlag, new GUIContent($"{label} {GetCounter(level)}"), "ToolbarButton", GUILayout.ExpandWidth(false));
        if (hasFlag != newFlagValue)
        {
            m_logLevelFilter ^= level;
        }
    }

    void OnGUI()
    {
        bool repaint = Event.current.type == EventType.Repaint;

        GUILayout.BeginHorizontal("Toolbar");
        if (GUILayout.Button("Clear", "ToolbarButton", GUILayout.ExpandWidth(false)))
        {
            Clear();
            GUIUtility.keyboardControl = 0;
        }
        m_clearOnPlay = GUILayout.Toggle(m_clearOnPlay, "Clear on Play", "ToolbarButton", GUILayout.ExpandWidth(false));
        m_clearOnBuild = GUILayout.Toggle(m_clearOnBuild, "Clear on Build", "ToolbarButton", GUILayout.ExpandWidth(false));
        m_errorPause = GUILayout.Toggle(m_errorPause, "Error Pause", "ToolbarButton", GUILayout.ExpandWidth(false));

        searchString = GUILayout.TextField(searchString, "ToolbarSeachTextField");
        if (!string.IsNullOrEmpty(searchString))
        {
            searchString = searchString.Trim();
        }

        // Log Level Flags
        FlagButton(LogLevel.Log, "L");
        FlagButton(LogLevel.Warning, "W");
        FlagButton(LogLevel.Error, "E");



        GUILayout.EndHorizontal();

        float startY = 0;
        GUILayout.Space(1);
        if (repaint)
        {
            Rect r = GUILayoutUtility.GetLastRect();
            startY = r.yMax;
        }

        m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);

        if (repaint)
        {
            float scrollTolerance = 0;
            m_autoScroll = m_scrollPosition.y >= (innerScrollableHeight - outerScrollableHeight - scrollTolerance + startY);
        }

        GUILayout.BeginVertical();

        if(m_entries.Count == 0) GUILayout.Space(10);

        var filteredEntries = m_entries.FindAll(e => ValidFilter(e));
        for (int i= 0;i< filteredEntries.Count;i++)
        {
            GUILayout.Label(filteredEntries[i].message);
        }

        GUILayout.EndVertical();

        if (repaint)
        {
            Rect r = GUILayoutUtility.GetLastRect();
            innerScrollableHeight = r.yMax;
        }

        GUILayout.EndScrollView();

        GUILayout.Space(1);
        if (repaint)
        {
            Rect r = GUILayoutUtility.GetLastRect();
            outerScrollableHeight = r.yMin;
        }

        if (repaint && m_autoScroll)
        {
            m_scrollPosition.y = innerScrollableHeight - outerScrollableHeight + startY;
        }

        if (GUILayout.Button("Log"))
        {
            Debug.Log($"Log {DateTime.Now.ToString()} {m_listening}");
        }

        if (GUILayout.Button("LogWarning"))
        {
            Debug.LogWarning($"Warning {DateTime.Now.ToString()} {m_listening} {m_autoScroll}");
        }

        if (GUILayout.Button("LogError"))
        {
            Debug.LogError("Error");
        }

        if (GUILayout.Button("LogAssert"))
        {
            Debug.Assert(false);
        }
    }

    private bool ValidFilter(ConsoleLogEntry e)
    {
        bool valid = true;

        if(m_logLevelFilter != LogLevel.All)
        {
            valid &= (e.level & m_logLevelFilter) == e.level;
            if (!valid)
            {
                return false;
            }
        }

        if (!string.IsNullOrEmpty(searchString))
        {
            valid &= e.message.IndexOf(searchString, System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (!valid)
            {
                return false;
            }
        }

        return valid;
    }

    public void OnBuild()
    {

        if (m_clearOnBuild)
        {
            Clear();
        }
    }

    private void Update()
    {
    }
}