using System;
using System.Collections.ObjectModel;

namespace ExcelDataTool.Core;

public static class Logger
{
    public static ObservableCollection<LogMessage> Messages { get; } = new();
    public static event Action? OnMessageLogged;
    
    public static void Info(string message)
    {
        Messages.Add(new LogMessage(message, LogMessageType.Info));
        OnMessageLogged?.Invoke();
    }

    public static void Warning(string message)
    {
        Messages.Add(new LogMessage(message, LogMessageType.Warning));
        OnMessageLogged?.Invoke();
    }

    public static void Error(string message)
    {
        Messages.Add(new LogMessage(message, LogMessageType.Error));
        OnMessageLogged?.Invoke();
    }

    public static void Success(string message)
    {
        Messages.Add(new LogMessage(message, LogMessageType.Success));
        OnMessageLogged?.Invoke();
    }

    public static void Clear()
    {
        Messages.Clear();
        OnMessageLogged?.Invoke();
    }
}