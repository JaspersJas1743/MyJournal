using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyJournal.Core.SubEntities;
using MyJournal.Desktop.Assets.Utilities.NotificationService;

namespace MyJournal.Desktop.Assets.Utilities.MarksUtilities;

public sealed class TeacherSubject : TeacherSubjectBase
{
	private readonly INotificationService _notificationService;
	private readonly TaughtSubject? _taughtSubject;
	private readonly StudyingSubjectInClass? _studyingSubjectInClass;
	private readonly IEnumerable<PossibleAssessment> _possibleAssessments;
	private TaughtClass? _taughtClass;

	public TeacherSubject(
		int classId,
		string? className,
		IEnumerable<PossibleAssessment> possibleAssessments,
		INotificationService notificationService
	)
	{
		ClassName = className;
		ClassId = classId;
		_possibleAssessments = possibleAssessments;
		_notificationService = notificationService;
	}

	public TeacherSubject(
		TaughtSubject taughtSubject,
		int classId,
		string? className,
		IEnumerable<PossibleAssessment> possibleAssessments,
		INotificationService notificationService
	) : this(
		classId: classId,
		className: className,
		possibleAssessments: possibleAssessments,
		notificationService: notificationService
	)
	{
		_taughtSubject = taughtSubject;
		Id = taughtSubject.Id;
		Name = taughtSubject.Name;
	}

	public TeacherSubject(
		StudyingSubjectInClass studyingSubjectInClass,
		int classId,
		string? className,
		IEnumerable<PossibleAssessment> possibleAssessments,
		INotificationService notificationService
	) : this(
		classId: classId,
		className: className,
		possibleAssessments: possibleAssessments,
		notificationService: notificationService
	)
	{
		_studyingSubjectInClass = studyingSubjectInClass;

		Id = studyingSubjectInClass.Id;
		Name = studyingSubjectInClass.Name;
	}

	public async Task LoadClass()
	{
		_taughtClass = await _taughtSubject?.GetTaughtClass()!;
		ClassName = _taughtClass.Name;
	}

	public async Task SetEducationPeriod(int educationPeriodId)
	{
		if (_taughtClass is null)
			return;

		await _taughtClass.SetEducationPeriod(educationPeriodId: educationPeriodId);
	}

	public async Task SetAttendance(
		DateTime date,
		IEnumerable<Attendance> attendance
	)
	{
		if (_taughtClass is not null)
		{
			await _taughtClass.SetAttendance(date: date, attendance: attendance.Select(
				selector: a => new TaughtClass.Attendance(
					 StudentId: a.StudentId,
					 IsPresent: a.IsAttend,
					 CommentId: a.CommentId
				)
			));
			return;
		}

		await _studyingSubjectInClass!.SetAttendance(date: date, attendance: attendance.Select(
			selector: a => new StudyingSubjectInClass.Attendance(
				StudentId: a.StudentId,
				IsPresent: a.IsAttend,
				CommentId: a.CommentId
			)
		));
	}

	public async Task<IEnumerable<ObservableStudent>> GetClass()
	{
		if (_taughtSubject is not null)
		{
			TaughtClass taughtClass = await _taughtSubject.GetTaughtClass();
			IEnumerable<StudentInTaughtClass> studentsInTaughtClass = taughtClass.Students;
			return await Task.WhenAll(tasks: studentsInTaughtClass.Select(selector: async (s, i) =>
			{
				ObservableStudent observable = s.ToObservable(
					position: i + 1,
					possibleAssessments: _possibleAssessments,
					notificationService: _notificationService
				);
				await observable.LoadGrade();
				return observable;
			}));
		}

		IEnumerable<StudentOfSubjectInClass> studentsOfSubjectInClass = await _studyingSubjectInClass!.GetStudents();
		return await Task.WhenAll(tasks: studentsOfSubjectInClass.Select(selector: async (s, i) =>
		{
			ObservableStudent observable = s.ToObservable(
				position: i + 1,
				possibleAssessments: _possibleAssessments,
				notificationService: _notificationService
			);
			await observable.LoadGrade();
			return observable;
		}));
	}
}