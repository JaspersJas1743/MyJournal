using MyJournal.Desktop.Models;

namespace MyJournal.Desktop.ViewModels;

public class WelcomeVM(WelcomeModel model) : BaseVM(model: model)
{
	public BaseVM Content
	{
		get => model.Content;
		set => model.Content = value;
	}
}