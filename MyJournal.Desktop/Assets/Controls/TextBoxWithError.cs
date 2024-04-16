using Avalonia;
using Avalonia.Controls;

namespace MyJournal.Desktop.Assets.Controls;

public class TextBoxWithError : TextBox
{
	public static readonly StyledProperty<bool> HaveErrorProperty =
		AvaloniaProperty.Register<TextBoxWithError, bool>(name: nameof(HaveError));

	public bool HaveError
	{
		get => GetValue(property: HaveErrorProperty);
		set => SetValue(property: HaveErrorProperty, value: value);
	}
}