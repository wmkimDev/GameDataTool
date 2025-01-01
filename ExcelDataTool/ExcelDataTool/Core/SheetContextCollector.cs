using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DataTool;
using ExcelDataTool.Models;
using ExcelDataTool.ViewModels;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace ExcelDataTool.Core;

public class SheetContextCollector
{
    private readonly List<SheetContext> _sheetContexts = new();
    private static readonly string[] SupportedExtensions = { ".xlsx", ".xls" };
    private readonly TableAddress _address;
    private readonly IReadOnlyDictionary<string, string> _typeMap;
    private readonly string _configPath;
    private SheetContext _currentSheetContext = null!;
    
    public IReadOnlyList<SheetContext> SheetContexts => _sheetContexts;

    public SheetContextCollector()
    {
        _address    = new TableAddress();
        _configPath = Path.Combine(Util.GetConfigDirectory(), "type_alias.json");
        _typeMap    = LoadTypeMap();
    }
    
    private Dictionary<string, string> LoadTypeMap()
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                return Util.GetDefaultTypeMap();
            }

            var jsonString    = File.ReadAllText(_configPath);
            var typeAliasList = System.Text.Json.JsonSerializer.Deserialize<List<TypeAliasItem>>(jsonString);

            return typeAliasList?.ToDictionary(
                t => t.AliasType,
                t => t.OriginalType,
                StringComparer.OrdinalIgnoreCase
            ) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            Logger.Error($"타입 별칭 설정을 불러오는 중 오류가 발생했습니다: {ex.Message}");
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
    
    public void CollectAll(string tablePath, string? enumFileName = null)
    {
        _address.Reset();
        var excelFiles = GetExcelFileList(tablePath);
        ProcessExcelFiles(excelFiles, enumFileName);
    }

    private List<string> GetExcelFileList(string tablePath)
    {
        var excelFiles = Directory.GetFiles(tablePath, "*.*", SearchOption.AllDirectories)
            .Where(IsExcelFile)
            .OrderBy(file => file)
            .ToList();

        if (excelFiles.Count == 0)
        {
            var message = $"지정된 디렉토리에서 엑셀 파일을 찾을 수 없습니다: {tablePath}";
            throw new FileNotFoundException(message);
        }

        Logger.Info("\n[엑셀 파일 목록]");
        foreach (var file in excelFiles)
        {
            Logger.Info($"  * {Path.GetFileName(file)}");
        }
        Logger.Info($"총 {excelFiles.Count}개의 파일을 찾았습니다.");

        return excelFiles;
    }

    private static bool IsExcelFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }

    private void ProcessExcelFiles(IEnumerable<string> files, string? enumFileName)
    {
        _sheetContexts.Clear();
        foreach (var file in files)
        {
            if (IsEnumDataFile(Path.GetFileNameWithoutExtension(file), enumFileName))
                continue;

            _address.TableName = Path.GetFileNameWithoutExtension(file);
            var contexts = ProcessExcelFile(file);
            _sheetContexts.AddRange(contexts);
        }
    }

    private bool IsEnumDataFile(string sheetName, string? enumFileName)
    {
        return sheetName == enumFileName;
    }

    private List<SheetContext> ProcessExcelFile(string filePath)
    {
        var sheetNames = new HashSet<string>();
        
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var workbook = new XSSFWorkbook(fs);
        var contexts = new List<SheetContext>();

        for (var i = 0; i < workbook.NumberOfSheets; i++)
        {
            var sheet = workbook.GetSheetAt(i);
            _address.SheetName = sheet.SheetName;
            
            var sheetContext = GetSheetContext(sheet);
            if (!sheetNames.Add(sheetContext.SheetName!))
                _address.ThrowException("중복된 시트 이름이 발견되었습니다");
            
            contexts.Add(sheetContext);
        }

        return contexts;
    }

    private SheetContext GetSheetContext(ISheet sheet)
    {
        _currentSheetContext = new SheetContext();
        var sheetName = GetSheetName(sheet);
        var (rowCount, columnCount) = GetTableDimensions(sheet);
        Logger.Info("----------------------------------------");
        Logger.Info($"{_address.TableName}/{sheetName} 테이블 크기: {rowCount}행 x {columnCount}열");
        _currentSheetContext.PropertyNames = ExtractPropertyNames(sheet, columnCount);
        _currentSheetContext.PropertyTypes = ExtractPropertyTypes(sheet, columnCount);
        _currentSheetContext.PropertyDescriptions = ExtractPropertyDescriptions(sheet, columnCount);

        PrintPropertyInfo(_currentSheetContext.PropertyNames, _currentSheetContext.PropertyTypes);
        
        var primaryKeyIndex = FindPrimaryKeyIndex(sheet, columnCount);
        var foreignKeys = FindForeignKeys(_currentSheetContext.PropertyNames);
        PrintKeyInfo(primaryKeyIndex, foreignKeys, _currentSheetContext.PropertyNames, _currentSheetContext.PropertyTypes);

        _currentSheetContext.TableName = _address.TableName!;
        _currentSheetContext.SheetName = sheetName;
        _currentSheetContext.Sheet = sheet;
        _currentSheetContext.RowCount = rowCount;
        _currentSheetContext.ColumnCount = columnCount;
        _currentSheetContext.PKeyColumnIndex = primaryKeyIndex;
        _currentSheetContext.ForeignKeys = foreignKeys;
        
        return _currentSheetContext;
    }

    private string GetSheetName(ISheet sheet)
    {
        var nameCell = sheet.GetRow(0)?.GetCell(1);
        if (nameCell?.CellType != CellType.String)
            _address.ThrowException("시트 이름 셀이 비어있거나 문자열이 아닙니다");
        return nameCell.GetValue();
    }

    private (int rowCount, int columnCount) GetTableDimensions(ISheet sheet)
    {
        var firstRow = sheet.GetRow(0);
        if (firstRow == null)
            _address.ThrowException("첫 번째 행을 찾을 수 없습니다.");

        var (firstMarker, lastMarker) = (-1, -1);
        for (int j = 0; j < firstRow!.LastCellNum; j++)
        {
            if (firstRow.GetCell(j)?.GetValue().Contains("@") != true) continue;
            if (firstMarker == -1) firstMarker = j;
            lastMarker = j;
        }

        if (firstMarker == -1)
            _address.ThrowException("테이블 범위 마커(@)를 찾을 수 없습니다.");

        var rowCount = 4;
        while (sheet.GetRow(rowCount)?.GetCell(firstMarker)?.GetValue().Contains("@") == true)
            rowCount++;

        return (rowCount - 4 - 1, lastMarker - firstMarker - 1);
    }

    private List<string> ExtractPropertyNames(ISheet sheet, int columnCount)
    {
        var propertyRow = sheet.GetRow(2);
        var propertyNames = new List<string>();

        for (int i = 1; i <= columnCount; i++)
        {
            var cell = propertyRow.GetCell(i);
            var propertyName = cell.GetValue();
            if (propertyName.Contains(" "))
                _address.ThrowException($"속성 이름에 공백이 포함되어 있습니다: {propertyName}");
            
            if (string.IsNullOrWhiteSpace(propertyName))
                _address.ThrowException("속성 이름이 비어있습니다.");
            
            if (_address.SheetName == propertyName)
                _address.ThrowException($"속성 이름이 시트 이름과 동일합니다: {propertyName}");

            propertyNames.Add(propertyName);
        }

        return propertyNames;
    }

    private List<string> ExtractPropertyTypes(ISheet sheet, int columnCount)
    {
        var typeRow = sheet.GetRow(3);
        var propertyTypes = new List<string>();

        for (int i = 1; i <= columnCount; i++)
        {
            _address.ColumnName = _currentSheetContext.PropertyNames![i - 1];
            var cell = typeRow.GetCell(i);
            if (cell?.CellType != CellType.String)
                _address.ThrowException("속성 타입은 문자열이어야 합니다.");

            var typeString = cell.GetValue();
            if (_typeMap.TryGetValue(typeString, out var mappedType))
            {
                propertyTypes.Add(mappedType);
            }
            else
            {
                _address.ThrowException($"지원되지 않는 타입입니다: {typeString}");
            }
        }

        return propertyTypes;
    }

    private List<string> ExtractPropertyDescriptions(ISheet sheet, int columnCount)
    {
        var descriptionRow = sheet.GetRow(1);
        var descriptions = new List<string>();

        for (int i = 1; i <= columnCount; i++)
        {
            _address.ColumnName = _currentSheetContext.PropertyNames![i - 1];
            var cell = descriptionRow.GetCell(i);
            descriptions.Add(cell.GetValue());
        }

        return descriptions;
    }

    private int FindPrimaryKeyIndex(ISheet sheet, int columnCount)
    {
        var headerRow = sheet.GetRow(2);
        for (int i = 1; i <= columnCount; i++)
        {
            var cell = headerRow.GetCell(i);
            if (cell?.GetValue().ToLower() == "pkey")
                return i - 1;
        }

        return -1;
    }

    private List<ForeignKeyInfo> FindForeignKeys(List<string> propertyNames)
    {
        var foreignKeys = new List<ForeignKeyInfo>();
        var propertyNameSet = new HashSet<string>();
        var regex = new Regex(@"^(\w+)\[(\w+)\]$");

        for (int i = 0; i < propertyNames.Count; i++)
        {
            var match = regex.Match(propertyNames[i]);
            if (match.Success)
            {
                var propertyName = match.Groups[1].Value;
                var referencedTable = match.Groups[2].Value;

                if (string.IsNullOrWhiteSpace(propertyName) || string.IsNullOrWhiteSpace(referencedTable))
                {
                    _address.ThrowException($"외래 키 형식이 잘못되었습니다. 값: {propertyNames[i]}");
                }

                foreignKeys.Add(new ForeignKeyInfo(i, referencedTable));
                propertyNames[i] = propertyName;
            }

            if (!propertyNameSet.Add(propertyNames[i]))
            {
                _address.ThrowException($"중복된 속성 이름이 발견되었습니다: {propertyNames[i]}");
            }
        }

        return foreignKeys;
    }

    private void PrintPropertyInfo(List<string> propertyNames, List<string> propertyTypes)
    {
        Logger.Info("\n[속성 정보]");
        for (int i = 0; i < propertyNames.Count; i++)
        {
            var propertyName = propertyNames[i].Split('[')[0];
            Logger.Info($"  * {propertyName} : {propertyTypes[i]}");
        }
    }

    private void PrintKeyInfo(int primaryKeyIndex, List<ForeignKeyInfo> foreignKeys, List<string> propertyNames, List<string> propertyTypes)
    {
        Logger.Info("\n[테이블 키 정보]");

        if (primaryKeyIndex != -1)
        {
            Logger.Info($"  Primary Key: {propertyNames[primaryKeyIndex]} ({propertyTypes[primaryKeyIndex]})");
        }
        else
        {
            Logger.Warning("  Primary Key가 설정되지 않았습니다. 기본 테이블 순서(행 인덱스)를 키로 사용합니다.");
        }

        Logger.Info("\n[외래 키 정보]");
        if (foreignKeys.Count != 0)
        {
            foreach (var fk in foreignKeys)
            {
                Logger.Info($"  Foreign Key: {propertyNames[fk.ColumnIndex]} (참조 테이블: {fk.ReferencedTableName})");
            }
        }
        else
        {
            Logger.Info("  외래 키가 없습니다.");
        }
    }
}