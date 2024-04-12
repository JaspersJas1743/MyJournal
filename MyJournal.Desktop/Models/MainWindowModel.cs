using MyJournal.Desktop.ViewModels;

namespace MyJournal.Desktop.Models;

public class MainWindowModel
{
	public MainWindowModel(MainViewModel model)
	{
		MainViewModel = model;
	}

	public MainViewModel MainViewModel { get; }
}