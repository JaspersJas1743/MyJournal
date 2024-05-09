using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia.Controls.Selection;
using DynamicData.Binding;
using MyJournal.Core.Collections;
using MyJournal.Core.SubEntities;
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

	public ReadOnlyObservableCollection<StudyingSubject> StudyingSubjects => model.StudyingSubjects;
	public ObservableCollectionExtended<AssignedTaskCollection.AssignedTaskCompletionStatus> EducationPeriods => model.TaskCompletionStatuses;
	public ObservableCollectionExtended<ObservableAssignedTask> Tasks => model.Tasks;
	public SelectionModel<StudyingSubject> SubjectSelectionModel => model.SubjectSelectionModel;
	public ObservableCollectionExtended<ExtendedAttachment> Attachments => model.Attachments;

	public string? Filter
	{
		get => model.Filter;
		set => model.Filter = value;
	}

	public AssignedTaskCollection.AssignedTaskCompletionStatus SelectedStatus
	{
		get => model.SelectedStatus;
		set => model.SelectedStatus = value;
	}

	public bool ShowAttachments
	{
		get => model.ShowAttachments;
		set => model.ShowAttachments = value;
	}
}