using System.Reactive;
using Avalonia.Controls;
using MyJournal.Desktop.Models;
using MyJournal.Desktop.Views;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels;

public class MainWindowViewModel : ViewModelBase<MainWindowView>
{
	private readonly MainWindowModel _model;

	public MainWindowViewModel(MainWindowModel model)
	{
		_model = model;
	}

	public ReactiveCommand<Unit, WindowState> Minimize => _model.Minimize;
	public ReactiveCommand<Unit, WindowState> Maximize => _model.Maximize;
	public ReactiveCommand<Unit, WindowState> Restore => _model.Restore;
	public ReactiveCommand<Unit, Unit> Close => _model.Close;
	public MainViewModel MainViewModel => _model.MainViewModel;
}