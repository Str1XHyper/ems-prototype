using Models.Enums;
using Models.Structs;

namespace Interfaces;

public interface IEntityRepo
{
    List<Dictionary<string, object>> GetAllEntities();
    List<Dictionary<string, object>> GetAllEntitiesAsOfDateTime(DateTime dateTime);
    void UpdateDateSelectedTime(DateTime time);
    DateTime SelectedDateTime { get; }
    void AddColumn(string columnName, ValueTypes columnValueType);
    IEnumerable<ColDescriptor> GetTableSpec();
    void RemoveColumn(Guid id);
    void UpdateColumn(ColDescriptor selectedColDescriptor);
}