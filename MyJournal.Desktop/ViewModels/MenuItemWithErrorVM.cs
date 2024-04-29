using MyJournal.Desktop.Models;

namespace MyJournal.Desktop.ViewModels;

public abstract class MenuItemWithErrorVM(ModelWithErrorMessage model) : MenuItemVM(model: model)
{
	public string Error
	{
		get => model.Error;
		set => model.Error = value;
	}

	public bool HaveError
	{
		get => model.HaveError;
		set => model.HaveError = value;
	}
}