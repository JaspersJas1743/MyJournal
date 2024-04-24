using MyJournal.Desktop.Models;

namespace MyJournal.Desktop.ViewModels;

public class InitialLoadingVM(InitialLoadingModel model) : BaseVM(model: model)
{
	public string LoadingText
	{
		get => model.LoadingText;
		set => model.LoadingText = value;
	}

	public void StopTimer()
		=> model.StopTimer();
}