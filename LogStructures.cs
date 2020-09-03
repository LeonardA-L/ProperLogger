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
    internal struct ConsoleLogEntry
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
    }
}