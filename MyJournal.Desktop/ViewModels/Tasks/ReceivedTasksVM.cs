using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia.Controls.Selection;
using DynamicData.Binding;
using MyJournal.Desktop.Assets.Utilities.ChatUtilities;
using MyJournal.Desktop.Assets.Utilities.TasksUtilities;
using MyJournal.Desktop.Models.Tasks;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Tasks;

public sealed class ReceivedTasksVM(ReceivedTasksModel model) : TasksVM(model: model)
{
	public ReactiveCommand<Unit, Unit> OnSubjectSelectionChanged => model.OnSubjectSelectionChanged;
	public ReactiveCommand<Unit, Unit> OnTaskCompletionStatusSelectionChanged => model.OnTaskCompletionStatusSelectionChanged;
	public ReactiveCommand<Unit, Unit> CloseAttachments => model.CloseAttachments;
	public ReactiveCommand<Unit, Unit> LoadTasks => model.LoadTasks;
	public ReactiveCommand<Unit, Unit> ClearTasks => model.ClearTasks;

	public ReadOnlyObservableCollection<StudentSubject> StudyingSubjects => model.StudyingSubjects;
	public ObservableCollectionExtended<ReceivedTaskCompletionStatus> EducationPeriods => model.TaskCompletionStatuses;
	public ObservableCollectionExtended<ObservableReceivedTask> Tasks => model.Tasks;
	public SelectionModel<StudentSubject> SubjectSelectionModel => model.SubjectSelectionModel;
	public ObservableCollectionExtended<ExtendedAttachment> Attachments => model.Attachments;

	public string? Filter
	{
		get => model.Filter;
		set => model.Filter = value;
	}

	public ReceivedTaskCompletionStatus SelectedStatus
	{
		get => model.SelectedStatus;
		set => model.SelectedStatus = value;
	}

	public bool ShowAttachments
	{
		get => model.ShowAttachments;
		set => model.ShowAttachments = value;
	}

	public bool AllTasksSelected
	{
		get => model.AllTasksSelected;
		set => model.AllTasksSelected = value;
	}

	public bool ExpiredTasksSelected
	{
		get => model.ExpiredTasksSelected;
		set => model.ExpiredTasksSelected = value;
	}

	public bool UncompletedTasksSelected
	{
		get => model.UncompletedTasksSelected;
		set => model.UncompletedTasksSelected = value;
	}

	public bool CompletedTasksSelected
	{
		get => model.CompletedTasksSelected;
		set => model.CompletedTasksSelected = value;
	}
}