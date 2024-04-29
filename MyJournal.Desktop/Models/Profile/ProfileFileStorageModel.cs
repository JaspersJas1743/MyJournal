using System;
using System.Reactive;
using System.Threading.Tasks;
using System.Web;
using Avalonia.Platform.Storage;
using MyJournal.Desktop.Assets.Utilities.ConfigurationService;
using MyJournal.Desktop.Assets.Utilities.FileService;
using ReactiveUI;

namespace MyJournal.Desktop.Models.Profile;

public sealed class ProfileFileStorageModel : ModelBase
{
	private const string Downloads = "Downloads";
	private const string ApplicationFolder = "MyJournal";
	private readonly IConfigurationService _configurationService;
	private readonly IFileStorageService _fileStorageService;

	private string _path = String.Empty;

	public ProfileFileStorageModel(
		IConfigurationService configurationService,
		IFileStorageService fileStorageService
	)
	{
		_configurationService = configurationService;
		_fileStorageService = fileStorageService;

		ChangeFolder = ReactiveCommand.CreateFromTask(execute: ChangeFolderForDownloads);
		ResetFolder = ReactiveCommand.Create(execute: ResetFolderForDownloads);

		Path = _configurationService.Get(key: ConfigurationKeys.StorageFolder)!;
		if (String.IsNullOrWhiteSpace(value: Path))
			ResetFolderForDownloads();
	}

	private void ResetFolderForDownloads()
	{
		Path = System.IO.Path.Combine(
			path1: Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
			path2: Downloads,
			path3: ApplicationFolder
		).Replace(oldChar: '\\', newChar: '/');

		_configurationService.Set(key: ConfigurationKeys.StorageFolder, value: Path);
	}

	private async Task ChangeFolderForDownloads()
	{
		IStorageFolder? pickedFolder = await _fileStorageService.OpenFolder();
		if (pickedFolder is null)
			return;

		Path = HttpUtility.UrlDecode(pickedFolder.Path.AbsolutePath);
		_configurationService.Set(key: ConfigurationKeys.StorageFolder, value: Path);
	}

	public ReactiveCommand<Unit, Unit> ChangeFolder { get; }
	public ReactiveCommand<Unit, Unit> ResetFolder { get; }

	public string Path
	{
		get => _path;
		set => this.RaiseAndSetIfChanged(backingField: ref _path, newValue: value);
	}
}