using System.Reactive;
using MyJournal.Desktop.Models.Profile;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Profile;

public sealed class ProfileChangeMenuItemTypeVM(ProfileChangeMenuItemTypeModel model) : BaseVM(model: model)
{
	public bool CompactTypeIsSelected
	{
		get => model.CompactTypeIsSelected;
		set => model.CompactTypeIsSelected = value;
	}

	public bool FullTypeIsSelected
	{
		get => model.FullTypeIsSelected;
		set => model.FullTypeIsSelected = value;
	}

	public ReactiveCommand<Unit, Unit> SelectedCompactType => model.SelectedCompactType;
	public ReactiveCommand<Unit, Unit> SelectedFullType => model.SelectedFullType;
}