using System;
using System.Reactive;
using System.Threading.Tasks;
using System.Web;
using Avalonia.Controls.Notifications;
using Avalonia.Platform.Storage;
using MyJournal.Core;
using MyJournal.Core.UserData;
using MyJournal.Desktop.Assets.Utilities;
using MyJournal.Desktop.Assets.Utilities.FileService;
using MyJournal.Desktop.Assets.Utilities.NotificationService;
using ReactiveUI;

namespace MyJournal.Desktop.Models.Profile;

public sealed class ProfilePhotoModel : ModelBase
{
	private readonly IFileStorageService _fileStorageService;
	private readonly INotificationService _notificationService;

	private string _nameAndPatronymic = String.Empty;
	private string? _role = String.Empty;
	private string? _photo = String.Empty;
	private ProfilePhoto _profilePhoto;

	public ProfilePhotoModel(
		IFileStorageService fileStorageService,
		INotificationService notificationService
	)
	{
		_fileStorageService = fileStorageService;
		_notificationService = notificationService;

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
		IStorageFile? pickedFile = await _fileStorageService.OpenFile(fileTypes: new FilePickerFileType[] { FilePickerFileTypes.ImageAll });
		if (pickedFile is null)
			return;

		StorageItemProperties basicProperties = await pickedFile.GetBasicPropertiesAsync();
		if (basicProperties.Size / (1024f * 1024f) > 1)
		{
			await _notificationService.Show(
				title: "Слишком большой файл",
				content: "Максимальный размер изображения - 1Мбайт.",
				type: NotificationType.Warning
			);
			return;
		}

		await _profilePhoto.Update(pathToPhoto: HttpUtility.UrlDecode(pickedFile.Path.AbsolutePath));
	}

	private async Task DeleteUserPhoto()
		=> await _profilePhoto.Delete();
}