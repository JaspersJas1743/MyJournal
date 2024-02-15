using FluentValidation;

namespace MyJournal.API.Assets.Validation;

[AttributeUsage(validOn: AttributeTargets.Class)]
public sealed class ValidatorAttribute<T> : Attribute where T : IValidator
{
}