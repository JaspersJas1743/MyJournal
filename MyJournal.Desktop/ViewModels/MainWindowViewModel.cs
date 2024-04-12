using MyJournal.Desktop.Models;
using MyJournal.Desktop.Views;

namespace MyJournal.Desktop.ViewModels;

public class MainWindowViewModel : ViewModelBase<MainWindowView>
{
	private readonly MainWindowModel _model;

	public MainWindowViewModel(MainWindowModel model)
	{
		_model = model;
	}

	public MainViewModel MainViewModel => _model.MainViewModel;
}