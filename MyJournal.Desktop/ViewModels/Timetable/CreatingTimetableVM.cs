using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls.Selection;
using DynamicData.Binding;
using MyJournal.Core;
using MyJournal.Desktop.Assets.Utilities.TimetableUtilities;
using MyJournal.Desktop.Models.Timetable;
using ReactiveUI;
using Class = MyJournal.Desktop.Assets.Utilities.TimetableUtilities.Class;

namespace MyJournal.Desktop.ViewModels.Timetable;

public sealed class CreatingTimetableVM(CreatingTimetableModel model) : BaseTimetableVM(model: model)
{
	public ReactiveCommand<Unit, Unit> OnClassSelectionChanged => model.OnClassSelectionChanged;
	public ReactiveCommand<Unit, Unit> SaveTimetableForSelectedClass => model.SaveTimetableForSelectedClass;
	public ReactiveCommand<Unit, Unit> SaveTimetable => model.SaveTimetable;

	public ObservableCollectionExtended<CreatingTimetable> Timetable => model.Timetable;

	public SelectionModel<Class> SubjectSelectionModel => model.SubjectSelectionModel;

	public ReadOnlyObservableCollection<Class> StudyingSubjects => model.StudyingSubjects;

	public string? Filter
	{
		get => model.Filter;
		set => model.Filter = value;
	}

	public override async Task SetUser(User user)
		=> await model.SetUser(user: user);
}