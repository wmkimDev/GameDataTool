using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using DataTool;
using ExcelDataTool.Core;
using ExcelDataTool.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;

namespace ExcelDataTool.ViewModels;

public class FilePathViewModel : ViewModelBase
{
    private readonly string          _projectSettingsPath;
    private readonly string          _memoryCachePath;
    private readonly ProjectSettings _projectSettings;
    private readonly MemoryCache     _memoryCache;

    public FilePathViewModel()
    {
        _projectSettingsPath = Path.Combine(Util.GetConfigDirectory(), "projects.json");
        _memoryCachePath     = Path.Combine(Util.GetConfigDirectory(), "memoryCache.json");
        _projectSettings     = LoadProjectSettings();
        _memoryCache         = LoadMemoryCache();

        _selectFilesInteraction = new Interaction<string?, string?>();
        _availableProjects      = new ObservableCollection<string>();
        _availableConfigs       = new ObservableCollection<string>();

        SelectTablePathCommand        = ReactiveCommand.CreateFromTask(SelectTablePath);
        SelectScriptOutputPathCommand = ReactiveCommand.CreateFromTask(SelectScriptOutputPath);
        SelectTableOutputPathCommand  = ReactiveCommand.CreateFromTask(SelectTableOutputPath);
        SelectStringOutputPathCommand = ReactiveCommand.CreateFromTask(SelectStringOutputPath);

        var canCreateConfig = this.WhenAnyValue(
            x => x.NewConfigName,
            name => !string.IsNullOrWhiteSpace(name)
        );

        CreateConfigCommand = ReactiveCommand.Create(CreateConfig, canCreateConfig);
        UpdateConfigCommand = ReactiveCommand.Create(UpdateConfig);
        DeleteConfigCommand = ReactiveCommand.CreateFromTask(DeleteConfig);

        // ProjectName의 변경을 관찰하는 Observable 생성
        var canCreate = this.WhenAnyValue(
            x => x.NewProjectName,
            name => !string.IsNullOrWhiteSpace(name)
        );

        CreateProjectCommand = ReactiveCommand.Create(CreateProject, canCreate);
        DeleteProjectCommand = ReactiveCommand.CreateFromTask(DeleteProject);

        ExtractAllCommand    = ReactiveCommand.CreateFromTask(() => ExtractAsync(ExtractionType.All));
        ExtractScriptCommand = ReactiveCommand.CreateFromTask(() => ExtractAsync(ExtractionType.Script));
        ExtractTableCommand  = ReactiveCommand.CreateFromTask(() => ExtractAsync(ExtractionType.Table));
        ExtractStringCommand = ReactiveCommand.CreateFromTask(() => ExtractAsync(ExtractionType.String));

        LoadAvailableProjects();

        // 마지막 프로젝트 로드
        if (!string.IsNullOrEmpty(_memoryCache.LastProject))
        {
            SelectedProject = _memoryCache.LastProject;
        }
    }

    private readonly Interaction<string?, string?> _selectFilesInteraction;
    public           Interaction<string?, string?> SelectFilesInteraction => _selectFilesInteraction;

    #region Project Management

    private ObservableCollection<string> _availableProjects;

    public ObservableCollection<string> AvailableProjects
    {
        get => _availableProjects;
        set => this.RaiseAndSetIfChanged(ref _availableProjects, value);
    }

    private string? _selectedProject;

    public string? SelectedProject
    {
        get => _selectedProject;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedProject, value);
            if (value != null)
            {
                LoadProjectConfigs(value);
                _memoryCache.LastProject = value;
                SaveMemoryCache();
            }
        }
    }

    private string? _newNewProjectName;

    public string? NewProjectName
    {
        get => _newNewProjectName;
        set => this.RaiseAndSetIfChanged(ref _newNewProjectName, value);
    }

    private string? _projectVersion = string.Empty;

    public string? ProjectVersion
    {
        get => _projectVersion;
        set => this.RaiseAndSetIfChanged(ref _projectVersion, value);
    }

    public ICommand CreateProjectCommand { get; }

    private MemoryCache LoadMemoryCache()
    {
        try
        {
            if (File.Exists(_memoryCachePath))
            {
                string jsonString = File.ReadAllText(_memoryCachePath);
                return JsonSerializer.Deserialize<MemoryCache>(jsonString) ?? new MemoryCache();
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"메모리 캐시를 불러오는 중 오류가 발생했습니다: {ex.Message}");
        }

        return new MemoryCache();
    }

    private void SaveMemoryCache()
    {
        try
        {
            string jsonString = JsonSerializer.Serialize(_memoryCache, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_memoryCachePath, jsonString);
        }
        catch (Exception ex)
        {
            Logger.Error($"메모리 캐시를 저장하는 중 오류가 발생했습니다: {ex.Message}");
        }
    }

    private ProjectSettings LoadProjectSettings()
    {
        try
        {
            if (File.Exists(_projectSettingsPath))
            {
                string jsonString = File.ReadAllText(_projectSettingsPath);
                return JsonSerializer.Deserialize<ProjectSettings>(jsonString) ?? new ProjectSettings();
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"프로젝트 설정을 불러오는 중 오류가 발생했습니다: {ex.Message}");
        }

        return new ProjectSettings();
    }

    private void CreateProject()
    {
        try
        {
            // 프로젝트 이름이 비어있으면 실행하지 않음
            if (string.IsNullOrWhiteSpace(NewProjectName))
            {
                Logger.Error("프로젝트 이름을 입력해주세요.");
                return;
            }

            // 이미 존재하는 프로젝트인지 확인
            var existingProject = _projectSettings.Projects.SingleOrDefault(p => p.Name == NewProjectName);

            if (existingProject != null)
            {
                Logger.Error($"프로젝트 '{NewProjectName}'이(가) 이미 존재합니다.");
                return;
            }

            // 새 프로젝트 추가
            _projectSettings.Projects.Add(new ProjectData
            {
                Name    = NewProjectName,
                Version = "1.0.0"
            });

            // 프로젝트 디렉토리 생성
            string projectDir = Path.Combine(Util.GetConfigDirectory(), NewProjectName);
            Directory.CreateDirectory(projectDir);

            // AvailableProjects에도 추가
            if (!AvailableProjects.Contains(NewProjectName))
            {
                AvailableProjects.Add(NewProjectName);
            }

            // 설정 파일 저장
            string jsonString = JsonSerializer.Serialize(_projectSettings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_projectSettingsPath, jsonString);

            // 프로젝트 리스트 리프레시
            LoadAvailableProjects();

            SelectedProject = NewProjectName;
            Logger.Success($"새 프로젝트 '{NewProjectName}'이(가) 생성되었습니다.");
            NewProjectName = string.Empty;
        }
        catch (Exception ex)
        {
            Logger.Error($"프로젝트 생성 중 오류가 발생했습니다: {ex.Message}");
        }
    }

    public ICommand DeleteProjectCommand { get; }

    private async Task DeleteProject()
    {
        if (string.IsNullOrWhiteSpace(SelectedProject))
        {
            Logger.Error("프로젝트가 선택되지 않았습니다.");
            return;
        }

        var box = MessageBoxManager
            .GetMessageBoxStandard("프로젝트 삭제", $"프로젝트 '{SelectedProject}'을(를) 삭제하시겠습니까?",
                ButtonEnum.YesNo);

        var result = await box.ShowAsync();
        if (result != ButtonResult.Yes)
            return;

        try
        {
            var projectToDelete = _projectSettings.Projects.SingleOrDefault(p => p.Name == SelectedProject);
            if (projectToDelete != null)
            {
                // 프로젝트 디렉토리 삭제
                string projectDir = Path.Combine(Util.GetConfigDirectory(), SelectedProject);
                if (Directory.Exists(projectDir))
                {
                    Directory.Delete(projectDir, true);
                }

                // 메모리 캐시에서 프로젝트 관련 정보 제거
                if (_memoryCache.LastProject == SelectedProject)
                {
                    _memoryCache.LastProject = null;
                }

                if (_memoryCache.LastConfigurations.ContainsKey(SelectedProject))
                {
                    _memoryCache.LastConfigurations.Remove(SelectedProject);
                }

                // 프로젝트 목록에서 제거
                _projectSettings.Projects.Remove(projectToDelete);

                // 설정 저장
                string jsonString = JsonSerializer.Serialize(_projectSettings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                await File.WriteAllTextAsync(_projectSettingsPath, jsonString);

                // 모든 필드 초기화
                SaveMemoryCache();

                Logger.Success($"프로젝트 '{SelectedProject}'이(가) 삭제되었습니다.");

                AvailableProjects.Remove(SelectedProject);
                HasAvailableProjects = AvailableProjects.Count > 0;

                AvailableConfigs.Clear();
                HasAvailableConfigs = AvailableConfigs.Count > 0;

                ClearAllFields();
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"프로젝트 삭제 중 오류가 발생했습니다: {ex.Message}");
        }
    }

    private void LoadAvailableProjects()
    {
        AvailableProjects.Clear();
        foreach (var project in _projectSettings.Projects)
        {
            AvailableProjects.Add(project.Name!);
        }

        HasAvailableProjects = AvailableProjects.Count > 0;
    }

    private void LoadProjectConfigs(string projectName)
    {
        var project = _projectSettings.Projects.SingleOrDefault(p => p.Name == projectName);
        if (project == null)
            return;

        // 모든 필드 초기화
        ClearAllFields();

        ProjectVersion = project.Version;

        // Load configurations for the project
        LoadAvailableConfigsForProject(projectName);

        // Load last configuration if exists
        if (_memoryCache.LastConfigurations.TryGetValue(projectName, out string? lastConfig))
        {
            SelectedConfig = lastConfig;
        }
    }

    private void LoadAvailableConfigsForProject(string projectName)
    {
        AvailableConfigs.Clear();
        string projectDir = Path.Combine(Util.GetConfigDirectory(), projectName);

        if (Directory.Exists(projectDir))
        {
            foreach (string file in Directory.GetFiles(projectDir, "*.json"))
            {
                AvailableConfigs.Add(Path.GetFileNameWithoutExtension(file));
            }
        }

        HasAvailableConfigs = AvailableConfigs.Count > 0;
    }

    private bool _hasAvailableProjects;

    public bool HasAvailableProjects
    {
        get => _hasAvailableProjects;
        private set => this.RaiseAndSetIfChanged(ref _hasAvailableProjects, value);
    }

    #endregion

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

    #region StringFileName

    private string? _stringFileName;

    public string? StringFileName
    {
        get => _stringFileName;
        set => this.RaiseAndSetIfChanged(ref _stringFileName, value);
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
            if (value != null && !string.IsNullOrEmpty(SelectedProject))
            {
                LoadConfig(value);
                _memoryCache.LastConfigurations[SelectedProject] = value;
                SaveMemoryCache();
            }
        }
    }

    private string? _newNewConfigName;

    public string? NewConfigName
    {
        get => _newNewConfigName;
        set => this.RaiseAndSetIfChanged(ref _newNewConfigName, value);
    }

    public ICommand CreateConfigCommand { get; }

    private void CreateConfig()
    {
        if (string.IsNullOrWhiteSpace(NewConfigName))
        {
            Logger.Error("새 설정의 이름이 지정되지 않았습니다.");
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedProject))
        {
            Logger.Error("지정된 프로젝트가 없습니다.");
            return;
        }

        if (AvailableConfigs.Contains(NewConfigName))
        {
            Logger.Error($"설정 '{NewConfigName}'이(가) 이미 존재합니다.");
            return;
        }

        try
        {
            var config = new ConfigurationData
            {
                TablePath        = TablePath,
                StringFileName   = StringFileName,
                IsEnumEnabled    = IsEnumEnabled,
                EnumFileName     = EnumFileName,
                ScriptOutputPath = ScriptOutputPath,
                TableOutputPath  = TableOutputPath,
                StringOutputPath = StringOutputPath,
                IsEncrypted      = IsEncrypted,
                EncryptionKey    = EncryptionKey
            };

            string projectDir = Path.Combine(Util.GetConfigDirectory(), SelectedProject);
            Directory.CreateDirectory(projectDir);

            string configPath = Path.Combine(projectDir, $"{NewConfigName}.json");
            string jsonString = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(configPath, jsonString);
            AvailableConfigs.Add(NewConfigName);
            HasAvailableConfigs = true;
            SelectedConfig      = NewConfigName;
            Logger.Success($"새 설정 '{NewConfigName}'이(가) 생성되었습니다.");
            NewConfigName = string.Empty;
        }
        catch (Exception ex)
        {
            Logger.Error($"설정 생성 중 오류가 발생했습니다: {ex.Message}");
        }
    }

    public ICommand UpdateConfigCommand { get; }

    private void UpdateConfig()
    {
        if (string.IsNullOrWhiteSpace(SelectedConfig))
        {
            Logger.Error("업데이트할 설정이 선택되지 않았습니다.");
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedProject))
        {
            Logger.Error("지정된 프로젝트가 없습니다.");
            return;
        }

        try
        {
            var config = new ConfigurationData
            {
                TablePath        = TablePath,
                StringFileName   = StringFileName,
                IsEnumEnabled    = IsEnumEnabled,
                EnumFileName     = EnumFileName,
                ScriptOutputPath = ScriptOutputPath,
                TableOutputPath  = TableOutputPath,
                StringOutputPath = StringOutputPath,
                IsEncrypted      = IsEncrypted,
                EncryptionKey    = EncryptionKey
            };

            string projectDir = Path.Combine(Util.GetConfigDirectory(), SelectedProject);
            string configPath = Path.Combine(projectDir, $"{SelectedConfig}.json");
            string jsonString = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(configPath, jsonString);
            Logger.Success($"설정 '{SelectedConfig}'이(가) 업데이트 되었습니다.");
        }
        catch (Exception ex)
        {
            Logger.Error($"설정 업데이트 중 오류가 발생했습니다: {ex.Message}");
        }
    }

    private void LoadConfig(string configName)
    {
        if (string.IsNullOrWhiteSpace(SelectedProject))
        {
            Logger.Error("프로젝트가 선택되지 않았습니다.");
            return;
        }

        ClearAllFields();

        string configPath = Path.Combine(Util.GetConfigDirectory(), SelectedProject, $"{configName}.json");
        if (!File.Exists(configPath))
            return;

        try
        {
            string jsonString = File.ReadAllText(configPath);
            var    config     = JsonSerializer.Deserialize<ConfigurationData>(jsonString);

            if (config == null)
            {
                Logger.Error($"설정 '{configName}'을 불러오는 중 오류가 발생했습니다.");
                return;
            }

            TablePath        = config.TablePath;
            StringFileName   = config.StringFileName;
            IsEnumEnabled    = config.IsEnumEnabled;
            EnumFileName     = config.EnumFileName;
            ScriptOutputPath = config.ScriptOutputPath;
            TableOutputPath  = config.TableOutputPath;
            StringOutputPath = config.StringOutputPath;
            IsEncrypted      = config.IsEncrypted;
            EncryptionKey    = config.EncryptionKey;
        }
        catch (Exception ex)
        {
            Logger.Error($"설정을 불러오는 중 오류가 발생했습니다: {ex.Message}");
        }
    }

    private void ClearAllFields()
    {
        TablePath        = null;
        StringFileName   = null;
        IsEnumEnabled    = false;
        EnumFileName     = null;
        ScriptOutputPath = null;
        TableOutputPath  = null;
        StringOutputPath = null;
        IsEncrypted      = false;
        EncryptionKey    = null;
    }

    private bool _hasAvailableConfigs;

    public bool HasAvailableConfigs
    {
        get => _hasAvailableConfigs;
        private set => this.RaiseAndSetIfChanged(ref _hasAvailableConfigs, value);
    }

    public ICommand DeleteConfigCommand { get; }

    private async Task DeleteConfig()
    {
        if (string.IsNullOrWhiteSpace(SelectedConfig))
        {
            Logger.Error("설정이 선택되지 않았습니다.");
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedProject))
        {
            Logger.Error("프로젝트가 선택되지 않았습니다.");
            return;
        }

        var box = MessageBoxManager
            .GetMessageBoxStandard("설정 삭제", $"설정 '{SelectedConfig}'을(를) 삭제하시겠습니까?",
                ButtonEnum.YesNo);

        var result = await box.ShowAsync();
        if (result != ButtonResult.Yes)
            return;

        try
        {
            string configPath = Path.Combine(Util.GetConfigDirectory(), SelectedProject, $"{SelectedConfig}.json");

            if (File.Exists(configPath))
            {
                File.Delete(configPath);

                if (_memoryCache.LastConfigurations.TryGetValue(SelectedProject, out string? lastConfig) &&
                    lastConfig == SelectedConfig)
                {
                    _memoryCache.LastConfigurations.Remove(SelectedProject);
                    SaveMemoryCache();
                }

                Logger.Success($"설정 '{SelectedConfig}'이(가) 삭제되었습니다.");

                ClearAllFields();
                AvailableConfigs.Remove(SelectedConfig);
                HasAvailableConfigs = AvailableConfigs.Count > 0;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"설정 삭제 중 오류가 발생했습니다: {ex.Message}");
        }
    }

    #endregion

    #region Operation

    private enum ExtractionType
    {
        All,
        Script,
        Table,
        String
    }

    // Command properties for operation buttons
    public ICommand ExtractAllCommand    { get; }
    public ICommand ExtractScriptCommand { get; }
    public ICommand ExtractTableCommand  { get; }
    public ICommand ExtractStringCommand { get; }

    private async Task ExtractAsync(ExtractionType type)
    {
        // Clear console at the start
        Logger.Clear();
        
        var operationName = type switch
        {
            ExtractionType.All    => "전체",
            ExtractionType.Script => "스크립트",
            ExtractionType.Table  => "테이블",
            ExtractionType.String => "문자열",
            _                     => throw new ArgumentOutOfRangeException(nameof(type))
        };

        try
        {
            Logger.Info($"{operationName} 추출 작업을 시작합니다...");

            // Perform all validations before starting any operation
            if (!ValidateExtraction(type))
            {
                return;
            }

            if (type == ExtractionType.String)
            {
                await TryExtractStringData();
            }
            else
            {
                // Collect context (except for string extraction)
                var collector = new SheetContextCollector();
                collector.CollectAll(TablePath, IsEnumEnabled ? EnumFileName : null);
            
                // Perform extraction based on collected context
                switch (type)
                {
                    case ExtractionType.All:
                        await TryExtractScriptData(collector.SheetContexts);
                        await TryExtractTableData(collector.SheetContexts);
                        await TryExtractStringData();
                        break;
            
                    case ExtractionType.Script:
                        await TryExtractScriptData(collector.SheetContexts);
                        break;
            
                    case ExtractionType.Table:
                        await TryExtractTableData(collector.SheetContexts);
                        break;
                }
            }
        
            Logger.Success($"{operationName} 추출이 완료되었습니다.");
        }
        catch (Exception ex)
        {
            Logger.Error($"{operationName} 추출 중 오류가 발생했습니다 > \n {ex.Message}");
        }
    }

    private async Task TryExtractScriptData(IEnumerable<SheetContext> sheetContexts)
    {
        if (string.IsNullOrEmpty(ScriptOutputPath))
        {
            throw new Exception("스크립트 출력 경로가 지정되지 않았습니다.");
        }

        // await ExtractScriptData(sheetContexts);
    }

    private async Task TryExtractTableData(IEnumerable<SheetContext> sheetContexts)
    {
        if (string.IsNullOrEmpty(TableOutputPath))
        {
            throw new Exception("테이블 출력 경로가 지정되지 않았습니다.");
        }

        // await ExtractTableData(sheetContexts);
    }

    private async Task TryExtractStringData()
    {
        if (string.IsNullOrEmpty(StringFileName))
        {
            throw new Exception("문자열 파일 이름이 지정되지 않았습니다.");
        }

        if (string.IsNullOrEmpty(StringOutputPath))
        {
            throw new Exception("문자열 출력 경로가 지정되지 않았습니다.");
        }

        // TODO : Implement string extraction
    }


    private bool ValidateExtraction(ExtractionType type)
    {
        if (string.IsNullOrWhiteSpace(SelectedProject))
        {
            Logger.Error("프로젝트가 선택되지 않았습니다.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(SelectedConfig))
        {
            Logger.Error("설정이 선택되지 않았습니다.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(TablePath))
        {
            Logger.Error("테이블 경로가 지정되지 않았습니다.");
            return false;
        }

        if (IsEncrypted && string.IsNullOrWhiteSpace(EncryptionKey))
        {
            Logger.Error("암호화가 활성화되었지만 암호화 키가 지정되지 않았습니다.");
            return false;
        }

        if (IsEnumEnabled && string.IsNullOrWhiteSpace(EnumFileName))
        {
            Logger.Error("열거형이 활성화되었지만 열거형 파일 이름이 지정되지 않았습니다.");
            return false;
        }

        switch (type)
        {
            case ExtractionType.All:
                return ValidateAllPaths();

            case ExtractionType.Script:
                if (string.IsNullOrWhiteSpace(ScriptOutputPath))
                {
                    Logger.Error("스크립트 출력 경로가 지정되지 않았습니다.");
                    return false;
                }
                break;
            case ExtractionType.Table:
                if (string.IsNullOrWhiteSpace(TableOutputPath))
                {
                    Logger.Error("테이블 출력 경로가 지정되지 않았습니다.");
                    return false;
                }
                break;
            case ExtractionType.String:
                if (string.IsNullOrWhiteSpace(StringFileName))
                {
                    Logger.Error("문자열 파일 이름이 지정되지 않았습니다.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(StringOutputPath))
                {
                    Logger.Error("문자열 출력 경로가 지정되지 않았습니다.");
                    return false;
                }
                break;
        }

        return true;
    }

    private bool ValidateAllPaths()
    {
        var missingPaths = new List<string>();

        if (string.IsNullOrWhiteSpace(ScriptOutputPath))
            missingPaths.Add("스크립트 출력 경로");

        if (string.IsNullOrWhiteSpace(TableOutputPath))
            missingPaths.Add("테이블 출력 경로");

        if (string.IsNullOrWhiteSpace(StringOutputPath))
            missingPaths.Add("문자열 출력 경로");

        if (string.IsNullOrWhiteSpace(StringFileName))
            missingPaths.Add("문자열 파일 이름");

        if (missingPaths.Count > 0)
        {
            Logger.Error($"다음 경로가 지정되지 않았습니다: {string.Join(", ", missingPaths)}");
            return false;
        }

        return true;
    }
    #endregion
}