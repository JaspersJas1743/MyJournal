using System.Threading.Tasks;
using MyJournal.Core;
using MyJournal.Desktop.ViewModels.Profile;

namespace MyJournal.Desktop.Models.Profile;

public sealed class ProfileModel(ProfilePhotoVM profilePhotoVM) : ModelBase
{
	public ProfilePhotoVM ProfilePhotoVM { get; } = profilePhotoVM;

	public async Task SetUser(User user)
		=> await ProfilePhotoVM.SetUser(user: user);
}