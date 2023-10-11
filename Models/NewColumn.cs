using System.ComponentModel.DataAnnotations;
using Models.Enums;
using Models.Models;

namespace Models;

public class NewColumn
{
    
    [Required]
    public string ColumnName { get; set; }

    public ValueTypes ColumnValueType { get; set; }

    public string? ReferenceTable { get; set; } = "";
    public ColDescriptor? ReferenceColumn { get; set; } = null;
    public Guid ReferenceColumnGuid { get; set; }
    public bool IsMultiSelect { get; set; }
    public bool IsDropdown { get; set; }
    public string DropdownOptions { get; set; } = "";
}