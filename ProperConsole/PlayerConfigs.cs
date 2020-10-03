using UnityEngine;

namespace ProperLogger
{
    internal class PlayerConfigs : ConfigsProvider
    {

        protected override bool GetBool(string key, bool defaultValue)
        {
            return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;
        }

        protected override float GetFloat(string key, float defaultValue)
        {
            return PlayerPrefs.GetFloat(key, defaultValue);
        }

        protected override int GetInt(string key, int defaultValue)
        {
            return PlayerPrefs.GetInt(key, defaultValue);
        }

        protected override string GetString(string key, string defaultValue)
        {
            return PlayerPrefs.GetString(key, defaultValue);
        }

        protected override void Reset(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }

        protected override void Save()
        {
            PlayerPrefs.Save();
        }

        protected override void SetBool(string key, bool newValue)
        {
            PlayerPrefs.SetInt(key, newValue ? 1 : 0);
        }

        protected override void SetFloat(string key, float newValue)
        {
            PlayerPrefs.SetFloat(key, newValue);
        }

        protected override void SetInt(string key, int newValue)
        {
            PlayerPrefs.SetInt(key, newValue);
        }

        protected override void SetString(string key, string newValue)
        {
            PlayerPrefs.SetString(key, newValue);
        }

        internal override LogCategoriesConfig CurrentCategoriesConfig { get => (ProperConsoleGameWindow.Instance == null ? null : ProperConsoleGameWindow.Instance.CategoriesAsset); set { Debug.LogAssertion("This should never be called"); } }
    }
}