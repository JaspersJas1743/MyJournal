using MyJournal.Desktop.Models;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels;

public class BaseVM : ReactiveObject
{
	public BaseVM(ModelBase model)
		=> model.PropertyChanged += (_, args) => this.RaisePropertyChanged(propertyName: args.PropertyName);

	public BaseVM()
	{ }
}