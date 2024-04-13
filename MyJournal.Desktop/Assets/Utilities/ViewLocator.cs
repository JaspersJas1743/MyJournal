using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using MyJournal.Desktop.ViewModels;

namespace MyJournal.Desktop.Assets.Utilities;

public class ViewLocator : IDataTemplate
{
	public Control? Build(object? data)
	{
		if (data is null)
			return null;

		string vmTypeName = data.GetType().FullName!;
		Type viewType = Type.GetType(typeName: vmTypeName.Replace(oldValue: "ViewModels", newValue: "Views").Replace(oldValue: "VM", newValue: "View")) ??
			throw new ArgumentException(message: $"View для {vmTypeName} не найдена.", paramName: nameof(data));
		App currentApplication = Application.Current as App
			?? throw new Exception(message: "Неизвестная ошибка.");
		Control control = currentApplication.GetService(serviceType: viewType) as Control ??
			throw new ArgumentException(message: $"Некорректный тип Control: {viewType.Name}.", paramName: nameof(data));
		control.DataContext = data;
		return control;
	}

	public bool Match(object? data)
		=> data is not null && data.GetType().BaseType!.IsEquivalentTo(other: typeof(BaseVM));
}