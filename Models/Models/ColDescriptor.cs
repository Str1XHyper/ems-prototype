using Models.Enums;

namespace Models.Models;

public class ColDescriptor
{
    public Guid DescriptorId;
    public string ColumnName;
    public string ColumnDisplayName;
    public ValueTypes ColumnValueType;
    public bool ColumnEnabled;
    public bool ColumnVisible;
    public bool ColumnEditable;
    public bool ValueEditable;
}
