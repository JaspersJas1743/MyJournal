using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MyJournal.API.Assets.Validation;

public sealed class ModelValidatorProvider : IModelValidatorProvider
{
	private Type? FindValidatorType(IEnumerable<object> attributes)
	{
		Attribute? attribute = attributes.OfType<Attribute>().FirstOrDefault(
			predicate: attribute => attribute.GetType().GenericTypeArguments.Any(
				predicate: type => type.GetInterfaces().Contains(value: typeof(IValidator))
			)
		);
		return attribute?.GetType().GenericTypeArguments.First();
	}

	public void CreateValidators(ModelValidatorProviderContext context)
	{
		if (context.ModelMetadata is not DefaultModelMetadata m || m.Attributes.TypeAttributes is null)
			return;

		Type? validatorType = FindValidatorType(attributes: m.Attributes.Attributes);
		if (validatorType is null)
			return;

		context.Results.Add(item: new ValidatorItem()
		{
			Validator  = new ModelValidator(validatorType: validatorType),
			IsReusable = false
		});
	}
}

public static class ModelValidationProviderExtension
{
	public static MvcOptions AddAutoValidation(this MvcOptions options)
	{
		options.ModelValidatorProviders.Add(item: new ModelValidatorProvider());
		return options;
	}
}