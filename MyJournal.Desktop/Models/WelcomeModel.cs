using MyJournal.Desktop.ViewModels;
using MyJournal.Desktop.ViewModels.Authorization;
using ReactiveUI;

namespace MyJournal.Desktop.Models;

public class WelcomeModel : ModelBase
{
	private BaseVM _content;

	public WelcomeModel(AuthorizationVM authorizationVM)
	{
		authorizationVM.Presenter = this;
		Content = authorizationVM;
	}

	public BaseVM Content
	{
		get => _content;
		set => this.RaiseAndSetIfChanged(backingField: ref _content, newValue: value);
	}
}