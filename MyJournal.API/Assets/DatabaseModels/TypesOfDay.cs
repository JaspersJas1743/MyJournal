using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public enum TypesOfDay
{
    WorkingDay,
    Weekend
}

public partial class TypeOfDay
{
    public int Id { get; set; }

    public TypesOfDay DayType { get; set; }

    public virtual ICollection<DaysOfWeek> DaysOfWeeks { get; set; } = new List<DaysOfWeek>();
}
