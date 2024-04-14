using MyJournal.Desktop.Models;

namespace MyJournal.Desktop.ViewModels;

public class Renderer(Drawable model) : BaseVM(model: model)
{
	public WelcomeModel Presenter
	{
		get => model.Presenter;
		set => model.Presenter = value;
	}
}