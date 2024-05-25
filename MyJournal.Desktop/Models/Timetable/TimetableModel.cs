using System.Reactive;
using System.Threading.Tasks;
using MyJournal.Core;
using MyJournal.Desktop.Assets.MessageBusEvents;
using MyJournal.Desktop.ViewModels.Timetable;
using ReactiveUI;

namespace MyJournal.Desktop.Models.Timetable;

public sealed class TimetableModel : BaseTimetableModel
{
	private BaseTimetableVM _content;
	private User _user;

	public TimetableModel(TimetableByDateVM timetableByDateVM)
	{
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
		await Content.SetUser(user: user);
	}
}