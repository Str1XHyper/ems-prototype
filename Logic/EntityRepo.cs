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
                    case "ColumnEnabled" when kvp.Value is bool ColumnEnabled:
                        descriptor.ColumnEnabled = ColumnEnabled;
                        break;
                }
            }
            colDescriptors.Add(descriptor);
        }
        return colDescriptors;
    }

    public List<Dictionary<string, object>> GetAllEntities()
    {
        var selectedColumns = string.Join(", ", _entityTableSpec.Select(x => $"[{x.ColumnName.Split('_')[1]}]"));
        var query = $"SELECT {selectedColumns} FROM [dbo].[Entity]";
        var result =_sqlConnector.GetList(query);
        return result;
    }
    
    public List<Dictionary<string, object>> GetAllEntitiesAsOfDateTime(DateTime dateTime)
    {
        var parameters = new Dictionary<string, object>();
        parameters.Add("dateTime",dateTime.ToUniversalTime().ToString("u").TrimEnd('Z'));
        var selectedColumns = string.Join(", ", _entityTableSpecAsOfSelected.Select(x => $"[{x.ColumnName.Split('_')[1]}]"));
        var query = $"SELECT {selectedColumns} FROM [dbo].[Entity]For System_Time As Of @dateTime";
        var result =_sqlConnector.GetList(query,parameters);
        return result;
    }
    
    public void AddColumn(string columnName, ValueTypes columnValueType)
    {
        var datatype = columnValueType switch
        {
            ValueTypes.NUMBER => "int",
            ValueTypes.STRING => "nvarchar(100)",
            ValueTypes.DATE => "datetime2(7)",
            ValueTypes.BOOLEAN => "bit",
            ValueTypes.GUID => "uniqueidentifier",
        };
        var internalColumnName = $"entity_{columnName}_1";
        
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

        var insertQuery = $"ALTER TABLE [dbo].[Entity] ADD [{internalColumnName}] {datatype}";
        const string specQuery = "INSERT INTO [dbo].[EntitySpec] (ColumnName, ColumnDisplayName, ColumnValueType, ColumnEnabled) VALUES (@internalColumnName, @columnName, @columnValueType, 1)";
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

    public IEnumerable<ColDescriptor> GetTableSpec()
    {
        using var connection = _sqlConnector.SqlConnection;
        connection.Open();
        var query = "SELECT * FROM [dbo].[EntitySpec] ORDER BY ValidFrom";
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
                    case "ColumnEnabled" when kvp.Value is bool ColumnEnabled:
                        descriptor.ColumnEnabled = ColumnEnabled;
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
                command.CommandText = "Update [dbo].[EntitySpec] SET ColumnEnabled = @columnEnabled, ColumnDisplayName = @columnDisplayName WHERE Id = @id";
                command.Parameters.AddWithValue("id", selectedColDescriptor.DescriptorId);
                command.Parameters.AddWithValue("columnEnabled", selectedColDescriptor.ColumnEnabled);
                command.Parameters.AddWithValue("columnDisplayName", selectedColDescriptor.ColumnDisplayName);

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
                };
                var internalColumnName = $"entity_{selectedColDescriptor.ColumnDisplayName}_1";
                command.CommandText = "UPDATE [dbo].[EntitySpec] SET ColumnEnabled = 0 WHERE Id = @id";
                command.Parameters.AddWithValue("id", selectedColDescriptor.DescriptorId);
                command.ExecuteNonQuery();
                command.CommandText = "Insert Into [dbo].[EntitySpec] (ColumnName, ColumnDisplayName, ColumnValueType, ColumnEnabled) VALUES (@internalColumnName, @columnName, @columnValueType, 1)";
                command.Parameters.AddWithValue("internalColumnName", internalColumnName);
                command.Parameters.AddWithValue("columnValueType", selectedColDescriptor.ColumnValueType);
                command.Parameters.AddWithValue("columnName", selectedColDescriptor.ColumnDisplayName);
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
}