using System.Threading.Tasks;
using MyJournal.Core;
using MyJournal.Desktop.ViewModels.Profile;

namespace MyJournal.Desktop.Models.Profile;

public sealed class ProfileModel(
	ProfilePhotoVM profilePhotoVM,
	ProfileEmailVM profileEmailVM,
	ProfilePhoneVM profilePhoneVM,
	ProfileSessionsVM profileSessionsVM
) : ModelBase
{
	public ProfilePhotoVM ProfilePhotoVM { get; } = profilePhotoVM;
	public ProfileEmailVM ProfileEmailVM { get; } = profileEmailVM;
	public ProfilePhoneVM ProfilePhoneVM { get; } = profilePhoneVM;
	public ProfileSessionsVM ProfileSessionsVM { get; } = profileSessionsVM;

	public async Task SetUser(User user)
	{
		await ProfilePhotoVM.SetUser(user: user);
		await ProfilePhoneVM.SetUser(user: user);
		await ProfileEmailVM.SetUser(user: user);
		await ProfileSessionsVM.SetUser(user: user);
	}
}