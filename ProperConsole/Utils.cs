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
        private static Regex s_linkPreMatchRegex = null;
        private static Regex s_warningLinkMatchRegex = null;

        private static Assembly[] s_allAssemblies = null;
        private static Assembly[] AllAssemblies => s_allAssemblies ?? (s_allAssemblies = AppDomain.CurrentDomain.GetAssemblies());

        private static Dictionary<string, bool> s_cachedHiddenCalls = null;
        public static Dictionary<string, bool> CachedHiddenCalls => s_cachedHiddenCalls ?? (s_cachedHiddenCalls = new Dictionary<string, bool>());

        internal static void ClearAssemblies()
        {
            s_allAssemblies = null;
        }

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
            if (isCallStack && lines.Length > 1 && lines[0].StartsWith(nameof(UnityEngine)))
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

            if (s_linkPreMatchRegex == null)
            {
                s_linkPreMatchRegex = new Regex("\\:(\\d+)\\)$", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
            }

            if (s_linkMatchRegex == null)
            {
                //s_linkMatchRegex = new Regex("^((.+)[:\\.](.+)(\\s?\\(.*\\))\\s?)\\(at\\s([a-zA-Z0-9\\-_\\.\\/]+)\\:(\\d+)\\)", RegexOptions.IgnoreCase);
                s_linkMatchRegex = new Regex("^(([^\\s]+)[:\\.]([^\\s]+)(\\s?\\([^\\s]*\\))\\s?)\\(at\\s([a-zA-Z0-9\\-_\\.\\/]+)\\:(\\d+)\\)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
            }

            for (int i = 0; i < split.Length; i++)
            {
                if (split[i].StartsWith(typeof(CustomLogHandler).FullName) && !split[i].Contains(nameof(CustomLogHandler.LogException)))
                {
                    result = string.Empty;
                    continue;
                }
                if (s_linkPreMatchRegex.IsMatch(split[i]))
                {
                    Match m = s_linkMatchRegex.Match(split[i]);
                    if (m.Success)
                    {
                        List<string> groups = new List<string>();
                        for (int k = 0; k < m.Groups.Count; k++)
                        {
                            groups.Add(m.Groups[k].Value);
                        }

                        bool isHidden = IsHiddenCall(m);

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
                else
                {
                    result += split[i].ToString() + Environment.NewLine;
                }
            }

            return result;
        }

        internal static bool ParseMessage(string message, out string firstAsset, out string firstLine, out string parsedMessage)
        {
            firstAsset = null;
            firstLine = null;
            parsedMessage = null;
            if (string.IsNullOrEmpty(message))
            {
                return false;
            }

            var split = message.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            string result = string.Empty;

            if (s_warningLinkMatchRegex == null)
            {
                s_warningLinkMatchRegex = new Regex("^([\\/a-zA-Z0-9\\-_\\.\\\\]+)(\\((\\d+),\\d+\\))", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
            }

            bool success = false;

            for (int i = 0; i < split.Length; i++)
            {
                if (split[i].StartsWith(typeof(CustomLogHandler).FullName) && !split[i].Contains(nameof(CustomLogHandler.LogException)))
                {
                    result = string.Empty;
                    continue;
                }
                Match m = s_warningLinkMatchRegex.Match(split[i]);
                if (i == 0 && m.Success)
                {
                    success = true;
                    List<string> groups = new List<string>();
                    for (int k = 0; k < m.Groups.Count; k++)
                    {
                        groups.Add(m.Groups[k].Value);
                    }

                    bool isHidden = IsHiddenCall(m);

                    if (isHidden)
                    {
                        continue;
                    }
                    result += split[i].Replace(m.Value, $"<a href=\"{ m.Groups[1].Value }\" line=\"{ m.Groups[3].Value }\">{ m.Groups[0].Value }</a>") + Environment.NewLine;

                    if (string.IsNullOrEmpty(firstAsset))
                    {
                        firstAsset = m.Groups[1].Value;
                        firstLine = m.Groups[3].Value;
                    }
                }
                else
                {
                    result += split[i].ToString() + Environment.NewLine;
                }
            }

            parsedMessage = result;
            return success;
        }

        private static bool IsHiddenCall(Match m)
        {
            if(CachedHiddenCalls.TryGetValue(m.Groups[1].Value, out bool hidden))
            {
                return hidden;
            }
            try
            {
                foreach (Assembly ass in AllAssemblies)
                {
                    foreach (Type t in ass.GetExportedTypes())
                    {
                        if (t.Name == m.Groups[2].Value)
                        {
                            MethodInfo method = t.GetMethod(m.Groups[3].Value, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (method != null)
                            {
                                var attributes = method.GetCustomAttributes(typeof(HideInCallStackAttribute), true);

                                if (attributes.Length > 0)
                                {
                                    CachedHiddenCalls.Add(m.Groups[1].Value, true);
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception) { }
            CachedHiddenCalls.Add(m.Groups[1].Value, false);
            return false;
        }

        #endregion Text Manipulation

        internal static bool IsMainThread(System.Threading.Thread mainThread)
        {
            return mainThread.Equals(System.Threading.Thread.CurrentThread);
        }
    }
}