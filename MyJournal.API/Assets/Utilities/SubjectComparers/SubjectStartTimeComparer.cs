using MyJournal.API.Assets.Controllers;

namespace MyJournal.API.Assets.Utilities.SubjectComparers;

public sealed class SubjectStartTimeComparer : IEqualityComparer<TimetableController.SubjectOnTimetable>
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

		return x.Start == y.Start;
	}

	public int GetHashCode(TimetableController.SubjectOnTimetable obj)
		=> HashCode.Combine(value1: obj.Start);
}