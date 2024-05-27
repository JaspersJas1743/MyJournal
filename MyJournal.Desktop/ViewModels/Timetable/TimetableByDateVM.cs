using System;
using System.Reactive;
using System.Threading.Tasks;
using DynamicData.Binding;
using MyJournal.Core;
using MyJournal.Desktop.Models.Timetable;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Timetable;

public sealed class TimetableByDateVM(TimetableByDateModel model) : BaseTimetableVM(model: model)
{
	public ReactiveCommand<Unit, Unit> OnDaysSelectionChanged => model.OnDaysSelectionChanged;
	public ReactiveCommand<Unit, Unit> SetNowDate => model.SetNowDate;
	public ReactiveCommand<Unit, Unit> ChangeVisualizerToSubjects => model.ChangeVisualizerToSubjects;

	public ObservableCollectionExtended<DateOnly> Dates => model.Dates;
	public DateOnly CurrentDate => model.CurrentDate;

	public DateOnly? SelectedDate
	{
		get => model.SelectedDate;
		set => model.SelectedDate = value;
	}

	public ObservableCollectionExtended<Assets.Utilities.TimetableUtilities.Timetable> Timetable => model.Timetable;

	public override async Task SetUser(User user)
		=> await model.SetUser(user: user);
}