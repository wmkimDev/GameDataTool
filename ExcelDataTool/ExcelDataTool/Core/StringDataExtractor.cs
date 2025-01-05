using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DataTool;
using ExcelDataTool.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace ExcelDataTool.Core;

public class StringDataExtractor
{
    private readonly IReadOnlyList<SheetContext> _sheetContexts;

    private readonly TableAddress _address;

    // Key: Language, Value: Dictionary of (fullKey, text)
    private readonly Dictionary<string, Dictionary<string, string>> _languageData;
    private readonly HashSet<string>                                _languages;

    public Dictionary<string, Dictionary<string, string>> LanguageData => _languageData;
    public IReadOnlySet<string>                           Languages    => _languages;

    public StringDataExtractor(IReadOnlyList<SheetContext> sheetContexts)
    {
        _sheetContexts = sheetContexts;
        _address       = new TableAddress();
        _languageData  = new Dictionary<string, Dictionary<string, string>>();
        _languages     = new HashSet<string>();
    }

    public void ValidateAndExtractData()
    {
        foreach (var sheetContext in _sheetContexts)
        {
            _address.TableName = sheetContext.TableName;
            _address.SheetName = sheetContext.SheetName;

            // Validate sheet has a primary key
            if (sheetContext.PKeyColumnIndex == -1)
            {
                _address.ThrowException("문자열 테이블에는 Primary Key가 필요합니다.");
            }

            ExtractLanguageColumns(sheetContext);
            ExtractStringData(sheetContext);
        }
    }

    private void ExtractLanguageColumns(SheetContext context)
    {
        // Skip the primary key column and collect language codes
        for (var i = 0; i < context.ColumnCount; i++)
        {
            if (i == context.PKeyColumnIndex)
                continue;

            var columnName   = context.PropertyNames![i];
            var propertyType = context.PropertyTypes![i];

            // Validate column type is string
            if (propertyType.ToLower() != "string")
            {
                _address.ColumnName = columnName;
                _address.ThrowException($"언어 열의 타입은 string이어야 합니다. 현재 타입: {propertyType}");
            }

            _languages.Add(columnName);
            if (!_languageData.ContainsKey(columnName))
            {
                _languageData[columnName] = new Dictionary<string, string>();
            }
        }
    }

    private void ExtractStringData(SheetContext context)
    {
        var existingKeys = new HashSet<string>();

        for (var row = 4; row < context.RowCount + 4; row++)
        {
            var currentRow = context.Sheet!.GetRow(row);

            // Get primary key
            var pKeyCell = currentRow.GetCell(context.PKeyColumnIndex + 1);
            _address.CellAddress = pKeyCell!.Address.ToString();
            _address.ColumnName  = context.PropertyNames![context.PKeyColumnIndex];

            var pKey = pKeyCell.GetValue();
            if (string.IsNullOrEmpty(pKey))
            {
                continue;
            }

            // Check for duplicate keys across all sheets
            if (!existingKeys.Add(pKey))
            {
                _address.ThrowException($"중복된 key 값이 발견되었습니다: {pKey}");
            }

            // Extract language values
            for (var col = 0; col < context.ColumnCount; col++)
            {
                if (col == context.PKeyColumnIndex) continue;

                var cell = currentRow.GetCell(col + 1);
                _address.CellAddress = cell!.Address.ToString();
                _address.ColumnName  = context.PropertyNames![col];

                var value = cell.GetValue();
                if (string.IsNullOrEmpty(value))
                {
                    _address.ThrowException("문자열 값이 비어있습니다.");
                }

                var language = context.PropertyNames![col];
                _languageData[language][pKey] = value;
            }
        }
    }

    public async Task SaveToJson(string outputPath, bool isEncrypted, string encryptionKey)
    {
        // 출력 디렉토리가 없으면 생성
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        // 현재 언어 파일들의 집합 생성
        // GSTR_{언어코드}.json 형식의 파일명 생성
        var currentFiles = _languageData.Keys.Select(lang => $"GSTR_{lang}.json").ToHashSet();

        // 더 이상 사용되지 않는 파일 검사 및 삭제 처리
        foreach (var file in Directory.GetFiles(outputPath, "*.json"))
        {
            var fileName = Path.GetFileName(file);
            if (!currentFiles.Contains(fileName))
            {
                // 사용되지 않는 파일 발견 시 사용자에게 삭제 여부 확인
                var box = MessageBoxManager
                    .GetMessageBoxStandard(
                        "파일 삭제",
                        $"더 이상 사용되지 않는 파일 '{fileName}'을(를) 삭제하시겠습니까?",
                        ButtonEnum.YesNo);

                var result = await box.ShowAsync();
                if (result == ButtonResult.Yes)
                {
                    File.Delete(file);
                }
            }
        }

        // JSON 직렬화 옵션 설정
        // WriteIndented: JSON 문자열을 보기 좋게 들여쓰기
        // Encoder: 한글 등 유니코드 문자를 안전하게 처리
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        // 각 언어별로 JSON 파일 생성
        foreach (var (language, languageData) in _languageData)
        {
            var jsonFileName = Path.Combine(outputPath, $"GSTR_{language}.json");
            var jsonString   = JsonSerializer.Serialize(languageData, jsonOptions);

            // 암호화 옵션이 활성화된 경우 데이터 암호화 수행
            if (isEncrypted)
            {
                try
                {
                    jsonString = DataEncryption.EncryptData(jsonString, encryptionKey);
                }
                catch (Exception ex)
                {
                    throw new Exception($"'gstr_{language}.json' 데이터 암호화 중 오류 발생: {ex.Message}");
                }
            }

            // JSON 파일 저장 (UTF-8 인코딩 사용)
            await File.WriteAllTextAsync(jsonFileName, jsonString, Encoding.UTF8);
        }
    }
}