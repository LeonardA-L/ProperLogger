using UnityEditor;

namespace ProperLogger
{
    internal class EditorConfigs : ConfigsProvider
    {
        protected override bool GetBool(string key, bool defaultValue)
        {
            return EditorPrefs.GetBool(key, defaultValue);
        }

        protected override float GetFloat(string key, float defaultValue)
        {
            return EditorPrefs.GetFloat(key, defaultValue);
        }

        protected override int GetInt(string key, int defaultValue)
        {
            return EditorPrefs.GetInt(key, defaultValue);
        }

        protected override string GetString(string key, string defaultValue)
        {
            return EditorPrefs.GetString(key, defaultValue);
        }

        protected override void Reset(string key)
        {
            EditorPrefs.DeleteKey(key);
        }

        protected override void Save()
        {
        }

        protected override void SetBool(string key, bool newValue)
        {
            EditorPrefs.SetBool(key, newValue);
        }

        protected override void SetFloat(string key, float newValue)
        {
            EditorPrefs.SetFloat(key, newValue);
        }

        protected override void SetInt(string key, int newValue)
        {
            EditorPrefs.SetInt(key, newValue);
        }

        protected override void SetString(string key, string newValue)
        {
            EditorPrefs.SetString(key, newValue);
        }

        private LogCategoriesConfig AttemptFindingCategoriesAsset()
        {
            string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(LogCategoriesConfig)));
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                LogCategoriesConfig foundAsset = AssetDatabase.LoadAssetAtPath<LogCategoriesConfig>(assetPath);
                if (foundAsset != null)
                {
                    return foundAsset;
                }
            }
            return null;
        }

        internal override LogCategoriesConfig CurrentCategoriesConfig
        {
            get
            {
                string guid = GetString("ProperConsole.CategoriesConfigPath", "");
                if (string.IsNullOrEmpty(guid))
                {
                    return AttemptFindingCategoriesAsset();
                }
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                {
                    return AttemptFindingCategoriesAsset();
                }
                return (LogCategoriesConfig)AssetDatabase.LoadMainAssetAtPath(path);
            }
            set
            {
                if (value == null)
                {
                    SetString("ProperConsole.CategoriesConfigPath", "");
                    return;
                }
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string GUID, out long localId);
                SetString("ProperConsole.CategoriesConfigPath", GUID);
                Save();
            }
        }
    }
}