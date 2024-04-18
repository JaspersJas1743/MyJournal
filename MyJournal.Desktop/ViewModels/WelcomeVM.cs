using MyJournal.Desktop.Models;

namespace MyJournal.Desktop.ViewModels;

public class WelcomeVM(WelcomeModel model) : BaseVM(model: model)
{
	public BaseVM Content
	{
		get => model.Content;
		set => model.Content = value;
	}

	public bool HaveLeftDirection
	{
		get => model.HaveLeftDirection;
		set => model.HaveLeftDirection = value;
	}

	public bool HaveRightDirection
	{
		get => model.HaveRightDirection;
		set => model.HaveRightDirection = value;
	}
}