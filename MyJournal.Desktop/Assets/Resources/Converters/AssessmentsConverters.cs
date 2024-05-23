using System.Collections.Generic;
using System.Linq;
using Avalonia.Data.Converters;
using MyJournal.Core.SubEntities;

namespace MyJournal.Desktop.Assets.Resources.Converters;

public static class AssessmentsConverters
{
	public static readonly IValueConverter OnlyAssessments = new FuncValueConverter<IEnumerable<PossibleAssessment>, IEnumerable<PossibleAssessment>?>(
		convert: possibleAssessments => possibleAssessments?.Where(
			predicate: a => a.GradeType == GradeTypes.Assessment
		)
	);
}