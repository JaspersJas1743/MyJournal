using MyJournal.API.Assets.Controllers;

namespace MyJournal.API.Assets.Utilities.SubjectComparers;

public sealed class SubjectNumberComparer : IEqualityComparer<TimetableController.SubjectOnTimetable>
{
	public bool Equals(TimetableController.SubjectOnTimetable? x, TimetableController.SubjectOnTimetable? y)
	{
		if (ReferenceEquals(objA: x, objB: y))
			return true;

		if (ReferenceEquals(objA: x, objB: null))
			return false;

		if (ReferenceEquals(objA: y, objB: null))
			return false;

		if (x.GetType() != y.GetType())
			return false;

		return x.Number == y.Number;
	}

	public int GetHashCode(TimetableController.SubjectOnTimetable obj)
		=> HashCode.Combine(value1: obj.Number);
}