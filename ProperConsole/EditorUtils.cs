﻿#if UNITY_EDITOR
using UnityEditor;

namespace ProperLogger
{
    internal class EditorUtils
    {
        // TODO verify if this works when we're in a package (finding may not work with the folder structure)
        internal static T LoadAssetByName<T>(string assetName) where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets(assetName, null);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath(path, typeof(T));
                if (asset is T t)
                    return t;
            }
            return null;
        }

        internal static string FindAssetPath<T>(string assetName) where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets(assetName, null);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath(path, typeof(T));
                if (asset is T t)
                {
                    return path;
                }
            }
            return null;
        }
    }
}
#endif