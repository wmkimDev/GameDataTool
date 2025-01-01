using System.Collections.Generic;
using NPOI.SS.UserModel;

namespace DataTool;

public class SheetContext
{
    public string?               TableName            { get; set; }
    public string?               SheetName            { get; set; }
    public ISheet?               Sheet                { get; set; }
    public int                  RowCount             { get; set; }
    public int                  ColumnCount          { get; set; }
    public List<string>?         PropertyTypes        { get; set; }
    public List<string>?         PropertyNames        { get; set; }
    public List<string>?         PropertyDescriptions { get; set; }
    public int                  PKeyColumnIndex      { get; set; }
    public List<ForeignKeyInfo>? ForeignKeys          { get; set; }
}

public record ForeignKeyInfo(int ColumnIndex, string ReferencedTableName);