using MyJournal.Desktop.Models;

namespace MyJournal.Desktop.ViewModels;

public class WelcomeVM(WelcomeModel model) : BaseVM(model: model)
{
	public BaseVM Content => model.Content;

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