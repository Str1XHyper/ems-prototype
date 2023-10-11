using Interfaces;
using Models;
using Models.Enums;
using Models.Models;

namespace Logic;

public class EntityService : IEntityService
{
    private readonly IEntityRepo _entityRepo;

    public DateTime SelectedDateTime
    {
        get => _entityRepo.SelectedDateTime;
        set => _entityRepo.UpdateDateSelectedTime(value);
    }
    
    public string SelectedTable
    {
        get => _entityRepo.SelectedTable;
        set => _entityRepo.UpdateSelectedTable(value);
    }

    public IEnumerable<ColDescriptor> GetColumnsSelectableForRelations(string tableName)
    {
        return _entityRepo.GetColumnsSelectableForRelations(tableName);
    }

    public IEnumerable<string> GetAllTableNames()
    {
        return _entityRepo.GetAllTableNames();
    }

    public void AddRow(Dictionary<ColDescriptor, object> row)
    {
        _entityRepo.AddRow(row);
    }

    // public void AddColumn(string columnName, ValueTypes columnValueType, string? referenceTable = null, string? referenceColumn = null, bool? newColumnIsMultiSelect = false)
    // {
    //     _entityRepo.AddColumn(columnName, columnValueType, referenceTable, referenceColumn,newColumnIsMultiSelect);
    // }

    public IEnumerable<ColDescriptor> GetTableSpec()
    {
        return _entityRepo.GetTableSpec();
    }

    public void RemoveColumn(Guid id)
    {
        _entityRepo.RemoveColumn(id);;
    }

    public void UpdateColumn(ColDescriptor selectedColDescriptor)
    {
        _entityRepo.UpdateColumn(selectedColDescriptor);
    }

    public int GetEntityCount()
    {
        return _entityRepo.GetTotalEntityCount();
    }

    public List<Dictionary<ColDescriptor, object>> GetRangeOfEntities(int start, int end)
    {
        return _entityRepo.GetRangeOfEntities(start, end);
    }

    public List<Dictionary<ColDescriptor, object>> GetRangeOfEntitiesBySelectedDate(int requestStartIndex, int requestCount)
    {
        return _entityRepo.GetRangeOfEntitiesBySelectedDate(requestStartIndex, requestCount);
    }

    public int GetEntityCountByDate()
    {
        return _entityRepo.GetEntityCountByDate();
    }

    public void UpdateRow(Dictionary<ColDescriptor, object> row)
    {
        _entityRepo.UpdateRow(row);
    }

    public EntityService(IEntityRepo entityRepo)
    {
        _entityRepo = entityRepo;
    }

    public void UpdateDateSelectedTime(DateTime time)
    {
        _entityRepo.UpdateDateSelectedTime(time);
    }

    public IEnumerable<string> GetTableNames()
    {
        return _entityRepo.GetTableNames();
    }

    public IEnumerable<ColDescriptor> GetColumns(string value)
    {
        return _entityRepo.GetColumnsFromTable(value);
    }

    public List<Dictionary<ColDescriptor, object>> GetRelationData(ColDescriptor colDescriptor)
    {
        return _entityRepo.GetRelationData(colDescriptor);
    }

    public List<TableDescriptor> GetAllTables()
    {
        return _entityRepo.GetAllTables();
    }

    public void AddTable(TableDescriptor newTable)
    {
        _entityRepo.AddTable(newTable);
    }

    public void AddColumn(NewColumn columnName)
    {
        _entityRepo.AddColumn(columnName);
    }
}