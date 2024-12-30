using System.Collections.ObjectModel;
using System.Windows.Input;
using ReactiveUI;

namespace ExcelDataTool.ViewModels;

public class TypeSettingViewModel : ViewModelBase
{
    public TypeSettingViewModel()
    {
        _typeAliasList = new ObservableCollection<TypeAliasItem>();
        SaveChangesCommand = ReactiveCommand.Create(SaveChanges);

        // 기본 타입들 로드
        LoadDefaultTypes();
    }

    private ObservableCollection<TypeAliasItem> _typeAliasList;

    public ObservableCollection<TypeAliasItem> TypeAliasList
    {
        get => _typeAliasList;
        set => this.RaiseAndSetIfChanged(ref _typeAliasList, value);
    }

    // 현재 선택된 항목
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
        TypeAliasList.Add(new TypeAliasItem("string", "string"));
        TypeAliasList.Add(new TypeAliasItem("int", "int"));
        TypeAliasList.Add(new TypeAliasItem("long", "long"));
        TypeAliasList.Add(new TypeAliasItem("float", "float"));
        TypeAliasList.Add(new TypeAliasItem("double", "double"));
        TypeAliasList.Add(new TypeAliasItem("bool", "bool"));
        TypeAliasList.Add(new TypeAliasItem("DateTime", "DateTime"));
    }

    private void SaveChanges()
    {
        // TODO: 설정을 파일이나 데이터베이스에 저장하는 로직 구현
        // 예: JSON 파일로 저장
    }

    // 설정 저장/로드 관련 메서드는 나중에 구현
    private void SaveSettings()
    {
        // TODO: 현재 타입 별칭 설정을 저장
    }

    private void LoadSettings()
    {
        // TODO: 저장된 타입 별칭 설정을 로드
    }
}