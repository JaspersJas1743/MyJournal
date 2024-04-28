using System;
using System.Collections.ObjectModel;
using MyJournal.Core;
using MyJournal.Desktop.Assets.Controls;
using MyJournal.Desktop.Assets.MessageBusEvents;
using MyJournal.Desktop.Assets.Utilities;
using ReactiveUI;

namespace MyJournal.Desktop.Models;

public sealed class MainModel : ModelBase
{
	private int _selectedIndex = 0;
	private MenuItem _selectedItem;
	private ObservableCollection<MenuItem> _menu;

	public MainModel()
	{
		MessageBus.Current.Listen<ChangeMenuItemTypesEventArgs>().Subscribe(onNext: args =>
		{
			foreach (MenuItem menuItem in Menu)
				menuItem.ItemType = args.MenuItemTypes;
		});
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

	public void SetAuthorizedUser(User user)
	{
		Menu = new ObservableCollection<MenuItem>(collection: RoleHelper.GetMenu(user: user));
		SelectedIndex = 0;
	}
}