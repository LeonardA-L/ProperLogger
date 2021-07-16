using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProperLogger
{
    //[CreateAssetMenu(menuName ="HiddenMethods")]
    [System.Reflection.Obfuscation(Exclude = true)]
    internal class HiddenMethodsRegistry : ScriptableObject
    {
        private static HiddenMethodsRegistry s_instance = null;
        public static HiddenMethodsRegistry Instance => s_instance;

        [SerializeField]
        private List<string> m_hiddenMethods = null;

        internal void SetMethods(List<string> methods)
        {
            m_hiddenMethods = new List<string>(methods);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        private void OnEnable()
        {
#if DEBUG
            Debug.Log($"HiddenMethodsRegistry OnEnable {m_hiddenMethods.Count}");
#endif
            s_instance = this;

            if (!Application.isEditor)
            {
#if DEBUG
                Debug.Log("Setting hidden methods");
#endif
                Utils.SetHiddenMethods(m_hiddenMethods);
            }
        }
    }
}