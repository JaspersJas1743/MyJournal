using System;
using MyJournal.Desktop.Assets.Controls;

namespace MyJournal.Desktop.Assets.Utilities.MenuConfigurationService;

public sealed class ChangeMenuItemsTypeEventArgs(MenuItemTypes menuItemTypes) : EventArgs
{
	public MenuItemTypes MenuItemTypes { get; } = menuItemTypes;
}