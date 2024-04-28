using System.Reactive;
using MyJournal.Desktop.Models.Profile;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Profile;

public sealed class ProfileChangeThemeVM(ProfileChangeThemeModel model) : BaseVM(model: model)
{
	public bool DarkThemeIsSelected
	{
		get => model.DarkThemeIsSelected;
		set => model.DarkThemeIsSelected = value;
	}

	public bool LightThemeIsSelected
	{
		get => model.LightThemeIsSelected;
		set => model.LightThemeIsSelected = value;
	}

	public ReactiveCommand<Unit, Unit> SelectedDarkTheme => model.SelectedDarkTheme;
	public ReactiveCommand<Unit, Unit> SelectedLightTheme => model.SelectedLightTheme;
}