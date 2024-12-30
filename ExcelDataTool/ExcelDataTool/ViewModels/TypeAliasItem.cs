using ReactiveUI;

namespace ExcelDataTool.ViewModels;

public class TypeAliasItem : ReactiveObject
{
    private string _originalType;
    public string OriginalType
    {
        get => _originalType;
        set => this.RaiseAndSetIfChanged(ref _originalType, value);
    }

    private string _aliasType;
    public string AliasType
    {
        get => _aliasType;
        set => this.RaiseAndSetIfChanged(ref _aliasType, value);
    }

    public TypeAliasItem(string originalType = "", string aliasType = "")
    {
        _originalType = originalType;
        _aliasType    = aliasType;
    }
}