#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ProperLogger
{
    internal class BuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildTarget target, string path)
        {
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (ProperConsoleWindow.Instance != null)
            {
                ProperConsoleWindow.Instance.OnBuild();
            }
        }
    }
}

#endif