using Avalonia;
using Avalonia.Controls;
using DynamicData;
using MyJournal.Desktop.ViewModels;

namespace MyJournal.Desktop.Assets.Controls;

public sealed class MenuItem : ListBoxItem
{
	public static readonly StyledProperty<XamlSvg> ImageProperty = AvaloniaProperty.Register<MenuItem, XamlSvg>(name: nameof(Image));
	public static readonly StyledProperty<string> HeaderProperty = AvaloniaProperty.Register<MenuItem, string>(name: nameof(Header));
	public static readonly StyledProperty<BaseVM> ItemContentProperty = AvaloniaProperty.Register<MenuItem, BaseVM>(name: nameof(ItemContent));

	public MenuItem()
	{ }

	public MenuItem(string image, string header, BaseVM itemContent)
	{
		Image = new XamlSvg() { Classes = { image } };
		Header = header;
		ItemContent = itemContent;
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

	public BaseVM ItemContent
	{
		get => GetValue(property: ItemContentProperty);
		set => SetValue(property: ItemContentProperty, value: value);
	}
}