using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataTool;

public class CodeGenerator
{
    private readonly IReadOnlyList<SheetContext> _sheetContexts;
    private readonly string _outputPath;
    
    public CodeGenerator(IReadOnlyList<SheetContext> sheetContexts, string outputPath)
    {
        _sheetContexts = sheetContexts;
        _outputPath = outputPath;
    }

    public void GenerateCode()
    {
        var header = $@"using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;

// 자동 생성된 코드입니다. 직접 수정하지 마세요.
// 생성 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
";
        var baseClass = GenerateBaseClass();
        var tables = GenerateTables();
        var manager = GenerateManager();
        
        var code = string.Join("\n\n", header, baseClass, tables, manager);
        File.WriteAllText(Path.Combine(_outputPath, "GameTables.cs"), code);
    }

    private string GenerateBaseClass() => $@"public abstract class GameTable<T> where T : GameTable<T>, new()
{{
    private static readonly Dictionary<string, T> _items = new();

    public static void LoadData(string json)
    {{
        var data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json);
        foreach (var kvp in data)
        {{
            var item = new T();
            item.Parse(kvp.Value);
            _items[kvp.Key] = item;
        }}
    }}

    protected abstract void Parse(Dictionary<string, string> data);
    
    public static IEnumerable<string> GetKeys() => _items.Keys;
    public static T Get(string key) => _items.GetValueOrDefault(key);
    public static IEnumerable<T> GetMany(IEnumerable<string> keys) => keys.Select(Get);
    public static IEnumerable<T> GetAll() => _items.Values;
    public static bool TryGet(string key, out T value) => _items.TryGetValue(key, out value);
    public static IEnumerable<T> Where(Func<T, bool> predicate) => _items.Values.Where(predicate);
}}";

    private string GenerateTables()
    {
        var tables = string.Join("\n\n", _sheetContexts.Select(GenerateTableClass));
        return tables;
    }

    private string GenerateTableClass(SheetContext context)
    {
        var properties = context.PropertyNames!
            .Select((name, i) => i != context.PKeyColumnIndex
                ? $@"    /// <summary>
    /// {context.PropertyDescriptions![i]}
    /// </summary>
    public {context.PropertyTypes![i]} {name} {{ get; private set; }}"
                : "")
            .Where(x => !string.IsNullOrEmpty(x));

        var parseStatements = context.PropertyNames!
            .Select((name, i) => i != context.PKeyColumnIndex
                ? $"        {name} = {GetParseCode(name, context.PropertyTypes![i])};"
                : "")
            .Where(x => !string.IsNullOrEmpty(x));

        return $@"public class {context.SheetName} : GameTable<{context.SheetName}>
{{
{string.Join("\n\n", properties)}

    protected override void Parse(Dictionary<string, string> data)
    {{
{string.Join("\n", parseStatements)}
    }}
}}";
    }

    private string GetParseCode(string name, string type) => type.ToLower() switch
    {
        "string" => $"data[\"{name}\"]",
        "int" => $"int.Parse(data[\"{name}\"], CultureInfo.InvariantCulture)",
        "float" => $"float.Parse(data[\"{name}\"], CultureInfo.InvariantCulture)",
        "double" => $"double.Parse(data[\"{name}\"], CultureInfo.InvariantCulture)",
        "bool" => $"bool.Parse(data[\"{name}\"])",
        "datetime" => $"DateTime.ParseExact(data[\"{name}\"], \"yyyy-MM-dd HH:mm:ss\", CultureInfo.InvariantCulture)",
        "timespan" => $"TimeSpan.ParseExact(data[\"{name}\"], @\"hh\\:mm\\:ss\", CultureInfo.InvariantCulture)",
        _ => $"{type}.Parse(data[\"{name}\"], CultureInfo.InvariantCulture)"
    };

    private string GenerateManager() => $@"public static class GameTableManager
{{
    public static void LoadAllData(Dictionary<string, string> jsonDict)
    {{
        foreach (var entry in jsonDict)
        {{
            string tableName = entry.Key;
            string jsonData = entry.Value;
            switch (tableName)
            {{
                {string.Join("\n                ", _sheetContexts.Select(ctx => $"case \"{ctx.SheetName}\": {ctx.SheetName}.LoadData(jsonData); break;"))}
                default:
                    throw new InvalidOperationException($""Unknown table name: {{tableName}}"");
            }}
        }}
    }}
}}";
}
