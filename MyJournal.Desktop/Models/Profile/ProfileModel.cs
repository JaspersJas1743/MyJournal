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
	private User _user;

	private readonly ICredentialStorageService _credentialStorageService;

	private SessionCollection _sessionCollection;

	public ProfileModel(
		ICredentialStorageService credentialStorageService,
		ProfilePhotoVM profilePhotoVM,
		ProfileEmailVM profileEmailVM,
		ProfilePhoneVM profilePhoneVM,
		ProfileSessionsVM profileSessionsVM,
		ProfileChangeMenuItemTypeVM profileChangeMenuItemTypeVM,
		ProfileChangeThemeVM profileChangeThemeVM,
		ProfileFileStorageVM profileFileStorageVM,
		ProfileSecurityVM profileSecurityVM,
		ProfileChangeStartedPageVM profileChangeStartedPageVM
	)
	{
		_credentialStorageService = credentialStorageService;

		ProfilePhotoVM = profilePhotoVM;
		ProfileEmailVM = profileEmailVM;
		ProfilePhoneVM = profilePhoneVM;
		ProfileSessionsVM = profileSessionsVM;
		ProfileChangeMenuItemTypeVM = profileChangeMenuItemTypeVM;
		ProfileChangeThemeVM = profileChangeThemeVM;
		ProfileFileStorageVM = profileFileStorageVM;
		ProfileSecurityVM = profileSecurityVM;
		ProfileChangeStartedPageVM = profileChangeStartedPageVM;

		CloseThisSession = ReactiveCommand.CreateFromTask(execute: CloseThis);
	}

	private async Task CloseThis()
	{
		Activity activity = await _user.GetActivity();
		await activity.SetOffline();
		await _sessionCollection.CloseThis();
	}

	public ProfilePhotoVM ProfilePhotoVM { get; }
	public ProfileEmailVM ProfileEmailVM { get; }
	public ProfilePhoneVM ProfilePhoneVM { get; }
	public ProfileSessionsVM ProfileSessionsVM { get; }
	public ProfileChangeMenuItemTypeVM ProfileChangeMenuItemTypeVM { get; }
	public ProfileChangeThemeVM ProfileChangeThemeVM { get; }
	public ProfileFileStorageVM ProfileFileStorageVM { get; }
	public ProfileSecurityVM ProfileSecurityVM { get; }
	public ProfileChangeStartedPageVM ProfileChangeStartedPageVM { get; }

	public ReactiveCommand<Unit, Unit> CloseThisSession { get; }

	public async Task SetUser(User user)
	{
		await Task.WhenAll(
			ProfilePhotoVM.SetUser(user: user),
			ProfilePhoneVM.SetUser(user: user),
			ProfileEmailVM.SetUser(user: user),
			ProfileSessionsVM.SetUser(user: user),
			ProfileSecurityVM.SetUser(user: user)
		);

		_user = user;
		Security security = await user.GetSecurity();
		_sessionCollection = await security.GetSessions();
	}
}