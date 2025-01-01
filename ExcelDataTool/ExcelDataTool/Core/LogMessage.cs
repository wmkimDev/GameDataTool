using System;
namespace ExcelDataTool.Core;

public enum LogMessageType
{
    Info,
    Warning,
    Error,
    Success
}

public class LogMessage
{
    private string         Message   { get; }
    public LogMessageType Type      { get; }
    private DateTime       Timestamp { get; }

    public LogMessage(string message, LogMessageType type)
    {
        Message   = message;
        Type      = type;
        Timestamp = DateTime.Now;
    }

    public override string ToString()
    {
        return $"{Message}";
    }
}