﻿using Models.Enums;

namespace Models.Structs;

public class ColDescriptor
{
    public Guid DescriptorId;
    public string ColumnName;
    public string ColumnDisplayName;
    public ValueTypes ColumnValueType;
    public bool ColumnEnabled;
}