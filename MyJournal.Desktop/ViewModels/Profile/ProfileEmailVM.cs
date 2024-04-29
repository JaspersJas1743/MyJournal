using System.Reactive;
using System.Threading.Tasks;
using MyJournal.Core;
using MyJournal.Desktop.Models.Profile;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Profile;

public sealed class ProfileEmailVM(ProfileEmailModel model) : MenuItemWithErrorVM(model: model)
{
	public string? EnteredEmail
	{
		get => model.EnteredEmail;
		set => model.EnteredEmail = value;
	}

	public ReactiveCommand<Unit, Unit> ChangeEmail => model.ChangeEmail;

	public bool EmailIsVerified => model.EmailIsVerified;

	public override async Task SetUser(User user)
		=> await model.SetUser(user: user);
}