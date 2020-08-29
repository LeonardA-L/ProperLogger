using System.Collections;
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

public class ProperConsoleWindow : EditorWindow
{
    private static ProperConsoleWindow m_instance = null;
    private bool m_listening = false;
    private object m_entriesLock;

    private List<string> m_entries = null;

    private Vector2 m_scrollPosition;
    private bool m_autoScroll = true;
    private bool m_clearOnPlay = false;
    private bool m_clearOnBuild = false;
    private bool m_errorPause = false;

    float innerScrollableHeight = 0;
    float outerScrollableHeight = 0;

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

    private void Awake()
    {
        m_clearOnPlay = false;
        m_clearOnBuild = false;
        m_errorPause = false;
    }

    private void OnEnable()
    {
        Debug.Log("OnEnable");
        m_entries = m_entries ?? new List<string>();
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
        Clear();
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
        m_entries = m_entries ?? new List<string>();
        lock (m_entriesLock)
        {
            m_entries.Add(condition);
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

    void OnGUI()
    {
        GUILayout.BeginHorizontal((GUIStyle)"Toolbar");
        if (GUILayout.Button("Clear", (GUIStyle)"ToolbarButton"))
        {
            Clear();
            GUIUtility.keyboardControl = 0;
        }
        m_clearOnPlay = GUILayout.Toggle(m_clearOnPlay, "Clear on Play", "ToolbarButton");
        m_clearOnBuild = GUILayout.Toggle(m_clearOnBuild, "Clear on Build", "ToolbarButton");
        m_errorPause = GUILayout.Toggle(m_errorPause, "Error Pause", "ToolbarButton");
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();


        bool repaint = Event.current.type == EventType.Repaint;

        m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);

        if (repaint)
        {
            float scrollTolerance = 0;
            m_autoScroll = m_scrollPosition.y >= (innerScrollableHeight - outerScrollableHeight - scrollTolerance);
        }

        GUILayout.BeginVertical();

        if(m_entries.Count == 0) GUILayout.Space(10);

        for (int i= 0;i<m_entries.Count;i++)
        {
            GUILayout.Label(m_entries[i]);
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
            m_scrollPosition.y = innerScrollableHeight - outerScrollableHeight;
        }

        if (GUILayout.Button("Log"))
        {
            Debug.Log($"Log {DateTime.Now.ToString()} {m_listening} {m_autoScroll} {m_entries.Count} {(m_entries.Count+1) * 40}");
        }

        if (GUILayout.Button("LogError"))
        {
            Debug.LogError("Error");
            object bad = null;
            bad.ToString();
            throw new Exception("Manual error");
        }
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