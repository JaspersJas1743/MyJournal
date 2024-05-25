using System;
using Avalonia;
using MyJournal.Desktop.ViewModels.Timetable;

namespace MyJournal.Desktop.Assets.MessageBusEvents;

public sealed class ChangeTimetableVisualizerEventArgs(Type timetableVM) : EventArgs
{
	public BaseTimetableVM TimetableVM { get; } = ((Application.Current as App)!.GetService(serviceType: timetableVM) as BaseTimetableVM)!;
}