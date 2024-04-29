using System.Reactive;
using MyJournal.Desktop.Models.Profile;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Profile;

public sealed class ProfileFileStorageVM(ProfileFileStorageModel model) : BaseVM(model: model)
{
	public string Path
	{
		get => model.Path;
		set => model.Path = value;
	}

	public ReactiveCommand<Unit, Unit> ChangeFolder => model.ChangeFolder;
	public ReactiveCommand<Unit, Unit> ResetFolder => model.ResetFolder;
}