using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using MyJournal.Desktop.ViewModels;

namespace MyJournal.Desktop;

public class ViewLocator : IDataTemplate
{
	public Control? Build(object? data)
	{
		if (data is null)
			return null;

		Type viewType = data.GetType().BaseType!.GenericTypeArguments.Single();

		App currentApplication = Application.Current as App ?? throw new Exception(message: "Неизвестная ошибка.");
		UserControl control = currentApplication.GetService(serviceType: viewType) as UserControl ??
			throw new ArgumentException(message: $"Некорректный тип UserControl: {viewType.Name}", paramName: nameof(data));
		control.DataContext = data;
		return control;
	}

	public bool Match(object? data)
	{
		if (data is null)
			return false;

		return data.GetType().BaseType!.IsGenericType && data.GetType().BaseType!.GetGenericTypeDefinition() == typeof(ViewModelBase<>);
	}
}