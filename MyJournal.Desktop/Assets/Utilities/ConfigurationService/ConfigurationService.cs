using System;

namespace MyJournal.Desktop.Assets.Utilities.ConfigurationService;

public sealed class ConfigurationService : IConfigurationService
{
	public T? Get<T>(string? key) where T : class =>
		throw new NotImplementedException();

	public string? Get(string? key) =>
		throw new NotImplementedException();

	public void Set(string key, object? value) =>
		throw new NotImplementedException();

	public void Set(string key, string? value) =>
		throw new NotImplementedException();
}