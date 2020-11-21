﻿using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ProperLogger
{
    public abstract class ImGuiWindow<T> : MonoBehaviour where T: MonoBehaviour
    {
        private static T m_instance = null;
        public static T Instance => m_instance;

        [SerializeField, Obfuscation(Exclude = true)]
        [Tooltip("The window will open when this key is pressed")]
        private KeyCode m_triggerKey = KeyCode.None;

        [SerializeField, Obfuscation(Exclude = true)]
        [Tooltip("GUI Skin Override")]
        private GUISkin m_skin = null;
        public GUISkin Skin => m_skin;

        protected bool m_active = false;
        private int m_windowID = 0;

        [SerializeField, Obfuscation(Exclude = true)]
        [Tooltip("Size and Position of this window")]
        protected Rect m_windowRect = new Rect(30, 30, 1200, 700);

        [SerializeField, Obfuscation(Exclude = true)]
#if !DEBUG
        [HideInInspector]
#endif
        protected int m_depth = 1;

        protected abstract string WindowName {get;}

        private float m_refScreenWidth = 1282f;
        private float m_refScreenHeight = 772f;

        [Obfuscation(Exclude = true)]
        protected virtual void Awake()
        {
            m_instance = this as T;
            m_active = false;

            if (m_windowRect.x < 0)
            {
                m_windowRect.x += (Screen.width / (Screen.width / 1282f)) - m_windowRect.width;
            }
        }

        [Obfuscation(Exclude = true)]
        protected virtual void OnDestroy()
        {
        }

        [Obfuscation(Exclude = true)]
        protected virtual void OnDisable()
        {
            if (m_active)
            {
                Toggle();
            }
        }

        [Obfuscation(Exclude = true)]
        protected virtual void Update()
        {
#if ENABLE_INPUT_SYSTEM
            // TODO implement and test this
#endif
            if (m_triggerKey != KeyCode.None && Input.GetKeyDown(m_triggerKey))
            {
                Toggle();
            }
        }

        internal virtual void Toggle()
        {
            m_active = !m_active;
            if (m_active)
            {
                OnWindowEnabled();
            }
            else
            {
                OnWindowDisabled();
            }
        }

        internal virtual void Open()
        {
            if (!m_active)
            {
                Toggle();
            }
            GUI.BringWindowToFront(m_windowID);
        }

        protected virtual void OnWindowEnabled()
        {
        }

        protected virtual void OnWindowDisabled()
        {
        }

        [Obfuscation(Exclude = true)]
        protected virtual void OnGUI()
        {
            if (!m_active)
            {
                return;
            }

            if (m_skin != null)
            {
                GUI.skin = m_skin;
            }

            GUI.depth = m_depth;

            float xFactor = Screen.width / m_refScreenWidth;
            float yFactor = Screen.height / m_refScreenHeight;
            GUIUtility.ScaleAroundPivot(new Vector2(xFactor, yFactor), Vector2.zero);

            m_windowID = GUIUtility.GetControlID(FocusType.Passive);
            m_windowRect = GUI.Window(m_windowID, m_windowRect, DoGui, WindowName);

            m_windowRect.x = Mathf.Clamp(m_windowRect.x, -m_windowRect.width + 20, m_refScreenWidth - 20);
            m_windowRect.y = Mathf.Clamp(m_windowRect.y, 0, m_refScreenHeight - 20);
        }

        protected virtual void DoGui(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 20));  // To make the window draggable
        }

        protected bool DisplayCloseButton()
        {
            GUI.depth = 0;
            var buttonRect = new Rect(m_windowRect.width - 20, 0, 20, 20);
            if(GUI.Button(buttonRect, Strings.x, Strings.CloseWindowButton))
            {
                Toggle();
                return true;
            }
            GUI.depth = m_depth;
            return false;
        }
    }
}