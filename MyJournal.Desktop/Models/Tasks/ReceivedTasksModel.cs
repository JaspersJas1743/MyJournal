using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Selection;
using Avalonia.Input;
using DynamicData;
using DynamicData.Binding;
using MyJournal.Core;
using MyJournal.Core.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.EventArgs;
using MyJournal.Desktop.Assets.Utilities.ChatUtilities;
using MyJournal.Desktop.Assets.Utilities.TasksUtilities;
using ReactiveUI;

namespace MyJournal.Desktop.Models.Tasks;

public sealed class ReceivedTasksModel : TasksModel
{
	private readonly ReadOnlyObservableCollection<StudyingSubject> _studyingSubjects;
	private readonly SourceCache<StudyingSubject, int> _studyingSubjectsCache = new SourceCache<StudyingSubject, int>(keySelector: s => s.Id);

	private StudyingSubjectCollection _studyingSubjectCollection;
	private AssignedTaskCollection? _assignedTaskCollection;
	private Student _student;
	private string? _filter = String.Empty;
	private AssignedTaskCollection.AssignedTaskCompletionStatus _selectedStatus = 0;
	private bool _showAttachments = false;
	private bool _allTasksSelected = false;
	private bool _expiredTasksSelected = false;
	private bool _uncompletedTasksSelected = false;
	private bool _completedTasksSelected = false;

	public ReceivedTasksModel()
	{
		OnSubjectSelectionChanged = ReactiveCommand.CreateFromTask(execute: SubjectSelectionChangedHandler);
		OnTaskCompletionStatusSelectionChanged = ReactiveCommand.CreateFromTask(execute: TaskCompletionStatusSelectionChangedHandler);
		CloseAttachments = ReactiveCommand.Create(execute: ClearAttachments);
		LoadTasks = ReactiveCommand.CreateFromTask(execute: LoadMoreTask);
		ClearTasks = ReactiveCommand.Create(execute: ClearSelection);

		IObservable<Func<StudyingSubject, bool>> filter = this.WhenAnyValue(model => model.Filter).WhereNotNull()
			.Throttle(dueTime: TimeSpan.FromSeconds(value: 0.25), scheduler: RxApp.TaskpoolScheduler)
			.Select(selector: FilterFunction);

		IObservable<SortExpressionComparer<StudyingSubject>> sort = this.WhenAnyValue(model => model.Filter).WhereNotNull()
			.Throttle(dueTime: TimeSpan.FromSeconds(value: 0.25), scheduler: RxApp.TaskpoolScheduler)
			.Select(selector: _ => SortExpressionComparer<StudyingSubject>.Ascending(expression: s => s.Teacher != null ? 1 : 0)
				.ThenByAscending(expression: s => s.Name!).ThenByAscending(expression: s => s.Teacher?.FullName ?? String.Empty));

		_ = _studyingSubjectsCache.Connect().RefCount().Filter(predicateChanged: filter).Sort(comparerObservable: sort)
			.Bind(readOnlyObservableCollection: out _studyingSubjects).DisposeMany().Subscribe();

		TaskCompletionStatuses.Load(items: Enum.GetValues<AssignedTaskCollection.AssignedTaskCompletionStatus>());
		SelectedStatus = 0;

		SubjectSelectionModel.LostSelection += OnSubjectSelectionLost;
	}

	public ReactiveCommand<Unit, Unit> OnSubjectSelectionChanged { get; }
	public ReactiveCommand<Unit, Unit> OnTaskCompletionStatusSelectionChanged { get; }
	public ReactiveCommand<Unit, Unit> CloseAttachments { get; }
	public ReactiveCommand<Unit, Unit> LoadTasks { get; }
	public ReactiveCommand<Unit, Unit> ClearTasks { get; }

	public SelectionModel<StudyingSubject> SubjectSelectionModel { get; } = new SelectionModel<StudyingSubject>();

	public ReadOnlyObservableCollection<StudyingSubject> StudyingSubjects => _studyingSubjects;

	public ObservableCollectionExtended<AssignedTaskCollection.AssignedTaskCompletionStatus> TaskCompletionStatuses { get; }
		= new ObservableCollectionExtended<AssignedTaskCollection.AssignedTaskCompletionStatus>();

	public ObservableCollectionExtended<ObservableAssignedTask> Tasks { get; }
		= new ObservableCollectionExtended<ObservableAssignedTask>();

	public ObservableCollectionExtended<ExtendedAttachment> Attachments { get; }
		= new ObservableCollectionExtended<ExtendedAttachment>();

	public AssignedTaskCollection.AssignedTaskCompletionStatus SelectedStatus
	{
		get => _selectedStatus;
		set => this.RaiseAndSetIfChanged(backingField: ref _selectedStatus, newValue: value);
	}

	public string? Filter
	{
		get => _filter;
		set => this.RaiseAndSetIfChanged(backingField: ref _filter, newValue: value);
	}

	public bool ShowAttachments
	{
		get => _showAttachments;
		set => this.RaiseAndSetIfChanged(backingField: ref _showAttachments, newValue: value);
	}

	public bool AllTasksSelected
	{
		get => _allTasksSelected;
		set => this.RaiseAndSetIfChanged(backingField: ref _allTasksSelected, newValue: value);
	}

	public bool ExpiredTasksSelected
	{
		get => _expiredTasksSelected;
		set => this.RaiseAndSetIfChanged(backingField: ref _expiredTasksSelected, newValue: value);
	}

	public bool UncompletedTasksSelected
	{
		get => _uncompletedTasksSelected;
		set => this.RaiseAndSetIfChanged(backingField: ref _uncompletedTasksSelected, newValue: value);
	}

	public bool CompletedTasksSelected
	{
		get => _completedTasksSelected;
		set => this.RaiseAndSetIfChanged(backingField: ref _completedTasksSelected, newValue: value);
	}

	private async Task LoadMoreTask()
	{
		if (_assignedTaskCollection!.AllItemsAreUploaded)
			return;

		int currentLength = _assignedTaskCollection.Length;
		await _assignedTaskCollection.LoadNext();
		IEnumerable<AssignedTask> tasks = await _assignedTaskCollection.GetByRange(start: currentLength, end: _assignedTaskCollection.Length);
		Tasks.Add(items: tasks.Select(selector: task => task.ToObservable(
			showLessonName: SubjectSelectionModel.SelectedItem!.Name!.Contains(value: "Все дисциплины"),
			showAttachments: GetShowAttachments(task: task)
		)));
	}

	private void ClearAttachments()
	{
		ShowAttachments = false;
		Attachments.Clear();
	}

	private void OnSubjectSelectionLost(object? sender, EventArgs e)
	{
		_assignedTaskCollection = null;
		Tasks.Clear();
	}

	public ReactiveCommand<Unit, Unit> GetShowAttachments(AssignedTask task)
	{
		return ReactiveCommand.Create(execute: () =>
		{
			ShowAttachments = true;
			Attachments.Load(items: task.Content.Attachments?.Select(selector: a => a.ToExtended())!);
		});
	}

	public Func<StudyingSubject, bool> FilterFunction(string? text)
	{
		return subject => subject.Name?.ToLower().StartsWith(value: text!, StringComparison.CurrentCultureIgnoreCase) == true ||
			subject.Teacher?.FullName.ToLower().StartsWith(value: text!, StringComparison.CurrentCultureIgnoreCase) == true;
	}

	private async Task TaskCompletionStatusSelectionChangedHandler()
	{
		if (_assignedTaskCollection is null || SubjectSelectionModel.SelectedItem is null)
			return;

		await _assignedTaskCollection.SetCompletionStatus(status: _selectedStatus);
		List<AssignedTask> tasks = await _assignedTaskCollection.ToListAsync();
		Tasks.Load(items: tasks.Select(selector: t => t.ToObservable(
			showLessonName: SubjectSelectionModel.SelectedItem.Name!.Contains(value: "Все дисциплины"),
			showAttachments: GetShowAttachments(task: t)
		)));
		SetTaskSelection();
	}

	private async Task SubjectSelectionChangedHandler()
	{
		_assignedTaskCollection = await SubjectSelectionModel.SelectedItem!.GetTasks();
		await _assignedTaskCollection.SetCompletionStatus(status: _selectedStatus);
		List<AssignedTask> tasks = await _assignedTaskCollection.ToListAsync();
		Tasks.Load(items: tasks.Select(selector: t => t.ToObservable(
			showLessonName: SubjectSelectionModel.SelectedItem.Name!.Contains(value: "Все дисциплины"),
			showAttachments: GetShowAttachments(task: t)
		)));
		SetTaskSelection();
	}

	private void SetTaskSelection()
	{
		AllTasksSelected = _selectedStatus == AssignedTaskCollection.AssignedTaskCompletionStatus.All;
		ExpiredTasksSelected = _selectedStatus == AssignedTaskCollection.AssignedTaskCompletionStatus.Expired;
		UncompletedTasksSelected = _selectedStatus == AssignedTaskCollection.AssignedTaskCompletionStatus.Uncompleted;
		CompletedTasksSelected = _selectedStatus == AssignedTaskCollection.AssignedTaskCompletionStatus.Completed;
	}

	private async void OnStudyingSubjectsCreatedTask(CreatedTaskEventArgs e)
	{
		if (_assignedTaskCollection is null)
			return;

		AssignedTask? task = await _assignedTaskCollection.FindById(id: e.TaskId);
		if (task is null)
			return;

		if (!Enum.TryParse(value: _selectedStatus.ToString(), out AssignedTask.TaskCompletionStatus taskStatus) &&
			_selectedStatus != AssignedTaskCollection.AssignedTaskCompletionStatus.All)
			return;

		if (task.CompletionStatus != taskStatus || _selectedStatus != AssignedTaskCollection.AssignedTaskCompletionStatus.All)
			return;

		Tasks.Add(item: task.ToObservable(
			showLessonName: SubjectSelectionModel.SelectedItem?.Name!.Contains(value: "Все дисциплины") == true,
			showAttachments: GetShowAttachments(task: task)
		));
	}

	private void ClearSelection()
	{
		SubjectSelectionModel.SelectedItem = null;
		Tasks.Clear();
	}

	public override async Task SetUser(User user)
	{
		_student = (user as Student)!;
		_studyingSubjectCollection = await _student.GetStudyingSubjects();
		List<StudyingSubject> subjects = await _studyingSubjectCollection.ToListAsync();
		_studyingSubjectsCache.Edit(updateAction: (a) => a.AddOrUpdate(items: subjects));

		_studyingSubjectCollection.CreatedTask += OnStudyingSubjectsCreatedTask;
	}
}