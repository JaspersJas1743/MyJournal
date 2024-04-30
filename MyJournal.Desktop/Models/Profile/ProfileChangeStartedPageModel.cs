using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using MyJournal.Desktop.Assets.Controls;
using MyJournal.Desktop.Assets.Utilities;
using MyJournal.Desktop.Assets.Utilities.ConfigurationService;
using ReactiveUI;

namespace MyJournal.Desktop.Models.Profile;

public sealed class ProfileChangeStartedPageModel : ModelBase
{
	private readonly IConfigurationService _configurationService;

	private int _selectedIndex;
	private IEnumerable<BaseMenuItem> _menu;

	public ProfileChangeStartedPageModel(IConfigurationService configurationService)
	{
		_configurationService = configurationService;

		SelectedIndex = Int32.Parse(s: _configurationService.Get(key: ConfigurationKeys.StartedPage)!);
		Menu = RoleHelper.GetBaseMenu();
		this.WhenAnyValue(property1: model => model.SelectedIndex)
			.Where(predicate: index => index >= 0)
			.Subscribe(onNext: index => _configurationService.Set(key: ConfigurationKeys.StartedPage, value: index));

		OnLayoutUpdated = ReactiveCommand.Create(execute: SetSelectedIndex);
	}

	private void SetSelectedIndex()
	{
		if (SelectedIndex < 0)
			SelectedIndex = Int32.Parse(s: _configurationService.Get(key: ConfigurationKeys.StartedPage)!);
	}

	public ReactiveCommand<Unit, Unit> OnLayoutUpdated { get; }

	public int SelectedIndex
	{
		get => _selectedIndex;
		set => this.RaiseAndSetIfChanged(backingField: ref _selectedIndex, newValue: value);
	}

	public IEnumerable<BaseMenuItem> Menu
	{
		get => _menu;
		set => this.RaiseAndSetIfChanged(backingField: ref _menu, newValue: value);
	}
}