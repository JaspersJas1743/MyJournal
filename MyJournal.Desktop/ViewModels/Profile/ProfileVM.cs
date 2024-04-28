using System.Reactive;
using System.Threading.Tasks;
using MyJournal.Core;
using MyJournal.Desktop.Models.Profile;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Profile;

public sealed class ProfileVM(ProfileModel model) : MenuItemVM(model: model)
{
	public BaseVM ProfilePhotoVM => model.ProfilePhotoVM;
	public BaseVM ProfileEmailVM => model.ProfileEmailVM;
	public BaseVM ProfilePhoneVM => model.ProfilePhoneVM;
	public BaseVM ProfileSessionsVM => model.ProfileSessionsVM;
	public BaseVM ProfileChangeMenuItemTypeVM => model.ProfileChangeMenuItemTypeVM;
	public BaseVM ProfileChangeThemeVM => model.ProfileChangeThemeVM;

	public ReactiveCommand<Unit, Unit> ClosedThisSession => model.CloseThisSession;

	public override async Task SetUser(User user)
		=> await model.SetUser(user: user);
}