using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ProperLogger
{
    [System.Serializable]
    [Obfuscation(Exclude = true, ApplyToMembers = false)]
    public class LogCategory
    {
        /*internal static List<Color> s_categoryColors = new List<Color>()
        {
            new Color(50f / 255f, 168f / 255f, 82f / 255f, 1f),
            new Color(36f / 255f, 209f / 255f, 203f / 255f, 1f),
            new Color(45f / 255f, 101f / 255f, 204f / 255f, 1f),
            new Color(133f / 255f, 66f / 255f, 227f / 255f, 1f),
            new Color(227f / 255f, 36f / 255f, 167f / 255f, 1f),
            new Color(217f / 255f, 35f / 255f, 35f / 255f, 1f),
            new Color(255f / 255f, 180f / 255f, 51f / 255f, 1f),
            new Color(124f / 255f, 219f / 255f, 105f / 255f, 1f),
        };*/ // TODO const

        [SerializeField, Obfuscation(Exclude = true)]
        private string m_name = null;
        [SerializeField, Obfuscation(Exclude = true)]
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