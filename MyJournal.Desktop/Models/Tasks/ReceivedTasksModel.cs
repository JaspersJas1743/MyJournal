using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Selection;
using DynamicData;
using DynamicData.Binding;
using MyJournal.Core;
using MyJournal.Core.Utilities.EventArgs;
using MyJournal.Desktop.Assets.Utilities.ChatUtilities;
using MyJournal.Desktop.Assets.Utilities.TasksUtilities;
using ReactiveUI;

namespace MyJournal.Desktop.Models.Tasks;

public sealed class ReceivedTasksModel : TasksModel
{
	private readonly ReadOnlyObservableCollection<StudentSubject> _studyingSubjects;
	private readonly SourceCache<StudentSubject, int> _studyingSubjectsCache = new SourceCache<StudentSubject, int>(keySelector: s => s.Id);
	private StudentSubjectCollection _studentSubjectCollection;
	private ReceivedTaskCollection? _taskCollection;
	private string? _filter = String.Empty;
	private TaskCompletionStatus _selectedStatus = 0;
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

		IObservable<Func<StudentSubject, bool>> filter = this.WhenAnyValue(model => model.Filter).WhereNotNull()
			.Throttle(dueTime: TimeSpan.FromSeconds(value: 0.25), scheduler: RxApp.TaskpoolScheduler)
			.Select(selector: FilterFunction);

		IObservable<SortExpressionComparer<StudentSubject>> sort = this.WhenAnyValue(model => model.Filter).WhereNotNull()
			.Throttle(dueTime: TimeSpan.FromSeconds(value: 0.25), scheduler: RxApp.TaskpoolScheduler)
			.Select(selector: _ => SortExpressionComparer<StudentSubject>.Ascending(expression: s => s.Teacher != null ? 1 : 0)
				.ThenByAscending(expression: s => s.Name!).ThenByAscending(expression: s => s.Teacher?.FullName ?? String.Empty));

		_ = _studyingSubjectsCache.Connect().RefCount().Filter(predicateChanged: filter).Sort(comparerObservable: sort)
			.Bind(readOnlyObservableCollection: out _studyingSubjects).DisposeMany().Subscribe();

		TaskCompletionStatuses.Load(items: Enum.GetValues<TaskCompletionStatus>());
		SelectedStatus = 0;

		SubjectSelectionModel.LostSelection += OnSubjectSelectionLost;
	}

	public ReactiveCommand<Unit, Unit> OnSubjectSelectionChanged { get; }
	public ReactiveCommand<Unit, Unit> OnTaskCompletionStatusSelectionChanged { get; }
	public ReactiveCommand<Unit, Unit> CloseAttachments { get; }
	public ReactiveCommand<Unit, Unit> LoadTasks { get; }
	public ReactiveCommand<Unit, Unit> ClearTasks { get; }

	public SelectionModel<StudentSubject> SubjectSelectionModel { get; } = new SelectionModel<StudentSubject>();

	public ReadOnlyObservableCollection<StudentSubject> StudyingSubjects => _studyingSubjects;

	public ObservableCollectionExtended<TaskCompletionStatus> TaskCompletionStatuses { get; }
		= new ObservableCollectionExtended<TaskCompletionStatus>();

	public ObservableCollectionExtended<ObservableReceivedTask> Tasks { get; }
		= new ObservableCollectionExtended<ObservableReceivedTask>();

	public ObservableCollectionExtended<ExtendedAttachment> Attachments { get; }
		= new ObservableCollectionExtended<ExtendedAttachment>();

	public TaskCompletionStatus SelectedStatus
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
		if (_taskCollection!.AllItemsAreUploaded)
			return;

		int currentLength = _taskCollection.Length;
		await _taskCollection.LoadNext();
		IEnumerable<ReceivedTask> tasks = await _taskCollection.GetByRange(start: currentLength, end: _taskCollection.Length);
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
		_taskCollection = null;
		Tasks.Clear();
	}

	public ReactiveCommand<Unit, Unit> GetShowAttachments(ReceivedTask task)
	{
		return ReactiveCommand.Create(execute: () =>
		{
			ShowAttachments = true;
			Attachments.Load(items: task.Content.Attachments?.Select(selector: a => a.ToExtended())!);
		});
	}

	public Func<StudentSubject, bool> FilterFunction(string? text)
	{
		return subject => subject.Name?.ToLower().StartsWith(value: text!, StringComparison.CurrentCultureIgnoreCase) == true ||
			subject.Teacher?.FullName.ToLower().StartsWith(value: text!, StringComparison.CurrentCultureIgnoreCase) == true;
	}

	private async Task TaskCompletionStatusSelectionChangedHandler()
	{
		ShowAttachments = false;
		if (_taskCollection is null || SubjectSelectionModel.SelectedItem is null)
			return;

		await _taskCollection.SetCompletionStatus(status: _selectedStatus);
		List<ReceivedTask> tasks = await _taskCollection.ToListAsync();
		Tasks.Load(items: tasks.Select(selector: t => t.ToObservable(
			showLessonName: SubjectSelectionModel.SelectedItem.Name!.Contains(value: "Все дисциплины"),
			showAttachments: GetShowAttachments(task: t)
		)));
		SetTaskSelection();
	}

	private async Task SubjectSelectionChangedHandler()
	{
		ShowAttachments = false;
		if (SubjectSelectionModel.SelectedItem is null)
			return;

		_taskCollection = await SubjectSelectionModel.SelectedItem.GetTasks();
		await _taskCollection.SetCompletionStatus(status: _selectedStatus);
		List<ReceivedTask> tasks = await _taskCollection.ToListAsync();
		Tasks.Load(items: tasks.Select(selector: t => t.ToObservable(
			showLessonName: SubjectSelectionModel.SelectedItem.Name!.Contains(value: "Все дисциплины"),
			showAttachments: GetShowAttachments(task: t)
		)));
		SetTaskSelection();
	}

	private void SetTaskSelection()
	{
		AllTasksSelected = _selectedStatus == TaskCompletionStatus.All;
		ExpiredTasksSelected = _selectedStatus == TaskCompletionStatus.Expired;
		UncompletedTasksSelected = _selectedStatus == TaskCompletionStatus.Uncompleted;
		CompletedTasksSelected = _selectedStatus == TaskCompletionStatus.Completed;
	}

	private async void OnStudyingSubjectsCreatedTask(CreatedTaskEventArgs e)
	{
		if (_taskCollection is null)
			return;

		ReceivedTask? task = await _taskCollection.FindById(id: e.TaskId);
		if (task is null)
			return;

		if (!Enum.TryParse(value: _selectedStatus.ToString(), out ReceivedTask.TaskCompletionStatus taskStatus) &&
			_selectedStatus != TaskCompletionStatus.All)
			return;

		if (task.CompletionStatus != taskStatus || _selectedStatus != TaskCompletionStatus.All)
			return;

		int index = Math.Max(val1: await _taskCollection.GetIndex(task: task), val2: 0);
		Tasks.Insert(item: task.ToObservable(
			showLessonName: SubjectSelectionModel.SelectedItem?.Name!.Contains(value: "Все дисциплины") == true,
			showAttachments: GetShowAttachments(task: task)
		), index: index);
		Debug.WriteLine($"END");
	}

	private void ClearSelection()
	{
		SubjectSelectionModel.SelectedItem = null;
		Tasks.Clear();
	}

	public override async Task SetUser(User user)
	{
		Parent? parent = user as Parent;

		_studentSubjectCollection = user is Student student
			? new StudentSubjectCollection(studyingSubjectCollection: await student.GetStudyingSubjects())
			: new StudentSubjectCollection(wardStudyingSubjectCollection: await parent!.GetWardSubjectsStudying());

		List<StudentSubject> subjects = await _studentSubjectCollection.ToListAsync();
		_studyingSubjectsCache.Edit(updateAction: (a) => a.AddOrUpdate(items: subjects));

		_studentSubjectCollection.CreatedTask += OnStudyingSubjectsCreatedTask;
	}
}