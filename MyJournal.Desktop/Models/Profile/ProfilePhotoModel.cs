using System;
using System.Reactive;
using System.Threading.Tasks;
using System.Web;
using Avalonia.Platform.Storage;
using MyJournal.Core;
using MyJournal.Core.UserData;
using MyJournal.Desktop.Assets.Utilities;
using MyJournal.Desktop.Assets.Utilities.FileService;
using ReactiveUI;

namespace MyJournal.Desktop.Models.Profile;

public sealed class ProfilePhotoModel : ModelBase
{
	private readonly IFileStorageService _fileStorageService;

	private string _nameAndPatronymic = String.Empty;
	private string? _role = String.Empty;
	private string? _photo = String.Empty;
	private ProfilePhoto _profilePhoto;

	public ProfilePhotoModel(IFileStorageService fileStorageService)
	{
		_fileStorageService = fileStorageService;

		ChangePhoto = ReactiveCommand.CreateFromTask(execute: ChangeUserPhoto);
		DeletePhoto = ReactiveCommand.CreateFromTask(execute: DeleteUserPhoto);
	}

	public ReactiveCommand<Unit, Unit> ChangePhoto { get; }
	public ReactiveCommand<Unit, Unit> DeletePhoto { get; }

	public string NameAndPatronymic
	{
		get => _nameAndPatronymic;
		set => this.RaiseAndSetIfChanged(backingField: ref _nameAndPatronymic, newValue: value);
	}

	public string? Role
	{
		get => _role;
		set => this.RaiseAndSetIfChanged(backingField: ref _role, newValue: value);
	}

	public string? Photo
	{
		get => _photo;
		set => this.RaiseAndSetIfChanged(backingField: ref _photo, newValue: value);
	}

	public async Task SetUser(User user)
	{
		PersonalData data = await user.GetPersonalData();
		NameAndPatronymic = $"{data.Name} {data.Patronymic}";
		Role = RoleHelper.GetRoleName(user: user);
		_profilePhoto = await user.GetPhoto();
		Photo = _profilePhoto.Link;
		_profilePhoto.UpdatedProfilePhoto += args => Photo = args.Link;
	}

	private async Task ChangeUserPhoto()
	{
		IStorageFile? pickedFile = await _fileStorageService.OpenFile();
		if (pickedFile is null)
			return;

		await _profilePhoto.Update(pathToPhoto: HttpUtility.UrlDecode(pickedFile.Path.AbsolutePath));
	}

	private async Task DeleteUserPhoto()
		=> await _profilePhoto.Delete();
}