using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using ExcelDataTool.Core;
using ReactiveUI;

namespace ExcelDataTool.ViewModels;

public class TypeSettingViewModel : ViewModelBase
{
    private readonly string _configPath;

    public TypeSettingViewModel()
    {
        _typeAliasList     = new ObservableCollection<TypeAliasItem>();
        SaveChangesCommand = ReactiveCommand.CreateFromTask(SaveChanges);

        // Initialize config path
        _configPath = Path.Combine(Util.GetConfigDirectory(), "type_alias.json");

        // 설정 로드 시도
        LoadSettings();

        // 기본 타입들 로드
        if (TypeAliasList.Count == 0)
        {
            LoadDefaultTypes();
        }
    }

    private ObservableCollection<TypeAliasItem> _typeAliasList;

    public ObservableCollection<TypeAliasItem> TypeAliasList
    {
        get => _typeAliasList;
        set => this.RaiseAndSetIfChanged(ref _typeAliasList, value);
    }

    private TypeAliasItem? _selectedItem;

    public TypeAliasItem? SelectedItem
    {
        get => _selectedItem;
        set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
    }

    public ICommand SaveChangesCommand { get; }

    private void LoadDefaultTypes()
    {
        // 기본 C# 타입들을 추가
        TypeAliasList = Util.GetDefaultTypeAliases();
    }

    private async Task SaveChanges()
    {
        try
        {
            if (!ValidateTypeAliases(out var errorMessage))
            {
                Logger.Error(errorMessage);
                return;
            }

            foreach (var item in TypeAliasList)
            {
                item.OriginalType = item.OriginalType.Trim();
                item.AliasType    = item.AliasType.Trim();
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
        
            var jsonString = JsonSerializer.Serialize(TypeAliasList, options);
            await File.WriteAllTextAsync(_configPath, jsonString);
            
            Logger.Success("타입 별칭 설정이 저장되었습니다.");
        }
        catch (Exception e)
        {
            Logger.Error($"타입 별칭 설정 저장 중 오류가 발생했습니다: {e.Message}");
        }
    }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(_configPath))
                return;

            var jsonString    = File.ReadAllText(_configPath);
            var typeAliasList = JsonSerializer.Deserialize<ObservableCollection<TypeAliasItem>>(jsonString);
            if (typeAliasList == null)
            {
                Logger.Error("타입 별칭 설정 파일이 있으나 불러오는 데 실패했습니다.");
                return;
            }

            TypeAliasList = typeAliasList;
        }
        catch (Exception e)
        {
            Logger.Error($"타입 별칭 설정 로드 중 오류가 발생했습니다: {e.Message}");
        }
    }

    private bool ValidateTypeAliases(out string errorMessage)
    {
        errorMessage = string.Empty;

        if (TypeAliasList.Count == 0)
        {
            errorMessage = "타입 별칭 목록이 비어있습니다.";
            return false;
        }

        var emptyAliasTypes = TypeAliasList
            .Where(item => string.IsNullOrWhiteSpace(item.AliasType))
            .ToList();

        if (emptyAliasTypes.Count != 0)
        {
            var emptyTypes = string.Join(", ", emptyAliasTypes.Select(x => x.OriginalType));
            errorMessage = $"다음 타입의 별칭이 비어있습니다: {emptyTypes}";
            return false;
        }

        var duplicateAliasTypes = TypeAliasList
            .GroupBy(item => item.AliasType.Trim())
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateAliasTypes.Count != 0)
        {
            errorMessage = $"중복된 별칭 타입이 존재합니다: {string.Join(", ", duplicateAliasTypes)}";
            return false;
        }

        return true;
    }
}