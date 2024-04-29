using System;
using Microsoft.Extensions.DependencyInjection;
using MyJournal.Desktop.Assets.Controls;
using MyJournal.Desktop.Assets.Utilities.ConfigurationService;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Utilities.MenuConfigurationService;

public sealed class MenuConfigurationService : IMenuConfigurationService
{
	private readonly IConfigurationService _configurationService;

	public MenuConfigurationService(IConfigurationService configurationService)
	{
		_configurationService = configurationService;
		IMenuConfigurationService.CurrentType = Enum.Parse<MenuItemTypes>(value: _configurationService.Get(key: ConfigurationKeys.MenuType) ?? nameof(MenuItemTypes.Full));
	}

	public void ChangeType(MenuItemTypes type)
	{
		MessageBus.Current.SendMessage(new ChangeMenuItemsTypeEventArgs(menuItemTypes: type));
		_configurationService.Set(key: ConfigurationKeys.MenuType, value: type);
		IMenuConfigurationService.InvokeChangeMenuItemsTypeEvent(e: new ChangeMenuItemsTypeEventArgs(menuItemTypes: type));
	}
}

public static class MenuConfigurationServiceExtensions
{
	public static IServiceCollection AddMenuConfigurationService(this IServiceCollection serviceCollection)
		=> serviceCollection.AddTransient<IMenuConfigurationService, MenuConfigurationService>();

	public static IServiceCollection AddKeyedMenuConfigurationService(this IServiceCollection serviceCollection, string key)
		=> serviceCollection.AddKeyedTransient<IMenuConfigurationService, MenuConfigurationService>(serviceKey: key);
}