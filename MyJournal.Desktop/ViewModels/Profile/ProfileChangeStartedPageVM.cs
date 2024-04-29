using System.Collections.Generic;
using MyJournal.Desktop.Assets.Controls;
using MyJournal.Desktop.Models.Profile;

namespace MyJournal.Desktop.ViewModels.Profile;

public sealed class ProfileChangeStartedPageVM(ProfileChangeStartedPageModel model) : BaseVM(model: model)
{
	public int SelectedIndex
	{
		get => model.SelectedIndex;
		set => model.SelectedIndex = value;
	}

	public IEnumerable<BaseMenuItem> Menu
	{
		get => model.Menu;
		set => model.Menu = value;
	}
}