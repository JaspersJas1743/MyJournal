using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyJournal.Core.SubEntities;

namespace MyJournal.Desktop.Assets.Utilities.TimetableUtilities;

public sealed class Subject
{
	private readonly StudyingSubject? _studyingSubject;
	private readonly WardSubjectStudying? _wardSubjectStudying;
	private readonly TaughtSubject? _taughtSubject;

	public Subject(int id, string? name)
	{
		Id = id;
		Name = name;
	}

	public Subject(int id, string? name, SubjectTeacher? teacher) : this(
		id: id,
		name: name
	) => Teacher = teacher;

	public Subject(StudyingSubject studyingSubject) : this(
		id: studyingSubject.Id,
		name: studyingSubject.Name,
		teacher: studyingSubject.Teacher
	) => _studyingSubject = studyingSubject;

	public Subject(WardSubjectStudying wardSubjectStudying) : this(
		id: wardSubjectStudying.Id,
		name: wardSubjectStudying.Name,
		teacher: wardSubjectStudying.Teacher
	) => _wardSubjectStudying = wardSubjectStudying;

	public Subject(TaughtSubject taughtSubject, int classId, string? className) : this(
		id: taughtSubject.Id,
		name: taughtSubject.Name
	)
	{
		_taughtSubject = taughtSubject;
		ClassId = classId;
		ClassName = className;
	}

	public int Id { get; init; }
	public string? Name { get; init; } = null;
	public int? ClassId { get; init; } = null;
	public string? ClassName { get; init; } = null;
	public SubjectTeacher? Teacher { get; init; } = null;

	public async Task<IEnumerable<Timetable>> GetTimetable()
	{
		if (_taughtSubject is not null)
		{
			IEnumerable<TimetableForTeacher> teacherTimetable = await _taughtSubject.GetTimetable();
			return teacherTimetable.Select(selector: t => new Timetable(timetableForTeacher: t));
		}

		IEnumerable<TimetableForStudent> timetable;
		timetable = _studyingSubject is not null ?
			await _studyingSubject.GetTimetable() :
			await _wardSubjectStudying!.GetTimetable();

		return timetable.Select(selector: t => new Timetable(timetableForStudent: t));
	}
}