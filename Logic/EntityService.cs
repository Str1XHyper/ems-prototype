using Interfaces;
using Models.Enums;
using Models.Structs;

namespace Logic;

public class EntityService : IEntityService
{
    private readonly IEntityRepo _entityRepo;

    public DateTime SelectedDateTime
    {
        get => _entityRepo.SelectedDateTime;
        set => _entityRepo.UpdateDateSelectedTime(value);
    }

    public void AddColumn(string columnName, ValueTypes columnValueType)
    {
        _entityRepo.AddColumn(columnName, columnValueType);
    }

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

    public EntityService(IEntityRepo entityRepo)
    {
        _entityRepo = entityRepo;
    }

    public List<Dictionary<string, object>> GetAllEntities()
    {
        return _entityRepo.GetAllEntities();
    }
    
    public List<Dictionary<string, object>> GetAllEntitiesByDate(DateTime dateTime)
    {
        return _entityRepo.GetAllEntitiesAsOfDateTime(dateTime);
    }

    public void UpdateDateSelectedTime(DateTime time)
    {
        _entityRepo.UpdateDateSelectedTime(time);
    }
}