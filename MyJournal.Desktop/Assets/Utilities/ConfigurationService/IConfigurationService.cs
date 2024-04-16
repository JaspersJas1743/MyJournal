namespace MyJournal.Desktop.Assets.Utilities.ConfigurationService;

public interface IConfigurationService
{
	T? Get<T>(string? key) where T : class;
	string? Get(string? key);

	void Set(string key, object? value);
	void Set(string key, string? value);
}