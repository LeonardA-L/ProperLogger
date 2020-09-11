using System.Collections.Generic;
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
        public string messageFirstLine;
        public LogLevel level;
        public string stackTrace;
        public int count;
        public UnityEngine.Object context;
        public string firstLine;
        public string firstAsset;
        public List<LogCategory> categories;
        public string originalMessage;
        public string originalStackTrace;
        public int unityMode;
    }

    public class CustomLogEntry
    {
        public string message;
        public int errorNum;
        public string file;
        public int line;
        public int column;
        public int mode;
        public int instanceID;
        public int identifier;
    }

    public enum UnityLogMode
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