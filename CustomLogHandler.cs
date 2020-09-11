using System;
using System.Reflection;
using UnityEngine;

namespace ProperLogger
{
    internal class CustomLogHandler : ILogHandler
    {
        private ILogHandler m_originalHandler;
        private ProperConsoleWindow m_console;

        public ILogHandler OriginalHandler => m_originalHandler;

        internal CustomLogHandler(ILogHandler host, ProperConsoleWindow console)
        {
            m_originalHandler = host;
            m_console = console;
        }

        public void LogException(System.Exception exception, UnityEngine.Object context)
        {
            m_console.ContextListener(LogType.Exception, context, "{0}", exception.Message, exception.StackTrace);
            m_originalHandler.LogException(exception, context);
        }

        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            m_console.ContextListener(logType, context, format, args);
            m_originalHandler.LogFormat(logType, context, format, args);
        }
    }
}