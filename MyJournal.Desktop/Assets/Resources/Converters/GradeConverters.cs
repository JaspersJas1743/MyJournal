using System;
using System.Globalization;
using Avalonia.Data.Converters;
using MyJournal.Core.SubEntities;

namespace MyJournal.Desktop.Assets.Resources.Converters;

public sealed class GradeConverters
{
	public static readonly IValueConverter IsFive =
		new FuncValueConverter<string?, bool>(convert: x => Double.TryParse(s: x, result: out double value, provider: CultureInfo.InvariantCulture) && value is <= 5 and >= 4.5);

	public static readonly IValueConverter IsFour =
		new FuncValueConverter<string?, bool>(convert: x => Double.TryParse(s: x, result: out double value, provider: CultureInfo.InvariantCulture) && value is < 4.5 and >= 3.5);

	public static readonly IValueConverter IsThree =
		new FuncValueConverter<string?, bool>(convert: x => Double.TryParse(s: x, result: out double value, provider: CultureInfo.InvariantCulture) && value is < 3.5 and >= 2.5);

	public static readonly IValueConverter IsTwo =
		new FuncValueConverter<string?, bool>(convert: x => Double.TryParse(s: x, result: out double value, provider: CultureInfo.InvariantCulture) && value < 2.5);

	public static readonly IValueConverter IsEmpty =
		new FuncValueConverter<string?, bool>(convert: x => x == "-.--");

	public static readonly IValueConverter IsTruancy =
		new FuncValueConverter<GradeTypes?, bool>(convert: x => x == GradeTypes.Truancy);
}