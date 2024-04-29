using System.Reactive;
using System.Threading.Tasks;
using MyJournal.Core;
using MyJournal.Desktop.Models.Profile;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Profile;

public sealed class ProfileSecurityVM(ProfileSecurityModel model) : MenuItemWithErrorVM(model: model)
{
	public ReactiveCommand<Unit, Unit> ChangePassword => model.ChangePassword;

	public string CurrentPassword
	{
		get => model.CurrentPassword;
		set => model.CurrentPassword = value;
	}

	public string NewPassword
	{
		get => model.NewPassword;
		set => model.NewPassword = value;
	}

	public string NewPasswordConfirmation
	{
		get => model.NewPasswordConfirmation;
		set => model.NewPasswordConfirmation = value;
	}

	public override async Task SetUser(User user)
		=> await model.SetUser(user: user);
}