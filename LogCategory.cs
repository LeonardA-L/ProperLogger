using UnityEngine;

namespace ProperLogger
{
    [System.Serializable]
    public class LogCategory
    {
        [SerializeField]
        private string m_name = null;
        [SerializeField]
        private Color m_color = Color.red;
        /*[SerializeField]
        private Sprite m_icon = null;*/

        public LogCategory(string name)
        {
            m_name = name;
        }

        //public Sprite Icon => m_icon;
        public string Name => m_name;
        public Color Color => m_color;
    }
}