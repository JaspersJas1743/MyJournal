using System;
using System.Reactive.Linq;
using DynamicData.Binding;
using ReactiveUI;

namespace MyJournal.Desktop.Models;

public abstract class ModelWithErrorMessage : ValidatableModel
{
	private bool _haveError = false;
	private string _error = String.Empty;

	protected ModelWithErrorMessage()
	{
		this.WhenValueChanged(propertyAccessor: model => model.Error)
			.Select(selector: error => !String.IsNullOrWhiteSpace(value: error))
			.Subscribe(onNext: hasError => HaveError = hasError);

		this.WhenValueChanged(propertyAccessor: model => model.HaveError)
			.Where(predicate: hasError => !hasError)
			.Subscribe(onNext: _ => Error = String.Empty);
	}

	public bool HaveError
	{
		get => _haveError;
		set => this.RaiseAndSetIfChanged(ref _haveError, value);
	}

	public string Error
	{
		get => _error;
		set => this.RaiseAndSetIfChanged(backingField: ref _error, newValue: value);
	}
}