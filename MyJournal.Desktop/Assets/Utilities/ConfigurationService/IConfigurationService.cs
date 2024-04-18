namespace MyJournal.Desktop.Assets.Utilities.ConfigurationService;

public interface IConfigurationService
{
	T? Get<T>(ConfigurationKeys? key) where T : class;
	string? Get(ConfigurationKeys? key);

	void Set(ConfigurationKeys key, object? value);
	void Set(ConfigurationKeys key, string? value);
}