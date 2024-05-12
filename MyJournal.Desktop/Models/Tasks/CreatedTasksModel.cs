using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using CreatedTaskCollection = MyJournal.Desktop.Assets.Utilities.TasksUtilities.CreatedTaskCollection;

namespace MyJournal.Desktop.Models.Tasks;

public sealed class CreatedTasksModel : TasksModel
{
	private readonly ReadOnlyObservableCollection<TeacherSubject> _studyingSubjects;
	private readonly SourceCache<TeacherSubject, int> _teacherSubjectsCache = new SourceCache<TeacherSubject, int>(keySelector: s => s.Id);
	private TeacherSubjectCollection _teacherSubjectCollection;
	private CreatedTaskCollection? _taskCollection;
	private string? _filter = String.Empty;
	private CreatedTaskCompletionStatus _selectedStatus = 0;
	private bool _showAttachments = false;
	private bool _allTasksSelected = false;
	private bool _expiredTasksSelected = false;
	private bool _notExpiredTasksSelected = false;

	public CreatedTasksModel()
	{
		OnSubjectSelectionChanged = ReactiveCommand.CreateFromTask(execute: SubjectSelectionChangedHandler);
		OnTaskCompletionStatusSelectionChanged = ReactiveCommand.CreateFromTask(execute: TaskCompletionStatusSelectionChangedHandler);
		CloseAttachments = ReactiveCommand.Create(execute: ClearAttachments);
		LoadTasks = ReactiveCommand.CreateFromTask(execute: LoadMoreTask);
		ClearTasks = ReactiveCommand.Create(execute: ClearSelection);

		IObservable<Func<TeacherSubject, bool>> filter = this.WhenAnyValue(model => model.Filter).WhereNotNull()
			.Throttle(dueTime: TimeSpan.FromSeconds(value: 0.25), scheduler: RxApp.TaskpoolScheduler)
			.Select(selector: FilterFunction);

		IObservable<SortExpressionComparer<TeacherSubject>> sort = this.WhenAnyValue(model => model.Filter).WhereNotNull()
			.Throttle(dueTime: TimeSpan.FromSeconds(value: 0.25), scheduler: RxApp.TaskpoolScheduler)
			.Select(selector: _ => SortExpressionComparer<TeacherSubject>.Ascending(expression: s => s.ClassName != null ? 1 : 0)
				.ThenByAscending(expression: s => s.Name!).ThenByAscending(expression: s => s.ClassName ?? String.Empty));

		_ = _teacherSubjectsCache.Connect().RefCount().Filter(predicateChanged: filter).Sort(comparerObservable: sort)
			.Bind(readOnlyObservableCollection: out _studyingSubjects).DisposeMany().Subscribe();

		TaskCompletionStatuses.Load(items: Enum.GetValues<CreatedTaskCompletionStatus>());
		SelectedStatus = 0;

		SubjectSelectionModel.LostSelection += OnSubjectSelectionLost;
	}

	public ReactiveCommand<Unit, Unit> OnSubjectSelectionChanged { get; }
	public ReactiveCommand<Unit, Unit> OnTaskCompletionStatusSelectionChanged { get; }
	public ReactiveCommand<Unit, Unit> CloseAttachments { get; }
	public ReactiveCommand<Unit, Unit> LoadTasks { get; }
	public ReactiveCommand<Unit, Unit> ClearTasks { get; }

	public SelectionModel<TeacherSubject> SubjectSelectionModel { get; } = new SelectionModel<TeacherSubject>();

	public ReadOnlyObservableCollection<TeacherSubject> StudyingSubjects => _studyingSubjects;

	public ObservableCollectionExtended<CreatedTaskCompletionStatus> TaskCompletionStatuses { get; }
		= new ObservableCollectionExtended<CreatedTaskCompletionStatus>();

	public ObservableCollectionExtended<ObservableCreatedTask> Tasks { get; }
		= new ObservableCollectionExtended<ObservableCreatedTask>();

	public ObservableCollectionExtended<ExtendedAttachment> Attachments { get; }
		= new ObservableCollectionExtended<ExtendedAttachment>();

	public CreatedTaskCompletionStatus SelectedStatus
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

	public bool NotExpiredTasksSelected
	{
		get => _notExpiredTasksSelected;
		set => this.RaiseAndSetIfChanged(backingField: ref _notExpiredTasksSelected, newValue: value);
	}

	private async Task SubjectSelectionChangedHandler()
	{
		ShowAttachments = false;
		if (SubjectSelectionModel.SelectedItem is null)
			return;

		_taskCollection = await SubjectSelectionModel.SelectedItem.GetTasks();
		await _taskCollection.SetCompletionStatus(status: _selectedStatus);
		List<CreatedTask> tasks = await _taskCollection.ToListAsync();
		Tasks.Load(items: tasks.Select(selector: t => t.ToObservable(
			showLessonName: SubjectSelectionModel.SelectedItem.Name!.Contains(value: "Все классы") ||
							SubjectSelectionModel.SelectedItem.Name!.Contains(value: "Все дисциплины"),
			showAttachments: GetShowAttachments(task: t)
		)));
		SetTaskSelection();
	}

	private void SetTaskSelection()
	{
		AllTasksSelected = _selectedStatus == CreatedTaskCompletionStatus.All;
		ExpiredTasksSelected = _selectedStatus == CreatedTaskCompletionStatus.Expired;
		NotExpiredTasksSelected = _selectedStatus == CreatedTaskCompletionStatus.NotExpired;
	}

	public Func<TeacherSubject, bool> FilterFunction(string? text)
	{
		return subject => subject.Name?.ToLower().StartsWith(value: text!, StringComparison.CurrentCultureIgnoreCase) == true ||
			subject.ClassName?.ToLower().StartsWith(value: text!, StringComparison.CurrentCultureIgnoreCase) == true;
	}

	private void ClearSelection()
	{
		SubjectSelectionModel.SelectedItem = null;
		Tasks.Clear();
	}

	private async void OnTaughtSubjectsCreatedTask(CreatedTaskEventArgs e)
	{
		if (_taskCollection is null || SubjectSelectionModel.SelectedItem is null)
			return;

		CreatedTask? task = await _taskCollection.FindById(id: e.TaskId);
		if (task is null)
			return;

		if (_selectedStatus == CreatedTaskCompletionStatus.Expired)
			return;

		int index = Math.Max(val1: await _taskCollection.GetIndex(task: task), val2: 0);
		Tasks.Insert(item: task.ToObservable(
			showLessonName: SubjectSelectionModel.SelectedItem.Name!.Contains(value: "Все классы") ||
							SubjectSelectionModel.SelectedItem.Name!.Contains(value: "Все дисциплины"),
			showAttachments: GetShowAttachments(task: task)
		), index: index);
	}

	private async Task TaskCompletionStatusSelectionChangedHandler()
	{
		ShowAttachments = false;
		if (_taskCollection is null || SubjectSelectionModel.SelectedItem is null)
			return;

		await _taskCollection.SetCompletionStatus(status: _selectedStatus);
		List<CreatedTask> tasks = await _taskCollection.ToListAsync();
		Tasks.Load(items: tasks.Select(selector: t => t.ToObservable(
			showLessonName: SubjectSelectionModel.SelectedItem.Name!.Contains(value: "Все классы") ||
							SubjectSelectionModel.SelectedItem.Name!.Contains(value: "Все дисциплины"),
			showAttachments: GetShowAttachments(task: t)
		)));
		SetTaskSelection();
	}

	public ReactiveCommand<Unit, Unit> GetShowAttachments(CreatedTask task)
	{
		return ReactiveCommand.Create(execute: () =>
		{
			ShowAttachments = true;
			Attachments.Load(items: task.Content.Attachments?.Select(selector: a => a.ToExtended())!);
		});
	}

	private void OnSubjectSelectionLost(object? sender, EventArgs e)
	{
		_taskCollection = null;
		Tasks.Clear();
	}


	private async Task LoadMoreTask()
	{
		if (_taskCollection!.AllItemsAreUploaded || SubjectSelectionModel.SelectedItem is null)
			return;

		int currentLength = _taskCollection.Length;
		await _taskCollection.LoadNext();
		IEnumerable<CreatedTask> tasks = await _taskCollection.GetByRange(start: currentLength, end: _taskCollection.Length);
		Tasks.Add(items: tasks.Select(selector: task => task.ToObservable(
			showLessonName: SubjectSelectionModel.SelectedItem.Name!.Contains(value: "Все классы") ||
							SubjectSelectionModel.SelectedItem.Name!.Contains(value: "Все дисциплины"),
			showAttachments: GetShowAttachments(task: task)
		)));
	}

	private void ClearAttachments()
	{
		ShowAttachments = false;
		Attachments.Clear();
	}

	public override async Task SetUser(User user)
	{
		Administrator? administrator = user as Administrator;

		_teacherSubjectCollection = user is Teacher teacher
			? new TeacherSubjectCollection(taughtSubjectCollection: await teacher.GetTaughtSubjects())
            : new TeacherSubjectCollection(classCollection: await administrator!.GetClasses());

		List<TeacherSubject> subjects = await _teacherSubjectCollection.ToListAsync();
		_teacherSubjectsCache.Edit(updateAction: (a) => a.AddOrUpdate(items: subjects));

		_teacherSubjectCollection.CreatedTask += OnTaughtSubjectsCreatedTask;
	}
}