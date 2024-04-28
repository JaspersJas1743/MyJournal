using System;
using System.Reactive;
using System.Threading.Tasks;
using MyJournal.Desktop.Assets.Controls;
using MyJournal.Desktop.Assets.MessageBusEvents;
using MyJournal.Desktop.Assets.Utilities.ConfigurationService;
using ReactiveUI;

namespace MyJournal.Desktop.Models.Profile;

public sealed class ProfileChangeMenuItemTypeModel : ModelBase
{
	private readonly IConfigurationService _configurationService;

	private bool _shortTypeIsSelected;
	private bool _fullTypeIsSelected;

	public ProfileChangeMenuItemTypeModel(IConfigurationService configurationService)
	{
		_configurationService = configurationService;

		MenuItemTypes menuItemType = Enum.Parse<MenuItemTypes>(value: _configurationService.Get(key: ConfigurationKeys.MenuType) ?? nameof(MenuItemTypes.Full));

		FullTypeIsSelected = menuItemType == MenuItemTypes.Full;
		ShortTypeIsSelected = menuItemType == MenuItemTypes.Compact;

		SelectedShortType = ReactiveCommand.CreateFromTask(execute: ChangeMenuItemTypeToShort);
		SelectedFullType = ReactiveCommand.CreateFromTask(execute: ChangeMenuItemTypeToFull);
	}

	public bool ShortTypeIsSelected
	{
        get => _shortTypeIsSelected;
        set => this.RaiseAndSetIfChanged(backingField: ref _shortTypeIsSelected, newValue: value);
    }

	public bool FullTypeIsSelected
	{
        get => _fullTypeIsSelected;
        set => this.RaiseAndSetIfChanged(backingField: ref _fullTypeIsSelected, newValue: value);
    }

	private async Task ChangeMenuItemTypeToFull()
	{
		MessageBus.Current.SendMessage(new ChangeMenuItemTypesEventArgs(menuItemTypes: MenuItemTypes.Full));
		_configurationService.Set(key: ConfigurationKeys.MenuType, value: MenuItemTypes.Full);
	}

	private async Task ChangeMenuItemTypeToShort()
	{
		MessageBus.Current.SendMessage(new ChangeMenuItemTypesEventArgs(menuItemTypes: MenuItemTypes.Compact));
		_configurationService.Set(key: ConfigurationKeys.MenuType, value: MenuItemTypes.Compact);
	}

	public ReactiveCommand<Unit, Unit> SelectedShortType { get; }
	public ReactiveCommand<Unit, Unit> SelectedFullType { get; }
}