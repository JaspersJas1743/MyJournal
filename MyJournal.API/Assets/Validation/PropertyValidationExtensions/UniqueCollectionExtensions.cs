using FluentValidation;

namespace MyJournal.API.Assets.Validation.PropertyValidationExtensions;

public static class UniqueCollectionExtensions
{
	public static IRuleBuilderOptions<T, IEnumerable<K>> IsUniqueCollection<T, K>(
		this IRuleBuilderOptions<T, IEnumerable<K>> ruleBuilder
	) => ruleBuilder.Must(predicate: collection => collection.Distinct().Count().Equals(collection.Count()));

	public static IRuleBuilderOptions<T, IEnumerable<K>> IsUniqueCollection<T, K>(
		this IRuleBuilderOptions<T, IEnumerable<K>> ruleBuilder,
		IEqualityComparer<K> comparer
	) => ruleBuilder.Must(predicate: collection => collection.Distinct(comparer: comparer).Count().Equals(collection.Count()));

	public static IRuleBuilderOptions<T, IEnumerable<K>> IsUniqueCollection<T, K>(
		this IRuleBuilderInitial<T, IEnumerable<K>> ruleBuilder
	) => ruleBuilder.Must(predicate: collection => collection.Distinct().Count().Equals(collection.Count()));

	public static IRuleBuilderOptions<T, IEnumerable<K>> IsUniqueCollection<T, K>(
		this IRuleBuilderInitial<T, IEnumerable<K>> ruleBuilder,
		IEqualityComparer<K> comparer
	) => ruleBuilder.Must(predicate: collection => collection.Distinct(comparer: comparer).Count().Equals(collection.Count()));
}