using System;
using System.Reactive;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using MyJournal.Core.Builders.EstimationChanger;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.EventArgs;
using MyJournal.Desktop.Assets.Utilities.NotificationService;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Utilities.MarksUtilities;

public sealed class ObservableEstimationOfStudent : ReactiveObject
{
	private readonly EstimationOfStudent _estimation;
	private readonly INotificationService _notificationService;

	public ObservableEstimationOfStudent(
		EstimationOfStudent estimation,
		INotificationService notificationService
	)
	{
		_estimation = estimation;
		_notificationService = notificationService;

		_estimation.ChangedAssessment += OnChangedAssessment;

		DeleteEstimation = ReactiveCommand.CreateFromTask(execute: DeleteEstimationHandler);
	}

	private async Task DeleteEstimationHandler()
	{
		await Delete();

		await _notificationService.Show(
			title: "Успеваемость",
			content: "Отметка успешно удалена!",
			type: NotificationType.Success
		);
	}

	private void OnChangedAssessment(ChangedAssessmentEventArgs _)
	{
		foreach (PropertyInfo propertyInfo in typeof(EstimationOfStudent).GetProperties())
			this.RaisePropertyChanged(propertyName: propertyInfo.Name);
	}

	public int Id => _estimation.Id;
	public string Assessment => _estimation.Assessment;
	public DateTime CreatedAt => _estimation.CreatedAt;
	public string? Comment => _estimation.Comment;
	public string? Description => _estimation.Description;
	public GradeTypes GradeType => _estimation.GradeType;
	public Estimation? Observable => _estimation;

	public ReactiveCommand<Unit, Unit> DeleteEstimation { get; }

	public async Task Delete()
		=> await _estimation.Delete();

	public IEstimationChanger Change()
		=> _estimation.Change();
}

public static class ObservableEstimationOfStudentExtensions
{
	public static ObservableEstimationOfStudent ToObservable(
		this EstimationOfStudent estimation,
		INotificationService notificationService
	)
	{
		return new ObservableEstimationOfStudent(
			estimation: estimation,
			notificationService: notificationService
		);
	}
}