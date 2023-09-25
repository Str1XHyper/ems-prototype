using System.Data.SqlClient;
using Interfaces;
using Microsoft.Extensions.Configuration;

namespace DB;

public class SqlConnector:ISqlConnector
{
    private readonly IConfiguration _configuration;

    public SqlConnector(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public SqlConnection SqlConnection => new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

    public Dictionary<string, object>? GetRow(string query,Dictionary<string, object>? parameters = null)
    {
        Dictionary<string, object> result = null;
        try
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();

            using var command = new SqlCommand(query, connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    result[reader.GetName(i)] = reader.GetValue(i);
                }
            }

            connection.Close();

        }
        catch
        {
            Console.WriteLine("Error");
        }

        return result;
    }

    public List<Dictionary<string, object>> GetList(string query,Dictionary<string, object>? parameters = null)
    {
        var resultList = new List<Dictionary<string, object>>();
        try
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();

            using var command = new SqlCommand(query, connection);
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value);
                }
            }
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.GetValue(i);
                }
                resultList.Add(row);
            }

            connection.Close();

        }
        catch
        {
            Console.WriteLine("Error");
        }

        return resultList;
    }
}