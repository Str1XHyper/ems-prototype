using Models.Enums;
using Models.Structs;

namespace Interfaces;

public interface IEntityService
{
    public List<Dictionary<ColDescriptor, object>> GetAllEntities();
    List<Dictionary<ColDescriptor, object>> GetAllEntitiesByDate(DateTime dateTime);
    void UpdateDateSelectedTime(DateTime time);
    DateTime SelectedDateTime { get; set; }
    
    public void AddColumn(string columnName, ValueTypes columnValueType);
    IEnumerable<ColDescriptor> GetTableSpec();
    void RemoveColumn(Guid id);
    void UpdateColumn(ColDescriptor selectedColDescriptor);
    void UpdateRow(Dictionary<ColDescriptor, object> row);
    int GetEntityCount();
    List<Dictionary<ColDescriptor, object>> GetRangeOfEntities(int start, int end);
    List<Dictionary<ColDescriptor, object>> GetRangeOfEntitiesBySelectedDate(int requestStartIndex, int requestCount);
    int GetEntityCountByDate();
}