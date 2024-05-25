using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using MyJournal.Core;
using MyJournal.Core.Utilities.EventArgs;
using MyJournal.Desktop.Assets.MessageBusEvents;
using MyJournal.Desktop.Assets.Utilities.NotificationService;
using MyJournal.Desktop.ViewModels.Timetable;
using ReactiveUI;

namespace MyJournal.Desktop.Models.Timetable;

public sealed class TimetableModel : BaseTimetableModel
{
	private readonly INotificationService _notificationService;
	private BaseTimetableVM _content;
	private User _user;

	public TimetableModel(
		TimetableByDateVM timetableByDateVM,
		INotificationService notificationService
	)
	{
		_notificationService = notificationService;
		Content = timetableByDateVM;
		OnVisualizerChanged = ReactiveCommand.CreateFromTask<ChangeTimetableVisualizerEventArgs>(execute: async e =>
		{
			await e.TimetableVM.SetUser(user: _user!);
			Content = e.TimetableVM;
		});
		MessageBus.Current.Listen<ChangeTimetableVisualizerEventArgs>().InvokeCommand(command: OnVisualizerChanged);
	}

	public BaseTimetableVM Content
	{
		get => _content;
		set => this.RaiseAndSetIfChanged(backingField: ref _content, newValue: value);
	}

	public ReactiveCommand<ChangeTimetableVisualizerEventArgs, Unit> OnVisualizerChanged { get; }

	public override async Task SetUser(User user)
	{
		_user = user;
		_user.ChangedTimetable += OnChangedTimetable;
		await Content.SetUser(user: user);
	}

	private async void OnChangedTimetable(ChangedTimetableEventArgs e)
	{
		await _notificationService.Show(
			title: "Расписание",
			content: "В расписание были внесены изменения!",
			type: NotificationType.Information
		);
	}
}