using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using ExcelDataTool.Models;
using ReactiveUI;

namespace ExcelDataTool.ViewModels;

public class FilePathViewModel : ViewModelBase
{
    private readonly Action<string> _logAction;
    
    public FilePathViewModel(Action<string> logAction)
    {
        _logAction = logAction;
        _selectFilesInteraction       = new Interaction<string?, string?>();
        SelectTablePathCommand        = ReactiveCommand.CreateFromTask(SelectTablePath);
        SelectStringPathCommand       = ReactiveCommand.CreateFromTask(SelectStringPath);
        SelectScriptOutputPathCommand = ReactiveCommand.CreateFromTask(SelectScriptOutputPath);
        SelectTableOutputPathCommand  = ReactiveCommand.CreateFromTask(SelectTableOutputPath);
        SelectStringOutputPathCommand = ReactiveCommand.CreateFromTask(SelectStringOutputPath);
        _availableConfigs             = new ObservableCollection<string>();
        SaveConfigCommand             = ReactiveCommand.Create(SaveConfig);
        LoadAvailableConfigs();
        LoadLastConfig();
            
        ExtractAllCommand    = ReactiveCommand.CreateFromTask(ExtractAllAsync);
        ExtractScriptCommand = ReactiveCommand.CreateFromTask(ExtractScriptAsync);
        ExtractTableCommand  = ReactiveCommand.CreateFromTask(ExtractTableAsync);
        ExtractStringCommand = ReactiveCommand.CreateFromTask(ExtractStringAsync);
    }
        
    private readonly Interaction<string?, string?> _selectFilesInteraction;
    public           Interaction<string?, string?> SelectFilesInteraction => _selectFilesInteraction;

    #region TablePath
    private string? _tablePath;
    public string? TablePath
    {
        get { return _tablePath; }
        set { this.RaiseAndSetIfChanged(ref _tablePath, value); }
    }
    public ICommand SelectTablePathCommand { get; }
    private async Task SelectTablePath()
    {
        TablePath = await _selectFilesInteraction.Handle("Select Table Path");
    }
    #endregion
        
    #region StringPath
    private string? _stringPath;
    public string? StringPath
    {
        get => _stringPath;
        set => this.RaiseAndSetIfChanged(ref _stringPath, value);
    }
    public ICommand SelectStringPathCommand { get; }
    private async Task SelectStringPath()
    {
        StringPath = await _selectFilesInteraction.Handle("Select String Path");
    }
    #endregion
        
    #region EnumFileName
    private bool _isEnumEnabled;
    public bool IsEnumEnabled
    {
        get => _isEnumEnabled;
        set => this.RaiseAndSetIfChanged(ref _isEnumEnabled, value);
    }
    private string? _enumFileName;
    public string? EnumFileName
    {
        get => _enumFileName;
        set => this.RaiseAndSetIfChanged(ref _enumFileName, value);
    }
    #endregion
        
    #region ScriptOutputPath
    private string? _scriptOutputPath;
    public string? ScriptOutputPath
    {
        get => _scriptOutputPath;
        set => this.RaiseAndSetIfChanged(ref _scriptOutputPath, value);
    }
    public ICommand SelectScriptOutputPathCommand { get; }
    private async Task SelectScriptOutputPath()
    {
        ScriptOutputPath = await _selectFilesInteraction.Handle("Select Script Output Path");
    }
    #endregion

    #region TableOutputPath
    private string? _tableOutputPath;
    public string? TableOutputPath
    {
        get => _tableOutputPath;
        set => this.RaiseAndSetIfChanged(ref _tableOutputPath, value);
    }
    public ICommand SelectTableOutputPathCommand { get; }
    private async Task SelectTableOutputPath()
    {
        TableOutputPath = await _selectFilesInteraction.Handle("Select Table Output Path");
    }
    #endregion

    #region StringOutputPath
    private string? _stringOutputPath;
    public string? StringOutputPath
    {
        get => _stringOutputPath;
        set => this.RaiseAndSetIfChanged(ref _stringOutputPath, value);
    }
    public ICommand SelectStringOutputPathCommand { get; }
    private async Task SelectStringOutputPath()
    {
        StringOutputPath = await _selectFilesInteraction.Handle("Select String Output Path");
    }
    #endregion
        
    #region Encrption
    private bool _isEncrypted;
    public bool IsEncrypted
    {
        get => _isEncrypted;
        set => this.RaiseAndSetIfChanged(ref _isEncrypted, value);
    }
    private string? _encryptionKey;
    public string? EncryptionKey
    {
        get => _encryptionKey;
        set => this.RaiseAndSetIfChanged(ref _encryptionKey, value);
    }
    #endregion
        
    #region Config
    private string? _configName;
    public string? ConfigName
    {
        get => _configName;
        set => this.RaiseAndSetIfChanged(ref _configName, value);
    }
        
    private ObservableCollection<string> _availableConfigs;
    public ObservableCollection<string> AvailableConfigs
    {
        get => _availableConfigs;
        set => this.RaiseAndSetIfChanged(ref _availableConfigs, value);
    }

    private string? _selectedConfig;
    public string? SelectedConfig
    {
        get => _selectedConfig;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedConfig, value);
            if (value != null)
            {
                ConfigName = value;
                LoadConfig(value);
                SaveCurrentConfig();
            }
        }
    }
        
    public ICommand SaveConfigCommand { get; }
        
    private void LoadLastConfig()
    {
        try
        {
            string lastConfigPath = Path.Combine(GetConfigDirectory(), "lastConfig.json");
            if (File.Exists(lastConfigPath))
            {
                string jsonString     = File.ReadAllText(lastConfigPath);
                var    lastConfigInfo = JsonSerializer.Deserialize<LastConfigInfo>(jsonString);
                if (lastConfigInfo != null)
                {
                    SelectedConfig = lastConfigInfo.ConfigName;
                }
            }
        }
        catch (Exception ex)
        {
            LogToConsole($"Error loading last configuration setting: {ex.Message}");
        }            
    }
        
    private void SaveCurrentConfig()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ConfigName))
            {
                return;
            }

            var lastConfigInfo = new LastConfigInfo
            {
                ConfigName = ConfigName
            };

            // JSON으로 변환합니다
            string jsonString = JsonSerializer.Serialize(lastConfigInfo, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            // 마지막 설정 파일에 저장합니다
            string lastConfigPath = Path.Combine(GetConfigDirectory(), "lastConfig.json");
            File.WriteAllText(lastConfigPath, jsonString);
        }
        catch (Exception ex)
        {
            LogToConsole($"Error saving current configuration setting: {ex.Message}");
        }
    }
        
    private string GetConfigDirectory()
    {
        // Create a directory in the application folder for storing configs
        string configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ExcelDataTool",
            "Configs"
        );
        Directory.CreateDirectory(configDir);
        return configDir;
    }
        
    private void LoadAvailableConfigs()
    {
        AvailableConfigs.Clear();
        string configDir = GetConfigDirectory();
        foreach (string file in Directory.GetFiles(configDir, "*.json"))
        {
            AvailableConfigs.Add(Path.GetFileNameWithoutExtension(file));
        }
    }

    private void SaveConfig()
    {
        if (string.IsNullOrWhiteSpace(ConfigName))
            return;

        var config = new ConfigurationData
        {
            TablePath        = TablePath,
            StringPath       = StringPath,
            IsEnumEnabled    = IsEnumEnabled,
            EnumFileName     = EnumFileName,
            ScriptOutputPath = ScriptOutputPath,
            TableOutputPath  = TableOutputPath,
            StringOutputPath = StringOutputPath,
            IsEncrypted      = IsEncrypted,
            EncryptionKey    = EncryptionKey
        };

        string configPath = Path.Combine(GetConfigDirectory(), $"{ConfigName}.json");
        string jsonString = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true
        });
            
        File.WriteAllText(configPath, jsonString);
    
        // If this is a new config (not in the list), add it to AvailableConfigs
        if (!AvailableConfigs.Contains(ConfigName))
        {
            AvailableConfigs.Add(ConfigName);
        }
    
        LoadAvailableConfigs();
            
        // Update SelectedConfig to match the saved/updated config
        SelectedConfig = ConfigName;
        LogToConsole($"Configuration '{ConfigName}' has been {(AvailableConfigs.Contains(ConfigName) ? "updated" : "saved")}.");
    }
    
    private void LoadConfig(string configName)
    {
        string configPath = Path.Combine(GetConfigDirectory(), $"{configName}.json");
        if (!File.Exists(configPath))
            return;

        string jsonString = File.ReadAllText(configPath);
        var    config     = JsonSerializer.Deserialize<ConfigurationData>(jsonString);

        if (config == null)
            return;

        TablePath        = config.TablePath;
        StringPath       = config.StringPath;
        IsEnumEnabled    = config.IsEnumEnabled;
        EnumFileName     = config.EnumFileName;
        ScriptOutputPath = config.ScriptOutputPath;
        TableOutputPath  = config.TableOutputPath;
        StringOutputPath = config.StringOutputPath;
        IsEncrypted      = config.IsEncrypted;
        EncryptionKey    = config.EncryptionKey;
    }
    #endregion
        
    #region Operation
    // Command properties for operation buttons
    public ICommand ExtractAllCommand    { get; }
    public ICommand ExtractScriptCommand { get; }
    public ICommand ExtractTableCommand  { get; }
    public ICommand ExtractStringCommand { get; }
        
    // Operation command implementations
    private async Task ExtractAllAsync()
    {
        try
        {
            LogToConsole("Starting full extraction...");
            // Implement your extraction logic here
            await Task.Delay(1000); // Placeholder for actual work
            LogToConsole("Full extraction completed successfully.");
        }
        catch (Exception ex)
        {
            LogToConsole($"Error during full extraction: {ex.Message}");
        }
    }

    private async Task ExtractScriptAsync()
    {
        try
        {
            LogToConsole("Starting script extraction...");
            await Task.Delay(1000); // Placeholder
            LogToConsole("Script extraction completed successfully.");
        }
        catch (Exception ex)
        {
            LogToConsole($"Error during script extraction: {ex.Message}");
        }
    }

    private async Task ExtractTableAsync()
    {
        try
        {
            LogToConsole("Starting table extraction...");
            await Task.Delay(1000); // Placeholder
            LogToConsole("Table extraction completed successfully.");
        }
        catch (Exception ex)
        {
            LogToConsole($"Error during table extraction: {ex.Message}");
        }
    }

    private async Task ExtractStringAsync()
    {
        try
        {
            LogToConsole("Starting string extraction...");
            await Task.Delay(1000); // Placeholder
            LogToConsole("String extraction completed successfully.");
        }
        catch (Exception ex)
        {
            LogToConsole($"Error during string extraction: {ex.Message}");
        }
    }

    #endregion
    
    private void LogToConsole(string message)
    {
        string timestampedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
        _logAction?.Invoke(timestampedMessage);
    }
}