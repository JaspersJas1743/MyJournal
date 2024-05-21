using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyJournal.Core.Builders.EstimationBuilder;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.EventArgs;
using MyJournal.Desktop.Assets.Utilities.NotificationService;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Utilities.MarksUtilities;

public sealed class ObservableGradeOfStudent : ReactiveObject
{
	private readonly GradeOfStudent _gradeOfStudent;
	private readonly INotificationService _notificationService;

	public ObservableGradeOfStudent(
		GradeOfStudent gradeOfStudent,
		IEnumerable<PossibleAssessment> possibleAssessments,
		INotificationService notificationService
	)
	{
		_gradeOfStudent = gradeOfStudent;
		_notificationService = notificationService;

		_gradeOfStudent.ChangedAssessment += OnChangedAssessment;
		_gradeOfStudent.CreatedAssessment += OnCreatedAssessment;
		_gradeOfStudent.DeletedAssessment += OnDeletedAssessment;
		_gradeOfStudent.CreatedFinalAssessment += OnCreatedFinalAssessment;
	}

	public GradeOfStudent? Observable => _gradeOfStudent;
	public int? FinalAssessment => _gradeOfStudent.FinalAssessment;
	public string AverageAssessment => _gradeOfStudent.AverageAssessment;
	public IEnumerable<ObservableEstimationOfStudent> Estimations { get; private set; }

	public IEstimationBuilder Add()
		=> _gradeOfStudent.Add();

	private void OnCreatedFinalAssessment(CreatedFinalAssessmentEventArgs _)
		=> this.RaisePropertyChanged(propertyName: nameof(FinalAssessment));

	private async void OnDeletedAssessment(DeletedAssessmentEventArgs _)
	{
		this.RaisePropertyChanged(propertyName: nameof(AverageAssessment));
		await LoadEstimations();
		this.RaisePropertyChanged(propertyName: nameof(Estimations));
	}

	private async void OnCreatedAssessment(CreatedAssessmentEventArgs _)
	{
		this.RaisePropertyChanged(propertyName: nameof(AverageAssessment));
		await LoadEstimations();
		this.RaisePropertyChanged(propertyName: nameof(Estimations));
	}

	private async void OnChangedAssessment(ChangedAssessmentEventArgs _)
	{
		this.RaisePropertyChanged(propertyName: nameof(AverageAssessment));
		await LoadEstimations();
		this.RaisePropertyChanged(propertyName: nameof(Estimations));
	}

	public async Task SetEducationPeriod(int educationPeriodId)
	{
		await _gradeOfStudent.SetEducationPeriod(educationPeriodId: educationPeriodId);
		await LoadEstimations();
		this.RaisePropertyChanged(propertyName: nameof(AverageAssessment));
	}

	public async Task LoadEstimations()
	{
		IEnumerable<EstimationOfStudent> estimations = await _gradeOfStudent.GetEstimations();
		Estimations = estimations.Select(selector: e => e.ToObservable(notificationService: _notificationService));
	}
}

public static class ObservableCreatedTaskExtensions
{
	public static ObservableGradeOfStudent ToObservable(
		this GradeOfStudent gradeOfStudent,
		IEnumerable<PossibleAssessment> possibleAssessments,
		INotificationService notificationService
	)
	{
		return new ObservableGradeOfStudent(
			gradeOfStudent: gradeOfStudent,
			possibleAssessments: possibleAssessments,
			notificationService: notificationService
		);
	}
}