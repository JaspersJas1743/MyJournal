using System.Threading.Tasks;
using MyJournal.Core;
using MyJournal.Desktop.Models.Profile;

namespace MyJournal.Desktop.ViewModels.Profile;

public sealed class ProfileVM(ProfileModel model) : MenuItemVM(model: model)
{
	public BaseVM ProfilePhotoVM => model.ProfilePhotoVM;
	public BaseVM ProfileEmailVM => model.ProfileEmailVM;
	public BaseVM ProfilePhoneVM => model.ProfilePhoneVM;
	public BaseVM ProfileSessionsVM => model.ProfileSessionsVM;

	public override async Task SetUser(User user)
		=> await model.SetUser(user: user);
}