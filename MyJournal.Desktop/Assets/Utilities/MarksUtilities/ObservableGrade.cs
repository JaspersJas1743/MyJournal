using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyJournal.Core.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.EventArgs;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Utilities.MarksUtilities;

public sealed class ObservableGrade : ReactiveObject
{
	private readonly Grade<Estimation> _grade;

	public ObservableGrade(Grade<Estimation> grade)
	{
		_grade = grade;

		_grade.ChangedAssessment += OnChangedAssessment;
		_grade.CreatedAssessment += OnCreatedAssessment;
		_grade.DeletedAssessment += OnDeletedAssessment;
		_grade.CreatedFinalAssessment += OnCreatedFinalAssessment;
	}

	private void OnCreatedFinalAssessment(CreatedFinalAssessmentEventArgs _)
	{
		this.RaisePropertyChanged(propertyName: nameof(FinalAssessment));
	}

	private void OnDeletedAssessment(DeletedAssessmentEventArgs _)
	{
		this.RaisePropertyChanged(propertyName: nameof(AverageAssessment));
	}

	private void OnCreatedAssessment(CreatedAssessmentEventArgs _)
	{
		this.RaisePropertyChanged(propertyName: nameof(AverageAssessment));
	}

	private void OnChangedAssessment(ChangedAssessmentEventArgs _)
	{
		this.RaisePropertyChanged(propertyName: nameof(AverageAssessment));
	}

	public string? FinalAssessment => _grade.FinalAssessment;
	public string AverageAssessment => _grade.AverageAssessment;

	public async Task SetEducationPeriod(int educationPeriodId)
		=> await _grade.SetEducationPeriod(educationPeriodId: educationPeriodId);

	public async Task<IEnumerable<ObservableEstimation>> GetEstimations()
	{
		IEnumerable<Estimation> estimations = await _grade.GetEstimations();
		return estimations.Select(selector: e => e.ToObservable());
	}

	public Grade<Estimation>? Observable => _grade;
}

public static class ObservableCreatedTaskExtensions
{
	public static ObservableGrade ToObservable(this Grade<Estimation> grade)
	{
		ObservableGrade observableGrade = new ObservableGrade(grade: grade);
		return observableGrade;
	}
}