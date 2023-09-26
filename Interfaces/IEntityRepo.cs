using Models.Enums;
using Models.Structs;

namespace Interfaces;

public interface IEntityRepo
{
    List<Dictionary<ColDescriptor, object>> GetAllEntities();
    List<Dictionary<ColDescriptor, object>> GetAllEntitiesAsOfDateTime(DateTime dateTime);
    void UpdateDateSelectedTime(DateTime time);
    DateTime SelectedDateTime { get; }
    void AddColumn(string columnName, ValueTypes columnValueType);
    IEnumerable<ColDescriptor> GetTableSpec();
    void RemoveColumn(Guid id);
    void UpdateColumn(ColDescriptor selectedColDescriptor);
    void UpdateRow(Dictionary<ColDescriptor, object> row);
    List<Dictionary<ColDescriptor, object>> GetRangeOfEntities(int start, int end);
    int GetTotalEntityCount();
    List<Dictionary<ColDescriptor, object>> GetRangeOfEntitiesBySelectedDate(int requestStartIndex, int requestCount);
    int GetEntityCountByDate();
}