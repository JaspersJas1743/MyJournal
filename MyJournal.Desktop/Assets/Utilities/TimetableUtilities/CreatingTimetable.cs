using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Controls.Notifications;
using DynamicData.Binding;
using MyJournal.Desktop.Assets.MessageBusEvents;
using MyJournal.Desktop.Assets.Utilities.NotificationService;
using ReactiveUI;
using DayOfWeek = MyJournal.Core.SubEntities.DayOfWeek;

namespace MyJournal.Desktop.Assets.Utilities.TimetableUtilities;

public class CreatingTimetable : ReactiveObject
{
	private const double AcademicHourInMinutes = 45;
	private readonly INotificationService _notificationService;
	private readonly IEnumerable<Subject> _possibleSubjects;
	private readonly int _classId;
	private DayOfWeek _dayOfWeek;
	private double _totalHours;
	private bool _canAddSubject = false;

	public CreatingTimetable(
		INotificationService notificationService,
		DayOfWeek dayOfWeek,
		double totalHours,
		IEnumerable<Subject> possibleSubjects,
		IEnumerable<SubjectOnTimetable> subjects,
		int classId
	)
	{
		_notificationService = notificationService;
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

		MessageBus.Current.Listen<ChangeNumberOfSubjectOnTimetableEventArgs>()
			.Where(predicate: e => e.ClassId == _classId)
			.Where(predicate: e => e.DayOfWeekId == _dayOfWeek.Id)
			.Distinct()
			.Subscribe(onNext: ChangeNumberOfSubjectOnTimetableHandler);
	}

	private void ChangeNumberOfSubjectOnTimetableHandler(ChangeNumberOfSubjectOnTimetableEventArgs e)
	{
		SubjectOnTimetable[] sorted = Subjects.OrderBy(keySelector: s => s.Number).ToArray();
		if (sorted.SequenceEqual(second: Subjects))
			return;

		e.Subject.Start = null;
		e.Subject.End = null;

		for (int i = 0; i < sorted.Length - 1; i++)
		{
			SubjectOnTimetable current = sorted[i];
			SubjectOnTimetable next = sorted[i + 1];
			current.Break = (next.Start - current.End)?.TotalMinutes;
		}

		Subjects.Load(items: sorted);
	}

	private async void SetBreakAfterChangedStartTimeOfSubjectHandler(SetStartTimeToSubjectOnTimetableEventArgs e)
	{
		int changedSubjectIndex = Subjects.IndexOf(item: e.Subject);
		if (changedSubjectIndex < 0)
			return;

		if (e.Subject.Start >= e.Subject.End)
		{
			await _notificationService.Show(
				title: "Расписание",
				content: "Занятие не может закончиться до его начала.",
				type: NotificationType.Information
			);
			e.Subject.Start = null;
			return;
		}
		e.Subject.End ??= e.Start.Add(ts: TimeSpan.FromMinutes(value: 45));
		CalculateHours();

		if (changedSubjectIndex <= 0)
			return;

		SubjectOnTimetable previousSubject = Subjects[changedSubjectIndex - 1];
		double? @break = (e.Start - previousSubject.End)?.TotalMinutes;
		if (@break > 0 || previousSubject.End is null)
		{
			previousSubject.Break = @break;
			return;
		}

		await _notificationService.Show(
			title: "Расписание",
			content: "Занятие не может начинаться до окончания предыдующего.",
			type: NotificationType.Information
		);
		e.Subject.Start = null;
		previousSubject.Break = null;
		CalculateHours();
	}

	private async void SetBreakAfterChangedEndTimeOfSubjectHandler(SetEndTimeToSubjectOnTimetableEventArgs e)
	{
		int changedSubjectIndex = Subjects.IndexOf(item: e.Subject);
		if (changedSubjectIndex < 0)
			return;

		if (e.Subject.Start >= e.Subject.End)
		{
			await _notificationService.Show(
				title: "Расписание",
				content: "Занятие не может закончиться до его начала.",
				type: NotificationType.Information
			);
			e.Subject.End = null;
			return;
		}
		e.Subject.Start ??= e.End.Add(ts: TimeSpan.FromMinutes(value: -45));
		CalculateHours();

		if (changedSubjectIndex >= Subjects.Count - 1)
			return;

		SubjectOnTimetable nextSubject = Subjects[changedSubjectIndex + 1];
		double? @break = (nextSubject.Start - e.End)?.TotalMinutes;
		if (@break > 0 || nextSubject.Start is null)
		{
			e.Subject.Break = @break;
			return;
		}
		await _notificationService.Show(
			title: "Расписание",
			content: "Занятие не может заканчиваться после начала следующего.",
			type: NotificationType.Information
		);
		e.Subject.End = null;
		e.Subject.Break = null;
		CalculateHours();
	}

	private void CalculateHours()
	{
		TotalHours = Subjects.Where(predicate: s => s.Start is not null && s.End is not null).Sum(
			selector: s => (s.End - s.Start)!.Value.TotalMinutes / AcademicHourInMinutes
		);
	}

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

	public bool GetHaveChange() => Subjects.Any(predicate: s => s.GetHaveChange());

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