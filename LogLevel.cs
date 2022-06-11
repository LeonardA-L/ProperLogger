namespace ProperLogger
{
    [System.Serializable]
    [System.Flags]
    internal enum LogLevel
    {
        Log = 1,
        Warning = 2,
        Error = 4,
        Exception = 8 | Error,
        Assert = 16 | Error,

        All = Log | Warning | Error | Exception | Assert
    }
}