using Avalonia.Controls;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels;

public class ViewModelBase<TView> : ReactiveObject
	where TView : UserControl
{
}