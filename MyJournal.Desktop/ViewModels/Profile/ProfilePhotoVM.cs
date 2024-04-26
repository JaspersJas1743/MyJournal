using System.Reactive;
using System.Threading.Tasks;
using MyJournal.Core;
using MyJournal.Desktop.Models.Profile;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Profile;

public sealed class ProfilePhotoVM(ProfilePhotoModel model) : MenuItemVM(model: model)
{
	public ReactiveCommand<Unit, Unit> ChangePhoto => model.ChangePhoto;
	public ReactiveCommand<Unit, Unit> DeletePhoto => model.DeletePhoto;

	public string NameAndPatronymic
	{
		get => model.NameAndPatronymic;
		set => model.NameAndPatronymic = value;
	}

	public string? Role
	{
		get => model.Role;
		set => model.Role = value;
	}

	public string? Photo
	{
		get => model.Photo;
		set => model.Photo = value;
	}

	public override async Task SetUser(User user)
		=> await model.SetUser(user: user);
}