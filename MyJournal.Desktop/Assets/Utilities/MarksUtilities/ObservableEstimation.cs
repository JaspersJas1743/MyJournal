using System;
using System.Diagnostics;
using System.Reflection;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.EventArgs;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Utilities.MarksUtilities;

public sealed class ObservableEstimation : ReactiveObject
{
	private readonly Estimation _estimation;

	public ObservableEstimation(Estimation estimation)
	{
		_estimation = estimation;

		_estimation.ChangedAssessment += OnChangedAssessment;
	}

	private void OnChangedAssessment(ChangedAssessmentEventArgs _)
	{
		foreach (PropertyInfo propertyInfo in typeof(Estimation).GetProperties())
			this.RaisePropertyChanged(propertyName: propertyInfo.Name);
	}

	public int Id => _estimation.Id;
	public string Assessment => _estimation.Assessment;
	public DateTime CreatedAt => _estimation.CreatedAt;
	public string? Comment => _estimation.Comment;
	public string? Description => _estimation.Description;
	public GradeTypes GradeType => _estimation.GradeType;
	public Estimation? Observable => _estimation;
}

public static class ObservableEstimationExtensions
{
	public static ObservableEstimation ToObservable(this Estimation estimation)
		=> new ObservableEstimation(estimation: estimation);
}