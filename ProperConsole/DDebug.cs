using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProperLogger
{
    [System.Reflection.Obfuscation(Exclude = true, ApplyToMembers = true)]
    public class DDebug
    {
        public static void Assert(bool condition, string message, Object context, params string[] categories)
        {
            Debug.Assert(condition, string.Join("", categories.Select(c => $"[{c}] ")) + message.ToString(), context);
        }
        public static void Assert(bool condition)
        {
            Assert(condition, "Assertion Failed", null);
        }
        public static void Assert(bool condition, object message, Object context, params string[] categories)
        {
            Assert(condition, message.ToString(), context, categories);
        }
        public static void Assert(bool condition, object message)
        {
            Assert(condition, message.ToString(), null);
        }
        public static void Assert(bool condition, Object context, params string[] categories)
        {
            Assert(condition, "Assertion Failed", context, categories);
        }
        public static void Log(object message, params string[] categories)
        {
            Log(message, null, categories);
        }
        public static void Log(object message, Object context, params string[] categories)
        {
            Debug.Log(string.Join("", categories.Select(c => $"[{c}] ")) + message.ToString(), context);
        }
        public static void LogAssertion(object message, params string[] categories)
        {
            LogAssertion(message, null, categories);
        }
        public static void LogAssertion(object message, Object context, params string[] categories)
        {
            Debug.LogAssertion(string.Join("", categories.Select(c => $"[{c}] ")) + message.ToString(), context);
        }
        public static void LogError(object message, params string[] categories)
        {
            LogError(message, null, categories);
        }
        public static void LogError(object message, Object context, params string[] categories)
        {
            Debug.LogError(string.Join("", categories.Select(c => $"[{c}] ")) + message.ToString(), context);
        }
        public static void LogWarning(object message, params string[] categories)
        {
            LogWarning(message, null, categories);
        }
        public static void LogWarning(object message, Object context, params string[] categories)
        {
            Debug.LogWarning(string.Join("", categories.Select(c => $"[{c}] ")) + message.ToString(), context);
        }
    }
}