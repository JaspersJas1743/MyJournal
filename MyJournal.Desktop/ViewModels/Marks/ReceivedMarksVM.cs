using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia.Controls.Selection;
using DynamicData.Binding;
using MyJournal.Core.SubEntities;
using MyJournal.Desktop.Assets.Utilities.MarksUtilities;
using MyJournal.Desktop.Models.Marks;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Marks;

public sealed class ReceivedMarksVM(ReceivedMarksModel model): MarksVM(model: model)
{
	public ReadOnlyObservableCollection<StudentSubject> StudyingSubjects => model.StudyingSubjects;
	public SelectionModel<StudentSubject> SubjectSelectionModel => model.SubjectSelectionModel;
	public ObservableCollectionExtended<EducationPeriod> EducationPeriods => model.EducationPeriods;
	public ObservableCollectionExtended<Estimation> Estimations => model.Estimations;

	public string? Filter
	{
		get => model.Filter;
		set => model.Filter = value;
	}

	public string? Average
	{
		get => model.Average;
		set => model.Average = value;
	}

	public string? Final
	{
		get => model.Final;
		set => model.Final = value;
	}

	public EducationPeriod? SelectedPeriod
	{
		get => model.SelectedPeriod;
		set => model.SelectedPeriod = value;
	}

	public ReactiveCommand<Unit, Unit> OnTaskCompletionStatusSelectionChanged => model.OnTaskCompletionStatusSelectionChanged;
	public ReactiveCommand<Unit, Unit> OnSubjectSelectionChanged => model.OnSubjectSelectionChanged;
	public ReactiveCommand<Unit, Unit> ClearTasks => model.ClearTasks;
}