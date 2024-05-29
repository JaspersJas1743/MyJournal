using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyJournal.Core.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Desktop.Assets.Utilities.NotificationService;

namespace MyJournal.Desktop.Assets.Utilities.MarksUtilities;

public sealed class TeacherSubjectCollection
{
	private readonly TaughtSubjectCollection? _taughtSubjectCollection;
	private readonly ClassCollection? _classCollection;
	private readonly IEnumerable<PossibleAssessment> _possibleAssessments;

	private TeacherSubjectCollection(
		IEnumerable<PossibleAssessment> possibleAssessments
	) => _possibleAssessments = possibleAssessments;

	public TeacherSubjectCollection(
		TaughtSubjectCollection taughtSubjectCollection,
		IEnumerable<PossibleAssessment> possibleAssessments
	) : this(possibleAssessments: possibleAssessments)
		=> _taughtSubjectCollection = taughtSubjectCollection;

	public TeacherSubjectCollection(
		ClassCollection classCollection,
		IEnumerable<PossibleAssessment> possibleAssessments
	) : this(possibleAssessments: possibleAssessments)
		=> _classCollection = classCollection;

	public async Task<IEnumerable<EducationPeriod>> GetEducationPeriods(int classId = 0)
	{
		if (_taughtSubjectCollection is not null)
			return await _taughtSubjectCollection.GetEducationPeriods();

		Core.SubEntities.Class @class = await _classCollection!.GetById(id: classId);
		StudyingSubjectInClassCollection studyingSubjects = await @class.GetStudyingSubjects();
		return await studyingSubjects.GetEducationPeriods();
	}

	public async Task<List<TeacherSubject>> ToListAsync(INotificationService notificationService)
	{
		if (_classCollection is not null)
		{
			return await _classCollection.SelectManyAwait(selector: async @class =>
			{
				StudyingSubjectInClassCollection a = await @class.GetStudyingSubjects();
				return a.Skip(count: 1).Select(selector: studyingSubjectInClass => new TeacherSubject(
					studyingSubjectInClass: studyingSubjectInClass,
					classId: @class.Id,
					className: @class.Name,
					possibleAssessments: _possibleAssessments,
					notificationService: notificationService
				));
			}).ToListAsync();
		}

		return new List<TeacherSubject>(collection: await Task.WhenAll(tasks: await _taughtSubjectCollection!.Skip(count: 1).Select(selector: async taughtSubject =>
		{
			TaughtClass @class = await taughtSubject.GetTaughtClass();
			TeacherSubject subject = new TeacherSubject(
				taughtSubject: taughtSubject,
				classId: @class.Id,
				className: @class.Name,
				possibleAssessments: _possibleAssessments,
				notificationService: notificationService
			);
			await subject.LoadClass();
			return subject;
		}).ToArrayAsync()));
	}
}