using System.Collections;
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

public class ProperConsoleWindow : EditorWindow
{
    private static ProperConsoleWindow m_instance = null;
    private bool m_listening = false;
    private object m_entriesLock;

    private List<string> m_entries = null;

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
        m_entries = m_entries ?? new List<string>();
        lock (m_entriesLock)
        {
            m_entries.Add(condition);
        }
        this.Repaint();
    }

    void OnGUI()
    {
        GUILayout.Label("Hello", EditorStyles.boldLabel);
        m_entries = m_entries ?? new List<string>();

        if (m_entries.Count > 0)
        {
            GUILayout.Label(m_entries.Last());
        }

        if (GUILayout.Button("Log"))
        {
            Debug.Log($"{DateTime.Now.ToString()} {m_listening}");
        }
    }
}