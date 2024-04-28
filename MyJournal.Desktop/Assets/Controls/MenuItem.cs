using Avalonia;
using Avalonia.Controls;
using DynamicData;
using MyJournal.Desktop.ViewModels;

namespace MyJournal.Desktop.Assets.Controls;

public enum MenuItemTypes
{
	Compact,
	Full
}

public sealed class MenuItem : ListBoxItem
{
	public const MenuItemTypes Full = MenuItemTypes.Full;
	public const MenuItemTypes Compact = MenuItemTypes.Compact;

	public static readonly StyledProperty<XamlSvg> ImageProperty = AvaloniaProperty.Register<MenuItem, XamlSvg>(name: nameof(Image));
	public static readonly StyledProperty<MenuItemTypes> ItemTypeProperty = AvaloniaProperty.Register<MenuItem, MenuItemTypes>(name: nameof(ItemType));
	public static readonly StyledProperty<string> HeaderProperty = AvaloniaProperty.Register<MenuItem, string>(name: nameof(Header));
	public static readonly StyledProperty<MenuItemVM> ItemContentProperty = AvaloniaProperty.Register<MenuItem, MenuItemVM>(name: nameof(ItemContent));

	public MenuItem() { }

	public MenuItem(string image, string header, MenuItemVM itemContent, MenuItemTypes itemType)
	{
		Image = new XamlSvg() { Classes = { image } };
		Header = header;
		ItemContent = itemContent;
		ItemType = itemType;
	}

	public XamlSvg Image
	{
		get => GetValue(property: ImageProperty);
		set => SetValue(property: ImageProperty, value: value);
	}

	public string ImageClasses
	{
		set
		{
			XamlSvg image = new XamlSvg();
			image.Classes.AddRange(items: value.Split());
			Image = image;
		}
	}

	public string Header
	{
		get => GetValue(property: HeaderProperty);
		set => SetValue(property: HeaderProperty, value: value);
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