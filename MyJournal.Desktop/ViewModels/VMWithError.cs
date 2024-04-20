using MyJournal.Desktop.Models;

namespace MyJournal.Desktop.ViewModels;

public class VMWithError(ModelWithErrorMessage model) : BaseVM(model: model)
{
	public string Error
	{
		get => model.Error;
		set => model.Error = value;
	}

	public bool HaveError => model.HaveError;
}