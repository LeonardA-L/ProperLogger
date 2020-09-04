using UnityEngine;

namespace ProperLogger
{
    [System.Serializable]
    public class LogCategory
    {
        [SerializeField]
        private string m_name = null;

        public LogCategory(string name)
        {
            m_name = name;
        }

        public string Name => m_name;
    }
}