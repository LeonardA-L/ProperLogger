using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ProperLogger
{
    internal class Utils
    {
        private static Regex s_linkMatchRegex = null;

        internal static LogLevel GetLogLevelFromUnityLogType(LogType type)
        {
            switch (type)
            {
                case LogType.Error:
                    return LogLevel.Error;
                case LogType.Assert:
                    return LogLevel.Assert;
                case LogType.Warning:
                    return LogLevel.Warning;
                case LogType.Log:
                default:
                    return LogLevel.Log;
                case LogType.Exception:
                    return LogLevel.Exception;
            }
        }
        #region Text Manipulation

        internal static string GetFirstLines(string[] lines, int skip, int count, bool isCallStack)
        {
            if (lines == null || lines.Length == 0)
            {
                return string.Empty;
            }
            if (isCallStack && lines.Length > 1)
            {
                skip += 1;
            }
            return string.Join(Environment.NewLine, lines.Skip(skip).Take(count));
        }

        internal static string[] GetLines(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }
            return text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        internal static string ParseStackTrace(string stackTrace, out string firstAsset, out string firstLine)
        {
            firstAsset = null;
            firstLine = null;
            if (string.IsNullOrEmpty(stackTrace))
            {
                return null;
            }

            var split = stackTrace.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            string result = string.Empty;

            if(s_linkMatchRegex == null)
            {
                s_linkMatchRegex = new Regex("^((.+)[:\\.](.+)(\\s?\\(.*\\))\\s?)\\(at\\s([a-zA-Z0-9\\-_\\.\\/]+)\\:(\\d+)\\)", RegexOptions.IgnoreCase);
            }

            for (int i = 0; i < split.Length; i++)
            {
                Match m = s_linkMatchRegex.Match(split[i]);
                if (m.Success)
                {
                    List<string> groups = new List<string>();
                    for (int k = 0; k < m.Groups.Count; k++)
                    {
                        groups.Add(m.Groups[k].Value);
                    }

                    bool isHidden = false;
                    try
                    {
                        if (m.Groups[2].Value == typeof(CustomLogHandler).FullName)
                        {
                            result = string.Empty;
                            continue;
                        }

                        Type type = Type.GetType(m.Groups[2].Value);
                        MethodInfo method = type.GetMethod(m.Groups[3].Value, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Instance);
                        var attributes = method.GetCustomAttributes(typeof(HideInCallStackAttribute), true); 
                        isHidden = attributes.Length > 0;
                    }
                    catch (Exception) { }

                    if (isHidden)
                    {
                        continue;
                    }
                    result += split[i].Replace(m.Value, $"{m.Groups[1].Value}(at <a href=\"{ m.Groups[5].Value }\" line=\"{ m.Groups[6].Value }\">{ m.Groups[5].Value }:{ m.Groups[6].Value }</a>)") + Environment.NewLine;

                    if (string.IsNullOrEmpty(firstAsset))
                    {
                        firstAsset = m.Groups[5].Value;
                        firstLine = m.Groups[6].Value;
                    }
                }
                else
                {
                    result += split[i].ToString() + Environment.NewLine;
                }
            }

            return result;
        }

        #endregion Text Manipulation

        internal static bool IsMainThread(System.Threading.Thread mainThread)
        {
            return mainThread.Equals(System.Threading.Thread.CurrentThread);
        }
    }
}