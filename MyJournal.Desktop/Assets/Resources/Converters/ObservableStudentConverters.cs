using Avalonia.Data.Converters;
using MyJournal.Desktop.Assets.Utilities.MarksUtilities;

namespace MyJournal.Desktop.Assets.Resources.Converters;

public static class ObservableStudentConverters
{
	public static readonly IValueConverter ShortFullName = new FuncValueConverter<ObservableStudent?, string>(
		convert: s => $"{s?.Surname} {s?.Name[index: 0]}. {s?.Patronymic?[index: 0]}."
	);
}