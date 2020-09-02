using System.Collections;
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using System.Globalization;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System.Reflection;

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
    private struct PendingContext
    {
        public string message;
        public LogType logType;
        public UnityEngine.Object context;
    }

    [Serializable]
    private struct ConsoleLogEntry
    {
        public long date;
        public string timestamp;
        public string message;
        public string messageFirstLine;
        public LogLevel level;
        public string stackTrace;
        public int count;
        public UnityEngine.Object context;
        public string firstLine;
        public string firstAsset;
    }

    [SerializeField]
    private GUISkin m_skin = null;

    private static ProperConsoleWindow m_instance = null;
    private bool m_listening = false;
    private object m_entriesLock;

    private List<ConsoleLogEntry> m_entries = null;

    private Vector2 m_scrollPosition;
    private Vector2 selectedAreaPos;
    private bool m_autoScroll = true;
    private bool m_clearOnPlay = false;
    private bool m_clearOnBuild = false;
    private bool m_errorPause = false;
    private bool m_collapse = false;

    private DateTime m_lastClick = default;
    private float m_doubleClickSpeed = 300 * 10000; // Could be a config ?

    float splitterPos;
    Rect splitterRect;
    private bool dragging = false;
    string searchString = null;
    private LogLevel m_logLevelFilter = LogLevel.All;
    float innerScrollableHeight = 0;
    float outerScrollableHeight = 0;

    // This could be a dictionnary, but Dictionnaries are not Unity-serializables which causes problems when switching Modes
    private int m_logCounter = 0;
    private int m_warningCounter = 0;
    private int m_errorCounter = 0;

    private int m_selectedIndex = -1;

    private CustomLogHandler m_logHandler = null;

    private List<PendingContext> m_pendingContexts = null;

    private static Texture2D m_iconInfo;
    private static Texture2D m_iconWarning;
    private static Texture2D m_iconError;

    private static Texture2D m_iconInfoGray;
    private static Texture2D m_iconWarningGray;
    private static Texture2D m_iconErrorGray;

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
        m_pendingContexts = m_pendingContexts ?? new List<PendingContext>();
        m_listening = false;
        m_entriesLock = new object();
        m_instance = this;
        EditorApplication.playModeStateChanged += ModeChanged;
        InitListener();

        Type editorGuiUtility = typeof(EditorGUIUtility);
        MethodInfo LoadIcon = editorGuiUtility.GetMethod("LoadIcon", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic);
        string[] parameters = { "blabla" };

        m_iconInfo = (Texture2D) LoadIcon.Invoke(null, new object[] { "console.infoicon" });
        m_iconWarning = (Texture2D)LoadIcon.Invoke(null, new object[] { "console.warnicon" });
        m_iconError = (Texture2D)LoadIcon.Invoke(null, new object[] { "console.erroricon" });

        m_iconInfoGray = (Texture2D)LoadIcon.Invoke(null, new object[] { "console.infoicon.sml" });
        m_iconWarningGray = (Texture2D)LoadIcon.Invoke(null, new object[] { "console.warnicon.inactive.sml" });
        m_iconErrorGray = (Texture2D)LoadIcon.Invoke(null, new object[] { "console.erroricon.inactive.sml" });
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
        m_pendingContexts?.Clear();
    }

    private void EnteredEditMode()
    {
        InitListener();
        m_pendingContexts?.Clear();
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
            m_logHandler = new CustomLogHandler(Debug.unityLogger.logHandler, this);
            Debug.unityLogger.logHandler = m_logHandler;
            Application.logMessageReceivedThreaded += Listener;
            m_listening = true;
        }
    }

    public void RemoveListener()
    {
        Application.logMessageReceivedThreaded -= Listener;
        Debug.unityLogger.logHandler = m_logHandler.OriginalHandler;
        m_listening = false;
    }

    private void Listener(string condition, string stackTrace, LogType type)
    {
        lock (m_entriesLock)
        {
            UnityEngine.Object context = null;
            for(int i=0;i<m_pendingContexts.Count;i++)
            {
                if(m_pendingContexts[i].message.Equals(condition) && m_pendingContexts[i].logType == type)
                {
                    context = m_pendingContexts[i].context;
                    m_pendingContexts.RemoveAt(i);
                    break;
                }
            }

            var now = DateTime.Now;
            string newStackTrace = ParseStackTrace(stackTrace, out string firstAsset, out string firstLine);
            m_entries.Add(new ConsoleLogEntry()
            {
                date = now.Ticks,
                timestamp = now.ToString("T", DateTimeFormatInfo.InvariantInfo),
                level = GetLogLevelFromUnityLogType(type),
                message = condition,
                messageFirstLine = condition.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0],
                stackTrace = newStackTrace,
                count = 1,
                context = context,
                firstAsset = firstAsset,
                firstLine = firstLine,
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

    private string ParseStackTrace(string stackTrace, out string firstAsset, out string firstLine)
    {
        firstAsset = null;
        firstLine = null;
        if (string.IsNullOrEmpty(stackTrace))
        {
            return null;
        }

        var split = stackTrace.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        string result = "";

        Regex scriptMatch = new Regex("\\(at\\s([a-zA-Z0-9\\-_\\.\\/]+)\\:(\\d+)\\)", RegexOptions.IgnoreCase); // TODO cache

        for (int i = 0; i < split.Length; i++)
        {
            Match m = scriptMatch.Match(split[i]);
            if (m.Success)
            {
                result += split[i].Replace(m.Value, $"(at <a href=\"{ m.Groups[1].Value }\" line=\"{ m.Groups[2].Value }\">{ m.Groups[1].Value }:{ m.Groups[2].Value }</a>)") + "\n";

                if (string.IsNullOrEmpty(firstAsset))
                {
                    firstAsset = m.Groups[1].Value;
                    firstLine = m.Groups[2].Value;
                }
            }
            else
            {
                result += $"{split[i]}\n";
            }
        }

        return result;
    }

    public void Listener(LogType type, UnityEngine.Object context, string format, params object[] args)
    {
        m_pendingContexts = m_pendingContexts ?? new List<PendingContext>();
        if (context != null && args.Length > 0)
        {
            m_pendingContexts.Add(new PendingContext()
            {
                logType = type,
                context = context,
                message = args[0] as string
            });
        }
    }

    private void Clear()
    {
        m_logCounter = 0;
        m_warningCounter = 0;
        m_errorCounter = 0;
        m_entries.Clear();
        m_pendingContexts.Clear();
    }

    private int GetCounter(LogLevel level)
    {
        if (level.HasFlag(LogLevel.Log)) { return m_logCounter; }
        if (level.HasFlag(LogLevel.Warning)) { return m_warningCounter; }
        return m_errorCounter;
    }

    private void FlagButton(LogLevel level, Texture2D icon, Texture2D iconGray)
    {
        float flagButtonMaxWidth = 60; 
        bool hasFlag = (m_logLevelFilter & level) != 0;
        int counter = GetCounter(level);
        if(counter >= 1000)
        {
            flagButtonMaxWidth = 60;// TODO const
        } else if(counter >= 100)
        {
            flagButtonMaxWidth = 50;
        }
        else if (counter >= 10)
        {
            flagButtonMaxWidth = 42;
        } else
        {
            flagButtonMaxWidth = 37;
        }
        bool newFlagValue = GUILayout.Toggle(hasFlag, new GUIContent($" {(counter > 999 ? "999+" : $"{counter}")}", (counter > 0 ? icon : iconGray)),
            (GUIStyle)"ToolbarButton"
            , GUILayout.MaxWidth(flagButtonMaxWidth), GUILayout.ExpandWidth(false)
            );
        if (hasFlag != newFlagValue)
        {
            m_logLevelFilter ^= level;
        }
    }

    void OnGUI()
    {
        bool callForRepaint = false;
        bool repaint = Event.current.type == EventType.Repaint;

        GUILayout.BeginHorizontal("Toolbar");
        if (GUILayout.Button("Clear", "ToolbarButton", GUILayout.ExpandWidth(false)))
        {
            Clear();
            GUIUtility.keyboardControl = 0;
        }
        bool lastCollapse = m_collapse;
        m_collapse = GUILayout.Toggle(m_collapse, "Collapse", "ToolbarButton", GUILayout.ExpandWidth(false));
        callForRepaint = m_collapse != lastCollapse;
        if(m_collapse != lastCollapse)
        {
            m_selectedIndex = -1;
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
        FlagButton(LogLevel.Log, m_iconInfo, m_iconInfoGray);
        FlagButton(LogLevel.Warning, m_iconWarning, m_iconWarningGray);
        FlagButton(LogLevel.Error, m_iconError, m_iconErrorGray);

        GUILayout.EndHorizontal();
        Rect windowRect = GUILayoutUtility.GetLastRect();

        float startY = 0;
        float totalWidth = Screen.width;
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
        List<ConsoleLogEntry> displayedEntries;
        if (m_collapse)
        {
            DisplayCollapse(filteredEntries, out displayedEntries, totalWidth);
        }
        else
        {
            DisplayList(filteredEntries, out displayedEntries, totalWidth);
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

        float splitterHeight = 10f;
        GUILayout.BeginVertical();
        GUILayout.Space((int)(splitterHeight / 2f));
        GUILayout.Box("",
             GUILayout.Height(1),
             GUILayout.MaxHeight(1),
             GUILayout.MinHeight(1),
             GUILayout.ExpandWidth(true));
        GUILayout.Space((int)(splitterHeight / 2f));
        GUILayout.EndVertical();
        splitterRect = GUILayoutUtility.GetLastRect();
        EditorGUIUtility.AddCursorRect(new Rect(splitterRect), MouseCursor.ResizeVertical); // TODO Editor

        selectedAreaPos = GUILayout.BeginScrollView(selectedAreaPos,
        GUILayout.Height(splitterPos),
        GUILayout.MaxHeight(splitterPos),
        GUILayout.MinHeight(splitterPos));
        if (m_selectedIndex != -1)
        {
            GUIStyle textStyle = GUI.skin.label;
            textStyle.richText = true;
            textStyle.normal.textColor = Color.black;

            var entry = displayedEntries[m_selectedIndex];

            GUILayout.Space(1);
            float currentX = (GUILayoutUtility.GetLastRect()).xMin;

            EditorSelectableLabel(entry.message, textStyle, currentX); // TODO if editor
            //GUILayout.Label($"{entry.message}", textStyle); // TODO if not editor
            if (entry.context != null)
            {
                EditorSelectableLabel(entry.context.name, textStyle, currentX); // TODO if editor// TODO if not editor
            }
            if (!string.IsNullOrEmpty(entry.stackTrace))
            {
                EditorSelectableLabel(entry.stackTrace, textStyle, currentX); // TODO if editor// TODO if not editor
            }
        }
        GUILayout.EndScrollView();

        if (GUILayout.Button("Log"))
        {
            Debug.Log($"Log {DateTime.Now.ToString()} {m_listening}", Camera.main);
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

        if (GUILayout.Button("Add 1000 Log"))
        {
            for (int i = 0; i < 1000; i++)
            {
                Debug.Log($"Log {DateTime.Now.ToString()} {m_listening}");
            }
        }

        if (Event.current != null)
        {
            switch (Event.current.rawType)
            {
                case EventType.MouseDown:
                    if (splitterRect.Contains(Event.current.mousePosition))
                    {
                        //Debug.Log("Start dragging");
                        dragging = true;
                    }
                    break;
                case EventType.MouseDrag:
                    if (dragging)
                    {
                        //Debug.Log("moving splitter");
                        splitterPos -= Event.current.delta.y;
                        Repaint();
                    }
                    break;
                case EventType.MouseUp:
                    if (dragging)
                    {
                        //Debug.Log("Done dragging");
                        dragging = false;
                    }
                    break;
                case EventType.MouseMove:
                    if (splitterRect.Contains(Event.current.mousePosition))
                    {
                    } else if(!dragging)
                    {

                    }
                    break;
            }
        }

        if (callForRepaint)
        {
            Repaint();
        }
    }

    private void EditorSelectableLabel(string text, GUIStyle textStyle, float currentX)
    {
        var content = new GUIContent(text);
        float height = textStyle.CalcHeight(content, EditorGUIUtility.currentViewWidth);
        var lastRect = GUILayoutUtility.GetLastRect();
        EditorGUI.SelectableLabel(new Rect(currentX, lastRect.yMax, EditorGUIUtility.currentViewWidth, height), text, textStyle);
        GUILayout.Space(height);
    }

    private void DisplayEntry(ConsoleLogEntry entry, int idx, float totalWidth)
    {
        var saveColor = GUI.color;
        var saveBGColor = GUI.backgroundColor;
        float imageSize = 35;
        float collapseBubbleSize = m_collapse ? 40 : 0;
        float empiricalPaddings = 20;
        GUIStyle currentStyle = m_skin.FindStyle("OddEntry");
        GUIStyle textStyle = m_skin.FindStyle("EntryLabel"); // Cache styles
        if (idx == m_selectedIndex) {
            currentStyle = m_skin.FindStyle("SelectedEntry");
            textStyle = m_skin.FindStyle("EntryLabelSelected"); // Cache styles
        }
        else if (idx % 2 == 0)
        {
            currentStyle = m_skin.FindStyle("EvenEntry"); // Cache styles
        }
        GUILayout.BeginHorizontal(currentStyle, GUILayout.Height(40));
        //GUI.color = saveColor;
        // Picto space
        GUILayout.BeginHorizontal(GUILayout.Width(imageSize + 10));
        GUILayout.FlexibleSpace();
        GUILayout.Box(GetEntryIcon(entry), GUIStyle.none, GUILayout.Width(imageSize), GUILayout.Height(imageSize));
        GUILayout.EndHorizontal();
        // Text space
        GUILayout.BeginVertical();
        GUILayout.Label($"[{entry.timestamp}] {entry.messageFirstLine}", textStyle, GUILayout.Width(totalWidth - imageSize - collapseBubbleSize - empiricalPaddings));
        if (!string.IsNullOrEmpty(entry.stackTrace))
        {
            GUILayout.Label($"{StackStraceFirstLine(entry.stackTrace)}", textStyle, GUILayout.Width(totalWidth - imageSize - collapseBubbleSize - empiricalPaddings)); // TODO cache this line
        }
        GUILayout.EndVertical();
        //GUILayout.Label("", GUILayout.ExpandWidth(true));
        GUILayout.FlexibleSpace();
        // Collapse Space
        if (m_collapse)
        {
            GUILayout.Label($"{entry.count}", GUILayout.ExpandWidth(false), GUILayout.Width(collapseBubbleSize)); // TODO style
        }
        // Category Space
        GUILayout.EndHorizontal();

        Rect r = GUILayoutUtility.GetLastRect();
        if(GUI.Button(r, GUIContent.none, GUIStyle.none))
        {
            if(entry.context != null)
            {
                EditorGUIUtility.PingObject(entry.context); // TODO Editor
            }
            if(m_selectedIndex == idx && DateTime.Now.Ticks - m_lastClick.Ticks < m_doubleClickSpeed)
            {
                HandleDoubleClick(entry);
            }
            m_selectedIndex = idx;
            m_lastClick = DateTime.Now;
        }

        GUI.color = saveColor;
        GUI.backgroundColor = saveBGColor;
    }

    private Texture GetEntryIcon(ConsoleLogEntry entry)
    {
        if (entry.level.HasFlag(LogLevel.Log)) { return m_iconInfo; }
        if (entry.level.HasFlag(LogLevel.Warning)) { return m_iconWarning; }
        return m_iconError;
    }

    private void HandleDoubleClick(ConsoleLogEntry entry)
    {
        if (!string.IsNullOrEmpty(entry.firstAsset))
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(entry.firstAsset);
            if (!string.IsNullOrEmpty(entry.firstLine))
            {
                AssetDatabase.OpenAsset(asset, int.Parse(entry.firstLine));
            }else
            {
                AssetDatabase.OpenAsset(asset);
            }
        }
    }

    private void DisplayList(List<ConsoleLogEntry> filteredEntries, out List<ConsoleLogEntry> displayedEntries, float totalWidth)
    {
        for (int i = 0; i < filteredEntries.Count; i++)
        {
            DisplayEntry(filteredEntries[i], i, totalWidth);
        }
        displayedEntries = filteredEntries;
    }
    private void DisplayCollapse(List<ConsoleLogEntry> filteredEntries, out List<ConsoleLogEntry> displayedEntries, float totalWidth)
    {
        List<ConsoleLogEntry> collapsedEntries = new List<ConsoleLogEntry>();


        for (int i = 0; i < filteredEntries.Count; i++)
        {
            bool found = false;
            int foundIdx = 0;
            for (int j = 0; j < collapsedEntries.Count; j++)
            {
                if(collapsedEntries[j].message == filteredEntries[i].message)
                {
                    foundIdx = j;
                    found = true;
                }
            }
            if (found)
            {
                collapsedEntries[foundIdx] = new ConsoleLogEntry()
                {
                    count = collapsedEntries[foundIdx].count + 1,
                    date = collapsedEntries[foundIdx].date,
                    message = collapsedEntries[foundIdx].message,
                    level = collapsedEntries[foundIdx].level,
                    stackTrace = collapsedEntries[foundIdx].stackTrace,
                    timestamp = collapsedEntries[foundIdx].timestamp
                };
            } else
            {
                collapsedEntries.Add(filteredEntries[i]);
            }
        }

        for (int i = 0; i < collapsedEntries.Count; i++)
        {
            DisplayEntry(collapsedEntries[i], i, totalWidth);
        }

        displayedEntries = collapsedEntries;
    }

    private string StackStraceFirstLine(string stack)
    {
        var split = stack.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if(split.Length == 0)
        {
            return "";
        }
        if(split.Length > 1)
        {
            return split[1];
        }
        return split[0];
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

public class CustomLogHandler : ILogHandler
{
    private ILogHandler m_originalHandler;
    private ProperConsoleWindow m_console;

    public ILogHandler OriginalHandler => m_originalHandler;

    public CustomLogHandler(ILogHandler host, ProperConsoleWindow console)
    {
        m_originalHandler = host;
        m_console = console;
    }

    public void LogException(Exception exception, UnityEngine.Object context)
    {
        m_console.Listener(LogType.Exception, context, "{0}", exception.Message, exception.StackTrace);
        m_originalHandler.LogException(exception, context);
    }

    public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
    {
        m_console.Listener(logType, context, format, args);
        m_originalHandler.LogFormat(logType, context, format, args);
    }
}