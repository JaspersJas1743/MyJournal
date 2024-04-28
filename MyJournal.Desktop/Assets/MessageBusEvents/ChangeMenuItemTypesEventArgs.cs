using System;
using MyJournal.Desktop.Assets.Controls;

namespace MyJournal.Desktop.Assets.MessageBusEvents;

public sealed class ChangeMenuItemTypesEventArgs(MenuItemTypes menuItemTypes) : EventArgs
{
	public MenuItemTypes MenuItemTypes { get; } = menuItemTypes;
}