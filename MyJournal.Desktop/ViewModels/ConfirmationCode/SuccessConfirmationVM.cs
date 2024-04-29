using MyJournal.Desktop.Models.ConfirmationCode;

namespace MyJournal.Desktop.ViewModels.ConfirmationCode;

public sealed class SuccessConfirmationVM(SuccessConfirmationModel model) : BaseVM(model: model)
{
	public string Text
	{
		get => model.Text;
		set => model.Text = value;
	}
}