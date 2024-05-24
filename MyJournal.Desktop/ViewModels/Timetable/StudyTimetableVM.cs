using System;
using System.Reactive;
using System.Threading.Tasks;
using DynamicData.Binding;
using MyJournal.Core;
using MyJournal.Desktop.Assets.Utilities.TimetableUtilities;
using MyJournal.Desktop.Models.Timetable;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Timetable;

public sealed class StudyTimetableVM(StudyTimetableModel model) : TimetableVM(model: model)
{
	public ReactiveCommand<Unit, Unit> OnDaysSelectionChanged => model.OnDaysSelectionChanged;
	public ReactiveCommand<Unit, Unit> SetNowDate => model.SetNowDate;

	public ObservableCollectionExtended<DateOnly> Dates => model.Dates;
	public DateOnly CurrentDate => model.CurrentDate;

	public DateOnly? SelectedDate
	{
		get => model.SelectedDate;
		set => model.SelectedDate = value;
	}

	public ObservableCollectionExtended<ObservableTimetableByDate> Timetable => model.Timetable;

	public override async Task SetUser(User user)
		=> await model.SetUser(user: user);
}