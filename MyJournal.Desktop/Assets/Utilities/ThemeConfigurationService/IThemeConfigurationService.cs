using Avalonia.Styling;

namespace MyJournal.Desktop.Assets.Utilities.ThemeConfigurationService;

public interface IThemeConfigurationService
{
	public static ThemeVariant CurrentTheme { get; set; }
	
	public void ChangeTheme(ThemeVariant theme);
}