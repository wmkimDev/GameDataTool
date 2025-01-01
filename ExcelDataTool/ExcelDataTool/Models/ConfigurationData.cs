using System.Collections.Generic;

namespace ExcelDataTool.Models;

public class ConfigurationData
{
    public string? TablePath { get; set; }
    public string? StringFileName { get; set; }
    public bool IsEnumEnabled { get; set; }
    public string? EnumFileName { get; set; }
    public string? ScriptOutputPath { get; set; }
    public string? TableOutputPath { get; set; }
    public string? StringOutputPath { get; set; }
    public bool IsEncrypted { get; set; }
    public string? EncryptionKey { get; set; }
}

public class ProjectData
{
    public string? Name { get; set; } = string.Empty;
    public string? Version { get; set; } = string.Empty;
}

public class MemoryCache
{
    public string? LastProject { get; set; } = string.Empty;
    public Dictionary<string, string> LastConfigurations { get; set; } = new();
}

public class ProjectSettings
{
    public List<ProjectData> Projects { get; set; } = new();
}