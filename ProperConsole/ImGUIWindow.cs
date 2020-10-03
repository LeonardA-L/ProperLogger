using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProperLogger
{
    public abstract class ImGuiWindow<T> : MonoBehaviour where T: MonoBehaviour
    {
        private static T m_instance = null;
        public static T Instance => m_instance;

#if ENABLE_LEGACY_INPUT_MANAGER
        [SerializeField]
        private KeyCode m_triggerKey = KeyCode.None;
#endif
        [SerializeField]
        private GUISkin m_skin = null;
        public GUISkin Skin => m_skin;

        protected bool m_active = false;
        private int m_windowID = 0;

        [SerializeField]
        protected Rect m_windowRect = new Rect(30, 30, 1200, 700);

        [SerializeField]
        protected int m_depth = 1;

        protected abstract string WindowName {get;}

        protected virtual void Awake()
        {
            m_instance = this as T;
            m_active = false;

            if (m_windowRect.x < 0)
            {
                m_windowRect.x += (Screen.width / (Screen.width / 1280f)) - m_windowRect.width;
            }
        }

        protected virtual void OnDestroy()
        {
        }

        protected virtual void OnDisable()
        {
            if (m_active)
            {
                Toggle();
            }
        }

        protected virtual void Update()
        {
#if ENABLE_INPUT_SYSTEM
            // TODO implement and test this
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (m_triggerKey != KeyCode.None && Input.GetKeyDown(m_triggerKey))
            {
                Toggle();
            }
#endif
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

            float refScreenWidth = 1280f;
            float refScreenHeight = 768f;

            float xFactor = Screen.width / refScreenWidth;
            float yFactor = Screen.height / refScreenHeight;
            GUIUtility.ScaleAroundPivot(new Vector2(xFactor, yFactor), Vector2.zero);

            m_windowID = GUIUtility.GetControlID(FocusType.Passive);
            m_windowRect = GUI.Window(m_windowID, m_windowRect, DoGui, WindowName);

            m_windowRect.x = Mathf.Clamp(m_windowRect.x, -m_windowRect.width + 20, refScreenWidth - 20);
            m_windowRect.y = Mathf.Clamp(m_windowRect.y, 0, refScreenHeight - 20);
        }

        protected virtual void DoGui(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 20));  // To make the window draggable
        }
    }
}