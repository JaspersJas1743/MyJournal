using System.Collections.Generic;
using System.Linq;
using MyJournal.Core;
using MyJournal.Desktop.Assets.Controls;
using ReactiveUI;

namespace MyJournal.Desktop.Models;

public sealed class MainModel : ModelBase
{
	private User _user;
	private MenuItem _selectedItem;
	private IEnumerable<MenuItem> _menu;

	public MainModel()
	{
		Menu = new List<MenuItem>()
		{
			new MenuItem(image: "Login", header: "Профиль", itemContent: null),
			new MenuItem(image: "Messages", header: "Диалоги", itemContent: null),
			new MenuItem(image: "Tasks", header: "Задания", itemContent: null),
			new MenuItem(image: "Marks", header: "Оценки", itemContent: null),
			new MenuItem(image: "Schedule", header: "Занятия", itemContent: null)
		};
		SelectedItem = Menu.First();
	}

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
}