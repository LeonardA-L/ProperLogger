#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProperLogger
{
    internal class AssetPostProcessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if(ProperConsoleWindow.Instance != null)
            {
                ProperConsoleWindow.Instance.AfterAssetProcess();
            }
        }
    }
}

#endif