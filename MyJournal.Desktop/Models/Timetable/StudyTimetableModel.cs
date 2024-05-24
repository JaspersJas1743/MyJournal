using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using DynamicData.Binding;
using MyJournal.Core;
using MyJournal.Desktop.Assets.Utilities.TimetableUtilities;
using ReactiveUI;

namespace MyJournal.Desktop.Models.Timetable;

public sealed class StudyTimetableModel : TimetableModel
{
	private ObservableTimetableByDateCollection _timetableCollection;
	private IEnumerable<ObservableTimetableByDate> _timetable;
	private DateOnly? _selectedDate = null;

	public StudyTimetableModel()
	{
		OnDaysSelectionChanged = ReactiveCommand.CreateFromTask(execute: DaysSelectionChangedHandler);
		IObservable<bool> canSetNowDate = this.WhenAnyValue(
			property1: model => model.SelectedDate,
			selector: date => date != DateOnly.FromDateTime(dateTime: DateTime.Now)
		);
		SetNowDate = ReactiveCommand.CreateFromTask(execute: SetTimetableForNow, canExecute: canSetNowDate);
	}

	public ReactiveCommand<Unit, Unit> OnDaysSelectionChanged { get; }
	public ReactiveCommand<Unit, Unit> SetNowDate { get; }

	public ObservableCollectionExtended<DateOnly> Dates { get; } = new ObservableCollectionExtended<DateOnly>();

	public ObservableCollectionExtended<ObservableTimetableByDate> Timetable { get; } =
		new ObservableCollectionExtended<ObservableTimetableByDate>();

	public DateOnly CurrentDate { get; } = DateOnly.FromDateTime(dateTime: DateTime.Now);

	public DateOnly? SelectedDate
	{
		get => _selectedDate;
		set => this.RaiseAndSetIfChanged(backingField: ref _selectedDate, newValue: value);
	}

	private async Task SetTimetableForNow()
	{
		Dates.Load(items: Enumerable.Range(start: -3, count: 7).Select(
					   selector: offset => DateOnly.FromDateTime(dateTime: DateTime.Now.AddDays(value: offset))
				   ));
		SelectedDate = CurrentDate;
		Timetable.Load(items: await _timetableCollection.GetTimetable(date: SelectedDate.Value));
	}

	private async Task DaysSelectionChangedHandler()
	{
		if (SelectedDate is null)
			return;

		int selectedIndex = Dates.IndexOf(item: SelectedDate.Value);
		int count = Dates.Count - 1;
		int half = count / 2;
		int step = half - selectedIndex;
		bool toTop = step < 0;
		step = Math.Abs(value: step);

		if (step == 0)
			return;

		SelectedDate = null;
		if (!toTop)
		{
			IEnumerable<DateOnly> newDays = Enumerable.Range(start: 1, count: step).Select(selector: offset => Dates[index: 0].AddDays(value: -offset)).Reverse();
			Dates.RemoveRange(index: count - step + 1, count: step);
			Dates.InsertRange(collection: newDays, index: 0);
		}
		else
		{
			IEnumerable<DateOnly> newDays = Enumerable.Range(start: 1, count: step).Select(selector: Dates[index: count].AddDays);
			Dates.RemoveRange(index: 0, count: step);
			Dates.AddRange(collection: newDays);
		}
		SelectedDate = Dates[index: half];
		Timetable.Load(items: await _timetableCollection.GetTimetable(date: SelectedDate.Value));
	}

	public override async Task SetUser(User user)
	{
		_timetableCollection = user switch
		{
			Student student => (await student.GetTimetable()).ToObservable(),
			Parent parent => (await parent.GetTimetable()).ToObservable(),
			Teacher teacher => (await teacher.GetTimetable()).ToObservable(),
			_ => throw new ArgumentException(message: "Пользователь имеет неподдержимваемую роль.")
		};
		await SetTimetableForNow();
	}
}