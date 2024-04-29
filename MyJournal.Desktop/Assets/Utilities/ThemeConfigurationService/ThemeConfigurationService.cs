using Avalonia;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using MyJournal.Desktop.Assets.Utilities.ConfigurationService;

namespace MyJournal.Desktop.Assets.Utilities.ThemeConfigurationService;

public sealed class ThemeConfigurationService(IConfigurationService configurationService) : IThemeConfigurationService
{
	public void ChangeTheme(ThemeVariant theme)
	{
		Application.Current!.RequestedThemeVariant = theme;
		IThemeConfigurationService.CurrentTheme = Application.Current.ActualThemeVariant;
		configurationService.Set(key: ConfigurationKeys.Theme, value: IThemeConfigurationService.CurrentTheme);
	}
}

public static class ThemeConfigurationServiceExtensions
{
	public static IServiceCollection AddThemeConfigurationService(this IServiceCollection serviceCollection)
		=> serviceCollection.AddTransient<IThemeConfigurationService, ThemeConfigurationService>();

	public static IServiceCollection AddKeyedThemeConfigurationService(this IServiceCollection serviceCollection, string key)
		=> serviceCollection.AddKeyedTransient<IThemeConfigurationService, ThemeConfigurationService>(serviceKey: key);
}