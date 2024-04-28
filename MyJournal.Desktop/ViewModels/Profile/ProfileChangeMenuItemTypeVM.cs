using System.Reactive;
using MyJournal.Desktop.Models.Profile;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Profile;

public sealed class ProfileChangeMenuItemTypeVM(ProfileChangeMenuItemTypeModel model) : BaseVM(model: model)
{
	public bool ShortTypeIsSelected
	{
		get => model.ShortTypeIsSelected;
		set => model.ShortTypeIsSelected = value;
	}

	public bool FullTypeIsSelected
	{
		get => model.FullTypeIsSelected;
		set => model.FullTypeIsSelected = value;
	}

	public ReactiveCommand<Unit, Unit> SelectedShortType => model.SelectedShortType;
	public ReactiveCommand<Unit, Unit> SelectedFullType => model.SelectedFullType;
}