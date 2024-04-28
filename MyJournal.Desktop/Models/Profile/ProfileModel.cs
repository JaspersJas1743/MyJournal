using System.Reactive;
using System.Threading.Tasks;
using MyJournal.Core;
using MyJournal.Core.Collections;
using MyJournal.Core.UserData;
using MyJournal.Desktop.Assets.Utilities.CredentialStorageService;
using MyJournal.Desktop.ViewModels.Profile;
using ReactiveUI;

namespace MyJournal.Desktop.Models.Profile;

public sealed class ProfileModel : ModelBase
{
	private readonly ICredentialStorageService _credentialStorageService;

	private SessionCollection _sessionCollection;

	public ProfileModel(
		ProfilePhotoVM profilePhotoVM,
		ProfileEmailVM profileEmailVM,
		ProfilePhoneVM profilePhoneVM,
		ProfileSessionsVM profileSessionsVM,
		ICredentialStorageService credentialStorageService
	)
	{
		_credentialStorageService = credentialStorageService;

		ProfilePhotoVM = profilePhotoVM;
		ProfileEmailVM = profileEmailVM;
		ProfilePhoneVM = profilePhoneVM;
		ProfileSessionsVM = profileSessionsVM;

		CloseThisSession = ReactiveCommand.CreateFromTask(execute: CloseThis);
	}

	private async Task CloseThis()
	{
		await _sessionCollection.CloseThis();
		_credentialStorageService.Remove();
	}

	public ProfilePhotoVM ProfilePhotoVM { get; }
	public ProfileEmailVM ProfileEmailVM { get; }
	public ProfilePhoneVM ProfilePhoneVM { get; }
	public ProfileSessionsVM ProfileSessionsVM { get; }

	public ReactiveCommand<Unit, Unit> CloseThisSession { get; }

	public async Task SetUser(User user)
	{
		await ProfilePhotoVM.SetUser(user: user);
		await ProfilePhoneVM.SetUser(user: user);
		await ProfileEmailVM.SetUser(user: user);
		await ProfileSessionsVM.SetUser(user: user);

		Security security = await user.GetSecurity();
		_sessionCollection = await security.GetSessions();
	}
}