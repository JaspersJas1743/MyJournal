using System.Linq;
using Avalonia.Data.Converters;

namespace MyJournal.Desktop.Assets.Resources.Converters;

public static class BoolConverters
{
	public static readonly IMultiValueConverter NotAnd =
		new FuncMultiValueConverter<bool, bool>(x => !x.All(y => y));
}