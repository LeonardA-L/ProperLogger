using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[assembly: Obfuscation(Exclude = false, Feature = "namespace('ProperLogger.Tools'):-rename")]
[assembly: Obfuscation(Exclude = false, Feature = "namespace('ProperLogger.Tools'):-constants")]

namespace ProperLogger.Tools
{
    [System.Reflection.Obfuscation(Exclude = true, ApplyToMembers = true)]
    public class DDebug
    {
        [Obfuscation(Exclude = true)]
        public static void Assert(bool condition, string message, Object context, params string[] categories)
        {
            Debug.Assert(condition, string.Join("", categories.Select(c => $"[{c}] ")) + message.ToString(), context);
        }
        [Obfuscation(Exclude = true)]
        public static void Assert(bool condition)
        {
            Assert(condition, "Assertion Failed", null);
        }
        [Obfuscation(Exclude = true)]
        public static void Assert(bool condition, object message, Object context, params string[] categories)
        {
            Assert(condition, message.ToString(), context, categories);
        }
        [Obfuscation(Exclude = true)]
        public static void Assert(bool condition, object message)
        {
            Assert(condition, message.ToString(), null);
        }
        [Obfuscation(Exclude = true)]
        public static void Assert(bool condition, Object context, params string[] categories)
        {
            Assert(condition, "Assertion Failed", context, categories);
        }
        [Obfuscation(Exclude = true)]
        public static void Log(object message, params string[] categories)
        {
            Log(message, null, categories);
        }
        [Obfuscation(Exclude = true)]
        public static void Log(object message, Object context, params string[] categories)
        {
            Debug.Log(string.Join("", categories.Select(c => $"[{c}] ")) + message.ToString(), context);
        }
        /*[Obfuscation(Exclude = true)]
        public static void LogAssertion(object message, params string[] categories)
        {
            LogAssertion(message, (Object)null, categories);
        }
        [Obfuscation(Exclude = true)]
        public static void LogAssertion(object message, Object context, params string[] categories)
        {
            UnityEngine.Debug.LogAssertion(JoinedCategories(categories) + message.ToString(), context);
        }*/
        [Obfuscation(Exclude = true)]
        public static void LogError(object message, params string[] categories)
        {
            LogError(message, null, categories);
        }
        [Obfuscation(Exclude = true)]
        public static void LogError(object message, Object context, params string[] categories)
        {
            Debug.LogError(string.Join("", categories.Select(c => $"[{c}] ")) + message.ToString(), context);
        }
        [Obfuscation(Exclude = true)]
        public static void LogWarning(object message, params string[] categories)
        {
            LogWarning(message, null, categories);
        }
        [Obfuscation(Exclude = true)]
        public static void LogWarning(object message, Object context, params string[] categories)
        {
            Debug.LogWarning(string.Join("", categories.Select(c => $"[{c}] ")) + message.ToString(), context);
        }

        private static string JoinedCategories(params string[] categories) => string.Join("", categories.Select(c => $"[{c}] "));
    }
}