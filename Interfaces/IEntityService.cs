using Models.Enums;
using Models.Structs;

namespace Interfaces;

public interface IEntityService
{
    public List<Dictionary<string, object>> GetAllEntities();
    List<Dictionary<string, object>> GetAllEntitiesByDate(DateTime dateTime);
    void UpdateDateSelectedTime(DateTime time);
    DateTime SelectedDateTime { get; set; }
    
    public void AddColumn(string columnName, ValueTypes columnValueType);
    IEnumerable<ColDescriptor> GetTableSpec();
    void RemoveColumn(Guid id);
    void UpdateColumn(ColDescriptor selectedColDescriptor);
}