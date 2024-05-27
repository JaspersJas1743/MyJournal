using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData.Binding;
using MyJournal.Desktop.Assets.MessageBusEvents;
using ReactiveUI;
using DayOfWeek = MyJournal.Core.SubEntities.DayOfWeek;

namespace MyJournal.Desktop.Assets.Utilities.TimetableUtilities;

public class CreatingTimetable : ReactiveObject
{
	private const double AcademicHourInMinutes = 45;
	private readonly IEnumerable<Subject> _possibleSubjects;
	private readonly int _classId;
	private DayOfWeek _dayOfWeek;
	private double _totalHours;
	private bool _canAddSubject = false;

	public CreatingTimetable(
		DayOfWeek dayOfWeek,
		double totalHours,
		IEnumerable<Subject> possibleSubjects,
		IEnumerable<SubjectOnTimetable> subjects,
		int classId
	)
	{
		AddSubject = ReactiveCommand.Create(execute: AddSubjectHandler);

		_classId = classId;
		_possibleSubjects = possibleSubjects;
		_dayOfWeek = dayOfWeek;
		_totalHours = totalHours;
		Subjects = new ObservableCollectionExtended<SubjectOnTimetable>(collection: subjects);
		CanAddSubject = Subjects.Count < 8;
		Subjects.CollectionChanged += OnSubjectsChanged;

		MessageBus.Current.Listen<RemoveSubjectOnTimetableEventArgs>()
			.Where(predicate: e => e.ClassId == _classId)
			.Where(predicate: e => e.DayOfWeek.Id == _dayOfWeek.Id)
			.Distinct()
			.Subscribe(onNext: RemoveSubjectHandler);

		MessageBus.Current.Listen<SetStartTimeToSubjectOnTimetableEventArgs>()
			.Where(predicate: e => e.ClassId == _classId)
			.Where(predicate: e => e.DayOfWeek.Id == _dayOfWeek.Id)
			.Distinct()
			.Subscribe(onNext: SetBreakAfterChangedStartTimeOfSubjectHandler);

		MessageBus.Current.Listen<SetEndTimeToSubjectOnTimetableEventArgs>()
			.Where(predicate: e => e.ClassId == _classId)
			.Where(predicate: e => e.DayOfWeek.Id == _dayOfWeek.Id)
			.Distinct()
			.Subscribe(onNext: SetBreakAfterChangedEndTimeOfSubjectHandler);
	}

	private void SetBreakAfterChangedStartTimeOfSubjectHandler(SetStartTimeToSubjectOnTimetableEventArgs e)
	{
		int changedSubjectIndex = Subjects.IndexOf(item: e.Subject);
		e.Subject.End ??= e.Start.Add(ts: TimeSpan.FromMinutes(value: 45));
		if (e.Subject.Start >= e.Subject.End)
			e.Subject.Start = e.Subject.End;

		CalculateHours();
		if (changedSubjectIndex <= 0)
			return;

		SubjectOnTimetable previousSubject = Subjects[changedSubjectIndex - 1];
		previousSubject.Break = (e.Start - previousSubject.End)?.TotalMinutes;
	}

	private void SetBreakAfterChangedEndTimeOfSubjectHandler(SetEndTimeToSubjectOnTimetableEventArgs e)
	{
		int changedSubjectIndex = Subjects.IndexOf(item: e.Subject);
		e.Subject.Start ??= e.End.Add(ts: TimeSpan.FromMinutes(value: -45));
		if (e.Subject.Start >= e.Subject.End)
			e.Subject.End = e.Subject.Start;

		CalculateHours();
		if (changedSubjectIndex >= Subjects.Count - 1)
			return;

		SubjectOnTimetable nextSubject = Subjects[changedSubjectIndex + 1];
		e.Subject.Break = (nextSubject.Start - e.End)?.TotalMinutes;
	}

	private void CalculateHours()
		=> TotalHours = Subjects.Sum(selector: s => ((s.End - s.Start)?.TotalMinutes / AcademicHourInMinutes) ?? 0);

	private void OnSubjectsChanged(object? sender, NotifyCollectionChangedEventArgs e)
		=> CanAddSubject = Subjects.Count < 8;

	public DayOfWeek DayOfWeek
	{
		get => _dayOfWeek;
		set => this.RaiseAndSetIfChanged(backingField: ref _dayOfWeek, newValue: value);
	}

	public double TotalHours
	{
		get => _totalHours;
		set => this.RaiseAndSetIfChanged(backingField: ref _totalHours, newValue: value);
	}

	public bool CanAddSubject
	{
		get => _canAddSubject;
		set => this.RaiseAndSetIfChanged(backingField: ref _canAddSubject, newValue: value);
	}

	public ObservableCollectionExtended<SubjectOnTimetable> Subjects { get; }

	public ReactiveCommand<Unit, Unit> AddSubject { get; }

	private void AddSubjectHandler()
	{
		Subjects.Add(item: new SubjectOnTimetable(
			number: (Subjects.Any() ? Subjects.Last().Number : 0) + 1,
			possibleSubjects: _possibleSubjects,
			dayOfWeek: _dayOfWeek,
			classId: _classId
		));
	}

	private void RemoveSubjectHandler(RemoveSubjectOnTimetableEventArgs e)
	{
		Subjects.Remove(item: e.SubjectToRemove);
		CalculateHours();
	}
}