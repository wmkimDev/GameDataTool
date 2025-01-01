using System;
using System.Linq;

namespace ExcelDataTool.Models;

public class TableAddress
{
    public string? TableName   { get; set; }
    public string? SheetName   { get; set; }
    public string? ColumnName  { get; set; }
    public string? CellAddress { get; set; }

    public override string ToString()
    {
        var parts = new[]
        {
            TableName,
            SheetName,
            ColumnName,
            CellAddress
        };

        return "[" + string.Join("/", parts.Where(p => !string.IsNullOrEmpty(p))) + "]";
    }
    
    public void Reset()
    {
        TableName   = null;
        SheetName   = null;
        ColumnName  = null;
        CellAddress = null;
    }
    
    public void ThrowException(string message)
    {
        throw new Exception(ToString() + " - " + message);
    }
}