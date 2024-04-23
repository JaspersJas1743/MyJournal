using Avalonia;
using Avalonia.Controls;

namespace MyJournal.Desktop.Assets.Controls;

public sealed class SelectionCard : RadioButton
{
	public static readonly StyledProperty<string?> HeaderProperty =
		AvaloniaProperty.Register<Button, string?>(name: nameof(Header));

	public object? Header
	{
		get => GetValue(property: HeaderProperty);
		set => SetValue(property: HeaderProperty, value: value);
	}
}