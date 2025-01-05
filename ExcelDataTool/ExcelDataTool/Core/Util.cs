using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using ExcelDataTool.ViewModels;
using NPOI.SS.UserModel;

namespace ExcelDataTool.Core;

public static class Util
{
    public static string GetConfigDirectory()
    {
        string configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ExcelDataTool",
            "Configs"
        );
        Console.WriteLine(configDir);
        Directory.CreateDirectory(configDir);
        return configDir;
    }
    
    public static Dictionary<string, string> GetDefaultTypeMap()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "string", "string" },
            { "int", "int" },
            { "long", "long" },
            { "float", "float" },
            { "double", "double" },
            { "bool", "bool" },
            { "DateTime", "DateTime" },
            { "TimeSpan", "TimeSpan" }
        };
    }

    public static ObservableCollection<TypeAliasItem> GetDefaultTypeAliases()
    {
        var collection = new ObservableCollection<TypeAliasItem>();
        foreach (var pair in GetDefaultTypeMap())
        {
            collection.Add(new TypeAliasItem(pair.Key, pair.Value));
        }
        return collection;
    }
    
    public static string GetValue(this ICell? cell)
    {
        if (cell == null)
        {
            throw new ArgumentNullException(nameof(cell));
        }

        switch (cell.CellType)
        {
            case CellType.String:
                return cell.StringCellValue;
            case CellType.Formula:
                switch (cell.CachedFormulaResultType)
                {
                    case CellType.String:
                        return cell.StringCellValue;
                    case CellType.Numeric:
                        return cell.NumericCellValue.ToString(CultureInfo.InvariantCulture);
                    case CellType.Boolean:
                        return cell.BooleanCellValue.ToString();
                    default:
                        return string.Empty;
                }
            default:
                return cell.ToString() ?? string.Empty;
        }
    }
}