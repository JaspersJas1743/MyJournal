using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Selection;
using Avalonia.Threading;
using DynamicData;
using DynamicData.Binding;
using MyJournal.Core;
using MyJournal.Core.Builders.TimetableBuilder;
using MyJournal.Core.Utilities.EventArgs;
using MyJournal.Desktop.Assets.Utilities.NotificationService;
using MyJournal.Desktop.Assets.Utilities.TimetableUtilities;
using ReactiveUI;
using Class = MyJournal.Desktop.Assets.Utilities.TimetableUtilities.Class;

namespace MyJournal.Desktop.Models.Timetable;

public sealed class CreatingTimetableModel : BaseTimetableModel
{
	private readonly INotificationService _notificationService;
	private readonly SourceCache<Class, int> _teacherSubjectsCache = new SourceCache<Class, int>(keySelector: s => s.Id);
	private ClassCollection? _classCollection;
	private readonly ReadOnlyObservableCollection<Class> _classes;
	private string? _filter = String.Empty;
	private bool _changeFromClient = false;

	public CreatingTimetableModel(INotificationService notificationService)
	{
		_notificationService = notificationService;
		OnClassSelectionChanged = ReactiveCommand.CreateFromTask(execute: ClassSelectionChangedHandler);
		SaveTimetableForSelectedClass = ReactiveCommand.CreateFromTask(execute: SaveTimetableForSelectedClassHandler);
		SaveTimetable = ReactiveCommand.CreateFromTask(execute: SaveTimetableHandler);
		ClearSelection = ReactiveCommand.Create(execute: ClearSelectionHandler);

		IObservable<Func<Class, bool>> filter = this.WhenAnyValue(property1: model => model.Filter).WhereNotNull()
			.Throttle(dueTime: TimeSpan.FromSeconds(value: 0.25), scheduler: RxApp.TaskpoolScheduler)
			.Select(selector: FilterFunction);

		IObservable<SortExpressionComparer<Class>> sort = this.WhenAnyValue(model => model.Filter).WhereNotNull()
			.Throttle(dueTime: TimeSpan.FromSeconds(value: 0.25), scheduler: RxApp.TaskpoolScheduler)
			.Select(selector: _ => SortExpressionComparer<Class>.Ascending(expression: s => Int32.Parse(s: s.Name!.Split()[0])));

		_ = _teacherSubjectsCache.Connect().RefCount().Filter(predicateChanged: filter).Sort(comparerObservable: sort)
			.Bind(readOnlyObservableCollection: out _classes).DisposeMany().Subscribe();
	}

	private void ClearSelectionHandler()
	{
		Timetable.Clear();
		SubjectSelectionModel.SelectedItem = null;
	}

	private async Task SaveTimetableHandler()
	{
		try
		{
			foreach (Class @class in Classes)
				await SaveTimetableForClass(@class: @class);
			await _notificationService.Show(
				title: "Расписание",
				content: "Расписание успешно изменено!",
				type: NotificationType.Success
			);
		}
		catch (Exception ex)
		{
			await _notificationService.Show(
				title: "Ошибка при сохранении",
				content: ex.Message,
				type: NotificationType.Error
			);
		}
	}

	private async Task SaveTimetableForSelectedClassHandler()
	{
		if (SubjectSelectionModel.SelectedItem is null)
			return;

		try
		{
			await SaveTimetableForClass(@class: SubjectSelectionModel.SelectedItem);
			await _notificationService.Show(
				title: "Расписание",
				content: "Расписание успешно изменено!",
				type: NotificationType.Success
			);
		}
		catch (Exception ex)
		{
			await _notificationService.Show(
				title: "Ошибка при сохранении",
				content: ex.Message,
				type: NotificationType.Error
			);
		}
	}

	private async Task SaveTimetableForClass(Class @class)
	{
		ObservableCollectionExtended<CreatingTimetable> existedTimetable = await @class.GetTimetable();
		if (existedTimetable.Count <= 0)
			return;

		ITimetableBuilder builder = @class.CreateTimetable();
		foreach (CreatingTimetable creatingTimetable in await @class.GetTimetable())
		{
			foreach (SubjectOnTimetable subjectOnTimetable in creatingTimetable.Subjects)
			{
				if (subjectOnTimetable.Number is null)
					throw new Exception(message: $"Номер занятия не указан.");

				if (subjectOnTimetable.SelectedSubject?.Id is null)
					throw new Exception(message: $"Дисциплина для {subjectOnTimetable.Number} занятия не указана.");

				if (subjectOnTimetable.Start is null)
					throw new Exception(message: $"Время окончания {subjectOnTimetable.Number} занятия не указан.");

				if (subjectOnTimetable.End is null)
					throw new Exception(message: $"Время окончания {subjectOnTimetable.Number} занятия не указан.");

				builder.ForDay(dayOfWeekId: creatingTimetable.DayOfWeek.Id)
					   .AddSubject()
					   .WithNumber(number: subjectOnTimetable.Number.Value)
					   .WithSubject(subjectId: subjectOnTimetable.SelectedSubject.Id)
					   .WithStartTime(time: subjectOnTimetable.Start.Value)
					   .WithEndTime(time: subjectOnTimetable.End.Value);
			}
		}

		await builder.Save();
		_changeFromClient = true;
	}

	public ReactiveCommand<Unit, Unit> OnClassSelectionChanged { get; }
	public ReactiveCommand<Unit, Unit> SaveTimetableForSelectedClass { get; }
	public ReactiveCommand<Unit, Unit> SaveTimetable { get; }
	public ReactiveCommand<Unit, Unit> ClearSelection { get; }

	public SelectionModel<Class> SubjectSelectionModel { get; } = new SelectionModel<Class>();

	public ObservableCollectionExtended<CreatingTimetable> Timetable { get; } =
		new ObservableCollectionExtended<CreatingTimetable>();

	public ReadOnlyObservableCollection<Class> Classes => _classes;

	public string? Filter
	{
		get => _filter;
		set => this.RaiseAndSetIfChanged(backingField: ref _filter, newValue: value);
	}

	private async Task ClassSelectionChangedHandler()
		=> await UpdateTimetable();

	private async Task UpdateTimetable()
	{
		if (SubjectSelectionModel.SelectedItem is null)
			return;

		await Dispatcher.UIThread.InvokeAsync(
			callback: async () => Timetable.Load(items: await SubjectSelectionModel.SelectedItem.GetTimetable())
		);
	}

	public Func<Class, bool> FilterFunction(string? text)
		=> subject => subject.Name?.ToLower().StartsWith(value: text!, StringComparison.CurrentCultureIgnoreCase) == true;

	public override async Task SetUser(User user)
	{
		Administrator administrator = (user as Administrator)!;
		administrator.ChangedTimetable += ChangedTimetableHandler;
		_classCollection = new ClassCollection(classCollection: await administrator.GetClasses());
		List<Class> subjects = await _classCollection.ToListAsync();
		_teacherSubjectsCache.Edit(updateAction: a => a.AddOrUpdate(items: subjects));
	}

	private async void ChangedTimetableHandler(ChangedTimetableEventArgs e)
	{
		if (_changeFromClient)
		{
			_changeFromClient = false;
			return;
		}

		if (SubjectSelectionModel.SelectedItem?.Id == e.ClassId)
			await UpdateTimetable();

		await _notificationService.Show(
			title: "Расписание",
			content: $"Расписание для {e.ClassId} класса было изменено!",
			type: NotificationType.Information
		);
	}
}