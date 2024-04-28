using System.Reactive;
using Avalonia.Styling;
using MyJournal.Desktop.Assets.Utilities.ThemeConfigurationService;
using ReactiveUI;

namespace MyJournal.Desktop.Models.Profile;

public sealed class ProfileChangeThemeModel : ModelBase
{
	private readonly IThemeConfigurationService _themeConfigurationService;

	private bool _darkThemeIsSelected;
	private bool _lightThemeIsSelected;

	public ProfileChangeThemeModel(IThemeConfigurationService themeConfigurationService)
	{
		_themeConfigurationService = themeConfigurationService;

		DarkThemeIsSelected = IThemeConfigurationService.CurrentTheme == ThemeVariant.Dark;
		LightThemeIsSelected = IThemeConfigurationService.CurrentTheme == ThemeVariant.Light;

		SelectedDarkTheme = ReactiveCommand.Create(execute: ChangeThemeToDark);
		SelectedLightTheme = ReactiveCommand.Create(execute: ChangeThemeToLight);
	}

	public bool DarkThemeIsSelected
	{
        get => _darkThemeIsSelected;
        set => this.RaiseAndSetIfChanged(backingField: ref _darkThemeIsSelected, newValue: value);
    }

	public bool LightThemeIsSelected
	{
        get => _lightThemeIsSelected;
        set => this.RaiseAndSetIfChanged(backingField: ref _lightThemeIsSelected, newValue: value);
    }

	private void ChangeThemeToLight()
		=> _themeConfigurationService.ChangeTheme(theme: ThemeVariant.Light);

	private void ChangeThemeToDark()
		=> _themeConfigurationService.ChangeTheme(theme: ThemeVariant.Dark);

	public ReactiveCommand<Unit, Unit> SelectedDarkTheme { get; }
	public ReactiveCommand<Unit, Unit> SelectedLightTheme { get; }
}