using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ProperLogger
{
#if ENABLE_INPUT_SYSTEM
    public class InGameConsoleOpener : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The window will open when this key is pressed")]
        private InputAction m_triggerAction = new InputAction("OpenConsole", binding: "<Keyboard>/F1");

        void Awake()
        {
            m_triggerAction.performed += OnToggle;
        }

        void OnEnable()
        {
            m_triggerAction.Enable();
        }

        void OnDisable()
        {
            m_triggerAction.Disable();
        }

        //private void OnToggle()
        private void OnToggle(InputAction.CallbackContext obj)
        {
            if (ProperConsoleGameWindow.Instance != null)
                {
                    ProperConsoleGameWindow.Instance.Toggle();
                }
            }
        }
#else
    public class InGameConsoleOpener : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The window will open when this key is pressed")]
        private KeyCode m_triggerKey = KeyCode.F1;

        void Update()
        {
            if (ProperConsoleGameWindow.Instance != null && m_triggerKey != KeyCode.None && Input.GetKeyDown(m_triggerKey))
            {
                ProperConsoleGameWindow.Instance.Toggle();
            }
        }
    }
#endif
}