using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProperLogger
{

    [System.Serializable]
    internal struct PendingContext
    {
        public string message;
        public LogType logType;
        public UnityEngine.Object context;
    }

    [System.Serializable]
    internal class ConsoleLogEntry
    {
        public long date;
        public string timestamp;
        public string message;
        public string[] messageLines;
        public string cachedFirstLine;
        public string cachedSecondLine;
        public LogLevel level;
        public string stackTrace;
        public string[] traceLines;
        public int count;
        public UnityEngine.Object context;
        public string assetLine;
        public string assetPath;
        public List<LogCategory> categories;
        public List<string> categoriesStringsCache = null;
        public List<string> categoriesStrings => categoriesStringsCache ?? (categoriesStringsCache = categories.Select(c=>c.Name).ToList());
        public string originalMessage;
        public string originalStackTrace;
        public int unityMode;
        public int unityIndex = -1;
        public float timetime;
        public int frame;

        internal string GetExportString()
        {
            return originalMessage + Environment.NewLine + originalStackTrace;
        }
    }

    internal class CustomLogEntry
    {
        public string message;
        public string file;
        public int line;
        public int column;
        public int mode;
        public int instanceID;
        public int identifier;
    }

    internal enum UnityLogMode
    {
        Error = 1,
        Assert = 2,
        Log = 4,
        Fatal = 16, // 0x00000010
        DontPreprocessCondition = 32, // 0x00000020
        AssetImportError = 64, // 0x00000040
        AssetImportWarning = 128, // 0x00000080
        ScriptingError = 256, // 0x00000100
        ScriptingWarning = 512, // 0x00000200
        ScriptingLog = 1024, // 0x00000400
        ScriptCompileError = 2048, // 0x00000800
        ScriptCompileWarning = 4096, // 0x00001000
        StickyError = 8192, // 0x00002000
        MayIgnoreLineNumber = 16384, // 0x00004000
        ReportBug = 32768, // 0x00008000
        DisplayPreviousErrorInStatusBar = 65536, // 0x00010000
        ScriptingException = 131072, // 0x00020000
        DontExtractStacktrace = 262144, // 0x00040000
        ShouldClearOnPlay = 524288, // 0x00080000
        GraphCompileError = 1048576, // 0x00100000
        ScriptingAssertion = 2097152, // 0x00200000
    }
}