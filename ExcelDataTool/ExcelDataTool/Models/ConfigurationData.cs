namespace ExcelDataTool.Models;

public class ConfigurationData
{
    public string? TablePath { get; set; }
    public string? StringPath { get; set; }
    public bool IsEnumEnabled { get; set; }
    public string? EnumFileName { get; set; }
    public string? ScriptOutputPath { get; set; }
    public string? TableOutputPath { get; set; }
    public string? StringOutputPath { get; set; }
    public bool IsEncrypted { get; set; }
    public string? EncryptionKey { get; set; }
}

// 마지막 설정 정보를 저장하는 클래스
public class LastConfigInfo
{
    public string ConfigName { get; set; } = string.Empty;
}
