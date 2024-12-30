using System.Collections.Generic;
using NPOI.SS.UserModel;

namespace DataTool;

public record SheetContext(
    string TableName,
    string SheetName,
    ISheet Sheet,
    int RowCount,
    int ColumnCount,
    List<string> PropertyTypes,
    List<string> PropertyNames,
    List<string> PropertyDescriptions,
    int PKeyColumnIndex,
    List<ForeignKeyInfo> ForeignKeys);

public record ForeignKeyInfo(int ColumnIndex, string ReferencedTableName);