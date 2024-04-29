using System;
using System.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MyJournal.Desktop.Assets.Utilities.ConfigurationService;

public sealed class ConfigurationService : IConfigurationService
{
	private readonly Configuration _configuration = ConfigurationManager.OpenExeConfiguration(userLevel: ConfigurationUserLevel.None);

	public T? Get<T>(ConfigurationKeys? key) where T : class
		=> Convert.ChangeType(value: Get(key: key), conversionType: typeof(T)) as T;

	public string? Get(ConfigurationKeys? key)
		=> _configuration.AppSettings.Settings[key: key.ToString()].Value;

	public void Set(ConfigurationKeys key, object? value)
		=> Set(key: key, value: value?.ToString());

	public void Set(ConfigurationKeys key, string? value)
	{
		_configuration.AppSettings.Settings[key: key.ToString()].Value = value;
		_configuration.Save();
		ConfigurationManager.RefreshSection(sectionName: _configuration.AppSettings.SectionInformation.Name);
	}
}

public static class ConfigurationServiceExtensions
{
	public static IServiceCollection AddConfigurationService(this IServiceCollection serviceCollection)
		=> serviceCollection.AddSingleton<IConfigurationService, ConfigurationService>();

	public static IServiceCollection AddKeyedConfigurationService(this IServiceCollection serviceCollection, string key)
		=> serviceCollection.AddKeyedSingleton<IConfigurationService, ConfigurationService>(serviceKey: key);
}