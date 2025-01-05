using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DataTool;
using ExcelDataTool.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using NPOI.SS.UserModel;

namespace ExcelDataTool.Core;

public class GameDataExtractor
{
    // Key: 시트 이름, Value: 시트의 PKey
    private readonly Dictionary<string, HashSet<string>> _tablePKeys;
    private readonly IReadOnlyList<SheetContext>         _sheetContexts;

    private readonly TableAddress _address;

    // Key: 시트 이름, Value: 시트의 데이터 (PKey, RowData)
    private readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> _tableData;
    public Dictionary<string, Dictionary<string, Dictionary<string, string>>> TableData => _tableData;

    public GameDataExtractor(IReadOnlyList<SheetContext> sheetContexts)
    {
        _sheetContexts = sheetContexts;
        _tablePKeys    = new();
        _address       = new();
        _tableData     = new();
    }

    public void ValidateGameDataSheetContext()
    {
        // 1단계: 모든 테이블의 PKey를 한 번만 수집
        CollectTablePKeys();

        // 2단계: 각 시트별로 Key 유효성 검사
        foreach (var sheetContext in _sheetContexts)
        {
            _address.TableName = sheetContext.TableName;
            _address.SheetName = sheetContext.SheetName;

            ValidateKeys(sheetContext);
        }
    }

    private void CollectTablePKeys()
    {
        foreach (var sheetContext in _sheetContexts)
        {
            if (!_tablePKeys.ContainsKey(sheetContext.SheetName!))
            {
                _tablePKeys[sheetContext.SheetName!] = new();
            }

            if (sheetContext.PKeyColumnIndex != -1)
            {
                for (var row = 4; row < sheetContext.RowCount + 4; row++)
                {
                    var pKeyCell = sheetContext.Sheet!.GetRow(row).GetCell(sheetContext.PKeyColumnIndex + 1);
                    _address.CellAddress = pKeyCell!.Address.ToString();
                    _address.ColumnName  = sheetContext.PropertyNames![sheetContext.PKeyColumnIndex];
                    var pKeyValue = ValidateExtractCellValue(pKeyCell,
                        sheetContext.PropertyTypes![sheetContext.PKeyColumnIndex]);
                    if (string.IsNullOrEmpty(pKeyValue))
                        continue;

                    if (!_tablePKeys[sheetContext.SheetName!].Add(pKeyValue))
                    {
                        _address.ThrowException($"중복된 Primary Key 값 '{pKeyValue}'이(가) 발견되었습니다.");
                    }
                }
            }
            else
            {
                for (var row = 0; row < sheetContext.RowCount; row++)
                {
                    _tablePKeys[sheetContext.SheetName!].Add(row.ToString());
                }
            }
        }
    }


    private void ValidateKeys(SheetContext context)
    {
        // 외래키가 없다면 검증 생략
        if (context.ForeignKeys == null || context.ForeignKeys.Count == 0)
            return;

        // 모든 외래키에 대해 검증
        foreach (var foreignKey in context.ForeignKeys)
        {
            // 참조하는 테이믈이 존재하는지 확인
            if (!_tablePKeys.ContainsKey(foreignKey.ReferencedTableName))
            {
                _address.ThrowException($"참조된 테이블 '{foreignKey.ReferencedTableName}'을(를) 찾을 수 없습니다.");
            }

            // 2. 모든 행의 외래키 값이 참조 테이블의 PKey에 존재하는지 확인
            var referencedPKeys = _tablePKeys[foreignKey.ReferencedTableName];
            for (var row = 4; row < context.RowCount + 4; row++)
            {
                var foreignKeyCell = context.Sheet!.GetRow(row).GetCell(foreignKey.ColumnIndex + 1);
                _address.CellAddress = foreignKeyCell!.Address.ToString();
                _address.ColumnName  = context.PropertyNames![foreignKey.ColumnIndex];

                var foreignKeyValue =
                    ValidateExtractCellValue(foreignKeyCell, context.PropertyTypes![foreignKey.ColumnIndex]);

                if (!referencedPKeys.Contains(foreignKeyValue))
                {
                    _address.ThrowException(
                        $"외래키 값 '{foreignKeyValue}'이(가) 참조 테이블 '{foreignKey.ReferencedTableName}'의 PKey에 존재하지 않습니다.");
                }
            }
        }
    }

    public void ExtractTableData()
    {
        foreach (var sheetContext in _sheetContexts)
        {
            _address.TableName = sheetContext.TableName;
            _address.SheetName = sheetContext.SheetName;

            var tableData = new Dictionary<string, Dictionary<string, string>>();

            for (var row = 4; row < sheetContext.RowCount + 4; row++)
            {
                var rowData = new Dictionary<string, string>();
                var currRow = sheetContext.Sheet!.GetRow(row);

                string rowKey;
                if (sheetContext.PKeyColumnIndex != -1)
                {
                    var pKeyCell = currRow.GetCell(sheetContext.PKeyColumnIndex + 1);
                    _address.CellAddress = pKeyCell!.Address.ToString();
                    _address.ColumnName = sheetContext.PropertyNames![sheetContext.PKeyColumnIndex];
                    rowKey = ValidateExtractCellValue(pKeyCell, sheetContext.PropertyTypes![sheetContext.PKeyColumnIndex]);
                }
                else
                {
                    // 만약 PKey가 없다면 행 번호를 PKey로 사용
                    rowKey = (row - 4).ToString();
                }

                for (var col = 0; col < sheetContext.ColumnCount; col++)
                {
                    if (col == sheetContext.PKeyColumnIndex)
                        continue;

                    var cell = currRow.GetCell(col + 1);
                    _address.CellAddress = cell!.Address.ToString();
                    _address.ColumnName  = sheetContext.PropertyNames![col];

                    var cellValue = ValidateExtractCellValue(cell, sheetContext.PropertyTypes![col]);
                    rowData.Add(sheetContext.PropertyNames![col], cellValue);
                }

                tableData[rowKey] = rowData;
            }

            _tableData[sheetContext.SheetName!] = tableData;
        }
    }

    public async Task SaveToJson(string outputPath, bool isEncrypted, string encryptionKey)
    {
        if (!Directory.Exists(outputPath))
            throw new DirectoryNotFoundException($"'{outputPath}' 디렉토리를 찾을 수 없습니다.");
        
        // 현재 시트 이름들의 집합 생성
        var currentSheets = new HashSet<string>(_tableData.Keys);
        
        // 기존 json 파일들 중 현재 시트에 없는 것들 삭제
        foreach (var file in Directory.GetFiles(outputPath, "*.json"))
        {
            var sheetName = Path.GetFileNameWithoutExtension(file);
            if (!currentSheets.Contains(sheetName))
            {
                var box = MessageBoxManager
                    .GetMessageBoxStandard(
                        "파일 삭제",
                        $"더 이상 사용되지 않는 파일 '{sheetName}.json'을(를) 삭제하시겠습니까?",
                        ButtonEnum.YesNo);

                var result = await box.ShowAsync();
                if (result == ButtonResult.Yes)
                {
                    File.Delete(file);
                }
            }
        }
        
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder       = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        
        foreach (var (sheetName, tableData) in _tableData)
        {
            var jsonFileName = Path.Combine(outputPath, $"{sheetName}.json");
            var jsonString   = JsonSerializer.Serialize(tableData, jsonOptions);
            
            // 암호화가 활성화된 경우 데이터 암호화
            if (isEncrypted)
            {
                try
                {
                    jsonString = DataEncryption.EncryptData(jsonString, encryptionKey);
                }
                catch (Exception ex)
                {
                    throw new Exception($"'{sheetName}' 데이터 암호화 중 오류 발생: {ex.Message}");
                }
            }
            
            await File.WriteAllTextAsync(jsonFileName, jsonString, Encoding.UTF8);
        }
    }

    private string ValidateExtractCellValue(ICell cell, string propertyType)
    {
        var cellValue = cell.GetValue();
        if (string.IsNullOrEmpty(cellValue))
            _address.ThrowException("셀 값이 비어있습니다.");

        switch (propertyType.Trim().ToLower())
        {
            case "int":
                if (!int.TryParse(cellValue, out _))
                    _address.ThrowException($"'{cellValue}'은(는) int 형식으로 변환할 수 없습니다.");
                break;
            case "long":
                if (!long.TryParse(cellValue, out _))
                    _address.ThrowException($"'{cellValue}'은(는) long 형식으로 변환할 수 없습니다.");
                break;
            case "string":
                // string 타입은 유효성 검사가 필요 없음
                break;
            case "float":
                if (!float.TryParse(cellValue, out _))
                    _address.ThrowException($"'{cellValue}'은(는) float 형식으로 변환할 수 없습니다.");
                break;
            case "double":
                if (!double.TryParse(cellValue, out _))
                    _address.ThrowException($"'{cellValue}'은(는) double 형식으로 변환할 수 없습니다.");
                break;
            case "bool":
                if (!bool.TryParse(cellValue, out _))
                    _address.ThrowException($"'{cellValue}'은(는) bool 형식으로 변환할 수 없습니다.");
                break;
            case "DateTime":
                if (!DateTime.TryParse(cellValue, out _))
                    _address.ThrowException($"'{cellValue}'은(는) DateTime 형식으로 변환할 수 없습니다.");
                break;
            case "TimeSpan":
                if (!TimeSpan.TryParse(cellValue, out _))
                    _address.ThrowException($"'{cellValue}'은(는) TimeSpan 형식으로 변환할 수 없습니다.");
                break;
            default:
                throw new Exception($"지원하지 않는 데이터 타입 '{propertyType}'입니다.");
        }

        return cellValue;
    }
}