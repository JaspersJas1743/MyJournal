using System.Reflection;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MyJournal.API.Assets.Validation;

public sealed class ModelValidator(
    Type validatorType
) : IModelValidator
{
    public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
    {
        if (context.Model is null)
            return Enumerable.Empty<ModelValidationResult>();

        IValidator validator = (IValidator)Activator.CreateInstance(type: validatorType)!;
        ValidationResult? result = validator.Validate(context: GetValidationContext(model: context.Model));
        if (result.IsValid)
            return Enumerable.Empty<ModelValidationResult>();

        return result.Errors.Select(
            selector: failure => new ModelValidationResult(
                memberName: failure.PropertyName,
                message: failure.ErrorMessage
            )).AsEnumerable();
    }

    private IValidationContext GetValidationContext(object model)
    {
        Type genericType = typeof(ValidationContext<>).MakeGenericType(typeArguments: model.GetType());
        ConstructorInfo? constructor = genericType.GetConstructors().FirstOrDefault(
            predicate: ctor => ctor.GetParameters().Length == 1
        );

        if (constructor is null)
            throw new ArgumentException(message: $"Не удалось найти конструктор с 1 параметров в виде {genericType}", paramName: nameof(constructor));

        return (IValidationContext)constructor.Invoke(parameters: new object?[] { model });
    }
}