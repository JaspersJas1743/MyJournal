using System.Collections.Generic;
using MyJournal.Core;
using MyJournal.Desktop.Assets.Controls;
using MyJournal.Desktop.Assets.Utilities;
using ReactiveUI;

namespace MyJournal.Desktop.Models;

public sealed class MainModel : ModelBase
{
	private User _user;
	private int _selectedIndex = 0;
	private MenuItem _selectedItem;
	private IEnumerable<MenuItem> _menu;

	public IEnumerable<MenuItem> Menu
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
		_user = user;
		Menu = RoleHelper.GetMenu(user: user);
		SelectedIndex = 0;
	}
}