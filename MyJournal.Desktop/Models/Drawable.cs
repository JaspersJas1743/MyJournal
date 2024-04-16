using Avalonia;
using MyJournal.Desktop.ViewModels;

namespace MyJournal.Desktop.Models;

public class Drawable : ModelBase
{
	public required WelcomeModel Presenter { get; set; }

	protected void MoveTo<VM>() where VM : Renderer
	{
		VM vm = (Application.Current as App)!.GetService<VM>();
		vm.Presenter = Presenter;
		Presenter.Content = vm;
	}
}