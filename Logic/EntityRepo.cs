using System.Data;
using System.Data.SqlClient;
using Interfaces;
using Models.Enums;
using Models.Structs;

namespace Logic;

public class EntityRepo:IEntityRepo
{
    private readonly ISqlConnector _sqlConnector;
    IEnumerable<ColDescriptor> _entityTableSpec;
    
    public DateTime SelectedDateTime { get; private set; } = DateTime.Now;
    private IEnumerable<ColDescriptor> _entityTableSpecAsOfSelected;

    public EntityRepo(ISqlConnector sqlConnector)
    {
        _sqlConnector = sqlConnector;
        _entityTableSpec = GetEntityTableSpec();
        _entityTableSpecAsOfSelected = GetEntityTableSpec(SelectedDateTime);
    }

    public IEnumerable<string> GetTableNames()
    {
        using var connection = _sqlConnector.SqlConnection;
        connection.Open();
        var query = "SELECT * FROM sys.Tables WHERE is_ms_shipped = 0 AND history_table_id IS NOT NULL AND name NOT LIKE '%Spec'";
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

    public IEnumerable<ColDescriptor> GetColumnsFromTable(string tableName)
    {
        using var connection = _sqlConnector.SqlConnection;
        connection.Open();
        var query =  $"SELECT * FROM [dbo].[{tableName}Spec] WHERE ColumnEnabled = 1";
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
                }
            }
            colDescriptors.Add(descriptor);
        }
        return colDescriptors;
    }

    public List<Dictionary<ColDescriptor, object>> GetRelationData(ColDescriptor colDescriptor)
    {
        var query = $@"
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
    OBJECT_NAME(f.parent_object_id) = 'Entity' 
    AND SCHEMA_NAME(f.schema_id) = 'dbo'
    AND COL_NAME(fc.parent_object_id, fc.parent_column_id) = @columnName
ORDER BY 
    TableName, 
    ColumnName, 
    ReferenceTableName, 
    ReferenceColumnName;";
        
        var constraintResult = _sqlConnector.GetRow(query, new Dictionary<string, object>()
        {
            {"columnName", colDescriptor.ColumnName}
        });
        if (constraintResult is null) throw new Exception();
        var otherTableSpec = GetTableSpec(constraintResult["ReferenceTableName"] as string ?? "");
        var otherTableSelectedColumns = string.Join(", ", otherTableSpec.Select(x => x.ColumnName));

        var dataQuery = $"SELECT {otherTableSelectedColumns} FROM dbo.[{constraintResult["ReferenceTableName"]}]";
        var result = _sqlConnector.GetList(dataQuery);
        var rows = result.Select(resultRow => resultRow.ToDictionary(col => otherTableSpec.First(x => x.ColumnName == col.Key), col => col.Value)).ToList();
        return rows;
    }

    public void UpdateDateSelectedTime(DateTime time)
    {
        SelectedDateTime = time;
        _entityTableSpecAsOfSelected = GetEntityTableSpec(SelectedDateTime);
    }


    private IEnumerable<ColDescriptor> GetEntityTableSpec(DateTime? asofDateTime = null)
    {
        using var connection = _sqlConnector.SqlConnection;
        connection.Open();
        var query = asofDateTime is not null ? 
            "SELECT * FROM [dbo].[EntitySpec] FOR SYSTEM_TIME AS OF @dateTime WHERE ColumnEnabled = 1 ORDER BY ValidFrom" : 
            "SELECT * FROM [dbo].[EntitySpec] WHERE ColumnEnabled = 1  ORDER BY ValidFrom";
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
                }
            }
            colDescriptors.Add(descriptor);
        }
        return colDescriptors;
    }

    public int GetTotalEntityCount()
    {
        var query = "SELECT Count([entity_Id_1]) FROM [dbo].[Entity]";
        
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
            var query = $"WITH Records as (SELECT *, ROW_NUMBER() OVER ( ORDER BY ValidFrom) AS 'RowNumber' FROM [dbo].[Entity] For System_Time As Of @dateTime) Select {selectedColumns} From Records WHERE RowNumber between {requestStartIndex} and {requestCount}  ORDER BY ValidFrom ";
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
        var query = "SELECT Count([entity_Id_1]) FROM [dbo].[Entity] For System_Time As Of @dateTime";
        
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
        var selectedColumns = string.Join(", ", _entityTableSpec.Select(x => $"[{x.ColumnName}]"));
        var query = $"WITH Records as (SELECT *, ROW_NUMBER() OVER ( ORDER BY ValidFrom, entity_Name_1) AS 'RowNumber' FROM [dbo].[Entity]) Select {selectedColumns} From Records WHERE RowNumber between {start} and {end} ORDER BY ValidFrom";
        var result =_sqlConnector.GetList(query);

        var rows = result.Select(resultRow => resultRow.ToDictionary(col => _entityTableSpec.First(x => x.ColumnName == col.Key), col => col.Value)).ToList();

        return rows;
    }

    public List<Dictionary<ColDescriptor, object>> GetAllEntities()
    {
        var selectedColumns = string.Join(", ", _entityTableSpec.Select(x => $"[{x.ColumnName}]"));
        var query = $"WITH Records as (SELECT {selectedColumns}, ROW_NUMBER() OVER ( ORDER BY ValidFrom) AS 'RowNumber' FROM [dbo].[Entity]) Select * From Records WHERE RowNumber between 1 and 1000";
        var result =_sqlConnector.GetList(query);

        var rows = result.Select(resultRow => resultRow.SkipLast(1).ToDictionary(col => _entityTableSpec.First(x => x.ColumnName == col.Key), col => col.Value)).ToList();

        return rows;
    }
    
    public List<Dictionary<ColDescriptor, object>> GetAllEntitiesAsOfDateTime(DateTime dateTime)
    {
        var parameters = new Dictionary<string, object>();
        parameters.Add("dateTime",SelectedDateTime.ToUniversalTime().ToString("u").TrimEnd('Z'));
        var selectedColumns = string.Join(", ", _entityTableSpecAsOfSelected.Select(x => $"[{x.ColumnName}]"));
        var query = $"SELECT {selectedColumns} FROM [dbo].[Entity] For System_Time As Of @dateTime";
        var result =_sqlConnector.GetList(query,parameters);
        
        
        var rows = result.Select(resultRow => resultRow.ToDictionary(col => _entityTableSpecAsOfSelected.First(x => x.ColumnName == col.Key), col => col.Value)).ToList();
        return rows;
    }
    
    public void AddColumn(string columnName, ValueTypes columnValueType, string? referenceTable =null, string? referenceColumn = null )
    {
        var datatype = columnValueType switch
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
            _ => throw new ArgumentOutOfRangeException(nameof(columnValueType), columnValueType, null)
        };
        
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
        if (_entityTableSpec.Any(x => x.ColumnName.StartsWith($"entity_{columnName}")))
        {
            var highest = _entityTableSpec
                .Where(x => x.ColumnName.StartsWith($"entity_{columnName}"))
                .Select(x => Int32.Parse(x.ColumnName.Split('_')[2])).MaxBy(x => x);
            counter = highest + 1;
        }
        var internalColumnName = $"entity_{columnName}_{counter}";

        var insertQuery = $"ALTER TABLE [dbo].[Entity] ADD [{internalColumnName}] {datatype};";

        if (columnValueType == ValueTypes.RELATION)
        {
            if(referenceColumn is null || referenceTable is null)
                throw new ArgumentNullException(nameof(referenceColumn), "Reference column and table must be specified");
            insertQuery += $"ALTER TABLE [dbo].[Entity] ADD CONSTRAINT [FK_entity_{referenceTable}] FOREIGN KEY ([{internalColumnName}]) REFERENCES [dbo].[{referenceTable}] ([{referenceColumn}]);";
        }
        
        const string specQuery = "INSERT INTO [dbo].[EntitySpec] (ColumnName, ColumnDisplayName, ColumnValueType, ColumnEnabled, ValueEditable, ColumnEditable, ColumnVisible) VALUES (@internalColumnName, @columnName, @columnValueType, 1, 1, 1, 1);";
        try
        {
            command.CommandText = insertQuery;
            command.ExecuteNonQuery();
            
            command.CommandText = specQuery;
            command.Parameters.AddWithValue("internalColumnName", internalColumnName);
            command.Parameters.AddWithValue("columnName", columnName);
            command.Parameters.AddWithValue("columnValueType", columnValueType);
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

    public IEnumerable<ColDescriptor> GetTableSpec(string tableName = "Entity")
    {
        using var connection = _sqlConnector.SqlConnection;
        connection.Open();
        var query = $"SELECT * FROM [dbo].[{tableName}Spec] ORDER BY ValidFrom";
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
                }
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
            command.CommandText = "Update [dbo].[EntitySpec] SET ColumnEnabled = 0 WHERE Id = @id";
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
                command.CommandText = "Update [dbo].[EntitySpec] SET ColumnEnabled = @columnEnabled, ColumnDisplayName = @columnDisplayName, ColumnVisible = @columnVisible, ColumnEditable = @columnEditable, ValueEditable = @valueEditable WHERE Id = @id";
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
                if (spec.Any(x => x.ColumnName.StartsWith($"entity_{selectedColDescriptor.ColumnDisplayName}")))
                {
                    var highest = _entityTableSpec
                        .Where(x => x.ColumnName.StartsWith($"entity_{selectedColDescriptor.ColumnDisplayName}"))
                        .Select(x => Int32.Parse(x.ColumnName.Split('_')[2])).MaxBy(x => x);
                    counter = highest + 1;
                }
                var internalColumnName = $"entity_{selectedColDescriptor.ColumnDisplayName}_{counter}";
                command.CommandText = "UPDATE [dbo].[EntitySpec] SET ColumnEnabled = 0 WHERE Id = @id";
                command.Parameters.AddWithValue("id", selectedColDescriptor.DescriptorId);
                command.ExecuteNonQuery();
                command.CommandText = "Insert Into [dbo].[EntitySpec] (ColumnName, ColumnDisplayName, ColumnValueType, ColumnEnabled, ColumnEditable, ValueEditable) VALUES (@internalColumnName, @columnName, @columnValueType, 1, @columnEditable, @valueEditable)";
                command.Parameters.AddWithValue("internalColumnName", internalColumnName);
                command.Parameters.AddWithValue("columnValueType", selectedColDescriptor.ColumnValueType);
                command.Parameters.AddWithValue("columnName", selectedColDescriptor.ColumnDisplayName);
                command.Parameters.AddWithValue("columnVisible", selectedColDescriptor.ColumnVisible);
                command.Parameters.AddWithValue("columnEditable", selectedColDescriptor.ColumnEditable);
                command.Parameters.AddWithValue("valueEditable", selectedColDescriptor.ValueEditable);
                command.ExecuteNonQuery();
                command.CommandText= $"ALTER TABLE [dbo].[Entity] ADD [{internalColumnName}] {datatype}";
                
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
        var setters = row.Keys.Where(x => x.ColumnName != "entity_Id_1").Aggregate("", (current, key) => current + $"[{key.ColumnName}] = @{key.ColumnName.Replace(' ', '_')}, ");
        setters = setters.TrimEnd(',', ' ');
        var query = $"Update [dbo].[Entity] SET {setters} WHERE [entity_Id_1] = @entity_Id_1";
        
        
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
                command.Parameters.AddWithValue(kvp.Key.ColumnName.Replace(' ', '_'), kvp.Value);
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
}