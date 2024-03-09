using FluentValidation;

namespace MyJournal.API.Assets.Validation.PropertyValidationExtensions;

public static class AllElementsInCollectionExtensions
{
	public static IRuleBuilderOptions<T, IEnumerable<K>> AllElementsInCollection<T, K>(
		this IRuleBuilderOptions<T, IEnumerable<K>> ruleBuilder,
		Predicate<K> predicate
	) => ruleBuilder.Must(predicate: collection => collection.All(predicate: item => predicate(item)));

	public static IRuleBuilderOptions<T, IEnumerable<K>> AllElementsInCollection<T, K>(
		this IRuleBuilderInitial<T, IEnumerable<K>> ruleBuilder,
		Predicate<K> predicate
	) => ruleBuilder.Must(predicate: collection => collection.All(predicate: item => predicate(item)));
}