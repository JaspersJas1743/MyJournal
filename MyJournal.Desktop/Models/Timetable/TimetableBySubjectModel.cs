using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Selection;
using Avalonia.Threading;
using DynamicData;
using DynamicData.Binding;
using MyJournal.Core;
using MyJournal.Core.Utilities.EventArgs;
using MyJournal.Desktop.Assets.MessageBusEvents;
using MyJournal.Desktop.Assets.Utilities.TimetableUtilities;
using MyJournal.Desktop.ViewModels.Timetable;
using ReactiveUI;

namespace MyJournal.Desktop.Models.Timetable;

public sealed class TimetableBySubjectModel : BaseTimetableModel
{
	private readonly ReadOnlyObservableCollection<Subject> _subjects;
	private readonly SourceCache<Subject, int> _subjectsCache = new SourceCache<Subject, int>(keySelector: s => s.Id);
	private SubjectCollection _studentSubjectCollection;
	private string? _filter = String.Empty;

	public TimetableBySubjectModel()
	{
		ChangeVisualizerToDate = ReactiveCommand.Create(execute: () => MessageBus.Current.SendMessage(
			message: new ChangeTimetableVisualizerEventArgs(timetableVM: typeof(TimetableByDateVM))
		));
		OnSubjectSelectionChanged = ReactiveCommand.CreateFromTask(execute: SubjectSelectionChangedHandler);
		ClearSelection = ReactiveCommand.Create(execute: ClearSelectionHandler);

		IObservable<Func<Subject, bool>> filter = this.WhenAnyValue(model => model.Filter).WhereNotNull()
			.Throttle(dueTime: TimeSpan.FromSeconds(value: 0.25), scheduler: RxApp.TaskpoolScheduler)
			.Select(selector: FilterFunction);

		IObservable<SortExpressionComparer<Subject>> sort = this.WhenAnyValue(property1: model => model.Filter).WhereNotNull()
			.Throttle(dueTime: TimeSpan.FromSeconds(value: 0.25), scheduler: RxApp.TaskpoolScheduler)
			.Select(selector: _ => SortExpressionComparer<Subject>.Ascending(expression: s => s.Teacher != null ? 1 : 0)
				.ThenByAscending(expression: s => s.Name!).ThenByAscending(expression: s => s.Teacher?.FullName ?? String.Empty));

		_ = _subjectsCache.Connect().RefCount().Filter(predicateChanged: filter).Sort(comparerObservable: sort)
								  .Bind(readOnlyObservableCollection: out _subjects).DisposeMany().Subscribe();
	}

	private void ClearSelectionHandler()
	{
		SubjectSelectionModel.SelectedItem = null;
		Timetable.Clear();
	}

	private async Task SubjectSelectionChangedHandler()
		=> await LoadTimetable();

	private async Task LoadTimetable()
	{
		if (SubjectSelectionModel.SelectedItem is null)
			return;

		IEnumerable<Assets.Utilities.TimetableUtilities.Timetable> timetable = await SubjectSelectionModel.SelectedItem!.GetTimetable();
		await Dispatcher.UIThread.InvokeAsync(callback: () => Timetable.Load(items: timetable));
	}

	public ReadOnlyObservableCollection<Subject> Subjects => _subjects;
	public ObservableCollectionExtended<Assets.Utilities.TimetableUtilities.Timetable> Timetable { get; }
		= new ObservableCollectionExtended<Assets.Utilities.TimetableUtilities.Timetable>();

	public SelectionModel<Subject> SubjectSelectionModel { get; } = new SelectionModel<Subject>();

	public string? Filter
	{
		get => _filter;
		set => this.RaiseAndSetIfChanged(backingField: ref _filter, newValue: value);
	}

	public ReactiveCommand<Unit, Unit> ChangeVisualizerToDate { get; }
	public ReactiveCommand<Unit, Unit> OnSubjectSelectionChanged { get; }
	public ReactiveCommand<Unit, Unit> ClearSelection { get; }

	public Func<Subject, bool> FilterFunction(string? text)
	{
		return subject => subject.Name?.ToLower().StartsWith(value: text!, StringComparison.CurrentCultureIgnoreCase) == true ||
			subject.Teacher?.FullName.ToLower().StartsWith(value: text!, StringComparison.CurrentCultureIgnoreCase) == true ||
			subject.ClassName?.ToLower().StartsWith(value: text!, StringComparison.CurrentCultureIgnoreCase) == true;
	}

	public override async Task SetUser(User user)
	{
		user.ChangedTimetable += OnChangedTimetable;
		_studentSubjectCollection = user switch
		{
			Teacher teacher => new SubjectCollection(taughtSubjectCollection: await teacher.GetTaughtSubjects()),
			Student student => new SubjectCollection(studyingSubjectCollection: await student.GetStudyingSubjects()),
			Parent parent => new SubjectCollection(wardStudyingSubjectCollection: await parent.GetWardSubjectsStudying())
		};

		List<Subject> subjects = await _studentSubjectCollection.ToListAsync();
		_subjectsCache.Edit(updateAction: (a) => a.Load(items: subjects.Skip(count: 1)));
	}

	private async void OnChangedTimetable(ChangedTimetableEventArgs e)
		=> await LoadTimetable();
}