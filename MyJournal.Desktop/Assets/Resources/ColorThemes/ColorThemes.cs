using Avalonia.Styling;

namespace MyJournal.Desktop.Assets.Resources.ColorThemes;

public static class ColorThemes
{
	public static ThemeVariant Light { get; } = new ThemeVariant(
		key: nameof(ColorThemes.Light),
		inheritVariant: ThemeVariant.Light
	);

	public static ThemeVariant Dark { get; } = new ThemeVariant(
		key: nameof(ColorThemes.Dark),
		inheritVariant: ThemeVariant.Dark
	);
}