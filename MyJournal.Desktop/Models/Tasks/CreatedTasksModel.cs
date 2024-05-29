using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Web;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Selection;
using Avalonia.Platform.Storage;
using DynamicData;
using DynamicData.Binding;
using MyJournal.Core;
using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.EventArgs;
using MyJournal.Desktop.Assets.MessageBusEvents;
using MyJournal.Desktop.Assets.Utilities.ChatUtilities;
using MyJournal.Desktop.Assets.Utilities.FileService;
using MyJournal.Desktop.Assets.Utilities.NotificationService;
using MyJournal.Desktop.Assets.Utilities.TasksUtilities;
using ReactiveUI;
using Class = MyJournal.Desktop.Assets.Utilities.TasksUtilities.Class;
using CreatedTask = MyJournal.Desktop.Assets.Utilities.TasksUtilities.CreatedTask;
using CreatedTaskCollection = MyJournal.Desktop.Assets.Utilities.TasksUtilities.CreatedTaskCollection;
using Subject = MyJournal.Desktop.Assets.Utilities.TasksUtilities.Subject;

namespace MyJournal.Desktop.Models.Tasks;

public sealed class CreatedTasksModel : TasksModel
{
	private List<Class> _classes;
	private ObservableCreatedTask _taskCreator;
	private readonly INotificationService _notificationService;
	private readonly IFileStorageService _fileStorageService;
	private readonly SourceList<TeacherSubject> _teacherSubjectsCache =
		new SourceList<TeacherSubject>();
	private readonly ReadOnlyObservableCollection<TeacherSubject> _studyingSubjects;
	private TeacherSubjectCollection _teacherSubjectCollection;
	private CreatedTaskCollection? _taskCollection;
	private string? _filter = String.Empty;
	private CreatedTaskCompletionStatus _selectedStatus = 0;
	private bool _showAttachments = false;
	private bool _showTaskCreation = false;
	private bool _showEditableAttachments = false;
	private bool _allTasksSelected = false;
	private bool _expiredTasksSelected = false;
	private bool _notExpiredTasksSelected = false;

	public CreatedTasksModel(
		INotificationService notificationService,
		IFileStorageService fileStorageService
	)
	{
		_fileStorageService = fileStorageService;
		_notificationService = notificationService;

		OnSubjectSelectionChanged = ReactiveCommand.CreateFromTask(execute: SubjectSelectionChangedHandler);
		OnTaskCompletionStatusSelectionChanged = ReactiveCommand.CreateFromTask(execute: TaskCompletionStatusSelectionChangedHandler);
		CloseAttachments = ReactiveCommand.Create(execute: ClearAttachments);
		CloseCreatedAttachments = ReactiveCommand.Create(execute: ClearCreatedAttachments);
		LoadTasks = ReactiveCommand.CreateFromTask(execute: LoadMoreTask);
		ClearTasks = ReactiveCommand.Create(execute: ClearSelection);
		LoadAttachment = ReactiveCommand.CreateFromTask(execute: AddAttachmentsToTask);

		IObservable<Func<TeacherSubject, bool>> filter = this.WhenAnyValue(model => model.Filter).WhereNotNull()
			.Throttle(dueTime: TimeSpan.FromSeconds(value: 0.25), scheduler: RxApp.TaskpoolScheduler)
			.Select(selector: FilterFunction);

		IObservable<SortExpressionComparer<TeacherSubject>> sort = this.WhenAnyValue(model => model.Filter).WhereNotNull()
			.Throttle(dueTime: TimeSpan.FromSeconds(value: 0.25), scheduler: RxApp.TaskpoolScheduler)
			.Select(selector: _ => SortExpressionComparer<TeacherSubject>.Ascending(expression: s => s.ClassId != null ? 1 : 0)
				.ThenByAscending(expression: s => s.ClassId ?? 0).ThenByAscending(expression: s => s.Name!));

		_ = _teacherSubjectsCache.Connect().RefCount().Filter(predicate: filter).Sort(comparerChanged: sort)
			.Bind(readOnlyObservableCollection: out _studyingSubjects).DisposeMany().Subscribe();

		TaskCompletionStatuses.Load(items: Enum.GetValues<CreatedTaskCompletionStatus>());
		SelectedStatus = 0;

		SubjectSelectionModel.LostSelection += OnSubjectSelectionLost;

		MessageBus.Current.Listen<AttachmentAddedToTaskEventArgs>().Subscribe(onNext: _ => AttachmentsForCreatedTask.Last().IsLoaded = true);

		MessageBus.Current.Listen<TaskSavedEventArgs>().Subscribe(onNext: _ => AttachmentsForCreatedTask.Clear());
	}

	private void ClearCreatedAttachments()
		=> ShowEditableAttachments = false;

	private async Task AddAttachmentsToTask()
	{
		IStorageFile? file = await _fileStorageService.OpenFile(fileTypes: new FilePickerFileType[] { FilePickerFileTypes.All });
		if (file is null)
			return;

		StorageItemProperties basicProperties = await file.GetBasicPropertiesAsync();
		if (basicProperties.Size / (1024f * 1024f) >= 30)
		{
			await _notificationService.Show(
				title: "Слишком большой файл",
				content: "Максимальный размер файла - 30Мбайт.",
				type: NotificationType.Warning
			);
			return;
		}

		string pathToFile = HttpUtility.UrlDecode(str: file.Path.AbsolutePath);
		Attachment attachment = new Attachment()
		{
			FileName = Path.GetFileName(path: pathToFile),
			IsLoaded = false
		};
		attachment.Remove = ReactiveCommand.CreateFromTask(execute: async () =>
		{
			AttachmentsForCreatedTask.Remove(item: attachment);
			MessageBus.Current.SendMessage(message: new RemoveAttachmentFromTaskEventArgs(pathToFile: pathToFile));
		});

		AttachmentsForCreatedTask.Add(item: attachment);
		MessageBus.Current.SendMessage(message: new AddAttachmentToTaskEventArgs(pathToFile: pathToFile));
	}

	public ReactiveCommand<Unit, Unit> OnSubjectSelectionChanged { get; }
	public ReactiveCommand<Unit, Unit> OnTaskCompletionStatusSelectionChanged { get; }
	public ReactiveCommand<Unit, Unit> CloseAttachments { get; }
	public ReactiveCommand<Unit, Unit> CloseCreatedAttachments { get; }
	public ReactiveCommand<Unit, Unit> LoadTasks { get; }
	public ReactiveCommand<Unit, Unit> LoadAttachment { get; }
	public ReactiveCommand<Unit, Unit> ClearTasks { get; }

	public SelectionModel<TeacherSubject> SubjectSelectionModel { get; } = new SelectionModel<TeacherSubject>();

	public ReadOnlyObservableCollection<TeacherSubject> StudyingSubjects => _studyingSubjects;

	public ObservableCollectionExtended<CreatedTaskCompletionStatus> TaskCompletionStatuses { get; }
		= new ObservableCollectionExtended<CreatedTaskCompletionStatus>();

	public ObservableCollectionExtended<ObservableCreatedTask> Tasks { get; }
		= new ObservableCollectionExtended<ObservableCreatedTask>();

	public ObservableCollectionExtended<ExtendedAttachment> Attachments { get; }
		= new ObservableCollectionExtended<ExtendedAttachment>();

	public ObservableCollectionExtended<Attachment> AttachmentsForCreatedTask { get; }
		= new ObservableCollectionExtended<Attachment>();

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

	public bool ShowTaskCreation
	{
		get => _showTaskCreation;
		set => this.RaiseAndSetIfChanged(backingField: ref _showTaskCreation, newValue: value);
	}

	public bool ShowEditableAttachments
	{
		get => _showEditableAttachments;
		set => this.RaiseAndSetIfChanged(backingField: ref _showEditableAttachments, newValue: value);
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

		if (_selectedStatus == CreatedTaskCompletionStatus.All && ShowTaskCreation)
		{
			Class? selectedClass = _classes.FirstOrDefault(predicate: c => c?.Id == SubjectSelectionModel.SelectedItem.ClassId);
			await _taskCreator.SetSelectedClass(selectedClass: selectedClass);
			await _taskCreator.SetSelectedSubject(selectedSubjectId: SubjectSelectionModel.SelectedItem.Id);
			Tasks.Insert(index: 0, item: _taskCreator);
		}

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

		Tasks.Insert(item: task.ToObservable(
			showLessonName: SubjectSelectionModel.SelectedItem.Name!.Contains(value: "Все классы") ||
							SubjectSelectionModel.SelectedItem.Name!.Contains(value: "Все дисциплины"),
			showAttachments: GetShowAttachments(task: task)
		), index: _selectedStatus == CreatedTaskCompletionStatus.All ? 1 : 0);
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

		if (_selectedStatus == CreatedTaskCompletionStatus.All && ShowTaskCreation)
		{
			Class? selectedClass = _classes.FirstOrDefault(predicate: c => c?.Id == SubjectSelectionModel.SelectedItem.ClassId);
			await _taskCreator.SetSelectedClass(selectedClass: selectedClass);
			await _taskCreator.SetSelectedSubject(selectedSubjectId: SubjectSelectionModel.SelectedItem.Id);
			Tasks.Insert(index: 0, item: _taskCreator);
		}

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

		TaughtSubjectCollection? taughtSubjectCollection = user is Teacher teacher ? await teacher.GetTaughtSubjects() : null;
		ClassCollection? classCollection = administrator is not null ? await administrator.GetClasses() : null;
		_teacherSubjectCollection = taughtSubjectCollection is not null
			? new TeacherSubjectCollection(taughtSubjectCollection: taughtSubjectCollection)
            : new TeacherSubjectCollection(classCollection: classCollection!);

		if (taughtSubjectCollection is not null)
		{
			_classes = await taughtSubjectCollection.SelectAwait(selector: async s => new
			{
				Subject = new Subject(subject: s),
				Class = await s.GetTaughtClass()
			}).GroupBy(keySelector: o => o.Class.Id)
			.SelectAwait(selector: async o => new Class(
				id: o.Key,
				name: (await o.Select(selector: x => x.Class).FirstAsync()).Name,
				subjectsFactory: async () => await o.Select(selector: x => x.Subject).ToListAsync()
			)).Skip(count: 1).ToListAsync();
		}

		List<TeacherSubject> subjects = await _teacherSubjectCollection.ToListAsync();
		_teacherSubjectsCache.Edit(updateAction: a =>
		{
			a.Clear();
			a.AddRange(items: subjects);
		});

		ShowTaskCreation = administrator is null;

		_taskCreator = new ObservableCreatedTask(
			classes: _classes,
			taskBuilderFactory: () => _teacherSubjectCollection.CreateTask(),
			showAttachments: ReactiveCommand.Create(execute: () => { ShowEditableAttachments = true; }),
			notificationService: _notificationService
		);
		_teacherSubjectCollection.CreatedTask += OnTaughtSubjectsCreatedTask;
	}
}