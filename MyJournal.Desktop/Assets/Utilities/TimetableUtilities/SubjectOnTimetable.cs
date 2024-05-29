using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using DynamicData.Binding;
using MyJournal.Desktop.Assets.MessageBusEvents;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Utilities.TimetableUtilities;

public sealed class SubjectOnTimetable : ReactiveObject
{
	private int? _number;
	private TimeSpan? _start;
	private TimeSpan? _end;
	private Subject? _selectedSubject;
	private readonly int? _initializedNumber;
	private readonly TimeSpan? _initializedStart;
	private readonly TimeSpan? _initializedEnd;
	private readonly Subject? _initializedSubject;
	private double? _break;
	private readonly Core.SubEntities.DayOfWeek _dayOfWeek;
	private readonly int _classId;

	public SubjectOnTimetable
	(
		int? number,
		IEnumerable<Subject> possibleSubjects,
		Core.SubEntities.DayOfWeek dayOfWeek,
		int classId
	)
	{
		_initializedNumber = Number = number;
		_dayOfWeek = dayOfWeek;
		_classId = classId;
		PossibleSubjects = new ObservableCollectionExtended<Subject>(collection: possibleSubjects);

		RemoveSubject = ReactiveCommand.Create(execute: RemoveSubjectHandler);

		this.WhenAnyValue(property1: s => s.Start)
			.WhereNotNull().Subscribe(onNext: StartTimeChangedHandler);

		this.WhenAnyValue(property1: s => s.End)
			.WhereNotNull().Subscribe(onNext: EndTimeChangedHandler);

		this.WhenAnyValue(property1: s => s.Number).WhereNotNull().Subscribe(onNext: newNumber =>
		{
			MessageBus.Current.SendMessage(message: new ChangeNumberOfSubjectOnTimetableEventArgs(
				classId: _classId,
				dayOfWeekId: _dayOfWeek.Id,
				subject: this
			));
		});
	}

	public SubjectOnTimetable(
		int? number,
		TimeSpan? start,
		TimeSpan? end,
		int? @break,
		int selectedSubjectId,
		IEnumerable<Subject> possibleSubjects,
		Core.SubEntities.DayOfWeek dayOfWeek,
		int classId
	) : this(
		number: number,
		possibleSubjects: possibleSubjects,
		dayOfWeek: dayOfWeek,
		classId: classId
	)
	{
		_initializedStart = Start = start;
		_initializedEnd = End = end;
		Break = @break;
		_initializedSubject = SelectedSubject = PossibleSubjects.FirstOrDefault(predicate: s => s.Id == selectedSubjectId);
	}

	public ObservableCollectionExtended<Subject> PossibleSubjects { get; }

	public ObservableCollectionExtended<int> PossibleNumbers { get; } =
		new ObservableCollectionExtended<int>(collection: Enumerable.Range(start: 1, count: 8));

	public int? Number
	{
		get => _number;
		set => this.RaiseAndSetIfChanged(backingField: ref _number, newValue: value);
	}

	public TimeSpan? Start
	{
		get => _start;
		set => this.RaiseAndSetIfChanged(backingField: ref _start, newValue: value);
	}

	public TimeSpan? End
	{
		get => _end;
		set => this.RaiseAndSetIfChanged(backingField: ref _end, newValue: value);
	}

	public Subject? SelectedSubject
	{
		get => _selectedSubject;
		set => this.RaiseAndSetIfChanged(backingField: ref _selectedSubject, newValue: value);
	}

	public double? Break
	{
		get => _break;
		set => this.RaiseAndSetIfChanged(backingField: ref _break, newValue: value);
	}

	public ReactiveCommand<Unit, Unit> RemoveSubject { get; }

	public bool GetHaveChange()
		=> GetNumberChanged() || GetStartChanged() || GetEndChanged() || GetSubjectChanged();

	private bool GetNumberChanged()
		=> Number != _initializedNumber;

	private bool GetStartChanged()
		=> Start != _initializedStart;

	private bool GetEndChanged()
		=> End != _initializedEnd;

	private bool GetSubjectChanged()
		=> SelectedSubject != _initializedSubject;

	private void EndTimeChangedHandler(TimeSpan? time)
	{
		MessageBus.Current.SendMessage(message: new SetEndTimeToSubjectOnTimetableEventArgs(
			endTime: time!.Value,
			changedSubject: this,
			dayOfWeek: _dayOfWeek,
			classId: _classId
		));
	}

	private void StartTimeChangedHandler(TimeSpan? time)
	{
		MessageBus.Current.SendMessage(message: new SetStartTimeToSubjectOnTimetableEventArgs(
			startTime: time!.Value,
			changedSubject: this,
			dayOfWeek: _dayOfWeek,
			classId: _classId
		));
	}

	private void RemoveSubjectHandler()
		=> MessageBus.Current.SendMessage(message: new RemoveSubjectOnTimetableEventArgs(subjectToRemove: this, dayOfWeek: _dayOfWeek, classId: _classId));
}