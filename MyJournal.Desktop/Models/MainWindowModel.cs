using System;
using System.Reactive;
using Avalonia.Controls;
using MyJournal.Desktop.Assets.MessageBusEvents;
using MyJournal.Desktop.ViewModels;
using MyJournal.Desktop.Views;
using ReactiveUI;

namespace MyJournal.Desktop.Models;

public class MainWindowModel : ModelBase
{
	private BaseVM _mainVM;

	public MainWindowModel(WelcomeVM startedMainVM, MainWindowView mainWindowView)
	{
		MessageBus.Current.Listen<ChangeMainVMEventArgs>().Subscribe(onNext: args => MainVM = args.NewVM);

		Minimize = ReactiveCommand.Create(execute: () => mainWindowView.WindowState = WindowState.Minimized);
		Maximize = ReactiveCommand.Create(execute: () => mainWindowView.WindowState = WindowState.Maximized);
		Restore = ReactiveCommand.Create(execute: () => mainWindowView.WindowState = WindowState.Normal);
		Close = ReactiveCommand.Create(execute: mainWindowView.Close);
		MainVM = startedMainVM;
	}

	public ReactiveCommand<Unit, WindowState> Minimize { get; }
	public ReactiveCommand<Unit, WindowState> Maximize { get; }
	public ReactiveCommand<Unit, WindowState> Restore { get; }
	public ReactiveCommand<Unit, Unit> Close { get; }

	public BaseVM MainVM
	{
		get => _mainVM;
		private set => this.RaiseAndSetIfChanged(backingField: ref _mainVM, newValue: value);
	}
}