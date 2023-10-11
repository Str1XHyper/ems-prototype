using System.Data;
using System.Data.SqlClient;
using System.Xml.Linq;
using Interfaces;
using Models;
using Models.Enums;
using Models.Extensions;
using Models.Models;

namespace Logic;

public class EntityRepo : IEntityRepo
{
    private readonly ISqlConnector _sqlConnector;
    IEnumerable<ColDescriptor> _entityTableSpec;

    public DateTime SelectedDateTime { get; private set; } = DateTime.Now;
    public string SelectedTable { get; private set; } = "";
    private IEnumerable<ColDescriptor> _entityTableSpecAsOfSelected;
    private IEnumerable<TableDescriptor> _tableDescriptors = new List<TableDescriptor>();

    public EntityRepo(ISqlConnector sqlConnector)
    {
        _sqlConnector = sqlConnector;
        // _entityTableSpec = GetEntityTableSpec();
        // _entityTableSpecAsOfSelected = GetEntityTableSpec(SelectedDateTime);
        // _tableDescriptors = GetAllTables();
    }

    public IEnumerable<string> GetAllTableNames()
    {
        using var connection = _sqlConnector.SqlConnection;
        connection.Open();
        var query = "SELECT * FROM sys.Tables WHERE is_ms_shipped = 0 AND name NOT LIKE '%Spec' and name NOT LIKE '%History' and name NOT Like 'DropdownOptions'";
        using var command = new SqlCommand(query, connection);
        using var reader = command.ExecuteReader();
        var result = new List<string>();
        while (reader.Read())
        {
            result.Add(reader.GetValue(0) as string ?? "");
        }

        connection.Close();
        return result;
    }

    public IEnumerable<string> GetTableNames()
    {
        using var connection = _sqlConnector.SqlConnection;
        connection.Open();
        var query = "SELECT * FROM sys.Tables WHERE is_ms_shipped = 0  AND name NOT LIKE '%Spec' and name NOT LIKE '%History' AND NOT name = @name  and name NOT Like 'DropdownOptions'";
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("name", SelectedTable);
        using var reader = command.ExecuteReader();
        var result = new List<string>();
        while (reader.Read())
        {
            result.Add(reader.GetValue(0) as string ?? "");
        }

        connection.Close();
        return result;
    }

    public IEnumerable<ColDescriptor> GetColumnsSelectableForRelations(string tableName)
    {
        var query = @"
SELECT 
    COLUMN_NAME, 
    CONSTRAINT_TYPE
FROM 
    INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KCU
JOIN 
    INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS TC
ON 
    KCU.TABLE_NAME = TC.TABLE_NAME 
    AND KCU.TABLE_SCHEMA = TC.TABLE_SCHEMA
    AND KCU.CONSTRAINT_NAME = TC.CONSTRAINT_NAME
WHERE 
    KCU.TABLE_NAME = @tableName
    AND TC.CONSTRAINT_TYPE IN ('PRIMARY KEY')
ORDER BY 
    CONSTRAINT_TYPE, COLUMN_NAME;";
        var result = _sqlConnector.GetList(query, new Dictionary<string, object>()
        {
            { "tableName", tableName }
        });

        var descriptorQuery = $"SELECT * FROM [dbo].[{tableName}Spec] WHERE ColumnEnabled = 1 AND ColumnName = @columnName";
        var ColumnName = result.First()["COLUMN_NAME"];
        result = _sqlConnector.GetList(descriptorQuery, new Dictionary<string, object>()
        {
            { "columnName", ColumnName }
        });
        var colDescriptors = new List<ColDescriptor>();
        foreach (var col in result)
        {
            var descriptor = new ColDescriptor();
            foreach (var kvp in col)
            {
                switch (kvp.Key)
                {
                    case "Id" when kvp.Value is Guid id:
                        descriptor.DescriptorId = id;
                        break;
                    case "ColumnName" when kvp.Value is string columnName:
                        descriptor.ColumnName = columnName;
                        break;
                    case "ColumnValueType":
                        if (Int32.TryParse(kvp.Value as string, out var columnType))
                        {
                            descriptor.ColumnValueType = (ValueTypes)columnType;
                        }

                        break;
                    case "ColumnDisplayName" when kvp.Value is string columnDisplayName:
                        descriptor.ColumnDisplayName = columnDisplayName;
                        break;
                    case "ColumnEnabled" when kvp.Value is bool columnEnabled:
                        descriptor.ColumnEnabled = columnEnabled;
                        break;
                    case "ColumnVisible" when kvp.Value is bool columnVisible:
                        descriptor.ColumnVisible = columnVisible;
                        break;
                    case "ColumnEditable" when kvp.Value is bool columnEditable:
                        descriptor.ColumnEditable = columnEditable;
                        break;
                    case "ValueEditable" when kvp.Value is bool valueEditable:
                        descriptor.ValueEditable = valueEditable;
                        break;
                }
            }

            colDescriptors.Add(descriptor);
        }

        return colDescriptors;
    }

    public IEnumerable<ColDescriptor> GetColumnsFromTable(string tableName)
    {
        if (string.IsNullOrEmpty(tableName)) return null;
        using var connection = _sqlConnector.SqlConnection;
        connection.Open();
        var query = $"SELECT * FROM [dbo].[{tableName}Spec] WHERE ColumnEnabled = 1";
        using var command = new SqlCommand(query, connection);
        using var reader = command.ExecuteReader();
        var result = new List<Dictionary<string, object>>();
        while (reader.Read())
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.GetValue(i);
            }

            result.Add(row);
        }

        connection.Close();
        var colDescriptors = new List<ColDescriptor>();
        foreach (var col in result)
        {
            var descriptor = new ColDescriptor();
            foreach (var kvp in col)
            {
                switch (kvp.Key)
                {
                    case "Id" when kvp.Value is Guid id:
                        descriptor.DescriptorId = id;
                        break;
                    case "ColumnName" when kvp.Value is string columnName:
                        descriptor.ColumnName = columnName;
                        break;
                    case "ColumnValueType":
                        if (Int32.TryParse(kvp.Value as string, out var columnType))
                        {
                            descriptor.ColumnValueType = (ValueTypes)columnType;
                        }

                        break;
                    case "ColumnDisplayName" when kvp.Value is string columnDisplayName:
                        descriptor.ColumnDisplayName = columnDisplayName;
                        break;
                    case "ColumnEnabled" when kvp.Value is bool columnEnabled:
                        descriptor.ColumnEnabled = columnEnabled;
                        break;
                    case "ColumnVisible" when kvp.Value is bool columnVisible:
                        descriptor.ColumnVisible = columnVisible;
                        break;
                    case "ColumnEditable" when kvp.Value is bool columnEditable:
                        descriptor.ColumnEditable = columnEditable;
                        break;
                    case "ValueEditable" when kvp.Value is bool valueEditable:
                        descriptor.ValueEditable = valueEditable;
                        break;
                    case "IsMultiSelect" when kvp.Value is bool isMultiSelect:
                        descriptor.IsMultiSelect = isMultiSelect;
                        break;
                    case "IsDropdown" when kvp.Value is bool isDropdown:
                        descriptor.IsDropdown = isDropdown;
                        break;
                }

                if (!descriptor.IsDropdown) continue;
                var optionsQuery = "SELECT Value FROM [dbo].[DropdownOptions] WHERE [Internal_Column_Name] = @columnName";
                var options = _sqlConnector.GetList(optionsQuery, new Dictionary<string, object>()
                {
                    {"columnName", descriptor.ColumnName ?? ""}
                });
                descriptor.DropdownOptions = options.Select(x => x["Value"] as string ?? "");
            }

            colDescriptors.Add(descriptor);
        }

        return colDescriptors;
    }

    public List<Dictionary<ColDescriptor, object>> GetRelationData(ColDescriptor colDescriptor)
    {
        var query = $@"
WITH DirectReferences AS (
    SELECT 
        OBJECT_NAME(f.parent_object_id) AS TableName,
        COL_NAME(fc.parent_object_id, fc.parent_column_id) AS ColumnName,
        OBJECT_NAME(f.referenced_object_id) AS ReferenceTableName,
        COL_NAME(fc.referenced_object_id, fc.referenced_column_id) AS ReferenceColumnName,
        f.name AS ForeignKeyName
    FROM 
        sys.foreign_keys AS f
    INNER JOIN 
        sys.foreign_key_columns AS fc 
        ON f.OBJECT_ID = fc.constraint_object_id
    WHERE 
        OBJECT_NAME(f.parent_object_id) = '{SelectedTable}' 
        AND SCHEMA_NAME(f.schema_id) = 'dbo'
        AND COL_NAME(fc.parent_object_id, fc.parent_column_id) like @columnName
),

PotentialJoinTables AS (
    SELECT 
        OBJECT_NAME(f.parent_object_id) AS TableName
    FROM 
        sys.foreign_keys AS f
    WHERE 
        OBJECT_NAME(f.referenced_object_id) = '{SelectedTable}' 
        AND SCHEMA_NAME(f.schema_id) = 'dbo'
),

JoinTableReferences AS (
    SELECT 
        jt.TableName AS JoinTableName,
        COL_NAME(fc.parent_object_id, fc.parent_column_id) AS ColumnName,
        OBJECT_NAME(f.referenced_object_id) AS ReferenceTableName,
        COL_NAME(fc.referenced_object_id, fc.referenced_column_id) AS ReferenceColumnName,
        f.name AS ForeignKeyName
    FROM 
        PotentialJoinTables jt
    JOIN 
        sys.foreign_keys AS f
        ON jt.TableName = OBJECT_NAME(f.parent_object_id)
    JOIN 
        sys.foreign_key_columns AS fc 
        ON f.OBJECT_ID = fc.constraint_object_id
    WHERE 
        OBJECT_NAME(f.referenced_object_id) <> '{SelectedTable}' 
        AND SCHEMA_NAME(f.schema_id) = 'dbo'
        AND COL_NAME(fc.parent_object_id, fc.parent_column_id) like @columnName
)

SELECT * FROM DirectReferences
UNION ALL
SELECT 
    JoinTableName AS TableName,
    ColumnName,
    ReferenceTableName,
    ReferenceColumnName,
    ForeignKeyName
FROM JoinTableReferences
ORDER BY 
    TableName, 
    ColumnName, 
    ReferenceTableName, 
    ReferenceColumnName;

";

        var constraintResult = _sqlConnector.GetRow(query, new Dictionary<string, object>()
        {
            { "columnName", $"{colDescriptor.ColumnName}%" }
        });
        if (constraintResult is null) throw new Exception();
        var otherTableSpec = GetTableSpec(constraintResult["ReferenceTableName"] as string ?? "");
        var otherTableSelectedColumns = string.Join(", ", otherTableSpec.Select(x => x.ColumnName));

        var dataQuery = $"SELECT {otherTableSelectedColumns} FROM dbo.[{constraintResult["ReferenceTableName"]}]";
        var result = _sqlConnector.GetList(dataQuery);
        var rows = result.Select(resultRow => resultRow.ToDictionary(col => otherTableSpec.First(x => x.ColumnName == col.Key), col => col.Value)).ToList();
        return rows;
    }

    public void UpdateSelectedTable(string value)
    {
        SelectedTable = value;
        _entityTableSpec = GetEntityTableSpec();
        _entityTableSpecAsOfSelected = GetEntityTableSpec(SelectedDateTime);
    }

    public void UpdateDateSelectedTime(DateTime time)
    {
        SelectedDateTime = time;
        _entityTableSpecAsOfSelected = GetEntityTableSpec(SelectedDateTime);
    }


    private IEnumerable<ColDescriptor> GetEntityTableSpec(DateTime? asofDateTime = null)
    {
        if (string.IsNullOrEmpty(SelectedTable)) return new List<ColDescriptor>();
        using var connection = _sqlConnector.SqlConnection;
        connection.Open();
        var query = "";
        if (_tableDescriptors.Any(x => x.TableName == SelectedTable && x.HasHistoryTable))
        {
            query = asofDateTime is not null ? $"SELECT * FROM [dbo].[{SelectedTable}Spec] FOR SYSTEM_TIME AS OF @dateTime WHERE ColumnEnabled = 1 ORDER BY ValidFrom" : $"SELECT * FROM [dbo].[{SelectedTable}Spec] WHERE ColumnEnabled = 1 ORDER BY ValidFrom";
        }
        else
        {
            query = $"SELECT * FROM [dbo].[{SelectedTable}Spec] WHERE ColumnEnabled = 1";
        }

        using var command = new SqlCommand(query, connection);
        if (asofDateTime is not null)
        {
            command.Parameters.AddWithValue("dateTime", asofDateTime.Value.ToUniversalTime().ToString("u").TrimEnd('Z'));
        }

        using var reader = command.ExecuteReader();
        var result = new List<Dictionary<string, object>>();
        while (reader.Read())
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.GetValue(i);
            }

            result.Add(row);
        }

        connection.Close();
        var colDescriptors = new List<ColDescriptor>();
        foreach (var col in result)
        {
            var descriptor = new ColDescriptor();
            foreach (var kvp in col)
            {
                switch (kvp.Key)
                {
                    case "Id" when kvp.Value is Guid id:
                        descriptor.DescriptorId = id;
                        break;
                    case "ColumnName" when kvp.Value is string columnName:
                        descriptor.ColumnName = columnName;
                        break;
                    case "ColumnValueType":
                        if (Int32.TryParse(kvp.Value as string, out var columnType))
                        {
                            descriptor.ColumnValueType = (ValueTypes)columnType;
                        }

                        break;
                    case "ColumnDisplayName" when kvp.Value is string columnDisplayName:
                        descriptor.ColumnDisplayName = columnDisplayName;
                        break;
                    case "ColumnEnabled" when kvp.Value is bool columnEnabled:
                        descriptor.ColumnEnabled = columnEnabled;
                        break;
                    case "ColumnVisible" when kvp.Value is bool columnVisible:
                        descriptor.ColumnVisible = columnVisible;
                        break;
                    case "ColumnEditable" when kvp.Value is bool columnEditable:
                        descriptor.ColumnEditable = columnEditable;
                        break;
                    case "ValueEditable" when kvp.Value is bool valueEditable:
                        descriptor.ValueEditable = valueEditable;
                        break;
                    case "IsMultiSelect" when kvp.Value is bool isMultiSelect:
                        descriptor.IsMultiSelect = isMultiSelect;
                        break;
                    case "IsDropdown" when kvp.Value is bool isDropdown:
                        descriptor.IsDropdown = isDropdown;
                        break;
                }

                if (!descriptor.IsDropdown) continue;
                var optionsQuery = "SELECT Value FROM [dbo].[DropdownOptions] WHERE [Internal_Column_Name] = @columnName";
                var options = _sqlConnector.GetList(optionsQuery, new Dictionary<string, object>()
                {
                    {"columnName", descriptor.ColumnName ?? ""}
                });
                descriptor.DropdownOptions = options.Select(x => x["Value"] as string ?? "");
            }

            colDescriptors.Add(descriptor);
        }

        return colDescriptors;
    }

    public int GetTotalEntityCount()
    {
        
        if (string.IsNullOrEmpty(SelectedTable)) return 0;
        var query = $"SELECT Count(*) FROM [dbo].[{SelectedTable}]";

        using var connection = _sqlConnector.SqlConnection;
        connection.Open();

        using var command = new SqlCommand(query, connection);
        using var reader = command.ExecuteReader();

        var count = -1;

        while (reader.Read())
        {
            count = reader.GetValue(0) as int? ?? -1;
        }

        return count;
    }

    public List<Dictionary<ColDescriptor, object>> GetRangeOfEntitiesBySelectedDate(int requestStartIndex, int requestCount)
    {
        try
        {
            var parameters = new Dictionary<string, object>();
            parameters.Add("dateTime", SelectedDateTime.ToUniversalTime().ToString("u").TrimEnd('Z'));
            var selectedColumns = string.Join(", ", _entityTableSpecAsOfSelected.Select(x => $"[{x.ColumnName}]"));
            var query = _tableDescriptors.Any(x => x.TableName == SelectedTable && x.HasHistoryTable) ? 
                $"SELECT {selectedColumns} FROM [dbo].[{SelectedTable}] For System_Time As Of @dateTime ORDER BY ValidFrom OFFSET {requestStartIndex} ROWS FETCH NEXT {requestCount} ROWS ONLY": 
                $"SELECT {selectedColumns} FROM [dbo].[{SelectedTable}] ORDER BY [{SelectedTable.ToLower()}_Id_1] OFFSET {requestStartIndex} ROWS FETCH NEXT {requestCount} ROWS ONLY";
            var result = _sqlConnector.GetList(query, parameters).ToList();

            var rows = result.Select(resultRow => resultRow.ToDictionary(col => _entityTableSpecAsOfSelected.First(x => x.ColumnName == col.Key), col => col.Value)).ToList();

            return rows;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new List<Dictionary<ColDescriptor, object>>();
        }
    }

    public int GetEntityCountByDate()
    {
        
        if (string.IsNullOrEmpty(SelectedTable)) return 0;
        var query = $"SELECT Count(*) FROM [dbo].[{SelectedTable}] For System_Time As Of @dateTime";

        using var connection = _sqlConnector.SqlConnection;
        connection.Open();

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("dateTime", SelectedDateTime.ToUniversalTime().ToString("u").TrimEnd('Z'));
        using var reader = command.ExecuteReader();
        var count = -1;

        while (reader.Read())
        {
            count = reader.GetValue(0) as int? ?? -1;
        }

        return count;
    }

    public List<Dictionary<ColDescriptor, object>> GetRangeOfEntities(int start, int end)
    {
        
        if (string.IsNullOrEmpty(SelectedTable)) return null;
        var foreignKeyQuery = $@"
SELECT 
    OBJECT_NAME(f.parent_object_id) AS TableName,
    COL_NAME(fc.parent_object_id, fc.parent_column_id) AS ColumnName,
    OBJECT_NAME(f.referenced_object_id) AS ReferenceTableName,
    COL_NAME(fc.referenced_object_id, fc.referenced_column_id) AS ReferenceColumnName,
    f.name AS ForeignKeyName
FROM 
    sys.foreign_keys AS f
INNER JOIN 
    sys.foreign_key_columns AS fc 
    ON f.OBJECT_ID = fc.constraint_object_id
WHERE 
    OBJECT_NAME(f.parent_object_id) = '{SelectedTable}' 
    AND SCHEMA_NAME(f.schema_id) = 'dbo'
ORDER BY 
    TableName, 
    ColumnName, 
    ReferenceTableName, 
    ReferenceColumnName;";

        var foreignKeysDBResult = _sqlConnector.GetList(foreignKeyQuery);
        var selectedColumns = string.Join(", ", _entityTableSpec.Select(x => $"[{x.ColumnName}]"));
        IEnumerable<ColDescriptor> foreignTableSpec = new List<ColDescriptor>();

        var joins = "";
        foreach (var dict in foreignKeysDBResult.Where(dict => dict["ReferenceTableName"] as string is not null or ""))
        {
            foreignTableSpec = GetTableSpec(dict["ReferenceTableName"] as string );
            var foreignKeySelectedColumns = string.Join(", ", foreignTableSpec.Select(x => $"[{x.ColumnName}]"));
            selectedColumns += ", " + foreignKeySelectedColumns;

            joins += " LEFT JOIN " + $"[dbo].[{dict["ReferenceTableName"]}] ON [{dict["ColumnName"]}] = [{dict["ReferenceColumnName"]}]";
        }

        var query = "";
        if (foreignKeysDBResult.Any())
        {
            query = $"SELECT {selectedColumns} FROM [dbo].[{SelectedTable}]" + joins;
        }
        else
        {
            query = $"SELECT {selectedColumns} FROM [dbo].[{SelectedTable}]";
        }

        query += $" ORDER BY [{SelectedTable.ToLower()}_Id_1] OFFSET {start} ROWS FETCH NEXT {end} ROWS ONLY";

        var result = _sqlConnector.GetList(query);

        List<Dictionary<ColDescriptor, object>> rows = new();
        foreach (var resultRow in result)
        {
            Dictionary<ColDescriptor, object> resultDict = new();
            foreach (var colDescriptor in _entityTableSpec)
            {
                if (foreignKeysDBResult.Any(x => x["ColumnName"] as string == colDescriptor.ColumnName))
                {
                    string value = "";
                    foreach (var foreignKey in foreignKeysDBResult.Where(x => x["ColumnName"] as string == colDescriptor.ColumnName))
                    {
                        var foreignTable = foreignKey["ReferenceTableName"] as string;
                        var foreignColumn = foreignKey["ReferenceColumnName"] as string;
                        List<object> foreignValues = new();
                        foreignTableSpec = GetTableSpec(foreignTable);
                        foreach (var column in foreignTableSpec.Where(x => x.ColumnName != foreignColumn))
                        {
                            foreignValues.Add(resultRow[column.ColumnName]);
                        }

                        value = string.Join(" ", foreignValues);
                        // var foreignValue = resultRow[foreignTableSpec.First(x => x.ColumnName != foreignColumn).ColumnName];
                        // colDescriptor.ColumnDisplayName = "Person: " + foreignTableSpec.First(x => x.ColumnName != foreignColumn).ColumnDisplayName;
                    }

                    resultDict.Add(colDescriptor, value);
                }
                else
                {
                    if (colDescriptor.IsMultiSelect && colDescriptor.ColumnValueType != ValueTypes.RELATION)
                    {
                        var xmlString = resultRow[colDescriptor.ColumnName] as string;
                        try
                        {
                            var xml = XElement.Parse(xmlString);
                            resultDict.Add(colDescriptor, xml.Elements().Select(x => x.Value).ToArray());
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                    else
                    {
                        resultDict.Add(colDescriptor, resultRow[colDescriptor.ColumnName]);
                    }
                }
                // if (foreignTableSpec.Any(x => x.ColumnName == colDescriptor.ColumnName))
                // {
                //     colDescriptor.ColumnDisplayName = "Person: " + colDescriptor.ColumnDisplayName;
                // }
            }

            rows.Add(resultDict);
        }

        // var rows = result.Select(resultRow => resultRow.ToDictionary(col => _entityTableSpec.FirstOrDefault(x => x.ColumnName == col.Key) ?? foreignTableSpec.First(x => x.ColumnName == col.Key), col => col.Value)).ToList();

        return rows;
    }

    public List<Dictionary<ColDescriptor, object>> GetAllEntities()
    {
        var selectedColumns = string.Join(", ", _entityTableSpec.Select(x => $"[{x.ColumnName}]"));
        var query = $"WITH Records as (SELECT {selectedColumns}, ROW_NUMBER() OVER ( ORDER BY ValidFrom) AS 'RowNumber' FROM [dbo].[{SelectedTable}]) Select * From Records WHERE RowNumber between 1 and 1000";
        var result = _sqlConnector.GetList(query);

        var rows = result.Select(resultRow => resultRow.SkipLast(1).ToDictionary(col => _entityTableSpec.First(x => x.ColumnName == col.Key), col => col.Value)).ToList();

        return rows;
    }


    // public void AddColumn(string columnName, ValueTypes columnValueType, string? referenceTable = null, string? referenceColumn = null, bool? isMultiSelect =false)
    // {
    //     var tableName = SelectedTable.ToLower();
    //     var datatype = columnValueType switch
    //     {
    //         ValueTypes.NUMBER => "int",
    //         ValueTypes.STRING => "nvarchar(100)",
    //         ValueTypes.DATE => "datetime2(7)",
    //         ValueTypes.BOOLEAN => "bit",
    //         ValueTypes.GUID => "uniqueidentifier",
    //         ValueTypes.EMAIL => "nvarchar(100)",
    //         ValueTypes.PHONE => "nvarchar(100)",
    //         ValueTypes.URL => "nvarchar(100)",
    //         ValueTypes.PASSWORD => "nvarchar(100)",
    //         ValueTypes.CURRENCY => "decimal(18,2)",
    //         ValueTypes.PERCENTAGE => "decimal(18,2)",
    //         ValueTypes.DATETIME => "datetime2(7)",
    //         ValueTypes.TIME => "time(7)",
    //         ValueTypes.RELATION => "uniqueidentifier",
    //         _ => throw new ArgumentOutOfRangeException(nameof(columnValueType), columnValueType, null)
    //     };
    //
    //     using var connection = _sqlConnector.SqlConnection;
    //     connection.Open();
    //
    //     SqlCommand command = connection.CreateCommand();
    //     SqlTransaction transaction;
    //
    //     // Start a local transaction.
    //     transaction = connection.BeginTransaction();
    //
    //     // Must assign both transaction object and connection
    //     // to Command object for a pending local transaction
    //     command.Connection = connection;
    //     command.Transaction = transaction;
    //
    //     var counter = 1;
    //     if (_entityTableSpec.Any(x => x.ColumnName.StartsWith($"{tableName}_{columnName}")))
    //     {
    //         var highest = _entityTableSpec
    //             .Where(x => x.ColumnName.StartsWith($"{tableName}_{columnName}"))
    //             .Select(x => Int32.Parse(x.ColumnName.Split('_')[2])).MaxBy(x => x);
    //         counter = highest + 1;
    //     }
    //
    //     var internalColumnName = $"{tableName}_{columnName}_{counter}";
    //
    //     var insertQuery = $"ALTER TABLE [dbo].[{tableName}] ADD [{internalColumnName}] {datatype};";
    //
    //     if (columnValueType == ValueTypes.RELATION)
    //     {
    //         if (referenceColumn is null || referenceTable is null)
    //             throw new ArgumentNullException(nameof(referenceColumn), "Reference column and table must be specified");
    //         insertQuery += $"ALTER TABLE [dbo].[{tableName}] ADD CONSTRAINT [FK_{tableName}_{referenceTable}] FOREIGN KEY ([{internalColumnName}]) REFERENCES [dbo].[{referenceTable}] ([{referenceColumn}]);";
    //     }
    //
    //     var specQuery = $"INSERT INTO [dbo].[{tableName}Spec] (ColumnName, ColumnDisplayName, ColumnValueType, ColumnEnabled, ValueEditable, ColumnEditable, ColumnVisible, IsMultiSelect) VALUES (@internalColumnName, @columnName, @columnValueType, 1, 1, 1, 1, @isMultiSelect);";
    //     try
    //     {
    //         command.CommandText = insertQuery;
    //         command.ExecuteNonQuery();
    //
    //         command.CommandText = specQuery;
    //         command.Parameters.AddWithValue("internalColumnName", internalColumnName);
    //         command.Parameters.AddWithValue("columnName", columnName);
    //         command.Parameters.AddWithValue("columnValueType", columnValueType);
    //         command.Parameters.AddWithValue("isMultiSelect", isMultiSelect);
    //         command.ExecuteNonQuery();
    //
    //         transaction.Commit();
    //     }
    //     catch (Exception ex)
    //     {
    //         Console.WriteLine(ex);
    //         transaction.Rollback();
    //     }
    //     finally
    //     {
    //         connection.Close();
    //         _entityTableSpec = GetEntityTableSpec();
    //     }
    // }

    public IEnumerable<ColDescriptor> GetTableSpec(string tableName = "")
    {
        if (tableName == "")
        {
            if (string.IsNullOrEmpty(SelectedTable)) return null;
            tableName = SelectedTable.ToLower();
        }

        using var connection = _sqlConnector.SqlConnection;
        connection.Open();
        var query = _tableDescriptors.Any(x => x.TableName == tableName && x.HasHistoryTable)? $"SELECT * FROM [dbo].[{tableName}Spec] ORDER BY ValidFrom" :  $"SELECT * FROM [dbo].[{tableName}Spec]";
        using var command = new SqlCommand(query, connection);
        using var reader = command.ExecuteReader();
        var result = new List<Dictionary<string, object>>();
        while (reader.Read())
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.GetValue(i);
            }

            result.Add(row);
        }

        connection.Close();
        var colDescriptors = new List<ColDescriptor>();
        foreach (var col in result)
        {
            var descriptor = new ColDescriptor();
            foreach (var kvp in col)
            {
                switch (kvp.Key)
                {
                    case "Id" when kvp.Value is Guid id:
                        descriptor.DescriptorId = id;
                        break;
                    case "ColumnName" when kvp.Value is string columnName:
                        descriptor.ColumnName = columnName;
                        break;
                    case "ColumnValueType":
                        if (Int32.TryParse(kvp.Value as string, out var columnType))
                        {
                            descriptor.ColumnValueType = (ValueTypes)columnType;
                        }

                        break;
                    case "ColumnDisplayName" when kvp.Value is string columnDisplayName:
                        descriptor.ColumnDisplayName = columnDisplayName;
                        break;
                    case "ColumnEnabled" when kvp.Value is bool columnEnabled:
                        descriptor.ColumnEnabled = columnEnabled;
                        break;
                    case "ColumnVisible" when kvp.Value is bool columnVisible:
                        descriptor.ColumnVisible = columnVisible;
                        break;
                    case "ColumnEditable" when kvp.Value is bool columnEditable:
                        descriptor.ColumnEditable = columnEditable;
                        break;
                    case "ValueEditable" when kvp.Value is bool valueEditable:
                        descriptor.ValueEditable = valueEditable;
                        break;
                    case "IsMultiSelect" when kvp.Value is bool isMultiSelect:
                        descriptor.IsMultiSelect = isMultiSelect;
                        break;
                    case "IsDropdown" when kvp.Value is bool isDropdown:
                        descriptor.IsDropdown = isDropdown;
                        break;
                }

                if (!descriptor.IsDropdown) continue;
                var optionsQuery = "SELECT Value FROM [dbo].[DropdownOptions] WHERE [Internal_Column_Name] = @columnName";
                var options = _sqlConnector.GetList(optionsQuery, new Dictionary<string, object>()
                {
                    {"columnName", descriptor.ColumnName ?? ""}
                });
                descriptor.DropdownOptions = options.Select(x => x["Value"] as string ?? "");
            }

            colDescriptors.Add(descriptor);
        }

        return colDescriptors;
    }

    public void RemoveColumn(Guid id)
    {
        using var connection = _sqlConnector.SqlConnection;
        connection.Open();

        SqlCommand command = connection.CreateCommand();
        SqlTransaction transaction;

        // Start a local transaction.
        transaction = connection.BeginTransaction();

        // Must assign both transaction object and connection
        // to Command object for a pending local transaction
        command.Connection = connection;
        command.Transaction = transaction;

        try
        {
            command.CommandText = $"Update [dbo].[{SelectedTable}Spec] SET ColumnEnabled = 0 WHERE Id = @id";
            command.Parameters.AddWithValue("id", id);

            command.ExecuteNonQuery();
            transaction.Commit();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            transaction.Rollback();
        }
        finally
        {
            connection.Close();
            _entityTableSpec = GetEntityTableSpec();
        }
    }

    public void UpdateColumn(ColDescriptor selectedColDescriptor)
    {
        var tableName = SelectedTable.ToLower();
        using var connection = _sqlConnector.SqlConnection;
        connection.Open();

        SqlCommand command = connection.CreateCommand();
        SqlTransaction transaction;

        // Start a local transaction.
        transaction = connection.BeginTransaction();

        // Must assign both transaction object and connection
        // to Command object for a pending local transaction
        command.Connection = connection;
        command.Transaction = transaction;
        var spec = GetTableSpec();
        var original = spec.FirstOrDefault(x => x.DescriptorId == selectedColDescriptor.DescriptorId);
        if (original is null)
        {
            return;
        }

        try
        {
            if (selectedColDescriptor.ColumnValueType == original.ColumnValueType)
            {
                command.CommandText = $"Update [dbo].[{tableName}Spec] SET ColumnEnabled = @columnEnabled, ColumnDisplayName = @columnDisplayName, ColumnVisible = @columnVisible, ColumnEditable = @columnEditable, ValueEditable = @valueEditable WHERE Id = @id";
                command.Parameters.AddWithValue("id", selectedColDescriptor.DescriptorId);
                command.Parameters.AddWithValue("columnEnabled", selectedColDescriptor.ColumnEnabled);
                command.Parameters.AddWithValue("columnDisplayName", selectedColDescriptor.ColumnDisplayName);
                command.Parameters.AddWithValue("columnVisible", selectedColDescriptor.ColumnVisible);
                command.Parameters.AddWithValue("columnEditable", selectedColDescriptor.ColumnEditable);
                command.Parameters.AddWithValue("valueEditable", selectedColDescriptor.ValueEditable);

                command.ExecuteNonQuery();
            }
            else
            {
                var datatype = selectedColDescriptor.ColumnValueType switch
                {
                    ValueTypes.NUMBER => "int",
                    ValueTypes.STRING => "nvarchar(100)",
                    ValueTypes.DATE => "datetime2(7)",
                    ValueTypes.BOOLEAN => "bit",
                    ValueTypes.GUID => "uniqueidentifier",
                    ValueTypes.EMAIL => "nvarchar(100)",
                    ValueTypes.PHONE => "nvarchar(100)",
                    ValueTypes.URL => "nvarchar(100)",
                    ValueTypes.PASSWORD => "nvarchar(100)",
                    ValueTypes.CURRENCY => "decimal(18,2)",
                    ValueTypes.PERCENTAGE => "decimal(18,2)",
                    ValueTypes.DATETIME => "datetime2(7)",
                    ValueTypes.TIME => "time(7)",
                    _ => throw new ArgumentOutOfRangeException()
                };
                var counter = 1;
                if (spec.Any(x => x.ColumnName.StartsWith($"{tableName}_{selectedColDescriptor.ColumnDisplayName}")))
                {
                    var highest = _entityTableSpec
                        .Where(x => x.ColumnName.StartsWith($"{tableName}_{selectedColDescriptor.ColumnDisplayName}"))
                        .Select(x => Int32.Parse(x.ColumnName.Split('_')[2])).MaxBy(x => x);
                    counter = highest + 1;
                }

                var internalColumnName = $"{tableName}_{selectedColDescriptor.ColumnDisplayName}_{counter}";
                command.CommandText = $"UPDATE [dbo].[{SelectedTable}Spec] SET ColumnEnabled = 0 WHERE Id = @id";
                command.Parameters.AddWithValue("id", selectedColDescriptor.DescriptorId);
                command.ExecuteNonQuery();
                command.CommandText = $"Insert Into [dbo].[{SelectedTable}Spec] (ColumnName, ColumnDisplayName, ColumnValueType, ColumnEnabled, ColumnEditable, ValueEditable) VALUES (@internalColumnName, @columnName, @columnValueType, 1, @columnEditable, @valueEditable)";
                command.Parameters.AddWithValue("internalColumnName", internalColumnName);
                command.Parameters.AddWithValue("columnValueType", selectedColDescriptor.ColumnValueType);
                command.Parameters.AddWithValue("columnName", selectedColDescriptor.ColumnDisplayName);
                command.Parameters.AddWithValue("columnVisible", selectedColDescriptor.ColumnVisible);
                command.Parameters.AddWithValue("columnEditable", selectedColDescriptor.ColumnEditable);
                command.Parameters.AddWithValue("valueEditable", selectedColDescriptor.ValueEditable);
                command.ExecuteNonQuery();
                command.CommandText = $"ALTER TABLE [dbo].[{SelectedTable}] ADD [{internalColumnName}] {datatype}";

                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            transaction.Rollback();
        }
        finally
        {
            connection.Close();
            _entityTableSpec = GetEntityTableSpec();
        }
    }

    public void UpdateRow(Dictionary<ColDescriptor, object> row)
    {
        var tableName = SelectedTable.ToLower();
        var setters = row.Keys.Where(x => x.ColumnName != $"{tableName}_Id_1").Aggregate("", (current, key) => current + $"[{key.ColumnName}] = @{key.ColumnName.Replace(' ', '_')}, ");
        setters = setters.TrimEnd(',', ' ');
        var query = $"Update [dbo].[{SelectedTable}] SET {setters} WHERE [{tableName}_Id_1] = @{tableName}_Id_1";


        using var connection = _sqlConnector.SqlConnection;
        connection.Open();
        SqlCommand command = connection.CreateCommand();
        SqlTransaction transaction;

        // Start a local transaction.
        transaction = connection.BeginTransaction();

        // Must assign both transaction object and connection
        // to Command object for a pending local transaction
        command.Connection = connection;
        command.Transaction = transaction;
        try
        {
            command.CommandText = query;
            foreach (var kvp in row)
            {
                if (kvp.Key.IsMultiSelect && kvp.Key.ColumnValueType != ValueTypes.RELATION)
                {
                    XElement xmlElements = new XElement("Values", ((string[])kvp.Value).Select(x => new XElement("Value", x)));
                    command.Parameters.AddWithValue(kvp.Key.ColumnName.Replace(' ', '_'), xmlElements.ToString());
                }

                else
                {
                    command.Parameters.AddWithValue(kvp.Key.ColumnName.Replace(' ', '_'), kvp.Value);
                    
                }
            }

            command.ExecuteNonQuery();
            transaction.Commit();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            transaction.Rollback();
        }
        finally
        {
            connection.Close();
            _entityTableSpec = GetEntityTableSpec();
        }
    }

    public void AddRow(Dictionary<ColDescriptor, object> row)
    {
        var tableName = SelectedTable.ToLower();
        var columns = string.Join(", ", row.Keys.Where(x => x.ColumnName != $"{tableName}_Id_1" && x is not { ColumnValueType: ValueTypes.RELATION, IsMultiSelect: true }).Select(x => $"[{x.ColumnName}]"));
        var values = string.Join(", ", row.Keys.Where(x => x.ColumnName != $"{tableName}_Id_1" && x is not { ColumnValueType: ValueTypes.RELATION, IsMultiSelect: true }).Select(x => $"@{x.ColumnName.Replace(' ', '_')}"));

        var query = $"INSERT INTO [dbo].[{tableName}] ({columns}) OUTPUT INSERTED.[{tableName}_Id_1] VALUES ({values}) ;";
        
        
        using var connection = _sqlConnector.SqlConnection;
        connection.Open();
        SqlCommand command = connection.CreateCommand();
        SqlTransaction transaction;

        // Start a local transaction.
        transaction = connection.BeginTransaction();

        // Must assign both transaction object and connection
        // to Command object for a pending local transaction
        command.Connection = connection;
        command.Transaction = transaction;
        try
        {
            command.CommandText = query;
            foreach (var kvp in row.Where(x => x.Key.ColumnName != $"{tableName}_Id_1" && x.Key is not { ColumnValueType: ValueTypes.RELATION, IsMultiSelect: true }))
            {
                command.Parameters.AddWithValue(kvp.Key.ColumnName.Replace(' ', '_'), kvp.Value);
            }

            var insertedId = command.ExecuteScalar();
            
            var joinTableQuery = "";

            foreach (var kvp in row.Where(x => x.Key is { ColumnValueType: ValueTypes.RELATION, IsMultiSelect: true }))
            {
                var (joinTableName, el1, el2) = GetJoinTableName(kvp.Key.ColumnName);
                joinTableQuery += $@"
INSERT INTO [dbo].[{joinTableName}] ({el1}, {el2}) VALUES
";
                var valuesList = (string[])kvp.Value;
                foreach (var value in valuesList)
                {
                    joinTableQuery += $"('{insertedId}', '{value}'),";
                }

                joinTableQuery = joinTableQuery.TrimEnd(',');
                joinTableQuery += ";";
            }

            if (string.IsNullOrEmpty(joinTableQuery))
            {
                transaction.Commit();
                return;
            }
            command.CommandText = joinTableQuery;
            command.ExecuteNonQuery();
            transaction.Commit();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            transaction.Rollback();
        }
        finally
        {
            connection.Close();
            _entityTableSpec = GetEntityTableSpec();
        }
    }

    public List<TableDescriptor> GetAllTables()
    {
        var query = @"
        SELECT 
            name as TableName,
            IIF(history_table_id IS NOT NULL, 1, 0) AS HasHistoryTable
        FROM sys.Tables
        WHERE is_ms_shipped = 0 AND name NOT LIKE '%Spec' AND name NOT LIKE '%History' AND name NOT Like 'DropdownOptions'
        ";
        var result = _sqlConnector.GetList(query);
        List<TableDescriptor> tableDescriptors = new();
        foreach (var col in result)
        {
            var descriptor = new TableDescriptor();
            foreach (var kvp in col)
            {
                switch (kvp.Key)
                {
                    case "TableName" when kvp.Value is string name:
                        descriptor.TableName = name;
                        break;
                    case "HasHistoryTable" when kvp.Value is int HasHistoryTable:
                        descriptor.HasHistoryTable = HasHistoryTable == 1;
                        break;
                }
            }

            tableDescriptors.Add(descriptor);
        }

        return tableDescriptors;
    }

    public void AddTable(TableDescriptor newTable)
    {
        var query = "";
        if (newTable.HasHistoryTable)
        {
            query = $@"
CREATE TABLE dbo.{newTable.TableName} (
    [{newTable.TableName.ToLower()}_Id_1] UniqueIdentifier DEFAULT NEWID() NOT NULL PRIMARY KEY ,
    [ValidFrom] DATETIME2 GENERATED ALWAYS AS ROW START,
    [ValidTo] DATETIME2 GENERATED ALWAYS AS ROW END,
    PERIOD FOR SYSTEM_TIME(ValidFrom, ValidTo)
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.{newTable.TableName}History));

CREATE TABLE dbo.{newTable.TableName}Spec (
    [Id] UniqueIdentifier DEFAULT NEWID() NOT NULL PRIMARY KEY ,
    [ColumnName] NVARCHAR(100) NOT NULL,
    [ColumnDisplayName] NVARCHAR(100) NOT NULL,
    [ColumnValueType] NVARCHAR(100) NOT NULL,
    [ColumnRestriction] NVARCHAR(100),
    [ColumnEnabled] BIT NOT NULL,
    [ColumnVisible] BIT NOT NULL,
    [ColumnEditable] BIT NOT NULL,
    [ValueEditable] BIT NOT NULL,
    [IsMultiSelect] BIT DEFAULT 0 NOT NULL,
    [IsDropdown] BIT DEFAULT 0 NOT NULL,
    [ValidFrom] DATETIME2 GENERATED ALWAYS AS ROW START,
    [ValidTo] DATETIME2 GENERATED ALWAYS AS ROW END,
    PERIOD FOR SYSTEM_TIME(ValidFrom, ValidTo)
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.{newTable.TableName}SpecHistory));
Insert Into [dbo].[{newTable.TableName}Spec] (ColumnName, ColumnDisplayName, ColumnValueType, ColumnEnabled, ColumnEditable, ValueEditable, ColumnVisible) VALUES ('{newTable.TableName.ToLower()}_Id_1', 'Id', 4, 1, 0, 0, 1)
";
        }
        else
        {
            query = $@"
CREATE TABLE dbo.{newTable.TableName} (
    [{newTable.TableName.ToLower()}_Id_1] UniqueIdentifier DEFAULT NEWID() NOT NULL PRIMARY KEY ,
    
);

CREATE TABLE dbo.{newTable.TableName}Spec (
    [Id] UniqueIdentifier DEFAULT NEWID() NOT NULL PRIMARY KEY ,
    [ColumnName] NVARCHAR(100) NOT NULL,
    [ColumnDisplayName] NVARCHAR(100) NOT NULL,
    [ColumnValueType] NVARCHAR(100) NOT NULL,
    [ColumnRestriction] NVARCHAR(100),
    [ColumnEnabled] BIT NOT NULL,
    [ColumnVisible] BIT NOT NULL,
    [ColumnEditable] BIT NOT NULL,
    [ValueEditable] BIT NOT NULL,
    [IsMultiSelect] BIT DEFAULT 0 NOT NULL,
    [IsDropdown] BIT DEFAULT 0 NOT NULL,
);
Insert Into [dbo].[{newTable.TableName}Spec] (ColumnName, ColumnDisplayName, ColumnValueType, ColumnEnabled, ColumnEditable, ValueEditable, ColumnVisible) VALUES ('{newTable.TableName.ToLower()}_Id_1', 'Id', 4, 1, 0, 0, 1)
";
        }

        using var connection = _sqlConnector.SqlConnection;
        connection.Open();
        var command = connection.CreateCommand();

        // Start a local transaction.
        var transaction =
            connection.BeginTransaction();

        // Must assign both transaction object and connection
        // to Command object for a pending local transaction
        command.Connection = connection;
        command.Transaction = transaction;
        try
        {
            command.CommandText = query;

            command.ExecuteNonQuery();
            transaction.Commit();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Console.WriteLine(ex);
        }
        finally
        {
            connection.Close();
        }
    }

    public void AddColumn(NewColumn newColumn)
    {
        var tableName = SelectedTable.ToLower();
        var datatype = newColumn.ColumnValueType switch
        {
            ValueTypes.NUMBER => "int",
            ValueTypes.STRING => "nvarchar(100)",
            ValueTypes.DATE => "datetime2(7)",
            ValueTypes.BOOLEAN => "bit",
            ValueTypes.GUID => "uniqueidentifier",
            ValueTypes.EMAIL => "nvarchar(100)",
            ValueTypes.PHONE => "nvarchar(100)",
            ValueTypes.URL => "nvarchar(100)",
            ValueTypes.PASSWORD => "nvarchar(100)",
            ValueTypes.CURRENCY => "decimal(18,2)",
            ValueTypes.PERCENTAGE => "decimal(18,2)",
            ValueTypes.DATETIME => "datetime2(7)",
            ValueTypes.TIME => "time(7)",
            ValueTypes.RELATION => "uniqueidentifier",
            _ => throw new ArgumentOutOfRangeException()
        };

        if (newColumn.IsMultiSelect && newColumn.ColumnValueType != ValueTypes.RELATION)
        {
            datatype = "xml";
        }

        using var connection = _sqlConnector.SqlConnection;
        connection.Open();

        SqlCommand command = connection.CreateCommand();
        SqlTransaction transaction;

        // Start a local transaction.
        transaction = connection.BeginTransaction();

        // Must assign both transaction object and connection
        // to Command object for a pending local transaction
        command.Connection = connection;
        command.Transaction = transaction;

        var counter = 1;
        if (_entityTableSpec.Any(x => x.ColumnName.StartsWith($"{tableName}_{newColumn.ColumnName}")))
        {
            var highest = _entityTableSpec
                .Where(x => x.ColumnName.StartsWith($"{tableName}_{newColumn.ColumnName}"))
                .Select(x => Int32.Parse(x.ColumnName.Split('_')[2])).MaxBy(x => x);
            counter = highest + 1;
        }

        var internalColumnName = $"{tableName}_{newColumn.ColumnName}_{counter}";

        var insertQuery = $"ALTER TABLE [dbo].[{tableName}] ADD [{internalColumnName}] {datatype};";

        if (newColumn is { ColumnValueType: ValueTypes.RELATION, IsMultiSelect: false })
        {
            if (newColumn.ReferenceColumn is null || newColumn.ReferenceTable is null)
                throw new ArgumentNullException(nameof(newColumn.ReferenceColumn), "Reference column and table must be specified");
            insertQuery += $"ALTER TABLE [dbo].[{tableName}] ADD CONSTRAINT [FK_{tableName}_{newColumn.ReferenceTable}] FOREIGN KEY ([{internalColumnName}]) REFERENCES [dbo].[{newColumn.ReferenceTable}] ([{newColumn.ReferenceColumn.ColumnName}]);";
        }
        else if(newColumn is {ColumnValueType: ValueTypes.RELATION, IsMultiSelect: true})
        {
            insertQuery += $@"
CREATE TABLE dbo.[{SelectedTable.ToLower()}_{newColumn.ReferenceTable.ToLower()}] (
    [Id] UniqueIdentifier DEFAULT NEWID() NOT NULL PRIMARY KEY ,
    [{internalColumnName}_{newColumn.ReferenceTable.ToLower()}_Id] UniqueIdentifier NOT NULL,
    [{internalColumnName}_{SelectedTable.ToLower()}_Id] UniqueIdentifier NOT NULL
);
ALTER TABLE dbo.[{SelectedTable.ToLower()}_{newColumn.ReferenceTable.ToLower()}] ADD CONSTRAINT [FK_{tableName}_{newColumn.ReferenceTable}_1] FOREIGN KEY ([{internalColumnName}_{newColumn.ReferenceTable.ToLower()}_Id]) REFERENCES [dbo].[{newColumn.ReferenceTable}] ([{newColumn.ReferenceColumn.ColumnName}]);
ALTER TABLE dbo.[{SelectedTable.ToLower()}_{newColumn.ReferenceTable.ToLower()}] ADD CONSTRAINT [FK_{tableName}_{newColumn.ReferenceTable}_2] FOREIGN KEY ([{internalColumnName}_{SelectedTable.ToLower()}_Id]) REFERENCES [dbo].[{tableName}] ([{tableName}_Id_1]);
";
        }

        var specQuery = $"INSERT INTO [dbo].[{tableName}Spec] (ColumnName, ColumnDisplayName, ColumnValueType, ColumnEnabled, ValueEditable, ColumnEditable, ColumnVisible, IsMultiSelect, IsDropdown) VALUES (@internalColumnName, @columnName, @columnValueType, 1, 1, 1, 1, @isMultiSelect, @isDropdown);";
        try
        {
            command.CommandText = insertQuery;
            command.ExecuteNonQuery();

            command.CommandText = specQuery;
            command.Parameters.AddWithValue("internalColumnName", internalColumnName);
            command.Parameters.AddWithValue("columnName", newColumn.ColumnName);
            command.Parameters.AddWithValue("columnValueType", newColumn.ColumnValueType);
            command.Parameters.AddWithValue("isMultiSelect", newColumn.IsMultiSelect);
            command.Parameters.AddWithValue("isDropdown", newColumn.IsDropdown);
            command.ExecuteNonQuery();

            if (newColumn.IsDropdown)
            {
                var options = newColumn.DropdownOptions.Split(',').Select(x => x.Trim());
                var optionValues = string.Join(", ", options.Select(x => $"('{internalColumnName}', '{x}')"));
                var optionQuery = $"INSERT INTO [dbo].[DropdownOptions] (Internal_Column_Name, Value) VALUES " + optionValues;
                command.CommandText = optionQuery;
                command.ExecuteNonQuery();
            }
            
            transaction.Commit();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            transaction.Rollback();
        }
        finally
        {
            connection.Close();
            _entityTableSpec = GetEntityTableSpec();
        }
    }

    private (string?, string?, string?) GetJoinTableName(string internalColumnName)
    {
                var query = $@"

WITH PotentialJoinTables AS (
    SELECT 
        OBJECT_NAME(f.parent_object_id) AS TableName
    FROM 
        sys.foreign_keys AS f
    WHERE 
        OBJECT_NAME(f.referenced_object_id) = '{SelectedTable}' 
        AND SCHEMA_NAME(f.schema_id) = 'dbo'
),

JoinTableReferences AS (
    SELECT 
        jt.TableName AS JoinTableName,
        COL_NAME(fc.parent_object_id, fc.parent_column_id) AS ColumnName,
        OBJECT_NAME(f.referenced_object_id) AS ReferenceTableName,
        COL_NAME(fc.referenced_object_id, fc.referenced_column_id) AS ReferenceColumnName,
        f.name AS ForeignKeyName
    FROM 
        PotentialJoinTables jt
    JOIN 
        sys.foreign_keys AS f
        ON jt.TableName = OBJECT_NAME(f.parent_object_id)
    JOIN 
        sys.foreign_key_columns AS fc 
        ON f.OBJECT_ID = fc.constraint_object_id
    WHERE 
         SCHEMA_NAME(f.schema_id) = 'dbo'
        AND COL_NAME(fc.parent_object_id, fc.parent_column_id) like @columnName
)
SELECT 
    JoinTableName AS TableName,
    ColumnName,
    ReferenceTableName,
    ReferenceColumnName,
    ForeignKeyName
FROM JoinTableReferences
ORDER BY 
    TableName, 
    ColumnName, 
    ReferenceTableName, 
    ReferenceColumnName;

";

        var constraintResult = _sqlConnector.GetList(query, new Dictionary<string, object>()
        {
            { "columnName", $"{internalColumnName}%" }
        });
        if (constraintResult is null) throw new Exception();
        
        var tableName = constraintResult.First()["TableName"] as string;
        string[] columns = constraintResult.Select(x=> x["ColumnName"] as string).ToArray();
        var el1 = columns.First(x => x.Contains($"_{SelectedTable.ToLower()}_"));
        var el2 = columns.First(x => !x.Contains($"_{SelectedTable.ToLower()}_"));
        return (tableName,el1,el2);
    }
}