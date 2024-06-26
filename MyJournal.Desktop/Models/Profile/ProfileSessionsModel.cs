using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyJournal.Core;
using MyJournal.Core.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.UserData;
using MyJournal.Core.Utilities.EventArgs;
using MyJournal.Desktop.Assets.MessageBusEvents;
using MyJournal.Desktop.Assets.Utilities.CredentialStorageService;
using MyJournal.Desktop.ViewModels;
using ReactiveUI;

namespace MyJournal.Desktop.Models.Profile;

public sealed class ProfileSessionsModel : ModelBase
{
	private readonly ICredentialStorageService _credentialStorageService;
	private ObservableCollectionExtended<Session> _sessions = new ObservableCollectionExtended<Session>();
	private User _user;
	private SessionCollection _sessionCollection;

	public ProfileSessionsModel(ICredentialStorageService credentialStorageService)
	{
		_credentialStorageService = credentialStorageService;

		CloseAllSessions = ReactiveCommand.CreateFromTask(execute: CloseAll);
		CloseOtherSessions = ReactiveCommand.CreateFromTask(execute: CloseOther);
	}

	public ReactiveCommand<Unit, Unit> CloseAllSessions { get; }
	public ReactiveCommand<Unit, Unit> CloseOtherSessions { get; }

	public ObservableCollectionExtended<Session> Sessions
	{
		get => _sessions;
		set => this.RaiseAndSetIfChanged(backingField: ref _sessions, newValue: value);
	}

	public async Task SetUser(User user)
	{
		_user = user;
		Security security = await user.GetSecurity();
		_sessionCollection = await security.GetSessions();
		_sessionCollection.ClosedSession += OnClosedSessions;
		_sessionCollection.CreatedSession += OnCreatedSession;
		Sessions.Load(items: await _sessionCollection.ToListAsync());
	}

	private async void OnCreatedSession(CreatedSessionEventArgs e)
		=> Sessions.Add(item: await _sessionCollection.FindById(id: e.SessionId));

	private void OnClosedSessions(ClosedSessionEventArgs e)
	{
		if (e.CurrentSessionAreClosed)
		{
			MessageBus.Current.SendMessage(message: new ChangeMainWindowVMEventArgs(
				newVMType: typeof(WelcomeVM), animationType: AnimationType.CrossFade
			));
			_credentialStorageService.Remove();
		}

		Sessions.RemoveMany(itemsToRemove: Sessions.Where(predicate: s => e.SessionIds.Contains(value: s.Id)));
	}

	private async Task CloseOther()
		=> await _sessionCollection.CloseOthers();

	private async Task CloseAll()
	{
		Activity activity = await _user.GetActivity();
		await activity.SetOffline();
		await _sessionCollection.CloseAll();
	}
}