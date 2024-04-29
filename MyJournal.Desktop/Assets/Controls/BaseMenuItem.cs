using Avalonia;
using Avalonia.Controls;
using DynamicData;

namespace MyJournal.Desktop.Assets.Controls;

public class BaseMenuItem : ListBoxItem
{
	public static readonly StyledProperty<XamlSvg> ImageProperty = AvaloniaProperty.Register<MenuItem, XamlSvg>(name: nameof(Image));
	public static readonly StyledProperty<string> HeaderProperty = AvaloniaProperty.Register<MenuItem, string>(name: nameof(Header));

	public BaseMenuItem() { }

	public BaseMenuItem(string image, string header)
	{
		Image = new XamlSvg() { Classes = { image } };
		Header = header;
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
}