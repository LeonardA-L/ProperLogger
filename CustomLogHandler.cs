﻿using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ProperLogger
{
    internal class CustomLogHandler : ILogHandler
    {
        private ILogHandler m_originalHandler;
        private List<ILogObserver> m_observers = new List<ILogObserver>();

        public ILogHandler OriginalHandler => m_originalHandler;

        internal CustomLogHandler(ILogHandler host)
        {
            m_originalHandler = host;
            Debug.Log("Created Handler");
        }

        public void LogException(System.Exception exception, UnityEngine.Object context)
        {
            foreach (var observer in m_observers)
            {
                observer.ContextListener(LogType.Exception, context, "{0}", exception.Message, exception.StackTrace);
            }
            m_originalHandler.LogException(exception, context);
        }

        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            foreach (var observer in m_observers)
            {
                observer.ContextListener(logType, context, format, args);
            }
            m_originalHandler.LogFormat(logType, context, format, args);
        }

        public void AddObserver(ILogObserver observer)
        {
            m_observers.Add(observer);
        }
        public void RemoveObserver(ILogObserver observer)
        {
            m_observers.Remove(observer);
            if(m_observers.Count == 0)
            {
                Debug.unityLogger.logHandler = OriginalHandler;
            }
        }
    }

    internal interface ILogObserver
    {
        void ContextListener(LogType logType, UnityEngine.Object context, string format, params object[] args);
    }
}