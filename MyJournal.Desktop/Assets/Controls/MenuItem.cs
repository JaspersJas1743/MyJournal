using Avalonia;
using MyJournal.Desktop.ViewModels;

namespace MyJournal.Desktop.Assets.Controls;

public enum MenuItemTypes
{
	Compact,
	Full
}

public sealed class MenuItem : BaseMenuItem
{
	public const MenuItemTypes Full = MenuItemTypes.Full;
	public const MenuItemTypes Compact = MenuItemTypes.Compact;

	public static readonly StyledProperty<MenuItemTypes> ItemTypeProperty = AvaloniaProperty.Register<MenuItem, MenuItemTypes>(name: nameof(ItemType));
	public static readonly StyledProperty<MenuItemVM> ItemContentProperty = AvaloniaProperty.Register<MenuItem, MenuItemVM>(name: nameof(ItemContent));

	public MenuItem() { }

	public MenuItem(string image, string header, MenuItemVM itemContent, MenuItemTypes itemType)
		: base(image: image, header: header)
	{
		ItemContent = itemContent;
		ItemType = itemType;
	}

	public MenuItemVM ItemContent
	{
		get => GetValue(property: ItemContentProperty);
		set => SetValue(property: ItemContentProperty, value: value);
	}

	public MenuItemTypes ItemType
	{
		get => GetValue(property: ItemTypeProperty);
		set => SetValue(property: ItemTypeProperty, value: value);
	}
}