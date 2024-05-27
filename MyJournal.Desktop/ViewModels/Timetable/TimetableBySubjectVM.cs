using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls.Selection;
using DynamicData.Binding;
using MyJournal.Core;
using MyJournal.Desktop.Assets.Utilities.TimetableUtilities;
using MyJournal.Desktop.Models.Timetable;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Timetable;

public sealed class TimetableBySubjectVM(TimetableBySubjectModel model) : BaseTimetableVM(model: model)
{
	public ReactiveCommand<Unit, Unit> ChangeVisualizerToDate => model.ChangeVisualizerToDate;
	public ReactiveCommand<Unit, Unit> OnSubjectSelectionChanged => model.OnSubjectSelectionChanged;
	public ReactiveCommand<Unit, Unit> ClearSelection => model.ClearSelection;

	public ReadOnlyObservableCollection<Subject> Subjects => model.Subjects;

	public SelectionModel<Subject> SubjectSelectionModel => model.SubjectSelectionModel;

	public ObservableCollectionExtended<Assets.Utilities.TimetableUtilities.Timetable> Timetable => model.Timetable;

	public string? Filter
	{
		get => model.Filter;
		set => model.Filter = value;
	}

	public override async Task SetUser(User user)
		=> await model.SetUser(user: user);
}