using System.Collections.Generic;
using MyJournal.Desktop.Assets.Controls;
using MyJournal.Desktop.Models;

namespace MyJournal.Desktop.ViewModels;

public sealed class MainVM(MainModel model) : BaseVM(model: model)
{
	public IEnumerable<MenuItem> Menu
	{
		get => model.Menu;
		set => model.Menu = value;
	}

	public MenuItem SelectedItem
	{
		get => model.SelectedItem;
		set => model.SelectedItem = value;
	}
}