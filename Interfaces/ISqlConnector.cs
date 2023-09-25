using System.Data.SqlClient;

namespace Interfaces;

public interface ISqlConnector
{
    List<Dictionary<string, object>> GetList(string query,Dictionary<string, object>? parameters = null);
    Dictionary<string, object>? GetRow(string query,Dictionary<string, object>? parameters = null);
    SqlConnection SqlConnection { get; }
}