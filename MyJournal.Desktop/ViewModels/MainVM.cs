using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MyJournal.Core;
using MyJournal.Desktop.Assets.Controls;
using MyJournal.Desktop.Models;

namespace MyJournal.Desktop.ViewModels;

public sealed class MainVM(MainModel model) : BaseVM(model: model)
{
	public ObservableCollection<MenuItem> Menu
	{
		get => model.Menu;
		set => model.Menu = value;
	}

	public MenuItem SelectedItem
	{
		get => model.SelectedItem;
		set => model.SelectedItem = value;
	}

	public int SelectedIndex
	{
		get => model.SelectedIndex;
		set => model.SelectedIndex = value;
	}

	public async Task SetAuthorizedUser(User user)
		=> await model.SetAuthorizedUser(user: user);
}