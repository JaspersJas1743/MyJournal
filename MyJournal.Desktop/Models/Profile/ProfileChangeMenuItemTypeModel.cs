using System.Reactive;
using System.Threading.Tasks;
using MyJournal.Desktop.Assets.Controls;
using MyJournal.Desktop.Assets.Utilities.MenuConfigurationService;
using ReactiveUI;

namespace MyJournal.Desktop.Models.Profile;

public sealed class ProfileChangeMenuItemTypeModel : ModelBase
{
	private readonly IMenuConfigurationService _menuConfigurationService;

	private bool _compactTypeIsSelected;
	private bool _fullTypeIsSelected;

	public ProfileChangeMenuItemTypeModel(IMenuConfigurationService menuConfigurationService)
	{
		_menuConfigurationService = menuConfigurationService;

		FullTypeIsSelected = IMenuConfigurationService.CurrentType == MenuItemTypes.Full;
		CompactTypeIsSelected = IMenuConfigurationService.CurrentType == MenuItemTypes.Compact;

		SelectedCompactType = ReactiveCommand.Create(execute: ChangeMenuItemTypeToCompact);
		SelectedFullType = ReactiveCommand.Create(execute: ChangeMenuItemTypeToFull);
	}

	public bool CompactTypeIsSelected
	{
        get => _compactTypeIsSelected;
        set => this.RaiseAndSetIfChanged(backingField: ref _compactTypeIsSelected, newValue: value);
    }

	public bool FullTypeIsSelected
	{
        get => _fullTypeIsSelected;
        set => this.RaiseAndSetIfChanged(backingField: ref _fullTypeIsSelected, newValue: value);
    }

	private void ChangeMenuItemTypeToFull()
		=> _menuConfigurationService.ChangeType(type: MenuItemTypes.Full);

	private void ChangeMenuItemTypeToCompact()
		=> _menuConfigurationService.ChangeType(type: MenuItemTypes.Compact);

	public ReactiveCommand<Unit, Unit> SelectedCompactType { get; }
	public ReactiveCommand<Unit, Unit> SelectedFullType { get; }
}