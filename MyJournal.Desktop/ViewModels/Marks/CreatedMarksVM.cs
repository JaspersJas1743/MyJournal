using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia.Controls.Selection;
using DynamicData.Binding;
using MyJournal.Core.SubEntities;
using MyJournal.Desktop.Assets.Utilities.MarksUtilities;
using MyJournal.Desktop.Models.Marks;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Marks;

public sealed class CreatedMarksVM(CreatedMarksModel model) : MarksVM(model: model)
{
	public ReadOnlyObservableCollection<TeacherSubject> StudyingSubjects => model.StudyingSubjects;

	public SelectionModel<TeacherSubject> SubjectSelectionModel => model.SubjectSelectionModel;

	public ObservableCollectionExtended<EducationPeriod> EducationPeriods => model.EducationPeriods;

	public ObservableCollectionExtended<ObservableStudent> Students => model.Students;

	public string? Filter
	{
		get => model.Filter;
		set => model.Filter = value;
	}

	public EducationPeriod? SelectedPeriod
	{
		get => model.SelectedPeriod;
		set => model.SelectedPeriod = value;
	}

	public ReactiveCommand<Unit, Unit> OnSubjectSelectionChanged => model.OnSubjectSelectionChanged;
	public ReactiveCommand<Unit, Unit> OnTaskCompletionStatusSelectionChanged => model.OnTaskCompletionStatusSelectionChanged;
	public ReactiveCommand<Unit, Unit> ClearTasks => model.ClearTasks;
}