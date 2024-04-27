using System.Reactive;
using System.Threading.Tasks;
using MyJournal.Core;
using MyJournal.Desktop.Models.Profile;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Profile;

public sealed class ProfilePhoneVM(ProfilePhoneModel model) : MenuItemWithErrorVM(model: model)
{
	public string? EnteredPhone
	{
		get => model.EnteredPhone;
		set => model.EnteredPhone = value;
	}

	public ReactiveCommand<Unit, Unit> ChangePhone => model.ChangePhone;

	public bool PhoneIsVerified => model.PhoneIsVerified;

	public override async Task SetUser(User user)
		=> await model.SetUser(user: user);
}