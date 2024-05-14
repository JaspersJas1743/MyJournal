using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia.Controls.Selection;
using DynamicData.Binding;
using MyJournal.Desktop.Assets.Utilities.ChatUtilities;
using MyJournal.Desktop.Assets.Utilities.TasksUtilities;
using MyJournal.Desktop.Models.Tasks;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Tasks;

public sealed class CreatedTasksVM(CreatedTasksModel model) : TasksVM(model: model)
{
	public ReactiveCommand<Unit, Unit> OnSubjectSelectionChanged => model.OnSubjectSelectionChanged;
	public ReactiveCommand<Unit, Unit> OnTaskCompletionStatusSelectionChanged => model.OnTaskCompletionStatusSelectionChanged;
	public ReactiveCommand<Unit, Unit> CloseAttachments => model.CloseAttachments;
	public ReactiveCommand<Unit, Unit> CloseCreatedAttachments => model.CloseCreatedAttachments;
	public ReactiveCommand<Unit, Unit> LoadTasks => model.LoadTasks;
	public ReactiveCommand<Unit, Unit> ClearTasks => model.ClearTasks;
	public ReactiveCommand<Unit, Unit> LoadAttachment => model.LoadAttachment;

	public ReadOnlyObservableCollection<TeacherSubject> StudyingSubjects => model.StudyingSubjects;
	public ObservableCollectionExtended<CreatedTaskCompletionStatus> EducationPeriods => model.TaskCompletionStatuses;
	public ObservableCollectionExtended<ObservableCreatedTask> Tasks => model.Tasks;
	public SelectionModel<TeacherSubject> SubjectSelectionModel => model.SubjectSelectionModel;
	public ObservableCollectionExtended<ExtendedAttachment> Attachments => model.Attachments;
	public ObservableCollectionExtended<Attachment> AttachmentsForCreatedTask => model.AttachmentsForCreatedTask;

	public string? Filter
	{
		get => model.Filter;
		set => model.Filter = value;
	}

	public CreatedTaskCompletionStatus SelectedStatus
	{
		get => model.SelectedStatus;
		set => model.SelectedStatus = value;
	}

	public bool ShowAttachments
	{
		get => model.ShowAttachments;
		set => model.ShowAttachments = value;
	}

	public bool ShowEditableAttachments
	{
		get => model.ShowEditableAttachments;
		set => model.ShowEditableAttachments = value;
	}

	public bool ShowTaskCreation
	{
		get => model.ShowTaskCreation;
		set => model.ShowTaskCreation = value;
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

	public bool NotExpiredTasksSelected
	{
		get => model.NotExpiredTasksSelected;
		set => model.NotExpiredTasksSelected = value;
	}
}