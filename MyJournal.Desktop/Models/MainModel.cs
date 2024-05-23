using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MyJournal.Core;
using MyJournal.Core.Authorization;
using MyJournal.Desktop.Assets.Controls;
using MyJournal.Desktop.Assets.Utilities;
using MyJournal.Desktop.Assets.Utilities.ConfigurationService;
using MyJournal.Desktop.Assets.Utilities.MenuConfigurationService;
using ReactiveUI;

namespace MyJournal.Desktop.Models;

public sealed class MainModel : ModelBase
{
	private readonly IConfigurationService _configurationService;

	private int _selectedIndex = 0;
	private MenuItem _selectedItem;
	private ObservableCollection<MenuItem> _menu;

	public MainModel(IConfigurationService configurationService)
	{
		_configurationService = configurationService;

		IMenuConfigurationService.ChangeMenuItemsType += OnChangeMenuItemsType;
	}

	~MainModel()
		=> IMenuConfigurationService.ChangeMenuItemsType -= OnChangeMenuItemsType;

	private void OnChangeMenuItemsType(ChangeMenuItemsTypeEventArgs e)
	{
		foreach (MenuItem menuItem in Menu)
			menuItem.ItemType = e.MenuItemTypes;
	}

	public ObservableCollection<MenuItem> Menu
	{
		get => _menu;
		set => this.RaiseAndSetIfChanged(backingField: ref _menu, newValue: value);
	}

	public MenuItem SelectedItem
	{
		get => _selectedItem;
		set => this.RaiseAndSetIfChanged(backingField: ref _selectedItem, newValue: value);
	}

	public int SelectedIndex
	{
		get => _selectedIndex;
		set => this.RaiseAndSetIfChanged(backingField: ref _selectedIndex, newValue: value);
	}

	public async Task SetAuthorizedUser(Authorized<User> user)
	{
		Menu = new ObservableCollection<MenuItem>(collection: await RoleHelper.GetMenu(user: user));
		SelectedIndex = Int32.Parse(s: _configurationService.Get(key: ConfigurationKeys.StartedPage)!);
	}
}