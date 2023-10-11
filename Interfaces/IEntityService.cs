using Models;
using Models.Enums;
using Models.Models;

namespace Interfaces;

public interface IEntityService
{
    void UpdateDateSelectedTime(DateTime time);
    DateTime SelectedDateTime { get; set; }
    string SelectedTable { get; set; }

    // public void AddColumn(string columnName, ValueTypes columnValueType, string? referenceTable = null, string? referenceColumn = null, bool? newColumnIsMultiSelect = false);
    IEnumerable<ColDescriptor> GetTableSpec();
    void RemoveColumn(Guid id);
    void UpdateColumn(ColDescriptor selectedColDescriptor);
    void UpdateRow(Dictionary<ColDescriptor, object> row);
    int GetEntityCount();
    List<Dictionary<ColDescriptor, object>> GetRangeOfEntities(int start, int end);
    List<Dictionary<ColDescriptor, object>> GetRangeOfEntitiesBySelectedDate(int requestStartIndex, int requestCount);
    int GetEntityCountByDate();
    IEnumerable<string> GetTableNames();
    IEnumerable<ColDescriptor> GetColumns(string value);
    List<Dictionary<ColDescriptor, object>> GetRelationData(ColDescriptor colDescriptor);
    IEnumerable<ColDescriptor> GetColumnsSelectableForRelations(string tableName);
    IEnumerable<string> GetAllTableNames();
    void AddRow(Dictionary<ColDescriptor, object> row);
    List<TableDescriptor> GetAllTables();
    void AddTable(TableDescriptor newTable);
    void AddColumn(NewColumn columnName);
}